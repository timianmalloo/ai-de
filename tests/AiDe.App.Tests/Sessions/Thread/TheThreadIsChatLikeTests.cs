using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation;
using System.Windows.Media;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.Tests.Sessions.Thread;

/// <summary>
/// DS-1 <b>L1–L5</b> — the thread contract's numbers (<c>DESIGN.md</c> "Chat-like, measured"):
/// the editor's top edge equal at 1 / 5 / 40 turns and neither region starving (Q14's rows), forty
/// turns realizing fewer containers than turns with the record equal to the tree, the follow rule as
/// a structural pin, the bounded fold, and the render under the app theme with no recycled state.
/// </summary>
public sealed class TheThreadIsChatLikeTests
{
    private const double Width = 1440;
    private const double Height = 900 - 28 - 40;   // the shell's title strip and tab strip above the document

    private static SessionDocumentSurface Document(IEnumerable<TurnView> turns)
    {
        var model = new SessionDocumentViewModel("20260912T140000Z-thread", "payments extraction", Path.GetTempPath(), ["console"]);
        var document = new SessionDocumentSurface(model, null, new RecordingAnnouncer());
        var ordinal = 0;
        foreach (var turn in turns)
        {
            ordinal = document.ReadModel.Accept(turn.SourceText, turn.Decorations, turn.SentBytes, turn.At);
            foreach (var line in turn.Events)
            {
                document.ReadModel.Append(ordinal, line);
            }

            if (turn.Outcome is { } outcome)
            {
                document.ReadModel.Conclude(ordinal, turn.State, turn.At + (outcome.Duration ?? TimeSpan.Zero), outcome.ExitCode, outcome.Edits, turn.Reply);
            }
        }

        return document;
    }

    private static (double ComposerTop, double EditorHeight, double ThreadHeight, double CompiledHeight) Layout(SessionDocumentSurface document, double width = Width, double height = Height)
    {
        document.Measure(new Size(width, height));
        document.Arrange(new Rect(0, 0, width, height));
        document.UpdateLayout();

        // The thread's Apply is a Background dispatcher operation; a headless run pumps it once.
        document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
        document.UpdateLayout();

        var composer = document.Composer;
        var editor = ThreadFixtures.Visuals<WebView2>(document).Single();
        var compiled = ThreadFixtures.Visuals<TextBox>(document).Single(box => box.IsReadOnly && box.MaxHeight <= ComposerSurface.CompiledPromptMaxHeight + 1);
        var top = composer.TransformToAncestor(document).Transform(new Point(0, 0)).Y;
        return (top, editor.ActualHeight, document.Thread.ActualHeight, compiled.IsArrangeValid ? compiled.ActualHeight : 0);
    }

    /// <summary>
    /// <b>L1.</b> <b>Red observed</b>: with <c>ComposerSurface.EditorFloor</c> unset (no MinHeight
    /// on the host) the editor measured 0 px at every count (spike Q14); with the document's belt
    /// removed the thread row measured 0 px at 40 turns (the composer took the whole column).
    /// </summary>
    [Theory]
    [InlineData(1440)]
    [InlineData(1024)]
    public void TheEditorsTopEdgeIsEqualAt1_5_40Turns_AndNeitherRegionStarves(double width)
    {
        Sta.Run(() =>
        {
            var tops = new List<double>();
            foreach (var count in new[] { 1, 5, 40 })
            {
                var turns = count == 40 ? ThreadFixtures.Forty() : ThreadFixtures.Five().Take(count).ToList();
                using var document = Document(turns);
                document.Composer.CompiledPromptOpen = true;   // the 30-line compiled prompt row (Q14)

                var (top, editor, thread, compiled) = Layout(document, width);
                tops.Add(top);

                Assert.True(editor >= ComposerSurface.EditorFloor - 0.5, $"{count} turns at {width}: the editor host is {editor:F1} px; the floor is {ComposerSurface.EditorFloor}");
                Assert.True(compiled <= ComposerSurface.CompiledPromptMaxHeight + 0.5, $"{count} turns: the compiled prompt is {compiled:F1} px");

                // ≥ 3 turns half-visible at rest: a half-turn is a constant derived from the template's
                // line heights (words 19.5 + decoration 24 + outcome 24 + margins 20 ≈ 88 px / 2) —
                // never read back from the control.
                const double HalfTurn = 44;
                Assert.True(thread >= 3 * HalfTurn, $"{count} turns at {width}: the thread row is {thread:F1} px; three half-turns need {3 * HalfTurn}");
                Assert.True(document.Composer.ActualHeight <= Math.Floor(SessionDocumentSurface.ComposerShare * Height) + 0.5, $"{count} turns: the composer took {document.Composer.ActualHeight:F1} px; the belt is {SessionDocumentSurface.ComposerShare:P0}");
            }

            Assert.Equal(tops[0], tops[1], 0.5);
            Assert.Equal(tops[1], tops[2], 0.5);
        });
    }

