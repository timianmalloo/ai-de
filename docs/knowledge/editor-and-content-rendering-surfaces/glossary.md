---
id: kb-content-rendering-glossary
title: "Editor & Content Rendering Surfaces — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, monaco, avalonedit, markdig, webview2]
links:
  - { to: kb-editor-and-content-rendering-surfaces, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Precise definitions for the code-viewing and content-rendering vocabulary so the panes and their
  docs agree.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **Monaco Editor** | The code editor extracted from VS Code (MIT), embeddable as a web component; in WPF it runs in WebView2. *(Verified, [ED1])* |
| **AvalonEdit** | ICSharpCode's native managed WPF text/code editor (MIT) — highlighting, folding, decorations; composites natively (no airspace). *(Verified, [ED3])* |
| **RoslynPad** | An editor built on AvalonEdit + Roslyn (MIT) giving C#/VB semantic completion, diagnostics and execution. *(Verified, [ED4])* |
| **Markdig** | The fast, CommonMark-compliant .NET markdown parser (BSD-2-Clause). *(Verified, [ED5])* |
| **Markdig.Wpf** | A WPF renderer (MIT) turning Markdig output into a native WPF `FlowDocument`; no JavaScript. *(Verified, [ED6])* |
| **FlowDocument** | WPF's native rich-text document model — the native (non-browser) target for rendered markdown. *(Verified)* |
| **WebView2** | Microsoft's Edge/Chromium embedding control (proprietary, free) — the host for Monaco, HTML, diagrams and graph panes. *(Verified, [ED7])* |
| **Airspace (problem)** | The WPF limitation that WebView2/HwndHost content renders above the WPF visual tree, so WPF effects (shadow, Mica) do not composite over it. *(Verified, cross-ref ai-native-ide-shell)* |
| **Read-only mode** | An editor configured for viewing/review (no edits) while keeping folding, decorations, diff — the AI-DE default. *(Verified, [ED1][ED3])* |
| **Decoration / gutter margin** | Editor API for marking a line/range (highlight, annotation, diff marker) — the substrate for review affordances and inline agent notes. *(Verified, [ED1])* |
| **Diff editor** | A side-by-side/inline comparison view of two texts; Monaco has one first-class. *(Verified, [ED1])* |
| **Native vs web renderer** | Native = pure WPF (AvalonEdit/RoslynPad/Markdig.Wpf), composites with chrome; Web = WebView2 (Monaco/HTML), richer/interactive but airspace-bound. *(Verified)* |
