---
id: kb-mcp-agent-integration
title: "MCP & Agent Integration — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [mcp, claude-code, copilot, hooks, agent-integration, tool-design]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for exposing a knowledge graph to coding agents over MCP: the stateless
  2026-07-28 spec and what it deprecated, the C#/.NET SDK, the per-client config and hook
  matrix, and the security surface a write-capable server inherits.
---

# MCP & Agent Integration — domain knowledge

**Domain & problem:** AI-DE's daemon exposes a codebase knowledge graph to coding agents (Claude Code,
GitHub Copilot CLI/cloud agent, other harnesses) over **MCP**, with hook-based integration so agent activity
triggers re-extraction and so agents can write knowledge (decisions, notes, terms) back into the graph.

**Canonical framing:** The field frames this as **tool-calling over a standard protocol** — MCP is JSON-RPC
2.0 with three primitives (tools, resources, prompts) and two transports (stdio, Streamable HTTP). Our
framing matches, with one deliberate divergence worth naming: we split **read tools** (query the graph)
from **write tools** that may only create *knowledge* nodes — extractors own all artifact-derived truth, so
an agent can annotate reality but never fabricate it. That constraint is ours, not the protocol's.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh — *and this is the
fastest-moving topic in the base; treat anything here older than a quarter as suspect.*

*(`data-and-constants.md` is folded into `references.md` §"Versions, packages and exact names" — spec
revisions, package IDs, config filenames and hook event names are the constants, and they belong beside
their source.)*

## Headline findings

1. **The current spec revision `2026-07-28` is stateless.** The `initialize` handshake, protocol-level
   sessions, `Mcp-Session-Id` and the HTTP GET endpoint are all **removed**; every request self-describes
   via `_meta`, carrying `_meta.io.modelcontextprotocol/protocolVersion` and client capabilities. Servers
   implement `server/discover` to advertise versions; a mismatch returns
   `UnsupportedProtocolVersionError` (`-32022`) and the client retries. — *(Verified, [S1][S2])*
2. **Sampling, Roots and Logging are deprecated as of 2026-07-28**, with earliest removal in the first
   release on or after 2027-07-28. The "server asks the client to call the LLM" pattern is going away;
   Roots migrate to tool parameters or resource URIs. Do not design on any of them. — *(Verified, [S3][S11][S12])*
3. **Server→client interaction is now MRTR (Multi Round-Trip Requests).** Instead of the server issuing its
   own JSON-RPC request, it returns a result with `resultType: "input_required"` and an
   `InputRequiredResult`; the client retries with `inputResponses`. Every result now carries `resultType`
   (`"complete"` | `"input_required"`). — *(Verified, [S2])*
4. **Two transports remain: stdio and Streamable HTTP.** HTTP+SSE has been deprecated since 2025-03-26 and
   its GET stream is gone; SSE now appears only as a *response format* for a POST. `subscriptions/listen`
   replaced `resources/subscribe`. — *(Verified, [S3][S4][S5])*
5. **The C# SDK is first-class and jointly maintained by Microsoft and Anthropic.** `ModelContextProtocol`
   on NuGet (plus `.Core`, `.AspNetCore`, `.Extensions.Tasks`, `.Extensions.Apps`), Apache-2.0, **v2.2.0**,
   stable rather than preview, with `AddMcpServer()` for stdio or ASP.NET Core hosting. This removes the
   main risk of building the daemon in .NET. — *(Verified, [S14][S15][S28])*
6. **Claude Code's hook system is large and can genuinely block.** ~31 documented events including
   `PreToolUse`, which denies a tool call via `hookSpecificOutput.permissionDecision: "deny"`; also
   `FileChanged`, `WorktreeCreate`/`WorktreeRemove`, `SessionStart`/`SessionEnd`, `PostToolBatch`,
   `SubagentStart`/`SubagentStop`, `Elicitation`. Handlers may be a shell command, an HTTP endpoint, or an
   LLM prompt. — *(Verified, [S18])*
7. **GitHub Copilot's cloud agent has no hook equivalent — this is documented absence, not an oversight.**
   Its customisation surface is entirely declarative: `.github/copilot-instructions.md`,
   `.github/instructions/*.instructions.md`, `AGENTS.md` (nearest-ancestor wins), `CLAUDE.md`/`GEMINI.md`,
   and `copilot-setup-steps.yml` for environment setup before the agent starts. There is no way to
   intercept or block a tool call mid-session. — *(Verified, [S23][S24][S25])*
