---
id: adr-0019-code-viewer-renderer
title: "ADR-0019 — Render the read-only code viewer with native AvalonEdit, not Monaco-in-WebView2"
type: adr
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [architecture, editor, avalonedit, monaco, webview2, airspace, read-only, viewer]
links:
  - { to: spec-editor-surfaces, rel: relates-to }
  - { to: adr-0015-canvas-hosting-and-overlay-strategy, rel: depends-on }
  - { to: adr-0018-node-content-reader-contract, rel: relates-to }
  - { to: kb-content-rendering-comparables, rel: depends-on }
review-by: 2027-02-28
review-suggested: []
summary: >-
  For the read-only code viewer (spec-editor-surfaces US-ED1–ED4), use native AvalonEdit (MIT) rather
  than Monaco-in-WebView2 (MIT). The deciding factor is the repo's own documented WebView2 airspace
  pain (ADR-0015: the windowed control cannot be drawn over and the composition alternative crashes on
  float) — a read-only viewer does not need Monaco's VS-Code parity, so it should not pay a second
  WebView2's airspace and process-risk cost. Markdown content renders via Markdig (BSD-2); rich
  content (Mermaid/charts) reuses the ONE existing canvas WebView2 rather than adding another.
---

# ADR-0019 — Read-only code viewer renderer

**Status:** Accepted · **Date:** 2026-08-30 · **Deciders:** Design (Enterprise Architect + the-Simplifier peers), grounded in ADR-0015 and `kb-content-rendering-comparables`

## Context

`spec-editor-surfaces` needs a **read-only** code viewer (US-ED1–ED4): syntax-highlighted source of a
selected node/file, read-only by construction, bounded (shortfall), with the node's typed edges still
walkable. It is the render side of the ADR-0018 `NodeContentAsync` contract when `RenderKind = code`.

The collected knowledge (`kb-content-rendering-comparables`, all Verified) gives two permissive
candidates: **Monaco Editor** (MIT, web, hosted in **WebView2**, VS-Code-parity highlighting + diff +
decorations) and **AvalonEdit** (MIT, **native** WPF, TextMate-ish highlighting, no airspace). RoslynPad
(MIT, native, C#-only) is a third but language-narrow.

The load-bearing constraint is **not** highlighting quality — it is **hosting**. ADR-0015 is the repo's
own hard-won evidence: the graph canvas is a **windowed WebView2** because *"the windowed WebView2
cannot be drawn over, and the composition control that can kills the process when its pane is floated."*
Every WebView2 in this shell carries that airspace cost and that float-crash risk, and the workbench is
a dockable/floatable multi-pane surface (US-9) where panes **do** float.

## Decision

**Use native AvalonEdit (MIT) for the read-only code viewer.** Render markdown content with **Markdig**
(BSD-2) → `Markdig.Wpf` FlowDocument (native). For genuinely rich content (Mermaid, charts, rendered
HTML reports), **reuse the one existing canvas WebView2 path** rather than instantiating another
WebView2 for the viewer.

## Options considered

1. **AvalonEdit (native) — chosen.** No airspace, no second WebView2, no float-crash surface (ADR-0015);
   MIT; read-only mode + line numbers + folding + on-demand highlighting are built in; sufficient for a
   *viewer* of C#/python/typescript/bicep/sql. *Cost:* highlighting is TextMate-ish, not VS-Code-exact;
   no built-in diff.
2. **Monaco-in-WebView2 — rejected.** Best-in-class highlighting and a ready diff editor, MIT. *But* it
   adds **another windowed WebView2** — the exact hosting the repo already fights (ADR-0015): it cannot
   be overlaid, and floating a pane hosting it risks the documented process crash. VS-Code parity buys
   nothing a **read-only** viewer needs. Rejected on hosting cost, not features.
3. **RoslynPad — rejected as the primary.** Excellent native C# surface but language-narrow; the viewer
   must show python/ts/bicep/sql too. May be revisited *for C# nodes specifically* if AvalonEdit's C#
   highlighting proves insufficient.

## Consequences

- **Positive:** the viewer is a native pane — it docks, floats, and is overlaid like any WPF control,
  with none of ADR-0015's airspace/crash caveats; one fewer WebView2 to manage; MIT throughout.
- **Negative / accepted:** highlighting is good-not-perfect and there is no diff editor. If a future
  spec needs a **diff/merge** surface or VS-Code-exact fidelity, that is a *separate* decision that may
  re-introduce Monaco **for that surface**, hosted with the ADR-0015 discipline (windowed + snapshot
  swap) — this ADR governs the read-only viewer only.
- **Reuse over rebuild:** rich content reuses the existing canvas WebView2 rather than adding another
  (the-Simplifier; BoK adopt-or-not).

## Confidence & residual — the spike was run (residual cleared)

- **Verified (structural):** the WebView2 airspace + float-crash cost (ADR-0015, this repo's own spike
  S4); both libraries' licences and hosting model (`kb-content-rendering-comparables`).
- **Verified (the residual PoC, run 2026-08-30):** the `spikes/avalonedit-viewer` Spike-Protocol PoC
  (AvalonEdit `6.*` on net10.0-windows) confirmed a read-only, syntax-highlighted viewer over a real
  88-line source file: `IsReadOnly=True` (blocks *user* editing — the US-ED1 requirement; it does not
  block the programmatic `Document` API, which is how the viewer *sets* content), `ShowLineNumbers`,
  `SyntaxHighlighting` applied. Decisively, **`TextEditor`'s base type is `Control`** — a pure WPF
  control, **not** an `HwndHost`/WebView2 — so the ADR-0015 airspace/float-crash failure mode is
  **absent by construction**, not merely mitigated.
- **Language coverage (Verified by the PoC):** AvalonEdit ships **21 built-in highlightings**; by
  extension `.cs→C#`, `.py→Python`, `.js→JavaScript`, `.sql→TSQL` are covered — our C#, python, and
  sql, plus JS (TS's near neighbour). **`.ts` and `.bicep` have no built-in definition** and therefore
  **degrade to plain monospaced text** — which is exactly US-ED2's accepted fallback ("an unknown
  language degrades to plain text, never an error"). If TS/bicep highlighting is later wanted, a custom
  `.xshd` definition can be registered with `HighlightingManager` (or TS reuses the JS definition)
  behind the same viewer seam — a follow-up, not a blocker.
- **Cleared:** the RoslynPad-fallback-for-C# contingency is not needed — AvalonEdit's built-in C#
  highlighting is present and sufficient for a viewer. The `NodeContentAsync` `RenderKind`/`Language`
  contract (ADR-0018) still carries what the viewer needs to pick the mode, so the renderer remains
  swappable behind that seam if a future diff/merge surface reopens the Monaco question.
