---
id: design-workbench-loops-wl-s
title: "Design: WL-S — Factory · Review · Understand perspectives, FullWindow retired, kinds, hosts, named layouts and slot migration (ADR-0045)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "workbench-loops / WL-S"
tags: [design, shell, perspectives, docking, layout, surfaces, workbench-loops, wl-s]
links:
  - { to: adr-0045-perspective-set-factory-review-understand, rel: implements }
review-by: "2026-11-18"
summary: >-
  The detailed design for all of ADR-0045: the closed Perspective set re-rowed to factory · review ·
  understand (all docking hosts), the full-window Explore body and its presenter path deleted, every
  existing surface kind re-admitted (Coordination's five under Factory), the notebook/execution
  kinds that split session-document, three hosts and the A↔B↔C unparent spike, named default
  layouts, and the slot migration by stable id. T4 builds §1–§2; T11 builds §4 and §7; T9 builds §5.
---

# Design: WL-S — the Perspective re-row (ADR-0045)

- **Status:** Accepted (gate record below) — T4 `wl-t4-sp3`, 2026-10-18.
- **Spec / architecture:** `docs/specs/workbench-loops-ux.md` §B1 (three places to work), AC-D01a–c;
  ADR-0045 (supersedes ADR-0030's rows, refines ADR-0031/0032); `docs/coordination/coordination-workbench-loops.md`
  reconciliations 2 and 3 (binding).
- **Delivery phase / vertical slice:** WL-S, beside WL-1a. The re-row lands on the serial spine **before**
  WL-1a binds the session document (reconciliation 2).
- **Author / date:** T4 (Claude Code, Opus), 2026-10-18. Grounded at `4b439996`.

## Who builds what

| Section | What | Track | Gate |
|---|---|---|---|
| §1 | Re-row the closed set (three rows, landings, routing order, gestures, rail, title) | **T4** (this track) | three-row build test |
| §2 | Retire `FullWindow`: delete the Explore body, its presenter path and `ExplorerSurface` | **T4** | no-row-uses-`FullWindow` build test |
| §3 | Re-admit every existing kind; Coordination's five under Factory; SH-4.1 on the Factory host | **T4** | kind-admission census; SH-4.1 oracle |
| §4 | `notebook` + `execution` kinds splitting `session-document` | T11 (after T8 joins) | T11's |
| §5 | Three hosts; the A↔B↔C unparent spike | T9 | spike exit 0 + `RESULT.md` |
| §6 | Named default layouts per perspective — constrained, not designed | **unassigned** in the drive plan — a finding for the Leader | — |
| §7 | Slot migration by stable id, `.bak`, drop report | T11 | golden round-trip |
| §8 | Knowledge-inclusive canvas mode in Understand (Ruling 149) | **unassigned** — the Leader assigns it inside WL-S | its own E2E check; WL-S does not close without it |

## Responsibility

One responsibility: **the closed set of places to work, and what each place admits**. A perspective is a
row of data (id, title, order, command, description, landing). Everything else — catalog commands,
the View menu, the palette, the rail, the window title, the routed open, the layout slot — is derived
from the rows and from the `Perspectives` column on each kind row. This slice changes the **rows** and
the **column values**. It does not change the mechanism (ADR-0045 §1(a): "ADR-0030's mechanism … is
unchanged; its rows are superseded").

## Internal contracts (the conventions this conforms to)

All **Verified** by reading the files at `4b439996`.

- `PerspectiveSet` (`src/AiDe.Core/Workbench/Perspectives.cs:61-108`) — static rows, `All`, `Initial`,
  `RoutingOrder`, `ByCommandId`. Catalog commands are derived from `All`
  (`WorkbenchCommands.cs:189-195`): title `"<Title> perspective"`, gesture `Ctrl+<Order>`.
- `WorkbenchLayout.Default(Perspective)` (`ZoneLayout.cs:186-196`) — a switch on `perspective.Id` to a
  per-host table. A per-host table, never a filtered shared seed.
- `SurfaceContentFactory.Kinds` (`SurfaceContentFactory.cs:135-284`) — one row per kind with a
  `Perspectives` allow-list. Row order is View-menu order.
- `PerspectiveMenu.For` / `Resolve` (`PerspectiveMenu.cs`) — a shape interpreter: no switch on a
  perspective or a kind. `Resolve` = the active host if it admits the kind, else the first of
  `RoutingOrder` that does.
- `SurfaceAdmission` (`SurfaceAdmission.cs:84-89`) — `AdmittedBy` reads `RoutingOrder`; this names the
  host in a drop report.
- `LayoutPersistence.SlotPathFor` (`LayoutPersistence.cs:89-104`) — "The map is not uniform and lives
  only here."
- `PerspectiveShell` (`PerspectiveShell.cs`) — retain-never-rebuild; one `shell.mode` line per change;
  focus lands one dispatcher turn after `Loaded` (SH-4.1's control).
- Tests: data invariants are proved by build tests over the rows (`PerspectiveSetTests`), and hosts are
  composed with `DockHost.Create` over a stub factory, never a `Border` stand-in (DC-135).

## Data model

This slice persists no new data. Its model is **reference data plus one current-state envelope**.

- **Bounded context:** the workbench shell (ubiquitous language: *perspective*, *host*, *zone*, *surface*,
  *kind*, *admission*, *landing*, *slot*). A spec word maps one-to-one: *Factory / Review / Understand*
  are perspectives; a *feature child* (Prompt notebook, Coordination, …) is an **admitted kind group**,
  not a perspective (ADR-0045 §2; spec §B1: "they do not determine journey identity").
- **Reference rows (a closed dimension in code):** one row is exactly **one place to work**, identified by
  its stable `Id`. History rule: **none kept in the row** — a row change is a code change with a build
  test; the superseded ids live on only as **slot aliases** (§7) and in this document.
- **Aggregate:** a host's `WorkbenchLayout` (root: the host's `ZoneBackedLayoutService`). Its one
  invariant: **every surface in a host's layout is admitted by that host's row** (enforced at
  `Apply` and at restore by `SurfaceAdmission`; a refused surface is dropped with a report, never
  silently). Hosts are referenced by perspective identity only; no operation spans two hosts.
- **Durable representation:** the existing per-host slot file `<layout>[.<suffix>].zones.json`
  (ADR-0032). One file is exactly **one host's last arrangement**. History rule: **Type-1 overwrite**
  — a recorded decision to discard history: an arrangement is a preference, not a record; §7's one-time
  `.bak` is the only retained prior state.
- **Derive, don't store:** menu, palette, rail, window title, catalog commands, routed open and the
  drop report's host name are all derived from the rows. Nothing caches them.
- **No migration in T4.** T4 keeps every saved file readable by **aliasing the new ids to the old file
  names** (§7). T11 owns the rename, the `.bak` and the report. There is no schema change, so
  expand-migrate-contract applies to file names only: *expand* = T4's alias (old names read and
  written under new ids); *migrate* = T11 renames with `.bak`; *contract* = T11 deletes the alias.

## §1 — The re-row (T4 builds)

| Order | Id | Title | Command | Gesture | Landing | Default layout |
|---|---|---|---|---|---|---|
| 1 | `factory` | Factory | `perspective.factory` | Ctrl+1 | **Left** (see D2) | today's Coding table, unchanged |
| 2 | `review` | Review | `perspective.review` | Ctrl+2 | Center | four empty zones, none collapsed (an empty zone already renders as its rail) |
| 3 | `understand` | Understand | `perspective.understand` | Ctrl+3 | Center | today's Architecture table (Center = Graph, Tree) |

Every row is a docking host; after §2 there is no `Body` column to state it (D5).
`WorkbenchLayout.Default` switches on the three ids with an explicit `review` arm and a throwing
default arm — no silent fall-back to Factory's table (the Patterns Expert's finding 5).

- `All = [Factory, Review, Understand]` — the spec's order (§B1). `Initial = Factory`.
- `RoutingOrder = [Understand, Review, Factory]` — "the reading host wins a shared kind". Today
  `codeviewer` is the only shared kind (Factory + Understand), so a codeviewer open from Review lands in
  Understand, and from Factory stays in Factory (the active host admits it).
- Descriptions (the palette hint), in the product's voice:
  - Factory — "Make the change: session documents, terminals, prompt drafts, and the fleet's coordination panes. The perspective the tool starts in."
  - Review — "Review a change against its intent. Change packets, test evidence and the rigor backlog open here as they arrive."
  - Understand — "Understand the code and its architecture: the graph, the tree, the evidence, and the class, sequence, context and join views."
- Rail glyphs (`Icon<Title>`, PS-R4): `IconFactory` = today's Coding glyph, `IconUnderstand` = today's
  Architecture glyph, `IconReview` = a new stroked tick-in-document path (the review verdict). The
  Explore and Coordination rail glyphs leave `MainWindow.xaml` with their rows; `IconExplore` stays in
  `App.xaml` only if something else still reads it (it is App.xaml's, not this slice's).
- Window title: `"<workspace> — <Title> — AI-DE"` (mechanism unchanged).

**D1 — the ids are the spec's words, lower-case.** Slot names, `shell.mode` lines and command ids carry
them. No translation table between "spec word" and "row id" exists anywhere (DM: one definition) —
**except** the time-boxed store alias in `LayoutPersistence.SlotPathFor` (§7), which carries a
`simplify:` marker naming T11 as its ceiling.

**D2 — Factory lands on the Left until the notebook exists.** ADR-0045 §1(a) says "Factory: Center =
notebook". The notebook is T11's kind (§4). Today the Factory's document is the session document,
which docks in the **Left** (Ruling 83) and is where focus must land (SH-4.2; the SH-4.1 oracle). A
`Center` landing now would land on the empty copy "The session is docked at the left" while a session is
open — a regression of a measured, ruled behaviour. So T4 sets `Landing: Left` and **T11 flips it to
`Center` in the same change that docks `notebook` in the Center**. This is a recorded, time-boxed
deviation, not a re-decision; the build test pins the value so the flip is a visible one-line diff,
and the Factory row carries a `simplify:` marker naming T11's flip.

**D3 — Review admits no kind until WL-5.** ADR-0045 §2 admits nothing to Review by name
(`change-packet`, `test-evidence`, `rigor-backlog` arrive with WL-5/3/6). Admitting `codeviewer` or
`diagnostics` there would invent a Review feature the spec does not name, and would change
`codeviewer`'s routing for no reader. So Review is an honest empty host: no derived View-menu entries,
and its Center copy says what is true (§1 copy below). Every existing kind is admitted **somewhere**
(the census test, §3).

**Center empty copy** (`CenterEmptyState.CopyFor`) — derived from **admission**, never from a
perspective's identity (a switch on rows is what `PerspectiveMenu` forbids; both reviewers):

| The host admits … | Heading | Line |
|---|---|---|
| `session-document` (today: Factory) | unchanged session copy ("No session open." + New session; "The session is docked at the left." …) | unchanged |
| a kind with a derived opener (today: Understand) | "Nothing open here." | "Open a view from the View menu." (true: there is one) |
| no kind (today: Review) | "Nothing opens here yet." | "Switch to Factory (Ctrl+1) or Understand (Ctrl+3)." — the rows that admit a kind, titles and gestures read from the rows |

The third branch retires itself when WL-5 admits `change-packet` to Review; it promises nothing this
build cannot do.

## §2 — Retire `FullWindow` (T4 builds)

Delete, as one HYG-A sweep (no commented-out code, no unused member, no parameter with one value):

- **The `Body` column and the `PerspectiveBody` enum (D5).** With `FullWindow` gone the enum has one
  value and every reader is a constant. `PerspectiveMenu.Offered`'s `CommandScopeKind.DockHost` arm
  becomes `true` (the scope kind itself stays: `Execute` uses it to pick the *active* host over the
  initial one). The filters at `PerspectiveShell.cs:82`, `ShellContrastCensus.cs:184`,
  `ShellContrastCensusTests.cs:97` and the branch at `PerspectiveRail.cs:330` go with it, and so do the
  now-unreachable refusals in `DockHost.Create` (`:65-68`) and `LayoutPersistence.SlotPathFor`
  (`:94-97`): the type can no longer express a non-host body.
- `PerspectiveSet.Explore`, `PerspectiveSet.Coordination` and the `perspective.explore` /
  `perspective.coordination` commands (derived — they go with the rows); `CoordinationDefault()` and its
  `"coordination"` arm in `WorkbenchLayout.Default`, with the stale remark above it.
- `PerspectiveShell`: the `explorerFactory` parameter, `_explorer`, `ExplorerSurface`, the
  full-window branch of `Activate`, `BodyFailed` / `PerspectiveBodyFailure` / `BodyFailedCode`
  (only a full-window body could fail to build; hosts are built at construction), `RefuseWithoutHost` /
  `NoHostCode` (unreachable when every row is a host), the full-window branch of `Announcement`, and
  `Escape()` / `Previous` (D4). `HostFor` and `ActiveHost` become non-nullable.
- `WorkbenchDiagnostics.ShellMode`'s `firstEntry`, `outcome` and `errorCode` parameters (each now has one
  value) — seam request `req-01M58JG10ZKHW75YME0P6G3DG2`; if refused, the constant values are passed and
  recorded as a residual.
