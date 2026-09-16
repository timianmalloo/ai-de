using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

/// <summary>Intentional P2.4 RED checkpoint: exercise recovery through the native writer and SQLite pump.</summary>
public sealed class CoordinationRecoveryTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private const string Child = "a-child";
    private const string Parent = "b-parent";
    private const string ParentMessageId = "301";
    private const int ActivePendingLimit = 1024;
    private const long ActivePayloadByteLimit = 16 * 1024 * 1024;

    [Fact]
    public void Pump_LateSameRepositoryParent_AppliesChildOnceWithOriginalAdmission()
    {
        using var files = new NativeRecovery();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var pump = files.Compose(store);
        files.Register(Child);
        files.Writer.WriteBoardPost(Child, "reply", "late-child", ParentMessageId);

        pump.PumpOnce();

        Assert.Empty(store.AllBoardMessages());
        var admission = files.ChildAdmission();
        files.AssertPending(admission);
        files.Register(Parent);
        files.Writer.WriteBoardPost(Parent, "question", "parent");
        files.Pump(pump, 4);

        Assert.Equal(ParentMessageId, Assert.Single(store.AllBoardMessages(), message => message.Content == "parent").MessageId);
        Assert.Equal(admission, files.ChildAdmission());
        output.WriteLine($"originalAdmission={admission}; pending={files.PendingCount()}; boardMessages={store.AllBoardMessages().Count}");
        Assert.True(files.Number("""
            SELECT COUNT(*) FROM coord_projection_event
            WHERE original_id='a-child' AND source_offset>0 AND application_state='applied';
            """) == 1, $"Late parent committed, but child remains pending at admission {admission}.");
        var child = Assert.Single(store.AllBoardMessages(), message => message.Content == "late-child");
        Assert.Equal(ParentMessageId, child.ParentMessageId);
        Assert.Equal(2, store.AllBoardMessages().Count);
        Assert.True(files.Number($"SELECT current_receipt_n FROM coord_projection_event WHERE first_receipt_n={admission};") > admission);
        Assert.Equal(1, files.Number($"SELECT COUNT(*) FROM coord_projection_feed WHERE admission_n={admission} AND application_state='applied';"));
        var receiptCount = files.Number("SELECT COUNT(*) FROM coord_projection_feed;");

        files.Pump(pump, 2);

        Assert.Equal(receiptCount, files.Number("SELECT COUNT(*) FROM coord_projection_feed;"));
        Assert.Equal(child, Assert.Single(store.AllBoardMessages(), message => message.Content == "late-child"));
    }

    [Fact]
    public void Pump_ParentAlreadyCommitted_AppliesCrossSessionChildOnce()
    {
        using var files = new NativeRecovery();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var pump = files.Compose(store);
        files.Register(Parent);
        files.Writer.WriteBoardPost(Parent, "question", "parent");
        pump.PumpOnce();
        files.Register(Child);
        files.Writer.WriteBoardPost(Child, "reply", "serial-child", ParentMessageId);

        files.Pump(pump, 3);

        var parent = Assert.Single(store.AllBoardMessages(), message => message.Content == "parent");
        var child = Assert.Single(store.AllBoardMessages(), message => message.Content == "serial-child");
        Assert.Equal(ParentMessageId, parent.MessageId);
        Assert.Equal(parent.MessageId, child.ParentMessageId);
        Assert.Equal(2, store.AllBoardMessages().Count);
        Assert.Equal(0, files.PendingCount());
        Assert.Equal(1, files.Number($"SELECT COUNT(*) FROM coord_projection_feed WHERE admission_n={files.ChildAdmission()} AND application_state='applied';"));
    }

    [Fact]
    public void Pump_ParentInAnotherRepository_NeverSatisfiesPendingChild()
    {
        using var files = new NativeRecovery();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var pump = files.Compose(store);
        files.Register(Child);
        files.Writer.WriteBoardPost(Child, "reply", "wrong-repository-child", ParentMessageId);
        pump.PumpOnce();
        var admission = files.ChildAdmission();
        files.Register(Parent, files.OtherRepository);
        files.Writer.WriteBoardPost(Parent, "question", "foreign-parent");

        files.Pump(pump, 4);

        Assert.Equal(ParentMessageId, Assert.Single(store.AllBoardMessages()).MessageId);
        Assert.Equal(admission, files.ChildAdmission());
        files.AssertPending(admission);
        Assert.Equal(1, files.PendingCount());
        Assert.Equal(0, files.Number($"SELECT COUNT(*) FROM coord_projection_feed WHERE admission_n={admission} AND application_state='applied';"));
    }

    [Fact]
    public void Pump_PermanentlyMissingParent_RetainsPayloadWithoutBoardOrLifecycleAuthority()
    {
        using var files = new NativeRecovery();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var pump = files.Compose(store);
        files.Register(Child);
        files.Writer.WriteBoardPost(Child, "reply", "missing-parent-child", ParentMessageId);
        files.Writer.Write("episode-open", Child, new Dictionary<string, string?>
        {
            [CoordContract.EpisodeAttributes.Goal] = "Observe only",
            [CoordContract.EpisodeAttributes.DoneWhen] = "No run permission",
        });

        files.Pump(pump, 5);

        Assert.Empty(store.AllBoardMessages());
        Assert.Equal(2, files.PendingCount());
        Assert.Equal(2, files.Number("""
            SELECT COUNT(*) FROM coord_projection_event
            WHERE application_state='pending' AND raw_bytes IS NOT NULL AND canonical_bytes IS NOT NULL;
            """));
        Assert.Equal(1, files.Number("""
            SELECT COUNT(*) FROM coord_projection_event e JOIN coord_projection_feed f ON f.n=e.current_receipt_n
            WHERE f.reason='TRUSTED_LIFECYCLE_REQUIRED' AND e.application_state='pending';
            """));
        Assert.Equal(0, files.Number("""
            SELECT COUNT(*) FROM coord_projection_feed
            WHERE application_state='applied' AND reason<>'OBSERVED_REGISTER';
            """));
    }

    [Fact]
    public void Pump_PendingLimitPlusOne_AccountsForParentWithoutExceedingActiveBoundsOrLosingChildren()
    {
        using var files = new NativeRecovery();
        using var store = SqliteWatcherObservationStore.Open(files.Database);
        var pump = files.Compose(store);
        var children = ActivePendingLimit + 1;
        files.Register(Child);
        files.WriteSyntheticChildren(children);
        pump.PumpOnce();
        Assert.Empty(store.AllBoardMessages());
        files.Register(Parent);
        files.Writer.WriteBoardPost(Parent, "question", "capacity-parent");

        // Twenty finite turns cover 1,025 children even with a future 64-attempt recovery turn.
        files.Pump(pump, (children + 63) / 64 + 3);

        Assert.Equal(ParentMessageId, Assert.Single(store.AllBoardMessages(), message => message.Content == "capacity-parent").MessageId);
        Assert.Equal(children, files.Number("""
            SELECT COUNT(*) FROM coord_projection_event
            WHERE original_id='a-child' AND source_offset>0 AND application_state IN ('pending','applied');
            """));
        var pending = files.PendingCount();
        var bytes = files.Number("""
            SELECT COALESCE(SUM(length(raw_bytes)+length(canonical_bytes)),0)
            FROM coord_projection_event WHERE application_state='pending';
            """);
        output.WriteLine($"children={children}; activePending={pending}; retainedBytes={bytes}; parentVisible=true; turns=20");
        Assert.True(bytes <= ActivePayloadByteLimit, $"Active retained bytes {bytes} exceed {ActivePayloadByteLimit}.");
        Assert.True(pending <= ActivePendingLimit, $"Active pending count {pending} exceeds {ActivePendingLimit}; retained bytes={bytes}.");
        Assert.Equal(children, store.AllBoardMessages().Count(message => message.ParentMessageId == ParentMessageId));
        Assert.Equal(0, files.PendingCount());
    }

    private sealed class NativeRecovery : IDisposable
    {
        private readonly FixedTimeProvider _time = new(DateTimeOffset.UnixEpoch);
        private int _messageId = 300;
        private string Root { get; } = Path.Combine(Environment.CurrentDirectory, "p24-recovery-" + Guid.NewGuid().ToString("N"));
        public string Database => Path.Combine(Root, "watcher.db");
        private string Logs => Path.Combine(Root, "wire");
        public string OtherRepository => Path.Combine(Root, "other-repository");
        public CoordContractWriter Writer => new(Logs, _time);

        public NativeRecovery()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(OtherRepository);
        }

        public void Register(string session, string? repository = null)
        {
            var attributes = WatcherFixtures.HarnessRegistration().Attributes.ToDictionary(pair => pair.Key, pair => pair.Value);
            attributes[OtelAttributes.RepoPath] = repository ?? Root;
            attributes[OtelAttributes.WorktreePath] = repository ?? Root;
            Writer.WriteRegister(session, attributes);
        }

        public CoordContractLogPump Compose(SqliteWatcherObservationStore store)
        {
            var registrar = new TrustedRegistrar(store, new SequentialCapabilityFactory(), new FakeMonotonicClock());
            var board = new MessageBoardService(store, registrar, _time,
                () => (++_messageId).ToString(CultureInfo.InvariantCulture));
            var ingest = new InjectedContractIngest(new IngestHost(store, registrar, _time, board: board));
            return new(Logs, ingest);
        }

        public void Pump(CoordContractLogPump pump, int turns)
        {
            for (var remaining = turns; remaining > 0; remaining--)
            {
                pump.PumpOnce();
                Assert.Null(pump.LastRun.Diagnostic);
            }
        }

        public void WriteSyntheticChildren(int count)
        {
            Writer.WriteBoardPost(Child, "reply", "capacity-child-0", ParentMessageId);
            var file = Path.Combine(Logs, CoordContractWriter.FileNameFor(Child));
            var template = File.ReadAllLines(file)[1];
            var extra = new StringBuilder();
            // Consumer-only bulk fixture derives its wire shape from a real writer record, not a second serializer.
            for (var index = 1; index < count; index++)
            {
                var record = JsonNode.Parse(template)!;
                record["seq"] = index + 2;
                record["attrs"]![CoordContract.BoardAttributes.Content] = $"capacity-child-{index}";
                extra.Append(record.ToJsonString()).Append('\n');
            }
            File.AppendAllText(file, extra.ToString(), new UTF8Encoding(false));
        }

        public long ChildAdmission() => Number("""
            SELECT first_receipt_n FROM coord_projection_event WHERE original_id='a-child' AND source_offset>0;
            """);

        public long PendingCount() => Number("SELECT COUNT(*) FROM coord_projection_event WHERE application_state='pending';");

        public void AssertPending(long admission)
        {
            Assert.Equal(1, Number($"""
                SELECT COUNT(*) FROM coord_projection_event
                WHERE first_receipt_n={admission} AND current_receipt_n={admission}
                  AND application_state='pending' AND raw_bytes IS NOT NULL AND canonical_bytes IS NOT NULL;
                """));
        }

        public long Number(string sql)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Database, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (long)command.ExecuteScalar()!;
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
