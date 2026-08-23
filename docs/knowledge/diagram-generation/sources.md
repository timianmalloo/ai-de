---
id: kb-diagrams-sources
title: "Diagram Generation — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-diagram-generation, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The full access-dated source list behind the diagram-generation knowledge base, keyed
  [S1]..[S29] as cited throughout the topic.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | Mermaid CHANGELOG (develop) | primary (source) | https://raw.githubusercontent.com/mermaid-js/mermaid/develop/packages/mermaid/CHANGELOG.md | Version 11.17.0, C4 unified shape system, classDiagram v2 default |
| S2 | Mermaid monorepo root `package.json` | primary (source) | https://raw.githubusercontent.com/mermaid-js/mermaid/develop/package.json | The 10.2.4 root-version caveat |
| S3 | mermaid-cli README | primary (repo docs) | https://github.com/mermaid-js/mermaid-cli | `mmdc`, Docker image, Puppeteer, `accTitle`/`accDescr` |
| S4 | D2 README + LICENSE.txt | primary (repo) | https://raw.githubusercontent.com/terrastruct/d2/master/README.md | MPL-2.0, layout engines, CLI, exports, sketch mode |
| S5 | D2 tour / intro | primary (docs) | https://d2lang.com/tour/intro | Language overview |
| S6 | PlantUML download | primary (official) | https://plantuml.com/download | v1.2026.6, licence options |
| S7 | PlantUML security | primary (official) | https://plantuml.com/security | `LEGACY`/`INTERNET`/`ALLOWLIST`/`SANDBOX`, default is LEGACY |
| S8 | PlantUML starting guide | primary (official) | https://plantuml.com/starting | Java 11+, GraphViz dependency, Docker |
| S9 | Graphviz — `dot` layout | primary (official) | https://graphviz.org/docs/layouts/dot/ | Hierarchical layout characteristics, determinism |
| S10 | Graphviz — `neato` layout | primary (official) | https://graphviz.org/docs/layouts/neato/ | "about 100 nodes", `pin` attribute, Kamada-Kawai |
| S11 | C4 model — notation | primary (author's site) | https://c4model.com/diagrams/notation | Notation independence, UML mapping |
| S12 | C4 model — home | primary | https://c4model.com/ | Four levels, L4 "often not worth the effort" |
| S13 | Structurizr DSL overview | primary (official) | https://docs.structurizr.com/dsl | DSL overview |
| S14 | Structurizr DSL language reference | primary | https://docs.structurizr.com/dsl/language | `workspace`/`model`/`views` keywords and separation |
| S15 | Structurizr Lite | primary | https://docs.structurizr.com/lite | Free/OSS, single-user, no collaboration |
| S16 | Structurizr on-premises | primary | https://docs.structurizr.com/onpremises | Paid WAR, Jakarta EE, Tomcat 10 |
| S17 | Structurizr export | primary | https://docs.structurizr.com/export | Export target list, `WorkspaceExporter` plugin, Ilograph exporter |
| S18 | Eclipse ELK — Layered algorithm | primary (official) | https://eclipse.dev/elk/reference/algorithms/org-eclipse-elk-layered.html | Sugiyama 1981 attribution, orthogonal routing, compound graphs |
| S19 | Kroki home | primary | https://kroki.io/ | HTTP API, GET/POST |
| S20 | Kroki documentation | primary | https://docs.kroki.io/kroki/ | Full backend DSL list |
| S21 | Cytoscape.js | primary | https://js.cytoscape.org/ | MIT, feature set, headless Node |
| S22 | Sigma.js | primary | https://sigmajs.org/ | WebGL, the d3 comparison quote, Graphology backend |
| S23 | Graphology | primary | https://graphology.github.io/ | MIT, graph object model |
| S24 | vis-network docs | primary | https://visjs.github.io/vis-network/docs/network/ | Canvas-only, "few thousand nodes and edges", clustering |
| S25 | G6 (AntV) | primary (repo) | https://github.com/antvis/G6 | MIT, TypeScript, v5 |
| S26 | dagre README | primary (repo) | https://github.com/dagrejs/dagre/blob/master/README.md | MIT, `@dagrejs/dagre` is the maintained package |
| S27 | Diagrams (mingrammer) | primary | https://diagrams.mingrammer.com/ | MIT, cloud icon node sets, Graphviz backend |
| S28 | Ilograph docs | primary (vendor) | https://www.ilograph.com/docs/ | Multi-perspective model, proprietary |
| S29 | yFiles SDK | primary (vendor) | https://www.yfiles.com/the-yfiles-sdk | Commercial, advanced/incremental layout |

## Source-quality notes

- Every version number and licence in `references.md` was read from the project's own repository, release
  page or official documentation — none is recalled.
- Two "scale limits" widely repeated in practice — Mermaid degrading past ~200 nodes and PlantUML class
  diagrams past ~100 classes — have **no documented source** and are marked Flagged wherever they appear.
  The two figures that *are* documented (`neato` ~100 nodes, vis-network "a few thousand") come from the
  tools' own docs.
- The Larkin & Simon (1987) citation supporting the mental-map argument was carried from the research
  summary rather than fetched, and is marked Flagged.
