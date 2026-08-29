---
id: "note-20260829-wpf-styling-knowledge-scope"
title: "WPF-styling knowledge request split into two new bases; diagram/UML/ERM cross-referenced, not duplicated"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: ""
tags: [decision-note, collectknowledge, scope, wpf, dashboards]
links:
  - { to: kb-wpf-modern-ui-styling, rel: relates-to }
  - { to: kb-operational-and-test-dashboards, rel: relates-to }
  - { to: knowledge-hub, rel: relates-to }
review-by: 2027-02-27
review-suggested: []
summary: >-
  The /collectknowledge run for "modern soft WPF styling + widget libraries + diagram/UML/ERM/test
  dashboards" produced two new bases (wpf-modern-ui-styling, operational-and-test-dashboards) and
  reconciled the already-covered diagram/UML/ERM asks by cross-reference rather than duplication.
---

# WPF-styling knowledge request split into two new bases; diagram/UML/ERM cross-referenced, not duplicated

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback weight.*

- **Kind:** decision
- **Confidence:** Verified *(the existing bases were read at grounding; the overlap is observed, not assumed)*
- **Made during:** `/collectknowledge` run, 2026-08-29 (prompt: modern/soft WPF styling, WPF UI control
  libraries, modern IDE/video-editor UX exemplars, diagramming, UML, ERM/ORM viz, and test/CI/CD/logs-metrics
  dashboards)

## The call
The user's request spanned seven buckets. Grounding found the repo already has a mature 10-base knowledge set,
and **three of the seven buckets were already covered**: diagramming (`diagram-generation`), UML/MDE/generative
(`uml-mde-and-4gl`), and data-model/ERM/ORM (`domain-modeling-and-erm`). Per the grounding discipline (extend,
don't duplicate) and the Simplifier gate (load-bearing, not a literature tour), the run produced **only the two
genuinely-new, load-bearing bases** — `wpf-modern-ui-styling` (buckets 1–3: styling, control libraries, IDE/UX
exemplars) and `operational-and-test-dashboards` (bucket 7: test/CI/metrics visualisation) — and **cross-
referenced** the already-covered buckets from both new indexes and the hub. The dashboards ask was split from
`microservice-interaction-visualization` because that base covers service *topology/traces*, a different data
shape and consumer than RED/USE *metrics*, *test* reporting, and *CI* pipelines.

## Alternatives dismissed
- **One combined "WPF client visual layer" base** — rejected; chrome styling and data-viz dashboards are
  distinct domains (different techniques, libraries, exemplars). Atomic bases per `knowledge-visualization.md` V1.
- **Re-covering diagram/UML/ERM for completeness** — rejected; duplication that would drift from the existing
  bases (grounding rule; Simplifier soft veto on scope sprawl).
- **Folding dashboards into `microservice-interaction-visualization`** — rejected; different data shape/consumer
  (metrics/tests/CI vs service topology/traces); cross-linked instead.

## Promotion rule
If the WPF styling direction becomes an architectural commitment (e.g. "adopt WPF UI library X" or "the shell
is dark-first islands"), promote that specific decision to an **ADR** and link it `supersedes` the relevant part
of `wpf-modern-ui-styling`. The knowledge bases stay as the evidence; the ADR records the decision.
