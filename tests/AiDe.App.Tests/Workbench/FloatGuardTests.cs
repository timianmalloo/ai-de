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
    public void APaneSavedFloating_RestoresThroughLayoutPersistence_Floating_DrawnVisibleAndNeverDocked() =>
        Sta.Pump(
            create: () => new DockingManager(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (main, manager) =>
            {
                Directory.CreateDirectory(_dir);
                var layoutPath = Path.Combine(_dir, "layout.json");

                // Before the restart: float the focused pane through the product's verb and save.
                var before = new ZoneBackedLayoutService();
                var ids = before.Zones.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
                var stack = before.Current.AllStacks().First(s => s.Surfaces.Count > 0 && before.Zones.FindZoneOf(s.Surfaces[0].SurfaceId) is not null);
                var floatedId = stack.Surfaces[0].SurfaceId;
                var said = new RecordingAnnouncer();
                var controller = new WorkbenchController(before, said)
                {
                    FocusedStackId = stack.Id,
                    FocusedSurfaceId = floatedId,
                };
                controller.Execute("workbench.floatPane");
                Assert.True(before.Zones.Floating.Any(s => s.Surfaces.Any(x => x.SurfaceId == floatedId)), $"floatPane did not float {floatedId} in {stack.Id}; it said: {said.Last}");
                using (var first = new LayoutPersistence(before, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds))
                {
                    first.SaveNow();
                    Assert.Null(first.LastSaveFailure);
                }

                // The restart: a new host, LayoutPersistence's real restore, and a render.
                var host = new ZoneBackedLayoutService();
                using var persistence = new LayoutPersistence(host, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds);
                var result = persistence.Restore();
                var adapter = new WorkbenchAdapter(manager, host, surface => new TextBox { Text = surface.SurfaceId });
                adapter.Render();
                await Task.Delay(500);

                Assert.True(host.Zones.FindZoneOf(floatedId) is null, $"the restore docked the floating pane; it said: {result.Announcement}");
                Assert.Equal(floatedId, Assert.Single(Assert.Single(host.Zones.Floating).Surfaces).SurfaceId);
                Assert.DoesNotContain("docked", result.Announcement, StringComparison.OrdinalIgnoreCase);
                Assert.Null(result.ErrorCode);
                var window = Assert.Single(manager.FloatingWindows);
                Assert.True(window.IsVisible, "the restored floating window is not shown");
                var content = Assert.IsType<TextBox>(adapter.ContentFor(floatedId));
                Assert.True(FloatRenderTests.HostedBy(content, window), "the restored pane is not drawn in the floating window");
                Assert.False(FloatRenderTests.HostedBy(content, main), "the restored pane is drawn in the workbench window");
                Assert.True(content.IsVisible, "the restored pane is not visible");
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
