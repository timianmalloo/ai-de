# Brief — T32 `wl-copy-pin` node 3 (the three stale labels) · Claude Code · Sonnet · node 3

**Goal:** the 7 DESIGN.md copy rows that node 2 marked `*(stale: …)*` say the true thing. The Owner (2026-10-20,
after the n2 join) ruled the single label "stale" wrong: it merges three different cases, and the rows cite the
Owner, so each must name its real state. Node 2 is joined; read its proof in
`docs/proof/workbench-loops-support/copy-pin/n2/` and the stale handling in
`tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs` before changing anything.

**The labels (Owner, exact):**
1. **SUPERSEDED** — the sentence was replaced; the label names the replacement.
   - RUN4 → `TurnCopy`'s DeliveryUnknown `ReasonSentence` (open it, cite `file:line`).
   - CHECK1 → CHK2 (`ChecklistCopy.Acknowledged`).
   - DEMO1 → mockup-only text: "prototype only, never product copy".
2. **NOT BUILT** — a spec or slice owes it; the label names the owner. Such a row returns to a pin when that slice lands,
   and is never left excluded.
   - NB1 "Draft saved on this device." → WL-2's draft store (ADR-0043).
   - RUN1 "No reasoning summary supplied." → WL-4 (spec `docs/specs/workbench-loops-ux.md:686`, slice row
     `docs/architecture.md:1509`; open both and confirm the lines say so before citing them).
3. **UNOWNED** — design copy with no spec story: "design-only, no owning story", kept for review by the next slice
   that touches runs or route auth. Never retired.
   - RUN3 and AUTH1 (0 hits outside DESIGN.md in `docs/`; re-run that search and save it).

Each row's marker drops "Owner ruling 2026-10-20" as the reason. It states its label and its named replacement or
owner. Only the date of the ruling stays, as `(Owner, 2026-10-20)`.

**This node, in this order (two checks, rule 16):**
- **(L1) The test learns the three labels, red first.** Replace the single `stale` marker with three:
  `*(superseded by <replacement>: …)*`, `*(not built, owed by <slice>: …)*` and `*(design-only, no owning story: …)*`.
  Choose the exact marker syntax, document it in the test's summary comment, and keep it parseable. The test holds:
  - (a) A row with any of the three labels is neither pinned nor on `UnmatchedRows`, as `stale` rows are today.
  - (b) If code renders the sentence of a **not built** or **design-only** row, the test fails, naming the label and
    the owner: "<id>: now rendered; the owning slice <slice> landed it, move the row back to a pin". A join that adds
    the constant is then told what to do.
  - (c) A **superseded** row whose named replacement no longer exists fails. The replacement is either a pinned row id
    or a `*Copy` member, checked by the same reflection the test already uses.
  - (d) The plain `stale` marker is no longer accepted. A row carrying it fails with "use one of the three labels".
  Red: run the new test against the DESIGN.md as it stands (the 7 rows still say `stale`) and see (d) fail naming all 7
  (`n3/L1-red.txt`). Also show (b) red with a scratch edit that adds NB1's sentence to a `*Copy` constant, and (c) red
  with a scratch rename of RUN4's replacement. Revert both scratch edits and never commit either. Save
  `n3/L1-red-b.txt` and `n3/L1-red-c.txt`.
- **(L2) DESIGN.md: the 7 rows relabelled** exactly as above. Then the test is green (`n3/L2-green.txt`).
- **Then** the whole App and Core suites, rule 22's ratchet, and rule 12. Update the DC-NEW-wl-t26-copy Status line to
  name the three labels.

**Known App failures on this machine** (T31, N=10): `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective`,
`ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation` and
`CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`. Name them as known. Any other failure is yours: run it alone and
report both results.

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 120k. **Kick** at 30 min by mail.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE` means no App run. Commit what is green, mail the Leader
`T32 n3 HELD by USER DEMO at <time>`, and end your turn with the same line.

## Owned authored paths
`tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs`; `DESIGN.md` (the markers of rows NB1, RUN1, RUN3, RUN4,
CHECK1, AUTH1, DEMO1 only); `docs/proof/workbench-loops-support/copy-pin/n3/**`; the DC-NEW-wl-t26-copy Status line in
`docs/lessons/defect-classes.md`. **No src change.**

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
   `cd C:/Projects/ai-de-wl-copy-pin-n3 && AGENT_SESSION=wl-t32-copy-pin-n3 AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t32-copy-pin-n3 --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t32-copy-pin-n3 AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:/Projects/ai-de-wl-copy-pin-n3` (branch `wl-copy-pin-n3`).** Every Bash command starts with `cd C:/Projects/ai-de-wl-copy-pin-n3 && `.
   Every Read/Edit/Write uses an absolute path under `C:/Projects/ai-de-wl-copy-pin-n3\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to <the Leader: coord leader who, read now> --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/copy-pin/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/copy-pin/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/copy-pin/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/copy-pin/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t32-copy-pin-n3` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t32-copy-pin-n3" --session wl-t32-copy-pin-n3 --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-support/copy-pin/` or a folder named for your track inside `C:/Projects/ai-de-wl-copy-pin-n3`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:/Projects/ai-de-wl-copy-pin-n3/docs/proof/workbench-loops-support/copy-pin/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t32-copy-pin-n3`
    from `C:/Projects/ai-de-wl-copy-pin-n3`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/copy-pin/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t32-copy-pin-n3-*` trees.
23. **A gate's refusal is answered by reading what it guards (DC-NEW-wl-t30-gate-evasion).** When a test, guard or gate
    refuses your change, first read its comment and the rule or ruling it cites, then decide: either the premise it
    guards still holds and your change is wrong, or the premise has become false and the gate is retired or replaced
    by a real control, with the reason written in the test. **Never** rename, reword, move or split code just to
    leave a lexical or structural gate's view. In the commit that answers a refusal, name the gate, the rule it
    cites and which of the two it was. T30 n5 renamed `SuggestedLinesAnnouncement` to pass
    `NoAssistPathExistsAnywhereInTheComposer`, while the prefill had made R21's assist path real (Ruling 180).
