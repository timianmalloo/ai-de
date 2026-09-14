using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AiDe.App.Workbench;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Tests.Shell;

/// <summary>
/// The Coding host as the product composes it (DC-135) — a real <see cref="WorkbenchShell"/> over
/// no queries, its host A's root inside a frame cut like <c>MainWindow.xaml</c> (menu · title strip
/// · a 56 px rail beside the body · status strip), shown off-screen at an outer size. The frame is a
/// replica of the window's rows, not the window: <c>MainWindow</c> starts the shell and reaches a
/// daemon, so its chrome is re-stated here from the markup's numbers and the body's measured size is
/// printed by every oracle that reads geometry from it.
/// </summary>
internal static class ComposedCoding
{
    /// <summary>A throw-away workspace root holding one session, created the way the sheet creates it.</summary>
    public sealed class Workspace : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "aide-sh42-" + Guid.NewGuid().ToString("N"));

        public Workspace() => Directory.CreateDirectory(Root);

        public SessionConfig Session(string name)
        {
            var now = DateTimeOffset.UtcNow;
            return new SessionConfigStore(Root, SessionId.New(now)).Create(name, Root, [], null, now);
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); } catch (IOException) { }
        }
    }

    /// <summary>The shown frame: the window, the shell, and the body cell host A's root fills.</summary>
    public sealed record Frame(Window Window, WorkbenchShell Shell, ContentControl Body) : IDisposable
    {
        public void Dispose()
        {
            Window.Close();
            Shell.Dispose();
        }

        /// <summary>The live session document a surface renders, through the island the factory wraps it in.</summary>
        public SessionDocumentSurface Document(string surfaceId)
        {
            var content = Shell.Coding.Adapter.ContentFor(surfaceId);
            Assert.NotNull(content);
            var document = Visuals<SessionDocumentSurface>(content!).FirstOrDefault() ?? content as SessionDocumentSurface;
            Assert.True(document is not null, $"'{surfaceId}' renders no SessionDocumentSurface");
            return document!;
        }

        /// <summary>The Center's empty copy — the content behind the projection's placeholder — or null while the Center holds documents.</summary>
        public FrameworkElement? CenterEmpty() => Shell.Coding.Adapter.ContentFor(ZonesToTree.WelcomePlaceholder.SurfaceId);

        public void Settle()
        {
            Window.UpdateLayout();
            Window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ContextIdle, () => { });
            Window.UpdateLayout();
        }
    }

    public static Frame Show(double outerWidth, double outerHeight)
    {
        var shell = new WorkbenchShell(queries: null);

        var frame = new Grid();
        frame.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        frame.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        frame.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        frame.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var menu = new Menu();
        menu.Items.Add(new MenuItem { Header = "_File" });
        Grid.SetRow(menu, 0);
        frame.Children.Add(menu);

        var title = new TextBlock { Text = "workspace", FontSize = 15, Margin = new Thickness(12, 8, 12, 8) };
        Grid.SetRow(title, 1);
        frame.Children.Add(title);

        var bodyRow = new Grid();
        bodyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });
        bodyRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var body = new ContentControl { Content = shell.Coding.Root };
        Grid.SetColumn(body, 1);
        bodyRow.Children.Add(body);
        Grid.SetRow(bodyRow, 2);
        frame.Children.Add(bodyRow);

        var status = new TextBlock { Text = "Coding perspective", Margin = new Thickness(12, 6, 12, 6) };
        Grid.SetRow(status, 3);
        frame.Children.Add(status);

        var window = new Window
        {
            Content = frame,
            Width = outerWidth,
            Height = outerHeight,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -20000,
            Top = -20000,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Resources.MergedDictionaries.Add(ThemeProbe.AppTheme());
        window.Show();
        window.UpdateLayout();
        shell.Coding.Adapter.Render();
        window.UpdateLayout();

        return new Frame(window, shell, body);
    }

    public static IEnumerable<T> Visuals<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T hit)
            {
                yield return hit;
            }

            foreach (var deeper in Visuals<T>(child))
            {
                yield return deeper;
            }
        }
    }
}
