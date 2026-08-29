---
id: kb-wpf-styling-comparables
title: "Modern WPF Styling — comparables & libraries"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [wpf, control-libraries, ide-ux, exemplars, licences]
links:
  - { to: kb-wpf-modern-ui-styling, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The permissively-licensed WPF control/styling library landscape (named, with licence, look
  and maintenance), plus the IDE and creative-tool UX exemplars that define the modern-soft target.
---

# Comparable solutions, libraries & UX exemplars

## WPF UI/styling libraries (open source, permissive)

| Library | Licence | Look / capability | Maintenance | Confidence |
|---|---|---|---|---|
| **Built-in .NET Fluent theme** (`PresentationFramework.Fluent`) | Part of .NET (MIT) | Windows 11 Fluent, light/dark, accent-tracking; Mica partial→.NET 10 | First-party, shipping | Verified [W3][W4] |
| **WPF UI** (`lepoco/wpfui`) | MIT | Fluent controls + `FluentWindow` with Mica/Acrylic backdrop wrapper; navigation, dialogs, icons | **Actively maintained** (~4.x) | Verified [W7][W25] |
| **ModernWpf** (`Kinnara/ModernWpf`) | MIT | WinUI/Fluent look for legacy + modern WPF; adaptive themes | **Winding down** (nearing 1.0 freeze) | Verified [W8] |
| **MahApps.Metro** | MIT | Metro/flat modern theme + `MetroWindow`; mature, large community | Mature, maintained | Verified [W9] |
| **HandyControl** | MIT | 80+ controls, new window, notifications, growl, rich data controls | Mature, maintained | Verified [W10] |
| **Material Design In XAML Toolkit** | MIT | Google Material: rounded, **elevation shadows**, ripple; composes with MahApps | Mature, maintained | Verified [W11] |
| **Dragablz** (`ButchersBoy/Dragablz`) | MIT | Chrome-style draggable/tearable tab control (`TabablzControl`) | Mature | Verified [W12] |
| **AvalonDock** (`Dirkster99/AvalonDock`) | MIT | Docking (already selected — see `ai-native-ide-shell`) | Maintained | Verified (cross-ref) |
| **AdonisUI**, **Fluent.Ribbon**, **Panuon.WPF.UI** | MIT | Alt modern themes / ribbon / rounded control pack | Varies | Inferred (ecosystem) |

**Commercial (reference only, not adopted):** Syncfusion (free Community licence under revenue threshold),
DevExpress, Telerik, Actipro, Infragistics — richer control suites and support, but proprietary and a
licensing/cost commitment. Listed so the field is complete; the project's constraint is permissive OSS.

**The awesome-wpf list** ([W23]) is the curated index to verify any of the above and find more.

## How the leaders frame "modern desktop"

| Product | Framing | What to borrow | What to avoid |
|---|---|---|---|
| **JetBrains New UI / Islands** | Rounded, elevated "islands" tool windows; Inter font; restrained accent; per-project accent colour; compact mode | The islands IA, spacing generosity, tool-window elevation, the Int UI Kit token reference | Over-rounding functional density; the classic-UI users' backlash shows density still matters |
| **VS Code** | Minimal chrome, strong focus rings, activity bar, command palette, excellent density options | Focus-ring discipline, command-palette pattern, panel collapse memory | Its extension-host architecture (not our problem) |
| **Zed** | Ultra-minimal chrome, always-visible focus, instant context switch, low colour noise | Chrome restraint, sharp dark contrast | — |
| **DaVinci Resolve** | Page-based (task-specific tabs), dark immersive, colour-coded, large icons for low-light | Task-focused tab/page separation, dark immersive canvas, colour-coding of function | Limited panel flexibility |
| **Adobe Premiere** | Modular dockable panels, customisable workspaces, "see everything" | Dockable workspace presets | Panel clutter when everything is open |

*(JetBrains/VS Code/Zed Verified [W16][W17][W18]; the dark-theme rationale [W21]; creative-tool comparison [W22].)*

## Adjacent problems worth borrowing from

- **Web design tokens** — the "islands" look is a token system (spacing scale, radius scale, elevation scale,
  one accent). Author it once as WPF resources; this is the pack's `DESIGN.md` discipline
  (`ui-interaction-design.md` U3a) applied to XAML `ResourceDictionary`.
- **The pack's own UI standards** — `ui-interaction-design.md` (U1–U20 excellence floor), `ui-design-craft.md`
  (DX1–DX25 direction→system→screens→critique), `ui-archetype-catalog.md` (the IDE maps to **B1
  KeyboardVelocity** / MasterDetail, dark-adaptive, compact), and `technical-ui-design.md` (dense-with-hierarchy
  for the graph/diagram panes). These govern *how excellent* the result must be; this base supplies *the WPF
  means*.
