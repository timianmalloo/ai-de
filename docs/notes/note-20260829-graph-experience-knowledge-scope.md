---
id: "note-20260829-graph-experience-knowledge-scope"
title: "Graph-experience request split into two new bases; GraphRAG cost finding flagged for update"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: ""
tags: [decision-note, collectknowledge, scope, graph, graphrag, rendering]
links:
  - { to: kb-graph-experience-and-visualization, rel: relates-to }
  - { to: kb-editor-and-content-rendering-surfaces, rel: relates-to }
  - { to: kb-code-knowledge-graphs, rel: relates-to }
  - { to: knowledge-hub, rel: relates-to }
review-by: 2027-02-27
review-suggested: []
summary: >-
  The /collectknowledge run for the unified code+knowledge graph experience produced two new bases
  (graph-experience-and-visualization, editor-and-content-rendering-surfaces); GraphRAG/Obsidian/
  Graphify overlap with existing code-knowledge-graphs and pack standards was reconciled, and
  finding #8 (GraphRAG 26-85x cost) was flagged for update by LazyGraphRAG.
---

# Graph-experience request split into two new bases; GraphRAG cost finding flagged for update

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback weight.*

- **Kind:** decision
- **Confidence:** Verified *(existing bases + pack standards read at grounding; the overlap and the update are observed)*
- **Made during:** `/collectknowledge` run, 2026-08-29 (prompt: rich end-to-end code+knowledge graph experience
  with node navigation/introspection, composing Obsidian + Graphify; GraphRAG; 2D/3D graph visualization; WPF
  KG explorers; code-editor viewing surfaces; markdown/HTML rendering — all permissive-license)

## The call
The request spanned five buckets over two distinct concerns: **the graph experience** (KG/GraphRAG/Obsidian/
Graphify, 2D/3D visualization, WPF explorers) and **content rendering surfaces** (code editor viewing, markdown/
HTML rendering). Per grounding (extend, don't duplicate) and the Simplifier gate, the run produced **two new
bases** — `graph-experience-and-visualization` (the fusion + visualization + hosting) and
`editor-and-content-rendering-surfaces` (the per-node renderers) — and **reconciled** heavy overlap with the
existing `code-knowledge-graphs` base and the pack's own `code-knowledge-graph.md` (Graphify) and
`obsidian-lens.md` standards by cross-reference rather than restatement. The **load-bearing new insight** is
that the genuinely-new piece is a **node-introspection router** that fuses the code graph (Graphify) and
knowledge graph (docs) and routes each node to the right renderer; everything under it (force-graph, metrics,
editors) is borrowed. A **material update** was surfaced and flagged: LazyGraphRAG (~700× cheaper global
queries) revises `code-knowledge-graphs` finding #8 (GraphRAG = 26–85× cost) — that base carries a
`review-suggested` flag pointing here.

## Alternatives dismissed
- **One combined "graph + rendering" base** — rejected; graph visualization and content rendering are distinct
  concerns with distinct libraries and trade-offs. Atomic bases (V1).
- **Re-covering GraphRAG/store material in a new base** — rejected; `code-knowledge-graphs` owns the store +
  cost baseline; the new base *updates* its finding via a flag rather than duplicating it.
- **Recommending native GraphX for the explorer** — dismissed as the default (kept as fallback); the shell
  already hosts WebView2, so a JS force-graph reuses infrastructure and gets 3D + scale.

## Promotion rule
If the graph-experience direction becomes an architectural commitment (e.g. "the explorer is a WebView2
force-graph with a native introspection router", or "adopt LazyGraphRAG for the code graph"), promote that
decision to an **ADR** and link it `supersedes` the relevant part of the base. If the LazyGraphRAG-for-code
measurement is run, fold the result back into `code-knowledge-graphs` finding #8 and clear its flag.
