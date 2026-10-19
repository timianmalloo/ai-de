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
    /// <summary>The longest obligation text or disposition reason one frame may carry.</summary>
    public const int MaxTextLength = 2000;

    public ChecklistEvaluateResult Evaluate(ChecklistEvaluateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var reader = store.BeginRead();
        RequireWorkJourney(reader, request.JourneyId, request.IntentNotebookId);
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
        RequireWorkJourney(reader, request.JourneyId, request.IntentNotebookId);

        // simplify: one checklist per journey, checklist_id = journey_id (Ruling 176). Ceiling: WL-1b and WL-1c need no
        // more. Upgrade trigger: the first spec story that needs two checklists in one journey requires the v3 owner
        // column on checklist_obligation_fact, backfilled from this alias.
        if (request.ChecklistId is not null && request.ChecklistId != request.JourneyId)
        {
            throw new WorkspaceStoreException(StoreErrorCodes.WorkSecondChecklist,
                $"'{request.ChecklistId}' would be a second checklist for journey '{request.JourneyId}'; a journey has one");
        }

        var latest = reader.ReadLatestChecklistRevision(request.JourneyId);
        var current = latest is { } revision ? reader.ReadObligations(request.JourneyId, revision) : [];
        foreach (var o in request.Obligations)
        {
            RequireWithinCap(o.Text, "obligation text");
        }

        var known = reader.ReadObligationHistory(request.JourneyId).Select(o => o.ObligationId).ToHashSet(StringComparer.Ordinal);
        var unknown = request.Obligations.FirstOrDefault(o => o.ObligationId is not null && !known.Contains(o.ObligationId));
        if (unknown is not null)
        {
            throw UnknownObligation(unknown.ObligationId!);
        }

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
        RequireWorkJourney(reader, request.JourneyId, request.IntentNotebookId);
        var head = reader.ReadLatestNotebookRevision(request.IntentNotebookId);
        var baseline = AcceptanceBaseline.For(request.IntentNotebookId, head?.RevisionNo);
        var isIntent = request.Kind == "intent-accepted";
        RequireWithinCap(request.Reason, "disposition reason");
        if (!isIntent)
        {
            RequireKnownObligation(reader, request.JourneyId, request.ObligationId);
        }

        RequireKindForSubject(isIntent ? "intent" : "obligation", request.Kind);

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
        RequireWorkJourney(reader, request.JourneyId, request.IntentNotebookId);
        var head = reader.ReadLatestNotebookRevision(request.IntentNotebookId);
        RequireKnownObligation(reader, request.JourneyId, request.ObligationId);
        RequireEligibleAttempt(reader, request, head?.RevisionNo);

        var artifactRef = AttemptStateFold.AttemptArtifactRef(request.DispatchKey);
        var baseline = AcceptanceBaseline.For(request.IntentNotebookId, head?.RevisionNo);
        var checklistRevision = reader.ReadLatestChecklistRevision(request.JourneyId)!.Value;
        var existing = reader.ReadEvidenceLinksForSubject("obligation", request.ObligationId).FirstOrDefault(l =>
            l.ArtifactRef == artifactRef && l.Baseline == baseline && l.SubjectRevision == checklistRevision
            && l.VerificationStatus == AcceptanceFold.ObservedStatus);
        if (existing is not null)
        {
            return new EvidenceLinkResult(existing);
        }

        var link = new EvidenceLinkRow(
            "lnk-" + Guid.NewGuid().ToString("N"), "obligation", request.ObligationId, checklistRevision,
            artifactRef, actor, baseline, AcceptanceFold.ObservedStatus);
        writer.AppendEvidenceLink(link);
        writer.Commit();

        using var after = store.BeginRead();
        return new EvidenceLinkResult(
            after.ReadEvidenceLinksForSubject("obligation", request.ObligationId).Single(l => l.LinkId == link.LinkId));
    }

    public WorkAcceptResult Accept(WorkAcceptRequest request, string actor)
    {
        ArgumentNullException.ThrowIfNull(request);
        var started = Stopwatch.GetTimestamp();

        try
        {
            using var writer = store.BeginWrite();
            using var reader = store.BeginRead();
            RequireWorkJourney(reader, request.JourneyId, request.IntentNotebookId);
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

    /// <summary>
    /// The journey must be a real work journey (not the standing terminal-drafts owner) that owns the intent
    /// notebook. A legacy, terminal or investigation notebook is refused, never adopted (Ruling 156).
    /// </summary>
    private static void RequireWorkJourney(StoreReader reader, string journeyId, string notebookId)
    {
        var journey = reader.ReadJourney(journeyId);
        var notebook = reader.ReadNotebook(notebookId);
        if (journey is null || journey.Purpose != "work" || journeyId == WorkspaceSchema.TerminalJourneyId
            || notebook?.OwnerJourneyId != journeyId)
        {
            throw new WorkspaceStoreException(StoreErrorCodes.WorkNotAWorkJourney,
                $"'{notebookId}' is not the intent notebook of a work journey '{journeyId}'");
        }
    }

    private static void RequireWithinCap(string? value, string field)
    {
        if (value is { Length: > MaxTextLength })
        {
            throw new WorkspaceStoreException(StoreErrorCodes.WorkFieldTooLong,
                $"{field} is {value.Length} characters; the limit is {MaxTextLength}");
        }
    }

    private static WorkspaceStoreException UnknownObligation(string obligationId) =>
        new(StoreErrorCodes.WorkObligationUnknown, $"'{obligationId}' is not an obligation of this checklist");

    private static void RequireKnownObligation(StoreReader reader, string journeyId, string? obligationId)
    {
        var latest = reader.ReadLatestChecklistRevision(journeyId);
        var inLatest = latest is { } revision
            && reader.ReadObligations(journeyId, revision).Any(o => o.ObligationId == obligationId);
        if (!inLatest)
        {
            throw UnknownObligation(obligationId ?? "(none)");
        }
    }

    private static void RequireKindForSubject(string subjectKind, string kind)
    {
        var allowed = subjectKind == "intent"
            ? WorkbenchVocabulary.DispositionKindsBySubject["intent"]
            : WorkbenchVocabulary.DispositionKindsBySubject["obligation"];
        if (!allowed.Contains(kind))
        {
            throw new WorkspaceStoreException(StoreErrorCodes.VocabularyViolation,
                $"'{kind}' is not a {subjectKind} disposition; the kinds are {string.Join(", ", allowed)}");
        }
    }

    /// <summary>
    /// The attempt must belong to this journey's intent notebook, have ended <c>ResponseComplete</c>, and still
    /// belong to the live draft. The message starts with the cause.
    /// </summary>
    private static void RequireEligibleAttempt(StoreReader reader, EvidenceLinkRequest request, int? headRevision)
    {
        var attempt = reader.ReadAttempt(request.DispatchKey);
        if (attempt is null || attempt.NotebookId != request.IntentNotebookId)
        {
            throw NotEligible("not-this-journey", "that attempt does not belong to this work's intent notebook");
        }

        if (!Completed(reader, request.DispatchKey))
        {
            throw NotEligible("not-complete", "that attempt has not ended with a complete response");
        }

        if (CurrentnessFold.Evaluate(attempt, headRevision, request.LiveProjectionSha).State != Currentness.Current)
        {
            throw NotEligible("not-current", CurrentnessFold.StaleCopy);
        }
    }

    private static WorkspaceStoreException NotEligible(string cause, string sentence) =>
        new(StoreErrorCodes.WorkEvidenceNotEligible, $"{cause}: {sentence}");

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
