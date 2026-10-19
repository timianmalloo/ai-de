---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 8 hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 8: the float guard red-first on the Leader's priority order (d82df79e, joined as the float fix); every controller layout command renders (WorkbenchController.LayoutApplied, 36884736, DC-NEW-wl-t21-float-d, five palette tests); floating tabs are named and carry the menu (28c60d1f). (A) and (C) green; (B) redocks with its state intact and is red on focus at 28c60d1f. Suites and ratchet in suite-node8.txt and ratchet.txt."
---

# T21 node 8 hand-off

Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`, node base `2a49cdc5`. The node 7 hand-off is in git history at
`89bcb068`. The node 8 table, the finding on node 2's test and the residuals are in `README.md` (section "Node 8").

## Done

- **Float guard** (Leader, Owner's order): red `9ba8572f` → green `d82df79e`. The Leader cut `wls-float-fix` at
  `36884736` for the float-fix join (the guard at `d82df79e` is the fallback).
- **(A) green and the class sweep:** red `626c7221` → green `36884736`. `WorkbenchController.LayoutApplied` → `adapter.Render`
  in `DockHost.Create`. `LayoutCommandsRenderTests`: float, close, collapse, maximize, next tab, each through the palette.
  DC-NEW-wl-t21-float-d is registered in `docs/lessons/defect-classes.md`.
- **Floating tabs:** red `18f9c404` → green `28c60d1f`. (C) is green with it.
- **(B):** red `74af5881` (no menu); after `28c60d1f` red only on focus ("it is on Window").

## Next (node 9)

1. **(B)'s focus.** See the last section below for what the node tried and what its suite says.
2. **The proposed gate:** a sibling of `ComposerContentOraclesDriveTheGestureTests`
   (`tests/AiDe.App.Tests/Workbench/Composer/`) for layout-command tests: a view-state oracle must drive the palette
   or the controller's `Execute` through `DockHost`, never an adapter rendered by hand. Evidence: node 2's
   `FloatPane_FloatsTheFocusedPane_...` passed while the palette drew 0 windows (README, node 8 finding).
3. Proof Pack demo step 5 in the running app, once (B) is green: the palette's *Float pane*, then the tab's menu key,
   *Move to*, *Center*.

## Rule 19 overlap (recorded, not hidden)

The float-fix join posted `join in flight` at 20:16:44Z. One App run of mine (`TheFloatingNotebook_RedocksToTheCenter`,
two cases, about 5 s) ran after that post: the board check printed 3 matching lines in the same command and the run was
not gated on it. Later runs are gated on the board check.
