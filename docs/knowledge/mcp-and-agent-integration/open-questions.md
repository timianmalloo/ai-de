---
id: kb-mcp-open-questions
title: "MCP & Agent Integration — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, mcp-security, prompt-injection]
links:
  - { to: kb-mcp-agent-integration, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  What research could not settle about MCP integration, how this domain fails — including the
  graph-as-injection-channel risk we create ourselves — and the strongest published case that
  MCP is the wrong integration layer.
---

# Open questions & domain failure modes

## Unresolved by research

1. **No measured guidance exists on tool granularity, description length, schema token cost, or result
   size.** All published advice is opinion. Since our tools (`describe`, `impact_of`, `architecture`,
   `graph_query`) are the whole product surface for agents, this must be settled by our own evaluation
   rather than by copying a best practice that does not exist. *(Flagged)*
2. **Does the Copilot cloud agent read a portable `.mcp.json`?** Inferred from the VS Code Agent Host
   documentation, not confirmed for the cloud agent specifically. Load-bearing if the cloud agent is a
   target. *(Inferred)*
3. **Cursor and Windsurf config paths and feature support** could not be fetched. *(Flagged)*
4. **Does Zed support tool invocation, or only context servers?** It used MCP first for read-only
   resources/prompts. *(Inferred, [S27])*
5. **What replaces Sampling for us, concretely?** Nothing in the current design needs it — but if any future
   feature wanted the daemon to ask the model something, the migration path is "direct LLM API integration",
   which means the daemon needs its own model credentials and its own cost budget. That is a different
   product decision. *(Open)*
6. **How do we detect and handle legacy-era clients?** The spec defines transport-specific probes; whether
   we support the legacy era at all is a scope decision with a real maintenance cost. *(Open)*
7. **What is the actual review burden of MCP server trust approval?** Claude Code requires per-workspace
   approval of `.mcp.json`. For a tool meant to be launched per workspace automatically, that friction is
   real and unmeasured. *(Open)*

## Known failure modes of this domain

- **Building on a deprecated primitive.** Sampling, Roots and Logging are all deprecated in the current
  revision and much published tutorial material predates that. A design that reaches for "the server asks
  the model" is building on something with a removal date. *(Verified, [S3])*
- **Assuming hooks are portable.** They are a Claude Code feature. Copilot's cloud agent has **no**
  interception surface, documented as absent. A refresh mechanism built only on hooks silently does nothing
  for half the intended users. *(Verified, [S23])*
- **The graph as a prompt-injection channel — the failure we would create ourselves.** Agents write
  `Note`/`Decision` nodes; other agents read them through `describe` and `find`. That is untrusted content
  flowing into another agent's context, inside our own product, with no network boundary to make it
  obvious. The spec's warnings about tool results apply in full to our own returned data.
- **Trusting annotations.** `readOnlyHint` is untrusted by specification. Any safety property that depends
  on a client honouring it is not a safety property.
- **Origin validation forgotten.** Streamable HTTP servers MUST validate `Origin` and return 403 —
  otherwise a web page can drive the local daemon via DNS rebinding. A local-first tool is exactly the
  target this defends. *(Verified, [S5][S13])*
- **Token passthrough.** If the daemon ever proxies to another service, forwarding the caller's token
  without validating the audience is the named anti-pattern. *(Verified, [S13])*
- **Config drift across clients.** Four different filenames with different scopes and precedence rules; a
  server that works in Claude Code and silently never loads in VS Code is a support burden, not a bug
  anyone will report.
- **Non-deterministic tool ordering.** Costs prompt-cache hits on every call, for free, invisibly.
  *(Verified, [S2])*
- **Assuming agents will volunteer knowledge.** The `server-memory` prior art shows the mechanism works and
  that nothing is captured unless the agent chooses to call a write tool. A knowledge graph that depends on
  voluntary writes will be thinner than the design assumes. *(Verified, [S26])*

## Disconfirming views we deliberately sought

**The strongest case that MCP is the wrong integration layer for this product.**

1. **The protocol is young and moving fast in breaking ways.** In roughly a year it removed the handshake,
   removed sessions, removed the GET endpoint, deprecated three primitives and replaced the entire
   server-to-client interaction model with MRTR. Building the product's primary interface on a spec with
   that change velocity means recurring, non-optional migration work. *(Verified from the changelog itself, [S2][S3])*
2. **The interception surface it needs does not exist portably.** MCP standardises the *call*, not the
   *lifecycle*. Refresh-on-agent-activity — the thing that makes the graph feel live — depends on hooks,
   which only one client has. The file event bus, which is not MCP at all, is the only universal mechanism.
3. **Security is delegated entirely to implementers.** The spec names the threats and provides no
   enforcement. Every safety property is ours to build and ours to get wrong.
4. **A simpler integration might suffice.** The graph could be exposed as generated Markdown in the repo —
   `AGENTS.md`-style projections and committed `docs/diagrams/` — which every agent reads with no protocol,
   no server, no trust approval and no version churn. That is precisely the mechanism the coordination
   spec's own explorer scores highest on "model/tool agnostic" and "low operational overhead".

**How it fared:** points 1–3 stand and change the design — isolate the protocol behind our own service
interface, build the file bus as the universal floor with hooks as a Claude-Code accelerator, and treat
security as ours. Point 4 is the interesting one, and it **half-succeeds**: static projections genuinely do
serve the "what is the shape of this codebase" question with none of MCP's cost, and we should generate them
regardless. What they cannot serve is the *query* — `impact_of(sql:orders.dbo.Order)` across repositories is
an arbitrary traversal over a graph too large to project, and pre-rendering every possible answer is not a
thing. So the honest position is not "MCP or files" but **files for the browsable surface, MCP for the
queryable one** — and the file surface should ship first, because it is cheaper, universal, and it makes the
MCP layer's marginal value measurable rather than assumed.

One residual risk the objection leaves untouched: the write side. `record_decision` — the mechanism by which
one session's knowledge reaches every future session — has no file-based equivalent that is safe, because
letting an agent append to a committed Markdown file is exactly the injection channel named above, minus the
schema that would let us constrain it.
