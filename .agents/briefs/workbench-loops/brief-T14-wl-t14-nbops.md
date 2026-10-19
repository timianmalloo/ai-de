# Brief — T14 `wl1a-notebook-ops` (slice WL-1a) · Claude Code · Sonnet

**Goal (seam ruling on `req-01M59AS2R0986PT82ARD0QZP8G`, items 1–3):** give the App the three notebook IPC operations T8
binds to, so a prepared draft is saved as an immutable revision in the product and survives a restart.
**Done when:** `notebook.open`, `notebook.revision.save` and `notebook.revision.latest` exist end to end (op constant →
client method → daemon handler → store writer/reader). Each behaviour below was seen red, then green, against a real store:
- `notebook.open {notebook_id, journey_title}`: in **one** transaction it appends `journey_dim` (purpose = work) and
  `notebook_dim` (owner = that journey, legacy = 0) when the notebook has no row. It is idempotent on `notebook_id`:
  a second open writes nothing and returns the existing ids.
- `notebook.revision.save {notebook_id, body, parent_revision_no?}` → `{revision_no, body_sha, saved_at}`.
  `revision_no` = max + 1 inside the writer transaction. It is idempotent when `body_sha` equals the latest revision's:
  no new row, and it returns that revision.
- `notebook.revision.latest {notebook_id}` → the latest `NotebookRevisionRow`, or null.
Then the whole `tests/AiDe.Core.Tests` project is green.
**Tier** T2 · **fan-out** 0 · **budget** 90 tool calls · 220k · 2 h · **deadline** 2 h.
**Not in scope:** the Ruling 148 adoption actor and any `notebook.adopt` op (held for a Data & Persistence design note);
any schema change (`WorkspaceSchema.cs` is not yours, and the v2 DDL is hash-pinned); any App/Workbench file (T8);
attempt ops (T5).

## Read first (rule 14: the code wins on what a field means)
`src/AiDe.Core/Store/WorkspaceSchema.cs` (`journey_dim`, `notebook_dim` and the notebook revision table, with their
triggers: `trg_notebook_owner_exists`, `trg_notebook_legacy_flag`, `trg_notebook_dim_version_order`). Also
`src/AiDe.Core/Store/Records/WorkbenchRecords.cs` (`NotebookRow`, `NotebookRevisionRow`), the existing
`StoreWriter.Workbench.cs` / `StoreReader.Workbench.cs`, and `StoreWriter.BodySha` (one body-hash writer, Ruling 148:
reuse it, never a second one). Then read how T5 wired `attempt.begin/outcome/read/sweep` (`WorkspaceOperations.cs:177`
and its client and daemon handler); copy that path exactly for the three new ops. Read
`docs/design/workbench-loops-wl-1a.md` §"Fold inputs" and the gate table for the transaction rules.

## Owned authored paths
New files: `src/AiDe.Core/Ipc/NotebookOperations.cs` (if the op constants or handlers fit a separate file; otherwise
the constants go in `WorkspaceOperations.cs`), `src/AiDe.Core/Store/Records/StoreWriter.Notebook.cs`,
`src/AiDe.Core/Store/Records/StoreReader.Notebook.cs`, `tests/AiDe.Core.Tests/Notebooks/**`, `docs/proof/workbench-loops-wl-1a/notebook-ops/**`.
**Registration lines only**, each under `coord claim --path <file>` for the minutes of the edit, in:
`src/AiDe.Core/Ipc/WorkspaceOperations.cs`, `WorkspaceClient.cs`, `IWorkspaceDispatch.cs`, and
`src/AiDe.Daemon/Program.cs`. These four files belong to T5, which is working on its own branch: add lines, change
none of T5's, and keep your additions in one contiguous block per file so the join can keep both.

## Proof
`docs/proof/workbench-loops-wl-1a/notebook-ops/` holds one `<check>-red.txt` / `<check>-green.txt` per op (`notebook-open`, `revision-save`,
`revision-latest`), plus `suite-green.txt`. T8 node 2 consumes these ops, so mail the Leader the green sha the moment
the three ops pass, before the suite run.

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader2-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **2**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl1a-notebook-ops && AGENT_SESSION=wl-t14-nbops AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t14-nbops --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t14-nbops AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl1a-notebook-ops` (branch `wl1a-notebook-ops`).** Every Bash command starts with `cd C:\Projects\ai-de-wl1a-notebook-ops && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl1a-notebook-ops\`. **Never** touch `C:\Projects\ai-de\`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-1a/notebook-ops/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-1a/notebook-ops/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-1a/notebook-ops/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-1a/notebook-ops/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t14-nbops` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t14-nbops" --session wl-t14-nbops --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
