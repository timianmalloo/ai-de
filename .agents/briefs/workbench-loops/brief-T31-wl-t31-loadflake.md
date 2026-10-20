# Brief — T31 `wl-loadflake` (DC-NEW-wl-leader3-b's measurement) · Claude Code · Sonnet · node 1

**Goal:** replace "it passed on a re-run" with a measured failure RATE. **DC-NEW-wl-leader3-b**
(`docs/lessons/defect-classes.md:8750`) records tests that fail inside the whole suite and pass alone. Its derive line
says a green re-run is not evidence of absence. Only a rate is. Instances so far:
`NoDispatchOutsideTheHostTests.ADeadHolder_NeverLocksTheUserOut_TheNextOpenSucceeds` (twice, most recently in the T32
join's first recount, 2026-10-20) and `TerminalHostInLifePathTests.ASessionWhoseChildExited_ReleasesItsHeadlessHostWhileTheOwnerLives`.
This node **measures**. It changes no code and no test. A deterministic repro is a later node, scoped by the Owner from
your rates.

**You are the only App runner on this machine while you run (Owner).** The measurement is the full-suite load rate,
and a parallel App run would change it. Do not start any other process that opens windows.

**This node, in this order:**
- **(M0) Build once:** `dotnet build AiDe.sln > docs/proof/workbench-loops-support/loadflake/build.txt 2>&1`, read its last 5 lines. Every run below uses
  `--no-build`, so each run measures the tests, not the compiler.
- **(M1) N = 10 full runs, numbered k = 1..10.** For each run, in this order:
  1. **Read the board as its own command** (rule 19's command). If the board's latest post on the demo reads
     `USER DEMO IN PROGRESS` with no later `USER DEMO DONE`, **start no new run**. Go to (M-hold).
     If a `join in flight` post has no matching `join done`, also go to (M-hold).
  2. `dotnet test tests/AiDe.Core.Tests --no-build --logger "trx;LogFileName=core-<k>.trx" --results-directory C:/Projects/ai-de-wl-loadflake/loadflake-runs > C:/Projects/ai-de-wl-loadflake/loadflake-runs/core-<k>.txt 2>&1`
  3. `dotnet test tests/AiDe.App.Tests --no-build --logger "trx;LogFileName=app-<k>.trx" --results-directory C:/Projects/ai-de-wl-loadflake/loadflake-runs > C:/Projects/ai-de-wl-loadflake/loadflake-runs/app-<k>.txt 2>&1`
  4. Append ONE line to `docs/proof/workbench-loops-support/loadflake/runs.txt`: `k  start(UTC)  end(UTC)  core passed/failed/skipped  app passed/failed/skipped`,
     with times from `date -u +%H:%M:%SZ` read in that turn and counts from the summary line of each `.txt` (read only
     its last lines, never the whole output).
  Do **not** set `AIDE_REAL_ADAPTER_ROOT`, `AIDE_REAL_PROJECTION_ROOT` or any real-lane variable. Those tests stay NotExecuted.
  `C:/Projects/ai-de-wl-loadflake/loadflake-runs/` is never committed (raw `.trx` and logs). Never read a `.trx` file into your context.
- **(M2) Rates.** Write `docs/proof/workbench-loops-support/loadflake/rates.py`, a stdlib script (a file, then a run; rule 6) that reads every
  `loadflake-runs/*.trx` and writes `docs/proof/workbench-loops-support/loadflake/rates.md`: one row per test that failed in at least one run, with suite,
  failures/runs (for example `3/10`), the run numbers, and the first line of each distinct failure message.
  A test that never failed is not listed, but the header states the total test count per suite and N. Any
  `subprocess` or `open` call carries `encoding="utf-8", errors="replace"` (the `verify-subprocess-utf8` and
  `verify-portable-text-io` gates).
- **(M3) Alone.** For each test in `rates.md`, run it alone 10 times with
  `dotnet test <project> --no-build --filter "FullyQualifiedName~<method>"`, output to
  `C:/Projects/ai-de-wl-loadflake/loadflake-runs/alone-<method>-<i>.txt`, and record `alone failures/10` in `rates.md`. Put the loop in a
  file and run that (rule 6). Read the board before this step too (step 1's rule).
- **(M4) Classify each listed test** in `rates.md`, one word plus the evidence:
  `load-only` (fails in the suite, 0/10 alone) · `fails-alone` (fails alone too; for example the restored-row fold
  `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`, a known keyboard test owed to a desk
  run) · `intermittent` (fails sometimes alone). For each class instance named above that did **not** fail in any run,
  write `NOT REPRODUCED, N=10` in the README. Give no cause. A cause without a repro is a guess (NG1). Cite the test's
  `file:line` so the repro node can start from it.
- **(M5) Register:** under DC-NEW-wl-leader3-b, add one `- **Measured (2026-10-20, T31, N=<n>):**` line after the
  Control line. It carries each listed test's in-suite and alone rates and a pointer to `rates.md`. Leave **Status**
  `partially-controlled`; the repro node closes it.
- **(M6)** README (V2 frontmatter, rule 13): the method, N, the per-run table (a copy of `runs.txt`), the rates table,
  the classification, and what was not measured. Then rule 22's ratchet and rule 12. The done mail carries the rates
  table in one line per test.

**(M-hold) The user's demo stops you; it does not start you (Owner, 2026-10-20).** The run in flight finishes; start no
new one. Write `runs.txt` with the runs so far, write `rates.md` from the completed runs only, headed
**`PARTIAL - k of 10 runs, held by USER DEMO IN PROGRESS`**, and commit. Never extrapolate a rate to N. Then send
`coord mail send --to wl-leader5-20261020 --kind note --body "T31 PARTIAL k/10, held by USER DEMO at <time>"` and end
your turn with the same line as your final message. The Leader resumes you on `USER DEMO DONE`. You then re-read the
board and continue from run k+1. The partial header is replaced when N completes.

**No red-first in this node** (rule 7 does not apply: no check is added and no code changes; the proof is the
measurement). **No whole-suite run is owed beyond M1** (rule 18 is met by M1's runs: name the last run's counts as the
suite result in the done mail).

**Tier** T2 · **fan-out** 0 · **budget** 120 tool calls · 200k. **Kick:** none by time. A Leader kick mail means:
finish the run in flight, then do M2 to M6 on the runs completed, labelled PARTIAL as in (M-hold).
**Rules 19, 20, 21 and 22 apply.** Rule 16's "never stop on your own wall-clock estimate" applies: the node ends at N=10,
at the hold, at a kick or at the cap.

**The Leader is `wl-leader5-20261020`, epoch 5.** Where the common rules below name any other Leader id or epoch, read
these.

## Owned authored paths
`docs/proof/workbench-loops-support/loadflake/**` (README, `runs.txt`, `rates.md`, `rates.py`, `build.txt`, `ratchet.txt`,
`handoff.md`); the DC-NEW-wl-leader3-b entry's new Measured line in `docs/lessons/defect-classes.md`;
`C:/Projects/ai-de-wl-loadflake/loadflake-runs/**` (never committed). **No src, test or DESIGN.md change.** A change there is a seam request.

## Standing rules for this track (CTX-F — every line binds)

You are a **Sub-Agent** in the workbench-loops drive. The Leader is `wl-leader5-20261020`. The
coordination plan is `docs/coordination/coordination-workbench-loops.md` (your row in "Tracks" and
"Track contracts" is your contract). Leader epoch: **5**. Decision requests go to the Owner
`wl-owner-20261018` (`python docs/ai-forward-pack/scripts/coord-decide.py request --to wl-owner-20261018 ...`) — only
for a genuine product trade-off the repo, spec, ADRs or a spike cannot answer, with a recommendation
and a default.

1. **First command, before any reading:** in your tree,
   `cd C:\Projects\ai-de-wl-loadflake && AGENT_SESSION=wl-t31-loadflake AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t31-loadflake --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t31-loadflake AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl-loadflake` (branch `wl-loadflake`).** Every Bash command starts with `cd C:\Projects\ai-de-wl-loadflake && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl-loadflake\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to wl-leader5-20261020 --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/loadflake/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/loadflake/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/loadflake/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/loadflake/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t31-loadflake` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t31-loadflake" --session wl-t31-loadflake --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
    commit it; then `coord mail send --to wl-leader5-20261020 --kind done --body "<exit evidence: commits, test names, proof files>"`.
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
    scratch files go under `docs/proof/workbench-loops-support/loadflake/` or a folder named for your track inside `C:\Projects\ai-de-wl-loadflake`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:\Projects\ai-de-wl-loadflake/docs/proof/workbench-loops-support/loadflake/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t31-loadflake`
    from `C:\Projects\ai-de-wl-loadflake`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/loadflake/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t31-loadflake-*` trees.
23. **A gate's refusal is answered by reading what it guards (DC-NEW-wl-t30-gate-evasion).** When a test, guard or gate
    refuses your change, first read its comment and the rule or ruling it cites, then decide: either the premise it
    guards still holds and your change is wrong, or the premise has become false and the gate is retired or replaced
    by a real control, with the reason written in the test. **Never** rename, reword, move or split code just to
    leave a lexical or structural gate's view. In the commit that answers a refusal, name the gate, the rule it
    cites and which of the two it was. T30 n5 renamed `SuggestedLinesAnnouncement` to pass
    `NoAssistPathExistsAnywhereInTheComposer`, while the prefill had made R21's assist path real (Ruling 180).
