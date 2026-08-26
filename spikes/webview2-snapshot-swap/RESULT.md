# Spike result — webview2-snapshot-swap (gut check for the S4 decision)

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · .NET SDK 10.0.303 · WebView2 Runtime
  **151.0.4129.107** · `Dirkster.AvalonDock` 5.0.0 · single display at **144 DPI (150%)**
- **Command:** `dotnet run --project spikes/webview2-snapshot-swap`
- **Raw output:** [`RESULT-raw.txt`](RESULT-raw.txt)

## What this checks and why

[Spike S4](../webview2-airspace/RESULT.md) established that the windowed WebView2 cannot be drawn
over, and that the composition control — which can — terminates the process when AvalonDock floats
its pane. [ADR-0015](../../docs/adr/0015-canvas-hosting-and-overlay-strategy.md) therefore keeps the
windowed control and hides it behind a **still frame** for the moments WPF needs the space: an
AvalonDock drop indicator during a pane drag, or the command palette over a large canvas.

**The risk was never whether it works — it is whether the seam shows.** The capture comes back in
device pixels while the `Image` is laid out in DIPs, and this machine runs at **150%**, so a naive
swap can land soft, offset or rescaled. So the page is four hard-edged quadrants and the samples sit
**a few pixels either side of each seam**: a sub-pixel misplacement becomes a colour flip rather than
a judgement call about sharpness.

## Findings

### 1. The still frame is pixel-aligned — all four seam-straddling pairs agree

| Sample | Live | Snapshot | Match |
|---|---|---|---|
| crimson centre (25%,25%) | crimson | crimson | yes |
| blue centre (75%,25%) | blue | blue | yes |
| amber centre (25%,75%) | amber | amber | yes |
| violet centre (75%,75%) | violet | violet | yes |
| **left of v-seam (49%,25%)** | crimson | crimson | yes |
| **right of v-seam (51%,25%)** | blue | blue | yes |
| **above h-seam (25%,49%)** | crimson | crimson | yes |
| **below h-seam (25%,51%)** | crimson | crimson | yes |

The four seam pairs are the ones that matter. Each sits ~1% of the canvas from a hard colour
boundary; an offset or scale error of more than a few device pixels would flip at least one.

The capture came back **1620×957** against a canvas of **1619×956** device pixels — a one-pixel
rounding difference, so WPF is not rescaling the image and there is no resampling softness to argue
about.

### 2. WPF draws over the canvas while the snapshot stands in — which is the entire point

```
overlay region : #22C55E -> overlay
```

An exact match for the overlay colour. With the live control hidden, the WPF overlay composites
normally, because a `BitmapImage` in an `Image` element is ordinary visual-tree content. The airspace
limitation is not worked around so much as stepped out of.

### 3. Restore is clean, and repeated swapping does not drift

The live canvas returns identical to its pre-swap state with `CoreWebView2` still alive, and after
**8 swap/restore cycles** every sample is unchanged. No leak, no drift, no re-initialisation.

### 4. The number that deserves a design decision: capture costs ~34 ms

```
capture p50 34.2 ms, min 30.7 ms, max 47.3 ms   (8 cycles)
```

That is **two to three frames at 60 fps**, paid once at the start of each drag, not per frame. It is
almost certainly acceptable — a drag begins with a mouse-down and a threshold, so there is natural
slack — but it is not free and it should not be discovered during implementation.

**The design consequence:** capture must not be on the synchronous drag-start path. Either capture
speculatively when a drag *could* begin (pointer down on a pane title, before the drag threshold is
crossed), or accept a ~34 ms stall on the first frame of a drag. The former is preferable and costs
nothing when the drag never happens.

The full swap measured 390 ms, but that figure includes the settle this harness deliberately forces
before sampling; it is an upper bound on the harness, not a measurement of the mechanism.

### 5. An instrument artifact worth recording, because it looks like a finding and is not

Live samples read `#CC3C4B`; snapshot samples read `#E21D47`. The source colour is `#E11D48`, so the
**snapshot is the more faithful of the two**.

