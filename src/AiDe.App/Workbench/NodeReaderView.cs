using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Presentation;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// The reader half of the Explorer surface (spec-knowledge-explorer-mode US-E4; design D4): a
/// selected node's header, metadata and walkable typed edges, and — below them, on the
/// <b>View source</b> gesture (Ruling 93) — the node's content through ADR-0018's node-content
/// query, rendered by the authority's <c>RenderKind</c>: code read-only and highlighted (the
/// AvalonEdit viewer ADR-0025 chose), markdown as prose (links are text), HTML in a sandbox (script
/// off, no navigation, no network — <see cref="HtmlSandboxHost"/>), and <c>None</c> as the
/// shortfall sentence, verbatim. With no selection it shows an explicit empty state (US-E7).
/// </summary>
/// <remarks>
/// <b>Two rows, one reader.</b> The top row (header · metadata · edges) scrolls on its own and is
/// capped at <see cref="TopShare"/> of the reader once content is shown, so the content keeps a
/// room of its own instead of being pushed below twenty-five edge rows (the writer-room lesson,
/// DC-137, applied to a reader). With nothing shown the top row takes the whole reader.
/// </remarks>
public sealed class NodeReaderView : ContentControl, IDisposable
{
    private Action<string>? _onWalk;
    private readonly List<UIElement> _focusStops = new();
    private readonly Grid _layout = new();
    private readonly ScrollViewer _top = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly ContentControl _contentArea = new() { IsTabStop = false };
    private CodeViewerView? _code;
    private HtmlSandboxHost? _html;
    private NodeContent? _htmlShown;
    private bool _disposed;

    /// <summary>The share of the reader the top row may take once content is shown.</summary>
    internal const double TopShare = 0.45;

    /// <summary>The top row's floor once content is shown, so the header is never squeezed away.</summary>
    internal const double TopMinHeight = 120;

    /// <summary>The top row's cap: unbounded with no content, a share of the reader with it.</summary>
    internal static double TopMaxHeight(double readerHeight, bool contentShown) =>
        contentShown && readerHeight > 0
            ? Math.Max(TopMinHeight, Math.Floor(readerHeight * TopShare))
            : double.PositiveInfinity;

    /// <summary>
    /// The node-content query (ADR-0018's <c>NodeContentAsync</c>, behind the client seam), read
    /// live on each gesture so a reader built before a workspace attached still reaches it (DC-040).
    /// </summary>
    public Func<string, CancellationToken, Task<NodeContent>>? ContentSource { get; set; }

    /// <summary>What the content area shows (Ruling 93).</summary>
    public NodeReaderContentState ContentState { get; private set; } = NodeReaderContentState.Idle;

