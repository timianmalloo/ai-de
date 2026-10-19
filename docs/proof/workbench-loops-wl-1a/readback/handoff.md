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
  T17d after node 3: Ruling 175 checks A (pending Send keeps words, restoring status, one turn as b3) and B (failure keeps
  words with its own cause) are red then green; check C (host-open and read-back durations) is not started. Suites: Core
  3024/0, App 1139/1, the one failure environmental (SolutionTreeChordTests). Node 4 does check C before the join.
---

# T17d hand-off (node 3)

## State

- **Nodes 1 and 2:** Ruling 172's checks 1-3 are red then green. See `README.md`.
- **Check A** (Ruling 175 (1), (3), (5)): red 7d07d8ed, then green f796d727. A Send while the read-back is pending keeps its words, and Send is disabled with `SessionDocumentSurface.RestoringTurns` as the status and the UIA help text. A second press admits nothing. The turn is admitted as b3, and only then does the editor clear. The DESIGN.md copy row is pinned to the const. `NoDispatchOutsideTheHostTests:47` is re-scoped as the ruling directs, so node 2's regression is closed.
- **Check B** (Ruling 175 (2)): red 7954c4ea, then green e16d2fcd. A failed read-back is now the bound host's `HostRefusal`, in Ruling 159's words with its own cause (`NotebookRuns.CannotHost`), and the Retry reads back again. A pending Send whose host refused, or whose read-back failed, keeps its words. The host-refusal half was already green after check A; its mutation red is `checkB-mutation-red.txt`.
- **Check C** (Ruling 175 (4)): **not started**. The node stopped at the Leader's kick.
- **Suites** (`suite-green.txt`), at e16d2fcd: Core 3024 passed, 0 failed. App 1139 passed, 1 failed (`SolutionTreeChordTests`, foreground not held, fails alone too: `chord-alone-n3.txt`).

## Next step (node 4: one check)

1. **Check C, red first.** Emit the host-open duration (bind start to bound) and the read-back duration (read-back start to end) on the normal path, with no flag (IO1), through `WorkbenchDiagnostics.Write`, as `ThreadDiagnostics` does. The two edges are `NotebookRuns.StartOpening` / `OpenHostAsync` and `ReadBackForTheBind` / `KeepingTheFailureAsync`. Red: a test sets `WorkbenchDiagnostics.Sink`, reopens with the open held, and asserts one record of each with `duration_ms` and the notebook id. A failed open or read-back records its outcome, never a duration made to look plausible (IO12). Then record the value seen on this machine in the WL-1a Proof Pack (the README here and the slice pack), from a real app run or from check 1's daemon-backed revive, labelled as the one it is.
2. Re-run the whole suites, then join `wl1a-readback`. Demo step 18 is owed to the user after the join.
3. Owner residuals (in `README.md`, node 3): "sent" shows for one dispatcher turn before the restoring sentence replaces it (a fix in `ComposerSurface`, not granted). The read-back failure's sentence says "open the host" (new copy would need the Owner).
