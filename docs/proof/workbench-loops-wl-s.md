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
review-by: "2026-11-19"
summary: >-
  The slice Proof Pack for WL-S, drafted by T11 node 9. Each row cites its red and green commits: T11 nodes 1-9
  (notebook and execution kinds, Factory's Center landing, Ctrl+1/2/3 inside a WebView2 page, hide, slot
  migration with its .bak and report, prompt kept, the .architecture rename and the coordination fold), T18's
  knowledge mode and T20's float guard. Open: D2 (execution at revive) is red until T17d lands (Ruling 172), and
  float/redock waits on T21 (Ruling 171). WL-S is not accepted until T21 lands.
---

# Proof Pack: WL-S - kinds, slot migration, knowledge mode, float guard

- **Change:** branches `wls-kinds-migration` (T11, nodes 1-9), `wls-knowledge-mode` (T18, on main),
  `wls-float-guard` (T20, on main at `5b47615e`). T21 (`wls-float`) and T17d (`wl1a-readback`) are not joined.
- **Design:** `docs/design/workbench-loops-wl-s.md` §2-§4, §7, §8. ADR-0045. Rulings 149, 152, 168, 170, 171, 172, 173.
- **Status: draft. WL-S is not accepted until T21 lands** (Ruling 171 (3)). D2 is red until T17d lands (Ruling 172).
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
| 10 | **Float and redock a live notebook** (Ruling 171 (2)) | T21 | not written | **pending T21**. Probe `C-float-probe.txt` (`8b190778`) measured that a floated pane vanished | **Open** |
| 11 | Slot migration: today's Factory slot restores each `session-document:<id>` as `notebook:<id>` (same zone, tab position and caption), drops the rest with a report, keeps a one-time `.pre-notebook.bak`, and round-trips | T11 n7-n8 | `LayoutSlotMigrationTests.TodaysSlot_RestoresEachSessionPaneAsItsNotebook_ReportsTheRest_KeepsAOneTimeBak_AndRoundTrips`; operator probe `ASessionDocumentIsShownWhereTheOperatorIsTests` (6) | `22649126` → `28cbad3a`; probe `0da0c3be` → `863c2df4`, mutation `D1-probe-mutation-red.txt` | Verified |
| 12 | **D2: a revived notebook whose attempt was live at close reopens its execution in the Bottom, without moving focus** | T11 (after T17d) | `ARevivedNotebookReopensItsLiveAttemptsExecutionTests.ASessionPane_RestoresAsItsNotebook_PlusOneExecutionInTheBottom_OnlyWhenItsAttemptWasLiveAtClose` | red `98b5d7e6` (`D2-execution-at-revive-red.txt`: "the revived notebook reads 0 turn(s)"). **Green pending T17d** (Ruling 172). The shell part is granted: `ReviveRestoredSessionDocuments` → `OpenExecution(document, ordinal, activate:false)` | **Open (expected red)** |
| 13 | `prompt` is kept (Ruling 173): the migration leaves a prompt draft as it was, `prompt` is not in `RetiredKinds`, and the migrating restore's report says so once | T11 n9 | `LayoutSlotMigrationTests.APromptDraft_IsKeptAsItWas_NotRetired_AndTheMigrationsReportSaysSo_Once` | `94c374d6` → `7383f868`; `P-prompt-kept-red.txt` → `-green.txt`; design §4 amended `31c75c3f` | Verified. The sentence is pending the wording seam (`req-01M5A3FJRCNCXBRTB6RQKVMPMA`) |
| 14 | §7 rename: Understand reads `.understand`, falls back to `.architecture` once, writes the new name, keeps `<old>.bak`; T4's alias is removed | T11 n9 | `LayoutSlotMigrationTests.UnderstandsSlot_IsRenamedFromArchitecture_ReadingTheOldNameOnce_KeepingAOneTimeBak_AndRoundTrips` | `54863920` → `c56da52c`; `R-rename-fold-red.txt` → `-green.txt` | Verified. `PerspectiveLayoutSlotTests.SlotPathFor_MapsTheReRowedIdsToTodaysFiles_…` still pins the alias; re-pin on seam `req-01M5A3V1Q07WEFR57AGDMQKD92` |
| 15 | §7 fold: every surface of the Coordination slot is appended to Factory's Center as tabs, never its Left. The Center stays on the notebook. What Factory refuses is dropped with a report, the coordination file is kept once as `.bak`, and the slot round-trips | T11 n9 | `LayoutSlotMigrationTests.CoordinationsSlot_FoldsIntoFactorysCenter_AsTabs_NeverItsLeft_LandingOnTheNotebook_DropsWithAReport_AndRoundTrips` | `54863920` → `c56da52c` | Verified |
| 16 | Knowledge-inclusive canvas mode (Ruling 149 (2)): a mode of the canvas kind, default off, with its own E2E over the real daemon projection | T18 n1-n3 | checks 1-7 in `workbench-loops-wl-s/knowledge-mode/README.md`, E2E `KnowledgeModeRealProjectionTests.Understand_TheDockedCanvasInKnowledgeInclusiveMode_ShowsAKnownKnowledgeNodeWithItsTypedEdges_AndOffHidesIt` | `74e690fd` → `2ff09885` (E2E); the rest in the README | Verified by T18 (cited, not re-run here except in the suite) |
| 17 | Float guard (Ruling 171 (1)): `workbench.floatPane` refuses with a stated cause; a restored floating window is docked with a report line | T20 | `workbench-loops-wl-s/float-guard/README.md` (`floatpane-refusal`, `restore-docking`, `dock3-wording`, `dock3-zone-label`) | each `-red.txt` → `-green.txt` in that folder; joined `5b47615e` | Verified by T20 (cited) |

