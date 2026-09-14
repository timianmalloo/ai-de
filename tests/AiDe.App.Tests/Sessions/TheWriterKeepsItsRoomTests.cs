using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Sessions;
using Microsoft.Web.WebView2.Wpf;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Sessions;

/// <summary>
/// <b>Ruling 80 — the editor's rest height is derived from the thread's need.</b> The operator's
/// words: <i>"should be a larger window size so that the default isn't scrolling"</i>; their
/// screenshot: a 130 px editor with a scrollbar under an empty thread that took ~70 % of the
/// document. With <b>0 turns</b> the thread is its two-line caption and the editor takes the rest
/// of the body; with <b>≥ 1 turn</b> the editor rests at <see cref="ComposerSurface.EditorRest"/>
/// and the thread takes the remainder; <see cref="ComposerSurface.EditorFloor"/> stays the floor
/// under a short window — <b>one constant</b>, read by the host and pushed to the page.
/// <c>SessionDocumentSurface.ComposerShare</c> is retired; the belt (DS-1 Q14) stays, its value
/// derived. The review's §7 CV-5.4 row names these oracles (R1–R5, L6).
/// </summary>
/// <remarks>
/// <para><b>Headless, no browser.</b> The document is measured detached from any window at the
/// Left pane's size (673 × 748 at 1440 × 900 docked Left with the Bottom collapsed — Rulings 83
/// and 88; D3's §2c), so the WebView2 has no HWND and its arranged height is what the layout gave
/// the editor's slot — the quantity the operator lost. The page's own scrollbar needs a browser:
/// its CSS contract is read here, its live state in the contrast census (real shell, real page).</para>
/// </remarks>
public sealed class TheWriterKeepsItsRoomTests(ITestOutputHelper output)
{
    /// <summary>The Left pane at 1440 × 900 with the session docked Left and the Bottom collapsed (D3 §2c: 673 wide, the document 748 tall).</summary>
    private const double LeftWidth = 673;
    private const double StartupHeight = 748;

    private sealed record Geometry(
        double Height,
        double Header,
        double ThreadRow,
        double CaptionDesired,
        double Hairline,
        double ComposerTop,
        double Composer,
        double Editor,
        double Compiled,
        double SendRowBottom);

    private static Geometry Layout(SessionDocumentSurface document, double width, double height)
    {
        document.Measure(new Size(width, height));
        document.Arrange(new Rect(0, 0, width, height));
        document.UpdateLayout();

        // The thread's Apply is a Background dispatcher operation; a headless run pumps it once.
        document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
        document.UpdateLayout();

        var composer = document.Composer;
        var header = ThreadFixtures.Visuals<DockPanel>(document).First(p => AutomationProperties.GetName(p) == "Session header");
        var caption = ThreadFixtures.Visuals<TextBlock>(document).First(t => t.Text.StartsWith("Nothing has run yet", StringComparison.Ordinal));
        var threadRow = (FrameworkElement)VisualTreeHelper.GetParent(caption);
        var composerHost = (Border)VisualTreeHelper.GetParent(composer);
        var editor = ThreadFixtures.Visuals<WebView2>(composer).Single();
        // The compiled prompt's box is a logical child whether or not its disclosure is open (a
        // collapsed Expander renders no visual for its content).
        var compiled = Logical<TextBox>(composer).Single(box => box.IsReadOnly && box.MaxHeight <= ComposerSurface.CompiledPromptMaxHeight + 1);
        var send = ThreadFixtures.Visuals<Button>(composer).Single(b => b.Content is "Send");

        return new Geometry(
            height,
            header.ActualHeight + header.Margin.Top + header.Margin.Bottom,
            threadRow.ActualHeight,
            caption.Visibility == Visibility.Visible ? caption.DesiredSize.Height : 0,
            composerHost.BorderThickness.Top,
            composer.TransformToAncestor(document).Transform(new Point(0, 0)).Y,
            composer.ActualHeight,
            editor.ActualHeight,
            compiled.IsArrangeValid ? compiled.ActualHeight : 0,
            send.TransformToAncestor(composer).Transform(new Point(0, send.ActualHeight)).Y);
    }

    private static List<TurnView> Turns(int count) =>
        count == 40 ? ThreadFixtures.Forty() : ThreadFixtures.Five().Take(count).ToList();

