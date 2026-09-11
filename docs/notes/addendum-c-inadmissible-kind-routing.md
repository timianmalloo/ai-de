---
id: note-addendum-c-inadmissible-kind-routing
title: "A kind-opening request the active perspective cannot satisfy routes to the first admitting perspective in the order Architecture · Coding and announces; in-body node actions never route — no dialog, no silent drop"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, perspective, routing, allow-list, ux]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
review-by: 2027-03-11
review-suggested: []
summary: >-
  When an admitted surface raises a kind-opening request its perspective cannot satisfy (a reader
  node in Explore → "View source" or "Class diagram"), the shell activates the first perspective in
  the routing order Architecture · Coding that admits the kind, opens it there, and announces the
  switch; the operator returns by activating the origin perspective (Escape, from Explore). In-body actions (Read document,
  Metadata & edges, Reveal in graph) never route — they act in the raising body. A target that fails
  to build leaves the perspective unchanged and says why. Blast radius: every cross-perspective
  "Open as…" and drill-to-node path.
---

# Inadmissible-kind requests are routed and announced

- **Kind:** decision
- **Confidence:** Inferred (a UX judgement over verified constraints)
- **Made during:** `/specify` of `spec-addendum-c-perspectives` (node S1)

## The call

The only paths that can request a kind outside the active perspective are explicit user actions
inside a graph or reader surface — `NodeViewMenu`'s "Open as…" options (`NodeViewMenu.cs:38-89`) and
`spec-uml-erm-surfaces` US-U8's drill-to-node. The menu and palette cannot raise one, because their
open/show entries are derived from the active allow-list (Ruling 55b). So the request is always the
operator's own, and the least surprising outcome is to **go where the thing can open**.

Two rules, because the options are of two kinds:

1. **In-body actions never route.** "Read document", "Metadata & edges" and "Reveal in graph" act on
   the raising body's own reader and canvas. The earlier draft routed them to the kind that backs
   them (`canvas`), which would have sent a knowledge node from Explore to Architecture's
   kind-filtered canvas — success announced, node absent (the UX-IA reviewer's Blocker, verified
   against `WorkbenchShell.OpenNodeView`, lines 1300-1330).
2. **Kind-opening actions route in the order Architecture · Coding.** "View source" (`codeviewer`),
   "Class diagram", "Sequence diagram": activate the first perspective in that order that admits the
   kind, open the surface there by its zone rule, announce *"Opened Class diagram in Architecture"*.
   The order is fixed, not rail order, because the requester is always a *reading* surface and
   `codeviewer` is shared — rail order would have sent "View source" from Explore into the Coding
   host. The order is stateless (no "last-active host" register to get wrong).

Return is the ordinary switch: the operator activates the perspective they came from (rail, gesture
or View menu — one action), or presses Escape if it was Explore; activating the already-active
destination is a no-op (a checked radio item does not un-check itself — the Simplifier cut the
"re-activate restores previous" rule of the first draft). A target whose body fails to build leaves the active perspective and focus
unchanged and reports *"Couldn't open Class diagram — Architecture failed to open: <reason>"*. A kind
admitted by no perspective is impossible by construction and asserted by test (US-C3).

## Alternatives dismissed

- *Confirm dialog ("Switch to Architecture?") with a remembered preference* — Eclipse's debug-launch
  pattern (recalled, not fetched — Flagged). Adds a modal, a preference and a place for it to be
  stored, for a request the operator just made deliberately. YAGNI until an operator asks for it.
- *Refuse with a status message* — turns a one-click intent into two clicks and a read.
- *Open in the current host regardless* — violates the Perspective Layout invariant.
- *Route in rail order* — the first draft; sends a shared kind from a reading surface into Coding.
- *Route to the last-active host that admits the kind* — correct in more cases than rail order, but
  stateful; a register nobody can see is the kind of thing the "previous perspective" slot already
  is, and one such register is enough.

## Validation condition

Holds until an operator reports an unwanted switch. The trigger to revisit is a *non-user* source of
open requests (an agent, a deep link) — then the confirm alternative is re-examined, with its source
fetched first.

## Promotion rule

Promote to an ADR only if a second routing source (agent-initiated opens) makes the rule load-bearing.
