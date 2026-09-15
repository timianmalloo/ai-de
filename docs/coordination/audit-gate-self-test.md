---
id: coord-audit-gate-self-test
title: "Audit verifier self-test coordination"
type: doc
status: complete
owner: "@timianmalloo"
tags: [coordination, testing]
links:
  - { to: plan-audit-gate-self-test, rel: relates-to }
  - { to: session-contracts, rel: depends-on }
review-by: 2026-12-15
summary: "Exact tooling handoff, one author and independent review for existing audit verifier semantics."
---

# Layer state

## Completion — 2026-09-15

Qualified at6fab141625fbe110b8b35ca710f03d793a560b66:38/38 required gates,11 self-test cases,7 named mutants and ratchet9. Independent Test/Python/Security/SRE/Simplifier PASS7094c147 on repaired author1581441d. Core Ruling119 is included verbatim through authorized8b164757. Final records and serialized candidate/base handoff complete this programme; no main push by Codex. See docs/proof/audit-gate-self-test.md for proof hashes, audit provenance and retained historical findings. Exact editing leases are released after each checkpoint; trees are retained for evidence/integration.

Base bbd1bece; own conductor worktree. Pack revision70: 10 PASS, 3 WARN, 0 FAIL. Driver registration effective; 11 patterns; four shared regen markers owed. Codex absolute path boundaries are observed-only; separate Git trees and commit hook observed. No installation or shared-primary regeneration.

# Artifact classes

| path | class | mechanism | coordination needed |
|---|---|---|---|
| tools/verify-audit-log.py | authored | narrow Core grant and short lease | yes |
| tools/verify-gate-self-tests.py | authored | exact frozen entry only | yes |
| docs/plans/audit-gate-self-test.md; programme proof/coord/owner records | authored | one writer per file, short lease | yes |
| docs/lessons/defect-classes.md | register | append instance under existing class | no lease; serialize join |
| docs/audit/*.jsonl | register | official writer, prescribed union | no lease |
| docs/docs-index.js; audit-data.js; generated docs | derived | own-tree regeneration after audits | no lease |
| site/index.html; site/collaboration.html; site/model.html | authored with generated figures | exact figure regeneration and short leases | yes |

# Tracks

| track | owns authored | depends on | tier | fan-out cap | budget | exit evidence | harness |
|---|---|---|---|---|---|---|---|
| Conductor | programme plan/coord/proof | peer receipts | T1 | 4 total | 50 calls | inspected join and handoff | Astra, own tree |
| Owner | decision receipt | grounded semantics | T1 | 0 | 10 calls/12min/12k tokens | confirmed scope; no implementation | Astra, own tree |
| Author | two exact tools; author proof | Core grant, Owner and plan PASS | T1 | 0 | 20 calls/20min/20k tokens | red/green, commits and residual risks | Sol high, own tree |
| Reviewer | review proof | plan then frozen author | T1 | 0 | 12 calls/12min/12k tokens per review | independent persona verdicts and observed mutants | Sol high, own tree |

# Serial spine

Grant/design -> author -> independent review -> scripted join. Conductor coordinates; it does not implement the tooling. Test Architect hard veto and Simplifier/Tech Lead scope decision precede author. No concurrent writes to registers or generated artifacts inside one tree.

# Seams

Core request req-01M2K3PZY3J1DQY7HZN443E81C names the exact two tools; section2 remains sole authority. No grant inferred from prior Ruling113. Atlas owns its recovery/desktop, Grok its Understanding Views programme. No expected shared product guard overlap because no src/tests input changes. Gate population remains tools/verify-*.py plus mutation-replay.py at top level as declared; this task changes one frozen entry, not population policy.

# Struck tracks

Separate fixture authors rejected: coupled single file. Broad debt sweep implementation rejected: ten gates are not one assignment. Core policy hardening rejected unless explicitly escalated and granted.

# Order of operations

Ground, get grant, settle design, independent plan review, dispatch author, inspect return, independent implementation review, join through conductor-join.py using exact contract, audit then regenerate, inspect state, commit and hand candidate/base to Core. Retain trees until integration; cleanup reports only, never deletes peer work.
