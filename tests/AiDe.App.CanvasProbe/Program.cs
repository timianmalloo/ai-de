using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;
using AiDe.Core.Workbench;

namespace AiDe.App.CanvasProbe;

/// <summary>
/// <b>P2-FOCUS-03, out of process.</b> The keyboard-trap test: enter the canvas, press a real Tab
/// off the end, and require that focus comes back to WPF.
/// </summary>
/// <remarks>
/// <para><b>Why this is a separate process.</b> The canvas needs a real window with a real WebView2
/// in it, which a <c>dotnet test</c> host does not reliably provide — defect class <b>DC-014</b>,
/// and the same control the ConPTY case used. The route to the page went through two measured dead
/// ends first: a posted <c>WM_KEYDOWN</c> never reaches Chromium's key handling, and <c>SendInput</c>
/// delivers to the FOREGROUND window, which neither a test host nor a shell-launched probe can hold
/// — the page reported <c>activeElement="first"</c>, so focus HAD landed, while seeing <b>zero</b>
/// Tab keydowns. Keys now go in through the browser's own input layer.</para>
///
/// <para><b>The exit code is the assertion.</b> 0 = focus left the canvas. Anything else is a
/// keyboard trap or a broken environment, and each gets its own code so the failure is diagnosable
/// from the test that launched it rather than only from a log nobody reads.</para>
/// </remarks>
internal static class Program
{
    private const int Ok = 0;
    private const int CanvasNeverLoaded = 2;
    private const int FocusNeverEntered = 3;
    private const int PageNeverPostedLeave = 4;      // the trap
    private const int FocusDidNotReturnToWpf = 5;
    private const int Crashed = 6;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            // `--measure <width> <height> [nodes]` reports the stage against its pane at a given
            // window size, and the cost of one fit()+place() at that size. It is a MEASUREMENT mode,
            // not an assertion: it prints numbers and exits 0, because the numbers are the evidence
            // and a pass/fail here would be the probe deciding what the budget is. With no arguments
            // this is the P2-FOCUS-03 keyboard-trap probe, unchanged.
            return args switch
            {
                ["--measure", var w, var h, ..] => Measure(
                    int.Parse(w), int.Parse(h), args.Length > 3 ? int.Parse(args[3]) : 0, dock: false),

                // `--dock` sizes the Explorer itself rather than the window, because Windows clamps
                // a window to the work area and this machine's display is smaller than the operator's
                // — measured: a requested 3840x2160 window came back as a 1709x1047 DIP Explorer.
                ["--dock", var w, var h, ..] => Measure(
                    int.Parse(w), int.Parse(h), args.Length > 3 ? int.Parse(args[3]) : 0, dock: true),

                _ => Run(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Crashed;
        }
    }

