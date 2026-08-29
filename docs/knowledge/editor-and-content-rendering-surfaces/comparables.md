---
id: kb-content-rendering-comparables
title: "Editor & Content Rendering Surfaces — comparables & libraries"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [monaco, avalonedit, roslynpad, markdig, libraries, licences]
links:
  - { to: kb-editor-and-content-rendering-surfaces, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Named code-viewing and markdown/HTML-rendering libraries for WPF, with licence, role and fit for
  the node-introspection panes.
---

# Comparable solutions & libraries

## Code-viewing controls

| Library | Licence | Native/Web | Languages | Role / fit | Confidence |
|---|---|---|---|---|---|
| **Monaco Editor** | MIT | Web (WebView2) | Many (VS Code parity) | Language-agnostic viewer; diff editor; decorations | Verified [ED1] |
| **WPFMonaco** | MIT | Web wrapper | via Monaco | Ready WPF wrapper over Monaco+WebView2 | Verified [ED2] |
| **Monaco.Editor.WebView** (Guyiming) | MIT | Web wrapper | via Monaco | Alt WPF wrapper | Verified [ED8] |
| **AvalonEdit** | MIT | **Native** | Many (TextMate-ish highlighting) | Native, no-airspace, light viewer | Verified [ED3] |
| **RoslynPad** (editor components) | MIT | **Native** | C#/VB (Roslyn) | Best native surface for **C#** nodes | Verified [ED4] |

## Markdown / HTML rendering

| Library | Licence | Output | Runs JS? | Role / fit | Confidence |
|---|---|---|---|---|---|
| **Markdig** | **BSD-2-Clause** | AST/HTML | — | The .NET markdown engine (parse) | Verified [ED5] |
| **Markdig.Wpf** | MIT | WPF FlowDocument | No | Native render of plain knowledge docs | Verified [ED6] |
| **Markdown → HTML → WebView2** | (Markdig BSD-2 + WebView2 free) | HTML in browser | **Yes** | Rich content: Mermaid, charts, reports | Verified [ED5][ED7] |
| **WebView2** | Proprietary, free | — | Yes | The web host; airspace applies | Verified [ED7] |

## Reuse-from-IDE assessment (permissive constraint)

| Source | Embeddable in WPF? | Licence | Verdict |
|---|---|---|---|
| **Monaco** (from VS Code) | Yes (WebView2) | MIT | **Use it** — the editor, extracted |
| **VS Code app / marketplace** | No | Proprietary build over MIT source | Study, don't embed (VSCodium caveat) |
| **JetBrains editor** | No (JVM, proprietary) | Proprietary | Study UX only |
| **Eclipse (SWT/JFace) editor** | No (SWT/JVM) | EPL | Study only |
| **RoslynPad / AvalonEdit** | Yes (native WPF) | MIT | **Use for C#/native** |

## Adjacent problems worth borrowing from

- **The shell's WebView2 discipline** — `ai-native-ide-shell` already solved the one-environment / virtual-host /
  airspace model that every web-rendered pane here inherits.
- **The token system** — `wpf-modern-ui-styling` supplies the dark-first token scale both the native controls
  (WPF resources) and the web panes (CSS variables) theme to, so the app looks unified.
- **VS Code / JetBrains / Zed as UX references** — their review surfaces (inline blame, diff gutters, folding,
  peek) are the interaction patterns to reproduce, even where their code cannot be embedded.
