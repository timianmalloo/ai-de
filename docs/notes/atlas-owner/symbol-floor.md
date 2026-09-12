---
id: note-atlas-symbol-floor
title: "Atlas E0 mandatory symbol subset and per-kind capability admission"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-architecture-content"
tags: [code-atlas, owner-ruling, symbols]
links:
  - { to: note-atlas-e1-identity, rel: refines }
  - { to: adr-01M2BBCC9EHCWVR1R4ZCZ7502T, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Makes E0's real member-navigation floor explicit while refusing blanket C# support claims.
  Per-kind compiler and native journey evidence is required before capability advertisement.
---
# E0 symbol floor

**Source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, turn 5, 2026-09-12.
**Confidence:** Verified document interpretation; not compiler/product capability certification.

Choose option A: probed mandatory kinds with explicit per-kind admission.
E0 requires source-declared types, ordinary methods/overloads, constructors, properties/accessors
and partial type/member declaration navigation. Remaining unprobed kinds stay explicit staged
obligations, not implied E0 support.

Each advertised kind needs compiler-contract and product-journey evidence; synthetic probing alone
is insufficient. Preserve scope-qualified IDs, distinct partial definition/implementation spans,
decoder/offset round trips and typed unsupported/null-ID states. Do not relabel a mandatory kind
optional to obtain an exit pass or reduce the journey to types.
