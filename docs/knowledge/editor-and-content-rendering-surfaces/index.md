---
id: kb-editor-and-content-rendering-surfaces
title: "Editor & Content Rendering Surfaces — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [monaco, avalonedit, roslynpad, markdig, webview2, code-viewing, markdown, html, wpf]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: kb-ai-native-ide-shell, rel: relates-to }
  - { to: kb-graph-experience-and-visualization, rel: relates-to }
  - { to: kb-wpf-modern-ui-styling, rel: relates-to }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Evidence base for the AI-DE content-rendering panes: viewing/reviewing code (Monaco via WebView2,
  AvalonEdit, RoslynPad — all MIT) and rendering markdown/HTML (Markdig + Markdig.Wpf, or WebView2),
  with the permissive-licence facts and the native-vs-web trade-off for each surface.
---

# Editor & Content Rendering Surfaces — domain knowledge

**Domain & problem:** When the graph node-walk (see
[`graph-experience-and-visualization`](../graph-experience-and-visualization/index.md)) lands on a node, the
introspection panel must **render its content**: a C# file as syntax-highlighted, navigable code; a spec/ADR/
decision-note as formatted **markdown**; a generated report or diagram as **HTML**. AI-DE is a *terminal host
with derived visual panes and no editor* (`ai-native-ide-shell`), so the requirement is **viewing/review**
surfaces — read code, read knowledge — not a full editing IDE. This base gathers the permissively-licensed
options and their trade-offs.

**Canonical framing:** The field frames "show code in a desktop app" two ways: **native managed control**
(AvalonEdit, RoslynPad — pure WPF, no browser) or **web editor embedded** (Monaco — the actual VS Code editor,
run in WebView2). And "show markdown/HTML in WPF" the same two ways: **native render** (Markdig → WPF
FlowDocument via Markdig.Wpf) or **web render** (Markdig → HTML → WebView2). Our framing narrows it: the panes
are *read-first*, they must **match the modern chrome** (`wpf-modern-ui-styling`), and they must **compose with
the WebView2 panes the shell already hosts** — which tilts several choices toward the web path we already pay
for, while keeping the native path for the airspace-sensitive cases.

