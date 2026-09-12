using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Understanding;

public sealed class AtlasDirectoryEnumerator(Action? afterEntryObserved = null) : IAtlasDirectoryEnumerator
{
    private static readonly ActivitySource ActivitySource = new("AiDe.Core.Understanding.AtlasDirectoryEnumerator");

    private const uint GenericRead = 0x80000000;
    private const uint FileShareRead = 0x00000001;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private long _sequence;
    private int _held;
    private bool _budgetExceeded;

    public async Task<DirectoryObservation> EnumerateAsync(
        AtlasRootGrant rootGrant,
        EnumerationLimits limits,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootGrant);
        ArgumentNullException.ThrowIfNull(limits);
        await Task.Yield();
        using var activity = ActivitySource.StartActivity("atlas.inventory.enumerate");
        activity?.SetTag("atlas.inventory.limit.entries", limits.MaxEntries);
        activity?.SetTag("atlas.inventory.limit.depth", limits.MaxDepth);
        activity?.SetTag("atlas.inventory.limit.descriptors", limits.MaxDescriptors);

        _held = 0;
        _budgetExceeded = false;
        var started = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(limits.Timeout);

        if (RefusedInput(rootGrant.ApprovedAbsoluteRoot))
        {
            var refused = Observation(rootGrant, null, AtlasCompletionState.Refused, [], limits, 0, "root outside ordinary-local policy");
            activity?.SetTag("atlas.inventory.completion", refused.Completion.ToString());
            return refused;
        }

