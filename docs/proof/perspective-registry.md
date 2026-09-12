---
id: proof-perspective-registry
title: "Proof Pack — The Perspective registry, the allow-list column and the derived menu, palette and routing (SH-1, ADR-0030)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, addendum-c, perspective, allow-list, menu, command-catalog, shell-lane]
links:
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: tested-by }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: coordination-addendum-cd, rel: relates-to }
  - { to: note-addendum-c-design-menu-names, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: note-sh1-scope-and-entry-columns, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  SH-1 of the Shell lane: PerspectiveSet's three Core rows, the Perspectives/Instances/Entry columns
  on the eighteen kind rows, and PerspectiveMenu — one derivation the menu bar, the palette and the
  routed kind-open all read. The plan's nine reds observed (five against the old code, the rest by
  mutation), twenty-five mutations run across three passes, the four US-C10 chord collisions
  resolved, ShellViewMode renamed to the row set. Core 2,262/0, App 673/0 at close (pasted from the
  runner after the last rebuild).
---

# Proof Pack — The Perspective registry and the derived menu (SH-1)

- **Change:** branch `lane/shell-sh1` from `main` @ `8d54aadc`; worktree `C:\Projects\ai-de-lane-shell-sh1`
- **Spec / design:** `docs/specs/addendum-c-perspectives.md` (§A7, §B3, US-C1, US-C3, US-C4, US-C10, US-C11) · `docs/adr/0030-perspective-registry-and-allow-lists.md` · `docs/notes/addendum-c-design-menu-names.md` (PS-M1, Ctrl+1/2/3) · `DESIGN.md` PS-M1/M3/M4 · the slice's own decisions: `docs/notes/sh1-scope-and-entry-columns.md`
- **Tier:** T2 · **Fan-out cap:** 3 (persona reviews, read-only, two rounds)
- **Author / date:** session `sh-1` (Claude Opus, `/implement`), 2026-09-12

## What landed (the E7 surface list, ticked)

