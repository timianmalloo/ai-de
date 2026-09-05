---
id: design-watcher-advisory-evaluator
title: "Loomkeeper - Local Advisory Evaluator & Egress Guard (connective 3)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, advisory, evaluator, egress, credential, adr-0018 credential-backed-grading-egress, phase-4]
links:
  - { to: design-watcher-advisory-grader, rel: depends-on }
  - { to: adr-0024-credential-backed-grading-egress, rel: implements }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Implement the IAdvisoryEvaluator seam two ways: a deterministic LOCAL heuristic evaluator that scores
  the two advisory dimensions from a quarantined evidence token list with a conservative default (needs
  no model, credential, or egress - the safe smoke-test default), and an EgressGuardedAdvisoryEvaluator
  that enforces default-deny egress (LK-0003) THEN a present credential (LK-0002) before any egressing
  cloud judge can run (ADR-0024 credential-backed-grading-egress), never calling the inner evaluator when either check fails. The real
  cloud model call stays a seam behind the guard - a local smoke test uses the local evaluator.
---

# Design: Local Advisory Evaluator & Egress Guard (connective 3)

## 1. Problem & scope

Slice 7 built the advisory calibration gates, the gated fold, and the `IAdvisoryEvaluator` seam, but
shipped **no implementation** - the seam was exercised only with fixture evaluators. So the advisory
dimensions (Evidence discipline, Solution economy) could never actually be judged, and the ADR-0024 credential-backed-grading-egress
credential/egress boundary the seam's doc promised was not enforced anywhere. This slice provides a
real, safe, local implementation and the egress boundary.

**In scope:** a deterministic `LocalHeuristicAdvisoryEvaluator` (no egress, no credential); the
`EgressGuardedAdvisoryEvaluator` decorator enforcing ADR-0024 credential-backed-grading-egress; the `IAdvisoryCredentialSource`
presence-check seam + a `NoCredential` default; a small deterministic evidence-token parser; the
negative egress/credential tests. **Out of scope:** an actual cloud model call (genuinely blocked on
operator-provided credentials + a chosen provider - it remains the seam behind the guard); disputes
(conn-4).

## 2. Two evaluators, one seam

`IAdvisoryEvaluator.Evaluate(dimension, episode, evidence) -> AdvisoryAssessment` is implemented twice:

**LocalHeuristicAdvisoryEvaluator (`local-heuristic/1`).** The safe default. It grounds ONLY on the
quarantined `evidence` string the caller composes from deterministic signals - a token list like
`"verification=executed; coverage=9/10; actions_after_done=0; premature=false; reuse=high"` - and maps
it to a 0-4 rubric by fixed rules. It needs no model, no credential and no egress, so a local smoke test
can see the advisory path work end to end. Because it is deterministic its `EvaluatorStability` trivially
passes, but it still folds into Weave points only after the ADR-0019 advisory-evaluator-calibration calibration gates qualify its
`(version, taskClass, schemaVersion)` triple (slice 7) - it is a transparent proxy an operator can
inspect, not a licence to score advisory dimensions unbounded. It judges only the two advisory
dimensions; a deterministic dimension is refused (LK-0002, rule 8).

**No guessing (NG1).** An absent or malformed token scores **conservatively** - `verification` absent is
"not executed" (0), `coverage` absent is 0.0, `actions_after_done` absent is 0 - so a missing signal can
only lower a score, never raise it. There is no optimistic default anywhere.

## 3. The egress + credential guard (ADR-0024 credential-backed-grading-egress)

`EgressGuardedAdvisoryEvaluator(inner, gate, egressPathId, credentials)` is the boundary a real cloud
judge sits behind. On `Evaluate` it enforces, **in order**:

1. **Egress first** - `EgressGate.Decide(egressPathId)` must be `Allowed` (an explicit per-path opt-in),
   else `WatcherException(EgressDenied, LK-0003)`. Default-deny: no opt-in means blocked. Egress is
   checked *before* the credential so a missing opt-in can never be masked by a present credential.
2. **Credential present** - `IAdvisoryCredentialSource.HasCredential`, else
   `WatcherException(InvalidBinding, LK-0002)`. The source authorises on **presence**; the secret itself
   is never in the watcher store (architecture §4) - it is resolved at the call by the credential-backed
   transport.

Only when both hold does the inner evaluator run. The `LocalHeuristicAdvisoryEvaluator` needs no guard
because it never egresses.

## 4. Failure modes & dispositions

| Failure mode | Disposition |
|---|---|
| Egress not opted in | `EgressDenied` (LK-0003); inner never called (tested) |
| Egress opted in but no credential | `InvalidBinding` (LK-0002); inner never called (tested) |
| Opt-in later revoked | Returns to denied (tested) |
| Absent/malformed evidence token | Conservative default (0/false/null), never optimistic (tested) |
| A deterministic dimension asked of the local evaluator | Refused, `InvalidBinding` (rule 8, tested) |
| Non-determinism in the local evaluator | Impossible by construction; stability over 20 repeats asserted |

## 5. Test plan

- `AdvisoryEvaluatorsTests` (15): local scoring (top/zero/partial/conservative-default/lean/wasteful),
  refusal of a deterministic dimension (Theory x4), determinism/stability; guard blocked-egress denial,
  no-credential denial, revoked-path denial, and the allowed+credentialed delegation + version delegation.
- The egress-first ordering is mutation-verified (neutralising the egress check reds the blocked-egress test).
