---
id: note-conductor-mode-cohort-not-partition
title: "Decision note — R4 bullet 2 proven by one cell holding both modes"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [conductor, leaderboard, scoring, cohort, partition, task-class]
links:
  - { to: note-conductor-r4-core-phase1-scope, rel: refines }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-09
summary: >-
  R4 forbids leaderboard cells splitting by mode, but the two ingest doors default to
  different task classes - and task class IS a partition axis - so the same work would
  split anyway under a different column name. Phase 1 adds a nullable mode column and
  proves one cell holds both modes under one caller-chosen task class.
---

# Decision note — R4 bullet 2 proven by one cell holding both modes

**Ruled by:** Owner agent, 2026-09-09. Confidence: **Verified** — the Owner opened every
citation below.

## The problem, which is worse than it first looked

R4 bullet 2 requires: *"Mode appears as a cohort attribute; leaderboard cells never split by
mode."* The partition key is `ScoreSegment(Workspace, TaskClass, SchemaVersion)`
(`Leaderboard.cs:25-70`), and a comparison never crosses any of its three axes.

The two ingest doors default to **different task classes**:

| Door | Task class | Citation |
| --- | --- | --- |
| Audit import (observed lanes today) | `"audit-import"` | `WatcherHost.cs:118` |
| Registered-session sweep (the governed path) | `ScoreSegment.Unclassified` | `ClosedEpisodeScoring.cs:69`, `WatcherHost.cs:146` |

So the same work lands in different cells — the split R4 forbids, **wearing the task-class
column instead of a mode column**.

It is worse than a two-cell split: **`Unclassified` is `IsComparable == false`**
(`Leaderboard.cs:66-70`). A governed episode would rank **nowhere**, while an observed episode
of the same work ranks in a real cell named after *the door it came through*.

## Ruling

Phase 1 adds `mode` as a **nullable cohort column** and proves, **in one test**, that a governed
and an observed episode carrying the **same caller-chosen task class** land in **one
`ScoreSegment` cell as two mode cohorts**.

**The door determines the mode; the work determines the task class. Never the reverse.**

## Because

`ScoringService.ScoreAndRecord` **already requires `taskClass` from the caller with no default**
(`ScoringService.cs:75`). So this needs **no production change to the scoring contract** — the
two *door-level defaults* are what make the same work diverge. A test that passes one explicit
task class through both `ImportAndScoreEpisodesFromAuditLog` and `ScoreClosedEpisodes` is the
oracle that *"mode is absent from the key"* can never be: the negative check proves a
compile-time tautology and cannot fail for the right reason.

Recording the divergence as a mere limitation would let the Phase-1 close claim "one scoring
path" while the board visibly shows two.

Spec §8.4 pins the partition and puts mode **inside** the cell as a lens — exactly what the
positive test demonstrates.

## Conditions

1. Pre-existing `scored_episode_cell` rows read `mode` as **NULL, meaning "not recorded"**. No
   backfill infers "observed" from the absence of a session record.
2. The test asserts the **positive** shape — one cell, exactly two distinct `mode` values, both
   episodes present. It does not sleep and does not depend on ingest order.
3. `mode` is **never** a `ScoreSegment` member. The negative check may exist but does not stand
   alone.
4. The **Data & Persistence Architect signs the migration as expand-only.**
5. The door-named default is registered as a defect class (condition 5 of the ruling).

## Scope effect

**Admits:** the `mode` column (additive, nullable, no backfill), the one-cell-two-cohorts test,
and mode set from the ingest path.

**Cuts:** no task-classification work; no change to `ScoringService`/`WeaveScorer` — the R4-core
"unchanged by diff" condition stands.

**Defers:** retiring the `"audit-import"` default — a task class that names the *door*, not the
*work* — to **Phase 3**, when spec §8.4 controlled task classes arrive. Changing it now would
silently move existing observed episodes from a comparable cell into `Unclassified`, a
history-rule change with no spec basis.
