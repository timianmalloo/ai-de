---
id: proof-wl-s-kinds-node1-handoff
title: "WL-S T11 kinds - node 1 hand-off (rule 16 kick)"
type: proof-pack
status: draft
owner: "@timianmalloo"
links:
  - implements: adr-0045-perspective-set-factory-review-understand
  - implements: design-workbench-loops-wl-s
review-by: 2026-11-19
summary: "Node 1 stopped at the Leader's kick. Check A (notebook half + Ruling 152 D2 flip) is green; 19 old Left-dock tests need re-scoping; the execution kind, check B and check C are not started."
---

# T11 node 1 — hand-off

The Leader kicked this node under rule 16, about 12 minutes in. This file records what is done, what is red and the next step.

## Done (check A, notebook half)

- **The `notebook` kind binds T8's model.** The code on main already binds `NotebookModel` to `SessionDocumentSurface`
  (`NotebookModel.cs:53`). So the notebook kind is that surface under a new name: `SessionDocumentSurface.Kind` changed
  from `session-document` to `notebook`. The factory row has the title "Notebook", Factory only, `Instances.Many`,
  `Verb("session.new")` and `Zone: Center`.
- **Ruling 152 D2.** Factory's `Landing` changed from Left to Center in the same change, and the `simplify:` marker is
  gone because its trigger fired. The Core build test now pins the value:
  `PerspectiveSetTests.EachRowHasItsOwnLandingZone_FactoryOnItsNotebookInTheCenter_ReviewAndUnderstandCenter`.
- **`session-document` is in `RetiredKinds`.** A saved placement that names it is dropped with a report, never in silence.
  This holds until node 2's migration (D) maps it to `notebook`.
- **Catalog scopes:** `session.cycleRegion`, `session.cycleRegionBack` and `session.console` now read `Admits("notebook")`.
- **Tests (red, then green):** `TheNotebookKindBindsTheModelTests.TheNotebookRow_IsFactorysOnly_DocksInTheCenter_AndFactoryLandsThere`
  and `TheNotebookKindBindsTheModelTests.ANewSession_OpensAsANotebookInTheCenter_BoundToTheShellsModel`. The red output is
  in `A-notebook-kind-red.txt`, and the green output, with the whole Core suite (3019 passed), is in `A-notebook-kind-green.txt`.
- **Seam request** `req-01M59VJZ0N12XCM461BTB230N0`: the edits to `SessionDocumentSurface.cs:215` and
  `WorkbenchCommands.cs` are outside the owned paths. Each file was claimed before its edit, and nothing near `WorkbenchShell.cs:1398` was touched.

## Red (to re-scope, not yet done)

The whole App suite had 20 failures out of 1127 (`A-rescope-red-list.txt`). Nineteen of them pin Ruling 83's Left dock,
which ADR-0045 §1(a) supersedes ("Factory: Center = notebook"). Those tests:
`SessionDocumentPlacementTests` (4), `ASessionDocumentIsShownWhereTheOperatorIsTests` (2, including the SH-4.1 oracle),
`PerspectiveShellTests` (2), `WorkbenchDragCompletedHookTests` (2), `WorkbenchShellTests.ShowLedger_InFactory_...`,
`CenterEmptyCopyTests.WhileASessionIsOpenAtLeft_...`, `CodingsLeftExtentTests` (4), `ConsoleSplitPlacementTests` (2) and
`AParallelSessionIsADerivedSiblingTests` (1). The 20th, `SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`,
**passed when run on its own** (green file), so it is not tied to this change (Ruling 155 calls it solo-flaky).

Several of these tests build `new Surface(.., "session-document", ..)` by its literal name. Others assert the Left zone
or the Left landing. Each one needs a decision: re-pin it to the Center notebook, or retire it with the ruling it pinned.
Some may need a ruling from the Leader, for example on the "docked at the left" copy and on the 96ch Left extent.

## Not started

- **The `execution` kind** (Zone Bottom, one per attempt, bound to `NotebookRuns.AttemptOf`). It has no door: the catalog
  has no verb that opens it, and the seam request asks for a `session.execution` row and its handler.
- **Check B**, Ctrl+1/2/3 with focus inside a WebView2 page. A real key path is needed, so it must run out of process.
  The canvas probe's pattern is `tests/AiDe.App.CanvasProbe`, with SendInput to the foreground window or CDP
  `Input.dispatchKeyEvent`. Whether a CDP key reaches `AcceleratorKeyPressed` is **not measured**.
- **Check C**, hide, float and re-dock a live notebook. The pattern is `TheModelOutlivesItsSurfaceTests`, plus
  `SetStackState(.., StackState.Floating)`.
- The caption "Notebook — <journey>" (ADR-0045 §5) is not applied. The title is still the session's name.

## Next step

1. Re-scope the 19 tests listed above, so that the App suite is green on the notebook-in-Center change.
2. Add the `execution` kind once the Leader rules on its verb.
3. Then do checks B and C.
