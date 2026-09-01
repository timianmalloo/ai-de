# Spike (slice 1b) — OTLP/HTTP receive contract (FINDINGS)

**Status:** complete · **Date:** 2026-08-30 · **Result:** PASS (`dotnet run` exit 0) · **Verdict:** the receiver can be **dependency-free** on the stdlib; build it on `HttpListener` + `System.Text.Json`.

## Question

How does the ingest wire receive real harness OTLP telemetry, and does it need a protobuf dependency (`Google.Protobuf` / `OpenTelemetry.Proto`)? How is an incoming span bound to a verified session so it cannot be forged?

## Method

A real `System.Net.HttpListener` on loopback accepts a POST to `/v1/traces`; the harness side POSTs a representative **OTLP/JSON** trace export with a per-session bearer token header; the receiver parses it with `System.Text.Json` into a `HarnessSpan` and reads the token. Ran end-to-end in one process.

## Findings

1. **OTLP/JSON + `System.Text.Json` is sufficient — no protobuf dependency (Verified).** The load-bearing fields (`traceId`, `spanId`, span `name`, and the `{key, value:{stringValue}}` attributes for `session.id`, `gen_ai.request.model`, `service.name`) parse cleanly from OTLP/JSON with the stdlib. Requiring the harness to export **`OTEL_EXPORTER_OTLP_PROTOCOL=http/json`** (which we already inject at session start alongside the endpoint) keeps the receiver on the Solution-Selection Ladder's stdlib rung, avoiding `Google.Protobuf` + `OpenTelemetry.Proto`.

2. **`HttpListener` binds loopback without admin (Verified on this machine).** `http://127.0.0.1:<free-port>/` started and served without a urlacl. The receiver binds **loopback only** (local-only v1).

3. **OTLP/JSON encodes `trace_id`/`span_id` as hex strings** (not base64) — they map straight to `HarnessSpan.TraceId`/`SourceSpanId`. Attributes are an array of `{key, value:{stringValue}}`; resource attributes (`service.name`) and span attributes (`session.id`, `gen_ai.request.model`) are merged per span for the mapper.

4. **The session→capability binding is a bearer token in a header (design).** A span's `session.id` is a claim, not authority (spike S1). The watcher issues a **per-session token** at registration, injects it into the harness's `OTEL_EXPORTER_OTLP_HEADERS`, and the exporter echoes it on every export. The receiver resolves **token → the session's `SessionCapability`** (an in-memory map the registration flow populates) and enqueues the span under that capability, so a local process without the token cannot inject spans into a session. **The capability itself never travels** — only the opaque token does.

5. **The receiver is a new network trust boundary.** An unauthenticated or flooded port is the risk (KB open-question 16). Mitigations: loopback-only bind, mandatory token resolution (unresolved → dropped + counted, never enqueued), and the ingest host's **bounded queue** as the flood absorber. A body-size cap on the POST bounds a single malicious export.

## Consequences for the design

- Receiver = **`OtlpJsonParser`** (pure, stdlib, fully unit-testable — the OTLP/JSON shape) + **`OtlpHttpReceiver`** (thin `HttpListener` glue: token resolve → `IngestHost.Enqueue`, proven by one real loopback integration test).
- **No new dependency.** Config injected at session start: `OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:<port>`, `OTEL_EXPORTER_OTLP_PROTOCOL=http/json`, `OTEL_EXPORTER_OTLP_HEADERS=x-loomkeeper-session-token=<token>`.

## Confidence

| Claim | Evidence | Label |
|---|---|---|
| OTLP/JSON parses with the stdlib, no protobuf | Ran the spike; all fields extracted | Verified |
| HttpListener serves loopback without admin | Spike bound and served | Verified (this machine; CI may need a urlacl — flagged) |
| Token-in-header binds session→capability safely | Design; token opaque, capability never on the wire | Inferred — the resolver map is built in slice 1b |
| A real Claude Code exporter emits this exact JSON when set to http/json | not run against a live harness | Inferred — OTLP/JSON is spec'd; verify on live integration |
