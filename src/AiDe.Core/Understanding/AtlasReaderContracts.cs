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

public sealed record AtlasCapabilitiesRequestDto(int[] SupportedVersions);
public sealed record AtlasCapabilitiesDto(
    int[] Versions, string[] Features, int MaxFrameBodyBytes, int MaxPageTextUtf8Bytes, int MaxPageItems);
public sealed record AtlasAdmitRequestDto(int Version, long ExpectedCoreEpoch);
public sealed record AtlasAdmitDto(
    int Version, string ScopeToken, string InitialManifestToken, long CoreEpoch, DateTimeOffset ExpiresAt);
public sealed record AtlasInventoryRequestDto(
    int Version, string ScopeToken, long ExpectedCoreEpoch, string ManifestToken, int Offset, int Limit);
public sealed record AtlasSelectRequestDto(
    int Version, string ScopeToken, long ExpectedCoreEpoch, string ManifestToken, string FileToken,
    string? DeclarationToken, int SourceOffset, int SourceLength, int OutlineOffset, int OutlineLimit);
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
    string DeclarationToken, string DisplayName, AtlasDeclarationKind Kind, AtlasSpanDto Span);
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
