---
id: api-aide-core-workbench
title: "API: AiDe.Core.Workbench"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Workbench: 63 types, 119 members, 49% carrying a summary doc comment.
---

# API: `AiDe.Core.Workbench`

**63 public types · 119 public members · 49% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `CanvasFocusOutcome`

*enum* — `CanvasFocusRouter.cs`

Why a focus transition ended the way it did.

## `CanvasFocusDirection`

*enum* — `CanvasFocusRouter.cs`

Which way focus left the canvas.

## `CanvasFocusResult`

*record* — `CanvasFocusRouter.cs`

The result of one focus transition, including the text the user hears on a refusal.

| Member | Summary |
|---|---|
| `bool Succeeded` | **(gap)** |

## `ICanvasFocusTarget`

*interface* — `CanvasFocusRouter.cs`

The canvas, as the focus router needs to see it. Implemented over the WPF `HwndHost`.

**Remarks.** This is a seam rather than a direct dependency because the mechanism is Win32 and the policy is
not: what should happen when the canvas is not ready, or is hidden behind the snapshot swap, is
decidable without a window — and a rule that can only be tested with a real WebView2 running is a
rule that stops being tested.

## `IHostFocusScope`

*interface* — `CanvasFocusRouter.cs`

The host's WPF focus, as the router needs to see it.

## `CanvasFocusRouter`

*class* — `CanvasFocusRouter.cs`

Routes focus across the canvas boundary in **both** directions, explicitly.

**Remarks.** Neither crossing happens by WPF traversal, because traversal does not work here: spike S4
measured `Focus()` refused and Tab never landing on the canvas in *both* hosting modes,
so this is a property of hosting a browser rather than of the mode ADR-0015 chose.





**Every refusal is announced.** A focus command that silently does nothing is
indistinguishable from a broken key (defect class **DC-011**), and this command has two
ordinary reasons to refuse — a canvas that has not been created yet, and one hidden behind the
snapshot swap.

| Member | Summary |
|---|---|
| `bool IsInsideCanvas { get; private set; }` | Whether focus is currently believed to be inside the canvas. |
| `object? PreEntryFocus` | The pre-entry focus target, exposed so a test can assert it was recorded. |
| `CanvasFocusResult Enter()` | `workbench.focusCanvas` — WPF to canvas. |
| `CanvasFocusResult Leave(CanvasFocusDirection direction)` | A `focus.leave` message from the canvas page — canvas to WPF. |

### `CanvasFocusResult Leave(CanvasFocusDirection direction)`

A `focus.leave` message from the canvas page — canvas to WPF.

**Remarks.** The page traps Tab on its last focusable element and Shift+Tab on its first, and posts here.
**These handlers are the only way out**, so a page that forgets them is a keyboard trap —
which is why this is a contract on the page and not a nicety.


Acting on this message moves focus and grants nothing, so a message forged by page
content rather than the boundary handler has no privileged effect.

## `struct`

*record* — `DropTargetResolver.cs`

A rectangle in window coordinates. Deliberately not `System.Windows.Rect` — this
layer stays free of WPF so the drop logic is testable headlessly and survives a shell change.

| Member | Summary |
|---|---|
| `double Right` | **(gap)** |
| `double Bottom` | **(gap)** |
| `bool Contains(LayoutPoint p)` | **(gap)** |

## `struct`

