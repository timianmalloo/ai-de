using System.Security.Cryptography;
using System.Text;
using AiDe.Core.Store.Records;

namespace AiDe.Core.Store;

// ADR-0039 append API. Every method is an INSERT: a dimension change is a new version row (never an
// UPDATE), a fact is appended, and the Type-0 guard lives here because SQLite cannot compare a row to
// its predecessor in a CHECK. No business logic and no folds.
public sealed partial class StoreWriter
{
    private string Now() => WorkspaceStore.Iso(DateTimeOffset.UtcNow);

    public void AppendJourney(JourneyRow row)
    {
        var previous = ScalarRow($"SELECT version_no, purpose, origin_scope, origin_journey_id, origin_kind, origin_object_ids "
            + "FROM journey_dim WHERE journey_id = $id ORDER BY version_no DESC LIMIT 1;", ("$id", row.JourneyId));
        if (previous is not null)
        {
            RequireSame("journey_dim", row.JourneyId,
                [("purpose", previous[1], row.Purpose), ("origin_scope", previous[2], row.OriginScope),
                 ("origin_journey_id", previous[3], row.OriginJourneyId), ("origin_kind", previous[4], row.OriginKind),
                 ("origin_object_ids", previous[5], row.OriginObjectIds)]);
        }

        var version = previous is null ? 1 : Convert.ToInt32(previous[0]) + 1;
        Exec("""
            INSERT INTO journey_dim (journey_id, version_no, purpose, origin_scope, origin_journey_id,
                                     origin_kind, origin_object_ids, title, recorded_at, ingress_seq)
            VALUES ($id, $ver, $purpose, $scope, $oj, $ok, $oo, $title, $at, $seq);
            """,
            ("$id", row.JourneyId), ("$ver", version), ("$purpose", row.Purpose), ("$scope", row.OriginScope),
            ("$oj", row.OriginJourneyId), ("$ok", row.OriginKind), ("$oo", row.OriginObjectIds),
            ("$title", row.Title), ("$at", Now()), ("$seq", NextIngressSequence()));
    }

    /// <remarks>
    /// The owner is Type-0 except for the one legal transition: a legacy notebook (no owner) adopted
    /// by an explicit user action, which is a new version row with <c>Legacy = false</c> and an owner.
    /// </remarks>
    public void AppendNotebook(NotebookRow row)
    {
        var previous = ScalarRow("SELECT version_no, owner_journey_id, legacy FROM notebook_dim "
            + "WHERE notebook_id = $id ORDER BY version_no DESC LIMIT 1;", ("$id", row.NotebookId));
        if (previous is not null)
        {
            var wasLegacy = Convert.ToInt64(previous[2]) == 1;
            if (!(wasLegacy && previous[1] is null))
            {
                RequireSame("notebook_dim", row.NotebookId, [("owner_journey_id", previous[1], row.OwnerJourneyId)]);
            }
        }

        var version = previous is null ? 1 : Convert.ToInt32(previous[0]) + 1;
        Exec("""
            INSERT INTO notebook_dim (notebook_id, version_no, owner_journey_id, legacy, recorded_at, ingress_seq)
            VALUES ($id, $ver, $owner, $legacy, $at, $seq);
            """,
            ("$id", row.NotebookId), ("$ver", version), ("$owner", row.OwnerJourneyId),
            ("$legacy", row.Legacy ? 1 : 0), ("$at", Now()), ("$seq", NextIngressSequence()));
    }

    public void AppendNotebookRevision(NotebookRevisionRow row)
    {
        Exec("""
            INSERT INTO prompt_revision_fact (draft_id, revision_no, body, saved_at, ingress_seq,
                                              parent_revision_no, body_sha)
            VALUES ($id, $rev, $body, $at, $seq, $parent, $sha);
            """,
            ("$id", row.NotebookId), ("$rev", row.RevisionNo), ("$body", row.Body), ("$at", Now()),
            ("$seq", NextIngressSequence()), ("$parent", row.ParentRevisionNo), ("$sha", BodySha(row.Body)));
    }

