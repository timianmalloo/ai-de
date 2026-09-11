---
id: inv-0006-workbench-pane-swap-on-native-tab-drag
title: "Moving one tab swaps both panes: the workbench has no drag-completed hook"
type: investigation
status: accepted
owner: "@timianmalloo"
phase: "facelift"
tags: [workbench, docking, avalondock, layout, zones, drag, reconcile, observability]
links:
  - { to: adr-0012-docking-shell-library, rel: depends-on }
  - { to: adr-0021-named-dock-zones, rel: refines }
  - { to: inv-terminal-crash-and-pane-moves, rel: refines }
review-by: 2026-12-01
summary: >-
  Dragging a tab in the workbench mutates AvalonDock's tree and nothing else — the zone model, which
  is the source of truth, is never told. Nothing subscribes to a drag-completed event; the only
  reconcile runs from four unrelated commands (new terminal, new agent terminal, new prompt draft,
  open reference document). So the view drifts from the model for minutes, and when one of those
  four commands finally fires, `TryMapByPosition` re-derives each zone's identity by MAJORITY CONTENT
  OVERLAP and `ZonesToTree.ToTree` re-renders in the fixed order Left | Center | Right. When the
  drift is large enough that a zone's majority has moved column, the two zone LABELS exchange and
  every pane changes side at once — the "all tabs swapped from right to left" the operator saw.
  Reproduced headlessly against the operator's own recorded model and screenshots. The gesture is the
  library's; the drift, the majority-vote reconcile and the total absence of telemetry are ours, and
  so is the remedy.
---

# Moving one tab swaps both panes: the workbench has no drag-completed hook

> **Investigation report — diagnosis only. No `src/` or `tests/` change made. Fix belongs to node C2.**

Read-only on `src/` and `tests/`. Evidence is the operator's eight screenshots, the app's own
committed telemetry (`%LOCALAPPDATA%/AiDe/logs/workbench-20260911.log`), the persisted zone layout
(`%LOCALAPPDATA%/AiDe/workspaces/aide.31abcd25046a4df98d65044abb06f0f5/layout.zones.json`), and a
headless probe that ran the real `AiDe.Core.Workbench` code against that recorded state.

## 0. What was measured, and what was not

**Measured (Verified).** The screenshot file times, the app's structured log for the session, the
persisted zone model, the build the operator ran, and every code path named below (opened, not
recalled). The probe executed `ZoneBackedLayoutService.ReconcileFromView` and `ZonesToTree.ToTree`
— the shipped code, not a re-implementation — against the model the log recorded.

**Not measured (Inferred, and named as such).** The exact pointer gesture and AvalonDock drop target
at 05:58:43. **It cannot be recovered, and that is finding F1**: a drag produces no telemetry at all.
The log has *nothing* between `12:55:02Z` and `12:59:50Z`, which spans screenshots 4, 5 and 6 —
including the defect itself. I could not drive the real surface either: reproducing the defect needs
a pointer drag against a live `DockingManager`, which the probe projects
(`AiDe.App.TerminalProbe` / `CanvasProbe` / `WebHostProbe`) do not do. **This investigation did not
reproduce the operator's gesture. It reproduced the mechanism.**

## 1. The timeline, from the operator's clock and the app's own log

Screenshot mtimes (local, PDT) against `workbench-20260911.log` (UTC = local + 7h):

