---
id: note-understanding-views-owner-d1-mapping-impl
title: "Admit D-1 mapping implementation; 108 land listing on main; do not block listing on the human"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "understanding-views-d1"
tags: [decision-note, owner, D-1, mapper, ruling-108]
links:
  - { to: note-understanding-views-owner-d1-mapper, rel: depends-on }
  - { to: note-d1-codex-entry-point-handshake-r5, rel: relates-to }
  - { to: spec-entry-points, rel: relates-to }
review-by: 2027-03-17
summary: >-
  Operator 2026-09-17: admit mapping-impl; 108 granted; stop blocking keep-going on the human.
  Owner persona + conductor execute. Codex same-blob ACK remains the watcher track, not a human gate.
---

# Mapping-impl admitted; 108 listing; stop over-blocking the operator

**Evidence:** human this turn; r5 freeze blob `a3cb0d63` (authorship only); Codex does not claim the mapper.

## Call

1. **Mapping-implementation contract is admitted.** Producer: Grok. Output: Core method-observation identities when that Core API exists; until it does, Open Sequence stays `mapping-unavailable` (cannot fake E1). Handshake **r6** (does not rewrite r3/r5). Freeze r6 after Codex same-blob ACK (watcher four-state). **Do not ask the human again** for this admit.

2. **Ruling 108 granted** for `understanding-views-d1` onto `main`. Land **listing as it exists** (Sequence still disabled). Do **not** wait for r6 ACK to land listing.

3. **Human is not the default Owner gate.** Keep-going listing, already-granted 108, and admits the operator already spoke are conductor/Owner-persona work. Still not self-cleared: N4 spec review (non-author), live desktop slot, Codex same-blob ACK.

## What Open Sequence still needs (not the human)

r6 Codex ACK + an **admitted Core observation operation** E1 actually implements. r5 forbade claiming that exists today.