    /// <summary>The one home of <c>body_sha</c>: every revision writer calls this, and the store refuses a row without it.</summary>
    internal static string BodySha(string body) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

    public void AppendAttempt(AttemptRow row) => Exec("""
        INSERT INTO dispatch_attempt_fact
            (dispatch_key, workspace_id, workspace_epoch, draft_id, revision_no, session_id,
             session_generation, attempted_at, ingress_seq, projection_sha, route_fingerprint,
             profile_key, scope_snapshot, parent_dispatch_key, queued_after_dispatch_key)
        VALUES ($key, $ws, $epoch, $draft, $rev, $session, $gen, $at, $seq, $proj, $route,
                $profile, $scope, $parent, $queued);
        """,
        ("$key", row.DispatchKey), ("$ws", row.WorkspaceId), ("$epoch", row.WorkspaceEpoch),
        ("$draft", row.NotebookId), ("$rev", row.RevisionNo), ("$session", row.SessionId),
        ("$gen", row.SessionGeneration), ("$at", Now()), ("$seq", NextIngressSequence()),
        ("$proj", row.ProjectionSha), ("$route", row.RouteFingerprint), ("$profile", row.ProfileKey),
        ("$scope", row.ScopeSnapshot), ("$parent", row.ParentDispatchKey), ("$queued", row.QueuedAfterDispatchKey));

    public void AppendOutcome(OutcomeRow row) => Exec("""
        INSERT INTO dispatch_outcome_fact (dispatch_key, ingress_seq, state, error_code, recorded_at, cause, detail)
        VALUES ($key, $seq, $state, $err, $at, $cause, $detail);
        """,
        ("$key", row.DispatchKey), ("$seq", NextIngressSequence()), ("$state", row.State.ToString()),
        ("$err", row.ErrorCode), ("$at", Now()), ("$cause", row.Cause), ("$detail", row.Detail));

    public void AppendPermissionDecision(PermissionDecisionRow row) => Exec("""
        INSERT INTO permission_decision_fact
            (session_id, run_id, seq, action, scope, arguments_sha, options_offered, option_chosen,
             actor, expires_at, recorded_at, ingress_seq)
        VALUES ($session, $run, $seq, $action, $scope, $sha, $offered, $chosen, $actor, $exp, $at, $order);
        """,
        ("$session", row.SessionId), ("$run", row.RunId), ("$seq", row.Seq), ("$action", row.Action),
        ("$scope", row.Scope), ("$sha", row.ArgumentsSha), ("$offered", row.OptionsOffered),
        ("$chosen", row.OptionChosen), ("$actor", row.Actor), ("$exp", row.ExpiresAt), ("$at", Now()),
        ("$order", NextIngressSequence()));

    public void AppendEvidenceLink(EvidenceLinkRow row) => Exec("""
        INSERT INTO evidence_link_fact
            (link_id, subject_kind, subject_id, subject_revision, artifact_ref, producer, baseline,
             limitations, verification_status, recorded_at, ingress_seq)
        VALUES ($id, $kind, $subject, $rev, $ref, $producer, $baseline, $lim, $status, $at, $seq);
        """,
        ("$id", row.LinkId), ("$kind", row.SubjectKind), ("$subject", row.SubjectId),
        ("$rev", row.SubjectRevision), ("$ref", row.ArtifactRef), ("$producer", row.Producer),
        ("$baseline", row.Baseline), ("$lim", row.Limitations), ("$status", row.VerificationStatus),
        ("$at", Now()), ("$seq", NextIngressSequence()));

    public void AppendAssessment(AssessmentRow row) => Exec("""
        INSERT INTO assessment_fact (assessment_id, kind, scope, baseline, omissions, payload, recorded_at, ingress_seq)
        VALUES ($id, $kind, $scope, $baseline, $omissions, $payload, $at, $seq);
        """,
        ("$id", row.AssessmentId), ("$kind", row.Kind), ("$scope", row.Scope), ("$baseline", row.Baseline),
        ("$omissions", row.Omissions), ("$payload", row.Payload), ("$at", Now()), ("$seq", NextIngressSequence()));

