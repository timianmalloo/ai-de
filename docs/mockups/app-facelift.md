---
id: mockup-app-facelift
title: "App Facelift — soft-islands shell (mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [mockup, facelift, soft-islands, menu, icons]
links:
  - { to: spec-app-facelift, rel: documents }
review-by: 2027-02-27
review-suggested: []
summary: >-
  Self-contained, dependency-free mockup of the facelift shell — soft rounded island panes with
  resting elevation, a discoverable menu bar + icon system, a focused-pane indicator, and a review
  harness (theme · motion · density · state · focus). The .html is data; this .md is its graph node.
---

# App Facelift — mockup

`app-facelift.html` — the soft-islands evolution of the workbench (spec-app-facelift, DESIGN.md facelift
section). Renders: custom title bar (DWM-rounded in the real window), menu bar with disabled-reason states,
icon sidebar, three island panes with a focused-pane accent, provenance status strip, and the Provenance
pane's **data / empty / error** states. Harness switches theme (dark/light/high-contrast), motion, density,
the provenance-pane state, and which pane is focused.

**Key correctness demonstrations:** confidence is glyph+word+colour (never colour alone); softening keeps
density; layout has no animation (0ms); high-contrast mode drops shadows and re-states semantic colours.
