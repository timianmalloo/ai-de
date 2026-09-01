using AiDe.App.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// Structural guards on the canvas page's chrome. The page's behaviour is exercised end-to-end by
/// <see cref="CanvasFocusIntegrationTests"/> through a real WebView2; these cheap assertions guard
/// the affordances that test does not name, so a refactor cannot silently drop them.
/// </summary>
public sealed class CanvasPageTests
{
    // The Overview affordance: deep drill-downs left the user with only a one-hop "Back", and no
    // single gesture to return to the whole graph. The button posts node.overview; the host reloads
    // the overview (rootId null); it is disabled at the overview (current === null). Fails RED if any
    // of the three halves of that contract is dropped.
    [Fact]
    public void Page_HasAnOverviewAffordance_ThatReturnsToTheWholeGraph()
    {
        Assert.Contains("id=\"home\"", CanvasPage.Html, StringComparison.Ordinal);
        Assert.Contains("node.overview", CanvasPage.Html, StringComparison.Ordinal);
        Assert.Contains("homeButton.disabled = !current;", CanvasPage.Html, StringComparison.Ordinal);
    }

    // Home key drives the same affordance, so the graph stays operable without a pointer.
    [Fact]
    public void Page_BindsTheHomeKey_ToTheOverviewAffordance()
    {
        Assert.Contains("e.key === 'Home'", CanvasPage.Html, StringComparison.Ordinal);
    }

    // The semantic-zoom "Group" affordance: a toggle that requests the grouped overview, group
    // super-nodes that open their members, and Back that returns to the group (not a node describe).
    // Fails RED if any half of the group contract is dropped.
    [Fact]
    public void Page_HasAGroupAffordance_WithGroupOpenDrillDown()
    {
        Assert.Contains("id=\"group\"", CanvasPage.Html, StringComparison.Ordinal);
        Assert.Contains("graph.grouped", CanvasPage.Html, StringComparison.Ordinal);
        Assert.Contains("group.open", CanvasPage.Html, StringComparison.Ordinal);
        Assert.Contains("function openGroup(", CanvasPage.Html, StringComparison.Ordinal);
    }
}
