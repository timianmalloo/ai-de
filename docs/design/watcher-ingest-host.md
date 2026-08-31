---
id: design-watcher-ingest-host
title: "Loomkeeper Ingest Host - Bounded Queue and Drain Loop"
type: design
status: draft
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, design, ingest, host, backpressure, phase-1]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-ingest-wire, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0020-trusted-registrar-harness-model-identity, rel: depends-on }
  - { to: adr-0002-workspace-fact-store, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper ingest host (slice 1): synchronous registration/heartbeat plus an async,
  bounded span queue (Channel.CreateBounded + DropOldest backpressure) drained into OtelSpanMapper ->
  TrustedRegistrar/SpanIngest, with forged spans rejected, malformed events quarantined, and counters
  exposing the operator questions. Transport is a substitutable IHarnessEventSource port; the OTLP
  network receiver is a follow-on adapter (slice 1b).
---

# Design: Loomkeeper Ingest Host

- **Status:** Draft · **Tier:** T2 · **Phase:** 1, slice 1 of the [Loomkeeper architecture](../architecture/loomkeeper.md)
- **Refines:** [`design-watcher-ingest-wire`](watcher-ingest-wire.md) — hosts and drives its `OtelSpanMapper` + the built `TrustedRegistrar`/`SpanIngest`.
- **Driving spec:** US-1 (register), US-2 (liveness), US-13 (harness/model), US-11 (fail honestly).

## 1. Responsibility and boundary

One responsibility: **host the ingest path** — accept harness events and turn them into registrations and capability-verified span ingests, absorbing a span flood without unbounded growth. It owns the **bounded span queue**, the **drain loop**, and the **disposition of forged/malformed/dropped events**; it borrows the mapper, registrar, ingest, and store (all built). It is **transport-agnostic**: events arrive through an `IHarnessEventSource` port, so the OTLP network receiver and an in-process source are interchangeable.

**Design decision (recorded):** registration and heartbeat are **synchronous, low-volume control** (a session needs its capability immediately); **spans are an async, high-volume stream** through the bounded queue (the flood/DoS surface). Conflating the two would either block the stream on registration or make registration racy.

**Placement in phasing.** This slice builds the **host core + bounded queue + drain** (pure, fully testable via a fake source). The **OTLP/HTTP network receiver** adapter — which authenticates a harness connection and produces events onto the port — is **slice 1b**, gated by an encoding/transport spike (its `session.id`→capability binding is a new trust boundary). Where the host runs (workbench process vs a watcher-level daemon) is a hosting choice deferred to integration; the host is a plain component either way.

## 2. Data model

**No new persisted shape.** The queue is **ephemeral, bounded, in-memory** — transient transport state, not a fact (nothing to persist; a dropped span is a coverage gap, not a lost record). It maps to the existing `ObservedSpan` fact and writes only through `SpanIngest`/`TrustedRegistrar`. Counters are in-memory operational measures (semi-additive point-in-time), exposed as an `IngestStats` snapshot — **derived, not stored**.

**Change-surface list (E7):** harness event → `IHarnessEventSource` → `IngestHost` (queue/drain) → `OtelSpanMapper` → `TrustedRegistrar`/`SpanIngest` → existing store → (existing) liveness/UI. No new field crosses a store boundary.

## 3. Contracts

```csharp
namespace AiDe.Core.Watcher;

// A harness event. Registration/heartbeat are handled synchronously; spans are queued.
public abstract record HarnessEvent;
public sealed record HarnessSpanEvent(SessionCapability Capability, HarnessSpan Span) : HarnessEvent;

// The transport port: an OTLP receiver or an in-process source produces events.
public interface IHarnessEventSource
{
    IAsyncEnumerable<HarnessSpanEvent> ReadSpansAsync(CancellationToken ct);
}

// Operator questions, answerable without a debugger (IO1): how many in/out/dropped/rejected.
public sealed record IngestStats(
    long Enqueued, long Dropped, long Ingested, long Deduped, long Rejected, long Quarantined);

public sealed class IngestHost
{
    public IngestHost(IWatcherObservationStore store, ITrustedRegistrar registrar,
        TimeProvider time, int queueCapacity = 1024);

    // Synchronous control path (low volume).
    public RegisteredSession Register(HarnessRegistration registration);   // map + register (LK-0004/LK-0002)
    public void Heartbeat(string sessionId, SessionCapability capability); // registrar.Heartbeat (LK-0001)

    // Async span stream (high volume, bounded). Enqueue never blocks; a full queue drops oldest.
    public bool Enqueue(HarnessSpanEvent spanEvent);   // false => dropped (backpressure)
    public ValueTask<int> DrainAvailableAsync(CancellationToken ct);  // deterministic: process what is queued
    public Task RunAsync(CancellationToken ct);        // production loop: wait-to-read + drain

    public IngestStats Stats { get; }   // a snapshot of the counters
}
```

**Consumed contracts (established, reused):** `OtelSpanMapper` (spike S1), `TrustedRegistrar`/`SpanIngest` (Phase-1 skeleton), `System.Threading.Channels.Channel.CreateBounded` (repo idiom, `ConPtyTerminalSession`), `TimeProvider` (LOA idiom, .NET 8+).

## 4. Patterns (named + justified; ladder climbed)

- **Bounded producer/consumer (Channel<T>)** — *Pattern: LOA `Channel<T>` backpressure*. **Rung 2 reuse** of the repo's `Channel.CreateBounded` + `BoundedChannelFullMode.DropOldest` idiom. Justified over an unbounded queue (the DoS the design must prevent).
- **Ports & Adapters** — `IHarnessEventSource` decouples the host from transport, so the OTLP receiver and a test source are interchangeable; the host tests need no network.
- **Drop-oldest backpressure** — a span flood degrades to a **coverage gap** (Not Recorded), the honest failure the spec mandates (US-11), not an OOM. Recorded as the accepted disposition.
- **The capability is the trust anchor** (existing) — the host verifies via `SpanIngest`; a forged span is Rejected, not stored. No new security machinery.
- `simplify:` — the queue is a single bounded channel with one drain loop (ceiling: fine at the reference scale of 1024 in-flight spans; upgrade trigger: measured sustained overflow, then partition per session).

## 5. Error and concurrency model

- **Single-writer-ish producer, single drain consumer.** `Channel.CreateBounded` is concurrency-safe; `Enqueue` uses `TryWrite` (non-blocking); the drain is one consumer (`SingleReader = true`).
- **`TimeProvider.GetUtcNow()`** stamps `ObservedSpan.RecordedAt` at ingest (never trusted from the span; injected for tests — no wall clock in tests).
- Registration/heartbeat throw the existing typed `WatcherException` codes (LK-0001/0002/0004); the async drain **never throws out** — a forged or malformed span increments a counter and is dropped, so one bad event cannot kill the loop.

## 6. Failure-mode analysis (mode → disposition)

| Category | Failure mode | Disposition |
|---|---|---|
| Resource | Span flood outruns the drain | **Mitigate** — bounded queue, `DropOldest`; `Dropped` counted; coverage gap, not OOM (US-11). Test fills past capacity and asserts drops |
| Input | Malformed span (no session.id) | **Prevent+detect** — `MapSpan` throws LK-0004; drain catches → `Quarantined++`, span dropped; loop survives. Test |
| Identity | Forged/wrong capability on a span | **Detect** — `SpanIngest` returns Rejected → `Rejected++`, not stored. Negative test |
| Input | Duplicate/out-of-order span | **Prevent** — idempotent downstream; `Deduped++`. Test |
| Input | Invalid registration | **Prevent** — `Register` throws LK-0004/LK-0002 synchronously to the caller. Test |
| Concurrency | Enqueue races the drain | **Prevent** — `Channel` is thread-safe; test with concurrent enqueues |
| State | Host stopped/unavailable | **Detect** — `RunAsync` honours cancellation; queued-but-undrained spans are a coverage gap on restart (US-11), never shown as current |
| Time | `recordedAt` skew | **Prevent** — stamped by the host via `TimeProvider`, not the span |

## 7. Adversarial analysis (STRIDE-lite) — boundary: the enqueue edge

| Threat | Disposition |
|---|---|
| **Spoofing** — a span claims another session | **Mitigate** — capability verified in `SpanIngest` (existing forgery test); the host trusts no `session.id` |
| **Tampering** — altered span | **Mitigate** — content-addressed `SpanId`; metadata only in Phase 1 |
| **Repudiation** — silent drop hides load | **Detect** — `Dropped`/`Rejected`/`Quarantined` counters are the audit signal (IngestStats) |
| **DoS** — span flood | **Mitigate** — bounded queue + DropOldest; the flood is absorbed and counted, not fatal. **The OTLP *network* receiver's own DoS (an unauthenticated port) is slice 1b's boundary** — named, not silently accepted |
| **Information disclosure** — attributes leak | **Mitigate** — metadata only; no content; capabilities never logged |
| **Elevation** — asserted identity gains authority | **Mitigate** — asserted trust can't clear a floor (existing) |

## 8. Privacy analysis (LINDDUN-lite)

**The host touches no personal data.** It moves `HarnessSpan` metadata and machine identities through a queue; content capture is Phase 5. Explicit negative recorded.

## 9. Telemetry (Observability Standard + Instrumentation-over-Inference)

- **The operator questions each have a named emitting source** (IO1): `IngestStats` counters — `Enqueued`, `Dropped`, `Ingested`, `Deduped`, `Rejected`, `Quarantined` — emitted on the normal path, readable without a flag. The daemon wires OTel `Meter`s over them; the library exposes the snapshot so the core stays deterministically testable.
- **Spans:** `loomkeeper.ingest.drain` per drain batch (in the host).
- **Error codes:** reuses LK-0001/0002/0004; no new code.
- **Degrades to "not recorded":** a dropped span is a visible `Dropped` increment (a coverage signal), never a silently wrong count.

## 10. Test plan (Testing Strategy — T1; D0 always)

- **D0:** deterministic — injected `TimeProvider` (fake), `DrainAvailableAsync` processes synchronously (no sleeps), parallel-safe.
- **D1 unit + mutation resistance:** register→capability; enqueue+drain→Accepted+stored+`Ingested`; duplicate→`Deduped`; **flood past capacity→`Dropped`** (backpressure); forged→`Rejected`+not stored; malformed→`Quarantined`+loop survives; heartbeat keeps Alive.
- **Negative/error-path (red-first):** forged capability; malformed span; invalid registration; a full queue.
- **Concurrency:** N concurrent `Enqueue` + a single drain → counts reconcile (enqueued = ingested + deduped + dropped, given no forgery).
- **Composition (E11-lite):** a fake `IHarnessEventSource` feeds Register + a span stream through the host to the **real store**, asserting liveness Alive and the span persisted — the ingest path proven end-to-end through the real composition.

## 11. Confidence ledger and residual risk

| Claim | Evidence | Label |
|---|---|---|
| Bounded queue absorbs a flood without OOM | `Channel.CreateBounded` + DropOldest (repo idiom); flood test | Verified |
| Forged/malformed events cannot kill the drain | drain catches, counts, drops; negative tests | Verified |
| Counters answer the operator questions | `IngestStats` asserted in tests | Verified |
| OTLP *network* receiver authenticates and binds session→capability | not built | **Flagged — slice 1b; encoding/transport + token-binding spike** |

**Residual risk:** the OTLP/HTTP network receiver (its encoding, its `session.id`→capability token binding, its unauthenticated-port DoS) is slice 1b and is not built here; the host is proven against a fake source.

## 12. Gate record

`GATE design · 2026-08-30 · reviewers (Adversary Mode): Patterns Expert ⇄ Simplifier, Test Architect, Security & Identity, Distributed Systems, SRE · exit criteria: single responsibility; sync-control/async-stream split justified; bounded-queue backpressure (DropOldest) as the DoS control; forged/malformed dispositioned with counters; transport behind a port; every failure mode a negative test; counters answer the operator questions (IO1) · verdict: PASS-WITH-CONDITIONS · vetoes: Distributed Systems (backpressure) and Security (forgery via capability) satisfied; condition — the OTLP network receiver's authentication/DoS is slice 1b, recorded not accepted`

**Handoff:** → `/implement` the `IngestHost` + bounded queue (this design's core), then spike + build the OTLP receiver adapter (slice 1b).
