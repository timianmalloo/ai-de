---
id: proof-watcher-weave-score
title: "Proof Pack - Loomkeeper Deterministic Weave (slice 5)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, proof-pack, weave, scoring, floors, coverage, phase-2]
links:
  - { to: design-watcher-weave-score, rel: tested-by }
  - { to: design-watcher-work-episode, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper deterministic Weave meets its design: a closed Work Episode is scored on
  the four deterministic dimensions (observed weight 70) with the two advisory dimensions excluded (not
  faked); a hard floor (correctness / security / privacy / data integrity / evaluator integrity) trips a
  Blocked verdict and suppresses the numeric headline; a missing goal / done-condition / verification
  path or an open episode is Not Scored; the Partial headline uses the observed-weight denominator and
  never rescales to 0-100; and Evidence Coverage is separate from points - proven by 27 tests incl. an
  E11 composition, with the no-rescale oracle mutation-verified. Full suite 834/0.
---

# Proof Pack: Loomkeeper Deterministic Weave (slice 5)

- **Component:** `src/AiDe.Core/Watcher/WeaveScore.cs` (`ScoreDimension`, `FloorDomain`, `AssessmentPosture`, `WeaveVerdict`, `ScoreSchema` (`weave/1`), `DimensionAssessment`, `EvidenceCoverage`, `DeterministicEpisodeSignals`, `Scorecard`, `WeaveScorer`).
- **Tests:** `tests/AiDe.Core.Tests/Watcher/WeaveScorerTests.cs` — 27 tests, **27/27**; full `AiDe.Core.Tests` suite **834/0**; build clean (0 warnings, `TreatWarningsAsErrors`).
- **Pure engine, model-free** — the advisory dimensions are declared-and-excluded (ADR-0019), never stubbed with numbers; scoring the advisory dimensions is slice 7.

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| A clean closed episode is Partial with observed weight 70 | `Clean_ClosedEpisode_IsPartial_WithObservedWeight70` | 4 deterministic dims | headline "Partial: 70 / 70 observed"; per-dim points 30/15/15/10 | Seen green | Verified | matches spec "58 / 70 observed" shape |
| The advisory dimensions are excluded, not faked 0 | `AdvisoryDimensions_AreExcluded_NotFakedZero` | schema posture Advisory | Evidence/Economy posture Advisory, EarnedPoints null | Seen green | Verified | they enter points in slice 7 after calibration |
| The Partial headline uses observed weight, never rescaled to 0-100 (rule 2) | `Partial_HeadlineDenominatorIsObservedWeight_NotRescaledTo100`, `Clean_...` | headline denominator | "/ 70 observed", not "/ 100" | **Yes** — swapping the denominator to total weight reds 6 tests | Verified | — |
| Missing goal / done-condition / verification path / open episode → Not Scored (rule 5) | `NoGoal_IsNotScored`, `NoDoneCondition_IsNotScored`, `NoVerificationPath_IsNotScored`, `OpenEpisode_IsNotScored` | `NotScoredReason` | verdict NotScored + reason | Seen green | Verified | the done_when made measurable |
| Any unresolved floor blocker trips Blocked and suppresses the headline (rules 6-7) | `AnUnresolvedFloorBlocker_TripsBlocked_AndSuppressesTheHeadline` (Theory ×5) | `TrippedFloors` | each of the 5 domains → Blocked, floor listed, no numeric | Seen green | Verified | — |
| Acceptance-not-met / regression / verification-not-executed trip the Correctness floor | `AcceptanceNotMet_...`, `Regression_...`, `RequiredVerificationNotExecuted_...` | correctness rule | Blocked via Correctness | Seen green | Verified | — |
| Unknown (null) acceptance does NOT trip the floor; Outcome is Not Recorded | `UnknownAcceptance_DoesNotTripCorrectness_ButOutcomeIsNotRecorded` | `== false` vs null | Partial, no floor, Outcome NotRecorded, "/ 40 observed" | Seen green | Verified | unknown ≠ failed |
| Outcome steps down when not completed | `Outcome_NotCompleted_StepsDownTheRubric` | Outcome rubric | Abandoned → rubric 2 | Seen green | Verified | — |
| Focus penalises work-after-done (drift) and premature completion — done_when measured | `Focus_WorkAfterDoneCondition_ReducesTheRubric_TheDriftPenalty`, `Focus_PrematureCompletion_...` | Focus rubric | each → rubric 2 (the PACK-O faces) | Seen green | Verified | — |
| Guidance / Coordination are proportional, or Not Recorded when none required | `Guidance_IsProportional...`, `Guidance_NoTriggersRequired_IsNotRecorded`, `Coordination_NoSignalsRequired_IsNotRecorded` | `Proportional` | 2/4 → rubric 2; required 0 → NotRecorded ("/ 55 observed") | Seen green | Verified | — |
| Evidence Coverage is Not Recorded when uncalibrated; present when calibrated; separate from points (rules 3-4) | `Coverage_Uncalibrated_IsNotRecorded`, `Coverage_Calibrated_IsObservedOverRequired_SeparateFromPoints` | coverage branch | null vs (7,10); points unchanged | Seen green | Verified | — |
| The score schema version is pinned (A6) | `SchemaVersion_IsPinned`, `Schema_TotalWeightIs100_WithSeventyDeterministic` | `ScoreSchema.Weave1` | "weave/1"; 100 total, 70 deterministic | Seen green | Verified | a bump is a gated change |
| End to end: a real closed episode from the service scores (E11) | `Composition_ScoresARealClosedEpisode_FromTheService` | real `WorkEpisodeService` → scorer | "ep-real", Partial, "70 / 70 observed" | Seen green | Verified | — |

**Boundary set covered:** clean, advisory-excluded, no-rescale, four Not-Scored gates, five floor domains, three correctness-trip conditions, unknown-acceptance, outcome-not-completed, focus-drift, focus-premature, guidance proportional / not-recorded, coordination not-recorded, coverage uncalibrated / calibrated, schema pin, composition.

**Testing Strategy triggers applied:** **D1** (the full scoring truth table), **A6** (the `weave/1` schema version is pinned and asserted — a change is a gated contract change), and an **E11** composition through the real `WorkEpisodeService`. No triggered directive dropped.

**Mutation sense:** the no-rescale oracle (spec rule 2) is proven behaviorally — swapping the Partial denominator from the observed weight to the schema's total weight reds 6 tests — then reverted.

**Security note (STRIDE, carried from design):** the engine is pure over already-collected deterministic signals and adds no trust boundary or egress. **Evaluator integrity is a first-class hard floor** — a forged/tampered-evidence blocker trips Blocked, and (by construction) an advisory judgment can never raise a deterministic failed dimension and never enters points before its calibration gates (slice 7). No personal data: the signals are counts and booleans about a task, not a person.

**Boundary clarified (what this slice does NOT do):** the two **advisory** dimensions (Evidence discipline, Solution economy = weight 30) are declared and **excluded** from points — the honest Partial denominator is the deterministic 70. The advisory grader, its calibration + QWK gates, the leaderboard, and standing are **slice 7** (ADR-0019).

**Residual:**
- **Signal collection** — populating `DeterministicEpisodeSignals` from the observation store, coordination log, and verification/CI ingest — is the connective follow-on; slice 5 ships the pure engine that both a real collector and the tests drive.
- **Scorecard persistence** (append-only fact; a dispute appends a superseding evaluation, rule 12) — a store follow-on.
- **The advisory grader + calibration + QWK + leaderboard + standing** — slice 7.
