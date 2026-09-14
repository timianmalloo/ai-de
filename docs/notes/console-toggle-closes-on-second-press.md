---
id: note-20260913-console-toggle-closes-on-second-press
title: "The session header's Console toggle closes the Console document on its second press; the verb focuses it"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "addendum-c"
tags: [decision-note, addendum-c, shell-lane, sh-4-2, ruling-89, console-document, accessibility]
links:
  - { to: proof-coding-recut-left-dock, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: refines }
  - { to: ui-review-operator-findings-2026-09-13, rel: relates-to }
review-by: 2027-03-13
review-suggested: []
summary: >-
  Ruling 89 says "a second toggle focuses it"; SH-4.2 ships the toggle as an honest ToggleButton
  whose state is the Console document's open state — a second press closes it — because a toggle
  that never releases lies to the UIA Toggle pattern (WCAG 4.1.2, the UX & Accessibility lens's
  Blocker). The verb `session.console`, the View menu row and a turn's Open the log focus the one
  open console; the count stays ≤ 1 either way. One line reverses it; the conductor ratifies.
---

# The session header's Console toggle closes the Console document on its second press; the verb focuses it

*A decision note (`knowledge-visualization.md` V17): below ADR weight, above chat-scrollback
weight. One note per call; written before the session that made it closes.*

- **Kind:** decision
- **Confidence:** Verified (the control's UIA behaviour; the oracle presses through `IToggleProvider.Toggle`)
- **Made during:** `/implement` of SH-4.2 (session `sh-4-2`), Stage 4 — the UX & Accessibility lens's loop-1 Blocker

## The call
CV-5.2 built the header's Console control as a `ToggleButton`. Hosted as a Center document (Ruling
89), the shell drives it from `Checked`/`Unchecked` — the events an assistive technology's Toggle
pattern raises with no `Click` — so **checked means the Console document is open and unchecked
closes it**; `ReflectConsole(open)` lets the shell mirror a state it changed itself (an open by the
verb, a close by the tab's ✕, a refused open) without dispatching. Ruling 89's letter — *"One per
session: a second toggle focuses it"* — is honoured by the **verb** (`session.console` from the View
menu, the palette, the chord) and by a turn's *Open the log*, both of which focus the one open
console; the count of `console:<id>` stays ≤ 1 on every path, which is the ruling's substance.

**Why:** a ToggleButton whose press never releases lies twice — its accessible role promises a
state that unchecking would change, and an AT's `IToggleProvider.Toggle` flips `IsChecked` with
nothing happening (WCAG 4.1.2; the lens's finding 1). Replacing the control with a plain Button was
the alternative, but the control is CV-5.2's and the seam names only the toggle's dispatch.

## Alternatives dismissed
- **Keep the ToggleButton, second press focuses (the ruling's letter)** — the state lies; UIA Toggle does nothing; the lens's Blocker stands.
- **A plain Button plus a separate "open" indicator** — honest, but a control-type change in a Conversation-lane file beyond the seam's named touch.
- **Re-check on Unchecked and focus** — fights the operator's and the AT's gesture.

## Validation condition
Holds until the conductor or the council rules on Ruling 89's letter. Reversal is one line: the
`Unchecked` handler in `SessionDocumentSurface` (dispatch `ConsoleRequested` instead of
`ConsoleDismissed`) and C5's assertion.

## Promotion rule
If this call starts bearing load — multiple artifacts depend on it, or reversing it would be
expensive — promote it to a ruling or an ADR; today it is one handler and one oracle.
