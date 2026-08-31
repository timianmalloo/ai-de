---
id: note-workspace-open-layout-restore
title: "Decision note — workspace-open layout restore semantics"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [layout, persistence, workbench, us-9]
links:
  - { to: architecture, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Opening a workspace restores its per-workspace saved layout (US-9), which the user experiences as
  "adding a workspace reset my panes" — especially when the saved layout is degenerate (had lost the
  graph). Shipped a conservative guard (keep the current layout when the restore would drop the graph
  it has) and recorded the open product fork: per-workspace restore vs keep-current-on-open.
---

# Workspace-open layout restore semantics

## What the user sees

Loading a workspace (e.g. TheTerrace) rearranges/loses the panes — the user has raised this more than
once ("adding a workspace should not reset the panes"). The screenshot showed a **graph-less, scattered**
workbench.

## Root cause (verified from the saved file)

`WorkbenchShell.AttachWorkspace` calls `Persistence.Restore()` on first workspace-open, which loads the
workspace's `layout.json` and applies it over the current arrangement (US-9 per-workspace layouts).
TheTerrace's actual saved `layout.json` had **no `canvas`/graph surface at all** — two stacks
(explore/…/classdiagram and terminal/claude/classdiagram). So the restore faithfully brought back a
degenerate, graph-less layout. The canvas *is* closable, so blindly re-injecting it is not safe.

## The product fork (open — for the user to settle)

- **A — per-workspace layouts (current, US-9):** each workspace remembers its own arrangement; opening
  restores it. Matches "reopen the workspace, my arrangement returns."
- **B — global layout:** the arrangement is a property of the app, not the workspace; opening a
  workspace keeps the current arrangement.

The user's complaint leans toward B, but A is the implemented, tested feature. This is a genuine product
decision; it is **not** silently changed here.

## Shipped this turn (conservative, model-agnostic guard)

`LayoutRestoreGuard.ShouldKeepPrevious(before, restored)` — when the restore would **drop the graph that
the current layout has**, keep the current layout instead of applying the degenerate saved one, and
announce it. This fixes the exact screenshot (graph-less restore) under *either* product model, changes
behaviour **only** for degenerate restores, and never overrides a valid saved layout. Verified by
`LayoutRestoreGuardTests`; the path taken is recorded via `WorkbenchDiagnostics`.

## Follow-on (needs the user's call on the fork)

If the user wants **B**, the change is: on workspace-open, do not restore-over-current at all (or only
restore when there is no meaningful current arrangement). If the user wants **A** kept, add a
"degenerate saved layout" repair (offer Reset, or drop the graph-less save) so a corrupted save cannot
persist. Deferred to an explicit decision rather than guessed.
