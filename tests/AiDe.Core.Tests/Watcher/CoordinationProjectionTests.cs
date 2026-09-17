using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

/// <summary>Actual constructor migration and raw SQL attacks on the P2 receipt aggregate.</summary>
public sealed class CoordinationProjectionTests : IDisposable
{
    private readonly string _root = Path.Combine(Environment.CurrentDirectory, "p22-" + Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_root, "watcher.db");

    public CoordinationProjectionTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Constructor_InstallsThreeCachesAndVersionTen()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Assert.Equal(10L, Scalar(connection, "SELECT MAX(version) FROM watcher_schema_version;"));
        Assert.Equal(3L, Scalar(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'coord_projection_%';"));
    }

    [Fact]
    public void InitialReceiptAndEvent_MustCommitTogether()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        using (var transaction = connection.BeginTransaction(deferred: false))
        {
            Execute(connection, Initial, transaction);
            Assert.Throws<SqliteException>(() => transaction.Commit());
        }
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_feed;"));
        using (var transaction = connection.BeginTransaction(deferred: false))
        {
            Execute(connection, Event, transaction);
            Assert.Throws<SqliteException>(() => transaction.Commit());
        }
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_event;"));
        Admit(connection);
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_event;"));
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_feed;"));
    }

    [Theory]
    [InlineData("UPDATE coord_projection_feed SET reason='changed';")]
    [InlineData("DELETE FROM coord_projection_feed;")]
    [InlineData("INSERT OR REPLACE INTO coord_projection_feed SELECT * FROM coord_projection_feed;")]
    [InlineData("DELETE FROM coord_projection_event;")]
    [InlineData("INSERT OR REPLACE INTO coord_projection_event SELECT * FROM coord_projection_event;")]
    [InlineData("UPDATE coord_projection_event SET raw_bytes=X'02';")]
    [InlineData("UPDATE coord_projection_event SET raw_digest='changed';")]
    [InlineData("UPDATE coord_projection_event SET canonical_version='changed';")]
    [InlineData("UPDATE coord_projection_event SET original_id='changed';")]
    [InlineData("UPDATE coord_projection_event SET first_receipt_n=2;")]
    [InlineData("UPDATE coord_projection_event SET current_receipt_n=0;")]
    [InlineData("UPDATE coord_projection_event SET application_state='pending';")]
    public void ImmutableFacts_RejectMutation(string mutation)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection);
        Assert.Throws<SqliteException>(() => Execute(connection, mutation));
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_feed;"));
        Assert.Equal(1L, Scalar(connection, "SELECT current_receipt_n FROM coord_projection_event;"));
    }

    [Theory]
    [InlineData("INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,outcome,application_state) VALUES('scope','epoch','event',1,'applied','applied');")]
    [InlineData("INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,application_state) VALUES('scope','epoch','event',0,1,'pending','pending');")]
    [InlineData("INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,application_state,message_id) VALUES('scope','epoch','event',0,1,'applied','applied','remapped');")]
    [InlineData("INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,application_state) VALUES('scope','epoch','event',0,99,'applied','applied');")]
    public void Transition_RejectsDuplicateInitialRegressionAndRemapping(string mutation)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection);
        Assert.Throws<SqliteException>(() => Execute(connection, mutation));
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_feed;"));
    }

    [Fact]
    public void Tombstone_ClearsPayloadAndCannotResurrect()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection);
        Execute(connection, """
            BEGIN IMMEDIATE;
            INSERT INTO coord_projection_feed
                (scope,epoch,event_key,is_initial,admission_n,outcome,application_state,payload_presence)
            VALUES('scope','epoch','event',0,1,'tombstone','applied',0);
            UPDATE coord_projection_event SET payload_presence=0,raw_bytes=NULL,canonical_bytes=NULL;
            COMMIT;
            """);
        Assert.Equal(2L, Scalar(connection, "SELECT current_receipt_n FROM coord_projection_event;"));
        Assert.Equal(0L, Scalar(connection,
            "SELECT COUNT(*) FROM coord_projection_event WHERE raw_bytes IS NOT NULL OR canonical_bytes IS NOT NULL;"));
        Assert.Throws<SqliteException>(() => Execute(connection, """
            INSERT INTO coord_projection_feed
                (scope,epoch,event_key,is_initial,admission_n,outcome,application_state)
            VALUES('scope','epoch','event',0,1,'applied','applied');
            """));
        Assert.Throws<SqliteException>(() => Execute(connection,
            "UPDATE coord_projection_event SET raw_bytes=X'01';"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("pending")]
    public void AppliedChild_RequiresActuallyAppliedParent(string? parentState)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        if (parentState is not null)
        {
            using var parent = connection.BeginTransaction(deferred: false);
            Execute(connection, Pending(Initial), parent);
            Execute(connection, Pending(Event), parent);
            parent.Commit();
        }
        using (var transaction = connection.BeginTransaction(deferred: false))
        {
            var offset = parentState is null ? 0 : 1;
            Execute(connection, $$"""
                INSERT INTO coord_projection_feed
                    (scope,epoch,event_key,is_initial,outcome,application_state,parent_event_key)
                VALUES('scope','epoch','child',1,'applied','applied','event');
                INSERT INTO coord_projection_event
                    (scope,epoch,event_key,source_offset,source_end,raw_bytes,raw_digest,
                     canonical_version,canonical_bytes,first_receipt_n,current_receipt_n,application_state)
                VALUES('scope','epoch','child',{{offset}},{{offset + 1}},X'02','digest2','legacy-raw/1',X'02',
                       last_insert_rowid(),last_insert_rowid(),'applied');
                """, transaction);
            var error = Assert.Throws<SqliteException>(() => transaction.Commit());
            Assert.Equal(787, error.SqliteExtendedErrorCode);
        }
        Assert.Equal(parentState is null ? 0L : 1L,
            Scalar(connection, "SELECT COUNT(*) FROM coord_projection_event;"));
    }

    [Fact]
    public void Checkpoint_RequiresAccountedPrefixAndCannotMoveBackward()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Assert.Throws<SqliteException>(() => Execute(connection, """
            INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest)
            VALUES('scope','epoch',1,'prefix');
            """));
        Admit(connection);
        Execute(connection, """
            INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest)
            VALUES('scope','epoch',1,'prefix');
            """);
        Assert.Throws<SqliteException>(() => Execute(connection,
            "UPDATE coord_projection_checkpoint SET accepted_offset=0;"));
        Assert.Throws<SqliteException>(() => Execute(connection,
            "UPDATE coord_projection_checkpoint SET prefix_digest='changed';"));
        Assert.Throws<SqliteException>(() => Execute(connection,
            "INSERT OR REPLACE INTO coord_projection_checkpoint SELECT * FROM coord_projection_checkpoint;"));
    }

    [Fact]
    public void Constructor_V7FixturePreservesLegacyIdsDuplicateSequencesAndWritableTables()
    {
        using (var original = SqliteWatcherObservationStore.Open(DatabasePath))
        {
            original.AppendBoardMessage(Message("legacy-a"));
            original.AppendBoardMessage(Message("legacy-b"));
        }
        using (var connection = Connect())
        {
            // Synthetic v7 fixture: remove all later empty tables, including the v9 additions.
            Execute(connection, """
                PRAGMA foreign_keys=OFF;
                DROP TABLE coord_projection_feed;
                DROP TABLE coord_projection_event;
                DROP TABLE coord_projection_checkpoint;
                DROP TABLE registration_notice_delivery;
                DROP TABLE native_registration_admission_fact;
                DELETE FROM watcher_schema_version;
                INSERT INTO watcher_schema_version(version,applied_at) VALUES(7,'1970-01-01T00:00:00Z');
                """);
        }
        using (var migrated = SqliteWatcherObservationStore.Open(DatabasePath))
        {
            Assert.Equal(new[] { "legacy-a", "legacy-b" },
                migrated.AllBoardMessages().Select(message => message.MessageId).Order().ToArray());
            Assert.All(migrated.AllBoardMessages(), message => Assert.Equal(7, message.Seq));
            migrated.AppendBoardMessage(Message("legacy-c"));
            var allocated = migrated.AppendBoardMessageAllocated(Message("new"));
            Assert.Equal(8, allocated.Seq);
            Assert.Equal(4, migrated.AllBoardMessages().Count);
        }
        using var reopened = SqliteWatcherObservationStore.Open(DatabasePath);
        using var raw = Connect();
        Assert.Equal(1L, Scalar(raw, "SELECT COUNT(*) FROM watcher_schema_version WHERE version=8;"));
        Assert.Equal(4, reopened.AllBoardMessages().Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CacheTransaction_FailsAtEachWriteBoundary_RollsBackWholeAggregate(int boundary)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        using (var transaction = connection.BeginTransaction(deferred: false))
        {
            Execute(connection, Initial, transaction);
            if (boundary >= 2)
            {
                Execute(connection, Event, transaction);
            }
            if (boundary >= 3)
            {
                Execute(connection, """
                    INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest)
                    VALUES('scope','epoch',1,'prefix');
                    """, transaction);
            }
            Assert.Throws<SqliteException>(() => Execute(connection,
                "INSERT INTO watcher_schema_version(version,applied_at) VALUES(NULL,NULL);", transaction));
            transaction.Rollback();
        }
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_feed;"));
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_event;"));
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_checkpoint;"));
    }

    [Fact]
    public void Transition_PendingToApplied_PreservesOriginalAdmissionAndAdvancesPointer()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        using (var transaction = connection.BeginTransaction(deferred: false))
        {
            Execute(connection, Pending(Initial), transaction);
            Execute(connection, Pending(Event), transaction);
            transaction.Commit();
        }
        Execute(connection, """
            BEGIN IMMEDIATE;
            INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,application_state)
            VALUES('scope','epoch','event',0,1,'applied','applied');
            UPDATE coord_projection_event SET application_state='applied',recovery_status='none';
            COMMIT;
            """);
        Assert.Equal(1L, Scalar(connection, "SELECT first_receipt_n FROM coord_projection_event;"));
        Assert.Equal(2L, Scalar(connection, "SELECT current_receipt_n FROM coord_projection_event;"));
        Assert.Equal(1L, Scalar(connection,
            "SELECT COUNT(*) FROM coord_projection_feed WHERE n=1 AND outcome='pending';"));
        Assert.Equal(1L, Scalar(connection,
            "SELECT COUNT(*) FROM coord_projection_event e JOIN coord_projection_feed f ON f.n=e.current_receipt_n WHERE f.outcome='applied';"));
    }

    [Theory]
    [InlineData("scope", "epoch", "pending")]
    [InlineData("other", "epoch", "applied")]
    [InlineData("scope", "other", "applied")]
    public void InitialCompositeForeignKeys_RejectBorrowedReceiptOrDifferentState(string scope, string epoch, string state)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        using var transaction = connection.BeginTransaction(deferred: false);
        Execute(connection, Initial, transaction);
        Execute(connection, Event.Replace("'scope'", $"'{scope}'").Replace("'epoch'", $"'{epoch}'")
            .Replace("'applied'", $"'{state}'")
            .Replace("'none'", state == "pending" ? "'active'" : "'none'"), transaction);
        var error = Assert.Throws<SqliteException>(() => transaction.Commit());
        Assert.Equal(787, error.SqliteExtendedErrorCode);
    }

    [Fact]
    public void SourcePosition_ChangedBytesOrNewEpochCannotReplaceOccurrence()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection);
        Assert.Throws<SqliteException>(() => Execute(connection, Event
            .Replace("'event'", "'different'").Replace("X'01'", "X'02'")));
        Assert.Throws<SqliteException>(() => Execute(connection, Event.Replace("'epoch'", "'new-epoch'")));
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_event;"));
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_feed;"));
    }

    [Fact]
    public void ReceiptMutation_WithoutUpdateTrigger_OracleDetectsChangedFact()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection);
        const string mutation = "UPDATE coord_projection_feed SET reason='changed';";
        Assert.Throws<SqliteException>(() => Execute(connection, mutation));
        Execute(connection, "DROP TRIGGER coord_feed_update;");
        Execute(connection, mutation);
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM coord_projection_feed WHERE reason='changed';"));
    }

    private static BoardMessage Message(string id) => new(
        id, "synthetic-repository", BoardMessageKind.Question, "synthetic-session",
        TrustClassification.Asserted, null, "synthetic", true, false, false, DateTimeOffset.UnixEpoch, 7);

    private const string Initial = """
        INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,outcome,application_state,recovery_status)
        VALUES('scope','epoch','event',1,'applied','applied','none');
        """;

    private const string Event = """
        INSERT INTO coord_projection_event
            (scope,epoch,event_key,source_offset,source_end,raw_bytes,raw_digest,
             canonical_version,canonical_bytes,first_receipt_n,current_receipt_n,application_state,recovery_status)
        VALUES('scope','epoch','event',0,1,X'01','digest','legacy-raw/1',X'01',1,1,'applied','none');
        """;

    private static string Pending(string sql) =>
        sql.Replace("'applied'", "'pending'").Replace("'none'", "'active'");

    private static void Admit(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction(deferred: false);
        Execute(connection, Initial, transaction);
        Execute(connection, Event, transaction);
        transaction.Commit();
    }

    private SqliteConnection Connect()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath, Pooling = false,
        }.ToString());
        connection.Open();
        Execute(connection, "PRAGMA foreign_keys=ON; PRAGMA recursive_triggers=ON;");
        return connection;
    }

    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
