---
id: api-aide-core-dispatch
title: "API: AiDe.Core.Dispatch"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Dispatch: 19 types, 34 members, 77% carrying a summary doc comment.
---

# API: `AiDe.Core.Dispatch`

**19 public types · 34 public members · 77% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `DispatchBeginResult`

*record* — `BoundaryDispatcher.cs`

The write-ahead half of a dispatch, as answered by whoever owns the store.

## `BoundaryDispatcher`

*class* — `BoundaryDispatcher.cs`

The durable half of dispatch, split so it can be answered across the daemon boundary.

**Remarks.** **Why this split exists.** D1 settled that terminal processes live in the shell while the
store lives in the daemon, so the two halves of a two-phase delivery are now in *different
processes*: only the shell can write to the pty, and only the daemon can make the attempt
durable. `DispatchService` does both in one call and remains correct in-process; this
is the same choreography with the side effect lifted out.





**The crash window got bigger, which makes the write-ahead matter more, not less.**
In-process the window between "attempt recorded" and "outcome recorded" was a pty write. Across
the boundary it is a pty write plus two IPC round trips plus the possibility that the shell dies
while the daemon lives. Every one of those leaves a `Pending` row, which
`SweepPendingToUnknown` resolves to an honest
`DeliveryUnknown` rather than a missing row a retry would read as
"never sent".

| Member | Summary |
|---|---|
| `DispatchBeginResult Begin(DispatchCommand command)` | Phase 1 — make the attempt durable. Runs where the STORE is. |
| `DispatchReceipt Finalize(string dispatchKey, DispatchState state, string? errorCode)` | Phase 2 — record the outcome of the side effect the caller performed. Runs where the STORE is. |
| `(DispatchState State, string? ErrorCode) Outcome(PtyWriteResult result)` | Maps a pty result onto the durable outcome. One place, so both hosting modes agree. |
| `Task<DispatchReceipt> BeginAndWriteAsync(` | The caller's side of the choreography: begin, write to the session it owns, finalize. |
| `AttemptBeginResult BeginAttempt(AttemptRow attempt)` | G0 — make the attempt durable, or return the attempt that already holds this key. |
| `AttemptAppendResult AppendAttemptOutcome(OutcomeRow outcome)` | G1–G3 — append one outcome event to an attempt, unless the attempt may not take it. |
| `AttemptReceipt? ReadAttempt(string dispatchKey)` | The attempt row and every outcome event on it, in total order; null when the key was never attempted. |
| `AttemptAppendResult WithdrawAttempt(string dispatchKey, CallerPrincipal caller)` | The operator's withdraw of a never-launched attempt (`WithdrawAttemptAsync`). |
| `AttemptSweepResult SweepAttempts(AttemptSweepRequest request)` | Resolves every abandoned native attempt to `DeliveryUnknown` with a cause. |

### `DispatchBeginResult Begin(DispatchCommand command)`

Phase 1 — make the attempt durable. Runs where the STORE is.

**Remarks.** The session-binding check deliberately does **not** happen here: this process has no
session to check against. It is the caller's obligation, asserted in
`BeginAndWriteAsync` before this is ever called, because a check performed against
a value the caller also supplied would prove nothing.

### `DispatchReceipt Finalize(string dispatchKey, DispatchState state, string? errorCode)`

Phase 2 — record the outcome of the side effect the caller performed. Runs where the STORE is.

**Remarks.** Finalizing a key that has already been finalized is a no-op returning the existing receipt,
not an error: a retried finalize after a lost reply must not turn a delivered prompt into a
failure.

### `Task<DispatchReceipt> BeginAndWriteAsync(`

The caller's side of the choreography: begin, write to the session it owns, finalize.

- **`readiness`** — Whether the session is known to be waiting for input. **Refused unless `Ready`**, and refused BEFORE anything is made durable.

