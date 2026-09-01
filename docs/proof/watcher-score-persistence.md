---
id: proof-watcher-score-persistence
title: "Proof Pack - Loomkeeper Scorecard & Leaderboard Persistence (connective 1)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, proof-pack, persistence, scorecard, leaderboard, materialized-cache, phase-4]
links:
  - { to: design-watcher-score-persistence, rel: tested-by }
  - { to: design-watcher-weave-score, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that a scored episode persists as a materialized derived cache (DM7) behind
  IWatcherObservationStore: the in-memory and real SQLite stores return an equal card; the persisted
  card equals the value WeaveScorer produced (persisted == in-memory == derived); a recompute upserts
  and leaves no stale dimension/floor child rows; null Coverage round-trips as null not zero; and
  AllScoredEpisodes() feeds LeaderboardComposer through to a comparable cell. 9 tests, full suite 897/0,
  the child-cleanup oracle mutation-verified.
---

# Proof Pack: Scorecard & Leaderboard Persistence (connective 1)

- **Components:** `IWatcherObservationStore` (+3 methods: `RecordScorecard`, `FindScoredEpisode`, `AllScoredEpisodes`), `InMemoryWatcherObservationStore`, `SqliteWatcherObservationStore` (tables `scored_episode_cell` + `score_dimension_cell` + `score_tripped_floor_cell`).
- **Tests:** `tests/AiDe.Core.Tests/Watcher/ScorePersistenceTests.cs` — 9 tests, **9/9**; full `AiDe.Core.Tests` suite **897/0**; build clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| An in-memory recorded card is returned equal on find | `InMemory_RecordThenFind_ReturnsEqualCard` | `RecordScorecard`/`FindScoredEpisode` | field-wise card equality (record list equality is referential, so compared explicitly) | Seen green | Verified | — |
| A SQLite recorded card is returned equal on find | `Sqlite_RecordThenFind_ReturnsEqualCard` | real SQLite (D4) | same field-wise equality across the DB round-trip | Seen green | Verified | — |
| Persisted == in-memory == derived (DM7(c)) | `Sqlite_PersistedCard_EqualsScorerOutput_AndInMemory` | `WeaveScorer.Score` output | all three agree for the same input; headline preserved | Seen green | Verified | the cache is faithful to its derivation |
| A scored episode survives a real reopen | `Sqlite_ScoredEpisode_PersistsAcrossReopen` | reopen the file | card equal after close+reopen (a real restart, not a field) | Seen green | Verified | — |
| A recompute upserts and leaves no stale child rows | `Sqlite_Recompute_Upserts_AndLeavesNoStaleDimensionRows` | transactional delete-children + insert | Blocked 2-dim+floor → Partial 1-dim → exactly one assessment, no floors | **Yes** — neutralizing `DELETE FROM score_dimension_cell` reds this | Verified | mutation-verified oracle |
| Null Coverage round-trips as null, not zero | `Sqlite_NullCoverage_RoundTripsAsNull_NotZero` | nullable coverage columns | NotScored card with null Coverage reads back null | Seen green | Verified | unknown ≠ 0 |
| The store read feeds the leaderboard composer (E11 compute reader) | `Sqlite_AllScoredEpisodes_FeedsLeaderboardComposer` | `LeaderboardComposer.Compose` | 5 persisted episodes over 2 operators → HarnessModel cell cohort 5, Comparable | Seen green | Verified | the exact path the WPF surface uses (conn-2) |
| Empty store returns no scored episodes / null find | `Empty_ReturnsNoScoredEpisodes` | both stores | empty list + null; no throw | Seen green | Verified | — |

## Testing Strategy triggers applied

- **T4 (real-infra integration):** the SQLite tests run against the real engine (`SqliteWatcherObservationStore.Open` on a temp file), not a substitute — only the real engine exhibits the transaction/UPSERT and reopen semantics. In-memory and SQLite share the same contract assertions.
- **DM7 (derive-don't-store, cache):** the persisted Scorecard is a materialized derived cache — labelled (`*_cell`, "materialized derived cache" doc), rebuildable (from episode+signals via `WeaveScorer`), and proven equal to its derivation (`Sqlite_PersistedCard_EqualsScorerOutput_AndInMemory`).
- **T1 mutation sense:** the child-cleanup transaction (the one place a recompute could leave stale rows) was mutated to delete nothing, observed to red `..._LeavesNoStaleDimensionRows`, then reverted.
- **D0 hygiene:** deterministic (`FixedTimeProvider(UnixEpoch)`), isolated (a fresh temp DB per test), focal-call + meaningful assertion on every test.

## Data-model note (grain & history)

One row of `scored_episode_cell` is exactly one scored episode (current state). A recompute is a **cache refresh** (UPSERT + child replace in one transaction), not a history rewrite, so the cell carries no append-only trigger — deliberately unlike `observed_span_fact` / `board_message_fact`, which are append-only observation facts. The score is derived, so historising it would be storing a second definition of a computable quantity (DM7 violation).

## Residual risk

- **Signals not persisted** — the `DeterministicEpisodeSignals` that feed the scorer are not stored; rebuildability is proven by re-running the scorer over the same inputs, not by reload-then-recompute. Persisting the signals (so a card can be rebuilt from the DB alone) is a follow-on.
- **No recompute-on-read** — the store serves the materialized card; a schema-version bump of the scorer would require a re-score pass, not yet wired.
- **Leaderboard/standing not themselves persisted** — they are composed on demand from `AllScoredEpisodes()`; that is intentional (cheap, always-fresh) but means a very large fleet recomputes the leaderboard per read.
