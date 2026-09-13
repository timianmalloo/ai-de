using System.Buffers.Binary;
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

    internal async ValueTask<AtlasMembershipSnapshot> CaptureForQualificationAsync(
        string sourceRoot, AtlasObjectIdentity expectedRootIdentity, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var pins = new PinSet();
        var invocations = 0;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            Require(OperatingSystem.IsWindows(), AtlasMembershipCaptureState.Refused, "windows-native-evidence-required");
            sourceRoot = OrdinaryPath(sourceRoot);
            var root = pins.Add(sourceRoot, trackChanges: true);
            Require(root.Identity.Equals(expectedRootIdentity), AtlasMembershipCaptureState.Refused, "source-root-identity-mismatch");
            pins.AddAncestors(sourceRoot);
            executable = OrdinaryPath(executable);
            var executablePin = pins.Add(executable, trackChanges: true);
            pins.AddAncestors(executable);
            Require(string.Equals(executablePin.Digest(long.MaxValue, deadline.Token), expectedSha256, StringComparison.OrdinalIgnoreCase),
                AtlasMembershipCaptureState.Refused, "approved-executable-digest-mismatch");

            var version = await Invoke(["--version"], 4096);
            Require(StrictUtf8.GetString(version).TrimEnd('\r', '\n') == expectedVersion,
                AtlasMembershipCaptureState.Refused, "approved-executable-version-mismatch");
            var discovery = await Invoke(DiscoveryArguments, 16 * 1024);
            var association = ParseDiscovery(discovery, sourceRoot);
            pins.Add(association.Repository, trackChanges: true);
            pins.Add(association.GitDirectory, trackChanges: true);
            pins.Add(association.CommonDirectory, trackChanges: true);
            pins.AddAncestors(association.GitDirectory);
            pins.AddAncestors(association.CommonDirectory);
            for (var directory = sourceRoot; ; directory = Path.GetDirectoryName(directory)!)
            {
                pins.Add(directory, trackChanges: true);
                if (string.Equals(directory, association.Repository, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            pins.AddIfPresent(Path.Combine(association.Repository, ".git"));
            pins.AddIfPresent(Path.Combine(association.GitDirectory, "commondir"));
            pins.AddIfPresent(Path.Combine(association.GitDirectory, "config.worktree"));
            pins.AddIfPresent(Path.Combine(association.CommonDirectory, "config"));
            pins.Add(Path.Combine(association.GitDirectory, "HEAD"), trackChanges: true);
            var index = pins.Add(association.Index, trackChanges: true);
            var indexDigest = index.Digest(MaxIndexBytes, deadline.Token);
            pins.AddIfPresent(Path.Combine(association.CommonDirectory, "packed-refs"));
            pins.AddTreeIfPresent(Path.Combine(association.CommonDirectory, "refs"));
            if (!string.Equals(association.GitDirectory, association.CommonDirectory, StringComparison.OrdinalIgnoreCase))
            {
                pins.AddTreeIfPresent(Path.Combine(association.GitDirectory, "refs"));
            }

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
                return await RunAsync(sourceRoot, arguments, maximumBytes, deadline.Token).ConfigureAwait(false);
            }
        }
        catch (CaptureFailure failure)
        {
            pins.Dispose();
            return Finish(failure.State, failure.Code, [], null, null, null, null);
        }
        catch (OperationCanceledException)
        {
            pins.Dispose();
            return Finish(cancellationToken.IsCancellationRequested ? AtlasMembershipCaptureState.Canceled
                : AtlasMembershipCaptureState.BudgetExceeded, "capture-canceled-or-deadline", [], null, null, null, null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or Win32Exception
            or DecoderFallbackException or ArgumentException or InvalidOperationException)
        {
            pins.Dispose();
            return Finish(AtlasMembershipCaptureState.Unavailable, "native-or-git-evidence-unavailable", [], null, null, null, null);
        }

        AtlasMembershipSnapshot Finish(AtlasMembershipCaptureState state, string? reason, AtlasMembershipEntry[] entries,
            Association? association, string? head, string? indexDigest, PinSet? retained)
        {
            timer.Stop();
            Captures.Add(1, new KeyValuePair<string, object?>("state", state.ToString()));
            Duration.Record(timer.Elapsed.TotalMilliseconds);
            return new AtlasMembershipSnapshot(state, reason, entries, association, head, indexDigest,
                invocations, timer.Elapsed, retained);
        }
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

    private static bool UnderOrSame(string root, string path) => string.Equals(root, path, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

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

    internal sealed class PinSet : IDisposable
    {
        private readonly Dictionary<string, NativePin> _pins = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        internal NativePin Add(string path, bool trackChanges)
        {
            path = OrdinaryPath(path);
            if (_pins.TryGetValue(path, out var existing))
            {
                existing.TrackChanges |= trackChanges;
                return existing;
            }

            Require(_pins.Count < MaxNativeHandles / 2, AtlasMembershipCaptureState.BudgetExceeded, "native-handle-budget");
            var pin = new NativePin(path, trackChanges);
            _pins.Add(path, pin);
            return pin;
        }

        internal void AddAncestors(string path)
        {
            for (var parent = Path.GetDirectoryName(path); parent is not null; parent = Path.GetDirectoryName(parent))
            {
                Add(parent, trackChanges: false);
            }
        }

        internal void AddIfPresent(string path)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                Add(path, trackChanges: true);
            }
        }

        internal void AddTreeIfPresent(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            Add(path, trackChanges: true);
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                var pin = Add(entry, trackChanges: true);
                if (pin.IsDirectory)
                {
                    AddTreeIfPresent(entry);
                }
            }
        }

        internal string? CurrentnessFailure => _disposed ? "snapshot-disposed"
            : _pins.Values.Select(pin => pin.CurrentnessFailure).FirstOrDefault(failure => failure is not null);
        internal bool IsCurrent() => CurrentnessFailure is null;

        public void Dispose()
        {
            _disposed = true;
            foreach (var pin in _pins.Values.Reverse())
            {
                pin.Dispose();
            }
            _pins.Clear();
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
        private NativeDirectoryChange? _directoryChange;
        private bool _trackChanges;
        internal bool TrackChanges
        {
            get => _trackChanges;
            set
            {
                if (value && IsDirectory && _directoryChange is null)
                {
                    _directoryChange = new NativeDirectoryChange(_path);
                }
                _trackChanges = value;
            }
        }
        internal AtlasObjectIdentity Identity { get; }
        internal bool IsDirectory { get; }

        internal NativePin(string path, bool trackChanges)
        {
            _path = path;
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
                _directoryChange?.Dispose();
                _handle.Dispose();
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
            _directoryChange?.Dispose();
            _handle.Dispose();
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
        private IntPtr _buffer;
        private IntPtr _overlapped;
        private bool _pending;
        private bool _disposed;
        internal bool Changed => _disposed || _completed.WaitOne(0);

        internal NativeDirectoryChange(string path)
        {
            _handle = OpenDirectory(path, 1, 1, IntPtr.Zero, 3, 0x40000000 | 0x02000000 | 0x00200000, IntPtr.Zero);
            try
            {
                if (_handle.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                _buffer = Marshal.AllocHGlobal(4096);
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

        public void Dispose()
        {
            if (_disposed)
                return;
            if (_pending)
            {
                if (!_completed.WaitOne(0))
                    _ = CancelIoEx(_handle, _overlapped);
                if (!_completed.WaitOne(TimeSpan.FromSeconds(2)))
                    throw new IOException("Native notification cancellation did not complete; its buffers remain owned.");
                _ = GetOverlappedResult(_handle, _overlapped, out _, false);
            }
            _disposed = true;
            _handle.Dispose();
            _completed.Dispose();
            if (_overlapped != IntPtr.Zero)
                Marshal.FreeHGlobal(_overlapped);
            if (_buffer != IntPtr.Zero)
                Marshal.FreeHGlobal(_buffer);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct OverlappedRecord
        {
            public UIntPtr Internal, InternalHigh;
            public uint Offset, OffsetHigh;
            public IntPtr Event;
        }

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
    int invocations, TimeSpan elapsed, AtlasGitMembership.PinSet? pins) : IAsyncDisposable
{
    internal AtlasMembershipCaptureState State { get; } = state;
    internal string? Reason { get; } = reason;
    internal IReadOnlyList<AtlasMembershipEntry> Entries { get; } = Array.AsReadOnly(entries);
    internal AtlasGitMembership.Association? Association { get; } = association;
    internal string? Head { get; } = head;
    internal string? IndexDigest { get; } = indexDigest;
    internal int Invocations { get; } = invocations;
    internal TimeSpan Elapsed { get; } = elapsed;
    internal long? MembershipTotal => State is AtlasMembershipCaptureState.CandidateComplete ? Entries.Count : null;
    internal bool IsCurrent() => State is AtlasMembershipCaptureState.CandidateComplete && pins is not null && pins.IsCurrent();

    public ValueTask DisposeAsync()
    {
        pins?.Dispose();
        return ValueTask.CompletedTask;
    }
}
