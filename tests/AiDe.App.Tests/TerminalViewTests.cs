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

    [Fact]
    public void AFullScreenRedraw_StaysInsideTheFrameBudget()
    {
        // 200x50 = 10,000 cells, the same shape S3 measured. Rasterised rather than timed at the
        // instruction-list level so the number is WPF's actual work.
        const int Frames = 30;
        const double FrameBudgetMs = 16.67;

        var p95 = OnStaThread(() =>
        {
            var screen = new TerminalScreen(200, 50);
            var parser = new VtParser(screen);

            // Styled runs rather than uniform text: a screen of one style would collapse to one run
            // per line and measure a case a real terminal never presents.
            for (var row = 0; row < 50; row++)
            {
                parser.Consume(System.Text.Encoding.UTF8.GetBytes(
                    $"[3{row % 8}mbuild [1mmodule-{row:D2}[0m compiled in {row * 7 % 900}ms "
                    + $"[32mok[0m [90m/src/path/to/file-{row:D2}.cs[0m\r\n"));
            }

            var view = new TerminalView(screen);
            view.Measure(new Size(1600, 1000));
            view.Arrange(new Rect(0, 0, 1600, 1000));
            view.UpdateLayout();

            var samples = new List<double>(Frames);

            for (var frame = 0; frame < Frames; frame++)
            {
                var bitmap = new RenderTargetBitmap(1600, 1000, 96, 96, PixelFormats.Pbgra32);

                var watch = Stopwatch.StartNew();
                view.InvalidateVisual();
                bitmap.Render(view);
                watch.Stop();

                samples.Add(watch.Elapsed.TotalMilliseconds);
            }

            samples.Sort();
            return samples[(int)(samples.Count * 0.95) - 1];
        });

        Assert.True(
            p95 < FrameBudgetMs,
            $"full-screen redraw p95 was {p95:F2} ms, over the {FrameBudgetMs} ms frame budget. "
            + "S3 measured GlyphRun-per-line at 6.64 ms and FormattedText-per-cell at 142.80 ms, so "
            + "a number in the hundreds means the draw path has reverted to per-cell text.");
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
