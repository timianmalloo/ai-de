---
id: kb-micro-sota
title: "Microservice Interaction Visualization — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [opentelemetry, otlp, service-graph, sampling, sequence-diagram]
links:
  - { to: kb-microservice-interaction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  How service graphs are actually derived from telemetry today, what the OpenTelemetry trace
  model guarantees, how async flows are modelled, and the modelling problems that make
  trace-to-sequence-diagram harder than it looks.
---

# State of the art — visualizing service interactions

## OpenTelemetry as the substrate

**Status.** Tracing API and SDK: **Stable**. OTLP **v1.11.0**: Stable for traces, metrics and logs;
Development for profiles. Semantic Conventions **v1.44.0**: mixed. *(Verified, [S1][S2][S3][S4])*

**Trace data model** — all Stable: a `Trace` is a DAG of `Span`s sharing a 128-bit `TraceId`; a `Span` has
name, `SpanKind`, start/end timestamps, Attributes, Events, Links, Status and `SpanContext`
(`TraceId` 16 bytes, `SpanId` 8 bytes, `TraceFlags` 1 byte including the `sampled` bit, `TraceState`);
`Status` is `Unset` (default) / `Ok` / `Error`; `InstrumentationScope` carries the named+versioned identity
of whatever created the span. *(Verified, [S1])*

**SpanKind — the semantics the whole service graph rests on**, from the spec:

| Kind | Meaning |
|---|---|
| `SERVER` | Server-side handling of a remote request while the client awaits a response |
| `CLIENT` | Request to a remote service where the client awaits a response; typically becomes parent of a remote `SERVER` span |
| `PRODUCER` | Initiation/scheduling of an operation; **often ends before the correlated `CONSUMER` span starts** |
| `CONSUMER` | Processing of an operation initiated by a PRODUCER; the producer does not await the outcome |
| `INTERNAL` | Default; internal to the application |

*(Verified, [S1])*

**Semantic-convention stability at v1.44.0** — the fact that most constrains design:

| Domain | Status | Key attributes |
|---|---|---|
| Resource `service` | **Stable** | `service.name` (required), `service.namespace`, `service.instance.id`, `service.version` |
| `telemetry.sdk` | **Stable** | `telemetry.sdk.name` / `.language` / `.version` |
| HTTP spans | **Stable** (opt-in migration `OTEL_SEMCONV_STABILITY_OPT_IN=http`) | `http.request.method`, `http.response.status_code`, `http.route`, `server.address`, `url.scheme` |
| Database spans | **Stable** (opt-in migration) | `db.system.name`, `db.operation.name`, `db.namespace`, `db.collection.name`, `db.query.text` |
| **Messaging spans** | **Development — not stable** | `messaging.system`, `messaging.operation.name`, `messaging.destination.name`, `messaging.message.id` |
| Exceptions | Stable | `exception.type`, `.message`, `.stacktrace` |

`service.name` must be identical across all instances of a horizontally scaled service; the SDK fallback is
`unknown_service:{process.executable.name}`; it is set via `OTEL_SERVICE_NAME`. *(Verified, [S5][S6][S8][S10][S11][S12])*

**W3C Trace Context** is a W3C Recommendation. `traceparent` is
`{version}-{trace-id}-{parent-id}-{trace-flags}` — version 2 hex (`00`), trace-id 32 lowercase hex with at
least one non-zero byte, parent-id 16 lowercase hex, flags 2 hex with bit 0 = sampled. Example
`00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01`. `tracestate` holds vendor key-values, max 32
list members. It is OTel's default propagator. *(Verified, [S13])*

**Sampling.** *Head* sampling decides at span creation — cheap, consistent across a trace via the TraceId,
but blind to the finished trace. *Tail* sampling decides after the spans are collected — enables
"keep it if it errored or was slow", but **requires a stateful intermediary holding the whole trace**, and
the OTel docs warn the sampler must be monitored because it can fall back under load. Consistent
probability sampling (`TraceState ot=th:…;rv:…`) is still **Development**. *(Verified, [S14][S15])*

**OTLP.** gRPC on **4317**; HTTP/protobuf on **4318** with paths `/v1/traces`, `/v1/metrics`, `/v1/logs`;
`application/x-protobuf` or `application/json`; gzip supported; 64 MiB recommended message-size limit.
*(Verified, [S3])*

**.NET.** `System.Diagnostics.Activity` = OTel Span; `ActivitySource` = Tracer; `ActivityListener` = the
observation hook. W3C Trace Context is the default ID format from .NET 5 (`Activity.DefaultIdFormat`); the
older hierarchical format remains for compatibility. OTel .NET's traces, metrics and logs are all Stable.
*(Verified, [S16][S17][S18])*

**Aspire Dashboard standalone** — `npx @microsoft/aspire-cli dashboard run` or the container
`mcr.microsoft.com/dotnet/aspire-dashboard:latest`; UI **18888**, OTLP **4317**/**4318**; accepts OTLP from
any OTel app with no Aspire orchestration; token-authenticated by default (`--allow-anonymous` for dev);
**in-memory only**, telemetry dropped past limits, nothing survives restart; the Aspire-specific resource
view is disabled in standalone mode. *(Verified, [S19])*

## How service graphs are actually derived

The shared pattern: collect spans, find correlated **pairs** representing a service-to-service call
(CLIENT+SERVER, or PRODUCER+CONSUMER), and aggregate the pairs into directed edges keyed by `service.name`.

- **Grafana Tempo** (metrics-generator, service-graphs processor) — matches CLIENT+SERVER and
  PRODUCER+CONSUMER pairs, holds the first span in an in-memory store with a configurable TTL while waiting
  for its partner, and emits Prometheus metrics `traces_service_graph_request_total{client, server,
  connection_type}` plus latency histograms. Database edges come from CLIENT spans carrying `db.*`.
  **Virtual nodes** stand in for uninstrumented peers, resolved in the documented order `peer.service` →
  `server.address` → `network.peer.address:port` → `db.namespace` → `db.name`. *(Verified, [S21][S22])*
- **Jaeger** — the **System Architecture** graph aggregates one-hop dependencies and, in its own docs, does
  **not** imply transitivity; the **Deep Dependency Graph** is transitive but focal-service-bound and can
  show endpoints as nodes (`A::op1`). *(Verified, [S20])*
- **Zipkin** — CLIENT/SERVER pairs sharing a TraceId; application instrumentation required. *(Inferred)*
- **Kiali (Istio)** — no traces at all: Envoy sidecar telemetry plus Prometheus. Requires a service mesh,
  needs no app instrumentation, sees all L7 traffic *through the mesh*, and is blind to anything bypassing
  Envoy. *(Verified in part, [S25])*
- **Hubble (Cilium)** — eBPF at the kernel: no instrumentation, no sidecar, no mesh. Sees L3/L4/L7 flows,
  HTTP patterns, TCP failures, DNS. Blind to in-process calls and to application intent. *(Verified, [S22]-adjacent source)*
- **Beyla, Datadog, Dynatrace, New Relic, X-Ray, Application Insights** — same family, different acquisition
  (eBPF, agent, bytecode injection, SDK). *(Flagged — documented from general knowledge, not fetched)*

## Trace → sequence diagram

The mapping is natural — parent-child is a synchronous call arrow, `service.name` is the lifeline, span
start/end give the activation box — and then five things make it hard:

- **Participants.** `service.name` for architecture; `service.instance.id` for concurrency debugging. Two
  different diagrams; pick per view.
- **Async gaps.** PRODUCER ends before CONSUMER begins. These must render as open-arrowhead async messages
  with a visible time gap, not as nested synchronous calls.
- **Concurrency.** A `Task.WhenAll` fan-out gives N overlapping activation boxes. The timestamps support the
  reconstruction; no reviewed tool renders deep fan-out elegantly.
- **Repetition.** A loop calling a database 1000 times yields 1000 near-identical spans. Without `loop`
  collapse the diagram is unusable.
- **Depth and width.** Jaeger handles 80,000-span traces — as a flame chart. A sequence diagram with 80,000
  arrows is not a diagram.

**AppMap** is the closest product: it records its own JSON (not OTLP) from tests or recording sessions and
renders a dependency map, a trace view and a sequence diagram with depth collapse, SVG export and a
**sequence-diagram diff** between two runs. *(Verified, [S23][S24])* Jaeger's own trace view is a flame/Gantt
chart, not a sequence diagram, and offers no sequence export. *(Inferred, [S20])*

## Declared vs observed

The canonical frame is the **reflexion model** (Murphy, Notkin & Sullivan): map a declared high-level model
onto an extracted/observed one and classify every edge as **convergence**, **divergence** or **absence**.
ArchUnit and NetArchTest enforce the declared side statically; tracing supplies the observed side; no widely
adopted OSS tool joins them. *(Inferred — [S24] not fetched directly)*

## The frontier

- **Messaging conventions reaching Stable** is the event that unblocks reliable async edge-minting.
- **Joining static and runtime graphs** is genuinely unoccupied ground — the tools each do one side.
- **eBPF acquisition** (Hubble, Beyla) removes the instrumentation prerequisite but strips semantic intent:
  it sees that Pod A called Pod B, not whether that was a retry, a health check or a business transaction.
