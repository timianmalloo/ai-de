---
id: mockup-workbench
title: "AI-DE workbench — reviewable mockup"
type: doc
status: in-review
owner: "@timianmalloo"
phase: "0"
tags: [mockup, ui, workbench, docking, accessibility]
links:
  - { to: spec-ai-native-ide, rel: implements }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2027-02-26
review-suggested:
  - { by: spec-ai-native-ide, on: 2026-08-26, reason: "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice" }
summary: >-
  Self-contained, dependency-free mockup of the dockable workbench (US-9) with a review harness
  covering state, named layout, theme, viewport, reduced motion and the layout lock. Renders the
  hard states — drop target, keyboard move, keyboard resize, at-minimum, floating, collapsed,
  maximized, loading, empty, error, partial restore, unreadable layout and overflow.
---

# AI-DE workbench — reviewable mockup

Open [`workbench.html`](workbench.html) directly over `file://`. No build step, no CDN, no
dependencies. The harness bar is review chrome and never ships.

## What it demonstrates

| Hard state | Why it is in the mockup |
|---|---|
| **drop target shown before release** | The single most universal exemplar behaviour; a move that commits without showing its destination is the category's baseline failure. |
| **keyboard move** | SC 2.5.7 — the non-drag equivalent, with the destination named in text, not implied by a cursor. |
| **keyboard resize** | The Eclipse `Alt+-` → Size → arrows pattern. The selected edge is **visibly indicated with a direction cap**, because a keyboard user cannot see what a pointer user infers from the cursor. Try it: focus a splitter and press the arrow keys. |
| **at minimum size** | The refusal is a *state*, not a silent no-op. |
| **floating / collapsed / maximized** | Collapsed keeps the surface **name readable** — hiding a pane must not erase the knowledge that it exists. |
| **partial restore / unreadable layout** | The two recovery paths US-9 requires. Both name exactly what was lost and still leave a usable window. |
| **overflow** | A surface name long enough to prove the tab ellipsizes rather than pushing the strip apart. |
| **locked layout** | Photoshop's Lock Workspace, treated as the accessibility control it actually is. |

## Verified in-artifact

The harness computes contrast for each text/surface pairing live per theme, and counts interactive
targets under 24×24. Switching **Theme → high contrast** re-runs both. Layout operations use **zero
animation**, so the reduced-motion path is identical by construction rather than by a special case —
which is why the motion inventory in `DESIGN.md` is short by design, not by omission.

## Known gap this mockup cannot close

The mockup proves the *design* is keyboard-operable and announce-able. It does **not** prove the
chosen shell library can be made to behave this way — AvalonDock ships zero automation peers and a
mouse-only splitter (ADR-0012). The keyboard-resize interaction shown here is exactly the behaviour
that must be rebuilt on top of it, and that is a named work item, not an assumption.
