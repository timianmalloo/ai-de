# Brief — T21 node 11 `wl-layout-gate-terminal-move` (the terminal move, restored on the route) · Claude Code · Sonnet · node 11

**Goal:** bring back the case node 10 dropped (Owner, 2026-10-20). Node 10 converted
`tests/AiDe.App.Tests/ZoneWorkbenchAdapterTests.cs::MovingASurfaceToAnotherZone_LosesNoPane_AndMovesOnlyThatSurface`
(`:254`) to the route, but it swapped the moved surface from `terminal-1` to `session:s2`. That changed what the test
proves so that it could meet the gate, which is the shape rule 23 forbids. The `session:s2` test stays. This node adds
the terminal case back as **its own test** beside it.

**What is known (read, do not re-derive):**
- The fixture: `WithDockHost` (`ZoneWorkbenchAdapterTests.cs:227`) over `FloatRenderTests.HostInto`
  (`tests/AiDe.App.Tests/Workbench/FloatRenderTests.cs:85`), a Factory `DockHost` drawn in an offscreen window.
- The route: `host.Controller.Move(surfaceId, DropTarget)` (`src/AiDe.App/Workbench/WorkbenchController.cs:251`).
- Why node 10 swapped: a Factory host's Bottom zone is collapsed, and a collapsed zone parks its terminal **unbuilt**
  (Ruling 88). Node 10's reasons are in `docs/proof/workbench-loops-wl-s/layout-gate/r2/R2b-sweep.txt`, line 5.
- The gate: `tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs`. The new test must not trip it.

**This node, in this order (one check, rule 16):**
- **(M1) The test.** Add one test to `ZoneWorkbenchAdapterTests.cs`. On a `WithDockHost` host, it moves `terminal-1`
  from the collapsed Bottom into Left through `host.Controller.Move`, with no by-hand render after the command. It
  asserts:
  - no pane is lost (the same surfaces as before, by the same oracle the `session:s2` test uses);
  - only `terminal-1` moved (its zone is Left; every other surface's zone is unchanged);
  - **`terminal-1` is built once it lands in a visible zone**: it has drawn content (`ContentFor("terminal-1")` is not
    null, and it is in the drawn layout). This is the point of the case: a parked, unbuilt terminal that the user moves
    must appear.
  **Mutation red, where the claim allows it:** with the `LayoutApplied` wiring in `src/AiDe.App/Workbench/DockHost.cs`
  removed in a scratch edit, run the test and save the result (`{PROOF}/M1-mutation.txt`). Then restore the file;
  `git diff --stat src/` must be empty before every commit. If the test stays green under the mutation, say why its
  claim does not depend on that render.
  **If the test fails on the route:** it stays as written and failing. Record it as **NOT CHECKED: finding T21n11-F1**
  in every suite result, never as a skip (no `Skip =`), and never on the gate's exception list. Name it first in the
  done mail, with the failing assertion's message. **No src fix.** Save the run to `{PROOF}/M1-run.txt` either way.
- **Then** run the gate, the whole App and Core suites (mandatory, Owner), rule 22's ratchet, and rule 12.

**Hard stop (Owner, 2026-10-20; a trial of a kick-text control, not the user's hook).** Your budget is **40 tool calls /
80k**. At either cap, stop and report, **even before the suites**: commit what you have, write `{PROOF}/README.md` with
what is done and what is not, send the done mail labelled `PARTIAL at the cap`, and end your turn. The Leader's kick
comes at about 11 min (80k ÷ 7.0k tokens/min); the kick means the same as the cap.

**Known App results on this machine.** Name these as known, never as new failures:
- `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`: record it as **NOT CHECKED: the OS
  reports the left mouse button down** (Owner, 2026-10-20).
- `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` and
  `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`: known failures (T31, N=10).
Any other failure is yours: run it alone and report both results. Never inject mouse or keyboard input outside a
test's own window, and never call `SendInput` or `mouse_event`.

**Run `coord claim` before the first edit of each file, even when the hook reports the paths free** (Owner,
2026-10-20).

**Tier** T2 · **fan-out** 0 · **budget** 40 tool calls · 80k, hard stop. **Kick** by mail at about 11 min.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE`: finish the run in flight, commit what is green, mail the
Leader `T21 n11 HELD by USER DEMO at <time>`, and end your turn with the same line.

## Owned authored paths
`tests/AiDe.App.Tests/ZoneWorkbenchAdapterTests.cs` (one new test; the existing tests unchanged). `{PROOF}/**`. A scratch
edit of `src/AiDe.App/Workbench/DockHost.cs` that exists only during the mutation run and is **never committed**.
**No src change.**
