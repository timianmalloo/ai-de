using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Understanding;

internal enum AtlasMembershipCaptureState { CandidateComplete, Refused, Unavailable, Unstable, BudgetExceeded, Canceled }
internal enum AtlasMembershipEntryKind { RegularFile, Symlink, Gitlink }
internal sealed record AtlasMembershipEntry(string RelativePath, AtlasMembershipEntryKind Kind);
internal sealed record AtlasMembershipNotification(uint Action, string Name, bool NameTruncated);
internal sealed record AtlasMembershipDiagnostic(
    string Stage, string PinRole, string PinPath, bool EventSignaled, bool CompletionSucceeded,
    int NativeError, uint NativeBytes, AtlasMembershipNotification[] Notifications,
    bool Truncated, string? DecodeFailure, string RawPrefixHex, AtlasMembershipWatchOwnership Ownership);
internal sealed record AtlasMembershipWatchOwnership(
    long WatchId, uint IssuingNativeThreadId, int IssuingManagedThreadId, bool IssuerAlive,
    bool IssuerJoinCompleted, bool IssuerThreadPool, uint ObservingNativeThreadId,
    long DirectoryHandle, long OverlappedAddress, long EventHandle, bool NativeHandleClosed,
    bool OverlappedOwned, bool Disposed, int DisposeCalls, int ExplicitCancelCalls,
    bool? CancelSucceeded, int? CancelError);

internal sealed class AtlasInjectedCleanupTimeout(string label) : IOException(label)
{
    internal string Label { get; } = label;
}

internal sealed class AtlasRetainedCleanupException(string label) : IOException(label);
internal sealed record AtlasCleanupCharges(
    int Capacity, int ChargedOwners, int RetainedOwners, int RetainedPins, int RetainedCreations,
    int WatchBuffers, int IssuerLeases, int IssuerThreads);

/// <summary>Labelled qualification faults before completion acknowledgement; not a simulated kernel stall.</summary>
internal sealed class AtlasCleanupFaultPlan
{
    internal string? PinPath;
    internal int PinTimeout;
    internal int CanceledCreationTimeout;
    internal int IssuerDrainTimeout;
    internal Action? AfterNativeCreation;
    internal WeakReference<IDisposable>? FailedResource;

    internal void BeforePinCleanup(string path, IDisposable resource)
    {
        if (string.Equals(path, PinPath, StringComparison.OrdinalIgnoreCase)
            && Interlocked.Exchange(ref PinTimeout, 0) != 0)
        {
            FailedResource = new(resource);
            throw new AtlasInjectedCleanupTimeout("qualification-pin-completion-timeout");
        }
    }

    internal void BeforeCanceledCreationCleanup(IDisposable resource)
    {
        if (Interlocked.Exchange(ref CanceledCreationTimeout, 0) != 0)
        {
            FailedResource = new(resource);
            throw new AtlasInjectedCleanupTimeout("qualification-canceled-creation-completion-timeout");
        }
    }

    internal void BeforeIssuerDrain()
    {
        if (Interlocked.Exchange(ref IssuerDrainTimeout, 0) != 0)
            throw new AtlasInjectedCleanupTimeout("qualification-issuer-drain-completion-timeout");
    }
}

/// <summary>
/// Qualification-only capture. No issuer, scope grant, content permission or production registration
/// consumes this candidate until the parent Security gate accepts NQ1 and NQ2.
/// </summary>
internal sealed class AtlasGitMembership(string executable, string expectedSha256, string expectedVersion)
{
    internal const int MaxStdoutBytes = 4 * 1024 * 1024;
    internal const int MaxIndexBytes = 8 * 1024 * 1024;
    internal const int MaxRecords = 25_000;
    internal const int MaxNativeHandles = 128;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly SemaphoreSlim GitSlots = new(2, 2);
    private static readonly Meter Meter = new("AiDe.Core.AtlasGitMembership");
    private static readonly Counter<long> Captures = Meter.CreateCounter<long>("atlas.membership.captures");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("atlas.membership.duration", "ms");
    private static readonly string[] DiscoveryArguments =
        ["rev-parse", "--path-format=absolute", "--show-toplevel", "--absolute-git-dir", "--git-common-dir", "--git-path", "index", "--show-ref-format"];
    private static readonly string[] HeadArguments = ["rev-parse", "HEAD", "--symbolic-full-name", "HEAD"];
    private static readonly string[] MembershipArguments = ["ls-files", "--cached", "--stage", "-z", "--full-name", "--sparse"];

    internal Action<int>? ProcessStartedForQualification { get; set; }
    internal ValueTask<AtlasMembershipSnapshot> CaptureAsync(
        string sourceRoot, AtlasObjectIdentity expectedRootIdentity, CancellationToken cancellationToken) =>
        CaptureForQualificationAsync(sourceRoot, expectedRootIdentity, cancellationToken);
    // Explicit qualification sink only. Paths and notification names never enter the ordinary metrics/reasons.
    internal Action<AtlasMembershipDiagnostic>? DiagnosticForQualification { get; set; }
    internal AtlasCleanupFaultPlan? CleanupFaultsForQualification { get; set; }
    internal static int LiveWatchBuffersForQualification => NativeDirectoryChange.LiveBuffers;
    internal static AtlasCleanupCharges CleanupChargesForQualification => CleanupLedger.Read();
    internal static AtlasCleanupCharges RetryRetainedCleanup()
    {
        foreach (var owner in CleanupLedger.RetryableOwners())
        {
            try { owner.Dispose(); }
            catch (AtlasRetainedCleanupException) { }
        }
        return CleanupLedger.Read();
    }

