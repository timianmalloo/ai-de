using System.Diagnostics;
using AiDe.Core.Presentation.Sessions;
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
/// <remarks>
/// A write holds the store's single writer gate before it reads, so nothing can commit between the read and the
/// append. <c>work.accept</c> relies on that: it re-folds eligibility under the gate and appends in the same
/// transaction (WL-1b design, "Error and concurrency model").
/// </remarks>
public sealed class ChecklistService(WorkspaceStore store)
{
    public ChecklistEvaluateResult Evaluate(ChecklistEvaluateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var reader = store.BeginRead();
        var evaluation = Assess(reader, request.JourneyId, request.IntentNotebookId, request.LiveProjectionSha);
        var closure = WorkClosureFold.Evaluate(reader.ReadDispositions("work", request.JourneyId), evaluation.Baseline);
        var headRevision = reader.ReadLatestNotebookRevision(request.IntentNotebookId)?.RevisionNo;
        var candidates = reader.ReadAttemptsForNotebook(request.IntentNotebookId)
            .Where(a => a.RevisionNo == headRevision && Completed(reader, a.DispatchKey))
            .Select(a => a.DispatchKey)
            .ToList();
        return new ChecklistEvaluateResult(evaluation, closure, candidates);
    }

    public ObligationsSetResult SetObligations(ObligationsSetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var writer = store.BeginWrite();
        using var reader = store.BeginRead();
        var latest = reader.ReadLatestChecklistRevision(request.JourneyId);
        var current = latest is { } revision ? reader.ReadObligations(request.JourneyId, revision) : [];
        var declared = request.Obligations
            .Select(o => (Id: o.ObligationId ?? MintObligationId(), o.Text, o.Mandatory))
            .ToList();

        if (latest is { } head && SameSet(current, request.Obligations, declared))
        {
            return new ObligationsSetResult(head);
        }

        var next = (latest ?? 0) + 1;
        foreach (var (id, text, mandatory) in declared)
        {
            writer.AppendObligation(new ObligationRow(id, request.JourneyId, next, text, mandatory));
        }

        writer.Commit();
        return new ObligationsSetResult(next);
    }

    public DispositionRecordResult RecordDisposition(DispositionRecordRequest request, string actor)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var writer = store.BeginWrite();
        using var reader = store.BeginRead();
        var head = reader.ReadLatestNotebookRevision(request.IntentNotebookId);
        var baseline = AcceptanceBaseline.For(request.IntentNotebookId, head?.RevisionNo);
        var isIntent = request.Kind == "intent-accepted";

        writer.AppendDisposition(isIntent
            ? new DispositionRow("intent", request.JourneyId, request.Kind, actor, baseline, request.Reason,
                request.IntentNotebookId, head?.RevisionNo)
            : new DispositionRow("obligation", request.ObligationId!, request.Kind, actor, baseline, request.Reason));
        writer.Commit();

