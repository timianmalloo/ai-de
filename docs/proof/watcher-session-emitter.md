---
id: proof-watcher-session-emitter
title: "Proof Pack - Session Coordination Emitter (conn-8)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, proof-pack, emitter, coordination, conn-8, phase-2]
links:
  - { to: design-watcher-session-emitter, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Proof Pack for conn-8: the auto-emitting session wrapper (SessionCoordinationEmitter + Reconcile) and
  its shell wiring, including the DC-064 session-end-to-Ended fix. 9 emitter tests, 2 mutation-verified;
  Core 979/0, App 139/0.
---

# Proof Pack — Session Coordination Emitter (conn-8)

| # | Claim | Evidence (test) | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| 1 | An identity maps onto the register attributes | `ToAttributes_MapsTheIdentity` | wrong/missing key mapping | n/a (additive) | Verified | — |
| 2 | An absent harness/model is omitted (US-13) | `ToAttributes_OmitsAbsentHarnessAndModel` | emitting a null key | n/a | Verified | — |
| 3 | Register+pump makes the session live in the store | `Register_ThenPump_SessionAppearsLiveInTheStore` | pump/store not folding the register | seen | Verified | — |
| 4 | `CreateEmitter` writes to the host coord dir | `CreateEmitter_UsesTheHostsCoordDirectory` | emitter bound to wrong dir | seen | Verified | — |
| 5 | Register is idempotent (re-seen id heartbeats) | `Register_IsIdempotent_OneSessionAfterPump` | second register duplicating/throwing | seen | Verified | — |
| 6 | `HeartbeatAll` keeps all tracked sessions alive | `HeartbeatAll_KeepsEveryLiveSessionAlive` | heartbeat-all missing a session | seen | Verified | — |
| 7 | **End→pump marks the session Ended (DC-064)** | `End_WritesSessionEnd_AndStopsTracking` | session-end not marking the store | **mutation-verified** (neutralise `EndSession` → red) | Verified | — |
| 8 | Heartbeat for an unknown session is a no-op | `Heartbeat_ForAnUnknownSession_IsANoOp` | creating a phantom session | seen | Verified | — |
| 9 | **Reconcile registers/heartbeats/ends from a snapshot** | `Reconcile_Registers_Heartbeats_AndEnds_FromASnapshot` | closed session not ended; survivor not kept | **mutation-verified** (neutralise "end gone" → red) | Verified | async loop timing untested |
| 10 | No regression | Core 979/0, App 139/0 (full suites) | any broken contract | n/a | Verified | — |

## Mutation log

- Neutralised `_host.EndSession(...)` in `InjectedContractIngest.ContractSessionEnd` →
  `End_WritesSessionEnd_AndStopsTracking` **failed** (liveness stayed Alive). Reverted.
- Neutralised the `gone` computation in `SessionCoordinationEmitter.Reconcile` (`gone = []`) →
  `Reconcile_Registers_Heartbeats_AndEnds_FromASnapshot` **failed** (closed session not ended).
  Reverted.

## Gates

- Build: Core clean, App clean (0 warnings, `TreatWarningsAsErrors=true`).
- `docs-graph.py derive` + `validate`: 0 defects, 0 orphans.
- `verify-defect-register.py`: OK (DC-064 added; header count controlled 20).

## Residual risk

Ends are snapshot-driven (≤2s latency); host dispose drops tracked sessions without an explicit end
(they go Stale). The async shell loop timing is not unit-tested — covered by the Core end-to-end
reconcile test + manual smoke.
