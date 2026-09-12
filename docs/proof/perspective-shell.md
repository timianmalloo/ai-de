---
id: proof-perspective-shell
title: "Proof Pack — The second docking host, the PerspectiveShell presenter and router, one layout slot per host, and the rail's three destinations (SH-2, ADR-0031/0032)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [proof-pack, addendum-c, perspective, docking, layout-persistence, rail, shell-lane]
links:
  - { to: adr-0031-second-docking-host, rel: tested-by }
  - { to: adr-0032-perspective-layout-slots, rel: tested-by }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: coordination-addendum-cd, rel: relates-to }
  - { to: note-sh2-presenter-router-and-slots, rel: relates-to }
  - { to: proof-perspective-registry, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  SH-2 of the Shell lane: the DockHost unit composed twice under the PerspectiveShell presenter and
  router; each host's layout service guarded by its perspective's allow-list at open, restore and
  reset; one zone-envelope slot per host with drop-with-report, a once-only pre-perspective backup,
  a refused file always preserved, and a golden rollback round-trip; the rail's three destinations
  with manual activation and one writer of the selection. The plan's reds observed (compile-red,
  then eleven mutations); three hard vetoes raised, two cleared in two rounds and the third's
  post-cap fix applied as prescribed for the conductor to confirm; the DC-135 ratio narrowed from
  65:29 to 65:38. Suite counts pasted from the runner at close.
---

# Proof Pack — The second host, the presenter, the slots and the rail (SH-2)

- **Change:** branch `lane/shell-sh2` from `main` @ `b0e092b5`; worktree `C:\Projects\ai-de-lane-shell-sh2`
- **Spec / design:** `docs/specs/addendum-c-perspectives.md` (US-C1, C2, C3, C5, C9, C10, C11, C12; §B4, §C4, §C5, §C6, §A10) · `docs/adr/0031-second-docking-host.md` · `docs/adr/0032-perspective-layout-slots.md` · `docs/adr/0017-primary-view-mode.md` (amendment clauses 1–5) · `DESIGN.md` PS-R2/R4/R5, PS-T1/T3, PS-C4 · `spikes/second-dock-host-unparent/RESULT.md` · the slice's decisions: `docs/notes/sh2-presenter-router-and-slots.md`
- **Tier:** T2 · **Fan-out cap:** 3 (persona reviews, read-only; two rounds, cap 2)
- **Author / date:** session `sh-2` (Claude Opus, `/implement`), 2026-09-12

## What landed (the E7 surface list, ticked)

