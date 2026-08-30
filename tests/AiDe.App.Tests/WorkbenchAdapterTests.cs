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
        // Generous: the first WPF window in a process pays for framework initialisation.
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "STA thread did not finish");
        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
        return result;
    }

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

    private sealed class DisposableBorder(Action onDispose) : Border, IDisposable
    {
        public void Dispose() => onDispose();
    }
}
