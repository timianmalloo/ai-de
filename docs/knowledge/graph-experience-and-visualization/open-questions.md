---
id: kb-graph-experience-open-questions
title: "Unified Graph Experience — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, graphrag, graph-visualization, disconfirming]
links:
  - { to: kb-graph-experience-and-visualization, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  What the graph-experience research could not settle, the domain's silent failure modes, and the
  disconfirming views deliberately sought against building a custom in-editor graph explorer.
---

# Open questions & domain failure modes

## Unresolved by research

- **Does LazyGraphRAG's 700× win hold for a *code* graph?** Microsoft's figures are document-corpus QA. A code
  graph has different topology (dense call/import edges, stable symbol IDs) and different queries ("impact of",
  "who calls"). Measure on a real C# solution before committing — the `code-knowledge-graphs` "nobody has
  published C# graph numbers" hole applies here too. *(Flagged.)*
- **At what node count does the WebView2 force-graph pane stop being usable?** WebGL (Sigma/3d-force-graph) goes
  to 100k+ but interaction (labels, hit-testing, physics) degrades earlier; the real limit for a *usable*
  introspection view is unmeasured. A cheap spike: render a real repo's graph and find the knee. *(Flagged.)*
- **Cosmograph's licence boundaries** — non-commercial is confirmed; the exact commercial terms were not
  enumerated. Moot if excluded, but confirm before any evaluation. *(Flagged.)*
- **How much of the "chat with the graph" UX should be built vs. delegated to the MCP/agent layer?** Knowledge
  Canvas and Copilot canvases both bundle chat; whether AI-DE renders its own chat pane or routes to the agent
  terminals is a product decision, not settled here. *(Flagged.)*

## Known failure modes of this domain

- **The hairball.** A force-graph of a whole codebase with no filtering/clustering/progressive-disclosure is
  unreadable noise. Every viable explorer filters to a neighbourhood; "show the whole graph" is the anti-pattern
  (`diagram-generation`: no DSL pipeline does progressive disclosure — the interactive renderer must). *(Verified.)*
- **3D occlusion.** 3D looks impressive and hides nodes behind nodes; precise reading and selection get worse.
  3D is a navigation mode, not the default. *(Verified, [GX13].)*
- **Provenance laundering.** Rendering an `INFERRED`/`AMBIGUOUS` edge identically to an `EXTRACTED` one makes the
  UI *more* convincingly wrong (GK7). The introspection panel must show edge confidence. *(Verified, GK.)*
- **Global-query cost blowout.** Even with LazyGraphRAG, an unbounded "summarise the whole graph" query is
  expensive; MCP tools must return bounded neighbourhoods. *(Verified, code-knowledge-graphs #8.)*
- **Graphify name confusion.** Wiring `graphify.net` (unaffiliated) instead of the canonical `graphifyy` is a
  supply-chain event (GK15, PACK-E). *(Verified.)*
- **Project mortality.** Knowledge Canvas, Kuzu, Sourcetrail, Stack Graphs all died; a hard dependency on any one
  renderer/store without an export seam inherits its death. *(Verified.)*

## Disconfirming views we deliberately sought

- **"Don't build a graph explorer — Obsidian already renders the graph, and Graphify already answers queries."**
  Strong and partly correct: the pack deliberately keeps Obsidian a *reader* and Graphify a *query engine*. The
  case *against* a custom explorer is real — building graph viz badly is easy, and the two tools exist. **But**
  neither does the **code↔knowledge node-walk *inside the editor*** the user asked for: Obsidian sees docs, not
  code reality; Graphify answers text queries, not a visual introspection walk. **Verdict:** embed, don't
  reinvent — render with an existing JS force-graph in WebView2, compute metrics with the existing `--analyze`/
  god-nodes, and add only the *node-introspection router* that fuses the two graphs and routes each node to its
  renderer. That router is the genuinely-new, load-bearing piece; everything under it is borrowed. This narrows
  the base rather than refuting it.
- **"3D knowledge graphs are the differentiator."** Sought and rejected as a headline: the Obsidian 3D plugins
  are popular, but the *insight* (InfraNodus's own value prop) is the **network-science analysis** (betweenness,
  community, gaps), which is 2D. 3D is engagement, not understanding. Ship it as a mode; do not lead with it.
- **"Native GraphX keeps it all in .NET — avoid the WebView2 bridge."** Considered; the airspace and one-
  environment findings (`ai-native-ide-shell`) already commit the shell to WebView2 panes, so the bridge exists
  regardless, and WebGL buys 3D + 10–100× the node scale. Native GraphX stays the fallback for a no-web
  constraint. The finding survives.
