---
id: note-conductor-r4-core-phase1-scope
title: "Decision note — R4-core scope for Conductor Phase 1"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, coordination-modes, scoring, phasing]
links:
  - { to: spec-conductor, rel: refines }
  - { to: architecture, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Spec R4 requires a run containing both a governed claude-code lane and an observed
  grok-build lane, but grok-build parity is R11, which the spec assigns to Phase 4.
  The Owner ruled what "R4(core)" means in Phase 1 so no Phase-4 work is pulled forward
  and no R4 criterion is silently dropped.
---

# Decision note — R4-core scope for Conductor Phase 1

**Ruled by:** Owner agent (delegated CT20 authority), 2026-09-09. Confidence: **Verified** —
the Owner opened the spec and the cited source itself.

## Question

Spec §12 assigns Phase 1 the requirements `R1 R2 R4(core)`, with exit evidence naming only
claude-code. But R4's first acceptance bullet requires a run with a **claude-code governed lane
and a grok-build observed lane**. The readiness profile and terminal binding for grok-build are
R11's first bullet, which §12 assigns to **Phase 4**. Building it now pulls Phase 4 forward;
ignoring the bullet drops an acceptance criterion silently.

## Ruling

**R4-core in Phase 1 means exactly three things, each proven by test:**

1. `GovernedSessionSource` lands episodes in the same `IWatcherObservationStore` by the live
   path `InjectedContractIngest` already uses, and they are scored by the **unchanged**
   `ScoringService`.
2. **Mode travels as a cohort attribute** and no leaderboard cell splits by it.
3. A governed lane's **out-of-lease edit raises a seam**, and the run **cannot close with an
   open seam** (outcome forced `Blocked`).

Plus: the existing `AuditLogEpisodeSource` import path is **demonstrated still working in the
same test run**, so the "two modes coexist" claim is evidenced rather than asserted.

The **live grok-build observed lane is R11 and stays in Phase 4.**

## Because

- §12 row 1 assigns `R4(core)` with exit evidence naming only claude-code (spec line 447).
- R4's first bullet (line 376) requires a grok-build observed lane, whose readiness/terminal
  binding is R11's first bullet (line 431), assigned to Phase 4 (line 450).
- R4's second and third bullets carry **no Phase-4 dependency** and are therefore "the core".
- The *coexist* half of bullet one is evidenceable without a live terminal, by exercising the
  import path in the same run.

## Conditions on this ruling

- The Phase-1 close must show the audit-import path and the governed path producing episodes
  **in one store in one test**, not in two separate tests.
- "Scored by the unchanged `ScoringService`" is shown **by diff** (no semantic change in
  `ScoringService`/`WeaveScorer`), not asserted.

## Scope effect

Admits R4 bullets 2 and 3 and the coexist-by-test proof. Defers R4 bullet 1's live grok-build
lane to Phase 4 (R11). Freezes the programme's Not-in-scope list as stated in the goal block.
