---
id: design-code-atlas-e1-static-views
title: "Code Atlas E1: bounded concrete class and member views"
type: design
status: proposed
owner: "@timianmalloo"
phase: "atlas-e1-detailed-design"
tags: [code-atlas, concrete-code, static-views, native-wpf, source-provenance]
review-by: 2026-09-21
links:
  - { to: spec-addendum-e-code-atlas, rel: refines }
  - { to: architecture-code-atlas-proposed, rel: depends-on }
  - { to: design-code-atlas-shared-host-admission, rel: depends-on }
  - { to: proof-code-atlas-live-reader-candidate, rel: depends-on }
  - { to: proof-code-atlas-contract-grounding, rel: depends-on }
summary: >
  Proposed detailed design for a file-scoped class-occurrence view and exact
  member-to-source navigation using the existing Atlas reader authority.
  Defines a narrowly bounded first implementation tranche, explicit nonclaims,
  source-review seams, falsifying tests and independent gates still required.
---

# E1 concrete static views — detailed design, not implementation authority

## 1. Decision, authority and terminal condition

**Proposed:** add a read-only, file-scoped classifier-occurrence view inside the
existing Architecture `CodeAtlas` surface. A selected class shows its directly
declared supported members. A member opens the existing hash-bound source journey.
The first tranche proves declaration structure, **not a project type system**.

Owner63's acceptance of bounded E0 and admission of E1 detailed design are inputs
from the parent task. They are **not** acceptance of this design, a normative
Addendum E/main grant, or permission to edit every E1 source file. The candidate
spec remains `draft`; the architecture remains `proposed`. This document leaves
their status unchanged.

Baseline inspected: `8b9232d5c0f7bebcaada0cdc01e7eff193e98bb3`.
Authored scope: this file only. Parent owns audit, derived index, collaboration
registers, review dispatch and integration. Retain the design worktree for that
handoff. No source, project, dependency, registry or generated artifact is changed.

**Done for this author:** a committed, source-grounded design with a bounded
implementation proposal, explicit contracts, negative oracles and unmet gates.
**Done for implementation:** the independently admitted tranche passes the
producer-to-real-window proof in §10. These are different terminal conditions.

### Opening contract and execution graph

| Field | Contract |
|---|---|
| Goal | Design the first concrete class/member visualization connected to real E0 |
| Done when | This design is committed; limits, seams, tests and remaining gates are named |
| Not in scope | Product implementation, shared records, global regeneration, later phases |
| Tier | T2 design: public payload, source authority and native UI implications |
| Fan-out cap | Zero author children; parent funds four independent review leaves |
| Main-line budget | Twelve leaf calls total, including skills, reads, claim, edit and commit |

Graph: **ground actual sources → model/contracts → disconfirm → validate/commit
→ parent review/admission**. Edges are data/decision dependencies. Grounding is
Reasoning; validation and commit are Deterministic mechanics; parent gates are
Independent review. This author does not collapse across the independent gate.
The authoring path is serial: in call units its planned work and span are both
12, hence no author fan-out speedup. This is a budget model, **Inferred**, not a
latency measurement. Wall-clock and token cost are not recorded here.

Alternative framing considered: render a whole-project class graph first.
Rejected because the actual producer supplies a single-file compilation and the
public outline lacks relationship evidence. Another alternative is a native
file-local structural view; it preserves E0 and is chosen. Missing semantics are
an explicit unsupported state, not an invitation to synthesize them.

## 2. Grounding and evidence ledger

Typed grounding path: this design `refines` candidate
`spec-addendum-e-code-atlas`; that spec's architecture dependencies lead to
`architecture-code-atlas-proposed`; the E0 design links the live-reader proof.
Those links were read from source frontmatter, not inferred from a generated
index. Proof links are inherited grounding references, **not E1 proof**.
The parent must check inbound impact and regenerate the index at integration.

The cited paths below are repository-relative. Ranges identify the source read
receipt; a signature read is not a claim to have executed its implementation.
Search output elided portions of several large files. Only the displayed
contracts are promoted below; unexposed lifecycle bodies remain source-review
obligations. No private corpus, proposal branch, new SDK or inspected project was
loaded or executed.

