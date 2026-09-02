using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;

namespace AiDe.App.Tests;

/// <summary>
/// The adapter obligations the UIA probe established (spikes/avalondock-a11y/RESULT.md).
/// These need a realized visual tree, so each runs on its own STA thread with a real (offscreen)
/// window — a headless assertion would not exercise the tab realization that causes the defect.
/// </summary>
public sealed class WorkbenchAdapterTests
{
    /// <summary>Runs WPF work on a dedicated STA thread and returns its result.</summary>
    private static T OnStaThread<T>(Func<T> work) =>
        Sta.Run<T>(work, 60);

    /// <summary>Realizes the workbench offscreen and hands the caller the live tree.</summary>
    private static T WithRealizedWorkbench<T>(Func<WorkbenchAdapter, T> assert) => OnStaThread(() =>
    {
        var manager = new DockingManager();
        var adapter = new WorkbenchAdapter(manager, new LayoutService());
        var window = new Window
        {
            Content = manager,
            Width = 900,
            Height = 600,
            // Offscreen and never activated: this is a test fixture, not a UI the runner should show.
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
        adapter.ApplyAccessibleNames();

        try { return assert(adapter); }
        finally { window.Close(); }
    });

    // The defect the probe found: every tab announced as "AvalonDock.Layout.LayoutDocument".
    // Fails RED against an adapter without the naming pass.
    [Fact]
    public void EveryTab_IsNamedFromItsSurfaceTitle_NotItsTypeName()
    {
        var names = WithRealizedWorkbench(a =>
            WorkbenchAdapter.AutomationNames(a.Manager).ToList());

        Assert.NotEmpty(names);
        foreach (var expected in new[] { "Explore", "Domain", "Terminal — pwsh", "Provenance" })
        {
            Assert.Contains(expected, names);
        }
    }

    // The regression control specified in the Phase-1b design. This exact defect would otherwise
    // return silently on any AvalonDock upgrade, and reflection cannot see it.
    [Fact]
    public void NoAutomationName_LeaksTheLibrarysTypeName()
    {
        var leaked = WithRealizedWorkbench(a => WorkbenchAdapter.AutomationNames(a.Manager)
            .Where(n => n.StartsWith(WorkbenchAdapter.LeakedNamePrefix, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList());

        Assert.True(leaked.Count == 0,
            "automation names leaked the library's type names: " + string.Join(", ", leaked));
    }

    private static List<AvalonDock.Layout.LayoutDocument> Documents(AvalonDock.Layout.ILayoutElement root)
    {
        var found = new List<AvalonDock.Layout.LayoutDocument>();
        void Walk(AvalonDock.Layout.ILayoutElement e)
        {
            if (e is AvalonDock.Layout.LayoutDocument d) { found.Add(d); }
            if (e is AvalonDock.Layout.ILayoutContainer c)
            {
                foreach (var child in c.Children) { Walk(child); }
            }
        }

        Walk(root);
        return found;
    }

    [Fact]
    public void Render_ProjectsEverySurfaceFromTheModel()
    {
        var titles = WithRealizedWorkbench(a =>
            Documents(a.Manager.Layout).Select(d => d.Title).ToList());

        // Derived from the model rather than typed: the assertion is "every surface is projected",
        // and a hardcoded count turns adding a surface into an unrelated test failure that says
        // nothing about whether projection works.
        var expected = AiDe.Core.Workbench.Layout.Default()
            .AllStacks().SelectMany(stack => stack.Surfaces).Count();

        Assert.Equal(expected, titles.Count);
        Assert.Contains("Explore", titles);
        Assert.Contains("Provenance", titles);
        Assert.Contains("Graph", titles);
    }

    // ContentId is what reunites a restored layout with its content, so it must be the stable
    // surface identity rather than the display title.
    [Fact]
    public void EveryDocument_CarriesItsSurfaceIdAsContentId()
    {
        var ids = WithRealizedWorkbench(a =>
            Documents(a.Manager.Layout).Select(d => d.ContentId).ToList());

        Assert.Contains("explore", ids);
        Assert.Contains("terminal-1", ids);
        Assert.DoesNotContain("Explore", ids);   // the title, not the id
    }

    // DC-029 control. Opening a second terminal — any layout mutation — must NOT rebuild the panes
    // that did not change: a rebuilt terminal is a fresh ConPTY process and the running one is lost.
    // RED before the reconcile fix (the factory was invoked again for terminal-1 and ContentFor
    // returned a new instance); GREEN after.
    [Fact]
    public void Render_ReusesExistingContent_WhenLayoutMutates_SoLiveSurfacesSurvive()
    {
        OnStaThread<object?>(() =>
        {
            var service = new LayoutService();
            var created = new Dictionary<string, int>(StringComparer.Ordinal);
            FrameworkElement Factory(Surface s)
            {
                created[s.SurfaceId] = created.TryGetValue(s.SurfaceId, out var n) ? n + 1 : 1;
                return new Border();
            }

            var manager = new DockingManager();
            var adapter = new WorkbenchAdapter(manager, service, Factory);
            var window = new Window
            {
                Content = manager, Width = 900, Height = 600,
                WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000,
                ShowInTaskbar = false, ShowActivated = false,
            };
            window.Show();
            adapter.Render();

            var terminalBefore = adapter.ContentFor("terminal-1");
            Assert.NotNull(terminalBefore);

            // The reported trigger: open a second terminal beside the first.
            var terminalStack = service.Current.AllStacks()
                .First(s => s.Surfaces.Any(su => su.Kind == "terminal"));
            service.Apply(new LayoutOperation.AddSurface(
                terminalStack.Id, new Surface("terminal-2", "terminal", "Terminal 2")));
            adapter.Render();

            // Same instance: the pre-existing terminal was reused, not rebuilt, so a live session on
            // it would still be alive.
            Assert.Same(terminalBefore, adapter.ContentFor("terminal-1"));
            // Built exactly once — twice would mean it was reconstructed (the kill).
            Assert.Equal(1, created["terminal-1"]);
            // The genuinely new surface was built.
            Assert.Equal(1, created["terminal-2"]);

            window.Close();
            return null;
        });
    }

    // A surface that was CLOSED must have its content disposed deterministically, so a closed
    // terminal's process ends now rather than lingering until a finalizer runs.
    [Fact]
    public void Render_DisposesContent_OfSurfacesThatWereClosed()
    {
        OnStaThread<object?>(() =>
        {
            var service = new LayoutService();
            var disposed = new List<string>();
            FrameworkElement Factory(Surface s) => new DisposableBorder(() => disposed.Add(s.SurfaceId));

            var manager = new DockingManager();
            var adapter = new WorkbenchAdapter(manager, service, Factory);
            var window = new Window
            {
                Content = manager, Width = 900, Height = 600,
                WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000,
                ShowInTaskbar = false, ShowActivated = false,
            };
            window.Show();
            adapter.Render();

            service.Apply(new LayoutOperation.CloseSurface("provenance"));
            adapter.Render();

            Assert.Contains("provenance", disposed);      // the closed one ended
            Assert.DoesNotContain("explore", disposed);   // a surviving one did not

            window.Close();
            return null;
        });
    }

    // Regression control: BuildPanel dropped SplitNode.Weights, so every projected pane got an equal
    // 1* share — the terminal pane could not be resized and a restored layout lost its proportions.
    // RED before weights are projected (every DockHeight/DockWidth is the default 1*), GREEN after.
    [Fact]
    public void Render_AppliesModelSplitWeights_AsProportionalDockSizing()
    {
        var (rootHeights, columnWidths) = WithRealizedWorkbench(a =>
        {
            var root = a.Manager.Layout.RootPanel;                       // vertical split-root
            var heights = root.Children.Select(DockHeightOf).ToList();
            var columns = (AvalonDock.Layout.LayoutPanel)root.Children[0]; // horizontal split-columns
            var widths = columns.Children.Select(DockWidthOf).ToList();
            return (heights, widths);
        });

        // Vertical root split [columns 0.68, terminal 0.32] projected as star heights.
        Assert.Equal(2, rootHeights.Count);
        Assert.All(rootHeights, h => Assert.Equal(GridUnitType.Star, h.GridUnitType));
        Assert.Equal(0.68, rootHeights[0].Value, 3);
        Assert.Equal(0.32, rootHeights[1].Value, 3);

        // Horizontal columns split [workspace 0.38, graph 0.62] projected as star widths.
        Assert.Equal(2, columnWidths.Count);
        Assert.All(columnWidths, w => Assert.Equal(GridUnitType.Star, w.GridUnitType));
        Assert.Equal(0.38, columnWidths[0].Value, 3);
        Assert.Equal(0.62, columnWidths[1].Value, 3);
    }

    private static GridLength DockHeightOf(AvalonDock.Layout.ILayoutPanelElement e) => e switch
    {
        AvalonDock.Layout.LayoutPanel p => p.DockHeight,
        AvalonDock.Layout.LayoutDocumentPane d => d.DockHeight,
        _ => throw new InvalidOperationException("unexpected pane " + e.GetType().Name),
    };

    private static GridLength DockWidthOf(AvalonDock.Layout.ILayoutPanelElement e) => e switch
    {
        AvalonDock.Layout.LayoutPanel p => p.DockWidth,
        AvalonDock.Layout.LayoutDocumentPane d => d.DockWidth,
        _ => throw new InvalidOperationException("unexpected pane " + e.GetType().Name),
    };

    // The regression for the empty class diagram: SurfaceChrome.WrapAsIsland frames a non-windowed
    // pane's content in a Border, so ContentFor(id) returns the Border — and a bind that did
    // ContentFor(id).OfType<ClassDiagramSurface>() found nothing and never populated the pane, which
    // sat on its construction-time empty state over a fully indexed workspace. SurfaceContent<T> must
    // look THROUGH the island. Fails RED without the unwrap branch.
    [Fact]
    public void SurfaceContent_LooksThroughIslandChrome_WhileContentForReturnsTheWrapper()
    {
        var (throughHelper, viaContentFor) = OnStaThread(() =>
        {
            var service = new LayoutService();
            var stackId = service.Current.AllStacks().First().Id;
            const string id = "classdiagram#test01";
            service.Apply(new LayoutOperation.AddSurface(
                stackId, new Surface(id, "classdiagram", "Class diagram")));

            var manager = new DockingManager();
            var adapter = new WorkbenchAdapter(manager, service,
                surface => surface.Kind == "classdiagram"
                    ? SurfaceChrome.WrapAsIsland(new ClassDiagramSurface(surface.Title))
                    : new ContentControl());

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

            try
            {
                return (adapter.SurfaceContent<ClassDiagramSurface>(id), adapter.ContentFor(id));
            }
            finally { window.Close(); }
        });

        Assert.NotNull(throughHelper);          // the helper unwraps the island and finds the surface
        Assert.IsType<Border>(viaContentFor);   // documents WHY: the raw content is the framing Border
    }

    // The reverse mapper that makes native pane drags survive a rebuild: rendering a model then reading
    // it back must reproduce the same stacks with the same surfaces. If forward-then-reverse were not the
    // identity, reconciling before an add would silently rearrange the user's panes. Compares the
    // surface-grouping (which surfaces share a stack), ignoring stack order and freshly-minted node ids.
    [Fact]
    public void ReadLayoutFromView_RoundTripsTheRenderedModel_PreservingStacksAndSurfaces()
    {
        var (before, after) = OnStaThread(() =>
        {
            var service = new LayoutService();   // Layout.Default(): workspace / graph+domain / console
            var manager = new DockingManager();
            var adapter = new WorkbenchAdapter(manager, service, _ => new ContentControl());

            var window = new Window
            {
                Content = manager,
                Width = 1000,
                Height = 700,
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

            try
            {
                var input = Groupings(service.Current);
                var roundTripped = adapter.ReadLayoutFromView();
                return (input, roundTripped is null ? null : Groupings(roundTripped));
            }
            finally { window.Close(); }
        });

        Assert.NotNull(after);
        Assert.Equal(before, after);
    }

    // Fail-safe: with nothing rendered there is no arrangement to read, so the reconcile REFUSES (null)
    // rather than inventing an empty layout that a Render would then apply, dropping every pane.
    [Fact]
    public void ReadLayoutFromView_RefusesWhenTheViewDoesNotHoldTheModelsSurfaces()
    {
        var result = OnStaThread(() =>
        {
            var manager = new DockingManager();   // never rendered — its layout does not hold the surfaces
            var adapter = new WorkbenchAdapter(manager, new LayoutService());
            return adapter.ReadLayoutFromView();
        });

        Assert.Null(result);
    }

    // Focus-aware placement reads AvalonDock's active document, so a new pane can open where the user is
    // looking. After a render the active content is one of the model's surfaces, never a stale id.
    [Fact]
    public void ActiveSurfaceId_AfterRender_IsOneOfTheModelsSurfaces()
    {
        var (active, known) = OnStaThread(() =>
        {
            var service = new LayoutService();
            var manager = new DockingManager();
            var adapter = new WorkbenchAdapter(manager, service, _ => new ContentControl());
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
            try
            {
                var ids = service.Current.AllStacks()
                    .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
                return (adapter.ActiveSurfaceId, ids);
            }
            finally { window.Close(); }
        });

        // Null is acceptable (nothing active); a non-null id must be a real surface, never invented.
        if (active is not null) { Assert.Contains(active, known); }
    }

    private static List<string> Groupings(AiDe.Core.Workbench.Layout layout) =>
        layout.AllStacks()
            .Select(s => string.Join(
                ",", s.Surfaces.Select(x => x.SurfaceId).OrderBy(x => x, StringComparer.Ordinal)))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

    private sealed class DisposableBorder(Action onDispose) : Border, IDisposable
    {
        public void Dispose() => onDispose();
    }
}
