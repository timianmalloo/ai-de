using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.ComposerProbe;

/// <summary>
/// Security <b>C10</b> and <b>C12</b>, in the only place they can be answered: a real
/// <see cref="WebView2"/> loading the real composer page.
/// </summary>
/// <remarks>
/// <para><b>What cannot be asserted in process, and why this exists.</b> Whether the three control
/// settings are really off, whether a page can change the control's document, and whether the editor
/// still RENDERS under <c>default-src 'none'; script-src 'self'</c> are all facts about the browser,
/// not about our code. C12 says as much in the plan: <c>style-src 'unsafe-inline'</c> is
/// <b>Inferred</b> to be required by the editor's style injection, so <b>the probe settles it rather
/// than Security's say-so</b>.</para>
///
/// <para><b>The exit code is the assertion</b>, and each failure has its own so the test that
/// launched this can say what went wrong without reading a log.</para>
/// </remarks>
internal static class Program
{
    private const int Ok = 0;
    private const int PageNeverLoaded = 2;
    private const int ModuleNeverExecuted = 3;
    private const int EditorNeverRenderedUnderTheCsp = 4;
    private const int SettingsFloorNotApplied = 5;
    private const int TheDocumentCouldBeChanged = 6;
    private const int HostObjectsReachable = 7;
    private const int NetworkReachable = 8;
    private const int PasteDidNotFence = 9;
    private const int Crashed = 10;
    private const int CouldNotEvaluate = 11;

    // --handshake only. Each is a DIFFERENT way the one-init-per-mount contract breaks, so the test
    // that launched this can say which without reading a log.
    private const int ThePageNeverMounted = 12;
    private const int TheInitCountWasNotOne = 13;
    private const int TheFieldValueDidNotSurvive = 14;

    // --shell only. The product's own New Session choreography, in the product's own docking host.
    private const int TheShellNeverShowedTheComposer = 20;
    private const int TheShellPageNeverMounted = 21;
    private const int TheShellRenderedNoEditor = 22;
    private const int TypingDidNotReachTheDraft = 23;
    private const int TheEditorHasNoSize = 24;
    private const int TheLaterRenderResetThePage = 25;
    private const int TheReloadLostThePage = 26;
    private const int TheEscapeResetTheHandshake = 27;

    /// <summary>Runs the handshake oracle instead of the CSP/one-document probe.</summary>
    private const string HandshakeArgument = "--handshake";

    /// <summary>Runs the shell-choreography typing oracle.</summary>
    private const string ShellArgument = "--shell";

    private static readonly List<string> Cancelled = [];

    [STAThread]
    private static int Main(string[] args)
    {
        // THE WORKBENCH LOG, ON STDOUT, IN EVERY MODE. The surfaces emit their bounds, handshake
        // transitions and input counts on the normal path (DC-136/DC-137); the test that launched
        // this reads them from here, and nothing a probe does lands in the operator's own log file
        // (INV-0007 F6: a test run and an operator's session were sharing one file).
        WorkbenchDiagnostics.Sink = line => Console.Out.WriteLine("diag: " + line);

        try
        {
            if (args is not null && args.Contains(ShellArgument, StringComparer.Ordinal))
            {
                return ShellTyping.Run(args!);
            }

            return args is not null && args.Contains(HandshakeArgument, StringComparer.Ordinal)
                ? Handshake.Run()
                : Run();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return Crashed;
        }
    }

    /// <summary>
    /// <b>Condition (d):</b> exactly one <c>host.init</c> per mount after <c>Configure</c>, and field
    /// values survive it — measured against the <b>shipped</b> <see cref="ComposerSurface"/>, not a
    /// hand-rolled host beside it.
    /// </summary>
    /// <remarks>
    /// <para><b>Why the real surface.</b> The defect was in the handshake between that control and
    /// that page: the page posted <c>editor.ready</c> only from inside its own <c>host.init</c>
    /// branch, and nothing pushed a first init, so the page never mounted at all. A probe that posts
    /// the init itself — which the CSP probe above does, deliberately, because its question is about
    /// the browser — cannot see that, because it IS the missing push (DC-016).</para>
    ///
    /// <para><b>Both orders, because both happen.</b> The shell configures a document it has just
    /// opened while WebView2 is still starting, and it can equally configure one whose page mounted
    /// first. Each is run as its own window, and the count must be one in both.</para>
    /// </remarks>
    private static class Handshake
    {
        private const string SeededGoal = "Seeded before the page mounted. It must still be here.";

        internal static int Run()
        {
            foreach (var configureBeforeShow in new[] { true, false })
            {
                var outcome = Once(configureBeforeShow);
                if (outcome != Ok)
                {
                    return outcome;
                }
            }

            Console.Out.WriteLine("the composer handshake pushed exactly one host.init per mount, both orders");
            return Ok;
        }

