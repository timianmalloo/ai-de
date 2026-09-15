---
id: proof-audit-gate-self-test-review
title: "Independent implementation review: audit gate self-test"
type: proof-pack
status: blocked
owner: "@codex-sol-review"
tags: [proof, audit, testing, review]
links:
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-15
summary: "Frozen candidate review finds an inherited-environment proof bypass and an abbreviated CLI alias."
---

# Independent implementation review: audit gate self-test

## Frozen inputs and scope

Reviewed author commit `8d4310852b9014163174423522b888aa8d8b0086`
against parent `8b164757e1db9da44bfe0ec5907cd8d6a6765fa6` in the
author's clean worktree. The final verifier resolves to Git blob
`b889708d621adfc9219e0c3bc7d94585e837cf28` and SHA-256
`06b949ec329c268747f1c99ea93e1dd3b9e2ed4802bcc3f8c4a1abb6cddf3845`.
The tool delta contains only `tools/verify-audit-log.py` and removal of its one
name from `tools/verify-gate-self-tests.py`. The pre-existing normal check,
identifier regex, findings, and `main` branches are textually unchanged.

The canonical valid ULID is minted by dynamically loading the installed
`audit-log.py` and calling `next_id([], "al")`. The fixture gate begins as a
byte-identical source copy. Its nine real Git/CLI cases assert exit plus named
diagnostics/counts. Its seven mutations require an exactly-once source target,
a named killing case, nonzero exit, and no traceback.

## Observed passing evidence

- `python <absolute-author>/tools/verify-audit-log.py --self-test`: exit 0;
  seven named mutant-rejection lines; summary reports nine cases, seven mutants,
  and the pinned SHA-256.
- Normal verifier: exit 0; 693 audit plus 147 change records, zero duplicates.
- Ratchet from the candidate repository root: normal exit 0, 36 gates and nine
  frozen names; ratchet self-test exit 0.
- `python -m py_compile` for both tools: exit 0.
- Cleanup rerun with `TEMP` and `TMP` bound to a unique reviewer-owned parent:
  self-test exit 0 and zero remaining children.

The first absolute ratchet invocation ran with the review tree as cwd. Because
the ratchet intentionally resolves its root from cwd, it examined the review
tree and returned 1. The corrected candidate-root invocations above pass. This
was a reviewer harness error, not candidate evidence.

A first cleanup census used a global prefix while the author concurrently ran
the same test and attributed two directories to this review. The isolated-parent
rerun disproved that attribution. The cleanup finding is withdrawn; unrelated
global temporary directories were not removed.

## Reproduced blockers

### 1. Ambient environment suppresses mutation proof

Observed:

```text
$env:AUDIT_GATE_MUTANT='1'
python <absolute-author>/tools/verify-audit-log.py --self-test
verify-audit-log: self-test OK — 9 fixed Git/CLI cases and 7 semantic mutants; ...
exit 0
```

That run emits zero `mutant rejected:` receipts because
`if not os.environ.get("AUDIT_GATE_MUTANT")` skips the entire mutation block.
The success summary nevertheless claims seven mutants. Ambient process state can
therefore turn off the proof while preserving its success-shaped output.

### 2. Argparse creates an unintended abbreviated flag

Observed parent behavior:

```text
python <parent-snapshot>/verify-audit-log.py --self
--self (absent)
verify-audit-log: OK — 0 entr(ies) across 1 log(s); ...
exit 0
```

Observed candidate behavior:

```text
python <absolute-author>/tools/verify-audit-log.py --self
... nine-case/seven-mutant self-test ...
exit 0
```

`ArgumentParser` enables long-option abbreviation by default, so `--self`
silently aliases `--self-test`. Ruling 119 authorizes the exact self-test route
and requires other positional-file behavior to remain unchanged.

## Persona verdicts

PERSONA: Test Architect  
MODE: Adversary  
TIER: T1  
VERDICT: BLOCK

FINDINGS:

- [Blocker] (Verified) Inherited `AUDIT_GATE_MUTANT` skips all seven mutants but
  the self-test exits 0 and claims they ran.
  Fix: remove ambient control of top-level proof, keep recursion control private
  to the spawned mutant run, derive the summary from actual executions, and add
  a regression that runs with the inherited variable set.
- [Blocker] (Verified) `--self` reaches self-test through implicit abbreviation,
  changing a promised normal positional invocation.
  Fix: accept only exact `--self-test` and regression-check that `--self` retains
  parent behavior.

CLEARS-THE-VETO: no — both counterexamples must pass on a new frozen candidate,
which must still emit seven named mutant receipts and pass the nine fixed cases.

RESIDUAL RISK: valid JSON primitives/arrays remain the explicitly excluded
normal-policy limitation.

PERSONA: Python Developer  
MODE: Adversary  
TIER: T1  
VERDICT: BLOCK

FINDINGS:

- [Blocker] (Verified) Default argparse abbreviation expands the CLI contract.
- [Major] (Verified) A generic inherited environment name controls internal test
  recursion and is not reflected in the reported count.

CLEARS-THE-VETO: no — use exact option parsing and a private in-process or
otherwise authenticated child-control seam with measured mutant counts.

RESIDUAL RISK: repair must stay after the normal `main` function and may not
change the existing verifier policy.

PERSONA: Security & Identity Architect  
MODE: Adversary  
TIER: T1  
VERDICT: BLOCK

FINDINGS:

- [Blocker] (Verified) Unvalidated ambient environment input crosses the proof
  boundary and suppresses required checks without changing the success verdict.
  Fix: make inherited environment unable to select the internal recursion path;
  fail closed if actual mutant executions differ from seven.

CLEARS-THE-VETO: no — the environment counterexample must execute all seven
mutants or fail explicitly, never report a false seven.

RESIDUAL RISK: this is local verification integrity; no network, credential, or
personal-data boundary is added.

PERSONA: SRE & Systems Diagnostician  
MODE: Adversary  
TIER: T1  
VERDICT: BLOCK

FINDINGS:

- [Major] (Verified) The self-test result depends on inherited environment while
  reporting a fixed count. Fix: isolate internal control and count executions.

CLEARS-THE-VETO: no — same environment regression as Test Architect. Fixture
cleanup itself is observed passing under an isolated temp parent.

RESIDUAL RISK: Git/setup/timeout failures must remain nonzero with actionable
diagnostics.

PERSONA: The Simplifier  
MODE: Adversary  
TIER: T1  
VERDICT: BLOCK

FINDINGS:

- [Major] (Verified) Two implicit mechanisms add behavior the contract does not
  need: argparse abbreviation and a public ambient recursion switch.
  Fix: exact flag matching plus the smallest private recursion seam; do not add
  a general harness framework or new dependency.

CLEARS-THE-VETO: no — the repair must remove both implicit paths without growing
the two-file scope.

RESIDUAL RISK: author cost is recorded as 23/20 calls; cost does not waive proof.

## Veto-clear predicate

A repaired frozen candidate clears this receipt only when independent runs show:

1. `--self-test` passes nine cases and emits exactly seven named mutation
   receipts, with the summary derived from that actual count;
2. inherited `AUDIT_GATE_MUTANT` cannot suppress those mutants or create a false
   success;
3. `--self` preserves its parent positional-path behavior;
4. normal verifier, candidate-root ratchet normal/self-test, compilation, copy
   fidelity, allocator origin, and isolated cleanup remain passing; and
5. the diff remains the exact two tooling files with no normal-policy branch
   change.