| ID | Read receipt / established statement | Confidence and limit |
|---|---|---|
| R1 | `docs\specs\addendum-e-code-atlas.md:2–16,254,270,328,600`: candidate status; unsupported-file explanation; visible altitude; stale/refreshing; phase-appropriate keyboard/native accessibility | **Verified source excerpts**; no blanket spec acceptance |
| R2 | `docs\architecture\code-atlas-proposed.md:2–16,85,931–932,952–982`: proposed status, staged views, E0 live-read policy and separate later admission | **Verified source excerpts**; historic test reports are not rerun evidence |
| R3 | `docs\design\code-atlas-shared-host-admission.md:2–14,581–586,650,723,790,1160–1163,1222`: linked E0 design; synchronous factory; host/lease separation; production security/composition proof requirements | **Verified excerpts**; superseding source, not older prose, controls runtime shape |
| R4 | `src\AiDe.Core\Understanding\CSharpDeclarationObservation.cs:239–485`: `Observe(CSharpCompilation, AtlasCompilationScope, IEnumerable<CSharpDeclarationSourceInput>, CSharpDeclarationObservationLimits?, CancellationToken)`; unique verified source per syntax tree; declaration production, supported syntax, spans, nullable logical identity | **Verified direct range read** |
| R5 | Same file `:14–49,136–153,210–236`: verified buffer/input boundary; declaration limit default 2048; result carries declarations, completion, bounds and limitations | **Verified displayed signatures/constants**; buffer internals not fully reread |
| R6 | `src\AiDe.Core\Understanding\AtlasIdentity.cs:15–29,49–64`; `AtlasIdentityCodec.cs:19–21,49–51,76–79,188–215`: type/member identity uses scope/project/TFM/symbol; codec distinguishes missing project/TFM/profile; file-limited versus supplied-project contexts | **Verified excerpts**; do not reimplement codec encoding |
| R7 | `src\AiDe.Core\Understanding\AtlasManifest.cs:324–353,357–383`: declaration occurrence includes logical symbol, source observation, context, binding, kind, role, signature, identifier, spans and unresolved reason; manifest validates membership | **Verified constructor and member excerpts**; no existing parent relation field established |
| R8 | `src\AiDe.Core\Understanding\AtlasSourceBinding.cs:26–32,79–85,119–127`: binding components are manifest, manifest-file, policy, root, file and content hash; ordinal comparison and canonical SHA-256 shape | **Verified excerpts**; native file identity remains the existing producer's responsibility |
| R9 | `src\AiDe.Core\Understanding\AtlasQueryContracts.cs:27–40,104–106,152–186,318–322`: page maximum 128; native outline; nonmatching source states; `IAtlasQueries` inventory/select/restore | **Verified excerpts**; nonmatching states cannot be treated as fresh source |
| R10 | `src\AiDe.Core\Understanding\AtlasQueryService.cs:265–271,291–305,375–409`: reader selection/restore accept a source window; `Prepare` creates `AtlasFile` from one parsed tree and an object assembly reference, then calls `Observe` with `ForFileLimited(..., null)` | **Verified excerpts**; this is not MSBuild/project/TFM loading |
| R11 | `src\AiDe.Core\Understanding\AtlasReaderContracts.cs:1–103`: complete direct read of public DTOs and reader/lease interfaces | **Verified**; existing outline row has only declaration token, display name, kind and span |
| R12 | `src\AiDe.Core\Understanding\AtlasReaderProjection.cs:13–16,161–189`: frame/content/input constants; `Selection(SelectionProjection, AtlasSelectRequestDto, AtlasSelectionPhaseContext, binding-token issuer, declaration-token issuer)` projects bounded outline rows | **Verified signatures/excerpts**; projection currently discards potential structural metadata |
| R13 | `src\AiDe.Core\Understanding\AtlasReadScopeIssuer.cs:29–34,157–204,283–300,427–428,478–479,507–555`: policy/membership/budget composition, select/restore checks, operation-current predicate, writer completion | **Verified excerpts**; all publication/drain branches require parent source review |
| R14 | `src\AiDe.App\Workbench\Understanding\AtlasReaderView.cs:23–36,881–927`: existing owner, file map/tree, outline list, source editor, Back, status/bounds text, native file and outline row objects | **Verified excerpts**; full constructor/event/lifecycle bodies were elided |
| R15 | `src\AiDe.App\Workbench\SurfaceContentFactory.cs:19–21,47,867–885`: factory has `AtlasWorkspaceOwner` composition seam and token-based wrapped unavailable text | **Verified excerpts**; exact CodeAtlas row/defaultCenter mapping is parent baseline evidence, not independently reread here |
| R16 | `.github\instructions\testing-strategy.instructions.md:54–67`: D1–D7 trigger table and AI-specific triggers | **Verified displayed table**; union applied in §10 |

**Parent-provided baseline facts, not newly executed here:** CodeAtlas is the
Architecture Center host with `defaultCenter` and full-label wrapping; the native
file/tree → member → source → Back journey uses a real Core issuer, isolated
pipe, factory and lease owner; publication retains pins through writer drain;
Git capture uses short critical sections rather than idle locks. Preserve all
of these. E0 was accepted on a real daemon/window fixture at width 1280. That
does not prove enterprise/project-wide behavior, all accessibility requirements,
other viewport sizes, or E1.

**Evidence gap that blocks source dispatch, not design authoring:** the complete
native event/selection constructor contract, factory registration and complete
publication/transport validation paths were not exposed by the bounded excerpts.
The exact extension points and protocol negotiation must be read by the parent
SourceReviews before granting source changes. Do not implement from names.

## 3. Model first: concrete declarations, not domain entities

### 3.1 Conceptual model and invariants

Bounded context: **observed source structure**. Ubiquitous terms:
*classifier occurrence*, *member occurrence*, *declaration containment*,
*source observation*, *profile*, *selection receipt*. A class in source is not
automatically a domain entity, aggregate, database table or architectural component.

The existing admitted reader scope is the authority boundary, not a new aggregate.
The existing manifest/source binding protects the invariant that a displayed
source span belongs to exactly the admitted observation. A proposed static-view
projection is an immutable value, not a new durable aggregate.

| Concept | Identity / grain | Invariant |
|---|---|---|
| Classifier occurrence | One supported classifier declaration in one observed file and compilation context, keyed by existing declaration occurrence identity | Never merge by display name or partial keyword |
| Member occurrence | One supported method, constructor, property or accessor declaration in that same observation | Overloads and partial definition/implementation remain distinct occurrences |
| Containment observation | One immediate lexical parent → child declaration pair, in one observation/profile | Both endpoints come from the same verified syntax association; child has at most one immediate parent |
| Static page | One admitted selection, one observation and one bounded outline page plus optional selected classifier | No field borrows identity or counts from another selection generation |
| View selection | One selected occurrence and a bounded return location within the current host lease | No stored UI token becomes source authority |

### 3.2 Logical shape and provenance

Reuse `AtlasDeclaration.ObservationKey`, `SourceObservationKey`, `ContextKey`,
`SourceBinding`, `Kind`, `Role`, spans and nullable `LogicalSymbolValue`. R4 creates
occurrence keys from context, file, hash, decoder, kind, role and span; exact codec
format remains owned by `AtlasIdentityCodec`.

The production path is **FILE-LIMITED**, project **not established**, TFM
**not established**, profile **unknown**, because R10 uses
`ForFileLimited(..., null)`. A semantic model inside that compilation does not
establish project semantics. R4 explicitly withholds logical identity in this
context. Do not fabricate a project, pretend `object` references complete the
compilation, or execute inspected `.csproj`, targets, generators or MSBuild.

Proposed additions, all **derived in memory**:

