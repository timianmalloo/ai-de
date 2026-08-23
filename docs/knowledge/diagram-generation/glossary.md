---
id: kb-diagrams-glossary
title: "Diagram Generation — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, layout, rendering, ubiquitous-language]
links:
  - { to: kb-diagram-generation, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Precise definitions for the diagram-generation vocabulary — Sugiyama method, orthogonal
  routing, layout stability, headless rendering, workspace — so the design uses one word per
  concept.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **C4 model** | Simon Brown's four-level hierarchy: L1 System Context, L2 Containers (deployable units), L3 Components, L4 Code. L4 is explicitly optional. Notation- and tooling-independent. *(Verified, [S11][S12])* |
| **Canvas** | Pixel-based browser rendering surface. No DOM per element, so fast; practical range around "a few thousand" nodes. *(Verified, [S24])* |
| **Diagrams-as-code** | Committing diagram *source* to version control and rendering it in CI, so diagrams diff and review like code. Our variant goes further: the source is generated, not authored. |
| **DSL** (here) | A text language whose syntax describes diagram elements and relationships, processed into a rendering. |
| **Force-directed layout** | Nodes repel, edges act as springs; iterative energy minimisation. ForceAtlas2, d3-force, Kamada-Kawai, Fruchterman-Reingold are variants. **Non-deterministic without a seed.** |
| **Headless rendering** | Running a browser engine without a display (Puppeteer/Chromium) to render JS-based diagram tools to static files in CI. *(Verified, [S3])* |
| **Kroki** | An HTTP gateway that proxies diagram source to the right backend and returns SVG/PNG, normalising a polyglot pipeline. *(Verified, [S19])* |
| **Layout stability / mental-map preservation** | The property that regenerating a diagram after a small topology change leaves nodes in roughly their previous positions. Without it, every regeneration forces the reader to re-learn the picture. The central unsolved problem for generated diagrams. *(Verified that the problem exists [S18][S10]; the framing Inferred)* |
| **Orthogonal routing** | Edges drawn only as horizontal and vertical segments with right-angle bends. Supported by ELK Layered. *(Verified, [S18])* |
| **Projection / view** | A rendered diagram derived from a model by a query. In Structurizr, everything in the `views {}` block. The opposite of an authored diagram. |
| **Security profile** (PlantUML) | `LEGACY` \| `INTERNET` \| `ALLOWLIST` \| `SANDBOX`, controlling filesystem and network access during rendering. **`LEGACY` is the default and grants full access.** *(Verified, [S7])* |
| **Sugiyama method** | Layer-based directed-graph layout (Sugiyama, Tagawa & Toda, 1981): assign layers → reorder within layers to minimise crossings → assign coordinates. ELK Layered implements it. *(Verified, [S18])* |
| **SVG** | DOM-based vector rendering. Fully styleable with CSS and accessible, but each element is a DOM node, so it degrades past roughly a thousand visible elements. *(Inferred)* |
| **Unified shape system** (Mermaid) | The 11.x rendering path that `classDiagram` (v2 renderer) and C4 elements now route through. *(Verified, [S1])* |
| **WebGL** | GPU-accelerated browser rendering. Best for tens of thousands of nodes; rendering is harder to customise. Sigma.js's substrate. *(Verified, [S22])* |
| **Workspace** (Structurizr) | The single file — DSL or JSON — containing both the `model` and the `views`. The canonical C4 artifact. *(Verified, [S14])* |
