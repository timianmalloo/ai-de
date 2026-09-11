---
id: note-addendum-c-coding-default-layout
title: "The Coding perspective's default layout is the session's place: Center empty until a session opens, Terminal sessions left, one terminal below; Evidence and the fleet views leave the default"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, coding, default-layout, loomkeeper, evidence]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: note-addendum-c-current-state-inventory, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  Ruling 55d fixes what leaves the Coding default (the Explore pane, the Domain/Provenance
  duplication); this note fixes what stays. Center: nothing until a session opens; Left: the watcher of
  observed terminal sessions, captioned "Terminal sessions"; Bottom: one terminal; Right: empty. The Evidence master-detail moves to Architecture; the
  Loomkeeper fleet views (board, leaderboard, ledger, daydreams) are admitted to Coding but reachable
  from the derived menu, not present by default. Blast radius: the first thing every operator sees.
---

# The Coding perspective's default layout is the session's place

- **Kind:** decision
- **Confidence:** Verified for the facts; **Inferred** for the placement judgement
- **Made during:** `/specify` of `spec-addendum-c-perspectives` (node S1, `plan-addendum-c-modes`)

## The call

Today's default (`src/AiDe.Core/Workbench/ZoneLayout.cs:132-153`, read at `5d8d51e3`) puts Graph ·
Domain · Sessions · Board · Leaderboard · Ledger in the Center, Explore · Provenance · Contexts · Joins
at Left, one terminal at Bottom. Ruling 55d removes Explore and the Domain/Provenance duplication from
Coding. Of what remains, the Center is the session document's place (Addendum A §A2: composer left,
canvas right; Ruling 47: a new session takes the stack), so a tab strip of fleet views beside a
session is the clutter the operator named. **Coding's default is therefore: Center empty (with the
"No session open" first-action state), Left = Terminal sessions, Bottom = Terminal — pwsh, Right = empty.**

The Left pane is the `sessions` kind — the Loomkeeper watcher of **observed terminal sessions** (its
own empty state says "Open a Claude Code or GitHub Copilot session from the Terminal menu, and it
appears here", `SurfaceContentFactory.cs:315-341`). It is *not* a list of sessions the operator can
open (the earlier draft of this note said so and was wrong — the UX-IA reviewer caught it). It stays
at Left because it is the secondary model's live list; it is captioned **"Terminal sessions"** because
Addendum A §A3 reserves the bare word for the user-facing container, and two meanings of "session" on
the 80% first screen is exactly the collision A3 repaired.

Two consequences that go beyond Ruling 55d's words and are this note's call:

1. **The Evidence master-detail (`view` master, `inspector` detail) is an Architecture surface**, at
   Left/Right of host B so it is side-by-side master-detail rather than sibling tabs. Evidence rows
   are indexed code facts with provenance — UC2/UC3 material, not UC1. The owed "two kinds render
   different content" test still lands in the Coding slice (Ruling 55d) because the factory rows are
   shared and the Coding slice is the first to touch them.
2. **The Loomkeeper fleet views (`board`, `leaderboard`, `ledger`, `daydreams`) are admitted to Coding
   but not in its default.** They observe agent work in coding sessions, so Coding is their perspective;
   they are not the 80% case, so they are one derived "Show …" entry away rather than tabs in the
   session's Center. `daydreams` is in no default layout and has no command today (reachable from
   nowhere); the derived menu gives it a door by construction.

## Alternatives dismissed

- *Keep Board/Leaderboard/Ledger as Center tabs* — the operator's complaint is exactly this mix; a
  session document sharing its stack with three fleet tabs is the status quo.
- *Keep Provenance in Coding as a selection-bound inspector* — nothing in Coding produces an evidence
  selection; an inspector with no master is the empty-forever pane.
- *Exclude the Loomkeeper kinds from every perspective* — makes five built surfaces unreachable; a
  kind admitted by no perspective is a defect the spec's US-C3 forbids.
- *A dedicated "Fleet" perspective* — a rail item for a use case the operator did not name (AR3).

## Validation condition

Holds until the operator sees the Coding default and objects, or until INV-0006's zone/tree repair
changes the zone model this layout is expressed in (Ruling 55 CONDITIONS). Findings 8 and 9 in the
spec's §R put the placement to the Owner.

## Promotion rule

If a second perspective's default depends on this (Architecture's does — it receives what Coding
drops), promote to an ADR alongside the ADR-0017 amendment at A1.
