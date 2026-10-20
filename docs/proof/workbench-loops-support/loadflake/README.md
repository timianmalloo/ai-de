---
id: proof-wl-loadflake
title: "Evidence: T31 - the full-suite failure rate of DC-NEW-wl-leader3-b, N=10"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, measurement, flaky-tests, defect-class]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  Ten full Core+App runs on 84538f7f. Core never failed. App failed 3 tests in 8 runs and 4 in 2; 3 fail every run.
  Neither named load-only instance reproduced. No cause is claimed.
---

# Evidence: T31 `wl-loadflake`

**Method.** One `dotnet build AiDe.sln` (`build.txt`), then N = 10 runs of `dotnet test tests/AiDe.Core.Tests --no-build`
and `tests/AiDe.App.Tests --no-build` (`run_one.sh`), serial, this machine the only App runner, board read before each
run (no USER DEMO or join hold appeared). No real-lane variable set. Rates come from the `.trx` files via `rates.py`
-> `rates.md`. Each failing test was then run alone 10 times (`alone.sh`, `alone.txt`). Raw trx and logs are in
`loadflake-runs/` and are not committed.

## Per-run table (copy of `runs.txt`)

| k | start | end | Core (pass/fail/skip) | App (pass/fail/skip) |
|---|---|---|---|---|
| 1 | 05:16:22Z | 05:24:26Z | 3119/0/1 | 1263/3/2 |
| 2 | 05:24:44Z | 05:33:12Z | 3119/0/1 | 1263/3/2 |
| 3 | 05:33:28Z | 05:41:56Z | 3119/0/1 | 1263/3/2 |
| 4 | 05:42:07Z | 05:50:21Z | 3119/0/1 | 1263/3/2 |
| 5 | 05:50:47Z | 05:59:04Z | 3119/0/1 | 1263/3/2 |
| 6 | 05:59:37Z | 06:07:48Z | 3119/0/1 | 1263/3/2 |
| 7 | 06:08:28Z | 06:16:49Z | 3119/0/1 | 1262/4/2 |
| 8 | 06:17:35Z | 06:25:50Z | 3119/0/1 | 1262/4/2 |
| 9 | 06:26:15Z | 06:34:34Z | 3119/0/1 | 1263/3/2 |
| 10 | 06:35:06Z | 06:43:23Z | 3119/0/1 | 1263/3/2 |

## Rates and classification

| Test (file:line) | In suite | Alone | Class |
|---|---|---|---|
| `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` (`tests/AiDe.App.Tests/Workbench/Notebook/CtrlDigitSwitchesThePerspectiveFromInsideAPageTests.cs:33`) | 10/10 | 9/10 | intermittent (passed alone once, run 1) |
| `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation` (`.../Workbench/Records/AReopenedNotebookRendersItsRecordedAttemptsTests.cs:220`) | 10/10 | 10/10 | fails-alone |
| `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` (`tests/AiDe.App.Tests/SolutionTreeChordTests.cs:10`) | 10/10 | 10/10 | fails-alone |
| `TheFloatingNotebook_RedocksToTheCenter_WithItsThreadDraftAndAttemptIntact_SayingNoHide_AndFocusInIt` (`.../Workbench/ALiveNotebookFloatsAndRedocksTests.cs:109`) | 1/10 (run 7) | 0/10 | load-only (one event) |
| `AfterStop_TheTurnIsStopRequested_AndTheQueuedTurnStaysQueuedBehindIt` (`.../Sessions/Thread/ASendWhileATurnRunsOffersWaitOrParallelTests.cs:300`) | 1/10 (run 8) | 0/10 | load-only (one event) |

The alone filter is a substring match; the Floating test's filter matched 2 tests, both passed.
Failure messages (first line) are in `rates.md`. The CtrlDigit and CtrlEnter messages name the foreground/focus
environment; this is quoted, not a cause.

## Named instances

- `NoDispatchOutsideTheHostTests.ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds`
  (`tests/AiDe.App.Tests/Workbench/Records/NoDispatchOutsideTheHostTests.cs:236`): NOT REPRODUCED, N=10.
- `TerminalHostInLifePathTests.ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives`
  (`tests/AiDe.Core.Tests/TerminalHostInLifePathTests.cs:37`): NOT REPRODUCED, N=10.

No cause is given for any row (NG1).

## Not measured

Real-lane tests (NotExecuted by design). Load other than the suite itself (the user's own desktop activity during
the window was not recorded). Any parent-commit comparison. Two single-event load-only rows are N=1 events, not rates.
A deterministic repro is the next node.
