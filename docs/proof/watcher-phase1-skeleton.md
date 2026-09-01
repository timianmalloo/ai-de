---
id: proof-watcher-phase1-skeleton
title: "Proof Pack - Loomkeeper Phase-1 Walking Skeleton"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, proof-pack, phase-1]
links:
  - { to: design-watcher-phase1-skeleton, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper Phase-1 deterministic core (identity + Trusted Registrar, idempotent
  span ingest, monotonic liveness, default-deny egress) and its durable SQLite store meet their design
  contracts: 41 xUnit tests green (30 core + 11 SQLite), with red observed on the forgery, dedup, and
  append-only oracles by mutation.
---

# Proof Pack: Loomkeeper Phase-1 Walking Skeleton

- **Component:** `src/AiDe.Core/Watcher/` (deterministic observation core)
- **Tests:** `tests/AiDe.Core.Tests/Watcher/` — 41 tests, **Passed 41 / Failed 0** (`dotnet test`, net10.0; full `AiDe.Core.Tests` suite 721/0). Build clean: 0 warnings, 0 errors (`TreatWarningsAsErrors=true`).

| Claim | Evidence (test) | Source | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| A process cannot act as a session without its capability | `TrustedRegistrarTests.Verify_CapabilityFromAnotherSession_IsRejectedAsForgery`; `Heartbeat_WithForgedCapability_ThrowsForgery` (LK-0001) | `TrustedRegistrar.cs:Verify/RequireCapability` | A wrong/other capability must return false / throw; would fail if `Verify` did not compare tokens | **Yes** — forged-capability test first failed on a test-double collision (DC-061), then fixed to an explicit never-issued token | Verified | Capability is process-lifetime only (persistence deferred to SQLite store) |
| Forged capability into ingest stores nothing | `SpanIngestTests.Ingest_ForgedCapability_Rejected_AndNothingStored` | `SpanIngest.cs:Ingest` | Returns `Rejected` only if verification precedes append | Yes (as above) | Verified | — |
| A restart cannot inherit the prior generation's authority | `RegisterNextGeneration_IncrementsGeneration_AndInvalidatesPriorCapability`; `RegisterNextGeneration_ClearsEndedState` | `TrustedRegistrar.cs:RegisterNextGeneration/Issue` | Old capability must stop verifying; generation must increment | Seen green; mutation-adjacent to the forgery oracle | Verified | — |
| Span ingest is idempotent under duplicate/out-of-order delivery | `Ingest_SameSpanTwice_SecondIsDuplicateIgnored`; `Ingest_DistinctSpansInAnyOrder_BothAccepted`; `Ingest_ConcurrentDuplicates_AppendExactlyOnce` | `ObservedSpan.cs:ComputeId`, `WatcherObservationStore.cs:TryAppendSpan` | Duplicate must return `DuplicateIgnored` and count stays 1 | **Yes** — mutation (dedup forced to always-append) turned `Ingest_SameSpanTwice` red, then reverted | Verified | In-memory store unbounded (`simplify:`; SQLite store bounds it) |
| Span id is content-addressed and collision-free per field | `ObservedSpan_SameInputs_YieldSameId`; `ObservedSpan_AnyDifferingField_YieldsDifferentId` (Theory) | `ObservedSpan.cs:ComputeId` (SHA256 canonical) | Any differing field must change the id | Seen green | Verified | — |
| Liveness cannot be flipped by a wall-clock change | `Evaluate_WithoutMonotonicAdvance_StaysAlive_EvenIfWallClockWouldJump`; `Evaluate_AfterStaleThreshold_IsStale` | `LivenessProjection.cs:Evaluate` (monotonic clock) | Uses only injected monotonic ticks; would fail if it read the wall clock | Seen green | Verified | — |
| Nothing egresses without an explicit per-path opt-in | `EgressGateTests.Decide_ByDefault_IsBlocked`; `Decide_AfterOptIn_IsAllowedForThatPathOnly`; `Decide_AfterRevoke_ReturnsToBlocked` | `EgressGate.cs:Decide/OptIn/Revoke` | Default must be `Blocked`; opt-in enables one path only | Seen green | Verified | Process-level outbound denial (S3) is the enforcement beneath this gate and is still Flagged |
| The slice composes end-to-end through the real store | `WatcherCompositionTests.RegisterObserveHeartbeatEvaluate_ComposesEndToEnd` | all Watcher types + `InMemoryWatcherObservationStore` | Drives register→observe→heartbeat→evaluate and the Alive→Stale transition | Seen green | Verified | UI treegrid row and daemon wire are the remaining Phase-1 slice |
| Observations persist across a restart | `SqliteWatcherObservationStoreTests.Spans_PersistAcrossReopen`; `FullCore_ComposesOverSqlite_AndSurvivesReopen` | `SqliteWatcherObservationStore.cs` (real SQLite file, reopen) | A reopened DB must still count the spans and recover the session; would fail if nothing was persisted | Seen green (D4 real engine, not a fake) | Verified | Not yet wired into the daemon |
| The span fact is append-only (DM11) | `ObservedSpanFact_IsAppendOnly_UpdateIsRejected`; `_DeleteIsRejected` | `SqliteWatcherObservationStore.cs` BEFORE UPDATE/DELETE triggers | A raw UPDATE/DELETE must raise `SqliteException('append-only')` | **Yes** — mutation (trigger neutered to `SELECT 1`) turned the update-rejection test red, then reverted | Verified | — |
| The SQLite store satisfies the same seam contract as in-memory | `SqliteWatcherObservationStoreTests` (dedup, heartbeat, session round-trip incl. null harness/model, ended) | `SqliteWatcherObservationStore.cs` | Fidelity: the contract tests pass on the real engine, not only the fake (D7) | Seen green | Verified | — |

**Boundary set covered:** empty/invalid binding (LK-0002), unknown harness/model (Not Recorded), duplicate span, out-of-order span, concurrent duplicate, forged/wrong-session/unknown-session capability, heartbeat expiry, wall-clock independence, ended + unknown session, egress default-deny and single-path opt-in.

**Mutation sense:** three load-bearing oracles were confirmed to fail on wrong code — forgery rejection (observed on the first run), span dedup (deliberate mutation, reverted), and the append-only trigger (trigger neutered to `SELECT 1`, update-rejection test went red, reverted).

**Not built this slice (residual):** the daemon ingest wire (needs spike S1) and the WPF Sessions-treegrid row — the remaining Phase-1 tasks. The SQLite store persists but is not yet wired into the daemon.
