using Microsoft.Data.Sqlite;

namespace AiDe.Core.Watcher;

public enum CoordinationRecoveryStatus { NotRecorded, Completed, Failed }

/// <summary>Observed pass work, including rolled-back attempts; Applied counts committed effects only.</summary>
public sealed record CoordinationRecoveryStats(int Examinations, int Attempts, int Applied,
    CoordinationRecoveryStatus Status = CoordinationRecoveryStatus.NotRecorded,
    string? ErrorCode = null, double? ElapsedMilliseconds = null);

internal sealed record RecoveryCandidate(
    string Scope, string Key, long Admission, long Receipt, long Offset, long End,
    string Digest, string CanonicalDigest, string Version, string? External, string? Parent,
    string Status, long Generation, int Presence, int Seen, int Attempts, long Due);

public sealed partial class SqliteWatcherObservationStore
{
    private static readonly string[] RecoveryLanes = ["ready", "deferred", "due"];

    internal void RecoverCoordination(
        IReadOnlyList<CoordinationCapture> captures, CoordinationAllocators allocators,
        out IReadOnlyList<CoordinationResult> observations, out CoordinationRecoveryStats measured)
    {
        using var activity = new System.Diagnostics.Activity("coordination.recovery").Start();
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var completed = new List<CoordinationResult>();
        var examinations = 0;
        var attempts = 0;
        string? errorCode = null;
        try
        {
            var records = captures.SelectMany(capture => capture.Pages)
                .SelectMany(page => page.Records.Select(record => (page.Scope, Record: record)))
                .ToDictionary(item => (item.Scope, item.Record.Offset), item => item.Record);
            var scopes = records.Keys.Select(key => key.Scope).Distinct(StringComparer.Ordinal).ToArray();
            var attempted = new HashSet<(string, string)>();
            lock (_gate)
            {
                foreach (var lane in RecoveryLanes)
                {
                    BeginRecoveryLane(scopes, lane);
                }
                int[] examinationLimits = [128, 64, 64];
                int[] attemptLimits = [32, 16, 16];
                // Every lane receives its protected opportunity before remaining work is borrowed.
                for (var lane = 0; lane < RecoveryLanes.Length; lane++)
                {
                    Serve(RecoveryLanes[lane], examinationLimits[lane], attemptLimits[lane]);
                }
                foreach (var lane in RecoveryLanes)
                {
                    Serve(lane, 256 - examinations, 64 - attempts);
                }
            }

            void Serve(string lane, int examinationBudget, int attemptBudget)
            {
                while (examinationBudget > 0 && attemptBudget > 0 && examinations < 256 && attempts < 64)
                {
                    using var transaction = _connection.BeginTransaction(deferred: false);
                    var candidate = NextRecovery(scopes, lane, transaction);
                    if (candidate is null)
                    {
                        using var exhausted = CoordinationCommand(transaction, $"""
                        UPDATE coord_projection_checkpoint SET {lane}_last={lane}_high
                        WHERE scope IN ({string.Join(",", scopes.Select((_, index) => "$scope" + index))});
                        """, scopes.Select((scope, index) => ("$scope" + index, (object?)scope)).ToArray());
                        if (scopes.Length > 0)
                        {
                            exhausted.ExecuteNonQuery();
                        }
                        transaction.Commit();
                        return;
                    }
                    examinations++;
                    examinationBudget--;
                    AdvanceRecovery(candidate, lane, transaction);
                    if (attempted.Contains((candidate.Scope, candidate.Key)))
                    {
                        transaction.Commit();
                        continue;
                    }
                    if (!records.TryGetValue((candidate.Scope, candidate.Offset), out var record)
                        || record.Key != candidate.Key || record.End != candidate.End
                        || record.Digest != candidate.Digest || candidate.Version != CoordContract.Version
                        || CoordinationSourceCapture.Hash(record.Canonical) != candidate.CanonicalDigest)
                    {
                        throw new CoordinationSourceException(CoordinationErrors.SourceGap);
                    }
                    var seen = candidate.Seen | RecoveryComponents(candidate.Scope, candidate.External, candidate.Parent, transaction);
                    var increment = ComponentCount(seen) - ComponentCount(candidate.Seen);
                    if (candidate.Generation > long.MaxValue - increment)
                    {
                        throw new CoordinationSourceException("COORD_ELIGIBILITY_OVERFLOW");
                    }
                    var generation = checked(candidate.Generation + increment);
                    var ready = (seen & 1) != 0 && (candidate.Parent is null || ParentPresent(candidate.Scope,
                        candidate.External, candidate.Parent, transaction));
                    var priorAttempts = increment > 0 ? 0 : candidate.Attempts;
                    var canAttempt = priorAttempts < 8
                        && (increment > 0 || candidate.Status == "deferred" && ready
                            || candidate.Status == "active" && candidate.Due <= allocators.RecordedAt.ToUnixTimeMilliseconds());
                    if (increment > 0 && candidate.Presence == 0 && !ready)
                    {
                        var eligibility = AppendReceipt(candidate.Scope, candidate.Key, candidate.Admission,
                            new("pending", "REGISTRATION_REQUIRED",
                                candidate.External is null ? null : FindObservation(candidate.Scope, candidate.External, transaction)),
                            transaction, candidate.Status, generation, 0);
                        FinalizeRecovery(candidate.Scope, record, eligibility, "pending", candidate.Status,
                            generation, 0, seen, 0, allocators.RecordedAt, transaction);
                        transaction.Commit();
                        continue;
                    }
                    if (lane == "ready" && increment == 0 || lane == "deferred" && !ready || !canAttempt)
                    {
                        transaction.Commit();
                        continue;
                    }
                    // Recovery observes dependencies; it never replays a live registrar or lifecycle refresh.
                    if (record.Event is ContractRegister or ContractHeartbeat or ContractSessionEnd or ContractUpdate)
                    {
                        transaction.Commit();
                        continue;
                    }
                    if (candidate.Presence == 0 && (!ready || !HasRecoveryCapacity(record, transaction, reserved: true)))
                    {
                        if (increment > 0)
                        {
                            var eligibility = AppendReceipt(candidate.Scope, candidate.Key, candidate.Admission,
                                new("pending", "REGISTRATION_REQUIRED"), transaction, candidate.Status, generation, 0);
                            FinalizeRecovery(candidate.Scope, record, eligibility, "pending", candidate.Status,
                                generation, 0, seen, 0, allocators.RecordedAt, transaction);
                        }
                        transaction.Commit();
                        continue;
                    }
                    if (candidate.Presence == 0)
                    {
                        var activation = AppendReceipt(candidate.Scope, candidate.Key, candidate.Admission,
                            new("pending", "NATIVE_BOARD_PENDING",
                                candidate.External is null ? null : FindObservation(candidate.Scope, candidate.External, transaction)),
                            transaction, "active", generation, 1);
                        FinalizeRecovery(candidate.Scope, record, activation, "pending", "active", generation, 1,
                            seen, priorAttempts, allocators.RecordedAt, transaction);
                    }
                    attempted.Add((candidate.Scope, candidate.Key));
                    attempts = checked(attempts + 1);
                    attemptBudget--;
                    var count = checked(priorAttempts + 1);
                    var effect = ApplyObservation(candidate.Scope, record, allocators, transaction);
                    if (candidate.Presence == 0 && effect.State == "pending")
                    {
                        transaction.Rollback();
                        // The reserve must never become a committed blocked holder.
                        using var progress = _connection.BeginTransaction(deferred: false);
                        AdvanceRecovery(candidate, lane, progress);
                        var retainedStatus = count == 8 ? "exhausted" : "deferred";
                        var retainedReceipt = candidate.Receipt;
                        if (generation != candidate.Generation || retainedStatus != candidate.Status)
                        {
                            retainedReceipt = AppendReceipt(candidate.Scope, candidate.Key, candidate.Admission,
                                effect, progress, retainedStatus, generation, 0);
                        }
                        FinalizeRecovery(candidate.Scope, record, retainedReceipt, "pending", retainedStatus,
                            generation, 0, seen, count, allocators.RecordedAt.AddSeconds(1 << count), progress);
                        progress.Commit();
                        continue;
                    }
                    var status = effect.State == "pending" ? count == 8 ? "exhausted" : "active" : "none";
                    var presence = status == "exhausted" ? 0 : 1;
                    var receipt = candidate.Receipt;
                    if (effect.State != "pending" || status != candidate.Status || generation != candidate.Generation)
                    {
                        receipt = AppendReceipt(candidate.Scope, candidate.Key, candidate.Admission, effect,
                            transaction, status, generation, presence);
                    }
                    FinalizeRecovery(candidate.Scope, record, receipt, effect.State, status, generation, presence,
                        seen, count, allocators.RecordedAt.AddSeconds(1 << Math.Min(count, 8)), transaction);
                    FailAt(CoordinationFault.BeforeCommit);
                    transaction.Commit();
                    if (effect.State == "applied")
                    {
                        completed.Add(new(candidate.Admission, effect.State, effect.MessageId, effect.Session,
                            record.Event?.ExternalSessionId, record.Event, false));
                    }
                    FailAt(CoordinationFault.AfterCommit);
                }
            }
        }
        catch (Exception error)
        {
            errorCode = error is CoordinationSourceException source ? source.Code : "COORD_RECOVERY_FAILED";
            throw;
        }
        finally
        {
            observations = completed;
            measured = new(examinations, attempts, completed.Count,
                errorCode is null ? CoordinationRecoveryStatus.Completed : CoordinationRecoveryStatus.Failed,
                errorCode, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            activity.SetTag("coordination.recovery.status", measured.Status.ToString());
            activity.SetTag("coordination.recovery.examinations", examinations);
            activity.SetTag("coordination.recovery.attempts", attempts);
            activity.SetTag("coordination.recovery.applied", completed.Count);
            activity.SetTag("coordination.recovery.elapsed_ms", measured.ElapsedMilliseconds);
            activity.SetTag("error.type", errorCode);
            activity.SetStatus(errorCode is null ? System.Diagnostics.ActivityStatusCode.Ok
                : System.Diagnostics.ActivityStatusCode.Error);
        }
    }

    private void BeginRecoveryLane(string[] scopes, string lane)
    {
        using var transaction = _connection.BeginTransaction(deferred: false);
        foreach (var scope in scopes)
        {
            using var command = CoordinationCommand(transaction, $"""
                UPDATE coord_projection_checkpoint SET
                    {lane}_last=CASE WHEN {lane}_last>={lane}_high THEN 0 ELSE {lane}_last END,
                    {lane}_high=CASE WHEN {lane}_last>={lane}_high THEN
                        COALESCE((SELECT MAX(first_receipt_n) FROM coord_projection_event WHERE scope=$scope),0)
                        ELSE {lane}_high END
                WHERE scope=$scope;
                """, ("$scope", scope));
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private RecoveryCandidate? NextRecovery(string[] scopes, string lane, SqliteTransaction transaction)
    {
        var parameters = scopes.Select((scope, index) => ("$s" + index, (object?)scope)).ToArray();
        if (parameters.Length == 0)
        {
            return null;
        }
        var predicate = lane switch
        {
            "deferred" => "e.recovery_status='deferred'",
            "due" => "e.recovery_status='active'",
            _ => "e.recovery_status IN ('active','exhausted')",
        };
        using var query = CoordinationCommand(transaction, $"""
            SELECT e.scope,e.event_key,e.first_receipt_n,e.current_receipt_n,e.source_offset,e.source_end,
                e.raw_digest,e.canonical_digest,e.canonical_version,e.original_id,e.requested_parent_message_id,
                e.recovery_status,e.eligibility_generation,e.payload_presence,e.seen_components,e.current_attempt,e.due_utc
            FROM coord_projection_checkpoint c JOIN coord_projection_event e ON e.scope=c.scope
            WHERE c.scope IN ({string.Join(",", parameters.Select(parameter => parameter.Item1))})
                AND e.application_state='pending' AND {predicate}
                AND e.first_receipt_n>c.{lane}_last AND e.first_receipt_n<=c.{lane}_high
            ORDER BY c.{lane}_served,c.scope,e.first_receipt_n LIMIT 1;
            """, parameters);
        using var reader = query.ExecuteReader();
        return !reader.Read() ? null : new(reader.GetString(0), reader.GetString(1), reader.GetInt64(2),
            reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6), reader.GetString(7),
            reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10), reader.GetString(11), reader.GetInt64(12),
            reader.GetInt32(13), reader.GetInt32(14), reader.GetInt32(15), reader.GetInt64(16));
    }

    private void AdvanceRecovery(RecoveryCandidate candidate, string lane, SqliteTransaction transaction)
    {
        using var maximum = CoordinationCommand(transaction,
            $"SELECT COALESCE(MAX({lane}_served),0) FROM coord_projection_checkpoint;");
        var lastServed = (long)maximum.ExecuteScalar()!;
        if (lastServed == long.MaxValue)
        {
            throw new CoordinationSourceException("COORD_RECOVERY_COUNTER_OVERFLOW");
        }
        var nextServed = checked(lastServed + 1);
        using var command = CoordinationCommand(transaction, $"""
            UPDATE coord_projection_checkpoint SET {lane}_last=$last,
                {lane}_served=$served
            WHERE scope=$scope;
            """, ("$last", candidate.Admission), ("$scope", candidate.Scope), ("$served", nextServed));
        command.ExecuteNonQuery();
    }

    private bool HasRecoveryCapacity(CoordinationRecord record, SqliteTransaction transaction, bool reserved)
    {
        using var command = CoordinationCommand(transaction, """
            SELECT COUNT(*),COALESCE(SUM(length(raw_bytes)+length(canonical_bytes)),0)
            FROM coord_projection_event WHERE application_state='pending' AND payload_presence=1;
            """);
        using var reader = command.ExecuteReader();
        reader.Read();
        return reader.GetInt64(0) < (reserved ? 1024 : 1023)
            && reader.GetInt64(1) + record.Raw.Length + record.Canonical.Length <= (reserved ? 16777216 : 16646144);
    }

    private static string? RequestedParent(CoordinationRecord record) =>
        record.Event is ContractBoardPost post ? Attribute(post.Attributes, CoordContract.BoardAttributes.Parent) : null;

    private static int ComponentCount(int components) => (components & 1) + ((components >> 1) & 1);

    private int RecoveryComponents(string scope, string? external, string? parent, SqliteTransaction transaction)
    {
        var session = external is null ? null : FindObservation(scope, external, transaction);
        if (session is null)
        {
            return 0;
        }
        using var binding = CoordinationCommand(transaction,
            "SELECT bound_repository_key FROM coord_projection_checkpoint WHERE scope=$scope;", ("$scope", scope));
        if (binding.ExecuteScalar() is string repository && repository != session.Binding.Repository.CanonicalPath)
        {
            throw new CoordinationSourceException(CoordinationBindingErrors.Mismatch);
        }
        return 1 | (parent is not null && ParentPresent(scope, external, parent, transaction) ? 2 : 0);
    }

    private bool ParentPresent(string scope, string? external, string parent, SqliteTransaction transaction)
    {
        var session = external is null ? null : FindObservation(scope, external, transaction);
        if (session is null)
        {
            return false;
        }
        using var command = CoordinationCommand(transaction, """
            SELECT 1 FROM board_message_fact WHERE message_id=$parent AND repository_key=$repo LIMIT 1;
            """, ("$parent", parent), ("$repo", session.Binding.Repository.CanonicalPath));
        return command.ExecuteScalar() is not null;
    }

    private void FinalizeRecovery(string scope, CoordinationRecord record, long receipt, string state,
        string status, long generation, int presence, int seen, int attempts, DateTimeOffset due,
        SqliteTransaction transaction)
    {
        using var command = CoordinationCommand(transaction, """
            UPDATE coord_projection_event SET application_state=$state,recovery_status=$status,
                eligibility_generation=$generation,payload_presence=$presence,seen_components=$seen,
                current_attempt=$attempts,due_utc=$due,raw_bytes=$raw,canonical_bytes=$canonical
            WHERE scope=$scope AND epoch=$epoch AND event_key=$key AND current_receipt_n=$receipt;
            """, ("$state", state), ("$status", status), ("$generation", generation), ("$presence", presence),
            ("$seen", seen), ("$attempts", attempts), ("$due", due.ToUnixTimeMilliseconds()),
            ("$raw", presence == 1 ? record.Raw : null), ("$canonical", presence == 1 ? record.Canonical : null),
            ("$scope", scope), ("$epoch", CoordinationSourceCapture.Epoch), ("$key", record.Key), ("$receipt", receipt));
        if (command.ExecuteNonQuery() != 1)
        {
            throw new CoordinationSourceException(CoordinationErrors.StaleSnapshot);
        }
    }
}