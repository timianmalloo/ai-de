using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CodeAtlas.SourceReaderProbe;

internal enum SourceReadStatus
{
    IndexedMatch,
    Changed,
    Unavailable,
    Unverifiable,
    UnsupportedEncoding,
    TooLargeToVerify,
    ReadUnstable,
    Refused,
    Canceled,
}

internal sealed record SourceReadResult(
    SourceReadStatus Status,
    string? Text,
    string? Sha256,
    string Detail,
    OpenedObjectInfo? RootBefore = null,
    OpenedObjectInfo? RootAfter = null,
    OpenedObjectInfo? FileBefore = null,
    OpenedObjectInfo? FileAfter = null,
    string? RootFinalPath = null,
    string? FileFinalPath = null);

internal sealed record OpenedObjectInfo(
    ulong VolumeSerialNumber,
    ulong FileIndex,
    long Length,
    uint Attributes,
    uint LinkCount,
    DateTime LastWriteUtc)
{
    public string Identity => $"vol={VolumeSerialNumber:x};idx={FileIndex:x};links={LinkCount};len={Length}";
}

internal sealed class OpenedSourceReader(int maxBytes)
{
    private const uint FileShareRead = 0x00000001;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileNameNormalized = 0x0;
    private const uint VolumeNameNt = 0x2;
    private const FileAttributes ReparsePoint = FileAttributes.ReparsePoint;

    public SourceReadResult Read(string root, string relativePath, string? indexedHash, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new SourceReadResult(SourceReadStatus.Canceled, null, null, "canceled before open");
        }

        var rootFull = Path.GetFullPath(root);
        var filePath = Path.GetFullPath(Path.Combine(rootFull, relativePath));
        if (!IsUnder(rootFull, filePath))
        {
            return new SourceReadResult(SourceReadStatus.Refused, null, null, "relative path escapes root");
        }

        try
        {
            using var rootHandle = OpenDirectory(rootFull);
            using var ancestorHandles = OpenAncestors(rootFull, relativePath);
            var rootBefore = Information(rootHandle);
            var rootFinal = FinalPath(rootHandle);

            if (ContainsReparsePoint(rootFull, filePath))
            {
                var rootAfterReparse = Information(rootHandle);
                return new SourceReadResult(SourceReadStatus.Unverifiable, null, null, "path contains a reparse point", rootBefore, rootAfterReparse, RootFinalPath: rootFinal);
            }

            using var fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
            var fileBefore = Information(fileHandle);
            var fileFinal = FinalPath(fileHandle);

            if (!IsFinalUnder(rootFinal, fileFinal))
            {
                return new SourceReadResult(SourceReadStatus.Refused, null, null, "opened file final path is outside held root", rootBefore, Information(rootHandle), fileBefore, null, rootFinal, fileFinal);
            }

            if (fileBefore.LinkCount > 1)
            {
                return new SourceReadResult(SourceReadStatus.Unverifiable, null, null, "file has more than one hard link", rootBefore, Information(rootHandle), fileBefore, null, rootFinal, fileFinal);
            }

            if (fileBefore.Length > maxBytes)
            {
                return new SourceReadResult(SourceReadStatus.TooLargeToVerify, null, null, $"{fileBefore.Length} bytes exceeds {maxBytes}", rootBefore, Information(rootHandle), fileBefore, null, rootFinal, fileFinal);
            }

            var bytes = ReadAll(fileHandle, checked((int)fileBefore.Length), cancellationToken);
            var rootAfter = Information(rootHandle);
            var fileAfter = Information(fileHandle);
            if (!SameIdentity(rootBefore, rootAfter) || !Stable(fileBefore, fileAfter) || bytes.Length != fileBefore.Length)
            {
                return new SourceReadResult(SourceReadStatus.ReadUnstable, null, null, "metadata changed while reading", rootBefore, rootAfter, fileBefore, fileAfter, rootFinal, fileFinal);
            }

            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            string text;
            try
            {
                text = Decode(bytes);
            }
            catch (DecoderFallbackException ex)
            {
                return new SourceReadResult(SourceReadStatus.UnsupportedEncoding, null, hash, ex.Message, rootBefore, rootAfter, fileBefore, fileAfter, rootFinal, fileFinal);
            }

            var status = indexedHash is null
                ? SourceReadStatus.Unverifiable
                : string.Equals(hash, indexedHash, StringComparison.OrdinalIgnoreCase)
                    ? SourceReadStatus.IndexedMatch
                    : SourceReadStatus.Changed;

            return new SourceReadResult(status, text, hash, status.ToString(), rootBefore, rootAfter, fileBefore, fileAfter, rootFinal, fileFinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or ArgumentException or NotSupportedException)
        {
            return new SourceReadResult(SourceReadStatus.Unavailable, null, null, ex.GetType().Name + ": " + ex.Message);
        }
    }

