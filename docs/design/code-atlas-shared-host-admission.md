---
id: design-code-atlas-shared-host-admission
title: "Code Atlas — frozen walking-host implementation contract"
summary: "Owner-38 corrected dispatch packet: physical-first inventory, lossless render bounds, public composition, required repository forms, persistent transport and conditionally admitted implementation tracks."
type: design
status: dispatch-ready
owner: "@timianmalloo"
tags: [code-atlas, shared-host, admission, proposed]
review-by: 2026-09-19
links:
  - { to: spec-addendum-e-code-atlas, rel: refines }
  - { to: architecture-code-atlas-proposed, rel: depends-on }
  - { to: proof-code-atlas-live-reader-candidate, rel: depends-on }
  - { to: note-atlas-live-reader-horizon, rel: depends-on }
---

# Code Atlas: frozen walking-host implementation contract

## 1. Status, authority and the terminal condition

**DISPATCH-READY DESIGN; OWNER-38 CONDITIONAL AUTHORING GRANT, NOT A SELF-CLEARED GATE.**
Sections 1–12 are the controlling implementation packet. Everything under
**Historical checkpoint** is preserved evidence, not an alternative contract.
Owner turns 34–35 replace the Conversation/DocumentSession approval hypothesis.
Owner turn 37 replaces both the unresolved-transport hypothesis and per-operation
reconnection. Owner turn 38 corrects the lossy wire, accessibility, repository-form,
physical-inventory and outline-limit defects in packet `2af3777b`. That packet remains
in Git history; its defective clauses are replaced here, not relabeled as previously correct.
An implementation may not revive any superseded proposal.

Goal: the first scoped walking-host slice, through the real window and existing
Architecture host, using the admitted native reader and bound source.
Done when: a granted branch implements the exact seams below, the negative controls
have been observed red, the real composition journey passes, and independent reviewers
accept the resulting evidence. This packet is done when this pair is frozen and committed.
Not in scope: diagrams, AI, another perspective, source editing, repository-wide adapters,
policy storage, new dependencies, main integration, counterpart polling or approval by silence.

Authority order:

1. Owner 34–38, recorded before dispatch in
   `docs\notes\atlas-owner\live-reader-horizon.md` in the Conductor tree.
2. **Only** `docs\collaboration\session-contracts.md` §2 assigns file ownership.
   The same Conductor tree carries the latest uncommitted ruling and packet grant.
3. Owner 38 conditionally admits Track C's 20-file/48-leaf and Track S's eight-file/32-leaf
   ceilings after the targeted predicates below clear. This does not transfer ownership.
   The three open counterpart requests are still **OPEN, NOT CONSENT**.

The frozen source base is `2632e5dcd070882eb83a777cc5289e10855707f4`.
The completed compatibility merge is `be3ace85`, joined by `16ea6f73`; the old
precommit wording below is historical. Native reader `a0ffcee3`, acceptance join
`cc67f7c6` and proof `1e688ace` remain distinct evidence. Transport qualification
`c78874bd` was joined into this base. Conductor's candidate-only repeat observed
**79 assertions / 0 failures; baseline EXCLUDED**. It does not prove the baseline
wrong-response hypothesis, exact partial bytes, native-root safety in the spike,
or any production adapter. Existing Core E/S native-source evidence stands separately.

**Confidence convention:** `EXISTING` means a source anchor or retained pinned observation;
`NEW DESIGN` means an exact requirement for future code, not a claim that the method exists.
None of the proposed limits or signatures below are reported as executed production facts.

## 2. Domain, authority and production composition

The bounded context is **workspace code understanding**, not conversation content.
A *read scope* is a short-lived, connection-bound authority aggregate. Its invariant is:
only the currently admitted peer can observe policy-permitted physical inventory beneath
the same native root, and read content eligible under the existing NodeContent basis,
while the epoch, policy generation and lease remain current.
A *manifest* is an immutable observation generation inside that scope. File/declaration
handles and receipts identify observations; they are not grants. A receipt references
Q's bounded history; Restore returns the successor that Q actually produces.

Native root/file identities, grants, verified buffers, compiler objects and authenticating
callbacks stay inside Core. Wire handles are opaque aliases of existing Core identities,
not a second file-identity algorithm. Relative paths are display data, never lookup authority.
The Shell's folder-name `WorkspaceId` is not the daemon workspace security identifier.

**EXISTING production permission path:** `WorkspaceOperations.NodeContent` →
`ProjectionService.NodeContent` → indexed declaring-source information →
bounded, workspace-confined source; `IWorkspaceQueries` and `CoreNodeContentSource`
already expose that read path. Owner 34–35 authorize Atlas to inherit that existing
workspace content permission with additional scope checks. Neither an IPC capability
alone nor `RecordedRootApproval` alone is a new source grant.

**NEW DESIGN production call path:**

```text
Daemon Program: WorkspaceCore.Open(workspaceId, rootPath, dataDirectory)
  → existing DaemonEndpoint / CapabilityRegistry / workspace registrations
  → public AtlasWorkspaceOperations.Register(endpoint, core, workspaceId, rootPath,
      dataDirectory, approvedGitExecutablePath, approvedGitSha256, approvedGitVersion)
  → opaque IAsyncDisposable lifetime; Core privately composes issuer, policy and global budget
Atlas admit request on its own handshaken connection
  → endpoint validates existing capability + actual peer + workspace + current epoch
  → AtlasReadScopeIssuer.AdmitAsync(peer, capabilityToken, expectedEpoch, cancellation)
  → WorkspaceCore.CaptureAtlasIndexedReadSet() captures content-eligibility evidence only
  → AtlasWorkspaceReadPolicy captures native root + in-memory policy generation
  → AtlasGitMembership discovers repository/admin metadata and captures stable membership
  → existing physical enumerator retains folders/files/refused entries under metadata policy
  → membership/semantic evidence annotates observations; neither filters physical visibility
  → AtlasQueryService.CreateForReadScopeAsync(grant, membership, cancellation)
  → scope registry owns Q, manifest handles, receipts and native resources
Every operation / publication
  → issuer.RequireCurrent(scope, actual peer, expectedEpoch)
  → native Q/E/S verification
  → explicit render-only DTO projection
  → publication gate rechecks currentness and commits or refuses
```

The `rootPath` and `workspaceId` above are the arguments already used by the daemon
composition, not newly inferred properties on `WorkspaceCore`. `core.Store.CoreEpoch`
is the existing epoch source used at the daemon composition anchor. No “current=true”
callback, proof JSON, caller-supplied absolute root or UI display ID may substitute.

Exact **NEW DESIGN** Core seams, in namespace `AiDe.Core.Understanding`:

```csharp
internal sealed class AtlasReadScopeIssuer : IAsyncDisposable
{
    internal ValueTask<AtlasIssuedScope> AdmitAsync(
        IpcPeer peer, string capabilityToken, long expectedEpoch, CancellationToken cancellationToken);
    internal AtlasCurrentScope RequireCurrent(
        string scopeToken, IpcPeer peer, long expectedEpoch);
    internal ValueTask EndConnectionAsync(
        IpcPeer peer, AtlasConnectionEndReason reason, CancellationToken cleanupToken);
    public ValueTask DisposeAsync();
}
internal sealed record AtlasIssuedScope(
    string ScopeToken, string InitialManifestToken, long CoreEpoch, DateTimeOffset ExpiresAt);
internal sealed record AtlasIndexedReadSet(
    long CoreEpoch, ImmutableArray<string> PermittedRelativePaths);
// Add to WorkspaceCore; content eligibility only, NEVER a physical-inventory filter.
internal AtlasIndexedReadSet CaptureAtlasIndexedReadSet();
// Add to AtlasQueryService; this is NOT an overload accepting RecordedRootApproval.
internal static ValueTask<AtlasScopedQueryHandle> CreateForReadScopeAsync(
    AtlasReadScopeGrant grant, AtlasMembershipSnapshot membership, CancellationToken cancellationToken);
```

`AtlasReadScopeGrant`, `AtlasCurrentScope` and `AtlasScopedQueryHandle` are NEW internal,
non-serializable types. The issuer alone constructs the grant. It contains the authenticated
connection/process/workspace binding, native root handle/identity, epoch, policy generation,
absolute expiry, revocation state and cancellation source. The scoped handle owns the
existing `IAtlasQueries` implementation, initial manifest and awaited shutdown.
`RequireCurrent` never returns an inactive scope and never opens source on refusal.

`AtlasWorkspaceReadPolicy` is a NEW **in-memory** adapter over the existing workspace
permission, not a new approval service. Its two decisions are distinct:

| Decision | Existing eligibility mapping / required production behavior |
|---|---|
| Physical metadata visibility | Native enumeration under the approved source root, constrained by the existing read policy. Semantic-index absence and Git-untracked status do not hide an entry. Retain directories, refused/unavailable entries and safe reasons where policy permits their metadata; policy-withheld names/parents remain withheld with an explicit unknown/withheld bound. |
| Source content eligibility | Reuse the exact declaring-source resolution and bounded/confined read checks of `WorkspaceOperations.NodeContent` → `ProjectionService.NodeContent` for the current workspace. `CaptureAtlasIndexedReadSet` records paths that satisfy that existing basis at the captured epoch; it is evidence for content admission, not permission merely because a path occurs in an index. Selection revalidates the same basis and native binding before opening content. |
| No affirmative existing content basis | Do not authorize additional indexed or unindexed source by silence. Preserve permitted metadata and return a typed content refusal/unavailable reason. An unindexed file is neither hidden from physical inventory nor automatically granted content access. Any separately existing eligible path must pass the same NodeContent mapping, demonstrated by a parity fixture, before being admitted. |

Changing the root, metadata policy or content eligibility increments the policy generation
and revokes its scopes.
Reindex/epoch change, native-root replacement, capability revocation, expiry, connection
end and daemon shutdown also invalidate scopes. Check these at admission, before each
read and at response publication. Membership is checked again for each selected handle.
Additional content access beyond that existing basis requires the applicable approval and
is not implemented here. Physical metadata visibility does not itself grant content access.

The semantic workspace index, Git's index and Q's source observation are different things.
Membership permission does not prove bytes equal an old semantic index. `IndexedMatch`
continues to mean the existing Q binding verification succeeded; do not invent an indexed
hash where the existing source does not provide one. The DTO carries the observation
handle and state, not an assertion that permission implies content freshness.

No new database, migration, durable policy table or persistent receipt cache is needed:
**schema/migration/rollback: N/A**. Reuse existing indexed data; all new scope/manifest
registries are bounded ephemeral resources. Revocation is monotonic within a scope.

## 3. Trusted Git membership: exact first-slice boundary

`AtlasGitMembership.CaptureAsync(AtlasReadScopeGrant, CancellationToken)` is NEW DESIGN
and returns `AtlasMembershipSnapshot`. Grain: one snapshot is one native source-root
identity, discovered repository/worktree/admin association, HEAD observation and index
generation observed consistently. Membership annotates physical inventory; it does not
replace it or authorize content. The capture is read-only with respect to the repository.

**Required forms are ordinary clones, linked-worktree `.git` files and nested approved
source roots.** Repository/admin discovery is separate from source-root authority.
A parent repository or external linked-worktree admin directory can supply bounded metadata;
its discovery never widens the approved source root. The copied-index/inert-directory
default from `2af3777b` is withdrawn as unqualified, not shipped as a safety mechanism.

Use the established bounded approved-Git execution approach, with the following contract:

1. Pin the approved native source-root handle/identity first. Use the approved Git
   executable to resolve repository top level, worktree Git directory, common directory
   and index path. Let Git resolve `.git` files and ref/index formats; do not build a
   general Git parser. Validate bounded discovery results and their native associations.
   A nested source root remains the authorization boundary even when the repo is above it.
