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

## P2.P2B — emitter corrective contract (recorded before code)

This unit implements the DS-admitted corrective contract supplied on 2026-09-16,
against `a855e1ad64258b47c08a378da3ec4a8c3ff2b62f`. Earlier pending statements below
are historical receipts, not a claim that the already admitted P2.P2A writer is
still absent. P0–P5 remain the programme goal; this is only the P2.P2B author lane.

The aggregate is one emitter-object identity plus one ordinal external session
id. Its invariant is one committed-membership flag and at most one pending
Register, Heartbeat or End. Phases are Preparing, AwaitingPreparation, Ready,
Appending and Uncertain. Register admission creates live membership; heartbeat
and end failures preserve live membership; only admitted End releases it.
The native append log is still the durable truth. There is no new persisted
shape, status database, wire field, reconstructed preparation or restart claim.
Without the actual preparation, restart recovery is uncertain and requires the
separately governed canonical pull.

One production static SemaphoreSlim(128,128), acquired with Wait(0), bounds all
live/reserved/pending/uncertain states across roots and emitter instances. A
strong owner registry retains obligations; no GC eviction, TTL or Dispose reset
is allowed. A fixture-only internal scope may supply another fixed-128 budget.
Reserve before identity callbacks, input copying, clock or Prepare; session 129
returns COORD_EMITTER_CAPACITY without calling those paths or creating files.
The writer binding is immutable. Unknown heartbeat/end allocate no slot.
Retirement requires empty and quiescent state. Disposal reports obligations and
does not invent an End. Release is only admitted End or quiescent proven-no-write
Register abandonment; abandonment of heartbeat/end preserves live membership.

Retain the actual Ready PreparedWrite before Append. Never overwrite it with a
failure result's nullable Prepared. RetryPending takes no payload and reuses the
same object, bytes, timestamp, sequence, prefix and range. Known Prepare
unavailability without an object retains frozen bounded input and can prepare
again because no identity was assigned. Different effective input or operation
while pending is COORD_INPUT_CONFLICT, with no preparation or append. Generic
exceptions and uncertainty never constitute proof of no write. A retained
preparation is abandonable only if it was never attempted.

Frozen input is at most ten fixed identity attributes and 65,536 UTF-16 code
units in total, counting session, keys and values. Do not retain caller
dictionaries or factories. Ready state discards its frozen dictionary; equality
uses the prepared payload, not a cached CopyBytes allocation. Prepared metadata
is separately bounded at 65,536 UTF-16 units. Retention accounting includes
UTF-16 storage, metadata, hash and packet rather than claiming 128 packets as the
whole bound. Admission.ByteCount, including a repair LF, is checked at 65,536
before append. Constructor path normalization alone creates no directory.

Per-session gates retain P1 reference-counted lifetime, with at most one waiting
control call; a third call returns Busy, not another waiter. No arrival FIFO
promise is made. Waiting End suppresses new heartbeat work. The short owner
lock never covers callbacks, I/O or waiting. HeartbeatAllResultsAsync snapshots
at most 128 states and revalidates each; one batch per owner and one in-flight
work item per state. Dispatch all independently eligible items before joining,
so a paused clock cannot block a healthy source. No timer queue or general
scheduler is added. Cancellation stops scheduling/waiting, not synchronous I/O:
in-flight state and budget remain owned until the work actually finishes.
Legacy void HeartbeatAll joins context-independent work and aggregates all
non-success outcomes in an IOException-derived typed exception, not stop-first.
Its blocking bridge is not a UI-safe or unconditional-deadline claim.

Reach: identity/factory → bounded owner state → existing Prepare/Append →
membership/result → legacy individual and batch wrappers → unchanged native
parser. No App, live store, endpoints or runtime activation. Structured outcome,
code, membership, retained-state count and duration answer operator questions;
no raw input or absolute root enters logs.

Finite execution graph (all edges data/decision): ground contracts (Reasoning)
→ commit this contract (Deterministic mechanics) → baseline behavioral RED
(Deterministic mechanics) → bounded state and tests (Reasoning) → selected suite,
mutants, stdout/TRX and pins (Deterministic mechanics) → receipt/commit
(Deterministic mechanics). No delegates; inferred work equals span, width one,
parallel speedup ceiling one. Forty-five calls is the reporting checkpoint, not
permission to label missing floors complete. Test/DS/Security independent review
and canonical Proof Pack consolidation are the parent's next gates.

