using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// <b>T21 node 6 check (B).</b> A saved floating stack whose id is already in use loads with the
/// partial-read sentence, and the restore never throws. The zone invariant checks surface ids only, so
/// <c>ZoneLayoutStore.Read</c> passed two floating stacks with one id (or a floating id equal to a
/// projected zone stack id), and the tree projection (<c>Layout.AssertInvariant</c>) then threw
/// "duplicate node id" at the first read of <c>Current</c>. Same class as node 5's crash shapes
/// (DC-NEW-wl-t21-float): the reader promises never to throw on a bad file.
/// </summary>
public sealed class ZoneLayoutFloatingIdInUseTests
{
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "fixtures", name);

    private static IReadOnlySet<string> None => new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlySet<string> Kinds => new HashSet<string>(["view", "canvas"], StringComparer.Ordinal);

    // Two floating stacks with one id: the first is kept and the second is left out, named by title.
    // A floating id equal to a projected zone stack id ("zone-left") collides with the docked zone.
    [Theory]
    [InlineData("zone-layout-floating-duplicate-id.json", "Notebook B")]
    [InlineData("zone-layout-floating-reserved-id.json", "Sessions A")]
    public void AFloatingStackWhoseIdIsInUse_IsLeftOut_NamedByTitle_AndTheRestoreNeverThrows(string fixture, string leftOut)
    {
        ZoneLoadResult? result = null;
        var thrownAtRead = Record.Exception(() => result = new ZoneLayoutStore(Fixture(fixture)).Read(None, Kinds));

        Assert.Null(thrownAtRead);
        Assert.Equal(ZoneLoadRefusal.None, result!.Refusal);

        var service = new ZoneBackedLayoutService();
        var thrownAtRestore = Record.Exception(() =>
        {
            service.RestoreZones(Assert.IsType<WorkbenchLayout>(result.Layout));
            _ = service.Current;
        });

        Assert.Null(thrownAtRestore);
        Assert.True(result.PartlyRead, "the read did not say that the floating stack was left out");
        Assert.Equal([leftOut], result.LeftOutTitles);
        Assert.Contains(service.Zones.AllSurfaces(), s => s.SurfaceId == "explore");
        Assert.DoesNotContain(service.Zones.AllSurfaces(), s => s.Title == leftOut);
    }
}
