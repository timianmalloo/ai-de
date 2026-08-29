---
id: kb-content-rendering-references
title: "Editor & Content Rendering Surfaces — references"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [monaco, avalonedit, roslynpad, markdig, references]
links:
  - { to: kb-editor-and-content-rendering-surfaces, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The authoritative repos, docs and licence facts behind the code-viewing and markdown/HTML
  rendering options — the ones to quote rather than recall.
---

# Reference information

## Code editors (primary)

- **Monaco Editor** — https://microsoft.github.io/monaco-editor/ — MIT; the VS Code editor; APIs for read-only,
  folding, decorations, diff. *(Verified, [ED1])*
- **WPFMonaco** — https://github.com/IceSkyDev/WPFMonaco — MIT WPF wrapper (Monaco + WebView2). *(Verified, [ED2])*
- **Monaco.Editor.WebView** — https://github.com/Guyiming/Monaco.Editor.WebView — MIT WPF wrapper. *(Verified, [ED8])*
- **AvalonEdit** — https://github.com/icsharpcode/AvalonEdit — MIT; native WPF editor (SharpDevelop/ILSpy). *(Verified, [ED3])*
- **RoslynPad** — https://github.com/roslynpad/roslynpad — MIT; AvalonEdit + Roslyn. *(Verified, [ED4])*

## Markdown / HTML (primary)

- **Markdig** — https://github.com/xoofx/markdig — **BSD-2-Clause**; CommonMark-compliant .NET engine. *(Verified, [ED5])*
- **Markdig.Wpf** — https://github.com/Kryptos-FR/markdig.wpf — MIT; renders to WPF FlowDocument. *(Verified, [ED6])*
- **WebView2** — https://learn.microsoft.com/en-us/microsoft-edge/webview2/ — proprietary, free; the HTML host. *(Verified, [ED7])*

## Licence summary (the permissive-constraint check)

| Component | Licence | Open? |
|---|---|---|
| Monaco, WPFMonaco, Monaco.Editor.WebView | MIT | Yes |
| AvalonEdit, RoslynPad | MIT | Yes |
| Markdig | BSD-2-Clause | Yes |
| Markdig.Wpf | MIT | Yes |
| WebView2 | Proprietary (free redistribution) | No (host only) |
| `microsoft/vscode` source | MIT | Yes (but product build/marketplace proprietary) |

## Standards / cross-refs

- **CommonMark** — the markdown spec Markdig implements. *(Verified, [ED5])*
- **WPF FlowDocument** — the native rich-text model Markdig.Wpf targets. *(Verified, [ED6])*
- **`ai-native-ide-shell`** — the WebView2 content model (one environment, virtual host, airspace) every web
  pane inherits. *(Verified, pack)*
- **`wpf-modern-ui-styling`** — the token system both native (WPF resources) and web (CSS variables) panes theme
  to. *(Verified, pack)*
