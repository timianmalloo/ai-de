# Provenance — captured ACP frame corpus

**These are real, captured wire frames from a live claude-code ACP session — not authored,
not hand-written, not synthesized.** Each `.jsonl` file is the exact byte sequence of every
complete newline-terminated line the adapter subprocess wrote to its stdout (received frames)
or that the probe wrote to the adapter's stdin (sent frames), captured verbatim before any
`JSON.parse`, truncation, or re-serialization — then redacted in place per the rules below.

## What was run

- OS: Windows 11 Pro (10.0.26200)
- Node: `v24.18.0`
- npm: `12.0.2`
- `claude` CLI: `2.1.266 (Claude Code)`
- Adapter package: `@agentclientprotocol/claude-agent-acp`, version **0.75.1**
  (installed into `spikes/acp-subscription-lane/node_modules/`, not committed)
- Authentication: Claude Max subscription, no `ANTHROPIC_API_KEY` in the environment.
  `initialize` result showed `authMethods: []` (already authenticated) and an
  `_auth/status_update` with `authStatus.kind: "account"`, `label: "Claude Max"`.
- Date captured: 2026-09-09 (UTC ~18:47)

## Exact commands run

```
cd spikes/acp-subscription-lane
npm install @agentclientprotocol/claude-agent-acp
node probe-read.js
node probe-write.js
```

`probe-read.js` asked the agent to run `git status --short` (read-only) in `C:/projects/ai-de`.
`probe-write.js` asked the agent to create `hello.txt` in a scratch directory
(`C:/Users/<home>/AppData/Local/Temp/claude/.../scratchpad/probe`, outside the repository) and
answered the resulting `session/request_permission` with the `reject` option — the probe never
allows the write. Verified after the run: `hello.txt` was **not** created in that scratch
directory, and `git status --short` at the repository root showed no changes from the probe run
itself.

## Files in this directory

| File | Direction | Lines | Source probe |
|---|---|---:|---|
| `read.jsonl` | received (adapter stdout) | 22 | `probe-read.js` |
| `read.sent.jsonl` | sent (probe stdin to adapter) | 3 | `probe-read.js` |
| `write.jsonl` | received (adapter stdout) | 59 | `probe-write.js` |
| `write.sent.jsonl` | sent (probe stdin to adapter) | 4 | `probe-write.js` |

Every line in every file parses as a standalone JSON object (verified with `JSON.parse` per
line, zero failures). Received frames are the raw line exactly as read from the child process's
stdout, before parsing — key order and whitespace are whatever the adapter emitted, not a
re-serialization by the probe. Sent frames are the exact string the probe wrote to the child's
stdin (`JSON.stringify(o) + "\n"`), captured at the point of sending.

## Redaction

Two literal-substring replacements were applied to all four files, chosen because they were the
only sensitive values found in the captured text (see "swept for" below):

> **The values themselves are deliberately not reproduced here.** An earlier revision of this file
> named the exact email and home-directory path it had redacted, which put both back into the
> committed repository and defeated the redaction it was documenting. The rule is recorded; the
> value is not. *Naming what you removed is how a redaction undoes itself.*

1. **The operator's account email** → `<REDACTED_ACCOUNT>` (one literal substring). It appears
   inside `_auth/status_update` frames as `account.email` and inside the derived
   `account.organization` string (`"<email>'s Organization"`); the substring replace fixed both
   structurally correctly.
2. **The operator's home-directory prefix**, in both path spellings the adapter used — the
   backslash form (backslash-escaped, inside JSON string values such as `rawInput.file_path` and
   `content[].path`) → `C:\<REDACTED_HOME>`, and the forward-slash form (inside the `session/new`
   `cwd` param) → `C:/<REDACTED_HOME>`.

To re-verify redaction on a fresh capture without naming either value, grep the frames for `@` in
`account.*` fields and for `Users` in any path field; both should return nothing.

No plan/label/id under `account` needed redaction beyond the email: the captured `account`
object was `{"plan":"max"|"Claude Max","email":"...","organization":"...'s Organization"}` —
`plan` is a subscription tier name, not an identifier, and was left as-is; `label` (`"Claude
Max"`) is likewise a tier name, not an identifier.

**Swept for and not found:** any field or substring matching `key`, `token`, `secret`,
`password`, or `credential` (case-insensitive), and any other value that looked like a token,
API key, or session secret. `sessionId` values (UUIDs, one per probe run) were left in place —
they are ephemeral, single-run identifiers with no standing access, not credentials.

Redaction was applied as a raw literal-string substitution over the captured text (not a
JSON-parse-then-re-serialize pass), so key order, nesting, and array shapes are exactly as
captured — only the two substring patterns above changed. All four files were re-verified after
redaction: every line still parses as standalone JSON, and a full-text search for `malla`
returns zero hits in every file.

## Observed vs the README's claimed frame-kind list

The spike README (`spikes/acp-subscription-lane/README.md`) claims these `session/update`
kinds and non-schema-v1 traffic appeared: `user_message_chunk`, `agent_message_chunk`,
`agent_thought_chunk`, `tool_call`, `tool_call_update`, `usage_update`, `_auth/status_update`,
`_session/goal`, `_meta.jetbrains.air`.

Actually observed in this capture:

- `session/update` discriminators seen: `available_commands_update`, `usage_update`,
  `tool_call`, `tool_call_update`, `agent_message_chunk`. **Not observed:**
  `user_message_chunk`, `agent_thought_chunk` — neither probe's prompt triggered them in this
  run.
- Top-level methods seen: `_auth/status_update`, `session/update`,
  `session/request_permission` (write probe only).
- `usage_update` is delivered as a `session/update` frame (`params.update.sessionUpdate ==
  "usage_update"`), not as a separate top-level method — worth noting since the README's phrase
  "non-schema-v1 traffic" could be read as implying a distinct wire method; it is not.
- `_session/goal` and `jetbrains.air` were observed, but only as **declared capabilities**
  inside the `initialize` response's `_meta` block (`_meta.goal.controlMethod == "_session/goal"`
  and `_meta.jetbrains.air.capabilities == [...]`) — this run never exercised `_session/goal` as
  a live inbound/outbound call, and no `_meta.jetbrains.air`-namespaced session/update frame
  appeared during the prompt turn itself.

This corpus is a test oracle for a wire contract — the point being that assertions about frame
shape are proven against what the adapter actually sent, not against events the wire-contract's
own author wrote.
