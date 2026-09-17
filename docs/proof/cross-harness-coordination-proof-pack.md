---
id: proof-cross-harness-coordination
title: "Cross-harness coordination: P1 foundation and P2 native replay evidence"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: P2
tags: [coordination, proof-pack, confidence, pending]
links:
  - { to: spec-cross-harness-coordination, rel: documents }
  - { to: design-cross-harness-coordination, rel: documents }
  - { to: plan-cross-harness-coordination-phases, rel: relates-to }
  - { to: investigation-cross-harness-message-delivery, rel: depends-on }
review-by: 2026-10-16
summary: >-
  Consolidates P1 producer and bounded P2 projection evidence, including the isolated
  pre-recovery measurement mutant and pending-update red/green receipts. Full lifecycle,
  independent review, production authority and P3–P5 remain unqualified.
---

# Proof Pack — P1/P2 candidates; independent code gate pending

## Native NOTICE corrective contract checkpoint — 2026-09-17

Source: `d0c3afd38fa9bd3fb6b3a6980338a61c389324fc`, containing diagnostic source
commit `7e9f1d9af4d692db94dc04d58b5260781f8a5fee` and derived-audit reconciliation.
The user's conditional Data/Security/DS contract is recorded in the existing P0
design/ADR and phase plan **before code**. No new independent review is asserted.
The first bounded implementation is dormant additive version-9 DDL in the actual
watcher database, not a substitute admission API or an in-memory queue repair.

GATE native-contract · 2026-09-17 · user-supplied Data/Security/DS conditional
CODEADMISSION · exit criteria: two-table contract, protected admission/lifecycle,
quota/owner, privacy and native/canonical boundary recorded · verdict:
CONDITIONAL, dormant synthetic code only · vetoes: live retention/erasure and
independent implementation qualification remain BLOCK.

N1/N2 at the source are **Verified RED**, not acceptance met. Their original
claims/reasons cannot be reconstructed from native session/liveness tables.
All enhanced admission/worker paths remain absent until the remaining contract
is fulfilled. Structural DDL tests, even green, do not prove either native oracle.
Remaining: trusted root capture, transactional admission and postcommit capability,
idempotent operation reconciliation, protected lifecycle CAS, process-global 128
hydration/reservation, actual store identity/enrolled owner, durable attempt
worker, immutable publisher/latest projection and caller wiring. Canonical bridge,
legacy binary qualification, P3–P5 and all-six-before-upstream remain unqualified.

### Native NOTICE dormant storage increment — author partial stop

Corrected contract committed first at
`52339c3e72ca309730ed3577d2094250ad77100e`. This increment changes only the actual
SQLite constructor/migration and a new private DDL constant, focal tests and their
receipt runner. It does **not** implement the requested complete admission and
delivery corrective unit. **N1 and N2 remain RED/UNSHIPPABLE.**

| Claim / oracle | Actual evidence | Red / confidence | Limit |
|---|---|---|---|
| Fresh and branch-v8 databases receive the same additive version-9 shape | `RegistrationAdmissionTests.Open_FreshAndVersionEight_ProduceIdenticalSchemaWithoutNativeEffects`: compares ordered actual `sqlite_master`, reads version 9 and zero native sessions/new facts | In `corrective-schema-red`, absent schema fails; in `corrective-final-green`, passes. **Verified structural behavior** | Branch-v8 fixture uses the unchanged real legacy DDL constants; not a released binary or candidate-v8 upgrade matrix |
| A collision at the second new table rolls the whole migration back | `Open_SecondNewTableCollides_RollsBackWholeMigration`: actual constructor raises; version stays 8, first new table absent, collision sentinel preserved | RED before migration exists; GREEN afterward. **Verified** | Fresh-creation fault at every statement and all precommit native writes are not covered |
| Admission facts and notice identity/bytes cannot be updated, deleted or replaced | Eight real SQL mutation cases, including raw connection with `recursive_triggers=OFF`; compares complete rows afterward | Initial table-absence RED → positive inserts and mutation refusals GREEN. **Verified tested mutations** | No general malicious-writer fence; disabling FK enforcement remains outside the enrolled protocol |
| Required fields reject NULL and typed/bounded values are checked | 29 generated required-field cases plus explicit invalid generation/path/digest/blob/target cases; actual inserts and row-count readback | First 38 structural cases were RED before DDL; added NULL/enum cases observed GREEN only. **Verified executions; isolated constraint-mutation coverage unverified** | SQL validates digest *shape*, not SHA-256 content or canonical serialization equality. Payload `x'01'` and zero digest are deliberate synthetic structural fixtures |
| Valid native Int64 generation values are not narrowed | `Admission_ValidInt64Generation_PreservesActualDomainValue` uses the actual `SessionGeneration` with 2,147,483,648 and Int64.MaxValue | Both RED against the author's mistaken Int32 ceiling; both GREEN after removing that ceiling. **Verified red/green** | No generation increment/overflow or lifecycle issuance API was added |
| Actual enum classifications are accepted without minting authority | `Admission_ActualTrustEnum_PreservesClassificationWithoutIssuingAuthority`: actual Asserted/Verified enum, stored string and zero native sessions | **Verified structural positive**, not a permission test | No human/source authority columns; enum acceptance is not authorization |
| Pending → InFlight → Pending → InFlight → Published preserves bytes and publication is terminal | Real SQL transition test checks attempts 2, ownership version 4, bytes `01`; invalid transitions and terminal reversal fail without changing rows | Original missing-table RED → GREEN. **Verified structural transitions** | No claimant, stale-worker CAS, due reader, OS ownership, retry worker or publication effect exists |
| Existing selected native behavior remains unchanged | `corrective-final-green`: **105 executed, 103 passed, 2 failed**; **71/71 structural cases pass**, actual dotnet exit **1** | **Verified**, including 32 existing positive cases | This selected union is not all Core or UI. The two failures are intentionally retained diagnostics, not regressions made green |
| N1 is still lost-notice failure | Actual host drain → native publisher IO failure → second drain: `Assert.Single`, empty retry collection | **Verified RED** in every new receipt | No durable recovery was implemented |
| N2 still admits correction 129 | Actual two-host/store pipeline changes sessionRows 128 → 129 and outstanding corrections 128 → 129 | **Verified RED** in every new receipt | No global reservation/hydration was implemented |

All run directories are under `docs/proofs/p25-notice-evidence/`, each retaining
`stdout.txt`, `stderr.txt`, `native-notice.trx` and `receipt.json`:

* `corrective-schema-red`: 72 executed / 32 passed / 40 failed; structural 0/38.
* `corrective-schema-green`: 72 executed / 70 passed / 2 native failures;
  structural 38/38.
* `corrective-generation-red`: 105 executed / 101 passed / 4 failed; structural
  69/71, two Int64 boundary failures plus the two native failures.
* `corrective-final-green`: 105 executed / 103 passed / 2 native failures;
  structural 71/71.

`run-corrective.ps1` captures actual process stdout/stderr separately and records
the real nonzero dotnet exit before checking expected failures. Its own success
means **receipt checked**, not suite passed. Each receipt pins the source,
projects, Core/test binaries, raw streams and TRX with SHA-256. No transcript
metadata is described as actual stdout. Baseline receipts remain unchanged.

**Class → sweep → derive → prevent.** Existing RIG-A recurred in this author
increment: the initial SQL assumed an Int32 generation without opening the actual
Int64 value object. The source sweep found `WatcherIdentity.SessionGeneration`
and both SQLite `GetInt64` readers; the new table was the only newly introduced
Int32 generation restriction. The corrected check uses SQLite integer/positive
semantics, not a competing 32-bit ceiling. Two actual-domain boundary tests were
observed RED then GREEN. The lessons register is outside this lane's explicit
ownership; this same canonical record carries the instance for conductor
incorporation. No additional lesson file or parallel Proof Pack was authored.

**Reach/measurement debt.** DDL is consumed by real `Open`, on fresh creation and
upgrade, in the existing transaction. There is no production writer/compute
reader for the new rows yet: this is a dormant prerequisite, explicitly not a
shipped capability. Existing `watcher_schema_version.applied_at` records
successful application; normal-path admission/publisher cost, failures, opaque
operation IDs and queue measurements remain unimplemented. The receipt's
measured durations describe tests only. Digest derivation, original binding
reconstruction, trusted local target validation, immutable file publication and
canonical target handling are **not** proved by these rows.

**Gates and handoff.** Data/Security/DS/Test implementation review remains open;
the author clears no veto. Release rollback is retain-tables/disable-enhanced
entrypoints, which are still absent; actual released old-binary qualification
remains owed. Live retention/erasure stays BLOCKED. No Workbench/public UI source
changed, so no desktop/UX review is requested for this increment; caller and
rendered-surface qualification remain later P5 work. Full prompt capture in the
audit is explicitly abbreviated, not represented as verbatim. Docs Explorer/
larger derived bundle and lessons reconciliation remain with the conductor.

#### Postcommit qualification

Executed against code commit
`fca4c04f190b11817c5f27144dd68d9470efeeb7` with `--no-build`, using the binaries
from the final build-backed run. `corrective-postcommit/` retains the complete
actual process streams, TRX and receipt: **105 executed / 103 passed / 2 native
failures**, structural **71/71**, actual dotnet exit **1**. A separate readback
verified every one of the **63 SHA-256 pins** and preserved all **23 predecessor
test occurrences** using an Ordinal name multiset. N1 still reports empty
`Assert.Single`; N2 still reports native session rows 128 versus 129.

| Postcommit evidence | SHA-256 of observed working bytes |
|---|---|
| `src/AiDe.Core/Watcher/SqliteWatcherObservationStore.RegistrationNotices.cs` | `70FF94BFDA0FD1526076C41004CCF2BC1A92F4C4B54BF3C1B49F2D41ED465254` |
| `tests/AiDe.Core.Tests/Watcher/RegistrationAdmissionTests.cs` | `D69CA7EF419604CD1AE50A7E50FB96797CCAD11D451067877DDAF489639D41FF` |
| Test output `AiDe.Core.Tests.dll` | `E4480609E388DE7B856F3D5EDD0AD7D2A8BE4B60383C974588A054E1F8D1C4FC` |
| Test output `AiDe.Core.dll` | `FD5392A9283C927A5CC1D8EB7B870C9D393516422E094F662F4AA272436EAA3D` |
| `corrective-postcommit/native-notice.trx` | `D1DBD3529D85465D097D609D421B316ED8E14CB323A23C36330121361E144ED1` |
| `corrective-postcommit/stdout.txt` | `8952EBC3D63DFAEDF5C555FF33696A89A1692E0AB8197F2528E3D3CC1FBD9705` |
| `corrective-postcommit/stderr.txt` | `8242E58A6BC1A86D8B667CC72EC84A282490E14C92C7FDC1BFE114BFBDB57086` |

These are **working-byte** hashes, not hashes of Git's normalized text blobs.
Git reported CRLF→LF normalization on text receipts/source when staging; a fresh
checkout may have different line-ending bytes. The immutable code commit above
pins the repository representation. Binary pins are recorded, not committed
binaries. Complete source/project paths and hashes are in `receipt.json`.

The bounded selfcheck found one **prior** goal-state gap (`p22-s1s6-request`) and
seven prior budget gaps; both new substantive entries have goal-state and budget.
No attempt was made to rewrite historical audit facts. `AIDE_SESSION` and
`AIDE_CONTRACT_LOG` are both absent, so no fabricated episode-close or alternate
transport was written. The canonical proof is the durable evidence pointer.
The assigned worktree and previously locked compiler scratch are retained;
no unknown process, other tree, live database, endpoint or GUI was touched.

## B5 qualification and bounded B6 update candidate — 2026-09-17

**Partial author evidence, not approval.** This continuation starts from
`eda167380704af1fbba62efbfea402fd0fa97104` in the assigned projection worktree.
Production actor authority remains **FALSE/DENY**. No tool launch, live endpoint,
schema, dependency, App, or producer-source change is included.

### Actual process receipts replace an overstated transcript claim

The earlier `recoveryB-finite-full-output.txt`,
`recoveryB-finite-restored-full-output.txt` and
`recoveryB-finite-mutants-output.txt` are retained unchanged. They are transcript
metadata/commands, **not retained actual runner stdout/stderr**. The earlier
paragraph calling them a full run transcript overstated the evidence. Their
TRX and pins remain historical receipts; no chronological production failure
is invented to repair that statement.

All new `lifecycle-*` evidence resolves under
`docs/proofs/p24-recovery-evidence/`. `qualify-lifecycle.ps1` invokes the installed
.NET runner directly using `ProcessStartInfo`, drains both process streams, saves
each separately, and records the actual process exit before checking it. Each
`.receipt.json` records arguments and SHA-256 pins for available source, projects,
test/Core binaries, TRX and both streams. The isolated mutant source/binary pins
are distinct from the unchanged main-tree source/binary pins. Scratch copies are
removed in `finally`; production source was never mutated for B5.

| Claim / oracle | Actual evidence | Red observed / confidence | Limit |
|---|---|---|---|
| Pre-recovery failure is NotRecorded, not completed zero | `lifecycle-b5-baseline.trx`: 1 passed, exit 0; `lifecycle-b5-mutant.trx`: 1 failed, exit 1; `lifecycle-b5-restored.trx`: 1 passed, exit 0 | **Verified:** same retained `Pump_FailsBeforeRecovery_ReportsNotRecordedRatherThanCompletedZero` fails `Expected: NotRecorded; Actual: Completed` when only the default measurement status in isolated Core is changed | Not a production bug RED; not the previous partial-Failed mutant; no unrelated fixture failure |
| An accepted pending UPDATE applies its original payload after a later observed registration | `lifecycle-update-red.trx`: 1 passed/1 failed, exit 1; `lifecycle-update-green.trx`: 2 passed, exit 0 | **Verified:** unchanged source left pending count 1 where 0 was required; removing only UPDATE from the skip list changes the result | Lost ACK and late identity/generation ambiguity for this new kind remain unqualified |
| UPDATE preserves admission, retained original body, exact model/harness and replayed native record | `Pump_PendingUpdate_OriginalPayloadAppliesOnceAfterObservationRegistration`: admission 1 retained, original raw-byte hex unchanged, exact harness/model names and versions, generation 1, exactly one OBSERVED_UPDATE receipt; four pumps and four pumps after new-store construction | **Verified executed assertions:** source body equality also preserves its original serialized observation time; restart compares the entire `SessionRecord` | No separate explicit source-time-field oracle, no heartbeat/capability mint counter, no recovery-specific after-commit fault |
| Already registered UPDATE changes only intended model fields | `Pump_RegisteredUpdate_PreservesNativeIdentity`: entire native record equals original with the explicit model replacement | **Verified** positive control passed even on baseline | Does not settle pending lifecycle identity ambiguity |
| Prior selection preserved | `lifecycle-union-green.trx`: 504 executed/passed, zero failures/skips, exit 0 | **Verified:** runner checks all 502 predecessor occurrences by Ordinal multiset, not unique display names; the prior 488 are contained by the preserved predecessor receipt | Selected class union, not all Core or full P2 |

The change reuses the existing `ApplyObservation` UPDATE SQL inside the existing
IMMEDIATE recovery transaction. The resolved native repository comparison still
precedes effects. It neither calls the live registrar nor increments generation,
and introduces no source-timestamp ordering rule. The existing same-repository
generation-2 controls were not changed. The original source body stays in the
accepted event; admission/current-receipt linkage remains the existing model.
Reach: native writer → immutable captured record → accepted pending event →
recovery eligibility → inert native session update → receipt → reopened store.
Recovery status/work/duration instrumentation remains the existing normal path.

### Lifecycle matrix: do not promote one kind to B6 completion

| Kind | Actual code disposition | Qualification |
|---|---|---|
| register | Still skipped by recovery. Initial `ApplyObservation` creates an observation session when unknown and returns OBSERVED_REGISTER when known | A valid admitted register has no REGISTRATION_REQUIRED return on that initial path. This is source inspection, **not N/A by assumption**; exceptional/deferred registration boundaries remain unqualified |
| update | Newly allowed through existing inert writer after dependencies resolve | Native-source pending and already-registered cases red/green; original body, admission and restart assertions executed. Lost-ACK, explicit time-field, full no-authority counters, metadata GAP and ambiguity matrix **remain open** |
| heartbeat | Still skipped | Existing initial SQL writes allocator heartbeat ticks, not source observation time. Enabling recovery as-is would refresh receiver-time liveness. **BLOCK/unqualified** |
| session-end | Still skipped | Initial SQL inserts `session_ended`. Historical identity and once-only end semantics are not qualified. **BLOCK/unqualified** |

**B6 remains BLOCK.** B2/B3/B7 and independent B8 retain their earlier finite
obligations. In particular recovery-specific lost ACK for UPDATE, source GAP,
exhausted-reference behavior and exact lifecycle authority effects are not
covered by the new positive path. No mixed registration-notice recovery is
included. B5 now has the missing isolated focal mutation receipt, subject to the
independent Test reviewer. Full P2 and P3–P5 remain unmet.

### Native producer P1: one canonical attachment, separate implementation branch

Read-only integration source:
`4c780db66d71a1fbf70c77faeef4a25070cb7616` in
`C:\Projects\ai-de-feature-xh-p2-producers`. No file there was edited.
`SessionCoordinationEmitter` writes register/end before changing membership,
serializes a session through a referenced gate and counts waiters before releasing
the lookup lock. This source inspection did **not** establish a current gate
retirement defect. The prior independent 21-pass review is supplied by the task;
this attachment is not the author's independent approval.

| Producer receipt under `docs/proofs/` at the producer commit | Observed results | SHA-256 |
|---|---|---|
| `p24-producer-evidence-restored-green.trx` | 21 passed | `36E28B4CD1B0F64B8C93395DECA42878D89A624DC40BC17CE8FE031294BB6196` |
| `p24-producer-evidence-green-with-bounds-red.trx` | 21 passed / 4 failed | `C00765067141A49699551CB8745DE97DD2DAB102F21CCA73201027C32683B887` |
| `p24-producer-evidence-mutant-membership-before-write.trx` | Add/Remove-before-write mutants: 2 failed | `E2448CF2D3C849E6F4C6423E792AA68F5BCAE1CD576D10A0DAF0184855AB677D` |

During this continuation, the parallel test author's new receipt became available
at `ff73f62c071aaf16bd379b494d27df372ff2a7b5`, not at the earlier producer commit.
`Register_FormerWaiterOwnsGate_ThirdOperationCannotOvertakeHeartbeat` supplies
the requested three-operation test. The new retirement mutant fails on actual
order: expected register/heartbeat/session-end, observed register/session-end/
heartbeat. The restored focal receipt passes. These are read-back receipts,
**not a rerun or independent review by this author**:

| New producer receipt | Results | SHA-256 |
|---|---|---|
| `p24-producer-retirement-mutant-red.trx` | 1 failed | `4864F745BB237E31E9CE807A14B2B069864BADA78D16905537C3B541F2A6CE51` |
| `p24-producer-retirement-restored-green.trx` | 1 passed | `1E6AEAC89426BE872441FDEF75EDF1D42C1C60B8C01A6787D12B232B11F35FA2` |
| `p24-producer-retirement-whole-floor.trx` | 22 passed / 4 failed | `5288CB3CB767F75217F2928F1AFA6462CDC3CE4F730D39E6915CE67B458ABC32` |

Independent review of the three-operation evidence remains pending.
Prepared/global-128/notices bounds and producer Distributed Systems qualification
are **not** cleared. Producer source/tests/audit are pinned by the two commits;
the new local `lifecycle-producer-pins.json` records their git blob identities.
The evidence stays on its producer branch until integration; no competing
producer Proof Pack is created.

### Class → sweep → derive → prevent and bounded close

* **Accepted lifecycle work with no execution path:** sweep the four explicit
  skips, retain unsafe/unqualified kinds, reuse the inert UPDATE writer.
  The new pending-update oracle failed on the unchanged skip and passed after it
  was narrowed. This is partial control, not a full lifecycle claim.
* **Transcript metadata promoted to runtime evidence:** preserve old receipts,
  classify them honestly, capture runner streams/exit directly, require nonzero
  selected counts and focal assertion text. The isolated false-Completed mutant
  proves the requested B5 control can fail.
* **Execution-budget finding:** oversized combined reads consumed the bounded
  tool budget without exposing their full output. Subsequent reads were narrowed.
  This prevented completion of the broader new-kind qualification matrix; the cap
  is not a proof of completion and has not been raised.

Central defect-register/index regeneration belongs to the conductor under the
explicit file ownership grant. No central gate is represented as passed here.
Independent Data/Distributed Systems/Test review remains required; no author
self-approval, deployment, upstream handoff or later phase completion is claimed.

## Recovery B finite-known-gap continuation — 2026-09-16

**Partial author evidence, not independent approval or full P2.** Base
`e5b7949324c449999d1e96f290d28f188a128af8`. The unchanged B amendment is
`f2ad827e`; its predecessor implementation base was
`f12e8b86b5dfc1341a4526fdf0540af162ff599b`. Enhanced canonical append
remains disabled and production authority is **DENY**.

### One canonical pack: process correction

The prior author created a second proof at
`docs/proof/p24-recoveryB-author-checkpoint.md`, with `type: proof`,
the wrong owner and path-valued/nonconforming typed links. That was an
unauthorized second Proof Pack, not a gate or independent approval. This section
consolidates its factual receipts and limits. The former path is now only a short
historical `type: doc` pointer, with owner `@timianmalloo` and a typed link to
`proof-cross-harness-coordination`. Its original text remains in commit
`e5b7949324c449999d1e96f290d28f188a128af8`. No historical JSONL, TRX or pins
were rewritten. No new design/review gate was invented for this path correction.

### Preserved predecessor receipts and limits

Every short evidence name here resolves under
`docs/proofs/p24-recovery-evidence/`. `recoveryB-pins.json` retains the
predecessor source/test/project/binary SHA-256 pins unchanged.

| Predecessor factual receipt | Observation and limit |
|---|---|
| `recoveryB-baseline-red.trx` | Original late-parent and 1,025-child runtime failures; both subsequently green. |
| `recoveryB-final-green.trx` | Late same-repository parent applies the child once at admission 3; pending 0, two board messages and unchanged native message on replay. |
| Original capacity oracle | All 1,025 admissions survive; all children apply after the parent within 20 turns; active pending 0, retained pending bytes 0. Not a near-byte-limit fixture. |
| Restart oracle | 130 children: recovery attempts/examinations 64/64/2/0/0/0 across fresh store/pump instances. Originally executed without a dedicated new budget mutant; the continuation supplies the 65-attempt mutant below. |
| Original exhaustion oracle | Eight **recovery** failures exhausted both payloads at generation 1; a later parent opened generation 2 and applied once. It did **not** prove a combined initial-plus-recovery budget. Superseded only in interpretation by the new combined-budget oracle below. |
| Original paired tuple boundaries | Generation 2 requires finalization, second staging rejects, eight invalid domain/half-payload cases reject, polling cannot reset attempts. Dedicated predecessor red proof was incomplete. |
| `recoveryB-union.trx` | 488 passing result occurrences, zero failed/skipped, zero multiset difference from the earlier 471. Selected classes, not the full Core suite. |
| `recoveryB-schema-mutants.trx` | 23 failing / 50 passing predecessor mutation-harness results: 21 lost-refusal assertions, one indexed-lookup assertion, one generation CHECK; zero missing anchors. Historical harness evidence, not a new recovery mutation score. |
| `recoveryB-first-runtime.trx` | Intermediate capacity failure: a traversal ending below its frozen high-water after a terminal row failed to wrap. Common end-of-lane advancement fixed all three lanes; original 1,025-child oracle was observed red then green. |
| `recoveryB-coordination.trx` | Intermediate reference activation lost the session snapshot; corrected by reusing the original scoped registration mapping. Old raw-SQL fixtures also required explicit staging/settling. These are failed receipts, never gate passes. |

The previous payload-clause mutant disabled the independent paired-payload CHECK
only inside its disposable transaction and rolled it back. No production guard
was weakened. Optional cross-stream snapshots, SQL VM-work bounds and pending
lifecycle semantics were not proven by these receipts.

### Current change reach and meaning

The existing aggregate remains one qualified event and its immutable receipts,
with a mutable derived recovery position. No new durable queue or live authority
was added. `ProjectRecord` now charges the actual initial `ApplyObservation`
call, including successful admissions and deferred source references.
`CoordinationRecovery` counts actual calls before execution, independently of
committed per-event attempts. A thrown transaction rolls its event counter and
cursor back; the pass still records the attempted call. A blocked reserved
activation rolls payload/native effects back, then records its observed attempt
and cursor in the existing progress transaction. Dedicated reserve rollback
qualification remains B-3; this implementation is not presented as that proof.

The reach is fresh-v8 DDL → event counter → projection → recovery → pump
`LastRecovery` → normal Activity tags → real SQLite/native-writer tests.
`NotRecorded` means recovery did not run; `Failed` carries observed partial work;
`Completed` alone means a completed pass. Operator questions have named sources:
work volume from examinations/attempts/applied, duration from elapsed milliseconds,
failure/path from status and stable error code. `Applied` counts committed effects,
including commits before a later failure. No raw content enters tags.
`COORD_RECOVERY_COUNTER_OVERFLOW` refuses checked logical service-counter advance;
`COORD_RECOVERY_FAILED` reports non-source exceptions without copying their text.

Seen components remain cumulative. Registration then parent is 0→1→2; both at once
is 0→2 without an intermediate attempt budget. Eligibility remains a nonnegative
integer, never a boolean. All nine logical last/high/served fields now reject
negative, fractional, nonnumeric, NULL and oversized values. Existing NOT NULL
paired-current FK and generation-2 finalization remain intact.

The pass still protects 32/16/16 attempts and 128/64/64 metadata selections before
borrowing, with global totals 64/256, persistent least-served/keyset cursors and
next-pass wrap. **Selections are not SQLite VM row visits.** Source records come
from the already validated bounded capture, not per-retry rereads. Retained
ordinary capacity remains 1,023 / 16,646,144 bytes; absolute reserve capacity
remains 1,024 / 16,777,216 bytes. Cross-source fairness and query-plan work are
explicitly unverified below.

### Executed current oracles and mutations

