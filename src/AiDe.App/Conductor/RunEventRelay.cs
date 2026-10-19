using System.Threading.Channels;
using AiDe.Core.AgentPlane;

namespace AiDe.App.Conductor;

/// <summary>
/// Carries the events <see cref="GovernedRunHost"/> drains to every other consumer — one channel per
/// subscriber (ADR-0041 §3: lane, fold, evaluation). <see cref="Reader"/> is the first subscriber's, the
/// shape <c>SessionLane</c> already takes.
/// </summary>
/// <remarks>
/// <para><b>Why a relay and not a second reader.</b> <see cref="AcpEventQueue"/> is
/// <c>SingleReader = true</c> and the host's own loop is that reader, so "let the console read the
/// plane's queue too" is impossible by construction rather than merely unwise. The host therefore
/// <i>republishes</i> what it has already drained, and this is the channel it republishes into. The
/// events are the same instances — nothing is re-normalized, re-sequenced or re-stamped, so no
/// consumer can hold a second opinion about what arrived (DM7).</para>
///
/// <para><b>Fan-out at publish, never a second reader.</b> <see cref="Publish"/> writes the same instance
/// into every subscriber's own <c>SingleReader</c> channel, so no channel ever has two readers. A subscriber
/// that joins late misses what was published before it and catches up from the run log by
/// <c>seq &gt; last_seen</c> (<see cref="AttemptSubscription"/>).</para>
///
/// <para><b>Unbounded, and that is a bounded risk.</b> A bounded relay would back-pressure the host
/// — and therefore the engine — on a consumer that stopped reading, which turns a closed pane into a
/// stalled run; an unbounded one holds at most one run's events. The retention cap is WL-4's. What is
/// not acceptable is losing an event silently, so a publish that cannot land is <b>counted</b> as
/// <see cref="Refused"/> rather than dropped.</para>
/// </remarks>
public sealed class RunEventRelay : IDisposable
{
    private readonly Lock _gate = new();
    private Channel<ObservedRunEvent>[] _subscribers;
    private bool _completed;

    private long _published;
    private long _refused;
    private long _highWater;

    public RunEventRelay()
    {
        var lane = NewChannel();
        _subscribers = [lane];
        Reader = lane.Reader;
    }

    /// <summary>The first subscriber's side — hand this straight to a <c>SessionLane</c>.</summary>
    public ChannelReader<ObservedRunEvent> Reader { get; }

    /// <summary>How many events reached the relay. The number the run's own count is compared with.</summary>
    public long Published => Interlocked.Read(ref _published);

    /// <summary>
    /// How many subscriber writes could not land. Above zero means a consumer saw fewer events than the run
    /// did, and it says so instead of the two numbers quietly disagreeing.
    /// </summary>
    public long Refused => Interlocked.Read(ref _refused);

    /// <summary>The highest <see cref="RunEvent.Seq"/> published so far; 0 before the first. A Stop records it (ADR-0041 §4).</summary>
    public long HighWaterSeq => Interlocked.Read(ref _highWater);

    /// <summary>A new subscriber's own channel; already completed when the relay is.</summary>
    public ChannelReader<ObservedRunEvent> Subscribe()
    {
        var channel = NewChannel();
        lock (_gate)
        {
            if (_completed)
            {
                channel.Writer.TryComplete();
            }
            else
            {
                _subscribers = [.. _subscribers, channel];
            }
        }

        return channel.Reader;
    }

    /// <summary>The sink itself: pass this as <c>GovernedRunHost.RunAsync</c>'s <c>sink</c>.</summary>
    public void Publish(ObservedRunEvent observed)
    {
        ArgumentNullException.ThrowIfNull(observed);

        Channel<ObservedRunEvent>[] subscribers;
        lock (_gate)
        {
            subscribers = _subscribers;
        }

        foreach (var subscriber in subscribers)
        {
            if (!subscriber.Writer.TryWrite(observed))
            {
                Interlocked.Increment(ref _refused);
            }
        }

        Interlocked.Increment(ref _published);
        if (observed.Event.Seq > Interlocked.Read(ref _highWater))
        {
            Interlocked.Exchange(ref _highWater, observed.Event.Seq);
        }
    }

    /// <summary>
    /// Closes every subscriber's channel, which ends each drain once it has read what is already queued.
    /// </summary>
    /// <remarks>
    /// Called when the run ends, not when the document closes: a lane whose run is over should stop
    /// waiting, and a lane whose document is closing is disposed by the document.
    /// </remarks>
    public void Complete()
    {
        Channel<ObservedRunEvent>[] subscribers;
        lock (_gate)
        {
            _completed = true;
            subscribers = _subscribers;
        }

        foreach (var subscriber in subscribers)
        {
            subscriber.Writer.TryComplete();
        }
    }

    /// <summary>Closes the relay — <see cref="Complete"/>, so a `using` and an explicit end agree.</summary>
    public void Dispose() => Complete();

    // One writer (the host's drain) and one reader per channel, exactly as AcpEventQueue declares them.
    private static Channel<ObservedRunEvent> NewChannel() =>
        Channel.CreateUnbounded<ObservedRunEvent>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
}
