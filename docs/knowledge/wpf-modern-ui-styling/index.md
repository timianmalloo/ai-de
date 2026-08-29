---
id: kb-wpf-modern-ui-styling
title: "Modern & Soft WPF UI Styling — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [wpf, styling, fluent, windowchrome, dwm, mica, control-libraries, ide-ux, dark-theme]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: kb-ai-native-ide-shell, rel: relates-to }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Evidence base for giving the AiDe.App WPF shell a modern, soft, rounded look — Windows 11
  DWM rounded corners and Mica, WindowChrome custom title bars, the built-in .NET Fluent theme,
  the permissive (MIT) control-library landscape, soft-shadow performance, and the IDE/editor
  UX exemplars (JetBrains New UI / Islands, VS Code, Zed, DaVinci Resolve) that inform it.
---

# Modern & Soft WPF UI Styling — domain knowledge

**Domain & problem:** The AiDe.App shell is a .NET 10 / C# 14 WPF application whose default look is
"boxy" Aero2 chrome. The goal is a **modern, softer** aesthetic — rounded corners, subtle drop shadows,
restrained accent colour, generous spacing, a dark-first theme — matching the register of a contemporary
IDE (JetBrains New UI, VS Code, Zed) rather than a classic line-of-business WPF app. This base gathers the
techniques, the permissively-licensed control libraries, and the design exemplars that get there.

**Canonical framing:** The field frames this two ways that collide. **Microsoft's** framing (since .NET 9)
is *"opt into the built-in Fluent theme and let DWM handle the window"* — a first-party path to a Windows 11
look with minimal code. The **community** framing is *"adopt a control library"* (WPF UI, MahApps, HandyControl,
Material Design in XAML) that pre-styles every control and wraps the DWM interop for you. Our framing adds a
third constraint the others ignore: **most of AiDe.App's content area is rendered inside WebView2 and
`HwndHost` terminal panes**, where WPF effects do not composite (the airspace problem — see
[`ai-native-ide-shell`](../ai-native-ide-shell/index.md)). So WPF styling here governs the **frame**
(window chrome, title bar, docking chrome, native side panels, dialogs, command surfaces) while the
**pane interiors** are styled in HTML/CSS or by the terminal renderer. Getting this split right is the
load-bearing decision.

