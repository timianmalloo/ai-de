---
id: "note-20260829-facelift-flat-to-soft-islands"
title: "Facelift direction: evolve the workbench from strict-flat to soft islands, not a redesign"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: ""
tags: [decision-note, ui-design, facelift, design-language]
links:
  - { to: spec-app-facelift, rel: relates-to }
  - { to: mockup-app-facelift, rel: relates-to }
review-by: 2027-02-27
review-suggested: []
summary: >-
  The facelift evolves the existing DESIGN.md rather than replacing it — three facet moves
  (Depth Flat→SoftShadow, rounded.lg 8→10 + island 12, Nav +MenuBar) toward the JetBrains
  Islands register, with density and the WCAG/confidence floors held constant.
---

# Facelift direction: strict-flat → soft islands (an evolution, not a redesign)

*A decision note (`knowledge-visualization.md` V17).*

- **Kind:** decision
- **Confidence:** Verified *(grounded in the existing DESIGN.md and `kb-wpf-modern-ui-styling`)*
- **Made during:** `/specify` + `/ui-design` of the app facelift, 2026-08-29

## The call
The existing `DESIGN.md` is deliberately **strict-flat** (no shadows on docked panes, 1px borders, radii 3–8px)
— a considered choice for an evidence-first workbench under cognitive load. The user asked for "rounded/softer
edges and styling." Rather than discard that reasoning, the facelift **evolves** the design language with three
bounded facet moves (grammar G9): `Depth: Flat → SoftShadow` (resting elevation on island panes), `rounded.lg
8→10px + rounded.island 12px`, and `Nav: +MenuBar`. The archetype (Workbench / MultiPanelWorkstation) does not
change kind. **Three floors are held constant and were the gate conditions:** density is unchanged (softness via
radius/elevation/space-rhythm, not by growing controls), WCAG 2.2 AA contrast is preserved (the DESIGN.md
contrast audit fails the build otherwise), and confidence stays glyph+word+colour. The WPF mechanics are gated
by the wpf-styling-expert: `AllowsTransparency=False` (keep the DWM shadow/corners), no effects over
HwndHost/WebView2 panes (airspace), shadows budgeted+cached.

## Alternatives dismissed
- **Full redesign to a rounded/Material look** — rejected; discards the evidence-first reasoning and risks the
  JetBrains-classic-UI density backlash. The three-facet evolution captures "modern/softer" without the cost.
- **Keep strict-flat, decline the request** — rejected; the user explicitly wants softer, and the Islands
  register achieves it within the floors.

## Promotion rule
If the softening proves to hurt operator density/legibility in use, revert the `Depth` facet (the shadows) while
keeping the radius/menu/icon changes — they are independently reversible. If the design language stabilises,
promote the facet changes into an ADR alongside the app's architecture.
