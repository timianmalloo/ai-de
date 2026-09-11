---
id: adr-0030-perspective-registry-and-allow-lists
title: "ADR-0030 — The Perspective set is a closed Core data row set; the allow-list is a column on the App's surface-kind rows; menu, palette, rail and routing are derived from the join"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [architecture, ui-shell, perspective, allow-list, menu, command-catalog, addendum-c]
links:
  - { to: architecture, rel: implements }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: adr-0017-primary-view-mode, rel: refines }
  - { to: note-addendum-c-current-state-inventory, rel: relates-to }
  - { to: note-addendum-c-menu-derivation-rule, rel: relates-to }
  - { to: note-addendum-c-inadmissible-kind-routing, rel: relates-to }
review-by: 2027-03-11
review-suggested:
  - { by: adr-0017-primary-view-mode, on: 2026-09-11, reason: "ADR-0017 accepted as amended (Ruling 52): the closed set is the Perspective set; a body may be a docking host; second-host clause discharged by spikes/second-dock-host-unparent" }
summary: >-
  Perspectives are three data rows in Core (id, order, body kind, gesture) with a fixed routing
  order; each App surface-kind descriptor row gains an explicit, non-empty allow-list column; the
  rail, the View menu's perspective radio, the derived "New/Show <Title>" entries, the palette rows
  and a routed kind-open are all computed from the join of those two row sets — never a second
  hand-written list. Rejected: a per-command Modes column on the catalog, a per-perspective tuple
  list, a switch arm.
---

# ADR-0030: The Perspective set is a closed Core row set; the allow-list is a column on the kind rows; everything else is derived

- **Status:** Accepted · **Date:** 2026-09-11 · **Deciders:** node A1 of `plan-addendum-c-modes`
  (session `addendum-c-chain`), the Enterprise Architect and Patterns Expert in Peer Mode; attacked
  at the gate (record in `docs/architecture.md` §Addenda C and D gate)
- **Context spec/architecture:** `spec-addendum-c-perspectives` §A6, §A7, §B3, US-C1, US-C3, US-C4;
  Rulings 22, 50, 52c, 54, 55b, 58–61; ADR-0017 as amended

## Context

Addendum C introduces the **Perspective** — the use case the whole tool is in — as a closed set
(Coding · Explore · Architecture) with, per perspective, an **allow-list** over the existing surface
kinds, a **derived** menu contribution (Ruling 55b: *never a second hand-written list*), a default
layout and a persistence slot. Ruling 52c fixes where the allow-list lives: *"expressed as a column
on the existing descriptor rows"* — the rows being `SurfaceContentFactory.Kinds`
(`src/AiDe.App/Workbench/SurfaceContentFactory.cs:107-128`, eighteen rows) **[Verified]**. Today
nothing in the shell knows which job the operator is doing: `WorkbenchCommandCatalog.All` has no
scoping field (`src/AiDe.Core/Workbench/WorkbenchCommands.cs:24-30`) **[Verified]**, the palette
lists the whole catalog (`CommandPalette.cs:152`, inventory §2) **[Verified]**, and
`MainMenuBuilder.Layout` is a static tuple list that already mixes shell-mode, Coding and
Architecture commands — while its own `_Terminal` entry shows the derived pattern and says why a
second list is wrong (`MainMenuBuilder.cs:89-96`) **[Verified]**.

