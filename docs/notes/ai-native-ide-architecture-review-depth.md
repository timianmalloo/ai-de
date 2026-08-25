---
id: note-ai-native-ide-architecture-review-depth
title: "Decision note — AI-native IDE architecture review depth"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "0"
tags: [architecture, review, execution-graph, phase-1]
links:
  - { to: architecture, rel: relates-to }
  - { to: plan-ai-native-ide-architecture, rel: relates-to }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  The architecture review exceeded its two-pass plan cap because independent hard-veto findings
  exposed missing storage, delivery, trust, privacy, and release contracts. The cap was treated as
  a defect signal; the contracts were completed rather than the gate being reduced.
---

# Decision note — AI-native IDE architecture review depth

**Decision:** Keep the completed architecture review contracts and record the plan-cap miss rather
than weakening the council or deferring the missing boundaries.

**Why:** The Data, Distributed Systems, Security, Privacy, Test, and Release lenses each found a
different load-bearing gap. Their convergence established that the original two-pass cap was too
small for a T2 architecture with persistent data, local IPC, agent tools, and privacy controls.

**Alternatives dismissed:** Stop after the cap (would leave hard-veto gaps); remove the contracts
(would make Phase 1 success-shaped but unsafe).

**Confidence:** Verified by the independent reviewer verdicts recorded in the architecture gate.