    /// <summary>
    /// The operator's 2026-09-14 report — <i>"I didn't see a compiled prompt"</i>: the <b>Compiled
    /// prompt</b> disclosure's header, the structure disclosure's header and the send row are on
    /// screen at 0, 1 and 40 turns — each inside the composer, the composer inside the document,
    /// nothing clipped by the editor's fill or rest. Measured, not assumed: the rows carry the
    /// header's bottom edge against the composer's height.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(40)]
    public void TheCompiledPromptDisclosure_IsOnScreen_AtEveryTurnCount(int count)
    {
        Sta.Run(() =>
        {
            using var document = ThreadFixtures.Document(Turns(count));
            var g = Layout(document, LeftWidth, StartupHeight);
            var composer = document.Composer;
            var disclosures = ThreadFixtures.Visuals<Expander>(composer).ToList();
            var compiled = disclosures.SingleOrDefault(e => AutomationProperties.GetName(e) == "Compiled prompt");
            Assert.True(compiled is not null, "the Compiled prompt disclosure is not in the composer's visual tree");
            Assert.True(compiled!.IsVisible || compiled.Visibility == Visibility.Visible, "the Compiled prompt disclosure is hidden");

            var header = ThreadFixtures.Visuals<ToggleButton>(compiled).FirstOrDefault() ?? (FrameworkElement)compiled;
            var top = header.TransformToAncestor(composer).Transform(new Point(0, 0)).Y;
            var bottom = top + header.ActualHeight;
            output.WriteLine($"{count} turns at {LeftWidth}×{StartupHeight}: composer {g.Composer:F1} · editor {g.Editor:F1} · Compiled prompt header {top:F1}–{bottom:F1} · send row bottom {g.SendRowBottom:F1}");

            Assert.True(header.ActualHeight > 0, "the Compiled prompt header has no height — it was never laid out");
            Assert.True(bottom <= g.Composer + 0.5, $"{count} turns: the Compiled prompt header ends at {bottom:F1} px inside a {g.Composer:F1} px composer — clipped");
            Assert.True(top >= g.Editor - 0.5, $"{count} turns: the Compiled prompt header ({top:F1}) sits above the editor's bottom ({g.Editor:F1}) — overlapped");
            Assert.True(g.ComposerTop + g.Composer <= g.Height + 0.5, $"the composer ends at {g.ComposerTop + g.Composer:F1} px in a {g.Height} px document");
        });
    }

    /// <summary>
    /// <b>R1.</b> 0 turns: the thread is its two-line caption and the editor takes the rest of the
    /// body — <c>editor == body − header − caption − chrome</c>; the page's scrollbar is the live
    /// fact's below. <b>Red today:</b> <c>ComposerShare = 0.45</c> belts the composer
    /// at 45 % and its DockPanel desires only the floor, so the editor sits at 130 under ~500 px of
    /// empty thread.
    /// </summary>
    [Fact]
    public void AtZeroTurns_TheEditorFillsTheBody_WithNoScrollbar()
    {
        Assert.Equal(130, ComposerSurface.EditorFloor);

        Sta.Run(() =>
        {
            using var document = ThreadFixtures.Document([]);
            var g = Layout(document, LeftWidth, StartupHeight);
            output.WriteLine($"0 turns at {LeftWidth}×{StartupHeight}: header {g.Header:F1} · caption {g.CaptionDesired:F1} (row {g.ThreadRow:F1}) · hairline {g.Hairline} · composer {g.Composer:F1} · editor {g.Editor:F1}");

            var chrome = g.Composer - g.Editor;
            var expected = g.Height - g.Header - g.CaptionDesired - g.Hairline - chrome;

            // THE CAPTION IS THE THREAD: the thread's row is exactly the caption's own height — no
            // empty band between the two lines and the editor's top edge.
            Assert.True(g.CaptionDesired > 0, "the empty caption was not laid out");
            Assert.Equal(g.CaptionDesired, g.ThreadRow, 0.5);

            // D3's formula (the review's §7 CV-5.4 R1) — the grid identity gives it once the row
            // above holds; kept as the row's stated shape, not as an independent oracle.
            Assert.Equal(expected, g.Editor, 0.5);

            // Non-vacuity: the editor is larger than its rest with turns (it FILLS), and never below its floor.
            Assert.True(g.Editor > ComposerSurface.EditorRest, $"the editor is {g.Editor:F1} px at 0 turns — it does not fill the body (rest {ComposerSurface.EditorRest}, floor {ComposerSurface.EditorFloor})");
            Assert.True(g.Editor >= ComposerSurface.EditorFloor, $"the editor is {g.Editor:F1} px; the floor is {ComposerSurface.EditorFloor}");

            // No overflow: the send row ends inside the composer, the composer inside the document.
            Assert.True(g.SendRowBottom <= g.Composer + 0.5, $"the send row's bottom is at {g.SendRowBottom:F1} px inside a {g.Composer:F1} px composer — clipped");
            Assert.True(g.ComposerTop + g.Composer <= g.Height + 0.5, $"the composer ends at {g.ComposerTop + g.Composer:F1} px in a {g.Height} px document");

            // The scrollbar the operator saw is the page's — a browser's, unobservable here: the
            // name's "no scrollbar" clause is carried by TheComposedShellsPageDoesNotScrollBeforeTheOperatorTypes
            // (the live page in the real shell). A WPF-scroller census at 0 turns would be empty by
            // construction (the feed is collapsed, the compiled prompt closed) and is not written.
        });
    }

