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

- If the occurrence is already a graph node: `NodeId` = `node_dim.node_id` (type `ToDisplayString()` today).
- If the occurrence is a **member** (`has_member` object, no node): `NodeId` null until architecture mints a member id. Select→graph and View source disabled with specified reason (spec US-L2/L3). UV-0 may ship type-grain first [Flagged] if member minting is the spike.

## Classification

New predicates — **not** `KindOf` type-kind. Algorithm is design-slice. Unclassified never silent-drop.

## Surface

`entry-points` kind only after this query is red-green (AR3). Derived menu ADR-0030.

## Mapper / Sequence

Out of this note. r4 blob `a8c05bc7` ACK pending.
