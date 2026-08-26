# Spike result — webview2-airspace (Phase-2 spike S4)

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · .NET SDK 10.0.303 · WebView2 Runtime
  **151.0.4129.107** · `Microsoft.Web.WebView2` 1.0.3485.44 · `Dirkster.AvalonDock` 5.0.0 ·
  single display at 144 DPI (150%)
- **Command:** `dotnet run --project spikes/webview2-airspace`
- **Raw output:** [`RESULT-raw.txt`](RESULT-raw.txt)

## The question, and why the obvious form of it is the wrong one

ADR-0008 chose a WPF shell hosting WebView2 for the graph canvas and recorded **airspace** as its
reversal trigger. "Does WebView2 have airspace limitations in WPF" is documented and could have been
answered from a doc page. The question that actually decides AI-DE is narrower: ADR-0012 put every
surface inside an **AvalonDock stack**, so the canvas lives in a pane that can be floated, tabbed,
hidden and resized by a library that knows nothing about hosted browsers.

Both hosting modes were driven through the **same five probes**, because the useful output is the
difference between them.

**How airspace was made measurable.** `RenderTargetBitmap` renders WPF's visual tree only;
`PrintWindow` with `PW_RENDERFULLCONTENT` captures the window as composited. A WPF overlay is placed
in the same `Grid` cell as the WebView2 and later in z-order, so ordinary WPF rules say it must paint
on top. Sampling the overlay's region in the composited capture answers which surface actually owns
that pixel — a measurement, not an inference about WPF's composition model.

## Findings

| Probe | `WebView2` (windowed, the default) | `WebView2CompositionControl` |
|---|---|---|
| **Q1 — WPF overlay on top?** | **NO — airspace confirmed** | **YES — airspace gone** |
| **Q3 — survives tab hide/restore?** | yes, repaints | **NO — never repaints** |
| **Q4 — focus** | `Focus()` refused; Tab does not reach it | identical |
| **Q5 — resizes with its pane?** | yes | yes |
| **Q2 — survives float/re-dock?** | yes | **NO — process dies (native AV)** |

### 1. Airspace is real in the default control, and not a close call

```
overlay area, composited : #CC3C4B -> web   [web=38  pane=176  overlay=219]
```

The overlay's own region reads as **web content**. Distances to each candidate surface are 38 (web)
versus 219 (overlay) — six times. The WPF overlay is simply not there.

Concretely, for this product: any **popup, context menu, tooltip, dialog or drag adorner** that
overlaps the graph canvas is invisible. That includes **AvalonDock's own drop-target indicators**,
which are drawn over the pane the user is dragging across — so US-9's drop-target preview would
vanish precisely over the canvas.

### 2. The composition control fixes airspace exactly, and breaks two things that matter more

```
overlay area, composited : #22C55E -> overlay   [overlay=0  pane=160  web=255]
```

Distance **0** — a pixel-exact match. It also puts the web content into WPF's own visual tree, which
the WPF-only capture confirms (it now sees the web content where the windowed control showed bare
pane). As a fix for airspace it is complete.

But under AvalonDock it fails at both of the operations a docking workbench performs constantly:

- **Floating the pane kills the process.** `Direct3D11CaptureFramePool.Recreate` throws
  `ArgumentException: Value does not fall within the expected range` from
  `GraphicsItemD3DImage.UpdateSize`, raised inside WPF's layout pass while AvalonDock shows the new
  floating window. **Handling that managed exception does not save the process** — the next captured
  frame arrives on a pool that is now inconsistent and the process dies with `0xC0000005` inside
  `Direct3D11CaptureFrame.Dispose()`. A native access violation cannot be caught, so there is no
  degrade-gracefully option here: floating the graph pane would terminate AI-DE.
- **Restoring a background tab leaves it blank.** After the tab is reselected the pane samples as
  `#1E293B` — the *pane* colour, not the web content. The `CoreWebView2` is still alive; it just
  never paints again.

**The composition control is therefore not the mitigation.** It trades a rendering limitation for a
crash, and US-9 requires floating panes.

### 3. Focus does not cross the boundary, in either mode

```
after MoveFocus(Next)     : TextBox   (unchanged)
explicit Focus() accepted : False
```

Tab traversal never lands on the canvas, and a programmatic `Focus()` is refused outright — in both
hosting modes. WPF *can* take focus back, so this is not a keyboard trap; it is the opposite
problem, a surface that cannot be entered from the keyboard at all.

Under [ADR-0014](../../docs/adr/0014-accessibility-posture.md) this is no longer an accessibility
veto, but it remains an ordinary product defect: this is a keyboard-first developer tool, and
"focus the graph" from the command palette needs a mechanism the control does not provide by
default. Routing focus into web content requires the `MoveFocusRequested` / `CoreWebView2.MoveFocus`
protocol, which is a design obligation rather than something that works by construction.

### 4. What both modes do handle

Rendering inside a docked pane, resizing with the pane, and — for the windowed control — floating,
re-docking and tab restoration all work without losing the browser process. The windowed control is
**stable**; its problem is purely that it cannot be drawn over.

### 5. A cost of the composition control, independent of the crash

`WebView2CompositionControl` needs the WinRT projections (`Microsoft.Windows.SDK.NET`) and throws
`FileNotFoundException` from its own `Loaded` handler on a plain `net10.0-windows` target. Using it
requires a Windows SDK TFM — the spike targets `net10.0-windows10.0.19041.0`, while `src/AiDe.App`
targets `net10.0-windows`. Adopting it would mean raising the shipped app's target framework.

## Verdict

**ADR-0008's reversal trigger IS met, and the obvious mitigation is worse than the problem.**

The decision now needs one of these, and this spike deliberately does not choose:

1. **Keep the windowed control and forbid WPF chrome over the canvas.** Every overlay that could
   cross it — drop indicators, context menus, tooltips, the command palette — is either rendered
   *inside* the web content or positioned to avoid it. Cheapest, and it constrains US-9's drop-target
   preview over one pane.
2. **Reverse ADR-0008 for the canvas** and render the graph in WPF. Removes the whole class of
   problem, at the cost of the graph-rendering libraries that motivated WebView2.
3. **Composition control with floating disabled for the canvas pane.** Rejected as written: it
   depends on a crash never being reached by a user action that US-9 explicitly offers, and the
   blank-after-tab-restore defect remains.

## Residual risk

- **One machine, one display, 150% scale, one runtime version.** The evergreen runtime updates
  itself, and both the crash and the repaint failure are runtime-version-sensitive by nature. This is
  a snapshot, not a permanent property.
- **The crash was reproduced through AvalonDock's float path only.** Other layout operations that
  resize a composition-hosted control to an unusual size may hit the same `UpdateSize` path; not
  enumerated.
- **Airspace was proven for one overlay in one pane.** It is a property of HWND composition rather
  than of this arrangement, so it generalises — but the *specific* claim that AvalonDock's drop
  indicators are hidden is Inferred from that property, not separately measured.
- **`GetSourceGeneratedDocumentsAsync`-style second paths were not explored for focus.** The
  `MoveFocusRequested` protocol is documented and untested here.
