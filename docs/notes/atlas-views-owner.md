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

## Design-review disposition — 2026-09-15

**Goal:** settle shared design choices and order two bounded lane revisions against
the eight P0 findings. **Done when:** each finding has a named revision output and
independent clearing predicate. **Tier T2; fan-out 0; ten-call/twelve-minute budget.**
No product changes, foundation admission or review clearance are part of this run.

**Verified evidence:** independent receipt
`593c7650ceaa0c92431ae5518caadbdeb6454a6e`,
`docs/proof/atlas-views-plan-review.md`, reviews E1 `755d0347` and E2 `e2c17251`.
It blocks implementation on P0.1–P0.8 and separately blocks the E2 spike's claimed
oracle coverage. Its earlier evidence-node finding was already cleared against
Conductor `137d4fe5`; do not repeat that repair. Source reads confirm E1 currently
selects G1 and its proposed response lacks a Restore receipt. No product was run.

### Shared ruling A — governing G6, one linked reading workstation

**Retain Addendum E C1's G6 Multi-Panel Data Terminal for all five views.** There is
no G1 modelling-authoring amendment. Sequence/Activity are linked read-only details
of selected code; domain/layer/Azure are linked evidence views. The source/history
tree and evidence inspector do not turn this job into parametric 3D modelling.

The opened grammar requires each facet once and single-choice Color. Its examples
reuse `Type` for category and typography; Addendum's combined Color value also
conflicts with that grammar. Use the documented extension hook to serialize the
existing design intent without those ambiguities:

```text
DataTerminal {
  Type:DSS; Arch:SPA; Layout:MultiPanelWorkstation; Density:UltraDense;
  Nav:CommandPalette+Sidebar; Viewport:DesktopBound;
  Input:KeyboardFirst+PrecisionPointer; Color:DarkAdaptive;
  x-typography:MonospaceTechnical; x-highContrast:System; Depth:Flat;
  Sync:LocalFirst; Persistence:Session; Feedback:Instant+Confirmed;
  Motion:None; Pacing:Freeform; Transition:HardCut;
  A11y:WCAG_2.2_AA+HighLegibility+ReducedMotion;
  x-platform:windows; x-framework:wpf;
}
```

This is an explicit serialization normalization, not a claim that the upstream
grammar/example defect was repaired. Conductor reports it to the governing-doc owner.
The concrete facet mappings below preserve the G6 family; independent UX still
checks round-trip fidelity and all P0.1 conditions on the revised designs.

| Facet group | Required concrete realization in both lane designs |
|---|---|
| DSS / SPA / workstation | Existing Architecture host; file or concept master selection; linked diagram, list, evidence and source details. No new perspective or editing model |
| Navigation/input | Existing palette/sidebar entry; keyboard-first focus chain, precision-pointer equivalent, explicit selection and Back; no new global menu without seam agreement |
| Density/type/color/depth | Existing WPF tokens, dense rows and hierarchy, technical code/identity typography, readable UI copy, Light/Dark plus Windows HighContrast, flat structure |
| Local/session | Local pinned evidence, explicit refresh; session selection/scroll/focus with opaque Core receipts. No duplicate source bodies or durable graph truth in view history |
| Feedback/time | Immediate local selection/loading feedback, data accepted only after validation; free reading, no animation, hard cuts without focus loss or false current data |
| Native/accessibility | WPF controls/UIA; keyboard source/Back; 100/150/200% DPI; reduced motion and full-label access; linked accessible alternatives |

The accepted deviations from catalog G6 are local evidence rather than market
streaming, sidebar/file navigation beside existing commands, and confirmed immutable
query results rather than flashing live quotes. E1 has a selected method and one
projection across Sequence/Activity; E2 has a selected concept/resource and one
presentation model across Domain/Layer/Azure. Neither changes the family.

### Shared ruling B — envelope semantics now, production shape after foundation

Both lanes must use this **logical contract** in their revised designs and synthetic
mock states. It is not a granted API, accepted codec or replacement reader:

