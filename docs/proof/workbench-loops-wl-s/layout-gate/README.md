---
id: wl-s-layout-gate-readme
title: "WL-S T21 node 9 - the layout-command class gate"
type: proof-pack
status: active
owner: "@timianmalloo"
links:
  - { to: architecture, rel: implements }
review-by: 2027-01-20
summary: "T21 n9: LayoutOraclesDriveTheCommandRouteTests fails when an App test issues a layout command off the user's route, renders the adapter by hand, then reads the drawn layout. Red on 8 tests (6db0cad4), 11 after V2b tightened the oracle set (343dbe07); all 11 converted to the route, 0 exceptions, 0 product findings."
---

# T21 node 9 - the layout-command class gate

The gate is `tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs`. Its summary comment holds the rule (V1) and the four token sets.

| Step | Result | File |
| --- | --- | --- |
| V2 red, empty exception list | 8 tests named | `V2-red-today.txt` |
| V2 mutation red (node 2's deleted test) | named | `V2-red-node2.txt` |
| V2 evasion red (render wrapped in a local function) | named | `V2-red-evasion.txt` |
| V2b recall (two search lenses, no gate regex) | 56 candidates: 11 matched, 45 out of class, 3 missed at first | `V2b-recall.txt`, `V2b-search.py`, `V2b-candidates.txt` |
| V2 re-run after tightening | 11 tests named | `V2-red-tightened.txt` |
| V3 sweep | 11 converted, 0 listed, 0 findings | `V3-sweep.txt` |
| V3 green | gate green, exception list empty | `V3-green.txt` |

## How the rule was tightened (V2b)

The first gate read `Manager.Layout`, `FloatingWindows`, `HostedBy` and `Drawn`. Three tests it missed read a drawn Center empty copy (`CenterEmpty()`) or a drawn rail (`RailVisible()`). Both are now oracles. `ContentFor` and `ActiveSurfaceId` are not (they read the adapter's cache and active id); that is a residual for the Owner's ruling, with four borderline out-of-class tests listed in `V2b-recall.txt`.

Lens A of the search listed 54 lines and missed one of the three (it had no rail read). Lens B (a hand render plus any non-AddSurface change, no read filter) found it and nine more, all out of class.

## Limits of the gate (also in its comment)

- A source scan: text order, not control flow.
- It follows same-file helpers and the test's own local functions and lambdas to three levels. A render or command in another file's helper, or passed in as a delegate, is not seen.
- A command through the route is not a layout command here: `host.Controller.Execute`, `Controller.Move`, `shell.Execute`, the palette.

## The conversions

Helpers in `FloatRenderTests.cs` (`HostInto`, `FloatByTheRoute`, `CollapseByTheRoute`, `CloseByTheRoute`, `StepBackByTheRoute`, `ExpandByTheRail`) run the route and render nothing themselves. 8 of the 11 tests go red when `DockHost`'s `LayoutApplied` wiring is removed (mutation files `V3-*-mutation-red.txt`; src restored). The other three cannot, for the reasons in `V3-sweep.txt`.

## R2 (T21 n10): ContentFor and ActiveSurfaceId are view oracles

Both read `Manager.Layout` (then the parked content), so the Owner ruled them view state. R2a counted, by a line scan that does not use the gate's regexes (`r2/R2a-search.py`, `r2/R2a-count.txt`), the App tests that read either one after a layout command with a by-hand render between: **count = 6**. R2b added both reads to the gate's oracle pattern; red named exactly those 6 (`r2/R2b-red.txt`); each now takes the route, with no rename (`r2/R2b-sweep.txt`); the gate is green (`r2/R2b-green.txt`). A mutation that unwires `LayoutApplied` turns 1 of the 6 red (`r2/R2b-mutation-red.txt`), and the sweep says why the other 5 are not sensitive to it. Residual unchanged: a command or render in another file's helper (`ChecklistHarness`, `ComposedCoding`) is not seen by the gate or by R2a.
