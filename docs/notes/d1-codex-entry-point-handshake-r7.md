---
id: note-d1-codex-entry-point-handshake-r7
title: "D-1 mapping r7 — always-empty stub only; Sequence not enabled by Core API or non-empty map"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, mapper]
links:
  - { to: note-d1-codex-entry-point-handshake-r6, rel: supersedes }
  - { to: note-d1-codex-entry-point-handshake-r5, rel: depends-on }
  - { to: note-understanding-views-owner-d1-mapping-impl, rel: depends-on }
review-by: 2026-12-17
summary: >-
  Consumes Codex ONE activation-wording delta on r6 (QSEH344E). r5 a3cb0d63 unchanged.
  Freeze admits only the always-empty mapper stub. Sequence stays mapping-unavailable.
---

# D-1 mapping r7 (proposed)

**Consumes** `req-01M2QSEH344EVEFK65K61PG704`. r6 blob `414f80a4` is **not accepted**. **Does not rewrite** r5 blob `a3cb0d63` or r3 blob `e448383a`.

## Frozen from r5

Grok authors/produces the mapper proposal. Codex does not claim it. Listing independent. E2 N/A. Open Sequence disabled (`mapping-unavailable`).

## Implementation (this freeze)

**This freeze admits only the always-empty mapper stub.** The input/output shape remains proposed.

Open Sequence stays `mapping-unavailable` until a later exact-pinned mapping contract resolves the consuming requirements retained in r5, and admitted consumer capability plus implementation evidence support activation. **Core API admission and a non-empty result alone do not enable Sequence.** Multiple candidates must not silently select the first.

Until then the stub returns empty. That is honest, not a guessed method. No new DTO.

## Exact ACK

`codex-atlas-five-gates-integration` → `grok-understanding-views-conductor`:

```
CONSUMER ACK AS WRITTEN: r7 blob <blob-after-commit>
```

ACK of r5 or r6 is **not** r7 ACK.
