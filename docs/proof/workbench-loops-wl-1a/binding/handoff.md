---
id: proof-workbench-loops-wl-1a-binding-handoff
title: "Hand-off — WL-1a T8 node 4: withdraw a host-held turn, no dispatch outside the host"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, t8, evidence, handoff]
links:
  - { to: adr-0045-perspective-set-factory-review-understand, rel: implements }
  - { to: adr-0043-notebook-editor-codemirror-canonical-source, rel: implements }
  - { to: design-workbench-loops-wl-1a, rel: depends-on }
review-by: "2026-11-18"
summary: >-
  Track T8 (wl-t8-bind) node 4: attempt.withdraw (Rejected/never_dispatched plus the completed command receipt in one
  store transaction) and Cancel on a host-held turn; Ruling 159 — a turn waits for the host's open, a window that
  cannot open it refuses and never runs directly, the epoch is read at dispatch. UIA names and the composer's disabled
  Send are handed to node 5.
---

# WL-1a T8 node 4 — hand-off

Branch `wl1a-notebook-binding`. Merged local `main` (d5e6b877) at db340a7a. `origin/main` was dafcbee8, behind local
`main`, so `git merge origin/main` was a no-op.

## Done — tests under `tests/AiDe.App.Tests/Workbench/Records/` unless named

| Check | Tests | Red | Green |
|---|---|---|---|
| 1 Withdraw (store) | `AiDe.Core.Tests.Attempts.AttemptWithdrawTests`: `AWithdraw_WritesRejectedNeverDispatched_AndTheCompletedCommand_SoTheReceiptReadsWithdrawn` · `ARetriedWithdraw_WithTheSameCommandId_ReturnsTheOriginalOutcome_AndWritesNoSecondRowOrReceipt` · `TheSameTerminal_WithNoWithdrawCommand_IsNotWithdrawn` · `AWithdraw_OfADispatchedAttempt_CommitsNothing` | `withdraw-store-red.txt` (e8722ff2) | `withdraw-store-green.txt` (cb25cefe) |
| 1 Withdraw (host + UI) | `TheHeldTurnIsWithdrawnTests`: `WithdrawingAHeldSuccessor_WritesRejectedNeverDispatched_AndNothingLaunchesIt` · `AWithdrawThatLosesTheLaunchRace_IsRefusedWithAStableCode_AndTheAttemptStaysLive` · `CancelOnAHostHeldTurn_WithdrawsIt_AndTheTurnReadsCancelledByYou` · `AHostRejectionOfAHeldTurn_WithNoWithdraw_ReadsRejected_NeverCancelled` | `withdraw-red.txt` (cc69aaae) | `withdraw-green.txt` (cb25cefe) |
| 2 Host lifecycle (Ruling 159) | `NoDispatchOutsideTheHostTests`: `ATurnSentBeforeTheHostOpens_WaitsForIt_AndRunsAsAnAttempt` · `WhenAnotherWindowHoldsTheHost_SendIsRefusedWithTheHostedElsewhereCopy_AndTheTurnNeverRuns` · `AnyOtherOpenFailure_RefusesWithItsOwnCause_AndARetryBindsTheHost` · `TheAttemptsEpoch_IsReadAtDispatch_NotFixedAtBind` · `ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds` | `host-lifecycle-red.txt` (a522575c) | `host-lifecycle-green.txt` (f5725d4a) |

Red notes: every red is a compile failure (the members did not exist). `AHostRejectionOfAHeldTurn_...` and
`ADeadHolder_...` are guards: they would have passed on the old code. The dead-holder behaviour was already true,
because the lock is the holder's own `FileShare.None` handle.

What changed:
- Core (seam req-01M59G3YK7REPTB5W3EDSDKDN7, granted):
  - `IAttemptLedger.WithdrawAttemptAsync` and `BoundaryDispatcher.WithdrawAttempt` write the outcome row and
    `command_receipt_fact` (`attempt.withdraw`, command id = dispatch key, outcome `completed`) in one writer
    transaction. The op is `attempt.withdraw`, and it calls `WorkspaceClient.WithdrawAttemptAsync`.
  - `AttemptReceipt.WithdrawCompleted` is read from the command row (`StoreReader.HasCompletedCommand`, which matches any
    caller). `IsWithdrawn` is true only when that row exists AND the terminal is `Rejected{never_dispatched}`.
  - No schema change and no new cause value.
- `AttemptHost.WithdrawAsync` takes the launch claim under the gate lock. It throws `AIDE-ATTEMPT-WITHDRAW-REFUSED`
  (`AttemptHostCodes.WithdrawRefused`) for an attempt that is not held or is already launched.
- `NotebookRuns.CancelAsync` replaces `Cancel`. On a held turn it withdraws, and `SessionDocumentSurface.CancelQueued`
  concludes the turn `Cancelled` ("cancelled by you", the Ruling 95 copy). A refused withdraw adds a line to the turn,
  and the turn does not change.
- `NotebookRuns.BindHostWhenOpen` / `HostRefusal` / `RetryHostAsync`, with the copy `HostedElsewhere` and `NotOpened`.
  `AttemptScope.RefreshEpoch` is read at each dispatch. The shell passes `client.RefreshEpochAsync`.
- `WorkbenchShell` no longer has the direct-`RunAsync` startup path or its sentence. A failed open is announced, and the
  next caller tries the open again.

## Suites (`suite-green.txt` = `suite-core.txt` + `suite-app.txt`)

- **Core:** 3017 passed, 1 skipped, 0 failed.
- **App:** 1079 passed, 1 skipped, 1 failed. The failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`
  ("foreground not held"). Run alone, it **passed** (`solution-tree-chord-alone.txt`).

## Not done / next (node 5)

1. **Ruling 159(1), half done.** The refused window refuses each turn: the turn is `Failed` with the exact copy and
   never runs. The composer's Send is **not yet disabled**, and the reason is **not yet exposed to UI Automation**.
   Next step: in `ComposerSurface`/`ComposerSendGate`, read `Notebook.Runs.HostRefusal`, disable Send, set the
   HelpText/Name to the copy, and offer Retry (`RetryHostAsync`) for a non-`HostedElsewhere` cause. Read-only views
   are untouched today.
2. **Check 3, not started:** the UIA names for the execution states.
3. `assume:` the UI word for an operator's withdraw is T3's `Cancelled` copy ("cancelled by you"). No spec text says
   "withdrawn". If the Owner wants that word, the copy belongs to `SessionThread.TurnCopy` (Core, T3's).
4. Residual: `attempt.withdraw` is proved in-process (`BoundaryDispatcher`). No test covers its round trip over the
   daemon pipe.
5. Residual: a notebook with no `WorkspaceClient` (a test shell or a degraded start) still runs turns directly. This
   was unchanged by Ruling 159.
