---
id: note-addendum-c-council-rulings
title: "Decision note — Rulings 50–55: Addendum C's vocabulary, phasing, ADR-0017, the graph substrate, the 80% case, and page one"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, ruling, conductor, addendum-c, perspective, ui, docking, explorer]
links:
  - { to: plan-addendum-c-modes, rel: relates-to }
  - { to: plan-conductor-front-door, rel: relates-to }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: spec-knowledge-explorer-mode, rel: relates-to }
  - { to: spec-knowledge-exploration, rel: relates-to }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: note-front-door-council-rulings, rel: refines }
review-by: 2026-12-11
summary: >-
  Six rulings the Owner issued before any Addendum C spec was written, on the evidence the conductor
  brought at node R0 of plan-addendum-c-modes. They fix the vocabulary (Perspective), the phasing
  (F5 untouched; code after F5 merges), ADR-0017's fate (retained and amended), the graph model (one
  substrate, two surfaces), the build order under the operator's 80% case, and four page-one facts a
  spec written without them would get wrong.
---

# Decision note — Rulings 50–55

## Provenance

Issued by the **Owner** (persona `.claude/agents/owner.md`, model `fable`) at node **R0** of
`docs/plans/addendum-c-modes.md`, on 2026-09-11, in answer to six questions the conductor put with
verified evidence. Filed verbatim by the conductor (session `conductor-addendum-c`). **Ruling 49**
(F5's exit run is triggered by the operator's own gesture) is the immediate predecessor; it is filed
as `docs/notes/front-door-ruling-49.md` on `feature/exit-evidence` and lands on `main` with that
branch — until then this note's predecessor is on a branch, and Ruling 51 says why that is correct.

The operator's intent these rulings serve, verbatim, is in the audit entry
`al-01M28QJMWGJT5AK438M37KJ9ZT` (the prompt of 2026-09-11 17:17Z).

