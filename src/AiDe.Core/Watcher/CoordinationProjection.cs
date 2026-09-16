using Microsoft.Data.Sqlite;

namespace AiDe.Core.Watcher;

// Allocators come only from trusted composition. Prepared wire records never contain executable work.
internal sealed record CoordinationAllocators(
    Func<string> SessionId, Func<string> MessageId, long HeartbeatTicks, DateTimeOffset RecordedAt);
internal sealed record CoordinationResult(
    long Admission, string State, string? MessageId, SessionRecord? Session,
    string? ExternalId, CoordContractEvent? Kind, bool Replayed);
internal sealed record CoordinationPageResult(
    bool Stale, CoordinationCheckpoint? Checkpoint, IReadOnlyList<CoordinationResult> Results);
internal enum CoordinationFault { None, BeforeCommit, AfterCommit }
internal sealed record CoordinationEffect(
    string State, string Reason, SessionRecord? Session = null, string? MessageId = null, string? ParentKey = null);

public sealed partial class SqliteWatcherObservationStore
{
    // Internal deterministic fault seam: instance-local, not a callback or a wire-selected option.
    internal CoordinationFault ProjectionFault { get; set; }

    internal IReadOnlyDictionary<string, CoordinationCheckpoint> CoordinationCheckpoints(string root)
    {
        lock (_gate)
        {
            using var command = CoordinationCommand(null, """
                SELECT scope,accepted_offset,prefix_digest FROM coord_projection_checkpoint
                WHERE scope LIKE $root LIMIT 129;
                """, ("$root", root + "%"));
            using var reader = command.ExecuteReader();
            var result = new Dictionary<string, CoordinationCheckpoint>(StringComparer.Ordinal);
            while (reader.Read())
            {
                result.Add(reader.GetString(0), new(reader.GetInt64(1), reader.GetString(2)));
            }
            if (result.Count > 128)
            {
                throw new CoordinationSourceException(CoordinationErrors.Bounds);
            }
            return result;
        }
    }

    internal bool HasCapturedRegistration(ContractRegister registration)
    {
        lock (_gate)
        {
            using var command = CoordinationCommand(null, """
                SELECT 1 FROM coord_projection_event e JOIN coord_projection_feed f ON f.n=e.current_receipt_n
                WHERE e.original_id=$external AND e.canonical_bytes=$canonical
                  AND f.reason='OBSERVED_REGISTER' AND f.application_state='applied' LIMIT 1;
                """, ("$external", registration.ExternalSessionId),
                ("$canonical", CoordinationSourceCapture.Canonical(registration)));
            return command.ExecuteScalar() is not null;
        }
    }

    internal CoordinationPageResult ProjectCoordination(
        CoordinationPage page, CoordinationCheckpoint? expected, CoordinationAllocators allocators)
    {
        lock (_gate)
        {
            using var transaction = _connection.BeginTransaction(deferred: false);
            var checkpoint = ReadCheckpoint(page.Scope, transaction);
            if (checkpoint != expected)
            {
                return new(true, checkpoint, []);
            }
            var results = new List<CoordinationResult>();
            foreach (var record in page.Records)
            {
                results.Add(ProjectRecord(page.Scope, record, allocators, transaction));
            }
            var end = page.Records[^1].End;
            if (end > (checkpoint?.Offset ?? 0))
            {
                // INSERT guards reject replacement before SQLite reaches an upsert's conflict arm.
                var sql = checkpoint is null
                    ? """
                      INSERT INTO coord_projection_checkpoint(scope,epoch,accepted_offset,prefix_digest)
                      VALUES($scope,$epoch,$end,$digest);
                      """
                    : """
                      UPDATE coord_projection_checkpoint SET accepted_offset=$end,prefix_digest=$digest
                      WHERE scope=$scope AND epoch=$epoch;
                      """;
                using var command = CoordinationCommand(transaction, sql,
                    ("$scope", page.Scope), ("$epoch", CoordinationSourceCapture.Epoch),
                    ("$end", end), ("$digest", page.PrefixDigest));
                command.ExecuteNonQuery();
                checkpoint = new(end, page.PrefixDigest);
            }
            FailAt(CoordinationFault.BeforeCommit);
            transaction.Commit();
            FailAt(CoordinationFault.AfterCommit);
            return new(false, checkpoint, results);
        }
    }

