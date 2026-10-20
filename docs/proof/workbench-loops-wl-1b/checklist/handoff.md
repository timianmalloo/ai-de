---
id: proof-workbench-loops-wl-1b-checklist-handoff
title: "Hand-off — WL-1b T26 checklist kind after node 2"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [workbench-loops, handoff, checklist]
links:
  - { to: architecture, rel: implements }
  - { to: proof-workbench-loops-wl-1b-checklist, rel: relates-to }
review-by: "2026-11-18"
summary: >-
  T26 node 2 is done at the kick: the three literal entries, the Owner's refusing test, D3 with the own-journey
  restore, and the one-live-sha ceiling are green. The next step is the WL-1b checklist join.
---

# T26 hand-off after node 2

**Done (green), each red first by a recorded mutation (see `README.md`, "node 2"):**

- (0) The three literal entries, `b306c7f9`. The node-1 reds are in `suite-node1.txt`.
- The Owner's refusing test: red `160bb519` (M4), green `2568c964`.
- D3 and the Leader's own-journey restore: red `559eb22d` (M7, M5; M6 recorded as not a red), green `32831438`.
- (E) The one-live-sha ceiling: red `ad6d2c8b` (M8), green `d5838812`. The ceiling is stated in the README and in the
  `simplify:` comment.

No node-2 commit changes product behaviour. The only `src` change is the `simplify:` comment in `ChecklistSurface.cs`.

**Suites (`suite-node2.txt`, on HEAD `d5838812`):**

- Core: 3119 passed, 1 skipped, 0 failed.
- App: 1252 passed, 2 skipped, 3 failed of 1257. None is a checklist test. They are not re-run in this node because
  the kick came first.
  - Owed desk runs (no foreground for this unattended run):
    `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests` (`foreground held=False`) and `SolutionTreeChordTests`
    (`foreground not held`).
  - `AReopenedNotebookRendersItsRecordedAttemptsTests.ARestoredRowsEvents_AreReachedFromTheRowByKeyboard_AndExposedToUiAutomation`
    fails with "Space on the restored row's fold did not open it". It is keyboard-driven. Node 1 listed it as a
    desktop-hold failure. T30 n4 saw the same failure on its own tree with its change reverted. Inferred: not T26's,
    because no T26 change touches that path. It needs a desk re-run to confirm.

**Not done / owed:**

- The Owner's confirmation of the checklist copy (DESIGN.md CHK1–CHK12, design `:592`).
- The desk runs named above.

**Next step:** the WL-1b checklist join (the ratchet is in `ratchet.txt`).
