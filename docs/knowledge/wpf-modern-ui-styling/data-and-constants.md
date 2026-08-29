---
id: kb-wpf-styling-data
title: "Modern WPF Styling — data, constants & recipes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [wpf, constants, recipes, tokens]
links:
  - { to: kb-wpf-modern-ui-styling, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Concrete attribute values, licence/version facts, soft-shadow/rounded recipes and a starter
  token scale for a modern-soft WPF look.
---

# Domain data, constants & recipes

## DWM attribute values (quote these)

| Constant | Value | Meaning |
|---|---|---|
| `DWMWA_USE_IMMERSIVE_DARK_MODE` | 20 | dark title-bar buttons |
| `DWMWA_WINDOW_CORNER_PREFERENCE` | 33 | rounded-corner opt-in |
| `DWMWCP_DEFAULT / DONOTROUND / ROUND / ROUNDSMALL` | 0 / 1 / 2 / 3 | corner style |
| `DWMWA_SYSTEMBACKDROP_TYPE` | 38 | backdrop opt-in |
| `DWMSBT_NONE / MAINWINDOW(Mica) / TRANSIENT(Acrylic) / TABBED` | 1 / 2 / 3 / 4 | backdrop type |

*(Verified, [W1])* — corners are a **hint**; ignored when `AllowsTransparency="True"`.

## The custom-title-bar recipe (retains DWM shadow + corners)

```xml
<Window WindowStyle="None" AllowsTransparency="False" ThemeMode="System">
  <WindowChrome.WindowChrome>
    <WindowChrome CaptionHeight="36" ResizeBorderThickness="6"
                  GlassFrameThickness="0" UseAeroCaptionButtons="False"/>
  </WindowChrome.WindowChrome>
  <!-- draw your own title bar in the top 36px; set WindowChrome.IsHitTestVisibleInChrome=True
       on interactive title-bar controls -->
</Window>
```
Then, in `OnSourceInitialized`: `DwmSetWindowAttribute(hWnd, 33, ref two, 4)` where `two = 2` (round).
*(Verified, [W2][W24])*

## Soft-shadow recipe (modern, subtle)

```xml
<Border CornerRadius="8" Background="{DynamicResource CardBackgroundBrush}"
        CacheMode="BitmapCache">
  <Border.Effect>
    <DropShadowEffect ShadowDepth="0" BlurRadius="16" Opacity="0.14" Color="Black"/>
  </Border.Effect>
</Border>
```
- Range: `BlurRadius` 8–24, `Opacity` 0.10–0.20, `ShadowDepth` 0 (even) or 1–2 (lifted).
- **Few and static.** Cache. Never animate. On dark canvases prefer border+glow. *(Verified, [W13][W14][W15])*

## Rounded-tab recipe

`TabItem` `ControlTemplate` → `Border CornerRadius="8,8,0,0"` with an `IsSelected` trigger swapping the fill;
apply via `ItemContainerStyle`. Wrap in `Dragablz.TabablzControl` for drag/tear. *(Verified, [W12])*

## Licence & version facts (verify versions before pinning — they move)

| Package | Licence | Approx current | Note |
|---|---|---|---|
| WPF UI (`WPF-UI` / `lepoco`) | **MIT** | ~4.x | actively maintained; `FluentWindow`, Mica wrapper *(Flagged: exact version)* |
| ModernWpf (`ModernWpfUI`) | **MIT** | ~1.0-preview | winding down *(Verified licence; Flagged version)* |
| MahApps.Metro | **MIT** | ~2.4.x | *(Flagged version)* |
| HandyControl | **MIT** | ~3.5.x | *(Flagged version)* |
| MaterialDesignThemes | **MIT** | ~5.x | elevation shadows / ripple *(Flagged version)* |
| Dragablz | **MIT** | ~0.0.3.x | draggable tabs *(Flagged version)* |
| AvalonDock (`Dirkster.AvalonDock`) | **MIT** | v5.x | already selected (`ai-native-ide-shell`) |

## Starter token scale (an "islands" dark-first system)

- **Radius:** `card 8`, `control 6`, `pill 999`. **Spacing:** 4/8/12/16/24 (8px grid).
- **Elevation:** `resting` blur 8 / op .10; `raised` blur 16 / op .16.
- **Colour (dark):** canvas `#1B1B1B`, surface `#232323`, surface-raised `#2A2A2A`, border `#3A3A3A`,
  text `#E6E6E6` (not pure white), muted `#A0A0A0`, accent = OS accent (tracked). *(Inferred synthesis from
  [W18][W21]; validate contrast with the U16 gate.)*
- **Type:** Segoe UI Variable (native) or Inter (JetBrains parity); one display/body pairing, ≤4 sizes.
