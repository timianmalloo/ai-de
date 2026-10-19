# Brief — T8 `wl1a-notebook-binding` (slice WL-1a) · Claude Code · Opus · hard track

**Goal (plan row):** Bind the session document to a notebook revision and an attempt, so a run outlives its pane.
**Done when:** prepare-from-revision (v1 protocol, zero new kinds); edit ⇒ the old result is marked stale; hide the pane
mid-run, reopen, the run continues (the CTS has left the surface); the model re-key test (create/move/hide/close never
disposes a model); restart ⇒ the attempt and its events read back; UIA names; red then green. **Plus the ruling
obligations below.**
**Tier** T2 · **fan-out** 0 · **budget** 320 tool calls · 400k · 6 h, **split into two nodes** (GO9): node 1 =
model re-key + prepare-from-revision + stale marker; node 2 = run-outlives-pane + restart read-back + UIA + the T3
follow-ups. At 200 calls or 300k context, write `docs/proof/workbench-loops-wl-1a/binding/handoff.md` and stop.
**Starts only when:** T4 (re-row) is on main, and T5's design commit plus host-interface commit exist (read them from
T5's tree or `main`).

## Binding lines (quoted, DC-189)
- ADR-0045 §4: "those dictionaries are re-keyed from surface to model (`Dictionary<NotebookId, NotebookModel>`), and the
  active attempt's cancellation and queue move out of `SessionDocumentSurface` (`:77, :551, :567`) into the attempt host
  (ADR-0041). A surface instance *binds* to a model and may be created, moved, floated, hidden, closed and reopened
  without creating or disposing the model, so "Pane hidden. The run continues." is structurally true".
- ADR-0043 §2: "WL-1a needs **zero** new message kinds (v1 suffices for save/prepare from a revision)."
- §WL.11 WL-1a: "the **existing** `session-document` kind in the **existing** Coding host re-bound to notebook + attempt"
  (the host is now Factory, Ruling 152).

## Obligations from rulings and earlier tracks
- **Ruling 148 (tracked obligation):** record the **adoption actor** when a legacy notebook (`legacy=1`) or a
  `journey-terminal-drafts` draft (Ruling 150) is adopted into a user journey: an explicit user action, with the actor
  stored on the new notebook version row through T2's API.
- **T3 follow-ups:** `ComposerSurface.cs:980` and `:1022` say "running" for a `StopRequested` turn, which must use T3's
  copy ("Stop requested. Waiting for the harness to acknowledge."). `ConsoleSurface.cs:132` does not treat `Preparing` or
  `StopRequested` as live, and must.
- Copy (spec §C3): "This output belongs to an earlier draft. Run again to refresh it." · "Pane hidden. The run continues."

## Owned authored paths (WL-1a window)
`src/AiDe.App/Workbench/Sessions/SessionDocumentSurface.cs`, `SessionComposerBinder.cs`, `ConsoleDocumentHost.cs`,
`ConsoleSurface.cs`, `src/AiDe.App/Workbench/Composer/ComposerSurface.cs`, `ComposerSendGate.cs`,
`src/AiDe.App/Workbench/WorkbenchShell.cs` (session regions), `SurfaceContentFactory.cs` (the `session-document` row's
`Build` only), `CenterEmptyState.cs`, `PerspectiveShell.cs` (the session-document focus line only), `MainWindow.xaml.cs`
(session open/parallel-start lines only), new `src/AiDe.App/Workbench/Records/**`, new
`tests/AiDe.App.Tests/Workbench/Records/**`, `docs/proof/workbench-loops-wl-1a/binding/**`. IPC ops and store reads are T5's, so request them by seam.

## Not in scope
The `notebook`/`execution` kind split and slot migration (T11), protocol v2 (WL-2), the host internals (T5), the folds (T6).

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader-20261018`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **1**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl1a-notebook-binding && AGENT_SESSION=wl-t8-bind AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t8-bind --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t8-bind AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl1a-notebook-binding` (branch `wl1a-notebook-binding`).** Every Bash command starts with `cd C:\Projects\ai-de-wl1a-notebook-binding && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl1a-notebook-binding\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to wl-leader-20261018 --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-1a/binding/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-1a/binding/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-1a/binding/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-1a/binding/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t8-bind` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t8-bind" --session wl-t8-bind --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
    commit it; then `coord mail send --to wl-leader-20261018 --kind done --body "<exit evidence: commits, test names, proof files>"`.
    Your final message to the Leader is the same exit-evidence report: every commit sha (red and green),
    every test name added, every proof file, the suite result with pass/fail counts, any seam request
    raised, anything not done. **Your "done" is evidence, not authority** — the Leader re-runs and reads back.
13. **Every Markdown file you add under `docs/` starts with V2 frontmatter** (`id` unique, `title`, `type` — `proof-pack`
    for evidence READMEs, `design` for design docs, `decision-note` for notes — `status`, `owner: "@timianmalloo"`, `links`
    with at least one `implements` edge to its ADR or `architecture`, `review-by`, `summary`). Before your done report run
    `python docs/ai-forward-pack/scripts/docs-graph.py validate` and fix every **problem** it lists. Index drift alone is
    fine, because the join regenerates the index. A file with no frontmatter turned this gate from green to red at a join
    (2026-10-19).
