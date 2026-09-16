---
id: proof-atlas-p1-03-image-diagnostics-review
title: "Independent review of image-query diagnostic candidate"
type: proof-pack
status: review
owner: "@timianmalloo"
tags: [atlas, proof, forensicreview, diagnostics]
links:
  - { to: proof-atlas-p1-03-pair-preparation, rel: depends-on }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-16
summary: "CLEAR for exact diagnostic-only runner and candidate input delta: independent 19/19 controls, persisted primary/secondary evidence, and full input comparison. Promotion and native execution remain separate."
---

# Exact diagnostic-only candidate: CLEAR

**Verified:** independent harmless selftests pass 19/19; both maintained diagnostic
consumers preserve immediate native error and pending identity, while actual process
and browser acceptance predicates refuse the faulted outputs. No blocking
counterexample was found in the exact diagnostic-only delta. This review approves
that delta and its input comparison, not historical cause or native operability.
Manifest promotion remains an Owner decision. No automatic slot request follows.

Goal: independently review exact runner/manifest diagnostics. Done when Test/SRE/
Simplifier clear or return a concrete veto with observed controls and source
preservation. Tier T2; Astra; fan-out zero; six calls/ten minutes, checkpoint four.
No maintained repairs, builds, native Facts, GUI/browser launch, source join or push.
Surface list: native call -> helper diagnostic -> exception -> primary/secondary
process record or browser failure row -> unchanged acceptance predicate -> receipt.
Existing repair -> independent review -> separate Owner graph is unchanged.

Tree `C:/Projects/ai-de-review-atlas-p1-03-image-diagnostics`, branch
`review/atlas-p1-03-image-diagnostics`, base
`41421c8e844942e0e23ad26e757d95da8ebf29d6`. Marker/liveness preceded work. Instruction
diff from `f8ad3323` is empty; previously grounded guidance applies.

## Exact candidate and input population

| Artifact | Independently computed SHA256 |
| --- | --- |
| Candidate runner | `9615a47ac9b5acb7ce8c4fb09e23bdb0e99d4ebd62aedf804a97944ded86af2b` |
| `manifest-image-diagnostics-candidate.json` | `66ccb1a395d44e34818f21c24c2f0608605f539484452c42890e2c0abcd1ff3e` |
| Existing `manifest-corrected.json` | `a58c5993e2a8ce0c30ca3ea38339da0f2faf5943a7d891d7b3cf6a42b51ba23c` |
| Original `manifest.json` | `ed4f937ba886f57b186ea02b0fa9cf74135bbb7137d5938cf99942d210360314` |

Manifests remain under the original prepared tree's
`artifacts/atlas-pair-preparation/`. **Verified:** candidate status remains
`proposed-not-promoted`. Independently compared maps: 11,630 files/1,030 roots;
exactly the runner changed; zero additions/removals; 11,629 unchanged other inputs,
including all 11,624 original non-runner inputs and all five measured Git additions.
Roots and stored tool identities match the prior corrected manifest.

The reviewer also independently enumerated the candidate roots and hashed the
current complete file population: zero missing roots, added/missing paths or hash
mismatches. This is byte/population evidence, not a fresh runtime resolver or native
qualification. No build ran. Product/test/tools diff against `f8ad3323` is empty.

Read-only comparison script and complete result are retained in this reviewer tree:
`artifacts/image-diagnostics-review/inspect.py` and `comparison.json`. The script
reads source/AST and JSON without importing or executing the runner; its assertions
gate all named preservation conditions. Raw files are local, not committed payloads.

## Red, independent green and actual persisted evidence

Read actual author `artifacts/atlas-image-diagnostics/{red,green-1,candidate-comparison,audit-readback}.json`.
Red contains 19 cases, zero assertion failures and three missing-field errors:
two `native_diagnostic` KeyErrors and one browser-row `failure` KeyError. The other
16 passed. Author green has 19 passed, no errors/failures. Each author's three
persisted image records was read directly and matched its copied record and hash.

After reading CLI and control bodies, ran exactly once:

```text
python -B docs/proof/records/atlas-p1-03-uia-transition/run_pair.py selftest --output artifacts/image-diagnostics-review/selftest.json
```

**Verified:** 19 tests passed in 2.430 seconds. Own summary contains all 19 case names
and empty details. Actual new persisted control/control-metadata JSON was inspected:

| New control directory suffix under prepared `controls/` | Maintained output SHA256 |
| --- | --- |
| `image-job-secondary-40a7d50bd6b04faa9c4e2560678588fe/run/process.json` | `e8ca790ad0dd5660f7037df7a631388a7732a05cd2bad6172bea314ffa46edc0` |
| `image-job-clobber-344c3b222f8d4c9d8b1794aa27e6309f/run/process.json` | `acbd0aaef00b46ecb365131b48c31cab722b0dc51fc3d4de0b9c87e7bea189e8` |
| `image-owned-snapshot-389160a0669b40a3ae899873d50f025b/browser-observations.json` | `6fe8a9557fb3df64e6011cc1a6ea54d87278a3af8dd88fa06ff14459c5a9af72` |

Both Job control commands point to this review tree's runner; their outer runner
is PID 7488/birth 134340717249644951. Failed child PID/birth values are 27160/
134340717255278307 (secondary) and 7804/134340717256000601 (clobber). Sibling row
identity is 26044/134340717256140512. The three folders were uniquely selected by
prefix and creation-period modification after the review marker; full records and
control metadata are copied into own comparison.json for durable local readback.

