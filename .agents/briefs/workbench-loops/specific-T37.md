# Brief — T37 `wl-input-state-sweep` (UI tests read the global input state they depend on, and report NOT CHECKED when it is not the default) · Claude Code · Sonnet · node 1

**Goal:** the control owed by `DC-NEW-wl-t33-input-state` (`docs/lessons/defect-classes.md`). A UI test whose result
depends on machine-wide input state reads that state as a precondition. When the state is not the default, the test
reports **NOT CHECKED** with the reason: a **skip**, never a FAIL and never a pass. Owner ruling 2026-10-20, option B
with amendments B1-B5 (`.agents/briefs/workbench-loops/obligations.md`, the last "Owner rulings on T36" row).
The baseline is T36: `docs/proof/workbench-loops-support/flake-remeasure/rates.md` (left button released: the fold
passes 0/10; CtrlDigit fails 10/10 and CtrlEnter 10/10, each with "foreground not held").

**The tests in scope:**
| Test | file:line | State | Today's message |
|---|---|---|---|
| `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation` | `tests/AiDe.App.Tests/Workbench/Records/AReopenedNotebookRendersItsRecordedAttemptsTests.cs:220` | left mouse button | "Space on the restored row's fold did not open it" (when the button reads down) |
| `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` | `tests/AiDe.App.Tests/Workbench/Notebook/CtrlDigitSwitchesThePerspectiveFromInsideAPageTests.cs:137` (and `Sta.cs:187`) | foreground window | "environment: focus could not be put inside the page (foreground held=False ...)" |
| `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` | `tests/AiDe.App.Tests/SolutionTreeChordTests.cs:10` | foreground window | "foreground not held; SendInput would prove another window" |
| the test at `tests/AiDe.App.Tests/Workbench/Composer/APrepareShowsItsSuggestedLinesTests.cs:67` | (B4) | foreground window | "desk run owed: foreground not held" |

**Never inject input.** Never call `SendInput`, `mouse_event` or anything that changes the desktop's state. The
states are read, never set. A red that needs a non-default state gets it **through a seam** (the helper takes the
reading as an injectable source), never from the real desktop.

**This node, in this order:**
- **(S0) Spike the skip (rule 14; the code on main outranks this brief).** App tests use xUnit **2.9.3**
  (`Directory.Packages.props:46`). xUnit 2 has no documented runtime `Assert.Skip` (that is xUnit v3). The repo's
  only skips are decided at **discovery**: custom `[Fact]` subclasses that set `Skip` in their constructor (for
  example `tests/AiDe.Core.Tests/AgentPlane/TheEnginesAnswerInitializeOnTheWireTests.cs:186`). B2 needs the
  foreground read **after the test's own focus attempt**, so at run time. Establish, by a throwaway spike in your
  tree (deleted before commit, its output kept in `{PROOF}/S0-spike.txt`), one mechanism that makes a running
  xUnit 2.9.3 test end as **Skipped / NotExecuted** in the `.trx` with a reason, with **no new package** and no
  version change. Record what you tried and what the `.trx` said. **If no such mechanism exists without a package
  change: stop.** Commit S0, mail the Leader `T37 STOPPED S0: no runtime skip in xUnit 2.9.3 without a package`
  with the evidence, and end your turn. Never fake NOT CHECKED with a pass or a fail.
- **(S1) Red, then green: the precondition helper** (one file under `tests/AiDe.App.Tests/`, your choice of name).
  - **Left button (B1):** the deciding reading is WPF's own view,
    `Mouse.PrimaryDevice.LeftButton` (= `InputManager.Current.PrimaryMouseDevice.LeftButton`), which
    `ButtonBase.OnKeyUp` reads through `GetMouseLeftButtonReleased`. Record `GetAsyncKeyState(VK_LBUTTON)` beside it in
    the reason text as evidence; it does not decide.
  - **Foreground (B2):** read **after** the test's own focus attempt: `GetForegroundWindow()` against the test's
    window. Held → the precondition is met, and the test's own assertions run and FAIL exactly as before. Not held →
    NOT CHECKED with the reason.
  - The reason text starts `NOT CHECKED:` and names the state and the reading.
  - Red first (rule 7): tests of the helper with the state injected through the seam: non-default → the skip fires
    with the reason; default → no skip. See them fail before the helper exists.
- **(S2) Convert** the fold, CtrlDigit and CtrlEnter to call the helper at the point where the state matters. Change
  no assertion and no other behaviour. **B4:** convert APrepare... only if it fits the same helper cleanly; if not,
  list it in the README with the reason and leave it unchanged.
