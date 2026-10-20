# Brief — T21 node 10 `wl-layout-gate-r2` (ContentFor and ActiveSurfaceId as view oracles) · Claude Code · Sonnet · node 10

**Goal:** settle the Owner's R2 on T21 node 9's gate (Owner, 2026-10-20). Two adapter reads were left out of the gate's
view-oracle set, `WorkbenchAdapter.ContentFor` and `WorkbenchAdapter.ActiveSurfaceId`. Both read AvalonDock's drawn
tree, not the model: `src/AiDe.App/Workbench/WorkbenchAdapter.cs:944-948` (the `Manager.Layout` descendants, then
`_parked`) and `:977-979` (`Manager.Layout.ActiveContent`). So they are view state. This node counts the App tests that
read either one after a layout command **with a by-hand render between the command and that read**, then acts on the
count.

**What is known (read, do not re-derive):**
- The gate: `tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs`. Its rule, token sets and
  residuals are in its summary comment. Its exception list is empty and stays shrink-only.
- Node 9's proof: `docs/proof/workbench-loops-wl-s/layout-gate/` (`README.md`, `V2b-recall.txt`, `V3-sweep.txt`). Its
  route helpers: `tests/AiDe.App.Tests/Workbench/FloatRenderTests.cs:111-142` (`FloatByTheRoute`, `CollapseByTheRoute`,
  `CloseByTheRoute`, `StepBackByTheRoute`) and `ExpandByTheRail`.
- Owner rulings on node 9: R1 (other-file helpers and delegates) is a stated residual, already in the gate's comment;
  R3 and R4 accepted. Do not reopen them.

**This node, in this order (two checks, rule 16):**
- **(R2a) Count, by a search independent of the gate.** Do not reuse the gate's regexes or its scan. List every App test
  that reads `ContentFor(` or `ActiveSurfaceId` after a layout command. For each one, say whether a by-hand render lies
  between the command and that read, by `file:line` of the command, the render and the read. Save the search command(s)
  and the list to `{PROOF}/R2a-count.txt`, ending with one line: `count = <n>` (tests with a by-hand render between).
- **(R2b) Act on the count.**
  - **count = 0:** add one sentence to the gate's summary comment, in its residuals: these two reads are not in the
    oracle set, with the count and the date (`0 tests on <sha>, T21 n10`). Record it in the README. No other change.
  - **count > 0:** add both reads to the gate's view-oracle pattern. **Red first:** run the gate and see it name exactly
    those tests (`{PROOF}/R2b-red.txt`). Then convert each named test as node 9's V3 did, with the same rules:
    - a conversion that only renames, wraps or moves a call so that the regex stops matching is **refused** (rule 23);
      only a test that now takes the user's route counts;
    - each converted test still proves its own claim: run it alone, red by mutation where its claim allows;
    - a test that fails once it takes the route is a **product finding**: keep it converted and failing, give it the id
      `T21n10-F<k>`, record it in every suite result as **NOT CHECKED: finding T21n10-F<k>**, never as a skip, and
      **never** on the exception list. Name it first in the done mail. Never fix src.
    Save one line per test to `{PROOF}/R2b-sweep.txt`. The gate is then green (`{PROOF}/R2b-green.txt`).
- **Then** the whole App and Core suites (mandatory, Owner), rule 22's ratchet, and rule 12. Update the Status line of
  DC-NEW-wl-t21-float-d to give R2's outcome.

**Known App results on this machine.** Name these as known, never as new failures:
- `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`: record it as **NOT CHECKED: the OS
  reports the left mouse button down** (Owner, 2026-10-20).
- `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` and
  `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`: known failures (T31, N=10).
Any other failure is yours: run it alone and report both results. Never inject mouse or keyboard input outside a
test's own window, and never call `SendInput` or `mouse_event`.

**Run `coord claim` before the first edit of each file, even when the hook reports the paths free** (Owner,
2026-10-20).

**Tier** T2 · **fan-out** 0 · **budget** 60 tool calls · 100k (Owner). **Kick** by mail at about 12 min: node 9 ran at
about 8.1k tokens/min, so 100k is reached at about 12 min (the kick ladder, DC-NEW-wl-leader2-b).
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE`: finish the run in flight, commit what is green, mail the
Leader `T21 n10 HELD by USER DEMO at <time>`, and end your turn with the same line.

## Owned authored paths
`tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs`. The test files R2b converts, each named in
`{PROOF}/R2b-sweep.txt`, and only the tests R2b's red named in them. `{PROOF}/**`. The DC-NEW-wl-t21-float-d entry's
Status line in `docs/lessons/defect-classes.md`. **No src change.**
