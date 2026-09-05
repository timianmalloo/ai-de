---
id: design-watcher-weave-score
title: "Loomkeeper Deterministic Weave - Score, Floors, Coverage"
type: design
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, design, weave, scoring, floors, coverage, phase-2]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-work-episode, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0019-advisory-evaluator-calibration, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper deterministic Weave (slice 5): a pure scoring engine that evaluates a CLOSED
  Work Episode on the four deterministic dimensions (Outcome integrity 30, Focus & termination 15,
  Guidance adherence 15, Coordination & learning 10 = observed weight 70), leaving the two advisory
  dimensions (Evidence discipline, Solution economy = 30) excluded until the grader passes its
  calibration gates (slice 7). Hard floors (correctness, security, privacy, data integrity, evaluator
  integrity) trip a Blocked verdict and suppress the numeric headline; a missing goal/done/verification
  path is Not Scored; the headline is honest "Partial: earned / observed weight" with no rescale to
  0-100. Evidence Coverage is separate from points. This is where done_when becomes measured.
---

# Design: Loomkeeper Deterministic Weave

- **Status:** Accepted · **Tier:** T2 · **Phase:** 2, slice 5 · **Refines:** [`design-watcher-work-episode`](watcher-work-episode.md) (scores a *closed* episode).
- **Grounding:** spec §"Weave Score" (the six-dimension table + scoring rules 1-14). The **advisory** dimensions are gated by ADR-0019 advisory-evaluator-calibration (calibration + QWK) and are **out of slice 5** — they enter points only after slice 7's grader passes both gates (rule 9). Slice 5 is the **deterministic** Weave: the countable signals, the hard floors, the coverage, and the honest verdicts.

## 1. Responsibility and boundary

One responsibility: **turn a closed Work Episode's deterministic evidence into an honest Scorecard** — a per-dimension 0-4 assessment normalized to weight, the tripped hard floors, Evidence Coverage, and a verdict (`Scored` / `Partial` / `Blocked` / `NotScored`) — **without** any model judgment. It owns the deterministic scoring rules; it borrows the episode (slice 4); it does **not** own the advisory grader, calibration, the leaderboard, or standing (slice 7), and it does **not** persist the Scorecard (a follow-on).

**This is where `done_when` becomes measured.** The Focus-and-termination dimension's *"work after done condition"* and the Outcome-integrity dimension's *"honest completion claim"* are exactly the PACK-O drift / under-validation faces (the AI-Forward `done_when` work): an episode's `DoneWhen` (slice 4) is the reference against which continuation past sufficiency and premature completion are counted.

## 2. Data model

No new persisted shape in this slice (the engine is pure; Scorecard persistence is a follow-on). The types:

