using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using Microsoft.Win32.SafeHandles;

namespace AiDe.Core.Watcher;

// Pattern: bounded reservation + retained enrollment. Committed obligations belong to the
// owner-locked file, not its roots. The global lock never runs composition callbacks or takes
// an admission store lock; each enrollment has an independent, read-only WAL connection.
internal sealed class NoticeAdmissionCoordinator : IDisposable
{
    internal const int Limit = 128;
    internal static readonly object Gate = new();
    private static readonly Dictionary<string, Enrollment> Enrollments = new(StringComparer.Ordinal);
    private static int _reserved;
    [ThreadStatic] private static bool _running;
    private readonly string _identity;
    private bool _disposed;

    private sealed class Enrollment(FileStream owner, SqliteWatcherObservationStore reader)
    {
        internal FileStream Owner { get; } = owner;
        internal SqliteWatcherObservationStore Reader { get; } = reader;
        internal List<NoticeAdmissionCoordinator> Handles { get; } = [];
        internal int Operations { get; set; }
        internal object PublicationGate { get; } = new();
        internal string PublicationOwner { get; } = Guid.NewGuid().ToString("N");

        internal long Pending()
        {
            try { return Reader.PendingNativeNoticeCount(); }
            catch (SqliteException) { throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Uncertain); }
            catch (InvalidOperationException) { throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Uncertain); }
        }
    }

    internal NoticeAdmissionCoordinator(SqliteWatcherObservationStore store)
    {
        if (!OperatingSystem.IsWindows()) throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Unavailable);
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
                    enrollment = new Enrollment(file, SqliteWatcherObservationStore.OpenReadOnly(store.DatabasePath));
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
        RequireNonReentrant();
        lock (Gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            Enrollments[_identity].Operations++;
        }
        _running = true;
        try { return operation(Reserve); }
        finally
        {
            _running = false;
            lock (Gate)
            {
                var enrollment = Enrollments[_identity];
                enrollment.Operations--;
                RetireIfQuiescent(enrollment);
            }
        }
    }

    internal static void RequireNonReentrant()
    {
        if (_running) throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Busy);
    }

    // The OS lock excludes other enrolled processes; this mutex proves prior local workers
    // quiescent. Holding both, not elapsed time, qualifies recovery of an abandoned attempt.
    internal T RunPublication<T>(Func<string, T> operation) => Run(_ =>
    {
        Enrollment enrollment;
        lock (Gate) enrollment = Enrollments[_identity];
        lock (enrollment.PublicationGate) return operation(enrollment.PublicationOwner);
    });

    private IDisposable Reserve(long localPending, bool correction)
    {
        lock (Gate)
        {
            var pending = Enrollments.Sum(pair => pair.Key == _identity ? localPending : pair.Value.Pending());
            if (pending + _reserved + (correction ? 1 : 0) > Limit)
                throw NativeAdmissionErrors.Error(NativeAdmissionErrors.Capacity);
            if (correction) _reserved++;
            return new Reservation(correction);
        }
    }

    private sealed class Reservation(bool correction) : IDisposable
    {
        private bool _released;
        public void Dispose()
        {
            lock (Gate)
            {
                if (_released) return;
                _released = true;
                if (correction) _reserved--;
            }
        }
    }

    public void Dispose()
    {
        lock (Gate)
        {
            if (_disposed) return;
            _disposed = true;
            var enrollment = Enrollments[_identity];
            enrollment.Handles.Remove(this);
            RetireIfQuiescent(enrollment);
        }
    }

    private void RetireIfQuiescent(Enrollment enrollment)
    {
        if (0 != enrollment.Handles.Count || 0 != enrollment.Operations || 0 != enrollment.Pending()) return;
        Enrollments.Remove(_identity);
        enrollment.Reader.Dispose();
        enrollment.Owner.Dispose();
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
