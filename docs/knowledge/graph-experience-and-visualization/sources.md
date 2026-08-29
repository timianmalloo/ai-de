---
id: kb-graph-experience-sources
title: "Unified Graph Experience — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-graph-experience-and-visualization, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The full access-dated source list behind the graph-experience-and-visualization base, keyed
  [GX1]..[GX22] as cited throughout the topic.
---

# Sources

All accessed **2026-08-29**. Citation keys `[GXn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| GX1 | Project GraphRAG (Microsoft Research) | primary (vendor) | https://www.microsoft.com/en-us/research/project/graphrag/ | Local/global search, community detection |
| GX2 | microsoft/graphrag | primary (repo) | https://github.com/microsoft/graphrag | MIT pipeline; LightRAG comparison via guide |
| GX3 | LazyGraphRAG: setting a new standard for quality and cost | primary (vendor blog) | https://www.microsoft.com/en-us/research/blog/lazygraphrag-setting-a-new-standard-for-quality-and-cost/ | ~0.1% index cost, ~700× cheaper global |
| GX5 | vasturiano/react-force-graph | primary (repo) | https://github.com/vasturiano/react-force-graph | MIT, 2D/3D/VR/AR React bindings |
| GX6 | vasturiano/3d-force-graph | primary (repo) | https://github.com/vasturiano/3d-force-graph | ThreeJS/WebGL 3D, physics, interactions |
| GX7 | Sigma.js | primary (site/repo) | https://www.sigmajs.org/ | MIT, WebGL, 100k+ nodes, Graphology |
| GX8 | reaviz/reagraph | primary (repo) | https://github.com/reaviz/reagraph | Apache-2.0, React WebGL 2D/3D |
| GX9 | Cytoscape.js | primary (site) | https://js.cytoscape.org/ | MIT, mature, progressive disclosure |
| GX10 | Cosmograph | primary (vendor) | https://cosmograph.app/ | GPU multi-million; non-commercial/commercial licence |
| GX11 | GraphX for .NET (panthernet + westermo fork) | primary (repo) | https://github.com/panthernet/GraphX | Native WPF, 2D only |
| GX12 | InfraNodus AI Graph View (Obsidian) | primary (vendor) | https://infranodus.com/obsidian-plugin | Betweenness, community, gap detection, AI |
| GX13 | Obsidian New 3D Graph (Apoo711) | primary (repo) | https://github.com/Apoo711/obsidian-3d-graph | Rust+WASM 3D, open |
| GX14 | GraphForge (Obsidian) | primary (repo) | https://github.com/bozoinc/graphforge-obsidian | Immersive 3D graph |
| GX15 | xyflow / React Flow + awesome-node-based-uis | primary (repo) | https://github.com/xyflow/xyflow | MIT node-editor; curated list |
| GX16 | litegraph.js | primary (repo) | https://github.com/jagenjo/litegraph.js | Node editor + dataflow |
| GX17 | rete.js | primary (repo) | https://github.com/retejs/rete | Visual programming framework |
| GX18 | KnowledgeCanvas/knowledge | primary (repo) | https://github.com/KnowledgeCanvas/knowledge | Electron KG + chat desktop app; **discontinued** |
| GX19 | AI-Forward `code-knowledge-graph.md` (GK1–GK16) | primary (pack standard) | (in-repo) `.github/instructions/` | Graphify canonical source, provenance, `--join` |
| GX20 | AI-Forward `obsidian-lens.md` (OB1–OB14) | primary (pack standard) | (in-repo) `.github/instructions/` | Obsidian reader, `--analyze`, egress, hub watch |
| GX21 | WebView2 documentation | primary (official) | https://learn.microsoft.com/en-us/microsoft-edge/webview2/ | Pane host for JS force-graph |
| GX22 | Coding agents need codebase maps (developersdigest) | secondary | https://www.developersdigest.tech/blog/codebase-knowledge-graphs-ai-coding-agents | Codegraph/Understand-Anything/Copilot canvases |

## Source-quality notes

- **GraphRAG cost figures:** the 26–85× baseline is from `code-knowledge-graphs` (Microsoft's own eval); the
  LazyGraphRAG ~700× and ~0.1% figures are from Microsoft's blog ([GX3]) and are **document-corpus** results —
  flagged as unproven for code graphs.
- **Library licences** (Sigma.js, Cytoscape.js, react/3d-force-graph, Reagraph, React Flow, litegraph, rete)
  are cited to each project's repo/site and are MIT/Apache; individual `LICENSE` files were **not** re-fetched
  this session — a quick check before adopting any as a dependency. GraphX's exact SPDX (MIT-family) should be
  read from the chosen fork.
- **Graphify** is deliberately cited to the pack's own standard ([GX19]) rather than a web search, because the
  name is overloaded and the pack already established the canonical source and the `.net` caution (GK15).
- **Cosmograph** is included only to record that its performance is real and its **licence excludes it** — not
  a candidate.
