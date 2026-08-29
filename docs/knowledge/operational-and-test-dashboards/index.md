---
id: kb-operational-and-test-dashboards
title: "Operational & Test Result Dashboards — domain knowledge"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [dashboards, test-results, ci-cd, observability, metrics, charting, grafana, allure]
links:
  - { to: knowledge-hub, rel: refines }
  - { to: kb-microservice-interaction-visualization, rel: relates-to }
  - { to: kb-diagram-generation, rel: relates-to }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Evidence base for the AI-DE panes that visualise test results, CI/CD pipeline execution, and
  operational logs/metrics: the reporting tools (Allure, ReportPortal), pipeline-as-diagram tools,
  the RED/USE dashboard methods, the permissive charting libraries (LiveCharts2, ScottPlot, OxyPlot),
  and the design rules that keep a dashboard actionable rather than a wall of numbers.
---

# Operational & Test Result Dashboards — domain knowledge

**Domain & problem:** AI-DE renders *derived visual panes* beside its agent terminals. Three of the highest-
value panes present **operational and quality data about the codebase under work**: (1) **test results** —
pass/fail, trends, flaky-test detection, coverage; (2) **CI/CD execution** — the pipeline as a live DAG with
per-job status; (3) **operational logs/metrics** — the RED/USE health of any service the code runs as. This
base gathers how the field builds each, the permissively-licensed rendering options, and the design rules.

