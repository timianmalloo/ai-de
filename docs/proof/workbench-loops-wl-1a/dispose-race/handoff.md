---
id: proof-workbench-loops-wl-1a-dispose-race-handoff
title: "Hand-off — WL-1a T17e: a read-back pending at disposal ends quietly"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1a"
tags: [workbench-loops, evidence, handoff]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  T17e: the read-back-at-disposal race was reproduced with a gate on the unit path and through WorkbenchShell.Dispose, then
  fixed in NotebookRuns (red 31b61650, green d7e0ac1b). The WL-1a residual line is effbadc1, and step 18 is unchanged.
  T17d's teardown NRE is a different race (a run's write) and stays open.
---

# T17e hand-off

## State

- **Check 1 (unit), red then green.** Red 31b61650: `ObjectDisposedException` escaped, and 2 turns were written after
  disposal. Green d7e0ac1b (2/2).
- **Check 2 (`WorkbenchShell.Dispose`, the app's shutdown at `MainWindow.xaml.cs:96`), red then green.** Red 31b61650: 2
  turns were written after the shell shut down. Green d7e0ac1b (1/1). Closing a pane is not a dispose path
  (`WorkbenchShell.cs:3406`, :3414 are the only disposals), so it has nothing to test. See `README.md`.
- **Fix:** `src/AiDe.App/Workbench/Records/NotebookRuns.cs`. The read-back runs under `_disposing.Token` and checks it
  after each read and before each thread write. Once disposed, it ends as `notebook.read-back` outcome `ended` with no
  refusal. No change was made in `SessionDocumentSurface.cs` or in Core. No seam request was needed.
- **Docs:** the `workbench-loops-wl-1a.md` residual line is effbadc1. Step 18 is unchanged, because the restoring status
  needs a Send (`SessionDocumentSurface.cs:603`).
- **Suites** (`suite-green.txt`, at d7e0ac1b): Core 3024/0. App 1147 passed and 1 failed, the environmental
  `SolutionTreeChordTests` (it fails alone too, `chord-alone.txt`).

## Next step

1. Join `wl1a-dispose-race`.
2. T17d's teardown NRE stays open. It is inferred to be a released stub run's terminal write
   (`AppendAttemptOutcome`, a write transaction) racing `NotebookFixture.Dispose` → `WorkspaceStore.Dispose` (:380). To
   confirm it, gate that write in a test and dispose the fixture while it is held. The likely fix is in the test or the
   fixture: wait for the run to end before disposing. Neither is owned by T17e.
