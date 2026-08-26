---
id: design-phase-1b-workbench
title: "Phase 1b workbench shell — detailed design"
type: design
status: in-review
owner: "@timianmalloo"
phase: "1b"
tags: [design, phase-1b, workbench, docking, layout, accessibility]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: adr-0012-docking-shell-library, rel: depends-on }
  - { to: adr-0013-layout-persistence-envelope, rel: depends-on }
  - { to: mockup-workbench, rel: relates-to }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The implementable blueprint for the dockable workbench: an owned, headless layout model (tree →
  stack → surface) that both the pointer and the keyboard mutate through one command set, an
  AvalonDock adapter that renders it, and a versioned envelope that persists it.
---

# Design: Phase 1b workbench shell

- **Status:** In review
- **Spec / architecture:** US-9 · [`docs/architecture.md`](../architecture.md) · ADR-0012 · ADR-0013
- **Delivery phase:** **Phase 1b**, between the walking skeleton and Phase 2. **Real:** layout model,
  command set, persistence envelope, named layouts, announcements, AvalonDock adapter. **Mocked:**
  terminal runtime (still the Phase-1 fixture session), Roslyn extractors, graph canvas.
- **Author(s) / date:** @timianmalloo · 2026-08-26

## Responsibility

Own the arrangement of the workspace window — and own the accessibility the docking library does not
provide. **Not** responsible for what any surface renders; a surface's content, identity and state
are independent of where it is docked.

## The load-bearing decision: an owned model, not the library's

The layout lives in **`AiDe.Core.Workbench` as a pure, headless model**. AvalonDock renders it and
reports pointer gestures back to it; it is never the source of truth.

This is not a portability preference. It is what makes three of US-9's requirements *testable at all*:

1. **"The keyboard path and the drag path produce an identical resulting tree" (SC 2.5.7).** This is
   only provable if both funnel through the same operations. If a drag mutated AvalonDock directly
   while the keyboard went through commands, the two paths could silently diverge and no test would
   catch it — which is precisely how VS Code ended up with keyboard resize that *doesn't work in
   floating windows*. Here, a drag is translated into the same `LayoutOperation` the keyboard emits,
   so the equivalence test is a genuine falsifier rather than a tautology.
2. **The tiling invariant** (no gap, no overlap, no empty stack) is checkable after every operation
   without a UI, on every test.
3. **The envelope is ours** (ADR-0013). We serialize *our* model; AvalonDock's serializer becomes an
   implementation detail we may never use, which also retires ADR-0012's bus-factor risk.

**Accepted cost:** two representations must be kept in step. The adapter is therefore one-way —
model → view — with gestures entering only as operation requests, never as direct view mutations.

## Data model

`Layout` is an aggregate whose root invariant is **the tiling**: the tree is always a complete,
non-overlapping partition of the window.

```csharp
public abstract record LayoutNode(string Id);

// A split divides its region among 2..n children by proportional weights that always sum to 1.
public sealed record SplitNode(string Id, Orientation Orientation,
    ImmutableList<LayoutNode> Children, ImmutableList<double> Weights) : LayoutNode(Id);

// A stack is a leaf: 1..n surfaces navigated by tabs. Zero surfaces is not representable.
public sealed record StackNode(string Id, ImmutableList<Surface> Surfaces, int ActiveIndex,
    StackState State, double MinWidth, double MinHeight) : LayoutNode(Id);

public sealed record Surface(string SurfaceId, string Kind, string Title);
public enum StackState { Docked, Floating, Collapsed, Maximized, Hidden }
public enum Orientation { Horizontal, Vertical }
```

**Invariants enforced in the type and the operations, not by convention:**

| Invariant | How it is made impossible to violate |
|---|---|
| A stack always has ≥1 surface | `StackNode` construction rejects an empty surface list; removing the last surface **destroys the stack** rather than emptying it. |
| A split always has ≥2 children | Removing the second-to-last child **collapses the split into its remaining child**. |
| Weights sum to 1 | Every weight-mutating operation renormalizes; the invariant check asserts it to a tolerance. |
| No overlap | Structural: a tree of proportional splits cannot express an overlap. **Floating stacks are held outside the tree**, which is exactly why they are the only things permitted to overlap. |
| Minimum size respected | A resize that would take any sibling below its minimum is **refused and reported**, never silently clamped to zero. |

