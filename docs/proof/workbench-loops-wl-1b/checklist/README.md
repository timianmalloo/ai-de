---
id: proof-workbench-loops-wl-1b-checklist
title: "Evidence — WL-1b T26: the minimal checklist kind"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-1b"
tags: [workbench-loops, evidence, checklist]
links:
  - { to: architecture, rel: implements }
review-by: "2026-11-18"
summary: >-
  Red and green evidence for T26 node 1: the checklist kind opens from View in Factory and Understand (D1), links a run,
  refuses and reads out acceptance while the intent is unaccepted, and accepts the deliverable naming the intent
  revision only (D2, with R1 and the Owner's null-sha condition). D3 is node 2.
---

# T26 checklist kind — node 1

The kind is `checklist`, identity `checklist:<sessionId>` (one per journey, Ruling 176; seam
`req-01M5B8W7NJRW7A1X4G21J2QKPT`), Right zone, admitted to Factory and Understand. The focused notebook picks the
journey at open only; the surface rebinds to its own notebook. Source: `src/AiDe.App/Workbench/Checklist/`.

Every test drives the product's shell (three hosts under one `PerspectiveShell` router) over a real `WorkspaceClient`
to a daemon endpoint with the notebook, attempt and checklist operations over a real schema-v2 store. Gestures are
UI Automation: the View menu item, the buttons, the obligation text box.

| Check | Red | Green |
|---|---|---|
| D1 demo path | `d1-red.txt` — View offers no "New checklist" (3 of 3 fail) | `d1-green.txt` — 3 D1 tests + `KindAllowListsTests` + the D6 reader test, 11 of 11 |
| D2 accept path | `d2-red-accept-mutation.txt` — **mutation** M1: *Accept deliverable* disabled while blocked | `d2-green.txt` — 3 of 3 |
| D2 R1 (Ruling 180 (5)) | `d2-red-r1-mutation.txt` — **mutation** M2: the composer's `LastProjection` as the live sha | same |
| D2 null-sha (Owner) | `d2-red-nullsha-mutation.txt` — **mutation** M3 at the caller: `live ?? the ran sha` | same |

The D2 reds are mutation reds. The surface was written with D1, so each D2 test was run against its own mutation of
`ChecklistSurface.cs`; every mutation was reverted before its commit. The R1 re-prepare changes the task class with
the draft unchanged: the class is in the projection's domain (`Projection.cs:175`), so the live compile differs while
revision 1 stays the head (asserted). The null-sha case is a reopen whose session directory (the compile history) is
gone, so the notebook has no envelope store and the call carries no live sha.

## Suites at the node-1 kick

- `suite-node1-core.txt` — Core 3119 passed, 1 skipped, 0 failed.
- `suite-node1-app-run1.txt` — App run 1: 7 failed. One was the DC-138 gate (a `Loaded` hook in the new surface);
  fixed in `f399ec87`, green in `suite-node1-rerun.txt`.
- `suite-node1.txt` — App run 2: **1245 passed, 2 skipped, 6 failed of 1253**.
  - 3 are literal-table tests the new row reddens, outside node 1's grant: `PerspectiveMenuTests.TheAllowListsEqualTheSpecsTable`,
    `PerspectiveMenuTests.TheRenderedMenuEqualsTheSpecsLiteralTable_ForEveryPerspective`,
    `MainMenuTests.FactorysViewMenu_ListsItsOwnEntries_ThenTheFiveShowEntries_AndTheDisputeVerb`. Seam
    `req-01M5BAKZ9BAA28CF4R07PTRKGV` grants them to node 2.
  - 3 are desktop-hold failures (no foreground for this unattended run): `SolutionTreeChordTests`,
    `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests`, `AReopenedNotebookRendersItsRecordedAttemptsTests`. They
    failed the same way when re-run after 2 min (`suite-node1-rerun.txt`). None touches the checklist.

## Residuals

- The Owner's confirmation of the checklist copy (design `:592`) is not recorded anywhere this node could find.
  The DESIGN.md rows CHK1–CHK12 follow the design's copy.
- One live sha per evaluation (a `simplify:` in `ChecklistSurface.EvaluateAsync`): the newest head-revision run's own
  envelope. The ceiling is one linked run per obligation at the head revision.