| Surface | Reached | Where |
|---|---|---|
| Core row set | yes | `src/AiDe.Core/Workbench/Perspectives.cs` — `Perspective(Id, Title, Order, Body, CommandId, Description)` (+ `Gesture` = `Ctrl+{Order}`), `PerspectiveSet.All` = Coding · Explore · Architecture, `RoutingOrder` = Architecture · Coding, `Initial` = Coding, `ByCommandId` |
| Command catalog | yes | `src/AiDe.Core/Workbench/WorkbenchCommands.cs` — `Scope` column (`CommandScope`: Global · DockHost · Admits(kind), no default); `perspective.*` rows derived from the set, replacing `shell.toggleExplorer`; the six per-kind openers retired (derived from kind rows now); entry verbs → `_File`; `_Terminal` → `_Prompt`; `workspace.diagnostics` captioned "Diagnostics report"; `focusCanvas` re-lettered and scoped `Admits("canvas")` |
| Kind rows (the allow-list column) | yes | `src/AiDe.App/Workbench/SurfaceContentFactory.cs` — `SurfaceKind(Kind, Title, Summary, Build, Perspectives, Instances, Entry, Windowed)`; `Entry` a closed record hierarchy `Derived(Menu)` \| `Verb(CommandId)`; eighteen rows, no defaults, row order = §B3 menu order |
| The derivation | yes | `src/AiDe.App/Workbench/PerspectiveMenu.cs` — `For(p[, kinds, catalog])`, `Menus` (`PerspectiveMenuGroup(Menu, Catalog, Derived)`), `Commands`, `Opener(row)`, `TryParseOpener`, `Admits`, `Resolve(kind, active)`, `Search` |
| Menu bar | yes | `src/AiDe.App/Workbench/MainMenuBuilder.cs` — renders `PerspectiveMenu`; the static `Layout` tuple list deleted; bound-only keystroke column; `PerspectiveMenuItem` (not checkable — WPF's own click pipeline runs untouched and never toggles it; the Toggle pattern exposed by its automation peer from `IsChecked`) with the accent check glyph in the icon column |
| Palette | yes | `src/AiDe.App/Workbench/CommandPalette.cs` — `Menu` property (the same model the bar renders); rows = the menu's commands; speaks a keystroke only when bound |
| Controller | yes | `src/AiDe.App/Workbench/WorkbenchController.cs` — `perspective.*` case → `PerspectiveRequested`; `surface.new.<kind>` / `surface.show.<kind>` case → one seam `OpenSurfaceRequested(kind, showExisting)` (replacing six); `KeyGestures.For` binds Ctrl+D1..3 and Ctrl+NumPad1..3 |
| Shell | yes | `src/AiDe.App/Workbench/WorkbenchShell.cs` — `OpenKind(kind, showExisting)` wires the seam by row (Show = activate the one open surface, else open); node-menu callers open by kind; `OpenReferenceDocument(intoStackId)` carries the prompt kind's placement; `PaletteCommands` (a test-only second path, DC-135's shape) deleted |
| Presenter (rename only) | yes | `src/AiDe.App/Workbench/ShellModeController.cs` — `ShellViewMode` deleted, `Mode : Perspective`, `Set(Perspective, trigger)`, `Toggle` retired; `MainWindow.xaml.cs` binds `PerspectiveRequested`, rebuilds the menu and hands the palette the same model on every switch, and carries the one rule `MainWindow.OnDocumentOpening(mode)` (a document opens where the operator is; only a full-window body hands over to the initial host) that the window, the replay probe and the pairing test all call; `WorkbenchDiagnostics.ShellMode` writes the row **id**; `MainWindow.xaml` names the rail item "Explore perspective" (its tooltip's keystroke set from the row) |
| The rail's binding point | exposed | `PerspectiveSet.All` (three destinations in row order) · `controller.Execute(p.CommandId)` · `ShellModeController.Mode`/`ModeChanged` · `PerspectiveMenu.For(p)` · `CommandPalette.Menu` · `PerspectiveMenu.Resolve`. SH-2 wires the rail; the Explore rail button runs `perspective.explore` (a destination, not a toggle) until then |
| Compute readers | yes | the menu bar (`Build`), the palette (`Refresh`), the controller's switch (`TryParseOpener`, `ByCommandId`), `KeyGestures.For`, `Bind` (window `InputBindings`), the `shell.mode` log line (`mode`/`from` = ids). `Resolve` has no product reader yet — SH-2's routed-open transaction (residual, below) |

## The registry's three rows

| Id | Title | Order / gesture | Body | Command |
|---|---|---|---|---|
| `coding` | Coding | 1 · Ctrl+1 (D1 + NumPad1) | DockHost | `perspective.coding` — **initial** |
| `explore` | Explore | 2 · Ctrl+2 | FullWindow | `perspective.explore` |
| `architecture` | Architecture | 3 · Ctrl+3 | DockHost | `perspective.architecture` |

Routing order for an inadmissible kind-open: **Architecture · Coding** (US-C3). `Tests` is reserved, absent.

## The allow-list matrix as landed (§A7 as ruled by 59–61)

| Kind | Coding | Explore | Architecture | Instances | Entry |
|---|---|---|---|---|---|
| `session-document` | admits | — | — | Many | verb `session.new` (File) |
| `terminal` | admits | — | — | Many | verb `terminal.new` (File) |
| `prompt` | admits | — | — | Many | derived, `_Prompt` |
| `sessions` (Terminal sessions) | admits | — | — | One | derived, `_View` |
| `board` (Message board) | admits | — | — | One | derived |
| `leaderboard` | admits | — | — | One | derived |
| `ledger` | admits | — | — | One | derived |
| `daydreams` | admits | — | — | One | derived — **reachable for the first time** (Ruling 60) |
| `search` | admits | — | — | Many | derived |
| `codeviewer` | admits | — | admits | Many | derived |
| `diagnostics` | admits | — | — | One | derived |
| `canvas` (Graph) | — | — | admits | One | derived |
| `view` (Evidence) | — | — | admits | One | derived |
| `inspector` (Provenance) | — | — | admits | One | derived |
| `classdiagram` | — | — | admits | Many | derived |
| `sequence` | — | — | admits | Many | derived |
| `contexts` | — | — | admits | One | derived |
| `joins` | — | — | admits | One | derived |

Explore's column is "—" by construction: no row names it, and the build test refuses a row that does.

## The derivation rule's oracle (§B3, rendered through `MainMenuBuilder`)

| Perspective | File | View — derived open/show | Prompt | Edit / Window |
|---|---|---|---|---|
| Coding | New session… · New terminal · New Claude Code session · New GitHub Copilot session · (workspace verbs) | Show terminal sessions · Show message board · Show leaderboard · Show ledger · Show daydreams · New search · New code viewer · Show diagnostics | Dispatch prompt to terminal… · New prompt draft | present |
| Explore | (same) | none (not a host) — View holds the radio and Clear the status message | absent | absent |
| Architecture | (same) | Show graph · Show evidence · Show provenance · New class diagram · New sequence diagram · Show contexts · Show joins · New code viewer | absent | present |

Top-level names: File · Edit · View · Window · Prompt · Help (PS-M1). **Deviations from the spec's table as printed, recorded here and in the test:** the File entries carry the catalog's own titles (`New session…` keeps the ellipsis it has always had — it opens a sheet; the table wrote "New session"); the derived titles are `<Verb> <title>` with the noun lower-cased, in the catalog's own voice and US-C4's own text ("New prompt draft", "New sequence diagram"), where the table capitalised the noun ("New Class diagram"); and **"Focus graph canvas" is absent from Explore** — the table's §B3 rule 2 offers it wherever the body holds a graph, but the seam it runs is bound to a docked canvas in the workbench, so in Explore the row would answer "not ready" (PS-M3: structurally inapplicable → absent; the UX & Accessibility reviewer's finding). Findings for the spec.

