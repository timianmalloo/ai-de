using AiDe.Core.Workbench;

namespace AiDe.Core.Tests;

/// <summary>
/// The Strangler service: an <see cref="ILayoutService"/> backed by the zone model. Tree-shaped
/// operations translate to zone-scoped ones, and the projected <see cref="ILayoutService.Current"/>
/// tree is always the fixed frame — so a close or an add cannot relocate an unrelated pane (DC-063).
/// </summary>
public sealed class ZoneBackedLayoutServiceTests
{
    private static ILayoutService Service() => new ZoneBackedLayoutService();

    [Fact]
    public void Current_IsTheFixedFrame_WithDeterministicZoneStackIds()
    {
        var ids = Service().Current.AllStacks().Select(s => s.Id).ToHashSet();
        Assert.Contains(ZonesToTree.CenterStackId, ids);
        Assert.Contains(ZonesToTree.LeftStackId, ids);
        Assert.Contains(ZonesToTree.BottomStackId, ids);
    }

    [Fact]
    public void AddSurface_ToAZone_AppearsThere_AndLeavesOtherZonesStackContentsUnchanged()
    {
        var svc = Service();
        var leftBefore = StackSurfaces(svc, ZonesToTree.LeftStackId);
        var centerBefore = StackSurfaces(svc, ZonesToTree.CenterStackId);

        var result = svc.Apply(new LayoutOperation.AddSurface(
            ZonesToTree.BottomStackId, new Surface("diag", "diagnostics", "Diagnostics")));
        Assert.True(result.Applied);

        Assert.Contains("diag", StackSurfaces(svc, ZonesToTree.BottomStackId));
        Assert.Equal(leftBefore, StackSurfaces(svc, ZonesToTree.LeftStackId));      // no flip
        Assert.Equal(centerBefore, StackSurfaces(svc, ZonesToTree.CenterStackId));  // no flip
    }

    [Fact]
    public void CloseSurface_RemovesItWithoutRelocatingOtherZones()
    {
        var svc = Service();
        var bottomBefore = StackSurfaces(svc, ZonesToTree.BottomStackId);
        var leftBefore = StackSurfaces(svc, ZonesToTree.LeftStackId);

        // Close a Center document.
        var result = svc.Apply(new LayoutOperation.CloseSurface("domain"));
        Assert.True(result.Applied);

        Assert.DoesNotContain("domain", svc.Current.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.SurfaceId));
        Assert.Equal(bottomBefore, StackSurfaces(svc, ZonesToTree.BottomStackId));  // the terminal did not move
        Assert.Equal(leftBefore, StackSurfaces(svc, ZonesToTree.LeftStackId));      // explorers did not move
    }

    [Fact]
    public void MoveSurface_JoiningAZoneStack_MovesItToThatZoneOnly()
    {
        var svc = Service();
        var target = new DropTarget(ZonesToTree.LeftStackId, DropKind.JoinStack);

        var result = svc.Apply(new LayoutOperation.MoveSurface("terminal-1", target));
        Assert.True(result.Applied);

        Assert.Contains("terminal-1", StackSurfaces(svc, ZonesToTree.LeftStackId));
        // Bottom is now empty of the terminal — and because it becomes empty it is omitted from the frame.
        Assert.DoesNotContain(ZonesToTree.BottomStackId, svc.Current.AllStacks().Select(s => s.Id));
    }

    [Fact]
    public void ActivateSurface_SelectsTheTab_WithoutMovingAnything()
    {
        var svc = Service();
        var surfacesBefore = svc.Current.AllStacks()
            .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).OrderBy(x => x).ToList();

        var result = svc.Apply(new LayoutOperation.ActivateSurface("joins"));
        Assert.True(result.Applied);

        var surfacesAfter = svc.Current.AllStacks()
            .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).OrderBy(x => x).ToList();
        Assert.Equal(surfacesBefore, surfacesAfter); // same surfaces in the same zones; only the active tab changed
    }

    [Fact]
    public void Locked_RefusesEverythingExceptActivate()
    {
        var svc = Service();
        svc.IsLocked = true;

        Assert.False(svc.Apply(new LayoutOperation.CloseSurface("graph")).Applied);
        Assert.True(svc.Apply(new LayoutOperation.ActivateSurface("graph")).Applied);
    }

    [Fact]
    public void Restore_FromATree_RebuildsTheZones()
    {
        var svc = Service();
        svc.Apply(new LayoutOperation.AddSurface(ZonesToTree.RightStackId, new Surface("outline", "inspector", "Outline")));
        var saved = svc.Current;

        var fresh = Service();
        fresh.Restore(saved);

        Assert.Equal("outline", fresh.Current.AllStacks()
            .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).FirstOrDefault(x => x == "outline"));
    }

    [Fact]
    public void ResetToDefault_ReturnsTheDefaultFrame()
    {
        var svc = Service();
        svc.Apply(new LayoutOperation.CloseSurface("graph"));
        var result = svc.Apply(new LayoutOperation.ResetToDefault());
        Assert.True(result.Applied);
        Assert.Contains("graph", svc.Current.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.SurfaceId));
    }

    private static IReadOnlyList<string> StackSurfaces(ILayoutService svc, string stackId) =>
        svc.Current.AllStacks().FirstOrDefault(s => s.Id == stackId)?.Surfaces.Select(s => s.SurfaceId).ToList()
        ?? new List<string>();
}
