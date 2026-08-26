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

        Assert.Equal(4, titles.Count);
        Assert.Contains("Explore", titles);
        Assert.Contains("Provenance", titles);
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
}
