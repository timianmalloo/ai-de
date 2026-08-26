---
id: adr-0015-canvas-hosting-and-overlay-strategy
title: "ADR-0015 — Host the graph canvas in the windowed WebView2 and yield it to WPF by snapshot swap"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [architecture, webview2, wpf, airspace, canvas, docking, focus]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0008-shell-host, rel: refines }
  - { to: adr-0012-docking-shell-library, rel: relates-to }
  - { to: design-phase-2-real-code-and-terminal, rel: relates-to }
review-by: 2027-02-26
summary: >-
  Spike S4 met ADR-0008's reversal trigger: the windowed WebView2 cannot be drawn over, and the
  composition control that can kills the process when its pane is floated. ADR-0008 is not reversed.
  The canvas keeps the windowed control, moves its own chrome into the web content, and hides behind
  a pixel-aligned still frame for the moments shell chrome must cross it.
---

# ADR-0015: Host the graph canvas in the windowed WebView2 and yield it to WPF by snapshot swap

- **Status:** Accepted
- **Date:** 2026-08-26
- **Deciders:** Product owner, Enterprise Architect, Native Desktop, Tech Lead
- **Context spec/architecture:** ADR-0008 · ADR-0012 · US-9 · Phase-2 design

## Context

[ADR-0008](0008-shell-host.md) chose a WPF shell hosting WebView2 for the graph canvas and named
**airspace** as its explicit reversal trigger. [Spike S4](../../spikes/webview2-airspace/RESULT.md)
ran that trigger on 2026-08-26 and **met it**:

- **Airspace is real and not marginal.** A WPF overlay in the same `Grid` cell as the WebView2, later
  in z-order, is not drawn — its own region samples as web content at a colour distance of 38 versus
  219. Any popup, menu, tooltip or adorner over the canvas is invisible.
- **`WebView2CompositionControl` is not the fix.** It removes airspace exactly (distance 0) and then
  **terminates the process** when AvalonDock floats its pane: `ArgumentException` from
  `GraphicsItemD3DImage.UpdateSize`, then an uncatchable `0xC0000005` in
  `Direct3D11CaptureFrame.Dispose()`. It also never repaints after a tab restore, and needs a Windows
  SDK TFM the app does not currently target.
- **`Focus()` is refused in both hosting modes**, and Tab traversal never reaches the canvas.

[ADR-0012](0012-docking-shell-library.md) puts every surface in an AvalonDock stack, so the canvas
lives in a pane that can be floated, tabbed and dragged across — and AvalonDock draws its own
drop-target indicators over whatever pane the pointer is crossing. That is US-9 functionality
failing silently over exactly one pane, which is the worst kind of failure: nothing errors.

## Decision

**ADR-0008 is not reversed. The canvas keeps the default (windowed) `WebView2` control**, and the
overlap problem is addressed in three layers rather than by a hosting change.

1. **Canvas-owned chrome moves into the web content.** Context menus, node tooltips, hover cards,
   selection affordances and inline legends are rendered by the page, not by WPF. These need graph
   coordinates anyway, so this is the natural home rather than a workaround.
2. **Shell-owned chrome that must cross the canvas triggers a snapshot swap.** The live control is
   hidden behind a pixel-aligned still frame of its last rendered content for the duration; WPF then
   composites normally because an `Image` is ordinary visual-tree content. This covers the two cases
   the canvas cannot own: **AvalonDock drop indicators during a pane drag**, and the **command
   palette** over a large canvas.
3. **The capture is speculative, not synchronous.** It is taken when a drag *could* begin — pointer
   down on a pane title, before the drag threshold — so the measured ~34 ms is never spent on a
   drag's first frame.

**No WPF chrome may be positioned over the canvas outside the swap.** A surface that needs to overlap
either moves into the page (rule 1) or triggers the swap (rule 2); there is no third option, and a
regression here is invisible rather than loud.

## Evidence

[`spikes/webview2-snapshot-swap`](../../spikes/webview2-snapshot-swap/RESULT.md), 2026-08-26, at
**150% DPI** — the scale where a device-pixel capture meeting a DIP layout goes wrong:

| Claim | Measurement |
|---|---|
| The still frame is pixel-aligned | All 8 samples agree, **including all four seam-straddling pairs** ~1% either side of a hard colour boundary. Capture 1620×957 against a canvas of 1619×956 device px — one pixel of rounding, no rescale. |
| WPF draws over it | Overlay region reads `#22C55E`, an exact match. |
| The live canvas returns intact | Identical to pre-swap, `CoreWebView2` still alive. |
| Repetition is stable | 8 swap/restore cycles, no drift, no leak. |
| Capture cost | **p50 34.2 ms**, min 30.7, max 47.3. |

## Alternatives considered

- **Reverse ADR-0008 and render the graph in WPF.** The serious alternative, and it removes the whole
  class — airspace, focus, and an evergreen runtime that updates itself underneath version-sensitive
  findings. Rejected **for now**: the expensive part of a graph canvas is layout (force-directed
  placement, edge routing, hit testing at scale), not drawing, and that is precisely what the web
  ecosystem already has. Spike S3 showed WPF *draws* fast; that says nothing about ELK-class layout.
  This remains the fallback if the constraints above prove unlivable, and the cost of switching later
  is bounded because the canvas sits behind a contract.
- **`WebView2CompositionControl`, with floating disabled for the canvas pane.** Rejected: it depends
  on a process-killing crash never being reached by an action US-9 explicitly offers, and the
  blank-after-tab-restore defect remains regardless.
- **Accept invisible drop indicators over the canvas.** Rejected: a US-9 feature that fails silently
  over one pane is worse than one that fails loudly everywhere.

## Consequences

- **Positive:** ADR-0008's ecosystem rationale is preserved; the app keeps its `net10.0-windows`
  target; the failure mode is a constraint to design within rather than a crash to avoid.
- **Negative / accepted:** a rule that must be honoured by every future surface author, and whose
  breach is invisible; a ~34 ms speculative capture; a frozen canvas for the duration of a drag; and
  the canvas must implement its own menu/tooltip layer instead of reusing WPF's.
- **Follow-ups:** focus routing is designed separately (Phase-2 design, "Focus routing across the
  canvas boundary") and is **not** solved by this ADR. A **colour flash at the swap is not ruled
  out** — the spike's two capture paths photograph colour differently, so a human must look at one
  real drag. Cross-monitor drags at mixed DPI remain untestable on current hardware.

## Reversal trigger

Reconsider rendering the graph in WPF if any of these hold: the swap shows a visible flash to a human
observer; a canvas-owned menu/tooltip layer proves substantially more expensive than reusing WPF's;
or a WebView2 runtime update reintroduces a defect in the windowed control's docking behaviour.