    /// <summary>
    /// <b>R2.</b> 1 and 40 turns, at rest (the structure and the compiled prompt collapsed): the
    /// editor is <see cref="ComposerSurface.EditorRest"/> at both counts and its top edge is
    /// equal — the thread grows upward and scrolls; the composer is pinned.
    /// </summary>
    [Fact]
    public void AtOneAndFortyTurns_TheEditorRestsAt280_WithEqualTopEdge()
    {
        Assert.Equal(280, ComposerSurface.EditorRest);

        Sta.Run(() =>
        {
            var tops = new List<double>();
            foreach (var count in new[] { 1, 40 })
            {
                using var document = ThreadFixtures.Document(Turns(count));
                var g = Layout(document, LeftWidth, StartupHeight);
                output.WriteLine($"{count} turns at {LeftWidth}×{StartupHeight}: header {g.Header:F1} · thread {g.ThreadRow:F1} · composer top {g.ComposerTop:F1} · composer {g.Composer:F1} · editor {g.Editor:F1}");
                tops.Add(g.ComposerTop);

                Assert.Equal(ComposerSurface.EditorRest, g.Editor, 0.01);
                Assert.True(g.ThreadRow >= SessionDocumentSurface.ThreadMinimum - 0.5, $"{count} turns: the thread row is {g.ThreadRow:F1} px; its minimum is {SessionDocumentSurface.ThreadMinimum}");
                Assert.True(g.SendRowBottom <= g.Composer + 0.5, $"{count} turns: the send row's bottom is at {g.SendRowBottom:F1} px inside a {g.Composer:F1} px composer — clipped");
                Assert.True(g.ComposerTop + g.Composer <= g.Height + 0.5, $"{count} turns: the composer ends at {g.ComposerTop + g.Composer:F1} px in a {g.Height} px document");
            }

            Assert.Equal(tops[0], tops[1], 0.5);
        });
    }

    /// <summary>
    /// <b>R2, the transition (the Test Architect's Blocker).</b> The operator's path is not a
    /// pre-loaded document: it is an empty session, then the first Send. A document laid out at 0
    /// turns (the editor filling) must leave its fill for its rest when the first turn lands
    /// <i>without a resize</i>. No <c>Measure</c> call here after the turn: only <c>UpdateLayout</c>,
    /// which is what the running shell does. <b>Green on its first run</b> (the Test Architect's
    /// Blocker was Inferred: WPF re-measures an ancestor only when a child's desired size changes,
    /// and the thread host's does not — but the header's does: <c>RenderHeader</c> rewrites the
    /// count (<i>no turns yet</i> → <i>1 turn</i>) and shows the feed, and that propagates to the
    /// document's <c>MeasureOverride</c>, which re-derives the belt). This fact is the guard that
    /// keeps the transition on the operator's path if the header ever stops changing.
    /// </summary>
    [Fact]
    public void AfterTheFirstTurn_TheEditorLeavesItsFillForItsRest_WithoutAResize()
    {
        Sta.Run(() =>
        {
            using var document = ThreadFixtures.Document([]);
            var before = Layout(document, LeftWidth, StartupHeight);
            Assert.True(before.Editor > ComposerSurface.EditorRest, $"the editor is {before.Editor:F1} px at 0 turns — it does not fill; the transition would measure nothing");

            var turn = Turns(1)[0];
            var ordinal = document.ReadModel.Accept(turn.SourceText, turn.Decorations, turn.SentBytes, turn.At);
            foreach (var line in turn.Events)
            {
                document.ReadModel.Append(ordinal, line);
            }

            document.ReadModel.Conclude(ordinal, turn.State, turn.At + (turn.Outcome!.Duration ?? TimeSpan.Zero), turn.Outcome.ExitCode, turn.Outcome.Edits);

            // The thread's Apply and the header's render are dispatcher work; then the layout pass
            // the shell would run — and nothing else.
            document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
            document.UpdateLayout();

            var composer = document.Composer;
            var editor = ThreadFixtures.Visuals<WebView2>(composer).Single();
            var threadRow = (FrameworkElement)VisualTreeHelper.GetParent(ThreadFixtures.Visuals<TextBlock>(document).First(t => t.Text.StartsWith("Nothing has run yet", StringComparison.Ordinal)));
            output.WriteLine($"0 → 1 turn at {LeftWidth}×{StartupHeight} without a resize: editor {before.Editor:F1} → {editor.ActualHeight:F1} · thread row {before.ThreadRow:F1} → {threadRow.ActualHeight:F1}");

            Assert.Equal(ComposerSurface.EditorRest, editor.ActualHeight, 0.01);
            Assert.True(threadRow.ActualHeight > before.CaptionDesired + 1, $"the first turn renders in the caption's {threadRow.ActualHeight:F1} px strip");
        });
    }

