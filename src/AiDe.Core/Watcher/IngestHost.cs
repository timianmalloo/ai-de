using System.Threading.Channels;

namespace AiDe.Core.Watcher;

/// <summary>A harness event. Registration/heartbeat are handled synchronously; spans are queued.</summary>
public abstract record HarnessEvent;

/// <summary>An observed span plus the capability the emitting process presented for its session.</summary>
public sealed record HarnessSpanEvent(SessionCapability Capability, HarnessSpan Span) : HarnessEvent;

/// <summary>
/// The transport port. An OTLP network receiver (slice 1b) or an in-process source implements this and
/// feeds <see cref="IngestHost.Enqueue"/>. Defined here as the seam; the host itself is transport-neutral.
/// </summary>
public interface IHarnessEventSource
{
    IAsyncEnumerable<HarnessSpanEvent> ReadSpansAsync(CancellationToken ct);
}

/// <summary>
/// A snapshot of the ingest counters - the operator questions answerable without a debugger (IO1):
/// how many spans came in, were dropped under load, stored, deduped, rejected as forged, or quarantined.
/// </summary>
public sealed record IngestStats(
    long Enqueued, long Dropped, long Ingested, long Deduped, long Rejected, long Quarantined);

/// <summary>
/// Hosts the ingest path: synchronous registration/heartbeat, plus an async, bounded span stream drained
/// into <see cref="OtelSpanMapper"/> + <see cref="SpanIngest"/>. A span flood is absorbed by the bounded
/// queue (drop-oldest), a forged span is rejected, and a malformed one is quarantined - one bad event can
/// never kill the drain loop, and every disposition increments a visible counter (US-11 fail honestly).
///
/// Pattern: bounded producer/consumer (LOA Channel&lt;T&gt; backpressure) - the repo's
/// <c>Channel.CreateBounded</c> + <c>DropOldest</c> idiom (ConPtyTerminalSession).
/// </summary>
public sealed class IngestHost
{
    private readonly Channel<HarnessSpanEvent> _queue;
    private readonly SpanIngest _ingest;
    private readonly ITrustedRegistrar _registrar;
    private readonly IWatcherObservationStore _store;
    private readonly TimeProvider _time;

    private long _enqueued;
    private long _dropped;
    private long _ingested;
    private long _deduped;
    private long _rejected;
    private long _quarantined;

    public IngestHost(
        IWatcherObservationStore store,
        ITrustedRegistrar registrar,
        TimeProvider time,
        int queueCapacity = 1024)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentOutOfRangeException.ThrowIfLessThan(queueCapacity, 1);

        _registrar = registrar;
        _time = time;
        _store = store;
        _ingest = new SpanIngest(store, registrar);

        // DropOldest keeps the freshest spans under load; the itemDropped callback makes every drop a
        // visible coverage-gap signal rather than a silent loss.
        _queue = Channel.CreateBounded<HarnessSpanEvent>(
            new BoundedChannelOptions(queueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
            },
            itemDropped: _ => Interlocked.Increment(ref _dropped));
    }

    /// <summary>Maps and registers a session synchronously, returning its capability (LK-0004/LK-0002).</summary>
    public RegisteredSession Register(HarnessRegistration registration)
        => _registrar.Register(OtelSpanMapper.MapRegistration(registration));

    /// <summary>Records a heartbeat after verifying the capability (LK-0001).</summary>
    public void Heartbeat(string sessionId, SessionCapability capability)
        => _registrar.Heartbeat(sessionId, capability);

    /// <summary>
    /// Marks a session ended (its terminal closed / it reported session-end). Liveness then reads Ended
    /// rather than lingering Alive/Stale. Called by the coordination ingest on a session-end event; the
    /// registrar's re-registration path clears the ended mark for a fresh generation.
    /// </summary>
    public void EndSession(string sessionId) => _store.MarkEnded(sessionId);

    /// <summary>
    /// Enqueues a span event. Never blocks: under load the bounded queue drops its oldest item (counted),
    /// so a flood degrades to a coverage gap rather than unbounded growth.
    /// </summary>
    public void Enqueue(HarnessSpanEvent spanEvent)
    {
        ArgumentNullException.ThrowIfNull(spanEvent);
        // With DropOldest, TryWrite always accepts the new item; a displaced older item fires itemDropped.
        _queue.Writer.TryWrite(spanEvent);
        Interlocked.Increment(ref _enqueued);
    }

    /// <summary>
    /// Processes every span currently queued and returns the count. Deterministic (no waiting), so tests
    /// drain exactly what they enqueued.
    /// </summary>
    public int DrainAvailable()
    {
        var processed = 0;
        while (_queue.Reader.TryRead(out var spanEvent))
        {
            Process(spanEvent);
            processed++;
        }

        return processed;
    }

    /// <summary>The production loop: wait for spans, then drain, until cancelled.</summary>
    public async Task RunAsync(CancellationToken ct)
    {
        while (await _queue.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
        {
            DrainAvailable();
        }
    }

    /// <summary>A point-in-time snapshot of the counters.</summary>
    public IngestStats Stats => new(
        Interlocked.Read(ref _enqueued),
        Interlocked.Read(ref _dropped),
        Interlocked.Read(ref _ingested),
        Interlocked.Read(ref _deduped),
        Interlocked.Read(ref _rejected),
        Interlocked.Read(ref _quarantined));

    private void Process(HarnessSpanEvent spanEvent)
    {
        ObservedSpan observed;
        try
        {
            observed = OtelSpanMapper.MapSpan(spanEvent.Span, _time.GetUtcNow());
        }
        catch (WatcherException ex) when (ex.Code == WatcherErrorCodes.MalformedEvent)
        {
            // A malformed span is dropped and counted; the loop must survive it (US-11).
            Interlocked.Increment(ref _quarantined);
            return;
        }

        switch (_ingest.Ingest(observed.SessionId, spanEvent.Capability, observed))
        {
            case IngestOutcome.Accepted:
                Interlocked.Increment(ref _ingested);
                break;
            case IngestOutcome.DuplicateIgnored:
                Interlocked.Increment(ref _deduped);
                break;
            case IngestOutcome.Rejected:
                Interlocked.Increment(ref _rejected);
                break;
        }
    }
}
