namespace AiDe.Core.Store.Records;

// Plain record types for the schema-v2 rows of ADR-0039. No behaviour: folds belong to the readers
// of these rows, not to the rows. Members after the domain fields are ASSIGNED BY THE WRITER
// (version number, total-order position, timestamp) and are ignored on input.

/// <summary>Closed vocabularies the store CHECKs. One home each; the DDL is built from the same lists.</summary>
public static class WorkbenchVocabulary
{
    public static readonly IReadOnlyList<string> JourneyPurposes = ["work", "investigation"];

    public static readonly IReadOnlyList<string> JourneyOriginKinds =
        ["promoted-from", "investigation-of", "review-assignment-of"];

    /// <summary>The <c>cause</c> of a <c>DeliveryUnknown</c> outcome (ADR-0039 §3).</summary>
    public static readonly IReadOnlyList<string> DeliveryUnknownCauses =
    [
        "never_dispatched", "relay_lost", "stop_not_acknowledged",
        "prompt_timeout", "host_restart", "run_log_unwritable",
    ];

    public static readonly IReadOnlyList<string> AssessmentKinds =
        ["change-packet", "test-worthiness", "drift", "rigor"];

    /// <summary>
    /// Keys an assessment payload may never carry, at any depth, in any letter case: each names a state
    /// ADR-0039 section 3 derives (satisfaction, acceptance, currentness, closure, readiness). A payload holds
    /// the producer's findings as asserted at the baseline. The per-kind payload schema lands with WL-5.
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenPayloadKeys =
        ["satisfied", "accepted", "stale", "current", "closed", "ready", "state", "status", "has_evidence", "eligible"];

    public static readonly IReadOnlyList<string> DispositionSubjectKinds =
        ["obligation", "assessment", "work", "investigation", "intent"];

    /// <summary>Disposition kinds allowed per subject kind; the DDL CHECK is built from this.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DispositionKindsBySubject =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["obligation"] = ["acknowledged", "deferred", "needs-decision"],
            ["assessment"] = ["fixed", "false-positive", "intentional", "deferred", "needs-decision"],
            ["work"] = ["accepted"],
            ["investigation"] = ["answered", "bounded-partial", "unresolved"],
            ["intent"] = ["intent-accepted"],
        };

    /// <summary>Fixed profile-observation kinds; <c>capability:&lt;name&gt;</c> is the open-ended one.</summary>
    public static readonly IReadOnlyList<string> ProfileObservationKinds =
        ["installed", "version", "authenticated", "account", "model-available", "quota"];

    public const string CapabilityObservationPrefix = "capability:";
}

/// <summary>One journey version. Type-0: Purpose and the Origin* columns never change across versions.</summary>
public sealed record JourneyRow(
    string JourneyId,
    string Purpose,
    string Title,
    string? OriginScope = null,
    string? OriginJourneyId = null,
    string? OriginKind = null,
    string? OriginObjectIds = null)
{
    public int VersionNo { get; init; }
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>
/// One notebook version. <see cref="Legacy"/> rows have no owner; adoption is a new version row with
/// <c>Legacy = false</c> and an owner (the one permitted change of an otherwise Type-0 owner).
/// </summary>
public sealed record NotebookRow(string NotebookId, string? OwnerJourneyId, bool Legacy)
{
    public int VersionNo { get; init; }
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>An immutable notebook revision (<c>prompt_revision_fact</c>; <c>draft_id</c> is the notebook id).</summary>
public sealed record NotebookRevisionRow(string NotebookId, int RevisionNo, string Body, int? ParentRevisionNo = null)
{
    /// <summary>Null = Not recorded (legacy rows); the writer computes it for every new row.</summary>
    public string? BodySha { get; init; }
    public long IngressSeq { get; init; }
    public string SavedAt { get; init; } = "";
}

/// <summary>An execution attempt as bound at write-ahead (<c>dispatch_attempt_fact</c>).</summary>
public sealed record AttemptRow(
    string DispatchKey,
    string WorkspaceId,
    long WorkspaceEpoch,
    string NotebookId,
    int RevisionNo,
    string SessionId,
    long SessionGeneration,
    string? ProjectionSha = null,
    string? RouteFingerprint = null,
    long? ProfileKey = null,
    string? ScopeSnapshot = null,
    string? ParentDispatchKey = null,
    string? QueuedAfterDispatchKey = null)
{
    public long IngressSeq { get; init; }
    public string AttemptedAt { get; init; } = "";
}

/// <summary>One outcome event on one attempt (<c>dispatch_outcome_fact</c>).</summary>
public sealed record OutcomeRow(
    string DispatchKey,
    AiDe.Core.Facts.DispatchState State,
    string? ErrorCode = null,
    string? Cause = null,
    string? Detail = null)
{
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>One decision on one permission request; the request is <c>(SessionId, RunId, Seq)</c>.</summary>
public sealed record PermissionDecisionRow(
    string SessionId,
    string RunId,
    long Seq,
    string Action,
    string Scope,
    string ArgumentsSha,
    string OptionsOffered,
    string OptionChosen,
    string Actor,
    string? ExpiresAt = null)
{
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>One link from one claim/criterion revision to one artifact or attempt.</summary>
public sealed record EvidenceLinkRow(
    string LinkId,
    string SubjectKind,
    string SubjectId,
    int SubjectRevision,
    string ArtifactRef,
    string Producer,
    string Baseline,
    string VerificationStatus,
    string? Limitations = null)
{
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>One assessment of one scope at one baseline. <see cref="Payload"/> is opaque JSON owned by its producer.</summary>
public sealed record AssessmentRow(
    string AssessmentId,
    string Kind,
    string Scope,
    string Baseline,
    string Omissions,
    string Payload)
{
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>One actor's disposition on one subject at one baseline. Satisfaction is never a disposition.</summary>
public sealed record DispositionRow(
    string SubjectKind,
    string SubjectId,
    string Kind,
    string Actor,
    string Baseline,
    string? Reason = null,
    string? NotebookId = null,
    int? RevisionNo = null)
{
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>One obligation at one checklist revision; its existence is "proposed".</summary>
public sealed record ObligationRow(string ObligationId, string ChecklistId, int Revision, string Text, bool Mandatory)
{
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>One harness-profile version. Type-0: <see cref="Family"/> and <see cref="EngineId"/>.</summary>
public sealed record HarnessProfileRow(
    string ProfileId,
    string EngineId,
    string Family,
    string NativeHome,
    string? DeclaredAccount = null,
    string? OperatorTokenRef = null)
{
    /// <summary>The version's surrogate key — what an attempt's <c>ProfileKey</c> points at.</summary>
    public long ProfileKey { get; init; }
    public int VersionNo { get; init; }
    public long IngressSeq { get; init; }
    public string RecordedAt { get; init; } = "";
}

/// <summary>One observation of one profile version. Absence of a row = Not recorded.</summary>
public sealed record ProfileObservationRow(
    long ProfileKey,
    string Kind,
    string Value,
    string Source,
    string ObservedAt,
    long? DurationMs = null,
    int? ExitCode = null)
{
    public long IngressSeq { get; init; }
}
