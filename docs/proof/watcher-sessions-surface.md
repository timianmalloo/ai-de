---
id: proof-watcher-sessions-surface
title: "Proof Pack - Loomkeeper Sessions Surface (slice 3)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, proof-pack, ui, sessions, wpf, phase-1]
links:
  - { to: design-watcher-sessions-surface, rel: tested-by }
  - { to: design-watcher-phase1-skeleton, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper Sessions surface meets its design: a synchronous, deterministic
  projection folds the observation store + liveness into honest session rows (Not Recorded for an
  unproven harness/model, a no-colour-alone liveness badge), the pane VM carries the full state set and
  never strands on Loading nor renders an unreadable store as blank success (DC-011), and the WPF
  "sessions" surface shows an observed row and is in the default layout - proven by 10 Core tests + 3
  STA render tests, with the Not-Recorded honesty oracle mutation-verified. Core 780/0, App 135/0.
---

# Proof Pack: Loomkeeper Sessions Surface (slice 3)

- **Components:** `IWatcherObservationStore.AllSessions()` (both stores); `src/AiDe.Core/Presentation/WatcherSessionsPaneViewModel.cs` (`LivenessBadge`, `WatcherSessionRow`, `WatcherSessionSnapshot`, `IWatcherSessionsQuery`, `WatcherSessionsQuery`, `WatcherSessionsPaneViewModel`); `SurfaceContentFactory` `"sessions"` kind; `Layout.Default()` sessions surface.
- **Tests:** `tests/AiDe.Core.Tests/Watcher/WatcherSessionsPaneViewModelTests.cs` (10, **10/10**) + `tests/AiDe.App.Tests/SurfaceContentTests.cs` (3 new, STA). Full `AiDe.Core.Tests` **780/0**, `AiDe.App.Tests` **135/0**; both builds clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| A session with an unproven harness/model renders **Not Recorded**, never blank (US-13) | `Load_SessionWithNoHarnessOrModel_RendersNotRecorded_NeverBlank` | `WatcherSessionRow.From` null-coalesce | null harness/model → "Not Recorded" ×2 | **Yes** — replacing `?? NotRecorded` with `?? ""` reds the test (behavioral) | Verified | — |
| No sessions → honest **Empty**, not a fake row | `Load_NoSessions_IsEmpty` | pane `Rows.Count == 0` | Empty + "No sessions observed yet" | Seen green | Verified | — |
| No watcher store wired → **Unavailable** stated, never blank success | `Load_NullQuery_IsEmpty_AndSaysWhatIsUnavailable` | null-query branch | Empty + "not available", not "Loading" | Seen green | Verified | Live daemon→app wiring is a later slice |
| A full session → **Ready**, badge Alive ✓, trust Verified, span count | `Load_OneFullSession_IsReady_WithAliveVerifiedRow` | pane map + badge | one row, glyph ✓, "Verified", 3 spans | Seen green | Verified | — |
| Liveness badge is **not colour alone** (glyph + text) | `Load_StaleSession_BadgeIsStale_NotColourAlone`, `Load_EndedSession_BadgeIsEnded` | `LivenessBadge.For` | Stale ~ / Ended × glyphs | Seen green | Verified | WCAG 2.2 AA |
| An unreadable store → **Error**, never Loading-forever, never blank (DC-011) | `Load_ThrowingQuery_IsError_NeverLoading_NeverBlankSuccess` | pane try/catch | Error + "unavailable", not "Loading" | Seen green | Verified | — |
| Row order is preserved from the query (deterministic) | `Load_PreservesTheQueryOrder` | pane maps in order | alpha, beta, gamma | Seen green | Verified | store enumerates ordered (repo, worktree, session) |
| A screen reader hears the whole row (WCAG 2.2 AA) | `Row_AccessibleName_AnnouncesTheWholeRow` | `WatcherSessionRow.AccessibleName` | agent + harness + liveness + spans announced | Seen green | Verified | — |
| The query folds store + liveness + span count into snapshots | `Query_FoldsStoreLivenessAndSpanCount_IntoSnapshots` | `WatcherSessionsQuery` over real store | register→heartbeat→span → Alive, SpanCount 1 | Seen green | Verified | — |
| The **rendered** "sessions" surface shows an observed row, not Loading (E11) | `TheSessionsSurface_ShowsAnObservedSessionRow` (STA) | `SurfaceContentFactory` "sessions" | ListBox item count 1, status not "Loading" | Seen green | Verified | synchronous load — no async binding trap |
| The rendered surface with no store says **not available**, not blank (E11) | `TheSessionsSurface_WithNoWatcherStore_SaysObservationIsUnavailable_NotBlank` (STA) | factory + null query | status "not available" | Seen green | Verified | — |
| The sessions surface is in the **default layout** (a control nobody can see cannot fire) | `TheSessionsSurface_IsInTheDefaultLayout`; existing `EveryKindTheFactoryClaimsToKnow…` covers "sessions" | `Layout.Default()` + `KnownKinds` | a "sessions" surface is present; kind is not the Unavailable pane | Seen green | Verified | — |

**Boundary set covered:** null query (unavailable), zero sessions (empty), one full session (ready), null harness/model (Not Recorded), Stale, Ended, throwing query (error), multiple ordered, accessible-name, real store+liveness fold, rendered surface (row / unavailable / default-layout).

**Testing Strategy triggers applied:** **D1** (pane VM + query + row units), and **E11** (the *rendered* WPF surface proven on an STA thread through the real `SurfaceContentFactory`, not just the VM — the exact class of defect `SurfaceContentTests` exists for). No triggered directive dropped.

**Mutation sense:** the **Not-Recorded honesty** oracle is proven behaviorally (replacing `?? NotRecorded` with `?? ""` reds `Load_SessionWithNoHarnessOrModel_RendersNotRecorded_NeverBlank`), then reverted. The DC-011 shape (Loading-forever / blank success) is closed structurally by a **synchronous** load, and asserted by the Error/Unavailable tests.

**UI (UX & Accessibility, U9/U16):** complete state set (Empty / Unavailable / Ready / Error) with real in-voice copy and no Loading-forever; liveness conveyed by glyph + text, not colour alone; each row exposes a full `AccessibleName` and the surface carries its title into the automation tree; the surface uses the existing `TextMutedBrush` token (no raw hex), island-wrapped like every non-windowed pane. The App `TokenDisciplineTests` remained green.

**Residual:**
- **Live daemon→app wiring** (a running app reading the daemon's live observation store over IPC) is a later Phase-1/Phase-2 concern; the surface renders honestly with a null/empty query until then (Unavailable/Empty, never a false success).
- **Nested repo→worktree→session TreeView** — the walking-skeleton row is a flat, sorted, grouped-by-label list; the projection already orders for a future nested treegrid.
