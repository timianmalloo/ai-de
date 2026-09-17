using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Watcher;

/// <summary>
/// Linux legacy-notice publication relative to held directory identities.
/// Ancestor renames do not redirect operations to replacement symlink targets.
/// </summary>
internal sealed class RegistrationPublicationUnix : IDisposable
{
    // Linux open(2)/openat(2), measured through glibc 2.39 on x86_64 in the p25 API spike.
    // https://man7.org/linux/man-pages/man2/open.2.html
    private const int DirectoryFlags = 0x10000 | 0x20000 | 0x80000;
    private const int ReadFlags = 0x20000 | 0x80000 | 0x800;
    private const int CreateFlags = 1 | 0x40 | 0x80 | 0x20000 | 0x80000;
    private const int Missing = 2;
    private const int Exists = 17;
    private readonly List<SafeFileHandle> pins = [];
    private SafeFileHandle DirectoryHandle => pins[^1];

    private RegistrationPublicationUnix() { }

    internal static RegistrationPublicationUnix Open(string root)
    {
        NativeAdmissionCodec.ValidateLocalPath(root);
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Unavailable);
        if (root == "/") throw NativeAdmissionErrors.Error(NativeAdmissionErrors.ContextMismatch);
        var scope = new RegistrationPublicationUnix();
        try
        {
            scope.pins.Add(Own(OpenAt(-100, "/", DirectoryFlags, 0)));
            foreach (var component in root.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Append(RegistrationPublisher.DirectoryName))
            {
                var descriptor = OpenAt(scope.DirectoryHandle.DangerousGetHandle().ToInt32(), component, DirectoryFlags, 0);
                if (descriptor < 0 && Marshal.GetLastPInvokeError() == Missing)
                {
                    if (MakeDirectoryAt(scope.DirectoryHandle, component, 0x1C0) != 0
                        && Marshal.GetLastPInvokeError() != Exists)
                        ThrowIo();
                    descriptor = OpenAt(scope.DirectoryHandle.DangerousGetHandle().ToInt32(), component, DirectoryFlags, 0);
                }
                scope.pins.Add(Own(descriptor));
            }
            // flock(2): advisory, per opened inode, nonblocking. No persistent lock-file namespace.
            if (Lock(scope.DirectoryHandle, 2 | 4) != 0) ThrowIo();
            return scope;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    internal void Publish(string name, string sessionId, byte[] bytes)
    {
        NativeAdmissionCodec.Text(sessionId, 512);
        if (name != StandingPublisher.FileNameFor(sessionId) || name.Contains('/')
            || name.Contains('\0') || bytes.Length is < 1 or > 65536)
            throw NativeAdmissionErrors.Error(NativeAdmissionErrors.InvalidInput);
        string? temporary = null;
        try
        {
            var existing = OpenAt(DirectoryHandle.DangerousGetHandle().ToInt32(), name, ReadFlags, 0);
            var replacing = existing >= 0;
            if (!replacing && Marshal.GetLastPInvokeError() != Missing) ThrowIo();
            if (replacing)
            {
                using var handle = Own(existing);
                using var stream = new FileStream(handle, FileAccess.Read);
                RegistrationPublisher.RequireCompatibilityOwner(stream, sessionId);
            }

            var candidate = $".notice-{Guid.NewGuid():N}.tmp";
            using (var handle = Own(OpenAt(DirectoryHandle.DangerousGetHandle().ToInt32(), candidate, CreateFlags, 0x180)))
            {
                temporary = candidate;
                using var stream = new FileStream(handle, FileAccess.Write);
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            // renameat(2) replaces the leaf itself, not a symlink's referent. linkat(2) never
            // overwrites an absent-target competitor. Both names are relative to the held inode.
            var result = replacing
                ? RenameAt(DirectoryHandle, temporary, DirectoryHandle, name)
                : LinkAt(DirectoryHandle, temporary, DirectoryHandle, name, 0);
            if (result != 0)
            {
                if (Marshal.GetLastPInvokeError() == Exists)
                    throw NativeAdmissionErrors.Error(NativeAdmissionErrors.NoticeConflict);
                ThrowIo();
            }

            using var installed = Own(OpenAt(DirectoryHandle.DangerousGetHandle().ToInt32(), name, ReadFlags, 0));
            using var read = new FileStream(installed, FileAccess.Read);
            if (read.Length != bytes.Length) throw NativeAdmissionErrors.Error(NativeAdmissionErrors.NoticeConflict);
            var actual = new byte[bytes.Length];
            read.ReadExactly(actual);
            if (!actual.AsSpan().SequenceEqual(bytes))
                throw NativeAdmissionErrors.Error(NativeAdmissionErrors.NoticeConflict);
        }
        catch (IOException) { throw NativeAdmissionErrors.Error(NativeAdmissionErrors.NoticeIo); }
        catch (UnauthorizedAccessException) { throw NativeAdmissionErrors.Error(NativeAdmissionErrors.NoticeIo); }
        catch (NotSupportedException) { throw NativeAdmissionErrors.Error(NativeAdmissionErrors.NoticeIo); }
        finally
        {
            if (null != temporary && UnlinkAt(DirectoryHandle, temporary, 0) != 0
                && Marshal.GetLastPInvokeError() != Missing)
                ThrowIo();
        }
    }

    public void Dispose()
    {
        for (var index = pins.Count - 1; index >= 0; index--) pins[index].Dispose();
        pins.Clear();
    }

    private static SafeFileHandle Own(int descriptor)
    {
        if (descriptor < 0) ThrowIo();
        return new SafeFileHandle(new IntPtr(descriptor), ownsHandle: true);
    }

    private static void ThrowIo() => throw NativeAdmissionErrors.Error(
        Marshal.GetLastPInvokeError() is 20 or 40 ? NativeAdmissionErrors.ContextMismatch : NativeAdmissionErrors.NoticeIo);

    [DllImport("libc", EntryPoint = "openat", SetLastError = true)]
    private static extern int OpenAt(int directory, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags, uint mode);

    [DllImport("libc", EntryPoint = "mkdirat", SetLastError = true)]
    private static extern int MakeDirectoryAt(SafeFileHandle directory, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, uint mode);

    [DllImport("libc", EntryPoint = "flock", SetLastError = true)]
    private static extern int Lock(SafeFileHandle descriptor, int operation);

    [DllImport("libc", EntryPoint = "renameat", SetLastError = true)]
    private static extern int RenameAt(SafeFileHandle oldDirectory, [MarshalAs(UnmanagedType.LPUTF8Str)] string oldName,
        SafeFileHandle newDirectory, [MarshalAs(UnmanagedType.LPUTF8Str)] string newName);

    [DllImport("libc", EntryPoint = "linkat", SetLastError = true)]
    private static extern int LinkAt(SafeFileHandle oldDirectory, [MarshalAs(UnmanagedType.LPUTF8Str)] string oldName,
        SafeFileHandle newDirectory, [MarshalAs(UnmanagedType.LPUTF8Str)] string newName, int flags);

    [DllImport("libc", EntryPoint = "unlinkat", SetLastError = true)]
    private static extern int UnlinkAt(SafeFileHandle directory, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int flags);
}
