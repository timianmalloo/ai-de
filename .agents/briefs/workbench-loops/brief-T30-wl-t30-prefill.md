# Brief — T30 `wl-prefill` (AC-W01a, Rulings 180 and 182) · Claude Code · Opus · node 4

**Goal:** the user sees their own finding fixed (Owner), and Ruling 180's last two conditions are met.
**T30 n1–n3b are joined on main** (`d986e4a4`). Under gate 1, the prefill is the default: lines are marked
`? suggested`, kept across a prepare, and never described as saved. The settings row gives the gate's real reason.
A restored row's compiled prompt is rebuilt and verified, or reads *Compiled prompt not recorded*. Keep's help text is
in the Owner's words.

**This node (n4), in this order:**
- **(0) The prefill demo script (Owner; docs only, first).** Add a section **"Human demo script — the prefill"** to
  `docs/proof/workbench-loops-wl-1a/prefill/README.md`. It runs **after** the user's WL-S walk-through, on a **fresh
  clean worktree of origin/main at `d986e4a4` or later**, Release build, never the primary. Keyboard only. **Read every
  gesture, label and string from the code, and cite file:line in a gesture table, as the WL-1a script did** (rule 14;
  the Leader's own unchecked claims were filed today, so check, do not recall). It must cover:
  - the two-press prepare-then-send under `agentic-advisory`: the first Ctrl+Enter prepares, the second sends;
  - **the one model call on the user's account, flagged in a line before the step**, the way WL-1a step 22 flagged
    real adapter usage;
  - the `? suggested` lines appearing, editing one inline, **Keep**, the line staying across a second prepare, and the
    Keep help text read in UIA;
  - the settings row naming the mode and the gate's reason, and switching to `mechanical-only`;
  - a restart: a sent turn's row shows the rebuilt compiled prompt, or *Compiled prompt not recorded* for a turn with
    attachments or a template; and the residual, said plainly: unsent typing and kept suggestions are lost on restart
    until WL-2.
  Mark anything you could not read from code as **rehearse**.
- **(A) Ruling 180 (4): the cost is shown.** Each advisory call's spend reaches the existing compile-cost line. Red
  first, with a stubbed call that reports a spend. Then the recorder for the first 50 advisory calls' cost and latency:
  where they land (a reader over the existing `called` rows, not a new store), and a test that the reader counts them.
  **No real model call in any test.**
- **(B) Ruling 180 (6): compile-eval runs in the track.** Run `tools/compile-eval` (score.py / ring.py) with ADR-0036's
  floors on the advisory path, the way the eval's README says. Record the result. If it needs a real model call or
  corpus data this machine lacks, record NOT RUN with the reason and the exact command; do not fake a pass. Carry the
  AI-systems lens's conditions from `docs/notes/wl-dr-prefill-goal-done.md` §4 into the Proof Pack as a checked list.
**Node 5 (not this one):** the four sheet tests that read the real `~/.aide/proof`, with the Leader's seam grant at
dispatch, then rule 22's ratchet.

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k; **hand off at 150k**. **Kick** at 15 min by mail.
**Rules 19, 20, 21 apply.** The WL-S walk-through may start at any moment, so check the board before every App run, as
its own command.

## Owned authored paths
As in node 3, plus `docs/proof/workbench-loops-wl-1a/prefill/**` and the compile-cost line's region. A change to the
eval's tooling is a seam request.

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader3-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **3**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl-prefill && AGENT_SESSION=wl-t30-prefill AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t30-prefill --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t30-prefill AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl-prefill` (branch `wl-prefill`).** Every Bash command starts with `cd C:\Projects\ai-de-wl-prefill && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl-prefill\`. **Never** touch `C:\Projects\ai-de\`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-1a/prefill/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-1a/prefill/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-1a/prefill/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-1a/prefill/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t30-prefill` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t30-prefill" --session wl-t30-prefill --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-wl-1a/prefill/` or a folder named for your track inside `C:\Projects\ai-de-wl-prefill`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:\Projects\ai-de-wl-prefill/docs/proof/workbench-loops-wl-1a/prefill/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t30-prefill`
    from `C:\Projects\ai-de-wl-prefill`. Save its refusal lines and last line in `docs/proof/workbench-loops-wl-1a/prefill/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t30-prefill-*` trees.
