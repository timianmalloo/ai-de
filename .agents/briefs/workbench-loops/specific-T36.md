# Brief — T36 `wl-flake-remeasure` (the known-failure list, re-derived with the left button released) · Claude Code · Sonnet · node 1 · READ-ONLY

**Goal:** re-derive the list of tests that fail on this machine, now that the OS reports the left mouse button
**released** (Owner, 2026-10-20). T31 (`docs/proof/workbench-loops-support/loadflake/rates.md`) measured the list while
the button read down all day (`GetAsyncKeyState(VK_LBUTTON)=0x8000`; the Owner's and T34's readings). After a release
(observed 17:50Z, 2026-10-20), the restored-row fold, CtrlDigit and CtrlEnter each pass alone, and the T21 n11 join's
two recounts failed three different tests, each passing alone. This node **measures**. It changes no code and no test,
and it claims no cause. The input-state sweep node is briefed from your rates.

**You are the only App runner on this machine while you run (Owner).** Do not start any other process that opens
windows. **Never inject input**: never call `SendInput`, `mouse_event` or anything that changes the desktop's state.

**The tests to measure alone (M3), whether or not they fail in your suite runs:**
| Test | file:line |
|---|---|
| `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation` | `tests/AiDe.App.Tests/Workbench/Records/AReopenedNotebookRendersItsRecordedAttemptsTests.cs:220` |
| `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` | `tests/AiDe.App.Tests/Workbench/Notebook/CtrlDigitSwitchesThePerspectiveFromInsideAPageTests.cs:33` |
| `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` (T31's "SolutionTreeChord" row) | `tests/AiDe.App.Tests/SolutionTreeChordTests.cs:10` |
| `CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen` | `tests/AiDe.App.Tests/Sessions/ConsoleSplitPlacementTests.cs:136` |
| `TheFloatingNotebook_RedocksToTheCenter_WithItsThreadDraftAndAttemptIntact_SayingNoHide_AndFocusInIt` | `tests/AiDe.App.Tests/Workbench/ALiveNotebookFloatsAndRedocksTests.cs:109` |
| `AfterStop_TheTurnIsStopRequested_AndTheQueuedTurnStaysQueuedBehindIt` | `tests/AiDe.App.Tests/Sessions/Thread/ASendWhileATurnRunsOffersWaitOrParallelTests.cs:300` |
| `ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds` | `tests/AiDe.App.Tests/Workbench/Records/NoDispatchOutsideTheHostTests.cs:236` |
| `ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives` (Core) | `tests/AiDe.Core.Tests/TerminalHostInLifePathTests.cs:37` |
Add every other test that fails in any of your N runs.

**This node, in this order:**
- **(M0) Build once:** `dotnet build AiDe.sln > {PROOF}/build.txt 2>&1`; read its last 5 lines. Every run below uses
  `--no-build`.
- **(M1) N = 10 full runs, k = 1..10.** For each run, in this order:
  1. **Read the board as its own command** (rule 19's command). A `USER DEMO IN PROGRESS` with no later
     `USER DEMO DONE`, or a `join in flight` with no `join done`: start no new run; go to (M-hold).
  2. **Read the left button:** `powershell.exe -NoProfile -ExecutionPolicy Bypass -File docs/proof/workbench-loops-support/deadholder-repro/mouse-state.ps1`
     (read-only; it injects no input). Append its line to `{PROOF}/mouse.txt` labelled `k start`. **If any of its 10
     samples reads `0x8000`, stop: start no run.** Go to (M-stop).
  3. `dotnet test tests/AiDe.Core.Tests --no-build --logger "trx;LogFileName=core-<k>.trx" --results-directory {TREE}/flake-runs > {TREE}/flake-runs/core-<k>.txt 2>&1`
  4. `dotnet test tests/AiDe.App.Tests --no-build --logger "trx;LogFileName=app-<k>.trx" --results-directory {TREE}/flake-runs > {TREE}/flake-runs/app-<k>.txt 2>&1`
  5. Read the left button again; append the line labelled `k end`.
  6. Append ONE line to `{PROOF}/runs.txt`: `k  start(UTC)  end(UTC)  core passed/failed/skipped  app passed/failed/skipped`
     (times from `date -u +%H:%M:%SZ`; counts from the last lines of each `.txt` only).
  Set no `AIDE_REAL_*` variable. `{TREE}/flake-runs/` is never committed. Never read a `.trx` file into your context.
- **(M2) Rates.** Reuse T31's `docs/proof/workbench-loops-support/loadflake/rates.py` (read it first; copy it into
  `{PROOF}` and change only its paths). It writes `{PROOF}/rates.md`: one row per test that failed in at least one run,
  with failures/runs, the run numbers and the first line of each distinct failure message; the header states the test
  count per suite and N.
- **(M3) Alone.** Read the board first. Run each test in the table above, and each other test in `rates.md`, alone 10
  times (`dotnet test <project> --no-build --filter "FullyQualifiedName~<method>"`, output to
  `{TREE}/flake-runs/alone-<method>-<i>.txt`). Read the left button before and after the whole M3 step (`mouse.txt`,
  labelled `M3 start` and `M3 end`). Put the loop in a file and run it (rule 6). Record `alone failures/10` in `rates.md`.
- **(M4) Classify** each test in the table and each other listed test, one word plus the evidence: `load-only` (fails in
  the suite, 0/10 alone) · `fails-alone` · `intermittent` (fails sometimes alone) · `passes` (0/N in the suite and 0/10
  alone). Beside each, give **T31's rates** from `loadflake/rates.md`, so the two states (button down, then released)
  sit side by side. Give no cause. A difference between the two columns is an observation, not a cause.
- **(M5) Register:** under DC-NEW-wl-leader3-b, add one `- **Re-measured (2026-10-20, T36, N=<n>, left button released):**`
  line, with each test's in-suite and alone rates and a pointer to `rates.md`. Status stays `partially-controlled`.
- **(M6)** README (V2 frontmatter, rule 13): the method, N, `runs.txt`, `mouse.txt`, the rates table with T31's column,
  the classification, and what was not measured. Then rule 22's ratchet and rule 12. The done mail carries one line per
  test: in-suite rate, alone rate, class, and T31's rates.

**(M-stop) The button reads down.** Start no further run. Write `runs.txt`, `mouse.txt` and `rates.md` from the
completed runs only, headed **`STOPPED - k of 10 runs, left button reads down at <time>`**, commit, mail the Leader
`T36 STOPPED k/10, left button down at <time>`, and end your turn with the same line. Never extrapolate a rate to N.
**(M-hold) The user's demo or a join stops you.** The run in flight finishes; start no new one. Do the same as (M-stop)
with the header **`PARTIAL - k of 10 runs, held by <USER DEMO | join> at <time>`** and the mail
`T36 PARTIAL k/10, held by <USER DEMO | join> at <time>`. The Leader resumes you; re-read the board and continue from k+1.

**No red-first in this node** (rule 7 does not apply: nothing is added or changed; the proof is the measurement). **No
whole-suite run is owed beyond M1** (rule 18 is met by M1: name the last run's counts as the suite result).

**Run `coord claim` before the first edit of each file, even when the hook reports the paths free** (Owner,
2026-10-20).

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (T31's form). **Kick** by mail at about 150 min, from the
ladder: T34, the last node of this shape (10 full App runs), measured about 131 s/call and 1.3k tokens/min, so 200k
falls at about 150 min (before 120 calls at about 262 min). A kick means: finish the run in flight, then M2 to M6 on the
completed runs, labelled PARTIAL.
**Rules 19, 20, 21, 22 and 24 apply.** Rule 16's "never stop on your own wall-clock estimate" applies: the node ends at
N=10, at a stop or hold, at a kick or at the cap.

## Owned authored paths
`docs/proof/workbench-loops-support/flake-remeasure/**` (README, `runs.txt`, `mouse.txt`, `rates.md`, `rates.py`,
`build.txt`, `ratchet.txt`, the M3 loop script). The DC-NEW-wl-leader3-b entry's new Re-measured line in
`docs/lessons/defect-classes.md`. `{TREE}/flake-runs/**`, never committed. **No src, test or DESIGN.md change.**
