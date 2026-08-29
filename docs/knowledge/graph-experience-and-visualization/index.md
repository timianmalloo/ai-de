---
id: kb-graph-experience-and-visualization
title: "Unified Graph Experience & Visualization — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [knowledge-graph, graphrag, obsidian, graphify, graph-visualization, 3d, force-graph, node-introspection]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: kb-code-knowledge-graphs, rel: relates-to }
  - { to: kb-diagram-generation, rel: relates-to }
  - { to: kb-ai-native-ide-shell, rel: relates-to }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Evidence base for the AI-DE end-to-end graph experience — a unified code-graph + knowledge-graph
  a user navigates and introspects node by node (walk from a C# file to the knowledge that informed
  it). Covers GraphRAG and its cheaper variants, composing Obsidian + Graphify, 2D/3D graph
  visualization libraries, node-based UIs, and how to host a graph explorer in a WPF/WebView2 shell.
---

# Unified Graph Experience & Visualization — domain knowledge

**Domain & problem:** AI-DE builds a knowledge graph over code artifacts and wants a **rich, navigable,
introspectable experience** over the *union* of the **code graph** (symbols, calls, imports, schemas — from
Graphify) and the **knowledge graph** (specs, ADRs, designs, decision notes — the Obsidian/docs graph). The
core scenario is *node-walking with introspection*: click a node that is a C# file → read its code → step to
the related metadata and knowledge that informed the implementation → step onward to callers, tests, or the
design that specified it. The graph must be **visualized** (2D, optionally 3D), **queryable by the LLM**
(GraphRAG), and **hosted inside the WPF editor** we are building.

**Canonical framing:** The field frames this as three separate things that AI-DE must fuse: (1) **GraphRAG** —
retrieval that walks a knowledge graph instead of flat vector chunks (Microsoft's framing; good for multi-hop,
expensive for global); (2) **network/graph visualization** — force-directed 2D/3D renderers (the web-viz
framing: Sigma.js, Cytoscape.js, react/3d-force-graph); (3) **personal-knowledge graphs** — Obsidian's
backlink graph + plugins. Our framing is the fusion the pack already names: **"docs hold intent, code holds
reality, and the expensive defects live in the gap"** (`code-knowledge-graph.md` GK). No off-the-shelf product
does the code↔knowledge join *and* an in-editor introspection walk — which is the opportunity and the reason
this must be composed, not bought.