2. Invoke only a Core-approved absolute executable with configured expected digest/version.
   Never use workspace/ambient PATH, a shell, auto-download or repository commands.
   Use `ProcessStartInfo.ArgumentList`. Fixed read-only command allowlist: `rev-parse`
   for discovery/HEAD, and `ls-files --cached --stage -z` for tracked membership.
   Discovery uses absolute path output from the approved Git version. Membership paths
   are repo-relative, explicitly rebased to the approved root; outside-root records never
   become source handles or visible names. No `status`, `diff`, checkout, fetch, submodule
   execution, filter, hook or helper command is added.
3. Supply an allowlisted environment only: OS-required variables plus trusted executable/
   system directories, `GIT_CONFIG_NOSYSTEM=1`, `GIT_CONFIG_GLOBAL=NUL`,
   `GIT_CONFIG_SYSTEM=NUL`, `GIT_TERMINAL_PROMPT=0`, `GIT_OPTIONAL_LOCKS=0`,
   `GIT_NO_REPLACE_OBJECTS=1`, `GIT_ATTR_NOSYSTEM=1`, `GIT_PAGER=`, `PAGER=`.
   Remove inherited Git config injection, tracing, SSH, proxy, helper and loader overrides.
   Explicit command configuration disables `core.fsmonitor`, `core.untrackedCache`,
   `submodule.recurse`, `maintenance.auto` and `gc.auto`; clears `credential.helper`;
   sets `protocol.allow=never`; and disables optional locks/pagers.
   The fixed commands must not invoke repository-configured hooks/helpers/fsmonitor or
   network. Repository configuration can still be read: do **not** call this an inert
   configuration sandbox. Prove its no-execution predicate with hostile-config fixtures.
4. Capture HEAD/index/root consistently, including the linked worktree's own HEAD/index
   and shared ref storage where applicable. Use bounded native administrative handles
   denying write/delete sharing for the state used by the observation; Git owns ref and
   index decoding. Record native identities, HEAD result and a streamed index digest.
   Recheck discovery association, held-file identities and stamps before accepting
   capture and before publication. A before/after hash alone does not exclude ABA races.
   A changing ref/index/root, held-handle failure or incomplete snapshot yields a typed
   unstable/unavailable observation, not complete-empty membership or source permission.
   Loose/packed refs and linked/nested forms are not categorically rejected.
5. Decode complete NUL-delimited membership with strict UTF-8 and mode/object ID/stage
   validation. Accept only unique stage-0 regular-file membership; malformed records,
   duplicate entries and invalid/traversal paths fail the capture. Symlink/gitlink records
   carry safe refusal/disclosure metadata where policy permits, never a submodule descent.
   The physical enumerator independently retains folders and files, including untracked,
   refused and unavailable entries. Semantic-index evidence never filters that enumeration.
6. Require clean process exit, complete framing and consistent native capture. On timeout,
   cancellation, limit overflow or malformed output, publish no membership success.
   Missing/unstable membership has an explicit unknown/withheld bound; do not relabel it
   a complete physical-inventory denominator. Source selection still requires its own
   content-eligibility and currentness checks.
7. Kill only the owned process tree if cancellation does not stop it; await process exit
   and both bounded stream readers, release native handles, then release reservations.
   No copied-index scratch is required by this design. Any qualification fixture stays
   inside the granted worktree/Core fixture data directory and is cleaned by its owner.

NEW limits: discovery output 16 KiB total; HEAD result 4 KiB; streamed index 8 MiB;
Git stdout 4 MiB; stderr 16 KiB; 25,000 records; 4,096 UTF-8 bytes per relative path.
Each capture runs one process at a time, at most six fixed invocations, five seconds per
invocation within the 30-second total admission deadline; at most two Git processes
daemon-wide; cleanup two seconds. Budget overflow is a disclosed incomplete observation,
not a hidden repository-form exclusion. There is no automatic retry loop.

**Narrow membership Security qualification remains OPEN:** NQ1 proves the fixed command/
environment combination cannot execute hostile hooks/helpers/fsmonitor or network on
the approved Git version; NQ2 proves native snapshot ownership across loose/packed refs,
linked-worktree admin associations and nested roots, including root/index/ref replacement.
These are specific execution/currentness threats, not authority to invent a Git parser.
Owner permits at most ten additional membership-source/test leaves only if a needed
mechanism requires this qualification; the parent must allocate that bounded work.
All three required repository forms must pass before the membership gate clears.

## 4. Versioned read-only wire and constructor-safe projection

Reuse the existing IPC envelope, handshake, workspace capability and one serialized exchange
at a time. **NEW operation names are exact and case-sensitive:**

| Operation | Request payload | Response payload |
|---|---|---|
| `atlas.capabilities.v1` | `AtlasCapabilitiesRequestDto` | `AtlasCapabilitiesDto` |
| `atlas.admit.v1` | `AtlasAdmitRequestDto` | `AtlasAdmitDto` |
| `atlas.inventory.v1` | `AtlasInventoryRequestDto` | `AtlasInventoryPageDto` |
| `atlas.select.v1` | `AtlasSelectRequestDto` | `AtlasSelectionDto` |
| `atlas.restore.v1` | `AtlasRestoreRequestDto` | `AtlasSelectionDto` |
| `atlas.release.v1` | `AtlasReleaseRequestDto` | `AtlasReleasedDto` |

Capability discovery follows the existing successful handshake on the same connection;
it is not a second handshake or a second authority token. It advertises version `[1]`
and required features `persistent-scope`, `terminal-abandonment`, `opaque-bindings`,
`utf16-pages`, `bounded-receipts`. No intersection, unknown operation or missing feature
means typed `UnsupportedVersion`; do not try the proof composition or another transport.
Existing envelope capability and epoch validation remain mandatory for discovery and admit.

All DTOs below are **NEW DESIGN**, sealed positional records with public constructor
parameters matching property names and types exactly. JSON uses camelCase property names,
explicit case-sensitive string enums (integer enum values forbidden), explicit nulls,
maximum depth 32 and strict required-field/unknown-field validation at this boundary.
Reject duplicate JSON property names before DTO construction. Malformed input never reaches Q.
Use `System.Text.Json` already used by IPC; add no serializer package.

| DTO | Complete fields, in constructor order |
|---|---|
| `AtlasCapabilitiesRequestDto` | `int[] SupportedVersions` |
| `AtlasCapabilitiesDto` | `int[] Versions, string[] Features, int MaxFrameBodyBytes, int MaxPageTextUtf8Bytes, int MaxPageItems` |
| `AtlasAdmitRequestDto` | `int Version, long ExpectedCoreEpoch` |
| `AtlasAdmitDto` | `int Version, string ScopeToken, string InitialManifestToken, long CoreEpoch, DateTimeOffset ExpiresAt` |
| `AtlasInventoryRequestDto` | `int Version, string ScopeToken, long ExpectedCoreEpoch, string ManifestToken, int Offset, int Limit` |
| `AtlasSelectRequestDto` | `int Version, string ScopeToken, long ExpectedCoreEpoch, string ManifestToken, string FileToken, string? DeclarationToken, int SourceOffset, int SourceLength, int OutlineOffset, int OutlineLimit` |
| `AtlasRestoreRequestDto` | `int Version, string ScopeToken, long ExpectedCoreEpoch, string ReceiptToken` |
| `AtlasReleaseRequestDto` | `int Version, string ScopeToken, long ExpectedCoreEpoch` |
| `AtlasReleasedDto` | `int Version, bool Released` |
| `AtlasInventoryPageDto` | `int Version, string ScopeToken, long CoreEpoch, string ManifestToken, AtlasCompletionState Completion, AtlasFileDto[] Files, AtlasBoundsDto Bounds, int? NextOffset, string[] Disclosures` |
| `AtlasFileDto` | `string FileToken, AtlasDirectoryEntryKind Kind, string? ParentToken, string RelativePath, AtlasFileClassification Classification, AtlasFileAvailability Availability, AtlasCountDto DeclarationTotal, string? Reason` |
| `AtlasSelectionDto` | `int Version, string ScopeToken, long CoreEpoch, string ManifestToken, string FileToken, string? DeclarationToken, string? ReceiptToken, AtlasSourceDto Source, AtlasBoundsDto SourceBounds, AtlasOutlineState OutlineState, string? OutlineReason, AtlasOutlineRowDto[] Outline, AtlasBoundsDto OutlineBounds, int? OutlineNextOffset, AtlasCoverageDto Coverage, string[] Disclosures` |
| `AtlasSourceDto` | `SourceProjectionState State, string ObservationToken, string? BindingToken, string? DecoderId, string? Text, AtlasSpanDto? PageSpan, AtlasSpanDto[] Highlights, int? NextOffset, string? Reason` |
| `AtlasOutlineRowDto` | `string DeclarationToken, string DisplayName, AtlasDeclarationKind Kind, AtlasSpanDto Span` |
| `AtlasSpanDto` | `int Start, int Length` |
| `AtlasCountDto` | `AtlasDenominatorState State, long? Value, string? Reason` |
| `AtlasCoverageDto` | `AtlasDenominatorState State, double? Value, string? Reason` |
| `AtlasBoundsDto` | `AtlasBoundsDimension Dimension, int RequestedLimit, int EffectiveLimit, int ReturnedRows, long ReturnedContentBytes, AtlasDenominatorState DenominatorState, long? DenominatorValue, string? DenominatorReason, string? OmissionReason, AtlasBoundsDimension? OmissionDimension` |

NEW public enums, in the same `AtlasReaderContracts.cs` file:
`AtlasBoundsDimension { InventoryRows, OutlineRows, SourceUtf16CodeUnits, SourceUtf8ContentBytes, SerializedMetadataBytes, Policy }`;
`AtlasOutlineState { Available, Canceled, BudgetExceeded, Unsupported, Unavailable, Refused }`.
`AtlasConnectionEndReason` used by the public endpoint hook is also public in that file.
All public ports, DTOs and DTO field types are public across Core/Daemon/App.

Envelope failures use existing IPC failure framing, with NEW stable Atlas error codes:
`Atlas.UnsupportedVersion`, `Atlas.Malformed`, `Atlas.AdmissionRefused`,
`Atlas.ScopeInvalid`, `Atlas.StaleManifest`, `Atlas.ReceiptUnavailable`,
`Atlas.MembershipUnavailable`, `Atlas.Busy`, `Atlas.DeadlineExceeded`,
`Atlas.PayloadTooLarge`, `Atlas.ConnectionLost`. The endpoint does not embed a successful
DTO inside an error response. Public reasons are fixed safe messages; do not echo paths,
tokens, source or Git stderr. `ScopeInvalid` includes wrong peer/process/workspace/epoch,
root/policy change, expiry and revocation without revealing which foreign scope existed.

Field constraints:

- Handles are nonempty opaque strings, at most 256 UTF-8 bytes. Validate lookup plus
  scope/manifest ownership; lexical validity alone confers nothing. No raw grant,
  native identity, absolute path, `ExpectedBinding`, approval record or compiler object is sent.
- `AtlasFileDto` represents a physical inventory entry, not only a selectable source file.
  Preserve the existing `AtlasDirectoryEntryKind` one-for-one, including `Directory`,
  `RejectedLink` and `Unavailable`. Its historically named FileToken is an opaque entry
  handle; Select accepts only a permitted file-kind handle. ParentToken aliases its parent
  in the same scope/manifest handle table, never the native parent identity. Null means
  the admitted scope root, not a missing parent silently reconstructed from a path.
  Withhold a child row if policy forbids revealing its necessary parent association.
  Reason preserves the safe refusal/unavailable explanation without raw native details.
- Inventory/outline Offset ≥ 0 and Limit 1–128 **rows**; apply existing `PageRequest`
  only to those item pages. SourceOffset and SourceLength are **UTF-16 code units**:
  offset ≥ 0, length > 0, checked offset-plus-length arithmetic. Do not apply the 128-row
  limit to source characters and do not allocate the requested source length unchecked.
  Clamp the effective source window to the verified extent and scalar-safe display-byte
  cap, carrying those effective bounds explicitly. Count Known requires
  nonnegative Value and no Reason. Unknown/Withheld requires null Value and a nonempty
  safe Reason. Coverage Known is finite 0–1; Unknown/Withheld has no numerical value.
  Null never becomes zero. Unknown totals never imply “all loaded.”
