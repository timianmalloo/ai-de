using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Understanding;

/// <summary>Core-only read context. The predicate checks current grant context, manifest membership and policy visibility.</summary>
internal sealed record AtlasSourceReadRequest(
    AtlasRootGrant RootGrant,
    AtlasFileEntry File,
    string ManifestToken,
    string ObservationKey,
    Func<AtlasRootGrant, string, AtlasFileEntry, bool> IsCurrentAndVisible);

/// <summary>Request-local owner; no handles survive return and no source bytes belong in query payloads.</summary>
internal sealed class AtlasSourceReadResult : IDisposable
{
    private byte[]? snapshot;
    private VerifiedSourceBuffer? buffer;
    private bool disposed;

    private AtlasSourceReadResult(SourceProjectionState state, string observationKey, byte[]? snapshot, VerifiedSourceBuffer? buffer)
    {
        State = state;
        ObservationKey = observationKey;
        this.snapshot = snapshot;
        this.buffer = buffer;
    }

    internal SourceProjectionState State { get; }
    internal string ObservationKey { get; }
    internal TimeSpan Duration { get; set; }
    internal AtlasSourceObservation? Observation => Buffer?.SourceObservation;
    internal AtlasSourceBinding? Binding => Buffer?.Binding;
    internal VerifiedSourceBuffer? Buffer
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return buffer;
        }
    }

    internal ReadOnlyMemory<byte> RawSnapshot
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return snapshot ?? throw new InvalidOperationException("No verified source snapshot.");
        }
    }

    internal static AtlasSourceReadResult Failure(SourceProjectionState state, string observationKey) =>
        state is SourceProjectionState.IndexedMatch
            ? throw new ArgumentException("Success requires a verified buffer.", nameof(state))
            : new(state, observationKey, null, null);

    internal static AtlasSourceReadResult Success(byte[] snapshot, VerifiedSourceBuffer buffer) =>
        new(SourceProjectionState.IndexedMatch, buffer.SourceObservation.ObservationKey, snapshot, buffer);

    public void Dispose()
    {
        buffer?.Dispose();
        buffer = null;
        if (snapshot is not null)
        {
            CryptographicOperations.ZeroMemory(snapshot);
            snapshot = null;
        }
        disposed = true;
    }
}

/// <summary>
/// Bounded Windows reader for ordinary local files. Held-object identities and final paths are checked;
/// this is not a claim that path-based opens can never traverse a concurrently substituted link.
/// Full source stays Core-only; display pages are never compiler inputs.
/// </summary>
internal sealed class AtlasSource
{
    internal const int MaxDisplayBytes = 128 * 1024;
    internal const string ActivityName = "AiDe.Core.Understanding.AtlasSource";
    private static readonly ActivitySource Activities = new(ActivityName);
    private readonly TimeProvider clock;
    private readonly Func<SafeFileHandle, Memory<byte>, long, CancellationToken, ValueTask<int>> readChunk;

    internal AtlasSource(TimeProvider? clock = null)
        : this(clock ?? TimeProvider.System, RandomAccess.ReadAsync) { }

    // The injected operation replaces only chunk I/O; every authority, identity, size and hash guard still runs.
    internal AtlasSource(TimeProvider clock, Func<SafeFileHandle, Memory<byte>, long, CancellationToken, ValueTask<int>> readChunk)
    {
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.readChunk = readChunk ?? throw new ArgumentNullException(nameof(readChunk));
    }

