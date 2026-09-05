---
id: adr-0026-class-diagram-architecture
title: "ADR-0026 — Class diagram: an App-side type-hierarchy view from the existing graph, dependency-free; members & Mermaid deferred"
type: adr
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [architecture, class-diagram, uml, mermaid, graph, derived-view]
links:
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: adr-0015-canvas-hosting-and-overlay-strategy, rel: depends-on }
  - { to: adr-0025-code-viewer-renderer, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  The class-diagram surface (spec-uml-erm-surfaces US-U*) renders a UML type hierarchy — classes and
  interfaces as the nodes, `inherits` → generalization and `implements` → realization as the edges —
  built App-side from the EXISTING graph projection (C# already extracts these edges), so Phase 1 needs
  no new Core query and no vendored diagram library. Members are NOT extracted today, so the Phase-1
  view is member-less by construction; a full member-bearing, notation-valid Mermaid `classDiagram`
  render is deferred to Phase 2, gated on a Core `has_member` extractor enhancement — because a Mermaid
  classDiagram with empty compartments is not worth vendoring ~3 MB of mermaid.js for.
---

# ADR-0026 class-diagram-architecture — Class diagram architecture

**Status:** Accepted · **Date:** 2026-08-30 · **Deciders:** Design (Enterprise Architect + the-Simplifier peers), grounded in the repo's extractor output and ADR-0015/0019

## Context

`spec-uml-erm-surfaces` makes UML class diagrams a first-class, read-only, notation-correct surface
derived from the repo graph. Two facts from the codebase decide the shape:

1. **The data that exists.** `CSharpExtractor` emits `inherits` and `implements` assertions and
   `has_type` = `class`/`interface`/… — and the `GraphProjection` turns `inherits`/`implements` into
   ordinary `GraphEdge`s (only `has_type`/`declared_in`/attributes are excluded). So the graph the App
   already receives carries the **type hierarchy**. What it does **not** carry is **members**: no
   extractor emits `has_member`/`has_method`/`has_field`, so class *contents* are unavailable.
2. **The render cost.** A notation-valid UML class diagram (boxes with member compartments, hollow-
   triangle generalization, dashed realization) is what Mermaid's `classDiagram` produces — but Mermaid
   is **not** in the repo and is a ~3 MB vendored JS bundle (no CDN, per the pack). The existing graph
   canvas, by contrast, already renders nodes + typed edges in a dependency-free WebView2 (ADR-0015).

## Decision

**Phase 1 — a dependency-free type-hierarchy view, App-side from the existing graph.** The class
diagram is a **derived filter over the graph the App already has**: keep nodes whose kind is a class or
interface (C#, python-class, typescript-class/interface), keep `inherits` (→ **generalization**) and
`implements` (→ **realization**) edges, drop the rest, and render them **notation-styled** (solid
hollow-triangle for generalization, dashed hollow-triangle for realization) in the **existing
dependency-free canvas rendering** — **no new Core query, no vendored library, real data**. The model
is built by a pure, tested function (`ClassHierarchyModel`), so the projection logic is verifiable
headlessly. It is **honest about being member-less** (a "type hierarchy", the F-shape of the classes),
which is exactly what the data supports today.

**Phase 2 — members + notation-valid Mermaid, gated on Core.** A full class diagram with member
compartments needs (a) a Core **`has_member`** extractor enhancement (handed off, §4c) and (b) a
render that draws compartments — at which point vendoring **Mermaid** (`classDiagram`, bundled locally,
no CDN) becomes worthwhile. A Mermaid classDiagram with empty compartments is not, so the dependency is
**deferred until the data justifies it** (the-Simplifier; Solution-Selection Ladder — reuse before a
new dependency).

**A bounded Core `ClassModelAsync` query is a Phase-2 option, not a Phase-1 need.** The overview graph
is node-capped (1,500) and omits edges, so a *complete* class model for a large scope may eventually
want a dedicated bounded query (a sibling of `OverviewAsync`/`NodeContentAsync`) that returns exactly
the classes/interfaces + their generalization/realization/association edges + members for a chosen
context. This is recorded as a Phase-2 handoff; Phase 1 filters the graph already in hand.

## Options considered

1. **App-side filter over the existing graph, canvas-rendered — chosen for Phase 1.** No dependency, no
   Core change, real data, immediately demoable; testable model. *Cost:* member-less, and not full UML
   box notation until Phase 2.
2. **Bundle Mermaid now and render `classDiagram` — deferred to Phase 2.** Notation-valid UML, diagrams-
   as-code (the spec's codified form). *But* it vendors ~3 MB, and with no member data the compartments
   are empty — paying the dependency cost for a view the current data cannot fill. Deferred until Core
   ships `has_member`.
3. **A new Core `ClassModelAsync` query now — deferred.** Cleanest for a *complete* model, but it is
   Core work the App does not need to render a Phase-1 type hierarchy from the graph it already holds.

## Consequences

- **Positive:** a real, demoable class-hierarchy surface ships now with zero new dependencies and zero
  Core-gating; the member-bearing, notation-valid version is a clean Phase-2 substitution behind the
  same surface once the data and (then-justified) Mermaid dependency exist.
- **Negative / accepted:** Phase 1 shows generalization/realization but no members, and its notation is
  canvas-styled rather than full UML boxes. Stated honestly in the surface (a caption), not hidden.
- **Airspace (ADR-0015):** Phase 1 reuses the existing canvas rendering approach, so it inherits the
  windowed-WebView2 discipline already in place rather than adding a second diagram WebView2.

## Handoffs to Core (§4c)

- **`has_member` extraction** (methods/fields/properties per class) — the Phase-2 unlock for member
  compartments. Priority call.
- **(Phase-2, optional) a bounded `ClassModelAsync`** query for a *complete* per-context class model
  (classes/interfaces + generalization/realization/association + members), a sibling of `OverviewAsync`.

## Confidence

- **Verified:** `inherits`/`implements` are extracted (CSharpExtractor) and become `GraphEdge`s
  (GraphProjection); members are not extracted (no `has_member` anywhere in the extractors); Mermaid is
  not vendored. All read from the codebase.
- **Inferred:** that the Phase-1 canvas-styled type hierarchy is *useful enough* to ship before members
  exist — to be confirmed by the first rendered view over TheTerrace (the walking-skeleton demo).