- Inventory Bounds has Dimension `InventoryRows`; Selection has separate SourceBounds
  (`SourceUtf16CodeUnits`) and OutlineBounds (`OutlineRows`). RequestedLimit preserves
  the caller's row limit or SourceLength; EffectiveLimit is the applied limit in that same
  dimension, nonnegative and no greater than requested. ReturnedRows counts actual
  inventory/outline entries, including directories and refused metadata rows; it is zero
  for SourceBounds. ReturnedContentBytes counts only unescaped UTF-8 **source content**:
  inventory metadata and outline metadata return **0 content bytes**, even when their JSON
  occupies a large frame. SourceBounds reports the actual returned Text byte count.
  Serialized-body/metadata metrics remain separate, never substituted for content bytes.
- Each bounds denominator uses its declared dimension. Known requires a nonnegative
  DenominatorValue and null DenominatorReason; Unknown/Withheld requires a null value
  and nonempty safe reason. OmissionReason and OmissionDimension are both present when
  data is omitted and both null otherwise. For example a UTF-8 display cut uses
  `SourceUtf8ContentBytes`, a row-page cut uses its row dimension, policy withholding
  uses `Policy`, and a metadata-size cut uses `SerializedMetadataBytes`.
  A refused/canceled phase may have EffectiveLimit zero; it retains the original request.
  Inventory total and outline total are derived from their bounds denominators, not
  separately stored copies. Truncation never erases the omitted dimension or unknown state.
- `NextOffset` is explicit, strictly advances when present, and comes from the server's
  accepted continuation. The reader never computes it from visible child count.
  Inventory offsets count admitted entries, not UI folders. Source offsets/spans count
  UTF-16 code units in the verified decoded text; they never split a surrogate pair.
  Outline offsets count admitted declarations. Null continuation means no additional
  page is offered, not that an unknown/withheld denominator has become known.
- Source text unescaped UTF-8 ≤ 131,072 bytes; source window limits are bounded by both
  this cap and Q's native limit. Encoded non-text metadata ≤ 131,072 bytes. Also measure
  the **complete serialized response envelope body** and enforce ≤ 1,048,576 bytes.
  The four-byte prefix is additional. JSON escaping, quotes, arrays and error metadata
  count. The 8 MiB Core verification buffer is never a wire page.
- Select's window/outline arguments are NEW bounded projection inputs, not a claim about
  existing native request constructors. Core adds the window projection needed to expose
  existing verified text safely; it reuses Q/E/S binding checks and does not bypass them.
  If even one item cannot fit, return a typed size refusal, not a silently shortened success.
- The exact nine source states are the EXISTING `SourceProjectionState`:
  `IndexedMatch`, `Changed`, `Unavailable`, `Unverifiable`, `UnsupportedEncoding`,
  `TooLargeToVerify`, `ReadUnstable`, `Refused`, `Canceled`.
  Only `IndexedMatch` may carry Text/PageSpan/Highlights/BindingToken. All other states
  clear previous source and highlights and carry a safe Reason. They are not empty matches.
- OutlineState/OutlineReason are independent of Source.State. An outline-only timeout,
  cancellation, budget failure or unsupported syntax changes only the outline state/bounds;
  already verified source may remain `IndexedMatch`. An abandoned whole operation or revoked
  lease still forbids successful publication under §5; this separation does not bypass it.
  The input boundary remains the admitted **8 MiB** source-verification input, including
  for outline work. There is no new smaller outline-input ceiling.

**Constructor mapping is explicit, not reflection over native models.**
`AtlasWireProjection` maps native entry identity and parent identity through the same
scope's opaque handle table, preserving Kind and safe Reason. Native outline ObservationKey
maps to a declaration handle; native page Text/PageSpan/Highlights use validated scalar
copying; source state maps one-for-one. Request limits plus native effective limits,
returned rows/content bytes, denominator state/value/reason and omission reason/dimension
map field-for-field into AtlasBoundsDto. Never derive content bytes from serialized DTO size,
or reconstruct a missing parent from RelativePath. A BindingToken refers to a Core-owned
verified binding. `AtlasSourceBinding`, `SourceProjection.IndexedMatch` and their private
or invariant-rich construction paths are **not** deserialized in App.

Local/native → shared render projection → wire DTO → remote validation → reader all use
these same fields. The local bridge must not pass richer directory/bounds data that the
remote route drops. Golden fixtures contain a folder and child, a refused entry with
reason, policy withholding, Unknown/Withheld denominators, omitted dimensions and an
inventory with nonzero metadata/frame bytes but **zero ReturnedContentBytes**.

The new render port is shared by the local proof bridge and the remote client:

```csharp
public interface IAtlasReaderQueries
{
    ValueTask<AtlasInventoryPageDto> InventoryAsync(
        AtlasInventoryRequestDto request, CancellationToken cancellationToken);
    ValueTask<AtlasSelectionDto> SelectAsync(
        AtlasSelectRequestDto request, CancellationToken cancellationToken);
    ValueTask<AtlasSelectionDto> RestoreAsync(
        AtlasRestoreRequestDto request, CancellationToken cancellationToken);
}
public interface IAtlasReaderLease : IAsyncDisposable
{
    string ScopeToken { get; }
    string InitialManifestToken { get; }
    long CoreEpoch { get; }
    DateTimeOffset ExpiresAt { get; }
    IAtlasReaderQueries Queries { get; }
    CancellationToken Invalidated { get; }
    bool IsTerminal { get; }
}
public interface IAtlasWorkspaceReader : IAsyncDisposable
{
    ValueTask<IAtlasReaderLease> AdmitAsync(CancellationToken cancellationToken);
}
```

These NEW interfaces/DTOs live in `AtlasReaderContracts.cs`; wire dispatch uses the
same value records rather than defining a second copy of each field. `AtlasReaderProjection`
and `AtlasLocalReaderQueries` bridge the unchanged native `IAtlasQueries` and retained
proof constructor. `AtlasRemoteReader` implements the read-only port and validates DTO
shape, epoch, handles and continuation before returning anything to WPF.
The transport never constructs a fake verified source observation to satisfy a constructor.

## 5. Awaited server seam and publication ordering

**EXISTING anchors:** `DaemonEndpoint.Register(string,
Func<IpcRequest,IpcPeer,IpcResponse>)` at :42; synchronous `Invoke` at :88;
`WorkspaceOperations.Register(DaemonEndpoint,ProjectionService)` at :175.
`IpcServer.cs`, `IpcFraming.cs`, `IpcClient.cs`, `IpcContract.cs` are existing files.
The following are **NEW signatures**, not descriptions of current code:

```csharp
// DaemonEndpoint
public void RegisterAsync(string operation,
    Func<IpcRequest, IpcPeer, CancellationToken, ValueTask<IpcResponse>> handler);
public ValueTask<IpcResponse> InvokeAsync(
    IpcRequest request, IpcPeer peer, CancellationToken cancellationToken);
public void RegisterConnectionEnded(
    Func<IpcPeer, AtlasConnectionEndReason, CancellationToken, ValueTask> handler);
public ValueTask ConnectionEndedAsync(
    IpcPeer peer, AtlasConnectionEndReason reason, CancellationToken cleanupToken);
// IpcServer: the production accept loop must await this connection task and retain ownership.
private Task HandleConnectionAsync(
    Stream pipe, IpcPeer peer, CancellationToken connectionCancellation);
// AtlasWorkspaceOperations.cs: the ONE public production-composition facade.
public static class AtlasWorkspaceOperations
{
    public static IAsyncDisposable Register(
        DaemonEndpoint endpoint, WorkspaceCore workspace, string workspaceId,
        string sourceRootPath, string dataDirectory, string approvedGitExecutablePath,
        string approvedGitSha256, string approvedGitVersion);
}
```

`AtlasConnectionEndReason` is NEW: `Released`, `Disconnected`, `Abandoned`,
`ProtocolViolation`, `Revoked`, `Expired`, `Shutdown`.
Its declaration is public in the already-listed `AtlasReaderContracts.cs`. The facade's
parameters are all public Core/BCL types; no signature exposes an internal issuer,
grant, policy or budget. It validates configuration/root association, privately constructs
those objects inside Core, registers handlers and returns only their opaque awaited lifetime.
Daemon uses `await using IAsyncDisposable atlasLifetime = AtlasWorkspaceOperations.Register(...)`
with the same workspace/root/data arguments already used for WorkspaceCore.Open and the
approved executable configuration. It disposes this lifetime before disposing WorkspaceCore.
App consumes only the public reader ports/DTOs. No `InternalsVisibleTo`, new facade file,
project edit or direct Daemon construction of internal Core objects is permitted.
Registration rejects duplicate sync/async names. `InvokeAsync` uses the same validation
as existing `Invoke`; it awaits async delegates, or invokes an existing sync delegate
directly and returns its result. Existing sync Register/Invoke and wire operations keep
their behavior. A sync call to an async-only name returns a typed unsupported-dispatch
error; it never blocks on a task. Tests must prove the old sync route as well as the new route.

The server loop owns exactly **one frame reader**. After reading one whole request it
does not start the next frame read while work executes. During that interval a single
one-byte EOF/unsolicited-input monitor may own the pipe read. Zero bytes means disconnect;
one unsolicited byte means protocol violation and revocation. On handler completion,
cancel **and await** that monitor before response publication. A positive read or EOF
that won its cancellation race still terminates the connection. Only after response write
does the main frame reader resume. No competing reader, detached `Task.Run` cleanup or
correlation-cancel protocol is admitted.

Use an independently timed server operation cancellation source, connected to scope
revocation and connection end. A caller's cancellation is not the server deadline.
Work, subprocesses and stream readers must actually observe cancellation and be awaited.
After a fully drained deadline the server may return `Atlas.DeadlineExceeded` and keep
the healthy connection. Unknown framing or undrained work cannot be described as clean.

**Linearization is normative:**

1. Each connection has one state gate and an attempt-local identity, never a wire request ID.
   Terminal state is monotonic. Scope revocation and response commit serialize through the
   issuer's currentness/publication gate.
2. Complete handler work, drain the monitor, validate/project and serialize into a bounded
   response buffer. Recheck capability, epoch, scope, policy/root/membership stamps and
   attempt abandonment **at the commit gate**, not just at request admission.
3. If abandonment/revocation wins that gate, no successful response is committed or written.
   If response commit wins, that is the server response's linearization point; a later
   revocation is later, not retroactively earlier. A failed/partial write is still terminal.
   The server must not hold a monitor read or start another frame read during this write.
4. Client exchange completion validates the full frame before atomically adopting returned
   manifest/receipt state. WPF publication separately requires the same workspace generation,
   live view-operation generation and non-invalidated lease. Cancellation/replacement winning
   that gate forbids successful visual publication. No late callback may paint another scope.
5. Terminal transition removes scope/manifest authority immediately, cancels work, then awaits
   resource disposal. Connection-ended hooks run once in the server connection task's `finally`;
   `CapabilityRegistry.RevokeConnection` and issuer cleanup both run even after handler failure.
   Cleanup has its own bounded token, not an already-canceled request token and not `Token.None`.

Remote revocation after an already committed success is not a claim that bytes can be recalled.
It invalidates subsequent work and the lease on observed connection end; local workspace
replacement clears immediately. Review/tests must distinguish server response commit,
client exchange completion and WPF publication rather than calling all three “completion.”

## 6. Persistent client lifetime and resource bounds

`WorkspaceClient.CreateAtlasReader()` is a NEW additive method returning
`IAtlasWorkspaceReader`. It preserves the authenticated daemon/workspace connection
information internally. It opens a **separate Atlas pipe**, not source operations on the
existing general query connection. `AtlasRemoteReader` owns that pipe and serial exchange.

