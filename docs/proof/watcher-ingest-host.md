---
id: proof-watcher-ingest-host
title: "Proof Pack - Loomkeeper Ingest Host (slice 1a)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, proof-pack, ingest, host, phase-1]
links:
  - { to: design-watcher-ingest-host, rel: tested-by }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper ingest host meets its design: registration/heartbeat are synchronous, the
  bounded span queue absorbs a flood with drop-oldest (every drop counted), forged spans are rejected,
  malformed ones are quarantined without killing the drain, and the counters reconcile - proven by 9
  tests with the backpressure counter compile-enforced.
---

# Proof Pack: Loomkeeper Ingest Host (slice 1a)

- **Component:** `src/AiDe.Core/Watcher/IngestHost.cs`
- **Tests:** `tests/AiDe.Core.Tests/Watcher/IngestHostTests.cs` — 9 tests, **Passed 9 / 9**; full `AiDe.Core.Tests` suite **741/0**; build clean (0 warnings, `TreatWarningsAsErrors`).

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| A span flood is absorbed and every drop counted (not OOM) | `Enqueue_FloodPastCapacity_DropsOldest_AndCountsEveryDrop` | `IngestHost` bounded `Channel` + `itemDropped` | 10 into cap-4 must count 6 drops and reconcile | **Yes** — removing the drop counter fails to compile (warnings-as-errors: field never assigned) — compile-enforced oracle | Verified | Capacity 1024 default; partition per session if measured overflow |
| A forged span is rejected and not stored | `EnqueueDrain_ForgedCapability_Rejected_AndNotStored` | `IngestHost.Process` → `SpanIngest` | Rejected++ and store unchanged | Seen green (forgery oracle proven in the skeleton) | Verified | — |
| A malformed span is quarantined and the drain survives | `EnqueueDrain_MalformedSpan_IsQuarantined_AndTheLoopSurvives` | `Process` catches LK-0004 | Quarantined++ and a following good span still ingests | Seen green | Verified | — |
| A valid span is ingested and counted | `EnqueueDrain_ValidSpan_IngestedAndCounted` | host → mapper → ingest | Ingested++ and span stored | Seen green | Verified | — |
| A duplicate span is deduped | `EnqueueDrain_DuplicateSpan_IsDeduped` | idempotent ingest | Deduped++, count stays 1 | Seen green | Verified | — |
| Registration returns a verifiable capability | `Register_ReturnsAVerifiableCapability` | `Register` → registrar | Session recorded, gen 1 | Seen green | Verified | — |
| Heartbeat rejects a forged capability | `Heartbeat_BadCapability_Throws` (LK-0001) | `Heartbeat` → registrar | Must throw | Seen green | Verified | — |
| The counters reconcile under concurrency | `Enqueue_Concurrent_CountsReconcile` | thread-safe `Channel` | enqueued == ingested+deduped+rejected+dropped+quarantined | Seen green | Verified | — |
| The host composes register→heartbeat→ingest through the real store | `Host_ComposesRegisterHeartbeatIngest_ThroughRealStore` | full core + `LivenessProjection` | Alive + persisted + harness/model recovered | Seen green | Verified | — |

**Boundary set covered:** valid span, duplicate, forged, malformed, flood-past-capacity, concurrent enqueue, bad-capability heartbeat, end-to-end composition.

**Mutation sense:** the drop counter is **compile-enforced** (removing it breaks the build under warnings-as-errors); the flood test additionally asserts the exact drop count (6) and a full reconcile equation.

**Not built this slice (residual — slice 1b):** the OTLP/HTTP **network receiver** adapter (its wire encoding, its `session.id`→capability token binding, its unauthenticated-port DoS) — gated by an encoding/transport spike. The host is proven against a direct producer; the receiver will produce onto the same `Enqueue`.
