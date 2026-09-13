using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiDe.Core.Understanding;

/// <summary>
/// Pure native-to-render projections and request-bound reader wire boundaries.
/// This is not an admission, token-store, transport-registration or root-I/O implementation.
/// </summary>
public static class AtlasReaderProjection
{
    public const int MaxFrameBodyBytes = 1024 * 1024;
    public const int FramePrefixBytes = sizeof(int);
    public const int MaxPageTextUtf8Bytes = 128 * 1024;
    public const int MaxSourceInputBytes = 8 * 1024 * 1024;

    private static readonly UTF8Encoding ContentEncoding = new(false, true);
    private static readonly Dictionary<string, string> ReasonMessages = new(StringComparer.Ordinal)
    {
        ["not-recorded"] = "not recorded",
        ["total-not-recorded"] = "total not recorded",
        ["total-withheld"] = "total withheld",
        ["entry-unavailable"] = "entry unavailable",
        ["link-refused"] = "link refused",
        ["inventory-partial"] = "inventory partial",
        ["page-omitted"] = "page omitted",
        ["descriptor-budget"] = "descriptor budget",
        ["declaration population unknown"] = "total not recorded",
        ["partial"] = "partial",
        ["detail withheld"] = "detail withheld",
        ["detail redacted"] = "detail redacted",
        ["outline budget exceeded"] = "outline budget exceeded",
        ["outline canceled"] = "outline canceled",
        ["outline unsupported"] = "outline unsupported",
        ["outline unavailable"] = "outline unavailable",
        ["outline refused"] = "outline refused",
        ["source display limit"] = "source display limit",
    };
    private static readonly JsonSerializerOptions WireOptions = CreateWireOptions();

