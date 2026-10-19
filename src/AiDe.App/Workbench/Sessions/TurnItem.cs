using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Store.Records;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The row the thread's panel binds — one per ordinal, updated <b>in place</b> (DS-1 P5; spike
/// Q10): a collection <c>Replace</c> keeps the container and the focus but drops the selection,
/// and the selection is the reading caret.
/// </summary>
/// <remarks>
/// <b>Every per-turn view state lives here, never on the container</b> (spike Q13): under
/// recycling, b3's expanded fold appeared on b38 when its container was reused, and b3 came back
/// collapsed. The three disclosure flags are two-way bound to the row, so a container carries no
/// state of its own.
/// </remarks>
public sealed class TurnItem : INotifyPropertyChanged
{
    /// <summary>How many event lines the fold shows (DESIGN.md: the last four); the split is the unbounded view.</summary>
    /// <remarks><c>simplify:</c> one constant; the trigger to revisit is a turn whose first four lines are not the ones an operator needs.</remarks>
    public const int FoldLines = 4;

    private readonly ObservableCollection<ConversationRow> _conversation = [];
    private IReadOnlyList<TurnRow> _eventRows = [];
    private TurnView _view;
    private bool _isFoldOpen;
    private bool _isProvenanceOpen;
    private bool _isCompiledOpen;

    public TurnItem(TurnView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        _view = view;
        MergeConversation();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The fold's projection. Setting it raises every bound facet at once.</summary>
    public TurnView View
    {
        get => _view;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_view, value))
            {
                return;
            }

            _view = value;
            MergeConversation();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    /// <summary>
    /// The positional merge of the conversation (the thread's P5, one level down): the row at each
    /// index takes the re-derived item — in place, so a status change never re-templates and focus
    /// inside a detail survives (T8) — and a new item appends. The items are append-only by
    /// construction (the stream is), so the collection never shrinks.
    /// </summary>
    private void MergeConversation()
    {
        var index = 0;
        var events = new List<TurnRow>();
        foreach (var item in _view.Items)
        {
            if (item is ConversationItem.Event evt)
            {
                events.Add(evt.Row);   // an event row is the fold's, never an item of the conversation
                continue;
            }

            if (index < _conversation.Count)
            {
                _conversation[index].Item = item;
            }
            else
            {
                _conversation.Add(new ConversationRow(item));
            }

            index++;
        }

        _eventRows = events;
    }

    public bool IsFoldOpen
    {
        get => _isFoldOpen;
        set => Set(ref _isFoldOpen, value);
    }

    public bool IsProvenanceOpen
    {
        get => _isProvenanceOpen;
        set => Set(ref _isProvenanceOpen, value);
    }

    public bool IsCompiledOpen
    {
        get => _isCompiledOpen;
        set => Set(ref _isCompiledOpen, value);
    }

    // ── the facets the template binds, each one derivation of the view (DM7) ──

    public int Ordinal => _view.Ordinal;
    public string DisplayOrdinal => _view.DisplayOrdinal;
    public string Words => _view.SourceText;
    public string Name => TurnCopy.Name(_view);
    public string DecorationLine => TurnCopy.DecorationLine(_view.Decorations);
    public string HelpText => _view.State switch
    {
        TurnState.Queued => QueuedSentence,
        TurnState.DeliveryUnknown when _attempt is not null => TurnCopy.ReasonSentence(_view) + " " + LastEventLine,
        _ => TurnCopy.ReasonSentence(_view),
    };

    /// <summary>
    /// Ruling 164: how far a DeliveryUnknown attempt's run log got — <i>#{seq} at {HH:mm:ss}</i> (the thread's local
    /// event clock), or <i>not recorded</i> when it holds no event. One const; a row with no attempt carries no line.
    /// </summary>
    public const string LastEventReceived = "Last event received: {0}.";

    private string LastEventLine => string.Format(
        CultureInfo.InvariantCulture,
        LastEventReceived,
        _lastEvent is { } last
            ? string.Create(CultureInfo.InvariantCulture, $"#{last.Seq} at {last.Ts.ToLocalTime():HH:mm:ss}")
            : NotRecorded);