    internal async ValueTask<AtlasMembershipSnapshot> CaptureForQualificationAsync(
        string sourceRoot, AtlasObjectIdentity expectedRootIdentity, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var invocations = 0;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var pins = new PinSet(DiagnosticForQualification, deadline.Token, CleanupFaultsForQualification);
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            Require(OperatingSystem.IsWindows(), AtlasMembershipCaptureState.Refused, "windows-native-evidence-required");
            sourceRoot = OrdinaryPath(sourceRoot);
            var root = pins.Add(sourceRoot, trackChanges: true, role: "source-root");
            Observe("source-root-pinned");
            Require(root.Identity.Equals(expectedRootIdentity), AtlasMembershipCaptureState.Refused, "source-root-identity-mismatch");
            pins.AddAncestors(sourceRoot);
            executable = OrdinaryPath(executable);
            var executablePin = pins.Add(executable, trackChanges: true, role: "approved-executable");
            pins.AddAncestors(executable);
            Require(string.Equals(executablePin.Digest(long.MaxValue, deadline.Token), expectedSha256, StringComparison.OrdinalIgnoreCase),
                AtlasMembershipCaptureState.Refused, "approved-executable-digest-mismatch");

            var version = await Invoke(["--version"], 4096);
            Require(StrictUtf8.GetString(version).TrimEnd('\r', '\n') == expectedVersion,
                AtlasMembershipCaptureState.Refused, "approved-executable-version-mismatch");
            var discovery = await Invoke(DiscoveryArguments, 16 * 1024);
            var association = ParseDiscovery(discovery, sourceRoot);
            pins.Add(association.Repository, trackChanges: true, role: "repository");
            pins.Add(association.GitDirectory, trackChanges: true, role: "worktree-admin");
            pins.Add(association.CommonDirectory, trackChanges: true, role: "common-admin");
            pins.AddAncestors(association.GitDirectory);
            pins.AddAncestors(association.CommonDirectory);
            for (var directory = sourceRoot; ; directory = Path.GetDirectoryName(directory)!)
            {
                pins.Add(directory, trackChanges: true, role: "discovery-ancestor");
                if (string.Equals(directory, association.Repository, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            pins.AddIfPresent(Path.Combine(association.Repository, ".git"), "git-selector");
            pins.AddIfPresent(Path.Combine(association.GitDirectory, "commondir"), "commondir-selector");
            pins.AddIfPresent(Path.Combine(association.GitDirectory, "config.worktree"), "worktree-config");
            pins.AddIfPresent(Path.Combine(association.CommonDirectory, "config"), "common-config");
            pins.Add(Path.Combine(association.GitDirectory, "HEAD"), trackChanges: true, role: "worktree-HEAD");
            var index = pins.Add(association.Index, trackChanges: true, role: "worktree-index");
            var indexDigest = index.Digest(MaxIndexBytes, deadline.Token);
            pins.AddIfPresent(Path.Combine(association.CommonDirectory, "packed-refs"), "packed-refs");
            pins.AddTreeIfPresent(Path.Combine(association.CommonDirectory, "refs"), "common-refs");
            if (!string.Equals(association.GitDirectory, association.CommonDirectory, StringComparison.OrdinalIgnoreCase))
            {
                pins.AddTreeIfPresent(Path.Combine(association.GitDirectory, "refs"), "worktree-refs");
            }
            Observe("administrative-pins-held");

            var head = await Invoke(HeadArguments, 4096);
            ValidateHead(head);
            var membership = await Invoke(MembershipArguments, MaxStdoutBytes);
            var entries = DecodeMembership(membership, association.Repository, sourceRoot);
            var finalDiscovery = await Invoke(DiscoveryArguments, 16 * 1024);
            var finalHead = await Invoke(HeadArguments, 4096);
            Require(discovery.AsSpan().SequenceEqual(finalDiscovery) && head.AsSpan().SequenceEqual(finalHead),
                AtlasMembershipCaptureState.Unstable, "git-association-changed");
            var nativeFailure = pins.CurrentnessFailure;
            Require(nativeFailure is null, AtlasMembershipCaptureState.Unstable, nativeFailure ?? "snapshot-changed");
            deadline.Token.ThrowIfCancellationRequested();
            return Finish(AtlasMembershipCaptureState.CandidateComplete, null, entries,
                association, StrictUtf8.GetString(head).Split('\n')[0].TrimEnd('\r'), indexDigest, pins);

            async Task<byte[]> Invoke(string[] arguments, int maximumBytes)
            {
                Require(++invocations <= 6, AtlasMembershipCaptureState.BudgetExceeded, "git-invocation-budget");
                var stage = invocations switch
                {
                    1 => "git-version",
                    2 => "git-discovery",
                    3 => "git-head",
                    4 => "git-membership",
                    5 => "git-discovery-recheck",
                    6 => "git-head-recheck",
                    _ => "git-invocation",
                };
                Observe("before-" + stage);
                var result = await RunAsync(sourceRoot, arguments, maximumBytes, deadline.Token).ConfigureAwait(false);
                Observe("after-" + stage);
                return result;
            }
        }
        catch (CaptureFailure failure)
        {
            Observe("capture-refused-" + failure.Code);
            return Fail(failure.State, failure.Code);
        }
        catch (OperationCanceledException)
        {
            return Fail(cancellationToken.IsCancellationRequested ? AtlasMembershipCaptureState.Canceled
                : AtlasMembershipCaptureState.BudgetExceeded, "capture-canceled-or-deadline");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or Win32Exception
            or DecoderFallbackException or ArgumentException or InvalidOperationException)
        {
            return Fail(AtlasMembershipCaptureState.Unavailable, "native-or-git-evidence-unavailable");
        }

        AtlasMembershipSnapshot Fail(AtlasMembershipCaptureState state, string reason)
        {
            try
            {
                pins.Dispose();
                return Finish(state, reason, [], null, null, null, null);
            }
            catch (AtlasRetainedCleanupException)
            {
                return Finish(AtlasMembershipCaptureState.Unavailable, "native-cleanup-retained", [], null, null, null, pins);
            }
        }

        AtlasMembershipSnapshot Finish(AtlasMembershipCaptureState state, string? reason, AtlasMembershipEntry[] entries,
            Association? association, string? head, string? indexDigest, PinSet? retained)
        {
            timer.Stop();
            Captures.Add(1, new KeyValuePair<string, object?>("state", state.ToString()));
            Duration.Record(timer.Elapsed.TotalMilliseconds);
            return new AtlasMembershipSnapshot(state, reason, entries, association, head, indexDigest,
                invocations, timer.Elapsed, retained, DiagnosticForQualification,
                retained?.AssociationIdentityStamp());
        }

        void Observe(string stage) => pins.ObserveForQualification(stage, DiagnosticForQualification);
    }

    private async Task<byte[]> RunAsync(string directory, string[] arguments, int maximumBytes, CancellationToken cancellationToken)
    {
        await GitSlots.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var invocation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            invocation.CancelAfter(TimeSpan.FromSeconds(5));
            using var process = new Process { StartInfo = StartInfo(directory, arguments) };
            Require(process.Start(), AtlasMembershipCaptureState.Unavailable, "git-start-failed");
            ProcessStartedForQualification?.Invoke(process.Id);
            var stdout = ReadBounded(process.StandardOutput.BaseStream, maximumBytes, invocation);
            var stderr = ReadBounded(process.StandardError.BaseStream, 16 * 1024, invocation);
            try
            {
                await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(invocation.Token)).ConfigureAwait(false);
                Require(process.ExitCode == 0, AtlasMembershipCaptureState.Unavailable, "git-exit-failure");
                return await stdout.ConfigureAwait(false);
            }
            catch
            {
                invocation.Cancel();
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await process.WaitForExitAsync(cleanup.Token).ConfigureAwait(false);
                try
                {
                    await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is OperationCanceledException or CaptureFailure or IOException)
                {
                    // The original failure remains authoritative after both stream readers stop.
                }

                throw;
            }
        }
        finally
        {
            GitSlots.Release();
        }
    }

