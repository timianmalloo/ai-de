---
id: kb-agentic-session-observability-sources
title: "Agentic Session Observability - Sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: "discovery"
tags: [sources, citations, provenance]
links:
  - { to: kb-agentic-session-observability, rel: refines }
review-by: 2026-11-28
review-suggested: []
summary: >-
  Full external and repository source list for the agentic watcher evidence base, with access dates
  and the claims each source supports.
---

# Sources

All external sources were accessed 2026-08-30.

| ID | Source | Type | URL / path | Used for |
|---|---|---|---|---|
| S1 | OpenTelemetry GenAI agent spans | standard, Development | https://github.com/open-telemetry/semantic-conventions-genai/blob/main/docs/gen-ai/gen-ai-agent-spans.md | Agent/workflow/plan/tool span vocabulary and stability |
| S2 | Claude Code Monitoring | official docs | https://code.claude.com/docs/en/monitoring-usage | Native OTLP, session signal, content and cardinality controls, subprocess gap |
| S3 | LangSmith Observability Concepts | official docs | https://docs.langchain.com/langsmith/observability-concepts | Run, trace, thread, trajectory, feedback, 25,000-run limit |
| S4 | OpenHands Backend Architecture | official docs | https://docs.openhands.dev/openhands/usage/architecture/backend | EventStream and Action/Observation runtime model |
| S5 | How we built our multi-agent research system | official engineering report | https://www.anthropic.com/engineering/multi-agent-research-system | Orchestrator-worker, 90.2%, 15x tokens, 80% variance, coding caution |
| S6 | ZooKeeper Programmer's Guide | official docs | https://zookeeper.apache.org/doc/current/zookeeperProgrammers.html | Ephemeral sessions, heartbeat expiry, watches, versions, zxid, small metadata |
| S7 | The Chubby lock service | primary paper page | https://research.google/pubs/the-chubby-lock-service-for-loosely-coupled-distributed-systems/ | Coarse-grained advisory locks and reliable low-volume storage |
| S8 | Why Do Multi-Agent LLM Systems Fail? (MAST v3) | research paper | https://arxiv.org/abs/2503.13657v3 | 1,600+ traces, 14 modes, 3 categories, kappa 0.88 |
| S9 | SWE-bench | research paper | https://arxiv.org/abs/2310.06770 | Repository outcome evaluation against executable tests |
| S10 | Measuring early-2025 AI on experienced OSS developers | RCT / official report | https://metr.org/blog/2025-07-10-early-2025-ai-experienced-os-dev-study/ | 19% slowdown, perception gap, benchmark-vs-work distinction |
| S11 | METR uplift update | official report | https://metr.org/blog/2026-02-24-uplift-update/ | Selection effects, unreliable current estimate, concurrent-agent timing difficulty |
| S12 | Monitoring Reasoning Models for Misbehavior | research paper | https://arxiv.org/abs/2503.11926 | CoT monitoring, reward hacking, obfuscation under optimization |
| S13 | Agent-as-a-Judge survey | research survey | https://arxiv.org/abs/2508.02994 | Outcome, trajectory, and component evaluation; judge limitations |
| S14 | Reflexion | research paper | https://arxiv.org/abs/2303.11366 | Verbal reflection and episodic memory |
| S16 | ExpeL | research paper | https://arxiv.org/abs/2308.10144 | Experience-derived natural-language lessons |
| S17 | Agent Workflow Memory | research paper | https://arxiv.org/abs/2409.07429 | Reusable workflows learned from trajectories |
| S18 | Agentic Context Engineering v3 | research paper, ICLR 2026 | https://arxiv.org/abs/2510.04618v3 | Incremental context curation, brevity bias, context collapse |
| S19 | AgentPoison | security research | https://arxiv.org/abs/2407.12784 | Memory poisoning and stealthy retrieval attacks |
| R1 | Multi-Agent Coordination knowledge base | repository authority | docs/knowledge/multi-agent-coordination/index.md | Existing evidence, claims thesis, fencing gap |
| R2 | Agent Coordination Layer specification v0.1 | repository seed | docs/knowledge/seed-material/agent-coordination-spec.md | Event vocabulary, append-only per-session records, projections |
| R3 | Phase-1 coordination core | implementation | docs/ai-forward-pack/scripts/coord-core.py | Repo-root record, worktree registration, claims, TTL, metrics |
| R4 | Two-session contract | repository contract | docs/collaboration/session-contracts.md | Active ownership seam and merge protocol |
| R5 | Audit, dream, and defect-class records | repository records | docs/audit/ ; docs/ai-forward-pack/scripts/dream.py ; docs/lessons/defect-classes.md | Existing durable history and continuous-improvement inputs |

## Source-quality notes

- S1 is authoritative but explicitly unstable.
- S10 is historical and now marked out of date by S11; both are retained because the change in
  evidence and the measurement limitations are design-relevant.
- S13 is a survey, so individual judge-performance claims should be checked against the underlying
  papers before becoming architecture thresholds.
- S14-S18 establish that experiential context can improve agents; they do not establish that
  unreviewed online promotion is safe.
