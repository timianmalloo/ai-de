using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// <b>T21 check 2 (Ruling 171 (2)).</b> A floating surface can be redocked into a zone and closed,
/// through the service every caller uses (<see cref="ZoneBackedLayoutService.Apply"/>). Before T21
/// the zone operations looked only in the four zones, so a floating surface was refused as "not
/// docked": it could be neither moved nor closed.
/// </summary>
public sealed class FloatingMoveCloseTests
{
    private static (ZoneBackedLayoutService Service, Surface Surface) Floated(string surfaceId)
    {
        var service = new ZoneBackedLayoutService();
        var surface = service.Zones.AllSurfaces().Single(s => s.SurfaceId == surfaceId);
        var floated = service.Apply(new LayoutOperation.MoveSurface(surfaceId, new DropTarget(string.Empty, DropKind.Float)));
        Assert.True(floated.Applied, floated.Announcement);
        Assert.Null(service.Zones.FindZoneOf(surfaceId));
        return (service, surface);
    }

    [Fact]
    public void AFloatingDocument_RedocksToTheCenter_AsItsActiveTab_AndLeavesNoFloatingWindow()
    {
        var (service, surface) = Floated("graph");
        var othersBefore = service.Zones.Zones.Values.Where(z => z.Id != ZoneId.Center).ToList();

        var result = service.Apply(new LayoutOperation.MoveSurface(
            "graph", new DropTarget(ZonesToTree.CenterStackId, DropKind.JoinStack)));

        Assert.True(result.Applied, result.Announcement);
        Assert.Null(result.RefusalCode);
        Assert.Equal($"Moved {surface.Title} to the center.", result.Announcement);
        Assert.Empty(service.Zones.Floating);
        Assert.Equal(ZoneId.Center, service.Zones.FindZoneOf("graph"));
        Assert.Equal("graph", service.Current.FindStackOf("graph")!.Active.SurfaceId);
        Assert.Equal(othersBefore, service.Zones.Zones.Values.Where(z => z.Id != ZoneId.Center).ToList());
    }

    [Fact]
    public void AFloatingSurface_Closes_AndIsGoneFromTheModel()
    {
        var (service, surface) = Floated("sessions");

        var result = service.Apply(new LayoutOperation.CloseSurface("sessions"));

        Assert.True(result.Applied, result.Announcement);
        Assert.Equal($"Closed {surface.Title}.", result.Announcement);
        Assert.Empty(service.Zones.Floating);
        Assert.DoesNotContain(service.Zones.AllSurfaces(), s => s.SurfaceId == "sessions");
    }

    [Fact]
    public void AFloatingSurface_CanBeActivated()
    {
        var (service, _) = Floated("sessions");

        var result = service.Apply(new LayoutOperation.ActivateSurface("sessions"));

        Assert.True(result.Applied, result.Announcement);
        Assert.Equal("sessions", Assert.Single(service.Zones.Floating).Active.SurfaceId);
    }

    [Fact]
    public void RedockingOneTabOfAFloatingStack_KeepsTheOtherTabFloating()
    {
        var a = new Surface("float-a", "sessions", "Sessions A");
        var b = new Surface("float-b", "sessions", "Sessions B");
        var service = new ZoneBackedLayoutService(WorkbenchLayout.Default() with
        {
            Floating = [new StackNode("float-1", [a, b], 1, StackState.Floating)],
        });

        var result = service.Apply(new LayoutOperation.MoveSurface(
            "float-b", new DropTarget(ZonesToTree.RightStackId, DropKind.JoinStack)));

        Assert.True(result.Applied, result.Announcement);
        Assert.Equal(ZoneId.Right, service.Zones.FindZoneOf("float-b"));
        var left = Assert.Single(service.Zones.Floating);
        Assert.Equal("float-1", left.Id);
        Assert.Equal(["float-a"], left.Surfaces.Select(s => s.SurfaceId));
        Assert.Equal("float-a", left.Active.SurfaceId);
    }
}