One healthy connection, handshake and scope serves Inventory → Select file → Select member
→ Back. There is no connection per operation. Clean completed operations and clean server
deadline responses retain it. Q's actual bounded receipt retention and Restore successor
semantics are authoritative; the spike's one-shot Back is nonnormative.

| Cancellation/failure point | Required client behavior |
|---|---|
| Waiting for exchange gate / before any write | Cancel only that attempt; scope/connection remain healthy |
| During possible write, response read, or unknown framing | Atomically mark terminal; close owned pipe; cancel linked attempt; reject further work |
| Complete validated frame, cancellation wins visual publication | No render; keep healthy framing. Retain validated returned session state internally; next inventory resynchronizes the view, with no automatic Select/Back replay |
| Completed A canceled while B runs | A's callback is detached or proves its attempt is inactive; it cannot close B's pipe |
| ScopeInvalid, revocation, expiry or disconnect | Clear source and history usability; terminate lease; no silent retry |
| Explicit reconnect action | Dispose/drain old owner, connect and handshake anew, readmit and reinventory; all prior scope/manifest/declaration/receipt handles are rejected |

No `.Result`, `.Wait()`, `GetAwaiter().GetResult()` or substitution of `CancellationToken.None`
for the caller token is permitted in the new path. Cancellation registrations are disposed
by their owning attempt. Client failures never release a dirty pipe back to the healthy queue.

The following are NEW, explicit admission ceilings, not spike measurements:

| Resource | Scope and ceiling | Enforcement / overflow behavior |
|---|---|---|
| Atlas connections/scopes | **Daemon process-wide** 4, including negotiating and draining connections; 1 scope per connection | Reserve before handshake/admit; Busy before creating Q; draining counts until disposal finishes |
| Work/pending | **Daemon-wide across all scopes** 4 active, 16 pending | One shared bounded FIFO scheduler; pending cancellation removes its own ticket; no per-scope bypass |
| Q's existing bounds | Per Q instance: 4 active / 16 pending | Retain; these are not the global limit. Outer scheduler is acquired before Q work |
| Git processes | Daemon-wide 2, also counted as active work | No nested permit acquisition that can deadlock; admission reserves the complete work class |
| Active source verification | Daemon-wide at most 4 × 8 MiB native buffers | Byte reservations before allocation; never wire or cache those buffers |
| Owned byte buffers | Daemon-wide 64 MiB total | Includes verification, Git captures, stream/framing/serialization buffers; reserve actual requested sizes; refuse before exceeding |
| Retained projected data/handles | Daemon-wide 16 MiB charged encoded payload/key sizes; per scope ≤ 25,000 file handles | No uncharged source-page cache; Q receipt cap is additionally retained; evicted receipt → ReceiptUnavailable |
| Compiler work | Daemon-wide one active parse; retain the admitted 8 MiB source input boundary | Cancellation/deadline/budget/unsupported outcomes affect OutlineState and OutlineBounds only; independently verified source remains IndexedMatch where applicable |
| Scope lifetime | Absolute five minutes; no silent renewal | Expiry terminates scope; visible readmission needed |
| Server deadlines | Admit 30 s; inventory/select/restore 10 s; release 2 s; cleanup 2 s | Independent clocks; clean deadline only after owned work drained |
| Client pending | Per workspace Atlas owner 16 waiting exchanges | Further requests fail Busy; UI coalesces superseded navigation, never queues unbounded work |

The 64/16 MiB numbers bound **charged buffers and retained encoded data**, not total CLR
heap/Roslyn/OS pipe memory. Do not report them as a process working-set guarantee.
Record peak incremental process memory and parse cancellation latency in the proof.
Uncooperative work is a failed cleanup oracle: mark the scope terminal, retain its
reservation/task in the daemon-owned drain set and refuse replacement capacity until it
really exits. Never claim reclamation or release a permit merely because a timer fired.
The server supervisor owns and awaits that drain set at shutdown; no orphaned cleanup task.

## 7. Architecture-only host and awaited Shell lifecycle

**EXISTING, reused unchanged:** `PerspectiveSet.Architecture` at `Perspectives.cs:58–60`;
`PerspectiveMenu.For` at :68–97; `DockHost.Create` at :54–85 and `AdmissionFor` at :92–99.
`MainMenuBuilder` and `CommandPalette` consume that row/catalog derivation.

Add exactly **one** `SurfaceContentFactory.Kinds` row:
kind **`code-atlas`**, title **`Code Atlas`**, Architecture-only, workspace-dependent,
native non-windowed content. Its builder synchronously returns NEW `AtlasLoadingHost`.
No fourth perspective, separate opener list, generic docking/adapter rewrite or new palette.
The row uses the existing approval/design/native reader style and accessibility naming.
`SurfaceContentFactory.Create(Surface)` stays synchronous and does no IPC or blocking wait.

NEW Shell seams:

```csharp
public sealed class AtlasLoadingHost : UserControl
{
    internal AtlasLoadingHost(AtlasWorkspaceOwner owner);
    public Task ActivateAsync(CancellationToken cancellationToken);
    public ValueTask DeactivateAsync();
}
internal sealed class AtlasWorkspaceOwner : IAsyncDisposable
{
    internal AtlasWorkspaceOwner(IAtlasWorkspaceReader reader, long workspaceGeneration);
    internal ValueTask<IAtlasReaderLease> GetLeaseAsync(CancellationToken cancellationToken);
    internal ValueTask StopViewsAsync();
    public ValueTask DisposeAsync();
}
// Add overload, keep the existing IAtlasQueries/string proof constructor.
public AtlasReaderView(
    IAtlasReaderQueries queries, AtlasAdmitDto admission);
// Add to WorkbenchShell; keep the old synchronous attach for non-Atlas callers.
public Task AttachWorkspaceAsync(
    IWorkspaceQueries queries, string? dataDirectory, IWorkspaceCommands? commands,
    string workspaceId, string rootPath, string? workspaceName,
    IAtlasWorkspaceReader? atlasReader, CancellationToken cancellationToken);
public ValueTask DisposeAsync();
```

Parameter names in the NEW async attach are design names; the existing six-argument
`AttachWorkspace` shape is anchored below, not silently asserted identical to these names.
The loading-host constructor is internal because AtlasWorkspaceOwner is internal; both
are used within the App assembly. Public Core contracts contain no internal parameter or
return types. Compile the Core facade from Daemon and the reader seam from App at C's
first checkpoint, rather than waiting for the host implementation to reveal accessibility errors.
Use the existing `DESIGN.md` and native `AtlasReaderView`; do not introduce a mockup,
color system or replacement reader. The retained proof constructor uses the local
render projection, so its old callers and tests remain meaningful.

The first activation loads visibly: “Opening Code Atlas…” → admitted inventory → bound
source/outline → member → Back. Missing workspace says “Open a workspace to use Code Atlas.”
Lease expiry says “Code Atlas access ended. Reconnect to reload this workspace.”
Unavailable membership, refusal, changed source, unknown totals and truncated pages
are visible states, not an empty success. Existing keyboard/focus/high-contrast behavior
and accessible names remain required.

Lifecycle ownership is explicit:

- MainWindow's workspace attach handoff at :181–185 becomes an **awaited** handoff to
  `AttachWorkspaceAsync`, with the NEW reader from the Core-owned view model.
  MainWindowViewModel's existing OpenAsync :234–264 still validates/connects; its new
  optional reader property is not a synthesized workspace security identity.
- At attach replacement, increment the workspace generation and clear old views first,
  cancel pending/active view operations, await `StopViewsAsync`, then await disposal of
  the old workspace reader/lease. Only then install the replacement factory/owner.
  Failure leaves an explicit unavailable host, never the old root's rendered source.
- Add `code-atlas` to `WorkspaceDependentPaneKinds`; invalidate both existing docking
  hosts on replacement. Restored layouts restore only the kind, not tokens or source.
- WPF Loaded/Unloaded event bridges may be `async void` **only** because WPF requires
  that event signature. They catch/report failure and register the underlying Task with
  `AtlasWorkspaceOwner`; owner shutdown can await it. Reparent/unload cancels reader
  operations and detaches the view, but **does not dispose the healthy workspace scope**.
  A subsequent activation uses that same lease unless it is terminal.
- `WorkbenchAdapter.Invalidate` disposes only direct `IDisposable` content; factory output
  may be wrapped in `SurfaceChrome`. `DockHost.Dispose` owns persistence, not query leases.
  Therefore neither is the cleanup authority. The Shell owner keeps view registrations
  independently of wrapper visibility and explicitly drains them.
- Final real-window close uses an awaited `WorkbenchShell.DisposeAsync` bridge, with a
  close-reentry guard and cancellation/cleanup status. The existing synchronous Dispose
  remains compatible for old non-Atlas callers, requests terminal cancellation, and may
  not block on async work. Production MainWindow must await async shutdown **before**
  final resource disposal; add a failing test if it calls only Dispose.
- Release is idempotent on the owning connection. After terminal failure local disposal
  closes the pipe and relies on the awaited connection-ended hook, not a release RPC on
  a broken stream. A release error cannot resurrect a scope.

## 8. Exact file manifest and cohesive dispatch tracks

**Ownership labels below come from the sole §2 lanes and the retained branch-local
Atlas exception. They are not a second ownership register.** Owner 38 conditionally
admits these exact branch-local ceilings after targeted readback clears; the parent records
actual writers, immutable bases and exact paths in sole §2 before source edits.
Core, Shell and Atlas-native counterpart requests remain requests, not acknowledgments.

Prefix `E` means existing at the pinned base; `N` means NEW DESIGN file.
Paths are exact. The implementation grant must name this list; a writer encountering
another required file reports that seam instead of silently editing it.

### Track C — Core-owned authority, membership, transport and render projection

One code writer, **48 leaf calls maximum**, no nested writers. Entry: the recorded
Owner-38 conditional grant and parent targeted Security/Test/C# predicates cleared, with
the membership-specific Security gate retained. The **first eight calls within the 48**
compile the DTO/public-facade seam across Core/Daemon/App and prove semantic-red/golden
directory/parent/reason/bounds/units/local-remote round trips, then commit that seam.
Shell starts only from it. Exit: negative controls red then green, real client/server
persistent journey and bounded cleanup proven. A needed narrow membership qualification
may receive up to ten additional owned source/test leaves only by explicit parent allocation;
it does not expand the 20-file ceiling or reopen architecture.

