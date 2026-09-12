---
id: adr-01M2BBCCDZT9CGP0W5YE3YKXDW
title: "PROPOSED — bounded Atlas contracts and native Architecture integration"
type: adr
status: proposed
owner: "@timianmalloo"
phase: "atlas-proposed-architecture"
tags: [code-atlas, proposed, query, ipc, native, coordination]
links:
  - { to: architecture-code-atlas-proposed, rel: refines }
  - { to: adr-01M2BBCCAYMNMM1MFH0653Z0XF, rel: depends-on }
  - { to: spec-addendum-c-perspectives, rel: depends-on }
  - { to: note-atlas-lane-admission, rel: depends-on }
review-by: 2026-12-12
summary: >-
  Extends the existing Core query/local-IPC boundary with versioned manifest-bound Atlas reads.
  Native views consume identity, content state and bounds through existing owner-controlled host seams.
---

# PROPOSED: one bounded read seam, native first

- **Date / author:** 2026-09-12 / `atlas-architecture-astra`.
- **Deciders:** separate Core/Shell acknowledgment and Owner/architecture gates pending.
- **Native ID provenance:** allocated by Conductor after 37-ADR/no-current-duplicate check;
  supplied to author, registration pending. Not a numbered global ADR reservation.
- **Status:** PROPOSED. Factory/menu/host/SH2 ownership unchanged.

## Context

K0 establishes `IWorkspaceQueries`, `ProjectionService`, operation registration and
`WorkspaceClient` as the existing read boundary. Canonical architecture §§C/D.1–C/D.5 preserves
three perspectives, shared factory/menu and retained Architecture host. Native tests were not
executed by K0 or this author. A source-only host map is not native proof.

## Proposed decision

Add negotiated Atlas query capabilities on the existing local/remote read boundary. New records
can form an Atlas facet, but must share authority and behavior; no App filesystem/SQL path.
Proposed operations are capabilities, inventory, outline, source, later view and comparison.
They are semantic contracts, not assertions about existing method names.

Every result propagates:

`workspace/root + scope set + manifest/revision vector + request/selection generation +
logical file/symbol + declaration/span binding + contentState + coverage + bounds`.

Required envelope: operation/schema/capability version, correlation, typed outcome, stable
error/refusal code and permitted recovery. A failure cannot serialize as successful empty data.
Counts carry known total or unknown/withheld denominator; include requested/effective limits,
returned/omitted bytes/rows/nodes/edges and reason/continuation. Filters precede caps and ordering
is stable by canonical identity. No UI re-derives coverage or invents a label from raw evidence.

Cursor scope includes principal/policy, manifest, operation/filter/sort and content binding for
source ranges. Mutation or authorization change invalidates it. The server derives caller
authority; possession of IDs or cursors never grants access.

Negotiate capability/version first. Preserve all existing query names and payload behavior;
unknown Atlas operation/version returns an explicit refusal/supported-version result.
Old daemon compatibility means Atlas unavailable, not a type-only tree fallback.
Golden local/wire tests must independently prove each critical field survives serialization.
Effective response bounds must fit the real IPC frame limit including envelope overhead.

## Native integration

E-0 uses existing approved WPF controls for physical tree, outline, source, inspector and history
when possible. No requirement to introduce WebView into a journey that does not need it.
Later diagrams use the existing approved host and synchronized accessible list, not a new
perspective or independent host stack.

`SurfaceContentFactory`/menu/palette/routing derive from their existing single registry.
Atlas proposes content and selection contracts; Claude/Shell authors shared registration and
SH2 integration after acknowledgment. Existing Core-owned `NodeReaderView`/`CodeViewerView`
can be adapted only by agreement; their old unbound payload is not source-version proof.

Selection is immutable and manifest-bound. New selection increments generation and cancels old
requests. Apply a response only if both generation and manifest match. Back restores file/member/
partial declaration/lens/branch context and focus/scroll without guessing rebinding. Deleted or
changed historical content retains a truthful unavailable/stale state.

Only a trusted broker responding to a human gesture may copy, navigate or export after policy
checks. Repository/spec/session/model strings are inert. WebView, if used later, has no direct
SDK/host-object bridge and no untrusted URI navigation. Closed typed UI messages cannot grant
authority beyond the current selection and user action.

## Async and failure semantics

- Propagate cancellation and deadlines across UI/query/IPC/Core; cancel does not promote partial
  scans or extracts to complete.
- Bound queues and memory; coalesce obsolete scope refreshes, not accepted distinct observations.
  Preserve canonical single-writer/control priority rather than inventing another scheduler.
- Retain per-scope desired-generation fencing; manifests add cross-scope disclosure, not an OS
  transaction. One busy/failed scope produces typed partial/stale state.
- Do not hold database read transactions while waiting on model/UI. Source binding is independent.
- Stale late responses are discarded even when remote cancellation races.
- Missing/unsupported capability has a reason and recovery; no spinner without terminal outcome.

## Alternatives

| Alternative | Why rejected |
|---|---|
| App reads source directly | Second authority and inconsistent confinement/version binding. |
| Unversioned optional fields on old content, silently assumed | Legacy/default values can look coherent while hashes/spans are absent. |
| Full workspace graph per view | Unbounded transport/render/context cost and privacy exposure. |
| Independent Atlas menu/factory/host | Duplicates C/D routing and crosses ownership; creates a fourth-perspective failure. |
| Web-only prototype as delivery | Cannot prove WPF composition, UIA, native focus, retained state or actual filesystem journey. |

## Proof and cost

`AtlasWireCarriesBindingsAndBounds` must fail when any critical field is removed at one hop.
`NativeSourceJourneyThroughComposition` traverses real composition/factory/menu/query/IPC/source;
two projects, overloads/partials, unsupported file, edit-after-index, A-new/B-failed and Back are
required. Keyboard/pointer/UIA/theme/high-contrast/DPI/loading/error/cancel states are real native
proof. WebView focus/list proof applies only when the admitted phase actually uses it.

Candidate performance targets: tree open ≤2 s p95; filter/retained switch ≤150 ms p95 on the
approved approximately 2,500-path workload. These are targets, not measured outcomes.
Record query/render/cancel/late-drop/bytes/omissions on normal paths with correlation and no
sensitive high-cardinality metric labels.

**LOA:** F/T0, Ports and Adapters, CQRS, Hot Path Bypass, Graceful Degradation;
P1/P2/P3/P7/P9/P11 and C4/C7/C8/C9/C11. **Rollback:** disable new capability and remove
Atlas registrations through the Shell owner; old perspectives/queries remain usable.
