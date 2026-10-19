using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>Ruling 171 (1)</b> — float is guarded until T21 makes it real, so no pane can vanish: the
/// float verb refuses with a stated cause, and a layout restored with a floating window docks that
/// pane back with a report line. Both through the real surface (the controller; the persistence
/// restore over the product's host admission).
/// </summary>
public sealed class FloatGuardTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-floatguard-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void FloatPane_RefusesWithAStatedCause_PaneStaysDockedAndFocused()
    {
        var service = new ZoneBackedLayoutService(DockHost.AdmissionFor(PerspectiveSet.Factory));
        var announcer = new RecordingAnnouncer();
        var stack = service.Current.AllStacks().First();
        var surface = stack.Active;
        var stackId = stack.Id;
        var controller = new WorkbenchController(service, announcer)
        {
            FocusedStackId = stackId,
            FocusedSurfaceId = surface.SurfaceId,
        };
        var before = service.Current.Shape();

        controller.Execute("workbench.floatPane");

        Assert.Equal(before, service.Current.Shape());
        Assert.Empty(service.Current.Floating);
        Assert.NotNull(service.Zones.FindZoneOf(surface.SurfaceId));
        Assert.Equal(stackId, controller.FocusedStackId);
        Assert.Equal(surface.SurfaceId, controller.FocusedSurfaceId);
        Assert.Contains("not available yet", announcer.Last, StringComparison.Ordinal);
    }

    [Fact]
    public void RestoringAFloatingWindow_DocksThePaneIntoItsKindsZone_DrawnReachableAndReported()
    {
        Directory.CreateDirectory(_dir);
        var layoutPath = Path.Combine(_dir, "layout.json");
        var slot = LayoutPersistence.SlotPathFor(layoutPath, PerspectiveSet.Factory);
        var floated = new Surface("terminal-float", "terminal", "Terminal — floated");
        var saved = WorkbenchLayout.Default(PerspectiveSet.Factory) with
        {
            Floating = [new StackNode("float-1", [floated], 0, StackState.Floating)],
        };
        new ZoneLayoutStore(slot).Save(saved);

        var host = new ZoneBackedLayoutService(DockHost.AdmissionFor(PerspectiveSet.Factory));
        using var persistence = new LayoutPersistence(
            host, layoutPath, new HashSet<string>(StringComparer.Ordinal), restorableKinds: SurfaceContentFactory.RestorableKinds);

        var result = persistence.Restore();

        Assert.Empty(host.Zones.Floating);
        Assert.Equal(ZoneId.Bottom, host.Zones.FindZoneOf("terminal-float"));
        var drawn = Assert.IsType<StackNode>(host.Current.FindStackOf("terminal-float"));
        Assert.DoesNotContain(host.Current.Floating, f => f.Id == drawn.Id);
        Assert.Contains("Terminal — floated", result.Announcement, StringComparison.Ordinal);
        Assert.Contains("floating", result.Announcement, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.ErrorCode);
    }
}
