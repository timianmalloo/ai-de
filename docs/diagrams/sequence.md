---
id: diagram-sequence
title: "Sequence diagrams — extraction and agent registration"
type: doc
status: current
owner: "@timianmalloo"
phase: "0"
tags: [diagram, sequence, extraction, watcher, coordination]
links:
  - { to: architecture, rel: documents }
  - { to: diagram-component, rel: relates-to }
  - { to: spec-agentic-watcher-substrate, rel: relates-to }
review-by: 2027-09-02
summary: >-
  The two flows the product turns on: repository change to rendered surface, and terminal launch to
  a scored, attributed episode — with the failure and refusal paths drawn, not only the happy one.
---

# Sequence diagrams

## 1 — A repository change reaches a surface

The path a fact takes from a file on disk to a pixel. The important property is that **no step
writes into a projection directly**: extraction appends assertions, and every view is rebuilt from
them.

```mermaid
sequenceDiagram
  autonumber
  participant FS as Repository
  participant Sched as Ingestion scheduler
  participant Ex as Extractor adapter
  participant Store as Fact store (SQLite)
  participant Proj as ProjectionService
  participant UI as Surface

  FS->>Sched: file/event signal
  Sched->>Sched: debounce; pin artifact revision
  Sched->>Ex: ExtractionRequest(scope, revision, trigger)
  activate Ex
  Ex->>Ex: parse — C# · TS · Python · Bicep · EF · SQL · knowledge
  Ex-->>Sched: evidence assertions + scope snapshot
  deactivate Ex

  Sched->>Store: one writer transaction
  activate Store
  Note over Store: validates epoch, scope generation AND artifact revision;<br/>UPDATE/DELETE abort by trigger; no REPLACE/UPSERT
  alt stale generation or revision
    Store-->>Sched: rejected atomically
  else accepted
    Store->>Store: append facts; update labelled rebuildable projections
  end
  deactivate Store

  UI->>Proj: bounded query (node / graph / search)
  Proj->>Store: read connection, query_only=1
  Store-->>Proj: rows
  Proj-->>UI: result + limits, returned/omitted counts,<br/>source revision, provenance, confidence
  Note over UI: renders Verified vs Inferred distinctly;<br/>an omitted count is shown, never swallowed
```

**The failure path is the designed one.** A stale generation is rejected atomically rather than
merged, and a divergence between a scope's observed and indexed revision raises a health incident
and re-enqueues the scope — the control for *silent watcher loss*, where indexing quietly stops and
every answer stays plausible.

## 2 — A terminal becomes an attributed, scored session

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant Shell as WorkbenchShell
  participant Term as ConPTY session
  participant Agent as Agent (any harness)
  participant Log as Coordination log
  participant Host as WatcherHost
  participant Reg as TrustedRegistrar
  participant Scorer as WeaveScorer

  User->>Shell: New Claude Code session (Ctrl+K, A)
  Shell->>Term: start with AIDE_* environment
  Note over Shell,Term: AIDE_SESSION · TERMINAL_ID · WORKSPACE · WORKTREE<br/>BRANCH · AGENT · HARNESS · CONTRACT_LOG
  Term->>Agent: launch

  Shell->>Host: Reconcile(identity)
  Host->>Reg: Register(binding)
  Reg-->>Host: RegisteredSession + capability
  Note over Reg: harness present → Verified; absent → Asserted

  loop while alive
    Agent->>Log: heartbeat
    Host->>Log: PumpOnce — re-reading is idempotent
    Log-->>Host: events, parsed and ordered
    Host->>Reg: Heartbeat(sessionId, capability)
  end

  Agent->>Log: update { harness, model }
  Host->>Reg: UpdateHarnessAndModel(...)
  alt capability does not match
    Reg-->>Host: LK-0001 forgery rejected
  else accepted
    Reg->>Reg: merge harness/model only
    Note over Reg: identity cannot be restated;<br/>trust never rises — the log is a forgeable file
  end

  Agent->>Log: session-end
  Host->>Scorer: Score(episode, deterministic signals)
  alt no goal / no done-condition / not closed / no verification path
    Scorer-->>Host: Not Scored + the reason
  else a hard floor tripped
    Scorer-->>Host: Blocked — numeric headline suppressed
  else
    Scorer-->>Host: Partial or Scored + per-dimension evidence
  end
```

**Why `update` is a separate verb.** A repeat `register` **discards** its attributes rather than
merging them, and that is correct: the first registration's capability must stand, or an external
session id becomes a way to re-mint authority. But AI-DE registers a terminal before knowing what
runs inside it, and the model is knowable only by the agent — so without a distinct enrichment
event the model was unrecordable for every AI-DE-launched session. The kind is additive within
`loomkeeper/1` because the parser already skips a syntactically valid line whose kind it does not
handle; an older reader ignores an update where a version bump would have made it reject the whole
log.

## Confidence

| Claim | Label | Basis |
|---|---|---|
| Flow 1 ordering and the store's refusals | Verified | `WorkspaceSchema`, `docs/design/conceptual-model.md` §"Store enforcement", `ProjectionService`. |
| Flow 2 ordering | Verified | `WorkbenchShell.WatcherLoopAsync` → `SessionCoordinationEmitter.Reconcile`; `WatcherHost.PumpOnce`; `InjectedContractIngest.Apply`; `TrustedRegistrar`. |
| Trust classification on registration | Verified | `TrustedRegistrar.UpdateHarnessAndModel` doc comment and `ContractUpdateTests.AnUpdate_DoesNotPromoteTrust`. |
| The scorer's three refusal paths | Verified | `WeaveScorer.Score` — the Not-Scored gate, `TrippedFloors`, and `ComposeScoredCard`. |