**Remarks.** Written once, here, and given the two durable phases as delegates — so the shell talking
to a daemon and a core talking to itself run **the same ordering**. A second copy of this
sequence for the remote case is how the two modes would drift into disagreeing about when the
attempt becomes durable.

### `AttemptBeginResult BeginAttempt(AttemptRow attempt)`

G0 — make the attempt durable, or return the attempt that already holds this key.

**Remarks.** Read-before-write inside ONE writer transaction: the existence check and the insert cannot be split by another
writer, because the writer holds the store's single-writer semaphore. Any prior attempt wins, even after the
epoch moved — exactly `Begin`'s rule, so a retry after a lost reply returns the receipt.

### `AttemptAppendResult AppendAttemptOutcome(OutcomeRow outcome)`

G1–G3 — append one outcome event to an attempt, unless the attempt may not take it.

**Remarks.** Refused (not committed, never thrown): a second `Dispatched`; a
`Dispatched` after a terminal — a sweep that resolved the attempt first wins, and no prompt may follow it
(design, DSA Blocker 1); a second terminal — the first stands (ADR-0041 §2). The checks run in the writer
transaction; the partial unique indexes are the net, and their violation maps to the same answer.

### `AttemptAppendResult WithdrawAttempt(string dispatchKey, CallerPrincipal caller)`

The operator's withdraw of a never-launched attempt (`WithdrawAttemptAsync`).

**Remarks.** One writer transaction: the effect (`Rejected{never_dispatched}`) and its command receipt commit together or
not at all. Only an attempt with no `Dispatched` row and no terminal is admitted, so a retried withdraw —
whose first try already wrote the terminal — commits nothing and reads back the original outcome.

### `AttemptSweepResult SweepAttempts(AttemptSweepRequest request)`

Resolves every abandoned native attempt to `DeliveryUnknown` with a cause.

**Remarks.** One writer transaction: the candidates are selected, the cause computed and the rows inserted inside it (design,
DSA 4) — never the PTY sweep's read-then-write. The caller's live set is excluded; so is a queued successor whose
predecessor has no `ResponseComplete` (it folds to "Queued — awaits you"). PTY rows are not candidates.

## `AttemptErrorCodes`

*class* — `BoundaryDispatcher.cs`

The stable codes of the native-route attempt gate (docs/design/workbench-loops-wl-1a.md).

| Member | Summary |
|---|---|
| `string StalePrepare = "AIDE-ATTEMPT-STALE-PREPARE"` | G1: the envelope's current projection is not the one the attempt was prepared from (AC-E04b). |
| `string NotDispatched = "AIDE-ATTEMPT-NOT-DISPATCHED"` | The attempt ended before its `Dispatched` row committed: no prompt was sent, so Retry is safe. |
| `string SecondTerminal = "AIDE-ATTEMPT-SECOND-TERMINAL"` | A second terminal was offered for an attempt that already has one; the first stands. |
| `string HostNotSole = "AIDE-ATTEMPT-HOST-NOT-SOLE"` | Another attempt host already holds this workspace; a second one can neither dispatch nor sweep. |

## `AttemptKey`

*class* — `BoundaryDispatcher.cs`

The native-route dispatch key (ADR-0041 §1). One home: the host and the binding compute the same key.

| Member | Summary |
|---|---|
| `string For(string envelopeId, string projectionSha, string? queuedAfterDispatchKey, string? parentDispatchKey)` | `sha256(envelope_id ‖ projection_sha ‖ queued_after_dispatch_key ‖ parent_dispatch_key)`, lower-case hex, 64 characters. Deterministic from the prepared envelope, so a retried click or a lost reply recomputes it. |

## `AttemptReceipt`

*record* — `BoundaryDispatcher.cs`

One attempt as the store holds it: the attempt row and its outcome events in total order.