| Field | Writer → reader | Meaning |
|---|---|---|
| `ClassifierFlavor` | Existing observation pass, syntax discriminator → projection → class glyph | `Class`, `RecordClass`, `Struct`, `RecordStruct`, `Interface`, `Enum`, or `NotEstablished`; only proven class flavors receive a class glyph |
| `ParentOccurrenceKey` | Observation pass using immediate supported declaration ancestor → native outline → token issuer | Nullable; absence has a reason, not an invented root |
| `RelationEvidence` | Same observation pass → validated static wire metadata → edge/compartment text | `Extracted`, `Inferred`, `Ambiguous`; first tranche emits only extracted lexical containment |
| `ContextDisclosure` | Compilation scope producer → projection → persistent view banner and accessible description | File-limited/profile/project/TFM limitation carried explicitly |
| `RelationshipOmission` | Observation/result bounds → projection → list and diagram | Unsupported, endpoint not on page, parse ambiguity, budget, or authority refusal |

No association is inferred from `DisplayName`, capitalization, type spelling or
span nesting in App. `AtlasOutlineRowDto.Span` alone is **insufficient** to prove
parentage: it does not carry the syntax-parent relation. An interface name in a
base list is a name reference, not a bound interface realization.

For syntax recovery ambiguity, refuse the structural relation or report
`Ambiguous` with a safe reason and no UML edge. Duplicate parent candidates,
equal-span collisions, cross-file endpoints and cycles fail validation.
Malformed syntax must not be repaired into a believable class diagram.

### 3.3 Durable representation and measures

There is **no new table, fact store, migration or durable cache**. The existing
observations remain the source of truth. The dimensional-storage decision is
therefore unchanged; a new storage ADR would be speculative.

Metadata follows observation history: changed bytes/context yield a new
occurrence/projection. No update rewrites an earlier occurrence into a new class.
Partial declarations in different files remain separate; even matching signatures
do not license identity unification. File-local counters count occurrences, not
unique logical types. Counts are additive only over disjoint occurrence sets in
the same observation/profile; they are **non-additive across time, overlapping
pages, partial projections or contexts**. Coverage is non-additive and keeps the
existing Known/Unknown/Withheld distinction. Unknown is not zero.

If a projection is retained for Back, it is a bounded disposable view cache,
invalidated with the existing lease, never historical-body storage. Its
rebuild/equality oracle compares it with the same source observation and same
projection inputs; rebuilding from later live bytes is a different observation.

## 4. Supported relationship set and honest altitude

| Relationship / feature | First tranche | Representation and nonclaim |
|---|---|---|
| File → declared classifier occurrence | Supported with extracted file association | File context header and occurrence cards, not UML aggregation |
| Class → directly declared supported member | Supported after new parent evidence is admitted | UML class operation/property compartments or synchronized native rows |
| Property → accessor | Supported lexical containment | Expandable accessor rows; not separate classes or graph association |
| Class → nested classifier | Supported only with extracted immediate parent | Labeled “declares nested type” navigation; not a composition diamond |
| Class inheritance | **Not supported** | “Base types not resolved in this profile”; no triangle edge |
| Interface realization | **Not supported** | Do not draw a dashed realization arrow from name text |
| Field/property type association, dependency, calls | **Not supported** | No named-reference-to-fact promotion; fields/events already have producer limitations |
| Partial logical type unification | **Not supported** | Separate source occurrences; role shown where available |
| Method execution order / sequence / activity | **Remaining E1**, not simulated | Requires separate behavior/control-flow contracts and admission |
| Domain, ER, layers, Azure resources | **E2 later** | No automatic altitude promotion |
| Source/spec correspondence | **E3 later** | No compliance or correspondence inference |
| Governed AI interpretation | **E4 later** | No model call, context upload, generated evidence or tool delegation |

A class glyph is UML only where the classifier flavor is established. It has a
name compartment and supported property/operation compartments. Display signatures
are source-oriented text; do not invent visibility, multiplicity, abstractness
or UML type notation. Other classifier flavors get explicitly labeled occurrence
cards, not class glyphs masquerading as semantic classes.

No lifetime claim is implied by lexical containment. In particular, a composition
diamond is **not** appropriate for “this source declaration contains that one.”
The derived view is read-only. Moving a card changes view state only; editing or
round-tripping a generated diagram as source is excluded.

## 5. Producer → query/wire → class → member/source

### 5.1 Required path and preserved authority

1. Existing Architecture CodeAtlas factory/owner acquires its real
   `IAtlasReaderLease`; do not introduce a second owner or admit from a static view.
2. `IAtlasReaderQueries.InventoryAsync(AtlasInventoryRequestDto, ct)` supplies the
   bounded file chooser. `SelectAsync(AtlasSelectRequestDto, ct)` uses the existing
   scope, epoch, manifest, file token and source/outline windows.
3. Existing `AtlasReadScopeIssuer.SelectAsync` checks the connection/scope,
   file eligibility and current membership. Native `SelectForReaderAsync` runs
   the established source path. New structural metadata is computed only after
   that path has established the verified input association.
4. `AtlasQueryService.Prepare` continues to parse the admitted verified buffer and
   call `CSharpDeclarationObservation.Observe`; augment that pass rather than
   opening or parsing the file separately in App.
5. The observation pass records immediate supported syntax-parent occurrence
   keys and classifier flavor. Native query projection carries those values with
   the existing outline, completion, bounds and limitations.
6. `AtlasReaderProjection.Selection` issues only scope-owned declaration tokens,
   projects the structural metadata and validates page/endpoint consistency.
   App receives display-safe data and opaque navigation tokens, not native paths
   it can use to bypass the source reader.
7. A deterministic App projection selects the active classifier and direct member
   rows. The same selection drives class compartments, the accessible list and
   the existing source selection action.
8. Activating a member sends its issued declaration token through the existing
   selection request. Do not reinterpret a display span as a fresh source grant.
   Only the verified binding/decoder/page/highlights from the response may render
   as current source.
