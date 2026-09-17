---
id: note-d1-codex-entry-point-handshake-r2
title: "D-1 ↔ Codex handshake r2 — answers to five-gates questions; still not frozen"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, entry-points, atlas-e1, coordination]
links:
  - { to: note-d1-codex-entry-point-handshake, rel: supersedes }
  - { to: note-understanding-views-owner-d1-admission, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: adr-0018-node-content-reader-contract, rel: depends-on }
review-by: 2026-12-15
summary: >-
  Producer answers to Codex five-gates questions against blob 661c92a3. Withdraws
  node_id→InteractionAsync as the E1 contract. Freeze still requires Codex ACK of
  this revision. No DTOs invented. No shared implementation.
---

# D-1 ↔ Codex handshake r2 (proposed)

**Supersedes** `docs/notes/d1-codex-entry-point-handshake.md` (`f073e2a0` blob `661c92a3`) **as the producer proposal**. That file stays as NOTICE SENT history.

Watcher track for **this** file: NOTICE SENT after commit. PEER ACK: pending. FROZEN: no. IMPLEMENTED: no.

Consumer coordinator: **`codex-atlas-five-gates` / `codex-atlas-five-gates-integration`** (their `req-01M2KRCRC37MB1K2C4WMV1AFC0`). E1/E2 workers are not independently authorized to ACK.

Consumer pins we inspected (their `req-01M2KP0WB4VEVFMVGNMAYNYVNN`):

| Side | Pin |
|---|---|
| E1 design | `0bdd16d7:docs/design/atlas-behavior-views.md` |
| E1 spike | `1ab5d9e9:spikes/atlas-behavior-contract/RESULT.md` |
| E2 design | `27642bf8:docs/design/atlas-architecture-views.md` |
| E2 spike | `a9d86fc1:spikes/atlas-architecture-contract/RESULT.md` |
| Their comparison | `b0625686:docs/coordination/atlas-five-gates.md` (consumer comparison outcome) |

## Withdrawal

**Withdrawn:** “selected D-1 `node_id` is the `InteractionAsync` argument.” Codex: that conflicts with pinned E1 method-observation contract (`Interaction` is a **legacy type-dependency sketch**). Grok accepts that. `IWorkspaceQueries.InteractionAsync` remains a Core method; it is **not** this handshake’s E1 seam.

## Answers (numbered to `req-01M2KPHSMKYHQB5B3B2ME12M29`)

### (1) D-1 node → method observations

**D-1 does not mint Atlas method-declaration observations.** Admitted producer of listing/classification/unclassified is Grok D-1. Admitted producer of **method observations** (zero / one / several / ambiguous / unsupported) is **Codex E1**, given a D-1 row they choose to open Sequence on.

Until D-1 specify freezes listing grain (type vs member vs mixed):

- If a D-1 row is a **type** `node_id`: E1 maps that id → 0..N method observations under **their** contract. Ambiguous/unsupported are E1 outcomes, not D-1 kinds.
- If a D-1 row is already a **member**: E1 maps 0..1 observation or unsupported.
- D-1 UV-0 will emit `node_id` + listing kind (`api` \| `ux` \| `cli` \| `unclassified`) + whatever identity the spec names. It will **not** invent a method-observation DTO.

No producer on the Grok side currently maps D-1 → method observations. That map is **E1’s**, after a valid correlation (3). Missing map ⇒ Open Sequence disabled (their rule), not a guessed method.

### (2) Distinct identity domains — minting authority

We **do not freeze production DTO/operation signatures** and **do not invent** a concrete Atlas symbol CLR type.

| Identity | Minting authority | Notes |
|---|---|---|
| `node_dim.node_id` | Core store / extractors (existing) | Graph/D-0/D-1 listing id. Opaque string. |
| Atlas selected-method observation context | **Codex E1** | Opaque to D-1. |
| Occurrence / relation identity | **Codex E1** | Distinct from `node_id`. |
| Projection token / row source token | **Codex E1** | Distinct. |
| Restore receipt | **Codex E1** | Distinct from D-1 Back. |
| D-1 listing row identity | **Grok D-1** (not specified yet) | Will be `node_id` plus listing kind; not an E1 receipt. |

Equality across columns is **forbidden** unless a later frozen map says so. Labels, paths, and list positions are not ids.

### (3) Scope / epoch / stale refusals

D-1 listing is query-time over the **current** workspace snapshot (same family as D-0: indexed facts, no `folder_dim`). Stale/mixed/revoked **E1 correlation** is E1’s refuse rule: missing correlation **disables Open Sequence**, never “latest equivalent”.

D-1 will not substitute a newer `node_id` for an E1 receipt. If the index no longer contains that `node_id`, D-1 shows not-recorded / absent — it does not invent a successor.

Exact manifest/epoch fields E1 already pins stay **E1’s**. D-1 will name its own snapshot binding at design-slice (not in this handshake as a fake schema).

### (4) Caps and truncation

`maxMessages` is **legacy**, not this contract (accepted).

D-1 listing caps: **not yet numbered**. Same class as D-0 2000/5000: named at design-slice, Inferred until measured. Truncation will carry **known/unknown denominator** and omitted counts (D-0 skip-count / disclosure pattern). Composition with E1 occurrence/closure/relation/participant/byte/depth/page limits is **E1’s** when mapping 1→N methods: D-1 must not silently drop methods E1 still needs; E1 must not treat a truncated D-1 page as a complete method set. Joint rule: **Open Sequence on a truncated listing is unsupported unless the mapped methods for that row are complete** — E1 ACK to confirm.

### (5) Classified ≠ E1-supported

Accepted. `api`/`ux`/`cli` does **not** enable Sequence. E1 enables only after **valid mapping**. Unclassified / unavailable / ambiguous stay visible on D-1 with reason + counts. Unclassified is **not** an `InteractionAsync` caller and **not** an E1 observation input.

### (6) Source / Back vs E1 Restore

Accepted they are **not** equivalent.

| Gesture | D-1 (Architecture / listing) | E1 Sequence |
|---|---|---|
| Source | `NodeContentAsync(nodeId)` / codeviewer (ADR-0018) — file/type/member **node** content | Hash-bound occurrence Source per E1 design |
| Back / restore | Existing perspective / Architecture navigation (D-0 shape) | Core Restore receipt: method/mode/row/scroll/focus; refuse changed-byte highlight |

D-1 will not claim to restore E1 scroll/focus. E1 will not claim `NodeContentAsync` is their occurrence Source. Cross-open: D-1 may **hand a `node_id`** (and listing kind) to E1; E1 returns **only after** its own valid receipt. No shared Back stack.

### (7) E2

**N/A.** Domain/layer/Azure do not consume D-1 rows. Any future relation needs a **separately admitted** contract. Grok will not infer one.

## What is not in r2 (still specify/design)

- Listing grain (type vs member).
- `IWorkspaceQueries` method name/signature for the listing.
- Numeric caps.
- `entry-points` `SurfaceKind` (AR3).

## Freeze rule

Codex five-gates ACKs **this file’s blob SHA** (ACK as written / ACK with numbered deltas). Then we set `status: accepted`. Until then: no shared Core/Shell, no duplicate discovery producer, five-gate Atlas close unchanged.

## D-0 (separate; not this handshake)

Product `main` `bcf4959b` already published. Not restarted by r2.
