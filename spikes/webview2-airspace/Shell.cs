using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Layout;
using Microsoft.Web.WebView2.Wpf;

namespace Webview2AirspaceSpike;

/// <summary>
/// The window under test: an AvalonDock layout with a WebView2 in one pane, exactly as ADR-0008 and
/// ADR-0012 compose together in the shipped shell.
/// </summary>
/// <remarks>
/// A plain WPF window hosting a WebView2 would answer an easier question than the one that matters.
/// The pane must be one AvalonDock can float, tab and resize, because that is the composition the
/// product actually ships and the interaction ADR-0008 flagged as its reversal trigger.
/// </remarks>
internal sealed class Shell : Window
{
    /// <summary>Which WebView2 hosting mode this window is testing.</summary>
    internal enum Hosting
    {
        /// <summary>The default `WebView2` control: a child HWND, composited by Windows.</summary>
        Windowed,

        /// <summary>`WebView2CompositionControl`: rendered into the WPF visual tree.</summary>
        Composition,
    }

    private readonly DockingManager _dock = new();
    private readonly LayoutDocumentPane _documentPane = new();
    private readonly LayoutAnchorablePane _sidePane = new();
    private readonly LayoutDocument _webDocument = new() { Title = "Graph", CanClose = false };
    private readonly LayoutDocument _otherDocument = new() { Title = "Evidence", CanClose = false };
    private readonly LayoutAnchorable _sideAnchorable = new() { Title = "Explore", CanClose = false };
    private readonly Border _overlay;
    private readonly Grid _webCell;
    private readonly dynamic _web;

    internal Shell(Color web, Color overlay, Color pane, Hosting hosting)
    {
        Mode = hosting;
        Title = $"S4 — WebView2 airspace ({hosting})";
        Width = 1100;
        Height = 700;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(pane);

        // Both controls derive from WebView2Base and share the API used here, so the SAME probes
        // run against both hosting modes. Comparing two runs of one harness is the measurement;
        // running a different harness per mode would compare the harnesses.
        // `WebView2` and `WebView2CompositionControl` expose identical members but share only an
        // INTERNAL base class, so there is no public type that names both. A dynamic handle is the
        // smallest correct way to drive one harness against both; the alternative is two wrapper
        // types to paper over an accessibility modifier.
        WebHost = hosting == Hosting.Composition
            ? new WebView2CompositionControl { Name = "GraphCanvas" }
            : new WebView2 { Name = "GraphCanvas" };
        _web = WebHost;
        TextBox = new TextBox { Name = "Probe", Height = 28, Margin = new Thickness(8) };

        // The overlay sits in the SAME cell as the WebView2 and later in z-order, so in ordinary WPF
        // it must paint on top. Whether it actually does is the airspace question.
        // Anchored top-left rather than centred, and deliberately smaller than the pane. Both matter:
        // an overlay centred in the same cell as the WebView2 has the SAME centre point, so "sample
        // the overlay" and "sample the web area" resolve to one pixel and the comparison measures
        // nothing. The first run of this spike did exactly that (DC-009).
        _overlay = new Border
        {
            Background = new SolidColorBrush(overlay),
            Width = 200,
            Height = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(24),
            Visibility = Visibility.Collapsed,
            Child = new TextBlock
            {
                Text = "WPF overlay",
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };

        _webCell = new Grid { Background = new SolidColorBrush(pane) };
        _webCell.Children.Add(WebHost);
        _webCell.Children.Add(_overlay);

        _webDocument.Content = _webCell;
        _otherDocument.Content = new Grid
        {
            Background = new SolidColorBrush(pane),
            Children = { new TextBlock { Text = "A second tab, so the web pane can be hidden." } },
        };

        var sideStack = new StackPanel { Background = new SolidColorBrush(pane) };
        sideStack.Children.Add(TextBox);
        _sideAnchorable.Content = sideStack;

        _documentPane.Children.Add(_webDocument);
        _documentPane.Children.Add(_otherDocument);
        _sidePane.Children.Add(_sideAnchorable);

        var group = new LayoutPanel { Orientation = Orientation.Horizontal };
        group.Children.Add(new LayoutAnchorablePaneGroup(_sidePane) { DockWidth = new GridLength(260) });
        group.Children.Add(new LayoutDocumentPaneGroup(_documentPane));

        _dock.Layout = new LayoutRoot { RootPanel = group };
        Content = _dock;
    }

    internal FrameworkElement WebHost { get; }

    /// <summary>True while a browser process is attached to the control.</summary>
    internal bool CoreWebView2Alive => _web.CoreWebView2 is not null;

    internal Hosting Mode { get; }

    internal TextBox TextBox { get; }

    internal Border Overlay => _overlay;

    /// <summary>Initialises the browser and loads a page that is a single flat colour.</summary>
    internal async Task WaitForWebViewAsync()
    {
        await _web.EnsureCoreWebView2Async();

        var loaded = new TaskCompletionSource();
        EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> onCompleted =
            (_, _) => loaded.TrySetResult();

        _web.NavigationCompleted += onCompleted;

        // A flat colour rather than a real page: the measurement is "which surface owns this pixel",
        // and any layout, font or scrollbar would only add ways for the sample to be ambiguous.
        _web.NavigateToString(
            "<html><body style=\"margin:0;background:#E11D48;\"></body></html>");

        await Task.WhenAny(loaded.Task, Task.Delay(TimeSpan.FromSeconds(20)));
        _web.NavigationCompleted -= onCompleted;
        await SettleAsync();
    }

    /// <summary>Lets WPF lay out, render, and the compositor present, before anything is sampled.</summary>
    /// <remarks>
    /// Sampling a pixel before the frame that contains it has been presented is the DC-009 shape:
    /// a plausible colour that describes the previous frame. Two render-priority yields plus a real
    /// delay is empirical rather than elegant, but it is what makes the capture reproducible.
    /// </remarks>
    internal async Task SettleAsync()
    {
        for (var i = 0; i < 2; i++)
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
        }

        await Task.Delay(320);
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
    }

    internal void ShowOverlay(bool visible) =>
        _overlay.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    internal void FloatWebPane() => _webDocument.Float();

    internal void DockWebPane() => _webDocument.Dock();

    internal void SelectOtherTab() => _otherDocument.IsSelected = true;

    internal void SelectWebTab() => _webDocument.IsSelected = true;

    internal void ResizeWebPane(double factor)
    {
        if (_dock.Layout.RootPanel.Children.FirstOrDefault() is LayoutAnchorablePaneGroup side)
        {
            side.DockWidth = new GridLength(Math.Max(80, side.DockWidth.Value * (2 - factor)));
        }
    }

    /// <summary>The centre of an element, in the device pixels both captures are indexed by.</summary>
    internal (int X, int Y) DevicePointIn(FrameworkElement element)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var centre = element.TranslatePoint(
            new Point(element.ActualWidth / 2, element.ActualHeight / 2), this);
        return ((int)(centre.X * dpi.DpiScaleX), (int)(centre.Y * dpi.DpiScaleY));
    }

    /// <summary>
    /// A point well inside the web pane and provably clear of the overlay — the bottom-right
    /// quadrant, where only the WebView2 can be.
    /// </summary>
    internal (int X, int Y) WebOnlyPoint()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var point = WebHost.TranslatePoint(
            new Point(WebHost.ActualWidth * 0.75, WebHost.ActualHeight * 0.75), this);
        return ((int)(point.X * dpi.DpiScaleX), (int)(point.Y * dpi.DpiScaleY));
    }
}
