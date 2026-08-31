---
id: proof-watcher-host
title: "Proof Pack - Loomkeeper In-Process Watcher Host (connective 5)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, proof-pack, host, ingest, coordination, liveness, phase-4]
links:
  - { to: design-watcher-host, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that the in-process WatcherHost composes and runs the ingest: a coordination-contract log
  registers a session into the shared store through the host; re-pumping is idempotent; liveness is exact
  because the registrar and the liveness projection share one monotonic clock in-process; an enqueued span
  is drained by PumpOnce; and the shared store feeds the same Sessions read query the WPF surface folds
  (E11). Wired into WorkbenchShell with a 2s background pump. 7 tests, Core 946/0, App 138/0; the drain
  wiring mutation-verified.
---

# Proof Pack: In-Process Watcher Host (connective 5)

- **Components:** `src/AiDe.Core/Watcher/WatcherHost.cs` (`Open`, `Store`, `Liveness`, `Ingest`, `Stats`, `PumpOnce`, `RunAsync`, `TryStartOtlp`, `Dispose`); `WorkbenchShell` wiring (host owns the store, 2s background pump, disposed with the shell).
- **Tests:** `tests/AiDe.Core.Tests/Watcher/WatcherHostTests.cs` — 7 tests, **7/7**; full `AiDe.Core.Tests` **946/0**, `AiDe.App.Tests` **138/0**; builds clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| A coordination-contract log registers a session into the shared store through the host | `PumpOnce_RegistersACoordinationSession_IntoTheSharedStore` | `CoordContractLogPump` + store | register+heartbeat → 1 session, harness "claude-code" | Seen green | Verified | the file-based smoke-test ingest |
| Re-pumping the whole directory does not double-register | `PumpOnce_IsIdempotent_ReReadDoesNotDoubleRegister` | idempotent register (keyed by external id) | pump twice → still 1 session | Seen green | Verified | safe to run on a loop |
| Liveness is exact in-process (shared monotonic clock) | `Liveness_IsExact_InProcess_SharedMonotonicClock` | one `clock` for registrar + projection | registered+heartbeated session → Alive | Seen green | Verified | removes the conn-2 cross-process caveat |
| The shared store feeds the real Sessions read query (E11) | `SharedStore_FeedsTheSessionsReadQuery_TheSurfacesUse` | `WatcherSessionsQuery(host.Store, host.Liveness)` | pane Ready, 1 row, harness "claude-code" | Seen green | Verified | the exact path the WPF pane uses |
| An enqueued span is drained by PumpOnce | `Ingest_EnqueuedSpan_IsDrainedByPumpOnce` | `PumpOnce` → `DrainAvailable` | SpanCount 1, Stats.Ingested 1 | **Yes** — removing `DrainAvailable` from `PumpOnce` reds this | Verified | mutation-verified oracle |
| An empty coordination directory pumps zero, no throw | `PumpOnce_EmptyCoordDirectory_IsZero_NoThrow` | tolerant read | 0 applied, no sessions | Seen green | Verified | — |
| Open creates the data and coord directories if missing | `Open_CreatesTheDataAndCoordDirectories_IfMissing` | `Directory.CreateDirectory` | both exist after Open | Seen green | Verified | — |

## Testing Strategy triggers applied

- **T4 (real-infra integration):** the host is exercised against a real SQLite file and real coordination-log files on disk (`CoordContractWriter` → `CoordContractLogPump`), not substitutes - only the real filesystem exhibits the whole-directory re-read idempotence.
- **E11 (rendered/read surface):** `SharedStore_FeedsTheSessionsReadQuery` proves the store the host writes is read through the exact query the WPF Sessions pane folds - not a hand-built VM.
- **Composition over invention (rung 2):** every part is separately tested (slices 1-3); the host test proves the *wiring*, and the drain wiring specifically is mutation-verified.
- **US-11 (fail honestly):** `RunAsync` absorbs a transient IO/permission failure and retries next tick; a bad read never kills the loop and the store degrades to "no new events".
- **D0 hygiene:** deterministic (`FixedTimeProvider`, `FakeMonotonicClock`), isolated (fresh temp data + coord dirs per test), focal-call + meaningful assertion.

## Security / privacy note

- **Trust preserved through the host:** registration and heartbeats flow through the `TrustedRegistrar`, so a forged capability is still rejected (LK-0001) and a malformed event quarantined (LK-0004) - the host composes the trust boundary, it does not bypass it.
- **OTLP is best-effort and opt-in:** `TryStartOtlp` only starts the network receiver when a loopback prefix binds; it is not started by default in this wiring, so the app opens no network listener unless asked.
- **Local-only:** the host reads local files and a local SQLite store; no egress.

## Residual risk

- **No live pane auto-refresh** — the background pump fills the store every 2s, but a docked pane folds on open; reopening the pane shows current data. A store-changed push that re-folds open panes is a follow-on.
- **Session opt-in** — a session appears only if it (or a wrapper) writes the coordination-contract log under `<dataDir>/loomkeeper-coord`, or emits OTLP. A CLI wrapper that injects the coordination writer automatically (the user's "coordination knowledge injected" vision) is future work; the ingest that consumes it is proven now.
- **OTLP hosting** — `TryStartOtlp` exists but is not started by the shell wiring (network binding + token provisioning is a deliberate opt-in); the span path is exercised via the in-process `Ingest.Enqueue` seam.
- **App-side background loop not unit-tested** — the `WorkbenchShell.RunAsync` fire-and-forget wiring is thin and covered by the existing 138 App tests staying green; the host's `RunAsync`/`PumpOnce` behaviour is unit-tested in Core.
