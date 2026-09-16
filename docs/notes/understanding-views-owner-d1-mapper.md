---
id: note-understanding-views-owner-d1-mapper
title: "Admit D-1 mapper to Grok; Codex E1 does not own D-1→observation mapping"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, owner, D-1, mapper]
links:
  - { to: note-understanding-views-owner-d1-admission, rel: depends-on }
  - { to: note-d1-codex-entry-point-handshake-r3, rel: relates-to }
  - { to: note-d1-r3-producer-ack, rel: relates-to }
  - { to: spec-entry-points, rel: relates-to }
review-by: 2027-03-16
summary: >-
  Operator: claim the mapper unless Codex E1 already does it. E1 does not: r3
  freeze forbids E1 consuming D-1 rows. Grok is producer of the mapping contract.
  Live Open Sequence stays dark until that contract is peer-ACK frozen (r4).
---

# Admit D-1 mapper to Grok

**Evidence opened**

- Frozen r3 blob `e448383a`: mapper UNASSIGNED; E1 does not consume D-1 rows; Open Sequence disabled `mapping-unavailable`.
- Codex consumer ACK of that blob (`req-01M2NFA6NQKFHN178A9P0JFE2P`).
- Operator 2026-09-16: claim the mapper unless it is being done by Codex as part of E1.

**Call**

Codex E1 is **not** the D-1→method-observation mapper. E1 independently selects Core method observations under its own contract. That is a different job.

**Grok D-1 is producer** of the mapping contract (later handshake r4). Codex remains consumer of **Core observations** when Sequence opens — not consumer of listing rows.

This does **not** rewrite r3. r3 stays the non-consuming listing/E1 boundary. The mapper is a **new** admitted contract.

Live Open Sequence stays disabled until r4 is frozen (notice → Codex ACK → freeze). Listing specify/architecture/UV-0 does not wait.
