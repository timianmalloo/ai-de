using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// Phase-1 controls for the Explorer mode's reader (ADR-0017 primary-view-mode; design D1-D5). The
/// reader is host-side (no WebView2), so it runs on a plain STA thread with stub content; the real
/// CanvasSurface graph and the "a live terminal survives" integration form of T1 are a launch smoke
/// test, not a headless one. The presenter's controls are <c>PerspectiveShellTests</c>.
/// </summary>
public sealed class ExplorerModeTests
{
    private static void OnSta(Action work) =>
        Sta.Run(work, 30);

    // T1 (the retain-not-rebuild control), US-E6's retained Explore surface and T5's activation
    // rule live in PerspectiveShellTests now: the presenter holds three bodies, two of them docking
    // hosts (ADR-0031), and its controls are written against the hosts the shell composes rather
    // than a Border stand-in (DC-135). This file keeps the reader's own controls.

    // T4 — the reader shows an explicit empty state (not a blank) with no selection, and Clear returns
    // to it after a selection.
    [Fact]
    public void Reader_StartsEmpty_AndClearReturnsToEmpty()
    {
        OnSta(() =>
        {
            var reader = new NodeReaderView();
            Assert.True(reader.IsEmpty);
            Assert.Null(reader.SelectedNodeId);

            reader.Show(new CanvasNode("A.B", "B", "code", false, "Ctx"), new List<CanvasEdge>());
            Assert.False(reader.IsEmpty);
            Assert.Equal("A.B", reader.SelectedNodeId);

            reader.Clear();
            Assert.True(reader.IsEmpty);
            Assert.Null(reader.SelectedNodeId);
        });
    }

    // T3 (host-side) — the reader follows a node selection: Show records the selected node so the
    // graph and the reader hold ONE definition of what is selected (design D3).
    [Fact]
    public void Reader_Show_RecordsTheSelectedNode()
    {
        OnSta(() =>
        {
            var reader = new NodeReaderView();
            var node = new CanvasNode("Shop.Order", "Order", "code", true, "Orders");
            var edges = new List<CanvasEdge> { new("Shop.Order", "Shop.Customer", "depends_on", "Verified") };

            reader.Show(node, edges);

            Assert.Equal("Shop.Order", reader.SelectedNodeId);
            Assert.False(reader.IsEmpty);
        });
    }

    // The reader exposes only the edges that TOUCH the node as walk targets — the focus the DC-039
    // bridge lands on when a Tab leaves the graph canvas.
    [Fact]
    public void Reader_Show_CountsOnlyEdgesTouchingTheNode()
    {
        OnSta(() =>
        {
            var reader = new NodeReaderView();
            var edges = new List<CanvasEdge>
            {
                new("A", "B", "calls", "Verified"),        // touches A
                new("C", "A", "documents", "Inferred"),    // touches A
                new("D", "E", "unrelated", "Verified"),    // does not
            };

            reader.Show(new CanvasNode("A", "A", "code", true, "X"), edges);
            Assert.Equal(2, reader.WalkableEdgeCount);

            reader.Clear();
            Assert.Equal(0, reader.WalkableEdgeCount);
        });
    }

    // DC-039 — a Tab off the graph canvas must escape the keyboard trap. The reader can receive focus,
    // so the Explorer's focus-leave bridge has somewhere to land.
    [Fact]
    public void Reader_FocusReader_LandsFocusInTheReader()
    {
        OnSta(() =>
        {
            var reader = new NodeReaderView();
            var window = new System.Windows.Window
            {
                Content = reader,
                Width = 400,
                Height = 300,
                WindowStartupLocation = System.Windows.WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };
            window.Show();
            reader.Show(new CanvasNode("A", "A", "code", true, "X"),
                new List<CanvasEdge> { new("A", "B", "calls", "Verified") });
            window.UpdateLayout();

            try
            {
                Assert.True(reader.FocusReader(), "focus did not land in the reader");
            }
            finally
            {
                window.Close();
            }
        });
    }

    // Phase 3 — the reader's edge case of the graph↔reader cycle. Shift+Tab off the FIRST stop (the
    // region itself) leaves Backward, so focus returns to the graph rather than being trapped. RED if
    // the boundary is not detected.
    [Fact]
    public void Reader_ShiftTabAtFirstStop_LeavesBackward()
    {
        OnSta(() =>
        {
            var reader = new NodeReaderView();
            reader.Show(new CanvasNode("A", "A", "code", true, "X"),
                new List<CanvasEdge> { new("A", "B", "calls", "Verified") });

            AiDe.Core.Workbench.CanvasFocusDirection? seen = null;
            reader.FocusLeaveRequested += (_, d) => seen = d;

            var handled = reader.HandleTabKey(reader.FocusStops[0], shift: true);

            Assert.True(handled);
            Assert.Equal(AiDe.Core.Workbench.CanvasFocusDirection.Backward, seen);
        });
    }