    private CoordinationResult ProjectRecord(
        string scope, CoordinationRecord record, CoordinationAllocators allocators, SqliteTransaction transaction)
    {
        using (var query = CoordinationCommand(transaction, """
            SELECT e.first_receipt_n,e.source_offset,e.raw_digest,e.raw_bytes,
                   f.application_state,f.message_id,f.session_id
            FROM coord_projection_event e JOIN coord_projection_feed f ON f.n=e.current_receipt_n
            WHERE e.scope=$scope AND e.epoch=$epoch AND e.event_key=$key;
            """, ("$scope", scope), ("$epoch", CoordinationSourceCapture.Epoch), ("$key", record.Key)))
        {
            long admission = 0;
            long offset = 0;
            string? state = null, message = null, sessionId = null;
            using (var reader = query.ExecuteReader())
            {
                if (reader.Read())
                {
                    if (reader.GetString(2) != record.Digest
                        || (!reader.IsDBNull(3) && !((byte[])reader[3]).AsSpan().SequenceEqual(record.Raw)))
                    {
                        throw new CoordinationSourceException(CoordinationErrors.DuplicateConflict);
                    }
                    admission = reader.GetInt64(0);
                    offset = reader.GetInt64(1);
                    state = reader.GetString(4);
                    message = reader.IsDBNull(5) ? null : reader.GetString(5);
                    sessionId = reader.IsDBNull(6) ? null : reader.GetString(6);
                }
            }
            if (admission != 0)
            {
                if (offset != record.Offset)
                {
                    AccountDuplicate(scope, record, admission, transaction);
                }
                return new(admission, state!, message,
                    sessionId is null ? null : FindSession(sessionId, transaction),
                    record.Event?.ExternalSessionId, record.Event, true);
            }
        }

        var pendingReason = record.Event switch
        {
            ContractEpisodeOpen or ContractEpisodeClose => "TRUSTED_LIFECYCLE_REQUIRED",
            ContractBoardPost => "NATIVE_BOARD_PENDING",
            _ => "NATIVE_SESSION_PENDING",
        };
        var initial = AppendReceipt(scope, record.Key, null, new("pending", pendingReason), transaction);
        using (var insert = CoordinationCommand(transaction, """
            INSERT INTO coord_projection_event
                (scope,epoch,event_key,source_offset,source_end,raw_bytes,raw_digest,canonical_version,
                 canonical_bytes,original_id,first_receipt_n,current_receipt_n,application_state)
            VALUES($scope,$epoch,$key,$offset,$end,$raw,$digest,$version,$canonical,$external,$n,$n,'pending');
            """, ("$scope", scope), ("$epoch", CoordinationSourceCapture.Epoch), ("$key", record.Key),
            ("$offset", record.Offset), ("$end", record.End), ("$raw", record.Raw), ("$digest", record.Digest),
            ("$version", CoordContract.Version), ("$canonical", record.Canonical),
            ("$external", record.Event?.ExternalSessionId), ("$n", initial)))
        {
            insert.ExecuteNonQuery();
        }
        var effect = ApplyObservation(scope, record, allocators, transaction);
        if (effect.State != "pending")
        {
            AppendReceipt(scope, record.Key, initial, effect, transaction);
        }
        // Pending retains the initial receipt and complete payload; it is not a successful application.
        return new(initial, effect.State, effect.MessageId, effect.Session,
            record.Event?.ExternalSessionId, record.Event, false);
    }

