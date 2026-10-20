---
id: handoff-workbench-loops-wl-1a-prefill-n1
title: "Hand-off: WL-1a T30 node 5 (the suggested lines are seen; the first-50 reader; the sheet tests' gate)"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, wl-1a, handoff, compile-mode, ruling-180, ruling-182]
links:
  - { to: architecture, rel: implements }
  - { to: proof-workbench-loops-wl-1a-prefill, rel: relates-to }
review-by: "2026-11-19"
summary: >-
  T30 n5: a prepare that suggests lines opens the fold, keeps focus in the editor, announces the count once and names
  each line '? suggested'; tools/compile-eval/first-calls.py reads the first 50 non-reused advisory calls; the four sheet
  test files inject the compile-mode gate, with a guard. Each red then green. Next: T30's second join. Earlier nodes'
  hand-offs are kept below.
---

# Hand-off — T30 node 5

**Done (shas and proof in `README.md` § Node 5), each red then green:**
- (A) the Owner's shape: red `2eba9709`, green `dc408cac` (`APrepareShowsItsSuggestedLinesTests`; the
  DC-NEW-wl-t28-compiled recurrence is in the same commit).
- (B) `tools/compile-eval/first-calls.py`: red `8b57a205`, green `ed24b2e8` (`--self-test`).
- (C) the four sheet test files inject the gate: red `f2630c47`, green `a39dcefa` (`NoSheetTestReadsTheRealProofDirectoryTests`).
- The demo script's step 11 now reads what the user sees after a prepare; finding 1 is marked fixed; C4 is checked.

**Not done / owed:** the first-50 reader has not been run over a real corpus (no `envelope-events.jsonl` in this tree; the
command is in the C4 line). The Tab count from the editor to the first suggested line is **rehearse** in the demo.

**Next step:** T30's second join (the Leader). The ratchet result is below.

**Rename after the (A) green:** `0528992b` renames `SuggestedLinesAnnouncement` to `ReviewLinesAnnouncement`, because
Core's `NoAssistPathExistsAnywhereInTheComposer` refuses `Suggest` in composer sources. No behaviour changed.

**Whole suites (rule 18), board checked before each App run:**
- **Core at `0528992b`:** 3119 passed, 0 failed, 1 skipped (`suite-core-node5-rerun.txt`). At `a39dcefa` it had 1
  failure, the rename's cause (`suite-core-node5.txt`).
- **App at `a39dcefa`:** 1245 passed, 3 failed, 2 skipped (`suite-app-node5.txt`). **At `0528992b`**, (A) and its
  neighbours, the guard and the three: 31 passed, 3 failed (`suite-app-node5-rerun.txt`). The three are node 4's: the
  foreground pair (`CtrlDigitSwitchesThePerspectiveFromInsideAPageTests`, `SolutionTreeChordTests`: owed desk runs) and
  `AReopenedNotebookRendersItsRecordedAttemptsTests.ARestoredRowsEvents_…` (fails on main's code too, per node 4).

**Ratchet (rule 22):** see `ratchet.txt`.

# Hand-off — T30 node 4

**Done (shas in `README.md` § Node 4):**
- (0) *Human demo script — the prefill* in `README.md`, with a gesture table citing file:line for every gesture and
  string (`012edb30`). Two findings for the Owner: the suggested lines arrive inside a folded section that no prepare
  opens, and the *not recorded* restored row cannot be reached through the UI on this build.
- (A) the cost line: red `e04fcb02`, green `51d70fa7`, defect class `77cf5cd4`
  (`EveryAdvisoryCallsSpendReachesTheOutcomeLineTests`).
- (B) compile-eval: `score.py` and `ring.py` self-tests pass. The real-corpus run is NOT RUN, with the reason and the
  command in `B-compile-eval-run.txt` (`bc08478c`). The AI-systems conditions are a checked list in `README.md`.

**Not done — the next node's first item (granted, not started; the rule-16 kick came first):**
`tools/compile-eval/first-calls.py`, seam `req-01M5B9DJWPJBEBCTP2GFJCZVRR`, granted with these conditions: stdlib only;
reuse `score.py`'s `read_rows` and `fold`; leave `score.py` and `ring.py` unchanged; absent values read *not recorded*,
never 0; `--self-test` red then green, with both outputs saved; not named `verify-*`; no network and no model. The
design, as granted:
1. The treatment arm exactly as `score.py:176-177` defines it.
2. The FIRST 50 `called` rows, excluding `reused`.
3. Report n, n with cost, n with latency, tokens in+cache and out (sum, p50, p95), latency p50/p95, outcomes by count,
   and the envelope ids.
4. The self-test fixture: 51 advisory calls, 3 reused, 2 with null cost and one mechanical-only control. The test
   asserts it counts exactly the first 50 non-reused calls.

The Leader asked that its command go into the demo script's **C4 line**: the C4 item of the checked list in `README.md`.

**Residuals:** a compile call whose wire reports no cost adds nothing to the turn's spend, and the line does not say
*not recorded* for it. A turn whose send the host refuses after the gate's submission loses that submission's compile
spend from every line.

**Ratchet (rule 22):** not run. The next step is node 5, not a join.

**Whole suites (rule 18), board checked before each:**
- **Core:** 3119 passed, 0 failed, 1 skipped (`suite-core-node4.txt`).
- **App:** 1243 passed, 3 failed, 2 skipped (`suite-app-node4.txt`). The re-run of the three failed the same way
  (`suite-app-node4-rerun.txt`):
  - Node 1's foreground pair: `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests` and `SolutionTreeChordTests`,
    both *foreground not held*.
  - `AReopenedNotebookRendersItsRecordedAttemptsTests.ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`:
    *Space on the restored row's fold did not open it*. It also fails with `ComposerSendGate.cs` set back to main's
    `d986e4a4` copy, which is this branch's only src change (`suite-app-node4-baseline.txt`; the file was restored
    after). So it is not caused by node 4. The join's recount passed it at `d986e4a4`. Inferred: desktop or focus
    state on this machine. The Leader's re-run decides.

