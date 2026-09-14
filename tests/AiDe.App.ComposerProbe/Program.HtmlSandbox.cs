using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AiDe.Core.AgentPlane;
using AiDe.Core.Presentation.Composer;
using AiDe.Core.Sessions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.ComposerProbe;

internal static partial class Program
{
    /// <summary>Runs the Ruling 93 HTML-sandbox oracle and the WebView2 private-bytes measurement.</summary>
    private const string HtmlSandboxArgument = "--html-sandbox";

    // --html-sandbox only. Each is a different way the reader's sandbox fails to be a sandbox.
    private const int TheComposerNeverMounted = 40;
    private const int TheSandboxWasNeverAsserted = 41;
    private const int TheSandboxNeverShowedTheDocument = 42;
    private const int ThePositiveControlSawNoScript = 43;
    private const int ScriptRanInTheSandbox = 44;
    private const int TheNetworkRequestWasNotBlocked = 45;
    private const int TheNavigationWasNotCancelled = 46;

    /// <summary>
    /// <b>Ruling 93, conditions (2) and (4), in the only place they can be answered:</b> the shipped
    /// <see cref="HtmlSandboxHost"/> in a real window beside the shipped <see cref="ComposerSurface"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>The sandbox oracle.</b> One page carries a <c>&lt;script&gt;</c> that would stamp
    /// the DOM, an <c>&lt;img&gt;</c> whose source is on a reserved <c>.invalid</c> host, and a
    /// <c>&lt;meta http-equiv="refresh"&gt;</c> to the same host. The same page is first shown in
    /// a plain, script-enabled WebView2 — the <b>positive control</b> that proves the oracle can
    /// see a script run (DC-016/DC-157) — and then in the reader's host, where the stamp must be
    /// absent, the request counted as blocked, and the refresh counted as cancelled. Host-injected
    /// script (<c>ExecuteScriptAsync</c>) runs even with <c>IsScriptEnabled = false</c>, which is
    /// what lets the probe read the DOM the page's own script was denied.</para>
    ///
    /// <para><b>The private-bytes measurement (P-4's form).</b> Private bytes of this process
    /// plus every process the WebView2 environment reports (<c>GetProcessInfos</c>), read at three
    /// points: before any browser, after the composer's page mounted, after the reader's host
    /// rendered. The two deltas are the composer's cost and the reader's marginal cost; the test
    /// that launched this records them and the Proof Pack decides on them.</para>
    /// </remarks>
    private static class HtmlSandbox
    {
        private const string Page =
            "<!doctype html><html><head><meta charset=\"utf-8\">"
            + "<meta http-equiv=\"refresh\" content=\"1;url=https://aide-sandbox-probe.invalid/refresh\">"
            + "<title>sandbox probe</title></head><body>"
            + "<h1 id=\"heading\">Sandbox probe</h1>"
            + "<script>document.body.appendChild(Object.assign(document.createElement('p'), { id: 'ran', textContent: 'script ran' }));</script>"
            + "<img id=\"leak\" src=\"https://aide-sandbox-probe.invalid/leak.png\" alt=\"\">"
            + "</body></html>";

