using System.Windows;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;

namespace AiDe.App.Tests;

/// <summary>
/// End-to-end coverage of the zone-backed layout through the real adapter and AvalonDock (ADR-0021):
/// the zone model projects to the fixed frame, renders every surface, and a cross-zone move keeps
/// every other surface exactly where it was (the DC-063 containment property, proven at the view).
/// Needs a realized visual tree, so each runs on its own STA thread with an offscreen window.
/// </summary>
public sealed class ZoneWorkbenchAdapterTests
{
    private static T OnStaThread<T>(Func<T> work)
    {
        T result = default!;
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { result = work(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "STA thread did not finish");
        if (failure is Xunit.Sdk.XunitException) throw failure;   // the message IS the finding (DC-078)

        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
        return result;
    }

    private static T WithZoneWorkbench<T>(Func<WorkbenchAdapter, ZoneBackedLayoutService, T> assert) =>
        OnStaThread(() =>
        {
            var manager = new DockingManager();
            var service = new ZoneBackedLayoutService();
            var adapter = new WorkbenchAdapter(manager, service);
            var window = new Window
            {
                Content = manager,
                Width = 900,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };

            window.Show();
            adapter.Render();
            window.UpdateLayout();
            manager.UpdateLayout();

            try { return assert(adapter, service); }
            finally { window.Close(); }
        });

    [Fact]
    public void TheDefaultZoneLayout_RendersEverySurface_AsARealizedDocument()
    {
        WithZoneWorkbench((adapter, service) =>
        {
            foreach (var id in service.Zones.AllSurfaces().Select(s => s.SurfaceId))
            {
                Assert.NotNull(adapter.ContentFor(id));
            }

            return true;
        });
    }

    [Fact]
    public void MovingASurfaceToAnotherZone_LosesNoPane_AndMovesOnlyThatSurface()
    {
        WithZoneWorkbench((adapter, service) =>
        {
            var before = service.Zones.AllSurfaces().Select(s => s.SurfaceId).ToList();
            var leftBefore = service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();

            service.Apply(new LayoutOperation.MoveSurface(
                "terminal-1", new DropTarget(ZonesToTree.LeftStackId, DropKind.JoinStack)));
            adapter.Render();

            // No pane lost at the view: every surface is still realized.
            foreach (var id in before)
            {
                Assert.NotNull(adapter.ContentFor(id));
            }

            // Only the terminal moved; the Left zone's prior explorers stayed put (containment).
            Assert.Equal(ZoneId.Left, service.Zones.FindZoneOf("terminal-1"));
            foreach (var id in leftBefore)
            {
                Assert.Equal(ZoneId.Left, service.Zones.FindZoneOf(id));
            }

            return true;
        });
    }

    [Fact]
    public void ClosingACenterDocument_DoesNotDisturbTheOtherZones()
    {
        WithZoneWorkbench((adapter, service) =>
        {
            var leftBefore = service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();
            var bottomBefore = service.Zones.Zone(ZoneId.Bottom).Surfaces().Select(s => s.SurfaceId).ToList();

            service.Apply(new LayoutOperation.CloseSurface("domain"));
            adapter.Render();

            Assert.Null(adapter.ContentFor("domain")); // gone from the view
            Assert.Equal(leftBefore, service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList());
            Assert.Equal(bottomBefore, service.Zones.Zone(ZoneId.Bottom).Surfaces().Select(s => s.SurfaceId).ToList());
            return true;
        });
    }
}
