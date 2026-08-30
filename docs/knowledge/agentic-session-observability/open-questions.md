---
id: kb-agentic-session-observability-open
title: "Agentic Session Observability - Open Questions"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: "discovery"
tags: [open-questions, risks, disconfirmation]
links:
  - { to: kb-agentic-session-observability, rel: refines }
review-by: 2026-11-28
review-suggested: []
summary: >-
  Unsettled contracts, known domain failure modes, and the strongest arguments against a watcher
  that scores and continuously teaches active coding agents.
---

# Open questions and domain failure modes

## Unresolved by research

1. **How much observable rationale is available?** Tool calls, messages, files, and tests are
   observable. Private hidden chain-of-thought may be unavailable and should not be required.
   Determine the supported event contract per terminal agent. [Flagged]
2. **What is the canonical cross-repo identity?** No OTel convention standardizes repository,
   worktree, and terminal identity. A local contract must be defined and versioned. [Flagged]
3. **How are non-AI-Forward sessions injected with coordination guidance?** Shell profile,
   wrapper, environment, startup prompt, MCP, and terminal host injection have different trust and
   portability properties. Each agent tool must be spiked. [Flagged]
4. **Which score dimensions are comparable across task classes?** A documentation task and a
   concurrency fix cannot share raw time, token, or diff-size expectations. The scorer needs task
   classification and calibrated baselines. [Flagged]
5. **How is simplicity scored without rewarding under-building?** Static complexity and diff size
   are weak proxies. A human-calibrated rubric and acceptance-criteria trace are likely required.
   [Flagged]
6. **What feedback reaches the agent, and when?** Immediate feedback can help the next turn but also
   causes grade optimization. The safest granularity and delay require evaluation. [Flagged]
7. **What is the retention policy for prompts, code fragments, tool arguments, and outputs?** This is
   work data and may contain secrets or personal data. Per-repo policy and redaction are required.
   [Flagged]
8. **Can the current coordination log carry messages without becoming a hotspot?** The existing
   one-file-per-session shape merges well. Reply threading, acknowledgement, and search need a
   contract without introducing a single shared mutable board file. [Flagged]
9. **How does the daydream log join the existing dream workflow?** The schema must align without
   letting the online watcher auto-promote into the offline fleet-learning store. [Flagged]
10. **What is the watcher failure posture?** Agent work must continue if the watcher is unavailable,
    but exclusive actions and score claims must fail honestly. [Flagged]
11. **How is worker-surveillance repurposing prevented?** Scores must evaluate agent/model behavior
    for the operator's benefit, not rank an identifiable person. Decide whether idle/wait time and
    rework can be used without becoming personnel monitoring. [Flagged - privacy Blocker]
12. **What is the declared purpose and notice model?** Reusing coordination and audit records for
    scoring is a new purpose. The operator must know what is captured, why, retention, deletion, and
    that scores are not employment-performance data. [Flagged - privacy Major]
13. **How is grader egress prevented?** V1 should keep OTLP, model grading, traces, and work content
    local. Any external provider requires an approved data-class, purpose, residency, retention, and
    training posture. [Flagged - privacy/security Blocker]
14. **How are daydream learnings deleted or retracted?** Source-event deletion and correction must
    reach derived lessons and remove or supersede promoted guidance. [Flagged - privacy/data Major]
15. **How is registration authenticated?** Environment-provided session identity is asserted, not
    authenticated. Decide the local trust model or issue an unforgeable per-session capability.
    [Flagged - security Major]
16. **How is local OTLP event injection prevented or labeled?** A local process can forge another
    session's spans unless ingress is bound and authenticated. Ingested telemetry alone must not
    satisfy a correctness floor. [Flagged - security Major]

## Known failure modes

- **Watcher as judge:** a single score becomes authority and hides failed dimensions.
- **Goodhart compliance theater:** agents add ceremony, verbose rationales, or trivial checks because
  those are visible to the scorer.
- **Obfuscated reasoning:** agents learn to hide exploit intent when the monitor becomes a reward.
- **Telemetry silence as success:** an unregistered or opaque session receives a high score because
  nothing bad was observed.
- **Stale session resurrection:** a process resumes after heartbeat expiry and acts on an old grant.
- **Repo identity aliasing:** two different repositories or worktrees collapse to one path-derived
  identifier.
- **Message-board poisoning:** an untrusted breadcrumb is retrieved later as coordination truth.
- **Context collapse:** repeated consolidation removes the exception or boundary that made a lesson
  correct.
- **Self-reinforcing lesson:** one mistaken diagnosis changes guidance, which then makes later agents
  reproduce the diagnosis.
- **Cross-task metric leakage:** a metric calibrated for one task class unfairly scores another.
- **Hidden evaluator drift:** model judge, rubric, or prompt changes without score versioning.
- **Privacy over-collection:** prompts and tool payloads are captured by default because they are
  useful for scoring.
- **Worker-surveillance repurposing:** per-terminal efficiency metrics become a proxy ranking of the
  human operator.
- **Grader prompt injection:** captured content instructs the model judge to award a score or promote
  a lesson.
- **Forged registration or telemetry:** a local process impersonates another session through
  environment identity or an unauthenticated OTLP receiver.
- **Derived-data orphaning:** source observations are deleted but their promoted lesson remains in
  agent guidance.
- **Coordination overreach:** a local watcher grows Paxos/Raft machinery before multi-host authority
  is a requirement.

## Disconfirming views deliberately sought

**Against multi-agent supervision:** Anthropic states that coding has fewer truly parallelizable
tasks than research and that agents are weak at real-time delegation. The watcher must not imply that
more concurrent agents are always better. [Verified: S5]

**Against process scoring:** reasoning-trace monitors can detect reward hacking, but optimization
against them creates obfuscation. Process scores should inform diagnosis and held-out evaluation, not
be the sole reward. [Verified: S12]

**Against automatic learning:** ACE shows benefits from context evolution, but AgentPoison shows that
memory is an attack surface and that very sparse poisoning can remain stealthy. Promotion must be
reviewable and reversible. [Verified: S18-S19]

**Against a consensus substrate:** ZooKeeper and Chubby prove useful semantics, but their replicated
deployment solves failures that a local single-machine authority does not have. Reusing the concepts
is justified; copying the operational system is not. [Verified/Inferred: S6-S7]

## Cheapest next probes

- Spike session event and telemetry surfaces for Copilot CLI, Claude Code, Codex CLI, and a plain
  shell wrapper.
- Characterize the existing `.agents` event fold and `dream.py` input schemas against a proposed
  daydream event.
- Build a small offline scoring corpus from committed audit entries and have two human reviewers
  label focus, evidence discipline, verification, and simplicity to test rubric agreement.
- Red-team a candidate lesson store with contradictory and malicious message-board entries.
- Test a forged registration, forged heartbeat, forged OTLP span, and injected grader instruction.
- Label a small scoring corpus by both agent and human identity, then prove the product can aggregate
  by agent/model/task class without exposing a personnel ranking.
