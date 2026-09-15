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

Revision authority: independent eight-P0 review `593c7650` and programme Owner supplement
`a766afa88cb4cbbd20c84dc383b213a4ab4e5e94`. This revision incorporates normalized G6,
shared logical envelope and measurement protocol; it clears no independent veto. Core mock
request `req-01M2KCTTG4ZWAC2K01PB1HYWGK` remains pending; no HTML was authored here.

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

**Owner dispositions before product work:** F1/F2 design choices were settled by Owner
`57a09ed6`: versioned explicit JSON carrier, exact declaration roots, source-declared aliases,
and equality only for fully known deployment identities. The bounded spike repair `90189411`
observed 28 Windows checks and three rejected subject faults; independent re-review remains
separate. It did not qualify a fully known deployment-identity producer. F3 (accepted Atlas
foundation and exact-revision store/binding adapter) remains open. Unknown states do not
replace the positive evidence required to close a story.

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
    long RequestSequence, string RequiredCapability,
    AtlasArchitectureViewKind View, string? SelectedObservationToken,
    string? SourceBindingToken, int Offset, int Limit);
public interface IAtlasArchitectureQueries
{
    ValueTask<AtlasArchitecturePageDto> ReadAsync(
        AtlasArchitectureRequestDto request, CancellationToken cancellationToken);
}
```

`AtlasArchitecturePageDto` fields: `Version`, `Capability`, `ScopeToken`, `CoreEpoch`,
`ManifestToken`, `RequestSequence`, `View`, `SelectedObservationToken`, `ProjectionObservationToken`,
`SourceBindingToken`, Core-issued nullable `RestoreReceipt`, nullable `RestoreUnavailableReason`,
`Completion`, `ObservationCoverage`, `Nodes`, `Edges`, `Unknowns`,
existing `AtlasBoundsDto Bounds`, nullable `NextOffset`, and `Disclosures`.
Row IDs and selection tokens are opaque, issued by Core. Each node contains
stable identity, label, semantic kind, evidence refs, optional source-selection token, and
explicit semantic/identity state. Each edge contains endpoints, exact relation kind, evidence
refs, source-selection token when available, and supported/unresolved disposition. Do not
serialize unknown enum values as valid claims; version/capability negotiation is required.

Evidence ref fields: existing assertion ID or pinned document/clause ID, scope/revision,
producer/rule version, origin, confidence, source binding state, and separate optional
authority/status/scope references. An absent required source binding disables Source with
reason; it does not authorize constructing a path token. Accepted-spec authority is an input
from F1, never inferred here from a frontmatter timestamp or word “accepted”.

The F2 declaration-root key is exact workspace/scope/file/resource-symbol identity; the
observation retains revision/content hash separately. This corrects the earlier wording that
put revision into the stable declaration key. It is not a globally deployed Azure ID.
Root equivalence requires explicit supported identity/scope proof, not same service family,
folded name, API version, proximity or `existing` keyword alone. Explicit aliases carry a
validated `rootRef` to one declaration, plus their own bound source anchors. RootRef cannot
equate two resource declarations. Alias UI preserves every anchor. Cross-declaration equality
remains unresolved until an admitted producer supplies fully known deployment scope, complete
provider/type chain and complete name/parent chain. No normalization beyond exact representation
is qualified. Unknown scope, expression, module binding or loop instance refuses equality.

### 5.1 F1 carrier and typed semantics

The proposed author-facing carrier is `docs/atlas-architecture.json`, version 1, read only via
the admitted source-policy/manifest binding. It is **not** a newly granted production path.
Unknown fields, duplicate IDs and invalid closed-set values reject the document. Missing,
stale or out-of-scope evidence targets yield explicit unresolved bindings; no trusted role,
relationship or Source action may be derived from those bindings. A Context remains a Context.

Typed schema notation below defines required logical fields, not production DTO/codecs:

```text
ArchitectureDocumentV1 = { version:1, concepts:Concept[], invariants:Invariant[],
  layers:Structure[], anchors:Anchor[], resources:DeclarationRef[], aliases:Alias[] }
