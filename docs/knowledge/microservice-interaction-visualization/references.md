---
id: kb-micro-references
title: "Microservice Interaction Visualization — references, versions and constants"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [reference, opentelemetry, semconv, otlp, w3c-trace-context]
links:
  - { to: kb-microservice-interaction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The specifications and the exact constants: semconv v1.44.0 stability by domain, OTLP ports
  and paths, traceparent format, SpanKind values, and the Tempo peer-attribute fallback order.
---

# Reference information

## Standards and specifications

- **OpenTelemetry Tracing API / SDK** — Stable. The span data model, `SpanKind`, `Status`, Links, Events,
  `InstrumentationScope`. *(Verified, [S1][S2])*
- **OTLP v1.11.0** — Stable for traces, metrics and logs; Development for profiles. *(Verified, [S3])*
- **OpenTelemetry Semantic Conventions v1.44.0** — mixed stability; see the table below. *(Verified, [S4])*
- **W3C Trace Context** — W3C Recommendation (originally published 2020-02-06, with editorial updates under
  the 2021 Process). Defines `traceparent` and `tracestate`. *(Verified, [S13])*
- **Dapper: A Large-Scale Distributed Systems Tracing Infrastructure** — Sigelman et al., Google, 2010. The
  founding paper for the whole model, including its sampling argument. *(Reference; not fetched here)*
- **Software Reflexion Models: Bridging the Gap between Design and Implementation** — Murphy, Notkin &
  Sullivan, *IEEE TSE* 27(4), 2001. Source of convergence/divergence/absence. *(Flagged — not fetched)*

## Versions, stability and constants

| Item | Value | Stability | Source |
|---|---|---|---|
| Semantic Conventions version | **1.44.0** | mixed | [S4] |
| Tracing API / SDK | — | **Stable** | [S1][S2] |
| OTLP version | **1.11.0** | Stable (trace/metric/log); Development (profiles) | [S3] |
| W3C Trace Context | W3C Recommendation | — | [S13] |
| OTLP/gRPC default port | **4317** | — | [S3][S19] |
| OTLP/HTTP default port | **4318** | — | [S3][S19] |
| OTLP/HTTP paths | `/v1/traces`, `/v1/metrics`, `/v1/logs` | — | [S3] |
| OTLP recommended max message size | 64 MiB | — | [S3] |
| `traceparent` format | `{version 2hex}-{trace-id 32hex}-{parent-id 16hex}-{flags 2hex}` | — | [S13] |
| TraceId / SpanId lengths | 16 bytes (32 hex) / 8 bytes (16 hex) | Stable | [S1][S13] |
| `tracestate` limit | max 32 list members | — | [S13] |
| SpanKind values | `SERVER`, `CLIENT`, `PRODUCER`, `CONSUMER`, `INTERNAL` | Stable | [S1] |
| SpanStatus values | `Unset` (default), `Ok`, `Error` | Stable | [S1] |
| Resource `service.*` | `service.name` (required), `.namespace`, `.instance.id`, `.version` | **Stable** | [S6] |
| `service.name` SDK fallback | `unknown_service:{process.executable.name}` | — | [S6] |
| `service.name` env var | `OTEL_SERVICE_NAME` | — | [S6] |
| HTTP spans | `http.request.method`, `http.response.status_code`, `http.route`, `server.address`, `url.scheme` | **Stable** (opt-in `OTEL_SEMCONV_STABILITY_OPT_IN=http`) | [S8] |
| Database spans | `db.system.name`, `db.operation.name`, `db.namespace`, `db.collection.name`, `db.query.text` | **Stable** (opt-in migration) | [S10] |
| **Messaging spans** | `messaging.system`, `messaging.operation.name`, `messaging.destination.name`, `messaging.message.id` | **Development — NOT stable** | [S12] |
| Exception attributes | `exception.type`, `.message`, `.stacktrace` | Stable | [S4] |
| Probability sampling `TraceState` | `ot=th:{threshold};rv:{randomness}` | **Development** | [S15] |
| .NET `Activity` / `ActivitySource` | = OTel Span / Tracer | — | [S16][S18] |
| .NET default trace-ID format (.NET 5+) | W3C Trace Context | — | [S16] |
| OTel .NET SDK signals | Traces, Metrics, Logs — all Stable | Stable | [S17] |
| Aspire Dashboard image | `mcr.microsoft.com/dotnet/aspire-dashboard:latest` | — | [S19] |
| Aspire Dashboard ports | UI **18888**, OTLP **4317** / **4318** | — | [S19] |
| Aspire Dashboard storage | **in-memory only**; dropped past limits; no persistence across restart | — | [S19] |
| Tempo service-graph metric | `traces_service_graph_request_total{client, server, connection_type}` | — | [S21] |
| Tempo peer-attribute fallback order | `peer.service` → `server.address` → `network.peer.address:port` → `db.namespace` → `db.name` | — | [S21] |

## The messaging span sequence (Development status — quoted structure)

For pub/sub, the spec defines a span per stage rather than a nested call tree:

| Span | Kind | When |
|---|---|---|
| **Create** | PRODUCER | Message created / handed to the client library; one per message in a batch |
| **Send** | PRODUCER (if its context is the creation context) else CLIENT | Messages sent to the broker |
| **Receive** | CLIENT | Consumer pulls messages |
| **Process** | CONSUMER | Message is processed |
| **Settle** | CLIENT | Processing acknowledged |

The **creation context** from the Create or Send span is injected into the message payload and extracted by
the consumer; the Process span takes it as parent **or links to it**. That link — not the parent-child tree —
is how an async edge is recovered. *(Verified, [S12])*

## Migration hazard

Both HTTP and database conventions emit their **stable** attribute names only under
`OTEL_SEMCONV_STABILITY_OPT_IN`. Deployed SDKs commonly still emit the older names, so a parser must accept
both — `http.url` and `url.full`, `db.system` and `db.system.name`. *(Verified, [S8][S10])*
