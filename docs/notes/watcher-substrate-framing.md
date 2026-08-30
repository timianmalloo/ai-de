---
id: note-watcher-substrate-framing
title: "Loomkeeper framing, score authority, and Observatory archetype"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "discovery"
tags: [decision-note, loomkeeper, scoring, ui-archetype, privacy]
links:
  - { to: spec-agentic-watcher-substrate, rel: relates-to }
  - { to: kb-agentic-session-observability, rel: depends-on }
  - { to: mockup-watcher-observatory, rel: relates-to }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Records the decision to name the watcher Loomkeeper, keep deterministic facts authoritative over
  advisory model judgments, human-gate Daydream promotion, and use a G6 evidence-led Observatory
  inside the existing AI-DE workbench.
---

# Loomkeeper framing, score authority, and Observatory archetype

## Decision

- Name the agentic watcher **Loomkeeper** and its user surface **The Observatory**.
- Model the product as a **Continuous Sentinel** that observes and proposes; it does not execute
  repository side effects or become repository truth.
- Keep deterministic outcome/security/privacy/integrity evidence authoritative. Local model judgments
  remain versioned, advisory, disputable, and excluded when unavailable or unqualified.
- Present a six-dimension **Weave Scorecard** with evidence coverage and hard floors; never show a
  complete `/100` score when a dimension is Not Recorded.
- Use the **G6 Multi-Panel Data Terminal** archetype, specialized to the existing AI-DE workbench,
  because the dominant job is parallel monitoring with causal drill-down.
- Keep v1 local-only and non-personnel. Daydream promotion requires disconfirmation and a human gate
  and remains retractable.

## Alternatives dismissed

- **Read-only dashboard:** too small; drops the requested coordination, scoring, feedback, and
  learning jobs.
- **Autonomous supervisor/judge:** too powerful; concentrates Goodhart, injection, privacy, and
  self-certification risk.
- **Telemetry bento dashboard:** too shallow; equal tiles hide repository/session identity and
  evidence paths.
- **Conversational watcher:** serializes a parallel-reading job and makes the agent's prose the
  navigation.

## Confidence

**Verified:** evidence for session traces, multi-dimensional failure modes, reward-hacking risk,
context collapse, memory poisoning, and current repo coordination surfaces.  
**Inferred:** the six dimension weights and the G6 specialization.  
**Validation condition:** review the mockup with the product owner and calibrate the score dimensions
on a labeled corpus before implementation.

