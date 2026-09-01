using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
    private static int Main()
    {
        try
        {
            return Run();
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
        // (CanvasSurface.cs:267). That is neither "0" nor "", so the count guard below waves it
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

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
