---
id: kb-factory-references
title: "Reference contracts for intent and evidence"
type: knowledge
status: researched
owner: "@timianmalloo"
phase: "research"
tags: [agentic-review, spec-driven-development, factory, desktop]
links:
  - { to: kb-spec-first-software-factory, rel: relates-to }
review-by: 2026-11-17
summary: "Reference contracts for intent and evidence: dated evidence, confidence and limitations for the local AI-DE workbench evolution."
---

# Reference model — proposed, Inferred

Do not store a second definition of a quantity already derivable from facts. The following is a
research-grounded proposal, not a schema migration or accepted architecture.

| Object / grain | Required identity and evidence | Invariant |
|---|---|---|
| Work item / one accepted outcome | Workspace, outcome, exact spec revision | Accepted intent is distinct from execution permission |
| Criterion revision / one obligation meaning | Stable criterion id + content version + authority | Meaning changes append a revision; history is preserved |
| Change set / one base→head/content comparison | Repo/worktree/base/head/uncommitted content hash | A commit id alone does not identify dirty content |
| Evidence link / one producer's versioned relation | Criterion→code/test/run, producer/version, confidence, location | An inferred link is not a proven oracle |
| Test attempt / one test in one environment | Run/execution identity, test identity, attempt, runner/version, environment/window, result, duration | Retry recovery never overwrites first failure |
| Assessment / one analyzer at one baseline | Scope, inspected/omitted files, failures, cost, findings | Partial analysis cannot become a clean whole-scope result |
| Review decision / one actor's disposition | Exact head/spec/architecture/policy version, reason | Changed relevant baseline makes the decision stale |

Version pinning and retained snapshots have documented prior art in
[CodeRabbit snapshots](https://docs.coderabbit.ai/change-stack/snapshots) and
[Copilot approvals](https://docs.github.com/en/copilot/concepts/agent-capabilities/agent-powered-workflows-and-operations/code-review).
Explicit spec authority is grounded in [Spec Kit persistence](https://github.github.com/spec-kit/concepts/spec-persistence.html).

## Baseline and drift disposition

Expected artifact may be accepted spec, architectural constraint, selected mockup, or exploratory
PoC. Preserve its authority. Observed artifact may be parsed code/schema, a run or rendered state.
An agent comparison records the gap, never silently replaces expected with observed. Dispositions:
implementation defect; approved requirement change; obsolete baseline; missing test; ambiguous
intent; incomplete evidence. These must lead to distinct next actions.

## Security, privacy and local desktop boundary

The source store and normal execution stay local. Optional provider inference and cloud review are
explicit egress boundaries, including upload scope, policy, cost and retained evidence. Treat
repository docs, diffs, test logs and external reviewer text as untrusted input, not instructions.
Approve intent, authorize an action and approve a change in separate controls. A read-only plan
does not imply shell/tool containment. [Grok caveat](https://docs.x.ai/build/features/plan-mode),
[ultrareview cloud boundary](https://code.claude.com/docs/en/ultrareview),
[VS Code planning](https://code.visualstudio.com/docs/agents/run/planning).

## Operator questions and emitting sources — proposed

How long/how costly? Run timestamps, token/cost provider receipts, plus unknown when absent.
What was inspected? Analyzer manifest with scope, omissions and errors.
Why is a human needed? Policy evaluation plus finding/decision record.
What changed since approval? Baseline identity comparison.
Was behavior proved? Criterion, assertion and executed-run records.
How often does it fail? First-attempt result history with environment/window denominators.
Never synthesize a plausible zero for a missing measurement.
