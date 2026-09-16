---
id: investigation-cross-harness-message-delivery
title: "Cross-harness message delivery: receipt is not agreement"
type: investigation
status: draft
owner: "@timianmalloo"
phase: investigation
tags: [coordination, messaging, liveness, root-cause, human-review]
links:
  - { to: session-contracts, rel: relates-to }
  - { to: spec-agentic-watcher-substrate, rel: relates-to }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: relates-to }
  - { to: adr-0023-watcher-observation-projection, rel: relates-to }
review-by: 2026-10-16
summary: >-
  Six bounded coordination cases separate recorded responses from consumed dispositions,
  contract acceptance, receiver availability, and execution authority. Actual-fold diagnostics
  demonstrate threading and replay hazards; this report proposes repairs and conditional
  service objectives, not an implemented protocol or permission to resume blocked work.
---

# Cross-harness message delivery

**Status:** diagnosis partly verified; repair proposed; STOP for actual human review.
**Severity/tier:** T2. A production-reuse blocker is identified below; no product change is authorized.
**Requested:** 2026-09-16 by the actual user, `/investigate`.
**Owned tree:** `C:\Projects\ai-de-investigation-cross-harness-message-delivery`,
branch `investigation/cross-harness-message-delivery`, base
`bcf4959bc0e0e361736e6a179f05b69fcd0500f8`.
**Method:** events-and-causal-factors with competing hypotheses and inert inverse controls.

## 1. Root-cause overview

There is not one “ACK lost” incident. The selected records show several distinct waits:

* **[Major] (Verified)** The repository request fold has only **open/resolved**. A new
  `request-add` carrying a response does not change the initiating request. Codex explicitly
  recorded “CHANGES REQUESTED, not contract ACK” and acknowledged leaving incoming requests
  open. The old question therefore remained apparently unanswered despite a sent response.
  The actual `fold_requests` function reproduces this; inserting an explicit resolution removes
  the ambiguity in the projection. This verifies the state mechanism, not every participant's
  reading history.
* **[Major] (Verified mechanism; Inferred contribution)** A completed answer is absent from
  an open-only list. Three R115 acknowledgements existed before a later identity request.
  That does not prove the waiting actor used that particular list command. The durable record
  proves the answer existed, not that it reached the correct conversation or was understood.
* **[Major] (Verified)** Model failures and pull-only notification paths are not contract
  refusals. The foreground trace records context-window and HTTP-body failures. A later
  foreground-owned attached observer has actual `read_powershell` completion/readback evidence;
  stdout alone and a child's process exit are not model wakeup evidence.
* **[Major] (Verified)** Test instrumentation claims a fixed Grok author regardless of its
  caller. A later test-process START cannot cause an earlier native failure. Author labels,
  registered actor identity, process ancestry and exclusive-slot ownership are different facts.
* **[Major] (Verified)** The filesystem request queue is not the AI-DE board's wire format.
  The reviewed board parser has no request-add/request-resolve cases. Its existence does not
  establish cross-harness enrollment, notification, consumption or agreement.

**Answer:** yes, a formal cross-harness **interaction contract** is needed, but not a second
ownership ledger, another independently writable status database, or an unconditional response
SLA. Much of the board envelope, registration, quarantine and parent validation already exists.
The missing common contract is typed disposition, correlation, routing generation, exact
proposal acceptance, availability and accountable escalation. Keep approval separate.

### Evidence boundary and currentness

This is a finite sample, not a census of all stalled work. Primary `.agents/requests.jsonl`
was read through the official repository-wide fold: **436 events, 316 requests, 198 open,
zero parse errors** at sampling. An open count is not a blocked-session count; it includes
announcements and completed work whose notification requests remain open.

Sources:

| Ref | Source and scope | Clock / confidence |
|---|---|---|
| Q | `C:\Projects\ai-de\.agents\requests.jsonl`, official `coord-core.py request list --json --status all` and `read_request_events/fold_requests` | Producer wall-clock Unix seconds, sorted by `at`; no clock-synchronization claim |
| T | Local parent `b0d0c445-0dbc-47cb-8e51-19bfe9427c29/events.jsonl` | Harness UTC timestamps; 15,588 events streamed at sampling, only selected tool/message fields inspected; no reasoning fields |
| O | Same session's `files/main-watch-observer.py` and recorded foreground-001/002 tool results | Source inspected read-only; no observer/state/nonce/lock changed |
| B | Board and coordination source at the base commit above | Static evidence; source version is not evidence that every running binary matches |
| P | Producer handshake documents at `f073e2a0`, `62670a0a`, `17cd8317` | Git commit/blob identities, not a live acceptance claim |
| L | Primary `.agents/sessions/*.md` and official liveness implementation | Self-reports and ledger age, not process or model-health proof |

`coord-core.py` SHA-256 for the executed diagnostics:
`f73185a306f7a5b63184cd0cc759569030d29bf7115ad8bd30adf6c19f8bdacf`.
Other source citations bind to the base commit and function/line ranges below. The parent
session is a local evidence locator, **not** a published transcript attachment.

