---
id: adr-0045-perspective-set-factory-review-understand
title: "ADR-0045 — The Perspective set becomes Factory · Review · Understand; feature children are admitted surface kinds; surface models are workspace-owned; layout slots migrate by stable id"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [architecture, shell, perspectives, docking, avalondock, layout, surfaces, workbench-loops]
links:
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: adr-0017-primary-view-mode, rel: supersedes }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: supersedes }
  - { to: adr-0031-second-docking-host, rel: refines }
  - { to: adr-0032-perspective-layout-slots, rel: refines }
  - { to: adr-0012-docking-shell-library, rel: depends-on }
  - { to: adr-0013-layout-persistence-envelope, rel: depends-on }
  - { to: adr-0040-journey-purpose-identity, rel: relates-to }
  - { to: mockup-factory-workbench-v2, rel: relates-to }
  - { to: architecture-loomkeeper, rel: refines }
review-by: 2027-04-18
summary: >-
  The closed Perspective row set is re-rowed from Coding · Explore · Architecture · Coordination to
  the spec's Factory · Review · Understand, each a DockHost body; the mechanism of ADR-0017/0030/0031
  (closed Core rows, allow-list column on surface kinds, derived menu/palette/rail, one layout slot
  per host) is kept and the rows are superseded. The Explorer full-window body becomes a dockable
  visualizer surface. Surface models (notebook, attempt, visualizer, checklist) are owned by a
  workspace registry keyed by object identity, so moving, hiding or closing a pane never disposes a
  model or stops a run. Old slots migrate by stable id with drop-with-report.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
---

# ADR-0045: Factory · Review · Understand as the Perspective set

- **Status:** Proposed (council gate 2026-10-18) — **surfaces a drift** between ADR-0030's rows and
  the spec, resolved by supersession rather than silent contradiction
- **Date:** 2026-10-18
- **Deciders (Peer Mode):** Enterprise Architect (author), Tech Lead, UX Researcher/IA
  (navigation), WPF Styling (consulted on hosts)
