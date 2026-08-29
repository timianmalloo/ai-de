---
id: kb-wpf-styling-glossary
title: "Modern WPF Styling — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, wpf, dwm, fluent]
links:
  - { to: kb-wpf-modern-ui-styling, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Precise definitions for the WPF-styling vocabulary — WindowChrome, DWM corner preference,
  Mica, Fluent theme, ThemeMode, elevation — so the styling code and its docs agree.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **DWM** | Desktop Window Manager — the Windows compositor. Draws window shadows, rounded corners and backdrops *outside* the WPF render tree, via `DwmSetWindowAttribute`. *(Verified, [W1])* |
| **`WindowChrome`** | In-box WPF class (`System.Windows.Shell`) that removes the OS caption while keeping resize/snap/caption-button behaviour, so the app draws its own title bar. Does **not** round corners. *(Verified, [W2])* |
| **`DWMWA_WINDOW_CORNER_PREFERENCE`** | DWM attribute 33; set to `DWMWCP_ROUND` (2) to opt a customised window back into Windows 11 rounded corners. A hint — ignored under per-pixel alpha. *(Verified, [W1])* |
| **`AllowsTransparency`** | WPF `Window` property. `True` gives a shaped window but **disables DWM shadow and rounded corners** and forces software edges. Keep it `False` for the modern look. *(Verified, [W1])* |
| **Fluent theme** | The built-in WPF theme (.NET 9+) delivering Windows 11 Fluent styling, light/dark, and live accent-colour tracking, via `Fluent.xaml` or `ThemeMode`. *(Verified, [W3][W4])* |
| **`ThemeMode`** | .NET 9+ property on `Application`/`Window` — `System`/`Light`/`Dark`/`None`. Experimental in .NET 9; set at Application level for stability. *(Verified, [W6])* |
| **Mica** | Windows 11 opaque, desktop-tinted material backdrop (`DWMSBT_MAINWINDOW`, attribute 38 = 2). Partial in WPF .NET 9; completion is a .NET 10 item. *(Verified, [W5])* |
| **Acrylic** | Translucent blur backdrop (`DWMSBT_TRANSIENTWINDOW`), for transient surfaces (flyouts). *(Verified, [W1])* |
| **`DropShadowEffect`** | GPU-accelerated soft-shadow effect. Modern-soft = `ShadowDepth 0, BlurRadius 8–24, Opacity .1–.2`; cache static ones, never animate en masse. *(Verified, [W13])* |
| **Elevation** | A design-token concept (from Material/Fluent): a small set of named shadow levels ("resting", "raised") applied consistently, rather than ad-hoc shadows per element. *(Verified, [W11])* |
| **Islands** | JetBrains' 2025.3 default look: rounded, subtly-elevated, spatially-separated tool windows on a dark-first theme — the reference target for "softer, rounded". *(Verified, [W18])* |
| **Airspace problem** | The WPF limitation that content hosted via `HwndHost`/WebView2 renders above the WPF visual tree, so WPF effects (shadows, Mica) do not composite over it. Defined in `ai-native-ide-shell`. *(Verified, cross-ref)* |