The initial graphify query produced irrelevant pack nodes, and inspection found **zero**
MessageBoard/SessionContract/Loomkeeper label matches in that graph. No graph rebuild or
extraction was performed. The maintained docs index supplied real typed edges:
`adr-0023-watcher-observation-projection -> implements -> architecture-loomkeeper`
`-> implements -> spec-agentic-watcher-substrate -> relates-to -> session-contracts`.
ADR-0020 also implements that architecture/spec and refines ADR-0007. Source reads followed
those documents. Graphify coverage is a **[Minor] (Verified)** orientation gap, not a fabricated
edge or proof of absent product code. An attempted docs context packet was refused because
2,500 bytes is below its 4,096-byte minimum; no packet was represented as successful.

## 2. Specific proposed fixes, not implementation

1. **Correlate response and disposition.** Add a typed response event keyed to the initiating
   request and exact revision; one fold projects both the question and reply. A response must
   remove “unanswered,” while `changes-requested` must never become `accepted`.
   Preserve the original append history. Roll back by disabling the new adapter/projection,
   retaining events for replay; never restore an older writable status copy.
2. **Read actionable threads, not just open rows.** Return pending obligations with their latest
   responses, accepted revisions and supersession. Retain an all-status by-ID lookup and a
   cursor/gap indication. Existing callers may keep the legacy list until migrated.
3. **Bind routing to an actual conversation generation.** Record send/queue admission separately
   from arrival/triage. A paused or failed endpoint exposes the obligation rather than inventing
   an ACK. Check superseding rulings at bounded turn boundaries and before irreversible actions.
   Cancellation requests are not proof that the old turn stopped.
4. **Reuse board infrastructure only after replay/ordering and identity gates.** The board should
   display authoritative coordination events, not acquire an independent ACK/freeze state.
   Disable the optional adapter to roll back; filesystem coordination must remain usable.
5. **Fix actor provenance in test notices, not the investigated native product.** Bind actor,
   session generation, worktree/candidate, run ID, PID plus start time and parent evidence from
   the launcher. Do not infer actor identity from a diagnostic run label. Roll back the notice
   adapter without changing tests' functional assertions.

### A blocker before extending board delivery

**[Blocker] (Inferred, concrete replay interleaving; static path Verified):** the existing
whole-directory board pump is unsafe as a reliable coordination delivery foundation.

`CoordContractLogPump.PumpOnce` (`CoordinationContractLog.cs:264–269`) reads the entire log and
calls `ApplyAll`. `InjectedContractIngest.ApplyBoardPost`
(`CoordinationContract.cs:576–628`) posts every recognized board event; it has no consumed-event
key. `MessageBoardService.Append` allocates a new GUID and count-plus-one sequence on each call.
`SqliteWatcherObservationStore.AppendBoardMessage:546–570` performs an ordinary INSERT.

Interleaving to confirm: register S, append board question Q, pump once => M1; pump the unchanged
directory again => fresh M2 for Q. A successful-but-unacknowledged retry has the same shape.
Duplicate registration suppression does not suppress posts. With repeated historical
register/end records, session restart/replay also needs separate examination. The test named
`Pump_ReRun_IsIdempotent_NoDoubleRegister` tests registration, not this side effect.

**Smallest required floor:** source-event identity and payload-conflict detection, atomically
persisted with the projected fact; retries return/reuse the original message identity.
Co-review this store boundary with **Data & Persistence** before choosing a migration.
Do not add a second “consumed” file that can get ahead of, or behind, the durable effect.
No C# runtime replay test was executed during this investigation. This is **not** established
as the cause of the sampled filesystem waits, and the investigator does not clear this veto.

Related hazards, **[Major] (Inferred scenarios)**: the parser orders by wall-clock
`(at, externalSessionId, seq)`, so a skewed reply can precede its parent and be quarantined;
the exporter keeps the newest 200 messages, and MCP keeps the newest requested page, so a
naive `sinceSeq` consumer can jump over an unconsumed middle range. The wire ACK currently
has no content/hash/disposition; it is not an agreement primitive. Required tests and
availability bounds appear in phase P2, not as claims of current delivery guarantees.

**Independent-review cursor interleaving:** `MessageBoardService` has an instance lock,
not a cross-instance transaction around count-plus-one sequence allocation. Two instances can
choose N+1; a reader can advance to N+1 after the first insert, then miss the later second
insert forever with `Seq > sinceSeq`. The store's primary uniqueness is message ID, not
repository/sequence. P2 must establish a committed ordering/cursor contract that cannot skip
a later-visible fact; deduplicating source IDs alone is insufficient. This remains Inferred,
not a runtime-reproduced defect.

## 3. Six representative timelines and competing explanations

Times are UTC. Elapsed values below are **measured record intervals**, not model processing
latency, actual blocked duration, or an SLO distribution. Different producers' wall clocks can
skew these values; UTC spelling does not prove synchronization.

### Case 1 — Notices, responses and exact contract acceptance

