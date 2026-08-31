---
id: proof-watcher-otlp-receiver
title: "Proof Pack - Loomkeeper OTLP Receiver (slice 1b)"
type: proof-pack
status: accepted
owner: "@timianmalloo"
phase: "1"
tags: [loomkeeper, watcher, proof-pack, ingest, otlp, receiver, phase-1]
links:
  - { to: design-watcher-otlp-receiver, rel: tested-by }
  - { to: design-watcher-ingest-host, rel: depends-on }
  - { to: spec-agentic-watcher-substrate, rel: tested-by }
review-by: 2027-02-26
review-suggested: []
summary: >-
  Evidence that the Loomkeeper OTLP/HTTP receiver meets its design: it accepts OTLP/JSON exports at
  /v1/traces with stdlib System.Text.Json (no protobuf dependency), resolves a per-session bearer token
  to a capability (the capability never travels the wire), parses and enqueues spans onto the ingest
  host, caps the body, and answers 200 even when it drops a bad/unauthenticated export - proven by 13
  tests including two real-loopback HTTP integration tests, with the auth oracle compile-enforced.
---

# Proof Pack: Loomkeeper OTLP Receiver (slice 1b)

- **Component:** `src/AiDe.Core/Watcher/OtlpReceiver.cs` (`OtlpJsonParser`, `SessionTokenRegistry`, `OtlpHttpReceiver`)
- **Tests:** `tests/AiDe.Core.Tests/Watcher/OtlpReceiverTests.cs` — 13 tests, **Passed 13 / 13**; full `AiDe.Core.Tests` suite **754/0**; build clean (0 warnings, `TreatWarningsAsErrors`).
- **Spike:** `spikes/watcher-otlp-receive/` (PASS) — established OTLP/JSON parses with stdlib `System.Text.Json` (hex trace/span ids; `{key,value:{stringValue}}` attrs), a loopback `HttpListener` binds without admin, and a per-session token can ride in a header.

| Claim | Evidence (test) | Source | Oracle | Red observed | Confidence | Residual |
|---|---|---|---|---|---|---|
| OTLP/JSON parses to spans, merging resource+span attributes | `Parse_ValidSpan_MergesResourceAndSpanAttributes` | `OtlpJsonParser.Parse` | resource `service.name` + span `gen_ai.request.model` + `session.id` all present | Seen green (merge asserts both scopes) | Verified | Only `stringValue` attrs read (spec keys are strings) |
| Malformed JSON yields empty, never throws | `Parse_MalformedJson_ReturnsEmpty` | `Parse` try/catch `JsonException` | `{ not json` and `""` → empty | Seen green | Verified | — |
| An export with no resourceSpans is empty | `Parse_NoResourceSpans_ReturnsEmpty` | `Parse` guards | `{}` / empty array → empty | Seen green | Verified | — |
| A span with no session.id parses but the mapper rejects it (LK-0004) | `Parse_MissingSessionId_StillParses_ButMapperRejectsLater` | `Parse` + `OtelSpanMapper.MapSpan` | `MapSpan` throws LK-0004 | Seen green | Verified | — |
| The body cap rejects an over-declared body | `ReadCapped_DeclaredLengthOverCap_ReturnsNull` | `ReadCapped` declared-length guard | declared 5000 > cap 8 → null | Seen green | Verified | — |
| The body cap rejects a stream that exceeds the cap mid-read (chunked, no length) | `ReadCapped_ActualBodyOverCap_ReturnsNull` | `ReadCapped` per-chunk guard | 100 bytes, declared -1, cap 8 → null | **Yes** — removing the per-chunk cap makes the 100-byte body return non-null (behavioral red) | Verified | 4 MB default cap |
| A valid token enqueues and ingests the span | `HandleExport_ValidToken_EnqueuesAndIngests` | `HandleExport` → `IngestHost.Enqueue` | Received++ and stored after drain | Seen green | Verified | — |
| An unknown token is unauthenticated and enqueues nothing | `HandleExport_UnknownToken_IsUnauthenticated_AndEnqueuesNothing` | token-resolve guard | Unauthenticated++, Received 0, store 0 | **Compile-enforced** — flipping the counter (CS0649) or inverting the guard (CS8604 nullable-flow) fails the build | Verified | — |
| A null/empty token is unauthenticated | `HandleExport_NullToken_IsUnauthenticated` | `string.IsNullOrEmpty(token)` | Unauthenticated++ | Seen green | Verified | — |
| A valid token with a malformed body enqueues nothing (empty export) | `HandleExport_ValidTokenButMalformedBody_EnqueuesNothing` | `Parse` → 0 spans | Received 0, store 0 | Seen green | Verified | Malformed body is a benign empty export, not counted rejected |
| A real loopback POST with a valid token ingests the span end to end | `RealLoopbackPost_ValidToken_IngestsSpanEndToEnd` (**D4**) | `OtlpHttpReceiver.RunAsync` over a real `HttpListener` + `HttpClient` | HTTP 200, Received 1, stored 1 | Seen green | Verified | Bind failure surfaces loudly (never a false pass); a CI box may need a loopback urlacl |
| A real loopback POST with an unknown token is dropped (still 200) | `RealLoopbackPost_UnknownToken_IsDroppedEndToEnd` (**D4**) | full receiver path | HTTP 200, Unauthenticated 1, stored 0 | Seen green | Verified | — |
| The receiver answers 200 even on a dropped export (no exporter retry storm) | both loopback tests assert `HttpStatusCode.OK` | `Respond200` in `finally` | 200 regardless of disposition | Seen green | Verified | — |

**Boundary set covered:** valid single span, resource+span attribute merge, malformed JSON, empty export, missing-session-id, body under cap, over-declared body, chunked-body-over-cap, valid token, unknown token, null token, valid-token-malformed-body, real-loopback valid, real-loopback unknown-token.

**Testing Strategy triggers applied:** **D1** (parser/HandleExport/ReadCapped units), **D4** (two real-loopback HTTP integration tests — the wire is exercised against a real `HttpListener`, not a mock), **A6** (the OTLP/GenAI attribute keys stay pinned in `OtelAttributes`; a change is a guarded contract change, inherited from the mapper's regression test). No triggered directive dropped.

**Mutation sense:** the auth oracle is **compile-enforced** — the per-session counters are single-writer, so flipping one to another counter fails the build (CS0649 "never assigned"), and inverting the `IsNullOrEmpty(token)` guard fails on nullable flow (CS8604). The body-cap oracle is proven **behaviorally** (removing the per-chunk cap makes an oversize chunked body return non-null → `ReadCapped_ActualBodyOverCap_ReturnsNull` red), then reverted.

**Security note (STRIDE, carried from design):** the capability is never on the wire — only an opaque per-session bearer token, resolved server-side to the capability (spoofing mitigation; a forged span still hits the ingest host's LK-0001 forgery check as defence in depth). The listener binds **loopback only**; the body is capped (DoS); a bad/unknown export is counted and dropped, never fatal (the accept loop survives one bad request). Answering 200 on a permanent drop prevents an exporter retry storm.

**Residual (slice-1 closeout, non-blocking):** the registration flow does not yet **issue** the per-session token and inject it into the harness's `OTEL_EXPORTER_OTLP_HEADERS` — slice 1b keeps `ISessionTokenResolver` as the seam (tests populate a `SessionTokenRegistry` directly). Token issuance/injection is a small follow-on (noted as residual, not blocking slice-1 completion). The harness must also be configured `OTEL_EXPORTER_OTLP_PROTOCOL=http/json` (spike finding).
