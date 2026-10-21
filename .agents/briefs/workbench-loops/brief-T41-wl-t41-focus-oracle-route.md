# Brief — T41 `wl-focus-oracle-route` (a focus or live-region oracle after a layout command is reached by the user's route) · Claude Code · Sonnet · node 1

**Goal:** the layout-command class gate (T21 n9, `LayoutOraclesDriveTheCommandRouteTests`) misses a test that asserts
a **focus landing** after a layout command applied off the route. Owner ruling 2026-10-21 (`obligations.md`, the
"T41 wl-focus-oracle-route" row). Docs-and-test only: **no src change.**

**What exists (read before you change it; rules 14 and 23):**
- `tests/AiDe.App.Tests/Sessions/ConsoleSplitPlacementTests.cs:159-165`, `CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen`:
  `shell.Factory.Service.Apply(new LayoutOperation.CloseSurface(...))` (`:159`), then `shell.Factory.Adapter.Render()`
  by hand (`:160`), then a `Keyboard.FocusedElement` assert, "focus after the close is on (none), not the landing"
  (`:165`). The close does not go through the route, and the route's close handling owns the focus landing.
- The route helper: `FloatRenderTests.CloseByTheRoute(DockHost host, string surfaceId)`
  (`tests/AiDe.App.Tests/Workbench/FloatRenderTests.cs:129`); see T21 n9/n10's conversions for the other helpers.
- The gate: `tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs`. Its `Oracle` regex (`:45-46`)
  covers the docking layout tree, the rail, `ContentFor` and `ActiveSurfaceId`, but **no keyboard-focus read and no
  live-region text**. Its `Command` regex already counts `.Apply(new LayoutOperation.X)` as off the route.
- The rate so far (the focus-none measurements): ConsoleSplit…CollapsingTheLeft… failed in 3 of about 31 recorded
  full runs (the T21 n11 join recount; T36 run 4; T40 run 5), each time "(none)", and passed 10/10 alone (T36).
  `TheFactoryLanding_IsTheNotebookInTheCenter_…` (`tests/AiDe.App.Tests/Sessions/SessionDocumentPlacementTests.cs:99`)
  failed once (T36 run 6), "focus landed on (none), not inside the composer".
  `docs/proof/workbench-loops-support/focus-none/README.md` and `flake-remeasure/rates.md`.
- **Inferred, not checked (the Owner's reading):** the occasional "(none)" may be the by-hand path not running the
  route's focus landing, with focus surviving only by a render side effect that is timing-dependent under load. This
  node does not claim that cause; G4 measures.

**This node, in this order:**
- **(G1) Red, then green: widen the gate's oracles.** Add keyboard-focus reads (`Keyboard.FocusedElement`,
  `FocusManager.GetFocusedElement`, `IsKeyboardFocusWithin`) and the live-region text read (find the live-region
  read the tests use; name it in the README) to the oracle set, after a layout command.
  - **Red:** on today's tree the widened gate must fail and **name ConsoleSplit…CollapsingTheLeft…**. Save the output.
  - **Mutation red:** as in T21 n9 (its README in `docs/proof/workbench-loops-wl-s/layout-gate/`): a planted
    off-route close followed by a focus read in a fixture string must be caught.
  - **Evasion red:** a focus read reached through a local alias or a helper in the same file must still be caught,
    or the gate's residual says why not.
