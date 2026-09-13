---
id: mockup-session-conversation
title: "Session conversation — n turns, one editor, the conversation as prose · reasoning · tool call+result · outcome; the Console one row per message (Addendum C/D elevate mockup, Rulings 80–83/87)"
type: doc
status: draft
owner: "@timianmalloo"
phase: "Addendum C/D · Conversation lane CV-5 (design node D3, 2026-09-13)"
tags: [ui-design, mockup, addendum-c, addendum-d, session, conversation, thread, composer, prepare, task-class, contrast, ai-ux]
links:
  - { to: mockup-conversation-composer, rel: refines }
  - { to: ui-review-session-conversation, rel: documents }
  - { to: ui-review-operator-findings-2026-09-13, rel: documents }
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-addendum-d-compile-step, rel: relates-to }
  - { to: note-addendum-c-council-rulings, rel: relates-to }
  - { to: mockup-perspective-shell, rel: relates-to }
  - { to: note-session-design-thread-not-panes, rel: depends-on }
  - { to: note-session-design-decoration-line, rel: depends-on }
review-by: 2026-12-13
summary: >-
  The session document as a conversation, elevated on 2026-09-13 to Rulings 80–83 and 87: docked in
  Coding's Left zone (extent 1.3, measured against the 96ch measure), the editor filling the body at
  0 turns and resting at 280px with turns, each turn's reply side rendered from Coalesce(events) as
  prose (rendered markdown, no link activation) · a collapsed dim Thinking line · tool call+result
  items · the outcome line last, the Console split one row per message with its chunk count — an
  identity the page measures on itself. Harness axes: State, Turns, Layout (3: the operator's screenshots, Ruling 83's text as written, D3's proposal),
  Theme, Viewport (incl. short and the operator's 2560 × 1600), Persona, Motion.
review-suggested:
  - { by: mockup-perspective-shell, on: 2026-09-13, reason: "D3 /ui-design elevate: Coordination as host C (Ruling 84), Coding re-cut (Ruling 83); the body-hiding gating selector fixed" }
---

# Session conversation

`docs/mockups/session-conversation.html` renders DESIGN.md's *The session is a conversation*
(SC1–SC10) over the ratified *The composer is a conversation* (PS-C1–PS-C7), behind
[`docs/reviews/ui-session-conversation.md`](../reviews/ui-session-conversation.md). Open it over
`file://`; no build step, no CDN, no network. `#state=gaps&turns=40&theme=light&vp=narrow` presets the
harness for a review link; the State control's option groups name every state and the rule each
one exercises is the legend's first words.

## Measured, not asserted

The verdict strip reports contrast failures over 63 classified pairings, targets under 24px,
dangling ARIA references walked over the rendered DOM, and misses against DESIGN.md's thread
contract as amended by Rulings 80–83, read from the layout on every change; the "chat-like" table
prints the threshold each row applied. The editor's top edge and height are read at 1, 5 and 40 turns
and at 0 turns inside one pass; the Console split's row count is read against `Coalesce(events)`;
the density row is read per layout. Headless (Edge) the strip reads
0 contrast fails · 0 targets under 24px · 0 dangling references in every state D3 measured; the
chat-like misses it reports are the density findings `ui-operator-findings-2026-09-13.md` §2c
carries as numbers (one turn at 1440 × 900; none with the startup terminal across the bottom).

## What this mockup is not

Direction evidence only (UI-T4): the thread is WPF, the editor a WebView2 page; the announcements,
F6, the feed's PageDown / PageUp, the UIA list exposure and the DPI rows are the runtime Proof
Pack's, measured at the slice. The tier's derivation and the compile's eval are Addendum D's; the
design shows their results. The `agent.thought` row's shape is Inferred until a frame is captured
(Ruling 82 condition 1); the density at 1440 × 900 (one turn in the operator's layout; none with the
startup terminal across the bottom) is measured here and put to the Owner in the review's §7, not argued.
