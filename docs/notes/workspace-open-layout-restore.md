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

## The product fork — RESOLVED (2026-08-31, product owner)

The user chose **keep the current/default arrangement when opening a workspace**: opening a workspace
must not restore a per-workspace saved layout. This supersedes US-9's restore-on-open.

- **A — per-workspace layouts (was current, US-9):** ~~each workspace remembers its own arrangement; opening restores it.~~ **Not chosen.**
- **B — keep-current-on-open (chosen):** the arrangement is kept when a workspace is opened; opening never rearranges the panes.

## Shipped

`WorkbenchShell` no longer calls `Persistence.Restore()` on workspace-open — `KeepArrangementOnWorkspaceOpen()`
keeps `Service.Current` and records the event via `WorkbenchDiagnostics`. Persistence still *saves* the
arrangement per workspace (the data is kept and a setting could re-enable restore later), but it is not
auto-applied on open. The earlier degenerate-restore guard (`LayoutRestoreGuard`) is removed — with no
restore-on-open, there is no restore to guard. Verified: App suite green; smoke-clean.

## Superseded design (kept for the record)

The original per-workspace restore + the interim `LayoutRestoreGuard` (keep-current-when-restore-drops-graph)
are superseded by "never restore on open." The root cause below is why the interim guard existed.