| Status | Exact file | Delta |
|---|---|---|
| E | `src\AiDe.Core\WorkspaceCore.cs` | Existing NodeContent content-eligibility snapshot only; never filter physical metadata; no new storage |
| E | `src\AiDe.Core\Understanding\AtlasQueryService.cs` | Internal production-grant composition; bounded projection/window access; retain proof composition and Q receipts |
| N | `src\AiDe.Core\Understanding\AtlasWorkspaceReadPolicy.cs` | Separate physical-metadata policy and existing-basis content eligibility; native-root/currentness adapter |
| N | `src\AiDe.Core\Understanding\AtlasReadScopeIssuer.cs` | Scope/grant/connection lifetime and publication gate |
| N | `src\AiDe.Core\Understanding\AtlasGitMembership.cs` | Required clone/linked/nested discovery distinct from source authority; stable membership and owned process cleanup |
| N | `src\AiDe.Core\Understanding\AtlasReadBudget.cs` | Shared scheduler, byte accounting and draining ownership |
| N | `src\AiDe.Core\Understanding\AtlasReaderContracts.cs` | Public render DTOs/ports/enums; lossless kind/parent/reason/bounds and explicit units |
| N | `src\AiDe.Core\Understanding\AtlasReaderProjection.cs` | Explicit native-to-render mapping, local bridge, wire projection; no native model deserialization |
| E | `src\AiDe.Core\Ipc\DaemonEndpoint.cs` | Additive async dispatch and connection-ended hooks; preserve sync behavior |
| E | `src\AiDe.Core\Ipc\IpcServer.cs` | Awaited connection dispatch, single reader/monitor ownership, final serialized-body cap |
| E | `src\AiDe.Core\Ipc\WorkspaceClient.cs` | Add `CreateAtlasReader`, preserving existing query/command client |
| N | `src\AiDe.Core\Ipc\AtlasWorkspaceOperations.cs` | One public registration/lifetime facade, six exact operations and capability discovery |
| N | `src\AiDe.Core\Ipc\AtlasRemoteReader.cs` | Isolated persistent pipe, attempt ownership, read-only lease, terminal abandonment |
| E | `src\AiDe.Daemon\Program.cs` | Call public facade using public parameter types; await opaque lifetime before Core shutdown |
| E | `src\AiDe.App\ViewModels\MainWindowViewModel.cs` | Core-owned typed reader property and existing open/dispose handoff |
| N | `tests\AiDe.Core.Tests\Understanding\AtlasProductionAdmissionTests.cs` | Authority, indexed-read boundary, epoch/policy/root/revocation publication |
| N | `tests\AiDe.Core.Tests\Understanding\AtlasGitMembershipTests.cs` | Native/Git consistency, hostile config, framing and cleanup |
| N | `tests\AiDe.Core.Tests\Understanding\AtlasReaderWireTests.cs` | Golden/red constructor/DTO/local-remote parity; kind/parent/reason/bounds/units, outline independence, escape-size/continuation |
| N | `tests\AiDe.Core.Tests\Understanding\AtlasIpcAdmissionTests.cs` | Real pipes, stateful persistence, cancellation and async/sync dispatch |
| N | `tests\AiDe.Core.Tests\Understanding\AtlasReadBudgetTests.cs` | Multiple scopes, capacity, fairness, accounting and drain ownership |

`CapabilityRegistry.cs`, `WorkspaceOperations.cs`, `IpcClient.cs`, `IpcContract.cs`,
`IpcFraming.cs`, existing identity/codec/F/E/D/S files and existing database models are
**read/reuse only** for this grant. The new Atlas client owns its isolated exchange rather
than changing the existing generic client's cancellation semantics. The server/endpoint
are explicit shared integration surfaces, not grounds to refactor unrelated operations.

### Track S — Shell-owned host lifecycle plus explicitly granted Atlas-native bridge

One code writer, **32 leaf calls maximum**, no nested writers. Entry: C's first-eight-call
DTO/public-facade/golden checkpoint committed, and the parent records its writer/native-file
assignment under Owner 38. Exit: actual MainWindow journey and replacement/close oracles pass.

| Status / lane | Exact file | Delta |
|---|---|---|
| E / Shell | `src\AiDe.App\Workbench\SurfaceContentFactory.cs` | One Architecture-only kind and additive owner dependency |
| E / Shell | `src\AiDe.App\Workbench\WorkbenchShell.cs` | Async attach/dispose, owner lifetime, workspace-dependent kind invalidation |
| E / Shell | `src\AiDe.App\MainWindow.xaml.cs` | Awaited workspace handoff and final shutdown bridge |
| N / Shell | `src\AiDe.App\Workbench\Understanding\AtlasLoadingHost.cs` | Existing-design loading/error container and tracked activation |
| N / Shell | `src\AiDe.App\Workbench\Understanding\AtlasWorkspaceOwner.cs` | Shared healthy lease and awaited view-operation drain |
| E / Atlas-native exception | `src\AiDe.App\Workbench\Understanding\AtlasReaderView.cs` | Add render-port overload; keep proof constructor; adopt returned continuation/receipt |
| E / Atlas-native exception | `tests\AiDe.App.Tests\Workbench\Understanding\AtlasReaderViewTests.cs` | Preserve old route; characterize local/remote render parity |
| N / Shell + granted native scope | `tests\AiDe.App.Tests\Workbench\Understanding\AtlasSharedHostAdmissionTests.cs` | Real composition, navigation, wrapper/reparent and lifetime proof |

`Perspectives.cs`, `PerspectiveMenu.cs`, `MainMenuBuilder.cs`, `CommandPalette.cs`,
`DockHost.cs`, `WorkbenchAdapter.cs`, generic chrome, styles and project files remain
**unchanged**. If the derived-opener test falsifies that exclusion, return the exact
counterexample for a narrow grant; do not invent a second opener registry.

### Dependencies, budgets and shared-surface rules

Graph: **parent targeted Security/Test/C# readback of this erratum → implementation
dispatch under recorded Owner 38 → C first-eight-call committed seam → {C implementation,
S implementation} → integration proof → independent gate disposition**.
NQ1/NQ2 are the narrow membership Security gate within C, not another architecture checkpoint.
No S writer changes C DTOs. C does not change S files; S receives the typed view-model
reader property through the frozen seam. Both agree on `code-atlas`, version 1, opaque
handles, single owner and terminal failures. Their fail-clauses are jointly satisfiable:
C rejects unauthorized/terminal reads; S renders those refusals and never bypasses them.
C's sync-compatibility rule allows S's new awaited entry point and does not ban async
methods repository-wide. S's “one row” rule applies to its Atlas addition, not all future kinds.

At most **two code writers**, total width **four** including read-only reviewers.
Reviewers, if the parent convenes them, each get six calls and a named predicate:
Security/Data = issuer/membership boundary; DS/Test = publication, lifecycle and proof.
They do not re-survey architecture or add files. Join is all required predicates resolved;
partial results remain explicit blockers. A transient tool timeout permits one retry within
the same budget, not a new work wave. No polling counterpart requests.

Each writer has a finite checklist: exact files implemented, named red inputs exercised,
claims recorded. A repair pass must reduce that checklist or a named failing oracle;
at most two repair passes inside the original allowance. Exhaustion is a finding returned
to Conductor, not authorization to extend the allowance. Estimates are **Inferred**:
work ≤ 80 author leaves plus ≤ 12 targeted review leaves; the critical path is C's contract
checkpoint and final integration, so width cannot eliminate it. No unmeasured speedup claim.
Parent owns proof/audit/graph artifacts and assigns their paths before code dispatch;
writers do not create unlisted planning or registry files.

## 9. Test union, red controls and real surface oracle

Triggered union: deterministic unit/contract tests; immutable/golden DTO tests; hostile
boundary/property tests; real native filesystem/Git/pipe integration; cancellation and
publication interleavings; multi-scope resource tests; actual WPF composition/STA rendering;
cross-surface consistency; telemetry readback. Migration and AI eval suites: N/A.
Existing native identity/codec/F/E/D/S/Q and reader proofs are regression evidence,
not substitutes for the new composition path.

| Claim / test home | Red input or fault that must fail | Required happy/adversarial evidence |
|---|---|---|
| Admission / `AtlasProductionAdmissionTests` | Replace currentness with true; use wrong peer, display folder ID, old epoch/policy/root or revoked lease | Zero source opens and no successful publication; an inherited permitted indexed file succeeds |
| Membership / `AtlasGitMembershipTests` | Swap index/ref/root during capture; hostile config starts a helper/hook/fsmonitor/network marker; invalid UTF-8/NUL/duplicates; over-limit output | Ordinary clone, linked-worktree gitfile and nested approved root all succeed on valid fixtures; no executable side effects; outside-root metadata grants no source; NQ1/NQ2 hold; owned process/readers/handles drain |
| Physical visibility / same + admission | Delete a semantic-index row or mark a physical file Git-untracked; grant content merely because its metadata is visible | Folder/file/refused metadata remains policy-correct; no semantic-index inventory filter; content eligibility matches the actual NodeContent basis independently |
| Projection / `AtlasReaderWireTests` | Drop Kind, ParentToken, Reason or one bounds field; serialize raw native parent identity; substitute frame bytes for content bytes; Unknown becomes zero | Local/native→wire→client golden directory/child/refused/withheld fixture preserves every field, all nine source states, unknown totals and zero content bytes for metadata-only responses |
| Public composition / same + C compile checkpoint | Make the public facade accept an internal issuer or give the public loading-host constructor an internal parameter; deserialize invariant-rich Core models directly | Core/Daemon/App compilation and public-facade fixture pass without InternalsVisibleTo; loading-host constructor is internal; DTO constructors round-trip without forging native evidence |
| Pagination / same | Apply PageRequest's 128-row limit to SourceLength; use UI child count for continuation; split surrogate pair; omit bounds dimension or successor receipt | Source request greater than 128 UTF-16 units succeeds within scalar-safe 128 KiB UTF-8/frame bounds; inventory/outline limits remain 1–128 rows; NextOffset/omission/denominator fields agree; Back adopts Q's successor |
| Outline independence / same + query regression | Reject input above the withdrawn smaller outline ceiling; convert outline-only cancellation/budget/unsupported into source failure | Valid input above that old ceiling through the admitted 8 MiB boundary remains eligible; phase-only outline failure preserves independently verified IndexedMatch source and correct source/outline bounds; whole-operation abandonment still forbids publication |
| Framing / same + IPC | Send escaped control-character text plus maximum metadata; exceed entire body by one byte | Real serialization ≤ 1 MiB body with four-byte prefix outside it; over-limit safe error, no partial success |
| Async compatibility / `AtlasIpcAdmissionTests` | Return an unawaited handler task; block the server; allow two concurrent reads | Task barrier proves server awaits; monitor drained before write; old sync op still behaves identically |
| Cancellation / same | Cancel before write, during write/read, after completed A while B runs, and before UI publication | Before-write and completed-A cancellation preserve healthy B; dirty attempt is terminal; no late-A accepted as B on new path |
| Persistent walking / same | Force a new handshake after every operation; reuse old tokens after reconnect | One connection/handshake/scope across inventory/file/member/Back; explicit reconnect readmits/reinventories and rejects all old handles |
| Deadline/revocation / same | Revoke immediately before response commit; expire during work; abandon during EOF monitor | Revocation/abandonment winning commit forbids success; clean drained deadline preserves connection; unknown/undrained result terminates it |
| Global limits / `AtlasReadBudgetTests` | Create several Q instances each below 4/16 but collectively above global caps; release capacity before actual drain | Global counters never exceed ceilings, Busy is typed, canceled queue ticket removed, reservations remain charged until actual disposal |
| Host lifecycle / `AtlasSharedHostAdmissionTests` | Remove the factory row; dispose only SurfaceChrome/DockHost; call sync-only final Dispose; keep old generation callback | Real Architecture opener exists; missing workspace is explicit; root replacement and final close drain; reparent retains healthy scope |
| Native parity / `AtlasReaderViewTests` | Drop one source state/coverage/disclosure when switching to the new port | Existing proof constructor and remote fixture render the same bounds, source text, span, outline and Back result |

The golden red control changes one field or semantic mapping at a time. A text-presence
check is not the oracle: construct directory/refused/unknown fixtures, cross the real
serializer/client projection, and compare the rendered local and remote values. Prove that
each deliberate field loss, raw parent identity, unit mix-up or metadata-as-content byte
mutation fails before accepting the committed seam.

The decisive test uses the **actual MainWindow composition**, not a hand-built reader:
create an isolated approved fixture workspace and real daemon/pipe; open workspace through
the actual view-model handoff; activate the existing Architecture rail/command; invoke the
derived Code Atlas opener; await the loading host; inventory → file → member → Back.
Assert one healthy connection, exact bound source/hash-derived observation and UTF-16
highlight, matching manifest/receipt across server/client/view, preserved directory kind/
parent/reason and requested/effective/returned/omitted bounds, correct NextOffset/unknown
totals, zero inventory content bytes, focus/accessible title, then switch workspace and
close through the real owner.
Coding/Explore must not admit the row. Restore the kind from layout and prove fresh admission.
An injected RootChanged/cancel/revoke must clear rendered source, not merely return an error.

