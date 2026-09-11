---
id: note-addendum-c-current-state-inventory
title: "Addendum C — current-state inventory (M0)"
type: doc
status: accepted
phase: "1"
owner: "@timianmalloo"
tags: [addendum-c, inventory, ux, workbench]
links:
  - { to: plan-addendum-c-modes, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: spec-knowledge-explorer-mode, rel: relates-to }
  - { to: spec-named-dock-zones, rel: relates-to }
review-by: 2026-12-11
summary: >-
  Read-only inventory of the workbench shell at main@7f0b67a3: every surface, menu/command,
  the two-mode shell (Workbench/Explorer, ADR-0017), zone/layout persistence, the specs and
  ADRs that already govern this surface, a local craft-gate run (104 findings), and the
  test-coverage ratio for the layout service the shell actually runs.
---

## 0. Scope note

Read-only against `C:\projects\ai-de`, `main` at `7f0b67a333381bbc41290281d16e8dccccc8f748`. Every
row below cites `file:line`; a claim I could not open a file to check is marked `unverified`.
Untracked files present at session start (`.agents/decisions/…`, `.agents/log/…`,
`.agents/sessions/claude-conductor.md`) were not read — out of scope for a workbench inventory.

## 1. Surfaces

Source: the `SurfaceContentFactory.Kinds` descriptor list
(`src/AiDe.App/Workbench/SurfaceContentFactory.cs:68-129`) plus every `*Surface.cs`/`*View.cs` under
`src/AiDe.App/Workbench/` (including `Composer/`, `Sessions/`). Use-case reading is mine, labelled.
UC1 = Agentic Coding, UC2 = Knowledge Exploration, UC3 = Code & Architecture Understanding, UC4 =
Test Coverage (later — no surface today serves it, see §8).

