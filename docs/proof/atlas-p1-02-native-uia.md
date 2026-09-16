---
id: investigation-atlas-p1-02-native-uia
title: "Atlas P1-02 native UIA failure: preserved observation and bounded next diagnostic"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [atlas, investigation, native, uia]
links:
  - { to: proof-atlas-five-gates, rel: depends-on }
review-by: 2026-12-15
summary: "The original Atlas files UIA query refused on the proof-owned HWND; current and historical evidence do not establish a necessary-and-sufficient cause."
---

# Root-cause overview

**Flagged: root cause is unresolved.** The exact failed query is now Verified:
AutomationElement.FromHandle(45680476), expected process7952, TreeScope.Descendants,
NameProperty equal to Atlas files. OriginalFound=false at03:27:53.4013825Z, original
query40.8494ms; Assert.NotNull atline489, calledline267. Original root process assertion
passed. Subsequent owned root is Normal, visible/enabled and titled
display-first-not-pipe-authority - Architecture - AI-DE. No alternate query was used
to clear the failed original. This failure precedes the later Back/replacement journey.

Bounded subsequent RawView census:100nodes, max128nodes/depth12, elapsed121.2929ms,
QueuedNotVisited0, Truncated=true. The census contains Code Atlas atordinals40/55 and
Loading Code Atlas. atordinal57; it does not establish a complete census because depth
truncation is explicit. The retained screenshot visibly contains the loaded source and
selected Answer method. A screenshot does not identify UIA nodes or establish timing
causation. The mismatch is a diagnostic lead, not proof of stale provider state.

Primary ui.primary and test.primary carry the same NotNull failure. Two subsequent
daemon-forced-cleanup events carry InvalidOperationException; they are separate effects,
not silently reclassified as the root cause. Both daemons were reaped with Forced=true;
dispatcher drained, first reader disposed, fixture deletion observed. Receipt
Completed=false/FailureCount4. P1-02 never produced a complete suite TRX before its
owned stop, so no full App/Core totals or pass result is asserted.

## Execution and evidence pins

Candidate e6aed0857749a1409e5a4d3c704ed131aeea721c; basebcf4959bc0e0e361736e6a179f05b69fcd0500f8.
Foreground grant req-01M2M3QKXAHRTB643718QYH74C and its paired watcher request authorize
ONE SLOT-CODEX-P1-02 run. Actual runner path/hash was supplied before spawn.
Same-tree Debug daemon preflight passed, input status clean before/after, three binaries
hashed. Canonical PID7888 started03:25:44.148310Z, ended03:29:44.662781Z, exit1.
Conductor observed the native receipt and verified PID command plus creation time before
taskkill/T/F. The wrapper saw canonical failure, released all four leases and emitted
END/RELEASE. Post-stop query found no matching integration-tree dotnet/testhost/AiDe
commands. This observed census is not an OS lock or proof about unrelated processes.
No runtime contention or current product regression is established. All failures remain.

Byte-verified snapshot: artifacts/atlas-five-gates/failed-slot-p1-02/manifest.json,
8files/216379bytes,
manifestSHA256d654c2110f3a42faf3988a58c5d8999536031d2aaa62948dc43358da6c74e015.
Native source SHA25653b792e4775f76279f199ccccee485d9143cb044abfbc3ffdc4f6d34573e2613. Native-source diff from prior diagnostic checkpoint
5f651aaa557ca0cf4e627f7e6def432cc69df32a: empty (same committed file).
No source or runner was changed in this investigation.

## Prior evidence and disconfirmation

Opened docs/proof/code-atlas-production-adapters.md IQV16/UWQ20 sections. IQV16 records
the same NotNull shape and four primary/cleanup events, before diagnostics identified the
name. A later instrumented1/1 and UWQ full1134/1134 App run passed, while explicitly
leaving the intermittent cause unknown. These are prior recorded outcomes, not a fresh
re-execution; their product/main basese861/c46e differ from currentbcf. Current preflight
removes the earlier missing-run-label/missing-daemon setup failure, but it does not prove
all other execution preconditions equivalent. Current missing name is established by the
new original-result event; neither stale cached peer, reparenting, focus nor contention
is necessary-and-sufficient on this evidence. No framework behavior is asserted from memory.

# Specific fixes

None proposed for implementation: no cause is established as necessary and sufficient.
Do not add a retry, alternate query, sleep, skip, or weaken the original NotNull/visibility
assertion. The smallest next action is a foreground-owned bounded diagnostic decision
around the original UIA lookup and real rendered view/loading-placeholder identity.
If new emitted evidence is needed, it requires an exact test-file diagnostic grant,
independent review and a fresh shown-run slot. Existing raw evidence is the starting point.

# Generalization and review lenses

Class/sweep/derive/prevent: the existing native diagnostic control detects and preserves
a missing original query without converting it to acceptance. The same four-event shape
recurs in prior IQV16 and current P1-02; UWQ's control is a diagnostic-preservation control,
not a cause-removal control. No new defect-class identity is allocated for an unverified
cause. The displayed loaded surface and provider query are distinct observation surfaces;
the permanent original-query/refusal and blank-window diagnostic-control tests remain
unchanged and fail closed. Any causal repair must add its own discriminating control.

SRE peer: observed original query40.8494ms, bounded census121.2929ms, known cleanup; no
latency/frequency extrapolation. Distributed Systems peer: rendered and provider state
are separate observations; the current record cannot establish their transition ordering.
Test Architect adversary: qualification BLOCK until the original native journey and
required suite/gate union pass under a reviewed, authorized correction; no self-clearance.
Security/architecture adversary: preserve own HWND/process binding, original selector and
scope; do not search desktop or another window to manufacture the expected name.
Data & Persistence: N/A to this read-only UIA investigation; no schema/data change.

