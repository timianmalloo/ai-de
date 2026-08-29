---
id: mockup-facelift-elevate
title: "Facelift elevate proposals — visualization"
type: doc
status: draft
owner: "@copilot-design"
phase: "facelift"
tags: [ui-design, mockup, facelift, icons, wayfinder, elevate]
links:
  - { to: review-ui-facelift, rel: documents }
  - { to: spec-app-facelift, rel: relates-to }
review-by: 2026-11-29
summary: >-
  A self-contained visualization of the four highest-leverage /ui-design elevate proposals — an
  icon'd clean menu (no goofy block), an icon'd activity rail with labels + active state, tabs with
  a type-glyph, and Wayfinder empty/loading/error states — with the standard review harness.
---

# Facelift elevate proposals — visualization

`docs/mockups/facelift-elevate.html` renders the top proposals from `docs/reviews/ui-facelift.md`
so they can be seen and reacted to before implementation:

1. **Icon'd clean menu** — the corrected dropdown (border fills the popup, no lopsided block), with a
   leading Lucide-style glyph per item, a grouped separator, and right-aligned shortcuts.
2. **Icon'd activity rail** — icon + label + active-state accent bar + tooltip, replacing the four
   cryptic glyphs.
3. **Tabs with a type-glyph** — a small per-kind icon on each surface tab.
4. **Wayfinder empty states** — the dead-ended "not available" / "no workspace" panes become
   inviting first actions ("Open a workspace to explore its graph" + an Open button), with the
   loading (skeleton) and error (retry) hard states in the harness.

Icons are Lucide-style geometry (MIT); the real app would ship them as XAML `Path` data. Harness
switches state · theme (dark/light/high-contrast) · viewport · reduced-motion, with a token-layer
contrast readout.
