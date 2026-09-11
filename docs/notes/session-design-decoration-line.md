---
id: note-session-design-decoration-line
title: "The decoration line — one grammar for every turn (class · tier · lease · shape · template, provenance on demand), the task class per prompt with free-form as the explicit default, and where the tier control sits"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, addendum-d, session, decoration, task-class, tier, lease, provenance, ruling-70, ruling-72, ui-design]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: refines }
  - { to: note-addendum-c-design-tier-decoration, rel: refines }
  - { to: mockup-session-conversation, rel: relates-to }
  - { to: ui-review-session-conversation, rel: relates-to }
review-by: 2026-12-11
review-suggested:
  - { by: mockup-session-conversation, on: 2026-09-11, reason: "D2 /ui-design elevate: the session as a conversation supersedes the composer-beside-Console layout; the specs' §C1/§B2, the tier control's home and the template control are findings for their owners" }
summary: >-
  Every turn carries one decoration line rendered from the envelope, the one store (DESIGN.md
  SC2–SC5). The task class is a per-prompt control with free-form as the session's explicit
  default (Ruling 72 over Ruling 70's refusal clause); the tier control sits on the same line
  beside its rationale, a placement that deviates from Addendum D's compile line; the write-scope
  line is the lease segment; the header's shape control became a template control. The note
  records the operator's words behind each choice, the rejected alternatives and the findings.
---

# The decoration line

**Decided:** DESIGN.md **SC2** (one grammar, rendered from the envelope; a past turn compact, the
current turn with its provenance inline), **SC3** (task class per prompt, `free-form` the explicit
default, no refusal), **SC4** (the tier control on the decoration line, *stale* on the compile line
only), **SC5** (the compile line only when an envelope exists; the nine strings; *what was read*).

## Why

- **Per-prompt class, defaulted:** *"task class seems like it should be something defined for every
  chat in a conversation"* (Ruling 70) and, mid-run at this node, *"I don't need to choose a task
  class — the basic should be free-form upon open and then I can change it"* (Ruling 72,
  `al-01M297VC0HTFJP761D9BVE9Z72`). The menu's sentence for `free-form` is *"no class ranks this
  turn"*, which keeps Ruling 19's intent (no system-chosen class can rank) as information, not a gate.
  The session-level class chip leaves the header; the *default* lives in session settings.
- **The tier beside its provenance, on the decoration line:** a past turn's tier and the current
  turn's tier must read in the same place, and past turns have no compile line at rest. Rulings
  63/64 are honoured unchanged: compiled, overridable in Prepare, never typed, never a field; the
  rule's value is one menu row away (*Restore the rule's value*).
- **One store:** the Simplifier found the provenance rows and the compiled prompt's comments
  authored twice and already drifting. The envelope's decoration rows are the store; the line, the
  provenance disclosure and the compiled bytes are three renders, and the bytes carry no comments.
- **The lease segment is the write scope:** `LeaseDerivation.Patterns(source_text)` (Ruling 42) is
  the one source; every pattern in full on the current turn, `+n more` with the list in the
  description on a past turn.
- **The template control:** with `free-form` a class and *shape* the submission shape (Message |
  Goal-block, Addendum A R15 b2) on every decoration line, the header's *Free-form | Template* would
  give both words a third meaning. It reads `template none` / `template change-order v2`; the send
  row's badge, a third carrier of the shape, is deleted.

## Rejected alternatives

- **A class chip on the header plus a per-turn override** — two homes for one value.
- **The class required on the New Session sheet** — rejected by the operator (Ruling 72).
- **A class or tier field on the send row** — a per-prompt settings field (Ruling 56's shape).
- **Refuse Send when the class is free-form** — free-form is a value (Ruling 72).
- **Keep buttons under the `agentic` rung** — *kept* is implicit at Send there (Addendum D §A11);
  *keep* is the `agentic-advisory` act and renders only in that mode.
- **Stale on the tier and the compile line** — one state, one carrier (the spec's compile line).

## Findings for the specs (the conductor's, not this node's)

1. Addendum D §B2/§B4/§B5: the tier control's home (decoration line vs compile line) and the
   tab-order criterion that follows it; whether `task_class` is in `inputs_sha` (does a class
   change after Prepare stale the envelope?).
2. Addendum C §B2: the write-scope region folds into the lease segment; §B2 / §C4 and the sheet: the
   class chip leaves the header; the sheet's field is *default task class*, `free-form` when unset.
3. Addendum B `:181` / C §B2 / US-C13: the header's control is a *template* picker (`none` |
   `<template>`), *shape* is reserved for Message | Goal-block, and the send-row badge sentence
   moves to the decoration segment. This also dissolves the `Free-form` / `free-form` collision
   without a ruling on the class name.
4. Addendum C §C4's refusal copy *"This prompt compiles at T2 and needs Not in scope"* versus its
   own shape rule (`SpawnContract.Validate` refuses a blank boundary tier-blind): which sentence is
   true — an Owner ruling. The mockup refuses `template` and `suspect` with *"This prompt is a goal
   block and needs Not in scope."* and keeps the ratified copy in `gaps`.
