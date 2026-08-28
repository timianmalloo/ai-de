using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.Presentation;
using AiDe.Core.Workbench;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.Workbench;

/// <summary>
/// The graph canvas: a windowed WebView2 pane, with focus routed explicitly in both directions.
/// </summary>
/// <remarks>
/// <para><b>Windowed, not composition (ADR-0015).</b> The composition control fixes the airspace
/// limitation and then kills the process with a native access violation when its pane is floated —
/// a crash is a worse failure than an overlay that is not drawn, so the windowed control is kept and
/// overlaps are handled by swapping in a still frame.</para>
///
/// <para><b>The page's boundary handlers are the only way out.</b> WPF's Tab traversal cannot reach
/// or leave the canvas, so the page traps Tab on its last focusable element and Shift+Tab on its
/// first and posts <c>focus.leave</c>. A page that forgets them is a keyboard trap — which is why
/// this is a contract on the page rather than a nicety, and why <c>P2-FOCUS-03</c> exists.</para>
/// </remarks>
public sealed class CanvasSurface : ContentControl, IDisposable
{
    private readonly WebView2 _view;
    private bool _obscured;
    private bool _disposed;

    public CanvasSurface(string surfaceId, string title)
    {
        SurfaceId = surfaceId;
        _view = new WebView2();

        AutomationProperties.SetName(_view, $"{title} graph canvas");
        AutomationProperties.SetName(this, title);

        FocusTarget = new CanvasFocusTarget(_view, () => _obscured);
        Content = _view;

        _view.NavigationCompleted += async (_, _) =>
        {
            Ready = true;
            await RefreshAsync();
        };
        _view.WebMessageReceived += OnWebMessage;

        Loaded += async (_, _) => await InitialiseAsync();
    }

    public string SurfaceId { get; }

    /// <summary>The focus seam the router drives. Non-null from construction.</summary>
    public ICanvasFocusTarget FocusTarget { get; }

    /// <summary>True once the page has loaded and can accept focus.</summary>
    public bool Ready { get; private set; }

    /// <summary>Raised when the page reports that focus should leave the canvas.</summary>
    public event EventHandler<CanvasFocusDirection>? FocusLeaveRequested;

    /// <summary>
    /// Supplies the graph to draw. Null until a workspace attaches, in which case the page says so.
    /// </summary>
    public Func<string?, CancellationToken, Task<CanvasGraph>>? GraphSource { get; set; }

    /// <summary>Loads the graph around <paramref name="rootId"/> and pushes it to the page.</summary>
    public async Task RefreshAsync(string? rootId = null, CancellationToken cancellationToken = default)
    {
        if (!Ready) return;

        var graph = GraphSource is null
            ? new CanvasGraph([], [], null, 0, [], "No workspace is open. Open one to see its graph.")
            : await GraphSource(rootId, cancellationToken);

        // Serialised with the SAME options the incoming direction uses, so a field that survives one
        // way survives the other.
        _view.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(
            new { kind = "graph", graph }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    /// <summary>
    /// Shows a still frame in place of the live canvas for the duration of a drag (ADR-0015).
    /// While set, the canvas refuses focus and says why.
    /// </summary>
    public void SetObscured(bool obscured) => _obscured = obscured;

    private async Task InitialiseAsync()
    {
        if (_disposed) return;

        try
        {
            await _view.EnsureCoreWebView2Async();
            _view.NavigateToString(CanvasPage.Html);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A missing or broken WebView2 runtime must not take the shell down: the canvas is one
            // pane. It stays not-Ready, and workbench.focusCanvas refuses with a reason.
            Content = new TextBlock
            {
                Text = "The graph canvas could not start. " + ex.Message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(12),
            };
        }
    }

    private void OnWebMessage(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        // The page is first-party, but the message is still parsed defensively and mapped onto a
        // fixed vocabulary: acting on focus.leave MOVES FOCUS and grants nothing, so a message that
        // page content forged has no privileged effect.
        CanvasMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<CanvasMessage>(
                e.WebMessageAsJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return;
        }

        if (message is null) return;

        if (string.Equals(message.Kind, "node.activate", StringComparison.Ordinal))
        {
            // Re-rooting is a READ. The page names a node and the host asks the projection about it;
            // nothing the page can say grants it anything it did not already have.
            if (!string.IsNullOrWhiteSpace(message.NodeId))
            {
                _ = RefreshAsync(message.NodeId);
            }

            return;
        }

        if (!string.Equals(message.Kind, "focus.leave", StringComparison.Ordinal))
        {
            return;
        }

        var direction = message.Direction switch
        {
            "backward" => CanvasFocusDirection.Backward,
            "restore" => CanvasFocusDirection.Restore,
            _ => CanvasFocusDirection.Forward,
        };

        FocusLeaveRequested?.Invoke(this, direction);
    }

    private sealed record CanvasMessage(string Kind, string? Direction, string? NodeId);

    /// <summary>
    /// Sends a key to the page through the browser's own input layer.
    /// </summary>
    /// <remarks>
    /// <para>Uses the DevTools <c>Input.dispatchKeyEvent</c> rather than <c>SendInput</c>, because
    /// <c>SendInput</c> delivers to the FOREGROUND window and neither a test host nor a probe
    /// launched from a non-interactive shell can reliably hold it — measured: the page reported
    /// <c>activeElement="first"</c>, so focus had landed, while seeing <b>zero</b> Tab keydowns.</para>
    ///
    /// <para><b>What this is and is not.</b> CDP injects at the renderer's input layer, so the page
    /// and the browser's own focus traversal both see an ordinary key — which is what the
    /// keyboard-trap contract is about. It does not exercise the OS→browser hop, so it cannot catch
    /// a regression where the host swallows the key before the browser sees it. That gap is stated
    /// rather than papered over.</para>
    /// </remarks>
    public async Task<bool> SendKeyAsync(string key, int windowsVirtualKeyCode, bool shift = false)
    {
        var core = _view.CoreWebView2;
        if (core is null) return false;

        var modifiers = shift ? 8 : 0;
        foreach (var type in (string[])["rawKeyDown", "keyUp"])
        {
            var payload = JsonSerializer.Serialize(new
            {
                type,
                key,
                windowsVirtualKeyCode,
                nativeVirtualKeyCode = windowsVirtualKeyCode,
                modifiers,
            });

            try
            {
                await core.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", payload);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Runs script in the page and returns its result. Used by tests to diagnose input.</summary>
    public async Task<string> EvaluateAsync(string script)
    {
        try { return await _view.ExecuteScriptAsync(script); }
        catch (Exception ex) { return "(evaluate failed: " + ex.Message + ")"; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _view.Dispose();
    }

}
