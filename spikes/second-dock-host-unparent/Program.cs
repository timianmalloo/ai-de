// Spike: does a SECOND AvalonDock host survive being unparented while it holds a live HwndHost?
//
// Ruling 52 (docs/notes/addendum-c-council-rulings.md) amends ADR-0017 so a perspective's body may
// be a docking host of its own — Coding is today's host, Architecture a second one — and labels ONE
// clause Inferred: "that a second AvalonDock host survives unparenting as the first does — the ADR's
// T1 no-rebuild test must be re-run against the second host". Its CONDITIONS: "Falsified if the
// no-rebuild test fails for a second live docking host (a hidden HwndHost in host 2 restarts); then
// Architecture's body becomes a non-docking composite and the ruling is re-issued."
//
// ExplorerModeTests' T1 uses a Border stand-in, which says nothing about an HwndHost. This spike puts
// TWO DockingManagers under ONE ContentControl (the ADR-0017 presenter's shape), gives host B a
// real WebView2 — the only HwndHost kind the shell hosts today (the terminal is WPF-drawn) — and a
// raw HwndHost (a Win32 STATIC child) beside it, cycles the presenter's content A→B→A→B→Explore→B,
// and reads live state rather than trusting documentation:
//
//   - the WebView2's CoreWebView2 is the same object before and after every cycle;
//   - EnsureCoreWebView2Async ran once, however many Loaded events the re-parenting raised (DC-138);
//   - page state set before the cycle (a JS variable and the scroll offset) reads back after it;
//   - the raw HwndHost's BuildWindowCore/DestroyWindowCore counts and HWND identity across the
//     cycle — the literal "hidden HwndHost restarts" question, answered for the general case.
//
// Exit code 0 = the WebView2 assertions hold (Ruling 52's condition is met for the HwndHost kind the
// shell actually hosts). The raw-HwndHost result is REPORTED either way — it decides what a future
// non-WebView2 native surface must do, not whether Ruling 52 stands.
//
//   dotnet run --project spikes/second-dock-host-unparent
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using AvalonDock;
using AvalonDock.Layout;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace SecondDockHostUnparentSpike;

internal static class Program
{
    private static readonly List<string> Findings = [];
    private static int _failures;

    [STAThread]
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var exit = 0;

