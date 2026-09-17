using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Watcher;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.Watcher;

/// <summary>Structural store proof only: these SQL fixtures confer no trusted capture provenance.</summary>
public sealed class CanonicalCoordinationProjectionTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "xh-v10-" + Guid.NewGuid().ToString("N"));
    private string Database => Path.Combine(_root, "watcher.db");
    private const string EmptyDigest = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";
    private const string Origin = "canonical-requests-v1";

    public CanonicalCoordinationProjectionTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Open_PopulatedV9_AddsV10WithoutChangingNativeRowsOrGuards()
    {
        CreateV9();
        string before;
        string guards;
        using (var connection = Connect())
        {
            Native(connection);
            before = NativeSnapshot(connection);
            guards = Snapshot(connection, "SELECT name,sql FROM sqlite_master WHERE type='trigger' ORDER BY name");
        }

        using var store = SqliteWatcherObservationStore.Open(Database);
        using var read = Connect();

        Assert.Equal(10L, Number(read, "SELECT max(version) FROM watcher_schema_version"));
        Assert.Equal(before, NativeSnapshot(read));
        Assert.Equal(guards, Snapshot(read, "SELECT name,sql FROM sqlite_master WHERE type='trigger' AND name NOT LIKE 'coord_official_%' ORDER BY name"));
        Assert.Equal(3L, Number(read, "SELECT count(*) FROM sqlite_master WHERE type='table' AND name LIKE 'coord_projection_%'"));
        Assert.Equal(0L, Number(read, "SELECT count(*) FROM coord_projection_event WHERE source_kind<>'native' OR official_raw_bytes IS NOT NULL OR official_canonical_bytes IS NOT NULL OR official_identity IS NOT NULL"));
    }

    [Fact]
    public void Open_VersionTenRecordFault_RollsBackEntirePopulatedSchemaAndRows()
    {
        CreateV9();
        string before;
        using (var connection = Connect())
        {
            Native(connection);
            Execute(connection, "CREATE TRIGGER reject_ten BEFORE INSERT ON watcher_schema_version WHEN NEW.version=10 BEGIN SELECT RAISE(ABORT,'TEST_V10_ABORT'); END;");
            before = FullSnapshot(connection);
        }

        var error = Assert.Throws<SqliteException>(() =>
        {
            using var failed = SqliteWatcherObservationStore.Open(Database);
        });

        Assert.Contains("TEST_V10_ABORT", error.Message);
        using var read = Connect();
        Assert.Equal(before, FullSnapshot(read));
        Assert.Equal(9L, Number(read, "SELECT max(version) FROM watcher_schema_version"));
    }

    [Fact]
    public void Open_FreshAndMigrated_HaveIdenticalSchema()
    {
        CreateV9();
        using var migrated = SqliteWatcherObservationStore.Open(Database);
        using var fresh = SqliteWatcherObservationStore.Open(Path.Combine(_root, "fresh.db"));
        using var a = Connect();
        using var b = new SqliteConnection($"Data Source={fresh.DatabasePath};Pooling=False");
        b.Open();

        Assert.Equal(Snapshot(a, "SELECT type,name,sql FROM sqlite_master ORDER BY type,name"),
            Snapshot(b, "SELECT type,name,sql FROM sqlite_master ORDER BY type,name"));
    }

    [Theory]
    [InlineData(65535, "\n", true)]
    [InlineData(65536, "\n", true)]
    [InlineData(65537, "\n", false)]
    [InlineData(65535, "\r\n", true)]
    [InlineData(65536, "\r\n", true)]
    [InlineData(65537, "\r\n", false)]
    public void OfficialFrame_ContentBoundary_EnforcesSeparateFramingBytes(int content, string ending, bool valid)
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();
        var raw = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(CanonicalCoordinationRecordTests.Example()).PadRight(content) + ending);
        var canonical = CanonicalCoordinationRecord.Parse(CanonicalCoordinationRecordTests.Example()).Record!.Canonical.ToArray();

        if (valid) Official(connection, raw, canonical);
        else Assert.Throws<SqliteException>(() => Official(connection, raw, canonical));

        Assert.Equal(valid ? 1L : 0L, Number(connection, "SELECT count(*) FROM coord_projection_event"));
        Assert.Equal(0L, Number(connection, "SELECT count(*) FROM agent_session_dim"));
        Assert.Equal(0L, Number(connection, "SELECT count(*) FROM board_message_fact"));
        Assert.Equal(0L, Number(connection, "SELECT count(*) FROM native_registration_admission_fact"));
    }

    [Theory]
    [InlineData(65535, true)]
    [InlineData(65536, true)]
    [InlineData(65537, false)]
    public void NativeFrame_RawBoundary_RemainsVersionNine(int length, bool valid)
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();

        if (valid) Native(connection, length);
        else Assert.Throws<SqliteException>(() => Native(connection, length));

        Assert.Equal(valid ? 1L : 0L, Number(connection, "SELECT count(*) FROM coord_projection_event"));
    }

    [Fact]
    public void OfficialFrame_RefusedRawOnly_DoesNotInventCanonicalPayload()
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();

        Official(connection, "bad\n"u8.ToArray(), null);

        Assert.Equal(1L, Number(connection, "SELECT count(*) FROM coord_projection_event WHERE application_state='refused' AND official_canonical_bytes IS NULL AND raw_bytes IS NULL AND canonical_bytes IS NULL AND payload_presence=0"));
    }

    [Theory]
    [InlineData("UPDATE coord_projection_event SET source_kind='native'")]
    [InlineData("UPDATE coord_projection_event SET official_raw_bytes=x'7B7D0A'")]
    [InlineData("UPDATE coord_projection_event SET official_canonical_bytes=x'7B7D'")]
    [InlineData("UPDATE coord_projection_event SET official_identity=x'01'")]
    [InlineData("UPDATE coord_projection_event SET current_attempt=1")]
    [InlineData("UPDATE coord_projection_event SET due_utc=1")]
    [InlineData("UPDATE coord_projection_checkpoint SET official_binding=x'5848423102'")]
    public void OfficialAdmission_Mutation_IsRefusedWithoutRowChange(string mutation)
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();
        Official(connection, " \n"u8.ToArray(), "{} "u8.ToArray());
        var before = FullSnapshot(connection);

        Assert.Throws<SqliteException>(() => Execute(connection, mutation));

        Assert.Equal(before, FullSnapshot(connection));
    }

    [Fact]
    public void OfficialPartition_NativeSpoof_IsRejectedAndIsolatedGuardMutationIsDetected()
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();
        Native(connection);
        const string spoof = "UPDATE coord_projection_event SET official_raw_bytes=x'0A'";

        var error = Assert.Throws<SqliteException>(() => Execute(connection, spoof));
        Assert.Contains("COORD_OFFICIAL_EVENT", error.Message);
        Execute(connection, "DROP TRIGGER coord_official_event_update;");
        Execute(connection, spoof);

        Assert.Equal(1L, Number(connection, "SELECT count(*) FROM coord_projection_event WHERE source_kind='native' AND official_raw_bytes IS NOT NULL"));
    }

    [Fact]
    public void OfficialAdmission_UnboundCheckpoint_IsRefused()
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();

        Assert.Throws<SqliteException>(() => Official(connection, "\n"u8.ToArray(), "{}"u8.ToArray(), bind: false));

        Assert.Equal(0L, Number(connection, "SELECT count(*) FROM coord_projection_event"));
        Assert.Equal(0L, Number(connection, "SELECT count(*) FROM coord_projection_feed"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("x\r")]
    [InlineData("x\nx\n")]
    public void OfficialFrame_IncompleteOrMultipleFrames_IsRefused(string frame)
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();

        Assert.Throws<SqliteException>(() => Official(connection, Encoding.UTF8.GetBytes(frame), "{}"u8.ToArray()));

        Assert.Equal(0L, Number(connection, "SELECT count(*) FROM coord_projection_event"));
    }

    [Theory]
    [InlineData(65536, true)]
    [InlineData(65537, false)]
    public void OfficialCanonical_BlobBoundary_IsIndependentOfRaw(int length, bool valid)
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();

        if (valid) Official(connection, "\n"u8.ToArray(), new byte[length]);
        else Assert.Throws<SqliteException>(() => Official(connection, "\n"u8.ToArray(), new byte[length]));

        Assert.Equal(valid ? 1L : 0L, Number(connection, "SELECT count(*) FROM coord_projection_event"));
    }

    [Theory]
    [InlineData("equal", "CANONICAL_EQUAL", CoordinationOccurrenceKind.Equal)]
    [InlineData("conflict", "XH.EVENT_CONFLICT", CoordinationOccurrenceKind.Conflict)]
    public void ReadCoordination_OfficialOccurrence_ExportsStoredKindWithoutChangingAdmission(
        string kind, string reason, CoordinationOccurrenceKind expected)
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();
        Official(connection, " \n"u8.ToArray(), "{}"u8.ToArray());
        var original = Snapshot(connection, "SELECT * FROM coord_projection_event");
        Execute(connection, """
            BEGIN IMMEDIATE;
            INSERT INTO coord_projection_feed(scope,epoch,event_key,is_initial,admission_n,outcome,
                application_state,payload_presence,parent_application_state,source_offset,source_end,
                raw_digest,occurrence_kind,reason)
            VALUES('official','epoch','key',0,1,'duplicate-occurrence-accounted',NULL,0,NULL,2,4,
                'synthetic-occurrence-digest',$kind,$reason);
            UPDATE coord_projection_checkpoint SET accepted_offset=4,prefix_digest='synthetic-prefix' WHERE scope='official';
            COMMIT;
            """, ("$kind", kind), ("$reason", reason));
        var repository = new RepositoryIdentity("repo", "Synthetic");
        var session = new SessionRecord("reader", new(1), new(repository,
            new(repository, "fixture", _root), new("terminal"), new("agent"), null, null,
            TrustClassification.Asserted));

        var result = AiDe.Mcp.BoardTools.ReadCoordination(store, session, new string('A', 64));
        var wire = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.Equal(CoordinationReadStatus.Available, result.Status);
        Assert.Equal(Origin, result.SourceOrigin);
        Assert.Equal(2, result.Entries.Count);
        Assert.Null(result.Entries[0].OccurrenceKind);
        Assert.Equal(expected, result.Entries[1].OccurrenceKind);
        Assert.Equal(reason, result.Entries[1].ReasonCode);
        Assert.Equal(1L, result.Entries[1].AdmissionN);
        Assert.Equal(1L, Number(connection, "SELECT current_receipt_n FROM coord_projection_event"));
        Assert.Equal("applied", result.Entries[0].State);
        Assert.Null(result.Entries[1].State);
        Assert.Contains($"\"OccurrenceKind\":\"{expected}\"", wire);
        Assert.DoesNotContain("official_raw_bytes", wire);
        Assert.DoesNotContain("XHB1", wire);
        Assert.DoesNotContain(_root, wire);
        Assert.Equal(original, Snapshot(connection, "SELECT * FROM coord_projection_event"));
    }

    [Theory]
    [InlineData("official_identity", "NULL")]
    [InlineData("official_raw_bytes", "NULL")]
    [InlineData("source_kind", "NULL")]
    public void OfficialPartition_NullOperand_DoesNotBypassGuard(string column, string value)
    {
        using var store = SqliteWatcherObservationStore.Open(Database);
        using var connection = Connect();
        Official(connection, "\n"u8.ToArray(), "{}"u8.ToArray());
        var before = FullSnapshot(connection);

        Assert.Throws<SqliteException>(() => Execute(connection, $"UPDATE coord_projection_event SET {column}={value}"));

        Assert.Equal(before, FullSnapshot(connection));
    }

    private void CreateV9()
    {
        using var connection = Connect();
        foreach (var field in new[] { "SchemaSql", "CoordinationSchemaSql", "NativeRegistrationSchemaSql" })
        {
            var sql = (string)typeof(SqliteWatcherObservationStore)
                .GetField(field, BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
            Execute(connection, sql);
        }
        Execute(connection, "INSERT INTO watcher_schema_version VALUES(9,'synthetic-v9');");
    }

    private SqliteConnection Connect()
    {
        var connection = new SqliteConnection($"Data Source={Database};Pooling=False");
        connection.Open();
        Execute(connection, "PRAGMA foreign_keys=ON; PRAGMA recursive_triggers=ON;");
        return connection;
    }

    private static void Native(SqliteConnection connection, int length = 1)
    {
        Execute(connection, "BEGIN IMMEDIATE;");
        try
        {
            Execute(connection, """
                INSERT INTO coord_projection_feed(n,scope,epoch,event_key,is_initial,outcome,application_state)
                VALUES(1,'native','epoch','native-key',1,'applied','applied');
                INSERT INTO coord_projection_event(scope,epoch,event_key,source_offset,source_end,
                  raw_bytes,raw_digest,canonical_version,canonical_bytes,first_receipt_n,current_receipt_n,application_state)
                VALUES('native','epoch','native-key',0,$length,$raw,'digest','native-v9',x'00',1,1,'applied');
                COMMIT;
                """, ("$length", length), ("$raw", new byte[length]));
        }
        catch (SqliteException) { Execute(connection, "ROLLBACK;"); throw; }
    }

    private static void Official(SqliteConnection connection, byte[] raw, byte[]? canonical, bool bind = true)
    {
        Execute(connection, "BEGIN IMMEDIATE;");
        try
        {
            Execute(connection, """
                INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest,
                  bound_repository_key,source_origin,public_source_id,official_binding)
                VALUES('official','epoch',0,$empty,$repository,$origin,$public,$binding);
                INSERT INTO coord_projection_feed(n,scope,epoch,event_key,is_initial,outcome,
                  application_state,payload_presence)
                VALUES(1,'official','epoch','key',1,$state,$state,0);
                INSERT INTO coord_projection_event(scope,epoch,event_key,source_offset,source_end,raw_digest,
                  canonical_version,first_receipt_n,current_receipt_n,application_state,payload_presence,
                  source_kind,official_raw_bytes,official_canonical_bytes,official_identity)
                VALUES('official','epoch','key',0,$end,$digest,'synthetic-sql',1,1,$state,0,
                  'canonical-official',$raw,$canonical,$identity);
                UPDATE coord_projection_checkpoint SET accepted_offset=$end,prefix_digest=$digest WHERE scope='official';
                COMMIT;
                """, ("$empty", EmptyDigest), ("$origin", Origin), ("$public", new string('A', 64)),
                ("$repository", new RepositoryIdentity("repo", "Synthetic").CanonicalPath),
                ("$binding", bind ? "XHB1:synthetic-structural-fixture"u8.ToArray() : null),
                ("$state", canonical is null ? "refused" : "applied"), ("$end", raw.Length),
                ("$digest", Convert.ToHexString(SHA256.HashData(raw))), ("$raw", raw), ("$canonical", canonical),
                ("$identity", Encoding.UTF8.GetBytes("XHE1|" +
                    Convert.ToHexString(Encoding.UTF8.GetBytes(new RepositoryIdentity("repo", "Synthetic").CanonicalPath)) + "|" +
                    Convert.ToHexString(Encoding.UTF8.GetBytes(Origin)) + "|7265706F|73747265616D|6576656E74")));
        }
        catch (SqliteException) { Execute(connection, "ROLLBACK;"); throw; }
    }

    private static string NativeSnapshot(SqliteConnection connection) =>
        Snapshot(connection, "SELECT scope,epoch,event_key,source_offset,source_end,raw_bytes,raw_digest,canonical_version,canonical_bytes,canonical_digest,recovery_status,eligibility_generation,payload_presence,seen_components,current_attempt,due_utc,requested_parent_message_id,original_id,first_receipt_n,first_kind,current_receipt_n,application_state FROM coord_projection_event") +
        Snapshot(connection, "SELECT n,scope,epoch,event_key,is_initial,admission_n,outcome,application_state,recovery_status,eligibility_generation,payload_presence,reason,session_id,session_generation,message_id,parent_event_key,parent_application_state,source_offset,source_end,raw_digest FROM coord_projection_feed");

    private static string FullSnapshot(SqliteConnection connection)
    {
        var schema = Snapshot(connection, "SELECT type,name,sql FROM sqlite_master ORDER BY type,name");
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
        var tables = new List<string>();
        using (var reader = command.ExecuteReader()) while (reader.Read()) tables.Add(reader.GetString(0));
        return schema + string.Join("", tables.Select(table => Snapshot(connection, $"SELECT * FROM \"{table.Replace("\"", "\"\"")}\" ORDER BY rowid")));
    }

    private static string Snapshot(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var rows = new List<object[]>();
        while (reader.Read())
        {
            var row = new object[reader.FieldCount];
            reader.GetValues(row);
            rows.Add(row);
        }
        return System.Text.Json.JsonSerializer.Serialize(rows);
    }

    private static long Number(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static void Execute(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
