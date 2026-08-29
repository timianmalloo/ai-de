---
id: kb-content-rendering-sota
title: "Editor & Content Rendering Surfaces — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [monaco, avalonedit, roslynpad, markdig, webview2]
links:
  - { to: kb-editor-and-content-rendering-surfaces, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Current best practice for viewing code and rendering markdown/HTML in a WPF desktop app — the
  native (AvalonEdit/RoslynPad/Markdig.Wpf) vs web (Monaco/WebView2) options and when each wins.
---

# State of the art — editor & content rendering surfaces

## Code viewing

- **Monaco Editor** (MIT) — the VS Code editor as an embeddable web component: best-in-class multi-language
  highlighting, folding, minimap, find/replace, read-only mode, and a first-class **diff editor**. In WPF it
  runs in **WebView2** with a JS↔.NET bridge; wrappers (**WPFMonaco**, **Monaco.Editor.WebView**) exist, or roll
  a thin wrapper over the official Monaco build. Best for a *language-agnostic* viewer. *(Verified, [ED1][ED2][ED8])*
- **AvalonEdit** (MIT, ICSharpCode) — pure managed WPF editor (SharpDevelop/ILSpy heritage): highlighting,
  folding, line numbers, gutter decorations. **Native compositing** (no airspace), trivial WPF theming, light.
  Less rich/modern than Monaco. Best for a native, chrome-integrated surface. *(Verified, [ED3])*
- **RoslynPad** (MIT) — AvalonEdit + **Roslyn**: C#/VB completion, diagnostics, semantic highlighting, live
  execution. Best for **C#** nodes, reusing the Roslyn the daemon already runs. *(Verified, [ED4])*

## Markdown / HTML rendering

- **Markdig** (BSD-2-Clause) — the fast, CommonMark-compliant .NET markdown parser; the de-facto engine. *(Verified, [ED5])*
- **Markdig.Wpf** (MIT) — renders Markdig output to a native WPF **FlowDocument**/control. No browser, themes
  with WPF resources. **Does not run JavaScript** — so no Mermaid/interactive content. Best for plain knowledge
  docs (specs, ADRs, decision notes). *(Verified, [ED6])*
- **Markdown → HTML → WebView2** — for rich content (Mermaid diagrams, tables, highlighted code blocks, HTML
  reports), render to HTML and show in WebView2 (the same pane as diagrams/graph). Web fidelity + JS; airspace
  applies. *(Verified, [ED5][ED7])*

## The native-vs-web decision (the core trade)

| Dimension | Native (AvalonEdit/RoslynPad/Markdig.Wpf) | Web (Monaco / WebView2 HTML) |
|---|---|---|
| Compositing | **Native** — WPF shadows/Mica over it | **Airspace** — no WPF effects over it |
| Fidelity/breadth | Good; C#-strong (RoslynPad) | **VS Code parity**, all languages, JS |
| Interactivity (Mermaid, charts) | No (FlowDocument) | **Yes** |
| Theming | WPF resources | CSS variables mirroring tokens |
| Cost | Light, in-process | One shared WebView2 environment |
| Best for | read-heavy, chrome-integrated, C# | breadth, interactivity, rich reports |

## Reuse from existing IDEs (permissive only)

- **Monaco** is genuinely reusable and MIT — it *is* the VS Code editor extracted. *(Verified, [ED1])*
- **VS Code the product** ≠ the MIT `microsoft/vscode` source: the branded build + marketplace are proprietary
  (hence VSCodium). Reuse means **Monaco**, not the app. *(Verified/Inferred)*
- **JetBrains / Eclipse editors** are JVM/SWT and (JetBrains) proprietary — **not cleanly embeddable** in a WPF
  app. Study their UX, don't embed their components. *(Inferred.)*

## The frontier / what's moving

- **Monaco wrapper liveness** — verify which WPF wrapper is maintained before depending on it.
- **AI-native review surfaces** — inline agent annotations, "explain this range", diff-with-rationale; Monaco's
  decoration API is the substrate.
