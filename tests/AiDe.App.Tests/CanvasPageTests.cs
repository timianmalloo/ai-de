using System.Text.RegularExpressions;

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

    // PROVENANCE REACHES EVERY EDGE, NOT ONLY THE JOINS (DESIGN.md:311-324, Ruling 141 / P6).
    //
    // The page encoded provenance inside `if (edge.isJoin)` and drew everything else with one line,
    // `stroke '#2A313B'` — no dash, no glyph, no word, no <title>. `CanvasEdge.IsInferred` is defined
    // on EVERY edge and `Status` is already on the wire, so an INFERRED edge was rendered pixel-
    // identical to a VERIFIED one with the fact needed to tell them apart already in the payload.
    // That is GRAPH-PROVENANCE-LAUNDERED, which DESIGN.md names as a hard escalation:
    // "an inferred edge rendered identically to an extracted one".
    //
    // WHAT THIS CAN AND CANNOT PROVE. The page is a JavaScript string with no engine to run it here
    // (the same boundary CanvasPageTests already works under), so this asserts that the encoding is
    // resolved for every edge BEFORE join-ness is consulted, that the three classes each carry a
    // distinct dash and their own word, and that the un-encoded else-branch is gone. The RENDERED
    // proof — that the dash and the title actually reach the SVG — belongs to the P1 desktop slot
    // under Ruling 141(a). This is the cheap, deterministic guard that fails if someone later
    // simplifies the encoding back onto the join branch.
    [Fact]
    public void Page_EncodesProvenanceOnEveryEdge_NotOnlyOnJoins()
    {
        var html = CanvasPage.Html;

        // The defect signature itself: a non-join edge that gets a colour and nothing else.
        Assert.DoesNotContain("line.setAttribute('stroke', '#2A313B');", html, StringComparison.Ordinal);

        // `function (edge)`, not `function (e)` — the page runs an earlier pass over the same array
        // to compute degree, and anchoring on the shorter spelling lands on that one.
        var loop = html.IndexOf("(graph.edges || []).forEach(function (edge) {", StringComparison.Ordinal);
        Assert.True(loop >= 0, "the edge-drawing loop must still be findable");

        var provenance = html.IndexOf("var prov = provenanceOf(edge);", loop, StringComparison.Ordinal);
        var join = html.IndexOf("edge.isJoin", loop, StringComparison.Ordinal);

        Assert.True(provenance >= 0, "every edge must resolve its provenance, join or not");
        Assert.True(provenance < join,
            "provenance must be resolved for the edge BEFORE join-ness is consulted — resolving it "
            + "inside the join branch is the defect this test exists for");

        // The <title> is built from the resolved provenance, so every edge carries its basis in words.
        Assert.Contains(
            "title.textContent = edge.predicate + ' (' + prov.glyph + ' ' + prov.word + ')';",
            html, StringComparison.Ordinal);

        // Three classes, three words, three glyphs, three distinct dash patterns. Colour is the third
        // signal only: the stroke measures ~1.45:1 against the stage, so the dash and the word have to
        // carry the meaning with the hue unread.
        Assert.Contains("word: 'Verified'", html, StringComparison.Ordinal);
        Assert.Contains("word: 'Inferred'", html, StringComparison.Ordinal);
        Assert.Contains("word: 'Flagged'", html, StringComparison.Ordinal);
        Assert.Contains("dash: '5 4'", html, StringComparison.Ordinal);
        Assert.Contains("dash: '2 3'", html, StringComparison.Ordinal);

        // Unverified is the wire's third word (VerificationStatus.Unverified); Flagged is what the
        // design system calls it. An unrecognised status is flagged, never drawn as a fact.
        Assert.Contains("s === 'Verified' || s === 'Inferred' ? s : 'Flagged'", html, StringComparison.Ordinal);
    }

    // The legend describes all three classes over all edges. It used to appear only when the graph
    // held a join, and then counted inferred joins out of joins — so on a graph with no joins the
    // reader was told nothing about provenance at all, and on one with joins the count described a
    // subset of the picture.
    [Fact]
    public void Page_LegendNamesAllThreeProvenanceClasses_NotOnlyJoins()
    {
        var html = CanvasPage.Html;

        Assert.Contains("edges by basis:", html, StringComparison.Ordinal);
        Assert.Contains(" Verified</b> solid (", html, StringComparison.Ordinal);
        Assert.Contains(" Inferred</b> dashed (", html, StringComparison.Ordinal);
        Assert.Contains(" Flagged</b> dotted (", html, StringComparison.Ordinal);

        // The old join-only count, which described a subset as though it were the whole.
        Assert.DoesNotContain("' of ' + joins", html, StringComparison.Ordinal);
    }

    // RULING 141(c), MECHANIZED: no new hex literal enters the page string.
    //
    // CanvasPage.Html is a token-free island (DC-230) — a C# raw-string literal that neither
    // ui-craft-gate.py nor design-lint.py can see into, so every colour in it is unlinted. That makes
    // it the one place in the product where a new raw colour costs nothing to add and is invisible
    // afterwards. This pins the palette at the sixteen colours already there.
    //
    // TO CHANGE IT: the right fix is DESIGN.md:546 TC5 — the hosted surface RECEIVES the tokens
    // rather than restating them. Adding a value to this list instead is a deliberate deepening of
    // DC-230 and needs a ruling, not an edit.
    [Fact]
    public void Page_AddsNoNewHexLiteral_ToATokenFreeIsland()
    {
        string[] palette =
        [
            "#0D1014", "#12151A", "#1A1F26", "#21303F", "#2A313B", "#47566B", "#5B9DD9", "#5FB98F",
            "#63748C", "#98A3B2", "#B08AD0", "#B99A5E", "#CDE3FF", "#D8A650", "#E0955F", "#E4E9EF",
        ];

        var found = Regex.Matches(CanvasPage.Html, "(?<!&)#[0-9A-Fa-f]{6}")
            .Select(m => m.Value.ToUpperInvariant())
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(palette, found);
    }
}
