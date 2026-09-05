---
id: proof-watcher-advisory-grader
title: "Proof Pack - Loomkeeper Advisory Grader, Calibration, Leaderboard & Standing (slice 7)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, proof-pack, advisory, calibration, qwk, leaderboard, standing, anti-goodhart, phase-4]
links:
  - { to: design-watcher-advisory-grader, rel: tested-by }
  - { to: design-watcher-weave-score, rel: depends-on }
  - { to: adr-0019-advisory-evaluator-calibration, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper advisory grader meets its design: the two advisory dimensions (Evidence
  discipline, Solution economy) enter Weave points ONLY after the ADR-0019 advisory-evaluator-calibration calibration gates pass -
  evaluator stability (>=95% modal band, spread <=1 over 20 repeats), quadratic-weighted-kappa >=0.75
  against human labels, and an anti-Goodhart held-out counter-metric check; the advisory fold never
  raises a Blocked or Not Scored verdict (rule 8) and only folds a dimension whose
  (evaluatorVersion, taskClass, schemaVersion) triple is qualified in the registry; the leaderboard is
  Not Comparable below a cohort of 5 (rule 10) or with a single operator (US-10 privacy suppression),
  and is segmented by (task class, schema version) (rule 11); and the AgentStanding exposes rank, trend
  and one reason per dimension but NO single optimizable scalar (US-16 anti-Goodhart) - proven by 27
  tests incl. a reflection guard on the no-scalar contract and a mutation-verified cohort-minimum oracle.
  Full suite 889/0.
---

# Proof Pack: Loomkeeper Advisory Grader, Calibration, Leaderboard & Standing (slice 7)

- **Components:** `src/AiDe.Core/Watcher/AdvisoryScoring.cs` (`QuadraticWeightedKappa`, `EvaluatorStability`, `CalibrationVerdict`, `AdvisoryCalibration`, `CalibrationRegistry`, `AdvisoryAssessment`, `IAdvisoryEvaluator`, `AdvisoryWeaveScorer`) and `src/AiDe.Core/Watcher/Leaderboard.cs` (`ScoredEpisode`, `LeaderboardFacet`, `LeaderboardCell`, `Leaderboard`, `LeaderboardComposer`, `DimensionReason`, `AgentStanding`, `StandingComposer`). Slice-5 `WeaveScore.cs` refactored to expose `internal static WeaveScorer.ComposeScoredCard(...)` so the fold reuses the exact headline logic (no re-derivation of rule 2).
- **Tests:** `tests/AiDe.Core.Tests/Watcher/AdvisoryScoringTests.cs` (19) + `tests/AiDe.Core.Tests/Watcher/LeaderboardTests.cs` (8) — **27/27**; full `AiDe.Core.Tests` suite **889/0**; build clean (0 warnings, `TreatWarningsAsErrors`).
- **Pure engine, model-free** — `IAdvisoryEvaluator` is the seam for a real model judge; the calibration gates, the fold, the leaderboard and the standing are all deterministic and tested against fixtures. The real judge + its ADR-0018 credential-backed-grading-egress credential/egress boundary is out of scope here (residual).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| QWK is 1.0 on perfect agreement and -1.0 on maximal reverse disagreement | `Qwk_PerfectAgreement_IsOne`, `Qwk_MaximalReverseDisagreement_IsMinusOne` | `QuadraticWeightedKappa.Compute` | identical vectors → 1.0; `[0,0,4,4]` vs `[4,4,0,0]` → -1.0 (num 4, den 2) | Seen green | Verified | hand-computed fixtures |
| QWK degenerate/empty is 1.0 and length mismatch throws | `Qwk_EmptyVectors_IsOne`, `Qwk_LengthMismatch_Throws` | degenerate denominator guard | empty → 1.0; mismatched lengths → `ArgumentException` | Seen green | Verified | — |
| A high-agreement rater (one off-by-one) clears the 0.75 floor | `Qwk_HighAgreementWithOneOffByOne_IsAboveTheFloor` | `Floor = 0.75` | QWK of a near-identical vector ≥ 0.75 | Seen green | Verified | floor is the ADR-0019 advisory-evaluator-calibration gate (b) |
| Evaluator stability passes only at >=95% modal band with spread <=1 | `Stability_AllSame_Passes`, `Stability_NineteenOfTwentyInBand_SpreadOne_Passes`, `Stability_TooMuchDrift_Fails`, `Stability_BelowNinetyFivePercentInBand_Fails`, `Stability_Empty_Fails` | `EvaluatorStability.Of` | 20 repeats; 19/20 in band spread 1 → pass; drift or <95% → fail; empty → fail | Seen green | Verified | ADR-0019 advisory-evaluator-calibration gate (a) |
| Calibration qualifies only when all three gates pass | `Qualify_AllGatesPass_IsQualified` | `AdvisoryCalibration.Qualify` | stable + QWK≥floor + counter-metrics-not-worse → Qualified | Seen green | Verified | — |
| Instability, low agreement, and counter-metric worsening are each rejected with a reason | `Qualify_Unstable_IsRejectedWithReason`, `Qualify_LowHumanAgreement_IsRejectedWithReason`, `Qualify_CounterMetricsWorsened_IsRejectedAsGaming` | three gate branches | each failing gate → not qualified + named reason | Seen green | Verified | anti-Goodhart = ADR-0019 advisory-evaluator-calibration gate (c), rule 14 |
| A qualified advisory dimension folds into points and can reach fully Scored | `Fold_QualifiedAdvisory_AddsPoints_AndCanReachFullyScored` | `AdvisoryWeaveScorer.Score` | base Partial 70/70 + both advisory rubric-4 → all 6 scored → verdict Scored, "100 / 100" | Seen green | Verified | uses `ComposeScoredCard` (rule 2 preserved) |
| An unqualified advisory dimension stays excluded; a mix folds only the qualified one | `Fold_UnqualifiedAdvisory_StaysExcluded`, `Fold_OneQualifiedOneNot_AddsOnlyTheQualifiedDimension` | registry `IsQualified` on the triple | unqualified → posture stays Advisory, no points; one qualified → "85 / 85 observed" | Seen green | Verified | — |
| The advisory fold NEVER overrides a floor or a Not Scored verdict (rule 8) | `Fold_BlockedBase_IsReturnedUnchanged_AdvisoryNeverOverridesAFloor`, `Fold_NotScoredBase_IsReturnedUnchanged` | verdict guard | Blocked/NotScored base returned byte-identical regardless of advisory | Seen green | Verified | advisory can only add, never rescue |
| The leaderboard ranks harnesses above cohort by median Weave | `Compose_TwoHarnessesAboveCohort_RankByMedianWeave`, `Compose_RanksTheHarnessModelFacet` | `LeaderboardComposer.Compose` | two 5-cohorts, median `[80,82,84,86,88]`=84 vs lower → ranked | Seen green | Verified | median, not mean (outlier-robust) |
| Below a cohort of 5 the cell is Not Comparable (rule 10) | `Compose_BelowCohortMinimum_IsNotComparable` | `cohortMinimum=5` guard | 4-episode cell → RankComparable false, reason "cohort 4 < 5" | **Yes** — changing `cohort < cohortMinimum` to `cohort < 0` reds this test | Verified | mutation-verified oracle |
| A single-operator cell is suppressed for privacy (US-10) | `Compose_SingleOperator_IsNotComparable_PrivacyProtected` | `< 2` operators guard | 5 episodes one operator → Not Comparable, privacy reason | Seen green | Verified | US-10 small-cohort privacy |
| The leaderboard is segmented by (task class, schema version) (rule 11) | `Compose_OtherSchemaVersion_IsSegmentedOut` | segment filter | weave/2 episodes excluded from a weave/1 board; median unchanged | Seen green | Verified | — |
| The standing shows rank + trend + one reason per dimension (US-16) | `Standing_ComparableCell_ShowsRankTrend_AndOneReasonPerDimension` | `StandingComposer.Compose` | comparable cell → Rank set, Trend set, one `DimensionReason` per scored dimension | Seen green | Verified | — |
| An insufficient cohort still yields reasons + trend but no comparable rank | `Standing_InsufficientCohort_RankNotComparable_ButReasonsAndTrendPresent` | standing guard | small cohort → RankComparable false, Reasons + Trend still present | Seen green | Verified | honest "Not Comparable" |
| The AgentStanding exposes NO single optimizable scalar (US-16 anti-Goodhart) | `AgentStanding_ExposesNoSingleOptimizableScalar` | reflection over `AgentStanding` public properties | no `Score`/`Weave`/`Points`/`Rating`/`Grade`/numeric-scalar property exists | Seen green | Verified | a structural guard, not a value assertion |

## Testing Strategy triggers applied

- **T1 (pure deterministic logic):** QWK, stability, calibration verdict, the fold, the leaderboard composer and the standing composer are all pure functions of their inputs — unit-tested with boundary vectors (perfect, reverse, empty, off-by-one, at/below thresholds).
- **T1 mutation sense:** the cohort-minimum comparability guard (US-14/US-10, the leaderboard's privacy + comparability gate) was mutated `cohort < cohortMinimum` → `cohort < 0`, observed to red `Compose_BelowCohortMinimum_IsNotComparable`, then reverted. `TreatWarningsAsErrors` continues to serve as a compile-enforced oracle for the single-writer/enum/unused-field mutation class.
- **A5/A6 boundary (advisory = a model-judged dimension):** the calibration gates ARE the A5 eval-harness discipline made deterministic — an advisory (probabilistic) evaluator may not score until it passes stability + QWK-vs-human + anti-Goodhart; a change to the evaluator version is a contract change gated by the registry triple `(evaluatorVersion, taskClass, schemaVersion)`.
- **D0 hygiene:** every test invokes the focal method, asserts a meaningful outcome, is deterministic (fixed vectors, `FixedTimeProvider`, no wall-clock/random), and order-independent.

## Anti-Goodhart note (why there is no single number)

US-16 forbids a single optimizable scalar for standing because a leaderboard that reduces an agent to one number becomes the target it optimizes against (Goodhart). The `AgentStanding` record therefore deliberately carries `Rank`, `Cohort`, `Trend`, `RankComparable` and a `Reasons` list (one per dimension) and **no** aggregate score property. `AgentStanding_ExposesNoSingleOptimizableScalar` enforces this by reflection so a future edit that adds a `Score` cannot pass silently. This composes with rule 14 (held-out counter-metrics must not worsen) at the calibration layer: the advisory dimensions cannot be gamed into points, and the standing cannot be collapsed into a gameable target.

## Security / privacy note

- **US-10 small-cohort privacy** is enforced at composition: a single-operator cell is Not Comparable and its Weave is not surfaced as a rankable value, so one operator's episodes cannot be de-anonymized off a public leaderboard.
- **No credentials, no egress** in this slice — `IAdvisoryEvaluator` is an unimplemented seam. The real model judge and its ADR-0018 credential-backed-grading-egress credential-backed / egress-controlled boundary are out of scope (residual).

## Residual risk

- **Real model judge unimplemented** — `IAdvisoryEvaluator` has no production implementation; the fold is exercised only with fixture evaluators. The real judge + ADR-0018 credential-backed-grading-egress credential/egress boundary is the connective follow-on.
- **Persistence** — `CalibrationRegistry`, `ScoredEpisode` and leaderboard/standing outputs are in-memory; the SQLite `Scorecard`/calibration/leaderboard tables are a connective follow-on.
- **UI surfaces** — the Leaderboard and Standing WPF surfaces are not built; this slice is the engine only.
- **Dispute path** — an operator dispute of an advisory score (US-16 fairness) is designed but not implemented.
- **Counter-metric registry** — the anti-Goodhart held-out counter-metrics are supplied to `Qualify` by the caller; the standing library of counter-metrics per task class is not yet curated.
