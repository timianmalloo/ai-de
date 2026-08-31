using Microsoft.Data.Sqlite;

namespace AiDe.Core.Watcher;

/// <summary>
/// The durable <see cref="IWatcherObservationStore"/> on one SQLite file, reusing the ADR-0002 fact-store
/// idiom (WAL, append-only facts enforced by triggers, a single writer). It substitutes for
/// <see cref="InMemoryWatcherObservationStore"/> behind the same seam - the same contract, now persisted
/// across a restart. Spans are an append-only fact (dedup by content-addressed primary key); sessions,
/// heartbeats, and the ended flag are current-state cells (upsert), mirroring the in-memory maps.
///
/// simplify: one connection guarded by a lock. Ceiling: fine at the reference scale for the skeleton.
/// Upgrade trigger: read volume grows enough to want the WorkspaceStore read/write connection split.
/// </summary>
public sealed class SqliteWatcherObservationStore : IWatcherObservationStore, IDisposable
{
    private const int SchemaVersion = 1;

    private readonly SqliteConnection _connection;
    private readonly object _gate = new();
    private bool _disposed;

    private SqliteWatcherObservationStore(SqliteConnection connection, string databasePath)
    {
        _connection = connection;
        DatabasePath = databasePath;
    }

    /// <summary>The backing database file. Exposed so a test can open a raw connection against it.</summary>
    public string DatabasePath { get; }

    public static SqliteWatcherObservationStore Open(string databasePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(databasePath);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        Execute(connection, "PRAGMA journal_mode=WAL;");
        Execute(connection, "PRAGMA foreign_keys=ON;");
        // recursive_triggers=ON so an INSERT path cannot slip past the append-only triggers (ADR-0002 S4).
        Execute(connection, "PRAGMA recursive_triggers=ON;");
        EnsureSchema(connection);

        return new SqliteWatcherObservationStore(connection, databasePath);
    }

