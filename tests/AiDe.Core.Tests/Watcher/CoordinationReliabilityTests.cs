using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using AiDe.Core.Watcher;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// P2 RED checkpoint, not a shippable candidate. Real writer/ingest/SQLite reproductions for
/// O11/O19 and native O14; these do not approve or implement the future projection schema.
/// </summary>
public sealed class CoordinationReliabilityTests(ITestOutputHelper output)
{
    private static readonly FixedTimeProvider Time = new(DateTimeOffset.UnixEpoch);
    private static readonly TimeSpan DeadlockTimeout = TimeSpan.FromSeconds(15);
    private const string ExternalSession = "synthetic-p2";

    [Fact]
    public void PumpOnce_RegisterAndPost_PersistsOneOriginalMessage()
    {
        using var files = new TemporaryPipeline();
        files.WriteRegisterAndPost();
        using var store = SqliteWatcherObservationStore.Open(files.DatabasePath);
        var (pump, ingest) = NewPump(store, new FakeMonotonicClock(), files.LogPath, "first");

        var applied = pump.PumpOnce();

        var message = Assert.Single(store.AllBoardMessages());
        Assert.Equal(2, applied);
        Assert.Equal(1, ingest.Stats.Registered);
        Assert.Equal(1, ingest.Stats.BoardPosts);
        Assert.Equal("first-message-1", message.MessageId);
        Assert.Equal("synthetic question", message.Content);
        Assert.Equal(1, message.Seq);
        Assert.Equal(Assert.Single(store.AllSessions()).SessionId, message.AuthorSessionId);
        output.WriteLine("PASS control: parsed=2; sessions=1; messages=1; id={0}; seq=1", message.MessageId);
    }

