---
id: adr-0017-primary-view-mode
title: "ADR-0017 — Full-window surfaces are a primary view mode (body-content swap), not a dock pane or a modal overlay"
type: adr
status: proposed
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
review-by: 2027-02-28
summary: >-
  A surface that needs the whole body (the Knowledge Explorer's graph+reader) is presented as a
  primary VIEW MODE the shell holds — Workbench | Explorer — realised as a body-content swap of the
  region the docking host occupies, with the activity rail as the mode selector. Rejects making it a
  dock pane (it would compete for space — the defect being fixed) and a modal overlay (the rail must
  persist and it is not dismiss-only). The non-active mode's state is retained, never rebuilt.
---

# ADR-0017 primary-view-mode: Full-window surfaces are a primary view mode (body-content swap)

- **Status:** Proposed 2026-08-30. Raised for the full-window Knowledge Explorer
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
