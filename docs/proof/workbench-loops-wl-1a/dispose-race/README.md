---
id: proof-workbench-loops-wl-1a-dispose-race
title: "Proof Pack: WL-1a T17e — a read-back pending at disposal ends quietly"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, evidence, read-back, dispose, ruling-172, ruling-175]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T17e: since T17d the restart read-back starts when the host binds, so it can still run when the shell disposes the
  notebook, host and store. Held on a gate, it was red: an ObjectDisposedException escaped, and 2 turn rows were written
  after disposal, on the unit path and through WorkbenchShell.Dispose over a real WorkspaceClient. The read-back now runs
  under the notebook's lifetime and ends quietly. T17d's teardown NRE is a different race (a write), and stays open.
---

# T17e — a read-back pending at disposal ends quietly

- **Branch:** `wl1a-dispose-race`, off `origin/main` `a0e33443` (T17d joined). Session `wl-t17e-dispose`.
- **Why:** T17d moved the read-back's start to the bind (`NotebookRuns.ReadBackForTheBind`). The shell builds the
  document at once and disposes everything on shutdown, so a read-back can be running then. The Owner asked for the race
  to be proven or ruled out deterministically, not by reruns.

## Checks

| # | What | Tests (exact, in `AReadBackPendingAtDisposalEndsQuietlyTests`) | Red | Green |
|---|---|---|---|---|
| 1 | Unit path: the read-back's store read is held, disposal happens, then the read is released | `AReadBackHeldWhileTheDocumentNotebookHostAndStoreAreDisposed_EndsQuietly_AndWritesNoTurn`; `AReadBackHeldWhileItsNotebookIsDisposed_RestoresNoTurn_WhenTheReadReturnsAfterward` | `check1-red.txt` at 31b61650: `ended with ObjectDisposedException: Cannot access a disposed object.`; `2 turn rows were written after disposal` | `check1-green.txt` at d7e0ac1b (2/2) |
| 2 | The shell's real shutdown, `WorkbenchShell.Dispose` (called by `MainWindow`'s `Closed`, `src/AiDe.App/MainWindow.xaml.cs:96`), while a revived notebook reads back | `TheShellShuttingDown_WhileARevivedNotebookReadsBack_EndsTheReadBackQuietly_AndWritesNoTurn` | `check2-red.txt` at 31b61650: `2 turn rows were written after the shell shut down` | `check2-green.txt` at d7e0ac1b (1/1) |

Regression check at d7e0ac1b: the whole Records namespace, 92/92 (`records-namespace-green.txt`), including T17d's read-back tests.

### How the gate works

- **Check 1.** A `DispatchProxy` over the real `AttemptHost` (`HeldReads`) holds `IAttemptHost.ReadAsync` until the test
  releases it, then calls the real read. The reopen is in the shell's order: `BindHostWhenOpen`, then build the document,
  then complete the open. The test waits until the read reaches the gate. It then disposes in `WorkbenchShell.Dispose`'s
  order: the document, the notebook, the host, and last the store (check 1a only). Then it releases the gate.
- **Check 2.** The shell runs over a real `WorkspaceClient` to an in-process daemon on a named pipe, as T17d's
  shell-path test does. The gate is on the wire. The daemon's `attempt.read` handler is replaced (`DaemonEndpoint.Register`
  overwrites, `DaemonEndpoint.cs:42-43`) by one that waits, then calls a second endpoint. That endpoint has the same pipe id
  and the same `CapabilityRegistry`, so the client's capability is valid there too (`DaemonEndpoint.Invoke`, :88). No
  product code was changed for the test.
- **The gate ignores the read's cancellation token on purpose.** A store read already on its way cannot be recalled.
  So the test proves that the read-back notices the disposal when the read comes back.
- **No unobserved exception.** Each test awaits the read-back task it captured before disposal
  (`Record.ExceptionAsync`), so a fault is observed and asserted, never left to the finalizer.

### What was wrong, and the fix

- **Before:** the read-back ran with `CancellationToken.None` (`NotebookRuns.cs:317` at a0e33443). Disposing the notebook
  (`NotebookRuns.Dispose` cancels `_disposing`) did not reach it. A read that came back after disposal restored its
  turns into the disposed notebook (`RestoreAsync` → `Thread.Accept`). A read that met a disposed store threw
  `ObjectDisposedException`. That type is outside `KeepingTheFailureAsync`'s caught set, so the read-back task faulted.
- **After** (`src/AiDe.App/Workbench/Records/NotebookRuns.cs`, d7e0ac1b):
  1. `ReadBackForTheBind` passes `_disposing.Token`, the notebook's lifetime.
  2. `ReadBackAsync` checks it after each `host.ReadAsync`. `RestoreAsync` checks it before `Thread.Accept`, before each
     folded event, and before `Thread.Append`/`Thread.Conclude`. So a disposed notebook takes no turn.
  3. `KeepingTheFailureAsync` catches any exception first when `_disposing` is cancelled. It writes `notebook.read-back`
     with outcome `ended` and the exception's type in `error_type`, and returns without setting a refusal. `ended` is
     the word `notebook.host-open` already uses for "the workspace closed" (`OpenHostAsync`).
