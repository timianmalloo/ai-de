using AiDe.Core.Workbench;

namespace AiDe.Core.Tests.Workbench;

/// <summary>
/// <b>T21 node 6 check (A), the Owner's close condition 3.</b> floatPane floats the focused tab, not
/// the first. The command reaches the service as <c>SetStackState(stackId, Floating)</c>
/// (<c>WorkbenchController</c>, "workbench.floatPane"). Before this, <c>ZoneBackedLayoutService.SetState</c>
/// passed the zone's first surface to <c>Float</c>, so a user who focused the second tab and pressed
/// floatPane saw the first tab leave.
/// </summary>
public sealed class FloatFocusedTabTests
{
    [Fact]
    public void AZoneWithTwoTabs_TheSecondFocused_FloatPane_FloatsTheSecond()
    {
        var first = new Surface("focus-a", "sessions", "Sessions A");
        var second = new Surface("focus-b", "sessions", "Sessions B");
        var start = WorkbenchLayout.Default();
        var service = new ZoneBackedLayoutService(start.WithZone(start.Zone(ZoneId.Left) with
        {
            Content = new ZoneStack([first, second]),
            Collapsed = false,
        }));
        Assert.True(service.Apply(new LayoutOperation.ActivateSurface("focus-b")).Applied);

        var result = service.Apply(new LayoutOperation.SetStackState(ZonesToTree.LeftStackId, StackState.Floating));

        Assert.True(result.Applied, result.Announcement);
        var window = Assert.Single(service.Zones.Floating);
        Assert.Equal(["focus-b"], window.Surfaces.Select(s => s.SurfaceId));
        Assert.Equal(["focus-a"], service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId));
    }
}