**Compiled:** 2026-08-29 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` carries the library licences, node-count thresholds and the GraphRAG cost figures to
quote rather than recall.)*

## Headline findings

1. **GraphRAG got dramatically cheaper, which materially updates our prior finding.** The existing
   [`code-knowledge-graphs`](../code-knowledge-graphs/index.md) base (finding #8) recorded GraphRAG global
   queries at **26–85× the token cost** of local vector retrieval. Microsoft's **LazyGraphRAG** (late 2024)
   skips LLM summarisation at index time (indexing cost ~0.1% of full GraphRAG, matching vector RAG) and does
   graph reasoning at query time — reported **~700× cheaper global queries** at competitive quality. **LightRAG**
   adds incremental graph updates (no full rebuild) for small/commodity setups. The old "GraphRAG is too
   expensive for global context" conclusion is now **Flagged for review**, not abandoned — the *hybrid* rule
   (vector first, graph for relational context) still dominates. — *(Verified, [GX1][GX2][GX3]; supersedes-in-part code-knowledge-graphs #8)*
2. **The retrieval win of GraphRAG is exactly our core scenario: multi-hop traversal.** GraphRAG beats vector
   RAG on multi-entity, relational, "how does X connect to Y across the codebase" questions and loses on simple
   lookups. "Walk from this C# file to the design that informed it" is a multi-hop relational query — the case
   GraphRAG is *for*. Community detection (grouping related nodes into hierarchical clusters) is the mechanism
   that makes global summaries affordable. — *(Verified, [GX1][GX2])*
3. **The pack already owns the two substrates — Graphify (code) and Obsidian (docs) — and the composition rule
   is stated: docs=intent, code=reality, join = the value.** `code-knowledge-graph.md` (GK1–GK16) establishes
   **Graphify** (on-device, Apache-2.0, PyPI `graphifyy`) mapping edge provenance `EXTRACTED/INFERRED/AMBIGUOUS`
   onto the pack's Verified/Inferred/Flagged; `obsidian-lens.md` (OB1–OB14) establishes Obsidian as a **reader**
   over the docs frontmatter graph. The `--join` lens ("documentation with no implementation; risk with no
   governance") is the code↔docs bridge our node-walk traverses. — *(Verified, pack standards [GX19][GX20])*
4. **Graphify is an overloaded name — verify the source.** The canonical Graphify is **graphify.com /
   github.com/Graphify-Labs/graphify (Apache-2.0, PyPI `graphifyy` — double-y)**; **`graphify.net` is
   unaffiliated** (pack GK15, defect class PACK-E/RID-D). A web search for "Graphify" returns the wrong product
   confidently. Establish it from the canonical source before wiring. — *(Verified, [GX19])*
5. **For an embedded graph explorer, the pragmatic path is a web force-graph in a WebView2 pane, not a native
   WPF graph control.** The two real options: **GraphX for .NET** (MIT-family, native WPF, 2D only, "not as
   cutting-edge", limited maintenance) or **embed a JS force-graph in WebView2** (Sigma.js / Cytoscape.js /
   3d-force-graph — WebGL, 2D+3D, huge ecosystem, progressive disclosure). The AI-DE shell *already hosts
   WebView2 panes* (`ai-native-ide-shell`), so the web path reuses infrastructure and gets 3D + large-graph
   performance GraphX cannot match; the cost is the .NET↔JS message bridge. — *(Verified, [GX11]; the recommendation Inferred)*
6. **The permissive 2D/3D graph-viz field is strong and MIT/Apache-dominated.** **Sigma.js** (MIT, WebGL, 100k+
   nodes 2D, the large-graph workhorse); **Cytoscape.js** (MIT, mature, algorithm-rich, progressive disclosure —
   already selected for the interactive tier in `diagram-generation`); **react-force-graph / 3d-force-graph**
   (vasturiano, MIT ecosystem, ThreeJS/WebGL, 2D/3D/VR/AR, click-to-focus, expand/collapse, ~4k-element "large"
   examples); **Reagraph** (reaviz, **Apache-2.0**, React WebGL 2D/3D). **Cosmograph** is GPU/multi-million-node
   but **non-commercial/commercial-licensed — avoid.** — *(Verified, [GX5][GX6][GX7][GX8][GX9][GX10])*
7. **3D graph views are exploration candy; 2D + network-science metrics carry the insight.** 3D force-graphs
   (and Obsidian's 3D plugins) help *navigate* large dense graphs and are engaging, but 3D adds occlusion and
   hurts precise reading. The insight comes from **network-science metrics on the 2D graph** — betweenness
   centrality (the bridges), community detection (the clusters), and **gap detection** (structural holes) —
   exactly what `obsidian-lens.md` OB14 ("watch the hub") and Graphify's god-nodes already compute. Offer 3D as
   a mode, not the default. — *(Verified, [GX12][GX13]; the 3D-caveat Inferred)*
8. **Obsidian's plugin ecosystem is a proven reference for graph navigation, but the good ones are
   analysis-first, not just pretty.** **InfraNodus AI Graph View** (betweenness, community, gap detection, AI
   ideation — but commercial + hosted AI, an egress decision), **New 3D Graph** (Apoo711, Rust+WASM, fast,
   open), **GraphForge** (immersive). Borrow the *analysis features* (metrics, gap detection), keep Obsidian a
   reader (OB1), and never make egress a default (OB11). — *(Verified, [GX12][GX13][GX14])*
9. **Node-based UI libraries are the reference for the "introspect and step" interaction, and React Flow leads.**
   **React Flow / xyflow** (MIT, production-tested) and the **awesome-node-based-uis** list (curated by the same
   team) are the canon; **litegraph.js**, **rete.js**, **Drawflow** are framework-agnostic alternatives. These
   solve *node selection → detail panel → edge-following* — the exact interaction of the node-walk — better than
   a raw force-graph, which is a *layout*, not an *editor*. The distinction matters: a force-graph shows the
   whole; a node-editor lets you work a neighbourhood. — *(Verified, [GX15][GX16][GX17])*
10. **Knowledge-Canvas-style desktop apps validate the pattern and warn about sustainability.** `KnowledgeCanvas/
    knowledge` (Electron, built-in Chromium, graph view + LLM chat over sources) is exactly the "navigate +
    chat with your knowledge graph" desktop shape — and it is **no longer developed**. Codegraph, Understand-
    Anything and GitHub Copilot **canvases** are the current instances of "codebase map for agents". The pattern
    is validated; the lesson (from `code-knowledge-graphs` too) is **design for project mortality** — own the
    graph and the renderer boundary. — *(Verified, [GX18][GX22])*

## Confidence summary

- **Verified:** GraphRAG/LazyGraphRAG/LightRAG mechanics and the cost figures; the MIT/Apache licences of
  Sigma.js, Cytoscape.js, react/3d-force-graph, Reagraph, React Flow, litegraph, rete; GraphX's native-WPF /
  2D-only profile; the Graphify canonical-source disambiguation; Knowledge Canvas's discontinuation.
- **Inferred:** the WebView2-force-graph-over-native-GraphX recommendation; the 3D-is-candy caveat; the
  node-editor-vs-force-graph interaction distinction.
- **Flagged (load-bearing):** whether **LazyGraphRAG's 700× figure holds for a *code* graph** (Microsoft's
  numbers are document-corpus QA, not code — untested here; the existing base's "measure it yourself" rule
  applies); **Cosmograph's exact licence tiers** (non-commercial confirmed, boundaries not enumerated); and the
  precise node-count at which a WebView2 SVG/Canvas force-graph degrades (~a few thousand DOM/SVG elements per
  `diagram-generation`; WebGL renderers go far higher).

## Design implications (what /design should do with this)

- **Build node introspection as a typed router, not a viewer.** The core scenario ("walk from a C# file to the
  knowledge that informed it") is: *select node → inspect panel routes by node type*. A **code** node opens the
  code renderer (see [`editor-and-content-rendering-surfaces`](../editor-and-content-rendering-surfaces/index.md));
  a **knowledge/markdown** node opens the markdown renderer; a **diagram** node opens the diagram pane
  (`diagram-generation`); every panel exposes the node's **typed edges** (Graphify `EXTRACTED/INFERRED/AMBIGUOUS`
  and the docs `implements/refines/tested-by/documents/uses-term`) as the "step onward" affordance. This router
  *is* the product; the force-graph is one view onto it.
- **Host the graph explorer as a web force-graph in a shared WebView2 pane.** Reuse the single
  `CoreWebView2Environment` (`ai-native-ide-shell`); render with **Sigma.js/Cytoscape.js (2D, large, progressive
  disclosure)** and offer **3d-force-graph (3D mode)**; bridge selection/hover events to .NET over
  `postMessage`. Do **not** build a native GraphX explorer unless a no-web constraint appears.
- **Query the graph with hybrid retrieval, graph for the multi-hop.** Vector-first for lookups; GraphRAG-style
  community/traversal for relational "how does this connect" questions; keep MCP tools returning **bounded
  neighbourhoods and summaries**, never whole subgraphs (the 26–85×-cost rule survives even with LazyGraphRAG).
  Evaluate LazyGraphRAG/LightRAG for the code graph before committing — the cost win is document-corpus-proven,
  not code-proven.
- **Compute and surface network-science metrics, default to 2D.** Betweenness (bridges), community (clusters),
  gap detection (structural holes) — from Graphify god-nodes and the Obsidian `--analyze` — are the insight
  layer; colour/size nodes by them. 3D is a toggle for exploration, not the default reading view.
- **Traverse the code↔docs join as a first-class edge.** The Graphify `--join` lens (documentation-without-
  implementation, risk-without-governance) is the bridge the node-walk crosses from a C# file to its governing
  design; make those join edges navigable, and carry each edge's **provenance/confidence** on the wire.
- **Design for mortality.** Keep the graph model and the renderer behind interfaces; Knowledge Canvas, Kuzu,
  Sourcetrail and Stack Graphs all died — the export format and the seam are what make the next death a
  migration (`code-knowledge-graphs`).

## Cross-references

- Graph *store* & GraphRAG cost baseline → [`code-knowledge-graphs`](../code-knowledge-graphs/index.md) (whose
  finding #8 this base updates — flagged there).
- Diagram DSL rendering & the interactive-tier renderer choice → [`diagram-generation`](../diagram-generation/index.md).
- The WPF/WebView2 host, airspace, one-environment rule → [`ai-native-ide-shell`](../ai-native-ide-shell/index.md).
- The code renderer and markdown/html rendering for the introspection panels →
  [`editor-and-content-rendering-surfaces`](../editor-and-content-rendering-surfaces/index.md).
- The chrome the explorer lives in → [`wpf-modern-ui-styling`](../wpf-modern-ui-styling/index.md).
- Governing pack standards: `code-knowledge-graph.md` (Graphify, GK1–GK16), `obsidian-lens.md` (OB1–OB14),
  `knowledge-visualization.md` (the docs graph, V1–V18).

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The licences and cost figures in
`references.md`/`data-and-constants.md` are the ones to quote. Refresh when a GraphRAG variant or a graph-viz
library ships a major version — this ecosystem moves monthly.