**Not stored in the fact store** (ADR-0013). A layout is mutable preference; the fact store is an
append-only evidence ledger. Putting one in the other would force an exemption in the single place
this architecture has refused to make one.

## Contracts

```csharp
// Every arrangement change is one of these. Pointer and keyboard both produce them.
public abstract record LayoutOperation
{
    public sealed record MoveSurface(string SurfaceId, DropTarget Target) : LayoutOperation;
    public sealed record ResizeSplit(string SplitId, int EdgeIndex, double Delta) : LayoutOperation;
    public sealed record SetStackState(string StackId, StackState State) : LayoutOperation;
    public sealed record ActivateSurface(string SurfaceId) : LayoutOperation;
    public sealed record ReorderSurface(string StackId, int From, int To) : LayoutOperation;
    public sealed record CloseSurface(string SurfaceId) : LayoutOperation;
    public sealed record ApplyNamedLayout(string Name) : LayoutOperation;
    public sealed record ResetToDefault() : LayoutOperation;
}

public sealed record DropTarget(string TargetNodeId, DropKind Kind);
public enum DropKind { SplitLeft, SplitRight, SplitTop, SplitBottom, JoinStack, Float }

public sealed record LayoutResult(Layout Layout, bool Applied, string? RefusalCode, string Announcement);

public interface ILayoutService
{
    Layout Current { get; }
    LayoutResult Apply(LayoutOperation operation);   // the ONE mutation path
    bool IsLocked { get; set; }
}
```

**`Apply` is the only mutation path.** It validates, applies, re-checks the tiling invariant, and
returns the announcement text — so an operation that mutates without announcing is not expressible.

## Error and concurrency model

Single-threaded UI ownership; the model is immutable and each `Apply` returns a new `Layout`, so
there is no shared mutable state to race. Refusals are **values, not exceptions** — a refused resize
is an ordinary outcome (`RefusalCode = AIDE-LAYOUT-MIN-SIZE`), not an error path.

**Stable error codes:** `AIDE-LAYOUT-MIN-SIZE`, `AIDE-LAYOUT-LOCKED`, `AIDE-LAYOUT-INVALID-TARGET`,
`AIDE-LAYOUT-SURFACE-UNKNOWN`, `AIDE-LAYOUT-UNREADABLE`, `AIDE-LAYOUT-VERSION-UNSUPPORTED`,
`AIDE-LAYOUT-PARTIAL-RESTORE`.

## Failure-mode analysis

| Failure mode | From which choice | Disposition | How addressed | Test |
|---|---|---|---|---|
| Drag and keyboard diverge | two input paths | **prevent** | Both emit `LayoutOperation`; `Apply` is the only mutation path | `KeyboardAndPointer_ProduceIdenticalTree` |
| Removing the last surface leaves an empty stack | tree mutation | **prevent** | Stack destroyed, parent split collapsed | `RemovingLastSurface_DestroysStackAndCollapsesSplit` |
| A resize starves a sibling to zero | proportional weights | **prevent + detect** | Refused with `AIDE-LAYOUT-MIN-SIZE`, announced | `Resize_BelowMinimum_IsRefused` |
| Restore un-maximizes panes the user had collapsed | maximize implemented as "minimize others" | **prevent** | Maximize records *which* stacks it changed; restore touches only those | `Restore_LeavesDeliberatelyCollapsedStacksCollapsed` |
| Saved layout names a surface that no longer exists | persistence across versions | **recover + detect** | Parked, remaining surfaces placed validly, `AIDE-LAYOUT-PARTIAL-RESTORE` names it | `Restore_WithMissingSurface_ReportsItByName` |
| Layout file unreadable or from a newer schema | own envelope | **recover** | Default arrangement, original preserved as `.bak`, user told | `Restore_Unreadable_FallsBackAndPreservesFile` |
| Floating pane restored onto a disconnected display | multi-monitor | **recover** | Re-homed onto a connected display and reported | `Restore_OffscreenFloating_IsRehomed` |
| Layout locked but a gesture arrives | lock mode | **prevent** | `Apply` refuses with `AIDE-LAYOUT-LOCKED` before any mutation | `Locked_RefusesEveryMutatingOperation` |
| An operation mutates without announcing | announcement channel | **prevent** | Announcement is part of `LayoutResult`; not expressible without one | `EveryAppliedOperation_ProducesAnAnnouncement` |
| Weights drift from 1 after many operations | float arithmetic | detect | Invariant check asserts the sum to tolerance after every op | `TilingInvariant_HoldsAfterOperationSequence` |

