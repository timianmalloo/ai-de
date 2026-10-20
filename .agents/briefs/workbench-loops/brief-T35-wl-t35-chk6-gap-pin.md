# Brief — T35 `wl-chk6-gap-pin` (CHK6's gap clauses, pinned against DESIGN.md) · Claude Code · Sonnet · node 1

**Goal:** every blocking-gap sentence that Core's `AcceptanceFold` writes is pinned by a **Core test** against its
DESIGN.md text, and every sentence has DESIGN.md text to pin against. This is the docs-and-test node of the Owner's T32
ruling (2) (`.agents/briefs/workbench-loops/obligations.md`, "Owner rulings on T32's findings"; Owner, 2026-10-20).
CHK6 is pinned today on `Ready to accept.` only (`tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs:129`;
`docs/proof/workbench-loops-support/copy-pin/README.md:37`), because the gap clauses are inline interpolated literals
with no constant to pin.

**What is known (read at base, confirm by opening):**
- The fold writes **7** gap sentences, one per `AcceptanceGapCodes` value, in
  `src/AiDe.Core/Workbench/Records/Folds/AcceptanceFold.cs`: lines 77-78 (checklist-empty), 83-84 (intent-missing),
  94-95 (intent-not-accepted, `{revision}`), 185-190 (evidence-stale, evidence-not-recorded, evidence-missing, each with
  the obligation's text), 196-197 (needs-decision).
- DESIGN.md's CHK6 row (`DESIGN.md:1433`) lists **3** of them, in its parenthesis separated by ` · `, with the
  placeholders `{obligation}` and `{n}`: no current evidence, intent revision not accepted, no obligations declared.
- So **4** sentences have no DESIGN.md text: intent-missing, evidence-stale, evidence-not-recorded, needs-decision.
  Confirm this count yourself before writing anything; if it differs, report the difference in the done mail.
- Existing fold tests: `tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceFoldTests.cs` (how to build each input state).
  Repo-root lookups in Core tests: for example `tests/AiDe.Core.Tests/Sessions/SessionPathContractTests.cs:178`.

**The oracle rule (DC-NEW-wl-t26-copy; Owner, binding):** the expected text is read **from DESIGN.md**, never from the
code under test. The test may not copy a sentence out of `AcceptanceFold.cs` into an expected string, and may not read
`AcceptanceGap.Sentence` to build its own expectation.

**This node, in this order (two checks, rule 16):**
- **(G1) The 4 missing clauses go into DESIGN.md, copy only.** Add each to CHK6's parenthesised list, in the same
  ` · `-separated form and with the same placeholders (`{obligation}`, `{n}`). Each clause is the text the code renders
  today (rule 14: the code on main outranks the document). Change nothing else in DESIGN.md. Keep CHK6's cell starting
  with `Ready to accept.`, so the App pin on CHK6 still holds. Choose your own way only if you find a reason this form
  cannot work; then say why in the README and the done mail.
- **(G2) The Core test, red first.** One new test file,
  `tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceGapSentencesMatchDesignTests.cs`. It:
  - reads DESIGN.md's CHK6 row and parses its clause list;
  - drives `AcceptanceFold.Evaluate` to each of the 7 gap codes, with known values for the obligation text and the
    revision number;
  - fills the DESIGN.md clause's placeholders with those same known values, and asserts the fold's sentence equals it;
  - fails if any gap code has no clause, or any clause matches no gap code. The failure names the code or the clause.
  **Red 1:** run it against DESIGN.md as it stands at base. It must fail naming the 4 missing clauses
  (`docs/proof/workbench-loops-support/chk6-gap-pin/G2-red-missing.txt`). Then do G1, and the test is green (`docs/proof/workbench-loops-support/chk6-gap-pin/G2-green.txt`).
  **Red 2, the mutation (Owner):** change one clause in a **scratch** copy of `AcceptanceFold.cs` (for example a word
  in the evidence-missing sentence), run the test, see it fail naming that clause, and restore the file
  (`docs/proof/workbench-loops-support/chk6-gap-pin/G2-red-mutation.txt`). The scratch edit is never committed; `git diff --stat src/` must be empty before
  every commit.
- **(G3) A disagreement is reported, never fixed.** If a sentence the code renders and its existing DESIGN.md clause
  disagree (any of the 3 existing clauses), change **neither**. Put the clause on a shrink-only list in the test with
  the reason "text differs, Owner to rule", put both texts in the README, and name it first in the done mail. (At base
  the Leader read the 3 existing clauses as equal to the code's text; confirm this, do not assume it.)
- **Then** the whole Core suite and the whole App suite (mandatory, Owner), rule 22's ratchet, and rule 12. Update the
  DC-NEW-wl-t26-copy Status line to say CHK6's gap clauses are pinned by the new Core test.

**Known App results on this machine.** Name these as known, never as new failures:
- `ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`: record it as **NOT CHECKED: the OS
  reports the left mouse button down** (Owner, 2026-10-20). It is not a failure of this node.
- `CtrlDigit_WithFocusInsideTheComposerOrTheCanvasPage_SwitchesThePerspective` and
  `CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`: known failures (T31, N=10).
Any other failure is yours: run it alone and report both results. Never inject mouse or keyboard input outside a
test's own window, and never call `SendInput` or `mouse_event` to change the desktop's state.

**Tier** T2 · **fan-out** 0 · **budget** 80 tool calls · 120k. **Kick** at 30 min by mail.
**Rules 19, 20, 21, 22 and 23 apply.** Check the board before every App run, as its own command. A
`USER DEMO IN PROGRESS` post with no later `USER DEMO DONE`: finish the run in flight, commit what is green, mail the
Leader `T35 HELD by USER DEMO at <time>`, and end your turn with the same line.

## Owned authored paths
`tests/AiDe.Core.Tests/Workbench/Folds/AcceptanceGapSentencesMatchDesignTests.cs` (new). `DESIGN.md`: the CHK6 row's
cell only. `docs/proof/workbench-loops-support/chk6-gap-pin/**`. The DC-NEW-wl-t26-copy Status line in `docs/lessons/defect-classes.md`. A scratch edit of
`src/AiDe.Core/Workbench/Records/Folds/AcceptanceFold.cs` that exists only during Red 2 and is **never committed**.
**No src change. No change to `tests/AiDe.App.Tests/DesignCopyRowsArePinnedTests.cs`**: if its CHK6 comment now reads
wrong, name it in the done mail as a finding.

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
   `cd C:/Projects/ai-de-wl-chk6-gap-pin && AGENT_SESSION=wl-t35-chk6-gap-pin AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t35-chk6-gap-pin --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t35-chk6-gap-pin AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:/Projects/ai-de-wl-chk6-gap-pin` (branch `wl-chk6-gap-pin`).** Every Bash command starts with `cd C:/Projects/ai-de-wl-chk6-gap-pin && `.
   Every Read/Edit/Write uses an absolute path under `C:/Projects/ai-de-wl-chk6-gap-pin\`. **Never** touch `C:\Projects\ai-de\`
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
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/chk6-gap-pin/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/chk6-gap-pin/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/chk6-gap-pin/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/chk6-gap-pin/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t35-chk6-gap-pin` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t35-chk6-gap-pin" --session wl-t35-chk6-gap-pin --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-support/chk6-gap-pin/` or a folder named for your track inside `C:/Projects/ai-de-wl-chk6-gap-pin`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:/Projects/ai-de-wl-chk6-gap-pin/docs/proof/workbench-loops-support/chk6-gap-pin/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t35-chk6-gap-pin`
    from `C:/Projects/ai-de-wl-chk6-gap-pin`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/chk6-gap-pin/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t35-chk6-gap-pin-*` trees.
23. **A gate's refusal is answered by reading what it guards (DC-NEW-wl-t30-gate-evasion).** When a test, guard or gate
    refuses your change, first read its comment and the rule or ruling it cites, then decide: either the premise it
    guards still holds and your change is wrong, or the premise has become false and the gate is retired or replaced
    by a real control, with the reason written in the test. **Never** rename, reword, move or split code just to
    leave a lexical or structural gate's view. In the commit that answers a refusal, name the gate, the rule it
    cites and which of the two it was. T30 n5 renamed `SuggestedLinesAnnouncement` to pass
    `NoAssistPathExistsAnywhereInTheComposer`, while the prefill had made R21's assist path real (Ruling 180).