- **(S3) Recall search (B5), independent of the four above:** search `tests/` for other tests that assert a focus or
  foreground precondition as a FAIL. At least: `foreground`, `GetForegroundWindow`, `desk run owed`,
  `SetForegroundWindow`, `focus could not`, `Keyboard.FocusedElement` asserted before the step under test. Put the
  search in a file and run it (rule 6); keep its raw hits in `{PROOF}/S3-hits.txt`. For each hit: converted, or
  listed with a reason (not a precondition; a real assertion; does not fit the helper). The count of hits, converted
  and listed goes in the README.
- **(S4) Visibility (B3), through the gate that already exists.** The join's recount is `tools/verify-test-run.py`.
  It already refuses any NotExecuted result that is not named in `expectedSkips` in `tools/expected-test-counts.json`
  (`verify-test-run.py:418`, `:478`). Read both before you change anything (rule 23).
  - Name in `expectedSkips` **only** the tests that are NOT CHECKED on this machine in your final suite run (expected:
    CtrlDigit and CtrlEnter, and APrepare... if converted and not held). **Not the fold:** it passes here, so if it ever
    skips, the recount stops the join and it goes to the Owner. That is B3's "a count up is a stop".
  - The recount's report must **name each NOT CHECKED test with its reason**, including the expected ones. If
    `verify-test-run.py` does not print the reason for a NotExecuted result today, add that (red first: it has no
    self-test today, so add a `--self-test` on a planted `.trx`, as other tools do). If the file's format cannot carry a reason per entry, keep
    the reason in the `.trx` and print it from there. Do not weaken any existing check.
- **(S5) Suites (rule 18):** Core and App, with `--logger trx`, read the counts only. **Expected on this machine:**
  App 0 failed (from T36's 2), with CtrlDigit and CtrlEnter (and APrepare... if converted) NOT CHECKED, and the fold
  **PASSED**, not skipped. Read the left button with
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File docs/proof/workbench-loops-support/deadholder-repro/mouse-state.ps1`
  (read-only) before and after each App run. Record the lines in `{PROOF}/mouse.txt`. If the button reads down, the
  fold must come out NOT CHECKED. Record that as observed; do not wait for the button.
  Then run `python tools/verify-test-run.py` (the check, no `--update`) and record its output in `{PROOF}/S5-recount.txt`.
- **(S6) Register:** in `DC-NEW-wl-t33-input-state`, update the Sweep, Control and Status lines. Add instances 2-3
  (CtrlDigit, CtrlEnter: the foreground state) with file:line, and APrepare... if converted. Status becomes
  `controlled` only if S4's recount names the NOT CHECKED tests and the fold passes; otherwise say what is missing.
  Rule 11 for anything new.
- **(S7) README** (V2 frontmatter, rule 13): S0's mechanism, the helper, each conversion, S3's table, S4's change,
  S5's counts beside T36's, and the residuals (what the precondition cannot see). Then rule 22's ratchet and rule 12.
  The done mail carries: the S0 mechanism in one line, each test's result in S5 (passed / NOT CHECKED + reason /
  failed), S3's counts, and `expectedSkips` as committed.

**No src change** and no package or version change. A need for either → seam request (rule 4), and stop that step.
**Run `coord claim --wi workbench-loops --path <file>` before the first edit of each file, even when the hook
reports the paths free.** `coord claim` refuses without `--wi`.
**You and T38 (`wl-claim-check`) run at the same time.** T38 runs no App suite and owns
`.agents/briefs/workbench-loops/leader_prejoin.py` and its own test file under `tools/`. You own the paths below;
touch nothing of T38's.

**Tier** T2 · **fan-out** 0 · **budget** 60 tool calls · 120k. **Kick** by mail at about 17 min, from the ladder
(Inferred from the fastest recent node of this shape: T21 n10 used ~158k in ~23 min, about 6.9k tokens/min, so 120k
falls at about 17 min; its s/call was not recorded). A kick means: commit what is green, run S5's suites, then S6-S7
on what is done, labelled PARTIAL. Rules 16, 18, 19, 20, 21, 22, 23 and 24 apply.

## Owned authored paths
- The new precondition helper and its tests under `tests/AiDe.App.Tests/`.
- The four test files in the table (only the conversion), and each S3 hit you convert.
- `tools/verify-test-run.py` (S4's reason printing only) and `tools/expected-test-counts.json` (`expectedSkips` only).
- The `DC-NEW-wl-t33-input-state` entry in `docs/lessons/defect-classes.md`.
- `docs/proof/workbench-loops-support/input-state-sweep/**`.
- `{TREE}/test-runs/**` (never committed).
