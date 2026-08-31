---
id: "note-20260831-panel-reorder-and-search-breadth"
title: "Panel reorder on redraw, and graph search breadth — root-caused, deferred to coordinated work"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "phase-3"
tags: [decision-note, layout, avalondock, search, ux]
links:
  - { to: adr-0012-docking-shell-library, rel: relates-to }
links-suggested: []
review-by: 2027-02-28
review-suggested: []
summary: >-
  Two findings from live testing of the new surfaces. (1) Opening a tab reorders panes because a
  native AvalonDock drag is never captured in the owned Layout model, so the full rebuild-from-model
  on every surface add reverts the user's arrangement — a real reverse-sync gap, deferred because it
  touches the keyboard/drag-identical and persistence invariants and is untestable headlessly.
  (2) The graph search box filters only the already-loaded node LABELS client-side; content/keyword/
  topic search needs a Core query (and file grep is a new Core capability under DC-022).
---

# Panel reorder on redraw, and graph search breadth

**Found 2026-08-31** during the user's live testing of the class diagram / code viewer / prompt
editor surfaces. Both are real, both are root-caused, both are deferred to careful coordinated work
rather than a solo overnight change — with the reasons recorded here so the next session starts from
the diagnosis, not the symptom.

## 1. Opening a tab reorders existing panes

**Symptom (user):** *"adding new tabs is still re-ordering panels — I had the graph on the left but
opening the class diagram moved the graph pane back to the right on the redraw."*

**Root cause (Verified).** The owned `Layout` model is the single source of truth, and
`WorkbenchAdapter.Render()` rebuilds AvalonDock wholesale from it (`Manager.Layout = new LayoutRoot`)
on **every** surface add. AvalonDock's `LayoutDocument` is draggable by default, and a native drag is
**never reconciled back into the model** — the adapter intercepts `DocumentClosing` (→ `CloseSurface`
Apply) but nothing equivalent for a move. So a pane the user dragged is a view-only arrangement the
model does not know about, and the next `Render()` (triggered by opening any surface) reverts it to
the model's arrangement. Moves made through the **command** path (`MoveSurface` via menu/keyboard) do
survive, because they go through `Apply` — which is exactly why the drag path does not.

**Why deferred, not fixed now.** The fix is a reverse-sync: on a completed native drag, read
AvalonDock's resulting tree and Apply the equivalent `LayoutOperation.MoveSurface(id, DropTarget)`
(the op is expressive enough — `Float`/`JoinStack`/`SplitLeft|Right|Top|Bottom`). It is App-capable
(no Core change needed — `MoveSurface` already exists), but it is **not** a safe unsupervised change:
it touches the load-bearing *"keyboard path and drag path provably identical"* accessibility invariant
(ADR-0012), it feeds layout **persistence**, it risks a render loop (Apply → Render → LayoutUpdated),
and it is effectively **untestable headlessly** — it needs a human dragging real panes to validate.
It should be done as a focused, supervised piece with the user awake to confirm the arrangement holds
across add / rename / close / restart.

**Recommended approach for the next session.** Hook the DockingManager's drag completion (AvalonDock
raises layout changes when a `LayoutDocument`/`LayoutDocumentPane` is re-parented); from the realized
tree compute, per moved surface, the `DropTarget` that reproduces its new home; `Apply` those moves so
the model becomes authoritative again; then the existing `Render()` is idempotent and preserves the
arrangement. Guard against the render loop by suppressing reconciliation while `Render()` is running.

## 2. Graph search is label-only

**Symptom (user):** *"search is not very functional — I need to search on keywords and topics, so on
content vs metadata… maybe search needs to search the graph AND grep the files?"*

**Root cause (Verified).** The graph canvas search box (`applySearch` in `CanvasPage`) is a
**client-side filter over the ~1,500 already-loaded nodes**, matching their **label** substring only.
It never queries the store, so it cannot find a node that was not drawn, and it cannot match on
content, attributes, or topic — only on the visible label text.

**Why it is (mostly) Core work.** Two separate asks:
- *Search the graph by content/keyword* → the store query `IWorkspaceQueries.FindAsync` is the right
  seam, but it must index/search more than labels (attributes, declared context, doc/knowledge
  content) — a **Core** projection change. The App follow-up is to point the canvas search box at
  `FindAsync` (and re-root / highlight the results) instead of the client-side label filter.
- *Grep the files* → the App **must not read workspace files** (DC-022, two content authorities), so a
  file-content search is a **new Core capability** (a content-search query over the indexed corpus, or
  a daemon-side grep), surfaced by the App.

**Recommended split.** Core: broaden `FindAsync` (or add a `SearchContentAsync`) to match content /
attributes / topic and, if wanted, a file-grep. App: replace the canvas label-filter with a call to
that query, keeping the keyboard-first `/` affordance and the focus trap intact.

## Status

Both are recorded as handoffs in `docs/collaboration/session-contracts.md §4c`. The class-diagram
empty-state bug and the missing whole-graph "Overview" affordance found in the same testing pass were
fixed and landed this session.
