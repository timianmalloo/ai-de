using System.Globalization;
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

    private sealed record Measure(double Body, double Left, double Document, double WordsColumn, double MeasureWidth, double Words);

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
            string.Join(" ", Enumerable.Repeat("measure the words column against the ninety-six character line and its gutter at the startup size", 4)),
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

        var m = new Measure(frame.Body.ActualWidth, left.ActualWidth, document.ActualWidth, column.ActualWidth, feed.MeasureWidth, words.ActualWidth);
        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"{width}×{height} requested, {frame.Window.ActualWidth:F0}×{frame.Window.ActualHeight:F0} given{Clamped(frame, width, height)}: body {m.Body:F0} · Left pane {m.Left:F0} · document {m.Document:F0} · words column {m.WordsColumn:F0} · 96ch = {m.MeasureWidth:F1} · words {m.Words:F1}"));
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

    /// <summary>L5 at the startup size — the row that promotes the extent from Inferred to measured.</summary>
    [Fact]
    public void CodingsLeftExtent_HoldsThe96chMeasureAtStartupSize()
    {
        var m = Sta.Run(() => MeasureAt(1440, 900, [LongProse(1)]), 90);

        Assert.True(m.Left < m.Body - 100, $"the Left pane is the whole {m.Body:F0} body — there is no Center column beside it");
        Assert.True(m.Left > m.Body / 2, $"the Left pane is {m.Left:F0} of a {m.Body:F0} body — not the document zone's 1.3 cut");
        Assert.True(m.WordsColumn >= m.MeasureWidth + 12, $"the words' column ({m.WordsColumn:F0}) cannot hold 96ch ({m.MeasureWidth:F0}) plus its margin");
        AssertWrappedAtTheMeasure(m);
    }

    /// <summary>
    /// A wrapping TextBlock's width is its longest line, which ends at the last word boundary before
    /// its MaxWidth — so the words measure at most the measure and within one word of it. A column
    /// narrower than the measure would constrain the block below that band: the clip this reads.
    /// </summary>
    private static void AssertWrappedAtTheMeasure(Measure m)
    {
        Assert.True(m.Words <= m.MeasureWidth + 0.5, $"the words ({m.Words:F1}) run past the 96ch measure ({m.MeasureWidth:F1})");
        Assert.True(m.Words >= m.MeasureWidth - 80, $"the words ({m.Words:F1}) wrap well short of the 96ch measure ({m.MeasureWidth:F1}) — the column clips them");
    }

    /// <summary>The operator's size, reported: confirms or corrects CV-5.4's 1197 px assumption for the Left at 2560 × 1600.</summary>
    [Fact]
    public void CodingsLeftExtent_HoldsThe96chMeasureAtTheOperatorsSize()
    {
        var m = Sta.Run(() => MeasureAt(2560, 1600, [LongProse(1)]), 90);

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
    [InlineData(1440, 900, "five", 1)]
    [InlineData(1440, 900, "prose", 1)]
    [InlineData(2560, 1600, "five", 1)]
    [InlineData(2560, 1600, "prose", 1)]
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