| local | source | what |
|---|---|---|
| 05:51:46 | log `terminal.start terminal-1` | app start, default layout |
| **05:52:49** | **shot 0** | left `Explore Provenance Contexts Joins` · right `Graph Domain Sessions Board Leaderboard Ledger` · bottom `Terminal — pwsh` · *"No workspace open"* |
| 05:52:58 | log `layout.mutation workspace-open / restore-zones` | zone-left `[graph,explore,provenance,contexts,joins]`, zone-center `[domain,sessions,board,leaderboard,ledger]` |
| **05:53:48** | **shot 1** | left `Graph Explore Provenance Contexts…` · right `Domain Sessions Board Leaderboard Ledger` · **bottom terminal gone** |
| 05:54:57 | shot 2 | (task-class UX — not mine, see §7) |
| 05:55:02 | log `layout.mutation open-session-document / split-beside-graph` | session document appended to zone-center |
| **05:56:01** | **shot 3** | identical to shot 1, plus `2026-09-11 session` last in the right pane |
| **05:57:39** | **shot 4** | left pane now leads with `2026-09-11 session` — **an unlogged native drag** |
| **05:58:43** | **shot 5** | **both columns have exchanged sides** |
| **05:59:20** | **shot 6** | one tab moved, columns stayed put |
| 05:59:50 | log `terminal.start terminal#b356ea` | first reconcile-bearing command in 4m48s |
| **06:00:19** | **shot 7** | new terminal placed as expected |
| 06:00:57 | `layout.zones.json` saved | Left `[graph,domain,explore,sessions]` · Center `[ledger,leaderboard,board,session,provenance,contexts,joins]` · Bottom `[terminal#b356ea]` |

The operator ran `C:\Projects\ai-de\src\AiDe.App\bin\Debug\net10.0-windows\` (visible in shot 0's
terminal prompt), built 2026-09-10 20:08 — **after** `TryMapByPosition`/`ReconcileFromView` landed
(`181dd41`, 2026-09-02). Not a stale build. (Verified.)

## 2. Root cause of step 5 — the swap

### The two representations, and the hook that does not exist

`WorkbenchAdapter` is documented as one-way, model → view:

`src/AiDe.App/Workbench/WorkbenchAdapter.cs:18-21`
> *"The adapter is deliberately **one-way**: model → view. Pointer gestures enter as
> `LayoutOperation` requests through `ILayoutService.Apply`, never as direct view mutations."*

**That second sentence is not true of the running app.** The app's own pointer pipeline —
`DropTargetResolver.Resolve` → `WorkbenchController.DragOver` → `Controller.Drop` →
`LayoutOperation.MoveSurface` → `ZoneLayoutService.MovePane` — has **no production caller**:

```
$ grep -rn "\.DragOver(\|\.Drop(\|CancelDrag(" src/ --include=*.cs --include=*.xaml | grep -v WorkbenchController.cs
src/AiDe.App/Workbench/WorkbenchShell.cs:1242:        Controller.DragStateChanged -= canvas.SetObscured;
src/AiDe.App/Workbench/WorkbenchShell.cs:1243:        Controller.DragStateChanged += canvas.SetObscured;
```

Only the event is subscribed — and `SetDragging(true)` is reached **only** from `DragOver`
(`WorkbenchController.cs:254`), so even the canvas still-frame swap never fires. The entire pointer
path is dead. (Verified, exhaustive grep.)

What actually happens on a tab drag is AvalonDock's own drag, mutating `Manager.Layout` directly.
Nothing observes it. `Manager.LayoutUpdated` is subscribed twice —
`WorkbenchAdapter.cs:52` (accessible names + tab decoration) and `WorkbenchShell.cs:511`
(`Persistence.MarkDirty()`) — and **neither touches the model**. So `MarkDirty` schedules a save of
a model that does not contain the user's drag.

The model is told only by `ReconcileViewIntoModel()`, called from exactly four places, all of them
*other* commands:

```
src/AiDe.App/Workbench/WorkbenchShell.cs:189   NewAgentTerminalRequested
src/AiDe.App/Workbench/WorkbenchShell.cs:257   NewTerminalRequested
src/AiDe.App/Workbench/WorkbenchShell.cs:300   NewPromptDraftRequested
src/AiDe.App/Workbench/WorkbenchShell.cs:1556  OpenReferenceDocument
```

**There is no drag-completed hook. This is the root cause.** Everything below is what that absence
does. In the operator's session the drift ran from 05:56:01 to 05:59:50 — **3m49s and at least three
drags** — before any command folded it back in.

### Why the drift turns into a *swap* rather than a mis-drop

Two facts compose:

1. `ZonesToTree.ToTree` renders the columns in a **fixed canonical order**, and a zone's *label*
   therefore decides its screen position:

   `src/AiDe.Core/Workbench/ZonesToTree.cs:42-52`
   ```csharp
   // The columns row: left | center | right, omitting collapsed/empty tool columns.
   var columnParts = new List<(LayoutNode Node, double Weight)>();
   if (left is not null) { columnParts.Add((left, zones.Zone(ZoneId.Left).Extent)); }
   columnParts.Add((center, CenterColumnWeight(zones)));
   if (right is not null) { columnParts.Add((right, zones.Zone(ZoneId.Right).Extent)); }
   ```

2. `TryMapByPosition` re-derives each zone's label from the view by **majority content overlap**:

   `src/AiDe.Core/Workbench/ZoneBackedLayoutService.cs:111-133`
   ```csharp
   int? AnchorFor(ZoneId z)
   {
       var owned = current.Zone(z).Surfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
       if (owned.Count == 0) { return null; }
       var best = -1; var bestCount = 0;
       for (var i = 0; i < colChildren.Count; i++)
       {
           var count = SurfacesUnder(colChildren[i]).Count(s => owned.Contains(s.SurfaceId));
           if (count > bestCount) { bestCount = count; best = i; }
       }
       return best >= 0 ? best : null;
   }
   ```

Majority overlap is stable only while each zone keeps most of its surfaces in one column. Accumulate
enough unreconciled drags and a zone's majority crosses to the other column; the label follows it;
`ToTree` then re-renders in canonical order — and **both columns change side at once**. No pane was
dragged across. The *labels* moved.

**Reproduced (Verified).** The probe ran the shipped `ReconcileFromView` on the operator's
screenshot-6 view against the model the log recorded as live at that moment
(`Left=[graph,explore,provenance,contexts,joins]`, `Center=[domain,sessions,board,leaderboard,ledger,session-document]`):

```
view (screenshot 6): left=[Graph,Domain,Explore,Sessions,Board,Leaderboard,Ledger]  right=[session,Provenance,Contexts,Joins]
ReconcileFromView -> True
next Render()     -> zone-left: 2026-09-11 session | Provenance | Contexts | Joins
                 ||  zone-center: Graph | Domain | Explore | Sessions | Board | Leaderboard | Ledger
