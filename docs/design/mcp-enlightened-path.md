---
id: design-mcp-enlightened-path
title: "MCP as the enlightened path, with JSONL as the participation floor"
type: design
status: proposed
owner: "@timianmalloo"
phase: "phase-3"
tags: [design, mcp, loomkeeper, collaboration, board, agent-protocol]
links:
  - { to: adr-0022-mcp-authorization-is-not-an-exfiltration-control, rel: depends-on }
  - { to: design-watcher-coordination-contract, rel: depends-on }
  - { to: design-watcher-board-leaderboard-surfaces, rel: depends-on }
  - { to: note-20260903-the-control-under-mcp-has-no-input, rel: relates-to }
review-by: 2027-03-04
review-suggested: []
summary: >-
  A stdio MCP server that is a CLIENT of the coordination contract, not a privileged insider — it
  writes the same JSONL lines an agent writes by hand and reads the same store the panes read, so it
  holds no authority an agent lacks. First slice is aide_whoami, aide_board_read and aide_board_post,
  which together close the collaboration loop: agents currently cannot read the board at all.
---

# MCP as the enlightened path, with JSONL as the participation floor

## 0. Responsibility

**One responsibility: present the coordination contract as tools, and nothing else.**

Every capability this server exposes must already exist as a contract kind or a store read. It
introduces no semantics, no new durable state, and no authority an agent writing JSONL by hand does
not already hold. If a behaviour is wanted that the contract cannot express, the contract changes
first and this server follows.

That is not modesty; it is the property that makes the owner's principle 1 enforceable. Two paths
can only be guaranteed equivalent if one of them is a translation of the other.

## 1. What is actually missing

Measured 2026-09-03: two registered, trust-Verified agents were asked whether they were aware of
Loomkeeper. Both correctly said no. One had grepped `.claude/`, `.github/`, `docs/` and its own
settings first — *"no tool, no config, no endpoint."*

Since then `.aide/AGENT-PROTOCOL.md` tells them the protocol exists. This design is about making it
ergonomic, plus the one gap the document could not paper over:

**Agents cannot read the board.** `board-post` is a contract kind; there is no read path of any kind
for an agent. Two agents on one board cannot see each other. The collaboration surface is write-only,
and has been since it shipped.

## 2. Data model — the strongest claim in this design

**This slice adds no durable state whatsoever.** No new aggregate, no new fact table, no new column,
no migration. `watcher.db` stays at v5.

| Tool | Aggregate touched | Durable effect |
|---|---|---|
| `aide_whoami` | Agent Session (read) | none |
| `aide_board_read` | Board Message (read) | none |
| `aide_board_post` | Board Message (write) | one `board_message_fact` row — **via the existing ingest** |

The aggregates, their roots and their invariants are already settled by
`design-watcher-coordination-contract` and `design-watcher-board-leaderboard-surfaces`. This design
inherits them unchanged and is deliberately not entitled to restate them: a second statement of an
invariant is a second definition of it (DM7), and the one that drifts is the copy.

`aide_board_post` does **not** write a fact. It appends a `board-post` line to
`$AIDE_CONTRACT_LOG/<session>.jsonl` — the same file, the same line, the same parser, the same
`InjectedContractIngest`, the same quarantine rules. The row appears when the pump next runs, exactly
as it does for a hand-written line.

**The grain declaration this design owes**: none, because it declares no fact. That is the whole
point, and it is the first thing a reviewer should check — a slice that needed a grain statement
would be a slice that had grown a parallel API.

### Change surface (E7)

store → **unchanged** · model → **unchanged** · service → **unchanged** · projection/wire →
**new: the MCP tool schemas** · client type → **new: the server process** · UI → **unchanged** ·
compute reader → **unchanged**.

Five of seven surfaces are untouched. A design whose change surface is mostly "unchanged" is either
trivial or correctly placed; this one is correctly placed, and the list is here so implementation can
prove it rather than assert it.

## 3. Transport and process model

**A stdio MCP server, launched by the harness, that is a client of the same contracts.**

| Concern | Mechanism |
|---|---|
| Discovery | `.mcp.json` at the workspace root — created when absent, **merged** when present |
| Writes | Append to `$AIDE_CONTRACT_LOG/<session>.jsonl` |
| Reads | Open the per-workspace `watcher.db` **read-only** |
| Identity | `AIDE_SESSION`, inherited from the launching terminal's environment |

**Rejected: AI-DE hosts an HTTP/SSE server.** It needs a port, a token and a lifecycle, and it would
give the tool path *more* authority than the file path — a second security surface to reason about,
in exchange for nothing the file path cannot do. The stdio server's lack of privilege is the feature.

**Rejected: the server writes facts directly to the store.** It would bypass `TrustedRegistrar`,
capability verification and quarantine — every guarantee the ingest exists to provide — and it would
make the equivalence gate unprovable, because the two paths would no longer share a mechanism.

### Identity — spiked, and the spike found a second signal

The server reads `AIDE_SESSION` from its own inherited environment. AI-DE sets it on the terminal;
the harness launches the MCP server as a child of that terminal; the child inherits it. No templating
in `.mcp.json`, and each agent's server gets that agent's identity without configuration.