Concept = { id:LocalId, label:Text, role:Entity|ValueObject|Aggregate,
  anchor:AnchorId, root?:ConceptId, members?:ConceptId[], invariants?:InvariantId[] }
Invariant = { id:LocalId, statement:Text, anchor:AnchorId }
Structure = { id:LocalId, label:Text, kind:Layer|Component,
  dimension:Logical|Deployment, state:Current|Target|Unspecified,
  members:ConceptId[], anchor:AnchorId }
Anchor = { id:AnchorId, target:SourceTargetId, scope:ScopeId, hash:ContentHash,
  start:NonnegativeUtf16Offset, length:NonnegativeUtf16Length }
DeclarationRef = { id:ResourceId, workspace:WorkspaceId, scope:ScopeId,
  file:FileId, symbol:ResourceSymbol, anchor:AnchorId }
Alias = { id:AliasId, label:Text, rootRef:ResourceId, anchor:AnchorId }
EvidenceBinding = { target:SourceTargetId, observationToken:OpaqueToken?,
  sourceBindingToken:OpaqueToken?, state:Bound|Missing|Stale|Denied|Unsupported,
  reason:Text?, sourceRevision:RevisionId, producer:ProducerId, producerVersion:Version }
ClaimProvenance = { origin:ExplicitDeclaration|StaticExtraction|RuntimeObservation,
  verification:Verified|Inferred|Unverified, declaredState:Current|Target|Unspecified,
  authority:Unestablished|AcceptedSpecification|Disputed|Superseded,
  authorityEvidenceRef:TrustedReference?, ruleVersion:Version }
ResourceIdentity = { declarationKey:BoundDeclarationKey,
  state:DeclarationOnly|DeploymentIdentityKnown|Unresolved,
  deploymentScope:ExactScopeIdentity?, providerTypeChain:ExactText?,
  resourceNameParentChain:ExactText?, evidence:EvidenceBinding[] }
ArchitectureNode = { id:OpaqueRowId, label:Text, semanticKind:ClosedNodeKind,
  evidence:EvidenceBinding[], provenance:ClaimProvenance, identity:ResourceIdentity?,
  sourceSelectionToken:OpaqueToken?, unavailableReason:Text? }
ArchitectureEdge = { id:OpaqueRowId, from:OpaqueRowId, to:OpaqueRowId,
  kind:AtlasArchitectureRelationKind, evidence:EvidenceBinding[],
  disposition:Supported|Unresolved|Unsupported, reason:Text? }
ArchitecturePage = { version:Int, capability:CapabilityId, scopeToken:OpaqueToken,
  coreEpoch:Long, manifestToken:OpaqueToken, requestSequence:PositiveLong,
  view:Domain|Layer|Azure, selectedObservationToken:OpaqueToken?,
  projectionObservationToken:OpaqueToken?, sourceBindingToken:OpaqueToken?,
  restoreReceipt:OpaqueToken?, restoreUnavailableReason:Text?,
  completion:Complete|Partial|Cancelled|Failed|Unsupported,
  observationCoverage:KnownCount|UnknownWithReason, bounds:PerDimensionBounds,
  nodes:ArchitectureNode[], edges:ArchitectureEdge[], unknowns:ReasonedUnknown[],
  nextOffset:NonnegativeInt?, disclosures:InertText[] }
