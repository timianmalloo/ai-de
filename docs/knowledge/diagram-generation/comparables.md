---
id: kb-diagrams-comparables
title: "Diagram Generation — comparable tools"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, mermaid, d2, plantuml, structurizr, cytoscape]
links:
  - { to: kb-diagram-generation, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Side-by-side comparison of diagram DSLs and interactive graph renderers by diagram type,
  licence, rendering model and failure mode — the table that decides which renderer serves
  which view.
---

# Comparable solutions & problem framings

## Text DSLs (generate → render)

| Tool | Diagram types | Licence | Rendering model | Does well | Does badly | Confidence |
|---|---|---|---|---|---|---|
| **Mermaid 11.x** | flow, sequence, class, ER, C4, state, gantt, git, timeline, block, architecture (beta) | MIT | JS in-browser (SVG); CLI via Puppeteer | GitHub-native, Markdown-embeddable, no server, renders in a WebView pane | Large graphs; C4 is a convenience layer not the model; PNG/SVG output likely non-deterministic across Chrome versions | Verified [S1][S3]; scale + determinism **Flagged** |
| **D2 v0.7.1** | general architecture, flowchart, sequence, SQL tables, classes | **MPL-2.0** | CLI → SVG/PNG/PDF; Go library | Three layout engines, sketch mode, deterministic CLI, embeddable as a Go library | No native ER/UML semantics; TALA is a separate proprietary binary | Verified [S4][S5]; TALA licence **Flagged** |
| **PlantUML v1.2026.6** | the widest set: sequence, class, activity, component, state, object, deployment, timing, network, ER (IE), C4, ArchiMate, BPMN, MindMap, WBS, Gantt | GPL / LGPL / MIT / Apache | Java JAR (+GraphViz); Docker server | Broadest coverage; best sequence diagrams | Java dependency; **`LEGACY` security default is an arbitrary-file-read hole**; large class diagrams unwieldy | Verified [S6][S7][S8] |
| **Graphviz / DOT** | any node-edge structure | EPL-1.0 | CLI (`dot`, `neato`, `fdp`, `sfdp`, `circo`, `twopi`) | `dot` is deterministic; fine layout control; the backend under many other tools | No UML semantics; verbose source; opaque to tune; `neato` documented at ~100 nodes | Verified [S9][S10] |
| **Structurizr DSL** | C4 Context/Container/Component/Deployment/Dynamic/Landscape + custom | Lite free/OSS; Cloud & on-prem paid | Browser; exports to PlantUML/Mermaid/SVG/PNG/JSON | **The** C4 reference implementation; model/views separation; one model, many exports | Needs the Structurizr runtime for interactive rendering; Java for on-prem; not general-purpose | Verified [S13]–[S17] |
| **Kroki** | 20+ DSLs as a gateway | Apache-2.0 (server) | HTTP API → backend → SVG/PNG | One endpoint for polyglot pipelines; self-hostable; simplifies CI | Extra network hop; **inherits backend security posture**, including PlantUML `LEGACY` | Verified [S19][S20] |
| **Diagrams (mingrammer)** | cloud/infrastructure only | MIT | Python → Graphviz → PNG/SVG | Cloud provider icon sets; IaC-adjacent | Not for UML/ER/sequence; needs Graphviz; directed node-edge only | Verified [S27] |
| **Ilograph** | interactive C4-style multi-perspective | Proprietary | Browser | Multi-perspective from one model; good UX | Closed; limited export | Inferred [S28] |

## Interactive graph renderers (explore, don't generate)

| Library | Licence | Substrate | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **Cytoscape.js** | MIT | Canvas (SVG via plugin), headless Node | Analysis + visualisation together; JSON-serialisable; extension ecosystem | Not a DSL — you write code; no built-in layout for very large graphs | Verified [S21] |
| **Sigma.js + Graphology** | MIT | **WebGL** | Large graphs; ForceAtlas2; clean data/render separation | Custom rendering harder than SVG; React needs a wrapper | Verified [S22][S23] |
| **vis-network** | Apache-2.0 | Canvas | Easy; accepts DOT input; clustering | Canvas only; degrades past "a few thousand" by its own docs | Verified [S24] |
| **G6 (AntV)** | MIT | Canvas + WebGL | Large layout library; TypeScript | Docs partly Chinese; v4→v5 API churn | Verified [S25] |
| **yFiles** | Commercial | Canvas/WebGL/SVG | Best-in-class layouts; **incremental layout** (the stability answer) | Expensive; closed | Verified [S29] |

## Layout engines (libraries, not renderers)

| Engine | Licence | Algorithm | Note |
|---|---|---|---|
| **elkjs** (Eclipse ELK) | **EPL-2.0** | Sugiyama layered, orthogonal routing, compound graphs, port constraints | Heavier and more configurable than dagre; claimed Web-Worker-capable *(Flagged — unconfirmed)* |
| **@dagrejs/dagre** | MIT | Layered (Dagre) | The maintained package; original `dagre` is stale. Underlies Mermaid and D2's Dagro |
| **d3-force** | ISC/BSD | Force-directed | Non-deterministic without a seed |
| **Graphviz engines** | EPL-1.0 | dot / neato / fdp / sfdp / circo / twopi | `dot` deterministic; `neato` supports `pin` |

*(Verified, [S18][S26][S9][S10])*

## Adjacent approaches worth borrowing from

- **Structurizr's model/views split** is the pattern this whole project is an instance of: one model, many
  projections, no diagram authored twice. Worth copying even where Structurizr itself is not used.
- **Kroki's gateway shape** — normalising N renderers behind one interface — is the right seam if more than
  two renderers are ever supported.
- **yFiles' incremental layout** is the only commercial answer to the stability problem, and knowing it
  exists is useful even if it is never bought: it establishes that the problem is real and non-trivial.