    private static int Run()
    {
        var canvas = new CanvasSurface("canvas-probe", "Graph");

        // The canvas MUST be populated for this test to mean anything. With no graph the page takes
        // its empty-graph escape and leaves on the first Tab — which passes, while proving nothing
        // about tabbing off the END of a populated node list. That regression appeared the moment
        // navigation was added, and it appeared as a still-green test.
        canvas.GraphSource = (rootId, _) => Task.FromResult(new CanvasGraph(
            [
                new CanvasNode("Shop.Order", "Order", "source", IsRoot: true),
                new CanvasNode("Shop.Customer", "Customer", "source", IsRoot: false),
                new CanvasNode("Shop.Ledger", "Ledger", "source", IsRoot: false),
            ],
            [new CanvasEdge("Shop.Order", "Shop.Customer", "depends_on", "Verified")],
            "Shop.Order", 0, [], null, DeclaredByKind: null));
        var before = new Button { Content = "before" };
        var after = new Button { Content = "after" };

        var panel = new StackPanel();
        panel.Children.Add(before);
        panel.Children.Add(canvas);
        panel.Children.Add(after);

        var window = new Window
        {
            Title = "AiDe canvas probe",
            Content = panel,
            Width = 700,
            Height = 520,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        var result = Crashed;
        window.Loaded += async (_, _) =>
        {
            result = await ProbeAsync(window, canvas);
            window.Close();
        };

        window.Show();
        SetForegroundWindow(new WindowInteropHelper(window).Handle);

        var frame = new DispatcherFrame();
        window.Closed += (_, _) => frame.Continue = false;

        // A hung probe must not hang the suite that launched it.
        var guard = new DispatcherTimer(
            TimeSpan.FromSeconds(90), DispatcherPriority.Normal,
            (_, _) => { frame.Continue = false; }, Dispatcher.CurrentDispatcher);
        guard.Start();

        Dispatcher.PushFrame(frame);
        guard.Stop();
        canvas.Dispose();
        return result;
    }

    private static async Task<int> ProbeAsync(Window window, CanvasSurface canvas)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!canvas.Ready && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        if (!canvas.Ready)
        {
            Console.Error.WriteLine("the canvas page never finished loading");
            return CanvasNeverLoaded;
        }

        SetForegroundWindow(new WindowInteropHelper(window).Handle);
        await Task.Delay(400);

        var scope = new WpfHostFocusScope(window);
        var router = new CanvasFocusRouter(canvas.FocusTarget, scope);

        CanvasFocusDirection? reported = null;
        canvas.FocusLeaveRequested += (_, direction) => reported = direction;

        if (router.Enter().Outcome != CanvasFocusOutcome.Entered)
        {
            Console.Error.WriteLine("focus never entered the canvas");
            return FocusNeverEntered;
        }

        // Tab repeatedly: the page decides how many focusable nodes it has, and the contract is
        // "tabbing off the END leaves", not "the graph has three nodes".
        for (var attempt = 0; attempt < 12 && reported is null; attempt++)
        {
            if (!await canvas.SendKeyAsync("Tab", 0x09))
            {
                Console.Error.WriteLine("the browser refused the injected key");
                return CanvasNeverLoaded;
            }

            await Task.Delay(150);
        }

        var seen = await canvas.EvaluateAsync("String(window.__tabsSeen || 0)");
        var nodeCount = await canvas.EvaluateAsync("String(document.querySelectorAll('.node').length)");
        Console.Out.WriteLine($"tab keydowns seen by the page: {seen}, nodes rendered: {nodeCount}");

        // Non-vacuity: an empty canvas leaves on the FIRST Tab by design, which would pass this test
        // while proving nothing about the end of a node list.
        //
        // The failure string is checked FIRST, and separately, because `EvaluateAsync` swallows its
        // own exception and returns "(evaluate failed: …)" as an ordinary string
        // (CanvasSurface.EvaluateAsync). That is neither "0" nor "", so the count guard below waves it
        // through — a page that never loaded would report `nodes rendered: (evaluate failed: …)`
        // and this probe would carry on as though the canvas were full. A control that cannot tell
        // "nothing rendered" from "I could not ask" is the shape it exists to prevent (DC-016).
        if (seen.StartsWith("(evaluate failed", StringComparison.Ordinal)
            || nodeCount.StartsWith("(evaluate failed", StringComparison.Ordinal))
        {
            Console.Error.WriteLine(
                "the page could not be evaluated, so nothing below this line was measured: "
                + $"tabsSeen={seen}, nodeCount={nodeCount}");
            return CanvasNeverLoaded;
        }

        if (nodeCount.Trim('"') is "0" or "")
        {
            Console.Error.WriteLine("the canvas rendered no nodes — this test would pass vacuously");
            return CanvasNeverLoaded;
        }

        if (reported is null)
        {
            Console.Error.WriteLine(
                "the canvas page never posted focus.leave — a user who entered the graph could not " +
                "get out with the keyboard. This is the trap P2-FOCUS-03 exists to catch.");
            return PageNeverPostedLeave;
        }

        var outcome = router.Leave(reported.Value);
        if (outcome.Outcome != CanvasFocusOutcome.Moved || router.IsInsideCanvas)
        {
            Console.Error.WriteLine($"focus did not return to WPF: {outcome.Outcome}");
            return FocusDidNotReturnToWpf;
        }

        Console.Out.WriteLine($"focus left the canvas: {reported.Value}");
        return Ok;
    }

