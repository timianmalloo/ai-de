---
id: pp-p24-recoveryB-author-b0d0
title: P2.4B runtime recovery author checkpoint
type: proof
status: draft
owner: core
review-by: 2026-09-23
tags: [coordination, recovery, sqlite]
links:
  - relates-to: docs/design/cross-harness-coordination.md
  - relates-to: docs/plans/cross-harness-coordination-phases.md
summary: The original two runtime recovery failures now pass, with bounded pass work, reference overflow and paired current receipts. This is a partial author checkpoint, not finite-B approval or activation.
---

# P2.4B runtime recovery: partial author checkpoint

Base: `f12e8b86b5dfc1341a4526fdf0540af162ff599b`.
The required design/ADR ERRATUM was committed **before code** as `f2ad827e`.
Data's earlier boolean description was wrong: eligibility is a nonnegative
INTEGER counter; payload presence alone is 0/1.

## Executed evidence

All TRXs below are under `docs/proofs/p24-recovery-evidence/`; old evidence was
preserved. `recoveryB-pins.json` records exact source/test/project/binary hashes.

| Claim | Evidence / oracle | Red observed | Confidence / residual risk |
|---|---|---|---|
| Late same-repository parent applies child exactly once at original admission | `Pump_LateSameRepositoryParent_AppliesChildOnceWithOriginalAdmission`: admission 3, pending 0, board count 2, unchanged native message on replay | `recoveryB-baseline-red.trx`, original test failed | Verified on native fixture; not canonical-stream qualification |
| 1,025 children remain admitted and eventually apply after parent | Original capacity test retains all admission/count/original-ID/application assertions; final active pending 0, retained pending bytes 0 after 20 turns | Baseline original capacity test failed with 1,025 pending | Verified sample; near-byte-limit payload fixture still missing |
| Pass attempt ceiling survives restart | `Pump_MoreThan64ReadyChildren_RestartRetainsProgressAndPassBudgets`: fresh store/pump each turn, 64/64/2/0/0/0 attempts and examinations, all 130 apply | Added after implementation; no dedicated budget mutant yet | Executed, not red-proven; cross-source fairness remains unproven |
| Exhaustion releases both payloads; later parent advances eligibility once | Eight recovery failures, presence 0, generation 1; later applied child generation 2, attempt 1 | First run failed rehydration because the activation omitted its existing session snapshot; corrected | Verified recovery counter, **not** combined initial-admission-plus-recovery budget |
| Current tuple cannot commit unfinished staging or half payloads | New `CoordinationRecoveryBoundaryTests`: generation 2 requires finalization; second staging and eight invalid domains/payload cases reject; poll cannot reset count | No dedicated old-schema mutant for the newly added boundaries | Executed, red-proof incomplete |
| Existing result occurrences remain passing | `recoveryB-union.trx`: 488 passed, 0 failed/skipped; original 471 sorted name multiset difference zero | Baseline preserved in prior A receipts | Verified selection, not entire Core suite |
| Predecessor schema mutation harness remains meaningful | `recoveryB-schema-mutants.trx`: 23 fail / 50 pass; 21 lost-refusal assertions, one indexed-lookup assertion and one generation CHECK; zero mutation-anchor failures | All 23 fault injections executed | Verified harness; not a mutation score for new recovery logic |

`recoveryB-first-runtime.trx` records the intermediate remaining capacity failure:
the exhausted cursor did not advance to its frozen high-water when its final
remaining row had already become terminal. `recoveryB-coordination.trx` records
the rehydration session-snapshot failure and old raw-SQL fixture assumptions.
Neither failed run is presented as a gate pass.

## Change reach and instrumentation

* `SqliteWatcherObservationStore.Coordination.cs` writes fresh-v8 event/feed tuple
  constraints, indexed retained-capacity rules and fixed lane checkpoint columns.
* `CoordinationProjection.cs` writes source-reference deferred admissions and
  atomically finalizes terminal transitions. The actual-session repository guard
  still runs inside the IMMEDIATE transaction.
* `CoordinationRecovery.cs` reads event dependency/generation/due/reference fields,
  reuses the already validated root capture, performs original-registration and
  same-repository board-ID point lookups, and calls the existing native SQL writer.
  Feed insertion stages only the current pointer; one guarded update settles the tuple.