*record* — `DropTargetResolver.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `double Right` | **(gap)** |
| `double Bottom` | **(gap)** |
| `bool Contains(LayoutPoint p)` | **(gap)** |

## `struct`

*record* — `DropTargetResolver.cs`

A pane as the resolver sees it: its id, its bounds, and where its tab strip is.

| Member | Summary |
|---|---|
| `double Right` | **(gap)** |
| `double Bottom` | **(gap)** |
| `bool Contains(LayoutPoint p)` | **(gap)** |

## `DropTargetResolver`

*class* — `DropTargetResolver.cs`

Turns a pointer position into the `DropTarget` a drop would use.

**Remarks.** This is the other half of SC 2.5.7. The keyboard already produces a `DropTarget`;
this makes the pointer produce one too, so both paths converge on the same
`LayoutOperation` and the equivalence test compares two real paths rather than one
path against itself.

It is also what makes "show the destination before release" honest: the preview and the commit
call **this same function**, so they cannot disagree. A preview computed one way and a drop
applied another is the classic source of "it docked somewhere else than the highlight showed".

| Member | Summary |
|---|---|
| `double EdgeFraction = 0.25` | Share of the pane's smaller dimension treated as an edge band. |
| `double MaxEdgeBand = 80` | Upper bound on the edge band, so a huge pane does not get an absurd split zone. |
| `double MinEdgeBand = 16` | Lower bound, so a tiny pane still has a usable split zone. |
| `DropTarget? Resolve(IReadOnlyList<PaneHitBox> panes, LayoutPoint pointer, bool isLocked = false)` | Resolves the destination for a pointer at . |
| `LayoutRect PreviewFor(PaneHitBox pane, DropTarget target)` | The rectangle to highlight for a destination — what the user sees before releasing. |
| `string Describe(DropTarget target, string paneTitle)` | The text announced while a keyboard move hovers this destination. |

### `DropTarget? Resolve(IReadOnlyList<PaneHitBox> panes, LayoutPoint pointer, bool isLocked = false)`

Resolves the destination for a pointer at .

- **`panes`** — Candidate panes, in hit-test order (topmost first).
- **`pointer`** — Pointer position in the same coordinate space as the pane bounds.
- **`isLocked`** — When the layout is locked, no destination is offered at all.

**Returns.** The destination, or  when the layout is locked. A pointer outside every pane resolves to `Float` — dragging a surface out of the window is how every exemplar creates a floating pane, so "no pane here" is a real destination, not a miss.

### `LayoutRect PreviewFor(PaneHitBox pane, DropTarget target)`

The rectangle to highlight for a destination — what the user sees before releasing.

**Remarks.** Derived from the same target the drop will apply, so the highlight cannot promise one
destination while the drop performs another.

## `LayoutMigration`

*record* — `LayoutMigrations.cs`

One step up the schema ladder: transforms a layout written at `FromVersion` into the
shape the next version expects.

**Remarks.** Migrations operate on the **DTO**, not the domain model, deliberately. A migration's whole job is
to read a shape the current domain model can no longer represent; running it through today's
types would defeat the purpose, because those types are exactly what changed.

## `LayoutMigrations`

*class* — `LayoutMigrations.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `IReadOnlyList<LayoutMigration> Default { get; } =` | The shipped migration chain. |
| `LayoutDto AddSurfaceBeside(LayoutDto dto, string anchorSurfaceId, SurfaceDto surface)` | Adds a surface into whichever stack already holds . |
| `LayoutDto RenameSurface(LayoutDto dto, string oldId, string newId)` | Rewrites one surface id throughout a layout, preserving its position and tab order. |
| `LayoutDto RemoveSurface(LayoutDto dto, string surfaceId)` | Drops a surface the current release no longer ships, healing the tree around it. |

### `IReadOnlyList<LayoutMigration> Default { get; } =`

The shipped migration chain.

**Remarks.** The mechanism shipped from day one, because a migration hook added after the first
breaking change is added too late for every layout already on disk. Its first entry was a
worked EXAMPLE — a rename that never happened in the product — which meant the chain looked
exercised while doing nothing, and the first real release that added a surface reached
existing users only if they knew to reset their layout. That example now lives in the test
that documents it; this is the real chain.

### `LayoutDto AddSurfaceBeside(LayoutDto dto, string anchorSurfaceId, SurfaceDto surface)`

Adds a surface into whichever stack already holds .

**Remarks.** **Beside an anchor rather than at a fixed path.** The saved tree is the user's, not
the default's: a stack id computed from the shipped layout may not exist in theirs at all.