| Phase | Bounded work | Exit evidence | State |
| --- | --- | --- | --- |
| 1 | Inspect retained original failure, root, census, screenshot and prior records | Missing Atlas files query pinned; cause explicitly unresolved | Complete |
| 2 | Foreground/Owner decide exact additional diagnostic seam | Named missing observation and scoped protocol; independent review | Requested, no implementation |
| 3 | One freshly scheduled discriminating native observation, if admitted | Original query result plus identity/transition evidence; all raw retained | Not authorized by P1-02 |
| 4 | Causal repair, review, canonical qualification and publication | Red/green cause-specific control plus full contract | Not yet designed/admitted |

Owner bounded this investigation to4calls/5minutes, no rerun/source/UI interaction.
Actual four calls include capture, prior comparison, artifact preparation and official
close. Artifact preparation found one guessed view path absent; no write occurred there;
rg file discovery was used to recover the actual path. No implementation proceeded.
Episode proof: docs/proof/atlas-p1-02-native-uia.md. AIDE_CONTRACT_LOG is absent;
no episode destination was invented.


# Observer-design appendix: same-instance WPF state and original UIA result

## Status, authority and end state

**Owner A scope settled; independent Test/SRE review and implementation dispatch pending.** This is design only. It neither changes the native test nor attributes P1-02 to stale automation state. The foreground grant `req-01M2M4FMQW8QWMJX0GFWF54A5X` permits later preparation in the existing native test file; it is not a shown-run grant. The current author tree is `C:/Projects/ai-de-test-atlas-native-instance-observer`, branch `test/atlas-native-instance-observer`, base `0f8111cb684ff2394432353ab9744fa21a3b2883`; session `codex-atlas-native-instance-observer`, author `codex-astra-native-observer`.

Goal: distinguish the actual WPF host/content/view/reader instances seen at existing boundaries from the original owned-HWND UIA result. Done when this bounded schema, exact insertion plan, failure semantics and non-GUI discriminators are available for Owner decision. Not in scope: source/test implementation, GUI execution, new waits/retries/roots, causal fixes, producer/model changes or self-clearance. Tier T2; one author; eight-call/15-minute ceiling; no delegates. Existing product specification/architecture remain authoritative; no new UI design, domain entity or storage schema is introduced.

The design-slice and optimize-graph workflows are applied at this scope. Graph: grounded failure and source contracts -> finite observer design -> adversarial control plan -> proof/own audit -> Owner settlement -> independently reviewed implementation/non-GUI controls -> fresh scheduled native slot. Surface list: capture -> original query -> WPF snapshot -> receipt -> non-GUI controls -> independent Test/SRE -> fresh slot. The final three nodes are future gates, not completed work. Parent owns the normative decision/change record and derived knowledge/audit views; this proposal does not create a competing central ruling.

## Verified grounding and unresolved relationship

Opened native test SHA256 `53b792e4775f76279f199ccccee485d9143cb044abfbc3ffdc4f6d34573e2613` is unchanged. Relevant exact members/locations in this pin:

| Source | Grounded contract / proposed insertion |
|---|---|
| native test `RunDispatcherAsync`, lines432-458 | Dedicated STA dispatcher and existing30-second outer WaitAsync; retain both. Observe only from its existing owned-window callbacks/continuations. |
| native journey lines160-170,202-206,209,304-307 | Existing first/replacement ObservedReader factory variables, asserted host/view and lease references; register these exact references when already obtained. Do not replace Assert.Single or obtain another host for acceptance. |
| native journey lines262/267 and279/280 | Existing capture and original UIA batch boundaries. Collect read-only WPF packets here; before the existing await and in its finally for the post packet. No dispatcher Invoke/InvokeAsync, IdleAsync or extra await is introduced. |
| native journey line314 | Replacement capture boundary; retain the first journey references so replacement and still-retained old objects can be distinguished. |
| `Capture`, line754 onward | Existing render/copy/save path. Place structural observations immediately before and after the existing bitmap.Render call and tag the existing capture name. These are separate observations, not a simultaneous pixel snapshot. Keep render, assertions and PNG behavior unchanged. |
| `ObserveAutomationAsync`, lines466-492 | Existing Task.Run MTA assertion, FromHandle, process assertion, five names, FindFirst Descendants/NameProperty, returned element and NotNull/IsOffscreen assertions remain decisive. Add only correlation/timestamps around the actual existing query. |
| `ObserveOwnedAutomationAfterOriginal`, lines495-521; `AutomationDiagnosticNode`, lines524-548 | Already reads the original root RuntimeId after the original result. Reuse this observation, tagged with query ID; no new pre-query provider read. The existing bounded RawView diagnostic stays diagnostic. |
| `ObservedReader`, lines986-1003; `ObservedLease`, lines1005-1046 | Existing wrapper/inner references, Lease, Disposed, release flags and terminal/cancellation state support test-local identity observations. Later same-file instrumentation may register wrapper+inner at construction/admission; no token fields are logged. |
| `Receipt`, line1110 region onward (member identity authoritative) | Append-only event list under its existing lock and file serialization. Add bounded structural packets; no arbitrary object serialization. New diagnostic sink failures must not replace original query outcomes. |
| `AtlasLoadingHost` | ContentControl.Content is the actual current child; internal ReaderView and StatusText are available. Private `_generation` is not an observation port. Deactivate clears ReaderView and installs status Content. |
| `AtlasReaderView`, lines65-80 | Public collection counts, read-only flag, CanGoBack/CanLoadMore and explicit controls are available. Private `_lease` and `_owner` are not exposed. |