        using var after = store.BeginRead();
        var (kind, id) = isIntent ? ("intent", request.JourneyId) : ("obligation", request.ObligationId!);
        return new DispositionRecordResult(after.ReadDispositions(kind, id).Last());
    }

    public EvidenceLinkResult LinkEvidence(EvidenceLinkRequest request, string actor)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var writer = store.BeginWrite();
        using var reader = store.BeginRead();
        var head = reader.ReadLatestNotebookRevision(request.IntentNotebookId);
        var link = new EvidenceLinkRow(
            "lnk-" + Guid.NewGuid().ToString("N"), "obligation", request.ObligationId,
            reader.ReadLatestChecklistRevision(request.JourneyId) ?? 0,
            AttemptStateFold.AttemptArtifactRef(request.DispatchKey), actor,
            AcceptanceBaseline.For(request.IntentNotebookId, head?.RevisionNo), AcceptanceFold.ObservedStatus);
        writer.AppendEvidenceLink(link);
        writer.Commit();
        return new EvidenceLinkResult(link);
    }

    public WorkAcceptResult Accept(WorkAcceptRequest request, string actor)
    {
        ArgumentNullException.ThrowIfNull(request);
        var started = Stopwatch.GetTimestamp();

        try
        {
            using var writer = store.BeginWrite();
            using var reader = store.BeginRead();
            var evaluation = Assess(reader, request.JourneyId, request.IntentNotebookId, request.LiveProjectionSha);
            var revision = reader.ReadLatestNotebookRevision(request.IntentNotebookId)?.RevisionNo;
            var existing = reader.ReadDispositions("work", request.JourneyId)
                .FirstOrDefault(d => d.Kind == "accepted" && d.Baseline == evaluation.Baseline);

            if (existing is null)
            {
                if (!evaluation.Eligible)
                {
                    throw new WorkspaceStoreException(StoreErrorCodes.WorkAcceptanceBlocked,
                        "the work cannot be accepted yet: "
                        + string.Join("; ", evaluation.Gaps.Select(g => $"[{g.Code}] {g.Sentence}")));
                }

                writer.AppendDisposition(new DispositionRow("work", request.JourneyId, "accepted", actor, evaluation.Baseline));
                writer.Commit();
            }

            var closure = new WorkClosure(WorkClosureState.Accepted, evaluation.Baseline, AtCurrentBaseline: true);
            return new WorkAcceptResult(closure, revision!.Value, $"Accepted — intent revision {revision}.");
        }
        finally
        {
            FoldSignal.Record("work.accept", Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    private static AcceptanceEvaluation Assess(StoreReader reader, string journeyId, string notebookId, string? liveProjectionSha)
    {
        const string AttemptPrefix = "attempt://";
        var revision = reader.ReadLatestNotebookRevision(notebookId)?.RevisionNo;
        var checklistRevision = reader.ReadLatestChecklistRevision(journeyId);
        var current = checklistRevision is { } head ? reader.ReadObligations(journeyId, head) : [];
        var history = reader.ReadObligationHistory(journeyId)
            .GroupBy(o => o.ObligationId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ObligationRow>)g.ToList(), StringComparer.Ordinal);
        var links = current.ToDictionary(o => o.ObligationId,
            o => reader.ReadEvidenceLinksForSubject("obligation", o.ObligationId), StringComparer.Ordinal);
        var dispositions = current.ToDictionary(o => o.ObligationId,
            o => reader.ReadDispositions("obligation", o.ObligationId), StringComparer.Ordinal);
        var attempts = links.Values.SelectMany(l => l)
            .Select(l => l.ArtifactRef)
            .Where(r => r.StartsWith(AttemptPrefix, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Select(r => reader.ReadAttempt(r[AttemptPrefix.Length..]))
            .OfType<AttemptRow>()
            .ToDictionary(a => a.DispatchKey, StringComparer.Ordinal);

        return AcceptanceFold.Evaluate(new AcceptanceInput(
            journeyId, notebookId, revision, current, history,
            links.ToDictionary(p => p.Key, p => (IReadOnlyList<EvidenceLinkRow>)p.Value, StringComparer.Ordinal),
            dispositions.ToDictionary(p => p.Key, p => (IReadOnlyList<DispositionRow>)p.Value, StringComparer.Ordinal),
            reader.ReadDispositions("intent", journeyId), attempts, liveProjectionSha));
    }

    private static bool Completed(StoreReader reader, string dispatchKey) =>
        AttemptStateFold.Evaluate(new AttemptStateInput(dispatchKey, [], reader.ReadOutcomes(dispatchKey), [], [])).State
            == TurnState.Completed;

    private static string MintObligationId() => "obl-" + Guid.NewGuid().ToString("N");

    private static bool SameSet(
        IReadOnlyList<ObligationRow> current,
        IReadOnlyList<ObligationInput> requested,
        List<(string Id, string Text, bool Mandatory)> declared) =>
        requested.All(o => o.ObligationId is not null)
        && current.Count == declared.Count
        && current.Zip(declared).All(p => p.First.ObligationId == p.Second.Id
            && p.First.Text == p.Second.Text && p.First.Mandatory == p.Second.Mandatory);
}
