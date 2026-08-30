using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// The full-window Explorer surface (spec-knowledge-explorer-mode; design D2): a graph region and a
/// reader region side by side, split by a draggable gutter. The graph is a dedicated
/// <see cref="CanvasSurface"/> (its own instance — the workbench's canvas is never reparented across
/// visual trees), and the reader follows the graph's selection through the
/// <see cref="CanvasSurface.NodeSelected"/> seam (D3), while activating a reader edge walks the graph
/// (US-E4/E5). Created once and retained by the mode controller, so a round-trip does not rebuild it.
/// </summary>
public sealed class ExplorerSurface : Grid
{
    public ExplorerSurface(CanvasSurface graph, NodeReaderView reader)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));

        AutomationProperties.SetName(this, "Knowledge Explorer");
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        // Graph (0) | splitter (1) | reader (2). Star columns so both resize; the split ratio the
        // splitter produces is what the user drags.
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.58, GridUnitType.Star), MinWidth = 320 });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.42, GridUnitType.Star), MinWidth = 280 });

        SetColumn(graph, 0);
        Children.Add(graph);

        var splitter = new GridSplitter
        {
            Width = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ResizeDirection = GridResizeDirection.Columns,
        };
        splitter.SetResourceReference(BackgroundProperty, "BorderBrush");
        AutomationProperties.SetName(splitter, "Resize graph and reader");
        SetColumn(splitter, 1);
        Children.Add(splitter);

        var readerHost = new Border
        {
            Child = reader,
            BorderThickness = new Thickness(1, 0, 0, 0),
        };
        readerHost.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        SetColumn(readerHost, 2);
        Children.Add(readerHost);

        // The reader follows the graph's selection, and a reader edge walks the graph — one selection,
        // two views, wired through the canvas seam so they cannot disagree (design D3).
        graph.NodeSelected += (_, selection) => reader.Show(selection.Node, selection.Edges);
        reader.OnWalk(targetId => _ = graph.RefreshAsync(targetId));

        // Phase 3 — the graph↔reader keyboard cycle (spec US-E7/E8). The canvas page traps Tab and
        // posts focus.leave at either boundary (ADR-0015); the Explorer routes that INTO the reader,
        // landing on the reader's first stop when the graph is left Forward and its last stop when
        // left Backward, so a keyboard user is never trapped in the canvas or ejected from the app.
        graph.FocusLeaveRequested += (_, direction) =>
        {
            if (direction == CanvasFocusDirection.Backward) { reader.FocusReaderLast(); }
            else { reader.FocusReader(); }
        };

        // The return leg: Tab off the reader's last stop and Shift+Tab off its first hand focus back
        // to the graph canvas, closing the cycle. Guarded so focus is not stolen while the canvas is
        // not ready or is showing a still frame (ADR-0015).
        reader.FocusLeaveRequested += (_, _) => ReturnFocusToGraph?.Invoke();
        ReturnFocusToGraph = () =>
            graph.FocusTarget is { IsReady: true, IsObscured: false } t && t.TryFocus();
    }

    public CanvasSurface Graph { get; }

    public NodeReaderView Reader { get; }

    /// <summary>
    /// The action that returns focus from the reader to the graph, completing the cycle. Defaults to
    /// focusing the graph canvas; replaceable so the routing is testable without a live WebView2.
    /// </summary>
    public Func<bool>? ReturnFocusToGraph { get; set; }
}