The retained raw receipt was directly read: original `Atlas files` result false at `2026-09-16T03:27:53.4013825Z`, own HWND45680476/process7952,40.8494ms; the later owned-root event at03:27:53.4067015Z reports RuntimeId `[42,45680476]`. This does not pair the UIA node with a particular managed WPF object. That distinction is the missing observation, not an established causal explanation.

**Owner A settled observation-only scope:** use observation-only identities/transitions instead of reading private host generation. Reader wrapper -> actual inner reader and wrapper -> returned lease can be observed in the existing test wrapper. Host -> current Content and Host -> ReaderView are direct reference relationships. **ReaderView -> private lease/owner remains not-observed.** Journey role association with `firstReader/firstLease/view` must be labeled `test-associated`, never direct ownership established by reference. If exact private view-to-lease linkage is required, this public/internal-only design cannot establish it; Owner must settle a different explicit seam before implementation. No reflection or product accessor is proposed.

## Identity, scope and structural schema

Bounded context: one owned native proof run. Entity identities are run-local observation IDs, not product IDs. Grain: one immutable packet is exactly one observed query or one separately timed WPF boundary sample; object/edge rows belong to that packet. Observations append, never overwrite older instance relationships. Counts are non-additive across snapshots; per-call durations are additive only for disjoint measured intervals and must not be summed across overlapping capture/query spans. IDs and observations are facts; replacement/detachment interpretations are derived from explicit edges and completeness flags.

Use a test-local bounded reference registry: at most256 objects, monotonic positive IDs, lookup by ReferenceEquals over the retained entries. No runtime hash is identity; labels, CLR type and overridden Equals/GetHashCode cannot merge objects. IDs are never recycled in the run, even after an object is detached. Registry scope/lifetime is the owned proof body and is cleared in cleanup; it holds bounded strong references, so `retained-by-observer` is explicit and cannot be treated as producer retention or proof of disposal/GC. Registry saturation gives unavailable IDs and a truncation reason; it cannot alias a new object to an existing ID. Pure registry access is synchronized if wrapper callbacks arrive off STA; WPF reads never move off their owning dispatcher.

Enumerate from the exact owned Window only, using a new bounded iterative version of the existing VisualTreeHelper child walk. Do not reuse the unbounded recursive Visuals<T> as the observer's completeness claim. Proposed ceilings:512 visited nodes, depth48,512 queued entries,16 host rows,32 view rows,32 reader/lease rows,256 registry entries,16 WPF packets, and25ms between observation operations per packet. These are design ceilings, not measured performance. One property/visual operation and receipt I/O cannot be preempted by this between-operation budget; no hard latency guarantee is asserted. Existing30-second timeout is unchanged. Exceeding a ceiling records observed counts, remaining queued count where known, Truncated and reasons; no full census/absence claim follows.

Also inspect the small explicit set of already-held journey references (`host`, `view`, `firstReader`, `firstLease`, next equivalents when obtained), even when absent from the current census. A bounded parent chain may establish connection to the same Window or termination before it; an incomplete walk yields unknown. `not-seen-in-truncated-census` is not detached. Distinguish `current-content`, `host-reader-property`, `window-visual-member`, `test-retained`, `observer-retained`, and `not-connected-to-owned-window` edges. Multiple hosts remain multiple rows; never pick a label-matching first host. Observation sequence/change ordinal is not producer `_generation`.

### Fields: exact emitting source, thread, boundary, relation and failure

All missing values carry a status (`recorded`, `null-observed`, `unavailable`, `truncated`, or `not-observed`) rather than a plausible zero/default. IDs are null when unavailable; collection counts are zero only when read as zero. Unknown text is never substituted for a status enum.

