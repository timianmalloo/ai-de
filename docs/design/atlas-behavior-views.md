---
id: design-atlas-behavior-views
title: "Atlas E1 static behavior views — proposed design contract"
type: design
status: proposed
owner: "@timianmalloo"
tags: [atlas, e1, sequence, activity, static-analysis, native-ui]
links:
  - { to: mockup-uml-erm-surfaces, rel: relates-to }
review-by: 2026-12-15
summary: >-
  Proposed method-level static behavior contract for one source-backed occurrence model rendered as
  native Sequence and Activity views, with honest bounds, unknowns, source navigation and Back.
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

**Archetype Signature — catalog selection:** G1, Parametric Modeling Workbench, is the nearest
Section-G row because its focal viewport, source/history tree and per-selection detail dock match
the spatial reading shape of a behavior diagram. Its canonical catalog signature is:

`ParametricWorkbench { Type:Configurator; Arch:SpatialBounded; Layout:ViewportWorkbench;
Density:Compact; Nav:Ribbon+CommandPalette; Viewport:DesktopBound;
Input:PrecisionPointer+SpatialGestures; Color:DarkAdaptive; Type:Utilitarian; Depth:Diegetic3D;
Sync:LocalFirst; Persistence:Cloud; Feedback:Optimistic+Confirmed; Motion:Micro;
Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA; }`

Explicit deviations: the Atlas viewport is a read-only 2D static diagram rather than a 3D
constructor; the tree is source/member structure rather than editable feature history; the detail
dock is evidence/provenance rather than parameters; input is keyboard-first plus precision pointer,
not spatial gestures; persistence is the existing session/receipt model, not cloud; feedback is
confirmed; depth is flat; motion is none. The selected method and projection are the master;
Sequence/Activity, accessible list and inspector are synchronized details. Selection and source
entry are serial; reading either presentation is parallel. These deviations preserve G1's
viewport-workbench structure without importing its editing, geometry or regeneration semantics.

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
observation and request sequence. A response that mixes observations is invalid.

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

The projection is derived from verified source and compiler evidence. It is not persisted as graph
truth. If a view cache is later needed, it is a disposable cache keyed by the complete aggregate
identity and must have a rebuild-equality oracle against the same source observation. A later live
read is a different observation, not a rebuild of old history.

### 3.3 Identity and evidence rules

An occurrence key is derived by Core from the selected method observation, source observation,
syntax kind and exact UTF-16 span. It is not derived from display text, target name, line number
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

## 4. Proposed contracts — names and signatures are not admitted APIs

The smallest correct shape is one behavior producer and one public projection. Sequence and
Activity are presentation modes over that projection. The following signatures are exact design
proposals for coordinated review.

```csharp
namespace AiDe.Core.Understanding;

public enum AtlasBehaviorEvidenceKind
{
    ExtractedSyntax,
    ResolvedSymbol,
    AmbiguousSymbol,
    Unavailable
}

public enum AtlasBehaviorConfidence
{
    Extracted,
    Ambiguous,
    Unknown
}

public enum AtlasBehaviorOccurrenceKind
{
    Call,
    RecursiveCall,
    Await,
    Throw,
    Return,
    Gap
}

public enum AtlasActivityNodeKind
{
    Entry,
    Action,
    Decision,
    Merge,
    Loop,
    Await,
    Throw,
    Catch,
    Finally,
    Return,
    Exit,
    Gap
}

public enum AtlasActivityEdgeKind
{
    NextInSource,
    WhenTrue,
    WhenFalse,
    Case,
    LoopBody,
    LoopExit,
    Exception,
    Cancellation,
    Continuation,
    Unknown
}

public sealed record AtlasBehaviorSourceAnchorDto(
    string FileToken,
    string ObservationToken,
    AtlasSpanDto Span);

public sealed record AtlasBehaviorEvidenceDto(
    AtlasBehaviorEvidenceKind Kind,
    AtlasBehaviorConfidence Confidence,
    string Predicate,
    string? Reason);

public sealed record AtlasBehaviorParticipantDto(
    string ParticipantToken,
    string DisplayName,
    string? DeclarationToken,
    AtlasBehaviorEvidenceDto Evidence,
    AtlasBehaviorSourceAnchorDto? DeclarationAnchor);

public sealed record AtlasBehaviorOccurrenceDto(
    string OccurrenceToken,
    int SourceOrdinal,
    AtlasBehaviorOccurrenceKind Kind,
    string FromParticipantToken,
    string? TargetParticipantToken,
    string[] TargetAlternativeTokens,
    string DisplayLabel,
    AtlasBehaviorSourceAnchorDto SourceAnchor,
    AtlasBehaviorEvidenceDto Evidence);

public sealed record AtlasActivityNodeDto(
    string NodeToken,
    int SourceOrdinal,
    AtlasActivityNodeKind Kind,
    string DisplayLabel,
    string? StaticCondition,
    AtlasBehaviorSourceAnchorDto SourceAnchor,
    AtlasBehaviorEvidenceDto Evidence);

public sealed record AtlasActivityEdgeDto(
    string EdgeToken,
    string FromNodeToken,
    string ToNodeToken,
    AtlasActivityEdgeKind Kind,
    string? StaticCondition,
    AtlasBehaviorEvidenceDto Evidence);

public sealed record AtlasBehaviorRequestDto(
    int Version,
    string ScopeToken,
    long ExpectedCoreEpoch,
    string ManifestToken,
    string FileToken,
    string EntryDeclarationToken,
    int OccurrenceOffset,
    int OccurrenceLimit,
    long RequestSequence);

public sealed record AtlasBehaviorProjectionDto(
    int Version,
    string ScopeToken,
    long CoreEpoch,
    string ManifestToken,
    string FileToken,
    string EntryDeclarationToken,
    string SourceObservationToken,
    long RequestSequence,
    string ReconstructionDisclosure,
    AtlasBehaviorParticipantDto[] Participants,
    AtlasBehaviorOccurrenceDto[] SequenceMessages,
    AtlasActivityNodeDto[] ActivityNodes,
    AtlasActivityEdgeDto[] ActivityEdges,
    AtlasBoundsDto OccurrenceBounds,
    int? NextOccurrenceOffset,
    AtlasCompletionState Completion,
    string[] Limitations);

public interface IAtlasReaderQueries
{
    ValueTask<AtlasBehaviorProjectionDto> SelectBehaviorAsync(
        AtlasBehaviorRequestDto request,
        CancellationToken cancellationToken);
}
```

