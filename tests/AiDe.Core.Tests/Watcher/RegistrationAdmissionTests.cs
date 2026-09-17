using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

/// <summary>Dormant native DDL qualification, not enhanced admission or N1/N2 qualification.</summary>
public sealed class RegistrationAdmissionTests(ITestOutputHelper output)
{
    private const string AdmissionTable = "native_registration_admission_fact";
    private const string NoticeTable = "registration_notice_delivery";

    public static IEnumerable<object[]> HexFields() =>
    [
        [AdmissionTable, "input_digest", 64],
        [AdmissionTable, "context_digest", 64],
        [AdmissionTable, "decision_digest", 64],
        [NoticeTable, "publication_digest", 64],
        [NoticeTable, "notice_id", 32],
    ];

    [Theory]
    [MemberData(nameof(HexFields))]
    public void NativeRows_HexWithNulAndOversizedSuffix_Rejects(string table, string column, int length)
    {
        using var db = Database.WithAdmissionFor(table);
        var value = new string('a', length) + "\0" + new string('a', 70000);
        output.WriteLine($"column={table}.{column}; utf8Bytes={System.Text.Encoding.UTF8.GetByteCount(value)}");

        AssertInsertRejected(db, table, column, value);
    }

    [Theory]
    [MemberData(nameof(HexFields))]
    public void NativeRows_ExactLowerHexBoundary_PreservesBytes(string table, string column, int length)
    {
        using var db = Database.WithAdmissionFor(table);
        var value = new string('a', length);
        InsertParameter(db, table, column, value);

        Assert.Equal(value, db.Scalar($"SELECT {column} FROM {table}"));
        Assert.Equal((long)length, db.Scalar($"SELECT length(CAST({column} AS BLOB)) FROM {table}"));
    }

    [Theory]
    [MemberData(nameof(HexFields))]
    public void NativeRows_NonHexOrWrongLength_Rejects(string table, string column, int length)
    {
        using var db = Database.WithAdmissionFor(table);
        foreach (var value in new[] { "", new string('a', length - 1), new string('a', length + 1),
                     new string('A', length), new string('g', length), new string('\u00e9', length) })
            AssertInsertRejected(db, table, column, value);
    }

    [Theory]
    [InlineData("operation_id", 128)]
    [InlineData("session_id", 512)]
    [InlineData("repository_sent", 4096)]
    [InlineData("repository_used", 4096)]
    [InlineData("worktree_path", 4096)]
    [InlineData("worktree_branch", 512)]
    [InlineData("terminal_id", 512)]
    [InlineData("agent_name", 256)]
    [InlineData("harness_name", 256)]
    [InlineData("harness_version", 128)]
    [InlineData("model_name", 256)]
    [InlineData("model_version", 128)]
    public void Admission_BoundedTextNulSuffix_Rejects(string column, int maximum)
    {
        using var db = Database.WithAdmissionFor(AdmissionTable);
        AssertInsertRejected(db, AdmissionTable, column, new string('a', maximum) + "\0suffix");
    }

    [Fact]
    public void Notice_TargetAndClaimOwnerNulSuffix_RejectsAtTheirValidWritePaths()
    {
        using var db = Database.WithAdmission();
        AssertInsertRejected(db, NoticeTable, "target_root", "target\0suffix");
        db.Execute(NoticeInsert);

        var error = Assert.Throws<SqliteException>(() => db.Execute("""
            UPDATE registration_notice_delivery SET state='InFlight',attempt=1,ownership_version=1,
                owner_id=$value
            """, ("$value", "owner\0suffix")));

        Assert.Equal(275, error.SqliteExtendedErrorCode);
        Assert.Equal("Pending", db.Scalar("SELECT state FROM registration_notice_delivery"));
    }

    [Theory]
    [InlineData(long.MinValue)]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    public void Notice_IntegerDueAtInsert_PreservesValue(long value)
    {
        using var db = Database.WithAdmission();
        InsertParameter(db, NoticeTable, "due_at_ms", value);
        Assert.Equal(value, db.Scalar("SELECT due_at_ms FROM registration_notice_delivery"));
    }

