---
id: design-watcher-advisory-grader
title: "Loomkeeper Advisory Grader - Calibration Gates, Leaderboard, Standing"
type: design
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, design, advisory, calibration, kappa, leaderboard, standing, phase-4]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-weave-score, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0019-advisory-evaluator-calibration, rel: depends-on }
  - { to: adr-0018-credential-backed-grading-egress, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper advisory grader (slice 7, final). The deterministic cores: the ADR-0019
  calibration gates (stability >=95% band consistency with spread <=1, quadratic weighted kappa >=0.75
  vs human labels, and anti-Goodhart counter-metrics that must not worsen) that decide whether an
  advisory evaluator version may contribute points; the gated fold of a qualified advisory dimension
  into the Weave (never overriding a deterministic dimension); the leaderboard (cohort >=5 or Not
  Comparable, segmented by task class + score schema version, per harness/model/harness-model,
  non-identifying); and per-turn agent standing (rank + trend + one evidence reason per dimension, no
  single optimizable scalar). The model judge itself sits behind an IAdvisoryEvaluator seam.
---

# Design: Loomkeeper Advisory Grader

- **Status:** Accepted · **Tier:** T2 · **Phase:** 4, slice 7 (final) · **Refines:** [`design-watcher-weave-score`](watcher-weave-score.md) (adds the advisory half the deterministic Weave excluded).
- **Grounding:** spec scoring rules **8-14**, **US-8** (calibration), **US-14** (leaderboard), **US-16** (standing), **US-10** (small-cohort privacy), and **ADR-0019** (the two gates). The **model judge** is non-deterministic and needs grounded evidence + credentials (Phase 4/5, its own threat model); slice 7 ships the **deterministic gate + fold + leaderboard + standing** and puts the judge behind a seam.

## 1. Responsibility and boundary

One responsibility: **decide whether, and how, a non-deterministic advisory judgment may enter the score and the fleet ranking - and turn a scorecard into honest per-turn feedback**. It owns the calibration math, the gated fold, the leaderboard, and the standing; it borrows the deterministic Weave (slice 5) and the scorecard shape. It does **not** own the model judge's prompt/grounding (a seam) or the credential/egress path (ADR-0018, Phase 4/5).

**The anti-Goodhart stance is the point (rules 8/9/14, US-16).** An advisory judgment can never *raise* a deterministic failed dimension (rule 8); it enters points *only* after passing both calibration gates (rule 9); a visible score gain is *rejected* if the held-out counter-metrics (outcome integrity, regression rate, rework, dispute overturns) worsen (rule 14); and per-turn standing exposes evidence + trend, **never a single optimizable scalar** (US-16).

## 2. Data model

No new persisted shape in this slice (pure engines over scorecards; persistence is a follow-on). Types:
- **`QualityRating`** = an int in 0..4 (the shared rubric scale).
- **`EvaluatorStability`** - over N repeated evaluations of one item: modal-band fraction + spread.
- **`CalibrationVerdict`** (`Qualified`, `Reasons`) - the outcome of the two gates + the counter-metric check.
- **`CalibrationRegistry`** - records qualified `(EvaluatorVersion, TaskClass, SchemaVersion)`; `IsQualified(...)`.
- **`IAdvisoryEvaluator`** - the model-judge seam: `EvaluatorVersion` + `Evaluate(dimension, episode, evidence) -> AdvisoryAssessment(rubric, rationale, evidencePointer)`.
- **`AdvisoryWeaveScorer`** - wraps `WeaveScorer`; folds a *qualified* advisory assessment into the scorecard as a scored dimension, leaving unqualified ones Advisory/excluded.
- **Leaderboard:** `LeaderboardCell` (`Facet`, `Harness?`, `Model?`, cohort size, median Weave, coverage, rank?, `Comparable`), `Leaderboard` (task class, schema version, cells).
- **Standing:** `DimensionReason`, `AgentStanding` (harness-model rank, trend, one reason per dimension; no aggregate scalar).

## 3. Contracts

```csharp
public static class QuadraticWeightedKappa { static double Compute(IReadOnlyList<int> a, IReadOnlyList<int> b, int categories = 5); }

public sealed record EvaluatorStability(double ModalBandFraction, int Spread)
{ bool Passes => ModalBandFraction >= 0.95 && Spread <= 1; static EvaluatorStability Of(IReadOnlyList<int> repeats); }

public sealed record CalibrationVerdict(bool Qualified, IReadOnlyList<string> Reasons);

public static class AdvisoryCalibration
{
    const double KappaFloor = 0.75;
    static CalibrationVerdict Qualify(IReadOnlyList<int> stabilityRepeats,
        IReadOnlyList<int> evaluatorRatings, IReadOnlyList<int> humanRatings, bool counterMetricsHeldNoWorse);
}

public sealed class CalibrationRegistry { void Qualify(string evaluatorVersion, string taskClass, string schemaVersion); bool IsQualified(...); }

public interface IAdvisoryEvaluator { string EvaluatorVersion { get; } AdvisoryAssessment Evaluate(ScoreDimension d, WorkEpisode e, string evidence); }

public sealed class LeaderboardComposer { Leaderboard Compose(IReadOnlyList<ScoredEpisode> episodes, string taskClass, string schemaVersion, int cohortMinimum = 5); }

public sealed class StandingComposer { AgentStanding Compose(ScoredEpisode subject, Leaderboard board, int trend); }
```

## 4. The deterministic algorithms

- **Quadratic Weighted Kappa (gate b):** categories `0..K` (K=4). Weights `w[i,j]=(i-j)^2/K^2`; observed `O` from the paired ratings; expected `E` from the marginals; `kappa = 1 - sum(w*O)/sum(w*E)`. `>= 0.75` passes (rule 9b). Degenerate (perfect agreement, or zero expected disagreement) -> kappa 1.
- **Stability (gate a):** over N repeats of one item, `ModalBandFraction = maxBandCount / N`, `Spread = max - min`. Passes when `>= 0.95` and `<= 1` (rule 9a).
- **Calibration verdict:** Qualified iff stability passes **and** QWK `>= 0.75` **and** `counterMetricsHeldNoWorse` (rule 14 anti-Goodhart). Each failing gate contributes a reason.
- **Gated fold (rule 9):** an advisory dimension is scored (posture Advisory-but-scored, `EarnedPoints = rubric/4 * weight`) **only** when its `(evaluatorVersion, taskClass, schemaVersion)` `IsQualified`; otherwise it stays excluded (posture Advisory, no points) - exactly the slice-5 behavior. A tripped hard floor still yields Blocked (advisory never overrides - rule 8).
- **Leaderboard (rules 10-11, US-14):** group episodes by facet (Harness / Model / HarnessModel) within one `(taskClass, schemaVersion)`; a cell with `cohort < 5` **or** that resolves to a single operator renders **Not Comparable**, never a rank (US-10/US-14); comparable cells rank by **median Weave**, carrying cohort size + Evidence Coverage + trend.
- **Standing (US-16):** the subject's harness-model rank + trend + **one evidence-backed reason per dimension**; it exposes **no single aggregate scalar** to optimize (the record has per-dimension reasons and a rank, never a "score to beat").

## 5. Failure-mode analysis

| # | Failure mode | Disposition |
|---|---|---|
| Calibration | evaluator unstable across repeats | gate a fails -> Not Qualified (reason); test |
| Calibration | low human agreement (QWK < 0.75) | gate b fails -> Not Qualified; test |
| Calibration | score up but counter-metrics worse (Goodhart) | rule 14 fails -> Not Qualified "score gaming/miscalibration" (US-8); test |
| Fold | advisory dimension from an unqualified evaluator | excluded (no points); test |
| Fold | advisory tries to lift a floored/failed result | impossible - floors gate first, advisory is its own dimension (rule 8); test |
| Leaderboard | cohort < 5 | Not Comparable, never a rank (US-14); test |
| Leaderboard | cell proxies one operator | Not Comparable (US-10 privacy); test |
| Leaderboard | mixed schema versions | segmented - only same-version compared (rule 10); test |
| Standing | exposes a single optimizable scalar | forbidden - per-dimension reasons + rank only (US-16); test |
| Standing | insufficient cohort for a rank | trend/reasons shown, rank Not Comparable; test |

## 6. Security / privacy

- **Anti-Goodhart** (rules 8/9/14, US-16) is the security property: no path lets an advisory judgment raise a deterministic fail, enter points uncalibrated, survive worsening counter-metrics, or become a single gameable target.
- **Small-cohort privacy (US-10):** a cohort `< 5` or a single-operator cell is suppressed as Not Comparable; ranking identifiable people is refused by construction (the leaderboard has no person facet).
- **Prompt-injection invariance:** the model judge (seam) consumes board/episode evidence that is quarantined (slice 6); the deterministic gate + fold guarantee an injection fixture cannot change a *scored* result (the scored dimensions are deterministic or calibration-gated). The credential/egress path for a real judge is ADR-0018 (Phase 4/5, opt-in, off by default).

## 7. Test plan (Testing Strategy D1)

- **D1 (QWK):** perfect agreement -> 1; systematic one-band disagreement -> known value; independent/adversarial -> low; symmetry; a hand-computed fixture.
- **D1 (stability):** all-same -> pass; one outlier within 5% -> pass; >5% off-band -> fail; spread 2 -> fail.
- **D1 (calibration):** qualified when all three hold; each single failing gate -> Not Qualified with its reason; counter-metric-worse -> Not Qualified.
- **D1 (fold):** qualified evaluator -> advisory dimension scored (points, Scored/Partial verdict reflects the added weight); unqualified -> excluded; floor still Blocked with advisory present.
- **D1 (leaderboard):** >=5 comparable -> ranked by median Weave with cohort+coverage; <5 -> Not Comparable; single-operator -> Not Comparable; different schema versions segmented.
- **D1 (standing):** rank + trend + one reason per dimension; no single scalar field; insufficient-cohort -> rank Not Comparable but reasons/trend present.
- **Mutation:** one load-bearing oracle (the QWK 0.75 floor, or the cohort-5 minimum) red-then-revert.

## 8. Ladder / simplicity

Pure math + composition over the existing scorecard - **no new dependency, no ML library** (QWK and band-consistency are a few lines each). The model judge is a seam, not built here (it needs grounding + credentials + a threat model - Phase 4/5). The registry is an in-memory set of qualified tuples, not a config system.

## 9. Residual (out of slice 7)

- **The real model judge** (`IAdvisoryEvaluator` grounded implementation) + its **credential/egress** path (ADR-0018) + its **prompt-injection-invariance** corpus - Phase 4/5, with a threat model.
- **Scorecard / leaderboard / standing persistence** and the **WPF surfaces** (Leaderboard + per-turn standing states) - follow-ons (the slice-3 pattern).
- **Dispute** superseding evaluation records (rule 12) - a persistence follow-on.

## 10. Gate record

`GATE design · 2026-08-31 · reviewers (Adversary Mode): AI Systems Engineer (advisory gated by calibration + QWK; anti-Goodhart counter-metric; never overrides deterministic - eval harness is the gate), Security & Identity (advisory cannot raise a fail; injection invariance), Privacy & Data Governance (small-cohort + single-human suppression; no person facet), Test Architect (each gate + the cohort minimum + the no-scalar standing has a test), Simplifier (pure math, judge behind a seam, no ML dep) · verdict: PASS-WITH-CONDITIONS · conditions - the real model judge, its credential/egress path, persistence, and the UI surfaces are Phase 4/5 follow-ons`

**Handoff:** → `/implement` this design (calibration + fold + leaderboard + standing, TDD).
