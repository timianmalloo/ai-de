---
id: design-watcher-scoring-service
title: "Loomkeeper - Evidence Composer & Scoring Service (connective 6)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, scoring, evidence, calibration, advisory, phase-4]
links:
  - { to: design-watcher-weave-score, rel: depends-on }
  - { to: design-watcher-advisory-grader, rel: depends-on }
  - { to: design-watcher-advisory-evaluator, rel: depends-on }
  - { to: design-watcher-score-persistence, rel: depends-on }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Compose a closed episode's DeterministicEpisodeSignals into the local evaluator's evidence token string
  (EvidenceComposer), and turn (episode + signals + classification) into a persisted ScoredEpisode
  (ScoringService) so scored episodes reach the Leaderboard/Standing surfaces. The four deterministic
  dimensions are always scored; the two advisory dimensions fold only when the evaluator's
  (version, taskClass, schemaVersion) is qualified in the calibration registry (ADR-0019 advisory-evaluator-calibration, rule 8);
  with no evaluator, only the deterministic Weave is recorded (the safe default).
---

# Design: Evidence Composer & Scoring Service (connective 6)

## 1. Problem & scope

conn-1 persists ScoredEpisodes and conn-2 renders them, but nothing PRODUCED a ScoredEpisode from an
episode, so the Leaderboard/Standing surfaces had nothing to show. conn-3 built the local advisory
evaluator but nothing fed it evidence. This slice wires the scoring path: signals -> evidence tokens ->
(deterministic Weave + optional qualified advisory fold) -> persisted ScoredEpisode.

**In scope:** `EvidenceComposer` (signals -> token string); `ScoringService.ScoreAndRecord` (score +
persist, with the advisory fold gated by the registry). **Out of scope:** deriving
`DeterministicEpisodeSignals` from raw spans/coordination (a whole analysis layer - the caller supplies
signals + classification); auto-scoring on episode close in the running host (a trigger wiring follow-on);
the human-label calibration workflow that qualifies an evaluator (the operator's action via
`AdvisoryCalibration`/`CalibrationRegistry.Qualify`).

## 2. EvidenceComposer (signals -> tokens, NG1)

The `LocalHeuristicAdvisoryEvaluator` (conn-3) grounds on a `key=value; ...` token string. The composer
maps the signals we actually capture:

| Token | From |
|---|---|
| `verification=executed\|none` | `RequiredVerificationExecuted` |
| `coverage=<observed>/<required>` | `ObservedSignalTotal` / `RequiredSignalTotal` |
| `actions_after_done=<n>` | `ActionsAfterDoneCondition` |
| `premature=true\|false` | `PrematureCompletion` |

A token the evaluator looks for but we do not observe (e.g. `reuse`) is **omitted**, so the evaluator's
conservative default applies (a missing signal can only lower a score, never raise it - NG1). This is the
honest mapping: we do not synthesise a signal we do not have.

## 3. ScoringService (score + persist)

`ScoreAndRecord(episode, signals, operatorId, taskClass, harness?, model?, evaluator?, registry?)`:

- **No evaluator (default):** score the four deterministic dimensions via `WeaveScorer.Score`; the two
  advisory dimensions stay excluded. This alone populates the Leaderboard.
- **Evaluator + registry:** compose the evidence, evaluate the two advisory dimensions with the local
  heuristic, and fold via `AdvisoryWeaveScorer` - which earns points ONLY for a dimension whose
  `(evaluatorVersion, taskClass, schemaVersion)` is qualified, and NEVER overrides a floor or a Not Scored
  verdict (rule 8). The result is wrapped as a `ScoredEpisode` (with the caller's classification) and
  `RecordScorecard`ed - a recompute replaces the prior card (conn-1 cache refresh).

The classification (harness/model/operator/taskClass) is supplied by the caller because it comes from the
session binding + the episode, which this pure service does not re-derive.

## 4. Failure modes & dispositions

| Failure mode | Disposition |
|---|---|
| Unobserved evidence token | Omitted; evaluator defaults conservatively (NG1, tested) |
| Evaluator supplied but not qualified | Advisory stays excluded (EarnedPoints null, tested) |
| Advisory would raise a floored/NotScored card | `AdvisoryWeaveScorer` returns the base card unchanged (rule 8) |
| Re-scoring an episode | UPSERT replaces the card (conn-1; tested Partial -> Blocked replace) |
| Empty operator/taskClass | Guarded (`ThrowIfNullOrEmpty`) |

## 5. Test plan

- `ScoringServiceTests` (9): composer maps clean + conservative signals and round-trips through the local
  evaluator; deterministic-only persist with advisory excluded; qualified fold earns points; unqualified
  stays excluded; classification carried; five scored episodes feed the Leaderboard composer; recompute
  replaces the card.
- The composer->evaluator mapping is mutation-verified (forcing `verification=none` reds the round-trip).
