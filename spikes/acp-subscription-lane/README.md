# Spike — can a non-Claude-Code client drive a governed ACP lane on a Max subscription?

**Question.** The Conductor spec (§4.4, §12 Phase 1) rests its entire first phase on AI-DE acting as
an **ACP client** that drives `claude-code` as a governed lane, authenticated by a **Max
subscription**, with `cwd` set to a provisioned worktree — and with structured tool-call, edit and
permission events coming back. Every one of those was **Inferred** and jointly load-bearing: if
subscription auth were unavailable to a third-party client, Phase 1's exit evidence ("a real
governed run on claude-code, scored end-to-end, with zero terminal hosting") would be unreachable
as specified, and the whole phase would need re-cutting.

**Answer: it works.** Run 2026-09-09 on Windows 11, Node v24.18.0, `claude` CLI **2.1.266**, with
**no API key in the environment** (`env | grep ANTHROPIC` empty).

A plain Node process — not Claude Code — spoke newline-delimited JSON-RPC to
`@agentclientprotocol/claude-agent-acp@0.75.1`, was told `"label":"Claude Max"`, opened a session
with `cwd` on a git working tree, and received `tool_call`, `tool_call_update` (`kind:"edit"`), and
a `session/request_permission` carrying a structured diff. Nothing on that path is JavaScript-
specific: one child process, stdin/stdout, newline-delimited JSON.

## Method

Two probes, ~36 lines each, deliberately minimal. `probe-read.js` asks the agent to run a read-only
shell command and records every distinct `session/update` kind it sees. `probe-write.js` asks for a
file write, so the permission request path is exercised and then **rejected** (the spike must not
modify the tree it is pointed at).

```
cd spikes/acp-subscription-lane
npm install @agentclientprotocol/claude-agent-acp
node probe-read.js      # a line containing "label":"Claude Max" plus a tool_call update means YES
node probe-write.js     # exercises session/request_permission; answers reject
```

An `authRequired` error, or `-32601 Method not found`, means NO.

## Observed

- `initialize` → `protocolVersion: 1`, `agentInfo: {name:"@agentclientprotocol/claude-agent-acp",
  version:"0.75.1"}`, and **`authMethods: []`** — an empty list means *already authenticated*.
- `_auth/status_update` → `{"authStatus":{"kind":"account","label":"Claude Max","account":{"plan":"max"}}}`
- `session/new` with `cwd:"C:/projects/ai-de"` → a session id, and
  `modes.availableModes` = `default | acceptEdits | plan | auto | bypassPermissions`.
- `session/prompt` → `tool_call` `{status:"pending", kind:"execute", title:"Terminal",
  _meta:{claudeCode:{toolName:"Bash"}}}`, then `tool_call_update` with
  `rawInput:{"command":"git status --short"}`.
- Write path → `session/request_permission` with
  `content:[{type:"diff", path:"…hello.txt", oldText:null, newText:"hi\n"}]` and
  `options:[{optionId:"allow-once",kind:"allow_once"}, …]`. Rejected; turn ended `end_turn`.

## The captured corpus — the authoritative record

The prose above is a summary. **`frames/*.jsonl` is the evidence**, and where the two disagree the
frames win. Captured 2026-09-09; 88 lines total, every one verified to parse standalone.

| File | Lines | Direction |
| --- | --- | --- |
| `frames/read.jsonl` | 22 | received (read-only probe) |
| `frames/read.sent.jsonl` | 3 | sent |
| `frames/write.jsonl` | 59 | received (permission probe) |
| `frames/write.sent.jsonl` | 4 | sent |

**Top-level methods observed:** `_auth/status_update`, `session/update`, `session/request_permission`
(write probe only).

**`update.sessionUpdate` discriminators observed:** `available_commands_update`, `usage_update`,
`tool_call`, `tool_call_update`, `agent_message_chunk`.

**Explicitly NOT observed** — recorded because an absent kind is *not recorded*, never zero, and
because an earlier revision of this README implied otherwise:

- `user_message_chunk` and `agent_thought_chunk` — **neither probe's prompt triggered them.** They
  may well exist; these two runs did not elicit them.
- `_session/goal` and `_meta.jetbrains.air` — **never fired as traffic**; they occur only as
  declared capabilities in the `initialize` response's `_meta` block.

> **Extended by the N4 live runs, 2026-09-09.** Driving the same pinned adapter from the .NET
> client (`AiDe.Core.AcpProbe --live`, 137 frames on a read prompt and 52 on a write prompt) saw
> **one `session/update` discriminator this corpus does not contain: `session_info_update`.** It
> was carried whole under `ext` as `acp.session.update.session_info_update`, which is the
> `ext`-preservation mitigation doing its job on traffic captured two hours earlier and already
> out of date. The same runs also observed `authStatus.account.plan` as **`"Claude Max"`**, not
> `"max"` — `PROVENANCE.md` already records both spellings, so nothing may key on that value.
>
> The write run also confirmed the inbound direction end to end against a live agent:
> `session/request_permission` arrived, was answered with the `reject` option, and `hello.txt` was
> **not** created.

Redaction is documented in `frames/PROVENANCE.md`, which deliberately records the *rule* and not the
values — naming what you removed is how a redaction undoes itself.

## What this establishes