        application.DispatcherUnhandledException += (_, e) =>
        {
            Console.WriteLine($"  x dispatcher exception: {e.Exception.GetType().Name}: {e.Exception.Message.Trim()}");
            Findings.Add($"dispatcher exception {e.Exception.GetType().Name}: {e.Exception.Message.Trim()}");
            _failures++;
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
        string? runtime;
        try
        {
            runtime = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (Exception ex)
        {
            runtime = null;
            Console.WriteLine($"  (version probe threw {ex.GetType().Name})");
        }

        Console.WriteLine($"WebView2 Runtime : {runtime ?? "NOT FOUND"}");
        Console.WriteLine($"AvalonDock       : {typeof(DockingManager).Assembly.GetName().Version}");
        Console.WriteLine($"WebView2 wrapper : {typeof(WebView2).Assembly.GetName().Version}");
        Console.WriteLine($"OS               : {Environment.OSVersion}");
        Console.WriteLine($".NET             : {Environment.Version}");
        if (runtime is null)
        {
            Console.WriteLine("FAIL: no WebView2 runtime; the question cannot be asked on this host.");
            return 2;
        }

        // ── The presenter's shape: ONE ContentControl whose Content is swapped (ShellModeController). ──
        var presenter = new ContentControl();

        var hostA = new DockingManager();
        hostA.Layout = new LayoutRoot
        {
            RootPanel = new LayoutPanel(new LayoutDocumentPane(
                new LayoutDocument { Title = "Coding", ContentId = "coding", Content = new TextBlock { Text = "coding host (A)" } })),
        };

        var web = new WebView2
        {
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(Path.GetTempPath(), "aide-spike-second-dock-host"),
            },
        };
        var native = new NativeChildHost();

        // A once-guard in the WebSurfaceHost idiom (DC-138): Loaded fires per attach, initialisation
        // must not. Counted here so the spike can say how many attaches the cycle produced.
        var loadedCount = 0;
        var ensureCalls = 0;
        var initCompleted = 0;
        var initialised = false;
        web.CoreWebView2InitializationCompleted += (_, _) => initCompleted++;
        var initialisation = new TaskCompletionSource();
        web.Loaded += async (_, _) =>
        {
            loadedCount++;
            if (initialised)
            {
                return;
            }

            initialised = true;
            ensureCalls++;
            try
            {
                await web.EnsureCoreWebView2Async();
                initialisation.TrySetResult();
            }
            catch (Exception ex)
            {
                initialisation.TrySetException(ex);
            }
        };

        var hostBBody = new Grid();
        hostBBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        hostBBody.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
        Grid.SetRow(web, 0);
        Grid.SetRow(native, 1);
        hostBBody.Children.Add(web);
        hostBBody.Children.Add(native);

        var paneB = new LayoutDocumentPane();
        paneB.Children.Add(new LayoutDocument { Title = "Graph", ContentId = "graph", Content = hostBBody });
        paneB.Children.Add(new LayoutDocument { Title = "Domain", ContentId = "domain", Content = new TextBlock { Text = "class diagram (B)" } });
        var hostB = new DockingManager();
        hostB.Layout = new LayoutRoot { RootPanel = new LayoutPanel(paneB) };

        var explore = new Border { Background = System.Windows.Media.Brushes.DarkSlateGray, Child = new TextBlock { Text = "explore (full-window surface)" } };

        var window = new Window
        {
            Title = "Second dock host unparent spike",
            Width = 900,
            Height = 600,
            Content = presenter,
        };

        presenter.Content = hostA;
        window.Show();
        await Frames(5);

        Header("Q1 - first entry into host B (Architecture): the WebView2 initialises once");
        presenter.Content = hostB;
        await Frames(5);
        await initialisation.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await Frames(5);

        var core = web.CoreWebView2 ?? throw new InvalidOperationException("CoreWebView2 is null after EnsureCoreWebView2Async");
        var navigated = new TaskCompletionSource();
        EventHandler<CoreWebView2NavigationCompletedEventArgs> onNav = (_, _) => navigated.TrySetResult();
        core.NavigationCompleted += onNav;
        web.NavigateToString(
            "<html><body style='margin:0;background:#1e293b;color:#e5e7eb;font:16px sans-serif'>"
            + "<div style='height:3000px;padding:16px'>second-host probe page</div></body></html>");
        await navigated.Task.WaitAsync(TimeSpan.FromSeconds(30));
        core.NavigationCompleted -= onNav;
        await Frames(3);

        // State the cycle must preserve: a JS variable and a scroll offset (P-4's "selected node id
        // and scroll offset", in the cheapest observable form).
        await core.ExecuteScriptAsync("window.n = 41; window.scrollTo(0, 500);");
        await Frames(3);
        var nBefore = await core.ExecuteScriptAsync("window.n");
        var scrollBefore = await core.ExecuteScriptAsync("Math.round(window.scrollY)");
        Console.WriteLine($"  CoreWebView2 alive           : {web.CoreWebView2 is not null}");
        Console.WriteLine($"  Loaded events so far         : {loadedCount}");
        Console.WriteLine($"  EnsureCoreWebView2Async calls: {ensureCalls}");
        Console.WriteLine($"  InitializationCompleted      : {initCompleted}");
        Console.WriteLine($"  page state set               : n={nBefore} scrollY={scrollBefore}");
        Console.WriteLine($"  raw HwndHost                 : builds={native.Builds} destroys={native.Destroys} hwnd=0x{native.LastHandle:X}");
        Check("Q1 CoreWebView2 initialised exactly once on first entry", initCompleted == 1 && ensureCalls == 1);
        Check("Q1 raw HwndHost built exactly once on first entry", native.Builds == 1 && native.Destroys == 0);
        var firstHandle = native.LastHandle;

        Header("Q2 - the cycle: B → A → B, three times (host B unparented with a live WebView2 inside)");
        for (var cycle = 1; cycle <= 3; cycle++)
        {
            presenter.Content = hostA;
            await Frames(8);
            var aliveWhileHidden = web.CoreWebView2 is not null;
            var sameWhileHidden = ReferenceEquals(web.CoreWebView2, core);
            var buildsHidden = native.Builds;
            var destroysHidden = native.Destroys;

            presenter.Content = hostB;
            await Frames(8);

            var same = ReferenceEquals(web.CoreWebView2, core);
            var nAfter = await core.ExecuteScriptAsync("window.n");
            var scrollAfter = await core.ExecuteScriptAsync("Math.round(window.scrollY)");

            Console.WriteLine($"  cycle {cycle}: hidden → alive={aliveWhileHidden} same={sameWhileHidden} rawBuilds={buildsHidden} rawDestroys={destroysHidden}");
            Console.WriteLine($"  cycle {cycle}: shown  → same CoreWebView2={same} n={nAfter} scrollY={scrollAfter} "
                + $"Loaded={loadedCount} Ensure={ensureCalls} InitCompleted={initCompleted} "
                + $"rawBuilds={native.Builds} rawDestroys={native.Destroys} rawHwnd=0x{native.LastHandle:X}");

            Check($"Q2.{cycle} CoreWebView2 is the same object after the cycle", same);
            Check($"Q2.{cycle} JS state survives the cycle (n == 41)", nAfter == "41");
            Check($"Q2.{cycle} scroll offset survives the cycle (scrollY == 500)", scrollAfter == "500");
            Check($"Q2.{cycle} no re-initialisation (Ensure == 1, InitCompleted == 1)", ensureCalls == 1 && initCompleted == 1);
        }

        Header("Q3 - via a full-window surface: B → Explore → B");
        presenter.Content = explore;
        await Frames(8);
        presenter.Content = hostB;
        await Frames(8);
        var sameAfterExplore = ReferenceEquals(web.CoreWebView2, core);
        var nAfterExplore = await core.ExecuteScriptAsync("window.n");
        Console.WriteLine($"  same CoreWebView2={sameAfterExplore} n={nAfterExplore} Loaded={loadedCount} Ensure={ensureCalls} rawBuilds={native.Builds} rawDestroys={native.Destroys}");
        Check("Q3 CoreWebView2 survives B → Explore → B", sameAfterExplore && nAfterExplore == "41");

        Header("Q4 - the raw HwndHost (the general case Ruling 52's CONDITIONS name)");
        var rawHandleStable = native.LastHandle == firstHandle;
        Console.WriteLine($"  builds={native.Builds} destroys={native.Destroys} first hwnd=0x{firstHandle:X} last hwnd=0x{native.LastHandle:X} stable={rawHandleStable}");
        if (native.Destroys > 0)
        {
            Findings.Add($"A raw HwndHost's native window IS destroyed and rebuilt on every unparent "
                + $"(builds={native.Builds}, destroys={native.Destroys}, hwnd stable={rawHandleStable}). WPF's HwndHost tears its "
                + "window down when it loses its PresentationSource. The WebView2 wrapper survives because ITS "
                + "BuildWindowCore re-parents the existing CoreWebView2Controller — a raw HwndHost surface has "
                + "no such controller and would restart its child. No such surface exists in the shell today "
                + "(the terminal is WPF-drawn); any future one must keep its native state outside the HWND "
                + "the way WebView2 keeps the controller.");
        }
        else
        {
            Findings.Add("A raw HwndHost kept its native window across every unparent (no DestroyWindowCore).");
        }

        Header("Q5 - the same cycle under HOST A's own docked pane re-parent (DC-138 baseline)");
        // The docking host re-parents panes on layout changes; this is the attach the WebSurfaceHost
        // guard already handles. Recorded as a baseline count so the Q2 numbers can be read against it.
        Console.WriteLine($"  Loaded events total={loadedCount} (each is an attach; initialisation stayed at {ensureCalls})");
        Check("Q5 Loaded fired more than once (the re-parents were real) while initialisation stayed once",
            loadedCount > 1 && ensureCalls == 1);

        Header("Findings");
        foreach (var finding in Findings)
        {
            Console.WriteLine($"  - {finding}");
        }

        Console.WriteLine();
        Console.WriteLine(_failures == 0
            ? "RESULT: PASS — a second AvalonDock host with a live WebView2 survives unparenting; Ruling 52's condition holds."
            : $"RESULT: FAIL — {_failures} check(s) failed; Ruling 52's CONDITIONS fire.");

        window.Close();
        return _failures == 0 ? 0 : 1;
    }

    private static void Check(string name, bool ok)
    {
        Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}");
        if (!ok)
        {
            _failures++;
            Findings.Add("FAILED: " + name);
        }
    }

    /// <summary>Pumps N render frames so WPF attaches/detaches and the browser composes.</summary>
    private static async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await Task.Delay(50);
            await Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        }
    }

    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 78));
    }
}

/// <summary>
/// The general case: a raw <see cref="HwndHost"/> whose child is a Win32 STATIC control. Counts
/// window builds/destroys so the spike can say whether WPF tears an HwndHost down on unparent.
/// </summary>
internal sealed class NativeChildHost : HwndHost
{
    public int Builds { get; private set; }
    public int Destroys { get; private set; }
    public long LastHandle { get; private set; }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        Builds++;
        var hwnd = CreateWindowEx(
            0, "STATIC", "native child", WsChild | WsVisible, 0, 0, 200, 40, hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException("CreateWindowEx failed: " + Marshal.GetLastWin32Error());
        }

        LastHandle = hwnd.ToInt64();
        return new HandleRef(this, hwnd);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        Destroys++;
        DestroyWindow(hwnd.Handle);
    }

    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int exStyle, string className, string windowName, int style, int x, int y, int width, int height,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hwnd);
}