- `ExplorerSurface.cs`; `WorkbenchShell.CreateExplorerGraph` and `ReadNodeContentAsync` (their only
  caller is the explorer factory in `MainWindow`); the `MainWindow` explorer factory, the Explore
  refresh on entry, the Escape-from-Explore key handler and `EntryFocusFor`'s Explore branch;
  `PerspectiveRail.ShowFailure` and its error state (its only raiser was `BodyFailed`);
  `MainMenuBuilder`'s `perspective.explore` icon arm; `App.xaml`'s `IconExplore` (seam request
  `req-01M58JG10ZKHW75YME0P6G3DG2`).
- Orphans outside T4's first owned paths — `NodeReaderView.cs`, `ExplorerNodeMenu.cs`,
  `NodeReaderEdgeRowLayoutTests.cs`, and `AiDe.App.CanvasProbe/Program.cs:229` (constructs
  `ExplorerSurface`) — seam request `req-01M58HXY8HGKDVTX393PN9TDP3`, **granted** by the Leader: T4
  deletes the first three and drops only the ExplorerSurface step from the probe.

**D4 — `Escape()` and `Previous` are deleted; the behaviour is kept as a test.** ADR-0045 §1(b) says
"`Escape()` and the previous-perspective slot are retained as perspective-level behaviour (US-C1)".
US-C1 defines both narrowly: the slot "exists for Escape-from-Explore and the drill-to-node return
only", and "Escape reaches the perspective level only from the Explore surface root … never from a
host". Verified at `4b439996`: `Escape()`'s only production caller is the Explore-guarded key handler
(`MainWindow.xaml.cs:107-117`), which §2 deletes; `Previous` is read only inside `Escape()`
(`PerspectiveShell.cs:301`); no drill-to-node return exists in `src/`. Kept, both would be dead
(HYG-A) — a method that always answers *no* and a slot nothing reads. The behaviour US-C1 keeps —
**Escape never switches the perspective from a host**, and activating the active perspective is a
no-op — is pinned by tests instead (a rendered-window test pressing Escape in each of the three hosts;
the existing no-op test). The slot comes back with its first real reader. This is a **recorded
conformance deviation** from the ADR's wording, not its intent; both adversaries co-signed it.

