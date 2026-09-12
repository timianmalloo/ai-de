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
    public void Create_SelectionRequest_UsesManifestFileAndSequenceNotGrantOrBindingAuthority()
    {
        var request = new SelectionRequest("manifest:1", "file:value", null, 42);

        Assert.Equal(42, request.RequestSequence);
        Assert.DoesNotContain(typeof(SelectionRequest).GetConstructors().SelectMany(c => c.GetParameters()), p => p.ParameterType == typeof(AtlasRootGrant) || p.ParameterType == typeof(AtlasSourceBinding));
        Assert.Throws<ArgumentException>(() => new SelectionRequest("", "file:value", null, 1));
    }

    [Fact]
    public void Create_SelectionProjection_CapturesImmutableLimitationsSnapshot()
    {
        var limitations = new List<string> { "source unavailable" };

        var projection = new SelectionProjection("receipt:1", "manifest:1", "file:value", 3, "outline", AtlasSourceResult.Unavailable, BoundsUnknown(), 0.5, limitations);
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

    private static AtlasBounds BoundsUnknown() => new(128, 64, 1, 10, null, AtlasDenominatorState.Unknown, "not-counted", "entries");
}
