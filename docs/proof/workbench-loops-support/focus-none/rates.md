---
id: proof-wl-focus-none-rates
title: "Evidence: T40 per-test non-pass rates over N full App runs"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, measurement, focus]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  Tests that failed or ended NOT CHECKED in any run, with T36's in-suite column beside them.
---

# T40 F5 - rates (N = 10 runs)

| Test | Failed | Not passed other (skipped / NOT CHECKED) | runs | T36 in suite | first message line |
|---|---|---|---|---|---|
| `AiDe.App.Tests.Canvas.KnowledgeModeRealProjectionTests.Understand_TheDockedCanvasInKnowledgeInclusiveMode_ShowsAKnownKnowledgeNodeWithItsTypedEdges_AndOffHidesIt` | 0/10 | 10/10 | 1,2,3,4,5,6,7,8,9,10 | not in T36's list | the real daemon projection over a repository: set AIDE_REAL_PROJECTION_ROOT to the repository root |
| `AiDe.App.Tests.Conductor.Attempts.AttemptRealLaneStopTests.Stop_OnTheRealClaudeCodeLane_EndsInTheAdrsTerminal_AndIsRecorded` | 0/10 | 10/10 | 1,2,3,4,5,6,7,8,9,10 | not in T36's list | the real claude-code lane: set AIDE_REAL_ADAPTER_ROOT (and AIDE_REAL_LANE_EVIDENCE to keep the run's evidence) |
| `AiDe.App.Tests.Kinds.CtrlDigitSwitchesThePerspectiveFromInsideAPageTests.CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` | 0/10 | 10/10 | 1,2,3,4,5,6,7,8,9,10 | not in T36's list | NOT CHECKED: the foreground window is 0xa80450 and the test's window is 0x82809b8; SendInput would prove another window. |
| `AiDe.App.Tests.Sessions.ConsoleSplitPlacementTests.CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen` | 1/10 | 0/10 | 5 | not in T36's list | focus after the close is on (none), not the landing |
| `AiDe.App.Tests.SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` | 0/10 | 10/10 | 1,2,3,4,5,6,7,8,9,10 | not in T36's list | NOT CHECKED: the probe's window was not the foreground window after its own focus attempt; SendInput would prove another |
| `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` | 0/10 | 0/10 | - | 10/10 | passed in every run |
| `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` | 0/10 | 0/10 | - | 10/10 | passed in every run |
| `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation` | 0/10 | 0/10 | - | 0/10 | passed in every run |
| `CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen` | 0/10 | 0/10 | - | 1/10 (run 4) | passed in every run |
| `TheFactoryLanding_IsTheNotebookInTheCenter_ElseTheCentersEmptyCopy_EachWithAFocusTarget` | 0/10 | 0/10 | - | 1/10 (run 6) | passed in every run |
