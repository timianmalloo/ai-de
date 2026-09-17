---
id: note-understanding-views-owner-d1-admission
title: "Admit D-1 Entry-points this cycle; D-2…D-4 stay keep-deferred; first slice is the substrate query"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, addendum-c, understanding-views, owner, D-1, entry-points]
links:
  - { to: note-understanding-views-owner-n14, rel: depends-on }
  - { to: note-understanding-views-owner-ruling, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-understanding-views, rel: relates-to }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: depends-on }
  - { to: plan-understanding-views, rel: relates-to }
review-by: 2027-03-15
review-suggested:
  - { by: spec-understanding-views, on: 2026-09-15, reason: "D-1 admitted; §A5 admitted-when is now this cycle's DoD" }
  - { by: plan-understanding-views, on: 2026-09-15, reason: "New N14-successor cycle; one view in flight" }
summary: >-
  Operator re-admits D-1 Entry-points only. D-2…D-4 remain keep-deferred. The listing
  query and API/UX/CLI/unclassified classification do not exist yet — UV-0 is that
  substrate; the Architecture kind is added only in the slice that builds it (AR3).
---

# Admit D-1 Entry-points this cycle; D-2…D-4 stay keep-deferred; first slice is the substrate query

*A decision note (`knowledge-visualization.md` V17). One note per call.*

- **Kind:** decision
- **Confidence:** per clause
- **Made during:** operator request "re-admit D-1 now", session `grok-understanding-views-conductor`, 2026-09-15. Predecessor: `note-understanding-views-owner-n14` (horizon stop).

## Evidence opened (not recalled)

- `docs/notes/understanding-views-owner-n14.md` — D-1 keep-deferred; no listing query; `KindOf` is type-kind.
- `docs/specs/addendum-c-perspectives.md` §A5 D-1 admitted-when (`:299-303`) and AR3 (`:321-322`).
- `docs/notes/addendum-c-council-rulings.md` Ruling 54 CONDITIONS: operator re-admits **one at a time**; revisit when a substrate query exists.
- `src/AiDe.Core/Projections/IWorkspaceQueries.cs` — `SolutionTreeAsync` present; no entry-points method (re-opened on `main` `bcf4959b`).
- `src/AiDe.App/Workbench/SurfaceContentFactory.cs` — `solution-tree` row; no entry-points kind.

## The call

The N14 horizon is **closed**. This is a **new** admission cycle. The operator request is in. It admits **D-1 only**. It does not re-open D-2…D-4.

### 1. Admit D-1 Entry-points — **Verified** (operator)

§A5 D-1 (quoted):

> Given an indexed workspace, When the view opens, Then every API surface, UX surface and CLI entry point in the index is listed as a node with its kind, And selecting one scopes the Architecture graph to the neighbourhood reachable from it, And an entry point the index could not classify is listed under "unclassified", never omitted silently.

That paragraph is this cycle's DoD. Do not thin it.

### 2. Substrate is absent — UV-0 is the query, not a kind on nothing — **Verified**

N14's table still holds on `bcf4959b`:

| Needed | Observed |
|---|---|
| Listing query of API / UX / CLI / unclassified | Absent |
| Classification predicates | `KindOf` is type-kind, not entry-point kind |
| Graph neighbourhood on select | `GraphAsync` / `DescribeAsync` exist; they are not a listing query |

Ruling 54's "when a substrate query exists" is **not** met. The operator still re-admits the view. The resolution: **the first slice creates the substrate** (classification + `IWorkspaceQueries` listing). The `SurfaceKind` row and derived menu pickup land only in the shell slice after that query is red-green (AR3). Do not add an `entry-points` kind in this ruling.

### 3. D-2…D-4 stay keep-deferred; D-5/D-6 unchanged — **Verified**

D-2's admitted-when starts from a **selected entry point**. It cannot lead. D-3/D-4 unchanged. Do not strike. Do not thin §A5.

### 4. Join and coordination — **Verified** (this machine)

- Branch: `understanding-views-d1` from `main` `bcf4959b`.
- Join target: this branch, then Ruling 108 onto `main` only with GHCP watcher intent (same as D-0).
- Atlas stays Codex. No Atlas `Understanding/*` authorship.
- Desktop: announce PID START before any shown-window/UIA run.

### 5. Spine (this cycle) — **Inferred** (same graph as D-0)

specify → architecture (query contract + grain) → spike if the classification contract is unfamiliar → design-slice → implement UV-0 query (red first) → UV-1 kind row. ADR for the query. Proof Pack. Authors do not self-clear N10.

## Alternatives dismissed

- **Admit D-1…D-4 as a set.** Ruling 54.
- **Add the kind now with a fake query.** AR3.
- **Treat `GraphAsync` as the listing.** It does not enumerate API/UX/CLI/unclassified.
- **Start D-2 in parallel.** Depends on a selected D-1 node.

## Variant

Undisposed §A5 views this ruling disposes: D-1 = **admit**. Already keep-deferred: D-2, D-3, D-4, D-5, D-6. Floor is not reopened for the set.
