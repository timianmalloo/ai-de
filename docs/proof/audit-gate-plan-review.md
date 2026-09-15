---
id: proof-audit-gate-plan-review
title: "Independent plan review: audit gate self-test"
type: proof-pack
status: current
owner: "@codex-sol-review"
tags: [proof, audit, testing, review]
links:
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-15
summary: "Initial plan block and superseding PASS after the executable red-first and Ruling 119 constraints were added."
---

# Independent plan review: audit gate self-test

## Superseding re-review — PASS

The initial review below BLOCKED author dispatch because the pre-change
`--self-test` command was false-green and the plan did not yet carry Ruling 119's
exact boundaries. Conductor commits `a95a8182` and `11d9f8a7` supersede those
gaps. The latter explicitly identifies the false-green baseline, authors the
intended harness first in a disposable copy, applies five guard mutants plus
always-green and always-fail CLI mutants at exactly-once sites, requires named
case failures before production insertion, and preserves the production
self-test dispatch while mutating normal verification. It also replaces the
open-ended permutation language with fixed cases, including reversed valid
order and one unique append.

Owner A2 approves that red-first sequence with pristine baseline/harness hashes,
byte-identical verifier copies, exact mutation application evidence, and the
rule that a traceback alone is not a discriminating failure. Ruling 119 remains
the authority: valid ULIDs come from the canonical `audit-log.py` `next_id`
import; planted defects alone are literals; the identifier regex and normal
refusal/acceptance branches remain unchanged; both tooling edits land in one
commit.

Final plan verdicts:

- **Test Architect: PASS.** The red-first sequence is now executable and
  falsifiable. D0/D1/D2/D4 oracles distinguish each required refusal and
  acceptance from generic nonzero, traceback, always-green, and always-fail
  behavior.
- **The Simplifier: PASS.** The plan uses a fixed matrix, one disposable-copy
  seam, stdlib, real Git, and no new framework or dependency.
- **Python Developer: PASS.** Existing positional CLI behavior is frozen; the
  self-test route, canonical dynamic import, copied verifier, subprocess status,
  and diagnostics have explicit implementation-review checkpoints.
- **SRE & Systems Diagnostician: PASS.** Fixtures are local temporary Git
  repositories with command arrays, local identity, disabled inherited
  signing/hooks, timeouts, and cleanup on failure paths. Git/setup failure is a
  failed self-test, never a skip.
- **Orchestrator: PASS.** The current grant, two-file/one-commit boundary,
  Owner-approved red sequence, serial author/reviewer spine, join gates, and
  escalation boundary are explicit.

All earlier BLOCK findings below remain historical evidence of why `11d9f8a7`
was required. They no longer veto author dispatch.

Residual implementation risk: independent frozen-candidate review must verify
the byte-identical copy hashes, allocator-produced ULID, exact parser
compatibility, seven applied mutants, case-specific output, cleanup behavior,
one frozen-entry deletion, and absence of normal-policy hunks. The valid-JSON
primitive/array exception stays outside this grant.

## Inputs and observed baseline

Reviewed the conductor plan and coordination record at base `bbd1bece`, Owner
receipt `075c73a`, Ruling 119 in resolved request
`req-01M2K3PZY3J1DQY7HZN443E81C`, and the current implementations of
`tools/verify-audit-log.py`, `tools/verify-gate-self-tests.py`, and the canonical
allocator import in `tools/merge-append-only-log.py:79-88`.

The existing verifier has no argument parser. It treats every argument as a log
path. Observed command:

```text
python tools/verify-audit-log.py --self-test
--self-test              (absent)
verify-audit-log: OK — 0 entr(ies) across 1 log(s); every id is claimed by exactly one entry.
exit 0
```

Therefore the plan's current instruction to observe red before adding the route
has no executable red oracle. This success is the existing missing-file policy,
not a self-test result.

## Required plan corrections before author dispatch

1. Supersede the stale “grant pending” state. Quote Ruling 119's exact authority:
   only `tools/verify-audit-log.py` plus removal of its one frozen-set entry in
   `tools/verify-gate-self-tests.py`, in one commit.
2. State the escalation boundary: no hunk may change the existing identifier
   regex or any normal refusal/acceptance branch outside the `--self-test` route
   without a new Owner decision. Existing no-argument and positional-file forms
   must remain byte-behavior compatible.
3. Resolve red-first into an executable sequence approved by the Owner. The
   current flag invocation cannot be called red. A sufficient sequence is to
   first build the intended harness in a temporary copy, inject each named guard
   mutant into a separate copied verifier, and observe the harness exit nonzero
   before copying the reviewed harness into production. Positive cases must kill
   an always-fail verifier mutant; negative cases must kill disabled duplicate,
   deletion, malformed-JSON, missing-id, and invalid-format guards. Record that
   each mutation applied and the specific failed case.
4. Require the valid ULID fixture to load `audit-log.py` with
   `importlib.util.spec_from_file_location`, `module_from_spec`, and
   `exec_module`, exactly as `merge-append-only-log.py:79-88`, then call its
   `next_id`. Hand-written literals are permitted only for planted defects.
   Do not derive a “valid ULID” from the verifier regex.
5. Require a byte-identical verifier copy in each clean temporary Git fixture
   before any explicitly recorded mutant is applied. Invoke it through
   `sys.executable`; assert exit status and the identifying diagnostic for every
   negative, and exit status plus count/absence of findings for every positive.
   Cover both default-log discovery and explicit positional paths.
