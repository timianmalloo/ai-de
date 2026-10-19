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
summary: "Node 3: Ruling 168's measure retargeted (startup, wide and the cap green; the minimum row red on a seam), the Console focus return fixed, the copy test re-scoped. The execution kind waits on seam req-01M59XNJH52JRRZ1DAJYK40X9D."
---

# T11 node 3 — hand-off

## Done

- **Ruling 168.** `CodingsLeftExtentTests` became `TheThreadMeasureMovesWithTheNotebookTests` (red commit `d8746b1d`).
  - The theory `TheThreadsMeasure_IsHeldBothWaysInTheCenter_NeverClippedNeverStretched` has three rows: minimum, startup and wide.
  - The density theory became `AtStartupSizeInTheCenterBottomCollapsed_TheThreadHoldsOneTurn_WithTheEditorAt280`. Both of its rows are green.
  - **The cap already exists on the product.** `ThreadFeed.cs:616` and `:858` set `MaxWidth = MeasureWidth`, so `ThreadFeed.cs` is unchanged.
  - The mutation red shows that assertion (2) can fail (`8976dc86`, `R168-cap-mutation-red.txt`). With the words uncapped, the startup row fails ("the words (1258.0) run past the 96ch measure (672.6)"), and so does the wide row (1544.7).
  - **The minimum row is red, and that is a product finding (`R168-measure-red.txt`).** At 820 × 520, the test fails with "minimum: the turn's row (706) cannot hold 96ch (673) plus its 40 px gutter and 12 px padding".
  - The cause is the shell's chrome, not `ThreadFeed`: 71 px of activity bar plus 43 px of pane chrome. The words still wrap within one word of the measure (632 ≥ 613.8), and the cap holds.
  - Seam `req-01M59XDZY6FSA83JGEXEJYCGB2` offers two options. (A, recommended) Change `MainWindow.xaml` MinWidth from 820 to 840. (B) Amend Ruling 168 (3) for the minimum.
  - The wide row is the display's maximum, 1706 × 1066 DIPs. 2560 is not reachable on this display. At that size the column is 1557 and the words are 662.7, so the cap is the thing that holds the line.
- **Console focus return.** The red is the existing test, `ConsoleSplitPlacementTests.WithTheNotebookInTheCenter_TheSplitOpensInTheCenterZone_AsAConsoleDocument` (`8e351e1c`). It read "Expected: ToggleButton Content:Console … Actual: ToggleButton Content:Goal · Done when · Not in scope".
  - The focus-return code is `WorkbenchShell.OnSurfaceClosed`. After the layout pass, the deferred callback now tries the toggle again, and only then falls back to the landing (`e3ead08d`).
  - The whole class is green (6/6). The mechanism is Inferred: `Focus()` fails while the notebook's tab is unrealized. That reading fits the green result, but it was not observed directly.
- **The copy re-scope.** `CenterEmptyCopyTests.WhileASessionIsOpenAtLeft_…` became `WhileANotebookIsMovedToTheLeft_…`. The notebook opens in the Center, then a `MoveSurface` JoinStack drop moves it to the Left. The class is green (10/10, `997f21da`).

## Not done

- **The `execution` kind.** Seam `req-01M59XNJH52JRRZ1DAJYK40X9D` asks for `SessionDocumentSurface.cs:926-928`, where OpenLog shares a case with OpenConsoleAt and calls `OpenSplit` → the Console. It also asks for a `WorkbenchShell.cs` opener like `OpenSessionConsole`.
  - The attempt's identity is `AttemptRow.DispatchKey`, read through `NotebookRuns.AttemptOf(ordinal)` (`NotebookRuns.cs:240`).
  - Rule 14: today "Open log" opens the Console at the turn. Re-pointing it changes pinned behaviour.
- **Owed to node 5's migration:** `ASessionDocumentIsShownWhereTheOperatorIsTests` (2).

## Next step

Node 4 or a re-run of node 3: answer the two seams, then build the execution kind red-first.

---

# T11 node 2 — hand-off (superseded in part by node 3 above)

Node 2 stopped at the rule-16 kick. It finished item 1 in part. Items 2 and 3 are not started.

## Done

