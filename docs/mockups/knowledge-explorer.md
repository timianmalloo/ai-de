---
id: mockup-knowledge-explorer
title: "Knowledge Explorer — graph + node introspection (mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [mockup, knowledge-graph, 2d-3d, node-introspection, provenance]
links:
  - { to: spec-knowledge-exploration, rel: documents }
review-by: 2027-02-27
review-suggested: []
summary: >-
  Self-contained mockup of the knowledge exploration surface — a bounded 2D neighbourhood graph
  with a 2D/3D toggle, a node-introspection panel that routes each node to its natural renderer
  (code editor, rendered markdown, rendered html, proof), a provenance legend, and empty /
  loading / too-large states. The .html is data; this .md is its graph node.
---

# Knowledge Explorer — mockup

`knowledge-explorer.html` — the node-walk surface (spec-knowledge-exploration). Renders a bounded
neighbourhood of `JournalEntry.cs` across artifact types (code / knowledge / proof / diagram), edges styled
by **provenance** (solid=extracted, dashed=inferred, dotted=flagged) with a legend, a **2D/3D** toggle
(3D = pseudo-isometric), a metric-overlay selector, and a right **introspection panel** routing by node type:
code → syntax-highlighted read-only editor, knowledge → **rendered markdown** (not raw), html → **rendered
html**, proof → verified summary. Typed-edge list is the node-walk. Harness switches theme, motion, graph
state (neighbourhood / loading / **empty** / **too-large**), and the selected node type.

**Key correctness demonstrations:** never the whole graph (bounded N≤2 with expand); provenance never
laundered (inferred visually distinct); 2D is default, 3D a mode; empty neighbourhood is an explicit state,
not a blank success; a keyboard node path exists via the edge list + focusable nodes.
