---
id: wl-s-float-proof
title: "WL-S T21 float - proof pack"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "Ruling 171 (2) nodes 1, 1b and 2, Ruling 174 node 3, and node 4: the WebView2 float probe; floating stacks drawn, redocked, closed and activated; read back and restored floating; T20's guard removed; floating bounds stored, written at move end, recovered off-screen; the schema-1 freeze as an allow-list; two malformed zone-file shapes load with a report line; one definition of the off-screen sentence; node 5: the partial-read sentence names the left-out surfaces by title, null list elements load with a report line, and floating-window ids are unique across a restore; node 6: floatPane floats the focused tab, a saved floating id in use loads with the sentence, and a native float never takes a restored id. Node 7: a live notebook floated by the palette is in the model and on no screen (red 0097164f); the fix waits for a DockHost.cs seam."
---

# T21 float, nodes 1, 1b, 2 (Ruling 171 (2)), 3 (Ruling 174) and 4

Node 1 did steps 0 to 2 and stopped at the Leader's 13:01Z kick. Node 1b did check 3 and the null
check. Node 2 removed T20's float guard in three checks. All are green. See handoff.md for node 3.

| Step | Test | Red | Green |
|---|---|---|---|
| 0. WebView2 float probe (**off-screen, no pixel check**) | `tests/AiDe.App.Tests/Workbench/WebView2FloatProbeTests.cs` `FloatingAndRedockingTheCanvasAndTheComposer_KeepsTheProcessAndBothBrowsersAlive` | n/a (a measurement) | webview2-float-probe.txt (18c844c2) |
| 1. Render Layout.Floating | `tests/AiDe.App.Tests/Workbench/FloatRenderTests.cs` `AFloatedPane_IsDrawnAsADockLibraryFloatingWindow_VisibleFocusableAndHoldingItsContent` | render-floating-red.txt (49f39e6c) | render-floating-green.txt (591f94b1) |
| 2. Move, close, activate a floating surface | `tests/AiDe.Core.Tests/Workbench/FloatingMoveCloseTests.cs` (4 tests) | move-close-floating-red.txt (44da2272) | move-close-floating-green.txt (11b2216e) |
| 3. Read-back and restart | `tests/AiDe.App.Tests/Workbench/FloatReadBackRestartTests.cs` `FloatingWindows_AreReadBackFromTheView_AndALayoutSavedWhileTheyFloat_RestoresThemFloatingAndVisible` | readback-restart-red.txt (0fd316bd) | readback-restart-green.txt, readback-restart-mutation.txt (268f0994) |
| null. No "floating" field | `tests/AiDe.Core.Tests/Workbench/ZoneLayoutWithoutFloatingTests.cs` `ASavedLayoutWithNoFloatingField_RestoresWithAnEmptyFloatingList_AndNeverThrows`, fixture `tests/AiDe.Core.Tests/fixtures/zone-layout-no-floating.json` | null-floating-red.txt (7302703f) | null-floating-green.txt (b581cc5d) |
| Suite | App and Core, once each, at 268f0994 | - | suite-node1.txt: Core 3029 passed / 0 failed / 1 skipped; App 1132 / 1 / 2 (the one failure is below) |

**The App failure, not proven either way.** `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`
failed in the suite and in two runs alone, each time with "foreground not held; SendInput would
prove another window". That is the probe refusing at its precondition
(`tests/AiDe.App.SolutionTreeProbe/Program.cs:119`), so nothing about Ctrl+Enter was tested. The
probe references none of the three changed source files. The cause, another window holding the
foreground, is Inferred, not measured. Leader report 27 records the same test as focus-stolen and
passing alone.

## Probe result (step 0)

The process **survived**. The app's real CanvasSurface and a real ComposerSurface were floated into
AvalonDock 5.0.0 floating windows and redocked through a fresh `Manager.Layout`, which is the same
re-parent Render does. Each browser ran script (6*7 = 42) docked, floated and redocked, and the
canvas initialised once. There was no crash, so no Owner escalation. Limits: the host window was
off-screen and no pixels were sampled. Separately, AvalonDock's own `LayoutDocument.Dock()` left the
canvas floating once its original pane had emptied. The product does not use `Dock()`.