9. Back calls the existing restore journey and restores view selection/focus only
   after the restore response is valid. An old receipt preserves a requested
   historical location; **it is never fresh authority**. If matching bytes are
   unavailable, keep the explanation, disable highlights and offer explicit
   refresh/reselection. Do not silently attach the old span to new bytes.

Publication retains the same operation and charged buffers through writer drain.
The view cannot release a pin early, replace an invalidated lease, reconnect
silently, or hold a Git lock while the user looks at a diagram. Existing short
critical membership captures remain Core-owned. No Git checkout/stash/reset,
project execution, repository mutation or HTTP endpoint is introduced.

### 5.2 Proposed payload seam — not an assertion about today's DTO

R11's current outline row is exactly:

```text
AtlasOutlineRowDto(
  string DeclarationToken, string DisplayName,
  AtlasDeclarationKind Kind, AtlasSpanDto Span)
```

There is no parent token, classifier flavor or provenance field today. Therefore
the full first tranche needs a **reviewed structural-metadata wire extension**;
App-only reconstruction is rejected.

Proposed logical addition to each supported outline row:

```text
StaticDeclarationMetadata:
  classifierFlavor: Class | RecordClass | Struct | RecordStruct |
                    Interface | Enum | NotEstablished
  parentDeclarationToken: issued opaque token | absent
  parentState: OnPage | OutsidePage | NotApplicable | NotEstablished
  relationEvidence: Extracted | Inferred | Ambiguous | absent
  reasonCode: fixed allowlisted code | absent

Selection static context:
  contextKind: FileLimited
  projectState: NotEstablished
  targetFrameworkState: NotEstablished
  profileState: Unknown
  structuralState: Available | Unsupported | Partial | Refused | Stale
```

These are **proposed values**, not admitted CLR names or a new wire version.
Core source review must choose the exact compatible envelope/capability change
against the actual serializer and isolated-pipe negotiation before implementation.
An older peer must keep E0 usable and show “Static structure unavailable”; it
must not guess defaults for absent structural evidence. Malformed, mismatched or
unnegotiated metadata is a typed refusal, never a best-effort UML edge.

Do not add a parallel query service. Prefer the existing select response with
explicitly negotiated optional structural metadata, if its codec can prove
compatibility. If it cannot, the protocol version/registration file manifest must
be returned to Owner; no unlisted transport edits are authorized by this design.

### 5.3 Concrete review fixture

Source in one admitted file, with no real project profile:

```csharp
class Box<T>
{
    public T Value { get; set; }
    public void Put(T value) { }
    public void Put(T value, bool replace) { }
}
```

Expected structural observations: one class occurrence; one property, two accessor
and two method occurrences. Direct class membership is the property and the two
overloads; accessor parent is the property. The two `Put` tokens differ despite
the shared name. `T` is displayed as source text, **not** a bound project type.
The class compartment, native list and source action refer to the same issued
declaration tokens. No association to a node named `T` is emitted.

Symbolic wire sketch, not executable token values:

```text
selection(scope=S, epoch=E, manifest=M, file=F, observation=O)
outline:
  C: Type, "Box<T>", flavor=Class, parent=NotApplicable
  P: Property, "Box<T>.Value", parent=C, evidence=Extracted
  G: Accessor, "get Value", parent=P, evidence=Extracted
  S1: Accessor, "set Value", parent=P, evidence=Extracted
  M1: Method, "Box<T>.Put(T)", parent=C, evidence=Extracted
  M2: Method, "Box<T>.Put(T, bool)", parent=C, evidence=Extracted
context: FileLimited / project-not-established / tfm-not-established / profile-unknown
source: existing binding token, decoder, page span and highlights
```

Real display strings must be asserted against the current producer, not this
illustrative spelling. Real tokens remain issuer-owned and opaque.

## 6. Bounds, progressive disclosure and retained memory

Do not convert an enumeration default into an enterprise capacity promise.

| Axis | Established bound / proposed behavior | Measurement and refusal |
|---|---|---|
| Source input | R5/R12 declare 8 MiB input constants | Preserve the existing source verifier's raw/decoded accounting; **decoded UTF-16 accounting is a distinct admission check**, not “8 MiB UTF-8 = 8 MiB decoded” |
| IPC body | `MaxFrameBodyBytes = 1 MiB` | Structural metadata shares the same frame budget; serialized bytes measured before publication |
| Source page | `MaxPageTextUtf8Bytes = 128 KiB` | Separate from source-input size, UTF-16 requested window and metadata |
| Outline page | `PageRequest.MaxLimit = 128` | Reuse existing requested/effective row contract; no new graph row cap |
| Observed declarations | Existing default `MaxDeclarations = 2048` | Bound extraction; returned page does not establish full-file completeness |
| Visible graph | One selected classifier plus the current bounded direct-member page | No recursive eager expansion or automatic “load all”; graph projection must remain within the admitted rows/bytes |
| Edges | At most one immediate parent relation per returned child | Endpoint outside page is disclosed, not replaced with a guessed node |
| Inventory | Existing bounded inventory pages | No project-wide type index, background repository crawl or automatic page draining |
| Retention | Existing scope/receipt/operation budgets | Charge structural arrays, strings, serialization buffers and any retained page before allocation/publication; no independent uncharged diagram cache |

**Flagged before source admission:** the full decoded-buffer charge rule and
available headroom for structural strings were not directly read. The source
review must reconcile the 8 MiB input constant with decoded UTF-16 allocation,
Roslyn state, 1 MiB wire and 128 KiB source page. Neither this document nor a
constant name establishes a decoded-memory proof.

No novel latency, graph-size or memory number is adopted here. The bound for a
static page derives from the existing page/frame limits and the one-parent
invariant. If usability requires a smaller visible page or independent graph
limit, measure the native fixture and return a justified threshold for admission.

