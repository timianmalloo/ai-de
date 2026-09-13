---
id: note-code-atlas-proposal-provenance
title: "Code Atlas delivery inputs and private-reference boundary"
type: doc
status: draft
owner: "@timianmalloo"
phase: "addendum-e-grounding"
tags: [code-atlas, provenance, privacy, fleet]
links:
  - { to: spec-addendum-c-perspectives, rel: relates-to }
  - { to: spec-uml-erm-surfaces, rel: relates-to }
  - { to: architecture, rel: relates-to }
review-by: 2026-12-12
summary: >-
  Records the user-reviewed Code Atlas proposal as input to the next addendum without importing
  its private reference source and session captures into the implementation branch. Distinguishes
  development-fleet model choices from the existing product-runtime conductor contract.
---

# Code Atlas delivery inputs

## User direction

On 2026-09-12 the user requested `/specify` of the next addendum from the reviewed proposal/mockup,
`/define-architecture` for the overall architecture, and fleet implementation using an Astra Owner,
an Astra Conductor and work-appropriate GPT execution models, principally GPT-5.5. The user
explicitly required coordination with the existing Claude conductor and its sub-agents.

The same conversation established these product requirements:

- File-first developers retain a real solution/project/folder/file explorer.
- Visual-first users start with a graph or entry points; neither entrance replaces the other.
- Navigation crosses system, component, type, member and concrete source altitude, preserving
  identity and a return path. Files and types are not interchangeable.
- Preserve conceptual class maps while supporting concrete members, UML and source-backed
  behavior/sequence/activity views; show supported coverage and unknowns.
- Derive named Azure layers and components from declarations and code evidence; deployment
  configuration, grants and observed runtime traffic remain different facts.
- Compare implementation with specification/architecture intent, preserving document authority,
  status, revision and scope. Planned targets are not automatically current-code defects.
- Link discrepant implementations to relevant prompts, human/authorized Owner/Conductor
  decisions, audit entries and session turns. Missing decision evidence is not "no decision."
- Use static evidence and bounded model interpretation together, without laundering inference
  into extracted or observed truth.

## Proposal source, not a new runtime dependency

The reviewed proposal is committed at `1065a851` on the local branch `proposal/code-atlas`:

- `docs/proposals/code-atlas/README.md`
- `docs/proposals/code-atlas/index.html`
- `docs/proposals/code-atlas/mockup.template.html` and generated `mockup.html`
- `docs/proof/code-atlas-terrace.md`

Its worktree is `C:\Projects\ai-de-proposal-code-atlas`. Its fixture uses TheTerrace at
`dba2a29c868d9844cb144d693555265d071ecdeb`. The complete tracked-path inventory, curated
source excerpts and a minimal historical user-decision excerpt were local review material,
not authorization to publish another repository's source or session history.

The delivery branch starts from current AI-DE main (`b0e092b5` at grounding). It does **not**
merge the proposal branch or embed that private fixture into product code. Specs may describe
the demonstrated scenarios and cite this provenance note. Implementation tests must use safe
purpose-built fixtures, or an explicitly configured read-only local acceptance corpus.

The mockup proved browser interactions, not native WPF behavior, automatic whole-repository
analysis, runtime call order, deployed Azure state or model quality. No prototype shortcut is
automatically an implementation contract.

## Authority and coexistence

The existing Conductor spec and Addenda A-D remain governing inputs. The currently observed
next letter is E; reservation/acceptance is a coordination and Owner decision, not a filename
guess. The Architecture perspective already exists in Addendum C's set of three.

The requested Astra/GPT fleet is the engineering arrangement for this work. It is not, without
a separate explicit ruling, a change to the product's runtime conductor/provider contract.

`docs/collaboration/session-contracts.md` section 2 remains the sole ownership register.
Native seam request `req-01M2B86TXF7SHG61B31P4H4173` asks the Claude conductor to acknowledge
Addendum E and agree narrow authored paths/integration seams. A lease is not an ownership grant.
No silence, stale liveness note, unclaimed path or separate worktree authorizes crossing a lane.