| Kind/class | What it shows | Opened via | Default zone/placement | Serves |
|---|---|---|---|---|
| `view` / `inspector` (both build `Evidence()`, byte-identical — SurfaceContentFactory.cs:71-108) | Evidence rows (Explore/Provenance/Domain captions are layout text, not a discriminator — SCF.cs:75-92) | default layout tabs; `EvidenceContent` SCF.cs:202 | tab captions "Explore"/"Domain"/"Provenance" (default layout, not read here) | UC2 (Explore is Explorer-rail-duplicate per SCF.cs:92); UC3 (Domain — mislabelled, see finding) |
| `terminal` → `TerminalSurface.cs:32` | One live ConPTY session, rendered by `TerminalView.cs:34` | `terminal.new`, per-harness `New <X> session`, agent panes | terminal/tool zone (Bottom by default per named-dock-zones.md:155) | UC1 |
| `canvas` → `CanvasSurface.cs` | Windowed WebView2 graph canvas (ADR-0015) | `workbench.focusCanvas`; created with sessions/Explorer | Center (documents→Center, named-dock-zones.md:155) | UC2, UC3 |
| `contexts` → `ContextMapSurface.cs:22` | Bounded-context boxes + cross-context traffic (crossing counts) | no catalog command found (`grep` of WorkbenchCommands.cs found none referencing "contexts") — **unreachable by menu/palette today**, see finding | n/a | UC3 |
| `joins` → `JoinSurface.cs:28` | Code/schema/infra joins, Verified vs Inferred, disclosures for what could not be joined | no catalog command found — **unreachable by menu/palette today**, see finding | n/a | UC3 |
| `sessions` → built by `SurfaceContentFactory.Sessions()` SCF.cs:315 | Loomkeeper watcher: live/inactive session rows | default layout | tool zone | UC1 |
| `board` → `SurfaceContentFactory.Board()` SCF.cs:497 | Loomkeeper message board (cross-repo posts) | default layout | tool zone | UC1 |
| `leaderboard` → SCF.cs:509 | Loomkeeper scoring leaderboard | default layout | tool zone | UC1 (none of UC2-4) |
| `ledger` → SCF.cs:518 | Append-only episode ledger | default layout | tool zone | UC1 |
| `daydreams` → SCF.cs:490 | Observed patterns / candidate lessons (read-only) | default layout | tool zone | UC1 |
| `prompt` → `PromptDraftSurface.cs:16` | Staged prompt, one-way Transfer to a ready terminal | `workbench.newPromptDraft` (Ctrl+K,D) | beside a terminal (its transfer target) — DocumentPlacement.cs:20 excludes it explicitly | UC1 |
| `classdiagram` → `ClassDiagramSurface.cs:19` | Type-hierarchy cards (inherits/implements), member-less Phase 1 (ADR-0026) | `workbench.newClassDiagram` (Ctrl+K,M); node "Open as… Class diagram" (`NodeViewMenu.cs:65`) | document stack via `DocumentPlacementPolicy` (DocumentPlacement.cs:23) | UC3 |
| `sequence` → `SequenceDiagramSurface.cs:23` | UML sequence diagram from `calls_at` graph assertions | `workbench.newSequenceDiagram` (Ctrl+K,Q); node "Open as… Sequence diagram" | tab/document | UC3 |
| `search` → `SearchSurface.cs:26` | Breadth search across types/members/files/graph/commands (scaffold — no index wired, SearchSurface.cs:18-22) | `workbench.newSearch` (Ctrl+K,F) | tab | UC2, UC3 |
| `codeviewer` → `CodeViewerView.cs:16` | Read-only AvalonEdit source view, syntax-highlighted (ADR-0025) | `workbench.newCodeViewer` (Ctrl+K,U); node "Open as… View source" | document stack (DocumentPlacementPolicy) | UC3 |
| `diagnostics` → `DiagnosticsSurface.cs:27` | Re-index coverage + daemon state | `workbench.newDiagnostics` (Ctrl+K,D — **same gesture as `workbench.newPromptDraft`**, see finding) | tool zone | UC1, UC3 |
| `session-document` (`Sessions.SessionDocumentSurface.Kind`, SCF.cs:128) | One session as a dock document: composer left / canvas right (`SessionDocumentSurface.cs:14`) | `session.new` (Ctrl+N) via `NewSessionFlow` | Center, paired-zone preset | UC1 |
| `ExplorerSurface.cs:32` (not a `Kind` row — a shell view mode) | Full-window graph+reader split (ADR-0017) | rail button `ExploreRailButton` / `shell.toggleExplorer` (Ctrl+K,E) | replaces the whole body, not docked | UC2 |
| `NodeReaderView.cs:18` | Reader half of Explorer: node header/metadata/edges (Phase 1; per-kind content is Phase 2, ADR-0018) | only inside `ExplorerSurface` | n/a (Explorer-internal) | UC2 |
| `CommandPalette.cs:23` | Keyboard command list (overlay, `RootLayer`) | Ctrl+K family opens it implicitly / a dedicated chord (not traced further) | overlays `RootLayer`, not docked | none of the four (utility) |
| `Composer/ComposerSurface.cs:37` | Rich WebView2 prompt editor, host-owned send, compiled-view read-before-send | inside `SessionDocumentSurface`'s composer zone | Center, composer half | UC1 |
| `Sessions/ConsoleSurface.cs:26` | Merged multi-lane event stream, one canvas mode of a session document | canvas-mode switch inside `SessionDocumentSurface` (`CanvasModeCatalog.cs`) | Center, canvas half | UC1 |
| `Sessions/NewSessionSheetDialog.cs:30` | Modal "new session" sheet (task class, harness, workspace) | `session.new` flow (`NewSessionFlow.cs`) | modal dialog | UC1 |

**Serves none of the four use cases as currently scoped:** `sessions`/`board`/`leaderboard`/`ledger`/
`daydreams` (SCF.cs:113-117) are the Loomkeeper multi-agent-fleet observation surfaces — they serve
none of UC1-4 as the operator defined them (UC1 is the session/terminal/hybrid construct, not fleet
scoring); confirm with product owner before Addendum C decides their mode.

## 2. Menus

Source: `MainMenuBuilder.Layout` (`src/AiDe.App/Workbench/MainMenuBuilder.cs:69-99`), each id resolved
against `WorkbenchCommandCatalog.All` (`src/AiDe.Core/Workbench/WorkbenchCommands.cs`). One catalog
feeds menu + palette + chord (MainMenuBuilder.cs:10-22).

