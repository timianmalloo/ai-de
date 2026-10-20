using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
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
            create: () => new Border(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (main, border) =>
            {
                var builds = new Dictionary<string, int>(StringComparer.Ordinal);
                var host = HostInto(border, surface =>
                {
                    builds[surface.SurfaceId] = builds.GetValueOrDefault(surface.SurfaceId) + 1;
                    return new TextBox { Text = surface.SurfaceId };
                });
                var manager = host.Manager;
                var service = host.Service;
                var adapter = host.Adapter;
                await Task.Delay(200);
                var dockedBefore = service.Zones.AllSurfaces().Select(s => s.SurfaceId).Where(id => id != "sessions").ToList();

                // The user's route: the controller's float verb on the focused pane (the palette runs it).
                var floatedStack = FloatByTheRoute(host, "sessions");
                dockedBefore = dockedBefore.Except(floatedStack).ToList();
                Assert.True(service.Current.Floating.Count == 1, "the service did not float the pane");
                await Task.Delay(500);

                var model = Assert.Single(manager.Layout.FloatingWindows);
                Assert.IsType<LayoutDocumentFloatingWindow>(model);
                var window = Assert.Single(manager.FloatingWindows);
                Assert.True(window.IsVisible, "the floating window is not shown");

                var content = Assert.IsType<TextBox>(adapter.ContentFor("sessions"));
                Assert.Equal("sessions", content.Text);
                Assert.Equal(1, builds["sessions"]);
                // AvalonDock hosts a floating window's content in its own HwndSource inside that window,
                // so the question "which window draws it" is answered by the HWND chain, not by
                // Window.GetWindow (which follows the inherited window through the logical tree).
                Assert.True(HostedBy(content, window), "the floated content is not drawn inside the floating window");
                Assert.False(HostedBy(content, main), "the floated content is still drawn in the workbench window");
                Assert.True(content.IsVisible, "the floated content is not visible in its window");

                window.Activate();
                Assert.True(content.Focus(), "the floated content did not take focus");
                adapter.ActivateInView("sessions");
                await Task.Delay(200);
                Assert.Equal("sessions", adapter.ActiveSurfaceId);

                // Floating one pane leaves every other pane where it was, still drawn in the docked tree.
                foreach (var id in dockedBefore.Where(id => service.Current.FindStackOf(id) is not null))
                {
                    var docked = adapter.ContentFor(id);
                    Assert.NotNull(docked);
                    Assert.False(HostedBy(docked!, window), $"{id} is drawn in the floating window");
                    Assert.Contains(manager.Layout.RootPanel.Descendents().OfType<LayoutDocument>(), d => d.ContentId == id);
                }
            });

    /// <summary>
    /// A Factory <see cref="DockHost"/> over <paramref name="content"/>, drawn in <paramref name="border"/>, as the shell builds one,
    /// holding a Sessions pane on the left and two notebooks in the Center. Opening panes is setup.
    /// </summary>
    internal static DockHost HostInto(Border border, Func<Surface, FrameworkElement> content)
    {
        var host = DockHost.Create(PerspectiveSet.Factory, content, new RecordingAnnouncer(), (_, _) => { });
        border.Child = host.Root;
        foreach (var (stackId, surface) in new[]
        {
            (ZonesToTree.LeftStackId, new Surface("sessions", "sessions", "Sessions")),
            (ZonesToTree.CenterStackId, new Surface("session:s1", "notebook", "S1")),
            (ZonesToTree.CenterStackId, new Surface("session:s2", "notebook", "S2")),
        })
        {
            if (host.Service.Current.FindStackOf(surface.SurfaceId) is null)
            {
                Assert.True(host.Service.Apply(new LayoutOperation.AddSurface(stackId, surface)).Applied, $"the host refused {surface.SurfaceId}");
            }
        }

        host.Adapter.Render();
        return host;
    }

    /// <summary>
    /// Floats the stack holding <paramref name="surfaceId"/> the way the palette does: it names the focused pane, then runs
    /// <c>workbench.floatPane</c> on the host's own controller, whose <c>LayoutApplied</c> draws it. Returns the surface ids
    /// that floated with it.
    /// </summary>
    internal static List<string> FloatByTheRoute(DockHost host, string surfaceId)
    {
        var stack = host.Service.Current.FindStackOf(surfaceId)!;
        var floated = stack.Surfaces.Select(s => s.SurfaceId).ToList();
        host.Controller.FocusedStackId = stack.Id;
        host.Controller.FocusedSurfaceId = surfaceId;
        Assert.True(host.Controller.Execute("workbench.floatPane"), "the controller refused workbench.floatPane");
        return floated;
    }

    /// <summary>Collapses the stack the way the palette does: the focused pane named, then <c>workbench.collapsePane</c>.</summary>
    internal static void CollapseByTheRoute(DockHost host, string stackId)
    {
        host.Controller.FocusedStackId = stackId;
        Assert.True(host.Controller.Execute("workbench.collapsePane"), "the controller refused workbench.collapsePane");
    }

    /// <summary>Closes a surface the way the palette does: the focused surface named, then <c>workbench.closeSurface</c>.</summary>
    internal static void CloseByTheRoute(DockHost host, string surfaceId)
    {
        host.Controller.FocusedStackId = host.Service.Current.FindStackOf(surfaceId)!.Id;
        host.Controller.FocusedSurfaceId = surfaceId;
        Assert.True(host.Controller.Execute("workbench.closeSurface"), "the controller refused workbench.closeSurface");
    }

    /// <summary>Activates the tab before <paramref name="surfaceId"/> in its stack the way the palette does: <c>workbench.previousSurface</c>.</summary>
    internal static void StepBackByTheRoute(DockHost host, string surfaceId)
    {
        host.Controller.FocusedStackId = host.Service.Current.FindStackOf(surfaceId)!.Id;
        host.Controller.FocusedSurfaceId = surfaceId;
        Assert.True(host.Controller.Execute("workbench.previousSurface"), "the controller refused workbench.previousSurface");
    }

    /// <summary>Expands a collapsed zone the way the user does: the rail's button, invoked through its automation peer (no desktop input).</summary>
    internal static void ExpandByTheRail(DockHost host, ZoneId zone)
    {
        var button = ComposedCoding.Visuals<Button>(host.Root).Single(b => System.Windows.Automation.AutomationProperties.GetName(b) == $"Expand the {zone}");
        var peer = new System.Windows.Automation.Peers.ButtonAutomationPeer(button);
        ((System.Windows.Automation.Provider.IInvokeProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
    }

    /// <summary>True when <paramref name="element"/> is drawn in <paramref name="window"/>'s HWND or one of its child HWNDs.</summary>
    internal static bool HostedBy(Visual element, Window window)
    {
        var host = new WindowInteropHelper(window).Handle;
        return PresentationSource.FromVisual(element) is HwndSource source
            && (source.Handle == host || IsChild(host, source.Handle));
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsChild(IntPtr parent, IntPtr child);
}