| Fields | Emitting source / thread / timing | Relation and failure semantics |
|---|---|---|
| RunId, BatchId, QueryId, PacketId, sequence | Test-local monotonic counters, bounded registry; allocated on owning STA except query sequence on existing MTA worker | Opaque run-local diagnostic IDs only; no scope/binding/declaration token. Sequence overflow/cap marks unavailable and ends extra observation. |
| UTC start/end, monotonic start/end, elapsed, thread ID, apartment, boundary | DateTimeOffset.UtcNow/Stopwatch/GetApartmentState at each actual read interval; STA for WPF, MTA for UIA | UTC samples may not imply total ordering; monotonic samples share this process. Separate WPF and query intervals remain separate. No interpolated/simultaneous timestamp. |
| OwnHwnd, expected process, root-binding status | Existing caller handle, Environment.ProcessId and original FromHandle/process assertion | Do not reacquire an alternate root. Original assertion failure remains original failure; missing root diagnostics are unavailable. |
| ExpectedName, TreeScope, property, original start/end/result | Existing names array and unchanged FindFirst invocation, MTA; capture start immediately before and end immediately after its single call | Preserve the returned element variable and assertions. A diagnostic result cannot become the original result. Query exception is recorded only best-effort and rethrown unchanged; it is not reported as false. |
| RootRuntimeId, root observation timestamp/status, QueryId | Existing post-original AutomationDiagnosticNode on the same AutomationElement, MTA | Link its event to the original query; ID was read after the query. Cap ID array32; truncation cannot establish exact identity. Provider unavailability does not trigger another read/root/peer. No extra pre-query RuntimeId read. |
| WindowId, window runtime type, IsLoaded/IsVisible, width/height | Actual Window reference and scalar FrameworkElement properties, owning STA at capture/query boundary | Record only finite dimensions; wrong dispatcher/shutdown yields unavailable packet. No UpdateLayout, focus, activation or automation-peer access. |
| HostId, ContentId/type, ReaderViewId, ContentIsReaderView | Actual AtlasLoadingHost reference, Content, ReaderView, ReferenceEquals; same STA sample | Distinguishes placeholder Content from current reader view and differing live hosts. Null properties are observed null. ReaderView can be non-null but not the current child; retain both edges. |
| Host state category | Match internal StatusText against only existing known fixed host states: loading/no-workspace/inactive/canceled/unavailable; otherwise other | State category is independent of actual Content. Never emit raw StatusText or infer Content from it. No producer generation number. |
| ViewId, FilesControlId, OutlineControlId, SourceControlId, source-document object ID | Actual public AtlasReaderView properties on STA | Direct view->control relationships. Document is identified as an object only; no source text, lines, binding or document path. Getter failure is per-field unavailable. |
| View loaded/visible, read-only, CanGoBack, CanLoadMore; file-root/outline/highlight counts | Public view/FrameworkElement scalar properties and existing IReadOnlyList.Count, STA | Structural observations, not proof of source correctness or UIA membership. No collection item dumps, selection labels, source text or tokens. |
| Visual parent/window connection, census membership/status | Bounded VisualTreeHelper child/parent operations on owning STA | Parent object IDs and complete/incomplete connectivity only. No logical-tree fallback or other-window search. Missing from an incomplete census stays unknown. |
| ObservedReaderId, inner ReaderId, LeaseId, Disposed | Exact existing test wrapper fields/constructor/admission references; callback's actual thread recorded; consume immutable packet on STA | Reader->inner and Reader->returned lease are direct. Role labels are auxiliary; record all replacements. Do not dereference WPF from callback threads. |
| lease terminal/invalidation, release-started/returned/healthy-at-release flags | Existing wrapper scalar state/read-only lease properties, sampled separately with actual thread/time | No lease query, AdmitAsync or DisposeAsync is introduced. No ScopeToken/InitialManifestToken/receipt/native binding value, expiry or source data. Unreadable/disposed getter gives unavailable. |
| Counts, caps, Truncated, limit reasons, Unavailable count/types, write status | Observer's actual counters and caught diagnostic exceptions | Exception type/category only; no exception message, StackTrace, arbitrary object ToString or serialized exception. Sink failure cannot alter original assertion/outcome. |

## Ordering and original-result preservation

1. Register already-obtained journey references at their existing assignments. At Capture, buffer pre-render and post-render WPF packets on the current STA; attach the capture name to the existing PNG receipt event. Pixel rendering is unchanged and is not run by the observer. Buffering avoids adding diagnostic file I/O between a new pre-query sample and the query launch.
2. Immediately before the existing ObserveAutomationAsync await, buffer the STA `before-query-batch` packet and allocate BatchId. Call the original method once. Inside its existing MTA Task.Run, keep the apartment/process assertions and exact FromHandle. For each existing name allocate QueryId and capture query timestamps without a pre-query receipt write or extra UIA call.
3. Preserve `uia.find-first.original` and original returned element. Add correlation fields to it and to the already-existing after-original root diagnostic so RootRuntimeId is joined from the same root observation. Keep Descendants/Name condition, order of five names, NotNull/IsOffscreen assertions and all existing timeouts. No alternate result, retry, cached/created/refreshed peer, focus/layout change, activation or fallback root.
4. In the caller's finally surrounding that same await, collect `after-query-batch` from the owning STA continuation and best-effort flush bounded immutable observer packets. If the original call fails, its original exception still unwinds; any new diagnostic read/write exception is contained within the diagnostic wrapper. Do not wrap or suppress the original assertion. A failed query may therefore have a later WPF observation, explicitly tagged `after-original-failure`.
5. No synchronous cross-thread Dispatcher invocation, extra dispatcher queue action, sleep, retry, event pumping or UIA wait is added. A wrong-thread WPF call emits unavailable and returns. If the existing continuation never resumes, the post packet is absent/not-recorded; no watchdog or extra timeout is added. Existing provider calls and sink I/O may perturb timing; no zero-observer-effect claim is made.

New diagnostics must use a no-throw best-effort boundary covering their own field reads, formatting and sink call. Diagnostic failure must not call an assertion, return an alternate AutomationElement, mutate receipt Completed, or independently increment FailureCount. Preserve original exceptions by catch only around diagnostic work and by the existing await/finally structure; exception containment itself must be controlled. Catastrophic runtime failure is not a recoverable diagnostic guarantee. If the sink is unavailable, record that status in the bounded in-memory packet for any later successful save; durable diagnostic coverage is unavailable if no save succeeds. Do not claim a persisted packet that was only buffered.

## Non-GUI implementation controls and later fidelity gate

These are future mandatory controls, not runs performed in this design unit. Keep them in the same admitted native test file and use an explicit non-GUI filter; never run the whole class, whose blank-window and daemon tests show windows.

