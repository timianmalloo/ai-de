using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TerminalRendererSpike;

/// <summary>
/// Spike S3 — can AI-DE own its terminal renderer in WPF, and does the render or the parse decide it?
/// </summary>
/// <remarks>
/// <para>ADR-0005 deferred the renderer choice behind <c>ITerminalSession</c>. The Phase-2 design
/// scoped S3 as "which renderer meets the keyboard/screen-reader contract" — but
/// <see href="../../docs/adr/0014-accessibility-posture.md">ADR-0014</see> withdrew the
/// accessibility obligation, so the selection criteria are now throughput, fidelity, input handling,
/// licence and integration cost.</para>
///
/// <para><b>What that leaves as the load-bearing unknown.</b> Two of the three candidate approaches
/// are already decided by evidence in hand. Embedding an unsupported WPF terminal control was
/// rejected by ADR-0005 on maintenance grounds. Hosting <c>xterm.js</c> in WebView2 inherits
/// everything spike S4 measured on 2026-08-26 — airspace in the default control, a process-killing
/// crash in the composition control when its pane is floated, and <c>Focus()</c> refused in both —
/// and a terminal is the surface that most needs keyboard focus, so it is the worst candidate for
/// that defect. What is genuinely unmeasured is whether <b>owning a WPF renderer</b> is fast
/// enough.</para>
///
/// <para><b>The budget, and the distinction the budget hides.</b> The architecture budgets 1 MiB/s
/// of terminal output. That is a *parse* rate, not a *draw* rate: a terminal coalesces, so it must
/// consume a megabyte a second while only ever presenting the final screen state at frame rate.
/// Those two are measured separately here, because conflating them is how a renderer gets blamed for
/// a parser's cost.</para>
/// </remarks>
internal static class Program
{
    private const int Columns = 200;
    private const int Rows = 50;
    private const int Frames = 60;

    // 60 fps leaves 16.7 ms per frame; 30 fps leaves 33.3 ms. A terminal that redraws a full screen
    // inside the smaller number has margin for the rest of the shell.
    private const double Budget60 = 1000.0 / 60;
    private const double Budget30 = 1000.0 / 30;

    [STAThread]
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var exit = 0;

        application.Startup += (_, _) =>
        {
            try
            {
                exit = Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
                exit = 1;
            }
            finally
            {
                application.Shutdown();
            }
        };

