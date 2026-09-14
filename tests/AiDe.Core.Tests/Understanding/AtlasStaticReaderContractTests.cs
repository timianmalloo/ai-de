using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasStaticReaderContractTests
{
    [Fact]
    public void Capabilities_CurrentWireAdvertisesOnlyLegacyE0Features()
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
        var body = Encoding.UTF8.GetString(AtlasReaderProjection.SerializeSelection(CurrentSelection(), CurrentSelectRequest()));
        var mutated = null == after
            ? body.Replace("\"declarationToken\"", before + "\"declarationToken\"", StringComparison.Ordinal)
            : body.Replace(before, after, StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            AtlasReaderProjection.DeserializeSelection(Encoding.UTF8.GetBytes(mutated), CurrentSelectRequest()));
    }

    [Fact]
    public void Restore_CurrentContractHasNoOptInAndSelectionValidationUsesOriginalRequest()
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
    public void PublicationBudget_CurrentBaselineKeepsEscapedBodyWithinFrameAndMetadataContentAtZero()
    {
        const string escapedText = @"A\Bé";
        var body = AtlasReaderProjection.SerializeSelection(CurrentSelection(escapedText), CurrentSelectRequest());
        var remote = AtlasReaderProjection.DeserializeSelection(body, CurrentSelectRequest());

        Assert.True(body.Length + AtlasReaderProjection.FramePrefixBytes <= AtlasReaderProjection.MaxFrameBodyBytes);
        Assert.Equal(sizeof(int), AtlasReaderProjection.FramePrefixBytes);
        Assert.Equal(1024 * 1024, AtlasReaderProjection.MaxFrameBodyBytes);
        Assert.Equal(128 * 1024, AtlasReaderProjection.MaxPageTextUtf8Bytes);
        Assert.Equal(Encoding.UTF8.GetByteCount(escapedText), remote.SourceBounds.ReturnedContentBytes);
        Assert.Equal(0, remote.OutlineBounds.ReturnedContentBytes);
        Assert.Equal(0, remote.OutlineBounds.ReturnedRows - remote.Outline.Length);
    }

    [Fact]
    public async Task ReadBudget_CurrentBaselineReportsReservationsNotHeapAndDrainsExactly()
    {
        var budget = new AtlasReadBudget();
        var active = new List<AtlasReadBudget.Reservation>();
        using var cancellation = new CancellationTokenSource();
        var pending = new List<Task<AtlasReadBudget.Reservation>>();

        try
        {
            for (var i = 0; i < 4; i++) active.Add(await budget.EnterAsync(cancellation.Token));
            for (var i = 0; i < 16; i++) pending.Add(budget.EnterAsync(cancellation.Token).AsTask());

            var held = budget.Read();

            Assert.Equal(4, held.Active);
            Assert.Equal(16, held.Pending);
            Assert.True(held.Owned <= AtlasReadBudget.MaxOwnedBytes);
            Assert.Throws<AtlasReadException>(() => budget.EnterAsync(cancellation.Token));
        }
        finally
        {
            cancellation.Cancel();
            foreach (var task in pending)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
            foreach (var reservation in active) reservation.Dispose();
        }

        Assert.Equal((0, 0, 0, 0L, 0L), budget.Read());
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