| Control | Discriminator / red oracle |
|---|---|
| Identity reuse and equality collision | Two different objects with equal labels and deliberately identical Equals/GetHashCode receive different IDs; repeated same reference returns the same ID; no recycled ID after detach; saturation returns unavailable. Reject a name/hash-keyed registry mutant. |
| Multiple/replacement/retained graph | Pure immutable rows representing two hosts, changing Content, old retained view/reader and new view/reader produce distinct relationships; old IDs remain old. A current-child edge and ReaderView-property edge are independent. Reject a first-host-only or label-coalescing mapper. |
| Partial census and unavailable reads | Inject bounded generic graph edges, cycles/depth/node/queue saturation, getter errors, null content and missing reader roles. Complete disconnected chain differs from unvisited/truncated. Assert exact observed count plus flags, not invented totals. |
| Original-result preservation | Pure orchestration adapter invokes its original delegate exactly once. Null remains null and the caller's original assertion fails; a sentinel original exception remains the same exception object. Throwing observer and throwing sink cannot replace it. Successful original delegate retains its exact object/result. Reject a fallback/retry or rethrow-new-exception mutant. |
| Timing and correlation | Deterministic clock/counter inputs prove separate before/query-start/query-end/after packets and monotonic IDs; RootRuntimeId explicitly belongs to the later root observation. Out-of-order/missing samples remain labeled, not filled. |
| Bounded safe payload | Golden/schema assertions allow only documented primitive fields/rows, enforce caps and unavailable variants, and reject raw source/token/text/exception-message fields. Real temporary file sink round-trip retains append order and unavailable status. |
| Native source contract | Inspect/compare the actual original FromHandle/process assertion/selector/result/assertion/timeout statements and zero new peer/layout/focus/activation/wait operations. Non-GUI orchestration controls alone cannot establish that the actual WPF/UIA integration preserves them. |

Testing Strategy union for later implementation: D0 hygiene; D1 identity/result mutation resistance; D2 bounded input and graph invariants; D6 packet schema/golden payload; D7 pairing any pure substitute with the actual adapter contract; D4 for the existing real receipt filesystem and later authorized native integration. No new service protocol/AI behavior is introduced. Installed WPF/BCL/property contracts are grounded in current source use; execution fidelity remains a required later spike/non-GUI compilation check and fresh native slot, not an inferred live-UIA proof. No design-only mock result is called a production observation.

## Failure, security/privacy and review lenses

Failure dispositions: input identity collision prevented by ReferenceEquals IDs; dependency/getter/sink unavailable detected and contained; concurrency misuse detects wrong dispatcher and keeps WPF reads on STA; state replacement records multiple references/edges without private-generation inference; resource growth bounded with explicit strong-retention lifetime; time limited between operations without pretending to interrupt a blocked call. Query outcome is never recovered through another query. Timing perturbation and incomplete census are consciously accepted diagnostic limits; they cannot establish a necessary-and-sufficient cause.

Trust boundary is the already-owned HWND/process plus local test receipt. STRIDE dispositions: spoofing constrained by original process assertion and exact run IDs; tampering cannot rewrite the original result; repudiation mitigated by timestamped immutable packets and artifact/source hashes; disclosure bounded to structural primitive allowlist with no source/token/message dumps; denial of service bounded by output/work ceilings with non-preemptible-call limits stated; elevation absent because observer invokes no product operation or broader UI root. No new personal-data flow: structural instance IDs die with the run; existing local proof retention applies. Existing security/privacy product records stay unchanged; parent owns any graph/central register updates. No new threat is quietly converted into product authorization.

Peer design lenses (not independent approvals): Patterns/Simplifier recommends one bounded registry and immutable packet builder, not a tracing framework. WPF/Distributed Systems insists on separate MTA query and owning-STA read intervals. SRE requires observed/unavailable quantities and no hard-timeout fiction. Test Architect requires actual insertion-point review plus original-result controls and a future native fidelity slot. Security requires same-root binding and the strict payload allowlist. Data/Persistence marks product modeling N/A while retaining append-only diagnostic facts. Adversarial open point: private view-to-lease linkage and producer generation are deliberately unobserved; Owner must accept that scope or settle another seam. No peer persona statement clears the independent veto.

Class -> sweep -> derive -> prevent: DC-118's proof-width mechanism applies to translating a loaded screenshot or same label into same-instance/provider claims. Sweep covers host, Content, view, controls, observed reader/lease, original query and later root RuntimeId as separate edges/intervals. Derive only relationships emitted from actual references; do not invent causal transitions or generation. Prevent through the listed identity, partial-census, unavailable and original-result discriminators. They remain planned until implemented/reviewed. No cause-removal defect class is asserted for the unresolved native failure.

| Completed | Remaining | Best next action |
|---|---|---|
| Opened failure/raw root evidence and actual source contracts; bounded schema/insertion/control proposal | Independent Test/SRE design review of the Owner-A observation scope | Review this appendix before separate same-file implementation dispatch |
| Native source unchanged; no GUI or tests executed | Implement and qualify non-GUI controls in later authorized unit; reviewed freeze | Schedule a fresh owned native slot only after that review |

Design evidence is this appendix and the unchanged native source pin. No experimental runtime claim was produced. Initial broad reads were truncated and targeted reads recovered the relevant methods; one guessed spike-doc path and one reader-interface filename were absent and no implementation depended on them. Actual administrative/call cost and lease release are recorded by the closing audit; this unit reserves its last call for closure, avoiding the prior asynchronous-test budget pattern. Original investigation and failure evidence above are preserved byte-for-byte in the working proof before Git newline normalization.


### Owner A settlement and final author return

Owner chose A during this design unit: use existing public/test references only, with actual host -> current child -> ReaderView reference and attachment observations. Reader and lease IDs remain separate test-wrapper observations. View -> backing lease and private producer generation are explicitly unobserved; neither role labels nor chronology supply those missing edges. Observed attachment/replacement transitions are not the private generation counter. Root RuntimeId remains an after-original-query observation of the same root object correlated by QueryId. STA before/after packets bracket the existing awaited batch, with after in finally; they are not simultaneous observations. If a later probe requires a missing private link, it needs a separate Owner decision. No source implementation is authorized by completing this design alone.

