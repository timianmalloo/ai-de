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
| `ToolAuthorization Authorize(McpCallerContext caller, string toolName)` | The deterministic (T0) authorization decision. A model never influences this — it runs before any content is read. |
| `McpToolResult Describe(McpCallerContext caller, string nodeId, int maxNeighbors)` | **(gap)** |
| `McpToolResult Find(McpCallerContext caller, string term, int maxResults)` | **(gap)** |
| `McpToolResult Standing(McpCallerContext caller, string episodeId)` | The agent's own standing for one episode: rank where comparable, trend, and one evidence-backed reason per dimension (US-16). |

### `McpToolResult Standing(McpCallerContext caller, string episodeId)`

The agent's own standing for one episode: rank where comparable, trend, and one
evidence-backed reason per dimension (US-16).

**Remarks.** **A PULL, deliberately.** US-16 says the agent receives its standing each turn, and
the obvious reading is a push. A push would put the scorer's output into the agent's context
every turn whether or not it asked — and ADR-0019's anti-Goodhart section is precisely about
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
