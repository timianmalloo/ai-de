---
id: "note-20260903-the-control-under-mcp-has-no-input"
title: "ADR-0011 governs MCP, and nothing can tell it what a session is"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "phase-3"
tags: [decision-note, mcp, security, adr-0011, collaboration, egress]
links:
  - { to: adr-0011-session-processing-class-egress, rel: relates-to }
  - { to: design-watcher-coordination-contract, rel: relates-to }
review-by: 2027-03-03
review-suggested: []
summary: >-
  Grounding the MCP design slice found that ADR-0011's authorization gate — the control that decides
  what an agent may read and write over MCP — has no production input: no session carries a
  processing class, and every McpCallerContext in the tree is built by a test. Worse, if the class
  were determined honestly, a Claude Code or Copilot session is ExternalProcessing by the ADR's own
  definition, and the ADR denies coordination writes for those. The collaboration surface and the
  egress control cannot both be right as written.
---

# ADR-0011 governs MCP, and nothing can tell it what a session is

Written while grounding `/design-slice` for the MCP integration path. The design stopped here rather
than working around it, because the answer changes the shape of what gets built.

## What ADR-0011 decides

> `LocalOnly`: bounded reads and permitted writes after normal authorization.
> `ExternalProcessing` / `UnknownProcessing`: rich reads are **denied or served minimum non-sensitive
> metadata only**, and knowledge/coordination writes are denied.
>
> The processing class is an attestable, revalidated contract … revalidated immediately before each
> tool call … an unverifiable or stale attestation downgrades to `UnknownProcessing` and fails closed.

The reasoning is sound and the rejected alternative is the important half: a transport-only control
"bounds *who connects*, not *where the bytes go next* — a local externally-processing agent passes it
and still exfiltrates to its provider."

## Three findings, all verified rather than recalled

**1. No session carries a processing class.** `SessionBinding` is
`(Repository, Worktree, Terminal, Agent, Harness, Model, Trust)`. There is nowhere to put one, and
the watcher store has no column for it.

**2. `SessionProcessingClass.LocalOnly` appears exactly once in the product**, hardcoded in
`TerminalSurface` on the ConPty session request — a different object from the watcher session the
gateway would authorize.

**3. Every `McpCallerContext` in the tree is constructed by a test.** No production code builds one,
which is consistent with there being no transport. `Authorize` is correct, tested, and has never been
called with a value the product determined.

So the control is fully specified, proven red-first by `P1-MCP-EGRESS`, and gates on a value that
does not exist. It cannot currently fail, because nothing can currently make it decide.

## The tension that actually stops the design

If the class were determined honestly, **a Claude Code or GitHub Copilot session is
`ExternalProcessing`** by the ADR's own definition: it forwards context to a provider. That is not an
edge case, it is the product's primary user.

Under ADR-0011 as written, those sessions get minimum metadata on reads and **coordination writes
denied**. Board posts and episode declarations are coordination writes. So the control, applied
honestly, would disable the collaboration surface the product exists for.

Meanwhile the JSONL path enforces none of it — `MessageBoard`, `CoordinationContract` and
`IngestHost` contain zero references to a processing class. Agents post today through a path the ADR
says should refuse them.

**The collaboration surface and the egress control cannot both be right as written.** That is an
architectural decision, not a design detail, which is why the slice stopped.

## The distinction that probably resolves it

ADR-0011's whole argument is about **exfiltration of the user's workspace content** — `describe` and
`find` over the code graph. Its threat is a provider-backed agent reading the user's code and
forwarding it.

An agent's board post is not workspace content. It is **the agent's own utterance**, already visible
to that agent by definition, and it flows *inward*. Denying it protects nothing: the agent cannot
leak to itself.

So the likely resolution is that "coordination writes" in ADR-0011 needs splitting:

| | Example | Under external processing |
|---|---|---|
| **Workspace content egress** | `describe`, `find`, ledger detail | denied / minimum metadata — the ADR stands |
| **The agent's own utterance** | board post, episode open/close, artifacts | permitted — there is nothing to exfiltrate |

That is a genuine amendment to a T0 security decision and the Security & Identity Architect holds the
veto on it. It is not something a design slice may assume.

## What is needed before the MCP slice proceeds

1. **Amend or confirm ADR-0011** on the utterance-versus-content distinction.
2. **Decide how the class is determined at all** — attested by the harness, inferred from the
   harness identity, or defaulted. Note that the ADR requires failing closed to `UnknownProcessing`,
   which under the current text would deny writes for every session whose class is unknown, i.e. all
   of them.
3. **Decide whether the JSONL path is in scope for the same control.** Today it is not, which means
   either the control has a hole or the JSONL path is deliberately exempt. Both are defensible; only
   one is written down.

## Residual risk of proceeding without this

Building the MCP slice first would mean either wiring `Authorize` to a fabricated `LocalOnly` — a
control that cannot fail, in the security boundary, which is the shape this repository has registered
twice — or bypassing the gateway entirely and building a second authorization path beside a T0
decision. Neither is acceptable, and the second is worse because it would look finished.
