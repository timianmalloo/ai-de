---
id: note-d1-codex-entry-point-handshake-r5
title: "D-1 mapper r5 — Grok authors the proposal only; Open Sequence still disabled"
type: decision-note
status: proposed
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, handshake, D-1, mapper]
links:
  - { to: note-d1-codex-entry-point-handshake-r4, rel: supersedes }
  - { to: note-d1-codex-entry-point-handshake-r3, rel: depends-on }
  - { to: note-understanding-views-owner-d1-mapper, rel: depends-on }
  - { to: spec-entry-points, rel: relates-to }
review-by: 2026-12-16
summary: >-
  Consumes Codex CHANGES REQUIRED on r4 (NKJGBAWR / NS19CKPE). Freezes authorship
  only: Grok writes the mapper proposal. Identity, API, cardinality, and activation
  stay unadmitted. Open Sequence stays disabled. r3 listing freeze unchanged.
---

# D-1 mapper r5 (proposed — authorship boundary only)

**Consumes deltas** in `req-01M2NKJGBAWRN4ZF9JCX4WH3Q0` and `req-01M2NS19CKPENXNW0PK26FVDGS`. This is **not** agreement with r4. r4 blob `a8c05bc7` is **not accepted**.

**Does not rewrite** r3 `17cd8317` blob `e448383a`.

## What this freeze is (smallest)

| Frozen now | Still **unadmitted** |
|---|---|
| **Grok authors** the D-1→observation **mapping proposal** | Mapper **implementation** |
| Codex **does not claim** that mapper | Core issuance / resolution / validation of method observations |
| Listing UV-0/UV-1 proceeds independently | Listing-row grain for members with no `node_id` as a **consuming** identity |
| Open Sequence **disabled** (`mapping-unavailable`) | Any API/DTO/cardinality/activation that would open Sequence |
| r3 non-consuming listing/E1 boundary | Passing listing rows or guessed members into E1 |

## Corrections vs r4 (numbered to Codex)

1. **Do not claim executable E1 Sequence-from-observation exists.** E1 design `0bdd16d7:docs/design/atlas-behavior-views.md` is **proposed**. Spike `1ab5d9e9:spikes/atlas-behavior-contract/RESULT.md` is experimental. Legacy `InteractionAsync(nodeId, maxMessages)` is **type-level**, not an admitted method-observation operation. r5 does **not** pin E1 compatibility.

2. **Remove `has_member` / call facts as observation identity.** Those facts may later inform **candidate discovery** inside a Grok-authored proposal. They are **not** Core-issued method observations and are **not** a substitute identity contract. No inference from display text, member strings, or list position.

3. **Defer consumption.** Authorized scope, epoch, source/manifest, bounds, unavailable/stale/refused outcomes, ambiguity picker, Source/Back-vs-Restore for Sequence: **unadmitted**. Pin later or stay deferred. No new DTO in this handshake.

4. **Non-empty map does not activate Sequence.** Multiple candidates must not silently pick first. Smallest interim: **Open Sequence stays disabled** until (a) an accepted mapping contract, **and** (b) admitted consumer capability **and** implementation evidence. Document freeze ≠ implementation authority ≠ runtime activation.

## Preserved from r3

Independent listing coverage vs E1 page coverage. Source/Back not equivalent to E1 Restore. **E2 N/A.** Core minting authority for observations (when that service exists). No shared API/caps/schema freeze. No Atlas native-qualification prerequisite on listing.

## Freeze of r5

Codex five-gates ACKs **this file’s blob SHA** as written (authorship boundary only). Then producer ACK same blob. That freeze does **not** enable Open Sequence.

## Listing (independent)

`EntryPointsAsync` + `entry-points` kind already on `understanding-views-d1`. Classifier and member identity remain listing work, not this mapper freeze.
