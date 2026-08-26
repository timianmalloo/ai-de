using System.Windows;
using System.Windows.Media;

namespace Webview2SnapshotSwapSpike;

/// <summary>
/// Gut check for the S4 decision — can a still frame stand in for the live canvas while WPF draws
/// over it, without a visible seam?
/// </summary>
/// <remarks>
/// <para>Spike S4 established that the windowed WebView2 cannot be drawn over and that the
/// composition control, which can, kills the process when its pane is floated. The chosen answer is
/// to keep the windowed control and hide it behind a snapshot for the moments WPF needs the space —
/// an AvalonDock drop indicator during a drag, a command palette over a large canvas.</para>
///
/// <para><b>The risk is not whether it works; it is whether it shows.</b> The capture comes back in
/// device pixels and the <c>Image</c> is laid out in DIPs, and this machine runs at 150%, so a
/// naive swap can land soft, offset or scaled. The page is therefore four hard-edged quadrants and
/// the samples sit a few pixels either side of the seams: a sub-pixel misplacement becomes a colour
/// flip rather than a judgement call about sharpness.</para>
/// </remarks>
internal static class Program
{
    private static readonly (string Name, (byte R, byte G, byte B) Colour)[] Quadrants =
    [
        ("crimson", (0xE1, 0x1D, 0x48)),
        ("blue", (0x25, 0x63, 0xEB)),
        ("amber", (0xF5, 0x9E, 0x0B)),
        ("violet", (0x7C, 0x3A, 0xED)),
    ];

    private static readonly Color OverlayColour = Color.FromRgb(0x22, 0xC5, 0x5E);
    private static readonly Color PaneColour = Color.FromRgb(0x1E, 0x29, 0x3B);

    private static readonly List<string> Findings = [];

    [STAThread]
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var exit = 0;

        application.DispatcherUnhandledException += (_, e) =>
        {
            Console.WriteLine($"  x dispatcher exception: {e.Exception.GetType().Name}: "
                + $"{e.Exception.Message.Trim()}");
            Findings.Add($"dispatcher exception {e.Exception.GetType().Name}");
            e.Handled = true;
        };

