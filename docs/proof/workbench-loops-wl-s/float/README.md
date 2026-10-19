---
id: wl-s-float-proof
title: "WL-S T21 float - proof pack"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "Ruling 171 (2) node 1: the WebView2 float probe (no crash), floating stacks drawn as AvalonDock floating windows, and floating surfaces redocked, closed and activated; read-back (check 3) not started, stopped at the Leader's kick."
---

# T21 float, node 1 (Ruling 171 (2))

Stopped at the Leader's rule-16 kick (about 13:01Z). Checks 0-2 are done. Check 3 is not started.
See handoff.md.

| Step | Test | Red | Green |
|---|---|---|---|
| 0. WebView2 float probe | `tests/AiDe.App.Tests/Workbench/WebView2FloatProbeTests.cs` `FloatingAndRedockingTheCanvasAndTheComposer_KeepsTheProcessAndBothBrowsersAlive` | n/a (a measurement) | webview2-float-probe.txt (18c844c2) |
| 1. Render Layout.Floating | `tests/AiDe.App.Tests/Workbench/FloatRenderTests.cs` `AFloatedPane_IsDrawnAsADockLibraryFloatingWindow_VisibleFocusableAndHoldingItsContent` | render-floating-red.txt (49f39e6c) | render-floating-green.txt (591f94b1) |
| 2. Move, close, activate a floating surface | `tests/AiDe.Core.Tests/Workbench/FloatingMoveCloseTests.cs` (4 tests) | move-close-floating-red.txt (44da2272) | move-close-floating-green.txt (11b2216e) |
| 3. Read-back and restart | not started | - | - |

## Probe result (step 0)

The process **survived**. The app's real CanvasSurface and a real ComposerSurface were floated into
AvalonDock 5.0.0 floating windows and redocked through a fresh `Manager.Layout`, which is the same
re-parent Render does. Each browser ran script (6*7 = 42) docked, floated and redocked, and the
canvas initialised once. There was no crash, so no Owner escalation. Limits: the host window was
off-screen and no pixels were sampled. Separately, AvalonDock's own `LayoutDocument.Dock()` left the
canvas floating once its original pane had emptied. The product does not use `Dock()`.

## What changed

- `WorkbenchAdapter.Render` draws each non-empty `Current.Floating` stack as a `LayoutDocumentFloatingWindow` (`BuildFloatingWindow`). The window opens at the stack's `FloatingBounds`, or at 640x420 offset 48 into the workbench when it has none. Content is reused, never rebuilt.
- `ZoneLayoutService.MovePane`, `ClosePane` and `Activate` look in the floating stacks before they refuse. Redock makes the surface the target zone's active tab, with the same expand and extent floor as a docked move. "is not docked" now applies only to a surface the layout does not hold.
- Check 1's test was corrected after its red: `Window.GetWindow` returns the workbench window for floated content (the logical tree). The assertion is now the HWND chain (user32 `IsChild`). The red failed earlier, at `Assert.Single(Manager.Layout.FloatingWindows)`, either way. render-floating-green.txt records this.

## Not done in this node

- **Check 3**, read-back: `ReadLayoutFromView` still returns null when the view has floating windows (WorkbenchAdapter.cs, the `root.FloatingWindows.Any()` line), and `TryMapByPosition` does not take `tree.Floating`. Plan: handoff.md.
- **Suite:** not run at the stop, per the kick. Adjacent runs only: App WorkbenchAdapter/FloatGuard/FloatRender/probe 25/25; Core *Zone*/*Float*/*Layout* 123/123.
- **Seam request** req-01M5A43ANDG4G188C55BY7BM3S: the zone file stores no floating bounds (ZoneStackDto), so a floating window's position does not survive a restart. Keeping it is a schema addition (Data and Persistence review).
- T20's guard still stands (node 2). `workbench.floatPane` still refuses, and the restore still docks floating panes. Through the controller and LayoutPersistence nothing floats yet.
