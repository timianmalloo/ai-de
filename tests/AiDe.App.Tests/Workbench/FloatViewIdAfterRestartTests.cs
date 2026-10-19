using System.Reflection;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;

namespace AiDe.App.Tests.Shell;

/// <summary>
/// <b>T21 node 6 check (C).</b> A window the user floats natively never takes the id of a floating
/// window restored from the saved file. <c>WorkbenchAdapter.MapFloatingWindow</c> names a natively
/// floated pane <c>float-view-N</c> from a static counter (<c>WorkbenchAdapter._nodeSeq</c>) that
/// starts at 0 in every process and is shared with the split and stack ids. The zone file keeps a
/// floating stack's id, so a restored <c>float-view-N</c> and a later native float in the new process
/// can share an id. The tree projection (<c>Layout.AssertInvariant</c>) then throws "duplicate node id".
/// </summary>
/// <remarks>
/// The counter's value at the native float depends on how many reconciles the new process has run.
/// The test sets it, by reflection, to the value at which the next native float would mint the
/// restored id. That is the restart condition, made deterministic.
/// </remarks>
public sealed class FloatViewIdAfterRestartTests : IDisposable
{
    private const string RestoredId = "float-view-1000";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "aide-float-view-id-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void ANativeFloatAfterARestart_NeverMintsTheIdOfARestoredFloatingWindow() =>
        Sta.Pump(
            create: () => new DockingManager(),
            configure: window => { window.Width = 900; window.Height = 600; },
            body: async (main, manager) =>
            {
                static TextBox Build(Surface surface) => new() { Text = surface.SurfaceId };

                // The earlier process: "sessions" was floated natively and saved under a float-view id.
                var earlier = new ZoneBackedLayoutService().Zones;
                var sessions = earlier.AllSurfaces().Single(s => s.SurfaceId == "sessions");
                var saved = ZoneLayoutService.ClosePane(earlier, "sessions").Layout with
                {
                    Floating = [new StackNode(RestoredId, [sessions], 0, StackState.Floating,
                        floatingBounds: new LayoutRect(100, 100, 400, 300))],
                };
                var path = Path.Combine(_dir, "zones.json");
                new ZoneLayoutStore(path).Save(saved);

                // The restart: read the file and restore it into a new service.
                var ids = saved.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
                var read = new ZoneLayoutStore(path).Read(ids, new HashSet<string>(StringComparer.Ordinal));
                Assert.Equal(ZoneLoadRefusal.None, read.Refusal);
                var service = new ZoneBackedLayoutService();
                service.RestoreZones(read.Layout!);
                Assert.Equal(RestoredId, Assert.Single(service.Zones.Floating).Id);

                var adapter = new WorkbenchAdapter(manager, service, Build);
                adapter.Render();
                await Task.Delay(300);

                // The user floats a second pane natively, from a zone that keeps a pane in the view.
                var nativeId = service.Zones.Zones.Values
                    .Select(z => z.Surfaces().ToList())
                    .First(surfaces => surfaces.Count >= 2)[^1].SurfaceId;
                manager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == nativeId).Float();
                await Task.Delay(500);
                Assert.Equal(2, manager.Layout.FloatingWindows.Count);

                // Count the mints one read makes up to the native float, then set the counter so that
                // the next read's native float would mint the restored id.
                var seq = typeof(WorkbenchAdapter).GetField("_nodeSeq", BindingFlags.NonPublic | BindingFlags.Static)!;
                seq.SetValue(null, 0);
                var probe = adapter.ReadLayoutFromView();
                Assert.NotNull(probe);
                var probeId = probe!.Floating.Single(s => s.Surfaces.Any(x => x.SurfaceId == nativeId)).Id;
                var mints = int.Parse(probeId["float-view-".Length..], System.Globalization.CultureInfo.InvariantCulture);
                seq.SetValue(null, 1000 - mints);

                var view = adapter.ReadLayoutFromView();

                Assert.NotNull(view);
                var floatIds = view!.Floating.Select(s => s.Id).ToList();
                Assert.Equal(2, floatIds.Count);
                Assert.True(floatIds.Distinct(StringComparer.Ordinal).Count() == floatIds.Count,
                    $"the native float minted an id a restored window holds: {string.Join(", ", floatIds)}");
                Assert.True(service.ReconcileFromView(view), "the service refused the view's floating windows");
                var thrown = Record.Exception(() => _ = service.Current);
                Assert.Null(thrown);
                Assert.Contains(service.Current.Floating, s => s.Id == RestoredId);
            });
}
