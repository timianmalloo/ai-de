---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 1b hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 1b: check 3 (read-back and restart through the store) and the null check are green; node 2 removes T20's guard, so the restart can also run through LayoutPersistence."
---

# T21 node 1b hand-off

Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`. All claims are released. Node 1's hand-off
(checks 0 to 2) is in git history at e16d0467.

## Done (green, committed)

- Node 1: step 0, the WebView2 float probe (18c844c2); check 1, red 49f39e6c and green 591f94b1; check 2, red 44da2272 and green 11b2216e.
- Node 1b, null check: red 7302703f, green b581cc5d.
- Node 1b, check 3 (read-back and restart): red 0fd316bd, green 268f0994.
- Suite at 268f0994 (suite-node1.txt): Core 3029/0/1. App 1132 passed, 1 failed, 2 skipped. The failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held", and it was also red in two runs alone. It is not proven either way, and the next run should be on a quiet desktop.

## Red / half done

Nothing.

## Next step: node 2

1. **Remove T20's guard, red first.** `LayoutPersistence.cs:151` calls `FloatingRedock.Dock`, so a restart through the product still docks every floating pane. `workbench.floatPane` still refuses (DOCK2). Write the red through `LayoutPersistence`/the controller: a pane saved floating restores floating. Then remove the call and the DOCK2/DOCK3 copy. `FloatReadBackRestartTests` already proves everything beneath that call.
2. **Duplicate floating stack id after a restore (found by reading, not tested).** `ZoneBackedLayoutService._floatSeq` (`:17`) starts at 0 in every new service, and `Float` mints `float-{++_floatSeq}` (`:518`). A restored layout keeps `float-1`, and the read-back keeps any `float-` id, so the first float after a restart mints `float-1` again. `WorkbenchLayout.AssertInvariant` does not check stack ids. `Layout.AssertInvariant` (LayoutModel.cs:192) does ("duplicate node id"), but I did not check whether anything calls it on the projection. Red first: restore a layout holding `float-1`, float another pane, and assert the ids are distinct. The fix is to mint past the highest id held (or a fresh id).
3. **The two open siblings of DC-NEW-wl-t21-float:** a file with no `"zones"` field, and a floating stack with no `"surfaces"` field, still throw in `ZoneLayoutStore.Read`.
4. The live-notebook float, per the Leader's report 32.

## Open

- Seam request req-01M5A43ANDG4G188C55BY7BM3S: floating bounds are not stored in the zone file. Until it is ruled, a restored floating window opens at the default placement. Two restored windows therefore open at the same spot, one over the other. Within a session the bounds are kept, because the read-back takes them from the view.
