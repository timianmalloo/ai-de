---
id: decision-adoption-boundary
title: "Adoption records current evidence without inventing product history"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: ""
tags: [adoption, decision-note]
links: []
review-by: 2027-02-19
review-suggested: []
summary: >-
  Records the adoption boundary: document the current WPF starter and its known gaps, while deferring unrecorded product intent, designs, and proofs to future owning workflows.
---

# Adoption records current evidence without inventing product history

- **Kind:** decision
- **Confidence:** Verified
- **Made during:** `/adopt`, 2026-08-23

## The call

Record only the current WPF starter and visible gaps. Do not retroactively
author product intent or engineering artifacts the project never had. Root
onboarding and legal files remain in place and are surfaced through
[`docs/project-documents.md`](../project-documents.md), because standard graph
derivation scans `docs/**`.

## Alternatives dismissed

- **Invent history or architecture layers** - false provenance.
- **Move or duplicate root documents** - two sources of truth.
- **Fork the graph scanner locally** - unnecessary for two conventional root
  documents.

## Consequence

The root documents' metadata and review dates are manually owned by
`@timianmalloo`; they are not included in derived freshness checks. `LICENSE`
is the non-Markdown legal-text exception and is linked from the Map of Content.
If this boundary recurs, promote it to a pack-level tool decision.
