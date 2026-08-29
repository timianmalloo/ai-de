---
id: kb-content-rendering-open-questions
title: "Editor & Content Rendering Surfaces — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, disconfirming, monaco, avalonedit]
links:
  - { to: kb-editor-and-content-rendering-surfaces, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  What the rendering-surface research could not settle, the domain's failure modes, and the
  disconfirming views sought against each renderer choice.
---

# Open questions & domain failure modes

## Unresolved by research

- **Which Monaco WPF wrapper is best-maintained?** WPFMonaco, Monaco.Editor.WebView, or a thin in-house wrapper
  over the official Monaco build + WebView2. Liveness was not verified per-wrapper this session; check commit
  activity before depending. A thin in-house wrapper avoids the bus-factor of a small wrapper repo. *(Flagged.)*
- **Do we need Monaco at all, or does AvalonEdit cover the viewing need?** If AI-DE is C#-first and other-
  language viewing is rare, AvalonEdit/RoslynPad (native, no airspace) may suffice, avoiding a WebView2 code
  pane entirely. Depends on the multi-language requirement — unsettled. *(Flagged.)*
- **Markdig.Wpf FlowDocument fidelity for complex docs** — tables, footnotes, task lists, syntax-highlighted
  code blocks render to what fidelity in FlowDocument vs HTML? Not measured; the escalation-to-HTML rule hedges
  it, but the native-path ceiling is unknown. *(Flagged.)*
- **Exact current versions** of Monaco/AvalonEdit/RoslynPad/Markdig — all move; read NuGet/npm at pin time. *(Flagged.)*

## Known failure modes of this domain

- **Airspace surprises.** Expecting a WPF tooltip/shadow/context-menu to draw over a Monaco/WebView2 pane — it
  won't; menus and overlays over web panes need in-content (HTML) implementations or layered-window hacks
  (`ai-native-ide-shell`). *(Verified.)*
- **WebView2 process sprawl.** Giving the code pane, markdown-HTML pane, diagram pane and graph pane each their
  own `CoreWebView2Environment` = four browser processes = a gigabyte idle. Share one environment + origin. *(Verified, ai-native-ide-shell.)*
- **Two renderers for one language.** Hosting both Monaco and RoslynPad for C# doubles theming, keybinding and
  behaviour surface and confuses users. Pick one per language. *(Inferred.)*
- **JS↔.NET bridge fragility.** Monaco interop (selection, scroll-to-line, decorations from .NET) is a message
  protocol that must be versioned; silent breakage on a Monaco upgrade is a real risk. *(Inferred.)*
- **Theme drift.** Native controls themed from WPF resources and web panes themed from CSS can drift apart on a
  token change unless both derive from one source. *(Inferred.)*

## Disconfirming views we deliberately sought

- **"Just use WebView2 + Monaco + HTML-rendered markdown for everything — one renderer stack."** Attractive
  (uniform, VS Code fidelity, one skill set) and it reuses the shell's WebView2. The case *against* is the
  **airspace** cost (no WPF effects over any content pane) and the process/interop weight; and for *plain*
  knowledge docs, native Markdig.Wpf is lighter and composites. **Verdict:** web for code-breadth and rich/
  interactive content; native for plain markdown and C#-first, airspace-sensitive panes. A single-stack
  simplification is defensible if the team values uniformity over compositing — record it as a deviation if
  chosen. This sharpens the base rather than refuting it.
- **"Just use AvalonEdit/Markdig.Wpf natively and avoid WebView2 for content."** Also viable and airspace-free,
  but it forfeits Monaco's multi-language richness and cannot render Mermaid/interactive knowledge — and the
  shell hosts WebView2 for the graph/diagram panes regardless, so the dependency exists either way. The
  native-default / web-escalate rule captures the best of both.
- **"Reuse the VS Code app or JetBrains editor directly."** Refuted on licence/runtime grounds: the VS Code
  *product* is proprietary (Monaco is the reusable MIT part); JetBrains/Eclipse editors are JVM/SWT and not
  embeddable in WPF. The permissive, embeddable reuse is Monaco (+ RoslynPad/AvalonEdit for native C#).
