---
id: kb-factory-data-and-constants
title: "Review policy, thresholds and measurement definitions"
type: knowledge
status: researched
owner: "@timianmalloo"
phase: "research"
tags: [agentic-review, spec-driven-development, factory, desktop]
links:
  - { to: kb-spec-first-software-factory, rel: relates-to }
review-by: 2026-11-17
summary: "Review policy, thresholds and measurement definitions: dated evidence, confidence and limitations for the local AI-DE workbench evolution."
---

# Thresholds and measures

## What the evidence does and does not establish

**Flagged gap:** no universal safe human-review cutoff was established. A diff-size limit is an
operational bound; a model confidence filter is a workflow heuristic; code coverage measures
execution, not obligation completeness. These are not interchangeable. Anthropic internal
risk tiers are one organization's policy, not a standard to import unchanged.
[Internal practice](https://claude.dev/blog/solving-code-review-at-anthropic/),
[plugin filter](https://raw.githubusercontent.com/anthropics/claude-code/main/plugins/code-review/README.md),
[ultrareview limits](https://code.claude.com/docs/en/ultrareview).

**Verified operating snapshot:** current ultrareview docs describe default limits of 500 changed
files and 8,000 changed lines, subject to the actual refusal's effective limits. They are not
proposed review safety limits for AI-DE. No monetary estimate is used in the proposal.

## Conservative initial AI-DE policy — Inferred, to specify later

| Trigger | Human obligation |
|---|---|
| Credentials, authorization, private-data boundary, destructive migration, release authority | Named security/data/privacy/release reviewer |
| New domain invariant or architecture precedent | Domain/architecture decision with scenario evidence |
| Changed accepted criterion, ambiguous intent, contradictory baseline | Resolve the intent question before acceptance |
| Missing, stale, partial or unsupported analysis | Show incomplete assessment; retain review obligation |
| Large or coupled change | Split guided semantic units; expose uncovered diff and dependencies |
| Routine, reversible, known pattern with complete evidence | Eligible for a lighter tier only after local outcome calibration |

Cold start: keep human change acceptance. Do not make an AI score a permission grant.

## Definitions and denominator rules — proposed

| Measure | Numerator / denominator / scope | Interpretation |
|---|---|---|
| Requirement evidence coverage | Criteria with reviewed meaningful oracles / applicable criteria at one spec version | Link presence alone does not establish meaningful evidence |
| Branch coverage | Executed instrumented branches / eligible branches, exclusions retained | Not semantic completeness |
| Mutation strength | Raw killed/survived/timeout/invalid counts and selected population | Equivalent mutants require disposition; preserve runner formula |
| First-pass rate | Tests passed on attempt one / tests attempted once in window/environment | Do not include retry recovery in numerator |
| Retry-recovered rate | First-failed then recovered tests / tests eligible for retry | Classification, not a causal flake probability |
| Review attention | Active human time plus waiting time, rounds and dispositions | Pair with escapes/reverts/recovery outcomes |
| Efficiency | Wall time, CPU and cost per run plus repeated setup | Parallel durations cannot be summed as wall time |

Counts are additive only over disjoint populations; rates are non-additive. Missing denominator
or window means **not recorded**, not zero. Flake quarantine does not erase failures. Never
auto-delete tests merely because coverage overlaps: platform/boundary checks may be purposeful.
[Mutation semantics](https://stryker-mutator.io/docs/mutation-testing-elements/mutant-states-and-metrics/),
[retry semantics](https://playwright.dev/docs/test-retries).

## Local measured baseline, not factory coverage

At review baseline `9f13ca7a`, the normal App build succeeded with zero warnings/errors.
One selected Core test slice passed 57/57, with no skips. The NuGet advisory query found no
vulnerable packages in its current sources. This is not a full test/coverage run, native UI proof,
all vulnerability assurance or a factory acceptance result. Focused forensic probes separately
reproduced dirty-edit staleness and an oversized entry-point response.
