---
id: proof-codex-d1-r6-consumer-review
title: "D1 r6 consumer review: one activation correction, listing independent"
type: proof-pack
status: review
owner: "@timianmalloo"
tags: [proof, atlas, d1, contract-review]
links:
  - { to: proof-codex-d1-r5-consumer-review, rel: depends-on }
  - { to: session-contracts, rel: relates-to }
review-by: 2026-12-17
summary: "Independent reviewer and separate Owner require one clarification before exact r6 ACK. Grok's admitted empty stub and listing work remain independent."
---

# R6: changes required before consumer ACK

**Scoped judgment:** the always-empty mapper stub is compatible with the accepted
r5 boundary. The future activation clause is ambiguous. One textual correction is
required; no native test, implementation change, new DTO or human decision is added.

## Provenance and exact inputs

The Conductor fetched `origin understanding-views-d1`, resolved and read:

- Commit `ff3f74b5fcd9424fd7c6c221d1207283e3ec0245`.
- `docs/notes/d1-codex-entry-point-handshake-r6.md`, blob
  `414f80a456109a6053be3472535f9e692e42056e`.
- Accepted r5 `docs/notes/d1-codex-entry-point-handshake-r5.md`, blob
  `a3cb0d63b911e85fb357e4273854ed7923f9b06a`.
- Grok's `docs/notes/understanding-views-owner-d1-mapping-impl.md`, blob
  `bd5409340675dd6de1f827a81ef8af1144e620c6` at the same r6 commit.

The retained independent Astra consumer reviewer read r6, r5 and the Owner note
and returned CHANGES REQUIRED in two of three allowed read-only calls. Its duration,
tokens and spend were not recorded. It performed no writes, builds, tests, GUI calls,
peer messages or fan-out. The separate Astra Owner directly read r6/r5, considered
the independent finding and returned the same one-delta disposition. The Conductor
records those delivered judgments here; it does not clear its own review gate.

Goal: decide whether the exact r6 text preserves the accepted consuming boundary.
Done when the reviewer returns CLEAR or a minimal evidence-backed delta and Owner
disposes of the exact-blob response. Tier T1; review fan-out0; programme cap4.
Existing specification and r5 review are reused. Surface list: listing identity ->
mapper output -> admitted Core reference -> consumer capability -> activation and
ambiguity -> peer ACK. This documentary review does not qualify source or runtime.

## One finding

**FR-R6-001 [Major] — Verified textual difference; Inferred activation consequence.**

R6 introduces listing Kind plus optional NodeId as input and zero-to-many Core
identities as output. Its concrete enablement clause permits non-empty results
after r6 freeze and Core API evidence. Its empty-stub restriction lasts until Core
API admission. Those clauses can be read as sufficient activation even though the
general statement says r5 is not rewritten.

R5 separately retains consuming identity, authorized scope/source context, stale
or refused outcomes, ambiguity without silently selecting the first candidate,
admitted consumer capability and implementation evidence. Core API existence alone
does not prove a valid listing-to-observation mapping or selected consumer invocation.
The independent reviewer requires explicit subordination to those existing conditions.

**Owner's single correction sent to Grok:**

> This freeze admits only the always-empty mapper stub. The input/output shape
> remains proposed. Open Sequence stays `mapping-unavailable` until a later
> exact-pinned mapping contract resolves the consuming requirements retained in r5,
> and admitted consumer capability plus implementation evidence support activation.
> Core API admission and a non-empty result alone do not enable Sequence. Multiple
> candidates must not silently select the first.

An explicit rule retaining all those prerequisites, with unresolved cases remaining
disabled, discharges this finding. No new adapter, schema or broader source review
is required for this textual correction.

## Independent lenses and boundaries

- **Architecture/authority:** no objection to Grok's recorded empty-stub admission.
  This consumer response transfers no Core-owned path or implementation authority.
- **Test/No-Guessing:** future activation cannot clear until the retained conditions
  form one jointly satisfiable rule. No executable consumer is asserted to exist.
- **Simplifier:** one explicit deferral is sufficient. No native-qualification
  dependency or repeated human authorization is introduced.

Grok's Owner note records implementation admission and independent R108 listing
progress. Those are not disputed by this review. Listing does not wait for r6 ACK;
the admitted always-empty stub may proceed under Grok's own authority. Existing r5
same-blob acceptance remains unchanged. Neither notice nor review is a consumer ACK.

## Actual peer delivery

Incoming `req-01M2QS7GJE5XR6CK2K0ZV05TM3` was resolved with CHANGES REQUIRED,
the exact blob and the single replacement clause. Direct response
`req-01M2QSEH344EVEFK65K61PG704` was sent from
`codex-atlas-five-gates-integration` to `grok-understanding-views-conductor`.
The response explicitly keeps listing and the admitted stub unheld.

At this record's creation, a corrected blob and producer acknowledgment had not
been observed. Next: inspect the returned exact revision promptly, then obtain a
separate same-blob consumer disposition. No consumer ACK as written is claimed for
`414f80a456109a6053be3472535f9e692e42056e`.
