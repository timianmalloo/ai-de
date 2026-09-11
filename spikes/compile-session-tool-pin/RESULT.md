# Spike result — compile-session-tool-pin (a source read; no model call)

- **Run:** 2026-09-11 · adapter `@agentclientprotocol/claude-agent-acp` **0.75.1**, installed under
  `spikes/acp-subscription-lane/node_modules/` (git-ignored; the sha below pins the exact build read)
- **Adapter sha256:** `c22424c297429378166524b59ee6bed239c999a420ad0dd06571b768efa7525b` (`dist/acp-agent.js`)
- **Command:** `python spikes/compile-session-tool-pin/verify-adapter-contract.py [path]`
- **Raw output:** [`RESULT-raw.txt`](RESULT-raw.txt) · exit code `0`

## The question

Addendum D §A13.4 C1 pins the compile session's tools to none through `session/new`
`_meta.claudeCode.options.tools: []`. Ruling 71 delivers the typed `session/new` tools argument on
`AcpLaneClient.NewSessionAsync` on `feature/exit-evidence`; the lane-pin node's spike note
(`docs/notes/lane-pin-spike.md`) **has not landed** on that branch at `757af057` (checked:
`git ls-tree` finds no such note; `AcpLaneClient.cs` there still sends `{cwd, mcpServers: []}` only)
— so this spike reads the adapter source itself, as the brief instructs. **No session was created and
no model was called**: the wire observation is P-D5, the deployment gate ADR-0036 names; this spike
establishes only what the *source* does with `_meta`, which is the contract ADR-0035 depends on.

## Findings (each a regex the installed source must contain; line numbers in this build)

| Fact | Line | What it carries |
|---|---|---|
| `const tools = userProvidedOptions?.tools ?? (params._meta?.disableBuiltInTools === true ? [] : { type: "preset", preset: "claude_code" })` | 5883 | `_meta.claudeCode.options.tools` is the **primary** pin; `_meta.disableBuiltInTools: true` is the legacy shorthand for `[]` (the adapter's own comment at 5880–5882 says callers should prefer the array); with neither, the SDK gets the full `claude_code` preset |
| `tools,` in the SDK query options | 6008 | the resolved value reaches the SDK verbatim |
| `disallowedTools: [...(userProvidedOptions?.disallowedTools \|\| []), ...disallowedTools]` | 6007 | the braces pin (`disallowedTools` naming the write tools) is concatenated with the adapter's own and reaches the SDK |
| `settingSources: ["user", "project", "local"]` | 5962 | the compile session loads the repository's `CLAUDE.md`, settings **and hooks** from its cwd — the constitution by harness load (Addendum D §A12.3) and the hooks residual (§A13.4) are both this line |
| `...userProvidedOptions` spread **after** `settingSources` | 5964 | a caller *could* override `settingSources` through `_meta`; the architecture rejects doing so (dropping `project` drops `CLAUDE.md`) |
| `const permissionMode = resolvePermissionMode(settingsManager.getSettings().permissions?.defaultMode, …)` | 5856 | the permission mode comes from the repository's/user's settings, not from the host — the host's reject-all `session/request_permission` handler is **not** the control |
| *"Claude Code applies bypassPermissions before invoking canUseTool"* | 5274 | an auto-allowed or bypassed tool never reaches the host's `canUseTool` — the adapter's own comment. **Citation correction:** the spec quotes *"canUseTool is not guaranteed to run for tools that the current permission mode auto-allows"* at `:5856-5857, 5883-5884`; that sentence does **not** appear in this build (Ruling 71 records the Owner did not see it either). The *mechanism* is verified at 5274–5279 and 5856; the quoted wording is a paraphrase and is not cited as a quote by the architecture |
| *"a `/model <name>` command typed as a prompt — which the CLI executes as a local command"* | 6034–6035 | why the compile prompt begins with a fixed host header (§A8.3, P-D3) |

**RESULT: PASS — every adapter-side fact the pin rests on holds in adapter 0.75.1.** [Verified in
source; the wire is unobserved until P-D5; the enforcement point — the Claude Code CLI binary the SDK
launches at `pathToClaudeCodeExecutable: process.env.CLAUDE_CODE_EXECUTABLE ?? claudeCliPath()`,
`:6003` — is outside this read, which is why ADR-0035 pins the CLI binary's sha as well]

## What this does and does not establish

- **Establishes (Verified, source):** `_meta.claudeCode.options.{tools, disallowedTools}` are honoured
  and reach the SDK; `mcpServers` from `_meta` is merged with `params.mcpServers` (6003–6006); the
  host's permission handler answers only what the SDK routes to `canUseTool`.
- **Does not establish (Flagged until P-D5):** that the SDK, given `tools: []` and a repository
  `.mcp.json`, answers a "read a file" prompt with **zero** `tool_call` frames of any name. That is the
  P-D5 wire spike — the first gate of the compile-mode ladder (ADR-0036) — and a failed P-D5 is a
  hard stop for every agentic rung, never a fallback. Two adapter facts sharpen it: `disableBuiltInTools`
  is **dead** beside `tools` (`??` never evaluates its right-hand side when `tools` is present), so it is
  no belt; and `tools: []` does not remove MCP tools, so a repository `.mcp.json` is closed only by the
  `settings` deny / `disableAllMcpJsonServers` belt (key names Inferred) or observed closed by P-D5.
- **Re-run trigger:** any adapter bump. The check prints the sha; ADR-0035 pins 0.75.1 and the sha
  above, and the settings model refuses the agentic rungs when the recorded sha differs from the
  installed adapter's (Addendum D US-D11).
