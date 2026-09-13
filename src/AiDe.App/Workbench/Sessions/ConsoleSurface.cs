using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>One row of the Console split: a heading per turn, or one event line under it.</summary>
public abstract record ConsoleSplitRow(int Ordinal)
{
    /// <summary><i>b5 · 15:02</i> — a <c>ListItem</c> with <c>HeadingLevel</c> 4 (SC10).</summary>
    public sealed record TurnHeading(int Ordinal, string Time) : ConsoleSplitRow(Ordinal)
    {
        public string Text => string.Create(CultureInfo.InvariantCulture, $"b{Ordinal} · {Time}");
    }

    /// <summary>One row of the turn's fold (Ruling 81: a message, a thought, or one event of any other kind), named by its text.</summary>
    public sealed record Line(int Ordinal, TurnRow Row) : ConsoleSplitRow(Ordinal)
    {
        public string Text => Row.Text;

        /// <summary><i>message</i> · <i>thought</i> (dim, by the shared ink style), else the kind verbatim (DESIGN.md SC1 as amended; the mockup's <c>crow</c>).</summary>
        public string KindWord => Row.Kind == Coalesce.MessageKind ? "message" : Row.Kind == Coalesce.ThoughtKind ? "thought" : Row.Kind;

        /// <summary>The row's status for assistive tech — <i>claude-code · message</i>: the attribution and the kind, never the count (SC9).</summary>
        public string Status => Row.Lane + " · " + KindWord;

        /// <summary><i>3 chunks</i> · <i>1 chunk</i>; empty for a kind that is one row per event — rendered from the fold, never asserted (Ruling 81 condition 3).</summary>
        public string ChunksText => Row.Chunks switch
        {
            null => string.Empty,
            1 => "1 chunk",
            var n => string.Create(CultureInfo.InvariantCulture, $"{n} chunks"),
        };

        public bool HasChunks => Row.Chunks is not null;
    }
}

/// <summary>
/// The Console split (SC1; Rulings 74 and 81): the same fold the thread renders per turn, unfolded
/// in time — a flat list of rows (a heading per turn, then <c>Coalesce(turn.Events)</c>), <b>a view
/// of the same fold</b>, never a second store (Ruling 74 condition 1 as amended: the split's rows
/// equal <c>Coalesce(turn.Events)</c> of every turn, in order, and the thread's reply side renders
/// the same output). The grain is the message, never the wire chunk — the operator's <i>"console
/// output is too fine grained"</i>; a row reads <c>hh:mm:ss · lane · message · the joined text · n chunks</c>.
/// </summary>
/// <remarks>
/// <para><b>The old merged-stream renderer is gone.</b> It rendered <c>ConsoleStreamModel</c> —
/// the run channel with no turn boundary — through a <c>StackPanel</c> rebuilt on every change
/// (the 40-turn cliff by construction). This is the second <see cref="FeedList"/> consumer: the
/// same virtualization, the same keys, the same pin. Opened from a fold's tail the caret is that
/// turn's heading; while a turn runs it follows.</para>
/// </remarks>
public sealed class ConsoleSurface : FeedList
{
    private readonly ObservableCollection<ConsoleSplitRow> _rows = [];
    private int? _at;

    public ConsoleSurface()
    {
        AutomationProperties.SetName(this, "Console: the merged stream");
        AutomationProperties.SetHelpText(this, "Every turn in time order: a heading per turn, then its lines. Page Down and Page Up move by row");
        AutomationProperties.SetItemStatus(this, "at the end");

        Resources[new DataTemplateKey(typeof(ConsoleSplitRow.TurnHeading))] = HeadingTemplate();
        Resources[new DataTemplateKey(typeof(ConsoleSplitRow.Line))] = LineTemplate();
        ItemContainerStyle = RowContainerStyle();
        ItemsSource = _rows;
    }

    /// <summary>The rows as rendered — the identity oracle's left side (M1).</summary>
    public IReadOnlyList<ConsoleSplitRow> Rows => _rows;

    /// <summary>The turn the split is at (the caret's heading), or null when following the end.</summary>
    public int? At => _at;

    /// <summary>
    /// Re-derives the rows from the thread's turns — one list, in order. Called by the document on
    /// every applied snapshot while the split is open, and once when it opens.
    /// </summary>
    /// <param name="turns">The fold.</param>
    /// <param name="at">The turn to open at (a tail button, <i>Open the log</i>), or null to keep the caret.</param>
    public void Show(IReadOnlyList<TurnView> turns, int? at = null)
    {
        ArgumentNullException.ThrowIfNull(turns);

        var pinned = IsPinnedAtEnd;

        // Positional merge, like the thread's: rows are appended, never replaced (the caret is the
        // selection, and a Replace drops it — spike Q10).
        var wanted = Derive(turns);
        for (var i = 0; i < wanted.Count; i++)
        {
            if (i < _rows.Count)
            {
                if (!_rows[i].Equals(wanted[i]))
                {
                    _rows[i] = wanted[i];
                }
            }
            else
            {
                _rows.Add(wanted[i]);
            }
        }

        while (_rows.Count > wanted.Count)
        {
            _rows.RemoveAt(_rows.Count - 1);
        }

        UpdateLayout();

        if (at is { } ordinal)
        {
            _at = ordinal;
            var index = IndexOfHeading(ordinal);
            if (index >= 0)
            {
                FocusItem(index);
            }
        }
        else if (pinned)
        {
            ScrollToEndOfFeed();
        }

        var running = turns.LastOrDefault(t => t.State is TurnState.Running or TurnState.Waiting);
        AutomationProperties.SetItemStatus(
            this,
            _at is { } here ? string.Create(CultureInfo.InvariantCulture, $"at b{here}")
            : running is not null ? string.Create(CultureInfo.InvariantCulture, $"following b{running.Ordinal}")
            : "at the end");
    }