**Compiled:** 2026-08-29 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` carries the concrete API attribute values, licences and version facts to quote
rather than recall.)*

## Headline findings

1. **Windows 11 rounded corners and system drop shadow are "free" from DWM — but customising the window
   turns them off, so you must opt back in.** A normal top-level window gets rounded corners and a shadow
   automatically. The moment you set `WindowStyle="None"` for a custom title bar you lose them, and you
   opt back in with `DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE=33, DWMWCP_ROUND=2)`.
   Corners are a **hint**: if the window uses per-pixel alpha (`AllowsTransparency="True"`) DWM will not
   apply them. — *(Verified, [W1][W24])*
2. **`AllowsTransparency="True"` is the trap.** It is the old way to get a custom-shaped window, and it
   **disables the native shadow and rounded corners** and forces software-composited edges. The modern path
   is `WindowStyle="None"` + `WindowChrome` + **`AllowsTransparency="False"`**, letting DWM keep the shadow
   and corners while WPF draws the caption content. — *(Verified, [W1][W2])*
3. **`WindowChrome` (System.Windows.Shell, in-box) is the first-party custom-title-bar mechanism** — it
   removes the OS caption while preserving resize borders, snap, Aero caption buttons and the maximise/restore
   behaviour, so you draw your own title bar without re-implementing window management. It does **not** draw
   rounded corners (DWM does that); use them together. — *(Verified, [W2][W24])*
4. **WPF on .NET 9/10 ships a built-in Fluent theme with light/dark + accent-colour tracking — no library
   required for a Windows 11 look.** Merge `PresentationFramework.Fluent;component/Themes/Fluent.xaml` (stable)
   or set `ThemeMode="System|Light|Dark"` (experimental in .NET 9, more complete in .NET 10). It re-tints from
   the OS accent colour live. **Mica/Acrylic backdrop is only partial in .NET 9 and is the .NET 10 work item** —
   for Mica *today*, interop or a library is still needed. — *(Verified, [W3][W4][W5][W6])*
5. **The permissive control-library field is healthy and MIT-dominated. The two live, actively-maintained,
   Fluent-first choices are `lepoco/WPF-UI` and the built-in .NET Fluent theme.** WPF UI (MIT) wraps the DWM
   calls behind `FluentWindow` + `WindowBackdropType="Mica"` and is actively released; `Kinnara/ModernWpf`
   (MIT) is the older WinUI-look option but is **winding down** (approaching a 1.0 freeze, feature work
   slowing) — prefer it only for legacy parity. — *(Verified, [W7][W8][W25])*
6. **MahApps.Metro, HandyControl and Material Design In XAML are all MIT and mature, but each imposes a
   look.** MahApps = Metro/flat; HandyControl = broad control suite (80+); Material Design In XAML = Google
   Material (rounded, elevation shadows, ripples) and composes with MahApps. Adopting one is adopting its
   design language wholesale — a dependency and an aesthetic commitment, not a neutral toolkit. — *(Verified, [W9][W10][W11])*
7. **Soft shadows are cheap visually and expensive at scale.** `DropShadowEffect` is GPU-accelerated but a
   high `BlurRadius` on many or animated elements spikes GPU use (reports of 25–60%); the modern-soft recipe
   is `ShadowDepth=0, BlurRadius≈8–24, Opacity≈0.1–0.2`, applied to **few, static** elements, with
   `CacheMode="BitmapCache"` on static shadowed content. Do not put a live `DropShadowEffect` on every card. — *(Verified, [W13][W14][W15])*
8. **A modern rounded TabControl is a re-template, and draggable/tearable tabs are a library.** Rounded tabs
   come from a custom `TabItem` `ControlTemplate` (a `Border` with `CornerRadius="8,8,0,0"`); Chrome-style
   drag-and-tear tabs come from `Dragablz` (MIT) `TabablzControl`, which the AvalonDock ecosystem already
   neighbours. — *(Verified, [W12])*
9. **The current IDE design language is "islands": rounded, elevated, spatially-separated panels on a
   dark-first theme with generous spacing and a restrained accent.** JetBrains shipped the New UI as default
   in 2024.2 and the **Islands theme** (rounded, floating tool windows with subtle elevation) as the new
   default look in 2025.3, on the Inter typeface. This is precisely the "softer, rounded" target the user
   described, and JetBrains publishes an **Int UI Kit (Figma)** and UI guidelines as a reference. — *(Verified, [W16][W17][W18][W19][W20])*
10. **Dark-theme craft has hard rules that a "modern" look must respect.** Never pure-black background
    (use `#121212`–`#1E1E1E` greys), never pure-white text on it (halation), keep body text ≥ 4.5:1 contrast
    (WCAG AA), desaturate accents, and prefer glow/border over drop shadow to signal interaction on dark
    surfaces. These compose with the pack's own `ui-interaction-design.md` (U16 a11y floor) and
    `technical-ui-design.md` (dense-with-hierarchy). — *(Verified, [W21]; a11y floor from the pack's U16)*

## Confidence summary

- **Verified:** the DWM corner/backdrop attributes and the `AllowsTransparency` caveat; `WindowChrome`'s role;
  the .NET 9/10 Fluent-theme + `ThemeMode` surface and the Mica-is-.NET-10 status; the MIT licences of WPF UI,
  ModernWpf, MahApps, HandyControl, Material Design In XAML and Dragablz; `DropShadowEffect` being
  GPU-accelerated with the documented cost pattern; JetBrains New UI (2024.2) and Islands (2025.3) facts; the
  dark-theme craft rules.
- **Inferred:** the exact GPU-percentage figures are a single issue report; the "islands ≈ our target" mapping;
  that Material Design In XAML's elevation model is the closest OOTB match to the user's soft-shadow ask.
- **Flagged (load-bearing):** **exact current library versions** (WPF UI ~4.x, MahApps ~2.4.x, HandyControl
  ~3.5.x — all move; verify before pinning) and **whether the .NET 10 Mica work landed as stable** at the
  time of build (the single fact most likely to have changed since this compile). Re-read [W5] before
  depending on built-in Mica.

## Design implications (what /design should do with this)

- **Split the styling surface explicitly.** WPF chrome (window, title bar, docking tabs, side panels, dialogs,
  command palette) is styled in XAML/Fluent; pane interiors (terminals, web-rendered graph/diagram views) are
  styled in their own runtime. **Do not attempt to drop-shadow or Mica *over* an `HwndHost`/WebView2 pane** —
  it will not composite (airspace). Reserve soft shadows for chrome that sits on the WPF-composited surface.
- **Start with the built-in Fluent theme + DWM, add a library only for a named gap.** The smallest correct
  path (Solution-Selection Ladder): `WindowStyle=None` + `WindowChrome` + `AllowsTransparency=False`
  + `DwmSetWindowAttribute` rounded corners + `Fluent.xaml` + `ThemeMode=System`. Reach for **WPF UI** only
  when you want its `FluentWindow`/Mica wrapper and its control set; reach for **Material Design In XAML** only
  if the team wants an explicitly Material (elevation-shadow, rounded, ripple) language. Each library is a
  supply-chain and aesthetic commitment (Security + Simplifier).
- **Adopt an "islands" information architecture as the visual target.** Rounded, subtly-elevated, clearly-
  separated docking regions on a dark `#1B1B1B`-class canvas, Inter/Segoe UI Variable type, one restrained
  accent (track the OS accent), 8px spacing grid. This is the JetBrains New-UI/Islands register and it maps
  onto AvalonDock's docking model.
- **Budget shadows.** Define one or two shadow tokens (a "resting" and a "raised" elevation), apply them to a
  small number of static chrome surfaces, cache them (`BitmapCache`), and never animate a `DropShadowEffect`.
  On dark surfaces prefer a 1px lighter border + faint glow to a heavy shadow.
- **Re-template `TabItem` for rounded docking tabs; use Dragablz only if tear-out is a requirement.** AvalonDock
  already provides docking tabs; a custom `TabItem` template gives the rounded look. Add `Dragablz` (MIT) only
  when Chrome-style drag-between-windows is an explicit feature.
- **Honour the dark-theme + accessibility floor as a gate, not a nicety.** ≥4.5:1 body contrast, no pure black,
  visible focus rings, not-by-colour-alone — enforced per `ui-interaction-design.md` U16 (the UX & Accessibility
  hard veto) and measured, not asserted.
- **Treat JetBrains' Int UI Kit and UI guidelines as the reference board** for spacing, tokens and component
  states — adapt, never clone (`ui-design-craft.md` DX4/U12); it is a design *language* reference, not assets to
  copy.

## Cross-references (what this base does NOT re-cover)

The user's request also asked for diagramming, UML/generative-from-UML, and data-model/ERM/ORM visualisation.
**Those are already established** and are not duplicated here:
- Diagram paradigms & libraries → [`diagram-generation`](../diagram-generation/index.md) (Mermaid, D2,
  PlantUML, Structurizr, Cytoscape/ELK, layout stability).
- UML, MDE, generative-from-UML, models-over-artifacts → [`uml-mde-and-4gl`](../uml-mde-and-4gl/index.md).
- Data-model / ERM / ORM visualisation → [`domain-modeling-and-erm`](../domain-modeling-and-erm/index.md).
- Service/trace visualisation → [`microservice-interaction-visualization`](../microservice-interaction-visualization/index.md).

The genuinely **new** gap the user identified — *visualising test results, CI/CD execution, and operational
logs/metrics* — is a separate new base:
[`operational-and-test-dashboards`](../operational-and-test-dashboards/index.md).

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The attribute values, licences and
version facts in `references.md` / `data-and-constants.md` are the ones to quote rather than recall. Refresh
when a WPF `.NET` minor lands (the Mica status moves) or a chosen library ships a major version.