8. **Config file names and scopes differ per client and are load-bearing.** Claude Code reads `.mcp.json`
   (project, committed, requires trust approval) plus `~/.claude.json` and `.claude/settings*.json`;
   VS Code reads `.vscode/mcp.json`; the Copilot Agent Host reads a portable `.mcp.json` and
   `~/.copilot/mcp-config.json`. Claude Code's documented precedence is managed → CLI → project-local →
   project-shared → user. — *(Verified, [S19][S20][S22])*
9. **The security model is entirely the implementer's responsibility.** The spec states plainly that "tools
   represent arbitrary code execution", that hosts "must obtain explicit user consent before invoking any
   tool", and that clients **MUST** treat tool annotations as untrusted unless the server is trusted. Named
   threat classes: prompt injection via tool results, tool poisoning / rug-pull descriptions, confused
   deputy, token passthrough, DNS rebinding (Streamable HTTP servers **MUST** validate `Origin` and return
   403). **No enforcement exists at the protocol level.** — *(Verified, [S1][S13])*
10. **Tool-name rules are specified; token-cost and output-size guidance are not.** Names SHOULD be 1–128
    chars from `[A-Za-z0-9_\-.]`, no spaces, unique per server; `inputSchema` is JSON Schema 2020-12;
    operational failures set `isError: true` rather than raising a JSON-RPC error; and servers **SHOULD
    return tools in deterministic order to improve LLM prompt-cache hit rates**. Everything about
    granularity, description length and result size is community opinion with no measurement behind it. — *(Verified for the rules [S7][S2]; the guidance Flagged)*

## Confidence summary

Verified: the spec revision and all its architectural changes, the deprecation registry and its removal
date, transports, primitives table, C#/TS/Python package identities, Claude Code's hook event list and
blocking mechanism, the config filenames and precedence, the security quotes, and the absence of a Copilot
hook surface. Inferred: Copilot cloud agent's `.mcp.json` usage; Zed's tool-invocation support. Flagged:
Cursor and Windsurf config details (docs inaccessible during research); tool-granularity and output-size
guidance (opinion, no measurements found); external security analyses (threat classes confirmed by the spec,
individual authorship not fetched).

**Load-bearing Flagged claims:** none gate a decision — but the **absence of measured guidance on tool
granularity and result size** is itself the finding, and it means our own tool design must be validated by
evaluation rather than by copying a best practice that does not exist.

## Design implications

- **Target `2026-07-28` and treat the SDK as the compatibility layer.** Statelessness suits a daemon well —
  no session affinity to manage — but it means every request must carry enough context, so tool signatures
  need to be self-sufficient rather than relying on prior calls.
- **Do not design on Sampling or Roots.** Anything that wanted "server asks the model" becomes MRTR
  elicitation or a client-side call; anything that wanted Roots becomes an explicit workspace/repo parameter
  on the tool. Since the daemon already knows the workspace from `ATLAS_WORKSPACE_ID`, this costs nothing.
- **Use the C# SDK and host both transports**: stdio for per-workspace agent launches, Streamable HTTP for
  the local daemon. Validate `Origin` and bind to localhost — the spec makes this a MUST.
- **Split read and write tools explicitly, and mark them.** `readOnlyHint` exists; it is *untrusted* by
  clients, but setting it correctly is still right. The real enforcement is that write tools can only create
  `Decision`/`Note`/`Term`/link nodes.
- **Return tools in a deterministic order** — a free prompt-cache win that the spec explicitly calls out.
- **Set `isError: true` for operational failures**; reserve JSON-RPC errors for protocol failures. Agents
  handle the two differently.
- **Treat every graph value returned to an agent as untrusted content.** A `Note` written by one agent and
  read by another is a prompt-injection channel *inside our own product*. Any agent-writable text that later
  enters another agent's context needs the same scrutiny as web content.
- **Build the refresh trigger on Claude Code hooks** (`PostToolUse`, `FileChanged`, `Stop`) **and a file
  event bus**, because Copilot has no hook surface at all. The file bus is the universal floor; hooks are
  the low-latency path where they exist.
- **Ship both `.mcp.json` and `.vscode/mcp.json`** with correct scoping, and document the trust-approval
  step for Claude Code — a server nobody approved is a server nobody uses.
- **Validate tool design with evals, not with folklore.** Since no measured guidance exists on granularity
  or result size, our own `describe`/`impact_of`/`architecture` tools need an eval harness that measures
  whether agents actually select and use them correctly.

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The exact names in
`references.md` — spec revision, package IDs, config filenames, hook events, error codes — are the ones to
quote rather than recall. **Re-run `/collectknowledge` on this topic before any MCP-facing design work that
starts more than a quarter after the compiled date.**
