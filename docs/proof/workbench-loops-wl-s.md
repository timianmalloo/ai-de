---
id: proof-workbench-loops-wl-s
title: "Proof Pack: WL-S - the notebook and execution kinds, slot migration, knowledge mode, float guard"
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: "workbench-loops WL-S"
tags: [proof, workbench-loops, wl-s, notebook, execution, slot-migration, knowledge-mode, float-guard]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0045-perspective-set-factory-review-understand, rel: implements }
  - { to: design-workbench-loops-wl-s, rel: implements }
  - { to: proof-wl-s-kinds-node1-handoff, rel: documents }
review-by: "2026-11-19"
summary: >-
  The slice Proof Pack for WL-S, drafted by T11 node 9. Each row cites its red and green commits: T11 nodes 1-9
  (notebook and execution kinds, Factory's Center landing, Ctrl+1/2/3 inside a WebView2 page, hide, slot
  migration with its .bak and report, prompt kept, the .architecture rename and the coordination fold), T18's
  knowledge mode and T20's float guard. D2 is green (node 11) and DOCK4's plural const is green (node 12). Open:
  T21's close condition 4, a live notebook floated and redocked, owed after the T11 join (Ruling 171 (2)); and two
  foreground tests on the user's attended list (Ruling 155 (3)).
---

# Proof Pack: WL-S - kinds, slot migration, knowledge mode, float guard

- **Change:** branches `wls-kinds-migration` (T11, nodes 1-9), `wls-knowledge-mode` (T18, on main),
  `wls-float-guard` (T20, on main at `5b47615e`). T21 (`wls-float`) and T17d (`wl1a-readback`) are not joined.
- **Design:** `docs/design/workbench-loops-wl-s.md` §2-§4, §7, §8. ADR-0045. Rulings 149, 152, 168, 170, 171, 172, 173.
- **Status: draft. WL-S is not accepted until T21's close condition 4 lands** (Ruling 171 (3)): T21 is joined (`8608d4eb`); the live-notebook float is owed after this track joins. D2 is green since node 11 (row 12). Row 22 is green since node 12.
- Each T11 row's proof file is under `docs/proof/workbench-loops-wl-s/kinds/`. The node-by-node record is that
  folder's `handoff.md`.

## Claims and evidence

"Verified" means the red and the green were each observed at the named node. Node 9's whole-suite run
(`kinds/suite-node9.txt`) re-runs every App and Core test. Its result is recorded at the end of this section.

