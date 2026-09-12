---
id: note-sh2-presenter-router-and-slots
title: "SH-2 decisions below ADR weight: the router is table-driven from the catalog's Scope, the seam names its host and follows the applied add, host B's interim default is today's default filtered, the switch is two log lines, and the rail is a ListBox that never selects on its own"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, addendum-c, perspective, docking, layout-persistence, rail, shell-lane]
links:
  - { to: adr-0031-second-docking-host, rel: refines }
  - { to: adr-0032-perspective-layout-slots, rel: refines }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: proof-perspective-shell, rel: relates-to }
  - { to: note-sh1-scope-and-entry-columns, rel: relates-to }
  - { to: coordination-addendum-cd, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Ten choices ADR-0031/0032 left to the implementing slice, made and defended here: the presenter's
  router reads the catalog row's CommandScope and the kind rows' allow-lists (no switch on an id);
  DocumentOpening names the host it opens into and follows the applied add (document first); a
  kind-open asks from the host that raised it and resolves by ADR-0030's order; host B starts from
  today's default filtered to its admitted kinds until SH-3's Default(perspective); the switch writes
  a synchronous shell.mode line and an asynchronous shell.mode.shown line; the rail is a ListBox
  whose selection has one writer; a refused file is preserved every time; a reconcile keeps the
  view's active tab; focus after a switch is the window's hook; the refusal code lives beside the rule.
---

# SH-2's decisions below ADR weight

- **Kind:** decision (below ADR weight; refines ADR-0031 and ADR-0032)
- **Made by:** session `sh-2` (`/implement`, Shell lane), 2026-09-12
- **Evidence opened:** ADR-0031 (Decision rules 1–5, the Tech Lead's bound on the router, the Owner's residual), ADR-0032 (rules 1–5, the seven tests), ADR-0017 amendment clauses 1–5; spec US-C1, US-C2, US-C3, US-C5, US-C9, US-C11, US-C12, §B4, §C4, §C5; DESIGN.md PS-R2/R4, PS-T1/T3; `WorkbenchShell.cs` (the composition at `:113–195`, the openers, the binders), `WorkbenchController.cs` (`Execute`, `Bind`), `ShellModeController.cs`, `MainWindow.xaml.cs`, `LayoutPersistence.cs`, `ZoneLayoutStore.cs`, `ZoneBackedLayoutService.cs`; `spikes/second-dock-host-unparent/RESULT.md`; the reviewers' findings (recorded in the Proof Pack).

## 1. The router reads the catalog's `Scope`; a switch on an id is the falsifier

ADR-0031 bounds the presenter's router to *active/previous, the three bodies, `Execute(id)` = resolve the host then delegate, the three perspective commands, the entry-verb rule* and names "a `switch` on any other command id inside the presenter" as the falsifier. SH-1 had already put the input the router needs on the rows: `CommandScope` (Global · DockHost · Admits(kind)) on every catalog row, `Perspectives` on every kind row. So `PerspectiveShell.Execute` is four lookups and no cases: a perspective command activates; a derived opener (`PerspectiveMenu.TryParseOpener`) or an `Admits(kind)` row goes to the host `PerspectiveMenu.Resolve(kind, Active)` names; a `DockHost` row goes to the active host or is refused with a reason and a stable code (`AIDE-PERSPECTIVE-NO-HOST`) while a full-window body is showing (ADR-0017 amendment clause 4: never a silent model update); everything else — the entry verbs and the workspace verbs, all `Global` — goes to the **initial** host's controller, which is where the shell wires their delegates. An id no row knows falls through to that controller too, which is today's behaviour for the workspace-profile harness rows. Rejected: a `Func<Perspective, DockHost>` strategy per command (a second table); a controller that knows about switching (ADR-0031's first rejected alternative).

## 2. The seam names its host and follows the applied add — document first, then the switch

