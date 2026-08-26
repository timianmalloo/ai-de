---
id: review-ui-workbench
title: "UI review — AI-DE dockable workbench"
type: doc
status: accepted
owner: "@timianmalloo"
phase: "0"
tags: [ui-review, workbench, accessibility, craft-gate]
links:
  - { to: mockup-workbench, rel: documents }
  - { to: spec-ai-native-ide, rel: relates-to }
review-by: 2027-02-26
review-suggested:
  - { by: spec-ai-native-ide, on: 2026-08-26, reason: "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice" }
summary: >-
  Rubric critique of the workbench mockup, structure before surface, with the deterministic craft
  gate folded in. Records one justified detector suppression and the accessibility regression the
  gate caught mid-review.
---

# UI review — AI-DE dockable workbench

**Mode:** create. **Triggered standards:** UI-T4 (native Windows desktop — Fluent 2 authoritative).
UI-T1, UI-T2, UI-T3 explicitly do **not** fire: the chrome is not a quantitative surface (the views
*inside* it are), no assets are generated, and v1 makes no model call.

## Measurement (DX23)

| Metric | Count |
|---|---|
| Distinct type sizes | 2 (12px meta, 13px body) |
| Distinct colours in component rules | 0 raw — all token references |
| Competing focal points | 1 (the focused stack, accent-bordered) |
| Animated moments on a layout operation | **0** |
| Craft-gate findings, final | 3 Minor (all one suppressed rule) |

## Rubric findings — structure before surface (DX22/DX24)

| # | Dimension | Severity | Finding | Disposition |
|---|---|---|---|---|
| 1 | Archetype fit | — | `MultiPanelWorkstation` verified against the task shape: the operator *reads in parallel* (evidence side by side) and *enters serially* (one prompt, one terminal). A user-arranged tree serves both; `MasterDetail` served neither. | Resolved in the spec |
| 2 | IA / flow | — | The drag path and the keyboard path converge on **one** flow with one commit point, rather than a primary path plus a lesser accessible path. | Held |
| 3 | State completeness | — | 14 states rendered including the four the category usually skips: at-minimum, partial restore, unreadable layout, locked. | Held |
| 4 | **Accessibility** | **Major (fixed mid-review)** | Chasing a flat-type-hierarchy Minor, I pushed the chrome to 11px and the gate returned **7 Major `tiny-text` findings**. A hierarchy nicety had been traded for a WCAG floor. | **Fixed** — nothing below 12px; body 13px (VS Code parity at `Density:Compact`). |
| 5 | Token discipline | Major (fixed) | The floating pane's shadow was a raw `rgba(0,0,0,.5)`. | **Fixed** — promoted to `--elev-dialog`. |
| 6 | Craft / generic tell | Minor (fixed) | Floating pane had a 1px border **and** a 24px shadow — the `gpt-thin-border-wide-shadow` signature. | **Fixed** — elevation alone says "floating"; the border was doing the same job twice. |
| 7 | Hierarchy | Minor | Type scale is 12/13px — a 1.08 ratio, below the detector's 1.25. | **Accepted deviation.** Compact IDE chrome carries hierarchy through **weight, colour and space**, not type size. Adding size steps to chrome makes it louder, not clearer. The evidence surfaces *inside* the panes carry the real type hierarchy. |
| 8 | Craft | Minor ×3 | `cramped-padding` on `.tabs`: children flush against the bottom border. | **Suppressed with reason (CD16).** A tab meeting its strip's bottom border is what makes it read as a tab — every exemplar does this. Side padding was added; the bottom flush is deliberate. |

## What the detector could not judge (CD13/CD14)

A clean run is a floor, never a verdict. The detector cannot see that the archetype fits, that the
drag and keyboard paths converge, that `not recorded` is rendered rather than blank, or that the
copy is *true* — e.g. that "Restored 5 of 6 panes" names the missing one instead of saying
"some panes were unavailable". Those were judged by the adversarial layers, not the gate.

## Ranked plan

**Must-fix (done in this run):** the 11px accessibility regression; the raw shadow token; the
border+shadow tell.

**Should-fix-next:** prove the keyboard-resize interaction against a real screen reader. The mockup
demonstrates the *design*; it does not prove AvalonDock can be made to behave this way, and
ADR-0012 records that the library ships zero automation peers.

**Worth doing:** collapse-to-icon with per-surface icons (Eclipse trim stacks / Photoshop icon
docks). Currently the collapsed strip shows names vertically, which works but scales poorly past
three surfaces.

**Highest improvement-to-effort change (DX25):** *the keyboard-selected splitter edge with its
direction cap.* It is roughly twenty lines of styling and it converts pane resize from
mouse-only — the state of the entire category, including VS Code — to fully keyboard-operable and
announced. No exemplar has this; it is the single thing that makes the accessibility claim real
rather than aspirational.
