---
id: spec-named-dock-zones
title: "Named Dock Zones — workbench layout specification"
type: spec
status: in-review
owner: "@timianmalloo"
phase: "3"
tags: [workbench, layout, docking, panes, ux]
links:
  - { to: investigation-terminal-crash-and-pane-moves, rel: refines }
  - { to: architecture, rel: relates-to }
  - { to: spec-editor-surfaces, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Replaces the proportional split-tree workbench layout with a fixed frame of named,
  absolute dock zones (Left / Right / Bottom / Center) so that moving a pane can only
  change the zones it belongs to — never relocate or reorient an unrelated pane. Zones
  resize, collapse to a rail, and maximize reversibly; splits are scoped inside a zone.
---

# Named Dock Zones — workbench layout specification

## Provenance

This spec is the Phase-3 remedy from `docs/investigations/terminal-crash-and-pane-moves.md`
(defect class **DC-063**). The current layout is a **proportional split tree**
(`LayoutModel.SplitNode`/`StackNode`): removing a pane collapses a single-child split into its
child, which reorients/relocates the *sibling* pane, and every op rebuilds the whole dock view.
The user's repeated, correct complaint — *"if I add one pane to a different dock the only thing
that should happen is the pane surfaces in the new dock… are the docks themselves absolute and
then panes contained within docks? that seems like the mental model I would have had"* — is the
industry-standard model (VS Code, Visual Studio, JetBrains/Rider, AvalonDock). This spec adopts it.

---

## A. Functional layer (what & why)

### A1. Problem (solution-independent)

Arranging panes in the workbench produces **non-local, surprising** results: moving or closing one
pane changes the position or orientation of others, and workspace/pane actions trigger a full
re-draw where content jumps (e.g. a left column flips to a top row). The user cannot form a stable
spatial model of the workbench because the arrangement is emergent from a tree whose rebalancing is
invisible.

### A2. Core scenario

A developer has the graph in the center, a terminal at the bottom, and a class diagram on the right.
They drag the class diagram from the right to the bottom. **Exactly one thing happens:** the class
diagram leaves the right zone and appears in the bottom zone. The graph does not move. The terminal
does not move or resize. The right zone, now empty, collapses to its rail. Nothing else redraws.

### A3. Users & jobs-to-be-done

- **The developer arranging their workspace** — wants a predictable, stable layout they can build a
  muscle-memory of; wants tool panels (terminals, diagnostics, diagrams) out of the way but one
  click from returning; wants the document/editor area to be the stable center of gravity.

### A4. In scope

- A fixed frame of **named zones**: `Left`, `Right`, `Bottom`, `Center`.
- Per-zone **resize** (drag the boundary with the center), **collapse to rail** (tool zones only),
  and **maximize** (reversible).
- **Move a pane between zones** with strict containment (only source + destination change).
- **Within-zone arrangement**: a zone holds a tab stack, and the Center may hold nested editor
  groups (a split *scoped to the zone*).
- Migration from the existing split-tree layout and its persisted `layout.json`.

### A5. Non-goals (explicit)

- **Arbitrary cross-zone nesting** (the current split tree). Deliberately removed — it is the cause.
- **Floating windows redesign** — floating panes keep working as today; only docked layout changes.
  (A floated window is the one place a pane may carry its own UI thread — noted, not built here.)
- **Per-zone theming / new visual identity** — the UI layer restyles the chrome minimally; a full
  visual language pass is separate.
- **Reflowing terminal content on zone resize** — the existing resize semantics are unchanged.

### A6. Functional acceptance criteria (falsifiable)

> Written to be traceable to tests in `AiDe.Core.Tests` (layout model) and `AiDe.App.Tests`
> (workbench). Each is a behavior a test asserts.

- **AC-F1 — Containment on move.** *Given* panes in ≥3 zones, *when* a pane moves from zone X to
  zone Y, *then* only X and Y's contents change; every other zone's content, order, size, and
  collapsed-state are byte-identical before and after. **(The DC-063 regression.)**
- **AC-F2 — No orientation flip.** *When* a pane is removed from a zone, *then* no other zone's
  panes change position or orientation; a zone never "collapses into" another zone.
- **AC-F3 — Center always present.** *At all times* the Center zone exists and is never collapsed to
  nothing; closing the last center pane leaves an empty Center placeholder (empty state), not a
  missing zone.
- **AC-F4 — Tool zones collapse reversibly.** *When* a tool zone (Left/Right/Bottom) is collapsed,
  *then* its panes are retained; *when* re-expanded, the same panes in the same order and the same
  active tab return.
- **AC-F5 — Maximize is reversible.** *When* a zone or pane is maximized then restored, *then* the
  prior arrangement (all zone sizes, collapsed states, active tabs) is exactly restored.
- **AC-F6 — Resize is local.** *When* a zone boundary is dragged, *then* only that zone and the
  Center change size; other tool zones keep their sizes.
- **AC-F7 — Add is destination-local.** *When* a new pane is opened into a target zone, *then* only
  that zone gains the pane (as a new active tab); no other zone redraws or moves. (Supersedes the
  earlier `DocumentPlacementPolicy` behavior — placement now resolves to a **zone**, not a tree
  position.)
- **AC-F8 — Workspace open preserves arrangement.** *When* a workspace is opened, *then* the current
  zone arrangement is kept (per the resolved `workspace-open-layout-restore` decision); opening a
  workspace never resets zones.
- **AC-F9 — Migration is lossless & reversible.** *Given* a persisted split-tree `layout.json`,
  *when* it is loaded, *then* every surface appears in a deterministic zone (Center for documents,
  the mapped tool zone otherwise) with no surface lost; the old format still round-trips for one
  release (expand-migrate-contract).

---

## B. UX layer (how it works — structure, flow, IA)

### B1. Information architecture

The workbench frame is a **cross** of four named regions around a central document area — the
canonical IDE shell:

```
┌──────────────────────────────────────────────┐
│                  (title / menu)                │
├───┬──────────────────────────────────┬─────────┤
│ L │                                  │    R    │
│ e │            C E N T E R           │  i g h  │
│ f │        (documents / editor       │  t      │
│ t │         groups — the anchor)     │  zone   │
│   │                                  │         │
├───┴──────────────────────────────────┴─────────┤
│                   B O T T O M                    │
│         (terminals, diagnostics, output)         │
└──────────────────────────────────────────────────┘
  ▐ rails: a collapsed zone shows as a thin activity rail with its pane icons ▐
```

- **Center** is the anchor — always visible, holds the document/diagram/graph surfaces, and may
  split into **editor groups** (a within-zone split, e.g. two class diagrams side by side).
- **Left / Right / Bottom** are **tool zones** — each a tab stack of tool panes (terminals,
  diagnostics, explorer, diagrams that the user parked there). Each can collapse to a **rail** (a
  thin strip of pane icons) and expand back.
- A pane belongs to **exactly one zone** at a time. Its identity and session persist across moves.

### B2. Primary user flows (happy + unhappy)

- **Move a pane between zones** — grab a tab → drag → zone drop-targets highlight → drop on a zone →
  pane leaves source (source collapses to rail if now empty) and becomes the active tab of the
  destination. *Recovery:* drop outside any zone = no-op (pane stays); Esc cancels the drag.
- **Collapse / expand a tool zone** — click the zone's collapse chevron (or its rail) → zone
  animates to/from the rail; panes retained. *Empty state:* a zone with no panes shows only its rail
  with a muted "drag a tab here" affordance on hover.
- **Maximize / restore** — double-click a tab or the zone header (or ⌘/Ctrl+M) → the zone/pane fills
  the frame; other zones hide to rails; restore returns the exact prior arrangement (memo).
- **Resize** — drag the boundary between a tool zone and the Center. *Constraint:* a zone has a min
  size; dragging past it collapses the zone to its rail rather than to zero.
- **Open a new pane** (e.g. "New class diagram", "New agent terminal") — resolves to a **target
  zone** by kind (documents → Center; terminals/diagnostics → Bottom; explorers → Left), added as
  the active tab there; if the user had focus in a compatible zone, honor that zone (fixes the
  earlier "added to the left window when I was focused in the right" complaint).

### B3. States

Each zone specifies: **populated**, **empty** (rail + hint), **collapsed** (rail only),
**maximized** (fills frame), **drag-target** (highlighted drop zone during a drag), and the Center's
**no-documents** placeholder. Each pane tab: default / hover / active / focused / dragging / closing.

### B4. UX acceptance criteria

- **AC-U1 — Drop targets are discoverable.** During a tab drag, the four zones show distinct drop
  highlights; the pane cannot be dropped into an invalid target (no ambiguous half-drops).
- **AC-U2 — Focus-aware open.** Opening a new pane of a zone-compatible kind while a zone is focused
  targets that zone; otherwise the kind's default zone.
- **AC-U3 — Rail affordance.** A collapsed zone is always re-openable via its rail with one click;
  no zone can become permanently unreachable.
- **AC-U4 — No spatial surprise.** No flow above ever changes a zone the user did not act on
  (the UX statement of AC-F1/F2).

---

## C. UI layer (how it looks — surface)

Specified against `ui-interaction-design.md` (U1–U20); the workbench is a **desktop IDE** medium
(WPF), so the authoritative reference is the platform (Fluent/native) plus the app's existing tokens.

- **Archetype (per `ui-archetype-grammar.md`):** `EnterpriseMasterDetail`-adjacent workbench —
  `Type:OLTP; Arch:SPA; Layout:MultiZoneWorkbench; Density:Compact; Nav:Sidebar+CommandPalette;
  Depth:Flat; Motion:Micro; Pacing:Freeform; A11y:WCAG_2.2_AA`. Zones are stable containers, not a
  bento of equal cells.
- **Tokens:** zone boundaries, rail width, tab metrics, drop-highlight color, collapse/maximize
  chevrons all reference existing app tokens (no arbitrary values, U3/U20). Rails reuse the activity
  strip metrics.
- **Chrome, minimal:** a zone header is a thin tab strip + a collapse chevron + an overflow menu; a
  rail is an icon column. The Center has no chrome of its own beyond editor-group tab strips.
- **Motion:** collapse/expand and maximize/restore animate ≤200ms, honor `prefers-reduced-motion`,
  and never block input (U10). Moving a tab does not animate the *other* zones (they are stable).
- **Accessibility (U16, hard floor):** every zone and tab is keyboard-reachable; collapse/expand/
  maximize/move have keyboard commands; focus order runs Center → tool zones; drop targets have
  accessible names; contrast on rails/highlights meets AA. The UX & Accessibility lens holds the
  veto.

### C1. UI acceptance criteria

- **AC-UI1 — Tokenized.** No zone/rail/tab metric is a literal; all resolve to tokens
  (`design-lint.py` clean).
- **AC-UI2 — Complete states.** Every zone state in B3 is rendered in the mockup and the build
  (populated / empty / collapsed / maximized / drag-target / center-empty).
- **AC-UI3 — Reduced motion.** With reduced motion, collapse/maximize are instant; no jitter.

---

## Traceability

| Layer | Realized by | Proven by |
|---|---|---|
| Functional | `LayoutModel` zone types + `LayoutService` zone ops (ADR-0021) | `AiDe.Core.Tests` layout model tests (AC-F1…F9) |
| UX | `WorkbenchShell` zone hosting + drag/collapse/maximize handlers | `AiDe.App.Tests` workbench tests (AC-U1…U4) |
| UI | Zone chrome + rails against tokens; `docs/mockups/named-dock-zones.html` | `design-lint.py`, mockup review harness (AC-UI1…UI3) |

## Open questions for the architecture / design gates

1. Is `Top` a zone in v1, or deferred? (Default: **deferred** — Left/Right/Bottom/Center only.)
2. Does the Center allow only horizontal editor-group splits, or a small nested grid? (Default:
   **horizontal + vertical editor groups**, but *scoped to Center*, never crossing into tool zones.)
3. Migration: keep reading old `layout.json` for one release, or one-shot convert on first open?
   (Default: **read-and-convert on open, write new format**, keep a converter for one release.)
