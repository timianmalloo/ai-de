---
id: adr-0001-derived-evidence-views
title: "ADR-0001 — Use derived evidence views, not editable models"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, provenance, diagrams, source-of-truth]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: knowledge-hub, rel: depends-on }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
  - { by: spec-ai-native-ide, on: 2026-08-26, reason: "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice" }
summary: >-
  AI-DE stores attributable evidence and generates architecture/model/flow views from it. Users
  may save view preferences but cannot edit a rendered view into source truth.
---

# ADR-0001: Use derived evidence views, not editable models

- **Status:** Proposed
- **Date:** 2026-08-25
- **Deciders:** Product owner, Architecture council

## Context

The seed proposed that the graph is the product. The knowledge hub qualifies that statement:
code-derived views avoid the historical model-drift failure mode only while code/infrastructure and
attributable observations remain authoritative.

## Decision

AI-DE will persist source/runtime evidence assertions and generate all architecture, class, ER,
sequence, activity, dependency, and knowledge views as projections. It will persist query, filter,
and layout preferences only; it will not persist user edits as artifact facts.

## Alternatives considered

- **Editable diagram/model source:** rejected because it creates a second source of truth and
  requires synchronization with code.
- **Hand-authored DSL as the source:** rejected because it reintroduces drift; generated DSL is
  reviewable output, not authority.

## Consequences

- **Positive:** provenance and confidence remain inspectable; views rebuild after extraction.
- **Negative / accepted trade-off:** users correct extraction by fixing declarations/configuration
  or by adding an attributable annotation, not by dragging a diagram edge.
- **Follow-up:** Phase 1 defines the projection/query preference boundary.

## Evidence

`docs/knowledge/index.md` finding 4 and cross-cutting “never make a derived view editable”
[Verified].
