---
id: kb-graph-experience-references
title: "Unified Graph Experience — references"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [graphrag, references, graph-visualization, standards]
links:
  - { to: kb-graph-experience-and-visualization, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The authoritative sources behind GraphRAG, the graph-viz libraries, and the Obsidian/Graphify
  composition — the ones to quote rather than recall.
---

# Reference information

## GraphRAG & retrieval

- **Project GraphRAG** (Microsoft Research) — the local/global search + community-detection model. *(Verified, [GX1])*
- **microsoft/graphrag** (MIT) — the open-source pipeline: chunk → extract entities/relationships → graph →
  community detection → summaries → local/global query. *(Verified, [GX2])*
- **LazyGraphRAG** (Microsoft Research blog) — defer summarisation to query time; ~0.1% index cost, ~700×
  cheaper global queries. *(Verified, [GX3])*
- **Prior baseline** — GraphRAG global context at **26–85×** vector cost, from `code-knowledge-graphs`
  finding #8 (Microsoft's own evaluation). This base flags it for review given LazyGraphRAG. *(Verified, cross-ref)*

## Graph-visualization libraries (primary)

- **Sigma.js** — https://www.sigmajs.org/ + https://github.com/jacomyal/sigma.js — MIT, WebGL. *(Verified, [GX7])*
- **Cytoscape.js** — https://js.cytoscape.org/ — MIT. *(Verified, [GX9])*
- **3d-force-graph** — https://github.com/vasturiano/3d-force-graph — ThreeJS/WebGL, d3-force-3d/ngraph physics. *(Verified, [GX6])*
- **react-force-graph** — https://github.com/vasturiano/react-force-graph — MIT, 2D/3D/VR/AR. *(Verified, [GX5])*
- **Reagraph** — https://github.com/reaviz/reagraph — Apache-2.0, React WebGL. *(Verified, [GX8])*
- **GraphX for .NET** — https://github.com/panthernet/GraphX + https://github.com/westermo/GraphX — native WPF. *(Verified, [GX11])*

## Node-based UI

- **React Flow / xyflow** — https://github.com/xyflow/xyflow — MIT. *(Verified, [GX15])*
- **awesome-node-based-uis** — https://github.com/xyflow/awesome-node-based-uis — curated index. *(Verified, [GX15])*
- **litegraph.js** — https://github.com/jagenjo/litegraph.js ; **rete.js** — https://github.com/retejs/rete. *(Verified, [GX16][GX17])*

## Obsidian & Graphify (pack standards — the composition authority)

- **`code-knowledge-graph.md`** (GK1–GK16) — Graphify: canonical source **graphify.com /
  github.com/Graphify-Labs/graphify** (Apache-2.0, PyPI **`graphifyy`**); **`.net` is unaffiliated** (GK15);
  provenance `EXTRACTED/INFERRED/AMBIGUOUS`; the `--join` code↔docs lens. *(Verified, [GX19])*
- **`obsidian-lens.md`** (OB1–OB14) — Obsidian as reader; commit config not state; `--analyze` betweenness/
  community/gap; OB11 AI egress rule; OB14 hub watch. *(Verified, [GX20])*
- **`knowledge-visualization.md`** (V1–V18) — the docs frontmatter graph, V14 relation registry, V15
  graph-aware grounding (the server-side node-walk). *(Verified, pack)*
- **Obsidian 3D/analysis plugins** — InfraNodus (https://infranodus.com/obsidian-plugin), New 3D Graph
  (https://github.com/Apoo711/obsidian-3d-graph), GraphForge (https://github.com/bozoinc/graphforge-obsidian). *(Verified, [GX12][GX13][GX14])*

## Hosting

- **WebView2** — https://learn.microsoft.com/en-us/microsoft-edge/webview2/ — proprietary but free; the pane
  host for a JS force-graph; process model & airspace in `ai-native-ide-shell`. *(Verified, [GX21])*
- **Codebase KG for AI agents** — https://www.developersdigest.tech/blog/codebase-knowledge-graphs-ai-coding-agents. *(Verified, [GX22])*