        application.Startup += async (_, _) =>
        {
            try
            {
                exit = await RunAsync();
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

    private static async Task<int> RunAsync()
    {
        Header("Q0 - environment");
        Console.WriteLine($"WebView2 Runtime : "
            + Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString());

        var shell = new Shell(OverlayColour, PaneColour);
        shell.Show();
        await shell.WaitForWebViewAsync();

        var dpi = VisualTreeHelper.GetDpi(shell);
        Console.WriteLine($"DPI scale        : {dpi.DpiScaleX:F2}x - "
            + "a non-integer scale, which is where alignment errors show");

        // Sample points: quadrant centres, plus pairs straddling each seam. The straddling pairs are
        // the alignment test - if the still frame is offset by even a couple of device pixels, a
        // point 6px inside one quadrant reads as its neighbour.
        var points = new List<(string Name, (int X, int Y) At)>
        {
            ("crimson centre  (25%,25%)", shell.CanvasPoint(0.25, 0.25)),
            ("blue centre     (75%,25%)", shell.CanvasPoint(0.75, 0.25)),
            ("amber centre    (25%,75%)", shell.CanvasPoint(0.25, 0.75)),
            ("violet centre   (75%,75%)", shell.CanvasPoint(0.75, 0.75)),
            ("left of v-seam  (49%,25%)", shell.CanvasPoint(0.49, 0.25)),
            ("right of v-seam (51%,25%)", shell.CanvasPoint(0.51, 0.25)),
            ("above h-seam    (25%,49%)", shell.CanvasPoint(0.25, 0.49)),
            ("below h-seam    (25%,51%)", shell.CanvasPoint(0.25, 0.51)),
        };

        Header("Q1 - LIVE: what does the real canvas show?");
        var live = SampleAll(shell, points, "live");
        if (live is null)
        {
            return 1;
        }

        Header("Q2 - SWAP: capture, hide the control, show the still frame");
        var swapped = await shell.SwapToSnapshotAsync();
        if (!swapped)
        {
            Console.WriteLine("  x   capture failed - the mechanism is unavailable.");
            Findings.Add("CapturePreviewAsync failed");
            return Report();
        }

        var canvas = shell.CanvasDeviceSize();
        Console.WriteLine($"  capture         : {shell.CaptureSize.Width:F0}x{shell.CaptureSize.Height:F0} px");
        Console.WriteLine($"  canvas (device) : {canvas.Width:F0}x{canvas.Height:F0} px");
        Console.WriteLine($"  capture latency : {shell.LastCaptureMs:F1} ms");
        Console.WriteLine($"  full swap       : {shell.LastSwapMs:F1} ms  "
            + "(includes the settle this harness forces, so it is an upper bound)");

        var sizeMatches = Math.Abs(shell.CaptureSize.Width - canvas.Width) <= 2
            && Math.Abs(shell.CaptureSize.Height - canvas.Height) <= 2;
        Console.WriteLine(sizeMatches
            ? "  OK  the capture is the canvas's device size - no resampling needed."
            : "  !   capture and canvas differ in size; WPF must rescale, which is where softness "
              + "comes from.");
        if (!sizeMatches)
        {
            Findings.Add(
                $"capture is {shell.CaptureSize.Width:F0}x{shell.CaptureSize.Height:F0} but the canvas "
                + $"is {canvas.Width:F0}x{canvas.Height:F0} device px - the still frame is rescaled");
        }

        Header("Q3 - SEAM: does the still frame match the live frame, pixel for pixel?");
        var still = SampleAll(shell, points, "snapshot");
        if (still is null)
        {
            return Report();
        }

        var mismatches = 0;
        Console.WriteLine();
        Console.WriteLine($"{"sample",-28}{"live",-12}{"snapshot",-12}  match");
        Console.WriteLine(new string('-', 66));
        for (var i = 0; i < points.Count; i++)
        {
            var a = Classify(live[i]);
            var b = Classify(still[i]);
            var ok = a == b;
            if (!ok)
            {
                mismatches++;
            }

            Console.WriteLine($"{points[i].Name,-28}{a,-12}{b,-12}  {(ok ? "yes" : "NO")}");
        }

        Console.WriteLine();
        if (mismatches == 0)
        {
            Console.WriteLine("  OK  every sample agrees, including all four seam-straddling pairs.");
            Console.WriteLine("      The still frame is aligned; there is no visible seam to see.");
        }
        else
        {
            Console.WriteLine($"  x   {mismatches} sample(s) disagree - the still frame is misplaced.");
            Findings.Add($"{mismatches} sample(s) differ between live and snapshot - visible seam");
        }

        Header("Q4 - THE POINT: can WPF now draw over the canvas?");
        shell.ShowOverlay(true);
        await shell.SettleAsync();

        var composited = Capture.Composited(shell);
        var overlayAt = shell.OverlayPoint();
        var overlayPixel = composited?.Dominant(overlayAt.X, overlayAt.Y) ?? ((byte)0, (byte)0, (byte)0);
        var overlayWins = Classify(overlayPixel) == "overlay";

        Console.WriteLine($"  overlay region  : {Capture.Hex(overlayPixel)} -> {Classify(overlayPixel)}");
        Console.WriteLine(overlayWins
            ? "  OK  the WPF overlay draws over the canvas while the snapshot stands in."
            : "  x   the overlay is STILL hidden - the swap does not solve airspace.");
        if (!overlayWins)
        {
            Findings.Add("WPF chrome is still occluded while the snapshot is shown");
        }

        shell.ShowOverlay(false);

        Header("Q5 - RESTORE: does the live control come back?");
        await shell.RestoreAsync();

        var restored = SampleAll(shell, points, "restored");
        var alive = shell.WebHost.CoreWebView2 is not null;
        Console.WriteLine($"  CoreWebView2 alive: {alive}");

        var restoredOk = restored is not null && !shell.ShowingSnapshot
            && restored.Select(Classify).SequenceEqual(live.Select(Classify));
        Console.WriteLine(restoredOk
            ? "  OK  the live canvas is back and identical to before the swap."
            : "  x   the canvas did not return to its pre-swap state.");
        if (!restoredOk)
        {
            Findings.Add("the live canvas did not return correctly after restore");
        }

        Header("Q6 - REPEAT: does swapping repeatedly leak or drift?");
        var latencies = new List<double>();
        for (var i = 0; i < 8; i++)
        {
            if (await shell.SwapToSnapshotAsync())
            {
                latencies.Add(shell.LastCaptureMs);
            }

            await shell.RestoreAsync();
        }

        if (latencies.Count > 0)
        {
            latencies.Sort();
            Console.WriteLine($"  8 swap/restore cycles - capture p50 {latencies[latencies.Count / 2]:F1} ms, "
                + $"min {latencies[0]:F1} ms, max {latencies[^1]:F1} ms");
            var final = SampleAll(shell, points, "after 8 cycles");
            var stable = final is not null && final.Select(Classify).SequenceEqual(live.Select(Classify));
            Console.WriteLine(stable
                ? "  OK  the canvas is unchanged after repeated swapping."
                : "  x   the canvas drifted after repeated swapping.");
            if (!stable)
            {
                Findings.Add("canvas state drifts after repeated swap/restore cycles");
            }
        }

        Header("Q7 - what focus API does the WPF control actually expose?");
        FocusApi.Report();

        Header("Q8 - does the Win32 route into the hosted browser work?");
        Win32Focus.Probe(shell.WebHost);

        shell.Close();
        return Report();
    }

    private static int Report()
    {
        Header("Verdict");
        if (Findings.Count == 0)
        {
            Console.WriteLine("The snapshot-swap mechanism holds on this host: the still frame is");
            Console.WriteLine("pixel-aligned, WPF draws over it, and the live canvas returns intact.");
            Console.WriteLine("The S4 decision (windowed control + snapshot swap) is supported.");
            return 0;
        }

        Console.WriteLine($"{Findings.Count} finding(s):");
        foreach (var finding in Findings)
        {
            Console.WriteLine($"  - {finding}");
        }

        return 0;
    }

    private static List<(byte R, byte G, byte B)>? SampleAll(
        Shell shell, List<(string Name, (int X, int Y) At)> points, string label)
    {
        var photo = Capture.Composited(shell);
        if (photo is null)
        {
            Console.WriteLine($"  x   could not capture the window for the {label} sample.");
            Findings.Add($"composited capture failed for the {label} sample");
            return null;
        }

        var samples = points.Select(p => photo.Dominant(p.At.X, p.At.Y, 4)).ToList();
        for (var i = 0; i < points.Count; i++)
        {
            Console.WriteLine($"  {points[i].Name,-28}{Capture.Hex(samples[i])}  -> {Classify(samples[i])}");
        }

        return samples;
    }

    private static string Classify((byte R, byte G, byte B) sample)
    {
        var candidates = Quadrants
            .Append(("overlay", (OverlayColour.R, OverlayColour.G, OverlayColour.B)))
            .Append(("pane", (PaneColour.R, PaneColour.G, PaneColour.B)))
            .ToArray();
        return Capture.Classify(sample, candidates);
    }

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 78));
    }
}
