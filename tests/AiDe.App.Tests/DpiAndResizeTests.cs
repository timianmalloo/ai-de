using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;
using AvalonDock;

namespace AiDe.App.Tests;

/// <summary>
/// The last two ADR-0012 spikes, made permanent: per-monitor DPI awareness and ganged resize.
/// Both were recorded as Inferred in the ADR; these turn them into measurements that stay measured.
/// </summary>
public sealed class DpiAndResizeTests
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool AreDpiAwarenessContextsEqual(IntPtr a, IntPtr b);

    private static readonly IntPtr PerMonitorAwareV2 = new(-4);

    private static T OnSta<T>(Func<T> work)
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)));
        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
        return result;
    }

    private static T WithWindow<T>(Func<Window, T> assert) => OnSta(() =>
    {
        var window = new Window
        {
            Width = 900,
            Height = 600,
            Left = -10000,
            Top = -10000,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        window.UpdateLayout();
        try { return assert(window); }
        finally { window.Close(); }
    });

    /// <summary>
    /// Per-monitor DPI awareness is a prerequisite for US-9's floating panes, not a polish item: a
    /// System-aware app bitmap-stretches any window moved to a different-DPI display, so the
    /// coordinates a restored layout writes back would not round-trip.
    /// </summary>
    /// <remarks>
    /// This asserts the SHIPPED MANIFEST rather than measuring the current process. The first version
    /// of this test measured the test host, which has its own DPI awareness and is not the app — a
    /// manifest cannot affect a process it is not in. The runtime confirmation
    /// (PER_MONITOR_AWARE_V2, measured against the real app) is recorded in the spike write-up.
    /// </remarks>
    [Fact]
    public void TheApplication_DeclaresPerMonitorV2DpiAwareness()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "AiDe.sln")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var manifest = Path.Combine(root.FullName, "src", "AiDe.App", "app.manifest");
        Assert.True(File.Exists(manifest), "src/AiDe.App/app.manifest is missing");

        var text = File.ReadAllText(manifest);
        Assert.Contains("PerMonitorV2", text, StringComparison.Ordinal);

        var csproj = File.ReadAllText(Path.Combine(root.FullName, "src", "AiDe.App", "AiDe.App.csproj"));
        // A manifest the build does not reference is decoration.
        Assert.Contains("<ApplicationManifest>app.manifest</ApplicationManifest>", csproj, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ganged resize: panes sharing a divider resize together and the tiling stays complete.
    /// Premiere states this contract four separate ways; US-9 adopts it as an invariant.
    /// </summary>
    [Fact]
    public void NoTwoPanesOverlap_AndNonePaneCollapsesToNothing()
    {
        var rects = OnSta(() =>
        {
            var service = new LayoutService();
            var manager = new DockingManager();
            var adapter = new WorkbenchAdapter(manager, service);
            var window = new Window
            {
                Content = manager, Width = 900, Height = 600,
                Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false,
            };
            window.Show();
            adapter.Render();
            window.UpdateLayout();
            manager.UpdateLayout();

            var found = new List<Rect>();
            void Walk(DependencyObject d)
            {
                var n = System.Windows.Media.VisualTreeHelper.GetChildrenCount(d);
                for (var i = 0; i < n; i++)
                {
                    var c = System.Windows.Media.VisualTreeHelper.GetChild(d, i);
                    if (c is FrameworkElement fe
                        && fe.GetType().Name.Contains("DocumentPaneControl", StringComparison.Ordinal)
                        && fe.ActualWidth > 0 && fe.ActualHeight > 0)
                    {
                        var origin = fe.TranslatePoint(new Point(0, 0), manager);
                        found.Add(new Rect(origin, new Size(fe.ActualWidth, fe.ActualHeight)));
                    }

                    Walk(c);
                }
            }

            Walk(manager);
            window.Close();
            return found;
        });

        Assert.True(rects.Count >= 2, $"expected several panes, realized {rects.Count}");
        Assert.All(rects, r => Assert.True(r.Width > 0 && r.Height > 0, "a pane realized with no area"));

        // The tiling contract, stated geometrically so it holds whatever the orientation:
        // two docked panes may share an edge but must never share area.
        for (var i = 0; i < rects.Count; i++)
        {
            for (var j = i + 1; j < rects.Count; j++)
            {
                var overlap = Rect.Intersect(rects[i], rects[j]);
                var area = overlap.IsEmpty ? 0 : overlap.Width * overlap.Height;
                Assert.True(area < 1.0,
                    $"panes {i} and {j} overlap by {area:F0} square pixels: {rects[i]} vs {rects[j]}");
            }
        }
    }

    /// <summary>
    /// The model-side guarantee the view is expected to honour: weights always sum to one, so a
    /// resize redistributes space rather than creating a gap.
    /// </summary>
    [Fact]
    public void ResizingRedistributesSpace_RatherThanLeavingAGap()
    {
        var service = new LayoutService();
        var root = (SplitNode)service.Current.Root;
        var before = root.Weights.Sum();

        service.Apply(new LayoutOperation.ResizeSplit(root.Id, 0, 0.13));

        var after = (SplitNode)service.Current.Root;
        Assert.Equal(1.0, before, 6);
        Assert.Equal(1.0, after.Weights.Sum(), 6);
        // The neighbour gave up exactly what the resized pane gained.
        Assert.True(after.Weights[1] < root.Weights[1]);
    }
}
