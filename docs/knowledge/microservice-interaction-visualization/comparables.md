---
id: kb-micro-comparables
title: "Microservice Interaction Visualization — comparable tools"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [comparables, jaeger, tempo, kiali, hubble, appmap]
links:
  - { to: kb-microservice-interaction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Service-graph and trace-diagram tools compared by acquisition method, what each requires,
  what it can see and what it is structurally blind to — the table that shows why no single
  tool answers the architecture question.
---

# Comparable solutions & problem framings

Grouped by **how the topology is acquired**, because that determines what the tool is blind to.

## Trace-derived (instrumentation required)

| Tool | How it builds the graph | Requires | Sees | Blind to | Confidence |
|---|---|---|---|---|---|
| **Jaeger — System Architecture** | Aggregates one-hop CLIENT→SERVER pairs; in-memory or Spark/Flink jobs | OTel/OpenTracing instrumentation | Synchronous HTTP/RPC between instrumented services | Uninstrumented services; async unless PRODUCER/CONSUMER used; sampled-out traces. **Explicitly non-transitive** | Verified [S20] |
| **Jaeger — Deep Dependency Graph** | Transitive closure of call chains from search results, around a focal service | Same | Transitive dependencies through the focal service; optional endpoint granularity | Anything not connected to the focal service; uninstrumented hops | Verified [S20] |
| **Grafana Tempo — service graphs** | Matches CLIENT/SERVER and PRODUCER/CONSUMER pairs in a TTL store; emits Prometheus metrics | OTel + Tempo + Prometheus/Mimir | HTTP, RPC and messaging edges; DB virtual nodes from `db.*`; virtual nodes for uninstrumented peers | Spans not paired within the TTL; in-process calls | Verified [S21][S22] |
| **Zipkin** | CLIENT/SERVER pairs sharing a TraceId | App-level instrumentation | Synchronous HTTP/RPC | Uninstrumented components; async without propagation | Inferred |
| **Datadog APM Service Map** | Aggregates agent trace data by `service` tag; infers services from outgoing calls | Agent + instrumentation | HTTP, RPC, DB, messaging; inferred services | Uninstrumented services | Flagged (not fetched) |
| **Dynatrace Smartscape / Service Flow** | OneAgent process-level capture + bytecode injection, plus OTel | OneAgent or OTel | Network calls and in-process calls; container topology | Hosts without OneAgent or OTel | Flagged (not fetched) |
| **New Relic** | Aggregates spans by `service.name`; Infinite Tracing for tail sampling | Agent or OTel | HTTP, DB, messaging | Uninstrumented services; async without propagation | Flagged (not fetched) |
| **AWS X-Ray Service Map** | X-Ray segments/subsegments | X-Ray SDK or ADOT | HTTP, AWS SDK calls, DB | Non-AWS services without X-Ray | Flagged (not fetched) |
| **Azure App Insights Application Map** | Correlates dependency telemetry by `operation_Id`, nodes keyed by `cloud_RoleName` | App Insights SDK or Azure Monitor OTel exporter | HTTP, SQL, Service Bus, Event Hubs, external HTTP | Uninstrumented services; traffic below the sampling threshold | Flagged (not fetched) |

## Infrastructure-derived (no app instrumentation)

| Tool | How it builds the graph | Requires | Sees | Blind to | Confidence |
|---|---|---|---|---|---|
| **Kiali (Istio)** | Envoy sidecar telemetry + Prometheus metrics — not traces | Istio mesh, sidecars everywhere | All L7 traffic through the mesh; mTLS status | Traffic bypassing Envoy; in-process calls; external services without a ServiceEntry | Verified (partial) [S25] |
| **Hubble (Cilium)** | eBPF kernel-level flow capture | Cilium CNI on Linux Kubernetes | L3/L4/L7 flows, HTTP patterns, DNS, TCP failures | In-process calls; **application intent** — retry vs health check vs business call | Verified |
| **Grafana Beyla** | eBPF auto-instrumentation emitting OTel spans | Beyla DaemonSet, supported runtimes | HTTP, gRPC, SQL without code changes | Unsupported runtimes; async broker flows; in-process calls | Flagged (not fetched) |

## Execution-recorded (the sequence-diagram class — closest to our goal)

| Tool | How it works | Requires | Sees | Blind to | Confidence |
|---|---|---|---|---|---|
| **AppMap** | Records execution as its own JSON from tests/recording sessions; renders dependency map, trace view and **sequence diagram** with depth collapse, SVG export and **run-to-run diff** | AppMap agent per language; a test or recording session | Intra-process function calls, HTTP endpoints, SQL, external calls — in one execution path | Cross-service unless the agent is in every service; per-test sampling bias; **not OTLP-based**; not for production | Verified [S23][S24] |
| **Jaeger UI trace view** | Flame/Gantt chart of the span tree | Instrumentation | Timing and nesting of one trace | Not a sequence diagram; no sequence export | Inferred [S20] |
| **`otel-cli`, tracetest, community Mermaid scripts** | Print span trees / assert on traces / ad-hoc conversion | Varies | Varies | No canonical trace→sequence-diagram tool exists in OSS | Flagged |

## Conformance / reconciliation (the declared side)

| Tool | What it does | Static or runtime | Note |
|---|---|---|---|
| **Reflexion model** (Murphy, Notkin & Sullivan) | Maps a declared model onto an extracted one; classifies convergence / divergence / absence | Either | The vocabulary to adopt. *(Inferred — paper not fetched)* |
| **ArchUnit / NetArchTest** | Enforces declared dependency rules against compiled code | Static only | Sees declared/compiled dependencies, never runtime behaviour |
| *(gap)* | Joining static and runtime graphs end-to-end | — | **No widely adopted OSS tool does this** — the space AI-DE is entering |

## Adjacent ideas worth borrowing

- **Tempo's virtual-node rule** with its explicit attribute fallback order — a solved sub-problem, free to copy.
- **AppMap's sequence-diagram diff** — comparing two runs is a better question than describing one, and it
  composes directly with declared-vs-observed reconciliation.
- **Jaeger's explicit non-transitivity warning** — a model for how our UI should state what an edge does and
  does not mean.
- **Dapper** (Sigelman et al., Google, 2010) — the origin of the whole distributed-tracing model and still
  the clearest statement of its sampling economics.
