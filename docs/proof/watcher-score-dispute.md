---
id: proof-watcher-score-dispute
title: "Proof Pack - Loomkeeper Operator Dispute Path (connective 4)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, proof-pack, dispute, fairness, append-only, us-16, phase-4]
links:
  - { to: design-watcher-score-dispute, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that the operator dispute path meets US-16 / rule 12: a ScoreDispute is an append-only fact
  that never overwrites the Scorecard (prior score preserved); it round-trips whole-score and
  per-dimension on both stores and persists across a reopen; the SQLite fact rejects UPDATE/DELETE (DM11)
  and ignores a duplicate id idempotently; the Disputed state is derived from the facts (DM7); and the
  Leaderboard surfaces the disputed-episode count so a disputed score is discoverable (US-16). 11 tests,
  full suite 939/0, App 138/0; the append-only/idempotent oracle mutation-verified.
---

# Proof Pack: Operator Dispute Path (connective 4)

- **Components:** `src/AiDe.Core/Watcher/ScoreDispute.cs` (`ScoreDispute`, `DisputeProjection`); `IWatcherObservationStore.{AppendScoreDispute,DisputesForEpisode,AllDisputes}` (both stores + `score_dispute_fact` table/triggers); `WatcherLeaderboardPaneViewModel` disputed-count surfacing + `IWatcherDisputeQuery`/`WatcherDisputeQuery`; `SurfaceContentFactory`/`WorkbenchShell` wiring.
- **Tests:** `tests/AiDe.Core.Tests/Watcher/ScoreDisputeTests.cs` — 11 tests, **11/11**; full `AiDe.Core.Tests` **939/0**, `AiDe.App.Tests` **138/0**; builds clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| A dispute is recorded and read back (in-memory) | `InMemory_AppendThenRead_ReturnsTheDispute` | `AppendScoreDispute` | reason preserved | Seen green | Verified | — |
| A dispute round-trips whole-score and per-dimension (SQLite) | `Sqlite_AppendThenRead_RoundTrips_WholeScoreAndPerDimension` | nullable dimension | null vs EvidenceDiscipline preserved | Seen green | Verified | — |
| A dispute persists across a real reopen | `Sqlite_Dispute_PersistsAcrossReopen` | reopen the file | present after close+reopen | Seen green | Verified | — |
| A dispute NEVER overwrites the Scorecard (rule 12) | `Dispute_NeverOverwritesTheScorecard_PriorScorePreserved` | separate table | Weave stays 84; dispute recorded alongside | Seen green | Verified | prior scores preserved |
| The dispute fact rejects UPDATE and DELETE (DM11) | `Sqlite_DisputeFact_IsAppendOnly_UpdateIsRejected`, `..._DeleteIsRejected` | append-only triggers | `SqliteException` "append-only" | Seen green | Verified | — |
| A duplicate dispute id is ignored idempotently | `Sqlite_DuplicateDisputeId_IsIgnoredIdempotently` | `INSERT OR IGNORE` | single row, first write wins | **Yes** — `OR IGNORE`→`OR REPLACE` trips the trigger and reds this | Verified | mutation-verified oracle |
| `AllDisputes` returns across episodes in raise order | `AllDisputes_ReturnsAcrossEpisodes_InRaiseOrder` | ordered read | ep-1 (t+1) before ep-2 (t+5) | Seen green | Verified | — |
| The Disputed state is derived from the facts (DM7) | `DisputeProjection_DerivesTheDisputedState_FromTheFacts` | `DisputeProjection` | IsDisputed/Count/Ids correct; undisputed false | Seen green | Verified | never a stored flag |
| The Leaderboard surfaces the disputed-episode count (US-16) | `LeaderboardPane_SurfacesTheDisputedCount` | pane fold | "2 disputed episode(s)" for 2 disputed of 5 | Seen green | Verified | discoverable from the surface |
| No dispute query omits the count | `LeaderboardPane_NoDisputeQuery_ShowsNoDisputedCount` | null dispute query | no "disputed" in status | Seen green | Verified | — |

## Testing Strategy triggers applied

- **T4 (real-infra integration):** the SQLite dispute tests run against the real engine, incl. the append-only trigger and reopen semantics only the real engine exhibits.
- **DM11 (append-only invariant, tested):** a test *attempts* the forbidden UPDATE and DELETE and asserts they abort — the invariant is enforced by the engine, not by discipline.
- **DM7 (derive-don't-store):** the Disputed state is computed from the dispute facts by `DisputeProjection`; there is no stored disputed flag on the score that could drift.
- **Rule 12 (non-overwrite):** proven directly — a dispute after a recorded score leaves the score's Weave unchanged.
- **T1 mutation sense:** the append-only + idempotent write (`INSERT OR IGNORE`) was mutated to `INSERT OR REPLACE`, observed to trip the append-only trigger and red the idempotent test, then reverted.
- **D0 hygiene:** deterministic (`UnixEpoch`), isolated (fresh temp DB per test), focal-call + meaningful assertion.

## Security / privacy note

- **Immutable audit of dissent:** a dispute, once recorded, cannot be altered or deleted (append-only triggers) — the record of *why* a score was contested is tamper-evident.
- **Non-secret content:** the dispute reason is the operator's own words, a non-secret fact, consistent with the store holding non-secret facts only (architecture §4).

## Residual risk

- **Resolution flow not implemented** — a dispute records the contest; producing a *new Scorecard version* from deterministic evidence or a human disposition (rule 12's "superseding evaluation record", and the deterministic-evidence/human-disposition-wins rule) is a larger follow-on.
- **No raise-dispute UI command** — the API + persistence + discoverability are proven; a per-episode UI affordance to *raise* a dispute (a command/menu bound to `AppendScoreDispute`) is a further step.
- **Dispute-overturn-rate metric** — the US-8 promotion gate's "dispute-overturn-rate no worse" counter-metric is not yet computed from these facts.
- **Per-episode Disputed badge** — the surfacing is a fleet-level count on the Leaderboard; a per-row/per-session Disputed badge (US-16 "within one interaction") is a light follow-on now that the derived state exists.
