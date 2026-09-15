---
id: note-atlas-views-owner
title: "Atlas five-view programme: Owner scope and design prerequisites"
type: decision-note
status: accepted
owner: "@timianmalloo"
tags: [atlas, owner-decision, e1, e2, coordination]
links:
  - { to: session-contracts, rel: depends-on }
  - { to: architecture, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Admits two bounded design and contract lanes for Sequence/Activity and
  domain/layer/Azure. Records evidence, reuse, shared prerequisites and required
  independent gates without granting product edits or accepting implementation.
---

# Decision and terminal condition

**Decision source:** Conductor instruction to Owner `codex-atlas-views-owner`,
2026-09-15, accepting the grounded five-view scope and separate design lanes.
**Programme tier: T2.** The initial read-only evidence inventory was T1; design,
model integrity, native UX and product acceptance remain T2.

**Goal:** deliver Sequence, Activity, domain, layer/component and Azure declaration
views through the existing Atlas evidence and native presentation architecture.
**Done when:** these five views satisfy their agreed source-to-native contracts,
independent gates and integrated journeys, with recorded evidence and serialized
handoff. This decision only admits the bounded design/contract phase.
**Not in scope:** native Class qualification and recovery integration (GHCP),
Grok's Solution tree programme, ER delivery, E3 comparison, E4 interpretation,
live Azure access, source execution, a second graph store or direct main publication.
**Fan-out:** two design lanes; programme cap four including Owner and Conductor.
Owner authors decisions, not a second implementation, and clears no author veto.

ER is an explicit remainder for GHCP coordination. Full Addendum E E2 includes ER;
completing these five views must never be labelled completion of all E2.

## Evidence pins and confidence

All **Verified** findings here mean opened document or source content, not executed
product behavior. No native run, product test or external SDK conformance was performed.

| Evidence | Observed pin and role |
|---|---|
| Main / Owner tree | `c46e112a924a8a0a4c86e4552af1f0e30bfa851c`; grounding base, not Atlas implementation foundation |
| Atlas intent/reference tree | `92e025ae8cacd0a2fc8580f865a2172c4a9bdeb4`; Addendum E, architecture, E1 design and candidate sources |
| Recovery tree | `6ac1e43419ff30e80bc87696ed2c61a73be6910e`; inspected read-only; it lacks the four referenced Atlas spec/architecture/design/coordination artifacts |
| Proposal | `1065a851e4039fc80804ca92f2730d9f23e6adfc`; private reference direction only; no private source, fixture or image copied |
| E1 Core checkpoint | E1 design reports `2b818f144980a0e2acecbaddc0795864a00673a3`; StaticStructure opt-in and bounded page metadata. Report read, execution not independently repeated |

Grounding follows `design-code-atlas-e1-static-views` → `spec-addendum-e-code-atlas`
and `architecture-code-atlas-proposed` → the delivery-horizon note and
`spec-uml-erm-surfaces`. The latter links the UML/domain/diagram knowledge bases.
These candidate-only Atlas nodes are not copied into the main-based Owner tree.

**Verified:** Addendum E still labels itself draft/not registered. Its current
requirements guide these lanes; this note does not rewrite that status or turn
historical acknowledgements into a new canonical grant. E1 design's current
checkpoint supersedes its historical implementation-status paragraphs only for
the explicit bounded Class/Core tranche; Sequence/Activity remain separate.

## Five-view contract and reuse

| View | Governing contract at reference pin | Required design result and reuse |
|---|---|---|
| Sequence | Addendum E US-E6.a/c/d; architecture §8 and §13 E1-BEHAVIOR | One static call-site occurrence references a caller declaration, target member or unresolved target, source version/span and ordinal. Repeated/recursive calls survive. Reuse SequenceDiagramSurface/SequenceModel drawing conventions; extend evidence deliberately. Existing Interaction remains type-level source-order, never renamed method execution. |
| Activity | US-E6.b/c/d; architecture §8 | Typed branch, loop, exception, cancellation and async relations; explicit bounded gaps and confidence. Reuse Atlas selection/source/history infrastructure and the sequence evidence contract where truly shared. A new flow model needs its own grain and notation review. |
| Domain | US-E7.a/d; architecture §8 | Entities, value objects, aggregates and invariants cite source or accepted specification evidence. Namespace/folder membership never invents a bounded context. Declared, inferred, target-state and unknown remain distinct. |
| Layer/component | US-E7.c/d; UML/ERM US-U1/U2/U4-U9 | Typed sourced edges, unsupported list and valid altitude. Logical application, deployment and target-state layers remain distinct. Reuse existing graph/diagram patterns after contract checks. |
| Azure | US-E8.a-e; architecture §8 and E2-DATA-RESOURCE | Scoped declaration identity, evidenced alias collapse, named service/type/layer/source; separate dependency, parent, network, configuration, explicit grant and observed runtime predicates. No grant from lookup/comment; no invented absent service; no Bicep-resource-to-C4-container promotion. |

**Verified source reuse:** `src/AiDe.App/Workbench/SequenceModel.cs` exposes only
participants and ordered From/To/Label/Kind messages. `Projections/Interaction.cs`
carries source revision and bounds, but not the required method/source-binding
contract. `ProjectionService.Interaction` uses the existing ordered call reader.
`CSharpExtractor.TypeCalls` keeps occurrence rows alongside deduplicated type edges.
Those are useful precedents, not completed US-E6 behavior.

`Extraction/BicepExtractor.cs` reads syntax as data and emits scoped resource/type/API,
existing-reference, loop and conditional evidence. `EfSchemaExtractor.cs` and
`SqlSchemaExtractor.cs` exist; EF reads migration syntax. Their existence does not
prove domain meaning, source/hash binding or Atlas policy safety. The lane must
reconcile actual producer contracts before claiming reuse closes an obligation.

**Verified mock direction:** proposal README preserves file-first and visual-first
entry to the same identity, explicit altitude, anchored static behavior and Back.
`docs/mockups/uml-erm-surfaces.md` documents ER/class/C4, read-only provenance and
generation-error/too-large states. It is not a five-view implementation contract.
Reuse that settled direction and `DESIGN.md`; lane-specific designs remain required
because method control-flow and semantic/resource projections have distinct grains,
evidence rules, unsupported cases, concurrency and native acceptance obligations.

## Phase and shared prerequisite decisions

1. Admit E1 behavior and E2 architecture **design/contract grounding in parallel**
   in separate main-based worktrees, reading the immutable reference pin.
2. Before implementation, settle one accepted foundation pin with GHCP. Core's
   acknowledged request `req-01M2KA74EQYXWCZB5GMRNXNWN1` requires landing first or
   explicit candidate-base agreement; `atlas/main-integration` remains its writer.
3. Freeze identity, source authorization/binding, manifest/coherence, evidence origin,
   bounds, selection/Back receipts, cancellation and renderer/list/inspector seams.
   E2 depends on this shared presentation/source contract, not automatically on all
   E1 behavior implementation. Any additional discovered dependency returns to Owner.
4. Each lane submits exact files and public signatures to Core. New Surface/View paths
   need section-2 assignment; no Shell/factory/PerspectiveMenuTests/Core-Ipc edits
   without integrator agreement. Coordinate Grok overlaps. No broad leases.
5. Independent T2 design gates precede product admission. Native proof and joins are
   serialized. Class-view qualification and recovery stay with GHCP throughout.

## Floors and proposed gate predicates

Surface list: source/store → identity/observation model → producer/service →
projection/wire → client type → native diagram/list/inspector → source/Back compute
reader → diagnostics → tests/proof. All representations derive from one result;
neither presentation nor a new cache becomes a second source of truth.

- **Data/UML:** declare grain and invariant first; provenance and confidence survive;
  no false DDD/C4 elevation, occurrence deduplication or resource alias inflation.
- **Test:** map every assigned clause to a falsifier; D0 plus triggered D1-D7 union.
  D4 uses real engine/filesystem semantics; D6 includes compatibility; D7 pairs fakes
  with real seam evidence. AI directives apply only if an admitted change triggers them.
- **E1 proof:** repeated/overloaded/recursive calls, unknown dispatch, branches,
  async/cancel/error paths, truncation and diagram/list agreement. Permanent static
  reconstruction disclosure; no timing/frequency/runtime-order claim.
- **E2 proof:** evidenced domain labels, typed/unsupported layer edges, target/current
  distinction, resource alias/unknown/absent cases and config/grant/runtime distinction.
  ER keys/cardinality/M:N remain outside this assignment and must not be marked passed.
- **Security/Privacy:** source authorization, stale bytes, hostile inert text, no path
  escape or execution; privacy-minimized proof corpora. No private proposal material.
- **UX/native:** source→view→source→Back in real composition, keyboard/UIA and stable
  focus; source identity and bounds agree across surfaces. Hard states include empty,
  loading, malformed, unsupported, stale, partial, canceled, error and overflow.
  Token/craft checks supplement actual native evidence; mocks cannot clear this gate.
- **SRE:** normal-path query/render duration, count/omission, selected path, cancellation
  and error emissions. Missing measurements say not recorded. Budget limits need a
  declared fixture and observable oracle before implementation.

These are Owner acceptance requirements, not independent PASS verdicts. Triggered
Test, Data, UML, Security/Privacy, UX/IA, accessibility/native, Distributed Systems,
SRE, language, Simplifier and Release lenses remain the Conductor's review work.

## Owner reading of the Conductor plan

Read the working plan and coordination artifacts in `ai-de-conductor-atlas-views`
on 2026-09-15. The dependency graph preserves independent lane grounding and a shared
contract gate. This is an Owner scope review, not its independent graph veto clearance.

Concrete gaps to close before their dependent node:

| Gap | Clearing condition |
|---|---|
| Tier and scope still provisional in plan/coordination | Record T2 and explicit ER remainder; avoid an all-E2 completion claim |
| Candidate foundation remains unknown | GHCP supplies explicit frozen base/handoff or landing; Conductor verifies it |
| C/R-D criteria name categories, not signatures | Lane designs supply exact shared records, wire compatibility, ownership and mutually satisfiable limits |
| Supported producer subset and performance ceilings not fixed | Owner reviews safe representative fixtures, supported syntax/evidence and explicit unresolved behavior; no silent scope downgrade |
| Independent plan/design verdicts pending | Author-independent reviewer supplies triggered PASS/BLOCK predicates against frozen artifacts |
| Runtime/native evidence absent | Execute admitted real composition under desktop slot and inspect actual result/state |

Planned inventory exit was five mapped views plus phased prerequisites and proposed
floors. Actual inventory reached that exit; no implementation or broad research was
added. Official audit records measured duration and tool-call count. No AIDE_* session
environment was present, so this worker cannot emit a valid registered episode close.

**Completed:** Owner scope and design-prerequisite decision. **Remaining:** exact lane
designs, foundation agreement, independent gates and product delivery. **Best next
action:** dispatch the two bounded design lanes and reconcile their shared contracts.
