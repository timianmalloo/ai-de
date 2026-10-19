---
id: proof-workbench-loops-wl-1a-readback-handoff
title: "Hand-off — WL-1a T17d: the read-back runs when the host binds"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T17d after node 4: all Ruling 172 and Ruling 175 checks are red then green, including the Owner's two corrections ("sent"
  not announced; the read-back failure's own copy) and the host-open and read-back durations (measured, shell-path test:
  medians 16.5 ms and 23.25 ms). Suites: Core 3024/0; App 1143/2, one environmental, one teardown race not reproduced.
---

# T17d hand-off (node 4)

## State

- **Nodes 1 and 2:** Ruling 172's checks 1-3 are red then green. See `README.md`.
- **Node 3:** Ruling 175's checks A and B are red then green.
- **Node 4.** All three checks are red then green, each committed separately:
  1. **"sent" is not announced** for a Send that waits on the restore. **Green:** red 074c1c90, green ce94643d.
  2. **The read-back failure's copy** is "Restoring this notebook's turns failed: {cause}. Retry to restore them." A failed host open keeps Ruling 159's words. **Green:** red 8825c4a8, green 0bf5f955. The DESIGN.md row is :1220.
  3. **The durations** (`notebook.host-open`, `notebook.read-back`) are emitted by default. **Green:** red 03a1dfd3, green fa310512. Measured in the shell-path test, n=10: host-open min/median/max 15.7/16.5/18.5 ms; read-back 22.2/23.25/26.6 ms (`timings-shell-path.txt`).
- **Recorded:** the restoring sentence's DESIGN.md row is after :1218 (:1219), not at :1204. The Owner accepts this.
- **Suites** (`suite-green.txt`), at fa310512: Core 3024/0. App 1143 passed, 2 failed. One failure is `SolutionTreeChordTests` (environmental, fails alone). The other is a teardown race in `TheRowsAttemptBindingSurvivesRestartAndSequenceTests.ASentTurnsRow_ShowsItsBinding_WhileTheRunIsStillHeld_ThroughTheSurface`, which is not reproduced (`teardown-race-rerun-n4.txt`).

## Next step

1. The Leader decides whether the teardown race blocks the join. If it recurs, investigate `NotebookFixture.Dispose` against a run still held.
2. Join `wl1a-readback`. Demo step 18 is owed to the user after the join, and its workbench log will hold the running-app durations.