- **Commit `a8ffe20f`** re-scopes 13 Ruling 83 tests to the Center notebook. Red: `A-rescope-red.txt` (14 assertion
  failures). Green: `A-rescope-green.txt` (40 of 41 in the re-scoped classes). Old → new names:
  - `SessionDocumentPlacementTests.ANewSessionDocument_OpensInTheLeftZone_Docked_WithMaximizedNull` → `ANewNotebook_OpensInTheCenterZone_Docked_WithMaximizedNull`
  - `…TheCodingLanding_IsTheSessionAtLeft_ElseTheCentersEmptyCopy_EachWithAFocusTarget` → `TheFactoryLanding_IsTheNotebookInTheCenter_ElseTheCentersEmptyCopy_EachWithAFocusTarget` (the collapsed-Left fallback to the "docked at the left" copy becomes "a collapsed Left does not move the landing")
  - `…TheSessionDocumentKindsRow_NamesTheLeftZone` → `TheNotebookKindsRow_NamesTheCenterZone`
  - `…AReopenedSessionDocument_FollowsTheSameZoneRule_AndRearrangesNothing` (name kept; Left → Center)
  - `PerspectiveShellTests.TheLanding_IsTheRowsZonesActiveTab_LeftForFactory_…` → `TheLanding_IsTheRowsZonesActiveTab_CenterForEveryRow_ElseTheEmptyCopy`
  - `PerspectiveShellTests.TheFactoryLanding_IsTheSessionDocument_WhenTheCoordinationKindsOccupyItsCenter` → `TheFactoryLanding_IsTheNotebook_WhenTheCoordinationKindsShareItsCenter` (the landing ignores the view's active tab and follows the model's)
  - `WorkbenchShellTests.ShowLedger_InFactory_WithASessionActiveInTheLeft_…_AndTheLandingStaysTheSession` → `ShowLedger_InFactory_WithTheNotebookInTheCenter_DocksInTheCenter_AndTheLandingIsTheCentersActiveTab`
  - `WorkbenchDragCompletedHookTests` (2): only the retired literal `"session-document"` → `"notebook"`. The drag gesture is the subject, so the Left start stays.
  - `ConsoleSplitPlacementTests.WithTheSessionAtLeft_…` → `WithTheNotebookInTheCenter_TheSplitOpensInTheCenterZone_AsAConsoleDocument`. `CollapsingTheLeftHoldingTheSession_LeavesItsConsoleOpen`: focus after the close lands inside the notebook, not on the "docked at the left" heading.
  - `AParallelSessionIsADerivedSiblingTests.…TheSiblingDocksLeftBesideIt…` → `…TheSiblingDocksInTheCenterBesideIt…`

## Still red, with the reason

1. **`ConsoleSplitPlacementTests.WithTheNotebookInTheCenter_TheSplitOpensInTheCenterZone_AsAConsoleDocument`. This is a product finding,
   not a test fix.** After the Console toggle closes the Console tab, the document's focus scope holds the notebook's
   "Goal · Done when · Not in scope" toggle instead of the Console toggle. The Console is now a sibling tab of the notebook,
   and the close reactivates the notebook, whose landing focus wins. That contradicts the CV-5.2 rule "focus returns to
   the toggle". The fix is in product code outside the grant. The next node or the Leader should decide where focus goes.
2. **`CenterEmptyCopyTests.WhileASessionIsOpenAtLeft_…`. It is NOT deleted. Seam `req-01M59WCW4YBGJ1AXQEVK63KN0J` is pending.** Rule 14:
   the premise "no notebook can dock Left any more" does not hold in code. `SurfaceKind.Zone` is read only on open, and
   drags follow drops with no kind guard. The re-scoped drag test still moves a notebook Center → Left, and nothing is refused. So
   "The session is docked at the <zone>." (`CenterEmptyState.cs:94-100`, a template, not a constant) is still reachable.
   The fallback: keep the copy, and re-scope the composed test to a notebook moved Left.
3. **`ASessionDocumentIsShownWhereTheOperatorIsTests` (2 probe tests).** They have no Left assertion. The operator's saved arrangement
   carries `session-document` surfaces, which are now retired and dropped with a report. So the probe's `dropped=6` count and the
   bind-on-restore revival fail. This belongs to slot migration (D, node 4): map `session-document` → `notebook`.
4. **`CodingsLeftExtentTests` (4).** It is untouched. **Ruling 168** (Leader mail) chose option A, tightened. Move
   `ThreadFeed.MeasureWidth` with the notebook to the Center and rename the test. Check (1) that the words column is ≥ MeasureWidth + its 40 px gutter + the
   body's padding, with nothing clipped; (2) that the rendered line width is ≤ MeasureWidth (a cap, so spare width becomes margin); (3) that both hold at the minimum and at a wide
   window. If ThreadFeed does not cap today, (2) is a red-first product change, and `ThreadFeed.cs` is granted.

## Not started

- **The `execution` kind.** The plan: Zone Bottom, `Instances.Many`, caption "Execution — <attempt>", opened from the turn row's
  existing "Open log" action (`SessionDocumentSurface.cs` ~:852) as a derived opener. Bind it to the attempt host. It never owns a
  token or a queue slot (design §4), and it carries a UIA name and a `KindAllowListsTests` row. Do not add a `session.execution` verb.

## Next step

Node 3: (a) Ruling 168's measure retarget, (b) the `execution` kind, (c) the console focus-return finding, or a ruling on it.
Answer seam `req-01M59WCW4YBGJ1AXQEVK63KN0J` before anyone touches `CenterEmptyState`.

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
