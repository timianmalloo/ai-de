---
id: kb-wpf-styling-references
title: "Modern WPF Styling — references"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [wpf, references, apis, standards]
links:
  - { to: kb-wpf-modern-ui-styling, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The authoritative API surfaces, standards and reference kits behind modern WPF styling — the
  ones to quote rather than recall.
---

# Reference information

## First-party APIs & specs

- **`WindowChrome`** (`System.Windows.Shell`) — custom-chrome mechanism; properties `CaptionHeight`,
  `ResizeBorderThickness`, `GlassFrameThickness`, `CornerRadius`, `UseAeroCaptionButtons`,
  `NonClientFrameEdges`. In-box; no package. *(Verified, [W2])*
- **`DwmSetWindowAttribute`** (`dwmapi.dll`) — the DWM opt-in surface. Attributes used here:
  `DWMWA_WINDOW_CORNER_PREFERENCE = 33` (values `DWMWCP_DEFAULT 0`, `DONOTROUND 1`, `ROUND 2`, `ROUNDSMALL 3`);
  `DWMWA_SYSTEMBACKDROP_TYPE = 38` (values `AUTO 0`, `NONE 1`, `MAINWINDOW 2`=Mica, `TRANSIENTWINDOW 3`=Acrylic,
  `TABBEDWINDOW 4`); `DWMWA_USE_IMMERSIVE_DARK_MODE = 20`. *(Verified, [W1])*
- **WPF Fluent theme** — `pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml`;
  `Application.ThemeMode` / `Window.ThemeMode` (`System | Light | Dark | None`). .NET 9+ ; more complete .NET 10.
  *(Verified, [W3][W4])*
- **`DropShadowEffect`** (`System.Windows.Media.Effects`) — `BlurRadius`, `ShadowDepth`, `Direction`, `Color`,
  `Opacity`, `RenderingBias`. GPU-accelerated. Pair with `UIElement.CacheMode="BitmapCache"`. *(Verified, [W13])*
- **`Dragablz.TabablzControl`** + `InterTabController` — draggable/tearable tabs. *(Verified, [W12])*

## Standards & guidelines

- **Windows 11 Fluent Design / "Apply rounded corners in desktop apps"** — Microsoft's own guidance on the
  DWM corner opt-in and the `AllowsTransparency` caveat. *(Verified, [W1])*
- **WCAG 2.2 AA** — contrast floor (≥4.5:1 body / ≥3:1 large & UI), focus visibility, not-by-colour-alone.
  The pack's `ui-interaction-design.md` U16 is the enforcing gate. *(Verified, pack standard)*
- **JetBrains Int UI Kit (Figma) + UI Guidelines** — the reference token/spacing/component-state system for
  the "New UI / Islands" look. *(Verified, [W19][W20])*

## Seminal / foundational

- **JetBrains "New UI becomes default" (2024.2)** and **"Islands theme"** posts — the primary account of the
  rounded-elevated-islands direction the user is describing. *(Verified, [W17][W18])*
- **Thomas Claudius Huber, "WPF in .NET 9 — Windows 11 Theming"** — the clearest practitioner walkthrough of
  the Fluent-theme + `ThemeMode` surface. *(Verified, [W6])*
- **`dotnet/wpf` Discussion #10387 — Fluent Theme in .NET 10 Plan** — the authoritative status of Mica/backdrop
  work. *(Verified, [W5])*