    /// <summary>
    /// <b>The P1 measurement</b> (Ruling 133's SRE condition, INV-0014 §1). Hosts the canvas in the
    /// real <see cref="ExplorerSurface"/> at a given window size and reports the stage's extent
    /// against its pane's — the numbers the investigation took off a screenshot, taken off the
    /// product instead. It prints and exits <see cref="Ok"/>: the numbers are the evidence, and a
    /// pass/fail here would be the probe deciding the budget.
    /// </summary>
    private static int Measure(int width, int height, int nodes, bool dock)
    {
        var canvas = new CanvasSurface("canvas-measure", "Graph");
        canvas.GraphSource = (_, _) => Task.FromResult(Graph(Math.Max(nodes, 3)));

        var explorer = new ExplorerSurface(canvas, new NodeReaderView());
        object content = explorer;

        if (dock)
        {
            // The Explorer is laid out at the requested size inside an unconstraining panel, so the
            // dock can be larger than this display. The windowed WebView2's own HWND is sized to the
            // element, so the page's viewport is the dock's — the part off-screen is clipped by the
            // window, not shrunk.
            explorer.Width = width;
            explorer.Height = height;
            var host = new Canvas();
            host.Children.Add(explorer);
            content = host;
        }

        var window = new Window
        {
            Title = "AiDe canvas measure",
            Content = content,
            Width = dock ? 1200 : width,
            Height = dock ? 800 : height,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = 0,
            Top = 0,
        };

        var result = Crashed;
        window.Loaded += async (_, _) =>
        {
            result = await MeasureAsync(window, canvas, explorer, width, height, nodes);
            window.Close();
        };

        window.Show();
        SetForegroundWindow(new WindowInteropHelper(window).Handle);

        var frame = new DispatcherFrame();
        window.Closed += (_, _) => frame.Continue = false;
        var guard = new DispatcherTimer(
            TimeSpan.FromSeconds(180), DispatcherPriority.Normal,
            (_, _) => { frame.Continue = false; }, Dispatcher.CurrentDispatcher);
        guard.Start();

        Dispatcher.PushFrame(frame);
        guard.Stop();
        canvas.Dispose();
        return result;
    }

    /// <summary>A graph of <paramref name="count"/> nodes, a third of the edges inferred.</summary>
    private static CanvasGraph Graph(int count)
    {
        var nodes = new List<CanvasNode>(count);
        var edges = new List<CanvasEdge>(Math.Max(count - 1, 0));
        for (var i = 0; i < count; i++)
        {
            nodes.Add(new CanvasNode($"Node.{i:D5}", $"Node {i}", "source", IsRoot: i == 0));
            if (i > 0)
            {
                edges.Add(new CanvasEdge(
                    $"Node.{i:D5}", $"Node.{i / 2:D5}", "depends_on", i % 3 == 0 ? "Inferred" : "Verified"));
            }
        }

        return new CanvasGraph(nodes, edges, "Node.00000", 0, [], null, DeclaredByKind: null);
    }

