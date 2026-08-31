---
id: mockup-editor-surfaces
title: "Editor & content surfaces — mockup"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [editor, code-viewer, prompt-draft, read-only, monaco, mockup, wpf]
links:
  - { to: spec-editor-surfaces, rel: documents }
  - { to: mockup-knowledge-explorer-mode, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Self-contained, dependency-free review mockup of the two editor & content surfaces
  (spec-editor-surfaces): a READ-ONLY code viewer (syntax-highlighted, read-only badge, shortfall
  banner, walkable typed-edge footer, with hard states code/markdown/overflow/loading/empty/error/
  unsupported-kind) and a PROMPT-DRAFT editor (staged badge, saved-with-layout note, a one-way
  Transfer to a NAMED ready session with the no-ready-session disabled state and the transferred
  confirmation). Review harness (surface · viewer-state · draft-state · theme · reduced-motion) and an
  in-artifact contrast audit (AA). Tokens are the project DESIGN.md chrome + Material Palenight syntax
  palette. Open `editor-surfaces.html` over file://.
---

## Rubric critique (structure before surface, DX24)

- **Archetype fit ✓** — the viewer is a dense reading surface (monospaced, line-numbered, gutter);
  the draft is a focused compose surface. Both are dock tabs (US-9), not a new shell concept.
- **Read-only invariant made visible (US-ED1)** — the `read-only` badge + no edit affordance; the
  viewer is `tabindex=0` for scroll/copy only. *Fix carried to build:* the real control (Monaco/
  AvalonEdit) must set its read-only option, not merely omit an editor — a viewer that is editable-
  but-unsaved still violates the invariant.
- **State completeness ✓ (US-ED8)** — code/markdown/overflow(shortfall)/loading(skeleton)/empty/
  error(+retry)/unsupported-kind all render; the typed-edge footer stays except in the empty state
  (edges belong to the node, not the content).
- **Transfer is consequential and honest (US-ED6/ED7)** — names its target session, disabled with a
  stated reason when no session is ready, and the transferred state clears the draft + shows the
  audit id (one-way). *Fix carried to build:* the target list must bind to the LIVE ready-session set
  (spec-terminal-sessions), not a static list.
- **Accessibility** — contrast AA (audited in-artifact: body/surface 13.9:1, code/sunken 13.5:1,
  muted 6.3:1); syntax tokens differ by hue *and* the comment token by italic, not colour alone;
  focus-visible rings on tabs/textarea/retry/transfer; high-contrast theme re-states the comment
  token so it does not drop below AA on pure black.
- **Highest-leverage next decision:** the renderer choice (Monaco-in-WebView2 vs AvalonEdit-native) —
  a `/define-architecture` spike, since it fixes airspace, licence surface, and the highlighting
  engine for every future code-content view.
