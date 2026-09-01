---
id: kb-agentic-session-observability-references
title: "Agentic Session Observability - References"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: "discovery"
tags: [references, standards, papers]
links:
  - { to: kb-agentic-session-observability, rel: refines }
review-by: 2026-11-28
review-suggested: []
summary: >-
  Standards, official documentation, benchmark papers, and learning-safety research that establish
  the watcher domain's contracts and known limitations.
---

# Reference information

## Standards and official specifications

- **OpenTelemetry GenAI semantic conventions** - agent and framework span vocabulary. Status is
  Development; pin a compatible schema version. [Verified: S1]
- **ZooKeeper Programmer's Guide** - canonical session, ephemeral node, watch, version, and zxid
  semantics. [Verified: S6]
- **RFC 9562 UUIDv7** - standards-track time-ordered identity option already established in the
  repo's coordination knowledge. [Verified through R1]

## Official product and framework sources

- **Claude Code Monitoring** - OTel metrics/events/traces, session-start metric, content controls,
  managed destination, and subprocess non-propagation. [Verified: S2]
- **LangSmith Observability Concepts** - run, trace, thread, trajectory, feedback, and trace limits.
  [Verified: S3]
- **OpenHands Backend Architecture** - EventStream, Action/Observation, and runtime separation.
  [Verified: S4]
- **Anthropic multi-agent research system** - orchestration, effort scaling, observability,
  evaluation, token cost, and the coding-parallelism caution. [Verified: S5]
- **Google Chubby paper page** - coarse-grained advisory locking and reliable low-volume storage.
  [Verified: S7]

## Evaluation and productivity research

- **MAST (2025)** - 1,600+ traces, 14 failure modes, three categories, kappa 0.88. [Verified: S8]
- **SWE-bench** - repository-level outcome evaluation against executable tests. [Verified: S9]
- **METR early-2025 study and 2026 update** - objective productivity measurement, perception gap,
  selection effects, and concurrent-agent timing limitations. [Verified: S10-S11]
- **Monitoring Reasoning Models for Misbehavior (2025)** - reasoning-trace monitoring and
  optimization-induced obfuscation. [Verified: S12]

## Continuous learning and memory safety

- **Reflexion, ExpeL, Agent Workflow Memory** - episodic reflection, extracted lessons, and reusable
  workflows. [Verified: S14, S16-S17]
- **Agentic Context Engineering (ACE)** - evolving playbooks, incremental curation, brevity bias,
  and context collapse. [Verified: S18]
- **AgentPoison** - memory and knowledge-base poisoning as an agent trust-boundary attack.
  [Verified: S19]

## Repository authorities

- **Multi-agent coordination knowledge base** - the existing claims-log research and fencing gap.
  [Verified: R1]
- **Agent coordination seed specification** - the original event vocabulary and repo-local fold.
  [Verified: R2]
- **`coord-core.py`** - the implemented Phase-1 session/worktree/claim record. [Verified: R3]
- **Session contracts** - current cross-session ownership and merge protocol. [Verified: R4]
- **Audit, dream, and defect-class records** - the existing learning and evidence surfaces the
  watcher must align with. [Verified: R5]