**D5 — the `Body` column is deleted; the no-`FullWindow` rule becomes a build fact.** ADR-0045 §1(a)
says "each `PerspectiveBody.DockHost`" and §1(b) asks for "a build test that no row uses `FullWindow`".
A one-value enum keeps the column as speculative generality (ADR-0045 plans no other body) while the
guards it would need are deleted — the contradiction both adversaries found. Deleting the column meets
the ADR's intent more strongly: no row *can* use a full-window body. The build test that the ADR asks
for is kept as a reflection and source test — `Perspective` has no `Body` member, Core has no
`PerspectiveBody` type, and no `.cs` file under `src/` names `FullWindow` — so re-adding a full-window
body fails the suite. Recorded conformance deviation from §1(a)'s wording.

## §3 — Kind re-admission (T4 builds)

Every one of today's 20 kind rows (`SurfaceContentFactory.cs:135-284`) is admitted somewhere:

| Kind(s) | Was | Now | Note |
|---|---|---|---|
| `canvas`, `view`, `classdiagram`, `sequence`, `contexts`, `joins`, `solution-tree`, `entry-points` | Architecture | **Understand** | §2's "visualizer is not a composite kind" |
| `codeviewer` | Coding + Architecture | **Factory + Understand** | the one shared kind; Understand wins routing |
| `terminal`, `prompt`, `search`, `diagnostics`, `session-document`, `console` | Coding | **Factory** | `session-document` stays a kind (reconciliation 3) |
| `sessions`, `board`, `leaderboard`, `ledger`, `daydreams` | Coordination | **Factory → Coordination** | the Loomkeeper five, as a group, `Zone: Center` |