| Claim | Focal oracle / evidence | Red observed | Confidence / limit |
|---|---|---|---|
| Initial + seven recovery failures exhaust at eight, not nine | `Pump_InitialPlusSevenFailures_ExhaustsWithoutNinthAttempt`; initial 1, seven actual attempts, payload 0, restart stays at 8, new parent generation 2/attempt 1 | `recoveryB-finite-red.trx`: expected 1, actual 0. Reverse initial-charge mutant reproduces it. | Verified native fixture; flapping/disappearance boundaries remain B-3. |
| Both components together do not create an intermediate budget | `Pump_TwoComponentsTogether_OpensGenerationTwoWithoutIntermediateBudget`; generation 2, no generation-1 transition, attempt 1 | Existing-path characterization, not independently mutated | Executed; no stand-alone mutation claim. |
| Partial failed pass reports actual work and rethrows | `Pump_PartialRecoveryFailure_PreservesActualWorkAndRolledBackEvent`; second attempt faults before commit after one committed child; 2 examinations, 2 calls, 1 applied, Failed, elapsed and exact Activity tags; pending events retain generation 1 | Baseline expected attempts 2, actual 0; status mutant expected Failed, actual Completed | Verified before-commit partial outage. Recovery-specific lost ACK remains B-3. |
| A failure before recovery is not completed zero work | `Pump_FailsBeforeRecovery_ReportsNotRecordedRatherThanCompletedZero`; injected projection fault, NotRecorded/no elapsed plus pump diagnostic | Fault executed; not separately source-mutated | Executed typed absence, not blanket outage coverage. |
| Logical domains refuse invalid values | `Checkpoint_InvalidLogicalCounter_Refuses`, all nine fields × five invalid values | `recoveryB-finite-domains-red.trx`: nine focal “No exception was thrown” failures before DDL guards | Verified real SQLite. Initial nine setup failures in the first RED receipt were **not** semantic evidence. |
| Logical service-counter overflow is atomic and typed | `Pump_ServedCounterOverflow_RefusesWithoutMovingCursorOrAttempt`; max integer, unchanged cursor and attempts | Baseline no exception; guard mutant throws wrong `OverflowException` instead of stable typed error | Verified served-counter overflow; eligibility-max runtime fixture remains B-3. |
| Global attempt ceiling stays 64 across restart | Existing 130-child oracle | Reverse ceiling mutant observed 65 and failed 0..64 bound | Verified single child-source fixture, not cross-source fairness. |
| Prior selected result occurrences remain passing | First restored implementation `recoveryB-finite-union.trx`: 502/502; final post-mutation `recoveryB-finite-restored.trx` with corresponding receipt | Four reverse mutants fail four independent focal oracles, 8 pass; none fail from build/setup | Exact ordinal multiset comparison against all 488 predecessor occurrences; selected classes only. |

`recoveryB-finite-mutants.trx` and `recoveryB-finite-mutants-pins.json`
retain mutant source/test/binary pins. The four simultaneous mutants have
distinct failing oracles (initial count, failed status, overflow error, 65 ceiling);
this is targeted mutation evidence, not an isolated mutation score.
The final source/test/project/binary/TRX pins and full run transcript are
`recoveryB-finite-restored-receipt.json` and
`recoveryB-finite-restored-full-output.txt`. The earlier green receipt/transcript
remain separate. Baseline RED source is the named base; no claim is made that
its transient pre-fix test binary was preserved.

### Additive feed machine-receipt correction

The immutable `docs/proofs/p24-feed-evidence/p24-two-blockers-receipt.json`
has empty `recovery_counters` and `restored_counters`. The new restored receipt
reads namespace-aware TRX `ResultSummary/Counters` directly and requires a
nonempty node; it includes the historical recovery RED and restored union.
It corrects their machine-readable interpretation without rewriting the receipt
or any TRX. Explicit comparer labels replace ambiguous “distinct” claims:

| Receipt group | Occurrences | Ordinal distinct names | OrdinalIgnoreCase distinct names |
|---|---:|---:|---:|
| Original prior selection | 304 | 301 | 301 |
| Feed/blocker restored union | 471 | 466 | 465 |
| MCP selection | 72 | 72 | 71 |
| Predecessor B union | 488 | 483 | 482 |
| Current first green union | 502 | 497 | 496 |

Selections overlap; none of these rows are additive coverage.
The comparator verifies occurrence multiplicity, not guessed display-name totals.

### Corrections as classes, controls and remaining gates

* **Attempt undercount:** initial execution outside the recovery counter allowed
  a ninth call. Sweep covers initial applied/pending and retained-reference paths.
  Derive the budget from the one persisted event counter; combined-budget oracle
  and reverse initial-charge mutant fail without the fix.
* **Success-shaped outage:** publish-on-return discarded partial work and reset
  looked like completed zero. Sweep covers pre-recovery failure, partial work and
  commit-before-fault accounting. Derive both Activity and pump stats from the
  same finally snapshot; actual partial-failure oracle and status mutant enforce it.
* **Unchecked numeric cursor:** SQLite promotion could turn integer overflow into
  a real value. Sweep covers all nine last/high/served columns. Derive service
  order with checked integer arithmetic; invalid-domain and overflow controls
  now fail on the old shape.
* **Fixture failure mistaken for RED:** the first numeric fixture inserted offset
  0 after a source row ending at 1. All nine cases failed setup. The corrected
  fixture uses the actual boundary; only its separate nine refusal-assertion
  failures count as RED. The original receipt is preserved, not relabeled.
* **Duplicate proof/ambiguous counters:** consolidated into this canonical pack
  and added an explicit comparer/counter receipt. No independent process gate
  was self-cleared. Central lesson/index integration remains conductor work
  under the explicit author scope.

The prior checkpoint's eight numbered obligations retain stable local IDs.
**Closed at author-evidence level: B-1, B-4, B-5. Exact remaining set:
B-2, B-3, B-6, B-7, B-8 (8 → 5).**

| Remaining ID | Exact unresolved obligation |
|---|---|
| B-2 | Cross-source fairness/blocked probes/restart, fresh-source starvation, shared 256/64 accounting across all sources, protected first opportunities/borrowing and SQL query-plan/VM-work bounds. Single-source ceiling evidence does not close this. |
| B-3 | Flapping/disappearance/reappearance/duplicate and boundary-poll eight-budget matrix; eligibility overflow atomic refusal; byte-cap+1; legacy native-Seq parent; reserved-slot rollback; recovery-specific lost ACK; source GAP during reference rehydration. No runtime oracle for these is claimed here. |
| B-6 | Pending register/update/heartbeat/end recovery semantics remain intentionally unexecuted; original-source whole-effect-once without live registration/generation/heartbeat refresh/capability is unproven. **Independent lifecycle/liveness floor BLOCK**, not a scope waiver. |
| B-7 | Paired exhausted/tombstone history qualification and optional cross-stream projection-reference snapshots. Same-repository board-ID lookup alone does not discharge these. |
| B-8 | Independent Data/Distributed Systems/Test code review and broader recovery mutations, including isolated attempts/examinations/fairness/reference guards. Historical 23 schema mutants remain predecessor evidence; not re-run or promoted to coverage of new code. |

Independent gates remain **BLOCK / not convened by this no-agent author unit**.
No producer, canonical bridge, released-v7 old-binary rollback, candidate-v8
upgrade, UI, live database, endpoint, P2 completion, P3–P5 or upstream integration
was performed. Retain the assigned worktree for those independent reviewers;
release edit claims and end the session after the commit.
The AI-DE capture environment (`AIDE_SESSION`, `AIDE_CONTRACT_LOG`) was absent
in this author process, so no episode-close was fabricated for another session.

## P2 finite F1/F2 repair — 2026-09-16

**Author implementation complete; independent Test/Security/Data re-gate pending.**
Baseline: `50aab8c1711d745c8cce937556ac5ff95dffbcea`. This closes only the two supplied
author blockers, not full P2, canonical-producer integration, recovery or activation.
All P0–P5 approvals remain in force. No new permission is requested.

### Surface and contract

F1 follows the real native path: bound JSONL source → capture → IMMEDIATE SQLite
projection transaction → resolved persisted session → native board/update/heartbeat/end
effect → receipt/checkpoint → public metadata reader. `ApplyObservation` now compares
the actual resolved session repository with the immutable checkpoint binding, inside
that transaction and before any native effect. A contradiction raises
`COORD_SOURCE_BINDING_MISMATCH`; transaction disposal rolls back initial receipt,
event and checkpoint changes. The already-persisted foreign session is retained, not
rewritten to manufacture agreement. No schema or `RecordSession` change was made.

The counterexample is **mutable native state**, not a caller-controlled MCP exploit.
Prepared registration attributes cannot establish the current persisted repository.
Generation is not a substitute for repository identity: foreign generations 1 and 2
are refused; same-repository generation 2 still applies through all four event kinds.
Legacy unbound projection remains compatible, but its public reader is Unavailable.
An origin string without a binding does not establish authority.

F2 follows actual `Tools.Call` and the real private `Program.Handle` request router,
invoked by reflection in tests, through its JSON-RPC response builder. Scalar, array
and string argument containers now reach the coordination reader's existing typed
InvalidRequest boundary rather than throwing before it. Missing/null/non-string
tool names return that typed result. A non-object `params` container returns the
existing JSON-RPC error envelope with code **-32602** and the original request ID.
The tests assert `"jsonrpc":"2.0"` and `"id":"request-17"`, not merely a non-null reply.
No raw synthetic marker appears in the negative response.

The necessary general-envelope change is limited to invalid tool names and non-object
tool parameters. Existing valid dispatches and other tools' argument handling remain
unchanged. No broad catch, empty-result fallback, transport rewrite, helper-only
response test, endpoint or external process was introduced. Full malformed JSON-RPC
framing outside these tool envelopes remains outside this finite repair.

### Executed evidence and failure sensitivity

All new receipts are in `docs/proofs/p24-feed-evidence/`.

| Receipt / claim | Observed evidence | Oracle and confidence |
|---|---|---|
| `p24-two-blockers-red.trx` | **22 executed: 21 failed, 1 passed; exit 1.** Eight foreign-repository cases failed because no exception was thrown. Thirteen direct/outer MCP cases failed on invalid node conversion or parsing the former non-typed reply. Legacy compatibility passed. | Verified baseline RED against unchanged production at `50aab8c…`; not missing-API or compilation RED. |
| `p24-two-blockers-first-green.trx` | **54 executed: 46 passed, 8 failed; exit 1.** The repaired guard fired. The eight failures were a test expectation comparing a pre-persistence worktree repository with SQLite's reconstructed value. | A fixture error, not eight remaining production failures. Preserved unchanged as historical evidence. |
| `p24-two-blockers-green.trx` | **26/26 passed; exit 0.** Eight foreign repository/generation refusals, four same-repository generation-2 controls, one legacy control, three direct malformed argument cases, seven outer tool-envelope cases, three outer parameter-container cases. | Verified real-store and real-router behavior. Final outer tests split RPC errors from typed tool errors to avoid conditional assertions. |
| `p24-resolved-repository-mutant-red.trx` | **12/12 failed; exit 1.** Invert only `repository != known.Binding.Repository.CanonicalPath` to `==`. Eight foreign cases lost refusal; four same-repository cases falsely raised binding mismatch. | Verified meaningful mutation kill in both directions and across generations. Restored operator before final build. |
| `p24-two-blockers-restored-union-green.trx` | **471 executed, 471 passed, 0 failed/skipped; exit 0; 465 distinct displayed names.** Derived union of 29 classes from the prior regression and MCP receipts, including **58 reader cases**. | Verified selected regression union, not the entire repository suite. Class filters intentionally include their existing related cases; counts are measured, not summed from overlapping selections. |
| `p24-two-blockers-recovery-still-red.trx` | **5 executed: 3 passed, 2 failed; exit 1.** Same late-parent admission-3 failure and 1,025-active-pending bound failure, retained bytes 323,572. | Verified unchanged recovery counterexamples. Not part of a green/full-P2 claim. |

The restored union preserves the original **304 executed / 301 distinct displayed
names**, the prior reader selection's **334 / 331**, and MCP's **72 / 71**. Each
baseline's displayed-name **multiset** has zero missing occurrences, including names
shared by multiple executions. These selections overlap. Historical TRX files were
not edited.

The guard tests read the persisted session before and after refusal and assert exact
equality; unchanged heartbeat, no end marker, no board effect, one original event,
unchanged accepted offset and only the two original registration receipts. Thus no
new applied receipt is allowed to advertise a refused effect. Positive controls
assert each event's actual applied reason and generation. The MCP controls assert
typed status/code and outer correlation, not a fabricated response helper.

`p24-two-blockers-receipt.json` contains the exact final class selector, logical exits,
TRX counters, membership and SHA-256 pins for production source, test source, project
files, package pins and built Core/MCP/test assemblies. Baseline source SHA-256:

| Path / assembly | Baseline SHA-256 |
|---|---|
| `CoordinationProjection.cs` | `E63B83AE5AB1B0C56C931EA3B7223C37ECE2279199F92EF36EE9D86E28001627` |
| `Tools.cs` | `060D02B307A5A6FE1C710BD5345459A9577CFE41F608F3893B8F023A3FCFC2E9` |
| `Program.cs` | `79612C9F5814B0942A7542D91C1AE7C03281DAF96328A4DD769BB15C6B20182D` |
| Initial test source | `7D37D042EBF89D7CD0C8EA9F163F0F1EAFDE4A59808FD3BBB4ED99DBB17B3BA6` |
| Core DLL in baseline test output | `52FD670F01A52598BAA652EE59FD59C1F8234DB8C9318AE2A6C905A2EAA2EF70` |
| MCP DLL in baseline test output | `23E830C3708C68CDBD7445B821161717EDA63F96ACFD764AC7D7DEA82571998D` |
| Baseline test DLL | `5D7BF29016113C14BDF0B758916A70179E37F9309DB52987B42F8B7C91549D9E` |

The inverted-guard source SHA-256 is
`F6DA8BC9CB85CDF99147F9A0A5E0468957D57D827D796C763FC8127659697E41`;
its Core DLL is `197E2188C7F822997EDBAD87629D8A3B3B128F10629CD51BB89AF1370959857F`.
The mutant and restored runs used the identical test DLL,
`A10F7F2EFBF2ED3155B1E68655973FBAAC9A8E72724A01AF0EB348333F7874D4`.
Reproduce the mutant by changing that single comparison, rebuilding the existing
test project with `--no-restore`, and filtering
`FullyQualifiedName~Pump_ResolvedSessionRepositoryDrifts|FullyQualifiedName~Pump_ResolvedSameRepositoryNewGeneration`;
restore `!=` and run the receipt's final selector. No package or clock authority is needed.

### Class sweep, instrumentation and limits

**Class → sweep → derive → prevent:** F1 is validation of captured metadata instead of
the state actually consumed. The sweep covers register and all four native effect
branches in `ApplyObservation`; one resolved-row guard supplies their shared
repository rule. F2 is JSON conversion before an established typed failure boundary;
the sweep covers tool name, outer parameters, argument container, then existing
cursor/limit validation. Conversion now belongs to the existing reader boundary;
actual dispatch and outer-router tests catch recurrence. The fixture correction is
the related class “expecting the input object to equal the persisted representation”;
the control is exact persisted before/after comparison with explicit repository and
generation assertions. The shared defect-class register is outside this author lane;
these class/control details are handed to its owner rather than edited without ownership.

Operators observe repository refusal through its stable binding error, reader availability,
entry count and elapsed time through the existing `coordination.cache.read` activity,
and malformed tool calls through typed status/code or correlated JSON-RPC error.
The restored union executes the existing safe-span tests. No new runtime latency,
transport reliability, recovery schedule, human-channel qualification or performance
SLO is claimed. Source binding is structural qualification only.

No P1 source, canonical bridge, recovery B implementation, GUI, live database, endpoint,
configuration, hook, dependency, main branch or remote was changed. Authority stays
DENY and all grant/activation flags remain false. Independent re-gate, producer/bridge
old-binary integration, recovery and remaining P2 work, and P3–P5 remain open.
As in the prior restricted author lane, shared derived-site regeneration is not run;
integration owns it. The historical constructor/schema and frozen-H rows below keep
their original RED qualifications; this section does not relabel every old promise RED.

## P2.4 recovery counterexamples — 2026-09-16

**Intentionally RED, unshippable checkpoint. No join or full-P2 clearance.**
The only C# change is `tests/AiDe.Core.Tests/Watcher/CoordinationRecoveryTests.cs`.
Production source, schema, dependencies, configuration, hooks and P1 remain unchanged.
This implements the requested evidence slice, not recovery itself. All P0–P5
approvals stand; this record requests no new product permission.

### Goal, graph and scope

- **Goal:** establish missing recovery against the current native runtime.
- **Done when:** actual semantic RED and passing controls are durable, pinned and
  distinguishable from missing APIs, compilation errors or imagined schema columns.
- **Not in scope:** production repair, migration, canonical bridge, activation,
  retry scheduler, 401 feed, GUI, live data/endpoints, upstream work or join.
- **Tier:** T2, evidence-only. **Fan-out:** zero. **Main-line budget:** 28 calls.
  The existing assigned worktree/session was reopened, not replaced.
- **Graph:** source/fixture trace → test authoring → runtime proof → documentation,
  audit and checkpoint. Each edge carries data. Source/test design requires
  reasoning; execution and capture are deterministic mechanics. There is no new
  independent-review node: this checkpoint does not clear an implementation gate.
  Four stages, width one, no additional agents. In normalized stage units,
  `T1=T∞=4`, so parallel speedup is one; this is **Inferred**, not timing.
- **Execution bounds:** the small recovery case receives four post-parent pumps
  plus two replay pumps after successful recovery. Capacity receives twenty turns
  after the parent arrives: enough for 1,025 children at a proposed 64 attempts per
  turn. Each loop decrements a finite turn/item count to zero. These are test
  observation windows, not a claimed production recovery scheduler or latency SLO.

### Source-grounded surface trace

Real `CoordContractWriter` → separate native session JSONL files →
`CoordinationSourceCapture` → `CoordContractLogPump.PumpOnce` →
`SqliteWatcherObservationStore.ProjectCoordination` → immutable initial/transition
feed receipts and current event cache → real `board_message_fact`/board reader.
No schema/production write path was added. No UI or endpoint was exercised.
Graph output was absent, so grounding used the assigned source and the existing
phase plan's P2 pending-parent/capacity gates directly.

`CoordinationProjection.cs` admits pending once, then its existing-record branch
returns the prior result before `ApplyObservation`. `CoordinationContractLog.cs`
pumps captured pages but does not re-drive pending application. The late-parent
fixture distinguishes this from an invalid parent: the same serial parent-first
path succeeds, whereas child-first remains pending after a committed same-repo
parent appears. `original_id` is the external **session**, not a native event ID.
The test identifies the child's non-registration record by source offset and
retains its actual first receipt number.

The native allocator is supplied through real `MessageBoardService`; its first
message is deterministic `"301"`. Separate `a-child.jsonl` and `b-parent.jsonl`
files exercise supported cross-session parents. The wrong-repository control
uses a distinct real fixture directory, with the normal repository locator.
No fabricated effect, inserted parent row or mock store substitutes for the pump.

### Executed results and oracles

Evidence directory: `docs/proofs/p24-recovery-evidence/`.
All five tests are in `AiDe.Core.Tests.Watcher.CoordinationRecoveryTests`.

| Test suffix | Observed result and oracle | Confidence / limits |
|---|---|---|
| `Pump_LateSameRepositoryParent_AppliesChildOnceWithOriginalAdmission` | **RED:** `Late parent committed, but child remains pending at admission 3.` Readback: original admission 3, pending 1, board messages 1. Empty board before parent and parent ID 301 after arrival both passed. | **Verified counterexample.** Applied child, fresh transition `n`, exactly one applied receipt and stable replay assertions are deliberately after the failing assertion and remain **unverified**. |
| `Pump_ParentAlreadyCommitted_AppliesCrossSessionChildOnce` | **PASS:** parent 301, exactly one child reply, two board messages, zero pending, exactly one applied child receipt after three pumps. | **Verified control.** Serial arrival is not late-parent recovery. |
| `Pump_ParentInAnotherRepository_NeverSatisfiesPendingChild` | **PASS:** foreign parent 301 exists, child admission unchanged, payload retained pending, zero applied child transitions after four pumps. | **Verified bounded negative control**, not proof across an infinite time horizon. |
| `Pump_PermanentlyMissingParent_RetainsPayloadWithoutBoardOrLifecycleAuthority` | **PASS:** five pumps leave missing-parent reply and episode-open pending with complete payloads, no board message, one `TRUSTED_LIFECYCLE_REQUIRED` receipt and no non-registration applied receipt. | **Verified observation-only control.** It does not exercise a harness run-permission API or a live turn. |
| `Pump_PendingLimitPlusOne_AccountsForParentWithoutExceedingActiveBoundsOrLosingChildren` | **RED:** `Active pending count 1025 exceeds 1024; retained bytes=323572.` Parent was visible after twenty pumps, all 1,025 obligations remained represented, and retained bytes were below 16 MiB. | **Verified count-bound counterexample.** This small-payload sample does **not** prove enforcement at the 16-MiB boundary. Eventual application of all children remains unverified after the count assertion fails. |

`p24-recovery-red.trx`: **5 executed, 3 passed, 2 failed, 0 skipped**;
logical `dotnet test` exit **1**. Compilation completed and both failures are the
named semantic assertions above; neither is an argument/API/schema error.

`p24-prior-304-green.trx`: **304 executed, 304 passed, 0 failed/skipped**;
logical exit **0**. Its exact method selector was derived from the existing
`p23-root-alias-full-union-green.trx`, not reconstructed from a guessed class list.
This preserves the prior native R1/R2/root controls without editing them.
Combined observed selection: **309 cases, 307 passed, 2 intentional failures**.
This is not a claim that the full repository suite ran.

`receipt.json` records the original production HEAD, both logical exits, TRX
counters, exact test outcomes/stdout and SHA-256 values. TRX timings and pump
diagnostics are measurements; no production timing or throughput claim is made.
Commands used the existing `net10.0` project and installed packages, first
building the new tests with `--no-restore`, then reusing the same binary with
`--no-build --no-restore` for the prior 304 cases.

### Capacity fixture fidelity and limits

The first capacity child is serialized by `CoordContractWriter`. The remaining
1,024 synthetic consumer records copy that actual JSON shape, change only `seq`
and content, and append once before capture. This avoids repeatedly rescanning
the growing log in the producer. It is **consumer** evidence, not 1,025 producer
admission tests. The real capture splits at 128 records per page and the real
pump traverses all pages. SQL reads only existing cache/feed columns; no
proposed retry/backoff/active-queue columns or new API were asserted.

No redundant standalone fairness test was added: the actual runtime currently
re-drives none of these pending children, and the capacity case already crosses
64. Active pending count is the measured current `application_state='pending'`
population; a future separate active/backlog model will require a separately
reviewed representation and corresponding observable-state oracle.

### Reproducibility pins (SHA-256)

Production source HEAD: `821f40e1d8fe90067deeb611c78d54f9dc7bf2d3`.

| File | SHA-256 |
|---|---|
| `src/AiDe.Core/Watcher/CoordinationProjection.cs` | `16CE12EAAD11A3D675F357EF33E3D35D1DBEF5D3A744B24BAAE147BF19892A64` |
| `src/AiDe.Core/Watcher/CoordinationContractLog.cs` | `DDAA9154591F4B7FF1D2D87BB843E5FFF4493E0D934ADF49C180FE30A808FEE7` |
| `src/AiDe.Core/Watcher/CoordinationSourceCapture.cs` | `CA2600BC46D7B0735E94E9ADC69FC24E2B3F66A222617507114CA1E8A3F1E899` |
| `tests/AiDe.Core.Tests/Watcher/CoordinationRecoveryTests.cs` | `8314C36705B7511F0ADE9D065DAE48B2EDE378A1FAD7F619EEB4BF8D757422A6` |
| `tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj` | `FFAD72D5B9D4E9F1D59FBCCDA771BBBAC648158F3C299FA5BCBC19497734136D` |
| `src/AiDe.Core/AiDe.Core.csproj` | `B63C41B97201DC3E1016B72B404A32411AC31E477231B281219DDA64D0AD584A` |
| `tests/AiDe.Core.Tests/bin/Debug/net10.0/AiDe.Core.Tests.dll` | `5C3C40434F59FA3E30921A9AE44BB35862A7A3BA8084C5B3A9FB9FA6DEA56709` |
| `tests/AiDe.Core.Tests/bin/Debug/net10.0/AiDe.Core.dll` | `97D00203214391C49B86280CACA5DEE249BD613A3697A4C01413DBA7100E2585` |

### Class, sweep, derive, prevent

**Class:** replay deduplication freezes a deferred obligation by treating prior
admission as completed application. **Sweep:** the existing-record return
precedes application for every native kind; board-parent, registration-required
and trusted-lifecycle cases all retain the pending initial receipt. Lifecycle
work must not be made authoritative by blindly replaying it. **Derive:** retain
the existing immutable admission and append a distinct application transition;
the approved recovery implementation is still pending. **Prevent:** the new
late-parent test and limit-plus-one test are observed failing against current
production source. They detect the missing behavior, not a repaired implementation.
Central defect-register integration belongs to the conductor and is not edited
outside this author's assigned file scope.

One authoring correction was caught before compilation: the writer helper was
initially guessed as `FileName`, then checked against source and corrected to
`FileNameFor`. It is not counted as runtime RED. This is the existing RIG-A
unchecked-own-API class; source signature inspection and successful compilation
control this occurrence.

Test-shape union: D0 determinism/isolation; D4 real SQLite/filesystem; D6
actual writer-derived synthetic payload; bounded replay/idempotency assertions.
No new boundary substitute, dependencies, probabilistic model calls or UI exists.
No mutation run or independent implementation clearance is claimed.
The capacity counterexample and two semantic failures are the RED floor, not
evidence of a finished feature. P2 canonical bridge, 401 feed, retry-eight,
full migration/old-binary rollback, deployment and independent final gates remain
pending. No fresh approval is requested. Keep this worktree for recovery repair;
do not join or ship the checkpoint.

## P2.3 R3 source-root spelling correction — 2026-09-16

