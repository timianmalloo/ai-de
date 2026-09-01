---
id: design-named-dock-zones-ui
title: "Named Dock Zones — UI design (direction, mockup, critique)"
type: design
status: draft
owner: "@timianmalloo"
phase: "3"
tags: [workbench, layout, docking, ux, ui-design]
links:
  - { to: spec-named-dock-zones, rel: implements }
  - { to: adr-0021-named-dock-zones, rel: relates-to }
  - { to: mockup-named-dock-zones, rel: documents }
review-by: 2027-02-28
summary: >-
  The /ui-design output for the named dock-zone workbench: the direction brief, the design
  decisions for zone chrome and rails, a self-contained mockup with a review harness, and a
  rubric critique with a ranked plan. Zones are stable containers; chrome is minimal; motion
  is purposeful and reduced-motion-safe; WCAG 2.2 AA is the floor.
---

# Named Dock Zones — UI design

## 1. Direction brief (words before pixels)

- **Who / emotional state.** A developer arranging a working IDE, mid-task, who wants the layout to
  *get out of the way and stay put*. The felt problem today is loss of spatial control — panes jump.
- **Archetype.** `MultiZoneWorkbench` (OLTP / master-detail family) — stable named containers around
  a document anchor, **not** a bento of equal cells. (`ui-archetype-grammar.md`.)
- **Three adjectives — and their opposites.** *Stable*, not rigid. *Quiet*, not sterile. *Dense*,
  not cramped. The chrome should recede; the content is the message.
- **Named references (what is taken).** VS Code — the activity-rail collapse idiom and the
  document-center anchor; Visual Studio — reversible maximize and dockable tool windows; JetBrains
  Rider — the calm, low-contrast tool-window headers. **Not** taken: any product's palette or icon
  set (adapt, never clone — U12).
- **Anti-goals.** Must not read as a tiling window manager (arbitrary splits everywhere); must not
  animate the *whole* frame on a single move; must not require a mouse (keyboard-first).
- **Constraints.** WPF desktop; existing app tokens are authoritative; WCAG 2.2 AA floor; the
  performance budget forbids whole-view redraw (that is the defect being fixed).

## 2. Design decisions (system before screens)

- **Zone chrome is minimal.** A zone = a thin tab strip + a collapse chevron + (Center/Bottom) a
  maximize affordance. No borders-as-decoration; **space and a single hairline** separate zones
  (DX13). The Center carries no chrome beyond its editor-group tab strip.
- **Rails.** A collapsed tool zone becomes a `--rail-w` (40px) icon strip — vertical for Left/Right,
  horizontal for Bottom — always one click from re-expanding (AC-U3). An empty zone's rail shows a
  muted "drag a tab here" hint on hover only.
- **The anchor reads as the anchor.** The Center uses a subtly distinct surface token
  (`--bg-center`) so the eye lands there first (DX18, one focal point).
- **Drop targets.** During a drag the valid zones get a dashed accent outline + a tinted fill
  (`--drop`), and a small drag-ghost follows the cursor. Invalid drops are no-ops (AC-U1).
- **Tokens.** Rail width, tab height, radius, gap, motion duration, and every colour are `:root`
  custom properties — no literals in component rules (U3/U20; `design-lint.py` intent).
- **Motion.** Collapse/expand and maximize/restore transition at `--motion` (160ms), ease-out;
  **only the acted-on zones animate** — the stable zones do not move, which is both the correctness
  property and the calm feel. `[data-motion="reduced"]` zeroes all transitions (U10, AC-UI3).

## 3. The mockup

`docs/mockups/named-dock-zones.html` (hub: `mockup-named-dock-zones`) — self-contained,
dependency-free, opens over `file://`, with the review harness (DX10): persona-free but
state/theme/motion switchable across the seven zone states in the spec's §B3. It is the artifact the
review below critiques and the reference the build targets.

## 4. Rubric critique (structure before surface — DX24)

| # | Dimension | Sev | Finding | Fix | Confidence |
|---|---|---|---|---|---|
| 1 | Archetype fit | 0 | Stable named containers match the entering/arranging task; not a dashboard | — | Verified (matches spec) |
| 2 | State completeness (U9) | 1 | Mockup renders all 7 states incl. empty-center + drag-target | Keep parity in the build | Verified (in mockup) |
| 3 | Accessibility (U16) | 3→ | Rails/collapse rely on icon glyphs; need accessible names + keyboard commands for collapse/maximize/move | Add `aria-label`s (done in mockup), bind keyboard commands in the build; **Blocker until the built control is keyboard-complete** | Inferred |
| 4 | Contrast (U16) | 2 | Dark-theme muted tab text on `--bg-tabstrip` is near the AA edge | Verify each pair at build; nudge `--fg-muted` in dark | Flagged (measure) |
| 5 | Motion (U10) | 1 | Only acted-on zones animate; reduced-motion path present | Keep; assert no whole-frame transition | Verified (in mockup) |
| 6 | Hierarchy / focal point (DX18) | 1 | Center distinguished by surface token | Consider a hair more contrast if it doesn't read as selected | Inferred |
| 7 | Copy (U11) | 1 | Empty-center + rail hint use real, instructive copy | Reuse the exact strings in the build | Verified |
| 8 | Token discipline (U3) | 1 | All metrics/colours are tokens in the mockup | Enforce with `design-lint.py` on the built `DESIGN.md` | Verified (mockup) |

**No Blocker on the *design*; the one Blocker (row 3) is on the *built control* — it is not "done"
until collapse / maximize / move-between-zones are fully keyboard-operable with accessible names.**
The UX & Accessibility lens holds that veto (U16).

## 5. Ranked plan (highest leverage first)

1. **Keyboard-complete the zone operations** (collapse/expand, maximize/restore, move-pane-to-zone,
   focus-cycle zones) with accessible names — clears the row-3 Blocker. *Highest leverage: it is the
   difference between shippable and not.*
2. **Wire the incremental per-zone adapter** so only acted-on zones repaint (the calm + the
   performance fix). Assert no whole-frame transition on a move.
3. **Lock the tokens into `DESIGN.md`** and gate `design-lint.py`; verify every dark-theme contrast
   pair (row 4).
4. **Build the seven states** to match the mockup (row 2) and add the state parity to the workbench
   tests (AC-UI2).

## 6. Self-check against the generic-AI-look tells (DX3)

- No violet/indigo gradient — accent is a single functional blue used only for the active-tab
  underline and drop highlight.
- Containment is space + one hairline, not a grid of equal bordered cards.
- Real, domain-accurate content (UserService.cs, OrderAggregate, `dotnet test` output), not lorem.
- One focal point (Center), asymmetric by intent (tool zones flank the anchor).
