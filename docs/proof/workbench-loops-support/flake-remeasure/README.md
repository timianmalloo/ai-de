---
id: proof-wl-flake-remeasure
title: "Evidence: T36 - the known-failure list re-derived with the left button released, N=10"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, measurement, flaky-tests, defect-class]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  Ten full Core+App runs on 44d2689b with the left button reading 0x0000, then nine tests alone x10.
  Core never failed. App failed 2 or 3 tests per run. The restored-row fold now passes. No cause is claimed.
---

# T36 - the known-failure list with the left button released

Read-only node. No src, test or DESIGN.md change, no input injected. It compares with T31
(`../loadflake/README.md`, `../loadflake/rates.md`), which measured while the button read `0x8000` all day.

## Method
- One `dotnet build AiDe.sln` (`build.txt`: 0 warnings, 0 errors). Every run uses `--no-build`.
- N = 10 full runs, k = 1..10, by `run-one.sh`: the board was read before each run (no `USER DEMO` and no join in
  flight); the left button was read before and after each run with the committed read-only
  `../deadholder-repro/mouse-state.ps1`; then Core, then App, with a trx per run.
- `runs.txt`: one line per run (UTC times, Core and App counts from the last summary line).
- `mouse.txt`: every button sample. 0 of the 22 lines hold `0x8000` (20 run samples, `M3 start`, `M3 end`);
  `SM_SWAPBUTTON=0`.
- `rates.py` (T31's, paths and the alone table changed) writes `rates.md` from the trx files.
- M3: `alone.sh` ran nine tests alone, 10 times each (`--filter FullyQualifiedName~<method>`), outputs in the untracked
  `flake-runs/`. `alone.txt` holds the counts. Each alone output shows `Total: 1`.

## Result
Core: 3122 results (3121 passed, 1 skipped) in every run, 0 failures in 10 runs.
App: 1274 results per run; 2 failed in 8 runs and 3 in 2 runs (runs 4 and 6).

| Test | In suite | Alone | Class | T31 (button down): in suite ; alone |
|---|---|---|---|---|
| `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` | 10/10 | 9/10 | intermittent | 10/10 ; 9/10 |
| `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` | 10/10 | 10/10 | fails-alone | 10/10 ; 10/10 |
| `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation` | 0/10 | 0/10 | passes | 10/10 ; 10/10 |
| `CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen` | 1/10 (run 4) | 0/10 | load-only | not in T31's list ; not run |
| `TheFactoryLanding_IsTheNotebookInTheCenter_ElseTheCentersEmptyCopy_EachWithAFocusTarget` | 1/10 (run 6) | 0/10 | load-only | not in T31's list ; not run |
| `TheFloatingNotebook_RedocksToTheCenter_WithItsThreadDraftAndAttemptIntact_SayingNoHide_AndFocusInIt` | 0/10 | 0/10 | passes | 1/10 ; 0/10 |
| `AfterStop_TheTurnIsStopRequested_AndTheQueuedTurnStaysQueuedBehindIt` | 0/10 | 0/10 | passes | 1/10 ; 0/10 |
| `ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds` | 0/10 | 0/10 | passes | 0/10 in suite ; not run |
| `ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives` (Core) | 0/10 | 0/10 | passes | 0/10 in suite ; not run |

First lines of the failure messages are in `rates.md`. The two stable failures keep their T31 messages
(`foreground held=False ... page hasFocus="true"`; `foreground not held; SendInput would prove another window`).
The two load-only failures read `focus after the close is on (none), not the landing` and
`focus landed on (none), not inside the composer`.

## Observations (no cause claimed)
- The restored-row fold went from 10/10 failing (T31) to 0/10 in suite and 0/10 alone.
- CtrlDigit and CtrlEnter failed as often as in T31, both in suite and alone, with the button released.
  CtrlDigit passed once in ten alone runs (the first), as it did in T31 (1 of 10).
- `TheFactoryLanding_...` is a new name on the load-only list; it failed once and passed 10/10 alone.
- `TheFloatingNotebook_...` and `AfterStop_...` each failed once in T31 and never in this N.

## Not measured
- Any cause, or any link between the button state and a result. Only one state (released) was measured here, in a
  different session from T31.
- Alone runs on T31's tests that never failed in either set (T31 did not run them alone).
- Core alone beyond the one Core test named; no Core test failed in suite.
- Rates beyond N = 10 runs per suite and 10 alone runs per test; a 1/10 event has a wide interval.
- Anything on a clean `origin/main` other than `44d2689b`.
