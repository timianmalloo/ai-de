using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.App.Workbench;
using AiDe.Core.Projections;

namespace AiDe.App.Tests;

public sealed class EntryPointsSurfaceTests
{
    [Fact]
    public void FactoryRow_IsArchitectureOne_DerivedView()
    {
        var row = Assert.Single(SurfaceContentFactory.Kinds, k => k.Kind == "entry-points");
        Assert.Equal("Entry-points", row.Title);
        Assert.Same(AiDe.Core.Workbench.PerspectiveSet.Architecture, Assert.Single(row.Perspectives));
        Assert.Equal(SurfaceContentFactory.Instances.One, row.Instances);
        var entry = Assert.IsType<SurfaceContentFactory.SurfaceEntry.Derived>(row.Entry);
        Assert.Equal("_View", entry.Menu);
        Assert.Null(row.Zone);
    }

    [Fact]
    public void Show_UnclassifiedRows_SequenceDisabled()
    {
        OnSta(() =>
        {
            var surface = new EntryPointsSurface();
            var window = new Window
            {
                Content = surface,
                Width = 480,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };
            window.Show();
            try
            {
                surface.Show(new EntryPointsResult(
                    [new EntryPointRow(EntryPointKind.Api, "Orders.OrdersController", "Orders.OrdersController", null)],
                    0,
                    [],
                    "rev-1"));
                Assert.False(surface.OpenSequenceEnabled);
                Assert.Contains("mapping-unavailable", AutomationProperties.GetName(
                    FindSequence(surface)), StringComparison.Ordinal);
                Assert.Contains("Orders.OrdersController", VisibleText(surface), StringComparison.Ordinal);

                surface.ListSelectFirst();
                EntryPointsActivate? seen = null;
                surface.ActivateRequested += (_, a) => seen = a;
                surface.HandleKey(Key.Return, ModifierKeys.None);
                Assert.NotNull(seen);
                Assert.Equal("Orders.OrdersController", seen!.NodeId);
                Assert.Equal(NodeViewKind.GraphNeighbourhood, seen.Kind);
                surface.HandleKey(Key.Return, ModifierKeys.Control);
                Assert.Equal(NodeViewKind.Source, seen.Kind);
                Assert.False(surface.OpenSequenceEnabled);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static void OnSta(Action body) => Sta.Run(body, 60);

    private static Button FindSequence(DependencyObject root)
    {
        if (root is Button b && (b.Content as string) == "Open Sequence")
        {
            return b;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            var found = FindOptional(child);
            if (found is not null)
            {
                return found;
            }
        }

        Assert.Fail("Open Sequence button missing");
        return null!;

        static Button? FindOptional(DependencyObject d)
        {
            if (d is Button btn && (btn.Content as string) == "Open Sequence")
            {
                return btn;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(d).OfType<DependencyObject>())
            {
                var nested = FindOptional(child);
                if (nested is not null)
                {
                    return nested;
                }
            }

            return null;
        }
    }

    private static string VisibleText(DependencyObject root)
    {
        var parts = new List<string>();
        Walk(root);
        return string.Join(" ", parts);

        void Walk(DependencyObject d)
        {
            switch (d)
            {
                case TextBlock tb:
                    parts.Add(tb.Text);
                    break;
                case ListBoxItem item:
                    parts.Add(item.Content?.ToString() ?? "");
                    break;
                case ListBox list:
                    foreach (var o in list.Items)
                    {
                        parts.Add(o?.ToString() ?? "");
                    }

                    break;
            }

            foreach (var child in LogicalTreeHelper.GetChildren(d).OfType<DependencyObject>())
            {
                Walk(child);
            }
        }
    }
}
