---
id: note-atlas-inventory-default
title: "Atlas physical inventory default and policy-relative completeness"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-architecture-content"
tags: [code-atlas, owner-ruling, inventory]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: adr-01M2BBCCAYMNMM1MFH0653Z0XF, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Chooses tracked plus authorized nonignored untracked files under versioned inclusion policy.
  Completeness is policy-relative; semantic support never silently determines physical visibility.
---
# Policy-relative physical inventory

**Source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 5, 2026-09-12.
**Confidence:** Verified document interpretation; no source dispatch granted.

Choose option A: tracked files plus authorized nonignored untracked files, under a versioned,
disclosed inclusion policy. Git membership classifies; it does not authorize. Non-Git roots use
physical enumeration.

Unsupported/generated/vendor/migration status is not a blanket exclusion for otherwise included
authorized paths. Preserve missing tracked entries. Metadata visibility differs from content-read
permission. Ignored inclusion needs policy admission; paths/counts may be withheld where policy
requires. Pagination cannot omit inventory; interrupted/inaccessible/unresolved-cycle scans cannot
claim completeness.
