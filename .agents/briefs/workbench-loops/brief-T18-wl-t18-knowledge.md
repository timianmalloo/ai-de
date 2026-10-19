# Brief — T18 `wls-knowledge-mode` (slice WL-S; Ruling 149 (2)) · Claude Code · Opus · nodes of ≤ 3 checks

**Goal (Ruling 149 (2)):** a **knowledge-inclusive representation as a MODE of the admitted canvas kind**, reached
through the **representation selector**, and **never a second graph surface**. Retiring the full-window Explorer
(ADR-0045 1(b), T4) removed the only in-app view of knowledge and spec nodes. This mode restores it inside the canvas.
**WL-S does not close until this mode's E2E check is green.**
**Done when (each red first, as an assertion failure, through the real canvas surface):**
1. The representation selector offers a knowledge-inclusive mode. Selecting it shows the graph with knowledge/spec
   nodes; switching back hides them. It is one canvas kind with two modes, never a second surface or kind.
2. The attach point is the `ExcludeKnowledge` seam (`src/AiDe.App/Workbench/WorkbenchShell.cs:1398`,
   `graph.ExcludeKnowledge = true`, recorded by T4). The mode turns it off for this canvas only, and the code-only
   default is unchanged.
3. The mode and its state are keyboard-reachable and exposed to UI Automation, and the selector names the mode in the
   words the design gives (rule 14: read `docs/design/workbench-loops-wl-s.md` §8 first; if it gives no words, send a
   seam request, never invent copy).
Then the `tests/AiDe.App.Tests` and `tests/AiDe.Core.Tests` projects are green.
**Tier** T2 · **fan-out** 0 · node budget per rule 16 (the Leader kicks at a time set from the measured rate).
**Not in scope:** a second graph surface or kind; the slot migration (T11 node 2); `SurfaceContentFactory.cs`,
`LayoutPersistence.cs` and `Perspectives.cs` (T11's; send a seam request if the mode needs a registration there).

## Owned authored paths
`src/AiDe.App/Workbench/CanvasSurface.cs` and the canvas page's mode code (name each file you touch),
`src/AiDe.App/Workbench/WorkbenchShell.cs` (the `ExcludeKnowledge` seam and the selector's wiring only), new tests
under `tests/AiDe.App.Tests/Workbench/Canvas/`, `docs/proof/workbench-loops-wl-s/knowledge-mode/**` (Markdown and .txt only, rule 17).

## Proof
`docs/proof/workbench-loops-wl-s/knowledge-mode/<check>-red.txt` / `-green.txt` per check, `docs/proof/workbench-loops-wl-s/knowledge-mode/suite-green.txt`, `docs/proof/workbench-loops-wl-s/knowledge-mode/README.md` (V2 frontmatter,
`type: proof-pack`, links `implements` → `architecture`).

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader2-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **2**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wls-knowledge-mode && AGENT_SESSION=wl-t18-knowledge AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t18-knowledge --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t18-knowledge AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wls-knowledge-mode` (branch `wls-knowledge-mode`).** Every Bash command starts with `cd C:\Projects\ai-de-wls-knowledge-mode && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wls-knowledge-mode\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to wl-leader2-20261019 --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-s/knowledge-mode/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-s/knowledge-mode/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-s/knowledge-mode/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-s/knowledge-mode/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t18-knowledge` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t18-knowledge" --session wl-t18-knowledge --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
    commit it; then `coord mail send --to wl-leader2-20261019 --kind done --body "<exit evidence: commits, test names, proof files>"`.
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
17. **Add no `.html` file under `docs/`.** Do not render HTML views of your README, hand-off or proof files. The docs
    graph's HTML knowledge-surface cap is full (`SURFACE_LIMIT = 100`, `docs-graph.py:82`; a pack defect), and one rendered
    `handoff.html` stopped T8's join at step 6 (2026-10-19). Markdown stays canonical, and proof is `.md` and `.txt` only.
