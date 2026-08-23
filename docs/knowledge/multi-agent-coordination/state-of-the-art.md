---
id: kb-coord-sota
title: "Multi-Agent Coordination — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [multi-agent, anthropic, mast, event-sourcing, leases, git-worktree]
links:
  - { to: kb-multi-agent-coordination, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  What the deployed multi-agent systems actually do, what MAST measured about how they fail,
  the distributed-systems primitives behind leases and logs, and git's real behaviour as a
  coordination substrate.
---

# State of the art — coordinating multiple coding agents

## The deployed systems

**Anthropic's research system (June 2025).** Orchestrator-worker: a lead agent (Opus 4) decomposing into
parallel subagents (Sonnet 4) with scoped tools and **separate context windows**. The orchestrator supplies
each subtask with an explicit output format, tool guidance and task boundaries. Measured: **+90.2%** over
single-agent Opus 4 on their internal research eval, at **~15×** chat token usage (single agent ~4×), with
token usage explaining **80%** of BrowseComp performance variance. An early failure mode they report is
agents spawning 50 subagents for simple queries and "distracting each other with excessive updates."
Parallelism helps for breadth-first search, work exceeding one context window, and heavy information
gathering; it hurts where subtasks have inter-dependencies — and they name coding explicitly:

> "Most coding tasks involve fewer truly parallelizable tasks than research, and LLM agents are not yet
> great at coordinating and delegating to other agents in real time."

*(Verified, [S1])*

**Anthropic's managed-agents architecture** decouples **session** (described as "the append-only log of
everything that happened"), **harness** (the brain) and **sandbox** (the hands), each replaceable
independently, with the harness calling the sandbox as a plain tool: `execute(name, input) → string`. The
session-as-append-only-log framing is strikingly close to the coordination spec's — but it is used there for
**crash recovery**, not for coordination between agents. *(Verified, [S3])*

**Claude Code worktrees.** `git worktree` per session; `--worktree` creates `.claude/worktrees/<name>/` on a
new branch; `EnterWorktree`/`ExitWorktree` tools. Objects and remote refs are shared; HEAD, index and partial
config are per-worktree; **hooks live in `.git/hooks/` and are shared**. `${CLAUDE_PROJECT_DIR}` stays at the
project root and the worktree path arrives as `cwd` in the hook input. Cleanup is explicit and
non-interactive runs do not auto-clean. Its documented use is "one session builds a feature while a second
fixes a bug" — file edits never collide because the trees are separate. *(Verified, [S12][S13])*

**Cognition / Devin** argue the opposite case (see `open-questions.md` for the full treatment) and prescribe
a single-threaded linear agent with context compression, carving out only "narrow, read-only subtasks where
context isn't fractured". They cite Claude Code's June 2025 subagent design approvingly as *purposefully
simple*: subagents answer questions, they do not write code in parallel. *(Verified, [S4])*

**OpenAI Codex cloud agent** gives each task an isolated sandboxed environment and submits PRs for review;
parallelism is by sandbox assignment with no shared workspace and no published coordination layer.
*(Inferred — no primary engineering source found)*

**GitHub Copilot coding agent** is developer-supervised and inline, handling multi-file changes via issue
integration, running locally or in Actions, with conflict resolution left to ordinary PR review.
*(Verified from official docs)*

**SWE-agent** (Princeton, NeurIPS 2024) is a deliberately minimal single-agent Agent-Computer Interface —
the reproducible baseline, at **72.0%** SWE-bench Verified with Claude Opus 4.5. **OpenHands** is
event-driven and Docker-sandboxed, the most actively developed open framework, at **77.6%**. Neither builds
coordination between concurrent agents on one tree. *(Verified, [S14][S15])*

**Frameworks.** AutoGen coordinates by turn-based group chat (conversational consensus, limited true
parallelism); MetaGPT/ChatDev by scripted role hierarchy with a managerial agent as arbiter; CrewAI by a
manager delegating to a "crew"; LangGraph by an explicit graph state machine with conditional branches,
human-in-the-loop checkpoints and native parallel branches — the best-in-class for production stateful
parallelism. *(MetaGPT/ChatDev Verified via [S5]; the rest Inferred from secondary sources, [S16])*

## How multi-agent systems fail — the MAST taxonomy

Across **1,600+ traces** with inter-rater agreement **Cohen's κ = 0.88**, MAST identifies **14 fine-grained
failure modes** in **3 categories**: system design, inter-agent misalignment, task verification. The
frequent ones:

| Failure mode | Frequency |
|---|---|
| Step repetition (FM-1.3) | **15.7%** |
| Action-reasoning mismatch (FM-2.6) | **13.2%** |
| Unaware of termination (FM-1.5) | **12.4%** |
| Disobey task specification (FM-1.1) | **11.8%** |
| Incorrect verification (FM-3.3) | **9.1%** |

Roughly **44%** are specification and design issues rather than model capability. The interventions matter
as much as the taxonomy: on ChatDev, improving the **role specification** alone gave **+9.4%** success, and
adding a **verification step** gave **+15.6%**. *(Verified, [S5][S18])*

## The distributed-systems primitives

**Leases** (Gray & Cheriton, SOSP 1989) are time-bounded locks that release themselves on expiry — the whole
point being that the holder need not be alive to release. **Chubby** (Burrows, OSDI 2006) is the canonical
production lease service and its published lessons are about exactly this hazard. *(Verified, [S7]; Gray & Cheriton referenced)*

**Kleppmann's fencing critique (2016)** is the definitive attack and remains unrefuted: a process can hold an
expired lease *and still be executing* — a GC pause, page fault or network delay suffices — so the lock
service's belief and reality diverge. The only correct fix is a **fencing token**: a monotonically
increasing number issued on each acquisition, which **the resource being written must itself reject** if it
is lower than the highest seen. Without that, leases are safe for *efficiency* (duplicate work is tolerable)
and unsafe for *correctness* (two holders both believing they are exclusive). *(Verified, [S6])*

**Advisory versus mandatory.** Every agent coordination scheme in this space — including claim-before-edit —
is **advisory**: processes voluntarily respect it and violation is possible. That is not a defect; it is
worth naming so nobody mistakes a convention for a guarantee.

**Event sourcing.** The canon is Fowler (2005), Young (2010) on CQRS, and Kleppmann's
turning-the-database-inside-out (2015): every state change is an immutable event in an append-only log, and
current state is a **fold** over it. Known operational pain points, all applicable here: **schema evolution**
of events (an old event must still replay after the shape changes), **replay cost** as the log grows, and
**eventual consistency of the read model** — the projection lags the log. *(Verified, [S8]; Fowler/Young referenced)*

**CRDTs** converge to a single state regardless of update order; a log preserves history. They are
complementary rather than alternatives — a CRDT answers "what is the state now", a log answers "how did we
get here", and the coordination design needs the second. *(Reference: Shapiro et al. 2011)*

## Git as the substrate

**Worktrees** share the object store and remote refs; HEAD, index and working files are per-worktree;
**hooks are shared** in `.git/hooks/`. Submodules carry documented caveats. *(Verified, [S13])*

**Append-only JSONL and merging.** Git's built-in `union` merge driver (`merge=union` in `.gitattributes`)
resolves conflicts by keeping all changed lines from both sides — correct for non-overlapping appends, and
it **creates duplicates for overlapping ones**. One file per agent session sidesteps this entirely, because
no two writers share a file. A custom driver could add semantic deduplication if the constraint were ever
relaxed. *(Verified, [S13][S19])*

**Merge queues** — GitHub Merge Queue serialises PRs FIFO with CI per PR; Mergify batches parallel queues
and bisects a failing batch to isolate the culprit; Graphite and Bors are the other entrants. These solve
"is main still green after N merges", not "did two agents edit the same function". *(Verified, [S17])*

**Git notes and commit trailers** are a metadata channel, scored in the coordination project's own decision
explorer as low on conflict-avoidance and parallelism relative to a repo-local file log.

## Identifiers

| | ULID | UUIDv7 |
|---|---|---|
| Standard | community spec, **no RFC** | **RFC 9562**, IETF Proposed Standard, May 2024 |
| Timestamp | 48-bit Unix ms | 48-bit Unix ms |
| Randomness | 80 bits | remaining bits |
| Encoding / length | Crockford Base32, **26 chars** | hex, **36 chars** |
| Monotonicity within a ms | increment the random component by 1 bit | optional sub-millisecond counter |

*(Verified, [S9][S10])*

## The frontier

- **No production multi-agent coding system uses an append-only coordination log.** The approach is novel;
  the closest published relative is Anthropic's managed-agents *session*, used for crash recovery rather
  than for inter-agent coordination. *(Inferred)*
- **No published merge-conflict-rate measurement** for concurrent AI agents on one repository exists —
  the very metric the design proposes to optimise.
- **No multi-agent productivity RCT** exists to sit alongside METR's single-developer result.
