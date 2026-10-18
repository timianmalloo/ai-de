---
id: note-workbench-loops-direction
title: Work and system are equal entry paths
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: proposal
tags: [direction, editor, auth, coordination]
links:
  - { to: proposal-workbench-loops, rel: documents }
  - { to: spec-workbench-loops-ux, rel: relates-to }
review-by: 2026-11-18
summary: Records the user's explicit direction for the exploration: two legitimate entry paths, native docking, first-class editor/run, core IntelliSense and explicit harness-owned sign-in. Production recommendations remain exploratory.
---

# Settled direction

The user chose a local dockable desktop workbench with Factory, Review and Understand. Features
belong under the corresponding left section, not across a right-pane feature tab strip. The prompt
editor and execution/evaluation surface are peers. The editor must exceed CLI authoring through
immersive text/code composition, autocomplete, IntelliSense, context inspection, history and precise
editing. A terminal remains an explicitly opened secondary surface.

Work-first and system-first are equally legitimate paths. An investigation can produce a useful
answer, visualization or checklist without creating work. Promotion is explicit and retains origin.
No global preference is silently chosen for every user.

Authentication is explicit and native-harness-owned. Reuse an existing signed-in session; open its
native sign-in only when required. The working benchmark provides the isolation/refresh/environment
precedent. AI-DE does not transparently broker subscription authentication or silently fall back to
API billing. ACP is one transport option; native transports keep their actual capabilities.

# Proposed, not settled implementation

Reuse CodeMirror 6 first, with a qualification spike before choosing a full rich-document engine.
Use existing DockHost and RunEventRelay seams. Project current pack coordination records before
adding command controls. These are recommendations in the proposal, not approved production
architecture. No product backend or provider integration was implemented in this episode.

# Rejected directions and why

- A task-only funnel would fail the user's system-first exploration need.
- A terminal transcript wrapped in cards would not establish a better authoring experience.
- A uniform six-provider permission dialog would invent native capabilities.
- An app-owned OAuth/token broker would contradict the clarified native-harness approach.
- A second C# leader/request authority would diverge from the current AI-Forward protocol.
- A fresh heartbeat, completed response or checked checklist box cannot establish accepted work.

Independent source and provider reviewers checked these boundaries. Remaining production questions
are listed in the proposal's `/specify` handoff rather than being presented as settled requirements.
