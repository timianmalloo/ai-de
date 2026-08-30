---
id: mockup-activity-rail
title: "Activity rail — icon-only elevate (mockup)"
type: doc
status: accepted
owner: "@timianmalloo"
tags: [mockup, ui-design, activity-rail, navigation, icons, tooltip, wcag]
links:
  - { to: spec-app-facelift, rel: relates-to }
  - { to: review-ui-activity-rail, rel: documents }
review-by: 2027-02-27
summary: >-
  Self-contained mockup for the icon-only activity rail that replaces the clipped icon+label rail
  ("Coordinate" -> "ordina" at 56px). Renders rest/hover/focus/active states, tooltip, before/after,
  theme and reduced-motion via the review harness, with a token-layer contrast readout. The change
  is landed in src/AiDe.App/MainWindow.xaml.
---

# Activity rail — icon-only elevate

The [self-contained mockup](activity-rail.html) renders the rail at its real 56px width with the
review harness (before/after · state · theme · reduced-motion) and an in-artifact contrast readout.

The defect and the fix are recorded in [the review](../reviews/ui-activity-rail.md). The change is
landed in `src/AiDe.App/MainWindow.xaml` (rail labels removed; icon-only; 44px targets; softened
active pill; tooltip + `AutomationProperties.Name` preserved).
