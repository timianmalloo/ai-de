---
id: ai-forward-pack-adoption
title: "AI-Forward Pack Adoption Plan"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [adoption, roadmap]
links:
  - { to: architecture, rel: depends-on }
  - { to: proof-adoption, rel: tested-by }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Phased plan for turning the recovered AI-DE baseline into complete, evidence-linked product, design, proof, and documentation artifacts without fabricating history.
---

# AI-Forward Pack Adoption Plan

## Baseline recovered

The current graph starts from source-backed evidence:

- one .NET 10 WPF runtime container;
- a minimal view-first MVVM seam;
- one view-model unit test;
- active build and docs-health workflows;
- project onboarding, licensing, provenance, audit history, and four governed
  terms.

No historical spec, component design, ADR, proof pack, API reference, rendered
surface proof, packaging plan, threat model, or privacy review was recovered.

## Gap table

| Area | Current evidence | Missing artifact or proof | Priority |
|---|---|---|---|
| Knowledge graph health | Architecture, glossary, audit hub, document map, adoption note, and this plan | Derived index, baseline snapshot, clean validation | Complete in this adoption |
| Current WPF public surface | Source, XAML, one view-model test | API reference, composition/render sequence proof, class/layer/component diagrams, runnable examples | **Phase 1** |
| Product intent | Starter copy only; no recorded actor or business capability | Functional/UX/UI specification, core scenario, non-goals, measurable acceptance criteria | **Phase 2** |
| First vertical feature | No feature design exists | Conceptual model if data-bearing, component contracts, UI states, telemetry questions, failure modes, test plan | **Phase 3** |
| Correctness evidence | Release build and one passing unit test | XAML startup/binding proof, rendered-surface proof, cross-surface checks, Proof Pack | **Phase 4** |
| Release and operations | Build CI only | Packaging, signing, distribution, rollback, runtime observability, support floor enforcement | Later, when the product path is known |
| Security and privacy | No runtime trust/data boundary exists | Threat model and privacy review when a feature introduces data, identity, external tools, or model egress | Triggered by the first applicable feature |

## Phased adoption

| Phase | Gap addressed | Skills | Deliverable and proof | Working graph increment |
|---|---|---|---|---|
| **1 - current shell** | Public API comments/reference, real composition/render proof, baseline threat/privacy records, and four required diagram families | `/document`; use `/design` or `/implement` only for a real contradiction or missing code contract | Document current signatures honestly; record undocumented members as gaps rather than inventing prose; exercise WPF startup/binding; record that no current runtime trust or personal-data boundary exists | API/documentation, threat-model, and privacy-review nodes linked to `architecture` |
| **2 - product intent** | No target user, job, core scenario, non-goals, or measurable requirements | `/collectknowledge` if the chosen domain is unfamiliar, then `/specify` | One Functional/UX/UI specification (or explicit N/A layers) cleared by its owning lenses and Test Architect | Accepted spec linked to `architecture` and `glossary` |
| **3 - first vertical feature design** | No component/data/AI/UI design for a product capability | `/define-architecture` if the top-level shape changes; otherwise `/design` | Sourced or spiked contracts, conceptual model when data-bearing, failure modes, telemetry questions, UI states, and tests; relevant vetoes clear | Design, ADR/notes, and triggered threat/privacy nodes linked to the spec |
| **4 - implementation proof** | No end-to-end feature or Proof Pack | `/implement`, then `/document` | Red-green-refactor evidence, real composition/render proof, cross-surface consistency, telemetry readback, and triggered test union | Source/API/proof nodes close `spec -> architecture -> design -> implementation -> proof` |

Persistence, runtime AI, identity, model/third-party egress, packaging, signing,
distribution, rollback, and operational telemetry remain triggered work rather
than speculative bootstrap layers.

## Adoption gate

Pending independent Documentation Steward, Test Architect, and Simplifier
review.

## Status

| | |
|---|---|
| **Completed** | Current evidence recovered into a connected graph. |
| **Remaining** | Phase 1 documentation bundle, then product specification and the first vertical feature. |
| **Best next action** | Run `/document` over the adopted baseline. |