| Recorded event | Evidence | What it establishes / does not |
|---|---|---|
| Sep 15 23:03:19.596/652 | `req-01M2KMCNBE64YY18Y358K8WG5K`, `req-01M2KMCND5SQDQXWM3ZY70Q14Y` | Two foreground notices, not bilateral agreement |
| 23:13:26.062/117 | `req-01M2KMZ5KFGWD7DSDCN8FKP2GV`, `req-01M2KMZ5N6E8G3AD5V1ZJKCMR3` | Explicit handshake asks for identity/provenance/bounds/navigation and authoritative pins |
| 23:23:07.766 | Grok resolves KMZ5K: proposal `f073e2a0`, blob `661c92a3`; “PEER ACK pending. NOT FROZEN” | Sending a proposal is a completed notice obligation, not agreement |
| Sep 16 00:20:54.997 | `req-01M2KSD1JQG5WEBJVSDNYEH54J` | Codex asks for exact r3 corrections to r2 `62670a0a`, blob `76e592a32f38b5cf51c48c9a6fdd964e177f3cfc` |
| 00:21:06.907/963 | `req-01M2KSDD6X299H53AZSTJDYR7R`, `req-01M2KSDD8MXW7K3JEQ6N9WK7ZZ` | Grok still reports no peer ACK; correction was recorded about 12 seconds earlier, consumption unknown |
| 16:01:10.249 | Codex resolves KSDD8 | Explicitly “CHANGES REQUESTED, not contract ACK”; admits open incoming requests obscured disposition |
| 16:01:10.304/358/413 | `req-01M2NF6PN1Q182VKC3ZA4NST29`, `req-01M2NF6PPRTD964M85HPWE9TEV`, `req-01M2NF6PRFE3979RVKGSA1Y60E` | Same reset to Grok plus watcher and foreground copies, not three distinct agreements |
| 16:03:05.184 | Grok resolves KSD1JQ | r3 `17cd8317`, blob prefix `e448383a`, non-consuming boundary, same-blob ACK still pending |

The correction's send-to-resolution interval is **56,530.187 s**; KSDD8 remained open
**56,403.286 s**. Neither is a proven network delay. The transcript supplies no continuous
receiver-delivery clock for these intervals. r3 was published before this investigation
started; “r3 still absent” is therefore stale at this report's sample.

The inspected r2/r3 documents explicitly separate NOTICE SENT, PEER ACK, FROZEN and IMPLEMENTED.
r2 pins the E1 design `0bdd16d7`, spike `1ab5d9e9`; pinned E2 documents inspected are
`27642bf8:docs/design/atlas-architecture-views.md` and
`a9d86fc1:spikes/atlas-architecture-contract/RESULT.md`. The E2 result records 57 synthetic
checks and an earlier incomplete dependency-Basis oracle correction; those are its recorded
results, not tests rerun here. Those are producer/consumer context, not authority for this investigation
to settle a technical E1 mapper. The smallest repair is to show the **negative disposition
and next owner action on the initiating thread**, not to require native qualification or
a future mapper before answering the corrected non-consuming boundary.

**Ruling:** state visibility mechanism Verified; end-to-end delivery failure unproven.
An unconditional “Codex has not answered” diagnosis is ruled out by Q.

### Case 2 — Permission and identity after recorded R115 acknowledgements

Q records three resolutions by `claude-conductor-watch-0915` at Sep 15 **15:13:09.389–569**:

| Request | Sent | Recorded resolution elapsed |
|---|---|---:|
| `req-01M2JQMKGC16MV3TH3XK04JYPD` | 14:30:51.147 | 2,538.242 s |
| `req-01M2JRH3N90RG40BF42F7X08HN` | 14:46:25.192 | 1,604.286 s |
| `req-01M2JS1GRH9N99XT5RDKHV86BA` | 14:55:22.897 | 1,066.672 s |

At **15:23:47.436**, `req-01M2JTNHBDX3HAH2RSSHF7FJ62` asks again about responsible identity;
the **15:28:02.018** response names the Claude conductor and states the acknowledgements
already exist. It distinguishes stale older Core/Design liveness files.
The response prose says “ALREADY RESOLVED at 15:33Z,” inconsistent with both its own
15:28 event timestamp and the earlier 15:13 resolution events. Calculations above use the
event timestamps, not that contradictory embedded time (independently found at Q line 65).

The record does not prove malicious withholding, a missing authority decision, or which list
the receiver ran. The actual fold proves an **open-only visibility gap**, not the actor's
mental state. Sole file ownership remains `session-contracts.md §2`; current rulings describe
carve-outs. No new transfer is approved here. Current actual-human restrictions supersede
any inference from a prior persona ruling.

### Case 3 — In-process task delivery and addressability

The tool contract states that `write_agent` queues a message to a RUNNING agent until its turn
ends. That is supported deferred input, not interrupt delivery or immediate compliance.
T contains:

* **14:20:37.661 Sep 15**, event `81519d84-6ab3-4494-9dcb-3c7636b9745d`: a delivered relay says
  the definitive ruling is already queued while another relay describes the earlier dual-field
  implementation. This is evidence of a reported stale-instruction interval, not a measured
  worker consumption timestamp.
