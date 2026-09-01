using System.Collections.Immutable;
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

    [Fact]
    public void Restore_AfterANativeDragBetweenZones_FollowsTheDropByPosition_NotByKind()
    {
        var svc = new ZoneBackedLayoutService(WorkbenchLayout.Default());
        var tree = svc.Current; // Vertical[ Horizontal[zone-left, zone-center], zone-bottom ]

        var left = tree.AllStacks().Single(s => s.Id == ZonesToTree.LeftStackId);
        var center = tree.AllStacks().Single(s => s.Id == ZonesToTree.CenterStackId);
        var bottom = tree.AllStacks().Single(s => s.Id == ZonesToTree.BottomStackId);
        var domain = center.Surfaces.Single(s => s.SurfaceId == "domain"); // a Center document (kind "view")

        // Simulate the user dragging "domain" out of the Center pane into the Bottom pane.
        var center2 = new StackNode("c", center.Surfaces.Remove(domain));
        var bottom2 = new StackNode("b", bottom.Surfaces.Add(domain));
        var post = new Layout(
            new SplitNode("root", Orientation.Vertical,
                [new SplitNode("cols", Orientation.Horizontal, [left, center2], [0.3, 0.7]), bottom2],
                [0.7, 0.3]),
            [], ImmutableDictionary<string, StackState>.Empty);

        svc.Restore(post);

        // Position wins: "domain" is now in the Bottom zone — a kind-based reconcile would have snapped
        // it back to the Center (its "view" kind).
        Assert.Equal(ZoneId.Bottom, svc.Zones.FindZoneOf("domain"));
        Assert.Equal(ZoneId.Center, svc.Zones.FindZoneOf("graph")); // untouched Center doc stayed
    }

    [Fact]
    public void Restore_OfAnUnmappableTree_FallsBackToConversion_WithoutLosingSurfaces()
    {
        var svc = new ZoneBackedLayoutService(WorkbenchLayout.Default());

        // A shape the position mapper will not recognise for the current 2-column occupancy: 3 columns.
        var a = new StackNode("a", [new Surface("x", "view", "X")]);
        var b = new StackNode("b", [new Surface("y", "canvas", "Y")]);
        var c = new StackNode("c", [new Surface("z", "inspector", "Z")]);
        var weird = new Layout(
            new SplitNode("cols", Orientation.Horizontal, [a, b, c], [0.33, 0.34, 0.33]),
            [], ImmutableDictionary<string, StackState>.Empty);

        svc.Restore(weird);

        // Falls back to kind-based conversion; every surface survives and lands in a deterministic zone.
        var all = svc.Current.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).ToHashSet();
        Assert.Contains("x", all);
        Assert.Contains("y", all);
        Assert.Contains("z", all);
        Assert.Equal(ZoneId.Center, svc.Zones.FindZoneOf("y")); // canvas → Center by kind
    }

    private static IReadOnlyList<string> StackSurfaces(ILayoutService svc, string stackId) =>
        svc.Current.AllStacks().FirstOrDefault(s => s.Id == stackId)?.Surfaces.Select(s => s.SurfaceId).ToList()
        ?? new List<string>();
}