    [Fact]
    public void Notice_TextDueAtInsert_RejectsCheckNotTransition()
    {
        using var db = Database.WithAdmission();
        AssertInsertRejected(db, NoticeTable, "due_at_ms", "tomorrow");
    }

    [Fact]
    public void Notice_RemovedDueTypeCheck_SameInsertAssertionFails()
    {
        var schema = NativeSchema();
        const string check = "due_at_ms INTEGER NOT NULL CHECK(typeof(due_at_ms)='integer')";
        Assert.Contains(check, schema);
        var mutant = schema.Replace(check, "due_at_ms INTEGER NOT NULL", StringComparison.Ordinal);
        using var db = Database.WithAdmissionFor(NoticeTable, mutant);
        output.WriteLine("MUTANT due_at_ms type guard only:\n" + mutant);

        Assert.IsType<Xunit.Sdk.ThrowsException>(
            Record.Exception(() => AssertInsertRejected(db, NoticeTable, "due_at_ms", "tomorrow")));
        Assert.Equal("text", db.Scalar("SELECT typeof(due_at_ms) FROM registration_notice_delivery"));
        Assert.Equal("tomorrow", db.Scalar("SELECT due_at_ms FROM registration_notice_delivery"));
    }

    [Theory]
    [InlineData("Untrusted")]
    [InlineData("verified")]
    [InlineData("")]
    public void Admission_UnknownTrust_Rejects(string value)
    {
        using var db = Database.WithAdmissionFor(AdmissionTable);
        AssertInsertRejected(db, AdmissionTable, "trust", value);
    }

    [Theory]
    [InlineData("Untrusted")]
    [InlineData("verified")]
    [InlineData("")]
    public void Admission_RemovedTrustConstraint_SameInvalidEnumAssertionFails(string value)
    {
        var mutant = WithoutColumnConstraint(AdmissionTable, "trust");
        using var db = Database.WithAdmissionFor(AdmissionTable, mutant);
        output.WriteLine("MUTANT trust constraint:\n" + mutant);

        Assert.IsType<Xunit.Sdk.ThrowsException>(
            Record.Exception(() => AssertInsertRejected(db, AdmissionTable, "trust", value)));
        Assert.Equal(value, db.Scalar("SELECT trust FROM native_registration_admission_fact"));
    }

    public static IEnumerable<object[]> RequiredFields() =>
        AdmissionFields().Keys.Select(column => new object[] { "native_registration_admission_fact", column })
            .Concat(NoticeFields().Keys.Select(column => new object[] { "registration_notice_delivery", column }));

    [Theory]
    [MemberData(nameof(RequiredFields))]
    public void NativeRows_RequiredFieldIsNull_Rejects(string table, string column)
    {
        using var db = new Database();
        AssertRequiredFieldRejected(db, table, column);
    }

    [Theory]
    [MemberData(nameof(RequiredFields))]
    public void NativeRows_RemovedRequiredColumnConstraints_SameNullAssertionFails(string table, string column)
    {
        var mutant = WithoutColumnConstraint(table, column);
        using var db = new Database(nativeSchema: mutant);
        output.WriteLine($"MUTANT {table}.{column} required-column constraints:\n{mutant}");

        Assert.IsType<Xunit.Sdk.ThrowsException>(
            Record.Exception(() => AssertRequiredFieldRejected(db, table, column)));
        Assert.Equal(1L, db.Scalar($"SELECT count(*) FROM {table}"));
    }

    private static void AssertRequiredFieldRejected(Database db, string table, string column)
    {
        using var store = SqliteWatcherObservationStore.Open(db.Path);
        if ("registration_notice_delivery" == table) db.Execute(Insert(AdmissionFields()));
        var fields = "registration_notice_delivery" == table ? NoticeFields() : AdmissionFields();
        fields[column] = "NULL";

        Assert.Throws<SqliteException>(() => db.Execute(Insert(fields, table)));

        Assert.Equal(0L, db.Scalar($"SELECT count(*) FROM {table}"));
        Assert.Empty(store.AllSessions());
    }