`ReconstructionDisclosure` must equal
`static reconstruction — not observed runtime order`; the producer/codec rejects any other value.
The request has one occurrence window shared by both presentations. A later need for independent
Activity paging requires a new versioned contract; it must not silently make the two presentations
describe different subsets.

The proposed capability is `static-behavior-v1`. A peer that does not advertise it receives no
behavior request. The operation extends the existing framed Atlas reader only after compatibility,
byte-limit, charge/publication and legacy-peer oracles pass. It does not create a second pipe,
reader authority or source loader.

The proposed internal producer entry is:

```csharp
internal static CSharpBehaviorObservationResult Observe(
    CSharpCompilation compilation,
    AtlasCompilationScope context,
    CSharpDeclarationSourceInput source,
    string entryDeclarationObservationKey,
    CSharpBehaviorObservationLimits? limits = null,
    CancellationToken cancellationToken = default);
```

This signature deliberately consumes the Core-issued verified source association already used by
the reference E0 producer. It does not accept an App path, raw source text or display name. Use of
Roslyn control-flow APIs remains a spike-gated implementation choice; this design only depends on
the syntax and semantic evidence listed in section 5.

## 5. Supported subset and refusal grammar

### 5.1 First admitted subset proposed

- Entry points: source-backed ordinary methods, constructors and property accessors that have a
  Core-issued method declaration observation and body span.
- Sequence: invocation and object-creation occurrences directly inside the selected method body;
  exact overload when supported semantic binding resolves it; recursion; repeated occurrences;
  source ordinal; call-site anchor; optional resolved declaration anchor.
- Activity: method entry/exit; blocks; `if`/`else`; `switch` arms with source condition text;
  `for`/`foreach`/`while`/`do`; `return`; `throw`; `try`/`catch`/`finally`; `await`; and calls shown
  as action nodes.
- Cancellation: only an explicitly bound cancellation operation or explicit cancellation exception
  construct may create a cancellation edge. A parameter named `cancellationToken`, a cancellable
  callee signature or an `await` does not prove a cancellation path.
- Errors: explicit `throw`, `catch` and `finally` syntax is represented. Exceptions a callee may
  throw are unresolved unless supported evidence is later admitted.
- Async: `await` is an explicit suspension/continuation marker. The continuation is static source
  structure; scheduler, thread and runtime ordering remain unknown.

Unsupported or deferred constructs include local functions/lambdas as independently selectable
entry methods, dynamic dispatch without a bounded alternative set, iterator/yield behavior,
`goto`, compiler-generated lowering, implicit disposal/monitor behavior, exception propagation
through callees and interprocedural expansion. Encountering one creates a typed gap or limitation;
it never causes the producer to skip the fact and report complete.

### 5.2 Projection state grammar