**Canonical framing:** The field frames these as three separate products — **test reporting** (Allure,
ReportPortal), **CI observability** (the platform's own run graph plus pipeline-as-diagram tools), and
**metrics/observability dashboards** (Grafana + Prometheus/Loki/Tempo). Our framing is one pane host over all
three, fed from the local daemon and the repo's own CI/telemetry — so the design question is less "which
product" and more "which *rendering* and which *design rules* make a trustworthy, glanceable pane." The unifying
principle across all three, stated by every source, is the same: **a dashboard is for at-a-glance decisions,
so it must be a small set of high-signal, actionable indicators with drill-down — not a metrics dump.**

**Compiled:** 2026-08-29 · **Lead:** Domain Researcher · **Status:** fresh

*(`data-and-constants.md` carries the charting-library licences and the RED/USE definitions to quote.)*

## Headline findings

1. **Test dashboards are a solved product category; the value is in trends, flakiness and drill-down, not
   pass/fail.** Allure Report (open source, static HTML per run — great for CI artifacts) and ReportPortal
   (self-hosted, collaborative, AI failure-clustering) both centre on **historical trend widgets, flaky-test
   detection, severity distribution, duration-over-time**, and one-click drill-down from a failing summary to
   logs/screenshots/stack traces. A dashboard that only shows the latest pass rate is the anti-pattern. — *(Verified, [D1][D2][D3][D4])*
2. **Consistent run identity is the precondition for trend analysis.** Every test-reporting source warns:
   do **not** put dynamic data (timestamps, build ids) in the run/launch *name* — put it in
   attributes/metadata — or longitudinal comparison breaks. This is the single most common test-dashboard
   defect. — *(Verified, [D1])*
3. **CI/CD pipelines are visualised as a job-dependency DAG, and the platform already emits one.** GitHub
   Actions renders a real-time job dependency + execution graph per run natively; third-party tools
   (Pipeline Visualizer VS Code extension → **Mermaid**, jsonviewer.tools, FOSSA/drawflow visualizers) turn
   the workflow YAML into an exportable diagram. **The pipeline-as-diagram path is Mermaid-based**, which
   connects directly to the existing [`diagram-generation`](../diagram-generation/index.md) base. — *(Verified, [D8][D9][D10])*
4. **RED and USE are the two canonical dashboard methods, and they answer different questions.** **RED**
   (Rate, Errors, Duration) monitors *services* and surfaces user-facing *symptoms* — the basis for alerting
   and SLOs. **USE** (Utilisation, Saturation, Errors) monitors *resources* and surfaces *causes*. A good
   operational pane alerts on RED symptoms and diagnoses with USE. This mirrors the pack's own
   `observability-and-instrumentation.md`. — *(Verified, [D5][D6][D7][D14])*
5. **Dashboard layout has documented rules: 6–12 high-signal panels, critical top-left, consistent time
   range, colour only for anomaly, group by layer, always offer drill-down.** Grafana's own best-practice
   docs and the observability literature converge on this. "Metrics sprawl" — dozens of vanity panels — is the
   named failure mode. — *(Verified, [D5][D7])*
6. **For an embedded WPF pane, the permissive charting field is MIT and strong: LiveCharts2, ScottPlot and
   OxyPlot.** **ScottPlot** (MIT) is fastest for large datasets and interactive; **LiveCharts2** (MIT core,
   paid advanced tier) is the most dashboard-oriented and animated, cross-platform incl. WPF; **OxyPlot**
   (MIT) is lightweight for scientific/static plots. All three support WPF natively and impose no copyleft. — *(Verified, [D11][D12][D13])*
7. **The three panes want different renderers, and it's the same tiering decision as diagrams.** A live
   metrics pane wants a fast native charting lib (ScottPlot/LiveCharts2) or an embedded Grafana view; a CI
   pipeline pane wants a Mermaid/graph render (already in `diagram-generation`); a test-report pane can embed
   Allure's static HTML directly in the WebView2 pane. Pick per pane, not one renderer for all. — *(Inferred from [D8][D11]; cross-ref diagram-generation)*
8. **Drill-down is the feature that makes a dashboard a tool rather than a poster.** Every source stresses:
   one click from a red widget must reach the evidence (the failing test's log, the saturated resource's
   trace, the failed job's output) without a context switch. A pane that shows a red bar and stops is a
   notification, not a dashboard. — *(Verified, [D1][D5][D7])*
9. **Test/CI/metrics data has a natural "confidence and evidence" shape that matches AI-DE's graph.** A flaky
   test, a low-coverage module, a saturated resource are all *derived facts with provenance* — the same
   confidence+evidence discipline the code-knowledge-graph bases already mandate. The panes should carry the
   run/commit/trace that produced each figure. — *(Inferred; consistent with the hub's cross-cutting finding.)*
10. **Dashboards degrade to wrong conclusions silently.** Averaged latency hides the tail; a green pipeline
    can hide a muted step; a pass-rate can hide a suite that never ran. The pack's own `ci-and-test-efficiency.md`
    (GATE-MUTED, CTRL-D) and `end-to-end-integrity.md` (E13/E14) are the failure modes to visualise *against* —
    a good pane shows percentiles not means, and "did the gate actually run" not just "is it green." — *(Verified, pack standards; [D7])*

## Confidence summary

- **Verified:** the Allure/ReportPortal feature model and the run-identity rule; the GitHub Actions native
  graph and the Mermaid-based pipeline-visualizer path; the RED/USE definitions and their alert-vs-diagnose
  split; the Grafana layout best practices; the MIT licences of LiveCharts2 (core), ScottPlot and OxyPlot.
- **Inferred:** the per-pane renderer tiering; the confidence+evidence mapping onto AI-DE's graph.
- **Flagged (load-bearing):** **LiveCharts2's exact free-vs-paid boundary** (core is MIT; some advanced
  features are a paid package — confirm the specific features needed are in the MIT core before adopting);
  and the **current versions** of all three charting libs.

## Design implications (what /design should do with this)

- **Build three distinct panes, not one dashboard.** Test-results, CI-pipeline, and metrics are different data
  shapes with different renderers and refresh models. Share the *chrome* (per `wpf-modern-ui-styling`) and the
  *design rules* below; not the renderer.
- **Reuse the diagram pipeline for the CI pane.** A pipeline DAG is a diagram; generate Mermaid/graph DSL from
  the workflow definition or the live run and render it in the existing diagram pane — do not build a bespoke
  pipeline renderer (`diagram-generation`).
- **Embed Allure static HTML for the test-report pane** (it already targets a browser and drops into WebView2),
  and/or render live test trends with a native charting lib for the always-on summary. Preserve **run identity**
  so trends work.
- **Design the metrics pane to RED/USE.** RED panels top (service symptoms, for the summary/alert), USE panels
  below (resource causes, for diagnosis); ≤12 panels; critical top-left; percentiles not means; colour for
  anomaly only; one time-range control; every panel drill-down-able. This is `observability-and-instrumentation.md`
  made visual.
- **Pick the charting library by need, MIT-only.** ScottPlot for large/fast interactive series; LiveCharts2 for
  animated dashboard widgets (confirm needed features are in the MIT core); OxyPlot for simple static plots.
- **Apply the technical-UI rules to every pane.** `technical-ui-design.md`: tabular right-aligned numerics with
  units and consistent precision (TQ2), perceptually-uniform colormaps — **never rainbow/jet** — for any
  heatmap (TQ3), dense-with-hierarchy (TQ1), and show uncertainty/percentiles not just point values (TQ5).
- **Visualise against the silent failures.** Show p95/p99 (not mean) latency; show "gate steps executed / gate
  steps defined" not just green/red; show flaky vs failing distinctly. The pack's CI-efficiency and
  end-to-end-integrity classes name exactly what a naive dashboard hides.

## Cross-references

- CI pipeline / any DAG rendering → [`diagram-generation`](../diagram-generation/index.md).
- Service/trace topology (distinct from metrics dashboards) → [`microservice-interaction-visualization`](../microservice-interaction-visualization/index.md).
- The WPF chrome these panes live in → [`wpf-modern-ui-styling`](../wpf-modern-ui-styling/index.md).
- Governing pack standards: `observability-and-instrumentation.md` (RED/USE, OTel, error codes),
  `technical-ui-design.md` (TQ1–TQ12), `ci-and-test-efficiency.md` (what a CI dashboard must not hide),
  `testing-strategy.md` (what "test results" should actually assert).

## How to use this base

Personas and the design skills cite these files as evidence (BoK §III.1). The licences and method definitions
in `references.md`/`data-and-constants.md` are the ones to quote. Refresh when a charting lib ships a major
version or the CI platform changes its run-graph surface.
