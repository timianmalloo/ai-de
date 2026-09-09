---
id: note-conductor-latency-slo-not-assertion
title: "Decision note — R1's 250 ms and R7's 2 s are recorded SLOs, not CI assertions"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, testing, performance, instrumentation, dc-107]
links:
  - { to: note-conductor-r4-core-phase1-scope, rel: refines }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-09
summary: >-
  Spec R1 requires run events within 250 ms of receipt, but this repo's CI gate
  verify-perf-assertions.py refuses any assertion that a measured duration is under a
  constant - it is the control for DC-107. The latency is therefore emitted and recorded
  on the normal path and evaluated as an SLO at the exit run; only arrival and the
  event-cycle ordinal are asserted.
---

# Decision note — R1's 250 ms and R7's 2 s are recorded SLOs, not CI assertions

**Ruled by:** Owner agent, 2026-09-09. Confidence: **Verified** (gate, spec text and the DC-107
register entry all opened). One sub-claim marked Inferred below.

## The conflict

Spec R1 bullet 2: *"Tool calls, edits, messages, and permission requests from the lane appear as
normalized run events **within 250 ms** of receipt."* R7 similarly requires live fields at
**"≤ 2 s lag"**.

`tools/verify-perf-assertions.py` is a **fail-closed CI gate** (`build.yml:162-166`, run with
`--self-test` first). Its rule: *"A test may not assert that a measured duration is under a
constant."* It is the control for **DC-107**, where `Assert.True(p95 < 16.67)` — a frame budget
taken on a developer workstation — failed deterministically on a ~3x slower CI runner **while
the code was correct**, stayed red for 38 runs, and took 26 other gates down with it.

A literal `elapsed < 250` is therefore **not a floor being thinned — it is a registered defect
class being re-created.**

## Ruling

R1 bullet 2 **passes as a test** when:

- the test asserts the event **arrives normalized**, under a **hang guard**; and
- the receipt-to-event **latency is emitted and recorded on the normal path**.

The **250 ms is an operator SLO**, evaluated against the recorded measurement at the Phase-1
exit run — **never a CI verdict**. The same rule applies to R7's "≤ 2 s lag": **record, do not
assert.**

**Distinguished:** R7's *"within one event cycle"* is an **ordinal, not a duration**. It **is**
asserted deterministically, and must not be quietly downgraded to telemetry.

## Because

The gate's own distinction is that *a hang guard asserts **finished**, a budget asserts
**fast***, and only the latter makes the host part of the verdict. The rejected alternatives:

- A `// perf-budget:` marker with a bound loose enough to be safe (say 5 s) is **an assertion
  that cannot fail**, and therefore proves nothing — a Test Architect finding.
- A same-host relational comparison has **no natural baseline** for "within N ms of receipt" and
  would be contrived. *(This sub-claim is **Inferred**.)*

**Marked plainly:** the spec names a duration **with no host**, which is under-specified as an
assertion. Reading "within 250 ms" as an SLO with a recorded measurement is an **extension of
the spec, not a reading of it.**

*(Correction carried from the ruling: the "measurable by default" rule cited in support is the
pack's instrumentation doctrine in `AGENTS.md`, not spec §8. Same conclusion.)*

## Conditions

1. The test asserts the latency measurement **exists and is a number**. A test that emits
   nothing and passes has **not** met R1.
2. The metric degrades to **"not recorded", never to 0**, when the receipt timestamp is missing.
3. The Phase-1 exit run **reports the measured latency against 250 ms with the host named**. A
   measured breach there is **a finding to bring back to the Owner**, not a silent pass.
4. The hang-guard bound is a **timeout argument**, not a relational comparison, so the gate
   stays green **without** a marker.

## Scope effect

**Admits:** arrival assertion + hang guard; a named latency metric emitted per event; recorded
p50/p95 with hardware named in the Phase-1 Proof Pack.

**Cuts:** any `// perf-budget:` marker in Phase 1 — none is needed, and one used to dodge the
gate is itself a finding.
