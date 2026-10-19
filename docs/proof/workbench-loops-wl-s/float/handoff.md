---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 4 hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 4: the schema-1 freeze as an allow-list, the two malformed zone-file shapes load with a report line (close condition 1), and one definition of the off-screen sentence; next is the Owner's refinement of the partial-read sentence, then close conditions 2 to 4."
---

# T21 node 4 hand-off

Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`. All claims are released. The node 3
hand-off is in git history at 0ec8dd34. Node 4 stopped at the Leader's rule-16 kick, after check 2's
green. No check is half done.

## Done (green, committed)

- Nodes 1 to 3: see README.md.
- Check 0, the schema-1 freeze as an allow-list: red 9e20dcd0, green 1da9f822.
- Check 1, no `"zones"` field and a floating stack with no `"surfaces"` field load with a report line (close condition 1): red 96ffc26a, green 7c3a40aa.
- Check 2, one definition of the off-screen sentence, and the `LayoutRect` comment: red c2dd868e, green 08f62223.
- Suite (suite-node4.txt, at 08f62223): Core 3034 passed / 0 failed / 1 skipped; App 1155 / 1 / 2. The one failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held; SendInput would prove another window". That is the probe refusing at its precondition, the same as nodes 1b to 3. It is not proven either way. Core is node 3's count plus 3. The App count also includes the tests main brought in at 003688ea, so it is not compared with node 3's.

## Next node, first item: the Owner's refinement of the partial-read sentence

The Owner confirmed "Part of your saved layout could not be read and was left out." with one
refinement, which arrived after check 1 was green:

- When the reader knows what it dropped (a surface title or a zone), the sentence names it: `Part of your saved layout could not be read and was left out: {what}.`
- When there is nothing to name, the sentence stays as it is.
- Each form gets one const, pinned by a test, red first.

Today `ZoneLoadResult.PartlyRead` is a bool, and `LayoutPersistence.PartlyReadSentence` is the one form.
The next node needs a list of what was left out in place of the bool, and must decide what each of the two shapes can name:
- A floating stack with no `"surfaces"` field has no title to name. Only its `id` is known, such as `float-1`, and that is not user-facing.
- A file with no `"zones"` field names no zone either.

So both fixtures today hit the nothing-to-name form. A test of the `{what}` form needs a shape that does name something, for example a zone entry whose content cannot be read. That shape is a new fixture. Ask the Owner which words `{what}` takes for the shapes in hand before you build it. Update the DESIGN.md row (granted in node 4) to carry both forms.

## Then: the Owner's T21 close conditions, in this priority

1. Done in node 4 (above).
2. **Floating-window ids are unique across a restore.** `ZoneBackedLayoutService._floatSeq` (:17) restarts at 0, so the first float after a restore that holds `float-1` mints `float-1` again. Found by reading, not tested.
3. **floatPane floats the focused tab.** `SetState` passes `Surfaces().FirstOrDefault()` to `Float` (`ZoneBackedLayoutService.cs:584`), so the zone's first tab floats. This is older than T20.
4. **The live-notebook float**, after T11's notebook kind lands on main.

## Seam requests (node 4)

- req-01M5A9WFD8G87N4DFPRB0B5C9X: merge main into the tree. Done by the Leader at 003688ea.
- req-01M5A9YZ4PB93ZD06KE5EMC5X8: the partial-read sentence. Accepted provisionally, then confirmed by the Owner with the refinement above.
- req-01M5A9YZ7H7QPANKV4YMECM31H: LayoutStore.cs, one line, granted. The changed line is :272, the literal's own line in the statement that starts at :271.
- Older: req-01M5A6W4HGN4PREM1CK48Q5X4H (off-screen sentence copy) and req-01M5A74MXYR2X0YG0EVE501PMF (60779f03, now replaced by the allow-list as the Owner asked).

## Other findings (still open)

- A JSON `null` element inside `zones`, `floating`, `tabs` or `surfaces` (for example `"floating": [null]`) is still dereferenced by `ZoneLayoutStore.Read`. Found by reading, not tested. DC-NEW-wl-t21-float stays `partially-controlled` for this.
- DESIGN.md:277 still has an older form of the off-screen sentence ("Display 2 is not connected. …"). The code says "“X” moved onto this display because the saved display is not connected."
- floatPane on the empty center's placeholder says "No surface “”.".
- No test references `ZoneLayoutService.ZoneName`, since FloatingRedockCopyTests was deleted.
- A move followed by a crash, before the next layout pass or the shutdown flush, is not saved. The reconcile does not call `MarkDirty` (`WorkbenchShell.cs`). This is Inferred.
