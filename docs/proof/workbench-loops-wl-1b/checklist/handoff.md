---
id: proof-workbench-loops-wl-1b-checklist-handoff
title: "Hand-off — WL-1b T26 checklist kind after node 1"
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
  T26 node 1 stopped at the Leader's kick: D1 and D2 green, D3 not started, three literal-table tests to update in
  node 2 under seam req-01M5BAKZ9BAA28CF4R07PTRKGV.
---

# T26 hand-off after node 1

**Done (green):** D1 (`81195e13`) and D2 (`c3ff46aa`), and the DC-138 fix (`f399ec87`). See `README.md`.

**Red, to fix in node 2 (granted by seam `req-01M5BAKZ9BAA28CF4R07PTRKGV`, three literal entries only, never derived
from `Kinds`):**

1. `tests/AiDe.App.Tests/Workbench/PerspectiveMenuTests.cs` near `:225`: add
   `("checklist", ["factory", "understand"], SurfaceContentFactory.Instances.Many)`; the count at `:238` becomes 22.
2. Same file, `:315` (Factory View) and `:345` (Understand View): insert `"New checklist"` right after `"New code viewer"`.
3. `tests/AiDe.App.Tests/MainMenuTests.cs:167`: insert `"New checklist"` after `"New code viewer"`.

**Not started: D3** (saved review state is not acceptance), with the Leader's added red from the seam condition:

- Record `deferred` and `needs-decision`, save the layout, restart (`LayoutPersistence` restore +
  `ReviveRestoredSessionDocuments`, as `ARevivedNotebookReopensItsLiveAttemptsExecutionTests` does). The checklist
  restores with the same states, `work` has no `accepted` row, and the gap line is unchanged.
- **Leader's red:** restore with a different notebook focused. The checklist must still show its own journey. Before
  its notebook reopens it shows `ChecklistCopy.NotOpen(journeyId)`. The mutation to see red: bind the factory's
  resolver to the focused notebook instead of `checklist:<sessionId>`'s own.
- The shell already invalidates `checklist:<sessionId>` in every host when that notebook registers
  (`WorkbenchShell.RegisterSessionDocument`), so a restored checklist rebuilds bound. Node 1 did not test this path.

**Next step:** node 2. Make the three literal entries, then D3 red then green, then the whole suites. Then run the
rule-22 ratchet before the join.

**Harness:** `tests/AiDe.App.Tests/Workbench/Checklist/ChecklistHarness.cs`. The stub turn ends `Answered` live and
`Completed` in the store. `ConfigureStub(document, taskClass)` exists because the task class is the projection input
that changes the compile while the draft stays the same.
