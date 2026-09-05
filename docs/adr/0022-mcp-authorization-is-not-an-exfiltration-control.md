---
id: adr-0022-mcp-authorization-is-not-an-exfiltration-control
title: "MCP authorization is not an exfiltration control"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "phase-3"
tags: [adr, mcp, security, egress, adr-0011, collaboration]
links:
  - { to: adr-0011-session-processing-class-egress, rel: supersedes }
  - { to: note-20260903-the-control-under-mcp-has-no-input, rel: relates-to }
review-by: 2027-03-04
review-suggested: []
summary: >-
  Supersedes ADR-0011's processing-class gate on MCP tools. The gate assumed MCP was an exfiltration
  path, but an agent in an AI-DE terminal already has a shell in the workspace and can read any file
  it likes — so denying it the same content through a tool reduces no real capability while making
  the enlightened path worse than the terminal beside it. Authorization stays, bound to session
  identity and capability rather than to a processing class the product cannot determine.
---

# MCP authorization is not an exfiltration control

## Status

Accepted, 2026-09-04. **Supersedes ADR-0011** on the processing-class binding.

The overruled veto is **Privacy & Data Governance**, not Security & Identity — an earlier draft of
this ADR named the wrong lens. `architecture.md` records it as *"Hard — Privacy: MCP results are
unanalyzed egress"*, cleared at the define-architecture gate by ADR-0011 and `P1-MCP-EGRESS`.
Attributing an overruled veto to the wrong lens misrepresents who raised it and who should review
its withdrawal.

## Context

ADR-0011 bound MCP tool authorization to a session's data-processing class: `LocalOnly` gets bounded
reads and permitted writes; `ExternalProcessing` / `UnknownProcessing` gets minimum metadata and
**denied coordination writes**. Its rejected alternative is the sharpest statement of its own
reasoning — a transport control "bounds *who connects*, not *where the bytes go next*."

Grounding the MCP design slice on 2026-09-03 found three things
(`note-20260903-the-control-under-mcp-has-no-input`):

- `SessionBinding` carries no processing class, and the store has no column for one.
- `SessionProcessingClass.LocalOnly` appears exactly once in the product, hardcoded, on the ConPty
  session request — a different object from the watcher session the gateway authorizes.
- Every `McpCallerContext` in the tree is constructed by a test. No production code builds one.

So the control is specified, proven red-first by `P1-MCP-EGRESS`, and has never been called with a
value the product determined.

## Decision

**MCP authorization is bound to session identity and capability, not to a data-processing class.**
The processing-class gate is withdrawn.

The reason is that **the threat model does not survive contact with the product's own shape.** An
agent in AI-DE is running in a terminal, in the workspace — since `c235611`, in its own git worktree
of it. It can `cat`, `grep` and `find` anything the user can. Denying that same content through
`describe` or `find` removes no capability it does not already have; it only makes the enlightened
path weaker than the terminal sitting beside it, which inverts the reason for building the path.

A control that constrains the polite interface while the impolite one is wide open is not a security
boundary. It is a tax on the well-behaved.

Two consequences follow directly:

1. **An agent's own utterance was never an exfiltration risk.** A board post, an episode declaration
   or a declared artifact path is the agent speaking *inward*, about itself, in content it already
   holds. ADR-0011 denied these for external sessions; there was nothing to protect.

2. **Workspace-content reads are not either**, for the same reason, given a terminal in the tree.
   This is the half ADR-0011 got most confidently wrong, and the half the owner named as overly
   conservative.

**What authorization still does**, and must keep doing:

- Verify the **session capability** on every write. Knowing a session id must never be enough to post
  as it — the Message Board is exactly where a forged origin would be most persuasive.
- Refuse a write from a session that never registered, and quarantine a malformed one, exactly as
  the JSONL path does.
- Keep every bound the gateway already applies: `maxNeighbors`, result caps, and the
  `SourceRevision` every result carries.

## Alternatives considered

- **Keep ADR-0011 and split "coordination write" from "content read"** (the design slice's own
  proposal): rejected as still overly conservative. It preserves a content gate whose threat the
  terminal already defeats, and it leaves the product maintaining a processing-class attestation
  mechanism that buys nothing.
- **Determine the processing class honestly and accept the consequences**: rejected. Done honestly, a
  Claude Code or GitHub Copilot session is `ExternalProcessing`, so the product's primary users would
  get minimum metadata and denied writes — disabling the collaboration surface the product exists
  for.
- **Leave the class hardcoded to `LocalOnly`**: rejected outright. That is a control that cannot
  fail, inside a security boundary, which this repository has registered as a defect class twice.
  Worse than removing it, because it looks finished.

## Consequences

- `McpToolGateway.Authorize`'s processing-class branch and `SessionProcessingClass` as an
  authorization input are removed. `P1-MCP-EGRESS` is retired **with an explicit note in its place**
  — a deleted security test with no explanation reads as an oversight to the next person.
- The MCP path and the JSONL path now enforce the **same** rules, which is what makes the
  equivalence gate between them meaningful: two paths, one set of refusals.
- `architecture.md` cites `P1-MCP-EGRESS` and needs updating in the same change.
- Nothing in the product currently changes behaviour, because nothing ever supplied a class.

## Residual risk, stated rather than implied

**If AI-DE ever hosts an agent that does NOT have a shell in the workspace, this reasoning expires.**
The entire argument rests on the agent already having filesystem access to the tree. A remote agent,
a sandboxed agent, or an agent granted MCP access without a terminal would be a genuinely different
case, and the gate withdrawn here would be the right control for it.

That is not hypothetical: `docs/specs/gemini-cli-agent-session.md` exists, and a future harness could
be hosted differently. The condition is therefore written into the codebase as a comment at the
authorization site and named here, so the trigger is discoverable rather than remembered.

**Accepted, with the owner's decision recorded**: the Privacy & Data Governance lens raised this as a
hard veto at the define-architecture gate; ADR-0011 cleared it; the owner judged the resulting
control overly conservative and instructed it be overruled. The residual risk is the
remote/sandboxed-agent case, and its trigger is named above.

The Privacy lens's underlying concern is **not** dismissed — an agent forwarding workspace content
to a provider is real. What is rejected is that an MCP gate mitigates it, when the same agent holds
a shell in the same tree. The mitigation, if one is wanted, has to sit where the capability
actually is: whether a provider-backed agent gets a terminal at all.

## Evidence

- ADR-0011 §Decision and §Alternatives, quoted above — **Verified**, read at
  `docs/adr/0011-session-processing-class-egress.md`.
- No production `McpCallerContext`, no processing class on `SessionBinding`, one hardcoded
  `LocalOnly` — **Verified** by grep across `src/` on 2026-09-03.
- The JSONL path enforces no processing class: zero references in `MessageBoard`,
  `CoordinationContract`, `IngestHost` — **Verified** by grep.
- Agent terminals run in the workspace, and since `c235611` in a worktree of it — **Verified**, this
  session implemented it.
