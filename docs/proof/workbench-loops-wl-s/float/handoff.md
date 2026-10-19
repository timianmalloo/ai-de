---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 7 hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 7 (close condition 4): (A) is red at 0097164f. Through the real shell, the palette's Float pane floats a live notebook in the model and draws no floating window, because no controller-routed layout command renders. The fix needs one line in DockHost.cs (seam req-01M5ATF6PNMEW0SX2KKZA34D4D, no reply at the kick). (B) and (C) are drafted, not run. Core 3046/0/1, App 1214/3/2."
---

# T21 node 7 hand-off

Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`, base `74849010`. The node 6 hand-off is in git history at
`164a3b96`. The node was held by rule 19 for the WL-1b join from
19:04Z to 19:31Z. The Leader's rule-16 kick came at 19:46Z while (A)'s green waited on a seam grant, so no check is
green and no new check was started.

## Done

- **(A) red, committed** `0097164f` (`live-float-red.txt`). Test:
  `tests/AiDe.App.Tests/Workbench/ALiveNotebookFloatsAndRedocksTests.cs`
  `ANotebookFloatedMidRun_KeepsItsModelAndItsOneAttempt_AndTheTurnCompletesInTheFloatingWindow`.
  The shell is the product's (`WorkbenchShell` over a daemon, the `notebook` kind, the shell's own `AttemptHost`, the
  stub lane holding the turn Running). The gesture is real: `ConsoleToggle.Focus()` puts keyboard focus in the
  notebook, the shell's focus tracking names the pane, then the palette runs *Float pane* by Enter. The test renders
  nothing by hand. Failure: "the model floats the notebook, but the view shows 0 floating window(s); the shell said
  [... | Binding is now floating.]".
- **Suites** at `0097164f` (`suite-node7.txt`): Core 3046 passed / 0 failed / 1 skipped. App 1214 / 3 / 2. The 3:
  (A)'s red, and the two foreground tests (`SolutionTreeChordTests.CtrlEnter_…` "foreground not held";
  `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests` "foreground held=False"), on the Owner's attended list. No
  "desktop hold is occupied" result occurred, so `desktop-hold.txt` was not written.
- Proof Pack (`docs/proof/workbench-loops-wl-s.md`): row 10, demo step 5 and the float risk row now say what is
  measured.

## (A): what the code does (Verified by the red, and by reading)

`WorkbenchShell.Execute` (`WorkbenchShell.cs:418`) and the window's router `PerspectiveShell.Execute`
(`PerspectiveShell.cs:253`) both end in `host.Controller.Execute`. `workbench.floatPane` (`WorkbenchController.cs:208`)
goes through `ApplyAndAnnounce` (`:732`), which applies and announces and never renders. Only the tab menu's
`MoveToZone` and `CloseSurface` (`WorkbenchAdapter.cs:723`, `:729`) render after their apply. So the float is in the
model and on no screen. *Inferred, not tested:* close, collapse, maximize and next-tab by command share the gap.

**Also measured by reading:** `Ctrl+K, F` is not bound as a key. `KeyGestures.For` (`WorkbenchController.cs`, after
`:771`) binds single-chord gestures only, so the palette is the keyboard route. The Proof Pack's demo step 5 now
says so.

## Seam request (open at the kick)

`req-01M5ATF6PNMEW0SX2KKZA34D4D` to the Leader, deadline 1800 s, sent ~19:33Z: grant one line in
`src/AiDe.App/Workbench/DockHost.cs` after `:73`, where the adapter (`:72`) and the controller (`:73`) are built:
`controller.LayoutApplied += adapter.Render;`. In T21's window, `WorkbenchController` would raise
`event Action? LayoutApplied` in `ApplyAndAnnounce` when the result is applied. It is not added before the grant,
because an event nobody wires is an unused member (HYG-A). One risk to check at green: a render after every command
reverts a native within-pane tab reorder the model has not seen yet (the known gap in `WorkbenchAdapter`'s
`LayoutRoot.Updated` comment).

## Next (node 8)

1. On the grant: add the event and the wiring, then re-run (A) to green. Record the class as `DC-NEW-wl-t21-float-d`
   in `docs/lessons/defect-classes.md` in the same commit (a model mutation path with no render: the view and the
   model disagree until an unrelated render). Sweep the other controller commands with one test each.
2. (B) and (C) are drafted in `n8-draft-B-C-tests.txt` (the whole test file with all three checks). Copy (B) and (C)
   into the test file, run each red, save `live-redock-red.txt` and `live-restart-red.txt`, then fix. Expected
   reds, by reading only: the floating window's tab may carry no context menu, because `DecorateTabs` walks
   `Manager` only (`WorkbenchAdapter.cs:82`), and floating windows are separate windows; the focus after a redock
   may not land in the notebook.
3. Then the whole suites, rule 22's ratchet, and the Proof Pack's step 5 and row 10.

## Not done

- (A) green, (B), (C). Rule 22's ratchet was not run: the kick said to run it only if (A)-(C) are all green, and
  this node's next step is not a join.
- **Claims.** `coord claim` refused every path with `COORD-CLAIM-STALE-CONTROLS` (one commit on origin/main touches
  the controls; remedy `git merge origin/main`). The node did not merge main mid-node, so the suite stays at its
  base. The docs were edited unclaimed; only T21 writes them.

## Findings (not fixed)

- From node 6 and earlier, still open: see the node 6 hand-off at `164a3b96`.
