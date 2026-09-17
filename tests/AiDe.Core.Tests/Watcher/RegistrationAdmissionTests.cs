using System.Reflection;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

/// <summary>Dormant native DDL qualification, not enhanced admission or N1/N2 qualification.</summary>
public sealed class RegistrationAdmissionTests
{
    public static IEnumerable<object[]> RequiredFields() =>
        AdmissionFields().Keys.Select(column => new object[] { "native_registration_admission_fact", column })
            .Concat(NoticeFields().Keys.Select(column => new object[] { "registration_notice_delivery", column }));

    [Theory]
    [MemberData(nameof(RequiredFields))]
    public void NativeRows_RequiredFieldIsNull_Rejects(string table, string column)
    {
        using var db = new Database();
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
        db.Execute("CREATE TABLE registration_notice_delivery(sentinel TEXT NOT NULL);");
        db.Execute("INSERT INTO registration_notice_delivery VALUES ('preserve');");

        Assert.Throws<SqliteException>(() => SqliteWatcherObservationStore.Open(db.Path));

        Assert.Equal(8L, db.Scalar("SELECT max(version) FROM watcher_schema_version"));
        Assert.Equal(0L, db.Scalar(
            "SELECT count(*) FROM sqlite_master WHERE name='native_registration_admission_fact'"));
        Assert.Equal("preserve", db.Scalar("SELECT sentinel FROM registration_notice_delivery"));
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

        public Database(bool versionEight = false)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (!File.Exists(System.IO.Path.Combine(root.FullName, "AiDe.Core.Tests.csproj")))
                root = root.Parent ?? throw new InvalidOperationException("Test project root not found.");
            _directory = System.IO.Path.Combine(root.FullName, "obj", "native-admission-fixtures",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "watcher.db");
            if (!versionEight) return;
            // The legacy DDL is unchanged. Construct the real branch-v8 shape without a DROP or backfill.
            foreach (var name in new[] { "SchemaSql", "CoordinationSchemaSql" })
                Execute((string)typeof(SqliteWatcherObservationStore)
                    .GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!);
            Execute("INSERT INTO watcher_schema_version(version,applied_at) VALUES(8,'2026-01-01');");
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

        public void Execute(string sql)
        {
            using var connection = Connect();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public object? Scalar(string sql)
        {
            using var connection = Connect();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
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
