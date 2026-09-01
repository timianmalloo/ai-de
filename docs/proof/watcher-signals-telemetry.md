---
id: proof-watcher-signals-telemetry
title: "Proof Pack - Signals Telemetry + Advisory Seam (t3/t4)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, proof-pack, signals, telemetry, advisory, t3, t4, phase-2]
links:
  - { to: design-watcher-signals-telemetry, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Proof Pack for t3/t4: an optional audit `signals` object is read into the deterministic signals with a
  conservative fallback for every absent field (fabrication guard mutation-verified); a fully-instrumented
  episode scores all four deterministic dimensions; and the advisory-evaluator seam folds the advisory dims
  through the host when a qualified local (on-device) evaluator is supplied. Core 1208/0.
---

# Proof Pack — Signals Telemetry + Advisory Seam (t3/t4)

| # | Claim | Evidence (test) | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| 1 | Explicit signals are used over the conservative defaults | `Derive_WithExplicitSignals_UsesThem_OverTheConservativeDefaults` | ignoring the signals | seen | Verified | — |
| 2 | A fully-instrumented episode scores all four deterministic dimensions | `FullyInstrumentedEpisode_ScoresEveryDeterministicDimension_NotJustFocus` | signals not lifting the dims | seen | Verified | Partial until advisory folds (t4) |
| 3 | **Absent signals stay conservative (no fabrication)** | `Derive_WithNoSignals_IsUnchanged_Conservative` | fabricating a default | **mutation-verified** (`AcceptanceMet ?? true` → red) | Verified | — |
| 4 | The `signals` object is parsed; absent object/field → null | `ParseWithEvidence_ReadsTheOptionalSignalsObject`, `ParseWithEvidence_NoSignalsObject_LeavesSignalsNull` | mis-parsing | seen | Verified | — |
| 5 | **The advisory seam folds the advisory dims with a qualified local evaluator; excluded by default** | `ImportAndScore_WithAQualifiedLocalEvaluator_FoldsTheAdvisoryDimensions` (D4, real SQLite) | seam not threading evaluator/registry | seen | Verified | needs calibration to qualify |
| 6 | No regression | Core 1208/0 | any broken contract | n/a | Verified | — |

## Mutation log

- `DeterministicSignalsDeriver`: `AcceptanceCriteriaMet: s?.AcceptanceMet` → `?? true` (fabricating
  acceptance when the turn recorded none) → `Derive_WithNoSignals_IsUnchanged_Conservative` **failed**.
  Reverted. This is the honesty guard: an absent signal never becomes a value.

## Gates

- Build: Core clean (0 warnings, `TreatWarningsAsErrors=true`); App unaffected (host signature change is
  source-compatible via optional parameters; the shell calls the default overload).
- `docs-graph.py derive` + `validate`: 0 defects, 0 orphans.

## Residual risk

The writer half (a harness emitting the `signals` object) is future AI-Forward work, so today's imported
entries still score conservatively — the reader is ready and dormant (the honest state). The advisory fold
needs calibration data (human-labelled episodes) to qualify any evaluator; the cloud judge additionally
needs an operator egress opt-in + credentials. The seam is complete and proven; activation is gated on
those external inputs, not on further code.
