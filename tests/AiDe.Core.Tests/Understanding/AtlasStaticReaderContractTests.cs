using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasStaticReaderContractTests
{
    [Fact]
    public void Capabilities_CurrentLiteralCodecAcceptsLegacyE0PayloadWithoutProvingRegistration()
    {
        const string json = """
            {"versions":[1],"features":["inventory","source","outline","receipts"],
            "maxFrameBodyBytes":1048576,"maxPageTextUtf8Bytes":131072,"maxPageItems":128}
            """;

        var capabilities = AtlasReaderProjection.DeserializeCapabilities(Encoding.UTF8.GetBytes(json));

        Assert.Equal([1], capabilities.Versions);
        Assert.Equal(["inventory", "source", "outline", "receipts"], capabilities.Features);
        Assert.DoesNotContain("static-structure-v1", capabilities.Features);
        Assert.Equal(AtlasReaderProjection.MaxFrameBodyBytes, capabilities.MaxFrameBodyBytes);
        Assert.Equal(AtlasReaderProjection.MaxPageTextUtf8Bytes, capabilities.MaxPageTextUtf8Bytes);
        Assert.Equal(128, capabilities.MaxPageItems);
    }

    [Theory]
    [InlineData("\"staticStructure\":null,")]
    [InlineData("\"staticStructure\":{\"parentToken\":null},")]
    public void SelectRequest_CurrentStrictCodecRejectsE1OptInFields(string injectedProperty)
    {
        var json = ValidSelectRequest().Replace("\"fileToken\"", injectedProperty + "\"fileToken\"", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSelect(Encoding.UTF8.GetBytes(json)));
    }

    [Theory]
    [InlineData("\"parentDeclarationToken\":null,")]
    [InlineData("\"classifierFlavor\":\"class\",")]
    [InlineData("\"kind\":\"Method\"", "\"kind\":1")]
    [InlineData("\"kind\":\"Method\"", "\"kind\":\"method\"")]
    [InlineData("\"span\":{\"start\":200,\"length\":4}", "\"span\":{\"start\":200,\"length\":4},\"span\":{\"start\":200,\"length\":4}")]
    public void SelectionResponse_CurrentStrictCodecRejectsStructuralMetadataAndMalformedRows(
        string before,
        string? after = null)
    {
        var baseJson = JsonNode.Parse(AtlasReaderProjection.SerializeSelection(CurrentSelection(), CurrentSelectRequest()))!.AsObject();
        var baseBody = Encoding.UTF8.GetBytes(baseJson.ToJsonString());

        var validated = AtlasReaderProjection.DeserializeSelection(baseBody, CurrentSelectRequest());
        var mutated = MutateFirstOutlineRow(baseJson, before, after);

        Assert.Equal("scope-token", validated.ScopeToken);
        Assert.Empty(RootDifferencesExcludingOutline(baseJson, mutated));
        AssertRowMutationPresent(mutated, before, after);
        Assert.Throws<JsonException>(() =>
            AtlasReaderProjection.DeserializeSelection(Encoding.UTF8.GetBytes(mutated.ToJsonString()), CurrentSelectRequest()));
    }

    [Fact]
    public void SelectionResponse_RowIsolationControlDetectsRootCorruptionBeforeCodec()
    {
        var baseJson = JsonNode.Parse(AtlasReaderProjection.SerializeSelection(CurrentSelection(), CurrentSelectRequest()))!.AsObject();
        var corrupted = MutateFirstOutlineRow(baseJson, "\"kind\":\"Method\"", "\"kind\":1");
        corrupted["scopeToken"] = "other-scope";
        corrupted["unexpectedRoot"] = true;

        var differences = RootDifferencesExcludingOutline(baseJson, corrupted);

        AssertRowMutationPresent(corrupted, "\"kind\":\"Method\"", "\"kind\":1");
        Assert.Contains("scopeToken", differences);
        Assert.Contains("unexpectedRoot", differences);
    }

    [Fact]
    public void Restore_CurrentLocalCodecHasNoOptInAndSelectionValidationUsesOriginalRequestWithoutRemoteRetention()
    {
        var restoreProperties = typeof(AtlasRestoreRequestDto).GetProperties().Select(property => property.Name).Order().ToArray();
        var outlineProperties = typeof(AtlasOutlineRowDto).GetProperties().Select(property => property.Name).Order().ToArray();
        var selectionBody = AtlasReaderProjection.SerializeSelection(CurrentSelection(), CurrentSelectRequest());

        var restoredAgainstOriginal = AtlasReaderProjection.DeserializeSelection(selectionBody, CurrentSelectRequest());

        Assert.Equal(["ExpectedCoreEpoch", "ReceiptToken", "ScopeToken", "Version"], restoreProperties);
        Assert.Equal(["DeclarationToken", "DisplayName", "Kind", "Span"], outlineProperties);
        Assert.DoesNotContain(outlineProperties, property => property.Contains("Parent", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(outlineProperties, property => property.Contains("Flavor", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("receipt-token", restoredAgainstOriginal.ReceiptToken);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSelection(
            selectionBody, CurrentSelectRequest() with { SourceLength = 8 }));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeRestoreRequest(
            Encoding.UTF8.GetBytes("""{"version":1,"scopeToken":"scope-token","expectedCoreEpoch":7,"receiptToken":"receipt-token","staticStructure":true}""")));
    }

    [Fact]
    public void PublicationBudget_CurrentBaselineBodyFitsFrameBodyLimitAndMetadataContentStaysZero()
    {
        const string escapedText = @"A\Bé";
        var body = AtlasReaderProjection.SerializeSelection(CurrentSelection(escapedText), CurrentSelectRequest());
        var remote = AtlasReaderProjection.DeserializeSelection(body, CurrentSelectRequest());

        Assert.True(body.Length <= AtlasReaderProjection.MaxFrameBodyBytes);
        Assert.Equal(sizeof(int), AtlasReaderProjection.FramePrefixBytes);
        Assert.Equal(1024 * 1024, AtlasReaderProjection.MaxFrameBodyBytes);
        Assert.Equal(128 * 1024, AtlasReaderProjection.MaxPageTextUtf8Bytes);
        Assert.Equal(Encoding.UTF8.GetByteCount(escapedText), remote.SourceBounds.ReturnedContentBytes);
        Assert.Equal(0, remote.OutlineBounds.ReturnedContentBytes);
        Assert.Equal(0, remote.OutlineBounds.ReturnedRows - remote.Outline.Length);
    }

    private static JsonObject MutateFirstOutlineRow(JsonObject baseJson, string before, string? after)
    {
        var mutated = JsonNode.Parse(baseJson.ToJsonString())!.AsObject();
        var row = mutated["outline"]![0]!.AsObject();
        if (null == after)
        {
            var property = before.Split(':')[0].Trim('"');
            if (before.Contains("\"class\"", StringComparison.Ordinal))
                row.Insert(0, property, "class");
            else
                row.Insert(0, property, JsonValue.Create<string?>(null));
            return mutated;
        }

        var rowJson = row.ToJsonString();
        Assert.Contains(before, rowJson);
        mutated["outline"]![0] = JsonNode.Parse(rowJson.Replace(before, after, StringComparison.Ordinal));
        return mutated;
    }

    private static void AssertRowMutationPresent(JsonObject mutated, string before, string? after)
    {
        var row = mutated["outline"]![0]!.AsObject();
        if (null == after)
        {
            var property = before.Split(':')[0].Trim('"');
            Assert.Contains(row, item => item.Key == property);
            if ("classifierFlavor" == property)
                Assert.Equal("class", row[property]!.GetValue<string>());
            else
                Assert.Null(row[property]);
            return;
        }

        Assert.Contains(after, row.ToJsonString(), StringComparison.Ordinal);
    }

    private static string[] RootDifferencesExcludingOutline(JsonObject expected, JsonObject actual)
    {
        var expectedKeys = expected.Select(item => item.Key).Where(key => key != "outline").ToHashSet(StringComparer.Ordinal);
        var actualKeys = actual.Select(item => item.Key).Where(key => key != "outline").ToHashSet(StringComparer.Ordinal);
        var keys = expectedKeys.Concat(actualKeys).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        return keys
            .Where(key => !expectedKeys.Contains(key)
                || !actualKeys.Contains(key)
                || !JsonNode.DeepEquals(expected[key], actual[key]))
            .ToArray();
    }

    private static string ValidSelectRequest() => """
        {"version":1,"scopeToken":"scope-token","expectedCoreEpoch":7,"manifestToken":"manifest-token",
        "fileToken":"file-token","declarationToken":"declaration-token","sourceOffset":200,
        "sourceLength":4,"outlineOffset":0,"outlineLimit":8}
        """;

    private static AtlasSelectRequestDto CurrentSelectRequest() =>
        AtlasReaderProjection.DeserializeSelect(Encoding.UTF8.GetBytes(ValidSelectRequest()));

    private static AtlasSelectionDto CurrentSelection(string text = "A😀é") => new(
        1,
        "scope-token",
        7,
        "manifest-token",
        "file-token",
        "declaration-token",
        "receipt-token",
        new AtlasSourceDto(SourceProjectionState.IndexedMatch, "observation-token", "binding-token", "utf8",
            text, new AtlasSpanDto(200, 4), [new AtlasSpanDto(201, 2)], null, null),
        new AtlasBoundsDto(AtlasBoundsDimension.SourceUtf16CodeUnits, 4, 4, 0,
            Encoding.UTF8.GetByteCount(text), AtlasDenominatorState.Known, 204, null, null, null),
        AtlasOutlineState.Available,
        null,
        [new AtlasOutlineRowDto("declaration-token", "M", AtlasDeclarationKind.Method, new AtlasSpanDto(200, 4))],
        new AtlasBoundsDto(AtlasBoundsDimension.OutlineRows, 8, 8, 1, 0,
            AtlasDenominatorState.Known, 1, null, null, null),
        null,
        new AtlasCoverageDto(AtlasDenominatorState.Known, 1, null),
        []);
}
