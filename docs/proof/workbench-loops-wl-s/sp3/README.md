# T4 `sp3-perspective-rerow` — proof (WL-S spine)

Track T4 of the workbench-loops drive: re-row the Perspective set to Factory · Review · Understand
and retire `FullWindow` (ADR-0045 §1(a), §1(b)). Design: `docs/design/workbench-loops-wl-s.md`
(`ed821a6e`). The WL-S Proof Pack is T11's; this folder is T4's evidence.

## Flagged risks

- **Capability loss — you can no longer browse knowledge and spec documents as graph nodes inside the
  app.** The full-window Explorer was the only in-app canvas that showed them; the docked graph
  excludes them (`WorkbenchShell.cs:1386`, `graph.ExcludeKnowledge = true`). **Interim path:** open
  the Docs Explorer, `docs/index.html` (hierarchy · graph · mind map). Ruling 149 accepts the loss
  bounded and owned by WL-S: a knowledge-inclusive mode of Understand's canvas, attached at the
  `ExcludeKnowledge` seam, with its own E2E check, before WL-S closes (design §8). Not built by T4.
- Factory lands on the Left until T11 docks the notebook in the Center (design D2; a `simplify:`
  marker on the row).
- ADR-0045 wording deviations, co-signed by both design adversaries: `Escape()` and the previous
  slot deleted (D4), the `Body` column deleted (D5).

## Checks (red first, then green)

| Check | Tests | Red | Green |
|---|---|---|---|
| three-rows | `PerspectiveSetTests.All_HasExactlyThreeRows_FactoryReviewUnderstand_EachWithABoundGesture`, `.EachRowHasItsOwnLandingZone_FactoryLeftUntilTheNotebook_ReviewAndUnderstandCenter`, `.TheRoutingOrderIsUnderstandThenReviewThenFactory`, `.ThePerspectiveGesturesAreCtrlPlusTheRailDigit_AndNothingElseUsesThem` | `three-rows-red.txt` — 4 failed, `0457daa6` | `three-rows-green.txt` — 4 passed |
| no-fullwindow | `PerspectiveSetTests.NoRowUsesFullWindow_TheBodyColumnIsGone` | `no-fullwindow-red.txt` — 1 failed, `0457daa6` | `no-fullwindow-green.txt` — 1 passed |
| kind-census | `KindAllowListsTests.TheFiveLoomkeeperKinds_AreAdmittedUnderFactory_AsOneContiguousGroup`, `.EveryKindIsAdmittedSomewhere_AndTheColumnIsTheDesignsTable` | `kind-census-red.txt` — 2 failed, `547a24f1` | `kind-census-green.txt` |
| sh41-factory | `PerspectiveShellTests.TheFactoryLanding_IsTheSessionDocument_WhenTheCoordinationKindsOccupyItsCenter`, `WorkbenchShellTests.ShowLedger_InFactory_WithASessionActiveInTheLeft_DocksInTheCenter_AndTheLandingStaysTheSession`; SH-4.1's named oracle `ASessionDocumentIsShownWhereTheOperatorIsTests.ARestoredSessionDocumentIsRevivedAndBoundAtWorkspaceOpen` on the `factory` row | `sh41-factory-red.txt` — 2 failed, `edc21c7f` | `sh41-factory-green.txt` |
| slot-alias | `PerspectiveLayoutSlotTests.SlotPathFor_MapsTheReRowedIdsToTodaysFiles_FactoryToCodingsUnderstandToArchitecturesReviewNew` | `slot-alias-red.txt` — 1 failed, `c63a8ae4` | `slot-alias-green.txt` |
| re-scoped old-row tests | every test that named an old row, the Explore body or the explorer | `rescope-red-build.txt` (compile red: 23 errors in probes/Core tests, then 343 in App tests), `rescope-red-run.txt` (runtime red: 7 of 1014 failed), `1ff6278a` | `suite-green.txt` |

`1ff6278a` also carries the deletion of `ExplorerModeTests.cs` and `ExplorerViewSourceTests.cs`
(staged earlier by `git rm`; they test only the deleted Explorer).

Tests added without their own red run (they replace deleted or re-scoped tests and were not run
against the old code): `PerspectiveRailTests.EscapeHasNoPerspectiveLevelRoute_ThePresenterHasNoEscapeOrPreviousSlot`
(its predecessor asserted the opposite), `CenterEmptyCopyTests.TheCopy_IsDerivedFromAdmission_…`,
`PerspectiveShellTests.TheLoomkeeperPanes_HaveAFocusTarget_InFactorysFirstRunState`,
`ZoneLayoutSlotsTests.ReviewPersistsToItsOwnFile_AndItsDefaultIsFourEmptyZones`.

## Runtime (Release build, launched)

`runtime-observed.txt`: the rail has three items (Factory · Review · Understand); Ctrl+2, Ctrl+3,
Ctrl+1 change the window title (`Review — AI-DE`, `Understand — AI-DE`, `Factory — AI-DE`) and move
the rail's selection; View → Show ledger opens a Ledger pane while Factory stays the active
perspective. Driven through UI Automation, no workspace open.

## Seams and files outside the first owned list

