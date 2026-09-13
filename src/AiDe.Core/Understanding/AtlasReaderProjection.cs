using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiDe.Core.Understanding;

/// <summary>
/// Pure native-to-render projections and strict inventory/source wire boundaries.
/// This is not an admission, token-store, transport-registration or root-I/O implementation.
/// </summary>
public static class AtlasReaderProjection
{
    public const int MaxFrameBodyBytes = 1024 * 1024;
    public const int FramePrefixBytes = sizeof(int);
    public const int MaxPageTextUtf8Bytes = 128 * 1024;
    public const int MaxSourceInputBytes = 8 * 1024 * 1024;

    private static readonly UTF8Encoding ContentEncoding = new(false, true);
    private static readonly JsonSerializerOptions WireOptions = CreateWireOptions();

    public static AtlasFileDto File(
        AtlasFileEntry entry, string fileToken, string? parentToken, AtlasCountDto declarationTotal)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var result = new AtlasFileDto(
            fileToken, entry.Kind, parentToken, entry.RelativePath, entry.Classification,
            entry.Availability, declarationTotal, SafeReason(entry.Reason));
        ValidateFile(result);
        return result;
    }

    /// <summary>
    /// Projects native row bounds only; source-range bounds require their own measured inputs.
    /// Unknown native omission dimensions remain absent rather than guessing a category.
    /// Content bytes are measured from rendered text, never native retention or serialization charges.
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
        InventoryPage page, string scopeToken, long coreEpoch, string manifestToken,
        AtlasCompletionState completion, Func<AtlasFileEntry, string> issueFileToken,
        Func<string, string?> findParentToken, Func<AtlasFileEntry, AtlasCountDto> declarationTotal,
        string[] disclosures)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(issueFileToken);
        ArgumentNullException.ThrowIfNull(findParentToken);
        ArgumentNullException.ThrowIfNull(declarationTotal);
        var files = page.Files.Select(entry => File(
            entry, issueFileToken(entry), findParentToken(entry.ParentPathKey), declarationTotal(entry))).ToArray();
        var bounds = Bounds(page.Bounds,
            new AtlasReaderPhaseContext(AtlasBoundsDimension.InventoryRows,
                page.Request.Limit, page.Bounds.EffectiveLimit), files.Length, 0);
        var result = new AtlasInventoryPageDto(
            1, scopeToken, coreEpoch, manifestToken, completion, files, bounds, page.NextOffset, disclosures);
        ValidateInventory(result);
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
            nextOffset, source.State is SourceProjectionState.IndexedMatch ? null : "not-recorded");
        ValidateSource(result);
        return result;
    }

    public static byte[] SerializeInventory(AtlasInventoryPageDto value)
    {
        ValidateInventory(value);
        return Write(value);
    }

    public static AtlasInventoryPageDto DeserializeInventory(ReadOnlyMemory<byte> body)
    {
        var value = Read<AtlasInventoryPageDto>(body);
        ValidateInventory(value);
        return value;
    }

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
        Require(value.Version == 1 && value.ExpectedCoreEpoch >= 0, "Invalid selection version or epoch.");
        RequireToken(value.ScopeToken);
        RequireToken(value.ManifestToken);
        RequireToken(value.FileToken);
        if (value.DeclarationToken is not null)
        {
            RequireToken(value.DeclarationToken);
        }

        Require(value.SourceOffset >= 0 && value.SourceLength >= 0
            && (long)value.SourceOffset + value.SourceLength <= int.MaxValue, "Invalid UTF-16 source range.");
        Require(value.OutlineOffset >= 0 && value.OutlineLimit is >= 1 and <= PageRequest.MaxLimit,
            "Invalid outline row page.");
        return value;
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
        Require(value.NextOffset is null or >= 0, "Invalid next offset.");
        foreach (var file in value.Files)
        {
            ValidateFile(file);
        }

        foreach (var disclosure in value.Disclosures)
        {
            Require(disclosure is not null, "Null disclosure.");
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
        Require(Enum.IsDefined(value.Kind) && Enum.IsDefined(value.Classification)
            && Enum.IsDefined(value.Availability), "Invalid file enum.");
        Require(value.DeclarationTotal is not null, "Declaration total is required.");
        ValidateCount(value.DeclarationTotal.State, value.DeclarationTotal.Value, value.DeclarationTotal.Reason);
        RequireSafeReason(value.Reason);
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
        Require(state is AtlasDenominatorState.Known || !string.IsNullOrWhiteSpace(reason),
            "Unknown denominators require an explanation.");
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
            Require(value.BindingToken is null && value.DecoderId is null && value.Text is null
                && value.PageSpan is null && value.Highlights.Length == 0 && value.NextOffset is null,
                "Only IndexedMatch may carry source content or a binding.");
            return;
        }

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
        AtlasDenominatorState.Unknown => "total-not-recorded",
        AtlasDenominatorState.Withheld => "total-withheld",
        _ => throw new JsonException("Invalid native denominator state."),
    };

    private static string? SafeReason(string? reason) =>
        reason is null ? null : IsToken(reason) ? reason : "not-recorded";

    private static bool IsToken(string? value) => value is { Length: > 0 and <= 128 }
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or ':');

    private static void RequireToken(string? token) => Require(IsToken(token), "Invalid opaque token.");
    private static void RequireSafeReason(string? reason) =>
        Require(reason is null || IsToken(reason), "Unsafe reason.");

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