    [Fact]
    public void PumpOnce_UnchangedLogTwice_PreservesOneOriginalMessage()
    {
        using var files = new TemporaryPipeline();
        files.WriteRegisterAndPost();
        using var store = SqliteWatcherObservationStore.Open(files.DatabasePath);
        var (pump, ingest) = NewPump(store, new FakeMonotonicClock(), files.LogPath, "first");
        Assert.Equal(2, pump.PumpOnce());
        var original = Assert.Single(store.AllBoardMessages());

        Assert.Equal(2, pump.PumpOnce());

        var messages = store.AllBoardMessages();
        output.WriteLine("R1: expected=[{0}]; actual={1}; boardPosts={2}; duplicateRegisters={3}",
            original.MessageId, JsonSerializer.Serialize(messages), ingest.Stats.BoardPosts, ingest.Stats.DuplicateRegister);
        Assert.Equal(original, store.FindBoardMessage(original.MessageId));
        Assert.Equal(new[] { original.MessageId }, messages.Select(message => message.MessageId).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PumpOnce_ReopenedComposition_PreservesObservationsAndLifecycle(bool ended)
    {
        using var files = new TemporaryPipeline();
        files.WriteRegisterAndPost();
        files.Writer.WriteHeartbeat(ExternalSession);
        if (ended)
        {
            files.Writer.WriteSessionEnd(ExternalSession);
        }
        var eventCount = ended ? 4 : 3;
        string original;
        using (var store = SqliteWatcherObservationStore.Open(files.DatabasePath))
        {
            var (pump, _) = NewPump(store, new FakeMonotonicClock(), files.LogPath, "first");
            Assert.Equal(eventCount, pump.PumpOnce());
            Assert.Single(store.AllBoardMessages());
            Assert.Equal(ended, store.IsEnded(Assert.Single(store.AllSessions()).SessionId));
            original = Snapshot(store);
        }

        using var reopened = SqliteWatcherObservationStore.Open(files.DatabasePath);
        Assert.Equal(original, Snapshot(reopened));
        var restartedClock = new FakeMonotonicClock();
        restartedClock.Advance(TimeSpan.FromSeconds(1));
        var (restartedPump, _) = NewPump(reopened, restartedClock, files.LogPath, "restarted");
        Assert.Equal(eventCount, restartedPump.PumpOnce());

        var replayed = Snapshot(reopened);
        output.WriteLine("R2 full replay, ended={0}: expected={1}; actual={2}", ended, original, replayed);
        Assert.Equal(original, replayed);
    }

    [Fact]
    public void Apply_ReplayedRegisterOfEndedSession_PreservesEndedGenerationAndHeartbeat()
    {
        using var files = new TemporaryPipeline();
        files.Writer.WriteRegister(ExternalSession, files.RegistrationAttributes);
        files.Writer.WriteSessionEnd(ExternalSession);
        string original;
        using (var store = SqliteWatcherObservationStore.Open(files.DatabasePath))
        {
            var (pump, _) = NewPump(store, new FakeMonotonicClock(), files.LogPath, "first");
            Assert.Equal(2, pump.PumpOnce());
            Assert.True(store.IsEnded(Assert.Single(store.AllSessions()).SessionId));
            original = Snapshot(store);
        }

        using var reopened = SqliteWatcherObservationStore.Open(files.DatabasePath);
        Assert.Equal(original, Snapshot(reopened));
        var restartedClock = new FakeMonotonicClock();
        restartedClock.Advance(TimeSpan.FromSeconds(1));
        var (_, restartedIngest) = NewPump(reopened, restartedClock, files.LogPath, "restarted");
        // Isolate the registration phase: a later replayed end would hide the cleared ended mark.
        var register = Assert.IsType<ContractRegister>(CoordContractLog.ReadDirectory(files.LogPath)[0]);
        restartedIngest.Apply(register);

        var replayed = Snapshot(reopened);
        output.WriteLine("R2 register phase only: expected={0}; actual={1}", original, replayed);
        Assert.Equal(original, replayed);
    }

    [Fact]
    public void Post_TwoServicesSerially_CursorSeesSecondCommittedMessage()
    {
        using var files = new TemporaryPipeline();
        using var storeA = SqliteWatcherObservationStore.Open(files.DatabasePath);
        using var storeB = SqliteWatcherObservationStore.Open(files.DatabasePath);
        using var reader = SqliteWatcherObservationStore.Open(files.DatabasePath);
        var registrar = new TrustedRegistrar(storeA, new SequentialCapabilityFactory(), new FakeMonotonicClock(), () => "session");
        var session = registrar.Register(WatcherFixtures.Binding(repoPath: files.Root));
        var repository = session.Session.Binding.Repository.CanonicalPath;
        var forwarded = BeforeBoardInsertStore.Wrap(storeB, _ => { });
        var boardA = new MessageBoardService(storeA, registrar, Time, () => "message-a");
        var boardB = new MessageBoardService(forwarded, registrar, Time, () => "message-b");

        var first = boardA.Post(repository, session.SessionId, session.Capability, BoardMessageKind.Question, "A");
        var cursor = Assert.Single(reader.BoardMessages(repository)).Seq;
        var second = boardB.Post(repository, session.SessionId, session.Capability, BoardMessageKind.Question, "B");

        Assert.Equal(1, first.Seq);
        Assert.Equal(2, second.Seq);
        Assert.Equal(second, reader.FindBoardMessage(second.MessageId));
        Assert.Equal(second, Assert.Single(reader.BoardMessages(repository), message => message.Seq > cursor));
        Assert.Equal(2, reader.BoardMessages(repository).Count);
        output.WriteLine("PASS serial/fidelity control: A.seq=1; cursor=1; B.seq=2; committed=2; next=[message-b]");
    }

    [Fact]
    public async Task Post_TwoServicesAllocateBeforeCommit_CursorSeesLaterCommittedMessage()
    {
        using var files = new TemporaryPipeline();
        using var storeA = SqliteWatcherObservationStore.Open(files.DatabasePath);
        using var storeB = SqliteWatcherObservationStore.Open(files.DatabasePath);
        using var reader = SqliteWatcherObservationStore.Open(files.DatabasePath);
        using var releaseB = new ManualResetEventSlim();
        var allocatedB = new TaskCompletionSource<BoardMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var registrar = new TrustedRegistrar(storeA, new SequentialCapabilityFactory(), new FakeMonotonicClock(), () => "session");
        var session = registrar.Register(WatcherFixtures.Binding(repoPath: files.Root));
        var repository = session.Session.Binding.Repository.CanonicalPath;
        var heldStore = BeforeBoardInsertStore.Wrap(storeB, message =>
        {
            allocatedB.SetResult(message);
            if (!releaseB.Wait(DeadlockTimeout))
            {
                throw new TimeoutException("P2 R3: B was not released before the deadlock timeout.");
            }
        });
        var boardA = new MessageBoardService(storeA, registrar, Time, () => "message-a");
        var boardB = new MessageBoardService(heldStore, registrar, Time, () => "message-b");
        var pendingB = Task.Run(() => boardB.Post(
            repository, session.SessionId, session.Capability, BoardMessageKind.Question, "B"));
        BoardMessage reservedB;
        BoardMessage first;
        int cursor;

        try
        {
            reservedB = await allocatedB.Task.WaitAsync(DeadlockTimeout);
            Assert.Empty(reader.BoardMessages(repository));
            first = boardA.Post(repository, session.SessionId, session.Capability, BoardMessageKind.Question, "A");
            Assert.Equal(first, Assert.Single(reader.BoardMessages(repository)));
            cursor = first.Seq;
        }
        finally
        {
            releaseB.Set();
            await pendingB.WaitAsync(DeadlockTimeout);
        }
        var second = await pendingB;

        Assert.Equal(reservedB, second);
        Assert.Equal(second, reader.FindBoardMessage(second.MessageId));
        Assert.Equal(2, reader.BoardMessages(repository).Count);
        var next = reader.BoardMessages(repository).Where(message => message.Seq > cursor).ToArray();
        output.WriteLine("R3: B allocated={0}; A committed={1}; reader cursor={2}; B committed={3}; durable count=2; next={4}",
            reservedB.Seq, first.Seq, cursor, second.Seq, JsonSerializer.Serialize(next));
        Assert.Equal(new[] { second.MessageId }, next.Select(message => message.MessageId).ToArray());
    }

    private static (CoordContractLogPump Pump, InjectedContractIngest Ingest) NewPump(
        IWatcherObservationStore store, FakeMonotonicClock clock, string logPath, string prefix)
    {
        var registrar = new TrustedRegistrar(store, new SequentialCapabilityFactory(), clock, () => prefix + "-session");
        var nextId = 0;
        var board = new MessageBoardService(store, registrar, Time, () => $"{prefix}-message-{++nextId}");
        var ingest = new InjectedContractIngest(new IngestHost(store, registrar, Time, board: board));
        return (new CoordContractLogPump(logPath, ingest), ingest);
    }

    private static string Snapshot(IWatcherObservationStore store) => JsonSerializer.Serialize(new
    {
        Sessions = store.AllSessions().Select(session => new
        {
            session.SessionId,
            Generation = session.Generation.Value,
            Heartbeat = store.LastHeartbeat(session.SessionId),
            Ended = store.IsEnded(session.SessionId),
        }).ToArray(),
        Messages = store.AllBoardMessages(),
    });

    private sealed class TemporaryPipeline : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("aide-p2-red-");
        public string Root => _directory.FullName;
        public string DatabasePath => Path.Combine(Root, "watcher.db");
        public string LogPath => Path.Combine(Root, "wire");
        public CoordContractWriter Writer => new(LogPath, Time);
        public IReadOnlyDictionary<string, string?> RegistrationAttributes
        {
            get
            {
                var attributes = WatcherFixtures.HarnessRegistration().Attributes.ToDictionary(pair => pair.Key, pair => pair.Value);
                attributes[OtelAttributes.RepoPath] = Root;
                attributes[OtelAttributes.WorktreePath] = Root;
                return attributes;
            }
        }

        public void WriteRegisterAndPost()
        {
            Writer.WriteRegister(ExternalSession, RegistrationAttributes);
            Writer.WriteBoardPost(ExternalSession, "question", "synthetic question");
        }

        public void Dispose() => _directory.Delete(recursive: true);
    }

    /// <summary>
    /// Test-only scheduling decorator: holds B after allocation, before its real SQLite INSERT.
    /// All reads and writes forward unchanged. The serial test above is its persistence fidelity pair.
    /// </summary>
    public class BeforeBoardInsertStore : DispatchProxy
    {
        private IWatcherObservationStore? _inner;
        private Action<BoardMessage>? _beforeInsert;

        internal static IWatcherObservationStore Wrap(IWatcherObservationStore inner, Action<BoardMessage> beforeInsert)
        {
            var proxy = Create<IWatcherObservationStore, BeforeBoardInsertStore>();
            var scheduling = (BeforeBoardInsertStore)proxy;
            scheduling._inner = inner;
            scheduling._beforeInsert = beforeInsert;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            var inner = _inner ?? throw new InvalidOperationException("P2 scheduling store was not initialized.");
            if (nameof(IWatcherObservationStore.AppendBoardMessage) == targetMethod.Name
                && args is [BoardMessage message])
            {
                (_beforeInsert ?? throw new InvalidOperationException("P2 scheduling callback is missing."))(message);
            }

            try
            {
                return targetMethod.Invoke(inner, args);
            }
            catch (TargetInvocationException error) when (error.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }
}
