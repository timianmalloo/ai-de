---
id: design-watcher-otlp-receiver
title: "Loomkeeper OTLP Receiver - Transport Adapter"
type: design
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, design, otlp, receiver, transport, phase-1]
links:
  - { to: architecture-loomkeeper, rel: implements }
  - { to: design-watcher-ingest-host, rel: refines }
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: adr-0018-credential-backed-grading-egress, rel: depends-on }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Design for the Loomkeeper OTLP receiver (slice 1b): a loopback HttpListener that accepts OTLP/JSON
  trace exports at /v1/traces, resolves a per-session bearer token to the session's capability, parses
  spans with stdlib System.Text.Json (no protobuf dependency), and enqueues them into the ingest host.
  Split into a pure OtlpJsonParser and thin OtlpHttpReceiver glue. Contract established by the slice-1b spike.
---

# Design: Loomkeeper OTLP Receiver

- **Status:** Draft · **Tier:** T2 · **Phase:** 1, slice 1b · **Refines:** [`design-watcher-ingest-host`](watcher-ingest-host.md) (produces onto its `Enqueue`).
- **Established contract:** the slice-1b spike (`spikes/watcher-otlp-receive/FINDINGS.md`, PASS) — OTLP/JSON parses with `System.Text.Json`, `HttpListener` binds loopback, the per-session token rides in a header.

## 1. Responsibility and boundary

One responsibility: **receive harness OTLP telemetry and hand it to the ingest host under a verified session**. It owns the **loopback HTTP endpoint**, the **OTLP/JSON parse**, and the **token→capability resolution**; it borrows the host. **This is a network trust boundary** — the first inbound network surface in the watcher.

**Decision (ladder):** accept **OTLP/HTTP with JSON** (`OTEL_EXPORTER_OTLP_PROTOCOL=http/json`), parsed with **stdlib `System.Text.Json`** — no `Google.Protobuf`/`OpenTelemetry.Proto` (rung 3 stdlib beats rung 5 dependency; spike-proven).

**Split:** `OtlpJsonParser` (pure, fully unit-testable — the OTLP/JSON shape) + `OtlpHttpReceiver` (thin `HttpListener` glue, one real loopback integration test).

## 2. Data model

No new persisted shape. Stateless transport; it holds an in-memory **token → `SessionCapability`** map populated by the registration flow. Produces existing `HarnessSpan`/`HarnessSpanEvent`. **Change-surface:** OTLP POST → parser → token resolve → `IngestHost.Enqueue` → (existing host path).

## 3. Contracts

```csharp
namespace AiDe.Core.Watcher;

// Pure: OTLP/JSON export -> spans. Resource + span attributes merged per span. Stdlib only.
public static class OtlpJsonParser
{
    public static IReadOnlyList<HarnessSpan> Parse(string otlpJson);   // malformed JSON => empty (never throws out)
}

// The session->capability resolver the registration flow populates.
public interface ISessionTokenResolver
{
    SessionCapability? Resolve(string token);   // null => unknown/unauthenticated
}

public sealed class OtlpHttpReceiver : IDisposable
{
    public OtlpHttpReceiver(IngestHost host, ISessionTokenResolver tokens, string loopbackPrefix, int maxBodyBytes = 4 * 1024 * 1024);
    public Task RunAsync(CancellationToken ct);   // accept loop; one export per POST
    public OtlpReceiverStats Stats { get; }       // Received, Unauthenticated, Rejected(oversize/parse)
}
```

**Consumed:** `System.Net.HttpListener`, `System.Text.Json` (stdlib); `IngestHost` (slice 1a); `OtelSpanMapper` (via the host).

## 4. Patterns (named + justified)

- **Adapter** — the receiver is one `HarnessEvent` producer behind the host's port; the host is unchanged.
- **Bearer-token capability binding** — the session token (opaque, injected into `OTEL_EXPORTER_OTLP_HEADERS`) resolves to the capability; **the capability never travels**, only the token. Justified over trusting `session.id` (forgeable, S1).
- **Bounded body + loopback-only** — the smallest correct DoS controls at the network edge; the host's bounded queue absorbs the span rate.
- `simplify:` — a single-threaded accept loop, one export per request (ceiling: fine for local single-operator load; upgrade trigger: measured concurrent-harness load → parallel handling).

## 5. Error and concurrency model

- The accept loop **never throws out**: a malformed body, an oversize body, or an unresolved token increments a counter and returns 200 with an empty OTLP response (an exporter must not be told to retry a permanent error). One bad export cannot stop the receiver.
- `CancellationToken` stops the loop and disposes the listener.

