---
id: adr-0040-journey-purpose-identity
title: "ADR-0040 — Journey purpose is a durable identity distinct from navigation, session and pane; bridges are explicit linked records"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "workbench-loops"
tags: [architecture, journey, work, investigation, bridge, acceptance, workbench-loops]
links:
  - { to: architecture, rel: implements }
  - { to: spec-workbench-loops-ux, rel: implements }
  - { to: adr-0039-workbench-records-durable-representation, rel: depends-on }
  - { to: adr-0045-perspective-set-factory-review-understand, rel: relates-to }
  - { to: note-workbench-loops-direction, rel: depends-on }
review-by: 2027-04-18
summary: >-
  A journey (work or investigation) is its own dimension row with an immutable purpose; Factory,
  Review and Understand are perspectives, a session is a native execution context, a pane is a
  surface — none of them is the journey. Promotion and linked investigation are append-only link
  facts created only by explicit confirmation. Work acceptance and investigation conclusion are
  computed folds over obligations, evidence links and dispositions, never stored flags.
review-suggested:
  - { by: architecture, on: 2026-10-18, reason: "2026-10-18 workbench-loops architecture: §WL added, ADR-0039..0045 proposed, Perspective set re-rowed (ADR-0045 supersedes ADR-0017/0030 rows)" }
  - { by: adr-0045-perspective-set-factory-review-understand, on: 2026-10-18, reason: "Ruling 84's Coordination bench superseded; Loomkeeper kinds move under Factory" }
---

# ADR-0040: Journey purpose is a durable identity; bridges are explicit records

- **Status:** Proposed (council gate 2026-10-18)
- **Date:** 2026-10-18
- **Deciders (Peer Mode):** Enterprise Architect (author), Data & Persistence Architect, Tech Lead,
  UX Researcher/IA (consulted on the ownership rule)
- **Context spec/architecture:** spec §A2 (two journey contracts), §A4 ownership rule, US-J01,
  US-W03, US-S01–S03, US-B01–B02, UX-01/02/06; `note-workbench-loops-direction` (user-settled: two
  distinct journeys).

## Context

The user settled that work-first and system-first are *distinct* journeys with separate purpose,
records, authority and closure (spec "central distinction"). Four things the shell already has could
each be mistaken for the journey, and the spec forbids each: the navigation section (Factory /
Review / Understand "are places to work, not journey identities"), the native session
(`session_dim` — "it is not the journey or a pane"), the open document, and the pane. The existing
conceptual model has a `WorkItem` whose invariant is "intent differs from assessment" but no concept
of an investigation that *exists, resumes and concludes without any work item* (AC-J01b, AC-S01a).
Acceptance must be a disposition at an exact baseline, blocked while a mandatory obligation lacks
current evidence (AC-P02c), and never a flag that saving sets.

## Decision

We will make the **journey a first-class dimension** (`journey_dim`, ADR-0039) with:

1. **An immutable purpose** — `work` or `investigation` — set at creation and never changed. A PR,
   repository or symbol is a *subject*; the user's chosen entry (Start with work / Start with the
   system / "review for a decision") selects the purpose, and the subject's type never does
   (AC-B02c).
2. **An owning relationship for notebooks and checklists** — exactly one owning journey; a
   notebook never needs both a work and an investigation owner, and no notebook, attempt or
   checklist carries a mandatory Work Item reference (spec ownership rule).
3. **Explicit bridges as Type-0 origin columns on the created journey** (`origin_journey_id`,
   `origin_kind` ∈ `promoted-from` | `investigation-of` | `review-assignment-of`,
   `origin_object_ids`; ADR-0039 §1). Every bridge in the spec creates its target, so the bridge is
   the creation: the confirm action of a previewed bridge writes the new journey row with its
   origin; cancel writes nothing (AC-B01b) and "no work item was created" is provable by the
   absence of a row. Origin carries object ids (question, finding, evidence links) and **never**
   carries permission, evidence confidence or acceptance across (AC-B01c). (Re-opened as a link
   table only if the IA finds a bridge that must join two *existing* journeys.)