```

JSON uses the spike's lowercase/kebab enum spellings; adapters cannot accept arbitrary aliases.
All IDs are stable document-local typed references, never labels. Aggregate root resolves to
an Entity included in members; every member/invariant reference resolves; at least one invariant
is required. The label is **Declared invariant**, never Proven invariant. EnforcesInvariant
requires separate supported enforcement evidence, absent from the initial carrier. Structure
membership is explicit and does not derive from namespace, folder or existing context tables.

Carrier fields do not include `accepted`, `authority`, token-minting instructions, or runtime
observations. Such injected fields reject the input. Authority evidence comes from a separate
trusted resolver, not the same JSON or its status text. Until that resolver is admitted,
authority is Unestablished. This design represents accepted-spec input without implementing
E3 assessment, compliance, supersession resolution or authority promotion. Origin never changes
because a human accepts a claim; acceptance is a separate attributable reference.

| Producer/input | Output origin and meaning | Unknown/refusal boundary |
|---|---|---|
| Validated F1 carrier with bound anchor | ExplicitDeclaration, declared role/membership/invariant | No enforcement or accepted-authority inference |
| Existing Bicep literal facts | StaticExtraction, source declaration/type/name and direct dependsOn | Not deployed state; folded defaults retain Inferred status |
| F1 alias | ExplicitDeclaration, alias of one exact declaration | Cannot certify equality of two declarations |
| Future fully evidenced identity producer | StaticExtraction plus its version and source-bound deployment context | Not admitted; no equality from partial tuple |
| Runtime-use observation | RuntimeObservation only from admitted observed trace | Outside declaration-only scope, capability unavailable |
| Parent/config/network/grant | Separate typed producer and evidence contract | Initial capability unavailable; no lookup/comment-derived counts or grants |

Proposed strict carrier limits: UTF-8 document at most 64 KiB before parse; JSON depth 8;
32 rows per collection, 192 total rows across the six collections; 32 references per list;
256 UTF-16 units per ordinary text/ID field; no whitespace-only required string. Reject bounds
before unbounded allocation. Spike checks row/text/role/reference cases; byte/depth and real
binding/authorization remain production negative-test obligations, not already proven controls.

### 5.2 One shared logical envelope, one future wire owner

The shared E1/E2 envelope is a logical agreement: version+capability, admitted scope, core epoch,
manifest, monotonically increasing client requestSequence, opaque selected observation and
source binding, completion, per-dimension bounds and Core-issued Restore receipt. It is not
a second DTO framework. Exact CLR types, operation names, JSON options, token issuers and golden
payloads are frozen only after the foundation pin, under the single Core/integrator writer.

Completion = Complete|Partial|Cancelled|Failed|Unsupported. This is **page work completion**,
separate from ObservationCoverage, unknown semantic targets and remaining continuation. A
complete bounded page may have further pages or unknown targets. `Complete` requires that
all returned claim/edge/source bindings share the manifest and every omission dimension is reported.
Transport failure never becomes an empty Complete graph. Unknown denominator carries a reason,
not zero. Invalid/unknown enums, capability mismatch and excessive limits fail closed.

Publish only if response scope/epoch/manifest/view/sequence still matches the active request.
Late N cannot replace N+1 even when cancellation raced. Opaque source bindings are never made
from carrier paths. Core issues a receipt for admitted selection; Back submits that receipt via
the accepted Restore path and restores exact prior manifest/observation/focus. Expired/revoked
receipts yield typed unavailable state, not lookup of a similarly named newest object.

The response's projection observation token binds its evidence set and never substitutes for
a row's source-selection binding. RequestSequence is positive and strictly increasing within
the owning view/session; retry uses a new sequence. Invalid bounds/token/version/sequence
produce typed refusal with no new usable selection or receipt; old evidence stays explicitly old.

A Partial page may receive a Core Restore receipt only if the selected observation is fully
published, source-bound and authorized for the exact manifest, with none of its required fields
omitted. Omission of unrelated rows is recorded in the receipt's result context. If the selected
observation itself is clipped, unbound, revoked or incomplete, receipt is absent with
RestoreUnavailableReason. Cancelled/Failed/Unsupported pages mint no new receipt. Previously
accepted receipts may remain visible only with their original state/manifest and validity.

Golden cases owed at foundation freeze: supported request/page, unsupported capability, partial
page with unknown denominator, cancelled page, unavailable source, receipt restore, malformed
enum/oversize rejection, N-after-N+1 interleaving, changed bytes with old receipt. Every golden
payload preserves origin, declared state, authority reference, identity state and relation kind.

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
   class-only neighbor -> native role/evidence -> source/spec -> Back. F1 design is settled;
   real binding/producer implementation, foundation and independent grants still block dispatch.
2. **Layer/component:** explicit membership plus one typed source relationship and one unsupported
   edge -> native grouped view/list -> underlying assertion -> Source/Back. Context grouping may
   be shown as Context, never silently relabelled Layer. Real carrier admission and F3 block dispatch.
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
Governing family: **G6 Multi-Panel Data Terminal**, adapted exactly as Addendum E C1 to the
linked native code-understanding workstation. The previous prose-only archetype is superseded.
The signature resolves the duplicate Type facet and illegal multi-valued Color spelling through
documented extension facets, without changing the governing family:

```text
DataTerminal { Type:DSS; Arch:SPA; Layout:MultiPanelWorkstation; Density:UltraDense;
  Nav:CommandPalette+Sidebar; Viewport:DesktopBound; Input:KeyboardFirst+PrecisionPointer;
  Color:DarkAdaptive; x-typography:MonospaceTechnical; x-highContrast:System;
  Depth:Flat; Sync:LocalFirst; Persistence:Session; Feedback:Instant+Confirmed;
  Motion:None; Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA+HighLegibility+ReducedMotion;
  x-platform:windows; x-framework:wpf; }