| Boundary | Mandatory meaning |
|---|---|
| Request | Negotiated version/capability; authorized scope token; expected Core epoch; exact manifest; monotonic request sequence within the owning view/session; opaque selection/observation context; bounded window with declared grain |
| Response | Echo/correlate scope, epoch, manifest, request sequence and selected subject; opaque projection observation token bound to its evidence set; typed completion/outcome, bounds/omissions, continuation, disclosures and capability/version |
| Row/evidence | Exact evidence origin/confidence and observation-bound source-selection token or typed unavailable reason; a projection token cannot substitute for a row's source binding |
| Navigation | Core-issued receipt for an accepted restorable selection, or explicit unavailable reason. Source and Back/Restore consume existing authority and receipts; no client-minted paths, receipts or reconstructed latest selection |
| Lifetime | Owner invalidation/cancellation on epoch, manifest, selected context or session replacement; late N cannot overwrite N+1 even if N succeeds after cancellation |
| Refusal | Unknown version/capability/enum, invalid sequence/bounds/token or malformed payload yields typed refusal. Refused/error replies publish no usable new selection/receipt and never make old evidence current |

Page completion, observation coverage and remaining continuation are distinct.
A complete page may still have an unknown semantic target or more pages; those
facts stay explicit. Partial/canceled work is never silently called complete.
The lane design must define exactly when a partial selection is restorable and which
receipt it receives; otherwise it is non-restorable with reason. A Back refusal
keeps its own expired/retired result and never selects a guessed latest equivalent.

Shared golden cases must cover round trip, older/non-capable peer, missing/unknown
field or enum under the chosen compatibility policy, inert disclosure text, stale
scope/epoch/manifest, reordered N/N+1, cancellation-before-publication, partial
completion, capped result, source-changed and expired Restore. After foundation
admission, the integrator maps this contract to actual shared types and freezes the
bytes/signatures with one writer per path. Until then P0.3/P0.4 remain open.

### Two revision nodes and priority

**Admit bounded design revisions in the existing lane Markdown paths.** They can
proceed independently of foundation after these shared decisions. They may not
implement product code, fabricate the missing producer, or add arbitrary files.

1. **E1 revision:** replace G1 with G6 and facet mappings; adopt envelope semantics;
   freeze supported control-flow rules and their spike oracles; specify occurrence
   paging versus structural-node accounting, cross-page edges/gaps, stable order and
   page recomposition. Source syntax order must not become executable control flow.
   The syntax-versus-CFG choice requires bounded evidence before semantic acceptance.
2. **E2 revision:** incorporate F1/F2 as typed schema/DTO proposals, including authority,
   origin/version, source binding, symbolic identity and carrier limits; adopt G6 and
   envelope semantics; specify per-view focus/UIA/state and graph/list/inspector oracles.
   Keep full cross-declaration equality/US-E8.b unresolved, and keep the failed spike
   oracles separate from production evidence. One E2 presentation core prevents drift.

In parallel with these Markdown revisions, Conductor requests exactly two new
mock paths: `docs/mockups/atlas-behavior-views.html` and
`docs/mockups/atlas-architecture-views.html`. **This note does not grant those paths.**
Existing design Markdown supplies each mock's graph-linked hub; no shared new
DESIGN file, generated private material or dependency is needed. After exact grant,
each file is a standalone synthetic DX8–DX10 harness using existing design tokens.

Harness selectors: persona, viewport, view, state, theme, capability and reduced
motion. For every state list trigger → visible content/copy → enabled actions →
forbidden content → preserved/cleared selection → recovery → focus destination.
Render all lane states, including ambiguity/async/cancel/error/omission for E1,
declared/unknown authority for Domain, typed/unsupported edges for Layer, and
declarations/aliases/unknown identity for Azure. Include long labels and overflow.
Run structure-first rubric critique; HTML proof cannot replace native proof.

### Performance and caps — normative candidates, not measurements

