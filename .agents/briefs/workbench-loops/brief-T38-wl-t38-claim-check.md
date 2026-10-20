# Brief — T38 `wl-claim-check` (the join refuses a branch whose changed paths have no coord claim from the node's session) · Claude Code · Sonnet · node 1

**Goal:** the control owed by `DC-NEW-wl-t36-claim` (`docs/lessons/defect-classes.md`), by Owner ruling 2026-10-20
(`.agents/briefs/workbench-loops/obligations.md`, the last "Owner rulings on T36" row, item 5).
`.agents/briefs/workbench-loops/leader_prejoin.py` runs before every join. It gains a check: for the branch being
joined, every path the branch changes has a granted `coord claim` from **that node's session**. Otherwise it
**refuses** (exit 1), naming each unclaimed path.

**What exists (read it before you change it; rule 14 and rule 23):**
- `leader_prejoin.py` (47 lines): `python -I leader_prejoin.py <repo root>`; exit 0 clean, 1 refused, 2 usage. Today
  it refuses on foreign uncommitted files in the primary checkout. That behaviour and its usage line stay unchanged
  when the new arguments are absent.
- Claim records: `kind: "claim"` events in `.agents/log/<session>.jsonl` in the **primary** checkout's coordination
  store, with `session`, `path`, `at`, `ttl` and `wi` (for example
  `.agents/log/wl-t36-flake-remeasure.jsonl`, seq 2-3). Read `coord-core.py`'s claim code
  (`docs/ai-forward-pack/scripts/coord-core.py`, the `args.cmd == "claim"` branch near `:5472`) to learn:
  how a directory lease covers a file, what `--except` carves out, and which paths a claim **refuses by class**.
  Measured 2026-10-20: a claim on a `.agents/**` path is refused with `COORD-CLAIM-SELF`. The claim self-test also
  names a "derived" refusal class. A path that coord refuses to claim **cannot** be claimed, so the check must exempt
  it, using **coord's own classifier** (import or call it), never a second hand-written list. If the classifier
  cannot be reached without a src change to coord-core.py, raise a seam request (rule 4) and stop that step.
- The audit log and its derived views are written by `audit-log.py` under rule 12. Find out from coord whether they
  are claimable or refused by class. Treat them by what coord does, and say so in the README.

**This node, in this order:**
- **(C1) Red, then green.** `leader_prejoin.py <repo root> --branch <b> --session <s>`:
  - The changed paths are `git diff --name-only <merge-base of main and b> <b>`.
  - Each path is covered by a claim event from session `<s>` (exact path, or a covering directory lease minus its
    `--except` paths), or exempt by coord's class refusal. Otherwise the path is listed and the exit is 1.
  - The output names the session, the branch, the count checked, the count exempt (with the class), and each
    unclaimed path.
  - A missing or unreadable session log is exit 1 with that reason, never a pass (instrumentation: degrade to
    "not recorded", never to clean).
  - **Red on a planted branch:** a test (under `tools/`, your file name; follow how the repo's existing tool
    self-tests run, for example `tools/verify-gate-self-tests.py`) builds a throwaway git repo in a temp directory
    with a branch that changes 3 paths, plus a session log claiming 2 of them. It asserts exit 1 naming the third.
    It asserts exit 0 when all 3 are claimed, exempt for a `.agents/**` path, and exit 1 for a missing log. Write it
    first and see it fail before the check exists (rule 7).
- **(C2) Real branches, read-only:** run the check on two branches that exist and are joined:
  - `wl-flake-remeasure` with session `wl-t36-flake-remeasure` (expected: claimed, because it claimed late);
  - `wl-chk6-gap-pin` with session `wl-t35-chk6-gap-pin` (expected: refused on DESIGN.md and the register).
  Record the outputs in `docs/proof/workbench-loops-support/claim-check/C2-real.txt`. If the branch or the log is gone, say so. **Never create, rename or
  delete a branch or a log in the primary checkout.**
- **(C3) Wire it:** the Leader's join pattern runs `leader_prejoin.py` before `conductor-join.py`. Add one clause to
  the **Joins** bullet of `docs/coordination/workbench-loops-leader-kickoff.md` (line 100, in "Cadence to the Owner")
  naming the new arguments. That is the only edit outside your tool.
- **(C4) Register:** in `DC-NEW-wl-t36-claim`, the Prevent item (2) and Status: `controlled` once C1 and C2 hold.
  Keep the Residual: a claim record shows a claim exists, not that it came before the edit.
- **(C5) README** (V2 frontmatter, rule 13): the check, the exemption rule and where it comes from, C1's red and
  green, C2's outputs and the residual. Then rule 22's ratchet and rule 12.

**No App run and no test suite run beyond your own test** (rule 18 is met by your test plus the ratchet; you change
no src and no test under `tests/`). **No src change.**
**Run `coord claim --wi workbench-loops --path <file>` before the first edit of each file, even when the hook
reports the paths free.** `coord claim` refuses without `--wi`. This node's own join will be the first one checked.
**You and T37 (`wl-input-state-sweep`) run at the same time.** T37 runs App suites and owns
`tools/verify-test-run.py`, `tools/expected-test-counts.json` and test files under `tests/`. Touch none of them.

**Tier** T1 · **fan-out** 0 · **budget** 40 tool calls · 80k. **Kick** by mail at about 12 min, from the ladder
(Inferred: ~6.9k tokens/min, the fastest recent node, puts 80k at about 12 min). A kick means: commit what is green,
then C4-C5 on what is done, labelled PARTIAL. Rules 16, 19, 21, 22, 23 and 24 apply.

## Owned authored paths
- `.agents/briefs/workbench-loops/leader_prejoin.py`.
- One new test file under `tools/` for C1.
- One clause in the Joins bullet of `docs/coordination/workbench-loops-leader-kickoff.md` (C3).
- The `DC-NEW-wl-t36-claim` entry in `docs/lessons/defect-classes.md`.
- `docs/proof/workbench-loops-support/claim-check/**`.

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
   `cd C:\Projects\ai-de-wl-claim-check && AGENT_SESSION=wl-t38-claim-check AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t38-claim-check --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t38-claim-check AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl-claim-check` (branch `wl-claim-check`).** Every Bash command starts with `cd C:\Projects\ai-de-wl-claim-check && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl-claim-check\`. **Never** touch `C:\Projects\ai-de\`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/claim-check/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/claim-check/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/claim-check/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/claim-check/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t38-claim-check` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t38-claim-check" --session wl-t38-claim-check --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-support/claim-check/` or a folder named for your track inside `C:\Projects\ai-de-wl-claim-check`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:\Projects\ai-de-wl-claim-check/docs/proof/workbench-loops-support/claim-check/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t38-claim-check`
    from `C:\Projects\ai-de-wl-claim-check`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/claim-check/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t38-claim-check-*` trees.
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
