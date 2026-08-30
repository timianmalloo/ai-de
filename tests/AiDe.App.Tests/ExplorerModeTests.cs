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
}