## The four chord collisions (US-C10 b2) — resolution

| Announced string | Was announced for | Resolution |
|---|---|---|
| `Ctrl+K, M` | `workbench.moveSurface` + `workbench.newClassDiagram` | the class-diagram opener is now derived from its kind row (`surface.new.classdiagram`) and carries **no gesture** — nothing bound one, and an unbound chord is shown nowhere (PS-M4) |
| `Ctrl+K, D` | `workspace.diagnostics` + `workbench.newPromptDraft` + `workbench.newDiagnostics` | both openers derived from their rows, no gesture; `workspace.diagnostics` keeps `Ctrl+K, D` and is captioned "Diagnostics report" (§B3 rule 1) |
| `Ctrl+K, F` | `workbench.floatPane` + `workbench.newSearch` | the search opener derived, no gesture; `floatPane` keeps `Ctrl+K, F` |
| `Ctrl+K, G` | `workbench.focusCanvas` + `terminal.new.copilot` ("New GitHub Copilot session") | `focusCanvas` re-lettered to **`Ctrl+K, Shift+G`** (in the Shell lane's own file); the harness row (Core/Terminal, unowned this horizon) keeps its letter |

The collector (`PerspectiveMenuTests.NoAnnouncedGestureStringNamesMoreThanOneCommand`) reads the catalog ∪ every perspective's derived rows ∪ `KeyGestures.For` bindings, and stays in the suite. Its copy half, `NoOperatorCopyNamesAnUnboundChord`, scans `src/AiDe.App` for `Ctrl+K,` in a string literal (allowlist: the catalog and the harness profiles; one frozen pre-existing count for `DiagnosticsSurface.cs:90`, outside this lane's paths — a finding for its owner).

## Claims & evidence