Load-next is explicit and does not imply all siblings have been observed. A
classifier with members outside the active page shows “More declarations outside
this page.” Never say “0 members” when the denominator is Unknown or Withheld.
Changing page replaces the bounded page; it does not accumulate an unbounded
collection behind the viewport. Parent context retained for navigation is also
charged and invalidated with the observation.

## 7. Native interaction, readability and accessibility

Reuse Architecture → CodeAtlas → Center, the current owner and full-label
wrapping. No new perspective/kind, WebView, graph library, icon dependency or
editable canvas is necessary. Native WPF layout and an item/list projection are
the first sufficient rung. Use the existing `DESIGN.md` tokens and resource
bindings, including established text/muted-text roles; token identifiers not
established by source review must not be invented. No arbitrary color/size values.

Archetype: a **technical explorer, hierarchical navigation plus selection and
detail**, not a dashboard, input wizard or chat. Show current altitude
`file → classifier occurrence → member → source`; conceptual system/component
altitudes remain unavailable rather than inferred from directories.

| Route | Interaction and preserved state |
|---|---|
| Developer file-first | Existing file tree → source/outline → explicit “Class view” action → selected enclosing classifier occurrence, if extracted parent evidence exists |
| Visual-first | Open existing CodeAtlas → choose “Class view” → bounded admitted-file chooser and classifier cards → select classifier → member → exact source |
| Member inspection | Class compartment and native member list select the same token; Enter activates existing source navigation |
| Back | Preserve prior file, classifier/member selection, outline page and keyboard focus request; validate restore/lease before re-enabling source |
| View switch | Switch source/class presentation without another workspace owner, admission or silent repository-wide query |

The visual-first entry is explicitly **file-scoped**. It is not advertised as a
whole-project graph. Where no classifier is on the current outline page, say so
and expose bounded next-page navigation; do not claim the file contains none.

Layout uses one clear class heading, member compartments, an accessible ordered
member list and a source/detail region already owned by the reader. Avoid an
all-types hairball. Nested types are expanded by user action. No force simulation
or random initial placement. Order is stable by source occurrence, with an
explicit deterministic tie-breaker, not by localized display-name identity.

| State / boundary | Required visible and accessible behavior |
|---|---|
| Empty workspace/file/class page | Distinguish no workspace, no supported declarations and none on this page; offer only valid next action |
| Loading | Preserve selection indication; announce loading once; do not expose a stale source action as current |
| Unsupported/profile-limited | Persistent “File-limited — project and target framework not established”; explicit unsupported relations |
| Refused | “Source access refused”; safe code/reason; no hidden path, source body or retained live highlight |
| Stale/changed | “Source changed — refresh required”; retained view labeled stale and source navigation disabled until validated |
| Error/canceled | Typed error or canceled state, retry only as an explicit new operation; no empty-success fallback |
| Overflow/budget | Requested/effective/returned/omitted information and reason; unknown denominator remains unknown |
| Long names and generics | Wrap full labels without token splitting that changes meaning; expose full accessible name; no tooltip-only identity |
| Many members | Virtualized/native bounded rows and explicit paging; visible omitted context, no whole-file eager materialization |
| Partial classes/duplicates | Separate occurrences with file/span context; no name merge |
| Cycles/invalid parents | Refuse impossible structural cycle; no loop in layout or traversal |
| Keyboard/focus | Predictable Tab order; native tree/list arrows; Enter to source; explicit Back; focus restored to logical item or announced fallback |
| Screen reader | Role/name/state for classifiers and members; bounds/provenance as text, not color; graph has an equivalent synchronized native list |
| Motion/theme | Stable layout; no required animation; respect reduced motion and existing light/dark/high-contrast behavior |

Required native proof includes normal default viewport and the accepted E0 width
1280, then at least a smaller and a larger supported window, keyboard-only use,
screen reader/automation tree, text scaling and high DPI. Dimensions other than
1280 are **to be selected from actual supported host behavior**, not asserted
as measured here. Full-label and source-readability assertions are release gates.
The separate footer synthetic-VM initialization follow-up is not part of this
design or its source grant.

## 8. Failure-mode analysis

| Category / mode | Disposition and owner | Falsifying oracle |
|---|---|---|
| Input: malformed/ambiguous syntax | Detect in observation pass; omit relation with reason | Recovery syntax produces a confident parent/UML edge |
| Dependency: no project/TFM/profile | Accept bounded file semantics, disclose constantly | Any screen or payload claims project completeness/bound inheritance |
| State: changed hash/file/native identity | Transfer to existing binding/source verifier, preserve refusal projection | Old span highlights changed bytes |
| Concurrency: late selection completion | Transfer to existing lease/selection sequencing; extend static projection invalidation | Old class page appears after newer selection or revocation |
| Publication: release during queued response | Transfer to issuer operation/publication and writer drain | Charged metadata/source freed before writer completion, or never released |
| Resource: hostile identifier/member count | Bound extraction, page, serialization and retained projection | Allocation uncharged; frame/page overflow; UI eagerly expands all declarations |
| Time: canceled parsing/query/view close | Transfer cancellation; dispose owned static values with current scope | Work or source navigation survives cancellation/close |
| Membership: Git changes during selection | Transfer current membership checks and short captures | Diagram opens source from a no-longer-eligible file or keeps an idle Git lock |
| Projection: missing parent or provenance | Reject malformed metadata; unsupported when absent by negotiated contract | UI synthesizes edge from display text or span |
| Navigation: expired receipt | Restore through existing authority, explicit stale/unavailable | Receipt restores source after revocation without fresh authority |

Accepted risk is limited to **incomplete static knowledge**, never false authority.
The UI says what is not known. No accepted risk permits a fabricated relationship,
unbounded memory or invisible inaccessible state.

## 9. Adversarial analysis (STRIDE-lite)