    /// <summary>
    /// <b>R3.</b> Under a short window the editor gives way — toward its floor, never below —
    /// while the thread keeps the belt's named minimum, <see cref="SessionDocumentSurface.ThreadMinimum"/>;
    /// when even the composer's own minimum (its lines, the editor's floor, the reader's floor)
    /// does not fit under the belt, the minimum wins and the thread gets less — the send row is
    /// never clipped. All three rows open the structure and a ~30-line compiled prompt: the most
    /// chrome the composer can carry (INV-0007's reader yields first, DC-137).
    /// </summary>
    /// <remarks>
    /// The regimes, at the chrome measured (a configured composer with the structure and the
    /// compiled prompt open: 330 px of lines, picker and send row; minimum 508): 720 → the belt
    /// holds and the editor sits between its floor and its rest; 640 (D3's number) → the belt holds
    /// and the editor is exactly the floor (the reader took what was left), as does 600 (the height
    /// L1's rows read, where the thread's guaranteed room is now the half-turn — Ruling 88's
    /// narrowing, recorded); 560 → the composer's minimum exceeds the belt. A chrome change that moves a row out of its regime fails that
    /// row by name rather than passing vacuously. <b>Red observed</b> (the constant belt): at 640
    /// the thread read 95 px and at 500 it read 0 — the editor never moved off 130.
    /// </remarks>
    [Theory]
    [InlineData(720, "gives way")]
    [InlineData(640, "at the floor")]
    [InlineData(600, "at the floor")]
    [InlineData(560, "the minimum wins")]
    public void UnderAShortWindow_TheEditorGivesWayToItsFloor_NeverBelow130(double height, string regime)
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-writer-room", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                using var document = ThreadFixtures.Document(Turns(1));
                Configure(document.Composer, root);
                document.Composer.StructureOpen = true;
                document.Composer.CompiledPromptOpen = true;

                var g = Layout(document, LeftWidth, height);
                var composer = document.Composer;
                var body = height - g.Header;
                var belt = body - SessionDocumentSurface.ThreadMinimum - g.Hairline;
                output.WriteLine($"1 turn at {LeftWidth}×{height} (structure + compiled open, {regime}): header {g.Header:F1} · thread {g.ThreadRow:F1} · belt {belt:F1} · composer {g.Composer:F1} (minimum {composer.MinimumHeight:F1}) · editor {g.Editor:F1} · compiled {g.Compiled:F1}");

                // NON-VACUITY: the reader really is the ~30 lines, laid out at a real height.
                Assert.True(composer.CompiledView.Count(c => c == '\n') >= 28, "the compiled view is not the ~30 lines the operator had");
                Assert.True(g.Compiled >= ComposerSurface.CompiledPromptMinHeight - 0.5, $"the compiled box was laid out at {g.Compiled:F0} px; its floor is {ComposerSurface.CompiledPromptMinHeight}");

                Assert.True(g.Editor >= ComposerSurface.EditorFloor - 0.5, $"the editor host was starved to {g.Editor:F1} px; its floor is {ComposerSurface.EditorFloor}");
                Assert.True(g.Editor < ComposerSurface.EditorRest, $"the editor is {g.Editor:F1} px — the window did not make it give way; this row measures nothing");
                Assert.True(g.SendRowBottom <= g.Composer + 0.5, $"the send row's bottom is at {g.SendRowBottom:F1} px inside a {g.Composer:F1} px composer — clipped");
                Assert.True(g.ComposerTop + g.Composer <= g.Height + 0.5, $"the composer ends at {g.ComposerTop + g.Composer:F1} px in a {g.Height} px document");

