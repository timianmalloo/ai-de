---
id: adr-0002-workspace-fact-store
title: "ADR-0002 — Use SQLite dimensions and append-only facts per workspace"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, sqlite, facts, dimensions, provenance]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: kb-code-knowledge-graphs, rel: depends-on }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  Each workspace uses an embedded SQLite operational store with stable dimensions, append-only
  evidence/coordination/audit facts, and rebuildable current-state caches rather than an archived
  graph database dependency.
---

# ADR-0002: Use SQLite dimensions and append-only facts per workspace

- **Status:** Proposed
- **Date:** 2026-08-25
- **Deciders:** Product owner, Data & Persistence review

## Context

The selected Kuzu store is archived. The architecture requires stable identities, provenance,
append-only evidence, idempotent scope replacement, bounded impact traversal, and a first-class
.NET embedded store. Domain modelling guidance defaults durable core entities to dimensions and
changes to append-only facts.

## Decision

Each workspace will own one SQLite operational store through `IWorkspaceStore`. Stable workspace,
repository, worktree, artifact, node, session, agent, tool, and view identities are dimensions.
Evidence, claim assessment, coordination, prompt revision, delivery receipt, audit reference, and
trace observation are append-only facts. Current graph state is a deterministic, indexed
projection.

## Alternatives considered

- **Kuzu/Cypher:** rejected because Kuzu and its .NET binding are archived.
- **DuckDB/DuckPGQ:** rejected for the initial operational store because its graph extension is
  research-grade and no measured workspace query benefit offsets an additional dependency.
- **Server graph database:** rejected for v1 because it adds operations, authentication, and
  licence surface to a local-first product.

## Consequences

- **Positive:** embedded, portable, .NET-supported store; constraints and recursive traversal;
  export/rebuild path; no graph-vendor lock-in.
- **Negative / accepted trade-off:** graph queries use relational recursive CTEs and projections
  rather than Cypher; writes serialize through a bounded daemon queue; current-state reads need
  indexes/caches.
- **Follow-up:** Phase 1 measures real graph size/query latency and validates cache rebuild.

## Evidence

`spikes/sqlite-fact-store` executed with `Microsoft.Data.Sqlite` 10.0.11: WAL command, unique
constraint rejection, and recursive impact query [Verified]. Installed XML documents no nested
transactions [Verified].