| # | Check | Owner | Tests (exact) | Evidence (red → green) | Confidence |
|---|---|---|---|---|---|
| 1 | The `notebook` kind binds T8's `NotebookModel` in the Center; Factory's `Landing` flips Left → Center in the same change (Ruling 152 D2); `session-document` is retired with a report | T11 n1 | `TheNotebookKindBindsTheModelTests.TheNotebookRow_IsFactorysOnly_DocksInTheCenter_AndFactoryLandsThere`, `.ANewSession_OpensAsANotebookInTheCenter_BoundToTheShellsModel`; `PerspectiveSetTests.EachRowHasItsOwnLandingZone_FactoryOnItsNotebookInTheCenter_ReviewAndUnderstandCenter` | `358c8f82` → `e95bb6b2`; `A-notebook-kind-red.txt` → `-green.txt` | Verified |
| 2 | The tests that pinned Ruling 83's Left dock are re-scoped to the Center notebook | T11 n2-n3 | the 19 tests listed in `handoff.md` (node 1); `CenterEmptyCopyTests.WhileANotebookIsMovedToTheLeft_…` | `A-rescope-red.txt` → `A-rescope-green.txt`; copy `997f21da` (`copy-rescope-green.txt`) | Verified |
| 3 | Ruling 168: the thread's 96ch measure moves with the notebook, never clipped, never stretched; Ruling 170: the minimum window width (820 → 840) is derived from it | T11 n3, n6 | `TheThreadMeasureMovesWithTheNotebookTests` (6, including the minimum row) | `d8746b1d` red, cap mutation `8976dc86` (`R168-cap-mutation-red.txt`); `9971fb3b` → `7785c44c` (`R170-minwidth-red.txt` → `-green.txt`) | Verified |
| 4 | Closing the Console returns focus to its toggle | T11 n3 | `ConsoleSplitPlacementTests` | `8e351e1c` → `e3ead08d`; `C-console-focus-red.txt` → `-green.txt` | Verified |
| 5 | The `execution` kind: Factory only, one per attempt, Bottom, captioned `Execution — <attempt>` | T11 n4 | `TheExecutionKindTests`; `KindAllowListsTests` column row | `610f4b9b` → `c500f9d8`; `A-execution-kind-red.txt` → `-green.txt` | Verified |
| 6 | A turn's *Open the log* opens one `execution:<DispatchKey>` in the Bottom; a second press focuses it | T11 n5 | `TheExecutionOpensFromOpenLogTests` | `1f78da93` → `583dddc6`; `A2-open-log-red.txt` → `-green.txt` | Verified |
| 7 | The execution's only door is *Open the log*: no View menu, no Center empty state offers it (Owner condition (1)) | T11 n6 | `TheExecutionKindTests`, `TheExecutionOpensFromOpenLogTests`, `PerspectiveMenuTests`, `CenterEmptyCopyTests` | red by mutation `18505b9f` → `56028e94`; `TA-condition1-mutation-red.txt` → `TA-condition1-green.txt` | Verified (the red is a mutation; the product already passed) |
| 8 | Ctrl+1/2/3 switch the perspective with focus inside a WebView2 page (composer, canvas), by real OS input | T11 n5 | `B-ctrl-digit` tests (38/38 with `PerspectiveMenuTests`) | red by mutation `b1703f64` → `1579f62a`; `B-ctrl-digit-mutation-red.txt` → `B-ctrl-digit-green.txt` | Verified (the red is a mutation; the product already passed) |
| 9 | Hide a live notebook: closing its pane mid-run keeps the run and says "Pane hidden. The run continues." once | T11 n6-n7 | the hide tests in `C-hide-green.txt` | `8b190778` → `bbf1465b`; shutdown `3c954c94` → `c736c96f` | Verified |
| 10 | **Float and redock a live notebook** (Ruling 171 (2)) | T21 n7, n8 | `ALiveNotebookFloatsAndRedocksTests`: (A) `ANotebookFloatedMidRun_KeepsItsModelAndItsOneAttempt_AndTheTurnCompletesInTheFloatingWindow`; (B) `TheFloatingNotebook_RedocksToTheCenter_WithItsThreadDraftAndAttemptIntact_SayingNoHide_AndFocusInIt` (mid-run, after); (C) `ALayoutSavedWithALiveNotebookFloating_RestoresItFloatingAtItsBounds_AndReadsItsRunningTurnBackAsDeliveryUnknown`. The sweep: `LayoutCommandsRenderTests` (5) | (A) red `0097164f` → green `36884736` (`float/command-render-green.txt`): `WorkbenchController.LayoutApplied`, wired to the adapter's render in `DockHost.Create`. (C) red `74af5881` (`float/live-restart-menu-red.txt`, the restored tab had no menu; `float/live-restart-red.txt`, the float leg with the wiring reverted) → green `28c60d1f` (`float/floating-tabs-green.txt`): the adapter names and decorates each floating window's tabs. (B) red `74af5881` → redocks with thread, draft and attempt intact and no DOCK1 at `28c60d1f`; red on focus there (`float/live-redock-focus-red.txt`: "it is on Window") → green `c80e5fbd` (`float/live-redock-green.txt`): the tab menu's move focuses the moved pane | **Verified by test** on T21's branch (A), (B), (C); not yet walked in the running app; joins with T21 |
| 11 | Slot migration: today's Factory slot restores each `session-document:<id>` as `notebook:<id>` (same zone, tab position and caption), drops the rest with a report, keeps a one-time `.pre-notebook.bak`, and round-trips | T11 n7-n8 | `LayoutSlotMigrationTests.TodaysSlot_RestoresEachSessionPaneAsItsNotebook_ReportsTheRest_KeepsAOneTimeBak_AndRoundTrips`; operator probe `ASessionDocumentIsShownWhereTheOperatorIsTests` (6) | `22649126` → `28cbad3a`; probe `0da0c3be` → `863c2df4`, mutation `D1-probe-mutation-red.txt` | Verified |
| 12 | **D2: a revived notebook whose attempt was live at close reopens its execution in the Bottom, without moving focus** | T11 (after T17d) | `ARevivedNotebookReopensItsLiveAttemptsExecutionTests.ASessionPane_RestoresAsItsNotebook_PlusOneExecutionInTheBottom_OnlyWhenItsAttemptWasLiveAtClose` | red `98b5d7e6` (`D2-execution-at-revive-red.txt`: "the revived notebook reads 0 turn(s)"). After T17d merged, red `ca392933` (`D2-revive-red.txt`: read back=1, "no execution:<key> opened; the Bottom holds []"; the concluded case's oracle corrected Answered → Completed, rule 14, `AttemptStateFold` maps `ResponseComplete` to `Completed`) → green `caa5df81` (`D2-revive-green.txt`, 9/9); its two guard regressions fixed in `bc1e8836` (`D2-guards-green.txt`, 16/16). App suite at `bc1e8836`: 1179/3/2 of 1184, the 3 being row 22's red and the two foreground tests (`suite-node11-app-rerun.txt`). `ExecutionAtRevive.LiveAtClose` (the LAST receipt, `DeliveryUnknown{host_restart}`) + `OpenExecution(.., activate:false)` (no `ActivateInView`, no `DocumentOpening`). `ExecutionAtReviveTests` (6); mutation `D2-predicate-mutation-red.txt` | Verified (tests); not walked in the running app |
| 13 | `prompt` is kept (Ruling 173): the migration leaves a prompt draft as it was, `prompt` is not in `RetiredKinds`, and the migrating restore's report says so once | T11 n9 | `LayoutSlotMigrationTests.APromptDraft_IsKeptAsItWas_NotRetired_AndTheMigrationsReportSaysSo_Once` | `94c374d6` → `7383f868`; `P-prompt-kept-red.txt` → `-green.txt`; design §4 amended `31c75c3f` | Verified. The Owner confirmed the sentence; DESIGN.md row DOCK4 carries it (row 20) |
| 14 | §7 rename: Understand reads `.understand`, falls back to `.architecture` once, writes the new name, keeps `<old>.bak`; T4's alias is removed | T11 n9 | `LayoutSlotMigrationTests.UnderstandsSlot_IsRenamedFromArchitecture_ReadingTheOldNameOnce_KeepingAOneTimeBak_AndRoundTrips` | `54863920` → `c56da52c`; `R-rename-fold-red.txt` → `-green.txt` | Verified. The alias pin is re-pinned to `.understand` as `PerspectiveLayoutSlotTests.SlotPathFor_MapsEachRowToItsFile_FactoryKeepsCodingsEmptySuffix_UnderstandAndReviewTheirIds` (T11 n10 `1b95c589`, `slot-repin-green.txt`, 27/27; seam `req-01M5A3V1Q07WEFR57AGDMQKD92` granted) |
| 15 | §7 fold: every surface of the Coordination slot is appended to Factory's Center as tabs, never its Left. The Center stays on the notebook. What Factory refuses is dropped with a report, the coordination file is kept once as `.bak`, and the slot round-trips | T11 n9 | `LayoutSlotMigrationTests.CoordinationsSlot_FoldsIntoFactorysCenter_AsTabs_NeverItsLeft_LandingOnTheNotebook_DropsWithAReport_AndRoundTrips` | `54863920` → `c56da52c` | Verified |
| 16 | Knowledge-inclusive canvas mode (Ruling 149 (2)): a mode of the canvas kind, default off, with its own E2E over the real daemon projection | T18 n1-n3 | checks 1-7 in `workbench-loops-wl-s/knowledge-mode/README.md`, E2E `KnowledgeModeRealProjectionTests.Understand_TheDockedCanvasInKnowledgeInclusiveMode_ShowsAKnownKnowledgeNodeWithItsTypedEdges_AndOffHidesIt` | `74e690fd` → `2ff09885` (E2E); the rest in the README | Verified by T18 (cited, not re-run here except in the suite) |
| 17 | Float guard (Ruling 171 (1)): `workbench.floatPane` refuses with a stated cause; a restored floating window is docked with a report line | T20 | `workbench-loops-wl-s/float-guard/README.md` (`floatpane-refusal`, `restore-docking`, `dock3-wording`, `dock3-zone-label`) | each `-red.txt` → `-green.txt` in that folder; joined `5b47615e` | Verified by T20 (cited) |
| 18 | DOCK1 is said once per close with no `Loaded` hook (DC-138). The reset runs when the shell shows a kept document again (`RegisterSessionDocument` → `SessionDocumentSurface.PaneShown`) | T11 n10 | `TheWebSurfacesInitialiseOnceAcrossReparentsTests.NoElementOutsideTheHostHooksLoadedForItsOwnInitialisation`, `ALiveNotebookSurvivesHideAndReopenTests` (2), `TheRunOutlivesItsPaneTests` | red `suite-node9.txt` (from `c736c96f`) → `005b8da5`; `DC138-reset-green.txt` (10/10) | Verified |
| 19 | Every `AddSurface` in the shell raises the seam after its add, and each add is judged within its own command: the head is derived from member declarations, not from a hand-kept list (DC-NEW-wl-t11-kinds) | T11 n10 | `EveryOpeningCommandPassesThroughTheSeamTests.EveryAddSurfaceInTheShell_IsFollowedByTheSeamOnceApplied_BeforeTheRender`, `TheExecutionOpensFromOpenLogTests`, `ConsoleSplitPlacementTests` | red `suite-node9.txt` → `3b567ce9`; `open-seam-green.txt` (12/12); mutation `open-seam-mutation-red.txt` names `OpenExecution` | Verified, both ways |
| 20 | DESIGN.md row DOCK4 carries the kept-prompt sentence, singular and plural (Ruling 173; the Owner confirmed it) | T11 n10 | `LayoutSlotMigrationTests.TheDesignRowDock4_CarriesTheKeptPromptSentenceWordForWord` | `47df88af` → `6d214263`; `DOCK4-row-red.txt` → `-green.txt` | Verified for the singular against the product's sentence. The plural is pinned to the product's const by row 22 |
| 21 | D6 as the Owner amended it: switch statements list the three `SurfaceEntry` cases with no default; a switch expression's discard throws `UnreachableException` naming the type; one reflection test covers every subtype for every reader | T11 n11 | `EverySurfaceEntryIsHandledByEveryReaderTests` (subtype theory, `CenterEmptyState_OffersTheViewMenu_ExactlyWhereItsArmCountsARow`, `EveryRow_NamesItsDoor_ThroughTheFactorysArm`, `TheReadersAreTheOnesThisTestCovers`) | red `06e37edb` (`D6-reflection-red.txt`, planted `PlantedFourth`, uncommitted and reverted: names all three readers) → green `cb5cb59b` (design D6 amended; `D6-reflection-green.txt` 53/53). CS8509 observed: `D6-cs8509-probe.txt` | Verified |
| 22 | DOCK4's plural is one const that the DOCK4 row test pins word for word, and a two-prompt restore says it | T11 n11-n12 | `LayoutSlotMigrationTests.TheDesignRowDock4_CarriesTheKeptPromptSentenceWordForWord`, `.TwoKeptPromptDrafts_SayDock4sPluralSentence_WordForWord` | red `b62261e8` (`DOCK4-plural-red.txt`: "LayoutPersistence has no KeptPromptsSentence const") → green `c25e3e90` (`DOCK4-plural-green.txt`, 6/6). Mutating the const ("as they are") turned both tests red (`DOCK4-plural-mutation-red.txt`); reverted | Verified |

**Node 9's suite run** (`kinds/suite-node9.txt`): Core 3024/3025 passed (1 skipped, 0 failed). App 1146/1155
passed (2 skipped, **7 failed**). The 7: three expected (D2 ×2, the alias pin), two environmental (real-input tests
without the foreground), and **two branch regressions from T11 nodes 4–8** (DC-138's Loaded-hook control on
`SessionDocumentSurface.cs`, from `c736c96f`; the open-seam ordering control in `WorkbenchShell.OpenSessionConsole`).
**The branch is not join-ready until those two are fixed.** Detail: `kinds/handoff.md`, node 9, Suites.

**Node 10's suite run** (`kinds/suite-node10.txt`, HEAD `6d214263`): Core 3024/3025 passed (1 skipped, 0 failed). App
1150/1156 passed (2 skipped, **4 failed**). Both branch regressions and the alias pin are green (rows 18, 19, 14). The
4 left: **D2 ×2**, expected until T17d ("read back=0"). **Two environmental:** `CtrlDigitSwitchesThePerspectiveFromInsideAPageTests`
("foreground held=False") and `SolutionTreeChordTests.CtrlEnter_…` ("foreground not held"). Re-run alone, they fail the
same way (`kinds/suite-node10-foreground-solo.txt`): this background session cannot take the foreground. *Unverified*
that they pass with it. The last green is `B-ctrl-digit-green.txt` (node 5).

**Node 12's suite runs.** At the merge head `31077bb7` (main with T21 joined), before any edit
(`kinds/suite-node12-start.txt`): Core 3046/3047 passed (1 skipped, 0 failed); App 1201/1206 passed (2 skipped,
3 failed): row 22's expected red and the two foreground tests. The merge brought in no red. At `c25e3e90`
(`kinds/suite-node12.txt`): Core 3046/3047 passed (1 skipped, 0 failed); App 1202/1206 passed (2 skipped,
**2 failed**), both foreground tests, now on the attended list. No desktop-hold result occurred.

## Merge conditions

| Condition | Where it is met |
|---|---|
| Ruling 152 D2: Factory's Landing → Center in the same change that docks `notebook` in the Center | row 1 (`e95bb6b2`) |
| Ruling 149 (2): the knowledge-inclusive mode with its own E2E | row 16 (T18) |
| Ruling 149 (1): the interim loss, in user terms, under Flagged risks | Flagged risks, below |
| Ruling 152 D4: the previous-perspective slot is not re-added here | not re-added (no reader exists) |
| ADR-0045 §7: a dropped kind is reported, a one-time `.bak` is written | rows 11, 14, 15 |
| Ruling 171 (3): hide/float/redock a live notebook | hide: row 9. Float and redock: row 10, green by test on T21's branch; **pending T21's join** and the app walk-through |

## Human demo script (keyboard only)

Every step uses the keyboard. The gestures are the catalog's (`WorkbenchCommands.cs`). Each step says what
you should see. T17d, T11 and T21 are all on main (the T21 float fix at `dc03007c`, the redock, restart and floating-tab
menu at `26e14f8b`). Run it on a Release build of a **clean worktree of origin/main**, never the primary checkout,
because joins change the primary (the WL-1a lesson).
*Not observed at node 9:* that **F6** reaches a turn's *Open the log* (step 3), and the **File → Recent
sessions** path (step 4; it is the path the retirement sentence names). Confirm both at the demo.

### 0. Build and launch
1. In a clean worktree of origin/main: `dotnet build AiDe.sln -c Release`, then start
   `src/AiDe.App/bin/Release/net10.0-windows/AiDe.App.exe`. Open this repository as the workspace.
2. Step 6 needs an older layout, but **do not copy it before the first launch**: the slot migration and the rename/fold
   run at the first launch that reads it (step 1), so step 6's relaunch would then show no drop report, no kept-prompt
   sentence and no new `.bak` files. Put it in place **with the app closed, between step 5 and step 6's relaunch**. It
   needs:
   - a `layout.zones.json` naming at least one `session-document` pane **and at least one `prompt` tab**, because the
     kept-prompt sentence prints only when a slot migrated and a prompt draft was kept (`LayoutPersistence.cs:427-431`);
   - a `layout.architecture.zones.json` and a `layout.coordination.zones.json`;
   - no `layout.understand.zones.json` and none of the three `.bak` files, from an earlier run.
   The Owner's staging script `wls-step6-reset.py` does exactly this for the demo tree.

### 1. Factory lands on the notebook in the Center
1. Press **Ctrl+1**. Factory opens.
2. Press **Ctrl+N** and start a session. **See:** a tab "Notebook — …" opens in the Center and has focus.
   Nothing new docks at the Left.

### 2. Ctrl+1/2/3 from inside a page
1. Type in the notebook's composer (the focus is inside its WebView2 page).
2. Press **Ctrl+3**, then **Ctrl+2**, then **Ctrl+1**. **See:** Understand, Review, then Factory, each time
   with no key typed into the composer.

### 3. A run, and its execution
1. Send a turn from the composer. While it runs, press **F6** until the focus is on the turn, then use
   *Open the log* (**Tab** to it, **Enter**). **See:** one "Execution — <attempt>" tab in the Bottom.
2. Press *Open the log* again. **See:** the same tab gets focus; no second tab opens.

### 4. Hide a live notebook
1. With the run still going, focus the notebook tab and press **Ctrl+W**. **See and hear:** "Pane hidden.
   The run continues." once.
2. Reopen it from **File → Recent sessions** (**Alt**, then the arrow keys). **See:** the run's end is in the thread.

### 5. Float and redock a live notebook — **proved by test on main; this walk is the first in the running app**
A layout command renders on main (`WorkbenchController.LayoutApplied`, joined at `dc03007c`). Before that fix, *Float
pane* floated the notebook in the model and drew no window, so watch for a visible floating window, not only the
announcement. **Ctrl+K, F is not bound as a key:** the catalog announces it, but
`KeyGestures.For` binds single-chord gestures only, so the keyboard route is the command palette.
1. With a run going, focus the notebook (**Tab** into it), press **Ctrl+Shift+P**, type `Float pane`, and press
   **Enter**. **See:** the notebook in its own window, the run still going; the status reads "Binding is now floating."
   *Proved by test (row 10 (A), the same keys through the palette); not yet walked in the running app.*
2. In the floating window, focus the notebook's tab and press the **menu key** (or **Shift+F10**), then **Move to**,
   then **C** (*Center*). **See:** the same notebook back in the Center, the same thread and draft, focus in it, and no
   "Pane hidden" announcement. *By test (row 10 (B)): the tab now carries the menu (`28c60d1f`), and the notebook
   returns with its thread, draft and attempt, no DOCK1, and the focus in it (`c80e5fbd`; before it, the focus
   landed on the window, `float/live-redock-focus-red.txt`).*

### 6. Restore an older layout
1. Close the app. With it closed, put step 0.2's older layout in place (or run `wls-step6-reset.py`). Then start the
   app again.
2. **See (Factory):** each old session pane is back as a notebook, in the same tab position. The status strip
   names each pane that was dropped and where it now lives. Any prompt draft is still a prompt draft, and the
   report says "Kept 1 prompt draft as it was: prompt drafts do not become notebooks."
3. **See (Factory):** the old Coordination panes are tabs in Factory's Center, after the notebook. The Center
   still shows the notebook. Factory's Left is unchanged.
4. Press **Ctrl+3**. **See (Understand):** the old Architecture arrangement.
5. Close the app. **See on disk:** `layout.understand.zones.json` exists, and
   `layout.architecture.zones.json.bak`, `layout.coordination.zones.json.bak` and
   `layout.zones.json.pre-notebook.bak` hold the old bytes.

### 7. Restart with a live attempt
1. Start a run, then end the app's process while it runs. Start the app again.
2. **See:** the notebook reopens with its turns, and the attempt that was running reads as delivery unknown.
   An "Execution — <attempt>" tab is in the Bottom, and the focus stays on the notebook.

### 8. Knowledge and spec documents on the canvas
1. Press **Ctrl+3**, then **Ctrl+K, Shift+G** to focus the canvas.
2. **Tab** to the *Knowledge-inclusive* toggle and press **Space**. **See:** knowledge and spec nodes with their
   typed edges. Press **Space** again. **See:** they are hidden.

### Attended list (Owner, Ruling 155 (3))
These two tests send real keyboard input, so they need the foreground. No unattended run on this branch could take
it ("foreground held=False", "foreground not held"), so both are **NOT CHECKED** until you run them at the desk.
Run each from a terminal in this repository's root. Start it, then do not touch the mouse or keyboard until it ends.
A pass is `Passed!` with `Failed: 0`.
1. Ctrl+1/2/3 with the focus inside a WebView2 page (step 2's oracle):
   `dotnet test tests/AiDe.App.Tests --filter "FullyQualifiedName~AiDe.App.Tests.Kinds.CtrlDigitSwitchesThePerspectiveFromInsideAPageTests" -nologo`
2. Ctrl+Enter on a file in the solution tree asks to reveal it in the graph:
   `dotnet test tests/AiDe.App.Tests --filter "FullyQualifiedName=AiDe.App.Tests.SolutionTreeChordTests.CtrlEnter_OnAFileArtifact_RequestsRevealInGraph" -nologo`

## Flagged risks

- **Interim loss of in-app knowledge and spec documents (Ruling 149 (1)).** T4 removed the Explore view. It was
  the only place in AI-DE where you could see the repository's knowledge and spec documents (the corpus).
  Understand's canvas hid them (`ExcludeKnowledge`). So on a build that has T4 but not T18, AI-DE shows you
  none of those documents. **Use `docs/index.html` (the Docs Explorer, in a browser) instead.** With WL-S, the
  canvas shows them again when you turn on *Knowledge-inclusive* (row 16), and the toggle starts off.
  *Inferred, not checked here:* the canvas shows the documents as nodes and edges. The deleted reader showed each
  document's text. Until a check shows the text in the app, `docs/index.html` stays the way to read it.
- **A live notebook's float and redock are proved by test on main, not yet walked in the running app** (Ruling 171
  (2)/(3)). T21 node 7's red (`0097164f`, row 10) found that no layout command run through the controller rendered:
  *Float pane* floated the notebook in the model and drew no window. The Leader measured that live on a clean origin/main.
  Node 8 found that close, collapse, maximize and next tab by the palette had the same gap (red `626c7221`,
  DC-NEW-wl-t21-float-d). All five are fixed on main (`dc03007c`), and the redock, restart and floating-tab menu are
  joined at `26e14f8b`. T21 node 2's earlier float test had passed by rendering by hand (DC-NEW-wl-t28-compiled's
  class); a layout-command class gate is owed (T21 node 9).