| Surface | Reached | Where |
|---|---|---|
| Model (the aggregate's invariant as data) | yes | `src/AiDe.Core/Workbench/SurfaceAdmission.cs` — `SurfaceAdmission(perspective, rules)`: `Admits`, `IsOneInstance`, `AdmittedBy` (first in `RoutingOrder`), `Filter(layout)` → `AdmissionFilter(Layout, Dropped)`; `KindRule`, `DroppedSurface(Surface, Reason, AdmittedBy)`, `DropReason`; `Unrestricted` (every pre-existing caller); the two stable codes `AIDE-LAYOUT-KIND-NOT-ADMITTED`, `AIDE-LAYOUT-ONE-INSTANCE` |
| Store | yes | `src/AiDe.Core/Workbench/ZoneLayoutStore.cs` — `Read` → `ZoneLoadResult(Layout, Refusal ∈ None · NoFile · NewerSchema · Corrupt)` (`Load` retired); `Save(layout, backupPath?)` = temp file + `File.Replace(temp, dest, backup)` / `File.Move(overwrite)`; `TempPath`; schema below 1 → Corrupt, above → NewerSchema; DTOs unchanged (schema 1) |
| Service | yes | `src/AiDe.Core/Workbench/ZoneBackedLayoutService.cs` — `(SurfaceAdmission admission, initial?)`; `Admission`; `DefaultLayout()` = `Default()` filtered (`simplify:` → SH-3's `Default(perspective)`); `DefaultDropped`; `RestoreZones` → `ZoneRestoreReport(Dropped, DefaultApplied)`; `Apply(AddSurface)` refuses an inadmissible kind and a second one-instance surface; `Restore(Layout)` and `ResetToDefault` filtered; `TryMapByPosition` carries the view's active tab per zone (INV-0006's class, §8 of the note) |
| Persistence (the slots) | yes | `src/AiDe.App/Workbench/LayoutPersistence.cs` — `SlotPathFor(layoutPath, perspective)` (Coding `<name>.zones.json` grandfathered; Architecture `<name>.architecture.zones.json`; a full-window perspective throws); `Perspective`; `Restore()` → the same `RestoreResult` (drops in `MissingSurfaces` by caption+kind, `ErrorCode` = `PartialRestore` / `VersionUnsupported` / `Unreadable`, `WasDefaulted`); `LastRestoreDropped` (typed); `.pre-perspectives.bak` owed once after a dropping restore, `.bak` owed after every refusal, both written by the save that rewrites the slot; `SaveNow` under a lock, `LastSaveFailure` + `SaveFailed` (never swallowed) |
| The unit, composed twice | yes | `src/AiDe.App/Workbench/DockHost.cs` — `DockHost(Row, Service, Manager, Adapter, Controller, Rails)` + `Root`, `Persistence`, `Create(row, content, announcer, expandZone)`, `AdmissionFor(row)`, `Dispose`; `WorkbenchShell.cs` — `Coding`, `Architecture`, `Hosts`, `CommandRouter`/`Execute`; `Service`/`Manager`/`Adapter`/`Controller`/`WorkbenchRoot` are host A's (every existing caller unchanged; a host's `Persistence` is read on the host); the binders walk both hosts (`AllHostSurfaces`, `SurfaceContents<T>`); per-host `OpenSurfaceRequested`, reconcile, focus tracking, persistence, restore; `OpenKind(from, kind, show)` / `OpenReferenceDocument(host, …)` resolve the host (`ResolveHost`) and announce where they landed; `DocumentOpening : Action<Perspective>` raised after an APPLIED add, before the render |
| Presenter + router | yes | `src/AiDe.App/Workbench/PerspectiveShell.cs` (rename of `ShellModeController`, Ruling 50) — `Active`, `Previous`, `HostFor`, `ActiveHost`, `ExplorerSurface`, `Activate(p, trigger)` (no-op for the active; a body that fails to build reports and changes nothing), `Escape()` (Explore only), `OnDocumentOpening(host)`, `Execute(id)` (table-driven: perspective row · opener/`Admits` → `Resolve` · `DockHost` → active host or refused `AIDE-PERSPECTIVE-NO-HOST` · else the initial host), `Changed`, `BodyFailed`, `EntryFocus`; the stop edge armed per switch and detached when superseded |
| Controller | yes | `src/AiDe.App/Workbench/WorkbenchController.cs` — `Bind(host, execute?)` routes bound gestures through the router; `CommandPalette` and `MainMenuBuilder.Build` gained `Func<string,bool>` overloads (the controller overloads delegate) |
| The rail | yes | `src/AiDe.App/Workbench/PerspectiveRail.cs` (new) + `MainWindow.xaml` — `PerspectiveRail : ListBox` with manual activation, `Reflect` the one writer, `ActivationRequested`, `ShowFailure`; UIA `Tab`/`TabItem` peers, `SelectionItem.Select()` = an activation request, `IsSelectionRequired`; `PerspectiveRailItem` (constant name, tooltip from the bound gesture, `Failure`/`HasFailure`, 44 × 44 floor, the tab index puts the selected destination first — every destination stays a tab stop); the item style on tokens (rest · hover · selected · focus (outer ring) · error); `IconCoding`/`IconArchitecture`; no `TabNavigation=Once` on the container |
| Window | yes | `src/AiDe.App/MainWindow.xaml.cs` — composes the presenter over `Shell.Hosts`, sets `Shell.CommandRouter`, wires `Rail.ActivationRequested → Shell.Execute`, `Changed → Rail.Reflect` + `RebuildMenu` + title + status strip, `BodyFailed → Rail.ShowFailure`, `EntryFocusFor`, Escape on the bubbling `KeyDown` (not inside the canvas), `Shell.DocumentOpening += host => _perspectives.OnDocumentOpening(host)` (the one line the replay wires too), `TitleFor(workspace, perspective)` with `ReflectPerspective` its one writer; both hosts themed |
| Instrumentation | yes | `src/AiDe.App/Workbench/WorkbenchDiagnostics.cs` — `ShellMode(from, to, trigger, first_entry, outcome, error_code)`; `ShellModeShown(to, trigger, duration_ms)`; `LayoutRestoreReport(perspective, placement, dropped, error_code)` incl. `default-filtered` |
| Design | yes | `DESIGN.md` — PS-R5; PS-C4's three declared roles (`--inferred`, `--verified`, `--border-strong`); four recorded deviations; `DockRoundedTabs.xaml` P-2 (selected-inactive on `surface` + the 2px `text-muted` edge) |
| Compute readers | yes | the rail (`Reflect`), the title (`TitleFor`), the status strip, the menu/palette rebuild, the `shell.mode`/`shell.mode.shown`/`layout.restore` lines, the announcer (drop report, refusal, routed open, no-host refusal, failed body), `RestoreResult.MissingSurfaces`/`ErrorCode`/`WasDefaulted` |

## Reds → green (names; before → after)

| Red | Observed | Green |
|---|---|---|
| ADR-0032 tests 1–7 (`PerspectiveLayoutSlotTests`: `APreAddendumCFile_RestoresIntoCoding…`, `AnEnvelopeWithNothingCodingAdmits…`, `TwoDiagnosticsPanesWithDistinctIds…`, `EachHostSlot_IsWrittenAndRestoredIndependently`, `DroppedSurfaces_AreNotCarriedIntoTheArchitectureSlot`, `ThePostAdrCodingFile_DeserialisesWithTheFrozenSchema1Dto` + `TheSchema1DtoConstructors…` + `TheFirstSaveAfterADroppingRestore…` + `WithTheBackupTargetUnwritable…`, `ARefusedFile_IsReportedWithItsReason_AndBackedUp…` ×3) | compile-red (no `SlotPathFor`, `LastRestoreDropped`, `SaveFailed`, `DockHost`); then **M1** (`RestoreZones` unfiltered): 5 of 18 red — tests 1, 2, 3, 5, 6 | 18/18, then 24/24 with the round-2 additions |
| ADR-0031 test 1 — identity across three bodies (`PerspectiveShellTests.CyclingAllThreeBodies…`) + test 4's switch clause (`CyclingThePerspectives_LeavesEveryHostsArrangementUntouched`) | compile-red (no `PerspectiveShell`, `DockHost`) | green |
| ADR-0031 test 3 — routing (`Execute_RoutesHostCommandsToTheActiveHost…`) | **M2** (a host command routed to host A): 1 of 9 red | green |
| The allow-list refusal at the service (`SurfaceAdmissionTests` ×10; `ZoneLayoutStoreTests` +8) | compile-red; **M1** | 21/21 |
| The rail's manual activation (`PerspectiveRailTests.Down_MovesFocusWithoutActivating…`) | **M3** (activate on Down): 1 of 6 red | green |
| The rail is reachable by Tab (`Tab_EntersTheRailOnTheSelectedDestination…`; `TheRailsContainer_IsNotAOneStopGroup_InTheWindowsXaml`) | **M4** (`TabNavigation="Once"` restored): 1 red | green |
| The switch event's shape (`EverySwitch_WritesOneShellModeLine_WithItsFullShape`; `EveryRetainedSwitch_IsShownOnce_WithAMeasuredDuration` incl. the rapid sequence) | **M5** (the stale stop edge left armed): 1 red | green |
| Document first, then the switch (`EveryAddSurfaceInTheShell_IsFollowedByTheSeamOnceApplied_BeforeTheRender`; the create-failure half of `PerspectiveMenuTests.ADerivedOpenerInAHostPerspective…`) | the first cut raised the seam BEFORE the add — the Test Architect's blocker; **M6** (one site switched first): 2 red | green |
| A reconcile keeps the view's active tab (`ZoneBackedLayoutServiceTests.ReconcileFromView_KeepsTheViewsActiveTabInEveryZone`) | the create-failure test went red on its first run for THIS reason (`@1` → `@0`, SH-1's seam note measured); **M7** (`ActiveIndex` dropped): 1 red | green |
| A refused file preserved when a `.bak` exists (`ARefusedFile_IsPreservedEvenWhenAnOlderBackupExists…`) | the D&P Architect's probe ran the first cut: refused bytes existed nowhere (their blocker) | green |
| `ANewSessionCreatedWhileExplorerIsTheBodyIsShown`, `AReopenedSessionIsShownAndItsComposerIsBound` (+4 siblings, the replay probe) | went red once mid-slice (exit 33: the replay compared the pre-perspective arrangement, which Coding now filters) — a probe-side re-scope, the product's document rendered on every run | green in runs 2 and 3 |
| Every existing shell/persistence/drag test | 12 went red at the extraction for the planned reason (the graph lives in host B) and were re-scoped to the admitting host; none weakened (the Test Architect judged each) | green |

**Mutation-sense record** — M1 filter disabled → 5 red · M2 host command to host A → 1 red · M3 activate on Down → 1 red · M4 `Once` restored → 1 red · M5 stale edge armed → 1 red · M6 switch-first → 2 red · M7 active index dropped → 1 red · M8 (round 1, the D&P probe) refused bytes lost → observed by the reviewer's scratch program · M9 the Show branch reading the projection → 1 red (`AShowEntry_ForAOneInstanceKindHeldInACollapsedZone…`) · M10 the `terminal.new` seam ungated (the Test Architect's named mutation) → 2 red (the pairing test, the scan) · M11 the UX & Accessibility reviewer's NEW-1 falsifier (Tab after exploring a non-selected destination) → red BEFORE the tab-index fix, green after. Every mutation reverted; `git diff` shows only the intended change.