Use an isolated fixture under the granted worktree/Core fixture data directory, not real
customer data or a system temporary directory. Native-root/junction tests must actually
run on the Windows native path. Candidate-only spike assertions are not those tests.
Write observed test counts and assertions, not only an exit code, into the parent-owned
Proof Pack. Do not mark any row Verified until its own red and green were observed.

## 10. Failure, security, privacy and measurement

Patterns: capability-bound lease, explicit anti-corruption/render projection, bounded
producer/consumer scheduler, single-reader state machine, cancellation generation guard,
composition-root ownership. These reuse existing Q/native verification and IPC framing.
There is no AI tier in this deterministic slice.

| Risk / STRIDE lens | Control and failure behavior |
|---|---|
| Spoofing / elevation: foreign handle, folder display ID, proof approval | Actual peer/capability/workspace/epoch + Core-issued scope; lookup and publication currentness; zero source reads on refusal |
| Tampering: native-root/path swap, index/ref race, wire mutation | Pinned native identities, restricted stable capture, existing E/S hash/decoder/span binding, strict DTO validation |
| Repudiation: unclear authority or outcome | Structured admission/terminal/publication events with safe reason codes and epoch; no self-reported success as evidence |
| Information disclosure: unauthorized/out-of-root content or hidden totals | Separate metadata visibility and NodeContent-equivalent content eligibility; native confinement; Unknown/Withheld and full bounds carried intact; no raw authority/native parent identity on wire |
| Denial of service: repeated scopes, parser/Git/output/escape pressure | Global reservations and queues, input/serialization caps, independent deadlines and awaited drain; Busy is visible |
| Lifetime confusion: old cancellation or late view update | Attempt ownership, terminal dirty connection, workspace/view-generation publication gate |

Conditional work-data privacy applies if source contains personal or customer information:
read only already permitted workspace content, for local code understanding, on the local
IPC route. No model call, telemetry upload or other third-party egress. Do not log source,
absolute/relative file paths, capability/scope/receipt tokens, Git stderr or raw identities.
Clear source views on invalidation and release owned process/handle/buffer resources on disposal.
No persistent source cache is added; existing index retention is unchanged.
The purpose does not expand to conversation/document-session access. Administrative Git
metadata discovery outside a nested/linked source root is not permission to read source there.

Normal-path instrumentation, using the repository's existing .NET telemetry facilities:

| Operator question | NEW emitting source and readback |
|---|---|
| Which operation/path and outcome? | `atlas.operation` activity: operation/version, stable outcome/reason, reused-vs-readmitted, epoch; measure on successful and failed real runs |
| How long / queue delay? | `atlas.operation.duration`, `atlas.queue.wait`, `atlas.cleanup.duration` histograms with bounded labels |
| How much capacity/memory/wire? | `atlas.connections`, `atlas.work.active`, `atlas.work.pending`, `atlas.bytes.reserved`, `atlas.response.body_bytes`; distinguish global and instance scope |
| Did scopes end and resources drain? | `atlas.scope.ended`, `atlas.drain.pending`, `atlas.membership.processes`; observe zero outstanding owned resources after fixture close |
| How often do refusal/deadline/reconnect happen? | `atlas.operation.outcomes`, `atlas.connection.terminal`, `atlas.readmission` counters by stable reason only |
| Did the real user route publish? | `atlas.view.transition` activity/event with state and workspace generation, without source/handle values |

Record Git duration/output-byte counts and compiler input/parse duration under the enclosing
operation. Token/spend measurements are **N/A: no model invocation**, not an invented zero
from a missing provider meter. Missing measurements report not-recorded. Test a failed
measurement path so it cannot emit a plausible false zero.

## 11. Choices, exclusions and parent-held gates

Chosen: persistent dedicated Atlas connection, explicit safe projection, Shell-owned async
lifetime and one row-derived opener. Rejected: per-operation reconnect (destroys scope and
receipt continuity); request-correlation cancel protocol (unnecessary new transport contract);
sync-over-async registration (deadlock/cancellation defect); proof approval as authority
(not production permission); direct serialization of native models (constructor/invariant
and authority leakage); cleanup delegated to visual wrappers (wrong owner).

A new typed render port is necessary because the existing native `IndexedMatch` constructor
requires verified native evidence. It is smaller and safer than forging that evidence on
the client or replacing the native reader. Existing source/identity/Q algorithms are reused.
Ordinary, linked-worktree and nested-root repository forms are required in this slice;
their narrow execution/currentness qualification is not an optional widening of scope.

Parent's **targeted** remaining `/design-slice` readback, not a new survey:

| Gate | Exact predicate / unresolved evidence |
|---|---|
| Security / Data | Physical metadata is not semantic-index filtered; content eligibility matches NodeContent without extra implicit grants; native-root and required-form bounded Git execution/currentness pass NQ1/NQ2; no policy DB or approval laundering |
| Distributed Systems | One reader, persistent scope, publication linearization, terminal dirty attempts, awaited end hook, global rather than instance-only limits |
| Test / C# / native UI | Public facade/ports compile across assemblies with opaque lifetime; Kind/ParentToken/reason/full bounds survive semantic-red/golden parity; source UTF-16 and row limits stay distinct; outline failure does not downgrade verified source; awaited Shell close is reachable |
| Owner authoring | Owner 38 conditionally admits §8's exact 20/8-file, 48/32-leaf tracks; C first eight compile/prove/commit the seam; Shell then starts; parent records actual writers/bases and membership qualification allocation; no main integration |

These predicates are **OPEN for parent targeted disposition**, not self-issued PASS records.
Owner 38's conditional grant is recorded; another Owner architecture checkpoint is not
required. Once the erratum's Security/Test/C# predicates clear, dispatch C immediately under
that grant, with NQ1/NQ2 retained as the narrow membership gate. Counterpart silence remains
non-consent. Approved Git configuration, native filesystem races, Roslyn transient heap and
cancellation/drain timings remain unexecuted acceptance evidence with the named oracles,
not invitations to a broader phase or blanket refusal of a required repository form.

## 12. Freeze and evidence custody

Only this Markdown and its HTML view are authored by this integrator. HTML's visible
`pre#canonical-content` contains the **entire** Markdown including YAML frontmatter;
HTML decoding must equal Markdown byte-for-byte after UTF-8 decoding. No hidden alternate
contract, generated interface, stylesheet or dependency is introduced.
Parent owns the full docs graph/audit regeneration and final Proof Pack capture.

Original authoring allowance: eight leaf calls, including claims, instruction reads, patching,
serialization verification, commit and release, committed at `2af3777b9fb50c15c2be980e142cc4cff76af36e`.
Owner 38 adds **four correction leaves, twelve cumulative**, same packet pair and no
implementation edits. The original first grounding command exceeded the
15 KiB output cap and its body was suppressed; later reads were bounded and no contract
claim relies on that suppressed body. This is a recorded authoring-process deviation, not
positive evidence. No product code, test, project, shared registry or main branch was edited.

Correction class → sweep → derive → prevent: a render projection dropped native structure/
bounds; cross-assembly signatures exposed internal types; safety defaults silently narrowed
required scope and confused source/outline units. The sweep covers active §§1–12, both
local/remote mapping paths, the same 28-file manifest and all corresponding test oracles.
Derive both render routes from one lossless projection; keep internal authority behind one
public facade; preserve physical metadata independently from content permission.
Prevent with C's first-eight-call compile plus field-by-field semantic-red/golden tests and
NQ1/NQ2 required-form qualification. Those controls are **specified, not yet executed**.
Parent owns durable defect-register/audit capture outside this integrator's two-file allowance.

The closing commit and clean-state readback identify the corrected immutable packet.
The next action is **Conductor targeted Security/Test/C# readback, then immediate
implementation dispatch under Owner 38**, not another architecture checkpoint, counterpart
request or implementation by this integrator.

---

# Historical checkpoint — superseded proposals and retained evidence

**Everything below is historical.** Its precommit, Conversation/DocumentSession,
unselected-transport and future-spike wording is superseded by §§1–12 and Owner 34–38.
It is retained to preserve the original evidence, source anchors, pins and corrections.
It grants no authority and must not be used as the implementation checklist.

# Shared-host admission checkpoint

**PROPOSED; incomplete admission evidence, not production-design approval.**
The local compatibility merge is now committed at `be3ace85` and joined as `16ea6f73`.
The precommit record below is historical. Owner turns 34-35 supersede its proposed
Conversation-session dependency; see **Superseding authority and transport decisions** below.
The full-content HTML companion is `code-atlas-shared-host-admission.html`.
Coordination authority is [session-contracts.md §2](../collaboration/session-contracts.md).
Conductor owns the final join, gates and audit publication. This delegate does not admit adapters.

## Goal and boundary

Goal: reconcile accepted detached Atlas with observed current main, measure compatibility,
and identify the next Architecture-host seams for Owner and the existing owners.
Done when: pins, build/test evidence, an exact change manifest, sourced production authority
and membership proposal, and one supported handoff receipt exist.
Not in scope: adapter implementation, main integration, another policy/store, model calls,
inspected-project execution, new diagrams, or palette/mockup redesign.
Tier T2; fan-out 0; checkpoint budget 12 leaf calls including two skill loads.
The budget is a reporting boundary, not evidence that all admission criteria passed.
Owner turn 33 released 12 further leaves to the same delegate and two authored files,
24 total, with final commit/readback calls reserved. No second handoff is permitted.
This continuation ends at PRECOMMIT for Conductor's staged-set review, not at a local commit.

## Pins and observed reconciliation

On this checkpoint, local `main`, `origin/main` and compatibility HEAD were all
`6d3e281a049b8baf3696a90b341d3468635ef2f1`.
Branch: `atlas/shared-host-compatibility`.
Tree: `C:\Projects\ai-de-atlas-shared-host-compatibility`.
No fetch or remote freshness claim is made.

The registered tree actually started at main, not at the supplied accepted-proof pin.
`git merge-base --is-ancestor a0ffcee3 HEAD` returned 1; the Atlas spec was absent.
Merging observed main was therefore a no-op. The delegate then merged accepted proof
`1e688ace` with `--no-commit --no-ff`, preserving both lines rather than replacing main.
Owner turn 32 accepts detached reader `a0ffcee3`, join `cc67f7c6`, proof `1e688ace`;
it does not accept integrated E-0, normative Addendum E, or the full programme.

The merge is resolved but uncommitted, pending Conductor's final-set review.
All four documentation conflicts were resolved: main's DC-172–176 were retained against
the empty incoming conflict side; the three site pages differed only in derived figure
values within each conflict. Authored surrounding content was preserved and figures regenerated.
`tools\regenerate-derived.py` passed its conflict, derived-view, figure, register, audit and
registry checks. The unmerged set was empty. No authored existing adapter change was made.
No compatibility commit or test run at a nonexistent commit is claimed.

Correction provenance: Conductor identified its initial `--base HEAD` resolution in the
primary checkout as the cause of the starting pin. Recovery preserved both parents.
This packet does not change the coordination tool or assign that error to the delegate.

## Executed compatibility evidence

All commands ran in the compatibility tree. No adapter source was authored.

| Command / evidence | Observed result | Scope |
|---|---|---|
| `dotnet build spikes\code-atlas-reader-candidate\CodeAtlas.ReaderCandidate.csproj --nologo -v:q` | Build succeeded; 0 warnings, 0 errors; 6.16 seconds reported | Runner and its referenced build graph |
| `dotnet test tests\AiDe.Core.Tests\AiDe.Core.Tests.csproj --filter FullyQualifiedName~Understanding` | TRX: total/executed/passed 272; failed/error/notExecuted 0 | Core Understanding selection |
| `dotnet test tests\AiDe.App.Tests\AiDe.App.Tests.csproj --filter FullyQualifiedName~AtlasReaderViewTests` | TRX: total/executed/passed 45; failed/error/notExecuted 0 | Native reader selection |

