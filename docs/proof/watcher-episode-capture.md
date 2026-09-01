---
id: proof-watcher-episode-capture
title: "Proof Pack - Episode-Lifecycle Capture (ep-capture)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, proof-pack, episode, capture, ep-capture, phase-2]
links:
  - { to: design-watcher-episode-capture, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Proof Pack for ep-capture: AuditLogEpisodeSource parses goal-state audit entries into imported closed
  Work Episodes and WatcherHost.ImportEpisodesFromAuditLog records them (upsert). Honest outcome mapping
  mutation-verified. 6 tests; Core 985/0.
---

# Proof Pack — Episode-Lifecycle Capture (ep-capture)

| # | Claim | Evidence (test) | Oracle (why it can fail) | Red observed | Confidence | Residual risk |
|---|---|---|---|---|---|---|
| 1 | A goal-state entry becomes a closed episode with the declared goal + interval | `Parse_GoalStateEntry_BecomesAClosedEpisode_WithTheDeclaredGoalAndInterval` | wrong field/interval mapping | seen | Verified | — |
| 2 | **A non-success outcome is not silently Completed** | `Parse_NonSuccessOutcome_IsNotSilentlyCompleted` | mapping blocked→Completed | **mutation-verified** (blocked→Completed → red) | Verified | — |
| 3 | An entry without a goal-state is **not** an episode (no fabrication) | `Parse_EntryWithoutAGoalState_IsNotAnEpisode` | inventing a goal | seen | Verified | — |
| 4 | Blank/corrupt lines skipped, valid kept | `Parse_SkipsBlankAndCorruptLines_KeepsTheValidOnes` | a corrupt line throwing/producing junk | seen | Verified | — |
| 5 | A missing file yields no episodes | `ReadFile_MissingFile_YieldsNoEpisodes` | throwing on missing file | seen | Verified | — |
| 6 | Host import records the goal-state episodes; re-import upserts | `ImportEpisodesFromAuditLog_RecordsTheGoalStateEpisodes_IntoTheStore` | duplicating on re-import; recording the note | seen | Verified | — |
| 7 | No regression | Core 985/0 | any broken contract | n/a | Verified | — |

## Mutation log

- `MapOutcome`: mapped `"blocked"` → `Completed` → `Parse_NonSuccessOutcome_IsNotSilentlyCompleted`
  **failed** (an unmet outcome must never read as Completed — the honesty claim). Reverted.

## Gates

- Build: Core clean (0 warnings, `TreatWarningsAsErrors=true`).
- `docs-graph.py derive` + `validate`: 0 defects, 0 orphans.

## Residual risk

This is the episode source; the shell auto-import and conn-10 scoring are the next increments. An imported
episode remains **Not-Scored** until a verification-path signal is observable — the honest outcome, not a
defect (see `design-watcher-episode-capture`).
