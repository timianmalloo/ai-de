---
id: adr-0010-two-phase-dispatch-receipt
title: "ADR-0010 — Write-ahead two-phase dispatch receipt for prompt delivery"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "0"
tags: [architecture, prompts, delivery, idempotency, crash-safety]
links:
  - { to: architecture, rel: implements }
  - { to: adr-0006-terminal-delivery-semantics, rel: relates-to }
  - { to: conceptual-model-ai-native-ide, rel: relates-to }
review-by: 2027-02-26
summary: >-
  Refines ADR-0006 with the mechanism that makes at-most-once terminal delivery true: a Pending delivery
  receipt is committed before the PTY write, the outcome is appended after, and core recovery sweeps any
  Pending receipt to DeliveryUnknown — so a crash in the write window cannot make a protocol-conformant
  retry re-deliver a prompt.
review-suggested:
  - { by: architecture, on: 2026-08-25, reason: "Architecture v2 supersedes the 2026-08-25 draft: write-ahead dispatch, in-process-first daemon, MCP egress binding, committed spikes" }
---

# ADR-0010: Write-ahead two-phase dispatch receipt for prompt delivery

- **Status:** Proposed
- **Date:** 2026-08-26
- **Deciders:** Product owner, Distributed Systems Architect, Data & Persistence Architect
- **Context spec/architecture:** docs/architecture.md

## Context

ADR-0006 declares terminal prompt transfer *at-most-once* with `DeliveryUnknown` blocking automatic
resend — but the prior draft recorded the receipt **after** the PTY write and never stated the ordering.
The Distributed Systems review (hard veto) showed the gap: if the core crashes **after** the PTY accepts
the bytes but **before** the receipt commits, restart finds **no** receipt for the dispatch key. That
state is `NotRecorded`, not `DeliveryUnknown`; a protocol-conformant retry reads no receipt and
**re-executes**, landing a duplicate consequential prompt in the agent session — exactly what ADR-0006
exists to prevent. The Data & Persistence review added that a single immutable "one outcome per key"
receipt grain cannot represent an attempt-then-outcome lifecycle, and the AI Systems review required the
generation check and the write to be atomic (LOA P8: idempotency at side-effect boundaries).

## Decision

We will make prompt delivery a **write-ahead two-phase receipt**:

1. Revalidate the binding `{workspace epoch, draft revision, session ID, session generation, dispatchKey}`.
2. **Commit a `Pending` delivery receipt for the dispatch key before any PTY byte is written.**
3. Execute the write on the session's single owner loop, comparing the bound generation to the live
   generation **atomically with the write** against the generation-specific PTY handle; a mismatch
   finalizes `Rejected` and writes nothing.
4. Append the outcome (`PtyWriteAccepted`/`Rejected`/`TimedOut`/`Failed`) as an event on the dispatch key.
5. **Core recovery sweeps any receipt still `Pending` to `DeliveryUnknown`.** A retry that reads any
   existing receipt — `Pending` included — returns it and never re-executes.

The receipt is an **append-only event series per dispatch key** (`DispatchAttempt` + one or more
`DispatchOutcome`) with a deterministic fold to the displayed outcome, so a late authenticated
`AgentAccepted` (ADR-0007) appends without rewriting an immutable row. `dispatchKey` is derived
deterministically from the envelope `commandId`, unifying the idempotency namespace.

## Alternatives considered

- **Record the receipt after the write (prior draft):** rejected — the crash window between write and
  record produces `NotRecorded`, which a conformant retry treats as "never sent" and re-delivers.
- **Single immutable one-outcome-per-key receipt:** rejected — cannot represent Pending→outcome or a
  late `AgentAccepted` without either losing evidence or mutating an immutable row.
- **Automatic resend on unknown outcome:** rejected (already, ADR-0006) — duplicates a consequential
  prompt; `DeliveryUnknown` stays a human-confirmed decision.

## Consequences

- **Positive:** at-most-once is now *true*, not asserted; the crash window resolves to the honest
  `DeliveryUnknown` state the UI already models; the receipt lifecycle is append-only-clean.
- **Negative / accepted trade-offs:** every dispatch performs two store writes (Pending, then outcome);
  the Pending write joins the control lane and so participates in writer preemption (architecture writer
  scheduling).
- **Follow-ups / new risks:** P1-DISPATCH injects a crash after the PTY write and after the Pending
  write and asserts the recovery sweep and non-re-send; the conceptual model's receipt grain is updated
  to the two event grains.

## Evidence

Distributed Systems review reproduced the check/write/receipt crash window [Verified — council review].
The append-only, trigger-enforced fact model that the event series rides on is spike-verified
(`spikes/sqlite-fact-store`, immutability under `recursive_triggers=ON`) [Verified].
