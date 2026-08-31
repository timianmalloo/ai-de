---
id: design-watcher-dispute-service
title: "Loomkeeper - Raise-Dispute API, Sessions Badge & Cloud-Judge Scaffold (connective 7)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, dispute, sessions, badge, cloud-judge, adr-0018, phase-4]
links:
  - { to: design-watcher-score-dispute, rel: refines }
  - { to: design-watcher-sessions-surface, rel: refines }
  - { to: design-watcher-advisory-evaluator, rel: depends-on }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Close the US-16 fairness loop and make the model-judge seam concrete. DisputeService.RaiseDispute is the
  operator API that mints the dispute id + timestamp and appends the append-only fact (requiring a reason).
  A session is Disputed iff any of its episodes carries a dispute (DM7), surfaced as a no-colour-alone
  badge on the Sessions row and computed by the sessions query. DelegatingAdvisoryEvaluator is the
  cloud-judge scaffold: an IAdvisoryEvaluator that delegates the 0-4 rubric to an injected model call and
  is placed inside the EgressGuardedAdvisoryEvaluator, so the network call only happens after the ADR-0018
  egress opt-in + credential check pass.
---

# Design: Raise-Dispute API, Sessions Badge & Cloud-Judge Scaffold (connective 7)

## 1. Problem & scope

conn-4 built the dispute fact + store + a fleet-level count, but there was no API to *raise* a dispute,
no per-session discoverability (US-16 "from the Sessions view"), and the real cloud judge behind the
conn-3 egress guard was still only a spy in tests. This slice adds those three.

**In scope:** `DisputeService.RaiseDispute` (the raise API); the per-session derived Disputed state
(`DisputeProjection.IsSessionDisputed`) + the Sessions-row badge + the sessions-query computation;
`DelegatingAdvisoryEvaluator` (the cloud-judge scaffold). **Out of scope:** a WPF command/menu binding for
raise-dispute (the API is proven; the UI affordance is a thin follow-on); an actual model provider call
(the delegate is the injection point - a provider, prompt, and response validation per LOA A1-A3 are
future work); the dispute-resolution flow (a new Scorecard version).

## 2. RaiseDispute (the API)

`DisputeService.RaiseDispute(episodeId, operatorId, reason, dimension?)` mints the dispute id (a Guid by
default; injectable for tests) and the timestamp (from `TimeProvider`), and appends the `ScoreDispute`
fact. The reason is **required and trimmed** - a dispute with no stated reason is not an audit trail
(US-16). The append-only, never-overwrites guarantee stays in the store (conn-4); this API just stops a
caller hand-building an id or reaching past the store's contract.

## 3. Per-session Disputed state + badge (US-16 discoverability)

A dispute targets an *episode*; a *session* is Disputed iff any of its episodes carries a dispute.
`DisputeProjection.IsSessionDisputed(sessionId)` derives it (DM7 - never stored on the session) by
intersecting the session's episodes with the disputed-episode set. `WatcherSessionsQuery` computes it per
session, `WatcherSessionSnapshot` carries it (defaulted false so existing constructions are unaffected),
and `WatcherSessionRow` renders a **"⚠ Disputed"** badge - glyph + text, never colour alone (WCAG 2.2 AA)
- and a "has a disputed score" screen-reader phrase, so a disputed score is discoverable from the
Sessions view within one interaction.

## 4. Cloud-judge scaffold (the seam made concrete)

`DelegatingAdvisoryEvaluator(version, judge)` is an `IAdvisoryEvaluator` whose `Evaluate` delegates the
0-4 rubric to the injected `judge` function (clamping it and wrapping it with the version + rationale). A
real integration supplies `judge` as a call to a provider - grounded on the quarantined evidence,
validating the structured output (LOA A1-A3) - and places this evaluator **inside** the
`EgressGuardedAdvisoryEvaluator` (conn-3), so the network call only happens after the ADR-0018 egress
opt-in and credential check pass. This makes the one undetermined piece - the model call - a single
injected function, with everything around it (guarding, folding, calibration) already deterministic and
proven.

## 5. Failure modes & dispositions

| Failure mode | Disposition |
|---|---|
| Dispute with no reason | `ThrowIfNullOrWhiteSpace` - rejected (tested Theory) |
| Session with no disputed episode | `IsSessionDisputed` false; no badge (tested) |
| Adding Disputed to the snapshot breaks callers | Defaulted `bool Disputed = false` - existing constructions unaffected (967/0, 138/0) |
| Model returns an out-of-range rubric | Clamped to 0..4 (tested) |
| Model called before egress/credential | Guarded - the delegate never runs until opted-in + credentialed (tested) |

## 6. Test plan

- `DisputeServiceTests` (12): raise appends with generated id + timestamp; whole-score vs per-dimension;
  reason required (Theory) + trimmed; `IsSessionDisputed` true/false; row badge shown/omitted; the query
  marks a disputed session; the delegating evaluator clamps + delegates and, behind the egress guard,
  does not judge until opted-in + credentialed.
- The per-session derivation is mutation-verified (inverting the membership check reds it).
