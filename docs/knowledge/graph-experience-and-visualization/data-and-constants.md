---
id: kb-graph-experience-data
title: "Unified Graph Experience — data, constants & thresholds"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [graphrag, constants, thresholds, licences, graph-visualization]
links:
  - { to: kb-graph-experience-and-visualization, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The cost figures, scale thresholds, licences and provenance mappings to quote for the graph
  experience — GraphRAG costs, renderer scale limits, and the code↔knowledge edge model.
---

# Domain data, constants & thresholds

## GraphRAG cost & variant facts (quote these)

| Fact | Value | Source |
|---|---|---|
| GraphRAG global context vs local vector | **26–85×** token cost | Microsoft eval (code-knowledge-graphs #8) |
| LazyGraphRAG indexing cost | **~0.1%** of full GraphRAG (≈ vector RAG) | [GX3] |
| LazyGraphRAG global query cost | **~700× cheaper** than full GraphRAG global | [GX3] |
| LightRAG | incremental updates (no full rebuild), dual-layer | [GX2] |

**Caveat (Flagged):** all figures are **document-corpus QA**, not code graphs. Measure on a real C# graph before
depending on them (the `code-knowledge-graphs` rule).

## Renderer scale thresholds (pick renderer by scale)

| Renderer | Tech | Practical scale |
|---|---|---|
| SVG/Canvas (Mermaid, d3 basic) | DOM | degrades past **~a few thousand** elements |
| Cytoscape.js | Canvas/WebGL | ~100k elements |
| Sigma.js | WebGL | **100k+** nodes (2D) |
| 3d-force-graph | ThreeJS/WebGL | ~4k "large" example; more with degradation |
| Cosmograph | GPU | multi-million (but licence excludes) |
| GraphX .NET | WPF native | large-ish, 2D only |

*(Verified [GX5][GX7][GX9][GX10][GX11]; the SVG figure cross-ref `diagram-generation`.)*

## Library licences (verify versions before pinning)

| Library | Licence | Note |
|---|---|---|
| microsoft/graphrag | **MIT** | pipeline |
| Sigma.js, Cytoscape.js | **MIT** | 2D WebGL/Canvas |
| react-force-graph, 3d-force-graph, force-graph | **MIT** (vasturiano) | 2D/3D/VR/AR |
| Reagraph | **Apache-2.0** | React WebGL |
| React Flow / xyflow, litegraph.js, rete.js, Drawflow | **MIT** | node editors |
| GraphX for .NET | **MIT-family** | native WPF *(verify exact SPDX)* |
| **Cosmograph** | **Non-commercial/commercial** | **excluded by licence** |
| Graphify | **Apache-2.0** | PyPI `graphifyy`; canonical graphify.com |
| WebView2 | **Proprietary, free** | host only, not vendorable |

## The node & edge model (what the introspection walks)

- **Node types** (union graph): `code` (C# file/symbol — Graphify), `knowledge` (spec/ADR/design/decision-note —
  docs graph), `diagram`, `test`, `infra`, `runtime-trace`. Each routes to a renderer (Base B).
- **Edge provenance** — Graphify `EXTRACTED` (AST, → **Verified**) / `INFERRED` (→ **Inferred**) / `AMBIGUOUS`
  (→ **Flagged**); docs `rel` registry `implements/refines/depends-on/supersedes/tested-by/documents/uses-term`.
  **A citation is not a promotion** — carry the provenance on the wire and in the UI (pack GK6–GK7). *(Verified, [GX19])*
- **The join edge** — Graphify `--join` produces *documentation-without-implementation* and *risk-without-
  governance*; these are the code↔knowledge bridges the node-walk crosses. *(Verified, [GX19])*

## Network-science metrics (the insight layer)

- **Betweenness centrality** — the bridge nodes (error there propagates furthest; `obsidian-lens.md` OB14).
- **Community detection / modularity** — the clusters (GraphRAG's affordability mechanism).
- **Gap / structural-hole detection** — the missing links (InfraNodus's insight; `--analyze`).
- **Degree / god-nodes** — the hubs (Graphify `god-nodes`, the riskiest change surface, GK10).