    /// <summary>
    /// Observes an approved ordinary local directory without issuing authority or reading source bytes.
    /// Approval includes the composition's recorded consent and expiry policy. Complete alone carries
    /// identity; Partial uses atlas.root.unverifiable or atlas.root.unavailable, while refusal and
    /// cancellation use atlas.root.refused and atlas.root.canceled. Reasons never contain private paths.
    /// Detected root/ancestor reparses are rejected; path-based checks are not a race-free traversal claim.
    /// </summary>
    internal (AtlasCompletionState Completion, AtlasObjectIdentity? Identity, string? Reason)
        ObserveApprovedRootIdentity(string approvedAbsoluteRoot, Func<bool> isApprovalCurrent, CancellationToken cancellationToken)
    {
        using var activity = Activities.StartActivity("atlas.source.root-observe");
        (AtlasCompletionState Completion, AtlasObjectIdentity? Identity, string? Reason) result;
        try
        {
            result = (AtlasCompletionState.Complete, ObserveRoot(approvedAbsoluteRoot, isApprovalCurrent, cancellationToken), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = (AtlasCompletionState.Canceled, null, "atlas.root.canceled");
        }
        catch (SourceFailure failure)
        {
            result = failure.State is SourceProjectionState.Refused
                ? (AtlasCompletionState.Refused, null, "atlas.root.refused")
                : (AtlasCompletionState.Partial, null, "atlas.root.unverifiable");
        }
        catch (UnauthorizedAccessException)
        {
            result = (AtlasCompletionState.Refused, null, "atlas.root.refused");
        }
        catch (Win32Exception exception)
        {
            result = exception.NativeErrorCode is 5 or 32 or 33
                ? (AtlasCompletionState.Refused, null, "atlas.root.refused")
                : (AtlasCompletionState.Partial, null, "atlas.root.unavailable");
        }
        catch (IOException exception)
        {
            result = (exception.HResult & 0xFFFF) is 5 or 32 or 33
                ? (AtlasCompletionState.Refused, null, "atlas.root.refused")
                : (AtlasCompletionState.Partial, null, "atlas.root.unavailable");
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            result = (AtlasCompletionState.Refused, null, "atlas.root.refused");
        }
        activity?.SetTag("atlas.root.completion", result.Completion.ToString());
        activity?.SetTag("atlas.root.reason", result.Reason);
        return result;
    }

    private static AtlasObjectIdentity ObserveRoot(string approvedAbsoluteRoot, Func<bool> isApprovalCurrent, CancellationToken cancellationToken)
    {
        CheckRootApproval(isApprovalCurrent, cancellationToken);
        ValidateRootPath(approvedAbsoluteRoot);
        Require(OperatingSystem.IsWindows(), SourceProjectionState.Unverifiable);
        var rootPath = Path.GetFullPath(approvedAbsoluteRoot);
        Require(new DriveInfo(Path.GetPathRoot(rootPath)!).DriveType is not DriveType.Network, SourceProjectionState.Refused);
        RejectRootReparseChain(rootPath);
        CheckRootApproval(isApprovalCurrent, cancellationToken);
        using var held = new HeldDirectories();
        var root = held.Open(rootPath);
        CheckRootApproval(isApprovalCurrent, cancellationToken);
        Require(held.IsStable(), SourceProjectionState.ReadUnstable);
        RejectRootReparseChain(rootPath);
        CheckRootApproval(isApprovalCurrent, cancellationToken);
        Require(held.IsStable(), SourceProjectionState.ReadUnstable);
        cancellationToken.ThrowIfCancellationRequested();
        return root.Before.Identity;
    }

    private static void CheckRootApproval(Func<bool> isApprovalCurrent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(isApprovalCurrent);
        Require(isApprovalCurrent(), SourceProjectionState.Refused);
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal Task<AtlasSourceReadResult> ObserveAsync(AtlasSourceReadRequest request, CancellationToken cancellationToken) =>
        RunAsync(request, null, cancellationToken);

    internal Task<AtlasSourceReadResult> ReadVerifiedAsync(AtlasSourceReadRequest request, AtlasSourceObservation indexedObservation, AtlasSourceBinding expectedBinding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(indexedObservation);
        ArgumentNullException.ThrowIfNull(expectedBinding);
        return RunAsync(request, (indexedObservation, expectedBinding), cancellationToken);
    }

    private async Task<AtlasSourceReadResult> RunAsync(AtlasSourceReadRequest request,
        (AtlasSourceObservation Observation, AtlasSourceBinding Binding)? indexed, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RootGrant);
        ArgumentNullException.ThrowIfNull(request.File);
        ArgumentNullException.ThrowIfNull(request.IsCurrentAndVisible);
        AtlasIdentityCodec.RequiredToken(request.ObservationKey, nameof(request));
        var started = Stopwatch.GetTimestamp();
        using var activity = Activities.StartActivity(indexed is null ? "atlas.source.observe" : "atlas.source.verify");
        AtlasSourceReadResult result;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateRequest(request);
            if (indexed is { } selection)
            {
                ValidateSelection(request, selection.Observation, selection.Binding);
            }
            result = await ReadSnapshotAsync(request, indexed, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            result = AtlasSourceReadResult.Failure(SourceProjectionState.Canceled, request.ObservationKey);
        }
        catch (SourceFailure failure)
        {
            result = AtlasSourceReadResult.Failure(failure.State, request.ObservationKey);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or Win32Exception)
        {
            result = AtlasSourceReadResult.Failure(SourceProjectionState.Unavailable, request.ObservationKey);
        }
        result.Duration = Stopwatch.GetElapsedTime(started);
        activity?.SetTag("atlas.source.state", result.State.ToString());
        activity?.SetTag("atlas.source.bytes", result.Observation?.ByteLength);
        return result;
    }

    private void ValidateRequest(AtlasSourceReadRequest request)
    {
        Require(request.RootGrant.ExpiresAt > clock.GetUtcNow()
            && request.IsCurrentAndVisible(request.RootGrant, request.ManifestToken, request.File), SourceProjectionState.Refused);
        Require(!string.IsNullOrWhiteSpace(request.ManifestToken), SourceProjectionState.Refused);
        Require(request.File.Kind is AtlasDirectoryEntryKind.File
            && request.File.Classification is AtlasFileClassification.CSharp or AtlasFileClassification.Text,
            SourceProjectionState.Refused);
        Require(request.File.Availability is not AtlasFileAvailability.Refused, SourceProjectionState.Refused);
        Require(request.File.Availability is not AtlasFileAvailability.Unavailable, SourceProjectionState.Unavailable);
        Require(request.File.Availability is AtlasFileAvailability.Available, SourceProjectionState.Unverifiable);
        ValidatePaths(request.RootGrant.ApprovedAbsoluteRoot, request.File.RelativePath);
        Require(Same(request.File.FileValue, AtlasIdentityCodec.ForFile(request.RootGrant.WorkspaceToken,
            request.RootGrant.RootToken, request.File.RelativePath)), SourceProjectionState.Refused);
        Require(request.File.ObservedIdentity is not null, SourceProjectionState.Unverifiable);
        Require(OperatingSystem.IsWindows(), SourceProjectionState.Unverifiable);
    }

    private static void ValidateSelection(AtlasSourceReadRequest request, AtlasSourceObservation observation, AtlasSourceBinding binding)
    {
        Require(observation.Status is AtlasSourceObservationStatus.Verified, SourceProjectionState.Unverifiable);
        Require(Same(observation.ManifestToken, request.ManifestToken)
            && Same(observation.FileValue, request.File.FileValue)
            && Same(observation.PolicyToken, request.RootGrant.PolicyToken)
            && Equals(observation.RootIdentity, request.RootGrant.ExpectedNativeRootIdentity)
            && Equals(observation.FileIdentity, request.File.ObservedIdentity)
            && binding.Matches(observation.ManifestToken, observation.FileValue, observation.PolicyToken,
                AtlasIdentityCodec.ForNativeObject(observation.RootIdentity!),
                AtlasIdentityCodec.ForNativeObject(observation.FileIdentity!), observation.CanonicalSha256!),
            SourceProjectionState.Changed);
    }

    private async Task<AtlasSourceReadResult> ReadSnapshotAsync(AtlasSourceReadRequest request,
        (AtlasSourceObservation Observation, AtlasSourceBinding Binding)? indexed, CancellationToken cancellationToken)
    {
        var rootPath = Path.GetFullPath(request.RootGrant.ApprovedAbsoluteRoot);
        Require(new DriveInfo(Path.GetPathRoot(rootPath)!).DriveType is not DriveType.Network, SourceProjectionState.Refused);
        RejectRootReparseChain(rootPath);
        using var directories = new HeldDirectories();
        var root = directories.Open(rootPath);
        Require(Equals(root.Before.Identity, request.RootGrant.ExpectedNativeRootIdentity), SourceProjectionState.Changed);
        var parts = request.File.RelativePath.Replace('\\', '/').Split('/');
        var current = rootPath;
        foreach (var part in parts[..^1])
        {
            cancellationToken.ThrowIfCancellationRequested();
            current = Path.Combine(current, part);
            RejectReparse(current);
            var ancestor = directories.Open(current);
            Require(IsUnder(root.FinalPath, ancestor.FinalPath), SourceProjectionState.Refused);
        }
        var path = Path.Combine(current, parts[^1]);
        RejectReparse(path);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRequest(request);
        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var before = Information(handle);
        Require((before.Attributes & (uint)(FileAttributes.Directory | FileAttributes.ReparsePoint)) is 0,
            SourceProjectionState.Unverifiable);
        var finalPath = FinalPath(handle);
        Require(IsUnder(root.FinalPath, finalPath), SourceProjectionState.Refused);
        Require(Equals(before.Identity, request.File.ObservedIdentity), SourceProjectionState.Changed);
        Require(before.Links is 1 && before.Length >= 0, SourceProjectionState.Unverifiable);
        Require(before.Length <= VerifiedSourceBuffer.MaxSourceBytes, SourceProjectionState.TooLargeToVerify);
        cancellationToken.ThrowIfCancellationRequested();
        byte[]? bytes = new byte[checked((int)before.Length)];
        VerifiedSourceBuffer? buffer = null;
        try
        {
            await ReadAllAsync(handle, bytes, cancellationToken).ConfigureAwait(false);
            Require(before == Information(handle) && SamePath(finalPath, FinalPath(handle))
                && directories.IsStable(), SourceProjectionState.ReadUnstable);
            ValidateRequest(request);
            var hash = "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            Require(indexed is null || Same(hash, indexed.Value.Binding.ContentHash), SourceProjectionState.Changed);
            var decoder = DecoderId(bytes);
            int length;
            try
            {
                length = VerifiedSourceBuffer.GetDecodedUtf16Length(bytes, decoder);
            }
            catch (Exception exception) when (exception is DecoderFallbackException or InvalidOperationException or ArgumentException)
            {
                throw new SourceFailure(SourceProjectionState.UnsupportedEncoding);
            }
            var observation = indexed?.Observation ?? AtlasSourceObservation.Verified(request.ObservationKey,
                request.ManifestToken, request.File.FileValue, request.RootGrant.PolicyToken,
                root.Before.Identity, before.Identity, hash, bytes.LongLength, decoder, length,
                new AtlasBounds(1, 1, 1, bytes.LongLength, 1, AtlasDenominatorState.Known, null, null));
            Require(Same(observation.DecoderId!, decoder) && observation.ByteLength == bytes.LongLength
                && observation.DecodedUtf16Length == length, SourceProjectionState.Changed);
            var binding = indexed?.Binding ?? AtlasSourceBinding.Create(request.ManifestToken, request.File.FileValue,
                request.RootGrant.PolicyToken, AtlasIdentityCodec.ForNativeObject(root.Before.Identity),
                AtlasIdentityCodec.ForNativeObject(before.Identity), hash);
            buffer = VerifiedSourceBuffer.FromBytes(observation, binding, bytes);
            cancellationToken.ThrowIfCancellationRequested();
            ValidateRequest(request);
            var result = AtlasSourceReadResult.Success(bytes, buffer);
            bytes = null;
            buffer = null;
            return result;
        }
        finally
        {
            buffer?.Dispose();
            if (bytes is not null)
            {
                CryptographicOperations.ZeroMemory(bytes);
            }
        }
    }

    private async Task ReadAllAsync(SafeFileHandle handle, byte[] bytes, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < bytes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var memory = bytes.AsMemory(offset, Math.Min(64 * 1024, bytes.Length - offset));
            var count = await readChunk(handle, memory, offset, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            Require(count > 0 && count <= memory.Length, SourceProjectionState.ReadUnstable);
            offset += count;
        }
        cancellationToken.ThrowIfCancellationRequested();
        var extra = await readChunk(handle, new byte[1], offset, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        Require(extra is 0, SourceProjectionState.ReadUnstable);
    }

    internal SourceProjection ReadPage(AtlasSourceReadResult result, AtlasTextSpan requestedPage, IEnumerable<AtlasTextSpan> highlights)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(highlights);
        var buffer = result.Buffer;
        if (buffer is null)
        {
            return FailureProjection(result);
        }
        var text = buffer.FullText;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(requestedPage.Start, text.Length, nameof(requestedPage));
        var start = ScalarStart(text, requestedPage.Start);
        var limit = ScalarEnd(text, Math.Min(requestedPage.End, text.Length));
        var end = start;
        var byteCount = 0;
        while (end < limit)
        {
            var rune = Rune.GetRuneAt(text, end);
            if (byteCount + rune.Utf8SequenceLength > MaxDisplayBytes)
            {
                break;
            }
            byteCount += rune.Utf8SequenceLength;
            end += rune.Utf16SequenceLength;
        }
        var clipped = new List<AtlasTextSpan>();
        foreach (var highlight in highlights)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(highlight.End, text.Length, nameof(highlights));
            var left = ScalarStart(text, Math.Max(start, highlight.Start));
            var right = ScalarEnd(text, Math.Min(end, highlight.End));
            if (left < right)
            {
                clipped.Add(new(left, right - left));
            }
        }
        return SourceProjection.IndexedMatch(buffer.SourceObservation, buffer.Binding, buffer.SourceObservation.DecoderId!,
            new SourceTextPage(text[start..end], new(start, end - start), clipped));
    }

