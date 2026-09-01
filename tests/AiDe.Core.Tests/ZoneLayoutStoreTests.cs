using AiDe.Core.Workbench;

namespace AiDe.Core.Tests;

/// <summary>
/// Zone-faithful persistence (ADR-0021 dz-persist): saving and restoring a WorkbenchLayout preserves
/// what the projected tree cannot — collapsed-zone content, per-zone extent, and exact placement —
/// while dropping surfaces the app can no longer provide.
/// </summary>
public sealed class ZoneLayoutStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-zone-store-" + Guid.NewGuid().ToString("N"));

    private string Path_ => System.IO.Path.Combine(_dir, "zones.json");

    private static IReadOnlySet<string> All => new HashSet<string>(StringComparer.Ordinal);

    private static IReadOnlySet<string> Kinds(params string[] kinds) => new HashSet<string>(kinds, StringComparer.Ordinal);

    [Fact]
    public void SaveThenLoad_PreservesCollapsedState_Extents_AndExactPlacement()
    {
        // Arrange: a non-default arrangement — Right populated, Left collapsed, a custom Bottom extent.
        var layout = WorkbenchLayout.Default();
        layout = ZoneLayoutService.OpenPane(layout, new Surface("outline", "inspector", "Outline"), ZoneId.Right).Layout;
        layout = ZoneLayoutService.CollapseZone(layout, ZoneId.Left).Layout;
        layout = ZoneLayoutService.ResizeZone(layout, ZoneId.Bottom, 0.45).Layout;

        var store = new ZoneLayoutStore(Path_);
        store.Save(layout);

        // All kinds restorable so nothing is filtered.
        var restored = store.Load(All, Kinds("view", "inspector", "canvas", "terminal", "sessions", "board", "leaderboard", "contexts", "joins"));

        Assert.NotNull(restored);
        Assert.True(restored!.Zone(ZoneId.Left).Collapsed);                 // collapsed state preserved
        Assert.False(restored.Zone(ZoneId.Left).IsEmpty);                   // collapsed content retained
        Assert.Equal(ZoneId.Right, restored.FindZoneOf("outline"));         // exact placement preserved
        Assert.Equal(0.45, restored.Zone(ZoneId.Bottom).Extent, precision: 6); // extent preserved
        Assert.Equal(ZoneId.Bottom, restored.FindZoneOf("terminal-1"));
    }

    [Fact]
    public void Load_DropsSurfacesTheAppCanNoLongerProvide()
    {
        var layout = ZoneLayoutService.OpenPane(
            WorkbenchLayout.Default(), new Surface("agent-terminal-xyz", "terminal", "Agent"), ZoneId.Bottom).Layout;
        new ZoneLayoutStore(Path_).Save(layout);

        // "terminal" kind is NOT restorable and the id is not available → the agent terminal is dropped;
        // everything else (view/canvas/etc.) is restorable by kind.
        var restored = new ZoneLayoutStore(Path_).Load(All, Kinds("view", "inspector", "canvas", "sessions", "board", "leaderboard", "contexts", "joins"));

        Assert.NotNull(restored);
        Assert.DoesNotContain(restored!.AllSurfaces(), s => s.SurfaceId == "agent-terminal-xyz");
        Assert.DoesNotContain(restored.AllSurfaces(), s => s.SurfaceId == "terminal-1"); // terminal kind not restorable
        Assert.Contains(restored.AllSurfaces(), s => s.SurfaceId == "graph");            // canvas kind restorable
    }

    [Fact]
    public void Load_WithNoFile_ReturnsNull_SoTheCallerKeepsItsCurrentLayout()
    {
        Assert.Null(new ZoneLayoutStore(Path_).Load(All, Kinds("view")));
    }

    [Fact]
    public void Load_OfACorruptFile_ReturnsNull_WithoutThrowing()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path_, "{ this is not valid json");
        Assert.Null(new ZoneLayoutStore(Path_).Load(All, Kinds("view")));
    }

    [Fact]
    public void RestoreZones_OnTheService_ReplacesTheArrangement()
    {
        var svc = new ZoneBackedLayoutService();
        var saved = ZoneLayoutService.OpenPane(WorkbenchLayout.Default(), new Surface("outline", "inspector", "Outline"), ZoneId.Right).Layout;

        svc.RestoreZones(saved);

        Assert.Equal(ZoneId.Right, svc.Zones.FindZoneOf("outline"));
        Assert.Contains(ZonesToTree.RightStackId, svc.Current.AllStacks().Select(s => s.Id)); // reflected in the projection
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) { Directory.Delete(_dir, recursive: true); } }
        catch (IOException) { }
    }
}
