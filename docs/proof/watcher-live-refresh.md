---
id: proof-watcher-live-refresh
title: "Proof Pack - Live Pane Auto-Refresh (conn-9)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, proof-pack, refresh, conn-9, phase-2]
links:
  - { to: design-watcher-live-refresh, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Proof Pack for conn-9: the watcher panes re-render on a store change, gated by a fingerprint whose
  liveness-state term catches an Ended transition with an unchanged session count. App 140/0.
---

# Proof Pack — Live Pane Auto-Refresh (conn-9)

| # | Claim | Evidence (test) | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| 1 | A new session flips the change signal | `Fingerprint_Changes_OnALivenessTransition_NotOnlyOnCount` (empty≠twoAlive) | fingerprint ignoring session count | seen | Verified | — |
| 2 | **An Ended transition flips the signal with an unchanged count** | same test (twoAlive≠oneEnded, count==2) | a count-only fingerprint | **mutation-verified** (drop the liveness-state term → red) | Verified | — |
| 3 | Refresh touches only the watcher pane kinds; terminals reconciled | design/DC-029 + shared `WatcherPaneKinds` set | invalidating a terminal | n/a (structural) | Inferred | render marshalling not unit-tested |
| 4 | No regression | App 140/0, Core 979/0 | any broken contract | n/a | Verified | — |

## Mutation log

- Replaced the per-session liveness term in `WatcherFingerprint` with the session id alone (dropping
  `=(int)Liveness.Evaluate(...)`) → `Fingerprint_Changes_OnALivenessTransition_NotOnlyOnCount` **failed**
  (twoAlive == oneEnded). Reverted.

## Gates

- Build: App clean (0 warnings, `TreatWarningsAsErrors=true`).
- `docs-graph.py derive` + `validate`: 0 defects, 0 orphans.

## Residual risk

The async loop cadence (2s) and the `BeginInvoke` render marshalling are not unit-tested — covered by
the pure fingerprint change-detection test (mutation-verified) + manual smoke. A pure reorder of
`AllSessions()` with no state change would cause one harmless spurious refresh.
