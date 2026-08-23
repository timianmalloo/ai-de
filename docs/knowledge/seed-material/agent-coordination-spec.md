---
id: seed-agent-coordination-spec
title: "Seed — Agent Coordination Layer Specification v0.1"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [seed-material, multi-agent, coordination, event-log]
links:
  - { to: knowledge-hub, rel: relates-to }
  - { to: kb-multi-agent-coordination, rel: relates-to }
review-by: 2026-11-21
review-suggested:
  - { by: knowledge-hub, on: 2026-08-23, reason: "New domain knowledge base established; four findings change prior architecture assumptions (Kuzu archived, MCP stateless, lease fencing gap, thesis framing)" }
summary: >-
  The originating specification for the agent coordination layer developed in a separate
  worktree: claims-not-commits, an append-only per-session JSONL log folded into a SQLite
  read model of leases, work items and decisions, surfaced via agentctl, MCP and hooks.
---
# Agent Coordination Layer — Specification v0.1

**Purpose:** Let multiple coding agents (Claude Code, GitHub Copilot CLI/agent, and any harness driving Fable, Opus, GPT-5.6, etc.) work the same repository in parallel with minimal merge conflicts and shared understanding of goals, tasks in flight, artifacts being touched, and decisions made.

**Thesis:** *Claims, not commits, are the unit of coordination.* Every agent appends **intent** to a shared, append-only log before it touches the working tree. All shared state (leases, backlog status, knowledge graph) is a fold over that log. Git remains the integration mechanism; it is never the coordination mechanism.

**Companion:** `agent-coordination-explorer.jsx` (interactive decision explorer). Section numbers below map to its tabs.

---

## 1. Goals and non-goals

### Goals
- **G1** Surface overlap *before* edits happen (claims/leases on paths or symbols).
- **G2** Maximize parallelism: agents only serialize on genuinely shared artifacts.
- **G3** Model/tool agnostic: one protocol reachable via CLI, MCP, and plain files.
- **G4** Shared knowledge graph of Goals → WorkItems → Artifacts → Decisions, scoped to what is in flight and orthogonal to working trees.
- **G5** Full auditability and replay: the log is the source of truth.
- **G6** Low operational overhead: repo-local, no cloud dependency in v1.

### Non-goals (v1)
- Multi-machine / cloud-hosted agents (design for it; do not build it).
- Replacing GitHub issues/Projects as the system of record for the backlog (we mirror and link, not replace).
- Semantic merge resolution.

---

## 2. Architecture

```
┌──────────────────────────────────────────────────────────────┐
│  Agents: Claude Code · Copilot CLI · GPT harness · ...       │
│      │ hooks / wrapper          │ MCP tools      │ CLI       │
│      ▼                          ▼                ▼           │
│  ┌───────────────────── agentctl (dotnet tool) ───────────┐  │
│  │  claim · release · heartbeat · announce · decide ·     │  │
│  │  block · done · check · status · tail · project        │  │
│  └────────────┬───────────────────────────────┬───────────┘  │
│               ▼ append                        ▼ query        │
│   .agents/log/<agent>-<session>.jsonl   .agents/state.db     │
│   (git-tracked, append-only)            (git-ignored, fold)  │
│               └──────────── fold ─────────────┘              │
│                                 │                            │
│                                 ▼                            │
│   .agents/graph/*.md  ·  AGENTS.md / CLAUDE.md projections   │
└──────────────────────────────────────────────────────────────┘
   Working trees: git worktree per agent session (../wt-<agent>)
```

**Layers**
1. **Log** — append-only JSONL, one file per agent *session*. Git-tracked so it survives clones and is reviewable; per-session files mean no two writers ever touch the same file.
2. **Fold / read model** — SQLite (`.agents/state.db`, git-ignored) rebuilt from the log on demand. Holds leases, work item status, graph edges.
3. **Surfaces** — `agentctl` CLI (primary), MCP server (same verbs), enforcement hooks, generated markdown projections.
4. **Working trees** — one `git worktree` per agent session; branch naming `agent/<agent>/<work-item>`.

---

## 3. Repository layout

