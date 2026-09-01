---
id: proof-watcher-work-episode
title: "Proof Pack - Loomkeeper Work Episode (slice 4)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, proof-pack, work-episode, goal, done-when, phase-2]
links:
  - { to: design-watcher-work-episode, rel: tested-by }
  - { to: design-watcher-phase1-skeleton, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper Work Episode meets its design: an episode binds one immutable goal +
  done-condition (the CT19 goal-state triple) to one bounded interval of one authenticated session; the
  lifecycle is capability-verified (forgery rejected on open/reframe/close); changing the goal starts a
  NEW episode (the old is Superseded, the next generation opens with the new goal, never a mutation);
  the projection binds only spans inside the interval (endpoints inclusive, open episode uses now); and
  it persists across a SQLite reopen - proven by 20 tests incl. D4 SQLite + an E11 composition, with the
  interval-endpoint oracle mutation-verified. Full suite 807/0.
---

# Proof Pack: Loomkeeper Work Episode (slice 4)

- **Components:** `src/AiDe.Core/Watcher/WorkEpisode.cs` (`Goal`, `DoneCondition`, `EpisodeGeneration`, `EpisodeOutcome`, `EpisodeState`, `WorkEpisode`, `WorkEpisodeService`, `EpisodeProjection`); store additions (`RecordEpisode`/`FindEpisode`/`EpisodesForSession`/`AllEpisodes`/`SpanCountInInterval`) in both `InMemoryWatcherObservationStore` and `SqliteWatcherObservationStore` (new `work_episode_dim` table; in-memory now retains span `RecordedAt`).
- **Tests:** `tests/AiDe.Core.Tests/Watcher/WorkEpisodeTests.cs` — 20 tests, **20/20**; full `AiDe.Core.Tests` suite **807/0**; build clean (0 warnings, `TreatWarningsAsErrors`).
- **Informed by** the AI-Forward `done_when` work (CT19–CT24 + PACK-O): the episode's `(Goal, DoneWhen, NotInScope)` mirrors the goal-state triple, so it is the durable, scoreable projection of a turn's goal-state, not a parallel structure.

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| Open binds goal/done/interval as generation 1, Active | `Open_BindsGoalDoneAndInterval_AsGeneration1_Active` | `WorkEpisodeService.Open` | gen 1, Active, fields set, no outcome, persisted | Seen green | Verified | — |
| An empty goal or done-condition is rejected (LK-0002) | `Open_EmptyGoalOrDone_ThrowsInvalidBinding` (theory) | `Validate` | empty/blank goal or done → InvalidBinding | Seen green | Verified | — |
| A blank not-in-scope becomes null | `Open_BlankNotInScope_BecomesNull` | `NullIfBlank` | "   " → null | Seen green | Verified | — |
| A forged capability cannot open an episode (LK-0001) | `Open_ForgedCapability_ThrowsForgery` | capability verify | forged cap → ForgeryRejected | Seen green | Verified | — |
| Changing the goal starts a NEW episode: old Superseded, next generation opens with the new goal, old goal immutable | `Reframe_SupersedesTheOldEpisode_AndOpensTheNextGenerationWithTheNewGoal` | `Reframe` | old Closed+Superseded, old goal unchanged, new id, gen 2, new goal, Active | Seen green | Verified | the aggregate invariant (spec 211/234) |
| A forged reframe is rejected and leaves the episode untouched | `Reframe_ForgedCapability_ThrowsForgery_AndLeavesTheEpisodeUntouched` | verify before mutate | ForgeryRejected, episode still Active | Seen green | Verified | — |
| Reframe of an unknown / already-closed episode is rejected | `Reframe_UnknownEpisode_Throws`, `Reframe_AlreadyClosedEpisode_Throws` | `RequireActive` | InvalidBinding | Seen green | Verified | — |
| Close records the outcome and closes the interval | `Close_RecordsOutcomeAndClosesTheInterval` | `Close` | Closed, Completed, ClosedAt set + persisted | Seen green | Verified | outcome is *declared*, not judged (Weave = slice 5) |
| Close of an already-closed / forged / unknown episode is rejected | `Close_AlreadyClosed_Throws`, `Close_ForgedCapability_ThrowsForgery` | `RequireActive` + verify | InvalidBinding / ForgeryRejected | Seen green | Verified | — |
| Two sequential episodes have incrementing generations | `TwoSequentialEpisodes_HaveIncrementingGenerations` | `NextGeneration` | gen 1 then 2 | Seen green | Verified | — |
| The projection binds ONLY spans inside the interval, endpoints inclusive (US-6) | `ObservedSpanCount_BindsOnlySpansInsideTheInterval_EndpointsInclusive` | `SpanCountInInterval` | before/after excluded, at-open/inside/at-close included → 3 | **Yes** — changing `<=` to `<` (exclusive) reds the test | Verified | — |
| An open episode's activity counts up to now | `ObservedSpanCount_OpenEpisode_CountsUpToNow` | projection `ClosedAt ?? now` | spans before now counted, future excluded | Seen green | Verified | — |
| ForSession returns generations in order | `ForSession_ReturnsEpisodesInGenerationOrder` | store order-by generation | gen 1, 2 | Seen green | Verified | — |
| An episode persists across a SQLite reopen with immutable goal/done + outcome (**D4**) | `Sqlite_EpisodePersistsAcrossReopen_WithImmutableGoalAndOutcome` | `work_episode_dim` upsert + read | reopened row: goal/done/not-in-scope/Closed/Completed | Seen green | Verified | expand-only new table |
| SpanCountInInterval over the real recorded_at column (**D4**) | `Sqlite_SpanCountInInterval_OverRealRecordedAt` | ISO-8601 string BETWEEN | 1 of 3 spans in the window | Seen green | Verified | ISO "O" is fixed-width, lexicographically ordered |
| End to end: register → open → ingest spans in-interval → close → projection reports Closed + bound span count (**E11**) | `Composition_RegisterOpenIngestClose_ProjectionReportsClosedAndBoundSpanCount` | real registrar + store + projection | Closed, 2 bound spans, outcome recovered | Seen green | Verified | — |

**Boundary set covered:** open (valid / empty-goal / empty-done / blank-scope / forged), reframe (supersede+increment / forged / unknown / closed), close (valid / already-closed / forged), two sequential episodes, interval binding (before / at-open / inside / at-close / after / open-episode-now), generation order, SQLite persist + interval, composition.

**Testing Strategy triggers applied:** **D1** (service + projection units), **D4** (two real-SQLite tests — persistence across reopen and the interval query over the real `recorded_at` column, not a mock), and an **E11** composition through the real registrar/store/projection. No triggered directive dropped.

**Mutation sense:** the interval-endpoint oracle is proven behaviorally (changing `recordedAt <= toInclusive` to `<` reds `ObservedSpanCount_...EndpointsInclusive`), then reverted. The reframe invariant (old Superseded, gen+1, old goal immutable) is asserted directly.

**Security note (STRIDE, carried from design):** the lifecycle is capability-verified — only the authenticated session may open/reframe/close its episodes (LK-0001 forgery on mismatch); a forged reframe/close leaves the episode untouched. The goal/done statements are session-authored task text kept **local** (no egress path added; the default-deny gate stands) and carry no personal data by construction (they describe a task, not a person); work-content governance is Phase 5.

**Boundary clarified (what this slice does NOT do):** the outcome is the **declared** lifecycle terminal state (`Completed/Abandoned/Superseded/Blocked`), **not** a quality judgment. Whether a `Completed` claim is *honest* (goal actually met vs. drifted past `done_when` — the PACK-O signature) is the Weave's Outcome-integrity dimension (slice 5), deliberately separated here (spec §"Work Evaluation": separate deterministic facts from advisory judgments).

**Residual:**
- **Wire ingest of the goal-state** — mapping an AI-Forward audit entry's `goal`/`done_when` (AL5b) to `Open`/`Close`, and an injected-contract `episode-open`/`episode-close` kind for non-pack sessions (the slice-2 contract vocabulary, extended) — is the connective follow-on; slice 4 ships the deterministic domain both paths feed, with the vocabulary already aligned.
- **Weave scoring** of the episode (outcome integrity, goal focus, drift) — slice 5.
