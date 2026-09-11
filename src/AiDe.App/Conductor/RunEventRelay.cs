using System.Threading.Channels;
using AiDe.Core.AgentPlane;

namespace AiDe.App.Conductor;

/// <summary>
/// Carries the events <see cref="GovernedRunHost"/> drains to a second consumer — the shape
/// <c>SessionLane</c> already takes, so nothing on the console side has to change to receive them.
/// </summary>
/// <remarks>
/// <para><b>Why a relay and not a second reader.</b> <see cref="AcpEventQueue"/> is
/// <c>SingleReader = true</c> and the host's own loop is that reader, so "let the console read the
/// plane's queue too" is impossible by construction rather than merely unwise. The host therefore
/// <i>republishes</i> what it has already drained, and this is the channel it republishes into. The
/// events are the same instances — nothing is re-normalized, re-sequenced or re-stamped, so the
/// console cannot hold a second opinion about what arrived (DM7).</para>
///
/// <para><b>It fits <c>SessionLane</c>'s existing constructor.</b> <see cref="Reader"/> is a
/// <see cref="ChannelReader{T}"/> of <see cref="ObservedRunEvent"/>, which is exactly the parameter
/// the lane already declares, so the seam is a new object rather than a changed contract on a type
/// another node had just finished.</para>
///
/// <para><b>Unbounded, and that is a bounded risk.</b> A bounded relay would back-pressure the host
/// — and therefore the engine — on a console that stopped reading, which turns a closed pane into a
/// stalled run; an unbounded one holds at most one run's events and is collected with the document
/// that opened it. What is not acceptable is losing an event silently, because the equality the
/// sink's oracle asserts would then quietly become an approximation, so a publish that cannot land
/// is <b>counted</b> as <see cref="Refused"/> rather than dropped.</para>
/// </remarks>
public sealed class RunEventRelay : IDisposable
{
    private readonly Channel<ObservedRunEvent> _channel = Channel.CreateUnbounded<ObservedRunEvent>(
        // One writer (the host's drain) and one reader (the lane), exactly as AcpEventQueue declares
        // them — the relay must not be the place a second reader becomes possible again.
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    private long _published;
    private long _refused;

    /// <summary>The consumer side — hand this straight to a <c>SessionLane</c>.</summary>
    public ChannelReader<ObservedRunEvent> Reader => _channel.Reader;

    /// <summary>How many events reached the relay. The number the run's own count is compared with.</summary>
    public long Published => Interlocked.Read(ref _published);

    /// <summary>
    /// How many could not. Above zero means the console saw fewer events than the run did, and it
    /// says so instead of the two numbers quietly disagreeing.
    /// </summary>
    public long Refused => Interlocked.Read(ref _refused);

    /// <summary>The sink itself: pass this as <c>GovernedRunHost.RunAsync</c>'s <c>sink</c>.</summary>
    public void Publish(ObservedRunEvent observed)
    {
        ArgumentNullException.ThrowIfNull(observed);

        if (_channel.Writer.TryWrite(observed))
        {
            Interlocked.Increment(ref _published);
            return;
        }

        Interlocked.Increment(ref _refused);
    }

    /// <summary>
    /// Closes the relay, which ends the lane's drain once it has read what is already queued.
    /// </summary>
    /// <remarks>
    /// Called when the run ends, not when the document closes: a lane whose run is over should stop
    /// waiting, and a lane whose document is closing is disposed by the document.
    /// </remarks>
    public void Complete() => _channel.Writer.TryComplete();

    public void Dispose() => Complete();
}
