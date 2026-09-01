---
id: note-zone-strangler-approach
title: "Dock zones implemented via Strangler Fig, not an adapter rewrite"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "3"
tags: [workbench, layout, docking, strangler, decision]
links:
  - { to: adr-0021-named-dock-zones, rel: refines }
  - { to: investigation-terminal-crash-and-pane-moves, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Chose to implement ADR-0021's named dock zones as a Strangler Fig — a
  ZoneBackedLayoutService : ILayoutService that projects the zone model to a fixed-shape
  tree the existing AvalonDock adapter renders — rather than rewriting the 554-line adapter.
  Records why, and why persistence (dz-persist) is deferred and the contract phase is N/A.
---

# Dock zones via Strangler Fig, not an adapter rewrite

## Decision

Implement ADR-0021 by keeping the existing `WorkbenchAdapter`, `WorkbenchShell`,
`WorkbenchController` and persistence unchanged, and swapping the layout **engine** underneath:
`ZoneBackedLayoutService : ILayoutService` holds the zone model (`WorkbenchLayout`) and projects it,
via `ZonesToTree`, to a **fixed-shape** `Layout` tree that the adapter already knows how to render.
Confidence: **Verified** (all slices landed green; app runs; model + view containment tests pass).

## Why (over the alternatives)

- **Rewriting the adapter** to render zones onto AvalonDock's native anchorable/document panes was the
  obvious reading of the ADR, but it is a 554-line rewrite against an intricate AvalonDock API, plus a
  new view→model read-back, touching the terminal render path just stabilised in Phases 1–2. High
  blast radius on the app the user is actively testing.
- **The Strangler** turned the risky rewrite into additive, independently-tested Core code
  (`ZoneLayout`, `ZoneLayoutService`, `ZonesToTree`, `ZoneBackedLayoutService`) plus a **one-line**
  shell swap. Because the projected frame is always the same shape, rendering it cannot flip — the
  containment fix falls out of the projection rather than out of new rendering code. All 223 App tests
  passed unchanged after the swap, which is the evidence the contract was preserved.

## Consequences / what this leaves

- The legacy tree types (`Layout`/`SplitNode`/`StackNode`) and the old `LayoutService` are **retained**
  — the tree is now the projection/render format, and ~10 test files still exercise the tree mechanics
  directly. So the ADR's **contract phase (delete the split tree) is not applicable**: there is nothing
  to delete; the tree is load-bearing as the projection.
- **Persistence (dz-persist) is deferred.** The shell deliberately does **not** call
  `Persistence.Restore()` on workspace open or startup (the resolved keep-arrangement decision), so the
  saved layout has no active reader. Making persistence zone-faithful (preserving collapsed-zone content
  and exact extents, which the projected tree loses) would be building ahead of a consumer that does not
  exist — YAGNI. If restore is ever re-enabled, `ZoneBackedLayoutService.Restore` already degrades
  safely (position-aware for a fixed-frame tree, kind-based conversion otherwise), losing arrangement
  detail but never a surface. Re-open dz-persist then, with a `WorkbenchLayout` serializer.

## Follow-ups (captured, not lost)

- Native AvalonDock **within-zone splitting** (side-drops creating extra panes) is not mapped to zones;
  the position-aware reconcile returns null for those shapes and the model reverts them (safe). A
  richer drag-to-zone (with in-Center editor-group splits in Left/Right too) is future work.
- A true **collapse-to-rail** visual (rather than hiding a collapsed zone) needs adapter/AvalonDock
  work; today a collapsed tool zone is omitted from the projection and re-expanded via the Window menu.