Retain governing thresholds: open ≤2 seconds p95; filter and retained switch ≤150 ms
p95. **Owner candidate frame budget: no continuous Atlas UI-thread work segment over
16 ms.** This is a proposed acceptance threshold, not an observed capability or a
claim that all OS rendering frames meet it. Independent SRE/Test review must accept
its exact instrumentation and workload before dispatch; unmet results return to
design, never silently relax the budget.

Each lane must define exact proposed request bytes, total response bytes, primary
rows, structural nodes, edges, text lengths, parser depth and layout-work limits in
its revised schema. Start from inspected foundation ceilings where applicable;
otherwise label the numeric choice **proposed limit** with a rationale. A cap on
primary rows alone cannot bound an auxiliary graph. Bound before allocation and
publication; refused/truncated states and every numeric unit require negative tests.
No hard cap is treated as measured capacity. Any still-blank cap keeps P0.6 open.

Protocol requirement: one explicitly authorized fixture per lane, 20 cold-view
samples (fresh view and projection cache, startup excluded) and 100 retained/warm
samples, reported separately; nearest-rank p95, raw samples retained. Record actual
hardware/OS/runtime/build, viewport/DPI, fixture hash/population, cache state and
instrumentation before running. Do not invent hardware values now. Opening measures
user action to correctly rendered/interactable content; separate query/render timing
explains the total. Boundary fixtures deliberately exceed each cap. Native dispatcher
segments and synthetic over-budget controls must test the stated frame criterion.

### P0 disposition and join guard

| Finding | Next evidence; independent clearance still required |
|---|---|
| P0.1 | Both frozen designs carry normalized G6, mappings, phase deviations and UX round-trip review |
| P0.2 | E2 frozen typed carrier/DTO schema folds F1/F2 and all unresolved identity/authority cases |
| P0.3 | Shared semantic table now; accepted-foundation mapping and actual golden payloads later at C |
| P0.4 | Exact accepted SHA, grants and jointly satisfiable E1/E2/GHCP/Core/Grok path/signature guards; one shared integrator |
| P0.5 | Exact mock grants, complete executable synthetic state harnesses and recorded rubric critique |
| P0.6 | Lane fixtures/protocols, complete numeric cap tables, instrumentation and SRE/Test-reviewed thresholds |
| P0.7 | E1 bounded syntax/CFG spike plus structural accounting, cross-page/gap and recomposition oracles |
| P0.8 | Domain/Layer/Azure per-view row/relation equality, keyboard Source/Back/focus, UIA identity/uncertainty, HighContrast/DPI/overflow/state-reachability and real WPF composition controls |

No P0 is removed by this decision. Freeze revised outputs and return them to the
independent reviewer. The exact foundation/grant guard is conjunctive, not alternative:
accepted foundation **and** exact grants **and** shared compatibility **and** independent
design clearance are all required before implementation. Design-only context-seam
consideration does not discharge that guard or supply the missing Azure context.

## E1 structural experiment D1–D5 — 2026-09-15

**Goal:** resolve five oracle ambiguities before the bounded E1 experiment author
starts. **Done when:** exact node/edge/closure/cap choices return to the independent
ledger reviewer. **Tier T2; fan-out 0; eight calls/twelve minutes, checkpoint six.**
No executable CFG, production behavior, new file grant or native evidence is admitted.

**Verified inputs:** reviewer ledger `0ed952832f076103c4c2b766e32169f59ecd865b`,
`docs/proof/atlas-views-plan-review.md`, and E1 design §5 at
`83e1139b580ef977bebcf68b38e84dd1a871da82`. The ledger freezes four literal fixtures,
13 primary identities and page counts; it blocks author start pending D1–D5.
The design establishes synthetic structural markers and page closure but leaves
their exact attachment/representation unsettled. The following are **Owner design
choices**, not assertions that existing code already implements them. They refine
§5 explicitly; independent ledger and semantic vetoes remain uncleared.

### Common identity and closure convention

Retain the ledger's literal sources, selected methods, B1–B4/L1–L2/T1–T5/G1–G2,
source spans/child paths and primary ordinals unchanged. Graph counts below concern
structural nodes and relations, not separate Sequence participant metadata. No extra
primary fact may be added to make an oracle convenient.

