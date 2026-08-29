---
id: kb-content-rendering-sources
title: "Editor & Content Rendering Surfaces — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-editor-and-content-rendering-surfaces, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The full access-dated source list behind the editor-and-content-rendering-surfaces base, keyed
  [ED1]..[ED8] as cited throughout the topic.
---

# Sources

All accessed **2026-08-29**. Citation keys `[EDn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| ED1 | Monaco Editor | primary (official) | https://microsoft.github.io/monaco-editor/ | MIT; VS Code editor; read-only, folding, decorations, diff |
| ED2 | WPFMonaco | primary (repo) | https://github.com/IceSkyDev/WPFMonaco | MIT WPF wrapper (Monaco + WebView2) |
| ED3 | AvalonEdit | primary (repo) | https://github.com/icsharpcode/AvalonEdit | MIT; native managed WPF editor |
| ED4 | RoslynPad | primary (repo) | https://github.com/roslynpad/roslynpad | MIT; AvalonEdit + Roslyn |
| ED5 | xoofx/markdig | primary (repo) | https://github.com/xoofx/markdig | BSD-2-Clause; CommonMark .NET engine |
| ED6 | Kryptos-FR/markdig.wpf | primary (repo) | https://github.com/Kryptos-FR/markdig.wpf | MIT; WPF FlowDocument render |
| ED7 | WebView2 documentation | primary (official) | https://learn.microsoft.com/en-us/microsoft-edge/webview2/ | Proprietary/free; HTML host; airspace |
| ED8 | Guyiming/Monaco.Editor.WebView | primary (repo) | https://github.com/Guyiming/Monaco.Editor.WebView | MIT WPF Monaco wrapper |

## Source-quality notes

- **Licences** (Monaco MIT, AvalonEdit MIT, RoslynPad MIT, Markdig BSD-2-Clause, Markdig.Wpf MIT, the Monaco
  wrappers MIT) are cited to each project's own repo/site and are corroborated across the ecosystem; individual
  `LICENSE` files were **not** re-fetched this session — a five-minute check before adopting any as a
  dependency. Treat as Verified-pending-that-check.
- **WebView2** being proprietary-but-free-to-redistribute is from Microsoft's own docs and the widely-cited
  Stack Overflow answer; the underlying Chromium is BSD-style but the WebView2 glue is not open.
- **The `microsoft/vscode` source is MIT but the branded product build + marketplace are proprietary** (the
  reason VSCodium exists) — so "reuse VS Code" is scoped to **Monaco**. This is well-established ecosystem fact,
  Inferred here for the JetBrains/Eclipse non-embeddability (JVM/SWT runtimes).
- The **native-vs-web / airspace** trade-offs draw on the pack's own `ai-native-ide-shell` base rather than an
  external source.
