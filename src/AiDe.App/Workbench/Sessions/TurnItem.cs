using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using AiDe.Core.Presentation.Sessions;

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
        foreach (var item in _view.Items)
        {
            if (item is ConversationItem.Event)
            {
                continue;   // an event row is the fold's, never an item of the conversation
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
    public string HelpText => TurnCopy.ReasonSentence(_view);
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
    public string ProvenanceName => "Provenance of " + _view.DisplayOrdinal;
    public string CompiledName => "Compiled prompt of " + _view.DisplayOrdinal;

    /// <summary><i>N events</i> — the non-conversation rows only (SC7 as amended: <c>acp.*</c>, the conductor's lines, stderr, a result with no call).</summary>
    public string FoldHeader => TurnCopy.EventsText(EventRows.Count);
    public bool IsLive => _view.State is TurnState.Running or TurnState.Waiting;

    /// <summary>The fold's rows: every event item's row, in order.</summary>
    private IReadOnlyList<TurnRow> EventRows => [.. _view.Items.OfType<ConversationItem.Event>().Select(e => e.Row)];

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

    /// <summary>The actions this turn offers, Deny first (SC7). A completed or past-failed turn offers none.</summary>
    public IReadOnlyList<TurnActionKind> Actions => _view.State switch
    {
        TurnState.Running => [TurnActionKind.Stop],
        TurnState.Waiting => _view.Waiting!.Actions,
        TurnState.Failed or TurnState.Stopped when IsLast => [TurnActionKind.SendAgain, TurnActionKind.OpenLog],
        _ => [],
    };

    public bool HasActions => Actions.Count > 0;

    /// <summary>Whether this is the thread's last turn — a failed PAST turn folds like a completed one (SC7).</summary>
    public bool IsLast
    {
        get => _isLast;
        set => Set(ref _isLast, value);
    }

    private bool _isLast;

    /// <summary>A boxed reason on a failed, stopped or waiting LAST turn; a past failure folds (SC7).</summary>
    public bool ShowsReasonBox => IsLast && (_view.State is TurnState.Failed or TurnState.Stopped or TurnState.Waiting);

    private void Set(ref bool field, bool value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        if (name == nameof(IsLast))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