**Node 9's suite run:** see `kinds/suite-node9.txt` and the line below, which the node writes after the run.

## Merge conditions

| Condition | Where it is met |
|---|---|
| Ruling 152 D2: Factory's Landing → Center in the same change that docks `notebook` in the Center | row 1 (`e95bb6b2`) |
| Ruling 149 (2): the knowledge-inclusive mode with its own E2E | row 16 (T18) |
| Ruling 149 (1): the interim loss, in user terms, under Flagged risks | Flagged risks, below |
| Ruling 152 D4: the previous-perspective slot is not re-added here | not re-added (no reader exists) |
| ADR-0045 §7: a dropped kind is reported, a one-time `.bak` is written | rows 11, 14, 15 |
| Ruling 171 (3): hide/float/redock a live notebook | hide: row 9. **Float/redock: pending T21** |

## Human demo script (keyboard only)

Every step uses the keyboard. The gestures are the catalog's (`WorkbenchCommands.cs`). Each step says what
you should see. Run it on the joined build after T17d and T21 land.
*Not observed at node 9:* that **F6** reaches a turn's *Open the log* (step 3), and the **File → Recent
sessions** path (step 4; it is the path the retirement sentence names). Confirm both at the demo.

### 0. Build and launch
1. `dotnet build AiDe.sln`, then start `src/AiDe.App`. Open this repository as the workspace.
2. Before the first launch, copy an older layout folder that has a `layout.zones.json` naming a
   `session-document`, a `layout.architecture.zones.json` and a `layout.coordination.zones.json`. Step 6 uses it.

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

### 5. Float and redock a live notebook — **pending T21**
1. With a run going, focus the notebook and press **Ctrl+K, F**. *Until T21 lands, this refuses with a stated
   cause (T20's guard).* After T21: **see** the notebook in its own window, the run still going.
2. Redock it to the Center (T21 names the gesture). **See:** the same notebook, same thread, focus on it.

### 6. Restore an older layout
1. Close the app, then start it again with step 0's older layout folder.
2. **See (Factory):** each old session pane is back as a notebook, in the same tab position. The status strip
   names each pane that was dropped and where it now lives. Any prompt draft is still a prompt draft, and the
   report says "Kept 1 prompt draft as it was: prompt drafts do not become notebooks."
3. **See (Factory):** the old Coordination panes are tabs in Factory's Center, after the notebook. The Center
   still shows the notebook. Factory's Left is unchanged.
4. Press **Ctrl+3**. **See (Understand):** the old Architecture arrangement.
5. Close the app. **See on disk:** `layout.understand.zones.json` exists, and
   `layout.architecture.zones.json.bak`, `layout.coordination.zones.json.bak` and
   `layout.zones.json.pre-notebook.bak` hold the old bytes.

### 7. Restart with a live attempt — **pending T17d**
1. Start a run, then end the app's process while it runs. Start the app again.
2. **See:** the notebook reopens with its turns, and the attempt that was running reads as delivery unknown.
   An "Execution — <attempt>" tab is in the Bottom, and the focus stays on the notebook.

### 8. Knowledge and spec documents on the canvas
1. Press **Ctrl+3**, then **Ctrl+K, Shift+G** to focus the canvas.
2. **Tab** to the *Knowledge-inclusive* toggle and press **Space**. **See:** knowledge and spec nodes with their
   typed edges. Press **Space** again. **See:** they are hidden.

## Flagged risks

- **Interim loss of in-app knowledge and spec documents (Ruling 149 (1)).** T4 removed the Explore view. It was
  the only place in AI-DE where you could see the repository's knowledge and spec documents (the corpus).
  Understand's canvas hid them (`ExcludeKnowledge`). So on a build that has T4 but not T18, AI-DE shows you
  none of those documents. **Use `docs/index.html` (the Docs Explorer, in a browser) instead.** With WL-S, the
  canvas shows them again when you turn on *Knowledge-inclusive* (row 16), and the toggle starts off.
  *Inferred, not checked here:* the canvas shows the documents as nodes and edges. The deleted reader showed each
  document's text. Until a check shows the text in the app, `docs/index.html` stays the way to read it.
- **Float/redock is not real yet** (Ruling 171). Until T21 lands, *Float pane* refuses, and a restored floating
  window docks with a report line (T20).
- **A revived notebook shows none of its recorded attempts in the running app** (Ruling 172; WL-1a reopened). D2
  cannot pass until T17d fixes the read-back.
- **The fold re-runs if its `.bak` already exists.** The first save after a fold moves the coordination file to
  `<file>.bak`, but never over an existing `.bak`. In that case the file stays, and the next restore folds it
  again. Surfaces already in Factory are skipped by id, so no tab is doubled. The drop report repeats.
- **A rename's old file that cannot be read is left in place.** If `.architecture` is corrupt or newer, Understand
  restores its default and never reads it. Nothing is lost.

## Open items

1. **D2 green** waits for T17d (Ruling 172). The Leader merges T17d into this branch, and a later node turns D2
   green with the granted shell change.
2. **Float/redock** waits for T21 (Ruling 171 (2)). **WL-S is not accepted until T21 lands.**
3. Seam `req-01M5A3FJRCNCXBRTB6RQKVMPMA`: the wording of the kept-prompt sentence (built with the proposed text).
4. Seam `req-01M5A3V1Q07WEFR57AGDMQKD92`: re-pin `PerspectiveLayoutSlotTests.SlotPathFor_…` to `.understand`.
5. The `prompt` kind's retirement waits until the notebook has all six capabilities, each tested (design §4; Ruling 173).
6. Captions "Notebook — <journey>" (ADR-0045 §5): node 1 recorded the notebook's title as the session's name.
   Not re-checked at node 9.
