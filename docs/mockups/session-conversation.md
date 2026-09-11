---
id: mockup-session-conversation
title: "Session conversation — n turns, one editor, the Console as the reply side (Addendum C/D elevate mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [ui-design, mockup, addendum-c, addendum-d, session, conversation, thread, composer, prepare, task-class, contrast, ai-ux]
links:
  - { to: mockup-conversation-composer, rel: refines }
  - { to: ui-review-session-conversation, rel: documents }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: mockup-perspective-shell, rel: relates-to }
  - { to: note-session-design-thread-not-panes, rel: depends-on }
  - { to: note-session-design-decoration-line, rel: depends-on }
review-by: 2026-12-11
summary: >-
  The session document as a conversation: a thread of turns above one pinned editor, the session
  taking the whole tree in the Coding perspective (Ruling 47, no graph), each turn rendered from
  its envelope (the decoration line, provenance on demand, the sent bytes on demand) with the
  lane's reply folded beneath it. Forty-three harness states over DESIGN.md SC1–SC10, a 1 / 5 / 40
  turn-count axis, and a "chat-like" contract the page measures on itself.
---

# Session conversation

`docs/mockups/session-conversation.html` renders DESIGN.md's *The session is a conversation*
(SC1–SC10) over the ratified *The composer is a conversation* (PS-C1–PS-C7), behind
[`docs/reviews/ui-session-conversation.md`](../reviews/ui-session-conversation.md). Open it over
`file://`; no build step, no CDN, no network. `#state=gaps&turns=40&theme=light&vp=narrow` presets the
harness for a review link; the State control's option groups name every state and the rule each
one exercises is the legend's first words.

## Measured, not asserted

The verdict strip reports contrast failures over 53 classified pairings, targets under 24px,
dangling ARIA references walked over the rendered DOM, and misses against DESIGN.md's thread
contract, read from the layout on every change; the "chat-like" table prints the threshold each row
applied. The editor's top edge is read at 1, 5 and 40 turns inside one pass. Every state was
rendered headless (Edge) in both themes, at 1440 and 1024, and measured 0 / 0 / 0 / 0.

## What this mockup is not

Direction evidence only (UI-T4): the thread is WPF, the editor a WebView2 page; the announcements,
F6, the feed's PageDown / PageUp, the UIA list exposure and the DPI rows are the runtime Proof
Pack's, measured at the slice. The tier's derivation and the compile's eval are Addendum D's; the
design shows their results. Whether a send may be queued while a turn runs is not specified
anywhere: the mockup refuses it with a reason and records that as Inferred.
