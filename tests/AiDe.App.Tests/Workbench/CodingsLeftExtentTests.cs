using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Tests.Sessions.Thread;
using AiDe.App.Workbench.Composer;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation.Sessions;
using AiDe.Core.Workbench;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Shell;

/// <summary>
/// Ruling 83 condition 1 and Ruling 88 condition 3, <b>measured on the composed tree</b> (SH-4.2,
/// L5/L6; P-12's successor): with a session docked at Left, the Bottom collapsed and the window at
/// the startup size, the Left zone holds the thread's 96ch measure — the words' column is at least
/// <see cref="ThreadFeed.MeasureWidth"/> plus its 40 px gutter and the body's padding, unclipped —
/// and the thread shows at least one turn at least half visible with the editor at its 280 px rest.
/// The 1.3 extent was Inferred from the mockup until these rows were green; the numbers each row
/// prints are the record (CV-5.4's L6 assumed 673 × 748 and 1197 at 2560 — these read the tree).
/// </summary>
public sealed class CodingsLeftExtentTests(ITestOutputHelper output) : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private sealed record Measure(double Body, double Left, double Document, double WordsColumn, double MeasureWidth, double Words, double LongestWord);

    /// <summary>The words of the long-prose fixture, one sentence repeated so the line runs far past 96 characters.</summary>
    private const string LongWords = "measure the words column against the ninety-six character line and its gutter at the startup size";

    /// <summary>
    /// The window's startup size, read from <c>MainWindow.xaml</c> — the size the product opens at
    /// (the Test Architect's finding on SH-4.2: the markup said 1180 × 720 while the design's contract
    /// is measured "at the shell's startup size, 1440 × 900"; the markup now says what the design
    /// says, and this row measures at whatever it says).
    /// </summary>
    private static (double Width, double Height) StartupSize()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        while (here is not null && !File.Exists(Path.Combine(here.FullName, "AiDe.sln")))
        {
            here = here.Parent;
        }

        Assert.NotNull(here);
        var xaml = File.ReadAllText(Path.Combine(here!.FullName, "src", "AiDe.App", "MainWindow.xaml"));
        var width = System.Text.RegularExpressions.Regex.Match(xaml, "\\bWidth=\"(\\d+)\"").Groups[1].Value;
        var height = System.Text.RegularExpressions.Regex.Match(xaml, "\\bHeight=\"(\\d+)\"").Groups[1].Value;
        Assert.True(width.Length > 0 && height.Length > 0, "MainWindow.xaml declares no Width/Height — this test is reading a document whose shape it does not understand (DC-016)");
        return (double.Parse(width, CultureInfo.InvariantCulture), double.Parse(height, CultureInfo.InvariantCulture));
    }

    /// <summary>Rows the measuring display can give: the startup size always; the operator's 2560 × 1600 when the display is at least that (a clamped window would measure the display, not the operator's size).</summary>
    public static IEnumerable<object[]> Viewports()
    {
        var (width, height) = StartupSize();
        yield return [(int)width, (int)height];
        if (SystemParameters.VirtualScreenWidth >= 2560 && SystemParameters.VirtualScreenHeight >= 1600)
        {
            yield return [2560, 1600];
        }
    }

    /// <summary>The density rows: the startup size with both fixtures; the operator's size when reachable.</summary>
    public static IEnumerable<object[]> DensityRows()
    {
        foreach (var viewport in Viewports())
        {
            yield return [viewport[0], viewport[1], "five", 1];
            yield return [viewport[0], viewport[1], "prose", 1];
        }
    }

    private static void Feed(SessionDocumentSurface document, IEnumerable<TurnView> turns)
    {
        foreach (var turn in turns)
        {
            var ordinal = document.ReadModel.Accept(turn.SourceText, turn.Decorations, turn.SentBytes, turn.At);
            foreach (var line in turn.Events)
            {
                document.ReadModel.Append(ordinal, line);
            }

            if (turn.Outcome is { } outcome)
            {
                document.ReadModel.Conclude(ordinal, turn.State, turn.At + (outcome.Duration ?? TimeSpan.Zero), outcome.ExitCode, outcome.Edits);
            }
        }
    }

    /// <summary>A turn whose words run well past 96 characters on one line, so the column's clip — if any — shows in the words' width.</summary>
    private static TurnView LongProse(int ordinal) =>
        ThreadFixtures.Turn(ordinal,
            string.Join(" ", Enumerable.Repeat(LongWords, 4)),
            ThreadFixtures.Decorations("free-form", "T0", null, "free-form"),
            TurnState.Completed,
            new OutcomeView("claude-code", 0, 0, null, TimeSpan.FromSeconds(3), 3),
            [.. ThreadFixtures.Reply(1, "claude-code", "A plain prose reply: one paragraph, no tool calls, no reasoning — the shape most turns have.", 3)]);

    private Measure MeasureAt(double width, double height, IEnumerable<TurnView> turns)
    {
        var config = _workspace.Session("measure");
        using var frame = ComposedCoding.Show(width, height);
        var shell = frame.Shell;
        shell.OpenSessionDocument(config);
        frame.Settle();
        var document = frame.Document(SessionDocumentSurface.SurfaceIdFor(config.SessionId));
        Assert.Equal(ZoneId.Left, shell.Coding.Service.Zones.FindZoneOf(document.SurfaceId));   // the row's precondition: docked at Left (Ruling 83)
        Assert.True(shell.Coding.Service.Zones.Zone(ZoneId.Bottom).Collapsed);
        Feed(document, turns);
        document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
        frame.Settle();

        var feed = document.Thread;
        var words = ComposedCoding.Visuals<TextBlock>(feed).Where(t => t.Text.StartsWith("measure the words column", StringComparison.Ordinal)).First();
        var column = (FrameworkElement)VisualTreeHelper.GetParent(words);   // the body StackPanel (the star column)
        var left = ComposedCoding.Visuals<AvalonDock.Controls.LayoutDocumentPaneControl>(frame.Body).First(p => IsAncestorOf(p, document));

        // The longest word of the fixture, in the words' own type: the most a wrapped line can fall
        // short of the measure by — the band AssertWrappedAtTheMeasure reads.
        var typeface = new Typeface(words.FontFamily, words.FontStyle, words.FontWeight, words.FontStretch);
        var longestWord = LongWords.Split(' ')
            .Max(w => new FormattedText(w + " ", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, words.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(words).PixelsPerDip).WidthIncludingTrailingWhitespace);

        var m = new Measure(frame.Body.ActualWidth, left.ActualWidth, document.ActualWidth, column.ActualWidth, feed.MeasureWidth, words.ActualWidth, longestWord);
        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{width}×{height} requested, {frame.Window.ActualWidth:F0}×{frame.Window.ActualHeight:F0} given{Clamped(frame, width, height)}: body {m.Body:F0} · Left pane {m.Left:F0} · document {m.Document:F0} · words column {m.WordsColumn:F0} · 96ch = {m.MeasureWidth:F1} · words {m.Words:F1} · longest word {m.LongestWord:F1}"));
        return m;
    }

    /// <summary>
    /// Windows caps a window at the display's maximum track size, so a 2560 × 1600 request on a
    /// smaller display measures at the display's size — said in the row rather than reported as
    /// the operator's size (a plausible wrong number is the one failure mode a measurement must
    /// not have).
    /// </summary>
    private static string Clamped(ComposedCoding.Frame frame, double width, double height) =>
        Math.Abs(frame.Window.ActualWidth - width) > 1 || Math.Abs(frame.Window.ActualHeight - height) > 1
            ? " — clamped by the display, NOT the requested size"
            : string.Empty;

    private static bool IsAncestorOf(DependencyObject ancestor, DependencyObject node)
    {
        for (var n = node; n is not null; n = VisualTreeHelper.GetParent(n))
        {
            if (ReferenceEquals(n, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// L5 at the startup size — the row that promotes the extent from Inferred to measured. The size
    /// is the window's own (<c>MainWindow.xaml</c>), asserted to be the design's 1440 × 900 so the
    /// contract and the product open at the same size. <b>Red records:</b> at HEAD before the slice
    /// the session opened in the Center (the precondition); with <c>CodingLeftExtent</c> mutated to
    /// the tool default 0.22 the Left pane measured 300 px and the words' column 205 — the extent's
    /// own red, and L6 read 0 of 2 turns half visible in that column (<c>docs/proof/coding-recut-left-dock.md</c>).
    /// </summary>
    [Fact]
    public void CodingsLeftExtent_HoldsThe96chMeasureAtStartupSize()
    {
        var (width, height) = StartupSize();
        Assert.Equal((1440, 900), ((int)width, (int)height));   // DESIGN.md: "the shell's startup size, 1440 × 900"

        // Whether the operator's 2560 × 1600 row is present in this run — the display's size in
        // DIPs decides (a 2560-wide display above 100 % scaling reports fewer), said rather than absent.
        var scale = Sta.Run(() => System.Windows.Media.VisualTreeHelper.GetDpi(new System.Windows.Controls.Border()).DpiScaleX, 30);
        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"display (virtual screen, DIPs): {SystemParameters.VirtualScreenWidth:F0}×{SystemParameters.VirtualScreenHeight:F0} at DPI scale {scale:F2} (≈ {SystemParameters.VirtualScreenWidth * scale:F0}×{SystemParameters.VirtualScreenHeight * scale:F0} physical) — the 2560×1600 rows are {(Viewports().Count() > 1 ? "present" : "ABSENT (not reachable in DIPs on this display)")} in this run"));

        var m = Sta.Run(() => MeasureAt(width, height, [LongProse(1)]), 90);

        Assert.True(m.Left < m.Body - 100, $"the Left pane is the whole {m.Body:F0} body — there is no Center column beside it");
        Assert.True(m.Left > m.Body / 2, $"the Left pane is {m.Left:F0} of a {m.Body:F0} body — not the document zone's 1.3 cut");
        Assert.True(m.WordsColumn >= m.MeasureWidth + 12, $"the words' column ({m.WordsColumn:F0}) cannot hold 96ch ({m.MeasureWidth:F0}) plus its margin");
        AssertWrappedAtTheMeasure(m);
    }

    /// <summary>
    /// A wrapping TextBlock's width is its longest line, which ends at the last word boundary before
    /// its MaxWidth — so the words measure at most the measure and, at the least, the measure less
    /// the fixture's longest word (measured in the words' own type). A column narrower than that
    /// would constrain the block below the band: the clip this reads.
    /// </summary>
    private static void AssertWrappedAtTheMeasure(Measure m)
    {
        Assert.True(m.LongestWord > 0 && m.LongestWord < m.MeasureWidth / 4, $"the fixture's longest word measures {m.LongestWord:F1} px — not a band worth the name");
        Assert.True(m.Words <= m.MeasureWidth + 0.5, $"the words ({m.Words:F1}) run past the 96ch measure ({m.MeasureWidth:F1})");
        Assert.True(m.Words >= m.MeasureWidth - m.LongestWord, $"the words ({m.Words:F1}) wrap more than a word short of the 96ch measure ({m.MeasureWidth:F1} − {m.LongestWord:F1}) — the column clips them");
    }

    /// <summary>
    /// The operator's size — a row only on a display that can give 2560 × 1600 (Windows caps a
    /// window at the display's maximum track size; a clamped window would measure the display and
    /// pass under the operator's name). Absent here, the row is absent from the run, not green.
    /// Confirms or corrects CV-5.4's 1197 px assumption for the Left at 2560 × 1600.
    /// </summary>
    [Theory]
    [MemberData(nameof(Viewports))]
    public void CodingsLeftExtent_HoldsThe96chMeasureAtEveryViewportTheDisplayGives(int width, int height)
    {
        var m = Sta.Run(() => MeasureAt(width, height, [LongProse(1)]), 90);

        Assert.True(m.Left < m.Body - 100 && m.Left > m.Body / 2, $"the Left pane is {m.Left:F0} of a {m.Body:F0} body");
        Assert.True(m.WordsColumn >= m.MeasureWidth + 12, $"the words' column ({m.WordsColumn:F0}) cannot hold 96ch ({m.MeasureWidth:F0})");
        AssertWrappedAtTheMeasure(m);
    }

    private sealed record Density(double Viewport, double Editor, int Realized, int HalfVisible, string Rows);

    private Density DensityAt(double width, double height, IReadOnlyList<TurnView> turns)
    {
        var config = _workspace.Session("density");
        using var frame = ComposedCoding.Show(width, height);
        var shell = frame.Shell;
        shell.OpenSessionDocument(config);
        frame.Settle();
        var document = frame.Document(SessionDocumentSurface.SurfaceIdFor(config.SessionId));
        Assert.Equal(ZoneId.Left, shell.Coding.Service.Zones.FindZoneOf(document.SurfaceId));   // the row's precondition: docked at Left, Bottom collapsed
        Assert.True(shell.Coding.Service.Zones.Zone(ZoneId.Bottom).Collapsed);
        Feed(document, turns);
        document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
        frame.Settle();

        var feed = document.Thread;
        var scroller = ComposedCoding.Visuals<ScrollViewer>(feed).First();
        var editor = ComposedCoding.Visuals<Microsoft.Web.WebView2.Wpf.WebView2>(document.Composer).Single();
        var viewport = scroller.ViewportHeight;
        var half = 0;
        var realized = 0;
        var rows = new List<string>();
        for (var i = 0; i < turns.Count; i++)
        {
            if (feed.ItemContainerGenerator.ContainerFromIndex(i) is not ListBoxItem container || container.ActualHeight <= 0)
            {
                continue;
            }

            realized++;
            var top = container.TransformToAncestor(scroller).Transform(new Point(0, 0)).Y;
            var shown = Math.Max(0, Math.Min(top + container.ActualHeight, viewport) - Math.Max(top, 0));   // the overlap of [top, top+h] with the viewport
            rows.Add(string.Create(CultureInfo.InvariantCulture, $"b{turns[i].Ordinal} {container.ActualHeight:F0} px, {shown:F0} shown"));
            if (shown >= container.ActualHeight / 2)
            {
                half++;
            }
        }

        var d = new Density(viewport, editor.ActualHeight, realized, half, string.Join(" · ", rows));
        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{width}×{height} requested, {frame.Window.ActualWidth:F0}×{frame.Window.ActualHeight:F0} given{Clamped(frame, width, height)}, {turns.Count} turns: document {document.ActualWidth:F0}×{document.ActualHeight:F0} · thread viewport {d.Viewport:F0} · editor {d.Editor:F0} · {d.HalfVisible} of {d.Realized} realized at least half visible · {d.Rows}"));
        return d;
    }

    /// <summary>
    /// L6 (Ruling 88 condition 3; the IA lens's clear condition): at 1440 × 900, session docked Left,
    /// Bottom collapsed, editor at its 280 px rest — the thread holds ≥ 1 turn at least half visible,
    /// with the review's fixture (b4 carries 77 tool calls) and with a plain prose turn. 2560 × 1600
    /// is reported the same way; the row prints the viewport and the threshold it applied.
    /// </summary>
    [Theory]
    [MemberData(nameof(DensityRows))]
    public void AtStartupSizeDockedLeftBottomCollapsed_TheThreadHoldsOneTurn_WithTheEditorAt280(int width, int height, string fixture, int atLeast)
    {
        var turns = fixture == "five"
            ? ThreadFixtures.Five()
            : Enumerable.Range(1, 5).Select(LongProse).ToList();

        var d = Sta.Run(() => DensityAt(width, height, turns), 120);

        Assert.Equal(ComposerSurface.EditorRest, d.Editor, 1.0);
        Assert.True(d.Viewport > 0, "the thread has no viewport — the Bottom or the Center took its room");
        Assert.True(d.HalfVisible >= atLeast, $"threshold ≥ {atLeast} at {width}×{height}: {d.HalfVisible} of {d.Realized} turns at least half visible · {d.Rows}");
    }
}
