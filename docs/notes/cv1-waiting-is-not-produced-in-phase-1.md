---
id: note-cv1-waiting-is-not-produced-in-phase-1
title: "The run-channel read model never produces a Waiting turn in Phase 1: the run host answers permission itself, and the cap is validated, not enforced"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, conversation-lane, cv-1, sc7, ruling-26c, ruling-78, waiting]
links:
  - { to: proof-composer-as-conversation, rel: relates-to }
  - { to: design-session-thread-itemscontrol, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
review-by: 2027-03-12
review-suggested: []
summary: >-
  CV-1 renders every turn state the run channel can truthfully reach; a Waiting turn (the
  permission request or the cap ask, SC7's permission / capask states) is not one of them, because
  GovernedRunHost answers permission requests by its own policy and no cap is enforced in Phase 1.
  The read model, the policy and the actions carry Waiting and are tested pure; the projection
  does not invent it. Holds until CV-3's operator channel lands.
---

# The run-channel read model never produces a Waiting turn in Phase 1

- **Kind:** decision
- **Confidence:** Verified (`GovernedRunHost.cs:673` — "Answers one permission request: allow an
  edit inside the lease, refuse one outside it"; Ruling 26c — validated, not enforced; the
  document's permission banner offers *Dismiss*, never Allow / Deny, for the same reason)
- **Made during:** `/implement` of CV-1 (`docs/proof/composer-as-conversation.md`)

## The call
`RunChannelSessionThread` maps today's run channel into `TurnView`s: accepted → Running; the
run's result → Completed (a goal block) or Answered (a Message); the operator's Stop → Stopped;
a refused or failed run → Failed. It never maps a `permission.request` event to `Waiting`: the
lane is not waiting on the operator — the host's permission chooser answers it — and rendering
*waiting for you · Deny · Allow once* would offer a choice the operator does not hold (DC-110's
shape: a plausible state nobody can act on). The event still reaches the turn as a line, and the
document's existing permission banner (Dismiss) is unchanged. `TurnState.Waiting`,
`WaitingRequest`, the policy's assertive row and the *Deny* / *Allow once* actions exist, are
rendered when a snapshot carries them, and are tested (A1, A4, C1, T1) so the channel that will
produce them (CV-3) changes no surface.

## Alternatives dismissed
- Map `permission.request` → `Waiting` anyway — a lie about who decides; the actions would do
  nothing.
- Drop `Waiting` from the read model until CV-3 — the policy's total table would lose a column
  and the mockup's states would have no code home to fill.

## Validation condition
Holds until the operator channel for permission (CV-3) or an enforced cap (Ruling 78's ask) exists.
When it trips: the projection maps the request to `Waiting` with its request id, and the Proof
Pack's *permission* / *capask* rows move from "not reached by design" to tested.

## Promotion rule
Below ADR weight: a projection rule with one reader. Promote only if a second producer of
`Waiting` appears.
