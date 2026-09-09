---
id: proof-conductor-agent-plane
title: "Proof Pack — Conductor agent plane, Phase 1 (N0–N7)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, acp, proof-pack, phase-1, exit-evidence]
links:
  - { to: plan-conductor-programme, rel: tested-by }
  - { to: spec-conductor, rel: tested-by }
  - { to: note-conductor-n7-refactor-oracle, rel: depends-on }
review-by: 2027-03-09
review-suggested: []
summary: >-
  Evidence for Phase 1 of the Conductor spec: the run-event envelope over a real captured ACP
  frame corpus, the engine catalog's three named refusals, the plane services, the bidirectional
  ACP client verified live, one cell with two mode cohorts, leases and seams, and the headless
  governed run through the App-layer composition root. Written and committed BEFORE the exit
  episode closes, because ClosedEpisodeScoring derives HasProofPack from a declared artifact that
  must already exist on disk.
---

# Proof Pack: Conductor agent plane, Phase 1

> **Committed before the run, deliberately.** `ClosedEpisodeScoring.EvidenceFor` asks
> `ProofPackVerifier` whether each declared artifact is a real file; an episode that declares none
> derives no verification path and scores **Not Scored**. So this file exists before the exit
> episode closes and is completed with the measured values afterwards. The rows below marked
> **run pending** are the ones the exit run fills; nothing else in this file changes after it.

- **Component:** `src/AiDe.Core/AgentPlane/` (13 types) + `src/AiDe.App/Conductor/`
  (`GovernedRunHost`, `ConductorEntry`, `GovernedRunRequest`).
- **Tests:** run pending.
- **Spike:** `spikes/acp-subscription-lane/` — the real captured ACP frame corpus (88 frames,
  `PROVENANCE.md` records adapter and CLI versions, command lines, OS and the redaction rule).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| run pending | run pending | run pending | run pending | run pending | run pending | run pending |

**Residual:** run pending.