    public static AtlasFileDto File(
        AtlasFileEntry entry, string fileToken, string? parentToken, AtlasCountDto declarationTotal)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var result = new AtlasFileDto(
            fileToken, entry.Kind, parentToken, entry.RelativePath, entry.Classification,
            entry.Availability, declarationTotal,
            SafeReason(entry.Reason) ?? (entry.Availability is AtlasFileAvailability.Refused or AtlasFileAvailability.Unavailable
                ? "not recorded" : null));
        ValidateFile(result);
        return result;
    }

    /// <summary>
    /// Projects native row bounds only; source-range bounds require their own measured inputs.
    /// Unknown native omission dimensions remain absent rather than guessing a category.
    /// Standalone metadata producers must report zero native and rendered content bytes.
    /// Selection's aggregate source bytes are reconciled separately before its outline split.
    /// </summary>
    public static AtlasBoundsDto Bounds(
        AtlasBounds native, AtlasReaderPhaseContext phase, int returnedRows, long returnedContentBytes)
    {
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(phase);
        Require(phase.Dimension is AtlasBoundsDimension.InventoryRows or AtlasBoundsDimension.OutlineRows,
            "Native row bounds require a row phase, not source-range units.");
        Require(phase.EffectiveLimit == native.EffectiveLimit, "Native and effective row limits disagree.");
        Require(returnedRows == native.ReturnedRows, "Native and rendered row counts disagree.");
        Require(native.ReturnedBytes == 0 && returnedContentBytes == 0,
            "A metadata producer must report zero content bytes.");
        var result = new AtlasBoundsDto(
            phase.Dimension, phase.RequestedLimit, phase.EffectiveLimit, native.ReturnedRows,
            returnedContentBytes, native.TotalState, native.TotalCount,
            DenominatorReason(native.TotalState), SafeReason(native.OmissionReason),
            native.LimitingDimension switch
            {
                "files" => AtlasBoundsDimension.InventoryRows,
                "declarations" => AtlasBoundsDimension.OutlineRows,
                "page" when phase.Dimension is AtlasBoundsDimension.InventoryRows => AtlasBoundsDimension.InventoryRows,
                _ => null,
            });
        ValidateBounds(result);
        return result;
    }

    public static AtlasInventoryPageDto Inventory(
        InventoryPage page, AtlasInventoryRequestDto request, long coreEpoch,
        AtlasCompletionState completion, Func<AtlasFileEntry, string> issueFileToken,
        Func<string, string?> findParentToken, Func<AtlasFileEntry, AtlasCountDto> declarationTotal,
        string[] disclosures)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(issueFileToken);
        ArgumentNullException.ThrowIfNull(findParentToken);
        ArgumentNullException.ThrowIfNull(declarationTotal);
        ValidateInventoryRequest(request);
        Require(page.Request.Offset == request.Offset && page.Request.Limit == request.Limit,
            "Native inventory request differs from the accepted request.");
        var files = page.Files.Select(entry => File(
            entry, issueFileToken(entry), findParentToken(entry.ParentPathKey), declarationTotal(entry))).ToArray();
        var bounds = Bounds(page.Bounds,
            new AtlasReaderPhaseContext(AtlasBoundsDimension.InventoryRows,
                page.Request.Limit, page.Bounds.EffectiveLimit), files.Length, 0);
        var result = new AtlasInventoryPageDto(
            1, request.ScopeToken, coreEpoch, request.ManifestToken, completion, files, bounds, page.NextOffset,
            disclosures.Select(reason => SafeReason(reason) ?? "not recorded").ToArray());
        ValidateInventory(result, request);
        return result;
    }

    /// <summary>The binding callback must be the owning Core table's opaque-token issuer.</summary>
    public static AtlasSourceDto Source(
        SourceProjection source, string observationToken, Func<AtlasSourceBinding, string> issueBindingToken,
        int? nextOffset, long observedContentBytes)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(issueBindingToken);
        var page = source.Page;
        Require(observedContentBytes == (page is null ? 0 : ContentBytes(page.Text)),
            "Producer and rendered UTF-8 content byte counts disagree.");
        var result = new AtlasSourceDto(
            source.State, observationToken,
            source.ExpectedBinding is { } binding ? issueBindingToken(binding) : null,
            source.DecoderId, page?.Text,
            page is null ? null : new AtlasSpanDto(page.PageSpan.Start, page.PageSpan.Length),
            page?.Highlights.Select(span => new AtlasSpanDto(span.Start, span.Length)).ToArray() ?? [],
            nextOffset, source.State is SourceProjectionState.IndexedMatch ? null : "not recorded");
        ValidateSource(result);
        return result;
    }

    /// <summary>Structural-only encoding. Use the request overload at publication.</summary>
    public static byte[] SerializeInventory(AtlasInventoryPageDto value)
    {
        ValidateInventory(value);
        return Write(value);
    }

    /// <summary>Structural-only decoding; it does not establish request identity or progress.</summary>
    public static AtlasInventoryPageDto DeserializeInventory(ReadOnlyMemory<byte> body)
    {
        var value = Read<AtlasInventoryPageDto>(body);
        ValidateInventory(value);
        return value;
    }

    public static byte[] SerializeInventory(AtlasInventoryPageDto value, AtlasInventoryRequestDto request)
    {
        ValidateInventory(value, request);
        return Write(value);
    }

    public static AtlasInventoryPageDto DeserializeInventory(ReadOnlyMemory<byte> body, AtlasInventoryRequestDto request)
    {
        var value = Read<AtlasInventoryPageDto>(body);
        ValidateInventory(value, request);
        return value;
    }

    public static AtlasSelectionDto Selection(
        SelectionProjection native, AtlasSelectRequestDto request, AtlasSelectionPhaseContext phase,
        Func<AtlasSourceBinding, string> issueBindingToken, Func<OutlineDeclaration, string> issueDeclarationToken)
    {
        ArgumentNullException.ThrowIfNull(native);
        ArgumentNullException.ThrowIfNull(phase);
        ArgumentNullException.ThrowIfNull(issueDeclarationToken);
        ValidateSelectRequest(request);
        Require(native.ManifestToken == phase.NativeManifestToken && native.FileValue == phase.NativeFileValue,
            "Native selection differs from the resolved request identities.");
        Require(native.Bounds.ReturnedRows == native.Outline.Declarations.Length,
            "Native declaration count disagrees with its outline.");
        var (source, byteCut, windowCut) = SelectionSource(native, request, phase, issueBindingToken);
        Require(phase.SourceTotal is not null, "Source extent facts are required.");
        ValidateCount(phase.SourceTotal.State, phase.SourceTotal.Value, phase.SourceTotal.Reason);
        var sourceBounds = new AtlasBoundsDto(
            AtlasBoundsDimension.SourceUtf16CodeUnits, request.SourceLength, source.Text?.Length ?? 0,
            0, source.Text is null ? 0 : ContentBytes(source.Text), phase.SourceTotal.State,
            phase.SourceTotal.Value, phase.SourceTotal.Reason,
            byteCut ? "source display limit" : SafeReason(phase.SourceOmissionReason) ?? (windowCut ? "page omitted" : null),
            byteCut ? AtlasBoundsDimension.SourceUtf8ContentBytes
                : phase.SourceOmissionDimension ?? (windowCut ? AtlasBoundsDimension.SourceUtf16CodeUnits : null));
        var available = phase.OutlineState is AtlasOutlineState.Available;
        Require(!available || phase.OutlineReason is null, "Available outline has no failure reason.");
        var outlineLimit = available ? Math.Min(request.OutlineLimit, native.Bounds.EffectiveLimit) : 0;
        var outline = available
            ? native.Outline.Declarations.Skip(request.OutlineOffset).Take(outlineLimit).Select(declaration => new AtlasOutlineRowDto(
                issueDeclarationToken(declaration), declaration.DisplayName, declaration.Kind,
                new AtlasSpanDto(declaration.Span.Start, declaration.Span.Length))).ToArray()
            : [];
        var outlineCut = available && Math.Max(0, native.Outline.Declarations.Length - request.OutlineOffset) > outline.Length;
        var outlineBounds = new AtlasBoundsDto(
            AtlasBoundsDimension.OutlineRows, request.OutlineLimit,
            outlineLimit,
            outline.Length, 0, native.Bounds.TotalState, native.Bounds.TotalCount,
            DenominatorReason(native.Bounds.TotalState),
            available ? SafeReason(native.Bounds.OmissionReason) ?? (outlineCut ? "page omitted" : null)
                : SafeReason(phase.OutlineReason) ?? "not recorded",
            available && (outlineCut || native.Bounds.LimitingDimension is "declarations") ? AtlasBoundsDimension.OutlineRows : null);
        var coverage = new AtlasCoverageDto(native.Coverage.State, native.Coverage.Value,
            DenominatorReason(native.Coverage.State));
        var result = new AtlasSelectionDto(
            1, request.ScopeToken, phase.CoreEpoch, request.ManifestToken, request.FileToken,
            request.DeclarationToken, phase.ReceiptToken, source, sourceBounds, phase.OutlineState,
            available ? null : SafeReason(phase.OutlineReason) ?? "not recorded",
            outline, outlineBounds, phase.OutlineNextOffset, coverage,
            native.Limitations.Select(reason => SafeReason(reason) ?? "not recorded").ToArray());
        ValidateSelection(result, request);
        return result;
    }

    private static (AtlasSourceDto Source, bool ByteCut, bool WindowCut) SelectionSource(
        SelectionProjection native, AtlasSelectRequestDto request, AtlasSelectionPhaseContext phase,
        Func<AtlasSourceBinding, string> issueBindingToken)
    {
        ArgumentNullException.ThrowIfNull(issueBindingToken);
        if (native.Source.State is not SourceProjectionState.IndexedMatch)
        {
            return (Source(native.Source, phase.ObservationToken, issueBindingToken,
                phase.SourceNextOffset, native.Bounds.ReturnedBytes), false, false);
        }

        var page = native.Source.Page ?? throw new JsonException("Indexed source requires its native page.");
        Require(native.Bounds.ReturnedBytes == ContentBytes(page.Text),
            "Native aggregate content bytes disagree with the verified page.");
        Require(native.Bounds.ReturnedBytes <= MaxSourceInputBytes, "Native source page exceeds the input budget.");
        var localOffset = (long)request.SourceOffset - page.PageSpan.Start;
        Require(localOffset >= 0 && localOffset <= page.Text.Length, "Requested source start is outside the verified page.");
        var start = (int)localOffset;
        Require(IsScalarBoundary(page.Text, start), "Requested source start splits a Unicode scalar.");
        var available = page.Text.Length - start;
        var windowLength = Math.Min(request.SourceLength, available);
        if (!IsScalarBoundary(page.Text, start + windowLength))
        {
            windowLength--;
        }

        Require(windowLength > 0 || available == 0, "Requested source window cannot contain a Unicode scalar.");
        var length = 0;
        var bytes = 0;
        foreach (var rune in page.Text.AsSpan(start, windowLength).EnumerateRunes())
        {
            if (bytes + rune.Utf8SequenceLength > MaxPageTextUtf8Bytes)
            {
                break;
            }

            bytes += rune.Utf8SequenceLength;
            length += rune.Utf16SequenceLength;
        }

        var end = request.SourceOffset + length;
        var highlights = new List<AtlasSpanDto>();
        foreach (var highlight in page.Highlights)
        {
            Require(IsScalarBoundary(page.Text, highlight.Start - page.PageSpan.Start)
                && IsScalarBoundary(page.Text, highlight.End - page.PageSpan.Start),
                "Native highlight splits a Unicode scalar.");
            var highlightStart = Math.Max(request.SourceOffset, highlight.Start);
            var highlightEnd = Math.Min(end, highlight.End);
            if (highlightStart < highlightEnd
                || (highlight.Length == 0 && highlight.Start >= request.SourceOffset && highlight.Start <= end))
            {
                highlights.Add(new AtlasSpanDto(highlightStart, highlightEnd - highlightStart));
            }
        }

        var binding = native.Source.ExpectedBinding ?? throw new JsonException("Indexed source requires its native binding.");
        var result = new AtlasSourceDto(SourceProjectionState.IndexedMatch, phase.ObservationToken,
            issueBindingToken(binding), native.Source.DecoderId, page.Text.Substring(start, length),
            new AtlasSpanDto(request.SourceOffset, length), highlights.ToArray(), phase.SourceNextOffset, null);
        ValidateSource(result);
        return (result, length < windowLength, length < available);
    }

    public static byte[] SerializeSelection(AtlasSelectionDto value, AtlasSelectRequestDto request)
    {
        ValidateSelection(value, request);
        return Write(value);
    }

    public static AtlasSelectionDto DeserializeSelection(ReadOnlyMemory<byte> body, AtlasSelectRequestDto request)
    {
        var value = Read<AtlasSelectionDto>(body);
        ValidateSelection(value, request);
        return value;
    }

    /// <summary>Structural-only source encoding; selection publication additionally requires request bounds.</summary>
    public static byte[] SerializeSource(AtlasSourceDto value)
    {
        ValidateSource(value);
        return Write(value);
    }

    public static AtlasSourceDto DeserializeSource(ReadOnlyMemory<byte> body)
    {
        var value = Read<AtlasSourceDto>(body);
        ValidateSource(value);
        return value;
    }

    public static AtlasSelectRequestDto DeserializeSelect(ReadOnlyMemory<byte> body)
    {
        var value = Read<AtlasSelectRequestDto>(body);
        ValidateSelectRequest(value);
        return value;
    }

    private static void ValidateSelectRequest(AtlasSelectRequestDto value)
    {
        Require(value is not null, "Selection request is required.");
        Require(value.Version == 1 && value.ExpectedCoreEpoch >= 0, "Invalid selection version or epoch.");
        RequireToken(value.ScopeToken);
        RequireToken(value.ManifestToken);
        RequireToken(value.FileToken);
        if (value.DeclarationToken is not null)
        {
            RequireToken(value.DeclarationToken);
        }

        Require(value.SourceOffset >= 0 && value.SourceLength > 0
            && (long)value.SourceOffset + value.SourceLength <= int.MaxValue, "Invalid UTF-16 source range.");
        Require(value.OutlineOffset >= 0 && value.OutlineLimit is >= 1 and <= PageRequest.MaxLimit,
            "Invalid outline row page.");
    }

    private static void ValidateInventoryRequest(AtlasInventoryRequestDto request)
    {
        Require(request is not null && request.Version == 1 && request.ExpectedCoreEpoch >= 0,
            "Invalid inventory request.");
        RequireToken(request.ScopeToken);
        RequireToken(request.ManifestToken);
        Require(request.Offset >= 0 && request.Limit is >= 1 and <= PageRequest.MaxLimit,
            "Invalid inventory row page.");
    }

    private static void ValidateInventory(AtlasInventoryPageDto value, AtlasInventoryRequestDto request)
    {
        ValidateInventoryRequest(request);
        ValidateInventory(value);
        Require(value.ScopeToken == request.ScopeToken && value.ManifestToken == request.ManifestToken
            && value.CoreEpoch == request.ExpectedCoreEpoch && value.Bounds.RequestedLimit == request.Limit,
            "Inventory does not belong to the accepted request.");
        ValidateProgress(request.Offset, value.Files.Length, value.NextOffset, value.Bounds.DenominatorValue);
    }

    private static void ValidateInventory(AtlasInventoryPageDto value)
    {
        Require(value is not null, "Inventory is required.");
        Require(value.Version == 1 && value.CoreEpoch >= 0, "Invalid inventory version or epoch.");
        RequireToken(value.ScopeToken);
        RequireToken(value.ManifestToken);
        Require(Enum.IsDefined(value.Completion), "Invalid completion.");
        Require(value.Files is not null && value.Disclosures is not null, "Required inventory collections.");
        ValidateBounds(value.Bounds);
        Require(value.Bounds.Dimension is AtlasBoundsDimension.InventoryRows
            && value.Files.Length == value.Bounds.ReturnedRows, "Inventory bounds disagree with content.");
        Require(value.NextOffset is null || (value.NextOffset > 0 && value.Files.Length > 0),
            "A continuation requires nonempty advancing content.");
        foreach (var file in value.Files)
        {
            ValidateFile(file);
        }

        foreach (var disclosure in value.Disclosures)
        {
            Require(disclosure is not null, "Null disclosure.");
            RequireSafeReason(disclosure);
        }
    }

    private static void ValidateFile(AtlasFileDto value)
    {
        Require(value is not null, "File is required.");
        RequireToken(value.FileToken);
        if (value.ParentToken is not null)
        {
            RequireToken(value.ParentToken);
        }

        Require(!string.IsNullOrWhiteSpace(value.RelativePath)
            && !Path.IsPathRooted(value.RelativePath)
            && !value.RelativePath.Contains(':')
            && !value.RelativePath.Split('/', '\\').Contains(".."), "Expected a relative path.");
        _ = ContentBytes(value.RelativePath);
        Require(Enum.IsDefined(value.Kind) && Enum.IsDefined(value.Classification)
            && Enum.IsDefined(value.Availability), "Invalid file enum.");
        Require(value.DeclarationTotal is not null, "Declaration total is required.");
        ValidateCount(value.DeclarationTotal.State, value.DeclarationTotal.Value, value.DeclarationTotal.Reason);
        RequireSafeReason(value.Reason);
        Require(value.Availability is not (AtlasFileAvailability.Refused or AtlasFileAvailability.Unavailable)
            || value.Reason is not null, "Unavailable metadata requires a safe explanation.");
    }

    private static void ValidateBounds(AtlasBoundsDto value)
    {
        Require(value is not null, "Bounds are required.");
        Require(Enum.IsDefined(value.Dimension), "Invalid bounds dimension.");
        Require(value.RequestedLimit >= 0 && value.EffectiveLimit >= 0
            && value.EffectiveLimit <= value.RequestedLimit && value.ReturnedRows >= 0
            && value.ReturnedContentBytes >= 0, "Invalid bounds values.");
        if (value.Dimension is AtlasBoundsDimension.InventoryRows or AtlasBoundsDimension.OutlineRows)
        {
            Require(value.ReturnedRows <= value.EffectiveLimit && value.ReturnedContentBytes == 0,
                "Metadata carries rows, not source content bytes.");
            Require(value.DenominatorValue is null || value.DenominatorValue >= value.ReturnedRows,
                "Known row totals cannot be smaller than the returned rows.");
        }
        else if (value.Dimension is AtlasBoundsDimension.SourceUtf16CodeUnits)
        {
            Require(value.ReturnedRows == 0 && value.ReturnedContentBytes <= MaxPageTextUtf8Bytes,
                "Source bounds carry content bytes, not metadata rows.");
        }

        ValidateCount(value.DenominatorState, value.DenominatorValue, value.DenominatorReason);
        Require(value.OmissionDimension is null || Enum.IsDefined(value.OmissionDimension.Value),
            "Invalid omission dimension.");
        Require(value.OmissionDimension is null || value.OmissionReason is not null,
            "An omission dimension requires a reason.");
        RequireSafeReason(value.OmissionReason);
    }

    private static void ValidateCount(AtlasDenominatorState state, long? value, string? reason)
    {
        Require(Enum.IsDefined(state), "Invalid denominator state.");
        Require(state is AtlasDenominatorState.Known ? value is >= 0 : value is null,
            "Only known denominators carry values.");
        Require(state is AtlasDenominatorState.Known ? reason is null : !string.IsNullOrWhiteSpace(reason),
            "Known denominators have no reason; unknown denominators require one.");
        RequireSafeReason(reason);
    }

    private static void ValidateSource(AtlasSourceDto value)
    {
        Require(value is not null && Enum.IsDefined(value.State), "Invalid source state.");
        RequireToken(value.ObservationToken);
        Require(value.Highlights is not null, "Highlights are required.");
        RequireSafeReason(value.Reason);
        if (value.State is not SourceProjectionState.IndexedMatch)
        {
            Require(value.Reason is not null, "Nonmatching source requires a safe explanation.");
            Require(value.BindingToken is null && value.DecoderId is null && value.Text is null
                && value.PageSpan is null && value.Highlights.Length == 0 && value.NextOffset is null,
                "Only IndexedMatch may carry source content or a binding.");
            return;
        }

        Require(value.Reason is null, "Indexed source has no failure reason.");
        RequireToken(value.BindingToken);
        RequireToken(value.DecoderId);
        Require(value.Text is not null && value.PageSpan is not null, "Indexed source requires a page.");
        var span = value.PageSpan;
        Require(span.Start >= 0 && span.Length == value.Text.Length
            && (long)span.Start + span.Length <= int.MaxValue, "Page span must describe UTF-16 text.");
        Require(ContentBytes(value.Text) <= MaxPageTextUtf8Bytes, "Source display budget exceeded.");
        Require(value.NextOffset is null || value.NextOffset == span.Start + span.Length,
            "Source continuation must follow the page.");
        foreach (var highlight in value.Highlights)
        {
            Require(highlight is not null && highlight.Start >= span.Start && highlight.Length >= 0
                && (long)highlight.Start + highlight.Length <= (long)span.Start + span.Length,
                "Highlight escapes its source page.");
            Require(IsScalarBoundary(value.Text, highlight.Start - span.Start)
                && IsScalarBoundary(value.Text, highlight.Start - span.Start + highlight.Length),
                "Highlight splits a Unicode scalar.");
        }
    }

    private static bool IsScalarBoundary(string text, int index) =>
        index >= 0 && index <= text.Length
        && !(index > 0 && index < text.Length && char.IsHighSurrogate(text[index - 1]) && char.IsLowSurrogate(text[index]));

    private static void ValidateProgress(int offset, int returned, int? next, long? total)
    {
        var end = (long)offset + returned;
        Require(offset >= 0 && returned >= 0 && end <= int.MaxValue, "Page extent overflow.");
        Require(total is null || returned == 0 || end <= total, "Page exceeds its known extent.");
        Require(next is null || (returned > 0 && next > offset && next == end && (total is null || end < total)),
            "Continuation does not advance by the exact returned extent or continues beyond a known end.");
    }

    private static void ValidateSelection(AtlasSelectionDto value, AtlasSelectRequestDto request)
    {
        ValidateSelectRequest(request);
        Require(value is not null && value.Version == 1, "Invalid selection.");
        RequireToken(value.ScopeToken);
        RequireToken(value.ManifestToken);
        RequireToken(value.FileToken);
        Require(value.ScopeToken == request.ScopeToken && value.ManifestToken == request.ManifestToken
            && value.CoreEpoch == request.ExpectedCoreEpoch && value.FileToken == request.FileToken
            && value.DeclarationToken == request.DeclarationToken, "Selection does not belong to the accepted request.");
        if (value.ReceiptToken is not null)
        {
            RequireToken(value.ReceiptToken);
        }

        ValidateSource(value.Source);
        ValidateBounds(value.SourceBounds);
        Require(value.SourceBounds.Dimension is AtlasBoundsDimension.SourceUtf16CodeUnits
            && value.SourceBounds.RequestedLimit == request.SourceLength
            && value.SourceBounds.ReturnedContentBytes == (value.Source.Text is null ? 0 : ContentBytes(value.Source.Text)),
            "Source bounds disagree with the request or content.");
        var sourceLength = value.Source.PageSpan?.Length ?? 0;
        Require(sourceLength <= value.SourceBounds.EffectiveLimit, "Source exceeds its effective limit.");
        if (value.Source.PageSpan is { } page)
        {
            Require(page.Start == request.SourceOffset, "Source starts outside the accepted request.");
            Require(value.SourceBounds.DenominatorValue is null
                || (long)request.SourceOffset + value.SourceBounds.EffectiveLimit <= value.SourceBounds.DenominatorValue,
                "Effective source limit exceeds verified extent.");
        }
        else
        {
            Require(value.SourceBounds.EffectiveLimit == 0, "Absent source has no effective content window.");
        }

        ValidateProgress(request.SourceOffset, sourceLength, value.Source.NextOffset, value.SourceBounds.DenominatorValue);
        Require(Enum.IsDefined(value.OutlineState) && value.Outline is not null, "Invalid outline.");
        RequireSafeReason(value.OutlineReason);
        Require(value.OutlineState is AtlasOutlineState.Available ? value.OutlineReason is null
            : value.OutlineReason is not null && value.Outline.Length == 0 && value.OutlineNextOffset is null,
            "Unavailable outline must carry an explanation, not rows or continuation.");
        ValidateBounds(value.OutlineBounds);
        Require(value.OutlineBounds.Dimension is AtlasBoundsDimension.OutlineRows
            && value.OutlineBounds.RequestedLimit == request.OutlineLimit
            && value.OutlineBounds.ReturnedRows == value.Outline.Length,
            "Outline bounds disagree with content or request.");
        Require(value.OutlineState is AtlasOutlineState.Available || value.OutlineBounds.EffectiveLimit == 0,
            "Failed outline has zero effective limit.");
        ValidateProgress(request.OutlineOffset, value.Outline.Length, value.OutlineNextOffset, value.OutlineBounds.DenominatorValue);
        foreach (var row in value.Outline)
        {
            Require(row is not null && row.Span is not null, "Null outline row.");
            RequireToken(row.DeclarationToken);
            Require(!string.IsNullOrWhiteSpace(row.DisplayName) && Enum.IsDefined(row.Kind), "Invalid outline metadata.");
            _ = ContentBytes(row.DisplayName);
            Require(row.Span.Start >= 0 && row.Span.Length >= 0
                && (long)row.Span.Start + row.Span.Length <= int.MaxValue, "Invalid outline span.");
        }

        Require(value.Coverage is not null && Enum.IsDefined(value.Coverage.State), "Invalid coverage.");
        Require(value.Coverage.State is AtlasDenominatorState.Known
            ? value.Coverage.Value is { } coverage && double.IsFinite(coverage) && coverage is >= 0 and <= 1 && value.Coverage.Reason is null
            : value.Coverage.Value is null && value.Coverage.Reason is not null, "Invalid coverage value or explanation.");
        RequireSafeReason(value.Coverage.Reason);
        Require(value.Disclosures is not null, "Disclosures are required.");
        foreach (var disclosure in value.Disclosures)
        {
            Require(disclosure is not null, "Null disclosure.");
            RequireSafeReason(disclosure);
        }
    }

    private static int ContentBytes(string text)
    {
        try
        {
            return ContentEncoding.GetByteCount(text);
        }
        catch (EncoderFallbackException exception)
        {
            throw new JsonException("Source is not valid Unicode.", exception);
        }
    }

    private static string? DenominatorReason(AtlasDenominatorState state) => state switch
    {
        AtlasDenominatorState.Known => null,
        AtlasDenominatorState.Unknown => "total not recorded",
        AtlasDenominatorState.Withheld => "total withheld",
        _ => throw new JsonException("Invalid native denominator state."),
    };

    private static string? SafeReason(string? reason) => reason is null ? null
        : ReasonMessages.TryGetValue(reason, out var message) ? message
        : ReasonMessages.ContainsValue(reason) ? reason : "detail redacted";

    private static void RequireToken(string? token)
    {
        Require(!string.IsNullOrWhiteSpace(token), "An opaque handle must be nonblank.");
        Require(ContentBytes(token) <= 256, "An opaque handle exceeds 256 UTF-8 bytes.");
    }
    private static void RequireSafeReason(string? reason) =>
        Require(reason is null || ReasonMessages.ContainsValue(reason), "Reason is not an approved safe message.");

    private static void Require(
        [System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition)
        {
            throw new JsonException(message);
        }
    }

    private static byte[] Write<T>(T value)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(value, WireOptions);
        Require(body.Length <= MaxFrameBodyBytes, "Atlas frame body budget exceeded.");
        return body;
    }

    private static T Read<T>(ReadOnlyMemory<byte> body) where T : class
    {
        Require(body.Length <= MaxFrameBodyBytes, "Atlas frame body budget exceeded.");
        using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 32 });
        RejectDuplicates(document.RootElement);
        return document.RootElement.Deserialize<T>(WireOptions) ?? throw new JsonException("Null Atlas body.");
    }

    private static void RejectDuplicates(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                Require(names.Add(property.Name), "Duplicate Atlas property.");
                RejectDuplicates(property.Value);
            }
        }
        else if (element.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                RejectDuplicates(item);
            }
        }
    }

    private static JsonSerializerOptions CreateWireOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            MaxDepth = 32,
            RespectRequiredConstructorParameters = true,
            RespectNullableAnnotations = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new StrictEnumConverter<AtlasCompletionState>());
        options.Converters.Add(new StrictEnumConverter<AtlasDirectoryEntryKind>());
        options.Converters.Add(new StrictEnumConverter<AtlasFileClassification>());
        options.Converters.Add(new StrictEnumConverter<AtlasFileAvailability>());
        options.Converters.Add(new StrictEnumConverter<AtlasDenominatorState>());
        options.Converters.Add(new StrictEnumConverter<AtlasBoundsDimension>());
        options.Converters.Add(new StrictEnumConverter<SourceProjectionState>());
        options.Converters.Add(new StrictEnumConverter<AtlasOutlineState>());
        options.Converters.Add(new StrictEnumConverter<AtlasDeclarationKind>());
        return options;
    }

    private sealed class StrictEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is not JsonTokenType.String
                || reader.GetString() is not { } name
                || !Enum.GetNames<T>().Contains(name, StringComparer.Ordinal))
            {
                throw new JsonException("Expected an exact Atlas enum name.");
            }

            return Enum.Parse<T>(name, ignoreCase: false);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            Require(Enum.IsDefined(value), "Undefined Atlas enum.");
            writer.WriteStringValue(value.ToString());
        }
    }
}
