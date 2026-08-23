---
id: kb-microservice-interaction
title: "Microservice Interaction Visualization — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [opentelemetry, tracing, service-graph, sequence-diagram, observability]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: seed-ai-native-ide-sketch, rel: relates-to }
review-by: 2026-11-21
review-suggested: []
summary: >-
  Evidence base for turning runtime traces into service graphs and sequence diagrams: which
  OpenTelemetry semantic conventions are actually stable, why pub/sub breaks parent-child
  tracing, and the reflexion-model vocabulary for declared-versus-observed dependencies.
---

# Microservice Interaction Visualization — domain knowledge

**Domain & problem:** AI-DE runs a local OTLP collector, turns span trees into Mermaid `sequenceDiagram`
views per named scenario, and mints runtime-observed `CALLS` edges in the code knowledge graph — so the
dependency graph can show **declared** dependencies (from static analysis) beside **observed** ones (from
telemetry) and highlight the difference.

**Canonical framing:** The field calls the runtime half a **service graph** or **service map**, derived by
pairing CLIENT/SERVER and PRODUCER/CONSUMER spans; and it calls the comparison half **architecture
conformance checking**, whose canonical vocabulary is the **reflexion model** (Murphy, Notkin & Sullivan):
*convergence*, *divergence*, *absence*. Our framing matches the canon on both halves. The unusual part is
joining them in one graph — no widely adopted OSS tool integrates static and runtime sides end to end.