That difference is between the two *capture paths*, not necessarily between what a user sees. The
live sample comes from `PrintWindow` over a composited child HWND; the snapshot comes from the
browser's own `CapturePreviewAsync`. The likely explanation is colour management applied on the
`PrintWindow` path.

**This is exactly why nearest-surface classification is used rather than absolute colour comparison**
(defect class DC-009): an absolute test would have reported all eight samples as mismatched and
condemned a mechanism that is in fact aligned.

But the honest limit follows from the same fact: **this spike cannot rule out a colour flash at the
swap**, because it has no instrument that photographs both states the same way. See the residual
risk below.

### 6. The documented answer for focus does not exist on this control

Spike S4 measured that `Focus()` is refused and Tab never reaches the canvas, which made focus
routing a design obligation. The obvious answer is `CoreWebView2Controller.MoveFocus`. Enumerating
the WPF control's **public declared surface** by reflection:

```
type: Microsoft.Web.WebView2.Wpf.WebView2
base: System.Windows.Interop.HwndHost

PUBLIC focus/controller members declared on the WPF control: 2
  Property FocusVisualStyle : Style
  Method   get_FocusVisualStyle() : Style

public CoreWebView2Controller property   : no
any public property of a Controller type : no
non-public controller properties          : none
```

**The controller is not reachable at all**, so `MoveFocus` and `AcceleratorKeyPressed` are
unavailable. Designing focus routing around them would have produced a design that cannot be built —
and the assembly's *string table* does contain those names, so a grep would have said the opposite.
The public surface had to be enumerated to know.

### 7. The Win32 route does work, and is the mechanism the design uses

`WebView2` derives from `HwndHost`, so it has a real child window handle:

```
HwndHost.Handle        : 0x2A105A
focus before SetFocus  : 0x1DC0D02
focus after SetFocus   : 0xF60C54
parent of focused hwnd : 0x1490E6C
OK  focus landed on a CHILD of the host - the browser's own input window.
```

Focus lands on a *descendant* of the host rather than the host itself, which is the correct and
expected outcome: the browser owns an inner input window, and keystrokes reaching it is the thing
that matters.

Read back with `GetFocus()` rather than trusting `SetFocus`'s return value, which is the
*previously* focused window and whose null case is ambiguous between "failed" and "nothing had
focus".

**This gives the design a verified way IN. It does not give it a way OUT** — nothing here moves focus
from the page back to WPF, which is why the design pairs it with a page→host message protocol.

## Verdict

**The mechanism holds, and the S4 decision is supported.** The still frame is pixel-aligned, WPF
composites over it, the live canvas returns intact, and repeated cycling is stable. One design
obligation falls out: **capture speculatively, before the drag threshold**, so the ~34 ms is not
spent on the drag's first frame.

## Residual risk

- **A colour flash at the swap is NOT ruled out.** The two capture paths disagree on colour by an
  amount that is invisible to nearest-surface classification and would be visible to an eye. Settling
  it needs a human looking at a real drag — a ten-second check, and the right instrument for the
  question. Recorded as **Inferred**: the alignment claim is measured, the "no visible seam" claim is
  measured *for geometry only*.
- **Static content only.** The canvas was four static quadrants. A graph mid-animation, or one with a
  running layout, would freeze at the swap — correct behaviour for a drag, but unmeasured, and the
  freeze is more noticeable the more the content was moving.
- **One DPI scale, one monitor, one runtime version.** 150% is the interesting case for alignment,
  but 100%, 125% and 200% are unmeasured, and a *cross-monitor* drag at mixed DPI is exactly the
  scenario that is still untestable on this hardware.
- **No real AvalonDock drag was performed.** The overlay stands in for a drop indicator. That the
  indicator specifically is occluded remains **Inferred** from HWND composition (S4), not separately
  observed; likewise that the swap fixes it specifically.
- **Scroll and focus state across the swap are unmeasured.** The control is hidden, not recreated, so
  both should survive — but "should" is the word this repo's own standing rule flags, and neither was
  read back.
