using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Layout;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Webview2SnapshotSwapSpike;

/// <summary>
/// A canvas pane that can hand its pixels to WPF for the duration of a drag, then take them back.
/// </summary>
/// <remarks>
/// This is the mechanism the S4 decision rests on. The windowed WebView2 cannot be drawn over, so
/// while WPF needs to draw over it — an AvalonDock drop indicator, the command palette — the live
/// control is hidden and a still image of its last frame stands in. WPF composites the image
/// normally, because it is an ordinary <c>Image</c> in the visual tree.
///
/// The gut check is not "does it work" but "does the seam show": whether the still frame lands
/// pixel-aligned with what it replaced, at a non-integer DPI scale where a capture in device pixels
/// meets a layout in DIPs.
/// </remarks>
internal sealed class Shell : Window
{
    private readonly DockingManager _dock = new();
    private readonly LayoutDocumentPane _documentPane = new();
    private readonly LayoutDocument _webDocument = new() { Title = "Graph", CanClose = false };
    private readonly Grid _cell;
    private readonly Image _snapshot;
    private readonly Border _overlay;

    internal Shell(Color overlayColour, Color paneColour)
    {
        Title = "Snapshot-swap gut check";
        Width = 1100;
        Height = 700;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(paneColour);

        WebHost = new WebView2 { Name = "GraphCanvas" };

        _snapshot = new Image
        {
            Visibility = Visibility.Collapsed,
            Stretch = Stretch.Fill,
            // The capture is in device pixels and the Image is laid out in DIPs. At 150% those are
            // not the same number, and letting WPF resample is exactly how a still frame ends up
            // visibly softer than the live content it replaced.
            SnapsToDevicePixels = true,
        };
        RenderOptions.SetBitmapScalingMode(_snapshot, BitmapScalingMode.NearestNeighbor);

        _overlay = new Border
        {
            Background = new SolidColorBrush(overlayColour),
            Width = 200,
            Height = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(24),
            Visibility = Visibility.Collapsed,
        };

        _cell = new Grid { Background = new SolidColorBrush(paneColour) };
        _cell.Children.Add(WebHost);
        _cell.Children.Add(_snapshot);
        _cell.Children.Add(_overlay);

        _webDocument.Content = _cell;
        _documentPane.Children.Add(_webDocument);

        var panel = new LayoutPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new LayoutDocumentPaneGroup(_documentPane));
        _dock.Layout = new LayoutRoot { RootPanel = panel };
        Content = _dock;
    }

    internal WebView2 WebHost { get; }

    internal bool ShowingSnapshot => _snapshot.Visibility == Visibility.Visible;

    /// <summary>Milliseconds spent capturing on the last swap.</summary>
    internal double LastCaptureMs { get; private set; }

    /// <summary>Milliseconds from swap request to the still frame being on screen.</summary>
    internal double LastSwapMs { get; private set; }

    internal async Task WaitForWebViewAsync()
    {
        await WebHost.EnsureCoreWebView2Async();

        var loaded = new TaskCompletionSource();
        EventHandler<CoreWebView2NavigationCompletedEventArgs> onCompleted = (_, _) => loaded.TrySetResult();
        WebHost.NavigationCompleted += onCompleted;

        // Four hard-edged quadrants rather than a flat colour. A flat page would make ANY snapshot
        // look correct, including one that is offset or scaled — the alignment error this spike
        // exists to catch would be invisible. Sampling either side of the quadrant seams turns
        // sub-pixel misplacement into a colour flip.
        WebHost.NavigateToString("""
            <html><body style="margin:0;padding:0;overflow:hidden">
            <div style="position:absolute;left:0;top:0;width:50%;height:50%;background:#E11D48"></div>
            <div style="position:absolute;left:50%;top:0;width:50%;height:50%;background:#2563EB"></div>
            <div style="position:absolute;left:0;top:50%;width:50%;height:50%;background:#F59E0B"></div>
            <div style="position:absolute;left:50%;top:50%;width:50%;height:50%;background:#7C3AED"></div>
            </body></html>
            """);

        await Task.WhenAny(loaded.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        WebHost.NavigationCompleted -= onCompleted;
        await SettleAsync();
    }

    /// <summary>Captures the live frame, shows it, and hides the control — the drag-start path.</summary>
    internal async Task<bool> SwapToSnapshotAsync()
    {
        var total = Stopwatch.StartNew();

        var capture = Stopwatch.StartNew();
        BitmapImage image;
        try
        {
            using var stream = new MemoryStream();
            await WebHost.CoreWebView2.CapturePreviewAsync(
                CoreWebView2CapturePreviewImageFormat.Png, stream);
            capture.Stop();

            stream.Position = 0;
            image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    capture threw {ex.GetType().Name}: {ex.Message.Trim()}");
            return false;
        }

        LastCaptureMs = capture.Elapsed.TotalMilliseconds;
        CaptureSize = new Size(image.PixelWidth, image.PixelHeight);

        _snapshot.Source = image;
        _snapshot.Visibility = Visibility.Visible;

        // Hidden, not Collapsed. Collapsing removes the control from layout, which resizes it to
        // nothing and forces the browser to re-lay-out on the way back — paying a cost on restore
        // for no benefit while hidden.
        WebHost.Visibility = Visibility.Hidden;

        await SettleAsync();
        total.Stop();
        LastSwapMs = total.Elapsed.TotalMilliseconds;
        return true;
    }

    /// <summary>Gives the pixels back to the live control — the drag-end path.</summary>
    internal async Task RestoreAsync()
    {
        WebHost.Visibility = Visibility.Visible;
        _snapshot.Visibility = Visibility.Collapsed;
        _snapshot.Source = null;
        await SettleAsync();
    }

    /// <summary>The pixel size of the last capture, for comparison against the pane's device size.</summary>
    internal Size CaptureSize { get; private set; }

    internal void ShowOverlay(bool visible) =>
        _overlay.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    internal async Task SettleAsync()
    {
        for (var i = 0; i < 2; i++)
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        }

        await Task.Delay(300);
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
    }

    /// <summary>A point inside the canvas at a fraction of its extent, in device pixels.</summary>
    internal (int X, int Y) CanvasPoint(double fractionX, double fractionY)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var point = _cell.TranslatePoint(
            new Point(_cell.ActualWidth * fractionX, _cell.ActualHeight * fractionY), this);
        return ((int)(point.X * dpi.DpiScaleX), (int)(point.Y * dpi.DpiScaleY));
    }

    internal (int X, int Y) OverlayPoint()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var centre = _overlay.TranslatePoint(
            new Point(_overlay.ActualWidth / 2, _overlay.ActualHeight / 2), this);
        return ((int)(centre.X * dpi.DpiScaleX), (int)(centre.Y * dpi.DpiScaleY));
    }

    /// <summary>The canvas size in device pixels, to compare against the capture's own size.</summary>
    internal Size CanvasDeviceSize()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        return new Size(_cell.ActualWidth * dpi.DpiScaleX, _cell.ActualHeight * dpi.DpiScaleY);
    }
}