INV-0009's `DocumentOpening` was a parameterless event the window answered with "if a full-window body is showing, show Coding", raised at the head of every opening command. Two hosts need the seam to say *which* host the document is about to enter: `DocumentOpening : Action<Perspective>`, raised by `OpeningDocument(host)`, answered by `PerspectiveShell.OnDocumentOpening(host)` — switch iff `host != Active`. That one rule covers all three cases: Explore → the document's host returns; Coding's own openers stay in Coding; a routed open (a kind the active host does not admit) brings the admitting host on screen. **The seam is raised after the add and only when the add was applied**, before the render that realises the pane (the Test Architect's blocker on the first cut, which raised it before the add): ADR-0031 rule 2 and US-C5/US-C11 say *document first, then the switch*, and their falsifier — a refused add (a locked layout, no pane) that leaves the operator switched into an empty host — is exactly what switch-first produced. INV-0009's guarantee is kept because the pane's content is built by the render, which runs after the switch into a body that is by then on screen. The scan (`EveryAddSurfaceInTheShell_IsFollowedByTheSeamOnceApplied_BeforeTheRender`) now asserts the order and the `Applied` gate textually; the pairing test locks host A and proves the failure half through the composition root.

## 3. A kind-open asks from the host that raised it

`OpenKind(from, kind, showExisting)` and `OpenReferenceDocument(host, …)` take the host: each host's `OpenSurfaceRequested` is wired with itself, the router picks the controller by `Resolve(kind, Active)`, and the shell resolves again from the asking host (`ResolveHost(from, kind)`) — the same function at both layers, so a headless caller that reaches host A's controller directly (every existing test and probe) still lands a class diagram in host B and switches to it. In-body node actions ("View source", "Class diagram", "Sequence diagram" from the graph's context menu) ask from the host that holds the graph (US-C3 b2), never from the active one. A routed open announces where it landed: *"Class diagram opened in Architecture."*; an in-host open says *"Class diagram opened."* (DESIGN.md's "The switch, and what it says").

## 4. Host B's interim default is today's default filtered to its kinds

§B4's per-perspective defaults are `WorkbenchLayout.Default(perspective)`, which SH-3 owns (`ZoneLayout.cs`). Until it lands, a host's service filters the one `Default()` through its own admission (`ZoneBackedLayoutService.DefaultLayout()`, marked `simplify:` with SH-3 as the trigger): Coding starts with sessions · board · leaderboard · ledger (Center) and the terminal (Bottom); Architecture with graph (Center) and explore · provenance · contexts · joins (Left) — `domain` (the second `view`, one-instance) is dropped because Left precedes Center in zone order. Both are true to the allow-lists and neither is §B4; the Proof Pack records them as interim. The same filter runs on `ResetToDefault`, so a reset can never bring an inadmissible kind back (SurfaceAdmissionTests).

## 5. The switch is two log lines

US-C12 asks for one event carrying `duration_ms` with a stop edge at the new body's first `Loaded`. The `shell.mode` line is written synchronously — INV-0009's replay reads `last-mode-trigger` right after the switch, and the headless presenter tests assert it — while the stop edge is asynchronous and, in a headless run, never comes. One event could only satisfy both by deferring the synchronous readers or by recording a modelled duration; so the switch line (from · to · trigger · `first_entry` · `outcome` · `error_code`) is written at the switch and a `shell.mode.shown` line (mode · trigger · `duration_ms`) at the edge, joined by `mode` and `trigger`. The hook that writes it unsubscribes itself on first fire, which is why `PerspectiveShell.cs` may hook `Loaded` (the DC-138 allow-list entry names the reason and the test that proves it). Recorded as a deviation in DESIGN.md.

## 6. The rail is a `ListBox` whose selection has one writer

