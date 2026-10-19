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
/// Ruling 168 (Ruling 88 condition 3, retargeted from Ruling 83's Left dock): the thread's 96ch
/// measure, <see cref="ThreadFeed.MeasureWidth"/>, is a property of the thread's text and moves with
/// the notebook to the Center. It is held <b>both ways</b>, measured on the composed tree:
/// (1) the turn's row holds at least the measure plus its 40 px gutter and the body's padding, with
/// no clipped words; (2) the rendered line width never exceeds the measure — it is a cap, and extra
/// Center width becomes margin, not longer lines; (3) both at the minimum window size and at a wide
/// one. The numbers each row prints are the record.
/// </summary>
public sealed class TheThreadMeasureMovesWithTheNotebookTests(ITestOutputHelper output) : IDisposable
{
    private readonly ComposedCoding.Workspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    private sealed record Measure(double Body, double Pane, double Row, double WordsColumn, double MeasureWidth, double Words, double Reply, double LongestWord);

    /// <summary>The words of the long-prose fixture, one sentence repeated so the line runs far past 96 characters.</summary>
    private const string LongWords = "measure the words column against the ninety-six character line and its gutter at the startup size";

    /// <summary>The turn's gutter column (<c>ThreadFeed.TurnTemplate</c>) and the body's right padding.</summary>
    private const double Gutter = 40;
    private const double BodyPadding = 12;

    /// <summary>A Center at least this much wider than the measure exercises the cap rather than the column.</summary>
    private const double WideSlack = 100;

    private static string MainWindowXaml()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        while (here is not null && !File.Exists(Path.Combine(here.FullName, "AiDe.sln")))
        {
            here = here.Parent;
        }