    /// <summary>
    /// <b>L2.</b> <b>Red observed</b>: with <c>VirtualizingPanel.IsVirtualizing</c> false the count
    /// read 40 of 40 (spike Q15).
    /// </summary>
    [Fact]
    public void FortyTurns_RealizeFewerContainersThanTurns_TheDeltaTo400IsBounded_AndTheRecordEqualsTheTree()
    {
        Sta.Run(() =>
        {
            var lines = new List<string>();
            var previous = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = lines.Add;
            try
            {
                var realized = new Dictionary<int, int>();
                var p95 = new Dictionary<int, double>();

                foreach (var count in new[] { 40, 400 })
                {
                    var turns = count == 40
                        ? ThreadFixtures.Forty()
                        : Enumerable.Range(0, 10).SelectMany(r => ThreadFixtures.Forty().Select(t => ThreadFixtures.Turn(t.Ordinal + 40 * r, t.SourceText, t.Decorations, t.State, t.Outcome, t.Reply, t.Events))).ToList();
                    var (feed, _, _) = ThreadFixtures.Feed(turns);
                    var window = new Window { Width = Width, Height = Height, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = feed };
                    window.Show();
                    try
                    {
                        feed.ScrollIntoView(feed.Items[^1]);
                        window.UpdateLayout();
                        realized[count] = feed.RealizedContainers;

                        var samples = new List<double>();
                        for (var i = 0; i < 12; i++)
                        {
                            var watch = System.Diagnostics.Stopwatch.StartNew();
                            feed.InvalidateMeasure();
                            feed.UpdateLayout();
                            samples.Add(watch.Elapsed.TotalMilliseconds);
                        }

                        samples.Sort();
                        p95[count] = samples[(int)Math.Floor(0.95 * (samples.Count - 1))];
                    }
                    finally
                    {
                        window.Close();
                    }
                }

                Assert.True(realized[40] < 40, $"40 turns realized {realized[40]} containers — virtualization is off");
                Assert.True(realized[400] < 400, $"400 turns realized {realized[400]} containers");
                Assert.True(Math.Abs(realized[400] - realized[40]) <= 2, $"realized 40:{realized[40]} 400:{realized[400]} — the count tracks the item count, not the viewport");

                // The p95 ratio is calibrated by Q15: the wrong shape measured 12.9× on the first layout
                // and ~4,000× on re-layout; the right shape 1.2–1.7×. A ratio, never an absolute (DC-107).
                Assert.True(p95[400] / Math.Max(p95[40], 0.01) < 5, $"p95 layout 40:{p95[40]:F2} ms 400:{p95[400]:F2} ms — ratio {p95[400] / Math.Max(p95[40], 0.01):F1}");

                // The record equals the tree (E12): the last thread.layout for the 400 run says what the tree said.
                var record = lines.Where(l => l.Contains("\"evt\":\"thread.layout\"", StringComparison.Ordinal) && l.Contains("\"turns\":400", StringComparison.Ordinal)).Last();
                using var json = JsonDocument.Parse(record);
                Assert.True(json.RootElement.GetProperty("realized").GetInt32() < 400);
            }
            finally
            {
                WorkbenchDiagnostics.Sink = previous;
            }
        });
    }

