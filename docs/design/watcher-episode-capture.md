---
id: design-watcher-episode-capture
title: "Loomkeeper Episode-Lifecycle Capture from the Audit Log (ep-capture)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, design, episode, capture, audit-log, done-when, ep-capture, phase-2]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: design-watcher-work-episode, rel: depends-on }
  - { to: design-watcher-weave-score, rel: depends-on }
  - { to: note-conn-10-11-episode-source-blocker, rel: refines }
review-by: 2027-02-26
review-suggested: []
summary: >-
  The episode source that unblocks scoring: imported closed Work Episodes are read from a repo's AI-Forward
  audit log (the goal + done_when + session goal-state entries, AL5b) via a pure AuditLogEpisodeSource and
  recorded directly into the store (historical facts, not live capability-verified operations). Only an
  explicit success maps to Completed; an entry without a declared goal-state is not an episode (no
  fabrication, spec L127 / NG1).
---

# Episode-Lifecycle Capture (ep-capture)

## Problem & spec trace

`note-conn-10-11-episode-source-blocker` established that scoring is blocked because **no terminal session
opens a goal/done-when Work Episode**. The scorer (`WeaveScore.cs:169`) refuses to score without a
declared goal + done-condition + a closed episode. This slice supplies the missing **episode source** from
a source that already exists and is observable: the repo's committed AI-Forward **audit log**, where a
substantive turn records its goal-state (`goal`, `done_when`, `session` — AL5b / front-matter CT19). 43
such entries exist in this repo's own log.

## Design

- **`AuditLogEpisodeSource` (pure, static):** `Parse(jsonlLines)` / `ReadFile(path)` turn each audit entry
  that carries **all three** of `goal` + `done_when` + `session` into an imported **closed** `WorkEpisode`:
  - `EpisodeId = "ep:" + <entry id>` (stable, so re-import upserts, not duplicates);
  - `Goal`/`DoneWhen` from the declared statements; `SessionId` from `session`;
  - interval `OpenedAt = started_at ?? datetime`, `ClosedAt = datetime` (a point episode when no start);
  - `Outcome` mapped **honestly**: only `"success"` → `Completed`; `"blocked"` → `Blocked`; everything
    else (failed/partial/unknown) → `Abandoned` — never silently Completed.
  - An entry missing any of the three fields is **skipped** — it is not an episode, and no goal is invented
    (spec L127; No-Guessing NG1). A corrupt line is skipped, never a wrong episode (IO8).
- **`WatcherHost.ImportEpisodesFromAuditLog(path)`:** reads the file and `RecordEpisode`s each imported
  episode (an **upsert** by id — idempotent re-import). Returns the count. A missing file imports nothing.

## Why importing is honest, not fabrication or forgery

An audit entry is a **durable, committed record** of a bounded goal that was worked and closed. Importing
it reads a *fact*; it does not invent a goal-state the way synthesising one from a bare terminal session
would. And it is **not** a forgery-gate bypass: the capability-verified `IWorkEpisodeService` is for
**live** real-time operations; imported **historical** episodes are recorded directly through the store —
the same pattern the coordination pump uses to import registrations. The store's `RecordEpisode` carries
no capability by contract.

## Failure modes & dispositions

| Mode | Disposition |
|---|---|
| Entry with no goal-state | Skipped — not an episode (the fabrication guard). |
| Non-success outcome | Mapped to Blocked/Abandoned, never Completed (mutation-verified). |
| Corrupt JSON line | Skipped (JsonException caught per line). |
| Missing `started_at` | Interval opens at `datetime` (a point episode), never a wrong time. |
| Missing file | Imports nothing (no throw). |
| Re-import | Upsert by `ep:<id>` — no duplicates. |
| Imported episode references an unregistered session | Recorded anyway (historical fact); projections read `AllEpisodes()`. |

## Boundary set

goal-state entry · non-success outcome · no-goal note · blank/corrupt lines · missing started_at ·
missing file · re-import (idempotence).

## What this unblocks, and what remains

This is the episode **source**. It makes real closed episodes exist, so the WeaveScorer's goal/done/closed
gates pass. Remaining (next increments): (1) the **shell auto-import** — call
`ImportEpisodesFromAuditLog(<workspaceRoot>/docs/audit/audit-log.jsonl)` on attach / periodically; (2)
**conn-10** — derive `DeterministicEpisodeSignals` for an imported episode and `ScoreAndRecord`; note the
score is honestly **Not-Scored** until a **verification-path signal** is observable (the scorer's last
gate), which is a separate telemetry-convention question, not a fabrication to paper over.

## Residual risk

An imported episode has no live capability, so it cannot be reframed/closed through the live service (by
design — it is already closed). A verification-path signal is not derivable from an audit entry alone, so
imported episodes remain Not-Scored until such a signal exists — which is the honest outcome, not a defect.
