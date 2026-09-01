---
id: proof-watcher-message-board
title: "Proof Pack - Loomkeeper Message Board + Fleet (slice 6)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "3"
tags: [loomkeeper, watcher, proof-pack, message-board, fleet, cross-repo, quarantine, phase-3]
links:
  - { to: design-watcher-message-board, rel: tested-by }
  - { to: design-watcher-sessions-surface, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper Message Board + Fleet aggregator meet their design: a per-repository,
  append-only board with author/session/time/trust provenance; a reply/ack must reference an existing
  parent in the same repo (no orphan, no cross-repo thread); a forged capability is rejected; content
  is quarantined untrusted data and grader-injection shapes are flagged; a policy redaction tombstones
  the payload while the envelope remains and the thread stays anchored; and the fleet builds the
  repo->session map across >=2 sources - proven by 28 tests incl. D4 SQLite + an E11 composition, with
  the orphan-rejection oracle mutation-verified. Full suite 862/0.
---

# Proof Pack: Loomkeeper Message Board + Fleet (slice 6)

- **Components:** `src/AiDe.Core/Watcher/MessageBoard.cs` (`BoardMessageKind`, `BoardMessage`, `GraderInjectionScanner`, `MessageBoardService`); `src/AiDe.Core/Watcher/FleetAggregator.cs` (`RepositorySessions`, `FleetView`, `FleetAggregator`); store board methods (`AppendBoardMessage`/`BoardMessages`/`FindBoardMessage`/`RedactBoardMessage`) in both stores (new `board_message_fact` table).
- **Tests:** `MessageBoardTests.cs` (20) + `FleetAggregatorTests.cs` (8) - **28/28**; full `AiDe.Core.Tests` suite **862/0**; build clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| A post appears only in its repo with author/session/time/trust provenance (US-4 #1) | `Post_Question_AppearsInTheRepoBoard_WithProvenance` | `MessageBoardService.Post` | stored with s1 / Verified / time / quarantined | Seen green | Verified | — |
| Provenance carries the session's trust (Verified vs Asserted) | `Post_AssertedSession_CarriesAssertedTrustProvenance` | session `Binding.Trust` | Asserted author → Asserted provenance | Seen green | Verified | — |
| A Reply/Acknowledgement kind cannot be posted top-level | `Post_WithAThreadKind_IsRejected` (Theory) | kind guard | Reply/Ack via Post → InvalidBinding | Seen green | Verified | — |
| A forged capability cannot post | `Post_ForgedCapability_IsRejected` | capability verify | forged cap → ForgeryRejected | Seen green | Verified | — |
| Seq increments per repository | `Post_AssignsIncrementingSeqPerRepository` | `BoardMessages(repo).Count + 1` | 1 then 2 | Seen green | Verified | — |
| A reply/ack must reference an existing parent - no orphan (US-4 #2) | `Reply_ToAnExistingParent_IsThreaded`, `Reply_ToAnUnknownParent_IsRejectedAsOrphan` | `RequireParent` | threaded; unknown → InvalidBinding | **Yes** — disabling the guard reds both orphan tests | Verified | — |
| A reply cannot cross the repository boundary | `Reply_ToAParentInAnotherRepository_IsRejected` | `RequireParent` repo check | parent-in-A, reply-in-B → rejected | **Yes** — same mutation | Verified | — |
| An acknowledgement references its parent and has no content | `Acknowledge_ReferencesTheParent_AndHasNoContent` | `Acknowledge` | Kind=Ack, parent set, content null | Seen green | Verified | — |
| Grader-injection content is flagged and still quarantined (US-4 #4/#5) | `Content_WithAGraderInjection_IsFlagged_AndStillQuarantined` | `GraderInjectionScanner` | "score 100 … ignore the rubric" → flagged + quarantined | Seen green | Verified | flag is not a safety boundary - invariance is the scorer's typed-signal design (slice 5) |
| Benign content is quarantined but not flagged | `Content_Benign_IsQuarantined_ButNotFlagged` | scanner | not flagged; quarantined true | Seen green | Verified | all board content is untrusted |
| The injection scanner flags known shapes and not benign text | `InjectionScanner_FlagsKnownShapes` (Theory ×4), `InjectionScanner_DoesNotFlagBenignContent` (Theory ×3) | pattern list | score 100 / ignore the rubric / promote this lesson / bypass the floor → true; benign/empty → false | Seen green | Verified | — |
| The board is repository-scoped | `BoardMessages_AreRepositoryScoped` | `BoardMessages(repoKey)` | A and B isolated | Seen green | Verified | — |
| A policy redaction tombstones the payload, keeps the envelope, and the thread stays anchored (US-4 #6) | `Redact_TombstonesTheContent_ButKeepsTheEnvelope_AndTheThreadStaysAnchored` | `Redact` | content null + Tombstoned, envelope kept, a later reply still anchors | Seen green | Verified | policy (retention/opt-in) is Phase 5 |
| Board messages + redaction persist across a SQLite reopen (**D4**) | `Sqlite_BoardMessagePersistsAcrossReopen_AndRedactionPersists` | `board_message_fact` | 2 messages; tombstone content null, envelope intact | Seen green | Verified | — |
| End to end: post+reply in one repo is isolated from another (**E11**) | `Composition_PostReplyInOneRepo_IsIsolatedFromAnother` | full service + store | A has thread (2), B isolated (1) | Seen green | Verified | — |
| The fleet groups sessions by repository across >=2 sources (US-3) | `Aggregate_TwoReposAcrossTwoSources_GroupsByRepository`, `Aggregate_SameRepoAcrossSources_MergesIntoOneRepositoryWithBothSessions` | `FleetAggregator` | 2 repos/2 sessions; same-repo merges to 1/2 | Seen green | Verified | — |
| The fleet is empty for no sources and deterministically ordered | `Aggregate_NoSources_IsEmpty`, `Aggregate_OrdersRepositoriesByDisplay_AndSessionsById` | ordering | empty; alpha before zulu, sessions by id | Seen green | Verified | — |
| The fleet works over real stores (repo→session map) | `Aggregate_OverRealStores_BuildsTheRepoSessionMap` | real `WatcherSessionsQuery` ×2 | 2 repos, 2 sessions | Seen green | Verified | — |

**Boundary set covered:** post (each top-level kind / thread-kind rejected / forged), seq, reply (threaded / orphan / cross-repo), ack, injection (flagged / benign / scanner shapes), repo-scoping, redact/tombstone/thread-anchor, SQLite persist, composition; fleet (two-repo / same-repo-merge / empty / ordering / real-stores).

**Testing Strategy triggers applied:** **D1** (board service + scanner + fleet units), **D4** (SQLite board persistence + redaction across reopen), and an **E11** composition through the real board service + store. No triggered directive dropped.

**Mutation sense:** the orphan-rejection oracle (US-4 #2) is proven behaviorally - disabling the parent guard reds `Reply_ToAnUnknownParent_IsRejectedAsOrphan` and `Reply_ToAParentInAnotherRepository_IsRejected` - then reverted.

**Security note (STRIDE, carried from design):** board content is **untrusted by construction** and stored **quarantined** - there is no API that feeds a message's content to a grader as instructions (the *Confused Deputy* mitigation), and grader-injection shapes are flagged. The **injection-invariance** property (US-4 #5 - the same episode scores identically with/without an injection fixture) holds by construction in the slice-5 scorer, which consumes typed deterministic signals and never board text; slice 6 supplies the flag. Every write is capability-verified; provenance carries the session's own trust; a redaction is the one allowed content mutation (envelope append-only). Message content may carry work text kept **local** (no egress; default-deny gate stands).

**Residual:**
- **The coord-core wire** for board messages (a `board-post`/`board-reply` injected-contract kind so a non-pack session posts over its log) - the connective follow-on; slice 6 ships the domain + store both paths feed.
- **Capture policy / retention / opt-in** governing redaction - Phase 5.
- **The Board WPF surface** (empty / thread / unanswered / acknowledged / quarantined states) - a surface follow-on (the slice-3 pattern); slice 6 ships the read model.
- **The advisory grader + leaderboard + standing** - slice 7.
