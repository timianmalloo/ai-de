using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Store.Records;

namespace AiDe.Core.Workbench.Records.Folds;

/// <summary>Everything one attempt's state fold reads (ADR-0041 §2). All of it is already durable.</summary>
public sealed record AttemptStateInput(
    string DispatchKey,
    IReadOnlyList<RunEvent> Events,
    IReadOnlyList<OutcomeRow> Outcomes,
    IReadOnlyList<PermissionDecisionRow> Decisions,
    IReadOnlyList<EvidenceLinkRow> EvidenceLinks);

/// <summary>The fold's answer: a state, the derived evidence bool, and the cited seqs the log lacks.</summary>
public sealed record AttemptState(
    TurnState State,
    bool HasEvidence,
    string? Cause,
    IReadOnlyList<long> NotRecordedSeqs);

/// <summary>Derives an attempt's state from outcome rows and the durable run log (ADR-0041 §2).</summary>
public static class AttemptStateFold
{
    public static string AttemptArtifactRef(string dispatchKey) => throw new NotImplementedException();

    public static AttemptState Evaluate(AttemptStateInput input) => throw new NotImplementedException();
}

/// <summary>The fold instrumentation: one <c>fold.duration_ms</c> activity per evaluation (ADR-0041 §7).</summary>
public static class FoldSignal
{
    public const string SourceName = "aide.attempt.fold";
}
