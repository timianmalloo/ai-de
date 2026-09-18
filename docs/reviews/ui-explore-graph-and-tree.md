---
id: review-ui-explore-graph-and-tree
title: "UI review — the Explore/Architecture right-side views: the graph is confidently wrong before it is small, and the Tree the operator asked for is 80% already built"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "conductor-watch-0915"
tags: [ui-design, explore, graph-canvas, solution-tree, provenance, accessibility, clean-machine, ruling-94, ruling-132]
links:
  - { to: inv-0014-the-graph-stage-is-capped-and-typescript-provenance-is-a-scope-id, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: spec-knowledge-exploration, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-17
summary: >-
  The KG-visualization UX lens reviewed the Explore/Architecture right-side views against the
  operator's two clean-machine issues. It found the container defect INV-0014 measured is not the
  worst problem on the surface: every non-join edge is rendered without its provenance while the
  status is already on the wire, the layout pins node position to ordinal so a one-node change
  re-scatters the view, the force pass runs exactly one iteration at the default size, and two
  counters on screen report numbers that are false. The Tree the operator asked for already exists
  as SolutionTreeSurface and needs placement and a selection seam, not a build.
---

# The Explore right-side views

*Reviewed 2026-09-17 by the KG-visualization UX lens under `/ui-design` **elevate**, on the operator's
clean-machine screenshots of 2026-09-16. Verdict: **BLOCK**. Nothing was built or run in this pass —
every design claim here is unrendered, and the craft gate was not run, so the craft floor is
**unproven, not passed**.*

The operator's two issues:

1. *"the right-side-views for the architecture explorer need to be Graph and Tree — where tree is the
   more familiar dev view like in vs code"*
2. *"graph view should be scaling with the dock on a larger monitor — the graph view should be
   filling the dock"*

## 1. The finding that reorders the queue

INV-0014 measured issue 2 and found the container: `CanvasPage.cs:65`, `#stage { height: 440px }`,
21.8% of the pane's height. That is real and the lens confirmed it twice — by opening the file, and
by measuring the stage off the screenshot independently at ≈1630×438 against the relayed 1624×437.

**But the container is not the worst defect on this surface, and enlarging it alone makes a bigger
wrong picture.** Ranked by what it costs a reader:

| # | Finding | Severity | Evidence |
|---|---|---|---|
| 1 | **Every non-join edge is drawn without its provenance.** `CanvasPage.cs:705-718` applies the provenance encoding only inside `if (edge.isJoin)`; the `else` branch is one line, `stroke '#2A313B'` — no dash, no glyph, no word, no `<title>`. `CanvasEdge.IsInferred` is defined on *every* edge and `Status` is already on the wire. | **Blocker** | An `INFERRED` edge is pixel-identical to an `EXTRACTED` one. `DESIGN.md:311-324` already specifies the three-channel encoding and names this exact anti-pattern — the implementation contradicts its own committed design system. |
| 2 | **Node position is a function of ordinal, not identity.** `CanvasPage.cs:658-666`: `i = others.indexOf(n)`, `rr = radius*√((i+1)/count)`, `aa = (i+1)·π(3−√5)` — over an array sorted by degree descending. | **Blocker** | Adding one node, or one *edge* anywhere (which re-ranks), shifts every later ordinal and rotates it by ≈137.5°. The whole layout re-scatters on a one-node change. US-K8 requires positions pinned by stable id. 3D has the identical defect. |
| 3 | **The force layout runs exactly one iteration at the default size.** `CanvasPage.cs:488`: `iters = max(1, min(300, floor(4000000/(n*n))))`. At n=1,500 that is **1**. | **Blocker** | The screenshot's disc is not a settled layout — it is phyllotaxis seeding plus a single jitter step. Growing the stage 440→1760 px raises `k` but `fit()` rescales regardless, so **structure will not emerge**: the operator gets a bigger undifferentiated disc. Dot separation does improve (≈66 → ≈1,026 px² per node), so the container fix genuinely helps legibility; it does not deliver insight. |
| 4 | **The default view is a smaller hairball, not a bounded neighbourhood.** `WholeGraphAsync` builds `RootId: null` with `OverviewNodeCap = 1_500`, ranked globally by degree. | **Blocker** | `docs/specs/knowledge-exploration.md:70` (US-K2) requires a focus node and N≤2 and names this lens as clears-when; `:175` already admits the violation. Ranking by degree selects precisely the god-nodes, guaranteeing the worst case. |
| 5 | **One counter, two names, at most one true.** `CanvasGraphViewModel.cs:174-176` renders `graph.Omitted` as **nodes** not drawn; `CanvasPage.cs:769` renders the same integer as **"edge(s) omitted by the result bound"**; `CanvasGraph`'s own doc comment says edges. | Major | The screenshot shows both reading 8,280, and 9,780 − 1,500 = 8,280 — so the value is the node shortfall and the banner's word "edge(s)" is wrong. A bounded view whose bound-disclosure is false is worse than no disclosure. |
| 6 | **The facet chips show false zeros.** `buildFilters(catCounts)` is fed from drawn nodes only. `CanvasGraph.DeclaredByKind` exists expressly to fix this and `CanvasPage.cs` never references it — zero occurrences. | Major | The screenshot reads **"Knowledge 0"** and **"Specs 0"** on a workspace with 8,280 undrawn nodes; an operator reads that as "this workspace has no knowledge". The chips sum to exactly 1,500, confirming they describe the sample rather than the workspace. The `count.lower-bound` rule is honoured in the status bar and violated three feet away. |
| 7 | **The edge stroke uses a token whose own disposition forbids this use.** `#2A313B` on `#0D1014` measures ≈**1.45:1**; `DESIGN.md:648` independently records the token at 1.39:1 with the note *"the border is decorative and spacing carries the grouping — it is never the only signal."* | Major | In a graph view the edge **is** the content and nothing else carries the relation. WCAG 2.2 SC 1.4.11 requires 3:1 for graphics needed to understand content. Handed to UX & Accessibility for the formal ruling. |
| 8 | **The degree encoding is dead where it matters and measures the wrong graph.** `CanvasPage.cs:653`: `--r = min(26, 9 + deg*3)` saturates at degree ≥ 6, so in a view ranked "most connected first" essentially every drawn node is at the cap. `degree` is computed from the **truncated** edge list while the server ranked on true degree. | Major | Two definitions of one quantity, and the client's is systematically wrong with 8,280 nodes' edges missing. |
| 9 | **`metric.legend` is specified and unimplemented.** No betweenness, community or gap overlay exists; the only encodings are saturated degree and `hsl(hash(context),50%,45%)`. | Major | `obsidian-setup.py --analyze` already computes hubs, exact betweenness bridges, components and structural gaps dependency-free. The metric exists in the repo and is not on the canvas. |
| 10 | **`CanvasPage.Html` is a token-free island.** Seven `App.xaml` token values restated as raw hex inside a C# raw-string literal. | Major | `DESIGN.md:546` TC5: *"Hosted web surfaces receive the tokens; they do not restate them."* This is the correct generalisation of DC-230 — see §5. |
| 11 | A comment asserts the opposite of the code beside it: `CanvasPage.cs:633-636` says *"Deliberately **NOT** a force simulation in either mode"* while `layout2d` (`:474-524`) is Fruchterman-Reingold and is called at `:724`. | Minor | A future reader will trust the wrong one. |

## 2. For the Ruling 132 P1 implementer, before the container fix lands

`CanvasPage.cs:724-726` calls `layout2d(); fit(); place();` together. The obvious flex fix adds a
resize handler — **and if that handler calls `layout2d`, every node moves every time the operator
drags the splitter**, trading DC-230 for a US-K8 violation. Attach a `ResizeObserver` that calls
**`fit()` + `place()` only**; `fit()` (`:527-544`) is pure re-framing and is exactly the right
primitive. Finding 2 (seed by stable id) should land **in the same change**, because the container fix
is what adds the resize path that would otherwise re-scatter the view.

## 3. The Tree — a placement, not a build

**It already exists and is already tested.** `src/AiDe.Core/Projections/SolutionTreeProjection.cs`
(`SolutionTreeNode(Path, Kind, Coverage, NodeId, NodeKind)`, caps, typed disclosures, `SourceRevision`)
and `src/AiDe.App/Workbench/SolutionTreeSurface.cs` — a real WPF `TreeView`, `Nest()` from flat paths,
File/Folder/Stale glyphs, coverage strokes, six load states, and a `BuildFileMenu` already carrying
**View source** and **Reveal in graph**. The operator's ask is ~80% built; what is missing is placement
as a peer view and the bidirectional seam.

**Spanning rule: workspace path containment.** Grain — *one row is exactly one `(Path, Kind)`*.

It is chosen because it is the **only relation in this graph that is genuinely a tree** — a file sits
in exactly one folder, a true partition — so the tree cannot imply a relation the graph does not
assert. It is also literally the VS Code idiom the operator named. **Inheritance was refused as a
spanning rule**: `ClassHierarchyModel` yields a DAG, and the operator's own screenshot shows it
degenerating — *"Class hierarchy — 1500 type(s), 0 relationship(s)"*, a forest of 1,500 singletons
rendered as a hierarchy.

What it discards, and this must be stated in the UI rather than only here: every non-containment edge
(`calls`, `imports`, `inherits`, `maps_to`, `hosted_on`, `is_declared_secret` — essentially all the
graph's semantics); the entire symbol tier (`SolutionTreeNode` bottoms out at the file); and files
outside the census walk, where *"we chose not to look"* and *"we ran out of room"* are different
claims and must be separately rendered.

**The discard is the design's safety property.** Because the tree renders only the relation that is
always `EXTRACTED`, **the tree is structurally incapable of laundering provenance** — and the hard
rule that follows is: **never put a non-containment child under a tree row.** The moment an `INFERRED`
call edge becomes a child with the same expander glyph as a folder, the tree has laundered it.

**Graph ↔ Tree: two tabs over one selection.** Not a split — the operator's live complaint is that the
graph has too little room, and splitting the pane three ways starves all three. `ExplorerSurface.cs:66-69`
already establishes *"one selection, two views, wired through the canvas seam so they cannot disagree"*;
extend that seam rather than forking it.

| Carries across the switch | Rule |
|---|---|
| Selection | Always, one `SelectedNodeId`. Graph→Tree expands the ancestor chain and scrolls the row in ("Reveal in tree", the inverse of the existing Reveal in graph). Tree→Graph re-centres on the bounded neighbourhood. |
| Folder rows | `NodeId` is **null** on `CensusFolder` — a folder has no graph node. Switching on a folder must not clear the selection: offer *"Show this folder's N indexed files as a group"* (the canvas already supports group super-nodes). If the group cannot be built, keep the previous selection **and say so** — never a silent no-op. |
| Expansion state | Per view, per workspace, keyed **by path, never by row index** — the same ordinal-vs-stable-id lesson as finding 2. |
| Graph viewport | Session-persistent. Re-frame on a selection the user made; never on an unrelated re-render. |
| Reader's current node | Follows the selection. The tree raises the **same** event as the canvas, so the reader cannot tell which view drove it. |
| Facet filter | Carries — or the two views disagree about what the workspace contains. |

## 4. Accessibility

**Tree — mostly free.** WPF `TreeView` carries the platform tree pattern (UIA `Tree`/`TreeItem`, arrow
navigation, collapse/expand, typeahead). Add per-row `AutomationProperties.Name` =
`"{name}, {kind}, {coverage}"` and Enter to activate. **Open check:** `BuildFileMenu` is reached on
pointer; whether Shift+F10 raises it is **not recorded** — that is DC-214's exact shape, and if absent
it is a Major.

**Graph — the harder half, and the one usually skipped.** The stage has no role (1,500 tab stops in
DOM order is a list with no announced structure), there is no keyboard edge traversal, and no "N of M"
position announcement. Proposed grammar: the stage becomes `role="application"` with **one** tab stop;
arrows move to the nearest neighbour **by edge, not by pixel**; Enter centres, Backspace goes back;
`[` / `]` cycle the focused node's edges announcing predicate **and provenance word** (*"calls,
Inferred"*); a live region reads *"Focused X. 7 edges: 5 Verified, 2 Inferred."* Tab stays the
**escape** grammar, never the traversal grammar. This is the largest a11y change on the surface and is
the KG lens's to name, **UX & Accessibility's to ratify**.

**Airspace (UI-T4 fires).** WPF cannot draw over the WebView2, so every overlay — banner, legend,
chips, tooltips — must live inside the HTML page, which today they do. Two open items: the node context
menu is a WPF `ContextMenu` raised from a WebView2 message and *should* composite above the browser
(**Inferred, not run** — check it at 3840×2160); and the tab strip must be WPF and outside the
WebView2's rect.

## 5. Class-register consequence

The lens sharpened DC-230. The class is **not** "a fixed extent in an unlinted medium" — it is that a
**C# raw-string literal is invisible to both `ui-craft-gate.py` and `design-lint.py`**, so
`height: 440px` *and* fifteen off-token colours both survive every gate in the same blind spot. The
control that follows is not a height assertion but **injecting the token set into the page at navigate
time**, after which a lint can see the page at all. DC-230's entry and control are amended to that
shape.

## 6. Routed to the Owner — Ruling 94 conflicts with the operator's words

Ruling 94 fixes **Left** = `[Graph]` at the operator's saved extent (0.22), **Center** =
`[Contexts (active), Domain]`, **Right** = empty/collapsed. The operator's "right-side views" are the
pane holding Domain | Contexts in their screenshot — Ruling 94's **Center**. So the request is
`Center = [Graph, Tree]`, which replaces the 94 pair *and* leaves Graph in two zones at once, which
cannot stand as written. Three options, the lens's recommendation second:

- **(a)** `Center = [Graph (active), Tree]`, Left retires its Graph — the operator's words taken
  literally; drops Contexts/Domain from the default.
- **(b, recommended)** `Center = [Graph (active), Tree, Contexts, Domain]` — Graph and Tree first and
  active, the 94 pair retained behind them. Satisfies the ordering, preserves 94's admitted surfaces,
  needs no zone deletion. Left's `[Graph]` @0.22 retires: one Graph, one home.
- **(c)** Defer the layout question and ship the container, provenance and stability fixes into the
  existing placement. **Does not satisfy issue 1.**

Ruling 94 condition (3) anticipated a finding here, and it lands: the Graph pane's header strip is
`CanvasPage`'s own flex row with **no `flex-wrap` on `header`** (`#filters` has it; `header` does not),
so at Left @0.22 it will overflow — DC-218's shape.

## 7. Ranked plan

**Highest-leverage change: render provenance on every edge** (`CanvasPage.cs:705-718`). ~10 lines in
one function, against data already on the wire, specified by a token table that already exists.
**Every other fix makes a wrong picture bigger or prettier; this one makes it true**, and it is the
only finding carrying a hard escalation.

1. Provenance on every edge — Blocker, small.
2. Seed layout by stable id, not ordinal — Blocker, small; **same change as the container fix**.
3. `fit()`-only on resize, never `layout2d` — trivial, and it is a precondition of P1.
4. Truthful bounded-result banner + `declaredByKind` chips — removes two false numbers on screen now.
5. Edge-stroke contrast token — trivial, unblocks the a11y ruling.
6. Place Tree as a peer view, wire the bidirectional seam, persist expansion by path — **gated on the
   Ruling 94 amendment**.
7. Settle the layout before first paint (fix the one-iteration collapse) — medium. Do not animate the
   settle: a layout that moves while you read it makes a node impossible to point at.
8. Graph keyboard traversal grammar — medium-large, with UX & Accessibility.
9. Betweenness/community overlay per `metric.legend`, reusing `--analyze` — this is where insight
   finally arrives.
10. Inject tokens into `CanvasPage.Html`, then lint it.

**The density question is named, not acted on** (Ruling 132 cuts LOD work before P1 is measured). At
the 1,760 px stage, record: iterations actually run; mean nearest-neighbour distance in px; fraction of
node dots overlapping another dot; node and edge counts after de-duplication. Those four settle whether
LOD is needed. Until then the true post-fix density is **not recorded**.

## 8. What this review does not cover

Nothing was built or run. No mockup, no review harness, no rendered hard states, and
`ui-craft-gate.py` was **not run** — so the craft floor is unproven and must not be reported as clean.
3D occlusion at the enlarged stage is not recorded. The ContextMenu-over-WebView2 airspace claim is
Inferred. The tree's keyboard menu entry (DC-214) is not recorded. The Archetype Signature drafted by
the lens is **Inferred** — its terminals were not checked against the grammar's EBNF. The lens did not
clear its own work: WCAG 2.2 AA and state completeness are UX & Accessibility's veto, the Ruling 94
amendment is the Owner's, and US-K2 and US-K8 need to be made falsifiable by the Test Architect —
both are spec prose with no gate today, which is why both regressed.