    /// <summary>The status word the header's Console toggle announces: <i>following b5</i> · <i>at b2</i> · <i>at the end</i>.</summary>
    public string Status => AutomationProperties.GetItemStatus(this);

    /// <summary>The rows a fold yields: for each turn, its heading then <c>Coalesce(turn.Events)</c> — the identity's right side (M1, Ruling 81).</summary>
    public static IReadOnlyList<ConsoleSplitRow> Derive(IReadOnlyList<TurnView> turns)
    {
        ArgumentNullException.ThrowIfNull(turns);

        var rows = new List<ConsoleSplitRow>();
        foreach (var turn in turns)
        {
            rows.Add(new ConsoleSplitRow.TurnHeading(turn.Ordinal, turn.At.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture)));
            foreach (var row in turn.Rows)
            {
                rows.Add(new ConsoleSplitRow.Line(turn.Ordinal, row));
            }
        }

        return rows;
    }

    private int IndexOfHeading(int ordinal)
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            if (_rows[i] is ConsoleSplitRow.TurnHeading heading && heading.Ordinal == ordinal)
            {
                return i;
            }
        }

        return -1;
    }

    private static DataTemplate HeadingTemplate()
    {
        var text = new FrameworkElementFactory(typeof(ThreadText));
        text.SetBinding(TextBlock.TextProperty, new Binding(nameof(ConsoleSplitRow.TurnHeading.Text)));
        text.SetValue(TextBlock.FontSizeProperty, 12.0);
        text.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        text.SetValue(TextBlock.FontFamilyProperty, ThreadFeed.Mono);
        text.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");
        text.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 10, 0, 2));
        text.SetValue(FrameworkElement.MinHeightProperty, 24.0);
        text.SetValue(AutomationProperties.HeadingLevelProperty, AutomationHeadingLevel.Level4);
        return new DataTemplate(typeof(ConsoleSplitRow.TurnHeading)) { VisualTree = text };
    }

    /// <summary>
    /// <c>hh:mm:ss · lane · message · the joined text · n chunks</c> (DESIGN.md SC1 as amended by
    /// Ruling 81): the thread's event-line grammar (its segments, its stderr ink, its clock) plus
    /// the kind word and, docked right, the chunk count; the text fills and wraps between them.
    /// The count is a rendered detail — the row's name is its text alone.
    /// </summary>
    private static DataTemplate LineTemplate()
    {
        var row = new FrameworkElementFactory(typeof(DockPanel));
        row.SetValue(FrameworkElement.MinHeightProperty, 20.0);

        row.AppendChild(ThreadFeed.Clock("Row." + nameof(TurnRow.At)));
        row.AppendChild(ThreadFeed.Segment(ThreadFeed.Text("Row." + nameof(TurnRow.Lane), 12, "AccentBrush", mono: true)));
        row.AppendChild(ThreadFeed.Segment(ThreadFeed.Text(nameof(ConsoleSplitRow.Line.KindWord), 12, "TextMutedBrush", mono: true)));

        var chunks = ThreadFeed.Segment(ThreadFeed.Text(nameof(ConsoleSplitRow.Line.ChunksText), 12, "TextMutedBrush", mono: true), Dock.Right);
        chunks.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
        chunks.SetValue(FrameworkElement.MinHeightProperty, 20.0);
        chunks.SetBinding(UIElement.VisibilityProperty, ThreadFeed.Visible(nameof(ConsoleSplitRow.Line.HasChunks)));
        row.AppendChild(chunks);

        var message = ThreadFeed.Text(nameof(ConsoleSplitRow.Line.Text), 12, brush: null, wrap: true);
        message.SetValue(FrameworkElement.StyleProperty, ThreadFeed.StderrInk("Row." + nameof(TurnRow.Kind)));
        row.AppendChild(message);

        return new DataTemplate(typeof(ConsoleSplitRow.Line)) { VisualTree = row };
    }

    /// <summary>Named by its text; its status the lane and the kind word (a heading has none) — the thread's container pattern.</summary>
    private static Style RowContainerStyle()
    {
        var style = new Style(typeof(ListBoxItem), ContainerStyle());
        style.Setters.Add(new Setter(AutomationProperties.NameProperty, new Binding("Text")));
        style.Setters.Add(new Setter(AutomationProperties.ItemStatusProperty, new Binding(nameof(ConsoleSplitRow.Line.Status))));
        style.Setters.Add(new Setter(PaddingProperty, new Thickness(10, 0, 10, 0)));
        return style;
    }
}
