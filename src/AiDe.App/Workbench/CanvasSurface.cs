using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.Presentation;
using AiDe.Core.Workbench;
using Microsoft.Web.WebView2.Wpf;

namespace AiDe.App.Workbench;

/// <summary>The node the canvas has re-rooted on, and the edges in view — for a reader to follow.</summary>
public sealed record CanvasNodeSelection(CanvasNode Node, IReadOnlyList<CanvasEdge> Edges);

/// <summary>A right-clicked node, with the type signals that decide which viewers it offers.</summary>
public sealed record NodeContextMenuRequest(string NodeId, string? NodeKind, bool IsKnowledge);

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
/// <summary>What a refresh actually did, so a caller can say something true about it.</summary>
/// <remarks>
/// <para><b>Why this is a return value and not a void.</b> The shell used to announce
/// <i>"Graph centred on X"</i> and then start the refresh fire-and-forget. Two independent things
/// could make that sentence false: the refresh opens with <c>if (!Ready) return;</c> and silently
/// does nothing while the WebView2 is still initialising, and a discarded task's fault is observed
/// by nobody. Measured by the design session on a real surface outside a window: <c>Ready</c> false,
/// the task completed, the graph source was asked <b>0</b> times — and the user had already been
/// told the graph centred on a node it never looked up.</para>
///
/// <para>A statement made BEFORE an action, about an action that may not happen, cannot be repaired
/// by wording. The refresh has to report, and the caller has to speak from the report.</para>
/// </remarks>
public enum CanvasRefreshOutcome
{
    /// <summary>The page was not ready; the requested root is held and will be applied on load.</summary>
    Deferred,

    /// <summary>No workspace is open, so there was no graph to centre.</summary>
    NoWorkspace,

    /// <summary>A plain redraw with no requested root.</summary>
    Refreshed,

    /// <summary>The requested node is in the drawn graph and is now the root.</summary>
    Centred,

    /// <summary>
    /// The refresh ran, but the requested node is not among the drawn nodes.
    /// </summary>
    /// <remarks>
    /// Not an error and not rare: the graph draws a bounded most-connected-first slice, and
    /// knowledge nodes have a measured median relation degree of 0, so a search hit the user picked
    /// may legitimately not be in view. Saying so is better than claiming a centring that did not
    /// happen.
    /// </remarks>
    NotInView,
}

