using System.Text.Json.Serialization;

namespace AiDe.Core.Understanding;

public enum AtlasBoundsDimension
{
    InventoryRows,
    OutlineRows,
    SourceUtf16CodeUnits,
    SourceUtf8ContentBytes,
    SerializedMetadataBytes,
    Policy,
}

public enum AtlasOutlineState
{
    Available,
    Canceled,
    BudgetExceeded,
    Unsupported,
    Unavailable,
    Refused,
}

public enum AtlasConnectionEndReason
{
    Released, Disconnected, Abandoned, ProtocolViolation, Revoked, Expired, Shutdown,
}

public sealed record AtlasCapabilitiesRequestDto(int[] SupportedVersions);
public sealed record AtlasCapabilitiesDto(
    int[] Versions, string[] Features, int MaxFrameBodyBytes, int MaxPageTextUtf8Bytes, int MaxPageItems);
public sealed record AtlasAdmitRequestDto(int Version, long ExpectedCoreEpoch);
public sealed record AtlasAdmitDto(
    int Version, string ScopeToken, string InitialManifestToken, long CoreEpoch, DateTimeOffset ExpiresAt);
public sealed record AtlasInventoryRequestDto(
    int Version, string ScopeToken, long ExpectedCoreEpoch, string ManifestToken, int Offset, int Limit);
/// <summary>Structural metadata is explicit opt-in; the remote reader omits it when the peer lacks the feature.</summary>
public sealed record AtlasSelectRequestDto(
    int Version, string ScopeToken, long ExpectedCoreEpoch, string ManifestToken, string FileToken,
    string? DeclarationToken, int SourceOffset, int SourceLength, int OutlineOffset, int OutlineLimit,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? StaticStructure = null);
public sealed record AtlasRestoreRequestDto(
    int Version, string ScopeToken, long ExpectedCoreEpoch, string ReceiptToken);
public sealed record AtlasReleaseRequestDto(int Version, string ScopeToken, long ExpectedCoreEpoch);
public sealed record AtlasReleasedDto(int Version, bool Released);
public sealed record AtlasInventoryPageDto(
    int Version, string ScopeToken, long CoreEpoch, string ManifestToken, AtlasCompletionState Completion,
    AtlasFileDto[] Files, AtlasBoundsDto Bounds, int? NextOffset, string[] Disclosures);
public sealed record AtlasFileDto(
    string FileToken, AtlasDirectoryEntryKind Kind, string? ParentToken, string RelativePath,
    AtlasFileClassification Classification, AtlasFileAvailability Availability,
    AtlasCountDto DeclarationTotal, string? Reason);
public sealed record AtlasSelectionDto(
    int Version, string ScopeToken, long CoreEpoch, string ManifestToken, string FileToken,
    string? DeclarationToken, string? ReceiptToken, AtlasSourceDto Source, AtlasBoundsDto SourceBounds,
    AtlasOutlineState OutlineState, string? OutlineReason, AtlasOutlineRowDto[] Outline,
    AtlasBoundsDto OutlineBounds, int? OutlineNextOffset, AtlasCoverageDto Coverage, string[] Disclosures);
public sealed record AtlasSourceDto(
    SourceProjectionState State, string ObservationToken, string? BindingToken, string? DecoderId,
    string? Text, AtlasSpanDto? PageSpan, AtlasSpanDto[] Highlights, int? NextOffset, string? Reason);
public sealed record AtlasOutlineRowDto(
    string DeclarationToken, string DisplayName, AtlasDeclarationKind Kind, AtlasSpanDto Span,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] AtlasStructureDto? Structure = null);
/// <summary>Descriptive, page-scoped syntax evidence. An omitted parent never carries an authority token.</summary>
public sealed record AtlasStructureDto(
    AtlasClassifierFlavor? ClassifierFlavor, AtlasLexicalParentState ParentState,
    string? ParentDeclarationToken, AtlasStructureProvenance Provenance, string? Reason);
public sealed record AtlasSpanDto(int Start, int Length);
public sealed record AtlasCountDto(AtlasDenominatorState State, long? Value, string? Reason);
public sealed record AtlasCoverageDto(AtlasDenominatorState State, double? Value, string? Reason);
public sealed record AtlasBoundsDto(
    AtlasBoundsDimension Dimension, int RequestedLimit, int EffectiveLimit, int ReturnedRows,
    long ReturnedContentBytes, AtlasDenominatorState DenominatorState, long? DenominatorValue,
    string? DenominatorReason, string? OmissionReason, AtlasBoundsDimension? OmissionDimension);

/// <summary>Explicit request/phase units; these are not fields on native AtlasBounds.</summary>
public sealed record AtlasReaderPhaseContext(
    AtlasBoundsDimension Dimension, int RequestedLimit, int EffectiveLimit);

/// <summary>Verified phase facts supplied by Core, not inferred from native declaration bounds.</summary>
public sealed record AtlasSelectionPhaseContext(
    string NativeManifestToken, string NativeFileValue, long CoreEpoch, string ObservationToken,
    string? ReceiptToken, AtlasCountDto SourceTotal, int? SourceNextOffset,
    string? SourceOmissionReason, AtlasBoundsDimension? SourceOmissionDimension,
    AtlasOutlineState OutlineState, string? OutlineReason, int? OutlineNextOffset);

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
