---
id: mockup-watcher-observatory
title: "Loomkeeper Observatory - Interactive UI Mockup"
type: doc
status: in-review
owner: "@timianmalloo"
phase: "discovery"
tags: [loomkeeper, ui-mockup, observability, agent-scoring, daydream]
links:
  - { to: spec-agentic-watcher-substrate, rel: implements }
  - { to: ui-review-watcher-observatory, rel: relates-to }
review-by: 2026-11-28
review-suggested: []
summary: >-
  Self-contained interactive mockup for watching cross-repository agent sessions, score evidence,
  repository messages, Daydream learning, privacy controls, and Loomkeeper's own health through a
  review harness covering personas, viewports, hard states, themes, density, and reduced motion.
---

# Loomkeeper Observatory mockup

Open [`watcher-observatory.html`](watcher-observatory.html) directly. It is dependency-free and works
over `file://`.

## Direction

- **Vigilant, not alarmist**
- **Forensic, not punitive**
- **Dense, not cramped**
- **Evidence-led, not score-led**

The mockup uses the G6 Multi-Panel Data Terminal archetype inside the existing AI-DE workbench.
Sessions are the default entry, with Message Board, Daydreams, Privacy & Capture, and Watcher Health
as peer surfaces.

## Review harness

The header switches:

- surface;
- fleet operator, learning curator, privacy steward, or watched-agent perspective;
- constrained viewport;
- default, loading, empty, error, partial, offline, blocked, disputed, recomputing, quarantined,
  partial-deletion, failed-retraction, grader-unavailable, and overflow states;
- dark, light, and high-contrast themes;
- compact and comfortable density;
- reduced motion.

The file also computes token contrast and visible-control target sizes. These checks are a floor, not
a substitute for the adversarial review in `ui-review-watcher-observatory`.