```

| Facets | Concrete WPF resolution / phase deviation |
|---|---|
| Type/DSS, Arch/SPA | One admitted workspace-reading host, no new website/router or transactional editor |
| Layout/MultiPanelWorkstation, Density/UltraDense | Linked view selector, graph/list and inspector/source panes; dense rows with full identity and no clipped source |
| Nav/CommandPalette+Sidebar | Existing command/perspective entry and Atlas navigation remain; no competing global menu |
| Viewport/DesktopBound, Input/KeyboardFirst+PrecisionPointer | Resizable native host; all selections accessible from keyboard list and pointer graph |
| Color/DarkAdaptive, x-highContrast/System | Existing dark/light resources; Windows high-contrast palette takes precedence, no color-only distinctions |
| x-typography/MonospaceTechnical, Depth/Flat | Existing code/technical text resource references and flat read-only panels; no new global font tokens |
| Sync/LocalFirst | Local immutable snapshots and explicit refresh; unlike generic G6, no polling, multiplayer or live market stream |
| Persistence/Session | In-memory host view/selection and Core Restore receipts only; no new durable model or guaranteed restore after lease expiry |
| Feedback/Instant+Confirmed | Immediate selection/focus acknowledgement; source/query result confirmed only by matching envelope |
| Motion/None, Transition/HardCut | No animated graph transitions; reduced motion remains a no-op consistent setting |
| Pacing/Freeform | User switches among admitted views freely; unavailable producer disables only its unsupported action |
| A11y/floor, x-platform/windows, x-framework/wpf | Named UIA controls, native focus and serialized shown-window proof at all specified DPI/themes |

Phase deviations are explicit: E0 remains physical reader; E1 Class/Sequence/Activity uses only
its admitted static evidence and source pivots; E2 Domain/Layer/Azure adds linked read-only views
and provenance, never live deployment telemetry. E3/E4 are not controls in this horizon.
No 3D viewport, editable parameter history or authoring persistence from G1 is introduced.
The final signature must match E1's Owner-approved signature at shared design review.
Reuse the accepted Atlas host and E1 Source/Class pivot patterns;
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

Performance: Addendum E A10's authoritative ceilings are open **<=2 s p95** and filter/retained
view switch **<=150 ms p95**. The proposed measurable E2 profile below supplies the remaining
caps; these are **targets requiring measured acceptance**, not observations or relaxed defaults.
Accepted foundation limits can tighten them, never silently widen them. Charge metadata/source
units separately. Emit normal-path duration, examined/returned/omitted rows and
bytes, unresolved counts, cancel/failure code, and manifest correlation; no raw source/secrets
in telemetry. Spend/model tokens are N/A because no model or cloud calls occur.

### 7.1 Hard-state transitions and forbidden output

All three E2 views use one state/view-model core. Each row below is a required harness selector
and a native-state transition oracle. A response may not change state without matching its
envelope; the same underlying row IDs and state feed graph, list and inspector.

| State and transition | Domain / Layer / Azure content | Actions and selection | Forbidden result |
|---|---|---|---|
| No workspace -> admit/index | “Open a workspace to inspect declarations.” / same / same | Open available; Source disabled; no selection | Demo objects or fake zero coverage |
| Needs index -> loading | “Index this workspace to load evidence.” / same / same | Index available; Source disabled | Complete graph from unindexed files |
| Loading -> ready/failed/cancelled | “Loading declared concepts…” / “Loading layer evidence…” / “Loading resource declarations…” | Cancel enabled; prior selection retained only if explicitly stale | Blank success state or unbound source |
| Ready unselected -> selected | Role/state rows / typed edges / declaration roots | Graph/list select; Source disabled until valid selection | Implicit random first-node authority |
| Ready selected -> Source -> Back | Declared invariant and authority / source predicate / alias and identity state | Source iff bound; Back iff valid receipt; restore originating view/item/focus | Latest-by-name restore |
| No supported evidence -> change scope | “No supported concept declarations.” / “No declared layers.” / “No supported resource declarations.” | Change scope/index available | Invented DDD/layers/services; runtime absence claim |
| Unsupported capability -> compatible view | “Declared concept producer unavailable.” / “Layer producer unavailable.” / “Relationship producer unavailable.” | Compatible view available; unsupported action disabled | Unsupported predicate hidden inside Complete |
| Unknown evidence/authority -> inspect reason | Unknown role/Unestablished authority / unsupported edge / symbolic root | Inspector available; Source only for independently bound observation | Marker/name promoting authority or grant |
| Partial/truncated -> next page | Shared count/denominator banner and explicit omitted records | Next only if cursor exists; keep selected row/receipt | Silent omission or treating aliases as root count |
| Invalid carrier/context -> correct source/retry | Validation reasons; no partial declared model | Authorized source or retry; preserve prior result as stale | Mixing valid and rejected rows as current |
| Stale/refreshing -> fresh/failed | Prior readable result plus “Refreshing — showing prior snapshot.” | Selection stays on prior manifest; refresh cancel available | Old result relabelled new/current |
| Cancelled -> retry | “Read cancelled; displayed evidence is incomplete.” | Retry/Back where valid; selection retained | Cancellation promoted to Complete |
| Failed -> retry | Stable code, affected scope, reason for each view | Retry/Back where valid | Empty-success substitution |
| Changed/unavailable source -> Back | Typed source state; no old span highlight | Back retains original context; Source disabled with reason | Highlighting new bytes at old location |
| Disconnected/revoked -> re-admit | “Workspace read session ended.” | Reconnect/re-admit; old Source/Restore disabled | Reusing expired authority tokens |
| Overflow/long labels -> bounded list/page | Accessible full label/detail, explicit row/text caps | List and paging available; maintain focus | Clipped identity, invisible overflow or UI freeze |

### 7.2 Per-view equality, keyboard and native falsifiers

Every control below runs through actual registered WPF composition in the serialized desktop
slot. The synthetic HTML harness is direction/state evidence only and cannot satisfy it.

| Proposed control | Exact failing oracle |
|---|---|
| `DomainGraphListInspectorEqual` | Concept/invariant/aggregate member ID, role, declared state, authority or evidence-ref set differs between graph, list and selected inspector |
| `LayerGraphListInspectorEqual` | Layer/component dimension, edge endpoints/direction/predicate, evidence or unsupported count differs; context is relabelled as layer |
| `AzureGraphListInspectorEqual` | Root/alias count, retained alias anchor, existing/loop/conditional/name state, relation kind or unresolved identity differs |
| `DomainKeyboardSourceBackRestoresFocus` | Keyboard selects aggregate/invariant, opens bound source and Back fails to restore the exact selected row ID, manifest, scroll/focus target |
| `LayerKeyboardSourceBackRestoresFocus` | Keyboard chooses an edge then Source/Back returns to endpoint/another edge or loses unsupported-edge position |
| `AzureKeyboardAliasSourceBackRestoresFocus` | Choosing alias B opens alias A source or Back returns to root instead of the originating alias row |
| `DomainUiaStatesReachable` | Any state in 7.1 is unreachable; UIA name omits role/identity/uncertainty or Source enabled on an unbound claim |
| `LayerUiaStatesReachable` | UIA relation omits direction/type/unknown reason; keyboard cannot reach unsupported list and return |
| `AzureUiaStatesReachable` | UIA calls declaration deployed, omits alias identity, or grants from name/lookup |
| `ArchitectureNativeThemeDpiMatrix` | In Domain/Layer/Azure at dark/light/system-high-contrast and 100/150/200% DPI, long 256-unit labels lose readable identity, focus indicator disappears, required control is unreachable, or color is sole state encoding |
| `ArchitectureResponseRaceAndRestore` | Deliver N after N+1 or restore old receipt after new manifest; any graph/list/inspector mixes snapshots or source bytes |

For each view/state/theme/DPI case record exact fixture seed, viewport, manifest, selected IDs,
UIA tree/readback and screenshot with expected/actual equality. Required viewports: 1280x720
and 1920x1080 logical pixels plus 640-pixel-wide docked host, with scroll/list fallback.
The finite native matrix is three views x required states x three themes x three DPI levels;
automate state/readback checks, with independent visual review of representative long-label,
unknown, overflow and focus paths. Any sampling of visual review is recorded explicitly and
does not skip automated state reachability. No native run is claimed by this design.

### 7.3 Performance fixture and numeric admission targets

Proposed authorized synthetic fixture `E2-PERF-v1`: 32 concepts including four aggregates,
16 invariants, eight logical/deployment structures, 32 declaration roots, 32 aliases,
96 source-backed typed edges, 16 unsupported edge records, and 256-unit worst-case labels.
Use deterministic seed 1 and synthetic bound source buffers; a second envelope test requests
one item/byte beyond each cap. This fixture is not a private repository or deployed inventory.

| Axis | Target / hard cap | Negative control and source |
|---|---|---|
| Open | <=2 s p95 | A10; opening timer ends only when graph/list/inspector show matching ready/partial state |
| Filter/retained view switch | <=150 ms p95 | A10; injected delayed publication exceeding target fails measured acceptance |
| UI-thread work | No continuous Atlas UI-thread segment >16 ms | Owner's proposed threshold, not measured capacity; instrument start/end of every Atlas dispatcher segment; an injected >16 ms segment fails |
| Request | <=8 KiB total UTF-8 request; positive sequence and bounded token/text fields | Proposed limit, enough for opaque envelope/windows without source bodies; exact-cap/one-over refusal |
| Request page | 1..32 nodes/root rows, <=96 edges, <=32 alias records, <=32 unknown records | Validate before work; exceeding hard cap rejects/truncates with explicit dimension |
| Structural display nodes | <=32 extra group/gap nodes, <=64 total primary+structural nodes | Proposed auxiliary-graph cap; structural node32/33 and total64/65 controls prevent hidden unbounded graph |
| Payload | <=256 KiB UTF-8 metadata per page, ordinary field <=256 UTF-16 units | Measure serialized bytes before publication; exact-cap/one-over cases |
| Total response | <=384 KiB serialized UTF-8 including envelope, metadata and source | Proposed ceiling allows256KiB metadata+64KiB source+bounded framing; measure actual total, refuse/truncate before publication |
| Source | Requested source window <=16 Ki UTF-16 units and <=64 KiB UTF-8 text, further bounded by accepted foundation | Never conflate source/metadata units; byte and UTF-16 boundaries each tested |
| Layout | At most 32 visible node/root rows and 96 visible edges per publication; at most 10,000 layout operations | Operation counter/cancellation; one-over returns bounded omission, not unbounded retry |
| Carrier | 64 KiB UTF-8, depth 8, 32 rows/collection, 32 refs/list, 256-unit required text | Byte/depth/row/list/text boundary negatives before allocation/publication |

Measurement protocol target: record CPU/model, physical/logical cores, RAM, Windows build,
.NET build, display refresh/DPI, power mode and actual fixture hash. Hardware is **not recorded
yet**; measured admission cannot pass until that record exists. Owner protocol: 20 cold-view
samples per view with fresh view and projection cache, application startup excluded; 100 retained/
warm samples per view reported separately. Retain all raw samples, cache state and exact fixture
hash. Opening starts at user action and ends at correctly rendered/interactable matching content;
separate query/render durations explain the total. Report nearest-rank p95, min/max and failures;
do not silently exclude cancelled/failed samples. UI-thread samples cover every continuous Atlas
dispatcher-work segment; this is not a claim about every OS/compositor rendering frame.
Bound checks are deterministic; timing checks require this hardware
calibration and no machine-independent sleep/assertion trick. Targets may be tightened after
measurement; relaxing an authoritative ceiling requires Owner/spec decision.

The source/blob, scope token and selected IDs in telemetry must be privacy-minimized; publish
durations/counts/unit-bearing limits and opaque correlation only. Not-recorded values remain
nullable with a reason, never zero. Failure to acquire reliable timing or required native
state evidence blocks dispatch/acceptance; a mock screenshot cannot substitute.

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

Completed as design material for re-review: normalized Owner G6/facets, F1/F2 typed carrier and
provenance/identity rules, logical envelope including projection observation/partial Restore,
hard-state/per-view native oracles, proposed caps and Owner20cold/100warm protocol.

| P0 | Revision disposition; no author clearance |
|---|---|
| P0.1 | G6 signature/facets/phase deviations now explicit; cross-lane UX round-trip review owed |
| P0.2 | Typed carrier, origin/authority, symbolic roots/aliases and byte/depth/row/text limits now explicit; full deployment producer remains open |
| P0.3 | Shared logical semantics adopted; foundation mapping and actual codec golden bytes remain open |
| P0.4 | Accepted foundation, exact code grants and jointly satisfiable shared writer guards remain open |
| P0.5 | Transition/forbidden-content/focus contract added; granted executable mock harness and rubric remain open |
| P0.6 | Numeric targets, fixture and sampling protocol proposed; actual hardware/timings and independent SRE/Test threshold review remain open |
| P0.7 | E1-only semantic/page contract, not cleared or changed by E2 |
| P0.8 | Per-view equality/keyboard/UIA/theme/DPI/long-label/state falsifiers now explicit; independent design review and later native execution owed |

Best next action: independent re-review of this frozen design while Core resolves foundation
and exact mock grant. No producer, production DTO/codec, native surface or deployment-context
implementation is admitted by this revision. ER/E3 remain outside horizon; US-E8.b remains open.

Confidence: source observations above Verified-by-reading only; all new API/design choices
Proposed; runtime correctness/latency and native craft Unverified. Historical source comments'
measurements were not rerun. Two path lookups were corrected during grounding (ContextProjection
filename and mock files vs directory); neither absent lookup is used as evidence of absent
capability. No full US-E7/8 acceptance is claimed.
