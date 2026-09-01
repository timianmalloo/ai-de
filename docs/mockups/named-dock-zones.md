---
id: mockup-named-dock-zones
title: "Named Dock Zones — mockup (hub)"
type: doc
status: draft
owner: "@timianmalloo"
phase: "3"
tags: [workbench, layout, mockup, ux]
links:
  - { to: design-named-dock-zones-ui, rel: documents }
  - { to: spec-named-dock-zones, rel: relates-to }
review-by: 2027-02-28
summary: >-
  Graph hub for docs/mockups/named-dock-zones.html — a self-contained, dependency-free
  mockup of the named dock-zone workbench with a review harness (zone states, theme,
  reduced motion). Open the HTML directly; this node makes it discoverable in the graph.
---

# Named Dock Zones — mockup (hub)

The mockup is [`named-dock-zones.html`](./named-dock-zones.html) — open it in any browser over
`file://`. It renders the four-zone frame (Left / Right / Bottom / Center) with tab strips, rails,
collapse chevrons and a maximize affordance, and a **review harness** (top bar) that switches:

- **State** — default (all populated) · left collapsed → rail · bottom collapsed → rail · center
  maximized · drag-into-Right · drag-into-Bottom · center empty (no documents).
- **Theme** — light / dark.
- **Motion** — full / reduced (proves the reduced-motion path).

Every value is a CSS custom-property token (U3/U20). The harness bar is review chrome and is not part
of the design under review. See `design-named-dock-zones-ui` for the direction brief and rubric
critique, and `spec-named-dock-zones` / `adr-0021-named-dock-zones` for the behavior and architecture.