    [Theory]
    [InlineData(2147483648L)]
    [InlineData(long.MaxValue)]
    public void Admission_ValidInt64Generation_PreservesActualDomainValue(long value)
    {
        using var db = new Database();
        using var store = SqliteWatcherObservationStore.Open(db.Path);
        var generation = new SessionGeneration(value);
        var fields = AdmissionFields();
        fields["generation"] = generation.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        db.Execute(Insert(fields));

        Assert.Equal(generation.Value, db.Scalar("SELECT generation FROM native_registration_admission_fact"));
    }

    [Theory]
    [InlineData(TrustClassification.Asserted)]
    [InlineData(TrustClassification.Verified)]
    public void Admission_ActualTrustEnum_PreservesClassificationWithoutIssuingAuthority(TrustClassification trust)
    {
        using var db = new Database();
        using var store = SqliteWatcherObservationStore.Open(db.Path);
        var fields = AdmissionFields();
        fields["trust"] = $"'{trust}'";

        db.Execute(Insert(fields));

        Assert.Equal(trust.ToString(), db.Scalar("SELECT trust FROM native_registration_admission_fact"));
        Assert.Empty(store.AllSessions());
    }

    [Fact]
    public void Open_FreshAndVersionEight_ProduceIdenticalSchemaWithoutNativeEffects()
    {
        using var fresh = new Database();
        using var upgraded = new Database(versionEight: true);
        using var first = SqliteWatcherObservationStore.Open(fresh.Path);
        using var second = SqliteWatcherObservationStore.Open(upgraded.Path);

        Assert.Equal(9L, fresh.Scalar("SELECT max(version) FROM watcher_schema_version"));
        Assert.Equal(fresh.Schema(), upgraded.Schema());
        Assert.Empty(first.AllSessions());
        Assert.Empty(second.AllSessions());
        Assert.Equal(0L, fresh.Scalar("SELECT count(*) FROM native_registration_admission_fact"));
        Assert.Equal(0L, fresh.Scalar("SELECT count(*) FROM registration_notice_delivery"));
    }

