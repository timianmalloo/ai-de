---
id: design-atlas-behavior-views
title: "Atlas E1 static behavior views — proposed design contract"
type: design
status: proposed
owner: "@timianmalloo"
tags: [atlas, e1, sequence, activity, static-analysis, native-ui]
links:
  - { to: mockup-uml-erm-surfaces, rel: relates-to }
  - { to: proof-atlas-behavior-contract, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Proposed method-level static behavior contract for one source-backed occurrence model with governing G6, shared logical envelope,
  bounded structural graph and page recomposition, hard states and numeric acceptance proposals.
---

# Atlas E1 static behavior views — proposed design contract

## 1. Decision status and delivery boundary

This document proposes the bounded E1 Sequence/Activity slice. It is design input, not accepted
policy, a source grant, an implementation result, or native proof.

**Verified source input.** `docs/specs/addendum-e-code-atlas.md` at
`92e025ae8cacd0a2fc8580f865a2172c4a9bdeb4` requires a supported **method** entry point; a
participant, predicate, source anchor, evidence kind and confidence on every sequence message;
branch, exception, cancellation and unknown-target gap nodes in Activity; the exact disclosure
`static reconstruction — not observed runtime order`; and explicit async/cancel/error content or
unresolved counts.

**Verified architecture input.** `docs/architecture/code-atlas-proposed.md` sections 8 and 13 at
the same pin require call-site occurrences rather than deduplicated type edges, dispatch
alternatives rather than invented targets, typed static control relations, explicit gap nodes and
one typed result shared by diagram, accessible list, export and inspector. It explicitly keeps the
existing `Interaction` projection labelled type-level.

**Verified current implementation.** At base
`c46e112a924a8a0a4c86e4552af1f0e30bfa851c`, `calls_at` facts and
`ProjectionService.Interaction` preserve repeated type-level call sites in source-position order.
`InteractionMessage` carries the call-site `Location`, while `InteractionResult` carries bounds,
truncation and source revision. `WorkbenchShell.ShowNodeInSequenceDiagramsAsync` reduces those
messages to `(From, To, Member)`, so the current `SequenceModel` and
`SequenceDiagramSurface` do not receive location, bounds, truncation or revision. The surface has
no evidence/confidence disclosure or Source/Back action, and lookup failure is swallowed. No
Activity diagram type or surface was found by the bounded source search. These are source
observations; no product code or runtime behavior was executed in this design pass.

**Reference-only foundation.** Checkpoint
`2b818f144980a0e2acecbaddc0795864a00673a3` supplies candidate Atlas E0 contracts for verified
source buffers, opaque declaration identities, method body spans, bounded source/outline pages,
reader capabilities, request sequencing and receipt-based Restore. This design depends on those
properties if the Owner/integrator admits that checkpoint. It does not accept the checkpoint or
claim those contracts are on this branch.

The terminal condition for this document is a reviewable contract and precise gaps. Product work
remains held until the foundation, shared seams, file grants and independent design gates are
recorded.

## 2. Goal, archetype and supported journey

The user is a developer or architect reading unfamiliar code. They arrive with a selected source
method and need to answer two different questions from the same evidence:

1. **Sequence:** which call expressions occur in this method, in source order, and what target did
   static binding establish?
2. **Activity:** which explicit source constructs branch, loop, await, throw, catch, return or leave
   a gap in this method?

The view is read-only. It does not execute source, simulate a trace, infer timing/frequency, or
expand into callee bodies.

**Archetype Signature — governing G6 Multi-Panel Data Terminal.** Owner supplement
`a766afa88cb4cbbd20c84dc383b213a4ab4e5e94` normalizes the Addendum E C1 signature:

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

This replaces this document's former G1 selection. The Owner's extension fields
avoid duplicate Type facets and a multi-choice Color; no upstream grammar repair
is claimed. Reading linked evidence is the job, not authoring a spatial model.

| Facets | Concrete realization |
|---|---|
| DSS / SPA / MultiPanelWorkstation | Existing Architecture host; selected method is master; diagram, list, inspector and source are linked details |
| UltraDense / typography / Flat | Dense aligned occurrence rows; code/identity uses existing technical type tokens; readable prose status; flat panels, no decorative cards |
| CommandPalette+Sidebar / DesktopBound | Existing file/member and palette entry; one Center reader inside the desktop shell, no new perspective |
| KeyboardFirst+PrecisionPointer | Same occurrence selection by keyboard or pointer; deterministic focus chain and Source/Back described in §7 |
| DarkAdaptive / System HighContrast | Existing Light/Dark theme choice plus Windows HighContrast; meaning has text/shape and never depends on color |
| LocalFirst / Session | Pinned local evidence; refresh is explicit; view history stores receipt plus mode/row/scroll/focus, never source bodies or independent graph truth |
| Instant+Confirmed | Immediate pending status; only a validated correlated response replaces current content |
| None / Freeform / HardCut | No animation; unrestricted reading; pivot cuts to the retained projection without changing focus or observation |
| WCAG / reduced motion / WPF | Native controls and UIA, full-label access, 100/150/200% DPI, linked list alternative; reduced-motion toggle changes no meaning |

Accepted G6 deviations: local evidence replaces streaming market data; sidebar/file
navigation accompanies commands; confirmed immutable results replace live quotes.
Selection is serial; reading synchronized details is parallel. Independent UX must
still validate round-trip fidelity to this family.

Direction: **grounded, ordered, inspectable**. Opposites: speculative, animated, decorative.
Reuse Visual Studio/VS Code member-to-source navigation, UML sequence notation and familiar
activity decisions. Reuse the existing Atlas Center reader, WPF resources and Source/Back model;
do not clone another product skin or make the diagram a second source authority.

Trigger union:

- UI-T1 applies because this is an expert technical view with precise spans and bounded counts.
  Stochastic/quantity and colormap clauses do not apply.
- UI-T4 applies: Windows native WPF, Windows keyboard/UI Automation conventions, theme/high
  contrast, DPI/windowing and real native proof are required.
- UI-T2 and UI-T3 do not apply. No generated imagery and no model-fronting behavior are admitted.
- Testing directives D0, D1, D2, D3, D4, D5-provider, D6 and D7-when-a-substitute-is-used apply
  to implementation. AI directives A1-A6 do not apply.

## 3. Domain model and invariants

### 3.1 Bounded context and language

The bounded context is **Atlas Static Behavior**. Its ubiquitous language is:

- **behavior selection** — one Core-issued method declaration occurrence in one manifest/source
  observation;
- **behavior occurrence** — one source construct within that selected method body;
- **call occurrence** — one invocation-like syntax occurrence, never a deduplicated relationship;
- **static target** — the member identity established by supported semantic binding;
- **target alternative** — a supported set of possible static targets when one target is not
  established;
- **gap** — a typed, source-anchored statement that the supported analysis cannot establish a
  target or control relation;
- **source ordinal** — a stable ordering of occurrences by the pinned source receipt, not execution
  order;
- **projection** — one bounded, immutable result from which both native presentations derive.

`BehaviorProjection` is the aggregate root. Its invariant is: every participant, occurrence,
control node, edge, bound and limitation belongs to the same selected method, manifest, source
observation and delivery context. A response that mixes observations is invalid; request sequence correlates delivery and is not content identity.

`BehaviorOccurrence` and `ActivityNode` are entities because their identity must survive selection,
Source and Back. `SourceAnchor`, `Evidence`, `Confidence`, `Bounds` and `Limitation` are value
objects. Participants are projection-local references to declarations or symbolic targets; they
do not become durable domain entities.

### 3.2 Grain, derivation and history

| Shape | Exact grain | Additivity/history rule |
|---|---|---|
| Sequence message | One invocation-like source occurrence inside the selected method | Counts add only within one projection; never across revisions. Repeated sites remain repeated rows. |
| Sequence participant | One distinct resolved declaration identity or one distinct symbolic/alternative target within the projection | Count is non-additive across views/revisions. |
| Activity node | One selected-method entry/exit, one supported source control/action occurrence, or one explicit gap | Count adds only within one projection. |
| Activity edge | One typed static relation between two activity-node occurrences | Count adds only within one projection; it is not observed traversal frequency. |
| Limitation | One distinct bounded reason code for this projection | Non-additive; an unknown total stays unknown. |

Window recomposition counts canonical identities once: auxiliary markers/edges repeated across
pages are not additive until deduplicated (§5.3). The projection is derived from verified source and compiler evidence. It is not persisted as graph
truth. If a view cache is later needed, it is a disposable cache keyed by the complete aggregate
identity and must have a rebuild-equality oracle against the same source observation. A later live
read is a different observation, not a rebuild of old history.

### 3.3 Identity and evidence rules

An occurrence key is derived by Core from the selected method observation, source observation,
syntax kind, exact UTF-16 span and syntax-child index path (§5.3). It is not derived from display text, target name, line number
alone or list position. This preserves repeated calls and overloads.

For a resolved overload, `TargetDeclarationToken` identifies the bound member and the display
label includes its source-oriented signature. For recursion, the target declaration token equals
the selected entry declaration token; the occurrence remains a distinct row. For dynamic,
late-bound, ambiguous, external or unsupported targets, the result carries no invented single
target. Supported alternatives may be listed as alternatives; otherwise a gap carries the reason.

Syntax evidence and semantic evidence remain separate:

- syntax may establish an invocation, branch text, `await`, `throw`, `catch`, `finally`, loop,
  `return` or an explicit cancellation call;
- semantic binding may establish the exact target member and overload;
- syntax order establishes source ordinal only;
- neither establishes runtime dispatch, execution order, frequency, duration, task scheduling,
  exception propagation through callees, or whether cancellation occurred.

Every visible row carries a call-site/control-site source anchor. A declaration anchor for a
resolved participant is additional evidence and never replaces the occurrence anchor.

## 4. Shared logical envelope — production bytes and DTOs are not frozen

One behavior producer returns one immutable projection. The former C# DTO sketches
are replaced by this logical contract under Owner shared ruling B. Names below are
design vocabulary, not accepted signatures, codec fields or a granted API.

| Boundary | Required fields and invariant |
|---|---|
| Request | Version and negotiated `static-behavior-v1` capability; authorized scope token; expected Core epoch; exact manifest; monotonically increasing request sequence within owner session; opaque selected method/observation context; bounded occurrence window `(offset, limit)` |
| Response | Correlated scope, epoch, manifest, sequence and selected subject; Core-issued projection observation token bound to the entire evidence set; version/capability; typed outcome; page bounds, observation coverage, continuation and disclosures |
| Row | Core-issued occurrence/node/relation identity; origin (`extracted-syntax`, `resolved-symbol`, `ambiguous-symbol`, `unavailable`), confidence (`extracted`, `ambiguous`, `unknown`), predicate and reason; exact observation-bound source-selection token or typed unavailable reason |
| Navigation | Core-issued receipt for the accepted restorable selection, or explicit unavailable reason; projection token never substitutes for a row source token |
| Page | Primary offset, returned count, known retained total; structural/edge counts and closure limit; page completion separately from observation coverage and continuation |
| Compatibility | No capability means no request and visible unavailable state. Unknown version/capability/enum, missing required field, invalid bounds/token or malformed shape yields typed refusal; v1 proposes rejecting unknown fields. The admitted foundation may choose another explicit policy only after golden-case review |
| Publication | Validate the whole envelope and every row before one atomic publish; no mixed manifest, epoch or observation. A refusal/error publishes no usable new selection or receipt |

Core owns authority, observation identity, continuation and Restore receipts. App
owns only transient presentation state and request sequencing. Epoch, manifest,
selection or session replacement cancels pending work. Publication compares the
current owner generation and request sequence even after successful cancellation;
late N cannot replace N+1. Cancellation before commit publishes no new snapshot.

An intentionally capped but internally coherent **published** observation is
restorable only if Core issued a receipt bound to that exact observation and page.
Back restores its partial coverage and omissions, not a newly computed complete
equivalent. In-flight canceled fragments have no receipt and are never restorable.
Expired/retired Restore preserves the refusal and offers explicit reselection;
the client never selects a guessed latest equivalent. Presentation mode, row token,
scroll anchor and focus target are re-applied only after successful Core Restore.

Golden cases: same-payload round trip; older/non-capable peer; missing/unknown field
and enum; hostile inert disclosure text; wrong scope/epoch/manifest; reordered N/N+1;
cancellation before publication; coherent partial/capped receipt; changed source;
expired Restore. Each must assert both returned state and absence of an unauthorized
new selection. Exact operation name, byte encoding, numeric field widths, source
selection method and Restore signatures remain P0.3/P0.4 at accepted-foundation C.
No second reader, pipe, source loader or client-minted navigation authority is added.

## 5. Supported subset and refusal grammar

### 5.1 Proposed algorithm: bounded structural source graph, not executable CFG

**Design decision, unimplemented:** build a deterministic structural source graph
from the selected method's Roslyn syntax and separately attach semantic target
evidence. Do not call this a control-flow graph or draw an edge as observed execution.
The spike `5d361f2a` observed syntax and symbols for six groups; it did not construct
this graph or prove all clauses below. P0.7 semantic acceptance remains open until
the bounded graph oracles run and UML/Test accept this representation for US-E6.

1. Validate Core-selected method and verified body/source observation. Proposed entry
   support is ordinary source methods, constructors and accessors with bodies; only
   ordinary methods were exercised in the current spike. Reject non-method selection.
2. Walk the selected body with an explicit bounded stack. Do not enter nested lambda
   or local-function bodies: emit a sourced `nested-body-not-expanded` gap for each
   declaration. Never expand a callee or execute/emit analyzed source.
3. Emit one **primary occurrence** for each invocation, object creation, if, switch,
   loop, return, throw, try, catch, finally, await and unsupported construct. Each
   construct has one role; recursion is a call attribute, not a second occurrence.
   Await and its nested call are distinct constructs with distinct keys and containment.
4. Sort primary occurrences by integer UTF-16 start ascending, end descending, then
   fixed role rank (control, await, invocation, creation, return, throw, gap). Any
   remaining tie uses syntax-child index path within the pinned body. Assign zero-based
   ordinal once over the observation, before paging or choosing a presentation.
5. Add method entry/exit and bounded structural region markers (block/branch/case/loop
   body/try/catch/finally). Attach every marker to its owning primary occurrence or
   method root. Markers reference the owner's source token and identify themselves as
   synthetic structure; they are not independently sourced statements.
6. Emit relations from the following closed table. A relation key is its observation,
   endpoint keys, kind and syntactic arm index. Edges carry their own predicate,
   confidence and evidence/source token; they cannot borrow a target declaration's
   authority. Order edges by endpoint ordinal, kind rank, arm index.

| Source case | Nodes and permitted structural relations | Prohibited inference / gap |
|---|---|---|
| Ordinary block and calls | `Contains` from region to directly contained facts; `NextInSource` between lexical siblings | A NextInSource edge is not an execution successor, including after return/throw |
| If/else | Decision plus explicit arm regions; `WhenTrueRegion`/`WhenFalseRegion` carry source condition; absent else has an empty false region | No claim either branch executes; short-circuit expression evaluation remains unresolved |
| Switch | Switch plus one region per source section/arm; `CaseRegion` carries literal pattern/guard text | No exhaustiveness, matching order or implicit fallthrough claim; unsupported pattern semantics yield gap |
| For/foreach/while/do | Loop plus body region and explicit condition/header facts; `LoopBodyRegion`, `LoopConditionSource` | No synthetic execution back-edge, loop count or implicit enumerator/disposal; unknown runtime behavior remains disclosed |
| Return/throw | Explicit sourced node; `EnclosedBy` identifies region; lexical next may still exist labelled source order | No return-to-caller or catch-target edge; propagation is unknown |
| Try/catch/finally | One region each; `HandlerDeclaration` and `FinallyDeclaration` connect the syntactically associated regions | Do not connect an arbitrary call/throw to a handler; filters and propagation are unresolved |
| Await | Await node contains operand facts; `AwaitOperand`; `ContinuationSource` points to next lexical sibling if present | ContinuationSource means subsequent source, not guaranteed resumption, scheduler or thread |
| Explicit cancellation | Exact bound CancellationToken.ThrowIfCancellationRequested call gains `cancellation-check` predicate and gap for runtime outcome; explicit bound OperationCanceledException creation/throw gains `cancellation-exception` evidence | Names, parameters, signatures or await alone cannot create cancellation evidence; no delivered-cancellation edge |
| Dynamic/ambiguous/external target | Preserve invocation; attach resolved external symbol as external evidence without fabricated local declaration token; candidate alternatives are bounded and labelled candidates, not exhaustive dispatch | No invented implementation. Unknown target is a target gap, not an omitted call |
| Goto/yield/using/lock/unsupported syntax | One sourced gap with kind/reason, retain enclosing supported structure | Never silently lower, flatten or omit implicit behavior |
| Malformed syntax | Sourced diagnostic gap and affected recovered constructs tagged uncertain | No complete semantic coverage; invalid nodes never establish a certain target |

Empty source regions are explicit structural markers. There is no synthetic merge
or executable exit edge: a method Exit marker is a source boundary, not proof that
all paths terminate there. If UML/Test require executable successor semantics, that
is an Owner decision requiring a separate Roslyn CFG contract spike; syntax facts
must not be relabelled to pass it. This is not an automatic scope downgrade.

Traversal classification is closed for control statements: block and empty statement
are structural only; the supported control cases above emit their primary facts;
any remaining statement/control form emits an unsupported gap. Ordinary expression
subnodes (identifiers, literals, member access and arithmetic) are traversed for
invocations/creations but do not independently imply control edges. Conditional and
short-circuit expressions emit a `expression-control-not-expanded` gap while keeping
their nested call occurrences. No gap can be removed merely because calls inside it
bound successfully. An expression statement without a supported fact is an anchored
`unmodelled-action` gap; the proposed fixture must avoid accidental extra facts and
assert its population before it is accepted for timing.

### 5.2 Projection state and coverage grammar

State is a product of **request outcome**, **published observation coverage** and
**page completion**. Keep these axes separate in the envelope, footer and UIA:

- Outcome: pending / published / refused / canceled / error.
- Coverage: fully enumerated / semantic gaps / capped-prefix / malformed / unknown.
- Page: complete window / shortened by page cap; continuation is independently present
  only when more retained primary rows are known in this same observation.

`ReadyComplete` means fully enumerated supported source structure, not complete
runtime knowledge. `ReadyPartial` carries semantic gaps, malformed source or a capped
prefix. One complete page may contain unknown dispatch and have a next page.
`EmptySupported` means zero primary facts after successful enumeration, not runtime
inactivity. Pending/canceled/error states may show an **old published observation**
under an explicit retained/previous label; they never render unfinished new fragments.
Changed, Unavailable, Unverifiable, UnsupportedEncoding, TooLargeToVerify,
ReadUnstable, Refused and Canceled source states cannot be upgraded by the client.

### 5.3 Stable identity, window closure and recomposition

**Observation identity:** Core binds scope, epoch, manifest, selected declaration,
source hash/observation, producer algorithm version and semantic-reference context.
Request sequence correlates delivery; it is not part of stable content identity.
An occurrence key is observation identity + syntax kind/role + exact span + syntax
child index path. It does not include page offset, mode, display text or target name.
Structural marker keys add owner occurrence + region role/arm index. Entry/exit key
from selected method plus boundary role. IDs survive page/pivot changes within the
same observation; no cross-revision stability is promised.

**Primary window:** offset/limit count all primary occurrences, including controls
and gaps, not just calls. Sequence displays the call/creation subset with sparse
source ordinals; Activity displays the selected primary facts. Their visible row
counts need not match; their occurrence identities and shared window must. A page
with controls but no calls says `No call sites in this window`, not empty method.

For half-open window [a,b), include primary rows in that range, method boundaries,
and the transitive owning-region chain needed to interpret those rows. Do not include
unselected sibling contents. Auxiliary markers consume the structural cap, not the
primary limit. Relations with both endpoints included render normally. A relation
incident to an included node whose other endpoint is outside closure renders once
with a **boundary stub** containing the canonical missing endpoint ID, direction,
relation kind and reason `outside-window`; if the primary ordinal is known, include
it and enable Go to window. Never make the stub look like an unknown semantic target.
Do not emit stubs for relations with neither endpoint included.

A missing endpoint due to observation truncation is `not-observed-after-cap`, with
unknown ordinal/total; it has no Go to window. Missing semantic target is
`unresolved-target`, even on the final complete page. Deduplicate structural markers,
stubs and relations by canonical identity, not label. Report primary, auxiliary,
boundary-stub and edge counts separately in bytes and UI.

**Page publication:** evaluate primary, closure, edge and encoded-byte limits together.
If a requested window exceeds a cap, shrink its end before publication and report
requested/returned counts and the first firing reason. Never drop a relation silently.
If one primary plus mandatory closure cannot fit, return typed `window-unrepresentable`
with no new selection. The continuation is Core-issued and binds observation, query policy,
next ordinal and algorithm version; client offset arithmetic is not authority.

**Observation truncation:** retain a coherent enumerated prefix only at a completed
primary boundary, with reason and known retained count. Unknown total stays unknown.
Continuation pages exhaust that retained prefix only; they do not resume an abandoned
parser or pretend omitted source was observed. Refresh with a changed policy requires
a new observation. User cancellation before atomic publication returns no new prefix.

**Recomposition oracle:** on an uncapped fixture, union every page from the same
observation, replace matching stubs with their canonical endpoints, deduplicate only
canonical auxiliary/edge IDs, and compare all primary IDs, predicates, anchors,
confidence, relations and order to the one-window projection. Test page sizes 1, 2,
7 and 128; cut before/inside/after if arms, loop body, try/catch/finally and await.
Changing only page size must not change any identity. A capped prefix recomposes to
that prefix plus its explicit observation gap, never to the uncapped result.

## 6. End-to-end change surfaces and grant requests

| Surface | Reuse | Proposed addition or precise gap | Grant request |
|---|---|---|---|
| Verified source and identity | E0 `AtlasSourceBinding`, verified buffers, `AtlasIdentity`, declaration observations at checkpoint `2b818…` | Foundation acceptance is pending | Owner/integrator decides the base; Core retains authority |
| Producer | Existing Roslyn compilation and semantic-binding patterns; existing call-census tests are useful fixtures | `src/AiDe.Core/Understanding/CSharpBehaviorObservation.cs` and `AtlasBehaviorContracts.cs` proposed | Core grant required |
| Projection/query | E0 request sequencing, bounds, limitations and one reader lease | Minimal extension to `AtlasQueryContracts.cs`/`AtlasQueryService.cs` proposed | Coordinated Core seam, not this lane's owned write |
| Wire | Existing framed/versioned reader, strict codec, feature negotiation and byte accounting | Minimal extension to `AtlasReaderContracts.cs`, `AtlasReaderProjection.cs` and remote/server operation registration proposed | Core/integrator grant; legacy compatibility gate |
| Client type | Reuse `IAtlasReaderQueries` and `AtlasWorkspaceOwner` generation/cancellation | `SelectBehaviorAsync` and stale-result rejection by echoed `RequestSequence` | Shared seam; no second client |
| Native UI | Reuse WPF resources, Atlas Center selection/source and receipt-based Restore | New `src/AiDe.App/Workbench/Understanding/AtlasBehaviorView.cs`; one shared view model with Sequence/Activity pivot and accessible lists | Native E1 grant after design review |
| Composition | Reuse accepted Atlas owner/factory location | Minimal owner/factory registration only after exact host decision | Owner/integrator; no Shell assumption |
| Source and Back | Reuse the selected receipt, source page and Restore behavior | Diagram row invokes existing source selection with occurrence span; Back restores method/view/mode/row/focus | Native/integrator seam |
| Compute reader | Sequence canvas, Activity canvas, both textual alternatives, inspector and export consume the same projection object | Equality oracle required across all readers | Native lane plus Test gate |

The existing `src/AiDe.Core/Projections/Interaction.cs`, `IWorkspaceQueries.InteractionAsync`,
`SequenceModel` and `SequenceDiagramSurface` stay unchanged during this slice unless the integrator
separately authorizes migration/removal. They remain a type-level legacy surface and are not
renamed or represented as method behavior.

Exact proposed production manifest:

- new `src/AiDe.Core/Understanding/AtlasBehaviorContracts.cs`
- new `src/AiDe.Core/Understanding/CSharpBehaviorObservation.cs`
- coordinated extension `src/AiDe.Core/Understanding/AtlasQueryContracts.cs`
- coordinated extension `src/AiDe.Core/Understanding/AtlasQueryService.cs`
- coordinated extension `src/AiDe.Core/Understanding/AtlasReaderContracts.cs`
- coordinated extension `src/AiDe.Core/Understanding/AtlasReaderProjection.cs`
- coordinated extension to the accepted Atlas remote/server operation registration paths, whose
  final names must be taken from the admitted foundation rather than guessed from this base
- new `src/AiDe.App/Workbench/Understanding/AtlasBehaviorView.cs`
- coordinated minimal extension `src/AiDe.App/Workbench/Understanding/AtlasWorkspaceOwner.cs`
- coordinated minimal factory/host registration path selected by the integrator

The last two unnamed paths are deliberate seam requests. This branch cannot name them as accepted
files while the foundation transfer is pending. The implementation grant must resolve them before
authoring; discovering them mid-edit is a stop condition.

## 7. Native interaction and presentation contract

The native view has one title and status strip, a Sequence/Activity segmented pivot, a diagram
pane, a synchronized accessible list and an evidence inspector. Selection is shared. Switching
the pivot does not re-query Core.

Every sequence message exposes: source ordinal, caller participant, static target or explicit
unknown/alternatives, predicate, source anchor, evidence kind and confidence. Every activity node
exposes: kind, static condition where present, source anchor, evidence kind and confidence. Visual
treatment never relies on color alone: extracted rows use solid line plus text `extracted`;
ambiguous rows use dashed line plus `ambiguous`; unknown gaps use a gap shape plus `unknown` and a
reason.

The exact disclosure is persistent above both diagrams:
`static reconstruction — not observed runtime order`.
No label says trace, execution, duration, frequency, happened, returned from, or runtime unless a
future separately admitted observed-trace source exists.

Source opens the exact call/control-site span through the existing Atlas source authority. Back
restores the selected method, Sequence/Activity mode, row, scroll and keyboard focus only after the
receipt is valid. If source bytes changed or are unavailable, the old anchor stays tied to the old
observation, highlighting is disabled, and the view offers refresh/reselection.

Keyboard order: status/disclosure → pivot → diagram/list mode → rows in source ordinal → inspector
→ Source → Back. Arrow keys may move between rows; Enter opens Source; Escape returns from the
inspector without losing row selection. Canvas-only hit targets are insufficient. Each diagram has
a full textual alternative with the same row count, order, conditions, evidence and omissions.
Automation names include view kind, selected method, completion state and exact shown/omitted
status. Full labels are available through AutomationProperties even when visually trimmed.

Native states cover the complete grammar in section 5.2 plus default, hover, focus, active,
disabled and overflow states for every interactive control. Theme, high contrast, 100–200% DPI,
small supported window, long qualified names, 128 participants/occurrences, reduced motion and
screen-reader reading order require proof. Motion is unnecessary; reduced motion therefore
changes no meaning.

### 7.1 Hard-state, linked-reader and navigation oracles

All below are **proposed mock and native tests**, not observations. UI design mode
is review/elevate of the existing Atlas reading pattern; no new HTML or native
surface is authored here. The existing WPF keys opened at the baseline include
`SurfaceBrush`, `SurfaceRaisedBrush`, `SurfaceSunkenBrush`, `TextBrush`,
`TextMutedBrush`, `DisabledTextBrush`, `BorderBrush`, `BorderStrongBrush`,
`AccentBrush`, `AccentContrastBrush`, `FocusBrush`, `VerifiedBrush`, `InferredBrush`,
`UnverifiedBrush` and `DangerBrush`. Consume dynamic resources; selected text over
AccentBrush uses AccentContrastBrush. HighContrast uses the accepted shell mapping;
no new palette. Typography/spacing keys must be mapped from the admitted host rather
than invented. Each interactive control has visible default/hover/focus/pressed,
disabled and overflow treatment; keyboard focus never relies on hover or color.

The permanent disclosure remains visible and in the accessibility tree in every
content-bearing state. `Previous observation` denotes retained old content. Source
and Back are enabled only for independently valid row tokens and Core receipts.
Focus rules below name real semantic targets, not unstable visual indices.

| State / trigger | Visible content and enabled actions | Forbidden / selection / recovery / focus |
|---|---|---|
| Unselected: no supported method | `Select a method to inspect its source structure.`; existing file/member selection | No generated diagram or error diagnosis; clear behavior row; focus method selector |
| Loading: accepted local selection starts N | `Reading selected method…`; identity; Cancel; prior view labelled `Previous observation` if present | No empty-success or new usable receipt; preserve old selection separately; cancel/retry; focus stays invoking control, status uses polite UIA announcement |
| ReadyComplete: validated fully enumerated result | Shared window counts, exact disclosure; pivot/filter/rows/Source/Back as authorized | No runtime completeness; selected row persists by token; initial focus first primary row or title if none |
| EmptySupported: successful zero facts | `No supported source constructs were found in this method.`; method Source and Back | No claim method does nothing; clear row/inspector; focus empty-state heading; choose another method |
| No matches: local filter hides all current rows | `No matches in this window.` plus retained window count; Clear filter, Next if available | No empty-method/global-search claim; keep hidden selected token as hidden, no stale visible inspector; focus filter; clearing restores token if present |
| Partial: semantic gaps or capped prefix | `Partial source structure` plus exact known counts or `Total not recorded: <reason>`; rows, valid Source, next retained page, explicit refresh | No complete badge or fabricated denominator; retain selected row; cap reason shown in diagram/list/inspector; focus stays selection |
| Boundary page: selected relation endpoint outside window | Label `Outside this window`, direction/kind, known target ordinal; Go to window when continuation authority supports it | No unknown-target glyph substitution; preserve relation selection; focus loaded target row after correlated response, else boundary row on refusal |
| Ambiguous/unknown dispatch | `Target unresolved` or `Candidate targets — not exhaustive dispatch`; call anchor and evidence list | No certain implementation participant; Source points to call site; select same call in both modes and focus its list row |
| Async/cancel/exception facts | `Await — continuation in source only`, `Cancellation check — outcome unknown`, `Throw — propagation unknown`; source actions | No scheduler, delivered cancel or handler-flow inference; inspector carries same predicates/anchors; focus selected fact |
| Malformed recovery | `Source has parse errors; structure is partial.`; diagnostic gap rows with stable codes; Source/refresh where allowed | No certain semantic coverage or silently skipped invalid node; selected valid/gap row retained; focus first diagnostic when user invokes Show issues |
| Unsupported entry or capability | `This method form is not supported.` or `This reader does not support behavior views.`; existing source selection/Back | No fallback Interaction masquerading as method view; no behavior rows; focus reason then supported Source/Back action |
| Stale/source-changed | `Source changed. This view uses the previous observation.`; old pinned rows, Refresh/reselect | No old highlight on new bytes; keep old row identity; Source refused when binding invalid; focus refresh result title, restore row only if identity still valid |
| Canceled before publication | `Reading canceled.`; previous published view labelled old if any; Retry/Back | Never publish new fragments or new receipt; prior selection only; focus Retry when cancel finishes; explicit retry creates new N |
| Error/refused | `Could not read source structure. <stable code>`; identity, previous labelled content if available; Retry/reselect/Back by reason | No swallowed error, fabricated selection or executable diagnostic text; preserve prior receipt only if valid; focus error heading then recovery action |
| Overflow/layout cap/long label | `Diagram limit reached. Use the accessible list.` or trimmed label with full text in inspector/UIA; list and valid Source/Back | No hidden fact loss or unreadable fixed-size viewport; preserve canonical selected row; focus equivalent list row; window/page reduction is explicit |
| Restore expired/retired | `This saved selection is no longer available.`; explicit reselection, current valid content remains labelled | Never choose latest equivalent automatically; no false focus restore; focus refusal heading, then reselection |

Local filter applies only to the already accepted window, does not mutate observation
or canonical order, and states its scope. Clearing/switching the pivot never queries
Core. Going to another window is a new sequenced request bound to the same observation;
selection is preserved only when its token is present, otherwise the status names
the prior row as outside this window and the inspector clears. Enter/Space activates
the focused Source action; Alt+Left uses the host's Back command if available after
foundation mapping. Escape closes inspector detail and returns to its owning row.

**Graph/list/inspector equality:** for each view and window compare canonical primary
IDs, sparse source order, predicates, evidence origin/confidence and row source tokens.
Activity relation list additionally equals the diagram's relation IDs, endpoints,
kinds and boundary reasons; selecting an edge shows its own receipt, not its target's.
Sequence's subset must equal precisely the invocation/creation facts in Activity's
shared primary window, not every Activity row. Structural markers and stubs have
explicit separate counts and synthetic-owner source attribution. Any export later
admitted consumes these same collections and limits.

**Source/Back receipt oracle:** select a repeated or overloaded call at a non-first
page and nonzero scroll position; save mode, occurrence ID, row/list focus, source
token, observation and Core receipt. Open Source through the real authority; inspect
the exact hash-bound UTF-16 span. Back must restore all saved values after validating
the receipt. Mutate only source bytes, then only epoch, then retire the receipt:
each run must refuse old highlighting/Restore and never restore a guessed row.
Run for Sequence call, Activity condition, gap and boundary relation; Source may be
disabled for an unavailable synthetic relation and its reason must be announced.

**Native falsifiers:** keyboard-only reach every state/action; inspect UIA names,
roles, selected state, full labels and source/gap reason; perturb one inspector
receipt or hide one relation from the list and equality must fail. Test Light/Dark,
Windows HighContrast, 100/150/200% DPI, supported small window and maximum labels.
Check no clipping hides disclosure, omissions or recovery actions; every pointer
action has a keyboard alternative. Owner-generation N/N+1 and cancellation races
must leave focus on N+1. Screen-reader order follows logical source/region order.
Real WPF composition under a serialized desktop slot is required; fixture HTML,
UIA metadata alone and headless view-model tests cannot clear native behavior.

## 8. Mockup disposition

`docs/mockups/uml-erm-surfaces.md/.html` is reusable for the permanent derived/read-only rule,
explicit evidence legend, theme/high-contrast/reduced-motion controls, generation-error and
too-large/curated states. It mentions Sequence in the catalog but does not render a behavior view,
bind a method identity, provide Source/Back, model behavior unknowns or prove a native path. Its
HTML and screenshots are direction evidence only.

The read-only proposal `docs/proposals/code-atlas/mockup.html` at
`1065a851e4039fc80804ca92f2730d9f23e6adfc` is reusable as interaction direction: one source-backed
selection, clickable sequence messages, a source anchor on drawn messages, a Sequence/Activity
pivot, static/non-runtime wording and Back restoring view state. It is a curated browser fixture,
not a producer or wire contract. Its Activity flow is hand-authored, its Sequence evidence does not
carry the proposed machine-readable kind/confidence on every row, and it does not establish
pagination, exact omission semantics, overload identity, unknown dispatch, async/cancel/error
coverage, native accessibility or runtime performance. No private source body, captured fixture or
screenshot from the proposal may enter product code or tests.

The exact requested path is `docs/mockups/atlas-behavior-views.html`, under Core
request `req-01M2KCTTG4ZWAC2K01PB1HYWGK`. This design is its graph hub. No HTML is
authored in this revision. After grant, the dependency-free synthetic harness must
exercise §7.1; selectors are persona (developer/architect/keyboard reader), viewport
(1024×768, 1280×800, 1920×1080), Sequence/Activity, state, Light/Dark/HighContrast,
capability (supported/older peer), and reduced motion. Use inert synthetic labels,
including 512-unit labels, nested branches and overflow. Record structure-first
rubric location/dimension/severity/evidence/fix/confidence; run the nonempty craft
detector and token/contrast checks. No HTML result clears native acceptance.

## 9. Failure, security and privacy analysis

| Category | Failure or threat | Disposition |
|---|---|---|
| Input | Entry token names a file/type rather than supported method; span outside verified text | Reject with stable code; retain selection and Source where allowed |
| Dependency | Foundation lacks accepted method body/identity or behavior capability | Disable behavior view with reason; do not fall back to type-level `Interaction` |
| State | Response belongs to old generation/request sequence | Detect before publication; discard and retain current view |
| Concurrency | Selection changes while query/restore is in flight | Owner cancellation plus echoed sequence; late completion cannot overwrite newer selection |
| Resource | occurrence/participant/byte limit fires | Return bounded retained rows plus exact known omissions or unknown reason; offer next page/search |
| Time | Query stalls or cancellation arrives | Bound by existing operation policy; return canceled/typed error, never blank current view |
| Meaning | Source ordinal is read as runtime order | Persistent exact disclosure, field name `SourceOrdinal`, forbidden-copy oracle |
| Meaning | target name collapses overloads or dynamic dispatch | Opaque member identity; alternatives/gap; never display-name identity |
| Meaning | async/cancel/error is inferred from names or signatures | Only explicit supported syntax/semantic predicates; otherwise unresolved limitation |
| Presentation | canvas and accessible list diverge | Both consume one immutable projection; row-for-row equality oracle |

STRIDE-lite boundaries:

- **Spoofing/tampering:** App-supplied paths/text or forged tokens cannot select behavior. The
  reader revalidates scope, epoch, manifest, file and method tokens against Core authority.
- **Repudiation:** normal-path telemetry records operation/result codes and counts, never source
  bodies. Request sequence and observation token make a stale publication diagnosable.
- **Disclosure:** payload contains minimal labels, opaque tokens, spans, conditions and limitations;
  no whole source body is added to behavior projection or telemetry.
- **Denial of service:** occurrence, participant, depth, byte and time bounds fail closed with
  visible omissions. A cap firing is partial evidence, not success.
- **Elevation:** repository text, condition labels and target labels render as inert text. They do
  not create commands, URIs or tool authority.

LINDDUN-lite: repository source and paths are work data. Linkability and disclosure are minimized
through opaque tokens and spans; telemetry excludes source text, display labels and absolute
paths. This slice adds no durable history, identity account, model egress or publication path.
Existing source retention/deletion rights and workspace close/release behavior remain authoritative.
Security and Privacy reviewers must confirm these claims against the accepted wire before product
admission.

## 10. Red-first oracle and reviewer contract

No test in this table was written or run in this design pass. Each implementation control must be
observed red against the old/mutated path before green through the real seam.

| Proposed control | Failing input / required oracle | Trigger |
|---|---|---|
| `BehaviorSelectionRequiresMethodObservation` | File/type token or out-of-span body is accepted | D1/D2/D4 |
| `RepeatedRecursiveCallsRemainOccurrences` | `A→B, A→A, A→B` collapses, reorders or loses recursion | D1/D4 |
| `OverloadTargetsKeepOpaqueMemberIdentity` | Same display name/signature family maps to one target | D1/D2 |
| `AmbiguousDispatchNeverInventsOneTarget` | dynamic/interface ambiguity renders one certain participant | D1/D2 |
| `SourceOrdinalNeverClaimsRuntimeOrder` | UI/export omits exact disclosure or uses runtime/timing copy | D1/D6/native |
| `ExplicitAwaitHasStaticContinuationOnly` | await implies thread/scheduler/runtime ordering | D1/D2 |
| `CancellationNeedsExplicitSupportedEvidence` | parameter name or cancellable signature creates a cancel edge | D1/D2 |
| `ExceptionAndUnsupportedControlBecomeNodesOrCounts` | throw/catch/goto/yield disappears while result says complete | D1/D2 |
| `BehaviorBoundsAreOneSharedWindow` | Sequence and Activity describe different occurrence pages or hide omitted rows | D1/D6 |
| `BehaviorWireCarriesEveryReceiptField` | delete anchor/evidence/confidence/bounds/completion/request sequence at a codec hop | D5-provider/D6 |
| `LegacyPeerOmitsStaticBehavior` | unadvertised peer receives the operation or strict decoder fails on an optional new field | D5-provider/D6 |
| `LateBehaviorResponseCannotReplaceNewSelection` | delayed request N publishes after N+1 | D1/D3/D5 |
| `SourceAndBackKeepObservationAndFocus` | changed bytes receive old highlight, or Back loses method/mode/row/focus | D3/D4/native |
| `DiagramListInspectorAndExportAgree` | any consumer changes count/order/condition/evidence/omission | D1/D6/native |
| `NativeBehaviorStatesAreReachable` | unselected/loading/partial/empty/unsupported/stale/canceled/error/overflow absent | D0/native |
| `BehaviorTelemetrySaysNotRecorded` | missing duration/count becomes zero or a plausible value; source text leaks | D1/SRE |

The real implementation fixture union includes repeated calls, direct recursion, overloads,
unknown and ambiguous targets, explicit branches/loops/throw/catch/finally/await/cancellation,
unsupported control, malformed/recovery syntax, source mutation, cancellation, stale completion,
limit and byte caps. Synthetic fixtures are safe supplements; the end-to-end journey must also use
an authorized real workspace through actual Core, framed wire, owner and native window.

| Reviewer | Trigger and hard question | Exit predicate |
|---|---|---|
| Test Architect — hard veto | Can every supported/unsupported semantic claim be falsified through the real seam? | Trigger union mapped; red receipts and mutation-resistant oracles accepted |
| UML/graph | Do Sequence and Activity notations represent static occurrences and control relations honestly? | Repeat/recursion/alternatives/gaps/disclosure/list agreement accepted |
| UX Researcher/IA — UX veto | Can a user move method → behavior row → exact source → Back without losing context? | Happy, partial, error and recovery flows plus buried-action check accepted |
| UX & Accessibility — hard veto | Is every canvas fact available and operable without color, pointer or vision? | Keyboard, UIA, contrast, textual alternative, DPI/theme/state proof accepted |
| Core Security/Concurrency/Codec — hard veto | Can forged/stale/oversized input cross authority or publish late? | Admission, strict compatibility, bounds, cancellation and publication ownership proven |
| Data & Persistence | Is projection identity revision-bound and derived rather than competing durable truth? | Aggregate key/rebuild rule/no-new-storage accepted |
| SRE | Can operators measure latency, volume, path, caps and failure without source leakage? | Normal-path emissions and safe-data checks observed |
| Native C# | Does one immutable projection feed both modes without UI-thread analysis? | Threading/lifetime/composition and long-content behavior accepted |
| Simplifier | Can any new type, copy or host edit be removed while keeping all obligations? | One producer/query/projection; no second source authority or duplicate view model |
| Release/integrator | Are foundation, exact grants and serialized native proof settled? | Pinned base, manifest, baseline and handoff recorded |

The author cannot clear these gates.

## 11. Numeric caps and performance admission

All numbers below are **proposed limits**, not measured capacity or admitted wire
limits. Retain the smaller applicable foundation limit after mapping; incompatibility
returns to Owner rather than silently changing this contract. Caps bind before row,
edge, layout or payload publication; input byte checks precede parsing.

| Quantity | Proposed maximum (inclusive) | Rationale and boundary oracle |
|---|---|---|
| Encoded request | 16 KiB UTF-8, opaque token 2,048 bytes each | Finite authority/window payload; max+1 refuses before decode allocation |
| One encoded response | 512 KiB UTF-8 including envelope/text/stubs | Bounds transport and publication; max+1 shortens window or refuses indivisible closure |
| Verified source supplied to parser | 1 MiB UTF-8; 524,288 UTF-16 code units | Bound Roslyn input, both measures checked; either max+1 refuses |
| Requested primary rows/page | 1..128; offset 0..1,023 | Same window across modes; zero/negative/129 refuse; offset beyond retained total gives typed exhausted page |
| Retained primary rows/observation | 1,024 | Bound total derived work; next row creates capped-prefix gap, total unknown unless independently counted |
| Auxiliary nodes incl. boundaries/stubs | 512/page; 2,048/observation | Primary count cannot hide graph growth; excess shortens page or caps observation |
| Relations | 2,048/page; 8,192/observation | Bound fan-out separately; never silently drop edges |
| Participants / target candidates | 128/page; 256/observation; 8 candidates/call | Excess becomes explicit candidate truncation/unknown completeness; participant closure can shorten page |
| Labels / conditions / reasons | 512 / 1,024 / 256 UTF-16 units each | Inert displayed summaries; truncation marker and original source receipt retained; test surrogate-pair-safe trimming |
| Diagnostic records | 64/observation; one aggregate overflow reason | Prevent diagnostic flooding; known count if available else unknown |
| Syntax traversal depth / nodes visited | 64 levels / 16,384 nodes | Explicit stack and visit charge; depth/node excess yields capped gap before child expansion |
| Layout input and work | 640 nodes (128+512), 2,048 edges; one linear placement and one linear routing pass, no force-directed iterations | Maximum 640 placements + 2,048 routes; exceeding charge publishes list fallback with layout-limit copy |
| Per-route points / retained geometry | 6 points/edge, 12,288 points/page | Bound auxiliary allocation; excessive route becomes straight labelled relation in accessible list, never vanished evidence |
| Analysis deadline / cancellation polling | 1,000 ms per query; observe cancellation at least every 64 visited nodes and before publication | Deadline produces typed incomplete/error, not successful work; cancellation mutation must prevent publication |

Parser cancellation, Roslyn allocation and deadline behavior were not measured by the
existing spike. Source input bytes are bounded before Roslyn; syntax depth is checked
after parsing and does **not** claim a hard bound on Roslyn's own allocation. A parser
stress/cancellation probe is a required P0.6 admission input; if the underlying API
cannot honor the proposed budget, return to Owner for a bounded alternative.

**Latency thresholds:** initial open ≤2,000 ms p95; filter and retained pivot each
≤150 ms p95 (Addendum E A10). Owner candidate: no continuous Atlas-owned UI-thread
work segment >16 ms. The latter is a review target, not an OS-frame guarantee.
Perform extraction/layout off the dispatcher; UI work is bounded publication and
virtualized visible-row updates. UI failure of this target returns to design.

**Fixture proposal E1-perf-v1:** one synthetic authorized workspace containing a
method with exactly 128 primary facts (96 calls, 8 ifs, 4 loops, 4 awaits, 4 returns,
4 throws, 2 try regions, 2 catches, 2 finally regions, 2 explicit gaps), 32 participant
identities, maximum nesting 8 and 20 labels at 512 UTF-16 units. Repeated, recursive,
overload, dynamic and cancellation cases are assigned within the 96 calls. Fixture
builder must assert these counts before timing; raw source/hash is frozen by Test
and Owner before execution. This is proposed fixture content, not a file or an
authorization to execute analyzed source. Companion cases hit every cap and cap+1.

Protocol: 20 cold-view samples with fresh view and projection cache, application
startup excluded; separately 100 retained samples for filter and 100 for pivot.
Nearest-rank p95 is sorted sample 19 of 20, or 95 of 100. Keep every raw sample,
failures and canceled runs; report failure/cancellation separately and do not replace
them with faster retries. Record hardware/CPU/RAM, OS/runtime/build, fixture hash and
population, viewport/DPI, cache policy and instrumentation version before running.
Those machine values are **not recorded yet**, not guessed from this session.
Use 1280×800 at 100% DPI for the primary timing cohort; report separate diagnostic
cohorts at 150/200% and HighContrast without pooling percentiles.

Open measures user action → correct content rendered, UIA populated and keyboard
interaction available. Filter/pivot measure action → all linked details coherent
and interactive. Proposed normal-path trace-correlated spans:
`aide.atlas.behavior.observe`, `.query`, `.layout`, `.publish`, `.render`,
`.interaction`; source/hash-safe counters record bytes, primary/aux/edge/stub counts,
cap/deadline reason, completion/coverage, cache path and cancellation/error outcome.
Dispatcher instrumentation records every Atlas-owned callback start/end, elapsed
monotonic duration and yield boundaries. A synthetic injected 17 ms callback must
fail the 16 ms oracle; missing samples report `not recorded`, never zero.
Count equality uses an operation correlation identifier, not logged source or tokens.
Telemetry excludes source/condition text, member labels, absolute paths and raw tokens.
No model, spend or external network path exists; their cost axes are N/A.

## 12. Confidence ledger, disconfirmation and status

| Claim | Evidence | Falsifier tried in this design pass | Label |
|---|---|---|---|
| Current interaction is type-level and preserves repeated call-site rows in Core | Opened `Interaction.cs`, `ProjectionService.cs`, `StoreReader.cs`, extractor and tests at `c46e112a` | Compared Core DTO with App tuple mapping | **Verified source** |
| Current native surface drops location/bounds/revision and has no Activity peer | Opened model/surface/shell; bounded repository search | Search for Activity types and source/back events returned none | **Verified source** |
| Pinned E1 intent requires method-level static honesty and gap handling | US-E6 plus architecture §§8/13 at `92e025ae…` | Compared against current type-level contract | **Verified document** |
| E0 checkpoint can supply the proposed foundation if admitted | Opened identity/query/reader/observation signatures at `2b818f1…` | No runtime or integration execution allowed in this lane | **Verified source, admission Flagged** |
| One projection can keep Sequence/Activity/list/inspector consistent | Contract construction and shared-window invariant | Implementation equality oracle not run | **Inferred design** |
| The structural subset can meet all US-E6 clauses without Roslyn CFG APIs | Six-group syntax/symbol spike at `5d361f2a`; §5 proposed rules | Spike does not build a graph or exercise paging/CFG | **Flagged**; independent semantic acceptance and graph/page oracles remain required |
| Native layout and performance meet budgets | No native artifact exists | No shown-window run authorized | **Unverified** |

Rejected alternatives:

1. Extend or rename the current type-level `Interaction` as method execution. Rejected because the
   architecture expressly preserves that label and its identity/grain cannot satisfy method
   entry, overload or control-flow requirements.
2. Build independent Sequence and Activity queries/models. Rejected because paging, omissions,
   identity and source selection could diverge across two answers to one method.
3. Hand-author behavior from the proposal mockup. Rejected because curated fixture data is not a
   producer, source receipt or wire contract.
4. Require interprocedural/runtime simulation in the first tranche. Rejected because no observed
   trace exists and expansion would invent dispatch/order while violating the bounded slice.

Residual risks: no accepted foundation or production shared envelope exists; exact
operation/registration grants remain pending. The syntax/symbol spike does not prove
the proposed structural graph, all supported syntax, paging or parser budgets.
Numeric limits and the fixture are specified but unmeasured and require SRE/Test
admission. Mock and native evidence are absent. Independent author vetoes stay open.

### Revision record and P0 disposition

This revision follows Owner `a766afa8` and independent review `593c7650`; neither
is self-cleared by this author. Peer design lenses: **UML** requires structural
edge names without executable successors; **Data** separates observation identity
from request sequence; **Distributed Systems** requires atomic receipt publication;
**UX** requires one linked G6 workspace; **SRE/Test** require explicit cap units and
missing-measurement refusal; **Simplifier** removes premature DTO/code duplication.
The graph/model proposals are **Inferred design**. The six-group spike remains
**Verified syntax/symbol observation** only. Normalizing G6 resolves this author's
earlier unsupported archetype choice; the control is the independent P0.1 round-trip
gate, which had blocked the old design. Shared defect-register reconciliation is
Conductor-owned because this revision grants only this design plus audit/derived.

| Finding | Revised design response | Remaining independent evidence |
|---|---|---|
| P0.1 | G6 normalized and every facet mapped in §2 | UX round-trip review |
| P0.2 / P0.8 | E2-only; no E1 scope expansion | E2 lane/reviewer |
| P0.3 | Shared logical envelope, row source tokens, partial Restore and publication in §4 | Foundation mapping, exact shared writer/signatures and golden bytes |
| P0.4 | Existing seam table retained; implementation guard is accepted foundation AND exact grants AND shared compatibility AND independent clearance | Conductor/Core/GHCP/Grok join guard |
| P0.5 | Complete mock/native state and receipt oracles in §7.1; exact mock request in §8 | Granted executable HTML harness + rubric, then native proof |
| P0.6 | Inclusive numeric caps, workload, samples, percentile and UI segment targets in §11 | Fixture authorization/freeze, parser probe, SRE/Test acceptance and measurements |
| P0.7 | Structural algorithm, paging grain/closure/boundaries/recomposition in §5 | Executed graph/page oracles and UML/Test acceptance; CFG remains unproven |

Execution graph reused from the admitted programme: bounded inputs → revise this
single design → consistency read → audit/change → derive/check → commit → independent
review. No parallel author or new plan artifact. Finite section checklist is the
termination variant; the 14-call cap is a defect signal, not completion. The first
inventory search was too broad and produced truncated output; subsequent reads were
bounded to exact pinned files. This process correction is not claimed as a new gate.

| Completed | Remaining | Best next action |
|---|---|---|
| Proposed G6/envelope/structural graph/page identity/caps/state contracts revised against Owner and reviewer | All independent clearance, accepted foundation and exact source grants, mock implementation, graph/parser oracles and product/native evidence | Freeze this revision for independent review; Conductor resolves mock grant and foundation separately |