The load-bearing question is **where the perspective data lives and how many places can disagree
about it**. Two row sets exist in two assemblies: the command catalog (Core) and the kind descriptors
(App). The allow-list must sit on the kind rows (Ruling 52c); the three perspective *commands* must
sit in the catalog (AR5: the rail and the menu run catalog commands); the derived "New/Show" entries
need both. LOA P2 (determinism at the floor) and DM7 (derive, don't store) govern: one definition of
each fact, every surface a projection of it.

## Decision

We will:

1. **Add a closed Perspective row set to Core** — `AiDe.Core/Workbench/Perspectives.cs`: a record
   `Perspective(Id, Title, Order, Body ∈ {DockHost, FullWindow}, CommandId)` and `PerspectiveSet.All`
   with exactly three rows in the order **Coding · Explore · Architecture**, plus the **routing order
   for a kind-open the active perspective does not admit: Architecture · Coding** (US-C3; the
   requester is always a reading surface, so the reading host wins a shared kind). Pure data, no
   WPF; constructible in any test. `Coding` is the initial active row. The three perspective commands
   (`perspective.coding` · `perspective.explore` · `perspective.architecture`, replacing
   `shell.toggleExplorer`) are **derived into `WorkbenchCommandCatalog.All` from this row set**, each
   carrying its bound single-stroke gesture (US-C10: `Ctrl+1/2/3` proposed; D1 fixes), so the catalog
   cannot list a perspective the set lacks.
2. **Add an explicit `Perspectives` column to the App's `SurfaceKind` row** — a non-defaulted
   `IReadOnlySet<string>` of perspective ids, **plus** the `Instances ∈ {One, Many}` fact §A7 needs
   for "Show" vs "New". Every row states its set; **an empty set fails the build test** (US-C3
   b5: an unreachable kind cannot be created by omission). The membership is §A7's table as ruled
   (Rulings 59–61): Coding admits `session-document · terminal · prompt · sessions · board ·
   leaderboard · ledger · daydreams · search · codeviewer · diagnostics`; Architecture admits `canvas
   (kind-filtered) · view · inspector · classdiagram · sequence · contexts · joins · codeviewer`;
   Explore admits no docked kind (its body is not a host).
3. **Derive every other surface from the join**, computed by one App function
   (`PerspectiveMenu.For(perspective, Kinds, catalog)`), and read by all consumers:
   - the **rail** (Addendum C §B2: one primary action, then the three destinations in row order);
   - the **View menu's perspective radio group** (the active row checked; PS-M1 top-level names);
   - the **allow-list-derived entries**: one per admitted kind — *"New `<Title>`"* for a many-instance
     kind, *"Show `<Title>`"* for a one-instance kind — under View, except `prompt` (the Prompt menu)
     and `terminal` (its opener is the entry verb `terminal.new`, File);
   - the **body-conditional entries** (Edit/Window/tab navigation iff the body is a host;
     `focusCanvas` iff the body holds a canvas; `raiseDispute` and the Prompt menu iff Coding) — a
     rule keyed on the perspective row's `Body`, not a list;
   - the **palette rows** — exactly the commands the menu offers in that perspective plus the entry
     verbs (US-C4 b3: there is no palette-only set);
   - the **routed kind-open** — `Resolve(kind, active) = active if admits else first of routing
     order that admits`, one function with one test (US-C3).
4. **Rename `ShellViewMode` → `Perspective` only in the commit that implements this ADR** (Ruling 50):
   the Core row set is the new name; the App enum is deleted, not aliased.

## Alternatives considered

- **A `Modes`/`Perspectives` column on the command catalog row** (inventory finding 8's suggestion):
  rejected — the "New/Show `<Title>`" commands are *derived from the kind rows*, so scoping them on
  the catalog row would be a second home for the same fact (DM-A), and a kind added to a perspective
  would need two edits that nothing checks against each other (the defect Ruling 22 removed for
  kinds). Body-conditional and entry-verb commands are scoped by a *rule* on the perspective's body,
  which no column expresses.
- **A per-perspective tuple list in `MainMenuBuilder` (`("Coding", [...]), ("Architecture", [...])`)**:
  rejected — Ruling 55b forbids it by name; it is the `_Terminal` comment's "second list" three times
  over, and the US-C4 mutation test (append a test-time kind row admitted only by Architecture; its
  entry appears there and nowhere else *with no edit to the builder*) cannot pass against a list.
- **The Perspective set in App only (an enum beside `ShellModeController`)**: rejected — the three
  perspective commands must be catalog rows (Core) derived from the set, or the catalog carries a
  hand-listed copy; and the spec's seam (*"a model the shell projects, constructible without
  `MainWindow`"*, US-C1) is cheapest as plain Core data.
- **Kind descriptors moved wholesale to Core**: rejected — their `Build` delegates are WPF; moving the
  data half alone would split one row into two files that must agree (the `KnownKinds`/switch defect
  Ruling 22 closed). The allow-list column stays on the row that builds the kind.
- **A switch expression over perspective in the builder**: rejected — Ruling 22's class; a switch arm
  is a list with worse tooling.

## Consequences

- **Positive:** adding a kind to a perspective is editing one row; a test proves the menu, palette
  and routing follow (US-C4 mutation test); the rail, menu radio, palette, window title suffix and
  status strip cannot disagree about the active perspective because all read one row set (E7
  consistency, Addendum C §B7); `daydreams` becomes reachable by construction (Ruling 60);
  `contexts`/`joins` gain their first door (inventory finding 2).
- **Negative / accepted:** the App row gains two mandatory members, so every existing row edit in
  the implementing commit is one atomic change (eighteen rows, all in one file); the catalog's
  perspective rows are derived at type-initialisation, so a perspective set change is a Core change —
  intended (the set is closed).
- **Follow-ups / new risks:** the four colliding chord strings (US-C10) are a pre-existing defect the
  derived entries inherit; the uniqueness collector lands red-first in the same slice. D1 may place
  Architecture's derived entries under a differently named menu without changing the rule (PS-M1).

## Falsifying tests (each red until the slice lands; each names its oracle)

1. `PerspectiveSet.All` has exactly three rows in order Coding · Explore · Architecture, each with a
   non-null body kind and a catalog command whose gesture `KeyGestures.For` binds; a fourth row, a
   missing command, or an unbound gesture fails (US-C1, US-C10). *Headless, Core.*
2. Every `SurfaceContentFactory.Kinds` row has a non-empty `Perspectives` set and the sets equal
   §A7's table as ruled; an empty set fails (US-C3 b5). *Headless, App.*
3. **The mutation test:** appending a test-time kind row admitted only by Architecture makes its
   "New/Show" entry appear in Architecture's View menu and palette rows and nowhere else, with no
   edit to the builder; the expected menu per perspective equals §B3's literal table (US-C4).
   *Headless, App (`MainMenuTests`' successor).*
4. `Resolve("codeviewer", Explore) == Architecture`; `Resolve("sequence", Coding) == Architecture`;
   `Resolve("terminal", Architecture) == Coding`; an in-body node action never calls `Resolve`
   (US-C3). *Headless.*
5. The palette's rows in each perspective equal the menu's commands ∪ entry verbs; a row for
   `workbench.newClassDiagram` while Coding is active fails (US-C4 b3). *Headless.*

## LOA mapping

Tier **T0** throughout — a closed lookup table, a join and a projection; no model. Patterns
(named as the Patterns Expert corrected them): **descriptor rows / Smart Enum** (a 3-row static
array with a closed set — the repo's Ruling-22 vocabulary; not a runtime Registry), **Derived View /
CQRS read model** (menu, palette, rail, title are projections of one row set), **Table-Driven
Method** (the routing order is a two-element ordered list on a row — data-driven dispatch, not
Strategy). C4 typed boundaries: the row records are the contract; C9: no duplicated list.

## Evidence

- **Verified:** `SurfaceContentFactory.cs:107-128` (eighteen rows, `SurfaceKind(Kind, Build,
  Windowed)`); `WorkbenchCommands.cs:24-30` (`Menu` is the only placement field);
  `MainMenuBuilder.cs:69-99` (the static tuple list; the derived `_Terminal` rows at `:89-96`);
  `CommandPalette.cs:152` (live-filters the whole catalog); `ShellModeController.cs:6-11` (the
  two-valued enum) — all read on `feature/addendum-c` at `a3f760a3`.
- **Rulings:** 22 (kinds are rows), 50 (vocabulary; rename timing), 52c (allow-list as a column),
  54/59/60/61 (membership), 55b (derived menu), 58 (Explore's selector cut; UML/ER kinds routed) —
  `note-addendum-c-council-rulings`.
- **Inferred:** that the D1-fixed gestures stay `Ctrl+1/2/3` (free today, computed over the catalog
  in Addendum C US-C10); the ADR binds *a bound single-stroke gesture per perspective*, not the keys.
- **Spec supersession this ADR records (quoted; a finding for the Owner):** Addendum C §A6's
  "realised as" note (`:391-396`) — *"the existing `ShellViewMode` gaining a third value with
  `ShellModeController` keeping its shape (79 lines today)"* — is superseded by a Core
  `PerspectiveSet` row set (rule 1) and, in ADR-0031, by `PerspectiveShell` as a command mediator; the
  note's intent (no reified aggregate classes) is honoured — the rows are data, the presenter is one
  class, and no `Perspective`/`PerspectiveLayout` aggregate type exists.
