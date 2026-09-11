---
id: mockup-perspective-shell
title: "Perspective shell — Coding · Explore · Architecture (Addendum C elevate mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [ui-design, mockup, addendum-c, perspective, rail, menu, docking, contrast]
links:
  - { to: ui-review-perspective-shell, rel: documents }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: mockup-session-front-door, rel: refines }
  - { to: ui-review-operator-feedback, rel: relates-to }
  - { to: note-addendum-c-design-signature, rel: relates-to }
  - { to: note-addendum-c-design-menu-names, rel: relates-to }
review-by: 2026-12-11
review-suggested: []
summary: >-
  A self-contained, dependency-free mockup of the shell as three perspectives: the rail (New session
  + Coding · Explore · Architecture as a radio group; Tests reserved and absent), the menu bar derived
  per perspective, the Coding and Architecture default layouts with their empty states, Explore
  full-window and unchanged, the switch in its retained / opening / error states, the drop-with-report
  status, the dock tab strip in all four states, and a live contrast audit over every ink/ground
  pairing including the accent-as-ground family the runtime census found failing.
---

# Perspective shell — Coding · Explore · Architecture

`docs/mockups/perspective-shell.html` renders the design language's
[perspective-shell section](../../DESIGN.md) behind
[`docs/reviews/ui-perspective-shell.md`](../reviews/ui-perspective-shell.md). Open it over
`file://`; there is no build step, no CDN and no network call.

## What it renders

1. **The rail** (PS-R1–PS-R4). New session above the divider in the accent fill with the on-accent
   ink (`accent-contrast`); three destinations as a vertical tab list with exactly one
   `aria-selected` and manual activation (Up/Down move focus only; Space, Enter or the gesture switch); the 3px accent bar as
   the non-colour active signal; the *opening* state (progress ring, status *"Opening
   Architecture…"*, focus unchanged) and the *error* state (danger badge, the reason in the tooltip,
   activation retries). No fourth item: "Tests" is a reserved name only.
2. **The menu bar, derived.** Coding: File · Edit · View · Window · Terminal · Help. Explore: File ·
   View · Help. Architecture: File · Edit · View · Window · Help. The View menu leads with the
   perspective radio group; every allow-list entry sits under View; the Terminal menu is absent
   outside Coding, not disabled. Keystrokes appear only for bound gestures.
3. **Coding (host A).** Center: the session document, or the empty state *"No session open."* with
   its first action; Left: **Terminal sessions**; Bottom: one terminal; Right: empty. Loading
   (restoring skeleton), error (a session that cannot be restored), overflow (nine document tabs).
4. **Explore.** The full-window `ExplorerSurface`, unchanged; focus lands on the search box, never
   in the canvas.
5. **Architecture (host B).** Center: Graph · Domain · Contexts · Joins as tabs; Left: Evidence;
   Right: Provenance rendering the selected row's detail (and its empty copy); Bottom collapsed; the
   graph's *showing 40 of 212* degraded state; the read-only banner; the evidence empty state.
6. **The tab strip** in its four states: rest, hover, selected-active (the one accent-filled tab,
   `accent-contrast` on `accent`), selected-inactive (`text` on `surface` with a 2px muted top edge as
   the state indicator). A keyboard-highlighted menu row draws the focus ring inset.
7. **The status strip** carrying the drop-with-report chip in the spec's plural and all-dropped
   forms, the switch outcome, and a keyboard-reachable close.

## Review harness

Perspective · switch state (retained / opening / error) · state (default / empty / loading /
overflow / error) · restore (clean / 3 panes dropped / every pane dropped) · menu (closed / File /
View / Terminal) · theme (dark / light / high-contrast) · viewport (1440 / wide / 1024) · persona
(operator / keyboard-only with focus-order badges and the focus-landing element outlined / screen
reader with the live-region trace in order) · reduced motion. The verdict strip reports computed
contrast failures, targets under 24px, rail targets under 44px, and the number of selected rail items
(must be exactly one). Every pairing is classified text / ui / decorative per DX11; under the high-contrast theme the values are stand-ins and the audit says *not measured*.

## What this mockup is not

Direction evidence only. The product is WPF (UI-T4): native accessibility, keyboard, DPI, windowing
and the retained-switch timing are runtime Proof Pack items (spec §A13, P-1…P-13) measured at the
slice, not here. The high-contrast values stand in for Windows system colours. Icons are
Lucide-style geometry (MIT); the real shell ships them as registry `Geometry` entries (`IconCoding`,
`IconArchitecture` are new). No generated imagery (UI-T2 does not fire).
