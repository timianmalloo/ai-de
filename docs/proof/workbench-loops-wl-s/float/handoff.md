---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 3 hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 3: Ruling 174's floating bounds in three red-then-green checks (four optional fields with no schema bump, a writer at move or resize end, off-screen recovery with a report line); next are the Owner's four T21 close conditions."
---

# T21 node 3 hand-off

Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`. All claims are released. The node 2
hand-off is in git history at ed91428d.

## Done (green, committed)

- Nodes 1, 1b and 2: see README.md.
- Node 3, check 1 (four optional `double?` fields on `ZoneStackDto`, all-or-none, schemaVersion 1): red 47074ff7, green 99226792.
- Node 3, check 2 (a writer at move or resize end; floating windows now open at their bounds): red 3b15c959, green 2ac90f20.
- The frozen-constructor theory now admits additive optional fields: 60779f03 (seam req-01M5A74MXYR2X0YG0EVE501PMF, droppable).
- Node 3, check 3 (off-screen recovery with a report line): red 74eba6d2, green f8bee27a.
- Item 4: the null-"floating" test passes (zone-bounds-store-green.txt, and the suite).
- Suite (suite-node3.txt, at f8bee27a): Core 3031 passed / 0 failed / 1 skipped; App 1135 / 1 / 2. The one failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held". It is also red alone. It is not proven either way.

## Red / half done

- `DropTargetResolver.cs:3`: the comment on `LayoutRect` still says "window coordinates". It was not started, because the kick came first. Drop hit boxes are in window coordinates, and floating bounds are DIPs in virtual-screen coordinates, so the comment should say both.

## Open seam requests

- req-01M5A6W4HGN4PREM1CK48Q5X4H: the off-screen report sentence. It is built with the proposed words. If other words are ruled, one const changes (`LayoutPersistence.RehomedFloatingSentenceTail`).
- req-01M5A74MXYR2X0YG0EVE501PMF: the frozen-constructor theory change in `PerspectiveLayoutSlotTests` (outside the grant), commit 60779f03.

## Next nodes: the Owner's four T21 close conditions, in this priority

1. **The two saved-file shapes that crash the reader each load with a report line.** A zone file with no `"zones"` field, and a floating stack with no `"surfaces"` field, still throw in `ZoneLayoutStore.Read` (the DC-NEW-wl-t21-float siblings). Red first, with a fixture for each.
2. **Floating-window ids are unique across a restore.** `ZoneBackedLayoutService._floatSeq` (:17) restarts at 0, so the first float after a restore that holds `float-1` mints `float-1` again. Found by reading, not tested.
3. **floatPane floats the focused tab.** `SetState` passes `Surfaces().FirstOrDefault()` to `Float` (`ZoneBackedLayoutService.cs:584`), so the zone's first tab floats. This is older than T20.
4. **The live-notebook float**, after T11's notebook kind lands on main.

## Other findings (still open)

- floatPane on the empty center's placeholder says "No surface “”.".
- No test references `ZoneLayoutService.ZoneName`, since FloatingRedockCopyTests was deleted.
- A move followed by a crash, before the next layout pass or the shutdown flush, is not saved. The reconcile does not call `MarkDirty` (`WorkbenchShell.cs`). This is Inferred.
- The off-screen sentence has two definitions in code (`LayoutStore.cs:271` and `LayoutPersistence.RehomedFloatingSentenceTail`), and no DESIGN.md row of its own.