## 6. Failure-mode analysis (mode → disposition)

| Category | Mode | Disposition |
|---|---|---|
| Input | Malformed/empty JSON | **Prevent** — parser returns empty; `Rejected++`; 200. Test |
| Input | Oversize body | **Mitigate** — `maxBodyBytes` cap; `Rejected++`; the body is not read past the cap. Test |
| Identity | Missing/unknown token | **Detect** — `Unauthenticated++`; spans dropped, never enqueued. Negative test |
| Identity | Valid token, forged span session | **Mitigate** — enqueued under the resolved capability; the host's `SpanIngest` rejects a mismatch (existing) |
| Resource | Export flood | **Mitigate** — loopback-only + the host's bounded queue absorbs; body cap bounds a single export |
| Dependency | Harness sends protobuf not JSON | **Detect** — non-JSON content-type/parse fails → `Rejected++`; we require `http/json` (config) |

## 7. Adversarial analysis (STRIDE-lite) — boundary: the OTLP HTTP endpoint

| Threat | Disposition |
|---|---|
| **Spoofing** — a process posts spans for a session it doesn't own | **Mitigate** — mandatory token→capability resolve; unknown token → dropped; the capability gates the actual write |
| **Tampering** — altered export | **Mitigate** — content-addressed span id downstream; metadata only |
| **Repudiation** — silent drops | **Detect** — `Received`/`Unauthenticated`/`Rejected` counters |
| **Information disclosure** — the endpoint leaks | **Mitigate** — loopback-only bind; no work content in Phase 1; the token is a secret, never logged |
| **DoS** — flood / huge body | **Mitigate** — loopback-only, `maxBodyBytes` cap, host bounded queue. Residual: an unbounded number of tiny requests on loopback (accepted for local v1; a local attacker already has bigger levers) |
| **Elevation** — token reuse across sessions | **Mitigate** — one token per session generation; a superseded generation's token resolves to the new capability or not at all |

## 8. Privacy analysis (LINDDUN-lite)

**No personal data.** Phase-1 spans carry operation metadata; the token is a secret (never logged). Explicit negative.

## 9. Telemetry

`OtlpReceiverStats` counters (`Received`, `Unauthenticated`, `Rejected`) answer the operator questions (IO1). The daemon wires OTel meters; the receiver exposes the snapshot. Error path uses no new codes (drops are counted, not thrown).

## 10. Test plan (Testing Strategy — T1 parser; D4 for the HTTP leg)

- **D0:** deterministic parser tests; the integration test uses a fixed loopback port and awaits the response (no sleeps).
- **D1 parser unit:** valid single span; multiple spans; resource+span attribute merge; missing session.id (parsed, `MapSpan` rejects later); **malformed JSON → empty**; missing fields → empty strings.
- **D4 integration (real HttpListener, red-first):** a real loopback POST of OTLP/JSON with a valid token → the host ingested the span (through the real store); an **unknown token → dropped** (`Unauthenticated++`, nothing stored); an **oversize body → rejected**.
- **A6 note:** the OTLP/JSON attribute keys reuse the pinned `OtelAttributes` snapshot (already gated).

## 11. Confidence ledger and residual risk

| Claim | Evidence | Label |
|---|---|---|
| OTLP/JSON parses with stdlib; token in header | spike PASS | Verified |
| Unknown token cannot inject spans | resolver + negative test | Verified (on impl) |
| A live Claude Code exporter emits this JSON at http/json | not run live | **Flagged — verify at live integration** |
| Loopback bind needs no urlacl in CI | machine-dependent | **Flagged — CI may need a urlacl; integration test guards for bind failure** |

**Residual risk:** live-harness verification is deferred to integration; the CI bind may need a urlacl (the integration test degrades to a clear skip/flag, not a false pass).

## 12. Gate record

`GATE design · 2026-08-30 · reviewers: Patterns Expert ⇄ Simplifier, Test Architect, Security & Identity, SRE · exit criteria: single responsibility; stdlib encoding decision (spike-proven); token→capability binding; network-boundary STRIDE with loopback + body cap + mandatory token; pure parser split from HTTP glue; every failure mode a test · verdict: PASS-WITH-CONDITIONS · vetoes: Security (token binding, loopback) satisfied; conditions — live-harness verification and CI urlacl are flagged, not silently accepted`

**Handoff:** → `/implement` `OtlpJsonParser` + `OtlpHttpReceiver`; completes slice 1.
