---
id: kb-mcp-sources
title: "MCP & Agent Integration — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-mcp-agent-integration, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The full access-dated source list behind the MCP knowledge base, keyed [S1]..[S29], with a
  freshness warning — this is the fastest-moving topic in the base.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

> **Freshness warning.** MCP changed its handshake model, its transports and three of its primitives inside
> a single year. Any fact here that is more than a quarter old should be re-read from the primary source
> before a design depends on it.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | MCP specification `2026-07-28` | standard (spec) | https://modelcontextprotocol.io/specification/2026-07-28 | Base protocol, security statements (quoted), primitives |
| S2 | MCP spec changelog | standard | https://modelcontextprotocol.io/specification/2026-07-28/changelog | Statelessness, MRTR (SEP-2322), removals, `resultType`, deterministic ordering |
| S3 | MCP deprecated-features registry | standard | https://modelcontextprotocol.io/specification/2026-07-28/deprecated | Sampling/Roots/Logging deprecation, HTTP+SSE, removal timeline |
| S4 | Transports overview | standard | https://modelcontextprotocol.io/specification/2026-07-28/basic/transports | stdio and Streamable HTTP |
| S5 | Streamable HTTP transport | standard | https://modelcontextprotocol.io/specification/2026-07-28/basic/transports/streamable-http | POST-only model, `Origin` validation MUST, 403 requirement |
| S6 | Versioning & compatibility | standard | https://modelcontextprotocol.io/specification/2026-07-28/basic/versioning | `server/discover`, modern/legacy eras, `-32022`, extensions |
| S7 | Tools specification | standard | https://modelcontextprotocol.io/specification/2026-07-28/server/tools | Name rules, `inputSchema`, `isError`, untrusted annotations |
| S8 | Resources specification | standard | https://modelcontextprotocol.io/specification/2026-07-28/server/resources | Resource semantics |
| S9 | Prompts specification | standard | https://modelcontextprotocol.io/specification/2026-07-28/server/prompts | Prompt semantics |
| S10 | Elicitation specification | standard | https://modelcontextprotocol.io/specification/2026-07-28/client/elicitation | Server-initiated input via MRTR |
| S11 | Sampling specification (deprecated) | standard | https://modelcontextprotocol.io/specification/2026-07-28/client/sampling | Deprecation and migration path |
| S12 | Roots specification (deprecated) | standard | https://modelcontextprotocol.io/specification/2026-07-28/client/roots | Deprecation and migration path |
| S13 | Security best practices | standard (docs) | https://modelcontextprotocol.io/docs/2026-07-28/tutorials/security/security_best_practices | Confused deputy, token passthrough, DNS rebinding, tool poisoning |
| S14 | MCP C# SDK repository | primary (repo) | https://github.com/modelcontextprotocol/csharp-sdk | Packages, hosting model, origin in `mcpdotnet` |
| S15 | `ModelContextProtocol` on NuGet | primary (registry) | https://www.nuget.org/packages/ModelContextProtocol | Version 2.2.0, Apache-2.0 |
| S16 | `@modelcontextprotocol/sdk` on npm | primary (registry) | https://www.npmjs.com/package/@modelcontextprotocol/sdk | TS SDK, zod peer dependency |
| S17 | `mcp` on PyPI | primary (registry) | https://pypi.org/project/mcp/ | Python SDK v2.x, 3.10+, spec coverage |
| S18 | Claude Code hooks reference | primary (vendor docs) | https://code.claude.com/docs/en/hooks | Complete hook event list, blocking semantics, handler types |
| S19 | Claude Code MCP reference | primary | https://code.claude.com/docs/en/mcp | Config scopes, transports, env-var expansion |
| S20 | Claude Code settings reference | primary | https://code.claude.com/docs/en/settings | Settings precedence order |
| S21 | Claude Code memory reference | primary | https://code.claude.com/docs/en/memory | `CLAUDE.md` scopes, auto-memory and its caps |
| S22 | VS Code — MCP servers | primary | https://code.visualstudio.com/docs/copilot/chat/mcp-servers | `.vscode/mcp.json`, scopes, sandboxing, feature support |
| S23 | GitHub Copilot cloud agent overview | primary | https://docs.github.com/en/copilot/concepts/agents/cloud-agent/about-cloud-agent | Ephemeral environment; absence of a hook surface |
| S24 | GitHub custom instructions | primary | https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/add-custom-instructions/add-repository-instructions | Instruction file names, `AGENTS.md` nearest-ancestor rule |
| S25 | Copilot setup steps | primary | https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/customize-the-agent-environment | `copilot-setup-steps.yml`, default-branch requirement |
| S26 | MCP memory server | primary (repo) | https://github.com/modelcontextprotocol/servers/tree/main/src/memory | Entities/relations/observations model, tool list, JSONL storage |
| S27 | Zed MCP announcement | primary (vendor blog) | https://zed.dev/blog/mcp | Context-server framing |
| S28 | Microsoft + Anthropic C# SDK announcement | primary (vendor blog) | https://developer.microsoft.com/blog/microsoft-partners-with-anthropic-to-create-official-c-sdk-for-model-context-protocol/ | Joint maintenance |
| S29 | MCP `llms.txt` index | primary | https://modelcontextprotocol.io/llms.txt | Navigation of the spec surface |

## Source-quality notes

- S1–S13 are the specification itself — the top of the hierarchy. Every protocol fact, error code, MUST/SHOULD
  and quoted security statement comes from there.
- S14–S28 are vendor or project primary sources (registries, repositories, official documentation).
- **Cursor and Windsurf documentation could not be fetched** during research; every claim about them is
  Flagged.
- **External security analyses** (e.g. Rehberger, Willison) were not fetched due to rate limits. The threat
  *classes* are Verified from the spec's own security guidance; individual attributions are not.
- The claim that the Copilot cloud agent reads a portable `.mcp.json` is **Inferred** from the VS Code
  Agent Host documentation, not confirmed for the cloud agent.