    private static SourceProjection FailureProjection(AtlasSourceReadResult result) => result.State switch
    {
        SourceProjectionState.Changed => SourceProjection.Changed(result.ObservationKey),
        SourceProjectionState.Unavailable => SourceProjection.Unavailable(result.ObservationKey),
        SourceProjectionState.Unverifiable => SourceProjection.Unverifiable(result.ObservationKey),
        SourceProjectionState.UnsupportedEncoding => SourceProjection.UnsupportedEncoding(result.ObservationKey),
        SourceProjectionState.TooLargeToVerify => SourceProjection.TooLargeToVerify(result.ObservationKey),
        SourceProjectionState.ReadUnstable => SourceProjection.ReadUnstable(result.ObservationKey),
        SourceProjectionState.Refused => SourceProjection.Refused(result.ObservationKey),
        SourceProjectionState.Canceled => SourceProjection.Canceled(result.ObservationKey),
        _ => throw new InvalidOperationException("IndexedMatch requires a live verified buffer."),
    };

    private static int ScalarStart(string text, int index) =>
        index < text.Length && char.IsLowSurrogate(text[index]) ? index + 1 : index;
    private static int ScalarEnd(string text, int index) =>
        index > 0 && char.IsHighSurrogate(text[index - 1]) ? index - 1 : index;

