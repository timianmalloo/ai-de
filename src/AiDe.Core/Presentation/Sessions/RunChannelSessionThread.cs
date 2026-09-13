namespace AiDe.Core.Presentation.Sessions;

/// <summary>
/// The session thread folded from today's run channel, in process: a turn joins when the composer's
/// send is accepted, its lines arrive from the run's sink, its outcome from the run's result.
/// </summary>
/// <remarks>
/// <para><b>CV-1's implementer of <see cref="ISessionThread"/>; CV-2's is the envelope fold.</b>
/// This type folds the two stores that exist today — the accepted send and the run channel — and
/// nothing durable: a reopened session has no history here (the envelope store, ADR-0034, is CV-2's
/// and replaces this with an envelope-backed implementer behind the same seam). What it does
/// honour is the read model's delivery contract, proven by
/// <c>TheReadModelPublishesOneSnapshotPerAppliedEventTests</c>: one raise per applied event after
/// catch-up, <see cref="ThreadSnapshot.Version"/> incremented by one, the snapshot in the event.</para>
///
/// <para><b>Safe off the UI thread.</b> A run's sink appends from the host's drain; every mutation
/// takes the gate, builds the next immutable snapshot, and raises outside the gate — a subscriber
/// that marshals to a dispatcher never holds this lock across the hand-off.</para>
/// </remarks>
public sealed class RunChannelSessionThread : ISessionThread
{
    private readonly Lock _gate = new();
    private readonly List<Turn> _turns = [];
    private bool _caughtUp;
    private long _version;
    private ThreadSnapshot _current;

    /// <param name="caughtUp">
    /// True (the default) for a live session with no history to replay: caught up from construction.
    /// False for a read model that will replay first and call <see cref="CatchUp"/> when done.
    /// </param>
    public RunChannelSessionThread(bool caughtUp = true)
    {
        _caughtUp = caughtUp;
        _current = new ThreadSnapshot([], 0, caughtUp);
    }

    /// <summary>A read model pre-loaded with history, caught up from construction — the fixture idiom (DS-1 A1's pre-loaded row).</summary>
    public static RunChannelSessionThread Preloaded(IEnumerable<TurnView> turns)
    {
        ArgumentNullException.ThrowIfNull(turns);

        var thread = new RunChannelSessionThread(caughtUp: true);
        foreach (var turn in turns.OrderBy(t => t.Ordinal))
        {
            thread._turns.Add(Turn.From(turn));
        }

        thread._current = new ThreadSnapshot(thread.Fold(), 0, true);
        return thread;
    }

    /// <inheritdoc/>
    public ThreadSnapshot Current => _current;

    /// <inheritdoc/>
    public event Action<ThreadSnapshot>? Changed;

    /// <summary>Ends the replay: the next snapshot is the folded history at <c>Version</c> 0, caught up.</summary>
    public void CatchUp()
    {
        ThreadSnapshot snapshot;
        lock (_gate)
        {
            if (_caughtUp)
            {
                return;
            }

            _caughtUp = true;
            _version = 0;
            snapshot = _current = new ThreadSnapshot(Fold(), 0, true);
        }

        Changed?.Invoke(snapshot);
    }

    /// <summary>A send the conductor accepted: the next ordinal joins, running.</summary>
    /// <returns>The turn's ordinal.</returns>
    public int Accept(
        string sourceText,
        IReadOnlyList<DecorationRow> decorations,
        string sentBytes,
        DateTimeOffset at,
        string? envelopeId = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(decorations);
        ArgumentNullException.ThrowIfNull(sentBytes);

        int ordinal;
        lock (_gate)
        {
            ordinal = _turns.Count + 1;
            _turns.Add(new Turn
            {
                Ordinal = ordinal,
                EnvelopeId = envelopeId ?? "turn-" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                SourceText = sourceText,
                Decorations = decorations,
                SentBytes = sentBytes,
                At = at,
                State = TurnState.Running,
            });
        }

        Publish();
        return ordinal;
    }

    /// <summary>One line of the turn's run. <paramref name="cost"/> is the event's measured usage, or null when the wire recorded none.</summary>
    public void Append(int ordinal, EventLine line, Spend? cost = null)
    {
        ArgumentNullException.ThrowIfNull(line);

        lock (_gate)
        {
            var turn = Find(ordinal);
            turn.Events.Add(line);
            turn.Lane ??= line.Lane;

            if (cost is not null)
            {
                turn.Spend = turn.Spend is { } so
                    ? new Spend(so.In + cost.In, so.Cached + cost.Cached, so.Out + cost.Out, so.Requests + cost.Requests)
                    : cost;
            }
        }

        Publish();
    }