- **What still escapes, by design.** Before disposal, an exception outside the caught set still faults the read-back, as
  it did before T17e. T17e changes only what happens after disposal.

### A notebook pane's close is not a dispose path

The brief asked about closing a notebook pane too. Closing a pane disposes neither the notebook nor its store:

- `WorkbenchShell.Dispose` is the one place a session document is disposed (`WorkbenchShell.cs:3406`) and a notebook
  model is disposed (:3414). A search of `src/` for `_sessionDocuments.Remove`, `_notebooks.Remove` and a document's
  `.Dispose()` finds only :3406.
- A surface built over a shell-owned model does not dispose it (`_ownsNotebook`, `SessionDocumentSurface.cs:1681-1684`).
  The read-back keeps running into the live model, which is correct: the next surface bound to it shows the turns.
- `SubmittedEnvelopes` is copied to a list under the envelope lock before the read starts (`SessionDocumentSurface.cs:739`),
  so closing the surface's envelope store cannot break the read-back's enumeration.

So a pane close has no disposal race to test. The shutdown (check 2) is the product path that disposes.

## T17d's teardown NRE: not this race

T17d saw one `NullReferenceException` in `SqliteTransaction.Complete`, from `SqliteConnection.Close`, from
`WorkspaceStore.Dispose` (:380, `_writeConnection?.Dispose()`), from `NotebookFixture.Dispose`. It was in
`TheRowsAttemptBindingSurvivesRestartAndSequenceTests.ASentTurnsRow_ShowsItsBinding_WhileTheRunIsStillHeld_ThroughTheSurface`.

- **Verified, by reading: the read-back cannot cause it.** That test binds over an empty store
  (`TheRowsAttemptBindingSurvivesRestartAndSequenceTests.cs:159-184`), so its read-back reads nothing. More generally,
  every read-back store touch is a read. `AttemptHost.ReadAsync` → `BoundaryDispatcher.ReadAttempt` opens its own
  `store.BeginRead()` connection (`BoundaryDispatcher.cs:277`), never the write connection whose open transaction the
  stack shows. The read-back's own writes are in memory (`Thread.Accept`, `Record`).
- **Inferred, not reproduced: a run's write.** That test releases a held stub run (`_fixture.ReleaseRun()`, :182) just before
  it ends. The run's terminal outcome is a store write (`AppendAttemptOutcome`, a write transaction), which can still be
  open when the fixture disposes the store. Confirming it needs a gate on the run's terminal write, in a fixture or test
  this track does not own.
- **The residual stays open.** It is a test-teardown order (the fixture disposes the store under a live run), not a
  product path: in the app the store is the daemon's, and closing the shell does not dispose it.

## Docs

- **`../../workbench-loops-wl-1a.md`, Residual risk:** one item. The race is closed by T17e, and the demo was only mildly
  exposed. The kill step (step 19) disposes nothing, and a normal close would have to land inside a read-back of about
  23 ms. It cites T17d's measured shell-path medians: host-open 16.5 ms, read-back 23.25 ms, n=10.
- **Step 18 is unchanged.** The restoring status (`SessionDocumentSurface.RestoringTurns`) is set only by `Launch`, a Send
  (`SessionDocumentSurface.cs:603`). Step 18 reopens and sends nothing, so the status cannot show during it.

## Whole suites

Fresh runs at d7e0ac1b (`suite-green.txt`; full logs `suite-core.txt`, `suite-app.txt`):

- **Core:** exit 0. 3024 passed, 0 failed, 1 skipped.
- **App:** exit 1. 1147 passed, 1 failed, 2 skipped, 1150 total. That is 3 more tests than T17d's 1147, the three added here.
  - **Environmental:** `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held".
    It fails alone too (`chord-alone.txt`), as it did in T17d's nodes 2-4.
  - T17d's teardown-race test is not among the failures. One run says nothing about whether that race is gone.

## Residual risk

- **Open: T17d's teardown NRE** (above). Inferred to be a run's terminal write racing `NotebookFixture.Dispose`. Not
  reproduced, and not this track's owned code.
- **Not observed: the running app.** Check 2 drives the shell's real `Dispose` in process over a real client and
  daemon. Closing the real window inside a read-back was not tried by hand, and the window is about 23 ms wide.
- **Inferred: a disposed notebook's late refusal.** None is set now. A read-back that fails *before* disposal still
  posts its refusal to the home context, where no surface listens any more. That is harmless, and untested.
