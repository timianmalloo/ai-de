using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// Opening a workspace restores its saved layout (US-9), but a saved layout that lost the graph pane
/// restores to a scattered, graph-less workbench. The guard keeps the current graph-bearing layout
/// instead. Pure, so it is verified headlessly.
/// </summary>
public sealed class LayoutRestoreGuardTests
{
    // The degenerate layout observed in the wild: two stacks, no canvas/graph surface at all.
    private static Layout Degenerate()
    {
        var top = new StackNode("s1",
        [
            new Surface("explore", "view", "Explore"),
            new Surface("classdiagram#a", "classdiagram", "Class diagram"),
        ]);
        var bottom = new StackNode("s2",
        [
            new Surface("terminal-1", "terminal", "Terminal"),
            new Surface("classdiagram#b", "classdiagram", "Class diagram"),
        ]);
        var root = new SplitNode("split", Orientation.Vertical, [top, bottom], [0.6, 0.4]);
        return new Layout(root, [], System.Collections.Immutable.ImmutableDictionary<string, StackState>.Empty);
    }

    [Fact]
    public void ShouldKeepPrevious_WhenTheRestoreDropsTheGraphTheCurrentLayoutHas()
    {
        Assert.True(LayoutRestoreGuard.HasCanvas(Layout.Default()));
        Assert.False(LayoutRestoreGuard.HasCanvas(Degenerate()));

        // Default (has graph) -> degenerate saved (no graph): keep the current default.
        Assert.True(LayoutRestoreGuard.ShouldKeepPrevious(Layout.Default(), Degenerate()));
    }

    [Fact]
    public void ShouldKeepPrevious_IsFalse_WhenTheRestoreAlsoHasTheGraph()
    {
        Assert.False(LayoutRestoreGuard.ShouldKeepPrevious(Layout.Default(), Layout.Default()));
    }

    [Fact]
    public void ShouldKeepPrevious_IsFalse_WhenThereWasNoGraphToProtect()
    {
        // Nothing to protect — the previous layout had no graph either, so the restore stands.
        Assert.False(LayoutRestoreGuard.ShouldKeepPrevious(Degenerate(), Degenerate()));
    }
}
