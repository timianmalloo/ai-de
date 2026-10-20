# Brief — T39 `wl-slash-claim` (the join refuses a trailing-slash claim, and reports a claim made after a refusal) · Claude Code · Sonnet · node 1

**Goal:** two more refusals in `.agents/briefs/workbench-loops/leader_prejoin.py`'s claim check (T38's
`--branch --session [--base]` mode), by Owner rulings of 2026-10-20 (`obligations.md`: the "trailing-slash false
grant" row and the "T37's join" row, condition 3).

1. **Slash claim (PD-6):** `coord claim --path <dir>/` is granted, and the record keeps the slash. That lease covers
   nothing (`docs/proof/workbench-loops-support/hygiene/pack-defects.md`, PD-6; measured with coord's own
   `lease_covers`). The check **refuses** when the node's session holds any `claim` event whose `path` ends in `/`.
   The message names the false grant and the no-slash form (`coord claim --wi workbench-loops --path <dir>`).
2. **Claimed after a refusal (DC-NEW-wl-t36-claim instance 3):** a node read `coord claim`'s exit-3 refusal as a
   warning and edited the file. Report a changed path whose last coord-recorded outcome for that path and session
   is a refusal as `claimed-after-refusal`, **if the coord record holds refusals at all.** The Leader measured on
   2026-10-20 (Inferred until you check it): no `.agents/log/*.jsonl` event has a refusal kind (claim 3,989,
   release 3,279, others; none refused). `.agents/decisions/<session>.jsonl` holds the commit hook's `kind: "allowed"`
   decisions only, including T37's unclaimed `tools/verify-gate-self-tests.py`. Read
   `docs/ai-forward-pack/scripts/coord-core.py`'s claim refusal path (near `:5475` onward, and `make_event`) to
   establish whether **any** refusal is written anywhere. **If none is, do not build this refusal.** Report it in the
   README as not checkable, with the evidence (the code path, and the kinds counted), and the residual: "a claim
   refused at the CLI leaves no record; rule 5's exit-status line is prose". Never edit `coord-core.py` (a pack copy,
   Ruling 151).

**This node, in this order:**
- **(L1) Red, then green, for (1):** extend `tools/test-leader-prejoin-claims.py` (T38's fixture: a throwaway repo
  and a session log) with a planted `claim` event whose path is `<dir>/` and a changed file inside `<dir>`. Expect
  exit 1 and the false-grant message. Also expect that a no-slash claim on `<dir>` still passes for that file. See it
  fail before the change (rule 7).
- **(L2) Establish (2).** Write your finding to `docs/proof/workbench-loops-support/slash-claim/L2-refusals.txt`: what the code writes on a refusal, and the
  kinds counted across `.agents/log` and `.agents/decisions`. If refusals are recorded, build (2) red-then-green the
  same way; if not, stop that step as above.
- **(L3) Real run, read-only:** run the check on `wl-input-state-sweep` with session `wl-t37-input-state-sweep` and
  `--base` the merge's first parent, plus on your own branch. Record both in `docs/proof/workbench-loops-support/slash-claim/L3-real.txt`.
- **(L4) Register:** in `DC-NEW-wl-t36-claim`, Prevent (3): what landed, and the residual if (2) is not checkable.
  In PD-6 (`pack-defects.md`), the Interim line: the join-side refusal landed.
- **(L5) README** (V2 frontmatter, rule 13), then rule 22's ratchet and rule 12.

**First write:** claim your proof directory, `coord claim --wi workbench-loops --path docs/proof/workbench-loops-support/slash-claim` (no trailing slash),
then each file before its first edit. **A claim's exit status is its result**: non-zero is not granted. On
`COORD-CLAIM-STALE-CONTROLS`, run `git merge origin/main` and claim again before the edit.
**No App run** and no test run beyond your fixture (rule 18 is met by the fixture plus the ratchet). **No src change.**

**Tier** T1 · **fan-out** 0 · **budget** 20 tool calls · 40k (Owner). **Kick** by mail at about 8 min (ladder: the last
two nodes ran at about 4.8k tokens/min, so 40k falls at about 8 min). A kick means: commit what is green, then L4-L5
on what is done, labelled PARTIAL. Rules 16, 19, 21, 22, 23 and 24 apply.

## Owned authored paths
- `.agents/briefs/workbench-loops/leader_prejoin.py`.
- `tools/test-leader-prejoin-claims.py`.
- The `DC-NEW-wl-t36-claim` entry in `docs/lessons/defect-classes.md`; the PD-6 section of
  `docs/proof/workbench-loops-support/hygiene/pack-defects.md`.
- `docs/proof/workbench-loops-support/slash-claim/**`.

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
   `cd C:\Projects\ai-de-wl-slash-claim && AGENT_SESSION=wl-t39-slash-claim AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t39-slash-claim --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t39-slash-claim AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl-slash-claim` (branch `wl-slash-claim`).** Every Bash command starts with `cd C:\Projects\ai-de-wl-slash-claim && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl-slash-claim\`. **Never** touch `C:\Projects\ai-de\`
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
   **At your first write, claim your proof directory as a directory lease:** `coord claim --wi workbench-loops --path docs/proof/workbench-loops-support/slash-claim` (no trailing slash: a claim on `docs/proof/workbench-loops-support/slash-claim/` is granted and
   covers nothing, measured 2026-10-20). Output files written there by redirection then carry a claim; the join's
   `leader_prejoin.py --branch --session` check refuses any changed path with no claim from your session.
   **A claim's exit status is its result:** non-zero means NOT granted, whatever the text says. On `COORD-CLAIM-STALE-CONTROLS`
   (exit 3), run `git merge origin/main` in your tree and claim again before the edit; any other refusal, mail the Leader.
6. **A multi-line program is a file, then a run** — never a heredoc program. **A gate's exit status is
   never behind a pipe**: run the test/build on its own line and read `$?`, or redirect output to a
   file and then read the file.
7. **Red first on every check named in your exit evidence.** For each: write the test, run it, see it
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/slash-claim/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/slash-claim/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/slash-claim/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/slash-claim/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t39-slash-claim` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t39-slash-claim" --session wl-t39-slash-claim --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-support/slash-claim/` or a folder named for your track inside `C:\Projects\ai-de-wl-slash-claim`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:\Projects\ai-de-wl-slash-claim/docs/proof/workbench-loops-support/slash-claim/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t39-slash-claim`
    from `C:\Projects\ai-de-wl-slash-claim`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/slash-claim/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t39-slash-claim-*` trees.
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
