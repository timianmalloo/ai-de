using AiDe.Core.Store.Records;

namespace AiDe.Core.Workbench.Records.Folds;

/// <summary>Everything one acceptance evaluation reads. All of it is already durable except the App's projection answer.</summary>
public sealed record AcceptanceInput(
    string JourneyId,
    string IntentNotebookId,
    int? IntentLatestRevisionNo,
    IReadOnlyList<ObligationRow> CurrentObligations,
    IReadOnlyDictionary<string, IReadOnlyList<ObligationRow>> ObligationHistory,
    IReadOnlyDictionary<string, IReadOnlyList<EvidenceLinkRow>> Links,
    IReadOnlyDictionary<string, IReadOnlyList<DispositionRow>> ObligationDispositions,
    IReadOnlyList<DispositionRow> IntentDispositions,
    IReadOnlyDictionary<string, AttemptRow> LinkedAttempts,
    string? LiveProjectionSha);

public enum EvidenceState { Missing, Satisfied, Stale, NotRecorded }

public enum HumanState { None, Acknowledged, Deferred, NeedsDecision }

/// <summary>One obligation's two derived states. A row and a fold never share one field.</summary>
public sealed record ObligationEvaluation(
    string ObligationId,
    string Text,
    bool Mandatory,
    EvidenceState Evidence,
    HumanState Human,
    IReadOnlyList<EvidenceLinkRow> CitedCurrentLinks);

public sealed record AcceptanceGap(string Code, string? ObligationId, string Sentence);

public sealed record AcceptanceEvaluation(
    string Baseline,
    IReadOnlyList<ObligationEvaluation> Obligations,
    IReadOnlyList<AcceptanceGap> Gaps,
    bool Eligible);

/// <summary>Stable gap codes of <see cref="AcceptanceGap.Code"/>.</summary>
public static class AcceptanceGapCodes
{
    public const string ChecklistEmpty = "checklist-empty";
    public const string IntentMissing = "intent-missing";
    public const string EvidenceMissing = "evidence-missing";
    public const string EvidenceStale = "evidence-stale";
    public const string EvidenceNotRecorded = "evidence-not-recorded";
    public const string NeedsDecision = "needs-decision";
    public const string IntentNotAccepted = "intent-not-accepted";
}

/// <summary>Acceptance eligibility as a pure fold over the v2 rows (WL-1b design, "The folds").</summary>
public static class AcceptanceFold
{
    /// <summary>The only <c>verification_status</c> WL-1b writes: the link names an attempt that ended <c>ResponseComplete</c>.</summary>
    public const string ObservedStatus = "observed";

    public static AcceptanceEvaluation Evaluate(AcceptanceInput input) => throw new NotImplementedException();
}