    private static async Task<int> MeasureAsync(
        Window window, CanvasSurface canvas, ExplorerSurface explorer, int width, int height, int nodes)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(90);
        while (!canvas.Ready && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        if (!canvas.Ready)
        {
            Console.Error.WriteLine("the canvas page never finished loading");
            return CanvasNeverLoaded;
        }

        // The graph is drawn after the page loads, so measuring straight away would report a stage
        // whose layout has not run. Non-vacuity: a measurement over zero nodes says nothing.
        var drawn = "0";
        while (DateTime.UtcNow < deadline)
        {
            drawn = (await canvas.EvaluateAsync("String(document.querySelectorAll('.node').length)")).Trim('"');
            if (drawn is not ("0" or "")) { break; }
            await Task.Delay(100);
        }

        if (drawn is "0" or "" || drawn.StartsWith("(evaluate failed", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"the canvas rendered no nodes ({drawn}) — nothing below would be measured");
            return CanvasNeverLoaded;
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        var page = await canvas.EvaluateAsync(
            "JSON.stringify({"
            + "stageH: document.getElementById('stage').clientHeight,"
            + "stageW: document.getElementById('stage').clientWidth,"
            + "viewportH: document.documentElement.clientHeight,"
            + "viewportW: document.documentElement.clientWidth,"
            + "bodyH: document.body.clientHeight,"
            + "dpr: window.devicePixelRatio,"
            + "nodes: document.querySelectorAll('.node').length})");

        Console.Out.WriteLine($"window {width}x{height} | layout {explorer.Layout} | dpiScale {dpi.DpiScaleX}");
        Console.Out.WriteLine(
            $"pane (WPF DIPs) {canvas.ActualWidth:F1}x{canvas.ActualHeight:F1} | "
            + $"explorer {explorer.ActualWidth:F1}x{explorer.ActualHeight:F1}");
        Console.Out.WriteLine($"page {page}");

        // RULING 141(a): P6's proof is rendered in this slot. The page string test can only see the
        // encoding; this reads what actually reached the SVG for a NON-JOIN edge of each class, so
        // an inferred edge cannot be rendered pixel-identical to an extracted one unnoticed.
        var provenance = await canvas.EvaluateAsync(
            "JSON.stringify([].slice.call(document.querySelectorAll('#edges line')).map(function (l) {"
            + " var t = l.querySelector('title');"
            + " return { title: t ? t.textContent : null, dash: l.getAttribute('stroke-dasharray'),"
            + " stroke: l.getAttribute('stroke') }; })"
            + ".filter(function (e, i, a) { return a.findIndex(function (x) { return x.title === e.title; }) === i; })"
            + ".slice(0, 6))");
        Console.Out.WriteLine($"edge provenance as rendered {provenance}");

        // The operator's actual complaint: not the stage, the DRAWN GRAPH. `fit()` scales to the
        // stage minus a 50px margin, so this is the number the stage's height decides.
        var drawnExtent = await canvas.EvaluateAsync(
            "(function(){ var s = document.getElementById('stage').getBoundingClientRect();"
            + " var ns = document.querySelectorAll('.node'); if (!ns.length) { return JSON.stringify({nodes:0}); }"
            + " var x0=1e9,y0=1e9,x1=-1e9,y1=-1e9;"
            + " for (var i=0;i<ns.length;i++){ var r = ns[i].getBoundingClientRect();"
            + " if (r.left-s.left<x0) x0=r.left-s.left; if (r.top-s.top<y0) y0=r.top-s.top;"
            + " if (r.right-s.left>x1) x1=r.right-s.left; if (r.bottom-s.top>y1) y1=r.bottom-s.top; }"
            + " return JSON.stringify({nodes:ns.length, drawnW:Math.round(x1-x0), drawnH:Math.round(y1-y0)}); })()");
        Console.Out.WriteLine($"drawn graph extent inside the stage {drawnExtent}");

        if (nodes >= 1000)
        {
            // The cost of the resize path itself: `fit()` re-frames and `place()` repositions, which
            // is exactly what the ResizeObserver runs on a splitter drag — never `layout2d`.
            var cost = await canvas.EvaluateAsync(
                "(function(){"
                + " if (typeof fit !== 'function' || typeof place !== 'function')"
                + " { return JSON.stringify({error:'fit/place are not reachable from the page scope'}); }"
                + " var t=[]; for (var i=0;i<21;i++){ var s=performance.now(); fit(); place(); t.push(performance.now()-s); }"
                + " t.sort(function(a,b){return a-b;});"
                + " return JSON.stringify({samples:t.length, medianMs:Math.round(t[10]*100)/100,"
                + " p90Ms:Math.round(t[18]*100)/100, maxMs:Math.round(t[20]*100)/100}); })()");
            Console.Out.WriteLine($"fit()+place() at {drawn} nodes {cost}");

            // And the frame the user actually sees: place() driven from requestAnimationFrame, so
            // the interval includes the browser's own style, layout and paint — not just the script.
            await canvas.EvaluateAsync(
                "(function(){ window.__frameResult = null; var d=[], last=performance.now(), n=0;"
                + " function step(){ var now=performance.now(); d.push(now-last); last=now; place();"
                + " if (++n < 41) { requestAnimationFrame(step); }"
                + " else { d.sort(function(a,b){return a-b;});"
                + " window.__frameResult = JSON.stringify({frames:d.length, medianMs:Math.round(d[20]*100)/100,"
                + " maxMs:Math.round(d[40]*100)/100}); } }"
                + " requestAnimationFrame(step); })()");

            var frames = "null";
            var until = DateTime.UtcNow + TimeSpan.FromSeconds(60);
            while (DateTime.UtcNow < until)
            {
                frames = (await canvas.EvaluateAsync("window.__frameResult")).Trim();
                if (frames is not ("null" or "")) { break; }
                await Task.Delay(100);
            }

            Console.Out.WriteLine($"rAF place() interval at {drawn} nodes {frames}");
        }

        return Ok;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
