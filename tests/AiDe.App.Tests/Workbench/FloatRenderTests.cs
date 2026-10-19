using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;

namespace AiDe.App.Tests.Shell;

/// <summary>
/// <b>T21 check 1 (Ruling 171 (2)).</b> A floated pane is drawn. <see cref="WorkbenchAdapter.Render"/>
/// projects every stack in <c>Layout.Floating</c> as one of AvalonDock's own floating windows: the
/// window is visible, holds the surface's realized content, the content takes focus, and the surface
/// can be made the active one.
/// </summary>
/// <remarks>
/// Before T21 the render drew only <c>Current.Root</c>. A floated pane was then in the model and on
/// no screen: it could be neither seen nor reached (T20's float guard exists for that reason).
/// </remarks>
public sealed class FloatRenderTests
{
    [Fact]
    public void AFloatedPane_IsDrawnAsADockLibraryFloatingWindow_VisibleFocusableAndHoldingItsContent() =>
        Sta.Pump(
            create: () => new DockingManager(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (_, manager) =>
            {
                var builds = new Dictionary<string, int>(StringComparer.Ordinal);
                var service = new ZoneBackedLayoutService();
                var adapter = new WorkbenchAdapter(manager, service, surface =>
                {
                    builds[surface.SurfaceId] = builds.GetValueOrDefault(surface.SurfaceId) + 1;
                    return new TextBox { Text = surface.SurfaceId };
                });
                adapter.Render();
                await Task.Delay(200);
                var dockedBefore = service.Zones.AllSurfaces().Select(s => s.SurfaceId).Where(id => id != "sessions").ToList();

                var floated = service.Apply(new LayoutOperation.MoveSurface("sessions", new DropTarget(string.Empty, DropKind.Float)));
                Assert.True(floated.Applied, floated.Announcement);
                Assert.True(service.Current.Floating.Count == 1, "the service did not float the pane");
                adapter.Render();
                await Task.Delay(500);

                var model = Assert.Single(manager.Layout.FloatingWindows);
                Assert.IsType<LayoutDocumentFloatingWindow>(model);
                var window = Assert.Single(manager.FloatingWindows);
                Assert.True(window.IsVisible, "the floating window is not shown");

                var content = Assert.IsType<TextBox>(adapter.ContentFor("sessions"));
                Assert.Equal("sessions", content.Text);
                Assert.Equal(1, builds["sessions"]);
                Assert.Same(window, Window.GetWindow(content));
                Assert.True(content.IsVisible, "the floated content is not visible in its window");

                window.Activate();
                Assert.True(content.Focus(), "the floated content did not take focus");
                adapter.ActivateInView("sessions");
                await Task.Delay(200);
                Assert.Equal("sessions", adapter.ActiveSurfaceId);

                // Floating one pane leaves every other pane where it was, still drawn in the docked tree.
                foreach (var id in dockedBefore)
                {
                    Assert.NotNull(adapter.ContentFor(id));
                    Assert.NotSame(window, Window.GetWindow(adapter.ContentFor(id)!));
                }
            });
}
