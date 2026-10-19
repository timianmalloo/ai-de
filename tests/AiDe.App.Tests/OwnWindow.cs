using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace AiDe.App.Tests;

/// <summary>
/// <b>Window queries scoped to the test's own window (DC-NEW-wl-t30-prefill).</b> WPF's list of open windows
/// (<see cref="PresentationSource.CurrentSources"/>) is process-wide: in the whole suite it holds every other test
/// thread's windows, whose roots throw on this thread, and other windows on this thread. A test that searches it by name
/// passes alone and reads another window's control, or throws, in the suite. Search through here instead.
/// </summary>
public static class OwnWindow
{
    /// <summary>
    /// The roots of the popups open over <paramref name="window"/>: this thread's sources only, and of those only the
    /// popups whose placement target is inside <paramref name="window"/>.
    /// </summary>
    public static IReadOnlyList<FrameworkElement> Popups(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return PresentationSource.CurrentSources.OfType<HwndSource>()
            .Where(source => source.CheckAccess() && !source.IsDisposed)
            .Select(source => source.RootVisual)
            .OfType<FrameworkElement>()
            .Where(root => PopupOf(root) is { PlacementTarget: { } target } && ReferenceEquals(Window.GetWindow(target), window))
            .ToList();
    }

    /// <summary>The popup a popup window's root belongs to: the logical parent of the first visual under it that has one.</summary>
    private static Popup? PopupOf(DependencyObject root)
    {
        var pending = new Queue<DependencyObject>([root]);
        while (pending.Count > 0)
        {
            var node = pending.Dequeue();
            if (LogicalTreeHelper.GetParent(node) is Popup popup)
            {
                return popup;
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                pending.Enqueue(VisualTreeHelper.GetChild(node, i));
            }
        }

        return null;
    }
}
