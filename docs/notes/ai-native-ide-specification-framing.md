---
id: note-ai-native-ide-specification-framing
title: "Decision note — AI-native IDE specification framing"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "0"
tags: [ai-native-ide, derived-views, ui-archetype, specification]
links:
  - { to: spec-ai-native-ide, rel: relates-to }
  - { to: knowledge-hub, rel: depends-on }
review-by: 2027-02-20
review-suggested:
  - { by: spec-ai-native-ide, on: 2026-08-26, reason: "US-9 dockable workbench added; archetype corrected to Layout:MultiPanelWorkstation + Persistence:LocalDevice" }
summary: >-
  Keeps the AI-native IDE specification technology-neutral while adopting code-derived views as
  its source-of-truth boundary and B1 Keyboard-Velocity as the workspace-shell interaction
  archetype.
---

# Decision note — AI-native IDE specification framing

**Decision:** The specification treats visual models as derived, provenance-labelled views over
repository/workspace evidence, not as editable architecture records. It records a B1
Keyboard-Velocity workspace shell because the primary job is expert navigation across many
sessions and evidence surfaces. The graph store, terminal host, diagram renderer, and prompt
editor remain spike decisions.

**Why:** The existing knowledge base invalidates the seed's Kuzu assumption and establishes that
code-derived views avoid the model-drift failure mode. The product owner's workflow requires rapid
keyboard/terminal operation with inspectable visual evidence, which maps to the B1 archetype.

**Alternatives dismissed:** A canvas-only shell would hide operational navigation; a generic
dashboard would flatten the high-density, keyboard-first job; selecting named implementation
libraries now would decide architecture before the required spikes.

**Confidence:** Verified for the derived-view constraint; Inferred for the initial B1 archetype
fit, pending independent operator research.