Testing union: D0/D1/D2/D4/D6/D7. Real files and existing writer fault seams cover
lost complete-write acknowledgement, prewrite retry, mutation/conflict, failed
heartbeat/end, unsafe abandonment/disposal, global cross-root live and reserved
capacity, input/metadata/repair bounds, bounded contention/gate retirement,
paused/failed alongside healthy batch progress and cancellation retention.
Existing 77-case evidence remains the regression floor. New APIs missing on the
baseline are not called semantic RED; baseline void counterexamples and targeted
restored-source mutants provide separate falsifying evidence.

Class → sweep → derive → prevent: dropping preparation after ambiguous I/O loses
operation identity; counting only one emitter's live set loses global capacity.
The three operation paths and Reconcile are the sibling sweep. One retained
operation executor and one production budget derive these rules once. Exact
replay, pre-callback capacity and unsafe-abandonment controls are the prevention.
Any incomplete floor is recorded in this receipt for parent consolidation, not
silently promoted into the canonical Proof Pack or site.

### P2.P2B author receipt — bounded partial, not a qualification PASS

Contract-first commit: `3889d326`. Production source is confined to
`SessionCoordinationEmitter.cs` and `CoordinationEmitterState.cs`; the P2.P2A writer,
wire shape, App and runtime activation are unchanged. This is executable author
work, not a replacement proposal. Independent Test/DS/Security review remains
required before the parent can grant any P2.P2B or whole-programme acceptance.

Observed evidence directory: `docs/proofs/p24-emitter-pending-evidence/`.
`baseline-red.trx` has two executed failures against the original void methods:
lost acknowledgement duplicated a complete register; a failed first heartbeat
prevented a healthy second heartbeat. `complete-selected.trx` has 91/91 PASS:
the previous 77-case selector plus 14 new logical cases. `targeted-mutants.trx`
has four executed failures: per-emitter instead of production-global capacity,
unsafe uncertain abandonment, replacing retained Prepared with nullable append
failure metadata, and increasing the UTF-16 bound. These are combined targeted
mutants, not a full mutation score. `final-restored.trx`, its complete child
stdout and SHA-256 pins record the rebuilt restored source separately. Failed
intermediate runs are retained as failures, never relabelled as proof.

Each run's `.pins.json` contains the exact command, logical TRX counters, elapsed
seconds and SHA-256 for coordination sources, tests, project and Core/test
binaries. `verify.py` uses the existing test runner without installing anything.
This directory is raw evidence, **not** the parent's canonical Proof Pack.

Implemented and exercised:

- A single static production budget owns a fixed-128 SemaphoreSlim and strongly
  retains emitter owners; an internal fixed-128 fixture budget cannot be supplied
  through a public constructor. Cross-root live capacity, refusal before
  Reconcile's identity factory, absence of the refused directory, and readmission
  after End are observed. The reservation test observes 127 live plus one paused
  registration, with an excess emitter unable to create its root.
- One retained operation, exact actual PreparedWrite reuse, frozen-input retry
  after preparation unavailability, ordinal input conflict and mutable-dictionary
  isolation. The real Activity exit seam holds the OS root lock after Prepare,
  producing Append's null-Prepared failure; the emitter still retains the original
  object and later obtains its exact Admission. A real complete write with lost
  acknowledgement remains uncertain until retry and cannot be abandoned/disposed.
- Failed heartbeat/end preparation preserves Live and its slot. Proven-no-write
  abandonment clears only the pending operation; admitted End releases capacity.
  Unknown heartbeat/end allocate nothing; unknown Retry reports canonical-pull
  recovery required instead of inventing a cold preparation.
- Input UTF-16 accounting, repair-LF packet refusal before append, and metadata/
  fingerprint accounting. Ready state does not retain the redundant frozen
  dictionary or a copied byte cache. A never-attempted over-bound preparation is
  discarded into a bounded sticky refusal, requiring explicit abandonment.
- One waiter per occupied session; 200 excess heartbeat calls return Busy.
  Existing three-operation gate-reference retirement assertions pass. A paused
  heartbeat allows a healthy peer to write before the pause releases.
  Cancellation reports InFlight, retains slots and the batch, and refuses
  retirement. The compatibility bridge is tested under a context that rejects
  Post, without claiming an unconditional scheduling or I/O deadline.

**Exact remaining P2.P2B floors / qualification limits:**

