---
id: design-cross-harness-native-producer
title: Cross-harness native producer — P2.P1 membership
type: design
status: draft
owner: "@timianmalloo"
phase: P2
tags: [coordination, native-producer, concurrency]
links:
  - {to: design-cross-harness-coordination, rel: refines}
  - {to: proof-cross-harness-coordination, rel: tested-by}
review-by: 2026-10-16
summary: Records the accepted P2.P1 emitter membership contract before implementation. Writer admission bounds, prepared writes, registration notices and Data recovery remain unqualified.
---

# P2.P1 — accepted membership contract

The current-task DS code admission authorizes only
`src/AiDe.Core/Watcher/SessionCoordinationEmitter.cs`. This is not approval of the
whole native producer or P2. The canonical design and Proof Pack remain the parent
conductor's responsibility.

## Grounding and scope

Baseline: `54ec0aae745b8f645b54ea631a2d97b4253e56f8`.
`CoordinationContractLog.cs` defines the sealed `CoordContractWriter`: `Append`
calls the supplied `TimeProvider`, determines sequence, serializes and appends.
`SessionCoordinationEmitter.cs` currently changes `_live` before registration/end
writes, and `HeartbeatAll` writes directly after its snapshot.

The bounded context is native session liveness. A session identity identifies a
live membership; membership is a process-local set, not durable recovery state.
One log record is one register, heartbeat or end transition. The existing wire
shape, writer, identity attributes and public void signatures do not change.
There is no new durable schema or measure.

## Invariant and algorithm

For the same session, Register, Heartbeat and End serialize across their membership
check, writer call and successful membership update. Register adds membership only
after WriteRegister returns; End removes membership only after WriteSessionEnd
returns. A thrown Register leaves LiveCount zero; a thrown End leaves it one and
does not discard the successful registration. Repeated successful calls retain
their existing idempotence. Register after a successful End may start another
lifecycle, as before.

Use a reference-counted per-session gate. A short emitter-wide lock protects the
live set, gate lookup, reference acquisition and reference release. References
include owners and waiters: removal at zero cannot replace a gate while another
operation still uses it. The emitter-wide lock is never held while waiting on a
session gate or doing writer I/O. A finally block releases references after both
successful and failed operations. Entries are reclaimed even while membership
remains live; this gate table is not an immortal history of session IDs.

HeartbeatAll snapshots the live set under the short lock, then calls the guarded
Heartbeat operation for each ID. A snapshot taken before End is not authorization
to emit after End. Reconcile already calls Register/Heartbeat/End and retains its
snapshot semantics; no atomic multi-session reconcile is promised.

Rejected: moving writes under the existing emitter-wide lock (blocks independent
sessions); changing membership first and rolling back later (publishes false
membership and permits conflicting operations); an immortal keyed-lock dictionary.

## Change reach and measurable questions

Surface list: existing writer return/exception → emitter membership and guarded
transitions → LiveCount/Reconcile/HeartbeatAll → existing JSONL reader. No reader,
projection, store or UI shape changes. Existing records expose kind/session/seq;
LiveCount exposes membership. Tests read both bytes and count, not exit status.
There is no new logging or performance claim.

## Proof plan

The fixed finite worklist is design, regression tests, implementation, disconfirmation,
commit. All depend on the previous result; no fan-out is admitted. Inferred work and
span are equal (one author); parallel speedup ceiling is 1. No timing estimate is
claimed. Main-line budget is 26 calls; reaching it is a reporting checkpoint, not
permission to drop a gate. The parent owns independent review and canonical proof.

Testing Strategy union: D0, D1, D4 and D6. Use real writer/files and synthetic
identities. FileStream sharing denial proves a pre-write refusal, not the red
oracle itself; the missing final register/end and wrong failure membership are
the regression oracles. Move Add/Remove before the write as a deliberate mutant;
the retained tests must kill both changes.

Use the writer's existing TimeProvider seam and bounded task/event synchronization
to pause inside real Append before file access. Concurrent independent-session
completion while that call is paused detects a global emitter lock across the
writer. This does not claim an OS-blocked file-write interleaving. Same-session
register/end and heartbeat/end overlaps must produce exactly one lifecycle with
no heartbeat after end. A stale HeartbeatAll snapshot must not bypass membership.
Reflection may observe the gate table under its lock to prove waiting/reference
reclamation; it does not replace durable wire assertions. Failed first registrations
and completed lifecycles must leave no gate entries.

Class → sweep → derive → prevent: premature process-local state publication can
outlive a failed durable write. In this emitter the siblings are Register and End;
Heartbeat and HeartbeatAll separately expose a check/write race. Derive transition
admission through one guarded operation and route all heartbeat snapshots through
it. Denial/retry and ordered concurrent wire tests are the controls. Canonical
defect-register updates are deferred to the parent-owned consolidation, not edited
under a competing lease here.

## Explicitly pending — not shippable as the full producer

- P2.P2 prepared-write admission and the proposed global 128 pending/prepared-state
  limit require the separate DS design; neither is implemented here.
- Complete encoded lines over 65,536 bytes, root file count 128 and root bytes
  32 MiB remain writer obligations. The two oversize encodings and two root-bound
  tests remain four expected RED cases.
- The live membership set and registration-notice path remain unbounded floors.
  Gate reclamation does not qualify either.
- Registration notices, Data recovery, uncertain/partial writes and restart
  semantics remain **unqualified**. Retry exactness is claimed only after the
  proven pre-write sharing denial.
- No global producer PASS, Data/DS recovery PASS, source reliability PASS or
  ship-readiness claim follows from these local membership tests.
