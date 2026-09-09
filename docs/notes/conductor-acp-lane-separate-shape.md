---
id: note-conductor-acp-lane-separate-shape
title: "Decision note — ITerminalSession stays frozen; the ACP lane is a separate shape"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, agent-plane, acp, dispatch, privacy, dc-096]
links:
  - { to: spec-conductor, rel: refines }
  - { to: note-conductor-r4-core-phase1-scope, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Spec §10 says the dispatch seam "generalizes to lanes... behind one interface". ACP is
  bidirectional and its events must be persisted, while ITerminalSession's Output is
  explicitly ephemeral and must never be persisted - an opposite contract. Phase 1 therefore
  freezes ITerminalSession, builds the ACP lane on its own types, and defers the one-interface
  convergence until ObservedLaneBinding exists.
---

# Decision note — `ITerminalSession` stays frozen; the ACP lane is a separate shape

**Ruled by:** Owner agent, 2026-09-09. Confidence: **Verified**.

## The tension

Spec §10 (a `class="note"` paragraph, not a numbered bullet) says: *"The dispatch seam
(`ITerminalSession`, `DispatchService`) generalizes to lanes: a lane is ACP-backed or
terminal-backed behind one interface."*

The ACP spike established by execution that **ACP is bidirectional** — the agent calls the
*client*: `session/request_permission`, `fs/read_text_file`, `fs/write_text_file`,
`terminal/create|output|wait_for_exit|kill|release`, `elicitation/create`. A .NET implementation
needs a **server loop**, not a request/response client. `ITerminalSession` is a strictly one-way
byte surface with no inbound-request concept.

## Ruling

**Leave `ITerminalSession` and its conformance suite untouched.** Build the ACP lane on its own
types (`AcpClient` / `AcpSession`, which spec §10 already lists as new). The **§7.2 run-event
stream is the only shared surface.** Defer "one interface" until `ObservedLaneBinding` lands.

**Also cut:** any new `ILane`-style interface in Phase 1. *An interface with one implementer is
the same defect in a new coat.*

## Because — and the strongest reason is not the one first offered

1. **It is a privacy-contract collision, not merely a shape mismatch.**
   `src/AiDe.Core/Dispatch/ITerminalSession.cs:68-72` documents `Output` as:
   *"Bounded, ephemeral output. Terminal text **never enters the fact store, an audit entry, a log
   or telemetry** (spec privacy) — reading this channel is the only way it is ever observed."*
   ACP events are the **exact opposite**: §7.2 requires them persisted to the run log. Folding one
   into the other would route persist-forbidden bytes through a persist-required channel.
   *(Verified — I read this line.)*

2. **The spec's own type list already contains two shapes plus a binding.** §10 names `AcpSession`
   as new in `AgentPlane/`, and `ObservedLaneBinding` as new in `Terminal/`. So "one interface" is
   read as the **lane concept above both**, satisfied by convergence once both exist — not as a
   Phase-1 instruction to unify. *(This is an interpretation, and is marked as one.)*

3. **Widening buys nothing at the consumer anyway.** `src/AiDe.App/Workbench/TerminalSurface.cs:39`
   and its pump are hard-typed to the **concrete** `ConPtyTerminalSession`, not the interface.

4. **DC-096** — an invariant that holds only because every input so far shared an incidental shape.
   `ITerminalSession` has had exactly one implementer; an ACP lane is precisely the
   differently-shaped second input. Plus the Solution-Selection Ladder's YAGNI rung.

**Citation correction:** DC-002 (unrepeatable "Verified" evidence) and DC-079 (two coexisting
conventions) are **not** the single-implementer class and must not be cited for it. Cite **DC-096**
and the ladder only.

## Scope effect

**Cuts** widening `ITerminalSession` and any serialize-ACP-into-bytes approach. **Freezes**
`ITerminalSession`, `TerminalSessionConformanceTests` and `ConPtyTerminalSession` for Phase 1 —
which also satisfies the programme's Not-in-scope rule against degrading the terminal stack.
**Defers** the one-interface convergence to the phase that builds `ObservedLaneBinding`
(R11, Phase 4), by a decision note at that time.

## Conditions

`DispatchService` may gain a lane-typed entry point **only if** a Phase-1 acceptance bullet
requires it. Otherwise it is not touched.
