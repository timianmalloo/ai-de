# Brief — T21 node 9 `wl-layout-gate` (the layout-command class gate) · Claude Code · Sonnet · node 9

**Goal:** a gate fails whenever an App test checks the drawn layout after a layout command without reaching that
command through the user's route. This is item 2 of T21's node 8 hand-off
(`docs/proof/workbench-loops-wl-s/float/handoff.md`, "Next (node 9)"). It sits beside
`tests/AiDe.App.Tests/Workbench/Composer/ComposerContentOraclesDriveTheGestureTests.cs` (DC-NEW-wl-t28-compiled's gate)
and controls DC-NEW-wl-t21-float-d's class. Owner, 2026-10-20: a gate red on today's shape, red first, plus a sweep of
the existing view-state tests. Brief approved with amendments A-C (Owner, 2026-10-20).

**What is known (read, do not re-derive):**
- The failure this prevents: T21 node 2's `FloatPane_FloatsTheFocusedPane_...` passed while the palette drew 0 windows.
  It called the adapter's `Render()` by hand after the command (T21 README, "Node 8"). It was deleted at `d82df79e`;
  its source is `git show d82df79e^:tests/AiDe.App.Tests/Workbench/FloatGuardTests.cs`.
- The route a passing test takes: `tests/AiDe.App.Tests/Workbench/LayoutCommandsRenderTests.cs:19-23` and `:124-171`.
  It builds `DockHost.Create`, then a `CommandPalette(host.Controller.Execute, …)` (or `host.Controller.Execute`), and
  its oracle is the drawn view (`DockHost.Manager`'s layout, the floating windows). The product renders through
  `WorkbenchController.LayoutApplied` (`src/AiDe.App/Workbench/WorkbenchController.cs:739,755`), which `DockHost.Create`
  wires to the adapter's `Render`.
- Today's tree calls `Adapter.Render()` by hand in at least these files: `PerspectiveShellTests.cs` (:73-75, :121-123,
  :132, :146, :180, :191, :216), `Sessions/ConsoleSplitPlacementTests.cs` (:88, :122, :153, :162, :198),
  `DpiAndResizeTests.cs:95`, `OpeningATerminalWithNoneOpenTests.cs:105`. Some of these renders are setup, not a step
  after a command. The gate must tell the two apart; this is the node's main design work.

**This node, in this order (four checks, rule 16):**
- **(V1) Define the rule in writing first.** In the gate's summary comment, name each token set as a regex, the way the
  composer gate does:
  - the **view oracle**: what reads the drawn layout;
  - the **layout command**: what changes the layout model;
  - the **route**: the palette, or `Controller.Execute` on a `DockHost`'s controller;
  - the **by-hand render**: what renders the adapter outside the route.
  The rule: a test that issues a layout command and then reads a view oracle reaches the command by the route, and does
  not render by hand between the command and the oracle. A by-hand render before any command is setup and is allowed.
  A test that cannot meet the rule without a src change goes on a **shrink-only** exception list, with its reason. An
  entry that now meets the rule, or no longer exists, fails the gate.
- **(V2) Red on today's shape, first.** Write the gate with an **empty** exception list and run it on the tree as it
  stands. It must fail, naming each test that breaks the rule, by `file::test` (`{PROOF}/V2-red-today.txt`). If it names
  none, the rule is too loose: tighten it and say how in the README. Then the mutation red: restore node 2's deleted test
  as a scratch file and show the gate names it (`{PROOF}/V2-red-node2.txt`). Then the **evasion red** (rule 23,
  DC-NEW-wl-t30-gate-evasion; Owner): in a scratch copy of one named test, wrap its by-hand render in a local helper
  with a new name, and show the gate still names the test (`{PROOF}/V2-red-evasion.txt`). If it cannot, make the gate
  follow one level of same-file helper calls, as the composer gate does; if that is not enough, state the limit in the
  gate's own summary comment as a residual and name it in the done mail for the Owner's ruling. Delete every scratch
  file and never commit one.
- **(V2b) Recall, a separate step (Owner).** List every App test that reads drawn layout state after a layout command.
  Find them by a search that does **not** reuse the gate's regexes: for example every test file that references
  `DockHost` or a layout type and also issues a command. Classify each test: **matched** by the gate, **out of class**
  (with the reason), or **missed** by the gate. Save the list to `{PROOF}/V2b-recall.txt`, with the search command. A
  missed test means the rule is tightened, and V2 re-run, before V3 starts. No test is converted on V2b's word alone.
- **(V3) The sweep, of the tests V2 named only (Owner).** For each, choose one:
  - **convert it** to take the user's route (a test change only; the test must still prove its own claim, so run it
    alone, red by mutation where its claim allows). A change that only renames, wraps or moves a call so that the
    regex stops matching is **refused** (rule 23): only a test that now takes the route counts as converted;
  - **list it** as an exception, with a one-line reason that names what stops the conversion. The list holds only
    entries that are correct by their stated reason.
  Save one line per test to `{PROOF}/V3-sweep.txt`: the test, convert or list, and the evidence. The gate is then green
  (`{PROOF}/V3-green.txt`).
  **A product finding:** a test that passed only because it rendered by hand, and fails when it takes the route, is a
  product defect, as in node 2. Keep it converted and failing. Give it a finding id (`T21n9-F<k>`), record it in every
  suite result as **NOT CHECKED: finding T21n9-F<k>**, never as a skip (no `Skip =`), and **never** put it on the
  exception list. Name it first in the done mail; the Owner rules on it. Never fix src.
- **Then** the whole App and Core suites (mandatory, Owner), rule 22's ratchet, and rule 12. Register the control under
  DC-NEW-wl-t21-float-d in `docs/lessons/defect-classes.md` (its Control and Status lines only).

**Known App results on this machine.** Name these as known, never as new failures:
- `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`: record it as **NOT CHECKED: the OS
  reports the left mouse button down** (Owner, 2026-10-20).
- `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` and
  `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`: known failures (T31, N=10).
Any other failure is yours: run it alone and report both results. Never inject mouse or keyboard input outside a
test's own window, and never call `SendInput` or `mouse_event`.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. **Kick** at 30 min by mail.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE`: finish the run in flight, commit what is green, mail the
Leader `T21 n9 HELD by USER DEMO at <time>`, and end your turn with the same line.

## Owned authored paths
`tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs` (new, the gate). The test files V3 converts,
each named in `{PROOF}/V3-sweep.txt`, and only the tests V2 named in them. `{PROOF}/**`. The DC-NEW-wl-t21-float-d
entry's Control and Status lines in `docs/lessons/defect-classes.md`. The scratch files of V2 (node 2's test, the evasion copy), **never
committed**. **No src change.**
