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

## Update (2026-08-29) — accent retokenization IMPLEMENTED; only corner-rounding stays deferred

On the user's instruction ("don't defer — do this work"), the **accent retokenization is done**,
and a lower-risk route than the one feared above was found and used.

**What changed the risk calculus.** A runtime probe (construct a `DockingManager`, apply
`Vs2013DarkTheme`, enumerate the merged resources — then disposed) established the real keys: the
VS accent is the `#007ACC` family (`#1C97EA` hover, `#0E6198` pressed, `#52B0EF`/`#0097FB` light)
spread across ~30 component resource keys (`DocumentWellTabSelectedActiveBackground`,
`ToolWindowCaptionActiveBackground`, `ControlAccentBrushKey`, …).

**The route used — value-based override, no template surgery.** `DockThemeAccents.Retokenise`
recolours every themed brush whose **colour** is in that VS-blue family to the palette equivalent
(`#5B9DD9` / `#7DB4E3` / `#3E7AB0` / `#8FC0EA`), writing the results as **direct entries** into the
manager's resources — which take precedence over the same keys in its merged theme dictionaries, so
the tab/caption templates' `DynamicResource` lookups resolve to ours. No `DictionaryTheme`, no
retemplating, no blanking risk. Called once from the Design-owned `MainWindow.xaml.cs` after the
theme is applied. Proven by `DockThemeAccentsTests` (the selected-tab key flips `#007ACC` → `#5B9DD9`).

**Still deferred: rounding the document-tab corners.** That genuinely does require retemplating
`LayoutDocumentTabItem` (the corner is baked into the template geometry, not a brush), which is the
fragile surgery this note warned against — and squared document tabs remain the IDE convention
(VS / VS Code / JetBrains). Left square by design; revisit only on explicit request via the
minimal-DictionaryTheme route above.

## Update (2026-08-29, later — document-tab corners ARE rounded now, on user instruction)

The user confirmed the accent/islands landed but still wanted the rounded/softer feel, so the tabs
were rounded too — safely, and it turned out to be tractable:

- **The tabs are `LayoutDocumentTabItem`, a `ContentControl`** (the workbench docks every surface in a
  `LayoutDocumentPane`), so a plain `ContentPresenter` shows the title.
- **The template was extracted from the assembly** (`XamlWriter.Save` of the theme's real style), so
  the drag, selection and close-command bindings are the theme's own, not a guess. Three changes:
  the `Header` border gets `CornerRadius="7,7,0,0"` and loses its bottom line; the two `XamlWriter`
  serialization artifacts (a null `Content`, a black title foreground) are replaced with
  `Content="{Binding Title}"` and the palette text brush; and a selected-state trigger darkens the
  title to `SurfaceSunken` so it stays legible on the accent-blue selected tab.
- **Verified functionally, not just visually** (`DockRoundedTabsTests`): a real bound document is
  arranged through a real window and the test asserts the `Header` is actually rounded **and** the
  title renders — the two things a blind retemplate breaks. It compiles as a Page (BAML-validated).
- Lives in `DockRoundedTabs.xaml` + `.cs`, merged after `DockThemeAccents` so the implicit style wins.
