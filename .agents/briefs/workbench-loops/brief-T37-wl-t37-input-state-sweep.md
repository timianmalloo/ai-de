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
  tree (deleted before commit, its output kept in `docs/proof/workbench-loops-support/input-state-sweep/S0-spike.txt`), one mechanism that makes a running
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
  search in a file and run it (rule 6); keep its raw hits in `docs/proof/workbench-loops-support/input-state-sweep/S3-hits.txt`. For each hit: converted, or
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
  (read-only) before and after each App run. Record the lines in `docs/proof/workbench-loops-support/input-state-sweep/mouse.txt`. If the button reads down, the
  fold must come out NOT CHECKED. Record that as observed; do not wait for the button.
  Then run `python tools/verify-test-run.py` (the check, no `--update`) and record its output in `docs/proof/workbench-loops-support/input-state-sweep/S5-recount.txt`.
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
- `C:\Projects\ai-de-wl-input-state-sweep/test-runs/**` (never committed).

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. **The Leader is whoever `coord leader who` names when you
send.** Leaders hand off mid-track, so never write, remember or copy a Leader id or epoch. Before every mail or request
to the Leader, run `python docs/ai-forward-pack/scripts/coord-core.py leader who` as its own command in the same turn.
Send to the session on its `leader` line. If its `state` is not `live`, send anyway and say so in the body. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl-input-state-sweep && AGENT_SESSION=wl-t37-input-state-sweep AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t37-input-state-sweep --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t37-input-state-sweep AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl-input-state-sweep` (branch `wl-input-state-sweep`).** Every Bash command starts with `cd C:\Projects\ai-de-wl-input-state-sweep && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl-input-state-sweep\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to <the Leader: coord leader who, read now> --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
   and carry on with what you own. `derived` files (docs/docs-index.js, docs/audit/audit-data.js,
   docs/audit/index.html, docs/api/*.md, docs/_site/*, docs/_meta.json) are regenerated at the join —
   do not edit or claim them. `register` files (docs/audit/*.jsonl, .agents/log/*.jsonl) are
   append-only and **never claimed**. Do not edit the test-count baseline; the join recounts.
5. **Claim for the minutes of the edit:** `coord claim --wi workbench-loops --path <file>` (default TTL; `coord claim` refuses without `--wi` and does not read `AGENT_WI`) just before
   editing a file, `coord release` after the commit. Never a lifetime lease.
6. **A multi-line program is a file, then a run** — never a heredoc program. **A gate's exit status is
   never behind a pipe**: run the test/build on its own line and read `$?`, or redirect output to a
   file and then read the file.
7. **Red first on every check named in your exit evidence.** For each: write the test, run it, see it
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/input-state-sweep/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/input-state-sweep/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/input-state-sweep/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/input-state-sweep/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t37-input-state-sweep` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t37-input-state-sweep" --session wl-t37-input-state-sweep --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
    commit it; then `coord mail send --to <the Leader: coord leader who, read now> --kind done --body "<exit evidence: commits, test names, proof files>"`.
    Your final message to the Leader is the same exit-evidence report: every commit sha (red and green),
    every test name added, every proof file, the suite result with pass/fail counts, any seam request
    raised, anything not done. **Your "done" is evidence, not authority** — the Leader re-runs and reads back.
13. **Every Markdown file you add under `docs/` starts with V2 frontmatter** (`id` unique, `title`, `type` — `proof-pack`
    for evidence READMEs, `design` for design docs, `decision-note` for notes — `status`, `owner: "@timianmalloo"`, `links`
    with at least one `implements` edge to its ADR or `architecture`, `review-by`, `summary`). Before your done report run
    `python docs/ai-forward-pack/scripts/docs-graph.py validate` and fix every **problem** it lists. Index drift alone is
    fine, because the join regenerates the index. A file with no frontmatter turned this gate from green to red at a join
    (2026-10-19).
14. **The code on main outranks your brief on what a field means.** Before you implement a field, record or kind that
    your brief describes, open its declaration and doc comment on main. If they disagree with the brief, follow the code.
    Send a seam request naming both texts, and do not build to the brief's wording. A brief that described an offset
    differently from `RunEventRedaction`'s doc comment sent T7 the wrong way (DC-NEW-wl-leader2, 2026-10-19).
15. **A real-lane proof copy never carries account identity (Ruling 157; the repo is public).** Before you commit any file
    captured from a real harness (run logs, frames, transcripts), replace every account email, organization or user-identity
    value with `[scrubbed before commit]`. Record in that folder's README the original's sha256 and the scrubbed field
    names. The gate `tools/verify-no-account-identity.py` refuses email-shaped strings in tracked `docs/proof/**` and
    `.agents/**`. Only `*@example.invalid`, `example.com`/`example.org` and GitHub `users.noreply` addresses pass.
16. **Node size is set by scope, not only by a cap (DC-NEW-wl-leader2-b).** A node carries at most three red-then-green
    checks, or one check plus its real-lane run. At 120 tool calls or 200k context, commit what is green and write
    `handoff.md`, even if a check is half done. The Leader starts the next node fresh. Three Opus nodes in a row
    overran caps sized for twice their real scope (T5 n1 346k, T8 n1 285k, T5 n2 330k).
    **Never stop on your own wall-clock estimate.** A node stops only at the Leader's kick mail (read your mail between
    checks) or at 120 calls / 200k. T11 n4 stopped itself after 5 minutes, believing it had used the 12-minute kick
    time, with two of three checks unstarted (2026-10-19).
17. **Add no `.html` file under `docs/`.** Do not render HTML views of your README, hand-off or proof files. The docs
    graph's HTML knowledge-surface cap is full (`SURFACE_LIMIT = 100`, `docs-graph.py:82`; a pack defect), and one rendered
    `handoff.html` stopped T8's join at step 6 (2026-10-19). Markdown stays canonical, and proof is `.md` and `.txt` only.
18. **A kick never skips the whole-suite run (DC-NEW-wl-leader2, 15th instance).** At a kick, finish or commit the check
    you are in, then run the whole affected suites (rule 7), then hand off. A node with no `suite-node<N>.txt` is not done.
19. **Do not start an App suite while a join is in flight (DC-NEW-wl-leader3).** A join's recount needs the Ruling 115
    desktop hold for its shown-window probe. A track suite that holds it makes the join refuse for a cause outside the
    join (T17e join, 2026-10-19 15:03Z). Before `dotnet test tests/AiDe.App.Tests`, read the board
    (`python docs/ai-forward-pack/scripts/coord-board.py board --since <last id you read>`). If a `join in flight` post has
    no matching `join done` or `join abandoned` post, run Core first and start App after the join ends.
20. **The user's demo holds the desktop (Owner, 2026-10-19).** App suites and UI probes open windows and send input, so
    a run during the user's walk-through can steal focus or type into their session. While the board's latest Owner
    post on it reads `USER DEMO IN PROGRESS`, with no later `USER DEMO DONE`, run **no App suite, no UI probe and no
    real-lane test**, in any tree. Core suites and docs-only work may continue. Read the board (rule 19's command) before
    every App, probe or real-lane run.
21. **Every output file you write stays in your own tree (DC-NEW-wl-leader3, 2nd instance).** Test logs, suite runs and
    scratch files go under `docs/proof/workbench-loops-support/input-state-sweep/` or a folder named for your track inside `C:\Projects\ai-de-wl-input-state-sweep`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:\Projects\ai-de-wl-input-state-sweep/docs/proof/workbench-loops-support/input-state-sweep/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t37-input-state-sweep`
    from `C:\Projects\ai-de-wl-input-state-sweep`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/input-state-sweep/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t37-input-state-sweep-*` trees.
23. **A gate's refusal is answered by reading what it guards (DC-NEW-wl-t30-gate-evasion).** When a test, guard or gate
    refuses your change, first read its comment and the rule or ruling it cites, then decide: either the premise it
    guards still holds and your change is wrong, or the premise has become false and the gate is retired or replaced
    by a real control, with the reason written in the test. **Never** rename, reword, move or split code just to
    leave a lexical or structural gate's view. In the commit that answers a refusal, name the gate, the rule it
    cites and which of the two it was. T30 n5 renamed `SuggestedLinesAnnouncement` to pass
    `NoAssistPathExistsAnywhereInTheComposer`, while the prefill had made R21's assist path real (Ruling 180).
24. **Never prompt the user directly (DC-NEW-wl-leader5-b).** No node and no Leader calls `AskUserQuestion`. A question
    for the user goes in your mail to the Leader, who sends it to the Owner; the Owner relays it. A direct prompt blocks
    the session until the user answers. The Leader's did for about three hours on 2026-10-20: its pin lapsed, and the
    Owner's rulings queued unread.