**A missing anchor means the migration does nothing.** If the user closed the pane
this one belongs beside, they have said something about that area of the workbench, and
re-opening it under a new name is not an upgrade. A surface already present is left alone, so
the step is safe to re-run.

## `Orientation`

*enum* — `LayoutModel.cs`

*No doc comment on this type.* **(gap)**

## `StackState`

*enum* — `LayoutModel.cs`

How a stack is currently presented. Only `Floating` may overlap.

## `Surface`

*record* — `LayoutModel.cs`

One thing the user works in. Its identity and state are independent of where it is docked.

## `LayoutNode`

*record* — `LayoutModel.cs`

*No doc comment on this type.* **(gap)**

## `SplitNode`

*record* — `LayoutModel.cs`

Divides its region among 2..n children by weights that always sum to 1.

**Remarks.** A split with fewer than two children is not representable: the operations collapse it into its
remaining child. That is what makes "no empty region" structural rather than a rule someone has
to remember.

| Member | Summary |
|---|---|
| `SplitNode(string id, Orientation orientation,` | **(gap)** |
| `Orientation Orientation { get; init; }` | **(gap)** |
| `ImmutableList<LayoutNode> Children { get; init; }` | **(gap)** |
| `ImmutableList<double> Weights { get; init; }` | **(gap)** |

## `StackNode`

*record* — `LayoutModel.cs`

A leaf region holding 1..n surfaces navigated by tabs.

**Remarks.** A stack with zero surfaces is not constructible. Removing the last surface destroys the stack
instead of emptying it — an empty region can never persist because it can never exist.

| Member | Summary |
|---|---|
| `double DefaultMinimum = 120` | **(gap)** |
| `StackNode(string id, ImmutableList<Surface> surfaces, int activeIndex = 0,` | **(gap)** |
| `ImmutableList<Surface> Surfaces { get; init; }` | **(gap)** |
| `int ActiveIndex { get; init; }` | **(gap)** |
| `StackState State { get; init; }` | **(gap)** |
| `double MinWidth { get; init; }` | **(gap)** |
| `double MinHeight { get; init; }` | **(gap)** |
| `LayoutRect? FloatingBounds { get; init; }` | Where a floating pane sits, in virtual-screen coordinates. Null while docked. |
| `Surface Active` | **(gap)** |

### `LayoutRect? FloatingBounds { get; init; }`

Where a floating pane sits, in virtual-screen coordinates. Null while docked.

**Remarks.** Stored because US-9 requires a floating pane to return to the display it was on. Without it
the off-screen guard has nothing to test and a restored floating pane would land wherever the
shell happened to put it.

## `DropKind`

*enum* — `LayoutModel.cs`

Where a move will land. Computed and shown to the user *before* the move commits.

## `DropTarget`

*record* — `LayoutModel.cs`

*No doc comment on this type.* **(gap)**

## `Layout`

*record* — `LayoutModel.cs`

The whole arrangement: a docked tree plus the floating stacks held outside it.

**Remarks.** Floating stacks live outside the tree deliberately. A tree of proportional splits structurally
cannot express an overlap — which is exactly the tiling invariant — so anything permitted to
overlap must not be in it.

| Member | Summary |
|---|---|
| `Layout Default()` | **(gap)** |
| `IEnumerable<LayoutNode> Walk()` | **(gap)** |
| `IEnumerable<StackNode> AllStacks()` | Every stack, docked or floating. |
| `StackNode? FindStackOf(string surfaceId)` | **(gap)** |
| `void AssertInvariant()` | The tiling invariant, checked after every operation rather than asserted once in prose: no empty stack, no under-filled split, weights summing to one, and unique ids. |
| `string Shape()` | Structural equality ignoring generated ids — the oracle for keyboard/pointer equivalence. |

## `LayoutErrorCodes`

