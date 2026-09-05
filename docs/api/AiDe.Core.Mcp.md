---
id: api-aide-core-mcp
title: "API: AiDe.Core.Mcp"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Mcp: 6 types, 8 members, 50% carrying a summary doc comment.
---

# API: `AiDe.Core.Mcp`

**6 public types · 8 public members · 50% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `McpErrorCodes`

*class* — `McpToolGateway.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `string EgressDenied = "AIDE-MCP-EGRESS-DENIED"` | **(gap)** |
| `string CrossWorkspace = "AIDE-AUTH-CROSS-WORKSPACE"` | **(gap)** |
| `string LimitExceeded = "AIDE-MCP-LIMIT-EXCEEDED"` | **(gap)** |
| `string NotFound = "AIDE-MCP-NOT-FOUND"` | The tool ran and its subject does not exist — distinct from a tool that refused. |

### `string NotFound = "AIDE-MCP-NOT-FOUND"`

The tool ran and its subject does not exist — distinct from a tool that refused.

**Remarks.** An absent subject must not come back as an empty payload. An empty standing reads as "you
have no rank and no reasons", which is a claim about the agent rather than about the lookup
(DC-087) — and the same is true of an empty description or an empty result set.

## `McpCallerContext`

*record* — `McpToolGateway.cs`

Identifies the calling agent session, including where its bytes go next.

## `ToolAuthorization`

*enum* — `McpToolGateway.cs`

*No doc comment on this type.* **(gap)**

## `McpToolResult`

*record* — `McpToolGateway.cs`

A tool result that knows whether it was reduced, and why.

## `McpToolGateway`

*class* — `McpToolGateway.cs`

The MCP boundary. Every tool call is authorized against the calling session's declared
processing class before any workspace content is assembled (ADR-0011).

**Remarks.** Pattern: Policy-Bound Egress. Loopback binding answers "who connected", not "where do these bytes
go next" — an externally-processing agent runs locally and forwards to its provider, so a
transport control cannot close that path. Authorization therefore follows the session class, and
an unknown class fails closed exactly like an external one.

| Member | Summary |
|---|---|
| `ToolAuthorization Authorize(McpCallerContext caller, string toolName)` | The deterministic (T0) authorization decision. A model never influences this — it runs before any content is read.  Authorizes a tool call. Bound to session identity and workspace, never to a processing class. |
| `McpToolResult Describe(McpCallerContext caller, string nodeId, int maxNeighbors)` | **(gap)** |
| `McpToolResult Find(McpCallerContext caller, string term, int maxResults)` | **(gap)** |
| `McpToolResult Standing(McpCallerContext caller, string episodeId)` | The agent's own standing for one episode: rank where comparable, trend, and one evidence-backed reason per dimension (US-16). |

### `ToolAuthorization Authorize(McpCallerContext caller, string toolName)`

The deterministic (T0) authorization decision. A model never influences this — it runs before
any content is read.

Authorizes a tool call. Bound to session identity and workspace, never to a processing class.

**Remarks.** **ADR-0022 supersedes ADR-0011 here.** This used to deny writes and reduce reads for
a session declared `ExternalProcessing`, on the reasoning that a provider-backed agent
would exfiltrate workspace content. The threat model does not survive contact with the
product's shape: an agent in AI-DE has a TERMINAL in the workspace — since `c235611`, in
its own worktree of it — and can `cat`, `grep` and `find` anything the user
can. Denying it the same content through a tool removed no capability it did not already have
and only made the enlightened path weaker than the shell beside it.





A control that constrains the polite interface while the impolite one is wide open is
not a boundary; it is a tax on the well-behaved. The owner overruled the Security hard veto on
exactly that reasoning, and ADR-0022 records the decision and its residual risk.





**THE CONDITION THAT REVIVES THE GATE, named so it is discoverable rather than
remembered:** this argument rests entirely on the agent already having a shell in the tree.
A remote agent, a sandboxed agent, or any agent granted MCP access *without* a terminal
is a genuinely different case, and a processing-class gate is the right control for it. If
AI-DE ever hosts one, reopen ADR-0022 before extending this method.





What authorization still does is enforced elsewhere and deliberately not here: the
workspace check in `Guarded` (a caller may not read another workspace), the result
bounds every tool applies, and — for writes, when they exist — session capability
verification, which is what stops knowing an id being enough to post as it.

### `McpToolResult Standing(McpCallerContext caller, string episodeId)`

The agent's own standing for one episode: rank where comparable, trend, and one
evidence-backed reason per dimension (US-16).

**Remarks.** **A PULL, deliberately.** US-16 says the agent receives its standing each turn, and
the obvious reading is a push. A push would put the scorer's output into the agent's context
every turn whether or not it asked — and ADR-0019 advisory-evaluator-calibration's anti-Goodhart section is precisely about
what an agent is shown regarding its own scoring. An agent that asks has chosen to look.





**Guarded like every other tool**, which is the reason it lives here rather than on a
new seam: it inherits the workspace check, the authorization gate and the
minimum-metadata degradation rather than restating any of them. A standing is an evaluation
of the caller, so a tool that skipped the cross-workspace check would let one workspace read
another's scoring.





**An unknown episode is an error, not an empty standing.** An empty one reads as
"you have no rank and no reasons" — a claim about the agent rather than about the lookup
(DC-087).

## `MinimumMetadata`

*record* — `McpToolGateway.cs`

What a non-LocalOnly caller is allowed to learn: that there is data, not what it says.
