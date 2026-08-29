---
id: kb-dashboards-open-questions
title: "Operational & Test Dashboards — open questions & failure modes"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [open-questions, failure-modes, dashboards, disconfirming]
links:
  - { to: kb-operational-and-test-dashboards, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  What the dashboards research could not settle, the domain's silent failure modes, and the
  disconfirming views deliberately sought against building bespoke visualisation panes.
---

# Open questions & domain failure modes

## Unresolved by research

- **LiveCharts2's exact free-vs-paid boundary.** The core is MIT but an advanced tier is paid; *which* features
  are gated was not enumerated this session. Confirm the specific widgets needed (gauges, maps, real-time
  streaming) are in the MIT core before adopting. *(Flagged — cheap to settle on the pricing page.)*
- **Whether to embed Grafana or render natively.** Embedding Grafana (AGPL, as a separate service) gives
  RED/USE + alerting + Loki/Tempo drill-down for near-zero build; native charting gives full control and no
  external service. The choice depends on whether the AI-DE user already runs a metrics stack. Not settled. *(Flagged.)*
- **Do CI platforms emit enough to render a live pipeline pane without polling their API heavily?** GitHub's
  native graph exists but the exportable/embeddable path is third-party YAML parsing. Whether a live per-job
  status pane needs API polling (rate limits) or can subscribe to events was not established. *(Flagged.)*

## Known failure modes of this domain

- **The mean hides the tail.** A latency panel showing average latency looks healthy while p99 is failing the
  SLO. Always percentiles. *(Verified, [D5].)*
- **Green pipeline, dead gate.** A CI pane that shows only pass/fail can render green when a step was muted or
  never ran (`ci-and-test-efficiency.md` GATE-MUTED/CTRL-D; `end-to-end-integrity.md` E13). The pane must show
  "steps executed / defined", not just the aggregate. *(Verified, pack standards.)*
- **Flaky counted as failing (or hidden).** Conflating flaky and failing tests either cries wolf or masks a real
  regression under "known flaky". Show them as distinct categories. *(Verified, [D1].)*
- **Trend-breaking run names.** Dynamic data in the run name silently fragments the history so every trend
  widget is empty or wrong. *(Verified, [D1].)*
- **Metrics sprawl.** A pane with 40 panels is a poster nobody reads; the signal drowns. *(Verified, [D5].)*
- **Rainbow colormaps.** A heatmap in jet/rainbow invents false boundaries and fails colour-blind users
  (`technical-ui-design.md` TQ3). *(Verified, pack standard.)*
- **A number with no provenance.** A figure with no link to the run/commit/trace that produced it cannot be
  trusted or acted on — the dashboard analogue of an uncited claim. *(Inferred; consistent with the graph
  confidence+evidence discipline.)*

## Disconfirming views we deliberately sought

- **"Don't build panes — just link out to Allure/Grafana/the CI run."** A strong, cheap option (Solution-
  Selection Ladder): the existing tools already render these well, and an AI-DE pane could simply host their
  web UIs in WebView2. The case *against* bespoke rendering is that reporting/observability dashboards are a
  mature, deep product category we would be re-implementing badly. **Verdict:** default to *embedding* the
  established tool (Allure HTML, Grafana service, the CI run graph) in a pane; build native charting only for
  the always-on *summary* glance and where tight integration with the code graph earns it. This narrows the
  base rather than refuting it. *(Corroborated by [D4][D7].)*
- **"The CI pane is just a diagram, so this base is redundant with `diagram-generation`."** Half true and
  deliberately reflected in the design implications: the *rendering* of the CI DAG is diagram-generation's job.
  What this base adds that diagram-generation does not is the **test-reporting** and **metrics/RED-USE** content
  and the **what-a-trustworthy-pane-must-not-hide** rules — which are not diagram concerns. The base survives,
  scoped to the data-and-design layer above the renderer.
- **"Metrics dashboards belong in `microservice-interaction-visualization`."** That base covers *topology and
  traces* (the service graph, trace→sequence). RED/USE *metric* dashboards, *test* reporting and *CI* pipelines
  are a different data shape and a different consumer (the developer watching their own build/quality), so a
  separate base is warranted; the two cross-link.