*class* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `string MinSize = "AIDE-LAYOUT-MIN-SIZE"` | **(gap)** |
| `string Locked = "AIDE-LAYOUT-LOCKED"` | **(gap)** |
| `string InvalidTarget = "AIDE-LAYOUT-INVALID-TARGET"` | **(gap)** |
| `string SurfaceUnknown = "AIDE-LAYOUT-SURFACE-UNKNOWN"` | **(gap)** |
| `string Unreadable = "AIDE-LAYOUT-UNREADABLE"` | **(gap)** |
| `string VersionUnsupported = "AIDE-LAYOUT-VERSION-UNSUPPORTED"` | **(gap)** |
| `string PartialRestore = "AIDE-LAYOUT-PARTIAL-RESTORE"` | **(gap)** |

## `LayoutOperation`

*record* — `LayoutService.cs`

One arrangement change. **Both the pointer and the keyboard produce these** — that is what makes
"the keyboard path and the drag path produce the same result" (SC 2.5.7) a testable property
rather than a hope.

## `MoveSurface`

*record* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

## `ResizeSplit`

*record* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

## `SetStackState`

*record* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

## `ActivateSurface`

*record* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

## `ReorderSurface`

*record* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

## `CloseSurface`

*record* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

## `AddSurface`

*record* — `LayoutService.cs`

Adds a new surface as a tab in an existing stack.

**Remarks.** Into a STACK rather than a new pane: a second terminal is another tab beside the first, not a
second region competing for the same space. The caller names the stack so the choice of where
stays with whoever knows why.

## `ResetToDefault`

*record* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

## `LayoutResult`

*record* — `LayoutService.cs`

The outcome of an `Apply`. Carries the announcement, so an operation
that mutates the layout without telling assistive technology is not expressible (SC 4.1.3).

## `ILayoutService`

*interface* — `LayoutService.cs`

*No doc comment on this type.* **(gap)**

## `LayoutService`

*class* — `LayoutService.cs`

The single mutation path for the workbench arrangement.

**Remarks.** Pattern: Command + immutable aggregate. Every gesture — pointer drag, keyboard command, palette
entry — is funnelled through `Apply`, which validates, applies, re-checks the tiling
invariant and produces the announcement. Refusals are values, not exceptions: hitting a minimum
size is an ordinary outcome the UI reports, not an error path.

| Member | Summary |
|---|---|
| `Layout Current { get; private set; } = initial ?? Layout.Default()` | **(gap)** |
| `bool IsLocked { get; set; }` | **(gap)** |
| `void Restore(Layout layout)` | **(gap)** |
| `LayoutResult Apply(LayoutOperation operation)` | **(gap)** |

## `LayoutEnvelope`

*record* — `LayoutStore.cs`

The owned persistence envelope (ADR-0013). The payload is ours, not the docking library's,
because `LayoutRootDto` ships no version field — without an envelope there is no way to
tell "written by an older build" from "corrupt", and both would surface as the same failure.

## `LayoutDto`

*record* — `LayoutStore.cs`

Serializable projection of the layout tree. Deliberately dumb: no behaviour, no invariants.

## `NodeDto`

*record* — `LayoutStore.cs`

*No doc comment on this type.* **(gap)**

## `SurfaceDto`

*record* — `LayoutStore.cs`

*No doc comment on this type.* **(gap)**

## `SurfaceAvailability`

*record* — `LayoutStore.cs`

What the application can currently provide, as the restore's reconciliation asks it.

**Remarks.** **Ids alone were wrong.** The shell passed the surface ids present in the DEFAULT
layout, so anything created at runtime — an agent terminal, whose id is
`agent:claude#<guid>` — was unknown at restore and dropped as "no longer available".
The user was told their agent pane was gone, every launch, because the check asked the wrong
question.





**The right question is whether content can be BUILT for it**, which is a property of
the surface's kind. Ids are still honoured for the fixed surfaces. A surface whose kind this
build no longer has is still dropped and still reported — the control keeps firing for the case
it was written for.

| Member | Summary |
|---|---|
| `SurfaceAvailability OfIds(IReadOnlySet<string> ids)` | **(gap)** |
| `bool CanProvide(Surface surface)` | **(gap)** |

## `RestoreResult`

*record* — `LayoutStore.cs`

