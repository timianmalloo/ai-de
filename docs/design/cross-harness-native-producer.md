---
id: design-cross-harness-native-producer
title: Cross-harness native producer — P2.P1 membership and P2.P2A writer
type: design
status: draft
owner: "@timianmalloo"
phase: P2
tags: [coordination, native-producer, concurrency]
links:
  - {to: design-cross-harness-coordination, rel: refines}
  - {to: proof-cross-harness-coordination, rel: tested-by}
review-by: 2026-10-16
summary: Records the accepted membership and participating prepared-writer contracts. Global emitter state, registration notices, durable Data recovery and full P2 qualification remain separate pending work.
---

# P2.P1 — accepted membership contract

## P2.P2A — participating writer contract (recorded before code)

The current task explicitly admits the writer portion of
`CoordinationContractLog.cs`, a small `CoordinationNativeWrite.cs` helper and
synthetic Core tests. Parent P2.P1 review reference:
`184906558911f3f49115d44781781aa094adfa47`; local starting source:
`ff73f62c071aaf16bd379b494d27df372ff2a7b5`. This section supersedes the earlier
writer-pending statements only for this bounded implementation unit, not full P2.

The native append stream remains the durable record: one complete encoded JSON
line per event, retaining the existing session/sequence key and filenames.
PreparedWrite is a process-local immutable value carrying bounded copied input,
captured time, source identity, expected sequence/start, prefix fingerprint and
exact append bytes. A private attempt marker distinguishes an actual attempted
append from an identical but never-attempted preparation. No new wire nonce,
acknowledgement database, pending file or accepted-log reservation is introduced.
Restart cannot reconstruct this marker from an invented new object: recovery
requires actual preparation and source evidence, otherwise remains uncertain.

`Prepare(input)` returns Ready(PreparedWrite), Refused(code), or Unavailable(code).
`Append(prepared)` returns Admitted(Admission), Refused, Unavailable, or
Uncertain(code, prepared). Legacy void entry points remain; unsuccessful results
throw IOException-derived CoordinationWriteException with Code and optional
Prepared. The codes use `COORD_`: RECORD_BOUND, FILE_BOUND, ROOT_BOUND,
SEQUENCE_CONFLICT, STALE_PREPARATION and IDENTITY_CONFLICT are refusals;
WRITER_BUSY, SOURCE_UNAVAILABLE and ROOT_OVERRUN are unavailability only before
candidate append is attempted; possible writes yield WRITE_UNCERTAIN.

Input copying, bounded preflight and the caller's clock run outside the root
guard. In particular a paused TimeProvider must not block another session before
physical I/O. No arbitrary production callback executes under the root guard.
Under the same normalized root gate across writer instances, and an empty
FileShare.None `.coordlock` across participating processes, both operations
validate sequence, quota and target identity. Target sharing excludes other
writes. Append holds exclusion through write, Flush(true), and disposal;
success is returned only after disposal. Gates are reference-counted and
reclaimed, never keyed forever. Acquisition fails fast, without retry queues,
PID ownership, leases or manual lockfile deletion. The empty lockfile is not a
JSONL ledger and is excluded from quota.

The complete encoded line including LF is at most 65,536 bytes. Preflight and
output are bounded before giant allocation; encoder buffer reservation must not
reject a valid exactly-65,536-byte ASCII or escaped-Unicode line. Scan matching
top-level `*.jsonl` only, at most 129 entries, including empty files; allow at most
128 files and 33,554,432 root bytes including any separator repair. Unknown
truncated tails are refused, not completed automatically. A complete valid JSON
tail without LF may be separated only with quota-accounted repair.

Bounded sequence scanning preserves native nonempty-line counting and prevents
duplicate native (session, sequence) keys. It makes no clock-ordering guarantee.
Existing names remain unchanged, including the legacy four-byte digest for
rewritten unsafe IDs. Existing target content must establish the same exact
session identity; aliases and collisions are refused rather than renamed or
deduplicated. Root normalization reuses the existing native helper. No untested
symlink, UNC or cross-platform filesystem guarantee follows.

A never-attempted stale preparation is refused even if another writer filled its
range with identical bytes. An actually attempted preparation can reconcile only
after its failed handle closes and root plus target exclusion is reacquired:
unchanged prefix and complete exact range plus successful Flush(true) gives the
same Admission; unchanged start permits the same exact bytes; matching partial
prefix ending at EOF permits only its remaining suffix, with quota continuity.
Different bytes, truncation, extra bytes after a partial record or unresolved
flush remain Uncertain with no additional append. No silent rebase or new-ID
retry is permitted.

The quota guarantee covers participating writers only. Legacy writers do not
obey the root guard. A detected external root overrun is unavailable and preserves
history; this is not an old-client disablement or ownership claim.

### Reach, verification and execution bounds

Surfaces: copied input → bounded encoding → prepared source snapshot → exclusive
append/flush → legacy void wrappers → unchanged native parser → existing emitter
membership. No store, UI or wire extension is in this unit. Operator evidence is
the structured outcome/code, encoded byte count and operation duration, without
payload logging.

Finite graph: source grounding (Reasoning) → this contract commit (Deterministic
mechanics) → original four RED controls (Deterministic mechanics) → writer plus
adversarial fixtures (Reasoning) → tests/mutants and raw TRX (Deterministic
mechanics) → receipt commit (Deterministic mechanics). All edges are data or
decision edges. One author, no agents; inferred work equals span, speedup ceiling
one. Forty-five tool calls is the reporting checkpoint. Unresolved assertions
decrease toward zero; budget exhaustion reports the exact remaining floors and
does not grant acceptance. Independent review and canonical Proof Pack attachment
belong to the parent conductor, not the author.

Testing union: D0/D1/D2/D4/D6/D7. Keep the original four producer-bound controls
unchanged, prove both exact-limit encodings, prepare/replay identity, injected
partial-write/flush/dispose failures against real files, competing instances and
processes, root aliases, malformed tails and legacy parser compatibility.
Retain membership I/O-denial and three operation gate-retirement controls.

Class → sweep → derive → prevent: bounds checked independently of exclusive
append allow concurrent overflow; a reconstructed preparation borrowing another
intent's identical bytes loses an independent event. The writer's register,
heartbeat, end and general/board writes share one admission path. Its single
typed outcome and immutable bytes remove duplicate policy producers. Boundary,
staleness and physical fault controls are the prevention; their actual execution
and remaining gaps will be recorded in the receipt, not presumed here.

Still pending outside this unit: global 128 emitter prepared-state capacity,
RegistrationNotice capacity, durable Data recovery, emitter retention of Prepared
after unresolved IOException, canonical P2 consolidation, old-binary/mixed-writer
qualification and P3–P5. No activation or full-producer PASS is authorized.

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