    /// <summary>
    /// The "View source" gesture for the node the reader is showing: fetches its content through
    /// <see cref="ContentSource"/> and renders it by kind. A reply for a node that is no longer the
    /// shown one is discarded (ADR-0018 echoes the id for exactly this).
    /// </summary>
    public async Task ViewSourceAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(SelectedNodeId, nodeId, StringComparison.Ordinal)) { return; }

        var source = ContentSource;
        if (source is null) { return; }

        SetContent(NodeReaderContentState.Loading, () => Sentence("Loading source…"));

        NodeContent content;
        try
        {
            content = await source(nodeId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (string.Equals(SelectedNodeId, nodeId, StringComparison.Ordinal)) { SetIdle(); }
            return;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            if (string.Equals(SelectedNodeId, nodeId, StringComparison.Ordinal))
            {
                SetContent(NodeReaderContentState.Failed, () => Sentence("The source could not be loaded: " + ex.Message));
            }

            return;
        }

        ShowContent(content);
    }

    /// <summary>
    /// Renders one node's content in the content area, by its kind. A reply whose id is not the
    /// shown node's is discarded: the selection moved on while the query was in flight.
    /// </summary>
    public void ShowContent(NodeContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!string.Equals(SelectedNodeId, content.NodeId, StringComparison.Ordinal)) { return; }

        switch (content.RenderKind)
        {
            case NodeContentKind.None:
                SetContent(NodeReaderContentState.None, () => Sentence(content.Shortfall ?? "No inline content for this node."));
                break;

            case NodeContentKind.Text when string.Equals(content.Language, "markdown", StringComparison.OrdinalIgnoreCase):
                SetContent(NodeReaderContentState.Markdown, () => WithShortfall(content.Shortfall, new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Padding = new Thickness(16, 8, 16, 14),
                    Content = new ProseView { Text = content.Content },
                }));
                break;

            case NodeContentKind.Html:
                ShowHtml(content);
                break;

            case NodeContentKind.Text:
                SetContent(NodeReaderContentState.Text, () => Code(content), CodeFocusTarget());
                break;

            default:
                SetContent(NodeReaderContentState.Code, () => Code(content), CodeFocusTarget());
                break;
        }
    }

    private void ShowHtml(NodeContent content)
    {
        if (_html is { Unavailable: { } reason })
        {
            ShowHtmlFallback(content, reason);
            return;
        }

        if (_html is null)
        {
            var host = new HtmlSandboxHost("explorer-reader-html");
            // The runtime reports once, at start; a host that failed stays failed, so every later
            // HTML node takes the fallback at once rather than waiting on a sandbox that never comes.
            host.SandboxUnavailable += why =>
            {
                if (ContentState == NodeReaderContentState.Html && SelectedNodeId is { } id && _htmlShown is { } shown && shown.NodeId == id)
                {
                    ShowHtmlFallback(shown, why);
                }
            };
            _html = host;
        }

        _htmlShown = content;
        _html.Show(content.Content);
        SetContent(NodeReaderContentState.Html, () => WithShortfall(content.Shortfall, _html), _html.View);
    }

    private void ShowHtmlFallback(NodeContent content, string reason)
    {
        var fallback = content with { RenderKind = NodeContentKind.Code, Language = "html" };
        SetContent(
            NodeReaderContentState.HtmlFallback,
            () => WithShortfall("Shown as source — the HTML sandbox is not available: " + reason, Code(fallback)),
            CodeFocusTarget());
    }

    private CodeViewerView Code(NodeContent content)
    {
        _code ??= new CodeViewerView("Source");
        _code.Show(content);
        return _code;
    }

    private UIElement CodeFocusTarget() => (_code ??= new CodeViewerView("Source")).FocusTarget;

    /// <summary>The authority's shortfall above the content, when there is one.</summary>
    private static UIElement WithShortfall(string? shortfall, UIElement body)
    {
        if (string.IsNullOrEmpty(shortfall)) { return body; }

        var panel = new DockPanel { LastChildFill = true };
        var banner = Muted(shortfall, 12);
        banner.Margin = new Thickness(16, 8, 16, 4);
        DockPanel.SetDock(banner, Dock.Top);
        panel.Children.Add(banner);
        panel.Children.Add(body);
        return panel;
    }

    private static TextBlock Sentence(string text)
    {
        var block = Muted(text, 12.5);
        block.Margin = new Thickness(16, 10, 16, 14);
        AutomationProperties.SetLiveSetting(block, AutomationLiveSetting.Polite);
        return block;
    }

    private void SetIdle() =>
        SetContent(NodeReaderContentState.Idle, () => Sentence("Right-click the node and choose View source to read it here."));

    /// <summary>
    /// Places what <paramref name="build"/> makes in the content area and re-caps the top row. The focus
    /// stops end at <paramref name="focusable"/> when the content can take focus, so the Tab
    /// boundary (spec US-E7/E8) stays truthful.
    /// </summary>
    private void SetContent(NodeReaderContentState state, Func<UIElement> build, UIElement? focusable = null)
    {
        ContentState = state;

        // Release the previous element FIRST: a retained renderer (the code viewer, the sandbox
        // host) may sit inside a wrapper the previous state built, and it can only be parented
        // into the next one once it has left the last.
        if (_contentArea.Content is UIElement old)
        {
            _contentArea.Content = null;
            if (old is Panel wrapper) { wrapper.Children.Clear(); }
        }

        _contentArea.Content = build();

        _focusStops.RemoveAll(stop => !ReferenceEquals(stop, this) && stop is not Button);
        if (focusable is not null) { _focusStops.Add(focusable); }

        _top.MaxHeight = TopMaxHeight(ActualHeight, state != NodeReaderContentState.Idle);
    }

    /// <summary>Releases the HTML sandbox's browser (a child process), if one was ever created.</summary>
    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        _html?.Dispose();
    }

    public NodeReaderView()
    {
        SetResourceReference(BackgroundProperty, "SurfaceBrush");
        AutomationProperties.SetName(this, "Node reader");

        _layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(_top, 0);
        Grid.SetRow(_contentArea, 1);
        _layout.Children.Add(_top);
        _layout.Children.Add(_contentArea);
        SizeChanged += (_, e) => _top.MaxHeight = TopMaxHeight(e.NewSize.Height, ContentState != NodeReaderContentState.Idle);
        // Focusable so a Tab off the graph canvas can land here even when the reader is empty — the
        // canvas is a keyboard trap (ADR-0015) and the reader is its escape while Explorer is active.
        Focusable = true;
        IsTabStop = false;
        // Phase 3: close the graph↔reader cycle. Tab off the reader's last stop and Shift+Tab off its
        // first stop hand focus back to the graph, so the two panes form one keyboard loop with no
        // trap and no ejection (spec US-E7/E8 a11y contract).
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Tab) { return; }
            var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            if (HandleTabKey(Keyboard.FocusedElement, shift)) { e.Handled = true; }
        };
        Clear();
    }

    /// <summary>The id of the node currently shown, or null when empty.</summary>
    public string? SelectedNodeId { get; private set; }

    public bool IsEmpty => SelectedNodeId is null;

    /// <summary>How many typed edges (walk targets) the reader is currently offering.</summary>
    public int WalkableEdgeCount { get; private set; }

    /// <summary>Registers the walk handler; called with the target id when an edge is activated.</summary>
    public void OnWalk(Action<string> walk) => _onWalk = walk;

    /// <summary>
    /// Raised when keyboard focus should leave the reader and return to the graph, so the Explorer
    /// can complete the graph↔reader cycle (spec US-E7/E8). The direction says which boundary was
    /// crossed (Forward = Tab off the last stop; Backward = Shift+Tab off the first stop).
    /// </summary>
    public event EventHandler<CanvasFocusDirection>? FocusLeaveRequested;

    /// <summary>
    /// The reader's ordered focus stops: the region itself (the entry) followed by its walkable
    /// edge buttons. Exposed so the cycle boundary is testable without a rendered visual tree.
    /// </summary>
    public IReadOnlyList<UIElement> FocusStops => _focusStops;

    /// <summary>
    /// Given the focused element and whether Shift is held, returns the direction focus should leave
    /// the reader — or null when the Tab stays inside the reader. Shift+Tab at the first stop leaves
    /// Backward; Tab at the last stop leaves Forward. A single-stop reader (empty state) leaves either
    /// way, since there is nowhere else inside it to go.
    /// </summary>
    public CanvasFocusDirection? BoundaryLeave(object? focused, bool shift)
    {
        if (_focusStops.Count == 0) { return null; }
        var first = _focusStops[0];
        var last = _focusStops[^1];
        if (shift && ReferenceEquals(focused, first)) { return CanvasFocusDirection.Backward; }
        if (!shift && ReferenceEquals(focused, last)) { return CanvasFocusDirection.Forward; }
        return null;
    }

    /// <summary>
    /// Handles a Tab keypress at the reader boundary: raises <see cref="FocusLeaveRequested"/> and
    /// returns true (the caller marks the event handled) when the Tab crosses a boundary; false when
    /// it stays inside the reader.
    /// </summary>
    public bool HandleTabKey(object? focused, bool shift)
    {
        var dir = BoundaryLeave(focused, shift);
        if (dir is null) { return false; }
        FocusLeaveRequested?.Invoke(this, dir.Value);
        return true;
    }

    /// <summary>
    /// Moves keyboard focus into the reader region so a Tab off the graph canvas lands here rather
    /// than being swallowed by the canvas's keyboard trap (design D3/Phase-3 interim). From the reader
    /// — a normal WPF region — Tab then traverses onward as usual, so the graph is no longer a trap.
    /// </summary>
    public bool FocusReader()
    {
        if (MoveFocus(new TraversalRequest(FocusNavigationDirection.First)))
        {
            return true;
        }

        // Empty reader (no edge to focus): focus the region itself so the user is still out of the
        // canvas trap and can Tab onward.
        return Focus();
    }

    /// <summary>
    /// Moves keyboard focus to the reader's LAST stop — used when the graph is left Backward
    /// (Shift+Tab off the graph's first node), so the cycle lands the user on the reader's end rather
    /// than its start (spec US-E7/E8 cycle).
    /// </summary>
    public bool FocusReaderLast()
    {
        if (_focusStops.Count > 0 && _focusStops[^1].Focus())
        {
            return true;
        }

        return Focus();
    }

    public void Clear()
    {
        SelectedNodeId = null;
        WalkableEdgeCount = 0;
        _focusStops.Clear();
        _focusStops.Add(this);
        SetIdle();
        Content = EmptyState();
    }

    public void Show(CanvasNode node, IReadOnlyList<CanvasEdge> edges)
    {
        if (node is null)
        {
            Clear();
            return;
        }

        SelectedNodeId = node.Id;
        var visible = edges ?? [];
        WalkableEdgeCount = visible.Count(e => e.From == node.Id || e.To == node.Id);
        // Rebuild the cycle's focus stops: the region itself (entry), then each walkable edge button
        // in order. Build appends the buttons.
        _focusStops.Clear();
        _focusStops.Add(this);
        _top.Content = Build(node, visible);
        // Content belongs to a node: a new selection starts idle, and a reply still in flight for
        // the previous one is discarded by ShowContent's id check.
        SetIdle();
        Content = _layout;
    }

    private static UIElement EmptyState()
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24),
        };
        panel.Children.Add(Muted("Select a node to read it.", 14, FontWeights.SemiBold, center: true));
        panel.Children.Add(Muted(
            "Click a dot in the graph — its metadata and edges appear here.", 12.5, center: true));
        return panel;
    }

    private UIElement Build(CanvasNode node, IReadOnlyList<CanvasEdge> edges)
    {
        var root = new StackPanel { Margin = new Thickness(16, 14, 16, 14) };

        // Prefer the specific has_type (azure-resource, table, class) over the node's coarse kind
        // (the dimensional source-vs-knowledge classification), so a bicep resource reads
        // "azure-resource" rather than the confusing "knowledge".
        var typeLabel = node.Kind;
        var typeEdge = edges.FirstOrDefault(e =>
            string.Equals(e.Predicate, "has_type", StringComparison.OrdinalIgnoreCase)
            && (e.From == node.Id || e.To == node.Id));
        if (typeEdge is not null)
        {
            var t = string.Equals(typeEdge.From, node.Id, StringComparison.Ordinal) ? typeEdge.To : typeEdge.From;
            if (!string.IsNullOrWhiteSpace(t)) { typeLabel = t; }
        }

        // Header: title + type.
        root.Children.Add(Text(node.Label, 15, FontWeights.SemiBold));
        root.Children.Add(Muted(typeLabel + (node.Context is { Length: > 0 } c ? "  ·  " + c : ""), 12));

        // Metadata.
        root.Children.Add(Divider());
        root.Children.Add(MetaRow("id", node.Id));
        root.Children.Add(MetaRow("type", typeLabel));
        root.Children.Add(MetaRow("context", node.Context ?? "—"));

        // Typed edges — the walk affordance (US-E4/E5).
        root.Children.Add(Divider());
        root.Children.Add(Muted("Typed edges — select to walk", 11, FontWeights.SemiBold));

        var touching = edges.Where(e => e.From == node.Id || e.To == node.Id).ToList();
        if (touching.Count == 0)
        {
            root.Children.Add(Muted("No linked artifacts in view.", 12));
        }
        else
        {
            foreach (var edge in touching)
            {
                var outgoing = edge.From == node.Id;
                var target = outgoing ? edge.To : edge.From;
                var rel = (outgoing ? "" : "← ") + edge.Predicate;
                var button = EdgeRow(rel, target, edge.Status);
                _focusStops.Add(button);
                root.Children.Add(button);
            }
        }

        root.Children.Add(Divider());
        return root;
    }

    /// <summary>The one size every block of an edge row renders at (Ruling 92: one baseline).</summary>
    internal const double EdgeRowFontSize = 12;

    /// <summary>The predicate column's minimum width, so the targets align down the list.</summary>
    internal const double EdgeRowPredicateMinWidth = 104;

    /// <summary>
    /// One typed edge as a walkable row (Ruling 92): a three-column grid — predicate (Auto, min
    /// <see cref="EdgeRowPredicateMinWidth"/>) · target (*) · status (Auto) — every block at
    /// <see cref="EdgeRowFontSize"/> on one baseline, the status told apart by the muted ink and
    /// never by a smaller size, inside a button whose own template stretches its presenter. The
    /// App's implicit Button style centres its presenter and ignores
    /// <c>HorizontalContentAlignment</c>, which is what floated every row to the middle and made
    /// the status a superscript beside a larger target — so the row carries its template rather
    /// than depending on the style.
    /// </summary>
    private Button EdgeRow(string rel, string target, string status)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = EdgeRowPredicateMinWidth });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var relText = Muted(rel, EdgeRowFontSize);
        relText.TextWrapping = TextWrapping.NoWrap;
        relText.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(relText, 0);
        row.Children.Add(relText);

        var targetText = Text(target, EdgeRowFontSize, FontWeights.Normal);
        targetText.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(targetText, 1);
        row.Children.Add(targetText);

        var statusText = Muted(status, EdgeRowFontSize);
        statusText.TextWrapping = TextWrapping.NoWrap;
        statusText.VerticalAlignment = VerticalAlignment.Center;
        statusText.Margin = new Thickness(12, 1, 0, 1);
        Grid.SetColumn(statusText, 2);
        row.Children.Add(statusText);

        var button = new Button
        {
            Content = row,
            Padding = new Thickness(0, 4, 0, 4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = target,
            Template = EdgeRowTemplate(),
        };
        AutomationProperties.SetName(button, $"Walk {rel} {target}");
        button.Click += (_, _) =>
        {
            if (button.Tag is string id)
            {
                _onWalk?.Invoke(id);
            }
        };
        return button;
    }

    /// <summary>
    /// The edge row's own button template: a chrome border whose presenter <b>stretches</b>, the
    /// hover ground and the focus ring the App's chrome template gives every other button. Built
    /// with <see cref="FrameworkElementFactory"/>, whose part names the template's own name scope
    /// registers (DC-166 is the Style built without one).
    /// </summary>
    private static ControlTemplate EdgeRowTemplate()
    {
        var root = new FrameworkElementFactory(typeof(Grid));

        // The chrome carries the padding and the hover ground; its border is zero so the row's
        // first column starts exactly where the metadata labels start.
        var chrome = new FrameworkElementFactory(typeof(Border), "Chrome");
        chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        chrome.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        chrome.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        chrome.SetValue(Border.BorderThicknessProperty, new Thickness(0));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter), "Content");
        presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        chrome.AppendChild(presenter);
        root.AppendChild(chrome);

        // The focus ring is an overlay drawn outside the chrome (the App's own idiom), never a
        // border that would shift the content.
        var ring = new FrameworkElementFactory(typeof(Border), "FocusRing");
        ring.SetValue(FrameworkElement.MarginProperty, new Thickness(-1));
        ring.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        ring.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        ring.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        ring.SetValue(UIElement.IsHitTestVisibleProperty, false);
        root.AppendChild(ring);

        var template = new ControlTemplate(typeof(Button)) { VisualTree = root };

        var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("MenuHoverBrush"), "Chrome"));
        template.Triggers.Add(hover);

        var focused = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
        focused.Setters.Add(new Setter(Border.BorderBrushProperty, new DynamicResourceExtension("FocusBrush"), "FocusRing"));
        template.Triggers.Add(focused);

        return template;
    }

    private UIElement MetaRow(string key, string value)
    {
        var panel = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };
        var k = Muted(key, 12);
        k.MinWidth = 72;
        DockPanel.SetDock(k, Dock.Left);
        panel.Children.Add(k);
        panel.Children.Add(Text(value, 12, FontWeights.Normal, wrap: true));
        return panel;
    }

    private static TextBlock Text(string text, double size, FontWeight weight, bool wrap = false)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextTrimming = wrap ? TextTrimming.None : TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 1, 0, 1),
        };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return block;
    }

    private static TextBlock Muted(string text, double size, FontWeight? weight = null, bool center = false)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight ?? FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 1),
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Left,
        };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return block;
    }

    private static Border Divider()
    {
        var border = new Border { Height = 1, Margin = new Thickness(0, 10, 0, 10) };
        border.SetResourceReference(Border.BackgroundProperty, "BorderBrush");
        return border;
    }
}
