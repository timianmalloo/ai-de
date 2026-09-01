---
id: proof-watcher-scoring-service
title: "Proof Pack - Loomkeeper Evidence Composer & Scoring Service (connective 6)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, proof-pack, scoring, evidence, calibration, phase-4]
links:
  - { to: design-watcher-scoring-service, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that the scoring path is wired: EvidenceComposer maps deterministic signals to the local
  evaluator's token vocabulary (omitting unobserved tokens so they default conservatively) and round-trips
  through the evaluator; ScoringService scores an episode and persists a ScoredEpisode that feeds the
  Leaderboard; the two advisory dimensions fold only when the evaluator is qualified in the registry
  (ADR-0019, rule 8) and stay excluded otherwise; and a recompute replaces the prior card. 9 tests, full
  suite 955/0, the composer->evaluator mapping mutation-verified.
---

# Proof Pack: Evidence Composer & Scoring Service (connective 6)

- **Components:** `src/AiDe.Core/Watcher/ScoringService.cs` (`EvidenceComposer.Compose`, `ScoringService.ScoreAndRecord`).
- **Tests:** `tests/AiDe.Core.Tests/Watcher/ScoringServiceTests.cs` — 9 tests, **9/9**; full `AiDe.Core.Tests` **955/0**; builds clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| Clean signals map to the expected tokens | `EvidenceComposer_MapsCleanSignals_ToTokens` | `EvidenceComposer.Compose` | verification=executed, coverage=9/10, actions_after_done=0, premature=false | Seen green | Verified | — |
| Not-executed / premature map conservatively | `EvidenceComposer_NotExecuted_Premature_MapsConservatively` | signal mapping | verification=none, premature=true, actions_after_done=5 | Seen green | Verified | NG1 - a missing/negative signal lowers |
| The composed tokens are exactly what the local evaluator reads | `EvidenceComposer_RoundTripsThroughTheLocalEvaluator` | composer + evaluator | clean signals → evidence rubric 4 | **Yes** — forcing `verification=none` reds this | Verified | mutation-verified oracle |
| No evaluator persists a deterministic card, advisory excluded | `ScoreAndRecord_NoEvaluator_PersistsDeterministicScorecard_AdvisoryExcluded` | `WeaveScorer` path | 1 stored, Partial, EvidenceDiscipline EarnedPoints null | Seen green | Verified | enough to populate the leaderboard |
| A qualified evaluator folds advisory points | `ScoreAndRecord_QualifiedEvaluator_FoldsAdvisory` | `AdvisoryWeaveScorer` + registry | EarnedPoints set, rationale "calibrated" | Seen green | Verified | ADR-0019 gate |
| An unqualified evaluator leaves advisory excluded | `ScoreAndRecord_UnqualifiedEvaluator_LeavesAdvisoryExcluded` | registry gate | EarnedPoints null | Seen green | Verified | rule 8 |
| The classification is carried onto the ScoredEpisode | `ScoreAndRecord_CarriesTheClassification` | wrapper | operator/task/harness/model preserved | Seen green | Verified | supplied by caller |
| Scored episodes feed the Leaderboard composer | `ScoreAndRecord_PersistsEpisodes_ThatFeedTheLeaderboard` | `LeaderboardComposer` | 5 scored → HarnessModel cell cohort 5, comparable | Seen green | Verified | conn-1+conn-6 end to end |
| A recompute replaces the persisted card | `ScoreAndRecord_Recompute_ReplacesTheCard` | `RecordScorecard` upsert | Partial → Blocked replaces, single row | Seen green | Verified | conn-1 cache refresh |

## Testing Strategy triggers applied

- **T1 (pure deterministic logic):** the composer and the deterministic scoring path are pure functions; unit-tested across clean and adversarial signals.
- **A6 (contract/version gate):** the advisory fold is gated by the `(evaluatorVersion, taskClass, schemaVersion)` registry key — a change to the evaluator or the schema is a contract change that must be re-qualified before it affects a score.
- **E11-ish (read-through):** `..._ThatFeedTheLeaderboard` proves the persisted cards reach the exact `LeaderboardComposer` the WPF surface folds.
- **T1 mutation sense:** the composer→evaluator mapping (the load-bearing bridge) was mutated to emit `verification=none`, observed to red the round-trip, then reverted.
- **D0 hygiene:** deterministic (`FixedTimeProvider`), isolated, focal-call + meaningful assertion.

## Residual risk

- **Signals not auto-derived** — the service takes `DeterministicEpisodeSignals` and the classification as inputs; deriving them from raw spans/coordination (the analysis layer) is future work. The scoring pipeline that consumes them is proven now.
- **No auto-score-on-close trigger** — nothing yet calls `ScoreAndRecord` when an episode closes in the running host; a trigger in the pump loop (score closed, unscored episodes) is a follow-on once signals derivation exists.
- **Calibration workflow** — qualifying the local heuristic requires human labels through `AdvisoryCalibration`; the registry gate is proven, the label-collection UX is not built.