                switch (regime)
                {
                    case "gives way":
                        Assert.True(belt >= composer.MinimumHeight, $"the belt ({belt:F1}) is under the composer's minimum ({composer.MinimumHeight:F1}) — this row is in the wrong regime");
                        Assert.True(g.Editor > ComposerSurface.EditorFloor + 0.5, $"the editor is at its floor ({g.Editor:F1}) with room to spare — this row is in the wrong regime");
                        Assert.Equal(SessionDocumentSurface.ThreadMinimum, g.ThreadRow, 0.5);
                        break;
                    case "at the floor":
                        Assert.True(belt >= composer.MinimumHeight, $"the belt ({belt:F1}) is under the composer's minimum ({composer.MinimumHeight:F1}) — this row is in the wrong regime");
                        Assert.Equal(ComposerSurface.EditorFloor, g.Editor, 0.5);
                        Assert.Equal(SessionDocumentSurface.ThreadMinimum, g.ThreadRow, 0.5);
                        break;
                    default:
                        Assert.True(belt < composer.MinimumHeight, $"the belt ({belt:F1}) holds the composer's minimum ({composer.MinimumHeight:F1}) — this row is in the wrong regime");
                        Assert.Equal(ComposerSurface.EditorFloor, g.Editor, 0.5);
                        Assert.Equal(ComposerSurface.CompiledPromptMinHeight, g.Compiled, 0.5);
                        Assert.Equal(composer.MinimumHeight, g.Composer, 0.5);
                        Assert.True(g.ThreadRow >= 0 && g.ThreadRow < SessionDocumentSurface.ThreadMinimum, $"the thread row is {g.ThreadRow:F1} px");
                        break;
                }
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    /// <summary>
    /// <b>R4.</b> One floor constant. The page's stylesheet reads <c>--editor-floor</c> with a
    /// fallback equal to <see cref="ComposerSurface.EditorFloor"/> for the frame before the push,
    /// carries no pixel floor of its own on the editor, and the host pushes the constant on
    /// <c>host.init</c> as <c>editorFloor</c> (the page applies it — the live proof is the
    /// census fact below). <b>Red today:</b> <c>composer.html</c> says <c>min-height: 110px</c>.
    /// </summary>
    [Fact]
    public void OneFloorConstant_ReadByHostAndPage()
    {
        Assert.Equal(130, ComposerSurface.EditorFloor);

        var css = StyleBlock(File.ReadAllText(Path.Combine(WebRoot(), "composer.html")));

        var reads = Regex.Matches(css, @"var\(\s*--editor-floor\s*,\s*(\d+(?:\.\d+)?)px\s*\)");
        Assert.True(reads.Count >= 1, "composer.html does not read --editor-floor; the page has a floor the host cannot set");
        Assert.All(reads, m => Assert.Equal(ComposerSurface.EditorFloor, double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)));

        // No second definition: no rule on the editor or the CodeMirror root carries a pixel
        // height or min-height of its own (DM-A).
        var editorRules = Regex.Matches(css, @"(?<sel>[^{}]*\.(?:editor|cm-editor)[^{}]*)\{(?<body>[^}]*)\}")
            .Select(m => (Selector: m.Groups["sel"].Value.Trim(), Body: m.Groups["body"].Value))
            .Where(r => Regex.IsMatch(r.Body, @"(?<![-\w])(?:min-)?height\s*:\s*[1-9]\d*(?:\.\d+)?px"))
            .Select(r => r.Selector)
            .ToList();
        Assert.True(editorRules.Count == 0, "these rules carry a pixel floor of their own beside the host's constant: " + string.Join(" · ", editorRules));

        // The page applies what the host pushes.
        var mjs = File.ReadAllText(Path.Combine(WebRoot(), "composer.mjs"));
        Assert.Contains("--editor-floor", mjs, StringComparison.Ordinal);
        Assert.Contains("editorFloor", mjs, StringComparison.Ordinal);

        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-writer-room", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var surface = new ComposerSurface("composer:s-rest", "s-rest — composer");
                Configure(surface, root);
                using var init = JsonDocument.Parse(surface.InitPayloadJson());
                Assert.Equal("host.init", init.RootElement.GetProperty("kind").GetString());
                Assert.Equal(ComposerSurface.EditorFloor, init.RootElement.GetProperty("editorFloor").GetDouble());
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    /// <summary>
    /// <b>R4, live.</b> The composed shell's composer page carries <c>--editor-floor</c> on its
    /// root's inline style with the host's value — only the push can have put it there (the
    /// stylesheet reads the property; it never declares it).
    /// </summary>
    [Fact]
    public void TheComposedShellsPageCarriesTheHostsFloor()
    {
        var census = ShellContrastCensusTests.Taken.Value;
        Assert.True(census.Failure is null, "the census was not taken: " + census.Failure);

        Assert.True(census.PageTheme.TryGetValue("--editor-floor", out var floor), "the composer page's root carries no --editor-floor; the host's push did not reach it");
        Assert.Equal($"{ComposerSurface.EditorFloor:0}px", floor);
    }