    [Fact]
    public void Open_SecondNewTableCollides_RollsBackWholeMigration()
    {
        using var db = new Database(versionEight: true);
        db.SeedVersionEight();
        db.Execute("CREATE TABLE registration_notice_delivery(sentinel TEXT NOT NULL);");
        db.Execute("INSERT INTO registration_notice_delivery VALUES ('preserve');");
        var before = db.Snapshot();
        output.WriteLine("BEFORE migration collision:\n" + before);

        var error = Assert.Throws<SqliteException>(() => SqliteWatcherObservationStore.Open(db.Path));

        Assert.Equal(1, error.SqliteErrorCode);
        Assert.Contains("registration_notice_delivery", error.Message);
        var after = db.Snapshot();
        output.WriteLine("AFTER migration collision:\n" + after);
        Assert.Equal(before, after);
        Assert.Equal(8L, db.Scalar("SELECT max(version) FROM watcher_schema_version"));
        Assert.Equal(0L, db.Scalar(
            "SELECT count(*) FROM sqlite_master WHERE name='native_registration_admission_fact'"));
        Assert.Equal("preserve", db.Scalar("SELECT sentinel FROM registration_notice_delivery"));
        using var exclusive = File.Open(db.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(exclusive.Length > 0);
    }

    [Fact]
    public void Database_HeldSqliteHandle_FailsExclusiveOpenUntilDisposed()
    {
        using var db = new Database(versionEight: true);
        using (var held = new SqliteConnection(new SqliteConnectionStringBuilder
               { DataSource = db.Path, Pooling = false }.ToString()))
        {
            held.Open();
            Assert.Throws<IOException>(() =>
            {
                using var unexpected = File.Open(db.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            });
        }
        using var exclusive = File.Open(db.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(exclusive.Length > 0);
    }

    [Fact]
    public void Database_PopulatedSnapshot_ChangesForRowAndObjectMutations()
    {
        using var db = new Database(versionEight: true);
        db.SeedVersionEight();
        var before = db.Snapshot();
        db.Execute("UPDATE session_heartbeat SET monotonic_ticks=43");
        var rowChanged = db.Snapshot();
        Assert.NotEqual(before, rowChanged);
        db.Execute("CREATE INDEX snapshot_oracle ON session_heartbeat(monotonic_ticks)");
        Assert.NotEqual(rowChanged, db.Snapshot());
    }

    [Theory]
    [InlineData("UPDATE native_registration_admission_fact SET repository_sent='changed'")]
    [InlineData("DELETE FROM native_registration_admission_fact")]
    [InlineData("INSERT OR REPLACE INTO native_registration_admission_fact SELECT * FROM native_registration_admission_fact")]
    [InlineData("UPDATE registration_notice_delivery SET publication_bytes=x'02'")]
    [InlineData("UPDATE registration_notice_delivery SET target_root='changed'")]
    [InlineData("UPDATE registration_notice_delivery SET publication_digest=printf('%064d',1)")]
    [InlineData("DELETE FROM registration_notice_delivery")]
    [InlineData("INSERT OR REPLACE INTO registration_notice_delivery SELECT * FROM registration_notice_delivery")]
    public void NativeRows_ImmutableMutation_RejectsWithoutChangingRows(string mutation)
    {
        using var db = Database.WithAdmission();
        db.Execute(NoticeInsert);
        var before = db.Rows();

        Assert.Throws<SqliteException>(() => db.Execute(mutation));

        Assert.Equal(before, db.Rows());
    }

    [Theory]
    [InlineData("operation_id", "NULL")]
    [InlineData("operation_id", "''")]
    [InlineData("operation_id", "zeroblob(4)")]
    [InlineData("generation", "NULL")]
    [InlineData("generation", "-1")]
    [InlineData("generation", "1.5")]
    [InlineData("generation", "'text'")]
    [InlineData("repository_sent", "NULL")]
    [InlineData("repository_sent", "printf('%4097d',1)")]
    [InlineData("decision_digest", "'incorrect'")]
    [InlineData("correction_code", "'HUMAN_AUTHORITY'")]
    public void Admission_InsertInvalidField_Rejects(string column, string value)
    {
        using var db = new Database();
        using var store = SqliteWatcherObservationStore.Open(db.Path);
        var fields = AdmissionFields();
        fields[column] = value;

        Assert.Throws<SqliteException>(() => db.Execute(Insert(fields)));

        Assert.Equal(0L, db.Scalar("SELECT count(*) FROM native_registration_admission_fact"));
        Assert.Empty(store.AllSessions());
    }

    [Theory]
    [InlineData("UPDATE registration_notice_delivery SET state='Published',published_at_ms=1")]
    [InlineData("UPDATE registration_notice_delivery SET state='InFlight',owner_id='owner'")]
    [InlineData("UPDATE registration_notice_delivery SET attempt=-1")]
    [InlineData("UPDATE registration_notice_delivery SET ownership_version=1.5")]
    [InlineData("UPDATE registration_notice_delivery SET due_at_ms=NULL")]
    [InlineData("UPDATE registration_notice_delivery SET state='Consumed'")]
    public void Notice_InvalidTransition_RejectsWithoutChangingObligation(string mutation)
    {
        using var db = Database.WithAdmission();
        db.Execute(NoticeInsert);
        var before = db.Rows();

        Assert.Throws<SqliteException>(() => db.Execute(mutation));

        Assert.Equal(before, db.Rows());
    }

    [Fact]
    public void Notice_ClaimRetryAndPublish_PreservesBytesAndMakesPublicationTerminal()
    {
        using var db = Database.WithAdmission();
        db.Execute(NoticeInsert);

        db.Execute("""
            UPDATE registration_notice_delivery SET state='InFlight',owner_id='owner',
                attempt=1,ownership_version=1;
            UPDATE registration_notice_delivery SET state='Pending',owner_id=NULL,
                ownership_version=2,due_at_ms=10;
            UPDATE registration_notice_delivery SET state='InFlight',owner_id='next-owner',
                attempt=2,ownership_version=3;
            UPDATE registration_notice_delivery SET state='Published',owner_id=NULL,
                ownership_version=4,published_at_ms=11;
            """);

        Assert.Equal("Published|2|4|01", db.Scalar("""
            SELECT state||'|'||attempt||'|'||ownership_version||'|'||hex(publication_bytes)
            FROM registration_notice_delivery
            """));
        var before = db.Rows();
        Assert.Throws<SqliteException>(() => db.Execute(
            "UPDATE registration_notice_delivery SET state='Pending',published_at_ms=NULL"));
        Assert.Equal(before, db.Rows());
    }

    [Theory]
    [InlineData("operation_id", "'absent'")]
    [InlineData("decision_digest", "printf('%064d',1)")]
    [InlineData("schema_version", "2")]
    [InlineData("publication_bytes", "NULL")]
    [InlineData("publication_bytes", "'not a blob'")]
    [InlineData("publication_bytes", "zeroblob(65537)")]
    [InlineData("publication_digest", "'invalid'")]
    [InlineData("target_kind", "'canonical-ack'")]
    [InlineData("target_root", "NULL")]
    public void Notice_InvalidIdentityOrPayload_Rejects(string column, string value)
    {
        using var db = Database.WithAdmission();
        var fields = NoticeFields();
        fields[column] = value;

        Assert.Throws<SqliteException>(() => db.Execute(Insert(fields, "registration_notice_delivery")));

        Assert.Equal(0L, db.Scalar("SELECT count(*) FROM registration_notice_delivery"));
    }

    [Fact]
    public void Admission_AnotherOperationForSameSessionGeneration_RejectsReplacement()
    {
        using var db = Database.WithAdmission();
        var fields = AdmissionFields();
        fields["operation_id"] = "'other'";

        Assert.Throws<SqliteException>(() => db.Execute(Insert(fields).Replace(
            "INSERT INTO", "INSERT OR REPLACE INTO", StringComparison.Ordinal)));

        Assert.Equal("op", db.Scalar("SELECT operation_id FROM native_registration_admission_fact"));
    }

    private static string NoticeInsert => Insert(NoticeFields(), "registration_notice_delivery");

    private static string NativeSchema() => (string)typeof(SqliteWatcherObservationStore)
        .GetField("NativeRegistrationSchemaSql", BindingFlags.Static | BindingFlags.NonPublic)!
        .GetRawConstantValue()!;

    private static string WithoutColumnConstraint(string table, string column)
    {
        var schema = NativeSchema();
        var start = schema.IndexOf($"CREATE TABLE {table} (", StringComparison.Ordinal);
        var end = schema.IndexOf(");", start, StringComparison.Ordinal);
        var definition = schema[start..end];
        var pattern = $@"(?m)^    {Regex.Escape(column)} (TEXT|INTEGER|BLOB)[\s\S]*?(?=,\r?\n)";
        var match = Regex.Match(definition, pattern);
        Assert.True(match.Success, $"Missing column definition: {table}.{column}");
        var changed = definition.Remove(match.Index, match.Length)
            .Insert(match.Index, $"    {column} {match.Groups[1].Value} NULL");
        return schema[..start] + changed + schema[end..];
    }

    private static void InsertParameter(Database db, string table, string column, object value)
    {
        var fields = NoticeTable == table ? NoticeFields() : AdmissionFields();
        if (AdmissionTable == table)
        {
            fields["harness_name"] = "'harness'";
            fields["model_name"] = "'model'";
        }
        // Bind exact UTF-8 bytes: this provider's string binding can truncate at embedded NUL.
        fields[column] = value is string ? "CAST($value AS TEXT)" : "$value";
        if (value is string text)
        {
            value = System.Text.Encoding.UTF8.GetBytes(text);
            Assert.Equal(text, db.Scalar("SELECT CAST($value AS TEXT)", ("$value", value)));
        }
        db.Execute(Insert(fields, table), ("$value", value));
    }

    private static void AssertInsertRejected(Database db, string table, string column, object value)
    {
        var error = Assert.Throws<SqliteException>(() => InsertParameter(db, table, column, value));
        Assert.Equal(275, error.SqliteExtendedErrorCode);
        Assert.Equal(0L, db.Scalar($"SELECT count(*) FROM {table}"));
    }

    private static Dictionary<string, string> AdmissionFields() => new()
    {
        ["operation_id"] = "'op'", ["schema_version"] = "1",
        ["input_digest"] = "printf('%064d',0)", ["context_digest"] = "printf('%064d',0)",
        ["decision_digest"] = "printf('%064d',0)", ["session_id"] = "'session'",
        ["generation"] = "1", ["repository_sent"] = "'synthetic-sent'",
        ["repository_used"] = "'synthetic-used'", ["worktree_path"] = "'synthetic-worktree'",
        ["worktree_branch"] = "'test'", ["terminal_id"] = "'terminal'", ["agent_name"] = "'test'",
        ["trust"] = "'Asserted'", ["correction_code"] = "'LINKED_WORKTREE'", ["recorded_at_ms"] = "0",
    };

    private static Dictionary<string, string> NoticeFields() => new()
    {
        ["notice_id"] = "printf('%032d',0)", ["operation_id"] = "'op'", ["schema_version"] = "1",
        ["decision_digest"] = "printf('%064d',0)", ["correction_code"] = "'LINKED_WORKTREE'",
        ["publication_bytes"] = "x'01'", ["publication_digest"] = "printf('%064d',0)",
        ["target_kind"] = "'native-file'", ["target_root"] = "'synthetic-target'",
        ["state"] = "'Pending'", ["attempt"] = "0", ["ownership_version"] = "0", ["due_at_ms"] = "0",
    };

    private static string Insert(Dictionary<string, string> fields,
        string table = "native_registration_admission_fact") =>
        $"INSERT INTO {table}({string.Join(',', fields.Keys)}) VALUES({string.Join(',', fields.Values)})";

    private sealed class Database : IDisposable
    {
        public string Path { get; }
        private readonly string _directory;

        public Database(bool versionEight = false, string? nativeSchema = null)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(System.IO.Path.Combine(root.FullName, "AiDe.Core.Tests.csproj")))
                root = root.Parent ?? throw new InvalidOperationException("Test project root not found.");
            _directory = System.IO.Path.Combine(root.FullName, "obj", "native-admission-fixtures",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "watcher.db");
            if (!versionEight && null == nativeSchema) return;
            // The legacy DDL is unchanged. Construct the real branch-v8 shape without a DROP or backfill.
            foreach (var name in new[] { "SchemaSql", "CoordinationSchemaSql" })
                Execute((string)typeof(SqliteWatcherObservationStore)
                    .GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!);
            Execute("INSERT INTO watcher_schema_version(version,applied_at) VALUES(8,'2026-01-01');");
            if (null != nativeSchema)
                Execute(nativeSchema + "\nINSERT INTO watcher_schema_version VALUES(9,'2026-01-02');");
        }

        public static Database WithAdmissionFor(string table, string? nativeSchema = null)
        {
            var db = new Database(nativeSchema: nativeSchema);
            try
            {
                using var store = SqliteWatcherObservationStore.Open(db.Path);
                if (NoticeTable == table) db.Execute(Insert(AdmissionFields()));
                return db;
            }
            catch { db.Dispose(); throw; }
        }

        public static Database WithAdmission()
        {
            var db = new Database();
            try
            {
                using var store = SqliteWatcherObservationStore.Open(db.Path);
                db.Execute(Insert(AdmissionFields()));
                return db;
            }
            catch { db.Dispose(); throw; }
        }

        private SqliteConnection Connect()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Path, Pooling = false,
            }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            // REPLACE guards must hold even on a raw writer with recursive triggers off.
            command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA recursive_triggers=OFF;";
            command.ExecuteNonQuery();
            return connection;
        }

