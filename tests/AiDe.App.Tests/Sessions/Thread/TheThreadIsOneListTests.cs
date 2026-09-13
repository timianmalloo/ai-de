using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Tests.Sessions.Thread;

/// <summary>
/// DS-1 <b>M1</b>, re-pointed by Ruling 81 (CV-5.2 <b>C2</b>, <b>C4</b>): the Console split is a
/// view of the thread's turns at the message grain — its rows are, for every turn in order, the
/// heading then <c>Coalesce(turn.Events)</c>; the chunk count is a rendered detail of a message
/// row, read from the visual tree and never from the row's name.
/// </summary>
public sealed class TheThreadIsOneListTests
{
    /// <summary><b>C2</b> (Ruling 74 condition 1 as amended by Ruling 81). The split's rows equal heading + <c>Coalesce(turn.Events)</c> of every turn, in order — an identity over <c>Turns</c>, never an equation between two sources.</summary>
    [Fact]
    public void TheSplitsRows_EqualHeadingPlusCoalesceOfEveryTurn()
    {
        Sta.Run(() =>
        {
            var turns = ThreadFixtures.Five();
            var split = new ConsoleSurface();

            split.Show(turns);

            var expected = turns.SelectMany(t => new[] { $"b{t.Ordinal}" }.Concat(Coalesce.Rows(t.Events).Select(r => $"{r.Kind}|{r.Text}|{r.Chunks?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-"}"))).ToList();
            var actual = split.Rows.Select(r => r is ConsoleSplitRow.TurnHeading h ? $"b{h.Ordinal}" : Key(((ConsoleSplitRow.Line)r).Row)).ToList();
            Assert.Equal(expected, actual);
            Assert.Equal(turns.Sum(t => t.Rows.Count) + turns.Count, split.Rows.Count);

            // The fixture folds: b2's answer arrived in three chunks and is one row, so the split
            // holds fewer rows than events — the grain the operator asked for.
            Assert.True(turns.Sum(t => t.Events.Count) > turns.Sum(t => t.Rows.Count), "no turn in the fixture folds; the identity would hold at either grain");

            // Grouped by turn, in order — never interleaved by time across turns.
            var ordinals = split.Rows.Select(r => r.Ordinal).ToList();
            Assert.Equal(ordinals.Order(), ordinals);

            Assert.Equal("59,460 tokens this session · bounded by your subscription", TurnCopy.SessionSpend(turns, null));
        });

        static string Key(TurnRow row) => $"{row.Kind}|{row.Text}|{row.Chunks?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-"}";
    }

    /// <summary>
    /// The operator's own Console (D3 screenshot 2) — <i>mer</i> · <i>ges are</i> · <i>missing from the
    /// tracker and</i> — as the wire stamped it, then a tool call, then a one-chunk message. The instant
    /// carries a <b>+05:00</b> offset so that a clock rendered from the stamp's own offset
    /// (<c>16:29:56</c>) differs from the local rendering on any machine not at +05:00 — a UTC
    /// runner included (the Test Architect's condition on E12).
    /// </summary>
    private static readonly DateTimeOffset OperatorsInstant = new(2026, 9, 13, 16, 29, 56, TimeSpan.FromHours(5));

    private static EventLine[] OperatorsEvents() =>
    [
        new(OperatorsInstant, "claude-code", Coalesce.MessageKind, "mer", "run"),
        new(OperatorsInstant, "claude-code", Coalesce.MessageKind, "ges are", "run"),
        new(OperatorsInstant.AddSeconds(1), "claude-code", Coalesce.MessageKind, " missing from the tracker and", "run"),
        new(OperatorsInstant.AddSeconds(2), "claude-code", "tool.call", "read docs/tracker.md", "run"),
        new(OperatorsInstant.AddSeconds(3), "claude-code", Coalesce.MessageKind, "Three of them are yours.", "run"),
    ];

    private static TurnView OperatorsTurn(EventLine[] events) =>
        ThreadFixtures.Turn(1, "Which merges are missing?", ThreadFixtures.Decorations("free-form", "T0", null, "message"), TurnState.Completed,
            new OutcomeView("claude-code", null, 0, null, TimeSpan.FromSeconds(3), events.Length), events, OperatorsInstant);

    private static string LocalClock(DateTimeOffset at) => at.ToLocalTime().ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// <b>C4</b> (Ruling 81 condition 3). The operator's three chunks render as ONE row:
    /// <c>hh:mm:ss · claude-code · message · merges are missing from the tracker and · 3 chunks</c>.
    /// The count is read from the rendered text — <i>3 chunks</i>, and <i>1 chunk</i> on a
    /// one-chunk message — the row is named by its text alone (the count is never announced) with
    /// the lane and the kind word as its status, and a tool row renders no count.
    /// </summary>
    [Fact]
    public void TheSplitsMessageRow_RendersItsChunkCount_AndIsNamedByItsTextAlone()
    {
        Sta.Run(() =>
        {
            var events = OperatorsEvents();
            var split = new ConsoleSurface();
            var window = new Window { Width = 900, Height = 600, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = split };
            window.Show();
            try
            {
                split.Show([OperatorsTurn(events)]);
                window.UpdateLayout();

                Assert.Equal(4, split.Rows.Count);   // heading · the message · the tool call · the one-chunk message
                var message = Container(split, 1);
                var texts = ThreadFixtures.Visuals<ThreadText>(message).Select(t => t.Text).ToList();
                Assert.Contains("3 chunks", texts);
                Assert.Contains("message", texts);
                Assert.Contains("merges are missing from the tracker and", texts);
                Assert.Contains(LocalClock(OperatorsInstant), texts);
                Assert.Equal("merges are missing from the tracker and", AutomationProperties.GetName(message));
                Assert.Equal("claude-code · message", AutomationProperties.GetItemStatus(message));

                var tool = Container(split, 2);
                var toolTexts = ThreadFixtures.Visuals<ThreadText>(tool).Select(t => t.Text).ToList();
                Assert.DoesNotContain(toolTexts, t => t.EndsWith("chunk", StringComparison.Ordinal) || t.EndsWith("chunks", StringComparison.Ordinal));
                Assert.Contains("tool.call", toolTexts);
                Assert.Equal("read docs/tracker.md", AutomationProperties.GetName(tool));
                Assert.Equal("claude-code · tool.call", AutomationProperties.GetItemStatus(tool));

                Assert.Contains("1 chunk", ThreadFixtures.Visuals<ThreadText>(Container(split, 3)).Select(t => t.Text));

                // Cross-surface (E12), last because it replaces the window's content: the thread's
                // fold line for the same event renders the same clock — one conversion, the
                // operator's local time, as the heading above it. The +05:00 instant makes a
                // stamp-clock rendering ("16:29:56") differ from local on any runner not at +05:00.
                var foldLine = new ContentPresenter { Content = events[0], ContentTemplate = ThreadFeed.EventLineTemplate() };
                window.Content = foldLine;
                window.UpdateLayout();
                Assert.Contains(LocalClock(OperatorsInstant), ThreadFixtures.Visuals<ThreadText>(foldLine).Select(t => t.Text));
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// The long-content state (U9): a 41-chunk message is one row several thousand pixels wide if
    /// the row cannot wrap — and the count, "rendered from the fold", is then the one thing off
    /// screen. The message wraps inside the list's width and the count's right edge stays inside
    /// it; the thread's fold line (the same row grammar) wraps too.
    /// </summary>
    [Fact]
    public void ALongMessageRow_WrapsItsText_AndKeepsItsChunkCountInView()
    {
        Sta.Run(() =>
        {
            const int Chunks = 41;
            var sentence = "The tracker lists every merge the lane recorded and three of them are missing. ";
            var events = Enumerable.Range(0, Chunks)
                .Select(i => new EventLine(OperatorsInstant.AddMilliseconds(i), "claude-code", Coalesce.MessageKind, sentence, "run"))
                .ToArray();
            var split = new ConsoleSurface();
            var window = new Window { Width = 600, Height = 600, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = split };
            window.Show();
            try
            {
                split.Show([OperatorsTurn(events)]);
                window.UpdateLayout();

                var row = Container(split, 1);
                var message = ThreadFixtures.Visuals<ThreadText>(row).Single(t => t.Text.StartsWith("The tracker lists", StringComparison.Ordinal));
                var count = ThreadFixtures.Visuals<ThreadText>(row).Single(t => t.Text == "41 chunks");
                var oneLine = message.FontSize * message.FontFamily.LineSpacing;
                Assert.True(message.ActualHeight > 2 * oneLine, $"the message is {message.ActualHeight:F0} px tall — one line of {oneLine:F0}; it did not wrap");
                var countRight = count.TransformToAncestor(split).Transform(new Point(count.ActualWidth, 0)).X;
                Assert.True(countRight <= split.ActualWidth, $"the count's right edge is at {countRight:F0} px in a {split.ActualWidth:F0} px list");
                Assert.True(message.ActualWidth <= split.ActualWidth, $"the message is {message.ActualWidth:F0} px wide in a {split.ActualWidth:F0} px list");

                // The same class in the thread's fold line: a long event line wraps.
                var foldLine = new ContentPresenter { Content = new EventLine(OperatorsInstant, "claude-code", "tool.result", string.Concat(Enumerable.Repeat(sentence, 6)), "run"), ContentTemplate = ThreadFeed.EventLineTemplate() };
                window.Content = foldLine;
                window.UpdateLayout();
                var foldText = ThreadFixtures.Visuals<ThreadText>(foldLine).Single(t => t.Text.StartsWith("The tracker lists", StringComparison.Ordinal));
                Assert.True(foldText.ActualHeight > 2 * oneLine, $"the fold line is {foldText.ActualHeight:F0} px tall; it did not wrap");
                Assert.True(foldText.ActualWidth <= foldLine.ActualWidth, $"the fold line's text is {foldText.ActualWidth:F0} px wide in {foldLine.ActualWidth:F0} px");
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// The thread's half of Ruling 81, as Ruling 82 re-points it: the reply side renders the same
    /// <c>Coalesce</c> output as items — one prose item per message row, in order, and the tool row
    /// as a tool item between them, never as prose. Two message runs around a tool call are two
    /// prose items (a fold that rendered only the first run, or the tool row as prose, fails here).
    /// </summary>
    [Fact]
    public void TheReplySide_RendersEachMessageRowAsText_InOrder_AndNoToolText()
    {
        Sta.Run(() =>
        {
            EventLine[] events =
            [
                new(OperatorsInstant, "claude-code", Coalesce.MessageKind, "Reading ", "run"),
                new(OperatorsInstant, "claude-code", Coalesce.MessageKind, "the tracker.", "run"),
                new(OperatorsInstant.AddSeconds(1), "claude-code", "tool.call", "read docs/tracker.md", "run"),
                new(OperatorsInstant.AddSeconds(2), "claude-code", Coalesce.MessageKind, "Three merges ", "run"),
                new(OperatorsInstant.AddSeconds(2), "claude-code", Coalesce.MessageKind, "are missing.", "run"),
            ];
            var (feed, _, _) = ThreadFixtures.Feed([OperatorsTurn(events)]);
            var window = new Window { Width = 900, Height = 600, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = feed };
            window.Show();
            try
            {
                var container = ThreadFixtures.Container(feed, 0);
                var conversation = ThreadFixtures.Visuals<ItemsControl>(container).Single(c => c.ItemsSource is IReadOnlyList<ConversationRow>);
                var rows = conversation.Items.Cast<ConversationRow>().ToList();

                Assert.Equal(["Reading the tracker.", "read docs/tracker.md", "Three merges are missing."], rows.Select(r => r.IsTool ? r.Title : r.Text));
                Assert.Equal([true, false, true], rows.Select(r => r.IsProse));
                Assert.Equal(
                    ["Reading the tracker.", "Three merges are missing."],
                    ThreadFixtures.Visuals<ProseView>(conversation).Select(p => string.Concat(ThreadFixtures.Visuals<ThreadText>(p).Select(t => new System.Windows.Documents.TextRange(t.ContentStart, t.ContentEnd).Text))));
                Assert.Equal(feed.Rows[0].View.Items.Where(i => i is not ConversationItem.Event), rows.Select(r => r.Item));
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static ListBoxItem Container(ConsoleSurface split, int index)
    {
        split.ScrollIntoView(split.Items[index]);
        split.UpdateLayout();
        return (ListBoxItem)split.ItemContainerGenerator.ContainerFromIndex(index)
            ?? throw new InvalidOperationException($"the split's row at {index} is not realized");
    }
}