Every fixture has auxiliary `Entry`, `Exit`, `MethodBody`. Their IDs are fixture
observation + selected method + distinct boundary/body role. All three are owned by
the selected method; MethodBody is the root region in this experiment. Entry/Exit
are source-boundary markers, not execution entry/return/termination events.
The method is selection metadata, not a fourth auxiliary graph node.

Every auxiliary region records an owner (method or canonical primary ID) and parent
region. Parent-region metadata determines transitive closure; it is not another
implicit graph edge. Owning primary nodes outside the requested window are never
pulled in as extra primary rows. Edges to those omitted owners use normal boundary
stubs. All page views retain Entry/Exit/MethodBody, plus ancestor regions of included
primary facts. No unselected sibling regions or bodies are added merely for context.

Canonical auxiliary order: Entry, MethodBody, then remaining regions by owner source
ordinal and fixed region-role/arm order, then Exit. Within these four fixtures this
means true before false; try-statement before finally-body. Relation identity retains
canonical endpoints/kind/arm; deterministic display ordering must not enter identity.

### D1 — boundary, body and ordinary-block attachment

Use exactly two common structural edges:
`Contains(MethodBody,Entry)` and `Contains(MethodBody,Exit)`. They mean the source
body has these boundaries. Do not add an Entry→first-fact, last-fact→Exit or other
executable-successor edge. Direct supported statements/facts attach using the exact
per-fixture relations below.

Supported ordinary nested blocks get one region marker unless a specialized arm,
loop, try-statement or finally-body region already represents that same admitted
block role. Never add a duplicate generic block underneath a specialized region.
The four fixed fixtures contain no additional standalone supported nested block.
Generic nested-block correctness beyond these fixtures is not proved by this run.

### D2 — loop condition, with no extra node

`L1` retains a source-evidence value `conditionText = "more"` with its exact
condition span. No condition primary or auxiliary node is added. The body region
`L.body` is owned by L1 and has parent MethodBody.
Both `LoopBodyRegion(L1,L.body)` and `LoopConditionSource(L1,L.body)` exist;
the latter carries exact `more` source evidence and means the loop header contains
that condition text. It does not claim evaluation, truth, entry or iteration.
Distinct kinds preserve distinct identities despite sharing endpoints. No self-loop
or loop-back edge exists.

### D3 — explicit try-statement region and finally-body region

Keep T1 (TryStatement) and T4 (FinallyClause) as primary nodes. Add two distinct
auxiliaries: `T.try` is a **try-statement region**, owned by T1, parent MethodBody;
`T.finally` is a **finally-body region**, owned by T4, parent T.try. T.try denotes
the selected TryStatement's syntactic extent, including its associated finally
clause; it is not mislabeled as only the try body. T.finally denotes the finally
body and keeps T4's owner/source binding. Both markers are disclosed synthetic
structure, not additional observed statements.

**Explicit refinement of Contains:** it may connect an owner construct to its owned
auxiliary region as well as a region to its direct represented facts. This is purely
syntactic containment. T.try directly represents T2 and T4; T.finally represents T5.
The precise edge is `FinallyDeclaration(T.try,T.finally)`, connecting associated
regions. No call/throw/await is connected to a finally handler as execution.
The fixed T fixture has no next lexical sibling after its await within the try body,
so it has no ContinuationSource edge. T2 and T4 are not lexical sibling statements;
do not add NextInSource between them.

### D4 — opaque unsupported lock and skipped local-function body

G1/G2 remain the ledger's two sourced gaps. Stop traversal at each gap construct
for this admitted subset. Neither the Local body nor the empty lock body gets an
auxiliary marker; nested Ping contributes no node, edge or target evidence.
This explicitly narrows the design's empty-region rule: an empty region is shown
only inside admitted structure, never by expanding an opaque unsupported subtree.
The lock gap retains its source anchor and unsupported reason; omission is visible.

### Exact uncapped inventories for independent ledger review

Each row adds its listed edges to the two common D1 edges. These are **complete**
sets for the four fixed fixtures, not lower bounds. No implicit ownership edge or
duplicate generic block is to be supplied by the subject.

