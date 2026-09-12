---
id: api-aide-core-understanding
title: "API: AiDe.Core.Understanding"
type: api
status: current
owner: "@timianmalloo"
phase: "0"
tags: [api, reference, generated]
links:
  - { to: architecture, rel: documents }
review-by: 2027-09-02
summary: >-
  Extracted public surface of AiDe.Core.Understanding: 43 types, 199 members, 14% carrying a summary doc comment.
---

# API: `AiDe.Core.Understanding`

**43 public types · 199 public members · 14% documented.**

> Extracted from the source by `tools/api-reference.py`. Prose here is the code's own
> `///` comment, never written for the reference; a member with no comment is listed as a
> gap rather than given invented text. The extractor is a lexical reader, not a compiler:
> it does not resolve generics, partial classes across files, or conditional compilation.

## `AtlasDirectoryEnumerator`

*class* — `AtlasDirectoryEnumerator.cs`

Ordinary local Windows metadata only. Detected reparse points are excluded; this is not
a never-open-outside race guarantee. The trusted composition must supply a live grant check.
I/O is synchronous: the Task-shaped port does not schedule a worker or make native opens cancelable.

| Member | Summary |
|---|---|
| `Task<DirectoryObservation> EnumerateAsync(` | **(gap)** |

## `AtlasIdentity`

*class* — `AtlasIdentity.cs`

Revision-independent Atlas logical identity for a source-declared type or supported source
member. Opaque caller tokens are preserved ordinally; the encoding never normalizes text.

| Member | Summary |
|---|---|
| `string Value { get; }` | **(gap)** |
| `AtlasIdentity ForType(` | **(gap)** |
| `AtlasIdentity ForMember(` | **(gap)** |
| `bool Equals(AtlasIdentity? other)` | **(gap)** |
| `bool Equals(object? obj)` | **(gap)** |
| `int GetHashCode()` | **(gap)** |
| `string ToString()` | **(gap)** |
| `bool operator ==(AtlasIdentity? left, AtlasIdentity? right)` | **(gap)** |
| `bool operator !=(AtlasIdentity? left, AtlasIdentity? right)` | **(gap)** |

## `AtlasIdentityCodec`

*class* — `AtlasIdentityCodec.cs`

Encodes Atlas identity tuples as UTF-8 byte-length-prefixed Base64 components.
Components are ordinal and are never trimmed, case-folded, or Unicode-normalized.

| Member | Summary |
|---|---|
| `string EncodeComponents(params string[] components)` | Encodes supplied components without changing ordinal values. |
| `string ForFile(string workspace, string root, string relativePath)` | Builds a domain-tagged file tuple for an observed file. |
| `string ForNativeObject(AtlasObjectIdentity identity)` | Builds the opaque binding token for a native object identity in this Windows candidate. |
| `string ForCompilationScope(` | Builds a compilation-scope tuple. File-limited scope uses explicit absence tokens for project and target framework so it cannot compare equal to a later project compilation. |

## `AtlasCompilationContextKind`

*enum* — `AtlasIdentityCodec.cs`

Closed set of compilation contexts admitted by the Atlas identity codec.

## `AtlasCompilationScope`

*class* — `AtlasIdentityCodec.cs`

Compilation context identity plus display facts. File-limited contexts report project and TFM as
not established; an unknown profile has no logical identity and supports observation navigation only.

| Member | Summary |
|---|---|
| `AtlasCompilationContextKind Kind { get; }` | **(gap)** |
| `string ObservationKey { get; }` | **(gap)** |
| `string? LogicalIdentity { get; }` | **(gap)** |
| `string ProjectDisplay { get; }` | **(gap)** |
| `string TargetFrameworkDisplay { get; }` | **(gap)** |
| `string ConfigurationOrProfileToken { get; }` | **(gap)** |
| `AtlasCompilationScope ForFileLimited(string workspace, string root, string? profileToken)` | **(gap)** |
| `AtlasCompilationScope ForSuppliedProjectCompilation(` | **(gap)** |

## `AtlasInventoryMembershipMode`

*enum* — `AtlasInventory.cs`

*No doc comment on this type.* **(gap)**

## `AtlasInventoryPolicy`

*class* — `AtlasInventory.cs`

