---
id: kb-coord-comparables
title: "Multi-Agent Coordination — comparable systems"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, multi-agent, merge-queue, frameworks]
links:
  - { to: kb-multi-agent-coordination, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  How every surveyed multi-agent coding system avoids conflict — by isolation, by turn-taking,
  by hierarchy, or by refusing parallelism — and the gap that makes the claims-log approach novel.
---

# Comparable solutions & problem framings

## Multi-agent coding systems

| System | Coordination mechanism | Conflict strategy | Does well | Does badly | Confidence |
|---|---|---|---|---|---|
| **Anthropic research system** | Orchestrator → parallel subagents with explicit task boundaries, output formats and tool guidance | **Separate context windows**; no shared working tree at all | Breadth-first research; work exceeding one context window; +90.2% on their eval | Inter-dependent subtasks — coding named explicitly; **15×** tokens | Verified [S1] |
| **Anthropic managed agents** | Session (append-only log) + harness + sandbox, decoupled | n/a — single agent lineage | Crash recovery; replaceable components | Not a coordination layer between agents | Verified [S3] |
| **Claude Code worktrees** | `git worktree` per session; subagents used for **questions, not code** | File-level isolation by branch | Independent features and bugfixes in parallel | Parallel code *writing* — excluded by design | Verified [S12] |
| **Cognition / Devin** | Single-threaded linear agent + context compression | none needed — no parallel writes | Long-running coherent tasks; production reliability | Genuinely parallel workstreams | Verified [S4] |
| **OpenAI Codex cloud** | Per-task isolated sandboxes; PRs for review | Isolation by sandbox assignment | Batch/async multi-task | Tight inter-task coordination | Inferred |
| **GitHub Copilot coding agent** | Developer-supervised, inline, issue-integrated | Standard PR review and merge | Supervised multi-file edits | Autonomous parallel execution | Verified |
| **SWE-agent** | Single-agent minimal ACI | n/a | Reproducible baseline — **72.0%** SWE-bench Verified | No coordination at all | Verified [S14] |
| **OpenHands** | Event-driven, Docker-sandboxed, extensible | Docker isolation per run | Most actively developed open framework — **77.6%** | Complex inter-agent state sharing | Verified [S15] |
| **AutoGen** | Turn-based group chat | Conversational consensus | Exploratory dialogue | Simultaneous writes | Inferred [S16] |
| **MetaGPT / ChatDev** | Scripted role workflow (PM, architect, coder, reviewer) | **Hierarchy** — the manager decides | Structured projects; measurable via MAST interventions | Dynamic decomposition | Verified [S5] |
| **CrewAI** | Crew with a delegating manager | Manager resolves | Parallel business-process workflows | Shared-state coherence | Inferred [S16] |
| **LangGraph** | Graph state machine, conditional branches, HITL checkpoints | Graph-conditional fallback / rollback | Best-in-class production stateful parallelism | Rapid iteration | Inferred [S16] |
| **← the coordination project** | **Per-session append-only JSONL claims log, folded to SQLite** | Hard leases with TTL + heartbeat; hotspots owned by an integrator | Auditability, replay, tool-agnosticism, pre-edit overlap detection | **Unproven** — no prior art; fencing gap; Cognition's Principle 2 | Inferred (novelty) |

**The pattern across the table:** every deployed system avoids conflict by **isolation** (separate contexts,
separate sandboxes, separate worktrees), by **turn-taking**, by **hierarchy**, or by **refusing parallel
writes altogether**. None coordinates concurrent writers on a shared tree. That is the gap the claims log
enters, and it is why there is no prior art to copy and no published failure list to avoid.

## Merge-integration mechanisms

| Mechanism | What it solves | What it does not solve | Confidence |
|---|---|---|---|
| **GitHub Merge Queue** | FIFO serialisation with CI per PR — "is main still green" | Two agents editing the same function; batching | Verified [S17] |
| **Mergify** | Batched parallel queues with bisection to isolate a failing PR | Same — integration, not authorship overlap | Verified [S17] |
| **Graphite, Bors/homu** | Same family | Same | Inferred |
| **git `union` merge driver** | Keeps all appended lines from both sides | **Does not deduplicate** overlapping appends — which is why one file per session matters | Verified [S13][S19] |

## Coordination primitives from distributed systems

| Primitive | What it gives | What it does not give | Source |
|---|---|---|---|
| **Lease + TTL** (Gray & Cheriton 1989; Chubby 2006) | Self-releasing locks; no liveness requirement on the holder | Correctness — the holder may be paused past expiry | Verified [S7] |
| **Heartbeat** | Extension while alive | Nothing about the pause between heartbeat and write | Verified [S6] |
| **Fencing token** (Kleppmann 2016) | **Correctness** — the resource rejects a stale writer | Requires the *write target* to validate it | Verified [S6] |
| **Optimistic concurrency** | No lock; conflict detected at commit | Needs a version check and a retry path | Reference |
| **Advisory locking** | A convention processes respect | Enforcement — violation is always possible | Reference |
| **Event sourcing** (Fowler, Young, Kleppmann) | Full history; replay; auditability; a fold to any read model | Schema evolution, replay cost, read-model staleness | Verified [S8] |
| **CRDTs** (Shapiro et al. 2011) | Order-independent convergence to one state | History — which the log preserves and a CRDT does not | Reference |

## Identifier options

| | ULID | UUIDv7 |
|---|---|---|
| Standard | community spec | **RFC 9562** (Proposed Standard, May 2024) |
| Length | **26** chars, Crockford Base32 | 36 chars, hex |
| Timestamp | 48-bit ms | 48-bit ms |
| Monotonic within a ms | increment random component | optional sub-ms counter |
| Choose it when | compactness in a log line matters | standards-track matters |

*(Verified, [S9][S10])*

## Adjacent ideas worth borrowing

- **MAST's intervention results** — a better role specification (+9.4%) and an explicit verification step
  (+15.6%) are the two cheapest known improvements to any multi-agent system, and both are protocol changes
  rather than model changes.
- **Anthropic's task-boundary discipline** — each subtask given an explicit output format, tool guidance and
  boundary. The `announce` verb is the same idea; making the *format* explicit is the part worth copying.
- **Mergify's batch bisection** — when N changes land together and the result is broken, isolating the
  culprit automatically is the integration-side analogue of claim overlap detection.
- **Anthropic's session-as-log** — independent arrival at the same primitive, for crash recovery. Evidence
  the shape is sound even though the application differs.
