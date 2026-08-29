---
id: kb-wpf-styling-sota
title: "Modern WPF Styling — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [wpf, fluent, dwm, windowchrome, mica]
links:
  - { to: kb-wpf-modern-ui-styling, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Current best practice for a modern, soft WPF look: the DWM + WindowChrome + Fluent-theme
  stack, the Mica situation, soft-shadow technique, and rounded-control re-templating.
---

# State of the art — modern & soft WPF styling

## The first-party stack (smallest correct path)

The modern Microsoft-endorsed way to get a Windows 11 look in WPF, in layers, all in-box on .NET 9/10:

1. **Custom title bar** — `WindowStyle="None"`, `AllowsTransparency="False"`, and a `WindowChrome` with a
   `CaptionHeight` and `ResizeBorderThickness`. WPF draws the caption; the OS keeps resize, snap and Aero
   caption buttons. *(Verified, [W2])*
2. **Rounded corners + native shadow** — call `DwmSetWindowAttribute` with `DWMWA_WINDOW_CORNER_PREFERENCE`
   (33) = `DWMWCP_ROUND` (2) after the HWND exists (`SourceInitialized`). DWM draws rounded corners and the
   drop shadow *outside* the client area, so they cost nothing to render and are correct on multi-monitor and
   high-DPI. *(Verified, [W1][W24])*
3. **Theme** — merge `Fluent.xaml`, or set `Application.ThemeMode="System"`, for Fluent light/dark that tracks
   the OS accent colour live. Bind chrome brushes to Fluent resource keys (`ApplicationBackgroundBrush`, etc.)
   so re-theme is automatic. *(Verified, [W3][W4][W6])*
4. **Mica backdrop** — *partial* in .NET 9; the completion is a .NET 10 work item. Until then, either
   `DwmSetWindowAttribute(DWMWA_SYSTEMBACKDROP_TYPE=38, DWMSBT_MAINWINDOW=2)` by hand, or a library. *(Verified, [W5])*

**Why this is the frontier:** before .NET 9 there was *no* first-party Fluent theme, so every modern look
required a library. The .NET 9/10 Fluent theme + `ThemeMode` collapses the common case into in-box code —
the biggest single change in WPF styling in a decade. *(Verified, [W3])*

## Soft-shadow technique (the "softer" ask, done right)

`DropShadowEffect` is GPU-accelerated (the obsolete `DropShadowBitmapEffect` is not — never use it). The
modern-soft recipe: `ShadowDepth="0"`, `BlurRadius` 8–24, `Opacity` 0.10–0.20, `Color` black (or a faint
brand tint). Apply to **few, static** chrome surfaces, and add `CacheMode="BitmapCache"` so the blurred layer
is rasterised once. High blur radii, many elements, or animation drive GPU use up sharply. On dark surfaces a
1px lighter border plus a faint glow often reads better than a shadow. *(Verified, [W13][W14][W15])*

## Rounded controls (the "rounded corners" ask)

- **Window** — DWM (above), not `CornerRadius` on a root Border (that rounds the *content* but leaves square
  window edges and no shadow).
- **Buttons / cards / inputs** — `Border CornerRadius` in the `ControlTemplate`; Fluent/WPF-UI already ship
  rounded control templates.
- **Tabs** — a custom `TabItem` `ControlTemplate` with `CornerRadius="8,8,0,0"` and an `IsSelected` trigger
  for the selected-tab fill; add `Dragablz` `TabablzControl` (MIT) for Chrome-style draggable/tearable tabs.
  *(Verified, [W12])*

## Leading techniques / recent advances

- **`ThemeMode` per-Window** (.NET 9, experimental) allows different themes per window — useful for a dark
  editor shell with a light dialog. Set at Application level for stability in .NET 9. *(Verified, [W6])*
- **Live accent-colour tracking** — the Fluent theme repaints from the Windows accent colour with no code,
  which is the cheapest way to feel "native and modern". *(Verified, [W6])*
- **WPF Gallery app** (Microsoft Store) is the reference for every Fluent-themed control's appearance. *(Verified, [W4])*

## The frontier / open research

- **Built-in Mica stability on .NET 10** — the one moving fact; verify before depending on it ([W5]).
- **Compositing modern effects over hosted content** — there is no first-party fix for the airspace problem;
  effects over `HwndHost`/WebView2 remain impossible without layered-window hacks (see `ai-native-ide-shell`).
