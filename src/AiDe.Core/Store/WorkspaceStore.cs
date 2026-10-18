using AiDe.Core.Upgrade;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Store;

/// <summary>Stable error codes this layer can raise. Never a bare exception message.</summary>
public static class StoreErrorCodes
{
    public const string ImmutableViolation = "AIDE-STORE-IMMUTABLE-VIOLATION";
    public const string ScopeGenerationStale = "AIDE-SCOPE-GENERATION-STALE";
    public const string EpochStale = "AIDE-AUTH-EPOCH-STALE";
    public const string VocabularyViolation = "AIDE-STORE-VOCABULARY-VIOLATION";
    public const string ReferenceViolation = "AIDE-STORE-REFERENCE-VIOLATION";
    public const string Type0Violation = "AIDE-STORE-TYPE0-VIOLATION";
    public const string NewerSchemaReadOnly = "AIDE-STORE-NEWER-SCHEMA-READ-ONLY";
    public const string MigrationFailed = "AIDE-STORE-MIGRATION-FAILED";
    public const string VersionOrderViolation = "AIDE-STORE-VERSION-ORDER-VIOLATION";
    public const string BodyHashRequired = "AIDE-STORE-BODY-HASH-REQUIRED";
    public const string DerivedStateInPayload = "AIDE-STORE-DERIVED-STATE-IN-PAYLOAD";
}

/// <summary>A store-layer failure carrying a stable, greppable code.</summary>
public sealed class WorkspaceStoreException(string errorCode, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public string ErrorCode { get; } = errorCode;
}

/// <summary>What opening the store found and did: the file's version, the binary's, and any migration.</summary>
/// <param name="FileVersion">The highest <c>schema_version</c> the file records.</param>
/// <param name="BinaryVersion">The version this binary writes.</param>
/// <param name="ReadOnly">True when the file is newer than the binary: it can be read, never written.</param>
/// <param name="MigratedFromVersion">The version a migration started from, or null if none ran.</param>
/// <param name="LegacyNotebooksBackfilled">Pre-workbench drafts surfaced as unowned legacy notebooks; the human is told.</param>
/// <param name="Summary">One sentence for a log or a banner.</param>
public sealed record StoreOpenReport(
    int FileVersion,
    int BinaryVersion,
    bool ReadOnly,
    int? MigratedFromVersion,
    int LegacyNotebooksBackfilled,
    string Summary);

