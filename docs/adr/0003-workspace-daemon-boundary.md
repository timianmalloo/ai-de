---
id: adr-0003-workspace-daemon-boundary
title: "ADR-0003 — Run one local daemon per workspace"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, daemon, workspace, isolation]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  A workspace-scoped local daemon owns durable facts, scheduling, query/projection, policy, and
  tool authorization so the WPF shell and agent sessions do not share an unbounded global state.
---

# ADR-0003: Run one local daemon per workspace

- **Status:** Proposed
- **Date:** 2026-08-25
- **Deciders:** Product owner, Enterprise and Distributed Systems review

## Context

The product groups multiple repositories and worktrees while requiring workspace isolation,
local-first processing, source-attributed facts, and agent/tool authorization. A shell-only
implementation would couple UI lifetime to extraction/store work; a global multi-tenant daemon
would make workspace authority and data isolation needlessly complex in v1.

## Decision

AI-DE will run one OS-local daemon per opened workspace. The daemon owns registry, SQLite store,
single-writer ingestion, projections, audit/coordination readers, health, and tool authorization.
The WPF shell is a local control client; agents receive bounded access only through the gateway.

## Alternatives considered

- **All state in the WPF process:** rejected because extractor/daemon lifecycle, crash recovery,
  and tool access would be coupled to pane lifecycle.
- **One global multi-workspace daemon:** rejected because tenancy and authorization complexity do
  not serve the local-first v1 core scenario.

## Consequences

- **Positive:** clear workspace trust/data boundary; restartable shell; one writer per database;
  direct health ownership.
- **Negative / accepted trade-off:** opening multiple workspaces starts multiple daemon processes.
- **Follow-up:** Phase 1 defines authenticated local IPC and daemon lifecycle/recovery.

## Evidence

Specification workspace identity, privacy isolation requirements, and current WPF baseline
[Verified].
