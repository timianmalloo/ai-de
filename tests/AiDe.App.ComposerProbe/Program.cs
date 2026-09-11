using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;
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

    /// <summary>Runs the handshake oracle instead of the CSP/one-document probe.</summary>
    private const string HandshakeArgument = "--handshake";

    private static readonly List<string> Cancelled = [];

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
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