    /// <summary><b>L3.</b> The follow rule is a structural pin: pinned → the last turn comes into view; mid-thread → the offset never moves.</summary>
    [Fact]
    public void AnAppendWhilePinned_BringsTheLastTurnIntoView_AndMidThread_NeverMoves()
    {
        Sta.Pump(
            create: () => ThreadFixtures.Feed(ThreadFixtures.Forty()).Feed,
            configure: window => { window.Width = Width; window.Height = 600; },
            body: async (window, feed) =>
            {
                var thread = (RunChannelSessionThread)typeof(ThreadFeed).GetField("_thread", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(feed)!;
                var scroller = ThreadFixtures.Visuals<ScrollViewer>(feed).First();

                // Pinned at the end: an append is followed — the witness is the LAST CONTAINER in
                // view, never the offset (the extent is an estimate under variable heights, Q4a).
                feed.ScrollIntoView(feed.Items[^1]);
                feed.UpdateLayout();
                Assert.True(feed.IsPinnedAtEnd);

                thread.Accept("appended while pinned", ThreadFixtures.Decorations("free-form", "T0", null, "message"), "bytes", ThreadFixtures.T0.AddHours(1));
                await PumpAsync(window);

                var last = feed.ItemContainerGenerator.ContainerFromIndex(feed.Items.Count - 1) as FrameworkElement;
                Assert.NotNull(last);
                var bottom = last!.TransformToAncestor(scroller).Transform(new Point(0, last.ActualHeight)).Y;
                Assert.True(bottom <= scroller.ViewportHeight + 1, $"the appended turn's bottom edge is {bottom:F1} px; the viewport is {scroller.ViewportHeight:F1}");

                // Mid-thread: a reader on b3 is never moved by an append.
                feed.ScrollIntoView(feed.Items[2]);
                scroller.ScrollToVerticalOffset(120);
                feed.UpdateLayout();
                Assert.False(feed.IsPinnedAtEnd);
                var before = scroller.VerticalOffset;

                thread.Accept("appended mid-thread", ThreadFixtures.Decorations("free-form", "T0", null, "message"), "bytes", ThreadFixtures.T0.AddHours(2));
                await PumpAsync(window);

                Assert.Equal(before, scroller.VerticalOffset, 0.5);
            });
    }

    /// <summary><b>L4.</b> A running turn shows its last four lines; the fold is bounded; the split virtualizes 10,000 rows.</summary>
    [Fact]
    public void ARunningTurnShowsItsLastFourLines_TheFoldIsBounded_AndTheSplitVirtualizes()
    {
        Sta.Run(() =>
        {
            var running = ThreadFixtures.Running(6, lines: 140);
            var turns = ThreadFixtures.Five().Append(running).ToList();
            var (feed, _, _) = ThreadFixtures.Feed(turns);
            var window = new Window { Width = Width, Height = Height, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = feed };
            window.Show();
            try
            {
                var row = feed.Rows[5];
                Assert.True(row.IsFoldOpen, "the running turn's lines are live, not folded");
                Assert.Equal(TurnItem.FoldLines, row.FoldedEvents.Count);
                Assert.Equal("b6 line 140: read docs/proof/pp-0141.md", row.FoldedEvents[^1].Text);
                Assert.Equal(136, row.OtherEvents);
                Assert.Equal("the other 136, in the Console", row.TailText);

                var container = ThreadFixtures.Container(feed, 5);
                var rendered = ThreadFixtures.Visuals<ThreadText>(container).Count(t => t.Text.StartsWith("b6 line", StringComparison.Ordinal));
                Assert.Equal(TurnItem.FoldLines, rendered);

                // The split over ten thousand rows realizes a viewport, not the list.
                var big = ThreadFixtures.Turn(7, "big", ThreadFixtures.Decorations("free-form", "T0", null, "message"), TurnState.Completed,
                    new OutcomeView("claude-code", null, 0, null, null, 10_000), null, ThreadFixtures.Lines(7, 10_000));
                var split = new ConsoleSurface();
                window.Content = split;
                split.Show([big]);
                window.UpdateLayout();
                Assert.Equal(10_001, split.Rows.Count);
                Assert.True(split.RealizedContainers < 200, $"the split realized {split.RealizedContainers} of 10,001 rows");
            }
            finally
            {
                window.Close();
            }
        });
    }

    /// <summary>
    /// <b>L5.</b> At rest, under the app dictionary: the feed's ground and the words' ink are the
    /// theme's (never Aero2's white and black), a completed turn shows ≤ 3 controls and no box, the
    /// focused container's ring is <c>{colors.focus}</c>, a fold header's ring lights on the header
    /// and never a second ring on the container, expanding b1's fold and scrolling to b400 leaves no
    /// other turn expanded (Q13), the measure is 96 × the advance of "0", and no Effect / Opacity
    /// sits in the document's tree.
    /// </summary>
    [Fact]
    public void AtRest_TheThreadRendersUnderTheAppTheme_TheRingsAreDrawn_AndRecyclingCarriesNoState()
    {
        ResourceDictionary theme = null!;
        Sta.Pump(
            create: () => Document(ThreadFixtures.Forty()),
            configure: window =>
            {
                // The real theme, as the app dictionary delivers it — the ContrastFloorTests idiom.
                theme = ThreadFixtures.AppTheme();
                window.Resources = theme;
                window.Background = (Brush)theme["SurfaceRaisedBrush"];
                window.Width = Width;
                window.Height = Height;
            },
            body: (window, document) =>
            {
                document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                document.UpdateLayout();
                var feed = document.Thread;

                Assert.Equal(ThemeProbe.Token(theme, "SurfaceRaisedBrush"), ((SolidColorBrush)feed.Background).Color);
                Assert.Equal(ThemeProbe.Token(theme, "TextBrush"), ((SolidColorBrush)feed.Foreground).Color);

                var container = ThreadFixtures.Container(feed, 1);   // b2, completed
                var words = ThreadFixtures.Visuals<ThreadText>(container).First(t => t.Text.StartsWith("Refactor the layout", StringComparison.Ordinal));
                Assert.Equal(ThemeProbe.Token(theme, "TextBrush"), ThemeProbe.Ink(words));
                Assert.Equal(feed.MeasureWidth, words.MaxWidth, 0.5);

                var expected = 96 * new FormattedText("0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(feed.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 13, Brushes.Black, 1.0).WidthIncludingTrailingWhitespace;
                Assert.Equal(expected, feed.MeasureWidth, 1.0);

                // ≤ 3 controls on a completed turn (provenance · compiled prompt · the fold), no reason box.
                var controls = ThreadFixtures.Visuals<System.Windows.Controls.Primitives.ButtonBase>(container).Count(b => b.IsVisible);
                Assert.True(controls <= 3, $"a completed turn shows {controls} controls");
                Assert.DoesNotContain(ThreadFixtures.Visuals<Border>(container), b => b.IsVisible && b.BorderThickness.Left == 1 && b.CornerRadius.TopLeft == 4);

                // The container's ring on focus, the fold header's ring on its focus — one ring each.
                Assert.True(container.Focus(), "the container refused focus");
                container.UpdateLayout();
                var ring = ThreadFixtures.Visuals<Border>(container).First(b => b.BorderThickness.Left == 2);
                Assert.Equal(ThemeProbe.Token(theme, "FocusBrush"), ((SolidColorBrush)ring.BorderBrush).Color);

                var header = ThreadFixtures.HeaderToggle(ThreadFixtures.Fold(container));
                Assert.True(header.Focus(), "the fold header refused focus");
                container.UpdateLayout();
                Assert.Equal(Brushes.Transparent.Color, ((SolidColorBrush)ring.BorderBrush).Color);
                var headerRing = ThreadFixtures.Visuals<Border>(header).First(b => b.BorderThickness.Left == 2);
                Assert.Equal(ThemeProbe.Token(theme, "FocusBrush"), ((SolidColorBrush)headerRing.BorderBrush).Color);

                // Recycling carries no state (Q13): expand b1's fold, End, Home — b1 comes back expanded, no other turn is.
                feed.Rows[0].IsFoldOpen = true;
                feed.FocusItem(feed.Items.Count - 1);
                feed.UpdateLayout();
                Assert.Null(feed.ItemContainerGenerator.ContainerFromIndex(0));
                Assert.All(feed.Rows.Skip(1), row => Assert.False(row.IsFoldOpen));
                feed.FocusItem(0);
                feed.UpdateLayout();
                Assert.True(feed.Rows[0].IsFoldOpen, "b1's fold state was lost by recycling");
                var fold = ThreadFixtures.Visuals<Expander>(ThreadFixtures.Container(feed, 0)).First(e => AutomationProperties.GetName(e).EndsWith("events", StringComparison.Ordinal));
                Assert.True(fold.IsExpanded, "b1's fold came back collapsed");

                // No Effect / Opacity < 1 in the thread's tree — the visuals that share a column with
                // the composer's HwndHost (the airspace rule); the composer's own WPF chrome is its
                // own, and every overlay is a Popup with its own HWND.
                foreach (var element in ThreadFixtures.Visuals<UIElement>(feed).Append(feed))
                {
                    Assert.Null(element.Effect);

                    // The theme's own button and scrollbar templates fade their content when disabled;
                    // what is asserted is what the THREAD authors, never a template it borrows.
                    var borrowed = element is FrameworkElement { TemplatedParent: ButtonBase or System.Windows.Controls.Primitives.ScrollBar };
                    Assert.True(borrowed || Math.Abs(element.Opacity - 1.0) < 0.001, $"{element.GetType().Name} at opacity {element.Opacity}");
                }

                return Task.CompletedTask;
            });
    }

    private static async Task PumpAsync(Window window)
    {
        for (var i = 0; i < 3; i++)
        {
            await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            window.UpdateLayout();
        }
    }
}
