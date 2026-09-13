---
id: mockup-perspective-shell
title: "Perspective shell — Coding · Explore · Architecture · Coordination (Addendum C elevate mockup, Rulings 83–84)"
type: doc
status: draft
owner: "@timianmalloo"
phase: "Addendum C · Shell lane SH-4 (design node D3, 2026-09-13)"
tags: [ui-design, mockup, addendum-c, perspective, rail, menu, docking, contrast]
links:
  - { to: ui-review-perspective-shell, rel: documents }
  - { to: ui-review-operator-findings-2026-09-13, rel: documents }
  - { to: note-adr-0030-0032-amendment-coordination, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: mockup-session-front-door, rel: refines }
  - { to: ui-review-operator-feedback, rel: relates-to }
  - { to: note-addendum-c-design-signature, rel: relates-to }
  - { to: note-addendum-c-design-menu-names, rel: relates-to }
review-by: 2026-12-13
review-suggested: []
summary: >-
  A self-contained, dependency-free mockup of the shell as four perspectives: the rail (New session
  + Coding · Explore · Architecture · Coordination as a manual-activation tab list; Tests reserved
  and absent), the menu bar derived per perspective, Coding's default re-cut by Ruling 83 (the
  session docked at Left, the Center's two empty copies, one terminal), the Coordination host of
  Ruling 84 (Terminal sessions at Left; Ledger · Leaderboard · Message board tabs; four states),
  Explore full-window and unchanged, Architecture as before, the switch in its retained / opening /
  error states, the drop-with-report status including the five Loomkeeper kinds dropped from Coding
  naming Coordination, and a live contrast audit over every ink/ground pairing.
---

# Perspective shell — Coding · Explore · Architecture · Coordination

`docs/mockups/perspective-shell.html` renders the design language's
[perspective-shell section](../../DESIGN.md) behind
[`docs/reviews/ui-perspective-shell.md`](../reviews/ui-perspective-shell.md). Open it over
`file://`; there is no build step, no CDN and no network call.

## What it renders

1. **The rail** (PS-R1–PS-R4). New session above the divider in the accent fill with the on-accent
   ink (`accent-contrast`); four destinations as a vertical tab list with exactly one
   `aria-selected` and manual activation (Up/Down move focus only; Space, Enter or the gesture switch); the 3px accent bar as
   the non-colour active signal; the *opening* state (progress ring, status *"Opening
   Architecture…"*, focus unchanged) and the *error* state (danger badge, the reason in the tooltip,
   activation retries). The fourth item is **Coordination** (Ruling 84; `IconCoordination`, three
   nodes joined; tooltip *Coordination — Ctrl+4*). No fifth item: "Tests" is a reserved name only.
2. **The menu bar, derived.** Coding: File · Edit · View · Window · Prompt · Help. Explore: File ·
   View · Help. Architecture and Coordination: File · Edit · View · Window · Help. The View menu
   leads with the perspective radio group (four rows, Ctrl+1–4); every allow-list entry sits under
   View — the five Loomkeeper *Show* entries and the dispute verb under Coordination, never under
   Coding (Ruling 84); the Prompt menu is absent outside Coding, not disabled. Keystrokes appear only
   for bound gestures.
3. **Coding (host A), re-cut by Ruling 83.** Left: **the session document**, docked, never
   maximized (extent 1.3, Inferred; `session-conversation.html` measures it), empty until one opens
   (its collapsed rail); Center: the empty state — *"No session open."* + New session with no
   session in the layout, *"The session is docked at the left."* + Maximize the session (Ctrl+K, Z)
   while one is open at Left — or the documents the session touches (overflow: a code viewer and
   more tabs); Bottom: one terminal; Right: empty. Loading (restoring skeleton), error (a session that
   cannot be restored). No Loomkeeper kind anywhere in this host.
4. **Explore.** The full-window `ExplorerSurface`, unchanged; focus lands on the search box, never
   in the canvas.
5. **Architecture (host B).** Center: Graph · Domain · Contexts · Joins as tabs; Left: Evidence;
   Right: Provenance rendering the selected row's detail (and its empty copy); Bottom collapsed; the
   graph's *showing 40 of 212* degraded state; the read-only banner; the evidence empty state.
6. **The tab strip** in its four states: rest, hover, selected-active (the one accent-filled tab,
   `accent-contrast` on `accent`), selected-inactive (`text` on `surface` with a 2px muted top edge as
   the state indicator). A keyboard-highlighted menu row draws the focus ring inset.
7. **Coordination (host C, Ruling 84).** Left: **Terminal sessions** (the landing); Center: Ledger ·
   Leaderboard · Message board as tabs, Ledger first; Right: empty; Bottom collapsed; Daydreams from
   the View menu. Empty (*No episodes scored yet.*), loading, error (the ledger file's parse error,
   rows before it listed) and overflow (*≥ 1,668 episodes (capped)*) states.
8. **The status strip** carrying the drop-with-report chip in the spec's plural and all-dropped
   forms — and the five Loomkeeper kinds dropped from a pre-C Coding layout, reported naming
   Coordination — the switch outcome, and a keyboard-reachable close.

## Review harness

Perspective (four) · switch state (retained / opening / error) · state (default / empty / loading /
overflow / error) · restore (clean / 3 panes dropped / the five Loomkeeper kinds dropped / every pane
dropped / a duplicate / a newer schema) · menu (closed / File / View / Prompt) · theme (dark / light / high-contrast) · viewport (1440 / wide / 1024) · persona
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
