---
id: note-uml-diagram-fidelity-roadmap
title: "Decision note — full-fidelity UML class & sequence diagrams"
type: decision-note
status: accepted
owner: "@timianmalloo"
phase: "2"
tags: [uml, class-diagram, sequence-diagram, visualization, class-diagram-surface]
links:
  - { to: architecture, rel: relates-to }
review-by: 2027-02-28
summary: >-
  The class diagram now renders variable-height, three-compartment UML classifier boxes
  (name / attributes / operations) sized to each type's members, with «interface» stereotypes,
  italic interface names, monospace member lines, and correct generalization/realization
  arrowheads. This note records the remaining UML class-diagram fidelity gaps and the design +
  Core data contract for a UML sequence-diagram surface.
---

# Full-fidelity UML class & sequence diagrams

## Context

The user asked for full-fidelity UML — "really understand UML symbols and styles for class
diagrams and sequence diagrams." The class diagram started as a relationship render, then gained
three-compartment boxes with async-filled members. This turn it became a real UML **classifier**
render: variable box height (each box measured and sized to its members), three compartments
(name / attributes / operations) each separated by a rule, «interface» guillemet stereotypes,
italic interface names (UML convention for interface/abstract), monospace member lines for
alignment, and up to 15 members per compartment with a "…+N more" and a "(+K more not listed)"
footer when the extractor truncated (`members_truncated`).

## What is UML-correct now (class diagram)

- **Classifier box** — three stacked compartments, content-sized (variable height).
- **Visibility glyphs** — `+ public`, `- private`, `# protected`, `~ package` (from the extractor's member strings).
- **Stereotype** — `«interface»` above the name; interface names italic.
- **Generalization** — solid line, hollow triangle at the base (parent) end.
- **Realization** — dashed line, hollow triangle at the interface end.
- **Attribute / operation split** — a member with `(` is an operation, otherwise an attribute.

## Remaining class-diagram fidelity gaps (roadmap, each needs data)

| UML feature | Symbol | Data needed (Core) | Status |
|---|---|---|---|
| **Dependency** | dashed line, open (stick) arrowhead | `depends_on` (exists, 7585) — needs a **draw toggle** (default off) to avoid a tangle | designed, deferred |
| **Association** | solid line, optional open arrow + multiplicity | an `associates`/`references` predicate with role + multiplicity (not emitted) | blocked on Core |
| **Aggregation** | solid line, **hollow** diamond at the whole | a `has_a` (shared) predicate (not emitted) | blocked on Core |
| **Composition** | solid line, **filled** diamond at the whole | a `owns` (composite) predicate (not emitted) | blocked on Core |
| **Static member** | underlined | a `static` flag on `has_member` (member string has no modifier) | blocked on Core |
| **Abstract class / operation** | italic | an `abstract` flag on the type / member | blocked on Core |
| **Derived attribute** | leading `/` | a `derived` flag | blocked on Core |
| **Multiplicity / role names** | `1`, `0..*`, role labels at ends | association metadata | blocked on Core |

The three drawn relationship kinds (generalization, realization) are the ones the extractor
emits today with clean type→type semantics. **Dependency** is the next drawable layer (data
exists) behind an opt-in toggle; the rest need new extractor flags/predicates from Core.

## Sequence diagram — design + Core data contract

A UML sequence diagram shows an **ordered interaction** between participants over time. The
surface (a new `SequenceDiagramSurface`, mirroring the class-diagram surface pattern) would render:

- **Lifelines** — a head box per participant (actor/object) at the top, a vertical dashed line descending.
- **Activation bars** — thin rectangles on a lifeline marking when it is executing.
- **Messages** — ordered top-to-bottom:
  - synchronous call: solid line, **filled** arrowhead;
  - asynchronous call: solid line, **open** arrowhead;
  - reply/return: **dashed** line, open arrowhead;
  - self-message: a loop back to the same lifeline;
  - create: dashed line to a lifeline head lower down; destroy: an `X` at a lifeline's end.
- **Combined fragments** — `alt` / `opt` / `loop` / `par` frames with a pentagon label.

**The blocker — there is no ordered-call data.** The store has no `calls` predicate and no call
**ordering / sequence index**. A sequence diagram over software artifacts needs, per interaction:
an ordered list of `(fromParticipant, toParticipant, message, kind[sync|async|return], order)`.

**Core data contract ask** (filed in `session-contracts`): emit a `calls` assertion per call site
with subject = the calling method/type, object = the called method/type, and metadata carrying a
**sequence ordinal** within the caller and the **call kind**. A first slice can scope to a single
method's outgoing call chain (one activation), which is enough to render a real sequence diagram.
Until then, `SequenceDiagramSurface` can be built and tested against a **stubbed interaction model**
(the mocked-seam pattern), so replacing the stub with the real `calls` query is a substitution.

## Decision

Ship variable-height three-compartment class boxes now (done, verified). Advance class-diagram
fidelity incrementally: **dependency edges (toggle) next** (data exists), then the association/
aggregation/composition/static/abstract features as Core adds the flags. Build the sequence-diagram
surface against a stubbed interaction model and file the `calls`-with-ordering contract to Core.
