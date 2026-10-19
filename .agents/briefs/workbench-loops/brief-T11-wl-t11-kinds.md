# Brief — T11 `wls-kinds-migration` (slice WL-S) · Claude Code · Opus

**Goal (plan row):** Split `session-document` into the `notebook` + `execution` kinds and migrate saved layouts with a report.
**Done when:** the `notebook` + `execution` kinds bind T8's models; slot migration golden round-trip with `.bak` and a
drop report; restore today's saved layout ⇒ report; `Ctrl+1/2/3` with focus inside a WebView2 page; hide/float/redock a
live notebook; the WL-S Proof Pack `docs/proof/workbench-loops-wl-s.md`. **Plus the merge conditions below.**
**Tier** T2 · **fan-out** 0 · **budget** 260 calls · 400k · 5 h, **split into two nodes** (GO9): node 1 = kinds + Landing
flip; node 2 = slot migration + the knowledge mode + Proof Pack. At 200 calls or 300k, hand off.
**Starts only when:** T4, T8 and T9 are on main. Design: `docs/design/workbench-loops-wl-s.md` (T4) §2, §6, §7, §8.

## Merge conditions (rulings — the slice does not close without them)
- **Ruling 152 (D2):** flip Factory's `Landing` from Left to **Center** in the **same change** that docks `notebook` in
  the Center. The build test pins the value, and the `simplify:` marker on the Factory row names this flip.
- **Ruling 149 (2):** a **knowledge-inclusive representation** as a **mode of the admitted canvas kind**, reached
  through the representation selector and never a second graph surface, with **its own E2E check (red first)**. The
  attach point is the `ExcludeKnowledge` seam at `WorkbenchShell.cs:1386` (T4 recorded it). **WL-S does not close until
  that check is green.** If it is too large for this track, the Leader splits it out as a named WL-S track; it is still
  a close condition.
- **Ruling 149 (1):** the WL-S Proof Pack states the interim in-app knowledge/spec corpus loss in user terms, with
  `docs/index.html` as the interim path, under Flagged risks.
- **Ruling 152 (D4):** the previous-perspective slot is re-added only with its first reader (drill-to-node return), not here.
- ADR-0045 §7: "a kind no longer admitted is dropped **with a report** (DC-219's control), a one-time `.bak` is written,
  and an unusable/off-screen placement recovers to the perspective default".

## Owned authored paths (WL-S window)
`src/AiDe.App/Workbench/SurfaceContentFactory.cs`, `LayoutPersistence.cs`, `src/AiDe.Core/Workbench/Perspectives.cs`
(the Landing flip only), new `src/AiDe.App/Workbench/Notebook/**` + `Execution/**`, the canvas kind's mode code
(name it), `tests/AiDe.App.Tests/Workbench/LayoutSlotMigrationTests.cs` + the knowledge-mode E2E test,
`docs/proof/workbench-loops-wl-s.md`, `docs/proof/workbench-loops-wl-s/kinds/**`.

## Not in scope
Protocol v2, completion sources (WL-2), named default layouts beyond what §6 requires for the demo.

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader2-20261019`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **2**. Decision requests go to the Owner
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
