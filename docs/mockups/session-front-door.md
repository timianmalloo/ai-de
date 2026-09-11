---
id: mockup-session-front-door
title: "Session front door — operator-feedback elevate mockup"
type: doc
status: draft
owner: "@claude-ui-elevation"
phase: "phase-1-front-door"
tags: [ui-design, mockup, composer, task-class, canvas-modes, activity-rail, contrast]
links:
  - { to: ui-review-operator-feedback, rel: documents }
  - { to: spec-app-facelift, rel: relates-to }
  - { to: mockup-facelift-elevate, rel: refines }
review-by: 2026-12-10
summary: >-
  A self-contained, dependency-free mockup of the five surfaces named in operator feedback on the
  running app: the composer as a notebook of blocks, the Console-only canvas mode strip under
  Ruling 45, the New Session sheet's required task class, the activity rail, and a contrast audit
  computed from live styles rather than claimed in prose. Ships the standard review harness plus a
  catalog-size control that switches the mode strip between one, two and five modes.
review-suggested:
  - { by: ui-review-operator-feedback, on: 2026-09-11, reason: "New elevate review: token-coverage rules TC1-TC6, required-field rules RQ1-RQ6, canvas mode strip MS1-MS5 and activity rail AR1-AR5 added to DESIGN.md; ranked plan for the running app" }
---

# Session front door — operator-feedback elevate mockup

`docs/mockups/session-front-door.html` renders the design behind
[`docs/reviews/ui-operator-feedback.md`](../reviews/ui-operator-feedback.md). Open it over `file://`;
there is no build step, no CDN and no network call.

## The five surfaces

1. **Composer** (feedback 5 and 6). The pane's largest element becomes the thing you type into. The
   Score of prior blocks reads above; the composer card is an island pinned below it with the two
   submission shapes, the editor, the context-recipe chips, and one explicit send carrying its chord.
   **Compiled view stops being a permanent field and becomes a disclosure** — it was read-only all
   along, and being the only white box on a dark screen is why it read as the editor. The lease
   sentence becomes the reason attached to a disabled Send.
2. **Canvas mode strip** (feedback 2, under Ruling 45). One 28px row whose geometry never changes and
   whose population is driven by catalog size. At one mode it renders the pane title; at two or more
   it renders the tab strip and the split control appears. All three sizes render side by side, and
   the harness switches the composer's live strip between them.
3. **New session sheet** (feedback 1). Task class stays required and stays undefaulted. What changes
   is that the set is visible, the explanation is at the field in consequence language rather than in
   the word "cohort", the required state is a visible state that flips to *Answered*, the disabled
   Create button states its reason beside itself, and the operator's own last answer is offered as a
   one-click suggestion that is never a preselection.
4. **Activity rail** (feedback 3). Three inert placeholders go; Explorer stays, because it is the only
   door to Explorer mode in the product; New Session arrives as the rail's one primary action, above
   the destination group and separated from it.
5. **Contrast audit** (feedback 4). Sixteen pairings **computed from the live resolved styles** and
   recomputed on every theme change, against the correct WCAG floor for each. The existing mockups in
   this folder state their ratios as static prose; that is the artifact-level form of the same failure
   the product has.

## Review harness

Persona · state (default / first-run / loading / error / **refused** / overflow) · **modes in catalog**
(1 / 2 / 5) · theme (dark / light / high-contrast) · viewport (desktop / wide / tablet / mobile) ·
reduced motion. The verdict strip reports computed contrast failures and undersized targets; it is
measured on every change, not typed in. The harness is review chrome and never ships.

## Hard states rendered, not described

First-run with a Wayfinder that teaches the first action · loading as a skeleton shaped like the
content it replaces · error with its real recovery affordance · **refused / wrong answer** with what
the lane declined and three real next actions (UI-T3) · overflow with a 41-block Score, a 200-character
path, a 40-line context recipe and a capped count rendered as a lower bound.

## What this mockup is not

It is **direction evidence only**. The product is WPF (UI-T4), so no HTML artifact can clear native
accessibility, keyboard, DPI, windowing or signing proof, and the accessibility veto in the review is
explicitly **not** cleared by this file. Icons are Lucide-style geometry (MIT); the real shell would
ship them as XAML `Path` data. No generated imagery is used anywhere (UI-T2 does not fire).