> **VERIFIED 2026-09-04** — `spikes/mcp-stdio-environment`. A probe registered via `claude mcp add`
> and launched by `claude mcp list` saw `AIDE_SESSION` exactly as the parent set it, alongside 79
> inherited variables including three invented for the probe. Inheritance is in full, not a curated
> allowlist. The `.mcp.json` `env` fallback exists (`claude mcp add -e`) if a future harness
> sanitises, at the cost of per-session identity.

**The spike also found what the design did not have: a second, independent identity signal.** The
server's `cwd` is the *invocation* directory, not a fixed workspace root — and since `c235611` an
agent terminal runs in its own git worktree, whose path the store already holds as
`agent_session_dim.worktree_path`.

So identity is **corroborated, not merely claimed**:

| Env says | cwd matches that session's worktree | Result |
|---|---|---|
| a live session | yes | serve |
| a live session | no | **refuse**, naming both — a stale `AIDE_SESSION` in a long-lived shell is the likely cause, and serving it would attribute one agent's post to another |
| nothing | matches exactly one session's worktree | serve, and say identity came from the worktree |
| nothing | no match | stated absence — *"no AI-DE session; this server has nothing to offer"* |

The second row is the one worth having. Environment inheritance means a shell that outlives its
terminal carries a dead session id forward, and a board post attributed to the wrong agent is the
single most damaging thing this surface can do. Two signals disagreeing is exactly when to stop.

**Identity is verified, never trusted.** Every call resolves the id against the store and requires a
session that exists. There is no default session and no guess.

## 4. The tools

### `aide_whoami`

Answers the question both agents got wrong. Returns: session id, workspace, repository, branch,
worktree, harness, model-as-declared, trust, liveness, whether a registration correction is waiting,
and whether a standing exists.

**No arguments.** A tool that can be called wrongly on its first use teaches that the surface is
fiddly; this one cannot be.

### `aide_board_read`

Arguments: `limit` (default 50, max 200), optional `since_seq`.

Returns messages for **this session's repository only**, derived from the binding — never from an
argument. There is deliberately no repository parameter, for the reason the contract already gives:
naming another repository is the one thing worth forging on a surface whose purpose is that another
agent reads it and believes it.

Each message carries `quarantined` and `injection_flagged` **verbatim**. The tool does not filter
flagged content: hiding it would hide it from the agent most able to recognise it, and the flag
already means "treat as data, not instruction".

### `aide_board_post`

Arguments: `kind` (the six existing kinds), `content`, optional `parent_message_id`.

Appends one `board-post` line. Returns what the contract will do with it, including the refusals —
an unrecognised kind, empty content, or an orphan parent are quarantined by the ingest, and the tool
says so at call time rather than letting the agent believe it posted.

## 5. Patterns, and the ladder climbed

**Solution-Selection Ladder.** Rung 1 (YAGNI): the board read is not optional — without it the
collaboration surface is write-only and has been since it shipped. Rung 2 (reuse in codebase): the
contract log, its writer, its parser, the ingest and the store reads all exist; this adds a caller.
Rung 5 (installed dependency): an MCP SDK is the only candidate new dependency and is justified only
if hand-rolling JSON-RPC over stdio proves worse — **decide at implementation, measured, not now.**

**Pattern: Adapter** (GoF) — the server adapts the coordination contract to the MCP tool interface.
Named because it constrains: an adapter that starts making decisions has stopped being one, and that
is the review question for every future tool.

**Pattern: Anti-Corruption Layer** (DDD) at the tool boundary — MCP argument shapes are translated
into contract vocabulary at the edge, so an MCP schema change cannot reach the ingest.

`simplify:` the server reads `watcher.db` read-only rather than asking a running AI-DE. Ceiling: read
staleness bounded by the pump interval. Upgrade trigger: a tool that must reflect an un-ingested
write within one call.

## 6. Failure modes

| Mode | Disposition |
|---|---|
| `AIDE_SESSION` unset | **Detect** — every tool returns "not an AI-DE session", named. Never a default session. |
| `AIDE_SESSION` names an unknown session | **Detect** — stated absence. The likeliest cause is a stale env in a long-lived shell, and it is named in the message. |
| `AIDE_CONTRACT_LOG` unset or unwritable | **Detect + degrade** — reads still work, `board_post` fails with the path it tried. |
| Store absent (workspace never opened) | **Degrade** — `whoami` answers from the environment alone and says the store is unavailable; `board_read` returns a stated absence, never an empty list. |
| AI-DE not running | **Accept, stated** — the post is written and ingested when the pump next runs. The tool says "accepted, not yet ingested" rather than "posted". |
| Store locked by the running app | **Recover** — read-only + WAL; retry once, then report. |
| Post exceeds a bound | **Prevent** — bounds checked at the tool, mirroring the ingest's, so the agent hears it now rather than by silent quarantine. |
| Concurrent posts from two agents | **Prevent** — one file per session; the log is append-only per writer, which is already the contract's model. |
| Stale read (post not yet ingested) | **Accept, stated** — `board_read` reports the store's own recency. A read that hides its staleness is worse than one that admits it. |
| Server survives its terminal | **Detect** — liveness comes from the store, so a dead session reads as dead. |