| Boundary / threat | Disposition: named control or proposed tested control | Negative test |
|---|---|---|
| App → issuer: scope/token forgery (S/E) | Transfer to `AtlasReadScopeIssuer` connection/scope checks and issuer-owned token dictionaries; extend structural token validation | Replay file/parent token from another scope, peer, epoch or manifest; no source open |
| Membership → source: path escape/link swap (T/E) | Transfer to existing admitted source-binding/native-reader and policy membership controls; source review must name their concrete tests | Outside-root path, replaced native object or symlink race never yields fresh text/highlights |
| Observation → wire: provenance laundering (T/R) | New structural metadata validator; no name-only or missing-evidence edge | Flip Extracted to absent, cross observation or fabricate parent; whole structural part refused |
| App display: false audit/identity claim (R) | Stable safe reason codes and scope/observation correlation; no UI-issued authority | Display name collision never aliases issued identities |
| Wire/log/UI: source or secret disclosure (I) | Existing lease permissions; new telemetry allowlist; no source/name/path in metrics or logs | Canary source/comment/secret never appears in telemetry |
| Query/layout: denial of service (D) | Existing source/frame/page/operation budget plus charged structural projection; cycle validation | Huge labels, excessive members, duplicate/cyclic parents stay bounded and cancelable |
| Stale receipt → source (S/T/E) | Existing restore checks in issuer and `AtlasQueryService.RestoreForReaderAsync` | Expired/revoked scope and changed bytes cannot reuse old authority |
| Transport compatibility (T/D) | Negotiated structural metadata, strict serializer validation, E0 fallback only for unsupported capability | Old peer stays E0; malformed new payload cannot silently become complete diagram |

All STRIDE categories are considered. RFC 9457 is **N/A for this IPC-only
tranche**. Keep existing typed IPC failures; no HTTP API is proposed.
Security review remains independent and open.

### Privacy analysis (LINDDUN-lite)

Code and repository paths may contain personal/work data; do not claim “no PII”
merely because the object is source code. Data purpose is local authorized code
inspection. Processing remains in the existing local daemon/App boundary.

| Category | Disposition |
|---|---|
| Linkability / identifiability | Keep opaque scope-owned tokens; avoid exporting paths, user names or source identifiers into analytics |
| Non-repudiation | Record safe operation/result provenance, not author identity inferred from source or Git |
| Detectability | Refusal and withheld counts must not reveal inaccessible declarations or files |
| Disclosure | No AI/provider/network egress, external renderer, clipboard/export addition or private corpus ingestion |
| Unawareness | Persistent profile/source-authority disclosure and explicit user navigation |
| Non-compliance | Existing access purpose and retention apply; rights/deletion follow workspace removal and scope invalidation; no new retained corpus |

Transient structural data is released with the existing scope/page ownership.
If implementation introduces disk caching, export or remote processing, stop:
that changes purpose/retention/egress and needs separate privacy admission.

## 10. Test contract and operator evidence

No product tests were run for this Markdown-only task. Every row below is a
**planned falsifying test**, not a green result or an E1 proof claim. Observe red
before green when the implementation is admitted.

Testing Strategy union from R16: **D0** hygiene for every test; **D1** deterministic
structural projection; **D2** parser/serializer/validator boundaries and generated
inputs; **D3** composition/dependency constraints; **D4** real filesystem and daemon
authority; **D5-provider** public reader contract; **D6** IPC golden payloads;
**D7** only where a substitute is used, paired with real-boundary contract proof.
D5-consumer HTTP/gRPC and A1–A6 AI/tool/prompt behavior are not triggered by this
non-AI product design. A design Markdown file is not an executable model prompt.

| Promise / proposed test | Oracle: what makes it fail | Composition and red strategy |
|---|---|---|
| `StaticContainmentUsesVerifiedSyntaxParent` | Wrong parent for nested types/properties/accessors; unattached syntax accepted | Real observation input; mutate parent selection |
| `StaticOccurrencesDoNotMergeNames` | Overloads, duplicates or partial occurrences collapse | Real parser + codec; replace identity with name and observe red |
| `FileLimitedNeverClaimsBoundTypes` | Bound base/interface edges or fabricated project/TFM | Production `Prepare` path; inject false profile/edge |
| `StructuralMetadataSurvivesWire` | Parent/flavor/provenance/bounds lost or altered | Native observation → query → public projection → actual codec; remove one projection field |
| `OldPeerKeepsE0WithoutInventingStaticEvidence` | Unsupported capability breaks E0 or invents defaults | Actual negotiated old/new payload fixtures |
| `MalformedStaticMetadataRefuses` | Cross-scope/observation endpoints, impossible cycle or unknown evidence accepted | Negative provider/codec tests, not UI-only tests |
| `ClassListAndSourceAgreeOnOneSelection` | Compartment/list/source use different occurrence or observation | One seeded scenario through real issuer/pipe/factory/window |
| `BackDoesNotRefreshOldAuthority` | Changed file/revoked scope permits old highlighting | Real filesystem mutation plus restore; disable binding check for red |
| `StaticPublicationDrainsBeforeRelease` | Buffer/pin released while writer still uses it | Deterministic blocked-writer interleaving; no sleeps |
| `StaticBoundsAreHonestAndCharged` | Byte/row/unit mismatch, unknown rendered zero, uncharged retained objects | Max/one-over, Unicode/surrogate, huge-name and many-member fixtures |
| `StaticViewKeyboardAndAccessibleNames` | Unreachable control, lost Back focus, truncated accessible name, absent state | Real native automation tree and keyboard-only journey |
| `StaticViewDefaultViewportReadable` | Clipped heading/member/bounds/source at normal viewport or 1280 | Real window captures; long generic names, empty/error/overflow and themes |
| `StaticTelemetryContainsNoSource` | Canary source, path, identifier, receipt or secret in logs/metrics | Real success/refusal/cancel/budget runs with telemetry capture |

Cross-surface scenario: the Box fixture in §5, then changed bytes, next-page
members and an inaccessible sibling file. Assert the file chooser, classifier
card, member list, source header, highlight, profile banner and Back response
agree on the same admitted observation. No test constructs only the final view
model and calls that the production proof.