PS-R2 rules out `RadioButton` and `TabControl` (both select on arrow). A `ListBox` also selects on arrow, page keys, typeahead, mouse-down (either button), a UIA `Select()` and any programmatic `SelectedItem` — the UX & Accessibility reviewer's list — so `PerspectiveRail` intercepts the keys (focus only for the movement keys; `ActivationRequested` for Enter/Space) and both mouse buttons before the list sees them, turns typeahead off, and **reverts every selection change `Reflect(active)` did not make** in `OnSelectionChanged`, raising the attempted destination as an activation request instead. `Reflect` is called from the presenter's `Changed` event — the rail follows the body and never leads it, so a failed switch leaves the selection where the body is. Its automation peers report a `Tab` list (`IsSelectionRequired = true`) of `TabItem`s whose `SelectionItemPattern` reads the container's real `IsSelected` and whose `Select()` is an activation request, never a write; the accessible name is constant; Tab enters the group on the selected destination (`IsTabStop` follows the selection). The rail's container in `MainWindow.xaml` is **not** a `TabNavigation=Once` group — a one-stop group would let Tab leave after New session and never reach a destination (the reviewer's blocker). The 44 × 44 target is the item's own `Height`/`MinWidth`, not the window style's, because a floor that lived only in a style would be absent wherever the style is. `IconCoding`/`IconArchitecture` are `MainWindow.xaml` resources for this slice (App.xaml is not the lane's; DESIGN.md records the deviation and the two-line seam request).

## 7. A refused file is preserved every time; the pre-perspective bytes once

ADR-0032 rule 3's "once, guarded by the backup's absence" is the `.pre-perspectives.bak` rule and only that. The first cut applied the same absence guard to the refused-file `.bak`, so a second refusal on a slot that already carried a `.bak` was rewritten with no copy while the announcement said "kept" — the D&P Architect's blocker, run and observed. Now a refused file is always passed as the backup of the save that rewrites it (`File.Replace` replaces an older `.bak`; the latest refusal is the recoverable one) and the announcement says so; the pre-perspective copy keeps its once-rule. Findings relayed to the ADR's owner: the rollback-then-re-upgrade sequence writes Architecture kinds into the Coding file and the later drop is reported but not preserved (the once-rule's accepted loss); `ReplaceFile`'s documented partial failures leave the original at the backup path and the slot absent until the next save; no fsync before the rename (a Type-1 preference file).

## 8. A reconcile keeps the view's active tab

SH-1's seam note said `ReconcileViewIntoModel` resets a stack's active tab to index 0 — INV-0006's class. SH-2's create-failure test compared a host's shape before and after a refused entry verb and saw `@1` become `@0`: the reconcile at the head of the command rebuilt every zone stack with `ActiveIndex 0`, so the next render would have moved the operator's active tab. `TryMapByPosition` now carries the view's active surface per zone into the model (`ReconcileFromView_KeepsTheViewsActiveTabInEveryZone`; mutation red observed).

## 9. Focus after a switch is the window's decision

Spec §C5 names where focus lands per body (Coding: the active document; Explore: never the canvas; Architecture: the Center's active tab). The presenter does not know the bodies' insides, so it exposes `EntryFocus` — a hook the window sets (`MainWindow.EntryFocusFor`: Explore → `Reader.FocusReader()`; a host → its active document's content) and calls on the new body's first `Loaded`, after the announcement was queued; unset, the body's first focusable. The spec's "search box" for Explore does not exist in `ExplorerSurface` (finding for the spec); the reader is the first non-canvas focusable.

## 10. The admission's refusal code lives beside the rule

`AIDE-LAYOUT-KIND-NOT-ADMITTED` belongs in `LayoutErrorCodes` by family, but that class is in `LayoutService.cs`, outside the slice's paths. It is `SurfaceAdmission.RefusalCode`, beside the rule that refuses, and moves to the family by a one-line seam request rather than by an unowned write. The same discipline kept `RestoreResult` (`LayoutStore.cs`) unchanged: the drop report rides `MissingSurfaces`/`ErrorCode`/`WasDefaulted` — the channel that reports a dropped surface today (ADR-0031 rule 3) — and the typed drops are `LayoutPersistence.LastRestoreDropped`.
