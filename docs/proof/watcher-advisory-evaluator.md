---
id: proof-watcher-advisory-evaluator
title: "Proof Pack - Loomkeeper Local Advisory Evaluator & Egress Guard (connective 3)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, proof-pack, advisory, evaluator, egress, credential, adr-0018 credential-backed-grading-egress, phase-4]
links:
  - { to: design-watcher-advisory-evaluator, rel: tested-by }
  - { to: adr-0024-credential-backed-grading-egress, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that the advisory seam has a safe local implementation and an enforced egress boundary: the
  local heuristic scores the two advisory dimensions deterministically from a quarantined evidence token
  list, defaults conservatively for absent tokens (a missing signal can only lower a score), refuses a
  deterministic dimension (rule 8), and is stable over 20 repeats; and the egress guard denies a
  non-opted-in path (LK-0003) and a missing credential (LK-0002) - egress checked first - never calling
  the inner cloud evaluator when either fails, and delegating only when both hold. 15 tests, full suite
  928/0, the egress-first ordering mutation-verified.
---

# Proof Pack: Local Advisory Evaluator & Egress Guard (connective 3)

- **Components:** `src/AiDe.Core/Watcher/AdvisoryEvaluators.cs` (`IAdvisoryCredentialSource`, `NoCredential`, `LocalHeuristicAdvisoryEvaluator`, `EgressGuardedAdvisoryEvaluator`, `EvidenceTokens`).
- **Tests:** `tests/AiDe.Core.Tests/Watcher/AdvisoryEvaluatorsTests.cs` — 15 tests, **15/15**; full `AiDe.Core.Tests` suite **928/0**; build clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| Full evidence scores the top rubric | `Local_EvidenceDiscipline_FullEvidence_ScoresTop` | `LocalHeuristicAdvisoryEvaluator` | verification+coverage≥0.9 → 4 | Seen green | Verified | — |
| No verification + low coverage scores zero | `Local_EvidenceDiscipline_NoVerification_LowCoverage_ScoresZero` | rubric rules | 0 | Seen green | Verified | — |
| Partial coverage scores between | `Local_EvidenceDiscipline_PartialCoverage_ScoresBetween` | coverage band | 3 (2+1) | Seen green | Verified | — |
| Absent tokens score conservatively, never optimistically (NG1) | `Local_AbsentTokens_ScoreConservatively_NotOptimistically` | conservative defaults | empty evidence → 0 (discipline), < 4 (economy) | Seen green | Verified | a missing signal only lowers |
| A lean run scores the top economy rubric | `Local_SolutionEconomy_LeanRun_ScoresTop` | economy rules | 0 after-done + not premature + reuse → 4 | Seen green | Verified | — |
| A wasteful, premature run scores low | `Local_SolutionEconomy_WastefulAndPremature_ScoresLow` | economy rules | many after-done + premature → 0 | Seen green | Verified | — |
| A deterministic dimension is refused (rule 8) | `Local_ADeterministicDimension_IsRefused` (Theory ×4) | dimension guard | InvalidBinding for Outcome/Focus/Guidance/Coordination | Seen green | Verified | the deterministic scorer owns those |
| The local evaluator is deterministic; stability passes | `Local_IsDeterministic_StabilityTriviallyPasses` | 20 repeats | identical band; `EvaluatorStability.Of` passes | Seen green | Verified | ties to ADR-0019 advisory-evaluator-calibration gate (a) |
| A non-opted-in egress path is denied; inner never runs | `Guard_EgressBlocked_IsDenied_AndInnerNeverRuns` | `EgressGate` default-deny | EgressDenied (LK-0003); spy not called | **Yes** — neutralising the egress check reds this | Verified | default-deny (ADR-0024 credential-backed-grading-egress) |
| Egress allowed but no credential is denied; inner never runs | `Guard_EgressAllowed_ButNoCredential_IsInvalidBinding_AndInnerNeverRuns` | credential guard | InvalidBinding (LK-0002); spy not called | Seen green | Verified | presence check, not the secret |
| Egress allowed + credential delegates to the inner judge | `Guard_EgressAllowed_WithCredential_DelegatesToInner` | delegation | inner called; version delegates | Seen green | Verified | the real judge sits here |
| A revoked path returns to denied | `Guard_RevokedPath_ReturnsToDenied` | `EgressGate.Revoke` | EgressDenied; spy not called | Seen green | Verified | opt-in is revocable |

## Testing Strategy triggers applied

- **T1 (pure deterministic logic):** the local heuristic and the token parser are pure functions; unit-tested across the rubric bands and the conservative-default boundary.
- **Security negative tests (STRIDE - Elevation/Information disclosure):** the egress boundary is proven with **negative tests written to fail first** - a blocked path and a missing credential each throw the stable error code AND the inner (egressing) evaluator is asserted *not* to have run. This is the Security & Identity concern (ADR-0024 credential-backed-grading-egress) made real: default-deny egress, presence-only credential, egress-before-credential ordering.
- **A6 (contract/version):** the evaluator version is part of the calibration registry key (slice 7); the guard delegates the version so a change to the inner judge is a registry-gated contract change.
- **T1 mutation sense:** the egress-first check (the boundary's load-bearing guard) was neutralised, observed to red the blocked-egress test, then reverted. Enum/`TreatWarningsAsErrors` cover the compile-enforced class.
- **D0 hygiene:** deterministic (`UnixEpoch`), isolated, focal-call + meaningful assertion; the spy inner proves the "never called" claim rather than merely the thrown code.

## Security / privacy note

- **Default-deny egress (ADR-0024 credential-backed-grading-egress):** no advisory model call can leave the machine unless an operator opts the exact path in; the local heuristic never egresses and is the default.
- **Presence-only credential:** `IAdvisoryCredentialSource` authorises on *presence*; the secret is never stored in the watcher store (architecture §4) and is resolved at the call by the credential-backed transport.
- **Untrusted evidence:** the evidence token list is quarantined data the local evaluator *reads by fixed rules*, never executes; an unknown token is ignored, not interpreted.

## Residual risk

- **No real cloud judge** — `EgressGuardedAdvisoryEvaluator`'s inner is a seam; a production model call (provider, prompt, credential-backed transport, response parsing/validation per A1-A3) is genuinely blocked on operator-provided credentials and remains future work. The boundary that would contain it is proven now.
- **Evidence composition** — the caller that turns `DeterministicEpisodeSignals` into the evidence token string is not yet wired (the ingest/scoring follow-on); the local evaluator's contract (the token vocabulary) is fixed and tested.
- **Calibration of the local heuristic** — the local evaluator's assessments still require the ADR-0019 advisory-evaluator-calibration QWK-vs-human gate to fold into points; a human-label set for the local heuristic is not curated.
