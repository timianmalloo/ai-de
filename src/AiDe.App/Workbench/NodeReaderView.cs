using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AiDe.Core.Presentation;

namespace AiDe.App.Workbench;

/// <summary>
/// The reader half of the Explorer surface (spec-knowledge-explorer-mode US-E4; design D4). Phase 1
/// renders a selected node's header, metadata and its walkable typed edges. The per-kind CONTENT view
/// (rendered markdown/html, syntax-highlighted code) arrives in Phase 2 behind the node-content
/// contract (ADR-0018), so the content area is an honest placeholder until then — never a blank. With
/// no selection it shows an explicit empty state (US-E7).
/// </summary>
public sealed class NodeReaderView : ContentControl
{
    private Action<string>? _onWalk;

    public NodeReaderView()
    {
        SetResourceReference(BackgroundProperty, "SurfaceBrush");
        AutomationProperties.SetName(this, "Node reader");
        // Focusable so a Tab off the graph canvas can land here even when the reader is empty — the
        // canvas is a keyboard trap (ADR-0015) and the reader is its escape while Explorer is active.
        Focusable = true;
        IsTabStop = false;
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

    public void Clear()
    {
        SelectedNodeId = null;
        WalkableEdgeCount = 0;
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
        Content = Build(node, visible);
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

        // Header: title + kind.
        root.Children.Add(Text(node.Label, 15, FontWeights.SemiBold));
        root.Children.Add(Muted(node.Kind + (node.Context is { Length: > 0 } c ? "  ·  " + c : ""), 12));

        // Content placeholder — honest about what Phase 2 will add (ADR-0018).
        root.Children.Add(Divider());
        root.Children.Add(Muted(
            "Rich content (rendered markdown/html, syntax-highlighted code) arrives with the "
            + "node-content query (ADR-0018).", 12.5));

        // Metadata.
        root.Children.Add(Divider());
        root.Children.Add(MetaRow("id", node.Id));
        root.Children.Add(MetaRow("kind", node.Kind));
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
                root.Children.Add(EdgeRow(rel, target, edge.Status));
            }
        }

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = root,
        };
    }

    private Button EdgeRow(string rel, string target, string status)
    {
        var row = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 1, 0, 1) };
        var relText = Muted(rel, 12);
        relText.MinWidth = 104;
        DockPanel.SetDock(relText, Dock.Left);
        row.Children.Add(relText);
        var statusText = Muted(status, 11);
        statusText.HorizontalAlignment = HorizontalAlignment.Right;
        DockPanel.SetDock(statusText, Dock.Right);
        row.Children.Add(statusText);
        row.Children.Add(Text(target, 13, FontWeights.Normal));

        var button = new Button
        {
            Content = row,
            Padding = new Thickness(8, 5, 8, 5),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Tag = target,
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