    // Phase 3 — Tab off the LAST stop (the last edge button) leaves Forward, completing the loop back
    // to the graph. The reader is never a dead end.
    [Fact]
    public void Reader_TabAtLastStop_LeavesForward()
    {
        OnSta(() =>
        {
            var reader = new NodeReaderView();
            reader.Show(new CanvasNode("A", "A", "code", true, "X"),
                new List<CanvasEdge>
                {
                    new("A", "B", "calls", "Verified"),
                    new("A", "C", "documents", "Inferred"),
                });

            AiDe.Core.Workbench.CanvasFocusDirection? seen = null;
            reader.FocusLeaveRequested += (_, d) => seen = d;

            var handled = reader.HandleTabKey(reader.FocusStops[^1], shift: false);

            Assert.True(handled);
            Assert.Equal(AiDe.Core.Workbench.CanvasFocusDirection.Forward, seen);
        });
    }

    // Phase 3 — a Tab that stays INSIDE the reader (forward off the first stop, back off the last)
    // does not leave: the cycle only crosses at the true boundaries, so internal traversal is WPF's.
    [Fact]
    public void Reader_TabInsideReader_DoesNotLeave()
    {
        OnSta(() =>
        {
            var reader = new NodeReaderView();
            reader.Show(new CanvasNode("A", "A", "code", true, "X"),
                new List<CanvasEdge>
                {
                    new("A", "B", "calls", "Verified"),
                    new("A", "C", "documents", "Inferred"),
                });

            var fired = 0;
            reader.FocusLeaveRequested += (_, _) => fired++;

            Assert.False(reader.HandleTabKey(reader.FocusStops[0], shift: false)); // forward off first → inward
            Assert.False(reader.HandleTabKey(reader.FocusStops[^1], shift: true)); // back off last → inward
            Assert.Equal(0, fired);
        });
    }

    // Phase 3 — an EMPTY reader (one stop: the region) still participates in the cycle: a Tab either
    // way returns to the graph, since there is nowhere else inside it to go. No trap in the empty
    // state (US-E7).
    [Fact]
    public void Reader_EmptyState_LeavesEitherWay()
    {
        OnSta(() =>
        {
            var reader = new NodeReaderView();  // empty: FocusStops == [reader]
            Assert.Single(reader.FocusStops);

            Assert.Equal(
                AiDe.Core.Workbench.CanvasFocusDirection.Backward,
                reader.BoundaryLeave(reader.FocusStops[0], shift: true));
            Assert.Equal(
                AiDe.Core.Workbench.CanvasFocusDirection.Forward,
                reader.BoundaryLeave(reader.FocusStops[0], shift: false));
        });
    }

    // US-E8 — responsive layout: wide is side-by-side (columns), narrow stacks (rows). Pure function
    // of width, so the rule is asserted without rendering: a narrow single-monitor window keeps both
    // panes usable instead of squeezing the reader to its minimum.
    [Fact]
    public void Explorer_NarrowWidthStacks_WideWidthIsSideBySide()
    {
        OnSta(() =>
        {
            var graph = new CanvasSurface("explorer", "Explorer");
            var reader = new NodeReaderView();
            var surface = new ExplorerSurface(graph, reader) { StackBelowWidth = 760 };

            try
            {
                Assert.Equal(ExplorerLayout.SideBySide, surface.Layout);   // wide default
                Assert.Equal(3, surface.ColumnDefinitions.Count);
                Assert.Empty(surface.RowDefinitions);

                surface.ApplyLayoutForWidth(600);                          // narrow → stacked
                Assert.Equal(ExplorerLayout.Stacked, surface.Layout);
                Assert.Equal(3, surface.RowDefinitions.Count);
                Assert.Empty(surface.ColumnDefinitions);

                surface.ApplyLayoutForWidth(1000);                         // wide → side-by-side
                Assert.Equal(ExplorerLayout.SideBySide, surface.Layout);
                Assert.Equal(3, surface.ColumnDefinitions.Count);

                surface.ApplyLayoutForWidth(0);                            // pre-measure → stays wide
                Assert.Equal(ExplorerLayout.SideBySide, surface.Layout);
            }
            finally
            {
                graph.Dispose();
            }
        });
    }
}