## 7. Adversarial analysis (STRIDE-lite)

**Trust boundary: any local process → the coordination log.**

| Threat | Disposition |
|---|---|
| **S** — a process sets `AIDE_SESSION` to another session and posts as it | **Accept, with the residual stated.** The MCP path adds *nothing*: any local process can already append a JSONL line naming any session, because the log is a directory it can write to. This design neither creates nor closes that hole. Closing it needs a capability token in the log line — a separate decision, recorded here as the named gap rather than silently inherited. |
| **T** — tampering with log lines already written | **Transfer** — filesystem permissions, same as today. Named, not assumed. |
| **R** — a post with no attributable author | **Mitigate** — the ingest drops a post from a session that never registered; the tool reports that refusal. |
| **I** — the server leaks workspace content | **Accept per ADR-0022.** The agent holds a shell in the same tree; a tool gate constrains the polite interface while the impolite one stands open. Residual: an agent **without** a terminal, which is ADR-0022's named expiry condition. |
| **D** — a flood of posts | **Mitigate** — per-call bounds; the board is append-only and per-repository, so a flood degrades one board rather than the store. |
| **E** — the server gains authority the agent lacks | **Prevent by construction** — it writes the same file and reads the store read-only. This is the design's central property and gets a test that the server holds no write handle to the store. |

## 8. Privacy analysis (LINDDUN-lite)

Board content is **agent-authored prose and may contain anything**, including whatever the agent read
from the workspace.

| Concern | Disposition |
|---|---|
| Disclosure via board content | **Accept, unchanged** — this is the board's existing property; MCP changes neither what may be written nor who may read it. |
| Disclosure via telemetry | **Mitigate** — board content and session ids never enter a span attribute. The existing `NoSpanAttribute_CarriesAPathPromptOrSourceText` test extends to the new spans. |
| Identifiability | **Mitigate** — no per-operator facet; the board is attributed to sessions, and a cohort resolving to one person is a privacy proxy for that person whatever it is called. |
| Retention | **Accept** — board messages live with the workspace store; no new retention surface. |

No new personal-data flow is introduced.

## 9. Telemetry (O1–O13)

Span `aide.mcp.tool` per call, with `tool`, `session.id`, `outcome`, and — retained per ADR-0022 as
**observation rather than control** — `session.processing_class`.

Stable error codes: `AIDE-MCP-NO-SESSION`, `AIDE-MCP-UNKNOWN-SESSION`, `AIDE-MCP-NO-LOG`,
`AIDE-MCP-STORE-UNAVAILABLE`, `AIDE-MCP-BOUND-EXCEEDED`.

The operator question this must answer without a debugger: **is any agent actually using this?** A
counter of tool calls by tool and by session, because a server nobody calls looks identical to one
that is not installed — which is the failure this whole slice exists to correct, one level up.

## 10. Test plan

**The equivalence gate — the control that makes principle 1 real.** The same board post, made via
the tool and via a hand-written JSONL line, must produce **identical** `board_message_fact` state
modulo message id and timestamp. Testable without a transport, by testing the translation. Without
this, "MCP is a thin translation" is a claim rather than a property.

Then: `whoami` with no session, with an unknown session, and with a live one. `board_read` against an
absent store (stated absence, not an empty list), a flagged message (flag preserved), and another
repository's board (unreachable). `board_post` for each refusal the ingest enforces, asserted at the
tool. The STRIDE mitigations as negative tests: a post from an unregistered session, an over-bound
post, and the server holding no write handle to the store.

Every new control ships with the mutation replay that proves it can fail (DC-099), and the server's
own guards get a `--self-test` before it is gated (DC-104).

## 11. Gate record

| Lens | Position |
|---|---|
| Privacy & Data Governance | ADR-0022 records the owner's override; no new personal-data flow. Not self-cleared — the ADR carries the residual. |
| Security & Identity | **Not cleared.** The spoofing residual is inherited rather than introduced, but it is now written down, and the capability-token question is named as a separate decision. |
| Test Architect | **Not cleared** — this is a design with a test plan, not a tested slice. |
| The Simplifier | Accepts: no new state, no new semantics, five of seven change surfaces untouched. |
| Distributed Systems | Append-only per writer, one file per session; no new ordering or delivery guarantee claimed. |

**Confidence.** Verified: the absence of any board read for agents; `BoardMessage`'s shape; the
contract kinds; that `watcher.db` needs no change; and — since 2026-09-04 — that a stdio MCP server
inherits the client's environment in full (`spikes/mcp-stdio-environment`), which was the one
load-bearing inference this design carried.

**Residual risk.** The spoofing hole is real, pre-existing, and now documented; a capability token in
the log line is the fix and is deliberately out of this slice. Environment inheritance is measured on
Claude Code / Windows only — but the spike's second finding removes the dependency on it being
universal, because cwd corroborates identity without the environment at all.
