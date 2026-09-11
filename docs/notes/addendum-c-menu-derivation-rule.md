---
id: note-addendum-c-menu-derivation-rule
title: "The menu is three derived sets — perspective-independent entries, body-conditional entries, allow-list entries — and a structurally inapplicable command is absent, not disabled"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, menu, palette, derivation, entry-verb]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: spec-app-facelift, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Ruling 55b says the per-perspective menu is derived; this note fixes the derivation's three inputs
  and two edge rules — entry verbs (session.new, terminal.new, the derived "New <Harness> session" rows) are global, live
  in File, and route to Coding, and
  "not admitted here" means absent while "cannot run right now" means disabled with a reason. Blast
  radius: MainMenuBuilder, CommandPalette, and every command row.
---

# The menu derivation rule

- **Kind:** decision
- **Confidence:** Verified for the inputs (`MainMenuBuilder.cs:69-99`, `WorkbenchCommands.cs:24-30`,
  `CommandPalette.cs:150-153` via the inventory); **Inferred** for the absent-vs-disabled line
- **Made during:** `/specify` of `spec-addendum-c-perspectives` (node S1)

## The call

Menu(perspective) = **independent** (File entry verbs and workspace commands; View's perspective radio
and `clearStatus`; Help) ∪ **body-conditional** (Edit/Window/tab navigation iff the body is a docking
host; `focusCanvas` iff the body holds a graph canvas; `raiseDispute` iff Coding) ∪
**allow-list-derived** (one "New <Title>" or "Show <Title>" per admitted kind, from the kind row, with
the row's chord). The palette lists exactly the same set. The expected result per perspective is a
test oracle in the spec (§B3), never a source.

**Entry verbs are global, in File, placed once.** `session.new` (Ruling 55c), `terminal.new` and the
derived "New `<Harness>` session" rows (Addendum A §A4.1: "one menu item away", a spec invariant) are
visible in every perspective under File and route to Coding, because what they create is admitted only
there. They appear **only** in File — the Terminal menu (Coding only) keeps `dispatchPrompt` and the
`prompt` kind's opener, so no command has two placements (the UX-IA and Test Architect reviewers both
caught the duplicate in the first draft). Every other surface-opening command derives from the active
allow-list.

**Absent vs disabled.** A kind the perspective does not admit is structurally unavailable — its entry
is absent from the menu and the palette (`MainMenuBuilder.cs:129-130`: a menu offering something that
cannot work teaches distrust). A command that is admitted but cannot run now (no focused pane, no
workspace) is disabled with a reason on hover (`spec-app-facelift` US-F4). The two are never confused.

## Alternatives dismissed

- *Disable inadmissible entries with a reason* — a View menu in Explore listing eleven greyed items
  is the clutter again, in the menu.
- *A per-perspective menu tuple list* — the second hand-written list Ruling 55b forbids; the
  Terminal menu's derived agent rows already show the right shape.
- *`terminal.new` derived like any other kind* — breaks Addendum A §A4.1's "one menu item away"
  outside Coding.

## Validation condition

Holds until a command appears that is neither an entry verb, nor body-conditional, nor a kind opener
— then the rule gains a fourth set, recorded here, not a special case in the builder.

## Promotion rule

If A1 moves the derivation onto the catalog row (the seam `session-contracts.md` already proposes),
this note is the origin story of that ADR.
