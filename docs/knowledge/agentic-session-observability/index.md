---
id: kb-agentic-session-observability
title: "Agentic Session Observability, Coordination, Learning, and Scoring"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: "discovery"
tags: [agent-observability, coordination, evaluation, continuous-learning, terminal-sessions]
links:
  - { to: kb-multi-agent-coordination, rel: refines }
  - { to: spec-ai-native-ide, rel: relates-to }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-11-28
review-suggested: []
summary: >-
  Evidence base for a local watcher that registers terminal-agent sessions across repositories,
  observes their traces and coordination, supports shared knowledge, evaluates agent effectiveness,
  and turns repeated failure patterns into reviewable daydream learnings.
---

# Agentic Session Observability, Coordination, Learning, and Scoring

**Domain and problem:** A local agentic substrate must observe concurrent terminal-agent sessions
across multiple repositories, map each session to its repository, worktree, terminal, and agent
identity, coordinate agents through repo-scoped shared records, evaluate how effectively each agent
serves its stated goal, and surface reusable learning without turning one observer's inference into
unreviewed policy.

**Canonical framing:** The field splits this into four coupled systems: agent observability
(session/trace/span), coordination (registration, leases, events, and messages), evaluation
(outcome plus trajectory/process evidence), and context evolution (reflection, curation, and memory).
The proposed watcher joins those systems. It is not merely a dashboard and should not be framed as a
distributed consensus service on a single machine. [Inferred synthesis from S1-S10]

**Grounding path:** `kb-agentic-session-observability -> kb-multi-agent-coordination ->
seed-agent-coordination-spec`, plus `spec-ai-native-ide -> session-contracts`. The existing repo
already has per-session append-only coordination logs, worktree registration, claims, audit/change
logs, a dream workflow, and a documented Core/Design seam. This base extends those records rather
than creating a competing ledger. [Verified: R1-R5]

**Compiled:** 2026-08-30 | **Lead:** Domain Researcher | **Status:** fresh

## Headline findings

1. **A session-scoped distributed trace is the strongest common model.** OpenTelemetry's GenAI
   conventions define agent, workflow, plan, and tool spans; Claude Code exports OTel metrics and
   events; LangSmith groups runs into traces, threads, and trajectories. The standard is real but
   still marked **Development**, so a watcher should pin a schema snapshot and preserve vendor
   extensions. [Verified: S1-S4]
2. **A dual ingest path is required.** Native OTLP is the preferred path when an agent emits it, but
   agent CLIs and subprocesses have uneven telemetry. Claude Code explicitly does not pass `OTEL_*`
   variables to Bash, hooks, MCP servers, or language servers. Registration and local event adapters
   remain necessary. [Verified: S2]
3. **Identity must be layered, not compressed into one agent name.** The minimum useful map is
   `machine -> repository -> worktree -> terminal -> agent session -> turn/trace -> span`, with
   branch, process generation, provider, model, and parent session as attributes. No current
   standard defines the repo/worktree layer. [Verified for session/trace hierarchy: S1-S4;
   Flagged for cross-repo standardization]
4. **Borrow ZooKeeper/Chubby semantics, not their deployment.** Ephemeral session registration,
   heartbeats, server-authoritative expiry, ordered watches, monotonic versions, and fencing are
   relevant. Paxos/Raft clusters, quorums, and gossip are unjustified for a single-machine
   authoritative watcher unless the scope expands to multiple hosts. [Verified: S5-S7]
5. **A lease is liveness evidence, not correctness.** A paused process can outlive its lease and
   resume. Exclusive actions require a monotonic fencing token that the guarded resource checks.
   The current repo knowledge already identifies this gap in claim-based coordination. [Verified:
   S5-S7, R1]
6. **Agent quality cannot be reduced safely to one opaque score.** Outcome correctness,
   verification, task focus, termination discipline, guidance adherence, evidence quality,
   coordination behavior, efficiency, and solution simplicity are distinct dimensions. A headline
   score may summarize them only if the dimension scores and evidence remain visible. [Verified for
   multi-dimensional failure/evaluation: S8-S13; Inferred for the final score composition]
7. **Outcome and process evidence must be separated.** Tests, builds, and delivered acceptance
   criteria are deterministic outcome evidence. Trajectory graders can detect repetition, drift,
   action-reasoning mismatch, and weak verification, but model judges are biased and gameable.
   [Verified: S8-S13]