    /// <summary>
    /// <b>R1, live.</b> The composed shell's composer page, as it opens (0 turns), has no
    /// scrollbar: the document's scroll height is its client height and the message editor's
    /// scroller holds nothing to scroll — read from the real page in the real shell, beside the
    /// host's arranged height (≥ the floor). The operator's screenshot 1, re-taken by a machine.
    /// </summary>
    [Fact]
    public void TheComposedShellsPageDoesNotScrollBeforeTheOperatorTypes()
    {
        var census = ShellContrastCensusTests.Taken.Value;
        Assert.True(census.Failure is null, "the census was not taken: " + census.Failure);
        Assert.True(census.ComposerPageScroll is not null, "the census did not read the composer page's scroll state; its omissions: " + string.Join(" | ", census.Omissions.Select(o => $"{o.What}: {o.Reason}")));
        var scroll = census.ComposerPageScroll!;
        output.WriteLine(string.Join(" · ", scroll.Select(p => $"{p.Key} {p.Value:0.#}")));

        Assert.Equal(0, scroll["turns"]);
        Assert.True(scroll["hostHeight"] >= ComposerSurface.EditorFloor - 0.5, $"the editor host is {scroll["hostHeight"]:0.#} DIP; its floor is {ComposerSurface.EditorFloor}");
        Assert.True(scroll["documentClientHeight"] > 0, "the page reported no client height — it was not laid out");
        Assert.True(scroll["documentScrollHeight"] <= scroll["documentClientHeight"], $"the composer page scrolls at 0 turns: {scroll["documentScrollHeight"]:0.#} px of content in a {scroll["documentClientHeight"]:0.#} px page");
        Assert.True(scroll["editorClientHeight"] > 0, "the message editor's scroller was not found or not laid out (.field.grows .cm-scroller)");
        Assert.True(scroll["editorScrollHeight"] <= scroll["editorClientHeight"], $"the message editor scrolls at 0 turns: {scroll["editorScrollHeight"]:0.#} px in {scroll["editorClientHeight"]:0.#}");

        // The host's arranged DIPs reached the page as its CSS pixels (zoom 1.0), and the composer
        // took its belt or its minimum, whichever is larger — the derivation seen live, not only
        // headless. The census's shell (MainWindow's 1180 × 720, the session in the Center over the
        // startup terminal) leaves the document under 400 px — the floor regime; the fill regime is
        // asserted only when a taller document is on the census's path.
        Assert.Equal(scroll["hostHeight"], scroll["documentClientHeight"], 1.0);
        Assert.Equal(Math.Max(scroll["belt"], scroll["minimum"]), scroll["composerHeight"], 1.0);
        if (scroll["documentHeight"] > 600)
        {
            Assert.True(scroll["hostHeight"] > ComposerSurface.EditorRest, $"a {scroll["documentHeight"]:0.#} px document at 0 turns gave the editor {scroll["hostHeight"]:0.#} DIP — it does not fill");
        }
    }

    /// <summary>
    /// <b>Ruling 80's other half, live:</b> "a scrollbar appears only when the text exceeds the rest
    /// height" — and it is the editor's, never the page's. The census types forty lines into the
    /// real page's message editor after its first reading and reads again: the editor's scroller
    /// overflows, the document still does not. The behaviour rests on the page's flex chain
    /// (<c>#fields</c> → <c>.field.grows</c> → <c>.editor</c> → <c>.cm-editor</c>, each
    /// <c>min-height: 0</c>); deleting any link makes the page scroll instead, which this reads.
    /// </summary>
    [Fact]
    public void TheComposedShellsPage_ScrollsTheEditorNotThePage_OnceTheTextExceedsTheRest()
    {
        var census = ShellContrastCensusTests.Taken.Value;
        Assert.True(census.Failure is null, "the census was not taken: " + census.Failure);
        Assert.True(census.ComposerPageScrollAfterTyping is not null, "the census did not read the composer page after typing; its omissions: " + string.Join(" | ", census.Omissions.Select(o => $"{o.What}: {o.Reason}")));
        var typed = census.ComposerPageScrollAfterTyping!;
        output.WriteLine(string.Join(" · ", typed.Select(p => $"{p.Key} {p.Value:0.#}")));

        Assert.True(typed["editorClientHeight"] > 0, "the message editor's scroller was not found after typing");
        Assert.True(typed["editorScrollHeight"] > typed["editorClientHeight"], $"forty lines did not overflow the editor's scroller ({typed["editorScrollHeight"]:0.#} in {typed["editorClientHeight"]:0.#}) — the reading proves nothing");
        Assert.True(typed["documentScrollHeight"] <= typed["documentClientHeight"], $"the page scrolls once the text exceeds the rest: {typed["documentScrollHeight"]:0.#} px of content in a {typed["documentClientHeight"]:0.#} px page — the editor should scroll, never the page");
        Assert.Equal(census.ComposerPageScroll!["hostHeight"], typed["hostHeight"], 0.5);
    }

