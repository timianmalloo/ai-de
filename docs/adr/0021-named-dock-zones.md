---
id: adr-0021-named-dock-zones
title: "ADR-0021: Named absolute dock zones replace the proportional split tree"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "3"
tags: [workbench, layout, docking, architecture, migration]
links:
  - { to: spec-named-dock-zones, rel: implements }
  - { to: investigation-terminal-crash-and-pane-moves, rel: depends-on }
  - { to: architecture, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Adopts a fixed frame of named absolute dock zones (Left/Right/Bottom/Center), each a
  container of a within-zone stack or editor-group split, replacing the cross-zone
  proportional split tree whose single-child collapse relocated unrelated panes (DC-063).
  Splits are scoped inside a zone; moving a pane changes only source and destination.
---

# ADR-0021: Named absolute dock zones replace the proportional split tree

**Status:** Accepted (implemented) · **Date:** 2026-08-31 · **Deciders:** @timianmalloo (+ Design session)

> **Implemented** via a Strangler Fig — `ZoneBackedLayoutService : ILayoutService` projecting the zone
> model to a fixed-shape tree the existing adapter renders — rather than an adapter rewrite; see
> `note-zone-strangler-approach`. The **contract phase below is N/A** (the tree is now the projection
> format), and **persistence is deferred** (no active restore caller). Model + view containment tests
> ship green (DC-063 controlled).

## Context

The workbench layout is a **proportional split tree**: `Layout(Root, Floating, MaximizeMemo)` where
`Root` is a tree of `SplitNode{Orientation, Children, Weights}` and `StackNode{Surfaces, ActiveIndex}`.
`LayoutService.Remove` collapses a single-child split into its child (`children.Count == 1 =>
children[0]`), and every mutation rebuilds the entire dock view via `Adapter.Render()`.

Two consequences, both reported repeatedly by the user and verified in
`docs/investigations/terminal-crash-and-pane-moves.md` (**DC-063**):

1. **Non-local moves.** Moving/removing a pane reorients or relocates *unrelated* panes (a left
   column flips to a top row) because collapsing an interior split restructures the tree above it.
2. **Whole-view redraw.** Every op rebuilds all panes, so content visibly jumps.

The user's own framing is the target model: *"are the docks themselves absolute and then panes
contained within docks?"* — yes, that is exactly what every mainstream IDE does (VS Code, Visual
Studio, JetBrains/Rider, AvalonDock), and it was **never a deliberate choice here** — the split tree
was the more general primitive adopted by default and then patched twice (destination-side
sibling-insert; this source-side collapse) rather than reconsidered.

## Decision

Replace the cross-zone split tree with a **fixed frame of named, absolute dock zones**. Splits still
exist but are **scoped strictly within a zone**; the top-level frame is not a tree.

### Model

```csharp
public enum ZoneId { Left, Right, Bottom, Center }   // Top deferred (spec open-question 1)

public sealed record WorkbenchLayout(
    IReadOnlyDictionary<ZoneId, ZoneState> Zones,     // all four always present
    IReadOnlyList<FloatingWindow> Floating,           // unchanged from today
    MaximizeMemo? Maximized);                          // reversible maximize snapshot

public sealed record ZoneState(
    ZoneId Id,
    ZoneContent Content,       // what the zone holds (below)
    double Extent,             // cross-axis size vs Center (proportion); ignored for Center
    bool Collapsed);           // tool zones only; Center is never collapsed

// A zone holds EITHER a tab stack OR a split — but that split's children are stacks/splits
// that never leave this zone. This is the containment boundary.
public abstract record ZoneContent;
public sealed record ZoneStack(IReadOnlyList<SurfaceId> Surfaces, int ActiveIndex) : ZoneContent;
public sealed record ZoneSplit(Orientation Orientation,
    IReadOnlyList<ZoneContent> Children, IReadOnlyList<double> Weights) : ZoneContent;  // Center editor groups
```

The **invariant that fixes DC-063**: every layout operation names a `ZoneId`, and its effect is
confined to that zone (and, for a move, the destination zone). The frame — which zones exist and
where they sit — is constant. There is no operation that restructures the relationship *between*
zones.

### Zone semantics

| Zone | Role | Collapse | Split within |
|---|---|---|---|
| **Center** | Documents / editor groups — the anchor; always present | No (never < 1 group; empty ⇒ placeholder) | Yes — editor groups (H/V), scoped to Center |
| **Left / Right** | Vertical tool stacks (explorers, diagrams parked there) | Yes → rail | v1: stack only |
| **Bottom** | Horizontal tool stack (terminals, diagnostics, output) | Yes → rail | v1: stack only |

### Operations (`LayoutService`, rewritten)

- `MovePane(SurfaceId, ZoneId target, DropIndex)` — remove from source `ZoneStack`/`ZoneSplit`, add
  to target; **only** source and target `ZoneState` values are rebuilt; source collapses to rail if
  it becomes empty (Center becomes a placeholder, never disappears).
- `ClosePane(SurfaceId)` — remove from its zone only.
- `OpenPane(SurfaceId, ZoneId target)` — add as active tab of target (target resolved by kind +
  focus, see below); destination-local (AC-F7).
