using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AiDe.App.Workbench;

/// <summary>
/// Wraps a pane's content in a soft "island" card — rounded, bordered, inset with a gap.
/// </summary>
/// <remarks>
/// <para><b>Radius + border, never shadow.</b> The facelift softens panes with a corner radius and
/// a one-pixel border, not a drop shadow — a <c>DropShadowEffect</c> over a windowed child
/// (WebView2 canvas, terminal <c>HwndHost</c>) is the airspace trap the wpf-styling-expert holds a
/// veto on (App.xaml notes this). So this frame is effect-free and therefore safe over every
/// surface, composited or windowed.</para>
///
/// <para><b>How it reads as raised.</b> The card is <c>SurfaceRaised</c> (#1A1F26); the docking
/// chrome behind the gap is retokenised to <c>surface</c>/<c>sunken</c> (darker) by
/// <see cref="DockThemeAccents"/>, so the lighter card sits proud of the darker gap.</para>
///
/// <para><b>Windowed children.</b> A rounded <see cref="Border"/> does not clip a child HWND to its
/// corners (airspace), so a small inset keeps the square-cornered WebView2/terminal off the rounded
/// edge rather than poking through it — the frame still softens the pane.</para>
/// </remarks>
public static class SurfaceChrome
{
    /// <summary>Wraps <paramref name="content"/> in an island card. Returns the wrapping border.</summary>
    public static FrameworkElement WrapAsIsland(FrameworkElement content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var card = new Border
        {
            CornerRadius = new CornerRadius(10),          // RadiusLg
            Margin = new Thickness(5),                    // the gap that separates islands
            Padding = new Thickness(1),                   // keeps a windowed child off the rounded edge
            BorderThickness = new Thickness(1),
            SnapsToDevicePixels = true,
            Child = content,
        };
        card.SetResourceReference(Border.BackgroundProperty, "SurfaceRaisedBrush");
        card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        return card;
    }
}