        Assert.NotNull(here);
        return File.ReadAllText(Path.Combine(here!.FullName, "src", "AiDe.App", "MainWindow.xaml"));
    }

    /// <summary>One attribute of <c>MainWindow.xaml</c> — the size the product opens at, or its minimum.</summary>
    private static double WindowAttribute(string name)
    {
        var value = System.Text.RegularExpressions.Regex.Match(MainWindowXaml(), "\\b" + name + "=\"(\\d+)\"").Groups[1].Value;
        Assert.True(value.Length > 0, $"MainWindow.xaml declares no {name} — this test is reading a document whose shape it does not understand (DC-016)");
        return double.Parse(value, CultureInfo.InvariantCulture);
    }

    private static (double Width, double Height) StartupSize() => (WindowAttribute("Width"), WindowAttribute("Height"));

    private static (double Width, double Height) MinimumSize() => (WindowAttribute("MinWidth"), WindowAttribute("MinHeight"));

    /// <summary>
    /// The wide row: the operator's 2560 × 1600, or the largest the display gives in DIPs when it
    /// cannot (a clamped window measures the display, and the row says so rather than reporting the
    /// operator's size).
    /// </summary>
    private static (double Width, double Height) WideSize() =>
        (Math.Min(2560, Math.Floor(SystemParameters.VirtualScreenWidth)), Math.Min(1600, Math.Floor(SystemParameters.VirtualScreenHeight)));

    /// <summary>The three rows Ruling 168 (3) names: the minimum, the startup size, and a wide one.</summary>
    public static IEnumerable<object[]> Viewports()
    {
        foreach (var (name, (width, height)) in new[] { ("minimum", MinimumSize()), ("startup", StartupSize()), ("wide", WideSize()) })
        {
            yield return [name, (int)width, (int)height];
        }
    }

    /// <summary>The density rows: the startup size with both fixtures.</summary>
    public static IEnumerable<object[]> DensityRows()
    {
        var (width, height) = StartupSize();
        yield return [(int)width, (int)height, "five", 1];
        yield return [(int)width, (int)height, "prose", 1];
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

    /// <summary>A turn whose words and reply each run well past 96 characters, so a clip or a stretch shows in their widths.</summary>
    private static TurnView LongProse(int ordinal) =>
        ThreadFixtures.Turn(ordinal,
            string.Join(" ", Enumerable.Repeat(LongWords, 4)),
            ThreadFixtures.Decorations("free-form", "T0", null, "free-form"),
            TurnState.Completed,
            new OutcomeView("claude-code", 0, 0, null, TimeSpan.FromSeconds(3), 3),
            [.. ThreadFixtures.Reply(1, "claude-code", string.Join(" ", Enumerable.Repeat("A plain prose reply that runs on past the measure so its rendered line width can be read.", 4)), 3)]);

    private Measure MeasureAt(double width, double height, IEnumerable<TurnView> turns)
    {
        var config = _workspace.Session("measure");
        using var frame = ComposedCoding.Show(width, height);
        var shell = frame.Shell;
        shell.OpenSessionDocument(config);
        frame.Settle();
        var document = frame.Document(SessionDocumentSurface.SurfaceIdFor(config.SessionId));
        Assert.Equal(ZoneId.Center, shell.Factory.Service.Zones.FindZoneOf(document.SurfaceId));   // the row's precondition: the notebook in the Center (ADR-0045 §1(a))
        Assert.True(shell.Factory.Service.Zones.Zone(ZoneId.Bottom).Collapsed);
        Feed(document, turns);
        document.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });
        frame.Settle();

        var feed = document.Thread;
        var words = ComposedCoding.Visuals<TextBlock>(feed).First(t => t.Text.StartsWith("measure the words column", StringComparison.Ordinal));
        var column = (FrameworkElement)VisualTreeHelper.GetParent(words);   // the body StackPanel (the star column)
        var row = (FrameworkElement)VisualTreeHelper.GetParent(column);     // the turn's grid: gutter + body
        var reply = ComposedCoding.Visuals<ProseView>(feed).SelectMany(ComposedCoding.Visuals<TextBlock>).Max(t => t.ActualWidth);
        var pane = ComposedCoding.Visuals<AvalonDock.Controls.LayoutDocumentPaneControl>(frame.Body).First(p => IsAncestorOf(p, document));

        // The longest word of the fixture, in the words' own type: the most a wrapped line can fall
        // short of the measure by — the band AssertHeldBothWays reads.
        var typeface = new Typeface(words.FontFamily, words.FontStyle, words.FontWeight, words.FontStretch);
        var longestWord = LongWords.Split(' ')
            .Max(w => new FormattedText(w + " ", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, words.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(words).PixelsPerDip).WidthIncludingTrailingWhitespace);

        var m = new Measure(frame.Body.ActualWidth, pane.ActualWidth, row.ActualWidth, column.ActualWidth, feed.MeasureWidth, words.ActualWidth, reply, longestWord);
        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{width}×{height} requested, {frame.Window.ActualWidth:F0}×{frame.Window.ActualHeight:F0} given{Clamped(frame, width, height)}: body {m.Body:F0} · Center pane {m.Pane:F0} · turn row {m.Row:F0} · words column {m.WordsColumn:F0} · 96ch = {m.MeasureWidth:F1} · words {m.Words:F1} · reply line {m.Reply:F1} · longest word {m.LongestWord:F1}"));
        return m;
    }

    /// <summary>
    /// Windows caps a window at the display's maximum track size, so a request larger than the
    /// display measures at the display's size — said in the row rather than reported as the
    /// requested size (a plausible wrong number is the one failure mode a measurement must not have).
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
    /// Ruling 168 (1)–(3) at every row. Never clipped: the turn's row holds the measure plus its
    /// gutter and the body's padding, and the wrapped words reach within one word of the measure.
    /// Never stretched: the words and the reply's widest line stay at or under the measure. At the
    /// wide row the Center is at least <see cref="WideSlack"/> wider than the measure, so it is the
    /// cap — not the column — that holds the line.
    /// </summary>
    [Theory]
    [MemberData(nameof(Viewports))]
    public void TheThreadsMeasure_IsHeldBothWaysInTheCenter_NeverClippedNeverStretched(string row, int width, int height)
    {
        if (row == "startup")
        {
            Assert.Equal((1440, 900), (width, height));   // DESIGN.md: "the shell's startup size, 1440 × 900"
        }

        var m = Sta.Run(() => MeasureAt(width, height, [LongProse(1)]), 90);

        Assert.True(m.LongestWord > 0 && m.LongestWord < m.MeasureWidth / 4, $"{row}: the fixture's longest word measures {m.LongestWord:F1} px — not a band worth the name");

        // (1) never clipped.
        Assert.True(m.Row >= m.MeasureWidth + Gutter + BodyPadding, $"{row}: the turn's row ({m.Row:F0}) cannot hold 96ch ({m.MeasureWidth:F0}) plus its {Gutter} px gutter and {BodyPadding} px padding");
        Assert.True(m.Words >= m.MeasureWidth - m.LongestWord, $"{row}: the words ({m.Words:F1}) wrap more than a word short of the 96ch measure ({m.MeasureWidth:F1} − {m.LongestWord:F1}) — the column clips them");

        // (2) never stretched: the measure is a cap.
        Assert.True(m.Words <= m.MeasureWidth + 0.5, $"{row}: the words ({m.Words:F1}) run past the 96ch measure ({m.MeasureWidth:F1})");
        Assert.True(m.Reply <= m.MeasureWidth + 0.5, $"{row}: the reply's line ({m.Reply:F1}) runs past the 96ch measure ({m.MeasureWidth:F1})");

        // (3) the wide row exercises the cap.
        if (row == "wide")
        {
            Assert.True(m.WordsColumn >= m.MeasureWidth + WideSlack, $"wide: the words column ({m.WordsColumn:F0}) is not {WideSlack} px wider than 96ch ({m.MeasureWidth:F0}) — the row does not test the cap");
        }
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
        Assert.Equal(ZoneId.Center, shell.Factory.Service.Zones.FindZoneOf(document.SurfaceId));   // the row's precondition: the notebook in the Center, Bottom collapsed
        Assert.True(shell.Factory.Service.Zones.Zone(ZoneId.Bottom).Collapsed);
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
    /// L6 (Ruling 88 condition 3): at the startup size, the notebook in the Center, Bottom collapsed,
    /// editor at its 280 px rest — the thread holds ≥ 1 turn at least half visible, with the review's
    /// fixture (b4 carries 77 tool calls) and with a plain prose turn.
    /// </summary>
    [Theory]
    [MemberData(nameof(DensityRows))]
    public void AtStartupSizeInTheCenterBottomCollapsed_TheThreadHoldsOneTurn_WithTheEditorAt280(int width, int height, string fixture, int atLeast)
    {
        var turns = fixture == "five"
            ? ThreadFixtures.Five()
            : Enumerable.Range(1, 5).Select(LongProse).ToList();

        var d = Sta.Run(() => DensityAt(width, height, turns), 120);

        Assert.Equal(ComposerSurface.EditorRest, d.Editor, 1.0);
        Assert.True(d.Viewport > 0, "the thread has no viewport — the Bottom took its room");
        Assert.True(d.HalfVisible >= atLeast, $"threshold ≥ {atLeast} at {width}×{height}: {d.HalfVisible} of {d.Realized} turns at least half visible · {d.Rows}");
    }
}
