# Brief — T40 `wl-focus-none` (who holds the foreground and the keyboard focus when "focus lands on (none)" fails under load) · Claude Code · Sonnet · node 1 · READ-ONLY

**Goal:** measure, without claiming a cause, what holds the foreground and the keyboard focus when a test fails with
focus on "(none)" in a full App run (Owner ruling 2026-10-20, `obligations.md`, the "T40 wl-focus-none APPROVED" row).
This node changes no code and no test.

**The events so far** (each passes 10/10 alone; T36 `docs/proof/workbench-loops-support/flake-remeasure/rates.md`):
| Test | file | Message | Events |
|---|---|---|---|
| `CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen` | `tests/AiDe.App.Tests/Sessions/ConsoleSplitPlacementTests.cs:136` | "focus after the close is on (none), not the landing" | T21 n11 join recount 1; T36 run 4 |
| `TheFactoryLanding_IsTheNotebookInTheCenter_ElseTheCentersEmptyCopy_EachWithAFocusTarget` | `tests/AiDe.App.Tests/Sessions/SessionDocumentPlacementTests.cs` | "focus landed on (none), not inside the composer" | T36 run 6 |
Any other failure whose message says focus is on "(none)" counts too.

**The hypothesis to disconfirm (Inferred):** "focus (none)" is DC-NEW-wl-t33-input-state's foreground state (another
window holds the foreground), seen through WPF keyboard focus.

**You are the only App runner on this machine while you run.** T39 (`wl-slash-claim`) may run at the same time; it
runs no App. **Never inject input**: never call `SendInput`, `mouse_event`, `SetForegroundWindow`,
`AttachThreadInput`, `SetFocus` or any API that changes the desktop's state.

**This node, in this order:**
- **(F0) State the outcomes first (A4).** Before any run, commit `{PROOF}/F4-outcomes.md` stating what each outcome
  will mean:
  (a) the test host held the foreground at every failure → the hypothesis is refuted (focus is lost inside the host);
  (b) another process held it → that process is named as the holder (no cause claimed beyond the observation);
  (c) the samples are too coarse to tell → say so, and give the sampling gap.
  Also state the sub-cases A2 separates: the host holds the foreground but `hwndFocus` is null, versus the focus
  is on a window of the host's that is not the test's. Nothing in F4-outcomes.md is edited after F2 starts.
- **(F1) The sampler (A1, A2).** One script in `{PROOF}` (put it in a file, rule 6), run as a **windowless** process,
  for example `pythonw` or `pwsh -WindowStyle Hidden`. Every 500 ms it appends one CSV row to
  `{TREE}/focus-runs/sampler.csv` with:
  - the UTC timestamp in ms;
  - `GetForegroundWindow()`, its process name and PID (`GetWindowThreadProcessId`), and its window class;
  - the foreground thread id;
  - `GetGUIThreadInfo` for that thread: `hwndActive`, `hwndFocus`, and `hwndFocus`'s class;
  - the left button (`GetAsyncKeyState(VK_LBUTTON)`), as evidence only.
  Read-only calls only. Start it before F2 and stop it after F2. **Its first rows must show that its own PID never
  holds the foreground.** Check this before run 1 and again at the end. If the sampler's PID ever appears as the
  holder, stop: the run is invalid (M-stop, with that reason).
- **(F2) N = 10 full App runs**, k = 1..10, in T36's shape (`flake-remeasure/README.md`, M0-M1). Build once. Before
  each run, read the board (rule 19's command) and the left button (`deadholder-repro/mouse-state.ps1`, read-only).
  Then run `dotnet test tests/AiDe.App.Tests --no-build --logger "trx;LogFileName=app-<k>.trx" --results-directory {TREE}/focus-runs`.
  Write one line per run in `{PROOF}/runs.txt`. Core is not run. Never read a `.trx` into your context; read it
  with a script.
- **(F3) Join.** For each "(none)" focus failure, take the test's start and end times from the trx and print the
  sampler rows inside that window (plus 2 s each side) to `{PROOF}/F3-events.md`: who held the foreground, the
  active window and the focus window, through the test's run.
- **(F4) Classify** each event as (a), (b) or (c), against `F4-outcomes.md` as committed. **A3:** if N = 10 shows 0
  events, report **NOT REPRODUCED** with the sampler's foreground-holder distribution over all runs (each process's
  share of the samples, the test host's included), and stop. Do not extend N.
- **(F5) Rates:** `{PROOF}/rates.md` with one row per test that failed or ended NOT CHECKED in any run, and T36's
  columns beside it. CtrlDigit and CtrlEnter now skip when the foreground is not held (T37), so their NOT CHECKED
  counts are a measurement, not a failure.
- **(F6) Register:** one `- **Re-measured (2026-10-20, T40, focus-none, N=<n>):**` line under DC-NEW-wl-leader3-b,
  with each event's class and a pointer to the README. Open a new class (rule 11) only if F4 shows a repeatable
  holder (the same non-host process at 2 or more events).
- **(F7) README** (V2 frontmatter, rule 13): the method, the sampler's proof of non-perturbation, the outcomes as
  stated before the data, the events, the distribution, and what was not measured. Then rule 22's ratchet and
  rule 12. The done mail carries one line per event (test, run, class, holder) and the distribution's top 3 holders.

**(M-stop)/(M-hold):** as T36's brief (`brief-T36-wl-t36-flake-remeasure.md`): a button that reads down or an invalid
sampler stops you; a `USER DEMO IN PROGRESS` or a `join in flight` holds you after the run in flight. Write what is
complete, labelled STOPPED or PARTIAL, mail the Leader, and end.

**First write:** claim your proof directory, `coord claim --wi workbench-loops --path {PROOF}` (no trailing slash),
then each file outside it before its first edit. **A claim's exit status is its result**: non-zero is not granted. On
`COORD-CLAIM-STALE-CONTROLS`, `git merge origin/main` and claim again before the edit. `{TREE}/focus-runs/**` is never
committed.

**No red-first** (nothing is added or changed; the proof is the measurement). Rule 18 is met by F2: name the last
run's counts as the suite result.

**Tier** T2 · **fan-out** 0 · **budget** 60 tool calls · 120k. **Kick** by mail at about 25 min (ladder: the last nodes
ran at about 4.8k tokens/min, so 120k falls at about 25 min). A kick means: finish the run in flight, then F3-F7 on the
completed runs, labelled PARTIAL. Rules 16, 19, 20, 21, 22 and 24 apply.

## Owned authored paths
`docs/proof/workbench-loops-support/focus-none/**` (F4-outcomes.md, the sampler script, runs.txt, F3-events.md,
rates.md, README.md, ratchet.txt). The new Re-measured line under DC-NEW-wl-leader3-b in
`docs/lessons/defect-classes.md`. `{TREE}/focus-runs/**`, never committed. **No src, test or DESIGN.md change.**
