---
id: note-understanding-views-owner-n14
title: "Stop this horizon: D-1…D-4 keep-deferred; no further admission; join stays understanding-views"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, addendum-c, understanding-views, owner, N14]
links:
  - { to: note-understanding-views-owner-ruling, rel: depends-on }
  - { to: note-understanding-views-owner-n1-disposition, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-understanding-views, rel: relates-to }
  - { to: adr-0038-d0-solution-tree-census-and-kind, rel: relates-to }
  - { to: proof-uv-0-solution-tree-core-query, rel: relates-to }
  - { to: proof-uv-1-solution-tree-shell, rel: relates-to }
  - { to: proof-native-ui-solution-tree, rel: relates-to }
  - { to: plan-understanding-views, rel: relates-to }
review-by: 2027-03-14
review-suggested:
  - { by: spec-understanding-views, on: 2026-09-15, reason: "N14 stop; D-1…D-4 remain quoted §A5 deferred; spec stays draft" }
  - { by: plan-understanding-views, on: 2026-09-15, reason: "N14 loop exit; variant 4→0" }
  - { by: note-understanding-views-owner-d1-admission, on: 2026-09-15, reason: "Successor ruling admits D-1; N14 horizon stays closed" }
summary: >-
  N14 stops this horizon. D-1…D-4 are keep-deferred with §A5 admitted-when intact.
  Blast radius: no next view, no D-1 kind, no main; D-0 chrome may finish on
  understanding-views without reopening the N14 loop.
---

# Stop this horizon: D-1…D-4 keep-deferred; no further admission; join stays understanding-views

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** per clause below (Verified | Inferred | Flagged)
- **Made during:** N14 Owner next-view-or-stop, session `understanding-views-owner-n14`, 2026-09-15. Predecessor: `note-understanding-views-owner-ruling` §6.

## Evidence opened (not the Conductor's paraphrase)

- `docs/notes/understanding-views-owner-ruling.md` §6 N14 rule (`:87-95`) and residual (`:158`).
- `docs/specs/addendum-c-perspectives.md` §A5 D-1…D-4 (`:297-313`) and AR3 (`:321-322`).
- `docs/notes/addendum-c-council-rulings.md` Ruling 54 CONDITIONS (`:199-200`).
- `docs/specs/understanding-views.md` frontmatter `status: draft`; D-1…D-6 copied, not paraphrased (`:279-308`).
- `docs/adr/0038-d0-solution-tree-census-and-kind.md` `status: accepted`; glyph chrome named as N8 (`:132`).
- `docs/proof/uv-0-solution-tree-core-query.md`; `docs/proof/uv-0-red-run.txt`.
- `docs/proof/uv-1-solution-tree-shell.md`; `docs/proof/native-ui-solution-tree.md` (not a native PASS).
- `src/AiDe.Core/Projections/IWorkspaceQueries.cs` (`:20-116`) — `SolutionTreeAsync` present; no entry-points method.
- `src/AiDe.Core/Extraction/CSharpExtractor.cs` `KindOf` (`:590-597`).
- `src/AiDe.App/Workbench/SurfaceContentFactory.cs` — `solution-tree` row present (`:179`).
- `docs/plans/understanding-views.md` N14 loop contract (`:162-167`) and horizon done-when (`:34`).

## The call

**No further admission this horizon.** Variant of undisposed §A5 views is 4 (D-1…D-4) and becomes 0.

### 1. Stop — do not admit D-1 (or D-2, D-3, D-4) — **Verified**

Ruling 54 CONDITIONS: *"Revisit the deferral list only when a deferred view has a substrate query it can be built on; an operator request re-admits one at a time, not the set."*

N0: D-1…D-4 remain eligible at N14 *only when a substrate query exists*.

§A5 D-1 admitted-when (quoted, not thinned):

> Given an indexed workspace, When the view opens, Then every API surface, UX surface and CLI entry point in the index is listed as a node with its kind, And selecting one scopes the Architecture graph to the neighbourhood reachable from it, And an entry point the index could not classify is listed under "unclassified", never omitted silently.

