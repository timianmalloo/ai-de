---
id: kb-micro-glossary
title: "Microservice Interaction Visualization — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, tracing, reflexion-model, ubiquitous-language]
links:
  - { to: kb-microservice-interaction, rel: refines }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Precise definitions for tracing and conformance vocabulary — SpanKind, creation context,
  virtual node, convergence/divergence/absence — so the graph, the MCP tools and the UI all
  use the same words.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **Absence** | Reflexion model: an edge in the **declared** model that is **not observed** at runtime. Weak evidence — may mean removed, or untested, or sampled away. *(Inferred)* |
| **Activity** | .NET's name for an OTel **Span**, in `System.Diagnostics`. *(Verified, [S16])* |
| **ActivitySource** | .NET's name for an OTel **Tracer** — a named factory for Activities. *(Verified, [S18])* |
| **Convergence** | Reflexion model: an edge present in **both** the declared and the observed model. *(Inferred)* |
| **Creation context** | The trace context a producer injects into a message payload so a consumer's span can link back to it across a broker. The mechanism that makes async edges recoverable. *(Verified, [S12])* |
| **Divergence** | Reflexion model: an edge **observed** at runtime that is **not declared** — the unauthorised dependency. Strong evidence. *(Inferred)* |
| **Deep Dependency Graph** | Jaeger's transitive graph of services reachable from a chosen **focal** service. *(Verified, [S20])* |
| **Head sampling** | The sampling decision made at span creation, before the trace is complete. Consistent across a trace via the TraceId. *(Verified, [S14])* |
| **InstrumentationScope** | The named and versioned identity of the library that created a span. *(Verified, [S1])* |
| **OTLP** | OpenTelemetry Protocol — the wire format, over gRPC (4317) or HTTP/protobuf (4318). *(Verified, [S3])* |
| **Observed dependency** | An edge minted from telemetry, carrying its evidence: how many traces, which scenario, when. Distinct from a declared dependency in both origin and confidence. |
| **Service graph / service map** | A directed graph whose nodes are services (`service.name`) and whose edges are observed runtime dependencies. |
| **`service.name`** | The stable logical service identifier; identical across all instances of a horizontally scaled service. The natural sequence-diagram participant. *(Verified, [S6])* |
| **SpanKind** | `SERVER` \| `CLIENT` \| `PRODUCER` \| `CONSUMER` \| `INTERNAL`. The pairing of CLIENT+SERVER and PRODUCER+CONSUMER is what a service graph is built from. *(Verified, [S1])* |
| **Span link** | A reference from one span to another, in the same or a different trace. How async correlation is expressed when parent-child cannot be. *(Verified, [S1][S12])* |
| **Tail sampling** | The sampling decision made after the spans of a trace are collected, requiring a stateful intermediary. Enables "keep it if it errored". *(Verified, [S14])* |
| **`traceparent`** | The W3C Trace Context header: `{version}-{trace-id}-{parent-id}-{trace-flags}`. *(Verified, [S13])* |
| **`tracestate`** | The W3C Trace Context header carrying vendor key-values; max 32 members. *(Verified, [S13])* |
| **TraceId / SpanId** | 128-bit (16-byte) trace identifier / 64-bit (8-byte) span identifier. *(Verified, [S1][S13])* |
| **Virtual node** | A service-graph node standing in for an **uninstrumented** peer or a database, synthesised from peer attributes on the calling CLIENT span. *(Verified, [S21])* |