Actual author cost:8/8 tool boundaries, one author/no delegates; measured duration from the original design-slice start marker is recorded in the closing audit. No tests, compilation or GUI operations were run. The only committed authored paths are this proof appendix and own official audit JSONL. Native source SHA remains53b792e4775f76279f199ccccee485d9143cb044abfbc3ffdc4f6d34573e2613. Generated audit-data and local scratch are retained; no cleanup/join/push or independent clearance occurred. Proof write/readback is explicit UTF-8 and atomic; closing command preflights the required audit shortname and identity, releases the exact proof lease in finally, and reports only the observed PRIMARY liveness outcome.


# Observer implementation receipt — 2026-09-16

Status: author freeze for independent review; no native result or cause clearance. Owner settled design e41a176c and independent design CLEAR973afc97a8bcd981e607aa73be0c89247045a3c5 authorize this same-file diagnostic preparation. The original P1-02 investigation above remains unresolved. Surface list: capture -> original query -> separately timestamped owning-STA snapshot -> receipt -> non-GUI controls -> independent Test/SRE -> later fresh native slot.

The only source change is the native test file. A test-local ReferenceEquals list supplies bounded run-local identities. Actual host Content and ReaderView are read independently, with visual attachment and retained references; public view controls/counts and separate wrapper reader/inner/lease identities are observed. Private producer generation and view-to-backing-lease are explicitly not observed. The release-start row is emitted after the existing release-start Receipt.Mark returns, with its own event sequence/thread/time; it is not an invented state flag.

Two native callsites use the actual await/finally wrapper: before and after packets bracket the unchanged awaited batch. Capture similarly brackets the existing Render once. Original FromHandle/process assertion, five names, single Descendants+Name lookup per name, returned element assertions, waits and30-second dispatcher timeout remain decisive. Query IDs correlate original query start/end/result with the later same-root RuntimeId observation. RuntimeId stays after query and is capped at32 values. The pre-existing diagnostic helper catch is unchanged, not silently repaired or promoted to a new guarantee.

The actual adapter runs in non-GUI controls on an owning STA with an unshown Window and real visual parent relationships; HWND remains zero. Actual AtlasLoadingHost/ReaderView references are established by fixture setup only. Controls observe two hosts, a Content replacement while ReaderView still points at the prior view, then detachment/deactivation and retained old references. Fixture source/authority sentinels do not appear in the serialized receipt. The original helper is replaced only by a test callback within controls; actual await/finally, adapter, Receipt.Mark/Save and JSON formatting paths execute. A missing-directory sink and throwing packet converter test real failures; original exception object/null-result assertion/success are preserved. The converter is optional and null at native callsites. No live UIA operation ran.

Bounds are256 references,16 packets,512 visited/queued nodes,depth48,16 hosts,32 views/readers/transitions; observations report unavailable/truncated. The25ms check occurs between operations, not hard preemption or an I/O latency guarantee. Strong references are intentionally retained until observer disposal; rows identify observation retention. Wrong-thread reads return unavailable. Added sampling/formatting/sink failures are contained; catastrophic runtime failure is outside this guarantee. A buffered packet is not evidence of successful persistence.

## Chronology and actual results

All runs used normal builds and only the NativeObserver_NonGui_ prefix or a narrower named control. No whole-class/full-App/STA-native/shown suite was run. The first11 controls were authored against a no-op observer skeleton and missing integration: all11 failed before the concrete observer implementation. The first concrete implementation passed11. Additional-red added metadata/release-event requirements (two real implementation gaps) and a source-contract test whose third failure was an incorrect Roslyn ArrayCreation versus actual ImplicitArrayCreation test expectation. Null-red then exposed a real observer defect: ReferenceEquals(null,null) falsely labeled null Content as a reader; a non-null guard and permanent null case fixed it. Complete passed16. The final source strengthens the mismatched Content test and adds bounded RuntimeId coverage, for17 cases.

Earlier source pins below were reconstructed from retained exact author scripts, not claimed as contemporaneous hash measurements; reconstruction was verified against the recorded complete-source hash d1d20e2366841cb138bd72d6d76770d354471e1d17a9997b1c58b236c34cb578. Actual TRX Times establish chronology. Later four mutation runs are post-implementation adversarial checks, not historical red-first evidence.

| Configuration | Actual TRX start UTC | Executed/pass/fail | Source SHA256 | TRX SHA256 |
|---|---|---|---|---|
| observer-red | 2026-09-16T09:31:11.5480382-07:00 | 11/0/11 | ed6d076fa02f4fa6dbc00ffe71f9602b1a8248887b0a6766b2d792200cc33e0e | 409cdd28512299e0a0ea2c252677c206ea789b4d9a19c73b60edb963569356ae |
| observer-first | 2026-09-16T09:35:25.9327925-07:00 | 11/11/0 | bd7486025d2050d9123451d092bec24b5e59a226b6a983d9cde3cb4fcac53c99 | f2c4d6f0a98135831a3f6d2eff94a7022ead7d7160e8631001ad58b37800fd6c |
| observer-additional-red | 2026-09-16T09:38:31.0558036-07:00 | 14/11/3 | 33e6471124f8a80eb58fd29813557143a20cfacd6fac54c686740348dc826068 | 57c6ab9330849209fa22af1240b2186451a6e8345a083cc97087e0b95c48dc8a |
| observer-null-red | 2026-09-16T09:40:24.5514521-07:00 | 7/6/1 | 09931c6013517fa7a46dbbfd8c7db73bca15fb775ec9f079a16c907ad05feb7f | e6fc32cdb6077a669ecb0b8ae5437f1a69e8390b135edaadd4e04262e0126e9b |
| observer-complete | 2026-09-16T09:41:59.0808863-07:00 | 16/16/0 | d1d20e2366841cb138bd72d6d76770d354471e1d17a9997b1c58b236c34cb578 | ebb0099e477c986bac603093a7b3cff74f85199ffcd849efdab29431aa76da3b |
| observer-mutant-equality | 2026-09-16T09:44:36.7805666-07:00 | 1/0/1 | b9f64a6557bb9a9c96dae36bcd693cf9bed8c1bdd650e8e02a9c2f94f952cc70 | bf9cd43cbfb3efaa8fb27441733315000582fb44645354fcddb4bddcd4eb57ae |
| observer-mutant-child | 2026-09-16T09:44:40.1764352-07:00 | 1/0/1 | 3c95634c90c8aee16385f62b6102966c53fcdde6a1737dd1214a7d586bfaf2e5 | 37160f6f4707113e1c0e763c6d719d4e3c9fe60c7da7cce3449d555ec8e5b141 |
| observer-mutant-finally | 2026-09-16T09:44:43.8160017-07:00 | 6/0/6 | ca76501d52ceee9f8a9f18f76d1b4f055899ab708afb254443624317f1688c1c | 7a4b788c96f818d95507586b101ec16c1d9518b604d9f2e20354119b0e2416fd |
| observer-mutant-sink | 2026-09-16T09:44:47.2053659-07:00 | 6/4/2 | 01663a07a560996119ad10d3cc2e9e40210e6b21e0b47277050b53fbb14728bc | 2eeab23cd33ba98898db6f3958b354bee58123c6ada1da474ec2374762c6d364 |
| observer-final | 2026-09-16T09:44:50.6078780-07:00 | 17/17/0 | 5565b10a9dffdd3f30dfa02ccb5bd41080e6694370fb4754844a78727f9659e5 | e36418be7c1c79f72a9a66969324db33062a219937274933ef677ac8515a8bed |