Files cited are under `C:\projects\ai-de\` at `main` `bb09e294`, except where a path names the F5
worktree.

---

## Ruling 50 — Vocabulary: the concept is a *Perspective*; "canvas mode" and `ShellViewMode` are not renamed by the spec

**RULING:** Name Addendum C's concept **Perspective** (Coding · Explore · Architecture; Tests
reserved), define it on page one in a three-row vocabulary table beside *canvas mode* and *primary
view mode*, and ban the unqualified word "mode" from Addendum C's normative text.

**BECAUSE:** Three meanings already exist — *canvas mode* is per-session, per-canvas, splittable
(`ai-de-spec-addendum-a-session-experience.html:174-185`, R16 at `:238-244`;
`CanvasModeCatalog.cs:21,46`); *primary view mode* is the shell's body swap
(`0017-primary-view-mode.md:79-82`; `ShellModeController.cs:6-11`); the operator's concept is a
task-scoped view-set plus menu contribution, which is exactly the Eclipse *perspective* — a term with
zero collisions in `src/` (only a 3D-projection comment at `CanvasPage.cs:20`). Renaming
`ShellViewMode` now would touch code F5 is measuring (Ruling 51).

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Freezes the terms *canvas mode* (Addendum A) and *primary view mode* (ADR-0017)
as-is; `ShellViewMode` is renamed to `Perspective` only in the commit that implements Ruling 52,
never before.

**CONDITIONS:** Revisit if the spec finds a fourth meaning it must name; the fix is a fourth table
row, not a synonym.

**RECORD AS:** Ruling 50 — Addendum C's concept is "Perspective"; canvas mode and primary view mode
keep their names until Ruling 52's implementation touches `ShellViewMode`.

---

## Ruling 51 — Phasing: F5 stands untouched; Addendum C is the next delivery, specified now, built from `main` after F5 merges

**RULING:** Write Addendum C's spec now in its own worktree (docs only), and start its implementation
branch from `main` only after `feature/exit-evidence` has merged; F5's exit evidence stands exactly as
Addendum A + Ruling 49 wrote it.

**BECAUSE:** F5's pending clauses measure `File → New Session` under the current menu and shell
(`conductor-front-door.md:50,73` in the F5 worktree; Ruling 49 at `front-door-ruling-49.md:28`), and
Addendum C rewrites the menu (`MainMenuBuilder.cs:77-99`) and the rail (`MainWindow.xaml:65-107`) — a
run taken against a changed shell would measure something Addendum A did not specify. The Ruling 49
note exists only on the feature branch (no match for "Ruling 49" under `C:\projects\ai-de\docs`), so
a ruling file on `main` numbered from 50 has a dangling predecessor until F5 lands.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Defers Addendum C code to after F5 merge; admits Addendum C spec work in parallel.
Addendum C does not amend Addendum A's text; it cites §A6.1 as-is.

**CONDITIONS:** This note cites `docs/notes/front-door-ruling-49.md` by path and notes it lands with
`feature/exit-evidence`. If F5 is abandoned rather than merged, re-rule before Addendum C branches.

**RECORD AS:** Ruling 51 — F5 exit evidence stands untouched; Addendum C is the next delivery, spec
in parallel, code from `main` after F5 merges.

---

## Ruling 52 — ADR-0017: retain and amend, do not supersede; a Perspective *is* a primary view mode, and a perspective body may be a docking host with a surface-kind allow-list

**RULING:** Keep ADR-0017's body-content swap and its retain-never-rebuild invariant; amend it so
(a) the closed mode set becomes the Perspective set, (b) a perspective's body content may be a docking
host of its own (Coding = today's workbench host unchanged; Architecture = a second host), (c) each
perspective declares an allow-list over `SurfaceContentFactory.Kinds` expressed as a column on the
existing descriptor rows, and (d) Explore keeps the full-window `ExplorerSurface` and does not become
docked panes.

**BECAUSE:** The ADR already anticipates this — "new full-window surfaces are new modes, not new
shell mechanisms" (`0017-primary-view-mode.md:89-90`) — and its rejection of option A (dock pane:
graph and reader compete) at `:51-56` still holds for the operator's own UC2 description. The
invariant is preserved by construction: the presenter holds every body content alive and only
unparents (`ShellModeController.cs:19-28,68-74`; ADR `:93-98`), so a terminal in Coding keeps running
while Architecture is open, exactly as it does while Explorer is open today.
`knowledge-explorer-mode.md:73-75` (non-goal 5) forbids restructuring the dock from a mode switch,
which this keeps. Ruling 22 requires kinds to be rows, not switch arms
(`SurfaceContentFactory.cs:58-61`), so the allow-list is a row attribute.

**CONFIDENCE:** Verified for the invariant and the ADR's reasons; **Inferred** that a second
AvalonDock host survives unparenting as the first does — the ADR's T1 no-rebuild test (`:98`) must be
re-run against the second host, and this is an extension of the ADR, not a reading of it.

**SCOPE EFFECT:** Cuts "Explorer becomes docked panes". Admits an ADR-0017 amendment (status
`proposed` → accepted-as-amended; it is implemented on `main` yet still `proposed` at `:5,27`).
Layout persistence gains one envelope slot per perspective (ADR-0013 amendment already named at
ADR-0017 `:99-101`); an old envelope carrying a kind a perspective now disallows migrates by
drop-with-report, never crash.

**CONDITIONS:** Falsified if the no-rebuild test fails for a second live docking host (a hidden
`HwndHost` in host 2 restarts); then Architecture's body becomes a non-docking composite and the
ruling is re-issued.

**RECORD AS:** Ruling 52 — ADR-0017 retained and amended: perspectives are primary view modes; a
perspective body may be an allow-listed docking host; Explorer stays full-window.

---

## Ruling 53 — UC2 vs UC3: one graph substrate, two surfaces; no "graph with two modes"

**RULING:** Explore's surface is the existing `ExplorerSurface` (graph + reader, node-walk);
Architecture's graph is a **second `CanvasSurface` instance** scoped by a node-kind filter (code ·
data · architecture) that composes with the Model surface — the same substrate, the same neighbourhood
query, no second graph store, and no in-surface "knowledge/architecture" toggle.

**BECAUSE:** `knowledge-exploration.md:69` (US-K1) commits to *one graph over all artifacts* with
node kinds enumerated at `:56-58`, and `:62` defines UML/ER views as projections of that graph;
`uml-erm-surfaces.md:55` says the Model surface is "not a re-implementation of the graph explorer —
structural views that compose with it", with drill-to-node-walk at `:78` (US-U8) and its IA at
`:90-92`. `ExplorerSurface.cs:20-21` already establishes a second `CanvasSurface` instance as the
pattern (a canvas is never reparented across visual trees). A toggle inside one surface would be a
fourth "mode" (Ruling 50).

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Freezes the graph model; the kind filter is a parameter on the existing
neighbourhood query. The class-diagram scaling defect the operator named routes to US-U7 / US-K10–K11
(aggregation, LOD) on the substrate, not to a new surface.

**CONDITIONS:** Falsified if the projection's node-kind set cannot express "code/data/architecture
only"; that is a substrate finding to fix first, not a reason to fork the graph.

**RECORD AS:** Ruling 53 — one graph substrate; Explore = `ExplorerSurface`, Architecture =
kind-filtered second canvas composing with the Model surface.

---

## Ruling 54 — The 80% case: the spec defines every perspective's allow-list and menu, but admits into Addendum C's build only Coding, the mechanism, and the Architecture views that already have a surface; the rest are named-and-deferred; UC4 is a named non-goal

**RULING:** Order Addendum C as (1) Coding perspective, (2) the Perspective mechanism — rail,
per-perspective menu, allow-lists, persistence slots, (3) Explore unchanged, (4) Architecture with
only its existing surfaces (`classdiagram`, `sequence`, `contexts`) plus the class-diagram scaling
fix; name entry-points view, data-flow, ER diagram, and bicep-derived layer/component diagrams with
acceptance criteria and **defer** them; name Use Case 4 as a non-goal with no rail item.

**BECAUSE:** The operator's priority is explicit ("80% case … where most of our calories must be
spent"). Kinds that exist today are the descriptor rows at `SurfaceContentFactory.cs:107-127` — there
is no ER, entry-point, data-flow, or solution-tree kind, and no `SolutionTree`/`WorkspaceTree` type in
`src/AiDe.App`. `MainWindow.xaml:108-111` (AR3) forbids a rail item that does nothing, so Tests gets
no icon until it exists. Coding's three models are not new scope: session-primary is Addendum A;
terminal-secondary is `TerminalSurface` + `File → New Terminal Session` (a spec invariant, Ruling 45
at `front-door-rulings-45-48.md:77-80`); "hybrid" is Addendum A's **Terminal** canvas row (`:183`),
which re-registers when observed-lane binding exists (Ruling 45 `:79-80`).

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Cuts four of the six understanding views from Addendum C's build (they remain
specified). Cuts UC4 entirely. Cuts any new graph capability in Explore
(`knowledge-explorer-mode.md:66-67`). Coding's allow-list excludes `classdiagram`, `sequence`,
`contexts` per the operator's "no sequence diagram in the agentic coding use case".

**CONDITIONS:** Revisit the deferral list only when a deferred view has a substrate query it can be
built on; an operator request re-admits one at a time, not the set.

**RECORD AS:** Ruling 54 — Addendum C priority: Coding, mechanism, Explore-unchanged,
Architecture-existing-surfaces; four views named-and-deferred; UC4 non-goal.

---

## Ruling 55 — What the spec gets wrong on page one without these four facts

**RULING:** Addendum C must state on page one that (a) Addendum A §A6.1 defines **five** canvas
modes, Terminal included, and the "hybrid" coding model is that row — not a new thing; (b) the
per-perspective menu is **derived** from the active perspective and the allow-listed `Kinds`, the way
the Terminal menu is derived from `AgentReadinessProfiles`, never a second hand-written list; (c) the
"New session" primary action stays visible in every perspective and switches to Coding; (d) the
Coding default layout drops the "Explore" pane and resolves the Domain/Provenance duplication in its
E7 surface list, with the owed "two kinds render different content" test landing in the same slice.

**BECAUSE:** (a) `ai-de-spec-addendum-a-session-experience.html:174,183` — the conductor's summary
listed four. (b) `MainMenuBuilder.cs:77-99` is a static tuple list mixing shell mode, Coding and
Architecture commands; `:89-96` already shows the derived pattern and says why a second list is
wrong. (c) `MainWindow.xaml:70-85` (AR2/AR5) — one door, above the destinations.
(d) `SurfaceContentFactory.cs:70-106` is an open finding: "Explore … duplicates the Explorer rail
mode", Domain wired to `view`, and the control is owed but deliberately not landed.

**CONFIDENCE:** Verified.

**SCOPE EFFECT:** Admits (d) into Addendum C's Coding slice — it is the default layout of the
perspective being specified, so it cannot be left to "another session". Freezes Addendum A's text.

**CONDITIONS:** (d) is contingent on INV-0006's zone/tree repair having merged; if not, the Coding
default-layout node blocks on it and says so.

**RECORD AS:** Ruling 55 — page-one facts for Addendum C: five canvas modes, derived menus,
New-session in every perspective, Coding layout resolves the Explore/Domain/Provenance duplication.

---

## Residual the Owner did not rule

Whether the Architecture perspective's second docking host shares one `WorkbenchController` command
set or gets its own — a design question for `/design-slice`, not a spec question. Carried to node A1.

## Re-plan checkpoints discharged (plan-addendum-c-modes, Stage 9)

- **① Phasing:** Addendum C does not touch Phase 1's exit; F5b's oracle is unchanged. Discharged.
- **② ADR-0017:** *retained and amended*, which is neither of the two branches the plan named
  ("supersede" or "retain, modes constrain only the workbench"). D1's brief therefore carries the
  amended shape: perspectives as primary view modes, Coding's body = today's host, Architecture's body
  = a second allow-listed host, Explore full-window and unchanged. Discharged with a third outcome.
