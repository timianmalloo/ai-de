---
id: review-ui-facelift
title: "UI review — the facelift, elevate pass"
type: doc
status: accepted
owner: "@copilot-design"
phase: "facelift"
tags: [ui-design, review, facelift, elevate]
links:
  - { to: spec-app-facelift, rel: relates-to }
  - { to: mockup-workbench, rel: documents }
  - { to: mockup-app-facelift, rel: documents }
review-by: 2026-11-29
summary: >-
  An elevate-mode /ui-design review of the shipped WPF facelift from live screenshots: the menu
  "goofy block" root-caused and fixed, the craft-gate measurement recorded, and a ranked plan of
  what else to do to reach best-in-class — led by a cohesive icon system.
---

# UI review — the facelift (elevate pass)

**Mode:** elevate. **Surface:** the running AI-DE WPF workbench (dark facelift), reviewed from live
screenshots + the implementation (`App.xaml`, the surface `.cs` files) + the design language
(`DESIGN.md`) + the mockups. **Archetype:** Workbench / MultiPanelWorkstation (unchanged, correct
for the job — an IDE-style multi-pane tool; reading is parallel here, so a dense docked layout fits).

## Measurement (DX23)

**Craft detector over the 5 mockups (the HTML proxies of the surfaces):** 41 findings — 28 Major,
13 Minor. Rule breakdown: `undersized-ui-text` 21, `cramped-padding` 10, `tiny-text` 7,
`nested-cards` 3. **No `design-system-color`** (token discipline clean) and **no `skipped-heading`**
(a11y heading order clean) — both were fixed in the prior craft-gate pass. The residue is
**review-harness chrome** (CD14 — never ships) and **deliberate dense-IDE meta** at 11px (DX17 —
density calibrated for an expert audience, consistent with VS Code / JetBrains).

**Live-screenshot observations (the real app, which the detector cannot see):**
- The dropdown menus rendered a **lopsided light "block"** framing the dark menu — a real defect.
- Panes render as **rounded islands** over darker gaps; document **tabs are rounded**; the palette is
  cohesive dark; the accent is `#5B9DD9`. The core facelift reads as intended.
- **Icons are largely absent**: menus are text-only, tabs have no type glyph, the left activity rail
  is four cryptic unlabeled glyphs.
- Empty states ("*X* is not available in this build", "No workspace is open") are **honest but dead-
  ended** — no next-action affordance.

## Rubric findings (DX22, structure → surface)

| # | Dim | Sev | Location | Evidence | Fix | Conf |
|---|-----|-----|----------|----------|-----|------|
| 1 | 17 Craft | 3 Major | dropdown menus | Lopsided light block around the dark menu — the popup `Border` had `Margin="2,2,12,12"` + a `DropShadowEffect`; the asymmetric margin/shadow area reads as a light frame (and violates App.xaml's own "radius+border, not shadow" principle) | Popup border fills the popup (`Margin="0"`, no shadow) → clean rounded thin-bordered card | Verified (fixed this pass) |
| 2 | 11 Archetype/Identity | 3 Major | whole app | No icon system — menus, tabs, activity rail, graph nodes all lack iconography; the spec explicitly asked for "icons and menu system" | Adopt a permissive icon set (Lucide MIT / Fluent System Icons MIT); wire menu icons, rail icons+tooltips, tab type-glyphs, node-type glyphs | Verified (gap) |
| 3 | 12 State completeness | 2 Minor | evidence panes + graph canvas | Empty states are dead ends ("not available", "No workspace open") — no CTA | Apply the Wayfinder pattern: an "Open workspace…" CTA + one line teaching the first action | Verified |
| 4 | 2 Match real world | 2 Minor | left activity rail | Four unlabeled glyphs; no tooltip, no active-state label | Tooltips + active indicator + labels-on-hover | Inferred |
| 5 | 17 Craft | 1 Nit | dense meta rows | 11px meta text flagged by the detector | Verify against the 11px floor in the real app; bump only if a non-expert surface reuses it | Verified |
| 6 | 10 Motion | 1 Nit | tab switch / pane open | `Transition:HardCut` — the app is static | Add subtle fade on tab-switch, ease on pane-open (DX19), reduced-motion aware | Inferred |

**Detector clean was NOT reported as "the design is good"** (CD13): the two highest findings
(menu block, missing icons) are things the detector cannot see — judged from the live surface.

## Ranked plan (DX25)

**Must-fix (defects):**
1. **Menu "goofy block"** — *done this pass* (popup fills the popup, no shadow/margin frame).

**Should-fix-next (highest impact, ranked):**
2. **A cohesive icon system** — *the single highest improvement-to-effort change.* It is explicitly in
   the spec, touches every surface (menus, tabs, rail, nodes, toolbar), and is the biggest remaining
   "identity" gap. Adopt Lucide (MIT) or Fluent System Icons (MIT), define an `icons:` set in
   DESIGN.md, and wire it through `MainMenuBuilder`, the tab headers, the activity rail, and the graph
   node renderer.
3. **Empty-state Wayfinders** — turn the dead-ended empty panes into inviting first actions
   ("Open a workspace to explore its graph" + an Open button). Design-owned; low effort, high warmth.
4. **Activity-rail affordances** — tooltips, active-state, hover labels for the left rail.
5. **Node inspection surface** — the marquee knowledge-exploration feature: click a graph node →
   render its natural form (Markdown rendered, HTML rendered, code with syntax highlighting) in an
   inspector. Needs a Markdown/HTML render control (see the editor-and-content-rendering knowledge
   base) and the CanvasGraph node payload.

**Worth-doing:**
6. **Richer 2D/3D graph** — elevate the canvas beyond the ring/sphere: typed edge labels, node-type
   glyphs, hover/selection, smoother pan/zoom.
7. **UML/ERM first-class surfaces** — designed (spec + mockups), *Core-gated* on new `KnownKinds`.
8. **Motion pass** — subtle, purposeful transitions + reduced-motion.
9. **Real-app WCAG audit** — contrast of dense meta, focus rings on rail/tabs, keyboard traversal.
10. **Splitter/gutter + terminal-chrome consistency** — style AvalonDock splitters to the palette.

**Blocked (Core-gated, tracked):** evidence-shortfall rendering (`Shortfall` on the view models);
UML/ERM surface kinds (`KnownKinds`).

## Residual risk / flagged unknowns

- The menu fix is validated to **parse + apply** (standalone) and compile; the *visual* result needs
  the user's on-screen confirmation (I cannot render the live app). If layered transparency is
  unavailable in some environment, rounded popup corners could show faint square edges — a far
  smaller artifact than the current block, and fixable by lowering the corner radius.
- The craft detector runs on the HTML mockups, not the WPF surfaces; the real-app craft (icons,
  motion, live contrast) is judged from screenshots and the implementation, labeled accordingly.