* `CoordContractLogPump.PumpOnce` invokes recovery once for the whole pass, never
  once per file/page, then publishes committed observations through existing ingest.
* `LastRecovery` and the recovery Activity emit examinations, attempts and applied
  results on successful passes. Existing pump telemetry emits elapsed time,
  capture bytes and diagnostics. Tests read actual counters, not elapsed estimates.
* Feed A remains a historical receipt reader. No public recovery-health field or
  fabricated healthy/zero value was added. Producer/emitter code is untouched.

Ordinary retained capacity is SQL-guarded at 1,023 / 16,646,144 bytes; reference
activation is SQL-guarded at absolute 1,024 / 16,777,216. Recovery uses three
protected lanes before borrowing; candidates advance persistent keyset positions,
and exhausted traversals wrap next pass. This describes implementation, not full
fairness/performance proof. The captured source records already held by the pump
are reused, not reread per retry. There is no second durable pending queue.

## Corrections and class controls

* **Cursor completeness:** a traversal ending below a frozen bound because the
  bound's row became terminal could fail to wrap. Sweep: all three shared lanes.
  Derive: common end-of-lane advancement. Control: unchanged 1,025-child oracle,
  observed failing before the fix and passing afterward.
* **Paired snapshot consistency:** rehydration lost an existing session snapshot.
  Sweep: pending activation and eligibility-only transitions. Derive: reuse
  original scoped registration mapping. Control: eight-failures/later-parent
  runtime test, observed failing before correction.
* Old raw-SQL schema fixtures depended on automatic state/payload finalization.
  They now stage and settle explicitly, preserving their original oracles. The
  isolated payload-clause mutant disables the independent paired-payload CHECK
  only inside its deliberate disposable-database mutant transaction, then rolls
  back. Production guards are not weakened.

Central lesson/index integration stays with the conductor under this author's
explicit no-lessons/no-site scope. This note captures the corrections but does
not claim that central integration happened.

## Remaining finite-B obligations — BLOCK independent code approval

1. **Attempt semantics:** initial admission's `ApplyObservation` call is not
   charged to `current_attempt`; the current eight count is eight *recovery*
   attempts, not the specified eight total semantic attempts. Correct with a
   red-first combined-admission/recovery oracle.
2. Add exact cross-source fairness/blocked-probe/restart and SQL query-plan
   bounds. Current counters count selected candidate metadata, **not** SQLite
   VM examinations. No bounded VM-work or unconditional SLA claim.
3. Complete eight-budget flapping/disappearance, overflow atomic refusal,
   byte-cap+1, old-native-Seq parent, reserved rollback, recovery-specific
   lost-ACK and reference-rehydration GAP runtime oracles.
4. Harden/check logical cursor numeric domains and overflow. The unused
   `recovery_turn` column was removed rather than left as a second unused clock.
5. Verify failure-path recovery counters: current LastRecovery is replaced
   only on successful return, so partial-pass work is not a measured result.
   Existing typed pump failure is preserved; do not interpret the reset counters
   on an exceptional call as completed zero work.
6. Complete pending lifecycle-event recovery semantics without replaying live
   registration, heartbeat or end refresh. Those kinds are intentionally not
   executed by the new recovery loop; this is not a full obligation-liveness proof.
7. Complete paired exhaustion/tombstone history qualification and optional
   cross-stream projection-reference snapshots. Same-repository legacy parent
   lookup is implemented; no new cross-key value enters the same-scope FK.
8. Independent Data/DS/Test code review and new recovery mutation coverage
   remain open. The author cannot clear those gates.

Production authority is **DENY**; enhanced canonical append remains disabled.
No existing candidate-v8 upgrade, canonical bridge, producer change, old-binary
rollback, P2 completion, P3–P5, live database or activation is claimed.

## Closing assessment

The requested priority—actual runtime two-RED→GREEN rather than schema-only
work—was reached. The full finite-B contract was **not** completed inside the
bounded author run. No new implementation track is opened at close.
Retain this assigned worktree for independent review; release edit claims and
end the registered session. The next action is finite-B counter/boundary closure
followed by independent Data/DS/Test review, not production integration.