| Menu | Item (command id) | Opens/does |
|---|---|---|
| _File | `session.new` (Ctrl+N) | New Session sheet, pre-bound to open workspace (WorkbenchCommands.cs:118) |
| _File | `workspace.open` (Ctrl+K,O) | Opens a folder as a workspace, starts its daemon (WorkbenchCommands.cs:106) |
| _File | `workspace.indexSolution` (Ctrl+K,I) | Indexes C# projects, one scope per TFM (WorkbenchCommands.cs:126) |
| _File | `workspace.reindexAll` (Ctrl+K,Shift+I) | Re-reads every scope, ignoring cache (WorkbenchCommands.cs:133) |
| _File | `workspace.refresh` (Ctrl+K,Ctrl+I) | Re-index this workspace; current evidence keeps rendering (WorkbenchCommands.cs:237) |
| _File | Recent sessions / Recent workspaces submenus | `RecentSessions`/`RecentWorkspaces` lists (MainMenuBuilder.cs:112,207-252) |
| _Edit | `workbench.moveSurface` (Ctrl+K,M) | Keyboard move-pane flow (WorkbenchCommands.cs:37) |
| _Edit | `workbench.resizePane` (Ctrl+K,R) | Keyboard resize-split flow (WorkbenchCommands.cs:41) |
| _View | `shell.toggleExplorer` (Ctrl+K,E) — "leads the View menu because it is a WHOLE MODE" (MainMenuBuilder.cs:79-80) | Swaps body Workbench↔Explorer (WorkbenchCommands.cs:217-220) |
| _View | `workbench.focusCanvas` (Ctrl+K,G) | Moves focus into graph canvas (WorkbenchCommands.cs:223) |
| _View | `workbench.nextSurface`/`previousSurface`/`reorderSurface` | Tab navigation/reorder in focused pane |
| _View | `watcher.raiseDispute` (Ctrl+K,Ctrl+U) | Appends operator dispute on latest scored episode (WorkbenchCommands.cs:247) |
| _View | `workbench.newSearch` (Ctrl+K,F) | Opens `search` surface |
| _View | `workbench.newClassDiagram` (Ctrl+K,M — **collides with `workbench.moveSurface`'s gesture**, see finding) | Opens `classdiagram` surface |
| _View | `workbench.newSequenceDiagram` (Ctrl+K,Q) | Opens `sequence` surface |
| _View | `workbench.newCodeViewer` (Ctrl+K,U) | Opens `codeviewer` surface |
| _View | `workbench.newDiagnostics` (Ctrl+K,D — **collides with `workbench.newPromptDraft`**) | Opens `diagnostics` surface |
| _View | `workbench.clearStatus` (Ctrl+K,Ctrl+C) | Empties status line |
| _Window | `workbench.floatPane`/`collapsePane`/`maximizePane`/`closeSurface`/`toggleLock`/`resetLayout` | Pane state operations (WorkbenchCommands.cs:44-83) |
| _Terminal | `terminal.new` (Ctrl+K,T) | Plain shell terminal (WorkbenchCommands.cs:158) |
| _Terminal | one item per `AgentReadinessProfiles.BuiltIn.All` launchable profile, alphabetical (MainMenuBuilder.cs:92-96) | "New `<Harness>` session" agent terminal |
| _Terminal | `workbench.dispatchPrompt` (Ctrl+K,P) | Dispatch prompt to terminal, with delivery receipt (WorkbenchCommands.cs:177) |
| _Terminal | `workbench.newPromptDraft` (Ctrl+K,D) | Opens `prompt` surface |
| _Help | `workspace.diagnostics` (Ctrl+K,D — **third collision on Ctrl+K,D**, see finding) | Daemon/health/MCP diagnostics report |

**`CommandPalette.cs`**: not a fixed list — it live-filters `WorkbenchCommandCatalog.Search(text)`
(CommandPalette.cs:152), so its contents are exactly `WorkbenchCommandCatalog.All`, i.e. every row
above plus any catalog command with no menu placement (`Menu` defaults to `""`, WorkbenchCommands.cs:28).
I did not enumerate catalog rows with no `Menu:` to find hidden palette-only commands; that is a gap
in this inventory (`unverified` for palette-only-command completeness).

**`NodeViewMenu.cs`**: not a menu builder, a pure mapping `nodeKind × isKnowledge → NodeViewOption[]`
(`src/AiDe.App/Workbench/NodeViewMenu.cs:33-90`) — the right-click "Open as…" contextual menu on a
graph/reader node. Type-driven: knowledge nodes get Read/Metadata/Reveal-in-graph; members get
Source/Sequence/Metadata; types get Source/ClassDiagram/Sequence/Metadata/Reveal; data shapes get
Source/Metadata/Reveal; everything else gets Metadata/Reveal only (NodeViewMenu.cs:38-89).

## 3. Shell modes today

`ShellModeController.cs:30` — a two-value enum `ShellViewMode { Workbench, Explorer }`
(ShellModeController.cs:7-11), ADR-0017's "primary view mode" realised as a **body-content swap**:
`_host.Content` toggles between the retained `_workbench` object and a lazily-created, then-retained
`ExplorerSurface` (ShellModeController.cs:66-75). No rebuild either direction (the load-bearing
invariant, ADR-0017 "Consequences").

Wiring: `MainWindow.xaml.cs:57-60` constructs `_mode = new ShellModeController(WorkbenchHost, Shell,
() => new ExplorerSurface(Shell.CreateExplorerGraph(), new NodeReaderView()))`. Toggle reachable via
`Shell.Controller.ExplorerToggleRequested = ToggleExplorerMode` (MainWindow.xaml.cs:92) and the
catalog command `shell.toggleExplorer` (AR5, MainMenuBuilder.cs:79-80). The rail button
`OnToggleExplorer` (MainWindow.xaml.cs:455-456) routes through the **same** catalog command rather
than calling the mode controller directly.

**The side toolbar / activity bar**: `x:Name="ActivityRail"` (`src/AiDe.App/MainWindow.xaml:65-120`),
a `StackPanel` in Grid Row 2 / Column 0 (56px wide, `MainWindow.xaml:59-63`), styled via
`{StaticResource RoundedButton}` and theme brushes (`SurfaceSunkenBrush`/`AccentBrush`/
`TextMutedBrush`). It holds exactly **two** live buttons today:
- `NewSessionRailButton` (MainWindow.xaml:75-85) — the one primary action (AR2), accent-filled, runs
  the same `session.new` catalog command as File → New Session.
- `ExploreRailButton` (MainWindow.xaml:100-106) — the only door to Explorer mode, with an
  `ExploreAccentBar` active-state indicator (MainWindow.xaml:98-99, wired at MainWindow.xaml.cs:492-498).

**Three former rail buttons were deleted, not merely disabled** — "Coordinate", "Compose", "Audit"
(MainWindow.xaml:108-119, comment AR3): *"A RAIL ITEM IS PRESENT ONLY IF IT DOES SOMETHING… whether
those modes get built is a product decision and an absent row is the honest state of one that has not
been made."* This is the closest prior art to Addendum C's four-icon use-case rail — a multi-mode
rail was tried, the modes were never built, and the rail was pruned back to what works rather than
extended.

## 4. Dock zones and layout persistence

**Named zones** (ADR-0021, `spec-named-dock-zones.md`): `Left`, `Right`, `Bottom`, `Center`
(named-dock-zones.md:61). Center always present, never collapses (AC-F3, named-dock-zones.md:88-90);
Left/Right/Bottom are tool zones, collapse reversibly to a rail (AC-F4, named-dock-zones.md:91-97).
Rails are drawn by `ZoneRails.cs:17` — a `DockPanel` wrapping the docking host with a 26px
(`RailThickness`, ZoneRails.cs:19) clickable `Border` per collapsed tool zone (Left/Right/Bottom only
— `RailFor` throws for any other `ZoneId`, ZoneRails.cs:84-90), showing the zone name + surface count
and expanding on click (ZoneRails.cs:107-124).

**`LayoutPersistence.cs:18`**: debounced (750ms default, LayoutPersistence.cs:41,60-61) save/restore.
Two schemas coexist:
- **Tree schema** (`LayoutStore.cs`): `LayoutEnvelope(SchemaVersion, …)` current version **4**
  (`LayoutStore.cs:91`); missing/newer/corrupt file handling via a `Migrations` chain
  (`LayoutStore.cs:161-181` — refuses a file from a *newer* schema, steps a chain for an older one).
- **Zone schema** (`ZoneLayoutStore.cs`): `ZoneEnvelope(SchemaVersion, …)` current version **1**
  (`ZoneLayoutStore.cs:53`), a **sibling file** (`ZonesPathFor`, LayoutPersistence.cs:64-69, suffix
  `.zones.json`); load discards on any version mismatch (`ZoneLayoutStore.cs:103`, no migration chain
  yet for zone schema).

`LayoutPersistence.Restore()` branches on which service was injected: when it is a
`ZoneBackedLayoutService` (`LayoutPersistence.cs:54-58,90-109`) it restores/keeps the **zone** model
(three outcomes: restored-saved, kept-current, i.e. never silently resets); otherwise it falls back to
the legacy tree `_store.Load` path (LayoutPersistence.cs:111-118), which has no "kept" branch — it
either loads or defaults.

**`ZoneBackedLayoutService` vs `LayoutService` — confirmed.** `src/AiDe.App/Workbench/WorkbenchShell.cs:113`:
`Service = new ZoneBackedLayoutService();` — the shell runs the zone-backed service exclusively; there
is no code path in `WorkbenchShell` that constructs the legacy `LayoutService`.

## 5. Existing specs and ADRs

| Spec | `id:` | status | What it fixes that Addendum C must honour/supersede |
|---|---|---|---|
| `docs/specs/knowledge-explorer-mode.md` | `spec-knowledge-explorer-mode` | draft | Defines Explorer as a full-window MODE (not a dock pane), rail-triggered, dual-pane graph+reader. Addendum C's mode system must either subsume this (Explorer becomes one of four use-case modes) or explicitly keep it as a sibling concept — the spec currently treats "mode" as a body-swap, not a use-case constraint on dockable surfaces. |
| `docs/specs/knowledge-exploration.md` | `spec-knowledge-exploration` | draft | Specifies the underlying graph/2D-3D/node-introspection/UML-ERM traversal surface that `knowledge-explorer-mode` wraps as a full-window presentation. Addendum C's UC2/UC3 surface set overlaps this spec directly (2D/3D toggle, node introspection are UC2+UC3 concerns per the task brief). |
| `docs/specs/named-dock-zones.md` | `spec-named-dock-zones` | in-review | Establishes the fixed Left/Right/Bottom/Center frame every dockable surface lives in today. Addendum C's per-mode surface restriction must be expressed as a constraint layered on top of this zone model, not a replacement — zones are orthogonal to "which surfaces this mode permits". |
| `docs/specs/editor-surfaces.md` | `spec-editor-surfaces` | draft | Specifies the read-only code viewer and prompt-draft editor as the two "still lacking" content surfaces at the time it was written — both now exist (`codeviewer`, `prompt` kinds). Addendum C should confirm/close or supersede this spec's open acceptance criteria against the shipped surfaces. |
| `docs/specs/uml-erm-surfaces.md` | `spec-uml-erm-surfaces` | draft | Specifies first-class UML/ERM surfaces (C4/class/component/sequence, crow's-foot ER) as read-only derived views. Only class + sequence are built (ADR-0026 Phase 1; `SequenceDiagramSurface`); component, C4 and ER diagrams are unbuilt — directly the UC3 gaps in §8. |
| `docs/specs/app-facelift.md` | `spec-app-facelift` | draft | Specifies the "soft islands" visual language, icon system and discoverable menu/command system the current shell already implements (`SurfaceChrome.WrapAsIsland`, MainMenuBuilder). Addendum C's new mode-contextual top menu must stay inside this facelift's icon/menu conventions, not invent a second visual language. |
| `docs/specs/terminal-sessions.md` | `spec-terminal-sessions` | draft | Specifies session-identity-preservation as an aggregate invariant (never destroy a terminal except by explicit user intent). A mode system that hides/unmounts surfaces on mode switch must preserve this invariant exactly as ADR-0017 already does for Explorer (retain, never rebuild) — the terminal/session construct is UC1's core object. |
| `docs/specs/ai-native-ide.md` (shell/workbench/explorer sections only) | `spec-ai-native-ide` | in-review | The top-level product spec: US-9 defines the dockable workbench's non-overlapping-tiling invariant (ai-native-ide.md:224,391-394,918) and the exemplar research (VS Code/Eclipse/Photoshop/Premiere custom-layout patterns, ai-native-ide.md:64-111). Addendum C's use-case modes are a *further* constraint on top of US-9's general workbench — US-9 does not currently know about use-case-scoped surface restriction. |

**ADR-0017** (`docs/adr/0017-primary-view-mode.md:1-125`) — quoted decision: *"Adopt C. A **primary
view mode** is a first-class shell concept: the shell is in exactly one mode at a time; the body
content is the projection of that mode; the activity rail selects it… distinct from a dock pane (A)
and from the modal `RootLayer` overlay (B)."* Stated reasons: option A (another dock pane) rejected
as "the exact defect being fixed — the surface competes with every other pane… cannot express 'this is
a different *kind* of view'"; option B (modal overlay) rejected because "the Explorer is a **place you
work in**, not a transient prompt: the activity **rail must remain visible and usable**… A full-window
modal that hides the rail would strand the mode selector, and one that keeps the rail is no longer an
overlay — it is option C." Load-bearing invariant: "retain, never rebuild" (ADR-0017 "Consequences").
**This is the architectural precedent Addendum C's four-mode system must either extend (N modes
instead of 2) or explicitly diverge from with a recorded reason** — a 4-mode body-swap is a direct
generalisation of the 2-mode mechanism already accepted here.

Other ADRs whose titles touch shell/docking/explorer/layout/menus (`docs/adr/*.md`, title line 3 of
each file):
- `adr-0008-shell-host` — WPF frame + embedded WebView2 as the shell host.
- `adr-0012-docking-shell-library` — AvalonDock adoption, owned accessibility layer.
- `adr-0013-layout-persistence-envelope` — versioned envelope outside the fact store.
- `adr-0015-canvas-hosting-and-overlay-strategy` — windowed WebView2, snapshot-swap overlay (referenced
  by ADR-0017's Explorer/canvas-focus consequence).
- `adr-0018-node-content-reader-contract` — the reader's on-demand content seam (Explorer Phase 2).
- `adr-0021-named-dock-zones` — the zone model itself (§4 above).
- `adr-0025-code-viewer-renderer` — AvalonEdit choice for `codeviewer`.
- `adr-0026-class-diagram-architecture` — App-side type-hierarchy view, Phase 1 scope.

**ADR-0028 (`mode-cohort-not-partition`) is a false lead by name.** It governs Loomkeeper leaderboard
scoring cohorts (`mode` as a nullable column beside `ScoreSegment`, adr-0028:47-52), not the shell's
`ShellViewMode` — unrelated to Addendum C despite the word "mode" in its title. Noted so the next
session does not re-open it expecting UI-mode guidance.

## 6. Craft-gate baseline (measured, local run)

Invocation source: `.github/workflows/ui-craft.yml:47-65` (design-lint) and `:67-79` (craft detector).
Ran both exactly as CI does, read-only, against committed source:

```
python docs/ai-forward-pack/scripts/design-lint.py DESIGN.md --strict
→ clean — all token references resolve (0 warning(s)).

python docs/ai-forward-pack/scripts/ui-craft-gate.py docs/mockups --markdown
→ 104 findings total: 66 Major, 38 Minor, 0 Blocker
```

Top rules by count (from the 104-row findings table, counted directly, not from the tool's own
possibly-partial summary table which I found printed only 78 of the 104):

| Rule | Count |
|---|---|
| Cramped padding | 26 |
| Undersized functional text | 21 |
| Color outside DESIGN.md | 19 |
| Low contrast text | 14 |
| Tiny body text | 11 |
| Flat type hierarchy | 6 |
| Nested cards | 4 |
| Side-tab accent border | 1 |
| Em-dash overuse | 1 |
| All-caps body text | 1 |

This is a **mockup-corpus** measurement (`docs/mockups/*.html`), not the running WPF app — the task's
`ui-craft-gate.py`/`design-lint.py` invocations in `.github/workflows/ui-craft.yml` only ever scan
`docs/mockups/` and `DESIGN.md`; there is no XAML/WPF-source craft scan in this workflow, so **the
actual shell UI's craft conformance is "not measured — no gate or script in this repo scans
`src/AiDe.App/**/*.xaml` for craft rules"** (checked: `.github/workflows/ui-craft.yml` full contents
read, `tools/verify-ui-craft-floor.py` per the workflow's own comment scans only
`docs/mockups/session-front-door.html` and `DESIGN.md`, not the app source).

## 7. Test surface

Grep of `tests/` (source files only, `bin/`/`obj/` excluded):

| Class under test | Test files (relevant) |
|---|---|
| `ShellModeController` | `tests/AiDe.App.Tests/ExplorerModeTests.cs` |
| `LayoutPersistence` | `tests/AiDe.App.Tests/LayoutPersistenceTests.cs`, `tests/AiDe.App.Tests/ZoneLayoutPersistenceTests.cs` |
| `MainMenuBuilder` | `tests/AiDe.App.Tests/MainMenuTests.cs`, `tests/AiDe.App.Tests/Sessions/TheFrontDoorIsInTheFileMenuTests.cs` |
| `SurfaceContentFactory` | `tests/AiDe.App.Tests/SurfaceContentTests.cs` |

**DC-135 fact** (`docs/lessons/defect-classes.md:5801-5840`): recorded as *"66 tests construct `new
LayoutService()`; 8 construct `new ZoneBackedLayoutService()`"* — cited as the reason Ruling 47's
premise (that `StackState.Maximized`/`workbench.maximizePane` were "pre-existing and tested") did not
hold, because the shell runs `ZoneBackedLayoutService` (WorkbenchShell.cs:113) whose projection makes
`Maximized` unobservable (`ZonesToTree.cs:67`, cited in the register, not independently re-verified by
me).

**Current ratio, re-measured** (`find tests -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*" |
xargs grep -o "new LayoutService(" | wc -l` → **70**; same for `"new ZoneBackedLayoutService("` →
**19**): the ratio has moved from the register's 66/8 to **70/19** as of this commit
(`7f0b67a3`). This is **not a contradiction of DC-135's shape** — the tree-service coverage still
dwarfs the zone-backed one the shell actually runs (70 vs 19, ~3.7:1, down from ~8:1) — but the
register's exact numbers are now stale and should be re-swept before the next ruling cites them
verbatim. (16 distinct files construct `LayoutService` directly per file-level grep; occurrence count
of 70 counts every call site, including multiple per file.)

## 8. Use case → surface map

Rows = the four use cases from the task brief. Columns = surface kinds from §1. "serves" = built and
reachable today; "could serve" = exists but not purposed/reachable for this use case, or is a
scaffold; "must not appear" = my reading of what a mode boundary should exclude; "—" = irrelevant.

| Surface | UC1 Agentic Coding | UC2 Knowledge Exploration | UC3 Code & Arch Understanding | UC4 Test Coverage |
|---|---|---|---|---|
| `session-document`/`ComposerSurface`/`ConsoleSurface` | serves | must not appear | must not appear | — |
| `terminal` | serves | must not appear | could serve (read-only shell for inspection) | — |
| `sessions`/`board`/`leaderboard`/`ledger`/`daydreams` | could serve (fleet ops, not session-local) | must not appear | must not appear | could serve (episode/coverage history) |
| `prompt` (PromptDraftSurface) | serves | must not appear | must not appear | — |
| `ExplorerSurface`/`NodeReaderView` | must not appear (competes with session focus) | serves | could serve (graph IS the architecture view) | — |
| `canvas` (bare CanvasSurface, non-Explorer) | must not appear | serves (Explorer's own instance) | could serve (as embedded graph in UC3 surfaces) | — |
| `search` | could serve (find a session/terminal) | serves | serves | could serve (find a test) |
| `classdiagram` | must not appear | could serve | serves | — |
| `sequence` | must not appear | could serve | serves | — |
| `codeviewer` | could serve (inspect a file mid-session) | serves (node content rendering) | serves | could serve (view test source) |
| `contexts` (ContextMapSurface) | must not appear | could serve | serves — **but unreachable by any command today (§2 finding)** | — |
| `joins` (JoinSurface) | must not appear | could serve | serves — **also unreachable by any command today** | — |
| `diagnostics` | serves (daemon/index health) | must not appear | could serve (index coverage informs architecture confidence) | could serve (index/coverage overlap) |
| `view`/`inspector` (Evidence) | must not appear | serves (if repointed, see §9 finding) | serves (Domain tab, see §9 finding) | — |
| `CommandPalette` | serves (utility) | serves (utility) | serves (utility) | serves (utility) |

**Gaps — what each use case needs that has no surface today:**
- **UC1 Agentic Coding:** largely served (`session-document`, `terminal`, `prompt`, `sessions`). No
  gap found against the task brief's "session construct, terminals, hybrid" framing.
- **UC2 Knowledge Exploration:** served by `ExplorerSurface`+`NodeReaderView`, but `NodeReaderView`'s
  per-kind content rendering is still Phase 1 stub (metadata+edges only; "the content area is an
  honest placeholder until then" — NodeReaderView.cs:14-16). The 2D/3D toggle named in
  `spec-knowledge-exploration`'s tags (`2d-3d`) has **no surface or command found** — `unverified`
  whether it exists anywhere in `src/AiDe.App` (not found in this grep pass; would need a dedicated
  search of `CanvasSurface`/`CanvasPage.cs` internals, out of this inventory's file budget).
- **UC3 Code & Architecture Understanding — the largest gap set:**
  - **Solution/code tree view**: no surface or `*Surface.cs`/`*View.cs` file found that renders a
    project/file tree. `ClassDiagramSurface`, `ContextMapSurface`, `JoinSurface` are graph/diagram
    views, not a tree. **Gap, unverified further** (a tree might exist inside `ExplorerSurface`'s
    graph pane as a filter, not confirmed).
  - **Scalable class/ER diagram**: `ClassDiagramSurface` is explicitly member-less Phase 1
    ("Member-less by construction… A member-bearing, notation-valid Mermaid render is Phase 2",
    ClassDiagramSurface.cs:14-16) — not yet the "scales" diagram the brief asks for. **No ER/crow's-foot
    surface exists** (`spec-uml-erm-surfaces` specifies it, ADR-0026 only covers class-diagram Phase 1).
  - **Data-flow from an entry point**: no surface found. `SequenceDiagramSurface` shows call order
    from a selected member, which is adjacent but is not a data-flow trace.
  - **Layer/component diagrams**: `ContextMapSurface` shows bounded-context traffic (closest analogue)
    but is unreachable by any command (§2); no separate layered-architecture or C4-component surface
    exists in `src/AiDe.App/Workbench`.
- **UC4 Test Coverage:** explicitly "later" per the task brief — no surface, no command, confirmed
  absent by this grep pass (no `*Coverage*.cs`, `*Test*Surface*.cs` found under `Workbench/`).

## 9. Findings (not recommendations)

1. **`view`/`inspector` are byte-identical and one is mislabelled** — not a fix candidate for this
   inventory but a load-bearing fact for Addendum C's UC2/UC3 split: the tab captioned "Domain" (a
   UC3 concern, US-2) is wired to the `view` kind, which renders identical Evidence-pane content to
   "Explore" and "Provenance", not the `classdiagram` kind that already exists and *is* Domain's real
   surface (SurfaceContentFactory.cs:71-107, comment block). **A mode system built on today's default
   layout would inherit this mislabelling into whichever use-case mode "Domain" lands in.**
2. **`contexts` and `joins` surface kinds exist, render real content, and have zero reachable command**
   — no menu item, no palette entry, no node "Open as…" option found for either
   (`grep` of `WorkbenchCommands.cs` and `NodeViewMenu.cs` for "contexts"/"joins" found nothing). Both
   are UC3 candidates per §8 but are currently dead code from a reachability standpoint (built,
   never openable) — the JoinSurface's own doc comment calls this out for its own antecedent
   (`JoinProjection`): *"This projection existed and nobody could see it… a control that cannot
   fire"* (JoinSurface.cs:12-15) — the surface fixed that for the projection but not for itself.
3. **Three keyboard-gesture collisions on the existing menu**: `Ctrl+K,M` is bound to both
   `workbench.moveSurface` (_Edit) and `workbench.newClassDiagram` (_View); `Ctrl+K,D` is bound to
   `workbench.newPromptDraft` (_Terminal), `workbench.newDiagnostics` (_View), *and*
   `workspace.diagnostics` (_Help) — three commands, one gesture (WorkbenchCommands.cs:41,192,182,207,120).
   Whichever binds last/first in WPF's `InputBindingCollection` wins silently; not exercised by this
   inventory (no test found asserting gesture uniqueness across the whole catalog — `MainMenuTests.cs`
   not read in full for this specific assertion, `unverified`).
4. **The rail already tried and abandoned a 4-mode design.** MainWindow.xaml:108-119 (comment AR3)
   documents three deleted rail buttons (Coordinate, Compose, Audit) that sat disabled because the
   modes behind them were never built. Addendum C proposes exactly this shape again (N use-case icons
   on the rail) — the prior attempt's failure mode (a rail item with nothing behind it reads as
   broken) is a directly applicable constraint: **each new rail icon needs its mode built before or
   atomically with the icon**, not scaffolded ahead of it.
5. **ADR-0017 is `status: proposed`, not `accepted`** (adr-0017-primary-view-mode.md:5) — the
   mechanism Addendum C would generalise from 2 modes to N is itself not yet a ratified architecture
   decision. Addendum C should either wait on/trigger its acceptance or explicitly supersede it.
6. **No craft-gate coverage of the actual WPF shell.** §6: the only automated craft measurement in
   this repo runs against static HTML mockups (`docs/mockups/`), not `src/AiDe.App`'s XAML. Any
   Addendum C surface built directly in XAML (as the rail/menu already are) ships with **no craft
   floor at all** unless a mockup is authored and gated alongside it, or a new XAML-scanning gate is
   built — a spec gap the craft-detection knowledge doc (`.github/knowledge/ui-craft-detection.md`
   per AGENTS.md) does not currently close, `unverified` beyond this workflow file.
7. **`spec-editor-surfaces`' "still lacks" framing is stale.** Its summary line
   (editor-surfaces.md, §5 above) says the workbench "still lacks" a code viewer and prompt-draft
   editor; both `codeviewer` and `prompt` kinds are built and wired (SCF.cs:118,122). The spec's
   `status: draft` has not been updated to reflect this — a spec/code drift Addendum C's authors
   should not re-derive requirements from without checking current code first.
8. **A mode system would break the "any surface can always be opened" assumption baked into the
   command catalog and command palette today.** `WorkbenchCommandCatalog.All` (WorkbenchCommands.cs)
   has no per-mode gating field, and `CommandPalette.Refresh()` lists the full catalog unconditionally
   (CommandPalette.cs:150-153) — Addendum C's "side toolbar constrains which surfaces can be
   viewed/docked" requirement has no seam in the current catalog/palette to hang mode-scoping off; it
   would need a new field (e.g. `Modes: ShellViewMode[]`) threaded through `WorkbenchCommand`,
   `MainMenuBuilder.Build`, and `CommandPalette.Refresh`.
9. **Zone-schema migration has no chain, tree-schema does.** `ZoneLayoutStore.Load` discards
   wholesale on any version mismatch (ZoneLayoutStore.cs:103) while `LayoutStore.Load` steps a
   `Migrations` chain (LayoutStore.cs:161-181). If Addendum C's mode system adds a per-mode layout
   slot (as ADR-0017 §"Consequences" already proposes for Explorer: *"Layout persistence gains a
   per-mode slot"*), it inherits the zone schema's no-migration gap — a schema bump for the new slot
   would silently discard every user's saved zone layout rather than migrating it.
