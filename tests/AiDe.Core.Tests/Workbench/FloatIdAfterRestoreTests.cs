using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// <b>T21 node 5 check (C), the Owner's close condition 2.</b> Floating-window ids are unique across a
/// restore. The id counter (<c>ZoneBackedLayoutService._floatSeq</c>) starts at 0 in every service, so
/// before this the first float after a restore that already held <c>float-1</c> minted <c>float-1</c>
/// again. The zone invariant checks surface ids only, so the duplicate was stored, and the tree
/// projection (<c>Layout.AssertInvariant</c>) then threw "duplicate node id" out of <c>Apply</c>.
/// </summary>
public sealed class FloatIdAfterRestoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-float-id-" + Guid.NewGuid().ToString("N"));

    private static LayoutOperation Float(string surfaceId) =>
        new LayoutOperation.MoveSurface(surfaceId, new DropTarget(string.Empty, DropKind.Float));

    [Fact]
    public void AfterARestoreThatHoldsFloat1_TheNextFloat_MintsAnIdNotAlreadyInUse()
    {
        var before = new ZoneBackedLayoutService();
        var ids = before.Zones.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
        var floated = before.Apply(Float("sessions"));
        Assert.True(floated.Applied, floated.Announcement);
        Assert.Equal("float-1", Assert.Single(before.Zones.Floating).Id);

        // The restart: save, read back, restore into a new service.
        var store = new ZoneLayoutStore(Path.Combine(_dir, "layout.zones.json"));
        store.Save(before.Zones);
        var read = store.Read(ids, new HashSet<string>(StringComparer.Ordinal));
        var after = new ZoneBackedLayoutService();
        after.RestoreZones(Assert.IsType<WorkbenchLayout>(read.Layout));
        Assert.Equal("float-1", Assert.Single(after.Zones.Floating).Id);

        var result = after.Apply(Float("graph"));

        Assert.True(result.Applied, result.Announcement);
        var floatIds = after.Zones.Floating.Select(f => f.Id).ToList();
        Assert.Equal(2, floatIds.Count);
        Assert.True(
            floatIds.Distinct(StringComparer.Ordinal).Count() == floatIds.Count,
            $"the float after the restore minted an id already in use: {string.Join(", ", floatIds)}");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) { Directory.Delete(_dir, recursive: true); } }
        catch (IOException) { }
    }
}
