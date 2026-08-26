# Spike result — terminal-renderer (Phase-2 spike S3)

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · .NET SDK 10.0.303 · Cascadia Mono ·
  144 DPI (150%) · **Debug configuration**
- **Command:** `dotnet run --project spikes/terminal-renderer`
- **Raw output:** [`RESULT-raw.txt`](RESULT-raw.txt)

## What this spike asks, and why the question changed

The Phase-2 design scoped S3 as *"which renderer meets the keyboard/screen-reader contract?"*, with
the note that "the a11y contract is a hard veto". [ADR-0014](../../docs/adr/0014-accessibility-posture.md)
withdrew that obligation on 2026-08-26, so the selection criteria are now **throughput, fidelity,
input handling, licence and integration cost**.

That re-weighting removes the criterion that would have decided between candidates, so the question
becomes: **is owning a WPF renderer fast enough?** The other two candidates are already settled by
evidence in hand rather than by preference:

| Candidate | Status | On what evidence |
|---|---|---|
| Embed an existing WPF terminal control | **rejected** | ADR-0005 — the available controls are unsupported CI artifacts, and a control update would change process/session semantics. |
| Host `xterm.js` in WebView2 | **rejected** | [Spike S4](../webview2-airspace/RESULT.md), same day. Airspace in the default control; a **process-killing native crash** in the composition control when its pane is floated; and `Focus()` refused in *both*. A terminal is the surface that most needs keyboard focus, which makes it the worst possible candidate for that particular defect. |
| **Own a WPF renderer** | **measured here** | — |

## Findings

### 1. Drawing is not the constraint — but the obvious design is 21× too slow

Full-screen redraw, 200 × 50 = 10,000 cells, 60 frames, rasterised offscreen so the number is WPF's
actual work rather than the cost of building an instruction list:

| Approach | p50 | p95 | fps ceiling | Verdict |
|---|---:|---:|---:|---|
| **`GlyphRun` per line** | 5.95 ms | **6.64 ms** | **151** | inside 60 fps, 2.5× margin |
| `FormattedText` per line | 10.14 ms | 12.28 ms | 81 | inside 60 fps |
| `FormattedText` per **cell** | 98.52 ms | 142.80 ms | 7 | **over budget** |

The spread is the finding. A terminal is *conceptually* a grid of independently styled cells, and
modelling it that way — one `FormattedText` per cell — is the natural first implementation. It is
**21× slower than the cached-glyph path** and misses the frame budget by more than 4×, at 7 fps.
Nothing about that design looks wrong when you write it; it only shows up when measured.

`GlyphRun` per line is what real terminals do, and the margin is comfortable enough that
damage-tracking (redrawing only changed rows) stays an optimisation rather than becoming a
requirement.

### 2. Parsing is nowhere near the constraint

```
1.00 MiB of representative VT output parsed in 0.4 ms  =>  2360.7 MiB/s
printable=1,013,245  escapes=4,129  newlines=16,400
```

**2361× the architecture's 1 MiB/s budget** on a single thread. The parse/draw distinction matters
because the budget is stated as an *output rate* and is easy to read as a *draw rate*: a terminal
coalesces, so it must consume a megabyte a second while only ever presenting the final screen state
at frame rate. Those are different jobs by three orders of magnitude, and conflating them is how a
renderer gets blamed for a parser's cost.

**Scope of that number, stated so it is not over-cited:** the scanner classifies printable text,
CSI escapes and newlines. It is not a terminal emulator — no state machine, no OSC handling, no
wide-character or grapheme-cluster work. It establishes that parsing is *plausibly not* the
bottleneck. It does not establish that "our VT parser is fast", because there is no VT parser yet.

### 3. Measured in Debug, which makes the conclusion safer rather than weaker

These numbers come from a `Debug` build. Release would be faster, so every figure here is a **floor**
— the viability conclusion holds a fortiori. It does mean the specific millisecond values should not
be quoted as the renderer's performance.

## Verdict

**Owning a WPF terminal renderer is viable on throughput, and S3 clears.** The renderer is not the
constraint; the coalescing policy is, and that is a design decision rather than a platform limit.

Two things this makes binding for Phase 2:

1. **The draw path is `GlyphRun` per line with a cached `GlyphTypeface`**, not per-cell text. This
   is a performance-critical decision that looks like an implementation detail, so it belongs in the
   design rather than being rediscovered.
2. **`ITerminalSession` stays renderer-independent** (ADR-0005 unchanged). Nothing measured here
   argues for letting the renderer own session state.

## Residual risk

- **This is a synthetic screen, not a real ConPTY session.** `spikes/conpty-foundation` established
  create/resize/close only; the input/output service loops are still unexercised, and end-to-end
  ConPTY → parser → renderer latency is unmeasured. This spike answers "can WPF draw fast enough",
  not "does the whole pipeline keep up".
- **No wide characters, combining marks, RTL, or emoji.** The fragments are ASCII build-log lines.
  Grapheme clustering and double-width cells are real terminal work and would change the glyph path's
  cost; unmeasured.
- **No scrollback.** A 50-row viewport was measured. Scrollback affects memory and hit-testing, not
  this draw cost, but it is not covered.
- **Selection, cursor blink and IME are not modelled**, and each adds per-frame work.
- **One machine, one font, one DPI.** Cascadia Mono at 150% scale.
