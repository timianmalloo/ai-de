using AiDe.Core.Understanding;

namespace AiDe.Core.Tests.Understanding;

public sealed class AtlasQueryContractsTests
{
    [Fact]
    public void Create_PageRequest_RequiresBoundedLimit()
    {
        Assert.Equal(128, PageRequest.MaxLimit);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(0, 129));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PageRequest(-1, 1));
    }

    [Fact]
    public void Create_DefaultEnumerationLimits_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnumerationLimits(0, 64, 128, TimeSpan.FromSeconds(30)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnumerationLimits(1, 0, 128, TimeSpan.FromSeconds(30)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnumerationLimits(1, 64, 0, TimeSpan.FromSeconds(30)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnumerationLimits(1, 64, 128, TimeSpan.Zero));
    }

    [Fact]
    public void Create_InventoryPage_FilesMustMatchBoundsReturnedRows()
    {
        var file = File("file:value");
        var request = new PageRequest(0, 10);

        Assert.Throws<ArgumentException>(() => new InventoryPage(request, new AtlasBounds(10, 10, 2, 20, 2, AtlasDenominatorState.Known, null, null), [file]));
        var empty = new InventoryPage(request, new AtlasBounds(10, 10, 0, 0, 0, AtlasDenominatorState.Known, null, null), []);
        Assert.Empty(empty.Files);
        Assert.Null(empty.NextOffset);
    }

    [Fact]
    public void Create_InventoryPage_AcceptsKnownMiddleContinuationAndKnownEndWithoutContinuation()
    {
        var middle = new InventoryPage(new PageRequest(10, 10), Bounds(10, 10, 2, 13, AtlasDenominatorState.Known), [File("file:1"), File("file:2")], nextOffset: 12);
        var end = new InventoryPage(new PageRequest(10, 10), Bounds(10, 10, 2, 12, AtlasDenominatorState.Known), [File("file:1"), File("file:2")]);

        Assert.Equal(12, middle.NextOffset);
        Assert.Null(end.NextOffset);
        Assert.Equal(AtlasDenominatorState.Known, middle.Bounds.TotalState);
        Assert.Equal(13, middle.Bounds.TotalCount);
    }

    [Fact]
    public void Create_InventoryPage_AcceptsUnknownAndWithheldContinuationWithoutInventingKnownTotal()
    {
        var unknown = new InventoryPage(new PageRequest(5, 10), Bounds(10, 10, 2, null, AtlasDenominatorState.Unknown), [File("file:1"), File("file:2")], nextOffset: 7);
        var withheld = new InventoryPage(new PageRequest(5, 10), Bounds(10, 10, 2, null, AtlasDenominatorState.Withheld), [File("file:1"), File("file:2")], nextOffset: 7);

        Assert.Equal(7, unknown.NextOffset);
        Assert.Equal(7, withheld.NextOffset);
        Assert.Null(unknown.Bounds.TotalCount);
        Assert.Null(withheld.Bounds.TotalCount);
        Assert.Equal(AtlasDenominatorState.Unknown, unknown.Bounds.TotalState);
        Assert.Equal(AtlasDenominatorState.Withheld, withheld.Bounds.TotalState);
    }

    [Fact]
    public void Create_InventoryPage_RejectsInvalidContinuationOffsets()
    {
        Assert.Throws<ArgumentException>(() => new InventoryPage(new PageRequest(0, 10), Bounds(10, 10, 0, null, AtlasDenominatorState.Unknown), [], nextOffset: 0));
        Assert.Throws<ArgumentException>(() => new InventoryPage(new PageRequest(0, 10), Bounds(10, 10, 1, null, AtlasDenominatorState.Unknown), [File("file:1")], nextOffset: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryPage(new PageRequest(0, 10), Bounds(10, 10, 1, null, AtlasDenominatorState.Unknown), [File("file:1")], nextOffset: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryPage(new PageRequest(int.MaxValue, 1), Bounds(1, 1, 1, null, AtlasDenominatorState.Unknown), [File("file:1")], nextOffset: int.MaxValue));
        Assert.Throws<ArgumentException>(() => new InventoryPage(new PageRequest(10, 10), Bounds(10, 10, 2, 12, AtlasDenominatorState.Known), [File("file:1"), File("file:2")], nextOffset: 12));
        Assert.Throws<ArgumentException>(() => new InventoryPage(new PageRequest(10, 10), Bounds(10, 10, 2, 11, AtlasDenominatorState.Known), [File("file:1"), File("file:2")], nextOffset: 12));
    }

    [Fact]
    public void Create_InventoryPage_NextOffsetIsImmutableOptionalProperty()
    {
        var page = new InventoryPage(new PageRequest(0, 10), Bounds(10, 10, 1, null, AtlasDenominatorState.Unknown), [File("file:1")], nextOffset: 1);
        var property = typeof(InventoryPage).GetProperty(nameof(InventoryPage.NextOffset))!;

        Assert.Equal(1, page.NextOffset);
        Assert.False(property.CanWrite);
    }

    [Fact]
    public void Create_SelectionRequest_UsesManifestFileAndSequenceNotGrantOrBindingAuthority()
    {
        var request = new SelectionRequest("manifest:1", "file:value", null, 42);

        Assert.Equal(42, request.RequestSequence);
        Assert.DoesNotContain(typeof(SelectionRequest).GetConstructors().SelectMany(c => c.GetParameters()), p => p.ParameterType == typeof(AtlasRootGrant) || p.ParameterType == typeof(AtlasSourceBinding));
        Assert.Throws<ArgumentException>(() => new SelectionRequest("", "file:value", null, 1));
    }

    [Fact]
    public void Create_NonMatchSourceProjectionWithText_ThrowsAndUnknownCoverageHasNoNumericFiction()
    {
        var outline = new SelectionOutline([new OutlineDeclaration("decl:1", "M", AtlasDeclarationKind.Method, new AtlasTextSpan(0, 1))]);
        var text = new SourceTextPage("hello", new AtlasTextSpan(0, 5), [new AtlasTextSpan(1, 2)]);
        var coverage = SelectionCoverage.Unknown("not-counted");

        Assert.Null(coverage.Value);
        Assert.Throws<ArgumentException>(() => SourceProjection.Unavailable("source:1", text));
        var projection = new SelectionProjection("receipt:1", "manifest:1", "file:value", 3, outline, SourceProjection.Unavailable("source:1"), BoundsUnknown(), coverage, ["source unavailable"]);
        Assert.Single(projection.Limitations);
    }

    [Fact]
    public void Create_IndexedMatch_RequiresVerifiedObservationAndValidPageRanges()
    {
        var source = AtlasSourceObservation.Verified("source:1", "manifest:1", "file:value", "policy", Identity(), Identity(), "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 5, "utf-8", 5, BoundsKnown(1));
        var binding = AtlasSourceBinding.Create("manifest:1", "file:value", "policy", NativeToken(Identity()), NativeToken(Identity()), "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        var match = SourceProjection.IndexedMatch(source, binding, "utf-8", new SourceTextPage("hello", new AtlasTextSpan(0, 5), [new AtlasTextSpan(1, 2)]));

        Assert.Equal(SourceProjectionState.IndexedMatch, match.State);
        Assert.Throws<ArgumentException>(() => SourceProjection.IndexedMatch(source, binding, "utf-8", new SourceTextPage("hello!", new AtlasTextSpan(0, 6), [])));
        Assert.Throws<ArgumentException>(() => new SourceTextPage("hello", new AtlasTextSpan(0, 5), [new AtlasTextSpan(4, 2)]));
    }

    [Fact]
    public void Create_SelectionProjection_CapturesImmutableLimitationsSnapshot()
    {
        var limitations = new List<string> { "source unavailable" };
        var outline = new SelectionOutline([]);

        var projection = new SelectionProjection("receipt:1", "manifest:1", "file:value", 3, outline, SourceProjection.Unavailable("source:1"), BoundsUnknown(), SelectionCoverage.Withheld("policy"), limitations);
        limitations.Add("mutated");

        Assert.Single(projection.Limitations);
    }

    [Fact]
    public void Ports_ExposeFrozenAsyncContracts()
    {
        Assert.Equal(typeof(Task<DirectoryObservation>), typeof(IAtlasDirectoryEnumerator).GetMethod(nameof(IAtlasDirectoryEnumerator.EnumerateAsync))!.ReturnType);
        Assert.Equal(typeof(Task<InventoryPage>), typeof(IAtlasQueries).GetMethod(nameof(IAtlasQueries.InventoryAsync))!.ReturnType);
        Assert.Equal(typeof(Task<SelectionProjection>), typeof(IAtlasQueries).GetMethod(nameof(IAtlasQueries.SelectAsync))!.ReturnType);
        Assert.Equal(typeof(Task<SelectionProjection>), typeof(IAtlasQueries).GetMethod(nameof(IAtlasQueries.RestoreAsync))!.ReturnType);
    }

    private static AtlasObjectIdentity Identity() => new("volume:1", "file:2");
    private static string NativeToken(AtlasObjectIdentity identity) => AtlasIdentityCodec.ForNativeObject(identity);
    private static AtlasFileEntry File(string value) => new(value, "src/A.cs", "src", AtlasDirectoryEntryKind.File, AtlasFileClassification.CSharp, null, AtlasFileAvailability.Available, null);
    private static AtlasBounds Bounds(int requestedLimit, int effectiveLimit, int returnedRows, long? totalCount, AtlasDenominatorState state) =>
        new(requestedLimit, effectiveLimit, returnedRows, 10, totalCount, state, state is AtlasDenominatorState.Known ? null : "not-counted", "entries");
    private static AtlasBounds BoundsUnknown() => new(128, 64, 1, 10, null, AtlasDenominatorState.Unknown, "not-counted", "entries");
    private static AtlasBounds BoundsKnown(long total) => new(128, 64, 1, 10, total, AtlasDenominatorState.Known, null, null);
}