- **"Factory → Coordination"** is the group: the five rows move to the **end** of `Kinds`, contiguous,
  under a `// ── Factory → Coordination ──` section. Row order is View-menu order, so Factory's View
  menu lists its own kinds first (New search · New code viewer · Show diagnostics) and the fleet group
  after them (Show terminal sessions · Show message board · Show leaderboard · Show ledger · Show
  daydreams) — the Patterns Expert's finding 7; final order is UX's to amend. The **left hierarchy**
  that will render "Coordination" as a feature child (ADR-0045 §2) is not built here.
- **They dock in the Center.** Each of the five rows gets `Zone: ZoneId.Center` — the existing zone-rule
  column (Rulings 83/89; `WorkbenchShell.OpenReferenceDocument` reads it before the placement policy).
  Without it, "Show ledger" with a session document active in the Left would tab the ledger into the
  Left stack beside the session (`DocumentPlacementPolicy.Decide` follows the active surface), and the
  Left's active tab — Factory's landing — would become the ledger: SH-4.2's regression (the Patterns
  Expert's finding 3).
- **They are admitted, not defaulted.** Factory's default stays Coding's table (Ruling 55d: "never a
  fleet-view tab strip beside a session"). The operator reaches them through View → Show … (Instances.One).
- **Ruling 84's isolation is superseded** ("no composer on this bench" no longer holds — they now share
  a host with session documents); this relocates Loomkeeper's Observatory bench
  (`architecture-loomkeeper` §4), already flagged by ADR-0045.

**SH-4.1 on the Factory host.** SH-4.1's shape: *after a body is (re)parented, the view's active content
is whichever pane control realized last, and zone occupancy decides that order* — so a model-stated
landing can be overridden when the occupancy changes. Folding the five into Factory is exactly an
occupancy change: Factory can now hold Loomkeeper tabs in its Center beside a session document in its
Left. The oracle, re-run on the Factory host:

1. **Landing by occupancy** (`PerspectiveShellTests`, headless on the product's `DockHost`): Factory with
   a session in the Left **and** Ledger · Leaderboard · Message board in its Center lands on the session
   document; with the Left collapsed it lands on the Center's active tab (the ledger the operator
   activated), never on whichever pane realized last. Red first: before the re-row, Factory's host
   refuses the ledger.
