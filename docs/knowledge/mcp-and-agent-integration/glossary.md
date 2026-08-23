---
id: kb-mcp-glossary
title: "MCP & Agent Integration — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, mcp, ubiquitous-language]
links:
  - { to: kb-mcp-agent-integration, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Precise definitions for MCP vocabulary — host, client, server, the primitives, MRTR, modern
  versus legacy era — plus the named threat classes, so design documents use one word per
  concept.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **MCP** | Model Context Protocol — an open protocol connecting LLM applications to external tools and data over JSON-RPC 2.0. *(Verified, [S1])* |
| **Host** | The LLM application that initiates MCP connections (Claude Code, VS Code). *(Verified, [S1])* |
| **Client** | The connector inside a host managing one connection to a server. *(Verified, [S1])* |
| **Server** | A process or service exposing Tools, Resources and Prompts over MCP. *(Verified, [S1])* |
| **Tool** | A **model-controlled** callable function. Represents arbitrary code execution by the spec's own framing. *(Verified, [S7])* |
| **Resource** | **Application-driven** read-only context exposed by a server. *(Verified, [S8])* |
| **Prompt** | A **user-controlled** template or workflow exposed by a server. *(Verified, [S9])* |
| **Elicitation** | A server-initiated request for user input, delivered through MRTR. *(Verified, [S10])* |
| **Sampling** | *(Deprecated 2026-07-28)* A server-initiated LLM completion routed through the client. *(Verified, [S11])* |
| **Roots** | *(Deprecated 2026-07-28)* Client-supplied filesystem directory hints. Migrate to tool parameters or resource URIs. *(Verified, [S12])* |
| **MRTR** | Multi Round-Trip Requests — the pattern replacing server-initiated requests: the server returns `resultType: "input_required"` with an `InputRequiredResult`, and the client retries with `inputResponses`. *(Verified, [S2])* |
| **`resultType`** | Required on every result: `"complete"` or `"input_required"`. *(Verified, [S2])* |
| **Modern era** | Protocol revision `2026-07-28` and later — stateless, per-request `_meta`, no handshake. *(Verified, [S6])* |
| **Legacy era** | Revision `2025-11-25` and earlier — required the `initialize` handshake. *(Verified, [S6])* |
| **`server/discover`** | The RPC a server must implement to advertise supported protocol versions, replacing handshake negotiation. *(Verified, [S6])* |
| **Streamable HTTP** | The current HTTP transport: one POST endpoint returning JSON or a per-request SSE stream. No session IDs, no GET. *(Verified, [S5])* |
| **HTTP+SSE** | *(Deprecated since 2025-03-26)* The older transport with a persistent GET/SSE stream plus a POST send endpoint. *(Verified, [S3][S4])* |
| **`subscriptions/listen`** | The single POST-based change-stream RPC that replaced `resources/subscribe`. *(Verified, [S2])* |
| **Extension** | A negotiated capability outside the core spec, keyed by a reverse-domain identifier such as `io.modelcontextprotocol/tasks`. *(Verified, [S6])* |
| **`isError`** | The result field marking an **operational** failure — as distinct from a JSON-RPC error, which marks a protocol/transport failure. *(Verified, [S7])* |
| **Tool annotations** | `readOnlyHint`, `destructiveHint` and similar. **Untrusted by default** — the spec requires clients to treat them as untrusted unless the server is trusted. *(Verified, [S7])* |
| **`PreToolUse`** | The Claude Code hook firing before a tool executes; can block via `hookSpecificOutput.permissionDecision: "deny"`. *(Verified, [S18])* |
| **`AGENTS.md`** | A cross-tool Markdown instruction convention; the nearest ancestor in the directory tree wins. *(Verified, [S24])* |
| **Auto-memory** | Claude Code's agent-written note store (~200 lines / 25 KB per repo) — the only automatic write-from-correction mechanism observed, and not MCP-based. *(Verified, [S21])* |
| **Prompt injection** | An attack in which content inside a tool result manipulates the agent's behaviour. The spec acknowledges it and provides **no automated enforcement**. *(Verified, [S1][S13])* |
| **Tool poisoning / rug-pull** | A server changing a tool's description or behaviour after approval, exploiting the fact that descriptions drive model tool selection. *(Verified, [S13])* |
| **Confused deputy** | An OAuth attack exploiting static client IDs in an MCP proxy server. *(Verified, [S13])* |
| **Token passthrough** | The anti-pattern of forwarding a client token to a downstream API without validating its audience. *(Verified, [S13])* |