Local outputs: `artifacts\atlas-shared-host\build.log`, `core.log`, `app.log`,
`core.trx`, `app.trx`. These are local run evidence, not a newly accepted Proof Pack.
The counters were parsed from TRX, not inferred from process exit.
Conductor independently read the build output and actual TRX counters. Owner turn 33 ruled
on Conductor-reported results after opening §2; Owner did not independently read those outputs.
Relevant code/build inputs have not changed
since that run; no redundant tests were run during the documentation continuation.
The continuation captured 899 tracked source/test/spike/build-input file hashes in
`artifacts\atlas-shared-host\tested-inputs-continuation.json`, SHA-256
`9f277e1cb7b250d884b1f37879f8db7d82352fb7414765e128340617db2451ed`.
This is a post-run identity manifest, not an invented pre-run snapshot. The recorded
no-code-edit history and final byte comparison connect it to the observed test run.
The staged tree before these corrections was `c5c0134c31d9338bf54cdae41e5fd48237f7b49d`.
The final reviewed tree, exact staged paths and both full parent SHAs are separate precommit
evidence emitted after regeneration; changing documentation does not re-date the tests.
No red-first experiment or shared-host render was run here. This is not whole-app,
Architecture reachability, production authority, or security certification.

## Existing-file / owner / minimum-delta / oracle manifest

Ownership below is the current §2 lane assignment, not historical blanket ownership.
Every delta is a proposal; none is authorized by this packet.

| Existing file | Owning party | Existing method contract → minimum proposed delta | Negative/runtime proof required |
|---|---|---|---|
| `src\AiDe.Core\Workbench\Perspectives.cs` | Shell lane | `PerspectiveSet.Architecture` at :58–60 already declares `architecture`, `DockHost`, `perspective.architecture`; reuse unchanged, no fourth perspective or rail registry | Architecture rail/command reaches its existing host; disallowed host rejects the new kind |
| `src\AiDe.App\Workbench\SurfaceContentFactory.cs` | Shell lane | `Kinds` :128 and `Create(Surface)` :287–307 locate a kind, call `row.Build`, name it for accessibility and wrap non-windowed content. Add one Architecture-only native Atlas row and append the admitted query/lifetime dependency; no positional reordering | Missing admission gives an explicit unavailable surface; wrapped content retains accessible title and native reader behavior |
| `src\AiDe.App\Workbench\PerspectiveMenu.cs` | Shell lane | `For(Perspective)` :68–69 delegates to `For(perspective,kinds,catalog)` :75–97, which filters derived entries by the same perspective column. Reuse unchanged; derive the Atlas entry from the kind row | Menu/palette and host admission agree for Architecture, Coding and Explore |
| `src\AiDe.App\Workbench\MainMenuBuilder.cs`, `CommandPalette.cs` | Shell lane | Consumers of the kind/catalog join per §2. No authored delta proposed unless the new-row runtime test proves necessary; their internal methods are not claimed verified here | Existing entry path invokes the same row-derived opener; no second hard-coded Atlas list |
| `src\AiDe.App\Workbench\WorkbenchShell.cs` | Shell lane | Constructor :176–178 creates Coding and Architecture using `surface => _factory.Create(surface)`. `AttachWorkspace(IWorkspaceQueries,string?,IWorkspaceCommands?,string,string,string?)` :591–631 replaces the factory and invalidates `WorkspaceDependentPaneKinds` in both hosts. Add Atlas to that set and carry/revoke the workspace-bound production lease at factory replacement | Open before workspace, attach, switch root and late response cannot retain another workspace's source |
| `src\AiDe.App\Workbench\DockHost.cs` | Shell lane | `Create(Perspective,Func<Surface,FrameworkElement>,IWorkbenchAnnouncer,Action<DockHost,ZoneId>)` :54–85 rejects a non-docking row and creates service/adapter/controller. `AdmissionFor(Perspective)` :92–99 derives kind rules; `Dispose()` :102 only disposes persistence. Reuse admission; do not assume it owns query resources | Coding/Explore reject unauthorized placement; host disposal alone must not be mistaken for lease disposal |
| `src\AiDe.App\Workbench\WorkbenchShell.cs` | Shell lane | `AttachPersistence(string)` :544–557 uses `KnownKinds` and one `LayoutPersistence` per host; `Dispose()` :3001–3042 disposes terminals, host persistence, session documents and watcher resources, not Atlas. Add explicit Atlas lease termination on workspace replacement and shell disposal; keep receipts ephemeral, never restore an old permission from layout | Layout can restore the kind, but fresh admission is required before loading; shutdown/replacement drains or cancels bounded source work |
| `src\AiDe.App\Workbench\WorkbenchAdapter.cs` | Core, not Shell | `Invalidate(IEnumerable<string>)` :140 and rebuilding :199–205 call the factory and dispose only direct old `IDisposable` content. No adapter edit proposed: shell owns the lease independently of the visual wrapper | Hide/reparent/unload is not authorization expiry; replacement and final owner close release the lease even through `SurfaceChrome` |
| `src\AiDe.App\ViewModels\MainWindowViewModel.cs` | Core | `OpenDefaultAsync` :222–223 reads `AIDE_WORKSPACE_ROOT`; `OpenAsync(string?,CancellationToken)` :234–264 checks a directory, connects the daemon, and exposes client queries/commands. Append the admitted Atlas client seam only after the Core authority decision. Its display `WorkspaceId` receives the folder name at :259–260; do not borrow it as the daemon's security identity | Folder-name collisions, absent root, failed connection and wrong workspace cannot issue source admission |
| `src\AiDe.App\MainWindow.xaml.cs` | Shell lane for this horizon, §2:316 | `AttachWorkspace(MainWindowViewModel)` :181–185 forwards queries, data directory, commands and root. Extend that existing handoff only if needed for the typed Atlas client; no rail or palette redesign | Real window → existing Architecture → derived entry → reader proves reachability and no-workspace failure |
| `src\AiDe.App\Workbench\Understanding\AtlasReaderView.cs` | Atlas branch-local native-reader scope; existing adapters remain with their owners | Consume the existing reader, not a substitute or mockup | Existing `tests\AiDe.App.Tests\Workbench\Understanding\AtlasReaderViewTests.cs` plus host-level open/close/restore/late-completion proof |
| `src\AiDe.Core\WorkspaceCore.cs` | Core | `Open(string workspaceId,string rootPath,string dataDirectory,IExtractor?)` :75–115 stores `WorkspaceId` and `RootPath` and constructs projections/dispatch. Reuse that daemon-owned scope; opening the store is not a session/policy/root-reading grant | Revoked/expired/wrong-session/wrong-root/stale-policy requests fail closed before source access |
| `src\AiDe.Core\Understanding\AtlasQueryContracts.cs` | Atlas branch-local query-contract scope | Preserve the existing port unless an independently reviewed production seam requires an additive contract | Inventory/select/receipt restore reject stale bindings and surface bounds |
| `src\AiDe.Core\Understanding\AtlasSourceBinding.cs` | Atlas branch-local source-binding scope | Preserve manifest/file/policy/root/file/hash binding, not a second identity scheme | Change each binding independently; stale data must clear rather than remain plausible |
| `src\AiDe.Core\Ipc\WorkspaceClient.cs` | Core | Implements `IWorkspaceQueries`, `IWorkspaceCommands`, `IWorkspaceDispatch`, `IAsyncDisposable` at :39, not `IAtlasQueries`. `ConnectAsync(string,TimeSpan,CancellationToken)` :61–79 connects and cleans up failures; `DisposeAsync()` :269 closes the IPC client. Add a separate read-only Atlas client surface, not source reads disguised as commands | Cancellation, disconnect, stale epoch and malformed response remain typed failures; no silent reconnect/retry across admission |
| `src\AiDe.Core\Ipc\WorkspaceOperations.cs`, `DaemonEndpoint.cs` | Core | `WorkspaceOperations.Register(DaemonEndpoint,ProjectionService)` :175 registers existing projections; `DaemonEndpoint.Register(string,Func<IpcRequest,IpcPeer,IpcResponse>)` :42 is synchronous and `Invoke` :88 validates before dispatch. No existing Atlas wire contract. An awaitable/cancelable Atlas operation path is a required new reviewed seam, not a blocking callback | No `.Result`/sync-over-async bridge; per-request cancellation, payload/response bounds, authority checks and connection close are tested |
| `src\AiDe.Daemon\Program.cs` | Core | :177–184 opens `WorkspaceCore`, constructs `DaemonEndpoint(workspaceId,new CapabilityRegistry(),_ => core.Store.CoreEpoch)` and registers daemon/workspace operations. Compose production admission, trusted membership and the Atlas service here, only after their contracts are admitted | Daemon shutdown/disconnect/revocation ends appropriate scope; malformed membership never turns into complete empty success |

Verified code anchors: `SurfaceContentFactory.cs:19–45` has optional composition arguments;
`:102–110` declares `SurfaceKind` with `Build`, `Perspectives`, `Instances`, `Entry`, `Windowed`.
Its comments `:90–94` require a non-empty explicit perspective allow-list.
`AtlasQueryContracts.cs:318–322` defines `IAtlasQueries.InventoryAsync(PageRequest, CancellationToken)`,
`SelectAsync(SelectionRequest, CancellationToken)`, and
`RestoreAsync(string issuedReceiptToken, long requestSequence, CancellationToken)`.
`AtlasReaderView.cs:45` accepts `(IAtlasQueries queries, string manifestToken)`;
`:69` exposes `LoadAsync`. This is an actual native consumer, not yet a shared-host integration.
Its `Loaded`/`Unloaded` handlers at :233–240 clear the unloaded flag or advance sequence,
cancel and dispose the request budget; `IsCurrent(long)` :482 rejects late/unloaded results.
The reader does not thereby own the backing service. Factory construction is synchronous:
negotiate the admitted query scope and initial manifest asynchronously before loading the
reader, expose loading/unavailable states, then call `LoadAsync`; never block `Create` with
`.Result`. Preserve the existing native styling and controls.

**Smallest proposed composition:** use the existing Architecture host and row-derived menu;
carry a separate read-only Atlas port through the real daemon/client/view-model handoff;
keep one bounded production scope lease per attached workspace in the shell, independent
of visual reparenting. Closing/hiding the reader cancels that reader's requests; replacing
the workspace or closing its owner terminates the scope. Retention while hidden is deliberate
and requires a measured resource ceiling before admission. Do not modify `DockHost`,
`PerspectiveSet`, the generic controller, Conversation adapters or `WorkbenchAdapter` merely
to route one additional kind. If a later contract requires such an edit, it needs its owner's
specific approval, not this path inventory.

## Production authority and membership: blockers, not guessed contracts

**Verified transport authority, insufficient source authority:** `CapabilityRegistry.cs:6–12`
binds token, connection, process, workspace, epoch and issuance time. `Issue(IpcPeer,string,long)`
at :60 mints it; `Validate(string?,IpcPeer,string,long)` at :88–148 checks live token,
connection, process, workspace and current epoch. `Revoke(string)` :152 removes one token;
`RevokeConnection(string)` :161 removes connection tokens. There is no root, document-session,
policy revision or expiry field in this record, and validation does not perform those checks.
Do not call this a session/policy/root grant or equate epoch with policy revision.