    public bool TryAppendSpan(ObservedSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);
        lock (_gate)
        {
            // INSERT OR IGNORE: a primary-key conflict is a duplicate to ignore idempotently, and -
            // unlike INSERT OR REPLACE - it never fires the BEFORE UPDATE trigger, so append-only holds.
            var affected = ExecuteNonQuery(
                _connection,
                """
                INSERT OR IGNORE INTO observed_span_fact
                    (span_id, session_id, trace_id, source_span_id, operation_name, recorded_at)
                VALUES ($id, $session, $trace, $source, $op, $recorded);
                """,
                ("$id", span.SpanId),
                ("$session", span.SessionId),
                ("$trace", span.TraceId),
                ("$source", span.SourceSpanId),
                ("$op", span.OperationName),
                ("$recorded", span.RecordedAt.ToUniversalTime().ToString("O")));
            return affected > 0;
        }
    }

    public int SpanCount(string sessionId)
    {
        lock (_gate)
        {
            var count = ExecuteScalar(
                _connection,
                "SELECT count(*) FROM observed_span_fact WHERE session_id = $session;",
                ("$session", sessionId));
            return Convert.ToInt32(count);
        }
    }

    public void UpsertHeartbeat(string sessionId, long monotonicTicks)
    {
        lock (_gate)
        {
            ExecuteNonQuery(
                _connection,
                """
                INSERT INTO session_heartbeat (session_id, monotonic_ticks) VALUES ($session, $ticks)
                ON CONFLICT(session_id) DO UPDATE SET monotonic_ticks = $ticks;
                """,
                ("$session", sessionId),
                ("$ticks", monotonicTicks));
        }
    }

    public long? LastHeartbeat(string sessionId)
    {
        lock (_gate)
        {
            var value = ExecuteScalar(
                _connection,
                "SELECT monotonic_ticks FROM session_heartbeat WHERE session_id = $session;",
                ("$session", sessionId));
            return value is null or DBNull ? null : Convert.ToInt64(value);
        }
    }

    public void RecordSession(SessionRecord session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var b = session.Binding;
        lock (_gate)
        {
            // Upsert the current session metadata, preserving heartbeat/ended which live in their own
            // tables - a dimension row, not an append-only fact.
            ExecuteNonQuery(
                _connection,
                """
                INSERT INTO agent_session_dim
                    (session_id, generation, repo_path, repo_display, worktree_branch, worktree_path,
                     terminal_id, agent_name, harness_name, harness_version, model_name, model_version, trust)
                VALUES ($id, $gen, $repoPath, $repoDisplay, $wtBranch, $wtPath,
                        $terminal, $agent, $hName, $hVer, $mName, $mVer, $trust)
                ON CONFLICT(session_id) DO UPDATE SET
                    generation = $gen, repo_path = $repoPath, repo_display = $repoDisplay,
                    worktree_branch = $wtBranch, worktree_path = $wtPath, terminal_id = $terminal,
                    agent_name = $agent, harness_name = $hName, harness_version = $hVer,
                    model_name = $mName, model_version = $mVer, trust = $trust;
                """,
                ("$id", session.SessionId),
                ("$gen", session.Generation.Value),
                ("$repoPath", b.Repository.CanonicalPath),
                ("$repoDisplay", b.Repository.DisplayName),
                ("$wtBranch", b.Worktree.Branch),
                ("$wtPath", b.Worktree.Path),
                ("$terminal", b.Terminal.TerminalId),
                ("$agent", b.Agent.AgentName),
                ("$hName", (object?)b.Harness?.Name ?? DBNull.Value),
                ("$hVer", (object?)b.Harness?.Version ?? DBNull.Value),
                ("$mName", (object?)b.Model?.Name ?? DBNull.Value),
                ("$mVer", (object?)b.Model?.Version ?? DBNull.Value),
                ("$trust", b.Trust.ToString()));
        }
    }

    public SessionRecord? FindSession(string sessionId)
    {
        lock (_gate)
        {
            using var command = _connection.CreateCommand();
            command.CommandText =
                """
                SELECT generation, repo_path, repo_display, worktree_branch, worktree_path, terminal_id,
                       agent_name, harness_name, harness_version, model_name, model_version, trust
                FROM agent_session_dim WHERE session_id = $session;
                """;
            command.Parameters.AddWithValue("$session", sessionId);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var repo = new RepositoryIdentity(reader.GetString(1), reader.GetString(2));
            var binding = new SessionBinding(
                repo,
                new WorktreeIdentity(repo, reader.GetString(3), reader.GetString(4)),
                new TerminalIdentity(reader.GetString(5)),
                new AgentIdentity(reader.GetString(6)),
                reader.IsDBNull(7) ? null : new HarnessIdentity(reader.GetString(7), reader.GetString(8)),
                reader.IsDBNull(9) ? null : new ModelIdentity(reader.GetString(9), reader.GetString(10)),
                Enum.Parse<TrustClassification>(reader.GetString(11)));
            return new SessionRecord(sessionId, new SessionGeneration(reader.GetInt64(0)), binding);
        }
    }

    public void MarkEnded(string sessionId)
    {
        lock (_gate)
        {
            ExecuteNonQuery(
                _connection,
                "INSERT OR IGNORE INTO session_ended (session_id) VALUES ($session);",
                ("$session", sessionId));
        }
    }

    public void ClearEnded(string sessionId)
    {
        lock (_gate)
        {
            ExecuteNonQuery(
                _connection,
                "DELETE FROM session_ended WHERE session_id = $session;",
                ("$session", sessionId));
        }
    }

    public bool IsEnded(string sessionId)
    {
        lock (_gate)
        {
            var value = ExecuteScalar(
                _connection,
                "SELECT EXISTS(SELECT 1 FROM session_ended WHERE session_id = $session);",
                ("$session", sessionId));
            return Convert.ToInt64(value) != 0;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        var exists = Convert.ToInt64(ExecuteScalar(
            connection,
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='watcher_schema_version';")) > 0;
        if (exists)
        {
            return;
        }

        using var tx = connection.BeginTransaction();
        ExecuteNonQuery(connection, SchemaSql, tx);
        ExecuteNonQuery(
            connection,
            "INSERT INTO watcher_schema_version (version, applied_at) VALUES ($v, $t);",
            tx,
            ("$v", SchemaVersion),
            ("$t", DateTimeOffset.UtcNow.ToString("O")));
        tx.Commit();
    }

    private const string SchemaSql =
        """
        CREATE TABLE watcher_schema_version (
            version    INTEGER NOT NULL PRIMARY KEY,
            applied_at TEXT    NOT NULL
        );

        -- Append-only observation fact. The content-addressed span_id is the primary key, so a
        -- redelivered span is a no-op INSERT OR IGNORE. Grain: one observed operation, once.
        CREATE TABLE observed_span_fact (
            span_id        TEXT NOT NULL PRIMARY KEY,
            session_id     TEXT NOT NULL,
            trace_id       TEXT NOT NULL,
            source_span_id TEXT NOT NULL,
            operation_name TEXT NOT NULL,
            recorded_at    TEXT NOT NULL
        );
        CREATE INDEX ix_observed_span_session ON observed_span_fact (session_id);

        -- Append-only enforcement (DM11): a fact cannot be updated or deleted in place.
        CREATE TRIGGER observed_span_fact_no_update BEFORE UPDATE ON observed_span_fact
        BEGIN SELECT RAISE(ABORT, 'observed_span_fact is append-only'); END;
        CREATE TRIGGER observed_span_fact_no_delete BEFORE DELETE ON observed_span_fact
        BEGIN SELECT RAISE(ABORT, 'observed_span_fact is append-only'); END;

        -- Current-state session metadata (a dimension; harness/model nullable => Not Recorded).
        CREATE TABLE agent_session_dim (
            session_id      TEXT    NOT NULL PRIMARY KEY,
            generation      INTEGER NOT NULL,
            repo_path       TEXT    NOT NULL,
            repo_display    TEXT    NOT NULL,
            worktree_branch TEXT    NOT NULL,
            worktree_path   TEXT    NOT NULL,
            terminal_id     TEXT    NOT NULL,
            agent_name      TEXT    NOT NULL,
            harness_name    TEXT    NULL,
            harness_version TEXT    NULL,
            model_name      TEXT    NULL,
            model_version   TEXT    NULL,
            trust           TEXT    NOT NULL
        );

        -- Latest heartbeat per session (monotonic ticks). Liveness is derived from this, never stored.
        CREATE TABLE session_heartbeat (
            session_id      TEXT    NOT NULL PRIMARY KEY,
            monotonic_ticks INTEGER NOT NULL
        );

        -- The ended flag. Presence means ended; ClearEnded removes it for a fresh generation.
        CREATE TABLE session_ended (
            session_id TEXT NOT NULL PRIMARY KEY
        );
        """;

    private static void Execute(SqliteConnection connection, string sql)
        => ExecuteNonQuery(connection, sql);

    private static int ExecuteNonQuery(
        SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
        => ExecuteNonQuery(connection, sql, null, parameters);

    private static int ExecuteNonQuery(
        SqliteConnection connection, string sql, SqliteTransaction? tx,
        params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = tx;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command.ExecuteNonQuery();
    }

    private static object? ExecuteScalar(
        SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command.ExecuteScalar();
    }
}