| Request | What | Outcome |
|---|---|---|
| `req-01M58HXY8HGKDVTX393PN9TDP3` | delete `NodeReaderView.cs`, `ExplorerNodeMenu.cs`, `NodeReaderEdgeRowLayoutTests.cs`; drop the ExplorerSurface step from `CanvasProbe/Program.cs` | granted; done |
| `req-01M58J8YEG7EB3NP3C998SHB67` | `ASessionDocumentIsShownWhereTheOperatorIsTests.cs`, `Workbench/ComposedCoding.cs` | expired unreceived; fallback applied: id strings, the two Explorer-body tests re-scoped to "another host is the body" (`--away`), **plus** one assertion beyond the fallback's words — the replay's dropped count 10 → 6 (the four Loomkeeper kinds are kept in Factory now) |
| `req-01M58JG10ZKHW75YME0P6G3DG2` | `ShellMode` one-valued parameters; `App.xaml` `IconExplore`; `DESIGN.md` perspective rows | expired unreceived; fallback: signature kept (constant values passed), `IconExplore` and `DESIGN.md` (36 lines with the old rows) left as named residuals |
| `req-01M58KFWAM77V2V21PBYKKA5F4` | cascade orphans `HtmlSandboxHost.cs`, `NodeReaderContentState.cs` and their tests/probe step | expired unreceived; fallback: untouched, **named HYG-A residuals** (no product user left; only `HtmlSandboxIntegrationTests.cs` and the ComposerProbe `--html-sandbox` step use them) |
| `req-01M58KH3EXYM10TGWHW3FY174Q` → `req-01M58MP8PAQRK1QP8M4XCAWHTH` | six App test files that named `shell.Coding` / `PerspectiveSet.Coding` and stopped compiling | both expired unreceived; 5b's fallback applied: the mechanical rename only (no assertion changed) in `Sessions/SessionDocumentPlacementTests.cs`, `Workbench/CollapsedBottomTests.cs`, `Sessions/TheRegionCycleCommandsReachTheFocusedDocumentTests.cs`, `Workbench/CodingsLeftExtentTests.cs`, `OpeningATerminalWithNoneOpenTests.cs`, `Sessions/AParallelSessionIsADerivedSiblingTests.cs` |
| `req-01M58M88614J682GM6JXM4NKMA` (Owner) | the knowledge-corpus loss | Ruling 149: proceed, with conditions (above) |

## Suite

`dotnet build AiDe.sln`: 0 errors. `suite-green.txt`: `tests/AiDe.Core.Tests` 2781 passed, 1 skipped,
0 failed; `tests/AiDe.App.Tests` 1038 passed, 1 failed in the full run —
`SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, environmental ("foreground
not held"; the probe needs the foreground for `SendInput`) — and that test passed alone on re-run.
The App total is 1039; the pre-T4 count was not measured here (the join recounts the baseline).

## Fix-forward

The Leader granted seams 3 and 4 retroactively (`req-01M58JG10…`, `req-01M58KFWA…`). Commit `bb28e407`.

1. **ShellMode.** `firstEntry`, `outcome` and `errorCode` removed, with the one-valued `first_entry`/`outcome`/`error_code`
   wire fields and tags (design §5 retired them); the one caller, `PerspectiveShell.Activate`, simplified. `IconExplore`
   deleted from `App.xaml`. Grep: no reference to `IconExplore`, `firstEntry`, or the old `ShellMode` shape remains in `src` or `tests`.
2. **Orphans.** Grep before deleting: `HtmlSandboxHost` was used only by `HtmlSandboxIntegrationTests.cs` and the ComposerProbe
   `--html-sandbox` step; `NodeReaderContentState` had no user at all. Deleted: both, `HtmlSandboxIntegrationTests.cs`,
   `ComposerProbe/Program.HtmlSandbox.cs`, and the probe's dispatch. `HtmlSandboxPolicy.cs` and `HtmlSandboxPolicyTests.cs`
   (used only by the host) were deleted too, by seam `req-01M58PV20W70QCYAYDQR1KPANH`; no reference remains. Build 0 errors,
   0 warnings; `AiDe.App.Tests` 1023 passed, 1 failed (the same foreground chord test), total 1024 (`suite-app-policy-delete.txt`).
3. **DESIGN.md.** The live sections now name Factory · Review · Understand (rail, icons, copy, focus landings, tooltips,
   Solution-tree prose); `IconCoding`/`IconArchitecture` read `IconFactory`/`IconUnderstand`, `IconReview` added to PS-R4;
   the menu sentence points at `PerspectiveMenu.For` rather than a per-row list. The dated errata (Rulings 83–84 and after)
   keep their rows of the day under a supersede note. Craft gate before/after: `craft-before.txt`, `craft-after.txt` are
   identical (red on main with 11 pre-existing Major findings in `docs/mockups/`; none added).
4. **Mutation reds.** Each test now has a red run against a deliberate, reverted mutation (never committed):

| Test | Mutation | Red | Green |
|---|---|---|---|
| `PerspectiveRailTests.EscapeHasNoPerspectiveLevelRoute_…` | add `PerspectiveShell.Escape()` | `mutation-escape-red.txt` | `mutation-escape-green.txt` |
| `CenterEmptyCopyTests.TheCopy_IsDerivedFromAdmission_…` | change Review's heading in `CenterEmptyState` | `mutation-centercopy-red.txt` | `mutation-centercopy-green.txt` |
| `PerspectiveShellTests.TheLoomkeeperPanes_HaveAFocusTarget_…` | sessions pane `Focusable = false` | `mutation-focustarget-red.txt` | `mutation-focustarget-green.txt` |
| `ZoneLayoutSlotsTests.ReviewPersistsToItsOwnFile_…` | Review shares Factory's slot file | `mutation-reviewslot-red.txt` | `mutation-reviewslot-green.txt` |

Suite: `AiDe.sln` 0 errors, 0 warnings (`build-fixforward.txt`). `AiDe.Core.Tests` 2781 passed, 1 skipped, 0 failed
(`suite-core-fixforward.txt`). `AiDe.App.Tests` 1037 passed, 1 failed (`suite-app-fixforward.txt`):
`SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph`, environmental ("foreground not held"). Total
1038 (was 1039; `HtmlSandboxIntegrationTests` deleted). Census NOT CHECKED: not reported by this run.
