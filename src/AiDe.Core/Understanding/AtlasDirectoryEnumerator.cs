using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Understanding;

/// <summary>
/// Ordinary local Windows metadata only. Detected reparse points are excluded; this is not
/// a never-open-outside race guarantee. The trusted composition must supply a live grant check.
/// I/O is synchronous: the Task-shaped port does not schedule a worker or make native opens cancelable.
/// </summary>
public sealed class AtlasDirectoryEnumerator(
    Action? afterEntryObserved = null,
    TimeProvider? timeProvider = null,
    Func<AtlasRootGrant, bool>? isGrantCurrent = null) : IAtlasDirectoryEnumerator
{
    private static readonly ActivitySource ActivitySource = new("aide.Core.Understanding.AtlasDirectoryEnumerator");
    private static readonly string ObservationEpoch = Guid.NewGuid().ToString("N");
    private static long _sequence;
    // Sharing exclusion requires data-read access; metadata-only ACLs may refuse this open.
    private const uint GenericRead = 0x80000000;
    private const uint FileShareRead = 1;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;

    public Task<DirectoryObservation> EnumerateAsync(
        AtlasRootGrant rootGrant, EnumerationLimits limits, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootGrant);
        ArgumentNullException.ThrowIfNull(limits);
        return Task.FromResult(Enumerate(rootGrant, limits, cancellationToken));
    }

    private DirectoryObservation Enumerate(AtlasRootGrant grant, EnumerationLimits limits, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("atlas.inventory.enumerate");
        var operation = new Operation(grant, limits, timeProvider ?? TimeProvider.System, isGrantCurrent, cancellationToken);
        var ancestors = new List<InspectedHandle>();
        AtlasObjectIdentity? observedRoot = null;
        var completion = AtlasCompletionState.Complete;
        string? reason = null;
        string? dimension = null;
        try
        {
            operation.Check();
            if (RefusedInput(grant.ApprovedAbsoluteRoot))
                throw new RefusedException("ordinary-local-root-required");

            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(grant.ApprovedAbsoluteRoot));
            var current = Path.GetPathRoot(root)!;
            string? parentFinal = null;
            foreach (var component in new[] { current }.Concat(root[current.Length..].Split('\\', StringSplitOptions.RemoveEmptyEntries)))
            {
                current = parentFinal is null ? component : Path.Combine(current, component);
                var inspected = operation.Open(current);
                ancestors.Add(inspected);
                if (IsReparse(inspected.Information) || !IsDirectory(inspected.Information))
                    throw new RefusedException("root-or-ancestor-reparse");
                if (parentFinal is not null && !UnderOrSame(parentFinal, inspected.FinalPath))
                    throw new RefusedException("ancestor-final-path-outside");
                parentFinal = inspected.FinalPath;
            }
            var rootHandle = ancestors[^1];
            observedRoot = Identity(rootHandle.Information);
            if (!observedRoot.Equals(grant.ExpectedNativeRootIdentity))
                throw new RefusedException("root identity mismatch");

            Walk(root, root, rootHandle.FinalPath, 0, operation);
            operation.Check();
            if (operation.ExcludedLink)
            {
                completion = AtlasCompletionState.Partial;
                reason = "reparse-excluded";
                dimension = "policy";
            }
        }
        catch (RefusedException ex)
        {
            completion = AtlasCompletionState.Refused;
            reason = ex.Message;
            dimension = "policy";
            observedRoot = null;
            operation.Entries.Clear();
        }
        catch (BudgetException ex)
        {
            completion = AtlasCompletionState.BudgetExceeded;
            reason = "limit-reached";
            dimension = ex.Message;
        }
        catch (OperationCanceledException)
        {
            completion = AtlasCompletionState.Canceled;
            reason = "canceled";
            dimension = "time-or-cancellation";
            operation.Entries.Clear();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            completion = AtlasCompletionState.Partial;
            reason = ex is Win32Exception win ? "native:" + win.NativeErrorCode.ToString(CultureInfo.InvariantCulture) : ex.GetType().Name;
            dimension = "io";
            operation.Entries.Clear();
        }
        finally
        {
            for (var i = ancestors.Count - 1; i >= 0; i--) ancestors[i].Dispose();
        }

        var sequence = Interlocked.Increment(ref _sequence);
        var complete = completion is AtlasCompletionState.Complete;
        // Data ruling: these rows contain metadata, not returned source/content payload.
        var bounds = new AtlasBounds(limits.MaxEntries, limits.MaxEntries, operation.Entries.Count, 0,
            complete ? operation.Entries.Count : null, complete ? AtlasDenominatorState.Known : AtlasDenominatorState.Unknown,
            reason, dimension);
        activity?.SetTag("atlas.inventory.completion", completion.ToString());
        activity?.SetTag("atlas.inventory.entries", operation.Entries.Count);
        activity?.SetTag("atlas.inventory.duration_ms", operation.Elapsed.Elapsed.TotalMilliseconds);
        activity?.SetTag("atlas.inventory.descriptors.peak", operation.PeakDescriptors);
        activity?.SetTag("atlas.inventory.collection.peak", operation.PeakCollection);
        activity?.SetTag("atlas.inventory.limit.entries", limits.MaxEntries);
        activity?.SetTag("atlas.inventory.limit.depth", limits.MaxDepth);
        activity?.SetTag("atlas.inventory.limit.descriptors", limits.MaxDescriptors);
        return new DirectoryObservation($"directory-observation:{ObservationEpoch}:{sequence.ToString(CultureInfo.InvariantCulture)}",
            grant.ExpectedNativeRootIdentity, observedRoot, completion, sequence, bounds, operation.Entries);
    }

    private void Walk(string root, string directory, string rootFinal, int depth, Operation operation)
    {
        operation.Check();
        var paths = new List<string>();
        var remaining = operation.Limits.MaxEntries - operation.Entries.Count;
        var more = false;
        // Reserve the live System.IO enumeration descriptor too; dispose it before opening children.
        using (operation.ReserveDescriptor())
        using (var iterator = Directory.EnumerateFileSystemEntries(directory).GetEnumerator())
        {
            while (true)
            {
                operation.Check();
                if (!iterator.MoveNext()) break;
                operation.Check();
                if (paths.Count == remaining) { more = true; break; }
                paths.Add(iterator.Current);
                operation.PeakCollection = Math.Max(operation.PeakCollection, paths.Count);
            }
        }
        paths.Sort(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            operation.Check();
            if (operation.Entries.Count == operation.Limits.MaxEntries) throw new BudgetException("entries");
            using var inspected = operation.Open(path);
            if (!UnderOrSame(rootFinal, inspected.FinalPath)) throw new RefusedException("child-final-path-outside");
            var info = inspected.Information;
            if (IsReparse(info))
            {
                operation.ExcludedLink = true;
                operation.Entries.Add(new AtlasDirectoryEntry(Relative(root, path),
                    AtlasDirectoryEntryKind.RejectedLink, null, null, null, "reparse-excluded"));
            }
            else
            {
                var directoryEntry = IsDirectory(info);
                var length = checked((long)(((ulong)info.FileSizeHigh << 32) | info.FileSizeLow));
                operation.Entries.Add(new AtlasDirectoryEntry(Relative(root, path),
                    directoryEntry ? AtlasDirectoryEntryKind.Directory : AtlasDirectoryEntryKind.File,
                    Identity(info), directoryEntry ? null : length, info.NumberOfLinks, null));
            }
            afterEntryObserved?.Invoke();
            operation.Check();
            if (!IsReparse(info) && IsDirectory(info))
            {
                if (depth + 1 >= operation.Limits.MaxDepth) throw new BudgetException("depth");
                Walk(root, path, rootFinal, depth + 1, operation);
            }
        }
        if (more) throw new BudgetException("entries");
    }

    internal AtlasObjectIdentity ObserveExpectedNativeRootIdentityForTest(string root)
    {
        using var handle = OpenNative(Path.GetFullPath(root));
        var information = Information(handle);
        if (IsReparse(information)) throw new IOException("root is a reparse point");
        return Identity(information);
    }

    private static bool RefusedInput(string root) =>
        !OperatingSystem.IsWindows() || root.Length < 3 || !char.IsAsciiLetter(root[0])
        || root[1] != ':' || root[2] != '\\' || !Path.IsPathFullyQualified(root)
        || root.AsSpan(2).Contains(':') || root.Contains('/') || root.Contains('\0')
        || root.Split('\\').Skip(1).Any(part => part is "." or ".." || part.EndsWith(' ') || part.EndsWith('.'));

    private static bool UnderOrSame(string root, string candidate) =>
        string.Equals(root, candidate, StringComparison.OrdinalIgnoreCase)
        || candidate.StartsWith(root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);
    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
    private static bool IsReparse(ByHandleFileInformation info) => (info.FileAttributes & (uint)FileAttributes.ReparsePoint) != 0;
    private static bool IsDirectory(ByHandleFileInformation info) => (info.FileAttributes & (uint)FileAttributes.Directory) != 0;
    private static AtlasObjectIdentity Identity(ByHandleFileInformation info) =>
        new(info.VolumeSerialNumber.ToString("x", CultureInfo.InvariantCulture),
            (((ulong)info.FileIndexHigh << 32) | info.FileIndexLow).ToString("x", CultureInfo.InvariantCulture));

    private static SafeFileHandle OpenNative(string path)
    {
        var handle = CreateFileW(path, GenericRead, FileShareRead, IntPtr.Zero, OpenExisting,
            FileFlagBackupSemantics | FileFlagOpenReparsePoint, IntPtr.Zero);
        if (!handle.IsInvalid) return handle;
        var error = Marshal.GetLastWin32Error();
        handle.Dispose();
        throw new Win32Exception(error);
    }

    private static ByHandleFileInformation Information(SafeFileHandle handle)
    {
        if (!GetFileInformationByHandle(handle, out var info)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return info;
    }

    private static string FinalPath(SafeFileHandle handle)
    {
        var buffer = new StringBuilder(512);
        var result = GetFinalPathNameByHandleW(handle, buffer, buffer.Capacity, 2);
        if (result == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        if (result >= buffer.Capacity)
        {
            buffer.EnsureCapacity(checked((int)result + 1));
            result = GetFinalPathNameByHandleW(handle, buffer, buffer.Capacity, 2);
            if (result == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (result >= buffer.Capacity) throw new IOException("final-path-unstable");
        }
        return buffer.ToString();
    }

    private sealed class Operation(AtlasRootGrant grant, EnumerationLimits limits, TimeProvider clock,
        Func<AtlasRootGrant, bool>? isCurrent, CancellationToken cancellation)
    {
        private int _descriptors;
        public EnumerationLimits Limits { get; } = limits;
        public Stopwatch Elapsed { get; } = Stopwatch.StartNew();
        public List<AtlasDirectoryEntry> Entries { get; } = [];
        public int PeakDescriptors { get; private set; }
        public int PeakCollection { get; set; }
        public bool ExcludedLink { get; set; }

        public void Check()
        {
            if (clock.GetUtcNow() >= grant.ExpiresAt || isCurrent?.Invoke(grant) is not true)
                throw new RefusedException("grant-expired-or-policy-unavailable");
            cancellation.ThrowIfCancellationRequested();
            if (Elapsed.Elapsed >= Limits.Timeout) throw new OperationCanceledException();
        }

        public DescriptorLease ReserveDescriptor()
        {
            Check();
            if (_descriptors == Limits.MaxDescriptors) throw new BudgetException("descriptors");
            _descriptors++;
            PeakDescriptors = Math.Max(PeakDescriptors, _descriptors);
            return new DescriptorLease(() => _descriptors--);
        }

        public InspectedHandle Open(string path)
        {
            var lease = ReserveDescriptor();
            SafeFileHandle? handle = null;
            try
            {
                handle = OpenNative(path);
                Check();
                var info = Information(handle);
                var finalPath = FinalPath(handle);
                Check();
                return new InspectedHandle(handle, lease, info, finalPath);
            }
            catch
            {
                handle?.Dispose();
                lease.Dispose();
                throw;
            }
        }
    }

    private sealed class DescriptorLease(Action release) : IDisposable
    {
        private Action? _release = release;
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }

    private sealed class InspectedHandle(SafeFileHandle handle, DescriptorLease lease,
        ByHandleFileInformation information, string finalPath) : IDisposable
    {
        public ByHandleFileInformation Information { get; } = information;
        public string FinalPath { get; } = finalPath;
        public void Dispose() { handle.Dispose(); lease.Dispose(); }
    }

    private sealed class RefusedException(string reason) : Exception(reason);
    private sealed class BudgetException(string dimension) : Exception(dimension);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation information);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle file, StringBuilder path, int pathLength, uint flags);
    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation { public uint FileAttributes; public FileTime CreationTime; public FileTime LastAccessTime; public FileTime LastWriteTime; public uint VolumeSerialNumber; public uint FileSizeHigh; public uint FileSizeLow; public uint NumberOfLinks; public uint FileIndexHigh; public uint FileIndexLow; }
    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime { public uint LowDateTime; public uint HighDateTime; }
}
