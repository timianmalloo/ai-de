using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit.Abstractions;

namespace AiDe.Core.Tests.Watcher;

/// <summary>
/// Fault injection into a constructor-created, disposable boundary-test database only.
/// Each case retains its original assertion. Joint mutants explicitly weaken overlapping guards;
/// no production schema, test binary, global SQLite setting, or other database is modified.
/// </summary>
internal static class CoordinationProjectionEvidence
{
    internal static void Apply(SqliteConnection connection, string identity, ITestOutputHelper output)
    {
        if (Environment.GetEnvironmentVariable("P22_BOUNDARY_EVIDENCE") != "faults")
        {
            return;
        }

        var database = Path.GetFullPath(connection.DataSource);
        var parent = Path.GetDirectoryName(database)!;
        if (!parent.StartsWith(Path.GetFullPath(Environment.CurrentDirectory) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(parent).StartsWith("p22-boundary-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("P22_MUTATION_REQUIRES_OWN_BOUNDARY_DATABASE");
        }

        var name = identity == "insertion-lookup-scan" ? "coord_event_insert" : "coord_projection_feed";
        var before = Scalar(connection, $"SELECT sql FROM sqlite_master WHERE name='{name}';");
        var after = before;
        var changes = new List<string[]>();
        string? indexMutation = null;

        void Replace(string oldValue, string newValue)
        {
            if (after.Split(oldValue, StringSplitOptions.None).Length != 2)
            {
                throw new InvalidOperationException($"P22_MUTATION_ANCHOR_NOT_UNIQUE: {identity}: {oldValue}");
            }
            changes.Add([oldValue, newValue]);
            after = after.Replace(oldValue, newValue, StringComparison.Ordinal);
        }

        switch (identity)
        {
            case "source_offset=NULL":
                Replace("AND source_offset IS NOT NULL", "AND 1=1");
                break;
            case "source_end=NULL":
                Replace("AND source_end IS NOT NULL", "AND 1=1");
                break;
            case "source_offset='oops'":
                Replace("typeof(source_offset)='integer'", "typeof(source_offset) IN ('integer','text')");
                // Text offsets also fail the independently enforced interval comparison.
                Replace("source_end > source_offset", "(typeof(source_offset)='text' OR source_end > source_offset)");
                break;
            case "source_end='oops'":
                Replace("typeof(source_end)='integer'", "typeof(source_end) IN ('integer','text')");
                break;
            case "source_offset=1.5":
                Replace("typeof(source_offset)='integer'", "typeof(source_offset) IN ('integer','real')");
                break;
            case "source_end=2.5":
                Replace("typeof(source_end)='integer'", "typeof(source_end) IN ('integer','real')");
                break;
            case "source_offset=-1":
                Replace("source_offset >= 0", "source_offset >= -1");
                break;
            case "source_end=1":
                Replace("source_end > source_offset", "source_end >= source_offset");
                break;
            case "raw_digest=NULL":
                Replace("typeof(raw_digest)='text'", "(raw_digest IS NULL OR typeof(raw_digest)='text')");
                break;
            case "raw_digest=''":
                Replace("length(raw_digest)>0", "length(raw_digest)>=0");
                break;
            case "raw_digest=X'01'":
                Replace("typeof(raw_digest)='text'", "typeof(raw_digest) IN ('text','blob')");
                break;
            case "admission_n=NULL":
                Replace("AND admission_n IS NOT NULL", "AND 1=1");
                break;
            case "admission_n=0":
                Replace("admission_n > 0", "admission_n >= 0");
                break;
            case "admission_n=1.5":
                Replace("typeof(admission_n)='integer'", "typeof(admission_n) IN ('integer','real')");
                break;
            case "admission_n='oops'":
                Replace("typeof(admission_n)='integer'", "typeof(admission_n) IN ('integer','text')");
                Replace("n > admission_n", "(typeof(admission_n)='text' OR n > admission_n)");
                break;
            case "application_state='applied'":
                Replace("AND application_state IS NULL", "AND (application_state IS NULL OR application_state='applied')");
                break;
            case "session_id='S',session_generation=1":
                Replace("AND session_id IS NULL AND session_generation IS NULL",
                    "AND ((session_id IS NULL AND session_generation IS NULL) OR (session_id='S' AND session_generation=1))");
                break;
            case "message_id='M'":
                Replace("AND message_id IS NULL", "AND (message_id IS NULL OR message_id='M')");
                break;
            case "parent_event_key='P'":
                Replace("AND parent_event_key IS NULL", "AND (parent_event_key IS NULL OR parent_event_key='P')");
                break;
            case "parent_application_state='applied'":
                Replace("AND parent_application_state IS NULL",
                    "AND (parent_application_state IS NULL OR parent_application_state='applied')");
                break;
            case "is_initial=1":
                Replace("AND is_initial=0", "AND is_initial IN (0,1)");
                Replace("(is_initial = 0 AND admission_n IS NOT NULL",
                    "((is_initial = 0 OR outcome='duplicate-occurrence-accounted') AND admission_n IS NOT NULL");
                // The first admission already occupies the unique initial-event key.
                // Keep uniqueness for semantic admissions, excluding only diagnostics.
                indexMutation = """
                    DROP INDEX ix_coord_projection_initial;
                    CREATE UNIQUE INDEX ix_coord_projection_initial
                    ON coord_projection_feed(scope,epoch,event_key)
                    WHERE is_initial=1 AND outcome<>'duplicate-occurrence-accounted';
                    """;
                break;
            case "insertion-lookup-scan":
                Replace("WHERE scope=NEW.scope AND epoch<NEW.epoch",
                    "WHERE +scope=NEW.scope AND epoch<NEW.epoch");
                break;
            case "semantic-generation-overstrict":
                Replace("session_generation > 0", "session_generation > 1");
                break;
            default:
                throw new InvalidOperationException($"P22_UNKNOWN_MUTANT: {identity}");
        }

        if (name == "coord_event_insert")
        {
            Execute(connection, "DROP TRIGGER coord_event_insert;");
            Execute(connection, after);
        }
        else
        {
            var foreignKeys = Scalar(connection, "PRAGMA foreign_keys;");
            var recursiveTriggers = Scalar(connection, "PRAGMA recursive_triggers;");
            var version = int.Parse(Scalar(connection, "PRAGMA schema_version;"), CultureInfo.InvariantCulture);
            Execute(connection, "PRAGMA writable_schema=ON;");
            try
            {
                using var update = connection.CreateCommand();
                update.CommandText = "UPDATE sqlite_master SET sql=$sql WHERE type='table' AND name='coord_projection_feed';";
                update.Parameters.AddWithValue("$sql", after);
                if (update.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("P22_MUTATION_TABLE_MISSING");
                }
                Execute(connection, $"PRAGMA schema_version={version + 1};");
            }
            finally
            {
                Execute(connection, "PRAGMA writable_schema=OFF;");
            }
            connection.Close();
            connection.Open();
            Execute(connection, $"PRAGMA foreign_keys={foreignKeys}; PRAGMA recursive_triggers={recursiveTriggers};");
            if (indexMutation is not null)
            {
                Execute(connection, indexMutation);
            }
        }

        if (Scalar(connection, $"SELECT sql FROM sqlite_master WHERE name='{name}';") != after)
        {
            throw new InvalidOperationException("P22_MUTATION_READBACK_MISMATCH");
        }
        output.WriteLine("P22_MUTATION " + JsonSerializer.Serialize(new
        {
            identity,
            target = name,
            changes,
            indexMutation,
            before,
            after,
            beforeSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(before))),
            afterSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(after))),
            sqlite = Scalar(connection, "SELECT sqlite_version();"),
            provider = typeof(SqliteConnection).Assembly.GetName().Version?.ToString()
        }));
    }

    private static string Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture)!;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
