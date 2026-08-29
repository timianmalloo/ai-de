---
id: kb-dashboards-sources
title: "Operational & Test Dashboards — sources"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [sources, citations]
links:
  - { to: kb-operational-and-test-dashboards, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  The full access-dated source list behind the operational-and-test-dashboards base, keyed
  [D1]..[D14] as cited throughout the topic.
---

# Sources

All accessed **2026-08-29**. Citation keys `[Dn]` are used throughout this topic.

| # | Title | Type | URL | Used for |
|---|---|---|---|---|
| D1 | ReportPortal — Test results visualization | primary (vendor) | https://reportportal.io/blog/test-results-visualization/ | Trends, flaky detection, run-identity rule, drill-down |
| D2 | ReportPortal — Dashboards and widgets | primary (vendor docs) | https://reportportal.io/docs/dashboards-and-widgets/ | Customisable widget model |
| D3 | Allure Report — Visual Analytics | primary (vendor docs) | https://allurereport.org/docs/visual-analytics/ | Trend/graph widgets |
| D4 | Allure Report — home | primary (vendor) | https://allurereport.org/ | Static-HTML per-run report, embeddable |
| D5 | Grafana dashboard best practices | primary (vendor docs) | https://grafana.com/docs/grafana/latest/dashboards/build-dashboards/best-practices/ | 6–12 panels, layout, RED/USE, drill-down |
| D6 | Grafana — common observability strategies (RED/USE) | primary (vendor docs) | https://grafana.com/docs/grafana/latest/dashboards/common-observability-strategies/ | RED/USE definitions |
| D7 | Groundcover — Grafana observability dashboards | secondary | https://www.groundcover.com/learn/observability/grafana-dashboards | Three-pillars cohesion, correlation, layout |
| D8 | GitHub Docs — Using the visualization graph | primary (official) | https://docs.github.com/en/actions/how-tos/monitor-workflows/use-the-visualization-graph | Native job-dependency DAG |
| D9 | jsonviewer.tools — Convert GitHub Actions YAML to diagrams | secondary | https://jsonviewer.tools/blog/github-ci-cd-workflows-diagram | YAML→DAG technique |
| D10 | Pipeline Visualizer (VS Code) | primary (repo) | https://github.com/ThatInfraDba/PipelineVisualizer | YAML→Mermaid pipeline diagram, multi-platform |
| D11 | LiveCharts2 — home + LICENSE | primary (repo/site) | https://livecharts.dev/ | MIT core, dashboard-oriented, WPF |
| D12 | OxyPlot | primary (site) | https://oxyplot.github.io/ | MIT, light 2D plotting, WPF |
| D13 | ScottPlot | primary (site) | https://scottplot.net/ | MIT, fast large-data interactive, WPF |
| D14 | Brendan Gregg — The USE Method | primary (author) | https://www.brendangregg.com/usemethod.html | USE definition; RED as its symptom-side complement |

## Source-quality notes

- **RED** is attributed to Tom Wilkie (Weaveworks/Grafana) and is documented in Grafana's own strategy docs
  ([D5][D6]); **USE** is Brendan Gregg's, from his own site ([D14]). Both are stable, canonical definitions.
- **Charting-library licences** (ScottPlot, LiveCharts2, OxyPlot = MIT) are cited to each project's own site;
  the **LiveCharts2 free-vs-paid boundary** was not enumerated this session and is Flagged.
- **Grafana's AGPLv3** licence is the one non-permissive item and is called out in `data-and-constants.md` —
  embed-as-a-service is fine, vendoring the source is not.
- The **CI-pane-must-not-hide** rules draw on the pack's own `ci-and-test-efficiency.md` and
  `end-to-end-integrity.md`, which are governing standards rather than external sources.
