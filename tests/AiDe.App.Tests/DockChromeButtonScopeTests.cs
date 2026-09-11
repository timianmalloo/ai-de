using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench;
using AvalonDock;
using AvalonDock.Layout;
using Xunit.Abstractions;

namespace AiDe.App.Tests;

/// <summary>
/// The app's implicit <c>Button</c> default reaches a docked pane's content, and nothing narrower
/// is allowed to intercept it.
/// </summary>
/// <remarks>
/// <para><b>This test exists because the first attempt was wrong, and only a measurement said so.</b>
/// App.xaml now carries an implicit <c>Button</c> default so a control added tomorrow is themed
/// without anyone remembering to theme it (TC1). The concern was AvalonDock: its chrome is icon
/// buttons sitting on a ground the VS2013 dark theme already painted, and the app's raised pill
/// behind each of them would read as a grey rectangle. The fix drafted for that was an implicit
/// <c>Button</c> style merged into <c>DockingManager.Resources</c>, on the reasoning that the
/// manager's dictionary is nearer in the element tree and would therefore win <i>for the dock's own
/// chrome</i>.</para>
///
/// <para><b>The reasoning was right and the conclusion was wrong.</b> The manager's dictionary is
/// nearer in the tree for EVERYTHING inside the docking host — and every surface in this product is
/// a document inside that manager, so "the dock's own chrome" is not a scope that dictionary can
/// express. Measured: a <c>Button</c> placed in a pane resolved to <c>#00FFFFFF</c> instead of the
/// palette's raised ground. Send, Attach file, Sign in and every control in every surface would have
/// lost their chrome to a change whose stated purpose was to protect a close icon. The exemption was
/// withdrawn; the app's default is the only implicit Button style in the tree.</para>
///
/// <para><b>What is left unresolved, and honestly.</b> Whether AvalonDock's own chrome buttons now
/// pick up the raised pill is reported by this test rather than asserted, because the answer depends
/// on which of them the theme styles explicitly and that is the theme's business, not ours. The
/// close button visible on a document tab is styled inline by <c>DockRoundedTabs.xaml</c>'s template
/// and is unaffected either way. If a grey pill ever does appear behind a piece of dock chrome, the
/// fix is a style scoped to that AvalonDock control type — not to the manager.</para>
/// </remarks>
public sealed class DockChromeButtonScopeTests(ITestOutputHelper output)
{
    [Fact]
    public void AButtonInsideAPaneKeepsTheAppsChrome()
    {
        Sta.Run(() =>
        {
            Window? window = null;

            try
            {
                var theme = ThemeProbe.AppTheme();

                var manager = new DockingManager { Theme = new AvalonDock.Themes.Vs2013DarkTheme() };
                DockThemeAccents.Retokenise(manager);
                DockRoundedTabs.Apply(manager);

                var inPane = new Button { Content = "Send" };
                var document = new LayoutDocument { Title = "Session", ContentId = "d1", Content = inPane };
                manager.Layout = new LayoutRoot { RootPanel = new LayoutPanel(new LayoutDocumentPane(document)) };

                window = new Window
                {
                    Resources = theme,
                    Content = manager,
                    Width = 700,
                    Height = 460,
                    Left = -10000,
                    Top = -10000,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                };

                window.Show();
                window.UpdateLayout();

                var raised = ThemeProbe.Token(theme, "SurfaceRaisedBrush");
                var ground = (inPane.Background as SolidColorBrush)?.Color;

                Assert.True(ground == raised,
                    "a Button in a docked pane resolved to "
                    + (ground?.ToString() ?? "a non-solid brush")
                    + $" instead of the app's {raised}. Something between the button and the "
                    + "application dictionary is answering the implicit Button lookup first — which "
                    + "is exactly what a style merged into DockingManager.Resources does to every "
                    + "control in every surface, not just to the dock's own chrome.");

                // Reported, not asserted: what the dock's own chrome buttons resolved to. The pane's
                // own button is excluded by identity.
                foreach (var chrome in Descendants<Button>(manager).Where(b => !ReferenceEquals(b, inPane)))
                {
                    output.WriteLine(
                        $"dock chrome button: {(chrome.Background as SolidColorBrush)?.Color.ToString() ?? "non-solid"}"
                        + $" · explicit style: {chrome.Style is not null}"
                        + $" · name: {chrome.Name}");
                }
            }
            finally
            {
                window?.Close();
            }
        });
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is T match)
            {
                yield return match;
            }

            foreach (var found in Descendants<T>(child))
            {
                yield return found;
            }
        }
    }
}
