---
id: design-watcher-host
title: "Loomkeeper - In-Process Watcher Host (connective 5)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, host, ingest, coordination, liveness, in-process, phase-4]
links:
  - { to: design-watcher-otlp-receiver, rel: depends-on }
  - { to: design-watcher-coordination-contract, rel: depends-on }
  - { to: design-watcher-board-leaderboard-surfaces, rel: refines }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Compose the observation store, trusted registrar, ingest host, injected coordination-contract ingest +
  log pump, and (best-effort) the OTLP receiver into one WatcherHost, and run it IN THE WPF APP PROCESS.
  Running the ingest beside the read surfaces makes liveness exact (the registrar and liveness projection
  share one process-global monotonic clock), which is the cross-process caveat conn-2 recorded, now
  removed. The host drains the coordination-contract log on a 2s background loop so a session that writes
  a register/heartbeat log appears live without a restart. This is the next-step that turns the panes
  from live-capable into live.
---

# Design: In-Process Watcher Host (connective 5)

## 1. Problem & scope

conn-2 wired the read panes to a store but nothing WROTE the store in a running process, so a smoke test
saw only honest-empty panes. The ingest pieces all exist and are tested in isolation (slice 1 OTLP
receiver + `IngestHost`, slice 2 coordination contract + `CoordContractLogPump` + `InjectedContractIngest`,
slice 3 `LivenessProjection`), but nothing composed and ran them. This slice composes them into a
`WatcherHost` and runs it in the WPF app.

**In scope:** the `WatcherHost` composition (store + registrar + ingest host + injected coordination
ingest + log pump + optional OTLP receiver); its `PumpOnce`/`RunAsync`/`TryStartOtlp`; wiring it into
`WorkbenchShell` (share the store with the read queries, run the pump on a background loop, dispose with
the shell); tests through the composed host. **Out of scope:** live pane auto-refresh (the panes fold on
open; a store-changed push is a follow-on - reopening a pane refreshes it); a session wrapper that emits
the coordination log automatically (the session opts in by writing the log); the scoring path (conn-6).

## 2. Composition (Solution-Selection Ladder rung 2 - reuse, not new behaviour)

`WatcherHost.Open(dataDirectory, coordLogDirectory, time?, clock?, staleAfter?)` wires:

- `SqliteWatcherObservationStore` at `<dataDir>/watcher.db` - the one store, shared for reads and writes;
- `TrustedRegistrar(store, CapabilityFactory, clock)` - issues capabilities, records heartbeats on `clock`;
- `IngestHost(store, registrar, time)` - the bounded span queue + drain;
- `InjectedContractIngest(ingestHost)` + `CoordContractLogPump(coordLogDirectory, injected)` - the
  file-based coordination path (a non-AI-Forward session opts in by writing register/heartbeat/session-end);
- `LivenessProjection(store, clock, staleAfter)` - **the same `clock`** the registrar uses.

The host exposes `Store` and `Liveness` (not the query types) so the app builds its `AiDe.Core.Presentation`
queries from them - the host stays in the `Watcher` namespace with no dependency on `Presentation`.

## 3. Why in-process makes liveness exact (removes the conn-2 caveat)

Liveness compares a session's last heartbeat tick against "now", both as monotonic ticks
(`Stopwatch.GetTimestamp()`), which is **process-relative**. conn-2 recorded that a heartbeat written by a
*separate* ingest process is not comparable to the app's clock. By hosting the ingest **in the app
process**, the registrar that records the heartbeat tick and the liveness projection that reads "now" use
the same process-global timestamp source, so the comparison is exact. `Liveness_IsExact_InProcess_SharedMonotonicClock`
pins it: a registered+heartbeated session reads `Alive` through the host's own projection.

## 4. Running it (the app)

`WorkbenchShell` opens the host when a workspace data directory is supplied, builds the four read queries
from `host.Store`/`host.Liveness`, and starts `host.RunAsync(2s, token)` fire-and-forget - so the
coordination log is drained into the store every couple of seconds and the surfaces reflect new sessions
without a restart. The host is owned by the shell and disposed with it (the pump is cancelled first). A
host that cannot be opened degrades to the null-query path (panes show "not available") - the workbench
is never blocked.

## 5. Failure modes & dispositions

| Failure mode | Disposition |
|---|---|
| Host cannot open (I/O, permission, locked db) | Degrades to null queries; workbench still opens (shell catch) |
| A coord log file mid-write during a pump | `RunAsync` catches IO/permission, skips the tick, re-reads next tick (nothing lost - whole-dir re-read) |
| Re-pumping the whole directory | Idempotent - registration keyed by external id (tested) |
| OTLP prefix cannot bind (no URL ACL on Windows) | `TryStartOtlp` returns false; coordination path still runs (best-effort) |
| Span enqueued but not drained | `PumpOnce` drains after pumping coord (tested; mutation-verified) |
| Empty coord directory | `PumpOnce` returns 0, no throw (tested) |

## 6. Test plan

- `WatcherHostTests` (7): coordination register into the shared store; idempotent re-pump; exact in-process
  liveness; the shared store feeding the real Sessions read query (E11); enqueued span drained by PumpOnce;
  empty-directory zero; Open creates the directories.
- The drain wiring is mutation-verified (removing `DrainAvailable` from `PumpOnce` reds the span test).
