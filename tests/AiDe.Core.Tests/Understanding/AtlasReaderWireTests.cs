using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasReaderWireTests
{
    [Fact]
    public void PhysicalInventoryGoldenPreservesDirectoryChildRefusedAndUnavailable()
    {
        var native = new InventoryPage(
            new PageRequest(0, 8),
            new AtlasBounds(8, 4, 4, 9876, null, AtlasDenominatorState.Withheld,
                "inventory-partial", "unestablished-native-dimension"),
            [
                Entry("folder", "src", "root", AtlasDirectoryEntryKind.Directory, AtlasFileAvailability.Available, null),
                Entry("child", "src\\a.cs", "folder", AtlasDirectoryEntryKind.File, AtlasFileAvailability.Available, null),
                Entry("refused", "src\\link", "folder", AtlasDirectoryEntryKind.RejectedLink, AtlasFileAvailability.Refused, "link-refused"),
                Entry("missing", "src\\missing", "folder", AtlasDirectoryEntryKind.Unavailable, AtlasFileAvailability.Unavailable, "entry-unavailable"),
            ]);

        var dto = AtlasReaderProjection.Inventory(
            native, "scope-token", 7, "manifest-token", AtlasCompletionState.Partial,
            entry => $"token-{entry.FileValue}", parent => parent == "root" ? null : $"token-{parent}",
            _ => new AtlasCountDto(AtlasDenominatorState.Unknown, null, "total-not-recorded"), ["partial"]);
        var body = AtlasReaderProjection.SerializeInventory(dto);
        var remote = AtlasReaderProjection.DeserializeInventory(body);
        var golden = """
            {"version":1,"scopeToken":"scope-token","coreEpoch":7,"manifestToken":"manifest-token","completion":"Partial","files":[
            {"fileToken":"token-folder","kind":"Directory","parentToken":null,"relativePath":"src","classification":"Unknown","availability":"Available","declarationTotal":{"state":"Unknown","value":null,"reason":"total-not-recorded"},"reason":null},
            {"fileToken":"token-child","kind":"File","parentToken":"token-folder","relativePath":"src\\a.cs","classification":"Unknown","availability":"Available","declarationTotal":{"state":"Unknown","value":null,"reason":"total-not-recorded"},"reason":null},
            {"fileToken":"token-refused","kind":"RejectedLink","parentToken":"token-folder","relativePath":"src\\link","classification":"Unknown","availability":"Refused","declarationTotal":{"state":"Unknown","value":null,"reason":"total-not-recorded"},"reason":"link-refused"},
            {"fileToken":"token-missing","kind":"Unavailable","parentToken":"token-folder","relativePath":"src\\missing","classification":"Unknown","availability":"Unavailable","declarationTotal":{"state":"Unknown","value":null,"reason":"total-not-recorded"},"reason":"entry-unavailable"}],
            "bounds":{"dimension":"InventoryRows","requestedLimit":8,"effectiveLimit":4,"returnedRows":4,"returnedContentBytes":0,"denominatorState":"Withheld","denominatorValue":null,"denominatorReason":"total-withheld","omissionReason":"inventory-partial","omissionDimension":null},"nextOffset":null,"disclosures":["partial"]}
            """;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(golden), JsonNode.Parse(body)));
        Assert.Equal(body, AtlasReaderProjection.SerializeInventory(remote));
        Assert.Equal(dto.Files, remote.Files);
        Assert.Equal(dto.Bounds, remote.Bounds);
        Assert.True(body.Length > 0);
        Assert.Equal(0, remote.Bounds.ReturnedContentBytes);
        Assert.DoesNotContain("observedIdentity", Encoding.UTF8.GetString(body));
        Assert.DoesNotContain("parentPathKey", Encoding.UTF8.GetString(body));
    }

    [Theory]
    [InlineData(AtlasDenominatorState.Unknown, "total-not-recorded")]
    [InlineData(AtlasDenominatorState.Withheld, "total-withheld")]
    public void DenominatorExplanationDoesNotInventOmissionCausality(AtlasDenominatorState state, string reason)
    {
        var native = new AtlasBounds(8, 4, 0, 321, null, state, "descriptor-budget", "descriptors");
        var dto = AtlasReaderProjection.Bounds(
            native, new AtlasReaderPhaseContext(AtlasBoundsDimension.InventoryRows, 8, 4), 0, 0);
        Assert.Null(dto.DenominatorValue);
        Assert.Equal(reason, dto.DenominatorReason);
        Assert.Equal("descriptor-budget", dto.OmissionReason);
        Assert.Null(dto.OmissionDimension);
        Assert.Equal(0, dto.ReturnedContentBytes);
    }

    [Theory]
    [InlineData("unknown-as-zero")]
    [InlineData("retention-as-content")]
    [InlineData("lost-directory")]
    public void RetainedSemanticMutantsAreRejected(string mutation)
    {
        var json = JsonNode.Parse(AtlasReaderProjection.SerializeInventory(EmptyInventory()))!.AsObject();
        switch (mutation)
        {
            case "unknown-as-zero":
                json["bounds"]!["denominatorValue"] = 0;
                break;
            case "retention-as-content":
                json["bounds"]!["returnedContentBytes"] = 9876;
                break;
            case "lost-directory":
                json["bounds"]!["returnedRows"] = 1;
                break;
        }

        Assert.Throws<JsonException>(() =>
            AtlasReaderProjection.DeserializeInventory(Encoding.UTF8.GetBytes(json.ToJsonString())));
    }

    [Theory]
    [InlineData("\"Partial\"", "\"partial\"")]
    [InlineData("\"Partial\"", "1")]
    [InlineData("\"Partial\"", "\"1\"")]
    [InlineData("\"Partial\"", "\"FutureState\"")]
    [InlineData("\"nextOffset\":null,", "")]
    [InlineData("\"nextOffset\":null", "\"nextOffset\":null,\"nextOffset\":null")]
    [InlineData("\"nextOffset\":null", "\"nextOffset\":null,\"unexpected\":true")]
    [InlineData("\"completion\"", "\"Completion\"")]
    [InlineData("\"omissionDimension\":null", "\"omissionDimension\":null,\"omissionDimension\":null")]
    public void StrictBoundaryRejectsMalformedWire(string before, string after)
    {
        var valid = Encoding.UTF8.GetString(AtlasReaderProjection.SerializeInventory(EmptyInventory()));
        Assert.Contains(before, valid);
        Assert.Throws<JsonException>(() =>
            AtlasReaderProjection.DeserializeInventory(Encoding.UTF8.GetBytes(valid.Replace(before, after, StringComparison.Ordinal))));
    }

    [Fact]
    public void BoundaryRejectsExcessDepthAndBodyBytes()
    {
        var deep = Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33));
        Assert.ThrowsAny<JsonException>(() => AtlasReaderProjection.DeserializeInventory(deep));
        Assert.Throws<JsonException>(() =>
            AtlasReaderProjection.DeserializeInventory(new byte[AtlasReaderProjection.MaxFrameBodyBytes + 1]));
        Assert.Equal(1024 * 1024, AtlasReaderProjection.MaxFrameBodyBytes);
        Assert.Equal(4, AtlasReaderProjection.FramePrefixBytes);
        Assert.Equal(8 * 1024 * 1024, AtlasReaderProjection.MaxSourceInputBytes);
    }

    [Fact]
    public void SourceRangesAreUtf16CodeUnitsNotInventoryRowLimits()
    {
        var json = """
            {"version":1,"scopeToken":"scope","expectedCoreEpoch":3,"manifestToken":"manifest","fileToken":"file",
            "declarationToken":null,"sourceOffset":128,"sourceLength":4096,"outlineOffset":0,"outlineLimit":128}
            """;
        var request = AtlasReaderProjection.DeserializeSelect(Encoding.UTF8.GetBytes(json));
        Assert.Equal(4096, request.SourceLength);
        Assert.Equal(128, request.SourceOffset);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSelect(
            Encoding.UTF8.GetBytes(json.Replace("\"outlineLimit\":128", "\"outlineLimit\":129", StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSelect(
            Encoding.UTF8.GetBytes(json.Replace("\"sourceOffset\":128", "\"sourceOffset\":2147483647", StringComparison.Ordinal))));
    }

    [Fact]
    public void AllNativeNonMatchStatesPreserveStateAndRejectTextSmuggling()
    {
        SourceProjection[] states =
        [
            SourceProjection.Changed("observation"), SourceProjection.Unavailable("observation"),
            SourceProjection.Unverifiable("observation"), SourceProjection.UnsupportedEncoding("observation"),
            SourceProjection.TooLargeToVerify("observation"), SourceProjection.ReadUnstable("observation"),
            SourceProjection.Refused("observation"), SourceProjection.Canceled("observation"),
        ];
        Assert.Equal(9, Enum.GetValues<SourceProjectionState>().Length);
        foreach (var native in states)
        {
            var dto = AtlasReaderProjection.Source(
                native, "observation-token", _ => throw new InvalidOperationException("No binding may be issued."), null, 0);
            var remote = AtlasReaderProjection.DeserializeSource(AtlasReaderProjection.SerializeSource(dto));
            Assert.Equal(native.State, remote.State);
            Assert.Equal("observation-token", remote.ObservationToken);
            Assert.Equal("not-recorded", remote.Reason);
            Assert.Null(remote.BindingToken);
            Assert.Null(remote.PageSpan);
            Assert.Empty(remote.Highlights);
            var mutant = JsonNode.Parse(AtlasReaderProjection.SerializeSource(dto))!;
            mutant["text"] = "forbidden";
            Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSource(
                Encoding.UTF8.GetBytes(mutant.ToJsonString())));
        }
    }

    [Fact]
    public void IndexedWireUsesUtf16SpansAndUtf8DisplayBudget()
    {
        var dto = new AtlasSourceDto(SourceProjectionState.IndexedMatch, "observation-token",
            "binding-token", "utf8", "A😀é", new AtlasSpanDto(200, 4), [new AtlasSpanDto(201, 2)], 204, null);
        var body = AtlasReaderProjection.SerializeSource(dto);
        var remote = AtlasReaderProjection.DeserializeSource(body);
        var golden = """
            {"state":"IndexedMatch","observationToken":"observation-token","bindingToken":"binding-token",
            "decoderId":"utf8","text":"A😀é","pageSpan":{"start":200,"length":4},
            "highlights":[{"start":201,"length":2}],"nextOffset":204,"reason":null}
            """;
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(golden), JsonNode.Parse(body)));
        Assert.Equal(body, AtlasReaderProjection.SerializeSource(remote));
        Assert.Equal(dto.Text, remote.Text);
        Assert.Equal(dto.PageSpan, remote.PageSpan);
        Assert.Equal(dto.Highlights, remote.Highlights);
        Assert.Equal(204, remote.NextOffset);
        Assert.Equal("binding-token", remote.BindingToken);
        Assert.Equal(7, Encoding.UTF8.GetByteCount(remote.Text!));
        var exact = dto with
        {
            Text = new string('é', 65536), PageSpan = new AtlasSpanDto(0, 65536), Highlights = [], NextOffset = null,
        };
        Assert.NotEmpty(AtlasReaderProjection.SerializeSource(exact));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(
            exact with { Text = exact.Text + "é", PageSpan = new AtlasSpanDto(0, 65537) }));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(dto with { PageSpan = new AtlasSpanDto(200, 7) }));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(dto with { Highlights = [new AtlasSpanDto(199, 2)] }));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(dto with { Text = "\ud800", PageSpan = new AtlasSpanDto(0, 1) }));
    }

    [Fact]
    public void ProjectionRejectsNativeRowCountMismatchAndSanitizesMissingDetails()
    {
        var native = new AtlasBounds(8, 4, 1, 321, 1, AtlasDenominatorState.Known, null, null);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Bounds(
            native, new AtlasReaderPhaseContext(AtlasBoundsDimension.InventoryRows, 8, 4), 0, 0));
        var file = AtlasReaderProjection.File(
            Entry("file", "a.cs", "root", AtlasDirectoryEntryKind.Unavailable,
                AtlasFileAvailability.Unavailable, "C:\\private\\root"),
            "file-token", null, new AtlasCountDto(AtlasDenominatorState.Unknown, null, "not-recorded"));
        Assert.Equal("not-recorded", file.Reason);
    }

    [Theory]
    [InlineData("files", AtlasBoundsDimension.InventoryRows, AtlasBoundsDimension.InventoryRows)]
    [InlineData("page", AtlasBoundsDimension.InventoryRows, AtlasBoundsDimension.InventoryRows)]
    [InlineData("declarations", AtlasBoundsDimension.OutlineRows, AtlasBoundsDimension.OutlineRows)]
    [InlineData("membership", AtlasBoundsDimension.InventoryRows, null)]
    [InlineData("selection", AtlasBoundsDimension.OutlineRows, null)]
    public void MapsOnlyEstablishedNativeOmissionDimensions(
        string nativeDimension, AtlasBoundsDimension phase, AtlasBoundsDimension? expected)
    {
        var native = new AtlasBounds(128, 128, 1, 600, null,
            AtlasDenominatorState.Unknown, "page-omitted", nativeDimension);
        var dto = AtlasReaderProjection.Bounds(native, new AtlasReaderPhaseContext(phase, 128, 128), 1, 0);
        Assert.Equal(expected, dto.OmissionDimension);
        Assert.Equal(phase, dto.Dimension);
        Assert.Equal(128, dto.RequestedLimit);
        Assert.Equal(128, dto.EffectiveLimit);
        Assert.Equal(1, dto.ReturnedRows);
        Assert.Equal(0, dto.ReturnedContentBytes);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Bounds(
            native, new AtlasReaderPhaseContext(AtlasBoundsDimension.SourceUtf16CodeUnits, 128, 128), 1, 0));
    }

    [Fact]
    public void SourceProjectionChecksProducerContentBytesAndNoMatchContinuation()
    {
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Source(
            SourceProjection.Changed("observation"), "observation-token",
            _ => throw new InvalidOperationException(), null, 1));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Source(
            SourceProjection.Changed("observation"), "observation-token",
            _ => throw new InvalidOperationException(), 1, 0));
    }

    [Fact]
    public void ReleasedDtoConstructorsArePublicWithoutNativeAuthorityFields()
    {
        Type[] contracts =
        [
            typeof(AtlasCapabilitiesRequestDto), typeof(AtlasCapabilitiesDto),
            typeof(AtlasAdmitRequestDto), typeof(AtlasAdmitDto), typeof(AtlasInventoryRequestDto),
            typeof(AtlasSelectRequestDto), typeof(AtlasRestoreRequestDto), typeof(AtlasReleaseRequestDto),
            typeof(AtlasReleasedDto), typeof(AtlasInventoryPageDto), typeof(AtlasFileDto),
            typeof(AtlasSelectionDto), typeof(AtlasSourceDto), typeof(AtlasOutlineRowDto),
            typeof(AtlasSpanDto), typeof(AtlasCountDto), typeof(AtlasCoverageDto), typeof(AtlasBoundsDto),
        ];
        foreach (var type in contracts)
        {
            Assert.True(type.IsVisible);
            Assert.NotEmpty(type.GetConstructors());
            Assert.DoesNotContain(type.GetProperties(), property =>
                property.PropertyType == typeof(AtlasRootGrant)
                || property.PropertyType == typeof(AtlasObjectIdentity)
                || property.PropertyType == typeof(AtlasSourceBinding));
        }

        Assert.True(typeof(AtlasReaderProjection).IsVisible);
        Assert.True(typeof(AtlasBoundsDimension).IsVisible);
        Assert.True(typeof(AtlasOutlineState).IsVisible);
    }

    private static AtlasInventoryPageDto EmptyInventory() => new(
        1, "scope", 3, "manifest", AtlasCompletionState.Partial, [],
        new AtlasBoundsDto(AtlasBoundsDimension.InventoryRows, 8, 4, 0, 0,
            AtlasDenominatorState.Unknown, null, "total-not-recorded", "partial", null),
        null, []);

    private static AtlasFileEntry Entry(
        string id, string path, string parent, AtlasDirectoryEntryKind kind,
        AtlasFileAvailability availability, string? reason) =>
        new(id, path, parent, kind, AtlasFileClassification.Unknown, null, availability, reason);
}
