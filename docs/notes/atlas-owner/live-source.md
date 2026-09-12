---
id: note-atlas-live-source
title: "Atlas E0 uses hash-validated live source without historical-body guarantees"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-architecture-content"
tags: [code-atlas, owner-ruling, source]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: adr-01M2BBCCAYMNMM1MFH0653Z0XF, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Chooses hash-validated live reads for E0. Historical source may be unavailable; retaining
  selection and manifest never permits substituting changed bytes or stale anchors.
---
# Hash-validated live source

**Source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 5, 2026-09-12.
**Confidence:** Verified document interpretation; no safe-reader implementation approved.

Choose option A for E0: hash-validated live reads, not retained bodies or Git-object retrieval.
It minimizes work-data retention but does not recover changed historical source.

Back preserves the original selection/manifest. When matching bytes cannot be obtained, show
historical source unavailable and disable its anchors. Opening changed current text is explicit,
not history. Authorization, same-buffer hash/decoder binding, bounded reads and executed native
confinement/TOCTOU proof remain required. Retained bodies/Git-object strategies require separate admission.
