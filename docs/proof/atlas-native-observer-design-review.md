---
id: proof-atlas-native-observer-design-review
title: "Independent review of the Atlas native observer design"
type: doc
status: accepted
owner: "@timianmalloo"
links:
  - { to: investigation-atlas-p1-02-native-uia, rel: depends-on }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Historical receipt: Independent review of the Atlas native observer design"
---

# CLEAR: bounded native observer design

**Verified by source/document reading, not execution.** The final appendix at
`e41a176cf6bb38a6ff144d857808c106bca180de` is coherent with foreground preparation grant
`req-01M2M4FMQW8QWMJX0GFWF54A5X` and Owner A. No blocking design discrepancy was found.
This clears design review only. Parent retains design freeze and implementation dispatch;
the grant supplies no current GUI slot or product repair authority.

## Scope and exact inputs

Goal: check the observer's actual seams, identity/timing claims, smallest sufficient
mechanisms and preservation of the original outcome. Done when independently reviewed and
committed with a verdict. Tier T2, fan-out 0, six tool boundaries/ten-minute ceiling.
No test/source implementation, build, test run, GUI operation, join or push occurred.
Only this receipt and own official audit are committed. Product modeling/rendered proof
is N/A to this document review; no claim about current UIA behavior is established.

Source/proof pins, checked again at closure:

| Input | Observed identity |
|---|---|
| docs/proof/atlas-p1-02-native-uia.md | Git blob c8071c15d1b654eb54238804619768bd0b289465 |
| tests/AiDe.App.Tests/Workbench/Understanding/AtlasDaemonMainWindowProofTests.cs | byte SHA256 53b792e4775f76279f199ccccee485d9143cb044abfbc3ffdc4f6d34573e2613 |
| src/AiDe.App/Workbench/Understanding/AtlasLoadingHost.cs | Git blob 8f7c6b072f17da7fd94f02bb9b47d78bc0ba6a2b |
| src/AiDe.App/Workbench/Understanding/AtlasReaderView.cs | Git blob 2b490a4bc7c1a14396dfc177ba40c632030925e7 |

Read the foreground request-add and resolved grant in primary `.agents/requests.jsonl`
(lines 408, 413), the complete appendix, actual source portions below, root AGENTS,
forensicreview workflow, Testing Strategy and Solution-Selection Ladder. `rg --files -g
AGENTS.md` found only the root instructions. The optimize-graph pass retained three nodes:
ground actual ports and authority -> independent falsifier/necessity review -> receipt,
audit and release. No loop/fan-out; no observation floor was optimized away. Surface list:
held test references -> STA WPF sample -> MTA original query/post-root observation ->
STA continuation -> bounded receipt -> future adapter controls/native fidelity.

## Source-to-design checks

Paths below use `native` for the exact test path above and `appendix` for the pinned proof.

| Design clause | Actual source and review disposition |
|---|---|
| Existing reference assignments, appendix 127-140/178 | native 162-169, 204-209 and 305-310 provide actual first/replacement wrapper, asserted host, view and lease references. Registering them does not authorize replacing Assert.Single or acceptance selection. CLEAR. |
| STA sample around existing await, appendix 179-182 | native 432-460 installs DispatcherSynchronizationContext and owns the dedicated STA/30-second timeout; existing awaits are 267 and 279. Before/finally-after samples are separately timed, not simultaneous, and require no new queue/invoke/await. CLEAR design seam. |
| Capture bracketing, appendix 178 | native 754-769 constructs one bitmap, renders at 760, copies/asserts/saves once. No-throw samples immediately around that Render preserve its original work and assertions; they are not a pixel-atomic observation. CLEAR. |
| Original query, appendix 179-180 | native 466-492 runs MTA, obtains FromHandle once, checks process, iterates five fixed names, calls FindFirst once per name with Descendants/NameProperty and retains NotNull/IsOffscreen. Correlation is additional data, never another query/result. CLEAR. |
| Later Root RuntimeId, appendix 167/180 | native 488 invokes the existing after-original helper; 502-506 constructs the root diagnostic and 539 calls GetRuntimeId. Reuse that returned array, apply the proposed cap/status without another provider read, and correlate QueryId. No pre-query identity read. CLEAR. |
| Public/test ownership, appendix 143/Owner settlement | host 15 is private generation; 28-29 expose StatusText/ReaderView; 48-50 assign ReaderView and Content; 79/101 clear ReaderView and install status. View 22-23 privately retain lease/owner; 65-80 expose the listed controls/counts. Host->Content and Host->ReaderView are direct; view->lease is not. CLEAR. |
| Wrapper relationships, appendix 172-173 | native 986-1001 exposes inner in its own wrapper and assigns returned ObservedLease at 993; 1005-1037 exposes lease state and release events. Release-start is currently an event at 1034, not an existing ReleaseStarted Boolean property: any new diagnostic transition must name its emitting event, not claim a field read. This is accommodated by the observation-only transition design. |
| New failure containment, appendix 184 | native Receipt.Mark 1108-1114 appends then calls Save; Save 1125-1135 performs synchronous serialization/file I/O and can throw. Therefore the proposed boundary must include formatting and Mark/Save, not just property getters. Existing query/assertion failures retain their object/outcome; new observer failures must not increment FailureCount. CLEAR requirement, unimplemented. |

