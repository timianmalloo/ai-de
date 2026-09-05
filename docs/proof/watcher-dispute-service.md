---
id: proof-watcher-dispute-service
title: "Proof Pack - Loomkeeper Raise-Dispute API, Sessions Badge & Cloud-Judge Scaffold (connective 7)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, proof-pack, dispute, sessions, cloud-judge, phase-4]
links:
  - { to: design-watcher-dispute-service, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that the US-16 fairness loop closes and the model-judge seam is concrete: RaiseDispute mints
  the id + timestamp and appends the fact (requiring a trimmed reason); a session is Disputed iff any of
  its episodes carries a dispute (DM7), shown as a no-colour-alone Sessions badge and computed by the
  query; and the DelegatingAdvisoryEvaluator clamps + delegates the rubric and, behind the ADR-0018 credential-backed-grading-egress
  egress guard, does not judge until opted-in and credentialed. 12 tests, Core 967/0, App 138/0; the
  per-session derivation mutation-verified.
---

# Proof Pack: Raise-Dispute API, Sessions Badge & Cloud-Judge Scaffold (connective 7)

- **Components:** `src/AiDe.Core/Watcher/DisputeService.cs` (`DisputeService.RaiseDispute`, `DelegatingAdvisoryEvaluator`); `DisputeProjection.IsSessionDisputed`; `WatcherSessionSnapshot`/`WatcherSessionRow`/`WatcherSessionsQuery` Disputed badge.
- **Tests:** `tests/AiDe.Core.Tests/Watcher/DisputeServiceTests.cs` — 12 tests, **12/12**; full `AiDe.Core.Tests` **967/0**, `AiDe.App.Tests` **138/0**; builds clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| RaiseDispute appends with a generated id + timestamp | `RaiseDispute_AppendsTheFact_WithGeneratedIdAndTimestamp` | `DisputeService` | id "d-1", RaisedAt = clock now, whole-score, stored | Seen green | Verified | — |
| A dispute can target one dimension | `RaiseDispute_CanTargetOneDimension` | `dimension` param | DisputedDimension = SolutionEconomy | Seen green | Verified | — |
| A blank reason is rejected | `RaiseDispute_RequiresANonBlankReason` (Theory ×2) | `ThrowIfNullOrWhiteSpace` | empty/whitespace → ArgumentException | Seen green | Verified | a reason is the audit trail |
| The reason is trimmed | `RaiseDispute_TrimsTheReason` | `reason.Trim()` | "  padded  " → "padded reason" | Seen green | Verified | — |
| A session is Disputed iff an episode is | `IsSessionDisputed_TrueWhenOneOfItsEpisodesIsDisputed` | `DisputeProjection` | s1 (disputed ep) true, s2 false | **Yes** — inverting the membership check reds this | Verified | mutation-verified oracle |
| The Sessions row shows the Disputed badge (no colour alone) | `SessionRow_Disputed_ShowsTheBadge_NoColourAlone` | `WatcherSessionRow` | "⚠ Disputed" in label, "disputed score" in a11y name | Seen green | Verified | US-16 discoverable, WCAG 2.2 AA |
| An undisputed row omits the badge | `SessionRow_NotDisputed_OmitsTheBadge` | render rule | no "⚠ Disputed" | Seen green | Verified | — |
| The sessions query marks a disputed session | `WatcherSessionsQuery_MarksASessionDisputed_WhenOneOfItsEpisodesIs` | `WatcherSessionsQuery` | snapshot.Disputed true | Seen green | Verified | the exact query the pane folds |
| The delegating evaluator clamps + delegates the rubric | `DelegatingEvaluator_DelegatesTheRubric_AndClampsIt` | `DelegatingAdvisoryEvaluator` | judge returns 9 → clamped 4; version carried | Seen green | Verified | the model-call injection point |
| Behind the guard, it does not judge until opted-in + credentialed | `..._DoesNotJudgeUntilOptedInAndCredentialed`, `..._JudgesOnceOptedInAndCredentialed` | `EgressGuardedAdvisoryEvaluator` | blocked → throws, judge not called; opted-in+cred → judged, rubric 3 | Seen green | Verified | ADR-0018 credential-backed-grading-egress boundary around the real judge |

## Testing Strategy triggers applied

- **T1 (pure/deterministic):** the raise API, the per-session derivation, the row render, and the delegating evaluator are unit-tested across their boundaries.
- **Security composition (ADR-0018 credential-backed-grading-egress):** the cloud-judge scaffold is tested *inside* the egress guard - the delegate (the network call) provably does not run until the egress opt-in and credential checks pass, and the "not called" claim is proven by a flag, not just the thrown code.
- **UI (U9/U16):** the Sessions badge is glyph+text (no colour alone) with a screen-reader phrase; shown/omitted states both tested; the defaulted field keeps every existing Sessions construction green.
- **T1 mutation sense:** the per-session dispute derivation (the US-16 discoverability guarantee) was mutated by inverting the membership check, observed to red, then reverted.
- **D0 hygiene:** deterministic (`FixedTimeProvider`, injected id), isolated, focal-call + meaningful assertion.

## Security / privacy note

- **Immutable dissent + a required reason:** a raised dispute is append-only (conn-4) and must carry a reason, so the record of *why* a score was contested is tamper-evident and never empty.
- **The real judge stays behind the guard:** `DelegatingAdvisoryEvaluator` never egresses by itself; egress is the guard's job (default-deny + credential, ADR-0018 credential-backed-grading-egress). The scaffold makes the seam concrete without opening a network path.

## Residual risk

- **No WPF raise-dispute command yet** — `RaiseDispute` is the API a menu/command binds to; the UI affordance (and a per-episode drill-down) is a thin follow-on.
- **No real model provider** — the `judge` delegate is the injection point; a provider call with prompt + structured-output validation (LOA A1-A3) and its credential-backed transport is future work. The boundary that contains it is proven.
- **Session Disputed cost** — `IsSessionDisputed` folds all disputes per query; at very large scale a disputed-session index would be cheaper (the read is correct, not yet optimized).
