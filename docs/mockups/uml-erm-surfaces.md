---
id: mockup-uml-erm-surfaces
title: "UML & ERM Surfaces — derived views (mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [mockup, uml, erm, c4, derived-views, read-only]
links:
  - { to: spec-uml-erm-surfaces, rel: documents }
review-by: 2027-02-27
review-suggested: []
summary: >-
  Self-contained mockup of the first-class UML & ERM surfaces — a model catalog master-detail with
  a crow's-foot ER diagram, a UML class diagram (composition/aggregation/dependency), and a C4
  context view, all read-only with a permanent derived-view banner, inferred relationships dashed,
  and generation-error / too-large-curated / attempt-edit states. The .html is data; this .md is its node.
---

# UML & ERM Surfaces — mockup

`uml-erm-surfaces.html` — the first-class modelling surfaces (spec-uml-erm-surfaces). Renders three
notation-valid derived views over the Ledger context: an **ER diagram** (crow's-foot cardinality, keys, an
explicit **associative entity** `Mandate` for the Agent–Account M:N — no silent M:N), a **UML class**
diagram (filled-diamond composition JournalEntry◆Money, open-diamond aggregation Ledger◇JournalEntry, dashed
dependency to Account), and a **C4 context** view. Every view carries a permanent **🔒 read-only banner**;
**inferred** relationships (EF/DI) are dashed and labelled, never shown as extracted facts. Harness switches
theme, motion, view state (rendered / generating / **generation-error** / **too-large-curated**), and an
**attempt-edit** action that surfaces the "derived view — edit the source" block.

**Key correctness demonstrations:** notation validity (UML relationship kinds, ER crow's-foot); the
**derived-view rule** (edits are refused and directed to source — `MODEL-VIEW-EDITABLE` impossible); C4
hierarchy; provenance carried; layout stability (positions are fixed, not re-laid-out).