8. **Do not optimize agents directly against a visible reasoning-trace grader.** OpenAI found that
   chain-of-thought monitoring can detect reward hacking, but strong optimization against that
   monitor teaches agents to obfuscate the monitored reasoning while continuing to exploit the
   objective. The watcher should monitor observable rationale and actions, not demand private hidden
   reasoning, and should keep some evaluation signals held out. [Verified: S12]
9. **The safe daydream loop is observation -> candidate lesson -> disconfirmation -> curation ->
   promotion.** Reflexion, ExpeL, Agent Workflow Memory, and ACE support learning from experience;
   ACE's incremental playbook updates address context collapse. AgentPoison shows that memory is a
   trust boundary: automatic promotion can preserve an error or an attack indefinitely. [Verified:
   S14-S19]
10. **Measure real work, not felt productivity.** METR's 2025 RCT found a 19% slowdown while users
    believed they were faster, then its 2026 follow-up found the newer estimate unreliable because
    task selection and concurrent-agent use broke the measurement design. Scoring must use objective
    task evidence and declare measurement gaps. This updates the older productivity framing in
    `kb-multi-agent-coordination`: the 2025 RCT remains valid for its cohort; the 2026 effect size is
    explicitly unresolved. [Verified: S10-S11]

## Confidence summary

- **Verified:** OTel's Development status and span vocabulary; Claude Code OTLP behavior and
  subprocess gap; LangSmith's run/trace/thread/trajectory model; Anthropic's token and coding-fit
  cautions; ZooKeeper session/watch/version semantics; Chubby's intended role; MAST's dataset and
  taxonomy; METR's measured results and caveats; reward-hacking monitor obfuscation; ACE context
  collapse; AgentPoison's memory attack; the current repo's coordination/audit/dream records.
- **Inferred:** the recommended normalized identity hierarchy; the exact composition of an agent
  score; that a local watcher can use one embedded linearizable store without consensus.
- **Flagged:** standardized repo/worktree telemetry attributes; reliable access to all local
  Copilot/Codex session events; whether any product exposes enough observable rationale to grade
  "reasoning" without collecting private chain-of-thought; the calibration dataset for simplicity
  and guidance-adherence scores.

## Design implications

- Reuse the repo's append-only coordination and audit records as aligned event sources. Add adapters,
  not a second truth.
- Define the identity map before defining dashboards: Repository, Worktree, Terminal, Agent Session,
  Turn/Trace, and Span are distinct entities.
- Prefer OTLP for observable agent/tool/model events, with registration/file adapters for opaque
  sessions and subprocess gaps.
- Keep high-cardinality trace data separate from low-cardinality health and score aggregates.
- Treat prompt, code, transcript, and reasoning-like content as opt-in work data with redaction,
  retention, and per-repo policy.
- Publish a scorecard with evidence per dimension. Never let the headline number hide a failed
  correctness or safety floor.
- Make the daydream log append-only and provenance-bearing. Promotion into agent guidance requires
  repeated evidence or a deterministic reproduction, a disconfirming check, and review.
- Feedback to an agent should name one observable behavior and one next-turn correction. Avoid
  exposing the full scoring implementation or optimizing directly against hidden evaluation signals.
- Scores evaluate agent/model behavior for the operator's own benefit. They are not aggregated by
  identifiable human and are not an input to personnel or performance evaluation.
- V1 is local-only. OTLP exporters, model judges, and learning stores may not send work content to a
  third party; any future egress re-opens privacy, security, residency, and provider-retention review.
- Scoring is a declared purpose of the captured events, not a silent repurposing of coordination or
  audit data. The operator must receive notice of what is captured, why, for how long, and how to
  delete it.
- Model graders consume untrusted content. Their output is advisory and cannot write an authoritative
  score or promote a lesson without deterministic validation or a human gate.
- Detect watcher blind spots explicitly: unregistered sessions, missing telemetry, truncated
  trajectories, stale heartbeats, and unknown task goals must render as **not recorded**, never as a
  good score.

## How to use this base

The specification and later architecture should cite the files in this directory. `state-of-the-art.md`
defines the evidence-backed system boundaries; `data-and-constants.md` contains the values worth
quoting; `open-questions.md` is the list of decisions that research did not settle.
