---
id: proof-wl-focus-oracle-route
title: "Evidence: T41 - the layout gate counts focus and live-region reads; ConsoleSplit takes the route"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [workbench-loops, evidence, gate, focus, live-region]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-19"
summary: >-
  The class gate now reads keyboard-focus and live-region oracles. On today's tree it named one test, ConsoleSplit
  CollapsingTheLeft. That test now closes by the route. Docs-and-test only; no src change. No cause is claimed for focus (none).
---

# T41 `wl-focus-oracle-route`

## G1 - the gate

- Oracle set widened in `LayoutOraclesDriveTheCommandRouteTests`: `Keyboard.FocusedElement`,
  `FocusManager.GetFocusedElement(`, `IsKeyboardFocusWithin`, `.LiveRegion.Text`.
- **Red** (`G1-red.txt`, commit `c7cfee1c`): 230 files, 2 tests read an oracle after a layout command, 1 refused:
  `Sessions/ConsoleSplitPlacementTests.cs::CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen` (command
  `.Apply(new LayoutOperation.CloseSurface`, by-hand render, then `.LiveRegion.Text`).
- **Mutation and evasion** (planted shapes in the gate's fixture): a focus read after an off-route close; a focus read
  reached through a same-file helper (`LandedInside`, which holds the `FocusManager.GetFocusedElement` read in a local
  `read`); a live-region read after an off-route close. With the old oracle the planted test fails
  (`G1-mutation-evasion-red.txt`: only the first six shapes found). With the widened oracle it passes
  (`G1-mutation-evasion-green.txt`, 10 command-then-oracle tests).
- **Residual:** a focus read in another file's helper, or reached through a delegate parameter, is not seen. The
  alias test is a same-file helper; a read stored in a local and used later is still textually a read.
- The live-region read the tests use is `shell.LiveRegion.Text`.

## G2 - recall search (independent of the gate)

`g2-recall.py` -> `G2-hits.txt`: every App test with a focus or live-region read after a `Render()`, an off-route
`LayoutOperation`, a `workbench.*` execute or a route helper. **4 tests:**

| Test | Verdict |
|---|---|
| `ConsoleSplitPlacementTests.cs:162` CollapsingTheLeft... | Converted (G3). |
| `ConsoleSplitPlacementTests.cs:98` WithTheNotebookInTheCenter_... | Already on the route (`StepBackByTheRoute`, `shell.Execute`). No conversion. |
| `WorkbenchShellTests.cs:103` Announcements_ReachTheLiveRegion... | `shell.Controller.Execute` is the route; no by-hand render. No conversion. |
| `LayoutOraclesDriveTheCommandRouteTests.cs:362` | The gate's own planted fixture string, not a test of the product. |

**`TheFactoryLanding_...` (`SessionDocumentPlacementTests.cs:99`) is not the same shape.** It opens the session by
`shell.OpenSessionDocument` and calls `PerspectiveShell.FocusLanding`, then reads `Keyboard.FocusedElement` (`:123`).
It applies no layout command off the route and renders no adapter by hand, so it is not a hit. Its one recorded failure
(T36 run 6) is a focus-under-load event, not a route gap.

## G3 - conversion

`ConsoleSplitPlacementTests.CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen`: the `Service.Apply(CloseSurface)`
plus `Adapter.Render()` became `FloatRenderTests.CloseByTheRoute(shell.Factory, ConsoleId(...))`. No rename, no wrapper.

**T41-F1.** On the route, the same test can still fail with `focus after the close is on (none), not the landing`.
Seen once, in `G3-green.txt` (a 4-test filter run, before the alone loop). The default input state is not at fault, so
it is not marked NOT CHECKED, and it is not a src finding this node may fix. The route does not remove the (none).

## G4 - measurements (no cause claimed)

- Alone, 10 times (`g4-alone-loop.sh`, `G4-alone.txt`): **10/10 passed**, as T36's 10/10.
- Core: 3121 passed, 1 skipped, 0 failed (`suite-core.txt`).
- App: 1275 passed, 4 skipped, 0 failed (`suite-app.txt`; one run). The in-suite rate comes from the join's recount.
- The one (none) on the route was in a short filtered run (`G3-green.txt`), beside T36 and T40's in-suite failures.

## G5 - register

`DC-NEW-wl-t21-float-d`: a new instance line, the widened control, and the residual.
