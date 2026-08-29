---
id: kb-graph-experience-sota
title: "Unified Graph Experience — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [graphrag, graph-visualization, 3d, obsidian, force-graph]
links:
  - { to: kb-graph-experience-and-visualization, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Current best practice for GraphRAG retrieval, 2D/3D graph visualization, and composing Obsidian +
  Graphify into a navigable code+knowledge graph experience.
---

# State of the art — unified graph experience & visualization

## GraphRAG and its variants (retrieval over a graph)

- **GraphRAG** (Microsoft, MIT) — build a knowledge graph from a corpus (entity/relationship extraction →
  graph → **community detection** → optional LLM community summaries), then answer with **local search**
  (entity-centric, vector-like precision) or **global search** (dataset-wide themes via community summaries).
  Wins on multi-hop/relational; loses on simple lookups; global context historically **26–85×** vector cost. *(Verified, [GX1][GX2])*
- **LazyGraphRAG** (late 2024) — defer LLM summarisation to *query time*: index with cheap NLP (cost ~0.1% of
  full GraphRAG, ≈ vector RAG), reason at query time — reported **~700× cheaper global queries** at competitive
  quality. Collapses the cost objection. *(Verified, [GX3])*
- **LightRAG** — incremental graph updates (no full rebuild on new docs), dual-layer local/global, tuned for
  small models / commodity hardware. *(Verified, [GX2] guide)*
- **The durable rule:** **hybrid** — vector first for lookups, graph for relational context; return **bounded
  neighbourhoods**, never whole subgraphs. And Microsoft's numbers are **document-corpus** QA — a *code* graph
  must be measured, not assumed (the `code-knowledge-graphs` "measure it yourself" rule). *(Verified/Flagged)*

## Graph visualization (2D and 3D)

- **2D, large graphs:** **Sigma.js** (MIT, WebGL, 100k+ nodes, analytics dashboards) with **Graphology** as the
  data model; **Cytoscape.js** (MIT, mature, algorithm-rich, progressive disclosure). *(Verified, [GX7][GX9])*
- **2D/3D, force-directed:** **react-force-graph / 3d-force-graph** (vasturiano, MIT ecosystem; ThreeJS/WebGL;
  d3-force-3d or ngraph physics; click-to-focus, expand/collapse, HTML-in-node; 2D/VR/AR siblings). **Reagraph**
  (reaviz, Apache-2.0, React-first WebGL 2D/3D, clustering). *(Verified, [GX5][GX6][GX8])*
- **Avoid for licence:** **Cosmograph** (GPU, multi-million nodes) is non-commercial/commercial-licensed. *(Verified, [GX10])*
- **Native WPF:** **GraphX for .NET** (panthernet + westermo fork; native, MVVM, 2D only, dated) — viable but
  outclassed by WebGL for size/3D. *(Verified, [GX11])*
- **Renderer-by-tier is the same decision as diagrams:** WebGL (Sigma/force-graph) for thousands+; SVG/Canvas
  degrades past ~a few thousand elements (`diagram-generation`). *(Verified, cross-ref)*

## Node-based UIs (the introspection interaction)

- **React Flow / xyflow** (MIT, production-tested by Stripe/Typeform) is the leading node-editor; the
  **awesome-node-based-uis** list (same team) is the index. **litegraph.js**, **rete.js**, **Drawflow** are
  framework-agnostic. These provide *node selection → detail → edge-following* — the "step onward" interaction a
  raw force-graph lacks. *(Verified, [GX15][GX16][GX17])*
- **The distinction:** a **force-graph** is a *layout of the whole*; a **node-editor** is a *workbench for a
  neighbourhood*. The node-walk experience needs both — overview (force-graph) and focus (node-editor panel). *(Inferred.)*

## Obsidian + Graphify composition

- **Obsidian** (`obsidian-lens.md`) — the docs frontmatter graph, read via the native graph view + plugins;
  network-science plugins (**InfraNodus**, **New 3D Graph**, **GraphForge**) add betweenness/community/gap
  detection and 3D. Keep it a **reader**; AI features are an **egress decision**. *(Verified, [GX12][GX13][GX14]; pack OB1/OB11)*
- **Graphify** (`code-knowledge-graph.md`) — the on-device **code** graph (Apache-2.0, PyPI `graphifyy`; verify
  the canonical source — `.net` is unaffiliated), with `EXTRACTED/INFERRED/AMBIGUOUS` provenance and a `--join`
  lens to the docs graph. *(Verified, [GX19])*
- **The fusion** — docs=intent, code=reality; the node-walk crosses the `--join` edges. *(Verified, pack GK.)*

## The frontier / what's moving

- **LazyGraphRAG/LightRAG for code** — unproven on code graphs; the open measurement.
- **Agentic graph traversal** — the LLM decides what to retrieve and iterates (GraphRAG DRIFT search, agentic RAG).
- **GitHub Copilot canvases** — interactive, prompt-driven codebase diagrams in the Copilot app; a moving target
  worth tracking as a comparable.
