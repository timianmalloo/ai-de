using System.Diagnostics;
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

    public static AcceptanceEvaluation Evaluate(AcceptanceInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var started = Stopwatch.GetTimestamp();

        var result = Fold(input);

        FoldSignal.Record("acceptance", Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return result;
    }

    private static AcceptanceEvaluation Fold(AcceptanceInput input)
    {
        var baseline = AcceptanceBaseline.For(input.IntentNotebookId, input.IntentLatestRevisionNo);
        var obligations = input.CurrentObligations.Select(o => Evaluate(input, o)).ToList();
        var gaps = new List<AcceptanceGap>();

        if (obligations.Count == 0)
        {
            gaps.Add(new AcceptanceGap(AcceptanceGapCodes.ChecklistEmpty, null,
                "Blocked — no obligations are declared for this work."));
        }

        if (input.IntentLatestRevisionNo is null)
        {
            gaps.Add(new AcceptanceGap(AcceptanceGapCodes.IntentMissing, null,
                "Blocked — the intent notebook has no saved revision."));
        }

        foreach (var o in obligations.Where(o => o.Mandatory))
        {
            gaps.AddRange(ObligationGaps(input, o));
        }

        if (input.IntentLatestRevisionNo is { } revision && !IntentAccepted(input, revision))
        {
            gaps.Add(new AcceptanceGap(AcceptanceGapCodes.IntentNotAccepted, null,
                $"Blocked — intent revision {revision} is not accepted."));
        }

        return new AcceptanceEvaluation(baseline, obligations, gaps, gaps.Count == 0);
    }

    private static ObligationEvaluation Evaluate(AcceptanceInput input, ObligationRow obligation)
    {
        var links = input.Links.GetValueOrDefault(obligation.ObligationId) ?? [];
        var verdicts = links
            .Where(l => l.VerificationStatus == ObservedStatus)
            .Select(l => (Link: l, Verdict: Judge(input, obligation, l)))
            .ToList();
        var cited = verdicts.Where(v => v.Verdict == EvidenceState.Satisfied).Select(v => v.Link).ToList();
        var human = LatestPerActor(input, obligation.ObligationId);

        return new ObligationEvaluation(
            obligation.ObligationId, obligation.Text, obligation.Mandatory,
            Strongest(verdicts.Select(v => v.Verdict)), ShownState(human), cited);
    }

    /// <summary>Satisfied beats Stale beats NotRecorded beats Missing: the state a reader can act on first.</summary>
    private static EvidenceState Strongest(IEnumerable<EvidenceState> verdicts) =>
        verdicts.DefaultIfEmpty(EvidenceState.Missing).MinBy(v => v switch
        {
            EvidenceState.Satisfied => 0,
            EvidenceState.Stale => 1,
            EvidenceState.NotRecorded => 2,
            _ => 3,
        });

    /// <summary>Baseline and criterion first, then the attempt's currentness; an unknown attempt is never current.</summary>
    private static EvidenceState Judge(AcceptanceInput input, ObligationRow now, EvidenceLinkRow link)
    {
        var baseline = AcceptanceBaseline.For(input.IntentNotebookId, input.IntentLatestRevisionNo);
        if (!string.Equals(link.Baseline, baseline, StringComparison.Ordinal) || !CriterionUnchanged(input, now, link))
        {
            return EvidenceState.Stale;
        }

        if (!TryAttemptKey(link.ArtifactRef, out var key) || !input.LinkedAttempts.TryGetValue(key, out var attempt))
        {
            return EvidenceState.NotRecorded;
        }

        return CurrentnessFold.Evaluate(attempt, input.IntentLatestRevisionNo, input.LiveProjectionSha).State switch
        {
            Currentness.Current => EvidenceState.Satisfied,
            Currentness.Stale => EvidenceState.Stale,
            _ => EvidenceState.NotRecorded,
        };
    }

    private static bool CriterionUnchanged(AcceptanceInput input, ObligationRow now, EvidenceLinkRow link)
    {
        var then = input.ObligationHistory.GetValueOrDefault(now.ObligationId)?.FirstOrDefault(o => o.Revision == link.SubjectRevision);
        return then is not null && then.Text == now.Text && then.Mandatory == now.Mandatory;
    }

    private static bool TryAttemptKey(string artifactRef, out string key)
    {
        var prefix = AttemptStateFold.AttemptArtifactRef("");
        key = artifactRef.StartsWith(prefix, StringComparison.Ordinal) ? artifactRef[prefix.Length..] : "";
        return key.Length > 0;
    }

    private static List<DispositionRow> LatestPerActor(AcceptanceInput input, string obligationId) =>
        (input.ObligationDispositions.GetValueOrDefault(obligationId) ?? [])
            .GroupBy(d => d.Actor, StringComparer.Ordinal)
            .Select(g => g.MaxBy(d => d.IngressSeq)!)
            .ToList();

    private static HumanState ShownState(List<DispositionRow> latestPerActor) =>
        latestPerActor.MaxBy(d => d.IngressSeq)?.Kind switch
        {
            "acknowledged" => HumanState.Acknowledged,
            "deferred" => HumanState.Deferred,
            "needs-decision" => HumanState.NeedsDecision,
            _ => HumanState.None,
        };

    private static IEnumerable<AcceptanceGap> ObligationGaps(AcceptanceInput input, ObligationEvaluation o)
    {
        var latest = LatestPerActor(input, o.ObligationId);
        var covered = latest.Any(d => AcceptancePolicy.Covers(d.Kind));

        if (o.Evidence != EvidenceState.Satisfied && !covered)
        {
            yield return o.Evidence switch
            {
                EvidenceState.Stale => new AcceptanceGap(AcceptanceGapCodes.EvidenceStale, o.ObligationId,
                    $"Blocked — “{o.Text}” has only evidence from an earlier draft."),
                EvidenceState.NotRecorded => new AcceptanceGap(AcceptanceGapCodes.EvidenceNotRecorded, o.ObligationId,
                    $"Blocked — whether the evidence for “{o.Text}” is current is not recorded."),
                _ => new AcceptanceGap(AcceptanceGapCodes.EvidenceMissing, o.ObligationId,
                    $"Blocked — “{o.Text}” has no current evidence."),
            };
        }

        if (latest.Any(d => d.Kind == "needs-decision"))
        {
            yield return new AcceptanceGap(AcceptanceGapCodes.NeedsDecision, o.ObligationId,
                $"Blocked — “{o.Text}” needs a decision.");
        }
    }

    private static bool IntentAccepted(AcceptanceInput input, int revision) =>
        input.IntentDispositions.Any(d => d.Kind == "intent-accepted"
            && d.NotebookId == input.IntentNotebookId
            && d.RevisionNo == revision);
}
