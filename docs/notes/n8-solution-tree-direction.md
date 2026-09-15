---
id: note-n8-solution-tree-direction
title: "N8 direction: Solution tree is a WPF TreeView navigator; HTML is direction; Reveal lives on the node menu"
type: decision-note
status: draft
owner: "@timianmalloo"
phase: "understanding-views"
tags: [decision-note, D-0, solution-tree, ui-design, N8]
links:
  - { to: mockup-solution-tree, rel: relates-to }
  - { to: spec-understanding-views, rel: relates-to }
  - { to: spike-d0-tree-toolkit, rel: depends-on }
review-by: 2027-03-15
review-suggested:
  - { by: mockup-solution-tree, on: 2026-09-15, reason: "N8 ui-design: Solution tree mockup settles dual-activate, Unindexed chrome, and hard states; HTML is direction only" }
summary: >-
  N8 closes three below-ADR calls: HTML mockup is direction only; pointer Reveal in graph
  uses the existing node menu; default Left zone is Inferred pending design-slice. No new
  colour token — Unindexed stays {colors.unverified}.
---

# N8 direction: Solution tree is a WPF TreeView navigator; HTML is direction; Reveal lives on the node menu

- **Kind:** decision
- **Confidence:** mix — toolkit **Verified** (spike); Left zone **Inferred**; native PASS **Flagged**
- **Made during:** `/ui-design` create, session `understanding-views-ui-design`

## The call

1. The product control is WPF `TreeView` with the N7 attachments. The committed HTML mockup is reviewable direction (DX8) and does not implement the tree.
2. Pointer path for **Reveal in graph** (N4 UX residual): the existing node context menu, not a second toolbar button. Primary pointer activate remains double-click = View source (same as Enter).
3. No new token. Unindexed and Not recorded use `{colors.unverified}` plus word plus glyph.
4. Default docking Left beside Graph is shown in the mockup so composition is reviewable. Spec left the zone to design-slice; UX-1 still holds via View → Show Solution tree.

## Alternatives dismissed

- WebView2 HTML tree in product — rejected in the spike (wrong host).
- Extra command bar for Reveal — fails Simplifier; the node menu already exists in the IDE idiom.
- `{colors.inferred}` / `{colors.stale}` for Unindexed — spec UI-2 fail.
- 44px rows — spec UI-10 fail (rail size, not compact list).

## Validation condition

Holds until UV-1 ships the spike attachments and a non-author UX & Accessibility review of the mockup (or the native surface) records PASS. Left-zone Inferred flips when `/design-slice` names the default layout.

## Promotion rule

If a later slice changes dual-activate or the unindexed token, promote that change to an ADR. This note does not replace ADR-0038 or the spec.