    private ProcessStartInfo StartInfo(string directory, string[] arguments)
    {
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        info.Environment.Clear();
        info.Environment["SystemRoot"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        info.Environment["WINDIR"] = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        info.Environment["ComSpec"] = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        info.Environment["PATH"] = Path.GetDirectoryName(executable) + Path.PathSeparator + Environment.SystemDirectory;
        info.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        info.Environment["GIT_CONFIG_GLOBAL"] = "NUL";
        info.Environment["GIT_CONFIG_SYSTEM"] = "NUL";
        info.Environment["GIT_TERMINAL_PROMPT"] = "0";
        info.Environment["GIT_OPTIONAL_LOCKS"] = "0";
        info.Environment["GIT_NO_REPLACE_OBJECTS"] = "1";
        info.Environment["GIT_NO_LAZY_FETCH"] = "1";
        info.Environment["GIT_ATTR_NOSYSTEM"] = "1";
        info.Environment["GIT_PAGER"] = "";
        info.Environment["PAGER"] = "";
        info.ArgumentList.Add("--no-pager");
        info.ArgumentList.Add("--no-optional-locks");
        foreach (var setting in new[]
        {
            "core.fsmonitor=false", "core.untrackedCache=false", "submodule.recurse=false",
            "maintenance.auto=false", "gc.auto=0", "credential.helper=", "protocol.allow=never",
            "core.hooksPath=NUL",
        })
        {
            info.ArgumentList.Add("-c");
            info.ArgumentList.Add(setting);
        }

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return info;
    }

    private static async Task<byte[]> ReadBounded(Stream stream, int maximumBytes, CancellationTokenSource invocation)
    {
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var count = await stream.ReadAsync(buffer, invocation.Token).ConfigureAwait(false);
            if (count == 0)
            {
                return output.ToArray();
            }

            if (output.Length + count > maximumBytes)
            {
                invocation.Cancel();
                throw new CaptureFailure(AtlasMembershipCaptureState.BudgetExceeded, "git-stream-budget");
            }

            output.Write(buffer, 0, count);
        }
    }

