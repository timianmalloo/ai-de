using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace NotebookEditorE1Spike;

/// <summary>
/// The page under test, seen from the host: navigation over a virtual host (the composer's hosting
/// shape), and a typed read of the page's <c>window.nb</c> probe surface. The host only reads state
/// and positions the caret through it; every edit a check asserts on arrives as native input.
/// </summary>
internal sealed class Page(WebView2 view)
{
    public const string Host = "nb.spike.invalid";

    public WebView2 View { get; } = view;

    /// <summary>Messages the page posts (the latency harness's paint stamps), with host receipt time.</summary>
    public List<(long HostTicks, string Json)> Posted { get; } = [];

    public async Task InitAsync()
    {
        await View.EnsureCoreWebView2Async();
        var root = Path.Combine(AppContext.BaseDirectory, "page");
        View.CoreWebView2.SetVirtualHostNameToFolderMapping(Host, root, CoreWebView2HostResourceAccessKind.DenyCors);
        View.CoreWebView2.WebMessageReceived += (_, e) => Posted.Add((Stopwatch.GetTimestamp(), e.WebMessageAsJson));
    }

    public async Task<bool> NavigateAsync()
    {
        var done = new TaskCompletionSource<bool>();
        void Handler(object? s, CoreWebView2NavigationCompletedEventArgs e) => done.TrySetResult(e.IsSuccess);
        View.NavigationCompleted += Handler;
        View.CoreWebView2.Navigate($"https://{Host}/notebook.html");
        var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(30)));
        View.NavigationCompleted -= Handler;
        if (finished != done.Task || !done.Task.Result)
        {
            return false;
        }

        return await WaitAsync("window.nb && window.nb.ready === true", TimeSpan.FromSeconds(20));
    }

    public async Task<JsonElement> EvalAsync(string js)
    {
        var raw = await View.CoreWebView2.ExecuteScriptAsync(js);
        return JsonDocument.Parse(raw).RootElement.Clone();
    }

    public async Task<string> DocAsync() => (await EvalAsync("nb.getDoc()")).GetString() ?? string.Empty;

    public async Task<bool> WaitAsync(string predicate, TimeSpan timeout)
    {
        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        while (Stopwatch.GetTimestamp() < deadline)
        {
            var value = await EvalAsync($"(function(){{ try {{ return !!({predicate}); }} catch (e) {{ return false; }} }})()");
            if (value.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return false;
    }

    public static string Js(string text) => JsonSerializer.Serialize(text);

    public static string Sha(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>A document position as a physical screen point (PerMonitorV2: PointToScreen returns device pixels).</summary>
    public async Task<(int X, int Y)> ScreenPointAsync(int pos)
    {
        var c = await EvalAsync($"nb.coords({pos})");
        var screen = View.PointToScreen(new Point(c.GetProperty("x").GetDouble(), c.GetProperty("y").GetDouble()));
        return ((int)Math.Round(screen.X), (int)Math.Round(screen.Y));
    }
}
