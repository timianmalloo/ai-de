---
id: adr-0028-mode-cohort-not-partition
title: "ADR-0028 — mode is a cohort attribute, never a ScoreSegment partition axis"
type: adr
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [architecture, agent-plane, watcher, leaderboard, scoring, cohort, dm-data-modelling]
links:
  - { to: architecture-agent-plane, rel: relates-to }
  - { to: note-conductor-mode-cohort-not-partition, rel: implements }
  - { to: adr-0023-watcher-observation-projection, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  R4 forbids leaderboard cells splitting by mode, but the two ingest doors already defaulted to
  different task classes, so the same work would split anyway under a different column name -
  and the governed door's default was the incomparable one. Phase 1 adds mode as a nullable,
  expand-only cohort column beside ScoreSegment, never inside it, and requires an explicit
  caller-chosen taskClass at both doors.
---

# ADR-0028 mode-cohort-not-partition — mode is a cohort attribute, never a partition axis

**Status:** Accepted · **Date:** 2026-09-09 · **Deciders:** Owner agent, grounded in
`Leaderboard.cs:25-70`, `ScoringService.cs:75`, `WatcherHost.cs:118,146`, `ClosedEpisodeScoring.cs:69`
**Context spec/architecture:** conductor spec §8.4, R4 bullet 2; `docs/architecture/agent-plane.md`
§5, §4 (`LaneMode` / `LaneScoring`)

## Context

Spec R4 bullet 2 requires: *"Mode appears as a cohort attribute; leaderboard cells never split by
mode."* The existing partition key is `ScoreSegment(Workspace, TaskClass, SchemaVersion)`
(`Leaderboard.cs:25-70`); a comparison never crosses any of its three axes.

The two ingest doors already default to **different task classes**: audit import defaults to
`"audit-import"` (`WatcherHost.cs:118`); the registered-session sweep defaults to
`ScoreSegment.Unclassified` (`ClosedEpisodeScoring.cs:69`, `WatcherHost.cs:146`). So the same work was
already landing in different cells — the exact split R4 forbids, wearing the task-class column
instead of a mode column. It is worse than a simple two-cell split: `Unclassified` is
`IsComparable == false` (`Leaderboard.cs:66-70`), so a governed episode acquiring it by default would
rank **nowhere**, while an observed episode of the same work ranks in a real cell named after the
door it came through, not the work it represents.

## Decision

**Add `mode` as a nullable, expand-only cohort column on `scored_episode_cell`** (`RecordEpisodeMode`
/ `FindEpisodeMode`, schema v5→v6), living **beside** `ScoreSegment`, never inside it. **Require an
explicit, caller-chosen `taskClass` at both ingest doors** — the governed path (`LaneScoring`) has no
default, so an episode cannot silently acquire either door's task-class default. Prove, in one test,
that a governed and an observed episode carrying the **same** caller-chosen task class land in **one**
`ScoreSegment` cell as **two** `mode` cohorts.

## Options considered

1. **Add `mode` as a fourth `ScoreSegment` axis — rejected.** This is exactly the split R4 forbids,
   only spelled with a new column instead of the existing `TaskClass` default. A fourth partition
   axis would still separate governed from observed work into different cells; R4's requirement is
   that they **coexist in one cell**, distinguishable as a lens, not partitioned apart.
2. **Leave `mode` unrecorded and rely on the existing `TaskClass` defaults — rejected.** This is the
   status quo the ruling responds to: it silently ranks a governed episode in `Unclassified`
   (nowhere) or an ad-hoc comparable cell named after the importer, neither of which is the work's
   actual classification, and neither of which is visible as a defect until someone reads the board.
3. **`mode` as a nullable cohort column beside `ScoreSegment`, with `taskClass` made a required,
   caller-supplied argument at both doors — chosen.** `ScoringService.ScoreAndRecord` already
   requires `taskClass` from the caller with no default (`ScoringService.cs:75`); the doors' own
   default parameters were the actual defect, not the scoring contract. This needed **no production
   change to the scoring contract itself.**

## Consequences

- **Positive:** one `ScoreSegment` cell can hold both a governed and an observed episode of the same
  work, distinguished by `mode`, satisfying R4 bullet 2 as a proven positive shape rather than an
  absence claim (a negative "mode is never in the key" test would be a compile-time tautology and
  cannot fail for the right reason).
- **Negative / accepted trade-offs:** pre-existing `scored_episode_cell` rows read `mode` as `NULL`
  ("not recorded"), with **no backfill** — inferring "observed" from the absence of a session record
  would bucket a guess as history (DM: a backfill never guesses). Retiring the `"audit-import"`
  default itself is deferred to Phase 3 (spec §8.4 controlled task classes); changing it now would
  silently move existing observed episodes into `Unclassified`, a history-rule change with no spec
  basis.
- **Follow-ups / new risks:** `WatcherHost.cs:118` and `:146` still carry **two defaults for one
  concept** — one comparable (`"audit-import"`), one not (`Unclassified`) — recorded as a Phase-3
  finding, not resolved here. The composer's cohort minimum of 5 renders every cell holding only the
  two Phase-1 exit episodes `NotComparable`; the "one cell" claim in Phase 1 is proven at the
  `ScoreSegment`/store level, not through a rendered `LeaderboardComposer` rank.

## Evidence

- **Verified:** `Leaderboard.cs:25-70` (partition key and the `Unclassified`/`IsComparable` rule,
  read directly); `ScoringService.cs:75` (no default on `taskClass`); `WatcherHost.cs:118,146`
  (the two doors' defaults); `ClosedEpisodeScoring.cs:69` (governed door's prior default).
- **Migration proof:** `ScoredEpisodeModeMigrationTests` against a pre-migration fixture database —
  row count and scores byte-identical, `mode` reads `NULL`. Signed as expand-only by the Data &
  Persistence Architect per the ruling's Condition 4.
- **Ruling of record:** `docs/notes/conductor-mode-cohort-not-partition.md`, Owner agent, 2026-09-09,
  confidence Verified.
