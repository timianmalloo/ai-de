---
id: mockup-new-session-sheet
title: "New Session sheet — session settings with defaults (Addendum C elevate mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [ui-design, mockup, addendum-c, new-session, session-settings, task-class, contrast]
links:
  - { to: ui-review-perspective-shell, rel: documents }
  - { to: spec-addendum-c-perspectives, rel: implements }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
  - { to: note-addendum-c-design-tier-decoration, rel: depends-on }
  - { to: mockup-session-front-door, rel: refines }
  - { to: ui-review-operator-feedback, rel: relates-to }
review-by: 2026-12-11
review-suggested: []
summary: >-
  The New Session sheet carrying the session settings as defaults: the fan-out ceiling and the
  budget as one row of two unit-bearing controls prefilled from workspace policy, no tier field
  (tier is attached by the compile step), the task class kept required and undefaulted, and seven
  states including create failure with answers kept, an invalid budget, the workspace chooser
  interposing when none is bound, and overflow.
---

# New Session sheet

`docs/mockups/new-session-sheet.html` renders DESIGN.md's *The New Session sheet carries the session
settings* (PS-S1–PS-S4) behind [`docs/reviews/ui-perspective-shell.md`](../reviews/ui-perspective-shell.md).
Open it over `file://`; no build step, no CDN, no network.

## What it renders

Name · **Task class** (required, no default, six named options with one line each, the operator's
last answer as a one-click suggestion that is never a preselection: RQ1–RQ6 kept from the front
door) · **Session settings**: *Fan-out ceiling* (sub-agents, workspace default, with the sentence
that says what the ceiling bounds) and *Budget* (tokens per session, workspace default), and a
one-line note that there is no tier to choose because the compile step attaches it · Agent backends
· the footer with the disabled primary's reason beside it.

## States

default (defaults prefilled, task class unanswered) · answered (one click on the defaults creates) ·
creating (*the document opens in Coding, then Coding activates*) · create failed (reason in the
sheet, answers kept, Create retries) · invalid budget (reason at the field, footer names the field)
· no workspace (the chooser interposes; New session is never disabled) · overflow (a 70-character
workspace name, a 90-character account label, six backends). Theme, viewport, persona (keyboard
focus order, screen-reader trace), reduced motion. The verdict strip reports contrast failures over
24 classified pairings, targets under 24px, and the count of tier fields (must be 0).

## What this mockup is not

Direction evidence only (UI-T4): the sheet's DWM dark caption (TC4), its UIA names and its modality
are runtime proof. The task-class vocabulary is illustrative, as in the front-door mockup; the real
set is sourced at the slice.
