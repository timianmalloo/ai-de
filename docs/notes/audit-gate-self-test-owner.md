---
id: note-audit-gate-self-test-owner
title: "Audit verifier self-test: bounded Owner decisions"
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [audit, verification, decision, coordination]
links:
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Adds observable proof of existing audit-verifier behavior through an isolated self-test.
  Preserves policy, allocator behavior, live logs, and every other frozen gate entry.
---

# Audit verifier self-test: Owner decisions

## Scope and acceptance

T1 confirmed. Goal: add a meaningful `--self-test` to `tools/verify-audit-log.py`, then remove
only its name from `KNOWN_WITHOUT_SELF_TEST` in `tools/verify-gate-self-tests.py`.
Done when the isolated fixture suite proves existing success and failure behavior, meaningful
mutants are rejected, independent reviewers clear applicable vetoes, and the reviewed candidate
has a Proof Pack and required records ready for serialized Core handoff.

Core grant request `req-01M2K3PZY3J1DQY7HZN443E81C` is pending at this decision; no implementation
before its explicit resolution. This decision grants no main push. No allocator, merge tooling,
product source, schema, ownership policy, live audit fixture, or other frozen-list change belongs
to this slice. Astra Owner adjudicates scope; the Conductor delegates execution; independent
reviewers, never the author or Owner, clear their hard vetoes.

## Grounding and specification reuse

Verified source at base `bbd1bece`: the gate docstring defines four checks: duplicate identifiers,
disappearance of HEAD identifiers, supported numeric/ULID identifier syntax, and valid JSON with
an identifier. `committed_ids` uses real `git show HEAD:<relative>`; `main` aggregates findings into
exit 1 or prints counted success with exit 0. The ratchet recognizes an argparse declaration of
`--self-test`, and requires removal of a stale frozen entry.

These existing contracts are the functional specification. Existing stdlib CLI architecture is
retained; this note supplies the new fixture/harness design. UX is the existing developer-facing
diagnostic and status contract plus explicit self-test results. Visual UI design is N/A.
Domain: one JSONL row is one audit/change event, identifier is a value, and HEAD supplies the
existing append-only comparison. No persisted representation or aggregate invariant changes.

## Policy boundaries

- Missing identifier (`{}`) is a negative fixture. An absent file is currently accepted and
  prints `(absent)`; preserve and characterize that behavior. Do not turn missing-file behavior
  into a new gate policy, including when the missing path was previously committed.
- Malformed JSON means syntactically invalid JSON text. Valid JSON primitives/arrays are a
  separate shape: source calls `.get` without checking object type. **Inferred runtime risk:**
  these can raise rather than produce a finding. Record/reproduce as a separate finding if needed;
  no primitive handling fix is authorized by this self-test-only scope.
- Preserve existing numeric and ULID acceptance and existing identifier-prefix rules. Do not
  add timestamp validation, prefix-to-filename coupling, nonempty-log requirements, or stricter
  JSON schema as incidental cleanup.
- Preserve normal default and explicit-file CLI forms. Introduce only the self-test route, with
  an actual argparse declaration so the existing ratchet recognizes it. Do not satisfy the
  ratchet with a docstring mention or a flag that merely returns success.

## Fixture strategy and oracles

Use a real temporary Git repository with local test identity, no network, and synthetic logs.
No fixture writes to the live repository or changes global Git configuration. Suppress inherited
signing/hooks within the fixture's commands/configuration so local user configuration cannot run
unrelated work. Cleanup belongs to the temporary-directory lifecycle, including failure paths.

Exercise real gate behavior and CLI status/diagnostics. A minimal implementation may place a
byte-identical copy of the gate under the fixture's `tools/` and invoke it with `sys.executable`;
this lets its existing repository-root calculation and `git show` operate without mocking Git
or adding a production root override. Do not recurse into the copied self-test. Another equally
small seam needs independent review for fidelity; no general harness framework is needed.

Required cases use isolated fixture states:

| Case | Oracle |
|---|---|
| Valid numeric and ULID identifiers | Exit 0, positive count, no findings |
| Append a new unique event after committing the baseline | Exit 0; committed identifiers retained |
| Duplicate identifier | Exit 1 and diagnostic naming that identifier and duplicate count |
| Delete one committed identifier while retaining valid distinct rows | Exit 1 and missing-HEAD identifier diagnostic |
| Syntactically malformed JSON | Exit 1 and file/line JSON diagnostic, not a traceback |
| Missing identifier | Exit 1 and missing-id diagnostic |
| Unsupported identifier spelling | Exit 1 and identifier-format diagnostic |
| Absent file | Existing accepted/absent behavior; no invented failure policy |

Assert status and identifying diagnostic together. A generic exception, missing fixture, or a
different gate failure is not evidence that the intended guard fired. Observe red before adding
the self-test route; then prove the resulting self-test rejects mutants that disable duplicates,
HEAD disappearance, malformed-JSON finding, missing-id finding, and identifier-format validation.
Positive cases must prevent a blanket failure implementation from passing the suite. Name mutant
application sites and observed outcomes; no mutation that silently failed to apply counts.

Surface list: argparse route -> temporary Git/files -> real verifier -> HEAD comparison ->
diagnostic/status assertions -> frozen-list ratchet -> existing gate execution -> Proof Pack.

## Failure, security, and privacy dispositions

Input mistakes: detect using exact synthetic oracles; preserve unapproved policy gaps separately.
Git dependency unavailable or setup/commit failure: fail the self-test with setup evidence, never
skip the deletion proof. Concurrency: unique temporary directory and no global Git writes.
State contamination: reset fixture states or use separate logs/repos so prior invalid rows cannot
mask the next oracle. Resource/time failure: cleanup in all paths; a timeout is a failed/incomplete
observation, not a valid negative case. CLI invocation failure: inspect return code and diagnostic.

The added boundary is synthetic local files and subprocess Git/Python execution. Use argument
arrays with fixed commands, never shell-evaluate log content. No personal data, credentials,
network, or live log contents enter fixtures. Existing read-only verifier behavior is unchanged.
Reviewer disposition of any triggered security veto remains required; this note does not waive it.

## Gates, defect class, and close

Reuse **DC-104** (unproven control), with existing duplicate/deletion policy from DC-013/DC-026.
Class -> sweep -> derive -> prevent: inventory the gate's four advertised checks and CLI paths;
derive one discriminating positive/negative fixture per behavior; prevent regressions with the
self-test and mutant evidence. Do not sweep or repair unrelated gates.

Testing union: deterministic behavior, parser/validator, real filesystem/Git, and CLI provider
contract. Use real infrastructure and deterministic cases; generated inputs only when they cover
a meaningful invariant beyond the fixed existing grammar. No new dependencies. Test Architect
must independently judge oracle fidelity and mutant evidence; Python Developer and Simplifier
review implementation and scope. Required integration gates remain required; unchanged .NET
receipts can be reused only with recorded source/build-input and receipt provenance.

Owner verdict: PASS-WITH-CONDITIONS for design scope, not implementation acceptance. Core grant,
red-first observation, self-test/mutant proof, and independent veto clearance remain outstanding.
The Conductor must preserve historical records, avoid premature acceptance claims, and present
the candidate/base SHAs and proof to Core for serialized publication.