2. **Placement by the zone rule**: every Loomkeeper row carries `Zone: Center`, and a shell-level open of
   `surface.show.ledger` with a session document active in the Left places the ledger in the Center and
   leaves the Left's active tab the session.
3. **SH-4.1's named oracle**: `ASessionDocumentIsShownWhereTheOperatorIsTests.ARestoredSessionDocumentIsRevivedAndBoundAtWorkspaceOpen`
   green against the `factory` row (the probe's `mode=` lines re-pinned to `factory`).

## §4 — `notebook` and `execution` (T11 builds; designed here)

- Two new kind rows, both admitted to **Factory** only: `notebook` (Instances.Many, opened by the
  `session.new` verb's successor, `Zone: Center`) and `execution` (Instances.Many, one per attempt,
  `Zone: Bottom`). Captions carry object identity and scope (ADR-0045 §5): `"Notebook — <journey>"`,
  `"Execution — <attempt>"`.
- They **bind** T8's models (`Dictionary<NotebookId, NotebookModel>`, the attempt host); the surface never
  owns a token or a queue slot (ADR-0045 §4; AC-D01a).
- `session-document` placements in a saved slot map to one `notebook` (same identity) plus, when an
  attempt was live, one `execution`; `prompt` placements map to `notebook` drafts or drop with a report.
  `session-document` and `prompt` then join `RetiredKinds` with the sentence the report shows.
- T11 flips Factory's `Landing` to `Center` (D2) in the same change, and removes the `simplify:` marker.
- New kinds follow the row contract: a UIA name, an allow-list entry, a derived or verb opener, an entry
  in `KindAllowListsTests`' column table.

## §5 — Three hosts and the A↔B↔C spike (T9 builds)

- The mechanism is unchanged: `PerspectiveShell` already holds three `DockHost`s
  (`WorkbenchShell.Hosts`). `DockHost.cs:12-13`'s "composes it twice" comment is corrected to "once
  per row" in T4's change.
- T9 extends `spikes/second-dock-host-unparent/` to cycle A → B → C → A … ×3 with a live WebView2 and a
  raw `HwndHost` in each host, asserting the same `CoreWebView2`, an identical HWND and zero
  `DestroyWindowCore`; it records private bytes per host idle and with a 100k-character notebook in
  `RESULT.md`. Exit 0 or a measured failure — the three-host memory budget is **Inferred** until then.

## §6 — Named default layouts (not designed here)

ADR-0045 §6 decides only *that* named layouts exist (*editor + execution*, *execution below*, *focus
editor*, *investigation*), as data, never locked templates, with `ZoneId` unchanged. They are built on
§4's kinds, which do not exist yet, so an API now would be speculative (the Simplifier's finding 8). One
constraint is fixed: **one per-row dispatch** — named layouts extend `WorkbenchLayout.Default`'s
per-row table rather than adding a second switch on the perspective (the Patterns Expert's finding 5).
**No track owns §6** in the drive plan — a finding for the Leader.

## §7 — Slot migration by stable id (T11 builds; T4 lays the alias)

The slot file name is the only key: the envelope stores zones and surfaces, never a perspective id
(`ZoneLayoutStore.cs:21,35,40`).

- **T4 — a time-boxed legacy persistence-key alias** (not Parallel Change's *expand*; the Patterns
  Expert's finding 4). `SlotPathFor`'s suffix rule: empty for `PerspectiveSet.Initial` (Factory keeps
  Coding's grandfathered `<layout>.zones.json` **permanently** — ADR §7's "slot `coding` → `factory`" is
  met by identity, there is no file to rename), `.architecture` for Understand (marked `simplify:`,
  ceiling T11), otherwise `.` + the id (`<layout>.review.zones.json`, new). A saved arrangement restores
  with no drop. `<layout>.coordination.zones.json` is left on disk, unread, until T11 folds it.
- **T11 — migrate and contract**, merged behind a `.bak` (acceptable under the single-user local-tool
  posture; recorded): rename `architecture` → `understand` (read the new name, fall back to the old,
  write the new); fold `coordination`'s surfaces into `factory` — **every** surface to Factory's
  Center (its Left `sessions` included, never Factory's Left: the landing rule of §3), appended as
  tabs; `explore` → none; one-time `.bak` per renamed or folded slot through the existing
  `ZoneLayoutStore.Save(layout, backupPath)`; map `session-document` placements per §4; drop anything
  no longer admitted **with a report** (DC-219's control; `ZoneRestoreReport`); recover an off-screen or
  unusable placement to the perspective default (AC-D01b). Then remove T4's `.architecture` alias.
  Golden round-trip test over today's saved files; an oracle case with Factory's Left holding a session
  and the folded `sessions` in the Center, landing on the session.

## §8 — Knowledge-inclusive representation (WL-S backlog; Ruling 149)

- **A committed WL-S row, not a WL-6 follow-up.** Understand's canvas admits the knowledge/spec corpus
  as a **mode of the admitted `canvas` kind**, chosen through the existing representation selector
  (ADR-0045 §2: "the visualizer is not a composite kind … linked selection … is the 'representation
  selector' without a new surface") — never a second graph surface and never a new kind.
- **Where it attaches:** the `ExcludeKnowledge` seam on the docked canvas's view model
  (`WorkbenchShell.cs:1386`, `graph.ExcludeKnowledge = true`; the parameter on the existing
  neighbourhood query). The mode toggles that parameter per canvas instance; no second store, no
  second query.
- **Its own E2E check:** in the running app, Understand → the canvas in its knowledge-inclusive mode
  shows a known `docs/knowledge/` node with its typed edges, and switching the mode back hides it.
  **WL-S does not close until that check is green.**
- **Owner:** not built by T4 (Ruling 149 condition 3; T4's done-when is unchanged). The Leader assigns
  it inside WL-S (T11's window is the natural home, beside §4).

## Patterns

| Pattern | Where | Why (both reviewers) |
|---|---|---|
| **Static registry / closed set as data** (table-driven rows) | `PerspectiveSet`, `Kinds` | existing; the re-row is a data edit, every surface follows |
| **Derived projection** (one source, many views) | catalog, menu, palette, rail, title, Center empty copy | existing; no per-perspective list to drift; the empty copy now derives from admission |
| **Ordered fallback list** (first admitter wins; a Chain-of-Responsibility shape) | `RoutingOrder` + `Resolve` | existing; the routing rule is an ordered list |
| **Legacy persistence-key alias** (anti-corruption mapping at the store boundary) | `SlotPathFor` (§7) | time-boxed to T11, `simplify:` marker |
| **Retained-view cache** (keep-alive bodies, retain-never-rebuild) | `PerspectiveShell` | existing; three hosts, no full-window body |
| **Null-object host** | Review | an honest empty host until WL-5 (D3) |

Rejected: a translation table spec-word → old id beyond the store alias (two definitions); a
`Coordination` feature-child abstraction now (YAGNI — §2's left hierarchy is its slice); keeping
`FullWindow`, a one-value `PerspectiveBody`, `Escape()` or `Previous` "for later" (HYG-A; D4, D5); a
§6 API before its kinds exist.

## Solution-Selection Ladder

Rung 2 (reuse what lives here) for every change: rows, kind column, the existing zone-rule column,
`WorkbenchLayout.Default`'s per-row table, the existing slot map. No new type, dependency or
abstraction in T4; net code goes down.

## Change-surface list (E7)

- **store:** slot file names — `LayoutPersistence.SlotPathFor` and its `?? PerspectiveSet.Coding`
  fallback (`:106`).
- **model:** `Perspectives.cs` rows (and the deleted `PerspectiveBody`); `ZoneLayout.Default` arms
  (`CoordinationDefault` deleted; Review's arm added).
- **service:** `SurfaceAdmission.AdmittedBy` via `RoutingOrder`; `WorkbenchCommandCatalog` derivation.
- **projection:** `SurfaceContentFactory.Kinds` column, order and zone rule; `PerspectiveMenu.Offered`.
- **client type:** `DockHost`, `PerspectiveShell`, `WorkbenchShell.Hosts` and host properties,
  `WorkbenchDiagnostics.ShellMode`.
- **UI:** `PerspectiveRail` (incl. `:330`), `MainWindow.xaml` glyphs and comments, `MainWindow.xaml.cs`
  composition and title, `MainMenuBuilder` icons, `CommandPalette` initial menu, `CenterEmptyState`,
  `App.xaml` `IconExplore`, `DESIGN.md`'s perspective rows (seam request `req-01M58JG10ZKHW75YME0P6G3DG2`).
- **compute readers:** `shell.mode` lines; `ShellContrastCensus` (`:184`, named sites); `ComposerProbe`
  session render and its `--explorer` replay step; `CanvasProbe`; every test that names a row.

## Error and concurrency model

Unchanged. All of it runs on the UI thread; a switch is a content swap. The only error paths this slice
removes are the full-window build failure and the no-host refusal, which can no longer occur.

## Failure-mode analysis

| Mode | Disposition |
|---|---|
| A saved slot stops restoring after the re-row | **prevent** — §7 alias; `SlotPathFor` test pins the three file names |
| A kind left with no admitting host (silently unopenable) | **prevent** — `KindAllowListsTests` column table: every row admitted, the whole column literal |
| A fourth row, or a full-window body, added later | **detect** — the three-row build test; the no-`FullWindow` reflection + source test |
| `WorkbenchLayout.Default` silently gives a row another row's table | **prevent** — explicit arms, throwing default; every default seeds with no drop |
| SH-4.1: Factory's landing overridden by the Loomkeeper tabs realizing last | **prevent** + **detect** — the deferred `EntryFocus` turn; §3 oracle 1 and 3 |
| SH-4.2: a Loomkeeper pane opens beside the session document in the Left and takes the landing | **prevent** — `Zone: Center` on the five; §3 oracle 2 |
| A routed open lands in the wrong host after the order change | **detect** — `Resolve` tests for `codeviewer` from each host and the five from every host |
| Review shows a copy that promises an action it cannot take | **prevent** — copy derived from admission; names only rows that admit a kind; `CenterEmptyCopyTests` |
| Stale `coordination.zones.json` | **accept** until T11 — the file is unread; residual: an operator's Coordination arrangement is not shown until T11's fold |
| Users lose the Explore full-window graph and reader | **accept** — ADR-0045 Consequences; the Graph, Tree and codeviewer remain in Understand |

## Adversarial analysis (STRIDE-lite)

No new trust boundary. The slot files are local, written and read by the same single-user process
(the repo's local-tool posture). **Tampering** with a slot file: **accept** — the restore already admits
only known, admitted kinds and drops the rest with a report (existing control). No other STRIDE threat
applies.

## Privacy analysis (LINDDUN-lite)

No personal data: perspective ids, kind ids and zone geometry only.

## UI and interaction design

- **Medium:** WPF desktop (Windows), the existing shell; tokens unchanged (`DESIGN.md`); no new colours.
- **Rail:** three items, a vertical tab list to UIA, selected state real, 44×44 targets, roving
  tab-stop (unchanged mechanism). Names "Factory perspective", "Review perspective", "Understand
  perspective" (derived). Glyphs `IconFactory` (Coding's), `IconReview` (new stroked tick-in-document),
  `IconUnderstand` (Architecture's). The rail loses its error state with `BodyFailed`.
- **States:** Factory — first-run (No session open + New session), session docked, collapsed-session;
  Review — empty (the only state until WL-5); Understand — default (Graph active), Nothing open here.
  Loading/error belong to the panes, unchanged.
- **Keyboard:** Ctrl+1/2/3; Ctrl+4 is unbound (the build test asserts it). Escape never switches the
  perspective (D4).
- **Motion / reduced motion / performance:** unchanged; a switch stays a content swap
  (`shell.mode.shown` measures it).
- **Craft gate:** `ui-craft-gate.py` is not re-run by T4 — no token, colour, font or radius changes; the
  one new glyph is a stroked path under the existing item template. Recorded deviation.

## Telemetry

Unchanged events; new values. `shell.mode` carries `mode`/`from` = `factory|review|understand`; the
`failed`/`refused` outcomes and `AIDE-PERSPECTIVE-BODY-FAILED` / `AIDE-PERSPECTIVE-NO-HOST` codes are
retired with their paths (and, if seam request `req-01M58JG10ZKHW75YME0P6G3DG2` is granted, the
one-valued `first_entry`/`outcome`/`error_code` fields). `EverySwitch_WritesOneShellModeLine_WithItsFullShape`
re-pinned.

## Test plan (Testing Strategy)

- **D0 hygiene:** build clean (`AiDe.sln`); no dead code — no match in `src/` or `tests/` for
  `FullWindow`, `PerspectiveBody`, `ExplorerSurface`, `NodeReaderView`, `CoordinationDefault`,
  `PerspectiveSet.(Coding|Explore|Architecture|Coordination)`, `perspective.(explore|coordination)`,
  `IconExplore`.
- **Data invariants (build tests, red first):** `All_HasExactlyThreeRows_FactoryReviewUnderstand_EachWithABoundGesture`;
  `NoRowUsesFullWindow_TheBodyColumnIsGone`; routing order Understand · Review · Factory; landings
  Left · Center · Center (D2 pinned); Ctrl+4 unbound.
- **Admission census (red first):** `KindAllowListsTests` — the five under Factory as one contiguous group
  with `Zone: Center`, resolving to Factory from every host; the whole column literal; Review admits none.
- **SH-4.1 oracle on Factory (red first):** §3 oracles 1–3.
- **Slot alias (red first):** `SlotPathFor` gives Coding's, Review's new and Architecture's file names.
- **Escape (D4):** the window carries no perspective-level Escape route.
- **Re-scoped old-row tests:** the red list (every test that named an old row, the Explore body or the
  explorer), then green. Recorded in `docs/proof/workbench-loops-wl-s/sp3/`.
- **Runtime evidence:** build Release, launch, Ctrl+1/2/3, the rail shows three rows, a Coordination kind
  opens under Factory — observed and recorded in the proof README.

## Conformance notes

- ADR-0045 §1(a): conformed except **D2** (Factory landing Left, time-boxed to T11) and **D5** (the
  `Body` column is deleted rather than every row stating `DockHost`; intent met more strongly).
- ADR-0045 §1(b): conformed except **D4** (`Escape()` and `Previous` deleted, their US-C1 behaviour
  pinned by tests). Both deviations co-signed by the Patterns Expert and the Simplifier; reported to the
  Leader in T4's exit evidence for the Owner's notice.
- ADR-0045 §7: Factory keeps Coding's file name permanently (met by identity).
- No HTML sibling for this document: the docs HTML surface cap is full (plan, "HTML surface cap").

## Flagged risks and residual unknowns

- §6 has no owning track (finding for the Leader).
- The three-host memory budget is **Inferred** until T9 runs.
- **Capability loss — you can no longer browse knowledge and spec documents as graph nodes inside the
  app (Ruling 149).** The docked graph canvas sets `ExcludeKnowledge = true`
  (`WorkbenchShell.cs:1386`; `GraphProjection.cs:93`: "never the knowledge/spec corpus Explore already
  reads"); the full-window Explorer's graph was the one canvas that showed knowledge nodes, so
  retiring it (ADR-0045 §1(b)) leaves no in-app view of them. ADR-0045's Consequences accepted losing
  the full-window arrangement, not the corpus. **Interim path:** the Docs Explorer, `docs/index.html`
  (hierarchy · graph · mind map), shows the same corpus. Ruling 149 accepts the loss bounded and owned
  by WL-S — see §8.
- **Found in implementation — the deletion cascades.** An orphan-type census over `src/AiDe.App` +
  `src/AiDe.Core` (before vs after) shows `HtmlSandboxHost` and `NodeReaderContentState` orphaned once
  `NodeReaderView` goes; seam request `req-01M58KFWAM77V2V21PBYKKA5F4`.
- Seam requests (outcomes in `docs/proof/workbench-loops-wl-s/sp3/README.md`): seam 1 granted; seams
  2–5 expired unreceived and their recorded fallbacks were applied. Residuals left by fallback:
  `WorkbenchDiagnostics.ShellMode` keeps its one-valued `firstEntry`/`outcome`/`errorCode` (constants
  passed), `App.xaml`'s `IconExplore` is unread, `DESIGN.md` still names the old rows, and
  `HtmlSandboxHost` / `NodeReaderContentState` have no product user.

## Status and next action

| | |
|---|---|
| **Completed** | §1–§5 and §7 designed; §6 constrained; T4's part (§1–§3) ready to build |
| **Remaining** | §4 + §7 (T11), §5 (T9), §6 (unassigned) |
| **Best next action** | `/implement` §1–§3 red-first in T4 |

## Gate record

Adversary Mode, 2026-10-18, two read-only sub-agents over the draft at `4b439996`.

| Reviewer | Verdict | Findings → disposition |
|---|---|---|
| **Patterns Expert** | PASS-WITH-CONDITIONS (advisory) | 1 D4 delete `Escape()`/`Previous` → **accepted** (D4). 2 delete the one-value `Body` column or keep the guards → **accepted**, column deleted (D5). 3 Coordination's Left `sessions` and "Show …" placement can take Factory's landing → **accepted**: `Zone: Center` on the five, §3 oracle 2, §7 fold rule. 4 the alias is not Parallel Change → **accepted**, renamed and marked `simplify:`; D1 amended. 5 `Default`'s silent fall-back and a second dispatch in §6 → **accepted**: explicit arms, throwing default, §6 constraint. 6 E7 gaps (`DESIGN.md`, `PerspectiveRail.cs:330`, `LayoutPersistence.cs:106`, `CoordinationDefault`) → **accepted** into E7/§2; `DESIGN.md` via seam request. 7 View-menu order → **accepted**: the group moves last. 8 `CenterEmptyState` would switch on rows → **accepted**: copy derived from admission. 9 two mis-named patterns → **accepted**, renamed. D2 and D3 confirmed correct. |
| **The Simplifier** | BLOCK (soft) → **cleared** by applying conditions 1–4 | 1 delete `Escape()` and `Previous` → **accepted** (D4). 2 delete `PerspectiveBody` and `Body` → **accepted** (D5). 3 guard-deletion rationale false while the enum lives → **resolved** by D5. 4 the sweep misses `ShellMode`'s one-valued parameters, `CoordinationDefault`, `IconExplore`, stale comments; D0 grep too thin → **accepted**; the D0 grep extended; the out-of-path items via seam request. 5 Review needs its own arm; no collapsed flags → **accepted**. 6 Review copy keyed on data, promising nothing → **accepted**. 7 Factory keeps the empty suffix permanently; `.bak` through `ZoneLayoutStore.Save` → **accepted** into §7. 8 cut §6 → **accepted**. 9 `ShellContrastCensus.cs:184` → **accepted** into E7. |

Mutual check: the two deletions that change ADR-0030's row shape (D4, D5) were proposed independently by
both reviewers. The author did not self-clear: every veto above is cleared by an applied change, and the
two ADR-wording deviations are recorded for the Owner's notice. Residual risk: T11's fold is the next
occupancy change and carries its own SH-4.1 oracle case (§7).
