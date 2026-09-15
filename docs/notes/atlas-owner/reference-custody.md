---
id: note-atlas-reference-custody
title: "Clean-main Atlas delivery with private proposal kept local"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-framing"
tags: [code-atlas, owner-ruling, privacy]
links:
  - { to: note-code-atlas-proposal-provenance, rel: depends-on }
review-by: 2026-12-12
summary: >-
  Delivery starts from confirmed main rather than importing the private proposal history.
  Only safe requirements summaries and independently safe test material enter delivery artifacts.
---
# Clean-main Atlas delivery with private proposal kept local

**Decision source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, 2026-09-12.

**Ruling:** use proposal `1065a851` solely as a local reference; no proposal-history merge or publication.
**Because:** its TheTerrace source/session captures are curated review inputs, not product fixtures
or authorization to distribute that corpus.

**Conditions:** confirm main and the reference before branching; admit only reviewed safe summaries
and independently safe test material. Exclude raw source, inventories, embedded fixtures, session
excerpts and mockup captures from copied artifacts. No push/publication this turn.

The executor observed main `b0e092b5` and the local proposal `1065a851` before branching.
The Owner's file reads did not independently verify Git state; this distinction is retained.
Unauthorized actual disclosure is a hard-floor escalation, not an Owner trade-off.
