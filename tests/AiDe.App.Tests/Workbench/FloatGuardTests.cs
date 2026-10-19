using System.Windows.Controls;
using AiDe.App.Tests.Shell;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;

namespace AiDe.App.Tests.Perspectives;

/// <summary>
/// <b>Ruling 171 (2), T21 node 2</b> — a pane saved floating restores floating through
/// <see cref="LayoutPersistence"/>, and T20's restore guard (DOCK3) stays gone.
/// </summary>
/// <remarks>
/// T21 n2's float-pane test (added in 650d65c2) was deleted with the
/// palette guard's return (Owner's order, 2026-10-19). It passed while the palette's Float pane drew 0 windows, because
/// it rendered by hand after the command (<c>adapter.Render()</c>), which no product route does
/// (DC-NEW-wl-t28-compiled's class). <c>LayoutCommandsRenderTests</c> now drives the float through the palette.
/// </remarks>
public sealed class FloatGuardTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-floatguard-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void TheRestoreGuardsCopy_DOCK3_AppearsNowhereInSrcOrDesignMd()
    {
        string[] phrases =
        [
            "Floating is not available yet",
            "floating panes in your saved layout are docked",
        ];
        var root = RepoRoot();
        var files = Directory.EnumerateFiles(Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".xaml", StringComparison.Ordinal) || f.EndsWith(".json", StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Append(Path.Combine(root, "DESIGN.md"));

        var hits = files
            .SelectMany(f => File.ReadAllLines(f).Select((line, i) => (f, i, line)))
            .Where(x => phrases.Any(p => x.line.Contains(p, StringComparison.Ordinal))
                || x.line.StartsWith("| DOCK3 |", StringComparison.Ordinal))
            .Select(x => $"{Path.GetRelativePath(root, x.f)}:{x.i + 1}")
            .ToList();

        Assert.True(hits.Count == 0, $"T20's guard copy is still present: {string.Join(", ", hits)}");
    }

    [Fact]
    public void APaneSavedFloating_RestoresThroughLayoutPersistence_Floating_DrawnVisibleAndNeverDocked() =>
        Sta.Pump(
            create: () => new DockingManager(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (main, manager) =>
            {
                Directory.CreateDirectory(_dir);
                var layoutPath = Path.Combine(_dir, "layout.json");

                // Before the restart: a saved layout with one floating pane. The subject is the restore, so the float is
                // the service's operation, with no view to draw it.
                var before = new ZoneBackedLayoutService();
                var ids = before.Zones.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
                var stack = before.Current.AllStacks().First(s => s.Surfaces.Count > 0 && before.Zones.FindZoneOf(s.Surfaces[0].SurfaceId) is not null);
                var floatedId = stack.Surfaces[0].SurfaceId;
                var floated = before.Apply(new LayoutOperation.SetStackState(stack.Id, StackState.Floating));
                Assert.True(before.Zones.Floating.Any(s => s.Surfaces.Any(x => x.SurfaceId == floatedId)), $"the service did not float {floatedId} in {stack.Id}; it said: {floated.Announcement}");
                using (var first = new LayoutPersistence(before, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds))
                {
                    first.SaveNow();
                    Assert.Null(first.LastSaveFailure);
                }

                // The restart: a new host, LayoutPersistence's real restore, and a render.
                var host = new ZoneBackedLayoutService();
                using var persistence = new LayoutPersistence(host, layoutPath, ids, restorableKinds: SurfaceContentFactory.RestorableKinds);
                var result = persistence.Restore();
                var adapter = new WorkbenchAdapter(manager, host, surface => new TextBox { Text = surface.SurfaceId });
                adapter.Render();
                await Task.Delay(500);

                Assert.True(host.Zones.FindZoneOf(floatedId) is null, $"the restore docked the floating pane; it said: {result.Announcement}");
                Assert.Equal(floatedId, Assert.Single(Assert.Single(host.Zones.Floating).Surfaces).SurfaceId);
                Assert.DoesNotContain("docked", result.Announcement, StringComparison.OrdinalIgnoreCase);
                Assert.Null(result.ErrorCode);
                var window = Assert.Single(manager.FloatingWindows);
                Assert.True(window.IsVisible, "the restored floating window is not shown");
                var content = Assert.IsType<TextBox>(adapter.ContentFor(floatedId));
                Assert.True(FloatRenderTests.HostedBy(content, window), "the restored pane is not drawn in the floating window");
                Assert.False(FloatRenderTests.HostedBy(content, main), "the restored pane is drawn in the workbench window");
                Assert.True(content.IsVisible, "the restored pane is not visible");
            });

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AiDe.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