```

The reconcile **succeeds** — it does not refuse, it does not warn, it reports nothing — and the next
render puts the right column on the left and the left column on the right. That is the operator's
"all tabs swapped from right to left and vice versa", produced from their own data by the shipped
code. The class is confirmed. (Verified.)

### The asymmetry: it is not directional

**The brief's premise that right-to-left differs from left-to-right is not supported.** Direction is
incidental. The behaviour is asymmetric in the *arithmetic*, not the compass:

- a move that leaves every zone's majority in the column it already anchored is applied faithfully —
  **step 6**;
- a move that carries a zone's majority across (or that lands on a view which has drifted far enough
  that the majorities no longer match the columns) flips the labels — **step 5**.

There is a second, sharper flip in the same function. `anchorZone` is keyed by column index and the
Center's write is unconditional:

`src/AiDe.Core/Workbench/ZoneBackedLayoutService.cs:147-151`
```csharp
var anchorZone = new Dictionary<int, ZoneId>();
if (AnchorFor(ZoneId.Left) is { } li)  { anchorZone[li] = ZoneId.Left; }
if (AnchorFor(ZoneId.Right) is { } ri) { anchorZone[ri] = ZoneId.Right; }
anchorZone[centerIndex] = ZoneId.Center; // the Center anchor wins any tie with a side
```

If `AnchorFor(Left) == centerIndex` — which is guaranteed the moment a tool zone's surfaces all sit
in the Center's column — the Left entry is **silently overwritten**, Left is left with no anchor at
all, and every unanchored column is then placed by raw position
(`i < centerIndex ? ZoneId.Left : ZoneId.Right`, line 155). The comment says the Center "wins any tie";
the code discards the loser's identity entirely. [Major] (Verified by reading; the collision is
unconditional, not a tie-break.)

### Honest limit on step 5 specifically

I can state the mechanism with a worked, reproduced instance. I **cannot** state which pointer
gesture at 05:58:43 supplied the flip, because nothing recorded it. Candidate view states I tested
(`join right column`, `join at index 1`, `new column far right`, `new column between`, `new column far
left`) all reconcile *faithfully* from the screenshot-4 model — so either the drop AvalonDock
performed was not one of those five, or more than one gesture occurred between the two screenshots.
**Inferred**, and the gap is F1.

## 3. Steps 1 and 3 — one case, not two, and not intermittent

**They do not contradict. They are the same arrangement, observed 2m13s apart.**

- Shot 0 (05:52:49) is `WorkbenchLayout.Default()` exactly — `Left=[explore,provenance,contexts,joins]`,
  `Center=[graph,domain,sessions,board,leaderboard,ledger]`, `Bottom=[terminal-1]`
  (`src/AiDe.Core/Workbench/ZoneLayout.cs:129-152`), with *"No workspace open"* in the status bar.
- At 05:52:58 — **nine seconds later** — the log records
  `layout.mutation workspace-open / restore-zones`, from `RestoreArrangementOnWorkspaceOpen()`
  (`WorkbenchShell.cs:1635-1641`). Opening the workspace **replaces the entire arrangement** with the
  saved per-workspace one.
- Shot 1 (05:53:48) shows exactly the restored layout the log records.
- Shot 3 (05:56:01) shows **the same arrangement**, plus the session document appended at 05:55:02.
  No restore happened in between.

So: one mechanism, one event, two readings — "odd" before the operator recognised it, "that's ok"
after. (Verified: log + both screenshots.)

**Why it reads as "without me doing anything":** the restore is silent. `LayoutPersistence.Restore()`
builds the sentence *"Restored your saved workbench arrangement."* (`LayoutPersistence.cs:81-82`) and
the caller **throws it away**:

`src/AiDe.App/Workbench/WorkbenchShell.cs:1637`
```csharp
var restore = Persistence?.Restore();
WorkbenchDiagnostics.LayoutMutation(
    "workspace-open", restore is null ? "keep-current" : "restore-zones", "layout", null, Service.Current);
