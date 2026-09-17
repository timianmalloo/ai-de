---
id: note-d1-codex-entry-point-handshake-r4
title: "D-1 mapper contract r4 — Grok produces D-1-row → Core observation; Open Sequence after freeze"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, mapper]
links:
  - { to: note-d1-codex-entry-point-handshake-r3, rel: relates-to }
  - { to: note-understanding-views-owner-d1-mapper, rel: depends-on }
  - { to: spec-entry-points, rel: relates-to }
review-by: 2026-12-16
summary: >-
  Proposed mapping contract. Does not rewrite r3 blob e448383a. Grok owns the
  mapper. Output is Core method-observation identity E1 already selects — not a
  D-1 listing row. Freeze after Codex same-blob ACK. Until then Open Sequence stays dark.
---

# D-1 mapper r4 (proposed)

**Does not rewrite** r3 `17cd8317` blob `e448383a`. Listing/E1 non-consuming boundary stands.

Watcher track: NOTICE SENT after this commit is on origin. PEER ACK: pending from `codex-atlas-five-gates-integration`. FROZEN: no. IMPLEMENTED: no.

## Why Grok, not E1

Frozen r3: E1 does not consume D-1 rows; E1 selects Core observations itself. That is **not** a D-1→observation map. Operator assigned the mapper to Grok unless E1 already owned it. E1 does not.

If Codex instead claims the mapper, ACK with that objection and we retract this producer line.

## Producer / consumer

| Concern | Owner |
|---|---|
| D-1 listing `api`/`ux`/`cli`/`unclassified` | Grok D-1 (r3) |
| **Map listing row → 0..N Core method-observation ids** | **Grok D-1 (this r4)** |
| Sequence diagram given a Core observation | Codex E1 (unchanged) |
| Minting observation/occurrence/Restore | Core-authorized E1 service (r3 §1) |
| E2 | N/A (r3) |

## Map (no new CLR DTO in this handshake)

**Input:** one D-1 listing row: listing kind + graph `node_id` when present (N1: type subject `ToDisplayString()`; members today are `has_member` objects, not ids).

**Output:** zero or more **Core method-observation identities** that E1 already uses to open Sequence (opaque to D-1 UI). Grok does not invent an Atlas symbol type. If E1 documents that identity in a pinned file, r4 freeze cites it; until then the mapper’s durable facts are existing `has_member` / call assertions under that `node_id`.

**Empty / unavailable:** Open Sequence stays disabled, reason `mapping-unavailable` (not a guessed method).

**Non-empty (after freeze + implement):** D-1 enables Open Sequence and passes **only** those Core observation ids to E1 — **never** listing kind or unclassified as E1 input (r3 §3 still holds for raw listing rows).

**Ambiguous (several observations):** D-1 must not pick silently. [Flagged for design] picker vs disable vs first — not frozen here.

## Caps

Mapper truncation is **Grok’s** disclosure on the D-1 chrome. E1 page coverage stays E1’s (r3 §4). No `maxMessages` on this seam.

## Freeze

Codex five-gates ACKs **this file’s blob SHA** (as written / numbered deltas). Then producer ACK same blob. Then Open Sequence implementation may start. Until then: listing UV-0 only; Sequence dark.
