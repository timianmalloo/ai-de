---
id: kb-diagrams-sota
title: "Diagram Generation — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [mermaid, d2, plantuml, structurizr, graphviz, elk, layout]
links:
  - { to: kb-diagram-generation, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  What each diagram DSL and renderer actually is today — versions, diagram types, rendering
  model and security posture — plus the layout algorithms underneath and the unsolved
  layout-stability problem.
---

# State of the art — diagram generation & rendering

## The text DSLs

### Mermaid

MIT. Version reported as **11.17.0** in `packages/mermaid/CHANGELOG.md`; the monorepo root `package.json`
shows `10.2.4`, which is a stale root artifact — **read the package, not the root**, and confirm with
`npm view mermaid version` before pinning. *(Verified [S1][S2]; the published-version ambiguity Flagged)*

Stable diagram types: flowchart, `sequenceDiagram`, `classDiagram`, `erDiagram`, gantt, gitGraph,
`stateDiagram`, pie, quadrantChart, requirementDiagram, timeline, xyChart. Newer / in active development:
C4 (`C4Context`, `C4Container`, `C4Component`, `C4Dynamic`, `C4Deployment`), architecture diagrams (beta),
block, packet, kanban, treeView. As of 11.17.0 `classDiagram` routes through a unified v2 renderer by
default (legacy is an opt-out flag) and C4 elements render through the unified shape system. *(Verified, [S1])*

API: `mermaid.render(id, definition)` → `{ svg, bindFunctions }`, async, DOMPurify bundled. Security levels:
`strict` (default, sanitised), `loose` (inline HTML), `antiscript`, `sandbox` (iframe isolation) — `strict`
for anything user-supplied. Accessibility: `accTitle` / `accDescr` emit SVG `<title>` / `<desc>`.
Headless: `mermaid-cli` (`mmdc`) wrapping Puppeteer/Chromium, Docker image `minlag/mermaid-cli`, outputs
SVG/PNG/PDF. *(Verified, [S1][S3])*

### D2

**MPL-2.0** (file-level copyleft — usable in a proprietary product provided D2's own files are not
modified). Current release **v0.7.1**. Layout engines: **Dagro** (bundled Go port of Dagre, default),
**elk-go** (bundled Go port of ELK Layered), and **TALA** (proprietary, separate binary from
`terrastruct/TALA`, licence not established here). Outputs SVG, PNG, PDF; `d2 --watch in.d2 out.svg` gives
live reload; usable as a Go library. Sketch mode via `rough-go`; LaTeX labels via `mathjax-go`.
*(Verified [S4][S5]; TALA licence Flagged)*

### PlantUML

Current **v1.2026.6**. Licences offered on the same download page: GPL (full features), LGPL (no bundled
GraphViz/ditaa), plus MIT and Apache variants for specific builds. Java 11+ required, always. GraphViz
required for some diagram types; embedded on Windows, separate install on Linux. *(Verified, [S6][S8])*

Widest diagram-type coverage of any tool here: sequence, use case, class, activity (legacy and beta
renderers), component, state, object, deployment, timing, network (nwdiag), wireframe (salt), Gantt,
MindMap, WBS, C4 (via the C4-PlantUML library), ER in IE notation, JSON/YAML visualisation, ArchiMate, BPMN
via plugin, and raw DOT pass-through. *(Verified, [S6])*

**Security — the critical fact.** Since v1.2020.11 PlantUML has security profiles: `LEGACY` (**the
default**; full local filesystem and URL access), `INTERNET`, `ALLOWLIST`, `SANDBOX`. Set via environment
variable or JVM `-D`. A PlantUML server or CI renderer left on `LEGACY` is an arbitrary-file-read surface.
*(Verified, [S7])*

### Structurizr DSL

The C4 model's reference tooling, and structurally the closest thing to AI-DE's own architecture: a
`workspace {}` contains a `model {}` (persons, softwareSystems, containers, components, deploymentEnvironments,
relationships) and a `views {}` (systemContext, container, component, dynamic, deployment, filtered, image,
custom). The model is the single source of truth; every view is a projection. *(Verified, [S13][S14])*

Distribution: **Lite** is free and open source, single-user, Docker, file-backed, no collaboration;
**Cloud** is paid SaaS; **on-premises** is a paid Jakarta EE/Spring 6 WAR on Tomcat 10+. *(Verified, [S15][S16])*

`structurizr-export` emits: `plantuml`, `plantuml/structurizr`, `plantuml/c4plantuml`, `mermaid`,
`websequencediagrams`, static HTML site, PNG, SVG, JSON, theme — with custom exporters via a JAR plugin
implementing `WorkspaceExporter` (the Ilograph exporter is the worked example). *(Verified, [S17])*

### Others

- **Graphviz / DOT**, EPL-1.0. Engines: `dot` (hierarchical/directed, minimises crossings — the DAG
  workhorse), `neato` (undirected, stress majorization / Kamada-Kawai, documented at **"about 100 nodes"**,
  supports the `pin` attribute), `fdp` (Fruchterman-Reingold force-directed), `sfdp` (multiscale, very large
  graphs), `circo`, `twopi`, `osage`, `patchwork`. `dot` is deterministic for identical input; `neato`/`fdp`
  vary with the `start` seed. *(Verified, [S9][S10])*
- **Kroki**, Apache-2.0 server. One HTTP API (GET with deflate+base64 in the URL, or POST) fronting 20+
  backends: BlockDiag family, BPMN, Bytefield, C4+PlantUML, Diagrams.net (experimental), Ditaa, Erd,
  Excalidraw, GoAT, GraphViz, Mermaid, Nomnoml, PlantUML, Structurizr, SvgBob, Symbolator, UMLet, Vega,
  Vega-Lite, WaveDrom, WireViz. Self-hostable. Inherits its backends' security posture. *(Verified, [S19][S20])*
- **Diagrams (mingrammer)**, MIT — Python DSL emitting Graphviz DOT with cloud-provider icon node classes.
  Infrastructure diagrams only; no sequence or ER. *(Verified, [S27])*
- **Ilograph** — proprietary, YAML, multi-perspective from one model; Structurizr exports to it. *(Inferred, [S28])*

## Interactive rendering

| Library | Licence | Rendering | Documented scale posture |
|---|---|---|---|
| **Cytoscape.js** | MIT (core + first-party extensions) | Canvas by default; SVG via plugin; headless in Node | Rich analysis + visualisation; no built-in auto-layout for very large graphs |
| **Sigma.js** + **Graphology** | MIT | **WebGL** | Its own FAQ: "If you have small graphs (like a few hundred nodes and edges) … then d3.js is a better fit" |
| **vis-network** | Apache-2.0 | HTML Canvas only | Self-documented "smooth on any modern browser for up to a few thousand nodes and edges", clustering beyond |
| **G6 (AntV)** | MIT | Canvas + WebGL (v5) | Rich layout set; API churn v4→v5 |
| **yFiles** | Commercial | Canvas/WebGL/SVG | Most advanced layouts, incremental layout, any size |

*(Verified, [S21][S22][S23][S24][S25][S29])*

Rendering-substrate rule of thumb: **WebGL** for tens of thousands of nodes, hard to customise;
**Canvas** fast and pixel-based, the "few thousand" range; **SVG** fully styleable and accessible but each
element is a DOM node, so it degrades past roughly a thousand visible elements. *(The first two Verified
[S22][S24]; the SVG figure Inferred)*

## Layout algorithms and the stability problem

**Sugiyama, Tagawa & Toda (1981)** — the layered method: assign nodes to layers, reorder within layers to
minimise crossings, then assign coordinates. **ELK Layered** is the reference implementation, with
orthogonal routing and compound-graph support; `elkjs` is its JS port under **EPL-2.0**. `@dagrejs/dagre`
(MIT) is the lighter, widely embedded alternative — it is what Mermaid and D2's Dagro descend from — and
note that only the `@dagrejs/dagre` package is maintained; the original `dagre` is stale.
*(Verified, [S18][S26])*

**The stability problem is real and unsolved cheaply.** ELK Layered is deterministic for identical input,
but a single added node can re-rank the graph and move everything. Force-directed layouts are worse: without
a seeded start they differ run to run. The available mitigations are Graphviz `neato`'s `pin`, seeded
randomness, and commercial incremental layout (yFiles). For a tool that regenerates a diagram on every file
save, this is a headline design constraint rather than a detail. *(Verified, [S18][S10]; the conclusion Inferred)*

## The frontier

- **Filtering and progressive disclosure exist only in the interactive tier.** No DSL→SVG pipeline reviewed
  supports "show only nodes within N hops of X". Static generation therefore has a hard ceiling on graph
  size, and the answer is a different renderer, not a bigger file.
- **Determinism of rendered output is untested territory** for the JS-based renderers, and it is exactly
  what decides whether generated diagrams can be committed as images.
- **Accessibility is inconsistent**: Mermaid has a mechanism, the others do not document one.
