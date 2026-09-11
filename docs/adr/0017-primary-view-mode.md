---
id: adr-0017-primary-view-mode
title: "ADR-0017 — Full-window surfaces are a primary view mode (body-content swap), not a dock pane or a modal overlay"
type: adr
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [architecture, ui-shell, view-mode, explorer, docking, accessibility]
links:
  - { to: architecture, rel: implements }
  - { to: spec-knowledge-explorer-mode, rel: refines }
  - { to: adr-0008-shell-host, rel: relates-to }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
  - { to: adr-0013-layout-persistence-envelope, rel: relates-to }
  - { to: adr-0015-canvas-hosting-and-overlay-strategy, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: relates-to }
  - { to: adr-0031-second-docking-host, rel: relates-to }
  - { to: adr-0032-perspective-layout-slots, rel: relates-to }
review-by: 2027-03-11
summary: >-
  A surface that needs the whole body (the Knowledge Explorer's graph+reader) is presented as a
  primary VIEW MODE the shell holds — Workbench | Explorer — realised as a body-content swap of the
  region the docking host occupies, with the activity rail as the mode selector. Rejects making it a
  dock pane (it would compete for space — the defect being fixed) and a modal overlay (the rail must
  persist and it is not dismiss-only). The non-active mode's state is retained, never rebuilt.
  AMENDED 2026-09-11 (Ruling 52): the closed set is the Perspective set (Coding · Explore ·
  Architecture); a mode's body may itself be a docking host; the Inferred second-host clause is
  discharged by spikes/second-dock-host-unparent.
review-suggested:
  - { by: adr-0013-layout-persistence-envelope, on: 2026-09-11, reason: "ADR-0013 amended (Ruling 52, ADR-0032): one zone-envelope file per host perspective; drop-with-report at restore; tested rollback" }
---

# ADR-0017 primary-view-mode: Full-window surfaces are a primary view mode (body-content swap)

- **Status:** **Accepted as amended, 2026-09-11** (Ruling 52 — retained and amended, not
  superseded; see *Amendment* at the end). Proposed 2026-08-30. Raised for the full-window Knowledge Explorer
  (`spec-knowledge-explorer-mode`); the mechanism is a general one (any future full-window surface —
  a diagram studio, a dashboard — uses the same seam), so it is recorded as an architecture decision,
  not a one-off in the Explorer's component design.
- **Phase:** UI-shell (post Phase-1 workbench).

## Context

The shell (`MainWindow.xaml`) is a fixed frame — menu bar, title strip, a **body** row, and a status
strip — whose body is a 56px activity **rail** plus a `WorkbenchHost` `ContentControl` that the
composition root fills with the AvalonDock docking host (ADR-0012). Every working surface today is a
**pane** inside that one docking host.

The Knowledge Exploration surface is a graph **and** a reader that renders the selected node's
contents (`spec-knowledge-exploration` US-K3/K4). As one dock pane among the terminal / domain /
provenance panes, on a single monitor the two halves of the one activity — *see the shape* and *read
the thing* — compete for a small area (`spec-knowledge-explorer-mode` Problem). The requirement is to
give the **whole body** to exploration on demand and return to the working layout untouched.

This is the first surface that wants the whole body. The decision is **how** a full-window surface is
presented, because it sets the pattern for every later one.

## The options

### A — Another dock pane (the status quo)

Make the graph+reader a bigger pane, or two panes, inside the docking host. **Rejected:** it is the
exact defect being fixed — the surface competes with every other pane, and "maximise the pane" still
leaves the rail/menu/other panes claiming the docking host's chrome and does not give a clean,
dedicated two-pane reading surface. It also cannot express "this is a different *kind* of view".

### B — A modal overlay over the whole window (like the command palette)

ADR-0015 established an overlay strategy: `RootLayer` lets the command palette float over the
workbench without displacing it. Reuse that for the Explorer. **Rejected:** the command-palette
overlay is **modal and dismiss-only** — it darkens/captures and returns you to exactly where you were.
The Explorer is a **place you work in**, not a transient prompt: the activity **rail must remain
visible and usable** (you switch modes from it), the menu/title/status strips stay, and the mode
persists across other interactions. A full-window modal that hides the rail would strand the mode
selector, and one that keeps the rail is no longer an overlay — it is option C.

### C — A primary view mode realised as a body-content swap (chosen)

The shell holds a **primary view mode** value — a small closed set, today `Workbench` and `Explorer`.
The **body region the docking host occupies** (`MainWindow.xaml` Row 2, Column 1) is presented by a
mode presenter that swaps its content between the docking host (Workbench) and the Explorer surface
(Explorer). The **rail is the mode selector** (its items become mode toggles), and the menu / title /
status strips are outside the swapped region, so they persist. Switching mode swaps *what fills the
body*; it does **not** restructure the dock, and it does **not** overlay.

## Decision

**Adopt C.** A **primary view mode** is a first-class shell concept: the shell is in exactly one mode
at a time; the body content is the projection of that mode; the activity rail selects it. Realised as
a **body-content swap** of the docking-host region — distinct from a dock pane (A) and from the modal
`RootLayer` overlay (B), which is retained for the command palette only.

## Consequences

- **Positive**
  - The Explorer (and any future full-window surface) gets the whole body with a clean two-pane layout
    and no competing panes, while the rail/menu/status chrome and the mode selector stay put.
  - The mechanism is **general and small**: a closed `ViewMode` enum + a content presenter keyed by it.
    New full-window surfaces are new modes, not new shell mechanisms.
  - The Workbench docking model (ADR-0012), its layout persistence (ADR-0013) and every existing pane
    are **untouched** — this is additive.
- **The load-bearing invariant — retain, never rebuild.** Switching mode **MUST NOT** rebuild the
  non-active mode. Entering Explorer must not restart a terminal in the Workbench; leaving it must not
  reload a live graph or reset the workbench layout. The presenter **holds both mode contents alive**
  and toggles visibility/hosting, rather than tearing down and recreating. This is the mode-level form
  of DC-029 (reconcile, don't rebuild) and is the property that makes the switch a view change, not a
  session loss. A focus-integration/no-rebuild test is required.
- **Layout persistence gains a per-mode slot (ADR-0013 amendment).** The Explorer's split ratio and
  last-focused node persist in their own envelope slot, separate from the Workbench layout, so each
  mode restores its own state (US-E6). The Workbench envelope is unchanged.
- **Accessibility contract crosses into ADR-0015.** While Explorer is active, the canvas keyboard trap
  (ADR-0015) must route a boundary `focus.leave` **into the reader pane** (and back), so the two panes
  form one keyboard cycle *inside the mode* rather than the canvas ejecting focus from the app. This is
  the highest-risk consequence and is delegated to the Explorer component `/design` with a
  focus-integration test (the P2-FOCUS analogue at the mode level). ADR-0015 is flagged
  `review-suggested`.
- **Negative / cost**
  - The shell gains state it did not have (a view mode) and a presenter; a mode with a live WebView2 or
    ConPTY child that is merely hidden (not unloaded) holds resources while inactive — accepted,
    because rebuilding them is the worse cost (the invariant above), and bounded because the mode set is
    small and the hidden surfaces are the same ones the Workbench would hold anyway.

## Delivery phasing (vertical slices)

1. **Walking skeleton** — the `ViewMode` value + the rail toggle + the body-content swap, with the
   Explorer surface showing the *existing* graph on one side and a reader stub (metadata + edges only,
   content mocked) on the other. Proves the swap, the retain-not-rebuild invariant, and the rail
   selector end-to-end. Human-validatable (toggle in/out, workbench intact); test-validatable (no
   rebuild of a live surface across a switch).
2. **Reader content by kind** — wire the reader to the node-content contract (ADR-0018 node-content-reader-contract): markdown/html
   rendered, code in the read-only editor. Mocked seam from Phase 1 becomes the real Core query.
3. **Keyboard cycle + responsive** — the canvas-trap↔reader focus routing and the narrow-viewport
   stacking (US-E8), each with its test.

## Amendment — 2026-09-11 (Ruling 52; `/define-architecture` of Addenda C and D)

Ruling 52 (`note-addendum-c-council-rulings`) **retains** this decision and its load-bearing
invariant and **amends** it in four clauses; a fifth (clause 4, from INV-0009) was added at the
Addenda C/D architecture gate. The original text above is left intact; the amendment
is read over it.

1. **The closed set is the Perspective set.** The primary-view-mode value's closed set becomes
   **Coding · Explore · Architecture** (Tests is a reserved name with no row and no rail item —
   Ruling 54). A *Perspective* (Addendum C page one, Ruling 50) **is** a primary view mode: the rail
   selects it, the body is its projection, the menu/title/status strips persist outside the swapped
   region. `ShellViewMode` is renamed `Perspective` **only in the commit that implements this
   amendment** (Ruling 50). The registry that carries the set and its order is ADR-0030.
2. **A body may be a docking host of its own.** Coding's body is today's workbench host, unchanged in
   mechanism; **Architecture's body is a second AvalonDock host** under the same presenter, with a
   surface-kind allow-list (ADR-0030) and its own layout service, controller and persistence slot
   (ADR-0031, ADR-0032). "New full-window surfaces are new modes, not new shell mechanisms" still
   holds: a host-bodied mode is the same seam with a different body.
3. **Explore stays the full-window `ExplorerSurface`** and does not become docked panes (Ruling 52d,
   Ruling 53: one graph substrate, two surfaces).
4. **A mode that unparents the docking host must say what a docking command does while it is the
   body — switch back, or refuse with a reason; never a silent model update.** INV-0009
   (`docs/investigations/INV-0009-a-session-document-opened-into-a-body-that-is-not-on-screen.md`,
   on `investigate/session-document-render`; its first defect class, registered there) reproduced it red: with Explore as the body,
   every `AddSurface` + `Render` command in `WorkbenchShell` (`:268-364`, `:1574`, `:2909`) opened a
   document into the layout model, configured it, and announced success from the model while
   nothing could render — the operator pressed File → New Session in Explore and saw nothing. The
   rule this record now carries: **a catalog command that opens a dock document is covered by one
   shell seam** (INV-0009 F1: `WorkbenchShell.DocumentOpening` — a `Func<bool> EnsureWorkbenchBody`
   invoked at the top of every such command), which **switches the body to the host that admits the
   kind, document first, then the switch, and folds *"Left Explore."* into the announcement**;
   retain-never-rebuild is kept (the switch is a view change). Under Addendum C that seam **is**
   `PerspectiveShell`'s routed kind-open (ADR-0030 `Resolve`; ADR-0031 rule 2's entry-verb rule) —
   the fix on `fix/session-document-render` is its walking skeleton, not a second mechanism.
   **Falsifying test:** `ANewSessionCreatedWhileExplorerIsTheBodyIsShown` (red today, exit 30 through
   `dotnet test`) and its sibling `LeavingExplorerShowsTheSessionCreatedInsideIt`; a `ShellModeController`
   unit test asserts one `shell.mode` diagnostic line per `Set` (INV-0009 F5). Two adjacent findings
   the same investigation verified are carried by ADR-0031 and the P1 note: **its second class** (a session
   created through the chooser with no workspace open — the operator ruled the chooser opens the
   workspace, then creates) and the reopen path's binder (`SessionComposerBinder.Bind(shell,
   config, providers, workspace, affirmation)` as the **one** construction site shared by New,
   Reopen and restore — still one registry / send-context / attachment-gate site, Ruling 47).
5. **Layout persistence gains one slot per host perspective** — the ADR-0013 amendment this record
   already named, now decided as ADR-0032 (one zone-envelope file per host; expand-only; an envelope
   carrying a kind its perspective no longer admits migrates by **drop-with-report**, never a
   crash and never silently). The Explore slot this ADR named for the split ratio and last node is
   **not built** (Addendum C non-goal 5); in-process retention within a run stands.

**The invariant, extended and re-proven.** *Retain, never rebuild* now holds across three bodies and
two hosts: the presenter holds every body alive and only unparents. Ruling 52 labelled the second
host **Inferred** ("a hidden `HwndHost` in host 2 restarts" was the named falsifier). Discharged:
`spikes/second-dock-host-unparent` (2026-09-11, PASS, exit 0) put two `DockingManager`s under one
`ContentControl`, a live `WebView2` and a raw `HwndHost` inside host B, cycled A → B → A → B three
times and B → Explore → B, and observed the same `CoreWebView2` object (reference identity), page
state (`n = 41`, `scrollY = 500`) intact, and the raw `HwndHost`'s HWND identical with zero
`DestroyWindowCore` calls — the "initialised once" count is enforced by the spike's own once-guard
and is not itself the evidence. **[Verified — run, for the presenter swap within one top-level
window]** Ruling 52's CONDITIONS do not fire;
Architecture's body stays a docking host. The slice's control is Addendum C **P-4** (a class diagram
in host B keeps its selection and scroll offset across a cycle; no `CoreWebView2` re-initialisation)
plus the headless identity test over all three bodies (US-C2), which re-scopes
`ExplorerModeTests.Toggle_FlipsModeAndRaisesModeChanged` (Addendum C §A12).

**Falsifying test for the amendment:** the P-4 runtime item, or the headless `Assert.Same` over
three bodies, fails — a second factory call, a new instance, or a `CoreWebView2` re-initialisation
across a perspective cycle. If it fails, Ruling 52's CONDITIONS apply: Architecture's body becomes a
non-docking composite and the ruling is re-issued.

**Rejected at the amendment:** *superseding* this ADR with a "perspective" ADR (the mechanism is
unchanged — a supersession would re-decide options A and B, which still lose for the same reasons);
*making Explore a docked pane inside Architecture* (option A's defect, re-opened; Ruling 52 cuts it).

