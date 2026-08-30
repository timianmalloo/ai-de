namespace AiDe.Core.Watcher;

/// <summary>
/// The persistence seam for watcher observations. This is the mock-substitutable contract the
/// architecture names (§4): the in-memory implementation serves the Phase-1 walking skeleton; the
/// SQLite implementation (extending the existing <c>Store/</c>, ADR-0002) replaces it later as a
/// substitution, not a redesign. It holds non-secret facts only - never a <see cref="SessionCapability"/>.
/// </summary>
public interface IWatcherObservationStore
{
    /// <summary>
    /// Appends a span if its content-addressed id is new. Returns false when the id is already
    /// present, which is how duplicate/redelivered spans are ignored idempotently.
    /// </summary>
    bool TryAppendSpan(ObservedSpan span);

    /// <summary>Number of distinct spans stored for a session (a span count is additive).</summary>
    int SpanCount(string sessionId);

    /// <summary>Records the latest heartbeat for a session as a monotonic tick value.</summary>
    void UpsertHeartbeat(string sessionId, long monotonicTicks);

    /// <summary>The last heartbeat tick for a session, or null if none was recorded.</summary>
    long? LastHeartbeat(string sessionId);

    /// <summary>Records non-secret session metadata (binding, generation).</summary>
    void RecordSession(SessionRecord session);

    /// <summary>The current session metadata for an id, or null if unknown.</summary>
    SessionRecord? FindSession(string sessionId);

    /// <summary>Marks a session ended (terminal closed or superseded generation).</summary>
    void MarkEnded(string sessionId);

    /// <summary>Clears the ended mark (a new generation of a restarted session).</summary>
    void ClearEnded(string sessionId);

    /// <summary>Whether a session has been marked ended.</summary>
    bool IsEnded(string sessionId);
}

/// <summary>
/// In-memory observation store for the walking skeleton. Thread-safe under a single lock: writes
/// serialize through the daemon queue in production (ADR-0002), but a concurrent caller must still
/// never corrupt the store or double-append a span.
///
/// simplify: unbounded in memory. Ceiling: fine at the reference scale for the skeleton. Upgrade
/// trigger: the SQLite store lands (remaining Phase-1 task), which bounds and persists these facts.
/// </summary>
public sealed class InMemoryWatcherObservationStore : IWatcherObservationStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, HashSet<string>> _spanIdsBySession = new();
    private readonly Dictionary<string, long> _heartbeats = new();
    private readonly Dictionary<string, SessionRecord> _sessions = new();
    private readonly HashSet<string> _ended = new();

    public bool TryAppendSpan(ObservedSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);
        lock (_gate)
        {
            if (!_spanIdsBySession.TryGetValue(span.SessionId, out var ids))
            {
                ids = new HashSet<string>(StringComparer.Ordinal);
                _spanIdsBySession[span.SessionId] = ids;
            }

            // HashSet.Add returns false when the id is already present: idempotent dedup.
            return ids.Add(span.SpanId);
        }
    }

    public int SpanCount(string sessionId)
    {
        lock (_gate)
        {
            return _spanIdsBySession.TryGetValue(sessionId, out var ids) ? ids.Count : 0;
        }
    }

    public void UpsertHeartbeat(string sessionId, long monotonicTicks)
    {
        lock (_gate)
        {
            _heartbeats[sessionId] = monotonicTicks;
        }
    }

    public long? LastHeartbeat(string sessionId)
    {
        lock (_gate)
        {
            return _heartbeats.TryGetValue(sessionId, out var ticks) ? ticks : null;
        }
    }

    public void RecordSession(SessionRecord session)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (_gate)
        {
            _sessions[session.SessionId] = session;
        }
    }

    public SessionRecord? FindSession(string sessionId)
    {
        lock (_gate)
        {
            return _sessions.TryGetValue(sessionId, out var record) ? record : null;
        }
    }

    public void MarkEnded(string sessionId)
    {
        lock (_gate)
        {
            _ended.Add(sessionId);
        }
    }

    public void ClearEnded(string sessionId)
    {
        lock (_gate)
        {
            _ended.Remove(sessionId);
        }
    }

    public bool IsEnded(string sessionId)
    {
        lock (_gate)
        {
            return _ended.Contains(sessionId);
        }
    }
}
