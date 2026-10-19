---
id: proof-workbench-loops-wl-1a-compiled-field-handoff
title: "Hand-off — WL-1a T28: the compiled field after a submit"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff, composer, ruling-96, ruling-181]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T28 after node 3: a queued send shows its turn headed 'Compiled prompt — queued as b{n}', display-only, until the next
  keystroke (red then green); a second press right after it queues and sends nothing, and after the drain sends nothing
  (planted-leak mutation red M3); the class gate ComposerContentOraclesDriveTheGestureTests refuses a composer content
  oracle that skips the gesture (red on a plant, green on the tree). Next is T30 (Ruling 180, the prefill).
---

# Hand-off — T28 `wl1a-compiled-field`, after node 3

**Done (node 3, `/implement`, Ruling 181 under the Owner's direction).** Commits, red then green:
- **(A)** red `13b32da6`, green `1874cb5e`. The tests are `TheCompiledFieldAfterAQueuedSendTests` (3 facts). The heading const is `ComposerSurface.QueuedHeadingFormat` = `"Compiled prompt — queued as {0}"`. `BeginNextTurn` heads a queued submit with it. The DESIGN.md row *Compiled prompt — after a queued send* follows the sent-state row.
- **(B)** mutation red `65bde9a9`, green `db516197`. The test is `TheSentPromptIsNeverResentTests.ASecondCtrlEnterRightAfterAQueuedSend_WithNoKeystroke_QueuesNothingAndSendsNothing`, and it replaces node 2's `AQueuedSend_ShowsItsQueuedState_NotTheSentState`. Under M3 (the queued words kept in the draft), the press after the drain resent b2 as b3.
- **(C)** red `d4995b84` (the plant), green `23c7c461` (the plant removed). **The gate is the test `tests/AiDe.App.Tests/Workbench/Composer/ComposerContentOraclesDriveTheGestureTests.cs`**, with 2 facts: the tree, and a self-test. It is a test rather than a `tools/` gate, because a new `tools/verify-*.py` needs a `.github/workflows` line, which is outside this track's paths. It has 3 `PreAdmit` entries, and the list may only shrink. `DC-NEW-wl-t28-compiled`'s Prevent line names the gate. Its status is `partially-controlled`, because the non-empty check is not mechanized.
- The gesture helper gained `ComposerGesture.CtrlEnterThenWait` (the queued gesture) and `ComposerGesture.StatusLink`.
- The whole suites are in `suite-node3.txt` (Core, then App). Core: 3046 passed, 0 failed, 1 skipped. App: 1177 passed, 2 failed, 2 skipped. Re-run alone, `NoDispatchOutsideTheHostTests.ADeadHolder_…` passes (a full-suite timing failure). `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_…` is refused again with "foreground not held" (the shown-window probe, Ruling 155 (3), NOT CHECKED). No desktop-hold result occurred.

**Red, by design:** none.

**Open:**
- **Inferred:** after the queue drains, the field keeps *queued as b2* until a keystroke. Nothing re-renders it on the drain, and no ruling names a queued-to-sent transition. One `ShowCompiled` call would add it if the Owner wants it.
- **Rule 19 lapse (recorded):** four filtered App runs of the gate test (source scans, no window) started during the T11 join (from 17:32:18Z). The whole App suite waited for `join done`.
- Seam `req-01M5AE5QGZMKFC36RA4C93QSPJ` (node 1) was answered by Ruling 181. The Leader can close it. No new seam request.

**Next step:** T30 (Ruling 180, the prefill) on `ComposerSurface.cs`. The class gate will hold its tests to the gesture.