- **Context spec/architecture:** spec §B1 (three navigation sections, feature children, "they do not
  determine journey identity"), US-D01–D02, UI-03, AC-D01b (layout migration), proposal §A shell
  designed for docking; ADR-0017 (amended), ADR-0030–0032; `DockHost.cs:32-54`;
  `Perspectives.cs:61-88`.

## Context

The shell already has what the proposal asks for: a closed `PerspectiveSet` in Core (today **four**
rows — Coding, Explore (full-window), Architecture, Coordination — `Perspectives.cs:63-82`, which
already exceeds ADR-0030's "three rows; a fourth fails the build test"), one `DockHost` per
dock-host perspective (`DockHost.cs:32`, `Create` :54, `AdmissionFor` :92), an admission rule per
surface kind (`SurfaceAdmission.cs:39`), eighteen surface kinds (`SurfaceContentFactory.Kinds`
:139-256: canvas, view, classdiagram, sequence, contexts, joins, solution-tree, entry-points,
terminal, sessions, board, leaderboard, ledger, daydreams, prompt, search, codeviewer,
diagnostics), zone operations with keyboard commands (`ZoneLayoutService.cs:18-195`), a per-host
layout slot `<layout>.<perspective>.zones.json` (`LayoutPersistence.cs:89-102`), a layout migration
chain (`LayoutMigrations.cs:12-40`, v1→v2→v3) and a zone restore report (`ZoneRestoreReport` :604).
All Verified 2026-10-18.

The spec replaces the navigation vocabulary: **Factory** (Prompt notebook, Work items, Execution,
Coordination), **Review** (Pull requests, Review notebook, Test evidence, Rigor backlog),
**Understand** (Ask the repo, Ask a PR, Visualizer, Checklists); feature destinations live on the
left, not as right-pane tabs; a visualizer's representation selector is not navigation; every pane
carries object identity and scope; moving a pane never changes the identity or lifetime of its
content; layout is retained per loop (per section) with focus and caret restored.

Today's `SessionDocumentSurface` owns the turn's `CancellationTokenSource` and queue slot
(inventory: `:77, :551, :567`) — a run's lifetime is tied to a pane's model, which the spec forbids.

LOA principles: P2 (a closed, derived row set), P7 (state at the edges: the model registry, not the
pane), P9 (typed admission).

## Decision

1. **Re-row the closed set — two decisions, stated separately.** *(a) Re-row:* `PerspectiveSet`
   becomes exactly three rows: `factory`, `review`, `understand`, each `PerspectiveBody.DockHost`,
   each with its own `Landing` zone (Factory: Center = notebook; Review: Center = change packet;
   Understand: Center = visualizer) and a `RoutingOrder` of Understand → Review → Factory ("the
   reading host wins a shared kind", the rule `Perspectives.cs:93-103` encodes today). ADR-0030's
   mechanism — Core rows, the `Perspectives` allow-list column on kind rows, `PerspectiveMenu.For`
   (`src/AiDe.App/Workbench/PerspectiveMenu.cs`, a shape interpreter with no row switch) as the
   one derivation of menu/palette/rail/title — is unchanged; its rows are superseded. *(b) Retire
   `FullWindow`:* no row keeps `PerspectiveBody.FullWindow`; `PerspectiveShell`'s explorer-factory
   / retained-explorer / `BodyFailed` path and `ExplorerSurface` are **deleted** (HYG-A), with a
   build test that no row uses `FullWindow`; `Escape()` and the previous-perspective slot are
   retained as perspective-level behaviour (US-C1). The `Coordination` row — Ruling 84's isolated
   bench for the five Loomkeeper kinds — is **superseded**: those kinds (board, leaderboard,
   ledger, daydreams, `sessions` = the watcher pane) are admitted under Factory → Coordination,
   and defect class SH-4.1's oracle (realization order by zone occupancy) is re-run on the Factory
   host before the slice is accepted. This relocates Loomkeeper's Observatory bench
   (`architecture-loomkeeper` §4) and is flagged to it.
2. **Feature children are admitted surface kinds, not a second navigation.** The left hierarchy is
   derived from `(perspective, kind rows admitted, instances)`; choosing a child opens or focuses
   that kind's surface in the current host; "Work items" is derived from `journey_dim` rows, not a
   kind. **This ADR adds two kinds**: `notebook` (work or investigation, bound to a journey) and
   `execution`; `checklist` arrives with WL-1b. The existing `session-document` kind (not
   `sessions`, which is the watcher pane — Ruling 18) is migrated into `notebook` + `execution`
   with drop-with-report until then; `prompt` likewise. The visualizer is **not** a composite kind:
   the existing `sequence`, `classdiagram`, `contexts`, `joins`, `view`, `codeviewer`, `canvas`
   and `solution-tree` kinds are admitted under Understand and follow linked selection (§5), which
   is the "representation selector" without a new surface. `change-packet` / `test-evidence` /
   `rigor-backlog` (possibly one `assessment` kind parameterised by assessment kind), `connections`
   and `coordination` are added by the slice that lands them (WL-5, WL-3, WL-6) — each row
   obliges a UIA name and an allow-list entry that slice carries.
3. **Three hosts under one presenter.** ADR-0031 proved two `DockHost`s under `PerspectiveShell`
   with a live WebView2 surviving A↔B cycles (`spikes/second-dock-host-unparent`). Three hosts is
   the same mechanism (`PerspectiveShell` already takes a third body; `DockHost.cs:12-13`'s "twice"
   comment is stale and is corrected); the cycle spike is extended to A↔B↔C, recording private
   bytes per host idle and with a 100k-character notebook as the memory budget (**Inferred** until
   run).
4. **Surface models are workspace-owned — no new registry class.** The shell already keys models
   by identity (`WorkbenchShell._sessionDocuments`, `_consoles`); those dictionaries are re-keyed
   from surface to model (`Dictionary<NotebookId, NotebookModel>`), and the active attempt's
   cancellation and queue move out of `SessionDocumentSurface` (`:77, :551, :567`) into the attempt
   host (ADR-0041). A surface instance *binds* to a model and may be created, moved, floated,
   hidden, closed and reopened without creating or disposing the model, so "Pane hidden. The run
   continues." is structurally true (AC-D01a, UI-03). A registry type is introduced only when a
   second owner appears.
5. **Identity and scope on every pane.** A content pane's title/automation name carries object
   identity and effective scope; linked panes follow the current selection; a pinned pane retains
   its baseline visibly and is never relabelled current (AC-D01c).
6. **Default arrangements are named layouts per perspective**: *editor + execution*, *execution
   below*, *focus editor*, *investigation* — `WorkbenchLayout.Default(Perspective)` plus named
   layouts (ADR-0012), never locked templates. `ZoneId` stays {Left, Right, Bottom, Center}.
7. **Layout migration by stable id.** Slot `coding` → `factory`, `architecture` → `understand`,
   `coordination` → folded into `factory` (its surfaces admitted there), `explore` → none (new
   default); `session-document` placements map to `notebook` + `execution`; a kind no longer
   admitted is dropped **with a report** (DC-219's control), a one-time `.bak` is written, and an
   unusable/off-screen placement recovers to the perspective default (AC-D01b). Every pointer-only
   arrangement already has a keyboard command; the new kinds inherit that contract.
9. **This ADR is its own parallel slice.** Nothing above touches the revision↔attempt seam
   (ADR-0041), so the re-row, migration and unparent spike run as slice WL-S beside WL-1a, not
   inside it; WL-1a lands `notebook` + `execution` as kinds admitted to the **existing** Coding
   host first.
8. **Terminal is a deliberate secondary surface.** "Open repo terminal" creates an
   `ITerminalSession` at the chosen path, visible with identity and process state; hiding the pane
   keeps the process; closing the session states the consequence (US-D02). Console (raw native
   output) is an evidence view, not the primary execution surface.

## Alternatives considered

- **Keep Coding/Explore/Architecture and map the three sections onto them:** rejected — the spec
  names the three sections as the user's navigation; two vocabularies for one row set is a second
  definition (DM "two definitions of one quantity").
- **Factory/Review/Understand as right-pane tabs inside one host:** rejected by the proposal
  ("feature destinations do not become right-pane tabs") and by ADR-0032's per-host slot design.
- **Keep Explorer as a full-window body:** rejected — the visualizer must dock beside the question
  (AC-S02a, UX-04); a full-window body cannot.
- **Keep the run's token in the pane:** rejected by AC-D01a and the copy contract "Pane hidden. The
  run continues."

## Consequences

- **Positive:** the shell machinery is reused wholesale; the drift is recorded, not hidden; a run
  cannot be killed by a layout gesture.
- **Negative / accepted trade-offs:** every test that asserts the old rows or the
  `Workbench | Explorer` toggle goes red and is re-scoped (the red-first list in the slice); users
  lose the Explore full-window arrangement; three hosts hold three WebView2-bearing bodies retained
  in memory.
- **Follow-ups / new risks:** the A↔B↔C unparent spike; the `Perspectives` build test re-pinned to
  three; UIA names for the new kinds; mixed-DPI float remains the airspace spike's obligation
  (ADR-0015).

## Evidence

- `Perspectives.cs:4-106`, `DockHost.cs:32-92`, `SurfaceAdmission.cs:39`,
  `SurfaceContentFactory.cs:139-256`, `ZoneLayoutService.cs:18-195`, `LayoutPersistence.cs:89-208`,
  `LayoutMigrations.cs:12-40`, `ZoneBackedLayoutService.cs:82-604`, `SessionDocumentSurface.cs:77-600`
  — **Verified** inventory 2026-10-18.
- `spikes/second-dock-host-unparent` (two hosts) — **Verified** 2026-09-11; three hosts — **Inferred**.
