using System.Windows;
using AvalonDock;

namespace AiDe.App.Workbench;

/// <summary>
/// Rounds the AvalonDock document tab tops to the facelift's soft feel.
/// </summary>
/// <remarks>
/// <para>The rounded style in <c>DockRoundedTabs.xaml</c> is the VS2013 theme's own
/// <c>LayoutDocumentTabItem</c> template (extracted from the assembly, so the drag, selection and
/// close-command bindings are the theme's real ones, not a guess) with three changes: the header
/// gets <c>CornerRadius="7,7,0,0"</c>, its bottom border line is dropped, and the two serialization
/// artifacts <c>XamlWriter</c> leaves behind — a null <c>Content</c> and a black foreground on the
/// title — are removed so the title shows in the palette's text colour.</para>
///
/// <para>The workbench docks every surface in a <c>LayoutDocumentPane</c>, so all its tabs are
/// <c>LayoutDocumentTabItem</c> (a <see cref="System.Windows.Controls.ContentControl"/>), which is
/// why a plain <c>ContentPresenter</c> shows the title safely. Merged AFTER the theme so the
/// implicit style wins; the accent/surface retokenisation (<see cref="DockThemeAccents"/>) still
/// supplies the tab background brushes this template binds to.</para>
/// </remarks>
public static class DockRoundedTabs
{
    public static void Apply(DockingManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        manager.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/AiDe.App;component/Workbench/DockRoundedTabs.xaml", UriKind.Relative),
        });
    }
}
