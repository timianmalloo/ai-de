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
- **Persistence (dz-persist) is implemented.** `ZoneLayoutStore` serializes the `WorkbenchLayout`
  directly (a `.zones.json` sibling of `layout.json`), preserving collapsed-zone content and per-zone
  extent that the projected tree cannot, and dropping surfaces the workspace can no longer provide.
  `LayoutPersistence` is zone-aware (saves zones on the existing debounce/dispose, restores them), and
  opening a workspace now **restores its saved zone arrangement** (`RestoreArrangementOnWorkspaceOpen`).
  This is safe where tree-restore was not: zone restore preserves exact placement (it cannot scatter),
  and an absent/unreadable/corrupt save degrades to "keep the current arrangement" — so it supersedes
  the earlier keep-current guard without reintroducing the reset/scatter complaint.

## Follow-ups (captured, not lost)

- Native AvalonDock **within-zone splitting** (side-drops creating extra panes) is not mapped to zones;
  the position-aware reconcile returns null for those shapes and the model reverts them (safe). A
  richer drag-to-zone (with in-Center editor-group splits in Left/Right too) is future work.
- A true **collapse-to-rail** visual (rather than hiding a collapsed zone) needs adapter/AvalonDock
  work; today a collapsed tool zone is omitted from the projection and re-expanded via the Window menu.