**Verified:** both Job outputs retain native error 122. The secondary case records
later WAIT_FAILED/error 6 under `later_exit` and nested `secondary_errors`; primary
122 remains. The clobber case's control records deliberate saved-error 9876, while
maintained JSON retains 122 and no secondary error. Sibling browser failure likewise
retains 122 through clobber 9876. Pending PID/birth matches the controlled handle;
membership precedes query timing; later observation ticks follow query end.

These are direct maintained outputs, not just proxy rows. The executed Job controls
parse process.json and call `verify_process_result` under `assertRaises(Refused)`.
The executed sibling control parses browser JSON and calls `verify_browser_use`
under `assertRaises(Refused)`. This supplies the missing acceptance execution from
the earlier investigation. The sibling queues harmless Python directly and fails
before CIM; it proves serialization/refusal, not browser runtime consumption.

Both faulted Job fixtures have total 2/active 0, forced true, retained handles exited,
and identities complete through the existing direct-child record. That record has
no image. Diagnostic data is not an accepted process row or a new identity source.
The historical seven-of-eight population is neither reproduced nor waived.

## Source and policy disconfirmation

Read full runner delta against `f8ad3323`. `query_process_image` calls
`ctypes.get_last_error()` immediately after the real false return, before clocks or
later native calls. The original native error is stored separately before later
wait diagnostics. The injection restores its real error immediately before returning
to the maintained caller; deliberate later clobber tests this ordering. No error
code alone is treated as cause.

The shared `failure_record` reaches both `run_owned` primary/secondary records and
the observer's existing error row, preserving its `error` field. Successful queries
still return the image. Image identity retention follows successful return only.
Existing `owned_snapshot` identity/live/membership checks and image comparison remain.

Independent AST comparisons observed unchanged `kernel`, `creation`, `gate`,
`execute`, `verify_process_result`, `verify_browser_use`, `profile_environment`,
`validate_receipt`, all 16 previous test bodies, and `run_owned` excluding only its
failure serializer. Thus no acceptance/containment/no-retry policy change was found.
Prior negative population/expiry/provider/cleanup controls ran again within 19/19.
This evidence is scoped to these named policies and actual cases, not all Windows
failure modes. Exceptional Python clock/wrapper throws are guarded source paths,
not independently executed new controls; native failed-wait behavior is exercised.

## Review-side location defect and correction

**FR-IDR-001 [Minor] (Verified):** the reviewer read CLI/control bodies but initially
missed hardcoded `ROOT`/`ARTIFACTS` at runner lines 23/28. Independent summary landed
in the requested review tree; UUID control files landed in the original prepared
tree. The read-only inspector expected local folders and stopped, with no comparison
success claimed. It was corrected to inspect the actual paths; no test was rerun.

The unchanged selftest also overwrote shared ignored
`artifacts/atlas-pair-preparation/containment-control.json`. That singleton is not
described as preserved author evidence. The three author image-control UUID records
and both prior manifests were separately checked and preserved. No maintained
file was changed by this location error; no cleanup or retrospective rollback ran.
The earlier broad peer statement that author evidence was preserved was corrected.

Class -> sweep -> derive -> prevent: cwd is not an output-root contract; inspect
ROOT, ARTIFACTS, UUID outputs and singleton outputs before cross-tree tests; use
the now checked exact destinations and fail if the matching raw population is not
unique. Root owns central recurrence/control follow-up. The unmodified runner's
fixed output root is a known preparation constraint, not a new diagnostic regression.

## Independent adversarial verdicts

PERSONA: test-architect   MODE: Adversary   TIER: T2

VERDICT: PASS

FINDINGS: author red is three specific missing-field errors; independent green is
19/19 with maintained-output assertions, both direct sites and executed refusals.
FR-IDR-001 limits evidence-location claims but does not contradict candidate results.

CLEARS-THE-VETO: yes for exact diagnostic-only delta; red, independent green and
raw Proof Pack inspected. No native readiness or historical-cause claim is cleared.

RESIDUAL RISK: synthetic faults are finite coverage; Python clock/wrapper exceptions
are source-guarded rather than independently exercised.

PERSONA: sre-diagnostician   MODE: Adversary   TIER: T2

VERDICT: PASS

FINDINGS: immediate 122 remains primary despite later 9876/6; pending identity and
later timestamps remain distinct from accepted identity and past liveness. No guard
relaxation found. Fixed raw output destination is disclosed in FR-IDR-001.

CLEARS-THE-VETO: n/a (advisory).

RESIDUAL RISK: historical image/UIA cause remains unknown; real browser/native
qualification needs a separately admitted unit.

PERSONA: the-simplifier   MODE: Adversary   TIER: T2

VERDICT: PASS

FINDINGS: one shared capture helper and serializer serve the two actual consumers;
three focused controls cover clobber, secondary failure and sibling serialization.
No new dependency, retry policy or process architecture introduced.

CLEARS-THE-VETO: yes; retained additions have concrete consumers/failure proofs.

RESIDUAL RISK: no general diagnostic framework or broader policy review is claimed.

## Closure

Checkpoint four reported the location defect and requested contingency budget;
comparison completed in batch five without test rerun. Remaining documentary close
fits guarded batch six; any gate/lease refusal stops rather than silently extending.
Audit duration is measured; final handoff records exact commit, calls and release.
No AIDE contract environment was available in this review chain; no contract event
is fabricated. Raw artifacts and review tree are retained. Source transport stays
separate; this receipt cannot join the experimental runner into main.

| Completed | Remaining | Best next action |
| --- | --- | --- |
| Exact diagnostic/input-delta review, independent controls, raw inspection | Owner promotion/disposition; any future qualification | Owner decides separately; no automatic native request |
