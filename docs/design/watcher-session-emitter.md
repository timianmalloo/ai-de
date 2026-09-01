---
id: design-watcher-session-emitter
title: "Loomkeeper Session Coordination Emitter - Auto-Emitting Session Wrapper (conn-8)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, design, coordination, emitter, session, liveness, conn-8, phase-2]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: design-watcher-coordination-contract, rel: depends-on }
  - { to: design-watcher-phase1-skeleton, rel: depends-on }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The app-side writer that makes a terminal/agent session appear in the watcher: a pure, testable
  SessionCoordinationEmitter (Register/Heartbeat/HeartbeatAll/End/Reconcile) over coordination-contract
  logs, plus the WorkbenchShell loop that reconciles the live terminal panes into coordination sessions
  and pumps them into the store. Also closes the session-end-that-never-ended liveness gap (DC-064).
---

# Session Coordination Emitter (conn-8)

## Problem & spec trace

The watcher only ever showed a session if *something* wrote a coordination-contract log under
`<dataDir>/loomkeeper-coord` (spec US-4 registration; DC-063 residual risk). Nothing in the tool
wrote one, so a terminal the operator opened was invisible to the watcher — the UX was wired but
inert against real terminals. conn-8 supplies the missing **writer** and drives it from the shell so
opening a terminal pane makes a live session appear (US-4/US-6), and closing it ends the session
(US-6 lifecycle).

## Contracts

- `SessionCoordinationIdentity` (`sealed record`): the non-secret attributes a session presents when it
  registers — repo path/display, worktree branch/path, terminal id, agent name, and optional
  harness/model. `ToAttributes()` maps them onto the `OtelAttributes` keys the register event carries
  (an absent harness/model is omitted, US-13). The registrar assigns trust; the emitter never claims it.
- `SessionCoordinationEmitter`: `Register(externalId, identity)`, `Heartbeat(id)`, `HeartbeatAll()`,
  `End(id)`, `Reconcile(currentIds, identityFor)`, `LiveCount`. It writes register/heartbeat/session-end
  through a `CoordContractWriter` and tracks the live id set behind a lock. Idempotent register (a
  re-seen id heartbeats, never re-registers); an unknown heartbeat/end is a no-op.
- `Reconcile(currentSessionIds, identityFor)`: the driver contract — register a newly-seen session,
  heartbeat one already live, end one that has gone. This lets the caller drive the emitter from a
  periodic **snapshot** of "which sessions exist now" without precise start/close events.
- `WatcherHost.CreateEmitter()` / `CoordLogDirectory`: the seam the app uses to obtain an emitter bound
  to the host's coordination directory.

## The shell driver

`WorkbenchShell.StartWatcher` opens the host, calls `host.CreateEmitter()`, and starts a background
loop (`WatcherLoopAsync`, off the UI thread) that every 2s:
1. snapshots the terminal surfaces in the current layout (`TerminalSnapshot()` — Core layout data, safe
   to read off-thread: `Service.Current` is an immutable layout);
2. `emitter.Reconcile(ids, IdentityFor)` — a new pane registers, an existing one heartbeats, a closed
   one ends;
3. `host.PumpOnce()` — folds the coordination log into the store.

`IdentityFor` derives the identity from the workspace root (repo path/display) + the surface id
(terminal id) + the surface title (agent name); branch is `"workspace"` (the shell does not track the
worktree branch). All required attributes are non-empty, so a register is never quarantined
(LK-0004). A hiccup on any tick is swallowed — watcher work never takes down the workbench.

## The liveness gap this closes (DC-064)

`InjectedContractIngest.ContractSessionEnd` removed the external→internal id mapping but never marked
the internal session ended, so liveness (a projection reading `IsEnded`) kept reporting **Alive** for
a session that had ended. Fixed: `IngestHost.EndSession(sessionId) => _store.MarkEnded(sessionId)`,
called from the `ContractSessionEnd` case before the mapping is removed. Control: the emitter's
`End_...` and `Reconcile_...` tests, both mutation-verified.

## Failure modes & dispositions

| Mode | Disposition |
|---|---|
| Register with a missing required attribute | The registrar throws `MalformedEvent` (LK-0004) and quarantines it (counted, no crash). `IdentityFor` supplies non-empty values so this cannot happen from the shell. |
| Heartbeat/End for an unknown id | No-op (guarded by the live set). |
| Re-register the same id | Idempotent — heartbeats instead (the `_live.Add` guard). |
| Session-end that leaves liveness Alive | Closed by DC-064 — `EndSession` marks the store, liveness reads Ended. |
| Reconcile snapshot read off the UI thread | Safe — `Service.Current` is an immutable layout snapshot. |
| A pane vanishes between ticks | Ended on the next tick (≤2s); residual risk recorded (DC-064). |
| A reconcile/pump throws | Swallowed per tick; the loop continues; the workbench is never blocked. |

## Boundary set

empty snapshot (no terminals) · single terminal · two terminals then one closes · re-register (same
id twice) · heartbeat unknown · end unknown · absent harness/model (omitted) · end→liveness Ended.

## Residual risk

Ends are snapshot-driven (≤2s latency), not per-pane close events; on host dispose tracked sessions
are dropped without an explicit end (they go Stale, then the host DB is disposed). The async shell
loop timing is not unit-tested — covered by the Core end-to-end reconcile test + manual smoke.
