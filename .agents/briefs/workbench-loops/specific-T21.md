# Brief — T21 `wls-float` (slice WL-S) · Claude Code · Opus · node 6

**Goal (Ruling 171 (2), Ruling 174, and the Owner's T21 close conditions):** float and redock are real. A floating pane
renders, moves, closes, persists its bounds and is read back. A malformed saved layout loads with a report line and
never throws.
**Node 5 is done** (715fe108): partial-read names by Title (seam req-01M5ACJG6Z… ACCEPTED by the Owner, so the saved
title is the right one), null list elements swept, unique float ids across a restore. Core 3043/0/1.

**This node (n6), in this order:**
- **(0) The owed App suite at node 5's head**, before any edit. `dotnet test tests/AiDe.App.Tests -nologo`, saved as
  `{PROOF}/suite-node5-app.txt`. Rule 20's hold was lifted at 15:46Z (USER DEMO DONE). Any App red that node 5 caused is
  this node's first fix.
- **(A) Close condition 3: floatPane floats the focused tab, not the first.** `ZoneBackedLayoutService.cs:593` passes
  `Surfaces().FirstOrDefault()` to `Float`. Red: a zone with two tabs, the second focused, floatPane → the second floats.
- **(B) Two floating stacks with the same id in a saved file** load with the partial-read sentence and never throw at
  restore. Node 5 found this by reading: the reader passes them, and the restore throws `duplicate node id`. Same class
  as node 5's crash shapes. Red first, with a fixture.
- **(C) The adapter's floating-view ids after a restart.** Node 5 said a `float-view-N` counter restarts with the process
  (`WorkbenchAdapter.cs:1102`). **The Leader's grep for `float-view` in `src/AiDe.App` found no match at af5a663b**, so
  open the code first (rule 14). If the collision is real, it is red then green (an App test). If it is not, record what
  the code does, with file:line, in handoff.md, and correct node 5's README line. This counts as the check.

**After this node:** T21 joins **before** T11 (Owner). Close condition 4, a live notebook floats and redocks, follows once
T11 is on main.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.
**Desktop hold (DC-NEW-wl-leader3):** other tracks may run App UI tests in parallel. A result that says "Ruling 115
desktop hold is occupied" is **neither a failure nor a pass**: record it, wait two minutes, and re-run.

## Owned authored paths (T21's window)
`src/AiDe.Core/Workbench/ZoneLayoutStore.cs`, `ZoneLayoutService.cs`, `ZoneBackedLayoutService.cs`, `LayoutStore.cs`
(node 4's one const line), `DropTargetResolver.cs`; `src/AiDe.App/Workbench/LayoutPersistence.cs` (the partial-read and
off-screen sentences only; T11 holds the DOCK4 arm), `WorkbenchAdapter.cs`, `WorkbenchController.cs`; the `DESIGN.md`
row node 4 added; T21's tests under `tests/AiDe.Core.Tests/Workbench/`, `tests/AiDe.App.Tests/Workbench/` and
`tests/AiDe.Core.Tests/fixtures/zone-layout-*.json`; `docs/lessons/defect-classes.md` (your DC-NEW entry only);
`{PROOF}/**`.

## Not in scope
The live-notebook float. The move-then-crash residual (`WorkbenchShell.cs`, needs a grant; it stays Inferred). The
"No surface “”." placeholder copy and the DESIGN.md:277 drift, which stay listed as findings.
