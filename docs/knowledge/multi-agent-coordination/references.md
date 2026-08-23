---
id: kb-coord-references
title: "Multi-Agent Coordination — references and measured numbers"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, mast, metr, rfc-9562, ulid, chubby, event-sourcing]
links:
  - { to: kb-multi-agent-coordination, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The papers and specifications behind this domain, plus every measured number with its source
  — token multipliers, MAST failure frequencies, METR's productivity result, and the ULID and
  UUIDv7 constants.
---

# Reference information

## Foundational papers and specifications

- **Gray & Cheriton (SOSP 1989)** — *Leases: An Efficient Fault-Tolerant Mechanism for Distributed File
  Cache Consistency*. The origin of the lease. *(Reference)*
- **Burrows (USENIX OSDI 2006)** — *The Chubby lock service for loosely-coupled distributed systems*. The
  canonical production lease service and its lessons. *(Verified, [S7])*
- **Kleppmann (2016-02-08)** — *How to do distributed locking*. The fencing-token argument; the GC-pause
  example; the HBase incident. **The load-bearing critique of TTL-only leases.** *(Verified, [S6])*
- **Fowler (2005)** — *Event Sourcing*; **Young (2010)** — *CQRS*; **Kleppmann (2015)** — *Turning the
  database inside-out* and *Using logs to build a solid data infrastructure*. The event-sourcing canon.
  *(Verified for Kleppmann [S8][S20]; Fowler and Young referenced)*
- **Shapiro et al. (INRIA RR-7506, 2011)** — *A Comprehensive Study of Convergent and Commutative Replicated
  Data Types*. The canonical CRDT reference. *(Reference)*
- **RFC 9562 (IETF, May 2024)** — *Universally Unique IDentifiers (UUIDs)*, defining UUIDv7. *(Verified, [S10])*
- **ULID specification** — community spec, no RFC. *(Verified, [S9])*
- **Chen et al., arXiv:2503.13657 (2025)** — *Why Do Multi-Agent LLM Systems Fail?* (MAST), UC Berkeley.
  *(Verified, [S5][S18])*
- **METR, arXiv:2507.09089 (July 2025)** — *Measuring the Impact of Early-2025 AI on Experienced Open-Source
  Developer Productivity*. *(Verified, [S11])*
- **Shapira et al. (NeurIPS 2024)** — *SWE-agent: Agent-Computer Interfaces Enable Automated Software
  Engineering*. **Wang et al. (2024)** — *OpenHands*. *(Verified, [S14][S15])*
- **git-worktree documentation** and **Claude Code worktrees documentation**. *(Verified, [S13][S12])*

## Measured numbers and constants

### Anthropic — first-party measurements

| Metric | Value |
|---|---|
| Multi-agent token usage vs chat | **~15×** |
| Single-agent token usage vs chat | ~4× |
| Share of BrowseComp performance variance explained by token usage | **80%** |
| Multi-agent vs single-agent Opus 4, internal research eval | **+90.2%** |

Quoted caution: *"Most coding tasks involve fewer truly parallelizable tasks than research, and LLM agents
are not yet great at coordinating and delegating to other agents in real time."* *(Verified, [S1])*

### MAST — failure taxonomy

| Metric | Value |
|---|---|
| Traces analysed | **1,600+** |
| Inter-rater agreement (Cohen's κ) | **0.88** |
| Categories / fine-grained modes | **3** / **14** |
| FM-1.3 Step repetition | **15.7%** |
| FM-2.6 Action-reasoning mismatch | **13.2%** |
| FM-1.5 Unaware of termination | **12.4%** |
| FM-1.1 Disobey task specification | **11.8%** |
| FM-3.3 Incorrect verification | **9.1%** |
| Intervention — improved role spec (ChatDev) | **+9.4%** success |
| Intervention — added verification step (ChatDev) | **+15.6%** success |

The three categories: **system design**, **inter-agent misalignment**, **task verification**. *(Verified, [S5])*

### METR — the productivity RCT

| Metric | Value |
|---|---|
| Participants | **16** experienced open-source developers |
| Tasks | **246** real GitHub issues |
| Measured effect of AI tools | **−19%** (slower) |
| Perceived effect | **+20%** (faster) |
| Pre-study expert prediction | +24–39% faster |
| Developer pay rate used | $150/hr |

*(Verified, [S11])*

### Benchmarks

| System | SWE-bench Verified | Model |
|---|---|---|
| OpenHands | **77.6%** | Claude Opus 4.5 |
| SWE-agent | **72.0%** | Claude Opus 4.5 |

*(Verified, [S14][S15] via [S22])*

### Identifiers

| Property | ULID | UUIDv7 |
|---|---|---|
| Standard | community spec, no RFC | **RFC 9562**, Proposed Standard, **May 2024** |
| Timestamp | 48-bit Unix ms | 48-bit Unix ms |
| Random bits | **80** | remainder |
| String length | **26** (Crockford Base32) | **36** (hex) |
| Monotonicity within one ms | increment random component by 1 bit | optional sub-ms counter |

*(Verified, [S9][S10])*

### Git mechanics

| Fact | Value |
|---|---|
| Shared across worktrees | objects, remote refs, **`.git/hooks/`** |
| Per-worktree | HEAD, index, partial config, working files |
| `${CLAUDE_PROJECT_DIR}` after entering a worktree | **project root**, not the worktree |
| Worktree path available to a hook via | `cwd` in the hook input JSON |
| `union` merge driver | keeps all lines from both sides; **does not deduplicate** |
| Pre-commit hook bypass | `git commit --no-verify` |

*(Verified, [S12][S13][S19])*

## The fencing-token requirement, stated precisely

From Kleppmann's argument, in the form that matters for a claims log:

1. The lock service issues a **monotonically increasing token** on each acquisition.
2. Every write carries its token.
3. **The resource being written rejects any write whose token is lower than the highest it has seen.**

Step 3 is the one that cannot be skipped: without it, a process that was paused past its lease expiry writes
successfully, and the lock service's belief and reality have diverged with no way to detect it. TTL and
heartbeat address *efficiency* — avoiding duplicate work — and never *correctness*. *(Verified, [S6][S7])*
