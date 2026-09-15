---
id: proof-audit-gate-self-test-author
title: "Audit verifier self-test author proof"
type: proof-pack
status: in-progress
owner: "@timianmalloo"
tags: [audit, verification, python, mutation-testing]
links:
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Records disposable-copy red-first evidence, real Git and CLI fixtures, seven semantic mutants,
  unchanged normal behavior, and the audit self-test ratchet reduction.
---

# Audit verifier self-test author proof

- **Base:** `8b164757`
- **Core grant:** `req-01M2K3PZY3J1DQY7HZN443E81C`, Ruling 119
- **Pristine gate Git blob:** `34664bad660f9e8d844294d08d8f229b0445391d`
- **Pristine source SHA-256:** `7a080aa25b053582c5755f2d6998dba2eba320c073044f17fc59dcb5044ce73f`
- **Proven harness SHA-256:** `06b949ec329c268747f1c99ea93e1dd3b9e2ed4802bcc3f8c4a1abb6cddf3845`
- **Final gate Git blob:** `b889708d621adfc9219e0c3bc7d94585e837cf28`
- **Final ratchet Git blob:** `86149c42d83e5767b6d906b1691dcbad00c4ea34`
- **Author/date:** `codex-sol-author`, 2026-09-15

## Author correction

At the commit boundary, an author guard compared `HEAD` with an invented full expansion of the
short base name `8b164757` and falsely reported an outside commit. Read-only inspection showed that
`HEAD` was unchanged at `8b164757e1db9da44bfe0ec5907cd8d6a6765fa6`; its only changed path from
the prior main commit was the known Ruling 119 note, with no overlap in this slice. The incident
report was withdrawn before any audit write, staging, or commit. The no-guessing control is to
capture `git rev-parse HEAD` at grounding and compare that observed value, never construct a full
identity from a short SHA.

## Red-first sequence

The unchanged production gate was copied byte for byte into a disposable tree before any production
edit. The pristine source and copy had the same SHA-256 shown above. The intended harness was then
authored only in that disposable copy.

The first harness run exited 1 in 2.113 seconds. This was a harness failure, before production
insertion:

- The optional-file oracle expected one extra padding space. The observed semantic output was still
  `change-log.jsonl (absent)` and exit 0. The corrected oracle checks the filename and `(absent)`
  separately.
- Five mutation targets occurred in both normal logic and their mutation metadata. Exact-once checks
  reported occurrence counts `2, 2, 3, 3, 2` and refused the mutations. The corrected metadata builds
  each target from non-contiguous literals, so the tested normal branch is its sole occurrence.
- The forced-green and forced-failure normal CLI mutants were already rejected by their named cases.

After those two harness-only corrections, the disposable run exited 0 in 5.686 seconds. It printed
all seven named mutation receipts:

| Mutant | Named killing oracle | Mutant exit |
|---|---|---:|
| duplicate guard disabled | `duplicate id rejected` | 1 |
| deletion guard disabled | `committed id deletion rejected` | 1 |
| malformed JSON guard disabled | `malformed JSON rejected` | 1 |
| missing-id guard disabled | `missing id rejected` | 1 |
| invalid-id guard disabled | `invalid id rejected` | 1 |
| normal CLI forced green | `duplicate id rejected` | 1 |
| normal CLI forced failure | `valid default logs accepted` | 1 |

Every replacement target was required to occur exactly once. A nonzero status counted only when the
named oracle appeared and the output contained no traceback.

## Independent BLOCK repair: ambient state and exact dispatch

Independent review of commit `8d4310852b9014163174423522b888aa8d8b0086` found three self-test
harness defects. All were reproduced before the follow-up repair:

| Red case | Blocked result |
|---|---|
| inherited `AUDIT_GATE_MUTANT=1` | exit 0, claimed 7 mutants, printed 0 mutant rejection receipts |
| positional `--self` | argparse abbreviation ran the self-test and exited 0 instead of checking an absent path |
| spectator `GIT_DIR`, `GIT_WORK_TREE`, `GIT_INDEX_FILE` | exit 1; fixture Git reinitialized the spectator repository and could not stage fixture paths |

The spectator red run recorded unchanged HEAD, index SHA-256, porcelain status, and local config.
The failure was still a hermeticity defect because fixture commands targeted the wrong repository.

The narrow repair makes `self_test(run_mutants=True)` call an explicit private execution path.
Mutant modules are loaded with `importlib` and call `self_test(False)` in process, so ambient
environment state is never a recursion control. The public full path deliberately sets and restores
`AUDIT_GATE_MUTANT`, ensuring every ordinary self-test exercises the inherited-state regression.
Completed case and mutant counters increment from actual executions; the full path also requires the
completed mutant count to equal the configured mutation map size.

Dispatch enters argparse only when `argv` is exactly `["--self-test"]`. All other argument vectors
go directly to the original `main(argv)`. Executable fixtures prove `--self` and `--help` remain
ordinary absent positional paths. A direct check also proved `--other` has the same behavior.

Every fixture Git and Python child receives a copied environment with all repository-local variables
reported by the installed `git rev-parse --local-env-vars` removed. Global/system Git configuration
is still disabled for fixture commands; local fixture identity, signing, and hook controls remain.

