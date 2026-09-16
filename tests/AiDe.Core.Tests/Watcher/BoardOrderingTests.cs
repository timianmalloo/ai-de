using AiDe.Core.Watcher;
using AiDe.Mcp;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

/// <summary>P2.1 native ordering and MCP cursor oracles; no claim about historical sequence ties.</summary>
public sealed class BoardOrderingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Post_SequenceHolesAndHistoricalTies_ReturnsMaximumPlusOne(bool sqlite)
    {
        using var fixture = new BoardFixture(sqlite);
        var first = fixture.Seed("old-a", 7);
        var tied = fixture.Seed("old-b", 7);
        fixture.Store.AppendBoardMessage(first with { MessageId = "other", RepositoryKey = "other", Seq = 500 });
        fixture.Store.RedactBoardMessage(first.MessageId);

        var posted = fixture.Post("new");

        Assert.Equal(8, posted.Seq);
        Assert.Equal(posted, fixture.Store.FindBoardMessage(posted.MessageId));
        Assert.Equal(tied, fixture.Store.FindBoardMessage(tied.MessageId));
        Assert.Equal(new[] { 7, 7, 8 }, fixture.Store.BoardMessages(fixture.Repository).Select(m => m.Seq));
        Assert.True(fixture.Store.FindBoardMessage(first.MessageId)!.Tombstoned);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Post_MaximumSequence_RefusesWithoutWriting(bool sqlite)
    {
        using var fixture = new BoardFixture(sqlite);
        var original = fixture.Seed("maximum", int.MaxValue);

        Assert.Throws<OverflowException>(() => fixture.Post("overflow"));

        Assert.Equal(original, Assert.Single(fixture.Store.BoardMessages(fixture.Repository)));
        Assert.Null(fixture.Store.FindBoardMessage("overflow"));
    }

    [Fact]
    public void Post_SqliteInt64Maximum_RefusesWithoutWrappingOrWriting()
    {
        using var fixture = new BoardFixture(sqlite: true);
        using var connection = new SqliteConnection($"Data Source={fixture.DatabasePath};Pooling=False");
        connection.Open();
        fixture.Seed("maximum", 1);
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE board_message_fact SET seq=$maximum WHERE message_id='maximum';";
        command.Parameters.AddWithValue("$maximum", long.MaxValue);
        command.ExecuteNonQuery();

        Assert.Throws<OverflowException>(() => fixture.Post("overflow"));

        command.CommandText = "SELECT COUNT(*) FROM board_message_fact;";
        Assert.Equal(1L, command.ExecuteScalar());
        command.CommandText = "SELECT seq FROM board_message_fact WHERE message_id='maximum';";
        Assert.Equal(long.MaxValue, command.ExecuteScalar());
        Assert.Null(fixture.Store.FindBoardMessage("overflow"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Post_DuplicateIdentity_DoesNotReplaceCommittedFact(bool sqlite)
    {
        using var fixture = new BoardFixture(sqlite);
        var original = fixture.Post("same");

        Assert.NotNull(Record.Exception(() => fixture.Post("same")));

        Assert.Equal(original, Assert.Single(fixture.Store.BoardMessages(fixture.Repository)));
        Assert.Equal(2, fixture.Post("next").Seq);
    }

    [Fact]
    public async Task Post_TwoServicesSharingMemory_CursorSeesLaterCommittedFact()
    {
        using var fixture = new BoardFixture(sqlite: false);
        using var release = new ManualResetEventSlim();
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = CoordinationReliabilityTests.BeforeBoardInsertStore.Wrap(fixture.Store, _ =>
        {
            arrived.SetResult();
            if (!release.Wait(TimeSpan.FromSeconds(15)))
            {
                throw new TimeoutException("P2.1 memory writer was not released.");
            }
        });
        var secondBoard = fixture.Board(held, "second");
        var pending = Task.Run(() => secondBoard.Post(fixture.Repository, fixture.Session.SessionId,
            fixture.Session.Capability, BoardMessageKind.Question, "second"));
        int cursor;
        try
        {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(15));
            cursor = fixture.Post("first").Seq;
            Assert.Equal(1, Assert.Single(fixture.Store.BoardMessages(fixture.Repository)).Seq);
        }
        finally
        {
            release.Set();
            await pending.WaitAsync(TimeSpan.FromSeconds(15));
        }

        var second = await pending;
        Assert.Equal(2, second.Seq);
        Assert.Equal(second, Assert.Single(fixture.Store.BoardMessages(fixture.Repository), m => m.Seq > cursor));
    }

    [Fact]
    public void Read_MoreThanTwoHundredCommittedMessages_ConsumesEarliestPagesWithoutLoss()
    {
        using var fixture = new BoardFixture(sqlite: true);
        for (var seq = 1; seq <= 205; seq++)
        {
            fixture.Seed($"message-{seq}", seq);
        }
        using var reader = SqliteWatcherObservationStore.OpenReadOnly(fixture.DatabasePath);

        var first = BoardTools.Read(reader, fixture.Session.Session, limit: 200, sinceSeq: 0);
        var second = BoardTools.Read(reader, fixture.Session.Session, limit: 200, sinceSeq: first.Entries[^1].Seq);
        var end = BoardTools.Read(reader, fixture.Session.Session, limit: 200, sinceSeq: second.Entries[^1].Seq);

        Assert.Null(first.Unavailable);
        Assert.Equal(200, first.Entries.Count);
        Assert.Equal(5, second.Entries.Count);
        Assert.Empty(end.Entries);
        Assert.Equal(Enumerable.Range(1, 205), first.Entries.Concat(second.Entries).Select(e => e.Seq));
        Assert.Equal(Enumerable.Range(1, 205).Select(n => $"message-{n}"),
            first.Entries.Concat(second.Entries).Select(e => e.MessageId));
        Assert.Equal(205, first.TotalInRepository);
        Assert.Equal(5, second.TotalInRepository);
    }

    [Fact]
    public void Read_WithoutCursor_PreservesRecentPage()
    {
        using var fixture = new BoardFixture(sqlite: false);
        for (var seq = 1; seq <= 55; seq++)
        {
            fixture.Seed($"message-{seq}", seq);
        }

        var page = BoardTools.Read(fixture.Store, fixture.Session.Session);

        Assert.Equal(Enumerable.Range(6, 50), page.Entries.Select(e => e.Seq));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(201, 200)]
    public void Read_CursorLimitBoundary_ClampsEarliestPage(int limit, int count)
    {
        using var fixture = new BoardFixture(sqlite: false);
        for (var seq = 1; seq <= 205; seq++)
        {
            fixture.Seed($"message-{seq}", seq);
        }

        var page = BoardTools.Read(fixture.Store, fixture.Session.Session, limit, sinceSeq: 0);

        Assert.Equal(Enumerable.Range(1, count), page.Entries.Select(e => e.Seq));
    }

    private sealed class BoardFixture : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateDirectory(
            Path.Combine(Environment.CurrentDirectory, "aide-p2-ordering-" + Guid.NewGuid().ToString("N")));
        private readonly TrustedRegistrar _registrar;
        public string DatabasePath => Path.Combine(_directory.FullName, "watcher.db");
        public IWatcherObservationStore Store { get; }
        public RegisteredSession Session { get; }
        public string Repository => Session.Session.Binding.Repository.CanonicalPath;

        public BoardFixture(bool sqlite)
        {
            Store = sqlite ? SqliteWatcherObservationStore.Open(DatabasePath) : new InMemoryWatcherObservationStore();
            _registrar = new TrustedRegistrar(Store, new SequentialCapabilityFactory(), new FakeMonotonicClock(), () => "session");
            Session = _registrar.Register(WatcherFixtures.Binding(repoPath: _directory.FullName));
        }

        public BoardMessage Seed(string id, int seq)
        {
            var message = new BoardMessage(id, Repository, BoardMessageKind.Question, Session.SessionId,
                TrustClassification.Verified, null, "seed", true, false, false, DateTimeOffset.UnixEpoch, seq);
            Store.AppendBoardMessage(message);
            return message;
        }

        public MessageBoardService Board(IWatcherObservationStore store, string id) =>
            new(store, _registrar, new FixedTimeProvider(DateTimeOffset.UnixEpoch), () => id);

        public BoardMessage Post(string id) =>
            Board(Store, id).Post(Repository, Session.SessionId, Session.Capability, BoardMessageKind.Question, id);

        public void Dispose()
        {
            (Store as IDisposable)?.Dispose();
            _directory.Delete(recursive: true);
        }
    }
}