        public void Execute(string sql, params (string Name, object Value)[] parameters)
        {
            using var connection = Connect();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
            command.ExecuteNonQuery();
        }

        public void SeedVersionEight() => Execute("""
            BEGIN;
            INSERT INTO agent_session_dim VALUES
                ('original-session',2147483648,'synthetic-repo','synthetic','branch','tree',
                 'original-terminal','test','harness','1','model','2','Asserted');
            INSERT INTO session_heartbeat VALUES('original-session',42);
            INSERT INTO session_ended VALUES('original-session');
            INSERT INTO observed_span_fact VALUES
                ('original-span','original-session','original-trace','original-source','test','2026-01-01');
            INSERT INTO work_episode_dim VALUES
                ('original-episode','original-session',2147483648,'goal','done','excluded','2026-01-01',NULL,NULL);
            INSERT INTO board_message_fact VALUES
                ('original-message','synthetic-repo','question','original-session','Asserted',NULL,
                 'synthetic content',0,0,0,'2026-01-01',7);
            INSERT INTO coord_projection_event
                (scope,epoch,event_key,source_offset,source_end,raw_bytes,raw_digest,canonical_version,
                 canonical_bytes,canonical_digest,original_id,first_receipt_n,current_receipt_n,application_state)
                VALUES('synthetic-scope','epoch-1','original-event',0,1,x'01','raw-digest','1',
                       x'02','canonical-digest','original-session',7,7,'applied');
            INSERT INTO coord_projection_feed
                (n,scope,epoch,event_key,is_initial,outcome,application_state,session_id,session_generation,message_id)
                VALUES(7,'synthetic-scope','epoch-1','original-event',1,'applied','applied',
                       'original-session',2147483648,'original-message');
            INSERT INTO coord_projection_checkpoint
                (scope,epoch,accepted_offset,prefix_digest,bound_repository_key,source_origin,public_source_id)
                VALUES('synthetic-scope','epoch-1',1,'prefix-digest','synthetic-repo','native','original-source');
            COMMIT;
            """);

