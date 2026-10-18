---
id: diagram-layers
title: "Layered architecture — capability tiers"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [diagram, layers, loa, tiers, architecture]
links:
  - { to: forensic-review-rev102, rel: relates-to }
  - { to: architecture, rel: documents }
  - { to: diagram-component, rel: relates-to }
review-by: 2027-09-02
summary: >-
  Where each AI-DE capability sits in the LOA tier ladder, and the single rule that governs the
  boundary: a model may explain a bounded, already-selected result, and may never select it.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# Recovered capability layers and authority

The diagram separates observed deterministic mechanics from optional model work. **Verified source
composition** at `9f13ca7a`; future factory analysis is a proposal, not a currently shipped tier.

```mermaid
flowchart TB
 UI[WPF shell: selection, conversation, diagrams, evidence]
 Query[Deterministic bounded queries and rendering models]
 Authority[Daemon/Core: extraction, snapshots, receipts and immutable store]
 Agent[External ACP/terminal harness and optional model compilation]
 Review[Proposed future agent assessments]
 UI --> Query --> Authority
 UI --> Agent
 Review -. proposed evidence only .-> UI
```

| Layer | Current authority / limitation |
|---|---|
| Local fact authority | Parsed assertions, provenance, scope generations and source identity; FR-001 freshness defect remains |
| Projections / views | Deterministic derived selection with explicit omissions; FR-002 byte enforcement gap remains |
| Agent execution | Operator-selected provider/tool lane; bounded permissions must be qualified for actual offered capabilities |
| Compilation | Deterministic precompile plus optional gated agent call; envelopes retain provenance/history |
| Watcher / proof | Observation, pointers and scoring of bounded signals; not automatic semantic correctness |
| Proposed factory assurance | Agent claims remain assessments; policy and human decisions are separate retained records |

No model-written summary becomes code truth or approval by presentation alone. Do not claim the
entire product is incapable of source changes: agents in execution lanes can edit, while diagram and
source viewers are read-only. Local storage and external inference are independent trust decisions.
