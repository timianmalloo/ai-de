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
  Red and green evidence for T26: the checklist kind opens from View in Factory and Understand (D1), links a run,
  refuses and reads out acceptance while the intent is unaccepted, and accepts the deliverable naming the intent
  revision only (D2, with R1 and the Owner's null-sha condition). Node 2: the three literal entries, the service
  refusing a journey that is not the notebook's, saved review state surviving a restart without acceptance (D3, with
  the own-journey restore), and the one-live-sha ceiling pinned (E).
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
  envelope. Node 2 restated the ceiling and pinned it (E, below).

# T26 checklist kind — node 2

| Check | Red | Green |
|---|---|---|
| (0) three literal entries (seam `req-01M5BAKZ9BAA28CF4R07PTRKGV`) | `suite-node1.txt` — the three node-1 reds | `literals-green.txt` — 3 of 3 (`b306c7f9`) |
| Owner's refusing test | `refuse-red-mutation.txt` — **mutation** M4 in `ChecklistService.RequireWorkJourney` (`160bb519`) | `refuse-green.txt` — 1 of 1 (`2568c964`) |
| D3 saved review state is not acceptance | `d3-red-restorable-mutation.txt` — **mutation** M7: `checklist` left out of `RestorableKinds` (`559eb22d`) | `d3-green.txt` — every checklist test, 10 of 10 (`32831438`) |
| D3 own journey (Leader's condition, seam `req-01M5B8W7NJRW7A1X4G21J2QKPT`) | `d3-red-own-journey-mutation.txt` — **mutation** M5: `SessionDocumentFor` falls back to the focused notebook | same |
| (E) one-live-sha ceiling | `e-red-oldest-sha-mutation.txt` — **mutation** M8: the oldest head-revision run's sha (`ad6d2c8b`) | `e-green.txt` — 1 of 1 (`d5838812`) |

Every red is a mutation red; each mutation was reverted before its commit, and no node-2 commit changes product
behaviour. The node-2 tests:

- `TheServiceRefusesAJourneyThatIsNotTheNotebooksTests.EveryChecklistCall_WithAnotherNotebooksJourney_IsRefused_AndNothingIsWritten`.
  Over the real client and daemon, all five checklist operations (evaluate, set, both disposition kinds, link, accept)
  are called with notebook A and notebook B's journey. Each is refused with `AIDE-WORK-NOT-A-WORK-JOURNEY`, and neither
  checklist moves. M4 removes the `owner_journey_id` clause, so the service passes the wrong journey. It touched
  `src/AiDe.Core` (read only for T26) only as an uncommitted, reverted mutation.
- `SavedReviewStateIsNotAcceptanceTests.DeferredAndNeedsDecision_SurviveARestart_WithNoAcceptedRow_AndTheSameGapLine`.
  `deferred` and `needs-decision` are recorded through the row buttons, and the Factory slot is saved by the shell's
  own `LayoutPersistence.SaveNow`. A new `WorkbenchShell` over the same data directory then restores it in its
  constructor. Before the revive, the restored checklist names its own journey (`NotOpen`).
  `ReviveRestoredSessionDocuments` then reopens the notebook. The rows and the gap line equal the saved ones, and the
  store holds no `work` disposition row.
- `SavedReviewStateIsNotAcceptanceTests.RestoredWithAnotherNotebookFocused_TheChecklistNamesItsOwnJourney_AndBindsNothingElse`.
  The checklist's own notebook is closed, and another notebook is open and focused at the restart. The pane reads
  `NotOpen(<its journey>)`, with no rows and no closing actions. It reopens, focus goes back to the other notebook,
  and the checklist still shows its own journey's deferred row. The other journey's checklist stays empty.
- `TheOneLiveShaCeilingTests.TwoLinkedRunsAtTheHeadRevision_ReadSatisfiedByTheNewest_AndAnOlderRunAloneReadsStale`.
  It covers two completed runs at revision 1 with different projections (task class `implement`, then `investigate`).

**Not a red, recorded (M6, `d3-not-red-invalidate-mutation.txt`):** D3 stays green with the checklist `Invalidate` in
`RegisterSessionDocument` removed. The restored pane rebinds on its visibility edge when the host re-renders, so that
invalidate is not D3's control. Not cited as one. `d3-green-first-run.txt` is the first run of the D3 tests, before the
pre-revive `NotOpen` step was added. It was green with no mutation, which is why D3's reds are mutation reds.

**The one-live-sha ceiling, as pinned (E).** The surface sends one live projection sha per evaluation: the newest
head-revision run's own envelope. It holds while every head-revision run shares one projection. Past that ceiling,
take two runs of one revision with different projections. A link to the OLDER run alone reads **Stale**, although
that run ran from the current draft. An obligation that also links the newest run reads Satisfied by it, which is
correct. The test asserts both readings, so it fails if the ceiling moves either way. The Core request was not
widened (brief). Upgrade trigger: a user links an older head-revision run after a re-prepare that changed the
projection. The request then takes a sha per dispatch key, which is a Core seam.

## Residuals after node 2

- **The checklist copy (DESIGN.md CHK1–CHK12) still awaits the Owner's confirmation** (design `:592`, the UX &
  Accessibility condition). No ruling or mail recording it was found.
