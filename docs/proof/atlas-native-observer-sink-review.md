---
id: proof-atlas-native-observer-sink-review
title: "Independent FR-NO-001 sink correction review"
type: doc
status: accepted
owner: "@timianmalloo"
links:
  - { to: investigation-atlas-p1-02-native-uia, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Historical receipt: Independent FR-NO-001 sink correction review"
---

# CLEAR FR-NO-001; complete observer clearance remains open

**Verified:** the exact shared-receipt formatter poisoning counterexample is corrected at
`715523d0bef2db92364f73a6d84ac79290ffae13`. This independent re-review clears FR-NO-001
from receipt commit `4d005a9e276cc1c3070005f3bc2ecab7eecde997`; that historical BLOCK
receipt and raw counterexample remain unchanged. This is not complete observer/native
qualification: the remaining view-state observation floor below is explicitly outstanding.

Native file: `tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs`.
Byte SHA256: `ae749110bb063a71609b2df3de68203f4002a223291486912786f1fd24735dca`.
Author proof blob: `6d842c086c3e15500bb86f98f8f1060fba272c1c`.
Both pins were verified before and after receipt closure; no source/proof edits occurred.

## Goal and bounded execution

Goal: independently recheck the exact sink correction against the original counterexample,
Owner A and the retained 17 plus four new controls. Done when its observed outcome and
remaining applicable floors are recorded in a committed review. Tier T2; six-call,
twelve-minute ceiling; fan-out 0. Graph: exact diff/authority -> serial normal build and
same-receipt replay -> actual TRX/JSON and floor readback -> receipt/audit/release.
The scratch build followed completion of the test build, avoiding shared output contention.
No whole class, full App, shown-window, native UIA or product operation was run.

Applicable forensic review, no-guessing, Testing Strategy D7 and original design obligations
remain in force. Root AGENTS is the only discovered AGENTS.md. Parent owns correction
admission, central records, derived join and any later native slot; review creates none.

## Correction inspected

The full source diff from 781acb2b adds four permanent cases and one local admission
method. `Receipt.MarkObservation`, lines 1707-1712, uses SerializeToElement with the
actual Receipt.SerializationOptions before calling Mark with the resulting JsonElement.
It does not publish the original object after validation. All three observer publication
sites use this method: WPF packets at 566, lease transitions at 569, status at 570.

The original Mark, Save and SerializationOptions bodies were mechanically extracted and
compared with 781acb2b: unchanged. Existing UIA query, helper, await/capture integration,
original assertions, order, waits and timeouts are not changed by this correction. No
rollback, new dependency, generalized receipt framework or original-error swallowing appears.

**Test Architect and SRE: CLEAR FR-NO-001.** Pre-serialization removes the demonstrated
formatter-failed object before shared publication. The actual serialized snapshot survives
later saves without revisiting its formatter. Existing Mark semantics still apply after
successful admission, including a later filesystem failure; this is not a durability repair.
**Architecture/Security: CLEAR this correction.** Structural payload shape and private
authority limits are retained. **Simplifier: CLEAR.** One BCL serialization primitive and
one local admission method solve the observed seam without widening the machinery.

## Independent replay of the original counterexample

Command: `dotnet run --project .artifacts/native-sink-review/SinkReplay.csproj -v q`.

This uses the same actual Receipt, existing ThrowingPacketConverter and real observer
Sample/Flush sequence as the original SinkProbe. Reflection only invokes those methods;
it mutates no private state. The same unavailable-input packet is used, with no Window,
HWND or automation provider. The replay's oracle was deliberately changed from expecting
the defect to requiring successful original-after persistence; old log/exit meanings were
not reused as correction evidence.

Observed: original.before saves; observer Flush returns with SinkUnavailable=true;
original.after through the **same receipt** saves successfully. Actual JSON has exactly
`original.before, original.after`, FailureCount 0. Marker:
`CONFIRMED_SAME_RECEIPT_CORRECTION_ORIGINAL_AFTER_PERSISTS`. Exit 0 verifies these checks.

## Normal-build tests and actual saved JSON

Command: `dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --filter
"FullyQualifiedName~NativeObserver_NonGui_" --logger "trx;LogFileName=sink-review.trx"
--results-directory .artifacts/native-sink-review -v q`.

Actual TRX: **21 executed, 21 passed, 0 failed, 0 not-executed**. All names and outcomes
were read; the prior 17-name set is a strict subset with exactly four new cases. New cases
are shared formatter failure with original sentinel/null assertion/success, and immutable
single serialization. Existing identity, actual adapter, four bounds, wrong-thread, six
await/finally/sink, release-event, query-source, null-content and RuntimeId cases remain.

The three shared-receipt tests execute the real await/finally and actual observer adapter
on unshown owning-STA controls. They assert the identical sentinel exception object,
original NotNull exception, or success; delegate count stays one. Persisted receipt files
were independently opened: both original stages survive; later dispatcher.drained is also
present, FailureCount 0 and Completed false. No observer packet was admitted by the failed
converter. These are unshown WPF controls, not live UIA evidence.

The one-write converter test's actual saved JSON contains observer.wpf Attributes as the
object `{ "Snapshot": "frozen-packet" }`, not a quoted serialized string. The converter
write count is exactly one after subsequent original Mark/Save. Later status writes also
succeed. The recorded immutable shape, not just exit 0, discharges publication fidelity.

Author TRXs were independently parsed: red 21/17/4, fix 21/21/0, mutant 21/20/1, restored
final 21/21/0; all have zero not-executed. Red's three shared-write cases fail with
DO-NOT-EMIT-FORMAT and the immutable case fails SinkUnavailable. Publishing the original
object instead of the snapshot fails the immutable single-serialization case. These actual
messages discriminate the correction; the mutant is post-fix, not historical red-first.

| Raw receipt | SHA256 |
|---|---|
| .artifacts/native-sink-review/sink-review.trx | be8fa5b35073f991c414e74f779f750294d1a41da65a25b673dded0082819709 |
| .artifacts/native-sink-review/replay.log | 6a6c298e2eab5952618741658b5fce0ac338c650e4e23e2da635edee0551df1e |
| .artifacts/native-sink-review/observed-evidence.json | 64af8f288681adc493af2f50bc7ebb461a193972f0d123ba947dcf3bcd7fdbc1 |
| Author sink-correction-final.trx | 998b5b9be866c15809cac3d3b10c89ac045b235b4dec34ca56ba742cf6c6b2cb |
| Author sink-correction-mutant.trx | 505fd0172e4b1567a00e8db16e994b3f4d2ba3797c28e2fd7b65802d5146d94b |

`observed-evidence.json` records full test inventories/messages/timestamps and the exact
paths/hashes of this review's three shared-failure and one snapshot receipt readbacks.
Raw author evidence remains in its original .artifacts/atlas-instance-design directory.

## Failed, buffered and persisted are distinct

Formatter failure occurs before shared mutation; the packet may remain in the bounded
observer buffer. The attempted index advances and is not retried automatically. A failed
Flush may omit status; the in-memory SinkUnavailable flag is not durable evidence. After
serialization succeeds, an ordinary Save failure may retain a safe JsonElement in memory
under unchanged Mark semantics. A subsequent successful original Save may persist it.
No guarantee of filesystem recovery, atomic capture, zero observer effect, performance,
native causality or exhaustive UIA fidelity is made.

## Remaining applicable floor: view loaded/visible observations

**Verified gap in the previously stopped overall implementation review.** The settled
design appendix, docs/proof/atlas-p1-02-native-uia.md:170, requires view loaded/visible
along with other view scalars. Current `ViewObservation` at native lines 366-368 does
not carry IsLoaded or IsVisible. This review opened the actual adapter's emitted Views
JSON: neither field exists. Host and Window have their own flags; those are different
objects and cannot establish the state of the independently retained/detached view.

This was not repaired or silently waived in the sink unit. Full observer clearance must
still discharge this existing floor through actual per-view values/unavailable states and
adapter JSON readback, or receive an explicit Owner scope decision; this reviewer supplies
neither a source fix nor a policy override. The 21 passing tests do not cover absent fields.
The original implementation review stopped at FR-NO-001, so no exhaustive full-observer
clearance is inferred. Future shown/native fidelity and P1-02 causal qualification also remain.

Completed: narrow FR-NO-001 correction is independently CLEAR. Remaining: the named
view-state floor and full observer/native qualification. Next action: Parent records this
split verdict and settles the existing floor before treating the observer as fully cleared.
Actual six tool boundaries, no delegates; measured duration in closing audit. Source and
author proof remain unchanged; only new receipt and official own audit committed. Exact
lease released in finally with PRIMARY liveness readback; raw/generated dirt retained.
