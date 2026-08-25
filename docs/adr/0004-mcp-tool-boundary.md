---
id: adr-0004-mcp-tool-boundary
title: "ADR-0004 — Expose bounded, typed MCP tools with deterministic authorization"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, mcp, tools, security, agents]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: implements }
  - { to: kb-mcp-agent-integration, rel: depends-on }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  The workspace daemon exposes bounded read and narrowly-authorized annotation tools over MCP;
  every request is self-contained, context-bound, audited, and protected from untrusted tool
  output and default HTTP-origin weaknesses.
---

# ADR-0004: Expose bounded, typed MCP tools with deterministic authorization

- **Status:** Proposed
- **Date:** 2026-08-25
- **Deciders:** Product owner, Security and AI Systems review

## Context

Agents need cross-worktree graph context without sharing terminal state. MCP 2026-07-28 is
stateless. Tool text and annotations are untrusted, and the executed SDK spike showed default HTTP
transport accepts an untrusted Origin.

## Decision

The daemon exposes typed MCP read tools for `find`, `describe`, `impact`, and `architecture`, and
narrow write tools for attributed knowledge/coordination annotations only. Every result is bounded
and contains provenance/limits. Every request carries workspace/caller/session context and is
authorized deterministically. HTTP is loopback-only and disabled until a custom Origin/caller guard
is verified; agent integrations may use a transport adapter but do not receive ambient authority.
The architecture’s **Optional model capability contract** is the normative versioned schema and
limit registry for these tools.

## Alternatives considered

- **Free-text graph context or code generation:** rejected because schemas, limits, authorization,
  and auditability disappear.
- **Direct store access from agents:** rejected because it lets agents fabricate artifact truth and
  bypasses workspace policy.
- **Trust SDK transport defaults:** rejected because the hostile-Origin probe succeeded.

## Consequences

- **Positive:** common tool surface across agents; bounded context; no fact writes; auditable
  requests and side effects.
- **Negative / accepted trade-off:** tool schemas and client integrations require explicit
  evaluation; transport configuration varies by agent.
- **Follow-up:** Phase 1 defines `describe`; Phase 5 validates configured external sessions and
  any HTTP adapter.

## Evidence

`spikes/mcp-server` compiled and ran with `ModelContextProtocol.AspNetCore` 2.2.0. Discovery,
tool listing, valid tool call, and invalid `isError` behavior executed [Verified]. Default hostile
Origin returned 200 [Verified].