        private static int Once(bool configureBeforeShow)
        {
            var surface = new ComposerSurface("composer:probe", "probe — composer");
            var window = new Window
            {
                Title = "AiDe composer handshake probe",
                Content = surface,
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            // THE DRAFT IS WRITTEN BEFORE ANYTHING IS PUSHED. This is the value the second render
            // used to wipe: PushInit sent `value = ""` for every field, so a form rebuilt after the
            // operator had typed came back empty.
            surface.Draft.SwitchTo(ComposerShape.GoalBlock);
            surface.Draft.SetGoalValue(GoalBlockFields.GoalKey, SeededGoal);

            if (configureBeforeShow)
            {
                Configure(surface);
            }

            var result = Crashed;
            window.Loaded += async (_, _) =>
            {
                result = await MeasureAsync(surface, configureBeforeShow);
                window.Close();
            };

            window.Show();

            var frame = new DispatcherFrame();
            window.Closed += (_, _) => frame.Continue = false;

            var guard = new DispatcherTimer(
                TimeSpan.FromSeconds(180), DispatcherPriority.Normal,
                (_, _) => { frame.Continue = false; }, Dispatcher.CurrentDispatcher);
            guard.Start();

            Dispatcher.PushFrame(frame);
            guard.Stop();
            surface.Dispose();
            return result;
        }

        private static void Configure(ComposerSurface surface)
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-handshake", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            surface.Configure(
                new SessionConfig("s-probe", "probe", "w-probe", DateTimeOffset.UnixEpoch, ["claude-code"]),
                new ComposerSendContext(
                    RepositoryRoot: root,
                    DataDirectory: root,
                    AdapterInstallRoot: root,
                    EngineId: "claude-code",
                    Model: "probe-model",
                    AccountLabel: "probe-account",
                    TaskClass: "probe",
                    ProofPackArtifacts: [],
                    Providers: []),
                ComposerFields.GoalBlock(),
                new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "anthropic", "probe-account"));
        }

        private static async Task<int> MeasureAsync(ComposerSurface surface, bool configureBeforeShow)
        {
            var view = FindWebView(surface);
            if (view is null)
            {
                Console.Error.WriteLine("the composer surface holds no WebView2");
                return Crashed;
            }

            if (!configureBeforeShow)
            {
                // The other order: wait for the mount, THEN wire the session. Nothing may have been
                // pushed yet — the surface has no fields to push.
                if (!await WaitAsync(() => surface.PageIsReady))
                {
                    Console.Error.WriteLine(
                        "the page never posted editor.ready, so the host was never told it mounted");
                    return ThePageNeverMounted;
                }

                Configure(surface);
            }

            if (!await WaitAsync(async () => await Eval(view, "String(window.__composerReady === true)") == "true"))
            {
                Console.Error.WriteLine(
                    $"the page never rendered a host.init. ready={surface.PageIsReady}, "
                    + $"drops={surface.Router.Dropped}, status='{surface.Status}', "
                    + $"source='{view.CoreWebView2?.Source}', "
                    + $"instance={await Eval(view, "String(window.__aideComposerInstance)")}, "
                    + $"module={await Eval(view, "typeof window.__composerToFence")}, "
                    + $"inits={await Eval(view, "String(window.__composerInitCount)")}, "
                    + $"metrics=[{string.Join(",", surface.Metrics.Keys)}], "
                    + $"error={await Eval(view, "String(window.__composerError)")}");

                return ThePageNeverMounted;
            }

            // Settle: a SECOND init would arrive after the first render, and measuring immediately
            // would report one either way — which is a measurement that cannot fail.
            await Task.Delay(TimeSpan.FromSeconds(2));

            var inits = await Eval(view, "String(window.__composerInitCount)");
            var rendered = await Eval(
                view, "String(document.querySelector('.cm-content').textContent)");

            Console.Out.WriteLine(
                $"configure-before-show={configureBeforeShow}: host.init count={inits}, "
                + $"router drops={surface.Router.Dropped}, rendered goal={rendered}");

            // THE ACCESSOR THAT SILENTLY ATE EVERY PAGE MESSAGE, measured on every run rather than
            // remembered. `AdditionalObjects` is only populated by `postMessageWithAdditionalObjects`;
            // reading it for a plain `postMessage` threw inside the multicast event invocation, which
            // aborted the handler list and was swallowed at the COM boundary — so nothing arrived and
            // nothing reported. The surface now reads it defensively; this line is what keeps that
            // justified by a measurement.
            Console.Out.WriteLine($"AdditionalObjects on a plain postMessage: {await AdditionalObjectsAsync(view)}");

            if (inits != "1")
            {
                Console.Error.WriteLine($"host.init arrived {inits} times for one mount");
                return TheInitCountWasNotOne;
            }

            if (!rendered.Contains("must still be here", StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"the seeded field value did not survive the init: '{rendered}'");
                return TheFieldValueDidNotSurvive;
            }

            return Ok;
        }

        /// <summary>What <c>AdditionalObjects</c> does for a message that carries none.</summary>
        private static async Task<string> AdditionalObjectsAsync(WebView2 view)
        {
            var answer = "the message never arrived";
            void Read(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
            {
                try
                {
                    answer = e.AdditionalObjects is null
                        ? "null"
                        : "count " + e.AdditionalObjects.Count.ToString(
                            System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception error)
                {
                    answer = "reading it threw " + error.GetType().Name;
                }
            }

            view.CoreWebView2.WebMessageReceived += Read;

            // `metrics` with a name and a number: one of the four kinds that carry no file object,
            // routed as itself so this measurement rides the shipped path rather than a side door.
            await Eval(
                view,
                "chrome.webview.postMessage({v:1,kind:'metrics',instance:window.__aideComposerInstance,"
                + "name:'composer.probe',value:1})");

            await Task.Delay(1000);
            view.CoreWebView2.WebMessageReceived -= Read;
            return answer;
        }

        private static async Task<bool> WaitAsync(Func<bool> settled)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
            while (DateTime.UtcNow < deadline)
            {
                if (settled())
                {
                    return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static async Task<bool> WaitAsync(Func<Task<bool>> settled)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
            while (DateTime.UtcNow < deadline)
            {
                if (await settled())
                {
                    return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static WebView2? FindWebView(DependencyObject node)
        {
            if (node is WebView2 found)
            {
                return found;
            }

            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
            {
                if (FindWebView(System.Windows.Media.VisualTreeHelper.GetChild(node, i)) is { } child)
                {
                    return child;
                }
            }

            if (node is System.Windows.Controls.ContentControl { Content: DependencyObject content })
            {
                return FindWebView(content);
            }

            if (node is System.Windows.Controls.Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is DependencyObject element && FindWebView(element) is { } inside)
                    {
                        return inside;
                    }
                }
            }

            return null;
        }

        /// <summary>The attach path is never exercised here, so nothing is ever asked.</summary>
        private sealed class NeverAsked : IAttachmentAffirmation
        {
            public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
        }
    }

    /// <summary>
    /// <b>INV-0007's reproduction:</b> after the product's own New Session choreography —
    /// <c>OpenSessionDocument</c>, <c>Configure</c> exactly as <c>MainWindow.BindComposer</c> calls
    /// it, then (on <c>main</c>) <c>NewSessionPlacement.GiveItTheWholeTree</c> and the re-render that
    /// follows — the composer's entry areas are <b>perceivable and accept a keystroke</b>, in the
    /// <b>real</b> <see cref="WorkbenchShell"/> with the real docking host, under the arrangement the
    /// operator's workbench log recorded at the moment of the gesture.
    /// </summary>
    /// <remarks>
    /// <para><b>Why not the handshake probe above.</b> That one hosts a bare <see cref="ComposerSurface"/>
    /// in a bare <see cref="Window"/>, and it is green. The operator's composer sits inside a session
    /// document inside an AvalonDock document inside a layout that is REPLACED wholesale on every
    /// <c>Adapter.Render()</c>. A probe that skips the docking host exercises a composition the product
    /// does not construct (DC-135), so this one constructs what the product constructs.</para>
    ///
    /// <para><b>Three traces, each measured, none reasoned about.</b> (a) SIZE: the WebView2's realised
    /// bounds and its HWND rectangle, read after layout, before and after the maximize. (b) CONTRAST:
    /// the page's field elements' computed colours, with the contrast ratio computed in-page, plus a
    /// <c>CapturePreviewAsync</c> PNG beside the window screenshot. (c) MOUNT: how many
    /// <c>editor.ready</c> messages the page posted, the router's drop count, whether a
    /// <c>host.init</c> rendered, and how many fields and editors exist. Then it types.</para>
    ///
    /// <para><b>The keystroke goes in two ways, and each is reported separately.</b> The OS path
    /// (<c>SendInput</c>) needs the foreground window, which a probe launched from a tool host cannot
    /// always hold — so whether it held it is printed rather than assumed. The renderer path
    /// (DevTools <c>Input.dispatchKeyEvent</c>) is what the canvas probe uses and proves the
    /// page-to-host half.</para>
    /// </remarks>
    private static class ShellTyping
    {
        /// <param name="Maximize">Run main's choreography (Ruling 47's maximize) rather than F5's.</param>
        /// <param name="RenderAfterMount">After the page mounts, render once more, as any later layout command does.</param>
        /// <param name="CapCompiled">The NECESSITY check: cap the compiled view's height from outside and re-measure.</param>
        /// <param name="ReloadAfterMount">After the page mounts and takes a keystroke, reload the document — crash recovery's shape — and read what came back.</param>
        /// <param name="EscapeAfterMount">After the page mounts, let the page try to navigate away (the policy cancels it) and type again.</param>
        /// <param name="WindowHeight">The window's height. 800 is a laptop; 1400 gives the document the ~1000px the operator's screenshot shows.</param>
        private sealed record Options(
            bool Maximize, bool RenderAfterMount, bool CapCompiled, bool ReloadAfterMount, bool EscapeAfterMount, double WindowHeight);

        private static double HeightArgument(string[] args)
        {
            var at = Array.IndexOf(args, "--height");
            return at >= 0 && at + 1 < args.Length
                && double.TryParse(args[at + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var height)
                ? height
                : 800;
        }

        /// <summary>
        /// The size rule, stated as a rule rather than a number: <b>the writer is never smaller than
        /// the reader</b>. The composer is where the operator writes; the compiled view is a reader of
        /// what was written, and a reader that takes more room than the writer has inverted the
        /// surface. So the editor host keeps at least the compiled view's height, and the compiled
        /// view keeps to at most <see cref="CompiledShareCeiling"/> of the composer, scrolling inside
        /// that. At the ~1000px document height the operator's screenshot shows, that leaves the
        /// editor ~500px, which is the six goal-block fields at rest (three long-text editors at three
        /// lines, three native rows, labels and gaps — ~490px by the page's own CSS).
        /// </summary>
        private const double CompiledShareCeiling = 0.35;

        private static readonly List<string> Transitions = [];

        /// <summary>How many composer handshake lines carry <paramref name="transition"/>.</summary>
        private static int Count(string transition)
        {
            lock (Transitions)
            {
                return Transitions.Count(l =>
                    l.Contains("\"surface\":\"composer:", StringComparison.Ordinal)
                    && l.Contains($"\"transition\":\"{transition}\"", StringComparison.Ordinal));
            }
        }

        internal static int Run(string[] args)
        {
            var options = new Options(
                Maximize: args.Contains("--maximize", StringComparer.Ordinal),
                RenderAfterMount: args.Contains("--render-after-mount", StringComparer.Ordinal),
                CapCompiled: args.Contains("--cap-compiled", StringComparer.Ordinal),
                ReloadAfterMount: args.Contains("--reload-after-mount", StringComparer.Ordinal),
                EscapeAfterMount: args.Contains("--escape-after-mount", StringComparer.Ordinal),
                WindowHeight: HeightArgument(args));

            // The composer's own transitions, kept so a verdict below can count them by name rather
            // than by the probe's raw event subscriptions (which also see cancelled navigations).
            var echo = WorkbenchDiagnostics.Sink;
            WorkbenchDiagnostics.Sink = line =>
            {
                lock (Transitions) { Transitions.Add(line); }
                echo?.Invoke(line);
            };

            var root = Path.Combine(Path.GetTempPath(), "aide-shell-typing", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var config = new SessionConfigStore(root, SessionId.New(DateTimeOffset.UtcNow))
                .Create("probe session", root, ["claude-code"], DateTimeOffset.UtcNow);

            // THE PRODUCT'S COMPOSITION ROOT, line for line from MainWindow's constructor: the shell,
            // its docking host as the window's body, the dark dock theme, and the key bindings.
            var shell = new WorkbenchShell(null);
            var window = new Window
            {
                Title = "AiDe composer shell-typing probe",
                Content = shell.WorkbenchRoot,
                Width = 1280,
                Height = options.WindowHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };
            shell.Manager.Theme = new AvalonDock.Themes.Vs2013DarkTheme();
            DockThemeAccents.Retokenise(shell.Manager);
            DockRoundedTabs.Apply(shell.Manager);
            shell.Bind(window);

            var result = Crashed;
            window.Loaded += async (_, _) =>
            {
                try
                {
                    result = await MeasureAsync(window, shell, config, root, options);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                    result = Crashed;
                }

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
            shell.Dispose();
            return result;
        }

        /// <summary>
        /// The arrangement the operator's workbench log carried on the <c>open-session-document</c>
        /// line (2026-09-11T17:41:12Z): graph, domain, explore and sessions on the left; the seven
        /// documents in the centre, one of them a session document restored from a saved layout with
        /// no live session behind it; one terminal at the bottom.
        /// </summary>
        private static void ArrangeAsTheOperatorHad(WorkbenchShell shell)
        {
            var left = new ZoneStack(
            [
                new Surface("graph", "canvas", "Graph"),
                new Surface("domain", "view", "Domain"),
                new Surface("explore", "view", "Explore"),
                new Surface("sessions", "sessions", "Sessions"),
            ]);

            var center = new ZoneStack(
            [
                new Surface("ledger", "ledger", "Ledger"),
                new Surface("leaderboard", "leaderboard", "Leaderboard"),
                new Surface("board", "board", "Board"),
                new Surface("session-document:20260911T125502Z-bd59855b", SessionDocumentSurface.Kind, "2026-09-11 session"),
                new Surface("provenance", "inspector", "Provenance"),
                new Surface("contexts", "contexts", "Contexts"),
                new Surface("joins", "joins", "Joins"),
            ]);

            // A plain shell rather than an agent: the probe must never launch an engine. The surface
            // kind is what the factory reads, and it launches the same PowerShell the operator's did.
            var bottom = new ZoneStack([new Surface("terminal#b356ea", "terminal", "Terminal")]);

            var zones = System.Collections.Immutable.ImmutableDictionary.CreateRange(new[]
            {
                KeyValuePair.Create(ZoneId.Left, new ZoneState(ZoneId.Left, left, ZoneState.DefaultExtent, Collapsed: false)),
                KeyValuePair.Create(ZoneId.Right, new ZoneState(ZoneId.Right, Content: null, ZoneState.DefaultExtent, Collapsed: false)),
                KeyValuePair.Create(ZoneId.Bottom, new ZoneState(ZoneId.Bottom, bottom, 0.30, Collapsed: false)),
                KeyValuePair.Create(ZoneId.Center, new ZoneState(ZoneId.Center, center, Extent: 1.0, Collapsed: false)),
            });

            ((ZoneBackedLayoutService)shell.Service).RestoreZones(new WorkbenchLayout(zones, [], Maximized: null));
            shell.Adapter.Render();
        }

        private static async Task<int> MeasureAsync(Window window, WorkbenchShell shell, SessionConfig config, string root, Options options)
        {
            Console.Out.WriteLine($"options: maximize={options.Maximize} renderAfterMount={options.RenderAfterMount} capCompiled={options.CapCompiled} windowHeight={options.WindowHeight}");
            ArrangeAsTheOperatorHad(shell);

            // Let the arrangement settle before the gesture, as it has by the time an operator
            // reaches File -> New Session.
            await Task.Delay(800);

            // ---- MainWindow.NewSession's `opened` callback, in its order. ----
            var opened = shell.OpenSessionDocument(config);

            var composer = shell.SessionComposer(config.SessionId);
            if (composer is null)
            {
                Console.Error.WriteLine("the shell opened the document but holds no composer for it");
                return TheShellNeverShowedTheComposer;
            }

            var view = FindWebView(composer);
            if (view is null)
            {
                Console.Error.WriteLine("the composer surface holds no WebView2");
                return TheShellNeverShowedTheComposer;
            }

            // MEASURED, NOT ASSUMED: how WPF treats the browser host across the choreography.
            var loaded = 0;
            var unloaded = 0;
            view.Loaded += (_, _) => loaded++;
            view.Unloaded += (_, _) => unloaded++;

            var navigationsStarted = 0;
            var navigationsCompleted = 0;
            var readyPosted = 0;
            var messagesPosted = 0;
            var coreWired = false;
            void WireCore()
            {
                if (coreWired || view.CoreWebView2 is not { } core)
                {
                    return;
                }

                coreWired = true;
                core.NavigationStarting += (_, _) => navigationsStarted++;
                core.NavigationCompleted += (_, _) => navigationsCompleted++;

                // A SECOND subscriber on the same event (DC-134's diagnostic): it counts what the
                // page posted whether or not the surface's own handler survived to route it.
                core.WebMessageReceived += (_, e) =>
                {
                    messagesPosted++;
                    if (e.WebMessageAsJson.Contains("\"editor.ready\"", StringComparison.Ordinal))
                    {
                        readyPosted++;
                    }
                };
            }

            view.CoreWebView2InitializationCompleted += (_, _) => WireCore();

            // BindComposer, exactly: the goal-block shape, then Configure with a send context and an
            // attach gate built from one binding. The three prose fields carry what the operator had
            // typed by the time of the screenshot, so the compiled view is the ~30 lines it was.
            composer.Draft.SwitchTo(ComposerShape.GoalBlock);
            composer.Draft.SetGoalValue(GoalBlockFields.GoalKey, "Investigate why the composer accepts no typing.\nName the cause.\nStop before the fix.");
            composer.Draft.SetGoalValue(GoalBlockFields.DoneWhenKey, "A red test exists.\nThe INV is written.");
            composer.Draft.SetGoalValue(GoalBlockFields.NotInScopeKey, "The vendored bundle.\nThe test-log pollution.");
            composer.Configure(
                config,
                new ComposerSendContext(
                    RepositoryRoot: root,
                    DataDirectory: root,
                    AdapterInstallRoot: root,
                    EngineId: "claude-code",
                    Model: "probe-model",
                    AccountLabel: "probe-account",
                    TaskClass: "probe",
                    ProofPackArtifacts: [],
                    Providers: []),
                ComposerFields.GoalBlock(),
                new AttachmentGate(root, new AttachmentFileReader(), new NeverAsked(), "anthropic", "probe-account"));

            // GiveTheNewSessionTheWholeTree, exactly (main's choreography): maximize the document's
            // stack, then render — in the SAME dispatcher operation as the open and the bind, which is
            // how MainWindow's `opened` callback runs it. F5's tree ends at the bind.
            var said = opened;
            if (options.Maximize)
            {
                said = NewSessionPlacement.GiveItTheWholeTree(shell.Service, config.SessionId, opened);
                shell.Adapter.Render();
            }

            Console.Out.WriteLine($"announced: {said}");

            // (a) SIZE, read after layout: what the choreography left on screen.
            window.UpdateLayout();
            await Task.Delay(300);
            var compiledBox = FindCompiledBox(composer);
            Console.Out.WriteLine("geometry after the choreography: " + Geometry(window, composer, view, compiledBox));

            if (options.CapCompiled && compiledBox is not null)
            {
                // THE NECESSITY CHECK, from outside the product: if bounding the compiled view is what
                // gives the editor its height back, the unbounded compiled view is the cause. A
                // probe-side ceiling, never a fix.
                compiledBox.MaxHeight = Math.Floor(composer.ActualHeight * CompiledShareCeiling);
                window.UpdateLayout();
                await Task.Delay(300);
                Console.Out.WriteLine($"geometry with the compiled view capped at {compiledBox.MaxHeight:F0}px: " + Geometry(window, composer, view, compiledBox));
            }

            // ---- The choreography is over. Now: what is on screen? ----
            if (!await WaitAsync(() => view.CoreWebView2 is not null))
            {
                Console.Error.WriteLine("the composer's WebView2 never initialised inside the docking host");
                return TheShellPageNeverMounted;
            }

            WireCore();

            var mounted = await WaitAsync(async () => await Eval(view, "String(window.__composerReady === true)") == "true");

            // Settle: a late re-navigation or a second init would arrive after the first, and the
            // counts below are the evidence.
            await Task.Delay(TimeSpan.FromSeconds(3));

            var inits = await Eval(view, "String(window.__composerInitCount)");
            var fields = await Eval(view, "String(document.querySelectorAll('#fields .field').length)");
            var editors = await Eval(view, "String(document.querySelectorAll('.cm-editor').length)");
            var natives = await Eval(view, "String(document.querySelectorAll('#fields input, #fields select').length)");
            var pageReadyFlag = await Eval(view, "String(window.__composerReady)");
            var pageError = await Eval(view, "String(window.__composerError || '')");
            var pageSize = await Eval(view, "String(window.innerWidth + 'x' + window.innerHeight + ' scroll=' + document.documentElement.scrollHeight)");

            Console.Out.WriteLine(
                $"mount (c): wpf loaded={loaded} unloaded={unloaded}, "
                + $"navigations started={navigationsStarted} completed={navigationsCompleted}, "
                + $"page messages={messagesPosted} editor.ready posted={readyPosted}, "
                + $"surface ready={composer.PageIsReady} router drops={composer.Router.Dropped}, "
                + $"page ready={pageReadyFlag} host.init count={inits} fields={fields} editors={editors} native inputs={natives}, "
                + $"page viewport={pageSize}, status='{composer.Status}', error='{pageError}'");

            Console.Out.WriteLine("geometry after mount: " + Geometry(window, composer, view, compiledBox));

            // (b) CONTRAST, computed in-page from the elements' own computed styles — never eyeballed.
            var contrast = await Eval(view, ContrastScript);
            Console.Out.WriteLine("contrast (b): " + contrast);

            var shots = Path.Combine(Path.GetTempPath(), "aide-shell-typing");
            SaveScreenshot(window, Path.Combine(shots, "shell-typing-window.png"));
            await SavePagePreviewAsync(view, Path.Combine(shots, "shell-typing-page.png"));

            // THE PERCEIVABLE-STATE VERDICTS, in the order the operator meets them: is there a page,
            // does it hold fields, do they have room, and do they take a keystroke. Nothing below is
            // skipped because an earlier verdict failed — each is measured and printed; the exit code
            // is the first failure.
            var verdict = Ok;
            var compiledHeight = compiledBox?.ActualHeight ?? 0;
            var compiledShare = composer.ActualHeight > 0 ? compiledHeight / composer.ActualHeight : 0;
            var editorKeepsItsRoom = view.ActualHeight >= compiledHeight && compiledShare <= CompiledShareCeiling;
            Console.Out.WriteLine(
                $"size verdict (a): editor host={view.ActualHeight:F0}px, compiled view={compiledHeight:F0}px "
                + $"({compiledShare:P0} of the composer's {composer.ActualHeight:F0}px, ceiling {CompiledShareCeiling:P0}), "
                + $"writer >= reader: {editorKeepsItsRoom}");

            if (!mounted)
            {
                Console.Error.WriteLine(
                    "the page on screen never rendered a host.init after the shell's New Session "
                    + "choreography, so there is nothing to type into");
                verdict = TheShellPageNeverMounted;
            }
            else if (editors is "0" or "" || editors.StartsWith("(evaluate failed", StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"the page rendered no editor: editors={editors}");
                verdict = TheShellRenderedNoEditor;
            }

            if (!editorKeepsItsRoom)
            {
                Console.Error.WriteLine(
                    $"the editor host is starved: it has {view.ActualHeight:F0}px of the composer's "
                    + $"{composer.ActualHeight:F0}px while the read-only compiled view has {compiledHeight:F0}px "
                    + $"({compiledShare:P0}, ceiling {CompiledShareCeiling:P0})");
                if (verdict == Ok)
                {
                    verdict = TheEditorHasNoSize;
                }
            }

            if (verdict != Ok && (!mounted || editors is "0" or ""))
            {
                return verdict;
            }

            // ---- Type. Two paths, each reported. ----
            var before = composer.CompiledView;

            // (1) The WPF hop: keyboard focus asked for on the WebView2 control, as a click or a Tab
            // would land it, then read back both from WPF and from the OS.
            view.Focus();
            Keyboard.Focus(view);
            await Task.Delay(300);
            var wpfFocused = ReferenceEquals(Keyboard.FocusedElement, view);
            var osFocus = GetFocus();
            var osFocusClass = ClassNameOf(osFocus);
            var pageHasFocus = await Eval(view, "String(document.hasFocus())");
            var pageActive = await Eval(view, "String(document.activeElement ? (document.activeElement.className || document.activeElement.tagName) : 'none')");
            Console.Out.WriteLine(
                $"focus after Keyboard.Focus(webview): wpf={wpfFocused}, os hwnd class='{osFocusClass}', "
                + $"page hasFocus={pageHasFocus}, page active='{pageActive}'");

            // (2) The OS path: SendInput, which reaches whatever the FOREGROUND window's thread has
            // focused. Whether this process holds the foreground is printed, never assumed.
            var hwnd = new WindowInteropHelper(window).Handle;
            SetForegroundWindow(hwnd);
            await Task.Delay(200);
            var foreground = GetForegroundWindow() == hwnd;
            var sent = foreground ? SendUnicode('k') : 0u;
            await Task.Delay(700);
            var afterOs = composer.CompiledView;
            var osTyped = afterOs != before && afterOs.Contains('k');
            Console.Out.WriteLine(
                $"os keystroke: foreground held={foreground}, SendInput injected={sent}, reached draft={osTyped}");

            // (3) The renderer path: the browser's own input layer, with the editor focused in-page.
            await Eval(view, "(function(){ const c = document.querySelector('.cm-content'); if (c) c.focus(); return 'focused'; })()");
            await Task.Delay(150);
            var cdp = await DispatchCharAsync(view, 'q', 0x51);
            await Task.Delay(700);
            var afterCdp = composer.CompiledView;
            var cdpTyped = afterCdp.Contains('q');
            var pageText = await Eval(view, "String((document.querySelector('.cm-content') || {}).textContent || '')");
            Console.Out.WriteLine(
                $"renderer keystroke: dispatched={cdp}, page editor text='{pageText}', reached draft={cdpTyped}, "
                + $"router drops={composer.Router.Dropped}");

            Console.Out.WriteLine($"compiled view after typing: '{afterCdp.Replace("\n", "\\n")}'");

            if (!osTyped && !cdpTyped)
            {
                Console.Error.WriteLine("no keystroke reached the draft by any path");
                if (verdict == Ok)
                {
                    verdict = TypingDidNotReachTheDraft;
                }
            }

            if (options.EscapeAfterMount)
            {
                // THE CANCELLED BRANCH. The page tries to leave; the policy cancels the navigation
                // (C10). A cancelled navigation replaces no document, so it must reset nothing: the
                // composer's own navigation count stays where it was, no ready is expected, and the
                // next keystroke still reaches the draft.
                var startedBefore = Count("navigation-started");
                await Eval(view, "(function(){ try { location.href = 'https://example.invalid/'; } catch (e) {} return 'tried'; })()");
                await Task.Delay(TimeSpan.FromSeconds(2));

                await Eval(view, "(function(){ const c = document.querySelector('.cm-content'); if (c) c.focus(); return 'focused'; })()");
                await Task.Delay(150);
                var again = await DispatchCharAsync(view, 'z', 0x5A);
                await Task.Delay(700);
                var afterEscape = composer.CompiledView;
                var escapeTyped = afterEscape.Contains('z');
                Console.Out.WriteLine(
                    $"after a cancelled navigation: composer navigation-started {startedBefore}->{Count("navigation-started")}, "
                    + $"raw NavigationStarting={navigationsStarted}, page ready={composer.PageIsReady}, router drops={composer.Router.Dropped}, "
                    + $"dispatched={again}, reached draft={escapeTyped}");

                if (Count("navigation-started") != startedBefore || !escapeTyped)
                {
                    Console.Error.WriteLine("a cancelled navigation reset the handshake: the composer counted it or the next keystroke was lost");
                    verdict = TheEscapeResetTheHandshake;
                }
            }

            if (options.ReloadAfterMount)
            {
                // THE ALLOWED BRANCH. A genuine reload — crash recovery's shape — replaces the
                // document. The new page's ready must be a mount, not a duplicate: one more
                // navigation-started, one more init-pushed, no message-dropped for the ready, six
                // fields, and the editor showing the draft the host still holds.
                var startedBefore = Count("navigation-started");
                var pushedBefore = Count("init-pushed");
                var dropsBefore = composer.Router.Dropped;
                var readyBefore = readyPosted;

                view.CoreWebView2!.Reload();
                var remounted = await WaitAsync(async () =>
                    await Eval(view, "String(window.__composerReady === true && document.querySelectorAll('#fields .field').length > 0)") == "true");
                await Task.Delay(TimeSpan.FromSeconds(2));

                var fieldsAfter = await Eval(view, "String(document.querySelectorAll('#fields .field').length)");
                var initsAfter = await Eval(view, "String(window.__composerInitCount)");
                var textAfter = await Eval(view, "String((document.querySelector('.cm-content') || {}).textContent || '')");
                var firstGoalLine = composer.Draft.GoalValues.TryGetValue(GoalBlockFields.GoalKey, out var goalNow) ? goalNow.Split('\n')[0] : string.Empty;
                Console.Out.WriteLine(
                    $"after a reload: remounted={remounted}, composer navigation-started {startedBefore}->{Count("navigation-started")}, "
                    + $"init-pushed {pushedBefore}->{Count("init-pushed")}, editor.ready posted +{readyPosted - readyBefore}, "
                    + $"router drops +{composer.Router.Dropped - dropsBefore}, host.init count={initsAfter} fields={fieldsAfter}, "
                    + $"editor text='{textAfter}', draft holds='{firstGoalLine}'");

                if (!remounted || fieldsAfter is "0" or "" || Count("navigation-started") != startedBefore + 1
                    || Count("init-pushed") != pushedBefore + 1 || composer.Router.Dropped != dropsBefore
                    || !textAfter.Contains(firstGoalLine, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine("a reload lost the page: the new document's ready was not a mount, or the draft did not come back");
                    verdict = TheReloadLostThePage;
                }
            }

            if (options.RenderAfterMount)
            {
                // ANY LATER LAYOUT COMMAND. Opening a pane, a layout command, a restore — every one of
                // them is one Adapter.Render(), which replaces Manager.Layout wholesale and re-parents
                // the composer. What that does to a page that has already mounted is measured here.
                var readyBefore = readyPosted;
                var navBefore = navigationsStarted;

                // THE SIBLING SWEEP, measured rather than read: the graph canvas is built the same
                // way (Loaded -> InitialiseAsync -> navigate), so the same render should reload it.
                var canvasNavigations = 0;
                var canvasView = shell.Adapter.ContentFor("graph") is { } graph ? FindWebView(graph) : null;
                if (canvasView?.CoreWebView2 is { } canvasCore)
                {
                    canvasCore.NavigationStarting += (_, _) => canvasNavigations++;
                }

                shell.Adapter.Render();
                await Task.Delay(TimeSpan.FromSeconds(4));

                Console.Out.WriteLine(
                    canvasView?.CoreWebView2 is null
                        ? "canvas sibling: not recorded (the graph canvas had no CoreWebView2)"
                        : $"canvas sibling: navigations started by the same render={canvasNavigations}");

                var fieldsAfter = await Eval(view, "String(document.querySelectorAll('#fields .field').length)");
                var initsAfter = await Eval(view, "String(window.__composerInitCount)");
                var textAfter = await Eval(view, "String((document.querySelector('.cm-content') || {}).textContent || '')");
                var firstGoalLine = composer.Draft.GoalValues.TryGetValue(GoalBlockFields.GoalKey, out var goal) ? goal.Split('\n')[0] : string.Empty;
                Console.Out.WriteLine(
                    $"after one later render: wpf loaded={loaded} unloaded={unloaded}, navigations started={navigationsStarted} (+{navigationsStarted - navBefore}), "
                    + $"editor.ready posted={readyPosted} (+{readyPosted - readyBefore}), router drops={composer.Router.Dropped}, "
                    + $"host.init count={initsAfter} fields={fieldsAfter}, editor text='{textAfter}', draft still holds='{firstGoalLine}'");

                if (fieldsAfter is "0" or "")
                {
                    Console.Error.WriteLine(
                        "one later render re-navigated the composer page; its second editor.ready was dropped as a "
                        + "duplicate, no host.init followed, and the fields the operator was typing into are gone");

                    // The claim this run exists to test, so it is the exit code even when the size
                    // verdict above also failed — both are printed; neither hides the other.
                    verdict = TheLaterRenderResetThePage;
                }
            }

            if (verdict == Ok)
            {
                Console.Out.WriteLine("the composer's entry areas were on screen, kept their room and accepted typing after the shell's New Session choreography");
            }

            return verdict;
        }

        /// <summary>
        /// Every colour pair the operator has to read, with WCAG contrast computed from the elements'
        /// own computed styles. The page background is walked up until an opaque colour is found, so a
        /// transparent field is measured against what actually sits behind it.
        /// </summary>
        private const string ContrastScript = """
            (function () {
              function parse(c) {
                const m = /rgba?\(([^)]+)\)/.exec(c || '');
                if (!m) return null;
                const p = m[1].split(',').map(function (s) { return parseFloat(s); });
                return { r: p[0], g: p[1], b: p[2], a: p.length > 3 ? p[3] : 1 };
              }
              function lum(c) {
                const f = function (v) { v = v / 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); };
                return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b);
              }
              function ratio(a, b) { const l1 = lum(a), l2 = lum(b); return ((Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05)).toFixed(2); }
              function bg(el) {
                let e = el;
                while (e) {
                  const c = parse(getComputedStyle(e).backgroundColor);
                  if (c && c.a > 0) return c;
                  e = e.parentElement;
                }
                return { r: 0, g: 0, b: 0, a: 1 };
              }
              const out = [];
              function report(name, el) {
                if (!el) { out.push(name + ': absent'); return; }
                const cs = getComputedStyle(el);
                const fg = parse(cs.color);
                const back = bg(el);
                const r = el.getBoundingClientRect();
                out.push(name + ': fg=' + cs.color + ' bg=' + cs.backgroundColor + ' effectiveBg=rgb(' + back.r + ',' + back.g + ',' + back.b + ')'
                  + ' contrast=' + (fg ? ratio(fg, back) : 'n/a') + ' border=' + cs.borderColor + ' size=' + Math.round(r.width) + 'x' + Math.round(r.height));
              }
              report('body', document.body);
              report('label', document.querySelector('#fields label'));
              report('editor-box', document.querySelector('.editor'));
              report('cm-content', document.querySelector('.cm-content'));
              report('input', document.querySelector('#fields input'));
              report('select', document.querySelector('#fields select'));
              report('drop-hint', document.getElementById('drop-hint'));
              const box = document.querySelector('.editor');
              if (box) {
                const cs = getComputedStyle(box);
                const b = parse(cs.borderColor), k = bg(box.parentElement);
                out.push('editor-border-vs-page: ' + (b ? ratio(b, k) : 'n/a'));
              }
              return out.join(' | ');
            })()
            """;

        private static string Geometry(Window window, ComposerSurface composer, WebView2 view, System.Windows.Controls.TextBox? compiledBox)
        {
            var hostRect = "(no hwnd)";
            var childRect = "(no child)";
            if (view.Handle != IntPtr.Zero && GetWindowRect(view.Handle, out var host))
            {
                hostRect = $"{host.Right - host.Left}x{host.Bottom - host.Top}@{host.Left},{host.Top}";
                var child = GetWindow(view.Handle, 5 /* GW_CHILD */);
                if (child != IntPtr.Zero && GetWindowRect(child, out var c))
                {
                    childRect = $"{ClassNameOf(child)} {c.Right - c.Left}x{c.Bottom - c.Top} visible={IsWindowVisible(child)}";
                }
            }

            return
                $"window={window.ActualWidth:F0}x{window.ActualHeight:F0}, "
                + $"composer={composer.ActualWidth:F0}x{composer.ActualHeight:F0} visible={composer.IsVisible}, "
                + $"webview={view.ActualWidth:F0}x{view.ActualHeight:F0} visible={view.IsVisible} hwnd={hostRect} browser={childRect}, "
                + $"compiled box={compiledBox?.ActualWidth:F0}x{compiledBox?.ActualHeight:F0} readOnly={compiledBox?.IsReadOnly}";
        }

        private static System.Windows.Controls.TextBox? FindCompiledBox(DependencyObject node)
        {
            if (node is System.Windows.Controls.TextBox { IsReadOnly: true } box)
            {
                return box;
            }

            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
            {
                if (FindCompiledBox(System.Windows.Media.VisualTreeHelper.GetChild(node, i)) is { } child)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>The window as the operator sees it, child HWNDs included (PW_RENDERFULLCONTENT).</summary>
        private static void SaveScreenshot(Window window, string path)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                GetWindowRect(hwnd, out var rect);
                var width = rect.Right - rect.Left;
                var height = rect.Bottom - rect.Top;
                using var bitmap = new System.Drawing.Bitmap(width, height);
                using var graphics = System.Drawing.Graphics.FromImage(bitmap);
                var hdc = graphics.GetHdc();
                PrintWindow(hwnd, hdc, 0x2);
                graphics.ReleaseHdc(hdc);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                Console.Out.WriteLine($"window screenshot: {path}");
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                Console.Out.WriteLine("window screenshot: not recorded (" + error.Message + ")");
            }
        }

        /// <summary>The page as the browser renders it, independent of the WPF host.</summary>
        private static async Task SavePagePreviewAsync(WebView2 view, string path)
        {
            try
            {
                if (view.CoreWebView2 is not { } core)
                {
                    Console.Out.WriteLine("page preview: not recorded (no CoreWebView2)");
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = File.Create(path);

                // BOUNDED: a zero-height host never completes a capture (measured — 181 s to the
                // guard timer), and the capture is evidence, not the assertion.
                var capture = core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
                if (await Task.WhenAny(capture, Task.Delay(TimeSpan.FromSeconds(8))) != capture)
                {
                    Console.Out.WriteLine("page preview: not recorded (the capture did not complete in 8 s)");
                    return;
                }

                await capture;
                Console.Out.WriteLine($"page preview: {path}");
            }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                Console.Out.WriteLine("page preview: not recorded (" + error.Message + ")");
            }
        }

        private static async Task<bool> DispatchCharAsync(WebView2 view, char c, int virtualKey)
        {
            var core = view.CoreWebView2;
            if (core is null)
            {
                return false;
            }

            foreach (var (type, text) in new[] { ("keyDown", c.ToString()), ("keyUp", "") })
            {
                var payload = JsonSerializer.Serialize(new
                {
                    type,
                    key = c.ToString(),
                    text,
                    unmodifiedText = text,
                    windowsVirtualKeyCode = virtualKey,
                    nativeVirtualKeyCode = virtualKey,
                });

                try
                {
                    await core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", payload);
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    Console.Error.WriteLine("Input.dispatchKeyEvent refused: " + error.Message);
                    return false;
                }
            }

            return true;
        }

        private static uint SendUnicode(char c)
        {
            var inputs = new INPUT[2];
            inputs[0].type = 1;
            inputs[0].u.ki.wScan = c;
            inputs[0].u.ki.dwFlags = 0x0004; // KEYEVENTF_UNICODE
            inputs[1].type = 1;
            inputs[1].u.ki.wScan = c;
            inputs[1].u.ki.dwFlags = 0x0004 | 0x0002; // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
            return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }

        private static string ClassNameOf(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return "(none)";
            }

            var buffer = new StringBuilder(256);
            return GetClassName(hwnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : "(unreadable)";
        }

        private static async Task<bool> WaitAsync(Func<bool> settled)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
            while (DateTime.UtcNow < deadline)
            {
                if (settled())
                {
                    return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static async Task<bool> WaitAsync(Func<Task<bool>> settled)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
            while (DateTime.UtcNow < deadline)
            {
                if (await settled())
                {
                    return true;
                }

                await Task.Delay(100);
            }

            return false;
        }

        private static WebView2? FindWebView(DependencyObject node)
        {
            if (node is WebView2 found)
            {
                return found;
            }

            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
            {
                if (FindWebView(System.Windows.Media.VisualTreeHelper.GetChild(node, i)) is { } child)
                {
                    return child;
                }
            }

            if (node is System.Windows.Controls.ContentControl { Content: DependencyObject content })
            {
                return FindWebView(content);
            }

            if (node is System.Windows.Controls.Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is DependencyObject element && FindWebView(element) is { } inside)
                    {
                        return inside;
                    }
                }
            }

            return null;
        }

        private sealed class NeverAsked : IAttachmentAffirmation
        {
            public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static int Run()
    {
        var view = new WebView2();
        var window = new Window
        {
            Title = "AiDe composer probe",
            Content = view,
            Width = 900,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        var result = Crashed;
        window.Loaded += async (_, _) =>
        {
            result = await ProbeAsync(view);
            window.Close();
        };

        window.Show();

        var frame = new DispatcherFrame();
        window.Closed += (_, _) => frame.Continue = false;

        var guard = new DispatcherTimer(
            TimeSpan.FromSeconds(180), DispatcherPriority.Normal,
            (_, _) => { frame.Continue = false; }, Dispatcher.CurrentDispatcher);
        guard.Start();

        Dispatcher.PushFrame(frame);
        guard.Stop();
        view.Dispose();
        return result;
    }

    private static async Task<int> ProbeAsync(WebView2 view)
    {
        await view.EnsureCoreWebView2Async();
        var core = view.CoreWebView2;

        ComposerPageContract.ApplySettingsFloor(core.Settings);
        WebAssetHost.Map(core);

        core.NavigationStarting += (_, e) =>
        {
            if (ComposerPageContract.MustCancelNavigation(e.Uri))
            {
                e.Cancel = true;
                Cancelled.Add("navigation:" + e.Uri);
            }
        };
        core.FrameNavigationStarting += (_, e) =>
        {
            if (ComposerPageContract.MustCancelNavigation(e.Uri))
            {
                e.Cancel = true;
                Cancelled.Add("frame:" + e.Uri);
            }
        };
        core.NewWindowRequested += (_, e) =>
        {
            e.Handled = true;
            Cancelled.Add("newwindow:" + e.Uri);
        };
        core.LaunchingExternalUriScheme += (_, e) =>
        {
            e.Cancel = true;
            Cancelled.Add("external:" + e.Uri);
        };

        var posted = new List<string>();
        core.WebMessageReceived += (_, e) => posted.Add(e.WebMessageAsJson);

        // THE SETTINGS FLOOR, read back rather than assumed: applying a setting and never reading it
        // is the shape of a control that cannot fire.
        if (core.Settings.AreDevToolsEnabled
            || core.Settings.AreHostObjectsAllowed
            || core.Settings.AreDefaultContextMenusEnabled)
        {
            Console.Error.WriteLine(
                $"the settings floor did not apply: devtools={core.Settings.AreDevToolsEnabled}, "
                + $"hostObjects={core.Settings.AreHostObjectsAllowed}, "
                + $"contextMenus={core.Settings.AreDefaultContextMenusEnabled}");
            return SettingsFloorNotApplied;
        }

        var url = ComposerPageContract.Url;
        var navigated = new TaskCompletionSource<bool>();
        view.NavigationCompleted += (_, e) => navigated.TrySetResult(e.IsSuccess);
        core.Navigate(url);

        var finished = await Task.WhenAny(navigated.Task, Task.Delay(TimeSpan.FromSeconds(60)));
        if (finished != navigated.Task || !navigated.Task.Result)
        {
            Console.Error.WriteLine("the composer page never loaded over the virtual host");
            return PageNeverLoaded;
        }

        // The host pushes the init the page waits for, exactly as the surface does.
        core.PostWebMessageAsJson(JsonSerializer.Serialize(new
        {
            v = 1,
            kind = "host.init",
            instance = "probe-instance",
            attachEnabled = false,
            fields = new object[]
            {
                new { id = "goal:1", name = "goal", label = "goal", widget = "long-text", required = true, hint = (string?)null, options = (string[]?)null, value = "## heading\n\nAsk @codemirror to render this.\n\n```csharp\nvar x = 1;\n```\n" },
                new { id = "tier:1", name = "tier", label = "tier", widget = "enum", required = true, hint = (string?)null, options = new[] { "T0", "T1", "T2" }, value = "T1" },
                new { id = "cap:1", name = "fan_out_cap", label = "fan_out_cap", widget = "text", required = true, hint = (string?)null, options = (string[]?)null, value = "0" },
                new { id = "budget:1", name = "budget", label = "budget", widget = "budget", required = true, hint = (string?)null, options = (string[]?)null, value = "10,1000" },
            },
            fileCandidates = new[] { "src/Payments.cs" },
            graphCandidates = new[] { "PaymentAggregate" },
        }));

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        var ready = "false";
        while (DateTime.UtcNow < deadline)
        {
            ready = await Eval(view, "String(window.__composerReady === true)");
            if (ready == "true")
            {
                break;
            }

            await Task.Delay(100);
        }

        var error = await Eval(view, "String(window.__composerError || '')");
        if (ready.StartsWith("(evaluate failed", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"the page could not be evaluated, so nothing below was measured: {ready}");
            return CouldNotEvaluate;
        }

        if (ready != "true")
        {
            Console.Error.WriteLine($"the composer module never executed. window.__composerError = {error}");
            return ModuleNeverExecuted;
        }

        // C12'S REAL QUESTION. The editor mounting under `default-src 'none'; script-src 'self';
        // style-src 'self' 'unsafe-inline'` is what settles whether the policy is survivable, and the
        // chip proves the BUNDLE's own plugin ran rather than that a div exists.
        var editors = await Eval(view, "String(document.querySelectorAll('.cm-editor').length)");
        var chips = await Eval(view, "String(document.querySelectorAll('.cm-mention-chip').length)");
        var natives = await Eval(view, "String(document.querySelectorAll('select, input[type=\\\"text\\\"], input[type=\\\"number\\\"]').length)");

        Console.Out.WriteLine($"editors: {editors}, chips: {chips}, native controls: {natives}");

        if (editors is "0" or "" || chips is "0" or "")
        {
            Console.Error.WriteLine(
                "the editor did not render under the shipped Content-Security-Policy — "
                + $"editors={editors}, chips={chips}");
            return EditorNeverRenderedUnderTheCsp;
        }

        // RULING 33, observed rather than argued: four native fields were pushed and exactly ONE
        // editor mounted, so no per-field editor was created for a native widget.
        if (editors != "1")
        {
            Console.Error.WriteLine($"a per-field editor was created for a native widget: {editors} editors for one long-text field");
            return EditorNeverRenderedUnderTheCsp;
        }

        // C12: the page cannot reach the network, and cannot RESOLVE a host object.
        //
        // BOTH ARE POLLED RATHER THAN RETURNED, and that is not a style choice: ExecuteScriptAsync
        // serialises the value the script evaluates to, and a Promise serialises as `{}`. The first
        // version of this probe read that `{}` and reported it as an answer — a measurement that
        // could never have been "resolved" and therefore could never have failed.
        //
        // AND `typeof chrome.webview.hostObjects` IS NOT THE TEST. Measured here: it reads `function`
        // whether or not host objects are allowed, because the accessor is part of the WebView2 script
        // API rather than a capability. The capability is whether one can be RESOLVED, so that is what
        // this asks.
        var fetchOutcome = await Poll(view, "__probeFetch", """
            window.__probeFetch = 'pending';
            try {
              fetch('https://example.invalid/')
                .then(function () { window.__probeFetch = 'resolved'; })
                .catch(function () { window.__probeFetch = 'rejected'; });
            } catch (e) { window.__probeFetch = 'rejected'; }
            'started'
            """);

        var hostObjectOutcome = await Poll(view, "__probeHost", """
            window.__probeHost = 'pending';
            (async function () {
              try {
                const o = chrome.webview.hostObjects.probeTarget;
                await o.ToString();
                window.__probeHost = 'resolved';
              } catch (e) {
                window.__probeHost = 'rejected';
              }
            })();
            'started'
            """);

        Console.Out.WriteLine($"fetch: {fetchOutcome}, hostObject resolution: {hostObjectOutcome}");

        if (hostObjectOutcome != "rejected")
        {
            Console.Error.WriteLine($"a host object could be resolved from the page: {hostObjectOutcome}");
            return HostObjectsReachable;
        }

        if (fetchOutcome != "rejected")
        {
            Console.Error.WriteLine($"the page reached the network: {fetchOutcome}");
            return NetworkReachable;
        }

        // C10: the control can reach exactly one document. The page tries every way it has.
        await Eval(view, "try { location.href = 'https://example.invalid/'; } catch (e) {}");
        await Eval(view, "try { window.open('https://example.invalid/'); } catch (e) {}");
        await Eval(
            view,
            "try { const f = document.createElement('iframe'); f.src = 'https://example.invalid/'; document.body.appendChild(f); } catch (e) {}");
        await Task.Delay(1500);

        var source = core.Source;
        Console.Out.WriteLine($"source after the probes: {source}; cancelled: {string.Join(", ", Cancelled)}");

        if (!string.Equals(source, url, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"the control's document changed: {source}");
            return TheDocumentCouldBeChanged;
        }

        // `paste-to-fence` (R15 b1), driven as a real paste event on the real editor.
        var pasted = await Eval(
            view,
            """
            (function () {
              const content = document.querySelector('.cm-content');
              content.focus();
              const data = new DataTransfer();
              data.setData('text/plain', 'var a = 1;\nvar b = 2;\n');
              content.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
              return 'dispatched';
            })()
            """);

        await Task.Delay(500);
        var fenced = await Eval(view, "String(document.querySelector('.cm-content').textContent.indexOf('text pasted') >= 0)");
        var prose = await Eval(view, "String(window.__composerToFence('one line') === 'one line')");

        Console.Out.WriteLine($"paste: {pasted}, fenced: {fenced}, single-line stays prose: {prose}");

        if (fenced != "true" || prose != "true")
        {
            Console.Error.WriteLine("pasted multi-line code did not land as a fenced block");
            return PasteDidNotFence;
        }

        // And nothing the page did produced a send: there is no such kind to produce.
        Console.Out.WriteLine($"page messages observed: {posted.Count}");
        if (posted.Exists(m => m.Contains("\"send", StringComparison.Ordinal) || m.Contains("run.start", StringComparison.Ordinal)))
        {
            Console.Error.WriteLine("the page posted a send-shaped message");
            return TheDocumentCouldBeChanged;
        }

        Console.Out.WriteLine("the composer page rendered under its policy and could reach exactly one document");
        return Ok;
    }

    /// <summary>
    /// Runs an asynchronous probe and waits for the global it settles, rather than reading the value
    /// the script returned.
    /// </summary>
    /// <remarks>
    /// A promise crosses <c>ExecuteScriptAsync</c> as <c>{}</c>, so reading the return value gives an
    /// answer that is neither of the two outcomes and cannot fail. The global is the result.
    /// </remarks>
    private static async Task<string> Poll(WebView2 view, string global, string script)
    {
        await Eval(view, script);

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        var value = "pending";
        while (DateTime.UtcNow < deadline)
        {
            value = await Eval(view, $"String(window.{global})");
            if (value != "pending")
            {
                return value;
            }

            await Task.Delay(100);
        }

        return value;
    }

    private static async Task<string> Eval(WebView2 view, string script)
    {
        try
        {
            var raw = await view.CoreWebView2.ExecuteScriptAsync(script);
            return raw?.Trim('"') ?? string.Empty;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return $"(evaluate failed: {ex.Message})";
        }
    }
}