```
.agents/
  AGENTS.md                 # rules every model reads (≤ 2k tokens)
  protocol.md               # this spec's §4 in agent-facing form
  log/
    fable-20260820T1412.jsonl
    opus-20260820T1415.jsonl
    copilot-20260820T1420.jsonl
  graph/                    # generated projection of the knowledge graph
    goals/G-001.md
    work-items/WI-142.md
    decisions/D-017.md
  hotspots.yml              # paths owned by the integrator agent
  state.db                  # git-ignored fold
  projections/              # git-ignored per-agent context (regenerated)
    fable.md
CLAUDE.md                   # → includes .agents/AGENTS.md
.github/copilot-instructions.md  # → includes .agents/AGENTS.md
tools/agentctl/             # dotnet tool source
tools/agentctl.mcp/         # MCP server (thin wrapper over same core)
```

---

## 4. Protocol

### 4.1 Verbs (events)
Seven event types. All share an envelope.

| Verb | When | Key fields |
|---|---|---|
| `announce` | Before claiming; states intent | `intent`, `likelyPaths[]` |
| `claim` | Before first edit to a path/symbol | `path`, `scope`, `ttl` |
| `heartbeat` | Every 2 min while holding leases | — |
| `release` | When done with a path | `path` |
| `decide` | Any convention/design choice others must honour | `topic`, `choice`, `rationale`, `artifacts[]` |
| `block` | Cannot proceed | `onWorkItem`, `needs` |
| `done` | Work item complete | `pullRequest`, `files[]` |

### 4.2 Envelope and records (C#)

```csharp
namespace AgentCoord.Protocol;

public enum ClaimScope { Dir, File, Symbol }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Announce), "announce")]
[JsonDerivedType(typeof(Claim), "claim")]
[JsonDerivedType(typeof(Heartbeat), "heartbeat")]
[JsonDerivedType(typeof(Release), "release")]
[JsonDerivedType(typeof(Decide), "decide")]
[JsonDerivedType(typeof(Block), "block")]
[JsonDerivedType(typeof(Done), "done")]
public abstract record AgentEvent(
    string Id,                 // ULID
    string Agent,              // logical name: fable, opus, copilot, gpt-5.6
    string Model,              // provider model string
    string Session,            // agent-<timestamp>
    string WorkItem,           // WI-### (required; use WI-000 for housekeeping)
    DateTimeOffset At);

public sealed record Announce(string Id, string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At,
    string Intent, string[] LikelyPaths) : AgentEvent(Id, Agent, Model, Session, WorkItem, At);

public sealed record Claim(string Id, string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At,
    string Path, ClaimScope Scope, TimeSpan Ttl) : AgentEvent(Id, Agent, Model, Session, WorkItem, At);

public sealed record Heartbeat(string Id, string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At) : AgentEvent(Id, Agent, Model, Session, WorkItem, At);

public sealed record Release(string Id, string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At,
    string Path) : AgentEvent(Id, Agent, Model, Session, WorkItem, At);

public sealed record Decide(string Id, string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At,
    string Topic, string Choice, string Rationale, string[] Artifacts) : AgentEvent(Id, Agent, Model, Session, WorkItem, At);

public sealed record Block(string Id, string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At,
    string OnWorkItem, string Needs) : AgentEvent(Id, Agent, Model, Session, WorkItem, At);

public sealed record Done(string Id, string Agent, string Model, string Session,
    string WorkItem, DateTimeOffset At,
    string PullRequest, string[] Files) : AgentEvent(Id, Agent, Model, Session, WorkItem, At);
```

Wire format: one JSON object per line, `System.Text.Json`, camelCase, `kind` discriminator first.

```json
{"kind":"claim","id":"01J5...","agent":"fable","model":"claude-fable-5","session":"fable-20260820T1412","workItem":"WI-142","at":"2026-08-20T14:13:02Z","path":"src/HealthHub.Api/Whoop/**/*.cs","scope":"Dir","ttl":"00:05:00"}
```

### 4.3 Read model (fold)

```csharp
public sealed record Lease(string Path, ClaimScope Scope, string Agent, string Session,
    string WorkItem, DateTimeOffset Expires);

public sealed record WorkItemState(string Id, string Goal, string Status,   // Open|Claimed|Blocked|Done
    string? Agent, string[] Touches, string[] BlockedOn);
```