* **14:23:35.920/927**: direct messages to existing workers return delivered receipts.
* **14:26:36.403**, `36257cb5-e8bf-44c7-9700-e915e6a39edf`: a successful write asks the worker
  to return its saved receipt to closer `ed7d1cd1-d8e4-437a-83bc-f39eb926c352`, explicitly
  reporting sibling `read_agent` “No agent found.”
* **14:34:56.969**, `9d7cb640-8bd8-4b18-bbd3-5be233bc3bb9`: another receipt-only relay after
  the worker's reported idle notification, explicitly prohibiting a rerun.

**Observed:** routing workarounds and successful write-tool responses.
**Supported by the current tool contract:** sibling write versus owned-agent read asymmetry.
**Not established:** that “delivered” means the target model has consumed the message; raw
child turn boundaries/call counts for the reported 40–50-call author turns; exact added delay.
Those counts remain **Flagged leads**, not measured findings. No child was restarted.

**Smallest remedy:** stable recipient-generation routing, explicit queued/triaged states and
a short supersession checkpoint before action. Do not relay through Test/Security merely to
manufacture an addressable parent, and do not treat a failed read as proof that a working
write endpoint is dead.

### Case 4 — Actor availability and notification ownership

| Time | Concrete T evidence | Classification |
|---|---|---|
| Sep 15 23:54:55.347 | `de34aa46-907b-4702-a520-f2560c29cae4`, read of closer ed7d…, `since_turn:94`: 400 context window exceeded after five retries | Verified model failure; “94” is requested start index, not independently verified total completed turns |
| Sep 16 02:26:52.519 | `c7f986cf-79e2-436c-aa06-b2c110fb280c`, read of watcher 17c3…, HTTP unexpected EOF | Verified model-request failure, not host-process death |
| 02:32:09.244 | foreground-001 background start receipt | Process started; no consumption proven by that alone |
| 02:32:56.182 | `13a09aa1-b569-4515-b307-a6dad4643396`, `read_powershell` foreground-001 | Actual foreground output readback |
| 02:36:10.274 / 02:36:30.985 | foreground-002 start / `1f739930-ef3e-4a23-a115-15ee74bb137a` readback ending WATCH-EVENT and completed exit 0 | Second witnessed completion/readback path |

The bounded trace supports actual foreground readback, not an always-on wakeup SLA.
The supplied “27 turns” and failed resume of watcher 17c3 remain leads not independently
counted in this sample. Claude quota exhaustion is **user-reported**; it is not a process-death
measurement. Earlier scratchpad stdout and the static-nonce adapter's dispatch/exit are
not promoted into end-to-end delivery proof. No consumed-state reset/resubmission was done.

O explicitly says “stdout alone is not supervision,” folds all requests through official
functions, publishes event/state/ready files, bounds inputs at 600 files/48 MiB and
90 seconds per snapshot, and initially labels callback UNVERIFIED. It is an observer over
the filesystem record, not AI-DE board enrollment. T contains a contemporaneous statement
that `AIDE_SESSION/AIDE_CONTRACT_LOG` were absent; no board identity is inferred.

`coord-core.py:1549–1596` drops a session from the **active fold** after 8 hours without a
qualifying ledger event, or a session-end record. That is separate from a 300-second edit
lease (900-second ordinary cap), and from the product watcher's default 30-second stale
threshold. Markdown liveness can remain unchanged. Neither active-fold disappearance nor
an expired lease authorizes another writer or proves an existing process stopped.

### Case 5 — False actor attribution and a ruled-out concurrency cause

`tests/AiDe.App.Tests/DesktopHold.cs:36,43,50–59` hard-codes Grok in agent/from/session
and the old Claude recipient. `SolutionTreeChordTests.cs:28,39` emits
`grok-uv-ctrl-enter`. Thus a test invoked by another suite can produce a “Grok” notice.
This static mechanism is **Verified**, and not evidence that Grok independently ran.

Q `req-01M2M4DHWZ4Q8FAZSXV2VZER0E` originally describes apparent slot overlap.
Its sampled resolution says the conductor did not launch PID12744 and identifies the
hard-coded announcer. T at **03:43:54.036** reports child12744 within testhost7952/canonical7888;
at **03:46:46.709** the foreground reads source and native receipt timestamps.

The recorded failed native query at **03:27:53.4013825** precedes the START at
**03:29:33.3644676** by **99.9630851 s**. That later START is therefore ruled out as the
cause of that earlier query failure. Process ancestry is corroborated by a saved tool
result/report, not a fresh live process census. The actual native failure cause is outside
this report and remains unresolved; no qualification rerun or process termination occurred.

### Case 6 — Resource-slot requests versus grants and work admission

The starting lead grouped four requests into one P1-02 slot; **the actual fold contradicts it**:

* `req-01M2KR9M0180MSWTCYZ2DW77SM` and `req-01M2KRV3S1EPSNG3V2VGN4V83B`
  resolve at **00:15:12.123/180** to **one SLOT-CODEX-P1-01**, candidate `fe95be86…`.
  Measured request-to-grant intervals: **817.915 s** and **244.756 s**.
