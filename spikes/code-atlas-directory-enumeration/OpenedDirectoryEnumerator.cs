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
    string Detail);

internal sealed class OpenedDirectoryEnumerator(int maxEntries, int maxDepth, int maxDescriptors)
{
    private const uint GenericRead = 0x80000000;
    private const uint FileShareRead = 0x00000001;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileNameNormalized = 0x0;
    private const uint VolumeNameNt = 0x2;

    public string LastMutationDetail { get; private set; } = string.Empty;

    public DirectoryEnumerationResult Enumerate(
        string root, string relativeDirectory, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new DirectoryEnumerationResult(DirectoryEnumerationStatus.Canceled, [], "canceled before open");
        }

        if (IsRefusedDirectoryId(relativeDirectory))
        {
            return new DirectoryEnumerationResult(DirectoryEnumerationStatus.Refused, [], "directory id is outside relative local domain");
        }

        try
        {
            var rootFull = Path.GetFullPath(root);
            var target = Path.GetFullPath(Path.Combine(rootFull, relativeDirectory));
            if (!IsUnderOrSame(rootFull, target))
            {
                return new DirectoryEnumerationResult(DirectoryEnumerationStatus.Refused, [], "relative directory escapes root");
            }

            using var rootHandle = OpenDirectory(rootFull);
            var rootInfoBefore = Information(rootHandle);
            var rootFinal = FinalPath(rootHandle);
            var relativeParts = RelativeParts(rootFull, target);
            if (relativeParts.Length > maxDescriptors)
            {
                return new DirectoryEnumerationResult(DirectoryEnumerationStatus.LimitExceeded, [], "descriptor limit reached before opening ancestors");
            }

            using var targetHandles = OpenPathDirectories(rootFull, target);
            var entries = new List<DirectoryEntryObservation>();
            var limited = false;
            EnumerateDirectory(rootFull, rootFinal, target, depth: 0, entries, ref limited, cancellationToken);
            var rootInfoAfter = Information(rootHandle);
            if (!SameIdentity(rootInfoBefore, rootInfoAfter))
            {
                return new DirectoryEnumerationResult(DirectoryEnumerationStatus.Unverifiable, entries, "root identity changed during enumeration");
            }

            return new DirectoryEnumerationResult(
                limited ? DirectoryEnumerationStatus.LimitExceeded : DirectoryEnumerationStatus.Complete,
                entries,
                limited ? "limit reached" : $"root={rootInfoBefore.Identity}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DirectoryEnumerationResult(DirectoryEnumerationStatus.Canceled, [], "canceled during enumeration");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            return new DirectoryEnumerationResult(DirectoryEnumerationStatus.Unavailable, [], ErrorDetail(ex));
        }
    }

    public bool RenameRootBlocked(string root) => MoveBlockedWhileHeld(root, root + ".moved", () => OpenDirectory(root));

    public bool RenameAncestorBlocked(string root, string relativeDirectory)
    {
        var dir = Path.Combine(root, relativeDirectory);
        return MoveBlockedWhileHeld(dir, dir + ".moved", () => OpenDirectory(dir));
    }

    private void EnumerateDirectory(
        string root,
        string rootFinal,
        string directory,
        int depth,
        List<DirectoryEntryObservation> entries,
        ref bool limited,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (depth > maxDepth || entries.Count >= maxEntries)
        {
            limited = true;
            return;
        }

        using var directoryHandle = OpenDirectory(directory);
        var directoryFinal = FinalPath(directoryHandle);
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
                limited = true;
                return;
            }

            var attributes = File.GetAttributes(path);
            var isDirectory = (attributes & FileAttributes.Directory) != 0;
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                entries.Add(Entry(root, path, isDirectory ? "directory" : "file", DirectoryEnumerationStatus.Unverifiable, "reparse point unsupported"));
                continue;
            }

            entries.Add(Entry(root, path, isDirectory ? "directory" : "file", DirectoryEnumerationStatus.Complete));
            if (isDirectory)
            {
                if (depth + 1 >= maxDepth)
                {
                    limited = true;
                    continue;
                }

                EnumerateDirectory(root, rootFinal, path, depth + 1, entries, ref limited, cancellationToken);
                if (limited)
                {
                    return;
                }
            }
        }
    }

    private static string[] RelativeParts(string root, string target)
    {
        var relative = Path.GetRelativePath(root, target);
        return relative == "."
            ? []
            : relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
    }

    private DirectoryHandleSet OpenPathDirectories(string root, string target)
    {
        var set = new DirectoryHandleSet();
        try
        {
            var current = root;
            var parts = RelativeParts(root, target);
            foreach (var part in parts)
            {
                if (set.Count >= maxDescriptors)
                {
                    throw new IOException("descriptor limit reached while opening ancestors");
                }

                current = Path.Combine(current, part);
                set.Add(OpenDirectory(current));
            }

            return set;
        }
        catch
        {
            set.Dispose();
            throw;
        }
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

    private static SafeFileHandle OpenDirectory(string path)
    {
        var handle = CreateFileW(path, GenericRead, FileShareRead, IntPtr.Zero, OpenExisting, FileFlagBackupSemantics, IntPtr.Zero);
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
        if (string.Equals(root, candidate, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var rooted = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rooted, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFinalUnderOrSame(string rootFinal, string candidateFinal)
    {
        if (string.Equals(rootFinal, candidateFinal, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var rooted = rootFinal.EndsWith('\\') ? rootFinal : rootFinal + "\\";
        return candidateFinal.StartsWith(rooted, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameIdentity(OpenedObjectInfo left, OpenedObjectInfo right) =>
        left.VolumeSerialNumber == right.VolumeSerialNumber && left.FileIndex == right.FileIndex;

    private static bool IsExpectedMutationBlock(Exception ex)
    {
        var code = ex.HResult & 0xFFFF;
        return ex is IOException or UnauthorizedAccessException && code is 5 or 32 or 33;
    }

    private static string ErrorDetail(Exception ex) =>
        $"{ex.GetType().Name};hresult=0x{ex.HResult:X8};win32={ex.HResult & 0xFFFF};message={ex.Message}";

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
        private readonly List<SafeFileHandle> _handles = [];
        public int Count => _handles.Count;
        public void Add(SafeFileHandle handle) => _handles.Add(handle);
        public void Dispose()
        {
            foreach (var handle in _handles)
            {
                handle.Dispose();
            }
        }
    }
}
