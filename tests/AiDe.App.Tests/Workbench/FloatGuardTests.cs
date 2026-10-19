using System.Windows.Controls;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;

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
        var service = new ZoneBackedLayoutService(WorkbenchLayout.Default());
        var announcer = new RecordingAnnouncer();
        var surface = service.Zones.AllSurfaces().First(s => service.Current.FindStackOf(s.SurfaceId) is not null);
        var stackId = service.Current.FindStackOf(surface.SurfaceId)!.Id;
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
    public void FloatPane_FloatsTheFocusedPane_DrawnInAFloatingWindow_VisibleAndFocusable() =>
        Sta.Pump(
            create: () => new DockingManager(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (main, manager) =>
            {
                var service = new ZoneBackedLayoutService();
                var announcer = new RecordingAnnouncer();
                var adapter = new WorkbenchAdapter(manager, service, surface => new TextBox { Text = surface.SurfaceId });
                adapter.Render();
                await Task.Delay(200);
                var stack = service.Current.FindStackOf("sessions")!;
                var surfaceId = stack.Surfaces[0].SurfaceId;
                var controller = new WorkbenchController(service, announcer)
                {
                    FocusedStackId = stack.Id,
                    FocusedSurfaceId = surfaceId,
                };

                controller.Execute("workbench.floatPane");
                adapter.Render();
                await Task.Delay(500);

                Assert.True(service.Current.Floating.Count == 1, $"floatPane did not float the pane; it said: {announcer.Last}");
                Assert.Equal(surfaceId, Assert.Single(Assert.Single(service.Current.Floating).Surfaces).SurfaceId);
                Assert.Null(service.Zones.FindZoneOf(surfaceId));
                Assert.IsType<LayoutDocumentFloatingWindow>(Assert.Single(manager.Layout.FloatingWindows));
                var window = Assert.Single(manager.FloatingWindows);
                Assert.True(window.IsVisible, "the floating window is not shown");
                var content = Assert.IsType<TextBox>(adapter.ContentFor(surfaceId));
                Assert.True(FloatRenderTests.HostedBy(content, window), "the floated pane is not drawn in the floating window");
                Assert.False(FloatRenderTests.HostedBy(content, main), "the floated pane is still drawn in the workbench window");
                Assert.True(content.IsVisible, "the floated pane is not visible");
                window.Activate();
                Assert.True(content.Focus(), "the floated pane did not take focus");
            });

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
