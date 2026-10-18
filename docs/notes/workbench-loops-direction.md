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
review-suggested: []
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

Independent source and provider reviewers checked these boundaries. The functional specification
now defines the required behavior; its qualification ledger separates native evidence still needed
from settled user direction.

# Functional specification follow-up — 2026-10-18

The user explicitly required two distinct journeys. The promoted
[functional specification](../specs/workbench-loops-ux.md) therefore gives J-W and J-S separate
triggers, records, stages, closure and recovery. J-W starts with intended work and ends in a
reviewable delivery disposition. J-S starts with a repository or PR question and may close with a
grounded answer, partial explanation or unresolved findings without creating a Work Item. A PR is
a possible subject in either journey, not a third journey. Factory, Review and Understand remain
navigation sections through which either journey can pass.

An explicit bridge creates linked work from an investigation while retaining its origin; opening
an investigation from work preserves the originating work and its outstanding obligations. Neither
operation silently converts evidence confidence, approvals or permissions. The conceptual model
keeps notebooks and investigations independent of mandatory Work Item ownership.

The specification has 30 stories and 90 acceptance criteria across functional, UX and UI layers.
Independent domain/test, UX/accessibility and security/AI reviews passed for specification scope.
Native implementation readiness remains unclaimed. The acceptance-state finding was resolved by
making unresolved review state distinct from accepted work in AC-P02c. The next architectural work
must preserve this distinction in both data invariants and executable acceptance tests.
