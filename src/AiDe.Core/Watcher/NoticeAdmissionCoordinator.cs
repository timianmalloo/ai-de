using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Watcher;

// Pattern: bounded reservation + disposable enrollment. Counts are refreshed from the database,
// not persisted here. This lock never calls a registrar: the order is registrar -> coordinator -> store.
internal sealed class NoticeAdmissionCoordinator : IDisposable
{
    internal const int Limit = 128;
    internal static readonly object Gate = new();
    private static readonly Dictionary<string, Enrollment> Enrollments = new(StringComparer.Ordinal);
    private static int _reserved;
    private readonly string _identity;
    private readonly SqliteWatcherObservationStore _store;
    private bool _disposed;

    private sealed class Enrollment(FileStream owner)
    {
        internal FileStream Owner { get; } = owner;
        internal List<NoticeAdmissionCoordinator> Handles { get; } = [];
    }

    internal NoticeAdmissionCoordinator(SqliteWatcherObservationStore store)
    {
        if (!OperatingSystem.IsWindows()) throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Unavailable);
        _store = store;
        var file = new FileStream(store.DatabasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        try
        {
            if (!GetFileInformationByHandle(file.SafeFileHandle, out var info))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            _identity = $"{info.VolumeSerialNumber:x8}:{info.FileIndexHigh:x8}{info.FileIndexLow:x8}";
            lock (Gate)
            {
                if (!Enrollments.TryGetValue(_identity, out var enrollment))
                {
                    if (Enrollments.Count >= Limit) throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Capacity);
                    try { file.Lock(long.MaxValue - 1, 1); }
                    catch (IOException) { throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Owner); }
                    enrollment = new Enrollment(file);
                    Enrollments.Add(_identity, enrollment);
                }
                else file.Dispose();
                enrollment.Handles.Add(this);
            }
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    internal T Run<T>(Func<Func<long, bool, IDisposable>, T> operation)
    {
        lock (Gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            IDisposable Reserve(long localPending, bool correction)
            {
                var pending = Enrollments.Sum(pair => pair.Key == _identity
                    ? localPending : pair.Value.Handles[0]._store.PendingNativeNoticeCount());
                if (pending + _reserved + (correction ? 1 : 0) > Limit)
                    throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Capacity);
                if (correction) _reserved++;
                return new Reservation(() => { if (correction) _reserved--; });
            }
            return operation(Reserve);
        }
    }

    private sealed class Reservation(Action release) : IDisposable
    {
        public void Dispose() => release();
    }

    public void Dispose()
    {
        lock (Gate)
        {
            if (_disposed) return;
            _disposed = true;
            var enrollment = Enrollments[_identity];
            enrollment.Handles.Remove(this);
            if (enrollment.Handles.Count != 0) return;
            Enrollments.Remove(_identity);
            enrollment.Owner.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        internal uint FileAttributes;
        internal System.Runtime.InteropServices.ComTypes.FILETIME CreationTime, LastAccessTime, LastWriteTime;
        internal uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, FileIndexHigh, FileIndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle handle, out ByHandleFileInformation information);
}
