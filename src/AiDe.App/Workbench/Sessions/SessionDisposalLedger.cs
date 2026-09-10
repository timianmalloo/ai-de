using System.Diagnostics;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// Where the session document and its lanes announce that they are being disposed. Emitted on the
/// normal path, with no flag, so the measurement exists whether or not anything is listening (IO2).
/// </summary>
/// <remarks>
/// <b>Started at the top of <c>Dispose</c>, before any teardown work.</b> That records the
/// <i>attempt</i> rather than the success, for the reason
/// <see cref="AiDe.Core.AgentPlane.TerminalHostingLedger"/> records it: a disposal that then threw
/// still disposed. With no listener attached, <see cref="ActivitySource.StartActivity(string, ActivityKind)"/>
/// returns null and costs nothing.
/// </remarks>
internal static class SessionDisposalSignal
{
    internal static readonly ActivitySource Source = new(SessionDisposalLedger.DisposalActivitySource);
}

/// <summary>
/// Counts session-surface and session-lane disposals while it is open — the oracle ADR-0017's
/// retain-never-rebuild claim needs and <c>Assert.Same</c> cannot supply.
/// </summary>
/// <remarks>
/// <para><b><c>Assert.Same</c> passes on a disposed instance.</b> A mode switch that disposed the
/// console surface and then handed the same reference back would satisfy a reference check exactly
/// as a correct retain does, so the reference check cannot tell "retained" from "retained and
/// killed". This makes disposal a number, and the number can be shown going to 1 — the companion
/// falsifier in <c>ModeSwitchRetainsTheSurfaceAndTheLaneTests</c> takes the rebuild path and shows
/// all three oracles going red together.</para>
///
/// <para><b>It listens rather than instruments, deliberately.</b> The two disposal sites already
/// have to run code to tear themselves down; this rides that emission rather than adding a counter
/// inside the thing being measured, which an edit to that thing could remove with nothing noticing.
/// The idiom, including this paragraph's reason, is
/// <see cref="AiDe.Core.AgentPlane.TerminalHostingLedger"/>'s.</para>
///
/// <para><b>It counts the attempt, not the success</b> — see <see cref="SessionDisposalSignal"/>.</para>
///
/// <para><b>Scoped, because an <see cref="ActivityListener"/> is process-global.</b> The count
/// belongs to one exercise, so the ledger is a disposable window over one. Two overlapping ledgers
/// each see every disposal in the process, which is correct for a claim of the form "nothing was
/// disposed anywhere while this ran".</para>
/// </remarks>
public sealed class SessionDisposalLedger : IDisposable
{
    /// <summary>The activity source the session document and its lanes publish on.</summary>
    public const string DisposalActivitySource = "aide.session.disposal";

    /// <summary>The activity a session surface opens for one disposal.</summary>
    public const string SurfaceDisposeActivity = "session.surface.dispose";

    /// <summary>The activity a session lane opens for one disposal.</summary>
    public const string LaneDisposeActivity = "session.lane.dispose";

    private readonly ActivityListener _listener;
    private long _surfaces;
    private long _lanes;

    private SessionDisposalLedger()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => string.Equals(source.Name, DisposalActivitySource, StringComparison.Ordinal),

            // AllDataAndRecorded rather than PropagationData: a sampler that declines leaves
            // StartActivity returning null, and the disposal would then be invisible to the very
            // counter that exists to see it.
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                if (string.Equals(activity.OperationName, SurfaceDisposeActivity, StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref _surfaces);
                }
                else if (string.Equals(activity.OperationName, LaneDisposeActivity, StringComparison.Ordinal))
                {
                    Interlocked.Increment(ref _lanes);
                }
            },
        };

        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>How many session surfaces were disposed since this ledger opened.</summary>
    public long Surfaces => Interlocked.Read(ref _surfaces);

    /// <summary>How many session lanes were disposed since this ledger opened.</summary>
    public long Lanes => Interlocked.Read(ref _lanes);

    /// <summary>Surfaces plus lanes — the single number the retain clause asserts is zero.</summary>
    public long Total => Surfaces + Lanes;

    /// <summary>Opens a ledger. Counting starts here and stops at <see cref="Dispose"/>.</summary>
    public static SessionDisposalLedger Open() => new();

    public void Dispose() => _listener.Dispose();
}
