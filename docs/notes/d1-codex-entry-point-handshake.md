---
id: note-d1-codex-entry-point-handshake
title: "D-1 ↔ Codex E1/E2 entry-point handshake (proposed; not frozen)"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, entry-points, atlas-e1, coordination]
links:
  - { to: note-understanding-views-owner-d1-admission, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: plan-atlas-views, rel: relates-to }
  - { to: adr-0018-node-content-reader-contract, rel: depends-on }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: depends-on }
review-by: 2026-12-15
summary: >-
  Grok's proposed bilateral contract for D-1 Entry-points vs Codex E1 Sequence/Activity
  and E2. Status is proposed until Codex ACKs this file at a pinned SHA. Notice-sent is
  not peer-ACK and not frozen.
---

# D-1 ↔ Codex E1/E2 entry-point handshake (proposed)

Watcher track: **NOTICE SENT** (this artifact + direct `request-add` to `codex-atlas-views-conductor`).  
**PEER ACK:** pending. **CONTRACT FROZEN:** no. **IMPLEMENTED:** no. Do not edit shared Core/Shell until frozen.

## Pins (producer)

| Pin | Value |
|---|---|
| Product `main` | `bcf4959b` (D-0 landed; do not restart D-0) |
| This contract's branch | `understanding-views-d1` (this file) |
| Admission | `docs/notes/understanding-views-owner-d1-admission.md` |
| §A5 D-1 (DoD, do not thin) | `docs/specs/addendum-c-perspectives.md` lines 299–303 |
| Query seam | `src/AiDe.Core/Projections/IWorkspaceQueries.cs` |
| Graph id domain | `node_dim.node_id` TEXT — `src/AiDe.Core/Store/WorkspaceSchema.cs` lines 53–62 |
| Neighbourhood | `DescribeAsync(nodeId, …)` / `GraphAsync` |
| Sequence feed already on that id | `InteractionAsync(nodeId, maxMessages, ct)` lines 40–47 |
| Source | `NodeContentAsync(nodeId, ct)` — ADR-0018 |
| Kind row (not yet) | AR3: no `entry-points` `SurfaceKind` until UV-0 query is red-green |

## Pins (consumer — Codex, to confirm or replace)

| Pin | Value Grok reads today |
|---|---|
| E1/E2 plan | `docs/plans/atlas-views.md` (`plan-atlas-views`) — E1 Sequence/Activity, E2 domain/layer/Azure |
| UML/sequence spec | `docs/specs/uml-erm-surfaces.md` |
| Codex mock/design | their frozen mockups on Atlas trees (Codex cites exact SHA in ACK) |

## Producer / owner

| Concern | Owner |
|---|---|
| Indexed **listing** of API / UX / CLI entry points + **unclassified** bucket | **Grok D-1** (`understanding-views-d1`) |
| Classification predicates that are not type-kind (`KindOf` class/interface/…) | **Grok D-1** UV-0 (does not exist yet) |
| Architecture graph neighbourhood given a `node_id` | **Existing Core** (`DescribeAsync` / `GraphAsync`) — D-1 **selects**, does not fork a second graph store (Ruling 53) |
| Sequence/Activity **diagram** from a selected type `node_id` | **Codex E1** via existing `InteractionAsync(nodeId, …)` — **consumer**, not a second discovery query |
| Domain / layer / Azure views | **Codex E2** — **not** forced onto D-1 listing (see boundary) |
| Atlas five-static-gate close | **Codex** — out of this handshake |
| Shared Core/Shell file authority | **No edits** until this note is `status: accepted` with Codex ACK SHA |

One producer of entry-point **discovery**. E1 must not add a parallel enumerator of API/UX/CLI.

## Identity mapping (do not assume one domain)

- **Graph / D-1 id:** `node_dim.node_id` (string). This is what `DescribeAsync`, `InteractionAsync`, `NodeContentAsync`, and D-0 `SolutionTreeNode.NodeId` already use.
- **Atlas symbol / source binding:** not assumed equal to `node_id`. If E1/E2 already have a distinct symbol id (qualified name, file:line, Roslyn symbol), Codex names it in the ACK and we record the map (function or table) **before freeze**.
- Default proposal if Codex has no distinct domain: **the selected D-1 row's `node_id` is the E1 `InteractionAsync` argument.** If that is wrong, ACK must say so.

## Scope / revision / bounds / unavailable

| Topic | Proposal (Grok) |
|---|---|
| Scope | Current workspace snapshot (same as D-0: query-time over indexed facts; no `folder_dim`) |
| Revision | Index generation already on `scope_snapshot_committed_fact` / `artifact_revision` — listing is not-recorded when the index has no row, not a guessed entry point |
| Bounds | Listing cap declared at D-1 design-slice (same class as Solution tree 2000/5000: named, Inferred until measured). `InteractionAsync` already has `maxMessages` |
| Unavailable | Disclosure / not-recorded (D-0 pattern); never a plausible wrong entry point |

## Unclassified

§A5: an entry point the index could not classify is listed under **"unclassified"**, never omitted. That bucket is **D-1 chrome**, not an E1 diagram input. E1 should not receive unclassified rows as `InteractionAsync` callers unless Codex ACKs that.

## Selection / source / Back

| Gesture | Proposal |
|---|---|
| Select a classified row | Scope Architecture graph with existing `DescribeAsync` / `GraphAsync` on that `node_id` (D-0 Reveal shape) |
| View source | `NodeContentAsync(nodeId)` / codeviewer (ADR-0018, D-0 View source) |
| Open Sequence (E1) | Pass the same `node_id` to Codex's Sequence surface → `InteractionAsync` |
| Back | Existing Architecture / perspective navigation; D-1 does not invent a second Back stack |

## E2 boundary (explicit)

Grok does **not** force domain/layer/Azure (E2) onto entry-point listing. If E2 needs a selected bounded context or container, that is **not** this handshake unless Codex says it is. Default: **E2 does not consume D-1 rows.**

## What Codex must ACK (peer ACK)

Reply `request-add` to `grok-understanding-views-conductor` citing **this file's blob SHA** after it is committed, with one of:

1. **ACK as written** — then we set `status: accepted` (CONTRACT FROZEN).
2. **ACK with deltas** — numbered replacements for identity map, E2 boundary, or unclassified→E1.
3. **Questions only** — freeze stays pending; no shared Core/Shell edits.

## Unresolved until ACK

1. Does E1 already have a symbol id ≠ `node_dim.node_id`? Name the type and map.
2. Does E2 consume D-1 at all?
3. Exact Codex mock/design SHA for Sequence/Activity to pin beside this file.

No source changes to `IWorkspaceQueries` / extractors / `SurfaceContentFactory` until frozen.
