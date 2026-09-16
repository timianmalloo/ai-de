---
id: proof-atlas-native-instance-observer-review
title: "Independent Atlas observer implementation review: shared receipt failure escapes containment"
type: doc
status: proposed
owner: "@timianmalloo"
links:
  - { to: investigation-atlas-p1-02-native-uia, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Historical receipt: Independent Atlas observer implementation review: shared receipt failure escapes containment"
---

# BLOCK: observer formatting failure poisons later original receipt writes

**Verified counterexample.** Frozen source `781acb2b4214d1a481cf343cf1413bfbc72461ed`
does not satisfy the design's failure-containment requirement. The independent normal-build
17-case non-GUI filter passes, but a separate same-receipt discriminator proves that a caught
observer formatter failure can make the next original receipt operation throw.

Native source: `tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs`,
byte SHA256 `5565b10a9dffdd3f30dfa02ccb5bd41080e6694370fb4754844a78727f9659e5`.
The author proof has Git blob `abc980e75fe6a0ead21fb35dd3d53a53dfb4b05d`.
Both remain unchanged. Independent design CLEAR was
`973afc97a8bcd981e607aa73be0c89247045a3c5`; it did not clear this implementation.

## FR-NO-001 — P1 / Test Architect and SRE BLOCK

**Issue, Verified.** Native `InstanceObserver.Flush` lines 494-513 calls the existing
`Receipt.Mark` and swallows formatting/sink exceptions. `Receipt.Mark` lines 1636-1641
adds the WpfObservation to `_events` before `Save`. `Save` lines 1659-1669 serializes the
entire event list. A WpfObservation whose formatter throws remains in that list after
Flush returns. Subsequent original `Mark`/`Save` calls serialize it again and throw.

The permanent `NativeObserver_NonGui_RealAwaitFinallyAndSinkPreserveOriginal` control
creates a separate `observerReceipt` for sink/format faults, while passing the original
`receipt` to `ObserveWithInstancesAsync`. The native journey uses the same receipt for
both. The test checks immediate await/finally failure preservation, but it never performs
a subsequent original write through the poisoned shared sink. This is a fidelity gap,
not evidence that all formatter failures are contained.

Violated contract: appendix Ordering and original-result preservation, especially its
no-throw formatting/sink boundary and requirement that added diagnostics cannot alter
the original outcome; Testing Strategy D7 requires the substitute to preserve the actual
shared-resource boundary. Containment must include persistent diagnostic state, not just
the stack frame which caught the exception.

### Deterministic reproduction and observed output

Command, after the independent test build completed:

`dotnet run --project .artifacts/native-instance-review/SinkProbe.csproj -v q`

The scratch console references the built test project and invokes the actual private
Receipt, existing ThrowingPacketConverter, InstanceObserver.Sample and Flush methods
through reflection. It changes no private fields or source. The existing sampler's
unavailable-input path produces a real WpfObservation from a null Window; no HWND,
shown window, dispatcher, automation provider or live UIA call is used. The failure is
specific to serialization of that packet type and does not depend on visual contents.

1. Construct one real Receipt with the author's admitted throwing packet converter.
2. `Mark("original.before", ...)` succeeds and persists.
3. Real Sample/Flush returns; `SinkUnavailable == true`.
4. `Mark("original.after", ...)` through that same Receipt throws
   `InvalidOperationException: DO-NOT-EMIT-FORMAT`.
5. The on-disk receipt still contains only `original.before`, with FailureCount 0.

Actual marker: `CONFIRMED_SHARED_RECEIPT_POISON_AFTER_CONTAINED_OBSERVER_FORMAT_FAILURE`.
Probe exit 0 means its counterexample assertions succeeded; it is not an observer pass.
The first scratch build failed because nullable annotations lacked an enabled context.
Adding `#nullable enable` only in scratch fixed that setup error; both logs are retained.

This is a controlled formatter-failure seam, not a claim that the null native converter
currently throws, not a measured production failure frequency, and not a diagnosis of
P1-02. The implementation explicitly claims this class of containment and uses the same
converter to test it. The demonstrated difference is shared receipt state.

### Bounded clear predicate

Keep newly failing observer payloads from contaminating subsequent original receipt
operations. A bounded local approach may validate/serialize observer rows before making
them part of shared durable state, or atomically remove only the newly failed observer
entry under the existing lock. The author/Owner must choose the exact correction; this
review edits no source and does not grant a general Receipt rewrite.

Add a permanent discriminator using the **same actual Receipt** for original and observer
work. With an observer-only formatter failure, original writes before and after the failed
observation must succeed; the original success/null-assertion/sentinel exception remains
unchanged through the real await/finally path; new diagnostic data/status must accurately
distinguish discarded, buffered and persisted observations. A mutation that leaves the
failed row in shared state must fail. Retain the current 17 controls, real sink failure
controls, exact query/timeout checks, no-GUI boundary and independent re-review.

Do not fix this by swallowing original receipt failures, dropping original events,
weakening original assertions, switching the control to a separate receipt again, or
adding an alternate UIA query/retry. Native qualification remains separately blocked.

## Independent execution and retained author evidence

Own normal-build command:

`dotnet test tests/AiDe.App.Tests/AiDe.App.Tests.csproj --filter "FullyQualifiedName~NativeObserver_NonGui_" --logger "trx;LogFileName=review-observer.trx" --results-directory .artifacts/native-instance-review -v q`

Actual TRX: **17 executed, 17 passed, 0 failed, 0 not-executed**. All 17 names/outcomes
were parsed and read, not inferred from exit 0. Inventory: reference identity 1; actual
host/content/retained-view adapter 1; bounds nodes/depth/references/packets 4; wrong-thread
unavailable 1; real-await variants original/sample/sink/null/success/format 6; actual release
event 1; source contract 1; null-content 1; bounded post-query RuntimeId 1.

| Raw evidence | SHA256 |
|---|---|
| .artifacts/native-instance-review/review-observer.trx | 5e239d26a78ac5d5f2b6bcff080fac243fda056829c90bf1e62c7b8938e806cd |
| .artifacts/native-instance-review/shared-sink-probe-final.log | 677281442245e7cbdbf6b3d66bdb6f85e1bf0e53c73a4e70346c284e34974069 |
| .artifacts/native-instance-review/SinkProbe.cs | bf434e91ae02ef62242a4a7882b607c12537efffe2b89d7b23f3c285e2f3d64c |
| .artifacts/native-instance-review/observed-results.json | 08f6f2da0175f0b2ac50bc20128a1c872db03dd05652706db5d258aeb8e72a84 |

`observed-results.json` also records independently parsed actual author TRXs, timestamps,
all outcomes and full failure messages. Observed initial red 11/0/11 precedes first green
11/11/0; additional red 14/11/3, null red 7/6/1, complete 16/16/0, final 17/17/0.
The final author TRX hash is
`e36418be7c1c79f72a9a66969324db33062a219937274933ef677ac8515a8bed`.
Equality and current-child mutants each fail 1/1, finally mutant 6/6, sink mutant 2/6;
the actual messages match the named discrimination. Those are post-implementation
mutations, not red-first chronology. Earlier source hashes remain explicitly reconstructed
from scripts; this review does not promote them to contemporaneous captures.

## Scope, observed positives and remaining limits

Goal: independent implementation clearance or precise veto with actual evidence. Tier T2,
fan-out 0, ten-call/twenty-minute ceiling. Graph: source/authority grounding -> normal-build
non-GUI run plus read-only diff review -> discriminating shared-sink check -> receipt/audit.
The decisive finding ends further exploration; this BLOCK is not an exhaustive clean bill.
Actual seven tool boundaries; measured duration is recorded by closing audit. Checkpoint
finding was escalated after boundary four, before any source repair. No cap was exhausted.

Observed by source reading: original single FromHandle/process assertion, five-name order,
single FindFirst Descendants/Name call per name, original returned-element assertions,
and existing waits/30-second dispatcher timeout remain. RuntimeId is bounded after the
original query through the same helper, without a new pre-query provider read. The actual
two caller sites use one await/finally wrapper; Capture retains one Render. Wrapper release
identity comes from the existing event. The observer labels private generation and
view-to-backing-lease not-observed and uses direct Content/ReaderView references separately.
These positives do not prove live UIA or complete fidelity of all fields/ceilings.

**Test Architect: BLOCK** on the demonstrated shared-sink fidelity/containment gap.
**SRE: BLOCK** because a swallowed diagnostic formatter failure changes later original
work and makes persistence claims incomplete. No timing/race extrapolation is made.
**Architecture/Security:** no new private authority or broad UI root was found in the
examined implementation; overall implementation clearance is withheld with the veto.
**Simplifier:** correction must remain local to observer append/serialization containment
and its real shared-sink control; no graph, tracing or persistence framework expansion.

No whole-class/full-App/shown/live-UIA test ran. The normal prefix does execute unshown
WPF objects on STA as explicitly designed; that is not native shown-window proof. All
source/author proof bytes remain frozen. Only this proof and own official audit are
committed; exact lease is released in finally, liveness records the observed result, and
raw/generated dirt remains. Parent owns any central recurrence record and the next exact
correction decision. Class -> sweep -> derive -> prevent: caught diagnostic failure can
leave shared state that fails later original work; identified Mark-before-Save/Flush and
split-sink control; derive transactional observer admission; permanent shared-sink mutation
control is required before clearance. No new product-cause class is invented.