# Hand-off — T30 node 3

**Done:** (A), (B), (C), each red then green. See `README.md` § Node 3 for the shas and proof files. Ruling 182 (3)'s
residual line is at the top of `README.md`. Whole suites (`suite-node3.txt`): **Core 3119 passed, 0 failed, 1 skipped;
App 1242 passed, 3 failed, 2 skipped.** On re-run, `SessionDocumentPlacementTests` passed. Node 1's foreground pair
(`CtrlDigitSwitchesThePerspectiveFromInsideAPageTests`, `SolutionTreeChordTests`) failed again: *foreground not held*.
`docs-graph.py validate`: 0 problems, index drift only.

## Findings

1. **Ruling 182 (4)'s premise was false until this node.** The restored row's disclosure was empty
   (`B-sent-survives-red.txt`). The fix rebuilds the bytes and shows them only on a sha match with
   `submitted.text_sha256`. A turn whose bytes carried **attachment bodies or a template** does not rebuild, so its
   restored row reads *not recorded*. That is honest, but it is not full survival. Full survival for those turns needs
   the bytes or the bodies stored, which is a store change. The decision belongs to the Owner or WL-2.
2. **`KeepHelp` says "a kept line stays in your draft."** (A)'s scan does not refuse it, and it is true across a prepare.
   A reader could take it to mean the line survives a restart. Not changed. It is the Owner's call whether that wording
   implies "saved".
3. **The WL-1a Proof Pack's residual paragraph** (`docs/proof/workbench-loops-wl-1a.md:202`, the Leader's) says the sent
   prompt survives. That is true from `9234fc77` on, with finding 1's limit.
4. **Rule-6 lapse (this node):** one empty heredoc was run by mistake while editing `NotebookRuns.cs`. It executed nothing
   and changed no file. Every edit was made with the Edit tool.
5. **Branch:** origin/main was merged into `wl-prefill` at `565b2883`, because `coord claim` refused on stale controls
   (DC-226).

## Next step (node 4, from the brief)

1. Ruling 180 (4), the cost line and the first 50 calls.
2. Ruling 180 (6), compile-eval.
3. Inject the gate in the four sheet tests that read the real `~/.aide/proof` (the Leader grants the seam).
4. Rule 22's ratchet, ahead of T30's join.

Ratchet (rule 22) not run: the next step is node 4, not a join.

## Node 3b (pre-join): the Owner copy change and rule 22 ratchet

1. `ComposerSurface.KeepHelp` is now exactly the Owner copy ("... a kept line stays in your draft until you send it. It is not yet kept when AI-DE restarts."). `DESIGN.md` row updated in the same commit (5c61cc29). Tests: NothingSaysAnUnsentDraftIsSavedTests, AKeptSuggestedLineIsKeptTests, ASentGoalLineSurvivesARestartTests = 7/7 green (`keep-copy-green.txt`). Board checked first: no demo in progress, no join in flight.
2. Ratchet (origin/main 669cf746 vs head c3564d6d): one unexpected refusal, `verify-harness-diagnostics` (TheCompileModeControlTests declared an STA thread with no failure capture). Fixed in c3564d6d: the thread captures its failure and the creator rethrows it as itself. Gate OK; TheCompileModeControlTests 5/5. Rerun: 3 refusals, all the expected derived-view gates (docs-graph index drift, verify-derived-views, verify-site-figures). Lines in `ratchet.txt`; the `ratchet/` folder is not committed.
