using System.Windows;
using System.Windows.Media;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Composer;
using AvalonDock;
using AvalonDock.Layout;
using Microsoft.Web.WebView2.Wpf;
using Xunit.Abstractions;

namespace AiDe.App.Tests.Shell;

/// <summary>
/// <b>T21 step 0: the ADR-0015 risk, measured before float is made real.</b> ADR-0015 kept the
/// windowed WebView2 because the composition control kills the process when AvalonDock floats its
/// pane. This test floats the app's two real WebView2 hosts, the graph canvas and a notebook's
/// composer, into AvalonDock's own floating window, then docks them back. After each move it
/// checks that each browser still runs script.
/// </summary>
/// <remarks>
/// A native crash cannot be caught. If one happens, the test host dies and the run reports a
/// crashed host, not a failed assertion. That is the signal this probe exists to record.
/// </remarks>
public sealed class WebView2FloatProbeTests(ITestOutputHelper output)
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public void FloatingAndRedockingTheCanvasAndTheComposer_KeepsTheProcessAndBothBrowsersAlive() =>
        Sta.Pump(
            create: () => new DockingManager(),
            configure: window => { window.Width = 900; window.Height = 600; },
            timeoutSeconds: 120,
            body: async (window, manager) =>
            {
                var canvas = new CanvasSurface("canvas-probe", "Graph");
                var composer = new ComposerSurface("composer:probe", "probe — composer");
                var canvasDoc = new LayoutDocument { ContentId = "canvas-probe", Title = "Graph", Content = canvas };
                var composerDoc = new LayoutDocument { ContentId = "composer:probe", Title = "Composer", Content = composer };
                var pane = new LayoutDocumentPane();
                pane.Children.Add(canvasDoc);
                pane.Children.Add(composerDoc);
                var panel = new LayoutPanel();
                panel.Children.Add(pane);
                manager.Layout = new LayoutRoot { RootPanel = panel };

                try
                {
                    // Each document must be selected once so its browser is realized and starts.
                    canvasDoc.IsSelected = true;
                    Assert.True(await Until(() => canvas.Ready), "the canvas page never loaded");
                    composerDoc.IsSelected = true;
                    Assert.True(await Until(() => composer.PageIsReady), "the composer page never loaded");

                    await Check("canvas docked", canvas);
                    await Check("composer docked", composer);

                    canvasDoc.Float();
                    await Settle();
                    output.WriteLine($"floating windows after canvas float: {manager.FloatingWindows.Count()}");
                    Assert.Single(manager.FloatingWindows);
                    await Check("canvas floated", canvas);

                    composerDoc.Float();
                    await Settle();
                    output.WriteLine($"floating windows after composer float: {manager.FloatingWindows.Count()}");
                    Assert.Equal(2, manager.FloatingWindows.Count());
                    await Check("composer floated", composer);

                    // The product redocks through the model and a Render, which frees each content
                    // element and replaces Manager.Layout with a fresh tree. AvalonDock's own Dock()
                    // is not that path (and it leaves a document floating once its original pane has
                    // emptied), so the redock here is the Render's re-parent.
                    canvasDoc.Content = null;
                    composerDoc.Content = null;
                    var redocked = new LayoutDocumentPane();
                    redocked.Children.Add(new LayoutDocument { ContentId = "canvas-probe", Title = "Graph", Content = canvas });
                    redocked.Children.Add(new LayoutDocument { ContentId = "composer:probe", Title = "Composer", Content = composer, IsSelected = true });
                    var redockedPanel = new LayoutPanel();
                    redockedPanel.Children.Add(redocked);
                    manager.Layout = new LayoutRoot { RootPanel = redockedPanel };
                    await Settle();
                    output.WriteLine($"after redock by a fresh layout: floating windows {manager.FloatingWindows.Count()}");
                    Assert.Empty(manager.FloatingWindows);
                    await Check("composer redocked", composer);
                    redocked.SelectedContentIndex = 0;
                    await Settle();
                    await Check("canvas redocked", canvas);
                    output.WriteLine($"canvas initialisations: {canvas.InitialisationsStarted}");
                    output.WriteLine("process survived float and redock of both WebView2 hosts");
                }
                finally
                {
                    canvas.Dispose();
                    composer.Dispose();
                }
            });

    private async Task Check(string step, DependencyObject surface)
    {
        var view = FindView(surface);
        Assert.True(view?.CoreWebView2 is not null, $"{step}: no live CoreWebView2");
        var result = await view!.ExecuteScriptAsync("6*7");
        output.WriteLine($"{step}: CoreWebView2 alive, script 6*7 = {result}");
        Assert.Equal("42", result);
    }

    private static WebView2? FindView(DependencyObject root)
    {
        if (root is WebView2 view)
        {
            return view;
        }

        if (root is System.Windows.Controls.ContentControl { Content: DependencyObject content } && FindView(content) is { } inner)
        {
            return inner;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (FindView(VisualTreeHelper.GetChild(root, i)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static async Task<bool> Until(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + ReadyTimeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        return condition();
    }

    // Long enough for the floating window to show and the browser to re-parent and repaint.
    private static Task Settle() => Task.Delay(TimeSpan.FromSeconds(2));
}
