---
id: investigation-terminal-crash-and-pane-moves
title: "Investigation — terminal render crash + pane-move relocation"
type: investigation
status: resolved
owner: "@timianmalloo"
phase: "2"
tags: [crash, terminal, rendering, layout, docking, threading, race]
links:
  - { to: architecture, rel: relates-to }
  - { to: investigation-redraw-isolation, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Two workbench-surface defects. (1) CRASH: TerminalView.DrawCursor reads screen[CursorRow,CursorColumn]
  and the indexer is unbounded; after writing the last column the cursor is at CursorColumn==Columns
  (deferred wrap), so at the bottom row the index equals the array length — IndexOutOfRangeException —
  compounded by the screen being mutated on a background thread while OnRender reads it on the UI thread
  (its own "reads between writes" invariant is false). (2) PANE MOVES: the layout is a tree of
  proportional splits, not absolute docks; removing a pane collapses a single-child split into its
  child, which relocates unrelated panes. This report is the diagnosis + phased plan; NO fix applied.
---

# Investigation — terminal render crash + pane-move relocation

**Status: diagnosis only. `/investigate` stops at this report; no fix has been implemented. The phased
plan below awaits your approval of which phases to execute.**

---

## Issue 1 — the crash (Verified)

### Symptom & evidence
Windows Error Reporting + .NET Runtime event log (2026-08-31 18:24), reproduced by the user "with two
sessions active":

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
  at AiDe.Core.Terminal.TerminalScreen.get_Item(row, column)   TerminalScreen.cs:141
  at AiDe.App.Workbench.TerminalView.DrawCursor(context)        TerminalView.cs:234
  at AiDe.App.Workbench.TerminalView.OnRender(context)          TerminalView.cs:126   (Arrange → UpdateLayout → render)
```

### Root cause A — unbounded indexer + deferred-wrap cursor (Verified, necessary+sufficient)
- The indexer is `public TerminalCell this[int row, int column] => _cells[(row * Columns) + column];`
  — **no bounds check.**
- `DrawCursor` reads `_screen[_screen.CursorRow, _screen.CursorColumn]` (TerminalView.cs:234) to draw
  the character under the cursor.
- `Write(char)` leaves the cursor **one past the last column** after filling it: it wraps only on the
  *next* write (`if (CursorColumn >= Columns) { CursorColumn = 0; LineFeed(); }` runs at the *start*).
  So between writes `CursorColumn` can equal `Columns` (the standard "deferred wrap" state).
- At the **bottom-right** (`CursorRow == Rows-1`, `CursorColumn == Columns`) the index is
  `(Rows-1)*Columns + Columns = Rows*Columns` = **`_cells.Length` → out of bounds.**
- **Verified by a throwaway repro** (run and observed, then deleted): fill a 4×3 screen; assert
  `CursorColumn == Columns`, `CursorRow == Rows-1`, and that `screen[CursorRow,CursorColumn]` throws
  `IndexOutOfRangeException`. It does. This is **deterministic and single-threaded** — a render while the
  cursor sits at bottom-right deferred-wrap crashes, no race required. *Necessary:* clamp the index (or
  handle deferred-wrap) and the crash goes; *sufficient:* the state above throws on its own.

### Root cause B — cross-thread read of background-mutated state (Verified by design analysis)
- `TerminalScreen` documents two invariants that are **both false**:
  - *"Nothing here throws on bad input. Every coordinate is clamped"* (line 97) — the **indexer is the
    one access path that does not clamp** (every *mutator* clamps; the reader does not).
  - *"Not thread-safe … the renderer reads it on the UI thread **between writes**"* (line 111) — but
    `TerminalSurface.PumpAsync` runs `_parser.Consume` (→ `Write`/`LineFeed`/`ScrollUp`/`MoveCursor`,
    mutating `_cells`, `Columns`, `Rows`, `CursorRow`, `CursorColumn`) on a **background thread**, while
    `OnRender`/`DrawCursor` reads on the **UI thread**, with **no lock, no snapshot, no marshalling**.
    The "between writes" guarantee does not exist.
- Under **two active sessions** (heavy concurrent output) the render can read a torn (`CursorRow`,
  `CursorColumn`, `Columns`, `_cells`) tuple, widening the crash window well beyond the deterministic
  deferred-wrap case. This is why the user hit it "with two sessions active."

### Relationship to the recent redraw change (Inferred)
The deferred-wrap bug **predates** this session's redraw rework (`DrawCursor`/indexer are unchanged;
the old per-frame `OnFrame` path hit the same `DrawCursor`). The event-driven `RequestRedraw` (pump →
`Dispatcher.BeginInvoke(Render)`) likely **increased the frequency** by triggering a repaint right after
`_parser.Consume` — i.e., exactly when the cursor can be in deferred-wrap — but it is **not the
introducer**. Marked Inferred; the bug is in `DrawCursor` + the indexer + the threading model.

### Ruled out
- *WebView2/HwndHost airspace* — no; `TerminalView` is pure WPF (`FrameworkElement`), and the stack is
  `OnRender`, not a WebView2 path.
- *A resize race on the UI thread* — `Resize` is called from `OnGridResized` (UI thread) and `OnRender`
  is UI-thread, so those two cannot race each other; the race is pump(background) vs render(UI).

---

## Issue 2 — pane moves relocate other panes (Verified by code trace)

### Your mental-model question, answered
> "Are the docks themselves absolute and then panes are contained within docks?"

**No — and that mismatch is the whole cause.** There are **no fixed/absolute dock regions.** The entire
layout is a **tree of proportional splits**: `SplitNode { Orientation, Children, Weights }` and
`StackNode { Surfaces, ActiveIndex }`, rooted at `Layout.Root` (+ a `Floating` list). The default is
`split-root(Vertical)[ split-columns(Horizontal)[workspace, graph], terminal ]`. "Bottom", "left",
"top" are not places — they are wherever the current tree puts a node.

### Root cause — single-child split collapse on removal
Moving a pane is `MoveSurface` = **Detach** then **re-insert**. In `Detach → Remove` (LayoutService.cs):

```csharp
return children.Count switch
{
    0 => null,
    1 => children[0],   // <-- a split with one child collapses INTO that child
    _ => split with { Children = …, Weights = SplitNode.Normalize(…) },
};
```

So when you move the **graph** out of `split-columns`, that split is left with one child (**workspace**)
and **collapses to workspace**. The root goes from `Vertical[ Horizontal[workspace, graph], terminal ]`
to `Vertical[ workspace, terminal ]` — the workspace, which was on the **left**, is now on **top**.
Moving one pane **relocated another**, and the orientation flipped — exactly "the contents of the dock
flipping from the bottom of the application to the top." The collapse is deliberate (it prevents empty
regions) but it restructures the tree, and **every** layout change then triggers a full
`Adapter.Render()` that rebuilds the whole AvalonDock view (the "re-draw").

Two compounding contributors:
- **Source-side collapse** (above) — relocates the moved pane's former sibling.
- **Full view rebuild** — `Adapter.Render()` reconstructs the entire dock tree on every op, so even a
  local model change repaints/relayouts everything.

*Verified by code trace* (the `1 => children[0]` collapse is unambiguous). A characterization test is
proposed in the plan (move a surface out of a 2-child split; assert the sibling's path in the tree
changed) to pin it before any change.

### Not a bug in the "everything moved" sibling-insert (already fixed)
The earlier "why did everything else move" defect was in the **destination** sibling-insert
(SplitRight into an existing same-orientation split) and was fixed (half-of-target weights). **This**
relocation is a **different** mechanism — **source-side collapse** — which that fix did not touch.

---

## Generalization — the failure classes

- **DC-061 — a type promises total input-safety but one access path is unguarded.** `TerminalScreen`
  documents "nothing throws / every coordinate is clamped", yet the indexer bypasses it. Survives
  because the promise makes every caller assume the reader is safe.
- **DC-062 — UI-thread render reads shared state a background thread mutates, with a false
  "single-threaded / reads-between-writes" invariant.** Works under light load; crashes under
  concurrent load (two sessions). The documented invariant is the tell.
- **DC-063 — a proportional-split-tree layout collapses single-child splits on pane removal,
  relocating unrelated panes.** Emergent surprise from individually-correct ops; mismatches the
  "fixed docks, panes contained" mental model.

## Marker harvest (mandatory)
`TerminalScreen` carries two `simplify:` markers (viewport-only; truncate-on-resize) — neither is the
crash. The crash was instead predicted by **two false documented invariants** (lines 97 and 111) — the
`assume:`-class finding: an assumption written as fact and never checked (`no-guessing-protocol.md` NG9).

---

## Phased repair plan

**Phases 1 & 2 IMPLEMENTED** (approved 2026-08-31). Phase 3 awaits the model decision — the user
chose **option A (named absolute dock zones)** and asked for a full `/specify → /ui-design →
/define-architecture` loop before any implementation.

| Phase | Scope (code + tests) | Eliminates | Risk | Status |
|---|---|---|---|---|
| **1 — Stop the crash (deterministic)** | Clamped the indexer (`TerminalScreen.this[row,column]` uses `Math.Clamp` on both coordinates), honouring the type's own "every coordinate is clamped" contract. **Tests:** `ReadingTheCursorCell_AtDeferredWrapOnTheBottomRow_DoesNotThrow` + `TheIndexer_ClampsOutOfRangeCoordinates_AndNeverThrows` — observed failing (5× `IndexOutOfRangeException`) then green. | Root cause A; **stops the crash** | Low | **Done** — landed. |
| **2 — Make the screen safe to render concurrently** | Added `TerminalScreen.SyncRoot`; the pump holds it across `_parser.Consume` (`TerminalSurface.PumpAsync`) and the renderer holds it across the whole frame (`TerminalView.OnRender`), so a frame never sees a half-applied write or a `Resize` array-swap. Kept off-UI-thread parsing (the 1 MiB/s budget). **Test:** `ConcurrentWritesAndReads_UnderSyncRoot_DoNotThrow`. | Root cause B (the race under two sessions) | Med | **Done** — landed. Chose the lock over a snapshot: a per-write immutable grid would allocate under a megabyte-a-second producer. |
| **3 — Pane-move: named absolute dock zones** | **Option A chosen.** Named zones (left / right / bottom / center-document) as stable containers; move between zones only changes those zones' contents; resize = splitter between zones; collapse = zone→rail (panes remembered); maximize = zone fills, reversible. Session/pump lifecycle stays independent of zone view lifecycle; the zone owns the pane visibility/priority signal (see the threading note below). **Running the full `/specify → /ui-design → /define-architecture` loop first.** | Root cause DC-063 (relocation + re-draw) | High | **In design** — spec + UI + architecture before any code. |

### Threading × named zones (design input for Phase 3)
The terminal threading model stays **pane/surface-local, not zone-local**: the race is inside one
pane; WPF has one UI/Dispatcher thread per top-level window (so per-zone UI threads are not possible
for docked panes — only a *floated* window can carry its own Dispatcher thread); and panes migrate
between zones, so zone-scoped threading would churn on every layout op. What the zone model *does*
change: (1) the zone becomes the authoritative owner of the "is this pane actually visible/active"
signal the pane's existing coalescing gate consumes (richer than raw WPF `IsVisible`); (2) zones are
the natural home for render prioritisation/backpressure policy on the single Dispatcher (active zone
gets priority; collapsed/background zones coalesce harder). Principle for the spec: **the zone owns
the visibility/priority signal; the pane owns its threading; the session lifecycle is independent of
zone view lifecycle** (collapse/move must never stop a pump).

## Residual risk / what would change the diagnosis
- Phases 1 & 2 landed with red-then-green tests; the crash the user hit is closed and the render/pump
  race is coordinated through `SyncRoot`.
- Issue 2's "flip" was traced, not reproduced in a harness; the Phase-3 design loop (option A) replaces
  the split-tree model rather than patching it, and will carry its own characterization tests.

## Gate
Adversarial review (self, Adversary Mode): the diagnosis explains **all** the evidence — the exact
stack (DrawCursor→indexer), the "two sessions" trigger (race width), and the "flip bottom→top"
(source-side collapse). Test-Architect: Phase 1 has a failing-first repro (seen). No fix applied;
stopped for human review.
