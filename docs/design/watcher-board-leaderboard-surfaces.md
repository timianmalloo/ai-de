---
id: design-watcher-board-leaderboard-surfaces
title: "Loomkeeper - Board & Leaderboard WPF Surfaces (connective 2)"
type: design
status: accepted
owner: "@timianmalloo"
phase: "4"
tags: [loomkeeper, watcher, wpf, surface, board, leaderboard, standing, ui, phase-4]
links:
  - { to: design-watcher-sessions-surface, rel: refines }
  - { to: design-watcher-score-persistence, rel: depends-on }
  - { to: design-watcher-message-board, rel: depends-on }
  - { to: design-watcher-advisory-grader, rel: depends-on }
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Two new WPF read surfaces - Message Board (US-4) and Leaderboard (US-14) - built exactly like the
  slice-3 Sessions pane: a synchronous store-fold view model in AiDe.Core.Presentation behind a null-safe
  query seam, rendered by SurfaceContentFactory, seeded into the default layout and added to existing
  layouts by a v2->v3 migration so they are reachable (E10). WorkbenchShell now opens the per-workspace
  watcher SQLite store and wires all three read queries, so the panes render live when the ingest host
  has written data and degrade to an honest "not available" when the store is absent.
---

# Design: Board & Leaderboard WPF Surfaces (connective 2)

## 1. Problem & scope

Slice 3 shipped the Sessions surface but `WorkbenchShell` constructed the factory as
`new SurfaceContentFactory(queries)` with **no** watcher query, so even Sessions rendered "not
available" in the running app. Slices 5-7 + conn-1 produced the Weave scorecards, the leaderboard and
the standing engines and their persistence, with **no UI**. This slice closes the "watch the watcher"
UX: it adds the **Board** and **Leaderboard** surfaces, and wires a real watcher store into the shell so
all three read surfaces show live data.

**In scope:** the two pane view models + query seams (Core.Presentation); the cross-repo
`AllBoardMessages()` reader; factory rendering of the `board`/`leaderboard` kinds; default-layout
seeding + a v2->v3 migration (reachability); the shell opening the per-workspace watcher store and
wiring the three queries; VM + render + migration tests. **Out of scope:** the ingest host that *writes*
the store during a live agent session (a documented follow-on - the panes are live-capable now, and
populate once ingest runs); a per-agent Standing drill-down with a time-series trend (needs a persisted
score history); the advisory evaluator (conn-3); disputes (conn-4).

## 2. Pattern reuse (the slice-3 Sessions shape, applied twice)

Both panes copy the Sessions pattern verbatim (the Solution-Selection Ladder rung-2 reuse):

- a `sealed record` **row** with a dense `DisplayLabel` (G6 density) and a full `AccessibleName`
  (WCAG 2.2 AA - screen-reader-complete, no colour-alone), and a `From(...)` that renders honestly;
- a **null-safe query seam** (`IWatcherBoardQuery` / `IWatcherLeaderboardQuery`) whose null means "no
  watcher store wired" -> the pane's Empty "not available" state, never a blank success;
- a **synchronous** `Load()` (a local store fold, no IPC) with the full state set
  (Loading -> Empty / Ready / Error), so it can never strand on "Loading…" the way an async
  construction-time binding did (DC-011);
- rendered by a **shared `ListPane` helper** in `SurfaceContentFactory` (one place for the ListBox +
  status + accessibility wiring - a new read surface never re-derives it).

## 3. Honest rendering (the two surfaces' specific rules)

**Board (US-4).** `BoardMessage.Content` is quarantined untrusted data: it is *shown* to the operator
but never as instruction. An injection-shaped post carries a visible `⚠ flagged · ` prefix and an
"flagged as possible injection" screen-reader phrase (US-4 #5), so it reads as untrusted; a redacted
post shows `[redacted]`, never the (now null) content and never blank (spec line 210); a null content
renders `Not Recorded`; long content is trimmed to a single-line preview.

**Leaderboard (US-14).** The pane discovers the distinct `(task class, score schema)` segments in the
scored episodes and composes one leaderboard per segment (never comparing across a segment - rule 11).
A comparable cell shows a rank; a below-cohort (< 5) or single-operator cell shows **Not Comparable**
with its reason and no rank (US-10 - a single operator is not de-anonymised off a public board). There
is deliberately no single optimisable scalar in a row (US-16).

## 4. Reachability (E10) - default layout + migration

A surface the factory can build but that no layout contains is a control nobody can see (the Joins
lesson). Both surfaces are added to `Layout.Default()` beside Sessions, **and** a shipped v2->v3
migration (`LayoutStore.CurrentSchemaVersion` 2 -> 3) adds them beside Sessions in every already-saved
layout via the idempotent `AddSurfaceBeside`. `TheV2ToV3Migration_AddsBoardAndLeaderboardBesideSessions`
and `ALayoutFromTheOldestSchema_ArrivesValidAtTheCurrentOne` (derived surface set) pin both.

## 5. The store wiring (the piece that makes the UX live)

`WorkbenchShell(queries, workspaceDataDirectory)` now, when a workspace data directory is supplied,
opens `SqliteWatcherObservationStore` at `<dataDir>/watcher.db` - the same file the ingest host writes -
builds a `LivenessProjection` (`SystemMonotonicClock`, 30 s stale), and passes
`WatcherSessionsQuery` / `WatcherBoardQuery` / `WatcherLeaderboardQuery` to the factory. The store is
owned by the shell and disposed with it. A store that cannot be opened degrades to the null-query path
(panes show "not available") rather than blocking the workbench from opening.

**Cross-process caveat (recorded).** Liveness compares monotonic ticks, which are process-relative, so
a heartbeat written by a *separate* ingest process is not comparable to the app's clock. In-process
ingest is exact; a cross-process liveness projection (a wall-clock or shared epoch heartbeat) is a
follow-on. Sessions/Board/Leaderboard reads themselves are unaffected (they are not tick-relative).

## 6. Failure modes & dispositions

| Failure mode | Disposition |
|---|---|
| No watcher store wired | Null query -> Empty "not available" state (tested, both panes) |
| Store unreadable / corrupt / locked | Shell catch degrades to null query; workbench still opens (tested behaviourally by the honest-state path) |
| Store read throws mid-load | Pane Error state, explicit message, never Loading-forever (DC-011, tested) |
| Untrusted board content as instruction | Shown but flagged/quarantined, never executed; injection prefix (tested) |
| Redacted content leak | Tombstone `[redacted]`, never the content (tested) |
| Cross-segment comparison | Composed per (task, schema) segment (tested) |
| Single-operator de-anonymisation | Not Comparable, no rank (US-10, tested) |
| Migration run twice | `AddSurfaceBeside` idempotent (Contains guard) - surface appears once (tested) |

## 7. Test plan

- `WatcherBoardPaneViewModelTests` (9): null/empty/ready, flag prefix, tombstone, Not Recorded, trim, error.
- `WatcherLeaderboardPaneViewModelTests` (8): null/empty/ready-rank, single-operator Not Comparable, below-cohort, segmentation, error.
- `ScorePersistenceTests.Sqlite_AllScoredEpisodes_FeedsLeaderboardComposer` (conn-1) already proves the store->composer path the pane uses.
- `SurfaceContentTests` (App): board & leaderboard render a populated ListBox and are in the default layout; the no-store case says "not available".
- `LayoutUpgradeTests.TheV2ToV3Migration_AddsBoardAndLeaderboardBesideSessions` (+ the oldest-schema climb).