| Fixture | Auxiliaries beyond Entry/Exit/MethodBody | Exact additional relations | Primary / auxiliary / total nodes / total edges |
|---|---|---|---|
| B | B.true (owner B2, parent MethodBody); B.false (owner B2, parent MethodBody) | Contains(MethodBody,B1); Contains(MethodBody,B2); NextInSource(B1,B2); WhenTrueRegion(B2,B.true); WhenFalseRegion(B2,B.false); Contains(B.true,B3); Contains(B.false,B4) | 4 / 5 / 9 / 9 |
| L | L.body (owner L1, parent MethodBody) | Contains(MethodBody,L1); LoopBodyRegion(L1,L.body); LoopConditionSource(L1,L.body); Contains(L.body,L2) | 2 / 4 / 6 / 6 |
| T | T.try (owner T1, parent MethodBody); T.finally (owner T4, parent T.try) | Contains(MethodBody,T1); Contains(T1,T.try); Contains(T.try,T2); AwaitOperand(T2,T3); Contains(T.try,T4); Contains(T4,T.finally); Contains(T.finally,T5); FinallyDeclaration(T.try,T.finally) | 5 / 5 / 10 / 10 |
| G | None | Contains(MethodBody,G1); Contains(MethodBody,G2); NextInSource(G1,G2) | 2 / 3 / 5 / 5 |

For closure, B1/B2 attach to MethodBody; B3 to B.true; B4 to B.false; L1 to
MethodBody; L2 to L.body; T1 to MethodBody; T2/T3/T4 to T.try; T5 to T.finally;
G1/G2 to MethodBody. T3's region chain includes T.try even when its owning await
primary T2 is outside the window; AwaitOperand then becomes a boundary stub.
Full-observation totals are 13 primary and 17 auxiliary nodes, with 30 structural
edges. These are chosen expected counts, not executed measurements.

Per-page edges/stubs follow §5.3 mechanically from these complete sets: both
endpoints included → edge; exactly one included → one directional outside-window
stub; neither included → absent. Auxiliary missing endpoints may have an owner
ordinal but not their own primary ordinal: label that distinction and never
fabricate a primary row index. Region closure and stub counts remain separate.
The ledger's page counts for sizes 1/2/7/128 remain B 4/2/1/1, L 2/1/1/1,
T 5/3/1/1 and G 2/1/1/1 under nonbinding caps. Recomposition must match the complete
sets above plus independent identities/anchors/predicates/confidence, not just counts.

### D5 — genuinely unrepresentable, test-only policy

Admit a local experimental policy override; no production default changes. Request
B window `[2,3)` (B3 only), primary limit 1. Mandatory auxiliaries are Entry, Exit,
MethodBody and B.true: **four**, computed from actual closure. Set auxiliary cap 3;
edge/stub caps 64 each, encoded result cap 65,536 bytes, traversal cap 10,000 and
depth 64 so the fixture's first firing limit must be auxiliary closure. No pruning
of B.true, substitution of a boundary stub for an owning region, or empty successful
page may evade the failure. Return `window-unrepresentable` and no published result
or synthetic restorable selection. No real Core token or receipt is created here.

Two controls prevent a tautological rejection: the identical B3 request with
auxiliary cap 4 must publish with all four auxiliaries; B window `[0,1)` (B1 only)
with cap 3 must publish with Entry/Exit/MethodBody. Derive actual auxiliary counts
from the projection, independently compare to these expectations, and show that
always-refuse, omitted-region and ignored-cap mutations fail their intended oracles.
These numbers are normative experiment inputs, not measured performance/capacity.

### Dispatch boundary

Conductor sends these precise choices back to the independent ledger reviewer and
folds accepted wording into the E1 design/ledger before author dispatch. This note
does not clear that review. The experiment stays in the existing four Ruling-121
files, with four fixtures, no new packages, no production wire/source authority and
no native/runtime/CFG claim. Any incompatible expected edge or count returns here;
the author must not silently alter the oracle to fit generated output.
