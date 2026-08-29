---
id: kb-dashboards-references
title: "Operational & Test Dashboards — references"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [dashboards, references, red, use, standards]
links:
  - { to: kb-operational-and-test-dashboards, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The authoritative methods, docs and specs behind operational/test dashboards — RED, USE,
  Grafana best practices, the reporting tools' docs, and the charting-library licences.
---

# Reference information

## Methods (the canonical definitions)

- **RED method** (Tom Wilkie) — for **services**: **R**ate (requests/sec), **E**rrors (failed requests),
  **D**uration (latency, as percentiles). Symptom-oriented; the basis for alerting and SLOs. *(Verified, [D5][D6])*
- **USE method** (Brendan Gregg, 2013) — for **resources**: **U**tilisation (busy %), **S**aturation (queue/wait),
  **E**rrors. Cause-oriented; for diagnosis. *(Verified, [D14])*
- **The Four Golden Signals** (Google SRE) — Latency, Traffic, Errors, Saturation — the superset both methods
  descend from. *(Verified, ecosystem / SRE book.)*

## Dashboard best practice

- **Grafana dashboard best practices** — 6–12 panels, critical top-left, consistent time range, colour/threshold
  for anomaly, group by layer, drill-down, avoid metrics sprawl. *(Verified, [D5])*
- **Groundcover — observability dashboards** — correlating metrics/logs/traces; the three-pillars-cohesion rule.
  *(Verified, [D7])*

## Tool documentation

- **Allure Report — Visual Analytics** — the trend/graph widget set. *(Verified, [D3])*
- **ReportPortal — Dashboards & Widgets** — the customisable widget model + the run-identity rule. *(Verified, [D1][D2])*
- **GitHub Actions — Using the visualization graph** — the native job DAG. *(Verified, [D8])*

## Charting-library licences & docs

- **ScottPlot** — MIT — https://scottplot.net/ *(Verified, [D13])*
- **LiveCharts2** — MIT core — https://livecharts.dev/ + LICENSE *(Verified, [D11])*
- **OxyPlot** — MIT — https://oxyplot.github.io/ *(Verified, [D12])*

## Governing pack standards (the panes must conform)

- **`observability-and-instrumentation.md`** (O1–O13) — OTel data model, RED/USE, W3C trace context, stable
  error codes, RFC 9457. The *data contract* a metrics pane renders.
- **`technical-ui-design.md`** (TQ1–TQ12) — numeric legibility, perceptually-uniform colormaps (no jet),
  uncertainty-first, dense-with-hierarchy. The *craft floor*.
- **`ci-and-test-efficiency.md`** (GATE-MUTED, GATE-SHELL, CTRL-D, OPS-CI) and **`end-to-end-integrity.md`**
  (E13 a gate's green ≠ its contents passed; E14 an exit code is not a result) — the **silent failures a CI/test
  pane must be designed to expose**, not hide.