**Compiled:** 2026-08-23 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` is folded into `references.md` §"Versions, stability and constants" — this
domain's constants are spec versions, attribute names and stability levels, which belong beside the spec.)*

## Headline findings

1. **OpenTelemetry semantic conventions are not uniformly stable, and the unstable one is the one we need
   most.** At semconv **v1.44.0**: `service.*` and `telemetry.sdk.*` are **Stable**; **HTTP spans Stable**;
   **database spans Stable**; **messaging spans are Development**. Any edge-minting logic that parses
   `messaging.*` is building on a moving contract. — *(Verified, [S4][S6][S8][S10][S12])*
2. **Pub/sub structurally breaks parent-child tracing.** The producer span ends before the consumer span
   starts, in another process. OTel's answer is a **message creation context** injected into the payload, so
   the consumer's Process span *links* to the producer rather than nesting under it. A graph builder that
   walks only parent-child edges silently misses every async flow. — *(Verified, [S12])*
3. **The reflexion model already gives us the vocabulary**, and it is worth adopting verbatim:
   **convergence** (declared and observed), **divergence** (observed, not declared — the unauthorised
   dependency), **absence** (declared, not observed). — *(Inferred — the paper was not fetched directly; the
   vocabulary is well established, [S24])*
4. **Absence is weak evidence.** A declared edge that no trace records may mean the dependency is gone — or
   that the test suite never covered that path, or that head sampling dropped the only trace. Divergence is
   strong evidence; absence is a prompt to investigate. This asymmetry should be surfaced in the UI, not
   flattened. — *(Inferred)*
5. **In .NET there is no impedance mismatch to solve: `Activity` *is* the OTel Span and `ActivitySource`
   *is* the Tracer.** .NET adopted the model before OTel standardised the names, and W3C Trace Context is
   the default ID format from .NET 5. The OTel .NET SDK wraps these natively and its traces, metrics and
   logs signals are all Stable. — *(Verified, [S16][S17][S18])*
6. **The .NET Aspire Dashboard runs standalone and accepts OTLP from any OTel app** — container
   `mcr.microsoft.com/dotnet/aspire-dashboard:latest`, UI on 18888, OTLP on 4317/4318 — but it is
   **in-memory only**, drops telemetry when limits are hit, and persists nothing across restarts. It is
   explicitly a development tool. — *(Verified, [S19])*
7. **Database calls produce a CLIENT span with no matching SERVER span**, so every service-graph tool
   invents a *virtual node* from `db.*` / `peer.service` attributes. Tempo's documented fallback order is
   `peer.service` → `server.address` → `network.peer.address:port` → `db.namespace` → `db.name`. Copy it
   rather than inventing one. — *(Verified, [S10][S21])*
8. **Jaeger's System Architecture graph is one-hop and explicitly non-transitive** — `A–B–C` does *not*
   imply a trace `A→B→C` — while its Deep Dependency Graph is transitive but requires a focal service.
   Neither is a whole-system architecture picture, and the first is commonly misread as one. — *(Verified, [S20])*
9. **AppMap is the closest existing product to the trace→sequence-diagram goal**: it records execution as
   its own JSON (not OTLP), and renders a dependency map, a trace view and a **sequence diagram** with
   depth-collapse, SVG export and a **sequence-diagram diff** between two runs. The diff is the idea worth
   stealing. — *(Verified, [S23][S24])*
10. **Semantic-convention migration is a real parsing burden.** Both HTTP and database conventions reach
    their stable names only under `OTEL_SEMCONV_STABILITY_OPT_IN`; many SDK versions still emit the old
    names. Any attribute parser must accept both `http.url`/`url.full` and `db.system`/`db.system.name`. — *(Verified, [S8][S10])*

## Confidence summary

Verified: all spec versions and stability levels, `traceparent` format, OTLP ports and paths, SpanKind
semantics, the messaging creation-context design, Aspire Dashboard's standalone limits, Tempo's service-graph
algorithm and peer-attribute fallback order, Jaeger's non-transitivity statement, AppMap's diagram features.
Inferred: the reflexion-model classification (not fetched), the absence-is-weak-evidence asymmetry, the
single-trace epistemic limit. Flagged: Datadog, Dynatrace, New Relic, X-Ray, Application Insights and
Grafana Beyla behaviour (documented from general knowledge, not fetched); whether the Aspire Dashboard
supports any sampler configuration.

**Load-bearing Flagged claims:** the messaging conventions' **Development** status is Verified and is the
one that most constrains design — build the async path behind an adapter, because those attribute names can
change. Nothing else Flagged gates a decision.

## Design implications

- **Mint `CALLS` edges from span *pairs and links*, not from the parent-child tree.** CLIENT+SERVER for
  synchronous, PRODUCER+CONSUMER plus creation-context links for async. A tree walk is the obvious
  implementation and it is wrong.
- **Label every runtime edge as observed, with the evidence attached** — how many traces, which scenario,
  what date. The declared/observed distinction is only useful if the provenance survives to the UI.
- **Adopt convergence / divergence / absence as the literal vocabulary** in the graph, the MCP tool output
  and the UI. It is the field's term set and it is precise.
- **Default local capture to 100% sampling.** For a single developer run, head sampling at anything less
  risks dropping the only trace of the rare-but-architecturally-important path (the error handler that calls
  the reporting service). Say so in the docs, along with the cost.
- **Build a virtual-node rule for databases and uninstrumented peers**, using Tempo's fallback order.
- **Sequence rendering needs three affordances or it is unreadable**: loop collapse for repeated spans,
  depth collapse for deep trees, and async arrows with a visible time gap for PRODUCER→CONSUMER. AppMap
  validates the first two; the third comes from the messaging spec.
- **Participants are `service.name`, not `service.instance.id`** — for architecture. Keep the instance ID
  available for the concurrency-debugging view, which is a different view.
- **Isolate the messaging attribute parser** behind an adapter with its semconv version recorded, so a
  Development-status convention change is a one-file fix.
- **Steal the sequence-diagram diff.** "What changed between these two runs" is a better question than
  "what does this run look like", and it composes with the graph's declared/observed reconciliation.

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). Attribute names and stability
levels in `references.md` are the ones to quote rather than recall — semconv versions monthly. Refresh when
messaging conventions reach Stable; that single event changes several design decisions.
