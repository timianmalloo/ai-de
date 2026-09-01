---
id: design-watcher-ingest-wire
title: "Loomkeeper Ingest Wire - Harness Telemetry to Observation"
type: design
status: draft
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, design, ingest, otlp, adapter, phase-1]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-phase1-skeleton, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
  - { to: adr-0017-watcher-observation-projection, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper ingest wire: a dual-path adapter that turns harness telemetry - native
  OTel spans and a registration/session-start event - into TrustedRegistrar registrations and
  capability-verified SpanIngest calls. Its deterministic core is a pure OtelSpanMapper (built now);
  the OTLP transport receiver and daemon host remain. Contract established by spike S1.
---

# Design: Loomkeeper Ingest Wire

- **Status:** Draft · **Tier:** T2 · **Phase:** 1 (the remaining ingest slice) of the [Loomkeeper architecture](../architecture/loomkeeper.md)
- **Driving spec:** [`docs/specs/agentic-watcher-substrate.md`](../specs/agentic-watcher-substrate.md) (US-1 registration, US-2 liveness, US-13 harness/model)
- **Refines:** [`design-watcher-phase1-skeleton`](watcher-phase1-skeleton.md) — feeds its already-built `TrustedRegistrar` and `SpanIngest`.
- **Established contract:** **spike S1** (`spikes/watcher-otlp-ingest/FINDINGS.md`, PASS) — the OTel-span and registration mappings, proven against the real `Activity` primitive, with the GenAI attribute vocabulary pinned (Development-status upstream).

## 1. Responsibility and boundary

One responsibility: **adapt a harness's telemetry into the watcher's domain** — nothing more. It turns (a) OTel spans into `ObservedSpan`s and (b) a registration event into a `SessionBinding`, then funnels both through the already-built `TrustedRegistrar` and `SpanIngest`. It owns the **anti-corruption boundary** that isolates the preview OTel/GenAI vocabulary from our domain; it does **not** own scoring, persistence, or the capability logic (that is the registrar/ingest it calls).

**Trust boundary (new):** the **event-ingest edge** — a process claims a session via a span's `session.id` or a registration event. This is where spoofing and flooding enter (§6).

**Placement in phasing.** The wire's **deterministic core is `OtelSpanMapper`** — a pure, fully-testable attribute→domain mapping, built in this slice. The **OTLP transport receiver** (accepting real exports over `OTEL_EXPORTER_OTLP_ENDPOINT`) and the **daemon host** that owns the bounded ingest queue are the remaining Phase-1 work; OTLP/HTTP is a stable protocol adoptable without a further spike (S1 finding).

## 2. Data model

No new persisted shapes. The wire is **stateless**: it maps to the existing `ObservedSpan` fact and `SessionBinding` (design-watcher-phase1-skeleton §2) and writes only through `SpanIngest`/`TrustedRegistrar` into the existing store. The **GenAI attribute keys are a pinned snapshot** (a value, versioned as a contract), not persisted data.

**Change-surface list (E7):** harness event → `OtelSpanMapper` → `TrustedRegistrar.Register` / `SpanIngest.Ingest` → existing store → (existing) liveness/UI. No new field crosses a store boundary; the mapper adds no column.

## 3. Contracts

```csharp
namespace AiDe.Core.Watcher;

// Transport-neutral inputs: an OTLP receiver or an ActivityListener constructs these, so the mapper
// is not coupled to any one transport (spike S1: the mapping is the contract, not the wire).
public sealed record HarnessSpan(
    string TraceId, string SpanId, string OperationName,
    IReadOnlyDictionary<string, string?> Attributes);

public sealed record HarnessRegistration(IReadOnlyDictionary<string, string?> Attributes);

// The pinned OTel/GenAI attribute snapshot (Development-status upstream - a change here is a
// contract change with a regression gate, A6).
public static class OtelAttributes
{
    public const string SessionId = "session.id";
    public const string ServiceName = "service.name";        // -> Harness name
    public const string ServiceVersion = "service.version";
    public const string GenAiModel = "gen_ai.request.model";  // -> Model name
    public const string GenAiModelVersion = "gen_ai.model.version";
    // identity supplied by the injected contract / session-start:
    public const string RepoPath = "repo.canonical_path";
    public const string RepoDisplay = "repo.display_name";
    public const string WorktreeBranch = "worktree.branch";
    public const string WorktreePath = "worktree.path";
    public const string TerminalId = "terminal.id";
    public const string AgentName = "agent.name";
}

// Pattern: Anti-Corruption Layer (DDD) + Adapter. Pure and deterministic.
public static class OtelSpanMapper
{
    // Throws WatcherException(LK-0004) when session.id is absent (an unmappable span).
    public static ObservedSpan MapSpan(HarnessSpan span, DateTimeOffset recordedAt);

    // Throws WatcherException(LK-0004) when a required identity attribute is absent.
    // Harness/Model absent => null (Not Recorded); trust Verified iff the harness names itself.
    public static SessionBinding MapRegistration(HarnessRegistration registration);
}
```

**Consumed contract (established, S1):** the OpenTelemetry span data model (`trace_id`, `span_id`, span name, attributes) and the **Development-status** GenAI conventions — pinned in `OtelAttributes`.

## 4. Patterns (named + justified; ladder climbed)

- **Anti-Corruption Layer** (DDD) — the mapper is the seam that keeps the preview OTel/GenAI vocabulary out of the domain; when the upstream schema churns, only the mapper and its regression test change. Justified over letting OTel attribute strings leak into the registrar/ingest.
- **Adapter / Ports-and-Adapters** — transport-neutral input records so the OTLP receiver and an in-process `ActivityListener` are interchangeable ports. **Rung 2 reuse**: maps to the existing `ObservedSpan`/`SessionBinding`; adds no new domain type.
- **The capability is the trust anchor, not the mapper** — the wire binds a span to the **registration capability** (already built), so `session.id` is treated as a claim, not authority (S1 finding 7). No new security machinery.
- `simplify:` — the bounded ingest queue and OTLP transport are deferred to the daemon host; the mapper is synchronous and pure (ceiling: fine as a library core; upgrade trigger: the transport lands and needs backpressure).

## 5. Error and concurrency model

- The mapper is **pure and stateless** → trivially thread-safe; no shared state.
- **Stable error code `LK-0004` (MalformedEvent)** for an unmappable span (no `session.id`) or a registration missing a required identity attribute. Raised as a typed `WatcherException`; the wire (transport) catches it and **quarantines** the event rather than crashing.
- Duplicate/out-of-order spans are already idempotent downstream (`SpanIngest`); the mapper does not dedup.

## 6. Failure-mode analysis (mode → disposition)

| Category | Failure mode | Disposition |
|---|---|---|
| Input | Span with no `session.id` | **Prevent** — `MapSpan` throws `LK-0004`; wire quarantines; negative test |
| Input | Registration missing repo/terminal/agent | **Prevent** — `MapRegistration` throws `LK-0004`; and `TrustedRegistrar.Register` re-validates (LK-0002); test |
| Input | Harness/model attributes absent | **Accept** — mapped to null → Not Recorded, Asserted trust; test (spec US-13) |
| Dependency | OTel GenAI schema drifts (Development status) | **Detect** — keys pinned in `OtelAttributes`; a regression test asserts the snapshot (A6); a rename fails the gate |
| Identity | A process forges another session's `session.id` | **Mitigate** — the wire ingests through `SpanIngest`, which verifies the **capability** issued at registration; a forged span is `Rejected` (already tested) |
| State | Subprocess emits no OTLP (Claude Code gap) | **Detect** — those operations are Not Recorded / Partially Observed, never absent-as-healthy (S1 finding 2) |
| Resource | Span flood | **Mitigate (deferred)** — bounded ingest queue in the daemon host (transport slice); recorded as remaining |
| Time | Span `recordedAt` skew | **Prevent** — `recordedAt` is stamped at ingest by the wire, not trusted from the span |

## 7. Adversarial analysis (STRIDE-lite) — boundary: the ingest edge

| Threat | Disposition |
|---|---|
| **Spoofing** — forged `session.id` in a span | **Mitigate** — capability verified on every ingest (`SpanIngest`/`TrustedRegistrar`); the mapper never treats `session.id` as authority (S1 finding 7). Negative test already exists (`Ingest_ForgedCapability_Rejected`). |
| **Tampering** — altered span content | **Mitigate** — the content-addressed `SpanId` makes a changed span a different id, not a silent overwrite; Phase-1 carries metadata only. |
| **Repudiation** — untraceable forgery | **Detect** — a rejected ingest is observable (the registrar records the forgery attempt). |
| **Information disclosure** — attributes leak PII | **Mitigate** — Phase-1 maps only operation metadata (name, ids, model name); no prompt/code content; capabilities never appear in a span. |
| **DoS** — span flood exhausts memory | **Transfer (deferred)** — the daemon host's bounded queue (ADR-0002 ingest queue); recorded as a remaining Phase-1 control, not silently accepted. |
| **Elevation** — asserted identity gains authority | **Mitigate** — `MapRegistration` marks a self-unnamed harness `Asserted`; asserted trust cannot clear a floor (ADR-0020). |

## 8. Privacy analysis (LINDDUN-lite)

**The mapper touches no personal data.** It maps OTel span metadata (operation name, trace/span ids, harness/model names) and machine identities (repo/worktree/terminal/agent), never prompt/code/transcript content. Work-content capture is Phase 5, opt-in, behind the governance gate, and analysed there. Explicit negative recorded; the Privacy veto has nothing to bind to in this slice.

## 9. Telemetry (Observability Standard)

- **Spans:** `loomkeeper.ingest.map_span`, `loomkeeper.ingest.map_registration` (in the wire; the mapper is pure and instrument-free so it stays deterministically testable — the instrumentation seam is the transport).
- **Error code:** `LK-0004` MalformedEvent (added to `WatcherErrorCodes`).
- **Metrics (in the transport):** `loomkeeper.events_mapped`, `loomkeeper.events_quarantined{reason}`, `loomkeeper.schema_snapshot` (a gauge/label so a schema mismatch is visible).

## 10. Test plan (Testing Strategy — T1, plus A6 for the pinned schema)

- **D0 (every test):** deterministic; `recordedAt` injected; no wall clock.
- **D1 unit + mutation resistance:** `MapSpan` field mapping; `MapRegistration` full binding; opaque → Not Recorded + Asserted; harness-named → Verified.
- **Negative / error-path (red-first):** span with no `session.id` → `LK-0004`; registration missing repo/terminal/agent → `LK-0004`.
- **A6 schema-snapshot regression:** a test asserts the pinned `OtelAttributes` keys (`session.id`, `service.name`, `gen_ai.request.model`, …) so a silent upstream rename fails the gate — the OTel Development-status contract made checkable.
- **Composition (E11-lite):** a `HarnessRegistration` → `MapRegistration` → `TrustedRegistrar.Register`, then a `HarnessSpan` → `MapSpan` → `SpanIngest.Ingest` → `Accepted`, proving the wire's mapping composes with the built core through the real (in-memory or SQLite) store.

## 11. Confidence ledger and residual risk

| Claim | Evidence | Label |
|---|---|---|
| The OTel-span and registration mappings are correct | spike S1 PASS + the D1 tests | Verified |
| GenAI vocabulary is Development-status; pinning is the right control | KB S1; spike FINDINGS | Verified |
| The capability defeats `session.id` forgery | existing `SpanIngest` forgery test | Verified |
| Real OTLP transport hosting behaves as mapped | not built | **Flagged — remaining Phase-1 (transport + daemon host + bounded queue)** |

**Residual risk:** the OTLP transport receiver, the daemon host, and the bounded ingest queue (DoS control) are the remaining Phase-1 work; the WPF treegrid row remains after that.

## 12. Gate record

`GATE design · 2026-08-30 · reviewers (Adversary Mode): Patterns Expert ⇄ Simplifier, Test Architect, Security & Identity, SRE, Distributed Systems · exit criteria: single responsibility (adapter/ACL); contract established by spike S1; no new persisted shape; failure modes + STRIDE (new ingest boundary) + LINDDUN dispositioned; pinned-schema regression gate in the plan; capability is the trust anchor · verdict: PASS-WITH-CONDITIONS · vetoes: Security (forgery via capability) and Distributed Systems (backpressure) addressed; condition — the bounded ingest queue ships with the transport slice, recorded not silently accepted`

**Handoff:** → `/implement` the `OtelSpanMapper` (this design's deterministic core), then the OTLP transport + daemon host.
