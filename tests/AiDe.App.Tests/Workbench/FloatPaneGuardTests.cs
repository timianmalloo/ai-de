using System.Windows.Controls;
using System.Windows.Input;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>The float guard, restored (Leader, Owner's order, 2026-10-19).</b> On main the palette's <i>Float pane</i> floated
/// the pane in the model and drew no window, so the pane vanished while the shell said it was floating (T21 n7's red,
/// <c>ALiveNotebookFloatsAndRedocksTests</c>). Until a layout command renders, <c>workbench.floatPane</c> refuses with
/// DOCK2's wording, T20's guard shape (0ea50971): the pane stays docked and focused.
/// </summary>
/// <remarks>
/// The route is the user's: the command palette over a <see cref="DockHost"/>'s own controller, by Enter. The oracle reads
/// the model, the drawn view and the live region; nothing in the test renders after the command.
/// </remarks>
public sealed class FloatPaneGuardTests
{
    [Fact]
    public void FloatPane_ByThePalette_RefusesWithDOCK2_AndThePaneStaysDockedDrawnAndFocused() =>
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (main, border) =>
            {
                var said = new RecordingAnnouncer();
                var host = DockHost.Create(PerspectiveSet.Factory, surface => new TextBox { Text = surface.SurfaceId }, said, (_, _) => { });
                border.Child = host.Root;
                Assert.True(host.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.CenterStackId, new Surface("session:s1", "notebook", "S1"))).Applied);
                host.Adapter.Render();
                await Task.Delay(200);

                var zones = host.Service.Zones;
                var stack = host.Service.Current.AllStacks().First(s => s.Surfaces.Count > 0 && zones.FindZoneOf(s.Surfaces[s.ActiveIndex].SurfaceId) is not null);
                var surfaceId = stack.Surfaces[stack.ActiveIndex].SurfaceId;
                var zone = zones.FindZoneOf(surfaceId);
                host.Controller.FocusedStackId = stack.Id;
                host.Controller.FocusedSurfaceId = surfaceId;

                var palette = new CommandPalette(host.Controller.Execute, said);
                palette.Open();
                palette.SearchBox.Text = "Float pane";
                Assert.Equal("workbench.floatPane", palette.Visible.FirstOrDefault()?.Id);
                Assert.True(palette.HandleKey(Key.Enter), "the palette did not take Enter");
                await Task.Delay(300);

                Assert.Equal("Floating a pane is not available yet. The pane stays where it is.", said.Last);
                Assert.Empty(host.Service.Zones.Floating);
                Assert.Equal(zone, host.Service.Zones.FindZoneOf(surfaceId));
                Assert.Empty(host.Manager.FloatingWindows);
                var content = Assert.IsType<TextBox>(host.Adapter.ContentFor(surfaceId));
                Assert.True(FloatRenderTests.HostedBy(content, main), "the pane is not drawn in the workbench window");
                Assert.Equal(surfaceId, host.Controller.FocusedSurfaceId);
                Assert.Equal(stack.Id, host.Controller.FocusedStackId);
                border.Child = null;
            });
}
