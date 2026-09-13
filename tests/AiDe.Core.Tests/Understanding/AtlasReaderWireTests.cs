using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasReaderWireTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Owner42StandaloneEmptyIndexedSourceCannotContinue(bool deserialize)
    {
        var source = new AtlasSourceDto(SourceProjectionState.IndexedMatch, "observation", "binding",
            "utf8", string.Empty, new AtlasSpanDto(0, 0), [], 0, null);
        if (deserialize)
        {
            const string json = """
                {"state":"IndexedMatch","observationToken":"observation","bindingToken":"binding",
                "decoderId":"utf8","text":"","pageSpan":{"start":0,"length":0},
                "highlights":[],"nextOffset":0,"reason":null}
                """;
            Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSource(Encoding.UTF8.GetBytes(json)));
        }
        else
        {
            Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(source));
        }
    }

    [Fact]
    public void Owner42StandaloneEmptyIndexedSourceWithoutContinuationRoundTrips()
    {
        var source = new AtlasSourceDto(SourceProjectionState.IndexedMatch, "observation", "binding",
            "utf8", string.Empty, new AtlasSpanDto(0, 0), [], null, null);
        var body = AtlasReaderProjection.SerializeSource(source);
        var remote = AtlasReaderProjection.DeserializeSource(body);
        Assert.Equal(string.Empty, remote.Text);
        Assert.Equal(new AtlasSpanDto(0, 0), remote.PageSpan);
        Assert.Null(remote.NextOffset);
        Assert.Empty(remote.Highlights);
        Assert.Equal(body, AtlasReaderProjection.SerializeSource(remote));
    }

    [Fact]
    public void Owner41RejectsZeroSourceLength()
    {
        const string json = """
            {"version":1,"scopeToken":"scope","expectedCoreEpoch":3,"manifestToken":"manifest","fileToken":"file",
            "declarationToken":null,"sourceOffset":0,"sourceLength":0,"outlineOffset":0,"outlineLimit":8}
            """;
        Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSelect(Encoding.UTF8.GetBytes(json)));
    }

    [Theory]
    [InlineData(202, 1)]
    [InlineData(201, 1)]
    public void Owner41RejectsSplitSurrogateHighlight(int start, int length)
    {
        var source = new AtlasSourceDto(SourceProjectionState.IndexedMatch, "observation", "binding",
            "utf8", "A😀é", new AtlasSpanDto(200, 4), [new AtlasSpanDto(start, length)], null, null);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(source));
    }

    [Fact]
    public void Owner41RejectsEmptyInventoryContinuation()
    {
        Assert.Throws<JsonException>(() =>
            AtlasReaderProjection.SerializeInventory(EmptyInventory() with { NextOffset = 0 }));
    }

    [Fact]
    public void Owner41RejectsNativeInventoryContentBytes()
    {
        var native = new AtlasBounds(8, 4, 0, 9876, 0, AtlasDenominatorState.Known, null, null);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Bounds(native,
            new AtlasReaderPhaseContext(AtlasBoundsDimension.InventoryRows, 8, 4), 0, 0));
    }

    [Theory]
    [InlineData("ascii")]
    [InlineData("multibyte")]
    public void Owner41Preserves256ByteOpaqueHandles(string kind)
    {
        var token = kind == "ascii" ? new string('x', 256) : new string('é', 128);
        var source = new AtlasSourceDto(SourceProjectionState.Refused, token, null, null, null, null, [], null, "not recorded");
        var remote = AtlasReaderProjection.DeserializeSource(AtlasReaderProjection.SerializeSource(source));
        Assert.Equal(token, remote.ObservationToken);
    }

    [Fact]
    public void Owner41RejectsKnownDenominatorReason()
    {
        var inventory = EmptyInventory();
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeInventory(inventory with
        {
            Bounds = inventory.Bounds with { DenominatorState = AtlasDenominatorState.Known, DenominatorValue = 0 },
        }));
    }

    [Fact]
    public void Owner41RejectsIndexedReasonAndMissingNonMatchReason()
    {
        var source = new AtlasSourceDto(SourceProjectionState.IndexedMatch, "observation", "binding",
            "utf8", "a", new AtlasSpanDto(0, 1), [], null, "not recorded");
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(source));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(source with
        {
            State = SourceProjectionState.Refused, BindingToken = null, DecoderId = null,
            Text = null, PageSpan = null, Reason = null,
        }));
    }

    [Fact]
    public void PhysicalInventoryGoldenPreservesDirectoryChildRefusedAndUnavailable()
    {
        var native = new InventoryPage(
            new PageRequest(0, 8),
            new AtlasBounds(8, 4, 4, 0, null, AtlasDenominatorState.Withheld,
                "inventory-partial", "unestablished-native-dimension"),
            [
                Entry("folder", "src", "root", AtlasDirectoryEntryKind.Directory, AtlasFileAvailability.Available, null),
                Entry("child", "src\\a.cs", "folder", AtlasDirectoryEntryKind.File, AtlasFileAvailability.Available, null),
                Entry("refused", "src\\link", "folder", AtlasDirectoryEntryKind.RejectedLink, AtlasFileAvailability.Refused, "link-refused"),
                Entry("missing", "src\\missing", "folder", AtlasDirectoryEntryKind.Unavailable, AtlasFileAvailability.Unavailable, "entry-unavailable"),
            ]);

        var request = new AtlasInventoryRequestDto(1, "scope-token", 7, "manifest-token", 0, 8);
        var dto = AtlasReaderProjection.Inventory(
            native, request, 7, AtlasCompletionState.Partial,
            entry => $"token-{entry.FileValue}", parent => parent == "root" ? null : $"token-{parent}",
            _ => new AtlasCountDto(AtlasDenominatorState.Unknown, null, "total not recorded"), ["partial"]);
        var body = AtlasReaderProjection.SerializeInventory(dto, request);
        var remote = AtlasReaderProjection.DeserializeInventory(body, request);
        var golden = """
            {"version":1,"scopeToken":"scope-token","coreEpoch":7,"manifestToken":"manifest-token","completion":"Partial","files":[
            {"fileToken":"token-folder","kind":"Directory","parentToken":null,"relativePath":"src","classification":"Unknown","availability":"Available","declarationTotal":{"state":"Unknown","value":null,"reason":"total not recorded"},"reason":null},
            {"fileToken":"token-child","kind":"File","parentToken":"token-folder","relativePath":"src\\a.cs","classification":"Unknown","availability":"Available","declarationTotal":{"state":"Unknown","value":null,"reason":"total not recorded"},"reason":null},
            {"fileToken":"token-refused","kind":"RejectedLink","parentToken":"token-folder","relativePath":"src\\link","classification":"Unknown","availability":"Refused","declarationTotal":{"state":"Unknown","value":null,"reason":"total not recorded"},"reason":"link refused"},
            {"fileToken":"token-missing","kind":"Unavailable","parentToken":"token-folder","relativePath":"src\\missing","classification":"Unknown","availability":"Unavailable","declarationTotal":{"state":"Unknown","value":null,"reason":"total not recorded"},"reason":"entry unavailable"}],
            "bounds":{"dimension":"InventoryRows","requestedLimit":8,"effectiveLimit":4,"returnedRows":4,"returnedContentBytes":0,"denominatorState":"Withheld","denominatorValue":null,"denominatorReason":"total withheld","omissionReason":"inventory partial","omissionDimension":null},"nextOffset":null,"disclosures":["partial"]}
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
    [InlineData(AtlasDenominatorState.Unknown, "total not recorded")]
    [InlineData(AtlasDenominatorState.Withheld, "total withheld")]
    public void DenominatorExplanationDoesNotInventOmissionCausality(AtlasDenominatorState state, string reason)
    {
        var native = new AtlasBounds(8, 4, 0, 0, null, state, "descriptor-budget", "descriptors");
        var dto = AtlasReaderProjection.Bounds(
            native, new AtlasReaderPhaseContext(AtlasBoundsDimension.InventoryRows, 8, 4), 0, 0);
        Assert.Null(dto.DenominatorValue);
        Assert.Equal(reason, dto.DenominatorReason);
        Assert.Equal("descriptor budget", dto.OmissionReason);
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
            Assert.Equal("not recorded", remote.Reason);
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
            "file-token", null, new AtlasCountDto(AtlasDenominatorState.Unknown, null, "not recorded"));
        Assert.Equal("detail redacted", file.Reason);
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
        var native = new AtlasBounds(128, 128, 1, 0, null,
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
            AtlasDenominatorState.Unknown, null, "total not recorded", "partial", null),
        null, []);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeSelectionGoldenPreservesVerifiedSourceThroughOutlineFailure(bool outlineFailed)
    {
        var (native, request, phase, binding) = NativeSelection();
        if (outlineFailed)
        {
            phase = phase with { OutlineState = AtlasOutlineState.BudgetExceeded, OutlineReason = "outline budget exceeded" };
        }

        var bindingTokens = new Dictionary<AtlasSourceBinding, string> { [binding] = "binding-token" };
        var dto = AtlasReaderProjection.Selection(native, request, phase,
            value => bindingTokens[value], _ => "declaration-token");
        var body = AtlasReaderProjection.SerializeSelection(dto, request);
        var remote = AtlasReaderProjection.DeserializeSelection(body, request);
        const string golden = """
            {"version":1,"scopeToken":"scope-token","coreEpoch":7,"manifestToken":"manifest-token","fileToken":"file-token",
            "declarationToken":"declaration-token","receiptToken":"receipt-token",
            "source":{"state":"IndexedMatch","observationToken":"observation-token","bindingToken":"binding-token","decoderId":"utf8",
            "text":"A😀é","pageSpan":{"start":200,"length":4},"highlights":[{"start":201,"length":2}],"nextOffset":null,"reason":null},
            "sourceBounds":{"dimension":"SourceUtf16CodeUnits","requestedLimit":4,"effectiveLimit":4,"returnedRows":0,
            "returnedContentBytes":7,"denominatorState":"Known","denominatorValue":204,"denominatorReason":null,"omissionReason":null,"omissionDimension":null},
            "outlineState":"Available","outlineReason":null,
            "outline":[{"declarationToken":"declaration-token","displayName":"M","kind":"Method","span":{"start":200,"length":4}}],
            "outlineBounds":{"dimension":"OutlineRows","requestedLimit":8,"effectiveLimit":8,"returnedRows":1,"returnedContentBytes":0,
            "denominatorState":"Known","denominatorValue":1,"denominatorReason":null,"omissionReason":null,"omissionDimension":null},
            "outlineNextOffset":null,"coverage":{"state":"Known","value":1,"reason":null},"disclosures":[]}
            """;
        var expected = JsonNode.Parse(golden)!;
        if (outlineFailed)
        {
            expected["outlineState"] = "BudgetExceeded";
            expected["outlineReason"] = "outline budget exceeded";
            expected["outline"] = new JsonArray();
            expected["outlineBounds"]!["effectiveLimit"] = 0;
            expected["outlineBounds"]!["returnedRows"] = 0;
            expected["outlineBounds"]!["omissionReason"] = "outline budget exceeded";
        }

        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(body)));
        Assert.Equal(body, AtlasReaderProjection.SerializeSelection(remote, request));
        Assert.Equal(SourceProjectionState.IndexedMatch, remote.Source.State);
        Assert.Equal("A😀é", remote.Source.Text);
        Assert.Equal(native.Bounds.ReturnedBytes, remote.SourceBounds.ReturnedContentBytes);
        Assert.Equal(0, remote.OutlineBounds.ReturnedContentBytes);
        var json = Encoding.UTF8.GetString(body);
        Assert.DoesNotContain("native-", json);
        Assert.DoesNotContain("expectedBinding", json);
    }

    [Fact]
    public void SelectionRejectsInconsistentNativeContentBytesAndResolvedIdentity()
    {
        var (native, request, phase, _) = NativeSelection();
        var inconsistent = new SelectionProjection(native.ReceiptToken, native.ManifestToken, native.FileValue,
            native.Generation, native.Outline, native.Source,
            new AtlasBounds(128, 128, 1, 8, 1, AtlasDenominatorState.Known, null, null), native.Coverage, []);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Selection(
            inconsistent, request, phase, _ => "binding-token", _ => "declaration-token"));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Selection(
            native, request, phase with { NativeManifestToken = "other-native-manifest" }, _ => "binding-token", _ => "declaration-token"));
    }

    [Fact]
    public void Owner41ProjectsRequestedWindowFromVerifiedNativePage()
    {
        var (native, request, phase, _) = NativeSelection(new string('x', 200) + "A😀é", 200, 4);
        var dto = AtlasReaderProjection.Selection(native, request, phase, _ => "binding-token", _ => "declaration-token");
        Assert.Equal(207, native.Bounds.ReturnedBytes);
        Assert.Equal("A😀é", dto.Source.Text);
        Assert.Equal(new AtlasSpanDto(200, 4), dto.Source.PageSpan);
        Assert.Equal(7, dto.SourceBounds.ReturnedContentBytes);
        Assert.NotEmpty(AtlasReaderProjection.SerializeSelection(dto, request));
    }

    [Fact]
    public void Owner41ClampsUtf8DisplayWithoutConfusingNativeAggregateBytes()
    {
        var (native, request, phase, _) = NativeSelection(new string('é', 65537), 0, 100000, 65536);
        var dto = AtlasReaderProjection.Selection(native, request, phase, _ => "binding-token", _ => "declaration-token");
        Assert.Equal(131074, native.Bounds.ReturnedBytes);
        Assert.Equal(65536, dto.Source.Text!.Length);
        Assert.Equal(131072, dto.SourceBounds.ReturnedContentBytes);
        Assert.Equal(65536, dto.SourceBounds.EffectiveLimit);
        Assert.Equal(100000, dto.SourceBounds.RequestedLimit);
        Assert.Equal(AtlasBoundsDimension.SourceUtf8ContentBytes, dto.SourceBounds.OmissionDimension);
        Assert.Equal("source display limit", dto.SourceBounds.OmissionReason);
        Assert.Equal(65536, dto.Source.NextOffset);
        Assert.NotEmpty(AtlasReaderProjection.SerializeSelection(dto, request));
    }

    [Fact]
    public void Owner41AppliesOutlineRequestOffsetAndLimitToNativeRows()
    {
        var (native, request, phase, _) = NativeSelection();
        native = new SelectionProjection(native.ReceiptToken, native.ManifestToken, native.FileValue,
            native.Generation,
            new SelectionOutline([
                new OutlineDeclaration("first", "First", AtlasDeclarationKind.Method, new AtlasTextSpan(0, 1)),
                new OutlineDeclaration("second", "Second", AtlasDeclarationKind.Method, new AtlasTextSpan(200, 4))]),
            native.Source, new AtlasBounds(128, 128, 2, 7, 2, AtlasDenominatorState.Known, null, null), native.Coverage, []);
        request = request with { OutlineOffset = 1, OutlineLimit = 1 };
        var dto = AtlasReaderProjection.Selection(native, request, phase, _ => "binding-token", row => row.ObservationKey);
        Assert.Equal("Second", Assert.Single(dto.Outline).DisplayName);
        Assert.Equal(1, dto.OutlineBounds.ReturnedRows);
        Assert.Equal(2, dto.OutlineBounds.DenominatorValue);
        Assert.NotEmpty(AtlasReaderProjection.SerializeSelection(dto, request));
    }

    [Fact]
    public void SourceWindowCutsOnlyBetweenScalarsAndRejectsLowHalfStarts()
    {
        var (native, request, phase, _) = NativeSelection("A😀é", 0, 2, 1);
        var dto = AtlasReaderProjection.Selection(native, request, phase, _ => "binding-token", _ => "declaration-token");
        Assert.Equal("A", dto.Source.Text);
        Assert.Equal(1, dto.SourceBounds.EffectiveLimit);
        Assert.Equal(1, dto.Source.NextOffset);
        Assert.Equal(AtlasBoundsDimension.SourceUtf16CodeUnits, dto.SourceBounds.OmissionDimension);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Selection(
            native, request with { SourceOffset = 2, SourceLength = 1 }, phase with { SourceNextOffset = null },
            _ => "binding-token", _ => "declaration-token"));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.Selection(
            native, request with { SourceOffset = 1, SourceLength = 1 }, phase with { SourceNextOffset = null },
            _ => "binding-token", _ => "declaration-token"));
    }

    [Fact]
    public void SelectionBoundaryChecksOriginalRequestAndAllContinuationExtents()
    {
        var (native, request, phase, _) = NativeSelection();
        var dto = AtlasReaderProjection.Selection(native, request, phase, _ => "binding-token", _ => "declaration-token");
        var body = AtlasReaderProjection.SerializeSelection(dto, request);
        AtlasSelectRequestDto[] wrongRequests =
        [
            request with { ScopeToken = "other" }, request with { ManifestToken = "other" },
            request with { FileToken = "other" }, request with { DeclarationToken = null },
            request with { ExpectedCoreEpoch = 8 }, request with { SourceOffset = 201 },
            request with { SourceLength = 8 }, request with { OutlineOffset = 1 }, request with { OutlineLimit = 4 },
        ];
        foreach (var wrong in wrongRequests)
        {
            Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeSelection(body, wrong));
        }

        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSelection(dto with
        {
            Source = dto.Source with { NextOffset = 204 },
        }, request));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSelection(dto with { OutlineNextOffset = 1 }, request));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSelection(dto with
        {
            SourceBounds = dto.SourceBounds with { ReturnedContentBytes = 8 },
        }, request));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSelection(dto with
        {
            Coverage = new AtlasCoverageDto(AtlasDenominatorState.Known, double.NaN, null),
        }, request));
        var continuation = dto with
        {
            Source = dto.Source with { NextOffset = 204 },
            SourceBounds = dto.SourceBounds with { DenominatorValue = 208 },
            OutlineNextOffset = 1,
            OutlineBounds = dto.OutlineBounds with { DenominatorValue = 2 },
        };
        Assert.NotEmpty(AtlasReaderProjection.SerializeSelection(continuation, request));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSelection(continuation with
        {
            Source = continuation.Source with { NextOffset = 203 },
        }, request));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSelection(continuation with { OutlineNextOffset = 2 }, request));
    }

    [Fact]
    public void InventoryBoundaryRequiresTheActualRequestNotJustJsonShape()
    {
        var request = new AtlasInventoryRequestDto(1, "scope", 3, "manifest", 10, 2);
        var dto = EmptyInventory() with
        {
            Files = [new AtlasFileDto("file", AtlasDirectoryEntryKind.File, null, "a.cs",
                AtlasFileClassification.CSharp, AtlasFileAvailability.Available,
                new AtlasCountDto(AtlasDenominatorState.Known, 0, null), null)],
            Bounds = new AtlasBoundsDto(AtlasBoundsDimension.InventoryRows, 2, 2, 1, 0,
                AtlasDenominatorState.Known, 12, null, "page omitted", AtlasBoundsDimension.InventoryRows),
            NextOffset = 11,
        };
        var body = AtlasReaderProjection.SerializeInventory(dto, request);
        Assert.Equal(11, AtlasReaderProjection.DeserializeInventory(body, request).NextOffset);
        foreach (var wrong in new[]
        {
            request with { Offset = 9 }, request with { ScopeToken = "other" },
            request with { ManifestToken = "other" }, request with { ExpectedCoreEpoch = 4 }, request with { Limit = 3 },
        })
        {
            Assert.Throws<JsonException>(() => AtlasReaderProjection.DeserializeInventory(body, wrong));
        }

        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeInventory(dto with { NextOffset = 10 }, request));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeInventory(dto with { NextOffset = 12 }, request));
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeInventory(dto with
        {
            Bounds = dto.Bounds with { DenominatorValue = 11 },
        }, request));
    }

    [Theory]
    [InlineData("ascii-over")]
    [InlineData("multibyte-over")]
    [InlineData("malformed")]
    [InlineData("blank")]
    public void HandlesRejectByteOverflowInvalidUnicodeAndBlank(string kind)
    {
        var token = kind switch
        {
            "ascii-over" => new string('x', 257),
            "multibyte-over" => new string('é', 129),
            "malformed" => "\ud800",
            _ => " ",
        };
        var dto = new AtlasSourceDto(SourceProjectionState.Refused, token, null, null, null, null, [], null, "not recorded");
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(dto));
    }

    [Fact]
    public void OpaqueHandlesPreserveOrdinalUnicodeAndSafeReasonsDoNotEchoDetails()
    {
        var token = " e\u0301 /😀 ";
        var dto = new AtlasSourceDto(SourceProjectionState.Refused, token, null, null, null, null, [], null, "not recorded");
        Assert.Equal(token, AtlasReaderProjection.DeserializeSource(AtlasReaderProjection.SerializeSource(dto)).ObservationToken);
        var missing = AtlasReaderProjection.File(Entry("file", "a.cs", "root",
            AtlasDirectoryEntryKind.Unavailable, AtlasFileAvailability.Unavailable, null), "file", null,
            new AtlasCountDto(AtlasDenominatorState.Withheld, null, "total withheld"));
        var redacted = AtlasReaderProjection.File(Entry("file", "a.cs", "root",
            AtlasDirectoryEntryKind.Unavailable, AtlasFileAvailability.Unavailable, "secret-token stderr /private"),
            "file", null, missing.DeclarationTotal);
        Assert.Equal("not recorded", missing.Reason);
        Assert.Equal("detail redacted", redacted.Reason);
        Assert.Throws<JsonException>(() => AtlasReaderProjection.SerializeSource(dto with { Reason = token }));
    }

    [Fact]
    public void NonfriendConsumerCompilesPortsDtosAndExplicitFacadeSignatureFixture()
    {
        const string assemblyName = "Atlas.Reader.NonFriend.CompileProof";
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using AiDe.Core;
            using AiDe.Core.Ipc;
            using AiDe.Core.Understanding;
            public static class Consumer
            {
                public static async ValueTask<AtlasSelectionDto> Read(
                    IAtlasWorkspaceReader workspace, AtlasSelectRequestDto request, CancellationToken cancellation)
                {
                    await using IAtlasReaderLease lease = await workspace.AdmitAsync(cancellation);
                    _ = lease.ScopeToken;
                    _ = lease.InitialManifestToken;
                    _ = lease.CoreEpoch;
                    _ = lease.ExpiresAt;
                    _ = lease.Invalidated;
                    _ = lease.IsTerminal;
                    IAtlasReaderQueries queries = lease.Queries;
                    return await queries.SelectAsync(request, cancellation);
                }
                public static AtlasCountDto Count() => new(AtlasDenominatorState.Unknown, null, "total not recorded");
                public static ValueTask<AtlasInventoryPageDto> Inventory(
                    IAtlasReaderQueries queries, AtlasInventoryRequestDto request, CancellationToken cancellation) =>
                    queries.InventoryAsync(request, cancellation);
                public static ValueTask<AtlasSelectionDto> Restore(
                    IAtlasReaderQueries queries, AtlasRestoreRequestDto request, CancellationToken cancellation) =>
                    queries.RestoreAsync(request, cancellation);
            }
            // Compile-only proposed facade signature. No production Register implementation is claimed.
            public interface IAtlasWorkspaceRegistrationFixture
            {
                static abstract IAsyncDisposable Register(
                    DaemonEndpoint endpoint, WorkspaceCore core, string workspaceId, string sourceRootPath,
                    string dataDirectory, string approvedGitExecutablePath, string approvedGitSha256, string approvedGitVersion);
            }
            """;
        var coreAssembly = typeof(AtlasReaderProjection).Assembly;
        Assert.DoesNotContain(coreAssembly.GetCustomAttributes(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute), false),
            attribute => ((System.Runtime.CompilerServices.InternalsVisibleToAttribute)attribute).AssemblyName.Split(',')[0] == assemblyName);
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(coreAssembly.Location).Distinct(StringComparer.OrdinalIgnoreCase);
        var references = paths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        var compilation = CSharpCompilation.Create(assemblyName, [CSharpSyntaxTree.ParseText(source)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var output = new MemoryStream();
        var emitted = compilation.Emit(output);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        Assert.True(output.Length > 0);
        var hidden = coreAssembly.GetTypes().First(type => !type.IsPublic && !type.IsNested
            && !type.IsGenericType && !type.Name.Contains('<'));
        var forbidden = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(
            $"public class Forbidden {{ public {hidden.FullName} Value; }}"));
        Assert.Contains(forbidden.GetDiagnostics(), diagnostic => diagnostic.Id == "CS0122");
    }

    private static (SelectionProjection Native, AtlasSelectRequestDto Request,
        AtlasSelectionPhaseContext Phase, AtlasSourceBinding Binding) NativeSelection(
            string? nativePageText = null, int offset = 200, int length = 4, int? nextOffset = null)
    {
        const string text = "A😀é";
        var fullText = nativePageText ?? new string('x', 200) + text;
        var hash = "sha256:" + Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(fullText))).ToLowerInvariant();
        var root = new AtlasObjectIdentity("volume", "root");
        var file = new AtlasObjectIdentity("volume", "file");
        var binding = AtlasSourceBinding.Create("native-manifest", "native-file", "policy",
            AtlasIdentityCodec.ForNativeObject(root), AtlasIdentityCodec.ForNativeObject(file), hash);
        var observation = new AtlasSourceObservation("native-observation", "native-manifest", "native-file",
            "policy", root, file, hash, Encoding.UTF8.GetByteCount(fullText), "utf8", fullText.Length,
            AtlasSourceObservationStatus.Verified,
            new AtlasBounds(1, 1, 1, Encoding.UTF8.GetByteCount(fullText), 1, AtlasDenominatorState.Known, null, null));
        var source = SourceProjection.IndexedMatch(observation, binding, "utf8",
            new SourceTextPage(nativePageText ?? text,
                new AtlasTextSpan(nativePageText is null ? 200 : 0, (nativePageText ?? text).Length),
                nativePageText is null ? [new AtlasTextSpan(201, 2)] : []));
        var native = new SelectionProjection("native-receipt", "native-manifest", "native-file", 7,
            new SelectionOutline([new OutlineDeclaration("native-declaration", "M", AtlasDeclarationKind.Method,
                new AtlasTextSpan(nativePageText is null ? 200 : 0, Math.Min(4, fullText.Length)))]),
            source, new AtlasBounds(128, 128, 1, Encoding.UTF8.GetByteCount(nativePageText ?? text),
                1, AtlasDenominatorState.Known, null, null), SelectionCoverage.Known(1), []);
        var request = new AtlasSelectRequestDto(1, "scope-token", 7, "manifest-token", "file-token",
            "declaration-token", offset, length, 0, 8);
        var phase = new AtlasSelectionPhaseContext("native-manifest", "native-file", 7,
            "observation-token", "receipt-token", new AtlasCountDto(AtlasDenominatorState.Known, fullText.Length, null),
            nextOffset, null, null, AtlasOutlineState.Available, null, null);
        return (native, request, phase, binding);
    }

    private static AtlasFileEntry Entry(
        string id, string path, string parent, AtlasDirectoryEntryKind kind,
        AtlasFileAvailability availability, string? reason) =>
        new(id, path, parent, kind, AtlasFileClassification.Unknown, null, availability, reason);
}
