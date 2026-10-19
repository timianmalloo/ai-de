# Brief — T24 `wl1b-service` (slice WL-1b) · Claude Code · Sonnet · node 1

**Goal (design row T24):** the WL-1b checklist service in Core. It holds one writer transaction per write, re-folds
acceptance inside the `work.accept` write, and refuses every guarded case with a stable code.
**Done when:** B1, B2 and B3 of `docs/design/workbench-loops-wl-1b.md` (§ Test plan, :461-463) are each red then green.
B3 is extended by Ruling 176 (below). The Core suite is green.
**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k (rule 16) · **kick** at 12 min by mail.
**Convergence:** the service checks green. No App code, no UI, no bench.
**Branches only:** nothing in WL-1b joins until the user's WL-1a demo passes.

## The design is your contract (read it first, then the code it cites)
`docs/design/workbench-loops-wl-1b.md` (on `wl1b-design`). Read:
- What already exists on main (:72-91)
- Data model (:93-182)
- The service (:259-282)
- Error and concurrency model (:343-357)
- Failure modes (:358-380)
- Test plan B1-B3 (:461-463)

Rule 14 holds: where the design and the code on main disagree, follow the code and send a seam request.

## Rulings (binding, each committed alone on main; read them in `docs/notes/addendum-c-council-rulings.md`)
- **Ruling 176 (DR-1):** `checklist_id = journey_id`, one checklist per journey, **enforced**. A write that would create
  a second checklist for a journey (any obligation row whose `checklist_id` is not its work journey's id) is refused with
  a stable code, never merged silently. That is part of check **B3**, red first. Record the upgrade trigger in a
  `simplify:` comment at the enforcement point: the first spec story that needs two checklists per journey requires the
  v3 owner column, with a backfill from the alias.
- **Ruling 177 (DR-2):** no disposition covers a mandatory obligation's gap. The service never bypasses the fold.
- **Ruling 179 (DR-4):** the deliverable is the accepted intent revision. `work.accept`'s result names the intent revision
  by number and never claims implementation.

## Inputs
T23 published the fold record types at `ab72eaca` on branch `wl1b-folds`. **Merge `wl1b-folds` into your branch first**
(`git merge --no-edit wl1b-folds`). This is a dependency merge of a sibling WL-1b branch, which the Leader allows; it is
not a merge of main. If the fold bodies are still stubs, build the service against the types. B1's eligible path then
needs T23's green: if it is not there yet, ask the Leader for a re-merge, then finish B1.

## Owned authored paths (design row T24)
- new `src/AiDe.Core/Ipc/ChecklistOperations.cs`
- `src/AiDe.Core/Store/Records/StoreReader.Workbench.cs` (the 3 new readers only)
- `src/AiDe.Core/Ipc/WorkspaceOperations.cs` (`RegisterChecklist` and the 5 operation names only)
- `src/AiDe.Core/Ipc/WorkspaceClient.cs` (the 5 methods only)
- `src/AiDe.Core/Store/WorkspaceStore.cs` (the new codes only)
- `AttemptStateFold.cs:55` (delete the `assume:` only)
- `src/AiDe.Daemon/Program.cs:186` (one line)
- `tests/AiDe.Core.Tests/Store/SchemaV2/ConstraintTests.cs` (the `ReadPlanTests` rows only)
- new `tests/AiDe.Core.Tests/Checklist/ChecklistOperationsTests.cs`
- `docs/proof/workbench-loops-wl-1b/service/**`

Open each file before you edit it, and keep to the named region.

## Not in scope
The folds' bodies (T23), the bench (T25), the checklist kind (T26), and Ruling 178's currentness unification (a later
track). No App code and **no App suite**: this track is Core only.

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader3-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **3**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl1b-service && AGENT_SESSION=wl-t24-wl1b-service AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t24-wl1b-service --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t24-wl1b-service AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl1b-service` (branch `wl1b-service`).** Every Bash command starts with `cd C:\Projects\ai-de-wl1b-service && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl1b-service\`. **Never** touch `C:\Projects\ai-de\`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-1b/service/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-1b/service/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-1b/service/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-1b/service/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t24-wl1b-service` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t24-wl1b-service" --session wl-t24-wl1b-service --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
