---
id: wl-s-float-proof
title: "WL-S T21 float - proof pack"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "Ruling 171 (2) nodes 1 and 1b: the WebView2 float probe (no crash); floating stacks drawn, redocked, closed and activated; floating windows read back and restored floating after a save and restore through the zone store; a zone file with no floating field restores without throwing."
---

# T21 float, nodes 1 and 1b (Ruling 171 (2))

Node 1 did steps 0 to 2 and stopped at the Leader's 13:01Z kick. Node 1b did check 3 and the null
check. Both are green. See handoff.md for node 2.

| Step | Test | Red | Green |
|---|---|---|---|
| 0. WebView2 float probe | `tests/AiDe.App.Tests/Workbench/WebView2FloatProbeTests.cs` `FloatingAndRedockingTheCanvasAndTheComposer_KeepsTheProcessAndBothBrowsersAlive` | n/a (a measurement) | webview2-float-probe.txt (18c844c2) |
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
- **Placement:** the zone file stores no floating bounds (seam request req-01M5A43ANDG4G188C55BY7BM3S, Data and Persistence review, then the Owner). No bounds fields were added. A restored floating window opens floating and visible at the default placement, and two restored windows open at the same spot. Within a session the bounds survive, because the read-back takes them from the view.
- T20's guard still stands (node 2). `workbench.floatPane` still refuses, and a restart through LayoutPersistence docks floating panes.
