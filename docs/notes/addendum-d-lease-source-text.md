---
id: note-addendum-d-lease-source-text
title: "Lease derivation runs over the editor's source text only — never attachment bodies, the rendered goal block, the history window, or model output; today it runs over the whole rendered text"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "1"
tags: [decision-note, addendum-d, lease, ruling-42, security, composer]
links:
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: note-front-door-rulings-41-42, rel: relates-to }
review-by: 2027-03-10
review-suggested: []
summary: >-
  Ruling 42's intent — the paths the operator referenced are the paths they mean — is not met by a
  mention inside an attached file or inside model-authored structure; today
  `LeaseDerivation.Derive(compiled.Text)` at `ComposerSendGate.cs:164` reads both. The call site
  changes its argument to the editor's source text; `LeaseDerivation` itself does not change. Blast
  radius: any operator relying on an attachment to widen a lease (none known; no test either way).
---

# The lease source is the editor's text

- **Kind:** decision (extends Ruling 42; changes built behaviour, so put to the Owner as PR-D3)
- **Confidence:** Verified — `ComposerSendGate.cs:164` derives over `compiled.Text`;
  `ComposerCompiler.Compile` appends attachment fences to that text (`:55-64`) and renders the
  goal block's sections into it (`:83-108`); `grep` finds no test asserting the attachment case
  either way; `new Lease(` has exactly two sites in `src/` (`LeaseDerivation.cs:94`,
  `ConductorEntry.cs:123`)
- **Made during:** `/specify` of `spec-addendum-d-compile-step` (node S2), found by the Security &
  Identity Architect in Peer Mode while closing every path from model output to a lease pattern

## The call

`LeaseDerivation.Patterns` and `Derive` run over the **editor's source text** — what the operator
typed — and nothing else. Four things are therefore excluded that today's argument includes:
attachment bodies (an affirmed file is bytes the operator read, not a scope they wrote), a template
body (not the operator's keystrokes), the rendered goal block (under Addendum D its lines may be
model-authored, and an operator's edit of a line is a decoration, not the editor), and — by
construction — the history window and any model proposal (neither is ever part of the text passed
to the function). The
boundary refuses any `derived` proposal carrying an `@\S+` token (C-Lease), so a mention can enter
the lease only by the operator inserting `@path` into the editor.

The change is at the **call site's argument** (`ComposerSendGate.cs:164` and the display caller at
`ComposerSurface.cs:449`); `LeaseDerivation` is byte-identical. The census gate holds the
construction sites at two.

## Alternatives dismissed

- `strip @ from attachment bodies before deriving` — a silent rewrite of bytes the operator affirmed;
  and the widening from the goal block would remain.
- `keep today's behaviour and warn` — a warning on a security control is a control switched off
  while looking present (the Ruling 42 note's own words about a `simplify:` on a lease).

## Validation condition

Holds until/unless the operator rules that a template body or an attachment may carry scope (then
the argument is the union, stated, and Ruling 42 is re-opened) — the Owner's question is PR-D3.

## Promotion rule

Promote into the Ruling 42 note's lineage when filed; the red-first test (an attached body with
`@src/` adds no pattern) is the control that outlives this note.
