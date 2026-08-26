using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Webview2AirspaceSpike;

/// <summary>
/// Spike S4 — does WebView2 compose with WPF focus, DPI and the docking layout?
/// </summary>
/// <remarks>
/// ADR-0008 chose a WPF shell hosting WebView2 for the graph canvas and recorded airspace as its
/// <b>reversal trigger</b>. ADR-0012 then put every surface inside an AvalonDock stack, so the real
/// question is narrower and harder than "does WebView2 work in WPF": does it work when the pane
/// hosting it can be floated, tabbed, hidden and resized by a docking library that knows nothing
/// about it.
///
/// Both hosting modes are measured by the SAME probes, because the useful output is the difference
/// between them. Everything is read from pixels and live state rather than from documentation — the
/// documented answer ("windowed hosting has airspace limitations") does not say what happens to
/// <i>our</i> layout, and as it turns out the interesting failure is not the documented one.
/// </remarks>
internal static class Program
{
    // Deliberately far apart in colour space so a sampled pixel is unambiguous.
    private static readonly Color WebColour = Color.FromRgb(0xE1, 0x1D, 0x48);      // crimson: web content
    private static readonly Color OverlayColour = Color.FromRgb(0x22, 0xC5, 0x5E);  // green: WPF on top
    private static readonly Color PaneColour = Color.FromRgb(0x1E, 0x29, 0x3B);     // slate: WPF beneath

    private static List<string> _findings = [];

    [STAThread]
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var exit = 0;

        // A probe that fails must REPORT, not abort the suite. WebView2CompositionControl raises its
        // failures from WPF's layout pass, where an unhandled exception ends the process and takes
        // every later measurement with it.
        application.DispatcherUnhandledException += (_, e) =>
        {
            Console.WriteLine($"  x dispatcher exception: {e.Exception.GetType().Name}: "
                + $"{e.Exception.Message.Trim()}");
            Console.WriteLine($"    origin: {Origin(e.Exception)}");
            _findings.Add($"dispatcher exception {e.Exception.GetType().Name}: "
                + $"{e.Exception.Message.Trim()} - at {Origin(e.Exception)}");
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

        var runtime = TryRuntimeVersion();
        Console.WriteLine($"WebView2 Runtime : {runtime ?? "NOT FOUND"}");
        if (runtime is null)
        {
            Console.WriteLine("Cannot proceed: the evergreen runtime is not installed.");
            return 1;
        }

        var results = new Dictionary<Shell.Hosting, List<string>>();

        foreach (var hosting in new[] { Shell.Hosting.Windowed, Shell.Hosting.Composition })
        {
            _findings = [];
            Header($"==== HOSTING MODE: {hosting} ====");
            Console.WriteLine(hosting == Shell.Hosting.Windowed
                ? "The default `WebView2` control - a child HWND composited by Windows."
                : "`WebView2CompositionControl` - rendered into the WPF visual tree.");

            Shell shell;
            try
            {
                shell = new Shell(WebColour, OverlayColour, PaneColour, hosting);
                shell.Show();
                await shell.WaitForWebViewAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  x could not create the {hosting} shell: "
                    + $"{ex.GetType().Name}: {ex.Message.Trim()}");
                _findings.Add($"{hosting} shell could not be created: {ex.GetType().Name}");
                results[hosting] = _findings;
                continue;
            }

            var dpi = VisualTreeHelper.GetDpi(shell);
            Console.WriteLine($"DPI scale        : {dpi.DpiScaleX:F2}x ({dpi.PixelsPerInchX} DPI)");

            Header($"[{hosting}] Q1 - AIRSPACE: does a WPF element render ON TOP of WebView2?");
            await ProbeAsync("Q1 airspace", () => MeasureAirspaceAsync(shell));

            Header($"[{hosting}] Q3 - does it survive being hidden behind a TAB and brought back?");
            await ProbeAsync("Q3 tab", () => MeasureTabAsync(shell));

            Header($"[{hosting}] Q4 - does keyboard focus cross the WPF / WebView2 boundary?");
            await ProbeAsync("Q4 focus", () => MeasureFocusAsync(shell));

            Header($"[{hosting}] Q5 - does it resize with its pane?");
            await ProbeAsync("Q5 resize", () => MeasureResizeAsync(shell));

            // FLOAT RUNS LAST, on purpose. In Composition mode it ends the process with a native
            // access violation that no handler can catch, so anything sequenced after it would never
            // be measured at all. Putting the destructive probe last is what makes the other four
            // comparable across both hosting modes.
            Header($"[{hosting}] Q2 - does it survive being FLOATED and re-docked?   (DESTRUCTIVE)");
            await ProbeAsync("Q2 float", () => MeasureFloatAsync(shell));

            results[hosting] = _findings;

            try
            {
                shell.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  (close threw {ex.GetType().Name})");
            }

            await Task.Delay(400);
        }

