using System.Diagnostics;
using System.Globalization;
using AiDe.Core.AgentPlane;
using AiDe.Core.Facts;
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
/// <remarks>
/// <para>Pure and total: no I/O and no store write. Terminals come only from the host's outcome rows
/// (lowest <c>ingress_seq</c> wins); an <c>acp.result</c> or <c>acp.error</c> event never yields one.
/// Precedence among non-terminals: StopRequested, OverLimit, Denied, Waiting, Running. A cited seq
/// the log lacks is reported in <see cref="AttemptState.NotRecordedSeqs"/> and never acted on.</para>
/// <para>assume: the budget, context-ceiling and input-request kinds below are this fold's own
/// vocabulary until WL-4 and the harness mapper emit them. Confirm: the mapper's kind list. If false:
/// OverLimit and input-Waiting never derive (they read Running), never a wrong terminal.</para>
/// <para>assume: a permission decision rejects when <c>OptionChosen</c> starts with <c>reject</c>
/// (ACP kinds <c>reject_once</c> / <c>reject_always</c>, adapter id <c>reject</c>). Confirm: the
/// decision writer's stored value. If false: Denied never derives.</para>
/// </remarks>
public static class AttemptStateFold
{
    private const string PermissionRequestKind = "permission.request";

    private static readonly string[] OverLimitKinds = ["budget.exceeded", "context.ceiling"];
    private static readonly string[] WaitingKinds = [PermissionRequestKind, "input.request"];

    private static readonly Dictionary<DispatchState, TurnState> TerminalStates = new()
    {
        [DispatchState.ResponseComplete] = TurnState.Completed,
        [DispatchState.Rejected] = TurnState.Rejected,
        [DispatchState.Failed] = TurnState.Failed,
        [DispatchState.DeliveryUnknown] = TurnState.DeliveryUnknown,
        [DispatchState.Interrupted] = TurnState.Interrupted,
    };

    /// <summary>The <c>artifact_ref</c> an evidence link to this attempt carries.</summary>
    /// <remarks>assume: WL-1b's evidence writer uses this form. Confirm: its first link row. If false: HasEvidence reads false.</remarks>
    public static string AttemptArtifactRef(string dispatchKey) => "attempt://" + dispatchKey;

    public static AttemptState Evaluate(AttemptStateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var started = Stopwatch.GetTimestamp();

        var result = Fold(input);

        FoldSignal.Record("attempt-state", Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return result;
    }

    private static AttemptState Fold(AttemptStateInput input)
    {
        var logSeqs = input.Events.Select(e => e.Seq).ToHashSet();
        var outcomes = input.Outcomes.OrderBy(o => o.IngressSeq).ToList();
        var stop = outcomes.FirstOrDefault(o => o.State == DispatchState.StopRequested);

        var missing = input.Decisions.Select(d => d.Seq).Where(s => !logSeqs.Contains(s)).ToList();
        if (stop?.Detail is { } detail
            && long.TryParse(detail, CultureInfo.InvariantCulture, out var highWater)
            && highWater > 0
            && !logSeqs.Contains(highWater))
        {
            missing.Add(highWater);
        }

        var notRecorded = missing.Distinct().Order().ToList();
        var hasEvidence = input.EvidenceLinks.Any(l => l.ArtifactRef == AttemptArtifactRef(input.DispatchKey));

        var terminal = outcomes.FirstOrDefault(o => TerminalStates.ContainsKey(o.State));
        if (terminal is not null)
        {
            return new AttemptState(TerminalStates[terminal.State], hasEvidence, terminal.Cause, notRecorded);
        }

        var dispatched = outcomes.Any(o => o.State == DispatchState.Dispatched);
        return new AttemptState(NonTerminal(input, logSeqs, stop is not null, dispatched), hasEvidence, null, notRecorded);
    }

    private static TurnState NonTerminal(AttemptStateInput input, HashSet<long> logSeqs, bool stopRequested, bool dispatched)
    {
        if (stopRequested)
        {
            return TurnState.StopRequested;
        }

        if (!dispatched && input.Events.Count == 0)
        {
            return TurnState.Preparing;
        }

        if (input.Events.Any(e => OverLimitKinds.Contains(e.Kind, StringComparer.Ordinal)))
        {
            return TurnState.OverLimit;
        }

        var last = input.Decisions.Where(d => logSeqs.Contains(d.Seq)).MaxBy(d => d.Seq);
        if (last is not null
            && last.OptionChosen.StartsWith("reject", StringComparison.OrdinalIgnoreCase)
            && input.Events.All(e => e.Seq <= last.Seq))
        {
            return TurnState.Denied;
        }

        var decided = input.Decisions.Select(d => d.Seq).ToHashSet();
        var waiting = input.Events.Any(e => WaitingKinds.Contains(e.Kind, StringComparer.Ordinal) && !decided.Contains(e.Seq));
        return waiting ? TurnState.Waiting : TurnState.Running;
    }
}

/// <summary>The fold instrumentation: one <c>fold.duration_ms</c> activity per evaluation (ADR-0041 §7).</summary>
/// <remarks>
/// Core publishes on an <see cref="ActivitySource"/> exactly as <c>CompileSignal</c> does; the host's
/// listener writes it to the rolling log. With no listener nothing is recorded, never a zero.
/// </remarks>
public static class FoldSignal
{
    public const string SourceName = "aide.attempt.fold";

    private static readonly ActivitySource Source = new(SourceName);

    internal static void Record(string fold, double durationMs)
    {
        using var activity = Source.StartActivity("fold.duration_ms");
        activity?.SetTag("fold", fold);
        activity?.SetTag("duration_ms", durationMs);
    }
}
