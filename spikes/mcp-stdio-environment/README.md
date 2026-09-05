# Spike — what environment does a stdio MCP server actually get?

**Question.** `design-mcp-enlightened-path` rests its whole identity model on a stdio MCP server
inheriting the launching client's environment, so each agent's server picks up that agent's
`AIDE_SESSION` with no configuration. That was **Inferred** and load-bearing: if it were false, the
fallback is an `env` block in `.mcp.json`, which is per-**workspace** — meaning one shared identity
for every agent in the workspace, and board posts attributed to the wrong session.

**Answer: inheritance holds.** Run 2026-09-04, Claude Code on Windows.

## Method

The probe is deliberately **not a working MCP server**. What is being measured is what the launcher
hands the child at startup, and that is settled before the handshake — so failing the handshake is
the cheapest possible probe and costs the launcher one logged error.

`claude mcp list` turned out to be a sufficient harness: it launches every configured stdio server to
report its status, so no nested session was needed.

```
claude mcp add envprobe --scope local -- python probe.py
export AIDE_SESSION="agent:claude#spike01"
export MCP_PROBE_OUT=.../probe-result.json
claude mcp list
claude mcp remove envprobe --scope local
```

## Observed

```json
{
  "cwd": "...\\scratchpad\\spike-mcp",
  "saw_AIDE_SESSION": "agent:claude#spike01",
  "saw_AIDE_CONTRACT_LOG": "C:/fake/coord",
  "saw_SPIKE_MARKER": "inheritance-test",
  "env_count": 79,
  "saw_PATH": true,
  "saw_USERPROFILE": true
}
```

## What this settles

1. **The child inherits the parent's environment in full.** Not a curated allowlist — 79 variables,
   including three invented for this probe. `AIDE_SESSION` therefore reaches the server without
   `.mcp.json` carrying it, and each agent's server gets *that agent's* session.

2. **`cwd` is the invocation directory**, not a fixed workspace root. Since `c235611` an agent
   terminal runs in its own git worktree, so cwd is a **second, independent identity signal** — and
   the design gains a cross-check it did not have: a session whose stored `worktree_path` disagrees
   with the server's cwd is a mis-attribution worth refusing rather than serving.

3. **The `.mcp.json` `env` fallback still exists** (`claude mcp add -e KEY=value`), so a future
   harness that sanitises the environment is recoverable — it just costs per-session identity.

## What it does not settle

Measured on **Claude Code, Windows, 2026-09-04** only. Another harness may sanitise; the design's
degradation is named rather than assumed, and finding #2 gives it a check that does not depend on the
environment at all.
