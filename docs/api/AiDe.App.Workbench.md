---
id: api-aide-app-workbench
title: "API: AiDe.App.Workbench"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.App.Workbench: 79 types, 321 members, 69% carrying a summary doc comment.
---

# API: `AiDe.App.Workbench`

**79 public types · 321 public members · 69% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `CanvasFocusTarget`

*class* — `CanvasFocusTarget.cs`

`ICanvasFocusTarget` over the WPF `WebView2`, which is an `HwndHost`.

**Remarks.** **Win32, because the managed route does not exist.** `CoreWebView2Controller.MoveFocus`
is the documented way to hand focus to web content, and the WPF control exposes no controller at
all — established by enumerating its public declared surface, which contains exactly two
focus-related members, both `FocusVisualStyle` (spike S4, finding 6). The API names DO appear
in the assembly's string table, so a grep would have confirmed a design that could not be built.





**The read-back is the contract.** `SetFocus` returns the *previously* focused
window, and its null return is ambiguous between "failed" and "nothing had focus" — so it cannot
distinguish success from failure. `GetFocus` is asked afterwards, and focus landing on a
*descendant* counts, because the browser's own input window is a child of the host.

| Member | Summary |
|---|---|
| `bool IsReady` | **(gap)** |
| `bool IsObscured` | **(gap)** |
| `bool TryFocus()` | **(gap)** |

## `CanvasNodeSelection`

*record* — `CanvasSurface.cs`

The node the canvas has re-rooted on, and the edges in view — for a reader to follow.

## `NodeContextMenuRequest`

*record* — `CanvasSurface.cs`

A right-clicked node, with the type signals that decide which viewers it offers.

## `CanvasRefreshOutcome`

*enum* — `CanvasSurface.cs`

The graph canvas: a windowed WebView2 pane, with focus routed explicitly in both directions.

What a refresh actually did, so a caller can say something true about it.

**Remarks.** **Windowed, not composition (ADR-0015).** The composition control fixes the airspace
limitation and then kills the process with a native access violation when its pane is floated —
a crash is a worse failure than an overlay that is not drawn, so the windowed control is kept and
overlaps are handled by swapping in a still frame.





**The page's boundary handlers are the only way out.** WPF's Tab traversal cannot reach
or leave the canvas, so the page traps Tab on its last focusable element and Shift+Tab on its
first and posts `focus.leave`. A page that forgets them is a keyboard trap — which is why
this is a contract on the page rather than a nicety, and why `P2-FOCUS-03` exists.

**Why this is a return value and not a void.** The shell used to announce
*"Graph centred on X"* and then start the refresh fire-and-forget. Two independent things
could make that sentence false: the refresh opens with `if (!Ready) return;` and silently
does nothing while the WebView2 is still initialising, and a discarded task's fault is observed
by nobody. Measured by the design session on a real surface outside a window: `Ready` false,
the task completed, the graph source was asked **0** times — and the user had already been
told the graph centred on a node it never looked up.





A statement made BEFORE an action, about an action that may not happen, cannot be repaired
by wording. The refresh has to report, and the caller has to speak from the report.

## `struct`

*record* — `CanvasSurface.cs`

The outcome of a refresh, with the label to use when speaking about it.

## `CanvasSurface`

