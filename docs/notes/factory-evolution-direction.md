---
id: note-factory-evolution-direction
title: "Factory exploration: accepted constraints and deferred decisions"
type: decision-note
status: exploratory
owner: "@timianmalloo"
phase: "proposal"
tags: [factory, desktop, authoring, review]
links:
  - { to: proposal-local-software-factory, rel: relates-to }
  - { to: mockup-factory-workbench, rel: relates-to }
review-by: 2026-11-17
summary: "Records the user's local-desktop constraint and PR-question/rich-authoring additions, while keeping the recommended work-centered direction exploratory."
---

# Accepted constraints, exploratory direction

The user explicitly requires a **local desktop workbench**. This is settled. They added two
requirements to the exploration: human reviewers can ask questions of the PR, and authoring
should become a rich prompt editor with multi-edit and a Back/Forward prompt stack.
Both are incorporated in the proposal and working mockup without replacing the original review,
research or six factory journeys.

The recommended direction is **Inferred, not accepted**: Factory / Review / Understand over one
versioned intent/evidence model, with work items primary and graph investigation fully accessible.
The graph-first alternative remains a real option. Provider/tool authority, spec authority,
human-review waivers, storage/schema details and first integrations remain `/specify` decisions.

Richer authoring must retain one canonical source and the existing host-owned compiler/send gate.
Editor undo, draft revision navigation and submitted envelopes are different histories. A context
mention is not an attachment or a write permission. The mockup implements rich document editing,
previewed exact multi-edit, prompt revisions/branching and local navigation; true multi-cursor,
native persistence and production sending remain unimplemented.

No provider was connected, no actual PR was reviewed or approved, and no production remediation
or remote issue was created. The source review's two reproduced correctness issues remain proposed
backlog work for human triage.
