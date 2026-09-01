---
id: "note-20260901-the-default-graph-is-a-map-of-the-code"
title: "The default graph is a map of the code, and knowledge is reached by navigation"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "phase-3"
tags: [decision-note, graph, knowledge, node-budget, projection]
links:
  - { to: note-20260830-the-graph-carries-only-observable-links, rel: relates-to }
links-suggested: []
review-by: 2027-03-01
review-suggested: []
summary: >-
  The default graph keeps the most-connected nodes and reserves no slots per category, so knowledge
  nodes — measured median relation degree 0, and 569 of 878 with no edge at all — are reached by
  search and drill-down rather than by a share of the node budget. The cut is stated rather than
  silent: DeclaredByKind carries the denominator, NotInView says when a hit was not drawn, and
  DescribeResult.KnowledgeIds lets the drill-down know what it is holding.
---

# The default graph is a map of the code

**Decision.** The default graph view keeps the most-connected nodes of the workspace and does **not**
reserve slots per category. Knowledge nodes appear when they have edges; otherwise they are reached
by **navigation** — search, then drill-down — rather than by a share of the node budget.

## Why this is a correctness question and not a preference

The user's decision of 2026-08-30 is that documentation and code *"will tend to be orthogonal, which
is why pruning the graph on one or the other is a meaningful cut"*, and that the graph carries only
observable links. Those two together settle this: if the two halves are orthogonal, a single view
cannot be a faithful map of both, and the honest move is to cut on one and say so.

The measurements say which cut. On the user's real workspace:

| Measured | Value |
|---|---|
| knowledge nodes | 878 |
| knowledge median relation degree | **0** (against 4 for everything else) |
| knowledge nodes with no edge at all | 569 of 878 |
| knowledge → code edges | **0** |

The ordering is already *declared-first, then by degree*, so knowledge is not excluded by category —
it loses on degree. Reserving slots would draw several hundred **disconnected dots** and evict
connected code nodes to do it. That is not a more representative view; it is a less readable one
purchased by discarding the relationships the graph exists to show.

## What makes this a stated bound rather than a silent loss

A cut is only honest if the user can see it and get past it. Three things carry that, and all three
are in place:

- **`DeclaredByKind`** crosses the wire, so a chip reading "Knowledge 257" has 878 behind it and the
  surface can say which number is exact and which is bounded.
- **`NotInView`** is returned by a refresh whose requested node is not among the drawn nodes, so a
  search hit the user picked is told the truth rather than being announced as a centring that did not
  happen.
- **`DescribeResult.KnowledgeIds`** means the drill-down a user lands on from `NotInView` knows what
  it is holding. Without it the view "genuinely cannot tell knowledge from source", which was a gap
  recorded in the view model and left open.

Remove any one of the three and the decision becomes a silent loss instead of a bound.

## What would change this

A workspace where knowledge is **linked** — where `documents`, `implements` or `refines` edges reach
code in numbers — would make knowledge nodes compete on degree honestly, and this decision would
stop mattering because the budget would already draw them. That is the state the docs graph is meant
to reach. It is not the state today, and the 2026-08-30 decision rules out manufacturing those edges
by inference to get there: matching titles against type names, reading prose for identifiers, or any
"probably relates to" edge, however labelled.

So the trigger is **observed** edges appearing, not a change of mind about the view.