*class* — `CanvasSurface.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `CanvasSurface(string surfaceId, string title)` | **(gap)** |
| `string SurfaceId { get; }` | **(gap)** |
| `ICanvasFocusTarget FocusTarget { get; }` | The focus seam the router drives. Non-null from construction. |
| `bool Ready { get; private set; }` | True once the page has loaded and can accept focus. |
| `event EventHandler<CanvasFocusDirection>? FocusLeaveRequested` | Raised when the page reports that focus should leave the canvas. |
| `Func<string?, CancellationToken, Task<CanvasGraph>>? GraphSource { get; set; }` | Supplies the graph to draw. Null until a workspace attaches, in which case the page says so. |
| `event EventHandler<CanvasNodeSelection>? NodeSelected` | Raised when the canvas re-roots on a specific node (a user activation), so a host — the Explorer reader (design D3) — can show that node without the graph and the reader keeping two definitions of "what is selected". … |
| `event EventHandler<NodeContextMenuRequest>? NodeContextMenuRequested` | Raised when a node is right-clicked, so the host can show the contextual "Open as…" menu. |
| `Task<CanvasRefresh> RefreshAsync(` | **(gap)** |
| `void SetObscured(bool obscured)` | Shows a still frame in place of the live canvas for the duration of a drag (ADR-0015). While set, the canvas refuses focus and says why. |
| `Task<bool> SendKeyAsync(string key, int windowsVirtualKeyCode, bool shift = false)` | Sends a key to the page through the browser's own input layer. |
| `Task<string> EvaluateAsync(string script)` | Runs script in the page and returns its result. Used by tests to diagnose input. |
| `void Dispose()` | **(gap)** |

### `Task<bool> SendKeyAsync(string key, int windowsVirtualKeyCode, bool shift = false)`

Sends a key to the page through the browser's own input layer.

**Remarks.** Uses the DevTools `Input.dispatchKeyEvent` rather than `SendInput`, because
`SendInput` delivers to the FOREGROUND window and neither a test host nor a probe
launched from a non-interactive shell can reliably hold it — measured: the page reported
`activeElement="first"`, so focus had landed, while seeing **zero** Tab keydowns.





**What this is and is not.** CDP injects at the renderer's input layer, so the page
and the browser's own focus traversal both see an ordinary key — which is what the
keyboard-trap contract is about. It does not exercise the OS→browser hop, so it cannot catch
a regression where the host swallows the key before the browser sees it. That gap is stated
rather than papered over.

## `ClassDiagramSurface`

*class* — `ClassDiagramSurface.cs`

The class-diagram surface (spec-uml-erm-surfaces; ADR-0020 Phase 1): a dependency-free, native WPF
render of the class HIERARCHY derived from the graph — classes and interfaces as cards, each showing
its generalizations (`inherits`) and realizations (`implements`). Member-less by construction (no
extractor emits members yet); the header says so rather than implying empty classes. No WebView2, so
none of ADR-0015's airspace concerns. A member-bearing, notation-valid Mermaid render is Phase 2
(gated on Core `has_member`).

| Member | Summary |
|---|---|
| `event EventHandler<NodeContextMenuRequest>? NodeContextMenuRequested` | Raised when a type box is right-clicked, so the host can show the contextual "Open as…" menu. |
| `Func<string, Task<(IReadOnlyList<string> Members, int Declared)>>? MembersSource { get; set; }` | Fetches a type's declared members (attributes + operations) for its UML compartment — the shell wires this to the workspace's `DescribeAsync`. Null leaves the compartment as a pending marker. |
| `int MembersRequestedCount` | **(gap)** |
| `ClassDiagramSurface(string title = "Class diagram")` | **(gap)** |
| `int TypeCount { get; private set; }` | The number of type cards currently shown (for tests). |
| `int RelationCount { get; private set; }` | The number of generalization/realization relations currently shown (for tests). |
| `bool IsEmpty` | **(gap)** |
| `void ShowGraph(IReadOnlyList<CanvasNode>? nodes, IReadOnlyList<CanvasEdge>? edges)` | Builds the hierarchy from a graph and renders it (ADR-0020). |
| `void Show(ClassHierarchy hierarchy)` | Stores and renders a prebuilt hierarchy (search re-renders a filtered view of it). |
| `void Clear()` | **(gap)** |
| `void ShowLoading()` | Shows a loading state while the graph is fetched (U9 state completeness). |
| `void ShowError(string message)` | Shows an explicit error state — never a misleading empty state — when the graph load fails. |

## `ClassRelationKind`

*enum* — `ClassHierarchyModel.cs`

A UML relationship kind we derive from the graph (ADR-0020).

## `ClassTypeNode`

*record* — `ClassHierarchyModel.cs`

A type in the class diagram — a class or interface. Members are not available yet (ADR-0020).

## `ClassRelation`

*record* — `ClassHierarchyModel.cs`

One generalization/realization edge between two types in the diagram.

## `ClassHierarchy`

*record* — `ClassHierarchyModel.cs`

The class-hierarchy view model (ADR-0020): the classes/interfaces and their generalization/
realization relationships, projected from the graph the App already holds. A pure function so the
projection is verifiable headlessly. Member-less by construction — no extractor emits members yet;
the surface says so rather than implying empty classes.

| Member | Summary |
|---|---|
| `bool IsEmpty` | **(gap)** |
| `IReadOnlyList<ClassRelation> Deps` | UML dependency edges (`depends_on`), kept separate from the inheritance relations so they never affect the generalization ranking/layout; drawn only when the user asks. |

## `ClassHierarchyModel`

*class* — `ClassHierarchyModel.cs`

Builds a `ClassHierarchy` from graph nodes and edges (ADR-0020 Phase 1).

| Member | Summary |
|---|---|
| `ClassHierarchy Build(` | Projects the class hierarchy. Keeps only class/interface nodes; keeps `inherits` (generalization) and `implements` (realization) edges whose BOTH endpoints are kept types (the internal hierarchy); counts relations who… |
| `ClassHierarchy Filter(ClassHierarchy hierarchy, string? term)` | Filters a hierarchy to types whose label contains  (case-insensitive), keeping only relations whose BOTH endpoints survive; relations to a filtered-out (or external) target are recounted as external. An empty/whitespa… |
| `IReadOnlyList<ClassRelation> DeriveAssociations(` | Derives UML association/aggregation relations from members: a field or property whose declared type — or, for a collection, its element type — matches a drawn type is a structural "has-a". A collection type is an aggr… |

## `CodeViewerView`

*class* — `CodeViewerView.cs`

The read-only code viewer (spec-editor-surfaces US-ED1–ED4; ADR-0019). A native AvalonEdit
`TextEditor` in read-only mode with syntax highlighting picked from the content's
language tag — a pure WPF control, so none of ADR-0015's WebView2 airspace concerns. Renders a
`NodeContent`: code (highlighted), text (plain), a shortfall banner when the content was
bounded (US-ED3), and an honest fallback when there is no inline content (US-ED8).

| Member | Summary |
|---|---|
| `CodeViewerView(string title = "Source")` | **(gap)** |
| `string? NodeId { get; private set; }` | The id of the node whose content is shown, or null when empty/cleared. |
| `string? HighlightingName` | The AvalonEdit highlighting currently applied, or null (plain). For tests. |
| `bool IsFallback` | Whether the viewer is showing the no-inline-content fallback. |
| `string ShownText` | The read-only text currently shown (empty in the fallback state). For tests. |
| `void Show(NodeContent content)` | Renders a node's content. RenderKind decides the branch (US-ED2/ED8). |
| `void Clear()` | **(gap)** |

## `CommandPalette`

*class* — `CommandPalette.cs`

The keyboard route to every layout command.

**Remarks.** This is the mechanism US-9 names for SC 2.5.7: "an equivalent command exists and is reachable
from the command palette". Without it the catalog is a list nobody can invoke — the conformance
test would still pass while the product remained mouse-only, which is exactly the gap between
*tested* and *usable* that the criterion exists to close.

Focus handling is the load-bearing part. Opening moves focus into the search box deliberately
(the user asked for it); closing **restores focus to wherever it was**, so invoking a command
never strands a keyboard user somewhere they did not choose (SC 2.4.3).

| Member | Summary |
|---|---|
| `CommandPalette(WorkbenchController controller, IWorkbenchAnnouncer announcer)` | **(gap)** |
| `Border Root { get; }` | **(gap)** |
| `TextBox SearchBox { get; }` | **(gap)** |
| `ListBox Results { get; }` | **(gap)** |
| `bool IsOpen` | **(gap)** |
| `IReadOnlyList<WorkbenchCommand> Visible` | The commands currently listed — what a test and the UI both read. |
| `void Open()` | **(gap)** |
| `void Close()` | **(gap)** |
| `bool InvokeSelected()` | Runs the selected command and closes. Returns false when nothing is selected. |
| `bool HandleKey(Key key)` | Handles palette keys. Returns true when the key was consumed. |

## `ContextMapSurface`

*class* — `ContextMapSurface.cs`

The context map: contexts as boxes, and the traffic between them.

**Remarks.** **The crossing count is the point.** A context map that only names contexts is a
picture of a decision; the number of edges leaving each one is the evidence for whether that
decision held. A context with no crossings is isolated and a context with hundreds is not
bounded, and neither is visible from a list of names.





**An invalid map renders its problems, not a partial diagram.** A context map drawn
from a file that failed validation is wrong in a way nobody can see, which is worse than one
that refuses and says why.

| Member | Summary |
|---|---|
| `ContextMapSurface(string title)` | **(gap)** |
| `Func<ContextMapView>? Source { get; set; }` | Supplies the view. Null until a workspace with a context map attaches. |
| `event EventHandler<string>? ContextSelected` | Raised when a context box is chosen, so another surface can show only that context. |
| `void Refresh()` | **(gap)** |

## `CoreNodeContentSource`

*class* — `CoreNodeContentSource.cs`

The real content source: Core's `NodeContentAsync`, behind the client seam.

**Remarks.** **The substitution the seam was built for.** `MockNodeContentSource` was
written to stand in "until Core ships `NodeContentAsync`" — and Core shipped it, after which
nothing swapped the field, so the code viewer went on showing a labelled SAMPLE against a fully
indexed workspace. A stand-in is only honest while the thing it stands in for is missing; once it
arrives, the stand-in is a defect that looks exactly like a feature.





**It translates, it does not decide.** The render kind and the language come from the
authority; this maps Core's enum to the client mirror and nothing else. A client that inferred
"this looks like C#" from the id would be a second authority on what a node contains, disagreeing
with the first the moment one resolved a path differently (DC-022) — the same reason the App does
not read workspace files at all.





**An unknown kind degrades to `None`.** If Core adds a render
kind this build has never heard of, the viewer falls back to metadata and edges — which is what it
does for a diagram or a binary. Guessing `Code` would put unhighlighted, possibly binary text
in a syntax-highlighted control and claim it was source.

| Member | Summary |
|---|---|
| `Task<NodeContent> GetAsync(` | **(gap)** |

## `DiagnosticsReport`

*record* — `DiagnosticsSurface.cs`

The report a `DiagnosticsSurface` renders. Built by the shell from the last re-index
(`IndexSummary`, its disclosures folded by `DisclosureSummary.Fold`) plus the daemon
diagnostics. A plain record so the surface is verifiable headlessly.

| Member | Summary |
|---|---|
| `bool HasIndex` | **(gap)** |

## `DiagnosticsSurface`

*class* — `DiagnosticsSurface.cs`

The workspace Diagnostics pane: the re-index analysis coverage (folded "not analysed" disclosures,
grouped by category with the counts summed) and the daemon state — the browsable home for what was
a 200-line wall in the one-line status strip. Host-side WPF, so it renders on an STA thread.

| Member | Summary |
|---|---|
| `DiagnosticsSurface(string title = "Diagnostics")` | **(gap)** |
| `void ShowLoading()` | **(gap)** |
| `void ShowError(string message)` | **(gap)** |
| `void ShowEmpty()` | **(gap)** |
| `void Show(DiagnosticsReport report)` | **(gap)** |

## `DiagramZoom`

*class* — `DiagramZoom.cs`

Pure pan/zoom math for the class-diagram surface (smoke 9-1 Phase D) — cursor-anchored zoom,
testable off the UI thread. The surface applies the result to a `ScaleTransform` on the
diagram canvas and to the `ScrollViewer` offsets.

| Member | Summary |
|---|---|
| `double Min = 0.3` | **(gap)** |
| `double Max = 3.0` | **(gap)** |
| `double NextScale(double current, int wheelDelta)` | One wheel notch's next zoom level, clamped to [`Min`, `Max`]. |
| `double Reanchor(double oldScale, double newScale, double oldOffset, double cursorViewport)` | The scroll offset that keeps the point under the cursor fixed as the scale changes. With a LayoutTransform on the canvas the ScrollViewer's extent is in scaled pixels, so the content point under the cursor is `(offset… |

## `DockRoundedTabs`

*class* — `DockRoundedTabs.cs`

Rounds the AvalonDock document tab tops to the facelift's soft feel.

**Remarks.** The rounded style in `DockRoundedTabs.xaml` is the VS2013 theme's own
`LayoutDocumentTabItem` template (extracted from the assembly, so the drag, selection and
close-command bindings are the theme's real ones, not a guess) with three changes: the header
gets `CornerRadius="7,7,0,0"`, its bottom border line is dropped, and the two serialization
artifacts `XamlWriter` leaves behind — a null `Content` and a black foreground on the
title — are removed so the title shows in the palette's text colour.





The workbench docks every surface in a `LayoutDocumentPane`, so all its tabs are
`LayoutDocumentTabItem` (a `ContentControl`), which is
why a plain `ContentPresenter` shows the title safely. Merged AFTER the theme so the
implicit style wins; the accent/surface retokenisation (`DockThemeAccents`) still
supplies the tab background brushes this template binds to.

| Member | Summary |
|---|---|
| `void Apply(DockingManager manager)` | **(gap)** |

## `DockThemeAccents`

*class* — `DockThemeAccents.cs`

Retokenises the AvalonDock VS2013 dark theme to the app palette — the VS-blue accent to our
accent, and the theme's background/border grays to our surface/border tokens.

**Remarks.** **By value, not by key name.** The theme's accent is the VS-blue family
(`#007ACC` and its hover/pressed tints), spread across ~30 component resource keys
(`DocumentWellTabSelectedActiveBackground`, `ToolWindowCaptionActiveBackground`,
`ControlAccentBrushKey`, …). Rather than name each key — which risks missing one and
leaving a stray blue — this recolours every themed brush whose *colour* is in that
family. The key set was established by enumerating a themed `DockingManager` at
runtime, not guessed (E15).





**No template surgery.** The overrides are written as DIRECT entries into the manager's
resources, which take precedence over the same keys in its merged theme dictionaries, so the
tab and caption templates' `DynamicResource` lookups resolve to ours. Document-tab corners
stay square — that is the IDE convention (VS / VS Code / JetBrains) and rounding them would need
the fragile template surgery this deliberately avoids. See
`docs/notes/avalondock-tab-styling-decision.md`.

| Member | Summary |
|---|---|
| `int Retokenise(DockingManager manager)` | Overrides the theme's accent brushes on  and returns how many were retokenised. Safe to call once, after the theme is applied. |

## `DocumentPlacement`

*record* — `DocumentPlacement.cs`

Where a new reference-document surface (class diagram, code viewer) should open. A document must
never be tabbed on top of the graph/canvas — that hides the graph, which is the surface the user
is almost always working *from* (the "my graph pane disappeared" defect). So the policy is: tab
into the focused document stack, else any existing document stack, else split a new stack BESIDE
the graph so both stay visible.

| Member | Summary |
|---|---|
| `bool IsSplit` | **(gap)** |

## `DocumentPlacementPolicy`

*class* — `DocumentPlacement.cs`

Pure placement policy for reference-document surfaces, so it is verifiable headlessly.

| Member | Summary |
|---|---|
| `DocumentPlacement? Decide(Layout layout, string? activeSurfaceId)` | **(gap)** |

## `ExplorerLayout`

*enum* — `ExplorerSurface.cs`

How the Explorer arranges its two panes for the available width (US-E8).

## `ExplorerSurface`

*class* — `ExplorerSurface.cs`

The full-window Explorer surface (spec-knowledge-explorer-mode; design D2): a graph region and a
reader region split by a draggable gutter. The graph is a dedicated `CanvasSurface`
(its own instance — the workbench's canvas is never reparented across visual trees), and the reader
follows the graph's selection through the `NodeSelected` seam (D3), while
activating a reader edge walks the graph (US-E4/E5). Created once and retained by the mode
controller, so a round-trip does not rebuild it.

**Remarks.** **Responsive (US-E8).** Above `StackBelowWidth` the panes sit side by side; below it
they stack (graph over reader), so both halves stay usable on one narrow single-monitor window
rather than the reader being squeezed to its minimum. The layout is recomputed on size change and
is a pure function of width, so it is testable without rendering.

| Member | Summary |
|---|---|
| `ExplorerSurface(CanvasSurface graph, NodeReaderView reader)` | **(gap)** |
| `CanvasSurface Graph { get; }` | **(gap)** |
| `NodeReaderView Reader { get; }` | **(gap)** |
| `double StackBelowWidth { get; set; } = 760` | The width below which the panes stack instead of sitting side by side (US-E8). |
| `ExplorerLayout Layout { get; private set; } = ExplorerLayout.SideBySide` | The current arrangement of the two panes. |
| `Func<bool>? ReturnFocusToGraph { get; set; }` | The action that returns focus from the reader to the graph, completing the cycle. Defaults to focusing the graph canvas; replaceable so the routing is testable without a live WebView2. |
| `void ApplyLayoutForWidth(double width)` | Chooses the layout for a given available width and applies it if it changed. Pure function of width (a width of 0 — before first measure — keeps the side-by-side default). Public so the responsive rule is testable wit… |

## `IHasDisplayName`

*interface* — `IHasDisplayName.cs`

A surface content element that carries a user-chosen display name, distinct from the model's
title. The adapter reads it when projecting a pane's tab caption, so a rename applied to a live
session persists across re-renders (which reuse the same content instance, DC-029) without a
change to the Core layout model. A null or empty name means "use the model title".

## `JoinSurface`

*class* — `JoinSurface.cs`

The joins: where code, schema and infrastructure meet, and how well each meeting is established.

**Remarks.** **This projection existed and nobody could see it.** `JoinProjection` was written,
tested and never called by the running application — a control that cannot fire, in the shape
that matters most here, because the joins are the whole reason the extractors read three
different artifact kinds.





**Verified and Inferred are separated, not sorted.** A ranked list mixes them, and a
user reading top-down acts on an inferred join believing it was checked. They are rendered under
their own headings with the basis on every row, because "why do you believe this" is the only
question worth asking of a join.





**What could not be joined is stated.** A disclosure is the reason a join is missing —
a SQL resource whose name is an expression nobody evaluated, for one — and a joins view that
showed only what it found would read as completeness.

| Member | Summary |
|---|---|
| `JoinSurface(string title)` | **(gap)** |
| `Func<JoinResult>? Source { get; set; }` | Supplies the joins. Null until a workspace attaches. |
| `event EventHandler<string>? NodeSelected` | Raised when a join's endpoint is chosen, so the graph can centre on it. |
| `void Refresh()` | **(gap)** |

### `event EventHandler<string>? NodeSelected`

Raised when a join's endpoint is chosen, so the graph can centre on it.

**Remarks.** A pane that names a symbol the canvas can already draw, and leaves the user to retype it into
a search box, is two tools that happen to share a window.

## `LayoutPersistence`

*class* — `LayoutPersistence.cs`

Keeps the workbench arrangement on disk across restarts (US-9).

**Remarks.** Everything this needs already existed — the store, the envelope, the migration chain, the
partial-restore reporting — and none of it had a production caller, so the layout was never
actually saved or loaded by the running app. This is the wiring that makes "close the
application, reopen the workspace, my arrangement returns" true rather than merely tested.

Saving is **debounced**: a resize drag produces an operation per arrow press or per mouse-move,
and writing the file on each one would turn a smooth drag into a stutter of disk writes.

| Member | Summary |
|---|---|
| `LayoutPersistence(` | **(gap)** |
| `RestoreResult? LastRestore { get; private set; }` | The last restore's outcome — what to announce, and what could not be honoured. |
| `RestoreResult Restore()` | Loads the saved arrangement, or the default when there is none or it cannot be honoured. |
| `void MarkDirty()` | Schedules a save. Repeated calls within the debounce window collapse into one write. |
| `void SaveNow()` | Writes immediately — used on shutdown, where a pending debounce would be lost. |
| `void Dispose()` | **(gap)** |

### `LayoutPersistence(`

- **`restorableKinds`** — Surface kinds the shell can build content for. Surfaces CREATED at runtime — an agent terminal, for one — have ids that no fixed list can contain, so without this they were dropped on every restart and announced as no longer available.

## `NodeContentKind`

*enum* — `NodeContentSource.cs`

How a node's content should be rendered (the client mirror of ADR-0018's RenderKind). The authority
(Core's future `NodeContentAsync`) decides this, so the viewer's per-kind branch is data, not a
client guess.

## `NodeContent`

*record* — `NodeContentSource.cs`

One node's content for the reader/viewer — the client mirror of ADR-0018's `NodeContent`. Bounded:
oversized content returns a `Shortfall` ("first N — open the source"), never an oversized
frame. `Language` is the authority's language tag (e.g. "csharp"), used to pick highlighting.

## `INodeContentSource`

*interface* — `NodeContentSource.cs`

The client seam the reader uses to fetch a selected node's content on demand (ADR-0018). Core's
future `IWorkspaceQueries.NodeContentAsync` is the real implementation; until it ships,
`MockNodeContentSource` stands in behind this interface so the viewer is buildable and
testable and the eventual wiring is a one-line substitution, not a redesign.

## `MockNodeContentSource`

*class* — `NodeContentSource.cs`

A stand-in content source until Core ships `NodeContentAsync` (ADR-0018 Phase 1). It returns a
clearly-labelled SAMPLE — it does NOT read files (the App is not a second file-content authority,
DC-022) — so the viewer can render and be tested end-to-end while the real query is built.

| Member | Summary |
|---|---|
| `Task<NodeContent> GetAsync(string nodeId, CancellationToken cancellationToken = default)` | **(gap)** |

## `NodeReaderView`

*class* — `NodeReaderView.cs`

The reader half of the Explorer surface (spec-knowledge-explorer-mode US-E4; design D4). Phase 1
renders a selected node's header, metadata and its walkable typed edges. The per-kind CONTENT view
(rendered markdown/html, syntax-highlighted code) arrives in Phase 2 behind the node-content
contract (ADR-0018), so the content area is an honest placeholder until then — never a blank. With
no selection it shows an explicit empty state (US-E7).

| Member | Summary |
|---|---|
| `NodeReaderView()` | **(gap)** |
| `string? SelectedNodeId { get; private set; }` | The id of the node currently shown, or null when empty. |
| `bool IsEmpty` | **(gap)** |
| `int WalkableEdgeCount { get; private set; }` | How many typed edges (walk targets) the reader is currently offering. |
| `void OnWalk(Action<string> walk)` | Registers the walk handler; called with the target id when an edge is activated. |
| `event EventHandler<CanvasFocusDirection>? FocusLeaveRequested` | Raised when keyboard focus should leave the reader and return to the graph, so the Explorer can complete the graph↔reader cycle (spec US-E7/E8). The direction says which boundary was crossed (Forward = Tab off the las… |
| `IReadOnlyList<UIElement> FocusStops` | The reader's ordered focus stops: the region itself (the entry) followed by its walkable edge buttons. Exposed so the cycle boundary is testable without a rendered visual tree. |
| `CanvasFocusDirection? BoundaryLeave(object? focused, bool shift)` | Given the focused element and whether Shift is held, returns the direction focus should leave the reader — or null when the Tab stays inside the reader. Shift+Tab at the first stop leaves Backward; Tab at the last sto… |
| `bool HandleTabKey(object? focused, bool shift)` | Handles a Tab keypress at the reader boundary: raises `FocusLeaveRequested` and returns true (the caller marks the event handled) when the Tab crosses a boundary; false when it stays inside the reader. |
| `bool FocusReader()` | Moves keyboard focus into the reader region so a Tab off the graph canvas lands here rather than being swallowed by the canvas's keyboard trap (design D3/Phase-3 interim). From the reader — a normal WPF region — Tab t… |
| `bool FocusReaderLast()` | Moves keyboard focus to the reader's LAST stop — used when the graph is left Backward (Shift+Tab off the graph's first node), so the cycle lands the user on the reader's end rather than its start (spec US-E7/E8 cycle). |
| `void Clear()` | **(gap)** |
| `void Show(CanvasNode node, IReadOnlyList<CanvasEdge> edges)` | **(gap)** |

## `NodeViewKind`

*enum* — `NodeViewMenu.cs`

Which viewer an "Open as…" action opens for a node.

## `NodeViewOption`

*record* — `NodeViewMenu.cs`

One contextual "Open as…" choice for a node — the verb and its menu label.

## `NodeViewMenu`

*class* — `NodeViewMenu.cs`

The IntelliJ-style contextual "Open as…" grammar (smoke 9-1 §3): a diagram/viewer is a *view*
opened from an entry point in the model, not a thing created blind. Right-clicking any node offers
exactly the viewers its TYPE supports — source and a class diagram for a type, a sequence diagram
for a method, "read" for a document.

**Remarks.** Type-driven from the producer's signal (`has_type` kind + the authoritative `IsKnowledge`
flag), never a spelling guess that fails across repositories — the DC-042 lesson, just fixed for
the Knowledge chip. Pure and dependency-free so the mapping is unit-tested off the UI thread.

| Member | Summary |
|---|---|
| `IReadOnlyList<NodeViewOption> OptionsFor(string? nodeKind, bool isKnowledge)` | The viewers this node supports, most specific first. Never empty. |

## `PromptBar`

*class* — `PromptBar.cs`

Stages a prompt and dispatches it to the focused terminal, reporting the receipt.

**Remarks.** **The receipt is the point, not the send.** A prompt delivered to an agent session is a
side effect that cannot be undone by the product, so what the user is shown is the recorded
outcome — including `DeliveryUnknown`, which means the write happened
but nothing survived to say whether it landed. Reporting that honestly is the whole reason the
two-phase receipt exists (ADR-0010); a UI that said "sent" would be inventing the half the
protocol deliberately refuses to guess.





**Enter dispatches, Escape cancels, and dispatch is disabled while one is in flight.** A
second Enter during the round trip would produce a second command id and therefore a second
prompt — the idempotency key protects a RETRY of the same command, not a user pressing twice.

| Member | Summary |
|---|---|
| `PromptBar(IWorkbenchAnnouncer announcer)` | **(gap)** |
| `Border Root { get; }` | **(gap)** |
| `TextBox Input { get; }` | **(gap)** |
| `TextBlock Status { get; }` | **(gap)** |
| `bool IsOpen` | **(gap)** |
| `Func<string, Task<DispatchReceipt>>? Dispatch { get; set; }` | Performs the dispatch. Null until a workspace attaches — the bar opens and refuses, rather than being hidden, so the chord never produces silence (**DC-011**). |
| `void Open()` | **(gap)** |
| `void Close()` | **(gap)** |
| `bool HandleKey(Key key)` | Handles a key while the bar is open. Returns true when the key was consumed. |

## `PromptDraftStore`

*class* — `PromptDraftStore.cs`

Persists prompt-draft bodies (spec-editor-surfaces US-ED5) keyed by the stable layout
`SurfaceId`, in a JSON sidecar beside the layout. Mirrors `TerminalCustomizationStore`:
it lives off the Core layout model deliberately, so it needs no schema change, and it is
best-effort — a missing or corrupt sidecar starts clean, and a failed write never crashes the UI.

| Member | Summary |
|---|---|
| `PromptDraftStore(string path)` | **(gap)** |
| `bool TryGet(string surfaceId, out string? body)` | **(gap)** |
| `void Save(string surfaceId, string body)` | **(gap)** |

## `PromptDraftSurface`

*class* — `PromptDraftSurface.cs`

The prompt-draft surface (spec-editor-surfaces US-ED5–ED8): a staged compose pane whose draft is
never sent by editing (US-ED5), and whose one explicit **Transfer** delivers it to a chosen
ready terminal session (US-ED6) one-way (US-ED7). The transfer rules live in
`PromptDraftViewModel`; this control renders them and persists the body across restart
via an injected save (US-ED5). The shell calls `Configure` after render to supply the
live ready-target list and the dispatch, exactly as it wires the canvas graph source.

| Member | Summary |
|---|---|
| `PromptDraftSurface(string surfaceId, string title)` | **(gap)** |
| `string SurfaceId { get; }` | **(gap)** |
| `string Body` | The staged body (for tests / persistence). |
| `bool Transferred` | True once transferred (one-way). |
| `void Configure(` | Wires the surface to the shell: the live ready-target list, the dispatch, the initial body (restored from persistence), and the save callback. Rebuilds the view-model around them. |
| `void RefreshTargets()` | Re-reads the live ready targets into the picker (call when sessions change). |

## `PromptTarget`

*record* — `PromptDraftViewModel.cs`

A terminal session a prompt draft can be transferred to (US-ED6): its id and display name.

## `PromptDraftViewModel`

*class* — `PromptDraftViewModel.cs`

The testable core of the prompt-draft surface (spec-editor-surfaces US-ED5–ED7). Holds the staged
draft body and the transfer rules — drafting never sends (US-ED5); transfer requires a ready target
and a non-empty body, names its target, and is one-way (US-ED6/ED7). The UI (`PromptDraftSurface`)
binds this; the dispatch and the live target list are injected so the rules are unit-testable
without a terminal.

| Member | Summary |
|---|---|
| `PromptDraftViewModel(` | **(gap)** |
| `string Body` | The staged prompt text. Editing it never sends anything (US-ED5). |
| `string? SelectedTargetId { get; set; }` | The chosen target session id, or null to use the first ready target. |
| `bool Transferred { get; private set; }` | True once the draft has been transferred: the session owns it thereafter (US-ED7). |
| `IReadOnlyList<PromptTarget> Targets` | The live ready targets (US-ED6). |
| `bool HasReadyTarget` | Whether at least one session is ready to receive a transfer. |
| `bool CanTransfer` | Transfer is allowed only with a ready target, a non-empty body, and not already sent. |
| `string BlockedReason` | Why transfer is blocked, for the disabled control's stated reason (never a silent no-op). |
| `event EventHandler? Changed` | Raised when the body or transfer state changes, so the UI can re-render. |
| `Task<bool> TransferAsync()` | Transfers the draft to the selected (or first) ready session. One-way: on success the draft is marked transferred and cannot be sent again. A failed dispatch leaves it un-transferred so the user can retry. Returns whe… |

### `PromptDraftViewModel(`

- **`readyTargets`** — The LIVE set of ready sessions — read on demand, never cached, so a session becoming ready or going away is reflected (the workbench mutates under the draft).
- **`dispatch`** — Delivers (targetSessionId, body) and reports whether it was accepted.

## `RelayCommand`

*class* — `RelayCommand.cs`

A minimal always-executable `ICommand` for wiring a button to an action.

| Member | Summary |
|---|---|
| `event EventHandler? CanExecuteChanged` | **(gap)** |
| `bool CanExecute(object? parameter)` | **(gap)** |
| `void Execute(object? parameter)` | **(gap)** |

## `SearchResultKind`

*enum* — `SearchModel.cs`

What kind of thing a search hit points at. Governs grouping order in the results list.

## `SearchResult`

*record* — `SearchModel.cs`

One breadth-search hit. `Id` is opaque and belongs to the provider (a node id, a
file path, a command id) — the surface hands it back verbatim to the navigate action so the
provider decides what "go there" means.

## `SearchGroup`

*record* — `SearchModel.cs`

A named, ordered bucket of hits of one kind, for a grouped results view.

## `SearchModel`

*class* — `SearchModel.cs`

Pure logic for the breadth-search surface (app-search-breadth): grouping hits by kind in a stable
order so the results list reads the same way every time, independent of the order the provider
returned them.

**Remarks.** **Scaffold.** The hits themselves come from a Core search index that does not exist yet; this
model shapes whatever a provider returns. Kept pure and dependency-free so it is unit-testable
off the UI thread, mirroring `SequenceModel` and `ClassHierarchyModel`.

| Member | Summary |
|---|---|
| `IReadOnlyList<SearchGroup> Group(IReadOnlyList<SearchResult>? results)` | Groups  by kind in `Order`, dropping empty groups and preserving each provider's order within a group. A null or empty input yields no groups. |
| `int Count(IReadOnlyList<SearchResult>? results)` | Total hit count across a set of results (null-safe). |

## `SearchSurface`

*class* — `SearchSurface.cs`

The breadth-search surface (app-search-breadth): one query box over the whole workspace, whose
grouped hits (types, members, files, graph nodes, commands) each navigate into the graph or a
diagram when activated. Dependency-free native WPF, mirroring `ClassDiagramSurface`
and `SequenceDiagramSurface`.

**Remarks.** **Scaffold.** The hits come from a Core search index that does not exist yet, so the surface
takes an injectable `Provider` and, with none wired, shows an explicit
"not indexed yet" state. Everything the App owns — the box, the debounced query, the grouped
results, keyboard activation, and the navigate hand-off — is done and tested now; wiring the
provider to the real index is the only remaining step.

| Member | Summary |
|---|---|
| `Func<string, Task<IReadOnlyList<SearchResult>>>? Provider { get; set; }` | Answers a query with hits, or null/empty for none. Null provider ⇒ the index is not available and the surface says so. Set by whatever wires the surface to the Core search index. |
| `Action<SearchResult>? OnActivate { get; set; }` | Raised when the user activates a hit. The argument is the provider's opaque result. |
| `string DisplayName` | **(gap)** |
| `SearchSurface()` | **(gap)** |
| `string Query` | The current query text (test hook / programmatic set). |
| `int ResultCount` | Hits currently shown (test hook). |
| `bool IsIdle` | True when no query has produced results — the idle/empty state (test hook). |
| `string StatusText` | The status line the user sees (test hook). |
| `Task SearchAsync(string query)` | Runs a query through the `Provider` and renders grouped results. Whitespace clears the surface; a null provider shows the "not indexed" state. Stale answers (a newer keystroke arrived first) are dropped so results nev… |
| `void ShowResults(IReadOnlyList<SearchResult> hits)` | Renders a result set directly (the render half of `SearchAsync`; test hook). |

## `SequenceDiagramSurface`

*class* — `SequenceDiagramSurface.cs`

The UML sequence-diagram surface (uml-sequence-diagram): participants as header boxes atop vertical
dashed lifelines, and ordered messages as horizontal arrows drawn top-to-bottom — solid/filled for
calls, dashed/open for returns, a loop for self-messages. Dependency-free native WPF, no WebView2,
mirroring `ClassDiagramSurface`.

**Remarks.** **No longer a scaffold.** This said the ordered call data was something "the graph does not yet
emit". It does: `calls_at` assertions carry the callee, the member and the call site, and
`WorkbenchShell.ShowNodeInSequenceDiagramsAsync` feeds them here through
`InteractionAsync`. Measured on the workspace where this surface was reported empty:
**4,967** of them. The remark outlived its subject and kept asserting a gap that had closed,
which is how the empty state below came to blame an extractor that had already done the work.

| Member | Summary |
|---|---|
| `SequenceDiagramSurface()` | **(gap)** |
| `string DisplayName { get; private set; } = "Sequence diagram"` | The tab title (survives re-render like other surfaces). |
| `bool IsEmpty { get; private set; } = true` | True while there is nothing to draw (the empty state is shown). |
| `int ParticipantCount` | Participants drawn by the last render (test hook). |
| `int MessageCount` | Messages drawn by the last render (test hook). |
| `string? NodeId { get; private set; }` | The node whose interactions are shown, or null when nothing has been loaded (Phase E). |
| `void ShowFor(string nodeId, SequenceModel model)` | Shows a specific node's interactions and records which node, so a re-render does not re-fetch. |
| `void Show(SequenceModel model, string? nodeId = null)` | Renders the interaction, or the empty state when it has no participants. |

## `SequenceMessageKind`

*enum* — `SequenceModel.cs`

The kind of a sequence-diagram message, which fixes its UML arrow style.

## `SequenceParticipant`

*record* — `SequenceModel.cs`

A participant (object/actor) in a sequence diagram — a header box atop a vertical lifeline.

## `SequenceMessage`

*record* — `SequenceModel.cs`

One message between participants, in wire order. `Order` is the position in the
interaction (0-based), which is what a sequence diagram draws top-to-bottom.

## `SequenceModel`

*record* — `SequenceModel.cs`

The sequence-diagram view model (UML interaction): the participants and the ordered messages
between them. A pure projection so it is verifiable headlessly, mirroring `ClassHierarchy`.

**Remarks.** **Data source (scaffold).** A faithful sequence diagram needs *ordered* call data — which
method calls which, in what order along a trace — which the graph does not yet emit (the Core ask
is `session-contracts §4k`). Until then `Build` projects from whatever ordered
call tuples it is handed (a test stub today; the Core feed when it lands), and the surface shows an
explicit empty state rather than implying an interaction that was not captured.

| Member | Summary |
|---|---|
| `SequenceModel Empty = new([], [])` | **(gap)** |
| `bool IsEmpty` | **(gap)** |
| `SequenceModel Build(IReadOnlyList<(string From, string To, string Label)>? calls)` | Builds a sequence model from ordered call tuples `(from, to, label)`. Participants are derived from the calls in first-seen order (so the leftmost lifeline is the caller that starts the interaction); a call whose endp… |

## `SessionRowPresenter`

*class* — `SessionRowPresenter.cs`

Pure presentation policy for the Sessions surface (smoke 9-1 #15). The read model
(`WatcherSessionRow`) is honest but its `DisplayLabel` is a flat, undifferentiated
line, so five sessions read as five identical blobs and the answer to "what is this and why is it
here" is buried. This turns a row into a legible two-line shape — a stable identity above muted
metadata — with a colour-plus-glyph liveness chip, and it states a telemetry gap the whole list
shares **once** rather than repeating "Not Recorded" down every row. No WPF, so it is verifiable
headless.

| Member | Summary |
|---|---|
| `string ChipBrushKey(LivenessBadge liveness)` | The theme brush key that colours a liveness chip. Colour is the third signal only — the glyph and text carry the meaning (WCAG 2.2 AA, not colour alone), matching `LivenessBadge`. |
| `string ChipText(LivenessBadge liveness)` | The chip's text: the glyph and word together, e.g. "✓ Alive". |
| `string Identity(WatcherSessionRow row)` | The primary line — who, where, and WHICH: the identity a session is recognised by. |
| `string Identity(WatcherSessionRow row, string? name)` | The identity line, preferring the name the operator gave this terminal. |
| `string Details(WatcherSessionRow row)` | The muted secondary line — harness, model, trust, spans: metadata, subordinate to identity. |
| `int LivenessRank(WatcherSessionRow row)` | Liveness ordering rank — Alive (0) leads, then Stale (1), then Ended (2). The session actually collaborating right now belongs at the top, not buried in store order. |
| `(IReadOnlyList<WatcherSessionRow> Live, IReadOnlyList<WatcherSessionRow> Inactive) Partition(` | Splits the sessions into the ones **actively collaborating** — **Live** (Alive only) — and the **Inactive** history to collapse (Stale then Ended). Only a heartbeating session is live; a stale one (heartbeat aged out)… |
| `string InactiveHeader(int count)` | The collapsed-section header for the inactive history, e.g. "14 inactive session(s)". |
| `string? SharedTelemetryNote(IReadOnlyList<WatcherSessionRow> rows)` | One line stating a telemetry gap the whole list shares, so it is said once instead of repeated on every row (#15 — "an all-'Not Recorded' row is a telemetry gap the list should state, not repeat five times"). Null whe… |

### `string Identity(WatcherSessionRow row)`

The primary line — who, where, and WHICH: the identity a session is recognised by.

**Remarks.** Two defects reported from the running product, in one line of text.





**It read as a path.** `{Repository}/{Worktree}` rendered
`TheTerrace/docs/fix-broken-design-links`, and the reporter reasonably asked why sessions
were not at the repository root. They were — that is a repo and a branch whose name contains
a slash, which is the dominant convention rather than an edge case.





**It did not identify anything.** Three live sessions rendered as three identical
strings, because agent, repository and branch were the same for all three. The session id was
on the record the whole time and never shown.

### `string Identity(WatcherSessionRow row, string? name)`

The identity line, preferring the name the operator gave this terminal.

**Remarks.** **The name is a presentation concern, resolved here rather than stored on the
session.** A terminal can already be renamed and the name already survives a restart, in
`TerminalCustomizationStore`, keyed by surface id — and a session's
`Terminal.TerminalId` IS that surface id. So the name needed carrying to this line, not
a column in the watcher store, a schema migration and a contract attribute to move it
there.





**The harness is kept, not replaced.** A row named "refactor the parser" that no
longer says which harness is running has traded one missing fact for another; the operator
named the session to tell it apart from its siblings, not to hide what it is.

### `(IReadOnlyList<WatcherSessionRow> Live, IReadOnlyList<WatcherSessionRow> Inactive) Partition(`

Splits the sessions into the ones **actively collaborating** — **Live** (Alive only) — and
the **Inactive** history to collapse (Stale then Ended). Only a heartbeating session is live;
a stale one (heartbeat aged out) and an ended one (closed) are both history.

**Remarks.** The Sessions surface is a LIVE-STATUS list, but a long-running workspace accumulates many
stale/ended terminals that otherwise bury the sessions collaborating now — the 2026-09-02 video
showed 3 "✓ Alive" agents leading but ~13 "~ Stale" terminals cluttering the same section
(partitioning Stale as live was too generous). Leading with Alive and collapsing everything else
is the fix. Pure and dependency-free (UX-SESSIONS-GRAVEYARD).

## `ShellViewMode`

*enum* — `ShellModeController.cs`

The shell's primary view mode (ADR-0017).

## `ShellModeController`

*class* — `ShellModeController.cs`

Owns the shell's primary `ShellViewMode` and the body-content swap that realises it
(ADR-0017). Switching mode only changes what fills the body region; it never disposes the
workbench.

**Remarks.** **Retain, never rebuild (the load-bearing invariant).** The workbench object is held by
the caller for the window's life, so a switch merely *unparents* the docking host — a
terminal running inside it keeps running while Explorer is open, and returning to the workbench
shows the same instance. WPF hides an unparented `HwndHost`/`WebView2` child rather than
destroying it, which is what makes the swap a view change and not a session loss. The design's T1
control proves this against a real terminal rather than trusting it
(`docs/design/knowledge-explorer-mode.md`).





**Lazy, then retained.** The Explorer surface is created on first entry and then held, so
re-entering Explorer does not rebuild it and its graph/reader survive a round-trip (US-E6).

| Member | Summary |
|---|---|
| `ShellModeController(ContentControl host, object workbench, Func<UIElement> explorerFactory)` | **(gap)** |
| `ShellViewMode Mode { get; private set; } = ShellViewMode.Workbench` | **(gap)** |
| `event EventHandler<ShellViewMode>? ModeChanged` | Raised after the mode changes, with the new mode. |
| `UIElement? ExplorerSurface` | The Explorer surface once it has been created; null until first entry. |
| `void Toggle()` | **(gap)** |
| `void Set(ShellViewMode mode)` | **(gap)** |

## `SurfaceChrome`

*class* — `SurfaceChrome.cs`

Wraps a pane's content in a soft "island" card — rounded, bordered, inset with a gap.

**Remarks.** **Radius + border, never shadow.** The facelift softens panes with a corner radius and
a one-pixel border, not a drop shadow — a `DropShadowEffect` over a windowed child
(WebView2 canvas, terminal `HwndHost`) is the airspace trap the wpf-styling-expert holds a
veto on (App.xaml notes this). So this frame is effect-free and therefore safe over every
surface, composited or windowed.





**How it reads as raised.** The card is `SurfaceRaised` (#1A1F26); the docking
chrome behind the gap is retokenised to `surface`/`sunken` (darker) by
`DockThemeAccents`, so the lighter card sits proud of the darker gap.





**Windowed children.** A rounded `Border` does not clip a child HWND to its
corners (airspace), so a small inset keeps the square-cornered WebView2/terminal off the rounded
edge rather than poking through it — the frame still softens the pane.

| Member | Summary |
|---|---|
| `FrameworkElement WrapAsIsland(FrameworkElement content)` | Wraps  in an island card. Returns the wrapping border. |

## `SurfaceContentFactory`

*class* — `SurfaceContentFactory.cs`

Builds the content for one surface.

**Remarks.** The workbench does not know what a surface renders and must not: a surface's identity, state and
content are independent of where it is docked (US-9). This factory is the single place that
mapping lives, so adding a surface kind never means touching the layout model.

| Member | Summary |
|---|---|
| `IReadOnlyList<string> KnownKinds { get; } = ["view", "inspector", "terminal", "canvas", "contexts", "joins", "sessions", "board", "leaderboard", "ledger", "daydreams", "prompt", "classdiagram", "sequence", "search", "codeviewer", "diagnostics"]` | Surface kinds this factory can build. An unknown kind still gets an honest pane. |
| `FrameworkElement Create(Surface surface)` | **(gap)** |

## `TerminalColorScheme`

*record* — `TerminalColorScheme.cs`

A named terminal colour scheme: the sixteen ANSI colours plus background, foreground and cursor.
Chosen per session so a user can tell two terminals apart by how their content reads, not only by
the tab caption. The ANSI sixteen are a vocabulary programs address by index (see
`TerminalPalette`), so a scheme re-maps the vocabulary — it never renames it.

| Member | Summary |
|---|---|
| `TerminalColorScheme Default { get; } = new(` | The shipped default — matches the App.xaml `TerminalAnsi*` tokens. |
| `TerminalColorScheme Warm { get; } = new(` | A warm amber-leaning scheme. |
| `TerminalColorScheme Cool { get; } = new(` | A cool teal/blue-leaning scheme. |
| `TerminalColorScheme HighContrast { get; } = new(` | Maximum legibility: pure black ground, bright ink and brights. |
| `IReadOnlyList<TerminalColorScheme> Presets { get; } =` | The presets offered in the colour-scheme menu, in order. |
| `TerminalColorScheme ByName(string? name)` | The preset with this name, or `Default` if none matches. |

## `TerminalCustomization`

*record* — `TerminalCustomizationStore.cs`

One terminal's persisted customization. All optional — a null field means "unset".

## `TerminalCustomizationStore`

*class* — `TerminalCustomizationStore.cs`

Persists per-session terminal customization (name, colour scheme, tab colour) keyed by the stable
layout `SurfaceId`, in a JSON sidecar beside the layout. This is the cross-restart half of
the customization the surface already keeps in memory (DC-029 keeps it within a session); it lives
off the Core layout model deliberately, so it needs no schema change. Best-effort: a missing or
corrupt sidecar starts clean, and a failed write never crashes the UI.

| Member | Summary |
|---|---|
| `TerminalCustomizationStore(string path)` | **(gap)** |
| `bool TryGet(string surfaceId, out TerminalCustomization? customization)` | **(gap)** |
| `void Save(string surfaceId, TerminalCustomization customization)` | **(gap)** |

## `TerminalInput`

*class* — `TerminalInput.cs`

Translates key presses into the bytes a terminal expects on its input stream.

**Remarks.** **Kept separate from the view so it is testable without a window.** Every entry below is
a lookup table with an exact right answer, and the cost of getting one wrong is a key that
silently does nothing — the single most common way a terminal feels broken. Behind a control
these would be verified by pressing keys.





**Text goes through `ForText`, not through this table.** Composed input,
dead keys and IME sequences all produce text rather than key presses, so mapping characters from
key codes would break every non-US keyboard.





**Control characters are computed, not enumerated.** Ctrl+A through Ctrl+Z are the
letter minus 64 by definition, and writing out twenty-six cases would be twenty-six chances to
mistype one.

| Member | Summary |
|---|---|
| `ReadOnlyMemory<byte> ForKey(Key key, ModifierKeys modifiers, bool applicationCursorKeys = false)` | The bytes for a key press, or empty when the key sends nothing. |
| `ReadOnlyMemory<byte> ForText(string text)` | The bytes for composed text input. |
| `ReadOnlyMemory<byte> ForPaste(string text, bool bracketed)` | The bytes for pasted text. When  (the child enabled bracketed paste), the text is wrapped in `ESC [ 200~ … ESC [ 201~` so the program treats it as one paste rather than running each line as it arrives. Carriage return… |

### `ReadOnlyMemory<byte> ForKey(Key key, ModifierKeys modifiers, bool applicationCursorKeys = false)`

The bytes for a key press, or empty when the key sends nothing.

- **`applicationCursorKeys`** — When the child has enabled DECCKM (application cursor key mode), the cursor keys are encoded as SS3 (`ESC O A`) rather than CSI (`ESC [ A`) — which is what a full-screen TUI expects.

## `TerminalMouseButton`

*enum* — `TerminalMouse.cs`

The pointer button a mouse report names.

## `TerminalMouse`

*class* — `TerminalMouse.cs`

Encodes a pointer event into the bytes a terminal expects when the child has enabled mouse tracking.

**Remarks.** Kept separate from the view so the wire format is testable without a window — every case is a
lookup with an exact right answer, and a wrong byte is a click that lands on the wrong cell or does
nothing. Two encodings exist: **SGR** (`ESC [ < b ; col ; row M/m`, xterm `?1006`),
which is unbounded and distinguishes press from release; and the **legacy** form
(`ESC [ M b col row`, each byte offset by 32), which cannot address past column/row 223.

| Member | Summary |
|---|---|
| `ReadOnlyMemory<byte> Encode(` | Encodes a button press/release (or a wheel notch) at a 0-based cell. Returns empty for an off-grid coordinate, or one the legacy form cannot represent. |

## `TerminalPalette`

*class* — `TerminalPalette.cs`

Resolves a `TerminalColor` to something the renderer can draw with.

**Remarks.** This is the one place the terminal model meets the theme, and the split is deliberate: the
screen model records what the *wire* said ("palette index 4"), and only here is that turned
into a shade. That keeps the whole of `AiDe.Core` free of a rendering framework and lets the
look change without touching a parser.





**The ANSI sixteen are a vocabulary, not a preference.** Programs address them by index
and expect red to mean red, so what a theme may choose is the shade — never the meaning. The
values live in `App.xaml` with every other token; the fallbacks below exist only for the
case where this type is used outside a running application, which is exactly what a unit test
does.





**The 240 above the sixteen are computed, not authored.** Indexes 16–231 are the
standard 6×6×6 cube and 232–255 the grey ramp, both defined by the protocol rather than by us —
so writing them out as tokens would be transcribing a specification and inviting a typo nobody
would ever notice.

| Member | Summary |
|---|---|
| `TerminalPalette()` | **(gap)** |
| `TerminalPalette(TerminalColorScheme scheme)` | A palette from an explicit per-session `TerminalColorScheme` rather than the global resources, so two terminals can render with different schemes at once. |
| `Color Background { get; }` | **(gap)** |
| `Color Foreground { get; }` | **(gap)** |
| `Color Cursor { get; }` | **(gap)** |
| `Color Resolve(TerminalColor color, bool isBackground)` | The colour to draw  in, given which role it plays. |

## `TerminalSurface`

*class* — `TerminalSurface.cs`

One terminal pane: a live session, a screen, and the view that draws it.

**Remarks.** The joins live here rather than in `TerminalView` so the view stays a renderer.
A control that also owned a process would be untestable without one, and ADR-0005 is explicit
that session state does not belong to the renderer.





**This is the first place in the product that asks for shell integration.** Until now
the nonce was generated and checked and nothing emitted it, so every session fell back to the
output heuristic. A terminal the user types into is exactly the session whose Ready/Busy state is
worth knowing, so it opts in.





**Failure is shown, not thrown.** A session that will not start is an ordinary outcome —
a missing shell, a denied policy — and an exception on a UI thread during pane construction takes
the window with it. The pane says what happened instead (**DC-011**: a silent refusal is
indistinguishable from a broken feature).

| Member | Summary |
|---|---|
| `TerminalSurface(` | **(gap)** |
| `string SurfaceId { get; }` | The layout surface id this pane renders — stable across restart, so it keys the customization store. |
| `string? DisplayName` | The user-chosen tab caption, or null to use the model title. See `IHasDisplayName`. |
| `event EventHandler? DisplayNameChanged` | Raised when the user renames this terminal, so the shell can refresh the tab caption. |
| `event EventHandler? CustomizationChanged` | Raised on any customization change (name, scheme, tab colour), so it can be persisted. |
| `TerminalColorScheme Scheme` | The scheme this session renders with. |
| `DependencyProperty TabColourProperty = DependencyProperty.Register(` | An optional accent shown on this session's tab, bound by the tab template. |
| `Brush? TabColour` | **(gap)** |
| `void Rename(string? name)` | Renames the terminal. An empty name is rejected so a tab is never nameless (US-4). |
| `void ApplyScheme(TerminalColorScheme scheme)` | Applies a per-session colour scheme to the live view. |
| `ContextMenu CreateContextMenu()` | Builds a fresh customization menu (Rename / Colour scheme / Tab colour). Fresh because a `ContextMenu` has one parent — the surface owns one for right-click in the body, and the tab owns another (where users look firs… |
| `void PromptRename()` | Opens the rename prompt for this terminal. |
| `SessionActivity Activity` | What the session is doing, as the runtime understands it. |
| `ITerminalSession? Session` | The live session, or null before it starts. Exposed so prompt dispatch can write to the terminal this pane owns. |
| `string? WorkingDirectory { get; set; }` | Where new sessions start. Set when a workspace attaches; the process directory otherwise. |
| `Func<string, string?>? WorkingDirectoryFor { get; set; }` | A per-surface working directory, overriding `WorkingDirectory` when it answers. |
| `Func<string, IReadOnlyDictionary<string, string>>? EnvironmentFor { get; set; }` | Extra environment for a session, by its id. Null (the default) means the child inherits exactly as it always has. |
| `AgentReadinessWatcher? AgentReadiness { get; private set; }` | Watches for an agent's prompt marker, when this session runs one. |
| `string CommandLine { get; set; } = "powershell.exe"` | What this pane runs. PowerShell unless a caller asks for something else. |
| `string? Executable { get; }` | Which executable THIS pane runs, chosen when it is created. |
| `AgentReadinessProfiles Profiles { get; set; } = AgentReadinessProfiles.BuiltIn` | The readiness markers in force, built in plus whatever the workspace configured. |
| `IReadOnlyList<string> AvailableAgents` | Agent executables this build can watch for readiness AND that exist on PATH. |
| `event EventHandler<string>? AttentionRequired` | Raised the first time this pane starts waiting on a person. |
| `string? AwaitingUser` | What this pane is waiting for, in words, or null when nothing is. |
| `ReadinessEvidence ReadinessEvidence` | How this session's readiness is established, for the dispatch policy to consult. |
| `void Dispose()` | **(gap)** |

### `TerminalSurface(`

- **`executable`** — The CLI this pane runs, or null for a plain shell. A parameter rather than a settable property because the constructor starts the session and therefore needs the value already — see `Executable`.

### `ITerminalSession? Session`

The live session, or null before it starts. Exposed so prompt dispatch can write to the
terminal this pane owns.

**Remarks.** The terminal lives HERE, in the shell, not in the daemon (D1) — so the shell is the only
process that can perform the side effect, while the daemon is the only one that can make the
attempt durable. That split is why `BoundaryDispatcher` takes its two phases as
delegates rather than owning them.

### `string? WorkingDirectory { get; set; }`

Where new sessions start. Set when a workspace attaches; the process directory otherwise.

**Remarks.** Static rather than per-instance because a surface is constructed by the layout factory before
any workspace is known, and a pane created after one opens should still land in the right
place. `simplify: a static default rather than threading the workspace through the surface
factory; ceiling is one workspace per shell, which the workspace lock already enforces;
upgrade trigger = a shell hosts two workspaces at once.`

### `Func<string, string?>? WorkingDirectoryFor { get; set; }`

A per-surface working directory, overriding `WorkingDirectory` when it answers.

**Remarks.** The static above is one value for every terminal, which was right while every terminal
opened in the workspace. An agent session now gets its OWN git worktree, so the cwd is a
property of the surface rather than of the shell — the same reason
`EnvironmentFor` is a function and not a value.





**Null is the honest default here**, unlike `EnvironmentFor`: falling
back to the workspace is a working answer and the state every plain terminal is in, so a
no-op default hides nothing (DC-084's test — a default is safe exactly when it works).

### `Func<string, IReadOnlyDictionary<string, string>>? EnvironmentFor { get; set; }`

Extra environment for a session, by its id. Null (the default) means the child inherits
exactly as it always has.

**Remarks.** A function rather than a value because `WorkingDirectory`'s shape does not
fit here: the workspace is one value for every terminal, and a session's identity is not —
`AIDE_SESSION` and `AIDE_HARNESS` differ per surface.





**The shell supplies this, not this class.** The values come from the git facts and
the harness choice, both of which the shell already resolves; computing them here would be a
second definition of the same quantities.





**Do not give this a default (DC-084).** Every candidate — an empty dictionary, a
no-op function — reproduces the defect it would appear to prevent: an agent launched with an
empty environment is exactly the failure, and a non-null hook returning nothing hides it
better than null does. The other hooks on this class are safe *because* their defaults
work (`Profiles` falls back to the built-ins, `CommandLine` to the
shell); this one has no working value to fall back to, so null is the honest state and the
guarantee has to live at the assignment instead.





**Which is why it is assigned in the `WorkbenchShell` constructor** rather than
in `AttachWorkspace`, and why `EnvironmentContractSurvivesNoWorkspaceTests` builds
a shell with no workspace and asserts the contract arrives anyway. That test is the control,
and it is **immune to whatever default anyone adds here**: it sets this property to null
before constructing the shell, so only an assignment that actually runs can satisfy it. Adding
a default and dropping the constructor line fails it on the null. *Verified by doing exactly
that* — the first version of this paragraph claimed it would fail on a missing
`AIDE_SESSION` instead, which was a plausible account of a test that had not been run.

### `AgentReadinessWatcher? AgentReadiness { get; private set; }`

Watches for an agent's prompt marker, when this session runs one.

**Remarks.** Null for a shell: a shell reports readiness through OSC 133 signed with the session nonce,
which is stronger evidence and needs no pattern. This exists only so an agent CLI can be
something other than permanently refused.

### `string CommandLine { get; set; } = "powershell.exe"`

What this pane runs. PowerShell unless a caller asks for something else.

**Remarks.** An agent CLI named here gets a readiness watcher and can therefore be dispatched to; a shell
gets OSC 133 integration instead, which is stronger.

### `string? Executable { get; }`

Which executable THIS pane runs, chosen when it is created.

**Remarks.** Per surface rather than the static default: an agent terminal and a shell terminal
coexist, and a single global would make opening one silently change the other on its next
restart.





**A CONSTRUCTOR PARAMETER, and it must stay one.** This was
`{ get; init; }`, set by an object initializer at the one construction site. An object
initializer runs AFTER the constructor body, and the constructor starts the session — so
`StartAsync` read this property while it was still `null`, every time, for
every pane. Measured: 243 `terminal.start` records across two days, `executable`
null in all 243, including a surface whose id was `agent:claude#aa8dcb` (DC-083).





The consequence ran the whole way down: null executable → the launch fell back to the
shell → no readiness profile matched `powershell` → `ShellIntegrationMode.PowerShell`
instead of `PowerShellHostedAgent` → `AgentCommandLine` was never called at all. A
fix to that method was verified correct in isolation and could not have changed anything a
user saw, because the branch reaching it was never taken.

### `AgentReadinessProfiles Profiles { get; set; } = AgentReadinessProfiles.BuiltIn`

The readiness markers in force, built in plus whatever the workspace configured.

**Remarks.** Settable because a built-in marker that does not match an agent's real prompt refuses that
agent forever, and until this the only way to change one was a rebuild. Defaults to the
built-ins so a shell with no configuration behaves exactly as before.

### `IReadOnlyList<string> AvailableAgents`

Agent executables this build can watch for readiness AND that exist on PATH.

**Remarks.** Read through `Profiles` rather than the static built-ins, so an agent added by
configuration is offered — otherwise configuring a marker would set up a watcher for an agent
no menu would ever open.

### `string? AwaitingUser`

What this pane is waiting for, in words, or null when nothing is.

**Remarks.** The trust gate is the NORMAL first screen for an agent this shell starts — measured in
`spikes/agent-readiness`, in a directory whose sessions run every day. Treating it as an
unexplained refusal leaves the user with a pane that will not accept a prompt and never says
why. The shell reports it and points at the pane; it does not answer it, because answering a
safety question on the user's behalf is exactly what that gate exists to prevent.

## `TerminalView`

*class* — `TerminalView.cs`

Draws a `TerminalScreen` and turns key presses into input bytes.

**Remarks.** **The draw path is binding, not a preference.** Spike S3 measured three ways of putting
a 200×50 screen on the display: `GlyphRun` per line at 6.64 ms p95, `FormattedText` per
line at 12.28 ms, and `FormattedText` per *cell* at 142.80 ms — 21× slower and four
times over the frame budget, at 7 fps. The per-cell design is the one a competent implementer
writes first, because a terminal genuinely is a grid of independently styled cells, and nothing
about it looks wrong until it is measured. That is why it is recorded here rather than left to be
rediscovered.





**Runs, not whole lines.** A line rarely has one style, so cells are grouped into runs
of identical style and each run becomes one `GlyphRun`. That is the same shape as the
measured path — a handful of draws per line rather than one per cell — and it is what real
terminals do.





**Presenting is decoupled from parsing.** The architecture budgets 1 MiB/s of sustained
output, which is an *output* rate and not a *draw* rate: a terminal coalesces, so it
must consume a megabyte a second while only ever showing the final state at frame rate. The two
differ by three orders of magnitude, and conflating them is how a renderer gets blamed for a
parser's cost. Here that means the screen is written by the reader and drawn on the rendering
tick, and only when `IsDirty` says something changed.

| Member | Summary |
|---|---|
| `void ApplyPalette(TerminalPalette palette)` | Swaps the colour scheme this view draws with and repaints. Per-session (DC-029 keeps the same view instance alive), so schemes do not leak between terminals. |
| `TerminalView(TerminalScreen screen, double fontSize = 13)` | **(gap)** |
| `event EventHandler<ReadOnlyMemory<byte>>? Input` | Raised when the user types. The surface forwards this to the session. |
| `event EventHandler<(int Columns, int Rows)>? GridResized` | Raised when the drawable area changes size, in character cells. |
| `double CellWidth { get; }` | **(gap)** |
| `double CellHeight { get; }` | **(gap)** |
| `void Attach(TerminalScreen screen)` | Points the view at a different screen — used when a session is replaced. |
| `void OnRenderSizeChanged(SizeChangedInfo info)` | **(gap)** |
| `void OnRender(DrawingContext context)` | **(gap)** |
| `void RequestRedraw()` | Requests a repaint, coalesced to at most once per dispatcher turn (≈ one frame). Called by the session pump when output has changed the screen. This REPLACES a persistent `CompositionTarget.Rendering` subscription, wh… |
| `void OnTextInput(TextCompositionEventArgs e)` | **(gap)** |
| `void OnPreviewKeyDown(KeyEventArgs e)` | **(gap)** |
| `void OnMouseDown(MouseButtonEventArgs e)` | **(gap)** |
| `void OnMouseUp(MouseButtonEventArgs e)` | **(gap)** |
| `void OnMouseWheel(MouseWheelEventArgs e)` | **(gap)** |
| `void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)` | **(gap)** |
| `void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)` | **(gap)** |
| `AutomationPeer OnCreateAutomationPeer()` | **(gap)** |

### `void RequestRedraw()`

Requests a repaint, coalesced to at most once per dispatcher turn (≈ one frame). Called by the
session pump when output has changed the screen. This REPLACES a persistent
`CompositionTarget.Rendering` subscription, which ran a handler every frame for the life of
the control — the WPF anti-pattern that keeps the render thread from ever going idle and makes
the whole window feel jittery. Now nothing runs when the terminal is idle.

**Remarks.** **Coalescing.** A producer emitting a megabyte a second updates the screen thousands
of times between frames; the user only ever sees the last. The atomic `_redrawScheduled`
gate collapses every request between dispatcher turns into a single `InvalidateVisual`.
Thread-safe because the pump raises this from a background thread.





**Isolation.** A background tab (not visible) does not repaint on output — it records
that it fell behind and repaints once when it is shown again. So one busy agent terminal cannot
drive repaints of a pane the user is not looking at.

## `TextPromptDialog`

*class* — `TextPromptDialog.cs`

A minimal modal text prompt — used for renaming a terminal tab. Returns the entered text on OK
(Enter), or null on Cancel (Escape). Deliberately tiny and dependency-free; it reuses the app's
tokens so it reads as part of the shell rather than a bare Windows dialog.

| Member | Summary |
|---|---|
| `string? Show(string title, string initial, Window? owner)` | Shows the prompt modally and returns the text, or null if cancelled. |

## `IWatcherLedgerQuery`

*interface* — `WatcherLedger.cs`

The Ledger read: every work episode the watcher has recorded, newest first. Where the Leaderboard
RANKS scored episodes and the Board shows breadcrumb messages, the Ledger is the raw append-only
record — "what work has this workspace seen", scored or not — the third view over the same
`IWatcherObservationStore` the Board and Leaderboard read.

## `WatcherLedgerQuery`

*class* — `WatcherLedger.cs`

Reads the work-episode ledger straight off the observation store (its append-only fact table).

| Member | Summary |
|---|---|
| `IReadOnlyList<WorkEpisode> GetEpisodes()` | **(gap)** |

## `LedgerRow`

*record* — `WatcherLedger.cs`

One dense line in the Ledger: what the episode was for, when it opened, and whether it closed.

**Remarks.** Pure and dependency-free so the label mapping is unit-tested off the UI thread, the same discipline
as `SessionRowPresenter` and the leaderboard row.

| Member | Summary |
|---|---|
| `LedgerRow From(WorkEpisode episode)` | **(gap)** |
| `IReadOnlyList<LedgerRow> Rows(IReadOnlyList<WorkEpisode> episodes)` | Newest first — a ledger reads top-down as most-recent-first. |
| `string StatusFor(IWatcherLedgerQuery? query)` | The honest status line: whether observation is wired, and how much it has recorded. |

## `WorkbenchAdapter`

*class* — `WorkbenchAdapter.cs`

Renders the owned `Layout` model into AvalonDock, and supplies the accessibility the
library does not (ADR-0012).

**Remarks.** The adapter is deliberately **one-way**: model → view. Pointer gestures enter as
`LayoutOperation` requests through `Apply`, never as direct
view mutations — that is what keeps the keyboard path and the drag path provably identical
(SC 2.5.7). The view is a projection; it is never the source of truth.

| Member | Summary |
|---|---|
| `string LeakedNamePrefix = "AvalonDock."` | Automation names starting with this prefix are the library's type names leaking through as accessible names — the defect the UIA probe found (spikes/avalondock-a11y). |
| `WorkbenchAdapter(` | **(gap)** |
| `DockingManager Manager { get; }` | **(gap)** |
| `void Invalidate(IEnumerable<string> surfaceIds)` | Projects the current model into AvalonDock and names everything for assistive tech.  Marks surfaces to be REBUILT (not reused) on the next `Render`. Used by the shell when a workspace attaches and the watcher read pan… |
| `void RefreshInPlace(IEnumerable<string> surfaceIds)` | Rebuilds the content of specific surfaces **in place** — replacing each `LayoutDocument`'s `Content` without swapping `Layout` — so refreshing one set of panes never disturbs the others. |
| `void Render()` | **(gap)** |
| `void ApplyAccessibleNames()` | **(gap)** |
| `FrameworkElement? ContentFor(string surfaceId)` | The content element currently hosting , or null. |
| `T? SurfaceContent<T>(string surfaceId) where T : class` | The inner surface content of type  for , looking THROUGH the island chrome (`WrapAsIsland`) that non-windowed panes are wrapped in. |
| `string? ActiveSurfaceId` | The surface id of the document the user is currently focused in, or null. Read from AvalonDock's own active-content tracking so a "new pane" command can open where the user is looking rather than in a fixed corner of … |
| `Layout? ReadLayoutFromView()` | Reads the CURRENT AvalonDock arrangement back into the owned model, so a native pane drag or a splitter resize the user performed is captured before the next `Render` would rebuild from a stale model and revert it. Re… |

### `void RefreshInPlace(IEnumerable<string> surfaceIds)`

Rebuilds the content of specific surfaces **in place** — replacing each
`LayoutDocument`'s `Content` without swapping `Layout`
— so refreshing one set of panes never disturbs the others.

**Remarks.** This exists because the watcher-pane refresh used to call the full `Render` on
every ~2s tick (a session heartbeat, a board post, a new score). A full render swaps
`Manager.Layout` wholesale, which **re-parents every pane** — re-firing the graph
canvas's `ResizeObserver` so it re-fits, and re-seating every tab from the model so a user
sitting on Sessions/Leaderboard is snapped back to the default tab. With live agents heartbeating
that made the graph "keep refreshing" and the watcher tabs impossible to stay on (smoke video
2026-09-02). Only the named surfaces are rebuilt here; layout, selection, focus and every other
pane are left exactly as the user left them.




Safe for the watcher read surfaces because they own no live process (unlike a terminal,
whose ConPTY a rebuild would kill — DC-029): the old content is dropped and the factory
reconstructs it against the current store.

### `void ApplyAccessibleNames()`

**Remarks.** Without this, AvalonDock reports each tab's **.NET type name** — `AvalonDock.Layout.LayoutDocument`
— as its accessible name, so every surface sounds identical to a screen reader
(verified, spikes/avalondock-a11y). A typed `TabItem` style setting the same property does
**not** reach these items; that was tested and rejected. Walking the realized visual tree does.

### `FrameworkElement? ContentFor(string surfaceId)`

The content element currently hosting , or null.

**Remarks.** Read from AvalonDock's own tree by `ContentId` rather than from a parallel dictionary:
a second map of surface-to-content is a second thing to keep in step with a layout the user
rearranges, and it would go stale exactly when a pane is moved or closed.

### `T? SurfaceContent<T>(string surfaceId) where T : class`

The inner surface content of type  for ,
looking THROUGH the island chrome (`WrapAsIsland`) that non-windowed
panes are wrapped in.

**Remarks.** A wrapped pane's `ContentFor` returns the framing `Border`, not the
surface, so `ContentFor(id).OfType<ClassDiagramSurface>()` silently finds nothing and
the pane never populates — the exact defect that left the class diagram (and every other wrapped
surface bound by type) empty over a fully indexed workspace. Canvas and terminal are returned
UNWRAPPED (airspace), so the direct-cast branch finds them; everything else is a
`Border` whose `Child` is the real surface. Both are handled here
so no caller has to know which, and so a future wrapped kind cannot reintroduce the same silence.

### `Layout? ReadLayoutFromView()`

Reads the CURRENT AvalonDock arrangement back into the owned model, so a native pane drag or a
splitter resize the user performed is captured before the next `Render` would rebuild
from a stale model and revert it. Returns null when the view cannot be mapped confidently.

**Remarks.** **Fail-safe by construction.** The model is the source of truth and a wrong reconcile
would be rendered AND persisted, so this returns null the moment it meets a shape it cannot map
losslessly — a floating window, an anchorable pane, an empty pane, an unknown node, a document
whose surface the model does not know, or a result that does not carry exactly the same set of
surfaces it started with. The caller then leaves the model untouched, degrading to the
pre-existing revert-on-rebuild, never to a lost or duplicated pane.





**Surface identity comes from the model, not the view.** A `LayoutDocument`
carries only its `ContentId` (the surface id); the Kind and Title live on the model's
`Surface` record, looked up here, so a reconciled surface keeps the identity the rest
of the system routes on. Node ids are freshly minted — they are internal and need not be stable.

## `IWorkbenchAnnouncer`

*interface* — `WorkbenchAnnouncer.cs`

Announces a completed layout change to assistive technology.

## `WorkbenchAnnouncer`

*class* — `WorkbenchAnnouncer.cs`

Speaks layout changes to assistive technology **without moving focus** (SC 4.1.3 Status Messages).

**Remarks.** Two mechanisms, deliberately together rather than either/or:

**A UIA notification** (`RaiseNotificationEvent`) — the
modern, purpose-built channel for "tell the user something happened here", which screen readers
announce without changing the focus or the reading position.
**A polite live region** — the older mechanism, kept because notification support varies
by screen reader and version. A layout change that reaches neither is a silent change, which is
the failure this class exists to prevent.

Focus is never touched. That is the whole point: an operator who has just floated a pane with the
keyboard should hear that it happened and still be exactly where they were.

No exemplar documents doing this at all — see the spec's workbench exemplar evidence — so this is
the one place AI-DE is deliberately ahead of the category rather than matching it.

| Member | Summary |
|---|---|
| `WorkbenchAnnouncer(TextBlock liveRegion)` | **(gap)** |
| `string Last { get; private set; } = string.Empty` | **(gap)** |
| `void Clear()` | **(gap)** |
| `void Announce(string message)` | **(gap)** |

## `RecordingAnnouncer`

*class* — `WorkbenchAnnouncer.cs`

A headless announcer for tests and for any host without a live region yet.

| Member | Summary |
|---|---|
| `IReadOnlyList<string> Messages` | **(gap)** |
| `string Last` | **(gap)** |
| `void Clear()` | **(gap)** |
| `void Announce(string message)` | **(gap)** |

## `WorkbenchController`

*class* — `WorkbenchController.cs`

Routes every keyboard layout command through the model and announces the outcome.

**Remarks.** This is where SC 2.5.7 and SC 4.1.3 stop being design intent and become behaviour: a command
produces a `LayoutOperation` — the same type a pointer drag produces — applies it via
the single mutation path, and announces whatever came back, including a refusal. A refused
operation is announced too, because "nothing happened" is information the user needs and silence
is indistinguishable from a broken key.

| Member | Summary |
|---|---|
| `string? FocusedStackId { get; set; }` | The stack the user is working in. Layout commands act on this. |
| `string? FocusedSurfaceId { get; set; }` | The surface within the focused stack, when one is selected. |
| `bool IsResizing` | **(gap)** |
| `Func<Task<string>>? WorkspaceRefresh { get; set; }` | Asks the workspace to re-index itself. Set when a workspace attaches; null before that. |
| `event Action? WorkspaceDataChanged` | Raised after a command that CHANGED what the store holds has finished. |
| `CanvasFocusRouter? CanvasFocus { get; set; }` | Routes focus across the canvas boundary. Set when a graph canvas surface attaches. |
| `bool Execute(string commandId)` | Runs a catalog command by id. Returns false when the id is unknown. |
| `bool Move(string surfaceId, DropTarget target)` | Applies a move produced either by a keyboard destination choice or by a drop. |
| `DropTarget? HoveredTarget { get; private set; }` | The destination the in-flight drag currently points at, or null for none. |
| `LayoutRect? HoveredPreview { get; private set; }` | The rectangle the UI should highlight for `HoveredTarget`. |
| `event Action<bool>? DragStateChanged` | Reports pointer movement during a drag. Resolves the destination and its preview from the SAME call, so the highlight the user sees and the drop that follows cannot disagree.  Raised when a drag starts and when it end… |
| `DropTarget? DragOver(IReadOnlyList<PaneHitBox> panes, LayoutPoint pointer)` | **(gap)** |
| `bool Drop(string surfaceId)` | Commits the drag at the hovered destination. Returns false when there is none. |
| `void CancelDrag()` | Abandons the drag with the layout untouched (Escape, or the pointer leaving). |
| `bool HandleResizeKey(Key key)` | Handles an arrow/Enter/Escape while a resize is in flight. Returns true if consumed. |
| `bool ReorderFocusedSurface(int direction)` | Moves the focused surface one position within its pane, wrapping at the ends. |
| `Action? PromptBarOpen { get; set; }` | Starts a re-index and announces both the start and the outcome.  `workbench.focusCanvas` — moves focus into the graph canvas, or says why it cannot.  Opens the prompt bar. Set when the shell builds one; null in a head… |
| `Func<Task<string>>? WorkspaceIndex { get; set; }` | Indexes the workspace's C# projects. Set when a workspace attaches; null before that. |
| `Func<Task<string>>? WorkspaceReindexAll { get; set; }` | Re-reads every scope, ignoring the fingerprint cache. |
| `Func<string>? WorkspaceDiagnostics { get; set; }` | Reports daemon, health and MCP state. Set when a workspace attaches. |
| `Func<Task<string>>? WorkspaceOpen { get; set; }` | Chooses and opens a workspace. Set by the window that can show a folder picker. |
| `Func<string, string>? NewAgentTerminalRequested { get; set; }` | Opens a terminal running an agent CLI. Set by the shell that can create surfaces. |
| `Func<string>? NewTerminalRequested { get; set; }` | Opens a plain shell terminal (never an agent). Set by the shell that can create surfaces. |
| `Func<string>? RaiseDisputeRequested { get; set; }` | Raises an append-only dispute against the latest scored episode. Set by the shell (US rule 12). |
| `Func<string>? NewPromptDraftRequested { get; set; }` | Opens a prompt-draft surface. Set by the shell that can create surfaces. |
| `Func<string>? NewClassDiagramRequested { get; set; }` | Opens a class-diagram surface. Set by the shell that can create surfaces. |
| `Func<string>? NewSequenceDiagramRequested { get; set; }` | Opens a sequence-diagram surface. Set by the shell that can create surfaces. |
| `Func<string>? NewSearchRequested { get; set; }` | Opens a workspace breadth-search surface. Set by the shell that can create surfaces. |
| `Func<string>? NewCodeViewerRequested { get; set; }` | Opens a read-only code-viewer surface. Set by the shell that can create surfaces. |
| `Func<string>? NewDiagnosticsRequested { get; set; }` | Opens the workspace diagnostics surface. Set by the shell that can create surfaces. |
| `void Bind(UIElement host)` | Binds the catalog's gestures to this controller on a WPF element. |

### `Func<Task<string>>? WorkspaceRefresh { get; set; }`

Asks the workspace to re-index itself. Set when a workspace attaches; null before that.

**Remarks.** A delegate rather than a workspace handle: the controller's job is layout and command
dispatch, and giving it something it could read evidence from would invite exactly that.

### `event Action? WorkspaceDataChanged`

Raised after a command that CHANGED what the store holds has finished.

**Remarks.** **Indexing used to end at the announcement.** A re-index wrote 10,242 new assertions
— the whole knowledge half of a real repository — and every open pane went on rendering the
projection it had fetched when it loaded. The user re-indexed, watched the message say it had
worked, and read a Knowledge count of 0 taken from a graph twenty-six seconds out of date.
The store was right and the screen was wrong, which is the worst of the two.





An event rather than a call into the panes: the controller dispatches commands and owns
no surface. Who listens, and what re-reading costs them, belongs to whoever holds the pane.

### `CanvasFocusRouter? CanvasFocus { get; set; }`

Routes focus across the canvas boundary. Set when a graph canvas surface attaches.

**Remarks.** **Null until the canvas exists, and that is a working state rather than a gap.** The
command stays in the catalog and stays keyboard-reachable; with no canvas it refuses and says
so, which is the same path a canvas that has not created its handle yet takes. Hiding the
command instead would make "the graph cannot be focused" indistinguishable from "the graph
does not exist", and a user who pressed the chord would get silence (**DC-011**).

### `event Action<bool>? DragStateChanged`

Reports pointer movement during a drag. Resolves the destination and its preview from the
SAME call, so the highlight the user sees and the drop that follows cannot disagree.

Raised when a drag starts and when it ends, so an airspace-limited surface can stand aside.

**Remarks.** Announces only when the destination actually changes. Re-announcing on every mouse-move would
flood a screen reader with the same sentence and drown everything else out.

ADR-0015: the windowed WebView2 is drawn by the OS above every WPF element in the same space,
so a drop indicator over the canvas is simply not visible. For the duration of a drag the
canvas is swapped for a still frame. The composition control fixes the airspace problem and
then kills the process when a pane is floated, which is why this exists at all.

### `bool ReorderFocusedSurface(int direction)`

Moves the focused surface one position within its pane, wrapping at the ends.

**Remarks.** Wrapping rather than stopping: a keyboard user repeating the key should be able to reach any
position without having to know which end they are at.

### `Action? PromptBarOpen { get; set; }`

Starts a re-index and announces both the start and the outcome.

`workbench.focusCanvas` — moves focus into the graph canvas, or says why it cannot.

Opens the prompt bar. Set when the shell builds one; null in a headless controller.

**Remarks.** **Announced twice on purpose.** Re-indexing takes as long as it takes, and a command
that acknowledged nothing until it finished would be indistinguishable from a key that did
not register — so the operator is told it started, and told again what happened.





With no workspace attached this still returns handled, and says so. A command in the
palette that silently does nothing is the failure the catalog's conformance test exists to
prevent (**DC-011**).

Always returns true: the command IS handled, and its refusal is an outcome rather than a
failure to dispatch. Returning false would make the palette treat a legitimate "the canvas is
mid-drag" refusal as an unknown command.

### `Func<string, string>? NewAgentTerminalRequested { get; set; }`

Opens a terminal running an agent CLI. Set by the shell that can create surfaces.

**Remarks.** Takes the agent because a session's harness must be known AT LAUNCH: a second coordination
register for a known session discards its attributes rather than merging them (observed —
`CoordinationContractTests.Apply_DuplicateRegister_DiscardsTheSecondAttributes_ItDoesNotMerge`),
so there is no later opportunity to say which harness this session is running.

## `WorkbenchDiagnostics`

*class* — `WorkbenchDiagnostics.cs`

Structured trace for workbench layout behaviour — pane placement, adds and closes. The workbench
had NO instrumentation, so a "my pane disappeared" report was untraceable after the fact. Each
event emits an OpenTelemetry-aligned `Activity` (via the `aide.workbench` source)
AND appends a compact JSON line to `%LOCALAPPDATA%/AiDe/logs/workbench-YYYYMMDD.log` so the
behaviour can be read back. Best-effort: diagnostics must never break the workbench, so every sink
path swallows its own failure.

| Member | Summary |
|---|---|
| `ActivitySource Source = new("aide.workbench")` | **(gap)** |
| `Action<string>? Sink { get; set; }` | Test seam: when set, records go here instead of the log file (headless assertion). |
| `void LayoutMutation(` | Records a layout mutation and the resulting stack/surface topology. |
| `void TerminalStart(` | Records the decision a terminal launch made, and how it ended. |
| `void Crash(string origin, Exception exception)` | Records an unhandled exception, with the context that says which gesture produced it. |

### `void TerminalStart(`

Records the decision a terminal launch made, and how it ended.

**Remarks.** **Why.** "New Claude Code session" opened a plain PowerShell prompt, twice, across
two different root causes. Nothing about the launch was recorded — the log carried only layout
mutations — so each round of diagnosis was static reading plus a screenshot, and the second
round confirmed a fix that then did not change what the user saw.





These are the INPUTS to the launch decision, not a narration of it: which executable was
resolved, whether a readiness profile was found (that single value chooses between hosting the
agent and running the command line as a shell), and whether the environment contract was
attached. A wrong value here explains the symptom immediately; reading the code cannot, because
the code is correct for the values it was written against.





Terminal BYTES are never recorded (spec privacy). This is the launch decision only.

### `void Crash(string origin, Exception exception)`

Records an unhandled exception, with the context that says which gesture produced it.

**Remarks.** **Why this exists.** The shell crashed on "New Claude Code session" and left
**nothing** — no Windows Error Reporting entry, no event-log record, and nothing in this
log, which until now recorded only layout mutations. A user could say only that the .exe
closed. The whole diagnosis had to start from a screenshot.





A crash is the one moment when the product knows the most and reports the least. This
does not change what happens next — the process still fails — it only makes the failure
legible, which is the difference between "it crashed" and a stack trace pointing at a
line.

## `WorkbenchShell`

*class* — `WorkbenchShell.cs`

The composition root for the workbench: model, adapter, controller, announcer and the docking
host, assembled and wired.

**Remarks.** This is the E10 reachability piece. Everything in Phase 1b was built and tested but unreachable —
the window still showed the superseded fixed grid, so a user could not touch any of it. A
capability nobody can open is not delivered.

Composition happens in one place on purpose: the live region, the controller and the adapter must
share the same `ILayoutService` instance, or the keyboard would mutate one layout
while the view rendered another.

| Member | Summary |
|---|---|
| `WorkbenchShell(IWorkspaceQueries? queries, string? workspaceDataDirectory = null)` | **(gap)** |
| `ILayoutService Service { get; }` | **(gap)** |
| `DockingManager Manager { get; }` | **(gap)** |
| `FrameworkElement WorkbenchRoot` | The docking host wrapped in collapse-to-rail edge strips (ADR-0021). Host this instead of `Manager` so a collapsed tool zone shows a one-click rail back. Falls back to the bare manager when the layout is not zone-based. |
| `WorkbenchAdapter Adapter { get; }` | **(gap)** |
| `WorkbenchController Controller { get; }` | **(gap)** |
| `IWorkbenchAnnouncer Announcer { get; }` | **(gap)** |
| `TextBlock LiveRegion { get; }` | The polite live region announcements are written to; also the visible status text. |
| `CommandPalette Palette { get; }` | The keyboard route to every layout command (SC 2.5.7). |
| `PromptBar Prompt { get; }` | Stages a prompt for the focused terminal and reports the delivery receipt. |
| `LayoutPersistence? Persistence { get; private set; }` | Saves and restores the arrangement across restarts. Null on first run. |
| `void Bind(UIElement host)` | Binds keyboard commands and the palette to a host element — normally the window. |
| `void AttachWorkspace(` | Points the shell at a workspace that became available after it was built. |
| `CanvasSurface CreateExplorerGraph()` | Builds a graph canvas for the full-window Explorer surface (design D2), bound to the SAME workspace queries the workbench canvas reads — two graph-shaped APIs would be two answers that can disagree. A dedicated instan… |
| `Task<bool> DispatchToAsync(string sessionId, string body)` | Transfers a prompt-draft body to a NAMED ready session (spec-editor-surfaces US-ED6), by its session id, through the same choreography as the focused path. Returns whether the terminal accepted the write (PtyWriteAcce… |
| `IReadOnlyList<PromptTarget> ReadyPromptTargets()` | The ready terminal sessions a prompt draft may transfer to (US-ED6), live. |
| `void Dispose()` | **(gap)** |
| `IReadOnlyList<WorkbenchCommand> PaletteCommands(string search)` | The command palette's rows: every keyboard-reachable layout command. |

### `void AttachWorkspace(`

Points the shell at a workspace that became available after it was built.

**Remarks.** Panes already on screen are re-rendered, because a pane showing "not available in this build"
after the workspace opened is worse than one that never claimed anything.

## `WpfHostFocusScope`

*class* — `WpfHostFocusScope.cs`

`IHostFocusScope` over WPF's own focus system — the half of the crossing that WPF
*can* do.

**Remarks.** Only the canvas is unreachable by traversal. Once focus is back on the WPF side, moving it on is
ordinary `MoveFocus`, so this deliberately does not reimplement
traversal — it hands the direction to WPF and lets the normal tab order apply.

| Member | Summary |
|---|---|
| `object? Current` | **(gap)** |
| `bool Restore(object target)` | **(gap)** |
| `bool MoveNext(CanvasFocusDirection direction)` | **(gap)** |

## `ZoneRails`

*class* — `ZoneRails.cs`

Edge rails for collapsed tool zones (ADR-0021 collapse-to-rail). Wraps the docking host in a
`DockPanel` and shows a thin, clickable strip on the Left, Right or Bottom edge
whenever that tool zone is collapsed — the panes are retained in the model, and the rail is the
one-click way back (AC-F4). When a zone is expanded its rail is hidden and the dock host reclaims
the space. This is custom chrome around AvalonDock (which renders documents, not auto-hiding
anchorables), driven entirely by the zone model.

| Member | Summary |
|---|---|
| `ZoneRails(FrameworkElement dockHost, Func<WorkbenchLayout?> zones, Action<ZoneId> expand)` | **(gap)** |
| `FrameworkElement Root` | The composed element to host in place of the bare docking manager. Attaching the host is deferred to here so that constructing the rails does not claim the host as a child — a caller (or a test) that hosts the manager… |
| `void Refresh()` | Re-reads the zone model and shows a rail for each collapsed tool zone. |
| `bool RailVisible(ZoneId zone)` | Whether a zone's rail is currently shown — exposed for tests. |