        internal static int Run()
        {
            var composer = new ComposerSurface("composer:sandbox-probe", "probe — composer");
            var control = new WebView2();
            var host = new HtmlSandboxHost("explorer-reader-html:probe");

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(composer, 0);
            Grid.SetColumn(control, 1);
            Grid.SetColumn(host, 2);
            grid.Children.Add(composer);

            var window = new Window
            {
                Title = "AiDe html-sandbox probe",
                Content = grid,
                Width = 1200,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            composer.Draft.SwitchTo(ComposerShape.GoalBlock);
            Configure(composer);

            var result = Crashed;
            window.Loaded += async (_, _) =>
            {
                result = await MeasureAsync(composer, control, host, grid);
                window.Close();
            };

            window.Show();

            var frame = new DispatcherFrame();
            window.Closed += (_, _) => frame.Continue = false;

            var guard = new DispatcherTimer(
                TimeSpan.FromSeconds(240), DispatcherPriority.Normal,
                (_, _) => { frame.Continue = false; }, Dispatcher.CurrentDispatcher);
            guard.Start();

            Dispatcher.PushFrame(frame);
            guard.Stop();
            host.Dispose();
            control.Dispose();
            composer.Dispose();
            return result;
        }

        private static async Task<int> MeasureAsync(ComposerSurface composer, WebView2 control, HtmlSandboxHost host, Grid grid)
        {
            var before = PrivateBytes();
            Console.Out.WriteLine($"private-bytes before any browser: host={before.Host:N0} tree={before.Tree:N0}");

            var composerView = FindWebView(composer);
            if (composerView is null || !await WaitAsync(async () => await Eval(composerView, "String(window.__composerReady === true)") == "true"))
            {
                Console.Error.WriteLine("the composer's page never mounted, so there is nothing to compare the reader against");
                return TheComposerNeverMounted;
            }

            await Task.Delay(TimeSpan.FromSeconds(3));
            var afterComposer = PrivateBytes(composerView.CoreWebView2?.Environment);
            Console.Out.WriteLine(
                $"private-bytes after the composer mounted: host={afterComposer.Host:N0} tree={afterComposer.Tree:N0} "
                + $"processes=[{afterComposer.Kinds}]");
            Console.Out.WriteLine($"composer delta: {afterComposer.Tree - before.Tree:N0} bytes");

            // The positive control: the SAME page, script enabled, in a plain control. If the
            // stamp is absent here the oracle cannot see a script run and everything below is
            // vacuous.
            grid.Children.Add(control);
            await control.EnsureCoreWebView2Async();
            var controlLoaded = new TaskCompletionSource<bool>();
            control.NavigationCompleted += (_, e) => controlLoaded.TrySetResult(e.IsSuccess);
            control.CoreWebView2.Settings.IsScriptEnabled = true;
            control.CoreWebView2.NavigateToString(Page);
            await Task.WhenAny(controlLoaded.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            var controlRan = await Eval(control, "String(!!document.getElementById('ran'))");
            Console.Out.WriteLine($"positive control (script enabled): #ran present = {controlRan}");
            if (controlRan != "true")
            {
                Console.Error.WriteLine("the positive control never saw its own script run — the oracle cannot see execution");
                return ThePositiveControlSawNoScript;
            }

            await Task.Delay(TimeSpan.FromSeconds(3));
            var afterControl = PrivateBytes(control.CoreWebView2?.Environment, composerView.CoreWebView2?.Environment);
            Console.Out.WriteLine(
                $"private-bytes after the positive control: host={afterControl.Host:N0} tree={afterControl.Tree:N0} "
                + $"processes=[{afterControl.Kinds}]");
            Console.Out.WriteLine($"second-webview2 delta (a plain control, script on): {afterControl.Tree - afterComposer.Tree:N0} bytes");

            // The reader's host: shown, sandboxed, then the document.
            var shown = new TaskCompletionSource<bool>();
            host.DocumentShown += () => shown.TrySetResult(true);
            host.SandboxUnavailable += why => Console.Error.WriteLine("sandbox unavailable: " + why);
            host.Show(Page);
            grid.Children.Add(host);

            if (!await WaitAsync(() => host.IsSandboxed || host.Unavailable is not null))
            {
                Console.Error.WriteLine("the reader's host never asserted its sandbox");
                return TheSandboxWasNeverAsserted;
            }

            if (host.Unavailable is not null)
            {
                Console.Error.WriteLine($"the sandbox could not be asserted: {host.Unavailable}");
                return TheSandboxWasNeverAsserted;
            }

            var settings = host.View.CoreWebView2!.Settings;
            Console.Out.WriteLine(
                $"sandbox settings read back: IsScriptEnabled={settings.IsScriptEnabled} "
                + $"IsWebMessageEnabled={settings.IsWebMessageEnabled} AreHostObjectsAllowed={settings.AreHostObjectsAllowed} "
                + $"AreDevToolsEnabled={settings.AreDevToolsEnabled}");

            await Task.WhenAny(shown.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            if (!shown.Task.IsCompleted)
            {
                Console.Error.WriteLine("the sandboxed document was never navigated to");
                return TheSandboxNeverShowedTheDocument;
            }

            // Settle: the page's refresh is at 1 s and the image request is issued at parse time;
            // a read at 0 s would report zero for both and mean nothing.
            await Task.Delay(TimeSpan.FromSeconds(4));

            var heading = await Eval(host.View, "String(!!document.getElementById('heading'))");
            var ran = await Eval(host.View, "String(!!document.getElementById('ran'))");
            Console.Out.WriteLine(
                $"sandboxed: heading rendered = {heading}, #ran present = {ran}, "
                + $"blocked requests = {host.BlockedRequests}, cancelled navigations = {host.CancelledNavigations}, "
                + $"source = {host.View.CoreWebView2.Source}");

            var afterReader = PrivateBytes(host.View.CoreWebView2.Environment, control.CoreWebView2?.Environment, composerView.CoreWebView2?.Environment);
            Console.Out.WriteLine(
                $"private-bytes after the reader rendered: host={afterReader.Host:N0} tree={afterReader.Tree:N0} "
                + $"processes=[{afterReader.Kinds}]");
            Console.Out.WriteLine($"reader delta (the sandbox host alone, a third WebView2): {afterReader.Tree - afterControl.Tree:N0} bytes");

            if (heading != "true")
            {
                Console.Error.WriteLine("the sandboxed document rendered no heading — the page was not shown at all");
                return TheSandboxNeverShowedTheDocument;
            }

            if (ran == "true")
            {
                Console.Error.WriteLine("the page's own script RAN inside the reader's sandbox");
                return ScriptRanInTheSandbox;
            }

            if (host.BlockedRequests == 0)
            {
                Console.Error.WriteLine("the image request to the .invalid host was not answered by the sandbox");
                return TheNetworkRequestWasNotBlocked;
            }

            if (host.CancelledNavigations == 0)
            {
                Console.Error.WriteLine("the meta refresh to the .invalid host was not cancelled");
                return TheNavigationWasNotCancelled;
            }

            Console.Out.WriteLine("the reader's HTML sandbox held: no script, no network, no navigation");
            return Ok;
        }

        private sealed record Bytes(long Host, long Tree, string Kinds);

        /// <summary>This process plus every process the given environments report, deduplicated by id.</summary>
        private static Bytes PrivateBytes(params CoreWebView2Environment?[] environments)
        {
            var self = Process.GetCurrentProcess();
            self.Refresh();
            var host = self.PrivateMemorySize64;
            var tree = host;
            var seen = new HashSet<int> { self.Id };
            var kinds = new List<string>();

            foreach (var environment in environments)
            {
                if (environment is null) { continue; }
                foreach (var info in environment.GetProcessInfos())
                {
                    if (!seen.Add(info.ProcessId)) { continue; }
                    try
                    {
                        using var process = Process.GetProcessById(info.ProcessId);
                        tree += process.PrivateMemorySize64;
                        kinds.Add($"{info.Kind}:{process.PrivateMemorySize64:N0}");
                    }
                    catch (ArgumentException)
                    {
                        // Exited between the listing and the read: not counted, and said so.
                        kinds.Add($"{info.Kind}:exited");
                    }
                    catch (InvalidOperationException)
                    {
                        kinds.Add($"{info.Kind}:exited");
                    }
                }
            }

            return new Bytes(host, tree, string.Join(", ", kinds));
        }

        private static void Configure(ComposerSurface surface)
        {
            var root = Path.Combine(Path.GetTempPath(), "aide-sandbox-probe", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            surface.Configure(
                new SessionConfig("s-probe", "probe", "w-probe", DateTimeOffset.UnixEpoch, [new AccountRef("anthropic", "max")], new AccountRef("anthropic", "max")),
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

        private static async Task<bool> WaitAsync(Func<bool> settled)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
            while (DateTime.UtcNow < deadline)
            {
                if (settled()) { return true; }
                await Task.Delay(100);
            }

            return settled();
        }

        private static async Task<bool> WaitAsync(Func<Task<bool>> settled)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
            while (DateTime.UtcNow < deadline)
            {
                if (await settled()) { return true; }
                await Task.Delay(100);
            }

            return await settled();
        }

        private static WebView2? FindWebView(DependencyObject node)
        {
            if (node is WebView2 found) { return found; }
            for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
            {
                if (FindWebView(System.Windows.Media.VisualTreeHelper.GetChild(node, i)) is { } child) { return child; }
            }

            return null;
        }

        private sealed class NeverAsked : IAttachmentAffirmation
        {
            public bool Confirm(OutsideWorkspaceAffirmation affirmation) => false;
        }
    }
}
