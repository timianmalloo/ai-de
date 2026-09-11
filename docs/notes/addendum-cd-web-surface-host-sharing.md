---
id: note-addendum-cd-web-surface-host-sharing
title: "Every web surface in the second docking host goes through WebSurfaceHost — the docking host attaches a pane's content twice on first entry and once per presenter cycle, and initialisation must be gated to the surface's lifetime, not the attach"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, addendum-c, webview2, docking, dc-138, perspective]
links:
  - { to: adr-0031-second-docking-host, rel: relates-to }
  - { to: adr-0017-primary-view-mode, rel: relates-to }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
review-by: 2027-03-11
review-suggested:
  - { by: adr-0017-primary-view-mode, on: 2026-09-11, reason: "ADR-0017 accepted as amended (Ruling 52): the closed set is the Perspective set; a body may be a docking host; second-host clause discharged by spikes/second-dock-host-unparent" }
summary: >-
  The second-dock-host spike measured Loaded firing twice on host B's first attach and once per
  presenter cycle while CoreWebView2 initialised once behind a once-gate; so host B's canvas and any
  later web surface must share WebSurfaceHost (DC-138) — a surface that initialised on Loaded would
  restart on every perspective switch. Blast radius: the Architecture canvas, the composer page if a
  session document ever docks in host B (it does not), any future web surface.
---

# Every web surface in host B goes through `WebSurfaceHost`

- **Kind:** decision
- **Confidence:** Verified — `spikes/second-dock-host-unparent/RESULT.md` (Q1: `Loaded` 2× on the
  first attach, `Ensure` 1×; Q2/Q3: `Loaded` 3 → 4 → 5 → 6 across four cycles, `Ensure` stayed 1);
  `src/AiDe.App/Workbench/WebSurfaceHost.cs:6-30` (the once-gate and its DC-138 rationale)
- **Made during:** `/define-architecture` of Addenda C and D (node A1, session `addendum-c-chain`)

## The call

Host B's graph canvas (a second `CanvasSurface` instance — Ruling 53) and every web surface that
may later be admitted to Architecture are constructed with **`WebSurfaceHost`** as their WebView2
owner, exactly as host A's surfaces are. The reason is measured, not inferred: AvalonDock raises
`Loaded` on the pane's content twice while it builds a document pane on first entry, and the
presenter's body swap raises it once more per return; a surface whose `EnsureCoreWebView2Async`
hangs off `Loaded` without the gate would re-initialise on every perspective switch — the exact
"hidden `HwndHost` restarts" symptom Ruling 52's CONDITIONS name, produced by our own code rather
than by WPF. With the gate, `CoreWebView2` initialised once and the same object survived every
cycle. **The rule generalises:** a native surface keeps its native state outside the attach
lifecycle (the WebView2 wrapper keeps its controller; the terminal keeps its ConPTY session and
screen model in the surface object) — a future `HwndHost`-based surface follows the same rule or
does not enter a perspective host.

## Alternatives dismissed

- `initialise on Loaded, guard by CoreWebView2 != null` — the spike shows the second `Loaded`
  arrives before the first initialisation completes (the race DC-138 records); the gate must be set
  before the first await.
- `re-create the canvas on each entry` — the ADR-0017 invariant forbids it; a re-created canvas
  re-issues the neighbourhood query and loses selection and scroll (P-4's falsifier).

## Validation condition

Holds until/unless the docking library's attach behaviour changes (an AvalonDock bump re-runs the
spike) or a web surface is hosted outside a `DockingManager` in a perspective body.

## Promotion rule

Already carried by ADR-0031 rule 5; this note is the measurement behind it.
