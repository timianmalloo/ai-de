---
id: proof-watcher-runtime-wiring
title: "Proof Pack - Loomkeeper watcher wired into the running app (DC-042)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, wiring, composition-root, e2e-c, dc-042, phase-4]
links:
  - { to: investigation-terminal-cursor-render-crash, rel: relates-to }
  - { to: design-watcher-host, rel: depends-on }
  - { to: design-watcher-board-leaderboard-surfaces, rel: depends-on }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that the Loomkeeper watcher read surfaces are now wired into the running app. The wiring moved
  from the shell constructor (which the app builds with a null workspace) into AttachWorkspace (the real
  runtime path, which previously rebuilt the factory without the watcher queries and never opened the
  host), and the already-realized watcher panes are invalidated so they rebuild against the wired factory
  (never a terminal, DC-029). Proven by an E11 test through the real composition root: after attach the
  Sessions pane shows its live empty state, not "not available". App 139/0.
---

# Proof Pack: Watcher wired into the running app (DC-042)

- **Components:** `WorkbenchShell.StartWatcher` (used by the constructor AND `AttachWorkspace`); `WorkbenchShell.AttachWorkspace` (wires the watcher queries + invalidates the watcher surfaces); `WorkbenchAdapter.Invalidate` + `Render` (rebuild-not-reuse for marked surfaces).
- **Tests:** `tests/AiDe.App.Tests/WorkbenchShellTests.cs` — 1 new E11 test; full `AiDe.App.Tests` **139/0**; `AiDe.Core.Tests` **970/0** (Core unchanged); builds clean.

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| After AttachWorkspace the Sessions pane is live, not "not available" | `AttachWorkspace_WiresTheWatcher_SoTheSessionsPaneIsLive_NotUnavailable` | `AttachWorkspace` + `Adapter.Invalidate` | attach a real data dir → the Sessions pane the Adapter builds shows "No sessions observed", not "not available" | **Yes** — before the fix (wiring in the constructor only, no invalidate) the same test asserts and the pane read "not available" | Verified | E11 through the real composition root |
| The full App suite (incl. the Adapter reconcile / terminal-reuse tests) stays green | full `AiDe.App.Tests` run | `WorkbenchAdapter.Render` | 139/0 — terminal reuse (DC-029) and layout reconcile unaffected | Seen green | Verified | only stateless watcher ids are invalidated, never a terminal |

## Testing Strategy triggers applied

- **E11 (prove through the real composition root):** the regression exercises `AttachWorkspace` → `Adapter.Render` → `ContentFor("sessions")` — the exact runtime path — rather than constructing the factory directly (which is what hid the defect). This is the control for DC-042.
- **DC-029 preserved:** the Adapter's `Invalidate` only marks the stateless watcher read surfaces; terminals are never rebuilt (a rebuilt terminal orphans its ConPTY process). The full App suite passing (139/0) confirms the reconcile invariant holds.
- **UI-thread safety:** `StartWatcher` starts the pump loop via `Task.Run`, so even its synchronous first pump (a directory read + SQLite fold) runs off the UI thread during attach.
- **Graceful degradation:** a host that cannot open returns null queries — the panes fall back to "not available" and the workbench still opens.

## Residual risk

- **Empty until a session registers** — the panes are now live and honest, but show empty states until a session writes a coordination-contract log under `<dataDir>/loomkeeper-coord` (or emits OTLP). An auto-emitting session wrapper is the next step for a *populated* smoke test.
- **Fold-once, no live auto-refresh** — a (re)built pane folds the store once; the 2s pump keeps the store current, and reopening a pane re-folds it. A store-changed push that re-folds open panes is a follow-on.
- **App-side pump loop not unit-tested** — the fire-and-forget `RunAsync` is thin; the host's `PumpOnce`/`RunAsync` are unit-tested in Core, and the wiring is proven by the E11 test.
