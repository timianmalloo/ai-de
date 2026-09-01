---
id: adr-0019-advisory-evaluator-calibration
title: "ADR-0019 — Advisory dimensions and the leaderboard require calibrated, held-out-validated evaluators"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "discovery"
tags: [architecture, loomkeeper, scoring, evaluation, calibration, leaderboard, ai-systems]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: kb-agentic-session-observability, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  A model-graded dimension contributes score points only after its evaluator version passes stability
  (>=95% same 0-4 band over 20 runs) and human agreement (quadratic weighted kappa >=0.75) on separate
  versioned corpora; leaderboard ranks are scoped to one calibrated task class and score schema, and
  anti-Goodhart counter-metrics gate whether a score rise counts as improvement.
---

# ADR-0019: Advisory evaluator qualification and task-class calibration

- **Status:** Accepted (methodology fixed; calibration itself is a Phase-4 deliverable)
- **Date:** 2026-08-30
- **Deciders:** Product owner, AI Systems, Test Architect, Data & Persistence
- **Context spec/architecture:** docs/architecture/loomkeeper.md, docs/specs/agentic-watcher-substrate.md

## Context

The Weave Score's qualitative dimensions and the harness/model leaderboard are only meaningful if the
grader is calibrated and comparisons are scoped. An uncalibrated advisory score, or a rank that mixes
task classes or rubric versions, is worse than none: it invites reward hacking and Goodhart drift
(KB `agentic-session-observability`: MAST taxonomy, reward-hacking obfuscation, AgentPoison). The spec
makes model judgments advisory and never authoritative; this ADR fixes *how* an advisory dimension
earns the right to contribute points and how ranks stay comparable.

## Decision

- **Separate, versioned corpora:** a calibration corpus and an independently-adjudicated held-out
  validation corpus, maintained as first-class contract artifacts.
- **Two qualification gates before an advisory dimension contributes score points:** (a) **stability**
  — 20 repeated evaluations stay in the same discrete 0–4 band ≥95% of the time and never differ by
  more than one band; (b) **human agreement** — quadratic weighted kappa ≥0.75 on the held-out corpus.
  A dimension that fails either gate stays visible but excluded (Advisory / Not Scored).
- **Scoped comparability:** comparisons and leaderboard ranks are permitted only within the same
  calibrated task class and score schema version; a cell below the cohort minimum (five independent
  episodes) or one that proxies a single human renders **Not Comparable**, never a rank; incompatible
  versions are segmented, never trended into one ranking.
- **Re-qualification on any change:** an evaluator model, prompt, rubric, schema, or corpus change
  must re-pass stability, human agreement, prompt-injection invariance, and held-out outcome checks
  before it can contribute points.
- **Anti-Goodhart improvement gate:** a visible score rise is accepted as improvement only if held-out
  outcome integrity, regression rate, rework, and dispute-overturn are no worse. The per-turn standing
  therefore exposes evidence and trend, never a single optimizable scalar.
- **Model output cannot raise a deterministic failed dimension or clear a hard floor** (LOA P5).

## Alternatives considered

- **Trust the grader's self-reported confidence.** Rejected: uncalibrated self-report is unreliable;
  qualify against held-out human labels instead.
- **One global leaderboard across task classes.** Rejected: apples-to-oranges; ranks are scoped to a
  calibrated task class and score schema version.
- **Expose the full scoring target to the agent to maximise the number.** Rejected: invites Goodhart
  gaming; standing exposes evidence and trend, and improvement is gated by outcome counter-metrics.

## Consequences

- **Positive:** advisory dimensions and ranks are trustworthy or explicitly excluded; the score
  resists gaming; comparisons are honest about task class and version.
- **Negative / accepted trade-off:** some task classes may never reach QWK ≥0.75 and stay
  Advisory/Not Scored — an accepted degradation; building and maintaining two corpora is real ongoing
  cost.
- **Follow-ups / new risks:** the calibration harness, the corpora, and the qualification thresholds'
  statistical power are a **Phase-4 deliverable with its own Proof Pack**; thresholds are initial
  safety floors that may tighten, never silently relax.

## Evidence

KB `agentic-session-observability` (MAST κ=0.88 taxonomy; reward-hacking obfuscation 2503.11926;
ACE 2510.04618; AgentPoison 2407.12784) [Verified by fetch]. Whether a real task class reaches the
stability and kappa thresholds is **not established** [Flagged — Phase-4 calibration harness].