```

`restore` is used only for a null test. The system knows precisely what it did and says nothing. It
already has an `IWorkbenchAnnouncer` and a visible status line. [Minor, one line.]

**A second defect visible in the same pair, not in the brief.** Shot 0 has `Terminal — pwsh` docked
at the bottom with a live PowerShell process; shot 1, after the restore, has **no bottom zone at
all**. `RestoreZones` replaces the whole `WorkbenchLayout`, so a running terminal that is not in the
saved file is dropped from the layout without confirmation — the same class of loss that
`CloseSurface` guards with a "this terminal is running something" prompt
(`WorkbenchAdapter.cs:399-416`) and that DC-029 protects against on rebuild. [Major] (Verified.)

## 4. Steps 0 and 7 — what the working controls prove

They are not filler; they isolate the failure to one path.

- **Shot 0** proves the *model → view* direction is sound: `WorkbenchLayout.Default()` renders
  surface-for-surface as drawn, in the canonical Left | Center | Right order, with the bottom zone
  present. Projection and render are not implicated.
- **Shot 7** proves the *command* path is sound: `NewTerminalRequested` (log `terminal.start
  terminal#b356ea`, 05:59:50) goes `ReconcileViewIntoModel()` → `Apply(AddSurface)` → `Render()` →
  `ActivateInView`, and the terminal lands in the Bottom zone "where i would expect". Model
  operations place correctly and *are* logged.

Between them they localise the defect precisely: **not the model, not the projection, not the
commands — only the view → model direction, which has no trigger.** Shot 7 also carries the sting:
it is the moment 3m49s of accumulated drift was finally folded in, by a command that has nothing to
do with layout.