Mutation discrimination: replacing ReferenceEquals with Equals collapsed two distinct equal objects (1/0/1); replacing current Content identity with ReaderView identity failed the actual adapter replacement row (1/0/1). Removing the real finally after-snapshot failed all six integration variants (6/0/6). Rethrowing receipt errors failed actual missing-directory and formatter variants (6/4/2). All mutant runs exited1; source restoration was byte-verified before the final normal build exited0 with17/17 pass. All actual result names/messages and counters were read and retained in inspected-results.json; no count-only acceptance is asserted.

Exact raw receipts live under .artifacts/atlas-instance-design/: observer-red.trx, observer-first.trx, observer-additional-red.trx, observer-null-red.trx, observer-complete.trx, observer-mutant-equality.trx, observer-mutant-child.trx, observer-mutant-finally.trx, observer-mutant-sink.trx, observer-final.trx and corresponding logs. source-chronology.json identifies reconstructed pins; mutation-results.json records actual mutation pins/exits; frozen-before-mutants.cs.txt is the restored final byte source. adapter-evidence.json names and hashes the actual serialized adapter receipt and preserves its rows.

Frozen source SHA256: 5565b10a9dffdd3f30dfa02ccb5bd41080e6694370fb4754844a78727f9659e5. Actual adapter receipt: .artifacts/atlas-observer-controls/4a8c97b70a5243cab7ad644d6800d690/receipt.json (SHA256 10e3625000ec61ca7749cbc58dcf02eeb4270550f10f9a4c4b26d5d40cb22dde).

Class -> sweep -> derive -> prevent: DC-118 proof-width discipline distinguishes reference, attachment, wrapper lease, query interval and later provider identity. The null-equality defect was swept across reader identity observations and prevented by a null-content control. Wrong Roslyn syntax-kind expectation was corrected against actual syntax and retained source-contract control. Closure briefly retried a nonexistent coord.py path and a Windows literal glob; the already-used coord-core.py contract was recovered before mutation. These administrative mistakes did not alter source or rerun tests. Parent owns central recurrence/derived records.

Actual author cost:18/18 tool boundaries, one author/no delegates, marker started16:25:50Z; measured closing duration is emitted by official implement audit. Context/token usage was not measured. Proof is explicit UTF-8, atomic and byte-read back. Commit contains exactly test, this proof and own official audit. Generated audit-data and raw artifacts remain retained. Lease release and PRIMARY liveness are reported from actual closure results. Independent source/non-GUI review remains pending; no own veto clearance, native causal diagnosis, join or push. The next stage is independent review of this exact source and controls, then Owner allocation of a fresh native slot.


# FR-NO-001 correction receipt — shared observer payload admission

**Author correction frozen for independent re-review.** Independent BLOCK4d005a9e276cc1c3070005f3bc2ecab7eecde997 remains authoritative until the retained reviewer clears it. Its receipt, docs/proof/atlas-native-instance-observer-review.md in the separate review tree, and real SinkProbe.cs/shared-sink-probe-final.log were read in full. The earlier17-case green did not establish shared-sink containment: its formatter control used a different Receipt. The reviewer proved that a caught observer formatting exception left its object in the shared event list, making a later original write fail. This supersedes that earlier containment claim while preserving all historical evidence above.

Owner A decision cl-01M2NJE8DAE8H4PBQJEP3D2YCS selects pre-serialization, not rollback. Goal: correct only this persistent shared-state failure. Done when the actual shared Receipt control is red before the fix, green afterward, original outcomes remain decisive, and exact source/proof/audit are frozen. Scope excludes product, native/UIA/shown/full suites, private observations and causal diagnosis. Tier T2, one author/no delegates. Graph: review counterexample -> four permanent controls/red -> six-line admission helper/all3 observer callsites -> normal21-case union -> validation-only mutant -> restored normal build -> proof/audit/commit/release.