    private CoordinationEffect ApplyObservation(
        string scope, CoordinationRecord record, CoordinationAllocators allocators, SqliteTransaction transaction)
    {
        if (record.Refusal is not null)
        {
            return new("refused", record.Refusal);
        }
        var known = FindObservation(scope, record.Event!.ExternalSessionId, transaction);
        if (record.Event is ContractRegister)
        {
            if (known is null)
            {
                known = new SessionRecord(allocators.SessionId(), new SessionGeneration(1), record.Binding!);
                RecordSession(known, transaction);
                ExecuteNonQuery(_connection,
                    "INSERT INTO session_heartbeat(session_id,monotonic_ticks) VALUES($id,$ticks);", transaction,
                    ("$id", known.SessionId), ("$ticks", allocators.HeartbeatTicks));
            }
            return new("applied", "OBSERVED_REGISTER", known);
        }
        if (known is null)
        {
            return new("pending", "REGISTRATION_REQUIRED");
        }
        switch (record.Event)
        {
            case ContractBoardPost post:
                return ApplyBoardObservation(scope, post, known, allocators, transaction);
            case ContractSessionEnd:
                ExecuteNonQuery(_connection, "INSERT OR IGNORE INTO session_ended(session_id) VALUES($id);",
                    transaction, ("$id", known.SessionId));
                return new("applied", "OBSERVED_END", known);
            case ContractHeartbeat:
                using (var ended = CoordinationCommand(transaction,
                    "SELECT 1 FROM session_ended WHERE session_id=$id;", ("$id", known.SessionId)))
                {
                    if (ended.ExecuteScalar() is not null)
                    {
                        return new("refused", "SESSION_ENDED", known);
                    }
                }
                ExecuteNonQuery(_connection,
                    "UPDATE session_heartbeat SET monotonic_ticks=$ticks WHERE session_id=$id;", transaction,
                    ("$id", known.SessionId), ("$ticks", allocators.HeartbeatTicks));
                return new("applied", "OBSERVED_HEARTBEAT", known);
            case ContractUpdate update:
                var harness = Attribute(update.Attributes, OtelAttributes.ServiceName);
                var model = Attribute(update.Attributes, OtelAttributes.GenAiModel);
                var binding = known.Binding with
                {
                    Harness = harness is null ? known.Binding.Harness :
                        new HarnessIdentity(harness, Attribute(update.Attributes, OtelAttributes.ServiceVersion) ?? "unknown"),
                    Model = model is null ? known.Binding.Model :
                        new ModelIdentity(model, Attribute(update.Attributes, OtelAttributes.GenAiModelVersion) ?? "unknown"),
                };
                known = known with { Binding = binding };
                RecordSession(known, transaction);
                return new("applied", "OBSERVED_UPDATE", known);
            default:
                return new("pending", "TRUSTED_LIFECYCLE_REQUIRED", known);
        }
    }

    private CoordinationEffect ApplyBoardObservation(
        string scope, ContractBoardPost post, SessionRecord session,
        CoordinationAllocators allocators, SqliteTransaction transaction)
    {
        var declared = Attribute(post.Attributes, CoordContract.BoardAttributes.Kind);
        var content = Attribute(post.Attributes, CoordContract.BoardAttributes.Content);
        var parent = Attribute(post.Attributes, CoordContract.BoardAttributes.Parent);
        if (declared is null || !Enum.TryParse<BoardMessageKind>(declared.Replace("-", ""), true, out var kind)
            || !Enum.IsDefined(kind) || (kind != BoardMessageKind.Acknowledgement && content is null)
            || (kind is BoardMessageKind.Reply or BoardMessageKind.Acknowledgement && parent is null))
        {
            return new("refused", "MALFORMED_BOARD", session);
        }
        var repository = session.Binding.Repository.CanonicalPath;
        string? parentKey = null;
        if (kind is BoardMessageKind.Reply or BoardMessageKind.Acknowledgement)
        {
            using var query = CoordinationCommand(transaction,
                "SELECT 1 FROM board_message_fact WHERE message_id=$parent AND repository_key=$repo;",
                ("$parent", parent), ("$repo", repository));
            if (query.ExecuteScalar() is null)
            {
                return new("pending", "PARENT_REQUIRED", session);
            }
            using var parentQuery = CoordinationCommand(transaction, """
                SELECT event_key FROM coord_projection_feed WHERE scope=$scope AND epoch=$epoch
                  AND message_id=$parent AND application_state='applied' ORDER BY n LIMIT 1;
                """, ("$scope", scope), ("$epoch", CoordinationSourceCapture.Epoch), ("$parent", parent));
            parentKey = parentQuery.ExecuteScalar() as string;
        }
        else
        {
            parent = null;
        }
        using var maximum = CoordinationCommand(transaction,
            "SELECT COALESCE(MAX(seq),0) FROM board_message_fact WHERE repository_key=$repo;", ("$repo", repository));
        var sequence = checked((int)checked((long)maximum.ExecuteScalar()! + 1));
        var message = new BoardMessage(allocators.MessageId(), repository, kind, session.SessionId,
            session.Binding.Trust, parent, kind == BoardMessageKind.Acknowledgement ? null : content,
            true, GraderInjectionScanner.LooksLikeInjection(content), false, allocators.RecordedAt, sequence);
        InsertBoardMessage(message, transaction);
        return new("applied", "OBSERVED_BOARD", session, message.MessageId, parentKey);
    }