    public bool RenameRootBlocked(string root) => MoveBlockedWhileHeld(root, root + ".moved", () => OpenDirectory(root));

    public bool RenameAncestorBlocked(string root, string relativeDirectory)
    {
        var dir = Path.Combine(root, relativeDirectory);
        return MoveBlockedWhileHeld(dir, dir + ".moved", () => OpenDirectory(Path.Combine(root, relativeDirectory)));
    }

    public bool RenameFileBlocked(string path) => MoveBlockedWhileHeld(path, path + ".moved", () => File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read));

    public bool WriteFileBlocked(string path)
    {
        using var held = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            using var write = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static bool CreateHardLink(string newPath, string existingPath) => CreateHardLinkW(newPath, existingPath, IntPtr.Zero);

    private static bool MoveBlockedWhileHeld(string source, string destination, Func<SafeFileHandle> hold)
    {
        using var handle = hold();
        try
        {
            if (Directory.Exists(source))
            {
                Directory.Move(source, destination);
                Directory.Move(destination, source);
            }
            else
            {
                File.Move(source, destination);
                File.Move(destination, source);
            }

            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static SafeFileHandle OpenDirectory(string path)
    {
        var handle = CreateFileW(path, 0x80000000, 0, IntPtr.Zero, OpenExisting, FileFlagBackupSemantics, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateFileW directory open failed for " + path);
        }

        return handle;
    }

    private static DirectoryHandleSet OpenAncestors(string root, string relativeFile)
    {
        var set = new DirectoryHandleSet();
        var parts = relativeFile.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        var current = root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            current = Path.Combine(current, parts[i]);
            set.Add(OpenDirectory(current));
        }

        return set;
    }

    private static bool ContainsReparsePoint(string root, string filePath)
    {
        var relative = Path.GetRelativePath(root, filePath);
        var parts = relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        var current = root;
        foreach (var part in parts)
        {
            current = Path.Combine(current, part);
            if ((File.GetAttributes(current) & ReparsePoint) != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] ReadAll(SafeFileHandle handle, int length, CancellationToken cancellationToken)
    {
        var bytes = new byte[length];
        var offset = 0;
        while (offset < bytes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = RandomAccess.Read(handle, bytes.AsSpan(offset), offset);
            if (read == 0)
            {
                break;
            }

            offset += read;
        }

        return offset == bytes.Length ? bytes : bytes[..offset];
    }

    private static string Decode(byte[] bytes)
    {
        if (bytes is [0xEF, 0xBB, 0xBF, ..])
        {
            return StrictUtf8().GetString(bytes, 3, bytes.Length - 3);
        }

        if (bytes is [0xFF, 0xFE, ..])
        {
            return new UnicodeEncoding(false, true, true).GetString(bytes, 2, bytes.Length - 2);
        }

        if (bytes is [0xFE, 0xFF, ..])
        {
            return new UnicodeEncoding(true, true, true).GetString(bytes, 2, bytes.Length - 2);
        }

        return StrictUtf8().GetString(bytes);
    }

    private static UTF8Encoding StrictUtf8() => new(false, true);

    private static bool IsUnder(string root, string candidate)
    {
        var rooted = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(rooted, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFinalUnder(string rootFinal, string fileFinal)
    {
        var rooted = rootFinal.EndsWith('\\') ? rootFinal : rootFinal + "\\";
        return fileFinal.StartsWith(rooted, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameIdentity(OpenedObjectInfo left, OpenedObjectInfo right) =>
        left.VolumeSerialNumber == right.VolumeSerialNumber && left.FileIndex == right.FileIndex;

    private static bool Stable(OpenedObjectInfo before, OpenedObjectInfo after) =>
        SameIdentity(before, after)
        && before.Length == after.Length
        && before.LastWriteUtc == after.LastWriteUtc
        && before.Attributes == after.Attributes
        && before.LinkCount == after.LinkCount;

    private static OpenedObjectInfo Information(SafeFileHandle handle)
    {
        if (!GetFileInformationByHandle(handle, out var info))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetFileInformationByHandle failed");
        }

        var index = ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow;
        var length = ((long)info.FileSizeHigh << 32) | info.FileSizeLow;
        return new OpenedObjectInfo(info.VolumeSerialNumber, index, length, info.FileAttributes,
            info.NumberOfLinks, DateTime.FromFileTimeUtc(info.LastWriteTime.ToLong()));
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

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLinkW(string fileName, string existingFileName, IntPtr securityAttributes);

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

        public long ToLong() => ((long)HighDateTime << 32) | LowDateTime;
    }

    private sealed class DirectoryHandleSet : IDisposable
    {
        private readonly List<SafeFileHandle> _handles = [];

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
