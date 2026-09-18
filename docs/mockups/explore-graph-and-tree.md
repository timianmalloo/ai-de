---
id: mockup-explore-graph-and-tree
title: "Explore: Graph and Tree as peer views — the operator's two clean-machine issues, designed against the fixes already ruled"
type: doc
status: draft
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [ui-design, explore, graph-canvas, solution-tree, provenance, wcag, ruling-140, ruling-141, dc-230]
links:
  - { to: review-ui-explore-graph-and-tree, rel: refines }
  - { to: inv-0014-the-graph-stage-is-capped-and-typescript-provenance-is-a-scope-id, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: mockup-first-use-accounts, rel: relates-to }
review-by: 2026-12-17
summary: >-
  The Explore pane with Graph and Tree as peer tabs over one selection, per Ruling 140's amendment to
  Ruling 94. Renders the fixes already ruled rather than the surface as it ships: a flex-remainder
  stage instead of a fixed height, provenance on every edge in dash and glyph and word, chips reading
  drawn-of-declared, and a bounded-result banner that names nodes rather than edges. Craft gate exit
  0 with one advisory Minor.
---

# Explore: Graph and Tree

The operator, testing a clean-machine build on 2026-09-16:

> *"the right-side-views for the architecture explorer need to be Graph and Tree — where tree is the
> more familiar dev view like in vs code"*
>
> *"graph view should be scaling with the dock on a larger monitor — the graph view should be filling
> the dock"*

`docs/reviews/ui-explore-graph-and-tree.md` is the review that measured the surface. This is the
design that follows from it. **Ruling 140** settled the placement — Architecture's default is now
`Center = [Graph (active), Tree]` with Left retired — and that part is already built
(`ZoneLayout.cs`, with a headless test). **Ruling 140 deferred the Graph↔Tree seam to its own slice**,
and this mockup is that slice's design input.

## It renders the fixes, not the defects

Deliberate, and the reason the mockup is worth having: every finding the review raised that has been
*ruled* is shown here as repaired, so the seam slice is designed against the surface it will actually
meet rather than the one in the screenshots.

| Shown as | Because |
|---|---|
| The stage is `flex: 1 1 auto` with a `min-height`, never a fixed `px` height | INV-0014's P1; DC-230's class. The screenshot's stage was 21.8% of its pane. |
| Every edge carries a **dash pattern, a glyph and a word** | P6, Ruling 141. Colour is the third signal, because the stroke measures about 1.45:1 today. |
| Chips read **drawn of declared** — *"Knowledge 0 of 878"* | The live surface reads *"Knowledge 0"* on a workspace with 878 knowledge nodes, which an operator reads as "there is none". |
| The banner counts **nodes**, and says so | One integer is currently rendered as "nodes" in one place and "edge(s)" in another. 9,780 − 1,500 = 8,280 proves it is the node shortfall. |
| The bounded state offers **Focus a node** first | US-K2's own exit. A cap on a global degree-ranked sample is a smaller hairball, not a bounded neighbourhood. |

## The tree's spanning rule, and what it refuses

**Path containment, and nothing else.** One row is exactly one `(Path, Kind)`. It is the only relation
in this graph that is genuinely a tree — a file sits in exactly one folder — so the tree cannot imply a
relation the graph does not assert. It is also the VS Code idiom the operator named.

The hard rule that follows, and it is a correctness rule rather than a style choice: **a
non-containment edge is never a tree child.** The moment an `INFERRED` `calls` edge becomes a child
with the same expander glyph as a folder, the tree has laundered its provenance. "What does this
touch" is routed to the graph and to the reader's edge list, both of which carry it.

Inheritance was refused as a spanning rule on the operator's own evidence: the screenshot's class
hierarchy reads *"1500 type(s), 0 relationship(s)"* — a forest of 1,500 singletons rendered as a
hierarchy.

## Hard states rendered

Nine, all switchable in the harness and rendered rather than described: no workspace · still
extracting (with a count, not a bare spinner) · bounded result · source not locatable (Ruling 93's
shortfall, verbatim, with the offending value visible) · single node with no neighbours · filtered to
empty, which is **distinct** from an empty workspace · an unindexed folder, which is *Flagged* and not
empty · error · and the ordinary populated case.

## Craft gate

```
ui-craft-gate.py --a11y-obligation --gate docs/mockups/explore-graph-and-tree.html
→ exit 0 · Minor 1 (em-dash-overuse, advisory)
```

**It did not pass on the first run**, and what it caught was a real defect rather than a style
quibble: five `low-contrast` Blockers at `1.0:1`, because `.btn:hover` set a background without a
colour while `.btn.primary` kept `--accent-contrast` — so **the primary button's label went invisible
on hover**. Also one `design-system-color` Major for the edge-stroke stand-in, now drawn from
`--border-strong` rather than an invented hex, and one `cramped-padding` where the canvas sat flush
against its container.

A clean run is a **floor, never a verdict**. It cannot see whether the archetype fits, whether the IA
is right, or whether the copy is true.

## Open, and routed

- **The edge-stroke token is a stand-in.** `--border` measures about 1.45:1 on the sunken ground and
  `DESIGN.md:648` itself records the token as *"never the only signal"* — but in a graph the edge **is**
  the content. `--border-strong` is used here because it is already in the palette; **UX & Accessibility
  holds the WCAG veto and rules the real value.**
- **The keyboard grammar for the graph is proposed, not ratified.** The stage is `role="application"`
  with one tab stop, arrows moving by *edge* rather than by pixel, and a live region announcing the
  focused node's edge counts by provenance. That is the largest accessibility change on the surface.
- **The tab strip is WPF host chrome**, outside the WebView2 rect. It is modelled here so the review
  can see it, but nothing inside the page may overlay it — the airspace boundary is real (UI-T4).
- **Not approved.** This is a design input for the deferred seam slice, not a built surface, and the
  author does not clear the accessibility veto.
