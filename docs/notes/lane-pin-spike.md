---
id: note-lane-pin-spike
title: "The governed lane's shell is pinned off through `_meta.claudeCode.options.disallowedTools` — the adapter's own path, read from its source"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, spike, agent-plane, acp, ruling-71, f5, security]
links:
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: note-addendum-d-compile-session-tools, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: proof-conductor-agent-plane, rel: relates-to }
  - { to: proof-lane-pin-ruling-71, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  A source spike (no model call) against adapter @agentclientprotocol/claude-agent-acp 0.75.1: the
  `session/new` extension slot the adapter reads is `_meta.claudeCode.options`, `disallowedTools`
  there is an array of SDK tool names spread into the SDK's option, and `"Bash"` is the shell tool's
  name. Ruling 71's spelling holds verbatim. The wire observation is the F5 run's Proof Pack.
---

# The governed lane's shell is pinned off through `_meta.claudeCode.options.disallowedTools`

- **Kind:** resolved-question (Spike Protocol, source-only)
- **Confidence:** Verified (every claim below was read from the vendored source at the cited line)
- **Made during:** `/implement` of Ruling 71 on `feature/exit-evidence` (session `f5-lane-pin`)

## The question

Ruling 71 (`note-addendum-c-council-rulings`) requires `AcpLaneClient.NewSessionAsync` to send
`_meta.claudeCode.options.disallowedTools: ["Bash"]` for the governed lane. The ruling's spelling is
the Owner's reading of the adapter; **the adapter's source is the contract**, so the spelling, the
member's type, and the tool's name were re-read before any code depended on them.

## Pin

`spikes/acp-subscription-lane/node_modules/@agentclientprotocol/claude-agent-acp` — `package.json`
version **`0.75.1`**; `package-lock.json` integrity
`sha512-Un6I4BRkhpCFS3I7kr5C/lkAm8Nc3VuGZU2YQ3xIpJAIxV94iWO0Q2CH2QABxMERpONRu4Le6XC9V+5PImQZ2A==`.
SDK peer `@anthropic-ai/claude-agent-sdk` **`0.3.257`**. All line numbers are into `dist/acp-agent.js`
of that adapter unless stated.

## Findings

| # | Claim | Where | Confidence |
|---|---|---|---|
| 1 | The extension slot the adapter reads on `session/new` is `params._meta`, and the options object is `_meta.claudeCode.options` | `:5859` `const sessionMeta = params._meta;` · `:5860` `const userProvidedOptions = sessionMeta?.claudeCode?.options;` | Verified |
| 2 | `disallowedTools` from that object is spread into the SDK option as an array, ahead of the adapter's own additions | `:6007` `disallowedTools: [...(userProvidedOptions?.disallowedTools \|\| []), ...disallowedTools],` | Verified |
| 3 | With no `_meta`, the SDK receives the `claude_code` preset — the lane is not toolless by default | `:5883-5884` `const tools = userProvidedOptions?.tools ?? (params._meta?.disableBuiltInTools === true ? [] : { type: "preset", preset: "claude_code" });` · `:6008` `tools,` | Verified |
| 4 | The permission mode is resolved from `settingSources: ["user","project","local"]`, so the repository's committed `.claude/settings.json` is in scope for a lane | `:5856` `resolvePermissionMode(settingsManager.getSettings().permissions?.defaultMode, …)` · `:5962` `settingSources: ["user", "project", "local"],` | Verified |
| 5 | The SDK's `disallowedTools` is `string[]` of tool names, removed from the model's context "even if they would otherwise be allowed" | `@anthropic-ai/claude-agent-sdk/sdk.d.ts:1465-1469` | Verified |
| 6 | `"Bash"` is the SDK's name for the shell tool | `sdk-tools.d.ts:750` `tool: "Bash";` · `sdk.d.ts:1498` example `['Bash', 'Read', 'Edit']` · adapter `:7508` `toolUse.name === "Bash"` | Verified |
| 7 | `tools` accepts `string[]` (including `[]` = disable all built-ins) or the preset object — the shape Addendum D's C1 will reuse with `tools: []` | `sdk.d.ts:1497-1505` | Verified |

**Ruling 71's spelling holds verbatim**: `_meta.claudeCode.options.disallowedTools`, an array of tool
names, `"Bash"` the shell. No divergence to report.

## What this spike does not establish

- **The pin is unobserved on the wire.** Findings 1–7 are the adapter's source, not a frame
  exchanged with a running adapter. Per Ruling 71 condition (2), the F5 run's Proof Pack must record
  the outgoing `session/new` frame and every observed `tool_call` name; a run whose pack lacks either
  is *not recorded*, not passed. The general closure is Addendum D's P-D5 spike.
- **Precedence of `...userProvidedOptions` at `:5964`.** The whole options object is spread into the
  SDK options before the adapter's own overrides; a caller could therefore set members this repo
  never sends (`settingSources`, `permissionMode`, …). The typed record on `NewSessionAsync` sends
  only `tools` and `disallowedTools` — nothing else can reach `_meta` from this client.
- The `"canUseTool is not guaranteed"` comment the Addendum D spec cites at `:5883-5884` is **not at
  those lines** in 0.75.1 (the Owner recorded the same). Not load-bearing for this change.

## Validation condition

Holds while the vendored adapter is `0.75.1` with the integrity above. An adapter bump re-reads
findings 1–3 and 6 (the `_meta` path, the spread, the preset default, the tool name) before the
pin is trusted — the wire test in `AcpLaneClientTests` asserts the *frame we send*, not what the
adapter does with it.

## Promotion rule

Absorbed by the Addendum D P-D5 spike artifact when it lands; until then this note is the source
grounding for `LaneSessionOptions` (`src/AiDe.Core/AgentPlane/AcpLaneClient.cs`).
