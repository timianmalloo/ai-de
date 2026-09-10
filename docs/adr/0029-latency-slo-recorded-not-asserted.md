---
id: adr-0029-latency-slo-recorded-not-asserted
title: "ADR-0029 — R1's 250 ms is a recorded SLO plus a deterministic ordinal, never a CI duration assertion"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [architecture, agent-plane, acp, performance, instrumentation, dc-107, testing-strategy]
links:
  - { to: architecture-agent-plane, rel: relates-to }
  - { to: note-conductor-latency-slo-not-assertion, rel: implements }
  - { to: note-conductor-n7-refactor-oracle, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Spec R1 requires run events within 250 ms of receipt, but this repo's fail-closed
  verify-perf-assertions.py gate refuses any test asserting a measured duration is under a
  constant - the control for DC-107, where exactly that shape of assertion broke deterministically
  on a slower CI runner while the code was correct. Phase 1 asserts a deterministic ordinal, emits
  and records the latency on the normal path, and evaluates the 250 ms figure as an operator SLO at
  the exit run with the host named.
---

# ADR-0029 latency-slo-recorded-not-asserted — R1's 250 ms is a recorded SLO, not a CI assertion

**Status:** Accepted · **Date:** 2026-09-09 (amended same day by Ruling 11) · **Deciders:** Owner
agent; disconfirmed and strengthened by the Test Architect against the defect-class register
**Context spec/architecture:** conductor spec R1 bullet 2, R7; `docs/architecture/agent-plane.md` §9;
`docs/lessons/defect-classes.md` DC-107

## Context

Spec R1 bullet 2: *"Tool calls, edits, messages, and permission requests from the lane appear as
normalized run events within 250 ms of receipt."* R7 similarly requires live fields at "≤ 2 s lag."

`tools/verify-perf-assertions.py` is a fail-closed CI gate whose rule is: *"A test may not assert
that a measured duration is under a constant."* It is the control for **DC-107**: a frame-budget
assertion (`Assert.True(p95 < 16.67)`) taken on a developer workstation failed deterministically on a
~3x slower CI runner while the underlying code was correct, stayed red for 38 runs, and took 26 other
gates down with it. A literal `elapsed < 250` in a test is therefore not a floor being thinned — it
is that registered defect class being re-created.

## Decision

**Assert a deterministic, host-independent ordinal as the required test**: the normalized run event
is observable **before** the client's response to the next inbound ACP request for the same call id.
Separately, **emit and record the per-event normalization latency on the normal path**, degrading to
`"not recorded"` — never `0` — when the receipt timestamp is absent. **Evaluate the spec's 250 ms
figure as an operator SLO** against that recorded measurement at the Phase-1 exit run, with the host
named, never as a CI verdict. The same treatment applies to R7's "≤ 2 s lag": record, do not assert;
R7's separate "within one event cycle" requirement **is** an ordinal and stays asserted.

## Options considered

1. **A literal duration assertion (`elapsed < 250`) — rejected.** Exactly DC-107's shape: a budget
   taken on one machine, asserted as a floor everywhere, breaks on a slower host with no code change.
2. **A loose `// perf-budget:` marker (e.g., 5 s) — rejected.** An assertion loose enough to be safe
   on any host is an assertion that cannot fail, which proves nothing — a Test Architect finding, not
   a control.
3. **Record only, assert nothing — the ruling's first pass, and incomplete.** The Owner's original
   ruling adopted this on the sub-claim that "a same-host relational comparison has no natural
   baseline and would be contrived." The Test Architect disconfirmed that sub-claim against DC-107's
   own control text, which explicitly permits *"a ratio between two values measured in the same
   process"* and generalizes: *"the cheapest fix is almost never a bigger threshold, it is an
   assertion that carries its own baseline."* A host-independent assertion **was** available.
4. **A deterministic ordinal, plus recording, plus the SLO evaluated at the exit run — chosen
   (Ruling 11, same-day amendment).** The ordinal has no guard band to mis-size, which is DC-107's
   actual discriminator; a same-process ratio is admitted as an *addition*, never a substitute, and
   only if its baseline and band are stated in writing.

## Consequences

- **Positive:** R1 bullet 2 has a required test that cannot fail on a slower CI runner while the code
  is correct, closing the exact DC-107 failure mode, while the spec's duration requirement is still
  answered — honestly, as a measured figure with its host, rather than silently dropped to
  telemetry-only.
- **Negative / accepted trade-offs:** the 250 ms figure carries **no enforcement power** — a breach
  at the exit run is a finding for the Owner, not a build failure. The recorded p50/p95 (0.022 ms /
  0.0641 ms on `TIMMALLSTRIX`, §9 of the architecture document) measure in-process normalization only
  and are explicitly **no evidence** about a loaded machine or a lane whose events cross a process
  boundary — carried forward as a residual, not resolved by this decision.
- **Follow-ups / new risks:** this amendment is itself a recorded instance of the disconfirmation
  loop working — an `Inferred` sub-claim ("no natural baseline exists") was carried into a ruling as
  settled and caught by the adversarial gate against the register, rather than discovered later in
  production. Any future host-independent ratio assertion in this area must state its baseline and
  band in writing per DC-107's own requirement.

## Evidence

- **Verified:** `tools/verify-perf-assertions.py`'s rule and its `--self-test`; `build.yml:162-166`;
  DC-107's control text at `docs/lessons/defect-classes.md:3994-4000`, read directly; the conductor
  spec's R1 bullet 2 and R7 text (spec lines cited in the ruling).
- **Ruling of record:** `docs/notes/conductor-latency-slo-not-assertion.md`, Owner agent, 2026-09-09,
  amended same day by Ruling 11 (Test Architect disconfirmation) and confirmed in Round 2 of
  `docs/notes/conductor-phase1-plan-approval.md`.
- **Exit-run measurement:** p50 0.022 ms, p95 0.0641 ms over 287 measured events, host `TIMMALLSTRIX`
  — `docs/proof/conductor-agent-plane.md`, row 4 of the four-point floor.
