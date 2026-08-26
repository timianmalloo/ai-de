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

    /// <summary>All current assertions across every scope that has a complete snapshot.</summary>
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
