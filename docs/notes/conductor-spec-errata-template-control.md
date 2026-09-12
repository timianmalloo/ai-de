---
id: note-conductor-spec-errata-template-control
title: "Spec erratum — Addendum B :181's shape control is the template control (template: none | <id>@<version>); B :186's Score outline is superseded by the jump list"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [conductor, spec, errata, addendum-b, addendum-c, template, shape, jump-list, score-outline, ruling-74, errata-e1]
links:
  - { to: spec-conductor, rel: relates-to }
  - { to: note-conductor-spec-errata-policy, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-conductor-spec-errata-session-thread, rel: relates-to }
  - { to: mockup-session-conversation, rel: relates-to }
review-by: 2027-03-11
summary: >-
  Addendum B names the composer header's control a "Shape control: Free-form | Template picker"
  (line 181) and gives each block a "shape badge (free-form or template id@version)" in the Score
  outline (line 186). Erratum E1 of the D2/A1 batch renames it the template control —
  template: none | <id>@<version> — because Free-form is now the default task class (Ruling 72) and
  shape names Message | Goal-block (Addendum A R15 b2); and Ruling 74's condition 3 records the Score
  outline as superseded by the jump list, not silently dropped. The HTML stays byte-frozen.
---

# Spec erratum — Addendum B `:181` / `:186`: the template control, and the Score outline's fate (E1; Ruling 74 condition 3)

**Filed by:** the conductor's errata node (session `errata-74-78`), 2026-09-11, applying **erratum E1**
of the D2/A1 batch and **Ruling 74** condition (3) (`note-addendum-c-council-rulings`). Confidence:
**Verified** — each quoted line was read directly from
`docs/specs/conductor/ai-de-spec-addendum-b-prompt-templates.html` at its cited line number in this
worktree; the Owner's wording is quoted from the ruling note.

Per `note-conductor-spec-errata-policy`: the addendum HTML stays byte-intact; this note is the
correction, linked from `docs/specs/conductor/README.md`.

## Why (the Owner's BECAUSE, E1)

> `Free-form` (template) · `free-form` (class) · *shape* twice — `DESIGN.md:1178`, the
> decoration-line note §Why.

Three meanings were riding two words: *Free-form* named a template state **and** the default task
class (Ruling 72), and *shape* named the header control **and** the submission shape every
decoration line carries (Addendum A R15 b2: Message | Goal-block). E1 reserves the vocabulary:
**shape** is Message | Goal-block; **template** is `none | id@version`; **class** is the task class.

## The two spec lines this corrects

**B6, line 181** (the composer header control):

> `  <li><b>Shape control</b> in the composer header: Free-form | Template picker (searchable, grouped by intent, recents first). Per block; switching shapes preserves content (template→free-form yields the compiled text for editing; free-form→template goes through apply-template).</li>`

Reads now — the Owner's wording, verbatim:

> **Template control** in the composer header: `template: none | <id>@<version>` (searchable picker,
> grouped by intent, recents first). Per block; switching preserves content (template → none yields
> the compiled text for editing; none → template goes through apply-template).

The control's position (the composer header) and behaviour are kept — Addendum C S-9 stands as
*renamed*, not superseded; the twelve built-in templates (B4) remain reachable from it.

**B6, line 186** (the Score outline):

> `  <li><b>Score outline:</b> each block shows its shape badge (<code>free-form</code> or template id@version); restart-from-block reopens the original shape with its values.</li>`

Reads now — the Owner's wording, verbatim:

> the jump list (Addendum C §B2) lists each turn's ordinal · words · outcome; the decoration line
> carries `<shape>` (message | goal block) and `template <id> v<n>`.

Ruling 74, condition (3): *"Addendum B `:186`'s Score outline is recorded as superseded by the jump
list, not silently dropped."* The Score outline's job — a navigable sequence of the session's blocks
— is the **jump list** on the session header's turn count (`DESIGN.md` SC8: ordinal · the words · the
outcome word, type-ahead on the ordinal, Enter focuses the turn), an on-demand view like the Console
split. The **shape badge** is gone from the send row and from the outline: the decoration line's
`<shape>` and `template` segments carry both, on every turn, in one grammar (`DESIGN.md` SC2).
*Restart-from-block* is not re-specified by this erratum; where it returns, it reopens the turn's
template with its values — a finding, not a rule.

## Scope effect

**Freezes** the addendum HTML, as the policy requires. **Records** the template control
(`template: none | <id>@<version>`) as the header control's name and grammar, the Score outline as
superseded by the jump list, and the three reserved words. **Amended in step:** Addendum C §B2
(`:971`, `:977`, the glossary), S-9, Flow 6 and US-C13 (in-repo, edited directly); `DESIGN.md`'s
recorded deviation for the header control is marked filed.