    internal static AtlasMembershipEntry[] DecodeMembership(byte[] body, string repository, string sourceRoot)
    {
        Require(body.Length <= MaxStdoutBytes, AtlasMembershipCaptureState.BudgetExceeded, "git-stdout-budget");
        if (body.Length == 0)
        {
            return [];
        }

        Require(body[^1] == 0, AtlasMembershipCaptureState.Refused, "incomplete-membership-framing");
        var rows = StrictUtf8.GetString(body).Split('\0');
        Require(rows.Length - 1 <= MaxRecords, AtlasMembershipCaptureState.BudgetExceeded, "membership-record-budget");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<AtlasMembershipEntry>();
        foreach (var row in rows.AsSpan(0, rows.Length - 1))
        {
            var tab = row.IndexOf('\t');
            Require(tab > 0, AtlasMembershipCaptureState.Refused, "malformed-membership-header");
            var header = row[..tab].Split(' ');
            Require(header.Length == 3 && IsObjectId(header[1]) && header[2] == "0",
                AtlasMembershipCaptureState.Refused, "noncanonical-membership-stage");
            var relative = row[(tab + 1)..];
            Require(!string.IsNullOrEmpty(relative) && StrictUtf8.GetByteCount(relative) <= 4096
                && !relative.Contains('\\') && !relative.Contains(':') && !relative.StartsWith('/')
                && relative.Split('/').All(component => component.Length > 0 && component is not "." and not "..")
                && seen.Add(relative), AtlasMembershipCaptureState.Refused, "unsafe-or-duplicate-membership-path");
            var kind = header[0] switch
            {
                "100644" or "100755" => AtlasMembershipEntryKind.RegularFile,
                "120000" => AtlasMembershipEntryKind.Symlink,
                "160000" => AtlasMembershipEntryKind.Gitlink,
                _ => throw new CaptureFailure(AtlasMembershipCaptureState.Refused, "unsupported-membership-mode"),
            };
            var absolute = Path.GetFullPath(Path.Combine(repository, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!UnderOrSame(sourceRoot, absolute))
            {
                continue;
            }

            entries.Add(new AtlasMembershipEntry(Path.GetRelativePath(sourceRoot, absolute), kind));
        }

        return entries.ToArray();
    }

    private static Association ParseDiscovery(byte[] body, string sourceRoot)
    {
        var lines = StrictUtf8.GetString(body).TrimEnd('\r', '\n').Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        Require(lines.Length == 5 && lines[4] == "files", AtlasMembershipCaptureState.Refused, "unsupported-admin-association");
        var association = new Association(OrdinaryPath(lines[0]), OrdinaryPath(lines[1]), OrdinaryPath(lines[2]), OrdinaryPath(lines[3]));
        Require(UnderOrSame(association.Repository, sourceRoot) && UnderOrSame(association.GitDirectory, association.Index),
            AtlasMembershipCaptureState.Refused, "admin-association-outside-boundary");
        return association;
    }

    private static void ValidateHead(byte[] body)
    {
        var lines = StrictUtf8.GetString(body).TrimEnd('\r', '\n').Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        Require(lines.Length == 2 && IsObjectId(lines[0])
            && (lines[1] == "HEAD" || lines[1].StartsWith("refs/", StringComparison.Ordinal)),
            AtlasMembershipCaptureState.Refused, "malformed-head-observation");
    }

    private static bool IsObjectId(string value) => value.Length is 40 or 64
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string OrdinaryPath(string path)
    {
        Require(Path.IsPathFullyQualified(path) && !path.StartsWith(@"\\", StringComparison.Ordinal),
            AtlasMembershipCaptureState.Refused, "ordinary-local-path-required");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static bool UnderOrSame(string root, string path) => string.Equals(root, path, PathComparison.ForThisFileSystem)
        || path.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, PathComparison.ForThisFileSystem);

    private static void Require(bool condition, AtlasMembershipCaptureState state, string code)
    {
        if (!condition)
        {
            throw new CaptureFailure(state, code);
        }
    }

    internal sealed class CaptureFailure(AtlasMembershipCaptureState state, string code) : Exception(code)
    {
        internal AtlasMembershipCaptureState State { get; } = state;
        internal string Code { get; } = code;
    }

    internal sealed record Association(string Repository, string GitDirectory, string CommonDirectory, string Index);

    /// <summary>One pre-reserved strong owner per capture. Retained debt blocks new admission.</summary>
    private static class CleanupLedger
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<PinSet, bool> Owners = [];
        internal const int Capacity = MaxNativeHandles / 2;

        internal static void Reserve(PinSet owner)
        {
            lock (Gate)
            {
                Require(!Owners.Values.Any(retained => retained), AtlasMembershipCaptureState.Unavailable,
                    "retained-cleanup-blocks-admission");
                Require(Owners.Count < Capacity, AtlasMembershipCaptureState.BudgetExceeded, "cleanup-owner-budget");
                Owners.Add(owner, false);
            }
        }

        internal static void Retain(PinSet owner)
        {
            lock (Gate)
            {
                if (Owners.ContainsKey(owner))
                    Owners[owner] = true;
            }
        }

        internal static void Release(PinSet owner)
        {
            lock (Gate)
                Owners.Remove(owner);
        }

        internal static PinSet[] RetryableOwners()
        {
            lock (Gate)
                return Owners.Where(pair => pair.Value && pair.Key.CleanupStarted).Select(pair => pair.Key).ToArray();
        }

        internal static AtlasCleanupCharges Read()
        {
            KeyValuePair<PinSet, bool>[] owners;
            lock (Gate)
                owners = Owners.ToArray();
            return new(Capacity, owners.Length, owners.Count(pair => pair.Value),
                owners.Where(pair => pair.Value).Sum(pair => pair.Key.PinCount),
                owners.Where(pair => pair.Value).Sum(pair => pair.Key.UnpublishedCount),
                LiveWatchBuffersForQualification, NativeWatchIssuer.LiveLeases, NativeWatchIssuer.LiveThreads);
        }
    }

