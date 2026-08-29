---
id: mockup-context-map-join
title: "Context Map & Join surfaces — Core→Design §4a rendering (mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [mockup, context-map, join, evidence-shortfall, collaboration]
links:
  - { to: session-contracts, rel: documents }
  - { to: spec-knowledge-exploration, rel: relates-to }
review-by: 2027-02-27
review-suggested: []
summary: >-
  Self-contained mockup rendering the Core session's ContextMapView and JoinResult view models,
  demonstrating the three accepted §4a requests — bounded-read "≥ N (capped)" counts, the dominant
  crossing class promoted out of the grey suffix, and the IsDeclared==false first-run empty state.
---

# Context Map & Join surfaces — mockup (Core→Design §4a)

`context-map-join.html` — the Design session's concrete rendering contract for the Core session's
`ContextMapView` and `JoinResult` view models (session-contracts §3, §4a). It shows, before Core adds the
fields, exactly how each renders:

- **Evidence shortfall** — a capped count renders `≥ 20,000` with a `capped` chip and a cap-naming tooltip,
  visually distinct from an exact `20,000`. A bounded read never looks complete (the same failure class as
  provenance laundering). Harness: **Read = Capped / Exact**.
- **Dominant target** — the dominant crossing class (`ORM · 57 of 72`) is an accent emphasis chip with a
  share bar, not a grey suffix. Renders `ContextEdge.DominantTarget` / `DominantCount`.
- **`IsDeclared == false`** — a first-run **empty state** (icon + one line + first action), not a heading and
  a muted paragraph. Harness: **Map = Not declared**.
- **Joins** — every join shows its `Status` (satisfied / inferred / unmet) and `Basis` with provenance;
  "unmet" / "not recorded" are explicit, never rendered as satisfied.

Harness switches theme, motion, the read (capped/exact), and the map state (declared / not-declared / error).
This mockup and its `DESIGN.md` §4a tokens are the Design half of the accepted two-session contract.
