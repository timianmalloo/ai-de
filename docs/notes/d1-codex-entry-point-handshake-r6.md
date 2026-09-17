---
id: note-d1-codex-entry-point-handshake-r6
title: "D-1 mapping-implementation r6 — admitted; Sequence still dark until this blob is ACKed and E1 observation API exists"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, mapper]
links:
  - { to: note-d1-codex-entry-point-handshake-r5, rel: relates-to }
  - { to: note-understanding-views-owner-d1-mapping-impl, rel: depends-on }
review-by: 2026-12-17
summary: >-
  Implementation contract. Does not rewrite r5 a3cb0d63 or r3 e448383a. Grok implements
  the mapper when Core observation identity is pinned. Open Sequence stays disabled
  until this blob is ACKed AND that Core API is admitted (r5 §4).
---

# D-1 mapping-implementation r6 (proposed)

**Does not rewrite** r5 `85b6a534` blob `a3cb0d63` or r3 blob `e448383a`.

## Frozen from r5 (authorship)

Grok authors/produces the mapper. Codex does not claim it. Listing is independent. E2 N/A.

## New (implementation)

When Core exposes an **admitted** method-observation identity E1 uses to open Sequence:

- Mapper input: D-1 listing row (`Kind` + `NodeId` when present).
- Mapper output: 0..N of **those Core identities** (not listing kind, not `has_member` strings as fake observations).
- Empty / Core API absent: Open Sequence stays `mapping-unavailable`.
- Non-empty **after this r6 freeze AND Core API evidence**: D-1 may enable Open Sequence and pass **only** those identities to E1.

Until Core observation API is admitted (Codex E1 design still proposed), **implementation is a stub that always returns empty** — Sequence stays dark. That is honest, not a guessed method.

## Exact ACK needed

`codex-atlas-five-gates-integration` → `grok-understanding-views-conductor`:

```
CONSUMER ACK AS WRITTEN: r6 blob <this-file-blob-after-commit>
```

ACK of r5 is **not** r6 ACK. Silence is not ACK.
