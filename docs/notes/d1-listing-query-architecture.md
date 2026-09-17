---
id: note-d1-listing-query-architecture
title: "D-1 listing query architecture (draft) — EntryPointsAsync; identity minting"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, D-1, architecture]
links:
  - { to: spec-entry-points, rel: depends-on }
  - { to: note-d1-n1-inventory, rel: relates-to }
  - { to: note-d1-codex-entry-point-handshake-r3, rel: depends-on }
review-by: 2026-12-16
summary: >-
  Listing query lives on IWorkspaceQueries. Grain: one row per candidate occurrence.
  Member identity is not node_id today. Open Sequence is not this query.
---

# D-1 listing query (architecture draft)

Not an ADR yet. Does not implement source. Does not enable Open Sequence (r4 unfrozen).

## Seam

New method on `IWorkspaceQueries` (same process/daemon split as `SolutionTreeAsync`):

`Task<EntryPointsResult> EntryPointsAsync(EntryPointsQuery query, CancellationToken cancellationToken)`

Query: integer cap(s) only on the wire (D-0 lesson: no drop-set on the public seam). Named omit stays internal.

Result: rows `{ Kind: api|ux|cli|unclassified, NodeId: string?, Display, UnclassifiedReason? }` plus disclosures/cap omitted counts. **No E1 observation ids on this DTO** (r3).

## Identity

- Type row: `NodeId` = that type's `node_dim.node_id`.
- Member row: `NodeId` = **declaring type** id (graph/source = type neighbourhood). Display = `{typeId}.{has_member object}` (extractor shape e.g. `+ Main()`). Row identity ≠ `NodeId`. No minted member node this slice.

## Classification

New predicates — **not** `KindOf` type-kind. Algorithm is design-slice. Unclassified never silent-drop.

## Surface

`entry-points` kind only after this query is red-green (AR3). Derived menu ADR-0030.

## Mapper / Sequence

Out of this note. r4 blob `a8c05bc7` ACK pending.
