---
id: investigation-code-atlas-native-repair-controls
title: "Code Atlas E repair - handle access and junction teardown failures"
type: investigation
status: draft
owner: "@timianmalloo"
phase: "atlas-e-native-diagnosis"
tags: [code-atlas, investigation, windows, native, tests]
links:
  - { to: proof-code-atlas-enumeration-safety, rel: depends-on }
  - { to: note-atlas-live-reader-horizon, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Isolates the three failing E repair tests into two mechanisms: metadata-only native access
  does not enforce the tested sharing exclusion, and recursive junction fixture teardown fails
  without the product enumerator. Proposes a bounded correction for Owner review.
---

# Symptom, scope and evidence

The uncommitted four-file proposal in `atlas/live-reader-inventory-repair`, based on
`e54f21e1`, ended its 30-call allowance with **95 executed, 92 passed, three failed**.
It remains a proposal, not admitted code. The failure file is
`.e-repair-results/e-repair-green3-20260912.trx`; the exact proposal is retained at
`.e-repair-results/e-repair-proposal.patch`.

The required behavior comes from the reviewed enumeration/source probes: held ordinary-local
objects prevent the relevant mutation while metadata is consumed, all resources are released,
and fixture cleanup must not follow links into an outside target.

The actual failures:

- `EnumerateAsync_InspectedMetadata_BlocksReplacementAndWriteUntilReleased`: an expected
  `IOException` was not thrown inside the callback while the inspected handle was live.
- `EnumerateAsync_RootBelowJunction_RefusesAncestorRedirection`: `UnauthorizedAccessException`
  in `Dispose -> TryDelete -> Directory.Delete(root, recursive: true)`.
- `EnumerateAsync_DirectAndParentListedJunctions_DoNotExposeOutsideNamesOrCounts`: the same
  teardown path, not an enumeration assertion.

The chosen method is change analysis plus controlled falsification. Competing explanations were
an ineffective access mask, leaked product handles, fixture-only recursive deletion behavior,
and ambient permissions preventing all mutation.

## Controlled observations

The Conductor used new synthetic files only, on CLR `10.0.11`, in the session-owned directory
`files/e-native-diagnostic-5f5181e0b7be4e6881f623365730e88e`. No product source was edited.

| Variable / experiment | Observed result | What it establishes |
|---|---|---|
| `CreateFileW` desired access `0x80` (`FileReadAttributes`), share Read, same no-follow/backup flags | Write and rename both succeeded while held | This access mode is insufficient for the tested exclusion |
| Change only desired access to `0x80000000` (`GENERIC_READ`) | Write and rename both failed with `IOException`, HRESULT `0x80070020`; both succeeded after disposal | Necessary/sufficient access-mode distinction in this controlled file case; not a general filesystem guarantee |
| Create ordinary synthetic parent + junction + target; call recursive `Directory.Delete(parent, true)` with **no enumerator involved** | `UnauthorizedAccessException` | Product enumerator handles are not necessary for this fixture failure |
| Read state after that failure | Junction gone; parent remains; synthetic target file remains | The failure cannot be interpreted as “nothing was removed” |
| Fresh fixture: delete the junction itself non-recursively, then recursively delete its ordinary parent | Both succeeded; target file remained | Explicit unlink-first teardown removes the observed fixture failure without traversing the target |

The proposal's `OpenNative` uses `FileReadAttributes` for all inspected/held objects. Its tests
use recursive parent deletion over live junction entries. These source observations match the
two independently reproduced mechanisms.

## Diagnosis and limits

**Verified for the controlled cases:** metadata-only access does not establish the sharing
exclusion the test requires; the fixture's recursive junction cleanup fails independently of the
product enumerator. Ambient permissions alone are ruled out by successful after-release writes/
moves and unlink-first cleanup. Product-handle leakage is not the cause necessary to reproduce
the two teardown failures.

The full repaired E suite has **not** yet been run with these changes. This investigation does
not certify all no-follow races, symlink behavior or product safety. The specific new evidence
that would change the diagnosis is a failing exact regression after applying only the access-mask
and unlink-first corrections.

## Proposed phased correction

| Phase | Scope: code + test | Oracle | Dependency / rollback |
|---|---|---|---|
| 1 | Restore the reviewed data-read access needed by the held-object sharing contract; retain no-follow inspection, all handle budgets and confinement checks | The exact held-metadata mutation test fails on attributes-only access and passes with mutation blocked until release | Owner approval; revert only this access-mode change if disproved |
| 2 | Fixture teardown unlinks known reparse entries non-recursively before removing owned ordinary parents; do not swallow cleanup failures | Both junction tests complete and synthetic outside targets remain intact | Owner approval; no production traversal change |
| 3 | Run the exact three cases, then the complete E/Understanding selection; preserve distinct result files and read counts | Zero remaining failures, no silent skip or weakened assertion | Phases 1–2; proposal remains held if any failure remains |

## Generalization and review gate

The access-mode instance belongs to the known adjacent-control class: obtaining metadata is not
proof of sharing exclusion. The earlier probes already demonstrated the distinction; a new
least-access choice must re-prove the safety property rather than inherit it by name.
The fixture instance is a different boundary: a teardown exception does not establish a product
failure, and a failed delete does not establish unchanged state.

Scoped sweep: the proposal has one native open helper using the metadata-only mask and one
recursive fixture-delete helper used by both failing junction cases. The readback found no
`assume:` or `simplify:` markers in the inspected enumerator/test paths. Broader unrelated
repository repairs are not admitted by this investigation.

Independent SRE disconfirmation and delegated Owner review were requested. **Implementation is
not released by this report.** The Owner must approve the precise phases/allowance; Security and
Test still own the subsequent E incorporation gates.

SRE `9289f3fc-6453-4151-8ffc-5f7a422a2347` returned PASS-WITH-CONDITIONS after reading the actual
TRX and callback/lifetime paths. The callback executes while the inspected handle is still held;
callback timing/disposal is not the explanation for the missing mutation exception. Both junction
failures are in fixture disposal. The SRE accepted the two-mechanism diagnosis with these limits:
retain no-follow flags and target-preservation checks, prove after-release mutation, and disclose
that requesting GenericRead may refuse metadata-only ACL configurations. No ACL/privilege change
is approved, and no full E safety clearance is inferred from this diagnosis.

Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 16, accepted this bounded diagnosis and
approved phases 1–3 for the same writer, same four files, **six additional leaf calls**. This
closes the investigation-to-repair gate only. Preserve the original 92/95 result, mutation
assertions, target-file preservation and exact sharing-error/release controls. No assertion
weakening, swallowed cleanup failure or automatic additional pass is authorized. Independent
Security/Test and parent readback still precede E incorporation.

## Approved repair outcome

The same writer completed the six-call close and committed `c7f6caf9`. The exact three failing
tests passed first; the complete targeted set then reported **95 executed, 95 passed, no skips**.
The Conductor independently repeated the 95-test run and read its result. The original 92/95
failure artifact remains retained.

Test Architect `e8c73a03-3fa2-4a77-8d17-68cdf80188b1` read the red/intermediate/final TRXs and
cleared the bounded E test-oracle blockers, including actual junction setup and target preservation.
This confirms the proposed phases resolved the three observed failures without weakening those
assertions. The independent Security incorporation disposition remains separate; no general
filesystem safety, symlink support or native-reader completion is asserted.
