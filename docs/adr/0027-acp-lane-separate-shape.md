---
id: adr-0027-acp-lane-separate-shape
title: "ADR-0027 — The ACP lane is a separate component shape; ITerminalSession stays frozen"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [architecture, agent-plane, acp, terminal, privacy, dc-096]
links:
  - { to: architecture-agent-plane, rel: relates-to }
  - { to: note-conductor-acp-lane-separate-shape, rel: implements }
  - { to: adr-0007-agent-session-adapter, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Spec §10 reads as if the dispatch seam generalizes to lanes behind one interface, but
  ITerminalSession.Output must never be persisted while ACP run events must always be persisted —
  an opposite contract, not merely a different shape. AgentPlane builds the ACP lane on its own
  types and leaves ITerminalSession untouched; the two share only the run-event envelope.
---

# ADR-0027 acp-lane-separate-shape — The ACP lane is a separate component shape

**Status:** Accepted · **Date:** 2026-09-09 · **Deciders:** Owner agent (delegated CT20 authority),
grounded in the ACP spike and `src/AiDe.Core/Dispatch/ITerminalSession.cs`
**Context spec/architecture:** `docs/specs/conductor/ai-de-conductor-spec-v1.html` §10;
`docs/architecture/agent-plane.md` §1, §3.1, §7.1

## Context

Spec §10 states, in prose rather than a numbered bullet: *"The dispatch seam (`ITerminalSession`,
`DispatchService`) generalizes to lanes: a lane is ACP-backed or terminal-backed behind one
interface."* The ACP spike established by execution that **ACP is bidirectional** — the agent calls
the client (`session/request_permission`, `fs/read_text_file`, `fs/write_text_file`,
`terminal/create|output|wait_for_exit|kill|release`, `elicitation/create`), so a .NET implementation
needs a **server loop**, not the request/response shape `ITerminalSession` has.

The reason this is a decision and not just an inconvenient shape mismatch: `ITerminalSession.Output`
is documented, verbatim, at `src/AiDe.Core/Dispatch/ITerminalSession.cs:68-72` as *"Bounded, ephemeral
output. Terminal text never enters the fact store, an audit entry, a log or telemetry (spec
privacy) — reading this channel is the only way it is ever observed."* Spec §7.2 requires ACP run
events **persisted** to the run log. These are opposite contracts on the same conceptual channel
("what came out of the lane"), not two dialects of one contract — a **privacy-contract collision**.

## Decision

**Leave `ITerminalSession`, `TerminalSessionConformanceTests`, and `ConPtyTerminalSession` untouched
for Phase 1.** Build the ACP lane on its own types (`AcpPeer`, `AcpLaneClient`,
`AcpEngineProcess`, `AcpRunEventMapper`) in `src/AiDe.Core/AgentPlane/`. The **only** shared surface
is the spec §7.2 run-event envelope, which both a governed (ACP) and an observed (terminal) lane can
in principle emit into — Phase 1 has exactly one producer. No new `ILane`-style interface is
introduced: an interface with one implementer is the same defect in a new coat (DC-096).

## Options considered

1. **Widen `ITerminalSession` into a shared lane interface — rejected.** Requires either (a) a
   persistence escape hatch on the interface itself, defeating the point of a typed boundary, or (b)
   two implementations that silently disagree about what their own shared method means. Also buys
   nothing at the one real consumer: `src/AiDe.App/Workbench/TerminalSurface.cs:39` is hard-typed to
   the concrete `ConPtyTerminalSession`, not the interface, so no caller is waiting for a shared
   shape.
2. **Serialize ACP frames into `ITerminalSession.Output` bytes — rejected.** This is the collision
   made concrete: it would route persist-required protocol events through a channel whose contract
   is "never persisted," or it would require special-casing the one channel that must break its own
   rule — either way the interface stops meaning what its doc comment says.
3. **A separate ACP lane, converging later behind a lane concept above both shapes — chosen.** Spec
   §10 itself names two new types (`AcpSession` in `AgentPlane/`, `ObservedLaneBinding` in
   `Terminal/`) plus a binding — read as the convergence point once **both** exist, not as a
   Phase-1 instruction to unify prematurely. Convergence is deferred to the phase that builds
   `ObservedLaneBinding` (R11, Phase 4), as a non-goal with a named trigger, not as debt.

## Consequences

- **Positive:** `ITerminalSession`'s privacy invariant is never at risk of a persistence leak through
  a generalized interface; the ACP lane's own types can be tested (framing, correlation, handshake,
  permission) with no terminal machinery in scope, and vice versa.
- **Negative / accepted:** two lane shapes coexist in the tree until Phase 4's convergence point,
  which is a real (if bounded) duplication of "a lane has an identity and a lifecycle." Recorded as a
  non-goal with a trigger (`ObservedLaneBinding` reaching Phase 4), not as debt, so a future session
  does not "pay it down" by building the unification Ruling 7 explicitly declined.
- **Follow-ups / new risks:** `DispatchService` may gain a lane-typed entry point only if a future
  acceptance bullet requires it; otherwise it stays untouched. The one shared surface (the run-event
  envelope) is the seam a future terminal-backed lane producer must write into without a second
  mapper appearing (`AcpRunEventMapper` is documented as "one mapper" for exactly this reason).

## Evidence

- **Verified:** `src/AiDe.Core/Dispatch/ITerminalSession.cs:68-72` (read directly); the ACP spike's
  captured bidirectional traffic, `spikes/acp-subscription-lane/` (88 real frames, both directions);
  `src/AiDe.App/Workbench/TerminalSurface.cs:39` (concrete-type consumer).
- **Ruling of record:** `docs/notes/conductor-acp-lane-separate-shape.md`, Owner agent, 2026-09-09,
  confidence Verified.
- **Citation correction carried from the ruling:** DC-002 (unrepeatable "Verified" evidence) and
  DC-079 (two coexisting conventions) are **not** the single-implementer class this decision cites;
  the applicable class is DC-096 plus the Solution-Selection Ladder's YAGNI rung.
