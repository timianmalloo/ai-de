---
id: proof-watcher-dispute-command
title: "Proof Pack - Raise-Dispute Command (conn-11)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, proof-pack, dispute, command, conn-11, phase-2]
links:
  - { to: design-watcher-dispute-command, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Proof Pack for conn-11: a keyboard-reachable command raises an append-only operator dispute against the
  latest genuinely-scored episode (score unchanged, local operator id); a Not-Scored card is not disputable
  (mutation-verified). Core 990/0, App 143/0.
---

# Proof Pack — Raise-Dispute Command (conn-11)

| # | Claim | Evidence (test) | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| 1 | Disputing the latest scored episode appends a dispute under a local operator id (not a human) | `RaiseDisputeOnLatest_AppendsADispute_AgainstTheScoredEpisode_UnderALocalOperator` | not appending; leaking a human id | seen | Verified | — |
| 2 | **A Not-Scored card is not disputable** | `RaiseDisputeOnLatest_ANotScoredCard_IsNotDisputable` | disputing a card with no number | **mutation-verified** (drop the Not-Scored filter → red) | Verified | — |
| 3 | An empty store yields an honest message, not a throw | `RaiseDisputeOnLatest_NothingScored_YieldsAnHonestMessage_NotAThrow` | throwing on no scored episode | seen | Verified | — |
| 4 | The command is wired into palette + menu (no drift) | `MainMenuTests.TheMenuCoversEveryCatalogCommand` + `Phase3SurfacingTests.DeclaredMenusMatchWhatTheBuilderRenders` | catalog/menu drift (DC-066) | seen (both reded before the builder + count were updated) | Verified | menu is a hand-maintained parallel list (DC-066) |
| 5 | No regression | Core 990/0, App 143/0 | any broken contract | n/a | Verified | — |

## Mutation log

- `RaiseDisputeOnLatest`: neutralised the Not-Scored filter (`|| true`) →
  `RaiseDisputeOnLatest_ANotScoredCard_IsNotDisputable` **failed** (a Not-Scored card wrongly became
  disputable). Reverted.

## Gates

- Build: Core + App clean (0 warnings, `TreatWarningsAsErrors=true`).
- `docs-graph.py derive` + `validate`: 0 defects, 0 orphans.
- `verify-defect-register.py`: OK (DC-066 added — the menu-drift class the conformance tests already control).

## Residual risk

The command disputes the *latest* scored episode with a default reason — no per-episode selection UI or
reason prompt yet. The real cloud judge is documented (design) but not wired: it is deferred behind an
operator egress opt-in, credentials, and the evaluator passing calibration (ADR-0019). Until then only the
deterministic Weave is recorded — today's honest behaviour.
