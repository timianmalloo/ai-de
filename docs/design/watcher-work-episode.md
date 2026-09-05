---
id: design-watcher-work-episode
title: "Loomkeeper Work Episode - Goal/Done-When Lifecycle"
type: design
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [loomkeeper, watcher, design, work-episode, goal, done-when, scoring, phase-2]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-phase1-skeleton, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
  - { to: adr-0023-watcher-observation-projection, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper Work Episode (slice 4): the unit scoring attaches to. An episode binds one
  immutable goal + done-condition (mirroring the AI-Forward CT19 goal-state triple Goal / Done when /
  Not in scope) to one bounded interval of one authenticated session, with observable activity (spans
  in the interval) bound to it. Changing the goal starts a NEW episode generation (the aggregate
  invariant); a capability-verified Open/Reframe/Close lifecycle records a DECLARED outcome. The
  quality judgment (was the goal actually met, did it drift) is the Weave's job (slice 5), not here.
---

# Design: Loomkeeper Work Episode

- **Status:** Accepted · **Tier:** T2 · **Phase:** 2, slice 4 · **Depends on:** the Phase-1 skeleton (session identity, capability, spans, store).
- **Informed by the AI-Forward `done_when` work** (`ai-forward` main, CT19–CT24 + PACK-O): a turn's **goal-state** is the triple **Goal · Done when · Not in scope**, where **`done_when` is a *terminal condition*** — "it must be possible to point at a result and say whether it is met." Its absence is defect class **PACK-O**, whose three faces — *needless ceremony* (work past sufficiency), *under-validation* (no acceptance criteria), *over-constraining* (an invented bound) — are exactly the agent-scoring axes this substrate exists to measure. The rung-2 control records `done_when` alongside the outcome so scope drift is **mechanically minable**. **A Loomkeeper Work Episode is the durable, per-session, scoreable projection of that goal-state** — so it uses the *same vocabulary* (Goal / DoneWhen / NotInScope), making the substrate symbiotic with the AI-Forward audit ledger rather than a parallel one.

## 1. Responsibility and boundary

One responsibility: **bind one immutable goal + done-condition to one bounded interval of one authenticated session, with the observable activity that occurred inside it** (spec US-6, lines 201-234). It is *the unit scoring attaches to* — but it does **not** score. It owns the episode aggregate and its lifecycle; it borrows session identity/capability (Phase 1) and the span store; it does **not** own the Weave, the grader, floors, or coverage (slice 5+).

**The trust boundary is episode management:** only the authenticated session may open, reframe, or close *its* episodes. Every lifecycle call presents the session's capability and is verified (LK-0001 forgery on mismatch), exactly as span ingest is (ADR-0020 trusted-registrar-harness-model-identity).

## 2. Data model (settled first)

**Bounded context:** Work Evaluation (spec §"Work Evaluation" — "evaluates one bounded goal episode; separates deterministic facts from advisory judgments").

**Aggregate + the one invariant it protects:**

| Aggregate root | One protected invariant |
|---|---|
| **WorkEpisode** | One episode fixes **one immutable** goal + done-condition over **one bounded interval** of one session; **changing the goal starts a NEW episode** (a new generation), never a mutation (spec line 211/234). |

- **Dimensions (value objects):** `Goal(string Statement)`, `DoneCondition(string Statement)` (the `done_when` terminal condition), `EpisodeGeneration(long Value)` — `sealed record`s compared by value. `NotInScope` is an optional string (the third CT19 element; enriches the drift signal — activity against a not-in-scope area is drift).
- **The episode is a dimension with a lifecycle** (opens → accrues → closes), not an append-only fact — so it is an **upsert current-state row** keyed by `EpisodeId` (like the session dimension), carrying `OpenedAt`, `ClosedAt?`, and `Outcome?`.
- **Grain:** *one row is exactly one Work Episode — one immutable (goal, done-condition, session, generation) over one interval [OpenedAt, ClosedAt?].* A reframe closes the current row (`Superseded`) and inserts a new row at the next generation.
- **Interval time base:** **wall-clock `TimeProvider`** (same base as span `RecordedAt`), **not** the monotonic liveness clock — because the episode binds *recorded activity*, and a span's `RecordedAt` is the thing it must compare against. (Liveness uses the monotonic clock precisely because it decides a *live* condition; an episode measures a *recorded* interval.)
- **Observable activity (US-6):** `SpanCountInInterval(sessionId, from, to)` — spans whose `RecordedAt` falls in the episode interval. This requires the in-memory store to retain each span's `RecordedAt` (SQLite already stores `recorded_at`). **Derive-don't-store (DM7):** the count is computed from the span facts, never a stored tally.
- **Outcome is a DECLARED lifecycle terminal state, not a quality score:** `EpisodeOutcome` ∈ { `Completed`, `Abandoned`, `Superseded`, `Blocked` }. Whether a `Completed` claim is *honest* (the goal was actually met vs. drifted past `done_when`) is the Weave's **Outcome-integrity** dimension (slice 5, spec line 251) — deliberately out of scope here so the deterministic fact and the advisory judgment stay separated (spec §"Work Evaluation").

**Change-surface (E7):** value objects → `WorkEpisode` dimension → `IWatcherObservationStore` (episode upsert + `SpanCountInInterval`) → `EpisodeProjection` (state + bound activity) → *(slice 5)* Weave. Every episode field has a **writer** (the lifecycle service) and a **compute reader** (the projection).

**Migration:** expand-only — new `work_episode_dim` table + a `recorded_at` retention in the in-memory store; no change to existing tables.

## 3. Contracts

```csharp
namespace AiDe.Core.Watcher;

public sealed record Goal(string Statement);
public sealed record DoneCondition(string Statement);   // the done_when terminal condition
public readonly record struct EpisodeGeneration(long Value);
public enum EpisodeOutcome { Completed, Abandoned, Superseded, Blocked }
public enum EpisodeState { Active, Closed }

public sealed record WorkEpisode(
    string EpisodeId, string SessionId, EpisodeGeneration Generation,
    Goal Goal, DoneCondition DoneWhen, string? NotInScope,
    DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt, EpisodeOutcome? Outcome)
{
    public EpisodeState State => ClosedAt is null ? EpisodeState.Active : EpisodeState.Closed;
}

public interface IWorkEpisodeService
{
    WorkEpisode Open(string sessionId, SessionCapability capability, Goal goal, DoneCondition doneWhen, string? notInScope = null);
    WorkEpisode Reframe(string episodeId, SessionCapability capability, Goal goal, DoneCondition doneWhen, string? notInScope = null); // supersede + new generation
    WorkEpisode Close(string episodeId, SessionCapability capability, EpisodeOutcome outcome);
}

// store additions (both impls):
void RecordEpisode(WorkEpisode episode);
WorkEpisode? FindEpisode(string episodeId);
IReadOnlyList<WorkEpisode> EpisodesForSession(string sessionId);
IReadOnlyList<WorkEpisode> AllEpisodes();
int SpanCountInInterval(string sessionId, DateTimeOffset from, DateTimeOffset toInclusive);

// projection:
public sealed class EpisodeProjection(IWatcherObservationStore store)
{
    EpisodeState State(string episodeId);
    int ObservedSpanCount(string episodeId);       // spans in [OpenedAt, ClosedAt ?? now]
    IReadOnlyList<WorkEpisode> ForSession(string sessionId);
}
```

## 4. Failure-mode analysis

| # | Failure mode | Disposition |
|---|---|---|
| Input | empty goal or done-condition | **Prevent** — reject at `Open`/`Reframe` with `LK-0002` (invalid binding); negative test |
| Identity | a process opens/closes an episode for a session it is not | **Detect+prevent** — capability verified (LK-0001 forgery); negative test |
| State | goal is changed on an open episode | **Prevent** — no mutation path; `Reframe` closes the current (`Superseded`) and opens a new generation; test asserts the old is Superseded and a new gen exists |
| State | close an already-closed episode | **Prevent** — reject (LK-0002 / invalid state); a closed episode is immutable; test |
| State | reframe/close an unknown episode id | **Prevent** — reject (not found); test |
| State | two sequential episodes per session | **Allow** — generations increment; `EpisodesForSession` returns both; test |
| Time | span `RecordedAt` outside the interval | **Correct** — excluded from the bound count; a span exactly at an endpoint is included (`[from, toInclusive]`); test |
| Time | open episode (no ClosedAt) activity count | **Correct** — interval is `[OpenedAt, now]`; test |
| Resource | unbounded episodes in memory | **Accept (bounded)** — `simplify:` (SQLite bounds/persists), mirrors the skeleton store |

## 5. Security / privacy (STRIDE / LINDDUN-lite)

- **Spoofing/Tampering:** capability-verified lifecycle (LK-0001); an episode is a claim by a session and is minted only for the authenticated session. A reframe/close by a forged capability is rejected and the episode is untouched.
- **Privacy:** the **Goal / DoneWhen / NotInScope statements are session-authored free text and MAY carry work content** — this is the first watcher surface that can. For slice 4 they are stored **locally** in the observation store like any other fact; they are **not** egressed (the default-deny egress gate stands, LK-0003) and carry **no personal data by construction** (they describe a task, not a person). Work-content capture governance (redaction, retention, opt-in) is Phase 5; this slice records the goal-state locally and adds no egress path. Recorded as an explicit LINDDUN note: the new personal-data surface is *task text*, kept local, non-egressed.

## 6. Instrumentation (IO1)

Operator questions answerable without a debugger: how many episodes **opened**, how many **closed** and with which **outcome**, how many **reframes** (a reframe rate is itself a scope-stability signal — the durable cousin of PACK-O's drift), and the **observed span count** per episode. Each is a counter/queryable on the normal path.

## 7. Test plan (Testing Strategy D1, D4; E11)

- **D1 (service):** open binds goal/done/interval + mints id/gen; empty goal/done → LK-0002; forged capability → LK-0001 (open/reframe/close); reframe supersedes the old (Superseded) and opens gen+1 with the new goal; close records outcome + ClosedAt; close-already-closed rejected; reframe/close-unknown rejected; two sequential episodes per session.
- **D1 (projection):** State Active before close / Closed after; ObservedSpanCount binds only spans in the interval (endpoint inclusive, outside excluded, open-episode uses now); ForSession returns generations in order.
- **D4 (SQLite):** an episode persists across reopen with its immutable goal/done and outcome; `SpanCountInInterval` over the real `recorded_at` column; append-only span facts unaffected.
- **E11 (composition):** register → open episode → ingest spans in the interval → close(Completed) → projection reports Closed + the right bound span count, through the real registrar + store.
- **Mutation:** one load-bearing oracle (the reframe-supersedes-and-increments-generation invariant, or the interval endpoint inclusion) red-then-revert; counters compile-enforced.

## 8. Ladder / simplicity

Reuse the registrar (capability verify), the store idiom (dimension upsert + a fact-count query), the `TimeProvider`, and the value-object idiom — **no new dependency**. The episode id is minted (injectable `Func<string>`, like the session id) since it is a lifecycle aggregate, not a content-addressed fact. `NotInScope` is a single nullable column, not a table.

## 9. Symbiosis with AI-Forward (the done_when alignment)

The episode's `(Goal, DoneWhen, NotInScope)` triple is **deliberately identical** to the CT19 goal-state, so:
- an **AI-Forward** session's audit entry already carries `goal` + `done_when` (AL5b) → a future thin ingest maps that entry to `Open`/`Close` (one ledger, projected — the slice-2 principle), with **no translation**;
- a **non-AI-Forward** session supplies the same triple via an injected-contract `episode-open`/`episode-close` kind (the slice-2 contract vocabulary, extended) — same domain, same store.

**Residual (out of slice 4):** the **wire ingest** of the goal-state (the audit-log AL5b mapping and the injected-contract episode kinds) is the connective follow-on; slice 4 ships the **deterministic domain + lifecycle + store + projection** that both paths feed, with the vocabulary already aligned. The **Weave scoring** of the episode (outcome integrity, goal focus, the PACK-O drift judgment) is slice 5.

## 10. Gate record

`GATE design · 2026-08-31 · reviewers (Adversary Mode): Data & Persistence (episode is a dimension; immutable goal/done; expand-only migration), Security & Identity (capability-verified lifecycle; new task-text surface kept local), Patterns Expert ⇄ Simplifier (mint-vs-content-addressed; no new dependency), Test Architect (negative-first, reframe-invariant, interval endpoints), SRE (reframe-rate + outcome counters) · verdict: PASS-WITH-CONDITIONS · conditions — wire ingest (audit AL5b + contract episode kinds) and Weave scoring are later slices; work-content governance is Phase 5`

**Handoff:** → `/implement` this design (value objects + service → store → projection, TDD).
