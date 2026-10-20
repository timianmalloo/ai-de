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
  stands. It must fail, naming each test that breaks the rule, by `file::test` (`docs/proof/workbench-loops-wl-s/layout-gate/V2-red-today.txt`). If it names
  none, the rule is too loose: tighten it and say how in the README. Then the mutation red: restore node 2's deleted test
  as a scratch file and show the gate names it (`docs/proof/workbench-loops-wl-s/layout-gate/V2-red-node2.txt`). Then the **evasion red** (rule 23,
  DC-NEW-wl-t30-gate-evasion; Owner): in a scratch copy of one named test, wrap its by-hand render in a local helper
  with a new name, and show the gate still names the test (`docs/proof/workbench-loops-wl-s/layout-gate/V2-red-evasion.txt`). If it cannot, make the gate
  follow one level of same-file helper calls, as the composer gate does; if that is not enough, state the limit in the
  gate's own summary comment as a residual and name it in the done mail for the Owner's ruling. Delete every scratch
  file and never commit one.
- **(V2b) Recall, a separate step (Owner).** List every App test that reads drawn layout state after a layout command.
  Find them by a search that does **not** reuse the gate's regexes: for example every test file that references
  `DockHost` or a layout type and also issues a command. Classify each test: **matched** by the gate, **out of class**
  (with the reason), or **missed** by the gate. Save the list to `docs/proof/workbench-loops-wl-s/layout-gate/V2b-recall.txt`, with the search command. A
  missed test means the rule is tightened, and V2 re-run, before V3 starts. No test is converted on V2b's word alone.
- **(V3) The sweep, of the tests V2 named only (Owner).** For each, choose one:
  - **convert it** to take the user's route (a test change only; the test must still prove its own claim, so run it
    alone, red by mutation where its claim allows). A change that only renames, wraps or moves a call so that the
    regex stops matching is **refused** (rule 23): only a test that now takes the route counts as converted;
  - **list it** as an exception, with a one-line reason that names what stops the conversion. The list holds only
    entries that are correct by their stated reason.
  Save one line per test to `docs/proof/workbench-loops-wl-s/layout-gate/V3-sweep.txt`: the test, convert or list, and the evidence. The gate is then green
  (`docs/proof/workbench-loops-wl-s/layout-gate/V3-green.txt`).
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
each named in `docs/proof/workbench-loops-wl-s/layout-gate/V3-sweep.txt`, and only the tests V2 named in them. `docs/proof/workbench-loops-wl-s/layout-gate/**`. The DC-NEW-wl-t21-float-d
entry's Control and Status lines in `docs/lessons/defect-classes.md`. The scratch files of V2 (node 2's test, the evasion copy), **never
committed**. **No src change.**

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
   `cd C:/Projects/ai-de-wl-layout-gate && AGENT_SESSION=wl-t21-layout-gate AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t21-layout-gate --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t21-layout-gate AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:/Projects/ai-de-wl-layout-gate` (branch `wl-layout-gate`).** Every Bash command starts with `cd C:/Projects/ai-de-wl-layout-gate && `.
   Every Read/Edit/Write uses an absolute path under `C:/Projects/ai-de-wl-layout-gate\`. **Never** touch `C:\Projects\ai-de\`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-s/layout-gate/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-s/layout-gate/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-s/layout-gate/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-s/layout-gate/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t21-layout-gate` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t21-layout-gate" --session wl-t21-layout-gate --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-wl-s/layout-gate/` or a folder named for your track inside `C:/Projects/ai-de-wl-layout-gate`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:/Projects/ai-de-wl-layout-gate/docs/proof/workbench-loops-wl-s/layout-gate/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t21-layout-gate`
    from `C:/Projects/ai-de-wl-layout-gate`. Save its refusal lines and last line in `docs/proof/workbench-loops-wl-s/layout-gate/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t21-layout-gate-*` trees.
23. **A gate's refusal is answered by reading what it guards (DC-NEW-wl-t30-gate-evasion).** When a test, guard or gate
    refuses your change, first read its comment and the rule or ruling it cites, then decide: either the premise it
    guards still holds and your change is wrong, or the premise has become false and the gate is retired or replaced
    by a real control, with the reason written in the test. **Never** rename, reword, move or split code just to
    leave a lexical or structural gate's view. In the commit that answers a refusal, name the gate, the rule it
    cites and which of the two it was. T30 n5 renamed `SuggestedLinesAnnouncement` to pass
    `NoAssistPathExistsAnywhereInTheComposer`, while the prefill had made R21's assist path real (Ruling 180).
