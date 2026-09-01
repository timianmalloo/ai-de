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

    /// <summary>Every recorded session, for the read projection (the compute reader, slice 3).</summary>
    IReadOnlyList<SessionRecord> AllSessions();

    /// <summary>Records (upserts) a work episode's current state - a lifecycle dimension row (slice 4).</summary>
    void RecordEpisode(WorkEpisode episode);

    /// <summary>The current state of a work episode, or null if unknown.</summary>
    WorkEpisode? FindEpisode(string episodeId);

    /// <summary>A session's episodes in generation order (its sequential episode chain).</summary>
    IReadOnlyList<WorkEpisode> EpisodesForSession(string sessionId);

    /// <summary>Every recorded work episode.</summary>
    IReadOnlyList<WorkEpisode> AllEpisodes();

    /// <summary>
    /// Distinct spans for a session whose <c>RecordedAt</c> falls in <c>[from, toInclusive]</c> - the
    /// observable activity bound to a Work Episode's interval (US-6). Endpoints are inclusive.
    /// </summary>
    int SpanCountInInterval(string sessionId, DateTimeOffset from, DateTimeOffset toInclusive);

    /// <summary>Appends a board message. The envelope/order/thread are append-only (slice 6).</summary>
    void AppendBoardMessage(BoardMessage message);

    /// <summary>A repository's board messages in append (seq) order - repository-scoped (US-4).</summary>
    IReadOnlyList<BoardMessage> BoardMessages(string repositoryKey);

    /// <summary>
    /// Every board message across all repositories, in append (seq) order - the cross-repo compute
    /// reader for the Board surface (US-4). The pane groups these by repository key.
    /// </summary>
    IReadOnlyList<BoardMessage> AllBoardMessages();

    /// <summary>A board message by id, or null if unknown.</summary>
    BoardMessage? FindBoardMessage(string messageId);

    /// <summary>
    /// Policy redaction: irreversibly nulls the content payload and marks the message a tombstone,
    /// while the immutable envelope remains (spec line 210). The one allowed content mutation.
    /// </summary>
    void RedactBoardMessage(string messageId);

    /// <summary>
    /// Records (upserts) a scored episode as a materialized derived cache (DM7): a recomputation
    /// replaces the prior card. Never an append-only fact - the score is derived, not observed.
    /// </summary>
    void RecordScorecard(ScoredEpisode scored);

    /// <summary>The materialized scored episode for an id, or null if none has been computed.</summary>
    ScoredEpisode? FindScoredEpisode(string episodeId);

    /// <summary>
    /// Every materialized scored episode - the compute reader for the leaderboard and standing
    /// (US-14/US-16), consumed by <c>LeaderboardComposer</c>/<c>StandingComposer</c>.
    /// </summary>
    IReadOnlyList<ScoredEpisode> AllScoredEpisodes();

    /// <summary>
    /// Appends an operator's dispute of a scored episode (US-16 / rule 12). Append-only: raising a
    /// dispute never overwrites the Scorecard. A duplicate dispute id is ignored idempotently.
    /// </summary>
    void AppendScoreDispute(ScoreDispute dispute);

    /// <summary>An episode's disputes in raise order - the audit trail of why a score was contested.</summary>
    IReadOnlyList<ScoreDispute> DisputesForEpisode(string episodeId);

    /// <summary>Every recorded dispute - the compute reader for the derived Disputed state (spec §10).</summary>
    IReadOnlyList<ScoreDispute> AllDisputes();

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
    private readonly Dictionary<string, Dictionary<string, DateTimeOffset>> _spansBySession = new();
    private readonly Dictionary<string, long> _heartbeats = new();
    private readonly Dictionary<string, SessionRecord> _sessions = new();
    private readonly Dictionary<string, WorkEpisode> _episodes = new();
    private readonly Dictionary<string, BoardMessage> _boardMessages = new();
    private readonly Dictionary<string, ScoredEpisode> _scored = new();
    private readonly Dictionary<string, ScoreDispute> _disputes = new();
    private readonly HashSet<string> _ended = new();

    public bool TryAppendSpan(ObservedSpan span)
    {
        ArgumentNullException.ThrowIfNull(span);
        lock (_gate)
        {
            if (!_spansBySession.TryGetValue(span.SessionId, out var spans))
            {
                spans = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
                _spansBySession[span.SessionId] = spans;
            }

            // TryAdd returns false when the content-addressed id is already present: idempotent dedup.
            return spans.TryAdd(span.SpanId, span.RecordedAt);
        }
    }

    public int SpanCount(string sessionId)
    {
        lock (_gate)
        {
            return _spansBySession.TryGetValue(sessionId, out var spans) ? spans.Count : 0;
        }
    }

    public int SpanCountInInterval(string sessionId, DateTimeOffset from, DateTimeOffset toInclusive)
    {
        lock (_gate)
        {
            if (!_spansBySession.TryGetValue(sessionId, out var spans))
            {
                return 0;
            }

            var count = 0;
            foreach (var recordedAt in spans.Values)
            {
                if (recordedAt >= from && recordedAt <= toInclusive)
                {
                    count++;
                }
            }

            return count;
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

    public IReadOnlyList<SessionRecord> AllSessions()
    {
        lock (_gate)
        {
            return [.. _sessions.Values];
        }
    }

    public void RecordEpisode(WorkEpisode episode)
    {
        ArgumentNullException.ThrowIfNull(episode);
        lock (_gate)
        {
            _episodes[episode.EpisodeId] = episode;
        }
    }

    public WorkEpisode? FindEpisode(string episodeId)
    {
        lock (_gate)
        {
            return _episodes.TryGetValue(episodeId, out var episode) ? episode : null;
        }
    }

    public IReadOnlyList<WorkEpisode> EpisodesForSession(string sessionId)
    {
        lock (_gate)
        {
            return [.. _episodes.Values
                .Where(e => string.Equals(e.SessionId, sessionId, StringComparison.Ordinal))
                .OrderBy(e => e.Generation.Value)];
        }
    }

    public IReadOnlyList<WorkEpisode> AllEpisodes()
    {
        lock (_gate)
        {
            return [.. _episodes.Values];
        }
    }

    public void AppendBoardMessage(BoardMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_gate)
        {
            _boardMessages[message.MessageId] = message;
        }
    }

    public IReadOnlyList<BoardMessage> BoardMessages(string repositoryKey)
    {
        lock (_gate)
        {
            return [.. _boardMessages.Values
                .Where(m => string.Equals(m.RepositoryKey, repositoryKey, StringComparison.Ordinal))
                .OrderBy(m => m.Seq)];
        }
    }

    public IReadOnlyList<BoardMessage> AllBoardMessages()
    {
        lock (_gate)
        {
            return [.. _boardMessages.Values.OrderBy(m => m.RepositoryKey, StringComparer.Ordinal).ThenBy(m => m.Seq)];
        }
    }

    public BoardMessage? FindBoardMessage(string messageId)
    {
        lock (_gate)
        {
            return _boardMessages.TryGetValue(messageId, out var message) ? message : null;
        }
    }

    public void RedactBoardMessage(string messageId)
    {
        lock (_gate)
        {
            // The one allowed content mutation: null the payload, keep the envelope as a tombstone.
            if (_boardMessages.TryGetValue(messageId, out var message))
            {
                _boardMessages[messageId] = message with { Content = null, Tombstoned = true };
            }
        }
    }

    public void MarkEnded(string sessionId)
    {
        lock (_gate)
        {
            _ended.Add(sessionId);
        }
    }

    public void RecordScorecard(ScoredEpisode scored)
    {
        ArgumentNullException.ThrowIfNull(scored);
        lock (_gate)
        {
            // Upsert: a recomputation replaces the prior card (a cache refresh, DM7). Records are
            // immutable so the whole ScoredEpisode is stored by value - no stale child state possible.
            _scored[scored.EpisodeId] = scored;
        }
    }

    public ScoredEpisode? FindScoredEpisode(string episodeId)
    {
        lock (_gate)
        {
            return _scored.TryGetValue(episodeId, out var scored) ? scored : null;
        }
    }

    public IReadOnlyList<ScoredEpisode> AllScoredEpisodes()
    {
        lock (_gate)
        {
            return [.. _scored.Values];
        }
    }

    public void AppendScoreDispute(ScoreDispute dispute)
    {
        ArgumentNullException.ThrowIfNull(dispute);
        lock (_gate)
        {
            // TryAdd: a redelivered dispute id is ignored idempotently; existing disputes are never
            // mutated (append-only, rule 12).
            _disputes.TryAdd(dispute.DisputeId, dispute);
        }
    }

    public IReadOnlyList<ScoreDispute> DisputesForEpisode(string episodeId)
    {
        lock (_gate)
        {
            return [.. _disputes.Values
                .Where(d => string.Equals(d.EpisodeId, episodeId, StringComparison.Ordinal))
                .OrderBy(d => d.RaisedAt)];
        }
    }

    public IReadOnlyList<ScoreDispute> AllDisputes()
    {
        lock (_gate)
        {
            return [.. _disputes.Values.OrderBy(d => d.RaisedAt)];
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
