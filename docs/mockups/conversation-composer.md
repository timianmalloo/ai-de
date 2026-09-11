---
id: mockup-conversation-composer
title: "Conversation composer — one editor, derived structure, compiled on demand (Addendum C elevate mockup)"
type: doc
status: draft
owner: "@timianmalloo"
phase: "next-delivery (after F5 merges — Ruling 51)"
tags: [ui-design, mockup, addendum-c, composer, conversation, session-settings, contrast, ai-ux]
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
  The Coding composer as a conversation (Ruling 57): one editor that is the largest and brightest
  element in the pane, the goal block's Goal · Done when · Not in scope derived beneath it as
  editable lines, one inherited-settings line (fan-out ceiling and budget from the session, no
  tier, no override), the write scope derived from an @path mention, the compiled prompt on demand
  carrying the tier the compile step attached, and the Console streaming beside it. Ten hard
  states, a no-provider variant, four tier-decoration states, and a density audit measured on the
  rendered page.
---

# Conversation composer

`docs/mockups/conversation-composer.html` renders DESIGN.md's *The composer is a conversation*
(PS-C1–PS-C7) behind [`docs/reviews/ui-perspective-shell.md`](../reviews/ui-perspective-shell.md).
Open it over `file://`; no build step, no CDN, no network.

## What it renders

The session header (28px: name · class chip · shape control · *Session settings* · backend health),
then the composer: **the editor** (the WebView2 page in the shell; here a `role=textbox` island on the
sunken ground, focus on open, an `@`-mention chip inline, the mention picker at the caret), then four
lines of 12px type beneath it: the **derived structure** (a disclosure holding Goal · Done when · Not
in scope as editable lines with *derived* / *edited* / *missing* marks, glyph + word, never colour
alone), the **inherited settings** line (*fan-out ≤ 3 · budget 40k tokens · from session settings*,
the text is the link, no tier, no override), the **write scope** line(s) (*from your mention*), and
the **compiled prompt** disclosure (collapsed; the exact outgoing text; the **tier decoration** the
compile step attached: *not derived yet* → *~ T1 derived* → *T1 confirmed*, and the T0-with-ceiling
reading). The send row: Send with its bound chord, the shape badge, the reason beside a disabled
Send, the refusal panels. The Console canvas beside it holds the thread.

## States (the harness's State control)

default · empty / first message · loading (editor starting, Send disabled with reason) · deriving
(provider working, one-line skeleton, typing never blocked) · **refused: T2 content gaps** (every gap
marked at once, one reason each) · **refused: no write scope** (the refusal names `@path` and opens the
picker) · sending · send failed (draft kept, *updated* on the compiled header) · editor error (Retry)
· overflow (a long draft, four mentions, four scope lines). Plus **assist provider present / absent**
(D-5: absent ships first, lines empty and editable with the spec's copy), the four **tier
decoration** states, **modes in catalog** 1 / 5, theme, viewport, persona (operator / keyboard-only
showing the P-13 focus order across the WebView2 boundary / screen reader with the live-region
trace), reduced motion.

## Measured, not asserted

The verdict strip reports contrast failures over 26 classified pairings, targets under 24px, and
**density misses** against DESIGN.md's "not chunky" contract, read from the rendered layout: chrome
above the editor (28px), the editor's share of the composer zone (≥ 45%), rows beneath the editor
at rest (4), bordered fields at rest (0), type sizes in the composer (≤ 3), per-prompt settings
fields (0).

## What this mockup is not

Direction evidence only (UI-T4). The real editor is a WebView2 page whose pairings the runtime census
reports *not measured* until it walks the DOM; the token names this mockup uses are the CSS variables
the host injects on `host.init` (PS-C4), so the page and the design read one palette. How the tier is
derived, and the compile step itself, are not designed here (the operator will specify them).