`DaemonEndpoint.OpenWorkspace(IpcRequest,IpcPeer)` :53–84 checks workspace and issues the
capability with the current store epoch. `Invoke` :88–133 separately validates the capability
and the request's epoch before dispatch. `ShellBootstrap.ConnectOrLaunchAsync(string,string,
CancellationToken,string?)` :50 and `IpcPipeName.ForWorkspace(workspacePath)` :57 establish
the existing production daemon route. Root-open and client transport success alone cannot
authorize Atlas source reads.

**Bounded absence evidence:** recursive source scans covered 307 C# files under `src`,
excluding `bin`, `obj` and `Understanding`. The literal token set
`AtlasRootGrant|RecordedRootApproval|IAtlasQueries|AtlasProofComposition` matched zero lines;
the membership set `ls-files|GetTrackedFiles|TrackedFiles|git.*membership|membership.*git`
also matched zero. No additional allowlist was used. This proves absence of those references
in that corpus, not that every conceivable policy mechanism in the repository is absent.
Together with the opened production entry/composition methods, it establishes that the
accepted Atlas contract is not wired into this production route.

**Proposed seam, not an existing API:** Core owns a typed `AtlasProductionAdmission`
exchange and a revocable runtime scope lease. Before coding, Core plus the session-policy
owner must decide the authenticated issuer and map the real document-session identity,
policy revision, approved root and expiry into that exchange. Existing session settings are
Conversation-owned (§2:325); transport `IpcPeer` is not a substitute for that identity.
If no existing approved-root policy covers source reads, Owner must explicitly admit the
missing decision surface; a generated factory cannot grant itself authority. The lease
borrows daemon workspace/root identity, preserves the accepted tuple codec, and is checked
on inventory/select/restore and invalidated on scope or policy change. It is ephemeral,
not a second policy database. No `callback => true`, proof-only approval factory,
external JSON approval or `RecordedRootApproval` may substitute for it.

`spikes\code-atlas-reader-candidate\Program.cs:134–146` obtains `Membership(...)`,
passes paths/completeness/reason to `AtlasProofComposition.CreateForApprovedRootAsync`,
constructs `AtlasReaderView`, and calls `LoadAsync`.
This establishes the **proof composition**, not production authority.
Its lines `158–162` render membership-unavailable status.
`AtlasQueryService.cs:11–24` explicitly declares `RecordedRootApproval` and the
proof factory accepting `Func<RecordedRootApproval,bool>` plus membership data.
`AtlasProofHandle.Dispose()` :37 owns the query service. This seam must not be renamed
“production” while retaining caller-supplied authority.

**Proposed trusted membership seam:** a daemon-owned, admitted-root-only Git snapshot
operation, separated from query projection. There is no production producer in the bounded
scan above. Reuse the proof's verified mechanisms, not its authorization:
`Program.cs:660–687` checks a pinned Git version, expected HEAD, clean status,
NUL-terminated strict UTF-8 `ls-files --cached -z` output, child-path validity and
case-insensitive uniqueness. Failure returns null membership, `Complete=false`, a reason.
`:690–725` sets an explicit executable/working directory, disables shell execution,
clears `GIT_*`, blocks hooks/helper/network-related settings, uses argument lists and a
10-second budget, and bounds stdout to 2 MiB and stderr to 16 KiB. These are proof bounds,
not accepted production limits. The production contract must additionally prove consistency
across HEAD/index changes during capture and process termination/drain on cancellation.
No inspected-project execution, recursive submodule work or untrusted executable selection.

Proposed minimum model: borrow existing workspace, repository, root and session identities;
use the accepted length-prefixed UTF-8/Base64 tuple codec, not the superseded JSON/hash-ID
proposal. Manifest and receipt state are bounded and ephemeral; introduce no second policy
or durable store. A production composition must supply membership for the approved root
and pinned repository state with explicit completeness. Unknown membership must remain
unknown, never become an empty complete inventory. Confirm limits, cancellation, changed
HEAD/index behavior and safe Git execution from the actual trusted producer before authoring.

## End-to-end reach and required measurements

Required trace: existing authority/store → identity and binding model → inventory/source
service → query projection (and wire only if needed) → client port → native reader →
host routing, restoration and selection compute readers. Source/file content never grants
authority. Transport currentness is traced above. The source-read policy issuer and its currentness
reader are not implemented on the opened production path; the proposed admission exchange
is a required decision, not evidence that this gap has disappeared.

Required invariants: explicit unavailable/error/unknown bounds; cancel and stale-completion
suppression; revocation/expiry checked on normal requests and restore; receipt replay bounded
to the current scope; deterministic selection; disposal releases handles and subscriptions;
no inspected-project execution; redaction of root paths, source text, credentials and tokens
from diagnostic output. Cross-surface tests must use one seeded scope and compare factory
admission, host state, reader selection and current authority, not isolated expectations.

Operator questions and proposed sources, **not observed production telemetry**:
request latency and volume from bounded query completion events; failure/cancel/currentness
reason from stable outcomes; visited/skipped/truncated counts from inventory bounds;
active handles/receipts and close completion from the owning lifetime.
No raw path or token metric labels. Missing measurement is “not recorded,” never zero.
No provider calls are admitted, so model spend is not part of this checkpoint.

## Proposed test manifest for the next admitted slice

These are proposed tests, not files added or tests run here. Keep the existing
`tests\AiDe.Core.Tests\Understanding\AtlasQueryServiceTests.cs` and
`tests\AiDe.App.Tests\Workbench\Understanding\AtlasReaderViewTests.cs` as regression controls.
No existing green test is relabeled as production-host proof.

| Proposed file / owner to admit | Failing input / runtime oracle |
|---|---|
| `tests\AiDe.Core.Tests\Understanding\AtlasProductionAdmissionTests.cs` — Core with Conversation review of document-session policy | A valid IPC capability with no source-read grant still refuses; vary root, document session, policy revision, expiry, epoch and revocation independently; zero source opens on each refusal |
| `tests\AiDe.Core.Tests\Understanding\AtlasGitMembershipTests.cs` — Core | Wrong executable, unsafe environment/path, invalid UTF-8/NUL framing, duplicate paths, output overflow, timeout, canceled process, dirty or changing HEAD/index: bounded typed unavailable, never complete empty success; prove process/stream cleanup |
| `tests\AiDe.Core.Tests\Understanding\AtlasIpcAdmissionTests.cs` — Core | Real client/server boundary: malformed/oversize request and response, wrong connection/process/workspace/epoch, disconnect/cancel and restore after revocation; no blocked synchronous server or silent identity retry |
| `tests\AiDe.App.Tests\Workbench\Understanding\AtlasSharedHostAdmissionTests.cs` — Shell and Atlas-native owner | Real window/composition root: Architecture rail → derived opener → native reader; no-workspace loading/error; Coding/Explore refusal; same-scope selection; layout restore requires fresh admission; switch/close/unload/late completion proves cleared source and bounded resources |

One seeded scope must agree across admission, membership, manifest, wire, reader and restored
selection. Independent Security/Test review must demand red-first or fault-injection evidence
for these controls. Pure pattern scans and the present 317 selected green tests are insufficient.

## Handoff, acceptance and remaining full-design checklist

Existing requests remain open unless their supported request records prove otherwise:
`req-01M2B86TXF7SHG61B31P4H4173`, `req-01M2BGHNCM6WRD4ZZMBBFEEB4K`.
Recipient: `conductor-addendum-c`. The one supported `coord request add` attempt completed
and returned `{"id":"req-01M2CAXKH01J8SMQV1HBCCAN08","status":"open"}`.
The native record is the delivery receipt; its local captured output is
`artifacts\atlas-shared-host\handoff-receipt.txt`. It names the tree, parents, selected test
results, concrete host files and source-authority/membership question. Conductor read the
native OPEN record; Owner preserved the non-consent boundary. No second attempt or polling occurred.
Delivery is not read acknowledgment; acknowledgment is not scope agreement. Counterpart
acknowledgment and adapter permission remain unverified. Return the corrected packet and
precommit tree to Owner; no human relay is required.

## Graph findings and record correction

Direct `docs-graph.py derive` classified both prior findings:

1. **Packet-owned:** this Markdown lacked required frontmatter `summary`. The current
   correction added it. The final direct `docs-graph.py derive` returned only the
   ruling-49 finding below; the packet's missing-summary finding is resolved.
2. **Outside this scope:** `docs/notes/front-door-ruling-49.md` links to missing
   `proof-conductor-front-door`. Leave that existing artifact unchanged and report the
   dangling link to Conductor; do not invent its target or edit another owner's note.

The initial `artifacts\atlas-shared-host\regenerate.log` is historical evidence of two
findings, not the corrected state. The final-seal receipts are
`artifacts\atlas-shared-host\regenerate-final-precommit.log` and
`artifacts\atlas-shared-host\derive-final-precommit.log`.

The initial packet also described already-resolved conflicts and an already-completed
handoff in the future tense. This continuation corrects those stale records explicitly
(REC-A shape). Controls here are source-state readback, full Markdown/HTML content parity
and the graph's observed missing-summary finding. Register/audit publication belongs to
Conductor; this delegate has no grant to edit the shared register for the correction.

Unmet: Owner/Core/Conversation admission of the new source-read authority decision and typed
lease; production membership snapshot atomicity, limits and cleanup proof; the new awaitable
Atlas IPC path, including method-level transport cancellation integration before coding;
actual Architecture sidebar/menu/render and resource-lifetime proof; failure-mode dispositions;
security/privacy/data/async and independent Test Architect gates; performance measurements;
Conductor precommit review and final local compatibility commit; counterpart agreement.
The existing transport and host method trace narrows these decisions; it does not clear them.
No production design-completion claim is made.

Full Code Atlas remains unfinished: sidebar, graph/file-tree pivot, public-API/domain/class/
sequence/layered/Azure views, comparison/decision lineage and governed AI.
The next step is legitimate Architecture-host admission, not a distraction into later views.

| Completed | Remaining | Best next action |
|---|---|---|
| Observed parents; resolved merge; runner build; 272 Core and 45 native tests; production transport and host method trace; delivered request receipt | New source-authority/membership/awaitable-IPC decisions and their tests; external dangling link; precommit review and local commit | Conductor reviews exact staged paths/tree/parents, then Owner decides the smallest admitted adapter scope |

## Superseding authority and transport decisions

**Owner turns 34-35:** Atlas inherits the existing approved workspace-read basis, with
additional workspace/connection-scoped Atlas admission. It does **not** require a Conversation/
DocumentSession, another human approval for already-permitted content, another policy database,
or proof-JSON authority. Broader roots/content still require their applicable approval.

The Conductor opened the existing production route beyond the Atlas-specific token scan:
`WorkspaceOperations.NodeContent` -> `ProjectionService.NodeContent` -> stored declaring
assertion/scope location -> `ResolveWithinWorkspace` -> bounded source read, consumed through
`CoreNodeContentSource` and `IWorkspaceQueries`. Security and Data revised their earlier
interpretation in light of that path. Zero Atlas references meant an unwired new port, not
absence of an existing source-read basis.

Core derives peer/workspace/epoch from the authenticated connection and restricts the Atlas
lease to its permitted root/content. Native-root binding, policy generation, expiry, revocation
and connection/workspace termination remain explicit strengthening requirements. App receives
opaque handles. An Atlas lifetime token is not a Coding conversation aggregate.

The focused Distributed Systems review identifies a **source-supported, not yet executed**
cancel/reuse hazard: `IpcClient.ExchangeAsync` can release its serial gate after a canceled
read without closing the pipe; `IpcResponse` has no request identifier. A late A response may
then be consumed as B. The server currently handles requests synchronously and does not observe
wire cancellation while a handler runs. A Task-returning client is not async server dispatch.

Owner admits a 16-leaf synthetic IPC comparison, with four independent DS/Test review calls,
in `atlas/ipc-contract` at verified `16ea6f73`. It tests isolated connection abort/fresh handshake
first, without selecting that policy for production. It must observe before/partial/after
write/read/completion cancellation, late-response identity, stale cancellation ownership,
awaited server execution, bounded server work, cleanup/revocation, reconnect pressure and
serialized page-plus-metadata limits. No existing Core/Shell edits, live daemon, user data,
sync-over-async bridge, token-none substitution or new correlation protocol are admitted.

Only the next Owner ruling may freeze production lease/cancellation semantics or grant adapter
files. The recorded counterpart requests remain non-consent. An executed hard-floor violation
in the existing production path is escalated to the human; the spike may not trade it away.