| Green case | Result |
|---|---|
| inherited `AUDIT_GATE_MUTANT=1` | exit 0 in 6.11 s; 11 completed cases; 7 mutant receipts |
| `--self`, `--help`, `--other` | each exit 0 as an absent positional path; no self-test output |
| private spectator environment | exit 0 in 8.04 s; 11 completed cases; 7 mutant receipts |
| spectator integrity | HEAD, index bytes, status, and local config all unchanged |

- **Repaired source SHA-256:** `8964a27a3de0fd26ed4535c5c6217815a9a0ed3342526c2ef6f3cdbe12f721b3`
- **Normal gate after A3 repair:** exit 0; 693 audit + 147 change = 840 entries; zero duplicates.
- **Frozen ratchet after A3 repair:** exit 0; 36 gates; 9 frozen names.

Additional proposed DC-104 instance text for the Conductor-owned lesson update:

> A self-test recursion switch inherited through ambient environment can silently skip the proof it
> reports, and permissive option parsing can widen a tooling-only flag into existing positional CLI
> behavior. Use an explicit private recursion parameter, derive receipts from completed work, and
> route only the exact new argument vector. Fixture subprocesses must also clear the complete
> repository-local environment contract reported by the installed Git; changing the current working
> directory alone does not isolate `GIT_DIR`, `GIT_WORK_TREE`, or `GIT_INDEX_FILE`.

## Fixed real-boundary cases

The harness creates a temporary repository, disables global and system Git configuration for every
fixture command, supplies local identity through per-command configuration, disables signing and
hooks, and uses 30-second subprocess timeouts. `TemporaryDirectory` owns cleanup on all returns.
The copied verifier is checked byte for byte against the source under test.

| Case | Mode | Observed oracle |
|---|---|---|
| numeric and allocator-minted ULID records | default | exit 0; 3 entries across 2 logs |
| one unique append | default | exit 0; 4 entries across 2 logs |
| reversed valid record order | explicit path | exit 0; 2 entries across 1 log |
| committed optional log absent | default | exit 0; `(absent)`; 2 entries across 2 logs |
| duplicate identifier | explicit path | exit 1; names `al-0001` and count 2 |
| committed identifier deleted | explicit path | exit 1; names missing `al-0001` from HEAD |
| malformed JSON | explicit path | exit 1; names file, line 3, and invalid JSON |
| missing identifier | explicit path | exit 1; names file, line 3, and missing id |
| invalid identifier | explicit path | exit 1; names `bad id` and both supported forms |

The ULID is minted by dynamically importing the installed
`docs/ai-forward-pack/scripts/audit-log.py` and calling its `next_id([], "al")`, matching the
allocator import used by `tools/merge-append-only-log.py`. No live audit fixture is written.

## Production verification

The inserted production source SHA-256 exactly matched the corrected disposable harness.

| Command | Exit | Duration | Observed result |
|---|---:|---:|---|
| `python -m py_compile tools/verify-audit-log.py` | 0 | included in verification run | no output |
| `python tools/verify-audit-log.py --self-test` | 0 | 5.705 s | 9 fixed Git/CLI cases and 7 semantic mutants passed |
| `python tools/verify-audit-log.py` | 0 | 0.118 s | 692 audit + 147 change = 839 entries; zero duplicates |
| `python tools/verify-gate-self-tests.py` | 0 | 0.058 s | 36 gates; frozen count reduced from 10 to 9 |

The normal identifier regex, duplicate/deletion/JSON/missing-ID/invalid-ID findings, and normal
`main` acceptance/refusal branches are unchanged. The production delta adds only self-test support
and dispatch; the ratchet delta removes only `verify-audit-log.py`.

## Class, sweep, derive, prevent

- **Class:** DC-104, a control that lacks executable proof of its own semantic branches.
- **Sweep:** the four advertised audit policies, default and explicit CLI modes, accepted missing
  optional file, ordering, append behavior, and both success/failure blanket mutations.
- **Derive:** each refusal requires exit 1 plus its own diagnostic; each acceptance requires exit 0
  plus cardinality. Tracebacks never satisfy an oracle.
- **Prevent:** `--self-test` runs real filesystem, Git HEAD, CLI, and mutation boundaries; the frozen
  ratchet now requires that self-test to remain discoverable.

Proposed DC-104 instance text for the Conductor-owned lesson update:

> Mutation metadata can contain the exact source token it intends to replace, making an
> exactly-once assertion count its own harness and refuse a valid mutation. Construct target
> metadata so it cannot self-match, and require a named semantic oracle rather than generic status.
> Presentation assertions should match semantic fields separately when alignment whitespace is not
> part of the CLI contract.

## Scope and residuals

- **Verified:** the missing optional file remains accepted, including when it exists in HEAD.
- **Flagged:** valid JSON primitives and arrays can still raise because the normal parser calls
  `.get`; Ruling 119 explicitly leaves that acknowledged limitation unchanged.
- **Flagged:** independent frozen-candidate review remains required; the author does not clear its
  own veto.
- **Flagged:** the Conductor owns integration and the full required gate runner.

| | |
|---|---|
| **Completed** | Disposable red-first harness, seven named mutants, production insertion, normal CLI and ratchet checks |
| **Remaining** | Independent candidate review and Conductor join |
| **Best next action** | Commit the two-tool slice with this proof and hand its SHA to the independent reviewer |
