# Brief — T21 `wls-float` (slice WL-S) · Claude Code · Opus · node 5

**Goal (Ruling 171 (2), Ruling 174, and the Owner's T21 close conditions):** float and redock are real. A floating pane
renders, moves, closes, persists its bounds and is read back. A malformed saved layout loads with a report line and
never throws.
**This node (n5), three checks, each red then green, in this order:**
- **(A) Partial-read sentence names what was dropped** (Owner, 2026-10-19 ~15:10Z, no numbered ruling). Name the dropped
  surfaces by **their own Title**: the words the user saw on the tab, in saved order, joined with ", ". Example:
  `Part of your saved layout could not be read and was left out: Notebook 2, Graph.` When the dropped part has **no
  surface with a title** (an empty stack such as `float-1`, or a file with no `"zones"`), use the plain sentence with
  nothing appended. **Never show an internal id.** One const per form, each pinned by a test, both cases red first.
  Update the DESIGN.md row that node 4 added so it carries both forms. Open the saved-file shape first (rule 14). If a
  dropped surface's Title cannot be known from what the reader holds, send a seam request naming the file:line where it
  could be had. Do not invent words.
- **(B) A `null` element in a saved-layout list loads with the partial-read sentence and never throws.** The Owner named
  `"floating": [null]`. It was found by reading, so make it red first, beside the two crash shapes from node 4
  (`zone-layout-no-zones.json`, `zone-layout-floating-no-surfaces.json`). **Sweep the class in the same check:** node 4's
  hand-off says `ZoneLayoutStore.Read` also dereferences a `null` inside `zones`, `tabs` and `surfaces`. Give each one a
  fixture, red then green. Then move DC-NEW-wl-t21-float to `controlled` only if no dereference of a list element is left.
- **(C) Close condition 2: floating-window ids are unique across a restore.** `ZoneBackedLayoutService._floatSeq` (:17)
  restarts at 0, so the first float after a restore that already holds `float-1` mints `float-1` again. Red: restore a
  layout holding `float-1`, float a pane, and assert the new id is not already in use.

**Next node (n6, not this one):** close condition 3, floatPane floats the focused tab and not the first
(`ZoneBackedLayoutService.cs:584`). Then T21 joins **before** T11 (Owner): T11 rebases onto T21 because both edit
`LayoutPersistence.cs`. Close condition 4 (a live notebook floats and redocks) follows once T11 is on main.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.
**Start:** read `{PROOF}/handoff.md` (node 4) and `{PROOF}/README.md`. The Leader merged origin/main into your tree before
dispatch. Your claims should pass; if one is refused with COORD-CLAIM-STALE-CONTROLS, send a request and stop.

## Owned authored paths (T21's window)
`src/AiDe.Core/Workbench/ZoneLayoutStore.cs`, `ZoneLayoutService.cs`, `ZoneBackedLayoutService.cs`, `LayoutStore.cs`
(the one const line granted in node 4), `DropTargetResolver.cs`; `src/AiDe.App/Workbench/LayoutPersistence.cs` (the
partial-read and off-screen sentences only; T11 holds the DOCK4 arm), `WorkbenchAdapter.cs`, `WorkbenchController.cs`;
the `DESIGN.md` row node 4 added; new and existing T21 tests under `tests/AiDe.Core.Tests/Workbench/`,
`tests/AiDe.App.Tests/Workbench/` and `tests/AiDe.Core.Tests/fixtures/zone-layout-*.json`; `docs/lessons/defect-classes.md`
(your DC-NEW entry only); `{PROOF}/**`. Anything else needs a seam request.

## Not in scope
The focused-tab float (n6). The live-notebook float. The move-then-crash residual (`WorkbenchShell.cs`, needs a grant;
it stays Inferred in the README). The "No surface “”." placeholder copy and the DESIGN.md:277 drift: list them in
`handoff.md` as findings and do not fix them.
