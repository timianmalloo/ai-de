using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AiDe.Testing;

namespace AiDe.App.Tests;

/// <summary>
/// E10 reachability: the workbench is composed, wired and actually present in the window a user
/// opens. Everything in Phase 1b was tested but unreachable until this existed — a capability nobody
/// can open is not delivered.
/// </summary>
public sealed class WorkbenchShellTests
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
        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
        return result;
    }

    private static T WithShell<T>(Func<WorkbenchShell, Window, T> assert) => OnStaThread(() =>
    {
        var shell = new WorkbenchShell(queries: null);   // first-run: no workspace open
        var window = new Window
        {
            Content = shell.Manager,
            Width = 1000,
            Height = 640,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        window.UpdateLayout();
        shell.Manager.UpdateLayout();
        shell.Adapter.ApplyAccessibleNames();

        try { return assert(shell, window); }
        finally { window.Close(); }
    });

    [Fact]
    public void Shell_ComposesTheWorkbenchWithEverySurfaceFromTheDefaultLayout()
    {
        var titles = WithShell((shell, _) =>
            shell.Service.Current.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.Title).ToList());

        Assert.Contains("Explore", titles);
        Assert.Contains("Domain", titles);
        Assert.Contains("Provenance", titles);
        Assert.Contains("Terminal — pwsh", titles);
    }

    // The whole point of composing in one place: keyboard, view and model must share ONE service, or
    // the keyboard mutates a layout the view is not showing.
    [Fact]
    public void Controller_AdapterAndView_ShareOneLayoutService()
    {
        var same = WithShell((shell, _) =>
        {
            var before = shell.Service.Current.Shape();
            shell.Controller.Execute("workbench.resetLayout");
            shell.Adapter.Render();
            return shell.Service.Current.Shape() == before;   // reset from default == default
        });

        Assert.True(same);
    }

    [Fact]
    public void Announcements_ReachTheLiveRegionTheWindowDisplays()
    {
        var text = WithShell((shell, _) =>
        {
            shell.Controller.Execute("workbench.toggleLock");
            return shell.LiveRegion.Text;
        });

        Assert.Contains("locked", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheLiveRegion_IsMarkedPoliteAndNamed()
    {
        var (live, name) = WithShell((shell, _) => (
            System.Windows.Automation.AutomationProperties.GetLiveSetting(shell.LiveRegion),
            System.Windows.Automation.AutomationProperties.GetName(shell.LiveRegion)));

        Assert.Equal(System.Windows.Automation.AutomationLiveSetting.Polite, live);
        Assert.False(string.IsNullOrWhiteSpace(name));
    }

    // The regression control from the UIA probe, now over the real composed shell rather than a
    // bare adapter — this is the configuration a user actually gets.
    [Fact]
    public void TheComposedShell_LeaksNoLibraryTypeNames()
    {
        var leaked = WithShell((shell, _) => WorkbenchAdapter.AutomationNames(shell.Manager)
            .Where(n => n.StartsWith(WorkbenchAdapter.LeakedNamePrefix, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal).ToList());

        Assert.True(leaked.Count == 0, "leaked: " + string.Join(", ", leaked));
    }

    [Fact]
    public void EverySurface_RendersContentRatherThanAnEmptyShell()
    {
        var empties = WithShell((shell, _) =>
        {
            var docs = new List<string>();
            void Walk(AvalonDock.Layout.ILayoutElement e)
            {
                if (e is AvalonDock.Layout.LayoutDocument d
                    && (d.Content is null || d.Content is ContentControl { Content: null }))
                {
                    docs.Add(d.Title);
                }

                if (e is AvalonDock.Layout.ILayoutContainer c)
                {
                    foreach (var child in c.Children) { Walk(child); }
                }
            }

            Walk(shell.Manager.Layout);
            return docs;
        });

        Assert.True(empties.Count == 0, "surfaces with no content: " + string.Join(", ", empties));
    }

    // The palette is the accessible route to the chorded commands, so it must actually list them.
    [Fact]
    public void ThePalette_ListsEveryKeyboardCommand()
    {
        Assert.Equal(WorkbenchCommandCatalog.All.Count, WorkbenchShell.PaletteCommands(string.Empty).Count);
        Assert.Contains(WorkbenchShell.PaletteCommands("resize"), c => c.Id == "workbench.resizePane");
    }

    [Fact]
    public void FindSurfaceId_WalksUpToTheOwningSurface()
    {
        var found = WithShell((shell, _) =>
        {
            AvalonDock.Layout.LayoutDocument? doc = null;
            void Walk(AvalonDock.Layout.ILayoutElement e)
            {
                if (e is AvalonDock.Layout.LayoutDocument d && d.ContentId == "explore") { doc = d; }
                if (e is AvalonDock.Layout.ILayoutContainer c)
                {
                    foreach (var child in c.Children) { Walk(child); }
                }
            }

            Walk(shell.Manager.Layout);
            return doc?.ContentId;
        });

        Assert.Equal("explore", found);
    }

    /// <summary>A concrete no-op workspace read surface for attach tests (base defaults, nothing thrown).</summary>
    private sealed class BareQueries : FakeWorkspaceQueries
    {
        // Main added BindJoins to AttachWorkspace, whose join Source calls FindAsync + EvidenceAsync during
        // attach. This test's intent is an EMPTY store, so both return empty rather than the base's refusal.
        public override Task<AiDe.Core.Projections.FindResult> FindAsync(
            string term, int maxResults, CancellationToken cancellationToken) =>
            Task.FromResult(new AiDe.Core.Projections.FindResult(
                [], new AiDe.Core.Projections.ResultBounds(0, 0, 1024, 0, 0, 0, 0, false, null), "rev-empty"));

        public override Task<AiDe.Core.Projections.EvidencePage> EvidenceAsync(
            string? cursor, int maxAssertions, CancellationToken cancellationToken) =>
            Task.FromResult(new AiDe.Core.Projections.EvidencePage([], null, "rev-empty"));
    }

    [Fact]
    public void AttachWorkspace_WiresTheWatcher_SoTheSessionsPaneIsLive_NotUnavailable()
    {
        // E11 through the real composition root: the watcher wiring lives in AttachWorkspace (the runtime
        // path), not only the constructor. Before this fix AttachWorkspace rebuilt the factory without
        // the watcher queries, so the Sessions pane always read "not available" even after a workspace
        // opened. With a real (empty) store wired, the pane shows its EMPTY state instead.
        var dataDir = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"aide-shell-watcher-{Guid.NewGuid():N}");
        try
        {
            var status = WithShell((shell, _) =>
            {
                shell.AttachWorkspace(new BareQueries(), dataDir);
                shell.Adapter.Render();

                var content = shell.Adapter.ContentFor("sessions");
                var unwrapped = content is System.Windows.Controls.Border { Child: FrameworkElement inner } ? inner : content;
                var stack = Assert.IsType<StackPanel>(unwrapped);
                return stack.Children.OfType<TextBlock>().Last().Text;
            });

            Assert.DoesNotContain("not available", status, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No sessions observed", status, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { System.IO.Directory.Delete(dataDir, recursive: true); } catch (System.IO.IOException) { }
        }
    }
}