- **`ScoreDimension`** ∈ { OutcomeIntegrity, FocusAndTermination, EvidenceDiscipline, GuidanceAdherence, SolutionEconomy, CoordinationAndLearning }.
- **`ScoreSchema`** — versioned (`ScoreSchemaVersion = "weave/1"`, pinned, A6 contract): the weight and posture (`Deterministic` | `Advisory`) per dimension. `weave/1`: Outcome 30 · Focus 15 · Guidance 15 · Coordination 10 = **70 deterministic**; Evidence 15 · Economy 15 = **30 advisory (excluded)**.
- **`FloorDomain`** ∈ { Correctness, Security, Privacy, DataIntegrity, EvaluatorIntegrity } (the canonical hard floors, rule 6).
- **`AssessmentPosture`** ∈ { Deterministic, Advisory, NotRecorded } — an advisory or un-signalled dimension is **NotRecorded**, never a fake 0.
- **`DimensionAssessment`** (`Dimension`, `Weight`, `Rubric0to4?`, `EarnedPoints?`, `Posture`, `Rationale`) — `EarnedPoints = Rubric/4 × Weight` when scored.
- **`EvidenceCoverage`** (`Observed`, `Required`) — nullable; **Not Recorded** when uncalibrated (rule 3). Separate from points, never a multiplier (rule 4).
- **`WeaveVerdict`** ∈ { Scored, Partial, Blocked, NotScored }.
- **`Scorecard`** (`EpisodeId`, `SchemaVersion`, `Verdict`, `Assessments`, `TrippedFloors`, `Coverage?`, `Headline`, `EvaluatedAt`).
- **`DeterministicEpisodeSignals`** — the countable evidence gathered about the episode (the engine's pure input; populating it from the store/ingest is the wiring residual). Fields map 1:1 to the spec's signals (see §3).

**Grain:** one Scorecard is one evaluation of one closed episode under one schema version at one evaluation time (spec line 236). Disputes append a superseding evaluation (rule 12) — a persistence concern, out of this slice.

## 3. Contracts

```csharp
public sealed record DeterministicEpisodeSignals(
    bool HasVerificationPath,                          // gate: minimum verification path present (rule 5)
    bool? AcceptanceCriteriaMet,                       // outcome; null => unknown (NotRecorded contribution)
    bool RequiredVerificationExecuted,                 // outcome + correctness floor
    bool RegressionPresent,                            // outcome + correctness floor
    IReadOnlySet<FloorDomain> UnresolvedFloorBlockers, // floors (rule 6)
    int ActionsAfterDoneCondition,                     // focus: work past done_when (PACK-O drift)
    bool PrematureCompletion,                          // focus: Completed but acceptance not met
    int RequiredGuidanceTriggers, int SatisfiedGuidanceTriggers,        // guidance
    int RequiredCoordinationSignals, int ObservedCoordinationSignals,   // coordination
    bool CoverageCalibrated, int RequiredSignalTotal, int ObservedSignalTotal); // coverage (rule 3)

public sealed class WeaveScorer
{
    Scorecard Score(WorkEpisode episode, DeterministicEpisodeSignals signals, TimeProvider time);
    // schema defaults to ScoreSchema.Weave1; an overload accepts an explicit schema.
}
```

## 4. Scoring algorithm (deterministic; the rules it enforces)

1. **Not Scored gate (rule 5):** if the episode's `Goal` or `DoneWhen` is blank, or `!HasVerificationPath`, or the episode is **not closed** → `NotScored` (no headline; reason stated). *An episode with a done-condition is scoreable; without one it is honestly Not Scored — the done_when made measurable.*
2. **Hard floors (rules 6-7):** a floor trips when its domain is in `UnresolvedFloorBlockers`; **Correctness additionally** trips when `AcceptanceCriteriaMet == false`, `RegressionPresent`, or `!RequiredVerificationExecuted`. Any tripped floor → verdict **Blocked**, numeric headline **suppressed**, tripped floors listed. Numeric scores have no independent pass/fail threshold (rule 7).
3. **Deterministic dimensions** (0-4 rubric, `EarnedPoints = Rubric/4 × Weight`):
   - **Outcome integrity (30):** `Completed` + `AcceptanceCriteriaMet==true` + `!RegressionPresent` + `RequiredVerificationExecuted` → 4; each missing/false steps it down; `AcceptanceCriteriaMet==null` → the dimension is **NotRecorded** (honest unknown).
   - **Focus & termination (15):** starts at 4; `ActionsAfterDoneCondition > 0` and `PrematureCompletion` each reduce it (the drift/under-validation penalty).
   - **Guidance adherence (15):** `round(4 × Satisfied/Required)` when `Required > 0`, else **NotRecorded**.
   - **Coordination & learning (10):** `round(4 × Observed/Required)` when `Required > 0`, else **NotRecorded**.
4. **Advisory dimensions (rule 9):** Evidence discipline (15) and Solution economy (15) are **Advisory** and **excluded** from points in `weave/1` (they enter only after the grader passes calibration + QWK, slice 7) → posture Advisory, `EarnedPoints` null.
5. **Evidence Coverage (rules 3-4):** `!CoverageCalibrated` → **Not Recorded**; else `EvidenceCoverage(Observed, Required)`. Never folded into points.
6. **Verdict & headline (rule 2):** all six scored → `Scored`, headline `"<earned> / 100"`; any NotRecorded/Advisory → **`Partial`**, headline `"Partial: <earned> / <observed weight>"` (sum of the *scored* dimensions' weights) — **no rescale to 0-100**. The common slice-5 case is `Partial: earned / 70` (the deterministic weight), matching the spec's `58 / 70 observed` example.

## 5. Failure-mode analysis

| # | Failure mode | Disposition |
|---|---|---|
| Input | blank goal/done, or no verification path | **NotScored** (rule 5); test |
| Input | episode still open (not closed) | **NotScored** ("not closed"); test |
| Floor | an unresolved Blocker in any floor domain | **Blocked** + floor listed + headline suppressed; test per domain |
| Floor | acceptance-not-met / regression / verification-not-executed | **Blocked** via Correctness; test |
| Data | a dimension has no deterministic signal (required triggers 0) | **NotRecorded**, never a fake 0; test |
| Data | AcceptanceCriteriaMet unknown (null) | Outcome **NotRecorded**; test |
| Coverage | uncalibrated required-signal set | Coverage **Not Recorded**, not 100% and not 0 (rule 3); test |
| Integrity | forged/tampered evidence (EvaluatorIntegrity blocker) | **Blocked** (rule 6, evaluator integrity); test |
| Rescale | tempting to rescale Partial to 0-100 | **Forbidden** (rule 2); a test asserts the observed-weight denominator |

## 6. Security / privacy

The engine is pure over already-collected deterministic signals; it adds no trust boundary and no egress. **Evaluator integrity is a first-class floor** (rule 6): forged/tampered evidence, grader injection, redaction failure, or held-out leakage trip **Blocked** — an advisory judgment can never raise a deterministic failed dimension (rule 8), and (slice 7) can never enter points before its calibration gates. No personal data: the signals are counts and booleans about a task, not a person; cross-episode aggregation and human non-identification (rules 11) are a slice-7 concern.

## 7. Instrumentation (IO1)

Operator questions: how many episodes scored **Scored / Partial / Blocked / NotScored**, which **floor** trips most, the **observed-weight** distribution (how much is deterministic-scoreable vs advisory-excluded), and the **coverage** distribution. Each is derivable from the Scorecard verdict + assessments.

## 8. Test plan (Testing Strategy D1; E11)

- **D1:** NotScored (blank goal / blank done / no verification / open episode); each floor domain trips Blocked (Correctness via blocker AND via acceptance-not-met / regression / verification-not-executed; Security; Privacy; DataIntegrity; EvaluatorIntegrity); Outcome rubric ladder (4 → down per missing); Outcome NotRecorded on null acceptance; Focus penalty for drift + premature; Guidance/Coordination proportional rubric + NotRecorded when required 0; advisory dimensions Advisory/excluded; Coverage Not Recorded when uncalibrated vs present when calibrated; Partial headline uses the observed-weight denominator (no rescale); a full-signal clean episode → the highest deterministic Partial.
- **E11 (composition):** a real closed `WorkEpisode` (from `WorkEpisodeService`) scored end to end → the expected verdict + headline.
- **A6:** `ScoreSchema.Weave1.Version == "weave/1"` pinned; a change is a gated contract change.
- **Mutation:** one load-bearing oracle (a floor trip suppressing the headline, or the Partial denominator) red-then-revert.

## 9. Ladder / simplicity

Pure function over an explicit signals record — **no store change, no dependency, no model**. The advisory dimensions are *declared and excluded*, not stubbed with fake numbers. The schema is a single pinned constant (`weave/1`), not a config system.

## 10. Residual (out of slice 5)

- **Signal collection** — populating `DeterministicEpisodeSignals` from the observation store, coordination log, and CI/verification ingest — is the connective follow-on; slice 5 ships the pure engine both a real collector and a test drive.
- **Scorecard persistence** (append-only fact; dispute-superseding, rule 12) — a store follow-on.
- **The advisory grader + calibration + QWK gates + leaderboard + standing** — slice 7 (ADR-0019 advisory-evaluator-calibration).

## 11. Gate record

`GATE design · 2026-08-31 · reviewers (Adversary Mode): Test Architect (every floor + verdict + the no-rescale denominator has a test; advisory excluded not faked), Security & Identity (evaluator-integrity floor; advisory cannot raise a deterministic fail), Simplifier (pure function, advisory declared-not-stubbed, one pinned schema), Patterns Expert (rubric-normalized-to-weight; floors-as-gates), SRE (verdict + floor counters) · verdict: PASS-WITH-CONDITIONS · conditions — signal collection, Scorecard persistence, and the advisory grader are later slices`

**Handoff:** → `/implement` this design (records + engine, TDD).
