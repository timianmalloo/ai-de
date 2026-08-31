---
id: investigation-terminal-cursor-render-crash
title: "Investigation - AiDe.App crash: terminal cursor render IndexOutOfRange (DC-041)"
type: investigation
status: resolved
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, terminal, crash, render, cursor, defect, dc-041, phase-4]
links:
  - { to: design-watcher-host, rel: relates-to }
  - { to: architecture-loomkeeper, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  AiDe.App terminated with an unhandled IndexOutOfRangeException while two agent CLIs (copilot + claude)
  were grounding in a repo. Verified root cause (from the Windows Application event log, reproduced
  deterministically): TerminalView.DrawCursor read the character under the cursor through the raw
  TerminalScreen indexer, but the cursor legitimately sits at the pending-wrap column
  (CursorColumn == Columns) after writing the last column; at the bottom row that indexes one past the
  end of the cell array, and the exception is unhandled on the WPF UI thread inside OnRender, which
  terminates the process. Fixed with a bounds-safe CellUnderCursor() the renderer uses. Registered as
  DC-041. (A separate finding: the watcher UX is not wired into the running app - see below.)
---

# Investigation: AiDe.App terminal cursor render crash (DC-041)

## 1. Symptom (reported)

The user opened AiDe.App on a repo ("healthwatch"), had two terminal panes running the copilot and
claude CLIs, and asked both to ground in the repo. **The application died.**

## 2. Evidence (verified, not inferred)

Windows Application event log, at the time of the crash:

```
Faulting application name: AiDe.App.exe ... Exception code: 0xe0434352 (managed exception)
Description: The process was terminated due to an unhandled exception.
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at AiDe.Core.Terminal.TerminalScreen.get_Item(Int32 row, Int32 column)   TerminalScreen.cs:141
   at AiDe.App.Workbench.TerminalView.DrawCursor(DrawingContext context)     TerminalView.cs:225
   at AiDe.App.Workbench.TerminalView.OnRender(DrawingContext context)       TerminalView.cs:117
   at System.Windows.UIElement.Arrange(Rect finalRect)
   ...
```

This is a first-hand crash stack, not a guess. The fault is a managed `IndexOutOfRangeException` on the
**WPF UI thread** inside `OnRender` → WPF does not catch it → the process is terminated (`0xe0434352`).

## 3. Root cause (reproduced deterministically)

`TerminalScreen`'s indexer is unguarded:

```csharp
public TerminalCell this[int row, int column] => _cells[(row * Columns) + column];
```

`DrawCursor` read the character under the cursor through it:

```csharp
var cell = _screen[_screen.CursorRow, _screen.CursorColumn];
```

But the cursor **legitimately sits off the grid** at the *pending-wrap* column. `Write(char)` writes at
`(CursorRow, CursorColumn)` then does `CursorColumn++`; after writing the **last** column, `CursorColumn`
is left `== Columns` (the pending-wrap state, resolved only on the next write). At the **bottom row** that
index is `(Rows-1) * Columns + Columns == Rows * Columns == _cells.Length` - one past the end. When the
render timer fires in that state on a **focused** terminal, `DrawCursor` throws, unhandled, on the UI
thread → crash.

Agent CLIs hit this continuously: they pin a full-width status/progress/spinner line to the bottom row,
which fills the last column of the bottom row and leaves the cursor at pending-wrap there. "Grounding"
prints exactly such lines. That is why two grounding agents reliably killed the app.

**Why the suite never caught it:** the pending-wrap cursor is *correct* model behaviour (all
`TerminalScreen` tests pass), and the cursor draw is **focus-gated** (`if (!IsKeyboardFocused) return;`),
so no render test - none of which run focused - ever executes the crashing branch. The crash needs a
*focused* terminal, which only happens in real use.

Reproduced deterministically at the screen level: fill the bottom row → cursor at `(Rows-1, Columns)` →
the raw index throws (mutation-verified - see the Proof Pack).

## 4. Fix

- `TerminalScreen.CellUnderCursor()` returns the cell **or null** when the cursor is not on a real cell
  (pending-wrap, or any off-grid position), bounds-checking row and column.
- `TerminalView.DrawCursor` reads the character-under-cursor through `CellUnderCursor()` (never the raw
  indexer) and **clamps the drawn rect** to the last cell, so a pending-wrap cursor shows on the last
  column instead of the margin, and no out-of-bounds index can fault `OnRender`.

`Resize` already clamps the cursor to the new bounds (the stale-cursor-after-shrink sibling is therefore
already safe); a characterisation test pins that.

## 5. Generalisation (DC-041)

**A render / `OnRender` path over untrusted, unbounded content must read every position through a
bounds-safe accessor - never a raw indexer - because an exception there is unhandled on the UI thread and
terminates the process.** The model may legitimately hold an off-grid caret; the *renderer*, not the
model, must be robust. Registered in `docs/lessons/defect-classes.md` as **DC-041**, controlled by the
mutation-verified regression test.

## 6. Separate finding (not the crash): the watcher UX is inert at runtime

While tracing the crash I found that the Loomkeeper read surfaces are **not wired into the running app**.
`MainWindow` builds the shell with `new WorkbenchShell(null)` (no data directory), and the real
runtime path, `WorkbenchShell.AttachWorkspace(...)`, rebuilds the factory as
`new SurfaceContentFactory(queries)` - **dropping the watcher queries and never opening the WatcherHost**.
The watcher wiring lives in the *constructor* (conn-2/conn-5), which only ever receives `null` at
runtime, so Sessions/Board/Leaderboard always render "not available" and no ingest runs. This is a
distinct defect (E2E-C / green-suite-broken-surface: the App tests exercise the factory directly, never
through `AttachWorkspace`). It is not the crash, but it must be fixed for the watcher smoke test to show
anything - tracked as the immediate next step. (This crash fix is committed first, as the urgent
stability fix.)
