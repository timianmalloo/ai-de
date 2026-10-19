---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 5 hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 5: the partial-read sentence names the left-out surfaces by title, a null element in any saved-layout list loads with a report line, and floating-window ids are unique across a restore (close condition 2). The App suite is owed under rule 20; next is close condition 3, the focused-tab float."
---

# T21 node 5 hand-off

Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`. All claims are released. The node 4
hand-off is in git history at ba6b0768. Node 5 stopped at the Leader's 15:35Z rule-16 kick, after
check (C)'s green. No check is half done.

## Done (green, committed)

- Nodes 1 to 4: see README.md.
- Check (A), the partial-read sentence names what was left out: red f6e7842e, green 733391b6.
- Check (B), a null element in a saved-layout list loads with the sentence and never throws: red 8c533157, green 3b12744f.
- Check (C), floating-window ids are unique across a restore (close condition 2): red f3d14d3b, green 4d43a871.
- Core suite (suite-node5-core.txt, at 4d43a871): 3043 passed / 0 failed / 1 skipped. That is node 4's 3034 plus 9 new: 2 in (A), 6 in (B), 1 in (C).

## Owed: the App suite (rule 20)

The Owner posted USER DEMO IN PROGRESS at 15:34Z, so no App run was made after that. The App tests
of (A) and (B) were run red and green before 15:34Z (in the check's red and green files). The whole
App suite was not run in this node. When the board shows USER DEMO DONE and no open join, run:

```
cd C:\Projects\ai-de-wls-float && dotnet test tests/AiDe.App.Tests -nologo > docs/proof/workbench-loops-wl-s/float/suite-node5-app.txt 2>&1
```

Expected from node 4's 1155 / 1 / 2: plus 6 (one named Fact, and five rows added to the plain-form
Theory). This is a prediction, not a result. The one known failure is the SolutionTreeChord probe at
its precondition, as in nodes 1b to 4.

## Next node (n6): close condition 3, then the join

1. **floatPane floats the focused tab.** `SetState` passes `Surfaces().FirstOrDefault()` to `Float` (`ZoneBackedLayoutService.cs:593` at 4d43a871; it was :584 before this node's change to `Float`).
2. T21 joins before T11 (Owner). T11 rebases onto T21, since both edit `LayoutPersistence.cs`.
3. Close condition 4 (a live notebook floats and redocks) after T11 is on main.

## Seam requests (node 5)

- req-01M5ACJG6ZCRGJ3BDBPCYW0WJJ (open): the sentence names a left-out surface by its saved title (`ZoneSurfaceDto.Title`). A renamed tab shows `IHasDisplayName.DisplayName` instead (`WorkbenchAdapter.cs:1128` and `:216`; `IHasDisplayName.cs:9`). The display name is session state and is not in the zone file. After a restart the restored tab also shows the saved title, so the sentence matches the tab the user sees next. The fallback, built: name the saved title. Saving the display name would need a schema-1 field and a ruling.

## Findings (not fixed)

- **Two floating stacks with one id in a saved file** (hand-edited) pass `ZoneLayoutStore.Read`. The zone invariant checks surface ids only. The tree projection's invariant (`Layout.AssertInvariant`, `LayoutModel.cs:199`) then throws "duplicate node id", the same exception check (C)'s red shows. Found by reading, not tested. It is a sibling of check (C) and of the reader's never-throws promise.
- **`float-view-N` ids** (`WorkbenchAdapter.NewNodeId`, `:1102`, used at `:1008` for a natively floated window) come from a static counter that restarts with the process. A restored `float-view-1` and the next native float in a new process could share an id. Found by reading, not tested. It needs an App test, so it waits for USER DEMO DONE.
- **A zone content with no `"tabs"` field** reads as an empty zone without a report (`tabs ?? []`). It holds nothing, so nothing is lost. Not changed.
- **A null scalar** (`surfaceId`, a floating stack's `id`) is not dereferenced by `Read`. It is passed into the model. Recorded in DC-NEW-wl-t21-float as outside that class.
- The "No surface “”." placeholder copy (floatPane on the empty center) is unchanged. Out of scope.
- DESIGN.md:277 still has an older form of the off-screen sentence ("Display 2 is not connected. …"). The code says "“X” moved onto this display because the saved display is not connected." Out of scope.
- No test references `ZoneLayoutService.ZoneName`.
- A move followed by a crash, before the next layout pass or the shutdown flush, is not saved (`WorkbenchShell.cs`, no grant). Inferred.
