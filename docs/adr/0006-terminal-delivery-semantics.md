---
id: adr-0006-terminal-delivery-semantics
title: "ADR-0006 — Treat terminal prompt delivery as an at-most-once attempt"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, terminal, prompts, idempotency, delivery]
links:
  - { to: architecture, rel: implements }
  - { to: spec-ai-native-ide, rel: refines }
review-by: 2027-02-21
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Defined the AI-DE workspace daemon, fact-store, MCP, terminal, and vertical delivery architecture" }
summary: >-
  Prompt transfer makes one at-most-once terminal-stream attempt because a terminal write and
  daemon receipt cannot share a transaction. Unknown delivery blocks automatic resend and
  requires explicit user confirmation.
---

# ADR-0006: Treat terminal prompt delivery as an at-most-once attempt

- **Status:** Proposed
- **Date:** 2026-08-25
- **Deciders:** Product owner, Distributed Systems review

## Context

The product must record a prompt transfer and avoid silently sending a reviewed revision to the
wrong session. A terminal stream cannot atomically persist a daemon receipt with accepting its
bytes. A crash after one side succeeds creates an unavoidable unknown outcome.

## Decision

AI-DE records exactly one idempotent **dispatch command receipt** per dispatch key, but treats
terminal-stream delivery as one at-most-once attempt with potentially unknown outcome.
`DeliveryUnknown` blocks automatic resend. A human reviews the target session generation and
explicitly creates a new dispatch command before any resend.

## Alternatives considered

- **Claim exactly-once terminal delivery:** rejected because no shared transaction or universal
  terminal-side deduplication protocol exists.
- **Call the attempt at-least-once:** rejected because no automatic retry occurs; that term would
  overstate the delivery guarantee.
- **Automatically retry unknown delivery:** rejected because it can duplicate a consequential
  prompt in an agent session.

## Consequences

- **Positive:** the UI is truthful about uncertainty; daemon command history remains idempotent.
- **Negative / accepted trade-off:** users must resolve an occasional unknown outcome.
- **Follow-up:** a future agent-side inbox with durable deduplication can supersede this ADR for
  supported clients only.

## Evidence

Distributed-systems review found the check/write/receipt crash window [Verified].
