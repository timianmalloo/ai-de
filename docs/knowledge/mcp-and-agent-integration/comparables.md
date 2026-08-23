---
id: kb-mcp-comparables
title: "MCP & Agent Integration — client and SDK comparison"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, claude-code, copilot, cursor, sdks]
links:
  - { to: kb-mcp-agent-integration, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Which MCP features each client supports, where its config lives, and what interception
  surface it offers — the matrix that decides which integration path is universal and which is
  Claude-Code-only.
---

# Comparable solutions & problem framings

## Client matrix

| Client | MCP features | Config location & scope | Hook / interception surface | Confidence |
|---|---|---|---|---|
| **Claude Code** | Tools ✅ Resources ✅ Prompts ✅ Elicitation ✅ · Sampling & Roots (legacy, deprecated) · WebSocket transport ✅ | `.mcp.json` (project, committed, trust-approved) · `~/.claude.json` (local — default for `claude mcp add`) · `~/.claude/settings.json` (user) · managed/MDM. Precedence: managed > CLI > `.claude/settings.local.json` > `.claude/settings.json` > user | **~31 hook events**; `PreToolUse` **blocks** via `permissionDecision: "deny"`; `FileChanged`, `WorktreeCreate/Remove`, `PostToolBatch`, `Elicitation`, … Handlers: shell command, HTTP endpoint, or LLM prompt | Verified [S18][S19][S20] |
| **VS Code / Copilot (IDE)** | Tools ✅ Resources ✅ Prompts ✅ MCP Apps ✅ · sandboxing on macOS/Linux | `.vscode/mcp.json` (workspace) · user-profile `mcp.json` · remote-user config · `devcontainer.json` → `customizations.vscode.mcp.servers` | **None.** Tool approval is a UI prompt, not programmable | Verified [S22] |
| **GitHub Copilot cloud agent** | Tools via a portable `.mcp.json`; resource/prompt exposure unconfirmed | `.mcp.json` (repo root) · `~/.copilot/mcp-config.json` (user) | **None — documented absence.** Only `.github/copilot-instructions.md`, `.github/instructions/*.instructions.md`, `AGENTS.md`, `CLAUDE.md`/`GEMINI.md`, and `copilot-setup-steps.yml` (default branch) for pre-run env setup | Verified for the absence [S23][S24][S25]; `.mcp.json` path Inferred |
| **Cursor** | Tools ✅ (community-confirmed); resources/prompts unknown | `.cursor/mcp.json` (project or user) | None documented | **Flagged** — docs inaccessible during research |
| **Windsurf** | Tools ✅ | project rules file | None documented | **Flagged** — not fetched |
| **Zed** | Context servers (resources/prompts) via extensions; tool invocation later | Zed extension config | None documented | Inferred [S27] |

**The conclusion this table forces:** the only interception surface that exists across clients is **the
filesystem**. Hooks are a Claude-Code accelerator, not a portable mechanism.

## SDK comparison

| SDK | Package | Version | Licence | Transports | Notes |
|---|---|---|---|---|---|
| **C# / .NET** | `ModelContextProtocol`, `.Core`, `.AspNetCore`, `.Extensions.Tasks`, `.Extensions.Apps` | **2.2.0** | Apache-2.0 | stdio, ASP.NET Core hosting | **Joint Microsoft + Anthropic**, in the `modelcontextprotocol` org, from the community `mcpdotnet`. **Stable, not preview.** `AddMcpServer()` on `IHostBuilder` |
| TypeScript | `@modelcontextprotocol/sdk` | see npm | MIT | stdio, Streamable HTTP, SSE (compat) | peer-dep `zod` ≥ 3.25, imports `zod/v4` internally |
| Python | `mcp` | **2.x** major | MIT | stdio, Streamable HTTP, SSE | Python 3.10+; supports 2026-07-28 and earlier; pin `<2` to stay on v1.x |

*(Verified, [S14][S15][S16][S17][S28])*

## Agent-memory mechanisms compared

| Mechanism | Written by | Format | Persistence & scope | Does well | Does badly |
|---|---|---|---|---|---|
| `CLAUDE.md` / `AGENTS.md` / `copilot-instructions.md` | human | Markdown | committed, repo/user scoped | zero dependency, reviewable, portable across tools | static; nothing learns from a session |
| Claude Code **auto-memory** | **the agent** | Markdown notes, ~200 lines / 25 KB cap per repo | per-repo | the only automatic write-from-correction mechanism observed | proprietary, not MCP, capped, Claude-Code-only |
| `@modelcontextprotocol/server-memory` | the agent, via tools | **JSONL knowledge graph** — entities, directed relations in active voice, atomic observations | file-backed per instance; exposed at `memory://knowledge-graph` | genuinely a graph; portable across MCP clients; subscribable resource | the agent must *choose* to write; nothing captured passively |
| Cursor / Windsurf rules | human | Markdown | project/user | familiar | static, tool-specific |

*(Verified, [S21][S26])*

**What this says for AI-DE:** the `server-memory` design — typed entities, directed relations, atomic
observations, a subscribable resource — is close enough to our `Decision`/`Note`/`Term` + `RELATES_TO`
model to be worth reading as a reference implementation, and its main lesson is the failure mode: *nothing
is captured unless the agent decides to call a write tool.* A design that depends on agents volunteering
knowledge will get less of it than expected.

## Adjacent framings worth borrowing

- **`AGENTS.md` as a convention** rather than a product feature — it is honoured by multiple tools, which is
  the only reason it is useful. Our projections should target it.
- **The extension mechanism** (`capabilities.extensions` keyed by reverse-domain identifiers) is the spec's
  own answer to "we need something the protocol does not have" — the right precedent if our graph needs a
  capability MCP lacks.
- **`copilot-setup-steps.yml`** is the only pre-run environment hook Copilot offers, and it is the correct
  place to install and register a local MCP server for the cloud agent.
