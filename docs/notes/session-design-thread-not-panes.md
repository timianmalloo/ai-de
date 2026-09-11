---
id: note-session-design-thread-not-panes
title: "The session is a thread, not two panes — the Console as the reply side of each turn, the editor pinned beneath, and what that reverses in Addendum C §C1/§B2"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [decision-note, addendum-c, addendum-d, session, thread, console, archetype, ui-design, operator-verdict]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: refines }
  - { to: mockup-session-conversation, rel: relates-to }
  - { to: mockup-conversation-composer, rel: relates-to }
  - { to: ui-review-session-conversation, rel: relates-to }
review-by: 2026-12-11
review-suggested:
  - { by: mockup-session-conversation, on: 2026-09-11, reason: "D2 /ui-design elevate: the session as a conversation supersedes the composer-beside-Console layout; the specs' §C1/§B2, the tier control's home and the template control are findings for their owners" }
summary: >-
  At node D2 the session document became a thread (DESIGN.md SC1, SC7, SC8): turns above one
  pinned editor, the lane's reply folded beneath the turn that caused it, the Console split and
  the Score outline kept as on-demand views of the same stream. This adopts Layout:StreamingThread,
  which D1 and spec §C1 declined. The note records the evidence, what is reversed and kept, the
  rejected alternatives, the one Inferred reading (one run at a time), and the findings for the
  specs.
---

# The session is a thread, not two panes

**Decided:** DESIGN.md **SC1** (one thread, one editor, the Console split on demand), **SC7** (the
reply side is the Console folded per turn; a failure lives where it happened; a refusal before start
is composer-side), **SC8** (the feed's keyboard model; the turn count is the jump list).

## Why (the evidence, verbatim)

- *"the UX is super chunky… it does not feel like a chat conversation and the whole enter in text
  boxes and see the render below is awful"* (`al-01M28T8C2WEVN4J0D10XQJMJAZ`).
- *"a session can have n prompts… because a session is a conversation"* and *"launching a session
  should use the whole real-estate so the doc with the graph etc. shouldn't be visible"*
  (`al-01M296K4DAJ7H8NP26WC7B135Y`).
- D1's own review listed *reply-beside-input* as a residual: *"unvalidated against the operator's
  'does not feel like a chat' verdict"* (`ui-review-perspective-shell` §9).

The measurable difference is DESIGN.md's thread contract: one left edge, the editor's top edge
independent of the turn count, the reply under the words that caused it.

## What this reverses, and what is kept

| Where | It said | Now |
|---|---|---|
| spec-addendum-c-perspectives §C1 | `Layout:StreamingThread` **not adopted** | **Adopted.** Addendum D Part C already reads `StreamingThread` for the session document; the two specs disagree today. |
| spec-addendum-c-perspectives §B2 | *"composer (now) beside canvas (before)"*; the Score outline (Addendum B `:186`) holds the sequence of blocks | The earlier turns live in the thread with their reply side; the Console is the reply side folded per turn; the **Score outline is the jump list** on the header's turn count (ordinal · the words · the outcome), an on-demand view like the split. |
| Addendum A §A2 (the canvas beside the composer) | composer + canvas | The canvas modes (Console today) are views of the thread's stream; MS1–MS5 keep their geometry rule. |

Kept: Ruling 21 (the split, on demand); Ruling 45 (Console-only strip); Ruling 47 (maximize on
create; the way back is the restore control, `Ctrl+K, Z` **[Inferred: `workbench.maximizePane`
toggles]**); Ruling 57 and D1's language (ratified, Ruling 72); Ruling 42.

## Rejected alternatives

- **Keep the two panes, add a thread inside the composer.** The reply would still be in another pane;
  the verdict is about where the reply is.
- **Bubbles; a centred narrow column.** D1's anti-goals and the whole-real-estate ask; prose gets a
  measure (96ch), the evidence lines get the width.
- **Unfold every turn's events in the thread.** At 40 turns the thread would be a log; the running
  turn shows its last four lines, a completed turn folds them, a failed *past* turn folds too.
- **A refused-before-start turn in the thread.** A turn joins the thread when the conductor accepts
  it (`Feedback:+Confirmed`); a refusal is the send row's, with the draft and its envelope kept.

## Inferred, and what would settle it

**One governed run at a time per session.** No spec sentence says what a send does while a turn
runs. The design refuses it with *"b1 is running; the next turn waits for it."* (announced as a
status) and keeps the editor editable. Confirmed by: Addendum A's run model or an Owner ruling. If
false (queued sends), the reason becomes *"queued after b1"* and a queued turn renders in the thread
with a *queued* outcome; nothing else moves.

## Findings for the specs (the conductor's, not this node's)

1. Addendum C §C1 and §B2: `Layout:StreamingThread` adopted; the earlier-turns paragraph; the Score
   outline's fate (the jump list).
2. Addendum A §A2: the canvas beside the composer becomes the on-demand split.
3. A sentence on a send while a turn runs (the Inferred item above), and the outcome vocabulary for
   a **stopped** turn and a **refused-before-start** send (which is not a turn).
4. Ruling 21's *"the canvas split is IN"* is honoured as on-demand; the Owner may want to say so.