## 5. Library or ours — answered: **ours**

- **AvalonDock (Dirkster.AvalonDock 5.0.0, `Directory.Packages.props:18`) owns the gesture only.**
  It moved a tab in its own tree. That is what a docking library is for.
- **The swap is not something AvalonDock can express.** It has no concept of zones. The Left | Center
  | Right ordering exists solely in `ZonesToTree.ToTree`, and a whole-column exchange can only be
  produced by a zone *relabel* followed by that canonical re-render. I reproduced the relabel in
  `ZoneBackedLayoutService.TryMapByPosition` against the operator's own state (§2).
- **The drift, the majority-vote reconcile, the silent success, and the absent telemetry are all
  ours.**

**This is not a vendor-boundary question and should not be routed as one.** Every lever is in
`AiDe.App` and `AiDe.Core`.

## 6. Why nothing catches it — the more useful half

There *is* a layout test suite, and it is substantial. It cannot catch this, by construction.

| control | what it asserts | why it is blind |
|---|---|---|
| `tests/AiDe.Core.Tests/ZoneBackedLayoutServiceTests.cs` — 8 drag-shaped tests incl. `Restore_AfterANativeDragBetweenZones_FollowsTheDropByPosition_NotByKind`, `…KeepsTheExplorersInLeft_NoScatter`, `ReconcileFromView_WhenPositionMappingFails_LeavesTheModelUnchanged_NoScatter` | that `ReconcileFromView` maps a hand-authored view tree correctly | every case feeds a view that is **one drag away from the model**. Production reconciles a view that is *n* drags and minutes away. The drifted case is the only one that fails, and it is the only one not tested. |
| `tests/AiDe.App.Tests/WorkbenchControllerTests.cs` — `DragOver_*`, `Drop(...)`, 6+ tests | that the pointer drag path resolves, previews, announces and applies correctly | **the path has no production caller.** These tests pass, and prove nothing about the app. |
| `tests/AiDe.App.Tests/WorkbenchAdapterTests.cs` — `ReadLayoutFromView_RoundTripsTheRenderedModel…` | that a view *rendered from the model* reads back unchanged | round-trips the un-drifted case. A drag is exactly the case where view ≠ model. |
| `WorkbenchDiagnostics.LayoutMutation` | pane placement, adds, closes | called only from explicit commands. **A drag emits nothing** — verified: zero records across the whole defect window. |

The suite asserts the *design*. Production runs a different path. That is why the operator, not CI,
found this. There is no test anywhere that starts from "the user dragged a tab".

## 7. Handed on, not investigated (per the brief)

- **Step 2** — "still dont know why the task class exists". → **node U1** (Task Class UX).
- **Step 4** — Explore / Provenance / Domain render the same list. I can confirm from shot 4 that all
  three panes show a byte-identical node list, so the redundancy is real and not a rendering
  artifact; whether the answer is "merge them" is information architecture. → **node U1**.

## 8. The fix

### Smallest correct fix — F1 only

**Give the drag a completion hook, so the reconcile is never deferred.** One subscription, in
`WorkbenchAdapter`'s constructor beside the two that already exist there:

- raise an event when `Manager.LayoutUpdated` fires **and** the view's surface topology differs from
  the last rendered projection (the adapter already computes the projection, and
  `ReadLayoutFromView()` already refuses any shape it cannot map losslessly);
- have `WorkbenchShell` subscribe it to `ReconcileViewIntoModel()`.

This is the smallest change that makes every drag reconcile against a model that is **exactly one
drag old** — precisely the regime `ZoneBackedLayoutServiceTests` already proves correct. It needs no
change to `TryMapByPosition` at all: the majority heuristic is sound over a single drag and only
degrades over accumulated drift. Owner: **C2**. Cost: one event, one handler, one guard against
re-entrancy from the `Render()` that reconcile precedes.

Three defects that must ride with it, because they are the same absence:

- **F2 [Major].** `ReconcileViewIntoModel` must emit a `WorkbenchDiagnostics.LayoutMutation` for
  `operation: "drag"` carrying the zone assignment *before and after*. Without it the next report is
  as unrecoverable as this one. This is the finding I would escalate on its own.
- **F3 [Major].** `ReconcileFromView` returning `false` currently means "the user's drag will silently
  revert on the next render". Announce it (`IWorkbenchAnnouncer` is already wired) and log it.
- **F4 [Minor].** `WorkbenchShell.cs:1637` — announce the restore sentence
  `LayoutPersistence.Restore()` already composed, instead of discarding it. Closes §3.

### Rejected, with the reason

- **Replace the majority heuristic with stable per-column zone identity** (tag each rendered
  `LayoutDocumentPane` with its `ZoneId` and read it back rather than inferring it). This is the
  *right* long-term shape and removes the flip class outright — but it is an ADR-0021-level change to
  the adapter contract, it is not needed once the drift is gone, and it would land on a code path
  that still has no telemetry. **Do it after F1+F2, with the trace to prove it.** Rejected now on
  smallest-correct grounds, not on merit.
- **Wire the dead `DragOver`/`Drop` pointer path to AvalonDock's drag** so gestures become
  `LayoutOperation`s. Correct in principle and it is what `WorkbenchAdapter.cs:18-21` claims — but it
  means intercepting the library's drag, which is a far larger change than making the reconcile
  prompt, and `MovePane` is zone-granular (`ZoneLayoutService.cs:18`) so it cannot express a
  within-column drop index. Measured: a model `MoveSurface("graph", zone-left)` **appends** —
  `Domain | Explore | … | Graph` — where the operator's step-6 drag put Graph *first*. Routing drags
  through it today would replace one wrong placement with another. Rejected.
- **Delete the dead pointer path.** Tempting under HYG-A, but it is the keyboard move path's other
  half (SC 2.5.7) and the accessibility equivalence argument rests on it. Not a hygiene call —
  flagged for the UX & Accessibility lens, not removed.

### Fix order

`F1 → F2 → F3 → F4`, then re-measure before considering the rejected structural change.

## 9. Falsifiable clause

**Fails if:** after any single reconcile of a view against the model, a surface that did not move in
the view changes column in the next render.

**Oracle, headless, assertable today.** `WorkbenchLayout.Shape()` already exists as exactly this
oracle (`ZoneLayout.cs:218-226`). For a view `V` derived from `Render(M)` by moving one surface `s`:

```
var before = M.Shape();
svc.ReconcileFromView(V);
var after  = svc.Zones.Shape();
// every surface except s must occupy the same zone in `after` as in `before`
```

This is a pure `AiDe.Core` test — no WPF, no driven surface. It would have failed on the operator's
screenshot-6 input (§2), where moving `graph` relocated **eleven** bystander surfaces.

**What it cannot assert headlessly, stated plainly.** It cannot prove AvalonDock's drop produced the
view tree the test feeds in. That link needs a driven surface — a `WorkbenchProbe` on an STA thread
hosting a real `DockingManager`, rendering the model, performing a real drop, and asserting
`Shape()`. The three existing probes (`TerminalProbe`, `CanvasProbe`, `WebHostProbe`) are the
precedent; none drives the docking host. **Recommended, and scoped as its own node** — the headless
oracle above is the floor, not the whole proof.

## 10. What in this brief turned out to be false

1. **"Step 5 is THE defect."** Step 5 is the *symptom*. The defect is F1 — no drag-completed hook —
   and it was already live at step 4, where the session-document tab's move went unrecorded. Fixing
   the swap without fixing the drift would move the failure, not remove it.
2. **"The behaviour is asymmetric… why right-to-left differs from left-to-right."** Not supported.
   Direction is incidental (§2). The asymmetry is whether the move carries a zone's majority across a
   column boundary. Step 6 "worked properly" for arithmetic reasons, not directional ones — and the
   *same* step-6 view, reconciled, swaps the columns (§2). **Step 6 is not a clean control: it is the
   same defect, not yet triggered.**