Fold rules:
- `claim` adds a lease unless an **unexpired** overlapping lease exists for a different session → reject (`EBUSY`).
- `heartbeat` extends all leases for the session by their TTL.
- `release`, `done`, or expiry removes leases. Expiry is materialized as a synthetic `release` with `reason: expired` in the fold (not in the log).
- Overlap = glob intersection for `Dir`/`File`; exact match for `Symbol` plus `File`-level check on the containing file.

### 4.4 Invariants (enforced by `agentctl`, stated in AGENTS.md)
1. `announce` before `claim`; `claim` before edit; `release` or `done` before PR.
2. Claims are globs over the working tree, never over `.agents/log/`.
3. Heartbeat every 2 min; default TTL 5 min; a session with no heartbeat for TTL loses all leases.
4. Paths in `hotspots.yml` are claimable only by the agent named `integrator`.
5. Before starting a work item, read `agentctl decisions --wi <id>` and honour them; to override, emit a new `decide` that supersedes.
6. Read your projection (`agentctl project <agent>`), not the whole graph.

---

## 5. Surfaces

### 5.1 `agentctl` (dotnet global tool, `tools/agentctl`)
```
agentctl announce  --wi WI-142 --intent "..." --paths a,b
agentctl claim     --wi WI-142 --path "src/X/**/*.cs" [--scope Dir|File|Symbol] [--ttl 5m]
agentctl check     <path>                      # exit 0 free/mine, 3 leased by other (prints holder)
agentctl heartbeat
agentctl release   --path ...
agentctl decide    --wi WI-142 --topic ... --choice ... --why ... [--artifacts ...]
agentctl block     --wi WI-140 --on WI-142 --needs "SleepSummaryDto"
agentctl done      --wi WI-142 --pr 88
agentctl status    [--wi|--agent|--path]       # folded view
agentctl tail      [--follow]                  # merged chronological log (the "shared console")
agentctl project   <agent> > .agents/projections/<agent>.md
agentctl fold      --rebuild                   # regenerate state.db and .agents/graph/
agentctl worktree  new --agent fable --wi WI-142   # creates ../wt-fable-WI-142 on agent/fable/WI-142
```
Identity comes from env: `AGENT_NAME`, `AGENT_MODEL`, `AGENT_SESSION` (auto-generated if absent, persisted to `.agents/.session` in the worktree).

### 5.2 MCP server (`tools/agentctl.mcp`)
Same core assembly; tools: `coord_announce`, `coord_claim`, `coord_check`, `coord_release`, `coord_decide`, `coord_block`, `coord_done`, `coord_status`, `coord_decisions`, `coord_project`. stdio transport. Registered in `.mcp.json` (Claude Code) and Copilot's MCP config. Heartbeat runs as a background timer inside the server process while any lease is held.

