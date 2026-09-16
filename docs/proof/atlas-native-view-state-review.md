---
id: proof-atlas-native-view-state-review
title: "Independent clearance of complete admitted Atlas observer preparation"
type: doc
status: accepted
owner: "@timianmalloo"
links:
  - { to: investigation-atlas-p1-02-native-uia, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Historical receipt: Independent clearance of complete admitted Atlas observer preparation"
---

# CLEAR: complete admitted test-only observer preparation

**Verified at source `626d16a211757db5e4da7515cf234a686f610e6c`.** The previously
outstanding per-view state floor is met, FR-NO-001 remains cleared, and no remaining
preparation hard veto was found in the settled observation-only envelope. This clears the
implementation/non-GUI preparation gate. It does not clear a shown/native run, P1-02's
unresolved cause, full application qualification or publication.

Native file: `tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs`.
Byte SHA256: `6af54fc651f355fbf22a4b17ecb256007fe6fa4ca8061b382cf61a45eb59841a`.
Design/proof `docs/proof/atlas-p1-02-native-uia.md` blob:
`300efe1f4665a72c956acd127573d479ccd0442d`. Both remained unchanged by this review.

## Scope, authority and verification plan

Goal: finish the complete admitted observer preparation review, not only its two new fields.
Done when settled fields, lifetime, original outcome, cost and bounds are accounted for by
source and actual adapter JSON plus an independent bounded normal-build run. Tier T2,
fan-out 0, eight-call/twelve-minute ceiling; checkpoint at five. Graph: frozen delta and
prior exact evidence -> own normal build with parallel read-only source review -> actual
TRX/JSON and full obligation matrix -> verdict/receipt/audit/release. No loop or scope expansion.

Owner A and the foreground preparation grant retain public/test-reference-only observation,
private generation and view-to-lease not-observed, Root RuntimeId after the original query,
and separately timed STA brackets. Root AGENTS is the only discovered nested guidance;
forensicreview, no-guessing, Testing Strategy and the previously applied review floors remain.
The structural surface trace is held references -> STA sample -> MTA original query/later
root -> STA sample -> immutable receipt -> actual JSON -> non-GUI checks -> later native gate.

Prior independent evidence is reused where the source is preserved: design CLEAR
973afc97a8bcd981e607aa73be0c89247045a3c5; initial implementation BLOCK
4d005a9e276cc1c3070005f3bc2ecab7eecde997; sink correction CLEAR
12c1f88336bc6d640c1dfdd6e76929acdda6b6f2. Their historical receipts are not overwritten.
The exact final delta from 715523d0 is 21 changed source lines: field declaration, independent
reads and persisted adapter oracle. Other source changes are absent. The remaining per-view
floor named in 12c1f883 is discharged below; it is not silently waived by this clearance.

## Complete preparation obligation matrix

All line references below are in the final native file unless a document is named.

| Obligation | Source and observed evidence | Disposition |
|---|---|---|
| Exact reference identity and multiple hosts | ReferenceEquals registry 412-420; independent host Content/ReaderView reads 514-529; held references and multiple candidates 425-434/506-516. Actual adapter returns two hosts, preserves mismatched Content and ReaderView, and retains old view ID 3. Equality/current-child discriminators retained. | CLEAR |
| Object-specific fields | Records 375-386 carry host/window state, view controls/document/counts/read-only/navigation and now nullable view IsLoaded/IsVisible. Reads 536-546 independently use the actual view. Own persisted view ID 3 has both false, detached and retained; host/window flags are not substituted. | CLEAR |
| Null/unavailable truth | Null reader comparison is guarded, nullable counters/flags stay null until read; wrong-dispatcher packet has null visited/queued plus its reason. Getter/sample failures emit bounded reasons, not invented values. The two new scalar catch branches were source-reviewed; no synthetic getter-failure or shown-true result is claimed. | CLEAR preparation |
| Attachment versus absence | Bounded parent chain 471-483 returns owned-window/detached/unavailable; census/held/observer-retained markers are separate. Actual child replacement and removed host retain distinct reference/attachment rows; a truncated census does not establish detachment. | CLEAR |
| Reader/lease authority | Separate wrapper/inner/lease IDs and release event 438-451/550-560. Actual release-event control is retained. BackingLeaseRelation and PrivateGeneration remain not-observed; chronology and roles never assert the missing private edge. Host/view private fields were re-opened to confirm the boundary. | CLEAR |
| Lifetime and bounded storage | Native body has using observer at 746. Registry/held lists cap at 256; transitions at 32; packets at 16. Dispose 595 flushes and clears all strong object-reference lists. Packet records retain scalar IDs/data, not live WPF/lease references. No claim about producer retention or GC follows. | CLEAR by source plus retained identity controls |
| Finite census/rows | Source caps nodes/queue 512, depth 48, hosts 16, views/readers 32 and reasons 32. Actual node-limit packet: visited 1, queued 1, truncated. Depth-limit: visited 1, queued 0, truncated. Reference-limit status: references 1 at cap 1, truncated. Packet-limit status: one packet at cap 1, truncated. | CLEAR |
| Cost and timing | Stopwatch and UTC delimit separate packets and query intervals; actual packet EndTick >= StartTick. The 25ms between-operation check is a soft observation-work budget, not preemption of getters/provider/I/O or a hard latency promise. Missing/failed samples remain explicit. | CLEAR, native cost unmeasured |
| Thread/batch semantics | Existing STA dispatcher and 30-second timeout remain; caller wrapper 597-610 samples before and in finally after the one existing await. MTA query remains Task.Run. No observer dispatcher action, wait, focus/layout/activation/peer refresh/retry was added. | CLEAR source and actual await controls |
| Original result and provider root | 1053-1085 retains one FromHandle/process check, five names in original order, one Descendants/Name FindFirst per name, same returned element and NotNull/IsOffscreen assertions. Existing after-original root helper 1088-1114 is preserved. RuntimeId is read afterward and bounded at 32, never reread pre-query. | CLEAR preparation, not live execution |
| Capture preservation | Existing three capture sites retain one Render; samples bracket it without new layout/focus/render work. The caller's original exceptions remain decisive; added sample/flush failures are bounded. Pixels and packets are not simultaneous. | CLEAR source |
| Persistent failure containment | All three observer kinds call MarkObservation 583-587; 1724-1728 serializes to immutable JsonElement before unchanged Mark/Save. FR-NO-001 same-receipt sentinel/null/success and single-serialization controls pass again. Failed formatting cannot insert its object into shared events. | CLEAR; prior veto remains discharged |
| Safe diagnostic output | Closed primitive records, fixed categories/reasons and bounded arrays; no new source text, token, private owner/generation, arbitrary exception message or object serialization in observer payloads. Actual adapter sentinel exclusion and immutable JSON readback retained. | CLEAR |
| Substitutes and exact integration | Controls use actual adapter, real unshown WPF references/parents, real await/finally, real Receipt serialization/filesystem and saved JSON. The source guard checks actual callsites but is not claimed as live UIA proof. Prior mutations remain evidence for unchanged paths. | CLEAR preparation; native fidelity still owed |

The run-scoped receipt and bounded IDs provide diagnostic correlation only. No identifier
is promoted to product authority, and none of these rows proves a managed WPF object is
the UIA provider node. That remains an observation question for the later controlled run.
The existing helper's own historical sink/error behavior is not newly certified as no-throw.

## Independent normal build and actual JSON

Command:

`dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --filter "FullyQualifiedName~NativeObserver_NonGui_" --logger "trx;LogFileName=view-state-review.trx" --results-directory .artifacts/native-view-review -v q`

Actual **21 executed, 21 passed, 0 failed, 0 not-executed**. Every name/outcome was read;
the exact 21-name set equals the independently reviewed sink-correction inventory. Original
17 controls plus three shared-formatter outcomes and immutable publication remain; the
view correction strengthens the existing adapter control instead of inflating the count.
Normal run console reports 504ms test duration; this is not build duration or native latency.

Own actual adapter receipt:
`.artifacts/atlas-observer-controls/53320ead5acb404e82c1ae64569f01af/receipt.json`.
Before/child-replaced/after packets retain view 3, control IDs 8/9/10, document ID 11;
IsLoaded=false, IsVisible=false, Attachment=detached, TestRetained=true and
BackingLeaseRelation=not-observed. Two host rows remain. These are observed unshown/detached
false values, not a loaded/visible-true test. Packet visited counts are 4/4/3, queued 0,
Truncated=false. Their measured intervals are 3,558/330/153 Stopwatch ticks; observed
frequency 10,000,000 ticks/second. These tiny fixtures establish instrumentation output,
not a performance bound for the eventual native tree.

Actual wrong-thread and injected sample-failure receipts retain null visited/queued values;
no successful census or zero work was invented. Packet-cap truncation is also present in
the run-level status even though the earlier accepted packet predates the later cap hit.
Unavailable/truncation and nullable fields must be interpreted together, not as absence proof.

Author red TRX was opened: one adapter case failed with KeyNotFoundException before the
fields existed. Author final TRX was opened: 21/21/0. Its actual receipt was separately
opened and matches the same retained view false values. This independent run supplies a
distinct receipt and test result; author claims alone did not clear the floor.

| Evidence | SHA256 |
|---|---|
| Own view-state-review.trx | b98f6abd9f539e12c63ae943993292a35da5f7447853e48168d8273a0c79465a |
| Own actual adapter receipt above | 882aaa61cd81289f4450aeb29bfb8a01a2365fba56a06dccda77b97efd3ec0c7 |
| .artifacts/native-view-review/observed.json | d0d5fd6c2348f36bbd9c78df8dd6581970d23fdb0b75dad2121af11cefb29afa |
| Author view-state-red.trx | c4be8c0150a9b7962f0486040dec843c8bc4464a1378d57a6c3433f9bf1fce57 |
| Author view-state-final.trx | db799ebc38fa0cf866419c8f90828fea8243cf0d6be73e4dba8b3ef2ea0741c9 |
| Author actual adapter receipt | a4fae674169ed35dc3cef20ece51ec78fbca4f5cbe789a121c7b30d0668347ef |

The raw observed.json contains actual names, messages, Times, counts, packet/status values,
receipt paths and hashes. Source and old evidence are preserved. No fresh execution of old
mutants was needed: exact code preservation plus the unchanged controls/current green and
the newly observed red/green were the relevant evidence for this final bounded delta.

## Independent lenses and residual limits

**Test Architect: CLEAR complete admitted preparation.** Per-view floor is closed;
FR-NO-001 remains cleared. **SRE: CLEAR preparation**, with measured/unavailable data and
explicit soft-budget/durability limits. **Architecture/Security: CLEAR** for Owner A's
existing public/test-reference boundary. **Simplifier: CLEAR**: test-local bounded lists,
packet builder and one BCL serialization boundary; no tracing framework or new product API.
No remaining preparation hard veto was identified; no author or Owner self-clearance is used.

Live UIA, shown/loaded-true behavior, provider failure/races, full census under a real window,
native cost, P1-02 causation, whole-App/full-gate/Release qualification and publication are
not established. The original native failure and raw cleanup evidence remain. The next
action is Conductor/Owner scheduling under a fresh checked native grant, not a rerun loop.

Actual cost: seven of eight tool boundaries; checkpoint sent after five; no delegates,
no source edit, no shown window, HWND creation or live UIA. Token usage is not exposed;
closing audit records measured wall time. Exact proof lease held only for atomic UTF-8
receipt/audit/commit and released in finally with PRIMARY liveness readback. Only this proof
and own official audit are committed; raw/generated dirt remains, parent owns derived join.
AIDE_CONTRACT_LOG is absent, so no watcher episode-delivery destination is invented.
