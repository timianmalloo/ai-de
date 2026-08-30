---
id: mockup-graph-canvas
title: "Graph canvas — large-graph UX (mockup)"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [mockup, ui-design, graph, canvas, force-layout, lod, semantic-zoom]
links:
  - { to: review-ui-graph-canvas, rel: documents }
  - { to: spec-knowledge-exploration, rel: relates-to }
review-by: 2027-02-27
summary: >-
  Self-contained, dependency-free mockup of the fixed graph-canvas UX: a force-directed node-link
  layout (degree-sized dots coloured by kind, thin edges behind), labels-on-demand, zoom/pan/fit,
  search-first focus+context, semantic-zoom clustering (LOD), an honest "showing N of M" caption, and
  disclosures as a chip. Replaces the current single-ring pile of opaque cards.
---

# Graph canvas — large-graph UX

The [self-contained mockup](graph-canvas.html) demonstrates the target UX with the review harness
(state · density · theme · reduced-motion). The full measurements, rubric critique and ranked plan
are in [the review](../reviews/ui-graph-canvas.md).

The current implementation is `CanvasPage.cs` (a WebView2 HTML page with a single-ring 2D layout and
opaque box nodes). The highest-leverage fix is to replace the ring with a force-directed layout and
render nodes as degree-sized dots with edges behind them — the rest (zoom/pan, LOD, states,
disclosures-as-chip) builds on that. Design-owned; LOD clustering additionally needs a Core
community/aggregation query (session-contracts §4c).