6. Keep each case isolated. Configure local-only Git identity, disable inherited
   signing/hooks for fixture commits, use argument arrays without shell
   evaluation, and guarantee cleanup on setup, assertion, timeout, and subprocess
   failures. Never read or write live `docs/audit/*` fixture data.
7. Replace the open-ended “permutation and append invariants where useful” and
   generated-input invitation with the fixed Ruling 119 matrix. D2 is satisfied
   by boundary examples for the two accepted identifier grammars and invalid
   neighbors; extra generation is outside this slice.

The known valid-JSON primitive/array `.get` exception remains a separately
recorded runtime risk. This grant neither tests it as malformed JSON nor permits
changing its normal branch.

## Persona verdicts

PERSONA: Test Architect  
MODE: Adversary  
TIER: T1  
VERDICT: BLOCK

FINDINGS:

- [Blocker] (Verified) The declared pre-change `--self-test` oracle exits 0 on an
  absent pseudo-path. The plan cannot satisfy red-first as written.
  Evidence: observed command and output above.
  Fix: adopt and Owner-approve the temporary-harness/mutant red sequence in item
  3, or provide another executable oracle that demonstrably fails before the
  production route exists.
- [Blocker] (Verified) The plan predates Ruling 119 and omits its allocator-source
  and no-normal-branch-edit conditions.
  Evidence: plan/coordination text compared with the resolved request.
  Fix: incorporate items 1, 2, and 4 verbatim into the author contract.

CLEARS-THE-VETO: no — both corrections must be present and independently
reviewed before author dispatch.

RESIDUAL RISK: a passing embedded self-test proves its fixed matrix, not the
unapproved valid-JSON primitive/array behavior.

PERSONA: The Simplifier  
MODE: Adversary  
TIER: T1  
VERDICT: BLOCK

FINDINGS:

- [Major] (Verified) Open-ended permutation/generation language exceeds the
  fixed granted matrix and invites a harness framework the change does not need.
  Fix: use isolated literal boundary cases, with only the valid ULID minted by
  the canonical allocator, and no new abstraction or dependency.

CLEARS-THE-VETO: no — the author plan must replace the open-ended scope with the
fixed matrix and the smallest real-Git fixture seam.

RESIDUAL RISK: repeated fixture setup may be verbose, but a shared general test
framework is not justified for this two-file slice.

PERSONA: Python Developer  
MODE: Adversary  
TIER: T1  
VERDICT: PASS-WITH-CONDITIONS

FINDINGS:

- [Major] (Verified) Adding argparse can accidentally reinterpret existing
  positional paths. Fix: declare `--self-test` plus a positional `logs` sequence
  and regression-check no-argument, one-path, and multiple-path invocation.

CLEARS-THE-VETO: yes for plan review when item 2 is explicit; implementation
review must inspect the exact parser and byte-identical-copy assertion.

RESIDUAL RISK: the existing `.get` call on valid JSON primitives remains outside
authority and must not be described as covered by the malformed-JSON case.

PERSONA: SRE & Systems Diagnostician  
MODE: Adversary  
TIER: T1  
VERDICT: PASS-WITH-CONDITIONS

FINDINGS:

- [Major] (Verified) The Owner requires local Git isolation and cleanup on all
  failure paths. Fix: retain explicit timeouts, local config, disabled inherited
  signing/hooks, command arrays, and `TemporaryDirectory`/`finally` cleanup in
  the acceptance contract.

CLEARS-THE-VETO: yes for plan review when item 6 is explicit.

RESIDUAL RISK: Git executable absence is an observed self-test failure, never a
skip; the join environment remains the integration proof.

PERSONA: Orchestrator  
MODE: Adversary  
TIER: T1  
VERDICT: BLOCK

FINDINGS:

- [Blocker] (Verified) The plan and Owner note say the grant is pending although
  request `req-01M2K3PZY3J1DQY7HZN443E81C` is resolved GRANTED with additional
  constraints. Fix: append a superseding grant section and dispatch only its
  exact two-file, one-commit contract after this review passes.

CLEARS-THE-VETO: no — current authority and the red-first resolution must be
present in the dispatchable plan.

RESIDUAL RISK: branch-local proof does not authorize main publication or edits to
live audit registers outside official audit bookkeeping.

## Testing trigger union

- **D0:** deterministic isolated cases; focal CLI invocation; status plus exact
  diagnostic; no clock, sleep, network, global config, conditional assertion, or
  leaked fixture.
- **D1:** named applied guard mutants plus always-green and always-fail CLI
  mutants; every mutant must make the whole self-test exit nonzero.
- **D2:** fixed grammar boundaries for sequential and allocator-minted ULID IDs,
  invalid spelling, blank/missing identity, malformed syntax, duplicates, and
  append/delete invariants. No unbounded generator is required.
- **D4:** real temporary filesystem and Git repository, real HEAD comparison,
  default and explicit CLI paths, commit/deletion/append state transitions.

Required scoped gates after implementation: the new audit verifier self-test,
normal audit verifier invocation, `verify-gate-self-tests.py --self-test`, normal
`verify-gate-self-tests.py`, Python syntax/compile check for both changed tools,
and the repository's unchanged full required join runner. A green author branch
does not replace independent frozen-candidate review or integrated join proof.