What a restore actually managed to do — never a silent success.

## `LayoutStore`

*class* — `LayoutStore.cs`

Reads and writes the workbench layout for one workspace.

**Remarks.** The contract US-9 sets is that a layout which cannot be honoured **degrades to the default
arrangement and says so, preserving the original file** — never to a broken window and never to a
silently dropped surface.

| Member | Summary |
|---|---|
| `int CurrentSchemaVersion = 4` | Bumped to 2 when the Joins pane was added. |
| `string BackupPath` | **(gap)** |
| `void Save(Layout layout)` | **(gap)** |
| `RestoreResult Load(` | Restores the layout, reconciling it against the surfaces that actually exist and the displays that are actually connected. |
| `RestoreResult Load(` | **(gap)** |

### `int CurrentSchemaVersion = 4`

Bumped to 2 when the Joins pane was added.

**Remarks.** The version is what makes a shipped surface reach a user who has already arranged their
workbench. Adding a pane to `Default` alone reaches only people with no
saved layout, which is nobody who has used the product.

### `RestoreResult Load(`

Restores the layout, reconciling it against the surfaces that actually exist and the displays
that are actually connected.

- **`availableSurfaces`** — Surface ids the application can currently provide.
- **`displayIsConnected`** — Whether a floating pane's display is still present.

## `TreeToZones`

*class* — `TreeToZones.cs`

Converts a legacy proportional split-tree `Layout` into a `WorkbenchLayout`
of named zones. The Expand step of the ADR-0021 migration: existing saved `layout.json` trees
are read through this so no surface is lost and every surface lands in a deterministic zone (AC-F9).

**Remarks.** Pure and deterministic — the same tree always yields the same zones, which is what lets a golden
`layout.json` fixture pin the conversion. Each tree stack is classified *as a unit* so its
tab grouping survives the move (the graph's document tabs stay together in the Center rather than
scattering). Stacks that map to the same zone are concatenated. Floating stacks carry over unchanged.

| Member | Summary |
|---|---|
| `WorkbenchLayout Convert(Layout tree)` | Converts a tree layout to zones, losing no surface. |

## `WorkbenchCommand`

*record* — `WorkbenchCommands.cs`

One keyboard-reachable layout command, as the command palette lists it.

**Remarks.** This catalog is the machine-checkable form of SC 2.5.7: every operation reachable by dragging
must have a keyboard equivalent. Because both the palette and the conformance test read the same
list, an operation added without a command fails the suite instead of shipping mouse-only.

**Placement is a Core decision that used to live in a Design-owned file.** Adding a command
and putting it in a menu is one atomic change — a conformance test requires every catalog command
to be reachable — so a Core addition forced an edit to `MainMenuBuilder`. Declaring it here lets
the menu builder derive its grouping instead, and the seam stops crossing. Additive with a
default, so nothing breaks before the builder reads it.

## `WorkbenchCommandCatalog`