    /// <summary>The turn is waiting on the operator (a permission or cap request, SC7).</summary>
    public void Wait(int ordinal, WaitingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            var turn = Find(ordinal);
            RefuseIfTerminal(turn);
            turn.State = TurnState.Waiting;
            turn.Waiting = request;
        }

        Publish();
    }

    /// <summary>The request was answered; the turn runs again.</summary>
    public void Resume(int ordinal)
    {
        lock (_gate)
        {
            var turn = Find(ordinal);
            RefuseIfTerminal(turn);
            turn.State = TurnState.Running;
            turn.Waiting = null;
        }

        Publish();
    }

    /// <summary>
    /// The run ended. <paramref name="terminal"/> is one of the four terminal states. What the run
    /// said arrived as its events (Ruling 81: the fold is <c>Coalesce(Events)</c>); a conclusion
    /// carries no text of its own — a reason is appended as a line before it, never stored beside it.
    /// </summary>
    public void Conclude(int ordinal, TurnState terminal, DateTimeOffset at, int? exitCode = null, int? edits = null)
    {
        if (!TurnView.IsTerminal(terminal))
        {
            throw new ArgumentOutOfRangeException(nameof(terminal), terminal, "a turn concludes in a terminal state");
        }

        lock (_gate)
        {
            var turn = Find(ordinal);
            RefuseIfTerminal(turn);
            turn.State = terminal;
            turn.Waiting = null;
            turn.ExitCode = exitCode;
            turn.Edits = edits;
            turn.ConcludedAt = at;
        }

        Publish();
    }

    private void Publish()
    {
        ThreadSnapshot snapshot;
        lock (_gate)
        {
            if (_caughtUp)
            {
                _version++;
            }

            snapshot = _current = new ThreadSnapshot(Fold(), _version, _caughtUp);
        }

        // Outside the gate: a subscriber marshals to a dispatcher, and a lock held across that
        // hand-off is how a render and an append deadlock against each other.
        Changed?.Invoke(snapshot);
    }

    private IReadOnlyList<TurnView> Fold() => [.. _turns.Select(t => t.View())];

    private Turn Find(int ordinal) =>
        _turns.FirstOrDefault(t => t.Ordinal == ordinal)
        ?? throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "no accepted turn has this ordinal");

    private static void RefuseIfTerminal(Turn turn)
    {
        if (TurnView.IsTerminal(turn.State))
        {
            throw new InvalidOperationException(
                $"turn b{turn.Ordinal} already concluded as {turn.State}; the stream is append-only and a terminal state never moves");
        }
    }

    /// <summary>The mutable fold of one turn — private, projected to an immutable <see cref="TurnView"/> on every publish.</summary>
    private sealed class Turn
    {
        public int Ordinal { get; init; }
        public string EnvelopeId { get; init; } = string.Empty;
        public string SourceText { get; init; } = string.Empty;
        public IReadOnlyList<DecorationRow> Decorations { get; init; } = [];
        public string SentBytes { get; init; } = string.Empty;
        public DateTimeOffset At { get; init; }
        public TurnState State { get; set; }
        public WaitingRequest? Waiting { get; set; }
        public List<EventLine> Events { get; } = [];
        public string? Lane { get; set; }
        public Spend? Spend { get; set; }
        public int? ExitCode { get; set; }
        public int? Edits { get; set; }
        public DateTimeOffset? ConcludedAt { get; set; }

        public static Turn From(TurnView view)
        {
            var turn = new Turn
            {
                Ordinal = view.Ordinal,
                EnvelopeId = view.EnvelopeId,
                SourceText = view.SourceText,
                Decorations = view.Decorations,
                SentBytes = view.SentBytes,
                At = view.At,
                State = view.State,
                Waiting = view.Waiting,
                Lane = view.Outcome?.Lane,
                Spend = view.Outcome?.Spend,
                ExitCode = view.Outcome?.ExitCode,
                Edits = view.Outcome?.Edits,
                ConcludedAt = view.Outcome?.Duration is { } d ? view.At + d : null,
            };
            turn.Events.AddRange(view.Events);
            return turn;
        }

        public TurnView View() => new(
            Ordinal,
            EnvelopeId,
            SourceText,
            Decorations,
            State,
            TurnView.IsTerminal(State)
                ? new OutcomeView(
                    Lane ?? "conductor",
                    ExitCode,
                    Edits,
                    Spend,
                    ConcludedAt is { } at ? at - At : null,
                    Events.Count)
                : null,
            State == TurnState.Waiting ? Waiting : null,
            [.. Events],
            SentBytes,
            At);
    }
}