/// <summary>The outcome of a refresh, with the label to use when speaking about it.</summary>
public readonly record struct CanvasRefresh(CanvasRefreshOutcome Outcome, string? Label);

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

            // A centring asked for while the page was loading is APPLIED here rather than dropped.
            // Without this the honest announcement would be "nothing happened", which is true and
            // useless; the request arrived, so the fix is to honour it, not to report its loss.
            var pending = _pendingRoot;
            _pendingRoot = null;

            await RefreshAsync(pending);
        };
        _view.WebMessageReceived += OnWebMessage;

        Loaded += async (_, _) => await InitialiseAsync();
    }

    /// <summary>A root requested before the page was ready, applied when it becomes ready.</summary>
    private string? _pendingRoot;

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

    /// <summary>
    /// Raised when the canvas re-roots on a specific node (a user activation), so a host — the
    /// Explorer reader (design D3) — can show that node without the graph and the reader keeping two
    /// definitions of "what is selected". Not raised for the initial unfocused overview.
    /// </summary>
    public event EventHandler<CanvasNodeSelection>? NodeSelected;

    /// <summary>Raised when a node is right-clicked, so the host can show the contextual "Open as…" menu.</summary>
    public event EventHandler<NodeContextMenuRequest>? NodeContextMenuRequested;

    /// <summary>Loads the graph around <paramref name="rootId"/> and pushes it to the page.</summary>
    // Sentinel roots the page uses to ask, through the ONE GraphSource seam, for a view that is not a
    // node neighbourhood: the grouped semantic-zoom overview, or one group's contents. The shell
    // recognises these and routes them to OverviewAsync / GroupAsync; a real node id never begins with
    // U+0001 (a control character), so there is no collision with a described node.
    internal const string GroupedOverviewRoot = "\u0001grouped";
    internal const string GroupRootPrefix = "\u0001group:";
    internal static string GroupRoot(string groupId) => GroupRootPrefix + groupId;

    public async Task<CanvasRefresh> RefreshAsync(
        string? rootId = null, CancellationToken cancellationToken = default)
    {
        if (!Ready)
        {
            // Hold the root, and say the request is deferred rather than letting the caller assume
            // it ran. A null root is a plain redraw and needs no holding — NavigationCompleted does
            // one anyway, which is what MainWindow's Explorer re-entry relies on.
            if (rootId is not null) _pendingRoot = rootId;

            return new CanvasRefresh(CanvasRefreshOutcome.Deferred, rootId);
        }

        var graph = GraphSource is null
            // No workspace, so no nodes and nothing for a count to be a fraction of.
            ? new CanvasGraph(
                [], [], null, 0, [], "No workspace is open. Open one to see its graph.",
                DeclaredByKind: null)
            : await GraphSource(rootId, cancellationToken);

        // Serialised with the SAME options the incoming direction uses, so a field that survives one
        // way survives the other.
        _view.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(
            new { kind = "graph", graph }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        // Reader-follows (design D3): a specific node was requested, so name the now-selected node.
        // The graph is rooted on it, so its own record and the neighbourhood edges are already here —
        // no second query, and the reader cannot disagree with the graph about the selection.
        if (rootId is null)
        {
            return new CanvasRefresh(
                GraphSource is null ? CanvasRefreshOutcome.NoWorkspace : CanvasRefreshOutcome.Refreshed,
                null);
        }

        var node = graph.Nodes.FirstOrDefault(n => string.Equals(n.Id, rootId, StringComparison.Ordinal))
            ?? graph.Nodes.FirstOrDefault(n => n.IsRoot);

        if (node is null)
        {
            // The drawn graph is the authority on what the user can see. It already knows the answer
            // — no second query, and the announcement cannot disagree with the picture.
            return new CanvasRefresh(
                GraphSource is null ? CanvasRefreshOutcome.NoWorkspace : CanvasRefreshOutcome.NotInView,
                rootId);
        }

        NodeSelected?.Invoke(this, new CanvasNodeSelection(node, graph.Edges));

        return new CanvasRefresh(CanvasRefreshOutcome.Centred, node.Label);
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

        if (string.Equals(message.Kind, "node.overview", StringComparison.Ordinal))
        {
            // Back to the whole-graph overview (rootId null -> the bounded overview projection). A READ,
            // like re-rooting: the page is asking for the same default view it gets on first load.
            _ = RefreshAsync(null);
            return;
        }

        if (string.Equals(message.Kind, "graph.grouped", StringComparison.Ordinal))
        {
            // Semantic-zoom top level: the workspace as groups, not nodes. A READ, routed through the
            // same GraphSource by a sentinel root the shell recognises (GroupedOverviewRoot).
            _ = RefreshAsync(GroupedOverviewRoot);
            return;
        }

        if (string.Equals(message.Kind, "group.open", StringComparison.Ordinal))
        {
            // Drill from a group super-node to its members. The sentinel carries the group id to the
            // shell, which asks the projection for exactly that group's contents (GraphQuery.GroupId).
            if (!string.IsNullOrWhiteSpace(message.NodeId))
            {
                _ = RefreshAsync(GroupRoot(message.NodeId));
            }

            return;
        }

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

        if (string.Equals(message.Kind, "node.contextmenu", StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(message.NodeId))
            {
                NodeContextMenuRequested?.Invoke(
                    this,
                    new NodeContextMenuRequest(message.NodeId, message.NodeKind, message.IsKnowledge));
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

    private sealed record CanvasMessage(
        string Kind, string? Direction, string? NodeId, string? NodeKind = null, bool IsKnowledge = false);

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
