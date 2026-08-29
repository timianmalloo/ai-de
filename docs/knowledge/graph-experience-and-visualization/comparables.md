---
id: kb-graph-experience-comparables
title: "Unified Graph Experience — comparables & libraries"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [graph-visualization, libraries, licences, obsidian-plugins, node-editors]
links:
  - { to: kb-graph-experience-and-visualization, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Named graph-visualization libraries, node-based UI frameworks, Obsidian graph plugins, and
  desktop knowledge-graph apps — with licence, role and fit for an embedded WPF/WebView2 explorer.
---

# Comparable solutions, libraries & apps

## Graph-visualization libraries (for a WebView2-hosted explorer)

| Library | Licence | Dim | Scale | Role / fit | Confidence |
|---|---|---|---|---|---|
| **Sigma.js** (+ Graphology) | MIT | 2D | 100k+ (WebGL) | The large-graph 2D workhorse; analytics-oriented | Verified [GX7] |
| **Cytoscape.js** | MIT | 2D (partial 3D ext) | ~100k | Mature, algorithm-rich, **progressive disclosure**; already picked for the interactive tier | Verified [GX9] |
| **3d-force-graph / react-force-graph** | MIT | 2D+3D (VR/AR) | ~4k "large" (more w/ degradation) | ThreeJS/WebGL 3D; click-to-focus, expand/collapse, HTML-in-node | Verified [GX5][GX6] |
| **Reagraph** | Apache-2.0 | 2D+3D | large (WebGL) | React-first, clustering, modern UX | Verified [GX8] |
| **Cosmograph** | **Non-commercial/commercial** | 2D+3D | multi-million (GPU) | Fastest, but **licence excludes it** | Verified [GX10] |
| **GraphX for .NET** | MIT-family | 2D | large-ish | Native WPF/MVVM; no 3D; dated but no-web-dependency | Verified [GX11] |

## Node-based UI frameworks (the introspection/step interaction)

| Library | Licence | Ecosystem | Role | Confidence |
|---|---|---|---|---|
| **React Flow / xyflow** | MIT | React (Svelte Flow sibling) | Leading node-editor; selection→detail→edges | Verified [GX15] |
| **litegraph.js** | MIT | vanilla JS | Node editor + dataflow engine | Verified [GX16] |
| **rete.js** | MIT | React/Vue/Angular/Svelte | Visual programming / dataflow | Verified [GX17] |
| **Drawflow** | MIT | vanilla JS | Lightweight flow editor | Verified [GX15] |
| **awesome-node-based-uis** | (list) | — | Curated index (xyflow team) | Verified [GX15] |

## Obsidian graph plugins (analysis + 3D references)

| Plugin | Note | Confidence |
|---|---|---|
| **InfraNodus AI Graph View** | Betweenness, community, **gap detection**, AI ideation — commercial + hosted AI (egress) | Verified [GX12] |
| **New 3D Graph** (Apoo711) | Rust+WASM, fast, open; large vaults, filtering | Verified [GX13] |
| **GraphForge** (bozoinc) | Immersive 3D, themes, physics | Verified [GX14] |
| **Native Obsidian graph** | The baseline; `obsidian-lens.md` OB14 hub watch; `--analyze` (dependency-free metrics) | Verified (pack) |

## Desktop / product comparables (the pattern, and its mortality)

| Product | Framing | Borrow | Avoid | Confidence |
|---|---|---|---|---|
| **Knowledge Canvas** (Electron) | Save/search/chat with sources + graph view + built-in Chromium | The navigate+chat+graph desktop shape; right-click "extract topics" | It is **discontinued** — design for mortality | Verified [GX18] |
| **Codegraph / Understand-Anything** | Pre-indexed local codebase KG for agents (Copilot/Claude/Cursor) | Auto-sync, token-efficient graph for agents | Liveness unverified | Verified [GX22] |
| **GitHub Copilot canvases** | Prompt-driven interactive codebase diagram in the Copilot app | Interactive node-click-to-relationships UX | Proprietary, moving | Verified [GX22] |
| **Microsoft GraphRAG / Discovery** | Graph retrieval in a Copilot-like agent | The local/global + community-summary retrieval model | Document-corpus, not code | Verified [GX1] |

## Adjacent problems worth borrowing from

- **Network science** — betweenness/community/modularity/structural-holes are the *insight* layer over any
  graph; the pack's `obsidian-lens.md --analyze` and Graphify god-nodes already compute them dependency-free.
- **The pack's diagram tier** — `diagram-generation` already solved renderer-by-scale, layout stability, and the
  MIT-vs-copyleft licence map for the interactive renderers; this base inherits it.
- **The pack's docs graph** — `knowledge-visualization.md` V15 (graph-aware grounding) is the *server-side* of
  the same node-walk the UI performs on screen.
