# Brief — T11 `wls-kinds-migration` (slice WL-S) · Claude Code · Opus · node 12 (the last node before the T11 join)

**Goal (plan row):** split `session-document` into the `notebook` + `execution` kinds and migrate saved layouts with a
report. The WL-S Proof Pack is `docs/proof/workbench-loops-wl-s.md`.
**Node 11 is done** (a2e2e8b8): D2 green, D6 amended with the reflection test, and the DOCK4 plural pin **red only**
(`b62261e8`), because T21 held `LayoutPersistence.cs`.
**T21 is joined on main** (Owner order: T21 first, then T11 on top of it). The Leader merged origin/main into your tree
before dispatch, and resolved any `LayoutPersistence.cs` conflict in that merge by keeping both sides. Read the merge
commit before your first edit.

**This node (n12), in this order:**
- **(0)** The whole Core and App suites at the merge head, before any edit, saved as `docs/proof/workbench-loops-wl-s/kinds/suite-node12-start.txt`.
  Any red the merge brought in (T21's partial-read sentence and the DOCK4 arm share `LayoutPersistence.cs`) is this
  node's first fix. Name its cause by file:line.
- **(A) The DOCK4 plural pin, green.** Your node-11 hand-off step 1: one `KeptPromptsSentence` const, and the :391 plural
  arm returns it with `{N}` replaced. Mutate the const once to prove the two-prompt test bites, then revert. Check the
  line numbers on the merged file first (rule 14).
- **(B) The WL-S demo script's attended list (Owner, Ruling 155 (3)).** `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests`
  and `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph` stay NOT CHECKED unattended. Add both to
  the user's **attended** list in `docs/proof/workbench-loops-wl-s.md`'s human demo script, with the exact commands.
  Mark the float step "pending: close condition 4, the live-notebook float after the T11 join" (T21's README names it).
  This is a docs check: no red/green, but the Leader reads the diff.
- **(C) The seam guard edit from node 10.** `EveryOpeningCommandPassesThroughTheSeamTests.cs` was edited outside the
  grant. The Leader confirmed it retroactively at node 10 (obligations.md). Make sure the merge did not reopen it: it
  must be green at the merge head. Record that in handoff.md.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.
**Desktop hold:** a "desktop hold occupied" result is re-run after 2 min and logged in `docs/proof/workbench-loops-wl-s/kinds/desktop-hold.txt`. It is
never counted as a pass or a fail. Rules 19, 20 and 21 apply.

## Owned authored paths
As in node 11, plus `src/AiDe.App/Workbench/LayoutPersistence.cs` (the DOCK4 arm and its const only; T21's sentences stay
as joined) and `docs/proof/workbench-loops-wl-s.md` (the demo script).

## Not in scope
Close condition 4, the live-notebook float and redock: it is owed after the T11 join, and the Leader dispatches it as a
T21 node.

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader3-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **3**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wls-kinds-migration && AGENT_SESSION=wl-t11-kinds AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t11-kinds --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t11-kinds AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wls-kinds-migration` (branch `wls-kinds-migration`).** Every Bash command starts with `cd C:\Projects\ai-de-wls-kinds-migration && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wls-kinds-migration\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to wl-leader3-20261019 --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-s/kinds/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-s/kinds/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-s/kinds/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-s/kinds/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t11-kinds` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t11-kinds" --session wl-t11-kinds --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
    commit it; then `coord mail send --to wl-leader3-20261019 --kind done --body "<exit evidence: commits, test names, proof files>"`.
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
    scratch files go under `docs/proof/workbench-loops-wl-s/kinds/` or a folder named for your track inside `C:\Projects\ai-de-wls-kinds-migration`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