        Header("Verdict");
        foreach (var (hosting, findings) in results)
        {
            Console.WriteLine();
            Console.WriteLine($"{hosting}: "
                + (findings.Count == 0 ? "no blocking findings" : $"{findings.Count} finding(s)"));
            foreach (var finding in findings)
            {
                Console.WriteLine($"  - {finding}");
            }
        }

        Console.WriteLine();
        var windowedAirspace = Has(results, Shell.Hosting.Windowed, "AIRSPACE");
        var compositionAirspace = Has(results, Shell.Hosting.Composition, "AIRSPACE");

        if (windowedAirspace && !compositionAirspace)
        {
            Console.WriteLine("RESULT: ADR-0008's reversal trigger IS met by the DEFAULT control.");
            Console.WriteLine("  WebView2CompositionControl removes the airspace limitation - but read its");
            Console.WriteLine("  own findings above before treating that as the fix.");
        }
        else if (windowedAirspace)
        {
            Console.WriteLine("RESULT: airspace is present in BOTH hosting modes; ADR-0008 needs revisiting.");
        }
        else
        {
            Console.WriteLine("RESULT: no airspace problem observed in either mode on this host.");
        }

        return 0;
    }

    private static bool Has(
        Dictionary<Shell.Hosting, List<string>> results, Shell.Hosting hosting, string prefix) =>
        results.TryGetValue(hosting, out var findings)
        && findings.Any(f => f.StartsWith(prefix, StringComparison.Ordinal));

    private static async Task MeasureAirspaceAsync(Shell shell)
    {
        shell.ShowOverlay(true);
        await shell.SettleAsync();

        var wpf = Capture.WpfVisualTree(shell);
        var composited = Capture.Composited(shell);
        if (composited is null)
        {
            Console.WriteLine("  x could not capture the composited window.");
            _findings.Add("composited capture failed; airspace unmeasured");
            return;
        }

        // Two DISTINCT points: the overlay's own centre, and a point in the web pane's bottom-right
        // quadrant that the overlay provably does not cover. Sampling one point for both was the
        // harness bug on the first run of this spike.
        var web = shell.WebOnlyPoint();
        var overlay = shell.DevicePointIn(shell.Overlay);

        Console.WriteLine($"  sample: web-only   @ {web.X},{web.Y}");
        Console.WriteLine($"  sample: overlay    @ {overlay.X},{overlay.Y}"
            + (web == overlay ? "   ** SAME POINT - harness broken **" : string.Empty));
        Console.WriteLine();

        if (web == overlay)
        {
            _findings.Add("harness: sample points coincided; airspace unmeasured");
            shell.ShowOverlay(false);
            return;
        }

        var webInWpf = wpf.Dominant(web.X, web.Y);
        var webInComposited = composited.Dominant(web.X, web.Y);
        var overlayInComposited = composited.Dominant(overlay.X, overlay.Y);

        Console.WriteLine($"  web area, WPF visual tree only : {Capture.Hex(webInWpf)}"
            + $"  -> {Surface(webInWpf)}");
        Console.WriteLine($"  web area, composited           : {Capture.Hex(webInComposited)}"
            + $"  -> {Surface(webInComposited)}   [{Margins(webInComposited)}]");
        Console.WriteLine($"  overlay area, composited       : {Capture.Hex(overlayInComposited)}"
            + $"  -> {Surface(overlayInComposited)}   [{Margins(overlayInComposited)}]");
        Console.WriteLine();

        var webRenders = Surface(webInComposited) == "web";
        Console.WriteLine(webRenders
            ? "  OK  WebView2 renders inside the docked AvalonDock pane."
            : "  x   WebView2 did NOT render its content in the pane.");
        if (!webRenders)
        {
            _findings.Add("WebView2 did not render inside the AvalonDock pane");
        }

        switch (Surface(overlayInComposited))
        {
            case "overlay":
                Console.WriteLine("  OK  NO AIRSPACE PROBLEM - the WPF overlay draws over the WebView2.");
                break;
            case "web":
                Console.WriteLine("  x   AIRSPACE CONFIRMED - the WebView2 draws over the WPF overlay,");
                Console.WriteLine("      though the overlay is later in z-order in the same Grid cell.");
                _findings.Add(
                    "AIRSPACE: WPF content cannot render above WebView2. Any popup, menu, drag "
                    + "adorner, tooltip or dialog overlapping the graph canvas is hidden by it - "
                    + "ADR-0008's recorded reversal trigger.");
                break;
            default:
                Console.WriteLine("  ?   the overlay region shows neither surface - inconclusive.");
                _findings.Add("airspace inconclusive; overlay region matched neither surface");
                break;
        }

        shell.ShowOverlay(false);
    }

    private static async Task MeasureFloatAsync(Shell shell)
    {
        Console.WriteLine($"  before float : CoreWebView2 alive = {shell.CoreWebView2Alive}");

        shell.FloatWebPane();
        await shell.SettleAsync();

        Console.WriteLine($"  floated      : CoreWebView2 alive = {shell.CoreWebView2Alive}, "
            + $"size {shell.WebHost.ActualWidth:F0}x{shell.WebHost.ActualHeight:F0}");
        var floatingAlive = shell.CoreWebView2Alive;

        shell.DockWebPane();
        await shell.SettleAsync();

        var dockedAlive = shell.CoreWebView2Alive;
        Console.WriteLine($"  re-docked    : CoreWebView2 alive = {dockedAlive}, "
            + $"size {shell.WebHost.ActualWidth:F0}x{shell.WebHost.ActualHeight:F0}");

        if (floatingAlive && dockedAlive)
        {
            Console.WriteLine("  OK  Survives float and re-dock without losing its browser process.");
        }
        else
        {
            Console.WriteLine("  x   The CoreWebView2 was torn down by a layout change.");
            _findings.Add("WebView2 loses its CoreWebView2 when its pane is floated or re-docked");
        }
    }

    private static async Task MeasureTabAsync(Shell shell)
    {
        shell.SelectOtherTab();
        await shell.SettleAsync();
        Console.WriteLine($"  hidden tab   : CoreWebView2 alive = {shell.CoreWebView2Alive}, "
            + $"IsVisible = {shell.WebHost.IsVisible}");

        shell.SelectWebTab();
        await shell.SettleAsync();

        var restored = Capture.Composited(shell);
        var point = shell.WebOnlyPoint();
        var colour = restored?.Dominant(point.X, point.Y) ?? ((byte)0, (byte)0, (byte)0);
        var rendersAgain = restored is not null && Surface(colour) == "web";

        Console.WriteLine($"  restored tab : CoreWebView2 alive = {shell.CoreWebView2Alive}, "
            + $"pixel {Capture.Hex(colour)} -> {Surface(colour)}");
        Console.WriteLine(rendersAgain
            ? "  OK  Renders correctly after being hidden and restored."
            : "  x   Did not render after being restored from a background tab.");
        if (!rendersAgain)
        {
            _findings.Add("WebView2 does not repaint after its tab is reselected");
        }
    }

    private static async Task MeasureFocusAsync(Shell shell)
    {
        shell.TextBox.Focus();
        await shell.SettleAsync();
        var start = Keyboard.FocusedElement?.GetType().Name ?? "none";

        shell.TextBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        await shell.SettleAsync();
        var afterTab = Keyboard.FocusedElement?.GetType().Name ?? "none";

        var reached = afterTab.Contains("WebView2", StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($"  focus start          : {start}");
        Console.WriteLine($"  after MoveFocus(Next): {afterTab}");
        Console.WriteLine($"  IsTabStop = {KeyboardNavigation.GetIsTabStop(shell.WebHost)}, "
            + $"Focusable = {shell.WebHost.Focusable}, base = {shell.WebHost.GetType().BaseType?.Name}");
        Console.WriteLine(reached
            ? "  OK  Tab traversal reaches the WebView2 from WPF."
            : "  ->  Tab traversal did not land on the WebView2 in one step.");

        var accepted = shell.WebHost.Focus();
        await shell.SettleAsync();
        Console.WriteLine($"  explicit Focus() accepted: {accepted}");
        if (!accepted)
        {
            _findings.Add(
                "WPF Focus() is refused by the WebView2 host, so the canvas cannot be focused "
                + "programmatically - a command-palette or shortcut route into the graph needs its "
                + "own mechanism");
        }

        shell.TextBox.Focus();
        await shell.SettleAsync();
        var returned = Keyboard.FocusedElement == shell.TextBox;
        Console.WriteLine(returned
            ? "  OK  Focus can be taken back out by WPF."
            : "  x   WPF could not reclaim focus - a keyboard trap.");
        if (!returned)
        {
            _findings.Add("focus cannot be moved back out of the WebView2 by WPF (keyboard trap)");
        }
    }

    private static async Task MeasureResizeAsync(Shell shell)
    {
        var before = (shell.WebHost.ActualWidth, shell.WebHost.ActualHeight);
        shell.ResizeWebPane(0.75);
        await shell.SettleAsync();
        var after = (shell.WebHost.ActualWidth, shell.WebHost.ActualHeight);

        Console.WriteLine($"  pane before  : {before.Item1:F0}x{before.Item2:F0}");
        Console.WriteLine($"  pane after   : {after.Item1:F0}x{after.Item2:F0}");

        var moved = Math.Abs(after.Item1 - before.Item1) > 1 || Math.Abs(after.Item2 - before.Item2) > 1;
        Console.WriteLine(moved
            ? "  OK  The WebView2 tracks its pane's size through a docking resize."
            : "  x   The WebView2 did not follow the pane resize.");
        if (!moved)
        {
            _findings.Add("WebView2 does not resize with its docking pane");
        }

        var composited = Capture.Composited(shell);
        var point = shell.WebOnlyPoint();
        var colour = composited?.Dominant(point.X, point.Y) ?? ((byte)0, (byte)0, (byte)0);
        var stillRenders = composited is not null && Surface(colour) == "web";
        Console.WriteLine($"  after resize, sampled {Capture.Hex(colour)} -> {Surface(colour)} - "
            + (stillRenders ? "still rendering." : "NOT rendering."));
        if (!stillRenders)
        {
            _findings.Add("WebView2 stops rendering after a pane resize");
        }
    }

    /// <summary>Runs one probe so that its failure becomes a finding instead of ending the run.</summary>
    private static async Task ProbeAsync(string name, Func<Task> probe)
    {
        try
        {
            await probe();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  x {name} THREW {ex.GetType().Name}: {ex.Message.Trim()}");
            Console.WriteLine($"    origin: {Origin(ex)}");
            _findings.Add($"{name} threw {ex.GetType().Name}: {ex.Message.Trim()} - at {Origin(ex)}");
        }
    }

    /// <summary>The first stack frame that is ours or a dependency's - the useful line.</summary>
    private static string Origin(Exception ex) =>
        (ex.StackTrace ?? string.Empty)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.Contains("WebView2", StringComparison.Ordinal)
                || l.Contains("AvalonDock", StringComparison.Ordinal))
        ?? "unknown";

    private static string? TryRuntimeVersion()
    {
        try
        {
            return Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .GetAvailableBrowserVersionString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  (version probe threw {ex.GetType().Name})");
            return null;
        }
    }

    private static (byte R, byte G, byte B) ToRgb(Color c) => (c.R, c.G, c.B);

    /// <summary>Which of the three known surfaces a sampled pixel belongs to.</summary>
    private static string Surface((byte R, byte G, byte B) sample) => Capture.Classify(
        sample, ("web", ToRgb(WebColour)), ("overlay", ToRgb(OverlayColour)), ("pane", ToRgb(PaneColour)));

    private static string Margins((byte R, byte G, byte B) sample) => Capture.Margins(
        sample, ("web", ToRgb(WebColour)), ("overlay", ToRgb(OverlayColour)), ("pane", ToRgb(PaneColour)));

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 78));
    }
}