- `CollapseZone/ExpandZone(ZoneId)` — flips `Collapsed`; panes retained (AC-F4).
- `ResizeZone(ZoneId, double extent)` — changes that zone's `Extent` and the Center only (AC-F6);
  min-extent clamps to collapse rather than to zero.
- `Maximize/Restore(ZoneId|SurfaceId)` — snapshot the full `WorkbenchLayout` into `MaximizeMemo`;
  restore is an exact structural replace (AC-F5).

Placement resolution replaces `DocumentPlacementPolicy`'s tree logic with a **zone router**:
`kind → default ZoneId` (documents → Center; terminal/diagnostics/output → Bottom; explorer → Left),
overridden by **the focused zone** when it is compatible (fixes the "opened in the wrong window"
complaint).

### View adapter (incremental)

The `Adapter` renders **per zone**. A zone whose `ZoneState` reference is unchanged is **not
re-rendered** (reference-equality short-circuit on the immutable records). So a move touches two
zones' visuals; the rest of the frame is untouched — eliminating the whole-view redraw. This is the
architectural fix for the "jump/flip" as distinct from the crash.

### Threading (from the investigation's design input)

Zones change *arrangement*, not *concurrency*. The terminal/pane threading model stays
**pane/surface-local, not zone-local**: WPF has one Dispatcher per top-level window (docked panes
cannot have per-zone UI threads; only a floated window can carry its own Dispatcher), and panes
migrate between zones so zone-scoped threading would churn. What the zone model adds: the **zone owns
the "is this pane visible/active" signal** the pane's existing render-coalescing gate consumes
(richer than raw WPF `IsVisible` — collapsed-zone / inactive-tab / maximized-other), and zones are
the home for **render prioritization** on the single Dispatcher. Principle: *zone owns the
visibility/priority signal; pane owns its threading; session lifecycle is independent of zone view
lifecycle* (collapsing or moving a zone must never stop a pump).

## Migration (expand-migrate-contract)

1. **Expand.** Introduce the `WorkbenchLayout` zone model beside the existing tree; add a
   `TreeToZones` converter that maps: document stacks → Center (as editor groups when the tree had
   side-by-side documents); each tool `StackNode` → the nearest zone by its tree position
   (left-most column → Left, right column → Right, bottom row → Bottom). No surface is dropped
   (AC-F9).
2. **Migrate.** On workspace open, if `layout.json` is the old tree format, convert to zones and
   write the new format; keep the converter and old-format reader for **one release**. Default
   layout (`Layout.Default()`) is reissued as a zone layout (graph in Center, terminal in Bottom).
   The resolved `workspace-open-layout-restore` decision (keep current arrangement on open) is
   preserved — conversion only runs when there is no in-memory arrangement to keep (AC-F8).
3. **Contract.** After one release, delete `SplitNode`/`StackNode` tree types, the tree `Adapter`
   path, and the old-format reader in a separate change.

Reversible: the converter is pure and tested against golden `layout.json` fixtures; the old format
still round-trips during the window, so a rollback re-reads it.

## Alternatives considered

- **A — Named absolute dock zones (this ADR).** Matches the user's mental model and every mainstream
  IDE; makes containment structural (the invariant is enforced by the model, not by careful op code).
  Cost: a real layout-model rewrite + migration. **Chosen.**
- **B — Keep the split tree, soften the collapse + incremental redraw.** Smaller change: don't
  collapse single-child splits (leave a structural placeholder) and diff the view instead of full
  rebuild. Rejected: it *reduces* the surprise but does not *remove* it — the tree can still
  reorient siblings on other operations, and it keeps a mental model the user has explicitly rejected
  twice. It treats the symptom; A removes the class.
- **C — Adopt a third-party docking library (AvalonDock/Dock).** Rejected for v1: the app renders its
  own surfaces (terminal GlyphRun path, canvas, diagram surfaces) and has bespoke placement/focus
  rules; adopting a docking framework is a large dependency and integration surface (BoK Part III
  adopt-or-not) that would re-open the terminal render path we just stabilized. Revisit only if the
  zone model proves insufficient.

## Consequences

**Positive.** Containment is structural (AC-F1/F2 provable at the model level); moves and opens are
local; the whole-view redraw is gone; the model matches the user's mental model and standard IDEs;
threading is cleanly separated from arrangement.

**Negative / cost.** A layout-model rewrite and a migration; `DocumentPlacementPolicy`,
`WorkbenchShell` hosting, and the `Adapter` all change; tests move from tree-shape assertions to
zone-containment assertions. Within-zone splits in Left/Right are deferred (v1 stack-only) — a known,
recorded limitation.

**Unknowns to monitor.** Editor-group split ergonomics in Center; whether Left/Right need
within-zone splits sooner than expected; migration fidelity on unusual saved trees (covered by
golden-fixture tests).

## Conformance / test plan

- Model tests (`AiDe.Core.Tests`): AC-F1…F9 — containment-on-move, no-flip, center-always-present,
  reversible collapse/maximize, local resize, destination-local open, lossless+reversible migration
  (golden `layout.json` fixtures). The **containment test is the DC-063 control** and must be seen to
  fail against a shim that reproduces the old collapse.
- Workbench tests (`AiDe.App.Tests`): AC-U1…U4 — drop targets, focus-aware open, rail reachability,
  no-spatial-surprise.
- UI: `design-lint.py` clean; mockup renders all zone states (AC-UI1…UI3).