        application.Run();
        return exit;
    }

    private static int Run()
    {
        Header("Q0 — what is being measured");
        Console.WriteLine($"Grid            : {Columns} cols x {Rows} rows = {Columns * Rows:N0} cells");
        Console.WriteLine($"Frames per case : {Frames}");
        Console.WriteLine($"Frame budget    : {Budget60:F1} ms at 60fps, {Budget30:F1} ms at 30fps");
        Console.WriteLine($"Rendering       : offscreen via DrawingVisual + RenderTargetBitmap,");
        Console.WriteLine($"                  so the number is WPF's cost and not the compositor's.");

        var screen = Screen.Sample(Columns, Rows);
        var typeface = new Typeface(
            new FontFamily("Cascadia Mono, Consolas, Courier New"),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        if (!typeface.TryGetGlyphTypeface(out var glyphTypeface))
        {
            Console.WriteLine("FAIL: no monospace glyph typeface available.");
            return 1;
        }

        Console.WriteLine($"Typeface        : {glyphTypeface.FamilyNames.Values.FirstOrDefault() ?? "?"}");

        Header("Q1 — DRAW: how long does a full-screen redraw take?");

        var results = new List<Measurement>
        {
            Measure("GlyphRun per line", () => DrawGlyphRuns(screen, glyphTypeface)),
            Measure("FormattedText per line", () => DrawFormattedText(screen, typeface)),
            Measure("FormattedText per CELL", () => DrawPerCell(screen, typeface)),
        };

        Console.WriteLine();
        Console.WriteLine($"{"approach",-26}{"p50 ms",8}{"p95 ms",12}{"max fps",11}   verdict");
        Console.WriteLine(new string('-', 78));
        foreach (var r in results)
        {
            var verdict = r.P95 <= Budget60 ? "inside 60fps"
                : r.P95 <= Budget30 ? "inside 30fps only"
                : "OVER BUDGET";
            Console.WriteLine($"{r.Name,-26}{r.P50,8:F2}{r.P95,12:F2}{1000 / r.P95,11:F0}   {verdict}");
        }

        Header("Q2 — PARSE: can a VT stream be consumed at the 1 MiB/s budget?");
        MeasureParse();

        Header("Verdict");
        var best = results.OrderBy(r => r.P95).First();
        Console.WriteLine($"Fastest draw path: {best.Name} at p95 {best.P95:F2} ms "
            + $"({1000 / best.P95:F0} fps ceiling).");
        Console.WriteLine();
        if (best.P95 <= Budget60)
        {
            Console.WriteLine("RESULT: owning a WPF renderer is VIABLE on throughput. A full 200x50");
            Console.WriteLine("  redraw fits inside a 60fps frame with margin, so the renderer is not");
            Console.WriteLine("  the constraint — the parser and the coalescing policy are.");
        }
        else if (best.P95 <= Budget30)
        {
            Console.WriteLine("RESULT: viable only at 30fps. Acceptable for a terminal, but the margin");
            Console.WriteLine("  is thin enough that damage-tracking (redraw only changed rows) stops");
            Console.WriteLine("  being an optimisation and becomes a requirement.");
        }
        else
        {
            Console.WriteLine("RESULT: a naive full-screen WPF redraw does NOT meet the budget.");
            Console.WriteLine("  Owning the renderer needs damage tracking or a different draw path.");
        }

        return 0;
    }

    private static Measurement Measure(string name, Action draw)
    {
        // One untimed pass so font/glyph caches and JIT are not charged to the first sample —
        // otherwise the first measurement describes startup, not steady state.
        draw();

        var samples = new List<double>(Frames);
        for (var i = 0; i < Frames; i++)
        {
            var watch = Stopwatch.StartNew();
            draw();
            watch.Stop();
            samples.Add(watch.Elapsed.TotalMilliseconds);
        }

        samples.Sort();
        var measurement = new Measurement(
            name,
            samples[samples.Count / 2],
            samples[(int)(samples.Count * 0.95)]);

        Console.WriteLine($"  {name,-26} p50 {measurement.P50,7:F2} ms   p95 {measurement.P95,7:F2} ms");
        return measurement;
    }

    /// <summary>The path a real terminal takes: cached glyph indices, one run per line.</summary>
    private static void DrawGlyphRuns(Screen screen, GlyphTypeface glyphs)
    {
        const double size = 14;
        var advance = glyphs.AdvanceWidths[glyphs.CharacterToGlyphMap['M']] * size;
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            for (var row = 0; row < screen.Rows; row++)
            {
                var line = screen.Line(row);
                var indices = new ushort[line.Length];
                var widths = new double[line.Length];

                for (var i = 0; i < line.Length; i++)
                {
                    indices[i] = glyphs.CharacterToGlyphMap.TryGetValue(line[i], out var g) ? g : (ushort)0;
                    widths[i] = advance;
                }

                var run = new GlyphRun(
                    glyphs, 0, false, size, 96f, indices, new Point(0, (row * size * 1.2) + size),
                    widths, null, null, null, null, null, null);

                context.DrawGlyphRun(screen.Brush(row), run);
            }
        }

        Rasterize(visual, screen, advance, size);
    }

    /// <summary>One <c>FormattedText</c> per line — the obvious implementation.</summary>
    private static void DrawFormattedText(Screen screen, Typeface typeface)
    {
        const double size = 14;
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            for (var row = 0; row < screen.Rows; row++)
            {
                var text = new FormattedText(
                    new string(screen.Line(row)), CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, size, screen.Brush(row), 96);
                context.DrawText(text, new Point(0, row * size * 1.2));
            }
        }

        Rasterize(visual, screen, 8.4, size);
    }

    /// <summary>
    /// One <c>FormattedText</c> per CELL — the shape you get by modelling a terminal as a grid of
    /// styled cells and drawing each one, which is the natural first design.
    /// </summary>
    private static void DrawPerCell(Screen screen, Typeface typeface)
    {
        const double size = 14;
        const double advance = 8.4;
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            for (var row = 0; row < screen.Rows; row++)
            {
                var line = screen.Line(row);
                var brush = screen.Brush(row);
                for (var column = 0; column < line.Length; column++)
                {
                    var text = new FormattedText(
                        line[column].ToString(), CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight, typeface, size, brush, 96);
                    context.DrawText(text, new Point(column * advance, row * size * 1.2));
                }
            }
        }

        Rasterize(visual, screen, advance, size);
    }

    /// <summary>
    /// Forces the visual to actually rasterise.
    /// </summary>
    /// <remarks>
    /// Without this the measurement times only the building of a drawing instruction list, which is
    /// the cheap half and would make every approach look viable. Rendering to a bitmap is what makes
    /// the number describe work that has to happen (defect class DC-009 — a proxy fails differently
    /// from the thing it stands for).
    /// </remarks>
    private static void Rasterize(DrawingVisual visual, Screen screen, double advance, double size)
    {
        var width = (int)(screen.Columns * advance) + 8;
        var height = (int)(screen.Rows * size * 1.2) + 8;
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
    }

    private static void MeasureParse()
    {
        var payload = Screen.VtStream(1024 * 1024);
        Console.WriteLine($"  stream: {payload.Length / 1024.0 / 1024.0:F2} MiB of representative VT output");

        var parser = new VtScanner();
        var samples = new List<double>();

        parser.Scan(payload);
        for (var i = 0; i < 5; i++)
        {
            var watch = Stopwatch.StartNew();
            parser.Scan(payload);
            watch.Stop();
            samples.Add(watch.Elapsed.TotalMilliseconds);
        }

        samples.Sort();
        var median = samples[samples.Count / 2];
        var rate = 1000.0 / median;

        Console.WriteLine($"  parsed in {median:F1} ms  =>  {rate:F1} MiB/s");
        Console.WriteLine($"  printable={parser.Printable:N0}  escapes={parser.Escapes:N0}  "
            + $"newlines={parser.Newlines:N0}");
        Console.WriteLine(rate >= 1.0
            ? $"  OK  {rate:F0}x the 1 MiB/s budget on a single thread."
            : "  x   BELOW the 1 MiB/s budget.");
    }

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 78));
    }
}

internal sealed record Measurement(string Name, double P50, double P95);