## What changed

- `WorkbenchAdapter.Render` draws each non-empty `Current.Floating` stack as a `LayoutDocumentFloatingWindow` (`BuildFloatingWindow`). The window opens at the stack's `FloatingBounds`, or at 640x420 offset 48 into the workbench when it has none. Content is reused, never rebuilt.
- `ZoneLayoutService.MovePane`, `ClosePane` and `Activate` look in the floating stacks before they refuse. Redock makes the surface the target zone's active tab, with the same expand and extent floor as a docked move.
- **Check 3.** `WorkbenchAdapter.ReadLayoutFromView` no longer returns null for a view with floating windows, and no longer writes `Floating = []`. `MapFloatingWindow` maps each document floating window that holds exactly one non-empty document pane of known surfaces to a floating `StackNode`. A window the render drew keeps its `float-` id. A window floated natively (AvalonDock's `Float()`) gets a fresh `float-view-N` id. Bounds are read from the pane, or else from its group. An anchorable window, several panes or the placeholder still returns null. `ZoneBackedLayoutService.TryMapByPosition` starts from the view's floating stacks (`current with { Floating = tree.Floating }`), so a natively floated pane passes the surface-set guard. Reverting that one line turns the test red ("the service refused the view's floating windows").
- **Null check.** `ZoneLayoutStore.Read` reads an absent `"floating"` field as no floating panes (`?? []`). System.Text.Json passed null for the missing positional member, and `.Select` threw `ArgumentNullException` at `ZoneLayoutStore.cs:186`. The schema-1 DTO is unchanged. New defect class **DC-NEW-wl-t21-float** is in `docs/lessons/defect-classes.md`.

## Check 3: what the restart leg is

The restart runs through `ZoneLayoutStore.Save`, then `Read`, then `RestoreZones` into a new
service, then `Render` into a new DockingManager. It does **not** run through `LayoutPersistence`,
because `LayoutPersistence.cs:151` still calls `FloatingRedock.Dock` (T20's guard). That call docks
every floating pane on restore, and node 2 removes it. Two corrections to the test came after the
red, both after the red's failing line: the set comparison now sorts both sides, and the test now
asserts that the native window's bounds are read back. Both are recorded in readback-restart-green.txt.

## Findings for node 2

- **Duplicate floating stack id after a restore (Verified by reading, not tested):** `_floatSeq` (`ZoneBackedLayoutService.cs:17`) restarts at 0 in every service, so the first float after a restore that holds `float-1` mints `float-1` again. handoff.md has the red-first plan.
- **DC-NEW-wl-t21-float siblings (open):** a zone file with no `"zones"` field, or a floating stack with no `"surfaces"` field, still throws in `Read`.
- **Placement (closed by node 3, Ruling 174):** at node 1b the zone file stores no floating bounds (seam request req-01M5A43ANDG4G188C55BY7BM3S, Data and Persistence review, then the Owner). No bounds fields were added. A restored floating window opens floating and visible at the default placement, and two restored windows open at the same spot. Within a session the bounds survive, because the read-back takes them from the view.
- T20's guard was removed in node 2 (below).

## Node 2: T20's guard removed

All three tests are in `tests/AiDe.App.Tests/Workbench/FloatGuardTests.cs`. They run through the
real surface: the controller, LayoutPersistence's save and restore, and the render.

| Check | Test | Red | Green |
|---|---|---|---|
| 1. floatPane floats again | `FloatPane_FloatsTheFocusedPane_DrawnInAFloatingWindow_VisibleAndFocusable` | floatpane-real-red.txt (aa64c64e): "floatPane did not float the pane; it said: Floating a pane is not available yet. The pane stays where it is." | floatpane-real-green.txt (650d65c2), 17/17 with WorkbenchControllerTests and FloatRenderTests |
| 2. restore keeps it floating | `APaneSavedFloating_RestoresThroughLayoutPersistence_Floating_DrawnVisibleAndNeverDocked` | restore-floating-red.txt (975f4430): "the restore docked the floating pane; it said: ... Explore was floating in your saved layout. Floating is not available yet, so it is docked in the left zone." | restore-floating-green.txt (aec61ebe), 67/67 with the persistence, perspective and restart tests |
| 3. DOCK2/DOCK3 copy gone | `TheGuardsCopy_DOCK2AndDOCK3_AppearsNowhereInSrcOrDesignMd` | dock-copy-gone-red.txt (629815fa): FloatingRedock.cs:12, :15, DESIGN.md:1398, :1399 | dock-copy-gone-green.txt (e9591175) |
| Suite | App and Core, once each | - | suite-node2.txt: Core 3025 passed / 0 failed / 1 skipped; App 1133 / 1 / 2. The one failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held" at the probe precondition, also red alone (same as node 1b). It is not proven either way. Counts match node 1b: Core minus the 4 deleted copy tests; App minus 2 T20 tests plus 3 new |

### What changed

- `WorkbenchController.cs`, the `workbench.floatPane` case, is back to its pre-T20 form: `WithFocusedStack(id => new LayoutOperation.SetStackState(id, StackState.Floating))`. The `FloatUnavailableAnnouncement` const lost its only user, so it was deleted in the same change (HYG-A).
- `LayoutPersistence.cs`, T20's restore lines only: `RestoreZones(zones)` and `DropAnnouncement(report)`, exactly as before T20. The `FloatingRedock.Dock` call and the report append are gone.
- Deleted: `src/AiDe.Core/Workbench/FloatingRedock.cs` (`FloatingRedock`, `FloatingRedockResult`, `RedockedPane`) and `tests/AiDe.Core.Tests/Workbench/FloatingRedockCopyTests.cs`. Nothing else used them (grep of src and tests). They were deleted in check 3, not check 2, because the copy tests were their last user, so each commit still builds.
- DESIGN.md: the DOCK2 and DOCK3 rows were deleted. No other DESIGN.md line changed.
- T20 tests deleted and named: `FloatPane_RefusesWithAStatedCause_PaneStaysDockedAndFocused` (check 1) and `RestoringAFloatingWindow_DocksThePaneIntoItsKindsZone_DrawnReachableAndReported` (check 2). The class summary now describes the guard's removal.

### Corrections before the reds

- Check 2's first run failed at the wrong line. The first stack in the projection was `zone-center`, which holds only the "welcome" placeholder, and floatPane said "No surface “”.". Then a Factory-admission host held no docked surface at all. The test now uses two unrestricted services, and picks the first stack whose first surface is really in a zone. The committed red fails at the restore line.

### Findings (not fixed, outside this node)

- **floatPane floats the zone's first surface, not the focused tab.** This is Verified by reading `ZoneBackedLayoutService.cs:584`: `SetState` passes `Surfaces().FirstOrDefault()` to `Float`. It is older than T20. With two tabs in a zone and the second focused, the first one floats. Check 1 focuses the first surface, so it does not cover this.
- **floatPane on the empty center's placeholder says "No surface “”."** (Verified by the first run of check 2.) The message names no surface.
- **`ZoneLayoutService.ZoneName` lost a test.** `FloatingRedockCopyTests` also checked that no zone's label equals its enum name. That check went with the file. A grep of tests/ finds no other test that references `ZoneName`, so the label is now unpinned.
- **Stale remark:** `FloatReadBackRestartTests.cs:18` still says LayoutPersistence calls `FloatingRedock.Dock`. That file is outside this node's grant. Node 3 has to change the same test's no-bounds assertion anyway (Ruling 174).

## Node 3: floating bounds (Ruling 174)

The Data and Persistence conditions (Leader DR req-01M5A4911PW38Z4E28QBGXMP7P) are met: four
`double?` fields, read all-or-none, width or height <= 0 is no bounds, `CurrentSchemaVersion` stays 1
(`ZoneLayoutStore.cs:71`), one rectangle per floating `ZoneStackDto` mapping to `StackNode.FloatingBounds`,
units DIPs in virtual-screen coordinates.

| Check | Test | Red | Green |
|---|---|---|---|
| 1. Four optional fields, no schema bump | `tests/AiDe.Core.Tests/Workbench/ZoneFloatingBoundsStoreTests.cs` (6: round-trip; an old file, fixture `tests/AiDe.Core.Tests/fixtures/zone-layout-floating-no-bounds.json`, loads with null bounds; three partial or empty cases; an older reader ignores the fields) | zone-bounds-store-red.txt (47074ff7): "the saved floating stack has no bounds fields" | zone-bounds-store-green.txt (99226792), 20/20 with the zone store and null-floating tests |
| 2. Writer at move or resize end | `tests/AiDe.App.Tests/Workbench/FloatBoundsPersistTests.cs` `AFloatingWindowMovedAndResized_IsSaved_AndAfterARestartOpensAtTheSameRectangle` | bounds-writer-red.txt (3b15c959): "the restart opened the floating window at the default placement (no saved bounds)" | bounds-writer-green.txt (2ac90f20); bounds-writer-neighbours.txt 33/33 |
| 3. Off-screen recovery with a report line | same file, `AFloatingWindowSavedOffEveryDisplay_RestoresAtTheDefaultPlacement_AndTheReportSaysSo` | offscreen-recovery-red.txt (74eba6d2): "the restore kept the off-screen rectangle" | offscreen-recovery-green.txt (f8bee27a), 40/40 with the guard, slot and persistence tests |
| 4. Null "floating" fix stays | `ZoneLayoutWithoutFloatingTests` | - | in zone-bounds-store-green.txt and the suite |
| Suite | App and Core, once each, at f8bee27a | - | suite-node3.txt: Core 3031 / 0 / 1 skipped; App 1135 / 1 / 2. The one failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held" at the probe precondition, also red alone. It is not proven either way. Counts are node 2's plus 6 Core and 2 App tests |

### What changed

- `ZoneLayoutStore.cs`: `ZoneStackDto` gains `floatX`, `floatY`, `floatWidth`, `floatHeight` (`double?`, default null). `ToStackDto` writes `FloatingBounds`. `FromStackDto` reads all four or none, and needs a width and height above 0.
- **The writer** is `WorkbenchAdapter.WatchTheFloatingPanes` and `OnFloatingBoundsUpdated`, beside `WatchTheDockingModel` in `WorkbenchAdapter.cs`. AvalonDock 5.0.0 handles `WM_EXITSIZEMOVE` in `LayoutFloatingWindowControl`. It copies the window's bounds into its panes and raises `FloatingPropertiesUpdated` (read from the decompiled assembly). A bounds change does not raise `LayoutRoot.Updated`, so before this the model learned of a move only at the next unrelated reconcile. `ViewArrangementSignature` now carries each floating pane's bounds, so a move end raises `ViewArrangementChanged`. The shell's existing reconcile (`WorkbenchShell.cs:226`, `:1892`) then writes the bounds into the model, with no render. The save is the shell's debounce on `Manager.LayoutUpdated` (`WorkbenchShell.cs:632`) or the shutdown flush.
- **Render defect fixed (DC-NEW-wl-t21-float-b):** `BuildFloatingWindow` set the bounds on the pane only. AvalonDock places a document floating window from its root panel (`DockingManager.CreateUIElementForModel`), so every floating window since node 1 opened at (0, 0), sized to its content. It now writes the pane and the group. Check 2's window assertion is the control.
- `LayoutPersistence.cs`, the zone restore lines only. Each floating stack that fails `_displayIsConnected` (default `VirtualScreen.IsOnAConnectedDisplay`) loses its bounds before `RestoreZones`. Its title goes into `RestoreResult.RehomedFloating`, and the code is `PartialRestore`. The announcement gains one sentence: `“Sessions” moved onto this display because the saved display is not connected.` It is the const `RehomedFloatingSentenceTail`, in the tree path's words (`LayoutStore.cs:271`). It is the copy proposed in seam request req-01M5A6W4HGN4PREM1CK48Q5X4H, built as that request's fallback.
- `DropTargetResolver.cs`: the `LayoutRect` comment was **not** changed, because the kick came first. It still says "window coordinates", but a floating pane's bounds are DIPs in virtual-screen coordinates.
- `FloatReadBackRestartTests`: the "no bounds in the file" assertion became "the file holds each stack's bounds", and the stale `FloatingRedock.Dock` remark is gone.
- `PerspectiveLayoutSlotTests.TheSchema1DtoConstructors_CarryExactlyTheirFrozenParameters` is outside the grant. Check 1's green made its `ZoneStackDto` case red: 3 frozen parameters against 7 live ones. That is Verified by reading; the test was not run at 99226792. The theory now requires the frozen parameters as an exact prefix, and every added parameter optional, nullable and null by default. The `Frozen` block is untouched. Seam request req-01M5A74MXYR2X0YG0EVE501PMF covers it. The change is its own commit, 60779f03, so it can be dropped.

### Residuals

- A move followed by a crash, before the next layout pass or the shutdown flush, is not saved. The reconcile does not call `MarkDirty`, and `WorkbenchShell.cs` is outside the grant. This is Inferred from reading, not measured.
- AvalonDock's own `KeepInsideNearestMonitor` also clamps a window on render. Check 3 asserts both the model and the drawn window, and they agree here. Mixed-DPI placement remains ADR-0015's spike (Ruling 174).
- The off-screen sentence had two definitions in code: `LayoutStore.cs:271` (the tree path, inline) and `LayoutPersistence.RehomedFloatingSentenceTail`. Node 4 check 2 closed this (below). DESIGN.md:277 still has an older form ("Display 2 is not connected. …"), which node 4 did not change.

## Node 4: the freeze allow-list, malformed zone files, one sentence

Main was merged into the tree by the Leader at 003688ea before the first claim (seam req-01M5A9WFD8G87N4DFPRB0B5C9X).

| Check | Test | Red | Green |
|---|---|---|---|
| 0. The schema-1 freeze is an allow-list (Owner, 14:07Z, on 60779f03) | `tests/AiDe.App.Tests/PerspectiveLayoutSlotTests.cs` `TheSchema1Freeze_RefusesAnAddedFieldNoRulingNamed` (2 planted copies of `ZoneStackDto`: an unruled `floatZ`, and `floatX` renamed to `x` on the wire), plus the existing 6-case theory through the same `FreezeViolations` | freeze-allowlist-red.txt (9e20dcd0): the old rule found nothing wrong with either plant, "Collection was empty" | freeze-allowlist-green.txt (1da9f822), 29/29 slot tests |
| 1. No `"zones"` field, and a floating stack with no `"surfaces"` field, load with a report line (close condition 1) | `tests/AiDe.Core.Tests/Workbench/ZoneLayoutPartlyReadTests.cs` (3, with fixtures `zone-layout-no-zones.json` and `zone-layout-floating-no-surfaces.json`, and a control that an absent `"floating"` is not reported); `tests/AiDe.App.Tests/Workbench/ZoneLayoutPartlyReadRestoreTests.cs` (2, through `LayoutPersistence.Restore`) | partly-read-red.txt (96ffc26a): `ArgumentNullException (Parameter 'source')` in Core and App, both shapes | partly-read-green.txt (7c3a40aa): Core 37/37, App 44/44 with the persistence, slot, bounds and guard tests |
| 2. One definition of the off-screen sentence; the `LayoutRect` comment | `tests/AiDe.App.Tests/Workbench/OffScreenSentenceDefinitionTests.cs` `TheOffScreenSentence_IsDefinedOnce_InCore` | offscreen-sentence-red.txt (c2dd868e): 2 definitions, `LayoutPersistence.cs:30` and `LayoutStore.cs:272` | offscreen-sentence-green.txt (08f62223): App 14/14 (including node 3's exact-sentence test), Core 55/55 |
| Suite | App and Core, once each, at 08f62223 | - | suite-node4.txt: Core 3034 / 0 / 1 skipped; App 1155 / 1 / 2. The one failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, "foreground not held" at the probe precondition, as in nodes 1b to 3. It is not proven either way |

### What changed

- **Check 0.** The freeze rule is now `FreezeViolations(live, frozen)` with a table, `RuledAdditions`. The live constructor must start with the frozen parameters. The parameters after them must be exactly the ruled ones, by name, type and wire name; today that is only Ruling 174's four bounds on `ZoneStackDto`. Each one must also be nullable with a null default. Any other field fails with "added to schema 1 without a ruling". The `Frozen` block is untouched. This check is stricter than the Owner's wording, because it also checks the wire name.
- **Check 1.** `ZoneLayoutStore.Read` reads a missing `zones` field as empty zones, the same way a zone missing from the list already reads (`ZoneLayoutStore.cs:191`). It leaves out a floating stack whose `surfaces` is missing. Either case sets the new `ZoneLoadResult.PartlyRead` flag. `LayoutPersistence.Restore` then adds `PartlyReadSentence`, "Part of your saved layout could not be read and was left out.", with code PartialRestore. The words were accepted provisionally (seam req-01M5A9YZ4PB93ZD06KE5EMC5X8), and the Owner confirms them before the join. DESIGN.md has the row (granted). The restore now builds its announcement from a list of sentences.
- **Check 2.** The const `ZoneLayoutStore.RehomedSentenceTail` has no final period, and each caller adds its own. It lives in `ZoneLayoutStore`, not `LayoutStore`, because the grant was LayoutStore.cs:271 alone, that one line (seam req-01M5A9YZ7H7QPANKV4YMECM31H). The edited line was 272 after the merge, since the literal is on the second line of that statement. The App const `RehomedFloatingSentenceTail` is deleted. The `LayoutRect` summary now says drop hit boxes are in window coordinates, and floating bounds are DIPs in virtual-screen coordinates.
- **DC-NEW-wl-t21-float:** both named siblings are controlled. It stays `partially-controlled`, because reading the code shows a JSON `null` element inside `zones`, `floating`, `tabs` or `surfaces` is still dereferenced. That case is not tested.

## Node 5: the sentence names what was left out, null list elements, unique float ids

| Check | Test | Red | Green |
|---|---|---|---|
| (A) The partial-read sentence names the left-out surfaces by title (Owner, ~15:10Z) | `tests/AiDe.Core.Tests/Workbench/ZoneLayoutPartlyReadTests.cs` `AZoneEntryTheBuildDoesNotKnow_IsLeftOut_AndTheReadNamesItsSurfacesByTitle_InSavedOrder` (fixture `zone-layout-unknown-zone.json`) and `AZoneEntryTheBuildDoesNotKnow_HoldingNoSurface_IsLeftOut_AndTheReadNamesNothing` (fixture `zone-layout-unknown-zone-empty.json`); `tests/AiDe.App.Tests/Workbench/ZoneLayoutPartlyReadRestoreTests.cs` `ALeftOutZoneHoldingTitledSurfaces_IsNamedByTheirTitles_InSavedOrder_AndNeverByAnId`, plus a third row in the plain-form Theory | partly-read-names-red.txt (f6e7842e): the unknown zone was dropped without a word (`PartlyRead` false; no PartialRestore code) | partly-read-names-green.txt (733391b6): Core 33/33, App 16/16 |
| (B) A null element in a saved-layout list loads with the sentence and never throws | `ZoneLayoutPartlyReadTests.ANullElementInASavedLayoutList_IsLeftOut_TheRestLoads_AndTheReadSaysSo` (5 rows: fixtures `zone-layout-{floating,zones,tabs,children,surfaces}-null.json`) and `ANullTabInsideAZoneEntryTheBuildDoesNotKnow_AddsNoTitle_AndNeverThrows` (fixture `zone-layout-unknown-zone-null-tab.json`); the same five fixtures as rows of the App plain-form Theory | null-list-element-red.txt (8c533157): `NullReferenceException` for floating, zones, tabs, surfaces and the null tab; a null in children loaded unreported | null-list-element-green.txt (3b12744f): Core 39/39, App 22/22 |
| (C) Floating-window ids are unique across a restore (close condition 2) | `tests/AiDe.Core.Tests/Workbench/FloatIdAfterRestoreTests.cs` `AfterARestoreThatHoldsFloat1_TheNextFloat_MintsAnIdNotAlreadyInUse` (save, read, `RestoreZones` into a new service, float) | float-id-unique-red.txt (f3d14d3b): `InvalidOperationException: duplicate node id 'float-1'` out of `Apply` | float-id-unique-green.txt (4d43a871): Core 73/73 |
| Suite | Core, at 4d43a871 | - | suite-node5-core.txt: Core 3043 / 0 / 1 skipped (node 4's 3034 plus 9). **The App suite is owed** under rule 20 (USER DEMO IN PROGRESS, 15:34Z); handoff.md has the command |

### What changed

- **(A)** `ZoneLayoutStore.Read` leaves out a zone entry whose id names no zone of this build, and reports it. Before this it was dropped without a word. `ZoneLoadResult.LeftOutTitles` holds the saved titles of the left-out surfaces, in saved order, through splits. `LayoutPersistence.Restore` says `PartlyReadNamingPrefix` + the titles joined with ", " + "." when there are titles, and `PartlyReadSentence` when there are none. The words are the saved `title` (`ZoneSurfaceDto.Title`), never an id. A renamed tab's display name is not in the file; seam request req-01M5ACJG6ZCRGJ3BDBPCYW0WJJ. DESIGN.md's row now carries both forms.
- **(B)** Each list `Read` restores from goes through one helper, `ZoneLayoutStore.Present`. It drops the JSON nulls and reports a partial read before any element is touched. `TitlesIn` skips its own nulls. **DC-NEW-wl-t21-float** is now `controlled` for `ZoneLayoutStore.Read`. The other JSON stores in `src/` are not swept.
- **(C)** `ZoneBackedLayoutService.Float` skips a `float-N` id already in use. The counter still starts at 0, so after a restore holding `float-1` the next float gets `float-2`.

## Node 6: the owed App suite, the focused tab, floating ids in use

| Check | Test | Red | Green |
|---|---|---|---|
| (0) The owed App suite at node 5's head (f21fdf95) | - | - | suite-node5-app.txt (db19c117): App 1165 passed / 1 failed / 2 skipped. The one failure is `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` at its precondition ("foreground not held"), as in nodes 1b to 4. Not a desktop-hold result. The +10 over node 4's 1155: node 5's 1 Fact and 6 Theory rows, and 3 Facts from main (`AReadBackPendingAtDisposalEndsQuietlyTests.cs`). No App red from node 5 |
| (A) floatPane floats the focused tab, not the first (close condition 3) | `tests/AiDe.Core.Tests/Workbench/FloatFocusedTabTests.cs` `AZoneWithTwoTabs_TheSecondFocused_FloatPane_FloatsTheSecond` | float-focused-tab-red.txt (2a590c8a): the floating window held `focus-a`, the first tab | float-focused-tab-green.txt (3c433c3f): Core 1/1 |
| (B) A saved floating stack whose id is in use loads with the partial-read sentence and never throws at restore | `tests/AiDe.Core.Tests/Workbench/ZoneLayoutFloatingIdInUseTests.cs` `AFloatingStackWhoseIdIsInUse_IsLeftOut_NamedByTitle_AndTheRestoreNeverThrows` (2 rows: fixtures `zone-layout-floating-duplicate-id.json`, two stacks `float-1`; `zone-layout-floating-reserved-id.json`, a floating stack `zone-left`) | floating-id-in-use-red.txt (5327d810): `InvalidOperationException: duplicate node id 'float-1'` and `'zone-left'` at `Layout.AssertInvariant` (`LayoutModel.cs:199`), reached from `service.Current` after `RestoreZones` | floating-id-in-use-green.txt (51ae8820): Core 20/20 with the partly-read, float-id and floating-bounds siblings |
| (C) A native float after a restart never takes a restored window's `float-view` id | `tests/AiDe.App.Tests/Workbench/FloatViewIdAfterRestartTests.cs` `ANativeFloatAfterARestart_NeverMintsTheIdOfARestoredFloatingWindow` | float-view-id-red.txt (f5fc73b0): "the native float minted an id a restored window holds: float-view-1000, float-view-1000" | float-view-id-green.txt (9973a1ce): App 8/8 with the float render, guard, read-back and bounds tests |
| Suite | Core and App, at 51ae8820 | - | suite-node6.txt: Core 3046 / 0 / 1 skipped (node 5's 3043 plus 3: 1 in (A), 2 in (B)). App 1166 / 1 / 2 skipped (1165 plus 1 in (C)); the one failure is the SolutionTreeChord precondition, as at (0). No desktop-hold result occurred in this node |

### What changed

- **(A)** `ZoneBackedLayoutService.SetState` floats `FocusedIn(zone.Content)`: a stack's active tab. A split renders as one tab strip whose active tab is its first (`ZonesToTree.StackFromContent`), so a split gives its first surface, as before. An empty Center still gives the empty id, so the "No surface “”." copy is unchanged (out of scope).
- **(B)** `ZoneLayoutStore.Read` keeps a set of node ids, seeded with the frame's six ids (`ZonesToTree` Left/Center/Right/Bottom stack ids, `frame-cols`, `frame-root`). A floating stack whose id is already in the set is left out, the read is partial, and its surfaces are named by their saved titles. The first stack with an id is kept. **DC-NEW-wl-t21-float-c** records the class.
- **(C)** The code exists; the Leader's grep missed it because the literal `float-view` is never written. `WorkbenchAdapter.MapFloatingWindow` (`WorkbenchAdapter.cs:1008` at f21fdf95) called `NewNodeId("float")`, and `NewNodeId` (`:1102`) builds `$"{prefix}-view-{N}"` from the static `_nodeSeq` (`:1101`). The counter is shared with the split and stack ids and restarts with each process, and the zone file keeps a floating stack's id. The red sets the counter by reflection to the value at which the next native float mints the restored id: that is the restart condition, made deterministic. Now `ReadLayoutFromView` passes the model's node ids (`_service.Current.AllStacks()`) to `MapFloatingWindow`, and `NewFloatId` skips every id in that set and every id this read has taken. Node 5's README and hand-off line on this was right; it needs no correction.

## Node 7: close condition 4, a live notebook floats and redocks (Ruling 171 (3))

The node was held by rule 19 for the WL-1b join (19:04Z to 19:31Z) and kicked at 19:46Z. See handoff.md.

| Check | Test | Red | Green |
|---|---|---|---|
| (A) A notebook floated mid-run keeps its model and its one attempt, and the turn completes in the floating window | `tests/AiDe.App.Tests/Workbench/ALiveNotebookFloatsAndRedocksTests.cs` `ANotebookFloatedMidRun_KeepsItsModelAndItsOneAttempt_AndTheTurnCompletesInTheFloatingWindow` | live-float-red.txt (0097164f): "the model floats the notebook, but the view shows 0 floating window(s)". No controller-routed layout command renders | waits for seam req-01M5ATF6PNMEW0SX2KKZA34D4D (DockHost.cs:73) |
| (B) Redock mid-run and after the run, by the floating tab's Move to, Center | drafted in n8-draft-B-C-tests.txt | not run | - |
| (C) A floating live notebook survives a restart | drafted in n8-draft-B-C-tests.txt | not run | - |
| Suite | Core and App, at 0097164f | - | suite-node7.txt: Core 3046 / 0 / 1 skipped. App 1214 / 3 / 2 skipped: (A)'s red and the two foreground tests (attended list). No desktop-hold result |
