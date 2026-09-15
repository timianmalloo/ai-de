---
id: plan-audit-gate-self-test
title: "Audit verifier self-test: bounded execution graph"
type: doc
status: in-progress
owner: "@timianmalloo"
tags: [plan, testing, coordination]
links:
  - { to: session-contracts, rel: depends-on }
  - { to: coord-audit-gate-self-test, rel: relates-to }
review-by: 2026-12-15
summary: "One tooling author followed by independent adversarial verification, with no audit policy expansion."
---

# Goal and graph

Goal: add meaningful self-tests to the audit-log verifier without changing its policy.
Done when: red-first fixtures, independent review, applicable checks and committed evidence support a Core handoff.
Not in scope: product, allocator, merge framework, live fixture data, ownership decisions or main integration.
Tier T1; fan-out cap 4 including Astra Owner and Conductor.

| Node | Dependency | Exit oracle |
|---|---|---|
| G Ground and grant | none | Read actual gate, register, live sessions and supported scripts; exact Core authority |
| D Design and independent plan review | G | Owner scope decision; Test Architect PASS, Simplifier PASS; failure clauses fixed |
| A One author | D | Hermetic fixture suite runs real gate, red mutants rejected, valid cases accepted |
| R Independent implementation review | A | Test, Python, SRE and Simplifier verdicts; no author self-clear |
| J Scripted join | R | Small checks and required full gate line pass on integrated commit |
| C Evidence and handoff | J | Committed Proof Pack, audit, released leases, candidate/base sent to Core |

```mermaid
flowchart LR
 G --> D --> A --> R --> J --> C
```

Optimize-graph runs once for this programme. Naive plan: several authors split fixture categories, then reconcile one file. Optimized: one author owns both tightly coupled tooling files; independent reviewers run after the freeze. No speedup is claimed. Real decision/data dependencies prevent widening the implementation spine. Gates cannot be removed for economy. At most four resident agents, normally three active. One transient transport retry; a red result is diagnosis input, never a retry trigger. Variant: outstanding acceptance obligations; if two passes do not decrease them, investigate and re-plan, not relax the floor. Partial evidence blocks the join.

Budget estimates (Inferred): Conductor 50 calls, author 20 calls/20 minutes/20k tokens, reviewer 12 calls/12 minutes/12k tokens, Owner 10 calls/12 minutes/12k tokens. Record overruns without enlarging estimates. Astra handles scope and orchestration; Sol high handles the small Python author and adversarial review. No smaller model is needed for a separate deterministic track: provisioning it adds a boundary without independent work. Mechanism: separate Git worktrees and received receipts observed; absolute-path discipline is observed-only, not a filesystem sandbox. Author/reviewer are serial.

## Specification and design

Existing functional authority: tools/verify-audit-log.py module contract and actual check/main functions; frozen ratchet in tools/verify-gate-self-tests.py; DC-013/DC-026/DC-104 in docs/lessons/defect-classes.md. Reuse audit-and-change-log architecture (append-only JSONL, derived viewer, official writer). No new domain model, representation, ADR, allocator or integrity policy.

Functional delta: --self-test proves existing checks on isolated fixture repositories; normal invocation preserves output, missing-file behavior and accepted ID forms. UX: CLI success/failure and case counts, actionable self-test diagnostics; existing no-argument and explicit-path modes preserved. Visual UI: N/A, no visual surface. Specification and define-architecture are satisfied by cited contracts; design-slice adds this fixture design and reviewer-reviewed acceptance matrix.

Data model: one line is one audit/change record; ID is record identity, not a counter to reallocate here. The per-log invariant is uniqueness plus preservation of committed IDs. Fixture records are disposable test data, not a second audit store. No measures or history changes.

Affected surface list: CLI selection -> fixture Git repository and JSONL -> real check/committed_ids/main -> findings and exit status -> self-test verdict/counts -> frozen ratchet -> existing gate invocation -> proof/audit/derived outputs.

Fixtures: duplicate IDs; deletion of committed record; syntactically malformed JSON; object missing ID; invalid ID; valid sequential and ULID records; legitimate append. Each negative must fail for the named finding, not merely throw or return any nonzero. Exercise default logs and explicit paths, missing optional log behavior, real HEAD comparison. Test positive/negative permutation and append invariants where useful. Capture unrelated discovered defects for Owner before changing normal behavior. Real temporary Git repositories preferred to mocks; no global Git config or network. Mutation proof must detect disabled duplicate/deletion/format/JSON/ID guards and an always-green CLI. Narrow stdlib changes only.

Testing union: D0 hygiene, D1 mutation, D2 validator invariants and D4 real filesystem/Git boundary. D7 only if a boundary substitute is introduced, in which case require fidelity or replace it with real Git. No product build input changes; reused runtime receipts require current source/tests/build-input and hash checks, never a fresh-run claim. Full required gate line still runs at join.

Class -> sweep -> derive -> prevent: DC-104 already names controls without executable self-proof; ten gates are frozen. This slice repairs one, not all ten. Derive assertions from existing policy, demonstrate guard mutations fail, then remove only this name from the ratchet. Other frozen names remain declared debt. Required record of this instance goes through the append-only lesson convention.

## Planned versus actual

Grounding observed 80 worktrees, effective merge drivers, 11 registry patterns and four shared regeneration markers owed; pack doctor 10 PASS/3 WARN/0 FAIL. Do not run shared producers over primary. Use the own-tree regeneration orchestrator as in the previous approved programme. No cleanup or installation is needed.

Core granted request req-01M2K3PZY3J1DQY7HZN443E81C under Ruling 119 (Core commit 8b164757). Both tooling edits must be in ONE commit. Valid ULIDs must be minted by importing audit-log.py next_id using the merge-append-only-log.py allocator pattern; planted defects remain literals. No normal refusal/acceptance branch changes are authorized. Owner A1 confirms T1 and real temporary Git/CLI fixtures. Preserve optional-file absence, including a committed file disappearing; preserve current non-object JSON limitation. Those gaps are findings, not this task's policy expansion. Plan review precedes author dispatch.
