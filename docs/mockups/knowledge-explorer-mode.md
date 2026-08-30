---
id: mockup-knowledge-explorer-mode
title: "Knowledge Explorer mode — mockup"
type: doc
status: draft
owner: "@timianmalloo"
phase: ""
tags: [knowledge-graph, explorer, reader, dual-pane, mockup, wpf]
links:
  - { to: spec-knowledge-explorer-mode, rel: documents }
  - { to: mockup-graph-canvas, rel: relates-to }
review-by: 2027-02-28
review-suggested: []
summary: >-
  Self-contained, dependency-free review mockup of the full-window dual-pane Knowledge Explorer mode
  (spec-knowledge-explorer-mode): the activity rail + a body-wide graph|reader split, with the reader's
  hard states (code/markdown/html/empty/loading/error/unsupported-kind/overflow) and the graph's
  loading/empty/too-large states, a review harness (state · viewport · theme · reduced-motion) and an
  in-artifact contrast/target audit. Tokens are the project DESIGN.md (chrome + the separate syntax
  palette + provenance). Open `knowledge-explorer-mode.html` over file://.
---

# Knowledge Explorer mode — mockup

The reviewable target for the [full-window dual-pane Explorer mode spec](../specs/knowledge-explorer-mode.md).
The `.html` beside this file is the artifact; this note is its graph node.

**What it demonstrates**

- The **mode chrome**: the activity rail with the Explorer item active (accent bar + raised pill), the
  menu / title / status strips retained around a body-wide surface (US-E2).
- The **dual-pane composition**: graph region (search · 2D/3D · Fit · canvas) `|` reader region, with a
  draggable splitter; a **narrow viewport** flips it to stacked (US-E3, US-E8).
- The **reader by kind** (US-K3 inherited): a syntax-highlighted code node (DESIGN.md Palenight
  palette), rendered markdown, rendered/sanitised html — each with a **metadata** block and a
  **typed-edges** list that is the **walk** affordance (US-K4).
- The **hard states**, switchable in the harness: reader empty (“Select a node to read it”),
  loading (skeleton), error (“couldn’t be read” + recovery), unsupported-kind fallback (metadata +
  edges), overflow (a long file scrolls within the pane); and graph loading / empty / too-large
  (US-K12) (US-E7).
- The **review harness** (never ships): state · viewport · theme (dark / high-contrast) · reduced
  motion, plus an in-artifact **contrast** audit (worst text/surface ratio) and target-size note.

**What it defers** (flagged in the spec): the **graph↔reader keyboard-cycle** (routing the canvas
boundary Tab into the reader) is described but is an interaction to be proven in `/design` with a
focus-integration test; the **reader content-fetch contract** (the node’s source/metadata is a Core
query, like `GraphOverview`) is for `/define-architecture`.
