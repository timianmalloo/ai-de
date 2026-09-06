using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AiDe.App.Workbench;
using AiDe.Core.Terminal;

namespace AiDe.App.Tests;

/// <summary>
/// The renderer: that it draws, and that it draws fast enough.
/// </summary>
/// <remarks>
/// <para>The performance case is the reason this class exists. Spike S3 measured the draw paths
/// before any renderer was written — <c>GlyphRun</c> per line at 6.64 ms p95 against
/// <c>FormattedText</c> per cell at 142.80 ms, 21× slower and four times over the frame budget — and
/// made the fast path binding for Phase 2. A design decision recorded only in prose survives until
/// the first person who has never read it makes a reasonable-looking change; measuring the real
/// renderer here is what turns that decision into something that fails when it is undone.</para>
///
/// <para>These run on dedicated STA threads with parallelisation disabled (<b>DC-008</b>).</para>
/// </remarks>
public sealed class TerminalViewTests
{
    private static T OnStaThread<T>(Func<T> work) =>
        Sta.Run<T>(work, 60);

    /// <summary>Lays the view out and rasterises it, which is what forces the real draw work.</summary>
    private static RenderTargetBitmap Rasterise(TerminalView view, int width, int height)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        return bitmap;
    }

    // ---- metrics ------------------------------------------------------------

    [Fact]
    public void TheCellGrid_HasPositiveMetrics()
    {
        // Zero would divide by zero when the view converts pixels to columns, and a monospace face
        // that failed to load is the way that happens.
        var (width, height) = OnStaThread(() =>
        {
            var view = new TerminalView(new TerminalScreen(80, 24));
            return (view.CellWidth, view.CellHeight);
        });

        Assert.True(width > 0, $"cell width was {width}");
        Assert.True(height > 0, $"cell height was {height}");
    }

    // ---- drawing ------------------------------------------------------------

    [Fact]
    public void AScreenWithText_Rasterises()
    {
        var rendered = OnStaThread(() =>
        {
            var screen = new TerminalScreen(80, 24);
            screen.Write("hello from the terminal");
            var view = new TerminalView(screen);
            return Rasterise(view, 800, 400).PixelWidth;
        });

        Assert.Equal(800, rendered);
    }

    [Fact]
    public void EveryStyleCombination_DrawsWithoutThrowing()
    {
        // Colours, attributes and inverse are the code paths that branch inside the run builder;
        // exercising them together is what catches an index or a null a plain-text screen misses.
        var thrown = OnStaThread(() =>
        {
            var screen = new TerminalScreen(80, 10);
            var parser = new VtParser(screen);
            parser.Consume(System.Text.Encoding.UTF8.GetBytes(
                "[31mred [1;4;7mbold underline inverse [38;5;208m256 "
                + "[38;2;10;200;30mtruecolour [44mon blue [0mplain"));

            try
            {
                var view = new TerminalView(screen);
                Rasterise(view, 800, 200);
                return null as Exception;
            }
            catch (Exception ex)
            {
                return ex;
            }
        });

        Assert.Null(thrown);
    }

    [Fact]
    public void ACharacterTheFontLacks_FallsBackInsteadOfThrowing()
    {
        // CharacterToGlyphMap throws on a miss, and a program printing an uncovered codepoint is an
        // ordinary event — so an unhandled miss is a crash any child process can trigger by echoing.
        var thrown = OnStaThread(() =>
        {
            var screen = new TerminalScreen(20, 2);
            screen.Write("安ก�");

            try
            {
                Rasterise(new TerminalView(screen), 200, 60);
                return null as Exception;
            }
            catch (Exception ex)
            {
                return ex;
            }
        });

        Assert.Null(thrown);
    }

    // ---- the budget S3 made binding --------------------------------------------

    /// <summary>Builds the 200×50 styled screen S3 measured — the shape a real terminal presents.</summary>
    /// <remarks>
    /// Styled runs rather than uniform text: a screen of one style would collapse to one run per line
    /// and measure a case a real terminal never presents.
    /// </remarks>
    private static TerminalScreen BusyScreen()
    {
        var screen = new TerminalScreen(200, 50);
        var parser = new VtParser(screen);

        for (var row = 0; row < 50; row++)
        {
            parser.Consume(System.Text.Encoding.UTF8.GetBytes(
                $"\u001b[3{row % 8}mbuild \u001b[1mmodule-{row:D2}\u001b[0m compiled in {row * 7 % 900}ms "
                + $"\u001b[32mok\u001b[0m \u001b[90m/src/path/to/file-{row:D2}.cs\u001b[0m\r\n"));
        }

        return screen;
    }

    /// <summary>
    /// p95 of <paramref name="frames"/> frames, where a frame is <b>produce the drawing and
    /// rasterise it</b>.
    /// </summary>
    /// <remarks>
    /// <b>Producing the drawing is inside the timed region on purpose.</b> The cost of the per-cell
    /// path is overwhelmingly the construction of one <see cref="FormattedText"/> per cell, which a
    /// real renderer pays on every frame inside <c>OnRender</c>. Building the visual once and timing
    /// only <c>Render</c> measures rasterisation alone and makes the rejected path look ~7× cheaper
    /// than it is — which is how a reference stops being a reference.
    /// </remarks>
    private static double FrameP95(int frames, Func<Visual> drawFrame)
    {
        var samples = new List<double>(frames);

        for (var frame = 0; frame < frames; frame++)
        {
            var bitmap = new RenderTargetBitmap(1600, 1000, 96, 96, PixelFormats.Pbgra32);

            var watch = Stopwatch.StartNew();
            bitmap.Render(drawFrame());
            watch.Stop();

            samples.Add(watch.Elapsed.TotalMilliseconds);
        }

        samples.Sort();
        return samples[Math.Max(0, (int)(samples.Count * 0.95) - 1)];
    }

    /// <summary>
    /// The reference: the rejected path, drawn here so the fast path has something to be fast
    /// <i>against</i>. One <see cref="FormattedText"/> per cell, which is what S3 measured at 142.80 ms.
    /// </summary>
    private static DrawingVisual PerCellReference(TerminalScreen screen, double cellWidth, double cellHeight)
    {
        var typeface = new Typeface(
            new FontFamily("Cascadia Mono, Consolas"),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            for (var row = 0; row < screen.Rows; row++)
            {
                for (var column = 0; column < screen.Columns; column++)
                {
                    var text = new FormattedText(
                        screen[row, column].Character.ToString(),
                        System.Globalization.CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, typeface, 13, Brushes.White, 96);

                    context.DrawText(text, new Point(column * cellWidth, row * cellHeight));
                }
            }
        }

        return visual;
    }

    /// <summary>
    /// EXPECTED RED IF THE DRAW PATH REVERTS: a full-screen redraw is drawn per LINE, not per cell.
    /// </summary>
    /// <remarks>
    /// <para><b>This assertion used to be a wall-clock constant, and it measured the machine.</b> It
    /// read <c>p95 &lt; 16.67 ms</c> — a number taken on a developer workstation. On the CI runner,
    /// which is roughly 3× slower, the correct draw path measures 17–23 ms, so the test failed
    /// deterministically on hardware while the code was right. It stayed red for 38 consecutive runs
    /// and, because it is ordered ahead of them, took 26 other gates down with it (INV-0005, DC-107).</para>
    ///
    /// <para><b>The guard band was the defect, not the threshold.</b> The signal this test wants is
    /// architectural and enormous — S3 measured GlyphRun-per-line at 6.64 ms against
    /// FormattedText-per-cell at 142.80 ms, a <b>21×</b> difference. The budget sat at 2.5× the good
    /// value, and the spread between the machines it runs on is ~3×. The noise was wider than the
    /// window, so the test could never reach its own subject.</para>
    ///
    /// <para><b>So it now measures the thing it is actually about, on whatever host it finds.</b> Both
    /// paths are rasterised in the same process, back to back, and the assertion is the <i>ratio</i>.
    /// A host that is slow makes both numbers big and changes nothing; a draw path that reverts to
    /// per-cell collapses the ratio toward 1 and reddens this immediately.</para>
    ///
    /// <para><b>It also still catches a large absolute regression</b>, which an absolute budget is
    /// usually kept around for: the reference is built in this test and does not go through
    /// <see cref="TerminalView"/>, so if the real renderer got 10× slower while staying per-line, the
    /// ratio would close and this would fail. No opt-in wall-clock check is kept alongside — a check
    /// that only ever runs on the machine that wrote it is not yet a control (CE23).</para>
    /// </remarks>
    [Fact]
    public void AFullScreenRedraw_IsDrawnPerLine_NotPerCell()
    {
        // 21× measured. Five is a deliberately loose floor: it is far below the real margin, so JIT
        // warm-up, a noisy runner or a shared CPU cannot reach it, and only a change of DRAW SHAPE can.
        const double MinimumSpeedup = 5.0;

        var (perLineP95, perCellP95) = OnStaThread(() =>
        {
            var screen = BusyScreen();
            var view = new TerminalView(screen);

            view.Measure(new Size(1600, 1000));
            view.Arrange(new Rect(0, 0, 1600, 1000));
            view.UpdateLayout();

            // InvalidateVisual inside the timed frame, so the view's own OnRender work is counted —
            // the same shape as the reference below, which rebuilds its drawing each frame.
            var fast = FrameP95(30, () => { view.InvalidateVisual(); return view; });

            // Fewer frames: the reference is ~20× slower, so it costs ~20× per sample and the ratio
            // does not need the same resolution as the number being defended.
            var slow = FrameP95(5, () => PerCellReference(screen, view.CellWidth, view.CellHeight));

            return (fast, slow);
        });

        Assert.True(
            perLineP95 * MinimumSpeedup < perCellP95,
            $"the per-line draw path was {perLineP95:F2} ms p95 against a per-cell reference of "
            + $"{perCellP95:F2} ms on this host — only {perCellP95 / perLineP95:F1}× faster, under the "
            + $"{MinimumSpeedup}× floor. S3 measured 21× (6.64 ms vs 142.80 ms). A ratio near 1 means "
            + "the renderer has reverted to per-cell text; a ratio that has merely shrunk means "
            + "TerminalView got materially slower while staying per-line. This is a ratio on purpose: "
            + "an absolute budget here measured the machine, not the code (DC-107).");
    }

    // ---- the palette is the tokens ------------------------------------------------

    [Fact]
    public void ThePaletteFallbacks_MatchTheTokensInAppXaml()
    {
        // TerminalPalette carries fallback values for use outside a running Application — which is
        // exactly what a unit test is. Two copies of a palette drift, and the drift would be
        // invisible: tests would assert one set of colours and the product would draw the other.
        var xaml = File.ReadAllText(Path.Combine(ProjectRoot(), "App.xaml"));
        var source = File.ReadAllText(Path.Combine(ProjectRoot(), "Workbench", "TerminalPalette.cs"));

        foreach (Match token in Regex.Matches(xaml, @"x:Key=""(Terminal\w+)"">#([0-9A-Fa-f]{6})<"))
        {
            var name = token.Groups[1].Value;
            var hex = token.Groups[2].Value.ToUpperInvariant();
            var expected = $"0x{hex[..2]}, 0x{hex[2..4]}, 0x{hex[4..]}";

            Assert.True(
                source.Contains(expected, StringComparison.OrdinalIgnoreCase),
                $"{name} is #{hex} in App.xaml, but TerminalPalette.cs has no matching "
                + $"Color.FromRgb({expected}). The token is the source of truth; update the fallback.");
        }
    }

    private static string ProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AiDe.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "src", "AiDe.App");
    }
}
