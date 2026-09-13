---
id: note-atlas-e1-identity
title: "E1 retains physical inventory and addressable member contracts"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-contract-grounding"
tags: [code-atlas, owner-ruling, identity]
links:
  - { to: note-atlas-delivery-horizon, rel: refines }
  - { to: proof-code-atlas-contract-grounding, rel: depends-on }
review-by: 2026-12-12
summary: >-
  The Owner rejects substituting a node-addressed type viewer for real physical inventory and
  member navigation. Missing inventory/member contracts must be designed, not specified away.
---
# E1 retains physical inventory and addressable member contracts

**Decision source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, follow-up turn 1,
2026-09-12, after the K0 report.

**Ruling:** retain real physical inventory and independently addressable type/member-to-source
navigation in E1. Reject the proposed type-only NodeContent pane as their replacement.

**Because:** K0's original minimal slice began with an indexed graph node, while the approved
horizon requires physical inventory. `has_member` compartment attributes, including their
truncation disclosure, do not establish independently addressable member identity.

**Scope:** explicit inventory/file-source and structured member-identity contracts, with scope,
revision, declaration spans and overload/partial handling. Do not parse display strings for
identity. Reuse the existing substrate; storage representation is not fixed by this ruling.
Full method-sequence reconstruction remains a later stage.

**Conditions:** preserve K0's observations but correct its proposed scope cut. Unsupported and
unindexed files stay visible; supported members reach actual source. Production edits and main
integration remain unapproved until the Core/Claude acknowledgment and seam agreement.

**Confidence:** Verified by the Owner's source/document reads. Its reading of the reported
79-test result is not an independent execution of those tests.
