---
id: proof-recursive-surface-ownership-plan-review
title: "Recursive surface ownership: independent plan review"
type: proof-pack
status: current
owner: "@timianmalloo"
tags: [proof, review, ownership, testing, coordination]
links:
  - { to: session-contracts, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-15
summary: "Independent pre-author review clears the bounded plan and fixes the task join gate floor."
---

# Recursive surface ownership: independent plan review

## Review contract

Goal: independently decide whether the recursive surface-ownership programme is specific and
verifiable enough to dispatch one Python author. Done when the Test Architect, Simplifier,
Python Developer, SRE and Orchestrator each return a shaped verdict; the Testing Strategy union
and join commands are exact; and every condition that can block the final join is named. Product
source, ownership policy, section 2, the gate implementation, and integration are outside this
review.

Reviewed sources:

- `docs/coordination/recursive-surface-ownership.md` and
  `docs/plans/recursive-surface-ownership.md` in the Conductor tree.
- Owner decision amendment `171791fb`, which reconciles Core Ruling 113 with grouped Path-cell
  context.
- Primary request resolutions for `req-01M2JQ113TK7HGE7YKQ4CB92GA` (Ruling 113) and
  `req-01M2JQ3T5VMQWJ59Q3M0ZE420T` (Ruling 114).
- `docs/collaboration/session-contracts.md` section 2, the current Python gate, its CI calls,
  the Testing Strategy, persona cards, and the join and gate-runner implementations.

## Decision

The plan is clear for author dispatch. The frozen handoff must use both path rules:

1. A bare shorthand following an explicit path in the same grouped Path cell inherits that
   explicit path's directory.
2. A standalone bare exact filename resolves only when exactly one recursively populated file
   has that basename. Zero matches fail. Multiple matches fail and name every candidate, even
   if another declaration covers one candidate.

Repository-relative POSIX paths become the identity after resolution. Basenames never become
the ownership key. Ruling 114 assigns `src/AiDe.App/Workbench/Sessions/ProseView.cs` to Design.
Until the authoritative section-2 row lands, the new real gate is expected to remain red on that
file. That red must be reported and may not be converted to `UNASSIGNED`, waived, or hidden.

### Test Architect

```text
PERSONA: test-architect   MODE: Adversary   TIER: T1
VERDICT: PASS
FINDINGS:
  - [Minor] (Verified) The existing gate proves only its flat 13-file population; the independent recursive census is 17.  evidence: current gate reports 13/13 while the recursive filesystem listing names 17  fix: execute the plan's recursive red-first fixtures and inspect the final real population
CLEARS-THE-VETO: yes — the plan traces each promised behavior to an oracle, requires red-before-green and a populated implementation Proof Pack, and preserves the mandatory integrated gate; final implementation acceptance remains pending those receipts.
RESIDUAL RISK: plan approval proves the verification path, not the unwritten implementation or the not-yet-landed ProseView section-2 row.
```

### Simplifier

```text
PERSONA: the-simplifier   MODE: Adversary   TIER: T1
VERDICT: PASS
FINDINGS:
  - [Nit] (Verified) Lean already. Ship.  evidence: one existing stdlib script, its existing self-test entry, no dependency, no product layer, one serial author  fix: none
CLEARS-THE-VETO: yes — every remaining boundary exists to separate an ownership decision, authorship, independent review, or the mandatory join.
RESIDUAL RISK: the bounded Markdown grammar remains custom code because no lower-rung dependency is smaller or safer for this one register.
net: -0 lines possible
```

### Python Developer

```text
PERSONA: python-developer   MODE: Adversary   TIER: T1
VERDICT: PASS-WITH-CONDITIONS
FINDINGS:
  - [Major] (Inferred) A whole-string fnmatch implementation would let `*` cross `/` and violate the declared grammar.  evidence: the plan requires segment-local `*`  fix: match path segments independently and reserve only trailing `/**` for recursive directory coverage
  - [Major] (Inferred) Basename-keyed intermediate state would recreate the alias defect after discovery becomes recursive.  evidence: Ruling 113 and the duplicate-basename oracle  fix: normalize once to sorted root-relative POSIX paths and key all owner, exception, conflict and diagnostic state by that identity
CLEARS-THE-VETO: yes for author dispatch — final clearance requires the frozen code to satisfy both conditions and the independent implementation review to inspect them.
RESIDUAL RISK: parser boundary behavior remains unobserved until the candidate exists.
```

### SRE

```text
PERSONA: sre-diagnostician   MODE: Adversary   TIER: T1
VERDICT: PASS
FINDINGS:
  - [Minor] (Verified) No production runtime or network boundary is added; the operator-facing measurements are population, assignment, exception and failure cardinalities plus per-gate/join duration from the existing runners.  evidence: current CLI output and conductor-join/run-verify-gates implementations  fix: preserve those emitted counts and inspect the final output
CLEARS-THE-VETO: yes — the serial implementation spine has no shared-file contention, transport retry is capped at one, partial review blocks the join, and the loop variant is unresolved evidence decreasing to zero.
RESIDUAL RISK: runtime cost of the recursive scan is unmeasured until candidate execution; no performance claim is made.
```

### Orchestrator

```text
PERSONA: orchestrator   MODE: Adversary   TIER: T1
VERDICT: PASS
FINDINGS:
  - [Minor] (Verified) The first draft lacked an executable task join contract and carried superseded bare-name language; both are corrected in the Conductor tree.  evidence: recursive-surface-ownership-join.json and order-of-operations handoff to 171791f/Rulings 113/114  fix: freeze those exact artifacts in the author dispatch
CLEARS-THE-VETO: yes — goal, exclusions, one-author ownership, hard gates, join ordering, bounded failure behavior, and re-plan triggers are explicit.
RESIDUAL RISK: Atlas can add `Workbench/Understanding/*View.cs`; the second lander must rebase, rerun the census and reconcile new identities before joining.
```

## Testing Strategy trigger union

| Trigger | Directive | Required evidence and failure oracle |
|---|---|---|
| all tests | D0 | Deterministic, hermetic, straight-line meaningful assertions; no clock, sleep, external network, conditional assertion, or machine-local identity. |
| T1: discovery, matching, owner-set and exception logic | D1 | Boundary examples plus mutations that remove recursion, collapse identity to basename, admit prose, suppress ambiguity/conflict, or retain stale exceptions. Each mutant must make the corresponding test red. |
| T2: Markdown/path parser and wide path population | D2 | Bounded generated permutations for declaration order, additions/removals, exact-path identity, segment boundaries and same-owner deduplication. Persist every found counterexample as a named regression case. |
| T4: recursive filesystem behavior | D4 | Real `TemporaryDirectory` trees and real files. Cover nested matches, nonmatching suffixes, zero population, missing contract/section, deleted exception target and duplicate basenames in different directories. No filesystem mock. |
| T6: CLI consumed by CI | D5-provider | Preserve `.github/workflows/build.yml` calls to the bare CLI and `--self-test`; exercise exit 0/1 and inspect stable diagnostic/cardinality content. There is no separately published consumer contract beyond those checked-in invocations. |

The boundary matrix must also cover: Path-cell-only parsing; owner context ending at a non-owner
heading; grouped same-cell shorthand; standalone unique, zero-match and ambiguous bare names;
qualified exact paths; segment-local `*`; trailing `/**`; unsupported, absolute and traversing
paths; malformed relevant rows; missing owner context; same-owner overlap; cross-owner conflict;
exact nonblank exceptions; stale missing/assigned exceptions; and case-preserving sorted output.

T3, T5, T7-T14 are not triggered: the slice adds no namespace/layer, remote service, payload
schema, mock boundary, model call, MCP surface, structured model output, agent workflow, generated
content, prompt, skill, or model tool description.

## Accepted join contract

`docs/coordination/recursive-surface-ownership-join.json` is accepted with these exact commands:

```text
checks:
  python -m py_compile tools/verify-surface-ownership.py
  python tools/verify-surface-ownership.py --self-test
  python tools/verify-surface-ownership.py
  python tools/verify-gate-self-tests.py
  python tools/verify-defect-register.py
recount: []
regenerate:
  python tools/regenerate-derived.py
gates:
  python tools/verify-surface-ownership.py
  python tools/verify-surface-ownership.py --self-test
  python tools/verify-gate-self-tests.py
  python tools/verify-defect-register.py
  python tools/verify-audit-log.py
  python docs/ai-forward-pack/scripts/docs-graph.py validate
  python tools/verify-derived-views.py
  python tools/run-verify-gates.py
build: []
```

Invoke `conductor-join.py` with
`--join docs/coordination/recursive-surface-ownership-join.json --no-push`. Do not use
`--docs-only`. Empty .NET recount and build arrays are scoped omissions: this slice changes no
.NET product or test source. The final `tools/run-verify-gates.py` is still mandatory and cannot
be replaced by the selected checks. A baseline red stops the join; Ruling 112 does not authorize
hiding it.

## Observed baseline evidence

All commands ran in the independent reviewer tree on 2026-09-15:

| Command | Observed state |
|---|---|
| `python tools/verify-surface-ownership.py` | PASS; 13 surfaces, 13 assigned, 0 pending |
| `python tools/verify-surface-ownership.py --self-test` | PASS; planted orphan was reported |
| `python tools/verify-gate-self-tests.py` | PASS; 36 gates, 10 frozen without self-test, no growth |
| `python tools/verify-gate-self-tests.py --self-test` | PASS; new debt and stale debt both rejected |
| `python tools/verify-defect-register.py` | PASS; 225 classes, all citations resolve |
| `python tools/verify-audit-log.py` | PASS; 793 entries, no duplicate IDs |
| `python docs/ai-forward-pack/scripts/docs-graph.py validate` | PASS; 476 artifacts, 0 defects; 66 existing suggestions |
| `python tools/verify-derived-views.py` | PASS; 4 generated views match |
| `python docs/ai-forward-pack/scripts/conductor-join.py --self-test` | PASS; clean join completes and a conflict marker stops at step 3 |

These results establish the existing controls and join mechanism. They do not promote the future
recursive implementation to Verified.

## Final veto-clear predicate

Final implementation acceptance requires all of the following:

1. The author records the old implementation red against every new promised behavior before the
   fix, then records the candidate green against the same cases.
2. The independent reviewer observes the candidate reject each named mutation and confirms the
   CLI exit status, diagnostics and actual recursive population.
3. Ruling 114's ProseView assignment is present in section 2; no temporary exception conceals it.
4. The frozen candidate includes no product, policy, workflow or unrelated tooling change.
5. The task join runs the accepted JSON with `--no-push`; all selected checks and the full gate
   runner pass, and the output is read rather than inferred from exit status.
6. A populated implementation Proof Pack, audit entry, DC-118 instance and mitigation record are
   attached. Concurrent Atlas changes are reconciled by the second lander.

## Cost record

Plan review used one reviewer, no subdelegation, and no retry loop. Audit entry
`al-01M2JQPXCYB8G0WWR0008YDPX7` measured 443 seconds from grounding through audit close and
recorded 19/25 main-line tool calls. Closing derivation and commit bookkeeping brought the
transcript total to 25/25 tool calls. Token usage is not exposed by the harness and is recorded as
`not recorded`; no estimate is substituted.