    /// <summary>The last run event the notebook's fold received for this turn, read by the feed; null when none.</summary>
    public AiDe.Core.AgentPlane.RunEvent? LastEvent
    {
        get => _lastEvent;
        set
        {
            if (Equals(_lastEvent, value))
            {
                return;
            }

            _lastEvent = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HelpText)));
        }
    }

    private AiDe.Core.AgentPlane.RunEvent? _lastEvent;
    public IReadOnlyList<DecorationRow> Decorations => _view.Decorations;
    public string Time => _view.At.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);
    public TurnState State => _view.State;
    public string OutcomeWord => TurnCopy.OutcomeWord(_view);
    public string Lane => _view.Outcome?.Lane ?? (_view.Waiting is not null ? "conductor" : string.Empty);
    public string Counts => _view.Outcome is null
        ? TurnCopy.EventsText(_view.Events.Count)
        : string.Join(" · ", TurnCopy.Counts(_view));
    /// <summary>
    /// The reply side's conversation (Ruling 82): the prose, reasoning and tool items of
    /// <see cref="TurnView.Items"/>, in event order, as rows updated in place — the same
    /// collection instance across snapshots, so the panel keeps its containers.
    /// </summary>
    public IReadOnlyList<ConversationRow> Conversation => _conversation;
    public string SentBytes => _view.SentBytes;
    /// <summary>The attempt this turn ran as, read from the attempt host by the feed; null when none is recorded.</summary>
    public AttemptRow? Attempt
    {
        get => _attempt;
        set
        {
            if (Equals(_attempt, value))
            {
                return;
            }

            _attempt = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    private AttemptRow? _attempt;

    /// <summary>
    /// <c>Sent from revision {n} · request {first 7 of projection_sha} · via {route}</c> (Ruling 161): the turn's own
    /// attempt, in the provenance disclosure and as its HelpText. A field the row does not carry reads <i>not recorded</i>.
    /// </summary>
    public string BindingLine => _attempt is not { } attempt
        ? AttemptNotRecorded
        : string.Create(CultureInfo.InvariantCulture,
            $"Sent from revision {attempt.RevisionNo} · request {(attempt.ProjectionSha is { Length: > 0 } sha ? sha[..Math.Min(7, sha.Length)] : NotRecorded)} · via {(attempt.RouteFingerprint is { Length: > 0 } route ? RouteWords.For(route) : NotRecorded)}");

    private const string NotRecorded = "not recorded";
    private const string AttemptNotRecorded = "Attempt not recorded";
    public string ProvenanceName => "Provenance of " + _view.DisplayOrdinal;
    public string CompiledName => "Compiled prompt of " + _view.DisplayOrdinal;

    /// <summary><i>N events</i> — the non-conversation rows only (SC7 as amended: <c>acp.*</c>, the conductor's lines, stderr, a result with no call).</summary>
    public string FoldHeader => TurnCopy.EventsText(EventRows.Count);
    public bool IsLive => _view.State is TurnState.Preparing or TurnState.Running or TurnState.Waiting or TurnState.StopRequested;

    /// <summary>The fold's rows: every event item's row, in order — collected once per merge.</summary>
    private IReadOnlyList<TurnRow> EventRows => _eventRows;

    /// <summary>The fold's content: the last <see cref="FoldLines"/> non-conversation rows, bounded, no inner scroller.</summary>
    public IReadOnlyList<TurnRow> FoldedEvents
    {
        get
        {
            var rows = EventRows;
            return rows.Count <= FoldLines ? rows : [.. rows.Skip(rows.Count - FoldLines)];
        }
    }

    /// <summary>How many non-conversation rows the fold does not show; 0 when it shows them all.</summary>
    public int OtherEvents => Math.Max(0, EventRows.Count - FoldLines);

    public bool HasOtherEvents => OtherEvents > 0;

    /// <summary><i>the other 136, in the Console</i> — the tail button's text (the button is collapsed when the fold shows every line: <see cref="HasOtherEvents"/>).</summary>
    public string TailText => string.Create(CultureInfo.InvariantCulture, $"the other {OtherEvents:N0}, in the Console");

    /// <summary>
    /// The actions this turn offers, Deny first (SC7). A completed or past-failed turn offers none.
    /// A queued turn offers Cancel — and <i>Send now</i> first, only while it waits on the operator
    /// after a Stop or a failure (Ruling 95); editing it is cancel-and-redraft, never in place.
    /// </summary>
    public IReadOnlyList<TurnActionKind> Actions => _view.State switch
    {
        TurnState.Running => [TurnActionKind.Stop],
        TurnState.Waiting => _view.Waiting!.Actions,
        TurnState.Queued => QueuedAwaitsYou ? [TurnActionKind.SendNow, TurnActionKind.Cancel] : [TurnActionKind.Cancel],
        TurnState.Failed or TurnState.Interrupted or TurnState.Rejected or TurnState.Denied or TurnState.OverLimit when IsLast =>
            [TurnActionKind.SendAgain, TurnActionKind.OpenLog],
        TurnState.DeliveryUnknown when IsLast => [TurnActionKind.OpenLog],
        _ => [],
    };

    /// <summary>The queued turn's sentence, from the snapshot it sits in (Ruling 95) — set by the feed on every merge, like <see cref="IsLast"/>.</summary>
    public string QueuedSentence
    {
        get => _queuedSentence;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (string.Equals(_queuedSentence, value, StringComparison.Ordinal))
            {
                return;
            }

            _queuedSentence = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    private string _queuedSentence = string.Empty;

    /// <summary>Whether the queued turn waits on the operator rather than on a run (Ruling 95: <i>Send now</i>) — the snapshot's derivation, set by the feed.</summary>
    public bool QueuedAwaitsYou
    {
        get => _queuedAwaitsYou;
        set => Set(ref _queuedAwaitsYou, value);
    }

    private bool _queuedAwaitsYou;

    public bool HasActions => Actions.Count > 0;

    /// <summary>Whether this is the thread's last turn — a failed PAST turn folds like a completed one (SC7).</summary>
    public bool IsLast
    {
        get => _isLast;
        set => Set(ref _isLast, value);
    }

    private bool _isLast;

    /// <summary>A boxed reason on a failed, stopped, waiting or queued LAST turn; a past failure folds (SC7).</summary>
    public bool ShowsReasonBox => IsLast && (_view.State is TurnState.Failed or TurnState.Interrupted or TurnState.StopRequested or TurnState.DeliveryUnknown
        or TurnState.Rejected or TurnState.Denied or TurnState.OverLimit or TurnState.Waiting or TurnState.Queued);

    private void Set(ref bool field, bool value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        if (name is nameof(IsLast) or nameof(QueuedAwaitsYou))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
