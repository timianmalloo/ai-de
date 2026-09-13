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

    private static SessionDocumentSurface Document(IEnumerable<TurnView> turns) => ThreadFixtures.Document(turns);

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
    /// removed the thread row measured 0 px at 40 turns (the composer took the whole column); at
    /// 600 px with the belt passed straight through as the content's constraint (the same clamp a
    /// <c>MaxHeight</c> is — a DesiredSize is clipped to what it was measured against): <i>1 turns
    /// at 600 (structure open): the editor ends at 130.0 px but the lines begin at 24.1 px — they
    /// overlap (composer 270.0 px arranged, 270.0 desired, minimum 414.0, belt 270)</i>; and with
    /// the old <c>MaxHeight</c> clamp back by mutation: <i>the composer ends at 711.9 px in a 600
    /// px document</i>. The belt now yields to the composer's minimum. <b>Ruling 80</b> re-pointed
    /// the belt's value from a constant share to the thread's need (the writer-room oracles in
    /// <c>Sessions/TheWriterKeepsItsRoomTests</c> prove the value); this fact keeps the mechanism's
    /// invariants — the editor's equal top edge, its floor and its rest as a ceiling with turns,
    /// the thread's named minimum (Ruling 88 moved the density thresholds to per-viewport measured
    /// rows: L6), the composer inside the belt or its minimum, nothing clipped or overlapped.
    /// </summary>
    [Theory]
    [InlineData(1440, 900 - 28 - 40, false)]
    [InlineData(1024, 900 - 28 - 40, false)]
    [InlineData(1440, 600, false)]
    [InlineData(1440, 600, true)]
    [InlineData(800, 600, true)]
    public void TheEditorsTopEdgeIsEqualAt1_5_40Turns_AndNeitherRegionStarves(double width, double height, bool structureOpen)
    {
        Sta.Run(() =>
        {
            var tops = new List<double>();
            foreach (var count in new[] { 1, 5, 40 })
            {
                var turns = count == 40 ? ThreadFixtures.Forty() : ThreadFixtures.Five().Take(count).ToList();
                using var document = Document(turns);
                document.Composer.CompiledPromptOpen = true;   // the 30-line compiled prompt row (Q14)
                document.Composer.StructureOpen = structureOpen;

                var (top, editor, thread, compiled) = Layout(document, width, height);
                tops.Add(top);

                Assert.True(editor >= ComposerSurface.EditorFloor - 0.5, $"{count} turns at {width}×{height}: the editor host is {editor:F1} px; the floor is {ComposerSurface.EditorFloor}");
                Assert.True(editor <= ComposerSurface.EditorRest + 0.5, $"{count} turns at {width}×{height}: the editor host is {editor:F1} px; with turns it rests at {ComposerSurface.EditorRest} and scrolls past it (Ruling 80)");
                Assert.True(compiled <= ComposerSurface.CompiledPromptMaxHeight + 0.5, $"{count} turns: the compiled prompt is {compiled:F1} px");

                // The thread keeps the belt's named minimum whenever the composer's own minimum
                // leaves it (Ruling 80); how many turns that shows at a viewport is L6's measured row.
                Assert.True(thread >= SessionDocumentSurface.ThreadMinimum - 0.5, $"{count} turns at {width}×{height} (structure {(structureOpen ? "open" : "collapsed")}): the thread row is {thread:F1} px; its minimum is {SessionDocumentSurface.ThreadMinimum}");

                // The belt (its value derived by the document), or the composer's own minimum when
                // the belt is smaller — never a clip and never an overlap: the send row ends inside
                // the composer, the composer inside the document, and the editor's bottom edge is
                // above the first line beneath it.
                var composer = document.Composer;
                var belt = composer.BeltHeight;
                Assert.True(composer.ActualHeight <= Math.Max(belt, composer.MinimumHeight) + 0.5, $"{count} turns at {height}: the composer took {composer.ActualHeight:F1} px; the belt is {belt}, its minimum {composer.MinimumHeight:F1}");
                var send = ThreadFixtures.Visuals<Button>(composer).Single(b => b.Content is "Send");
                var sendBottom = send.TransformToAncestor(composer).Transform(new Point(0, send.ActualHeight)).Y;
                Assert.True(sendBottom <= composer.ActualHeight + 0.5, $"{count} turns at {height}: the send row's bottom is at {sendBottom:F1} px inside a {composer.ActualHeight:F1} px composer — clipped");
                Assert.True(composer.ActualHeight + top <= height + 0.5, $"{count} turns at {height}: the composer ends at {composer.ActualHeight + top:F1} px in a {height} px document");
                var host = ThreadFixtures.Visuals<WebView2>(composer).Single();
                var editorBottom = host.TransformToAncestor(composer).Transform(new Point(0, host.ActualHeight)).Y;
                var lines = ThreadFixtures.Visuals<WrapPanel>(composer).Single(p => AutomationProperties.GetName(p) == "This turn");
                var linesTop = lines.TransformToAncestor(composer).Transform(new Point(0, 0)).Y;
                var structure = ThreadFixtures.Visuals<Expander>(composer).Single(e => AutomationProperties.GetName(e).StartsWith("Goal,", StringComparison.Ordinal));
                var structureTop = structure.TransformToAncestor(composer).Transform(new Point(0, 0)).Y;
                Assert.True(editorBottom <= Math.Min(linesTop, structureTop) + 0.5, $"{count} turns at {height} (structure {(structureOpen ? "open" : "collapsed")}): the editor ends at {editorBottom:F1} px but the lines begin at {Math.Min(linesTop, structureTop):F1} px — they overlap (composer {composer.ActualHeight:F1} px arranged, {composer.DesiredSize.Height:F1} desired, minimum {composer.MinimumHeight:F1}, belt {belt}, thread {thread:F1})");
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
                var realizedAfterApply = new Dictionary<int, int>();
                var p95 = new Dictionary<int, double>();

                foreach (var count in new[] { 40, 400 })
                {
                    var turns = count == 40
                        ? ThreadFixtures.Forty()
                        : Enumerable.Range(0, 10).SelectMany(r => ThreadFixtures.Forty().Select(t => ThreadFixtures.Turn(t.Ordinal + 40 * r, t.SourceText, t.Decorations, t.State, t.Outcome, t.Events))).ToList();
                    var (feed, thread, _) = ThreadFixtures.Feed(turns);
                    var window = new Window { Width = Width, Height = Height, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = feed };
                    window.Show();
                    try
                    {
                        feed.ScrollIntoView(feed.Items[^1]);
                        window.UpdateLayout();
                        realized[count] = feed.RealizedContainers;

                        // One applied event after the scroll: the record it writes and the tree it
                        // leaves are read at the same instant (E12's falsifier: record ≠ tree).
                        thread.Append(turns[^1].Ordinal, ThreadFixtures.Line(1, "claude-code", "late"));
                        window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
                        window.UpdateLayout();
                        realizedAfterApply[count] = feed.RealizedContainers;

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
                // and ~4,000× on re-layout; the right shape 1.2–1.7×. A ratio, never an absolute
                // (DC-107) — and the bound sits in the calibrated gap (< 50, floor 0.1 ms) so a loaded
                // machine's 1.2–1.7× never trips it while the wrong shape's thousands always do
                // (the Test Architect's ruling on this row).
                // perf-budget: the 50 bounds a RATIO of two same-process measurements (p95 at 400 turns
                // over p95 at 40), never a wall-clock number — a slower runner scales both sides; the
                // right shape reads 1.2–1.7× on this workstation (Q15), the wrong one ~4,000×.
                Assert.True(p95[400] / Math.Max(p95[40], 0.1) < 50, $"p95 layout 40:{p95[40]:F2} ms 400:{p95[400]:F2} ms — ratio {p95[400] / Math.Max(p95[40], 0.1):F1}");

                // The record equals the tree (E12): the thread.layout the apply wrote says exactly what
                // the tree held when it was written — an equation, not a bound.
                var record = lines.Where(l => l.Contains("\"evt\":\"thread.layout\"", StringComparison.Ordinal) && l.Contains("\"turns\":400", StringComparison.Ordinal)).Last();
                using var json = JsonDocument.Parse(record);
                Assert.Equal(realizedAfterApply[400], json.RootElement.GetProperty("realized").GetInt32());
                Assert.True(realizedAfterApply[400] < 400, $"after the apply, 400 turns realized {realizedAfterApply[400]} containers");
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

    /// <summary><b>L4.</b> A running turn renders its tool items live and its fold shows the last four non-conversation lines (Ruling 82); the fold is bounded; the split virtualizes 10,000 rows.</summary>
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
                // 140 working lines: the 28 tool.call lines (every fifth, line 140 among them) are
                // items; the 112 tool.result lines with no call fold, the last four shown.
                Assert.Equal(TurnItem.FoldLines, row.FoldedEvents.Count);
                Assert.Equal("b6 line 139: read docs/proof/pp-0141.md", row.FoldedEvents[^1].Text);
                Assert.Equal(108, row.OtherEvents);
                Assert.Equal("the other 108, in the Console", row.TailText);
                Assert.Equal(28, row.Conversation.Count);
                Assert.All(row.Conversation, r => Assert.Equal("running", r.StatusWord));

                var container = ThreadFixtures.Container(feed, 5);
                var rendered = ThreadFixtures.Visuals<ThreadText>(container).Count(t => t.Text.StartsWith("b6 line", StringComparison.Ordinal));
                Assert.Equal(TurnItem.FoldLines + 28, rendered);

                // The split over ten thousand rows realizes a viewport, not the list.
                var big = ThreadFixtures.Turn(7, "big", ThreadFixtures.Decorations("free-form", "T0", null, "message"), TurnState.Completed,
                    new OutcomeView("claude-code", null, 0, null, null, 10_000), ThreadFixtures.Lines(7, 10_000));
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

                // THE OUTCOME WORD'S INK IS THE STATE'S (the WPF lens's finding: a Foreground the
                // template set outranked every style trigger, so every outcome word painted in
                // TextBrush). Red observed by mutation (the Foreground back on the template):
                // `Expected: #FF5FB98F Actual: #FFE4E9EF` — VerifiedBrush expected, TextBrush painted.
                var completedWord = ThreadFixtures.Visuals<ThreadText>(container).Single(t => t.Text == "completed");
                Assert.Equal(ThemeProbe.Token(theme, "VerifiedBrush"), ThemeProbe.Ink(completedWord));

                feed.FocusItem(16);   // b17, failed
                feed.Rows[16].IsFoldOpen = true;
                feed.UpdateLayout();
                var failed = ThreadFixtures.Container(feed, 16);
                var failedWord = ThreadFixtures.Visuals<ThreadText>(failed).Single(t => t.Text == "lane exited 1");
                Assert.Equal(ThemeProbe.Token(theme, "DangerBrush"), ThemeProbe.Ink(failedWord));
                var stderr = ThreadFixtures.Visuals<ThreadText>(failed).Single(t => t.Text.StartsWith("b17 line 12: exit 1", StringComparison.Ordinal));
                Assert.Equal(ThemeProbe.Token(theme, "DangerBrush"), ThemeProbe.Ink(stderr));
                var stdout = ThreadFixtures.Visuals<ThreadText>(failed).First(t => t.Text.StartsWith("b17 line 11", StringComparison.Ordinal));
                Assert.Equal(ThemeProbe.Token(theme, "TextBrush"), ThemeProbe.Ink(stdout));
                feed.Rows[16].IsFoldOpen = false;
                feed.FocusItem(1);
                feed.UpdateLayout();
                container = ThreadFixtures.Container(feed, 1);

                var expected = 96 * new FormattedText("0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(feed.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), 13, Brushes.Black, 1.0).WidthIncludingTrailingWhitespace;
                Assert.Equal(expected, feed.MeasureWidth, 1.0);

                // ≤ 3 controls on a completed turn beside its items (provenance · compiled prompt · the
                // fold) plus exactly one disclosure per reasoning or tool item (Ruling 82), no reason box.
                var controls = ThreadFixtures.Visuals<System.Windows.Controls.Primitives.ButtonBase>(container).Count(b => b.IsVisible);
                var items = feed.Rows[1].Conversation.Count(r => r.IsTool || r.IsReasoning);
                Assert.True(items > 0, "the positive control: b2 has tool items");
                Assert.Equal(3 + items, controls);
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
