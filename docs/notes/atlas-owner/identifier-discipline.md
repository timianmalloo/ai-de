---
id: note-atlas-identifier-discipline
title: "Atlas decision titles precede collision-checked identifier allocation"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-framing"
tags: [code-atlas, owner-ruling, identifiers]
links:
  - { to: note-atlas-next-addendum, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Record named Owner decisions now; allocate any numeric ruling, ADR or defect identifier only
  through the repository's current allocator/checks. No silent sequence guessing.
---
# Atlas decision titles precede collision-checked identifier allocation

**Decision source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, 2026-09-12.

**Ruling:** use the supplied note titles and let the Conductor allocate identifiers after collision checks.
**Because:** the register is explicit about cross-session collisions and append-only audit writers;
the Owner did not establish a current global allocation state.

**Conditions:** file each named note and append an audit entry through `audit-log.py`.
Registration, carve-out acknowledgment and integration-seam resolution stay outstanding until
observed. Do not guess a global ruling/ADR/DC number or hand-edit an audit record.
The local framing notes use unique semantic IDs, not a parallel numeric ruling sequence.
