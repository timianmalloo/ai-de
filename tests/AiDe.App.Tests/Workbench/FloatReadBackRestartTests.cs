using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;

namespace AiDe.App.Tests.Shell;

/// <summary>
/// <b>T21 check 3 (Ruling 171 (2)).</b> Floating windows are read back from the view, and a layout
/// saved while panes float restores them floating and visible after a restart. Two panes float: one
/// through the service (the product's float verb) and one through AvalonDock's own
/// <see cref="LayoutContent.Float"/>, the native gesture only a read-back can bring into the model.
/// </summary>
/// <remarks>
/// <para><b>The restart leg runs through the store.</b> It saves with <see cref="ZoneLayoutStore.Save"/>,
/// reads with <see cref="ZoneLayoutStore.Read"/>, restores with <see cref="ZoneBackedLayoutService.RestoreZones"/>
/// into a new service, and renders into a new DockingManager. The restart through
/// <c>LayoutPersistence</c> is <c>FloatGuardTests</c> and <c>FloatBoundsPersistTests</c>.</para>
/// <para><b>Placement.</b> The zone file stores each floating stack's bounds (Ruling 174), so both
/// windows keep the bounds the read-back took from the view.</para>
/// </remarks>
public sealed class FloatReadBackRestartTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-float-restart-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void FloatingWindows_AreReadBackFromTheView_AndALayoutSavedWhileTheyFloat_RestoresThemFloatingAndVisible() =>
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (main, border) =>
            {
                static TextBox Build(Surface surface) => new() { Text = surface.SurfaceId };

                var host = FloatRenderTests.HostInto(border, Build);
                var manager = host.Manager;
                var service = host.Service;
                var adapter = host.Adapter;
                await Task.Delay(200);

                // One pane floats the way the palette floats it: the controller verb, which draws it.
                var floatedByTheRoute = FloatRenderTests.FloatByTheRoute(host, "sessions");
                await Task.Delay(300);

                // A second pane floats through AvalonDock's own gesture. It comes from a zone holding two
                // or more panes, so the pane it leaves keeps the zone in the view.
                var nativeId = service.Zones.Zones.Values
                    .Select(z => z.Surfaces().ToList())
                    .First(surfaces => surfaces.Count >= 2 && !floatedByTheRoute.Contains(surfaces[^1].SurfaceId))[^1].SurfaceId;
                manager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == nativeId).Float();
                await Task.Delay(500);
                Assert.Equal(2, manager.Layout.FloatingWindows.Count);

                // Read-back: the view maps, and the model takes both floating panes.
                var view = adapter.ReadLayoutFromView();
                Assert.NotNull(view);
                Assert.Equal(floatedByTheRoute.Append(nativeId).Order(StringComparer.Ordinal).ToArray(), view!.Floating.SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).Order(StringComparer.Ordinal).ToArray(),
                    StringComparer.Ordinal);
                Assert.True(service.ReconcileFromView(view), "the service refused the view's floating windows");
                Assert.All(floatedByTheRoute.Append(nativeId), id =>
                {
                    Assert.Null(service.Zones.FindZoneOf(id));
                    Assert.Contains(service.Zones.Floating, s => s.Surfaces.Any(x => x.SurfaceId == id));
                });

                // In the session the model keeps where the view put the native window, so the next
                // Render reopens it there rather than at the default placement.
                var native = service.Zones.Floating.Single(s => s.Surfaces.Any(x => x.SurfaceId == nativeId));
                Assert.True(native.FloatingBounds is { Width: > 0, Height: > 0 }, "the native floating window's bounds were not read back");

                // Restart: save, read, restore into a new service, and render into a new manager.
                var path = Path.Combine(_dir, "zones.json");
                new ZoneLayoutStore(path).Save(service.Zones);
                var ids = service.Zones.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
                var read = new ZoneLayoutStore(path).Read(ids, new HashSet<string>(StringComparer.Ordinal));
                Assert.Equal(ZoneLoadRefusal.None, read.Refusal);
                Assert.Equal(2, read.Layout!.Floating.Count);
                Assert.All(read.Layout.Floating, s => Assert.Equal(
                    service.Zones.Floating.Single(f => f.Id == s.Id).FloatingBounds, s.FloatingBounds)); // the bounds are in the file (Ruling 174)

                var restarted = new ZoneBackedLayoutService();
                restarted.RestoreZones(read.Layout);
                var manager2 = new DockingManager();
                main.Content = manager2;
                var adapter2 = new WorkbenchAdapter(manager2, restarted, Build);
                adapter2.Render();
                await Task.Delay(500);

                Assert.Equal(2, manager2.Layout.FloatingWindows.Count);
                var windows = manager2.FloatingWindows.ToList();
                Assert.Equal(2, windows.Count);
                Assert.All(windows, w => Assert.True(w.IsVisible, "a restored floating window is not shown"));
                foreach (var id in floatedByTheRoute.Append(nativeId))
                {
                    var content = Assert.IsType<TextBox>(adapter2.ContentFor(id));
                    Assert.Equal(id, content.Text);
                    Assert.True(content.IsVisible, $"{id} is not visible after the restart");
                    Assert.Contains(windows, w => FloatRenderTests.HostedBy(content, w));
                    Assert.False(FloatRenderTests.HostedBy(content, main), $"{id} is drawn in the workbench window after the restart");
                }
            });
}
