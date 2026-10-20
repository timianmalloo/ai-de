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
- **(F0) State the outcomes first (A4).** Before any run, commit `docs/proof/workbench-loops-support/focus-none/F4-outcomes.md` stating what each outcome
  will mean:
  (a) the test host held the foreground at every failure → the hypothesis is refuted (focus is lost inside the host);
  (b) another process held it → that process is named as the holder (no cause claimed beyond the observation);
  (c) the samples are too coarse to tell → say so, and give the sampling gap.
  Also state the sub-cases A2 separates: the host holds the foreground but `hwndFocus` is null, versus the focus
  is on a window of the host's that is not the test's. Nothing in F4-outcomes.md is edited after F2 starts.
- **(F1) The sampler (A1, A2).** One script in `docs/proof/workbench-loops-support/focus-none` (put it in a file, rule 6), run as a **windowless** process,
  for example `pythonw` or `pwsh -WindowStyle Hidden`. Every 500 ms it appends one CSV row to
  `C:\Projects\ai-de-wl-focus-none/focus-runs/sampler.csv` with:
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
  Then run `dotnet test tests/AiDe.App.Tests --no-build --logger "trx;LogFileName=app-<k>.trx" --results-directory C:\Projects\ai-de-wl-focus-none/focus-runs`.
  Write one line per run in `docs/proof/workbench-loops-support/focus-none/runs.txt`. Core is not run. Never read a `.trx` into your context; read it
  with a script.
- **(F3) Join.** For each "(none)" focus failure, take the test's start and end times from the trx and print the
  sampler rows inside that window (plus 2 s each side) to `docs/proof/workbench-loops-support/focus-none/F3-events.md`: who held the foreground, the
  active window and the focus window, through the test's run.
- **(F4) Classify** each event as (a), (b) or (c), against `F4-outcomes.md` as committed. **A3:** if N = 10 shows 0
  events, report **NOT REPRODUCED** with the sampler's foreground-holder distribution over all runs (each process's
  share of the samples, the test host's included), and stop. Do not extend N.
- **(F5) Rates:** `docs/proof/workbench-loops-support/focus-none/rates.md` with one row per test that failed or ended NOT CHECKED in any run, and T36's
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

**First write:** claim your proof directory, `coord claim --wi workbench-loops --path docs/proof/workbench-loops-support/focus-none` (no trailing slash),
then each file outside it before its first edit. **A claim's exit status is its result**: non-zero is not granted. On
`COORD-CLAIM-STALE-CONTROLS`, `git merge origin/main` and claim again before the edit. `C:\Projects\ai-de-wl-focus-none/focus-runs/**` is never
committed.

**No red-first** (nothing is added or changed; the proof is the measurement). Rule 18 is met by F2: name the last
run's counts as the suite result.

**Tier** T2 · **fan-out** 0 · **budget** 60 tool calls · 120k. **Kick** by mail at about 25 min (ladder: the last nodes
ran at about 4.8k tokens/min, so 120k falls at about 25 min). A kick means: finish the run in flight, then F3-F7 on the
completed runs, labelled PARTIAL. Rules 16, 19, 20, 21, 22 and 24 apply.

## Owned authored paths
`docs/proof/workbench-loops-support/focus-none/**` (F4-outcomes.md, the sampler script, runs.txt, F3-events.md,
rates.md, README.md, ratchet.txt). The new Re-measured line under DC-NEW-wl-leader3-b in
`docs/lessons/defect-classes.md`. `C:\Projects\ai-de-wl-focus-none/focus-runs/**`, never committed. **No src, test or DESIGN.md change.**

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
   `cd C:\Projects\ai-de-wl-focus-none && AGENT_SESSION=wl-t40-focus-none AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t40-focus-none --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t40-focus-none AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl-focus-none` (branch `wl-focus-none`).** Every Bash command starts with `cd C:\Projects\ai-de-wl-focus-none && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl-focus-none\`. **Never** touch `C:\Projects\ai-de\`
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
   **At your first write, claim your proof directory as a directory lease:** `coord claim --wi workbench-loops --path docs/proof/workbench-loops-support/focus-none` (no trailing slash: a claim on `docs/proof/workbench-loops-support/focus-none/` is granted and
   covers nothing, measured 2026-10-20). Output files written there by redirection then carry a claim; the join's
   `leader_prejoin.py --branch --session` check refuses any changed path with no claim from your session.
   **A claim's exit status is its result:** non-zero means NOT granted, whatever the text says. On `COORD-CLAIM-STALE-CONTROLS`
   (exit 3), run `git merge origin/main` in your tree and claim again before the edit; any other refusal, mail the Leader.
6. **A multi-line program is a file, then a run** — never a heredoc program. **A gate's exit status is
   never behind a pipe**: run the test/build on its own line and read `$?`, or redirect output to a
   file and then read the file.
7. **Red first on every check named in your exit evidence.** For each: write the test, run it, see it
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/focus-none/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/focus-none/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/focus-none/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/focus-none/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t40-focus-none` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t40-focus-none" --session wl-t40-focus-none --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-support/focus-none/` or a folder named for your track inside `C:\Projects\ai-de-wl-focus-none`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:\Projects\ai-de-wl-focus-none/docs/proof/workbench-loops-support/focus-none/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t40-focus-none`
    from `C:\Projects\ai-de-wl-focus-none`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/focus-none/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t40-focus-none-*` trees.
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