    private SessionRecord? FindObservation(string scope, string external, SqliteTransaction transaction)
    {
        using var query = CoordinationCommand(transaction, """
            SELECT f.session_id FROM coord_projection_event e JOIN coord_projection_feed f ON f.n=e.current_receipt_n
            WHERE e.scope=$scope AND e.epoch=$epoch AND e.original_id=$external
              AND f.reason='OBSERVED_REGISTER' AND f.application_state='applied' ORDER BY f.n LIMIT 1;
            """, ("$scope", scope), ("$epoch", CoordinationSourceCapture.Epoch), ("$external", external));
        return query.ExecuteScalar() is string sessionId ? FindSession(sessionId, transaction) : null;
    }

    private long AppendReceipt(
        string scope, string key, long? admission, CoordinationEffect effect, SqliteTransaction transaction)
    {
        using var command = CoordinationCommand(transaction, """
            INSERT INTO coord_projection_feed
                (scope,epoch,event_key,is_initial,admission_n,outcome,application_state,reason,
                 session_id,session_generation,message_id,parent_event_key)
            VALUES($scope,$epoch,$key,$initial,$admission,$state,$state,$reason,$session,$generation,$message,$parent);
            SELECT last_insert_rowid();
            """, ("$scope", scope), ("$epoch", CoordinationSourceCapture.Epoch), ("$key", key),
            ("$initial", admission is null ? 1 : 0), ("$admission", admission), ("$state", effect.State),
            ("$reason", effect.Reason), ("$session", effect.Session?.SessionId),
            ("$generation", effect.Session?.Generation.Value), ("$message", effect.MessageId), ("$parent", effect.ParentKey));
        return (long)command.ExecuteScalar()!;
    }

    private void AccountDuplicate(string scope, CoordinationRecord record, long admission, SqliteTransaction transaction)
    {
        using var existing = CoordinationCommand(transaction, """
            SELECT raw_digest FROM coord_projection_feed
            WHERE scope=$scope AND epoch=$epoch AND source_offset=$offset;
            """, ("$scope", scope), ("$epoch", CoordinationSourceCapture.Epoch), ("$offset", record.Offset));
        if (existing.ExecuteScalar() is string digest)
        {
            if (digest != record.Digest)
            {
                throw new CoordinationSourceException(CoordinationErrors.DuplicateConflict);
            }
            return;
        }
        using var command = CoordinationCommand(transaction, """
            INSERT INTO coord_projection_feed
                (scope,epoch,event_key,is_initial,admission_n,outcome,application_state,
                 parent_application_state,source_offset,source_end,raw_digest)
            VALUES($scope,$epoch,$key,0,$admission,'duplicate-occurrence-accounted',NULL,NULL,$offset,$end,$digest);
            """, ("$scope", scope), ("$epoch", CoordinationSourceCapture.Epoch), ("$key", record.Key),
            ("$admission", admission), ("$offset", record.Offset), ("$end", record.End), ("$digest", record.Digest));
        command.ExecuteNonQuery();
    }

    private CoordinationCheckpoint? ReadCheckpoint(string scope, SqliteTransaction transaction)
    {
        using var query = CoordinationCommand(transaction,
            "SELECT accepted_offset,prefix_digest FROM coord_projection_checkpoint WHERE scope=$scope;",
            ("$scope", scope));
        using var reader = query.ExecuteReader();
        return reader.Read() ? new(reader.GetInt64(0), reader.GetString(1)) : null;
    }

    private SqliteCommand CoordinationCommand(
        SqliteTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = _connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }
        return command;
    }

    private void FailAt(CoordinationFault point)
    {
        if (ProjectionFault == point)
        {
            ProjectionFault = CoordinationFault.None;
            throw new IOException("COORD_FAULT_" + point);
        }
    }

    private static string? Attribute(IReadOnlyDictionary<string, string?> attributes, string key) =>
        attributes.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