The new Receipt.MarkObservation uses JsonSerializer.SerializeToElement with the actual Receipt.SerializationOptions before calling the existing Mark with that immutable JsonElement. All new observer WPF packets, lease transitions and status use it. It publishes the serialized value, never the original object after validation. Existing Mark and Save bodies were compared against781acb2b and remain byte-equivalent text; no general receipt framework, rollback, original-error swallowing or new dependency was introduced. Surface list: real adapter payload -> actual receipt serialization options -> immutable event attributes -> existing Save -> subsequent original Mark/Save -> persisted JSON readback.

Four added permanent cases retain all17 existing controls unchanged. Three run the real await/finally wrapper and observer adapter on an unshown owning-STA Window with the SAME actual Receipt and throwing WpfObservation converter. original.before persists, observer formatting fails and SinkUnavailable is true, the exact original sentinel exception/null assertion/success survives, and original.after succeeds. Saved readback contains both original events, FailureCount0, Completedfalse, and HWNDzero. The fourth uses an actual one-write packet converter: a real unavailable-input adapter packet serializes once, then later shared writes preserve its JSON snapshot without revisiting the converter. This tests immutable publication, not merely an initial validation call.

## Executed chronology and pins

All four commands were normal dotnet test builds restricted to FullyQualifiedName~AtlasDaemonMainWindowProofTests.NativeObserver_NonGui_. Initial red was observed BEFORE MarkObservation existed. Red's four failures were three DO-NOT-EMIT-FORMAT exceptions on later original writes and one SinkUnavailable assertion caused by repeated packet formatting. Green passed21. The post-fix mutant changes only Mark(stage,snapshot) to Mark(stage,attributes), leaving a second-serialization failure in shared events; the one-write control fails. This mutant is post-implementation evidence, not historical red. Source restoration is byte-verified and followed by a final normal build. Every TRX name/outcome and all failure messages were parsed/read.

| Run | Actual start | Executed/pass/fail | Exit | Source SHA256 | TRX SHA256 |
|---|---|---|---|---|---|
| red | 2026-09-16T10:00:04.0541349-07:00 | 21/17/4 | 1 | e8bb58647fe1383bddb529d0e6123cb546329d002541da448eebb73b1fe59bd8 | 4b9f577629e977e6625fd76577e3b2d315cf00148e4d3af4cb41f60e7746e364 |
| fix | 2026-09-16T10:00:18.4736194-07:00 | 21/21/0 | 0 | ae749110bb063a71609b2df3de68203f4002a223291486912786f1fd24735dca | b0ba6bc2121bf74d5730c9de982e9c80f718fcb026ac7315b00feedd04586667 |
| mutant | 2026-09-16T10:00:30.9471961-07:00 | 21/20/1 | 1 | c490741807dd263c19788bbad63dd2145afa201867086dac93d9ae69930a81f8 | 505fd0172e4b1567a00e8db16e994b3f4d2ba3797c28e2fd7b65802d5146d94b |
| final | 2026-09-16T10:01:01.8824591-07:00 | 21/21/0 | 0 | ae749110bb063a71609b2df3de68203f4002a223291486912786f1fd24735dca | 998b5b9be866c15809cac3d3b10c89ac045b235b4dec34ca56ba742cf6c6b2cb |

Raw evidence: .artifacts/atlas-instance-design/sink-correction-{red,fix,mutant,final}.trx; red/fix/mutant logs and JSON include observed pins/exits/timestamps/messages. sink-correction-inspected.json preserves all21 result names per run. sink-correction-shared-readback.json identifies and hashes actual persisted shared receipts; those files remain under .artifacts/atlas-observer-controls. sink-correction-frozen.cs.txt and each mode's source copy retain mutation/restoration evidence. Final console run was inspected directly; its TRX is the frozen acceptance artifact.

Failure semantics remain narrow and explicit. Formatter-failed payloads never enter shared events; their observation packet may remain in the bounded observer buffer, with SinkUnavailable true. Flush advances its attempted index before formatting, so failed rows are not retried automatically. Status may be absent if a flush exits on failure; an in-memory flag does not prove a persisted status. A filesystem Save failure may still leave an already-serialized safe JSON event in shared memory under the unchanged original Mark semantics. This correction makes no persistent filesystem recovery, guaranteed durability, zero overhead, native causality or live UIA claim. Catastrophic runtime failure remains outside the diagnostic containment guarantee.

Peer design lenses (not independent approvals): Patterns/Simplifier selects the installed stdlib SerializeToElement primitive and one local admission method, preserving existing receipt operations. Test Architect carries D0/D7 shared-resource fidelity, red-first failure paths and immutable-publication mutation discrimination. SRE distinguishes failed formatting before admission from a later filesystem failure and retains truthful unavailable status. Security retains the structural payload allowlist; no new source text/private authority. Data/Persistence: product modeling N/A; observation event grain remains one separately timed diagnostic fact. Adversarial Test/SRE verdict remains BLOCK pending independent replay of the same-receipt counterexample at the new pin.

Class -> sweep -> derive -> prevent: catching an exception does not undo persistent shared-state mutation. Sweep covers all3 added observer Mark sites and subsequent original Save, including their actual shared receipt and serialization options. Derive immutable observer admission before shared mutation. Prevent with same-receipt before/fault/after controls across all original outcomes and the validate-then-publish-original mutant. Parent owns the central recurrence register/derived union; no shared ledger was hand-edited.

Actual author cost:8/8 tool boundaries, no delegates; implement marker16:58:35Z, closing audit supplies measured duration. Context/token usage not measured. UTF-8 proof write is atomic and byte-read back; only native test, this existing proof and own official audit are committed. Exact leases release in finally; PRIMARY liveness is updated from observed outcomes only. Generated audit-data/raw artifacts remain retained. Remaining: independent source/reproduction review, then separate Owner allocation for any native slot. No own clearance, join or push.
