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
  execution-state UIA names and the withdrawn copy are handed to node 6. Node 6: the no-client refusal, the internal
  UnhostedForTests seam and its src/ census, withdraw over the pipe, and the withdrawn copy; check 3 to node 7.
---

# WL-1a T8 node 6 — hand-off

Node 6 started at c5eb4aa8 and merged `origin/main` at 9e6778b9, because `coord claim` refused with STALE-CONTROLS. It
finished item 1 (Check 2) and item 2 (the withdrawn copy). Check 3 is handed to node 7.

## Done

| Item | Tests | Red | Green |
|---|---|---|---|
| Check 2: no-client refusal (Ruling 159(2)) | `NoDispatchOutsideTheHostTests.WithNoWorkspaceClient_SendIsRefusedWithItsCause_NoRetryIsOffered_AndNoTurnRuns` | `no-client-red.txt` (c5c0d7ea), an assertion failure: "Send stayed enabled …" | `no-client-green.txt` (c0a1efc2) |
| Check 2: the seam is internal, and the src/ census | `TheUnhostedRunIsATestSeamTests.TheDirectRunSeam_IsInternal` · `NothingUnderSrc_CallsTheDirectRunSeam_OnlyItsDeclarationNamesIt` | `no-client-red.txt` (c5c0d7ea). Planted call: `seam-guard-plant-red.txt` (cef15b04) names `AiDe.App/Workbench/WorkbenchShell.cs` as a "call". The plant is removed | `no-client-green.txt` (c0a1efc2) |
| Withdraw over the pipe | `AttemptWithdrawTests.AWithdraw_RoundTripsOverThePipe_AndTheStoreReadsWithdrawn` | none. It is a **guard**: it passed on its first run, because `attempt.withdraw` was already registered | `withdraw-pipe-green.txt` (6c541288) |
| Withdrawn copy | Core `TheQueuedTurnIsOneTurnOfTheStreamTests` `:109`/`:200` · App `TheHeldTurnIsWithdrawnTests.CancelOnAHostHeldTurn_…` (row HelpText, the announcement exactly "Turn b2 cancelled by you before it was sent.", not "stopped by you", not "Refused before it started") · `AHostRejectionOfAHeldTurn_…` (a Rejected row never reads "cancelled by you": a guard) | `withdrawn-copy-red-core.txt` (0b95c73f) · `withdrawn-copy-red-app.txt` (4955c7b0) | `withdrawn-copy-green-core.txt` · `withdrawn-copy-green-app.txt` (bf4f3d14) |

What changed:
- `NotebookRuns.HostRefusal` is now derived. It is null when a host is bound or the seam is set. Otherwise it is the
  open failure, or `NoWorkspace` when there is no opener. `CanRetryHost` is true only for a non-HostedElsewhere open
  failure. `BindHost`, `BindHostWhenOpen` and the seam raise `HostRefusalChanged` inline. The open's continuation posts
  it to the home context. With no host, no opener and no seam, `Start` fails the turn with `NoWorkspace` and never
  calls `GovernedRunHost`.
- The seam is `internal void NotebookRuns.UnhostedForTests()`. Test-side, `tests/AiDe.App.Tests/Workbench/Records/UnhostedSeam.cs`
  spells it as `.Unhosted()` on a surface or a model, and as `shell.OpenSessionDocumentUnhosted(config)`. The census
  found 10 App test classes and `tests/AiDe.App.ComposerProbe/Program.SessionRender.cs` (the chooser path) on the direct
  path, not 21. Each was changed only to call the seam. `TheThreadAnnouncesAndExposesRealPropertiesTests:372` sets the
  seam on the model before the surface binds. Otherwise the no-client refusal is announced first, and the test's
  `Assert.Single` sees two announcements.
- `TurnCopy.ReasonSentence(Cancelled)` = `$"{DisplayOrdinal} cancelled by you before it was sent."`. The Cancelled row of
  `ThreadAnnouncementPolicy` announces `"Turn " + ReasonSentence` (req-01M59JK0HSVX0EYTXEY0CAAAMM). TurnCopy has one
  Cancelled sentence, and it starts with the label. The Owner chose HelpText (c), and that row is the same announcer
  path Stop uses, so no `assume:` remains. `DESIGN.md:1204` has the string appended. Its "cancel it" lines do not
  contradict it, so they are unchanged.

## Not done / next (node 7)

1. **Check 3, not started:** the UIA names for the execution states (brief, node-4 obligation).

## Suites, node 6

Core: 3018 passed, 1 skipped, 0 failed (`suite-core.txt`). App: 1084 passed, 1 skipped, 1 failed: `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, the same "foreground not held" test as nodes 4 and 5 (`suite-app.txt`). Run alone, it passed (`solution-tree-chord-alone.txt`). `docs-graph.py validate`: 0 problems, 5 index-drift items, which the join regenerates.

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
