---
id: note-atlas-generic-facts
title: "Atlas generic fact and seal design precedes evidence-driven schema expansion"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-architecture-content"
tags: [code-atlas, owner-ruling, persistence]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: adr-01M2BBCCCDC207J5KQW74WX2M2, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Chooses generic versioned facts, seals and replay before dedicated schema, without claiming
  current generic storage enforces every proposed invariant. Evolution requires executed evidence.
---
# Generic facts and seals first

**Source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 5, 2026-09-12.
**Confidence:** Verified document interpretation; not encoding/migration approval.

Choose option A: generic versioned facts, completion seals and replay in the existing substrate.
Reopen physical-schema choices only on executed query/invariant evidence.

Before implementation admission prove retry identity, atomic publication/consumption, stale-writer
behavior, historical binding and replay equivalence including omissions/unknowns. If generic facts
cannot preserve an invariant or meet measured query needs, reopen the design rather than weaken it.
Necessary evolution is separately admitted, additive and compatibility-tested; rollback preserves
evidence instead of destructively undoing it.
