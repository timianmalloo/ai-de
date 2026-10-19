---
id: proof-workbench-loops-wl-1a-binding-handoff
title: "Hand-off — WL-1a T8 nodes 4 and 5: withdraw a host-held turn, no dispatch outside the host, Send disabled"
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
  cannot open it refuses and never runs directly, the epoch is read at dispatch. Node 5: the composer's Send is disabled
  with the host's refusal (UIA help text, Retry wired to RetryHostAsync); the no-client refusal, the pipe test, the
  execution-state UIA names and the withdrawn copy are handed to node 6.
---

# WL-1a T8 node 5 — hand-off

Node 5 started at 8bb6db9c. It finished one of its three checks. Rule 16's 21-minute window ended before the other two.

## Done

| Check | Tests (`tests/AiDe.App.Tests/Workbench/Records/NoDispatchOutsideTheHostTests.cs`) | Red | Green |
|---|---|---|---|
| 1 Ruling 159(1), finished | `WhileAnotherWindowHoldsTheHost_SendIsDisabledWithTheCopy_TheViewsStillWork_AndClosingTheHolderEnablesIt` · `AnyOtherOpenFailure_DisablesSendWithItsCause_AndTheRetryControlBindsTheHost` | `send-disabled-red.txt` (372778c5). Both are assertion failures: "Send stayed enabled …" | `send-disabled-green.txt` (629be5ac). All 7 tests in the class pass |

What changed:
- `NotebookRuns.HostRefusalChanged` is raised on the home context. A `HostedElsewhere` refusal tries the open again
  every `HolderProbe` (1 s) until it binds or the workspace closes. So the copy shows only while a live holder exists.
  Any other cause stops, and waits for the operator's Retry.
- `ComposerSurface.ShowHostRefusal(refusal, retry)` disables Send and sets its UIA HelpText to the refusal. It shows the
  refusal in the status line, and shows a `Retry hosting runs` button when a retry is given. `Send()` refuses while Send
  is disabled, so Ctrl+Enter follows the button. The composer surface and the document stay enabled (the read-only views).
- `SessionDocumentSurface` sends the refusal to the composer. Retry is offered only for causes other than `HostedElsewhere`.

## Not done / next (node 6)

1. **Check 2, not started: no turn runs unrecorded.** `NotebookRuns.Start` still runs a turn directly
   (`RunAsync`, via `GovernedRunHost`) when there is no host and no opener. 27 App test files send turns or use
   `GovernedRunHost`, and only 6 bind a host. So a refusal here turns most of them red. Next step: count which of the 27
   rely on the direct path, then either bind them to a fixture host or ask the Leader for a ruling on test shells. The
   pipe test for `attempt.withdraw` (like T14's `TheThreeOperations_RoundTripOverThePipe`) is part of this check.
2. **Check 3, not started:** the UIA names for the execution states.
3. **The withdrawn copy (Owner ruling, Leader mail to node 5): not done. It needs two answers first.**
   - The Cancelled reason sentence `TurnCopy.ReasonSentence` (Core, `SessionThread.cs:354`) is shared with Ruling 95's
     queued-turn cancel ("Cancelled before it was sent; its words went back to the editor."). A T3-owned test pins that
     string: `tests/AiDe.Core.Tests/Sessions/Thread/TheQueuedTurnIsOneTurnOfTheStreamTests.cs:109`. If the string is
     replaced with "b2 cancelled by you before it was sent.", the queued cancel changes too and that test fails. Does the
     ruling cover both cases, and is that test file granted?
   - Condition (c), "the UIA name of that turn row carries the same words": the row's UIA Name is `TurnCopy.Name`
     (the ordinal and the turn's words) for every state, and the reason sentence is its HelpText
     (`ThreadFeed.cs:485-487`). Putting the sentence in the Name changes `TurnCopy.Name`, which is outside the one-string
     grant. Is HelpText meant, or is `Name` granted?
4. Earlier residuals stand: `attempt.withdraw` has no daemon-pipe test (see 1).

## Suites, node 5

Core 3017 passed, 1 skipped, 0 failed. App 1081 passed, 1 skipped, 1 failed: `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` ("foreground not held"), the same test as node 4. Run alone, it passed. The runs are `suite-core.txt` and `suite-app.txt`. The `SolutionTreeChordTests` re-run alone is in
`solution-tree-chord-alone.txt`.

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
