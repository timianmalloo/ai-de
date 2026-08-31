---
id: design-watcher-score-persistence
title: "Loomkeeper - Scorecard & Leaderboard Persistence (connective 1)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, persistence, scorecard, leaderboard, materialized-cache, sqlite, phase-4]
links:
  - { to: design-watcher-weave-score, rel: depends-on }
  - { to: design-watcher-advisory-grader, rel: depends-on }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0002-workspace-fact-store, rel: depends-on }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Persist a scored episode (ScoredEpisode + its Scorecard) as a MATERIALIZED DERIVED CACHE behind the
  existing IWatcherObservationStore seam, so the WPF Leaderboard/Standing surfaces read scored data
  without recomputing. The cache is a current-state cell (upsert, not append-only) because a
  recomputation must replace the prior card; it is rebuildable from (episode + signals) via WeaveScorer
  (DM7), and a round-trip test asserts persisted == in-memory == the value the scorer produced.
---

# Design: Scorecard & Leaderboard Persistence (connective 1)

## 1. Problem & scope

Slices 5 and 7 built the deterministic Weave scorer, the advisory fold, the `LeaderboardComposer` and the
`StandingComposer` as **pure engines** over `IReadOnlyList<ScoredEpisode>`. Nothing persists a
`ScoredEpisode`, so a restart loses every score and the WPF surfaces (connective 2) have nothing durable
to read. This slice adds persistence for scored episodes behind the existing `IWatcherObservationStore`
seam, in both the in-memory and SQLite implementations, and exposes the read that the composers consume.

**In scope:** three interface methods; the SQLite tables + child rows; a faithful round-trip; a
rebuildability assertion. **Out of scope:** the WPF surfaces (connective 2), the advisory evaluator
(connective 3), disputes (connective 4), and computing the signals (that remains the ingest follow-on).

## 2. The data-model decision (DM7 - derive, don't store)

A `Scorecard` is a **derived** value: `WeaveScorer.Score(episode, signals)`. Persisting it is therefore
storing a derived quantity, which DM7 permits **only** as a cache that is (a) labelled a cache, (b)
rebuildable from its inputs, and (c) covered by a test proving it equals its derivation. All three hold:

- **(a) Labelled.** The tables are named `*_cell` (current-state), the interface XML-doc says
  "materialized derived cache", and the store method is `RecordScorecard` (record a computed result),
  not a fact append.
- **(b) Rebuildable.** The inputs are the `WorkEpisode` (already persisted, slice 4) and the
  `DeterministicEpisodeSignals` (ingest-derived). Re-running the scorer over the same inputs reproduces
  the card; slice 5's 27 tests already pin that derivation. This slice does not re-derive on read - it
  serves the materialized card - but the card **can** be rebuilt, which is what DM7 requires.
- **(c) Tested.** `RoundTrip_PersistedScoredEpisode_EqualsInMemory` asserts the SQLite read equals the
  in-memory store's value for the same input, and `Persisted_EqualsScorerOutput` asserts both equal the
  value `WeaveScorer.Score` produced - persisted == in-memory == derived.

**Grain.** One row of `scored_episode_cell` is exactly one scored episode (one `episode_id`), current
state. A recomputation **upserts** (replaces) the row and its children - this is a cache refresh, not a
history rewrite, so it is a current-state cell (like `session_heartbeat`), **not** an append-only fact,
and carries no append-only trigger. The `evaluated_at` column records when the card was computed.

## 3. Contract (the seam extension)

```csharp
// IWatcherObservationStore - three additions:

/// Records (upserts) a scored episode as a materialized derived cache (DM7). Replaces any prior card.
void RecordScorecard(ScoredEpisode scored);

/// The materialized scored episode for an id, or null if none has been computed.
ScoredEpisode? FindScoredEpisode(string episodeId);

/// Every materialized scored episode - the compute reader for the leaderboard & standing (US-14/US-16).
IReadOnlyList<ScoredEpisode> AllScoredEpisodes();
```

The WPF Leaderboard surface (connective 2) reads `AllScoredEpisodes()` and folds it through
`new LeaderboardComposer().Compose(...)` and `StandingComposer` - the composers are unchanged.

## 4. SQLite schema (added to SchemaSql - pre-release, no migration)

```sql
-- Materialized scored-episode cache (DM7). Current-state cell: a recompute UPSERTs. NOT append-only.
CREATE TABLE scored_episode_cell (
    episode_id        TEXT    NOT NULL PRIMARY KEY,
    harness           TEXT    NULL,
    model             TEXT    NULL,
    operator_id       TEXT    NOT NULL,
    task_class        TEXT    NOT NULL,
    schema_version    TEXT    NOT NULL,
    verdict           TEXT    NOT NULL,
    headline          TEXT    NOT NULL,
    coverage_observed INTEGER NULL,
    coverage_required INTEGER NULL,
    evaluated_at      TEXT    NOT NULL
);
CREATE INDEX ix_scored_episode_task ON scored_episode_cell (task_class, schema_version);

-- Per-dimension child cells (composed back into the Scorecard on read).
CREATE TABLE score_dimension_cell (
    episode_id    TEXT    NOT NULL,
    dimension     TEXT    NOT NULL,
    weight        INTEGER NOT NULL,
    rubric        INTEGER NULL,
    earned_points REAL    NULL,
    posture       TEXT    NOT NULL,
    rationale     TEXT    NOT NULL,
    PRIMARY KEY (episode_id, dimension)
);

-- Tripped-floor child cells.
CREATE TABLE score_tripped_floor_cell (
    episode_id TEXT NOT NULL,
    floor      TEXT NOT NULL,
    PRIMARY KEY (episode_id, floor)
);
```

The upsert deletes the child rows for the episode and reinserts them inside one transaction, so a
recompute never leaves stale dimension/floor rows.

## 5. Failure modes & dispositions

| Failure mode | Disposition |
|---|---|
| Re-record after a recompute leaves stale child rows | Upsert wraps delete-children + insert-all in one transaction (tested) |
| An unknown enum string in the DB (verdict/posture/dimension/floor) | Parsed with `Enum.Parse`; a malformed value throws on read (fail-loud, not silently wrong) |
| Coverage null vs zero | `coverage_observed/required` are nullable; null Coverage round-trips as null, not 0 (tested) |
| Concurrent writers | Single connection under `_gate`, mirroring the existing store (ADR-0002 single writer) |
| Reading before any score computed | `AllScoredEpisodes()` returns empty; `FindScoredEpisode` returns null (tested) |

## 6. Test plan

- `RecordScorecard_ThenFind_ReturnsEqualCard` (in-memory & SQLite via a shared theory)
- `RoundTrip_PersistedScoredEpisode_EqualsInMemory` (SQLite read == in-memory read for same input)
- `Persisted_EqualsScorerOutput` (both == `WeaveScorer.Score` output - DM7(c))
- `Recompute_UpsertsAndLeavesNoStaleDimensionRows`
- `NullCoverage_RoundTripsAsNull_NotZero`
- `AllScoredEpisodes_FeedsLeaderboardComposer` (the compute reader path, E11-style through the composer)
- `Empty_ReturnsNoScoredEpisodes`
