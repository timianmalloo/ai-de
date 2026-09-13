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
    /// <b>C4</b> (Ruling 81 condition 3). The operator's own Console — <i>mer</i> · <i>ges are</i> ·
    /// <i>missing from the tracker and</i> at 16:29:56 — renders as ONE row: <c>16:29:56 · claude-code
    /// · message · merges are missing from the tracker and · 3 chunks</c>. The count is read from
    /// the rendered text, the row is named by its text alone (the count is never announced), and a
    /// tool row renders no count.
    /// </summary>
    [Fact]
    public void TheSplitsMessageRow_RendersItsChunkCount_AndIsNamedByItsTextAlone()
    {
        Sta.Run(() =>
        {
            var at = new DateTimeOffset(2026, 9, 13, 16, 29, 56, TimeSpan.Zero);
            EventLine[] events =
            [
                new(at, "claude-code", Coalesce.MessageKind, "mer", "run"),
                new(at, "claude-code", Coalesce.MessageKind, "ges are", "run"),
                new(at.AddSeconds(1), "claude-code", Coalesce.MessageKind, " missing from the tracker and", "run"),
                new(at.AddSeconds(2), "claude-code", "tool.call", "read docs/tracker.md", "run"),
            ];
            var turn = ThreadFixtures.Turn(1, "Which merges are missing?", ThreadFixtures.Decorations("free-form", "T0", null, "message"), TurnState.Completed,
                new OutcomeView("claude-code", null, 0, null, TimeSpan.FromSeconds(3), 4), events, at);
            var split = new ConsoleSurface();
            var window = new Window { Width = 900, Height = 600, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = split };
            window.Show();
            try
            {
                split.Show([turn]);
                window.UpdateLayout();

                Assert.Equal(3, split.Rows.Count);   // heading · the message · the tool call
                var message = Container(split, 1);
                var texts = ThreadFixtures.Visuals<ThreadText>(message).Select(t => t.Text).ToList();
                Assert.Contains("3 chunks", texts);
                Assert.Contains("message", texts);
                Assert.Contains("merges are missing from the tracker and", texts);
                var localClock = at.ToLocalTime().ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                Assert.Contains(localClock, texts);
                Assert.Equal("merges are missing from the tracker and", AutomationProperties.GetName(message));

                var tool = Container(split, 2);
                var toolTexts = ThreadFixtures.Visuals<ThreadText>(tool).Select(t => t.Text).ToList();
                Assert.DoesNotContain(toolTexts, t => t.EndsWith("chunk", StringComparison.Ordinal) || t.EndsWith("chunks", StringComparison.Ordinal));
                Assert.Contains("tool.call", toolTexts);
                Assert.Equal("read docs/tracker.md", AutomationProperties.GetName(tool));

                // Cross-surface (E12), last because it replaces the window's content: the thread's
                // fold line for the same event renders the same clock — one conversion, the
                // operator's local time, as the heading above it.
                var foldLine = new ContentPresenter { Content = events[0], ContentTemplate = ThreadFeed.EventLineTemplate() };
                window.Content = foldLine;
                window.UpdateLayout();
                Assert.Contains(localClock, ThreadFixtures.Visuals<ThreadText>(foldLine).Select(t => t.Text));
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
