using System.Windows;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;

namespace AiDe.App.Tests;

/// <summary>
/// End-to-end coverage of the zone-backed layout through the real adapter and AvalonDock (ADR-0021):
/// the zone model projects to the fixed frame, renders every surface, and a cross-zone move keeps
/// every other surface exactly where it was (the DC-063 containment property, proven at the view).
/// Needs a realized visual tree, so each runs on its own STA thread with an offscreen window.
/// </summary>
public sealed class ZoneWorkbenchAdapterTests
{
    private static T OnStaThread<T>(Func<T> work) =>
        Sta.Run<T>(work, 60);

    // A content factory that returns a fresh, identifiable element each call, so a rebuild is
    // observable by reference and a build count. Raw (unwrapped) so ContentFor returns it directly.
    private sealed class CountingFactory
    {
        public int Builds { get; private set; }
        public System.Windows.FrameworkElement Create(Surface s)
        {
            Builds++;
            return new System.Windows.Controls.TextBlock { Text = $"{s.SurfaceId}#{Builds}" };
        }
    }

    private static T WithCountingWorkbench<T>(Func<WorkbenchAdapter, ZoneBackedLayoutService, CountingFactory, T> assert) =>
        OnStaThread(() =>
        {
            var manager = new DockingManager();
            var service = new ZoneBackedLayoutService();
            var factory = new CountingFactory();
            var adapter = new WorkbenchAdapter(manager, service, factory.Create);
            var window = new Window
            {
                Content = manager, Width = 900, Height = 600,
                WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000,
                ShowInTaskbar = false, ShowActivated = false,
            };
            window.Show();
            adapter.Render();
            window.UpdateLayout();
            manager.UpdateLayout();
            try { return assert(adapter, service, factory); }
            finally { window.Close(); }
        });

    [Fact]
    public void RefreshInPlace_RebuildsOnlyTheNamedPane_LeavingEveryOtherPaneUntouched()
    {
        // The watcher refresh must not re-parent the OTHER panes. A full Render() swaps the whole
        // layout, re-firing the graph canvas's ResizeObserver (it re-fits) — the "graph keeps
        // refreshing" in the 2026-09-02 smoke video. In place, only the named surface is rebuilt.
        WithCountingWorkbench((adapter, service, factory) =>
        {
            var layoutBefore = adapter.Manager.Layout;
            var graphBefore = adapter.ContentFor("graph");
            var sessionsBefore = adapter.ContentFor("sessions");

            adapter.RefreshInPlace(["sessions"]);

            Assert.Same(layoutBefore, adapter.Manager.Layout);              // layout root NOT swapped → no re-parent
            Assert.Same(graphBefore, adapter.ContentFor("graph"));          // untouched — no re-fit
            Assert.NotSame(sessionsBefore, adapter.ContentFor("sessions")); // rebuilt against the store
            return true;
        });
    }

    [Fact]
    public void RefreshInPlace_LeavesTheActiveTabWhereTheUserPutIt()
    {
        // The reported regression: sitting on a watcher tab, the periodic Render() snapped the active
        // tab back to the default (graph) because RestoreSelection reads the model, which a mouse
        // tab-click never updated. An in-place refresh does not touch selection, so the user stays put.
        WithCountingWorkbench((adapter, service, factory) =>
        {
            adapter.ActivateInView("leaderboard");
            Assert.Equal("leaderboard", adapter.ActiveSurfaceId);

            // DERIVED from the shell's own set, not restated. Written out, this list disagreed
            // with the product the moment a fifth watcher pane was added — which is DC-021, and
            // the gate caught it on the merge that added "ledger".
            adapter.RefreshInPlace([.. WatcherPaneKinds()]);

            Assert.Equal("leaderboard", adapter.ActiveSurfaceId);  // NOT snapped back to graph
            return true;
        });
    }