* `req-01M2M3QKXAHRTB643718QYH74C` and `req-01M2M3QKYWFQNJ6CHJGQQ78V06`
  resolve at **03:24:08.090/145** to **one SLOT-CODEX-P1-02**, candidate `e6aed085…`.
  Intervals: **160.882 s** and **160.886 s**.

Four request rows correspond to **two distinct run grants, each copied twice**, not four
runs and not one run. The latter pair addresses the foreground identity and watcher alias.
Grants cite checked candidate/process state, not generic work-area ownership.

Admission, exact-file grant, ready candidate, current checked exclusive slot, START, END,
and RELEASE are separate predicates. A run token must consume a slot at most once.
An ACK-only request must not initiate another qualification run. A stale “slot required”
reply can be a supersession/consumption problem, but repetition alone does not prove it.

**Over-ceremony hypothesis:** measured sample includes two notice rows, two explicit handshake
rows, three identical reset copies, and two duplicate-address slot pairs. This supports
avoidable rereading/correlation work. It does **not** establish a programme-wide cost
percentage or that all small author allocations are wasteful. Coherent units with one
receipt may reduce overhead; unrestricted allocations or automatic work transfers are
not justified by these counts.

## 4. Existing board, wire and reader paths

US-4 in `docs/specs/agentic-watcher-substrate.md:343–358` requires repository scope, provenance,
existing-parent replies/ACKs, explicit write failure and quarantined content. ADR-0020 binds
identity at registration. ADR-0023 says watcher facts extend the shared store and views
are derived, rather than a rival repository truth.

Actual paths reviewed:

* **Repository queue:** `coord-core.py:405–442,1786–1828` resolves the primary checkout,
  appends request-add/request-resolve to `.agents/requests.jsonl`, sorts by producer `at`,
  then folds by request ID. A resolution is free text; it has no typed parent, contract
  acceptance hash or dedicated recipient-generation field.
* **Board write:** `loomkeeper/1` contract log (`CoordContractWriter`) ->
  `CoordContractLog.ReadDirectory` -> parser -> `InjectedContractIngest` ->
  registered session's capability/repository -> `MessageBoardService` -> observation store.
  `BoardMessage` carries ID, repo, author/trust, parent, content, quarantine/injection flags,
  tombstone, recorded time and seq. An unknown session is dropped; missing/invalid content
  is quarantined; unknown same-repository parents are refused.
* **Board read:** `BoardPublisher` replaces `board/board.json` via temporary file/move,
  retaining newest 200 non-tombstoned messages, with total/showing disclosure.
  `BoardTools.Read` reads the bound repository, default 50/max 200 and optional `sinceSeq`;
  unreadable is distinct from empty. `BoardTools.Post` writes the contract log, not the store.
  The UI `WatcherBoardQuery` reads all messages, while `WatcherBoardRow` drops message/parent
  identity from its display row and has no “answered/awaiting revision” thread state.
* **Runtime:** `WatcherHost.Open:51–73` uses the provided contract-log directory and a
  SQLite `watcher.db`; it also has a separate audit-episode import. That import is not
  evidence of request/response delivery. The reviewed parser recognizes no request-add
  or request-resolve event. No automatic `.agents/requests.jsonl` bridge was found on this
  inspected path. A broader adapter elsewhere is not categorically disproved.

Thus the two JSONL mechanisms are **separate formats and paths**, not interchangeable
because both contain a field called `contract`. In the queue that field is an arbitrary
seam/topic label; `loomkeeper/1` is a wire version.

### Proposed single source of truth

**Human decision required:** retain `.agents/requests.jsonl`, through official coord tooling,
as the authoritative cross-harness coordination event stream. Extend its schema compatibly;
do not create `acks.json`, a watcher decision database, or a new ownership table.
Project the same stable events into the existing AI-DE board. For these coordination threads,
board/MCP responses must append through the same canonical event path; do not dual-write
independent dispositions. Native non-coordination board content need not be migrated merely
to repair coordination. Compatibility mapping must label its source and retain original IDs.

The board store is a rebuildable read model for these events. Receipt uniqueness/checkpoint
must commit atomically with its projection; replay must be harmless after crash/restart.
Producer append receipt does not assert SQLite visibility or model arrival. Adapter outage
must leave the canonical thread readable through the filesystem fallback.

Ownership remains **only `session-contracts.md §2`**, with explicit scoped rulings/grants
referenced by immutable identity. A message can carry a request, evidence or an authenticated
grant reference; it cannot independently mint authority. New work transfers require the
**actual human**, never silence, quota exhaustion, a budget, or an Owner persona.

### Typed state: orthogonal facts, not one “ACK” ladder

