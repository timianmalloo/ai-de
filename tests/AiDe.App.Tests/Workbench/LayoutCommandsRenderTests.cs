using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock.Layout;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>DC-NEW-wl-t21-float-d's sweep (T21 n8).</b> A layout command run through the controller changes the model and
/// is drawn. Before the fix no controller-routed command rendered (<c>WorkbenchController.ApplyAndAnnounce</c>), so the
/// model and the view disagreed until an unrelated render: a float vanished, and a close, a collapse, a maximize or a
/// next tab said it was done while the screen showed the layout before it.
/// </summary>
/// <remarks>
/// <para><b>The route is the user's</b>: the command palette over a <see cref="DockHost"/>'s own controller, by Enter,
/// as the shell builds it (<c>WorkbenchShell</c>: <c>new CommandPalette(Execute, Announcer)</c>, and every route ends in
/// <c>host.Controller.Execute</c>). Opening the panes and naming the focused pane are setup. Nothing in a test renders
/// after the command, except where a test reproduces a product route that renders a second time.</para>
/// <para><b>The oracle is the drawn view</b>: the documents in the <see cref="DockHost.Manager"/>'s layout, the selected
/// tab, and the floating windows. The model is read only to say what the view should show.</para>
/// </remarks>
public sealed class LayoutCommandsRenderTests
{
    private const string First = "session:s1";
    private const string Second = "session:s2";
    private const string Terminal = "terminal#t1";

    [Fact]
    public void FloatPane_ByThePalette_IsDrawnInAFloatingWindow_AndASecondRenderKeepsIt() =>
        WithHost((main, host, said, run) =>
        {
            Focus(host, Second);
            var content = host.Adapter.ContentFor(Second)!;

            run("Float pane");

            Assert.True(host.Service.Zones.Floating.Count == 1, $"the model did not float the pane; it said: {said.Last}");
            var window = Assert.Single(host.Manager.FloatingWindows);
            Assert.True(window.IsVisible, "the floating window is not shown");
            Assert.True(FloatRenderTests.HostedBy(content, window), $"the floated pane is not drawn in its floating window; it said: {said.Last}");
            Assert.False(FloatRenderTests.HostedBy(content, main), "the floated pane is still drawn in the workbench window");

            // The floating window's tab is a workbench tab: a screen reader hears its title, and it carries the menu that
            // redocks it. Both used to be applied to the DockingManager's own tree only. The window's content is in a child
            // HWND, so the walk crosses it as the adapter does.
            var tab = WorkbenchAdapter.HostedRoots(window).SelectMany(ComposedCoding.Visuals<TabItem>).FirstOrDefault(t => t.DataContext is LayoutContent { ContentId: Second });
            Assert.True(tab is not null, "the floating window draws no tab for the pane");
            Assert.Equal("S2", AutomationProperties.GetName(tab!));
            Assert.True(tab!.ContextMenu?.Items.OfType<MenuItem>().Any(i => Equals(i.Header, "Move to")) == true, "the floating tab carries no Move to menu");

            // A route that renders again after the command (MainWindow's reset renders after Shell.Execute) is harmless:
            // the reconcile keeps the same content, still in one floating window, and closes nothing.
            var closed = Closed(host);
            host.Adapter.Render();
            Pump(main);
            Assert.Same(content, host.Adapter.ContentFor(Second));
            Assert.True(FloatRenderTests.HostedBy(content, Assert.Single(host.Manager.FloatingWindows)), "a second render moved the floated pane out of its window");
            Assert.Empty(closed);
        });

    [Fact]
    public void CloseSurface_ByThePalette_RemovesTheTabFromTheView() =>
        WithHost((main, host, said, run) =>
        {
            Focus(host, Second);

            run("Close surface");

            Assert.Null(host.Service.Zones.FindZoneOf(Second));
            Assert.DoesNotContain(Second, Drawn(host));
            Assert.Contains(First, Drawn(host));
        });

    [Fact]
    public void CollapsePane_ByThePalette_TakesTheZonesTabsOffTheView() =>
        WithHost((main, host, said, run) =>
        {
            Focus(host, Terminal);

            run("Collapse pane");

            Assert.True(host.Service.Zones.Zone(ZoneId.Bottom).Collapsed, $"the model did not collapse the Bottom zone; it said: {said.Last}");
            Assert.DoesNotContain(Terminal, Drawn(host));
            Assert.Contains(First, Drawn(host));
        });