    /// <summary>The shell's own watcher-pane set, read from the field rather than repeated.</summary>
    /// <remarks>
    /// A hand-written copy is a second authority on which panes the watcher owns. It agreed with the
    /// product until "ledger" was added, and a fixture that has to be remembered is one that will
    /// eventually be wrong quietly (DC-021).
    /// </remarks>
    private static IReadOnlySet<string> WatcherPaneKinds()
    {
        var field = typeof(WorkbenchShell).GetField(
            "WatcherPaneKinds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(field);
        return (IReadOnlySet<string>)field!.GetValue(null)!;
    }

    // SH-4.1 (DC-194): after a whole-arrangement replacement the pre-render active
    // surface is gone, so the adapter activates the Center's active tab — now, and again one
    // dispatcher turn later, because each docking pane control activates its own selection as it
    // realizes and the last to realize wins (here: the Bottom's `term-x`, measured). The second
    // activation reads the model at that moment: a Center tab the operator activated in between is
    // what gets activated. RED with the deferred turn activating the tab it captured at render
    // time (`doc-b`), and RED without the deferred turn at all (`term-x`).
    [Fact]
    public void AfterAWholeArrangementReplacement_TheCentersActiveTabIsActive_AndAnActivationInBetweenIsKept()
    {
        var (afterRender, afterOperator, afterTurn) = WithZoneWorkbench((adapter, service) =>
        {
            var replacement = WorkbenchLayout.Empty()
                .WithZone(new ZoneState(ZoneId.Center, new ZoneStack(
                [
                    new Surface("doc-a", "codeviewer", "A"),
                    new Surface("doc-b", "codeviewer", "B"),
                    new Surface("doc-c", "codeviewer", "C"),
                ], activeIndex: 1), 1.0, Collapsed: false))
                .WithZone(new ZoneState(ZoneId.Bottom, new ZoneStack([new Surface("term-x", "diagnostics", "Bottom")]), 0.3, Collapsed: false));

            service.RestoreZones(replacement);   // every pre-render surface is gone
            adapter.Render();
            var afterRender = adapter.ActiveSurfaceId;

            // The operator activates another tab before the deferred turn runs — in the model and
            // in the SAME rendered tree (no re-render, so the deferred turn's tree guard does not
            // absorb it; only the model guard can).
            Assert.True(service.Apply(new LayoutOperation.ActivateSurface("doc-c")).Applied);
            adapter.ActivateInView("doc-c");
            var afterOperator = adapter.ActiveSurfaceId;

            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
            return (afterRender, afterOperator, adapter.ActiveSurfaceId);
        });

        // One comparison so a failure shows all three readings at once.
        Assert.Equal("render=doc-b operator=doc-c turn=doc-c", $"render={afterRender} operator={afterOperator} turn={afterTurn}");
    }

    // The kept branch of the same class (the WPF lens's finding, measured in the census window:
    // a Center document active before a plain re-render, the Bottom terminal active after it
    // settled): the pre-render surface still the active tab of its stack is asserted again at the
    // deferred turn, so a rename's or an attach's re-render never moves the operator's focus.
    [Fact]
    public void APlainReRender_KeepsTheActiveCenterDocumentActive_AfterThePaneControlsRealize()
    {
        var (before, immediate, settled) = WithZoneWorkbench((adapter, service) =>
        {
            var arrangement = WorkbenchLayout.Empty()
                .WithZone(new ZoneState(ZoneId.Center, new ZoneStack(
                [
                    new Surface("doc-a", "codeviewer", "A"),
                    new Surface("doc-b", "codeviewer", "B"),
                ], activeIndex: 1), 1.0, Collapsed: false))
                .WithZone(new ZoneState(ZoneId.Bottom, new ZoneStack([new Surface("term-x", "diagnostics", "Bottom")]), 0.3, Collapsed: false));
            service.RestoreZones(arrangement);
            adapter.Render();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
            var before = adapter.ActiveSurfaceId;

            adapter.Render();   // a plain re-render: the model is unchanged, doc-b is still its stack's active tab
            var immediate = adapter.ActiveSurfaceId;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
            return (before, immediate, adapter.ActiveSurfaceId);
        });

        Assert.Equal("before=doc-b immediate=doc-b settled=doc-b", $"before={before} immediate={immediate} settled={settled}");
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

    /// <summary>
    /// A Factory <see cref="DockHost"/> drawn in an offscreen window (Sessions on the left, two notebooks in the Center), so a
    /// layout command reaches the view the way the user's does: through the host's controller, whose <c>LayoutApplied</c> draws it.
    /// </summary>
    private static T WithDockHost<T>(Func<DockHost, T> assert) =>
        OnStaThread(() =>
        {
            var border = new System.Windows.Controls.Border();
            var host = FloatRenderTests.HostInto(border, s => new System.Windows.Controls.TextBlock { Text = s.SurfaceId });
            var window = new Window
            {
                Content = border,
                Width = 900,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };

            window.Show();
            host.Adapter.Render();
            window.UpdateLayout();
            host.Manager.UpdateLayout();

            try { return assert(host); }
            finally { window.Close(); }
        });

    [Fact]
    public void MovingASurfaceToAnotherZone_LosesNoPane_AndMovesOnlyThatSurface()
    {
        WithDockHost(host =>
        {
            var adapter = host.Adapter;
            var service = host.Service;
            // A collapsed zone parks its panes unbuilt (Ruling 88), so the panes drawn are the expanded zones'.
            var before = Enum.GetValues<ZoneId>().Where(z => !service.Zones.Zone(z).Collapsed)
                .SelectMany(z => service.Zones.Zone(z).Surfaces()).Select(s => s.SurfaceId).ToList();
            var leftBefore = service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();

            Assert.True(host.Controller.Move("session:s2", new DropTarget(ZonesToTree.LeftStackId, DropKind.JoinStack)));

            // No pane lost at the view: every surface is still realized.
            foreach (var id in before)
            {
                Assert.True(adapter.ContentFor(id) is not null, $"{id} has no drawn content after the move");
            }

            // Only the moved notebook moved; the Left zone's prior explorers stayed put (containment).
            Assert.Equal(ZoneId.Left, service.Zones.FindZoneOf("session:s2"));
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
        WithDockHost(host =>
        {
            var adapter = host.Adapter;
            var service = host.Service;
            var leftBefore = service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList();
            var bottomBefore = service.Zones.Zone(ZoneId.Bottom).Surfaces().Select(s => s.SurfaceId).ToList();

            FloatRenderTests.CloseByTheRoute(host, "session:s1");

            Assert.Null(adapter.ContentFor("session:s1")); // gone from the view
            Assert.Equal(leftBefore, service.Zones.Zone(ZoneId.Left).Surfaces().Select(s => s.SurfaceId).ToList());
            Assert.Equal(bottomBefore, service.Zones.Zone(ZoneId.Bottom).Surfaces().Select(s => s.SurfaceId).ToList());
            return true;
        });
    }

    // ---- #11 / #3-focus: a re-render keeps the model's active tab and the user's focus ----

    [Fact]
    public void Render_SelectsTheModelsActiveTab_NotTheFirstDocument()
    {
        AiDe.App.Tests.Sta.Pump(
            create: () => new System.Windows.Controls.Border(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (main, border) =>
            {
                var host = AiDe.App.Tests.Shell.FloatRenderTests.HostInto(border, surface => new System.Windows.Controls.TextBox { Text = surface.SurfaceId });
                await Task.Delay(200);

                // What the Center's pane shows as selected, read from the drawn layout.
                string? SelectedInTheView() =>
                    (host.Manager.Layout!.Descendents().OfType<LayoutDocumentPane>()
                        .First(p => p.Children.OfType<LayoutDocument>().Any(d => d.ContentId == "session:s2")).SelectedContent as LayoutDocument)?.ContentId;

                // The Center is stepped back to its first tab, then forward: the user's verb makes a later tab the model's active one.
                host.Controller.FocusedStackId = host.Service.Current.FindStackOf("session:s1")!.Id;
                host.Controller.FocusedSurfaceId = "session:s2";
                Assert.True(host.Controller.Execute("workbench.previousSurface"));
                await Task.Delay(200);
                Assert.Equal("session:s1", SelectedInTheView());

                Assert.True(host.Controller.Execute("workbench.nextSurface"));
                await Task.Delay(200);

                // The rebuilt pane shows the model's active tab, not its first document (the desync
                // that hid the surviving tab after a close, #11).
                Assert.Equal("session:s2", SelectedInTheView());
            });
    }

    [Fact]
    public void Render_PreservesTheActiveSurface_WhenAnotherPaneOpens()
    {
        WithZoneWorkbench((adapter, service) =>
        {
            // The user is focused in the graph document.
            var graph = adapter.Manager.Layout!.Descendents().OfType<LayoutDocument>()
                .First(d => d.ContentId == "graph");
            graph.IsActive = true;
            Assert.Equal("graph", adapter.ActiveSurfaceId); // setup: focus is on the graph

            // Opening a reference document elsewhere must not snap focus to another pane (#3-focus).
            service.Apply(new LayoutOperation.AddSurface(
                ZonesToTree.RightStackId, new Surface("src-1", "codeviewer", "Source")));
            adapter.Render();

            Assert.Equal("graph", adapter.ActiveSurfaceId); // focus preserved across the layout swap
            return true;
        });
    }

    [Fact]
    public void ActivateInView_FocusesTheNamedSurface_ForTheDeliberateOpenCase()
    {
        WithZoneWorkbench((adapter, service) =>
        {
            // Open a terminal in the Bottom and focus it, as the session-open path does.
            service.Apply(new LayoutOperation.AddSurface(
                ZonesToTree.BottomStackId, new Surface("agent:claude#abc", "terminal", "Claude")));
            adapter.Render();
            adapter.ActivateInView("agent:claude#abc");

            Assert.Equal("agent:claude#abc", adapter.ActiveSurfaceId); // focus landed on the new session
            return true;
        });
    }
}
