---
id: kb-diagram-generation
title: "Diagram Generation & Rendering — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [diagrams-as-code, mermaid, d2, plantuml, structurizr, graph-layout, c4]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for generating every view from a graph query rather than authoring it: the DSL
  and renderer landscape with verified versions and licences, the layout-stability problem that
  decides whether regenerated diagrams are usable, and the case against generated diagrams.
---

# Diagram Generation & Rendering — domain knowledge

**Domain & problem:** In AI-DE no diagram is ever hand-authored. Every view is
`graph query → projection model → text DSL → renderer`. Required views: C4 context/container/component,
domain/class, ER, sequence-from-traces, and an interactive dependency/knowledge-graph explorer. Generated
DSL is committed to `docs/diagrams/` so architecture changes appear in PR diffs.

**Canonical framing:** The field calls this **diagrams-as-code** and frames it as a *documentation
freshness* problem — text in version control, rendered in CI. Our framing is one step further and is
genuinely different: the DSL itself is **generated from extracted facts**, not hand-written. That removes
the drift the canonical framing only mitigates, and it inherits a problem the canonical framing does not
have — **layout instability across regenerations**.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` is folded into `references.md` §"Versions, licences and constants" — this
domain's constants are version and licence facts that belong beside their source.)*

## Headline findings

1. **Structurizr is the only tool that is the C4 model's reference implementation, and its
   model/views separation is exactly our architecture.** `workspace { model {…} views {…} }` — the model is
   the single source of truth, views are projections of it, and `structurizr-export` emits PlantUML,
   C4-PlantUML, Mermaid, WebSequenceDiagrams, JSON, SVG/PNG and a static HTML site from one workspace file.
   Structurizr Lite is free and open source (single-user); Cloud and on-premises are paid. — *(Verified, [S13][S14][S15][S16][S17])*
2. **Mermaid's C4 syntax is a convenience layer, not the C4 model.** It will happily let a Component call a
   Person; Structurizr's DSL enforces the hierarchy. If C4 correctness matters, generate Structurizr DSL and
   *export* to Mermaid for rendering rather than generating Mermaid directly. — *(Verified, [S1][S17]; the enforcement contrast Inferred)*
3. **Licences differ in ways that matter for an embedded product.** Mermaid MIT; D2 **MPL-2.0** (file-level
   copyleft); PlantUML GPL for the full build (LGPL drops bundled GraphViz); Graphviz **EPL-1.0**;
   **elkjs EPL-2.0**; Cytoscape.js, Sigma.js, Graphology, G6, `@dagrejs/dagre` MIT; vis-network Apache-2.0;
   yFiles commercial. — *(Verified, [S1][S4][S6][S9][S18][S21]–[S26])*
4. **PlantUML's default security profile is `LEGACY`, which grants full local filesystem and URL access.**
   Running a PlantUML server or CI renderer without setting `INTERNET`, `ALLOWLIST` or `SANDBOX` is a
   documented arbitrary-file-read hole. This is the single most dangerous default in the domain. — *(Verified, [S7])*
5. **SVG byte-determinism is unresolved and it gates the "commit generated diagrams" plan.** `d2` and
   Graphviz `dot` are expected deterministic for identical input; Mermaid-CLI renders through
   Puppeteer/Chromium, where font metrics land in the SVG geometry, so output is *likely* to differ across
   Chrome versions. Untested. If generated SVG is committed, a non-deterministic renderer turns every CI run
   into a spurious diff. — *(Flagged — needs an empirical check; the rendering mechanism is Verified [S3])*
6. **Layout stability has no free solution.** ELK Layered implements Sugiyama, Tagawa & Toda (1981) and is
   deterministic for identical input — but any topology change re-runs the whole algorithm and can reorder
   everything. Graphviz `neato` offers a `pin` attribute; commercial tools offer incremental layout;
   nothing else does. Regenerating a diagram after a one-node change can destroy the reader's mental map. — *(Verified, [S18][S10])*
7. **Renderer choice is decided by graph size and by SVG-versus-GPU, and the numbers are documented.**
   vis-network self-documents "smooth … up to a few thousand nodes and edges" on Canvas; Sigma.js uses WebGL
   and its own FAQ says d3 is the better fit "if you have small graphs (like a few hundred nodes)"; SVG
   degrades past roughly a thousand visible elements because each is a DOM node; Graphviz's own docs put
   `neato` at "about 100 nodes". — *(Verified, [S24][S22][S10]; the SVG figure Inferred)*
8. **Kroki collapses the polyglot rendering problem into one HTTP endpoint** for 20+ DSLs (Mermaid,
   PlantUML, Graphviz, Structurizr, C4-PlantUML, Excalidraw, Vega, and more), self-hostable via Docker — but
   it **inherits the security posture of its backends**, including PlantUML's `LEGACY` default. — *(Verified, [S19][S20])*
9. **No static DSL pipeline supports progressive disclosure.** "Show only nodes within N hops of X" is
   solved in the interactive renderers (Cytoscape.js, Sigma.js, vis-network) and absent from every
   DSL→SVG pipeline reviewed. A large graph therefore needs the interactive tier; it cannot be served by
   generating a bigger Mermaid file. — *(Verified by absence across [S1]–[S20]; the conclusion Inferred)*
10. **Accessibility support is inconsistent across the pipeline.** Mermaid has `accTitle`/`accDescr` →
    SVG `<title>`/`<desc>`. PlantUML and D2 have no documented equivalent. Graphviz emits IDs but no
    semantic alt text. — *(Verified, [S3]; the absence for others Flagged)*

## Confidence summary

Verified: all versions, licences, layout-algorithm attributions, security profiles, and the two documented
scale figures (vis-network "few thousand", Graphviz neato "about 100"). Inferred: the SVG ~1000-element
figure, the Mermaid-versus-Structurizr enforcement contrast, and the progressive-disclosure conclusion.
Flagged: Mermaid's exact published npm version (monorepo root `package.json` says 10.2.4 while
`packages/mermaid/CHANGELOG.md` says 11.17.0 — read the package, not the root); SVG byte-determinism;
"Mermaid degrades past ~200 nodes" and "PlantUML class diagrams past ~100 classes" (both practitioner
folklore, not documented); TALA's licence terms (not fetched); whether `elkjs` genuinely runs in a Web
Worker (claimed, not confirmed from the README here).

**Load-bearing Flagged claims:** SVG byte-determinism (gates committing rendered output) and the Mermaid
version (gates a dependency pin). Both are cheap to settle empirically and should be settled before either
decision is made.

## Design implications

- **Generate Structurizr DSL as the canonical architecture artifact and export from it.** It is the only
  tool that enforces the C4 model, and its exporters cover every other renderer we need. One generated
  workspace file, many rendered views.
- **Commit the DSL, not the rendered SVG** — at least until byte-determinism is measured. DSL diffs are the
  reviewable thing anyway ("this change added a dependency from Catalog→Ordering" reads perfectly in a text
  diff); rendered SVG diffs are noise even when correct.
- **Pick renderers by tier, not by preference.** Mermaid for the ≤100-node embedded views (class, ER,
  sequence) because it is MIT, needs no server and renders in the pane we already have; Cytoscape.js + ELK
  for the interactive explorer; Graphviz `dot` where determinism matters most.
- **Budget for layout stability from the start.** Pin node positions by stable node ID across
  regenerations, or accept that every regeneration re-teaches the diagram. This is a first-class design
  problem, not polish.
- **If PlantUML is used anywhere, set the security profile explicitly.** `SANDBOX` in CI, never the default.
- **Do not attempt L4/code-level diagrams.** Simon Brown says the code level is "often not worth the
  effort", and complete generated diagrams at component level are precisely where automation over-generates.
- **Treat accessibility as a renderer-selection criterion,** since only Mermaid has a documented mechanism.

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). Versions and licences in
`references.md` are the ones to quote rather than recall — this ecosystem moves monthly. Refresh when a
renderer major version lands; re-run `/collectknowledge` and bump the date.
