using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Tests.SchemaV2;

/// <summary>Shared fixtures for the schema-v2 proofs: a real v1 file, a seeded v2 store, raw inspection.</summary>
internal static class SchemaV2Support
{
    internal static string NewDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aide-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    internal static SqliteConnection OpenRaw(string path, bool readOnly = false)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false,
        }.ToString());
        connection.Open();
        return connection;
    }

    internal static void Run(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    internal static long Count(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    /// <summary>
    /// Builds a genuine v1 workspace.db: the frozen v1 baseline DDL, v1 triggers, WAL mode, and rows
    /// in every table a migration must carry across — two drafts (one with two revisions), two
    /// attempts with outcomes, a session.
    /// </summary>
    internal static void CreateV1Database(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false,
        }.ToString());
        connection.Open();
        using (connection)
        {
            Run(connection, "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA recursive_triggers=ON;");
            Run(connection, WorkspaceSchema.CreateSql);
            Run(connection, WorkspaceSchema.TriggerSql(WorkspaceSchema.V1FactTables));
            Run(connection, """
                INSERT INTO schema_version (version, applied_at) VALUES (1, '2026-10-01T00:00:00.0000000Z');
                INSERT INTO core_state (id, core_epoch, ingress_seq) VALUES (1, 3, 10);
                INSERT INTO prompt_revision_fact VALUES ('draft-a', 1, 'first', '2026-10-01T00:00:01Z', 1);
                INSERT INTO prompt_revision_fact VALUES ('draft-a', 2, 'second', '2026-10-01T00:00:02Z', 2);
                INSERT INTO prompt_revision_fact VALUES ('draft-b', 1, 'other', '2026-10-01T00:00:03Z', 3);
                INSERT INTO dispatch_attempt_fact VALUES
                    ('key-a', 'ws', 3, 'draft-a', 2, 'sess-1', 1, '2026-10-01T00:00:04Z', 4);
                INSERT INTO dispatch_attempt_fact VALUES
                    ('key-b', 'ws', 3, 'draft-b', 1, 'sess-1', 1, '2026-10-01T00:00:05Z', 5);
                INSERT INTO dispatch_outcome_fact VALUES ('key-a', 6, 'PtyWriteAccepted', NULL, '2026-10-01T00:00:06Z');
                INSERT INTO dispatch_outcome_fact VALUES ('key-b', 7, 'DeliveryUnknown', 'AIDE-DISPATCH-DELIVERY-UNKNOWN', '2026-10-01T00:00:07Z');
                INSERT INTO session_dim (session_id, generation, processing_class, display_name, valid_from_seq, valid_to_seq)
                    VALUES ('sess-1', 1, 'LocalOnly', 'one', 8, NULL);
                """);
        }

        SqliteConnection.ClearAllPools();
    }

    /// <summary>Every column of every row, for the named tables and columns, as comparable text.</summary>
    internal static List<string> Dump(SqliteConnection connection, params (string Table, string Columns)[] tables)
    {
        var lines = new List<string>();
        foreach (var (table, columns) in tables)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {columns} FROM {table} ORDER BY 1, 2;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var cells = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.IsDBNull(i) ? "<null>" : Convert.ToString(reader.GetValue(i)));
                lines.Add($"{table}: {string.Join(" | ", cells)}");
            }
        }

        return lines;
    }

    /// <summary>The v1 tables with exactly the columns v1 defined — what a migration must not alter.</summary>
    internal static readonly (string Table, string Columns)[] V1Columns =
    [
        ("prompt_revision_fact", "draft_id, revision_no, body, saved_at, ingress_seq"),
        ("dispatch_attempt_fact", "dispatch_key, workspace_id, workspace_epoch, draft_id, revision_no, session_id, session_generation, attempted_at, ingress_seq"),
        ("dispatch_outcome_fact", "dispatch_key, ingress_seq, state, error_code, recorded_at"),
        ("session_dim", "session_id, generation, processing_class, display_name, valid_from_seq, valid_to_seq"),
    ];

    /// <summary>The whole schema as comparable text: every object's type, name, table and normalised DDL.</summary>
    internal static List<string> SchemaObjects(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT type, name, tbl_name, COALESCE(sql, '') FROM sqlite_master
            WHERE name <> 'sqlite_sequence' ORDER BY type, name;
            """;
        using var reader = command.ExecuteReader();
        var rows = new List<string>();
        while (reader.Read())
        {
            var sql = string.Join(' ', reader.GetString(3).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            rows.Add($"{reader.GetString(0)} {reader.GetString(1)} on {reader.GetString(2)}: {sql}");
        }

        return rows;
    }

    /// <summary>One row in every v2 table, through the typed API. Returns the harness-profile key.</summary>
    internal static long SeedEveryV2Table(WorkspaceStore store)
    {
        using var writer = store.BeginWrite();
        writer.AppendJourney(new JourneyRow("j1", "work", "First journey", OriginScope: "{\"scope\":\"s\"}"));
        writer.AppendNotebook(new NotebookRow("nb1", "j1", Legacy: false));
        writer.AppendNotebookRevision(new NotebookRevisionRow("nb1", 1, "body one"));
        var profile = writer.AppendHarnessProfile(new HarnessProfileRow("hp1", "engine-1", "claude", "C:/home"));
        writer.AppendAttempt(new AttemptRow("k1", "ws", 1, "nb1", 1, "s1", 1,
            ProjectionSha: "sha-p", RouteFingerprint: "route-x", ProfileKey: profile, ScopeSnapshot: "{}"));
        writer.AppendOutcome(new OutcomeRow("k1", AiDe.Core.Facts.DispatchState.Dispatched));
        writer.AppendPermissionDecision(new PermissionDecisionRow(
            "s1", "k1", 7, "write", "repo", "sha-a", "[\"allow\",\"deny\"]", "allow", "user"));
        writer.AppendEvidenceLink(new EvidenceLinkRow(
            "link1", "criterion", "c1", 1, "artifact://a", "producer", "base-1", "Verified"));
        writer.AppendAssessment(new AssessmentRow("as1", "drift", "scope-1", "base-1", "[]", "{}"));
        writer.AppendObligation(new ObligationRow("ob1", "cl1", 1, "Do the thing", Mandatory: true));
        writer.AppendDisposition(new DispositionRow("obligation", "ob1", "acknowledged", "user", "base-1"));
        writer.AppendProfileObservation(new ProfileObservationRow(profile, "installed", "true", "probe", "2026-10-18T00:00:00Z"));
        writer.Commit();
        return profile;
    }

    internal static AttemptRow Attempt(string key, string notebook = "nb1", int revision = 1) =>
        new(key, "ws", 1, notebook, revision, "s1", 1);
}
