using System.IO;
using System.Windows;
using System.Windows.Threading;
using AiDe.App.Workbench;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.WebHostProbe;

/// <summary>
/// <b>F1's exit clause, out of process:</b> serve the vendored CodeMirror ES-module bundle to a real
/// <see cref="WebView2"/> over <c>SetVirtualHostNameToFolderMapping</c>, and require that the module
/// actually ran.
/// </summary>
/// <remarks>
/// <para><b>Why not headless Chromium.</b> The spike already proved CodeMirror 6 runs in Chromium
/// over HTTP. What was never proven is that it runs in the control this shell hosts: the existing
/// host uses <c>NavigateToString</c>, which cannot serve an ES module at all. Proving it somewhere
/// else would be a control that cannot fail for the reason it exists (<b>DC-016</b>).</para>
///
/// <para><b>The exit code is the assertion,</b> and every failure gets its own so the test that
/// launched this can say what went wrong without reading a log. The three interesting ones are
/// <c>3</c> (the module never executed — the mapping or the MIME handling is wrong), <c>4</c> (the
/// editor did not mount) and <c>5</c> (the editor mounted but the bundle's OWN view plugin produced
/// nothing, which is the vacuity guard).</para>
/// </remarks>
internal static class Program
{
    private const int Ok = 0;
    private const int PageNeverLoaded = 2;
    private const int ModuleNeverExecuted = 3;
    private const int EditorNeverRendered = 4;
    private const int BundlePluginNeverRan = 5;
    private const int Crashed = 6;
    private const int CouldNotEvaluate = 7;

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
        var view = new WebView2();
        var window = new Window
        {
            Title = "AiDe web-host probe",
            Content = view,
            Width = 900,
            Height = 600,
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

        // A hung probe must not hang the suite that launched it.
        var guard = new DispatcherTimer(
            TimeSpan.FromSeconds(120), DispatcherPriority.Normal,
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

        var root = WebAssetHost.Map(view.CoreWebView2);
        var url = WebAssetHost.Url("composer-host.html");
        Console.Out.WriteLine($"mapped {WebAssetHost.VirtualHostName} -> {root}");
        Console.Out.WriteLine($"navigating to {url}");

        // The bundle must be WHERE the mapping points, or a 404 would be indistinguishable from a
        // module that failed to parse.
        var bundle = Path.Combine(root, "vendor", "codemirror-composer.bundle.mjs");
        if (!File.Exists(bundle))
        {
            Console.Error.WriteLine($"the vendored bundle is not in the build output at {bundle}");
            return PageNeverLoaded;
        }

        var navigated = new TaskCompletionSource<bool>();
        view.NavigationCompleted += (_, e) => navigated.TrySetResult(e.IsSuccess);
        view.CoreWebView2.Navigate(url);

        var finished = await Task.WhenAny(navigated.Task, Task.Delay(TimeSpan.FromSeconds(45)));
        if (finished != navigated.Task || !navigated.Task.Result)
        {
            Console.Error.WriteLine("the page never loaded over the virtual host");
            return PageNeverLoaded;
        }

        // The module is loaded with a top-level await, so NavigationCompleted can fire before it has
        // finished. Poll the flag the page sets rather than sleeping a guessed interval.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        var ready = "false";
        while (DateTime.UtcNow < deadline)
        {
            ready = await Eval(view, "String(window.__composerReady === true)");
            if (ready == "true") break;
            await Task.Delay(100);
        }

        var error = await Eval(view, "String(window.__composerError || '')");

        // DC-016: "I could not ask" must never read as "nothing rendered". This probe's Eval returns
        // a marker string on failure, and that marker is neither "0" nor "", so a count guard alone
        // would wave a dead page straight through.
        if (ready.StartsWith("(evaluate failed", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"the page could not be evaluated, so nothing below was measured: {ready}");
            return CouldNotEvaluate;
        }

        if (ready != "true")
        {
            Console.Error.WriteLine(
                "the vendored ES module never executed inside the real WebView2 host. " +
                $"window.__composerError = {error}");
            return ModuleNeverExecuted;
        }

        var editors = await Eval(view, "String(document.querySelectorAll('.cm-editor').length)");
        var chips = await Eval(view, "String(document.querySelectorAll('.cm-mention-chip').length)");
        var lines = await Eval(view, "String(document.querySelectorAll('.cm-line').length)");

        if (editors.StartsWith("(evaluate failed", StringComparison.Ordinal)
            || chips.StartsWith("(evaluate failed", StringComparison.Ordinal)
            || lines.StartsWith("(evaluate failed", StringComparison.Ordinal))
        {
            Console.Error.WriteLine(
                $"the page could not be evaluated: editors={editors}, chips={chips}, lines={lines}");
            return CouldNotEvaluate;
        }

        Console.Out.WriteLine($"editors: {editors}, mention chips: {chips}, rendered lines: {lines}");

        if (editors is "0" or "")
        {
            Console.Error.WriteLine("the module ran but no CodeMirror editor mounted");
            return EditorNeverRendered;
        }

        // NON-VACUITY. An editor element proves a class name. `.cm-mention-chip` is produced by the
        // ViewPlugin compiled INTO the bundle, so a chip proves the bundle's own code executed —
        // which an empty div with the right class could never do.
        if (chips is "0" or "")
        {
            Console.Error.WriteLine(
                "an editor mounted but the bundle's own mention-chip plugin produced nothing — " +
                "this run would pass vacuously");
            return BundlePluginNeverRan;
        }

        Console.Out.WriteLine(
            $"the vendored bundle rendered in the real WebView2 host over https://{WebAssetHost.VirtualHostName}");
        return Ok;
    }

    /// <summary>
    /// Reads a value out of the page, returning a marker rather than throwing, so the caller can tell
    /// "the answer is zero" from "I could not ask" (the distinction DC-016 is about).
    /// </summary>
    private static async Task<string> Eval(WebView2 view, string script)
    {
        try
        {
            var raw = await view.CoreWebView2.ExecuteScriptAsync(script);
            return raw?.Trim('"') ?? "";
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return $"(evaluate failed: {ex.Message})";
        }
    }
}