Native readability and accessibility are mandatory. Plan the existing
`ui-craft-gate.py` as an applicable source/token detector, plus WPF token checks,
native rendering and automation proof. A web/DOM checker that cannot inspect
WPF is **not** an accessibility pass; report unsupported coverage explicitly.
The class/UML notation review is also mandatory; no editable derived model.

### Operator questions and normal-path emissions

Reuse existing trace correlation and `aide.atlas.declarations` /
`atlas.declarations.observe` from R4. New event/span names below are proposals
subject to the existing observability contract review.

| Question | Emitting source and safe fields | Required observation |
|---|---|---|
| How long did extraction/projection/render take? | Existing declaration span plus proposed static projection/render spans; elapsed duration and fixed outcome | Read one success and one canceled/refused run |
| How many rows/edges were produced or omitted? | Proposed static projection event; requested/effective/returned counts, omission dimension/reason, provenance-class counts | Compare with decoded payload and visible bounds |
| How much memory/wire was charged? | Existing read budget plus proposed structural charge accounting; charged bytes, serialized body bytes, source-page bytes | Read charge/release with blocked writer and cancellation |
| Which profile/path ran? | Fixed `file-limited` profile category, source/class route, fixed phase | Never log source text, full path or symbol display strings |
| Did it fail and where? | Stable existing refusal codes plus reviewed structural error codes | Error, stale, budget and malformed-payload fixtures retain correct categories |
| How much model/network spend? | No model or remote call exists in this tranche | Verify absence of dispatch/egress, not an invented dollar estimate |

Measure on the normal path, not behind a debug flag. Avoid token/path identifiers
in metric labels. Use safe correlation only under the existing policy. Missing
measurement is “not recorded,” never a plausible zero. No performance target is
claimed achieved until the real pipeline emits and the reader verifies it.

## 11. Exact proposed implementation manifest and ownership seams

**This is a proposal for review, not permission to edit these files.** The first
tranche is one end-to-end file-local classifier/member path. No new project,
dependency, database, broad namespace sweep or blanket E1 grant.

| Proposed path | Scope and owner seam |
|---|---|
| `src\AiDe.Core\Understanding\CSharpDeclarationObservation.cs` | Core: produce classifier flavor and immediate parent evidence in the existing pass |
| `src\AiDe.Core\Understanding\AtlasManifest.cs` | Core: immutable declaration structural metadata; preserve occurrence and binding identity |
| `src\AiDe.Core\Understanding\AtlasQueryContracts.cs` | Core: carry metadata in the native outline/selection, not a second query service |
| `src\AiDe.Core\Understanding\AtlasQueryService.cs` | Core: carry observed metadata and limitations through existing selection/retention |
| `src\AiDe.Core\Understanding\AtlasReaderContracts.cs` | Shared public seam: proposed negotiated structural metadata; compatibility review required |
| `src\AiDe.Core\Understanding\AtlasReaderProjection.cs` | Core: bounded tokenized projection, validation and serialized-size accounting |
| `src\AiDe.Core\Understanding\AtlasReadScopeIssuer.cs` | Core: token issuance/parent validation, existing operation charge/lifetime; no new authority |
| `src\AiDe.App\Workbench\Understanding\AtlasStaticViewProjection.cs` **new, proposed** | Native owner: deterministic bounded card/member projection; no source reads |
| `src\AiDe.App\Workbench\Understanding\AtlasStaticView.cs` **new, proposed** | Native owner: read-only class/member controls and accessible list |
| `src\AiDe.App\Workbench\Understanding\AtlasReaderView.cs` | Native owner: explicit mode/selection/Back integration, reuse real owner and source actions |
| `tests\AiDe.Core.Tests\Understanding\AtlasStaticObservationTests.cs` **new, proposed** | Producer, identity/profile/containment/cycle boundary oracles |
| `tests\AiDe.Core.Tests\Understanding\AtlasStaticReaderContractTests.cs` **new, proposed** | Golden payload/provider compatibility, source authority, bounds and publication tests |
| `tests\AiDe.App.Tests\Workbench\Understanding\AtlasStaticViewTests.cs` **new, proposed** | Projection, supported states, selection/focus and accessible names |
| `tests\AiDe.App.Tests\Workbench\Understanding\AtlasStaticCompositionTests.cs` **new, proposed** | Real daemon/pipe/factory/window routes and cross-surface consistency |

Existing files in this table were encountered in source receipts. New test and
view paths are proposed names, **not a claim that those files already exist**.
Fixture mechanics and actual existing test helpers require source review before
construction; a mock that bypasses issuer/pipe/factory cannot clear composition.

**Read-only / no-change seams:** `AtlasIdentity.cs`, `AtlasIdentityCodec.cs`,
`AtlasSourceBinding.cs` retain their authority; `SurfaceContentFactory.cs` retains
CodeAtlas registration, Center/defaultCenter and owner composition. If the actual
event/factory contract demands a factory change, return that exact diff for Shell
admission rather than changing the manifest silently.

**Unresolved transport seam:** any additional isolated-pipe capability/codec
registration file is not yet in this source grant proposal. Core SourceReview
must return exact existing paths and compatible changes, or reject the optional
metadata approach. Do not invent a transport adapter based on naming intuition.

### Bounded first-tranche sequence and Inferred budget

1. **Source-review checkpoint:** establish complete current DTO/serializer,
   native selection and publication/charge contracts. Parent's four review leaves
   cover Core/security/concurrency, native UX/accessibility, UML/graph correctness,
   and test/composition/simplification. All relevant vetoes need named reviewers.
2. **Core red → green:** the Box/nested/partial/malformed fixtures produce bounded
   lexical metadata; preserve every E0 source and identity invariant.
3. **Wire red → green:** approved compatibility change carries metadata and honest
   bounds through the actual isolated pipe. Stop if an unlisted file is needed.
4. **Native red → green:** one classifier plus direct member page; both entry
   routes, exact source and Back; complete hard states.