| Fact | Who may attest | Meaning / forbidden inference |
|---|---|---|
| Durable receipt | Canonical append service | Bytes accepted under stable event ID; not a promise of power-loss fsync durability unless tested |
| Correct-conversation delivery | Harness adapter with recipient ID + generation | Arrived/queued at that endpoint; not read |
| Seen / triaged | Recipient | Read and classified, next action/owner/pause reason recorded |
| Semantic response | Recipient, tied to initiating request | Answer, changes-requested, rejected, needs-human, or unable; question no longer “unanswered” |
| Exact contract accepted | Each required authorized peer | Same full content hash plus artifact/repo/revision and proposal ID; superseding proposal invalidates applicability of old acceptance |
| Ready to execute | Assigned executor | Acceptance, scope grant, candidate and prerequisites all hold; does not itself grant a slot |
| Started | Executor with one checked run token | Consumes that token once; duplicate delivery returns the same run receipt |
| Completed / released | Executor/scheduler with evidence | Outcome and resource release are distinct; failure is still an outcome |

Minimum proposed envelope: schema version, stable event/message/request/thread IDs,
causation/in-reply-to, sender/recipient/session generation, canonical repo, producer sequence,
receiver recorded time, event type and typed disposition, proposal artifact plus full hash,
supersedes, scope/grant/run references, and payload hash. Human-authenticated grants remain
references to the authority record, not trust in prose. Exact field spellings are **not**
approved protocol/API names. No cross-machine wall-clock order is a causality guarantee.

Retry **transport only** with a stable key and bounded backoff; conflicting payload under
the same key is an explicit error. Never retry a semantic decision or run as if it were a
lost packet. Unknown parents and unsupported versions need bounded pending/quarantine plus
visible negative receipt, not silently invented parents. Recover via cursor/gap handling,
not “latest 200 means everything.” Bound producer queue admission, pending parents, in-flight
notifications and retries; reject/backpressure explicitly instead of dropping obligations.

### Conditional service-level objectives — all targets PROPOSED

No percentile or availability was measured. The table is a pilot design proposal, not a
vendor guarantee or a promise that an idle/quota-limited model will answer.

| Objective / owed by | Proposed target and measured clock | Preconditions / pause / escalation |
|---|---|---|
| Append receipt / local coord service | p95 <= 1 s, local monotonic accept-to-write-result | Service runnable, writable local storage, bounded valid record; explicit failure instead of ACK on timeout |
| Projection / board adapter | p95 <= 30 s, canonical sequence visible to projection; duration measured by one local observer | Adapter healthy and enrolled; expose lag/gap immediately; unavailable adapter pauses this SLI but outage remains counted separately |
| Conversation delivery / harness adapter | next supported turn boundary, report overdue after 2 minutes | Correct live generation, tool connectivity; RUNNING queues are not interrupted. Unavailable/unknown endpoint surfaces to foreground/human, not another writer |
| Triage / recipient | first resumed turn, <= 5 minutes of **available** execution time | Model budget/quota/context and auth/tool availability. Publish pause reason and next checkpoint; no unconditional wall-clock promise |
| Semantic answer / assigned peer | negotiate per request at triage | Technical analysis may be deferred; provide a next update checkpoint, not an invented universal five-minute design SLA |
| Exact hash acceptance / required peers | no timeout-to-acceptance | Changes-requested/rejection/needs-human valid outcomes. Timeout surfaces outstanding obligation |
| Run completion/release / executor + scheduler | workload-specific deadline set in checked slot | Record START/END; expired lease only removes validity of future grants, not proof the old process stopped. Reconcile actual process identity before regrant |

Log both wall-clock timestamps and monotonic duration where one process measures an interval.
Keep total elapsed and paused duration separately; do not “improve” availability by discarding
outages from all metrics. The current foreground or actual human owns escalation when the
recipient is unavailable; no successor ownership is automatically assigned. SLO breaches
surface evidence and a choice, **never approval**.

## 5. Class/sibling sweep and disconfirmation

### Inert actual-code diagnostics (executed)

These are diagnostic controls, not fixes. Load `coord-core.py` with `importlib` in the owned
tree and supply only in-memory dictionaries; no command writes a live request or ACK:

```python
add = {"kind": "request-add", "id": "owned-q", "at": 1}
resolve = {"kind": "request-resolve", "id": "owned-q", "at": 2,
           "resolution": "changes requested"}
reply = {"kind": "request-add", "id": "owned-reply", "at": 3,
         "reason": "answers owned-q"}
f = coord.fold_requests
assert f([add, reply])[0]["status"] == "open"
assert f([add, resolve, reply])[0]["status"] == "resolved"
assert not [r for r in f([add, resolve]) if r["status"] == "open"]
assert f([add, resolve, add])[0]["status"] == "open"
assert f([resolve, add])[0]["status"] == "open"
assert f([add, resolve, resolve])[0]["status"] == "resolved"
```

**Result:** 6/6 assertions PASS, proving existing behavior. The last three distinguish an
idempotent repeated resolution from add-redelivery reopening a request and resolve-before-add
being ignored. The duplicate-add test supplies that order directly; the official reader's
wall-clock sort can produce equivalent ordering for a rewritten retry/skewed event.
Byte-identical events sorted back to their original timestamp need not exhibit reopening.
No duplicate-add/skew incident is claimed in the live sample. A desired reliable fold would
fail the “never reopen on transport replay / preserve causal resolution” expectations today.
No patched implementation was compared, so no production regression control is “controlled.”