**Compiled:** 2026-08-29 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` carries the licence facts and the decision matrix to quote rather than recall.)*

## Headline findings

1. **Every serious option is MIT — the permissive-licence constraint is fully satisfied.** Monaco Editor (MIT),
   AvalonEdit (MIT, ICSharpCode), RoslynPad (MIT, built on AvalonEdit + Roslyn), and the Monaco WPF wrappers
   (WPFMonaco, Monaco.Editor.WebView — MIT). Markdig core is **BSD-2-Clause** and Markdig.Wpf is **MIT** — both
   maximally permissive. **The one non-open piece is WebView2 itself** (proprietary, free to redistribute), and
   the shell already depends on it. — *(Verified, [ED1][ED3][ED4][ED5][ED6][ED8])*
2. **Monaco is the VS Code editor and the richest code surface, at the cost of the WebView2 bridge.** It is the
   *same core editor* as VS Code — best-in-class multi-language syntax highlighting, folding, minimap, find,
   read-only mode — but it runs as HTML/JS in a browser, so in WPF it needs WebView2 and a JS↔.NET interop
   layer. For a *language-agnostic* viewer this is the top option, and it reuses the shell's WebView2. — *(Verified, [ED1][ED2])*
3. **AvalonEdit is the native, no-browser, lightweight code surface — no airspace, easy MVVM, less rich.** Pure
   managed WPF (the SharpDevelop/ILSpy editor): syntax highlighting, folding, line numbers. It composites
   natively with WPF chrome (no airspace problem, unlike a WebView2/HwndHost pane), is trivial to theme with the
   `wpf-modern-ui-styling` tokens, but is less "modern-feeling" and weaker on breadth than Monaco. — *(Verified, [ED3])*
4. **RoslynPad is AvalonEdit + Roslyn — the right choice specifically for C# with live analysis.** It adds
   Roslyn-powered C# completion, diagnostics and semantic highlighting on top of AvalonEdit. Since the code
   graph is C#-first and the daemon already runs Roslyn (`code-and-infra-extraction`), RoslynPad's components
   are the natural native surface for *C#* nodes, with AvalonEdit/Monaco for other languages. — *(Verified, [ED4])*
5. **Markdig + Markdig.Wpf renders markdown natively to a WPF FlowDocument — no browser, fully permissive.**
   Markdig is the fast, CommonMark-compliant .NET markdown engine (BSD-2); Markdig.Wpf (MIT) renders it to a
   native WPF `FlowDocument`/control. This is the lightest, most native way to show the knowledge nodes (specs,
   ADRs, decision notes) and it themes with WPF resources. — *(Verified, [ED5][ED6])*
6. **For rich/interactive knowledge content (Mermaid diagrams, tables, syntax-highlighted code blocks, HTML
   reports), render markdown→HTML and show it in WebView2.** Markdig.Wpf's native FlowDocument does not run
   JavaScript, so **Mermaid, interactive charts, and web-report fidelity require the HTML path**. This is the
   same pane that hosts the diagram and graph views — so the knowledge renderer and the diagram renderer can be
   the *same WebView2 surface* with different content. — *(Verified, [ED5][ED7]; the consolidation Inferred)*
7. **The native-vs-web split is an airspace and fidelity trade, and the shell's constraints already decide much
   of it.** Native (AvalonEdit/RoslynPad/Markdig.Wpf) composites with WPF, so it works in floating/docked panels
   with shadows and Mica *over* it; web (Monaco/WebView2 HTML) does **not** composite (airspace,
   `ai-native-ide-shell`) but gives VS Code fidelity and JS interactivity. **Read-heavy, chrome-integrated panes
   → native; fidelity/interactivity-heavy panes → web.** — *(Verified, cross-ref ai-native-ide-shell + wpf-modern-ui-styling)*
8. **Reusing VS Code / JetBrains / Eclipse "editor components" is only clean for Monaco.** Monaco is genuinely
   extractable and MIT. VS Code *as a whole* is under the MIT-licensed `microsoft/vscode` **but the Microsoft-
   branded product build and marketplace are not** (VSCodium exists precisely because of this) — so "reuse the
   editor" means Monaco, not the VS Code app. JetBrains' editor and Eclipse's are **not** cleanly embeddable in
   a WPF app (JVM/SWT, different runtimes; JetBrains' is proprietary). The realistic public/permissive reuse is
   **Monaco (the editor) and, for C#, RoslynPad/AvalonEdit**. — *(Verified, [ED1]; the JetBrains/Eclipse non-fit Inferred)*
9. **A read-only Monaco or AvalonEdit gives "review surface" features cheaply — diff, folding, go-to, inline
   annotations.** Both support read-only mode, folding, and gutter/margin decorations, which cover the code-
   review affordances (highlight a range, annotate a line, show a diff) the node-walk wants when landing on a
   code node. Monaco's diff editor is a first-class component. — *(Verified, [ED1][ED3])*
10. **WebView2 content model is the load-bearing constraint for all web-rendered panes.** One
    `CoreWebView2Environment`, virtual-host-name mapping for local assets, per-origin renderer sharing — the
    same rules as the graph/diagram panes (`ai-native-ide-shell`). The markdown-HTML, Monaco, diagram and graph
    panes should **share one environment and origin** so four "web panes" cost one browser process, not four. — *(Verified, cross-ref ai-native-ide-shell)*

## Confidence summary

- **Verified:** the MIT/BSD licences of Monaco, AvalonEdit, RoslynPad, the Monaco WPF wrappers, Markdig and
  Markdig.Wpf; Monaco being the VS Code editor and needing WebView2; AvalonEdit being native managed WPF;
  RoslynPad's Roslyn integration; Markdig.Wpf rendering to FlowDocument; WebView2 being proprietary-but-free.
- **Inferred:** consolidating the markdown-HTML and diagram renderers onto one WebView2 surface; the read-heavy→
  native / interactive→web split; that JetBrains/Eclipse editors are not cleanly embeddable in WPF.
- **Flagged (load-bearing):** the **exact current versions** of Monaco, AvalonEdit, RoslynPad and Markdig (all
  move); and **which Monaco WPF wrapper is best-maintained** (WPFMonaco vs Monaco.Editor.WebView vs rolling your
  own thin wrapper over `WebView2` + the official Monaco build — verify liveness before depending).

## Design implications (what /design should do with this)

- **Pick the renderer per node type, and consolidate web panes.** The introspection router (Base A) routes:
  **C# code node → RoslynPad/AvalonEdit (native) or read-only Monaco (web)**; **other-language code → Monaco**;
  **knowledge/markdown node → Markdig.Wpf (native, fast) for plain docs, or markdown→HTML in WebView2 when it
  contains Mermaid/interactive content**; **diagram/report node → the shared WebView2 pane**. All web panes share
  one `CoreWebView2Environment` + origin.
- **Default the knowledge renderer to native Markdig.Wpf, escalate to WebView2-HTML on demand.** Most specs/ADRs/
  decision-notes are plain markdown — render them native (no airspace, themed, cheap). Detect embedded Mermaid/
  HTML and offer "open in rich view" (WebView2). This keeps the common case native and the rich case web.
- **Choose the code surface by breadth requirement.** If the editor must show many languages richly, standardise
  on **Monaco (read-only) in WebView2**; if C# fidelity + native compositing matter most and other languages are
  secondary, use **RoslynPad/AvalonEdit** for C# and Monaco for the rest. Decide once; do not host two code
  renderers for the same language.
- **Use read-only + decorations for the review surface.** Read-only Monaco/AvalonEdit with gutter decorations and
  a diff view covers "read this code, see what changed, annotate a line" — the review affordances of the node-
  walk — without building an editor.
- **Theme every renderer from the one token system.** AvalonEdit/Markdig.Wpf theme via WPF resources; Monaco/
  HTML theme via a CSS variable set mirroring the `wpf-modern-ui-styling` tokens (dark-first, Inter/Segoe,
  the elevation/radius scale) so native and web panes look like one app.
- **Respect the airspace boundary.** Do not expect WPF shadows/Mica to fall over a Monaco/WebView2 code pane;
  give web panes their own in-content styling and keep the soft-chrome effects on the native frame.

## Cross-references

- The node-walk that drives which renderer opens → [`graph-experience-and-visualization`](../graph-experience-and-visualization/index.md).
- WebView2 process model, airspace, one-environment rule → [`ai-native-ide-shell`](../ai-native-ide-shell/index.md).
- The chrome/tokens these panes are themed to → [`wpf-modern-ui-styling`](../wpf-modern-ui-styling/index.md).
- Roslyn (already in the daemon) that RoslynPad leverages → [`code-and-infra-extraction`](../code-and-infra-extraction/index.md).
- Markdown rendered as diagrams (Mermaid) → [`diagram-generation`](../diagram-generation/index.md).

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The licence facts and the decision
matrix in `references.md`/`data-and-constants.md` are the ones to quote. Refresh when Monaco, AvalonEdit or
Markdig ship a major version.
