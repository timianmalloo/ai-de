---
id: note-sh4-coordination-landing-and-drop-sentence
title: "The switch's landing is a column on the Perspective row, and the drop-with-report sentence names each admitting perspective with its gesture — two calls made while landing Ruling 84"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "Addendum C · Shell lane SH-4.1 (2026-09-13)"
tags: [decision-note, addendum-c, perspective, coordination, landing, focus, drop-with-report, ruling-84, sh-4]
links:
  - { to: proof-coordination-perspective, rel: relates-to }
  - { to: note-adr-0030-0032-amendment-coordination, rel: refines }
  - { to: adr-0030-perspective-registry-and-allow-lists, rel: refines }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: ui-review-operator-findings-2026-09-13, rel: relates-to }
review-by: 2027-03-13
review-suggested: []
summary: >-
  Two calls below ADR weight, made by SH-4.1 while landing Ruling 84: (1) the zone a switch lands
  focus on is stated on the Perspective row (Architecture: Center; Coordination: Left; Coding:
  null) because the view's own "active" after a body is parented is the last pane control to
  realize — measured, not designed; (2) the drop-with-report sentence names each admitting
  perspective with its bound gesture ("They live in Coordination (Ctrl+4); open them from its View
  menu."), choosing the review's oracle over DESIGN.md's "Coordination opens with them", which
  asserts the default's contents. Blast radius: every switch's landing; every partial restore's copy.
---

# The landing is a row column; the drop sentence names the perspective and its gesture

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. Two calls in one note because one slice made them and one Proof Pack proves them
(`docs/proof/coordination-perspective.md`, Claims 6, 8, 9).*

- **Kind:** decision (both)
- **Confidence:** Verified — each is measured in the shown census window and headless
- **Made during:** `/implement` of SH-4.1 (session `sh-4`), the Coordination perspective

## Call 1 — `Perspective.Landing`: the zone whose active tab takes focus after a switch

Spec §C5 and DESIGN.md's landing row say where focus lands per perspective: Architecture on the
Center's active tab (Graph), Coordination on the Left's (Terminal sessions, the master list),
Coding on its active document. The window's entry focus read the view's `ActiveSurfaceId` — and
in a shown window that is whichever `LayoutDocumentPaneControl` realized last, because each pane
control activates its selected content from its own `SelectionChanged` as its template applies.
Measured in the census: Provenance (the Right) for Architecture, the Center's last-added tab for
Coordination. Neither is a landing anyone designed.

So the landing is **data on the row** (`ZoneId? Landing`, null for a body with another rule),
read by `PerspectiveShell.LandingSurfaceFor(host)` — the zone's active tab when the zone is open
with content, else the view's active surface — and applied by the window's entry focus **one
dispatcher turn after the body's Loaded** (WPF broadcasts Loaded parent-first; the pane controls
select on theirs). Coding keeps `null`: its rule is the active session document, which SH-4.2
re-cuts to the Left zone; when that lands, Coding's landing may become `Left` and `EntryFocusFor`'s
document rule the fallback — that is SH-4.2's call.

**Alternatives dismissed.** *Leave it to the attended row* — the design row is explicit, and the
artefact was already measured; a finding that would re-dispatch. *A `switch` on the perspective in
`EntryFocusFor`* — the presenter's own falsifier (a switch on a perspective id); the row set exists
so no derived surface needs a case per perspective. *Activate at the body's Loaded edge without
deferral* — measured overridden by the pane controls' own Loaded.

## Call 2 — the drop sentence: "They live in Coordination (Ctrl+4); open them from its View menu."

Two D3 artifacts render one sentence two ways: `DESIGN.md`'s errata *"Coordination opens with them
(Ctrl+4); open Daydreams from its View menu."* and the review's P6 oracle *"They live in
Coordination (Ctrl+4)"*. The oracle's form landed: *"opens with them"* asserts the contents of
Coordination's default — Inferred until the operator runs it, and false for Daydreams, which the
sentence then has to special-case — where *"live in"* is true by the allow-list for every admitted
kind, and the gesture tells the operator how to get there. Two or more admitting perspectives name
their panes each (*"Graph, … live in Architecture (Ctrl+3); Ledger, … live in Coordination
(Ctrl+4). Open them from the View menu there."*); one pane takes the singular (*"It lives in …"*).
The pre-amendment form (*"Architecture admits them; open them from its View menu."*) could not say
which of two perspectives admitted which pane.

**Alternatives dismissed.** *DESIGN.md's sentence verbatim* — asserts the default; needs the Daydreams
clause; the design row is not this slice's to edit (recorded as a finding for D3/the conductor).
*Counts only for the mixed case* (*"7 live in Architecture, 4 in Coordination"*) — shorter, but the
operator cannot tell which pane went where.

## Validation condition

Call 1 holds until a perspective needs a landing that is neither a zone's active tab nor the active
document — then the column becomes a rule, and the rule belongs in an ADR. Call 2 holds until the
design language settles the sentence; if DESIGN.md's row is amended to the landed form, this note
is confirmed; if the Owner rules for *"opens with"*, `LayoutPersistence.DropAnnouncement` and
`ZoneLayoutSlotsTests.TheOperatorsPreCCodingLayout_…` change together.

## Promotion rule

Promote Call 1 to an ADR-0031 amendment if a second landing rule (SH-4.2's Coding re-cut) makes the
column load-bearing across three rows. Call 2 stays a note.
