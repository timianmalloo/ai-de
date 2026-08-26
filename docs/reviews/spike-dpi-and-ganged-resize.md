---
id: spike-dpi-and-ganged-resize
title: "Spike — per-monitor DPI and ganged resize"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "1b"
tags: [spike, dpi, multi-monitor, resize, adr-0012]
links:
  - { to: adr-0012-docking-shell-library, rel: documents }
  - { to: design-phase-1b-workbench, rel: relates-to }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The last two ADR-0012 spikes. Found the app was System DPI aware rather than Per-Monitor V2 — a
  prerequisite defect for US-9's floating panes, in our code rather than the docking library's.
  Fixed and verified against the running executable. Ganged resize holds: no two docked panes share
  area. The cross-monitor transition itself remains untested for want of a second display.
---

# Spike — per-monitor DPI and ganged resize

- **Run:** 2026-08-26 · Windows 11 Pro 10.0.26200 · **one display** at 144 DPI (150%) · .NET 10.0.303

## 1. Per-monitor DPI — a prerequisite defect, in our code not theirs

**Finding: AI-DE was `SYSTEM_AWARE`, not `PER_MONITOR_AWARE_V2`.** Measured, not inferred.

System-aware means Windows tells the app the **primary** display's DPI once at startup, and the app
assumes it everywhere. A window moved to a different-DPI display is then **bitmap-stretched by the
OS** — blurry text, and coordinates that do not round-trip. US-9 requires floating panes to restore
onto the display they were on, so this is a precondition of the feature rather than polish.

Crucially this is **our defect, not AvalonDock's**. WPF defaults to System-aware unless an
application manifest says otherwise, and AI-DE shipped no manifest at all. Testing the library's
cross-monitor behaviour inside a System-aware host would have measured our bug and blamed the library.

**Fix, verified end to end:** an `app.manifest` declaring `PerMonitorV2`, referenced by
`<ApplicationManifest>` in the csproj. Confirmed by probing the **running `AiDe.App.exe`**, not the
build output or the test host:

```
AiDe.App.exe DPI awareness: PER_MONITOR_AWARE_V2
```

### A measurement error worth recording

The first probe reported `UNAWARE`. It was wrong: it read the thread's DPI context **before WPF had
initialised**, catching the process default rather than the value WPF sets when it creates its first
window. Re-measured after `Window.Show()` it reported `SYSTEM_AWARE`.

Same class as the `ClassName` lookup miss in the UIA probe: *a probe that runs at the wrong moment
reports a confident, wrong fact.* Both corrections came from asking "could this be looking at the
wrong thing?" rather than from the number looking implausible — which is the point, because it did
not look implausible either time.

### What could NOT be tested here

**This machine has one display.** The cross-monitor transition — drag a floating pane to a second
display at a different scale, save, restart, restore — **was not run and cannot be run on this
hardware.** Per-Monitor V2 is now declared and verified, which is the *precondition*; whether
AvalonDock's floating windows behave correctly across a real DPI boundary remains **Inferred**.

The honest residual: this spike removed a certain defect and could not confirm the absence of a
possible one. It stays open in ADR-0012, needing a two-display machine.

## 2. Ganged resize — holds

**Finding: no two docked panes share area.** The realized pane rectangles were measured in the
docking manager's coordinate space and pairwise intersected; every intersection is under one square
pixel, i.e. edge-sharing only. Panes touch; they never overlap. Moved from **Flagged** to
**Verified**.

The model-side half is separately pinned: split weights always sum to 1, so a resize takes from the
neighbour exactly what it gives the resized pane — a redistribution, never a gap.

### A second measurement error worth recording

The first version of this test **summed pane widths and compared the total to the container**,
reporting *"1319px inside an 885px container — they overlap"*. That was wrong: the default layout
stacks two panes **vertically**, so summing their widths double-counts the horizontal space they
share. There was no overlap.

The corrected test asserts the actual contract — *no two panes share area* — which is geometric and
therefore orientation-agnostic. The lesson generalises: **a test that encodes a proxy for the
invariant fails differently from the invariant.** Summing widths was a proxy; pairwise intersection
is the invariant.

## Tests that keep these measured

| Test | Pins |
|---|---|
| `TheApplication_DeclaresPerMonitorV2DpiAwareness` | The manifest exists, declares PerMonitorV2, **and is referenced by the build** — a manifest the build ignores is decoration. It asserts the shipped artifact rather than the current process, because a test host cannot measure the app's DPI awareness. |
| `NoTwoPanesOverlap_AndNonePaneCollapsesToNothing` | The tiling contract on the realized view, geometrically. |
| `ResizingRedistributesSpace_RatherThanLeavingAGap` | The model-side invariant behind it. |
