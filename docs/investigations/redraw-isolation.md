---
id: investigation-redraw-isolation
title: "Investigation — redraw isolation and UI jitter"
type: investigation
status: resolved
owner: "@timianmalloo"
phase: "2"
tags: [performance, rendering, wpf, terminal, jitter, redraw-isolation]
links:
  - { to: architecture, rel: relates-to }
review-by: 2027-02-28
summary: >-
  The whole window felt jittery and a background agent terminal writing output stole focus/activated
  its tab. Root cause: every TerminalView held a persistent CompositionTarget.Rendering subscription —
  a per-frame callback that runs for the life of the control and never lets WPF's render thread go
  idle — and it invalidated regardless of whether the pane was visible. Fixed with event-driven,
  coalesced, visibility-gated invalidation. This note is also the systemic framework for redraw
  isolation across the app.
---

# Investigation — redraw isolation and UI jitter

## Symptoms (reported)

1. **Jitter.** "On redraw it seems like the whole extent of surfaces are doing a redraw which makes the
   entire experience jittery."
2. **Focus steal.** "Two agent terminal sessions running and one is writing to the console — it switches
   focus to that one."

## Root cause (Verified)

Every `TerminalView`, while loaded, ran:

```csharp
Loaded  += (_, _) => CompositionTarget.Rendering += OnFrame;   // per-frame, forever
Unloaded+= (_, _) => CompositionTarget.Rendering -= OnFrame;
void OnFrame(...) { if (_screen.IsDirty) InvalidateVisual(); }
```

`CompositionTarget.Rendering` fires **once per frame (~60 Hz) for the whole application** and calls
*every* subscribed handler. Two consequences:

- **The render thread never idles.** WPF is retained-mode: normally it composites only dirty regions
  and then rests. A live `CompositionTarget.Rendering` subscription defeats that — the UI thread does
  work every frame *forever*, even when every terminal is idle. This is a documented WPF anti-pattern
  (justified only for games/particle systems). With N terminals, N handlers run per frame. *This is the
  jitter:* the compositor is always ticking, so the window never settles.
- **Background panes still invalidated.** A terminal in a **hidden** tab (a background agent) is still
  `Loaded`, so it still polled and still called `InvalidateVisual()` when its screen changed. A hidden
  `LayoutDocument`'s content invalidating is what makes AvalonDock activate that tab — **the focus
  steal.** No application code focuses a terminal on output (the only `Focus()` in the terminal path is
  on mouse-down); the activation came from the background invalidation, not a deliberate focus call.

So **one root cause produced both symptoms**: a per-frame, non-visibility-gated invalidation.

## The fix (applied)

Event-driven, coalesced, visibility-gated repaint. The `CompositionTarget.Rendering` loop is gone.

- **Event-driven:** the session pump calls `view.RequestRedraw()` **only when output changed the
  screen** (`_screen.IsDirty`). An idle terminal causes **zero** repaints and **zero** per-frame work.
- **Coalesced:** `RequestRedraw` uses an atomic `_redrawScheduled` gate + one
  `Dispatcher.BeginInvoke(Render)`, so a producer emitting a megabyte a second collapses into **one**
  `InvalidateVisual` per dispatcher turn (the user only ever sees the last state). Thread-safe (the pump
  raises it from a background thread).
- **Visibility-gated:** a terminal that is **not visible** does not repaint on output — it records that
  it fell behind (`_dirtyWhileHidden`) and repaints **once** when it is shown again. A busy background
  agent can no longer drive repaints of (or activate) a pane the user is not looking at.

Expected to resolve the **focus steal** as well (Inferred — the background terminal no longer invalidates,
so AvalonDock has nothing to react to). Asked the user to confirm live.

## The systemic framework — redraw isolation for this application

Five principles, to apply to every surface (terminal, canvas, class diagram, evidence panes, future
surfaces). This is how to *think* about redraw here.

1. **Event-driven, never frame-polled.** Repaint in response to a **change**, not on a clock. A
   persistent `CompositionTarget.Rendering` (or an always-running `DispatcherTimer` that invalidates) is
   an anti-pattern outside a genuine animation. When idle, the surface must do **no** rendering work.
2. **Coalesce to at most one repaint per frame.** High-frequency inputs (terminal output, streaming
   data, rapid model updates) are collapsed to a single invalidation per dispatcher turn — decouple the
   *data rate* from the *repaint rate*. Never `InvalidateVisual` in a loop; mutate all state, then
   invalidate once.
3. **Gate on visibility.** A surface that is off-screen (background tab, collapsed pane) does not
   repaint; it repaints once when it becomes visible. This both saves work and prevents a hidden pane
   from perturbing the layout/focus of the visible one.
4. **Invalidate at the right level, and only the leaf.** Use `InvalidateVisual` (render only) for
   appearance changes; reserve `InvalidateMeasure`/`InvalidateArrange` for genuine size/position changes
   — those propagate **up** the tree and cause broad re-layout. A content change must not trigger a
   layout pass. For custom controls, prefer dependency properties with `AffectsRender` metadata over
   manual invalidation.
5. **Keep surfaces as isolated render islands.** One surface's churn must not invalidate another's or
   the shell chrome. The windowed surfaces (WebView2 canvas, terminal) are already their own islands;
   the rule for new surfaces is the same — no shared mutable visual state, no cross-surface invalidation,
   and a full `Adapter.Render()` (which rebuilds the dock tree) only on **structural** layout changes,
   never on content updates.

## Verification

- App suite green (209); build clean; smoke-launch clean. The render-loop behaviour is a WPF
  dispatcher/visibility runtime property (not unit-testable headlessly), so it is validated by smoke and
  the reasoning above, and by the user re-testing jitter + focus live.

## References

- WPF rendering model: `CompositionTarget.Rendering` runs every frame for the life of the subscription
  and keeps the render thread from idling (Stack Overflow; WPF rendering best-practice write-ups). The
  recommended pattern is event-driven `InvalidateVisual` on change, batched, with the right invalidation
  level (Measure/Arrange/Render) — see the search trail captured in this run's audit entry.
- In-pack: `agent-body-of-knowledge.md` Part VII.8 (measure, don't guess — profile before optimizing);
  `instrumentation-over-inference.md` (the jitter was reasoned from the code, and the fix's effect should
  be *measured* live). Defect class DC-059 (below).
