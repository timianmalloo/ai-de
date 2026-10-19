---
id: api-aide-app-workbench-records
title: "API: AiDe.App.Workbench.Records"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Workbench.Records: 12 types, 53 members, 86% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench.Records`

**12 public types · 53 public members · 86% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `DaemonNotebookRevisions`

*class* — `DaemonNotebookRevisions.cs`

The product's `INotebookRevisions`: T14's daemon ops over the App's one workspace client (D1 — the App
reaches the store only through the daemon). `notebook.open` once per model, then `notebook.revision.save`.

**Remarks.** `notebook.open` writes only when the notebook has no row (version 1), so this path never writes a
`notebook_dim` row with version > 1 — no adoption (Ruling 156).

| Member | Summary |
|---|---|
| `string OpenRefusedCopy = "notebook revisions are not recorded: the workspace refused to open this notebook"` | What the model shows when `notebook.open` refused: nothing was written, so no revision is recorded. |
| `Task<NotebookRevision> SaveAsync(NotebookId notebook, string body, int? parentRevisionNo, CancellationToken cancellationToken)` | **(gap)** |

## `NotebookOpenRefusedException`

*class* — `DaemonNotebookRevisions.cs`

`notebook.open` refused with a stable store code; nothing was written.

## `struct`

*record* — `NotebookModel.cs`

The identity of one notebook (ADR-0039 `notebook_dim.notebook_id`; `draft_id` on its revisions).

| Member | Summary |
|---|---|
| `NotebookId ForSession(string sessionId)` | WL-1a re-binds the existing session document to one notebook (§WL.11), so the notebook is named by its session: a pure function of an id the document already has, so nothing new is stored to keep the two in step. |
| `string ToString()` | **(gap)** |

## `NotebookRevision`

*record* — `NotebookModel.cs`

One saved, immutable revision of a notebook, as the store recorded it (`prompt_revision_fact`).

## `INotebookRevisions`

*interface* — `NotebookModel.cs`

Where a notebook's revisions are saved: one store transaction per save, the row read back.

**Remarks.** The App reaches the store only through the daemon (D1): the product implementer is
`DaemonNotebookRevisions` over T14's ops. With no daemon the shell passes none and the model says so
(`RevisionState`), rather than keeping revisions in memory and calling them saved.

## `ResultCurrentness`

*enum* — `NotebookModel.cs`

Whether a turn's output still belongs to the notebook's current draft (UI-04: stale is never shown as current).

## `NotebookModel`

*class* — `NotebookModel.cs`

The workspace-owned model a session-document surface *binds* to (ADR-0045 §4): the notebook's identity, its
revision chain, the thread of turns and which revision each turn ran. The shell keys these by
`NotebookId`; a surface may be created, moved, hidden, closed and reopened over one without creating or
disposing it.

**Remarks.** **The runs are the model's** (`Runs`): the per-run cancellation, the queue and the drain, so a
surface closed mid-run leaves the run running. The envelope store's handle stays the surface's (ADR-0034 rule 7).




**Disposal is counted, not trusted:** `Dispose` opens `DisposeActivity` first, on
the normal path, so a test can show that nothing disposed a model (the `SessionDisposalLedger` idiom).

| Member | Summary |
|---|---|
| `string StaleCopy = "This output belongs to an earlier draft. Run again to refresh it."` | Spec §C3's load-bearing copy for a result whose revision is no longer the draft. |
| `string RevisionsNotRecorded = "notebook revisions are not recorded: no workspace daemon is connected"` | Why revisions are not saved when no store is bound — shown, never silent. |
| `string DisposalActivitySource = "aide.notebook.model"` | **(gap)** |
| `string DisposeActivity = "notebook.model.dispose"` | **(gap)** |
| `NotebookModel(SessionDocumentViewModel session, INotebookRevisions? revisions = null)` | **(gap)** |
| `NotebookId Id { get; }` | **(gap)** |
| `SessionDocumentViewModel Session { get; }` | **(gap)** |
| `NotebookRuns Runs { get; }` | The notebook's runs: they outlive every surface and end only with the model. |
| `RunChannelSessionThread Thread { get; } = new(caughtUp: true)` | The thread of turns. The model's, so a surface built later over this model shows the same turns. |
| `NotebookRevision? Latest` | The newest revision this model saved, or null before the first. |
| `string? RevisionState` | Why the last revision was not saved, or null when revisions are being recorded. |
| `event Action? Changed` | A revision was saved or a turn was bound to one; the stale mark is re-derived. |
| `bool IsDisposed { get; private set; }` | **(gap)** |
| `Task<NotebookRevision?> PrepareRevisionAsync(string body, CancellationToken cancellationToken)` | The revision a Prepare (or a Send) runs from: the latest one when  is its body, else a new immutable revision whose parent is the latest. Null when it could not be saved — the reason on `RevisionState`. |
| `void BindResult(int ordinal, NotebookRevision? revision)` | Records which revision turn  ran; a null revision leaves it not recorded. |
| `NotebookRevision? RevisionOf(int ordinal)` | The revision turn  ran, or null when none was recorded. |
| `ResultCurrentness CurrentnessOf(int ordinal, string liveDraft)` | Derived, never stored: a result is **stale** when a later revision was saved, or when the editor holds other bytes than the revision it ran. |
| `void Dispose()` | **(gap)** |

### `NotebookModel(SessionDocumentViewModel session, INotebookRevisions? revisions = null)`

- **`session`** — The session document's state; the notebook is named by its session.
- **`revisions`** — Where revisions are saved, or null when this build has nowhere to save them.

### `ResultCurrentness CurrentnessOf(int ordinal, string liveDraft)`

Derived, never stored: a result is **stale** when a later revision was saved, or when the editor holds other
bytes than the revision it ran.

**Remarks.** assume: an EMPTY editor is the composer's next block after a send (BeginNextTurn clears it), not an edit of the
notebook — so it leaves the result current. Confirm: the Owner's WL-1a demo step "edit and see the old result
marked stale". If false: drop the empty-draft clause; one line, and the surface test's first assertion flips.

## `INotebookRunObserver`

*interface* — `NotebookRuns.cs`

What a surface bound to the model hears about the model's runs; null when no surface is bound.

## `NotebookRunEnd`

*record* — `NotebookRuns.cs`

How one run ended, for the surface's envelope record and its last-run readouts.

## `AttemptScope`

*record* — `NotebookRuns.cs`

The attempt identity the workspace gives a notebook's dispatches (`AttemptDispatch`'s own fields).

| Member | Summary |
|---|---|
| `Func<CancellationToken, Task<long>>? RefreshEpoch { get; init; }` | Reads the workspace epoch at each dispatch — the current handshake's, so a daemon restart after bind is seen. Null: `WorkspaceEpoch` holds for the binding's life. |

## `RunLaunch`

*record* — `NotebookRuns.cs`

What a turn's launch carries from the composer, captured at Send (never read back from the gate later).

## `NotebookRuns`

*class* — `NotebookRuns.cs`

The notebook's runs (ADR-0045 §4; ADR-0041): the per-run cancellation, the one queued turn (Ruling 95) and the drain
— owned by the model, so a surface may be closed and reopened while a run is in flight ("Pane hidden. The run
continues."). Disposing the model is what cancels them.

| Member | Summary |
|---|---|
| `INotebookRunObserver? Observer { get; set; }` | The bound surface, or null while no surface shows the notebook. |
| `bool InFlight` | Whether any run is in flight. |
| `int? QueuedOrdinal` | The queued turn's ordinal, or null. |
| `IAttemptHost? Host` | The attempt host this notebook's turns dispatch through, or null while none is bound. |
| `event Action? AttemptsChanged` | Raised each time an attempt becomes known to this notebook: a live receipt recorded. |
| `IReadOnlyList<AttemptReceipt> ReadBack { get; private set; } = []` | The attempts read back from the store for this notebook's submitted envelopes (restart read-back). |
| `void BindHost(IAttemptHost host, AttemptScope scope)` | Binds the notebook to the workspace's attempt host (ADR-0041): from now on a sent turn is an attempt of the revision it ran, launched and owned by the host, never by a surface. |
| `string HostedElsewhere = "This workspace's runs are hosted in another window. Send from that window, or close it to host runs here."` | Ruling 159(1): the copy Send shows while another window holds the workspace's attempt host. |
| `string NotOpened = "Runs cannot be hosted in this window: the attempt host did not open. Retry to open the host again."` | The refusal when the host's open ended with no cause this binding knows. |
| `string NoWorkspace = "Runs cannot be hosted in this window: the workspace service is not connected, so a turn would not be recorded. Reopen the workspace once the service runs."` | Ruling 159(2): the refusal of a window with no workspace client — nothing here can record a turn, and no retry can help. |
| `string? HostRefusal` | Why this window cannot host runs, or null. A turn sent meanwhile is refused with it and never runs directly. With neither a host nor an opener the window has no workspace client: `NoWorkspace`. With a host bound whose… |
| `bool CanRetryHost` | True while a retry can help: an open that may succeed on another try, or a read-back that failed. |
| `string RestoringFailed = "Restoring this notebook's turns failed: {cause}. Retry to restore them."` | Ruling 175 (the Owner's copy, DESIGN.md): the refusal, status and UIA help text, of a bound host whose restart read-back failed. `{cause}` is the failure's own message. The Retry reads back again; it does not reopen. |
| `void BindHostWhenOpen(Func<Task<IAttemptHost>> open, AttemptScope scope)` | Binds to the workspace's attempt host once  completes (Ruling 159): a turn sent before then waits for it, and a host that cannot be opened refuses every send with its cause — never a direct run, which would break ADR-… |
| `Task RetryHostAsync()` | The refusal's retry: opens the host again, or, bound with a failed read-back, reads back again for this bind. Completes once this try bound the host or set its refusal, or the read-back ended. |
| `Task ReadingBack` | The restart read-back a turn sent now must wait for (Ruling 163): while the host opens, the wait for that open's bind and its read-back; else the read-back of the last bind (completed when none ran). |
| `void ReadBackOnEachBind(Func<IEnumerable<(string EnvelopeId, string ProjectionSha, string Words)>> submitted)` | Registers the bound surface's submitted envelopes (AC-L01a) as the restart read-back's source: the read-back runs now when a host is bound, and on every later bind, exactly once per bind (Ruling 172). |
| `event Action? HostRefusalChanged` | Raised on the model's home context each time `HostRefusal` is set or cleared. |
| `TimeSpan HolderProbe = TimeSpan.FromSeconds(1)` | How often a hosted-elsewhere window tries the open again, so the copy shows only while the holder lives. |
| `AttemptReceipt? AttemptOf(int ordinal)` | Turn 's attempt as the host last answered it, or null when it was not one. |
| `Task ReadBackAsync(IEnumerable<(string EnvelopeId, string ProjectionSha, string Words)> submitted, CancellationToken cancellationToken)` | Restart read-back: each submitted envelope's dispatch key is derived (`For`, nothing stored beside it) and read from the host; an envelope never attempted is skipped. Each concluded attempt this notebook does not alre… |
| `RunEvent? LastEventOf(int ordinal)` | The last run event the one fold received for turn ; null when it received none. |
| `Task LastRun { get; private set; } = Task.CompletedTask` | The last run's own task — completed before the first. It never faults: a refusal is a line of the run. |
| `Task Start(int ordinal, GovernedRunRequest request, RunLaunch launch)` | Starts the run for accepted turn ; the task never faults. |
| `void Queue(int ordinal, GovernedRunRequest request, RunLaunch launch)` | Queues turn  (Ruling 95: exactly one). Host-bound, the queue is the host's (ADR-0045 §4, design "Queued launch"): the turn is dispatched now as the held successor of the turn in flight, and the host launches it after … |
| `void Stop(int ordinal)` | Stop: the turn is StopRequested, never Interrupted by our own word (ADR-0041 §2). Host-bound, the Stop is the host's (`StopAsync`), whose terminal the turn then takes; otherwise the run's token is cancelled. |
| `Task? SendNow(int ordinal)` | Send now on the queued turn: started when it is the queued one and no turn is in flight. |
| `Task<bool> CancelAsync(int ordinal)` | Cancel on the queued turn: dropped from the queue. True when it was the queued one. |
| `Action<ObservedRunEvent> Sink(string lane, RunEventRelay relay, int ordinal)` | The run's sink: every observed event reaches the relay (the lane) and the turn's fold (the read model) from ONE call, so the two can never disagree. |
| `void Dispose()` | The workspace closing: every run is cancelled; nothing is drained. |

### `void BindHost(IAttemptHost host, AttemptScope scope)`

Binds the notebook to the workspace's attempt host (ADR-0041): from now on a sent turn is an attempt of the
revision it ran, launched and owned by the host, never by a surface.

**Remarks.** Called on the model's home context: `HostRefusalChanged` is raised inline.

### `Task ReadBackAsync(IEnumerable<(string EnvelopeId, string ProjectionSha, string Words)> submitted, CancellationToken cancellationToken)`

Restart read-back: each submitted envelope's dispatch key is derived (`For`, nothing
stored beside it) and read from the host; an envelope never attempted is skipped. Each concluded attempt this
notebook does not already hold becomes a turn (Ruling 163), in dispatch order, so the next turn sent continues
after it.

- **`submitted`** — Each submitted envelope, with the words it opened with (the restored row's words).
- **`cancellationToken`** — The notebook's lifetime on a bind's read-back. It is checked after each read and before each thread write, because a read already on its way to the store cannot be recalled. So a disposed notebook takes no turn (T17e).

**Remarks.** assume: an attempt with no terminal row at read-back is live in this host (the host's startup sweep resolves every
other one), so its own dispatch still owns its turn. Confirm: the sweep's live-key exclusion
(`BoundaryDispatcher.SweepAttempts`). If false: an open attempt from another process has no row.

### `Task? SendNow(int ordinal)`

Send now on the queued turn: started when it is the queued one and no turn is in flight.

**Returns.** The run (host-bound and held at the host: the host's launch), or null when nothing was started.

### `Task<bool> CancelAsync(int ordinal)`

Cancel on the queued turn: dropped from the queue. True when it was the queued one.

**Remarks.** A successor held at the host is withdrawn there (`WithdrawAsync`): true only when the
store's receipt reads withdrawn. A withdraw the host refuses — its launch was already claimed — leaves the turn
as it is, and the refusal is a line of the turn.
