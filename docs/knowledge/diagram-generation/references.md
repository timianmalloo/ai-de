---
id: kb-diagrams-references
title: "Diagram Generation — references, versions, licences and constants"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, versions, licences, layout-algorithms]
links:
  - { to: kb-diagram-generation, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The version numbers, licence terms, documented scale figures and security defaults for every
  diagram tool in scope, each read from a primary source and dated — the facts to quote rather
  than recall.
---

# Reference information

## Specifications and canonical works

- **The C4 model** (Simon Brown, c4model.com) — Context, Container, Component, Code, plus supporting
  System Landscape, Dynamic and Deployment diagrams. Explicitly **notation-independent and
  tooling-independent**; the code-level (L4) diagram is stated to be "often not worth the effort" for most
  teams; the model can be expressed in UML with packages/components/stereotypes at some cost to descriptive
  text. *(Verified, [S11][S12])*
- **Sugiyama, Tagawa & Toda (1981)** — the layered graph-drawing method, cited by Eclipse ELK as the basis
  of its Layered algorithm. *(Verified, [S18])*
- **Larkin & Simon (1987), "Why a Diagram is (Sometimes) Worth Ten Thousand Words"** — the standard citation
  for spatial arrangement carrying meaning; the basis of the mental-map argument in `open-questions.md`.
  *(Flagged — cited from the research summary, not fetched directly here)*

## Versions, licences and constants

| Item | Value | Source | Confidence |
|---|---|---|---|
| Mermaid — CHANGELOG version | **11.17.0** | `packages/mermaid/CHANGELOG.md` | Verified [S1] |
| Mermaid — monorepo root `package.json` | 10.2.4 (stale root artifact — do not pin from this) | repo root | Verified [S2] |
| Mermaid — licence | MIT | repo | Verified [S1] |
| Mermaid — security levels | `strict` (default) · `loose` · `antiscript` · `sandbox` | docs | Verified [S1] |
| Mermaid — accessibility | `accTitle` / `accDescr` → SVG `<title>` / `<desc>` | mermaid-cli README | Verified [S3] |
| Mermaid — headless | `mmdc` via Puppeteer/Chromium; Docker `minlag/mermaid-cli` | mermaid-cli README | Verified [S3] |
| D2 — version | **v0.7.1** | GitHub releases | Verified [S4] |
| D2 — licence | **MPL-2.0** (file-level copyleft) | LICENSE.txt | Verified [S4] |
| D2 — layout engines | Dagro (default, bundled) · elk-go (bundled) · TALA (separate proprietary binary) | README | Verified [S4] |
| TALA — licence | **not established** | — | **Flagged** |
| PlantUML — version | **v1.2026.6** (June 2026) | plantuml.com/download | Verified [S6] |
| PlantUML — licences | GPL (full) · LGPL (no bundled GraphViz) · MIT · Apache variants | plantuml.com/download | Verified [S6] |
| PlantUML — Java | Java 11+ recommended (Java 8 snapshot exists); always required | plantuml.com/starting | Verified [S8] |
| PlantUML — **security default** | **`LEGACY` — full local filesystem + URL access** | plantuml.com/security | Verified [S7] |
| PlantUML — security profiles | `LEGACY` · `INTERNET` · `ALLOWLIST` · `SANDBOX` (env var or JVM `-D`), since v1.2020.11 | plantuml.com/security | Verified [S7] |
| Graphviz — licence | EPL-1.0 | graphviz.org | Verified [S9] |
| Graphviz — `neato` documented scale | **"about 100 nodes"** | graphviz.org/docs/layouts/neato/ | Verified [S10] |
| Graphviz — layout stability aid | `pin` attribute (`neato`) | same | Verified [S10] |
| Graphviz — determinism | `dot` deterministic for identical input; `neato`/`fdp` vary with `start` seed | graphviz docs | Verified [S9][S10] |
| Structurizr Lite | free, open source, single-user, no collaboration | docs.structurizr.com/lite | Verified [S15] |
| Structurizr on-premises | paid WAR, Jakarta EE / Spring 6, Tomcat 10+ | docs.structurizr.com/onpremises | Verified [S16] |
| Structurizr export targets | plantuml · plantuml/structurizr · plantuml/c4plantuml · mermaid · websequencediagrams · static HTML · PNG · SVG · JSON · theme; custom via `WorkspaceExporter` JAR | docs.structurizr.com/export | Verified [S17] |
| Kroki — licence / deployment | Apache-2.0 server, self-hostable Docker, SaaS at kroki.io | docs.kroki.io | Verified [S19][S20] |
| Kroki — backend count | 20+ DSLs (full list in docs) | docs.kroki.io | Verified [S20] |
| elkjs — licence | **EPL-2.0** (copyleft) | Eclipse ELK | Verified [S18] |
| ELK Layered — basis | Sugiyama, Tagawa & Toda (1981) | eclipse.dev/elk reference | Verified [S18] |
| `@dagrejs/dagre` | MIT; the maintained package (original `dagre` stale) | dagre README | Verified [S26] |
| Cytoscape.js | MIT (core + first-party extensions) | js.cytoscape.org | Verified [S21] |
| Sigma.js | MIT; **WebGL**; d3 preferred "for a few hundred nodes" | sigmajs.org | Verified [S22] |
| Graphology | MIT | graphology.github.io | Verified [S23] |
| vis-network | Apache-2.0; **Canvas only**; "smooth … up to a few thousand nodes and edges" | visjs.github.io | Verified [S24] |
| G6 (AntV) | MIT; Canvas + WebGL in v5 | github.com/antvis/G6 | Verified [S25] |
| yFiles | commercial/proprietary | yworks.com | Verified [S29] |
| SVG practical ceiling | ~1000 visible elements (each is a DOM node) | — | **Inferred** |
| Mermaid flowchart degradation ≥ ~200 nodes | community reports only | — | **Flagged** (folklore) |
| PlantUML class diagram ≥ ~100 classes | practitioner folklore | — | **Flagged** |
| SVG byte-determinism across renderers | unknown; `d2`/`dot` expected deterministic, Mermaid-CLI likely not | — | **Flagged** — needs an empirical test |
| `elkjs` in a Web Worker | claimed; not confirmed from the README | — | **Flagged** |

## Licence notes that affect embedding

- **MPL-2.0** (D2): file-level copyleft. A proprietary product may link it; modifications *to D2's own
  files* must be published under MPL-2.0.
- **EPL-1.0 / EPL-2.0** (Graphviz, elkjs): copyleft. Check before embedding in a distributed product.
- **GPL** (PlantUML full build): the LGPL build exists precisely to avoid this, at the cost of the bundled
  GraphViz.
- **MIT / Apache-2.0** (Mermaid, Cytoscape.js, Sigma.js, Graphology, G6, `@dagrejs/dagre`, vis-network,
  Kroki server, mingrammer/diagrams): unproblematic for embedding.

*(All Verified from the sources in the table; the *consequences* for our embedding are Inferred and are not
legal advice.)*
