using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

/// <summary>SQL storage/receipt proofs only; not a canonical-source classifier or pump.</summary>
public sealed class CoordinationProjectionBoundaryTests : IDisposable
{
    private readonly string _root = Path.Combine(Environment.CurrentDirectory, "p22-boundary-" + Guid.NewGuid().ToString("N"));
    private readonly ITestOutputHelper _output;
    private string DatabasePath => Path.Combine(_root, "watcher.db");

    public CoordinationProjectionBoundaryTests(ITestOutputHelper output)
    {
        _output = output;
        Directory.CreateDirectory(_root);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Feed_ExplicitBackdateBehindGlobalCursor_Rejects(bool transition)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0, 1);
        Admit(connection, "L", 1, 10);

        var error = Assert.Throws<SqliteException>(() =>
        {
            if (transition)
            {
                Execute(connection, """
                    INSERT INTO coord_projection_feed(n,scope,epoch,event_key,is_initial,admission_n,outcome,application_state,payload_presence)
                    VALUES(5,'scope','epoch','K',0,1,'tombstone','applied',0);
                    """);
            }
            else
            {
                Admit(connection, "M", 2, 5);
            }
        });
        Assert.Contains("COORD_FEED_ORDER", error.Message);
        Assert.Equal(10L, Number(connection, "SELECT MAX(n) FROM coord_projection_feed;"));
        Assert.Equal(1L, Number(connection, "SELECT current_receipt_n FROM coord_projection_event WHERE event_key='K';"));
        Admit(connection, "N", 2);
        Assert.Equal(11L, Number(connection, "SELECT MAX(n) FROM coord_projection_feed;"));
    }

    [Theory]
    [InlineData("coord_projection_event", "source_offset=1.5,source_end=2.5,raw_bytes=NULL")]
    [InlineData("coord_projection_event", "source_offset='nonsense',source_end='zzzz',raw_bytes=NULL")]
    [InlineData("coord_projection_event", "source_end='oops',raw_bytes=X''")]
    [InlineData("coord_projection_event", "source_end=1.5,raw_bytes=NULL")]
    [InlineData("coord_projection_event", "first_receipt_n='nonsense',current_receipt_n='nonsense'")]
    [InlineData("coord_projection_event", "first_receipt_n=1.5,current_receipt_n=1.5")]
    [InlineData("coord_projection_event", "current_receipt_n='nonsense'")]
    [InlineData("coord_projection_event", "current_receipt_n=1.5")]
    [InlineData("coord_projection_event", "first_kind=1.5")]
    [InlineData("coord_projection_event", "source_offset=-1")]
    [InlineData("coord_projection_event", "source_end=0")]
    [InlineData("coord_projection_event", "first_receipt_n=0")]
    [InlineData("coord_projection_event", "current_receipt_n=0")]
    [InlineData("coord_projection_event", "source_offset=NULL")]
    [InlineData("coord_projection_event", "source_end=NULL")]
    [InlineData("coord_projection_event", "first_receipt_n=NULL")]
    [InlineData("coord_projection_event", "current_receipt_n=NULL")]
    [InlineData("coord_projection_event", "first_kind=NULL")]
    [InlineData("coord_projection_feed", "session_id='session',session_generation='nonsense'")]
    [InlineData("coord_projection_feed", "session_id='session',session_generation=1.5")]
    [InlineData("coord_projection_feed", "session_id='session',session_generation=0")]
    [InlineData("coord_projection_feed", "session_id='session',session_generation=-1")]
    [InlineData("coord_projection_feed", "session_id='session',session_generation=NULL")]
    [InlineData("coord_projection_feed", "is_initial=NULL")]
    [InlineData("coord_projection_feed", "is_initial=1.5")]
    [InlineData("coord_projection_feed", "n=1.5")]
    [InlineData("coord_projection_checkpoint", "accepted_offset='nonsense'")]
    [InlineData("coord_projection_checkpoint", "accepted_offset=1.5")]
    [InlineData("coord_projection_checkpoint", "accepted_offset=0")]
    [InlineData("coord_projection_checkpoint", "accepted_offset=-1")]
    [InlineData("coord_projection_checkpoint", "accepted_offset=NULL")]
    public void Storage_InvalidNumericDomain_Rejects(string table, string assignments)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0);
        Checkpoint(connection, 1);
        // Isolate CHECK/NOT NULL storage rules from immutable-pointer and deferred-FK guards.
        Execute(connection, """
            PRAGMA foreign_keys=OFF;
            DROP TRIGGER coord_event_update;
            DROP TRIGGER coord_feed_update;
            DROP TRIGGER coord_checkpoint_update;
            """);
        var error = Assert.Throws<SqliteException>(() => Execute(connection, $"UPDATE {table} SET {assignments};"));
        Assert.Contains(error.SqliteErrorCode, new[] { 19, 20 });
    }

    [Theory]
    [InlineData("scope", 4096)]
    [InlineData("epoch", 128)]
    [InlineData("event_key", 512)]
    public void Storage_Keys_RequireExactTextAndUtf8ByteCeiling(string column, int ceiling)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0);
        Checkpoint(connection, 1);
        Execute(connection, """
            PRAGMA foreign_keys=OFF;
            DROP TRIGGER coord_event_update;
            DROP TRIGGER coord_feed_update;
            DROP TRIGGER coord_checkpoint_update;
            """);
        foreach (var table in new[] { "coord_projection_event", "coord_projection_feed", "coord_projection_checkpoint" })
        {
            if (column == "event_key" && table.EndsWith("checkpoint", StringComparison.Ordinal))
            {
                continue;
            }
            var valid = new string('Ω', ceiling / 2);
            using var command = connection.CreateCommand();
            command.CommandText = $"UPDATE {table} SET {column}=$value;";
            var parameter = command.Parameters.AddWithValue("$value", valid);
            Assert.Equal(1, command.ExecuteNonQuery());
            Assert.Equal(ceiling, Number(connection, $"SELECT length(CAST({column} AS BLOB)) FROM {table};"));
            foreach (var invalid in new object[] { valid + "a", "K\0hidden", new byte[] { 75 }, "" })
            {
                parameter.Value = invalid;
                Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
            }
        }
    }

    [Fact]
    public void Occurrence_DuplicateThenReplayThenNewAdmission_AccountsWithoutSemanticEffect()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0);
        Checkpoint(connection, 1);
        using (var transaction = connection.BeginTransaction(deferred: false))
        {
            Diagnostic(connection, "K", 1, 1, 2, transaction);
            Execute(connection, "UPDATE coord_projection_checkpoint SET accepted_offset=2;", transaction);
            transaction.Commit();
        }
        Assert.Equal(1L, Number(connection, "SELECT COUNT(*) FROM coord_projection_event;"));
        Assert.Equal(1L, Number(connection, "SELECT COUNT(*) FROM coord_projection_feed WHERE is_initial=1;"));
        Assert.Equal(1L, Number(connection, "SELECT current_receipt_n FROM coord_projection_event;"));
        Assert.Equal(1L, Number(connection, "SELECT COUNT(*) FROM coord_projection_feed WHERE outcome='duplicate-occurrence-accounted';"));
        Assert.Equal(2L, Number(connection, "SELECT accepted_offset FROM coord_projection_checkpoint;"));
        // SQL rejects double accounting. A future writer must detect replay and return without INSERT.
        Assert.Throws<SqliteException>(() => Diagnostic(connection, "K", 1, 1, 2));
        Assert.Equal(2L, Number(connection, "SELECT COUNT(*) FROM coord_projection_feed;"));
        Admit(connection, "L", 2);
        Execute(connection, "UPDATE coord_projection_checkpoint SET accepted_offset=3;");
        Assert.Equal(3L, Number(connection, "SELECT accepted_offset FROM coord_projection_checkpoint;"));
        Assert.Throws<SqliteException>(() => Execute(connection, """
            UPDATE coord_projection_event SET current_receipt_n=2 WHERE event_key='K';
            """));
    }

    [Theory]
    [InlineData(0, 1, "scope", "epoch", "K", 1)]
    [InlineData(2, 3, "scope", "epoch", "K", 1)]
    [InlineData(0, 1, "other", "epoch", "K", 1)]
    [InlineData(0, 1, "scope", "other", "K", 1)]
    [InlineData(1, 2, "scope", "epoch", "other", 1)]
    [InlineData(1, 2, "scope", "epoch", "K", 99)]
    public void Occurrence_InvalidBoundaryOrAdmission_Rejects(int start, int end, string scope, string epoch, string key, int admission)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0);
        Assert.Throws<SqliteException>(() =>
        {
            using var tx = connection.BeginTransaction(deferred: false);
            Diagnostic(connection, key, admission, start, end, tx, scope, epoch);
            tx.Commit();
        });
        Assert.Equal(1L, Number(connection, "SELECT COUNT(*) FROM coord_projection_feed;"));
    }

    [Fact]
    public void Checkpoint_StaleButPreviouslyAccountedEndpoint_Rejects()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0);
        Admit(connection, "L", 1);
        Assert.Throws<SqliteException>(() => Checkpoint(connection, 1));
        Checkpoint(connection, 2);
        Admit(connection, "M", 2);
        Admit(connection, "N", 3);
        Assert.Throws<SqliteException>(() => Execute(connection, "UPDATE coord_projection_checkpoint SET accepted_offset=3;"));
        Execute(connection, "UPDATE coord_projection_checkpoint SET accepted_offset=4;");
    }

    [Theory]
    [InlineData("raw_bytes=X'02'")]
    [InlineData("canonical_bytes=X'02'")]
    [InlineData("raw_bytes=NULL")]
    [InlineData("canonical_bytes=NULL")]
    public void Payload_ValidNextReceipt_IsolatesGuardAndAdmitsOnlyClauseMutant(string change)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0, state: "pending");
        Execute(connection, """
            BEGIN IMMEDIATE;
            DROP TRIGGER coord_feed_transition;
            INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,application_state)
            VALUES('scope','epoch','K',0,1,'applied','applied');
            UPDATE coord_projection_event SET current_receipt_n=2;
            """);
        var mutation = $"UPDATE coord_projection_event SET application_state='applied',recovery_status='none',{change};";
        var error = Assert.Throws<SqliteException>(() => Execute(connection, mutation));
        Assert.Contains("COORD_PAYLOAD_IMMUTABLE", error.Message);
        var trigger = Text(connection, "SELECT sql FROM sqlite_master WHERE name='coord_event_update';");
        var clause = trigger.IndexOf("SELECT CASE WHEN (NEW.raw_bytes", StringComparison.Ordinal);
        Assert.True(clause > 0);
        Execute(connection, "DROP TRIGGER coord_event_update;");
        Execute(connection, trigger[..clause] + " END;");
        // Joint mutant isolates the immutable-bytes clause from the new independent paired-payload CHECK.
        Execute(connection, "PRAGMA ignore_check_constraints=ON;");
        Assert.Throws<Xunit.Sdk.ThrowsException>(() => Assert.Throws<SqliteException>(() => Execute(connection, mutation)));
        Assert.Equal(2L, Number(connection, "SELECT current_receipt_n FROM coord_projection_event;"));
        Execute(connection, "ROLLBACK;");
        _output.WriteLine($"Isolated payload clause mutant admitted: {change}; intact={error.SqliteExtendedErrorCode}");
    }

    [Fact]
    public void Migration_VersionRecordAbort_RollsBackDdlAndPreservesV7History()
    {
        using (var store = SqliteWatcherObservationStore.Open(DatabasePath))
        {
            foreach (var id in new[] { "old-a", "old-b" })
            {
                store.AppendBoardMessage(new BoardMessage(id, "synthetic-repository", BoardMessageKind.Question,
                    "synthetic-session", TrustClassification.Asserted, null, "synthetic", true, false, false,
                    DateTimeOffset.UnixEpoch, 7));
            }
        }
        using (var connection = Connect())
        {
            // Fixture construction only, not an upgrade/downgrade implementation.
            Execute(connection, """
                PRAGMA foreign_keys=OFF;
                DROP TABLE coord_projection_feed;
                DROP TABLE coord_projection_event;
                DROP TABLE coord_projection_checkpoint;
                DELETE FROM watcher_schema_version;
                INSERT INTO watcher_schema_version(version,applied_at) VALUES(7,'1970-01-01T00:00:00Z');
                CREATE TRIGGER reject_v8 BEFORE INSERT ON watcher_schema_version
                WHEN NEW.version=8 BEGIN SELECT RAISE(ABORT,'TEST_VERSION_ABORT'); END;
                """);
        }
        var error = Assert.Throws<SqliteException>(() =>
        {
            using var failed = SqliteWatcherObservationStore.Open(DatabasePath);
        });
        Assert.Contains("TEST_VERSION_ABORT", error.Message);
        using (var connection = Connect())
        {
            Assert.Equal(7L, Number(connection, "SELECT MAX(version) FROM watcher_schema_version;"));
            Assert.Equal(0L, Number(connection, "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'coord_projection_%';"));
            Execute(connection, "DROP TRIGGER reject_v8;");
        }
        using var migrated = SqliteWatcherObservationStore.Open(DatabasePath);
        Assert.Equal(new[] { "old-a", "old-b" }, migrated.AllBoardMessages().Select(x => x.MessageId).Order().ToArray());
        Assert.All(migrated.AllBoardMessages(), x => Assert.Equal(7, x.Seq));
    }

    [Fact]
    public void Endpoint_OneHundredfoldHistory_UsesIndexedBoundedLookup()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        var probes = 0;
        connection.CreateFunction<long, bool>("visit", _ => { probes++; return true; });
        foreach (var size in new[] { 10, 1000 })
        {
            var existing = (int)Number(connection, "SELECT COUNT(*) FROM coord_projection_event;");
            for (var i = existing; i < size; i++)
            {
                Admit(connection, $"K{i}", i);
            }
            var sql = "SELECT source_end FROM coord_projection_event WHERE scope='scope' AND epoch='epoch' ORDER BY source_end DESC LIMIT 1;";
            var plan = Plan(connection, sql);
            _output.WriteLine($"rows={size} plan={plan}");
            Assert.Contains("ix_coord_projection_event_end", plan);
            Assert.DoesNotContain("TEMP B-TREE", plan);
            probes = 0;
            Assert.Equal(size, Number(connection, sql.Replace(" ORDER BY", " AND visit(source_end) ORDER BY")));
            Assert.Equal(1, probes);
        }
        Diagnostic(connection, "K0", 1, 1000, 1001);
        var diagnosticPlan = Plan(connection, """
            SELECT source_end FROM coord_projection_feed
            WHERE scope='scope' AND epoch='epoch' AND outcome='duplicate-occurrence-accounted'
            ORDER BY source_end DESC LIMIT 1;
            """);
        Assert.Contains("ix_coord_projection_diagnostic_end", diagnosticPlan);
        Assert.DoesNotContain("TEMP B-TREE", diagnosticPlan);
        _output.WriteLine(diagnosticPlan);
        _output.WriteLine($"sqlite={Text(connection, "SELECT sqlite_version();")} provider={typeof(SqliteConnection).Assembly.GetName().Version}");
    }

    private static string Plan(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "EXPLAIN QUERY PLAN " + sql;
        using var reader = command.ExecuteReader();
        var lines = new List<string>();
        while (reader.Read())
        {
            lines.Add(reader.GetString(3));
        }
        return string.Join("\n", lines);
    }

    [Theory]
    [InlineData("source_offset=NULL")]
    [InlineData("source_end=NULL")]
    [InlineData("source_offset='oops'")]
    [InlineData("source_end='oops'")]
    [InlineData("source_offset=1.5")]
    [InlineData("source_end=2.5")]
    [InlineData("source_offset=-1")]
    [InlineData("source_end=1")]
    [InlineData("raw_digest=NULL")]
    [InlineData("raw_digest=''")]
    [InlineData("raw_digest=X'01'")]
    [InlineData("admission_n=NULL")]
    [InlineData("admission_n=0")]
    [InlineData("admission_n=1.5")]
    [InlineData("admission_n='oops'")]
    [InlineData("application_state='applied'")]
    [InlineData("session_id='S',session_generation=1")]
    [InlineData("message_id='M'")]
    [InlineData("parent_event_key='P'")]
    [InlineData("parent_application_state='applied'")]
    [InlineData("is_initial=1")]
    public void Diagnostic_InvalidStorageOrSemanticShape_Rejects(string assignments)
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0);
        Diagnostic(connection, "K", 1, 1, 2);
        Execute(connection, "PRAGMA foreign_keys=OFF; DROP TRIGGER coord_feed_update;");
        CoordinationProjectionEvidence.Apply(connection, assignments, _output);
        Assert.Throws<SqliteException>(() => Execute(connection,
            $"UPDATE coord_projection_feed SET {assignments} WHERE outcome='duplicate-occurrence-accounted';"));
    }

    [Fact]
    public void Diagnostic_OneHundredfoldHistory_IndexesActualInsertionLookups()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0);
        CoordinationProjectionEvidence.Apply(connection, "insertion-lookup-scan", _output);
        var probes = 0;
        connection.CreateFunction<long, bool>("visit", _ => { probes++; return true; });
        var next = 1;
        foreach (var size in new[] { 10, 1000 })
        {
            for (; next <= size; next++)
            {
                Diagnostic(connection, "K", 1, next, next + 1);
            }
            var sql = """
                SELECT source_end FROM coord_projection_feed
                WHERE scope='scope' AND epoch='epoch' AND outcome='duplicate-occurrence-accounted'
                ORDER BY source_end DESC LIMIT 1;
                """;
            probes = 0;
            Assert.Equal(size + 1, Number(connection, sql.Replace("ORDER BY", "AND visit(source_end) ORDER BY")));
            Assert.Equal(1, probes);
            _output.WriteLine($"diagnostics={size}; predicate-visits={probes}; plan={Plan(connection, sql)}");
        }
        var trigger = Text(connection, "SELECT sql FROM sqlite_master WHERE name='coord_event_insert';");
        var queries = System.Text.RegularExpressions.Regex.Matches(trigger,
            @"SELECT (?:source_end|1) FROM coord_projection_(?:event|feed)\s+WHERE[^)]+?(?=\))");
        Assert.Equal(5, queries.Count);
        foreach (System.Text.RegularExpressions.Match query in queries)
        {
            var sql = query.Value.Replace("NEW.scope", "'scope'").Replace("NEW.epoch", "'epoch'")
                .Replace("NEW.event_key", "'next'");
            var plan = Plan(connection, sql);
            _output.WriteLine($"actual insertion lookup: {sql}\n{plan}");
            Assert.Contains("SEARCH", plan);
            Assert.DoesNotContain("SCAN ", plan);
            Assert.DoesNotContain("TEMP B-TREE", plan);
        }
    }

    [Fact]
    public void Receipt_NullDiagnosticsAndPositiveGeneration_AreLegalOnlyOnSemanticRows()
    {
        using var store = SqliteWatcherObservationStore.Open(DatabasePath);
        using var connection = Connect();
        Admit(connection, "K", 0, state: "pending");
        CoordinationProjectionEvidence.Apply(connection, "semantic-generation-overstrict", _output);
        Execute(connection, """
            BEGIN IMMEDIATE;
            INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,application_state,session_id,session_generation)
            VALUES('scope','epoch','K',0,1,'applied','applied','S',1);
            UPDATE coord_projection_event SET application_state='applied',recovery_status='none';
            COMMIT;
            """);
        Diagnostic(connection, "K", 1, 1, 2);
        Assert.Equal(2L, Number(connection, "SELECT current_receipt_n FROM coord_projection_event;"));
        Assert.Equal(1L, Number(connection, "SELECT first_receipt_n FROM coord_projection_event;"));
        Assert.Equal(1L, Number(connection, "SELECT session_generation FROM coord_projection_feed WHERE n=2;"));
        Execute(connection, """
            BEGIN IMMEDIATE;
            INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,application_state,session_id,session_generation,payload_presence)
            VALUES('scope','epoch','K',0,1,'tombstone','applied','S',1,0);
            UPDATE coord_projection_event SET payload_presence=0,raw_bytes=NULL,canonical_bytes=NULL;
            COMMIT;
            """);
        Assert.Equal(4L, Number(connection, "SELECT current_receipt_n FROM coord_projection_event;"));
        Assert.Equal(1L, Number(connection, "SELECT COUNT(*) FROM coord_projection_event WHERE raw_bytes IS NULL AND canonical_bytes IS NULL;"));
        Assert.Throws<SqliteException>(() => Diagnostic(connection, "K", 1, 1, 2));
    }

    private static void Admit(SqliteConnection connection, string key, long start, long? n = null, string state = "applied")
    {
        using var transaction = connection.BeginTransaction(deferred: false);
        Execute(connection, $"""
            INSERT INTO coord_projection_feed(n,scope,epoch,event_key,is_initial,outcome,application_state,recovery_status)
            VALUES({(n?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NULL")},'scope','epoch','{key}',1,'{state}','{state}','{(state == "pending" ? "active" : "none")}');
            INSERT INTO coord_projection_event(scope,epoch,event_key,source_offset,source_end,raw_bytes,raw_digest,
                canonical_version,canonical_bytes,first_receipt_n,current_receipt_n,application_state,recovery_status)
            VALUES('scope','epoch','{key}',{start},{start + 1},X'01','digest','legacy-raw/1',X'01',
                last_insert_rowid(),last_insert_rowid(),'{state}','{(state == "pending" ? "active" : "none")}');
            """, transaction);
        transaction.Commit();
    }

    private static void Diagnostic(SqliteConnection connection, string key, long admission, long start, long end,
        SqliteTransaction? transaction = null, string scope = "scope", string epoch = "epoch")
    {
        // Baseline v8 lacks the new columns: use its existing shape to observe semantic CHECK
        // rejection of the new outcome, never a "no such column/table" RED.
        using var shape = connection.CreateCommand();
        shape.Transaction = transaction;
        shape.CommandText = "SELECT COUNT(*) FROM pragma_table_info('coord_projection_feed') WHERE name='source_offset';";
        var hasIntervals = Convert.ToInt64(shape.ExecuteScalar()) == 1;
        Execute(connection, $"""
            INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,application_state,parent_application_state
                {(hasIntervals ? ",source_offset,source_end,raw_digest" : "")})
            VALUES('{scope}','{epoch}','{key}',0,{admission},'duplicate-occurrence-accounted',NULL,NULL
                {(hasIntervals ? $",{start},{end},'digest'" : "")});
            """, transaction);
    }

    private static void Checkpoint(SqliteConnection connection, long end) => Execute(connection, $"""
        INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest)
        VALUES('scope','epoch',{end},'prefix');
        """);

    private SqliteConnection Connect()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath, Pooling = false }.ToString());
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

    private static string Text(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture)!;
    }

    private static long Number(SqliteConnection connection, string sql) => long.Parse(Text(connection, sql), System.Globalization.CultureInfo.InvariantCulture);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_root, recursive: true);
    }
}