1. **Reserved-state publication is incomplete.** A newly reserved state has null
   Pending and AwaitingPreparation until its input is frozen. It consumes a
   global slot and is not Live, but the requested explicit Preparing/Register
   publication before the identity callback is not yet represented. A Busy
   result in that interval can report Unknown membership. This requires code and
   a blocked-factory result assertion, not a wording change.
2. **End-pending batch classification is incomplete.** Waiting/running End
   suppresses new batch work through gate references. A *quiescent failed* End,
   however, reaches the generic operation-conflict path and reports
   Refused/INPUT_CONFLICT, not the requested Busy/InFlight batch classification.
   No write is attempted. This needs a narrow batch-admission correction/test.
3. **The full stress/boundary union is not proven.** The suite does not run 128
   simultaneously blocked registrations across production roots; it runs 128
   live globally and 127 live plus one reserved in a fixture scope. It lacks
   GC/strong-owner retention observation, irrecoverable heartbeat/end uncertainty,
   eleven fixed attributes and adversarial enumerator controls. Metadata boundary
   arithmetic is synthetic; an actual Windows path at that boundary is not
   exercised. The UTF-16 exact boundary is checked through Freeze, while the
   public oversized path and exact packet-plus-repair refusal are exercised.
4. **Batch transport contention remains visible, not solved.** Independent tasks
   can contend on the existing writer's fail-fast root guard. The typed batch
   reports those individual Unavailable outcomes. The legacy bridge performs
   at most one serial retry of WriterBusy results after the parallel join; it
   never retries uncertainty or rebases a Prepared. That compatibility policy
   needs DS review. There is no dedicated failed-plus-healthy typed-result test,
   nor a full stressed scheduling/End/cancellation matrix.
5. **Instrumentation proof is incomplete.** Executed leases emit Activity
   outcome/code/membership/count/duration without payloads. Capacity/Busy/unknown
   early returns expose typed codes but do not emit that Activity, and emitted
   emitter telemetry has not been captured in a dedicated assertion. The writer
   telemetry controls still pass; they do not establish emitter telemetry.
6. **Independent gates and programme work remain open.** No author-cleared
   Test/DS/Security gate, canonical Proof Pack, notice/Data recovery qualification,
   cross-platform claim, P3–P5 completion or activation follows. Raw evidence
   stays in the assigned tree for the parent's separate consolidation. No site,
   docs index, canonical register or public status claims were regenerated.

Fixture changes are part of the memory fix: original emitter/producer fixtures
now End or prove-no-write-abandon and retire before deleting their files. The
former stale-snapshot test reflected the removed `_live` HashSet and assumed
serial batch dispatch. Its revised assertion permits an admitted heartbeat
before End, verifies End is final and unique, and verifies a later heartbeat
cannot append; explicit batch failures are not disguised as admission. This
adaptation is disclosed rather than calling all 77 test bodies unchanged.

Corrections for the parent-owned class register (class → sweep → derive → prevent):

- **Self-contention introduced by parallel dispatch:** the ordinary legacy
  HeartbeatAll test went red because two tasks can hit the writer's root guard.
  Sweep: all batch dispatch and legacy joins. Derive: preserve typed outcomes,
  then one bounded compatibility retry only for root Busy; no retry of uncertain
  writes. Prevent: original ordinary batch test and denied-first/healthy control.
  Broader contention qualification remains item 4, not silently certified.
- **Test fixture coupled to private collection shape:** the former snapshot
  test failed on `_live` removal before reaching its behavioral oracle. Sweep:
  the single `_live` reflection; gate reflection remains intentional to observe
  references and bounded waiters. Derive: durable end ordering rather than
  HashSet order. Prevent: final End plus post-End no-write assertions.
- **Concurrent read fixture violates transport sharing:** polling ReadAllLines
  while the writer owns its real handle raised IOException in the new test.
  Sweep: new progress polls. Derive: observe gate quiescence before reading the
  completed healthy file. Prevent: paused/cancelled batch test; no sleep/retry
  that hides a production error.
- **Helper lexical scope/analyzer errors:** the first cleanup helper landed
  nested in the new test class, and predicate assertions triggered xUnit2031.
  Sweep: all new helper callers and new Single assertions. Derive: one namespace
  helper, built-in predicate overload. Prevent: the unchanged compiler/analyzers;
  the failed `extended.stdout.txt` is retained, with null TRX counts.

Planned graph and actual work used one author and no agents. Full token usage is
not recorded; no inferred token cost is presented as measured. The 45-call bound
is a handoff checkpoint: this is a coherent, tested partial implementation with
the six floors above still binding, not authority to mark the whole goal done.

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

