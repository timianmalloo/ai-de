---
id: design-atlas-architecture-views
title: "Atlas E2 domain, layer, and Azure declaration views"
type: doc
status: proposed
owner: "@timianmalloo"
tags: [atlas, domain, architecture, azure, design]
links:
  - { to: session-contracts, rel: depends-on }
  - { to: mockup-uml-erm-surfaces, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Source-grounded vertical design and producer-gap ledger for three E2 views. Requires a
  pinned Atlas foundation and explicit evidence contracts before product implementation.
---

# Atlas E2 architecture views

## 1. Decision state and scope

**Proposed design, not implementation acceptance.** This worker is the E2 author, not programme
Owner or independent reviewer. T2. Goal: source -> identity/provenance -> projection -> wire ->
native view -> exact Source/Back, for Domain, Layer/component, and Azure declarations.
Done for this design unit: every applicable US-E7/8 obligation has opened-source evidence or
an exact gap, with proposed signatures, files, and falsifiers. Product completion remains the
programme's later native and independent acceptance gate.

Grounding base: `c46e112a924a8a0a4c86e4552af1f0e30bfa851c`. Intent/reference pin:
`92e025ae8cacd0a2fc8580f865a2172c4a9bdeb4`, specifically Addendum E US-E7/8,
`docs/architecture/code-atlas-proposed.md` sections 8/13, and
`docs/design/code-atlas-e1-static-views.md`. Reference pin is candidate intent, not accepted
production foundation. The base lacks `Understanding` files present at the reference pin.
Conductor must fix the accepted foundation before implementation.

ER (US-E7.b) remains outstanding outside the five-view programme; this document does not retire
it. Sequence/Activity belong to E1, Class/native recovery to GHCP, Solution tree to Grok.
No live Azure access, analyzed-code execution, model interpretation, comparison engine, second
graph store, or main push is admitted. Missing evidence must not become inferred domain truth.

## 2. Opened-source evidence and reuse

All rows are **source-observed**, not measured runtime behavior. No product test or native
render was executed in this design unit.

| Source at grounding base unless marked pin | Observation | Reuse / limit |
|---|---|---|
| `Facts/EvidenceAssertion.cs` under Core | Grain is one extractor assertion of a normalized relation at one scope/revision; origin and verification status are separate; provenance includes path, location, extractor/version/time | Retain assertion identity and provenance. These fields do not prove domain authority or authorize navigation |
| `Store/StoreReader.cs:19,34` | Latest committed scope snapshot; `CurrentAssertions` selects latest complete generation | Reuse store, but do not independently query latest facts for a pinned Atlas manifest. Exact-revision bridge is an unresolved shared seam |
| `Extraction/BoundedContextMap.cs:4,65` | Explicit name/description/includes/tables; parser validates unsupported syntax, unknown patterns, overlaps | Declared context grouping only. Neither namespaces nor this map declare entity/value-object/aggregate/invariant |
| `Projections/ContextProjection.cs:106,129` | Contexts and typed relational crossings; uncovered groups; invalid map renders no partial diagram | Reuse crossing semantics and invalid/uncovered states; legacy name “domain view” does not establish DDD role evidence |
| `Projections/GraphProjection.cs` | GraphEdge retains predicate/status; query filters before cap | Reuse bounded projection principles; generic graph DTO lacks full assertion/source/authority binding |
| `Extraction/BicepExtractor.cs:122–211` | Static resource symbol identity is scope/symbol; type/API/declaration/existing/loop/conditional/name; explicit dependsOn references; folded names are Inferred | Declaration evidence, not inventory of deployed resources. No canonical root, parent/config/network/grant/runtime predicates are produced here |
| `Extraction/EfSchemaExtractor.cs:183–202`; `SqlSchemaExtractor.cs:218–227` | Tables and columns are emitted | Neither producer proves DDD concepts. Do not reinterpret schema as entity/aggregate identity; ER enrichment is separate |
| `Facts/EvidencePredicates.cs` | Attribute vs relation distinction is explicit | New attributes require coordinated declaration, never turn names/API versions into graph nodes |
| `Workbench/ContextMapSurface.cs:39,46` | Source delegate, selection event, invalid/no-map/undeclared states | Reuse interaction expectations, not source as a drop-in E2 view |
| `Workbench/CanvasSurface.cs:74,137` | Existing canvas is WebView-backed and loads a CanvasGraph | Not a native Atlas renderer by renaming it; E2 must use agreed native composition |
| Reference pin `AtlasReaderContracts.cs` | Scope/epoch/manifest opaque tokens; select/restore receipts; source bounds/coverage; `IAtlasReaderQueries` | Preserve admission, tokens, cancellation and Source/Back. New architecture operation needs a shared contract, not E2 bypass |
| Reference pin `AtlasSourceBinding.cs` | Manifest/file/policy/root/content hash binding | Current line:column provenance cannot itself mint a binding or validate stale source |

Actual mock files are `docs/mockups/uml-erm-surfaces.md` and `.html` (not a directory).
The opened Markdown specifies read-only derived views, provenance, dashed labelled inference,
generating/error/too-large states, and immutable model interaction. The HTML was not rendered
in this unit; no visual pass claimed. Proposal `1065a851e4039fc80804ca92f2730d9f23e6adfc`
`docs/proposals/code-atlas/mockup.html` was inspected read-only for domain/Azure navigation;
its embedded data is not evidence admitted into the product or this design.

Reuse Addendum E as functional/UX requirements, the proposed architecture as invariants, and
the E1 native source contract as a dependency. This new design fills E2 projection/evidence/UI
details. New source-evidence/authority or durable schema decisions require Owner/Data review;
they are not settled merely by writing DTOs. UI-design is in review mode over the named mocks,
followed by an E2-specific native state harness before implementation acceptance.

## 3. Requirement disposition

| Obligation | Supported source / required behavior | Exact gap and falsifier |
|---|---|---|
| US-E7.a domain roles | Explicit evidence for every entity/value-object/aggregate/invariant; confidence plus source/spec anchor | No opened producer supplies these roles. Namespace/class/table-only input must yield unsupported role, not invented DDD. Accepted-spec intake/authority contract must be decided |
| US-E7.b ER | Keys/cardinalities/associative entities | Outside this programme, still outstanding; table/column readers do not discharge it |
| US-E7.c layer/component | Declared context grouping and source predicate crossings exist | Context is not automatically layer/component. Explicit layer/component membership producer needed; unsupported edges listed with reason/source, no guessed layer from path |
| US-E7.d target vs current | Architecture pin demands status/scope and source/spec distinction | No admitted E3 authority resolver. Display supplied clause status/scope and binding; do not calculate compliance or invent applicability. Missing authority stays unknown |
| US-E8.a resource view | Existing Bicep type/API/symbol/file/location/name-state | Family can derive from declared type with rule/version and raw type retained; logical layer requires evidence or Unknown; source anchor requires Atlas binding bridge |
| US-E8.b alias collapse | Exact declaration identity prevents local-symbol collisions | No root/alias producer exists. Merge only explicit, supported same-root evidence; equal display/folded name is insufficient. Unresolved candidates stay distinct with reason |
| US-E8.c typed relationships | Existing explicit deployment dependsOn | Parent/config/network/grant/runtime support absent from opened Bicep producer. Vocabulary distinguishes them, supported producer subset is declared, unsupported observations retained; never relabel dependsOn |
| US-E8.d no grant by lookup | Existing extractor emits no grant edge | Future grant producer must cite explicit grant declaration and subject/target binding. Name/comment/lookup candidates remain unsupported/unknown |
| US-E8.e absent examples | Enumerate only evidence-backed resources | AKS/Cosmos vocabulary alone must not instantiate nodes. No evidence is not proof of runtime absence |

**Owner forks before product work:** (F1) exact supported domain/layer evidence intake and
authority source; (F2) source-bound Azure root/alias and additional predicate producer subset;
(F3) accepted Atlas foundation and exact-revision store/binding adapter. Unknown UI states are
required but do not replace the positive supported evidence needed to close these stories.

## 4. Domain and durable representation

Bounded context is repository understanding. A **domain claim** says an explicitly identified
concept has a role under a cited source or specification; the software class is an evidence
target, not inherently a domain entity. Entity means domain identity evidenced by that contract;
value object means evidenced value semantics; aggregate root means an evidenced consistency
boundary with a named invariant. Association or folder co-location proves none of these.

An immutable **architecture snapshot** groups references under one admitted manifest/epoch.
Its invariant: every claim, edge, alias and source selection belongs to that snapshot, and no
claim's origin, confidence, authority or status is silently promoted. It references existing
assertions/documents by identity; it is not a new mutable graph aggregate/store.

Grains: existing durable fact = one extractor assertion at scope/revision; projected node = one
concept or declaration identity in a pinned snapshot; projected edge = one typed relation with
its evidence-set identity; alias membership = one proven alias-to-root association. A resource
declaration row is not one deployed resource, especially for loops/conditionals. Different
documents pointing at one concept are evidence references, not duplicate domain entities.

History: retain original assertion/document revision, extraction rule/version and origin.
Changed label/type/role/scope/root membership changes meaning and therefore requires versioned
evidence, never Type-1 rewriting of past claims. This slice adds no durable schema; projections
derive from retained facts. If source facts cannot represent a needed observation, stop for an
explicit producer/schema decision, with expand/migrate/contract and no guessed backfill.

Counts within disjoint pages of one manifest are additive. Unique roots across overlapping
pages/alias sets are non-additive. Snapshot counts across time are semi-additive (not summed
over time). Confidence, coverage ratios and authority are non-additive; unknown denominators
stay unknown. Derive totals from authoritative bounded results; do not count aliases as roots.

## 5. Proposed contract and exact seams

Names below are proposals, not existing types or granted implementation paths.

```csharp
public enum AtlasArchitectureViewKind { Domain, Layer, Azure }
public enum AtlasArchitectureRelationKind
{
    DomainAssociation, AggregateMembership, EnforcesInvariant,
    LayerDependency, ComponentDependency, DeploymentDependency,
    ParentChild, ConfigurationReference, NetworkRule, ExplicitGrant, RuntimeUse
}
public sealed record AtlasArchitectureRequestDto(
    int Version, string ScopeToken, long ExpectedCoreEpoch, string ManifestToken,
    AtlasArchitectureViewKind View, int Offset, int Limit);
public interface IAtlasArchitectureQueries
{
    ValueTask<AtlasArchitecturePageDto> ReadAsync(
        AtlasArchitectureRequestDto request, CancellationToken cancellationToken);
}
```

`AtlasArchitecturePageDto` fields: `Version`, `ScopeToken`, `CoreEpoch`, `ManifestToken`,
`View`, `Nodes`, `Edges`, `Unknowns`, existing `AtlasBoundsDto Bounds`, nullable `NextOffset`,
and `Disclosures`. Row IDs and selection tokens are opaque, issued by Core. Each node contains
stable identity, label, semantic kind, evidence refs, optional source-selection token, and
explicit semantic/identity state. Each edge contains endpoints, exact relation kind, evidence
refs, source-selection token when available, and supported/unresolved disposition. Do not
serialize unknown enum values as valid claims; version/capability negotiation is required.

Evidence ref fields: existing assertion ID or pinned document/clause ID, scope/revision,
producer/rule version, origin, confidence, source binding state, and separate optional
authority/status/scope references. An absent required source binding disables Source with
reason; it does not authorize constructing a path token. Accepted-spec authority is an input
from F1, never inferred here from a frontmatter timestamp or word “accepted”.

Resource root key remains **unresolved pending F2**. The known safe declaration key uses
workspace/scope/artifact revision/symbol identity; it is not a globally deployed Azure ID.
Root equivalence requires explicit supported identity/scope proof, not same service family,
folded name, API version, proximity or `existing` keyword alone. Alias UI preserves all
declaration anchors. An unresolved root is shown symbolically and is never merged speculatively.

Proposed owned files (request list, not ownership map):

| Files | Responsibility |
|---|---|
| `src/AiDe.Core/Understanding/Architecture/AtlasArchitectureContracts.cs` | DTOs/enums/port after shared contract agreement |
| `src/AiDe.Core/Understanding/Architecture/AtlasArchitectureProjection.cs` | Deterministic projection from admitted pinned evidence; no independent latest-store reads |
| `src/AiDe.Core/Understanding/Architecture/AtlasResourceIdentity.cs` | Explicit equivalence/unknown handling after F2; no invented root algorithm |
| `src/AiDe.App/Workbench/Understanding/AtlasDomainView.cs`, `AtlasLayerView.cs`, `AtlasAzureView.cs` | Native read-only view of same result; selection requests use approved reader host |
| `tests/AiDe.Core.Tests/Understanding/AtlasArchitectureProjectionTests.cs`, `AtlasResourceIdentityTests.cs` | Facts-to-projection and negative identity/predicate oracles |
| `tests/AiDe.App.Tests/Workbench/Understanding/AtlasArchitectureJourneyTests.cs` | Real composition, UIA, diagram/list/inspector/source/history consistency |

Shared integrator seams: `IAtlasReaderQueries` adapter/source restore, read-scope issuer and
binding, exact-revision store selection, `Ipc/WorkspaceOperations.cs`, `WorkspaceClient.cs`,
`IpcContract.cs`, daemon registration, `SurfaceContentFactory.cs`, active perspective/menu/host
files and their tests. Conductor must resolve the accepted paths at foundation and assign one
writer. Producer modifications to Bicep, attribute vocabulary or domain intake are **not**
granted by this manifest; F1/F2 must produce exact separate files/signatures first.

## 6. Vertical delivery and source navigation

Surface list: source/spec observation -> scoped/revision-bound identity -> existing assertion
store or admitted document evidence -> manifest-pinned projection -> bounded capability wire
-> native diagram/list/inspector -> approved Source select -> exact bytes or typed unavailable
-> receipt-based Back. Every arrow carries the same manifest and bounds.

1. **Domain:** one positively supported concept with explicit role and invariant, plus unsupported
   class-only neighbor -> native role/evidence -> source/spec -> Back. F1 blocks dispatch now.
2. **Layer/component:** explicit membership plus one typed source relationship and one unsupported
   edge -> native grouped view/list -> underlying assertion -> Source/Back. Context grouping may
   be shown as Context, never silently relabelled Layer. F1/F3 block dispatch now.
3. **Azure:** actual supported declarations plus existing/deferred names, loop/conditional and
   dependsOn -> native declaration view -> exact source -> Back. Positive alias proof and typed
   relationship distinctions require F2; declaration-only demo does not close all US-E8.

Source generation must be content-bound: do not turn legacy line:column into a current span
without a verified file binding. Edit-after-index, missing bytes, denied scope, manifest mismatch
or stale request produce typed unavailable/changed states. Back restores original selection and
manifest receipt, not a guessed equivalent object in the latest snapshot. Epoch/request ordering
prevents a delayed response from replacing a newer selection; cancellation never implies complete.

## 7. Native UX and UI design

Direction: evidence-first architecture exploration, readable at one altitude at a time.
Archetype: proposed spatial inspector/master-detail (`read-only graph + synchronized list +
selection inspector`, keyboard-first, explicit drill-down/history, asynchronous bounded pages).
Formal catalog signature and final token/viewport measurements remain UX review inputs, not
a claimed native craft pass. Reuse the accepted Atlas host and E1 Source/Class pivot patterns;
do not build a competing workbench, dashboard of cards, or editable diagram.

Structure: view selector and manifest/bounds banner; central typed graph with synchronized
accessible list; selection inspector showing identity, declaration/current/target status,
origin/confidence and source refs; Source and Back actions. Azure banner always says
“Declarations — deployed state not observed.” Domain roles without supporting evidence appear
as Unknown, not unlabelled entity boxes. Layer unsupported edges have a visible count/list.

State matrix: no workspace/needs-index, loading, available, no supported evidence, partial,
truncated, unknown denominator, unsupported producer, invalid context map, stale/refreshing,
cancelled, failed scoped read, changed/unavailable source, disconnected/revoked lease. Preserve
last readable stale result with explicit state; no sample production data or green zero-coverage
claim. Each state has a next action consistent with authority (index/retry/change scope/back).

Reuse actual WPF resource keys from the reference native design: `SurfaceBrush`,
`SurfaceSunkenBrush`, `TextBrush`, `TextMutedBrush`; no global token changes. Full labels wrap or
expand without losing identity. Relation kind and uncertainty use text/legend plus stroke style,
not color alone. No rainbow confidence map or runtime animation. Read-only actions refuse edits
and point to authorized source navigation. Inert hostile text cannot invoke URLs/tools/clipboard.

Keyboard/accessible contract: every node/edge selectable from a virtualized list; graph and
list selection agree; UIA name includes kind, identity and uncertainty; Source/Back reachable
and focus returns to original item. Test high contrast, light/dark, reduced motion, 100/150/200%
DPI, narrow host and long identifiers. Relation direction must remain accessible in text.

Performance: inherit accepted Atlas request/frame/page budgets and charge metadata/source units
separately. Bound rows, edges, text bytes and layout work before publication. No numeric latency
budget is invented here; SRE/native spike must record hardware, workload, p50/p95 and choose
the budget before dispatch. Emit normal-path duration, examined/returned/omitted rows and
bytes, unresolved counts, cancel/failure code, and manifest correlation; no raw source/secrets
in telemetry. Spend/model tokens are N/A because no model or cloud calls occur.

## 8. Red-first proof and review floor

| Oracle (proposed test name) | Failing input / mutation |
|---|---|
| `DomainRoleRequiresExplicitEvidence` | Class/table/namespace alone becomes Entity or AggregateRoot; delete evidence requirement |
| `TargetClauseIsNotCurrentImplementation` | Target or unknown-authority clause shown as current implemented truth; erase status/scope |
| `LayerEdgeRetainsPredicateAndSource` | Untyped crossing silently becomes dependency; drop source binding or unsupported row |
| `ResourceAliasNeedsRootProof` | Same folded name in two scopes collapses; explicit supported same-root aliases fail to collapse |
| `DeclarationIsNotDeployment` | Loop count becomes deployed count; existing declaration becomes created resource |
| `LookupCannotGrantAccess` | Lookup/comment/annotation creates grant; swap config/dependency/grant kinds |
| `AbsentServiceIsNotInvented` | AKS/Cosmos fixture absent but vocabulary creates node |
| `PinnedFactsCannotReadLatest` | One scope refreshes/fails while response combines generations; mutate manifest validation |
| `ArchitectureSourceBackKeepsBinding` | Changed bytes highlight old span; Back restores latest rather than receipt; two identical names alias |
| `WireAndNativeKeepUnknowns` | Drop bounds/origin/status/source/unknown field; diagram differs from list/inspector |

Use synthetic, locally authorized fixtures, real SQLite/IPC and actual native host composition.
No private mock corpus import. Prove positive and negative oracles before fixes; mutated assertion
removal must fail the intended control, not only crash. Parser support needs unsupported-syntax
cases; paging/order invariants use deterministic generated inputs. Testing union: D0/D1/D2,
D3 composition, D4 store/files, D5/D6 wire, D7 substitutes; native accessibility/lifecycle,
source privacy and cancellation/interleavings. Existing baseline suites are not new feature proof.

Failure/STRIDE dispositions: deny forged/cross-workspace tokens through existing reader authority;
detect source/manifest tampering via binding; retain origin to prevent repudiation; minimize
spec/source text and redact secure Bicep values to avoid disclosure; bounded pages/layout/cancel
mitigate resource exhaustion; no data-driven execution/navigation authority prevents elevation.
LINDDUN: repository/work data stays local; no model/cloud egress, export or new raw-data retention;
existing source access policy governs read and history. New persistence or publication would need
separate review, not this display grant.

Required independent lenses: Test, Data/Persistence and DDD/UML (roles/identity/invariants),
UX/IA and UX/accessibility (native graph/list/navigation/states), Security/Privacy (source and
grant claims), Distributed Systems (wire/lease/epoch), C#/native, SRE and Simplifier. Release
checks version/capability compatibility. No author self-clears these gates. Re-plan if evidence
intake, foundation, grant or resource-root semantics change; keep E1/E2 shared guards jointly
satisfiable and shared-file writers unique.

## 9. Exit and residuals

Completed: requirement/source mapping, proposed contract/files, explicit evidence rules,
bounded vertical designs and falsifiers. Remaining: F1/F2/F3 Owner decisions, exact grants,
accepted foundation, formal native signature/performance contract, independent design review,
all implementation and native proof. Best next action: Conductor submits the three forks and
manifest to programme Owner/Core; dispatch no producer or UI implementation until resolved.

Confidence: source observations above Verified-by-reading only; all new API/design choices
Proposed; runtime correctness/latency and native craft Unverified. Historical source comments'
measurements were not rerun. Two path lookups were corrected during grounding (ContextProjection
filename and mock files vs directory); neither absent lookup is used as evidence of absent
capability. No full US-E7/8 acceptance is claimed.