    [Fact]
    public void MaximizePane_ByThePalette_LeavesOnlyTheMaximizedZoneDrawn() =>
        WithHost((main, host, said, run) =>
        {
            Focus(host, Second);

            run("Maximize pane");

            Assert.True(host.Service.Zones.Maximized is not null, $"the model did not maximize; it said: {said.Last}");
            Assert.DoesNotContain(Terminal, Drawn(host));
            Assert.Contains(Second, Drawn(host));
        });

    [Fact]
    public void NextTab_ByThePalette_SelectsTheModelsActiveTabInTheView() =>
        WithHost((main, host, said, run) =>
        {
            Focus(host, Second);
            var before = ActiveInCenter(host);

            run("Next tab in pane");

            var after = ActiveInCenter(host);
            Assert.NotEqual(before, after);
            var selected = host.Manager.Layout.Descendents().OfType<LayoutDocument>().Where(d => d.IsSelected).Select(d => d.ContentId).ToList();
            Assert.True(selected.Contains(after), $"the model's active tab is {after}, but the view selects [{string.Join(", ", selected)}]; it said: {said.Last}");
            Assert.DoesNotContain(before, selected);
        });

    /// <summary>
    /// A Factory host drawn in a window with two notebooks in the Center and a terminal in the Bottom zone, and a
    /// palette over its controller; <c>run(title)</c> types the title and presses Enter.
    /// </summary>
    private static void WithHost(Action<Window, DockHost, RecordingAnnouncer, Action<string>> body) =>
        Sta.Pump(
            create: () => new Border(),
            configure: window => { window.Width = 1200; window.Height = 800; },
            body: async (main, border) =>
            {
                var said = new RecordingAnnouncer();
                var host = DockHost.Create(PerspectiveSet.Factory, surface => new TextBox { Text = surface.SurfaceId }, said, (_, _) => { });
                border.Child = host.Root;
                Assert.True(host.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.CenterStackId, new Surface(First, "notebook", "S1"))).Applied);
                Assert.True(host.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.CenterStackId, new Surface(Second, "notebook", "S2"))).Applied);
                Assert.True(host.Service.Apply(new LayoutOperation.AddSurface(ZonesToTree.BottomStackId, new Surface(Terminal, "terminal", "Terminal"))).Applied);
                host.Adapter.Render();
                await Task.Delay(200);
                Assert.Contains(Second, Drawn(host));
                Assert.Contains(Terminal, Drawn(host));

                var palette = new CommandPalette(host.Controller.Execute, said);
                void Run(string title)
                {
                    palette.Open();
                    palette.SearchBox.Text = title;
                    Assert.Equal(title, palette.Visible.FirstOrDefault()?.Title);
                    Assert.True(palette.HandleKey(Key.Enter), "the palette did not take Enter");
                    Pump(main);
                }

                body(main, host, said, Run);
                await Task.Delay(100);
                border.Child = null;
            });

    private static void Focus(DockHost host, string surfaceId)
    {
        host.Controller.FocusedStackId = host.Service.Current.FindStackOf(surfaceId)!.Id;
        host.Controller.FocusedSurfaceId = surfaceId;
    }

    private static string ActiveInCenter(DockHost host) =>
        ((ZoneStack)host.Service.Zones.Zone(ZoneId.Center).Content!).Active.SurfaceId;

    private static HashSet<string> Drawn(DockHost host) =>
        host.Manager.Layout.Descendents().OfType<LayoutDocument>()
            .Select(d => d.ContentId)
            .Where(id => id is not null)
            .ToHashSet(StringComparer.Ordinal)!;

    private static List<string> Closed(DockHost host)
    {
        var closed = new List<string>();
        host.Adapter.SurfaceClosed += (id, _) => closed.Add(id);
        return closed;
    }

    /// <summary>Runs the window's pending layout and render work, so the view reads what a user would see.</summary>
    private static void Pump(Window window)
    {
        window.UpdateLayout();
        window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
        window.UpdateLayout();
    }
}
