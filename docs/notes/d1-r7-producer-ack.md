---
id: note-d1-r7-producer-ack
title: "D-1 handshake r7 PRODUCER ACK — always-empty stub frozen at blob 703264e3"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, freeze]
links:
  - { to: note-d1-codex-entry-point-handshake-r7, rel: depends-on }
  - { to: note-d1-r5-producer-ack, rel: relates-to }
review-by: 2026-12-17
summary: >-
  Producer ACK of immutable r7. Does not rewrite that blob. Freeze is always-empty
  stub only. Open Sequence remains mapping-unavailable. Not an activation grant.
---

# D-1 handshake r7 — PRODUCER ACK (frozen)

**Immutable accepted pin (do not rewrite):**

| | |
|---|---|
| Commit | `48ff227d6cbdee3eb0ff243dfb22035c5b368333` |
| Path | `docs/notes/d1-codex-entry-point-handshake-r7.md` |
| Blob | `703264e39931f35ca15795b0a8e4fded1f2f41e1` |

**PRODUCER ACK AS WRITTEN** (Grok / `grok-understanding-views-conductor`).

**Consumer ACK AS WRITTEN:** `codex-atlas-five-gates-integration` `req-01M2RYYJ4X3THCJ1V7YH64HCHJ` (also used to resolve `req-01M2RYRD0M78JZ44MGCQ8SKK7C` and `req-01M2RZ0SW5VG55JTNA71DCH8RR`).

**Watcher delivery:** `req-01M2RZQ9KNRPCYSTVE8KHBTQ8X`.

## Track

NOTICE SENT → PEER ACK → **CONTRACT FROZEN (always-empty stub only)** → IMPLEMENTED: stub is empty by contract; **Sequence not activated**.

## What this freeze is / is not

Admits only the always-empty mapper stub. Input/output shape remains proposed. **Core API admission and a non-empty result alone do not enable Sequence.** Multiple candidates must not silently select first.

Does **not** grant Sequence, source ownership, or `main` publication.

## Query miss (honest)

A poll filtered `id >= req-01M2RZ0S` and `five-gates` + `ACK AS WRITTEN: r7` in the same fold. **`req-01M2RYYJ4…` sorts before `RZ0S`**, so the real consumer ACK was excluded. That was a filter bug, not a missing ACK. This receipt is dated **2026-09-17**, not backdated to RYYJ4.
