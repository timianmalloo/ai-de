---
id: kb-dashboards-sota
title: "Operational & Test Dashboards — state of the art"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [dashboards, red, use, allure, reportportal, grafana]
links:
  - { to: kb-operational-and-test-dashboards, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Current best practice for test-result, CI/CD and operational-metrics visualisation — the
  reporting tools, the pipeline-as-diagram path, and the RED/USE dashboard methods.
---

# State of the art — operational & test dashboards

## Test-result reporting

- **Allure Report** (open source) — generates a **static HTML** report per run: suites, timeline, categories,
  graphs (status/severity/duration trends), retries, and rich attachments (screenshots, logs). Ideal as a CI
  artifact and trivially embeddable in a WebView2 pane. *(Verified, [D3][D4])*
- **ReportPortal** (self-hosted) — a live, collaborative analytics platform: customisable dashboards + widgets,
  historical trends, **AI-assisted failure clustering**, flaky-test detection. Better for continuous
  multi-team analytics than per-run artifacts. *(Verified, [D1][D2])*
- **The trend, not the run, is the product.** Both centre analytics on *history*: pass-rate over time, flaky
  trend, failure-reason distribution, duration regression. And both require **stable run identity** (no dynamic
  data in the launch name) for trends to be valid. *(Verified, [D1])*
- **Drill-down is mandatory** — one click from a failing widget to the log/screenshot/stack trace. *(Verified, [D1])*

## CI/CD pipeline visualisation

- **Native platform graph** — GitHub Actions renders a real-time job-dependency + execution graph per run;
  good for monitoring, not customisable/exportable. *(Verified, [D8])*
- **Pipeline-as-diagram tools** — the **Pipeline Visualizer** VS Code extension renders GitHub Actions / Azure
  DevOps / GitLab / Bitbucket pipelines as interactive **Mermaid** diagrams; jsonviewer.tools and FOSSA/drawflow
  turn workflow YAML into exportable DAGs. The dominant technique is **YAML → DAG → Mermaid/graph render**.
  *(Verified, [D9][D10])*
- **Implication:** a CI pane is a *diagram* pane; it reuses the diagram pipeline, not a bespoke renderer.

## Operational metrics dashboards (the RED/USE methods)

- **RED** (Rate, Errors, Duration) — per *service*: request rate, error count/ratio, latency (as percentiles).
  Surfaces **symptoms**; the basis for alerting and SLOs. Coined by Tom Wilkie. *(Verified, [D5][D6])*
- **USE** (Utilisation, Saturation, Errors) — per *resource* (CPU, memory, disk, queue): how busy, how
  overloaded, error rate. Surfaces **causes**; for diagnosis. Coined by Brendan Gregg. *(Verified, [D14])*
- **Layout best practice** — 6–12 high-signal panels per view; most-critical top-left; left-to-right,
  top-to-bottom detail flow; one dashboard-level time control; colour and thresholds for anomaly only;
  group by layer (RED on top, USE below); correlate metrics↔logs↔traces with panel links; drill-down
  everywhere. Avoid "metrics sprawl." *(Verified, [D5][D7])*
- **Three pillars cohesion** — a mature pane shows at least one metrics, one logs, and one traces panel for the
  service in view, with links to jump between them. *(Verified, [D7])*

## Rendering options for an embedded WPF pane

- **Native charting** — ScottPlot (fast, large data, interactive), LiveCharts2 (animated, dashboard-oriented),
  OxyPlot (light, static/scientific). All MIT. *(Verified, [D11][D12][D13])*
- **Embed Grafana** — a hosted/self-hosted Grafana dashboard rendered in the WebView2 pane gives RED/USE
  panels, alerting and the Loki/Tempo drill-downs for free; heavier dependency, richer result. *(Inferred.)*
- **Embed Allure HTML** — the test-report pane's simplest path. *(Verified, [D4].)*

## The frontier / what's moving

- **AI failure clustering** (ReportPortal) — grouping test failures by root cause automatically; maturing.
- **Pipeline observability convergence** — CI runs increasingly emit OpenTelemetry traces, so a pipeline can be
  visualised as a *trace* (waterfall) as well as a DAG. Watch the OTel CI/CD semantic conventions.
