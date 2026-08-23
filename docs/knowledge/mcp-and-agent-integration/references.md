---
id: kb-mcp-references
title: "MCP & Agent Integration — references, versions, packages and exact names"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, mcp-spec, package-ids, hook-events, config-files]
links:
  - { to: kb-mcp-agent-integration, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The spec revisions, deprecation dates, error codes, package IDs, config filenames and the
  complete Claude Code hook event list — every load-bearing exact name in this domain, read
  from its primary source.
---

# Reference information

## Specifications

- **MCP specification, revision `2026-07-28`** — the current revision; stateless base protocol, MRTR,
  Streamable HTTP. Schema authority: `schema/2026-07-28/schema.ts` in the specification repository.
  *(Verified, [S1][S2])*
- **Previous revision `2025-11-25`** — the legacy-era boundary; had the `initialize` handshake.
  *(Verified, [S2])*
- **Security best practices** — the confused-deputy attack sequence, token-passthrough anti-pattern,
  DNS-rebinding requirements. *(Verified, [S13])*
- **JSON Schema 2020-12** — the schema dialect required for `inputSchema`. *(Verified, [S7])*

## Versions and dates

| Fact | Value |
|---|---|
| Current spec revision | **`2026-07-28`** |
| Legacy-era boundary revision | `2025-11-25` |
| Streamable HTTP introduced | `2025-03-26` |
| HTTP+SSE deprecated since | `2025-03-26` (SEP-2596) |
| Sampling / Roots / Logging deprecated in | `2026-07-28` (SEP-2577) |
| Earliest removal of deprecated features | first release **on or after 2027-07-28** |
| MRTR introduced | `2026-07-28` (SEP-2322) |
| `UnsupportedProtocolVersionError` code | **`-32022`** |
| `resultType` values | `"complete"` \| `"input_required"` |

*(Verified, [S2][S3])*

## Packages

| Language | Package ID | Version | Licence |
|---|---|---|---|
| C# | `ModelContextProtocol` | **2.2.0** | Apache-2.0 |
| C# | `ModelContextProtocol.Core` | 2.2.0 | Apache-2.0 |
| C# | `ModelContextProtocol.AspNetCore` | 2.2.0 | Apache-2.0 |
| C# | `ModelContextProtocol.Extensions.Tasks` | 2.2.0 | Apache-2.0 |
| C# | `ModelContextProtocol.Extensions.Apps` | 2.2.0 | Apache-2.0 |
| TypeScript | `@modelcontextprotocol/sdk` | see npm | MIT |
| Python | `mcp` (`pip install "mcp[cli]"`) | 2.x major | MIT |
| Memory server | `@modelcontextprotocol/server-memory` | see npm | — |

C# SDK docs: `csharp.sdk.modelcontextprotocol.io`. Origin: the community project `mcpdotnet` (Peder
Holdgaard Pederson), now co-maintained by Microsoft and Anthropic. *(Verified, [S14][S15][S28])*

## Config file names and scopes (load-bearing — these differ per client)

| Client | File | Scope | Note |
|---|---|---|---|
| Claude Code | `.mcp.json` | project root, committed | requires workspace trust approval |
| Claude Code | `~/.claude.json` | user / local | default scope for `claude mcp add` |
| Claude Code | `.claude/settings.json` | shared project | MCP server approvals stored here |
| Claude Code | `.claude/settings.local.json` | local project | git-untracked; add to `.gitignore` |
| Claude Code | `~/.claude/settings.json` | user global | |
| Claude Code | `CLAUDE.md`, `.claude/CLAUDE.md`, `CLAUDE.local.md`, `~/.claude/CLAUDE.md` | project / local / user | instruction files |
| VS Code | `.vscode/mcp.json` | workspace, committed | `servers` key |
| VS Code / Agent Host | `.mcp.json` | repo root | portable; also read by the Copilot Agent Host |
| VS Code / Agent Host | `~/.copilot/mcp-config.json` | user | portable |
| GitHub Copilot | `.github/copilot-instructions.md` | repo-wide | |
| GitHub Copilot | `.github/instructions/*.instructions.md` | path-specific | |
| Generic | `AGENTS.md` | nearest ancestor in the tree | honoured by multiple tools |
| GitHub Copilot | `.github/workflows/copilot-setup-steps.yml` | repo | **must be on the default branch** |

Claude Code precedence, highest → lowest: **managed/MDM → CLI `--settings` → `.claude/settings.local.json`
→ `.claude/settings.json` → `~/.claude/settings.json`**. `${CLAUDE_PROJECT_DIR}` is set in the spawned
server's environment; `${VAR}` expansion with default syntax is supported in `args`. *(Verified, [S19][S20][S22])*

## Claude Code hook events (complete documented list)

`Setup`, `SessionStart`, `SessionEnd`, `UserPromptSubmit`, `UserPromptExpansion`, `PreToolUse`,
`PermissionRequest`, `PermissionDenied`, `PostToolUse`, `PostToolUseFailure`, `PostToolBatch`,
`SubagentStart`, `SubagentStop`, `TaskCreated`, `TaskCompleted`, `Stop`, `StopFailure`, `TeammateIdle`,
`InstructionsLoaded`, `ConfigChange`, `CwdChanged`, `DirectoryAdded`, `FileChanged`, `WorktreeCreate`,
`WorktreeRemove`, `PreCompact`, `PostCompact`, `Elicitation`, `ElicitationResult`, `Notification`,
`MessageDisplay`.

- Handler types: `command` (shell process, JSON on stdin), HTTP endpoint (POST, JSON body), LLM prompt.
- **`PreToolUse` blocks** with `hookSpecificOutput.permissionDecision: "deny"` (+ optional
  `permissionDecisionReason`).
- **`PermissionDenied` retries** with `hookSpecificOutput.retry: true`.
- The **`EndConversation` tool skips both `PreToolUse` and `PostToolUse`** — a documented exception.
- Blocking events also include `UserPromptSubmit` and `UserPromptExpansion`.

*(Verified, [S18])*

## Tool contract rules (specified)

- `name`: 1–128 chars, `[A-Za-z0-9_\-.]`, no spaces, case-sensitive, unique per server.
- `inputSchema`: valid JSON Schema 2020-12 object; no-argument tools use
  `{"type":"object","additionalProperties":false}`.
- `outputSchema`: optional, describes structured output.
- `isError: true` for **operational** failures; JSON-RPC errors for **protocol/transport** failures.
- `annotations` (`readOnlyHint`, `destructiveHint`) — clients **MUST** treat as untrusted unless the server
  is trusted.
- Servers **SHOULD** return tools in deterministic order (prompt-cache hit rate).

*(Verified, [S2][S7])*

## Security requirements quoted from the spec

> "Tools represent arbitrary code execution and must be treated with appropriate caution."
> "Hosts must obtain explicit user consent before invoking any tool."
> "Clients MUST consider tool annotations to be untrusted unless they come from trusted servers."

Streamable HTTP servers **MUST** validate the `Origin` header and **MUST** return `403` on an invalid
origin (DNS-rebinding defence). *(Verified, [S1][S5][S7][S13])*

## Not specified (opinion only — Flagged)

Tool granularity (few coarse vs many fine), description length, the token cost of tool definitions injected
per inference call, and any maximum tool-result size. Community guidance suggests keeping results to "a few
KB"; **no authoritative source or measurement was found**.