## The spike's property, re-measured in the product

`spikes/second-dock-host-unparent` (exit 0) established the presenter swap keeps a second host's `CoreWebView2` and a raw HWND across A → B → A → B ×3 and B → Explore → B. In the product the same swap is `PerspectiveShell.Activate` over `DockHost.Root`s (`_body.Content = body`, never a rebuild); the headless half is `CyclingAllThreeBodies_ShowsTheSameInstances_AndBuildsExploreOnce` (reference identity of both hosts' roots and the Explore surface across the cycle; the Explore factory called once) and `CyclingThePerspectives_LeavesEveryHostsArrangementUntouched`. The runtime half — `CoreWebView2` identity, page state and the HWND for a class diagram in host B, a live WebView2 in the Explore body cycled with host B's, and a WebView2 in a non-selected tab — is **P-4, attended, RUN-PENDING** (below); the product's once-gate is `WebSurfaceHost` per surface (unchanged; both hosts build through the one factory, so host B's web surfaces take it by construction — `TheWebSurfacesInitialiseOnceAcrossReparentsTests` still passes with the presenter's self-removing `Loaded` hook on its allow-list).

## The slot migration — before / after

| | Before (main @ b0e092b5) | After |
|---|---|---|
| Files | `<layout>.zones.json` (one host) | `<layout>.zones.json` (Coding, byte-compatible at read — the golden `tests/AiDe.App.Tests/Fixtures/pre-perspectives.zones.json` is a schema-1 file the store wrote with today's default plus a class diagram) · `<layout>.architecture.zones.json` (Architecture) |
| A pre-C file | restored whole into the one host | read into the Coding slot; `canvas`, `view` ×2, `inspector`, `contexts`, `joins`, `classdiagram` dropped **with a report** (7 named by caption and kind, Architecture named as the admitting perspective; `layout.restore` carries `dropped_count` 7 and the kinds); the Architecture file stays absent; Architecture opens with its default |
| Zero admissible | n/a | the perspective's default, and the report says so |
| One-instance duplicates | n/a | the first in zone order (Left · Right · Bottom · Center · floating) kept, the second reported |
| Rewrite | in place (`File.WriteAllText`) | temp + `File.Replace`/`File.Move`; the first save after a dropping restore passes `.pre-perspectives.bak` (once); a save after a refusal passes `.bak` (every time, replacing an older copy); a backup that cannot be written leaves the original untouched and reports |
| A refused file | `null`, silent | `NewerSchema` / `Corrupt` with the reason in `RestoreResult.ErrorCode` and the announcement; preserved at `.bak` before the slot is rewritten |
| Rollback | untested | the post-ADR Coding file deserialises with the frozen schema-1 DTO into the same arrangement (per zone: tabs, active, extent, collapsed; floating count); the DTO constructors carry exactly their frozen parameters and wire names |

## The rail's activation model, as landed

Three destinations (`PerspectiveSet.All`) as a `ListBox`-derived single-selection group: Up/Down/Home/End/PageUp/PageDown move focus only; Enter, Space or a left-click raise `ActivationRequested`; the window runs the perspective's catalog command; the presenter switches; `Changed` → `Rail.Reflect`. `Reflect` is the ONE writer of the selection — every other writer (a page key, a right-click, typeahead, a UIA `Select()`, a programmatic `SelectedItem`) is reverted and turned into an activation request. Tab enters on the selected destination and leaves after one stop; the container is not a `Once` group. To UIA: a `Tab` list (`IsSelectionRequired`) of `TabItem`s with the real selected state; names constant (`"<Title> perspective"`); tooltips from the bound gesture's display string; the error state via `ItemStatus`; a 44 × 44 floor on the control. The spec's *opening* state is not built (a synchronous, retained switch has no such interval — recorded as a deviation for the conductor).

## The DC-135 ratio watch

| | `new LayoutService(` under `tests/` | `new ZoneBackedLayoutService(` under `tests/` |
|---|---|---|
| Register (Ruling 47) | 66 | 8 |
| `main` @ b0e092b5 | 65 | 29 |
| after SH-2 | **65** | **38** |

Not widened: no new test constructs the tree service; every new host test constructs the zone-backed service (`PerspectiveLayoutSlotTests` through the target-typed `new(DockHost.AdmissionFor(row))`, which the token does not count).

## The census

`ShellContrastCensusTests` boots the real App and walks what it composes. It adds every kind to `shell.Service` — host A's — so under ADR-0031 the seven Architecture kinds are refused there and their rows are not measured; the rail's three destinations are walked in their rest state only (the census does not switch perspective). **The census therefore covers host A and the rail's rest state; host B and the rail's selected/focused states are not in its population** — DC-135 recurrence 3 in the register, and a seam request to X-1 (the probe's owner). Its result at close is in the gate table.

## Claims & evidence

| # | Claim | Evidence | Source | Oracle | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|---|
| 1 | Two hosts are composed as one unit; host A is today's instances | `WorkbenchShellTests.Shell_ComposesTheWorkbenchWithEverySurfaceFromTheDefaultLayout_AcrossBothHosts`; every pre-existing shell test green through `Shell.Service/Adapter/Manager/Controller` | `DockHost.cs`, `WorkbenchShell.cs` ctor | Coding holds no `canvas`; Architecture holds no `terminal` | re-scoped reds at the extraction | Verified | host B's default is interim (§4 of the note) |
| 2 | Retain, never rebuild, across three bodies | `CyclingAllThreeBodies…`, `CyclingThePerspectives_LeavesEveryHostsArrangementUntouched` | `PerspectiveShell.Activate` | `Assert.Same` on the roots; shapes unchanged | compile-red; M2's sibling | Verified (headless) / P-4 for the HWND | P-4 attended |
| 3 | The router sends a host command to the active host, an opener to the admitting host, an entry verb to host A, and refuses a host command in Explore | `Execute_RoutesHostCommandsToTheActiveHost…`, `Execute_OfAHostCommandInExplore_IsRefused…`, the pairing test | `PerspectiveShell.Execute` | recording announcers per host | M2 | Verified | none |
| 4 | Document first, then the switch; a refused add swaps nothing | the scan + the create-failure half of the pairing test | `WorkbenchShell` add sites | Active unchanged, shape unchanged, refusal announced | M6; the first cut was red | Verified | none |
| 5 | The allow-list is enforced at open, restore and reset; one-instance at open and restore | `SurfaceAdmissionTests` ×10, `PerspectiveLayoutSlotTests` 1–3 | `ZoneBackedLayoutService.Apply/RestoreZones/DefaultLayout` | refusal codes; drops reported | M1 | Verified | none |
| 6 | One slot per host; independent; not carried over; grandfathered | slot tests 4, 5, `SlotPathFor…` | `LayoutPersistence.SlotPathFor` | file names; A's file lacks B's surface | compile-red | Verified | none |
| 7 | The original is preserved before it is rewritten; a refused file every time; a failure reports and leaves the original | `TheFirstSaveAfterADroppingRestore…`, `WithTheBackupTargetUnwritable…` (+ retry), `ARefusedFile…` ×3, `ARefusedFile_IsPreservedEvenWhenAnOlderBackupExists…`, `ZoneLayoutStoreTests.Save_*` ×3 | `ZoneLayoutStore.Save`, `LayoutPersistence.SaveNow` | byte equality of the backup; the original's bytes after a failed replace | M8 (the D&P probe) | Verified | `ReplaceFile` partial failures; no fsync (relayed) |
| 8 | Rollback: the post-ADR file reads with the frozen schema-1 DTO | `ThePostAdrCodingFile…`, `TheSchema1DtoConstructors…` | the frozen `Frozen.*` records; the golden | per-zone equality; wire names | compile-red | Verified (DTO contract) | a real pre-ADR binary not run |
| 9 | The rail: manual activation; one writer; reachable by Tab; UIA honest | `PerspectiveRailTests` ×9 | `PerspectiveRail.cs`, `MainWindow.xaml` | focus vs selection; the raised requests | M3, M4 | Verified (headless) / P-1 for the pixels | P-1 attended |
| 10 | Every switch is measured: one `shell.mode` line, one `shell.mode.shown` line, never a stale duration | `EverySwitch_WritesOneShellModeLine…`, `EveryRetainedSwitch_IsShownOnce…`, `ABodyThatFailsToBuild…`, `Execute_OfAHostCommandInExplore…` | `WorkbenchDiagnostics.ShellMode/ShellModeShown` | the JSON fields | M5 | Verified | the `mode`+`trigger` join key is not unique across repeated identical switches (SRE lane) |
| 11 | The restore event carries the dropped count and kinds per host | `TheWorkspaceOpenRestore_WritesOneRestoreEventPerHost…` | `WorkbenchDiagnostics.LayoutRestoreReport` | `dropped_count` 7; kinds | added at round 2 (the Test Architect's finding) | Verified | none |
| 12 | INV-0009 stays true through the extraction | the six replay tests | the probe + `MainWindow.OnDocumentOpening` | the composer loaded and mounted | one probe-side red (exit 33) mid-slice | Verified | none |
| 13 | A reconcile keeps the view's active tab | `ReconcileFromView_KeepsTheViewsActiveTabInEveryZone` | `TryMapByPosition` | the zone's `Active` | M7; first seen red in the create-failure test | Verified | none |

## Boundary set

Empty host (four empty zones never: the default applies) · a file with every surface inadmissible · a one-instance kind five times across four zones and a floating stack · a `schemaVersion` 0 / 2 / a corrupt file / a duplicated surface id · a backup target that is a directory · a stale temp file · a `.bak` that already exists · a locked host at an entry verb (a refused add) · a body factory that throws · activating the active perspective · Escape from a host · rapid A → B → A → B within one dispatcher frame · a command id no row knows · a kind no perspective admits · Tab from the New session button · PageDown, right-click, typeahead, UIA `Select()`, programmatic `SelectedItem` on the rail.

## Failure modes addressed

A refused add after a switch (document first) · a body that fails to build (announced with the reason, the rail's error state, active unchanged, retry on activation) · a host command with no host (refused with a code) · a refused file (reported with its reason; preserved before rewrite) · a backup that cannot be written (the original untouched; reported; retried on the next save) · a torn write (temp + replace) · a superseded switch (its stop edge detached) · a concurrent save (a lock) · a second attach in one shell lifetime (the prior persistence disposed) · a seed default that violates the host's invariant (filtered and recorded, never silent).

## Instrumentation (IO2)

`shell.mode{mode, from, trigger, first_entry, outcome, error_code}` at every switch, refusal and failure · `shell.mode.shown{mode, trigger, duration_ms}` at the new body's first `Loaded` · `layout.restore{perspective, placement, dropped_count, dropped_kinds, dropped_reasons, error_code}` per host at workspace open and for the seed default · the existing `layout.mutation`/`layout.reconcile` per host · `LayoutPersistence.SaveFailed` → the live region. Every path degrades to "not recorded": a headless switch has no `shown` line; a superseded switch has none.

## Testing Strategy directives applied

T1 (unit, red-first) · T3 (characterisation: the golden pre-perspective file) · T5/T6 (contract: the frozen DTO reflection + wire names) · T8 (failure paths red-first) · T9 (the composition-root test: the pairing test over `WorkbenchShell` + `PerspectiveShell` + `CommandRouter` + `MainWindow.OnDocumentOpening`) · T12 (source scans as controls: the seam order, the `Loaded` allow-list, the rail's container) · D0 hygiene (no sleeps; STA harness; every Sink capture restored).

## Reviews (Adversary Mode, read-only; two rounds, cap 2)

| Lens | Round 1 | Round 2 |
|---|---|---|
| Data & Persistence Architect (hard) | **BLOCK** — a pre-existing `.bak` suppressed the refused-file backup (run and observed); Major: the seed default's silent duplicate drop; minors 3–11 | **PASS-WITH-CONDITIONS** — the blocker traced closed (the `File.Replace`-with-existing-backup primitive verified by execution); one new Major — "Show" of a one-instance kind held in a collapsed zone read the projection and was now refused — closed in this slice (M9); the minors accepted or relayed |
| UX & Accessibility (hard) | **VETO** — `TabNavigation=Once` on the rail's container made the rail unreachable by Tab; Majors: other selection writers, Escape from anywhere in Explore, the focus landing; minors 5–13 | **VETO (NEW-1)** — F1–F4 closed; a new Blocker of F1's class introduced by the round-1 fix (`IsTabStop` toggling inside the rail's own Once group makes the rail unenterable after arrowing to a non-selected destination), Inferred, with a six-line falsifier and the fix named. **The cap fired:** the falsifier was run — **red, as predicted** — the prescribed fix (tab index, never `IsTabStop`) applied, the falsifier green (M11), NEW-2 (Escape inside the canvas is the page's) and NEW-3 (the first item's clipped ring) applied as prescribed; not re-reviewed by the lens — **the conductor confirms** (SH-1's precedent) |
| Test Architect (hard) | **BLOCK** — switch-first seam order with no create-failure test; no Proof Pack; Majors: the restore event, switching never resets, the stale stop edge; minors 6–16 | **PASS-WITH-CONDITIONS** (CLEAR conditional on this Proof Pack) — blocker 1 and Majors 3–5 closed; the round-2 minors (the derived-opener refused add, extents on switch, "seven" → "six", the seam's doc) closed; the named mutations (M10, M7) run and recorded here |
| The Simplifier | **soft veto** on the zero-caller class (seven members with no reader; the dead `PerspectiveRequested` wiring) — every one deleted; the `ContainerOf` walk replaced by `ItemsControl.ContainerFromElement`; `SlotPathFor` takes the row; one `AddSurface` match; one owed-backup value; `HostOf` private; the window's title has one writer; the two test-only controller overloads kept with `simplify:` markers (one of the eleven test sites is in the census track's file); the router honours the Tech Lead's bound (no command-id literal) — **net −70 lines** | — |
| WPF styling lens (host B's airspace) | **ESCALATE (airspace, F1 — outside this diff)** / PASS-WITH-CONDITIONS on the slice's own files. F1: the command palette is in-tree WPF chrome over a windowed WebView2 and ADR-0015's snapshot swap does not exist in the product — a pre-existing mechanism that Architecture's default body (the canvas in the Center) now meets by default → relayed. **F2 (Major, fixed here):** `Application.TryFindResource` never searches a Window's dictionary, so two of three rail glyphs resolved null — now a resource reference (`SetResourceReference`), with `EveryDestinationsGlyph_ResolvesFromTheWindowsOrTheApplicationsRegistry` (M12 red on the old lookup). F3 (the first item's clipped ring) had been fixed with the UX & Accessibility round (item margin 0,2,0,2). F4 (the permission overlay lives inside the session document, host A's tree — P-5 cannot pass while Architecture is the body), F5–F9 (P-4 additions, floating panes from host B, per-host theme cost, the P-2 edge over the TabColour band, no layout rounding) relayed | — |

Every round-1 blocker and Major was closed in code with its falsifying test (the Reds table); the findings relayed rather than fixed are in §Deviations.

## Deviations and findings (not acted on here)

- **ADR-0032 text** (the conductor / the ADR's owner): the `.bak` once-rule applies to the pre-perspective backup only — the refused backup is every-time (rule 4's wording); the rollback-then-re-upgrade sequence's accepted loss; `ReplaceFile`'s partial failures; no fsync; the one-instance "first" is the zone walk Left · Right · Bottom · Center · floating; the D&P assertion that the data directory is git-ignored is `tools/verify-aide-gitignore.py`, not a test here.
- **SH-3:** `WorkbenchLayout.Default(perspective)` must satisfy each host's one-instance invariant (today's seed carries two `view`s; the Architecture host's interim default drops `domain` and records it as `default-filtered`); the drop-with-report's second sentence then becomes the spec's verbatim (§B4's default named).
- **X-1 (the census probe):** walk host B (add the kinds through `shell.HostFor(PerspectiveMenu.Resolve(kind, …))`, switch the presenter) and the rail's selected/focused states; fail a named surface that measured zero rows; the selected-inactive tab's `TabColour` band under the 2px edge and the caption's rest ink (the UX & Accessibility reviewer's F11).
- **App.xaml (unowned this horizon):** `IconCoding`/`IconArchitecture` move to the icon registry; `BorderStrongBrush` for PS-C4's `--border-strong` role; the `.bak` refusal code joins `LayoutErrorCodes`.
- **Spec §C4/§C5:** the *opening* state has no interval in a synchronous retained switch; Explore has no search box (the reader is the first non-canvas focusable); the error copy's colon vs em dash (DESIGN.md's colon kept).
- **US-C12:** the switch is two log lines, joined by `mode` and `trigger` (recorded in DESIGN.md); the join key is not unique across repeated identical switches.
- **DC-135 recurrence 3** registered; **DC-161** (an expected order typed from a picture) registered with the id left for the conductor.
- **WPF lens escalations (outside this diff):** (F1) the command palette is in-tree chrome over a windowed WebView2 — `CanvasSurface.SetObscured` only sets a bool; no snapshot swap exists; host it in a `Popup` or implement ADR-0015 rule 2 (owner: the palette's / ADR-0015's; check: Architecture + Ctrl+Shift+P, `PrintWindow(PW_RENDERFULLCONTENT)` over the palette's rect). (F4) the permission banner is docked inside `SessionDocumentSurface` (host A's tree), so **P-5's expectation** — visible while Architecture is the body — cannot hold as written: re-scope P-5 to the live region, or add a shell-level mirror (the Conversation lane's file). (F5/F6) add to P-4: after A → B → A the canvas HWND's `GetParent()` is not the main window; float a pane in Architecture, Ctrl+1, observe, Ctrl+3, same `CoreWebView2`. (F7) each host loads its own copy of the VS2013 theme — emit a `dock.theme.applied duration_ms` per host or share one; `AppStart` reports host A's theme only. (F8) the P-2 edge paints over the top 2px of a session tab's `TabColour` band (identity vs state) → UX & A. (F9) no `UseLayoutRounding` anywhere — 3 DIP and 1 DIP elements soften at 125/150 % (pre-existing). Residual named: floating windows take a fresh theme copy (VS-blue, square tabs on a float — pre-existing); `workbench.focusCanvas` routed to a hidden host would focus an HWND under the notification window; `PromptBar.Root` uses `Brushes.White/Gray`.

## Attended (RUN-PENDING, the operator)

| Item | Steps | Expected |
|---|---|---|
| **P-1** UIA walk of the rail | Open the app; run Accessibility Insights (or Inspect) over the rail; read the control types, names, selection state; press Tab from New session; Down ×2; Enter; Ctrl+1 | a Tab list of three TabItems named "<Title> perspective" with exactly one `IsSelected`; Tab lands on the selected destination; Down moves focus only; Enter switches; the 3px bar and the accent glyph follow the selection; 44 × 44 targets |
| **P-4** WebView2 / HWND identity across a cycle | Open a workspace; Architecture; open a class diagram (View → New class diagram); select a node, scroll; Ctrl+1, Ctrl+2, Ctrl+3 ×3; also with a second class diagram in a non-selected tab; record `msedgewebview2` process count and the app's private bytes before host B's first entry, after it, after three cycles | the diagram's selected node and scroll offset unchanged; `WebSurfaceHost.InitialisationsStarted == 1` per web surface (the `web-surface.handshake` lines show one `initialising`); process count unchanged after the first entry; private bytes delta recorded |
| **P-5** the permission overlay while Architecture is active | Start a governed run in Coding that will ask for permission; switch to Architecture before it asks | the overlay is visible within one dispatcher frame regardless of the active perspective |
| **P-7** the gesture from inside a WebView2 page | Focus inside the composer editor (Coding), press Ctrl+3; focus inside the Explore graph, press Ctrl+3; then inside the terminal | Architecture becomes active each time (or: the finding that the page swallows the chord → the web surface forwards it, `/design-slice`'s decision) |
| **P-8** retained-switch p95 | ≥ 20 Ctrl+1/Ctrl+3 alternations after first entry; read `shell.mode.shown.duration_ms` from `%LOCALAPPDATA%\AiDe\logs\workbench-*.log` | p95 ≤ 150 ms; first-entry durations reported, no budget |
| **P-9** screen-reader trace | NVDA; Ctrl+3 from Coding; Escape from the Explore reader | the announcement precedes the focus move; no double-speak; Escape from Explore returns to the previous perspective |

## Gate table (at close, bare, stop on the first red)

| Gate | Result |
|---|---|
| `dotnet build` Core · App · Core.Tests · App.Tests `-p:TreatWarningsAsErrors=true` | 0 warnings / 0 errors, each |
| `tests/AiDe.Core.Tests` (full) | **2312 / 2312 passed, 0 failed** (`artifacts/test-results/AiDe.Core.Tests.trx` counters, the run of 10:36–10:38) |
| `tests/AiDe.App.Tests` (full) | **781 / 781 passed, 0 failed** (`artifacts/test-results/AiDe.App.Tests.trx` counters, the re-run of 10:5x after the glyph fix; the run before it 780 / 780) |
| `tools/verify-test-run.py` (CHECK only, never `--update`) | OK — App 781 executed against the baseline 735, Core 2312 against 2294, both Completed; the conductor re-baselines at the join |
| `tools/verify-terminal-host-exit-paths.py` | OK — 5 exit paths executed and passed |
| every other `tools/verify-*.py` | OK, except: `verify-derived-views` / `verify-site-figures` (regenerated by `regenerate-derived.py` after the audit entry — see the commit), `verify-stranded-audit` (**the conductor's tree** `C:\Projectsi-de-conductor-addendum-c` has uncommitted `docs/audit/audit-log.jsonl` lines — not this slice's; reported) |
| `design-lint.py DESIGN.md --strict` | clean — 0 warnings |
| `verify-design-modes.py` | OK — 30 colour roles, each with a light value |
| `ui-craft-gate.py docs/mockups` | unchanged: Major 60 · Minor 38, identical to `main` (`70a106c2`) — the mockups are not touched |
| `ShellContrastCensusTests` (in the App suite) | passing — 0 failing pairs over host A and the rail's rest state (host B and the rail's other states are not in the probe's population: DC-135 recurrence 3, the X-1 seam request) |
| The DC-135 ratio watch | 65 : 38 (not widened from 65 : 29 on `main`; the register's 66 : 8) |
| The two flaky environmental reds seen in run 2 (the terminal-host leak tests: "the process table could not be read" under three concurrent sessions' load) | passed in isolation and in runs 3 and the final run; not this slice's |
