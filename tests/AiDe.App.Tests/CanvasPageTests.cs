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

    // KNOWLEDGE IS DECIDED BY THE FLAG, NOT BY SPELLING (DC-074).
    //
    // The Knowledge chip read 0 three times, by three mechanisms. The third was this: Core sends an
    // authoritative `isKnowledge` on every node, read from `node_kind` — the one dimension that
    // separates knowledge from source (INV-0004) — and the page ignored it, categorising by a fixed
    // list of spellings that could not match a repository whose knowledge kinds are `investigation`
    // and `glossary`. Those fell through to `code`, against a workspace holding 2,343 knowledge
    // nodes.
    //
    // WHAT THIS CAN AND CANNOT PROVE. The page is a JavaScript string with no engine to run it here,
    // so this asserts the flag is READ and that the whole node is passed to the categoriser — not
    // that the categorisation is right. The behaviour is proven one layer down, where
    // CanvasGraphViewModel is real C#: FieldsSurviveTheClientBoundaryTests fails if the flag stops
    // reaching CanvasNode at all.
    [Fact]
    public void Page_CategorisesKnowledgeByTheFlag_NotBySpelling()
    {
        Assert.Contains("node.isKnowledge === true", CanvasPage.Html, StringComparison.Ordinal);

        // The whole node, not just its kind — the call site is what made the flag unreachable.
        Assert.Contains("categoryOf(n)", CanvasPage.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("categoryOf(n.kind)", CanvasPage.Html, StringComparison.Ordinal);
    }

    // The spelling list survives as a FALLBACK, for a node from a store written before the flag
    // existed, where `isKnowledge` is absent rather than false. Deleting it would strand exactly
    // the users who have not re-indexed — the state this whole defect keeps being found in.
    [Fact]
    public void Page_KeepsTheSpellingList_AsAFallbackBehindTheFlag()
    {
        var html = CanvasPage.Html;

        var flag = html.IndexOf("node.isKnowledge === true", StringComparison.Ordinal);
        var list = html.IndexOf("k === 'knowledge' || k === 'doc'", StringComparison.Ordinal);

        Assert.True(flag >= 0 && list >= 0, "the flag and its fallback must both be present");
        Assert.True(flag < list,
            "the spelling list is consulted before the flag, so a store that HAS the flag is still "
            + "categorised by guesswork — which is the defect, not the fallback");
    }

    // A spec IS knowledge, and the filter bar offers Specs as its own category. Filing specs under
    // Knowledge would empty a category the user can click, which is a different defect from the one
    // being fixed.
    [Fact]
    public void Page_KeepsSpecsAsTheirOwnCategory_AheadOfTheKnowledgeFlag()
    {
        var html = CanvasPage.Html;

        var specs = html.IndexOf("k === 'spec' || k === 'requirement'", StringComparison.Ordinal);
        var flag = html.IndexOf("node.isKnowledge === true", StringComparison.Ordinal);

        Assert.True(specs >= 0 && flag >= 0);
        Assert.True(specs < flag, "the specific docs bucket must win over the general one");
    }
}