### P2.P2A implementation receipt

Contract-first commit: `95a7e17b`. The writer now has a single bounded prepared
admission path; the existing emitter source remains unchanged. The original
producer tests were not edited. Their baseline is 17 cases: 13 PASS, four RED
(two 65,537-byte encodings, file 129 and root 33,554,432 + 92 bytes). The broader
final selector is 76 cases, including the nine ordinary emitter tests, seven log
compatibility tests, parser corpus and 26 prepared-writer cases. The previously
described producer-plus-emitter set is 26, not 26 producer-only tests.

Evidence is raw, not a second Proof Pack:
`docs/proofs/p24-producer-baseline-red.trx`;
`docs/proofs/p24-producer-final-restored.trx`;
`docs/proofs/p24-producer-final-restored.stdout.txt`;
`docs/proofs/p24-producer-writer-pins.json`.
The finite runner `docs/proofs/p24-producer-verify.py` records actual child stdout
and stderr, per-run logical counts, measured duration, and source/test/project/
binary SHA-256 values. Its mutants raise all three bounds, falsely authenticate
a never-attempted preparation, publish membership before I/O, and prematurely
retire a gate used by three operations. Every mutant is followed by exact source
restoration; the last run rebuilds restored source. Initial baseline binary
SHA was not captured and is not reconstructed or claimed.

Observed controls cover: exact ASCII/escaped-Unicode limits; refused oversized
lines; empty-file count; root bytes and separator overhead; mutable attributes
and clock; independent-session progress while the clock pauses before I/O;
competing process file-count, byte-quota and sequence admission; same-root aliases
busy during flush; root gate reclamation; safe/unsafe filename collisions and
Windows case aliases; native CR/LF/CRLF nonempty-line counting and gapped sequence
values; original native parser corpus; immutable copied bytes; stale independent
intent refusal; attempted complete replay; actual prefix writes of 0/1/19 bytes
followed by IOException; flush/disposal acknowledgement loss; blocked recovery;
changed bytes, extra partial suffix and truncated prefix; and measured outcome,
code, byte count and duration without payload logging.

Boundaries of that evidence: Windows local filesystem only. The fault hooks
write real bytes and inject errors; the disposal test simulates lost
acknowledgement **after actual successful disposal**, not a kernel-generated
FileStream.Dispose failure. The return path does wait for target and root-handle
disposal. Unsuccessful durability acknowledgement remains uncertain. An available
PreparedWrite instance can be retried by another participating writer instance;
there is no durable PreparedWrite deserializer or restart-recovery claim.
Prepare may create the directory and empty non-JSONL lockfile, never reserve
accepted JSONL capacity. Source scanning allocates at most the 32-MiB target
snapshot and bounds each decoded line; it is bounded, not constant-memory.
Serialization preflight limits copied character volume, and a composed bounded
output Stream counts actual bytes rather than encoder buffer reservations.

Corrections from this run, for parent defect-register consolidation:

- **Virtual-overload recursion:** the first bounded stream inherited MemoryStream
  and forwarded span/array overrides to each other through virtual dispatch.
  The real test host stack-overflowed. Sweep: both overrides in this new helper;
  no second new subtype. Derive: compose an unmodified MemoryStream behind Stream.
  Prevent: every serializer test exercises the real path; the retained aborted
  TRX is `p24-producer-stream-recursion-red.trx`. No correctness label is attached
  to that aborted run.
- **Platform text in mutation mechanics:** exact mutation seams initially treated
  CRLF bytes as LF text; printing unbounded Unicode test output also interrupted
  the evidence runner. Derive: normalize text only for mutation, restore original
  bytes exactly, persist actual output as UTF-8 and print numeric counters. The
  runner requires a unique seam, exact expected failures, and restored-source
  equality. Prior incomplete runs are not used as completed mutation proof.
- **Blocking task result in async test:** xUnit1031 rejected `.Result`; use `await`.
  The existing analyzer is the prevention, not a suppression.
- **Commit identity:** the contract-first commit reported AGENT_SESSION unset.
  Subsequent coordination and commit calls explicitly export both variables;
  this does not retroactively label the initial check enforcing.

Independent DS/Test/language final review and canonical proof attachment remain
the parent's gates. Global emitter 128 prepared-state capacity, RegistrationNotice
capacity, Data recovery and P3–P5 remain pending. Legacy concurrent writers can
ignore root exclusion; only detected overruns are refused, without history edits.
The writer's guarantee does not disable or qualify those binaries.

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
