---
id: kb-content-rendering-data
title: "Editor & Content Rendering Surfaces — data, constants & decision matrix"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [decision-matrix, licences, monaco, avalonedit, markdig]
links:
  - { to: kb-editor-and-content-rendering-surfaces, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The licence facts and the per-node-type renderer decision matrix for the introspection panes.
---

# Domain data, constants & decision matrix

## Renderer decision matrix (route by node type)

| Node type | Content | Default renderer | Escalate to | Compositing |
|---|---|---|---|---|
| **C# file/symbol** | code | **RoslynPad/AvalonEdit** (native) *or* read-only Monaco | Monaco diff editor for review | native (or airspace if Monaco) |
| **Other-language code** | code | **Monaco** (read-only, WebView2) | Monaco diff | airspace |
| **Knowledge (spec/ADR/note)** | plain markdown | **Markdig.Wpf** (native FlowDocument) | markdown→HTML in WebView2 (if Mermaid/HTML) | native |
| **Rich knowledge / report** | markdown+Mermaid/HTML | **WebView2 HTML** | — | airspace |
| **Diagram** | Mermaid/DSL | **WebView2** (shared with graph pane) | — | airspace |
| **Graph** | force-graph | **WebView2** (Sigma/3d-force-graph) | — | airspace |

**Rule:** read-heavy + chrome-integrated → native; interactive/high-fidelity → web; **all web panes share one
`CoreWebView2Environment` + origin** (`ai-native-ide-shell`).

## Licence facts (verify versions before pinning)

| Component | Licence | Note |
|---|---|---|
| Monaco Editor | **MIT** | VS Code editor; WebView2 host *(Flagged: version)* |
| WPFMonaco / Monaco.Editor.WebView | **MIT** | wrappers *(Flagged: which is best-maintained)* |
| AvalonEdit | **MIT** | native; ICSharpCode *(Flagged: version)* |
| RoslynPad | **MIT** | AvalonEdit + Roslyn *(Flagged: version)* |
| Markdig | **BSD-2-Clause** | markdown engine *(Flagged: version)* |
| Markdig.Wpf | **MIT** | FlowDocument render *(Flagged: version)* |
| WebView2 | **Proprietary, free** | host only, not vendorable |

## Review-surface capabilities (what the node-walk needs when landing on code)

- **Read-only mode** — both Monaco and AvalonEdit. *(Verified, [ED1][ED3])*
- **Folding** — both. **Gutter/margin decorations** (highlight range, annotate line) — both. *(Verified.)*
- **Diff view** — Monaco has a first-class diff editor; AvalonEdit needs a diff add-on. *(Verified, [ED1].)*
- **Go-to / peek** — Monaco (rich); AvalonEdit (basic, extensible). *(Verified.)*
- **Semantic C# (completion, diagnostics)** — RoslynPad (Roslyn). *(Verified, [ED4].)*

## Theming tokens (shared with wpf-modern-ui-styling)

- Native controls (AvalonEdit/Markdig.Wpf): bind to WPF resource brushes (`ApplicationBackgroundBrush`, etc.).
- Web panes (Monaco/HTML): a CSS variable set mirroring the token scale — dark canvas `#1B1B1B`, surface
  `#232323`, text `#E6E6E6`, accent = OS accent, Inter/Segoe UI Variable, 8px spacing, radius 6–8 — so native
  and web read as one app. *(Inferred synthesis; cross-ref wpf-modern-ui-styling data-and-constants.)*
