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
- **(M0) Build once:** `dotnet build AiDe.sln > docs/proof/workbench-loops-support/flake-remeasure/build.txt 2>&1`; read its last 5 lines. Every run below uses
  `--no-build`.
- **(M1) N = 10 full runs, k = 1..10.** For each run, in this order:
  1. **Read the board as its own command** (rule 19's command). A `USER DEMO IN PROGRESS` with no later
     `USER DEMO DONE`, or a `join in flight` with no `join done`: start no new run; go to (M-hold).
  2. **Read the left button:** `powershell.exe -NoProfile -ExecutionPolicy Bypass -File docs/proof/workbench-loops-support/deadholder-repro/mouse-state.ps1`
     (read-only; it injects no input). Append its line to `docs/proof/workbench-loops-support/flake-remeasure/mouse.txt` labelled `k start`. **If any of its 10
     samples reads `0x8000`, stop: start no run.** Go to (M-stop).
  3. `dotnet test tests/AiDe.Core.Tests --no-build --logger "trx;LogFileName=core-<k>.trx" --results-directory C:/Projects/ai-de-wl-flake-remeasure/flake-runs > C:/Projects/ai-de-wl-flake-remeasure/flake-runs/core-<k>.txt 2>&1`
  4. `dotnet test tests/AiDe.App.Tests --no-build --logger "trx;LogFileName=app-<k>.trx" --results-directory C:/Projects/ai-de-wl-flake-remeasure/flake-runs > C:/Projects/ai-de-wl-flake-remeasure/flake-runs/app-<k>.txt 2>&1`
  5. Read the left button again; append the line labelled `k end`.
  6. Append ONE line to `docs/proof/workbench-loops-support/flake-remeasure/runs.txt`: `k  start(UTC)  end(UTC)  core passed/failed/skipped  app passed/failed/skipped`
     (times from `date -u +%H:%M:%SZ`; counts from the last lines of each `.txt` only).
  Set no `AIDE_REAL_*` variable. `C:/Projects/ai-de-wl-flake-remeasure/flake-runs/` is never committed. Never read a `.trx` file into your context.
- **(M2) Rates.** Reuse T31's `docs/proof/workbench-loops-support/loadflake/rates.py` (read it first; copy it into
  `docs/proof/workbench-loops-support/flake-remeasure` and change only its paths). It writes `docs/proof/workbench-loops-support/flake-remeasure/rates.md`: one row per test that failed in at least one run,
  with failures/runs, the run numbers and the first line of each distinct failure message; the header states the test
  count per suite and N.
- **(M3) Alone.** Read the board first. Run each test in the table above, and each other test in `rates.md`, alone 10
  times (`dotnet test <project> --no-build --filter "FullyQualifiedName~<method>"`, output to
  `C:/Projects/ai-de-wl-flake-remeasure/flake-runs/alone-<method>-<i>.txt`). Read the left button before and after the whole M3 step (`mouse.txt`,
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
`docs/lessons/defect-classes.md`. `C:/Projects/ai-de-wl-flake-remeasure/flake-runs/**`, never committed. **No src, test or DESIGN.md change.**

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
   `cd C:/Projects/ai-de-wl-flake-remeasure && AGENT_SESSION=wl-t36-flake-remeasure AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t36-flake-remeasure --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t36-flake-remeasure AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:/Projects/ai-de-wl-flake-remeasure` (branch `wl-flake-remeasure`).** Every Bash command starts with `cd C:/Projects/ai-de-wl-flake-remeasure && `.
   Every Read/Edit/Write uses an absolute path under `C:/Projects/ai-de-wl-flake-remeasure\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to <the Leader: coord leader who, read now> --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
   and carry on with what you own. `derived` files (docs/docs-index.js, docs/audit/audit-data.js,
   docs/audit/index.html, docs/api/*.md, docs/_site/*, docs/_meta.json) are regenerated at the join —
   do not edit or claim them. `register` files (docs/audit/*.jsonl, .agents/log/*.jsonl) are
   append-only and **never claimed**. Do not edit the test-count baseline; the join recounts.
5. **Claim for the minutes of the edit:** `coord claim --path <file>` (default TTL) just before
   editing a file, `coord release` after the commit. Never a lifetime lease.
6. **A multi-line program is a file, then a run** — never a heredoc program. **A gate's exit status is
   never behind a pipe**: run the test/build on its own line and read `$?`, or redirect output to a
   file and then read the file.
7. **Red first on every check named in your exit evidence.** For each: write the test, run it, see it
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/flake-remeasure/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/flake-remeasure/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/flake-remeasure/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/flake-remeasure/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t36-flake-remeasure` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t36-flake-remeasure" --session wl-t36-flake-remeasure --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-support/flake-remeasure/` or a folder named for your track inside `C:/Projects/ai-de-wl-flake-remeasure`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:/Projects/ai-de-wl-flake-remeasure/docs/proof/workbench-loops-support/flake-remeasure/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t36-flake-remeasure`
    from `C:/Projects/ai-de-wl-flake-remeasure`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/flake-remeasure/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t36-flake-remeasure-*` trees.
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