    /// <summary>
    /// The infinite-constraint boundary (spike Q14's contract, kept): with no belt the host declares
    /// only its floor; a belt and a rest make it the rest; an infinite constraint again returns it to
    /// the floor — the explicit <c>Height</c> is reset, never left stale. And the rest refuses a
    /// non-positive value.
    /// </summary>
    [Fact]
    public void UnderAnInfiniteConstraint_TheHostDeclaresItsFloor_AndForgetsAnEarlierRest()
    {
        Sta.Run(() =>
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-writer-room", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var surface = new ComposerSurface("composer:s-rest", "s-rest — composer");
                Configure(surface, root);
                var host = ThreadFixtures.Visuals<WebView2>(surface).Concat(Logical<WebView2>(surface)).First();

                double HostAt(double belt, double rest, double constraint)
                {
                    surface.BeltHeight = belt;
                    surface.EditorRestHeight = rest;
                    surface.Measure(new Size(LeftWidth, constraint));
                    surface.Arrange(new Rect(0, 0, LeftWidth, surface.DesiredSize.Height));
                    surface.UpdateLayout();
                    return host.ActualHeight;
                }

                Assert.Equal(ComposerSurface.EditorFloor, HostAt(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity), 0.5);
                Assert.Equal(ComposerSurface.EditorRest, HostAt(700, ComposerSurface.EditorRest, 700), 0.5);
                Assert.Equal(ComposerSurface.EditorFloor, HostAt(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity), 0.5);

                Assert.Throws<ArgumentOutOfRangeException>(() => surface.EditorRestHeight = 0);
                Assert.Throws<ArgumentOutOfRangeException>(() => surface.EditorRestHeight = double.NaN);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        });
    }

    /// <summary>
    /// <b>R5 (architecture).</b> <c>ComposerShare</c> is retired: no member, and no reference in
    /// the product's source or the tests — a constant share cannot come back under another caller.
    /// </summary>
    [Fact]
    public void ComposerShareIsRetired_NoReferenceSurvives()
    {
        Assert.Empty(typeof(SessionDocumentSurface).GetMember("ComposerShare", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));

        var repo = RepoRoot();
        var self = Path.GetFullPath(Path.Combine(repo, "tests", "AiDe.App.Tests", "Sessions", "TheWriterKeepsItsRoomTests.cs"));
        var survivors = new[] { "src", "tests" }
            .SelectMany(top => Directory.EnumerateFiles(Path.Combine(repo, top), "*.*", SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".mjs", StringComparison.Ordinal) || f.EndsWith(".html", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}vendor{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !string.Equals(Path.GetFullPath(f), self, StringComparison.OrdinalIgnoreCase))
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (File: Path.GetRelativePath(repo, f), Line: i + 1, Text: line)))
            .Where(l => l.Text.Contains("ComposerShare", StringComparison.Ordinal))
            .Select(l => $"{l.File}:{l.Line}: {l.Text.Trim()}")
            .ToList();

        Assert.True(survivors.Count == 0, "ComposerShare is retired (Ruling 80) but survives here:" + Environment.NewLine + string.Join(Environment.NewLine, survivors));
    }

    /// <summary>
    /// <b>L6 (Ruling 88, the IA lens's clear condition).</b> At the startup size with the session
    /// docked Left and the Bottom collapsed, the thread holds at least one turn at least half
    /// visible with the editor at its rest. The count is measured from the rendered tree and
    /// reported; Ruling 88 condition 3 replaces the row's numbers with the measured ones at the
    /// join, and the editor is never shrunk to pass.
    /// </summary>
    /// <remarks>
    /// <b>Measured (the finding for the Owner):</b> Ruling 88's <i>≥ 2 at 2560 × 1600</i> was the
    /// mockup's number (Inferred, the ruling says). The WPF tree with the review's fixture reads
    /// <b>1</b> at both viewports: the last turn is shown in full (b5/b40: 345–402 px) and the one
    /// above it is a 77-call turn (b4/b39: 1,994–2,034 px — one inline item per <c>tool.call</c>,
    /// Ruling 82) of which a 977 px viewport shows 633 — under half. The metric is dominated by a
    /// single tall turn, not by the editor's rest; the rows below pin the measured threshold (≥ 1)
    /// and print the per-turn heights.
    /// </remarks>
    [Theory]
    // The last column is the thread's measured viewport (the room the rest leaves), pinned so a
    // regression that halves the thread cannot hide behind "one turn is still half visible".
    [InlineData(1440, 900, LeftWidth, StartupHeight, 1, 1, 277)]
    [InlineData(1440, 900, LeftWidth, StartupHeight, 5, 1, 277)]
    [InlineData(1440, 900, LeftWidth, StartupHeight, 40, 1, 277)]
    // assume: the Left pane's width at 2560 × 1600 scales with the window (673/1440 × 2560 = 1197) —
    // confirmed by SH-4.2's measured Left extent; if false the words wrap differently and the
    // count moves by at most one turn (the document's height, D3 §2c's 1448, is the load-bearing axis).
    [InlineData(2560, 1600, 1197, 1448, 5, 1, 977)]
    [InlineData(2560, 1600, 1197, 1448, 40, 1, 977)]
    public void AtStartupSizeDockedLeftBottomCollapsed_TheThreadHoldsOneTurn_WithTheEditorAt280(int viewportWidth, int viewportHeight, double width, double height, int turns, int atLeast, double viewportAtLeast)
    {
        Sta.Run(() =>
        {
            using var document = ThreadFixtures.Document(Turns(turns));
            var g = Layout(document, width, height);
            Assert.Equal(ComposerSurface.EditorRest, g.Editor, 0.01);

            var scroller = ThreadFixtures.Visuals<ScrollViewer>(document.Thread).First();
            Assert.True(scroller.ViewportHeight >= viewportAtLeast - 1, $"the thread's viewport is {scroller.ViewportHeight:F1} px at {viewportWidth}×{viewportHeight}; the measured room is {viewportAtLeast}");
            var viewport = new Rect(0, 0, scroller.ViewportWidth, scroller.ViewportHeight);
            var halfVisible = 0;
            var realized = 0;
            var rows = new List<string>();
            for (var i = 0; i < document.Thread.Items.Count; i++)
            {
                if (document.Thread.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem container || container.ActualHeight <= 0)
                {
                    continue;
                }

                realized++;
                var bounds = container.TransformToAncestor(scroller).TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                var shown = Rect.Intersect(bounds, viewport);
                var shownHeight = shown.IsEmpty ? 0 : shown.Height;
                rows.Add($"b{i + 1} {container.ActualHeight:F0} px, {shownHeight:F0} shown");
                if (shownHeight >= container.ActualHeight / 2)
                {
                    halfVisible++;
                }
            }

            output.WriteLine($"{viewportWidth}×{viewportHeight} (document {width}×{height}), {turns} turns: thread {g.ThreadRow:F1} px (viewport {scroller.ViewportHeight:F1}) · editor {g.Editor:F1} · {halfVisible} of {realized} realized turn(s) at least half visible (threshold ≥ {atLeast}): {string.Join(" · ", rows)}");
            Assert.True(realized > 0, "no turn container was realized — the count would be vacuous");
            Assert.True(halfVisible >= atLeast, $"{halfVisible} turn(s) at least half visible at {viewportWidth}×{viewportHeight} with {turns} turns; Ruling 88's row says ≥ {atLeast} — a finding for the Owner, not a reason to shrink the editor");
        });
    }

    // ── helpers ──

    private sealed class NeverAsked : IAttachmentAffirmation
    {
        public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
    }

    /// <summary>
    /// Configures a composer on the host's side with the goal-block draft the shell probe seeds
    /// (three prose answers and a message, ~30 compiled lines) — so its init payload is what a
    /// page would receive and its compiled prompt is a real reader.
    /// </summary>
    private static void Configure(ComposerSurface surface, string root)
    {
        surface.Draft.SwitchTo(ComposerShape.GoalBlock);
        surface.Draft.SetGoalValue(GoalBlockFields.GoalKey, "Investigate why the composer accepts no typing.\nName the cause.\nStop before the fix.");
        surface.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "A red test exists.\nThe INV is written.");
        surface.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "The vendored bundle.\nThe test-log pollution.");
        surface.Draft.SetFreeFormText("Find out why typing into @src/AiDe.App/Web/composer.mjs reaches nothing.\nRead the handshake first.\nThen the router.\n");

        surface.Configure(
            new SessionConfig("s-rest", "rest", "w-1", DateTimeOffset.UnixEpoch, ["claude-code"]),
            new ComposerSendContext(
                RepositoryRoot: root,
                DataDirectory: root,
                AdapterInstallRoot: root,
                EngineId: "claude-code",
                Model: "sonnet",
                AccountLabel: "max-personal",
                TaskClass: "implement",
                ProofPackArtifacts: [],
                Providers: []),
            ComposerFields.GoalBlock(),
            new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "Anthropic (Claude Code)", "max-personal"));
    }

    /// <summary>The logical descendants of one type — independent of whether a disclosure is open.</summary>
    private static IEnumerable<T> Logical<T>(DependencyObject node) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(node))
        {
            if (child is not DependencyObject element)
            {
                continue;
            }

            if (element is T match)
            {
                yield return match;
            }

            foreach (var found in Logical<T>(element))
            {
                yield return found;
            }
        }
    }

    private static string StyleBlock(string html)
    {
        var withoutComments = Regex.Replace(html, "<!--.*?-->", "", RegexOptions.Singleline);
        var m = Regex.Match(withoutComments, "<style>(.*?)</style>", RegexOptions.Singleline);
        Assert.True(m.Success, "no <style> element outside comments");
        return Regex.Replace(m.Groups[1].Value, @"/\*.*?\*/", "", RegexOptions.Singleline);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    private static string WebRoot() => Path.Combine(RepoRoot(), "src", "AiDe.App", "Web");
}
