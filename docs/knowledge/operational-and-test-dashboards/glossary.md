---
id: kb-dashboards-glossary
title: "Operational & Test Dashboards — glossary"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [glossary, dashboards, red, use, ci-cd, test]
links:
  - { to: kb-operational-and-test-dashboards, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Precise definitions for the dashboard/observability/test-reporting vocabulary so the panes and
  their docs agree.
---

# Glossary — ubiquitous language

| Term | Definition |
|---|---|
| **RED method** | Service-monitoring method: **R**ate, **E**rrors, **D**uration (latency). Symptom-oriented; the basis for alerting/SLOs. *(Verified, [D5])* |
| **USE method** | Resource-monitoring method: **U**tilisation, **S**aturation, **E**rrors. Cause-oriented; for diagnosis. *(Verified, [D14])* |
| **Four Golden Signals** | Google SRE's Latency, Traffic, Errors, Saturation — the superset of RED/USE. *(Verified)* |
| **Flaky test** | A test that changes status (pass↔fail) without a code change; detected by historical-status analysis. Distinct from a failing (regressed) test — must be shown separately. *(Verified, [D1])* |
| **Launch / run identity** | The stable name that groups a test run into a trend. Dynamic data (timestamps, build ids) must live in attributes, **not** the name. *(Verified, [D1])* |
| **Drill-down** | One-click navigation from a summary widget to the underlying evidence (log, screenshot, trace, job output) without a context switch. The feature that makes a dashboard a tool. *(Verified, [D1][D7])* |
| **Metrics sprawl** | The anti-pattern of dozens of low-signal panels; the reason for the 6–12-panel rule. *(Verified, [D5])* |
| **Pipeline DAG** | A CI/CD run rendered as a directed acyclic graph of jobs with dependency edges and per-job status. Rendered natively by the platform or via YAML→Mermaid tools. *(Verified, [D8][D10])* |
| **Three pillars** | Metrics, logs, and traces — the three observability signal types a mature pane correlates. *(Verified, [D7])* |
| **SLO / SLI** | Service Level Objective / Indicator — a target (e.g. p99 < 300ms, error budget) a dashboard panel tracks against. *(Verified, ecosystem)* |
| **Percentile latency** | Latency reported as p50/p95/p99 rather than a mean, so the tail is visible. The mean is the classic hidden-failure. *(Verified, [D5])* |
| **Allure Report** | Open-source test-reporting tool producing a static-HTML per-run report with trend graphs and attachments. *(Verified, [D3])* |
| **ReportPortal** | Self-hosted, collaborative test-analytics platform with customisable dashboards and AI failure clustering. *(Verified, [D1])* |
