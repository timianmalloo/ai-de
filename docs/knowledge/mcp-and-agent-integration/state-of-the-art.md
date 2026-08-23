---
id: kb-mcp-sota
title: "MCP & Agent Integration — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [mcp, spec, transports, hooks, security, agent-memory]
links:
  - { to: kb-mcp-agent-integration, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  What MCP is as of the 2026-07-28 revision — stateless base protocol, two transports, the live
  and deprecated primitives — plus the SDKs, the hook surfaces per client, the documented
  security model, and the prior art for agent-written knowledge.
---

# State of the art — MCP & coding-agent integration

## The protocol as of `2026-07-28`

**Base.** JSON-RPC 2.0 over UTF-8. **No handshake**: capability negotiation is per-request via
`_meta.io.modelcontextprotocol/clientCapabilities` and `_meta.io.modelcontextprotocol/protocolVersion` on
*every* request. Servers **MUST** implement `server/discover` to advertise supported versions; a mismatch
returns `UnsupportedProtocolVersionError` (`-32022`) with a `supported` list and the client retries.
Extensions are negotiated through a `capabilities.extensions` map keyed by reverse-domain identifiers
(e.g. `io.modelcontextprotocol/tasks`). Revisions `2026-07-28`+ are the **modern era**; `2025-11-25` and
earlier are the **legacy era**; clients detect legacy servers by transport-specific probes. *(Verified, [S1][S2][S6])*

**What was removed or replaced in this revision** — the list is long enough that older tutorials are
actively misleading:

| Removed / replaced | Now |
|---|---|
| `initialize` + `notifications/initialized` | nothing — stateless, per-request `_meta` |
| `Mcp-Session-Id`, protocol sessions | nothing |
| HTTP GET endpoint | removed; SSE is only a POST *response* format |
| `ping`, `logging/setLevel`, `notifications/roots/list_changed` | removed |
| `resources/subscribe` / `unsubscribe` | `subscriptions/listen` (single POST stream) |
| server-initiated JSON-RPC requests | **MRTR**: `resultType: "input_required"` + `InputRequiredResult`, client retries with `inputResponses` |
| — | every result carries `resultType`: `"complete"` \| `"input_required"` |
| — | `ttlMs` / `cacheScope` caching hints on list/read results |
| — | advisory: return tools in **deterministic order** for LLM prompt-cache hits |

*(Verified, [S2])*

**Transports.**

| Transport | Status | Notes |
|---|---|---|
| **stdio** | Standard | newline-delimited JSON-RPC over the child process's stdin/stdout; `notifications/cancelled` for cancellation |
| **Streamable HTTP** | Standard | single POST endpoint; server returns JSON or an SSE stream per request; no session IDs; no GET |
| HTTP+SSE | **Deprecated** since 2025-03-26 | removal pending SEP-2596 reaching Final; Python SDK retains compat |
| WebSocket | not in the spec | Claude Code accepts `"type":"ws"` in config |

*(Verified, [S3][S4][S5])*

**Primitives.**

| Primitive | Status | Controlled by |
|---|---|---|
| **Tools** | Active | model |
| **Resources** | Active | application |
| **Prompts** | Active | user |
| **Elicitation** | Active (via MRTR) | server-initiated |
| **Completions** | Active | autocomplete utility |
| Sampling | **Deprecated** | was client-side LLM call |
| Roots | **Deprecated** | was client filesystem hints |
| Logging | **Deprecated** | was server→client logs |
| Tasks / MCP Apps | Extensions | `io.modelcontextprotocol/tasks`, `…/ui` |

Deprecated features have an earliest removal of the first release **on or after 2027-07-28**. Sampling
migrates to direct LLM API integration; Roots migrate to tool parameters or resource URIs. *(Verified, [S3][S11][S12])*

## SDKs

| SDK | Package | Version | Licence | Notes |
|---|---|---|---|---|
| **C#/.NET** | `ModelContextProtocol` (+ `.Core`, `.AspNetCore`, `.Extensions.Tasks`, `.Extensions.Apps`) | **2.2.0** | Apache-2.0 | **Joint Microsoft + Anthropic**, in the `modelcontextprotocol` GitHub org, originating from the community `mcpdotnet` project. Stable, not preview. `AddMcpServer()` on `IHostBuilder`; stdio and ASP.NET Core hosting |
| TypeScript | `@modelcontextprotocol/sdk` | see npm | MIT | peer-dep `zod` ≥3.25; stdio, Streamable HTTP, SSE (compat) |
| Python | `mcp` (`pip install "mcp[cli]"`) | **2.x** major | MIT | Python 3.10+; supports 2026-07-28 and all earlier revisions; v1.x on its own branch — pin `<2` to avoid the migration |

*(Verified, [S14][S15][S16][S17][S28])*

## Tool design — what is specified, and what is only opinion

**Specified** *(Verified, [S2][S7])*:
- `name` SHOULD be 1–128 characters from `[A-Za-z0-9_\-.]`, no spaces, case-sensitive, unique per server.
- `description` is the primary signal a model uses to select a tool.
- `inputSchema` must be a valid JSON Schema 2020-12 object; for a no-argument tool,
  `{"type":"object","additionalProperties":false}` is recommended.
- `outputSchema` is optional and describes structured output.
- Results carry `isError: boolean` — set it for **operational** failures; JSON-RPC errors are for
  **protocol/transport** failures.
- `annotations` (`readOnlyHint`, `destructiveHint`) exist, and clients **MUST** treat them as untrusted
  unless the server is trusted.
- Servers **SHOULD** return tools in deterministic order to improve prompt-cache hit rates.

**Not specified — community opinion only** *(Flagged)*: that descriptions are contracts; that fewer coarse
tools beat many fine ones; the token cost of tool schemas injected on every inference; any maximum result
size (community suggests "a few KB", with no measurement). No authoritative source was found for any of these.

## Client and hook matrix

**Claude Code** — the richest surface. Transports stdio / HTTP / SSE / WebSocket. Config: `.mcp.json`
(project, committed, requires per-workspace trust approval), `~/.claude.json` (local — the default for
`claude mcp add`), `~/.claude/settings.json` (user), managed/MDM settings. Precedence highest→lowest:
**managed → CLI `--settings` → `.claude/settings.local.json` → `.claude/settings.json` → user settings**.
`${CLAUDE_PROJECT_DIR}` is set in the spawned server's environment and `${VAR}` expansion with defaults is
supported in `args`. *(Verified, [S19][S20])*

Its hook system has ~31 documented events across session (`Setup`, `SessionStart`, `SessionEnd`), turn
(`UserPromptSubmit`, `UserPromptExpansion`, `Stop`, `StopFailure`), the agentic loop (`PreToolUse`,
`PermissionRequest`, `PermissionDenied`, `PostToolUse`, `PostToolUseFailure`, `PostToolBatch`,
`SubagentStart`, `SubagentStop`, `TaskCreated`, `TaskCompleted`), MCP (`Elicitation`, `ElicitationResult`),
filesystem and lifecycle (`InstructionsLoaded`, `ConfigChange`, `CwdChanged`, `DirectoryAdded`,
`FileChanged`, `WorktreeCreate`, `WorktreeRemove`), and context/display (`PreCompact`, `PostCompact`,
`MessageDisplay`, `TeammateIdle`, `Notification`). Handlers may be a shell command (JSON on stdin), an HTTP
endpoint, or an LLM prompt. **`PreToolUse` blocks** with `hookSpecificOutput.permissionDecision: "deny"`;
`PermissionDenied` can request a retry with `hookSpecificOutput.retry: true`; the `EndConversation` tool
skips both `PreToolUse` and `PostToolUse`. *(Verified, [S18])*

**VS Code / Copilot in the IDE** — Tools, Resources, Prompts and MCP Apps supported; config in
`.vscode/mcp.json` (workspace), a user-profile `mcp.json`, remote-user config, or
`devcontainer.json` → `customizations.vscode.mcp.servers`. `sandboxEnabled: true` exists on macOS/Linux,
not Windows. **No hook system** — tool approval is a UI prompt, not a programmable surface. *(Verified, [S22])*

**GitHub Copilot cloud agent** — declarative customisation only: `.github/copilot-instructions.md`,
`.github/instructions/*.instructions.md`, `AGENTS.md` (nearest-ancestor wins), `CLAUDE.md`/`GEMINI.md`, and
`.github/workflows/copilot-setup-steps.yml` (must be on the default branch) for pre-run environment setup.
**No hook equivalent, documented as absent.** MCP tools come from a portable `.mcp.json`.
*(Verified for the customisation surface and the absence [S23][S24][S25]; the `.mcp.json` path Inferred)*

**Cursor / Windsurf / Zed** — Cursor and Windsurf support MCP tools, with config details **Flagged**
(documentation inaccessible during research). Zed used MCP first for read-only **context servers**
(resources/prompts) via extensions; full tool support is Inferred. *(Flagged / Inferred, [S27])*

## Security

The spec's own guidance, quoted: *"Tools represent arbitrary code execution and must be treated with
appropriate caution."* · *"Hosts must obtain explicit user consent before invoking any tool."* · *"Clients
MUST consider tool annotations to be untrusted unless they come from trusted servers."* *(Verified, [S1][S7])*

| Threat | Spec position | Mitigation named |
|---|---|---|
| Prompt injection via tool results | acknowledged; verify trust before connecting content-fetching servers | user consent; **no automated enforcement** |
| Tool poisoning / rug-pull descriptions | annotations explicitly untrusted | trust only verified servers; explicit consent |
| Confused deputy (OAuth proxy) | full attack + mitigation sequence in the best-practices doc | per-client consent, `state` validation, `__Host-` cookies, exact redirect-URI matching |
| Token passthrough | named an anti-pattern | validate the audience claim; never forward unvalidated tokens |
| Over-broad scopes | servers SHOULD implement access controls | per-request authorization |
| DNS rebinding | Streamable HTTP servers **MUST** validate `Origin`, return 403 on mismatch | bind to localhost; authenticate |
| Rug-pull tool changes | mitigated by `listChanged` + re-fetch; deterministic ordering required | client caching + re-validation |

*(Verified, [S1][S13])*

## Agent-writable knowledge — prior art

| Mechanism | Adopted by | Format | Written by |
|---|---|---|---|
| `CLAUDE.md` (project/user/local/org scopes) | Claude Code | Markdown | human |
| Claude Code **auto-memory** | Claude Code | Markdown notes, capped ~200 lines / 25 KB per repo | **the agent**, via `/remember` |
| `AGENTS.md` | Copilot cloud agent + the agentsmd.org convention | Markdown, nearest-ancestor wins | human |
| `.github/copilot-instructions.md` | Copilot everywhere | Markdown | human |
| `@modelcontextprotocol/server-memory` | any MCP client | **JSONL knowledge graph**: entities + relations (directed, active voice) + observations (atomic facts) | the agent, via `create_entities`, `create_relations`, `add_observations`, `delete_*`, `read_graph`, `search_nodes`, `open_nodes`; exposed as a subscribable resource at `memory://knowledge-graph` |
| Cursor rules, Windsurf rules | Cursor, Windsurf | Markdown | human |

What is actually established: **Markdown instruction files are the most widely adopted pattern** because
they are zero-dependency, version-controlled and human-inspectable; **MCP knowledge-graph servers work but
require the agent to proactively call write tools** — nothing captures observations passively; and Claude
Code's auto-memory is the only automatic agent-writes-from-corrections mechanism observed, and it is
proprietary and not MCP-based. *(Verified, [S21][S26])*
