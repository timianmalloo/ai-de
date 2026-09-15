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

## F1/F2 decision supplement — 2026-09-15

**Run goal:** settle the domain/layer intake and Azure evidence forks without product
authoring. **Done when:** F1/F2 have a positive supported contract, honest exclusions
and bounded spike oracles; F3 remains with the foundation authorities. **Tier T2;
fan-out 0; eight-call / twelve-minute circuit breaker.** This follows the existing
programme graph: frozen design/source read → Owner decision → recorded handoff.
It does not re-run graph optimization or clear its independent gates.

**Verified input:** E2 design sections 3–6 at
`e2c17251ad0480ac66e3442d5b31483e02e1ecee`, in the architecture-views lane.
Direct source reads at the earlier Atlas reference confirm:
`BoundedContext` contains Name/Description/Includes/Tables; its reader validates
declared groups against known symbols/tables. It emits no DDD role or invariant.
Bicep emits source-scoped symbol, type/API, name/name-expression, existing-reference,
loop/conditional and explicit depends_on evidence. Its folded-default name may be
Inferred. No opened producer supplies canonical deployed resource identity or the
richer Azure predicates. These are source observations, not parser/runtime proof.

### F1 — explicit declared semantics, not inferred domain truth

**Decision: choose A, explicit validated intake.** Reject heuristic DDD inference
and reject an unavailable-only page as completion of US-E7.a/c. Retain the existing
bounded-context contract unchanged; a Context remains a Context.

The smallest proposed new author-facing carrier is a versioned JSON document,
`docs/atlas-architecture.json`, loaded only through the admitted source-policy and
manifest binding. This names a design contract, not permission to create the file
in an analyzed repository or to grant a new production source path. Reuse an existing
equivalent structured carrier if the lane demonstrates the same semantics and
strict validation; do not stretch the existing small YAML parser silently.

Minimum logical fields, to become exact DTO/schema and limits in the lane design:

- `version` and stable document-local IDs; all references are typed IDs, never labels.
- `concepts`: ID, label, role (`entity`, `value-object`, `aggregate`), exact evidence
  targets and bound declaration anchors. Entity/value-object semantics are explicitly
  declared. An aggregate names its entity root, explicit concept members, and at least
  one invariant ID; no namespace wildcard establishes membership or a boundary.
- `invariants`: ID, plain-text statement and evidence anchors. Display **declared
  invariant**; this intake does not prove that code enforces it. An EnforcesInvariant
  edge requires separate supported enforcement evidence, not the statement alone.
- `layers` / `components`: ID, label, explicit kind, dimension (`logical` or
  `deployment`), declared state (`current`, `target`, `unspecified`) and exact source
  memberships. Context includes/table lists are not automatically these memberships.
- Relations: typed endpoint IDs plus evidence references. A current implementation
  dependency requires an existing supported source assertion. A declared/target
  relationship may be displayed as such, never substituted for an extracted edge.

An evidence reference resolves a real artifact/declaration or supplied document clause
in the same pinned observation: scope, artifact hash/revision, location, producer/rule
version and origin travel together. Core issues source-selection tokens after validation.
Missing/stale/out-of-scope references produce explicit unresolved claims, not invented
targets. Duplicate IDs, invalid shape/enums and contradictory identity references reject
the affected intake with a stable diagnostic; no partial shape becomes a valid claim.
Input byte/row/depth/text caps and cancellation are required before implementation.

**Authority boundary:** source-declared semantics provide the positive initial path.
Their label is **declared in source**, not verified DDD truth or accepted architecture.
Accepted-spec evidence is optional: consume a trusted caller-supplied admission record
bound to exact document hash/clause, scope, state and acceptance reference. If no such
record exists, show supplied status as unverified/authority unknown and do not use it
to establish accepted/current applicability. No authority from frontmatter, timestamp,
commit, filename or prose; no new E3 resolver, compliance engine or acceptance UI.
Acceptance of a clause still does not prove that current code realizes it.

**Positive exit fixtures:** a declared entity and value object, an aggregate with root,
members and invariant, plus a class-only unclassified neighbor; two explicitly declared
logical layers with a supported source dependency and an unsupported crossing. Each
must reach the bound declaration/evidence and Back through actual native composition.
Unknown-only or table-as-entity output cannot pass.

### F2 — resource roots require identity evidence

**Decision:** distinguish an observed resource declaration root from a known Azure
deployment identity. Neither is evidence that a resource was deployed or used.

