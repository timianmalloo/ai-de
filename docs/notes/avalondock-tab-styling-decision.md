---
id: note-avalondock-tab-styling
title: "Decision — AvalonDock document-tab accent & corner styling"
type: decision-note
status: accepted
owner: "@copilot-design"
phase: "facelift"
tags: [wpf, avalondock, theming, facelift, deviation]
links:
  - { to: spec-app-facelift, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-11-27
summary: >-
  Records the deliberate decision NOT to retokenize the AvalonDock VS2013 dark theme's
  document-tab accent hue or round its tab corners, with the runtime evidence that made
  that a high-risk/low-value change, and the IDE-convention rationale for squared tabs.
---

# Decision — AvalonDock document-tab accent & corner styling

**Confidence: Verified** (evidence gathered by runtime probe + vstheme extraction, then the
probe scaffolding was disposed per the Spike Protocol).

## Context

After wiring `Shell.Manager.Theme = new Vs2013DarkTheme()`, the workbench renders cohesively
dark — panes, title bar, and tab strips all read as one surface, matching the approved
`app-facelift` / `workbench` mockups. Two residual observations from the running app
(screenshot, 2026-08-29):

1. Document tabs have **square** corners.
2. The **selected** document tab shows AvalonDock's default VS blue (~`#007ACC`), not our
   palette accent `#5B9DD9`.

The question was whether to retokenize the accent and round the tab tops.

## What was established (not guessed — E15)

A temporary STA probe loaded the `AvalonDock.Themes.VS2013` assembly and the embedded
`vs2013dark.vstheme` colour table. Findings:

- The theme's colours come from an **embedded `.vstheme` XML colour table** (three of them:
  blue/dark/light) mapped to WPF brushes by **compiled BAML implicit styles** in
  `themes/generic.baml` — there is **no small set of app-reachable `DynamicResource` accent
  keys**.
- The `Cider`-category tab colours (`TabItemSelected`, `TabBackground`, …) are all **dark grays**
  (`#1B1B1C`). The **blue selected-document accent is not among them** — it originates in
  AvalonDock's own document-well theming, deeper in the docking control templates.

Therefore retokenizing the accent or rounding the corners requires one of:
- decompiling `themes/generic.baml` to find the exact bound key and hoping it is overridable
  from app scope (uncertain), **or**
- supplying a **complete** custom `DictionaryTheme` (must cover every AvalonDock key or panes
  break — the exact failure we just fixed), **or**
- **retemplating** `LayoutDocumentTabItem` / `LayoutAnchorableTabItem` (high-risk surgery on a
  third-party docking control, theme-fragile across upgrades).

## Decision

**Defer the accent retokenization and keep squared document tabs**, recorded as a deliberate
deviation (Rules of the Road §4), on three grounds:

1. **Proportionality / risk.** All three routes are high-effort, high-risk changes to
   third-party docking chrome for a hue shift and a corner radius. The `wpf-styling-expert`
   lens counsels against retemplating third-party docking controls for cosmetic deltas.
2. **IDE convention (Jakob's Law, U12).** Squared document tabs are the established convention
   in VS, VS Code, and JetBrains IDEs. The "soft-islands" treatment is correctly applied to
   **panels, cards, buttons, and composited overlays** (command palette, evidence cards) — the
   surfaces a user reads and acts on — not to document-well tabs. Rounded docking tabs would be
   *less* conventional, not more polished.
3. **The accent hue is close and low-salience.** VS `#007ACC` and our `#5B9DD9` are the same
   blue family; the delta is a saturation shift on a thin selection indicator.

## If revisited

Only on explicit go-ahead, and via the **least-fragile** route: author a minimal
`DictionaryTheme` seeded from the VS2013 dark dictionary (`pack://…/AvalonDock.Themes.VS2013;
component/themes/generic.xaml`) with **only** the document-accent brush overridden — validated
against every pane state (float, dock, auto-hide, empty) before landing, since an incomplete
dictionary theme blanks panes.
