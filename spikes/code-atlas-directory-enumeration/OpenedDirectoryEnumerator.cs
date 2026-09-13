using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CodeAtlas.DirectoryEnumerationProbe;

internal enum DirectoryEnumerationStatus { Complete, Refused, Unavailable, Unverifiable, LimitExceeded, Canceled }

internal sealed record DirectoryEntryObservation(
    string RelativePath,
    string Kind,
    DirectoryEnumerationStatus Status,
    string Detail = "");

internal sealed record DirectoryEnumerationResult(
    DirectoryEnumerationStatus Status,
    IReadOnlyList<DirectoryEntryObservation> Entries,
    string Detail,
    int PeakHeldHandles = 0,
    int? NativeErrorCode = null);

internal sealed record RootBinding(ulong VolumeSerialNumber, ulong FileIndex, string FinalPath)
{
    public string Identity => $"vol={VolumeSerialNumber:x};idx={FileIndex:x}";
}

internal sealed class OpenedDirectoryEnumerator(
    int maxEntries,
    int maxDepth,
    int maxDescriptors,
    Action? afterEntryObserved = null)
{
    private const uint GenericRead = 0x80000000;
    private const uint FileShareRead = 0x00000001;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileNameNormalized = 0x0;
    private const uint VolumeNameNt = 0x2;

    private int _held;
    private int _peak;
    private bool _limitReached;

    public string LastMutationDetail { get; private set; } = string.Empty;

    public RootBinding CaptureRootBinding(string root)
    {
        var rootFull = Path.GetFullPath(root);
        using var inspected = OpenDirectory(rootFull, followReparse: false);
        var info = Information(inspected);
        if (IsReparse(info))
        {
            throw new IOException("root is a reparse point");
        }

        using var held = OpenDirectory(rootFull, followReparse: true);
        var final = FinalPath(held);
        var heldInfo = Information(held);
        return new RootBinding(heldInfo.VolumeSerialNumber, heldInfo.FileIndex, final);
    }

    public DirectoryEnumerationResult Enumerate(
        string root,
        string relativeDirectory,
        CancellationToken cancellationToken = default) =>
        Enumerate(root, relativeDirectory, binding: null, cancellationToken);

    public DirectoryEnumerationResult Enumerate(
        string root,
        string relativeDirectory,
        RootBinding? binding,
        CancellationToken cancellationToken = default)
    {
        _held = 0;
        _peak = 0;
        _limitReached = false;

        if (cancellationToken.IsCancellationRequested)
        {
            return Result(DirectoryEnumerationStatus.Canceled, [], "canceled before open");
        }

        if (IsRefusedDirectoryId(relativeDirectory))
        {
            return Result(DirectoryEnumerationStatus.Refused, [], "directory id is outside relative local domain");
        }

        try
        {
            var rootFull = Path.GetFullPath(root);
            var target = Path.GetFullPath(Path.Combine(rootFull, relativeDirectory));
            if (!IsUnderOrSame(rootFull, target))
            {
                return Result(DirectoryEnumerationStatus.Refused, [], "relative directory escapes root");
            }

            using var rootInspection = OpenDirectory(rootFull, followReparse: false);
            var inspectedRoot = Information(rootInspection);
            if (IsReparse(inspectedRoot))
            {
                return Result(DirectoryEnumerationStatus.Unverifiable, [], "root is a reparse point");
            }

            using var rootHandle = OpenHeldDirectory(rootFull, followReparse: true);
            var rootInfo = Information(rootHandle.Handle);
            var rootFinal = FinalPath(rootHandle.Handle);
            if (binding is not null
                && (binding.VolumeSerialNumber != rootInfo.VolumeSerialNumber
                    || binding.FileIndex != rootInfo.FileIndex))
            {
                return Result(DirectoryEnumerationStatus.Unverifiable, [], "opened root identity differs from authorized binding");
            }

            if (!InspectRelativeComponents(rootFull, target, out var refusal))
            {
                return Result(DirectoryEnumerationStatus.Unverifiable, [], refusal);
            }

            if (RelativeParts(rootFull, target).Length + 1 > maxDescriptors)
            {
                return Result(DirectoryEnumerationStatus.LimitExceeded, [], "descriptor limit reached before opening ancestors");
            }

            using var targetHandles = OpenPathDirectories(rootFull, target);
            var entries = new List<DirectoryEntryObservation>();
            EnumerateDirectory(rootFull, rootFinal, target, depth: 0, entries, cancellationToken);
            var status = _limitReached ? DirectoryEnumerationStatus.LimitExceeded : DirectoryEnumerationStatus.Complete;
            return Result(status, entries, _limitReached ? "limit reached" : $"root={rootInfo.Identity}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result(DirectoryEnumerationStatus.Canceled, [], "canceled during enumeration");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            return Result(DirectoryEnumerationStatus.Unavailable, [], PublicErrorDetail(ex), NativeCode(ex));
        }
    }

    public bool RenameRootBlocked(string root) => MoveBlockedWhileHeld(root, root + ".moved", () => OpenDirectory(root, followReparse: true));

    public bool RenameAncestorBlocked(string root, string relativeDirectory)
    {
        var dir = Path.Combine(root, relativeDirectory);
        return MoveBlockedWhileHeld(dir, dir + ".moved", () => OpenDirectory(dir, followReparse: true));
    }

    private DirectoryEnumerationResult Result(
        DirectoryEnumerationStatus status,
        IReadOnlyList<DirectoryEntryObservation> entries,
        string detail,
        int? nativeErrorCode = null) =>
        new(status, entries, detail, _peak, nativeErrorCode);

    private void EnumerateDirectory(
        string root,
        string rootFinal,
        string directory,
        int depth,
        List<DirectoryEntryObservation> entries,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (depth > maxDepth || entries.Count >= maxEntries)
        {
            _limitReached = true;
            return;
        }

        using var directoryHandle = OpenHeldDirectory(directory, followReparse: true);
        var directoryFinal = FinalPath(directoryHandle.Handle);
        if (!IsFinalUnderOrSame(rootFinal, directoryFinal))
        {
            entries.Add(Entry(root, directory, "directory", DirectoryEnumerationStatus.Refused, "final path outside root"));
            return;
        }

        foreach (var path in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entries.Count >= maxEntries)
            {
                _limitReached = true;
                return;
            }

            using var inspect = OpenDirectory(path, followReparse: false);
            var info = Information(inspect);
            var isDirectory = (info.Attributes & (uint)FileAttributes.Directory) != 0;
            if (IsReparse(info))
            {
                entries.Add(Entry(root, path, isDirectory ? "directory" : "file", DirectoryEnumerationStatus.Unverifiable, "reparse point unsupported"));
                afterEntryObserved?.Invoke();
                continue;
            }

            entries.Add(Entry(root, path, isDirectory ? "directory" : "file", DirectoryEnumerationStatus.Complete));
            afterEntryObserved?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            if (isDirectory)
            {
                if (depth + 1 >= maxDepth)
                {
                    _limitReached = true;
                    continue;
                }

                if (_held + 1 > maxDescriptors)
                {
                    _limitReached = true;
                    return;
                }

                EnumerateDirectory(root, rootFinal, path, depth + 1, entries, cancellationToken);
                if (_limitReached)
                {
                    return;
                }
            }
        }
    }

    private bool InspectRelativeComponents(string root, string target, out string refusal)
    {
        var current = root;
        foreach (var part in RelativeParts(root, target))
        {
            current = Path.Combine(current, part);
            using var inspected = OpenDirectory(current, followReparse: false);
            if (IsReparse(Information(inspected)))
            {
                refusal = "path component is a reparse point";
                return false;
            }
        }

        refusal = string.Empty;
        return true;
    }

    private DirectoryHandleSet OpenPathDirectories(string root, string target)
    {
        var set = new DirectoryHandleSet();
        try
        {
            var current = root;
            foreach (var part in RelativeParts(root, target))
            {
                if (_held + 1 > maxDescriptors)
                {
                    _limitReached = true;
                    throw new IOException("descriptor limit reached while opening ancestors");
                }

                current = Path.Combine(current, part);
                set.Add(OpenHeldDirectory(current, followReparse: true));
            }

            return set;
        }
        catch
        {
            set.Dispose();
            throw;
        }
    }

    private CountedHandle OpenHeldDirectory(string path, bool followReparse)
    {
        if (_held + 1 > maxDescriptors)
        {
            _limitReached = true;
            throw new IOException("descriptor limit reached");
        }

        var handle = OpenDirectory(path, followReparse);
        _held++;
        _peak = Math.Max(_peak, _held);
        return new CountedHandle(handle, this);
    }

    private static string[] RelativeParts(string root, string target)
    {
        var relative = Path.GetRelativePath(root, target);
        return relative == "."
            ? []
            : relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
    }

    private static DirectoryEntryObservation Entry(
        string root,
        string path,
        string kind,
        DirectoryEnumerationStatus status,
        string detail = "") =>
        new(Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'), kind, status, detail);

    private static bool IsRefusedDirectoryId(string relativeDirectory) =>
        Path.IsPathFullyQualified(relativeDirectory)
        || relativeDirectory.Contains(':', StringComparison.Ordinal)
        || relativeDirectory.StartsWith("\\\\", StringComparison.Ordinal)
        || relativeDirectory.StartsWith("\\\\.\\", StringComparison.Ordinal);

    private static bool IsReparse(OpenedObjectInfo info) =>
        (info.Attributes & (uint)FileAttributes.ReparsePoint) != 0;

    private static SafeFileHandle OpenDirectory(string path, bool followReparse)
    {
        var flags = FileFlagBackupSemantics | (followReparse ? 0 : FileFlagOpenReparsePoint);
        var handle = CreateFileW(path, GenericRead, FileShareRead, IntPtr.Zero, OpenExisting, flags, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateFileW directory open failed for " + path);
        }

        return handle;
    }

    private bool MoveBlockedWhileHeld(string source, string destination, Func<SafeFileHandle> hold)
    {
        LastMutationDetail = string.Empty;
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new IOException("destination collision would invalidate oracle: " + destination);
        }

        var blocked = false;
        using (hold())
        {
            try
            {
                Directory.Move(source, destination);
                Directory.Move(destination, source);
                return false;
            }
            catch (Exception ex) when (IsExpectedMutationBlock(ex))
            {
                blocked = true;
                LastMutationDetail = ErrorDetail(ex);
            }
        }

        if (!blocked)
        {
            return false;
        }

        Directory.Move(source, destination);
        Directory.Move(destination, source);
        LastMutationDetail += ";destination absent before;after-release move succeeded";
        return true;
    }

    private static bool IsUnderOrSame(string root, string candidate)
    {
        if (string.Equals(root, candidate, StringComparison.OrdinalIgnoreCase)) return true;
        var rooted = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rooted, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFinalUnderOrSame(string rootFinal, string candidateFinal)
    {
        if (string.Equals(rootFinal, candidateFinal, StringComparison.OrdinalIgnoreCase)) return true;
        var rooted = rootFinal.EndsWith('\\') ? rootFinal : rootFinal + "\\";
        return candidateFinal.StartsWith(rooted, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExpectedMutationBlock(Exception ex)
    {
        var code = ex.HResult & 0xFFFF;
        return ex is IOException or UnauthorizedAccessException && code is 5 or 32 or 33;
    }

    private static string ErrorDetail(Exception ex) =>
        $"{ex.GetType().Name};hresult=0x{ex.HResult:X8};native={NativeCode(ex)?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none"};message={ex.Message}";

    private static string PublicErrorDetail(Exception ex) => ex switch
    {
        Win32Exception => "native open failed",
        IOException => "io unavailable",
        UnauthorizedAccessException => "access denied",
        ArgumentException => "invalid path",
        NotSupportedException => "unsupported path",
        _ => "unavailable",
    };

    private static int? NativeCode(Exception ex) => ex is Win32Exception win32 ? win32.NativeErrorCode : null;

    private static OpenedObjectInfo Information(SafeFileHandle handle)
    {
        if (!GetFileInformationByHandle(handle, out var info))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetFileInformationByHandle failed");
        }

        var index = ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow;
        return new OpenedObjectInfo(info.VolumeSerialNumber, index, info.FileAttributes);
    }

    private static string FinalPath(SafeFileHandle handle)
    {
        var buffer = new StringBuilder(512);
        var result = GetFinalPathNameByHandleW(handle, buffer, buffer.Capacity, FileNameNormalized | VolumeNameNt);
        if (result == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetFinalPathNameByHandleW failed");
        }

        if (result >= buffer.Capacity)
        {
            buffer.EnsureCapacity(checked((int)result + 1));
            result = GetFinalPathNameByHandleW(handle, buffer, buffer.Capacity, FileNameNormalized | VolumeNameNt);
            if (result == 0 || result >= buffer.Capacity)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "GetFinalPathNameByHandleW retry failed");
            }
        }

        return buffer.ToString();
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode,
        IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation information);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle file, StringBuilder filePath, int filePathLength, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public FileTime CreationTime;
        public FileTime LastAccessTime;
        public FileTime LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    private sealed record OpenedObjectInfo(ulong VolumeSerialNumber, ulong FileIndex, uint Attributes)
    {
        public string Identity => $"vol={VolumeSerialNumber:x};idx={FileIndex:x};attrs=0x{Attributes:x}";
    }

    private sealed class DirectoryHandleSet : IDisposable
    {
        private readonly List<CountedHandle> _handles = [];
        public void Add(CountedHandle handle) => _handles.Add(handle);
        public void Dispose()
        {
            foreach (var handle in _handles) handle.Dispose();
        }
    }

    private sealed class CountedHandle(SafeFileHandle handle, OpenedDirectoryEnumerator owner) : IDisposable
    {
        public SafeFileHandle Handle => handle;
        public void Dispose()
        {
            handle.Dispose();
            owner._held--;
        }
    }
}
