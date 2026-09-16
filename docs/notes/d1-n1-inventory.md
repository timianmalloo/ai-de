---
id: note-d1-n1-inventory
title: "D-1 N1 inventory — existing store and queries (facts; handshake not frozen)"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, D-1, inventory, N1]
links:
  - { to: note-understanding-views-owner-d1-admission, rel: depends-on }
  - { to: note-d1-codex-entry-point-handshake-r2, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Opened-code inventory for D-1. Does not freeze listing/classification/E1 mapping.
  Handshake r2 remains NOTICE SENT. No IWorkspaceQueries change.
---

# D-1 N1 inventory (facts only)

Opened on `understanding-views-d1` at `main` product pin `bcf4959b`. **Not** a contract freeze.

## What exists

| Fact | Citation | Implication for D-1 |
|---|---|---|
| Graph id is `node_dim.node_id` TEXT | `WorkspaceSchema.cs` 53–62 | Listing rows, if they are graph nodes, use this string domain |
| C# type node id is `INamedTypeSymbol.ToDisplayString()` as assertion subject | `CSharpExtractor.cs` 166–174 `has_type` | Type-level ids already in the store |
| `KindOf(INamedTypeSymbol)` is type-kind only (`class`/`interface`/…) | `CSharpExtractor.cs` 590–597 | **Not** API/UX/CLI/unclassified |
| Members are `has_member` strings on the **type** subject, not their own `node_id` | `CSharpExtractor.cs` 188–201 | Member-grain listing would need new identity minting (specify), not assumed |
| Visibility is `+`/`#`/`-` for class-diagram compartments | `CSharpExtractor.cs` 571–575 | Public is not “API entry point” |
| No `EntryPointsAsync` (or equivalent) on `IWorkspaceQueries` | `IWorkspaceQueries.cs` 20–115 | UV-0 is a new listing method after handshake freeze |
| Neighbourhood: `DescribeAsync` / `GraphAsync` | same file 22, 84 | Select-to-graph can reuse these **after** freeze names the selected id |
| Sequence sketch: `InteractionAsync(nodeId, maxMessages)` cap 200 | `IWorkspaceQueries.cs` 47; `ProjectionService.MaxInteractionMessages` | **Withdrawn** as E1 seam (handshake r2). Legacy type-dependency feed |
| Source: `NodeContentAsync(nodeId)` | ADR-0018; `IWorkspaceQueries.cs` 61 | D-1/D-0 node source; **not** E1 hash-bound occurrence Source |
| D-0 census is path×kind, not entry-point | `SolutionTreeAsync` | Do not reuse census grain for D-1 |

## What does not exist

- API / UX / CLI / unclassified predicates.
- A listing query over those predicates.
- A map from D-1 rows to Codex method-declaration observations (E1 owns that map per r2).
- `entry-points` `SurfaceKind` (AR3).

## Handshake status (not this note)

r2 pin `62670a0a` blob `76e592a3`. NOTICE SENT. Codex five-gates PEER ACK pending. This inventory does **not** freeze listing grain (type vs member).
