using System.Diagnostics;

namespace AiDe.App.Conductor;

/// <summary>
/// Where the one composition root announces that it is composing a governed run. Emitted on the
/// normal path, with no flag, so the measurement exists whether or not anything is listening (IO2).
/// </summary>
/// <remarks>
/// <b>Started before anything in <see cref="GovernedRunHost.RunAsync"/> that can throw.</b> That
/// records the <i>attempt</i> rather than the success, for the reason
/// <see cref="AiDe.Core.AgentPlane.TerminalHostingLedger"/> records it — and here the property is
/// load-bearing twice over: it is what lets clause 5's falsifier run without a live adapter. With no
/// listener attached, <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> returns null
/// and costs nothing.
/// </remarks>
internal static class CompositionRootSignal
{
    internal static readonly ActivitySource Source = new(CompositionRootLedger.CompositionActivitySource);
}

/// <summary>
/// Counts governed-run compositions while it is open — the ledger §F5 clause 5 asserts against when
/// it says the run was "launched through the same composition root, no second entry point".
/// </summary>
/// <remarks>
/// <para><b>"One composition root" is unfalsifiable as prose.</b> It reads identically whether the
/// UI really calls <see cref="GovernedRunHost.RunAsync"/> or whether a surface quietly assembled a
/// lane of its own — and N7's floor is explicit that a hand-assembled path is not exit evidence. This
/// makes the claim a number: one run, one root, <c>Roots == 1</c>.</para>
///
/// <para><b>It counts the attempt, not the success, and that is what makes the falsifier cheap.</b>
/// The activity opens before <c>EngineCatalog.ResolveLaunch</c> — the first statement in
/// <see cref="GovernedRunHost.RunAsync"/> that refuses — so two runs with an unknown engine id read
/// <c>2</c> with no adapter, no node and no network. Move the emission below anything throwable and
/// the falsifier needs a live subscription run to observe, which is a falsifier nobody re-runs.</para>
///
/// <para><b>It listens rather than instruments.</b> The idiom, including this reason, is
/// <see cref="AiDe.Core.AgentPlane.TerminalHostingLedger"/>'s: a counter added inside the thing being
/// measured is one an edit to that thing can remove with nothing noticing.</para>
///
/// <para><b>Scoped, because an <see cref="ActivityListener"/> is process-global.</b> The count
/// belongs to one exercise, so the ledger is a disposable window over one.</para>
/// </remarks>
public sealed class CompositionRootLedger : IDisposable
{
    /// <summary>The activity source the composition root publishes on.</summary>
    public const string CompositionActivitySource = "aide.conductor.composition";

    /// <summary>The activity <see cref="GovernedRunHost.RunAsync"/> opens for one composition.</summary>
    public const string GovernedRunComposeActivity = "governed-run.compose";

    private readonly ActivityListener _listener;
    private long _roots;

    private CompositionRootLedger()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source =>
                string.Equals(source.Name, CompositionActivitySource, StringComparison.Ordinal),

            // AllDataAndRecorded rather than PropagationData: a sampler that declines leaves
            // StartActivity returning null, and the composition would then be invisible to the very
            // counter that exists to see it.
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                if (string.Equals(activity.OperationName, GovernedRunComposeActivity, StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref _roots);
                }
            },
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>How many governed runs were composed since this ledger opened.</summary>
    public long Roots => Interlocked.Read(ref _roots);

    /// <summary>Opens a ledger. Counting starts here and stops at <see cref="Dispose"/>.</summary>
    public static CompositionRootLedger Open() => new();

    public void Dispose() => _listener.Dispose();
}
