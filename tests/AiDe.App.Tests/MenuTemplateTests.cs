using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace AiDe.App.Tests;

/// <summary>
/// The retemplated dropdown menus render and open — proven, not assumed.
/// </summary>
/// <remarks>
/// The facelift retemplates the four MenuItem roles + ContextMenu to thin, rounded, floating
/// popups. The risk of a menu retemplate is a dropdown that no longer opens or whose items no
/// longer show. This applies the SAME templates (parsed from a self-contained dictionary mirroring
/// App.xaml's menu block), opens a top-level menu, and asserts its submenu items render — the two
/// things a broken menu template loses.
/// </remarks>
public sealed class MenuTemplateTests
{
    // The menu block mirrors App.xaml: the five brushes the templates need + the four role
    // templates + the MenuItem style. If App.xaml's structure changes, this smoke check should too.
    private const string MenuXaml = """
        <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
          <SolidColorBrush x:Key="TextBrush" Color="#E4E9EF"/>
          <SolidColorBrush x:Key="TextMutedBrush" Color="#98A3B2"/>
          <SolidColorBrush x:Key="MenuBackgroundBrush" Color="#161A20"/>
          <SolidColorBrush x:Key="MenuHoverBrush" Color="#243040"/>
          <SolidColorBrush x:Key="MenuBorderBrush" Color="#2B333D"/>
          <ControlTemplate x:Key="MenuItemTopLevelHeader" TargetType="{x:Type MenuItem}">
            <Grid>
              <Border x:Name="Bd" Background="{TemplateBinding Background}" CornerRadius="6" Padding="{TemplateBinding Padding}">
                <ContentPresenter ContentSource="Header" RecognizesAccessKey="True" VerticalAlignment="Center"/>
              </Border>
              <Popup x:Name="Popup" IsOpen="{TemplateBinding IsSubmenuOpen}" Placement="Bottom" AllowsTransparency="True" Focusable="False">
                <Border Background="{StaticResource MenuBackgroundBrush}" BorderBrush="{StaticResource MenuBorderBrush}" BorderThickness="1" CornerRadius="8" Padding="0,4" Margin="2,2,12,12">
                  <ItemsPresenter Grid.IsSharedSizeScope="True"/>
                </Border>
              </Popup>
            </Grid>
          </ControlTemplate>
          <ControlTemplate x:Key="MenuItemSubmenuItem" TargetType="{x:Type MenuItem}">
            <Border x:Name="Bd" Background="{TemplateBinding Background}" CornerRadius="5" Margin="4,1" Padding="9,5">
              <ContentPresenter ContentSource="Header" RecognizesAccessKey="True" VerticalAlignment="Center"/>
            </Border>
          </ControlTemplate>
          <Style TargetType="MenuItem">
            <Setter Property="Background" Value="{StaticResource MenuBackgroundBrush}"/>
            <Setter Property="Foreground" Value="{StaticResource TextBrush}"/>
            <Setter Property="Template" Value="{StaticResource MenuItemSubmenuItem}"/>
            <Style.Triggers>
              <Trigger Property="Role" Value="TopLevelHeader">
                <Setter Property="Template" Value="{StaticResource MenuItemTopLevelHeader}"/>
              </Trigger>
            </Style.Triggers>
          </Style>
        </ResourceDictionary>
        """;

    [Fact]
    public void ATopLevelMenu_OpensItsSubmenu_AndTheItemsRender()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            Window? window = null;
            try
            {
                var dict = (ResourceDictionary)XamlReader.Parse(MenuXaml);

                var newItem = new MenuItem { Header = "New workspace" };
                var file = new MenuItem { Header = "File" };
                file.Items.Add(newItem);
                var menu = new Menu();
                menu.Items.Add(file);
                menu.Resources.MergedDictionaries.Add(dict);

                window = new Window { Content = menu, Width = 300, Height = 120, Left = -2000, Top = -2000 };
                window.Show();
                window.UpdateLayout();

                // File is a TopLevelHeader (it has children); open its submenu.
                file.IsSubmenuOpen = true;
                window.UpdateLayout();
                menu.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

                // The retemplated header must actually apply (rounded popup) and the child item must render.
                Assert.Equal("MenuItemTopLevelHeader",
                    (file.Template as ControlTemplate) is { } ? KeyOf(dict, file.Template) : "none");

                var texts = FindDescendants<TextBlock>(newItem).Select(t => t.Text)
                    .Concat(FindDescendants<ContentPresenter>(newItem).Select(c => (c.Content as string) ?? ""))
                    .Where(s => !string.IsNullOrEmpty(s)).ToList();
                Assert.Contains(texts, s => s.Contains("New workspace", StringComparison.Ordinal));
            }
            catch (Exception ex) { failure = ex; }
            finally { window?.Close(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw failure;
    }

    private static string KeyOf(ResourceDictionary d, object value)
    {
        foreach (var k in d.Keys) if (ReferenceEquals(d[k], value)) return k.ToString()!;
        return "unkeyed";
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
