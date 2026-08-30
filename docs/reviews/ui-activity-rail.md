---
id: review-ui-activity-rail
title: "UI review — activity rail (elevate)"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [ui-review, ui-design, activity-rail, navigation, wcag, elevate]
links:
  - { to: spec-app-facelift, rel: relates-to }
  - { to: mockup-activity-rail, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2027-02-27
summary: >-
  Review/elevate of the workbench activity rail. Measured defect: at a 56px column, 9px captions
  under the glyphs clipped ("Coordinate" -> "ordina"). Fix (landed): icon-only rail with tooltip +
  accessible name — the VS Code / JetBrains idiom — 44px targets, and a softened borderless active
  pill. Registered UX-F (a caption clipped by its own container).
---

# UI review — activity rail (elevate mode)

## Direction brief

- **Who / JTBD:** a developer switching between the workbench's primary modes (Explore, Coordinate,
  Compose, Audit) from a persistent left rail, at a glance, without reading.
- **Archetype:** vertical **icon-navigation rail** — the VS Code Activity Bar / JetBrains tool-window
  bar idiom. `Nav:IconRail; Density:Compact; A11y:WCAG_2.2_AA`. Established pattern (Jakob's Law,
  U12): icon-only, tooltip on hover, accessible name, active-mode accent — text-under-icon in a
  narrow rail is the anti-pattern.
- **Adjectives (and opposites):** *glanceable* (not label-dependent) · *legible* (not clipped) ·
  *quiet* (not clunky/boxy).
- **References (what's taken):** VS Code Activity Bar (icon-only + tooltip + active accent);
  JetBrains new-UI tool bar (icon-only). **Not cloned** — keeps the shipped Lucide-style geometries
  and accent `#5B9DD9`.
- **Anti-goals:** clipped captions; a heavy bordered box on the active item.

## Measured (before)

| Metric | Value |
|---|---|
| Rail column width | **56 px** |
| Usable caption width (56 − 2×6 margin) | ~44 px |
| Caption font size | **9 px** |
| Captions that exceed the width | "Explore" (→ "xplor"), "Coordinate" (→ "ordina"), "Compose" (→ "ompos") |
| Distinct focal competitors in the rail | 4 (fine) |
| Redundant channels | icon **and** clipped caption saying the same thing |

The caption was a redundant, clipped second channel — it added no information the icon+tooltip did
not already carry, and it was the thing that broke.

## Rubric critique (structure → surface)

| # | Dimension | Finding | Sev | Fix |
|---|---|---|---|---|
| 1 | Archetype fit | Icon-rail archetype is correct; the caption was a mis-applied "labelled nav" facet at a width that cannot hold it | 3 Major | Drop captions; icon-only + tooltip |
| 2 | State completeness | rest/hover/focus/active present; hover carried no discovery once the label clipped | 2 Minor | Tooltip provides hover discovery |
| 3 | Accessibility | Clipped text is unreadable **and** `AutomationProperties.Name` already carried the true name — the caption was visual-only debt | 3 Major (a11y) | Remove caption; name + tooltip preserved; targets 44px (2.5.8) |
| 4 | Craft — focal/boxiness | Active item's hard border read as a clunky box against the soft-island facelift | 2 Minor | Borderless raised pill + accent glyph |
| 5 | Craft — hierarchy | Icon 20px with a 9px caption split attention in a tiny target | 1 Nit | Icon 22px, single element, centered |

**Detector note (CD13–CD14):** the deterministic gate cannot see caption clipping (it is a
width-vs-content layout fact, not an off-token value) — this defect is caught by the human/measure
layer, which is exactly why it survived a clean token lint. A clean detector run is a floor, not a
verdict.

## Ranked plan

- **Must fix (done):** remove the four captions → icon-only rail with tooltip + accessible name;
  44px targets. *This is the single highest-leverage change and it is landed.*
- **Should fix next (done):** soften the active state — borderless raised pill, accent glyph, 22px icon.
- **Worth doing (done):** a 3px left accent bar on the active item (the literal VS Code cue), landed
  beside the raised pill so the active mode reads by more than colour alone.
- **Worth doing (done):** keyboard roving-tabindex traversal of the rail — `TabNavigation=Once` +
  `DirectionalNavigation=Cycle`, so Tab lands on the rail once and Up/Down move between its buttons.

## Outcome

Landed in `src/AiDe.App/MainWindow.xaml`: icon-only rail, `ToolTip` + `AutomationProperties.Name`
retained on all four, `BorderThickness=0`, 44×44 targets, 22px icons. XAML markup-compiles
(MainWindow.baml regenerated). WCAG contrast holds at the token layer (see the mockup readout: active
icon ~5.1:1, muted ~7.6:1, focus ring ~6.7:1 — all ≥ 3:1 for graphics).

## Gate record

- **GATE ui-design · elevate · UX & Accessibility** — VERDICT **PASS**. State set complete;
  accessible name + tooltip carry the meaning the caption used to (badly); targets ≥ 44px; contrast
  ≥ AA. The author (Copilot design session) did not self-clear a hard veto — the fix *restores* the
  accessible name that was already present and removes a visual-only clipped string, so there is no
  accessibility regression to clear. Simplifier: `net: −8 elements` (4 captions + 4 wrapper stacks).
