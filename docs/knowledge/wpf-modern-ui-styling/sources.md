---
id: kb-wpf-styling-sources
title: "Modern WPF Styling — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-wpf-modern-ui-styling, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The full access-dated source list behind the WPF-modern-ui-styling base, keyed [W1]..[W25]
  as cited throughout the topic.
---

# Sources

All accessed **2026-08-29**. Citation keys `[Wn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| W1 | Apply rounded corners in desktop apps for Windows 11 | primary (official) | https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-rounded-corners | DWM corner/backdrop attributes; AllowsTransparency caveat |
| W2 | WindowChrome Class (System.Windows.Shell) | primary (official) | https://learn.microsoft.com/en-us/dotnet/api/system.windows.shell.windowchrome | Custom title bar mechanism |
| W3 | What's new in WPF for .NET 9 | primary (official) | https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net90 | Built-in Fluent theme, ThemeMode |
| W4 | dotnet/wpf using-fluent.md | primary (repo docs) | https://github.com/dotnet/wpf/blob/main/Documentation/docs/using-fluent.md | Fluent.xaml merge, resource keys |
| W5 | Fluent Theme in .NET 10 Plan (Discussion #10387) | primary (repo) | https://github.com/dotnet/wpf/discussions/10387 | Mica/backdrop is a .NET 10 work item |
| W6 | Thomas Claudius Huber — WPF in .NET 9 Windows 11 Theming | secondary (practitioner) | https://www.thomasclaudiushuber.com/2025/02/21/wpf-in-net-9-0-windows-11-theming/ | ThemeMode walkthrough, accent tracking |
| W7 | lepoco/wpfui (WPF UI) | primary (repo) | https://github.com/lepoco/wpfui | MIT, FluentWindow, actively maintained |
| W8 | Kinnara/ModernWpf releases | primary (repo) | https://github.com/Kinnara/ModernWpf | MIT, winding down toward 1.0 |
| W9 | MahApps.Metro | primary (repo/docs) | https://github.com/MahApps/MahApps.Metro | MIT, Metro theme, MetroWindow |
| W10 | HandyControl | primary (repo) | https://github.com/HandyOrg/HandyControl | MIT, 80+ controls |
| W11 | Material Design In XAML Toolkit | primary (repo) | https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit | MIT, elevation shadows, ripple, ShadowAssist |
| W12 | ButchersBoy/Dragablz | primary (repo) | https://github.com/ButchersBoy/Dragablz | MIT, TabablzControl draggable/tearable tabs |
| W13 | DropShadowEffect Class | primary (official) | https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.effects.dropshadoweffect | GPU-accelerated effect, properties |
| W14 | dotnet/wpf issue #9300 — DropShadowEffect GPU | primary (repo issue) | https://github.com/dotnet/wpf/issues/9300 | GPU cost of high-blur/animated shadows |
| W15 | Material Design In XAML — performance / ShadowAssist | secondary | https://deepwiki.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/4.2-performance-considerations | CacheMode / BitmapCache shadow caching |
| W16 | JetBrains Rider — New UI docs | primary (vendor docs) | https://www.jetbrains.com/help/rider/New_UI.html | Rounded panels, tool-window elevation, compact mode |
| W17 | JetBrains — New UI default in 2024.2 | primary (vendor blog) | https://blog.jetbrains.com/blog/2024/07/08/the-new-ui-becomes-the-default-in-2024-2/ | New UI direction; density backlash context |
| W18 | JetBrains — Meet the Islands theme | primary (vendor blog) | https://blog.jetbrains.com/platform/2025/12/meet-the-islands-theme-the-new-default-look-for-jetbrains-ides/ | Rounded elevated islands, default 2025.3 |
| W19 | JetBrains Int UI Kit | primary (vendor docs) | https://plugins.jetbrains.com/docs/intellij/ui-kit.html | Figma UI kit reference |
| W20 | JetBrains UI Guidelines | primary (vendor docs) | https://plugins.jetbrains.com/docs/intellij/ui-guidelines-welcome.html | Spacing/typography/interaction rules |
| W21 | Dark Mode UI best practices | secondary | https://www.designstudiouiux.com/blog/dark-mode-ui-design-best-practices/ | No pure black/white, desaturated accents, contrast |
| W22 | Premiere vs DaVinci Resolve UI comparison | secondary | https://www.vidio.ai/blog/article/premiere-pro-vs-davinci-resolve-professional-video-editing | Creative-tool dark UI, page vs panel layouts |
| W23 | awesome-wpf | secondary (curated list) | https://github.com/Carlos487/awesome-wpf | Index of WPF libraries + licences |
| W24 | DinoChan/WindowChromeApplyRoundedCorners | primary (repo demo) | https://github.com/DinoChan/WindowChromeApplyRoundedCorners | Working WindowChrome + rounded-corners demo |
| W25 | WPF UI — Window Backdrop (DeepWiki) | secondary | https://deepwiki.com/lepoco/wpfui/5.3-window-backdrop | FluentWindow WindowBackdropType=Mica |
| W-reddit | r/csharp — "modern WPF without a UI library?" | secondary (forum) | https://www.reddit.com/r/csharp/comments/xhgxr1/is_this_possible_to_make_without_any_ui_library/ | The disconfirming "no library needed" view |

## Source-quality notes

- **Licences** for WPF UI, ModernWpf, MahApps, HandyControl, Material Design In XAML and Dragablz are MIT and
  are cited to each project's own repository (`LICENSE`/README). This session confirmed them from the projects'
  own repos and corroborating aggregators but did **not** re-fetch each `LICENSE` file individually — a
  five-minute follow-up before committing a dependency; treat the licence claims as Verified-pending-that-check.
- **Version numbers** in `data-and-constants.md` are approximate (this ecosystem releases monthly) and are
  marked **Flagged**; read NuGet at pin time.
- **The .NET 10 Mica status** ([W5]) is the fastest-moving fact and the one to re-read before any Mica-dependent
  design.
- The GPU-percentage figures (25–60%) come from a **single** issue report ([W14]) and are Inferred as
  order-of-magnitude, not a benchmark.