| State | Required content | Forbidden claim / recovery |
|---|---|---|
| `Unselected` | Prompt to select a supported method | No extractor diagnosis |
| `Loading` | Selected identity and request sequence; prior valid view may remain marked refreshing | No empty-success substitution |
| `ReadyComplete` | Shared projection, exact disclosure, bounds and no unreported omissions | No runtime wording |
| `ReadyPartial` | Rendered retained rows plus exact known omitted counts or unknown denominator reason; visible limitations | No “complete” label |
| `EmptySupported` | Selected supported method and observed zero supported behavior occurrences | No claim that the method does nothing at runtime |
| `Unsupported` | Identity, source anchor and reason; Source remains available when policy permits | No synthetic diagram |
| `Stale` | Last valid projection remains readable and pinned to its old observation; refresh action | No old anchor against new bytes |
| `Canceled` | Retained partial rows, if any, marked canceled; omission reason/denominator state | No promotion to partial success |
| `Error` | Stable code, affected scope, retained identity and recovery action | No swallowed exception or blank canvas |

Only `ReadyComplete`, `ReadyPartial` and `EmptySupported` may render behavior content. A source
state of Changed, Unavailable, Unverifiable, UnsupportedEncoding, TooLargeToVerify, ReadUnstable,
Refused or Canceled cannot be upgraded by the App. Exact known counts use the existing Atlas bounds
semantics. Unknown and withheld denominators remain nonnumeric. `NextOccurrenceOffset` is present
only when another retained page is known to exist.

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

No new mockup is proposed before the one-projection contract and native registration seam are
accepted. If the UX reviewer finds the existing proposal cannot exercise the full state grammar,
the follow-up is an updated synthetic self-contained mockup, never a private-corpus capture.

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

## 11. Instrumentation and performance admission

Operator questions and proposed emitting sources:

| Question | Normal-path source |
|---|---|
| How long did observation, projection, wire and native render take? | `aide.atlas.behavior.observe`, `.query`, `.render` spans |
| How much was requested, returned and omitted, and which bound fired? | count/byte/depth tags from Core bounds plus native rendered counts |
| Which path ran: complete, partial, canceled, unsupported, stale-discarded or failed? | stable result/state code and completion tag |
| Did Sequence, Activity and accessible list consume the same projection? | projection observation token/request sequence and per-consumer row counts |
| Did a failure occur and where? | stable error code, phase and exception type; missing data emits `not recorded` |

Telemetry excludes source text, static condition text, participant/member display labels, absolute
paths and raw identity tokens. A keyed/correlated opaque operation identifier is sufficient.

The retained Sequence/Activity pivot must meet the upstream retained-view switch threshold of
150 ms p95 because it reuses one in-memory projection. Initial behavior query/render and UI-thread
frame thresholds remain **Flagged**: the pinned documents require measured performance but do not
state a behavior-specific initial-open or frame number. Owner/design review must set those values
before implementation acceptance. Native measurement uses the admitted E1 fixture and reports
query, codec, render, participant/occurrence counts, viewport/DPI and cap state. A source comment,
headless model test or HTML mockup cannot clear that gate.

## 12. Confidence ledger, disconfirmation and status

| Claim | Evidence | Falsifier tried in this design pass | Label |
|---|---|---|---|
| Current interaction is type-level and preserves repeated call-site rows in Core | Opened `Interaction.cs`, `ProjectionService.cs`, `StoreReader.cs`, extractor and tests at `c46e112a` | Compared Core DTO with App tuple mapping | **Verified source** |
| Current native surface drops location/bounds/revision and has no Activity peer | Opened model/surface/shell; bounded repository search | Search for Activity types and source/back events returned none | **Verified source** |
| Pinned E1 intent requires method-level static honesty and gap handling | US-E6 plus architecture §§8/13 at `92e025ae…` | Compared against current type-level contract | **Verified document** |
| E0 checkpoint can supply the proposed foundation if admitted | Opened identity/query/reader/observation signatures at `2b818f1…` | No runtime or integration execution allowed in this lane | **Verified source, admission Flagged** |
| One projection can keep Sequence/Activity/list/inspector consistent | Contract construction and shared-window invariant | Implementation equality oracle not run | **Inferred design** |
| The syntactic subset can meet all US-E6 clauses without Roslyn CFG APIs | Explicit syntax/semantic grammar and gaps | No producer spike or implementation | **Flagged**; spike/reviewer may narrow or require a supported CFG contract |
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

Residual risks: foundation admission and final operation/registration paths are pending; supported
control-flow semantics need an implementation spike or bounded source proof; behavior-specific
initial-open/frame budgets are unset; and every independent veto remains open. The conclusion
changes if the accepted E0 reader cannot carry a versioned behavior operation within its strict
compatibility/byte/charge rules, or if independent UML/Test review shows the syntax subset cannot
represent the required exception/cancellation/async cases honestly.

| Completed | Remaining | Best next action |
|---|---|---|
| US-E6 mapped to inspected code or a precise gap; one occurrence model, proposed signatures, state/refusal grammar, manifest, mock disposition, red-first controls and reviewer matrix recorded | Owner foundation decision; exact reader server/remote and host registration paths; independent design gates; source grants; all implementation/native evidence | Conductor settles the shared seams and pins the admitted foundation, then dispatches independent Test/UML/UX/Core review of this proposal |
