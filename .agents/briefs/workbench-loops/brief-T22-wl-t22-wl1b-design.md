# Brief — T22 `wl1b-design` (slice WL-1b) · Claude Code · Opus

**Goal (plan refresh, wl-leader3):** run `/design-slice` for WL-1b and cut it into red-first implementation tracks.
**Done when:** `docs/design/workbench-loops-wl-1b.md` is committed on branch `wl1b-design`. It holds the named patterns
and contracts, the E7 surface list, the WL-1b track cut (owned paths, checks red-first, dependencies), the draft human
demo script and the Proof Pack skeleton `docs/proof/workbench-loops-wl-1b.md`. The Patterns-Expert-vs-Simplifier
review and the Data & Persistence Architect's review are recorded in it.
**Tier** T2 · **fan-out** 0 (personas run inline, not as spawned agents) · **budget** 120 tool calls · 200k · this
is one node · **deadline** 1 h.
**Convergence:** a design doc another track can implement without re-reading the ADRs. **No production code, no tests.**
This node writes Markdown only. WL-1b's tracks run on branches; nothing joins until the user's WL-1a demo passes.

## Binding lines (quoted, DC-189; open them, do not work from this summary)
- `docs/architecture.md` §WL.11, the WL-1b row: "Evaluate one mandatory obligation, link evidence, see acceptance
  blocked with the named gap, record a disposition, accept the declared deliverable" · Real: "`checklist_obligation_fact`
  (+`mandatory`), `evidence_link_fact`, `disposition_fact` (obligation/work/intent kinds), the acceptance-eligibility and
  closure folds, a minimal `checklist` kind" · Mocked: "assessments absent" · Demo: "accept a plan deliverable without
  claiming implementation (AC-W03c)" · E2E red first: "acknowledged ≠ satisfied (a row vs a fold); acceptance blocked
  while a mandatory obligation lacks a current link; saved review state is not acceptance; FK/trigger overhead measured
  P1-PERF-style at 100×".
- ADR-0039 §1 table rows for Evidence link (:83), Checklist obligation (:85, "The row's existence is 'proposed'") and
  Disposition (:86); §3 (:129, :151): acceptance eligibility and closure are **fold-only, never stored**.
- ADR-0045 (:96): "`checklist` arrives with WL-1b".
- Spec `docs/specs/workbench-loops-ux.md` AC-W03a/b/c (:162-164), AC-P02c (:298); the Assurance/Checklist row (:109):
  "Human acknowledgment and demonstrated satisfaction are distinct facts".
- Ruling 156 (addendum-c): the adoption actor is a v3 column landing in the first adopting slice; until then the store
  refuses adoption. **Decide in the design, with the code cited, whether any WL-1b disposition is an adoption.** If one
  is, WL-1b is the first adopting slice and owes the v3 migration (expand-migrate-contract, golden round-trip). If none
  is, say so and cite why.

## Inputs on `main` (read the code; rule 14: the code outranks this brief)
- The tables already exist in schema v2: `src/AiDe.Core/Store/WorkspaceSchema.cs` (:204-207, :293-345). Read their
  columns, CHECKs and triggers. Read `StoreWriter.cs` / `StoreReader.cs` and `src/AiDe.Core/Store/Records/` for any
  writer or reader these rows already have. Do not assume one exists.
- The WL-1a folds `src/AiDe.Core/Workbench/Records/Folds/` (`AttemptStateFold`, `CurrentnessFold`). Acceptance
  eligibility needs currentness ("a current link"), so name how it consumes `CurrentnessFold`.
- `docs/design/workbench-loops-wl-1a.md` and `docs/design/workbench-loops-wl-s.md`, for the doc shape and for D6
  (every switch over `SurfaceEntry` lists Derived/Verb/TurnAction; how a `checklist` kind is admitted).
- The P1-PERF precedent: find the existing `P1-PERF` / 100× measurement in the repo and name the harness WL-1b reuses.

## The cut must respect the live WL-S window
T11 (`wls-kinds-migration`) and T21 (`wls-float`) are still running and own `SurfaceContentFactory.cs`,
`LayoutPersistence.cs`, `ZoneBackedLayoutService.cs` and the `WorkbenchShell.cs` regions they hold. Any WL-1b track that
must edit those files (the `checklist` kind) is scheduled **after** the WL-S joins, and its row says so. Core-only
tracks (folds, store writer/reader, the 100× measurement) depend on nothing in WL-S and may start at once. State each
track's dependencies as real data edges only (GO5).

## Owned authored paths
new `docs/design/workbench-loops-wl-1b.md`, new `docs/proof/workbench-loops-wl-1b.md` (skeleton: frontmatter, the
draft human demo script, empty evidence sections), `docs/proof/workbench-loops-wl-1b/design/**` (`handoff.md` only).

## Not in scope
Any `.cs`, test or schema change. Any WL-1c item. Re-deciding settled ADRs; a gap in one goes in the design as a
decision request draft (recommendation + default) for the Leader to raise, not a ruling.

## Node-specific overrides of the standing rules
- Rule 1: the skill is `design-slice`, not `implement`. Rule 7 (red first) and the suite run do not apply: this node
  writes no code. Run `python docs/ai-forward-pack/scripts/docs-graph.py validate` before done (rule 13).
- Rule 12: `--skill design-slice`, `--artifact docs/design/workbench-loops-wl-1b.md`.

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader3-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **3**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl1b-design && AGENT_SESSION=wl-t22-wl1b-design AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t22-wl1b-design --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t22-wl1b-design AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl1b-design` (branch `wl1b-design`).** Every Bash command starts with `cd C:\Projects\ai-de-wl1b-design && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl1b-design\`. **Never** touch `C:\Projects\ai-de\`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-wl-1b/design/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-wl-1b/design/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-wl-1b/design/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-wl-1b/design/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t22-wl1b-design` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t22-wl1b-design" --session wl-t22-wl1b-design --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
