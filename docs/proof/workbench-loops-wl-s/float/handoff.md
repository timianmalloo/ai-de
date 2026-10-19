---
id: wl-s-float-handoff-n1
title: "WL-S T21 float - node 1 hand-off"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-19
summary: "T21 node 1 stopped at the Leader's kick: probe (no crash), check 1 and check 2 green; check 3 (read-back, restart) not started, with the plan."
---

# T21 node 1 hand-off

**Stopped at the Leader's rule-16 kick.** Branch `wls-float`, tree `C:\Projects\ai-de-wls-float`.
All claims are released. The tree is clean apart from these proof files.

## Done (green, committed)

- Step 0, the WebView2 float probe: **no crash**. Canvas and composer both survive an AvalonDock float and redock (18c844c2, webview2-float-probe.txt).
- Check 1, render floating: red 49f39e6c, green 591f94b1.
- Check 2, move, close and activate floating: red 44da2272, green 11b2216e.

## Red / half done

Nothing. Check 3 was not started. Its test file was claimed and released unwritten.

## Next step: check 3 (read-back and restart)

1. Test (App, `Sta.Pump` with a DockingManager, as in FloatRenderTests): float `sessions` through the service and Render. Float `graph` with AvalonDock's own `LayoutDocument.Float()`, the native gesture only read-back can bring in. Then `ReadLayoutFromView()` is non-null, and `service.ReconcileFromView(view)` is true with both surfaces in `Zones.Floating`. Save with `new ZoneLayoutStore(path).Save(service.Zones)`, read with `Read(allIds, empty)`, `RestoreZones` into a new service, and Render into a new DockingManager (`main.Content = manager2`). Two floating windows, visible, each holding its content. Use the HWND check: make `FloatRenderTests.HostedBy` internal. Expected red: `Assert.NotNull(view)`, because ReadLayoutFromView bails on any floating window.
2. `WorkbenchAdapter.ReadLayoutFromView`: replace the `root.FloatingWindows.Any()` bail and the `Floating = []`. For each `LayoutDocumentFloatingWindow`, map its one non-empty `LayoutDocumentPane` to a floating `StackNode`, keeping the pane id when it is a floating id. Take bounds from `FloatingLeft/Top/Width/Height`. Return null for an anchorable window, more than one pane, or the placeholder.
3. `ZoneBackedLayoutService.TryMapByPosition`: set `result = result with { Floating = tree.Floating }` before the surface-set guard. Without it, a natively floated pane fails the guard.
4. Restart through `LayoutPersistence` still docks floating panes back (T20's `FloatingRedock` call). That is node 2's red-first removal. Check 3's restart leg therefore calls the store and `RestoreZones` directly, and must say so.

## Open

- Seam request req-01M5A43ANDG4G188C55BY7BM3S: floating bounds are not stored in the zone file (schema addition, Data and Persistence review).
- Suite not run (kick). The next node runs App and Core once each, with SolutionTreeChordTests alone if it flakes.
