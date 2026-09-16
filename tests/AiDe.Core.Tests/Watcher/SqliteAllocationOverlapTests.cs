using System.Reflection;
using System.Runtime.InteropServices;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

/// <summary>O14: the second writer must contend before its allocation read, not before method entry.</summary>
public sealed class SqliteAllocationOverlapTests(ITestOutputHelper output)
{
    private static readonly TimeSpan DeadlockTimeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Post_OverlappingSqliteAllocations_AcquiresWriterBeforeReadingMaximum()
    {
        var directory = Directory.CreateDirectory(Path.Combine(
            Environment.CurrentDirectory, "aide-p2-overlap-" + Guid.NewGuid().ToString("N")));
        try
        {
            await AssertOverlappingWrites(Path.Combine(directory.FullName, "watcher.db"), directory.FullName);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private async Task AssertOverlappingWrites(string databasePath, string repositoryPath)
    {
        using var storeA = SqliteWatcherObservationStore.Open(databasePath);
        using var storeB = SqliteWatcherObservationStore.Open(databasePath);
        using var reader = SqliteWatcherObservationStore.OpenReadOnly(databasePath);
        var registrar = new TrustedRegistrar(storeA, new SequentialCapabilityFactory(),
            new FakeMonotonicClock(), () => "session");
        var session = registrar.Register(WatcherFixtures.Binding(repoPath: repositoryPath));
        var repository = session.Session.Binding.Repository.CanonicalPath;
        var time = new FixedTimeProvider(DateTimeOffset.UnixEpoch);
        var boardA = new MessageBoardService(storeA, registrar, time, () => "message-a");
        var boardB = new MessageBoardService(storeB, registrar, time, () => "message-b");
        using var schedule = new AllocationSchedule(Connection(storeA), Connection(storeB));
        var firstWrite = Task.Run(() =>
        {
            var message = boardA.Post(repository, session.SessionId, session.Capability, BoardMessageKind.Question, "A");
            schedule.FirstCommitted();
            return message;
        });
        Task<BoardMessage>? secondWrite = null;
        var cursor = 0;

        try
        {
            await schedule.FirstAtMaximum.Task.WaitAsync(DeadlockTimeout);
            Assert.Empty(reader.BoardMessages(repository));
            secondWrite = Task.Run(() => boardB.Post(repository, session.SessionId,
                session.Capability, BoardMessageKind.Question, "B"));
            await schedule.SecondAtBoundary.Task.WaitAsync(DeadlockTimeout);
            var first = await firstWrite.WaitAsync(DeadlockTimeout);
            Assert.Equal(first, Assert.Single(reader.BoardMessages(repository)));
            cursor = first.Seq;
        }
        finally
        {
            schedule.Release();
            await Task.WhenAll(secondWrite is null ? [firstWrite] : [firstWrite, secondWrite])
                .WaitAsync(DeadlockTimeout);
        }

        var firstMessage = await firstWrite;
        var secondMessage = await secondWrite!;
        var committed = reader.BoardMessages(repository);
        output.WriteLine("SQLite={0}; busy={1}; earlyMaximum={2}; callbackTimeouts={3}; committed={4}:{5},{6}:{7}",
            raw.sqlite3_libversion().utf8_to_string(), schedule.BusyCalls, schedule.EarlyMaximum,
            schedule.CallbackTimeouts, firstMessage.MessageId, firstMessage.Seq, secondMessage.MessageId, secondMessage.Seq);
        Assert.Equal(new[] { firstMessage, secondMessage }, committed);
        Assert.Equal(new[] { "message-a", "message-b" }, committed.Select(message => message.MessageId));
        Assert.Equal(new[] { 1, 2 }, committed.Select(message => message.Seq));
        Assert.Equal(secondMessage, Assert.Single(committed, message => message.Seq > cursor));
        Assert.Equal(0, schedule.CallbackTimeouts);
        Assert.False(schedule.EarlyMaximum, "B reached the allocation SELECT before A committed; writer acquisition did not serialize allocation.");
        Assert.Equal(1, schedule.BusyCalls);
    }

    // simplify: reflection is confined to this fixture's two owned connections; revisit if the store
    // changes its connection owner. Native callbacks observe SQL; no production test seam is added.
    private static SqliteConnection Connection(SqliteWatcherObservationStore store) =>
        Assert.IsType<SqliteConnection>(typeof(SqliteWatcherObservationStore)
            .GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(store));

    private sealed class AllocationSchedule : IDisposable
    {
        private readonly SqliteConnection _first;
        private readonly SqliteConnection _second;
        private readonly ManualResetEventSlim _releaseFirst = new();
        private readonly ManualResetEventSlim _releaseSecond = new();
        private readonly strdelegate_trace _firstTrace;
        private readonly strdelegate_trace _secondTrace;
        private readonly BusyCallback _busy;
        private int _firstCommitted;
        private int _earlyMaximum;
        private int _busyCalls;
        private int _callbackTimeouts;
        public TaskCompletionSource FirstAtMaximum { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondAtBoundary { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool EarlyMaximum => Volatile.Read(ref _earlyMaximum) != 0;
        public int BusyCalls => Volatile.Read(ref _busyCalls);
        public int CallbackTimeouts => Volatile.Read(ref _callbackTimeouts);

        public AllocationSchedule(SqliteConnection first, SqliteConnection second)
        {
            _first = first;
            _second = second;
            _firstTrace = TraceFirst;
            _secondTrace = TraceSecond;
            _busy = OnBusy;
            Assert.Equal(raw.SQLITE_OK, SetBusyHandler(second.Handle!.DangerousGetHandle(), _busy, IntPtr.Zero));
            raw.sqlite3_trace(first.Handle!, _firstTrace, null);
            raw.sqlite3_trace(second.Handle!, _secondTrace, null);
        }

        public void FirstCommitted() => Volatile.Write(ref _firstCommitted, 1);

        private void TraceFirst(object state, string sql)
        {
            if (!IsAllocation(sql))
            {
                return;
            }
            FirstAtMaximum.TrySetResult();
            Wait(_releaseFirst);
        }

        private void TraceSecond(object state, string sql)
        {
            if (!IsAllocation(sql) || Volatile.Read(ref _firstCommitted) != 0)
            {
                return;
            }
            Interlocked.Exchange(ref _earlyMaximum, 1);
            SecondAtBoundary.TrySetResult();
            _releaseFirst.Set();
            Wait(_releaseSecond);
        }

        private int OnBusy(IntPtr state, int count)
        {
            Interlocked.Increment(ref _busyCalls);
            SecondAtBoundary.TrySetResult();
            _releaseFirst.Set();
            return Wait(_releaseSecond) && count == 0 ? 1 : 0;
        }

        // SQLite invokes these callbacks on its native stack: never throw or wait without a bound.
        private bool Wait(ManualResetEventSlim release)
        {
            if (release.Wait(DeadlockTimeout))
            {
                return true;
            }
            Interlocked.Increment(ref _callbackTimeouts);
            return false;
        }

        private static bool IsAllocation(string sql) =>
            sql.StartsWith("SELECT COALESCE(MAX(seq)", StringComparison.Ordinal);

        public void Release()
        {
            _releaseFirst.Set();
            _releaseSecond.Set();
        }

        public void Dispose()
        {
            Release();
            raw.sqlite3_trace(_first.Handle!, (strdelegate_trace)null!, null);
            raw.sqlite3_trace(_second.Handle!, (strdelegate_trace)null!, null);
            var cleared = SetBusyHandler(_second.Handle!.DangerousGetHandle(), null, IntPtr.Zero);
            GC.KeepAlive(_busy);
            _releaseFirst.Dispose();
            _releaseSecond.Dispose();
            Assert.Equal(raw.SQLITE_OK, cleared);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int BusyCallback(IntPtr state, int count);

    // SQLitePCLRaw 2.1.12 exposes trace but not busy_handler. Bind the installed e_sqlite3 export.
    [DllImport("e_sqlite3", EntryPoint = "sqlite3_busy_handler", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SetBusyHandler(IntPtr database, BusyCallback? callback, IntPtr state);
}