5. **Integrated proof:** default viewport/1280 and additional supported sizes,
   keyboard/automation, late completion, stale source, blocked writer and telemetry.
   Parent publishes a new Proof Pack and decides acceptance.

Estimated implementation: **four build/verification checkpoints after source
review, ten production files and four new test files in the provisional manifest**.
These counts describe the proposal, not an approved effort estimate. A **2–4
focused engineering-day** envelope is **Inferred**, based on one producer/payload/
native vertical path and explicit integration fixtures, not measured repository
velocity. Unknown codec compatibility or native event seams can invalidate it.
No budget increase is automatic; a discovered additional seam returns to Owner.

## 12. Patterns, ladder and rejected alternatives

| Choice | Why it earns its place |
|---|---|
| Derived read projection | Reuse existing declaration facts; no second source of truth |
| Capability/lease boundary | Reuse established source authority and lifetime rather than mint UI authority |
| Immutable observation values | Preserve hash/profile provenance across bounded selection and Back |
| Presentation adapter | Map admitted static metadata to native controls without filesystem/semantic work |
| Progressive disclosure | One classifier/member page; no unbounded graph or background crawl |

Ladder result: need a concrete view → reuse observer/query/issuer/reader → use
existing Roslyn and .NET/WPF → minimum added metadata and native projection.
No unfamiliar dependency is adopted. Any future renderer, analyzer SDK or project
loader remains **Flagged until an executed spike and admission**, not implied by
this design.

Rejected: span/name-based parent guessing in App; a separate source reader or
Atlas identity store; project loading to make names look bound; an all-enterprise
force graph; image-generated diagrams; a new WebView/HTTP boundary; editing a
derived UML model; source-body capture to make Back always succeed.

## 13. Disconfirmation, remaining phases and gate record

Author disconfirmation found a real **design input gap**: the current public
outline has no parent/provenance/classifier-flavor metadata. A UI-only class graph
would therefore overclaim. This design requires producer-written evidence and a
reviewed wire extension instead. No implementation defect was fixed in this
documentation task. Relevant standing failure shapes are RIG-A (own-code guessing),
E2E-A (projection loss), E2E-C/D (green units, broken/inconsistent surface), DM-A
(two homes), and provenance laundering; controls are the explicit source-review,
wire and composition tests above, **not yet observed red or green**.

| Phase / obligation | Remaining status |
|---|---|
| E1 class/member first tranche | Proposed here; not implemented or accepted |
| E1 richer static semantics | Bound inheritance/interfaces, references, extended member kinds, project/TFM profiles and partial unification require later evidence/admission |
| **E1 method sequence** | **Still required in E1**; separate bounded method-interaction/call evidence, dispatch/async uncertainty and source navigation design |
| **E1 activity** | **Still required in E1**; separate control-flow/branch/loop/exception evidence and notation design |
| E2 | Domain/ER/layer/Azure later; no inferred conceptual elevation here |
| E3 | Implementation/spec correspondence later |
| E4 | Governed AI later; no AI capability or new egress authorized |
| E0 footer follow-up | Separate synthetic-VM initialization work, untouched |

### Definition-of-done disposition — no self-cleared gate

The design-slice checklist was read from
`.claude\skills\design-slice\reference\definition-of-done.md:9–28`.
The flow headings were consulted; the very large Stage-3 search line was elided,
so no claim is made that every stage-detail paragraph was read.

| Checklist obligation | Status |
|---|---|
| Responsibility, model/grain/history/additivity, change reach and phase | Specified; physical migration/append-only enforcement N/A because no new durable shape |
| Local conventions and every consumed contract | Partially established by R1–R16; **unmet** complete App event/factory, transport/codec and decoded-charge source review |
| Named patterns, ladder and both pattern/simplifier acceptance | Proposed; **unmet independent pattern/simplifier verdict** |
| Failure modes, STRIDE, LINDDUN dispositions | Written; **unmet independent Security/Privacy/Distributed Systems review** |
| UI design, tokens, native hard states, craft gate | Planned; **unmet rendered preview/token validation, accessibility/UML/graph review and measured viewport/performance proof** |
| DESIGN update and rendered design lint | Existing design language reused; no global token edits authorized; applicability/native coverage must be confirmed independently |
| Security/privacy rollups, index, inbound impact and audit | **Parent-owned and unmet in this leaf**, by explicit one-file scope |
| Testing Strategy union and falsifiable claims | Planned; **unmet Test Architect gate and all implementation red/green/composition evidence** |
| Telemetry | Specified emitting sources; **unmet observed normal-path measurements and safe-data checks** |
| Hard veto resolution | **Unmet; author cannot clear any independent veto** |
| Confidence/residual risk/status | Recorded here |

`GATE E1-static-design · 2026-09-14 · author submission only · exit criteria:
source-grounded proposal and explicit gaps recorded · verdict: REVIEW REQUIRED ·
vetoes: no independent acceptance claimed.`

The parent should not authorize implementation until SourceReviews establish the
unread load-bearing seams and the four funded leaves resolve their applicable
gates. An incompatible codec or inability to charge/retain metadata correctly
changes this proposal; it must not be patched around during implementation.

### Confidence and residual risk

**Verified:** the direct wire contract, observation pass, displayed signatures,
constants and source excerpts in R1–R16; the current public outline cannot express
the required evidence. **Inferred:** the proposed minimal extension, native layout
approach and effort envelope. **Flagged:** complete lifecycle/transport/native
contracts, decoded charge semantics, review verdicts and all E1 execution proof.
No enterprise, full-project, accessibility or multi-viewport success is asserted.

| Completed | Remaining | Best next action |
|---|---|---|
| One bounded class/member detailed design with source receipts, explicit nonclaims, manifest, negative oracles and E1 sequence/activity ledger | SourceReviews; independent gates; parent audit/index/rollups; separate implementation funding | Parent dispatches its four funded review leaves against this file and exact baseline before any source grant |
