---
id: adr-0007-agent-session-adapter
title: "ADR-0007 — Separate terminal readiness from agent acceptance"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, agents, terminal, prompts, contracts]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: refines }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
  - { by: spec-ai-native-ide, on: 2026-08-26, reason: "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice" }
summary: >-
  V1 reports only PTY/terminal readiness and paste acceptance. It does not claim an external
  coding agent accepted a prompt until a supported agent-side adapter provides an authenticated,
  versioned acknowledgement.
---

# ADR-0007: Separate terminal readiness from agent acceptance

- **Status:** Proposed
- **Date:** 2026-08-25
- **Deciders:** Product owner, Enterprise and Distributed Systems review

## Context

The product stages prompts for coding-agent CLI sessions. Terminal output and OSC sequences are
untrusted/advisory, and a successful PTY write proves only that bytes reached the terminal input.
It cannot prove an agent received, parsed, or acted on the intended prompt.

## Decision

V1 exposes `TerminalReady` and `PtyWriteAccepted` only. Prompt transfer is a user-confirmed
terminal paste with at-most-once attempt semantics. A later agent-session adapter may expose
`AgentAccepted` only when it declares capabilities, readiness evidence, prompt framing, an
authenticated acknowledgement, protocol version, and fallback behavior.

## Alternatives considered

- **Infer agent acceptance from terminal text or OSC:** rejected because output is forgeable and
  cannot prove prompt handling.
- **Claim delivery acknowledgement from a daemon receipt:** rejected because it records the
  daemon command, not the agent’s semantic acceptance.

## Consequences

- **Positive:** truthful UI and no false agent-delivery guarantee.
- **Negative / accepted trade-off:** v1 cannot automate prompt resend or claim that an external
  agent processed a transfer.
- **Follow-up:** a supported agent adapter requires its own Spike Protocol, tool schema, mutual
  authentication, conformance vectors, and privacy decision.

## Evidence

Enterprise architecture review and ADR-0006 delivery semantics [Verified].