    internal sealed class PinSet(
        Action<AtlasMembershipDiagnostic>? lifecycleSink = null, CancellationToken cancellationToken = default,
        AtlasCleanupFaultPlan? cleanupFaults = null) : IDisposable
    {
        private readonly Dictionary<string, NativePin> _pins = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;
        private bool _diagnosticLoss;
        private NativeWatchIssuer.NativeWatchLease? _issuerLease;
        private readonly object _cleanupGate = new();
        private readonly List<IDisposable> _unpublished = [];
        private bool _registered;
        private bool _cleanupStarted;
        internal bool CleanupStarted => Volatile.Read(ref _cleanupStarted);
        internal int PinCount => _pins.Count;
        internal int UnpublishedCount => _unpublished.Count;

        internal NativePin Add(string path, bool trackChanges, string role = "ancestor")
        {
            Require(!_disposed, AtlasMembershipCaptureState.Refused, "disposed-capture");
            path = OrdinaryPath(path);
            if (_pins.TryGetValue(path, out var existing))
            {
                existing.TrackChanges |= trackChanges;
                existing.DiagnosticRoles.Add(role);
                return existing;
            }

            Require(_pins.Count < MaxNativeHandles / 2, AtlasMembershipCaptureState.BudgetExceeded, "native-handle-budget");
            ReserveOwner();
            _issuerLease ??= NativeWatchIssuer.Acquire(cleanupFaults);
            var pin = new NativePin(path, trackChanges, _issuerLease.Owner, cancellationToken, cleanupFaults, RetainUnpublished);
            pin.DiagnosticForQualification = lifecycleSink;
            pin.DiagnosticRoles.Add(role);
            _pins.Add(path, pin);
            return pin;
        }

        internal void ReserveOwner()
        {
            Require(!_disposed, AtlasMembershipCaptureState.Refused, "disposed-capture");
            if (_registered)
                return;
            CleanupLedger.Reserve(this);
            _registered = true;
        }

        internal void AddAncestors(string path)
        {
            for (var parent = Path.GetDirectoryName(path); parent is not null; parent = Path.GetDirectoryName(parent))
            {
                Add(parent, trackChanges: false);
            }
        }

        internal void AddIfPresent(string path, string role = "admin-entry")
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                Add(path, trackChanges: true, role);
            }
        }

        internal void AddTreeIfPresent(string path, string role = "ref-storage")
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            Add(path, trackChanges: true, role);
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                var pin = Add(entry, trackChanges: true, role);
                if (pin.IsDirectory)
                {
                    AddTreeIfPresent(entry, role);
                }
            }
        }

        internal string? CurrentnessFailure => _disposed ? "snapshot-disposed"
            : _diagnosticLoss || _pins.Values.Any(pin => pin.DiagnosticLoss) ? "qualification-diagnostic-loss"
            : _pins.Values.Select(pin => pin.CurrentnessFailure).FirstOrDefault(failure => failure is not null);
        internal bool IsCurrent() => CurrentnessFailure is null;

        internal void PrepareExclusiveSourceRead(string sourceRoot)
        {
            lock (_cleanupGate)
            {
                sourceRoot = OrdinaryPath(sourceRoot);
                _pins.TryGetValue(sourceRoot, out var root);
                Require(!_disposed && CurrentnessFailure is null
                    && root is not null && root.DiagnosticRoles.Contains("source-root"),
                    AtlasMembershipCaptureState.Unstable, "source-root-handoff-unavailable");
                var parent = Path.GetDirectoryName(sourceRoot);
                Require(parent is not null, AtlasMembershipCaptureState.Refused, "source-root-parent-required");
                // The parent-recursive notification is a superset of the root namespace watch.
                // It is armed before releasing the root handle that conflicts with S's exclusive open.
                Add(parent!, trackChanges: true, role: "source-root-handoff-parent");
                Require(CurrentnessFailure is null, AtlasMembershipCaptureState.Unstable, "source-root-handoff-changed");
                try
                {
                    root!.Dispose();
                    _pins.Remove(sourceRoot);
                }
                catch
                {
                    CleanupLedger.Retain(this);
                    throw;
                }
            }
        }

        internal string AssociationIdentityStamp()
        {
            var identities = _pins.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}|{pair.Value.Identity.VolumeSerial}|{pair.Value.Identity.FileIndex}");
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", identities))));
        }

        internal void ObserveForQualification(string stage, Action<AtlasMembershipDiagnostic>? sink)
        {
            if (sink is null || _disposed)
                return;
            foreach (var pin in _pins.Values)
            {
                try
                {
                    var diagnostic = pin.ObserveForQualification(stage);
                    if (diagnostic is not null)
                        sink(diagnostic);
                }
                catch (Exception)
                {
                    _diagnosticLoss = true;
                }
            }
        }

        public void Dispose()
        {
            lock (_cleanupGate)
            {
                var retry = _cleanupStarted;
                _disposed = true;
                _cleanupStarted = true;
                var failures = new List<Exception>();
                foreach (var pair in _pins.Reverse().ToArray())
                {
                    try
                    {
                        pair.Value.Dispose();
                        _diagnosticLoss |= pair.Value.DiagnosticLoss;
                        _pins.Remove(pair.Key);
                    }
                    catch (Exception failure)
                    {
                        failures.Add(failure);
                        CleanupLedger.Retain(this);
                    }
                }
                if (retry)
                {
                    foreach (var resource in _unpublished.ToArray())
                    {
                        try
                        {
                            resource.Dispose();
                            _unpublished.Remove(resource);
                        }
                        catch (Exception failure)
                        {
                            failures.Add(failure);
                        }
                    }
                }
                else if (_unpublished.Count > 0)
                {
                    failures.Add(new AtlasRetainedCleanupException("unpublished-cleanup-awaits-retry"));
                }
                if (_pins.Count == 0 && _unpublished.Count == 0)
                {
                    try
                    {
                        _issuerLease?.Dispose();
                        _issuerLease = null;
                    }
                    catch (Exception failure)
                    {
                        failures.Add(failure);
                    }
                }
                if (failures.Count > 0)
                {
                    CleanupLedger.Retain(this);
                    var label = failures.OfType<AtlasInjectedCleanupTimeout>().FirstOrDefault()?.Label ?? "native-cleanup-retained";
                    throw new AtlasRetainedCleanupException(label);
                }
                CleanupLedger.Release(this);
                _registered = false;
            }
        }

        private void RetainUnpublished(IDisposable resource)
        {
            lock (_cleanupGate)
            {
                if (!_unpublished.Contains(resource))
                    _unpublished.Add(resource);
                CleanupLedger.Retain(this);
            }
        }
    }

    /// <summary>
    /// Bounded producer-consumer: one shared issuing thread and a bounded command queue.
    /// A pin set's lease ends only after its watches have canceled and drained. The last lease
    /// completes the queue and joins the issuer, so no pending watch loses its issuing thread.
    /// </summary>
    internal sealed class NativeWatchIssuer
    {
        private static readonly object Gate = new();
        private static NativeWatchIssuer? _shared;
        private static int _liveThreads;
        private static int _peakThreads;
        private readonly BlockingCollection<Action> _commands = new(MaxNativeHandles / 2);
        private readonly Thread _thread;
        private int _references;
        private bool _stopping;

        private NativeWatchIssuer()
        {
            _thread = new Thread(Run) { IsBackground = true, Name = "Atlas native watch issuer" };
            _thread.Start();
        }

        internal static int LiveThreads => Volatile.Read(ref _liveThreads);
        internal static int PeakThreads => Volatile.Read(ref _peakThreads);
        internal static int LiveLeases
        {
            get
            {
                lock (Gate)
                    return _shared?._references ?? 0;
            }
        }

        internal static NativeWatchLease Acquire(AtlasCleanupFaultPlan? cleanupFaults = null)
        {
            lock (Gate)
            {
                var owner = _shared ??= new NativeWatchIssuer();
                Require(!owner._stopping, AtlasMembershipCaptureState.Unavailable, "native-issuer-shutdown-pending");
                owner._references++;
                return new NativeWatchLease(owner, cleanupFaults);
            }
        }

        internal T Invoke<T>(Func<T> create, CancellationToken cancellationToken,
            Action<IDisposable> retainCleanupFailure, AtlasCleanupFaultPlan? cleanupFaults = null) where T : IDisposable
        {
            ArgumentNullException.ThrowIfNull(retainCleanupFailure);
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _commands.Add(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }
                try
                {
                    var result = create();
                    if (cleanupFaults is not null)
                        Interlocked.Exchange(ref cleanupFaults.AfterNativeCreation, null)?.Invoke();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            cleanupFaults?.BeforeCanceledCreationCleanup(result);
                            result.Dispose();
                        }
                        catch
                        {
                            retainCleanupFailure(result);
                            throw;
                        }
                        completion.TrySetCanceled(cancellationToken);
                    }
                    else
                    {
                        completion.TrySetResult(result);
                    }
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                }
            }, cancellationToken);
            // Once queued, acknowledge creation/cancellation before releasing the owner's lease.
            return completion.Task.GetAwaiter().GetResult();
        }

        private void Run()
        {
            var live = Interlocked.Increment(ref _liveThreads);
            if (live > Volatile.Read(ref _peakThreads))
                Interlocked.Exchange(ref _peakThreads, live);
            try
            {
                foreach (var command in _commands.GetConsumingEnumerable())
                    command();
            }
            finally
            {
                Interlocked.Decrement(ref _liveThreads);
            }
        }

        private void Release(AtlasCleanupFaultPlan? cleanupFaults)
        {
            lock (Gate)
            {
                if (_references > 1)
                {
                    _references--;
                    return;
                }
                if (!_stopping)
                {
                    _stopping = true;
                    _commands.CompleteAdding();
                }
                cleanupFaults?.BeforeIssuerDrain();
                if (!_thread.Join(TimeSpan.FromSeconds(2)))
                    throw new IOException("Native issuing thread did not drain; ownership remains retained.");
                _commands.Dispose();
                _references = 0;
                _shared = null;
            }
        }

        internal sealed class NativeWatchLease(NativeWatchIssuer owner, AtlasCleanupFaultPlan? cleanupFaults) : IDisposable
        {
            private NativeWatchIssuer? _owner = owner;
            private readonly object _releaseGate = new();
            internal NativeWatchIssuer Owner => _owner ?? throw new ObjectDisposedException(nameof(NativeWatchLease));
            public void Dispose()
            {
                lock (_releaseGate)
                {
                    if (_owner is not { } current)
                        return;
                    current.Release(cleanupFaults);
                    _owner = null;
                }
            }
        }
    }

    /// <summary>
    /// Shares data reads only. A kernel directory notification guards namespace ABA;
    /// per-file USN records do not report every change to a directory's children.
    /// FSCTL_READ_FILE_USN_DATA: Windows SDK 10.0.26100.0 winioctl.h, function 58 / METHOD_NEITHER.
    /// </summary>
    internal sealed class NativePin : IDisposable
    {
        private readonly SafeFileHandle _handle;
        private readonly string _path;
        private readonly byte[] _changeRecord;
        private readonly NativeWatchIssuer? _watchIssuer;
        private readonly CancellationToken _captureCancellation;
        private readonly AtlasCleanupFaultPlan? _cleanupFaults;
        private readonly Action<IDisposable>? _retainUnpublished;
        private NativeDirectoryChange? _directoryChange;
        private bool _trackChanges;
        internal SortedSet<string> DiagnosticRoles { get; } = new(StringComparer.Ordinal);
        internal Action<AtlasMembershipDiagnostic>? DiagnosticForQualification { get; set; }
        internal bool DiagnosticLoss { get; private set; }
        internal bool TrackChanges
        {
            get => _trackChanges;
            set
            {
                if (value && IsDirectory && _directoryChange is null)
                {
                    _directoryChange = _watchIssuer is null
                        ? new NativeDirectoryChange(_path)
                        : _watchIssuer.Invoke(() => new NativeDirectoryChange(_path), _captureCancellation,
                            _retainUnpublished ?? throw new InvalidOperationException("Native owner requires a cleanup retainer."), _cleanupFaults);
                }
                _trackChanges = value;
            }
        }
        internal AtlasObjectIdentity Identity { get; }
        internal bool IsDirectory { get; }

        internal NativePin(string path, bool trackChanges, NativeWatchIssuer? watchIssuer = null,
            CancellationToken captureCancellation = default, AtlasCleanupFaultPlan? cleanupFaults = null,
            Action<IDisposable>? retainUnpublished = null)
        {
            _path = path;
            _watchIssuer = watchIssuer;
            _captureCancellation = captureCancellation;
            _cleanupFaults = cleanupFaults;
            _retainUnpublished = retainUnpublished;
            _handle = CreateFileW(path, 0x80000000, 1, IntPtr.Zero, 3, 0x02000000 | 0x00200000, IntPtr.Zero);
            if (_handle.IsInvalid)
            {
                _handle.Dispose();
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                var info = Information();
                Require((info.Attributes & (uint)FileAttributes.ReparsePoint) == 0,
                    AtlasMembershipCaptureState.Refused, "administrative-reparse-refused");
                IsDirectory = (info.Attributes & (uint)FileAttributes.Directory) != 0;
                Identity = ObjectIdentity(info);
                Require(FinalPathMatches(), AtlasMembershipCaptureState.Refused, "native-path-association-mismatch");
                TrackChanges = trackChanges;
                _changeRecord = ChangeRecord();
            }
            catch
            {
                try
                {
                    _directoryChange?.Dispose();
                }
                catch
                {
                    _retainUnpublished?.Invoke(this);
                    throw;
                }
                finally
                {
                    _handle.Dispose();
                }
                throw;
            }
        }

        internal string? CurrentnessFailure
        {
            get
            {
                try
                {
                    if (_handle.IsClosed || !FinalPathMatches() || !ObjectIdentity(Information()).Equals(Identity))
                        return "native-association-changed";
                    if (TrackChanges && _directoryChange is { Changed: true })
                        return "native-namespace-notification";
                    if (TrackChanges && !_changeRecord.AsSpan().SequenceEqual(ChangeRecord()))
                        return "native-change-record-changed";
                    return null;
                }

                        catch (Exception exception) when (exception is IOException or Win32Exception or CaptureFailure or ObjectDisposedException)
                {
                    return "native-currentness-unavailable";
                }
            }
        }

        internal AtlasMembershipDiagnostic? ObserveForQualification(string stage) =>
            _directoryChange is not null
                ? _directoryChange.ObserveForQualification(stage, string.Join(",", DiagnosticRoles), _path)
                : null;

        internal string Digest(long maximumBytes, CancellationToken cancellationToken)
        {
            var length = RandomAccess.GetLength(_handle);
            Require(length <= maximumBytes, AtlasMembershipCaptureState.BudgetExceeded, "index-byte-budget");
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[8192];
            long offset = 0;
            while (offset < length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = RandomAccess.Read(_handle, buffer.AsSpan(0, (int)Math.Min(buffer.Length, length - offset)), offset);
                Require(count > 0, AtlasMembershipCaptureState.Unstable, "incomplete-index-read");
                hash.AppendData(buffer, 0, count);
                offset += count;
            }

            return Convert.ToHexString(hash.GetHashAndReset());
        }

        private byte[] ChangeRecord()
        {
            var buffer = new byte[1024];
            if (!DeviceIoControl(_handle, 0x000900eb, IntPtr.Zero, 0, buffer, buffer.Length, out var length, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Require(length >= 8 && length <= buffer.Length && BinaryPrimitives.ReadUInt32LittleEndian(buffer) == length
                && BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(4)) == 2,
                AtlasMembershipCaptureState.Unavailable, "unsupported-native-change-record");
            return buffer[..length];
        }

        private InformationRecord Information()
        {
            if (!GetFileInformationByHandle(_handle, out var information))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return information;
        }

        private bool FinalPathMatches()
        {
            var path = new StringBuilder(32768);
            var length = GetFinalPathNameByHandleW(_handle, path, path.Capacity, 0);
            if (length == 0 || length >= path.Capacity)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            var final = path.ToString();
            if (final.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                final = final[4..];
            }
            return string.Equals(Path.TrimEndingDirectorySeparator(final), _path, StringComparison.OrdinalIgnoreCase);
        }

        private static AtlasObjectIdentity ObjectIdentity(InformationRecord info) => new(
            info.Volume.ToString("x", CultureInfo.InvariantCulture),
            (((ulong)info.IndexHigh << 32) | info.IndexLow).ToString("x", CultureInfo.InvariantCulture));

        public void Dispose()
        {
            try
            {
                EmitDisposalDiagnostic("explicit-dispose-before");
                _cleanupFaults?.BeforePinCleanup(_path, this);
                _directoryChange?.Dispose();
            }
            finally
            {
                EmitDisposalDiagnostic("explicit-dispose-after");
                _handle.Dispose();
            }
        }

        private void EmitDisposalDiagnostic(string stage)
        {
            if (DiagnosticForQualification is not { } sink)
                return;
            try
            {
                if (ObserveForQualification(stage) is { } diagnostic)
                    sink(diagnostic);
            }
            catch (Exception)
            {
                DiagnosticLoss = true;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFileW(string name, uint access, uint sharing, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle file, out InformationRecord information);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetFinalPathNameByHandleW(SafeFileHandle file, StringBuilder path, int length, uint flags);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle file, uint code, IntPtr input, int inputLength,
            [Out] byte[] output, int outputLength, out int returned, IntPtr overlapped);
        [StructLayout(LayoutKind.Sequential)]
        private struct InformationRecord
        {
            public uint Attributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME Created;
            public System.Runtime.InteropServices.ComTypes.FILETIME Accessed;
            public System.Runtime.InteropServices.ComTypes.FILETIME Written;
            public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
        }
    }

    /// <summary>
    /// One pending kernel notification, never rearmed. Completion (including overflow/error)
    /// invalidates monotonically. Checking its native event avoids managed callback-queue latency.
    /// </summary>
    private sealed class NativeDirectoryChange : IDisposable
    {
        private readonly SafeFileHandle _handle;
        private readonly EventWaitHandle _completed = new(false, EventResetMode.ManualReset);
        private static int _liveBuffers;
        internal static int LiveBuffers => Volatile.Read(ref _liveBuffers);
        private static long _nextWatchId;
        private readonly long _watchId = Interlocked.Increment(ref _nextWatchId);
        private readonly Thread _issuer = Thread.CurrentThread;
        private readonly uint _issuingNativeThreadId = GetCurrentThreadId();
        private readonly int _issuingManagedThreadId = Environment.CurrentManagedThreadId;
        private readonly bool _issuingThreadPool = Thread.CurrentThread.IsThreadPoolThread;
        private readonly long _directoryHandleValue;
        private readonly long _eventHandleValue;
        private IntPtr _buffer;
        private IntPtr _overlapped;
        private bool _pending;
        private bool _disposed;
        private int _disposeCalls;
        private int _explicitCancelCalls;
        private bool? _cancelSucceeded;
        private int? _cancelError;
        private AtlasMembershipDiagnostic? _lastDiagnostic;
        internal bool Changed => _disposed || _completed.WaitOne(0);

        internal NativeDirectoryChange(string path)
        {
            _handle = OpenDirectory(path, 1, 1, IntPtr.Zero, 3, 0x40000000 | 0x02000000 | 0x00200000, IntPtr.Zero);
            _directoryHandleValue = _handle.DangerousGetHandle().ToInt64();
            _eventHandleValue = _completed.SafeWaitHandle.DangerousGetHandle().ToInt64();
            try
            {
                if (_handle.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                _buffer = Marshal.AllocHGlobal(4096);
                Interlocked.Increment(ref _liveBuffers);
                _overlapped = Marshal.AllocHGlobal(Marshal.SizeOf<OverlappedRecord>());
                Marshal.StructureToPtr(new OverlappedRecord { Event = _completed.SafeWaitHandle.DangerousGetHandle() }, _overlapped, false);
                // Held data handles exclude writes; this notification guards namespace insertion/removal.
                var started = ReadDirectoryChangesW(_handle, _buffer, 4096, true,
                    0x1 | 0x2, IntPtr.Zero, _overlapped, IntPtr.Zero);
                if (!started && Marshal.GetLastWin32Error() != 997)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                _pending = true;
            }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            internal AtlasMembershipDiagnostic ObserveForQualification(string stage, string role, string path)
            {
                if (_disposed)
                {
                    return (_lastDiagnostic ?? throw new InvalidOperationException("No completion was recorded before disposal."))
                        with { Stage = stage, PinRole = role, PinPath = path, Ownership = CurrentOwnership() };
                }
                var succeeded = GetOverlappedResult(_handle, _overlapped, out var nativeBytes, false);
                var error = succeeded ? 0 : Marshal.GetLastWin32Error();
                var notifications = new List<AtlasMembershipNotification>();
                var truncated = false;
                string? decodeFailure = null;
                var rawPrefix = "";
                if (succeeded && nativeBytes is > 0 and <= 4096)
                {
                    var buffer = new byte[(int)nativeBytes];
                    Marshal.Copy(_buffer, buffer, 0, buffer.Length);
                    rawPrefix = Convert.ToHexString(buffer.AsSpan(0, Math.Min(64, buffer.Length)));
                    var offset = 0;
                    while (offset < buffer.Length && notifications.Count < 8)
                    {
                        if (buffer.Length - offset < 12)
                        {
                            decodeFailure = "short-notification-header";
                            break;
                        }
                        var next = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset));
                        var action = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 4));
                        var nameBytes = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 8));
                        if ((nameBytes & 1) != 0 || nameBytes > buffer.Length - offset - 12)
                        {
                            decodeFailure = "invalid-notification-name-length";
                            break;
                        }
                        string name;
                        try
                        {
                            name = new UnicodeEncoding(false, false, true).GetString(buffer, offset + 12, (int)nameBytes);
                        }
                        catch (DecoderFallbackException)
                        {
                            decodeFailure = "invalid-notification-unicode";
                            break;
                        }
                        var nameTruncated = name.Length > 256;
                        if (nameTruncated)
                        {
                            var end = char.IsHighSurrogate(name[255]) && char.IsLowSurrogate(name[256]) ? 255 : 256;
                            name = name[..end];
                            truncated = true;
                        }
                        notifications.Add(new AtlasMembershipNotification(action, name, nameTruncated));
                        if (next == 0)
                        {
                            offset = buffer.Length;
                            break;
                        }
                        if ((next & 3) != 0 || next < 12 + nameBytes || next > buffer.Length - offset)
                        {
                            decodeFailure = "invalid-notification-offset";
                            break;
                        }
                        offset += (int)next;
                    }
                    truncated |= offset < buffer.Length && notifications.Count == 8;
                }
                else if (succeeded)
                {
                    decodeFailure = nativeBytes == 0 ? "zero-byte-completion" : "native-byte-count-exceeds-buffer";
                }
                var diagnostic = new AtlasMembershipDiagnostic(stage, role, path, _completed.WaitOne(0),
                    succeeded, error, nativeBytes, notifications.ToArray(), truncated, decodeFailure, rawPrefix, CurrentOwnership());
                _lastDiagnostic = diagnostic;
                return diagnostic;
            }

            public void Dispose()
            {
                Interlocked.Increment(ref _disposeCalls);
                if (_disposed)
                    return;
                if (_pending)
                {
                    if (!_completed.WaitOne(0))
                    {
                        Interlocked.Increment(ref _explicitCancelCalls);
                        var canceled = CancelIoEx(_handle, _overlapped);
                        _cancelSucceeded = canceled;
                        _cancelError = canceled ? 0 : Marshal.GetLastWin32Error();
                    }
                    if (!_completed.WaitOne(TimeSpan.FromSeconds(2)))
                        throw new IOException("Native notification cancellation did not complete; its buffers remain owned.");
                    _ = GetOverlappedResult(_handle, _overlapped, out _, false);
                }
                if (_lastDiagnostic is { } previous)
                    _ = ObserveForQualification("native-disposal-completion", previous.PinRole, previous.PinPath);
                _disposed = true;
            _handle.Dispose();
            _completed.Dispose();
            if (_overlapped != IntPtr.Zero)
                Marshal.FreeHGlobal(_overlapped);
            if (_buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_buffer);
                Interlocked.Decrement(ref _liveBuffers);
            }
        }

        private AtlasMembershipWatchOwnership CurrentOwnership() => new(
            _watchId, _issuingNativeThreadId, _issuingManagedThreadId, _issuer.IsAlive, _issuer.Join(0),
            _issuingThreadPool, GetCurrentThreadId(), _directoryHandleValue, _overlapped.ToInt64(),
            _eventHandleValue, _handle.IsClosed, !_disposed && _overlapped != IntPtr.Zero, _disposed,
            Volatile.Read(ref _disposeCalls), Volatile.Read(ref _explicitCancelCalls), _cancelSucceeded, _cancelError);

        [StructLayout(LayoutKind.Sequential)]
        private struct OverlappedRecord
        {
            public UIntPtr Internal, InternalHigh;
            public uint Offset, OffsetHigh;
            public IntPtr Event;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle OpenDirectory(string name, uint access, uint sharing, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadDirectoryChangesW(SafeFileHandle directory, IntPtr buffer, uint size, bool subtree,
            uint filter, IntPtr returned, IntPtr overlapped, IntPtr completion);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelIoEx(SafeFileHandle file, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetOverlappedResult(SafeFileHandle file, IntPtr overlapped, out uint transferred, bool wait);
    }
}

internal sealed class AtlasMembershipSnapshot(
    AtlasMembershipCaptureState state, string? reason, AtlasMembershipEntry[] entries,
    AtlasGitMembership.Association? association, string? head, string? indexDigest,
    int invocations, TimeSpan elapsed, AtlasGitMembership.PinSet? pins,
    Action<AtlasMembershipDiagnostic>? diagnostics = null, string? associationIdentityStamp = null) : IAsyncDisposable
{
    internal AtlasMembershipCaptureState State { get; } = state;
    internal string? Reason { get; } = reason;
    internal IReadOnlyList<AtlasMembershipEntry> Entries { get; } = Array.AsReadOnly(entries);
    internal AtlasGitMembership.Association? Association { get; } = association;
    internal string? Head { get; } = head;
    internal string? IndexDigest { get; } = indexDigest;
    internal string? AssociationIdentityStamp { get; } = associationIdentityStamp;
    internal int Invocations { get; } = invocations;
    internal TimeSpan Elapsed { get; } = elapsed;
    internal long? MembershipTotal => State is AtlasMembershipCaptureState.CandidateComplete ? Entries.Count : null;
    internal bool IsCurrent()
    {
        pins?.ObserveForQualification("snapshot-currentness", diagnostics);
        return State is AtlasMembershipCaptureState.CandidateComplete && pins is not null && pins.IsCurrent();
    }

    internal void PrepareExclusiveSourceRead(string sourceRoot)
    {
        if (State is not AtlasMembershipCaptureState.CandidateComplete || pins is null)
            throw new AtlasGitMembership.CaptureFailure(AtlasMembershipCaptureState.Refused, "source-root-handoff-unavailable");
        pins.PrepareExclusiveSourceRead(sourceRoot);
    }

    public ValueTask DisposeAsync()
    {
        pins?.Dispose();
        return ValueTask.CompletedTask;
    }
}
