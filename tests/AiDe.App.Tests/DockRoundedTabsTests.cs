using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench;
using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Layout;

namespace AiDe.App.Tests;

/// <summary>
/// The document tabs are rounded to the facelift, and still show their title — proven, not assumed.
/// </summary>
/// <remarks>
/// Guards the tab retemplate: the rounded style is the theme's own <c>LayoutDocumentTabItem</c>
/// template with a rounded header and the <c>XamlWriter</c> serialization artifacts (null content,
/// black title) removed. This drives a real bound document through a real window and asserts both
/// that the header is actually rounded AND that the title still renders — the two things a blind
/// retemplate breaks (invisible title; square corners).
/// </remarks>
public sealed class DockRoundedTabsTests
{
    [Fact]
    public void TheDocumentTab_IsRounded_AndStillShowsItsTitle()
    {
        // The shared harness (Sta.Run), not an inlined thread. This one joined with NO
        // timeout, so a hang here hung the whole suite with nothing to read; the shared
        // form bounds it and rethrows an assertion failure as itself (DC-078/DC-079).
        Sta.Run(() =>
        {
            Window? window = null;
            try
            {
                var manager = new DockingManager();
                manager.Theme = new AvalonDock.Themes.Vs2013DarkTheme();
                DockThemeAccents.Retokenise(manager);
                DockRoundedTabs.Apply(manager);

                var doc = new LayoutDocument { Title = "RoundedTabTitle", ContentId = "d1" };
                var pane = new LayoutDocumentPane(doc);
                manager.Layout = new LayoutRoot { RootPanel = new LayoutPanel(pane) };

                window = new Window { Content = manager, Width = 600, Height = 400, Left = -2000, Top = -2000 };
                window.Show();
                window.UpdateLayout();

                var tab = FindDescendants<LayoutDocumentTabItem>(manager).FirstOrDefault();
                Assert.NotNull(tab);

                // Rounded: the templated "Header" border has our top corner radius.
                var header = tab!.Template.FindName("Header", tab) as Border;
                Assert.NotNull(header);
                Assert.Equal(7d, header!.CornerRadius.TopLeft);
                Assert.Equal(7d, header.CornerRadius.TopRight);

                // Title still renders (the artifact this fixes is a tab with no title).
                var titles = FindDescendants<TextBlock>(tab)
                    .Select(t => t.Text)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
                Assert.Contains(titles, s => s.Contains("RoundedTabTitle", StringComparison.Ordinal));
            }
            finally { window?.Close(); }
        });
    }

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var d in FindDescendants<T>(child)) yield return d;
        }
    }
}
