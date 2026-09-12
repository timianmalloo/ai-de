---
id: note-design-slice-thread-split-is-a-view-of-the-fold
title: "The Console split renders the thread's fold, not ConsoleStreamModel — Ruling 74's equality oracle is an identity over one list, never an equation between two stores"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "Addendum C/D · Conversation lane · DS-1 → CV-1 / CV-2"
tags: [decision-note, session, thread, console, split, derive-dont-store, ruling-74, dm7]
links:
  - { to: design-session-thread-itemscontrol, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: note-session-design-thread-not-panes, rel: relates-to }
review-by: 2027-03-11
review-suggested: []
summary: >-
  The on-demand Console split (Ruling 74) is a FeedList over a flat list of rows derived from
  ISessionThread.Turns — a heading row per turn plus that turn's event lines, in order, each a
  ListItem — so "the split's rows equal the folded events of every turn, in order" (Ruling 74
  condition 1) is an identity by construction and the test M1 is a guard against a second source
  creeping in. Blast radius: CV-1 (the view), the fate of the existing ConsoleSurface in the session
  document (a finding for the conductor), CV-2's join.
---

# The Console split renders the thread's fold, not `ConsoleStreamModel`

- **Kind:** decision
- **Confidence:** Verified for the rule (Ruling 74 `docs/notes/addendum-c-council-rulings.md:1006-1010`,
  `:1028-1031`; SC1 `DESIGN.md:1116`); Inferred for the join the read model performs (CV-2's)
- **Made during:** `/design-slice` of `design-session-thread-itemscontrol` (session `ds-1`, 2026-09-11)

## The call

The split is a `FeedList` (the same base class as the turn feed — P1's virtualization settings,
the owned keys, the structural pin; two consumers) over a **flat** list of rows
`Turns.SelectMany(t => [heading(t), ...t.Events])` from the same `ISessionThread` the feed renders:
a heading row `b<n> · hh:mm` per turn (a `ListItem`, `HeadingLevel` 4), then that turn's event
lines in order, each a `ListItem` named by its text (an AT steps them one by one — the UX &
Accessibility lens's pass-2 finding); `ItemStatus` *following b<n>* while a turn runs, *at b<n>*
when opened from a turn's fold. No `GroupStyle`: grouped virtualization is a separate, non-default
switch that no spike measured, while the flat shape is Q1–Q3's. It reuses `ConsoleSurface`'s row
idiom (`ConsoleSurface.cs:92-117`: lane chip + wrapping `TextBlock`, a UIA name per row) but not
its source, and shares one `EventLine` template with the fold.

**Why.** `ConsoleStreamModel` (`src/AiDe.Core/Presentation/Sessions/ConsoleStreamModel.cs:15`) is the
merged stream across lanes with no turn boundary; rendering the split from it would make Ruling 74's
oracle an equation between two independently maintained sources (the fold and the stream), which
drifts exactly where a compile event or a second lane's line is attributed differently (DM-A: one
quantity, two homes; `TWO-REGISTERS-OF-ONE`). Over one list the equality is an identity, and D1
exists to fail if anyone reintroduces a second source (its falsifier: a `compile.*` line or a second
lane's line present in the stream but not under a turn).

## Alternatives dismissed

- Parametrise `ConsoleSurface` by source — its lane-filter rail and full re-render per `Changed`
  (`:86-123`) are the Console *canvas mode*'s shape (Ruling 45's Console-only strip), not a per-turn
  view; the split needs a heading per turn and a follow/at state the surface has no home for.
- Unfold every turn's events in the thread instead of a split — rejected by the thread-not-panes
  note (`note-session-design-thread-not-panes` §Rejected).
- A second `ThreadFeed` instance over lines — its merge, policy and actions do not apply to lines
  (the Patterns Expert's and the Simplifier's pass-2 finding); the shared base is what both need.
- `GroupStyle` headings — unmeasured grouped virtualization; a heading is a row of the flat list.
- One focusable scroller of `Text` lines — an AT cannot step the lines one by one (the UX &
  Accessibility lens's pass-2 finding).

## Validation condition

Holds unless CV-2's join cannot attribute a run's events to exactly one turn (a reused `run_id`, or
events without one): then the split shows the unattributed lines under a final *unattributed*
heading row and M1 still holds as an identity. Whether the existing `ConsoleSurface` is retired from the
session document or kept for Ruling 45 is **a finding for CV-1 and the conductor**, not this note's.

## Promotion rule

If the split gains its own state (filters, a lane rail, a search) that is not derivable from `Turns`,
promote to an ADR on the Console's stores and link it `supersedes` this note.
