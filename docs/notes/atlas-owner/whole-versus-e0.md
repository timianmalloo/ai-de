---
id: note-atlas-whole-versus-e0
title: "Atlas whole-architecture obligations separated from E0 runtime composition"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-architecture-content"
tags: [code-atlas, owner-ruling, simplification]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: review-code-atlas-architecture-content-gates, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Resolves the Simplifier objection without deleting the required whole architecture: future
  contracts stay explicit, E0 composition excludes them, and navigation is view-local restoration
  state validated by Core rather than an append-only domain aggregate.
---
# Whole architecture, E0 composition

**Decision source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 6,
2026-09-12. **Confidence:** Verified against opened architecture sections.

Keep E1-E4 obligations in the whole PROPOSED overlay and phase-labelled future-contract section.
Make section 7.1 explicitly PROPOSED E0 runtime composition, without later Diagram/ModelPort
boxes. Scope append/seal semantics to durable evidence.

Navigation is view-local history/state plus Core-validated selection. It keeps synchronization,
stale-response rejection, manifest/policy validation and historical-source unavailability.
Memento describes restoration, not a mandated framework or extra class.

The Conductor checked final author revision `51961b6c` after joining through `aac360e7`:
E0's section has no future Diagram/ModelPort boxes; the whole overlay and future contracts remain;
the document explicitly rejects an append-only NavigationSession aggregate.
This satisfies the Simplifier's conditional reductions without cutting the user's full vision.
It grants no code, registration, main integration or normative architecture acceptance.