    private static string DecoderId(byte[] bytes) => bytes switch
    {
        [0xFF, 0xFE, 0x00, 0x00, ..] or [0x00, 0x00, 0xFE, 0xFF, ..] => throw new SourceFailure(SourceProjectionState.UnsupportedEncoding),
        [0xEF, 0xBB, 0xBF, ..] => "utf-8-bom",
        [0xFF, 0xFE, ..] => "utf-16le-bom",
        [0xFE, 0xFF, ..] => "utf-16be-bom",
        _ => "utf-8",
    };

    private static void ValidatePaths(string root, string relative)
    {
        ValidateRootPath(root);
        Require(!string.IsNullOrWhiteSpace(relative) && !Path.IsPathRooted(relative)
            && relative.Replace('\\', '/').Split('/').All(SafePart), SourceProjectionState.Refused);
    }

    private static void ValidateRootPath(string root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Require(root.Length >= 3 && char.IsAsciiLetter(root[0]) && root[1] is ':' && root[2] is '\\' or '/'
            && !root[2..].Contains(':') && Path.IsPathFullyQualified(root), SourceProjectionState.Refused);
        var rootParts = root[3..].Replace('\\', '/').TrimEnd('/').Split('/');
        Require(rootParts.All(part => part.Length is 0 || SafePart(part)), SourceProjectionState.Refused);
    }

