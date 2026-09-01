using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>How the Explorer arranges its two panes for the available width (US-E8).</summary>
public enum ExplorerLayout
{
    /// <summary>Graph and reader side by side, split by a vertical gutter (the wide default).</summary>
    SideBySide,

    /// <summary>Graph over reader, split by a horizontal gutter (below the width threshold).</summary>
    Stacked,
}

/// <summary>
/// The full-window Explorer surface (spec-knowledge-explorer-mode; design D2): a graph region and a
/// reader region split by a draggable gutter. The graph is a dedicated <see cref="CanvasSurface"/>
/// (its own instance — the workbench's canvas is never reparented across visual trees), and the reader
/// follows the graph's selection through the <see cref="CanvasSurface.NodeSelected"/> seam (D3), while
/// activating a reader edge walks the graph (US-E4/E5). Created once and retained by the mode
/// controller, so a round-trip does not rebuild it.
/// </summary>
/// <remarks>
/// <b>Responsive (US-E8).</b> Above <see cref="StackBelowWidth"/> the panes sit side by side; below it
/// they stack (graph over reader), so both halves stay usable on one narrow single-monitor window
/// rather than the reader being squeezed to its minimum. The layout is recomputed on size change and
/// is a pure function of width, so it is testable without rendering.
/// </remarks>
public sealed class ExplorerSurface : Grid
{
    private readonly GridSplitter _splitter;
    private readonly Border _readerHost;
    private bool _configured;

    public ExplorerSurface(CanvasSurface graph, NodeReaderView reader)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        Reader = reader ?? throw new ArgumentNullException(nameof(reader));

        AutomationProperties.SetName(this, "Knowledge Explorer");
        SetResourceReference(BackgroundProperty, "SurfaceBrush");

        Children.Add(graph);

        _splitter = new GridSplitter
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
        };
        _splitter.SetResourceReference(BackgroundProperty, "BorderBrush");
        AutomationProperties.SetName(_splitter, "Resize graph and reader");
        Children.Add(_splitter);

        _readerHost = new Border { Child = reader };
        _readerHost.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        Children.Add(_readerHost);

        // Start side-by-side (the wide default); SizeChanged corrects to stacked when narrow.
        ApplyLayout(ExplorerLayout.SideBySide);
        SizeChanged += (_, e) => ApplyLayoutForWidth(e.NewSize.Width);

        // The reader follows the graph's selection, and a reader edge walks the graph — one selection,
        // two views, wired through the canvas seam so they cannot disagree (design D3).
        graph.NodeSelected += (_, selection) => reader.Show(selection.Node, selection.Edges);
        reader.OnWalk(targetId => _ = graph.RefreshAsync(targetId));

        // Phase 3 — the graph<->reader keyboard cycle (spec US-E7/E8). The canvas page traps Tab and
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

    /// <summary>The width below which the panes stack instead of sitting side by side (US-E8).</summary>
    public double StackBelowWidth { get; set; } = 760;

    /// <summary>The current arrangement of the two panes.</summary>
    public ExplorerLayout Layout { get; private set; } = ExplorerLayout.SideBySide;

    /// <summary>
    /// The action that returns focus from the reader to the graph, completing the cycle. Defaults to
    /// focusing the graph canvas; replaceable so the routing is testable without a live WebView2.
    /// </summary>
    public Func<bool>? ReturnFocusToGraph { get; set; }

    /// <summary>
    /// Chooses the layout for a given available width and applies it if it changed. Pure function of
    /// width (a width of 0 — before first measure — keeps the side-by-side default). Public so the
    /// responsive rule is testable without a rendered window.
    /// </summary>
    public void ApplyLayoutForWidth(double width)
    {
        var target = width > 0 && width < StackBelowWidth ? ExplorerLayout.Stacked : ExplorerLayout.SideBySide;
        if (_configured && target == Layout) { return; }
        ApplyLayout(target);
    }

    private void ApplyLayout(ExplorerLayout layout)
    {
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();

        if (layout == ExplorerLayout.SideBySide)
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.58, GridUnitType.Star), MinWidth = 320 });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.42, GridUnitType.Star), MinWidth = 280 });

            SetColumn(Graph, 0); SetRow(Graph, 0);
            SetColumn(_splitter, 1); SetRow(_splitter, 0);
            SetColumn(_readerHost, 2); SetRow(_readerHost, 0);

            _splitter.Width = 6;
            _splitter.Height = double.NaN;
            _splitter.ResizeDirection = GridResizeDirection.Columns;
            _readerHost.BorderThickness = new Thickness(1, 0, 0, 0);   // divider on the reader's left
        }
        else
        {
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.58, GridUnitType.Star), MinHeight = 200 });
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.42, GridUnitType.Star), MinHeight = 160 });

            SetRow(Graph, 0); SetColumn(Graph, 0);
            SetRow(_splitter, 1); SetColumn(_splitter, 0);
            SetRow(_readerHost, 2); SetColumn(_readerHost, 0);

            _splitter.Height = 6;
            _splitter.Width = double.NaN;
            _splitter.ResizeDirection = GridResizeDirection.Rows;
            _readerHost.BorderThickness = new Thickness(0, 1, 0, 0);   // divider on the reader's top
        }

        Layout = layout;
        _configured = true;
    }
}