Reproduction loader (run from the owned tree, then execute the assertions above; no writes):

```python
import importlib.util
import sys
from pathlib import Path
script = Path("docs/ai-forward-pack/scripts/coord-core.py")
sys.path.insert(0, str(script.parent))
spec = importlib.util.spec_from_file_location("coord_diagnostic", script)
coord = importlib.util.module_from_spec(spec)
spec.loader.exec_module(coord)
events, errors = coord.read_request_events(Path(r"C:\Projects\ai-de\.agents"))
rows = coord.fold_requests(events)
print(len(events), len(rows), sum(r["status"] == "open" for r in rows), len(errors))
```

Captured investigator output: `436 316 198 0`; diagnostics printed `PASS 6 inert actual-fold
assertions`. **The argument is the `.agents` metadata root**, not `C:\Projects\ai-de`.
The CLI equivalent is `python docs/ai-forward-pack/scripts/coord-core.py request list
--json --status all` from the registered worktree. The independent reviewer checked raw
436/zero malformed records but did not independently reproduce folded counts or assertions;
its final invocation used the wrong root and returned zero, and its 8-call budget then ended.

| Candidate class / sibling | Evidence | Disposition |
|---|---|---|
| DC-045 stale read after successful write, cross-channel instance | Case 1 explicit confession + actual fold | Confirmed mechanism; broader UI family, not claimed identical UI event bug |
| TWO-REGISTERS-OF-ONE | Queue/board formats and absent common disposition | Design hazard confirmed; avoid creating an additional authority copy |
| DC-024 ledger versus actual availability | 8h fold vs failed model, unchanged Markdown | Confirmed observation limitation; no cleanup/reassignment attempted |
| Fixed actor label in a reusable test helper | DesktopHold and SolutionTreeChordTests | Confirmed sibling notices can be misattributed; original query failure not explained |
| Board re-read non-idempotent side effect | Pump -> ApplyBoardPost -> fresh GUID -> INSERT | Static replay scenario; C# execution proof outstanding |
| DC-067 session-end mapping without store-end | Apply session-end now calls `_host.EndSession` before removing mapping | Ruled out on inspected source; do not rediscover the repaired old instance |
| “No board read path exists” | BoardPublisher and BoardTools both present | Ruled out; missing enrollment/routing is not absent reader implementation |
| “All four qualification asks are P1-02” | Actual resolutions distinguish P1-01 and P1-02 | Ruled out |
| “Quota means dead process / timeout means grant” | No such evidence or authority rule | Rejected inference; human wait remains legitimate |

Scoped marker harvest: `src/AiDe.Core/Watcher`, `src/AiDe.Mcp`, and
`tests/AiDe.Core.Tests/Watcher` contained two `simplify:` markers:
`WatcherObservationStore.cs:203` (unbounded in-memory skeleton; persistence upgrade trigger)
and `SqliteWatcherObservationStore.cs:12` (one locked connection; reference-scale ceiling).
No `assume:` marker was found in that bounded sweep. SQLite is already wired by
WatcherHost; do not label the in-memory marker as proof the running product is unbounded.
Scale-trigger exceedance was not measured. The pump's “idempotent” comment is an **unmarked
contract assumption broader than the registration test**. The 200-item reader caps explicitly
say their basis is not recorded; they bound context, not delivery backlog or thread completeness.

Register additions use existing classes, not invented numeric IDs. Controls recorded for this
instance are diagnostic/register-only. Production prevention remains proposed below.
Potential pack-wide learning: separate receipt/triage/acceptance/authority and never describe
a registry fold as actor health. Upstream promotion is deferred to human review; no new
`/extendaibundle` programme was launched.

## 6. Phased CODE + TEST repair plan

All phases require actual-human approval before implementation. No row is a current grant.

