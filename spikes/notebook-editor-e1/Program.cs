using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;

namespace NotebookEditorE1Spike;

/// <summary>
/// <c>dotnet run --project spikes/notebook-editor-e1 -- [--check a,b,...] [--out result.json]</c>.
/// Exit 0: every selected check passed. 1: at least one failed. 2: the harness could not start.
/// </summary>
internal static class Program
{
    private const int Passed = 0;
    private const int Failed = 1;
    private const int CouldNotRun = 2;

    [STAThread]
    private static int Main(string[] args)
    {
        var selected = Arg(args, "--check")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Checks.All;
        var output = Arg(args, "--out");
        var unknown = selected.Except(Checks.All).ToArray();
        if (unknown.Length > 0)
        {
            Console.Error.WriteLine($"unknown check(s): {string.Join(",", unknown)}; known: {string.Join(",", Checks.All)}");
            return CouldNotRun;
        }

        // An explicit user-data folder: the default is beside the host executable, which under
        // `dotnet <dll>` is Program Files and is refused (E_ACCESSDENIED, observed on the first run).
        var view = new WebView2
        {
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(AppContext.BaseDirectory, "webview2-data"),
            },
        };
        var window = new Window
        {
            Title = "E-1 notebook editor spike",
            Content = view,
            Width = 1000,
            Height = 700,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };

        var exit = CouldNotRun;
        var results = new List<CheckResult>();
        window.Loaded += async (_, _) =>
        {
            try
            {
                var page = new Page(view);
                await page.InitAsync();
                if (!await page.NavigateAsync())
                {
                    Console.Error.WriteLine("the page never became ready over the virtual host");
                    return;
                }

                var checks = new Checks(page, window, new WindowInteropHelper(window).Handle);
                foreach (var name in selected)
                {
                    var result = await checks.RunAsync(name);
                    results.Add(result);
                    Print(result);
                }

                exit = results.All(r => r.Pass) ? Passed : Failed;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
            finally
            {
                window.Close();
            }
        };

        window.Show();
        var frame = new DispatcherFrame();
        window.Closed += (_, _) => frame.Continue = false;
        var guard = new DispatcherTimer(TimeSpan.FromMinutes(10), DispatcherPriority.Normal, (_, _) => frame.Continue = false, Dispatcher.CurrentDispatcher);
        guard.Start();
        Dispatcher.PushFrame(frame);
        guard.Stop();
        Console.Out.WriteLine("window closed; disposing the WebView2");
        view.Dispose();

        if (output is not null)
        {
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                at = DateTimeOffset.UtcNow,
                exit,
                webview2Runtime = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString(),
                results,
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        Console.Out.WriteLine($"E-1 exit {exit}: {results.Count(r => r.Pass)}/{results.Count} passed");
        return exit;
    }

    private static void Print(CheckResult result)
    {
        Console.Out.WriteLine($"== {result.Name}: {(result.Pass ? "PASS" : "FAIL")} — {result.Value}");
        Console.Out.WriteLine($"   method: {result.Method}");
        foreach (var line in result.Detail)
        {
            Console.Out.WriteLine($"   | {line}");
        }
    }

    private static string? Arg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