- Rule 5: the node-2 test files under `tests/AiDe.App.Tests/Workbench/Checklist/` (an owned new path) were edited
  without a per-edit claim. The source, test-table, README and hand-off edits were claimed.

# T26 checklist kind — node 3

## (F) The Owner's copy confirmation (Owner, 2026-10-20 ~01:10Z; carried by the T26 n3 brief, 0481ee2d)

The Owner read DESIGN.md:1410-1421 at b82eba50. The Owner's decision:

- **Confirmed as written:** CHK1, CHK2, CHK4, CHK5, CHK6, CHK7, CHK8, CHK10, CHK11 and CHK12.
- **Changed, exactly:**
  - CHK3 → `Satisfied by run {7-char key} (intent revision {n}).` One term, "intent revision", everywhere.
  - CHK9 → `This notebook has no work journey. It was made before work journeys existed. Obligations need a work journey.`
    "(pre-workbench)" was internal jargon the user has never seen.

This closes the residual "the checklist copy still awaits the Owner's confirmation" from nodes 1 and 2.

**Red first.** No test pinned either sentence before this node: every test read the `ChecklistCopy` member the surface
renders, so a copy change could never go red. `TheChecklistSaysTheOwnersCopyTests` asserts each sentence as a literal,
as the user sees it on the surface:

- `CHK3_ALinkedRun_ReadsSatisfiedByRun_AtItsIntentRevision` drives the link gesture (View → New checklist, Add
  obligation, Link the latest run) and reads the row's UIA name.
- `CHK9_ANotebookMadeBeforeWorkJourneys_SaysItHasNoWorkJourney` opens the checklist over a notebook the v1 → v2
  migration backfilled as legacy (no work journey). That is the only way such a notebook exists (Ruling 148 c), so
  `ChecklistHarness.LegacyStore` writes a v1 file from the store's own frozen baseline and opens it.

| Step | Commit | Proof |
| --- | --- | --- |
| Red on the old copy: CHK3 reads "(revision 1)", CHK9 reads "(pre-workbench)" | `2ce683f0` | `copy-red.txt` |
| Green: `ChecklistCopy.cs`, DESIGN.md CHK3 and CHK9, design `:426`, in one commit | see `handoff.md` | `copy-green.txt` (12 of 12 checklist App tests) |

The design doc's CHK3 line (`docs/design/workbench-loops-wl-1b.md:418`) still reads "(revision {n})". It is outside
this node's grant (only `:426` was granted). Seam request `req-01M5BDHVX8T545SQC0V4BESVF9` asks for it.