/// <summary>
/// One workspace, one SQLite file. Owns the single writer, the core epoch, and the total fact order.
/// </summary>
/// <remarks>
/// Pattern: Repository + Unit of Work (narrow). SQLite has no nested transactions
/// (spikes/sqlite-fact-store S8), so one write transaction per aggregate operation is the contract,
/// enforced by the writer semaphore rather than an ambient transaction scope.
/// </remarks>
public sealed class WorkspaceStore : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection? _writeConnection;
    private readonly SemaphoreSlim _writerGate = new(1, 1);
    private bool _disposed;

    private WorkspaceStore(string connectionString, SqliteConnection? writeConnection, long coreEpoch, StoreOpenReport report)
    {
        _connectionString = connectionString;
        _writeConnection = writeConnection;
        CoreEpoch = coreEpoch;
        Report = report;
    }

    /// <summary>
    /// Monotonic, store-persisted, incremented inside the open transaction. Never random and never
    /// clock-derived, so "stale" is decidable and an old epoch value cannot recur (ABA). Zero for a
    /// read-only store: it never opened a writer, so it never advanced the epoch.
    /// </summary>
    public long CoreEpoch { get; }

    /// <summary>What opening found and did. Never null.</summary>
    public StoreOpenReport Report { get; }

    public static WorkspaceStore Open(string databasePath)
    {
        var fullPath = Path.GetFullPath(databasePath);
        var workDirectory = Path.Combine(Path.GetDirectoryName(fullPath)!, "migration");

        // A crash mid-migration leaves a journal in Started: put the snapshot back BEFORE reading
        // the version, or the version read describes a half-migrated file.
        if (File.Exists(fullPath))
        {
            var recovery = UpgradeCoordinator.RecoverIfIncomplete(fullPath, workDirectory);
            if (recovery.Failure is not null)
            {
                throw new WorkspaceStoreException(StoreErrorCodes.MigrationFailed, recovery.Failure);
            }
        }

        var fileVersion = PeekVersion(fullPath);
        if (fileVersion > WorkspaceSchema.Version)
        {
            return OpenReadOnly(fullPath, fileVersion.Value);
        }

        int? migratedFrom = null;
        var legacyNotebooks = 0;
        if (fileVersion is { } from && from < WorkspaceSchema.Version)
        {
            legacyNotebooks = Migrate(fullPath, workDirectory, from);
            migratedFrom = from;
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        ConfigureWriterConnection(connection);

        var created = EnsureSchema(connection);
        var epoch = BumpEpoch(connection, created);

        var report = new StoreOpenReport(
            WorkspaceSchema.Version, WorkspaceSchema.Version, ReadOnly: false, migratedFrom, legacyNotebooks,
            migratedFrom is { } m
                ? $"migrated workspace store v{m} to v{WorkspaceSchema.Version}; {legacyNotebooks} pre-workbench "
                  + "notebook(s) are unassigned (legacy) until adopted"
                : $"workspace store v{WorkspaceSchema.Version}");
        return new WorkspaceStore(connectionString, connection, epoch, report);
    }

    /// <summary>
    /// The file is newer than this binary. Reading is safe (additive schema); writing could silently
    /// drop columns it does not know, so the store is read-only and says why.
    /// </summary>
    private static WorkspaceStore OpenReadOnly(string path, int fileVersion)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
        }.ToString();
        var report = new StoreOpenReport(
            fileVersion, WorkspaceSchema.Version, ReadOnly: true, MigratedFromVersion: null,
            LegacyNotebooksBackfilled: 0,
            $"workspace store is v{fileVersion} but this build writes v{WorkspaceSchema.Version}: "
            + "opened read-only; update the app to write");
        return new WorkspaceStore(connectionString, null, 0, report);
    }

    /// <summary>The highest recorded schema version, or null when the file or the table does not exist yet.</summary>
    private static int? PeekVersion(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            return null;
        }

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
        }.ToString());
        connection.Open();
        var hasTable = Convert.ToInt64(ExecuteScalar(connection,
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='schema_version';")) > 0;
        return hasTable && ExecuteScalar(connection, "SELECT max(version) FROM schema_version;") is long v ? (int)v : null;
    }

    /// <summary>
    /// Runs the migration chain from <paramref name="from"/> through <see cref="UpgradeCoordinator"/>:
    /// WAL-safe snapshot, journal, migrate, health gate, and either keep or put the snapshot back.
    /// Every connection here is closed before the coordinator may restore the file.
    /// </summary>
    private static int Migrate(string path, string workDirectory, int from)
    {
        var legacyNotebooks = 0;
        var result = new UpgradeCoordinator(path, workDirectory).Run(
            $"workspace-v{from}-to-v{WorkspaceSchema.Version}", from, WorkspaceSchema.Version,
            migrate: () => legacyNotebooks = ApplyMigrations(path, from),
            gate: () => MigrationGate(path));

        if (!result.Upgraded)
        {
            throw new WorkspaceStoreException(StoreErrorCodes.MigrationFailed,
                $"migrating the workspace store from v{from} to v{WorkspaceSchema.Version} failed and was rolled back: "
                + (result.Failure ?? "unknown failure"));
        }

        return legacyNotebooks;
    }

    private static int ApplyMigrations(string path, int from)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false,
        }.ToString());
        connection.Open();
        Execute(connection, "PRAGMA foreign_keys=ON;");
        Execute(connection, "PRAGMA recursive_triggers=ON;");

        var legacyNotebooks = 0;
        using var tx = connection.BeginTransaction();
        foreach (var (version, ddl, backfill) in WorkspaceSchema.Migrations.Where(m => m.Version > from).OrderBy(m => m.Version))
        {
            Execute(connection, ddl, tx);
            legacyNotebooks += backfill(connection, tx);
            Execute(connection, "INSERT INTO schema_version (version, applied_at) VALUES ($v, $t);", tx,
                ("$v", version), ("$t", Iso(DateTimeOffset.UtcNow)));
        }

        tx.Commit();
        return legacyNotebooks;
    }

    /// <summary>The fast checks that decide whether a migrated file may be kept.</summary>
    private static HealthGateResult MigrationGate(string path)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
        }.ToString());
        connection.Open();
        Execute(connection, "PRAGMA foreign_keys=ON;");

        return new HealthGate(TimeSpan.FromSeconds(60)).Run(
        [
            new GateStep("schema-version", () =>
            {
                var version = Convert.ToInt32(ExecuteScalar(connection, "SELECT max(version) FROM schema_version;"));
                return version == WorkspaceSchema.Version
                    ? HealthCheck.Pass("schema-version", $"v{version}")
                    : HealthCheck.Fail("schema-version", $"expected v{WorkspaceSchema.Version}, found v{version}");
            }),
            new GateStep("integrity", () =>
            {
                var result = Convert.ToString(ExecuteScalar(connection, "PRAGMA integrity_check;"));
                return result == "ok" ? HealthCheck.Pass("integrity", "ok") : HealthCheck.Fail("integrity", result ?? "no result");
            }),
            new GateStep("foreign-keys", () =>
            {
                var violations = Convert.ToInt64(ExecuteScalar(connection, "SELECT count(*) FROM pragma_foreign_key_check;"));
                return violations == 0
                    ? HealthCheck.Pass("foreign-keys", "no violations")
                    : HealthCheck.Fail("foreign-keys", $"{violations} violation(s)");
            }),
            .. WorkspaceSchema.ReferenceChecks.Select(check => new GateStep(check.Name, () =>
            {
                var violations = Convert.ToInt64(ExecuteScalar(connection, check.CountSql));
                return violations == 0
                    ? HealthCheck.Pass(check.Name, check.Passed)
                    : HealthCheck.Fail(check.Name, $"{violations} {check.Failed}");
            })),
            new GateStep("outcome-states-known", () =>
            {
                var unknown = UnknownOutcomeStates(connection);
                return unknown.Count == 0
                    ? HealthCheck.Pass("outcome-states-known", "every outcome state is in the vocabulary")
                    : HealthCheck.Fail("outcome-states-known",
                        $"unknown outcome state(s): {string.Join(", ", unknown.Select(u => $"'{u}'"))}");
            }),
        ]);
    }

    /// <summary>Distinct stored outcome states the current vocabulary cannot parse (a read would throw on them).</summary>
    private static List<string> UnknownOutcomeStates(SqliteConnection connection)
    {
        var known = Enum.GetNames<AiDe.Core.Facts.DispatchState>().ToHashSet(StringComparer.Ordinal);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT state FROM dispatch_outcome_fact ORDER BY state;";
        using var reader = command.ExecuteReader();
        var unknown = new List<string>();
        while (reader.Read())
        {
            if (!known.Contains(reader.GetString(0)))
            {
                unknown.Add(reader.GetString(0));
            }
        }

        return unknown;
    }

    /// <summary>
    /// Writer pragmas. <c>recursive_triggers=ON</c> is load-bearing, not hygiene: without it
    /// INSERT OR REPLACE bypasses the immutability triggers entirely (spike S4).
    /// </summary>
    private static void ConfigureWriterConnection(SqliteConnection connection)
    {
        Execute(connection, "PRAGMA journal_mode=WAL;");
        Execute(connection, "PRAGMA foreign_keys=ON;");
        Execute(connection, "PRAGMA recursive_triggers=ON;");
    }

    /// <summary>Creates a fresh store: the v1 baseline, then v2 from the same definition the migration runs.</summary>
    private static bool EnsureSchema(SqliteConnection connection)
    {
        var exists = Convert.ToInt64(ExecuteScalar(connection,
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='schema_version';")) > 0;
        if (exists)
        {
            return false;
        }

        using var tx = connection.BeginTransaction();
        Execute(connection, WorkspaceSchema.CreateSql, tx);
        Execute(connection, WorkspaceSchema.V2Sql, tx);
        Execute(connection, WorkspaceSchema.TriggerSql(), tx);
        Execute(connection,
            "INSERT INTO schema_version (version, applied_at) VALUES ($v, $t);", tx,
            ("$v", WorkspaceSchema.Version), ("$t", Iso(DateTimeOffset.UtcNow)));
        Execute(connection, "INSERT INTO core_state (id, core_epoch, ingress_seq) VALUES (1, 0, 0);", tx);
        tx.Commit();
        return true;
    }

    private static long BumpEpoch(SqliteConnection connection, bool freshlyCreated)
    {
        _ = freshlyCreated;
        using var tx = connection.BeginTransaction();
        Execute(connection, "UPDATE core_state SET core_epoch = core_epoch + 1 WHERE id = 1;", tx);
        var epoch = Convert.ToInt64(ExecuteScalar(connection, "SELECT core_epoch FROM core_state WHERE id = 1;", tx));
        tx.Commit();
        return epoch;
    }

    /// <summary>
    /// Acquire the single writer. Blocks until the previous writer completes — the control path
    /// waits rather than erroring, which is the right shape for one local operator.
    /// </summary>
    /// <exception cref="WorkspaceStoreException">The store was opened read-only because the file is newer than this build.</exception>
    public StoreWriter BeginWrite()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_writeConnection is null)
        {
            throw new WorkspaceStoreException(StoreErrorCodes.NewerSchemaReadOnly, Report.Summary);
        }

        _writerGate.Wait();
        try
        {
            return new StoreWriter(_writeConnection, _writerGate);
        }
        catch
        {
            _writerGate.Release();
            throw;
        }
    }

    /// <summary>
    /// A read connection pinned to <c>query_only=1</c>, so a read path physically cannot write
    /// (spike S6) — reads never queue behind the writer.
    /// </summary>
    public StoreReader BeginRead()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        Execute(connection, "PRAGMA foreign_keys=ON;");
        Execute(connection, "PRAGMA query_only=1;");
        return new StoreReader(connection);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _writeConnection?.Dispose();
        _writerGate.Dispose();
        SqliteConnection.ClearAllPools();
    }

    internal static string Iso(DateTimeOffset value) => value.ToUniversalTime().ToString("O");

    internal static void Execute(
        SqliteConnection connection, string sql, SqliteTransaction? tx = null,
        params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = tx;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }

    internal static object? ExecuteScalar(
        SqliteConnection connection, string sql, SqliteTransaction? tx = null,
        params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = tx;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command.ExecuteScalar();
    }
}
