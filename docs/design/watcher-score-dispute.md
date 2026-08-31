---
id: design-watcher-score-dispute
title: "Loomkeeper - Operator Dispute Path (connective 4)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, dispute, fairness, append-only, us-16, phase-4]
links:
  - { to: design-watcher-score-persistence, rel: depends-on }
  - { to: design-watcher-board-leaderboard-surfaces, rel: refines }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0002-workspace-fact-store, rel: depends-on }
review-by: 2027-02-28
review-suggested: []
summary: >-
  The operator dispute path (US-16 / spec rule 12): an operator records a ScoreDispute against a scored
  episode - an append-only fact that NEVER overwrites the Scorecard - and the episode's Disputed state is
  DERIVED from the presence of dispute facts (DM7), never a stored flag. Persisted in both stores
  (append-only trigger + idempotent id), read by a DisputeProjection, and surfaced as a disputed-episode
  count on the Leaderboard so a disputed score is discoverable from the surface (US-16).
---

# Design: Operator Dispute Path (connective 4)

## 1. Problem & scope

The spec makes Disputed a first-class, discoverable state (US-16, §10) and rule 12 is explicit: *"a
dispute appends a superseding evaluation record; prior scores are not overwritten."* Nothing recorded a
dispute. This slice adds the dispute fact, its persistence, the derived Disputed state, and a minimal
surfacing so an operator can contest a score and see that it is contested.

**In scope:** the `ScoreDispute` append-only fact; store persistence (interface + both stores, append-only
trigger, idempotent id); the `DisputeProjection` (derived Disputed state, DM7); a disputed-episode count
on the Leaderboard surface; tests incl. the non-overwrite guarantee and the append-only invariant.
**Out of scope:** the dispute *resolution* flow (a new Scorecard version from deterministic evidence or a
human disposition - a larger workflow); a per-episode dispute UI with a raise-dispute command (needs a
menu/command surface); the dispute-overturn-rate anti-Goodhart metric (US-8 promotion gate).

## 2. The dispute fact (append-only, never overwrites - rule 12)

`ScoreDispute(DisputeId, EpisodeId, OperatorId, DisputedDimension?, Reason, RaisedAt)` is an **append-only
fact**, stored in `score_dispute_fact` with `BEFORE UPDATE`/`BEFORE DELETE` triggers (DM11) exactly like
`observed_span_fact`. `DisputedDimension` is `null` for a whole-score dispute or one `ScoreDimension` for
a targeted one. Crucially it lives in its **own table**, separate from `scored_episode_cell`: raising a
dispute writes a new fact and touches no score, so the prior Scorecard is preserved by construction
(rule 12), proven by `Dispute_NeverOverwritesTheScorecard_PriorScorePreserved`.

## 3. The derived Disputed state (DM7)

An episode is **Disputed** iff it has at least one dispute fact - a *derived* state, never a stored flag
on the score (DM7 - two homes for one truth is the defect this avoids). `DisputeProjection` computes it:
`IsDisputed`, `DisputeCount`, and `DisputedEpisodeIds` fold the append-only facts. The Leaderboard pane's
`WatcherDisputeQuery` uses it to count how many of the scored episodes on the board are disputed and
surfaces `"N disputed episode(s)"` in its status line, so a disputed score is discoverable from the
surface within one interaction (US-16). Absent a dispute query the count is simply not shown.

## 4. Failure modes & dispositions

| Failure mode | Disposition |
|---|---|
| Dispute overwrites a score | Impossible - separate table; the score is never written by a dispute (tested) |
| Duplicate/redelivered dispute id | `INSERT OR IGNORE` / `TryAdd` - idempotent, first write wins, no update (tested) |
| Tampering with a recorded dispute | `BEFORE UPDATE`/`BEFORE DELETE` triggers abort (DM11, tested) |
| Disputed state drifts from the facts | Derived on read, never stored (DM7); `DisputeProjection` folds the facts (tested) |
| Whole-score vs per-dimension | `DisputedDimension` nullable; both round-trip (tested) |
| No dispute query wired | Leaderboard simply omits the disputed count (tested) |

## 5. Test plan

- `ScoreDisputeTests` (11): in-memory + SQLite append/read (whole-score + per-dimension), persist across
  reopen, non-overwrite of the Scorecard, append-only UPDATE/DELETE rejection, idempotent duplicate id,
  cross-episode `AllDisputes` order, `DisputeProjection` derivation, and the Leaderboard disputed-count
  surfacing (with and without a dispute query).
- The append-only + idempotent guarantee is mutation-verified (INSERT OR IGNORE -> REPLACE trips the trigger).
