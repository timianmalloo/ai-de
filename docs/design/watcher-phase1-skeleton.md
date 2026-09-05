---
id: design-watcher-phase1-skeleton
title: "Loomkeeper Phase-1 Walking Skeleton - Deterministic Observation Core"
type: design
status: draft
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, design, identity, ingest, liveness, egress, walking-skeleton]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
  - { to: adr-0023-watcher-observation-projection, rel: depends-on }
  - { to: adr-0024-credential-backed-grading-egress, rel: depends-on }
  - { to: adr-0002-workspace-fact-store, rel: depends-on }
  - { to: adr-0006-terminal-delivery-semantics, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Detailed design for the Loomkeeper Phase-1 walking skeleton: the deterministic T0 observation core -
  identity value objects with harness/model, a Trusted Registrar issuing per-session capabilities,
  content-addressed idempotent span ingest, monotonic liveness projection, and a default-deny egress
  gate - over an IWatcherObservationStore seam with an in-memory implementation. No personal data, no
  model, no network.
---

# Design: Loomkeeper Phase-1 Walking Skeleton

- **Status:** Draft
- **Tier:** T2 · **Phase:** 1 (walking skeleton) of the [Loomkeeper architecture](../architecture/loomkeeper.md) phasing plan
- **Driving spec:** [`docs/specs/agentic-watcher-substrate.md`](../specs/agentic-watcher-substrate.md) (US-1, US-2, US-13, US-15 egress default)
- **Author / date:** @timianmalloo · 2026-08-30
- **Grounding traversal:** `architecture-loomkeeper` (implements) → ADR-0020 trusted-registrar-harness-model-identity / ADR-0023 watcher-observation-projection / ADR-0024 credential-backed-grading-egress (depends-on) → `adr-0002-workspace-fact-store` (the `Facts/` + `Store/` idiom this reuses). Conforms to the existing `AiDe.Core` conventions: `sealed record` facts with a **grain comment**, **SHA256 content-addressed identity** for idempotent replay (the `EvidenceAssertion` pattern), `Nullable enable`, `TreatWarningsAsErrors=true`, xUnit in `AiDe.Core.Tests`.

## 1. Responsibility and boundary

One responsibility: **turn raw session events into a trustworthy, deterministic observation state** — who is running (identity), whether they are alive (liveness), each observed operation once (idempotent span), and nothing leaves the device (egress deny). This is the T0 deterministic heart of Phase 1. It owns identity, capability, span dedup, liveness, and the egress default; it borrows persistence (behind a seam) and does **not** own scoring, the grader, the board, the UI, or work-content capture.

**What crosses the boundary:** a registration binding and per-event capability in; `RegisteredSession`, `LivenessState`, `IngestOutcome`, and `EgressDecision` out. **The trust boundary is the event-ingest seam** — where a process claims a session identity and emits events (§6 STRIDE).

**Placement in phasing.** This is Phase 1's deterministic core. Its one mock-substitutable seam is **`IWatcherObservationStore`**, with **two implementations now in place**: an in-memory store for fast unit composition, and a **SQLite store** (`SqliteWatcherObservationStore`, reusing the ADR-0002 idiom — WAL, append-only facts enforced by triggers, single writer) that persists observations across a restart. Both satisfy the same contract tests. The **daemon ingest wire** (requires spike **S1** — harness OTLP/injected-contract ingest) and the **WPF Sessions-treegrid row** are the remaining Phase-1 slices.

## 2. Data model (settled first)

**Bounded context:** Session Observability, plus a thin slice of Capture & Scoring Governance (the egress default). Ubiquitous language per the spec glossary.

**Aggregates and the one invariant each protects:**

| Aggregate root | One protected invariant |
|---|---|
| **AgentSession** | One live session generation binds exactly one repository, worktree, terminal, agent, **harness**, **model**, and registration authority; **every event must present the session's capability**. |
| **ObservedSpan** | One span belongs to exactly one trace and session, is immutable, and is **idempotent under duplicate delivery** (content-addressed id). |
| **EgressPolicy** | Every egress path is **denied until an explicit opt-in enables it** (default `Blocked`). |

**Durable representation (ADR-0023 watcher-observation-projection, reusing ADR-0002):** dimensions + append-only facts.
- **Dimensions (value objects):** `RepositoryIdentity`, `WorktreeIdentity`, `TerminalIdentity`, `AgentIdentity`, `HarnessIdentity`, `ModelIdentity`, `SessionGeneration` — all `sealed record` value objects compared by value.
- **Fact:** `ObservedSpan`. **Grain:** *one row is exactly one observed operation emitted by one authenticated session generation, identified by its source span identity, recorded at ingest.* **Additivity:** span **count** is additive within an episode; **current live sessions** is a semi-additive point-in-time measure, never summed across time. **History:** the Phase-1 facts are immutable append-only; no SCD attribute here (policy Type-2 history is Phase 5).
- **Derive-don't-store (DM7, ADR-0001):** `LivenessState` is **computed** from the latest heartbeat and the monotonic clock — never stored. There is no stored "is-alive" flag.

**Change-surface list (E7)** this data must reach, ticked off in implementation: identity value objects → `ObservedSpan` fact → `IWatcherObservationStore` (persist) → `LivenessProjection` (compute reader) → *(remaining Phase 1)* daemon wire → UI treegrid row. Every field of `ObservedSpan` has a **writer** (ingest) and a **compute reader** (liveness reads `RecordedAt`/session; §5).

**Migration:** none — new facts/dimensions added beside the existing store (expand only).

## 3. Contracts

```csharp
namespace AiDe.Core.Watcher;

// Dimensions (value objects) - compared by value; a canonical key disambiguates same-named repos.
public sealed record RepositoryIdentity(string CanonicalPath, string DisplayName);
public sealed record WorktreeIdentity(RepositoryIdentity Repository, string Branch, string Path);
public sealed record TerminalIdentity(string TerminalId);
public sealed record AgentIdentity(string AgentName);
public sealed record HarnessIdentity(string Name, string Version);   // Claude Code / Copilot ...
public sealed record ModelIdentity(string Name, string Version);     // Opus 4.8 / GPT-5.6 Terra ...
public readonly record struct SessionGeneration(long Value);

public enum TrustClassification { Verified, Asserted }
public enum LivenessState { Alive, Stale, Ended }

// The unforgeable per-session secret. Never logged; compared in constant time.
public sealed class SessionCapability { /* opaque 256-bit token */ }

public sealed record SessionBinding(
    RepositoryIdentity Repository, WorktreeIdentity Worktree, TerminalIdentity Terminal,
    AgentIdentity Agent, HarnessIdentity? Harness, ModelIdentity? Model,   // null => Not Recorded
    TrustClassification Trust);

public sealed record RegisteredSession(
    string SessionId, SessionGeneration Generation, SessionBinding Binding, SessionCapability Capability);

public interface ITrustedRegistrar
{
    RegisteredSession Register(SessionBinding binding);                    // new id + generation + capability
    RegisteredSession RegisterNextGeneration(string sessionId, SessionBinding binding); // restart
    bool Verify(string sessionId, SessionCapability presented);           // false => forgery (LK-0001)
}

public sealed record ObservedSpan(
    string SessionId, string TraceId, string SourceSpanId, string OperationName,
    DateTimeOffset RecordedAt)
{
    public string SpanId { get; }   // computed SHA256 canonical (SessionId,TraceId,SourceSpanId) - idempotent
}

public enum IngestOutcome { Accepted, DuplicateIgnored, Rejected }

public interface ISpanIngest
{
    IngestOutcome Ingest(string sessionId, SessionCapability capability, ObservedSpan span);
}

public interface ILivenessProjection
{
    LivenessState Evaluate(string sessionId);   // computed from heartbeat + monotonic clock
    void Heartbeat(string sessionId, SessionCapability capability);
}

public enum EgressDecision { Blocked, Allowed }

public interface IEgressGate
{
    EgressDecision Decide(string pathId);   // default Blocked
    void OptIn(string pathId);              // enables exactly one path
    void Revoke(string pathId);
}

public interface IWatcherObservationStore   // the mock-substitutable seam (in-memory now; SQLite later)
{
    bool TryAppendSpan(ObservedSpan span);   // false => duplicate id already present (idempotent)
    void UpsertHeartbeat(string sessionId, long monotonicTicks);
    long? LastHeartbeat(string sessionId);
    void RecordSession(RegisteredSession session);
    RegisteredSession? FindSession(string sessionId);
}

public interface IMonotonicClock { long Ticks { get; } long TicksPerSecond { get; } }  // NOT wall clock
```

**Consumed contracts:** `Microsoft.Data.Sqlite` (already referenced; the SQLite store is deferred, so not consumed in this slice); `System.Security.Cryptography` (`SHA256`, `RandomNumberGenerator`, `CryptographicOperations.FixedTimeEquals`) — stdlib, established.

## 4. Patterns (named + justified; Solution-Selection Ladder climbed)

- **Value Object** (DDD) for identities — **rung 2 reuse**: the repo's `sealed record` idiom (`Provenance`, `EvidenceAssertion`). No new type machinery.
- **Content-addressed identity** for idempotent ingest — **rung 2 reuse** of `EvidenceAssertion.ComputeId` (SHA256 over a `\u001F`-joined canonical form). *Pattern: LOA 5.3 Idempotent Action* — a replayed span is the same id, so the store dedups by construction. Justified over a sequence number (out-of-order/duplicate delivery, ADR-0006).
- **Capability-based security** for the per-session token — the established unforgeable-identity idiom; a 256-bit `RandomNumberGenerator` token compared with `CryptographicOperations.FixedTimeEquals`. Justified over trusting the session id (forgeable, ADR-0007/0020). Simplifier: no ACL framework — one secret per session is the smallest correct thing.
- **Projection / derived view** (ADR-0001) for liveness — **rung 2 reuse** of the `Projections/` idiom; liveness is computed, never stored (DM7).
- **Default-deny Gateway** for egress (ADR-0011/0018) — the smallest correct control: a set of opted-in path ids; everything else `Blocked`.
- **`simplify:` markers:** the in-memory store is unbounded in this slice (`simplify:` — ceiling: fine at the reference scale; upgrade trigger: the SQLite store lands, which bounds and persists). The in-memory store is the walking-skeleton seam, not the production store.

## 5. Error and concurrency model

- **Single-writer ingest** (ADR-0002: writes serialize through the daemon's bounded queue), but the in-memory store is made **thread-safe** (a `lock` around the span set and heartbeat map) so a concurrent caller cannot corrupt it or double-append.
- **Capability comparison is constant-time** (`FixedTimeEquals`) to deny a timing side-channel.
- **Monotonic clock injected** (`IMonotonicClock` over `Stopwatch.GetTimestamp`), never `DateTime.Now`, so a wall-clock change cannot flip liveness (defect class TEST-CLOCK; spec US-2).
- **Stable error codes:** `LK-0001` forgery/invalid capability; `LK-0002` invalid registration binding (empty required field); `LK-0003` egress denied. Failures are typed results/exceptions carrying the code, never bare booleans at the boundary.

## 6. Failure-mode analysis (mode → disposition)

| Category | Failure mode | Disposition |
|---|---|---|
| Input | Null/empty required binding field (repo path, terminal, agent) | **Prevent** — reject at `Register` with `LK-0002`; negative test |
| Input | Harness or model unknown | **Accept** — bound as `null` → renders Not Recorded; episode still observable (spec US-13) |
| Input | Duplicate span (same source id) redelivered | **Prevent** — content-addressed id; `TryAppendSpan` returns false → `DuplicateIgnored`; test |
| Input | Out-of-order span | **Prevent** — facts are order-independent; accepted regardless of arrival order; test |
| Identity | Process presents wrong/absent capability | **Detect + prevent** — `Verify` false → `Rejected` + `LK-0001` forgery recorded; negative test |
| Identity | Terminal restart reuses a process id | **Prevent** — `RegisterNextGeneration` issues a new generation that cannot inherit prior liveness/capability; test |
| Concurrency | Two identical spans ingested concurrently | **Prevent** — locked at-most-once append; test with parallel writers |
| State/time | Heartbeat expires while process still runs | **Detect** — liveness → `Stale`; test |
| State/time | Wall-clock moves forward/backward | **Prevent** — monotonic clock; state unchanged; test |
| Resource | Unbounded in-memory growth | **Accept (bounded)** — `simplify:` marker; SQLite store bounds it (remaining Phase 1) |
| Egress | Any path attempts egress with no opt-in | **Prevent** — default `Blocked` (`LK-0003`); negative test |

## 7. Adversarial analysis (STRIDE-lite) — boundary: event ingest

| Threat | Disposition |
|---|---|
| **Spoofing** — a process claims another session's id | **Mitigate** — per-session capability verified on every event (`Verify`, constant-time); forgery → `Rejected` + recorded. Negative test: wrong/forged capability rejected. |
| **Tampering** — span altered in flight | **Mitigate** — span identity is content-addressed; a changed span is a different id, not a silent overwrite. (Content payload is out of Phase-1 scope.) |
| **Repudiation** — a forgery leaves no trace | **Detect** — forgery attempts are recorded as an observation fact (an operator can see the attempt). |
| **Information disclosure** — capability leaks | **Mitigate** — capabilities are secrets, never logged or emitted (O11); no work content in this slice. |
| **DoS** — flood of spans | **Transfer + accept** — bounded by the daemon's ingest queue (ADR-0002); in-memory growth accepted for the skeleton (`simplify:`). |
| **Elevation** — asserted identity gains authority | **Mitigate** — `Asserted` trust is labelled and (in later phases) cannot clear a floor; the capability itself is unforgeable. |

## 8. Privacy analysis (LINDDUN-lite)

**This slice touches no personal data.** The identities are repository / worktree / terminal / agent / harness / model / session — tools and machines, not persons. Phase-1 `ObservedSpan` carries operation **metadata** (name, trace linkage, timestamps), **not** prompt/code/transcript content; work-content capture is Phase 5, opt-in, behind the governance gate, and re-analysed there. The `SessionCapability` is a **secret** (a Security concern), not a person's data. Explicit negative recorded per the skill's requirement; the Privacy veto has nothing to bind to in this slice.

## 9. Telemetry (Observability Standard)

- **Spans:** `loomkeeper.register`, `loomkeeper.ingest`, `loomkeeper.heartbeat`, `loomkeeper.egress.decide`.
- **Error codes:** `LK-0001` (forgery), `LK-0002` (invalid binding), `LK-0003` (egress denied) — stable, documented in a `WatcherErrorCodes` constants type.
- **Metrics:** `loomkeeper.registrations`, `loomkeeper.forgery_attempts`, `loomkeeper.spans_ingested`, `loomkeeper.spans_deduped`, `loomkeeper.sessions_by_liveness{state}`.
- **Structured logs** carry `trace_id`/`span_id` from the active `Activity`; **no capability, no content** in any log (O11). The library exposes the error codes and outcomes; the daemon wires the `ActivitySource`/`Meter` (kept out of the pure core so the core stays deterministically testable — the instrumentation seam is the daemon).

## 10. Test plan (Testing Strategy — triggers T1, T2; D0 always)

- **D0 (every test):** deterministic — injected `IMonotonicClock` and injected capability RNG (seedable fake); no wall clock; no sleeps; parallel-safe.
- **D1 unit + mutation resistance:** identity value equality and canonical-key disambiguation of same-named repos; capability issue uniqueness; span id determinism; liveness transitions Alive→Stale→Ended; egress default-deny and single-path opt-in.
- **D2 property-based:** span id — same `(session,trace,source)` ⇒ same id; any differing field ⇒ different id (idempotence + collision-freedom); capability tokens unique across N issues.
- **Negative / error-path (one per handled failure mode, red-first):** invalid binding `LK-0002`; forged/wrong-session capability `LK-0001`; duplicate span `DuplicateIgnored`; out-of-order span accepted; concurrent identical spans → single append; wall-clock change does not flip liveness; egress `Blocked` by default and only the opted-in path `Allowed`.
- **Composition proof (E11-lite):** one test drives `Register → Heartbeat → Ingest → Evaluate` through the real in-memory store and asserts the end-to-end observation state — the walking skeleton proven through its real composition seam.
- **UI craft gate:** N/A this slice (no UI surface; the treegrid row is a later Phase-1 task).

## 11. Confidence ledger and residual risk

| Claim | Evidence | Label |
|---|---|---|
| Content-addressed ingest is idempotent | Reuses verified `EvidenceAssertion.ComputeId` pattern | Verified (reuse) |
| Monotonic liveness resists clock skew | `Stopwatch.GetTimestamp` is monotonic (established) | Verified |
| Capability compare is timing-safe | `CryptographicOperations.FixedTimeEquals` (stdlib) | Verified |
| In-memory store is the right seam for the skeleton | Architecture §4 (mocked seams are contracts) | Inferred |
| SQLite store persists observations with the append-only invariant enforced | `SqliteWatcherObservationStoreTests` (11 tests, real engine, reopen + trigger) | Verified |
| Daemon ingest wire + UI row complete Phase 1 | not built this slice | **Flagged — remaining Phase-1 tasks; S1 spike** |

**Residual risk:** the in-memory store is unbounded (accepted, `simplify:`); the **daemon ingest wire** and the **WPF treegrid row** are the remaining Phase-1 slices and are not built here. The SQLite store persists but is not yet wired into the daemon.

## 12. Gate record

`GATE design · 2026-08-30 · reviewers (Adversary Mode): Patterns Expert ⇄ Simplifier, Test Architect, Security & Identity, SRE, Data & Persistence · exit criteria: single responsibility; data model settled first (aggregates+invariants, grain, additivity, history, derive-don't-store); contracts named; patterns justified via ladder; failure modes + STRIDE + LINDDUN dispositioned; telemetry + error codes named; every triggered Testing-Strategy directive in the plan; change-surface list written · verdict: PASS-WITH-CONDITIONS · vetoes: Security (capability + default-deny egress) and Test Architect (negative-first plan) satisfied; conditions — SQLite store, daemon ingest wire, and UI row are remaining Phase-1 tasks`

**Handoff:** → `/implement` this design (identity + registrar → idempotent ingest → liveness → egress gate, TDD).
