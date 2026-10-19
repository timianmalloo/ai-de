---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 2 hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 2: T20's float guard is removed in three red-then-green checks (floatPane floats again, LayoutPersistence restores floating, the DOCK2/DOCK3 copy and FloatingRedock are deleted); node 3 is Ruling 174's floating bounds."
---

# T21 node 2 hand-off

Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`. All claims are released. The node 1b
hand-off is in git history at 29cfd609.

## Done (green, committed)

- Nodes 1 and 1b: see README.md.
- Node 2, check 1 (floatPane floats again): red aa64c64e, green 650d65c2.
- Node 2, check 2 (LayoutPersistence restores floating): red 975f4430, green aec61ebe.
- Node 2, check 3 (DOCK2/DOCK3 copy, FloatingRedock and its copy tests deleted): red 629815fa, green e9591175.
- Suite (suite-node2.txt): Core 3025 passed / 0 failed / 1 skipped; App 1133 / 1 / 2. The one failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held" at the probe precondition, also red alone (same as node 1b). It is not proven either way. Counts match node 1b: Core minus the 4 deleted copy tests; App minus 2 T20 tests plus 3 new.

## Red / half done

Nothing.

## Next step: node 3 (Ruling 174)

1. ZoneStackDto gets four optional `double?` fields (X, Y, Width, Height), read all-or-none as `LayoutStore.cs:318` does. No schemaVersion bump. Red first: an older-shaped file still loads.
2. A writer: the dock library's move and resize end events write FloatingBounds. Cite it by file:line.
3. Off-screen recovery (AC-D01b) with a report line, tested with bounds outside every display. `LayoutPersistence.cs` already has `VirtualScreen.IsOnAConnectedDisplay(StackNode)` (:353), which I did not read. Check what it already does before you build one.
4. `FloatReadBackRestartTests` asserts that the file holds no bounds (`Assert.All(... Assert.Null(s.FloatingBounds))`). Ruling 174 turns that red. Its remarks also still say LayoutPersistence calls `FloatingRedock.Dock` (:18), which is now false. Fix both there.

## Open (from node 1b, still open)

- Duplicate floating stack id after a restore: `ZoneBackedLayoutService._floatSeq` (:17) restarts at 0. Found by reading, not tested.
- DC-NEW-wl-t21-float siblings: a file with no `"zones"` field, or a floating stack with no `"surfaces"` field, still throws in `ZoneLayoutStore.Read`.
- The live-notebook float and redock, after T11's notebook kind lands on main.

## New findings (node 2)

- floatPane floats the zone's first surface, not the focused tab (`ZoneBackedLayoutService.cs:584`). This is older than T20.
- floatPane on the empty center's placeholder says "No surface “”.".
- With FloatingRedockCopyTests deleted, no test references `ZoneName` (grep of tests/), so nothing pins that every `ZoneLayoutService.ZoneName` label differs from its enum name.