**Narrow author correction; independent code re-gate remains pending.** The supplied
independent P2.3 review reports 291 existing tests green, including all seven
original reliability tests (all four historical reds now green), plus 17 runtime
fault cases. It also reports actual same-repository cross-source-session reply,
invalid-UTF8 typed refusal and 129-file-limit history preservation passing.
These are supplied independent results, not fresh author executions. That review
left one verified blocker, **R3-SOURCE-ROOT-ALIAS**: unchanged bytes under `wire`
and `wire\` produced two sessions, two messages and two checkpoints.

### Scope, surfaces and correction class

Source base: `37923c0350583632091a9e55fd7396a108db6fa8`. One production file
changes: `CoordinationSourceCapture.cs`. Trusted source root → `Normalize` →
`RootKey`/file scope → existing raw capture → SQLite original admission, session,
message and checkpoint readers is the full changed path. No new persisted field,
schema, consumed ledger, canonical alias layer, capability or input authority exists.
The accepted raw source bytes and existing non-trailing-prefix identity remain
unchanged. Existing Windows casing and `Path.GetFullPath` behavior remain in place.

**Class:** equivalent directory spellings partition one replay history into multiple
source scopes. **Sweep:** this capture's root-prefix and per-file scope both use
`Normalize`; the correction belongs there, before encoding, not in a second mapping.
**Derive:** normalize with the existing native full-path operation, then use
`Path.TrimEndingDirectorySeparator` while above the native root length and ending
in a directory separator. **Prevent:** retained runtime replay and pure filesystem-root
cases below fail against the original source. Central lessons editing is explicitly
outside this author's scope; this record supplies the conductor's class/control handoff.

The loop's variant is remaining trailing-separator length, bounded below by native
root length. Native .NET execution showed drive roots preserved and UNC share
spellings normalized to the share root without an optional trailing separator.
UNC checks are **pure-function only**: no network share or filesystem root was read,
created or pumped. There is no Unix execution, symlink, junction, link-resolution,
device-namespace or general alias-canonicalization qualification.
This change does not merge or rekey historical scopes already stored under a
trailing-separator alias, nor repair histories previously duplicated by that
alias. Such stores need separate qualification; the retained replay proof begins
with the existing bare-directory scope and preserves its original identities.

### Retained red/green evidence

All raw receipts remain under the historical plural directory
[`../proofs/p23-native-evidence/`](../proofs/p23-native-evidence/).
Neither historical TRX files nor historical identity manifests were rewritten.

| Claim | Oracle and observation | Confidence |
|---|---|---|
| Reopening with one, two or eight native trailing separators, or alternate separators, preserves the original session/message/admission history and one checkpoint | `Pump_EquivalentRootAfterReopen_PreservesOriginalIdentitiesAndCheckpoint`, four cases; two repeat pumps plus exact original identity, admission extrema, event/feed/checkpoint counts, unchanged bytes and replay-result admission/message assertions. All four fail on the base source. | Verified by retained red and candidate green |
| Filesystem roots are not emptied; redundant root suffixes collapse | `RootKey_FilesystemRootWithRedundantSeparators_PreservesQualifiedRoot`, nine Windows drive/UNC cases; decoded key equals the native qualified root. Five fail on base; four already pass. | Verified pure function on Windows only |
| No original R1/R2 test was weakened | `CoordinationReliabilityTests.cs` remains untouched; its source pin remains `F582F7B3BBBC84A481AC4459925B19449D3099F5EC014DEB995E6CB398FADA43`. | Verified source and retained candidate run |

`p23-root-alias-red.trx`: **13 total, 9 failed, 4 passed, 0 skipped; exit 1**.
`root-alias-red-identities.json` pins the original production source, new tests,
actual Core/test binaries and raw red receipt.
`p23-root-alias-candidate-green.trx`: **272 passed, 0 failed/skipped; exit 0**.
This is the previous 259-case primary selector plus 13 new cases, **not** the
entire previously reported 291-case union. `root-alias-green-identities.json`
pins source, tests, original reliability tests, actual binaries and that receipt.
`p23-root-alias-full-union-green.trx`: **304 passed, 0 failed/skipped; exit 0**.
The stable TRX `testId` comparison in `root-alias-full-union-coverage.json`
establishes **all 291 existing cases present, zero missing, 13 added**.
`root-alias-full-union-identities.json` pins that run and its binaries.

The first coverage inventory incorrectly deduplicated display names: three
distinct existing cases share displayed names, yielding 288/301, not 291/304.
That inventory is retained unchanged, with the stable-id correction in the
separate coverage receipt. **Class/control:** a display label is not an identity;
the corrected check asserts 291/304 distinct test IDs and zero missing IDs.
The incorrect count check was observed red despite the 304-case execution green.
The initial probe compile also sorted installed reference-pack versions as text,
selecting 8.x for a net10 target and failing before execution. The corrected
command filters 10.x, sorts `[version]`, and refuses a nonexistent reference
directory. No verifier result is inferred from that failed compilation.

Commands, from the assigned worktree (no piped test exits):

```powershell
dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter 'FullyQualifiedName~Pump_EquivalentRootAfterReopen|FullyQualifiedName~RootKey_FilesystemRootWithRedundantSeparators' --logger 'trx;LogFileName=p23-root-alias-red.trx' --results-directory docs\proofs\p23-native-evidence --verbosity quiet
dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter 'FullyQualifiedName~Coordination|FullyQualifiedName~MessageBoard|FullyQualifiedName~BoardOrderingTests|FullyQualifiedName~ContractBoardPostTests|FullyQualifiedName~SqliteWatcherObservationStore|FullyQualifiedName~IngestHostTests|FullyQualifiedName~TrustedRegistrar|FullyQualifiedName~RestartDoesNotMultiplySessions|FullyQualifiedName~WatcherHostTests' --logger 'trx;LogFileName=p23-root-alias-candidate-green.trx' --results-directory docs\proofs\p23-native-evidence --verbosity quiet
dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~Coordination|FullyQualifiedName~MessageBoard|FullyQualifiedName~BoardOrderingTests|FullyQualifiedName~ContractBoardPostTests|FullyQualifiedName~SqliteWatcherObservationStore|FullyQualifiedName~IngestHostTests|FullyQualifiedName~TrustedRegistrar|FullyQualifiedName~RestartDoesNotMultiplySessions|FullyQualifiedName~WatcherHostTests|FullyQualifiedName~BoardPublisherTests|FullyQualifiedName~DaydreamPersistenceTests|FullyQualifiedName~McpMatchesTheJsonlPathTests|FullyQualifiedName~SqliteAllocationOverlapTests' --logger 'trx;LogFileName=p23-root-alias-full-union-green.trx' --results-directory docs\proofs\p23-native-evidence --verbosity quiet
```

Candidate production source SHA-256:
`CA2600BC46D7B0735E94E9ADC69FC24E2B3F66A222617507114CA1E8A3F1E899`.
Runtime test SHA-256:
`B9DCF488863BF13D48048E284920F4415044292E2C0A42C67956D7A8EAF3DCF3`.
Core binary SHA-256:
`7857AD44D36B4C9FC58CAE945FD73559306CC0C5F97EF69567ECE9D4C39D8C2F`.
Test binary SHA-256:
`C57E41BDDAC1F06C71DFA7784D374B7197C64FFB9900BAF41CFC5D5831938BF7`.

`LastRun.Replayed` is read back on the normal runtime path. Existing native pump
volume, refusal and timing instrumentation is unchanged; no new production latency,
capacity or exported-telemetry claim is made.

### Mechanical Proof Pack path correction

The sole current pack now lives at **`docs/proof/cross-harness-coordination-proof-pack.md`**,
retaining frontmatter id `proof-cross-harness-coordination`. The old plural pack
path did **not** qualify under the C# `ProofPackVerifier.ProofDirectory =
"docs/proof/"` rule or the `AuditLogEpisodeSource` evidence-path check. A looser
script substring check was not evidence that historical captures qualified.
**No historical capture is retroactively declared passing.** Audit JSONL history,
old TRX and hash manifests, and plural raw-receipt directories remain unchanged.
New audit declarations name only the singular pack path.

Current spec/design/ADR/phase-plan references move with the pack. P1's parallel
branch still carries the old path; consolidation belongs to the future join, not
an edit in that branch. There is no duplicate pack and no verifier widening.

Source inspection also corrects the scope of the forthcoming verifier receipt:
`Verify` checks reachable repository, path containment, proof-directory membership
and file presence; **it does not inspect Git commitment**. `VerifyInCheckouts`
folds those same checks. Running them after commit plus a separately recorded
commit establishes committed presence, not a Git check inside the verifier.
Neither a `Verified` verdict nor a commit proves full P0–P5 acceptance.

**Post-commit execution, observed:** at
`dbd963a895676ef6c21d6f4b1caed4d1ea1aa1ab`, a temporary dependency-free C# probe
called the actual compiled Core's `ProofPackVerifier.Verify` and
`VerifyInCheckouts([assignedWorktree], path)` for both paths. It returned exit 0:

| Declared path | `Verify` | `VerifyInCheckouts` |
|---|---|---|
| `docs/proof/cross-harness-coordination-proof-pack.md` | **Verified** | **Verified** |
| `docs/proofs/cross-harness-coordination-proof-pack.md` | **NotFound** | **NotFound** |

Raw receipt: [`root-alias-committed-pack-verifier.txt`](../proofs/p23-native-evidence/root-alias-committed-pack-verifier.txt).
The probe used SDK **10.0.303**, native reference pack **10.0.11**, and a byte-identical
copy of the pinned Core binary (`7857AD44…C2F`). No dependency was added. The probe
source and generated executable are removed after execution, not shipped as tooling.
Actual execution command:

```powershell
dotnet .agents\root-alias-probe\probe.dll C:\Projects\ai-de-feature-xh-p2-projection docs\proofs\p23-native-evidence\root-alias-committed-pack-verifier.txt dbd963a895676ef6c21d6f4b1caed4d1ea1aa1ab
```

`root-alias-commit-identities.json` separately pins the **committed Git blobs**.
Git normalized the working-tree CRLF bytes to LF on staging. Historical manifests
and the new execution manifests retain their original run-byte hashes, not silently
rewritten commit hashes. Committed source SHA-256 is
`f8f21d5c65765d06b857c28658a5738c42378c85576d9363bd5d9f07c9b713af`;
committed runtime test SHA-256 is
`ffcbf7ddddbe399be743f39cf6056f4bbb8557e688f287c1359457b3db3eee97`.
The later evidence-only commit does not change the production/test candidate.

### Remaining and bounded execution

Independent review was partially passing with one root blocker, not full P2
qualification. Broad DTO/cross-file permutations and aggregate/page boundaries
remain open, as do canonical bridge integration, retry/re-drive, 401/feed work,
old-binary rollback and P3/P4/P5. Upstream work follows verified completion of all
six phases; none is performed here. Next: independent code re-gate of this exact
candidate. The mechanical path fix is user-approved and needs no new design gate.

Execution is serial: source/test grounding → retained red → native correction →
green union → mechanical move → commit → actual verifier → evidence commit.
All edges are data dependencies; shared binaries preclude concurrent validation.
No agents, new dependencies, UI, live database, hooks, site or external actions.
Inferred work equals span at width one; no duration/speedup estimate is claimed.
The 35-call author budget is a circuit breaker, never permission to omit a gate.
Two mechanical rework passes were needed: display-name coverage counting and SDK
reference-version selection. No additional source fix or agent was needed. Site
and docs-index regeneration remain outside the explicit write boundary; the
stable frontmatter id and current document links are retained for the future join.

## P2.2 supplemental failure sensitivity — 2026-09-16

**23/23 previously unsubstantiated cases now have observed focal mutant failures.
Independent Test re-gate remains PENDING; this author does not clear it.** The supplied
review reports independent Data PASS (S1–S6, zero static residual) and Test 202/202 plus
27 real-SQL groups/314 assertions PASS, but blocks on these 23 missing sensitivity
receipts. Those review results are supplied context, not rerun or self-certified here.

Source base: `85a3f9b19d739f28f2cf605378024342ce23411e`. No production source, project,
package, configuration, schema version, or installed binary was patched. Three hooks
were added to the existing boundary tests; all existing assertions and the 21
`InlineData` values remain unchanged. **No new test cases were added.** Tests still
invoke `SqliteWatcherObservationStore.Open` from the actual Core assembly and real
Microsoft.Data.Sqlite. No Python schema implementation or substitute constructor exists.

The committed runner is `tests\AiDe.Core.Tests\Watcher\Run-P22Evidence.ps1`; the bounded
mutation definitions are `CoordinationProjectionEvidence.cs` beside it. Reproduce from
this tree with:

```powershell
& tests\AiDe.Core.Tests\Watcher\Run-P22Evidence.ps1 -Run p22-failure-sensitivity-review
```

The runner refuses an existing run directory. It builds once, then uses the same test,
Core and provider binaries (`--no-build`) for faults, restoration and the 202-case
candidate. It requires matching test names across the three 23-case passes, checks each
focal failure message rather than counting arbitrary failures, and records every logical
exit separately. Any missing TRX, unexpected exception, survived mutant, count mismatch
or wrong oracle fails the runner. No gate exit is taken from a formatter.

### Isolation and specificity

`Open` first creates a fresh, disposable `p22-boundary-*` database beneath the test
working directory. Mutation runs are restricted to that path. For each theory case,
the original valid K admission and diagnostic exist **before** injection. The existing
test then disables FKs and removes only the update-immutability trigger to isolate
storage/semantic CHECKs. This pre-existing isolation is unchanged.

The helper edits only the named table's SQL definition in that disposable database,
increments its schema cookie, closes/reopens the test connection, restores connection
PRAGMAs and verifies schema readback. `writable_schema` is disabled in `finally`.
It never enables `ignore_check_constraints`. Constraints unrelated to the explicit
replacement remain active. Every fault emits exact before/after SQL, the replacement
list, any index change, SHA-256 identities and SQLite/provider versions into its TRX.
The test fixture deletes the database on both failure and success. No live DB is opened.

| Mutant identity / unchanged bad input | Exact weakened boundary | Observed focal result |
|---|---|---|
| `source_offset=NULL` | Diagnostic offset non-NULL conjunct | No exception; original `Assert.Throws` FAIL |
| `source_end=NULL` | Diagnostic end non-NULL conjunct | Same |
| `source_offset='oops'` | **Joint:** offset storage type plus independent end/offset comparison for text offsets | Same |
| `source_end='oops'` | End storage type admits TEXT | Same |
| `source_offset=1.5` | Offset storage type admits REAL | Same |
| `source_end=2.5` | End storage type admits REAL | Same |
| `source_offset=-1` | Offset lower bound 0 → -1 | Same |
| `source_end=1` | End strictly greater → greater-or-equal to offset | Same |
| `raw_digest=NULL` | TEXT discriminator admits NULL; SQL's nullable length CHECK then allows NULL | Same |
| `raw_digest=''` | Digest length >0 → >=0 | Same |
| `raw_digest=X'01'` | Digest storage type admits BLOB | Same |
| `admission_n=NULL` | Noninitial admission non-NULL conjunct | Same |
| `admission_n=0` | Admission lower bound >0 → >=0 | Same |
| `admission_n=1.5` | Admission storage type admits REAL | Same |
| `admission_n='oops'` | **Joint:** admission storage type plus independent n/admission order comparison for TEXT | Same |
| `application_state='applied'` | Diagnostic application-state nullability allows applied | Same |
| `session_id='S',session_generation=1` | Diagnostic session pair allows S/1; independent pair consistency and positive generation remain | Same |
| `message_id='M'` | Diagnostic message nullability allows M | Same |
| `parent_event_key='P'` | Diagnostic parent nullability allows P | Same |
| `parent_application_state='applied'` | Diagnostic parent-state nullability allows applied | Same |
| `is_initial=1` | **Joint:** diagnostic initial-kind clause, noninitial admission-shape clause, and initial unique index narrowed to semantic rows only | Same |
| `insertion-lookup-scan` | Actual event-insertion epoch lookup changes `scope` to unary `+scope`, preventing indexed scope search without changing returned meaning | Original `Assert.Contains("SEARCH", plan)` FAIL: `SCAN coord_projection_event USING COVERING INDEX ix_coord_projection_event_end` |
| `semantic-generation-overstrict` | Feed generation >0 → >1, after valid pending admission | Original legal S/1 semantic INSERT fails with `SqliteException`, SQLite **19**, CHECK on `session_generation > 1` |

These are **23 bounded mutant/case pairs**, not 23 individually sufficient guard
claims. In particular, the three joint mutants do not show that deleting any single
constituent defeats the observable contract. The `is_initial` mutant preserves the
initial unique index for semantic rows and excludes diagnostics only, so the already
valid K admission is not an unrelated unique-key failure. The text mutants deliberately
address the second comparison that would otherwise keep rejecting the bad state.

The index mutant preserves the original **10 and 1000 populated diagnostic rows**:
both stages still report one predicate visit and SEARCH on
`ix_coord_projection_diagnostic_end`. All five actual insertion queries are extracted;
the first mutated query fails the original SEARCH assertion. It does not fail on
zero fixtures, an altered query count, a missing table, or an unsupported outcome enum.
The legal-row mutant fails on the focal semantic INSERT, not constructor/setup.

### Receipts and limits

New immutable run directories under `docs\proofs\p22-cache-evidence`:
`p22-failure-sensitivity-01` (first complete execution) and
`p22-failure-sensitivity-02` (runner hardening: name-set equality and fail-closed
exception recording). Each contains full `positive-control.trx`,
`targeted-faults.trx`, `restored-green.trx`, `candidate-202.trx` and `receipt.json`.
The JSON includes exact command arguments, elapsed seconds, per-case outcomes,
messages/stacks/stdout, logical exits, and source/project/package/binary SHA-256 pins.

| Pass | Required observed state | Logical exit |
|---|---|---|
| Unmutated positive controls | 23 PASS, 0 FAIL/SKIP | 0 |
| Targeted faulty schema/query compositions | 23 focal FAIL, 0 PASS/SKIP | 1 |
| Restored unmutated composition | 23 PASS, 0 FAIL/SKIP | 0 |
| Same method union as retained 202-case candidate | 202 PASS, 0 FAIL/SKIP | 0 |

This is **mutation-based failure sensitivity**, not reconstructed chronological TDD.
The original 22-failure baseline remains 20 schema-semantic failures, one fixture
error-code expectation and one constructor handle leak. Its missing first-test-revision
and baseline-binary hashes remain disclosed; these later receipts do not repair that
historical gap. New pins identify executed local bytes, not cross-machine deterministic
binaries or Git's line-ending-normalized blobs.

**Class → sweep → derive → prevent:** green-only additions lack a demonstrated
failure oracle; all 23 supplied missing cases were enumerated and matched to the
same committed assertions; mutation definitions derive from the constructor's actual
SQLite schema instead of duplicating the DDL; the committed runner rejects any
surviving or nonfocal result and retains exact faults for independent replay. The
central defect register remains coordinator-owned; this supplements its recorded
unreproducible-mutation-manifest shape without claiming fleet-wide closure.

**Remaining:** independent Test re-gate, then the authorized native-pump author.
Native R1/R2's four retained REDs are unchanged and not rerun in this evidence-only
unit. No new schema defect was found. Fresh unreleased v8 scope remains unchanged:
no pre-release-v8 migration, live activation, endpoint, slots, GUI or upstream handoff.
All six approved implementation tasks remain pending; evidence does not execute them.
Derived graph/backlinks and central-register promotion stay with the conductor under
the existing ownership boundary. The existing proof remains in `docs/proofs`, outside
the episode scorer's `docs/proof` namespace; no score or self-approved gate is claimed.

## P2.2 S1–S6 repair evidence — 2026-09-16

**Author repair complete; independent Data/Test re-gate PENDING. Full P2 remains
incomplete. No activation is authorized.** Baseline DDL:
`584975f99547f67c2245ac879a47fd44f3f8fcf2`. The design/ADR correction was written
before production DDL edits. Exactly the existing three caches remain. Fresh
unreleased v8 was amended; existing prerelease v8 fixtures are NOT upgraded.
Released v7→fresh v8 stays additive; no production database was opened or migrated.

### Evidence, scope and oracles

New cases: `tests/AiDe.Core.Tests/Watcher/CoordinationProjectionBoundaryTests.cs`.
Existing `CoordinationProjectionTests.cs` and native reliability tests are unchanged.

| Finding / claim | Oracle and observed evidence | Confidence / limit |
|---|---|---|
| S1 global committed order | Both explicit n=5 initial and tombstone inserts behind committed n=1,10 succeeded on baseline; repaired tests reject with `COORD_FEED_ORDER`, preserve current=1/high-water=10, then automatically allocate 11 | Verified SQL behavior; holes allowed; no old ID/Seq rewrite |
| S2 integer storage | Baseline admits nonintegral and text offsets/endpoints/pointers/generation/checkpoint. Storage-isolation tests disable only update immutability/FKs and test real CHECK/NOT NULL definitions. NULL, zero, negative, text, REAL, positive-generation controls run | Verified for present numeric columns. INTEGER PRIMARY KEY datatype mismatch is SQLite code 20, not CHECK code 19. No due/retries columns exist here |
| S3 key representation | Exact TEXT on all scope/epoch/event-key columns; BLOB, embedded NUL, empty and UTF-8 limit+1 rejected; legal multibyte exact limits accepted | Verified; byte ceilings 4096/128/512. No normalization. Historical/native IDs are not rewritten |
| S4 accounting | K[0,1): one event/admission. Equal-identity accounting diagnostic [1,2) plus checkpoint=2 in one transaction: original/current admission=1 unchanged. Replay INSERT rejected with no growth. L[2,3) advances checkpoint=3. Wrong scope/epoch/key/admission, gaps/overlap, diagnostic-as-current and semantic mappings on diagnostics reject | Verified SQL accounting/reference facts only. This fixture assumes duplicate classification; it does NOT compare captured canonical bytes or implement a production replay API/native effect |
| S4 diagnostic shape | 21 negative cases require integer/ranged non-NULL intervals, original admission, nonempty TEXT digest and NULL application/session/message/parent state. Positive semantic receipts retain NULL interval metadata; positive diagnostics have NULL semantic state | Verified. Digest is not equality evidence; it is reference metadata |
| S5 ordered lookup | Actual event-insertion trigger queries extracted and explained: five SEARCH plans, no SCAN or temporary sort. Event and diagnostic histories measured separately at 10 and 1000 rows. Both endpoint probes visit one matching row at each size | Verified narrow lookup paths on SQLite 3.53.3/provider 10.0.11.0. Instrumented predicate visits are not total VM steps or whole-pump runtime. No runtime/canonical-bridge guarantee |
| S6 payload guard isolation | Stage valid pending→applied receipt with only automatic transition trigger removed. Changed raw/canonical bytes or single-sided NULL with valid next pointer rejects specifically `COORD_PAYLOAD_IMMUTABLE`/1811. Remove ONLY payload clause: the SAME `Assert.Throws<SqliteException>` fails, mutant mutation persists pointer=2 | Four executed clause mutants; verified guard isolation. Existing positive legal transition/tombstone tests and new mapped tombstone control pass |
| Migration fault | Real Open with v7 fixture and abort-on-version-8 trigger rolls all coordination DDL back: max version=7, zero coord tables, old-a/old-b retain Seq=7 after successful retry. Baseline failed Open leaked connection and fixture deletion failed; Open now disposes failed initialization | Verified injected deployment failure and cleanup; NOT an old-binary rollback test |
| Prior compatibility | Original 32 schema + 91 ordering/compatibility + 6 migration cases all retained | Verified 129 passing, unchanged tests |
| Native R1/R2 | Entire unchanged reliability class still 3 pass / 4 fail; names in native TRX | Verified remaining defects, intentionally not repaired here |

The old `p22-cache-red.trx` remains **21 missing-table failures plus one version
assertion**, not 22 semantic REDs. This repair's new baseline is different:
50 executed, 28 PASS / 22 FAIL = **20 schema-semantic REDs**, **one incorrect test
expectation** (SQLite INTEGER PRIMARY KEY mismatch returns 20), and **one real
failed-constructor resource leak**. No missing-table/column failure is counted.
The baseline duplicate helper used the existing feed shape to expose semantic
`COORD_TRANSITION_REFUSED`, not a missing new diagnostic column.

### Immutable receipts and exact execution

All receipts below are new; original seven receipts remain unchanged. Shell identity
was `xh-p2-projection-b0d0` / `copilot-p22-schema`, UTF-8 enabled each call; official
session reopened, exact edit leases released before tests.

Common: `dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore
--filter <selector> --logger "console;verbosity=quiet"
--logger "trx;LogFileName=<name>.trx"
--results-directory docs\proofs\p22-cache-evidence`.

| New receipt stem (same directory) | Selector / state | Logical exit / shell |
|---|---|---|
| `p22-s1s6-baseline-red` | `FullyQualifiedName~CoordinationProjectionBoundaryTests`, unchanged baseline DDL, first 50 cases | **1** / 1030 |
| `p22-s1s6-first-green` | Prior 129-case union below plus `CoordinationProjectionBoundaryTests` (73): **202 PASS, 0 failed/skipped** | **0** / 1035 |
| `p22-s1s6-native-remaining` | `FullyQualifiedName~CoordinationReliabilityTests`, with `--no-build`: **3 PASS / 4 FAIL**, total 7 | **1** / 1037 |

Each stem has the full `-tool.txt` command output plus TRX with assertions, durations,
plans, provider version and mutant observations. Prior union: `CoordinationProjectionTests`,
`BoardOrderingTests`, `CoordinationReliabilityTests.Post_`, `MessageBoardTests`,
`SqliteWatcherObservationStoreTests`, `McpMatchesTheJsonlPathTests`, `ContractBoardPostTests`,
`BoardPublisherTests`, `SqliteAllocationOverlapTests`, `DaydreamPersistenceTests`;
each clause uses `FullyQualifiedName~`, joined with `|`. No zero-selection PASS.

`p22-s1s6-final-pins.json` records SHA-256 for both changed production sources, both
schema test files, unchanged reliability source, Core/test projects, package manifest,
and tested Core/test/provider binaries. Baseline production is pinned by commit;
the first test revision and baseline binary were not separately hashed before overwrite:
that reproducibility limit is explicit, not reconstructed as a measured hash.

### Class → sweep → derive → prevent; scope-limited handoff

* **Class:** local order is mistaken for global cursor order; affinity is mistaken for
  stored type; physical occurrences are mistaken for logical admissions; pointer guards
  mask payload guards; constructor failure loses ownership of a native handle.
* **Sweep:** all numeric/key columns and all endpoint/epoch queries in the three-cache
  DDL, both checkpoint guards, both feed insert/transition paths and event current
  validation were reviewed. No due/retry fields exist. Failed initialization was traced
  to Open before ownership reaches the store; read-only Open performs no migration.
* **Derive:** endpoint from immutable event-first and diagnostic histories, never another
  stored counter. Keep original admission and separate current state. Connection disposal
  stays at the existing Open ownership seam.
* **Prevent:** named boundary tests above; 20 semantic baseline failures, four isolated
  payload mutants, actual migration-version fault and explicit fixture deletion.
  The incorrect code-19-only assertion was calibrated to 19/20, not production-swallowed.
  Updating the global lesson register is outside this author's granted paths; this
  durable class record is handed to the coordinator for promotion.

Normal SQL errors emit stable refusal codes, and diagnostic counts/endpoints can be
queried on the normal data path. There is no production diagnostic writer yet.
Pump latency/spend/volume/failure instrumentation, whole-loop bounds, full canonical
byte equality, trusted native effects/capture, old-binary rollback and P3–P5 remain
unmet integration floors. The repaired schema cannot self-certify them. All six
upstream actions remain AFTER VERIFIED. No author-cleared Data/Test/DS gate is asserted.

### Author close / handoff

Contract amendment commit: `d85c8a9e2321fcb15a090f6a2b8d6ca1f4d7922c`.
The implementation audit measured **741 seconds** from its explicit start marker.
The optimizer has no separate measured start/duration; no elapsed estimate is presented
as measurement. Audit selfcheck exposed one new prompt-entry metadata gap: its full
prompt is stored but its goal/budget were omitted; the implementation/optimizer entries
carry them. This is reported, not retroactively rewritten.

`AIDE_SESSION` and `AIDE_CONTRACT_LOG` are absent; no watcher episode close was emitted.
Evidence lives at the user-assigned `docs/proofs/` paths, not an invented `docs/proof/`
capture path. No shared site/index regeneration or global lesson edit is authorized
in this finite ownership grant; the coordinator must regenerate derived surfaces at
the join. The audit renderer's own `docs/audit/audit-data.js` is included with the
append-only audit. Independent re-gate, join-time regeneration and whole-P2 acceptance
are explicitly not author-cleared.

## P2.2 additive cache checkpoint — 2026-09-16

**PARTIAL; R1/R2 NOT FIXED; P2 NOT COMPLETE.** Source baseline:
`5bb8bf2119b235467bec88dc5af2c062f612b103`; assigned branch
`feature/xh-p2-projection`, session `xh-p2-projection-b0d0`. The accepted design/ADR
amendment at `36e560f54b55306575dd3a5e2c57ab679feef2a1` was read before code.
No P1 source was copied. Production authority stays DENY; enhanced append stays
disabled. No live source/database, endpoint, GUI or application suite was used.

GATE P2.1 · 2026-09-16 · independent reviewer result supplied in this author task ·
exit criteria: prior 91-case subset and true-overlap IMMEDIATE-vs-DEFERRED oracle ·
verdict: PASS as supplied for `5bb8bf21` only · full P2: NOT CLEARED.
This author re-executed those 91 cases, but did not independently re-run their
DEFERRED mutant. Independent Data/DS/Test **code** review of this v8 delta is pending.

### Change reach and exact boundary

Changed production code is only `SqliteWatcherObservationStore.cs` plus its
coordination DDL partial. Both fresh creation and the additive v8 migration use
the same DDL string and the existing constructor/connection. Exactly three cache
tables are created. Old board IDs, sequence ties and legacy writes are preserved.
The partial has no second connection, database, generic transaction abstraction,
public authority surface or producer callback.

Historical checkpoint wording (superseded by the S1–S6 amendment above): the event
was described as a captured occurrence. The corrected event grain is one logical
admission with its first occurrence, the feed adds distinct immutable occurrence
diagnostics, and the checkpoint is the accounted prefix. Initial/current receipt composite FKs are deferred and
bidirectional; only one initial receipt is allowed. An actual constrained
`parent_application_state` column enforces applied-parent eligibility. A feed
transition moves the current pointer and pairs payload clearing with a tombstone.
Mappings cannot change once present, and terminal content cannot resurrect.

**Not implemented:** an authenticated/scoped source-key producer; raw-byte hash
validation; canonicalization/version admission; native/session effects; the
production receipt lookup/replay API; memory-store semantics; reader capture and
checkpoint validation. The schema does not prove any of these by itself. In
particular, the SQL checkpoint guards require accounted contiguous rows, not that
a file was captured honestly. Raw fixtures are not trusted source bytes.
There is no source-field production writer or UI/compute reader yet. This remains
an unqualified foundation, not a shipped declared-but-unwritten capability.

### Executed receipts

All commands ran in the assigned worktree with explicit session/name/UTF-8.
Tests used the actual `SqliteWatcherObservationStore.Open` constructor and isolated
SQLite files. New fixtures live beneath the test working directory and remove their
own directories. Existing compatibility fixtures were unchanged. No test ran while
an edit lease was held.

Common command:
`dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore
--filter <selector> --logger "console;verbosity=quiet"
--logger "trx;LogFileName=<receipt>.trx" --results-directory .artifacts\p22`.
The first final compatibility/reliability pair used `--no-build`. After tightening
the parent oracle to assert SQLite extended error 787, the final compatibility run
rebuilt the test assembly. Seven raw TRX receipts are committed under
`docs\proofs\p22-cache-evidence`; their generated paths inside the XML preserve
the actual original run locations. They are evidence, not source/authority.

| Receipt | Selection / observed result | Logical exit / tool |
|---|---|---|
| `p22-baseline-red.trx` | Four original replay cases: **0 PASS / 4 FAIL** before source changes | **1**, shell 960 |
| `p22-cache-red.trx` | `CoordinationProjectionTests`: **0 PASS / 22 FAIL**, actual constructor still v7; missing cache tables/version | **1**, shells 970/972 |
| `p22-cache-first-green.trx` | **20 PASS / 2 FAIL**; parent fixtures accidentally failed contiguity before their FK oracle | **1**, shells 974/975 |
| `p22-cache-green.trx` | Corrected oracle plus expanded controls: **32 PASS**, zero failed/skipped | **0**, shell 977 |
| `p22-compatibility.trx` | **129 PASS** = original **91 P2.1** + **32 cache** + **6 Daydream migration**; zero failed/skipped | **0**, shells 981/983 |
| `p22-reliability-remaining.trx` | Entire unchanged reliability class: **3 PASS / 4 FAIL** | **1**, shell 981 |
| `p22-compatibility-final.trx` | Rebuilt final test source with explicit parent FK error **787**: **129 PASS**, zero failed/skipped | **0**, shell 987 |

The joined compatibility/reliability command returned **1**, not success. Its explicit outputs were
`COMPATIBILITY_EXIT=0 RELIABILITY_EXIT=1`. The 129-case selection was the union of
`CoordinationProjectionTests`, `BoardOrderingTests`, `CoordinationReliabilityTests.Post_`,
`MessageBoardTests`, `SqliteWatcherObservationStoreTests`, `McpMatchesTheJsonlPathTests`,
`ContractBoardPostTests`, `BoardPublisherTests`, `SqliteAllocationOverlapTests`, and
`DaydreamPersistenceTests`. Group counts were read from the TRX, not inferred from
test names. No zero-selection or blanket full-Watcher PASS is claimed.

### Claim / oracle ledger

All new cases are in `tests\AiDe.Core.Tests\Watcher\CoordinationProjectionTests.cs`.

| Claim / case group | Oracle | Red / confidence / residual |
|---|---|---|
| Three caches/version 8 | Actual constructor; SQLite schema/version queries | Old constructor RED; current **Verified** |
| Initial receipt/event pairing | Commit either side alone fails; both together commit | Missing-schema RED; paired actual SQLite commit failures **Verified** |
| Twelve immutable-fact attacks | UPDATE/DELETE/REPLACE, raw/digest/version/ID/first/current/state changes raise SQLite errors | Missing-schema RED; refusals **Verified** |
| Four invalid transitions | Duplicate initial, applied regression, remap, wrong admission fail without feed growth | Missing-schema RED; refusals **Verified** |
| Tombstone | Paired transition clears bytes, moves pointer; reinsertion and resurrection fail | Missing-schema RED; current **Verified** |
| Applied parent | Missing/pending parent produces extended FK error; whole child transaction rolls back | Fixture correction retained above; current **Verified**, not native-thread validation |
| Checkpoint | Missing accounted row, backwards update, changed same-prefix digest and REPLACE fail | Missing-schema RED; current **Verified**, not filesystem SOURCE_GAP proof |
| v7-shaped migration | Constructor upgrades a complete v7-shaped fixture; original IDs/ties survive; old/new writes work; reopen does not remigrate | Added after DDL; **Verified execution**, no old-binary rollback or pre-fix RED claim |
| Three rollback boundaries | Deliberate failing SQL after initial/event/checkpoint; explicit transaction rollback leaves all three caches empty | Fault injected and observed; **Verified**, not native-effect/OS-crash proof |
| Pending→applied | First receipt stays pending; current receipt advances to applied | Added after DDL; **Verified execution**, not replay API proof |
| Cross-scope/epoch/state receipt borrowing | Deferred FK commit error **787** | Added after DDL; **Verified execution**, not an authority check |
| Changed occurrence/epoch | Attempt leaves event/feed counts at one | Added after DDL; **Verified SQL refusal**, not stable producer refusal/replay behavior |
| Trigger-removal mutation | Same UPDATE is refused before dropping `coord_feed_update`, then changes the row after removal | Actual destructive mutation on isolated test DB **Verified**; not a full mutation-score claim |

**Correction/control:** two parent test fixtures initially used offset 2 without an
accounted predecessor, so they failed the wrong invariant. The corrected fixture
uses contiguous offsets and requires the expected FK error at COMMIT. This is the
wrong-failure-oracle class; it is recorded here without editing the lessons register,
which this author's scope excludes. The ten expanded cases were added after the
first schema implementation; they are not falsely labelled pre-code REDs.

### SHA-256 identities (executed working bytes)

| Path | SHA-256 |
|---|---|
| `src\AiDe.Core\Watcher\SqliteWatcherObservationStore.cs` | `F712D2CEB543E7DD2269D9D71DF83E99101A72966627BC4A07A4AA8903FA26B2` |
| `src\AiDe.Core\Watcher\SqliteWatcherObservationStore.Coordination.cs` | `C84D78B1B4D31C2BC5B41A8898547182BEECF8BF3DD77AAEFD3C5024F18307A5` |
| `tests\AiDe.Core.Tests\Watcher\CoordinationProjectionTests.cs` (final) | `F63FACD9F50232173FADE4C0110D13B4C82D0098988B1402D9361D7B01FB4F79` |
| `tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj` (unchanged) | `FFAD72D5B9D4E9F1D59FBCCDA771BBBAC648158F3C299FA5BCBC19497734136D` |
| Test output `AiDe.Core.Tests.dll` (final) | `A998BB9AD0B7BEA6ED7CCA9065430D2A69E5A7E1608BB8897E0E1129FD56F918` |
| Test output `AiDe.Core.dll` | `B766C583A46C23FB8292FB0890F09C8C9476DA046694921275A466FAA202C61C` |
| Pre-change `AiDe.Core.v7.dll` (hashed before rebuild; scratch copy removed, not executed as rollback) | `305005F5DADD7FD217FA30C0A41861CD73D4CF162F21B0F24EEC481DBB743B56` |
| `p22-baseline-red.trx` | `C446F8970AB1F787C04053C3B92DA9F77E818C2DB36205105D47BC6A83E73DB5` |
| `p22-cache-red.trx` | `86446EB95E7A85336D6BC382388723C88BE15399FC3E9A36201CF514FD3C85B7` |
| `p22-cache-first-green.trx` | `44690C211E40C22D0D1277F9F511CDF8B81EB4317EC7443EFC00306784510C5A` |
| `p22-cache-green.trx` | `CA6B81C46CF7CC960C1C1705315D3240EA1AA5CEE03E4B62D247F1C8F9847A21` |
| `p22-compatibility.trx` | `FBA6E95C2E1A1C0A3861A59BCC7713DE542D071D314DAC6B98486485C975C23D` |
| `p22-reliability-remaining.trx` | `83F99220E410C295A56823DD98086CD492B00AF62C1E107A94F0C0B8B4D2940D` |
| `p22-compatibility-final.trx` | `2868404E7B587D4297BAD3BF0404D327FD5A1B5E413EA842EF2D84F896E32D90` |

Before that test-only tightening, test source SHA was
`FBA5E3525355E05B89C2B52D0FFD5104D00833C7BCEC3E5654E9F9C5F1AB3D28`
and test assembly SHA was
`87558188B094AC2FD72E88A8C2FE7133D0575B4F65C7DD50B333675B9F8D74AC`.
The production assembly did not change. The remaining-reliability RED receipt was
observed against those prior test bytes and the same final production bytes.

Git normalizes CRLF on the new files. These additional **staged LF byte hashes**
identify the committed copies; the working-byte table above identifies executed
inputs/outputs, not a promise that a checkout retains CRLF.

| Staged path (receipt basenames are under `docs\proofs\p22-cache-evidence`) | SHA-256 |
|---|---|
| `p22-baseline-red.trx` | `3c3b33531a7711f1333973e25ee5f84ab03a644b6f8daa30e51e21f5029db83d` |
| `p22-cache-first-green.trx` | `350b0206981ac39538ce822eb235583c54f75b876eb595ecf196ed1f62d8b37f` |
| `p22-cache-green.trx` | `9058feb3e7e87ab35264393c9e6fd6ca7a1b0d5c92bb9e2e56509097d44dc5db` |
| `p22-cache-red.trx` | `0cf148dd942bf3031a342b60ba5077255dc5bd01b9be8f083290bdf12533cf6f` |
| `p22-compatibility-final.trx` | `12f2595eef06696ec3d2242b2a4b45f90c16e1ad14e0192eac98521cdc73fa30` |
| `p22-compatibility.trx` | `34f3ad5db243d6effc84f9704da4bafce3cac0fcd3fa340507838f5c94a4a7be` |
| `p22-reliability-remaining.trx` | `439a59a2ac083afa9975cd53a79f206a5f5a292457a2811929a86b8bd7d007af` |
| `src\AiDe.Core\Watcher\SqliteWatcherObservationStore.Coordination.cs` | `355682130be26b000b7bb1451d01cf66ee121b7511b03005f3f7acc16991f04d` |
| `src\AiDe.Core\Watcher\SqliteWatcherObservationStore.cs` | `f712d2ceb543e7dd2269d9d71df83e99101a72966627bc4a07a4aa8903fa26b2` |
| `tests\AiDe.Core.Tests\Watcher\CoordinationProjectionTests.cs` | `c6d208e7f815e3783dc7bf6eb0460d383a534f2f1a0c7688c183b9471b15abee` |

Hashes identify local observed bytes, not trusted provenance or cross-machine
deterministic builds. The complete remaining graph is in the phase plan's P2.2
checkpoint: native R1/R2; typed effect/mapping atomicity and failed-delivery replay;
bounded source capture/late parents; 401 paging and canonical bridge; OS crash,
old-binary rollback and query-plan/100× proof; P3 adapters; P4 launcher; P5 surface
SLIs; all six upstream actions AFTER VERIFIED. No floor has been waived.

Schema instrumentation uses the existing normal-path version/applied-at row and
SQL refusal tokens; tests read both state and errors. Native operator latency,
volume, path/failure telemetry remains a named gap. Conductor-owned derived
index/backlink/site/rollup regeneration remains pending. No self-cleared code gate.
The official audit append auto-rendered `docs\audit\audit-data.js`; only that
unowned generated change was restored to the baseline. The append-only audit
entry remains intact. Closing selfcheck observed zero missing goal states and the
same two historical main-budget gaps already recorded by `5bb8bf21`.

## P2.1 allocation-overlap remediation — 2026-09-16

**Test-only candidate; independent Test/Data/DS re-gate pending.** Base
`494b2488fb869846df47d365c5407fd83f8c1556`, branch `feature/xh-p2-projection`.
The supplied independent review found that changing only `deferred:false` to
`deferred:true` survived the existing 90 selected cases. Their green was not proof
of writer acquisition before allocation. This receipt closes that author's missing
oracle, not the independent hard veto or full P2.

### Actual transaction-boundary scheduling

New file: `tests\AiDe.Core.Tests\Watcher\SqliteAllocationOverlapTests.cs`.
Exact test:
`Post_OverlappingSqliteAllocations_AcquiresWriterBeforeReadingMaximum`.
The real path is two `MessageBoardService` instances → two owned
`SqliteWatcherObservationStore` connections → actual allocation/INSERT/COMMIT →
third, read-only SQLite connection → `Seq > cursor`. Production code, schema,
dependencies and existing reader-between-commits tests are unchanged.

The fixture holds A in SQLite's trace callback for the allocator's MAX statement.
B then enters the real allocator. On the candidate, SQLite calls B's **native busy
handler**, proving actual writer contention, rather than a pause before method
entry. The busy handler releases A and waits for the test to read A's committed
message before allowing one native retry. On the weakened DEFERRED path, B reaches
its MAX trace while A is still held; that callback records the violation, releases
A, and waits for the same reader checkpoint. B's SELECT executes after release.
Thus **both genuine writes succeed even in the mutant**; the failure is the observed
allocation-read boundary before serialization, not an accepted SQL exception,
timeout, fake write, duplicated backend or inferred source/IL shape.

Both runs assert exact returned-versus-durable records, IDs
`[message-a,message-b]`, sequences `[1,2]`, A alone between commits, and precisely
B beyond A's cursor. Only after both workers complete do assertions check zero
callback timeouts, no early MAX, and one native busy callback. Callbacks contain
no assertions or explicit throws. Manual waits and async waits have a 15-second
deadlock bound; `finally` releases both gates and awaits both tasks. Callbacks are
unregistered, delegates kept alive through removal, connections disposed, and the
owned database directory deleted. No sleeps, probabilistic workload or retry-until-
green test loop is used.

### Installed contracts and executed spike

| Contract | Local evidence |
|---|---|
| Microsoft.Data.Sqlite 10.0.11 | Pinned package; reflected `SqliteConnection.Handle : SQLitePCL.sqlite3` and `BeginTransaction(Boolean)`; actual store transaction executed |
| SQLitePCLRaw assembly 2.1.12.3116 | Reflected `strdelegate_trace.Invoke(Object,String)`, `raw.sqlite3_trace(sqlite3,strdelegate_trace,Object)`, and `sqlite3.DangerousGetHandle()` |
| Native busy callback | SQLitePCLRaw has no `sqlite3_busy_handler` wrapper. `NativeLibrary.GetExport` found that exact export in installed `e_sqlite3.dll`; test-only Cdecl P/Invoke registered it with SQLITE_OK, observed one contention call, retried successfully and removed it with SQLITE_OK |
| SQLite engine | Actual callback run reported **3.53.3**; installed win-x64 native DLL SHA-256 `B7385D722C83FB52142A00477A726723745916D22A555711EE89834C1111FB2E` |

This is an installed-version execution spike, not upstream documentation research.
Reflection is confined to the fixture's two private connections; its ceiling is
the current single-connection store owner and the allocator's MAX statement. A
connection-owner or SQL-shape change requires updating this scheduling fixture,
not a new production observer/configuration. Other platforms/providers are not
qualified by this Windows x64 run.

### GREEN → exact fault injection RED → GREEN

| Run | Observed result and oracle | Measured time |
|---|---|---|
| New test, candidate build | **1 PASS**, exit 0; `busy=1; earlyMaximum=False; callbackTimeouts=0; committed=message-a:1,message-b:2` | VSTest total **0.6790 s**, displayed test **62 ms** |
| Isolated DEFERRED binary | **1 FAIL**, exit 1; same two successful commits; `busy=0; earlyMaximum=True; callbackTimeouts=0`; failure at line 81: “B reached the allocation SELECT before A committed; writer acquisition did not serialize allocation.” | VSTest total **0.6801 s**, displayed test **57 ms** |
| Unmodified candidate, combined regression selection | **91 PASS**, 0 failed/skipped/aborted; exit 0; new test again reports one busy call, no early MAX or timeout | Runner duration **444 ms**; new test TRX duration **0.0161857 s** |

The 91 cases are the previous 15 ordering + 75 compatibility cases and this new
case. Exact combined filter:

```text
FullyQualifiedName~SqliteAllocationOverlapTests|FullyQualifiedName~BoardOrderingTests|FullyQualifiedName~CoordinationReliabilityTests.Post_|FullyQualifiedName~MessageBoardTests|FullyQualifiedName~SqliteWatcherObservationStoreTests|FullyQualifiedName~McpMatchesTheJsonlPathTests|FullyQualifiedName~ContractBoardPostTests|FullyQualifiedName~BoardPublisherTests
```

Candidate commands use `dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj
--no-restore --filter <selector>`; the combined run adds `--no-build`.
Both use console and TRX loggers with results under the owned worktree's
`.agents\p21-overlap-evidence`. Mutation uses `dotnet vstest
<isolated-copy>\AiDe.Core.Tests.dll /TestCaseFilter:FullyQualifiedName~SqliteAllocationOverlapTests`.
Existing compatibility fixture TEMP/TMP were process-local, rooted in this worktree;
no live database, App/GUI, endpoint or new dependency was used.

The mutant copied the complete built test output **within this worktree**, never
modified source or the candidate binaries, and changed exactly one byte:
method metadata token **100665633**, `AppendBoardMessageAllocated`, IL offset
**38**, file offset **139722**, `ldc.i4.0` (`16`) → `ldc.i4.1` (`17`).
Before patching, reflection resolved the following callvirt to
`Microsoft.Data.Sqlite.SqliteConnection.BeginTransaction(Boolean)`, checked its
sole Boolean parameter, and uniquely matched the complete original method body
in the PE file. The harness required precisely one changed byte, parsed the RED
TRX for one failure with the exact oracle text and successful-commit output,
and checked the original DLL hash remained unchanged. This is a pinned mutation
recipe, **not** a source-string or IL assertion inside the committed regression test.

| Identity | SHA-256 |
|---|---|
| New test source | `C9BD62923980304C02055F460F57FDCAE92AD6157EDA9CD10C955F747F151782` |
| Unchanged allocator source | `52492D886EADA2EA22FB27F82C05EC4DA79F49D90B4F5CE0005822E92DE5F784` |
| Candidate Core DLL | `C8FBD2F84EE9C2C5BABE55821BA786F14F76B5F7C1632BBAF9327B425A766A28` |
| Isolated DEFERRED Core DLL | `FD3108B5DA28F52AF926CCB54FD914896B1030623C6C26B55B7C26BB707531E2` |
| Identical test DLL in both runs | `A604FF231B650F7FA4D783314DC726A5B8221C1CB95AD090E191A92C8FA4F0AB` |

**Class → sweep → derive → prevent:** a scheduling test can serialize calls before
the operation under test and therefore never exercise its claimed concurrency
boundary. Fresh reads of `BoardOrderingTests` and `CoordinationReliabilityTests`
found pre-entry holds in the memory case and R3; neither is promoted to SQLite
overlap proof. The serial fidelity test remains valid. Derive scheduling evidence
from the real engine's trace/contention callbacks, not from another store or
production observer. The new named test kills the exact surviving mutant.
Formal lesson-register incorporation remains conductor-owned; no unowned register
edit was made. The two initial inspection outputs exceeded display size and were
replaced with bounded selections; neither is test evidence.

**Verified:** the local API signatures, callback spike, two successful writes and
read checkpoint in both modes, exact one-byte mutant failure and 91-case GREEN.
**Inferred:** broader scheduler/platform behavior beyond this fixed interleaving.
**Flagged:** independent Test/Data/DS/C# verdicts, full P2 and production observability.
The prior four R1/R2 failures out of the seven-case reliability suite are unchanged
and were not rerun or repaired here. Crash/atomic receipt/checkpoint, replay,
schema/rollback, source gaps, pending bounds, 401-row coordination-feed paging,
outage/rebuild, P3–P5 and authority/runtime activation remain open. No self-clear.

Audit inspection corrected the supplied suspicion rather than rewriting history:
`al-01M2NV7CVPKCN4J7JQKV5TGMA3` and `al-01M2NV7CYXZXK0K90EWYWFS319`
already contain `main_calls:42`, `main_budget:45`, `fan_out:0`, `tier:T2`.
They are not missing budget metadata; this finding is recorded additively in the
new audit summary. Raw cloned binaries/TRX/helper files are local execution
artifacts, removed after this durable receipt; derived views remain conductor-owned.

## P2 real C#/SQLite RED receipt - 2026-09-16, TEST ONLY / UNSHIPPABLE

This is new executed evidence, not a retrospective promotion of the static reports below.
Source base: `6b0c00420609ad54bf36fc38e5025c5629bba814`, branch
`feature/xh-p2-projection`, officially reopened session `xh-p2-projection-b0d0`.
Production code, DDL, dependencies and configuration are unchanged. No P1 work is included.
The checkpoint deliberately commits failing regression tests: **do not join or ship it;
the ordinary full CI selection would include these RED cases**.

### Selected execution and actual assertions

Test class: `AiDe.Core.Tests.Watcher.CoordinationReliabilityTests`, file
`tests\AiDe.Core.Tests\Watcher\CoordinationReliabilityTests.cs`.
SDK `10.0.303`; runtime `.NET 10.0.11`; xUnit `2.9.3`, adapter `3.1.4`;
Microsoft.Data.Sqlite `10.0.11`. All persistence below is a temporary real SQLite file.

| Exact method / case | Outcome | Observed oracle and scope |
|---|---|---|
| `PumpOnce_RegisterAndPost_PersistsOneOriginalMessage` | **PASS** | Real writer -> parser/pump -> ingest/registrar/board -> SQLite: 2 events, 1 session, 1 post, ID `first-message-1`, Seq 1, exact content and author checked |
| `PumpOnce_UnchangedLogTwice_PreservesOneOriginalMessage` | **RED R1**, line 56 | Expected IDs `[first-message-1]`; actual `[first-message-1, first-message-2]`. Original row equality passed; adapter reported 2 board posts and 1 duplicate register. Same unchanged log, same pump |
| `PumpOnce_ReopenedComposition_PreservesObservationsAndLifecycle(ended: false)` | **RED R2**, line 91 | Snapshot equality before replay passed after reopening. Replay kept one session ID but changed generation 1 -> 2, heartbeat 1000000 -> 1001000, message count 1 -> 2; IDs became `[first-message-1, restarted-message-1]`. Ended stayed false |
| `PumpOnce_ReopenedComposition_PreservesObservationsAndLifecycle(ended: true)` | **RED R2**, line 91 | Same generation, heartbeat and duplicate-message changes. Ended was true before and after the full replay: replayed session-end masks the intermediate clear |
| `Apply_ReplayedRegisterOfEndedSession_PreservesEndedGenerationAndHeartbeat` | **RED R2 phase**, line 120 | After real writer/pump registration+end and a DB reopen, replay only the parsed register into fresh ingest: same session ID, generation 1 -> 2, heartbeat 1000000 -> 1001000, ended true -> false. Zero board messages on both sides |
| `Post_TwoServicesSerially_CursorSeesSecondCommittedMessage` | **PASS** | Separate service/store instances, independent SQLite reader: A Seq 1, cursor 1, B Seq 2, 2 durable rows; next read contains exactly B. Scheduling proxy forwards real persistence and returned message equals reader result |
| `Post_TwoServicesAllocateBeforeCommit_CursorSeesLaterCommittedMessage` | **RED R3**, line 198 | B allocated Seq 1, held before INSERT; A allocated and committed Seq 1; reader saw only A and advanced cursor to 1; B then committed Seq 1. Independent reader found both durable rows by ID/count, but `Seq > 1` returned `[]`, expected `[message-b]` |

Executed selection: **7 total, 2 PASS, 5 RED, zero skipped**, test-run time
**0.8732 seconds** (runner measurement, not build or end-to-end elapsed time), exit **1**.
Each RED reached the stated assertion, not a timeout, parse refusal or dependency failure.
Full console receipts remain in this Copilot turn (PowerShell execution **829**); the
table records the durable, path-independent result rather than committing local temp paths.

```powershell
dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter 'FullyQualifiedName~CoordinationReliabilityTests' --logger 'console;verbosity=detailed' --verbosity minimal
```

Prerequisite history is explicit: two initial `--no-restore` invocations returned zero
without creating assets/binary or executing tests; neither counts as evidence. An offline
restore using only the existing local NuGet package cache materialized assets. No packages
were downloaded or dependencies added. The first real build then failed existing analyzer
`xUnit2031`; the serial control was corrected to use `Assert.Single(collection, predicate)`.
The subsequent build and seven-case run produced the results above. Existing build-order
references also built Core/MCP/Daemon and test helpers; **no daemon/helper process, App,
native/UI test, GUI, endpoint, live queue or full suite was run**.

### Fidelity, bounds and claim limits

R1/R2 use the real `CoordContractWriter`, `CoordContractLogPump`, `InjectedContractIngest`,
`IngestHost`, `TrustedRegistrar`, `MessageBoardService` and `SqliteWatcherObservationStore`.
Only existing clock/capability fixtures and deterministic ID factories are supplied.
Every fixture owns a unique temp directory and disposes connections before deleting it;
cleanup errors are not swallowed. Fixed wall time and explicit monotonic-clock advance
make lifecycle changes observable without sleeps.

R2 is a **process-composition restart**, not an OS process kill: the first store is
disposed and new store, registrar, board, host, ingest and pump open the same DB and wire
log. Exact snapshots compare persisted session IDs/count, generation, heartbeat, ended
state and full board observations/IDs. The separately named registration-phase case
does not pretend that the full replay leaves ended false. Neither case proves
authorization rejection, capability non-minting, crash atomicity or a repaired lifecycle.

R3 uses two real SQLite store connections and a third independent reader. A test-only
`DispatchProxy` forwards every store call unchanged and holds **only B**, after its
`BoardMessage` allocation and before the real `AppendBoardMessage` INSERT. B signals
allocation through a TCS; the reader first asserts no commit, then A commits while B
is held. A manual event releases B only after the reader advances. Both waits have
explicit **15-second deadlock timeouts**, and `finally` releases and awaits B even on
assertion failure. The serial positive case is the proxy's persistence-fidelity pair.
No persistence is mocked, no production source is patched to manufacture interleaving.
The cursor predicate is tested over the real store's committed rows; this is **not**
execution of MCP/UI pagination or the future coordination-feed cursor API.

Class -> sweep -> derive -> prevent: R1/R2 expose durable replay routed through
non-durable ingest identity; the scoped source sweep also found the register adoption/
generation/heartbeat/end chain. R3 exposes instance-local allocation of a shared
committed sequence; both service instances reach the same ordinary INSERT seam.
Future controls must derive identity from canonical source receipts and allocate native
Seq inside the store writer transaction, not synchronize multiple service counters.
These named tests are now observed RED controls, **not fixes**. Wider sibling sweep,
lesson-register incorporation and all production prevention remain with the admitted
implementation/conductor. The local xUnit assertion-shape correction is controlled by
the existing failing `xUnit2031` build rule, not a new analyzer or suppression.

### Immutable execution identities

SHA-256 hashes below were read after execution. Source/project files are unchanged from
the source base above; the test hash identifies the new test content. Binary hashes
identify this local Debug build, not a claim of cross-machine reproducibility.

| File | SHA-256 |
|---|---|
| `src\AiDe.Core\Watcher\CoordinationContractLog.cs` | `C9FDB639D1F2DED724D08AAA8FFEAA25DE4010E89FE4D28E900E5EDC2E5F2F2F` |
| `src\AiDe.Core\Watcher\CoordinationContract.cs` | `5198CD119D5079E91FD130774981593BD09E8D8D03D530EC3A50F4BD959CFBAA` |
| `src\AiDe.Core\Watcher\MessageBoard.cs` | `B9843DFD7E1DF08DEFD24E958E2D9D58B61FC76465A570DF2D48C46F1E33E005` |
| `src\AiDe.Core\Watcher\WatcherObservationStore.cs` | `725EDFAD68B9DE35489889F7FDC9F5A024A3457D0827FAC82E0B22B433160D89` |
| `src\AiDe.Core\Watcher\SqliteWatcherObservationStore.cs` | `462188EC8116302E1B5A0FBDB1BAF01BAAB35FBC2A7449021EF3988D2E7E0816` |
| `src\AiDe.Core\Watcher\IngestHost.cs` | `EADC24CB644BC8C2155A9182962DA9B4462D2B7340297B48DFF2EE987B5BFFB8` |
| `src\AiDe.Core\Watcher\TrustedRegistrar.cs` | `AE8B3BAAC979A87512DA8DD2492F28ADC9510D9A2C71682936B166971BD114DC` |
| `tests\AiDe.Core.Tests\Watcher\WatcherTestDoubles.cs` | `449C16CAA5D03B8A034E5DA92B4E6161FC1D1A17DC1B28963A66578951F353E0` |
| `tests\AiDe.Core.Tests\Watcher\CoordinationReliabilityTests.cs` | `780883C180E4B24BA5037A143DA298CDD4D51C52CE91E6B58096701AD33E48A4` |
| `tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj` | `FFAD72D5B9D4E9F1D59FBCCDA771BBBAC648158F3C299FA5BCBC19497734136D` |
| `src\AiDe.Core\AiDe.Core.csproj` | `B63C41B97201DC3E1016B72B404A32411AC31E477231B281219DDA64D0AD584A` |
| `Directory.Packages.props` | `A57D2EE58BD9787119F5D71C07B956A72F986FEF30287647AABCA25212E95F64` |
| `tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.Tests.dll` | `5D04FC1BECC273CDF5E0485711B637A57BF0819D52386073FCB2882B751EE0CE` |
| `tests\AiDe.Core.Tests\bin\Debug\net10.0\AiDe.Core.dll` | `CD037D365519BF085611C03024474EDD50717095EFD05C0D8E8085898AAB304D` |
| `tests\AiDe.Core.Tests\bin\Debug\net10.0\Microsoft.Data.Sqlite.dll` | `4ABD9C2A61E580EB853E93CA8953A3CEF2C05714AE28D2D1859D4DBC5E5700BC` |

**Verified:** the seven outcomes and the stated SQLite observations. **Inferred/static:**
original reports remain historical; other mechanisms are not promoted by these runs.
**Flagged:** all remaining P2 floors, independent design/schema approval and solution
admission. Existing `watcher.db`, three additive cache tables, canonical `.agents`
requests as sole truth, future indexed native MAX+1 **inside** the writer transaction
without historical dedup, raw source keys and atomic effect/receipt/checkpoint,
overflow-before-parent/accounted quarantine/cursor are still future contracts.
No schema, transaction, crash, forbidden-mutation, rollback, rebuild, source-gap,
100x query-plan, 401-row paging, limit+1/outage, tombstone/version or endpoint floor is
cleared here. O11/O19 and native O14 have **partial RED evidence**, not full completion.
Prior Data/DS BLOCK findings stand. Next: independent Data/DS schema/transaction
approval, then admitted implementation with **all** P2 floors; no reapproval of P0-P5.

Derived views and lesson incorporation remain conductor-owned under the existing
checkpoint protocol. This author does not claim a clean site/index gate. No AIDE
contract-log/session environment was supplied, so no live episode/board event was sent.

## P2 design correction receipt — 2026-09-16, independent gates NOT cleared

**Docs-only Data/DS scribe.** Officially registered session `xh-p2-projection-b0d0`,
branch `feature/xh-p2-projection`, tree `C:\Projects\ai-de-feature-xh-p2-projection`,
exact base `f4109e144d4c56d16b4548013e948d0ba1f50e36`. P1 author's tree and pending
semantics review are untouched. Original P1 evidence below is historical input, not
this scribe's execution. Only design §3, ADR P2 addendum, P2 proof/gates/oracles and
phase plan are authored, plus own append-only audit. Inherited typed links remain;
derived/site/lessons incorporation belongs to conductor, no regeneration here.

### Fresh scoped source contracts at the exact base

Paths below are relative to `src\AiDe.Core\Watcher` unless otherwise stated.
**Verified means read source; no runtime reproduction or SQL execution occurred.**

| Source / lines | Observed contract | What remains Inferred / unperformed |
|---|---|---|
| `MessageBoard.cs:85–175` | Instance lock; fresh GUID; BoardMessages.Count+1; current registrar capability check | Real two-service sequence race and repaired allocation/returned-message behavior |
| `SqliteWatcherObservationStore.cs:546–588` | Native append persists caller Seq; board read orders by Seq | Non-deferred writer acquisition, atomic effect/feed/checkpoint, future indexed MAX+1 |
| `CoordinationContractLog.cs:234–269` | Top-directory files read whole; synthetic newline added; every parsed event reapplied | Raw complete-record capture, replay one-effect and bounded read behavior |
| `CoordinationContract.cs:259–305,576–654` | Sorts `(At, externalSessionId, Seq)`; non-string attrs become null; board posts call service; registration map in adapter | Pre-lossy canonical capture; durable observation mapping after restart/end |
| `TrustedRegistrar.cs:65–119`; `IngestHost.cs:109–136` | Registration chooses next generation for an existing terminal; Issue publishes capability then writes session, clears ended and updates heartbeat | No replay capability mint; exact native first-registration prepare/write/postcommit seam |
| `WatcherIdentity.cs:85–111` | Canonicalise uses backslash identities, trailing-separator rule and Windows-only invariant lowercase | Source-file alias/handle/coherent-snapshot qualification; no new identity rule invented |
| `src\AiDe.Mcp\BoardTools.cs:76–112`; `BoardPublisher.cs:70–88` | sinceSeq filtering followed by newest-N; tombstones omitted; publisher is bounded snapshot | Future insert-only native incremental guarantee, not tombstone/history completeness |
| `SqliteWatcherObservationStore.cs:17,32–49,1057–1084`; `WatcherHost.cs:51–73` | SchemaVersion 7; foreign keys/recursive triggers enabled; current >= version returns; actual host opens watcher.db | Actual v7 binary opening additive v8 and preserving new facts through rollback/re-enable |

### Supplied independent DS findings and corrected proposal status

| Finding | Correction recorded in design §3 / ADR | Independent status |
|---|---|---|
| DS1 native two-instance ordering | Future allocation inside store writer transaction, returned allocated message, earliest-N incremental read; preserve historical duplicates | Prior BLOCK retained pending Data/DS + O14/O18 |
| DS2 overflow child before parent | Capacity-deferred source reference + receipt/checkpoint, continued scan, fair recovery including exhausted attempts, no unbounded payload copy | Prior BLOCK retained pending Data/DS + O16/O20 |
| DS3 source identity evidence | XHK/1 injective tuple; explicit logical epoch; full-prefix snapshot once/pass; same bytes preserve identity, mutation/truncation SOURCE_GAP; finite discovery/read ceilings | Prior BLOCK retained; actual snapshot/exclusion and normalization goldens not established |
| DS4 registration replay authority | Historical observation only; no RebindObservedRegistration API or wrapped Register; native lifecycle evidence required | Prior BLOCK retained; atomic first-registration durable mapping/publication unresolved |
| DS5 schema invariant gap | Three-table one-way FK candidate; actual parent discriminator; feed immutable, current state mutable; raw SQL oracle exposes missing initial receipt enforcement | **BLOCKED**, not waived: initial-receipt and state/feed pairing not store-enforced |
| DS6 duplicate and cursor semantics | Original ADMISSION plus separate CURRENT; stable conflict refusal; last-returned continuation; semantic same-watermark rebuild | Pending independent Data/DS + O09/O12/O18 |
| DS7 rollback contract | Correct old current>=7 behavior; actual old-binary/new-facts/re-enable requirement retained | Pending independent Data/DS/Release + O15 |

**GATE P2-DS-prior · 2026-09-16 · supplied independent DS findings · verdict:
BLOCK; corrected proposal recorded, not independently cleared.**
**GATE P2-schema · 2026-09-16 · independent Data/DS pending · verdict: BLOCKED;
initial-receipt constraint, snapshot evidence and native registration seam unresolved.**
**GATE P2-runtime · 2026-09-16 · Test/Data/DS/Release pending · verdict: NOT RUN;
real C# RED, transaction, raw SQL, migration/rollback and query-plan evidence absent.**
These are statuses, not fabricated reviewer PASS receipts. P2 test-only RED authoring
is allowed; no solution code admission follows from this docs commit.

### P2 oracle refinements — every row NOT RUN / RED not observed

These specialize the original catalogue, not reduce it. Future tests must report exact
selected names/counts, fixtures and asserted database state; Python diagnostics or
registration-count-only assertions cannot qualify C# behavior.

| Oracle | Falsifying fixture / required assertion |
|---|---|
| O09 / O12 | Equal enhanced key/full canonical bytes after pending→applied returns original admission receipt/mapping plus distinct current state. Lost ACK/uncertain commit retries same identity. Changed canonical bytes at a new offending occurrence gets one stable conflict refusal; repeat it, row counts stop growing; original state/mapping never overwritten. Changed accepted-position bytes cause SOURCE_GAP |
| O11 / O19 | Real SQLite + real register/post writer/pump: two pumps leave one effect/receipt. Restart after end, then replay: same observation ID/generation, ended and heartbeat unchanged; no new capability. Native effect without current trusted lifecycle evidence cannot apply |
| O13 | Crash before every durable step and immediately before commit: no partial effect/application/feed/checkpoint. Crash after DB commit before process publication/ACK: original receipt survives; native first-registration mapping is atomic and does not mint a replay capability |
| O14 | Two distinct services, store connections and barriers: A holds non-deferred writer, B waits; reader between commits sees A; after B commit next page contains B. Test native future Seq allocation/returned message separately from coordination feed n; preload historical duplicate native Seq and preserve them |
| O15 | Actual WatcherHost deployer expands representative v7; insert new accepted facts; actual old binary opens/writes against v8 without version downgrade; re-enable and compare semantic source/mapping/tombstone state at same watermark. Do not compare regenerated feed n/retry schedule; no DROP/dedup |
| O16 / O20 | Fill active pending to each item/byte bound; overflow child occurs BEFORE parent in canonical input. Commit visible reference-only deferred receipt/checkpoint, reach parent, recover children across restart in bounded fair batches, including attempts exhausted; tombstoned applied parent identity remains valid |
| O17 | Same-byte physical replacement keeps identity; altered/truncated accepted prefix, disappearance, path alias ambiguity and unsupported version are explicit. Capture full raw bytes before lossy attrs/timestamp sorting. Unterminated tail never advances; malformed/oversized complete line remains accounted, not ignored |
| O18 | 401 unread transitions paginate 200/200/1, then repeat with >401, interleaved other-repo feed n and tombstones. Read page/highwater in one snapshot; continue from last returned, not highwater. Epoch/version/scope mismatch resets explicitly; native sinceSeq separately returns earliest N |
| O20 limits | Every item/byte/retry limit at limit-1/limit/limit+1: 128 files (129th detected), 32 MiB snapshot, 128 records/4 MiB page, 64 KiB record, active 1024/16 MiB, 64 retries/pass, 8 attempts/cycle. Bounded reader proves bytes/rows visited and no prefix reread per page. Caps are proposed pilot fixtures, not measurements |
| O20 outage | DB busy, indefinite missing parent, exhausted attempts, reserve exhaustion and actual injected disk-full: no accepted obligation lost; no false ACK/checkpoint. If reference/status cannot commit, scanning stops; resume only after storage restored, never a hidden second spool |
| SQL / O13 / O16 | Execute candidate DDL in isolated real SQLite; FK/recursive triggers read back. Attempt feed UPDATE/DELETE, source identity rewrite/DELETE/REPLACE, changed established mapping and applied-parent demotion; each forbidden mutation must fail. Actual discriminator/composite FK rejects cross-scope/non-applied parent and applied child with missing parent |
| SQL gap / schema gate | Raw transaction INSERT event then COMMIT without initial feed; raw state change without transition. One-way candidate permits the gap: observe it and keep Data BLOCK until concrete store-enforced resolution. If revised to a cycle, prove named first_receipt_n initial receipt and later transition insertion |
| Index / boundedness | EXPLAIN QUERY PLAN + 100x cardinality, actual rows/bytes for source/admission/cursor/parent/due queries. Assert per-pass prefix capture rather than O(pages × prefix). No unmeasured O(new bytes) claim for arbitrary prefix mutation detection |
| Canonical goldens | Python/C# exact XHK/1 bytes: delimiter-containing text, empty field, non-ASCII, field/type/length distinctions, integer boundaries, platform path cases, invalid Unicode, digest versions. Full pre-lossy semantic JSON goldens distinguish null/absent and typed values; P1 parallel semantics not assumed settled |

Change reach: official accepted bytes/native log observation -> raw capture -> source
key/PreparedEffect -> existing watcher store transaction -> feed/receipt/checkpoint ->
projection DTO/cursor -> native MCP incremental read or coordination reader -> P5 UI.
This turn changes only the four governing docs, not any listed implementation surface.
Field writer/compute readers remain design §3's grain table plus P2-A–F: identity/digest
serve equality/conflict; parent fields serve FK/recovery; state/attempt/due/active bytes
serve scheduling/budget; feed n/outcome/mapping serve admission/cursor; checkpoint
serves bounded recovery. No live capability is stored in observation mapping.

**Scope/process receipt:** no agents, solution code, test authoring, runtime tests,
live DB/GUI/endpoints, dependencies, hooks/config or upstream research. No graph exists
in this own tree; inherited typed links are the bounded grounding path
design -> spec/ADR -> proof -> plan. Source reads are at the pinned base, not histories.
Two oversized tool outputs required paging; an unavailable shell `rg` attempt was
replaced with available tools. These are process overhead, not product RED.
The initial-receipt, coherent-snapshot, native-registration and live privacy gaps are
explicitly unresolved. Full design DoD items 2/6/7/15/18 (enforced model, executed
contracts, independent pattern review, rollups and veto clearance) are not claimed met.
Append-only official audit helpers avoid the CLI renderer; no derived file is authored.
`AIDE_SESSION` and `AIDE_CONTRACT_LOG` are absent, so episode capture is unavailable;
no evidence path under `docs\proof` is invented for this existing `docs\proofs` artifact.
The worktree is retained for independent review/integration because its commit is unmerged.

## Current receipt — four-finding repair, 2026-09-16, Python peer author

**Outcome: F1–F4 repaired and regression-tested in the dormant P1 candidate;
independent re-review pending. Full P1 and runtime activation are not cleared.**
The author reopened official session `xh-p1-responses-b0d0` in the existing
`C:\Projects\ai-de-feature-xh-p1-responses` tree, branch `feature/xh-p1-responses`.
Repair baseline is `ec85de0be8713bbd20d2b2035df2c4c5bf4c8126`.
The limited P0 gate at `62af66ca98ef0fed810b6b07d59f79f2b227176a` remains
**dormant-code-only**. This record does not promote it to full P1 approval.

### Exact scope, surfaces and implementation

The finite worklist was the independent review's four findings, not a new investigation.
Inputs remain original JSONL occurrences; derived folds are not another stored status.
Surfaces reached: synthetic raw ledger → official `read_request_events` →
`_request_event`/`validate_response` → `fold_requests`/`_fold_responses` →
official subprocess `collaborate summary|check`, JSON and text modes.
The existing actionable by-ID CLI and real pinned old-worktree rollback test also ran.
No database, WPF, launcher or endpoint adapter was changed.

* **F1:** duplicate legacy adds compare sorted JSON serialization instead of Python
  dictionary equality. Nested booleans, integers and floats retain distinct serialization;
  object-property ordering remains immaterial and array order remains significant.
  Neither input dictionaries nor original raw history are rewritten.
* **F2:** `_is_enhanced_record` is the shared append/reader/fold discriminator:
  a `schemaVersion` key, regardless of its value, or a string `kind` beginning
  `coordination-v` reserves an envelope for strict validation. Missing, changed,
  unsupported or malformed discriminators on that path refuse `XH.SCHEMA_INVALID`;
  the disabled append path refuses `XH.ENHANCED_DISABLED` before filesystem effects.
  Genuinely markerless legacy extensions remain tolerant. An `eventType` alone is
  not an enhanced marker: reserving every extension name would break that tolerance.
* **F3:** invalid request-ledger records or fold conflicts terminate both collaboration
  commands with logical exit **4**. JSON emits `status: not-checked`, stable `code`,
  `request_errors`, and measured `duration_seconds`; it emits no partial `requests`,
  `findings` or `active_sessions` claimed as checked. Text mode emits the refusal on
  stderr, not an uncaught traceback or `check: OK`. Valid-path output is unchanged.
* **F4:** production correlation logic was already rejecting mismatches. Its missing
  control now independently changes repository, stream, thread, obligation and causation,
  in both event orders. Each retained original reply has `XH.CORRELATION_MISMATCH`,
  unanswered/remaining true, latest disposition/next null, and acceptance/execution false.

### Executed receipts and immutable source pins

All rows below are **Verified by execution**. Counts distinguish selected test methods
from failing subtests. Raw receipts are local, reproducible files under `.agents`;
this committed table preserves their results even when those local files are absent.
The five run-output files are removed during final cleanup rather than committed as
additional authored files. Every per-test fixture was torn down after its run.

| Run / tool receipt | Source | Selected tests | Failing subtests / errors | Logical exit | Measured elapsed |
|---|---|---:|---:|---:|---:|
| Unchanged candidate baseline, shell 683, `.agents\xh-p1-repair-baseline.txt` | `ec85de0b` | 19 | 0 / 0 | 0 | 0.781 s |
| Final RED, shell 687, `.agents\xh-p1-four-findings-red-final.txt` | unchanged `ec85de0b`, new tests | 27 | 21 / 0 | 1 | 1.220 s |
| GREEN, shell 689, `.agents\xh-p1-four-findings-green.txt` | fixed source | 27 | 0 / 0 | 0 | 1.287 s |
| Guard-False mutant, shell 691, `.agents\xh-p1-four-findings-mutant.txt` | fixed source; function mutated in memory only | 27 | 10 / 0 | 1 | 1.218 s |
| Pristine discovery after mutant, shell 692 | fixed on-disk source | 27 | 0 / 0 | 0 | 1.225 s |

`git diff --check` also returned 0 in shell 692. The eight new methods and their
oracles are listed below. Existing 19 methods were retained.

| Control (`ResponseTests` method) | Claim and falsifying oracle | RED evidence / remaining limit |
|---|---|---|
| `test_Fold_NestedJsonTypes_ConflictingAddsRefusedInEitherOrder` | F1: nested `true/1`, `false/0`, `1/1.0`, each in both orders, must raise `XH.EVENT_CONFLICT`; raw bytes and parsed inputs unchanged | 6 failing subtests on `ec85de0b`; GREEN. Does not qualify cross-language canonicalization |
| `test_Fold_ReorderedObjectProperties_AreLegitimateDuplicates` | Object key permutations plus resolve yield exactly one resolved row; original raw file unchanged | Positive preservation control, six permutations; passes before/after, not separately claimed RED |
| `test_ReadAndFold_ReservedMarkers_ExplicitSchemaRefusal` | F2: 13 malformed/unsupported discriminator cases reject at reader and direct fold; raw file unchanged | 6 failing subtests on `ec85de0b`; other cases already rejected. GREEN reaches both direct fold modes for every case |
| `test_ReadAndFold_UnversionedLegacyExtensions_StayTolerant` | Markerless unknown fields/types and extension payload survive; ordinary request stays open | Positive preservation control; passes before/after, not separately claimed RED |
| `test_Append_ReservedMarkers_RejectBeforeFilesystemEffects` | Every reserved marker refuses, with no parent directory/file created | 1 failing subtest on `ec85de0b`; GREEN. No live enhanced append performed |
| `test_Cli_CollaborateConflictingAdds_ExplicitFailureWithoutPartialState` | F3: two actions × JSON/text, stable exit 4 and no traceback/partial checked state | 4 failures: old CLI exit 1 with traceback instead of structured refusal; GREEN through real subprocess |
| `test_Cli_CollaborateUnsupportedEnvelope_ExplicitFailureWithoutPartialState` | F2/F3: unsupported envelope must not disappear into a successful partial summary/check | 4 failures on `ec85de0b`; GREEN through real subprocess |
| `test_Fold_EachCorrelationMismatch_RetainsReplyWithoutAnswering` | F4: all five context dimensions, each independently changed in two event orders | Original guard passes; guard-False mutation yields exactly 10 failing subtests, zero errors |

The earlier pre-P1 run's **22 failures + 22 errors** is historical evidence, not the RED
oracle for these new assertions. In particular, its 17 unsupported-enhanced-API errors
cannot establish any new semantic assertion. This repair's final RED has **zero errors**.
An initial repair-test run (shell 685: 24 failures) included three cascading fixture
failures: a first failing append created the directory shared by subsequent subtests.
Each case now owns a distinct `absent-<index>` path. That test-only correction preceded
the final RED and is not counted as three product defects.

| Pin | Value |
|---|---|
| Supplied pre-P1 parent source Git blob, verified via `ec85de0b^:<path>` | `b2ed495fcf4b6332ddf517aee17144173ac5b96b` |
| Actual repair-baseline source Git blob, verified via `ec85de0b:<path>` | `be4767487c8af98ed9b1468ab8dfb48c5daa9f44` |
| Repair-baseline source SHA-256, executed bytes | `0f16846da8664a3e39bc1eef42d143d04e1f71405210977034be701897bc937b` |
| Fixed source Git blob | `ee3981c854c857884d2af324d7cc0d1de1efdad4` |
| Fixed source SHA-256, executed and Git/LF bytes | `67b6db01231a1f695a1b1e35390530f76681b714996bcc574f1e3b0e91475c77` |
| Final tests Git blob | `e7fdcc8c55fe97611f5a2a1891f09567c9ee5051` |
| Final tests SHA-256, executed Windows bytes in RED/GREEN/mutant | `beba2b5e54683ee3e167d990bb971ac42973294cab85f871c3cdf0fd9d481360` |
| Final tests SHA-256, normalized Git/LF bytes | `705ffb2237a60e777899747593c8278f6e71ca4c1540a447528a4bc1d61b7f36` |
| Unmodified compatibility client commit / source SHA-256 | `94ec9036dd0b72aa5b759badcf21a9e3aba6659b` / `f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf` |

Source path: `docs/ai-forward-pack/scripts/coord-core.py`; tests path:
`docs/ai-forward-pack/scripts/tests/test_coord_responses.py`.
The LF and executed test hashes differ only by checkout line endings; neither is
presented as the other. The compatibility method
`test_Cli_PinnedUnenrolledLegacy_ActualLinkedTreeAndRollback` executes the unmodified
client's actual add→resolve→list from an unenrolled linked tree, compares IDs/payloads
with the upgraded reader, then replaces upgraded files with pinned originals and repeats.
It does **not** prove mixed-client contention, complete-record atomicity or live authority.

Reproduce the candidate and discovery receipts with the existing official test entry:

```powershell
python docs\ai-forward-pack\scripts\tests\test_coord_responses.py --receipt .agents\xh-p1-four-findings-green.txt
python -m unittest discover -s docs\ai-forward-pack\scripts\tests -p test_coord_responses.py -v
```

The mutation run imported that same test module and real official helper, parsed only
`_fold_responses` with stdlib `ast`, selected the **single** `if` whose test contains
all five correlation field names (asserting exactly one match), replaced its test with
`ast.Constant(False)`, compiled the function back into the isolated imported module,
and ran all 27 methods. No production file was edited. All 10 failures belonged to
`test_Fold_EachCorrelationMismatch_RetainsReplyWithoutAnswering`: each field listed
above failed in both `[request-add, coordination-v1]` and reverse order. This is an
observed killed mutant, not a mutation-score claim or a claim about subprocess mutation.

### Class → sweep → derive → prevent; gate and remaining work

| Class | Bounded sweep / derivation | Control |
|---|---|---|
| Host-language equality erases wire types | Legacy duplicate comparison, enhanced canonical bytes and resolution tie-breaking inspected; enhanced bytes already preserve JSON types | F1 nested-type permutation test; use sorted JSON rather than dict equality |
| Marker detection differs across admission and reading | Append, `_request_event` and fold all inspected and moved to one predicate | F2 reserved-marker reader/fold/append matrix; tolerant legacy positive |
| A throwing fold escapes a CLI status boundary | Both collaborate actions/modes inspected and exercised; request-list already catches fold conflicts | F3 actual subprocess tests and explicit not-checked output. Request-resolve's separate pre-existing conflict path is outside this four-finding repair and not qualified here |
| Composite guard lacks independent negative cases | Five correlation operands and both event orders enumerated; positive correlated response remains covered | F4 10-case killed guard-False mutant |
| A failing subtest contaminates later filesystem cases | New append cases swept; shared absent directory replaced by per-case path | Final RED count excludes three cascading fixture failures |
| A lower-level helper has a different root contract from its CLI | Initial audit append used repository root, but the official helper takes the docs root; status read-back found only this run's two untracked entries | Shell 703 asserts the resolved canonical path, exact two owned IDs, original byte-prefix preservation and appended event equality; then removes only the stray files |

These controls and the bounded class sweep are recorded here because the author does
not own the lessons register or site surfaces; conductor owns any register incorporation.
D0/D1/D2/D4/D5-provider/D6 apply to this change. No dependency or API installation was
needed. Failure telemetry was read back from real CLI JSON: stable refusal and measured
duration are emitted by the ordinary failure path, not a debug switch.

**GATE F1–F4 repair · 2026-09-16 · Python peer author · author evidence complete;
independent Security/Test/DS re-review PENDING · no author self-clearance.**
No enhanced writer, send, grant, transfer or launcher was activated.
Remaining: P1 acceptance/consumption/proposal-supersession/full authority and proposal
verification, cross-language digest vectors and unchanged mixed-client qualification;
P2 real SQLite atomicity/migration/rebuild; P3 pinned real endpoint spikes and
arrival/consumption/wake receipts; P4 qualified launcher provenance; P5 cross-surface
conformance and measured SLIs. User approval for all phases remains; this repair
does not request it again. Deferred upstream reuse is recorded in the phase plan only.

Finalization uses the existing audit module's `next_id`, `consume_start`,
`duration_fields` and `append_log` helpers. Its `cmd_append` unconditionally renders
derived pages; using the append-only helpers honors the conductor's explicit prohibition
on derived/site writes. No audit implementation is changed. `AIDE_CONTRACT_LOG` was
absent, so no episode-close capture could be emitted; no evidence path was invented.
The worktree is retained for independent review and integration, not removed while
it carries the only unmerged repair commit.
Official graph audit `al-01M2NPCVC7N695D3BN42ZWZJVF` and repair audit
`al-01M2NPCVC8FYZM78P6ZFGM61G6` are in the canonical `docs/audit/audit-log.jsonl`.
The corrected repair append consumed the original 17:54:38Z start marker and records
**792.0 measured seconds**, excluding the earlier reads as stated in the phase plan.
The audit reports **29/35 calls through its corrected append**; later claim release,
commit and final state verification are lifecycle calls, not unrecorded product work.

## Prior receipt — 2026-09-16, original dormant P1 author (historical)

The P0 account below is historical. This section supersedes its pending-P0 status, not
its unperformed runtime gates. Supplied conductor gate, not authored self-clearance:

**GATE P0-delta · 2026-09-16 · Test/DS · unchanged unenrolled old clients and exact
pinned O08/O10 oracles, dormant-only zero authority, additive existing watcher.db ruling ·
PASS FOR DORMANT P1 ONLY · F1 resolved; authority/runtime floors BLOCKED.**

**Outcome: partial programme delivery, reviewable dormant response-only candidate.**
Worktree `C:\Projects\ai-de-feature-xh-p1-responses`, branch `feature/xh-p1-responses`,
official session `xh-p1-responses-b0d0`, base
`62af66ca98ef0fed810b6b07d59f79f2b227176a`. Created by the official registered worktree
command; no installation/configuration/hooks changed. Source/tests and this Proof Pack/
plan are the only authored surfaces. Audit is append-only. Derived pages and rollups
remain conductor-owned. No source changes in the conductor, main or primary checkout.

### Implemented seam and explicit narrower boundary

`coord-core.py` is still the sole official helper. No extra module or status database.
`read_request_events` → `fold_requests` → `cmd_request` is the exercised reader path.
Ordinary legacy commands retain their arguments and JSON shape. Added
`request list --id <original-id> --actionable --json` reads all statuses for that ID.
The new read is opt-in. It returns original response envelopes, refusal codes, latest
applicable disposition and separate unanswered/remaining/accepted/execution fields.
Production supplies **no** generation fixture map, so typed replies remain visible but
cannot mark an unverified generation answered. A legacy resolve is not typed acceptance.

The dormant schema admits **response-recorded only**, with a closed disposition set,
explicit endpoint generations, exact `(proposal id, revision, sha256)`, correlation,
response supersession and semantic digest. This is not the full immutable AuthorityRef
or ProposalRef verifier. Missing full commit/path/blob proof is not silently approved.
Proposal-superseded, proposal-accepted and recipient-consumed event implementations remain
pending. Response supersession is supported; proposal revision supersession is not.

Synthetic fold callers can supply a generation map to exercise deterministic semantics;
that map is explicitly **not authentication** and is never supplied by the CLI. Every
folded acceptance/execution flag is false. `append_record` rejects enhanced/schema-versioned
records before opening a file, regardless of synthetic verifier content. There is no
activation flag, grant/transfer operation, endpoint send or launch implementation.
Concurrent enhanced attempts all refuse. No enhanced event was appended to a live stream.

Legacy duplicate/reordered add and resolve cannot reopen a resolved request. Unequal
duplicate legacy adds refuse rather than replace. The reader rejects non-object JSON,
duplicate keys, invalid IDs/timestamps, non-finite constants and invalid UTF-8, without
sorting malformed objects. Strict new-envelope validation is separate from tolerant
legacy unknown fields/types. Same typed source key with different semantic bytes produces
`XH.EVENT_CONFLICT`; originals and raw file history are untouched. This is **reader/fold
conflict proof**, not successful idempotent enhanced append receipt proof.

Short `os.write` counts now raise `COORD-SHORT-WRITE`. A one-byte injected append leaves
one byte and fails. There is **no claim that an error atomically repairs or undoes that
partial append**, no fsync/power-loss guarantee, and no cooperative-lock solution to
unchanged-writer races. CLI request errors return nonzero with a stable error code.

### Executed RED → GREEN and immutable pins

Final suite: `docs/ai-forward-pack/scripts/tests/test_coord_responses.py`.
No official Python tests directory/self-test command was found in this vendored scripts
snapshot; this creates the requested discoverable stdlib suite. No C# test is claimed.
Full named GREEN output and summarized RED failures are in tool receipt **shell 639**.
The runner can regenerate full local receipts without output redirection:

```powershell
python docs\ai-forward-pack\scripts\tests\test_coord_responses.py --baseline-receipt .agents\xh-p1-red.txt
python docs\ai-forward-pack\scripts\tests\test_coord_responses.py --receipt .agents\xh-p1-green.txt
python -m unittest discover -s docs\ai-forward-pack\scripts\tests -p test_coord_responses.py -v
```

**Observed:** final pinned baseline ran **19 tests**, exit 1, **22 failing subtests and
22 errors** (3 test methods passed). Candidate discovery ran **19 tests**, **0 failures,
0 errors**, exit 0, **0.784 seconds**. Subtest counts are not test-method counts.
Initial fixture defects (missing pinned `repo_identity.py`, inherited `GIT_CONFIG_*`,
Windows read-only Git objects and a mocked descriptor close) were corrected before this
final comparison. They are not counted as product RED evidence.

| Pin | Observed value |
|---|---|
| Pre-P1 commit | `94ec9036dd0b72aa5b759badcf21a9e3aba6659b` |
| Pre-P1 coord-core Git blob | `b2ed495fcf4b6332ddf517aee17144173ac5b96b` |
| Pre-P1 coord-core SHA-256, asserted by legacy test | `f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf` |
| Pinned coord_ids support blob | `6afe13e87fd37e650e790b2135c119e16b85797c` |
| Pinned repo_identity support blob | `27425b9f40e8f3d2abde90032862b01b4969c78c` |
| Candidate coord-core working-byte SHA-256 | `0f16846da8664a3e39bc1eef42d143d04e1f71405210977034be701897bc937b` |
| Final test working-byte SHA-256 | `7f65bad0c32e6be7894d0e9ccdd0c9abf315f074ec4b4d363ba37c42d06cb7e8` |
| Full final RED receipt SHA-256 (local, regenerable, not committed) | `7fdace563ab49921aad01a1b38459a642c96fbaea350ead9e00e83ba9d250d8f` |

Every name below is a `test_` method of `ResponseTests`. Names are shortened only by
that common prefix. **Verified** means executed locally, not independently accepted.

| Test name | Oracle and RED observation | Confidence / remaining boundary |
|---|---|---|
| Append_ConcurrentEnhancedAttempts_AllRefused | 8 attempts, width 4; baseline succeeds, candidate refuses all without file | Verified denial; no mixed-client activation |
| Append_DiskFailure_Propagates | Injected disk error propagates; both versions pass | Verified existing behavior preserved |
| Append_EnhancedSyntheticVerifier_ZeroEffects | Baseline appends; candidate refuses before file-open/subprocess | Verified append boundary only; full O07 authority adapter pending |
| Append_ShortWrite_FailsWithoutRepairClaim | Baseline reports success; candidate raises and retains exactly one byte | Verified; no atomic repair/durability claim |
| Cli_AllStatusById_ReadsHistoryWithoutAuthority | Baseline rejects flags; candidate exposes exact reply, unknown generation, zero eligibility | Verified official CLI composition/read |
| Cli_PinnedUnenrolledLegacy_ActualLinkedTreeAndRollback | Actual git primary + 2 linked trees; old client add→resolve→list; compare new/old rows and original raw records; restore exact old source/support and repeat | Verified O08 and rollback portion of O10; old client never enrolls/upgrades |
| Fold_ConcurrentResponses_RefusesAmbiguousLatest | Concurrent unchained replies cannot pick arbitrary latest | Verified synthetic semantics |
| Fold_CorrelatedReply_AnswersWithoutAcceptance | Changes requested clears unanswered but never acceptance/execution; inputs unchanged | Verified O01 fixture, not authenticated production answer |
| Fold_DuplicateAndPermutedLegacy_NeverReopens | All 6 permutations of add/resolve/add; baseline reopens | Verified legacy regression |
| Fold_DuplicatePermutedResponses_OneSemanticAnswer | All 6 permutations of parent/reply/retry; one semantic response | Verified O03 fixture |
| Fold_LegacyAddResolve_PreservesPayload | Ordinary resolve preserves question; ACK prose has no acceptance field | Verified existing legacy behavior |
| Fold_StaleProposal_PreservesHistoricalResponse | Different revision/hash stays historical and unanswered | Verified stale response; full O05 acceptance/supersession pending |
| Fold_SupersedingResponse_UsesCausalLatestNotClock | All 6 permutations; later causal reply has earlier clock; deferred retains checkpoint | Verified response supersession only |
| Fold_UnknownOldGeneration_DoesNotApply | Empty generation map and G1→G2 both refuse | Verified response applicability; O06 authenticated consumption pending |
| Read_DigestMutation_Refuses | Alter payload after hashing; no event admitted | Verified Python digest only |
| Read_EnvelopeValidation_RejectsInvalidSchema | 9 invalid schema/type/disposition fixtures; baseline accepts | Verified dormant subset only |
| Read_InvalidUtf8_ReportsAndKeepsNextRecord | Invalid byte followed by valid event; baseline decoder aborts | Verified record-local refusal |
| Read_MalformedObjects_ReportsInsteadOfSorting | 7 malformed/non-finite/duplicate-key cases | Verified parser control |
| Read_SameEventKeyChangedPayload_ExplicitConflict | Changed bytes under same key refuse; all 3 raw records preserved | Verified reader conflict; O09 append retry receipt pending |

### Class → sweep → derive → prevent

**Class:** replay/arrival order used as domain state; ambiguous input normalized into a
plausible record; attempted append reported as completed. Related standing classes are
E2E-F, RIG-C and DC-026. **Sweep:** request reader, fold, append, resolve and list were
traced together. `append_decision` also ignores write count, but it is explicitly best-effort
telemetry, not canonical admission; no claim of repair there. **Derive:** one official
fold computes the read, no second disposition/ACK/ownership store. **Prevent:** the
permutation, malformed corpus and real one-byte write tests above fail on the baseline.
Fixture corrections are CI-ENV/RES-LEAK-TEST shapes: pinned support plus scrubbed Git
environment and read-only-aware teardown now live in the tests. Defect-register editing
was outside author ownership; conductor must incorporate this receipt, not invent a lease.

### Unmet floors and next independent gate

* **Blocker / Verified:** enhanced writer is disabled. O09 successful retry receipts,
  mixed unchanged-writer contention/full-record/conflict safety, cross-language golden
  bytes and complete P1 authority checks are unimplemented/unqualified.
* **Blocker / Flagged:** qualified verifier/human channel and authenticated generations
  remain unavailable. O04 exact valid peer-acceptance positive, full O05 proposal
  supersession, O06 consumption and full O07 authority-entrypoint proof remain pending.
* **Major / Verified:** opt-in read has unknown generations in production. It displays
  historical replies, not an authenticated actionable workflow. No new transport or
  acceptance status is presented as live.
* **Major / Flagged:** full parser/ledger size bounds, invalid-surrogate corpus,
  generation revocation, privacy retention and endpoint behavior are not qualified.
* **Independent code gate pending:** Test/DS/Security review this exact candidate; the
  author does not clear it. P2–P5 remain approved work, not completed work.

Instrumentation: enhanced CLI list emits elapsed seconds, events scanned, explicit disabled
writer and not-qualified generation status on the normal opt-in path. Errors are returned
without raw payload logging. No inference spend, network or endpoint latency is measured
because none is invoked. Full OTel integration and P5 SLIs are not claimed.

Closing audit `al-01M2NMW23RP1R4EC0ZESNW60NF` records **808.00 measured seconds** from
the official start marker and outcome `partial`. The official prompt logger also wrote
`al-01M2NMW20JGD8H8XXRMPCXZA3R`. `audit-log.py selfcheck` reported a goal/budget
presence gap on that prompt-only entry; the implementation entry carries both. This is
a reported tooling/record gap, not a PASS. Official audit append regenerated
`docs/audit/audit-data.js` as a side effect; that generated diff is excluded/restored
because the conductor owns rollups. Local synthetic Git roots and receipt scratch files
are removed after capture; the committed tests regenerate their evidence.
**Minor / Verified process gap:** the closing-audit paragraph was added after its prior
lease was released. The exact Proof Pack lease was reacquired for this correction and
released before commit. It is not represented as continuously leased editing.

**Original author:** Data & Persistence peer/co-author. **Delta:** Python phase-specific
recording of the supplied independent F1–F6 corrections. **Tier:** T2.
**Scope:** five existing authored docs and official append-only audit entries; no solution code,
migration, endpoint probe, new dependency, runtime repair or independent gate clearance.
**GATE P0-delta pending independent review.** The actual human approved implementation
of all P0–P5; that all-six-phase goal remains approved without renewed human phase approval.

## 1. Receipt and immutable evidence

Repository: `timianmalloo/ai-de`.
Source baseline/conductor HEAD: `94ec9036dd0b72aa5b759badcf21a9e3aba6659b`.
Main last-read value supplied by conductor: `bcf4959bc0e0e361736e6a179f05b69fcd0500f8`;
not re-investigated or claimed current.
Branch: `feature/xh-p0-contract`; session: `xh-p0-contract-b0d0`.
P0-delta input HEAD: `a9595585547b7661457847642f799d6ff521a15e`.
The previously ended official session is reopened under the same identity for this
bounded correction; exact-file TTL300 edit claims are released before validation/commit.
Official worktree creation registered `C:\Projects\ai-de-feature-xh-p0-contract`.
Requested path `C:\Projects\ai-de-xh-p0-contract` was not honored by `worktree new`:
`--path` is cleanup-only; `cmd_worktree` derives path from primary basename and branch.
No manual move, installation, config/hook edit or peer-tree edit was used.

Official allocated ADR: `adr-01M2NJ2PQS7X6GE3F559JC4TNP`, under established `docs/adr/`.
All five authored paths are leased at TTL300 only for editing; register/derived paths
are not authored claims. `.agents/artifacts.yml` is classification authority.

| Commit-bound path | Full Git blob | Working-byte SHA-256 at fresh read |
|---|---|---|
| `docs/collaboration/session-contracts.md` | `536352879936b5b10a4b57e71b31f23981ce9b84` | `4e0c3f3532d0a8e0ddddad82477d0d42f2a943a2768710354de1be7bcba8280f` |
| `docs/investigations/cross-harness-message-delivery.md` | `5cdc8be97381f7a00d7b661e8e2983bdc0a92d69` | `b866cbcbfb60429405b3cd78e4fb85361b88c938543f53cb5c6f72fbfe202b04` |
| `docs/ai-forward-pack/scripts/coord-core.py` | `b2ed495fcf4b6332ddf517aee17144173ac5b96b` | `f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf` |

The blob/working hash pins above are local integrity observations, **not trusted-registrar
attestations**. §2 remains the sole authority. A full AuthorityRef additionally requires
verified repository identity, authenticated decision/issuer evidence, scope and a verifier
receipt as specified in the design. This author cannot manufacture those.

The original receipt reported no conductor `graphify-out/graph.json`; the P0-delta
existence check at `C:\Projects\ai-de` returned true. No graph generation/install or
traversal was attempted in this correction. Original document links establish ADR-0023 → implements → architecture-loomkeeper
→ implements → spec-agentic-watcher-substrate → relates-to → session-contracts; ADR-0020
also implements the architecture/spec. The index stores links inside artifact records,
not a top-level `edges` collection; an initial edges lookup returned empty and is not
represented as absence of graph relations. Derived-index freshness after new docs is
conductor-owned and pending, as are security/privacy rollup backlinks.

## 2. Fresh source evidence (not fresh runtime reproduction)

| Source | Verified static fact | Runtime claim boundary |
|---|---|---|
| `coord-core.py:388–442,1786–1830` | Old primary append is unlocked: O_APPEND and one os.write without an fsync/full-byte-count check; reader sorts producer `at`; fold open/resolved; add overwrites same ID; resolve lacks typed revision/generation | A new cooperative lock does not automatically protect the unchanged client. O10 must qualify mixed-client complete-record/conflict safety; no power-loss guarantee. Independent extracted-fold diagnostic is recorded separately below, not CLI/C# execution |
| `CoordinationContractLog.cs:13–105,230–269` | Writer emits contract records; pump rereads entire directory and applies every event | Two-pump post safety still needs O11 |
| `CoordinationContract.cs:259–268,576–628` | Time ordering; ApplyBoardPost calls service per recognized post; no source-key check on that path | Actual repeated-post/restart effects remain Inferred |
| `MessageBoard.cs:127–175` | Instance lock, fresh message ID, count+1; parent/capability validation in service | Two-service cursor loss is an Inferred interleaving until O14 |
| `SqliteWatcherObservationStore.cs:546–617,1262–1276` | Ordinary insert; message PK; repository index; no repository/seq unique constraint or parent FK there | No migration up/down or query plan executed in P0 |
| `WatcherHost.cs:45–73` | Opens `watcher.db` and composes registrar/ingest/pump | Conflicts with shared-store architecture wording; no live binary census |
| `BoardPublisher.cs:70–88`; `BoardTools.cs:76–112` | Drop tombstones; keep newest bounded page; MCP filters sinceSeq then takes newest | Cursor-complete recovery not proven by these views |
| `WatcherBoardPaneViewModel.cs:11–51,68–72` | Row omits source/parent IDs; query reads all board messages | No UI test run or native failure inference |
| `CoordinationContractLogTests.cs:117–130` | Existing rerun test asserts registration counts only | Not proof of post idempotence |
| `MessageBoardTests.cs:100–143` | Tests existing/cross-repo/missing parent, content-free ACK | Not proposal acceptance or migration evidence |

## 3. One confidence ledger

| ID | Finding / severity / confidence | State and clearance evidence owed |
|---|---|---|
| F01 | **Blocker / Inferred:** reliable coordination reuse can violate one-effect/cursor-completeness invariants via replay/two-instance interleaving; static path Verified | OPEN for P2 live delivery. O11–O15 real red tests and independently reviewed repair; author does not clear |
| F02 | **Major / Verified:** ADR-0023 shared-store wording conflicts with `WatcherHost.Open` watcher.db composition | INHERITED ARCHITECTURE DEBT, not claimed conformant. Bounded ruling: additive application/feed/checkpoint caches behind existing watcher observation-store seam in existing watcher.db used by WatcherHost/MCP; requests.jsonl sole canonical source. No new DB/native relocation/workspace.db migration/cross-DB transaction. Data/DS concrete representation coapproval before P2 code; do not expand work |
| F03 | **Blocker / Flagged:** authority verifier/human-channel evidence is not established for the new authority adapter | OPEN for live authority use, not a proven exploit. O04–O07 plus independent Security gate; hash alone cannot clear |
| F04 | **Major / Verified:** old newest-N/read DTO shapes do not encode complete coordination cursor/thread semantics | OPEN for enhanced reads; O18/O26 plus compatibility proof |
| F05 | **Major / Flagged:** endpoint positive conformance and retention/erasure basis not yet established | OPEN; per-endpoint O22 and Privacy/operator review before live sensitive-data admission |
| F06 | **Minor / Verified:** official CLI generated a different worktree path than requested | Documented deviation; use registered generated tree, no bypass |
| F07 | **Minor / Verified:** new index/rollup entries not regenerated by this author; prior graph-absence statement is historical, not current | Conductor checkpoint must derive/validate and report stale/orphan results honestly |
| F08 | **Blocker / Verified (independent F1):** original rollout required upgrading/enrolling every canonical writer, excluding unchanged old-worktree clients | CORRECTED DRAFT, not independently cleared. Unchanged unenrolled official request-add/request-resolve/list remain operational on the same primary requests.jsonl, no enrollment/upgrade. O08/O10 pin unmodified pre-P1 client; enhanced writes disabled until separate coexistence proof |
| F09 | **Major / Verified (independent F3):** P0 admission could be read as full P1/live authority or schema deployment | CORRECTED DRAFT, not independently cleared. P0 admits only compatible dormant fold/envelope/schema-validation and isolated tests after delta review; not deployed SQLite schema, production authority or P1 completion. O07 production-entrypoint zero-side-effect oracle; authenticated fixture/channel and P3/P4 activation BLOCKED |

No runtime red/green result is implied by a static finding. Hard veto applies to
invariant-violating live reuse or an unsafe migration, not to publishing this draft.

## 4. Oracle catalogue — ALL PENDING, unless explicitly BLOCKED

Synthetic fixtures only; approved real-endpoint tests are separate. The test names below
are **specified future tests**, not files already written or passing tests selected by a filter.

| Oracle | Runnable-test specification / meaningful assertion | Phase |
|---|---|---|
| O01 | `Fold_CorrelatedReply_ShowsOnInitiatingThread`: invoke real official fold/read; one negative reply removes unanswered, keeps acceptance false | P1 |
| O02 | `Read_ResolvedThread_ByIdIncludesResponse`: all-status by-ID contains original + response when open-only omits completed work | P1 |
| O03 | `Fold_DuplicateAndPermutedEvents_NeverReopens`: enumerate causal permutations/retry multiplicity; same logical state, no duplicate semantic action | P1 |
| O04 | `Accept_TransportAck_RefusesApproval`: ACK/legacy “approved” text cannot produce acceptance; exact valid synthetic peer pair positive control proves deterministic contract behavior ONLY, no production authority | P1 |
| O05 | `Accept_SupersededHash_RefusesCurrentAcceptance`: H then H2; late H acceptance preserved historically but not applicable | P1 |
| O06 | `Consume_UnknownOrOldGeneration_Refuses`: absent G, G1→G2 restart, alias reuse; zero G2 consumption/action | P1 |
| O07 | `Authorize_NewTransferWithoutHuman_Refuses` / `Production_UnqualifiedVerifier_ZeroPrivilegedEffects`: valid hash but forged verifier, wrong repo/path, Owner prose, no human evidence AND apparently valid synthetic verifier receipt through production entrypoint all deny absent qualified verifier evidence; assert zero grants, transfers, endpoint sends and launches. Isolated synthetic positives prove contract behavior only; authenticated scoped positive fixture is separate | P1 dormant denial/contract tests only; authenticated authority fixture, supported-channel qualification and P3/P4 activation BLOCKED |
| O08 | `Legacy_UnmodifiedUnenrolled_AddResolveList_PreservesOriginals`: pin unmodified pre-P1 official client; run from unenrolled synthetic worktree against the same synthetic primary requests.jsonl; official add→resolve→list with enhanced disabled, and again across rollback; assert original request IDs and payloads at every stage. No enrollment/upgrade or substitute shim | P1 |
| O09 | `Append_SameKeyDifferentPayload_RefusesConflict`: same bytes returns same receipt; changed payload under same key errors without extra write | P1/P2 |
| O10 | `Legacy_RollbackAndMixedClientActivation_GatesEnhancedOnly`: repeat O08 pinned unchanged unenrolled old-worktree add→resolve→list with enhanced disabled/across rollback and original IDs/payloads. Separate pre-activation mixed-client contention, complete-record and conflict-safety tests must include the same unmodified client; torn/short writes and disk failures cannot report false success. Old append is unlocked; new cooperative lock or upgraded shim is not proof. Enhanced writing stays disabled until proof; no legacy client disabled | P1 dormant compatibility tests; separate enhanced-activation qualification pending |
| O11 | `Pump_RegisterPostTwice_OneMessage`: real SQLite and writer/pump path, register+post, two pumps; one stable message and one receipt, not just one registration | P2 |
| O12 | `Import_CommitThenLostAck_RetrySameEffect`: inject crash after commit before receipt; retry returns original mapping/sequence | P2 |
| O13 | `Import_CrashBeforeCommit_NoEffectOrCheckpoint`: crash each write boundary; effect, receipt and checkpoint all-or-none after restart | P2 |
| O14 | `Import_TwoInstancesReaderBetweenCommits_NoSkippedFact`: two services/connections; barrier A write/B waiting, commit A, reader cursor, commit B, reader; both distinct facts observed exactly once | P2 |
| O15 | `Migrate_ExpandOperationalRollback_PreservesAllHistory`: representative real old DB + new events; actual deployer up, old binary/rollback, re-enable/replay; original IDs/payloads and accepted facts identical | P2 |
| O16 | `Import_LateParentAcrossRestart_EventuallyAppliesOnce`: skewed producer times, pending child, restart, parent; new applied feed transition, no orphan or lost consumed cursor | P2 |
| O17 | `Import_UnsupportedVersionOrSourceGap_ExplicitRefusal`: unknown version, malformed line, truncated/changed prefix; no false checkpoint success | P2 |
| O18 | `Read_401Unread_PagesWithoutGaps`: page 200/200/1 ascending; tombstones included as envelopes; no newest-N skip; epoch mismatch explicit reset | P2 |
| O19 | `Replay_TombstoneAndEndedGeneration_NoResurrection`: restart/register/end/late receipt; redacted payload never returns, old generation never revived | P2 |
| O20 | `Queue_LimitPlusOneOutage_PreservesAccepted`: parameterize every P2 item/byte/retry bound; already-canonical overflow becomes visible reference-only capacity-deferred quarantine with receipt/checkpoint, not dropped or blocked before its later parent; fair recovery includes exhausted attempts; disk/DB outage then recovery loses no accepted obligation. P1 producer admission is separate | P2 |
| O21 | `Deliver_InFlightLimitAndRunningTurn_DefersSafely`: fake time/transport, limit+1, failed/unavailable model, sibling-read refusal; stable retry, no duplicate semantic action, outage recovery | P3 |
| O22 | `Endpoint_RegisteredGeneration_ConsumesAndWakes`: installed/version-pinned foreground/background GHCP/Codex/Grok/Claude, approved low-volume synthetic message, witnessed correct conversation arrival+consumption+supported post-turn wake | P3; all real endpoints BLOCKED pending spikes/availability |
| O23 | `Launch_TwoCallers_AttributesCorrectActor`: same helper under two launcher identities; actor/generation/worktree/candidate/run and PID creation/parent match each caller | P4 |
| O24 | `Start_ReplayedOrExpiredToken_NoSecondRun`: checked token replay returns original; expiry with live holder refuses regrant; PID reuse is not same process | P4 |
| O25 | `Outcome_Failure_DoesNotImplyRelease`: failed END captured; release only after independent holder check; duplicate release stable | P4 |
| O26 | `Conformance_SameCorpus_AllSurfacesAgree`: official queue fold, board projection, MCP and UI expose equal obligations/revisions/negative states at same watermark | P5 |
| O27 | `Sli_PauseTimeout_NoApproval`: fake clocks; total/paused/available duration and outage denominator correct, threshold never creates acceptance | P5 |
| O28 | `Telemetry_Failures_HaveCodesAndNoSecrets`: correlate spans, stable codes, bounded labels; reject raw prose/capability leakage; UI state/craft/accessibility assertions | P5 |

Additional P2 assertions: forbidden fact UPDATE/DELETE fails at store; same-repo parent
FK rejects cross-repo/orphan insertion; SourceEventKey conflict tested at DB boundary;
rebuild equality from source; EXPLAIN QUERY PLAN uses planned indexes for cursor/pending/
thread lookups at 100× corpus; bounded rows/bytes/attempts. These are part of O09/O13/
O15/O16/O20, not optional afterthoughts.

O08/O10 pre-P1 pin: source baseline commit `94ec9036dd0b72aa5b759badcf21a9e3aba6659b`,
official client path `docs/ai-forward-pack/scripts/coord-core.py`, Git blob
`b2ed495fcf4b6332ddf517aee17144173ac5b96b`. P1 must verify and run the unmodified pinned
client with its required pre-P1 support files from the unenrolled synthetic worktree,
not import a changed wrapper or silently reroute it to the upgraded client. The synthetic
primary is shared by those test clients; no real primary requests or endpoints are test data.

**P2 floors unchanged (independent F4):** O11–O20 require real SQLite replay/crash,
two instances with a reader between commits, atomic effect/source-key/receipt/checkpoint,
limits+1/outage recovery, late and permanently missing parents, 401-row paging,
tombstones/version rejection, forbidden mutations, rebuild equality and **actual rollback**.
The static C# replay/cursor risk remains **Inferred**, not upgraded by the Python diagnostic.

### Commands / oracle integrity

P1 should add a discoverable stdlib unittest module for the official Python focal
functions, then execute `python -m unittest discover -s <admitted-test-directory> -v`;
the Proof Pack must name the actual path and nonzero executed case count. No fictional
current test file is supplied here. A directly runnable **red control** for the existing
fold, without live queue writes, is:

```powershell
python -c "import importlib.util,sys; from pathlib import Path; p=Path('docs/ai-forward-pack/scripts/coord-core.py'); sys.path.insert(0,str(p.parent)); s=importlib.util.spec_from_file_location('coord',p); c=importlib.util.module_from_spec(s); s.loader.exec_module(c); q={'kind':'request-add','id':'synthetic-q','at':1}; r={'kind':'request-resolve','id':'synthetic-q','at':2}; assert c.fold_requests([q,r,q])[0]['status']=='resolved', 'transport replay must not reopen'"
```

The command above was specified by the original P0 author and **was not executed by this
delta author**. Supplied independent-review evidence instead executed an **extracted
current fold**, with duplicate-add replay **RED: actual open != expected resolved** and
ordinary legacy add→resolve **PASS**. This is a narrow isolated Python diagnostic,
**not official CLI execution, full O03/O08/O10 evidence or C# reproduction**. P1 still
owes discoverable official-code red→green tests; no tests are run in this correction.

C# seam suite after adding named tests:
`dotnet test tests/AiDe.Core.Tests/AiDe.Core.Tests.csproj --filter "FullyQualifiedName~Watcher"`.
Capture exact selected names, nonzero counts and red→green evidence; that broad command
alone does not prove O11–O20. Use real SQLite/two connections and explicit scheduling
barriers. The existing no-double-register test cannot substitute for O11. No GUI/App/full
qualification is authorized by these commands.

## 5. Operator and rollback questions

1. Which authenticated actual-human decision channel can the trusted registrar verify,
   and how are revocation/supersession and human scope checked offline?
2. Which coexistence mechanism can prove mixed-client contention/complete-record/conflict
   safety without upgrading, enrolling or disabling unchanged legacy clients? Enhanced
   capability enrollment alone and a new cooperative lock cannot protect old unlocked append.
3. Which concrete additive application/feed/checkpoint representation will Data and DS
   coapprove before P2 code behind the existing watcher observation-store seam in existing
   watcher.db? Placement is bounded; inherited ADR-0023 divergence is debt, not conformance.
4. What are pilot queue/byte/attempt bounds, disk reserve, retention durations, policy
   deletion basis and backup expiry? Who owns a permanently missing parent?
5. Which installed endpoint modes can actually wake/consume, and which remain BLOCKED?
6. Who can attest actual holder release after run failure/expiry without touching peer slots?

No unanswered operator question can be resolved by a timeout-to-approval rule.

## 6. Original P0 validation record (historical, not re-executed by this delta)

Executed: official CLI help/start/worktree registration/ADR allocation/exact-path claims;
complete investigation read; sole §2 ownership read; relevant architecture/US4/ADRs;
fresh scoped source and referenced test reads; local commit/blob/working-hash lookup.
No C# runtime tests, migration up/down, query plan, endpoint consumption, latency
percentiles or security authority positive fixture have been executed.

Documentation-only closeout:

* Official `docs-graph.py validate`: **exit 1**, 525 artifacts, **zero schema/link
  problems, zero orphans, zero date-stale artifacts**; five defects are exactly the
  five new artifacts not yet in the derived index. **74 existing review-suggested
  flags** remain. This is not a clean full documentation gate.
* Initial frontmatter validation exposed two unregistered types and one relation;
  corrected using the actual type/relation registry. No unresolved frontmatter defect.
* Actual index artifact links verified the stated ADR→architecture→spec→authority
  traversal. No top-level `edges` assumption remains.
* `git diff --check`: passed on tracked changes. Final staged check also required
  for the new files; no runtime correctness inferred from whitespace.
* Official audit `al-01M2NJNPENFDV441RY54BJFWCQ` and change
  `cl-01M2NJNPKE3B9BWR5FDWRR1PJZ` record **partial / draft**, not authority or
  independent acceptance. `audit-log.py verify`: **763 audit + 156 change,
  zero unreadable lines**.
* Baseline comparison proved each audit register preserved every prior line and
  appended exactly one own entry (762→763; 155→156). Official append/change
  automatically rendered `docs/audit/audit-data.js`; only that own generated change
  was restored, leaving the conductor to regenerate at checkpoint. No site or
  whole-documentation generation was run or committed by this author.
* No source edits, dependencies, runtime tests, migration, endpoint/nonce probes,
  peer slots, observer changes or nested agents.

Conductor must regenerate derived index/audit views/security/privacy rollups and
validate them at checkpoint. Do not label deferred derived data as a clean site build.

## 7. P0-delta textual correction receipt — partial, not acceptance

| Independent item | Exact correction recorded; evidence still owed |
|---|---|
| F1 — Blocker / Verified compatibility design defect | Replaced upgrade-all/serialized-old-command compatibility with unchanged unenrolled old-worktree official request-add/request-resolve/list on the same primary requests.jsonl. Enrollment gates enhanced capabilities only. O08/O10 unmodified pre-P1 add→resolve→list/rollback IDs+payloads, plus separate mixed-client contention/complete-record/conflict safety before enhanced activation; old append unlocked, no upgraded-shim substitute or disabled legacy client |
| F2 — Major / Verified bounded-store ruling | Existing watcher observation-store seam and existing watcher.db used by WatcherHost/MCP; additive application/feed/checkpoint caches only, requests.jsonl sole canonical source. No new DB/native relocation/workspace.db migration/cross-DB transaction. ADR-0023 physical divergence inherited debt, not conformance; Data/DS coapproval of concrete representation before P2 code |
| F3 — Major / Verified admission scoping | Independent P0-delta clearance admits ONLY compatible dormant P1 fold/envelope/schema-validation and isolated tests, not deployed SQLite schema or full P1 completion. All privileged production paths deny absent qualified verifier evidence; O07 includes apparently valid synthetic receipt and zero grants/transfers/sends/launches. Synthetic positives are deterministic contract evidence only; authenticated fixture, channel qualification and P3/P4 activation BLOCKED; prose inert |
| F4 — mandatory P2 floors / narrow diagnostic | O11–O20 and additional store assertions unchanged; static replay/cursor risk remains Inferred. Independent extracted-current-fold duplicate-add RED open!=resolved and ordinary legacy resolution PASS recorded as isolated diagnostic, not CLI/C# evidence or tests executed in this delta |
| F5 — unresolved privacy and endpoint evidence | Live retention/erasure across source/cache/export/endpoint/backup payload copies unresolved. Every foreground/background GHCP/Codex/Grok/Claude positive arrival/consumption/supported-wake cell BLOCKED pending supported availability; fakes do not clear |
| F6 — reporting honesty | Proposed SLIs remain unmeasured; docs index/audit views/security/privacy rollups pending conductor. Official audit records partial draft correction, no acceptance/verification-executed signal manufactured |

This bounded delta makes no source/test changes, executes no runtime tests, and uses no
agents or real endpoints. A shell `rg` discovery attempt was unavailable; bounded Python
source inspection supplied official command syntax instead. Original source/test pins
and validation outputs above are historical, not rerun claims. The ordinary-resolution
PASS belongs solely to the supplied independent isolated diagnostic.

| Phase | Current state |
|---|---|
| P0 | Corrected draft; **GATE P0-delta pending independent review** |
| P1 | Pending implementation; dormant subset only after independent gate; does not complete P1 |
| P2 | Pending; concrete additive representation Data/DS coapproval before code; real-store proof owed |
| P3 | Pending; real endpoint conformance/activation BLOCKED |
| P4 | Pending; qualified authority/launcher activation BLOCKED |
| P5 | Pending; same-corpus/UX/SLI evidence owed, no measured SLIs |

**Next: independent delta reviewer on the exact correction commit, then conductor-admitted
Python P1 author. No self-clearance; no renewed human approval of the already approved phases.**

## P2.1 native ordering/paging implementation receipt — 2026-09-16

**Narrow implementation evidence, NOT full-P2 or activation PASS.** The design gate
was committed first at `36e560f54b55306575dd3a5e2c57ab679feef2a1`, based on unchanged
source `3d13270683655156f79dfe9d698cc0b410f2a32b`. Design §11 records the exact
accepted Data/DS amendments and the compatibility exception. The independent DS
verdict was DESIGN PASS-WITH-CONDITIONS, not an implementation review. No independent
C#/Data/DS implementation verdict has yet been issued for this candidate.

### Change reach and compatibility

| Surface | Writer → reader / result |
|---|---|
| IWatcherObservationStore | New returning `AppendBoardMessageAllocated`; unsupported default throws NotSupportedException without fallback |
| SQLite store | IMMEDIATE writer transaction before MAX and insertion; existing repository index; checked Int64 addition and Int32 conversion; read inserted row in transaction, commit, return |
| In-memory store | One store-wide lock covers MAX, checked increment and dictionary Add; duplicate identity cannot overwrite through the allocated path |
| MessageBoardService | Existing Post/Reply/Acknowledge signatures and capability checks unchanged; returns the allocated row, not Seq=0 proposal |
| Native BoardMessage / MCP BoardEntry | Seq stays Int32, no wire/schema widening or uniqueness retrofit |
| MCP read | sinceSeq selects earliest qualifying N ascending; cursor is last returned Seq; no-cursor retains recent-N |
| Existing publisher / GUI | No source changes; 12 publisher tests in compatibility batch pass, but no complete change-feed or GUI proof is claimed |

Legacy void append remains caller-sequenced insertion, including historical gaps and
duplicate sequences. It deliberately does not delegate to allocation: that would
rewrite seed/import semantics. Such legacy writes are outside the reliable future
cursor guarantee. Native tombstones and historical sequence ties are likewise not a
complete change feed. Schema version remains 7; no migration, index, dependencies,
configuration, hook, authority or P1 source changes.

Provider contract was read from installed Microsoft.Data.Sqlite.Core **10.0.11** XML,
`SqliteConnection.BeginTransaction(Boolean)`: `deferred:true` defers creation and
allows read-to-write upgrades. The candidate explicitly selects `deferred:false`.
Source contains existing `ix_board_message_repo(repository_key)`. Reusing it is not
a constant-time MAX or production latency claim.

### Executed commands and receipts

All commands ran in `C:\Projects\ai-de-feature-xh-p2-projection`, with edit leases
released. No gate exit was piped or redirected. Standard VSTest TRX files were written
under `.agents\p2-ordering-evidence`, read back and hashed, then removed at close to
leave a clean tree. They are not claimed committed artifacts. Full console results
remain in tool receipts; the durable result tables and receipt hashes are below.

* Core and MCP: `dotnet build <project.csproj> --no-restore
  -p:BuildProjectReferences=false -v:quiet`: each exit **0**, zero warnings/errors.
  Observed elapsed output: Core 3.04 s, MCP 0.53 s; these are build measurements only.
* Tests: `dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore
  -p:BuildProjectReferences=false --filter <selector> --logger
  "console;verbosity=quiet" --logger "trx;LogFileName=<receipt>.trx"
  --results-directory .agents\p2-ordering-evidence`.
  RED receipt and compatibility/reliability reruns use `--no-build`; GREEN compiles
  tests against the separately rebuilt Core/MCP. No App/native/Atlas/UIA tests ran.
* RED source check `git diff --exit-code -- src`: **0** before solution changes.
  First RED execution's verbose output exceeded tool display size; the subsequent
  same-binary RED rerun persisted TRX and printed every test result without truncation.
* Existing compatibility fixtures used a process-local TEMP/TMP rooted in this
  worktree's `.agents\p2-ordering-data`, removed after execution. New fixtures create
  their own directories below the test working directory and dispose them.

| Batch / exact selector | Executed result / exit | Receipt SHA-256 |
|---|---|---|
| `FullyQualifiedName~BoardOrderingTests\|FullyQualifiedName~CoordinationReliabilityTests.Post_` before fix | 15 total, **12 failed, 3 passed**, 0 skipped; exit **1**; 240 ms reported | `B3CB060ED73099CE33574ADC38918F48D2CDD32BC6FCDD24344DE653B26E0CFE` (`ordering-red.trx`) |
| Same selector after fix | **15 passed**, 0 failed/skipped; exit **0**; 253 ms reported | `8176696A6CA3A7694C02C1455CFAD79B025266C18A89B072DB9223EBB4A918B1` (`ordering-green.trx`) |
| `FullyQualifiedName~MessageBoardTests\|FullyQualifiedName~SqliteWatcherObservationStoreTests\|FullyQualifiedName~McpMatchesTheJsonlPathTests\|FullyQualifiedName~ContractBoardPostTests\|FullyQualifiedName~BoardPublisherTests` | **75 passed**, 0 failed/skipped; exit **0**; 233 ms reported | `1B90130328EDEE69860E884BF974030FA6C17BD970094C65AD2D08F2C9DB248A` (`ordering-compatibility.trx`) |
| `FullyQualifiedName~CoordinationReliabilityTests` | 7 total, **3 passed, 4 failed**, 0 skipped; exit **1**; 188 ms reported | `12C3968FEFB07E93284583CFC4E8FA0CC2B7D433F3B3D9061AB25D1D5128B229` (`reliability-remaining.trx`) |

The final shell printed `COMPATIBILITY_EXIT=0 RELIABILITY_EXIT=1` and returned **1**.
That red is intentional evidence of the remaining programme defects, not a green
aggregate or a shippable suite.

### Per-oracle proof

Test names below are in `tests\AiDe.Core.Tests\Watcher\BoardOrderingTests.cs`,
except the two explicitly identified CoordinationReliabilityTests cases.

| Test / claim | Oracle and observed RED | GREEN / confidence |
|---|---|---|
| `Post_SequenceHolesAndHistoricalTies_ReturnsMaximumPlusOne` (memory/SQLite) | Two historical Seq=7 rows, one tombstoned, another repository at 500; old result **3**, expected **8** | Both pass; committed returned row, unchanged tied identity, tombstone and `[7,7,8]` verified |
| `Post_MaximumSequence_RefusesWithoutWriting` (memory/SQLite) | Int32.MaxValue seed; old code threw no exception | Both throw OverflowException, no inserted overflow row; Verified |
| `Post_SqliteInt64Maximum_RefusesWithoutWrappingOrWriting` | Raw SQL sets Seq=Int64.MaxValue; old code threw no exception | OverflowException; SQL row count remains 1 and max remains Int64.MaxValue; Verified |
| `Post_DuplicateIdentity_DoesNotReplaceCommittedFact` (memory/SQLite) | Memory overwrote existing identity with no exception; SQLite already refused | Both refuse; original exact row survives; next Seq=2. Memory red→green Verified; SQLite baseline preservation only |
| `Post_TwoServicesSharingMemory_CursorSeesLaterCommittedFact` | B held at store seam, A commits, reader obtains cursor, then B released; old B Seq=1 instead of 2 | Seq=2 and exact later committed row visible beyond cursor; Verified |
| `Read_MoreThanTwoHundredCommittedMessages_ConsumesEarliestPagesWithoutLoss` | 205 real SQLite rows, independent read-only reader; newest-200 exhausted stream before second page and the next cursor access failed | Pages 200+5+0, exact IDs and sequences 1–205; Verified |
| `Read_CursorLimitBoundary_ClampsEarliestPage` (0→1, -1→1, 201→200) | Old one-row pages returned 205; old 200-row page started 6, expected 1 | All three earliest/clamped ranges pass; Verified |
| `Read_WithoutCursor_PreservesRecentPage` | Baseline control already passed: 55 rows → Seq 6–55 | Still passes; preservation Verified, not a red→green control |
| `CoordinationReliabilityTests.Post_TwoServicesAllocateBeforeCommit_CursorSeesLaterCommittedMessage` | R3, two real SQLite connections/services and third reader: old later committed B not visible above cursor; expected message-b, actual empty | Later B persisted with higher sequence and exact ID, visible above reader cursor; Verified |
| `CoordinationReliabilityTests.Post_TwoServicesSerially_CursorSeesSecondCommittedMessage` | Baseline fidelity control already passed | A=1, cursor=1, B=2 and exact committed B visible; Verified preservation |

R3's old precondition compared the preinsert proposal with the returned committed
row. Removed only Seq equality: after this fix the proposal is deliberately unallocated.
MessageId equality, exact returned-versus-durable row, total count and the **actual
reader-between-commits** oracle remain. The decorator intercepts both legacy and
new allocated method names and forwards all operations to the real store. B's barrier
is now before transactional allocation, not a callback inside the transaction.
The 15-second waits detect deadlocks; they are not sleeps or ordering assumptions.

### SHA-256 identity ledger

Paths below are relative to this worktree. Test source hashes are identical between
RED and GREEN. The solution source is pinned to the baseline before the first RED.

| File | Before | Tested candidate |
|---|---|---|
| `src\AiDe.Core\Watcher\WatcherObservationStore.cs` | `725EDFAD68B9DE35489889F7FDC9F5A024A3457D0827FAC82E0B22B433160D89` | `F02EE62A87D088C8A1828CD85D19E4249A5FC57C3783AB501CA7D2E313326E0B` |
| `src\AiDe.Core\Watcher\SqliteWatcherObservationStore.cs` | `462188EC8116302E1B5A0FBDB1BAF01BAAB35FBC2A7449021EF3988D2E7E0816` | `52492D886EADA2EA22FB27F82C05EC4DA79F49D90B4F5CE0005822E92DE5F784` |
| `src\AiDe.Core\Watcher\MessageBoard.cs` | `B9843DFD7E1DF08DEFD24E958E2D9D58B61FC76465A570DF2D48C46F1E33E005` | `6BF539E99D92FCC4057E727BDCF61BE9EE8A7D2245B5CA277DADEAEFE45B97B8` |
| `src\AiDe.Mcp\BoardTools.cs` | `FFED802495DA9C1A0A8233CFF54B0A0EBAC519FC84730EADD1E4FD4EA2754787` | `96B2D17FB5B1A330A6E61A9A7B4D7A4105F7632AEAECD524CB606C89368234F0` |
| `tests\AiDe.Core.Tests\Watcher\BoardOrderingTests.cs` | New RED test | `DB93C6E0BA1DF2268FB0203FC058E1CC64F75E9EC686378B4DFACE5CE6E197FC` |
| `tests\AiDe.Core.Tests\Watcher\CoordinationReliabilityTests.cs` | Adapted before RED | `F582F7B3BBBC84A481AC4459925B19449D3099F5EC014DEB995E6CB398FADA43` |
| `tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj` | Unchanged | `FFAD72D5B9D4E9F1D59FBCCDA771BBBAC648158F3C299FA5BCBC19497734136D` |
| `src\AiDe.Core\AiDe.Core.csproj` | Unchanged | `B63C41B97201DC3E1016B72B404A32411AC31E477231B281219DDA64D0AD584A` |
| `src\AiDe.Mcp\AiDe.Mcp.csproj` | Unchanged | `C7C0B953C60DD8CF638E8725FB10FFC9B709F0B76AD52267553BFFC70AA68AB0` |

All three loaded binaries are under `tests\AiDe.Core.Tests\bin\Debug\net10.0`:

| Binary | RED | GREEN/compatibility/reliability |
|---|---|---|
| `AiDe.Core.Tests.dll` | `361E3A25E21EEDEA986BD40A646F8D352FDCBB4E806CA74B12456E6941CF2C60` | `5F7CF90CFDFF468344124C640EB7BB78C81A8AB6C8895664A43902568F39A1FC` |
| `AiDe.Core.dll` | `CD037D365519BF085611C03024474EDD50717095EFD05C0D8E8085898AAB304D` | `60E9A5D40F102E4128F5D75D06A27D2898545EFB59035AF6966D09FB7F4D6E62` |
| `AiDe.Mcp.dll` | `67BC41EC06F56F8157F0471B4D91A2CF57F6DC86CE6C7C3B5D0BBF3AADB3E90A` | `0DF0D0F7F19BC6F070A1FC6715708A7E66F7FB4C7B55A7104270AF678023B55C` |

Hashes identify observed local bytes; no deterministic cross-machine binary claim.
Git may normalize the pre-existing CRLF reliability file on staging; the hash above
deliberately identifies **executed working bytes**, not an asserted Git blob hash.

### Finite remaining findings / DoD qualification

1. **R1 remains RED:** `PumpOnce_UnchangedLogTwice_PreservesOneOriginalMessage`.
2. **R2 remains RED:** `PumpOnce_ReopenedComposition_PreservesObservationsAndLifecycle`
   for ended=false/true, and
   `Apply_ReplayedRegisterOfEndedSession_PreservesEndedGenerationAndHeartbeat`.
3. **Later P2:** three-cache constraints, capture validation/source-gap semantics,
   observation-only replay, receipt/effect/checkpoint atomicity, pending/thread/
   tombstone/rebuild tests, migration deployment and rollback, capacity/query-plan/
   scale evidence. The reduced Data spike does not clear these.
4. **Independent C#/Data/DS/Test/SRE review is outstanding.** No self-issued PASS.
   No binary-compatibility, historical-tie completeness, GUI, property/fuzz campaign
   or production performance claim. D0/D1/D4/D5-provider/D6/D7/A2 apply; native
   tests prove these selected boundaries, not full MCP transport qualification.
5. **Instrumentation gap:** durable Seq/identity and explicit exception expose
   allocated order/refusal on the normal path; no new unconditional latency,
   throughput or failure-rate SLI was added. Those measurements remain not recorded.
   No log payloads or identifiers were exported. AIDE_SESSION/AIDE_CONTRACT_LOG are
   absent, so no episode evidence event can be emitted here. The existing proof is
   under `docs/proofs`, not the scorer's `docs/proof` accepted namespace.
6. **Conductor-owned artifacts:** derived index/backlinks/rollups and central
   defect-register integration remain unedited by this narrow author. Class → sweep
   → derive → prevent is recorded in design §11 E and the oracles above; no claim
   that the central register has been updated. Audit rendering incidentally produced
   `audit-data.js`; only that derived local byproduct is reverted, never audit JSONL.
   Tool-output overflow cost repeated reads; the corrective control was quiet
   console + standard TRX + explicit exit/result readback, not a piped gate.

**Next:** independent C#/Data/DS review of the second commit and this exact proof.
Keep the tree for that review; no merge/push, P1 integration or activation.

## P2.3 native replay/effect author receipt — 2026-09-16

**Result:** the four original R1/R2 failures now pass unchanged through the real
writer, SQLite pump and reopened composition. The final scoped run is **259
executed / 259 passed / 0 failed / 0 skipped**, logical exit **0**. This is a
code-and-tests author checkpoint, **not a full-P2 or independent-gate PASS**.
The historical report above is retained unchanged; this section supersedes only
its statement that R1/R2 are still unimplemented.

### Scope and execution contract

Goal: atomically project native source records and replay their original effects.
Done when: the four original REDs, rollback/lost-ack/restart, source-integrity and
independent-connection controls pass, and code/tests/proof are committed.
Not in scope: P1 merge/copy, activation, authorization policy changes, canonical
adapter, retry scheduler, UI, P3–P5, live stores/endpoints, or upstream work.
Tier T2; fan-out 0; main-line budget 45 calls. Worktree:
`C:\Projects\ai-de-feature-xh-p2-projection`, base
`b6339e771c23772d9d9f4ffdc47310b0c342b527`.

Execution graph: source/API grounding → unchanged baseline RED → typed native
implementation → focused compatibility run → checkpoint correction → adversarial
commit-order mutation → restored final run → evidence/commit. These are data or
decision edges, not independent branches. No delegates or external research.
No measured token/spend estimate is available. Tool-output overflow caused repeated
grounding reads; that is an execution-cost finding, not evidence of extra rigor.

### Change reach and authority boundary

| Surface | Writer → reader; evidence |
|---|---|
| Raw source | `CoordContractWriter` → bounded immutable `CoordinationSourceCapture`; validated UTF-8, raw SHA-256 and byte coordinates precede lossy parser attributes |
| Source identity | trusted normalized directory + normalized file path, injectively encoded; fixed logical epoch; checkpoint read before capture and reloaded under the SQLite writer |
| Prepared input | inert typed records, canonical bytes and corrected binding; trusted ID factories are separate composition inputs, called only inside the transaction |
| Durable admission | initial feed receipt → event reference → native effects → terminal feed receipt → checkpoint → COMMIT; same owning SQLite connection/IMMEDIATE transaction |
| Session mapping | immutable applied registration receipt external id → native session id/generation; native readers and subsequent board/session observations use it |
| Native session | session dimension, heartbeat and ended rows written transactionally; historical replay does not mint capabilities, increment generation, clear ended, or refresh heartbeat |
| Native board | quarantined content and injection flag, bound session/repository provenance, parent existence and same-repository checks, native max+1 order under the same writer |
| Acknowledgement | only after commit, `InjectedContractIngest.Observe` publishes a `SessionRecord` observation, not a capability; replay returns original admission/message identifiers |
| Legacy direct Apply | exact captured typed registration is recognized durably without source coordinates or a call to `Register`; original phase-only R2 assertion remains intact |
| UI/canonical/live authority | unchanged and not claimed; public `Post`, `Reply`, `Acknowledge`, `Register`, capability verification and production DENY/enhanced-append-disabled policy remain unchanged |

The accepted three-table v8 DDL is unchanged. There is no fourth status/ACK table,
consumed-file ledger, in-memory replay dedup substitute, general callback transaction
framework, or payload-selected fault switch. The internal, instance-local fault enum
is a deterministic test seam immediately before and after commit.

### Executed receipts and falsifying inputs

All filenames below are beneath `docs/proofs/p23-native-evidence/`.
TRX files contain the complete test-run result records, failures and test output.

| Claim / oracle | Evidence and logical exit | Red observed | Confidence / limitation |
|---|---|---|---|
| Original pump replay and both restart variants retain one message, generation, ended flag and heartbeat | `p23-baseline-red.trx`: 7 total, 4 fail, 3 pass, exit 1; `p23-runtime-green.trx`: same 7 pass, exit 0 | Four unchanged baseline tests failed against base source | Verified, real writer/pump/SQLite/new composition |
| Direct replayed registration alone cannot reset lifecycle | Original `Apply_ReplayedRegisterOfEndedSession_PreservesEndedGenerationAndHeartbeat`, unchanged | Original R2 phase-only failure retained in baseline | Verified; value-only legacy path does not acquire authority |
| Receipt/event/native effect/checkpoint roll back together | `Pump_BeforeCommit_RollsBackEffectsReceiptsCheckpointAndMemory`; independent read-only connection asserts all durable tables/native effects empty and observation map absent | `p23-commit-order-mutant-red.trx`: moving COMMIT before the fault fails `Assert.Empty` on durable session; 1 fail, exit 1 | Verified; rollback mutant restored byte-for-byte before final build |
| Lost post-commit acknowledgement cannot duplicate effects or issue authority | `Pump_AfterCommitBeforeAcknowledgement_ReplayReturnsOriginalReceiptAndEffectWithoutAuthority`; reopened composition, original admission/message returned, unchanged feed count, public Post rejects forged capability | Explicit injected post-COMMIT IOException; committed native facts read independently before retry | Verified for the internal fault seam; not an OS process-kill campaign |
| Exact appended duplicate is accounted once; changed bytes under the same key cannot rewrite history | exact-duplicate and conflicting-duplicate runtime tests; original message/receipt ids retained; conflict preserves old checkpoint | Conflicting input returns `COORD_DUPLICATE_CONFLICT`; checkpoint growth tests initially failed with `COORD_CHECKPOINT_GAP` | Verified |
| Accepted source mutation/truncation/missing file cannot advance | three `Pump_AcceptedSourceGap_RefusesWithoutAdvance` cases; diagnostic and unchanged native/checkpoint state | Each adversarial source yields `COORD_SOURCE_GAP` | Verified optimistic snapshot contract, not filesystem exclusion/ABA |
| Incomplete tail defers, unsupported version receives explicit refusal | `Pump_IncompleteTail_DefersUntilLfAndReportsUnsupportedVersion`; counts/native state/checkpoint checked | Partial input is unaccepted; adding LF produces version-refused receipt | Verified |
| Bounded capture and page checkpoint transitions | record >64 KiB and >128 files refuse before admission; 127/128/129/257 record scenarios replay stably | Over-bound cases throw; growth controls failed before checkpoint correction | Verified selected boundaries; 32 MiB aggregate and 4 MiB page exact limits are implemented but not independently boundary-tested here |
| Stale capture loses no competing writer's effects | `Project_StaleCapture_DiscardsWithoutAllocatingOrPublishing`; allocators throw if called; independent connections | A stale expected checkpoint returns `Stale`, no results or allocations | Verified |
| Independent SQLite writers produce one map/message | `Pump_IndependentWriters_CommitOneMappingAndEffect`, plus unchanged native ordering tests | Concurrent independent connections and bounded completion; original ordering mutants are supplied foundation evidence below | Verified executions; this new test is not a deterministic barrier-controlled overlap proof |
| Pending work is not represented as completed | late-parent and episode fixtures retain non-null source/canonical bytes, `pending` state and explicit initial pending reason | Parent absent and no trusted lifecycle prevent application | Verified retention only; re-drive is not implemented |

Intermediate `p23-watcher-candidate.trx`: **226 total, 222 pass, 4 fail**, exit 1.
Its four failures shared the new checkpoint-advance defect. Explicit insert/update
branches produced `p23-watcher-green.trx`: **231/231**, exit 0. The restored final
run adds all `BoardOrderingTests` and `ContractBoardPostTests`: **259/259**, exit 0.
No test was skipped, weakened, or removed. `CoordinationReliabilityTests.cs` is
unchanged. Existing schema and native-ordering controls remain in the final selection.

Final command (from the assigned worktree):

```powershell
dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --no-restore --filter 'FullyQualifiedName~Coordination|FullyQualifiedName~MessageBoard|FullyQualifiedName~BoardOrderingTests|FullyQualifiedName~ContractBoardPostTests|FullyQualifiedName~SqliteWatcherObservationStore|FullyQualifiedName~IngestHostTests|FullyQualifiedName~TrustedRegistrar|FullyQualifiedName~RestartDoesNotMultiplySessions|FullyQualifiedName~WatcherHostTests' --logger 'trx;LogFileName=p23-final-restored-green.trx' --results-directory docs\proofs\p23-native-evidence --verbosity quiet
```

### Exact source / project / binary identities

`final-identities.json` records SHA-256 for all changed C# sources, unchanged v8
DDL, unchanged original reliability test, new runtime tests, Core/test project
files and the actual Core/test binaries used in the final run. It records base,
timestamp, result counts and logical exit. `mutation-identities.json` records the
mutant source/test/project/Core-binary/test-binary identities and exit 1.
The restored projection source SHA-256 is
`16CE12EAAD11A3D675F357EF33E3D35D1DBEF5D3A744B24BAAE147BF19892A64`,
identical to its measured pre-mutation SHA. The baseline binary SHA was not captured
before rebuilding; it is **not recorded**, not reconstructed. Its source is the
named base commit and its execution is the retained baseline TRX.

### Foundation gate receipts — supplied, narrow, not reissued by the author

- Native ordering `5bb`: independent **91 PASS** reported; real-overlap deferred
  mutant fails while both writes commit. Applies to native ordering only.
- Schema Data S1/S6 **PASS** at `85a` reported. Applies to the accepted v8 foundation.
- Schema/Test failure-evidence **PASS** at `b633` reported: 23 focal mutant failures,
  23 positive/restored greens, 202 candidate greens; production source unchanged.
- P1 mechanics `535b` independent **PASS** is a reference only. Nothing from that
  branch is merged/copied here.

These are provenance-labelled supplied review receipts, not fresh independent
review of this runtime patch. **Next gate: independent Data/Distributed Systems/Test
review of this exact code, artifacts and commit.** The author does not clear it.

### Correction class and instrumentation

**Class:** an INSERT-or-update shortcut conflicts with immutable INSERT guards,
even when the intended UPDATE would be valid. **Sweep:** the new runtime had one
checkpoint upsert; the native session metadata upsert intentionally targets a
mutable dimension and is not governed by the immutable checkpoint trigger.
**Derive:** read checkpoint under IMMEDIATE, choose INSERT for absence or UPDATE
for presence. **Prevent:** unchanged emitter growth tests plus duplicate/tail/page
boundary controls were observed failing and now pass. The central defect register
is outside this author's ownership; conductor integration remains explicit.

Operator questions have named sources: how many records/bytes, how many replayed,
pending/refused, which failure, and elapsed milliseconds are emitted in `LastRun`
on the normal path. A native-pump Activity carries the counters and error status
without raw content, session ids or repository paths. Durable feed/state/checkpoint
rows expose original admission and application state. Runtime tests assert volume,
replay, elapsed non-negativity, refusals and source diagnostic readback.
Activity export/listener behavior is not separately tested here; process-level
SLIs/export configuration and token/spend measurements remain unverified. On an
early capture failure, the counters describe returned/processed captures, not total
filesystem bytes attempted. No production latency or capacity claim is made.

### Remaining, explicitly not waived

Full P2 still requires canonical origin/adapter/bridge integration, pending-parent
and retry scheduling, full rebuild/tombstone behavior, 401/MCP-feed work, exact
aggregate/page-limit and broader malformed/property/mutation campaigns, actual
old-binary rollback/deployment, independent runtime gates and final instrumentation
qualification. UI/composition rendering is not exercised by this portable Core
slice. No source investigation report, lessons/site claim, P1 source, activation,
dependency/configuration/hook, live database, endpoint or upstream repository was
changed. P3/P4/P5 remain later work; upstream work follows verification of all six
phases. Keep this worktree for independent review; no push or merge.

## P2.4A author checkpoint — bound metadata reader (2026-09-16)

**A implemented and unit-verified; full P2 remains unshippable.** This section
updates the earlier “401/MCP-feed work remains” statement for this bounded A unit
only. Recovery B, canonical producers/bridge, released-binary rollback and the
independent implementation gates remain open. Production authority is **DENY**;
enhanced canonical append is disabled. The trusted overload is internal and
exercised only by isolated fixtures; no production composition opts into it.

### Approval, scope and execution

Base: `7dd92c51acae91fa4034749fc0f3bad4c731cd7c`. Contract was committed before
source changes as `2a9a66ad` (`docs(coordination): record approved P2.4A binding
and reader contract`). Design/ADR contain the accepted Data/DS A contract;
this is not independent code approval. The implementation is pinned by the exact
Git blobs below, avoiding a self-referential commit hash.

Goal: implement the approved bound, metadata-only A reader and actual MCP adapter.
Done when: contract-first commit, real constructor/reader/adapter tests, regression
membership readback, and a committed evidence checkpoint. Not in scope: B,
production activation, human-channel qualification, P3–P5, actual old-binary
rollback, upstream work, GUI/App or live stores. T2, fan-out zero, 45-call ceiling.
Actual graph: scoped contracts → contract commit → schema/API RED → implementation
→ bounded fault/boundary corrections → restored tests → evidence. No nested agents.
The initial broad console reads exceeded the tool output bound; subsequent reads
were scoped. That was retrieval overhead, not evidence of code behavior.

### Writer → store → reader → wire reach

| Surface | Implementation and scope |
|---|---|
| Trusted writer | Internal `CoordContractLogPump` overload supplies existing `RepositoryIdentity` and stable origin. No attribution from registration. Known typed register/update/board/episode repository contradictions reject before effects. |
| Durable binding | Same three caches, fresh pre-release v8. Checkpoint has all NULL or all bound fields, unique scope hash, immutable binding/scope/epoch, and zero only at the empty digest with no accepted content. No candidate-v8 upgrade or NULL→bound backfill. |
| Compute reader | `CoordinationProjection.cs` validates binding before querying receipts in one deferred read transaction. Source/epoch-local H and `(scope,epoch,n)` index; immutable receipt outcomes, including duplicate accounting and tombstones. |
| Interface | `IWatcherObservationStore.ReadCoordination`; default Unsupported. Request repository is internal and derived from a service-supplied SessionRecord. |
| Adapter | `BoardTools.ReadCoordination`; legacy `Read` and public Post authorization are unchanged. |
| Wire/composition | Actual `Tools.Schema` and `Tools.Call` register/dispatch `aide_coordination_read`. Only `source_id`, structured `cursor`, `limit`; reject overrides, explicit nulls and malformed cursors. No public scope/root/repository argument. |

The cursor is the returned JSON object, not a base64 credential. It contains
SourceId, ContractVersion, Epoch, AfterN and nullable FrozenHighWater. It is not
authorization. Continue with Continuation; after completion use FreshResume,
which drops frozen H. No current-state event join rewrites earlier outcomes.
Output omits raw scope, filesystem root, event payload and prose; only allowlisted
stable reasons travel. Availability is explicitly CacheRead. Recovery, source
health, lag and loss evidence are NotRecorded, not invented zeros or live claims.

### Executed claims and oracles

All tests below are in `tests/AiDe.Core.Tests/Watcher/CoordinationFeedTests.cs`.
Raw receipts are confined to **new** `docs/proofs/p24-feed-evidence/`; older raw
TRX files and recovery tests were not changed. This singular-path proof pack
remains the canonical evidence narrative.

| Claim | Concrete oracle / evidence | RED observed | Confidence / limit |
|---|---|---|---|
| Fresh binding columns and honest empty checkpoint | `FreshSchema_CheckpointBinding_HasExactlyThreeNullableColumns`, `FreshSchema_BoundEmptyCheckpoint_AcceptsOnlyEmptyDigest`; old constructor had zero columns and rejected the insert | `p24-schema-red.trx`: 2 failed | Verified for fresh v8, not candidate-v8 upgrade |
| 401 source receipts page 200/200/1 despite global interleaving | `Read_Interleaved401Receipts_Freezes2002001ThenResumesNewTombstone`: source H=402, 401 distinct receipts, 200 pending admissions, 199 message-bearing applications, one duplicate occurrence | Missing-API compile RED, then frozen-H mutation | Verified through actual store/interface/BoardTools |
| Frozen snapshot ends; fresh resume sees later tombstone without rewriting admissions | Same test: frozen reread remains one entry; resume returns receipt 403, original admission 400 | `p24-frozen-high-mutant-red.trx`: replacing requested H with current H leaked the tombstone into the earlier page and failed collection equality | Verified; mutation restored |
| Repository checks include empty and unknown sources | `Read_EmptyBound_LimitAndAbsenceAreDistinct`, `Binding_LegacyOrContradictorySource_IsNeverBackfilledOrApplied` | Schema RED plus explicit mismatch fixtures | Verified; structural qualification only |
| Binding cannot change during an otherwise valid advance | `Binding_ValidOffsetAdvanceCannotHideRebinding`, immutable/replacement/partial/collision tests; real SQL and foreign keys enabled | Source columns absent on baseline; no separate mutation campaign for each constraint | Verified executions; per-constraint mutation confidence unverified |
| Binding contradictions on updates cannot slip past registration-only checks | `Binding_UpdateRepositoryContradiction_RefusesWholeCaptureBeforeEffects` asserts zero session/event/checkpoint effects | `p24-boundary-red.trx`: no exception before shared typed-record validation | Verified |
| Limit 1/200 accepted; 0/-1/201 and bad cursor ranges rejected; version/epoch reset | Limit and cursor theories, plus other-source receipt/token test | Missing-API RED; boundary assertions executed restored | Verified |
| Binding, local maximum and rows share one snapshot | `Read_ConcurrentWriterAfterBinding_UsesOneSnapshotForMaximumAndRows`: barriers commit tombstone on another connection after binding read; old snapshot has four rows/H=4; fresh read sees tombstone | Deterministic concurrent schedule executed; no isolated transaction-removal mutation | Verified execution; broader interleavings unverified |
| Actual MCP schema/dispatch reject hidden override fields and explicit nulls | `Mcp_ActualSchemaAndDispatch_RejectsOverridesAndReturnsOnlyMetadata`, `Mcp_NullCursorOrLimit_RejectsSchemaViolations` | Null cursor/limit first returned Available; boundary RED captured | Verified in-process real dispatch, not live stdio/human transport qualification |
| Failure is unavailable, not empty success; no exception-path leak | `Mcp_RealExclusiveLockOrMissingFile_ReturnsTypedUnavailable`, `Read_StorageFailure_IsTypedUnavailableAndEmitsSafeSpan` | Actual exclusive lock, missing file, disposed store, injected SQLite busy exception | Verified; busy path retains provider default timeout (~30 s observed), no latency SLO claimed |
| Metadata and telemetry exclude synthetic private markers | 401, malformed-refusal, MCP and failure tests assert marker/root absence. Span status, entry count and elapsed field are read back | Synthetic forbidden payload/reason/exception markers are the negative inputs | Verified fixture outputs, not a general PII classifier |

### Results, membership and pins

| New raw receipt | Observed result |
|---|---|
| `p24-schema-red.trx` | 0 passed / 2 failed |
| `p24-reader-first.trx` | 21 passed / 4 test-harness failures |
| `p24-reader-green.trx` | 25 passed / 0 failed |
| `p24-frozen-high-mutant-red.trx` | 0 passed / 1 expected mutation failure |
| `p24-boundary-red.trx` | 0 passed / 2 expected pre-fix boundary failures |
| `p24-final-feed-green.trx` | **32 passed / 0 failed / 0 skipped** |
| `p24-prior-304-green.trx` | **334 executed / 334 passed / 0 failed; 331 distinct displayed names** in the original class selection. The old baseline has **304 executed results / 301 distinct displayed names**; multiset comparison preserves all 304 occurrences, zero missing. Filename names the baseline selection, not the current count. |
| `p24-mcp-green.trx` | **72 passed / 0 failed**; overlaps other selections, so do not sum counts as unique tests |
| `p24-recovery-still-red.trx` | **3 passed / 2 intentional existing failures**; separate from all green claims |

Recovery failures remain exactly:
`Pump_LateSameRepositoryParent_AppliesChildOnceWithOriginalAdmission` (child pending
at admission 3), and
`Pump_PendingLimitPlusOne_AccountsForParentWithoutExceedingActiveBoundsOrLosingChildren`
(1,025 active pending, limit 1,024; 323,572 retained bytes). They are not A fixes.

Runner: .NET SDK **10.0.303**, target net10.0, Microsoft.Data.Sqlite **10.0.11**,
xUnit **2.9.3**, Microsoft.NET.Test.Sdk **17.14.1**, VS runner **3.1.4**.
`dotnet test ... --no-restore --filter FullyQualifiedName~CoordinationFeedTests`
built the final binaries. Subsequent class-selection, MCP and recovery runs used
`--no-build --no-restore` against those same binaries.

| Source/test path | Git blob |
|---|---|
| `src/AiDe.Core/Watcher/WatcherObservationStore.cs` | `952c5f81a79fcca63fca1d21571ecfb45ffe03e1` |
| `src/AiDe.Core/Watcher/SqliteWatcherObservationStore.Coordination.cs` | `9e5ecfcd13ed2ee5362d9cb1bbf51a0638bb2b03` |
| `src/AiDe.Core/Watcher/CoordinationProjection.cs` | `6ea40a1e7d7f9e4b57927bd86a84a1864dc50de6` |
| `src/AiDe.Core/Watcher/CoordinationContractLog.cs` | `97c2cb2afbb38cc392d50a55da86319d3b15e5a6` |
| `src/AiDe.Core/Watcher/CoordinationRead.cs` | `c905c9f5dcad97371e5b20ea59d2a9727d31c2bd` |
| `src/AiDe.Mcp/BoardTools.cs` | `bbdf633f0da397f41d10ec18443186b198f9501f` |
| `src/AiDe.Mcp/Tools.cs` | `9d2e4cd8aa22809562ffd68f8401915be960042e` |
| `tests/AiDe.Core.Tests/Watcher/CoordinationFeedTests.cs` | `af0223466f31e2146b8e2801dd94890189aa3b40` |

| Final Debug/net10.0 binary | SHA-256 |
|---|---|
| `AiDe.Core.dll` | `9EB1B7446E5FEFD942D4C07BEFCD4E274ACD3C53E8C2A26A9EBAF15CDA0EC0E7` |
| `AiDe.Mcp.dll` | `52E1948598454C84AB7E4ED76CE0A9B913B7F8EE33EF040528F8D5F1B125E8D4` |
| `AiDe.Core.Tests.dll` | `F2172642575827EE7800CB7FF700DBFF4B901C27AF1E3B3496D140CB04A4409B` |

### Corrections, instrumentation and limits

Class → sweep → derive → prevent:

* A file-content result was initially associated with the wrong parallel read.
  The exact-context patch refused before modifying either affected file; direct
  path inspection located runtime in `CoordinationProjection.cs` and DDL in
  `SqliteWatcherObservationStore.Coordination.cs`. Corrected patch targets were
  checked by compilation and the real constructor tests. No new central lesson
  entry is authored from this lane.
* Test invocation is not test execution: three boxed Int32 theory values could
  not bind to nullable Int64, and a test reused an already-parented JsonNode.
  Swept the new theory high-water literals and local MCP request builder;
  use explicit long literals and fresh node ownership. The four initial failures
  remain in the raw receipt; the final run executes all 32 assertions/cases.
* Registration-only validation misses other typed records' repository claims.
  Swept register, update, board, episode-open and episode-close; derive checks
  from one shared validator used before binding and at projection. The update
  contradiction test was observed red, then green.
* Optional and explicit-null are different schema states. Swept cursor and limit;
  reject explicit null before defaults. The real Tools dispatch test was observed
  red, then green. Neither correction changes native Post authorization.

Operator sources: normal `coordination.cache.read` Activity emits status, entry
count, elapsed milliseconds and stable error type. Existing pump LastRun remains
unchanged in meaning. No source IDs, roots, payloads or exception messages enter
the new span. No recovery fields, new database, dependency, application surface,
production producer, upstream investigation or installation/configuration change.

Empty-file binding enumerates existing zero-length files because legacy captures
have no page/scope for them; nonempty scopes come from captured pages. This
retains the approved optimistic filesystem contract, not exclusion or ABA
protection. Concurrent filesystem identity replacement and actual human-channel
qualification remain unverified. Existing candidate-v8 upgrade is not supported
or claimed. Actual released-v7 binary rollback remains later qualification.

**Next gate:** independent Test/Security/Data review of these pinned code and
evidence surfaces. Recovery eligibility/counter-domain clarification precedes B.
Full P2 producers/canonical bridge, rollback, P3/P4/P5 and upstream-after-all-six
verification remain open. Retain this worktree for review; no merge or push.

### Native NOTICE P2 — current-failure evidence, 2026-09-16

**RED / UNSHIPPABLE.** Source base:
`184906558911f3f49115d44781781aa094adfa47`. This addition contains tests and
evidence only. No production C#, schema, App/UI, endpoint, dependency, hook,
configuration, live database or other producer tree was changed.

| Claim / oracle | Executed evidence | Confidence / boundary |
|---|---|---|
| N1: after accepting one corrected registration, failed publication must leave the exact original correction available for one retry without another registration | `RegistrationNoticeReliabilityTests.DrainRegistrationNotices_PublicationFails_RetryRetainsOriginalCorrection`: actual `Publish` writes `.tmp`, then its final rename hits an owned destination directory. Windows raises `UnauthorizedAccessException`; after removing the obstruction, the real next drain returns **0**, failing `Assert.Single` | **Verified failure**, not successful recovery. Original session ID, sent/used repositories and reason were checked before delivery. No stable notice ID exists in the current record, so equality is original record value, not invented ID semantics. Retry publication assertions after `Assert.Single` were not reached |
| N1 retry must not re-register, rotate authority or change lifecycle | Real SQLite row count, generation, heartbeat, ended state and registrar capability count agree before failure and after retry. The original capability still verifies; the session was explicitly ended and the clock advanced before retry | **Verified on this run**. One `Register` call only. No cold-restart or external authority claim |
| N2: 128 outstanding corrections shared across two hosts/store handles must refuse number 129 before native mutation | Two real SQLite handles on the same owned `watcher.db`, two real registrars, 64 notices in each real host queue. `Register` number 129 succeeds: native dimension rows **128→129**, capability entries **128→129**, pending corrections **128→129**. Full before/after generation, heartbeat and ended snapshots are in TRX/receipt. Fails `Assert.Equal(before, after)` | **Verified legacy-path diagnostic**. This is not an unqualified promise that the future enhanced entry point uses the same API. No destructive drain is used to count quota; read-only reflection inspects the actual `_notices` and capability dictionaries |
| Ordinary publication and identical retry preserve existing native projection | Positive case checks original fields/provenance, session-keyed filename, one JSON document, identical SHA-256 bytes on repeat `Publish`, unchanged native state and no root JSONL | **Verified**. Proves existing overwrite behavior, not immutable notice-ID retention or canonical cross-harness delivery |
| Non-correction produces no notice | Canonical repository registration creates one real native session and zero queued/published notices | **Verified** |
| Locator substitution has fidelity | Known-root test compares the substituted `IRepositoryLocator` with `FileSystemRepositoryLocator` over owned synthetic git-marker directories, canonical roots and unknown paths | **Verified**. No private checkout or arbitrary user filesystem lookup |

**Final finite run:** 23 executed, **21 passed / 2 failed**, dotnet logical exit
**1**, measured **3.7772216 seconds**. Five new cases (three positive, two semantic
REDs); seven existing `AWorktreeRegistrationIsCorrectedAndSaidSoTests` and eleven
`TrustedRegistrarTests` passed. No zero-selection proof or skipped new case.
The receipt checker returning success means the expected-failure evidence was
validated; it does **not** turn the test command green.

Raw artifacts, all in `docs/proofs/p25-notice-evidence/`:

* `semantic-red/native-notice.trx` — authoritative executed assertions and output.
* `semantic-red/console.txt` — full invocation, build/test output and logical exit.
* `semantic-red/receipt.json` — named results, measured duration and SHA-256 for
  actual watcher source, new test source, project/build inputs and built Core/test DLLs.
* `run.ps1` — finite, fail-closed receipt runner; requires the two named semantic
  assertions **and** their diagnostic values, not merely two failing method names.
* Root `native-notice.trx`, `console.txt`, `receipt.json` preserve the **first
  attempt**, which is **not N1 semantic evidence**: an over-specific `IOException`
  expectation stopped at Windows' `UnauthorizedAccessException`.
* `prompt.txt` — verbatim task for the append-only audit entry.

**Class → sweep → derive → prevent.** Two production failure shapes remain
unfixed: destructive dequeue before side-effect success, and admission bounds
applied nowhere before authority/state mutation. The scoped source sweep covered
`IngestHost.Register`, its real notice queue/drain, `TrustedRegistrar.Issue`,
SQLite native state and `RegistrationPublisher.Publish`. No second notice
producer was added. Future authoritative admission/delivery must derive from one
durable source rather than synchronization of unbounded host queues. N1/N2 are
the observed-failing controls. The fixture correction exposed a third shape:
**counting expected failing names as semantic proof**. The runner now rejects
setup/exception-type failures and requires the semantic assertion and diagnostic
state. Register integration belongs to the conductor; no out-of-ownership
lesson-register edit was made.

**Scope/review gates remain closed.** These tests do not implement the proposed
two native append-fact/delivery tables in the same `watcher.db`, shared
reserved/in-flight/hydrated quota, recovery, canonical notice publication,
immutable identity, ACK handling or old-client fencing. Data/Distributed Systems/
Security design acceptance is required before that production/schema work.
Legacy clients cannot be fenced by claiming new APIs or adding triggers to old
writes; compatibility and actual released-binary qualification remain separate.
No additional agents or self-cleared hard veto. Operator evidence here is the
normal test output/TRX; production observability remains pending.

P0–P5 remain **approved for execution**, not verified complete. Production
producer, recovery B, canonical bridge, old-binary qualification and P3–P5 remain
open. Cross-harness obligations must later use official canonical requests.
Routing-typo/triage vectors stay backlog items; no peer research or reply action
was taken. Upstream transfer waits for verified completion of all six phases.
Keep this isolated branch for review; do not merge or ship its intentional REDs.

### Native NOTICE schema review response S1-S3, 2026-09-17

The exact correction and claim ledger are in
[p2-notice-schema-s123.md](p2-notice-schema-s123.md). Six fixed-hex columns now
reject NUL and enforce exact UTF-8 byte lengths. Five distinct malformed cases
were observed RED before repair. Candidate: 175 executed, 173 pass, only the
unchanged N1/N2 REDs; 141/141 schema/evidence cases pass.

Historical structural RED was 36 missing-table failures, one version failure,
one cleanup IOException, plus N1/N2; it never established 71 semantic constraint
mutations. New evidence executes 29 required-column mutants, three invalid-trust
cases against a weakened constraint, and one isolated due-time type mutant.
Populated current-branch v8 contents and all sqlite_master definitions survive
the actual second-table constructor collision unchanged. This does not qualify
released v7/v8 binaries, prior prerelease variants, existing v9 repair, durable
admission atomicity, payload erasure, native delivery, or full P2.
