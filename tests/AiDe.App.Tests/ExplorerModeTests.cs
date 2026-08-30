using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Presentation;

namespace AiDe.App.Tests;

/// <summary>
/// Phase-1 controls for the Explorer mode (ADR-0017; design D1-D5). The mode swap and the reader are
/// host-side (no WebView2), so they run on a plain STA thread with stub content; the real
/// CanvasSurface graph and the "a live terminal survives" integration form of T1 are a launch smoke
/// test, not a headless one.
/// </summary>
public sealed class ExplorerModeTests
{
    private static void OnSta(Action work)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { work(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA thread did not finish");
        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
    }

    // T1 — the retain-not-rebuild control (ADR-0017's load-bearing invariant, reference-level). The
    // workbench object is the SAME instance across an Explorer round-trip: the swap only unparented
    // it, it was never rebuilt. RED if the swap recreated or disposed the workbench.
    [Fact]
    public void ExplorerRoundTrip_KeepsTheSameWorkbenchInstance()
    {
        OnSta(() =>
        {
            var host = new ContentControl();
            var workbench = new Border();   // stands in for Shell.Manager
            var built = 0;
            var mode = new ShellModeController(host, workbench, () => { built++; return new Grid(); });

            Assert.Same(workbench, host.Content);     // starts on the workbench
            mode.Set(ShellViewMode.Explorer);
            Assert.NotSame(workbench, host.Content);  // now the Explorer surface
            mode.Set(ShellViewMode.Workbench);
            Assert.Same(workbench, host.Content);     // the SAME workbench instance returns
            Assert.Equal(1, built);                   // Explorer built once, not per entry
        });
    }

    // The Explorer surface is created once and retained across a round-trip (US-E6): re-entering does
    // not rebuild it, so its graph/reader state survives.
    [Fact]
    public void ReenteringExplorer_DoesNotRebuildTheExplorerSurface()
    {
        OnSta(() =>
        {
            var host = new ContentControl();
            var built = 0;
            var mode = new ShellModeController(host, new Border(), () => { built++; return new Grid(); });

            mode.Set(ShellViewMode.Explorer);
            var first = host.Content;
            mode.Set(ShellViewMode.Workbench);
            mode.Set(ShellViewMode.Explorer);

            Assert.Same(first, host.Content);         // the same Explorer instance
            Assert.Equal(1, built);
        });
    }

    // T5 — Toggle flips the mode and raises ModeChanged with the new mode.
    [Fact]
    public void Toggle_FlipsModeAndRaisesModeChanged()
    {
        OnSta(() =>
        {
            var host = new ContentControl();
            var mode = new ShellModeController(host, new Border(), () => new Grid());
            var seen = new List<ShellViewMode>();
            mode.ModeChanged += (_, m) => seen.Add(m);

            Assert.Equal(ShellViewMode.Workbench, mode.Mode);
            mode.Toggle();
            Assert.Equal(ShellViewMode.Explorer, mode.Mode);
            mode.Toggle();
            Assert.Equal(ShellViewMode.Workbench, mode.Mode);
            Assert.Equal(new[] { ShellViewMode.Explorer, ShellViewMode.Workbench }, seen);
        });
    }

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
}
