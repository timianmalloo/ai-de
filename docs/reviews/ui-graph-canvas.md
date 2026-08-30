---
id: review-ui-graph-canvas
title: "UI review — graph canvas (large-graph UX)"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [ui-review, ui-design, graph, canvas, force-layout, lod, wcag, elevate]
links:
  - { to: spec-knowledge-exploration, rel: relates-to }
  - { to: mockup-graph-canvas, rel: relates-to }
  - { to: inv-0003-graph-exceeds-ipc-frame-cap, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2027-02-27
summary: >-
  Review of the graph canvas on TheTerrace after the scaling fix let it load. The graph renders as an
  unreadable pile of overlapping opaque cards: the 2D layout is a single ring (fine for ~15 neighbours,
  catastrophic for 50), nodes are heavy boxes that occlude each other and the edges, there is no
  force-spread, no zoom/pan, no level-of-detail, a fixed 440px stage, and a disclosure wall on top.
  Target UX (mockup): a force-directed node-link layout with dots-not-cards, labels-on-demand, zoom/pan,
  semantic-zoom clustering, search-first focus+context, and disclosures as a chip.
review-suggested:
  - { by: mockup-graph-canvas, on: 2026-08-30, reason: "Graph canvas implemented to the target: 2D force layout + degree-sized dots + pan/zoom/fit landed (DC-036); realizes part of US-K11 — re-check spec/implementation alignment and the still-open semantic-zoom LOD item." }
---

# UI review — graph canvas (review / elevate mode)

## Direction brief

- **Who / JTBD:** a developer exploring a large **code + knowledge** graph to understand structure and
  navigate to artifacts — arriving wanting *insight at a glance*, not a wall of boxes.
- **Archetype:** **Spatial node-link canvas** (catalog C1 · SpatialCanvas), specialised to a
  **force-directed graph with focus+context + semantic zoom**. This is the established idiom for the
  job (Jakob's Law): Obsidian graph, Neo4j Bloom, Gephi/Graphia, `3d-force-graph` all use it.
- **Adjectives (and opposites):** *legible* (not a pile) · *navigable* (not a static dump) · *calm*
  (not a neon hairball).
- **References (what's taken):** **Obsidian graph** (dots sized by links, labels appear on zoom,
  force spread) · **Neo4j Bloom / Gephi** (colour by category/community, degree-sized nodes) ·
  **`3d-force-graph`** (the 2D/3D pair we already have). **Not cloned** — keeps the app's tokens and
  the existing 2D/3D toggle and keyboard-trap contract (ADR-0015).
- **Anti-goals:** the current pile of opaque cards; a neon hairball with every label drawn; a fixed
  small stage in a large pane.

## Measured (before) — from the screenshot + the code

| Metric | Value | Source |
|---|---|---|
| Nodes rendered | **~50** on a single ring | status bar "50 item(s)"; `CanvasPage.cs` line ~248 |
| 2D layout | **single ring** — root centred, all neighbours on one ring, *"Deliberately NOT a [force sim]"* | `CanvasPage.cs:248-251` |
| Node shape | **opaque padded box** (`padding:6px 10px; border; background`) | `CanvasPage.cs` `.node` CSS |
| Nodes clearly separated | **~0** — the whole graph is one overlapping blob | screenshot |
| Defended focal points | **0** — uniform pile, no hierarchy | screenshot |
| Zoom / pan | **none** | `CanvasPage.cs` (no transform on `#stage`) |
| Stage height | **fixed 440px** in a pane ~2–3× taller | `CanvasPage.cs` `#stage{height:440px}` |
| Disclosure text | **~5 lines** of yellow warning occupying the top ~15% of the pane | screenshot |
| Edges legible as connections | **no** — hidden behind opaque boxes | screenshot |
| Colour meaning | everything reads **blue/selected** — colour encodes nothing | screenshot |

## Rubric critique (structure → surface)

| # | Dimension | Finding | Sev | Fix |
|---|---|---|---|---|
| 1 | **Archetype fit / layout** | A single ring is the wrong layout for >~15 nodes; 50 on one ring is an unreadable pile. The spec's own **US-K11** calls for force/semantic-zoom. | **4 Blocker** | Force-directed layout that spreads nodes (mockup) |
| 2 | **State completeness** | The "too large / narrow focus" state (US-K12), a real empty state, and a loading skeleton are not designed on the canvas | 3 Major | Empty / loading / error(too-large) states (mockup) |
| 3 | **Occlusion / legibility** | Opaque box nodes occlude each other **and** the edges; nothing is readable | **4 Blocker** | Dots sized by degree; thin edges *behind* nodes; labels-on-demand |
| 4 | **Navigation** | No zoom, no pan, no fit — a large graph is unnavigable | 3 Major | Wheel-zoom, drag-pan, Fit, search-to-focus |
| 5 | **LOD / scale** | No aggregation; even the bounded 50 is a pile, and 1,500 would be hopeless | 3 Major | Semantic-zoom clustering (community/package super-nodes) |
| 6 | **IA / disclosures** | Disclosures render as a 5-line wall above the graph — noise that reads as an error | 3 Major | A collapsible "⚠ N disclosures" chip |
| 7 | **Space** | A 440px stage wastes a tall pane and crams the graph | 2 Minor | Stage fills the pane |
| 8 | **Colour semantics** | Uniform blue reads as "all selected"; colour should encode kind/provenance | 2 Minor | Colour by node kind + a legend; provenance by edge style (US-K5) |
| 9 | **Focus+context** | 50 nodes are dumped with no entry; the spec's search-first/neighbourhood model isn't in the UX | 3 Major | Search-first; hover dims non-neighbours; click to re-root (node-walk US-K4) |
| 10 | **Accessibility** | The keyboard-trap + node-list contract (ADR-0015) is intact and must be **preserved** through the rebuild | — (floor) | Keep the `.node` focus order, Esc-to-leave, and the 2D default |

**Detector note (CD13–CD14):** the canvas is a WebView2 client-rendered surface, so the static
detector sees a shell — it cannot judge the force layout, occlusion, or LOD. This review is the
human/measure layer the detector cannot replace (defect class E2E-H).

## Target UX (the mockup)

The [self-contained mockup](graph-canvas.html) (dependency-free vanilla-JS force sim, opens over
`file://`) demonstrates the fix, with the review harness (state · density · theme · reduced-motion):

- **Force-directed spread** — nodes settle apart; no overlap. Runs a bounded simulation then rests.
- **Dots, not cards** — circles sized by **degree**, coloured by **kind** (code / knowledge / spec /
  architecture / external), with a legend. Edges are thin lines *behind* the nodes.
- **Labels on demand** — only hubs are labelled at rest; **all** labels appear on zoom-in or hover;
  the hovered node highlights its neighbours and dims the rest (focus+context).
- **Zoom / pan / fit** — wheel to zoom, drag to pan, **Fit**, and **search-to-focus** (US-K4/K10).
- **Semantic zoom / LOD** — a **Clusters** toggle collapses the graph to community/package
  super-nodes (the `1,500 → 12 clusters` density option), expandable on click (US-K11).
- **Honest caption** — "showing 50 of 1,500 most-connected" (US-K10), never a silent slice.
- **Disclosures as a chip** — "⚠ 4 disclosures" opens a popover, instead of a wall.
- **Real states** — loading skeleton, empty ("nothing indexed — Ctrl+K,I"), and the **too-large**
  error ("narrow your focus — search a node, or zoom in to a cluster", US-K12).

## Ranked plan

- **Must fix (the single highest-leverage change):** **replace the single-ring 2D layout with a
  force-directed layout and render nodes as degree-sized dots with edges behind them.** This one change
  turns the pile into a readable graph; everything else builds on it. *(Blocker #1 + #3.)*
- **Should fix next:** zoom/pan/fit + search-to-focus (#4, #9); the too-large / empty / loading states
  on the canvas (#2); disclosures → chip (#6); colour-by-kind + legend (#8).
- **Worth doing:** semantic-zoom clustering / LOD for the 1,500-node overview (#5) — the scalable
  end-state (US-K11), and the natural next `/design` once the query exposes communities; stage fills
  the pane (#7).
- **Preserve (do not regress):** the ADR-0015 keyboard trap, node focus order, Esc-to-leave, 2D
  default, and the 2D/3D toggle (#10).

## Ownership

The canvas HTML/JS (`CanvasPage.cs`, `CanvasSurface.cs`) is **Design-owned** (`AiDe.App`), so the
layout/rendering rebuild is mine to `/implement`. **LOD clustering (#5)** needs a **Core** aggregated/
community query (`GraphProjection`; session-contracts §4c) before it can render real super-nodes;
until then the mockup's clustering is illustrative.

## Gate record

- **GATE ui-design · review · UX & Accessibility + UX Researcher/IA** — VERDICT **BLOCK (as-is)** →
  the current canvas fails archetype-fit (#1) and legibility (#3) at Blocker severity. The **target
  mockup PASSES** the same rubric (force spread, dots, labels-on-demand, zoom/pan, LOD, complete
  states, colour semantics, honest caption) and preserves the ADR-0015 a11y contract. Author did not
  self-clear the accessibility floor — the keyboard-trap/node-list contract is carried forward
  unchanged. Simplifier: the rebuild *removes* the opaque-card treatment and the disclosure wall
  (`net: less chrome, more graph`).