        try
        {
            var rootPath = Path.GetFullPath(rootGrant.ApprovedAbsoluteRoot);
            using var inspected = OpenDirectory(rootPath, followReparse: false);
            var inspectedRoot = Identity(inspected);
            if (IsReparse(inspected))
            {
                var refusedRoot = Observation(rootGrant, inspectedRoot, AtlasCompletionState.Refused, [], limits, started.ElapsedMilliseconds, "root is a reparse point");
                activity?.SetTag("atlas.inventory.completion", refusedRoot.Completion.ToString());
                return refusedRoot;
            }

            using var heldRoot = OpenHeld(rootPath, followReparse: true, limits);
            var observedRoot = Identity(heldRoot.Handle);
            if (!observedRoot.Equals(rootGrant.ExpectedNativeRootIdentity))
            {
                var mismatch = Observation(rootGrant, observedRoot, AtlasCompletionState.Refused, [], limits, started.ElapsedMilliseconds, "root identity mismatch");
                activity?.SetTag("atlas.inventory.completion", mismatch.Completion.ToString());
                return mismatch;
            }

            var entries = new List<AtlasDirectoryEntry>();
            Walk(rootPath, rootPath, 0, limits, entries, timeout.Token);
            var completion = _budgetExceeded ? AtlasCompletionState.BudgetExceeded : AtlasCompletionState.Complete;
            var complete = Observation(rootGrant, observedRoot, completion, entries, limits, started.ElapsedMilliseconds,
                _budgetExceeded ? "limit reached" : null);
            activity?.SetTag("atlas.inventory.completion", complete.Completion.ToString());
            activity?.SetTag("atlas.inventory.entries", complete.Entries.Length);
            activity?.SetTag("atlas.inventory.duration_ms", started.ElapsedMilliseconds);
            return complete;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            var canceled = Observation(rootGrant, null, AtlasCompletionState.Canceled, [], limits, started.ElapsedMilliseconds, "canceled");
            activity?.SetTag("atlas.inventory.completion", canceled.Completion.ToString());
            return canceled;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            var failed = Observation(rootGrant, null, AtlasCompletionState.Partial, [], limits, started.ElapsedMilliseconds, PublicError(ex));
            activity?.SetTag("atlas.inventory.completion", failed.Completion.ToString());
            return failed;
        }
    }

    internal AtlasObjectIdentity ObserveExpectedNativeRootIdentityForTest(string root)
    {
        using var inspected = OpenDirectory(Path.GetFullPath(root), followReparse: false);
        if (IsReparse(inspected)) throw new IOException("root is a reparse point");
        using var held = OpenDirectory(Path.GetFullPath(root), followReparse: true);
        return Identity(held);
    }

    private void Walk(string root, string directory, int depth, EnumerationLimits limits, List<AtlasDirectoryEntry> entries, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (depth >= limits.MaxDepth || entries.Count >= limits.MaxEntries)
        {
            _budgetExceeded = true;
            return;
        }

        using var held = OpenHeld(directory, followReparse: true, limits);
        foreach (var path in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entries.Count >= limits.MaxEntries)
            {
                _budgetExceeded = true;
                return;
            }

            using var inspected = OpenDirectory(path, followReparse: false);
            var attributes = File.GetAttributes(path);
            var isDirectory = (attributes & FileAttributes.Directory) != 0;
            if (IsReparse(inspected))
            {
                entries.Add(new AtlasDirectoryEntry(Relative(root, path), AtlasDirectoryEntryKind.RejectedLink, null, null, null, "reparse point unsupported"));
                afterEntryObserved?.Invoke();
                continue;
            }

            var info = Info(inspected);
            var identity = Identity(info);
            entries.Add(new AtlasDirectoryEntry(Relative(root, path), isDirectory ? AtlasDirectoryEntryKind.Directory : AtlasDirectoryEntryKind.File,
                identity, isDirectory ? null : Length(path), info.NumberOfLinks, null));
            afterEntryObserved?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            if (isDirectory)
            {
                if (_held + 1 > limits.MaxDescriptors)
                {
                    _budgetExceeded = true;
                    return;
                }
                Walk(root, path, depth + 1, limits, entries, cancellationToken);
                if (_budgetExceeded) return;
            }
        }
    }

    private CountedHandle OpenHeld(string path, bool followReparse, EnumerationLimits limits)
    {
        if (_held + 1 > limits.MaxDescriptors)
        {
            _budgetExceeded = true;
            throw new IOException("descriptor limit reached");
        }
        var handle = OpenDirectory(path, followReparse);
        _held++;
        return new CountedHandle(handle, this);
    }

    private DirectoryObservation Observation(AtlasRootGrant grant, AtlasObjectIdentity? observed, AtlasCompletionState completion, IReadOnlyList<AtlasDirectoryEntry> entries, EnumerationLimits limits, long elapsedMs, string? reason)
    {
        var bounds = new AtlasBounds(limits.MaxEntries, limits.MaxEntries, entries.Count, elapsedMs, completion is AtlasCompletionState.Complete ? entries.Count : null,
            completion is AtlasCompletionState.Complete ? AtlasDenominatorState.Known : AtlasDenominatorState.Unknown,
            completion is AtlasCompletionState.Complete ? null : reason ?? completion.ToString(), completion is AtlasCompletionState.Complete ? null : "entries");
        return new DirectoryObservation("directory-observation:" + Interlocked.Increment(ref _sequence), grant.ExpectedNativeRootIdentity, observed, completion, _sequence, bounds, entries);
    }

    private static bool RefusedInput(string root) => root.Contains(':') && !Path.IsPathRooted(root) || root.StartsWith("\\\\", StringComparison.Ordinal) || root.StartsWith("\\\\.\\", StringComparison.Ordinal);
    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
    private static long Length(string path) => new FileInfo(path).Length;
    private static string PublicError(Exception ex) => ex is Win32Exception win ? "native:" + win.NativeErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture) : ex.GetType().Name;
    private static bool IsReparse(SafeFileHandle handle) => (Info(handle).FileAttributes & (uint)FileAttributes.ReparsePoint) != 0;
    private static ByHandleFileInformation Info(SafeFileHandle handle) { if (!GetFileInformationByHandle(handle, out var i)) throw new Win32Exception(Marshal.GetLastWin32Error()); return i; }
    private static AtlasObjectIdentity Identity(SafeFileHandle handle) => Identity(Info(handle));
    private static AtlasObjectIdentity Identity(ByHandleFileInformation i) => new(i.VolumeSerialNumber.ToString("x"), (((ulong)i.FileIndexHigh << 32) | i.FileIndexLow).ToString("x"));
    private static SafeFileHandle OpenDirectory(string path, bool followReparse)
    {
        var flags = FileFlagBackupSemantics | (followReparse ? 0 : FileFlagOpenReparsePoint);
        var handle = CreateFileW(path, GenericRead, FileShareRead, IntPtr.Zero, OpenExisting, flags, IntPtr.Zero);
        if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());
        return handle;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation information);
    [StructLayout(LayoutKind.Sequential)] private struct ByHandleFileInformation { public uint FileAttributes; public FileTime CreationTime; public FileTime LastAccessTime; public FileTime LastWriteTime; public uint VolumeSerialNumber; public uint FileSizeHigh; public uint FileSizeLow; public uint NumberOfLinks; public uint FileIndexHigh; public uint FileIndexLow; }
    [StructLayout(LayoutKind.Sequential)] private struct FileTime { public uint LowDateTime; public uint HighDateTime; }
    private sealed class CountedHandle(SafeFileHandle handle, AtlasDirectoryEnumerator owner) : IDisposable { public SafeFileHandle Handle => handle; public void Dispose() { handle.Dispose(); owner._held--; } }
}



