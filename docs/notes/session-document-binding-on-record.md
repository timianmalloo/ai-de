---
id: note-20260912-session-document-binding-on-record
title: "A session that already exists is shown and bound from what is on record: no task class until one is chosen, a malformed provider file as a refusal on the composer, and never bound twice"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "conductor-front-door"
tags: [decision-note, session-document, composer, reopen, layout-restore, dc-084, dc-040, ruling-70]
links:
  - { to: inv-0009-a-session-document-opened-into-a-body-that-is-not-on-screen, rel: relates-to }
  - { to: proof-session-document-render, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: depends-on }
review-by: 2027-03-12
review-suggested: []
summary: >-
  Reopen and workspace-open restore bind a session's composer from what session.json carries: the
  routable set derived by the sheet's own rule, no task class (Send refuses by name until one is
  chosen), a malformed provider file surfaced as a named refusal on the shown composer, and a bound
  composer left alone. Blast radius: every path that shows an existing session document; Ruling 72's
  per-prompt class will replace the refusal with the default.
---

# A session that already exists is shown and bound from what is on record

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** Verified (each call has a red-first test in `docs/proof/session-document-render.md`)
- **Made during:** `/implement` of INV-0009 phases 1, 2, 2b, 3, 4 (session `render-fix`, branch `fix/session-document-render`)

## The call

Four calls the plan left to the implementer, made together because they are one shape — *what a
session that already exists is bound with* — and recorded together so the next reader finds them
in one place.

1. **No task class on reopen or restore.** `SessionConfig` carries none on this branch, so
   `ComposerSendContext.TaskClass` became nullable (Ruling 70's own constraint) and `Send` refuses
   by name — *choose a task class for this prompt* — rather than binding with a guessed class
   (DC-110: a guessed class ranks the episode in a cohort nobody chose and is indistinguishable
   afterwards). Ruling 72's `free-form` default belongs to the session model (D2's lane); when it
   lands, the refusal stops firing on its own.
2. **A malformed provider file on reopen or restore is a refusal on the composer, not a document
   withheld.** New Session keeps refusing before its sheet (the sheet is not constructible without
   a registry); a session that exists is still shown, with `providers: <the reader's message>` on
   the status line and in the log (`session-document.refused`). A bound composer over an empty
   registry read out of a broken file would be a wrong claim about the file (Ruling 47 (b)).
3. **A bound composer is not bound again.** A reopen of a session whose document the restore
   already revived reaches the binder a second time; a second `Configure` after the page mounted
   re-mints the fields and pushes a second `host.init` over whatever the operator typed. The binder
   answers *Composer already bound.* and writes nothing. Consequence, recorded on the window's
   `ReadProviders` remark: a provider-file edit reaches a bound composer only through a fresh
   document.
4. **The routable set on reopen is the sheet's own derivation** —
   `NewSessionSheetViewModel.RoutableAmong(config.EnabledBackends, registry)` — so a reopen cannot
   bind an engine the sheet would have refused (DM7: one derivation, two readers).

## Alternatives dismissed

- Bind reopened sessions with `free-form` in the binder — two definitions of the default once the
  session model carries one; a door-invented value on this branch.
- Refuse to reopen over a malformed provider file (mirror New Session) — withholds a session that
  exists and whose history is readable; the operator's next action is the same either way.
- Rebind on every reopen so a provider-file edit is picked up — destroys the draft on screen.
- Derive the routable set in the binder from `providers.Registry` on the New Session path too — the
  sheet's list can differ after a Sign in inside the sheet (a re-probed registry); the sheet's list
  is passed through unchanged there.

## Promotion rule

If the per-prompt task class (Ruling 72) or a second binder caller appears, fold (1) and (3) into
the session-design ADR that lands them; this note stays as the origin.