    public void AppendDisposition(DispositionRow row) => Exec("""
        INSERT INTO disposition_fact
            (subject_kind, subject_id, kind, actor, reason, baseline, notebook_id, revision_no, recorded_at, ingress_seq)
        VALUES ($sk, $sid, $kind, $actor, $reason, $baseline, $nb, $rev, $at, $seq);
        """,
        ("$sk", row.SubjectKind), ("$sid", row.SubjectId), ("$kind", row.Kind), ("$actor", row.Actor),
        ("$reason", row.Reason), ("$baseline", row.Baseline), ("$nb", row.NotebookId), ("$rev", row.RevisionNo),
        ("$at", Now()), ("$seq", NextIngressSequence()));

    public void AppendObligation(ObligationRow row) => Exec("""
        INSERT INTO checklist_obligation_fact
            (obligation_id, checklist_id, revision, text, mandatory, recorded_at, ingress_seq)
        VALUES ($id, $checklist, $rev, $text, $mandatory, $at, $seq);
        """,
        ("$id", row.ObligationId), ("$checklist", row.ChecklistId), ("$rev", row.Revision),
        ("$text", row.Text), ("$mandatory", row.Mandatory ? 1 : 0), ("$at", Now()),
        ("$seq", NextIngressSequence()));

    /// <summary>Appends a profile version and returns its surrogate key (what attempts bind to).</summary>
    public long AppendHarnessProfile(HarnessProfileRow row)
    {
        var previous = ScalarRow("SELECT version_no, engine_id, family FROM harness_profile_dim "
            + "WHERE profile_id = $id ORDER BY version_no DESC LIMIT 1;", ("$id", row.ProfileId));
        if (previous is not null)
        {
            RequireSame("harness_profile_dim", row.ProfileId,
                [("engine_id", previous[1], row.EngineId), ("family", previous[2], row.Family)]);
        }

        var version = previous is null ? 1 : Convert.ToInt32(previous[0]) + 1;
        Exec("""
            INSERT INTO harness_profile_dim (profile_id, version_no, engine_id, family, native_home,
                                             declared_account, operator_token_ref, recorded_at, ingress_seq)
            VALUES ($id, $ver, $engine, $family, $home, $account, $token, $at, $seq);
            """,
            ("$id", row.ProfileId), ("$ver", version), ("$engine", row.EngineId), ("$family", row.Family),
            ("$home", row.NativeHome), ("$account", row.DeclaredAccount), ("$token", row.OperatorTokenRef),
            ("$at", Now()), ("$seq", NextIngressSequence()));
        return Convert.ToInt64(WorkspaceStore.ExecuteScalar(_connection, "SELECT last_insert_rowid();", _transaction));
    }

    public void AppendProfileObservation(ProfileObservationRow row) => Exec("""
        INSERT INTO profile_observation_fact
            (profile_key, kind, value, source, observed_at, duration_ms, exit_code, ingress_seq)
        VALUES ($key, $kind, $value, $source, $obs, $dur, $exit, $seq);
        """,
        ("$key", row.ProfileKey), ("$kind", row.Kind), ("$value", row.Value), ("$source", row.Source),
        ("$obs", row.ObservedAt), ("$dur", row.DurationMs), ("$exit", row.ExitCode),
        ("$seq", NextIngressSequence()));

    private object?[]? ScalarRow(string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = _connection.CreateCommand();
        command.Transaction = _transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var values = new object?[reader.FieldCount];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }

        return values;
    }

    private static void RequireSame(string table, string key, (string Column, object? Was, object? Now)[] columns)
    {
        foreach (var (column, was, now) in columns)
        {
            if (!Equals(was, now))
            {
                throw new WorkspaceStoreException(StoreErrorCodes.Type0Violation,
                    $"{table} '{key}': '{column}' is Type-0 and cannot change in a new version row");
            }
        }
    }
}