    private static bool SafePart(string part)
    {
        if (string.IsNullOrWhiteSpace(part) || part is "." or ".." || part.EndsWith('.') || part.EndsWith(' ')
            || part.IndexOfAny([':', '*', '?', '"', '<', '>', '|', '\0']) >= 0 || part.Any(char.IsControl))
        {
            return false;
        }
        var stem = part.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
        return stem is not ("CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$")
            && !(stem.Length is 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal))
                && stem[3] is >= '1' and <= '9' or '\u00b9' or '\u00b2' or '\u00b3');
    }

    private static void RejectRootReparseChain(string root)
    {
        for (DirectoryInfo? directory = new(root); directory is not null; directory = directory.Parent)
        {
            RejectReparse(directory.FullName);
        }
    }

    private static void RejectReparse(string path) =>
        Require((File.GetAttributes(path) & FileAttributes.ReparsePoint) is 0, SourceProjectionState.Unverifiable);
    private static bool Same(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
    private static bool SamePath(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static bool IsUnder(string root, string file) => file.StartsWith(root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);
    private static void Require(bool condition, SourceProjectionState failure)
    {
        if (!condition)
        {
            throw new SourceFailure(failure);
        }
    }

    private sealed class SourceFailure(SourceProjectionState state) : Exception
    {
        internal SourceProjectionState State { get; } = state;
    }

    private sealed record OpenedInformation(AtlasObjectIdentity Identity, long Length, uint Attributes, uint Links, long Created, long Written);
    private sealed record HeldDirectory(SafeFileHandle Handle, OpenedInformation Before, string FinalPath);

    private sealed class HeldDirectories : IDisposable
    {
        private readonly List<HeldDirectory> directories = [];

        internal HeldDirectory Open(string path)
        {
            // GenericRead, not metadata-only access: the controlled mutation probe established this distinction.
            var handle = CreateFileW(path, 0x80000000, 0, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
            try
            {
                if (handle.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                var before = Information(handle);
                Require((before.Attributes & (uint)FileAttributes.Directory) is not 0
                    && (before.Attributes & (uint)FileAttributes.ReparsePoint) is 0, SourceProjectionState.Unverifiable);
                var held = new HeldDirectory(handle, before, AtlasSource.FinalPath(handle));
                directories.Add(held);
                return held;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        internal bool IsStable() => directories.All(directory =>
            directory.Before == Information(directory.Handle) && SamePath(directory.FinalPath, AtlasSource.FinalPath(directory.Handle)));

        public void Dispose()
        {
            foreach (var directory in directories)
            {
                directory.Handle.Dispose();
            }
        }
    }

    private static OpenedInformation Information(SafeFileHandle handle)
    {
        if (!GetFileInformationByHandle(handle, out var info))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        var index = ((ulong)info.IndexHigh << 32) | info.IndexLow;
        var identity = new AtlasObjectIdentity(info.Volume.ToString("x", CultureInfo.InvariantCulture), index.ToString("x", CultureInfo.InvariantCulture));
        return new(identity, ((long)info.SizeHigh << 32) | info.SizeLow, info.Attributes, info.Links,
            ((long)info.CreatedHigh << 32) | info.CreatedLow, ((long)info.WriteHigh << 32) | info.WriteLow);
    }

    private static string FinalPath(SafeFileHandle handle)
    {
        var buffer = new StringBuilder(512);
        var length = GetFinalPathNameByHandleW(handle, buffer, buffer.Capacity, 2);
        if (length >= buffer.Capacity)
        {
            buffer.EnsureCapacity(checked((int)length + 1));
            length = GetFinalPathNameByHandleW(handle, buffer, buffer.Capacity, 2);
        }
        if (length is 0 || length >= buffer.Capacity)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        return buffer.ToString();
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out NativeFileInformation information);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder path, int capacity, uint flags);
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeFileInformation
    {
        public uint Attributes, CreatedLow, CreatedHigh, AccessLow, AccessHigh, WriteLow, WriteHigh;
        public uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }
}
