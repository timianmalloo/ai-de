---
id: design-workbench-loops-wl-1a
title: "Design: WL-1a — the revision ↔ attempt seam (ADR-0041 gate, write-ahead, sweep, run log, Stop, host interface)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops / WL-1a"
tags: [design, execution, attempt, idempotency, write-ahead, sweep, run-log, stop, workbench-loops, wl-1a]
links:
  - { to: adr-0041-execution-attempt-lifecycle, rel: implements }
  - { to: adr-0039-workbench-records-durable-representation, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  The detailed design of the WL-1a seam. A stateful AttemptHost owns the live registry, the gate
  choreography, queue, Stop and subscriptions, and calls GovernedRunHost.RunAsync, which stays the one
  composition root. The store-side gate (read-before-write in one transaction, partial unique indexes as
  the net, the sweep predicate) lives in BoundaryDispatcher behind an IAttemptLedger seam that the
  daemon answers over IPC. It fixes the crash windows W1–W4, the terminal mapping, the run-log line
  format and the host interface that T6, T7 and T8 build to.
---

# Design: WL-1a — the revision ↔ attempt seam

Track T5 (`wl1a-attempt-host`). Implements ADR-0041 §1–§4 and §7 for the claude-code ACP lane.
Builds on T2's v2 schema (`WorkspaceSchema.cs:366-391`), T3's `RunEvent` envelope and `TurnState`.
Consumers: T6 (folds), T7 (redaction function), T8 (App binding).

## Responsibility

One responsibility: **turn a prepared notebook revision into at most one launched execution attempt,
whose outcome is durable, honest and readable after a crash.** The host decides *whether* a run
launches (the gate), *records* each step before its side effect (write-ahead), *writes* the one
terminal outcome from its own correlated reply, and *resolves* abandoned attempts (the sweep). It does
not fold non-terminal state (T6), redact (T7), or render anything (T8).

## Grounding (quoted, Verified by reading on 2026-10-18 at base `f1688dba`)

- ADR-0041 §1: "read-before-write inside one store transaction; an existing key returns the existing
  receipt and launches nothing. The host launches only when *its own* insert of the `Dispatched`
  outcome row committed (partial unique index)".
- ADR-0041 §1 order: "attempt row (`Pending`) → `AcpEngineProcess.Start` → worktree provision
  (`WorktreeProvisioned` row) → `session/new` → `Dispatched` row → `client.PromptAsync` → terminal row".
- Schema (T2): `ux_outcome_one_dispatched` (`WHERE state = 'Dispatched'`) and `ux_outcome_one_terminal`
  (`WHERE state IN ('ResponseComplete','Failed','Interrupted','DeliveryUnknown','Rejected')`),
  `cause` CHECKed to the six `DeliveryUnknownCauses` (`WorkspaceSchema.cs:374-380`). The attempt row
  has **no `envelope_id` column**; `trg_dispatch_attempt_binds_revision` refuses an attempt whose
  `(draft_id, revision_no)` has no revision.
- `GovernedRunHost` is a `static class` (`GovernedRunHost.cs:48`); `run_id` is minted inside `RunAsync`
  (`:148`); its other caller is `ConductorEntry.cs:80`, called positionally; `CompositionRootLedger`
  counts `governed-run.compose` activities.
- The PTY gate: `BoundaryDispatcher.Begin` reads first and returns `AlreadyAttempted` (`:49-72`); the PTY
  sweep `DispatchService.SweepPendingToUnknown` runs at **daemon** start (`WorkspaceCore.cs:108`) over
  `StoreReader.PendingDispatchKeys` = every attempt with no outcome row (`StoreReader.cs:687-702`).
- IPC today: `dispatch.begin` / `dispatch.finalize` (`WorkspaceOperations.cs:162-163, 258-268`),
  `IWorkspaceDispatch` (`IWorkspaceDispatch.cs`), one implementer `WorkspaceClient` (`:39`). The App
  reaches the store only through the daemon (D1).
- `AcpPeer.RequestAsync` throws `AgentPlaneException` `EngineReturnedError` on an `error` reply,
  `EngineRequestTimedOut` on its deadline, `EngineStreamEnded` when stdout closes
  (`AcpPeer.cs:445-447, 620-627, 653`).
- The envelope store is a per-session JSONL with one exclusive writer held by `ComposerSendGate`
  (App/Workbench, T8; `EnvelopeStore.cs:36`, `ComposerSendGate.cs:139`).

## Conformance to the architecture — and one finding

- **Finding F1 (drift, resolved here):** the PTY sweep would resolve a *native* attempt. A native
  attempt has no outcome row while it is `Pending`, so `PendingDispatchKeys` picks it up, and the daemon
  runs that sweep at its own start — while the App host may still hold the run live. It would write
  `DeliveryUnknown` with no `cause` onto a live attempt, and ignore the queued-successor exclusion.
  Disposition: `PendingDispatchKeys` is narrowed to PTY rows (`route_fingerprint IS NULL OR = 'pty'`);
  native attempts are swept only by the host's sweep (§7). Red test first.
- **Finding F2 (placement):** ADR-0041 §2 says the `consumed` row is "written by the same host path and
  by the sweep". The envelope store's single writer is `ComposerSendGate` in T8's window. Disposition:
  the host *orders* it (the terminal commit resolves `Completion` first) and T8's binding writes the
  `consumed` row; for swept attempts T8 finds the envelope by **recomputing** the key from its own fold
  (derive, don't store — no `envelope_id` column is added). The ordering rule "outcome first, then
  consumed" is kept. **Crash between the two (DSA 6):** at startup T8 writes `consumed` for every
  accepted, unconsumed envelope whose recomputed key has a terminal row (idempotent, `envelope_id`-keyed),
  so no envelope stays open forever.

## Data model (DM — settled first)

No new table, column or index. WL-1a writes only T2's rows.

| Store | Grain ("one row is exactly one …") | Kind | History rule |
|---|---|---|---|
| `dispatch_attempt_fact` | one attempt to run one prepared revision | fact, append-only | immutable; a retry is a **new** row with `parent_dispatch_key` |
| `dispatch_outcome_fact` | one outcome event of one attempt | fact, append-only | immutable; at most one `Dispatched`, at most one terminal (partial unique) |
| `runs/<dispatch_key>.jsonl` | one `RunEvent` of one attempt | append-only log (node 2) | never rewritten; redacted at ingest |
| live registry (host memory) | one attempt this host process is running | not durable, by design | gone on process death — which is what the sweep reads |

- **Aggregate:** the *attempt*, root `dispatch_key`, invariant "at most one `Dispatched`, at most one
  terminal, terminal only after the attempt row". Outcomes reference the attempt by identity.
- **Derived, never stored:** the attempt's state (latest-relevant outcome; T6), `Pending` (attempt row
  with no outcome row — no `Pending` outcome row is written, as on the PTY path), duration
  (`terminal.recorded_at − Dispatched.recorded_at`), event count and bytes (from the run log),
  `envelope_id` ↔ attempt (recompute the key).
- **Measures (NFR-11):** event count and bytes out are additive per attempt; duration is non-additive.

## The shape

```
T8 binding ──IAttemptHost──▶ AttemptHost (App/Conductor, stateful, one per workspace)
                                 │ live registry · queue · Stop · relay per attempt
                                 ├──IAttemptLedger──▶ WorkspaceClient ══IPC══▶ daemon: BoundaryDispatcher (one txn each)
                                 │                    (tests: BoundaryDispatcher in-process, same code)
                                 └──▶ GovernedRunHost.RunAsync(request, token, sink, attempt)   ← the one composition root
                                          └─ AttemptRun: the in-run write points (worktree, Dispatched gate, terminal)
```

- `GovernedRunHost` **stays static and stays the one root.** `RunAsync` gains one trailing optional
  parameter `AttemptRun? attempt = null`, after `sink`, so `ConductorEntry.cs:80` is not edited (its
  comment at `:120-125` holds). With `attempt == null` the run behaves exactly as today (the headless
  entry has no envelope, so it is not an attempt). With an attempt: `run_id := dispatch_key`, the
  Dispatched gate sits between `session/new` and `PromptAsync`, and the terminal is written from the
  awaited prompt.
- `AttemptHost` is new (`src/AiDe.App/Conductor/AttemptHost.cs`; seam request
  `req-01M599H0364THVM23WJJ68R11S`). It holds what ADR-0045 §4 moves out of `SessionDocumentSurface`:
  the per-attempt cancellation, the queue, the live set.
- The store-side gate is added to `BoundaryDispatcher` — the ADR names "`BoundaryDispatcher.Begin`
  semantics", and the class already is the store-side half of a two-phase dispatch.

### `IAttemptLedger` (Core, `IWorkspaceDispatch.cs`) — the store seam

```csharp
public interface IAttemptLedger
{
    Task<AttemptBeginResult> BeginAttemptAsync(AttemptRow attempt, CancellationToken ct);           // G0
    Task<AttemptAppendResult> AppendAttemptOutcomeAsync(OutcomeRow outcome, CancellationToken ct);  // G1–G3
    Task<AttemptReceipt?> ReadAttemptAsync(string dispatchKey, CancellationToken ct);
    Task<AttemptSweepResult> SweepAttemptsAsync(AttemptSweepRequest request, CancellationToken ct);
}
public sealed record AttemptReceipt(AttemptRow Attempt, IReadOnlyList<OutcomeRow> Outcomes)
{   // derived on read, never stored
    public OutcomeRow? Dispatched { get; }  public OutcomeRow? Terminal { get; }  public bool IsPending { get; }
}
public sealed record AttemptBeginResult(AttemptReceipt Receipt, bool AlreadyAttempted);
public sealed record AttemptAppendResult(AttemptReceipt Receipt, bool Committed);
public sealed record AttemptSweepRequest(IReadOnlyList<string> LiveKeys, string DispatchedCause);
public sealed record AttemptSweepResult(int Count, IReadOnlyList<OutcomeRow> Swept, string SweptAt);
```

Implemented by `BoundaryDispatcher` (in-process) and by `WorkspaceClient` over four new IPC ops
`attempt.begin` · `attempt.outcome` · `attempt.read` · `attempt.sweep`, registered beside
`dispatch.*` in `WorkspaceOperations.RegisterDispatch` (so `Daemon/Program.cs:185` needs no edit).
`AttemptKey.For(envelopeId, projectionSha, queuedAfter, parent)` lives beside it (Core), so T8 computes
the same key from its fold.

### `IAttemptHost` (App, `AttemptHost.cs`) — the interface T8 consumes

```csharp
public interface IAttemptHost
{
    // Gate decides; the run continues in the host. QueuedAfterDispatchKey set ⇒ G0 now, launch after the
    // predecessor's ResponseComplete (one method, branching on the field — Simplifier 1).
    Task<AttemptStart> DispatchAsync(AttemptDispatch dispatch, CancellationToken ct);
    Task<AttemptStart> SendNowAsync(string dispatchKey, CancellationToken ct);   // the explicit move of a held successor (node 2)
    Task<AttemptReceipt> StopAsync(string dispatchKey, CancellationToken ct);     // §4 Stop (node 2)
    Task<AttemptReceipt?> ReadAsync(string dispatchKey, CancellationToken ct);    // "read an attempt" (brief); events via Subscribe
    AttemptSubscription Subscribe(string dispatchKey);                             // own channel + run-log catch-up (node 2)
}
public sealed record AttemptDispatch(
    GovernedRunRequest Run, string WorkspaceId, long WorkspaceEpoch,
    string EnvelopeId, string ProjectionSha, string NotebookId, int RevisionNo,
    string SessionId, long SessionGeneration,
    Func<string?> CurrentProjectionSha,              // T8's envelope fold, read at the gate
    string? QueuedAfterDispatchKey = null, string? ParentDispatchKey = null, string? RouteFingerprint = null)
{ public string DispatchKey { get; } /* AttemptKey.For(...) */ }
public sealed record AttemptStart(AttemptReceipt Receipt, bool AlreadyAttempted, Task<AttemptReceipt> Completion);
// The in-run write points RunAsync takes (Simplifier 8): a record, not a callback hierarchy.
public sealed record AttemptRun(IAttemptLedger Ledger, string DispatchKey);
```

`Completion` resolves after the terminal row committed (or with the existing receipt when nothing was
launched). Every member returns a receipt read back from the store, never an in-memory guess.
**Why an interface over one class (Simplifier 3):** T8 builds its binding in parallel and its UI tests
need a host fake with no engine; that is the only reason, and it is enough. The sweep and the live set
are `internal` (Simplifier 2): every trigger fires inside the host. `ProfileKey` / `ScopeSnapshot` are
not on `AttemptDispatch` (Simplifier 7): the columns stay null until WL-3 binds them.

**One host per workspace, enforced (DSA 7).** `AttemptHost.Open(ledger, hostLockFile)` opens
`<ws>/.aide/attempt-host.lock` with `FileShare.None`; a second host is refused
(`AIDE-ATTEMPT-HOST-NOT-SOLE`) and so can neither dispatch nor sweep. The OS releases the handle on
process death, so a restarted host acquires it. A file handle, not a named `Mutex`: a mutex is
thread-affine and the host is async.

## The gate and the write order

`run_id := dispatch_key`. `dispatch_key = sha256(envelope_id ␟ projection_sha ␟ queued_after ␟ parent)`,
lower-case hex, 64 chars (not truncated; `␟` = U+001F, a null field is the empty string). The PTY key
(`DispatchCommand.DispatchKey`) is unchanged.

| Step | Where | Write | Refusal |
|---|---|---|---|
| L | host, under the gate lock | `TryAdd(dispatch_key, owner)` to the live set **before** G0 (ADR §4: a launch in progress is never swept). Already live ⇒ a duplicate in flight: return the receipt, launch nothing, touch nothing. Only the owner that won G0 deregisters (DSA 2) | — |
| G0 | `BeginAttempt`, one txn | if the key exists → return it, `AlreadyAttempted`, **launch nothing** (the owner removes its own L entry); else insert the attempt row (= `Pending`) | epoch stale → `AIDE-AUTH-EPOCH-STALE`, no row |
| G1 | host | `CurrentProjectionSha()` ≠ the attempt's (or null) → `Rejected` (`error_code AIDE-ATTEMPT-STALE-PREPARE`, `detail rejected_stale`); **no process** | — |
| E | `RunAsync` | `AcpEngineProcess.Start` → initialize → observed auth → spawn authorization | **any** failure before G2 committed (refusal, `EngineStreamEnded`, timeout, cancel) → `Failed` (`AIDE-ATTEMPT-NOT-DISPATCHED`, `detail` = the cause's code) — nothing was sent, so Retry is safe (DSA 5) |
| WT | `RunAsync` (write turn) | `WorktreeProvisioned` row after `Provision` | — |
| S | `RunAsync` | `session/new` | as E |
| G2 | `AppendAttemptOutcome(Dispatched)` | committed only when the attempt has **no `Dispatched` row and no terminal row** (read-first in the writer transaction; the unique index is the net) — so a sweep that resolved the attempt first wins and no prompt follows it (DSA 1) | not committed, **or its reply lost** → no prompt; the host writes `Failed` `AIDE-ATTEMPT-NOT-DISPATCHED` if the ledger answers, else leaves it to the sweep |
| P | `RunAsync` | `client.PromptAsync` — **the consequential write** | — |
| G3 | `AppendAttemptOutcome(terminal)` | exactly one terminal, mapped below | not committed → a second terminal; logged as defect `AIDE-ATTEMPT-SECOND-TERMINAL`, the first stands |
| D | host | deregister live **after** G3 | — |

Read-before-write inside the writer transaction is atomic because `WorkspaceStore.BeginWrite` holds
the single-writer semaphore for the life of the transaction (`StoreWriter.cs:17-28`). Two hosts on two
connections still serialize there. A UNIQUE violation is mapped to `Committed = false`, never thrown
to the caller.

### Terminal mapping (ADR §2, from the host's own `await prompt`)

| Prompt task ends with | Terminal | `cause` / `error_code` |
|---|---|---|
| `stopReason` `end_turn` / `max_tokens` | `ResponseComplete` | — |
| `stopReason` `cancelled` | `Interrupted` **only when a Stop was requested** and the reply's `seq` > the high-water mark (node 2); otherwise `DeliveryUnknown{relay_lost}` with `detail` "uncorrelated cancelled" | — |
| any other `stopReason` (`refusal`, `max_turn_requests`) | `ResponseComplete` with `detail stopReason=<v>` | assume: below |
| `AgentPlaneException EngineReturnedError` (error reply correlated to the prompt id) | `Failed` | the engine code |
| `EngineRequestTimedOut` | `DeliveryUnknown` | `prompt_timeout` |
| `EngineStreamEnded` / `IOException` / pump or token cancelled | `DeliveryUnknown` | `relay_lost` |
| run log unwritable (node 2) | `DeliveryUnknown` | `run_log_unwritable` |

`assume:` the adapter's other stopReasons (`refusal`, `max_turn_requests`) end the turn normally — the
model answered. Confirm: ACP schema `StopReason`. If false: they map to `Failed`; one table row changes.

The terminal row's `detail` carries `{"stopReason":…,"usage":{…}}` or `{"usage":"not recorded"}` — the
NFR-11 `usage` home (node 2 asserts it).

## Crash windows (the P1-DISPATCH pattern)

A crash is the process dying between two durable writes. Injected in tests by an `IAttemptLedger`
decorator that **commits** the write and then throws `SimulatedProcessCrashException` (or throws before
delegating), so no production code carries a test hook. "Restart" is a new host over the same store
with an empty live set, running its startup sweep.

| Window | Between | Durable state left | Startup sweep writes | Re-dispatch of the same key |
|---|---|---|---|---|
| W1 | G0 committed → engine start | attempt row only | `DeliveryUnknown{never_dispatched}` | `AlreadyAttempted`, no launch |
| W2 | session open → G2 commit | attempt row (+ `WorktreeProvisioned`) | `DeliveryUnknown{never_dispatched}` | `AlreadyAttempted`, no launch |
| W3 | G2 committed → prompt written | attempt + `Dispatched` | `DeliveryUnknown{host_restart}` | `AlreadyAttempted`, no launch |
| W4 | prompt answered → G3 commit | attempt + `Dispatched` (events in the run log) | `DeliveryUnknown{host_restart}` | `AlreadyAttempted`, no launch |

Never resend: nothing in the host retries; inspection, reconnection and navigation only read
(AC-R01b).

## The sweep (store half here; triggers and emission in node 2)

`BoundaryDispatcher.SweepAttempts(live, dispatchedCause)` — **one writer transaction**: the
candidates are selected, the cause computed and the rows inserted inside it (DSA 4; the PTY code's
read-outside-the-writer shape is not copied). The host takes its live-set snapshot and sends the sweep
under the **same gate lock** as L+G0, so no attempt can be begun between the snapshot and the sweep
(DSA 1); G2's terminal check is the store-side fence if the lock is ever bypassed.

- candidates: native attempts (`route_fingerprint IS NOT NULL AND <> 'pty'`) with no terminal row,
  `dispatch_key ∉ live`, and **not** (`queued_after_dispatch_key IS NOT NULL` and the predecessor has no
  `ResponseComplete` row);
- each gets `DeliveryUnknown` with `cause = never_dispatched` when it has no `Dispatched` row, else
  `dispatchedCause` (`host_restart` at host startup; `relay_lost` when triggered by an engine exit);
- the terminal append is the same read-first + unique-index path, so a sweep racing a live terminal
  write loses cleanly.

The sweep runs in the **host process**: only the host knows its live set, and passes it over IPC. The
daemon's own startup sweep is PTY-only (finding F1). One host per workspace is **enforced** by the
host lock file (above), not assumed.

**Queued launch (DSA 3, node 2).** A queued attempt's row is written at queue time (G0). Its later
launch — by the drainer or by Send-now — goes through L (`TryAdd`, under the gate lock) before E, so
the drainer and Send-now cannot both start an engine; G2 stays the store gate.

A native attempt's `route_fingerprint` is never null: the host writes `AttemptDispatch.RouteFingerprint`
or `acp:<engine-id>` when T8 has none (the route binding proper is WL-3).

## Run log, relay, redaction (node 2 — fixed here so T6 and T7 build to it)

- **Line format (T6's input):** one line per `RunEvent` =
  `JsonSerializer.Serialize(runEvent)` with default options — T3's serialization, round-trip-tested by
  `RunEventVocabularyTests.RunEvent_CarriesRedactionsAndScannedWith_UnderTheirExactKeys` (PascalCase
  members, `redactions` / `scanned_with` snake). UTF-8, `\n`, no BOM.
- **Path:** `SessionPaths.RunLogFile(workspaceRoot, sessionId, dispatch_key)`
  (`<ws>/.aide/sessions/<id>/runs/<dispatch_key>.jsonl`).
- **Order:** `RunLogStore.Append` + flush **before** `Events.PublishAsync` in `AcpPeer.DispatchAsync`;
  an unwritable line is not published; the prompt fails with `RunLogUnwritable` → G3
  `DeliveryUnknown{run_log_unwritable}` + a sidecar incident.
- **Redaction (T7):** the mapper calls `FrameRedaction.Apply(string frame, string patternsVersion)` →
  `RedactedFrame(Frame, IReadOnlyList<RunEventRedaction> Redactions, ScannedWith)` on the raw line before
  the `RunEvent` is built, and sets `Redactions` / `ScannedWith` (signature per the Leader, 2026-10-19;
  `RunEventRedaction.Offset` indexes the **redacted** stored frame, `RunEvent.cs:193`; `Len` is the
  original span length). If T7 has not joined, the redaction fixture stays red.
- **Relay:** `RunEventRelay.Subscribe()` returns a new `SingleReader` channel; `Publish` writes the same
  instance to every subscriber; no channel has two readers. A late subscriber catches up by
  `seq > last_seen` from the run log.

## Fold inputs (fixed for T6; Leader delegation 2026-10-19)

T6's assumptions are **adopted** where the code does not already decide; each line says which.

| Input | Fixed as | Source / status |
|---|---|---|
| Waiting on permission | run-log kind `permission.request` | **Verified**: the mapper emits it for `session/request_permission` (`AcpRunEventMapper.cs:46`); the fold already shares the name (`SessionDocumentViewModel.PermissionRequestKind`) |
| Waiting on input | run-log kind `input.request` | **adopted**, reserved. No ACP frame the claude-code adapter sends maps to it today, so in WL-1a it never occurs and `Waiting` comes from `permission.request` alone. The mapper that first sees an input-elicitation frame emits this kind |
| Budget exceeded | run-log kind `budget.exceeded` | **adopted**, reserved. Host-emitted, not from the wire; the producer is WL-4's budget enforcement (ADR-0041 §6). No WL-1a producer, so `OverLimit` never folds in WL-1a |
| Context ceiling | run-log kind `context.ceiling` | **adopted**, reserved, as `budget.exceeded`. It stays a distinct kind because NFR-11 keeps ceiling and consumption budget distinct |
| Evidence link to an attempt | `artifact_ref = attempt://<dispatch_key>` | **adopted**. The schema leaves the column free text (`WorkspaceSchema.cs:298`) |
| `PermissionDecisionRow.OptionChosen` | the ACP `PermissionOptionKind` of the chosen option (`allow_once` · `allow_always` · `reject_once` · `reject_always`), **not** the adapter's `optionId` | **adopted**. Matches ADR-0041 §5 ("`allow_always` is recorded as the native option chosen"). The `optionId` survives inside `OptionsOffered`, which stores the offered options array verbatim. The writer is the permission-binding path (§5); no WL-1a node-1 code writes the row |
| `fold.duration_ms` | node 2: the host subscribes an `ActivityListener` to `aide.attempt.fold` and writes `fold.duration_ms` to the rolling log | node 2 |

## Stop (node 2 — fixed here so T8 binds to it)

`StopAsync`: append `StopRequested` (`detail` = the relay's high-water `seq`) → `AcpPeer.NotifyAsync
("session/cancel")` (through the same write lock) → answer outstanding permission requests `cancelled`
and append `PermissionCancelledByStop` → wait on the prompt with a 10 s deadline (config) →
`cancelled` reply with `seq` > high-water ⇒ `Interrupted`; deadline ⇒ `DeliveryUnknown{stop_not_acknowledged}`
unconditionally, then cancel the token. Teardown-caused exit is never `Interrupted`.
`RunChannelSessionThread.Wait`/`Resume` must not move `StopRequested` back to `Running` (red first).

## Error & concurrency model

- Writers: one store writer at a time (semaphore); every gate step is one transaction.
- **The gate lock is a `SemaphoreSlim(1)`, held across the awaits it fences** (DSA re-review, Major 1):
  L→G0 and snapshot→sweep each run entirely inside it, so a sweep can never land between a dispatch's
  L and its G0. The host's maps (live set, per-attempt run state) change only under it. It is never
  held across a launch (E onwards), so a long run never blocks the gate.
- **Queued successors are live from queue time** (re-review, Major 2): the queue registers the
  successor's entry in L at G0 and hands that entry to whichever launcher (drainer or Send-now) wins;
  a launcher re-reads the receipt after L and launches only a still-`Pending` attempt (Minor 3).
- **A lost G2 reply with the ledger unreachable** runs D (deregister), so the next sweep resolves the
  attempt rather than leaving it live forever (re-review, Minor 4).
- `Failed NOT-DISPATCHED` is consumed by T8 with the existing failure reason (re-review, Minor 5).
- `RunAsync` keeps its exception surface; the host catches only `AgentPlaneException`, `IOException`,
  `OperationCanceledException` and `WorkspaceStoreException` to map a terminal. Anything else — the
  injected crash among them — propagates, leaving the rows a real crash would leave.
- DC-221: `DrainAsync`'s `Task.WhenAny(readable, prompt)` is unchanged;
  `TheDrainEndsWhenThePromptEndsTests` stays green. Stop's wait is `Task.WhenAny(prompt, deadline)`.

## Failure-mode analysis

| Mode | Disposition | Test |
|---|---|---|
| Double click / lost IPC reply / drainer vs Send-now race | G0 returns the existing receipt; one launch | `SameDispatchKeyTwice_LaunchesOnce`, `BeginTwice_WritesOneAttemptRow` |
| Two writers both reach G2 | unique index; the loser sends no prompt | `ASecondDispatchedRow_IsNotCommitted` |
| Two terminals (sweep vs live write) | first wins; second refused + defect log | `ASecondTerminal_IsNotCommitted` |
| Prepared projection changed | `Rejected`, no process | `AStalePrepare_IsRejected_AndStartsNoProcess` |
| Crash W1–W4 | sweep → `DeliveryUnknown{cause}`; no resend | `ACrashInAWindow_SweepsToDeliveryUnknown_AndNeverResends(W1..W4)` |
| Daemon restart while the host runs | PTY sweep skips native rows | `ThePtySweep_NeverTouchesANativeAttempt` |
| Queued successor behind a running predecessor | never swept | `Sweep_SkipsAQueuedSuccessorWhosePredecessorLacksResponseComplete` |
| Live attempt at sweep time | never swept | `Sweep_SkipsLiveAttempts` |
| Engine refuses before dispatch | `Failed` + `AIDE-ATTEMPT-NOT-DISPATCHED` | `ARefusalBeforeDispatch_IsFailedAndSendsNoPrompt` |
| Sweep resolves an attempt before its G2 | G2 not committed; no prompt | `Dispatched_AfterATerminal_IsNotCommitted` |
| A second host on the same workspace | refused at open | `ASecondHost_IsRefused` |

## Adversarial analysis (STRIDE-lite)

| Boundary | Threat | Disposition |
|---|---|---|
| IPC `attempt.*` (local pipe, same user) | Tampering: a caller supplies a forged key | accept — the key is recomputed nowhere server-side by design (the daemon has no envelope); the single-user local tool posture (residual recorded) |
| run log on disk | Information disclosure: credentials in frames | mitigate — redaction at ingest (T7, node 2) |
| `attempt.sweep` | Repudiation: a sweep with a wrong live set | mitigate — the host lock file makes one host per workspace a refusal, not an assumption |

## Privacy analysis (LINDDUN-lite)

No new data class. Prompt bodies already live in `prompt_revision_fact`; the run log stores redacted
frames (ADR-0042 §6). Disposition: unchanged.

## UI & interaction design

N/A — no surface in this track. T8 binds `IAttemptHost`; copy ("Stop requested. Waiting for the harness
to acknowledge.") is ADR-0041 §4's.

## Telemetry (measured by default)

`ActivitySource "aide.attempt"`: `attempt.begin {dispatch_key, already_attempted}`,
`attempt.terminal {state, cause, error_code}` (a `Rejected` is a terminal, so no separate span),
`attempt.sweep {count, last_sweep_at, trigger}` (node 2 emits it to the sidecar even at zero). No
`attempt.dispatched` span: duration is derived from the rows (ADR §7; Simplifier 10).

## Test plan (Testing Strategy directives: unit for the pure key, integration on a real SQLite store,
process-boundary E2E with the stub adapter, crash injection; red first)

Node 1 — `tests/AiDe.Core.Tests/Attempts/AttemptGateTests.cs` (real store): the key; G0 idempotency;
G2/G3 uniqueness; sweep predicate (causes, live, queued exclusion); the PTY-sweep exclusion.
`tests/AiDe.App.Tests/Conductor/Attempts/AttemptHostTests.cs` (the product's `RunAsync` spawning
`StubAcpAdapter` over stdio): write order and `run_id = dispatch_key`; same key twice ⇒ one prompt
received; stale prepare ⇒ no process; refusal before dispatch; W1–W4.
Node 2 — Stop (fixture + one real claude-code run saved under
`docs/proof/workbench-loops-wl-1a/host/real-lane-stop/`), token cancel ⇒ not `Interrupted`, sweep
emission at zero, run-log order and failure, redaction fixture end to end, NFR-11 oracle.

## Patterns

- **Receipt Ledger (LOA 4.3)** — the attempt and outcome facts are the receipt; read-before-write gives
  idempotency at the side-effect boundary (P8). Survives the Simplifier: it is the existing PTY gate
  reused, not a new mechanism.
- **Hot Path Bypass (LOA 1.4)** — the relay publishes in memory on the hot path; the run log is the
  durable path a late reader catches up from.
- **Write-ahead log** for the outcome order; **Decorator** for crash injection (test only).
- Rejected: a new `AttemptLedger` class beside `BoundaryDispatcher` (two classes for one store-side
  dispatch half); an instance `GovernedRunHost` (edits the other caller, breaks the one-root count).

## Flagged risks & residual unknowns

- `session/cancel` honoured by the claude-code adapter — **Flagged** until the real-lane Stop run.
- One host per workspace — enforced by the host lock file; a stale lock after an OS-level handle leak is the residual.
- `consumed` row writer is T8 (F2) — the ordering is the host's, the write is T8's.

## Status & next action

| | |
|---|---|
| **Completed** | WL-1a seam designed: gate, write order, windows, sweep, terminal mapping, run-log format, host interface |
| **Remaining** | node 1 implementation (gate, W1–W4, idempotency, stale prepare); node 2 (Stop, sweep triggers, run log, redaction wiring, NFR-11, Proof Pack) |
| **Best next action** | the host-interface commit, then the red gate tests |

## Gate record

Adversary Mode, read-only sub-agents, 2026-10-19.

**The Simplifier — PASS-WITH-CONDITIONS** (no Major). Accepted: 1 `QueueAsync` merged into
`DispatchAsync`; 2 sweep and live set `internal` (`ReadAsync` stays public — the brief's "read an
attempt"); 3 the interface's one reason written above; 6 `AttemptSweepResult.Count` dropped
(`Swept.Count` is the one definition); 7 `ProfileKey`/`ScopeSnapshot` off `AttemptDispatch`; 8
`AttemptRun` is a sealed record; 10 spans cut to begin / terminal / sweep. Kept as reviewed: 4, 5, 9, 11.

**Distributed Systems Architect — VETO**, then the revision above. Dispositions:
1 (Blocker, G2 vs sweep) — G2 refuses when a terminal exists; snapshot + sweep under the gate lock
with L+G0. 2 — `TryAdd` with an owner; only the G0 winner deregisters. 3 — a queued launch goes
through L before E. 4 — the sweep reads and writes in one writer transaction. 5 — every pre-G2
failure is `Failed NOT-DISPATCHED`; a lost G2 reply is not-committed. 6 — T8's startup
reconciliation of `consumed`. 7 — the host lock file, now, not WL-4. 8 (Minor, synchronous writer
wait on the in-process path) — **accepted**: the in-process ledger serves the daemon's synchronous
handlers and the tests; the App's ledger is the async IPC client. Residual noted by the reviewer: the
default prompt `RequestTimeout` may cut a long `Waiting` turn as `prompt_timeout` — a node-2 check.
Re-review of the revision: recorded below.

**Distributed Systems Architect re-review of the revision — PASS-WITH-CONDITIONS, CLEARS-THE-VETO: yes.**
G2's terminal check closes the Blocker at the store. Conditions, all applied above: the gate lock is a
`SemaphoreSlim` held across L→G0 and snapshot→sweep (Major 1); queued successors are live from queue
time (Major 2); a launcher launches only a still-`Pending` attempt (Minor 3); D on a lost G2 reply with
the ledger unreachable (Minor 4); the consumed reason for `Failed NOT-DISPATCHED` (Minor 5); stale
`assume:` text removed (Nit). Residual the reviewer names: a live attempt can still be resolved by
mistake (safe — G2 refuses, no prompt), and the prompt `RequestTimeout` vs a long `Waiting` turn.
