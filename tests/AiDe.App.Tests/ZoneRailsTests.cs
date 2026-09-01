using System.Windows;
using System.Windows.Controls;
using AiDe.App.Workbench;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests;

/// <summary>
/// Collapse-to-rail (ADR-0021): a collapsed tool zone shows an edge rail whose one click expands it.
/// Rails are WPF elements, so each test runs on its own STA thread.
/// </summary>
public sealed class ZoneRailsTests
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "STA thread did not finish");
        if (failure is not null) { throw new InvalidOperationException("STA work failed", failure); }
        return result;
    }

    [Fact]
    public void ARail_AppearsWhenItsZoneCollapses_AndHidesWhenItExpands()
    {
        OnStaThread(() =>
        {
            var layout = WorkbenchLayout.Default();
            var rails = new ZoneRails(new Border(), () => layout, _ => { });

            Assert.False(rails.RailVisible(ZoneId.Left)); // default: Left is expanded, no rail

            layout = ZoneLayoutService.CollapseZone(layout, ZoneId.Left).Layout;
            rails.Refresh();
            Assert.True(rails.RailVisible(ZoneId.Left));  // collapsed → rail shows

            layout = ZoneLayoutService.ExpandZone(layout, ZoneId.Left).Layout;
            rails.Refresh();
            Assert.False(rails.RailVisible(ZoneId.Left)); // expanded → rail hides
            return true;
        });
    }

    [Fact]
    public void ClickingARail_InvokesTheExpandCallback_ForThatZone()
    {
        OnStaThread(() =>
        {
            var layout = ZoneLayoutService.CollapseZone(WorkbenchLayout.Default(), ZoneId.Bottom).Layout;
            ZoneId? expanded = null;
            var rails = new ZoneRails(new Border(), () => layout, z => expanded = z);

            // Find the Bottom rail's button and click it.
            var button = FindRailButton(rails.Root, "Bottom");
            Assert.NotNull(button);
            button!.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            Assert.Equal(ZoneId.Bottom, expanded);
            return true;
        });
    }

    private static Button? FindRailButton(DependencyObject root, string label)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is Button b && b.Content is string s && s.Contains(label, StringComparison.Ordinal))
            {
                return b;
            }

            var found = FindRailButton(child, label);
            if (found is not null) { return found; }
        }

        return null;
    }
}
