---
id: kb-dashboards-comparables
title: "Operational & Test Dashboards — comparables & tools"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [dashboards, tools, charting-libraries, licences]
links:
  - { to: kb-operational-and-test-dashboards, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Named tools and libraries for test-result, CI/CD and metrics visualisation, with licence, role
  and where each fits an embedded WPF pane.
---

# Comparable solutions, tools & libraries

## Test-result reporting

| Tool | Licence | Role | Fits our pane as | Confidence |
|---|---|---|---|---|
| **Allure Report** | Apache-2.0 | Per-run static-HTML report, rich attachments, trend graphs | Embed HTML in WebView2 pane | Verified [D3][D4] |
| **ReportPortal** | Apache-2.0 | Live collaborative analytics, AI failure clustering, flaky detection | Self-host + embed / API feed | Verified [D1][D2] |
| **Native runner output** (xUnit `.trx`, JUnit XML) | — | Raw result data | Parse into native trend charts | Inferred |

## CI/CD pipeline visualisation

| Tool | Licence | Role | Fits our pane as | Confidence |
|---|---|---|---|---|
| **GitHub Actions run graph** | (platform) | Native live job DAG | Reference / link out | Verified [D8] |
| **Pipeline Visualizer** (VS Code ext) | open source | YAML → interactive **Mermaid** DAG (GH/ADO/GitLab/Bitbucket) | Reuse Mermaid render in diagram pane | Verified [D10] |
| **jsonviewer.tools / FOSSA / drawflow** | web tools | YAML → exportable DAG image | Reference technique | Verified [D9] |

## Metrics / observability dashboards

| Tool | Licence | Role | Fits our pane as | Confidence |
|---|---|---|---|---|
| **Grafana** | AGPLv3 (OSS core) | RED/USE panels, alerting, Loki/Tempo drill-down | Embed in WebView2 pane (note AGPL) | Verified [D5][D7] |
| **Prometheus / Loki / Tempo** | Apache-2.0 | metrics / logs / traces stores | Data sources behind a pane | Verified (ecosystem) |

## Charting libraries for a native WPF pane (all MIT, permissive)

| Library | Licence | Strength | Weakness | Confidence |
|---|---|---|---|---|
| **ScottPlot** | MIT | Fastest for large datasets; interactive; many plot types; WPF native | 2D only; community support only | Verified [D13] |
| **LiveCharts2** | **MIT core** (+ paid advanced tier) | Animated, dashboard-oriented, gauges/maps, cross-platform incl. WPF | Confirm needed features are in the MIT core | Verified [D11] |
| **OxyPlot** | MIT | Lightweight, scientific/static 2D plotting; WPF native | Limited animation/dashboard features | Verified [D12] |

**Commercial (reference only):** LightningChart, SciChart, Syncfusion, DevExpress charts — GPU-accelerated,
3D, support tiers, but proprietary/cost. Listed for completeness; the constraint is permissive OSS.

## Adjacent problems worth borrowing from

- **The pack's observability standard** (`observability-and-instrumentation.md`) already prescribes the OTel
  data model, RED/USE, stable error codes and RFC 9457 problems — the *data* a metrics pane visualises.
- **The diagram-generation base** already solves DAG rendering, layout stability and the MIT-vs-copyleft
  licence map — the CI pane inherits all of it.
- **The technical-UI base** (`technical-ui-design.md`) already prescribes numeric legibility, colormaps and
  uncertainty-first — the *craft* every pane must meet.
