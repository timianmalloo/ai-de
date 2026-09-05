---
id: adr-0017-watcher-observation-projection
title: "ADR-0017 — Loomkeeper observes as a projection over the shared fact store, not a second database"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "discovery"
tags: [architecture, loomkeeper, facts, dimensions, projection, observability]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0002-workspace-fact-store, rel: refines }
  - { to: adr-0001-derived-evidence-views, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Loomkeeper adds harness/model dimensions and watcher facts (span, board message, work episode,
  evidence, scorecard, daydream observation) to the existing ADR-0002 SQLite fact store and computes
  liveness, Weave, and the leaderboard as ADR-0001 derived views, rather than owning a second store.
---

# ADR-0017 watcher-observation-projection: Loomkeeper observes as a projection over the shared fact store

- **Status:** Accepted
- **Date:** 2026-08-30
- **Deciders:** Product owner, Data & Persistence review, Enterprise Architect
- **Context spec/architecture:** docs/architecture/loomkeeper.md, docs/specs/agentic-watcher-substrate.md

## Context

Loomkeeper must record sessions, spans, board messages, work episodes, evidence, scorecards, and
daydream observations across repositories, attributed by harness and model. The spec's highest
directive is that the watcher is a projection, never a source of truth (DM6): the expensive failure
is a watcher store that drifts from the repositories it watches. ADR-0002 already provides a
per-workspace SQLite operational store of stable dimensions and append-only facts with rebuildable
current-state caches, and ADR-0001 already establishes derived evidence views.

## Decision

Loomkeeper **extends the ADR-0002 store rather than creating its own**:

- Add **`Harness`** and **`Model`** dimensions (each versioned), as attributes of the Agent Session,
  never a separate hierarchy level.
- Add append-only **facts** — `ObservedSpan`, `BoardMessage`, `WorkEpisode`, `EvidenceRecord`,
  `Scorecard`, `DaydreamObservation` — at the grain declared in the spec.
- Keep `CapturePolicy` / `ScoringPolicy` / `WatcherConfiguration` as **Type-2** versioned records.
- Compute **liveness roster, Trace/Trajectory, Weave summary, Evidence Coverage, the leaderboard,
  recurrence counts, and "current learning in force" as ADR-0001 derived views** — never stored
  rankings or scores. The leaderboard is a projection over comparable episodes per (task class,
  score schema version, harness/model).
- The **cross-repository fleet view is a read-only aggregator** over multiple per-workspace stores;
  it holds no authoritative state of its own.

## Alternatives considered

- **A standalone watcher database.** Rejected: a second source of truth that drifts, duplicates the
  daemon's ingest/queue/identity machinery, and re-opens the egress questions ADR-0011 settled.
- **A stored leaderboard/score table.** Rejected: two definitions of one quantity is a defect
  signature (DM7); ranks and scores are recomputable projections and must be derived.

## Consequences

- **Positive:** one source of truth; history and audit are the data; new measures are new
  rows/columns, not rewrites; the AI-Forward symbiosis is structural.
- **Negative / accepted trade-off:** current-state reads are "latest row per key" and need indexes or
  a labelled rebuildable cache (ADR-0002's accepted cost); the fleet aggregator must tolerate
  eventual consistency and label stale/paused repositories.
- **Follow-up:** Phase 1 measures store growth and cache rebuild for the added fact volume; a
  cross-store read contract for the fleet aggregator is a Phase-3 design item.

## Evidence

ADR-0002 (`spikes/sqlite-fact-store` verified) and ADR-0001 accepted [Verified]. The domain-model
grain, additivity, and history rules are declared in the spec's data-model section [Verified from docs].
