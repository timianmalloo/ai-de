---
id: note-cross-harness-timestamp-boundary
title: P1 timestamp conversion failures are field errors
type: doc
status: accepted
owner: "@timianmalloo"
phase: P1
tags: [cross-harness, coordination, validation]
links:
  - to: spec-cross-harness-coordination
    rel: relates-to
  - to: design-cross-harness-coordination
    rel: relates-to
review-by: 2026-10-17
summary: Records the bounded Python P1 timestamp adapter contract and its stable field error. Arbitrary JSON integer payloads and canonical digest bytes remain unchanged.
---

# Decision recorded before implementation

The user's 2026-09-17 field-error correction applies to dormant schemaVersion 1,
digestVersion 1 responses and all four protocol fact kinds. `producerAt` and
`recordedAt` accept exact Python `int` or `float`, never `bool`, when Python's
`math.isfinite` conversion succeeds and yields finite binary64. Negative finite
values, zero, subnormals and the largest finite float remain valid. There is no
nonnegative, calendar or database-range rule in these validators.

An integer is mathematically finite but may overflow this adapter's binary64
conversion. Catch that specific `OverflowError` in one predicate in
`coord_protocol.py`, reused through the core's existing lazy module loader.
Reject the timestamp with `XH.FIELD_INVALID`, not an unsupported-version error
or runtime exception text. Do not convert or replace stored values. Conversion
rounding remains Python's existing behavior; this is not a new exact-integer or
absolute-magnitude limit.

This intentionally corrects the v1 error contract: wrong timestamp types
previously reported `XH.SCHEMA_INVALID`; oversized integers escaped direct
validators and leaked exception text through the reader. Nonfinite JSON floats
still fail the existing tree check as `XH.NONFINITE_JSON`; duplicate keys and
unsupported envelopes retain their existing errors. Missing required fields
remain schema errors. Legacy `at` retains its `XH.INVALID_TIMESTAMP` code.

Surface reach: direct response validator / direct fact validator -> official
JSONL reader -> request-list CLI refusal, before fold or authority verification.
`recordedAt` remains excluded from the digest, not from validation. Response
payload extensions may still contain exact 1001-digit integers (`10**1000`);
the tree walker, JSON conversion limits, canonicalization and digest algorithm
are unchanged. Protocol fact payloads retain their existing closed schemas.

## Compatibility and control

The defect class is a bounded numeric adapter throwing before its field-error
boundary. The sweep found the response timestamp predicate, both fact timestamp
fields, and legacy `at`; the generic tree walker already tests only floats and
must not acquire an integer conversion or global BigInt ban. Derive the numeric
predicate once; cover both signs of overflow, both fields, official reader/CLI
errors, finite counterexamples, exact huge payload bytes and digests. Reverse the
overflow result in memory and require these same focal assertions to fail.

The independent C# work remains pinned to
`ebd4f1c8473b70934ec29d778419289719ef5481`. Its oracle must be updated explicitly
by its owner to this repair's commit/source hashes; do not rewrite the spike
corpus, canonical design or Proof Pack here. Endpoint and triage authentication
remain unqualified; enhanced writer admission and all production rights remain
DENY. No new peer review or live qualification is claimed.

## Executed evidence, 2026-09-17

Verified on Python 3.12 in the registered P1 worktree:

| Check | Observed result |
| --- | --- |
| Unmodified P1 suite | 62 tests passed, including pinned legacy CLI/rollback |
| Nine new tests before production edits | 96 assertion failures, zero test errors; direct overflow and reader/CLI runtime text reproduced for both timestamp fields and signs |
| Complete suite after repair | 71 tests passed, zero failures/errors |
| Existing semantic mutations | All 18 killed with zero test errors |
| Overflow guard reversed to return true | Response focal test: 4 failures; fact focal test: 16 failures; zero test errors |
| Positive counterexamples | Finite boundaries and exact 1001-digit payload integer preserved; existing type/null/Unicode golden bytes passed |

Controls live in `test_coord_responses.py` and `test_coord_protocol.py` beside the
existing P1 fixtures. Run `python -B -m unittest discover -s
docs\ai-forward-pack\scripts\tests -p "test_coord_*.py" -q`, then
`python -B docs\ai-forward-pack\scripts\tests\test_coord_protocol.py --mutations`.
Reader fixtures retain the next legacy row and unchanged input bytes; CLI
fixtures exit 4 with the stable field error, empty stdout and no traceback.

The canonical Proof Pack and shared defect register are outside this owner's
authorized edits; this note carries the bounded class/control handoff without
claiming those artifacts were updated. Docs Explorer/site regeneration is also
deliberately deferred to the owner; only the local audit view is regenerated.
