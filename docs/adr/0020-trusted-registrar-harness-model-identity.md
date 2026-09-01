---
id: adr-0020-trusted-registrar-harness-model-identity
title: "ADR-0020 — A trusted registrar binds harness/model identity and issues a per-session capability"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "discovery"
tags: [architecture, loomkeeper, identity, registration, capability, harness, model]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0007-agent-session-adapter, rel: refines }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Registration binds repository/worktree/terminal/agent/harness/model/session-generation and issues a
  per-session capability verified on every event; asserted identity is labelled and cannot clear a
  floor; non-AI-Forward sessions get an injected coordination contract while AI-Forward sessions reuse
  the coord-core records rather than a second ledger.
---

# ADR-0020: Trusted registrar and harness/model identity

- **Status:** Accepted (provisional pending spikes S1/S4)
- **Date:** 2026-08-30
- **Deciders:** Product owner, Security & Identity, Distributed Systems, Enterprise Architect
- **Context spec/architecture:** docs/architecture/loomkeeper.md, docs/specs/agentic-watcher-substrate.md

## Context

Loomkeeper must map many sessions across repositories unambiguously and attribute every score to its
harness and model, without trusting forgeable terminal output (ADR-0007: output is advisory; a PTY
write proves only that bytes arrived). Two repositories can share a folder name; a terminal can
restart and reuse a process id; a process can assert another session's identifier. Identity is
therefore asserted until verified, and the watcher must observe sessions that did not originate from
an AI-Forward repository without inventing telemetry those sessions do not emit.

## Decision

- A **Trusted Registrar** binds `repository → worktree → terminal → agent → harness → model → session
  generation` and issues a **per-session capability verified on every subsequent event**. A process
  using another session's identifier without its capability is **rejected and recorded as a forgery
  attempt**.
- **Environment-asserted identity is labelled** with its trust classification and **cannot satisfy a
  correctness floor** (extends ADR-0007's authenticated-acknowledgement requirement).
- A **terminal restart yields a new session generation** that cannot inherit the prior session's
  liveness, claims, or score.
- **Harness and model are bound at registration** as versioned dimensions; when unknown, the
  attribution renders **Not Recorded** and the episode is still scored on available evidence.
- **Non-AI-Forward sessions receive an injected coordination contract** (registration, repository
  identity, heartbeat, message, telemetry). **AI-Forward sessions coordinate through the existing
  `coord-core` records** — one ledger, projected by Loomkeeper, never duplicated.
- Liveness uses **monotonic** heartbeat duration, so a wall-clock change does not alter session state.

## Alternatives considered

- **Infer identity/harness/model from terminal text or OSC.** Rejected: forgeable; cannot prove
  handling (ADR-0007).
- **A second coordination ledger for the watcher.** Rejected: it would duplicate and drift from
  `coord-core`; the watcher projects the existing records instead.
- **Trust a self-declared session id with no capability.** Rejected: allows forgery/hijack; the
  per-session capability is verified on every event.

## Consequences

- **Positive:** an unambiguous fleet map; truthful trust labels; forgery is detected and recorded;
  harness/model attribution enables the leaderboard without personnel scoring; AI-Forward symbiosis
  without a second ledger.
- **Negative / accepted trade-off:** sessions from tools with no adapter and no injected contract are
  Blind Spots (Partially Observed / Not Watched) rather than silently assumed healthy — a deliberate
  honesty cost.
- **Follow-ups / new risks:** the exact registration/authentication contract per harness needs
  **S1** (Claude Code / Copilot OTLP + injected-contract ingestion), and the board/injected-contract
  alignment needs **S4** (`coord-core` append / one-file-per-session semantics), both before their
  phases.

## Evidence

ADR-0007 (asserted-vs-verified identity, authenticated agent acknowledgement) [Verified from docs].
Claude Code does not pass OTLP into subprocesses — a Blind Spot source — per KB
`agentic-session-observability` [Verified by fetch]. `coord-core.py` append records exist in the repo
[Verified]. The per-harness ingestion contract (S1) and board alignment (S4) are **not yet
spike-verified** [Flagged — provisional until PoC].
