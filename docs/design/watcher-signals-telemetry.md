---
id: design-watcher-signals-telemetry
title: "Loomkeeper Signals Telemetry Convention + Advisory-Evaluator Seam (t3/t4)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, design, signals, telemetry, advisory, cloud-judge, t3, t4, phase-2]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: design-watcher-signals-derivation, rel: refines }
  - { to: design-watcher-dispute-command, rel: depends-on }
review-by: 2027-02-28
review-suggested: []
summary: >-
  The honest richer-signals path: an OPTIONAL `signals` object an instrumented AI-Forward turn records on its
  audit entry (the telemetry convention), read into the deterministic signals with a conservative fallback
  for every absent field (no fabrication). Plus the advisory-evaluator seam - the auto-score path accepts an
  optional evaluator + calibration registry, so the on-device local heuristic (no egress) folds the two
  advisory dimensions when qualified; the cloud judge is the same seam behind an egress opt-in + creds.
---

# Signals Telemetry Convention + Advisory-Evaluator Seam (t3/t4)

## Problem & spec trace

conn-10 scored imported episodes from the one honestly-observable signal (a committed Proof Pack) → a thin
**Partial** (Focus only). To score richer without **fabricating** the signals we cannot observe (spec L127,
NG1), two things are needed and neither may be guessed: (t3) a way for a turn to *record* the signals it
actually observed, and (t4) the evaluator seam that folds the qualitative (advisory) dimensions.

## t3 — the signals telemetry convention (reader half)

**The convention:** an instrumented AI-Forward turn MAY record an optional `signals` object on its audit
entry (AL5), carrying only what it actually observed:

```json
"signals": {
  "verification_path": true, "verification_executed": true, "acceptance_met": true, "regression": false,
  "guidance_required": 5, "guidance_satisfied": 5, "coordination_required": 2, "coordination_observed": 2
}
```

**The reader (this change):** `AuditSignals` (all-nullable record) is parsed from the object;
`EpisodeEvidence` carries it; `DeterministicSignalsDeriver` uses each field as **`explicit ?? conservative
default`**. So:
- an **instrumented** turn (full `signals`) → all four deterministic dimensions score + coverage recorded
  (the honest ceiling of deterministic scoring — still Partial until the advisory dims fold, t4);
- an **un-instrumented** entry (no `signals`) → **exactly the conservative conn-10 behaviour** (acceptance
  null, guidance/coordination 0, coverage uncalibrated). Absent never fabricates (mutation-verified).

**Writer half (future, out of scope here):** `audit-log.py` emitting the `signals` object is an AI-Forward
enhancement. The reader tolerates its absence, so this ships value now and richer scores arrive when the
harness is instrumented — a clean cross-component contract, not a guess.

## t4 — the advisory-evaluator seam

`WatcherHost.ImportAndScoreEpisodesFromAuditLog` gains optional `IAdvisoryEvaluator? evaluator` +
`CalibrationRegistry? registry`, passed straight to `ScoringService.ScoreAndRecord`. Semantics (ADR-0019,
rule 8, unchanged): the two advisory dimensions (EvidenceDiscipline, SolutionEconomy) fold **only** if the
evaluator has **qualified** in the registry; otherwise they stay excluded.

- **Default (both null):** deterministic Weave only — the safe default the shell uses.
- **Local heuristic (`local-heuristic/1`):** **on-device, no egress**; folds once calibrated. This is the
  honest "cloud judge" alternative that needs no credentials — the gate is calibration (human-labelled
  episodes), not connectivity.
- **Cloud judge (`DelegatingAdvisoryEvaluator` behind `EgressGuardedAdvisoryEvaluator`):** the *same seam*,
  additionally behind an operator **egress opt-in + credentials**. No further code is needed to adopt it —
  supply the qualified evaluator + registry.

The shell stays deterministic-only by design: it has no calibration data, so folding would be excluded
anyway (and silently supplying an unqualified evaluator does nothing).

## Data model

No new persisted shape. `AuditSignals` is transient evidence (not stored); the `ScoredEpisode`/`Scorecard`
are unchanged (the advisory fold reuses the existing `AdvisoryWeaveScorer`). The scorecard remains a
derived cache (DM7).

## Change-surface list (E7)

audit entry (`signals`, reader) → `AuditSignals`/`EpisodeEvidence` (new field) → parser (reads object) →
deriver (explicit-or-default) → host auto-score (evaluator+registry passthrough) → scorer (unchanged fold)
→ store scorecard → leaderboard read (unchanged). No new field crosses the wire beyond the transient signals.

## Failure modes & dispositions

| Mode | Disposition |
|---|---|
| No `signals` object | `Signals` null → deriver uses conservative defaults (conn-10 behaviour). |
| Partial `signals` (some fields) | Each absent field falls back to its default; present fields used. |
| Malformed `signals` (wrong types) | Type-checked reads return null → conservative default; a corrupt line is still skipped whole. |
| Fabricated-high signals in a hand-edited entry | Self-tampering of one's own local record for a non-comparable local score (accepted; STRIDE §below). |
| Evaluator supplied but unqualified | Advisory dims stay excluded (ADR-0019) — no silent over-scoring. |
| Cloud evaluator without egress opt-in | `EgressGuardedAdvisoryEvaluator` blocks egress by default — no content leaves the device. |

## Adversarial analysis (STRIDE-lite)

Trust boundary: the **audit-log file** (local, the operator's own committed history) and the **evaluator**
(local on-device, or cloud behind the guard). **Tampering** — a hand-edited `signals` object could inflate
a local score; accepted, as it is self-tampering of a local, non-comparable score that never leaves the
device (spec: on-device, no identifiable human). **Information disclosure** — the only egress path is the
cloud evaluator, **off by default** behind the egress guard; the local heuristic sends nothing. **Elevation
/ Spoofing** — none (pure local read + local score). Disposition: the egress guard is the load-bearing
control; local tampering is a consciously-accepted, contained residual risk.

## Privacy analysis (LINDDUN-lite)

`AuditSignals` are integers/booleans (no personal data); the audit entry's goal/done text is the operator's
own, local. **Identifiability** — operatorId stays the opaque session id. **Disclosure** — no new egress; the
cloud judge remains gated. No new personal-data flow.

## Telemetry design

The reader degrades to "not recorded" (null / conservative default) for any absent signal — never a wrong
value (IO8). The import count is the operator-visible measure. No new spans/error-codes/HTTP.

## Test plan (Testing Strategy triggers)

- **D0** on every test.
- **D1 (unit + mutation)** — explicit signals used over defaults; **absent stays conservative**
  (mutation: `AcceptanceMet ?? true` → the no-signals test reds — the fabrication guard); a fully-instrumented
  episode scores all four deterministic dims (not just Focus).
- **Parsing** — the `signals` object is read; an absent object leaves `Signals` null; an absent field stays null.
- **D4 (real-infra)** — through a real SQLite host: a qualified **local** evaluator folds the advisory dims;
  the default (no evaluator) leaves them excluded — proving the seam threads them through.

## Residual risk

The **writer half** (a harness emitting `signals`) does not exist yet, so today's imported entries still
score conservatively (Partial) — the reader is ready and dormant, which is the honest state, not a defect.
The advisory fold needs **calibration data** (human-labelled episodes) to qualify any evaluator; until that
exists, both the local and cloud judges stay excluded by ADR-0019. The cloud judge additionally needs an
operator egress opt-in + credentials.
