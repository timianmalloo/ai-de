---
id: proof-watcher-board-leaderboard-surfaces
title: "Proof Pack - Loomkeeper Board & Leaderboard WPF Surfaces (connective 2)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, proof-pack, wpf, surface, board, leaderboard, ui, phase-4]
links:
  - { to: design-watcher-board-leaderboard-surfaces, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Evidence that the Board (US-4) and Leaderboard (US-14) WPF surfaces render honestly and are reachable:
  the pane view models fold the store synchronously and degrade to explicit states (never Loading-forever,
  DC-011); untrusted board content is shown-but-flagged and a redaction is a tombstone; the leaderboard
  segments by (task class, schema) and shows Not Comparable for a below-cohort or single-operator cell
  (US-10); both surfaces render a populated ListBox through SurfaceContentFactory and are in the default
  layout; a v2->v3 migration adds them to existing layouts (E10); and WorkbenchShell opens the
  per-workspace watcher store and wires all three queries. 15 Core + 3 App render + 1 migration test;
  Core suite 913/0, App suite 138/0; the migration oracle mutation-verified.
---

# Proof Pack: Board & Leaderboard WPF Surfaces (connective 2)

- **Components:** `WatcherBoardPaneViewModel` + `WatcherBoardRow` + `IWatcherBoardQuery`/`WatcherBoardQuery`, `WatcherLeaderboardPaneViewModel` + `WatcherLeaderboardRow` + `IWatcherLeaderboardQuery`/`WatcherLeaderboardQuery` (Core.Presentation); `IWatcherObservationStore.AllBoardMessages` (both stores); `SurfaceContentFactory` (board/leaderboard kinds + `ListPane`); `LayoutModel.Default()` + `LayoutMigrations` v2->v3 + `LayoutStore.CurrentSchemaVersion=3`; `WorkbenchShell` store wiring.
- **Tests:** `WatcherBoardPaneViewModelTests` (9) + `WatcherLeaderboardPaneViewModelTests` (8) + `SurfaceContentTests` board/leaderboard (3) + `LayoutUpgradeTests.TheV2ToV3Migration...` (1). Core suite **913/0**, App suite **138/0**; builds clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| A null board query renders an honest "not available", never blank/Loading | `WatcherBoardPaneViewModelTests.Load_NullQuery_...` | null-query guard | Empty state, "not available", no "Loading" | Seen green | Verified | the walking-skeleton default |
| Board posts render and count repos + flags | `Load_Posts_IsReady_AndCountsReposAndFlags` | pane fold | 2 posts, 2 repos, 1 flagged | Seen green | Verified | — |
| An injection-shaped post reads as untrusted, not a directive | `FlaggedPost_CarriesFlagPrefix_...` | `WatcherBoardRow.From` | `⚠ flagged · ` prefix + "flagged as possible injection" | Seen green | Verified | flag, not a safety boundary (US-4 #5) |
| A redacted post shows a tombstone, never the content | `RedactedPost_ShowsTombstone_...` | tombstone branch | `[redacted]`, content absent | Seen green | Verified | — |
| Null content renders Not Recorded; long content trims to one line | `NullContent_...`, `LongContent_...` | render rules | Not Recorded; single-line, ellipsis | Seen green | Verified | — |
| Board store-read failure is an explicit Error, not Loading | `Load_StoreThrows_IsError_...` | catch branch | Error state, "unavailable", no "Loading" | Seen green | Verified | DC-011 |
| A comparable cohort shows a rank | `WatcherLeaderboardPaneViewModelTests.Load_ComparableCohort_ShowsARank` | `LeaderboardComposer` | HarnessModel cell rank #1, cohort 5 | Seen green | Verified | — |
| A single-operator cell is Not Comparable (privacy) | `Load_SingleOperator_IsNotComparable_PrivacyProtected` | US-10 guard | not comparable, reason present, no rank | Seen green | Verified | — |
| A below-cohort cell is Not Comparable | `Load_BelowCohortMinimum_IsNotComparable` | cohort<5 guard | not comparable, median "—" | Seen green | Verified | — |
| Two task classes are segmented, never compared | `Load_TwoTaskClasses_AreSegmented_NeverCompared` | per-segment compose | distinct segments; medians 84 vs 44 | Seen green | Verified | rule 11 |
| The board & leaderboard surfaces render a populated ListBox and are in the default layout | `SurfaceContentTests.TheBoardSurface_...`, `TheLeaderboardSurface_...` | `SurfaceContentFactory` + `Layout.Default()` | 1+ item in the ListBox; kind present in default layout | Seen green | Verified | E10 reachable |
| No-store board renders "not available", not blank | `TheBoardSurface_WithNoWatcherStore_...` | factory null path | status "not available", no "Loading" | Seen green | Verified | — |
| The v2->v3 migration adds board+leaderboard beside sessions, once | `LayoutUpgradeTests.TheV2ToV3Migration_...` | shipped migration | both present in the sessions stack, exactly once | **Yes** — dropping the leaderboard add reds this | Verified | mutation-verified oracle |
| Every release surface survives the oldest-schema climb | `ALayoutFromTheOldestSchema_ArrivesValidAtTheCurrentOne` | derived surface set | restored == CurrentSurfaces (incl. board/leaderboard) | Seen green | Verified | forgetting a migration fails here, not on a user's machine |

## Testing Strategy triggers applied

- **T1 (deterministic view models):** both pane VMs are pure folds of a query; unit-tested across the full state set (null/empty/ready/error) and the honest-render rules (flag/tombstone/trim/segment).
- **UI state completeness (U9):** each pane implements and tests Loading -> Empty / Ready / Error, and the "no store" and "store throws" degraded states - the states the urge-to-complete skips.
- **Accessibility (U16):** every row carries an `AccessibleName` (screen-reader-complete, no colour-alone); the flag is glyph+text, not colour.
- **E10 reachability:** both surfaces are in the default layout and added to existing layouts by a migration - proven, not assumed.
- **E11 rendered surface:** the App `SurfaceContentTests` go through the real `SurfaceContentFactory` to a real `ListBox`, not a hand-built VM.
- **T1 mutation sense:** the v2->v3 migration (the reachability guarantee) was mutated to drop the leaderboard add, observed to red the migration test, then reverted.
- **D0 hygiene:** deterministic (`UnixEpoch`), isolated, focal-call + meaningful assertion; App tests marshalled onto an STA thread.

## Security / privacy note

- **Untrusted content (US-4):** board content is rendered as display text with a visible injection flag; it is never interpreted. The flag is a signal, not a boundary - the invariance guarantee still lives in the grader path (slice 6/7).
- **US-10 small-cohort privacy** is enforced in the composed cell the pane renders: a single-operator cell shows Not Comparable and no rankable Weave.
- **No credentials/egress** in this slice; the store is opened read/write locally and owned by the shell.

## Residual risk

- **Ingest host not running** - the panes are live-capable but nothing writes the store during a real agent session yet; hosting the OTLP receiver / coordination ingest in a running process (so a smoke test with real agents populates the panes) is the next connective step.
- **Cross-process liveness** - monotonic-tick heartbeats are process-relative; a separate ingest process's liveness is not comparable to the app's clock (recorded caveat; sessions/board/leaderboard reads are unaffected).
- **Standing drill-down** - the per-agent Standing (rank + trend + per-dimension reasons, US-16) is not a dedicated pane; the leaderboard row carries rank/comparability, and a trend needs a persisted score history (follow-on).
- **No auto-refresh** - the panes fold the store once on construction; a live-updating pane (re-fold on a store-changed signal) is a follow-on.