## Adversarial analysis (STRIDE-lite)

Phase-1b adds **no new trust boundary** — the layout is local user preference, never repository truth,
never agent-reachable, never egressed. The one relevant threat: **T — a malformed layout file causes
unbounded work or a crash on open**. Mitigated by validating the deserialized tree against the
invariant *before* adopting it and falling back to the default on any failure; tested by
`Restore_Unreadable_FallsBackAndPreservesFile` and a deeply-nested-tree fixture.

Explicit negative: MCP tools cannot read or write the layout, so ADR-0011's egress binding is not in
play here.

## Privacy analysis (LINDDUN-lite)

Surface **titles** may embed a repository path or a branch name (work data). Disposition: **mitigate**
— layouts are local-device only, excluded from telemetry entirely and from workspace export unless
explicitly requested, and layout files are included in the workspace deletion purge. No other personal
data flows through this component.

## Telemetry

Span `aide.workbench.operation` per `Apply`, tagged `operation.kind`, `outcome`
(`applied`/`refused`), `error.code`, `input.mode` (`pointer`/`keyboard` — so the keyboard path's real
usage is measurable rather than assumed), and `tree.depth`. Metric: refusal counts by code — a spike
in `MIN-SIZE` means the minimums are wrong. **No surface titles, no paths.**

## Test plan

Triggered directives: **D0** (hygiene) + **D1** (pure model logic) + **D2** (deserialization over a
wide input domain, incl. malformed) + **D3** (new namespace/composition) + **D4** (filesystem
persistence) + **D7** (the AvalonDock adapter is a substitutable boundary). **A-series does not fire**
— no model call.

Beyond the failure-mode tests above:

- `TilingInvariant_HoldsAfterOperationSequence` — a scripted sequence of 20 mixed operations, asserting
  the invariant after **each** one, not just at the end.
- `KeyboardAndPointer_ProduceIdenticalTree` — the SC 2.5.7 oracle. A drag expressed as a `DropTarget`
  and the keyboard command that names the same target must produce **structurally equal** trees.
- `Envelope_RoundTrips` and `Envelope_RejectsUnsupportedSchemaVersion`.
- `EveryAppliedOperation_ProducesAnAnnouncement` — reflection over the operation union so a **newly
  added operation that forgets its announcement fails the suite**.

## Flagged risks

- The AvalonDock adapter is **not** covered by these tests; it is the Phase-1b spike surface, and the
  four ADR-0012 probes (Accessibility Insights, round-trip, multi-monitor DPI, ganged resize) remain
  open.
- Announcements are tested as *emitted*; whether a real screen reader **speaks** them is an NVDA
  session, not a unit test. Named, not assumed.

## Status and next action

| | |
|---|---|
| **Completed** | Phase-1b design: owned layout model, one-mutation-path command set, envelope, failure modes, test plan. |
| **Remaining** | Implementation; then the AvalonDock adapter and its four spikes. |
| **Best next action** | `/implement` red-first, starting with the tiling invariant and the keyboard/pointer equivalence — the two properties everything else rests on. |

## Gate record

`GATE design · 2026-08-26 · Patterns Expert ⇄ Simplifier (owned model justified by testability, not portability taste); Test Architect (SC 2.5.7 equivalence is a real falsifier; announcement completeness enforced by reflection); UX & Accessibility (every operation announced by construction); Data & Persistence (layout is preference, kept out of the fact store) · verdict: PASS-WITH-CONDITIONS · conditions: adapter spikes remain open; screen-reader verification is a named session, not a unit test · author did not self-clear.`
