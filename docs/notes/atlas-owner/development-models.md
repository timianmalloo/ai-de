---
id: note-atlas-development-models
title: "Development fleet does not amend product conductor hosting"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "atlas-framing"
tags: [code-atlas, owner-ruling, models]
links:
  - { to: note-code-atlas-proposal-provenance, rel: depends-on }
  - { to: spec-conductor, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Astra and GPT-5.5 are the user-selected engineering fleet. That choice does not replace the
  product's headless-Claude conductor or subscription-first access contract.
---
# Development fleet does not amend product conductor hosting

**Decision source:** Astra Owner `61e506c4-2d12-42e9-85cb-153f2f916811`, 2026-09-12.

**Ruling:** treat Astra/GPT-5.5 as the development fleet only.
**Because:** Conductor v1 section 5.1 fixes the product conductor as headless Claude Code;
section 4.2 preserves subscription-first access. The proposal requires the approved harness boundary.

**Scope:** no product provider replacement, direct-SDK shortcut, or reuse of the prompt compiler
as a generic analysis endpoint.
**Condition:** a product-hosting change requires a separate explicit specification decision.
Development model selection supplies no product data-egress authorization.
**Confidence:** Verified document interpretation; not a new runtime capability claim.