Trusted composition supplies membership; this component never discovers Git state or runs hooks.

| Member | Summary |
|---|---|
| `AtlasInventoryMembershipMode Mode { get; }` | **(gap)** |
| `string Reason { get; }` | **(gap)** |
| `bool IsComplete { get; private init; } = true` | **(gap)** |
| `AtlasInventoryPolicy NonGit(string reason)` | **(gap)** |
| `AtlasInventoryPolicy UnavailableMembership(string reason)` | **(gap)** |
| `AtlasInventoryPolicy KnownMembership(AtlasRootGrant grant, IEnumerable<string> paths, bool complete, string reason)` | Immutable, ordinal path membership bound to the exact issued grant instance. Completeness describes the supplied membership snapshot, not physical coverage. Ancestors of authorized paths are metadata members too. Miss… |

## `AtlasInventory`

*class* — `AtlasInventory.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `Task<AtlasManifest> BuildManifestAsync(` | **(gap)** |

## `AtlasCompletionState`

*enum* — `AtlasManifest.cs`

*No doc comment on this type.* **(gap)**

## `AtlasDirectoryEntryKind`

*enum* — `AtlasManifest.cs`

*No doc comment on this type.* **(gap)**

## `AtlasFileClassification`

*enum* — `AtlasManifest.cs`

*No doc comment on this type.* **(gap)**

## `AtlasFileAvailability`

*enum* — `AtlasManifest.cs`

*No doc comment on this type.* **(gap)**

## `AtlasDenominatorState`

*enum* — `AtlasManifest.cs`

*No doc comment on this type.* **(gap)**

## `AtlasSourceObservationStatus`

*enum* — `AtlasManifest.cs`

*No doc comment on this type.* **(gap)**

## `AtlasDeclarationKind`

*enum* — `AtlasManifest.cs`

*No doc comment on this type.* **(gap)**

## `AtlasDeclarationRole`

*enum* — `AtlasManifest.cs`

*No doc comment on this type.* **(gap)**

## `AtlasObjectIdentity`

*class* — `AtlasManifest.cs`

Native filesystem identity: volume serial and file index only.

| Member | Summary |
|---|---|
| `AtlasObjectIdentity(string volumeSerial, string fileIndex)` | **(gap)** |
| `string VolumeSerial { get; }` | **(gap)** |
| `string FileIndex { get; }` | **(gap)** |
| `bool Equals(AtlasObjectIdentity? other)` | **(gap)** |
| `bool Equals(object? obj)` | **(gap)** |
| `int GetHashCode()` | **(gap)** |

## `struct`

*record* — `AtlasManifest.cs`

UTF-16 span where null means absent; zero length is still a present span.

| Member | Summary |
|---|---|
| `AtlasTextSpan(int start, int length)` | **(gap)** |
| `int Start { get; }` | **(gap)** |
| `int Length { get; }` | **(gap)** |
| `int End` | **(gap)** |

## `AtlasBounds`

*class* — `AtlasManifest.cs`

Request/response bounds with an explicit denominator state; unknown is never encoded as zero.

| Member | Summary |
|---|---|
| `AtlasBounds(` | **(gap)** |
| `int RequestedLimit { get; }` | **(gap)** |
| `int EffectiveLimit { get; }` | **(gap)** |
| `int ReturnedRows { get; }` | **(gap)** |
| `long ReturnedBytes { get; }` | **(gap)** |
| `long? TotalCount { get; }` | **(gap)** |
| `AtlasDenominatorState TotalState { get; }` | **(gap)** |
| `string? OmissionReason { get; }` | **(gap)** |
| `string? LimitingDimension { get; }` | **(gap)** |

## `AtlasRootGrant`

*class* — `AtlasManifest.cs`

Core-issued immutable grant; only Core can construct it and expected root identity is required.

| Member | Summary |
|---|---|
| `string GrantVersion { get; }` | **(gap)** |
| `string WorkspaceToken { get; }` | **(gap)** |
| `string RootToken { get; }` | **(gap)** |
| `string PolicyToken { get; }` | **(gap)** |
| `string SessionToken { get; }` | **(gap)** |
| `string ApprovedAbsoluteRoot { get; }` | **(gap)** |
| `AtlasObjectIdentity ExpectedNativeRootIdentity { get; }` | **(gap)** |
| `DateTimeOffset ExpiresAt { get; }` | **(gap)** |

## `AtlasDirectoryEntry`

*class* — `AtlasManifest.cs`

Observed directory entry metadata. Missing identity/counts remain nullable absence.

| Member | Summary |
|---|---|
| `AtlasDirectoryEntry(string relativePath, AtlasDirectoryEntryKind kind, AtlasObjectIdentity? objectIdentity, long? lengthBytes, long? linkCount, string? reason)` | **(gap)** |
| `string RelativePath { get; }` | **(gap)** |
| `AtlasDirectoryEntryKind Kind { get; }` | **(gap)** |
| `AtlasObjectIdentity? ObjectIdentity { get; }` | **(gap)** |
| `long? LengthBytes { get; }` | **(gap)** |
| `long? LinkCount { get; }` | **(gap)** |
| `string? Reason { get; }` | **(gap)** |

## `DirectoryObservation`

*class* — `AtlasManifest.cs`

Directory observation with expected/observed root identities and immutable ordered entries.

| Member | Summary |
|---|---|
| `DirectoryObservation(string observationKey, AtlasObjectIdentity expectedRootIdentity, AtlasObjectIdentity? observedRootIdentity, AtlasCompletionState completion, long sequence, AtlasBounds bounds, IEnumerable<AtlasDirectoryEntry> entries)` | **(gap)** |
| `string ObservationKey { get; }` | **(gap)** |
| `AtlasObjectIdentity ExpectedRootIdentity { get; }` | **(gap)** |
| `AtlasObjectIdentity? ObservedRootIdentity { get; }` | **(gap)** |
| `AtlasCompletionState Completion { get; }` | **(gap)** |
| `long Sequence { get; }` | **(gap)** |
| `AtlasBounds Bounds { get; }` | **(gap)** |
| `ImmutableArray<AtlasDirectoryEntry> Entries { get; }` | **(gap)** |

## `AtlasFileEntry`

*class* — `AtlasManifest.cs`

Manifest file row; file identity is the codec value, not a hash alias.

| Member | Summary |
|---|---|
| `AtlasFileEntry(string fileValue, string relativePath, string parentPathKey, AtlasDirectoryEntryKind kind, AtlasFileClassification classification, AtlasObjectIdentity? observedIdentity, AtlasFileAvailability availability, string? reason)` | **(gap)** |
| `string FileValue { get; }` | **(gap)** |
| `string RelativePath { get; }` | **(gap)** |
| `string ParentPathKey { get; }` | **(gap)** |
| `AtlasDirectoryEntryKind Kind { get; }` | **(gap)** |
| `AtlasFileClassification Classification { get; }` | **(gap)** |
| `AtlasObjectIdentity? ObservedIdentity { get; }` | **(gap)** |
| `AtlasFileAvailability Availability { get; }` | **(gap)** |
| `string? Reason { get; }` | **(gap)** |

## `AtlasSourceObservation`

*class* — `AtlasManifest.cs`

Verified source metadata. The body is request-local only and is not retained here.

| Member | Summary |
|---|---|
| `AtlasSourceObservation(string observationKey, string manifestToken, string fileValue, string policyToken, AtlasObjectIdentity rootIdentity, AtlasObjectIdentity fileIdentity, string canonicalSha256, long byteLength, string decoderId, int decodedUtf16Length, AtlasSourceObservationStatus status, AtlasBounds bounds)` | **(gap)** |
| `string ObservationKey { get; }` | **(gap)** |
| `string ManifestToken { get; }` | **(gap)** |
| `string FileValue { get; }` | **(gap)** |
| `string PolicyToken { get; }` | **(gap)** |
| `AtlasObjectIdentity? RootIdentity { get; }` | **(gap)** |
| `AtlasObjectIdentity? FileIdentity { get; }` | **(gap)** |
| `string? CanonicalSha256 { get; }` | **(gap)** |
| `long? ByteLength { get; }` | **(gap)** |
| `string? DecoderId { get; }` | **(gap)** |
| `int? DecodedUtf16Length { get; }` | **(gap)** |
| `AtlasSourceObservationStatus Status { get; }` | **(gap)** |
| `AtlasBounds Bounds { get; }` | **(gap)** |
| `string? Reason { get; }` | **(gap)** |
| `AtlasSourceObservation Verified(string observationKey, string manifestToken, string fileValue, string policyToken, AtlasObjectIdentity rootIdentity, AtlasObjectIdentity fileIdentity, string canonicalSha256, long byteLength, string decoderId, int decodedUtf16Length, AtlasBounds bounds)` | **(gap)** |
| `AtlasSourceObservation Unavailable(string observationKey, string manifestToken, string fileValue, string policyToken, AtlasBounds bounds, string reason)` | **(gap)** |

## `AtlasDeclaration`

*class* — `AtlasManifest.cs`

Declaration metadata. Logical symbol value can be absent when the compiler identity is unavailable.

| Member | Summary |
|---|---|
| `AtlasDeclaration(string observationKey, string? logicalSymbolValue, string sourceObservationKey, string contextKey, AtlasSourceBinding sourceBinding, AtlasDeclarationKind kind, AtlasDeclarationRole role, string displaySignature, string identifier, AtlasTextSpan identifierSpan, AtlasTextSpan declarationSpan, AtlasTextSpan? bodySpan, string? unresolvedReason)` | **(gap)** |
| `string ObservationKey { get; }` | **(gap)** |
| `string? LogicalSymbolValue { get; }` | **(gap)** |
| `string SourceObservationKey { get; }` | **(gap)** |
| `string ContextKey { get; }` | **(gap)** |
| `AtlasSourceBinding SourceBinding { get; }` | **(gap)** |
| `AtlasDeclarationKind Kind { get; }` | **(gap)** |
| `AtlasDeclarationRole Role { get; }` | **(gap)** |
| `string DisplaySignature { get; }` | **(gap)** |
| `string Identifier { get; }` | **(gap)** |
| `AtlasTextSpan IdentifierSpan { get; }` | **(gap)** |
| `AtlasTextSpan DeclarationSpan { get; }` | **(gap)** |
| `AtlasTextSpan? BodySpan { get; }` | **(gap)** |
| `string? UnresolvedReason { get; }` | **(gap)** |

## `AtlasManifest`

*class* — `AtlasManifest.cs`

In-memory manifest for detached reader producers; no source body or compiler object is retained.

| Member | Summary |
|---|---|
| `AtlasManifest(string token, AtlasRootGrant rootGrant, string directoryObservationKey, IEnumerable<AtlasFileEntry> files, IEnumerable<AtlasSourceObservation> sourceObservations, IEnumerable<AtlasDeclaration> declarations, AtlasCompletionState completion, AtlasBounds bounds)` | **(gap)** |
| `string Token { get; }` | **(gap)** |
| `AtlasRootGrant RootGrant { get; }` | **(gap)** |
| `string DirectoryObservationKey { get; }` | **(gap)** |
| `ImmutableArray<AtlasFileEntry> Files { get; }` | **(gap)** |
| `ImmutableArray<AtlasSourceObservation> SourceObservations { get; }` | **(gap)** |
| `ImmutableArray<AtlasDeclaration> Declarations { get; }` | **(gap)** |
| `AtlasCompletionState Completion { get; }` | **(gap)** |
| `AtlasBounds Bounds { get; }` | **(gap)** |

## `EnumerationLimits`

*class* — `AtlasQueryContracts.cs`

Directory enumeration limits. Candidate ceilings are policy defaults, not measured guarantees.

| Member | Summary |
|---|---|
| `EnumerationLimits(int maxEntries, int maxDepth, int maxDescriptors, TimeSpan timeout)` | **(gap)** |
| `int MaxEntries { get; }` | **(gap)** |
| `int MaxDepth { get; }` | **(gap)** |
| `int MaxDescriptors { get; }` | **(gap)** |
| `TimeSpan Timeout { get; }` | **(gap)** |
| `EnumerationLimits CandidateDefault { get; } = new(25_000, 64, 128, TimeSpan.FromSeconds(30))` | **(gap)** |

## `PageRequest`

*class* — `AtlasQueryContracts.cs`

Bounded page request for Atlas query ports.

| Member | Summary |
|---|---|
| `int MaxLimit = 128` | **(gap)** |
| `PageRequest(int offset, int limit)` | **(gap)** |
| `int Offset { get; }` | **(gap)** |
| `int Limit { get; }` | **(gap)** |

## `SourceProjectionState`

*enum* — `AtlasQueryContracts.cs`

Closed source projection states for selection results.

## `SelectionCoverage`

*class* — `AtlasQueryContracts.cs`

Coverage denominator; unknown and withheld carry no numeric fiction.

| Member | Summary |
|---|---|
| `AtlasDenominatorState State { get; }` | **(gap)** |
| `double? Value { get; }` | **(gap)** |
| `string? Reason { get; }` | **(gap)** |
| `SelectionCoverage Known(double value)` | **(gap)** |
| `SelectionCoverage Unknown(string reason)` | **(gap)** |
| `SelectionCoverage Withheld(string reason)` | **(gap)** |

## `OutlineDeclaration`

*class* — `AtlasQueryContracts.cs`

One addressable declaration in a selection outline.

| Member | Summary |
|---|---|
| `OutlineDeclaration(string observationKey, string displayName, AtlasDeclarationKind kind, AtlasTextSpan span)` | **(gap)** |
| `string ObservationKey { get; }` | **(gap)** |
| `string DisplayName { get; }` | **(gap)** |
| `AtlasDeclarationKind Kind { get; }` | **(gap)** |
| `AtlasTextSpan Span { get; }` | **(gap)** |

## `SelectionOutline`

*class* — `AtlasQueryContracts.cs`

Structured, addressable outline. Empty outlines are valid.

| Member | Summary |
|---|---|
| `SelectionOutline(IEnumerable<OutlineDeclaration> declarations)` | **(gap)** |
| `ImmutableArray<OutlineDeclaration> Declarations { get; }` | **(gap)** |

## `SourceTextPage`

*class* — `AtlasQueryContracts.cs`

Projected source page with UTF-16 page and highlight spans.

| Member | Summary |
|---|---|
| `SourceTextPage(string text, AtlasTextSpan pageSpan, IEnumerable<AtlasTextSpan> highlights)` | **(gap)** |
| `string Text { get; }` | **(gap)** |
| `AtlasTextSpan PageSpan { get; }` | **(gap)** |
| `ImmutableArray<AtlasTextSpan> Highlights { get; }` | **(gap)** |

## `SourceProjection`

*class* — `AtlasQueryContracts.cs`

Source projection union. Only IndexedMatch may carry source text and highlights.

| Member | Summary |
|---|---|
| `SourceProjectionState State { get; }` | **(gap)** |
| `string ObservationKey { get; }` | **(gap)** |
| `AtlasSourceBinding? ExpectedBinding { get; }` | **(gap)** |
| `string? DecoderId { get; }` | **(gap)** |
| `SourceTextPage? Page { get; }` | **(gap)** |
| `SourceProjection IndexedMatch(AtlasSourceObservation observation, AtlasSourceBinding expectedBinding, string decoderId, SourceTextPage page)` | **(gap)** |
| `SourceProjection Changed(string observationKey)` | **(gap)** |
| `SourceProjection Unavailable(string observationKey)` | **(gap)** |
| `SourceProjection Unavailable(string observationKey, SourceTextPage page)` | **(gap)** |
| `SourceProjection Unverifiable(string observationKey)` | **(gap)** |
| `SourceProjection UnsupportedEncoding(string observationKey)` | **(gap)** |
| `SourceProjection TooLargeToVerify(string observationKey)` | **(gap)** |
| `SourceProjection ReadUnstable(string observationKey)` | **(gap)** |
| `SourceProjection Refused(string observationKey)` | **(gap)** |
| `SourceProjection Canceled(string observationKey)` | **(gap)** |

## `InventoryPage`

*class* — `AtlasQueryContracts.cs`

Inventory page returned by query ports.

| Member | Summary |
|---|---|
| `InventoryPage(PageRequest request, AtlasBounds bounds, IEnumerable<AtlasFileEntry> files)` | **(gap)** |
| `PageRequest Request { get; }` | **(gap)** |
| `AtlasBounds Bounds { get; }` | **(gap)** |
| `ImmutableArray<AtlasFileEntry> Files { get; }` | **(gap)** |

## `SelectionRequest`

*class* — `AtlasQueryContracts.cs`

Selection input. It names manifest/file/declaration observations and cannot issue grant authority.

| Member | Summary |
|---|---|
| `SelectionRequest(string manifestToken, string fileValue, string? declarationObservationKey, long requestSequence)` | **(gap)** |
| `string ManifestToken { get; }` | **(gap)** |
| `string FileValue { get; }` | **(gap)** |
| `string? DeclarationObservationKey { get; }` | **(gap)** |
| `long RequestSequence { get; }` | **(gap)** |

## `SelectionProjection`

*class* — `AtlasQueryContracts.cs`

Selection output with coverage, bounds, limitations, source status and Core-issued receipt.

| Member | Summary |
|---|---|
| `SelectionProjection(string receiptToken, string manifestToken, string fileValue, long generation, SelectionOutline outline, SourceProjection source, AtlasBounds bounds, SelectionCoverage coverage, IEnumerable<string> limitations)` | **(gap)** |
| `string ReceiptToken { get; }` | **(gap)** |
| `string ManifestToken { get; }` | **(gap)** |
| `string FileValue { get; }` | **(gap)** |
| `long Generation { get; }` | **(gap)** |
| `SelectionOutline Outline { get; }` | **(gap)** |
| `SourceProjection Source { get; }` | **(gap)** |
| `AtlasBounds Bounds { get; }` | **(gap)** |
| `SelectionCoverage Coverage { get; }` | **(gap)** |
| `ImmutableArray<string> Limitations { get; }` | **(gap)** |

## `IAtlasDirectoryEnumerator`

*interface* — `AtlasQueryContracts.cs`

Port for directory enumeration under a pre-issued root grant.

## `IAtlasQueries`

*interface* — `AtlasQueryContracts.cs`

Read-only Atlas query port. Implementations revalidate receipts and never trust caller bindings.

## `AtlasSourceBindingMismatch`

*enum* — `AtlasSourceBinding.cs`

*No doc comment on this type.* **(gap)**

## `AtlasSourceBinding`

*class* — `AtlasSourceBinding.cs`

Manifest-bound source observation identity. The five source tokens are opaque ordinal values;
the content hash is the canonical `sha256:` plus 64 lowercase hexadecimal characters.

| Member | Summary |
|---|---|
| `string ManifestIdentity { get; }` | **(gap)** |
| `string ManifestFileIdentity { get; }` | **(gap)** |
| `string PolicyIdentity { get; }` | **(gap)** |
| `string RootIdentity { get; }` | **(gap)** |
| `string FileIdentity { get; }` | **(gap)** |
| `string ContentHash { get; }` | **(gap)** |
| `AtlasSourceBinding Create(` | **(gap)** |
| `bool Matches(` | **(gap)** |
| `AtlasSourceBindingMismatch CompareTo(` | **(gap)** |
| `bool Equals(AtlasSourceBinding? other)` | **(gap)** |
| `bool Equals(object? obj)` | **(gap)** |
| `int GetHashCode()` | **(gap)** |
| `bool operator ==(AtlasSourceBinding? left, AtlasSourceBinding? right)` | **(gap)** |
| `bool operator !=(AtlasSourceBinding? left, AtlasSourceBinding? right)` | **(gap)** |

## `CSharpDeclarationObservationResult`

*class* — `CSharpDeclarationObservation.cs`

*No doc comment on this type.* **(gap)**

| Member | Summary |
|---|---|
| `CSharpDeclarationObservationResult(IEnumerable<AtlasDeclaration> declarations, AtlasCompletionState completion, AtlasBounds bounds, IEnumerable<string> limitations)` | **(gap)** |
| `ImmutableArray<AtlasDeclaration> Declarations { get; }` | **(gap)** |
| `AtlasCompletionState Completion { get; }` | **(gap)** |
| `AtlasBounds Bounds { get; }` | **(gap)** |
| `ImmutableArray<string> Limitations { get; }` | **(gap)** |

## `CSharpDeclarationObservation`

*class* — `CSharpDeclarationObservation.cs`

*No doc comment on this type.* **(gap)**
