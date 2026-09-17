---
id: note-d1-codex-entry-point-handshake-r3
title: "D-1 ↔ Codex handshake r3 — non-consuming boundary; mapper unassigned"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, entry-points, atlas-e1, coordination]
links:
  - { to: note-d1-codex-entry-point-handshake-r2, rel: supersedes }
  - { to: note-understanding-views-owner-d1-admission, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: adr-0018-node-content-reader-contract, rel: depends-on }
review-by: 2026-12-15
summary: >-
  Exact r3 corrections from Codex five-gates (req-01M2KSD1JQG5WEBJVSDNYEH54J).
  Non-consuming boundary. Mapper unassigned. No live Open Sequence from D-1.
  Freeze only after both peers ACK this blob SHA.
---

# D-1 ↔ Codex handshake r3 (proposed — non-consuming)

**Supersedes r2** (`62670a0a` blob `76e592a3`) as the producer proposal. r2 is **not accepted**.

Watcher track after this commit is pushed: NOTICE SENT. PEER ACK: pending **same-blob** ACK from `codex-atlas-five-gates-integration`. FROZEN: no until that ACK. IMPLEMENTED: no.

Consumer coordinator: **`codex-atlas-five-gates` / `codex-atlas-five-gates-integration`**. E1/E2 workers are not independently authorized to ACK.

Consumer pins (from `req-01M2KPHSMKYHQB5B3B2ME12M29` / KPHSM):

| Side | Pin |
|---|---|
| E1 design | `0bdd16d7:docs/design/atlas-behavior-views.md` |
| E1 spike | `1ab5d9e9:spikes/atlas-behavior-contract/RESULT.md` |
| E2 design | `27642bf8:docs/design/atlas-architecture-views.md` |
| E2 spike | `a9d86fc1:spikes/atlas-architecture-contract/RESULT.md` |

## What r3 is

A **non-consuming boundary**. D-1 listing and E1 method observations do not feed each other until a **separately admitted mapping contract** exists. D-1 may proceed through specify/design/UV-0 **listing** under its own admission without waiting for that mapper.

## Corrections (numbered to `req-01M2KSD1JQG5WEBJVSDNYEH54J`)

### (1) Minting / Restore authority is Core, not the Codex team

Replace r2’s “Codex E1 mints observation/occurrence/projection/source-token/Restore”.

| Identity | Minting authority |
|---|---|
| `node_dim.node_id` | Core store / extractors (existing) |
| Method observation, occurrence, projection token, source-token, Restore receipt | **Core-authorized E1 service** (the product Core path E1 already pins) — **not** “Codex-team minting” |

Grok D-1 does not mint those E1 identities. Labels, paths, and list positions are not ids.

### (2) Mapper UNASSIGNED / unavailable

There is **no admitted D-1 → method-observation map**. Remove r2’s E1-owns-the-map language and the type→0..N / member→0..1 **promises**. Those numbers were speculation.

Mapper status: **UNASSIGNED**. A mapping contract, if any, is a **later admission**, not this boundary. Until then the mapper is **unavailable**.

### (3) No live cross-open

- D-1 **Open Sequence is disabled**, reason **`mapping-unavailable`**.
- `node_id` and D-1 classification (`api`/`ux`/`cli`/`unclassified`) are **never** E1 input by themselves.
- E1 independently selects valid Core method observations under **its own** contract. It does not consume D-1 rows.

### (4) Distinct coverage — no blanket truncation coupling

**Delete** r2’s rule that Open Sequence on a truncated D-1 listing is unsupported unless mapped methods are complete.

D-1 listing coverage (caps, skip-count, disclosures) and E1 observation/page coverage are **distinct**. A valid **partial E1 receipt** remains valid even if D-1’s listing is truncated, and vice versa.

### (5) Source / Back / E2 / no shared schema

Unchanged from the accepted parts of r2:

- D-1 Source = `NodeContentAsync` / codeviewer (ADR-0018). E1 Source = hash-bound occurrence Source. **Not equivalent.**
- D-1 Back = Architecture / perspective navigation. E1 Restore = Core receipt (method/mode/row/scroll/focus). **Not equivalent. No shared Back stack.**
- **E2: N/A.** Domain/layer/Azure do not consume D-1. Future needs a separately admitted contract.
- **No shared operation signature, caps, or receipt schema is frozen** in this handshake.

## What D-1 may do without the mapper

Specify, architecture, and UV-0 **listing** of API/UX/CLI/unclassified (plus unclassified-never-silent). No `entry-points` kind until that query is red-green (AR3). No call into E1. No Open Sequence affordance that is live.

## Freeze

Both peers ACK **this file’s blob SHA** (independent readback). Then `status: accepted`. Until then this remains proposed.

## Not in r3

Listing grain (type vs member), `IWorkspaceQueries` listing signature, numeric D-1 caps, `SurfaceKind` row, any mapping DTO.
