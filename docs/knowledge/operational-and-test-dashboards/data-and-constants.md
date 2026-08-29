---
id: kb-dashboards-data
title: "Operational & Test Dashboards — data, constants & layout rules"
type: knowledge
status: draft
owner: "@timianmalloo"
phase: ""
tags: [dashboards, constants, layout, red, use, licences]
links:
  - { to: kb-operational-and-test-dashboards, rel: refines }
review-by: 2026-11-27
review-suggested: []
summary: >-
  Concrete method definitions, layout rules, charting-library licences and the metrics/percentiles
  a trustworthy operational or test pane must show.
---

# Domain data, constants & layout rules

## RED / USE / Golden Signals (quote these)

| Method | Scope | Signals |
|---|---|---|
| **RED** | services | Rate · Errors · Duration(latency percentiles) |
| **USE** | resources | Utilisation · Saturation · Errors |
| **Four Golden Signals** | services | Latency · Traffic · Errors · Saturation |

*(Verified, [D5][D14].)* Alert on **RED symptoms**; diagnose with **USE causes**.

## Dashboard layout rules (documented)

- **6–12 panels** per view; more is "metrics sprawl". *(Verified, [D5])*
- **Most-critical top-left**; detail increases left→right, top→bottom. *(Verified, [D7])*
- **One dashboard-level time range** control shared by all panels. *(Verified, [D5])*
- **Colour + thresholds for anomaly only** — not decoration. *(Verified, [D5])*
- **Group by layer** — RED (symptoms) on top, USE (causes) below. *(Verified, [D7])*
- **Drill-down on every panel**; **three-pillars cohesion** (≥1 metrics, ≥1 logs, ≥1 traces panel + links). *(Verified, [D7])*

## What a *trustworthy* pane must show (against the silent failures)

- **Latency as p50/p95/p99, never a mean** — averages hide the tail. *(Verified, [D5]; technical-ui TQ5.)*
- **Flaky vs failing distinctly** — a flaky test is not a regression; conflating them is a defect. *(Verified, [D1].)*
- **"Gate steps executed / defined", not just green/red** — a green pipeline can hide a muted or never-run step
  (`end-to-end-integrity.md` E13, `ci-and-test-efficiency.md` GATE-MUTED/CTRL-D). *(Verified, pack standards.)*
- **Run/commit/trace provenance on every figure** — a number without the run that produced it is unverifiable.
- **Numerics** — tabular, right-aligned, consistent precision, unit-bearing (`technical-ui-design.md` TQ2).
- **Colormaps** — perceptually uniform (viridis family), **never rainbow/jet** (`technical-ui-design.md` TQ3).

## Test-reporting rule

- **Stable run/launch identity** — dynamic data (timestamps, build ids) goes in attributes/metadata, **never in
  the run name**, or trend analysis breaks. *(Verified, [D1].)*

## Charting-library licence facts (verify versions before pinning)

| Package | Licence | Note |
|---|---|---|
| **ScottPlot** | MIT | fast/large-data/interactive; 2D; WPF native *(Flagged: version)* |
| **LiveCharts2** | **MIT core** (+ paid advanced) | animated dashboards; confirm needed features in MIT core *(Flagged: free/paid boundary + version)* |
| **OxyPlot** | MIT | light static/scientific; WPF native *(Flagged: version)* |
| **Grafana** (if embedded) | **AGPLv3** | copyleft — embedding as a separate service is fine; do not vendor its source *(Verified — note the licence)* |
| **Allure Report** | Apache-2.0 | embed generated HTML |
| **ReportPortal** | Apache-2.0 | self-host + API |

**Licence caution:** Grafana core is **AGPLv3** — running it as a separate service the pane loads over HTTP is
standard and safe; **statically linking or vendoring its source into the app is not.** The native charting libs
(MIT) carry no such constraint. *(Verified — AGPL is the one non-permissive item here; Security/Simplifier note.)*
