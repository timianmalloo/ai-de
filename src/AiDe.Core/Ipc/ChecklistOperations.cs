using AiDe.Core.Store;
using AiDe.Core.Store.Records;
using AiDe.Core.Workbench.Records.Folds;

namespace AiDe.Core.Ipc;

/// <summary>One obligation a caller declares. A null id is minted by the service; a given id must already be in the checklist's history.</summary>
public sealed record ObligationInput(string? ObligationId, string Text, bool Mandatory);

/// <summary>Sets the complete obligation list of a journey's checklist. <paramref name="ChecklistId"/> may only be null or the journey id (Ruling 176).</summary>
public sealed record ObligationsSetRequest(
    string JourneyId, string IntentNotebookId, IReadOnlyList<ObligationInput> Obligations, string? ChecklistId = null);

/// <summary>The checklist revision now at the head, whether this call wrote it or found it.</summary>
public sealed record ObligationsSetResult(int Revision);

/// <summary>Records one human disposition. An obligation kind needs <paramref name="ObligationId"/>; <c>intent-accepted</c> needs none.</summary>
public sealed record DispositionRecordRequest(
    string JourneyId, string IntentNotebookId, string? ObligationId, string Kind, string? Reason = null);

/// <summary>The disposition row this call wrote.</summary>
public sealed record DispositionRecordResult(DispositionRow Disposition);

/// <summary>Links one completed attempt to one obligation as evidence.</summary>
public sealed record EvidenceLinkRequest(
    string JourneyId, string IntentNotebookId, string ObligationId, string DispatchKey, string? LiveProjectionSha = null);

/// <summary>The link row, whether this call wrote it or found an identical current one.</summary>
public sealed record EvidenceLinkResult(EvidenceLinkRow Link);

/// <summary>Asks whether the work may be accepted now.</summary>
public sealed record ChecklistEvaluateRequest(string JourneyId, string IntentNotebookId, string? LiveProjectionSha = null);

/// <summary>Acceptance eligibility, work closure, and the attempts that could still be linked as evidence.</summary>
public sealed record ChecklistEvaluateResult(
    AcceptanceEvaluation Evaluation, WorkClosure Closure, IReadOnlyList<string> EvidenceCandidates);

/// <summary>Accepts the work's declared deliverable: the intent notebook's latest revision.</summary>
public sealed record WorkAcceptRequest(string JourneyId, string IntentNotebookId, string? LiveProjectionSha = null);

/// <summary>The acceptance. It names the intent revision by number and claims nothing about implementation (Ruling 179).</summary>
public sealed record WorkAcceptResult(WorkClosure Closure, int IntentRevisionNo, string Line);

/// <summary>The five checklist operations, answered where the store is. Each write is one writer transaction.</summary>
public sealed class ChecklistService(WorkspaceStore store)
{
    public ChecklistEvaluateResult Evaluate(ChecklistEvaluateRequest request) =>
        throw new NotImplementedException();

    public ObligationsSetResult SetObligations(ObligationsSetRequest request) =>
        throw new NotImplementedException();

    public DispositionRecordResult RecordDisposition(DispositionRecordRequest request, string actor) =>
        throw new NotImplementedException();

    public EvidenceLinkResult LinkEvidence(EvidenceLinkRequest request, string actor) =>
        throw new NotImplementedException();

    public WorkAcceptResult Accept(WorkAcceptRequest request, string actor)
    {
        _ = store;
        throw new NotImplementedException();
    }
}
