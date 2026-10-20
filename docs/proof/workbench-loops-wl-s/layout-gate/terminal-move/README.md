---
id: wl-s-layout-gate-terminal-move-readme
title: "WL-S T21 node 11 - the terminal move on the route"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-20
summary: "T21 n11: MovingTheParkedTerminalIntoAVisibleZone_LosesNoPane_MovesOnlyIt_AndBuildsIt moves terminal-1 from the collapsed Bottom into Left through host.Controller.Move. Green; red with LayoutApplied removed. Whole suites: only the two known App failures."
---

# T21 node 11 - the terminal move on the route

DONE. No finding T21n11-F1: the test passes on the route.

- Test: `ZoneWorkbenchAdapterTests.MovingTheParkedTerminalIntoAVisibleZone_LosesNoPane_MovesOnlyIt_AndBuildsIt`, beside the unchanged `session:s2` test. It asserts Bottom is collapsed first, moves `terminal-1` to Left with no by-hand render, then checks no drawn pane lost, only `terminal-1` moved, and `ContentFor("terminal-1")` is non-null and in the drawn layout.
- `M1-run.txt`: 1 passed.
- `M1-mutation.txt`: `controller.LayoutApplied += adapter.Render;` (DockHost.cs:74) commented out in a scratch edit: 1 failed. File restored; `git diff --stat src/` empty.
- `suite-core.txt`: 3121 passed, 1 skipped, 0 failed.
- `suite-app.txt`: 1270 passed, 2 skipped, 2 failed, both known (T31): `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective`. The restored-row fold test: PASSED in this suite (1270/2/2: no `[FAIL]` line for it in `suite-app.txt`). This line first read "NOT CHECKED, the OS reports the left mouse button down", which followed the brief and not the result; corrected at the join (Owner ruling, 2026-10-20, on the T21 n11 join). The layout gate is in the suite and passed.
- `ratchet.txt`: PASS, 0 refusals over 50 gates.
- Not done: nothing. No src change, no exception-list entry.
