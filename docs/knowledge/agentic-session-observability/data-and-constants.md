---
id: kb-agentic-session-observability-data
title: "Agentic Session Observability - Data and Constants"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: "discovery"
tags: [metrics, benchmarks, invariants, scoring]
links:
  - { to: kb-agentic-session-observability, rel: refines }
review-by: 2026-11-28
review-suggested: []
summary: >-
  Measured findings, system invariants, candidate score dimensions, and boundary conditions that
  should constrain later specification and architecture work.
---

# Domain data, constants, and invariants

## Verified measurements

| Measurement | Value | Meaning | Source |
|---|---:|---|---|
| Anthropic multi-agent research improvement | +90.2% on its internal research eval | Multi-agent breadth can help on suitable tasks | R1 / S5 |
| Multi-agent token use | about 15x chat | Coordination and parallel context are expensive | R1 / S5 |
| Token use explanatory power | 80% of BrowseComp performance variance | More compute is a major confound in multi-agent gains | R1 / S5 |
| MAST corpus | 1,600+ traces, 7 frameworks | Process failures have an empirical taxonomy | R1 / S8 |
| MAST taxonomy | 14 modes, 3 categories | Scoring should expose dimensions, not one label | R1 / S8 |
| MAST annotator agreement | Cohen's kappa 0.88 | The taxonomy had strong expert agreement | R1 / S8 |
| METR early-2025 result | 19% slower with AI | Perceived speed can be wrong | R1 / S10 |
| METR perceived effect | users believed about 20% faster after use | Self-report is not a sufficient metric | R1 / S10 |
| METR follow-up | 57 developers, 143 repos, 800+ tasks; effect estimate unreliable | Concurrent agents and selection break simple timing designs | S11 |
| LangSmith trace limit | 25,000 runs per trace | Long sessions need turn/thread partitioning | S3 |
| ZooKeeper znode guidance | coordination data in KB; hard sanity cap below 1 MB | Keep control-plane state small; store pointers to bulk data | S6 |
| AgentPoison | >80% attack success, <1% benign impact, <0.1% poison rate | Small poisoned memory can steer an agent stealthily | S19 |
| ACE benchmark gains | +10.6% agents, +8.6% finance | Incremental context curation can improve performance | S18 |
| Current `coord-core.py` default claim TTL | 300 seconds | Existing repo liveness assumption | R3 |

## Candidate score dimensions

These are **dimensions and observable signals, not a final formula**.

| Dimension | Observable evidence | Floor / caveat |
|---|---|---|
| Goal attainment | acceptance criteria met, tests/builds, requested artifact exists | A failed correctness floor cannot be averaged away |
| Verification quality | focal checks run, state read back, red-first evidence, residual risk stated | A green exit code alone is insufficient |
| Task focus and termination | goal-state match, off-goal actions, step repetition, work after done condition | Use MAST repetition/termination labels |
| Guidance adherence | repo instructions loaded, required skills/gates used, forbidden actions avoided | Missing telemetry renders not recorded |
| Evidence discipline | files/contracts read before claims, Verified/Inferred/Flagged split, assumption count | Do not infer private chain-of-thought |
| Coordination quality | registered session, claims honored, messages answered, collisions avoided | A quiet ledger may mean no participation |
| Efficiency | elapsed time, tool calls, tokens, retries, rework, idle/wait time | Compare within task class and difficulty |
| Simplicity and elegance | unnecessary files/layers/config, diff-to-requirement ratio, complexity delta | Requires human-calibrated rubric; current literature is weak |
| Learning effect | recurrence reduction, later task success with/without promoted lesson | Evaluate the lesson, not its eloquence |
| Integrity | test/harness tampering, score gaming, hidden work, misleading success claims | Keep some checks held out |

## Invariants

1. **Identity grain:** one session record identifies one agent process generation in one terminal,
   worktree, and repository at one observed interval. [Inferred domain invariant from S1-S4, R3]
2. **No invented success:** missing goal, missing telemetry, or missing verification is `not recorded`
   or `unknown`, never a neutral or positive score. [Inferred from the no-guessing and
   instrumentation standards, R5]
3. **Correctness floor:** a Blocker in correctness, security, privacy, or data integrity prevents a
   passing headline score. [Inferred from repository persona/veto standards, R5]
4. **Evidence drill-down:** every score contribution points to events, artifacts, tests, or a
   versioned rubric. [Inferred from S3, S8-S13]
5. **Score versioning:** a score always records metric schema, grader, prompt/rubric, and code
   versions. [Inferred from S1, S3, S12-S13]
6. **No self-certification:** the scored agent does not promote its own score or lesson to truth.
   [Verified repository rule: R5]
7. **Learning provenance:** each daydream item retains originating sessions, evidence, confidence,
   counter-evidence, and promotion status. [Inferred from S14, S16-S19]
8. **Fenced exclusivity:** if the watcher grants an exclusive action, the guarded resource rejects a
   stale fencing token. [Verified requirement inherited from R1 and S6-S7]
9. **Monotonic time for liveness:** heartbeat expiry uses monotonic duration; wall time is display
   metadata only. [Inferred from S6 and canonical lease literature cited by R1]
10. **Bounded history:** append-only logs have snapshots/compaction and retention without rewriting
    the meaning of surviving records. [Inferred from R1, R5]
11. **Operator-benefit boundary:** scores describe agent/model behavior for the operator's own
    improvement; they are not aggregated per person or used for personnel evaluation. [Privacy
    requirement; Flagged pending accountable-human approval]
12. **Local-only v1:** work content, prompts, code fragments, tool arguments, trajectories, scores,
    and lessons do not leave the device. [Privacy/security requirement; Flagged pending spec]
13. **Advisory graders:** untrusted trace or message-board content cannot instruct a model grader to
    write authoritative scores or promoted guidance. [Inferred from S12, S19]
14. **Retractable learning:** deletion or correction of source observations reaches derived lessons
    and can retract promoted guidance. [Privacy requirement; Flagged pending data design]

## Boundary set

- Two repositories with the same folder name. [Inferred identity boundary]
- One repository with several worktrees and several terminals. [Verified current product scenario: R4]
- One terminal that restarts and reuses an OS process identifier. [Inferred identity boundary]
- A session that never registers. [Verified current coordination failure class: R3-R5]
- A session that registers and then stops heartbeating while its process remains alive. [Verified
  lease boundary: R1, S6]
- An OTel-capable session plus an opaque session with only local events. [Verified/inferred: S1-S4]
- Missing, truncated, malformed, delayed, duplicated, or out-of-order events. [Inferred event-log
  boundary from R1, R3]
- A task whose goal changes mid-session. [Inferred scoring boundary]
- A session with no acceptance criteria or no verification path. [Verified repository gate: R5]
- A model judge that disagrees with deterministic tests or a human reviewer. [Inferred from S12-S13]
- A lesson observed once, contradicted later, or derived from untrusted content. [Verified risk:
  S18-S19]
- A session that learns how the scoring rule works and optimizes for the grade. [Verified risk: S12]
- A hostile prompt, tool result, or board message that instructs the grader to assign a score or
  promote a lesson. [Inferred injection boundary from S12, S19]
- An operator requests deletion after observations have already shaped promoted guidance. [Privacy
  requirement; Flagged]
