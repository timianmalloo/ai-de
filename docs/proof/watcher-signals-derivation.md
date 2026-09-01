---
id: proof-watcher-signals-derivation
title: "Proof Pack - Deterministic Signals Derivation + Auto-Score (conn-10)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, proof-pack, signals, scoring, conn-10, phase-2]
links:
  - { to: design-watcher-signals-derivation, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Proof Pack for conn-10: DeterministicSignalsDeriver derives honest signals (proof pack -> verification
  path; acceptance null) and WatcherHost auto-scores imported episodes - a proof-pack episode scores an
  honest Partial, one without is Not-Scored. HasVerificationPath mutation-verified. Core 990/0, App 140/0.
---

# Proof Pack — Deterministic Signals Derivation + Auto-Score (conn-10)

| # | Claim | Evidence (test) | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| 1 | A committed Proof Pack sets the verification path; acceptance stays null | `Derive_WithProofPack_SetsTheVerificationPath_ButNeverFabricatesAcceptance` | fabricating acceptance/verification | seen | Verified | — |
| 2 | **No proof pack → no verification path** | `Derive_WithoutProofPack_HasNoVerificationPath` | verification set unconditionally | seen | Verified | — |
| 3 | **A proof-pack episode scores an honest Partial (no floor)** | `ProofPackEpisode_ScoresPartial_NotNotScored_AndTripsNoFloor` | acceptance fabricated (would score/blocked); verification missing (would Not-Score) | **mutation-verified** (verified=true → the no-proof test reds; see mutation log) | Verified | only Focus scores today → thin |
| 4 | **A no-proof episode is Not-Scored, not Blocked** | `NoProofPackEpisode_IsNotScored_NotBlocked` | verification fabricated true | **mutation-verified** (verified=true → red) | Verified | — |
| 5 | Host import auto-scores; operatorId is the session id; re-run upserts | `ImportAndScore_ProofPackEntryPartial_NoProofNotScored_ReRunUpserts` (D4, real SQLite) | wrong verdict per entry; duplicate on re-run; a human operator id | seen | Verified | — |
| 6 | No regression | Core 990/0, App 140/0 | any broken contract | n/a | Verified | — |

## Mutation log

- `DeterministicSignalsDeriver`: forced `verified = true` (fabricating a verification path) →
  `NoProofPackEpisode_IsNotScored_NotBlocked` **failed** (a no-proof episode wrongly became scoreable).
  Reverted. This is the honesty guard: an episode is scoreable only on real committed evidence.

## Gates

- Build: Core + App clean (0 warnings, `TreatWarningsAsErrors=true`).
- `docs-graph.py derive` + `validate`: 0 defects, 0 orphans.

## Residual risk

Only FocusAndTermination is deterministically scorable today (acceptance/guidance/coordination are not
observable from an audit entry), so every scored imported episode is a **Partial** — honest but thin.
Proof-pack presence is a coarse verification signal (it does not read the proof pack's contents); a
committed-but-empty proof pack still sets HasVerificationPath — accepted (the artifact's existence is the
operator's own committed claim). Richer dimensions need telemetry conventions that do not exist yet.