- **(G2) A recall search, independent of the gate** (T21 n9's V2b shape). Put it in a file and run it (rule 6): every
  App test that reads keyboard focus or live-region text after a layout command or a `Render()`. It must cover
  `TheFactoryLanding_…`: say whether it has the same shape (a layout command off the route, then a focus oracle) or
  not, with file:line. Raw hits in `docs/proof/workbench-loops-support/focus-oracle-route/G2-hits.txt`; each hit converted, or listed with a reason.
- **(G3) Convert** every test the gate or G2 names to the route: `CloseByTheRoute` and the existing helpers. No
  rename and no wrapper. **A test that fails on the route stays failing**: mark it NOT CHECKED only through T37's
  `[CheckedFact]` mechanism if, and only if, its failure is a non-default input state. Otherwise leave it failing and
  record finding `T41-F<k>` with the failure text. **No src fix.**
- **(G4) Suites and the rate.** Core and App whole suites (rule 18), with the trx logger. Then
  ConsoleSplit…CollapsingTheLeft… alone 10 times (`--filter "FullyQualifiedName~CollapsingTheLeftHoldingTheSession"`),
  in a loop file. Its in-suite rate after conversion is taken by the join's recount. Report both as measurements
  beside T36's and T40's; claim no cause.
- **(G5) Register:** in DC-NEW-wl-t21-float-d (the gate's class), a new instance line (a focus oracle after an
  off-route command, missed by the oracle set), the widened Control line, and the residual. Rule 11 for anything new.
- **(G6) README** (V2 frontmatter, rule 13), then rule 22's ratchet and rule 12. The done mail carries: G1's red
  naming ConsoleSplit, the mutation and evasion results, G2's counts and TheFactoryLanding's verdict, each conversion,
  each T41-F<k>, the suites, and ConsoleSplit's alone rate.

**First write:** claim your proof directory, `coord claim --wi workbench-loops --path docs/proof/workbench-loops-support/focus-oracle-route` (no trailing slash),
then each file outside it before its first edit. **A claim's exit status is its result**: non-zero is not granted. On
`COORD-CLAIM-STALE-CONTROLS`, `git merge origin/main` and claim again before the edit. The join's pre-join check
refuses any unclaimed changed path, and there are no more excuses.
**Never inject input.** App suites hold the desktop: rules 19 and 20 apply.

**Tier** T2 · **fan-out** 0 · **budget 88 tool calls · 224k** (GO19, `.agents/briefs/workbench-loops/node-actuals.md`:
gate+sweep, the median of T21 n9, n10, n11 and T37: 70 calls / about 179k, plus 25%). **Kick** by mail at about 32 min
(ladder: the fastest gate+sweep rate on record, T21 n10, about 7.0k tokens/min, puts 224k at about 32 min; 120 calls ×
T21 n9's 27.6 s/call is about 55 min, so the token bound is earlier). A kick means: commit what is green, run G4's
suites, then G5-G6 on what is done, labelled PARTIAL. Rules 16, 18, 19, 20, 21, 22, 23 and 24 apply.

## Owned authored paths
- `tests/AiDe.App.Tests/Workbench/LayoutOraclesDriveTheCommandRouteTests.cs`.
- `tests/AiDe.App.Tests/Sessions/ConsoleSplitPlacementTests.cs` and each test file G1 or G2 names (conversion only).
- The DC-NEW-wl-t21-float-d entry in `docs/lessons/defect-classes.md`.
- `docs/proof/workbench-loops-support/focus-oracle-route/**`.
- `C:\Projects\ai-de-wl-focus-oracle-route/test-runs/**` (never committed).

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
   `cd C:\Projects\ai-de-wl-focus-oracle-route && AGENT_SESSION=wl-t41-focus-oracle-route AGENT_HOST=claude AGENT_WI=workbench-loops python docs/ai-forward-pack/scripts/audit-log.py start --session wl-t41-focus-oracle-route --skill implement`.
   Prefix **every** coord / audit-log / git command with `AGENT_SESSION=wl-t41-focus-oracle-route AGENT_HOST=claude AGENT_WI=workbench-loops`.
2. **Your tree is `C:\Projects\ai-de-wl-focus-oracle-route` (branch `wl-focus-oracle-route`).** Every Bash command starts with `cd C:\Projects\ai-de-wl-focus-oracle-route && `.
   Every Read/Edit/Write uses an absolute path under `C:\Projects\ai-de-wl-focus-oracle-route\`. **Never** touch `C:\Projects\ai-de\`
   (the primary checkout) or another track's tree. **Never call `EnterWorktree` or `ExitWorktree`.**
3. **Windows:** run `python`, never `python3`. `coord <verb>` = `python docs/ai-forward-pack/scripts/coord-core.py <verb>`.
4. **Owned authored paths** are listed below. You edit nothing else. Need a change outside them →
   `coord request add --to <the Leader: coord leader who, read now> --deadline 1800 --fallback "<what you do meanwhile>" "<the request>"`
   and carry on with what you own. `derived` files (docs/docs-index.js, docs/audit/audit-data.js,
   docs/audit/index.html, docs/api/*.md, docs/_site/*, docs/_meta.json) are regenerated at the join —
   do not edit or claim them. `register` files (docs/audit/*.jsonl, .agents/log/*.jsonl) are
   append-only and **never claimed**. Do not edit the test-count baseline; the join recounts.
5. **Claim for the minutes of the edit:** `coord claim --wi workbench-loops --path <file>` (default TTL; `coord claim` refuses without `--wi` and does not read `AGENT_WI`) just before
   editing a file, `coord release` after the commit. Never a lifetime lease.
   **At your first write, claim your proof directory as a directory lease:** `coord claim --wi workbench-loops --path docs/proof/workbench-loops-support/focus-oracle-route` (no trailing slash: a claim on `docs/proof/workbench-loops-support/focus-oracle-route/` is granted and
   covers nothing, measured 2026-10-20). Output files written there by redirection then carry a claim; the join's
   `leader_prejoin.py --branch --session` check refuses any changed path with no claim from your session.
   **A claim's exit status is its result:** non-zero means NOT granted, whatever the text says. On `COORD-CLAIM-STALE-CONTROLS`
   (exit 3), run `git merge origin/main` in your tree and claim again before the edit; any other refusal, mail the Leader.
   This line is prose for the CLI's STALE-CONTROLS, REGISTER-CLASS, DERIVED-CLASS and TTL-CAP refusals: they write no
   record, so the join's claimed-after-refusal check cannot see them (PD-7).
6. **A multi-line program is a file, then a run** — never a heredoc program. **A gate's exit status is
   never behind a pipe**: run the test/build on its own line and read `$?`, or redirect output to a
   file and then read the file.
7. **Red first on every check named in your exit evidence.** For each: write the test, run it, see it
   fail for the right reason, save the output to `docs/proof/workbench-loops-support/focus-oracle-route/<check>-red.txt`, commit
   (`test: red — <check>`); then implement, re-run, save `docs/proof/workbench-loops-support/focus-oracle-route/<check>-green.txt`, commit. A test
   that never failed is not evidence. Tests: `dotnet test <project> --filter <...> -nologo` (node reuse
   is off at the boundary via Directory.Build.rsp). Before you report done, the **whole** affected test
   projects must be green (`dotnet test tests/AiDe.Core.Tests -nologo`, and `tests/AiDe.App.Tests`
   when you touched App) — save that run as `docs/proof/workbench-loops-support/focus-oracle-route/suite-green.txt`.
8. **Context ceiling 400k tokens.** Near ~350k: commit what is green, write
   `docs/proof/workbench-loops-support/focus-oracle-route/handoff.md` (what is done, what is red, the next step), and stop with a partial report.
9. **Commit on your branch only; do not push; do not merge.** Commit messages end with
   `Co-Authored-By: Claude sonnet <noreply@anthropic.com>`. The Leader joins via `conductor-join.py`.
10. **No dead code survives** (HYG-A): no commented-out code, no unused members.
11. A new defect class you discover → append it at the end of `docs/lessons/defect-classes.md` with id
    `DC-NEW-wl-t41-focus-oracle-route` (class / sweep / derive / prevent), in the same commit as the fix.
12. **Last actions:** `python docs/ai-forward-pack/scripts/audit-log.py append --shortname "wl-t41-focus-oracle-route" --session wl-t41-focus-oracle-route --skill implement --kind skill --prompt "<your goal line>" --summary "<what landed>" --artifact <main file> --goal "<goal>" --done-when "<done when>" --tier T2 --fan-out 0`;
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
    scratch files go under `docs/proof/workbench-loops-support/focus-oracle-route/` or a folder named for your track inside `C:\Projects\ai-de-wl-focus-oracle-route`. Never use the session
    scratchpad, `%TEMP%` or any other shared path: sub-agents share one session scratchpad, and T21's suite overwrote
    T28's result files there (2026-10-19 ~16:05Z), so T28 had to discard a whole suite run.
22. **A node that hands off for a join runs the step-8 ratchet first (DC-NEW-wl-leader3, 3rd instance).** The T11 join
    was refused by three gates that no track suite runs: `docs-graph.py validate` (an orphan hand-off),
    `tools/verify-cited-controls.py` (a comment citing a renamed test) and `tools/verify-surface-ownership.py` (a new
    `*Surface.cs` with no owner in `docs/collaboration/session-contracts.md` §2). Before the done mail of a node whose
    next step is a join, run
    `RATCHET_OUT_DIR=C:\Projects\ai-de-wl-focus-oracle-route/docs/proof/workbench-loops-support/focus-oracle-route/ratchet sh .agents/briefs/workbench-loops/run_ratchet.sh <origin/main sha> <your head sha> wl-t41-focus-oracle-route`
    from `C:\Projects\ai-de-wl-focus-oracle-route`. Save its refusal lines and last line in `docs/proof/workbench-loops-support/focus-oracle-route/ratchet.txt`. A `REFUSE green->red` is yours to fix
    before done, **except the three derived-view gates**: `docs-graph.py` (when its only defect is index drift),
    `tools/verify-derived-views.py` and `tools/verify-site-figures.py`. Join step 6 regenerates those views before the
    join's own ratchet, so a branch head is always red there (T11 n13, 2026-10-19). Name them in `ratchet.txt` as
    expected. Do not commit the `ratchet/` folder. The script makes and removes its own `C:/Projects/rt-wl-t41-focus-oracle-route-*` trees.
23. **A gate's refusal is answered by reading what it guards (DC-NEW-wl-t30-gate-evasion).** When a test, guard or gate
    refuses your change, first read its comment and the rule or ruling it cites, then decide: either the premise it
    guards still holds and your change is wrong, or the premise has become false and the gate is retired or replaced
    by a real control, with the reason written in the test. **Never** rename, reword, move or split code just to
    leave a lexical or structural gate's view. In the commit that answers a refusal, name the gate, the rule it
    cites and which of the two it was. T30 n5 renamed `SuggestedLinesAnnouncement` to pass
    `NoAssistPathExistsAnywhereInTheComposer`, while the prefill had made R21's assist path real (Ruling 180).
24. **Never prompt the user directly (DC-NEW-wl-leader5-b).** No node and no Leader calls `AskUserQuestion`. A question
    for the user goes in your mail to the Leader, who sends it to the Owner; the Owner relays it. A direct prompt blocks
    the session until the user answers. The Leader's did for about three hours on 2026-10-20: its pin lapsed, and the
    Owner's rulings queued unread.