| Phase / priority | Small coherent code + tests | Failure eliminated / validation | Depends on / rollback |
|---|---|---|---|
| P0 / urgent design gate | Agree typed obligation/disposition and immutable authority references; contract examples for positive/negative/superseded replies | Tests must reject ACK-as-approval, old-hash acceptance, unknown generation and unauthorized transfer; human signs exact scope | Human + narrow Security/identity review; no runtime effect to roll back |
| P1 / high | Extend official coord fold with correlated immutable response events and actionable thread read; keep legacy list compatibility | Failing-first actual-code tests: reply updates initiating unanswered view; changes-requested not accepted; all-status resolution visible; duplicate/reordered events do not reopen; conflicting key refused | P0; disable new projection/read mode, preserve all log events |
| P2 / high, BLOCKER prerequisite | Idempotent board import/source keys, atomic effect/checkpoint, bounded retry/parent backlog, cursor-complete reads | Register+post+two pumps -> one message; crash after DB write before ACK; restart; skewed clocks; parent-late; >200 unread; tombstones; refused version; two service instances allocate N+1 with reader between commits must not skip either fact. Limit-plus-one producer/pending-parent/retry saturation and recovery, including permanently missing parent: explicit backpressure/refusal, accepted obligations preserved, bounded memory/attempts | P0/P1 + Data & Persistence and independent DS clearance; additive migration with tested rollback, disable importer, no destructive compaction |
| P3 / high | Harness adapters for foreground/background Copilot, Codex, Grok and capped Claude: stable endpoint generation, queued/triaged receipt, negative availability, bounded wake/poll | Deterministic fake transport: running turn defers; unavailable/failed model; sibling-read restriction; redelivery at most one semantic action; in-flight limit-plus-one, recipient outage and recovery preserve obligations. Separately, actual-human-approved bounded real-endpoint conformance for EACH supported adapter proves endpoint generation, correct-conversation arrival, recipient consumption and supported post-turn wake; fake tests cannot clear that gate | P1/P2, vendor/local supported contracts; disable adapters and use canonical manual pull |
| P4 / high | Launcher-owned actor/run provenance and slot-token idempotency; independent release predicate | Same helper under two callers attributes each correctly; replayed request starts one run; expired lease cannot regrant over live holder; END failure still releases only with evidence | Human scope grant; preserve old notices as historical evidence, disable new emitter if needed |
| P5 / measured follow-up | Contract conformance tests across queue/board/MCP/UI and low-volume SLI capture | Same events produce same obligations; timeout never accepts; availability/pause and lag visible; deterministic corpus prevents duplicate semantic retries | P1–P4; reporting-only rollback, no authority effects |

Before any storage choice, **Data & Persistence must co-review** event-key uniqueness,
projection transaction, restart checkpoints, bounded pending parents and expand/rollback.
Before any authority adapter, **Security must independently review** binding and human-grant
verification. These are unperformed production design gates, not approvals hidden in this
report. No GUI/App/full qualification, package install, SDK nonce, source repair, ownership
transfer or peer-worktree edit occurred.

## 7. Validation, independent gate and residuals

Executed: official all-status queue fold (zero parse errors); six inert actual-fold assertions;
bounded chronological tool/request inspection; pinned board/source reads; marker/sibling sweep.
Not executed: C# replay test, adapter conformance suite, native failure reproduction, latency
distribution, root-owned/sibling capability spike, filesystem power-loss durability test.

Independent review: **Test Architect — PASS-WITH-CONDITIONS, 8/8 calls**, concrete source
and primary request inspection. Three Majors accepted: real-harness conformance beyond fake
transport, concurrent cursor safety, and saturation/recovery oracles. Two Minors accepted:
contradictory 15:33 prose timestamp, and the incomplete independent count/assertion reproduction.
The report now includes those plan floors and evidence limitations. No follow-up clearance
was obtained; the reviewer did not run tests/builds or modify files. The author's inferred
board-delivery blocker remains uncleared. Data/Security review is still a future production
gate, not part of this conditional report review.

### Documentation/audit closeout

Official audit entry: `al-01M2NGFQHFNBSSQ54JJ3W2C81N` in
`docs/audit/audit-log.jsonl`, with the original delegated prompt retrieved locally and
recorded verbatim, outcome **partial** (conditional review, unresolved production gates).
No change-log decision accepts the proposed protocol; this report has no normative effect.

Executed in the owned tree after that append:

* `tools/regenerate-derived.py`: **PASS** — API, documentation bundle, site figures, audit
  rendering and docs index regenerated in official dependency order. Conflict markers,
  derived views, site figures, defect register, audit log and registry coverage all green.
* `docs-graph.py validate`: **zero defects, zero orphans, zero index drift**; 520 artifacts.
  **74 existing review-suggested flags remain**, advisory rather than failed validation.
  No unrelated flags were cleared. New report has four typed links and Docs Explorer exists.
* `audit-log.py selfcheck --session cross-harness-messaging-rca-b0d0 --gate`: **PASS**,
  one substantive turn with goal state.
* `git diff --check`: **PASS** before final closeout annotation.

Authored changes: this report plus two scoped existing-class additions in
`docs/lessons/defect-classes.md`. Official generation updates the index/audit/bundle and
six numerical regions across `site/{collaboration,index,model}.html`; those three authored
pages were individually leased before generation. No lease was claimed on JSONL registers
or derived artifacts. No source, test, observer, nonce, peer tree or production configuration
was changed. The finite investigation stops with the gaps below; it does not spend the
remaining budget on new causal theories.

Residuals / what could change the diagnosis:

* Raw child consumption markers could quantify stale-turn delay; current relay messages cannot.
* A deployed adapter with stable source-event deduplication could change the board replay finding;
  its code/version and a restart test are required, not an assurance.
* A current canonical by-thread receipt could change “awaiting response” without implying agreement.
* The native failure needs its own causal investigation; the disproved late-START theory must not
  be replaced with another plausible story.
* This report does not know every live actor's quota, model health or process state.
* Small observed request intervals do not warrant percentiles or unconditional SLA targets.

**STOP — investigation/report only. Actual human review chooses whether, and which,
repair phases proceed. Existing admitted Codex/Grok work continues under its existing
boundaries; this report grants no new work, slot, ownership or agreement.**