Opened substrate:

| Needed for D-1 | Observed |
|---|---|
| Listing query of API / UX / CLI / unclassified | **Absent.** `IWorkspaceQueries` methods are Describe, Impact, Find, SearchContent, Interaction, Knowledge, NodeContent, Evidence, Graph, Paths, Overview, SolutionTree. |
| Classification predicates | **Absent.** `KindOf` emits type-kind (`class`, `interface`, `struct`, `enum`, `delegate`, `record`), not API / UX / CLI / unclassified. |
| Graph neighbourhood on select | **Partial.** `GraphAsync` / `DescribeAsync` already scope a `nodeId`. That is not a listing query and does not create the unclassified bucket. |

Admitting D-1 now would be extractor classification + a new query + a new Architecture kind. That is a new programme, not a walking skeleton on an existing query.

D-2’s admitted-when starts from a **selected entry point**. It cannot lead. D-3’s admitted-when is `spec-uml-erm-surfaces` US-U3 against a fixture schema; no ER kind exists. D-4’s admitted-when is US-U1 at container/component plus bicep provenance; no such kind exists.

### 2. D-1…D-4 = keep-deferred this horizon; do not strike; do not thin §A5 — **Verified**

| View | N0 | N14 |
|---|---|---|
| D-0 Solution/tree | admit | remains the one admitted view |
| D-1 Entry-points | undisposed | **keep-deferred this horizon** |
| D-2 Data flow | undisposed | **keep-deferred this horizon** |
| D-3 ER crow's-foot | undisposed | **keep-deferred this horizon** |
| D-4 Layer/component + bicep | undisposed | **keep-deferred this horizon** |
| D-5 Structure deriver | keep-deferred | unchanged |
| D-6 Conductor round-trips | keep-deferred | unchanged |

Keep D-1…D-4 in the spec with their §A5 admitted-when paragraphs intact. Do not add their kinds. Do not hand-write menu entries. AR3 stands.

### 3. D-0 walking skeleton is the horizon’s delivered view — **Verified** (code and red log)

- ADR-0038 **Accepted**. Spec `understanding-views` stays **draft**.
- UV-0 captured red on the **internal** overload: Failed 19 / Passed 6 / Total 25 (`docs/proof/uv-0-red-run.txt`).
- `solution-tree` kind row is in `SurfaceContentFactory`.
- Join target remains `understanding-views`. `main` is not granted.

### 4. Join target stays `understanding-views`; `main` is not granted — **Verified** (N0 grant unchanged)

### 5. D-0 follow-through does not reopen N14 — **Verified**

These may land on `understanding-views` as D-0 work. They are not a new admitted view:

- N8 kind-glyph chrome.
- Physical Ctrl+Enter / attended Reveal.
- Zone / default layout; N10 design acceptance.
- Join recount if still open.

## Alternatives dismissed

- **Admit D-1 now.** No substrate query; classification predicates absent; Ruling 54.
- **Admit D-3 because `spec-uml-erm-surfaces` exists.** N0 already dismissed this.
- **Keep-defer only D-1 and leave D-2…D-4 undisposed.** Leaves the loop live with no next legal admit.
- **Strike D-1…D-4.** Thins §A5.
- **Thin D-0 admitted-when.** N14 `:95` forbids it.
- **Grant `main`.** N0; Claude conductor still owns `main`.

## What the Conductor may do next

Finish D-0 chrome and join hygiene on `understanding-views` if still open. Close the N14 loop. Do not start D-1.

## What the Conductor must not do

Specify, design, or implement D-1…D-6 this horizon. Add allow-list rows for D-1…D-4. Thin §A5. Author Atlas. Join to `main`. Start another N14 cycle.

## Validation condition

Holds until a later Owner ruling admits exactly one deferred view that has a substrate query it can be built on, without thinning §A5.

## Promotion rule

A later horizon that admits D-1 (or D-3/D-4) writes a new Owner note and one ADR for that kind. This note stays the origin of the horizon stop.