        public string Snapshot()
        {
            var snapshot = new Dictionary<string, string[]> { ["sqlite_master"] = Schema() };
            foreach (var table in ReadRows("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name"))
            {
                using var connection = Connect();
                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM \"{table.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
                using var reader = command.ExecuteReader();
                var rows = new List<string>();
                while (reader.Read())
                    rows.Add(JsonSerializer.Serialize(Enumerable.Range(0, reader.FieldCount).Select(i => new
                    {
                        column = reader.GetName(i), type = reader.GetDataTypeName(i),
                        value = reader.IsDBNull(i) ? null : reader.GetValue(i),
                    })));
                snapshot.Add(table, rows.Order(StringComparer.Ordinal).ToArray());
            }
            return JsonSerializer.Serialize(snapshot);
        }

        public object? Scalar(string sql, params (string Name, object Value)[] parameters)
        {
            using var connection = Connect();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
            return command.ExecuteScalar();
        }

        public string[] Schema() => ReadRows(
            "SELECT type,name,tbl_name,sql FROM sqlite_master ORDER BY type,name");

        public string[] Rows() => ReadRows("SELECT * FROM native_registration_admission_fact")
            .Concat(ReadRows("SELECT * FROM registration_notice_delivery")).ToArray();

        private string[] ReadRows(string sql)
        {
            using var connection = Connect();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
                rows.Add(string.Join('|', Enumerable.Range(0, reader.FieldCount).Select(i =>
                    reader.GetValue(i) is byte[] bytes ? Convert.ToHexString(bytes) : reader.GetValue(i))));
            return rows.ToArray();
        }

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }
}