3. **"Steps 1 and 3 appear to contradict each other."** They do not (§3). Same arrangement, 2m13s
   apart, one logged cause at 05:52:58, no intervening restore.
4. **"`DockRoundedTabs.cs` is 33 lines — styling, not behaviour; do not start there."** Correct, and
   confirmed — but the list that followed it omitted the two files where the defect actually lives:
   `src/AiDe.Core/Workbench/ZoneBackedLayoutService.cs` (the reconcile) and
   `src/AiDe.Core/Workbench/ZonesToTree.cs` (the canonical order). Of the five files named,
   `SurfaceChrome.cs` (45 lines) and `DocumentPlacement.cs` (56) are not on the drag path at all, and
   `LayoutPersistence.cs` only via §3.
5. **"Is the swap our code's behaviour or the library's? If it is the library's, the fix becomes a
   vendor-boundary question."** The antecedent does not hold — it is ours (§5). No vendor-boundary
   node is needed.
6. **"Step 7 … a control that works."** It works, but it is also the moment 3m49s of drift was
   silently folded in (§4). It is evidence *for* the defect as much as against it.

## 11. Residual risk left open

- **The layout the operator is looking at may not be the layout that gets saved.** `MarkDirty` is
  hooked to `Manager.LayoutUpdated` (`WorkbenchShell.cs:511`) but writes `_zoneService.Zones` — the
  model. Between a drag and the next reconcile, **every save writes an arrangement the user is not
  looking at**, including the flush in `LayoutPersistence.Dispose()`. Closing the app right after
  rearranging — the case that `Dispose`'s own comment calls out as the most common — persists the
  pre-drag layout. Not reproduced; consistent with the persisted file's divergence from shot 6.
  [Major, Inferred.]
- **A collapsed tool zone makes every native drag silently revert.** Measured: with `Left` collapsed
  and non-empty, `ReconcileFromView` returns `false` and the model is untouched, so the next render
  undoes the drag with no message. `TryMapByPosition` pre-seeds `assigned` with `Left`/`Center`/`Right`
  (`ZoneBackedLayoutService.cs:153-158`), so the `TryGetValue` guard at line 181 — whose comment
  promises "a collapsed/empty zone that was not rendered keeps its current content" — never fires for
  those three; the collapsed zone's content is wiped and only the surface-set guard at line 197
  rescues it, by refusing the whole reconcile. **The comment describes behaviour the code does not
  have.** [Major, Verified.]
- **No performance budget is stated for the reconcile.** `TryMapByPosition` is O(zones x columns x
  surfaces) and runs on the UI thread inside command handlers that then `Render()`. At today's ~12
  surfaces this is free; there is no bound and no measurement, and it sits in front of a full
  `Manager.Layout` swap that re-parents every pane (`WorkbenchAdapter.cs:96-106` documents that cost).
  Moving it onto a drag hook makes it run far more often. **Measure it when F1 lands.** [Minor.]
- **`ReconcileFromView`'s success is indistinguishable from a faithful mapping.** It returns `bool`,
  not "what I changed". Even with F2's trace, a reviewer cannot tell a correct reconcile from a
  relabel without diffing `Shape()`. Returning the before/after shape would make F2 trivial.
- The step-5 gesture itself remains unrecovered and always will (§0). Every future report of this
  shape is equally unrecoverable until F2 lands.

## 12. Handoff

- → **C2** (fix node): F1, then F2, F3, F4, in that order. Re-measure before the structural change.
- → **Test Architect**: the §9 oracle, and the `WorkbenchProbe` scope question.
- → **node U1**: steps 2 and 4 (§7).
- → **UX & Accessibility**: the dead `DragOver`/`Drop` path is the pointer half of the SC 2.5.7
  keyboard-equivalence argument. It is not wired. That claim needs re-examining, not deleting.