### 5.3 Enforcement hooks
- **Claude Code:** `PreToolUse` hook on `Edit|Write|MultiEdit` runs `agentctl check <file>`; non-zero exit blocks the edit and returns the holder + work item to the model.
- **Copilot CLI / coding agent:** wrapper script sets `AGENT_*` env and pre-commit hook runs `agentctl check` on staged files. (Edit-time enforcement depends on Copilot's hook surface at implementation time — verify and document.)
- **Any harness:** pre-commit hook is the universal floor.

### 5.4 Projections
`agentctl project <agent>` renders ≤ 2k tokens: the agent's open work items, their goals, current leases (own and neighbours), open decisions on artifacts it touches, and anything blocked on it. `AGENTS.md` is static rules; projections are dynamic context. Both are included by `CLAUDE.md` and `copilot-instructions.md`.

---

## 6. Knowledge graph

**Nodes:** `Goal`, `WorkItem`, `Artifact` (path or symbol), `Decision`, `Agent`, `Session`.
**Edges:** `decomposes` (Goal→WI), `touches` (WI→Artifact), `dependsOn` (WI→WI, derived from `block`), `decidedBy` (Decision→Session), `appliesTo` (Decision→Artifact), `executedBy` (WI→Session).

Derivation: every edge is computed from events — no separate graph write path. `.agents/graph/*.md` is a generated, git-tracked projection (markdown + YAML front-matter) so any model can read it without tools; SQLite is the query surface.

Scoping: graph contains only nodes reachable from an **open** Goal. Closing a goal archives its subgraph to `.agents/graph/archive/`.

Backlog: `WorkItem` nodes are created by `agentctl wi new --goal G-001 --title ...` or imported from GitHub Issues (`agentctl wi import --label agent-ready`). Status is derived: Open → Claimed (first claim) → Blocked → Done.

---

## 7. Claim granularity and Roslyn

- Default scope: `File`. Use `Dir` for greenfield modules, `Symbol` for edits inside large shared files.
- `Symbol` claims: `--path src/Foo.cs --symbol HealthHub.Api.SleepService.Summarize`. `agentctl check` for a symbol claim verifies via a Roslyn workspace that the edit diff only touches the claimed member (v2; v1 treats Symbol as File with advisory metadata).
- Existing LOA analyzers can emit an analyzer warning when a file is edited without a matching lease (reads `state.db`).

---

## 8. Conflict strategy

- **Leases are hard** (reject, not warn) when enforcement is wired; advisory otherwise. Default to hard.
- **Hotspots** (`Program.cs`, DI registration, `*.csproj`, EF migrations, `Directory.Packages.props`) are owned by the `integrator` agent. Other agents emit `block --needs "register IFoo in DI"`; the integrator batches these.
- **Planner pass** (optional, later): an agent reads the graph and assigns open work items to minimize shared artifacts.
- **Merge queue:** PRs merge in `done` order; rebase conflicts are routed to the integrator, never auto-resolved silently.

---

## 9. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Coordination files become the conflict | One append-only file per session; nothing ever edits a log line. |
| Agents forget to claim | Edge enforcement (hook / pre-commit); `AGENTS.md` rule §1. |
| Stale leases | TTL + heartbeat; expiry visible in `status` and `tail`. |
| Shared hotspots | `hotspots.yml` + integrator ownership; source generators to eliminate hand-edited registration. |
| Convention drift across models | Decisions are nodes; `agentctl decisions --wi` is mandatory pre-read. |
| Context bloat | Projections capped at 2k tokens; graph scoped to open goals. |
| Goal drift | Closing a goal archives its subgraph; orphan WIs flagged by `fold`. |

---

## 10. Roadmap

| Phase | Deliverable | Exit criterion |
|---|---|---|
| 1 — Log | Protocol records, JSONL writer, `agentctl` announce/claim/release/heartbeat/status/tail, `AGENTS.md`, worktree script | Two Claude Code sessions run concurrently and see each other's leases in `tail` |
| 2 — Enforcement | PreToolUse hook, pre-commit hook, Copilot wrapper, TTL expiry | An unclaimed edit is refused with holder info in both tools |
| 3 — Graph + MCP | SQLite fold, graph projections, MCP server, `project` | A GPT-driven harness completes a work item using only MCP tools |
| 4 — Planner + integrator | Hotspot ownership, integrator agent, planner assignment, metrics | Conflicts/PR and agent idle time measured over one week |

**Metrics:** merge conflicts per PR · % edits on claimed paths · mean time an agent waits on a lease · decisions re-litigated per week.

---

## 11. Implementation notes for Claude Code

- .NET 10, `System.CommandLine`, `Microsoft.Data.Sqlite`, `System.Text.Json` polymorphism, ULIDs via `Cysharp.Ulid`.
- Core assembly `AgentCoord.Core` (protocol, log, fold, glob overlap). `agentctl` and `agentctl.mcp` are thin hosts over it.
- Glob overlap: use `Microsoft.Extensions.FileSystemGlobbing`; overlap test = either pattern matches a sample path set generated from the other, plus literal-prefix comparison. Document false-positive policy (prefer false positive).
- Tests: property tests on fold idempotence (replaying the log twice yields identical state); concurrency test with two writers and simulated clock for TTL.
- First gut-check: build `AgentCoord.Core` + `agentctl claim/check/tail` only, run two terminals, confirm `EBUSY` on overlap. Everything else waits on that.