1. The initial symbolic root is the exact workspace/scope/file/resource-symbol
   declaration identity; observations retain revision/hash separately. Counts say
   **declaration roots**, never deployed resources. Loop/conditional declarations
   preserve unknown instance count/condition and are not flattened into one deployment.
2. Admit a positive explicit alias contract: alias ID/label, exact `rootRef` to one
   validated resource declaration, and the bound source declaration of that reference.
   It may be carried by the same explicit JSON intake. This asserts a source-declared
   alias of that declaration, with that origin visible; it does not establish equality
   of two separately declared Azure resources. All alias anchors remain inspectable.
3. Merge separately declared resources only when a supported producer supplies equal,
   fully known deployment identity: exact deployment-scope identity, full provider/type
   chain and full resource-name/parent chain. Parameter defaults, display names,
   service family, API version, proximity and `existing` alone are never equivalence.
   Unknown scope/expression/module binding/loop instance remains symbolic. Partial
   tuples never compare equal. Normalization rules need documented spike evidence;
   until then use exact representations and accept missed merges rather than false ones.
4. An intake `rootRef` cannot manufacture cross-declaration equivalence. It can name a
   declaration root; it cannot certify an Azure deployment tuple or link a second
   declaration to that root without the separate supported identity evidence above.

**Initial supported Bicep producer subset:** existing source-evidenced resource
declarations, raw type/API/symbol, source-scoped literal or explicitly Inferred folded
name, existing/loop/conditional disclosures, and direct explicit `dependsOn` resource
references whose endpoints resolve in the admitted scope. Present these as deployment
dependencies, not runtime calls or C4 component edges. A dangling target is unresolved.
Service-family classification may use a documented versioned rule over raw type;
logical layer comes only from F1 explicit membership, otherwise Unknown.

Parent/child, configuration, network, explicit grant and runtime use remain separate
capabilities with unsupported reasons until their own supported producers are admitted.
Do not manufacture candidate counts by scanning comments or looking up familiar names.
An explicit runtime-use edge requires observed runtime evidence and is outside this
declaration-only programme. Grant-by-comment/name/lookup remains a negative oracle.

**Positive exit fixtures:** distinct roots with equal names stay distinct; multiple
explicit aliases of the same declaration collapse and retain their anchors; a separate
fully known equivalent-declaration case exercises the admitted identity producer before
claiming cross-declaration collapse. Unresolved scope, loop, conditional, absent service
and mixed unsupported predicate cases accompany real declaration/dependsOn journeys.
The full US-E8.b obligation remains open until its supported cross-declaration fixture
passes; source-declared aliases alone or a declaration-only demo cannot close all US-E8.

### Required bounded spikes and unresolved gates

| Spike | Exact question / falsifying oracle | Required output before implementation |
|---|---|---|
| F1 carrier and identity binding | Can the strict carrier resolve explicit roles/membership to admitted source identities without changing Context semantics or treating declared invariants as proven? Duplicate IDs, stale hashes, missing target, unknown role and a malicious acceptance marker must fail or remain explicitly unresolved. | Schema/example, exact proposed files/signatures, limits, producer-origin/status table and positive/negative fixture results |
| F2 scope and alias evidence | Which read-as-data syntax/input can supply full deployment scope/type/name identity without executing Bicep or guessing parameters? Equal names in different scopes must remain distinct; equal fully evidenced identities must merge; partial identity must never merge. | Small supported-syntax contract and direct evidence from source-bound fixtures; exact normalization rules, alias provenance and residual unsupported cases |
| Shared binding adapter (F3 dependency) | Can each new observation/query/source token preserve the accepted foundation's manifest, authorization, hashes and receipt-based Back? | Peer-approved foundation pin, exact adapter/wire compatibility contract and grant; cannot be inferred from candidate code |

These spikes qualify proposed contracts; synthetic examples are not native product
acceptance. If F2 cannot supply positive cross-declaration evidence without new scope,
return that failed oracle to Owner: preserve the open US-E8.b obligation rather than
declare the story done. Product admission still requires the independent T2 design
reviews and exact grants. F3 remains peer authority and has no invented replacement pin.

**E1 alignment:** preserve Interaction's existing type-level contract. The reported E1
proposal for a separate source-bound behavior-occurrence model, with Sequence and
Activity over one projection, is directionally consistent with the earlier decision;
its uncommitted design has not been independently read or accepted in this run.

**Owner disposition:** F1 and F2 design choices settled subject to named qualification;
no source grant, self-cleared veto or product acceptance. Conductor assigns the exact
spike/design amendments and carries pending foundation authority and full-story exits.
