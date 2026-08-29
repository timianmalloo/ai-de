using System.Globalization;
using AiDe.Core.Facts;
using Microsoft.Data.Sqlite;

namespace AiDe.Core.Store;

/// <summary>
/// A snapshot read. The connection is pinned <c>query_only=1</c>, so this path cannot write even by
/// accident (spike S6).
/// </summary>
public sealed class StoreReader : IDisposable
{
    private readonly SqliteConnection _connection;

    internal StoreReader(SqliteConnection connection) => _connection = connection;

    /// <summary>The latest committed generation for a scope, or null if nothing complete exists yet.</summary>
    public (long Generation, string ArtifactRevision, int AssertionCount)? LatestCommittedSnapshot(string scopeId)
    {
        using var command = Command("""
            SELECT generation, artifact_revision, assertion_count
            FROM scope_snapshot_committed_fact
            WHERE scope_id = $scope AND complete = 1
            ORDER BY generation DESC LIMIT 1;
            """, ("$scope", scopeId));
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)) : null;
    }

    /// <summary>
    /// Current evidence for a scope: assertions of the latest COMPLETE snapshot only. A partial or
    /// superseded snapshot never contributes, so the graph cannot silently mix generations.
    /// </summary>
    public IReadOnlyList<StoredAssertion> CurrentAssertions(string scopeId)
    {
        var latest = LatestCommittedSnapshot(scopeId);
        if (latest is null)
        {
            return [];
        }

        using var command = Command("""
            SELECT assertion_id, scope_id, artifact_revision, subject, predicate, object, origin, status,
                   artifact_path_id, source_location, extractor_id, extractor_version, observed_at
            FROM evidence_assertion_fact
            WHERE scope_id = $scope AND generation = $gen
            ORDER BY subject, predicate, object;
            """, ("$scope", scopeId), ("$gen", latest.Value.Generation));
        return ReadAssertions(command);
    }

    /// <summary>
    /// The "current generation" filter every bounded read composes with: one row per scope, so the
    /// join stays tiny while the traversal predicate drives the index.
    /// </summary>
    private const string LatestCte = """
        WITH latest AS (
            SELECT scope_id, max(generation) AS generation
            FROM scope_snapshot_committed_fact WHERE complete = 1 GROUP BY scope_id
        )
        """;

    private const string AssertionColumns = """
        a.assertion_id, a.scope_id, a.artifact_revision, a.subject, a.predicate, a.object,
        a.origin, a.status, a.artifact_path_id, a.source_location, a.extractor_id,
        a.extractor_version, a.observed_at
        """;

    /// <summary>Assertions where the node is the subject OR the object, bounded in SQL.</summary>
    /// <remarks>
    /// Deliberately a UNION ALL of two single-column lookups rather than one <c>OR</c> predicate:
    /// SQLite will not use two different indexes to satisfy one OR, so the OR form degrades into
    /// the full scan this method exists to avoid (measured, P1-PERF 2026-08-26).
    /// </remarks>
    public IReadOnlyList<StoredAssertion> AssertionsTouching(string nodeId, int limit)
    {
        using var command = Command($"""
            {LatestCte}
            SELECT * FROM (
                SELECT {AssertionColumns} FROM evidence_assertion_fact a
                JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
                WHERE a.subject = $node
                UNION ALL
                SELECT {AssertionColumns} FROM evidence_assertion_fact a
                JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
                WHERE a.object = $node AND a.subject <> $node
            )
            ORDER BY subject, predicate, object
            LIMIT $limit;
            """, ("$node", nodeId), ("$limit", limit));
        return ReadAssertions(command);
    }

    /// <summary>Total assertions touching a node, so a bounded read can report what it omitted.</summary>
    public int CountAssertionsTouching(string nodeId)
    {
        using var command = Command($"""
            {LatestCte}
            SELECT
              (SELECT count(*) FROM evidence_assertion_fact a
                 JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
                 WHERE a.subject = $node)
            + (SELECT count(*) FROM evidence_assertion_fact a
                 JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
                 WHERE a.object = $node AND a.subject <> $node);
            """, ("$node", nodeId));
        return Convert.ToInt32(command.ExecuteScalar());
    }

    /// <summary>Outgoing edges only — one traversal step of a bounded impact walk.</summary>
    public IReadOnlyList<StoredAssertion> OutgoingAssertions(string nodeId, int limit)
    {
        using var command = Command($"""
            {LatestCte}
            SELECT {AssertionColumns} FROM evidence_assertion_fact a
            JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
            WHERE a.subject = $node
            ORDER BY a.object
            LIMIT $limit;
            """, ("$node", nodeId), ("$limit", limit));
        return ReadAssertions(command);
    }

    /// <summary>Assertions with a given predicate — the knowledge projection's entry point.</summary>
    public IReadOnlyList<StoredAssertion> AssertionsWithPredicate(string predicate, int limit)
    {
        using var command = Command($"""
            {LatestCte}
            SELECT {AssertionColumns} FROM evidence_assertion_fact a
            JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
            WHERE a.predicate = $predicate
            ORDER BY a.subject
            LIMIT $limit;
            """, ("$predicate", predicate), ("$limit", limit));
        return ReadAssertions(command);
    }

    /// <summary>
    /// Node identities matching a substring, with the total matched so omissions are reportable.
    /// </summary>
    /// <remarks>
    /// A leading-wildcard LIKE cannot use an index, so this selects only the identity columns: it
    /// scans a covering index rather than hydrating every row's provenance, which is what made the
    /// naive version cost a full-corpus materialization.
    /// </remarks>
    /// <summary>
    /// Subjects this workspace's own artifacts DECLARE — the things it owns.
    /// </summary>
    /// <remarks>
    /// Distinct from every node in the graph, which also contains external package types a
    /// repository merely depends on. Any denominator that counts those is measuring the wrong
    /// population — bounded-context coverage above all, because nobody can assign
    /// <c>Azure.Storage.Blobs.BlobClient</c> to a context in their own codebase.
    /// </remarks>
    public IReadOnlyList<string> ReadDeclaredSubjects()
    {
        using var command = Command($"""
            {LatestCte}
            SELECT DISTINCT a.subject FROM evidence_assertion_fact a
            JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
            WHERE a.predicate = 'declared_in'
            ORDER BY a.subject;
            """);

        using var reader = command.ExecuteReader();
        var subjects = new List<string>();
        while (reader.Read()) subjects.Add(reader.GetString(0));
        return subjects;
    }

    public (IReadOnlyList<string> Matches, int TotalMatched) SearchNodeIds(string term, int limit)
    {
        using var command = Command($"""
            {LatestCte}
            SELECT DISTINCT id FROM (
                SELECT a.subject AS id FROM evidence_assertion_fact a
                JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
                WHERE a.subject LIKE $pattern
                UNION
                SELECT a.object AS id FROM evidence_assertion_fact a
                JOIN latest l ON l.scope_id = a.scope_id AND l.generation = a.generation
                WHERE a.object LIKE $pattern
                  -- An attribute's object is a VALUE. Without this, api_version puts dates in the
                  -- graph and resource_name_expression puts unevaluated strings there.
                  AND a.predicate NOT IN ({AiDe.Core.Facts.EvidencePredicates.SqlList})
            )
            ORDER BY id;
            """, ("$pattern", $"%{term}%"));

        using var reader = command.ExecuteReader();
        var all = new List<string>();
        while (reader.Read())
        {
            all.Add(reader.GetString(0));
        }

        return (all.Count > limit ? all[..limit] : all, all.Count);
    }

    /// <summary>
    /// The highest generation any scope has ever been asked for, or 0 for an empty store.
    /// </summary>
    /// <remarks>
    /// The in-memory counter starts at zero on every open, so without this a workspace's SECOND
    /// index after a restart re-uses generation 1 and violates the desired-generation primary key.
    /// The daemon opens the store fresh every time it starts, which made "index, restart, index"
    /// a guaranteed failure — found by a test that indexed twice across a reopen, which nothing had
    /// done before.
    /// </remarks>
    public long HighestDesiredGeneration()
    {
        using var command = Command("SELECT COALESCE(MAX(generation), 0) FROM scope_generation_desired_fact;");
        var value = command.ExecuteScalar();
        return value is null or DBNull ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <summary>The source revision currently rendered, for a result's provenance header.</summary>
    public string CurrentSourceRevision()
    {
        using var command = Command("""
            SELECT artifact_revision FROM scope_snapshot_committed_fact
            WHERE complete = 1 ORDER BY generation DESC, scope_id LIMIT 1;
            """);
        return command.ExecuteScalar() as string ?? "none";
    }

    /// <summary>
    /// All current assertions across every scope that has a complete snapshot.
    /// </summary>
    /// <remarks>
    /// A deliberate full read, used only where the whole set IS the answer — the claim-cache rebuild.
    /// Bounded reads must never call this: at 50,000 edges it costs roughly 350 ms of materialization
    /// no matter how small the caller's result is (measured, P1-PERF 2026-08-26).
    /// </remarks>
    /// <summary>
    /// A page of the current assertions, ordered stably, starting after <paramref name="after"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Paged because the caller is across a pipe.</b> The panes want every current
    /// assertion — 12,085 of them on one real repository — and they were reconstructing that set one
    /// node at a time through <c>Describe</c>, which is bounded at 50 neighbours per node and lost
    /// two join edges out of 124 doing it. A single unbounded response would blow the result-byte
    /// cap instead, so the answer is neither: pages, with a cursor.</para>
    ///
    /// <para>The cursor is the last row's <c>(subject, predicate, object)</c> — the same tuple the
    /// ORDER BY uses, so a page boundary cannot skip or repeat a row. An id-based cursor would order
    /// by something the query does not, which is how paging quietly loses records.</para>
    /// </remarks>
    public IReadOnlyList<StoredAssertion> CurrentAssertionPage(
        (string Subject, string Predicate, string Object)? after, int limit)
    {
        var sql = """
            SELECT a.assertion_id, a.scope_id, a.artifact_revision, a.subject, a.predicate, a.object,
                   a.origin, a.status, a.artifact_path_id, a.source_location, a.extractor_id,
                   a.extractor_version, a.observed_at
            FROM evidence_assertion_fact a
            JOIN (
                SELECT scope_id, max(generation) AS generation
                FROM scope_snapshot_committed_fact WHERE complete = 1 GROUP BY scope_id
            ) latest ON latest.scope_id = a.scope_id AND latest.generation = a.generation
            """;

        sql += after is null
            ? " ORDER BY a.subject, a.predicate, a.object LIMIT $limit;"
            : """
               WHERE (a.subject, a.predicate, a.object) > ($s, $p, $o)
               ORDER BY a.subject, a.predicate, a.object LIMIT $limit;
              """;

        using var command = after is null
            ? Command(sql, ("$limit", limit))
            : Command(sql, ("$s", after.Value.Subject), ("$p", after.Value.Predicate),
                      ("$o", after.Value.Object), ("$limit", limit));

        return ReadAssertions(command);
    }

    public IReadOnlyList<StoredAssertion> AllCurrentAssertions()
    {
        using var command = Command("""
            SELECT a.assertion_id, a.scope_id, a.artifact_revision, a.subject, a.predicate, a.object,
                   a.origin, a.status, a.artifact_path_id, a.source_location, a.extractor_id,
                   a.extractor_version, a.observed_at
            FROM evidence_assertion_fact a
            JOIN (
                SELECT scope_id, max(generation) AS generation
                FROM scope_snapshot_committed_fact WHERE complete = 1 GROUP BY scope_id
            ) latest ON latest.scope_id = a.scope_id AND latest.generation = a.generation
            ORDER BY a.subject, a.predicate, a.object;
            """);
        return ReadAssertions(command);
    }

    /// <summary>Folds a dispatch key's attempt + outcome events into one displayed receipt.</summary>
    public DispatchReceipt? ReadDispatchReceipt(string dispatchKey)
    {
        using var command = Command("""
            SELECT a.dispatch_key, a.session_id, a.session_generation, a.attempted_at,
                   (SELECT state      FROM dispatch_outcome_fact o WHERE o.dispatch_key = a.dispatch_key
                     ORDER BY o.ingress_seq DESC LIMIT 1),
                   (SELECT error_code FROM dispatch_outcome_fact o WHERE o.dispatch_key = a.dispatch_key
                     ORDER BY o.ingress_seq DESC LIMIT 1)
            FROM dispatch_attempt_fact a WHERE a.dispatch_key = $key;
            """, ("$key", dispatchKey));
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        // No outcome event yet => the attempt is still Pending. Recovery resolves it, never a caller.
        var state = reader.IsDBNull(4)
            ? DispatchState.Pending
            : Enum.Parse<DispatchState>(reader.GetString(4));

        return new DispatchReceipt(
            reader.GetString(0), state, reader.GetString(1), reader.GetInt64(2),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            DateTimeOffset.Parse(reader.GetString(3)));
    }

    /// <summary>Dispatch keys with an attempt but no outcome — what recovery must resolve.</summary>
    public IReadOnlyList<string> PendingDispatchKeys()
    {
        using var command = Command("""
            SELECT a.dispatch_key FROM dispatch_attempt_fact a
            WHERE NOT EXISTS (SELECT 1 FROM dispatch_outcome_fact o WHERE o.dispatch_key = a.dispatch_key)
            ORDER BY a.ingress_seq;
            """);
        using var reader = command.ExecuteReader();
        var keys = new List<string>();
        while (reader.Read())
        {
            keys.Add(reader.GetString(0));
        }

        return keys;
    }

    public string? ReadCommandOutcome(string workspaceId, CallerPrincipal caller, string commandType, string commandId)
    {
        using var command = Command("""
            SELECT outcome FROM command_receipt_fact
            WHERE workspace_id = $ws AND caller_principal = $caller
              AND command_type = $type AND command_id = $id;
            """,
            ("$ws", workspaceId), ("$caller", caller.Id), ("$type", commandType), ("$id", commandId));
        return command.ExecuteScalar() as string;
    }

    public (long Generation, SessionProcessingClass ProcessingClass)? ReadSession(string sessionId)
    {
        using var command = Command("""
            SELECT generation, processing_class FROM session_dim
            WHERE session_id = $id AND valid_to_seq IS NULL;
            """, ("$id", sessionId));
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? (reader.GetInt64(0), Enum.Parse<SessionProcessingClass>(reader.GetString(1)))
            : null;
    }

    public string? ReadNodeKind(string nodeId)
        => Command("SELECT node_kind FROM node_dim WHERE node_id = $id AND valid_to_seq IS NULL;", ("$id", nodeId))
            .ExecuteScalar() as string;

    public string? ReadNodeLabel(string nodeId)
        => Command("SELECT display_label FROM node_dim WHERE node_id = $id AND valid_to_seq IS NULL;", ("$id", nodeId))
            .ExecuteScalar() as string;

    /// <summary>Reads the labelled cache. Provably equal to its derivation — see the rebuild test.</summary>
    public IReadOnlyList<(string Subject, string Predicate, string Object, string Status, int Count, string Revision)>
        ReadClaimCache()
    {
        using var command = Command("""
            SELECT subject, predicate, object, status, assertion_count, source_revision
            FROM claim_current_cache ORDER BY subject, predicate, object;
            """);
        using var reader = command.ExecuteReader();
        var rows = new List<(string, string, string, string, int, string)>();
        while (reader.Read())
        {
            rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetInt32(4), reader.GetString(5)));
        }

        return rows;
    }

    internal SqliteCommand Command(string sql, params (string Name, object? Value)[] parameters)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    private static IReadOnlyList<StoredAssertion> ReadAssertions(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var results = new List<StoredAssertion>();
        while (reader.Read())
        {
            results.Add(new StoredAssertion(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetString(5),
                Enum.Parse<EvidenceOrigin>(reader.GetString(6)),
                Enum.Parse<VerificationStatus>(reader.GetString(7)),
                new Provenance(
                    reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9),
                    reader.GetString(10), reader.GetString(11),
                    DateTimeOffset.Parse(reader.GetString(12)))));
        }

        return results;
    }

    public void Dispose() => _connection.Dispose();
}

/// <summary>An assertion as stored, carrying its computed identity back out.</summary>
public sealed record StoredAssertion(
    string AssertionId,
    string ScopeId,
    string ArtifactRevision,
    string Subject,
    string Predicate,
    string Object,
    EvidenceOrigin Origin,
    VerificationStatus Status,
    Provenance Provenance);
