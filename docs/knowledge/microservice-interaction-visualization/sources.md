---
id: kb-micro-sources
title: "Microservice Interaction Visualization — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-microservice-interaction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  The full access-dated source list behind the microservice-interaction knowledge base, keyed
  [S1]..[S25] as cited throughout the topic.
---

# Sources

All accessed **2026-08-23**. Citation keys `[Sn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| S1 | OTel Tracing API specification | standard (spec) | https://opentelemetry.io/docs/specs/otel/trace/api/ | Span data model, SpanKind semantics, Status, Links, Events, cardinality guidance |
| S2 | OTel Tracing SDK specification | standard | https://opentelemetry.io/docs/specs/otel/trace/sdk/ | Sampling, SpanProcessor, TracerProvider |
| S3 | OTLP specification v1.11.0 | standard | https://opentelemetry.io/docs/specs/otlp/ | Protocol version, ports 4317/4318, HTTP paths, 64 MiB limit |
| S4 | OTel Semantic Conventions v1.44.0 (index) | standard | https://opentelemetry.io/docs/specs/semconv/ | Version, domain stability overview |
| S5 | Semconv — Resource | standard | https://opentelemetry.io/docs/specs/semconv/resource/ | Resource stability overview |
| S6 | Semconv — Resource/Service (Stable) | standard | https://opentelemetry.io/docs/specs/semconv/resource/service/ | `service.name`/`.namespace`/`.instance.id`, fallback, `OTEL_SERVICE_NAME` |
| S7 | Semconv — HTTP (index) | standard | https://opentelemetry.io/docs/specs/semconv/http/ | HTTP convention status and migration approach |
| S8 | Semconv — HTTP spans (Stable) | standard | https://opentelemetry.io/docs/specs/semconv/http/http-spans/ | HTTP attributes, stability, opt-in migration |
| S9 | Semconv — Database (index) | standard | https://opentelemetry.io/docs/specs/semconv/db/ | Database convention status |
| S10 | Semconv — Database spans (Stable) | standard | https://opentelemetry.io/docs/specs/semconv/db/database-spans/ | DB attributes, CLIENT-span-only nature, opt-in migration |
| S11 | Semconv — Messaging (index) | standard | https://opentelemetry.io/docs/specs/semconv/messaging/ | Messaging convention status (Development) |
| S12 | Semconv — Messaging spans (Development) | standard | https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/ | Create/Send/Receive/Process/Settle spans, creation context, span links |
| S13 | W3C Trace Context | standard (W3C Rec.) | https://www.w3.org/TR/trace-context/ | `traceparent`/`tracestate` format and limits |
| S14 | OTel sampling concepts | primary (docs) | https://opentelemetry.io/docs/concepts/sampling/ | Head vs tail sampling, stateful requirement, fallback warning, cost guidance |
| S15 | OTel probability sampling (Development) | standard | https://opentelemetry.io/docs/specs/otel/trace/tracestate-probability-sampling/ | `ot=th:…;rv:…`, Development status |
| S16 | .NET distributed tracing concepts | primary (vendor docs) | https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-concepts | `Activity` = Span, W3C default from .NET 5 |
| S17 | OTel .NET language docs | primary | https://opentelemetry.io/docs/languages/dotnet/ | .NET SDK signal stability |
| S18 | OTel .NET instrumentation | primary | https://opentelemetry.io/docs/languages/dotnet/instrumentation/ | `ActivitySource` = Tracer |
| S19 | .NET Aspire Dashboard — standalone mode | primary (vendor docs) | https://aspire.dev/dashboard/standalone/ | Standalone usage, image, ports 18888/4317/4318, in-memory limits |
| S20 | Jaeger features (v2.20) | primary (docs) | https://www.jaegertracing.io/docs/2.20/features/ | System Architecture graph non-transitivity, Deep Dependency Graph, OTLP support |
| S21 | Grafana Tempo — service graphs | primary (docs) | https://grafana.com/docs/tempo/latest/metrics-from-traces/service_graphs/ | Pair matching, TTL store, Prometheus metric name, virtual nodes, peer fallback order |
| S22 | Grafana Tempo — metrics-generator | primary (docs) | https://grafana.com/docs/tempo/latest/metrics-generator/ | Processor architecture |
| S23 | AppMap — using AppMap diagrams | primary (docs) | https://appmap.io/docs/reference/guides/using-appmap-diagrams.html | Sequence diagram view, depth collapse, SVG export, sequence diff |
| S24 | AppMap — overview | primary (docs) | https://appmap.io/docs/appmap-docs.html | Recording model, IDE extensions |
| S25 | Kiali concepts | primary (docs) | https://kiali.io/docs/architecture/terminology/concepts/ | Envoy/Prometheus-derived mesh topology |

## Additional references cited but not fetched

| Citation | Status |
|---|---|
| Sigelman et al., *Dapper: A Large-Scale Distributed Systems Tracing Infrastructure*, Google 2010 | Referenced for provenance of the tracing model — **not fetched** |
| Murphy, Notkin & Sullivan, *Software Reflexion Models*, IEEE TSE 27(4), 2001 | Source of convergence/divergence/absence — **Flagged, not fetched** |
| Zipkin, Datadog, Dynatrace, New Relic, AWS X-Ray, Azure Application Insights, Grafana Beyla | Characterised from general knowledge — **Flagged, not fetched** |
| Cilium/Hubble introduction (docs.cilium.io) | eBPF acquisition model — Verified in the research pass, listed here for completeness |

## Source-quality notes

- S1–S15 are the OpenTelemetry and W3C specifications themselves — the top of the source hierarchy. Every
  attribute name, port, format and **stability level** in this topic comes from them and is labelled Verified.
- S16–S25 are vendor or project documentation for the specific tools, also primary.
- The reflexion-model vocabulary, which this topic recommends adopting wholesale, rests on an **unfetched**
  citation and is therefore labelled Inferred throughout. Confirm the paper before quoting it in a design.