- **A restored turn row's compiled prompt may be empty after a restart** (Inferred; T30 is measuring it under
  Ruling 182 (4)). The fix rebuilds and verifies the text, or shows "not recorded".
- **D2 is proved by tests, not in the running app.** After T17d, the revive E2E test over a real daemon is green
  (row 12). No one has walked the restart in the app on this branch yet.
- **The fold re-runs if its `.bak` already exists.** The first save after a fold moves the coordination file to
  `<file>.bak`, but never over an existing `.bak`. In that case the file stays, and the next restore folds it
  again. Surfaces already in Factory are skipped by id, so no tab is doubled. The drop report repeats.
- **A rename's old file that cannot be read is left in place.** If `.architecture` is corrupt or newer, Understand
  restores its default and never reads it. Nothing is lost.

## Open items

1. Closed at node 11: D2 is green (row 12).
2. Closed by join: T21 landed (`dc03007c`, `26e14f8b`), and close condition 4 is proved by test. **WL-S is accepted
   only by the user's walk-through of this script** (Ruling 171 (3)), as WL-1a was.
3. Closed at node 10. The Owner confirmed the kept-prompt sentence (DOCK4, row 20). The alias pin was re-pinned (row 14).
4. Closed at node 11: D6 amended (row 21). Closed at node 12: DOCK4's plural const (row 22, `c25e3e90`).
5. The `prompt` kind's retirement waits until the notebook has all six capabilities, each tested (design §4; Ruling 173).
6. Captions "Notebook — <journey>" (ADR-0045 §5): node 1 recorded the notebook's title as the session's name.
   Not re-checked at node 9.
