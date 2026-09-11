---
id: note-addendum-c-design-tier-decoration
title: "Tier is compiled, not typed — the operator's correction to Ruling 56's tier clause, and how the composer and the sheet render it"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, composer, session-settings, tier, compile-step, operator-correction]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: refines }
  - { to: mockup-conversation-composer, rel: relates-to }
  - { to: mockup-new-session-sheet, rel: relates-to }
  - { to: ui-review-perspective-shell, rel: relates-to }
review-by: 2026-12-11
review-suggested: []
summary: >-
  During D1 the operator corrected Ruling 56: tier is not a session setting; it is a decoration
  the compile step attaches to the compiled prompt. This note records the correction verbatim as
  relayed, what the design does with it (no tier field anywhere; the compiled disclosure carries
  the derived tier with three states; the sheet and the settings line carry the fan-out ceiling and
  the budget only), the one reading that is the conductor's and not the operator's (ceiling versus
  effective cap), and what is deliberately not designed (the derivation and the compile step).
---

# Tier is compiled, not typed

- **Kind:** decision (records an operator correction; below ADR weight; the spec is amended by the conductor on `main`)
- **Made by:** node D1 (`/ui-design`), session `addendum-c-chain`, 2026-09-11, on the conductor's message relaying the operator
- **Arrived when:** after Stages 0–3 and the first mockup were done; before the composer and sheet mockups and the critique. Folded into DESIGN.md (PS-C7, PS-S1–S3, the density contract, the copy) and the two later mockups before the adversaries convened.

## The operator's words (relayed by the conductor; logged today in session `conductor-addendum-c`) **[Verified — relayed; the audit entry is on the conductor's branch]**

> "shouldn't tier be decided by the compilation of the prompt? A key aspect and benefit of being able to
> type a prompt and then post-process it would be to decorate it with things like tier."

> "that makes me realize we haven't really thought through the compile step explicitly — maybe I need
> to do that."

## What the design does

1. **No tier field anywhere the operator types.** The New Session sheet's *Session settings* row holds
   the **fan-out ceiling** and the **budget** only, each with unit and source, and a one-line note that
   there is no tier to choose (PS-S1–S2). The composer's inherited-settings line reads *"fan-out ≤ 3 ·
   budget 40k tokens · from session settings"* (no tier). The sheet mockup's audit counts tier fields
   and requires 0.
2. **Tier is a decoration on the compiled prompt.** The compiled disclosure's header carries it as a
   derived value the operator sees and confirms by sending: *tier not derived yet* (before the draft
   settles) → *~ T1 derived* (`{colors.inferred}` glyph + word) → *T1 confirmed* (on send). A T0
   derivation in a session whose ceiling is non-zero says *"T0 derived: this run uses no fan-out
   (session ceiling 3)"* on the same line (PS-C7). The tier appears inside the compiled text too, as a
   line with the comment *attached by the compile step (derived)*: the disclosure shows exactly what goes
   out.
3. **The fan-out value in the sheet is the session's ceiling; the effective cap is the compiled tier's
   cap within it** (0 at T0, 2 at T1, the GO7 cap at T2) **[Inferred — the conductor's reconciliation of
   CT19 with the correction; not yet the operator's word]**. The sheet says so in the ceiling's own
   sentence. If the operator's compile-step specification says otherwise, the ceiling sentence and the
   T0 line change; nothing structural does.

## What is deliberately not designed

- **How the tier is derived** (from the compiled prompt's content? its structure? the mentions?) — the
  operator is going to think the compile step through; this design shows the *result* (a derived value
  with a state) and not the mechanism.
- **The compile step itself** — its inputs, its ordering, whether it is one pass or several. The
  disclosure's copy says *attached by the compile step* and no more.
- **Whether the operator may override a derived tier before sending.** The design shows *derived* and
  *confirmed* only; an *edited* state for the tier would be a per-prompt setting through the back door
  and is left to the compile-step specification.

## Supersession

This correction overrides the **tier clause** of Ruling 56 (`note-addendum-c-council-rulings` §Ruling
56: *"fan-out cap, budget and tier are session settings with defaults"*) as the operator's own decision
(the ruling's own CONDITIONS said an operator answer files over it). The fan-out and budget clauses
stand. The spec's S-1/S-2 rows and US-C13's *"T2 · fan-out 3 · budget from session"* copy are the
conductor's to amend on `main`; D1 does not edit `docs/specs/`.

## Falsifier

An operator observed typing a tier per prompt, or asking where to set one: the signal that a derived
tier was not enough, and the remedy is the compile-step spec, not a field.