**Remarks.** Every member past the three fields is DERIVED on read (DM: derive, don't store) and never crosses the wire.

| Member | Summary |
|---|---|
| `string WithdrawCommand = "attempt.withdraw"` | The `command_type` of the operator's withdraw; its `command_id` is the dispatch key. |
| `bool IsWithdrawn` | Withdrawn by the operator: `Rejected{never_dispatched}` AND the completed withdraw command — never the terminal alone. |
| `IReadOnlySet<DispatchState> TerminalStates = new HashSet<DispatchState>` | The terminal states — exactly the set the store's `ux_outcome_one_terminal` index names. |
| `string DispatchKey` | **(gap)** |
| `OutcomeRow? Dispatched` | The one `Dispatched` row, or null when the prompt was never launched. |
| `OutcomeRow? Terminal` | The one terminal row (the lowest ingress sequence wins, ADR-0041 §2), or null while the attempt is open. |
| `bool IsPending` | An attempt row with no outcome event yet — the PTY path's meaning of Pending, kept. |

## `AttemptBeginResult`

*record* — `BoundaryDispatcher.cs`

*No doc comment on this type.* **(gap)**

## `AttemptAppendResult`

*record* — `BoundaryDispatcher.cs`

*No doc comment on this type.* **(gap)**

## `AttemptSweepRequest`

*record* — `BoundaryDispatcher.cs`

*No doc comment on this type.* **(gap)**

## `AttemptSweepResult`

*record* — `BoundaryDispatcher.cs`

*No doc comment on this type.* **(gap)**

## `DispatchErrorCodes`

*class* — `DispatchService.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `string GenerationChanged = "AIDE-DISPATCH-GENERATION-CHANGED"` | **(gap)** |
| `string DeliveryUnknown = "AIDE-DISPATCH-DELIVERY-UNKNOWN"` | **(gap)** |
| `string EpochStale = "AIDE-AUTH-EPOCH-STALE"` | **(gap)** |
| `string SessionUnknown = "AIDE-DISPATCH-SESSION-UNKNOWN"` | **(gap)** |
| `string WriteFailed = "AIDE-DISPATCH-WRITE-FAILED"` | **(gap)** |
| `string SessionNotReady = "AIDE-DISPATCH-SESSION-NOT-READY"` | The session does not report readiness, or reports that it is not ready. |

### `string SessionNotReady = "AIDE-DISPATCH-SESSION-NOT-READY"`

The session does not report readiness, or reports that it is not ready.

**Remarks.** A refusal, not a failure: nothing was attempted and no receipt exists, so a retry once the
session is ready is a first attempt rather than a duplicate.

## `DispatchCommand`

*record* — `DispatchService.cs`

A user-confirmed request to transfer one immutable prompt revision to one session.

| Member | Summary |
|---|---|
| `string DispatchKey { get; } = Convert.ToHexStringLower(` | Derived from `CommandId`, so the command and dispatch idempotency namespaces are one. Two namespaces would let a retry miss the receipt it was meant to find. |

## `DispatchService`

*class* — `DispatchService.cs`

Prompt delivery under a write-ahead two-phase receipt (ADR-0010).

**Remarks.** Pattern: Write-Ahead Receipt / Two-Phase Delivery (LOA P8 — idempotency at side-effect boundaries).
A terminal cannot atomically acknowledge a write and persist a store receipt, so recording the
receipt *after* the write leaves a crash window in which no receipt exists: the state reads
`NotRecorded`, a protocol-conformant retry treats it as never-sent, and a duplicate consequential
prompt lands in the agent session. Committing the attempt first turns that window into an honest
`DeliveryUnknown` instead.

| Member | Summary |
|---|---|
| `Task<DispatchReceipt> DispatchAsync(` | Returns the existing receipt if this dispatch key was ever attempted — including a still `Pending` one — and otherwise performs the two-phase delivery. Never re-executes. |
| `int SweepPendingToUnknown()` | Recovery. Resolves every attempt that never recorded an outcome to `DeliveryUnknown`. Run at core startup — this is what converts a crash window into an honest state instead of a missing row that a retry would read as… |
| `DispatchReceipt? ReadReceipt(string dispatchKey)` | **(gap)** |

## `struct`

*record* — `ITerminalSession.cs`

One read of the session's output, and whether anything was dropped to produce it.

**Remarks.** `Truncated` rides on the chunk rather than being a session-level flag on purpose: it
says "bytes were dropped immediately before this chunk", which is where a renderer needs to draw
its gap marker. A session-level flag would say only that loss happened at some point, which
cannot be rendered anywhere in particular.

## `SessionActivity`

*enum* — `ITerminalSession.cs`

Advisory session state. Never agent acceptance (ADR-0007) — a terminal cannot tell us an agent
agreed to anything, only that a process is or is not producing output.

## `SessionExit`

*record* — `ITerminalSession.cs`

How a session ended. `ExitCode` is null when the process was killed.

## `ITerminalSession`

*interface* — `ITerminalSession.cs`

The terminal seam. Phase 1 substitutes a fixture session; Phase 2 substitutes a real ConPTY
runtime behind this same contract, so the swap is a substitution rather than a redesign.

**Remarks.** **Phase-2 amendment.** The Phase-1 shape was **write-only**, because the fixture
recorded bytes and returned and nothing ever needed to read. A real terminal's output is the
entire point — the renderer subscribes to it, the OSC parser reads it, and the resource budget is
defined over it. `WriteAsync` and the generation fence are unchanged, so the
write-ahead dispatch built on them is untouched.





**Output is pull-based, not an event.** An event would let a fast-producing process drive
unbounded work on whatever thread raised it — exactly the sustained-1 MiB/s case the architecture
budgets for. A bounded channel makes backpressure representable and truncation a *state*
rather than a crash.





Every implementation must satisfy the shared conformance suite (D7). With two
implementations in play, tests written against the fake prove something about the fake unless
that suite exists.

## `SessionReadiness`

*enum* — `SessionReadiness.cs`

Whether a session can be given a prompt right now.

**Remarks.** Deliberately three-valued. "Not ready" and "we cannot tell" are different situations with
different correct responses, and collapsing them is how a prompt gets sent into a dialog box.

## `ReadinessEvidence`

*enum* — `SessionReadiness.cs`

How a session's readiness was established. Not all evidence is equal.

## `SessionReadinessPolicy`

*class* — `SessionReadiness.cs`

Establishes whether a session is ready for a prompt, from evidence rather than from hope.

**Remarks.** **ADR-0007 already requires readiness evidence** before an adapter may claim agent
acceptance. What was missing was the other half: what to do when there is none. Until this, a
prompt was written and reported `PtyWriteAccepted` regardless — which is true about the
bytes and misleading about the outcome.





**Shell integration is the only readiness evidence that exists today.** OSC 133 with the
session nonce is what makes `Ready` a claim rather than a guess; a
session without it has an activity value derived from output timing, which is not the same thing
and must not be treated as one.

| Member | Summary |
|---|---|
| `SessionReadiness Evaluate(ITerminalSession session, bool hasReadinessEvidence)` | Readiness for , given whether its shell integration is active. |
| `SessionReadiness Evaluate(ITerminalSession session, ReadinessEvidence evidence)` | Readiness for , given what kind of evidence exists. |
| `string Explain(SessionReadiness readiness)` | The sentence a user is shown when a dispatch is refused. |

### `SessionReadiness Evaluate(ITerminalSession session, bool hasReadinessEvidence)`

Readiness for , given whether its shell integration is active.

- **`hasReadinessEvidence`** — True only when something authenticates the session's own claim about its state — today, the OSC 133 nonce. Derived output timing does not count: a quiet agent mid-thought looks exactly like an idle one.

### `string Explain(SessionReadiness readiness)`

The sentence a user is shown when a dispatch is refused.

**Remarks.** Each case says what would change it. "Not ready" resolves by waiting; "unknown" does not, and
telling a user to wait for something that will never happen is worse than telling them why.