*class* — `WorkbenchCommands.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `IReadOnlyList<WorkbenchCommand> All { get; } =` | The commands, in palette order. Gestures follow Windows/Fluent conventions and deliberately avoid the Alt+<letter> menu-mnemonic space. |
| `IEnumerable<WorkbenchCommand> Search(string term)` | **(gap)** |

## `KeyboardResizeSession`

*class* — `WorkbenchCommands.cs`

The keyboard resize interaction, modelled after Eclipse's `Alt+-` → Size → arrows — the only
keyboard resize proven in any of the four exemplars.

**Remarks.** It is a small explicit state machine rather than a stream of resize operations because the user
must be able to **see which edge is selected** before moving it, and to **cancel back to where
they started**. Adjustments are applied live so the effect is visible, and Cancel restores the
layout captured on entry.

| Member | Summary |
|---|---|
| `bool IsActive { get; private set; }` | **(gap)** |
| `string? SplitId { get; private set; }` | **(gap)** |
| `int EdgeIndex { get; private set; }` | **(gap)** |
| `double Step { get; init; } = 0.02` | The step per arrow press, as a share of the split. Matches the mockup's declared increments. |
| `string Begin(string splitId, int edgeIndex, string edgeLabel)` | Enters resize mode on an edge and announces which edge is selected. |
| `LayoutResult Adjust(int direction)` | Applies one arrow press. A refusal (minimum size) keeps the session open. |
| `string Commit()` | **(gap)** |
| `string Cancel()` | Abandons the resize and puts the layout back exactly as it was on entry. |
| `string Describe()` | **(gap)** |

## `ZoneBackedLayoutService`

*class* — `ZoneBackedLayoutService.cs`

An `ILayoutService` whose real state is a `WorkbenchLayout` of named zones,
projected to a fixed-shape `Layout` tree for the existing adapter/persistence to render
(ADR-0021). Every tree-shaped operation is translated to a zone-scoped one, so an operation on one
pane changes only the zone(s) it names — the frame cannot "flip" (defect class DC-063). This is the
Strangler that lets the layout logic become zone-based without touching the adapter, controller,
persistence or shell wiring, all of which speak `ILayoutService`.

| Member | Summary |
|---|---|
| `ZoneBackedLayoutService(WorkbenchLayout? initial = null)` | **(gap)** |
| `WorkbenchLayout Zones` | The zone model — the real source of truth behind the projected tree. |
| `void RestoreZones(WorkbenchLayout zones)` | Replaces the whole zone arrangement (used by persistence restore). |
| `Layout Current` | **(gap)** |
| `bool IsLocked { get; set; }` | **(gap)** |
| `void Restore(Layout layout)` | **(gap)** |
| `bool ReconcileFromView(Layout layout)` | Reconciles a native drag from the VIEW's fixed-frame tree by POSITION only. Returns true when it mapped confidently and applied; returns **false without touching the model** when it cannot — so an unmappable drag reve… |
| `LayoutResult Apply(LayoutOperation operation)` | **(gap)** |

## `ZoneId`

*enum* — `ZoneLayout.cs`

The four named, absolute regions of the workbench frame. Unlike the proportional split tree
(`Layout`), these are **stable containers**: the frame never restructures, so an
operation on a pane can only change the zone(s) that pane belongs to — never relocate or reorient
an unrelated pane (defect class DC-063). See `adr-0021-named-dock-zones`.

## `ZoneContent`

*record* — `ZoneLayout.cs`

What a zone holds: either a single tab stack or, within the Center, a split into editor groups.

**Remarks.** The load-bearing rule is that a `ZoneSplit`'s children never leave the zone — a split
is *scoped to its zone*. That is what keeps the top-level frame from being a tree: there is
no operation that restructures the relationship *between* zones. A zone with no content is
represented by a null `Content` (a rail for a tool zone; a placeholder for
the Center), not by an empty stack — an empty `ZoneStack` is not constructible.

| Member | Summary |
|---|---|
| `IEnumerable<Surface> Surfaces()` | Every surface this content holds, in tab/traversal order. |

## `ZoneStack`

*record* — `ZoneLayout.cs`

A tab stack of one or more surfaces. The unit v1 tool zones are built from.

| Member | Summary |
|---|---|
| `ZoneStack(ImmutableList<Surface> surfaces, int activeIndex = 0)` | **(gap)** |
| `ImmutableList<Surface> Tabs { get; init; }` | **(gap)** |
| `int ActiveIndex { get; init; }` | **(gap)** |
| `Surface Active` | **(gap)** |
| `IEnumerable<Surface> Surfaces()` | **(gap)** |

## `ZoneSplit`

*record* — `ZoneLayout.cs`

A split into editor groups, scoped to its zone (Center only in v1). Its children are themselves
zone content, so a group can be a stack or a nested split — but always inside this zone.

| Member | Summary |
|---|---|
| `ZoneSplit(Orientation orientation, ImmutableList<ZoneContent> children, ImmutableList<double> weights)` | **(gap)** |
| `Orientation Orientation { get; init; }` | **(gap)** |
| `ImmutableList<ZoneContent> Children { get; init; }` | **(gap)** |
| `ImmutableList<double> Weights { get; init; }` | **(gap)** |
| `IEnumerable<Surface> Surfaces()` | **(gap)** |

## `ZoneState`

*record* — `ZoneLayout.cs`

One zone's state: what it holds, its cross-axis size relative to the Center, and whether it is
collapsed to a rail. The Center is never collapsed and its `Extent` is ignored (it
takes the remaining space).

| Member | Summary |
|---|---|
| `double DefaultExtent = 0.22` | Default cross-axis extent for a tool zone, as a proportion of the frame. |
| `bool IsEmpty` | **(gap)** |
| `IEnumerable<Surface> Surfaces()` | **(gap)** |

## `MaximizeMemo`

*record* — `ZoneLayout.cs`

The arrangement to restore a maximized zone or pane back to.

## `WorkbenchLayout`

*record* — `ZoneLayout.cs`

The whole workbench arrangement as named zones: a fixed frame plus the floating stacks held
outside it. Replaces the proportional split tree (`Layout`).

**Remarks.** All four zones are **always present** in `Zones` — an empty zone has null content,
it is never removed. That is what makes "the Center is always there" and "moving a pane cannot
delete a zone" structural rather than rules to remember. Floating stacks live outside the frame,
unchanged from the tree model (only docked layout changes in ADR-0021).

| Member | Summary |
|---|---|
| `WorkbenchLayout Default()` | The default arrangement: graph document in the Center, a terminal in the Bottom. |
| `WorkbenchLayout Empty()` | An empty frame — all four zones present, none with content. Used by the converter as a base. |
| `ZoneState Zone(ZoneId id)` | **(gap)** |
| `IEnumerable<Surface> AllSurfaces()` | **(gap)** |
| `ZoneId? FindZoneOf(string surfaceId)` | The zone currently holding , or null if it is floating/absent. |
| `WorkbenchLayout WithZone(ZoneState zone)` | Replaces one zone's state, leaving the other three byte-identical (the containment primitive). |
| `void AssertInvariant()` | The frame invariant, checked after every operation: the four zones exist, the Center is never collapsed, no surface appears twice, and no stack is empty. |
| `string Shape()` | Structural signature ignoring extents — the oracle for "which zone holds what, in what order". Two layouts with the same shape are the same arrangement of panes. |

## `ZoneLayoutResult`

*record* — `ZoneLayoutService.cs`

The outcome of a zone-layout operation, carrying the accessibility announcement (SC 4.1.3).

## `ZoneLayoutService`

*class* — `ZoneLayoutService.cs`

The zone-scoped layout operations. Every operation names a `ZoneId`, and its effect is
confined to that zone (and, for a move, the destination zone) — the other zones come through
reference-identical via `WithZone`. That confinement is the structural
remedy for DC-063: there is no operation that restructures the relationship between zones.

**Remarks.** Pure functions over an immutable `WorkbenchLayout`; no shell/UI dependency.

| Member | Summary |
|---|---|
| `ZoneLayoutResult MovePane(` | Moves a surface into , changing only its source and destination zones. |
| `ZoneLayoutResult OpenPane(WorkbenchLayout layout, Surface surface, ZoneId target)` | Opens a new surface as the active tab of  — destination-local. |
| `ZoneLayoutResult ClosePane(WorkbenchLayout layout, string surfaceId)` | Closes a surface, changing only its own zone. The Center never disappears (becomes empty). |
| `ZoneLayoutResult Activate(WorkbenchLayout layout, string surfaceId)` | Activates a surface's tab in whichever zone holds it. |
| `ZoneLayoutResult CollapseZone(WorkbenchLayout layout, ZoneId zoneId)` | Collapses a tool zone to its rail; its panes are retained (AC-F4). The Center refuses. |
| `ZoneLayoutResult ExpandZone(WorkbenchLayout layout, ZoneId zoneId)` | Re-expands a collapsed tool zone, restoring the same panes and active tab. |
| `ZoneLayoutResult ResizeZone(WorkbenchLayout layout, ZoneId zoneId, double extent)` | Resizes a tool zone. Only that zone changes size; the Center absorbs the difference (AC-F6). |
| `ZoneLayoutResult Maximize(WorkbenchLayout layout, ZoneId zoneId)` | Maximizes a zone (others collapse to rails), snapshotting the arrangement for an exact restore. |
| `ZoneLayoutResult Restore(WorkbenchLayout layout)` | Restores the arrangement captured at the last `Maximize` — exactly (AC-F5). |

## `ZoneEnvelope`

*record* — `ZoneLayoutStore.cs`

*No doc comment on this type.* **(gap)**

## `ZoneLayoutDto`

*record* — `ZoneLayoutStore.cs`

*No doc comment on this type.* **(gap)**

## `ZoneStateDto`

*record* — `ZoneLayoutStore.cs`

*No doc comment on this type.* **(gap)**

## `ZoneContentDto`

*record* — `ZoneLayoutStore.cs`

*No doc comment on this type.* **(gap)**

## `ZoneStackDto`

*record* — `ZoneLayoutStore.cs`

*No doc comment on this type.* **(gap)**

## `ZoneSurfaceDto`

*record* — `ZoneLayoutStore.cs`

*No doc comment on this type.* **(gap)**

## `ZoneLayoutStore`

*class* — `ZoneLayoutStore.cs`

Saves and restores a `WorkbenchLayout` of named zones as JSON (ADR-0021 dz-persist),
preserving what the projected tree cannot: collapsed-zone content, per-zone extent, and exact
placement. Restore filters out surfaces the app can no longer provide, so a saved terminal whose
process is gone (or a surface kind the build dropped) does not resurrect — an empty zone simply
becomes a placeholder, never a broken pane.

| Member | Summary |
|---|---|
| `int CurrentSchemaVersion = 1` | **(gap)** |
| `string FilePath` | **(gap)** |
| `void Save(WorkbenchLayout layout)` | **(gap)** |
| `WorkbenchLayout? Load(IReadOnlySet<string> availableSurfaces, IReadOnlySet<string> restorableKinds)` | Loads the saved zone layout, dropping surfaces that are no longer available. Returns null when there is no file, it cannot be read, or it does not deserialize — the caller then keeps its current arrangement (or the de… |

## `ZonesToTree`

*class* — `ZonesToTree.cs`

Projects a `WorkbenchLayout` of named zones into a **fixed-shape** legacy
`Layout` tree, so the existing AvalonDock adapter, persistence and controller render
zones without change (the Strangler-Fig step of ADR-0021). The projected tree is always the same
frame — `Vertical[ Horizontal[left, center, right], bottom ]` — so rendering it can never
"flip": closing or opening a pane changes only which surfaces a zone's pane holds, never the frame.

**Remarks.** Node ids are deterministic per zone (`zone-left`, `zone-center`, …) so the projection is
stable across renders. Collapsed tool zones are omitted from the tree (their content is retained in
the zone model — the rail visual is a later phase). An empty Center still renders, via a synthetic
welcome placeholder, because the Center is never absent (AC-F3).

| Member | Summary |
|---|---|
| `string CenterStackId = "zone-center"` | **(gap)** |
| `string LeftStackId = "zone-left"` | **(gap)** |
| `string RightStackId = "zone-right"` | **(gap)** |
| `string BottomStackId = "zone-bottom"` | **(gap)** |
| `string ColumnsSplitId = "frame-cols"` | **(gap)** |
| `string RootSplitId = "frame-root"` | **(gap)** |
| `Surface WelcomePlaceholder = new("welcome", "welcome", "Welcome")` | The surface shown when the Center has no documents (kept out of the zone model itself). |
| `Layout ToTree(WorkbenchLayout zones)` | Builds the fixed-shape tree for the current zones. |
| `ZoneId? ZoneOfStackId(string stackId)` | Which zone a projected stack id belongs to, or null for an unknown id. |
