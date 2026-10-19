---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 6 hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 6: the owed App suite at node 5's head is 1165/1/2 (the known SolutionTreeChord precondition); floatPane floats the focused tab (close condition 3); a saved floating stack whose id is in use loads with the partial-read sentence; a native float never takes a restored float-view id. Core 3046/0/1, App 1166/1/2. Next is the T21 join, before T11."
---

# T21 node 6 hand-off

Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`. All claims are released. The node 5
hand-off is in git history at 715fe108. The Leader's rule-16 kick came at 16:01Z, while check (B)'s
green was one step away. (B) green was committed, then both whole suites ran. No check is half done.

## Done (green, committed)

- Nodes 1 to 5: see README.md.
- (0) The owed App suite at f21fdf95 (suite-node5-app.txt, db19c117): 1165 passed / 1 failed / 2 skipped. The failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` at its precondition ("foreground not held"), as in nodes 1b to 4. Node 5 caused no App red.
- (A) floatPane floats the focused tab (close condition 3): red 2a590c8a, green 3c433c3f.
- (B) A saved floating stack whose id is in use loads with the partial-read sentence and never throws at restore: red 5327d810, green 51ae8820 (with DC-NEW-wl-t21-float-c).
- (C) A native float after a restart never takes a restored window's `float-view` id: red f5fc73b0, green 9973a1ce. The collision was real.
- Suites at 51ae8820 (suite-node6.txt): Core 3046 / 0 / 1 skipped; App 1166 / 1 / 2 skipped (the same precondition failure).
- No "desktop hold is occupied" result occurred, so `desktop-hold.txt` was not written.

## (C): what the code does

The Leader's grep for `float-view` in `src/AiDe.App` found nothing because the literal is never
written. At f21fdf95, `WorkbenchAdapter.MapFloatingWindow` (`WorkbenchAdapter.cs:1008`) called
`NewNodeId("float")`, and `NewNodeId` (`:1102`) returns `$"{prefix}-view-{N}"` from the static
`_nodeSeq` (`:1101`). That counter is shared with the split and stack ids and restarts with the
process. Node 5's README and hand-off line was right, so it needs no correction.

## Next

1. The T21 join, before T11 (Owner). T11 rebases onto T21, since both edit `LayoutPersistence.cs`.
2. Close condition 4 (a live notebook floats and redocks) after T11 is on main.

## Seam requests (node 6)

None.

## Findings (not fixed)

- **A kept `float-` pane id the model does not hold.** If a read maps a native window first and
  mints an id, and a later window in the same read keeps that same id from its pane name, both are
  kept. A render names panes from the model, so this is not reachable from a render. Inferred.
- **The welcome placeholder's surface id.** While the Center is empty, the projection shows the
  surface `welcome`. A saved floating surface with id `welcome` would repeat a surface id in the tree.
  Found by reading; `welcome` is not a restorable kind, so it is not tested.
- From node 5, still open: a zone content with no `"tabs"` field reads as an empty zone without a
  report; a null scalar is passed into the model; the "No surface “”." copy; DESIGN.md:277's older
  off-screen sentence; no test references `ZoneLayoutService.ZoneName`; a move followed by a crash
  is not saved (`WorkbenchShell.cs`, no grant, Inferred).
