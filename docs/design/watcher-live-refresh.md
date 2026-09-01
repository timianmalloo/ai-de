---
id: design-watcher-live-refresh
title: "Loomkeeper Live Pane Auto-Refresh (conn-9)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, design, refresh, liveness, ux, conn-9, phase-2]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: design-watcher-session-emitter, rel: depends-on }
  - { to: design-watcher-sessions-surface, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The watcher read panes (sessions/board/leaderboard) re-render live when the observation store changes -
  a session registering/ending, a board post, or a new score shows up without a manual reopen - gated by a
  cheap store fingerprint so an idle watcher never gratuitously rebuilds a pane (no scroll reset/flicker).
---

# Live Pane Auto-Refresh (conn-9)

## Problem & spec trace

DC-066 wired the watcher panes, but they folded **once** on build: a session that registered after a
pane opened stayed invisible until the pane was reopened (DC-066 residual risk; spec US-4 live board,
US-6 live sessions). conn-9 makes the open watcher panes re-render as the store changes.

## Design

`WorkbenchShell.WatcherLoopAsync` (the conn-8 reconcile+pump loop) gains a third step after
`PumpOnce()`: compute a cheap **store fingerprint**, and if it changed since the last tick, marshal a
pane refresh to the UI dispatcher.

- **Fingerprint** (`WatcherFingerprint`, pure/static): session count + **each session's liveness state**
  (so a session going Stale/Ended is caught, not only a count change) + episode/board/scorecard counts.
  The liveness-state term is the load-bearing part — a count-only signal would leave a pane showing an
  ended session as live (the DC-067 shape, one layer up).
- **Refresh** (`RefreshWatcherPanesOnUi`, UI thread): invalidate only the stateless watcher pane kinds
  (`sessions`/`board`/`leaderboard`) and `Render()`. A terminal is reconciled, never rebuilt (DC-029),
  so the terminal is untouched. A no-op if the host was reset since the tick was queued.
- **Gating**: refresh only on a fingerprint change, so an idle watcher never rebuilds a pane — no scroll
  reset, no flicker, no gratuitous work. The `WatcherPaneKinds` set is shared with the AttachWorkspace
  invalidate (DC-066) so the two paths cannot drift.

## Failure modes & dispositions

| Mode | Disposition |
|---|---|
| Refresh runs after the host/workspace reset | `RefreshWatcherPanesOnUi` is a no-op when `_watcherHost` is null. |
| Fingerprint read races the UI-thread pane read | Same store-access pattern the existing pump already relies on; reads are on the loop thread, render on the UI thread. |
| A refresh throws | Swallowed by the loop's per-tick catch; the workbench is never blocked. |
| No dispatcher (headless/test) | `Application.Current?.Dispatcher` is null → refresh is skipped; the fingerprint logic is still unit-testable directly. |
| Idle watcher | Fingerprint unchanged → no render (the gating is the whole point). |

## Boundary set

empty store · first session appears · session goes Ended (count unchanged) · board post · new score ·
idle (no change) · host reset between queue and run.

## Residual risk

The async loop timing (2s cadence, BeginInvoke marshalling) is not unit-tested — the pure fingerprint
change-detection is (an App test, mutation-verified); the render marshalling is covered by manual smoke.
A pure reorder of `AllSessions()` with no state change would cause one spurious refresh (harmless).
