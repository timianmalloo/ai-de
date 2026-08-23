---
id: kb-coord-sources
title: "Multi-Agent Coordination — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-multi-agent-coordination, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The full access-dated source list behind the multi-agent coordination knowledge base, keyed
  [S1]..[S22], distinguishing first-party measurement from vendor comparison.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | Anthropic — How we built our multi-agent research system (Jun 2025) | primary (first-party engineering) | https://www.anthropic.com/engineering/multi-agent-research-system | **15× / 4× token multipliers, 80% variance, +90.2%**, over-spawning, and the quoted coding caution |
| S2 | Anthropic — Building Effective Agents | primary | https://www.anthropic.com/engineering/building-effective-agents | Orchestrator-worker pattern, workflow taxonomy |
| S3 | Anthropic — Managed Agents | primary | https://www.anthropic.com/engineering/managed-agents | Session-as-append-only-log; decoupled harness/sandbox/session |
| S4 | Cognition (Walden Yan) — Don't Build Multi-Agents | primary (vendor blog) | https://cognition.com/blog/dont-build-multi-agents | **The main disconfirming view**: Principles 1 & 2; the Claude Code design note |
| S5 | Why Do Multi-Agent LLM Systems Fail? (MAST), arXiv:2503.13657 | academic | https://arxiv.org/html/2503.13657 | 14 failure modes with frequencies, 3 categories, intervention results, κ = 0.88 |
| S6 | Kleppmann — How to do distributed locking (2016-02-08) | primary (technical) | https://martin.kleppmann.com/2016/02/08/how-to-do-distributed-locking.html | **Fencing tokens**; the GC-pause hazard; the HBase incident |
| S7 | Burrows — The Chubby lock service (USENIX OSDI 2006) | academic | https://research.google.com/archive/chubby-osdi06.pdf | Lease design and production lessons |
| S8 | Kleppmann — Turning the database inside-out (2015-03) | primary (talk transcript) | https://martin.kleppmann.com/2015/03/04/turning-the-database-inside-out.html | Event sourcing, immutable facts, the fold |
| S9 | ULID specification | primary (community spec) | https://github.com/ulid/spec | 48-bit ms + 80 random bits, 26 chars, monotonicity rule |
| S10 | RFC 9562 — UUIDs | standard (IETF) | https://www.rfc-editor.org/rfc/rfc9562 | UUIDv7 structure, sub-ms counter, Proposed Standard, May 2024 |
| S11 | METR — Measuring the Impact of Early-2025 AI on Experienced OSS Developer Productivity, arXiv:2507.09089 | academic | https://arxiv.org/abs/2507.09089 | **−19% actual / +20% perceived**; 16 devs, 246 tasks |
| S12 | Claude Code — Run parallel sessions with worktrees | primary (vendor docs) | https://code.claude.com/docs/en/worktrees | Worktree mechanics, shared hooks, `${CLAUDE_PROJECT_DIR}` behaviour, cleanup |
| S13 | git-worktree documentation | primary (official) | https://git-scm.com/docs/git-worktree | Shared objects/refs, per-worktree HEAD/index, submodule caveats |
| S14 | SWE-agent (NeurIPS 2024) | academic | — (paper; scores via [S22]) | ACI concept; 72.0% SWE-bench Verified |
| S15 | OpenHands (2024) | academic | https://shiptheloop.com/sigint/papers/openhands-ai-sw-agent-2024/ | Event-driven Docker-sandboxed architecture; 77.6% |
| S16 | AutoGen vs CrewAI vs LangGraph comparison — Galileo AI | secondary (technical blog) | https://galileo.ai/blog/autogen-vs-crewai-vs-langgraph-vs-openai-agents-framework | Framework coordination mechanisms — **Inferred** |
| S17 | Mergify vs GitHub Merge Queue | secondary (vendor comparison) | https://mergify.com/compare/github-merge-queue | Merge-queue mechanics, batching, bisection |
| S18 | MAST repository and dataset | primary (repo) | https://github.com/multi-agent-systems-failure-taxonomy/MAST | Dataset, LLM-as-judge pipeline |
| S19 | JSONL ledgers in git as an agent state layer | secondary (technical blog) | https://dev.to/rulestack/jsonl-ledgers-in-git-as-the-state-layer-for-an-autonomous-agent-patterns-that-survive-crashes-and-4ljp | Per-agent JSONL + union merge driver pattern |
| S20 | Kleppmann — Using logs to build a solid data infrastructure (2015-05) | primary | https://martin.kleppmann.com/2015/05/27/logs-for-data-infrastructure.html | Dual-write problem; log as infrastructure |
| S21 | Shapiro et al. — CRDT study (INRIA RR-7506, 2011) | academic | — | Canonical CRDT reference — **referenced, not fetched** |
| S22 | SWE-bench leaderboard | benchmark | https://www.swebench.com/ | OpenHands 77.6%, SWE-agent 72.0% |

## Source-quality notes

- **First-party measurement is distinguished from vendor comparison throughout.** The token multipliers,
  the +90.2%, and the coding caution are Anthropic measuring their own system ([S1]) — the strongest class of
  evidence available here, and notable because the caution works *against* the vendor's interest.
- **MAST ([S5]) and METR ([S11]) are peer-reviewable academic work with stated methodology**, sample sizes
  and agreement statistics. Their numbers are quoted exactly and are the only rigorous quantities in the
  domain.
- **Kleppmann ([S6]) and Chubby ([S7])** are the load-bearing distributed-systems sources; the fencing
  argument is quoted in the form that applies to a claims log.
- **[S16] and [S17] are secondary** — a technical blog and a vendor comparison respectively. Everything
  drawn from them is labelled **Inferred**.
- **Gray & Cheriton (1989), Fowler (2005), Young (2010) and Shapiro et al. (2011)** are referenced from the
  research summary and **were not fetched**; they are canonical enough to cite but should be read before
  being quoted in a design.
- **OpenAI Codex's architecture** has no primary engineering source; every claim about it is **Inferred**.
