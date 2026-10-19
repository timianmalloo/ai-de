# Brief — T6 `wl1a-folds` (slice WL-1a) · Claude Code · Sonnet

**Goal (plan row):** Derive non-terminal attempt state and currentness as pure folds.
**Done when:** the pure folds (ADR-0041 §2: Running / Waiting / Denied / OverLimit from the run log;
`StopRequested` dominates `Waiting`; a cited seq absent ⇒ Not recorded); currentness (ADR-0040 §5); fixture
tests red then green; a `fold.duration_ms` emission test.
**Tier** T2 · **fan-out** 0 · **budget** 140 tool calls · 400k · 3 h · **deadline** 3 h.
**Convergence:** the fold fixtures are green. No UI, no host, no store writes.

## Binding lines (quoted, DC-189)
- ADR-0041 §2: "Running / Waiting (one state for input *or* permission, per spec §C2) / Denied (last permission
  decision on the attempt was reject, no later event) / OverLimit (a budget or context-ceiling event) | **fold over
  the durable run log** for `run_id`; an uncorrelated `acp.result`/`acp.error` never yields a terminal state" ·
  "The fold is pure, implemented once in Core, and reads Not recorded for any cited seq the log lacks. Fold
  store-writes (none exist by design — terminals are the host's) never run inside the host's drain sink" ·
  §4 "Fold precedence: `StopRequested` dominates `Waiting`." · terminal states come from the outcome rows (T2's
  `OutcomeRow`), never from events.
- ADR-0040 §5: "A journey's scope snapshot is compared to the live scope on every open/refresh; a changed HEAD, PR
  head, intent revision or dirty-content hash marks dependent preparations, assessments and obligations `stale` by
  a derived rule, retaining the historical rows (AC-P01c, AC-B02b)." For WL-1a the consumer is "edit and see the
  old result marked stale": an attempt bound to `(notebook_id, revision_no, projection_sha)` is stale when the
  notebook's current revision or the live envelope's `projection_sha` differs. Copy: "This output belongs to an
  earlier draft. Run again to refresh it." (spec §C3).
- ADR-0041 §7: "`fold.duration_ms` emitted to the rolling log" — find how other metrics are emitted to the rolling
  log/sidecar in Core and reuse that path; the test asserts one emission per fold evaluation.

## Inputs on `main` (read the code)
T2's record types (`src/AiDe.Core/Store/Records/`), T3's `RunEvent` and `TurnState`, and T5's design doc
`docs/design/workbench-loops-wl-1a.md` (the run-log line format and the fold's inputs). The fold's output is a
`TurnState` value plus `HasEvidence` (a derived bool, **not** a state; for WL-1a it reads `evidence_link_fact`
through T2's reader and is false when none exist).

## Owned authored paths
new `src/AiDe.Core/Workbench/Records/Folds/AttemptStateFold.cs`, `CurrentnessFold.cs` (+ small helper types in the
same folder), new `tests/AiDe.Core.Tests/Workbench/Folds/**`, `docs/proof/workbench-loops-wl-1a/folds/**`.

## Not in scope
Writing any row; the host; the relay subscriber wiring (T5 wires the fold channel); UI (T8); acceptance/closure
folds (WL-1b); readiness (WL-3).

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader-20261018`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **1**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl1a-folds && AGENT_SESSION=wl-t6-folds AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t6-folds --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t6-folds AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl1a-folds` (branch `wl1a-folds`).** Every Bash command starts with `cd C:\Projects\ai-de-wl1a-folds && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl1a-folds\`. **Never** touch `C:\Projects\ai-de\`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-1a/folds/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-1a/folds/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-1a/folds/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-1a/folds/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t6-folds` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t6-folds" --session wl-t6-folds --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