| # | Claim | Evidence (test) | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| 1 | Exactly three perspectives, Coding · Explore · Architecture, Coding initial (US-C1; ADR test 1) | `PerspectiveSetTests.TheSetHasExactlyThreeRows_InTheOrderCodingExploreArchitecture` | a fourth row; a reordered row | **M9** (a fourth `tests` row) | Verified | — |
| 2 | Bodies as ruled: two hosts and one full-window surface (Ruling 52d) | `TheBodiesAreTwoHostsAndOneFullWindowSurface` | Explore as a host | **M17** (Explore given `DockHost`) | Verified | — |
| 3 | The three perspective commands are derived into the catalog from the rows — one each, `_View`, Global, title ends in " perspective"; `shell.toggleExplorer` is gone | `EveryRowHasACatalogCommand_DerivedFromIt_AndTheToggleIsGone` | a row's command missing from the derivation; the toggle surviving | **M18** (one perspective filtered out of the derivation) | Verified | — |
| 4 | Ctrl+1/2/3 spelled from the rail digit; bound on D1..3 and NumPad1..3; the announced string is the binding's display string; `Bind` installs six bindings at the host (US-C10 b1/b3) | `ThePerspectiveGesturesAreCtrlPlusTheRailDigit_AndNothingElseUsesThem`; `PerspectiveMenuTests.EveryPerspectiveCommandIsBound_…`; `Bind_InstallsSixPerspectiveKeyBindings_AtWindowScope` | `KeyGestures.For` yields nothing (the DC-011 class) | **M6** (case disabled); M9 | Verified | delivery inside a WebView2 page — SH-2's attended P-7 |
| 5 | Routing order Architecture · Coding; `Resolve` = active if admits else first that admits else null — the law over every kind × perspective (US-C3; ADR test 4) | `TheRoutingOrderIsArchitectureThenCoding`; `Resolve_RoutesAnInadmissibleKindOpen_ArchitectureThenCoding` (9 rows); `Resolve_ObeysItsLaw_OverEveryKindAndEveryPerspective` (54 cells); `Resolve_ReturnsNullForAKindNoPerspectiveAdmits` | order reversed; the active perspective not preferred | **M8** (reversed); **M3** (codeviewer leaves Architecture) | Verified | **`Resolve` has no product caller until SH-2's routed-open transaction; US-C3's falsifier ("View source" from Explore opens in the Coding host) is reproducible today** (the pre-Addendum-C behaviour, kept by the guarded handler) |
| 6 | Every kind row admits ≥ 1 host perspective, never Explore, no duplicates; a `Verb` row names a Global `_File` catalog command and derives nothing; a `Derived` row names one of the six menus (US-C3 b5; ADR test 2) | `EveryKindRowAdmitsAtLeastOneHostPerspective_AndNeverAFullWindowOne`; `EveryEntryVerbRowNamesAGlobalCatalogCommand_AndEveryOtherRowDerivesItsEntry` | an empty set; a full-window admitter; a verb the catalog lacks | **M2** (`daydreams` → `[]`); **M12** (`terminal.newAgent`) | Verified | — |
| 7 | The matrix equals §A7 as ruled, incl. Instances | `TheAllowListsEqualTheSpecsTable` (18 literal rows) | any row moved, added or dropped | M2, M3 | Verified | — |
| 8 | The rendered menu equals §B3's table per perspective (with the three deviations above); Explore has no Edit/Window/Prompt; Architecture no Prompt (US-C4; ADR test 3) | `TheRenderedMenuEqualsTheSpecsLiteralTable_ForEveryPerspective` — through the real `MainMenuBuilder` (E11) with a test-built controller and no Exit/Recent hand-offs | a derived row missing/misplaced; a body-conditional entry offered where the body is not a host | **M1** (derived rows dropped); M2; M17 | Verified | order within View beyond the table's columns is this slice's choice (radio · body ops · derived) |
| 9 | The mutation test: a test-time kind row admitted only by Architecture appears in Architecture's View menu and palette and nowhere else, with no edit to the builder (US-C4) | `ATestTimeKindRowAdmittedOnlyByArchitecture_AppearsThereAndNowhereElse` | a per-perspective list in the builder | M1 | Verified | the appended row is not openable (the shell reads the product list) — not claimed |
| 10 | Palette rows == the rendered menu's items in every perspective; `surface.new.classdiagram` absent in Coding (US-C4 b3; ADR test 5) | `ThePaletteRowsEqualTheMenusCommands_InEveryPerspective` (rendered bar vs palette); `CommandPaletteTests.ThePalette_ListsExactlyTheMenusCommands` | a palette-only row; a row the perspective cannot offer | M1 (`surface.show.daydreams` absent) | Verified | the product's hand-off (`ModeChanged` → `RebuildMenu` → `Palette.Menu`) is window-only — **Inferred** until SH-2's attended run |
| 11 | Bound-only keystrokes on the menu; the palette speaks a keystroke only when bound; no operator copy names an unbound chord (PS-M4, US-C10 b3) | `MainMenuTests.EveryMenuItemShowsItsKeyboardChord` (re-scoped); `ThePerspectiveItemsShowTheirBoundGesture_AndDerivedOpenersShowNone`; `ThePaletteSpeaksAKeystrokeOnlyWhenOneIsBound`; `NoOperatorCopyNamesAnUnboundChord` | printing/speaking the catalog chord; a new chord literal | **seen red against the old builder** ("workspace.open shows 'Ctrl+K, O' but nothing binds it"); **M5**, **M11**, **M19** | Verified | `DiagnosticsSurface.cs:90` frozen (outside the lane) |
| 12 | The perspective entries are a radio group: the active one checked **and drawn** (the accent glyph), the Toggle state exposed to the automation tree from `IsChecked` on every row, and a click through WPF's own pipeline (mouse, keyboard, UIA Invoke) never toggles it **while the menu still closes and nothing keeps mouse capture** (PS-M1, US-C1 b3, WCAG 2.1.2) | `ThePerspectiveEntriesAreARadioGroup_WithTheActiveOneCheckedAndDrawn_AndAClickNeverTogglesIt` — the menu shown in a window, View opened, invoked through the peer, pumped; asserts `IsChecked` kept, zero `Unchecked`, `IsSubmenuOpen == false`, `Mouse.Captured == null`, `ToggleState` On/Off per row | WPF's own click toggle (a checkable item); the glyph missing; the Toggle pattern absent; a click path that skips `PreviewClick` and leaves the popup in menu mode | **M4** (never checked); **M20** (glyph never drawn); **M21** (the peer deleted → no Toggle pattern); **M22** (made checkable → toggled); **M23** (the round-2 `OnClick` override restored → the View menu stays open) | Verified | the check is drawn in the template's icon column because `App.xaml`'s menu template renders no check (outside the lane); the runtime shape (a real mouse click on a real popup) is SH-2's attended P-1 |
| 13 | No announced gesture string names more than one command (US-C10 b2) | `NoAnnouncedGestureStringNamesMoreThanOneCommand` | any collision, incl. between a derived row and the catalog | **seen red with the four collisions listed**; **M13** (`Ctrl+K, G` restored) | Verified | — |
| 14 | Activating a named perspective raises `ModeChanged` once; activating the active one is a no-op — no event, no log line (US-C1) | `ExplorerModeTests.Set_ActivatesANamedPerspective_AndActivatingTheActiveOneIsANoOp`; `EveryOpeningCommandPassesThroughTheSeamTests.Set_ToTheCurrentMode_WritesNothing`, `Set_ToADifferentMode_WritesOneShellModeLineNamingTheTrigger` (ids `explore`/`coding`) | the no-op guard removed | **M7** | Verified | Architecture shows host A until SH-2's second host — said plainly by the window's announcement |
| 15 | **A derived opener in a host perspective does not switch the perspective; from Explore the initial host returns (INV-0009)** — one rule, `MainWindow.OnDocumentOpening(mode)`, that the window, the replay probe and the pairing test all call | `ADerivedOpenerInAHostPerspective_DoesNotSwitchThePerspective_ButFromExploreTheHostReturns` (shell + presenter + the product's rule); `TheWindowAndTheReplay_WireTheSeamToTheWorkbenchBody` (asserts both lines wire `OnDocumentOpening(`) | the guard removed (every open returned to Coding — the Test Architect's and the reviewer's Blocker) | **M15** (the window's inline guard removed, round 2 → the scan red); **M15b** (the guard removed in the then-copied handler → the pairing test red); **M24** (the guard removed in the product's static → the pairing test red) | Verified | — (the tested rule is the product's, the Test Architect's condition A) |
| 16 | A "Show" entry activates the one open surface of its kind (even when another tab is active in the model and the view), else opens it; a "New" entry adds every time — through the real shell (E11) | `WorkbenchShellTests.AShowEntryOpensTheKindOnce_ThenFocusesIt_AndANewEntryAddsEveryTime` — daydreams a LATER tab, the viewer activated in model and view first | Show never activating; Show adding twice | **M10** (adds twice); **M16** (never activates) — M16 survived the first form of the test because a view→model reconcile lands on tab 0 (finding, below); the rewritten test kills it | Verified | placement of a shown kind follows `DocumentPlacementPolicy` (unchanged) |
| 17 | Entry verbs are in File in every perspective; every catalog command has a placement in ≥ 1 perspective (US-C11; US-C4) | `MainMenuTests.TheMenuCoversEveryCatalogCommand` (re-scoped); `TheFrontDoorIsInTheFileMenuTests.FileNewTerminalSessionStillProducesTodaysTerminalSession`; `HarnessSessionCommandsAreDerivedTests.EveryLaunchableProfileHasExactlyOneCommand`; `Phase3SurfacingTests.EveryCommandNamesAMenuTheShellRenders` | `_Terminal` surviving; a command with no menu anywhere | **seen red against the old catalog** (`_File` vs `_Terminal` ×3; `_Terminal` undeclared) | Verified | — |
| 18 | Derived ids parse back to their row (round trip); anything else — incl. null — is rejected without throwing; a stale id is answered honestly; the node menus' kinds are rows | `TryParseOpener_RoundTripsEveryDerivedOpener`; `TryParseOpener_RejectsAnythingElse_AndNeverThrows`; `AStaleDerivedId_IsAnsweredHonestly_AndTheNodeMenusKindsAreRows` | a prefix mismatch; an NRE on null (was one before round 2) | n/a — laws added at the reviewer's request; the null case was red (NRE) before the fix | Verified | — |
| 19 | Every catalog command still executes and announces; every palette row executes | `WorkbenchControllerTests.EveryCatalogCommand_Announces`; `CommandPaletteTests.EveryListedCommand_ActuallyExecutes` (existing, walk the new sets) | a `perspective.*` or `surface.*` id with no case | n/a (existing controls, kept green) | Verified | — |
| 20 | No regression | Core **2,262 / 0**, App **673 / 0** (full suites, rebuilt from restored sources after the mutation loops) | any broken contract | n/a | Verified | `tools/expected-test-counts.json` is recounted by the conductor at the join (never `--update` here) |

## Mutation-sense record

Every mutation was applied to the product (or, for M15b, to the test's then-copy of the product rule), built, run under a filter, and **reverted** by the loop (`scratchpad/mutate.py`, `mutate2.py`, `mutate3.py`; results `mutation-results-sh1.json`, `-round2.json`, `-round2b.json`, `-round2c.json`, `-round3.json`; the table is generated from them). **Lesson from the loop itself:** a restored source is not a rebuilt binary — the full suites were run once against a stale mutant and went red until rebuilt (recorded as a DC-023 recurrence; the round-3 loop rebuilds after its last restore).

| Mutation | Reds observed |
|---|---|
| M1 derived rows dropped from `PerspectiveMenu.For` | `ATestTimeKindRow…`, `NoAnnouncedGestureString…`, `ThePaletteRowsEqual…`, `ThePaletteSpeaks…`, `ThePerspectiveItemsShow…`, `TheRenderedMenuEquals…`, `ThePalette_ListsExactlyTheMenusCommands` |
| M2 `daydreams` admits `[]` | `EveryKindRowAdmits…`, `Resolve_Routes…`, `TheAllowListsEqual…`, `ThePaletteSpeaks…`, `ThePerspectiveItemsShow…`, `TheRenderedMenuEquals…` |
| M3 `codeviewer` leaves Architecture | `Resolve_Routes…`, `TheAllowListsEqual…`, `TheRenderedMenuEquals…` |
| M4 radio never checked | `ThePerspectiveEntriesAreARadioGroup…` |
| M5 menu prints the unbound chord (old behaviour) | `EveryMenuItemShowsItsKeyboardChord`, `ThePerspectiveItemsShow…` |
| M6 perspective gestures unbound | `EveryPerspectiveCommandIsBound…`, `ThePaletteSpeaks…`, `ThePerspectiveItemsShow…` |
| M7 activating the active perspective not a no-op | `Set_ActivatesANamedPerspective…`, `Set_ToTheCurrentMode_WritesNothing` |
| M8 routing order reversed | `Resolve_Routes…`, `TheRoutingOrderIsArchitectureThenCoding` |
| M9 a fourth row (`tests`) | `TheCommandLookupReturnsNull…`, `ThePerspectiveGesturesAre…`, `TheSetHasExactlyThreeRows…` |
| M10 Show always adds | `AShowEntryOpensTheKindOnce…` |
| M11 palette speaks the unbound chord (old behaviour) | `ThePaletteSpeaks…` |
| M12 a `Verb` row names a command the catalog lacks | `EveryEntryVerbRowNames…` |
| M13 `focusCanvas` back to `Ctrl+K, G` | `NoAnnouncedGestureString…` |
| M14 the radio item toggles on click (`OnClick` override deleted) | `ThePerspectiveEntriesAreARadioGroup…` |
| M15 the window's `DocumentOpening` guard removed | `TheWindowAndTheReplay_WireTheSeamToTheWorkbenchBody` |
| M15b the guard removed in the pairing test's handler | `ADerivedOpenerInAHostPerspective…` |
| M16 Show never activates the open surface | `AShowEntryOpensTheKindOnce…` (after the test was rewritten; the first form survived it) |
| M17 Explore given a `DockHost` body | `TheBodiesAre…`, `TheRenderedMenuEquals…` |
| M18 a perspective filtered out of the catalog derivation | `EveryRowHasACatalogCommand…` |
| M19 a new unbound chord literal in operator copy | `NoOperatorCopyNamesAnUnboundChord` |
| M20 the check glyph never drawn | `ThePerspectiveEntriesAreARadioGroup…` |
| M21 the Toggle-exposing peer deleted | `ThePerspectiveEntriesAreARadioGroup…` |
| M22 the item made checkable (WPF toggles it) | `ThePerspectiveEntriesAreARadioGroup…` |
| M23 the round-2 `OnClick` override restored (skips `PreviewClick`; the View menu stays open) | `ThePerspectiveEntriesAreARadioGroup…` |
| M24 the product rule `OnDocumentOpening` loses its guard | `ADerivedOpenerInAHostPerspective…` |

## Boundary set

| Boundary | Covered by |
|---|---|
| empty allow-list | claim 6 (build test) |
| a full-window perspective named as an admitter | claim 6 |
| a `Verb` row naming a missing or non-Global command | claim 6 |
| a kind no perspective admits (`Resolve`) | claim 5 (`null`, never a default) |
| the active perspective activated again | claim 14 (no-op; announced "already showing" by the window) |
| a derived id with no kind, or null | claim 18 |
| a stale derived id naming a kind that is not a row | claim 18 ("There is no '<kind>' surface in this build.") |
| an unknown command id | `WorkbenchControllerTests.UnknownCommand_IsNotHandled` (existing) |
| a Show with no pane to open in | `OpenKind` → `OpenReferenceDocument`'s "There is no pane for <title>…" (existing path; announced) |
| a derived opener from a host perspective vs from the full-window body | claim 15 |

## Failure modes addressed

| Failure mode | Handled in code by | Proven by |
|---|---|---|
| A kind unreachable by omission (the `contexts`/`joins`/`daydreams` defect) | required `Perspectives` and `Entry` columns, no defaults, `Entry` a closed type | claims 6, 7 |
| A menu offering what the body cannot do (PS-M3) | `CommandScope` evaluated per perspective; absent, never disabled; `focusCanvas` scoped to a docked canvas | claim 8 |
| A keystroke promised but not bound (the fifth page-one fact) | bound-only rendering from `KeyGestures.For`; the copy scan | claim 11 |
| Two surfaces disagreeing about the active perspective | one `PerspectiveMenu` model handed to bar and palette; the presenter rebuilds both on `ModeChanged` | claim 10 (rendered vs palette); the window hand-off Inferred |
| A no-op switch writing a log line or raising an event | the guard in `Set` | claim 14 |
| A click un-checking the active radio item | `PerspectiveMenuItem.OnClick` never toggles | claim 12 |
| An opener in Architecture switching the shell to Coding | the body guard on `DocumentOpening` | claim 15 |
| A perspective command with no window seam; a derived opener with no shell seam | announces "…is not available in this build." | `EveryCatalogCommand_Announces`; claim 18 |

## Instrumentation (IO2)

| Operator question | Emitting source | Observed |
|---|---|---|
| which perspective is showing, and what asked for it? | `shell.mode` line: `mode`/`from` = row ids, `trigger` = the command id / seam | yes — `Set_ToADifferentMode_WritesOneShellModeLineNamingTheTrigger` |
| did a derived open land, where? | `LayoutMutation("open-<kind>", mode, …)` in `OpenReferenceDocument` (existing) | existing |
| how long does a switch take; did it fail? | **not recorded** — `duration_ms`, `outcome`, `error_code` are US-C12's, SH-2's row (the second host is where a switch can fail) | gap, named |

## Testing Strategy directives applied

T1 (pure functions: `PerspectiveMenu`, `PerspectiveSet`) → unit tests with literal oracles; T2 (a parser over an unbounded string domain — `TryParseOpener`) → D2 laws: the round trip over every derived row, never-throws incl. null, and the exhaustive `Resolve` law over 54 cells; T3 (structural — Core gained `Perspectives.cs`, the App row type embeds a Core record) → D3, the literal §A7 and §B3 tables as the oracles; UI → rendered through the real builder and palette on STA (E11) and the shell + presenter pairing; scan-shaped guards state root/recursion/token/allowlist (DC-118) — the gesture collector, the copy scan, the seam scan's guard token; D0 hygiene (no shared state; every test names its falsifier); the US-C4 mutation test and a 21-mutation sense pass over the implementation across two rounds. No AI-integrated code.

## Reviews (Adversary Mode, read-only; two rounds, cap 2)

| Lens | Round 1 | Round 2 | After the cap |
|---|---|---|---|
| **Test Architect (hard)** | **VETO** — 1 Blocker (Architecture's derived openers switched the shell to Coding through the `DocumentOpening` handler; no test could see it), 3 Majors (two "red observed" cells unsupported by the mutation record; the radio click guard had no failing input; the Show-focuses half had no falsifier), 5 Minors (E11 honesty, `Resolve` uncalled, `Execute(null)` NRE, D2 laws missing, boundaries), 1 Nit | **PASS-WITH-CONDITIONS** — Blocker and Majors cleared (M15/M15b, M17/M18, M14/M20, M16); conditions **A** (the tested rule should be the product's, not a copy — the handler lifted into a static), **B** (the `OnClick` bypass is attended-only), **C** (Explore's oracle lost "Focus graph canvas" — a §B3 rule-2 deviation, recorded), **D** (explain the suite-count drop before the join recount) | **A done** (`MainWindow.OnDocumentOpening`; M24); **B superseded** (the override is gone; the menu-closes assertion and M23 cover it headlessly; the real popup stays P-1); **C recorded**; **D answered**: the draft's numbers (2,268 / 680) were typed before any run — a defect of this slice, registered as a new class; the measured counts never fell (Core 2,262 → 2,262; App 666 → 673) |
| **UX & Accessibility (hard, a11y)** | **VETO** — 1 Blocker (the checked state never rendered: the app's menu template has no check glyph), 8 Majors (click-restores-check unsound; rail still named "Explorer mode"; a destination with no way back; the same opener-switches-to-Coding defect; "Focus graph canvas" offered in Explore where its seam cannot work; two unbound chords in operator copy; the menu template's 1.31:1 keyboard highlight; Architecture announced as a perspective it is not yet), 3 Minors, 1 Nit | **VETO on one NEW Blocker** the round-1 fix introduced (the `OnClick` override skipped `PreviewClick`, the event the menu closes on — the popup stayed in menu mode; WCAG 2.1.2); everything else converged. Conditions for the next slice: the template's highlight ring (SH-2, `DESIGN.md` P-5), `ImportantAll` + focus on switch (SH-2 attended), Escape/previous-perspective (SH-2), `DiagnosticsSurface.cs:90` (its owner) | **The prescribed fix applied after the cap** (cap 2 fired — a defect signal, recorded for the conductor): no override, `IsCheckable = false`, the Toggle pattern exposed by a custom `MenuItemAutomationPeer`; the test shows the menu, opens View, invokes through the peer and asserts the popup closes and capture is released; M21–M23 red. **Not re-reviewed by the lens** — the conductor decides whether the cap's verdict stands as PASS-WITH-CONDITIONS on this fix, as the reviewer wrote it would |
| **Simplifier (soft) + Patterns Expert** | **SOFT VETO** — 1 Major (the prompt kind's placement arm: kind-keyed code beside the row set, unmarked); 8 Minors/Nits (`SurfaceEntry` as two nullables → the closed record hierarchy; `Offers`/`ById`/`Search(string)` test-only; one seam not two; `?? "_View"`; a doubled reconcile; a doubled summary). Patterns: the registry shape matches ADR-0030 as decided; `Offered` is an interpreter over rule shapes, not a Ruling-22 arm | **CLEARED** — the arm marked (`simplify:` ceiling + trigger) and shrunk through `OpenReferenceDocument(intoStackId)`; every listed delete applied; `GraphCanvas` gone. Remaining: three Nits kept with reasons; `docs/api` stale until regenerated (done at close) | — |

## Deviations and findings (not acted on here)

- **Spec §B3 table:** three literal deviations (above). Findings for the spec: the casing/ellipsis, and rule 2's "Focus graph canvas iff the body holds a graph" — Explore's graph is not the seam's target.
- **Rename blast radius beyond the plan's file list:** `ShellViewMode`'s references in `tests/AiDe.App.ComposerProbe/Program.SessionRender.cs` (three `Set` calls, the guarded `DocumentOpening` line, one `Execute("workbench.newCodeViewer")`, two `mode={mode.Mode}` prints) and `tests/AiDe.App.Tests/Sessions/ASessionDocumentIsShownWhereTheOperatorIsTests.cs` (the replay's `mode=Explorer`/`mode=Workbench` stdout assertions). Both edited (claimed on the record) because the solution does not build otherwise and the rename is this commit's by Ruling 50. Two class entries appended to the register with ids left for the join.
- **`DiagnosticsSurface.cs:90`** — "Run Re-index (Ctrl+K, I)…" names an unbound chord (US-C10 b3); outside the Shell lane's paths; frozen in the copy scan at count 1; a one-line fix for its owner.
- **`App.xaml`'s menu template** (outside the lane): no check glyph (worked around in the icon column) and a keyboard highlight that is a 1.31:1 ground step with no `{colors.focus}` ring — DESIGN.md's menu state table calls for the ring (the review `ui-perspective-shell` A-16's product half). Finding for SH-2, which owns `DESIGN.md`'s menu brushes (P-5).
- **`ReconcileViewIntoModel` (`TryMapByPosition`) resets a stack's active tab to index 0** — measured while writing claim 16's falsifier: with the code viewer selected in the view, a reconcile alone made tab 0 active. Pre-existing zone machinery (the INV-0006 class); not this slice's. Finding for SH-2/SH-3.
- **`DocumentPlacementPolicy.DocumentKinds`** — a two-kind hand list ("classdiagram", "codeviewer") that decides "is this a document stack" for the fifteen kinds the derivation now routes through the policy (the Simplifier's finding). The prompt kind's placement stays a marked `simplify:` arm in `OpenKind`; both retire together when a `Placement` column lands on the row.
- **Interim rail:** the Explore rail button is a destination; every Explore answer names the way back (the Coding row's gesture); Escape-from-Explore and the previous-perspective slot (US-C1 b3/b4) are SH-2's. Architecture activates and shows host A, and says so. The rail's "(active)" suffix is state-in-name — SH-2's rail exposes a real selected state.
- **The check glyph is builder-drawn** because `App.xaml`'s template has no `IsChecked` trigger: when SH-2 gives the template one, `MainMenuBuilder.CheckGlyph` must go in the same commit or two checks render.
- **The review loop's cap fired.** Two rounds were the contract; the UX & Accessibility lens raised a new Blocker in round 2 (introduced by the round-1 fix) and prescribed the fix. The fix was applied and proven (M21–M23) but not re-reviewed by the lens — the conductor confirms the lens's own "on its fix, PASS-WITH-CONDITIONS" or convenes a third round.
- **DESIGN.md "The switch, and what it says"** — `ImportantAll` announcement processing and moving focus into the new body are unimplemented (pre-existing for the toggle); SH-2's attended run (P-1/P-7).
- **US-C3 routing** — `Resolve` is landed and law-tested but has no product caller until SH-2's routed-open transaction; the spec falsifier is reproducible until then.