4. **Closure as folds, not flags.** *Work acceptance eligibility* = every obligation with
   `mandatory = 1` has a current-baseline `evidence_link_fact` (satisfaction is **only** this fold;
   it cannot be asserted) **or** a `disposition_fact` on that obligation explicitly permitted by
   policy at the current baseline; otherwise acceptance is blocked and the UI shows the gap
   (AC-W03b, AC-P02c). Human assertions on an obligation (`acknowledged`, `deferred`,
   `needs-decision`) are `disposition_fact` rows with `subject_kind = obligation` — acknowledgment
   is a row, satisfaction is a fold, so they can never be confused. *Intent acceptance* is a
   `disposition_fact` of kind `intent-accepted` on `(notebook_id, revision_no)` — the "accepted
   intent revision" the scope snapshot names. *Investigation closure* = the latest
   `disposition_fact` of kind conclusion (`answered` | `bounded-partial` | `unresolved`); open
   questions remain obligations without evidence and are never marked satisfied by closing
   (AC-S03b).
5. **Currentness invalidation.** A journey's scope snapshot is compared to the live scope on every
   open/refresh; a changed HEAD, PR head, intent revision or dirty-content hash marks dependent
   preparations, assessments and obligations `stale` by a derived rule, retaining the historical rows
   (AC-P01c, AC-B02b).
6. **Navigation never writes.** Switching perspective, selecting a feature, opening a pane or
   reopening the workspace reads the journey; none of those creates a journey, attempt, queue item,
   link or disposition (AC-J01c, UX-05).
7. **Pre-workbench notebooks have no owner and get none by guessing.** Legacy drafts migrate as
   `notebook_dim.legacy = 1` ("Unassigned (pre-workbench)") and are adopted into a journey only by
   an explicit user action that writes a new notebook version row (ADR-0039 §1, §5). The Factory
   child "Work items" is derived from `journey_dim` rows with `purpose = work`; it is not a kind.

## Alternatives considered

- **Journey as an attribute of the notebook:** rejected — a journey owns several notebooks (work
  notebook + linked question notebooks) and outlives any one; the ownership rule would invert.
- **Journey = native session:** rejected — a session is a provider-owned execution context with its
  own lifecycle and settings; one journey spans many attempts across sessions and routes, and a
  session may serve a compile call that is no journey at all (ADR-0035).
- **PR object type selects the journey (a third "review" journey):** rejected by the spec ("a PR is
  a possible subject in either journey, not a third journey").
- **Stored `accepted` / `concluded` columns:** rejected — the spec's own gate found that a stored
  review state reads as acceptance; folds make the AC-P02c block structural.
- **A `journey_link_fact` table (first draft):** rejected by the Simplifier/Data review — every
  bridge creates its target, so a 1:1 link is a column; the uniqueness "one promoted-from origin
  per journey" is then free.

## Consequences

- **Positive:** two journeys share every capability (editor, execution, visualizer, checklist)
  without collapsing; the bridge is auditable; "no work item was created" is provable by the
  absence of a row.
- **Negative / accepted trade-offs:** every closure question is a query, so the fold must be fast
  and cached (ADR-0039's labelled caches); the UI must render "blocked — obligation X lacks current
  evidence" rather than a disabled button with no reason.
- **Follow-ups / new risks:** the policy that permits a human disposition to cover an obligation is
  a product decision the spec defers ("lighter human review policy") — until recorded, the default
  is *no* disposition covers a mandatory obligation except `needs-decision` escalation.

## Evidence

- Spec §A2/§A4 and the gate record's AC-P02c resolution — **Verified** (document read 2026-10-18).
- `src/AiDe.Core/Store/WorkspaceSchema.cs` `session_dim` exists and is not a journey — **Verified**.
- The fold's performance on a realistic journey (hundreds of obligations) — **Flagged**, measured in
  the slice that builds it (NFR-02 selection/filter budgets apply).