The existing diagnostic helper at native 510-518 catches selected UIA exceptions and its
own fallback receipt write can throw. That existing behavior is not newly certified as
no-throw. The design's containment obligation applies to added work; it must not silently
refactor old behavior while promising original preservation. Actual delta review remains owed.

## Smallest sufficient mechanisms and control necessity

| Mechanism | Necessary distinction / limit |
|---|---|
| One run-local bounded ReferenceEquals registry (appendix 149) | Repeated and detached/replaced references need stable distinct IDs; equal names or overridden equality cannot establish identity. A short retained list is sufficient. No global registry, service or tracing framework is required. Strong retention is explicitly observer-caused. |
| Bounded owned-Window child walk (151) | Foreground requires accounting for multiple hosts. Existing Visuals at native 793-800 is recursively unbounded. A local bounded walk earns its place; it must preserve all observed hosts rather than choose the first matching label. |
| Small held-reference set and bounded parent walk (153) | A replaced view can remain held while absent from the current walk. Explicit references prevent loss; a completed parent chain distinguishes connection from absence in a truncated census. No generalized graph store or logical-tree fallback is needed. |
| Immutable primitive packets, counters, separate intervals (157-174) | MTA query and STA samples cannot be one simultaneous snapshot. Stable correlation and statuses prevent zero/defaults or role labels from fabricating relationships. A local packet builder suffices; deterministic clock inputs are for discriminating tests, not a clock service. |
| Work, output and retention limits (151) | Bound diagnostic perturbation and growth. The proposed 512-node/48-depth/512-queue, 16-host/32-view/32-reader-lease, 256-reference, 16-packet and 25ms between-operation ceilings are design limits, not measured capacity. Saturation may lose IDs/evidence and must remain unavailable/truncated. |
| Safe sink and scalar allowlist (169-184/197) | Structural state may be correlated without dumping source, tokens, labels or exception messages. Source-document ID and scalar counts are contextual; they cannot substitute for host/view edges or prove correct source/UIA membership. No item enumeration is justified. |
| Non-GUI discriminators (192-198) | Collision/saturation, two-host/replacement, partial census/getter failure, same-exception/exactly-once result, timing/correlation and payload/sink controls each target a named misleading-success shape. One bounded helper/adapter can support these; no generic graph/testing framework is required. |

**Simplifier: CLEAR within this local envelope.** `yagni:` do not promote synthetic graph
inputs, counter injection or fault delegates into a reusable observer framework. This is
an implementation ceiling already consistent with appendix 208, not a new design rewrite.
No source line-deletion estimate exists before implementation. The observation floors
cannot be removed merely because the proposed controls are numerous.

## Adversarial verdicts and remaining proof

**Test Architect — CLEAR (design).** The design explicitly preserves the actual selector,
result, assertions, ordering, waits and timeout and requires D7 actual-adapter pairing.
Pure immutable input rows alone cannot prove WPF reference extraction; future controls must
exercise the real observer helper's produced rows, and the exact native delta must be read.
Likewise a delegate-only exception test cannot prove the real await/finally and sink path.
Appendix 198/200 correctly retains both obligations and later native fidelity. None ran here.

**SRE — CLEAR (design).** Timings and unavailable quantities have explicit sources. A 25ms
between-operation budget cannot preempt one property/provider call or synchronous receipt I/O;
the appendix acknowledges that. Before/after packets bracket the batch, not each query's
simultaneous WPF state. Missing continuation/save means absent evidence, not successful capture.
No performance, race frequency or no-perturbation claim follows from this review.

**Architecture/Security — CLEAR (design).** Owner A is respected: no private generation or
view/lease accessor/reflection; wrapper associations remain distinct from direct host/content
edges. The same HWND/process root is preserved, RuntimeId remains post-query, and no broader
window/peer search or authority token is introduced. Finite structural status has no product
ownership or navigation authority. Source correctness and stale-provider causation remain open.

No BLOCK finding or repair request is raised. Minor implementation clarification: release-start
must come from its actual event/transition as described above. No existing proof/source was edited.
Two source batches exceeded output limits; narrow member/line reads recovered the omitted seams
before conclusion. Raw preparation/pins and generated audit output are retained; no hidden output
is promoted to an observed result. AIDE_SESSION/AIDE_CONTRACT_LOG are absent, so no watcher
episode-delivery claim is made. The official audit names this receipt.

Planned and actual: six tool boundaries, no delegates, checkpoint after four; measured duration
is in the closing audit. Exact receipt lease is short and released in finally with liveness
readback. Remaining: parent design freeze/dispatch, implementation and independent non-GUI/delta
review, then a fresh authorized native slot. Existing P1-02 failure and qualification veto remain.