> **Correction, 2026-09-09.** An earlier revision of this README claimed *"the spec's package names
> are stale — it names the `@zed-industries` adapters."* **That claim was false.** The spec names no
> package at all: a case-insensitive search for `zed-industries`, `@zed` and `claude-code-acp` across
> `ai-de-conductor-spec-v1.html` returns **zero** hits. §4.1 says only *"ACP via Claude Code adapter
> (wraps official Agent SDK)"* and *"ACP via Codex adapter"*, and §4.1's preamble states that engines
> are **data, not code paths**. The package identity is therefore a **catalog fact to be pinned**,
> not a spec erratum. The error was mine; it is recorded here rather than silently edited away.

1. **The adapter packages to pin are `@agentclientprotocol/claude-agent-acp` (v0.75.1) and
   `@agentclientprotocol/codex-acp` (v1.10.0).** The ACP project moved out of `zed-industries`; the
   older `@zed-industries/claude-code-acp` last published 2026-03-26 at v0.16.2, pinning ACP SDK
   0.14.x. Anyone reaching for the `zed-industries` name from memory gets a March build. These values
   belong in the engine catalog entry, which §4.1 makes data, and the spec's own §13 risk row already
   says *"Pin adapter versions per workspace."*
2. **Framing is newline-delimited JSON, not LSP `Content-Length`.** SDK writer is
   `JSON.stringify(message) + "\n"`.
3. **The connection is bidirectional.** The agent calls *the client*:
   `session/request_permission`, `fs/read_text_file`, `fs/write_text_file`,
   `terminal/create|output|wait_for_exit|kill|release`, `elicitation/create`. A .NET implementation
   needs a **server loop**, not a request/response client. This is the single biggest architectural
   consequence.
4. **`ext` preservation is load-bearing on turn one, not defensive.** Traffic absent from schema v1
   appeared immediately: `_auth/status_update` as a **top-level method**, and `usage_update` as a
   **`session/update` discriminator**.

   > **Corrected 2026-09-09 against the captured corpus.** An earlier revision of this line listed
   > `usage_update`, `_auth/status_update`, `_session/goal` and `_meta.jetbrains.air` together as
   > traffic that "appeared", which the frames disprove in two ways:
   >
   > - **`_session/goal` and `_meta.jetbrains.air` never fired as events.** They appear **only as
   >   declared capabilities inside the `initialize` response's `_meta` block.** A capability
   >   announcement is not traffic, and reading it as traffic would have had the client waiting for
   >   frames that never arrive.
   > - **`usage_update` is not a top-level method.** It is delivered as a `session/update` frame
   >   whose `update.sessionUpdate` discriminator is `usage_update` — a different place in the
   >   envelope, and therefore a different mapper.
   >
   > This is what the corpus is for: the claim was written from truncated console output, and the
   > frames corrected it. **`ext` preservation remains load-bearing** — the corrected traffic is
   > still absent from schema v1 — but the *shape* was wrong, and the shape is what gets built.
5. **File edits are not a distinct update type.** They arrive as `tool_call` with `kind:"edit"` and
   a `content:[{type:"diff", …}]` payload.
6. **No official .NET SDK exists.** Four unofficial NuGet families exist, all low-download, none
   from the ACP org (official SDKs: Rust, TypeScript, Python, Java, Kotlin). Hand-rolling against
   `System.Text.Json` + `Process` is the cheaper and more controllable path.

## Residual risks — flagged, not closed

1. **Commercial, not technical — NOT ESTABLISHED, and it is the one that matters.** The adapter
   carries a `--hide-claude-auth` flag whose *purpose* is to let redistributors **disable**
   subscription auth (`shouldHideClaudeAuth()` → *"This integration does not support using
   claude.ai subscriptions."*). Its existence means the question "may a third-party product drive a
   customer's Max subscription?" is a live licensing question that **this probe cannot answer**.
   **Do not treat "it worked" as permission.** Settle by reading the Claude Code / Agent SDK
   commercial terms, or by asking Anthropic.
2. **API-key sources outrank the subscription.** `ANTHROPIC_API_KEY`, an `apiKeyHelper`, or a
   `/login` managed key take precedence over the stored subscription and would **silently bill the
   API instead**. Spec §4.2's invariant must be enforced against this specific list, not merely
   against "is an account configured".
3. **Subscription auth requires the local `claude` CLI to already be logged in.** The adapter reads
   that credential store; it does not carry its own. So AI-DE's "never handles subscription
   credentials" claim (§4.3) holds — but the readiness probe must check CLI login state.
4. **Subagent surface is draft.** `_meta.jetbrains.air.capabilities.nativeSubagentSessions` is a
   vendor-namespaced stopgap; the canonical `clientCapabilities.subagents` is still draft. Without
   a signal, subagents degrade to ordinary tool calls on the root session — which is exactly the
   spec's §G4 "summary-level leaves" fallback, for free.
5. **Schema v2 alpha is in flight** (`schema-v2.0.0-alpha.3`), and adapters publish near-daily.
   Pin `protocolVersion: 1` explicitly and **assert the echoed value**.
6. **Codex and Copilot adapters were read, not run.** Settle by re-running `probe-read.js` with the
   spawn target swapped to `npx -y @agentclientprotocol/codex-acp` or `copilot --acp`; a
   `{"protocolVersion":1,…}` line back means yes.

## Note on the probe scripts

`probe-read.js` hard-codes `cwd:"C:/projects/ai-de"`. `cwd` **must be absolute** — a relative path
returns `-32602 "Invalid params: \`cwd\` must be an absolute path"`. Change it before reuse
elsewhere.
