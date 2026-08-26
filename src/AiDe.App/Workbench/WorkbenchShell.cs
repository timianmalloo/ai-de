using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.Core;
using AiDe.Core.Workbench;
using AvalonDock;

namespace AiDe.App.Workbench;

/// <summary>
/// The composition root for the workbench: model, adapter, controller, announcer and the docking
/// host, assembled and wired.
/// </summary>
/// <remarks>
/// This is the E10 reachability piece. Everything in Phase 1b was built and tested but unreachable —
/// the window still showed the superseded fixed grid, so a user could not touch any of it. A
/// capability nobody can open is not delivered.
///
/// Composition happens in one place on purpose: the live region, the controller and the adapter must
/// share the same <see cref="ILayoutService"/> instance, or the keyboard would mutate one layout
/// while the view rendered another.
/// </remarks>
public sealed class WorkbenchShell
{
    public WorkbenchShell(WorkspaceCore? core)
    {
        Service = new LayoutService();

        LiveRegion = new TextBlock { TextWrapping = TextWrapping.Wrap };
        LiveRegion.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        Announcer = new WorkbenchAnnouncer(LiveRegion);

        var factory = new SurfaceContentFactory(core?.Projections);
        Manager = new DockingManager();
        AutomationProperties.SetName(Manager, "Workbench");

        Adapter = new WorkbenchAdapter(Manager, Service, factory.Create);
        Controller = new WorkbenchController(Service, Announcer);

        Palette = new CommandPalette(Controller, Announcer);

        Adapter.Render();
        TrackFocusedPane();
    }

    public ILayoutService Service { get; }

    public DockingManager Manager { get; }

    public WorkbenchAdapter Adapter { get; }

    public WorkbenchController Controller { get; }

    public IWorkbenchAnnouncer Announcer { get; }

    /// <summary>The polite live region announcements are written to; also the visible status text.</summary>
    public TextBlock LiveRegion { get; }

    /// <summary>The keyboard route to every layout command (SC 2.5.7).</summary>
    public CommandPalette Palette { get; }

    /// <summary>Binds keyboard commands and the palette to a host element — normally the window.</summary>
    public void Bind(UIElement host)
    {
        Controller.Bind(host);

        // The palette must intercept BEFORE the workbench sees the key, or Up/Down would move the
        // pane selection underneath it while the user is choosing a command.
        host.PreviewKeyDown += (_, e) =>
        {
            if (Palette.HandleKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.P && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                Palette.Open();
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// Keeps the controller's notion of "the focused pane" in step with real focus.
    /// </summary>
    /// <remarks>
    /// Layout commands act on the focused pane, so a controller whose idea of focus drifts from the
    /// user's would apply commands to the wrong pane — silently, and only sometimes, which is the
    /// worst shape of bug. Derived from the actual focus event rather than tracked by hand.
    /// </remarks>
    private void TrackFocusedPane()
    {
        Manager.GotKeyboardFocus += (_, e) =>
        {
            if (e.NewFocus is not DependencyObject element)
            {
                return;
            }

            var surfaceId = FindSurfaceId(element);
            if (surfaceId is null)
            {
                return;
            }

            var stack = Service.Current.FindStackOf(surfaceId);
            Controller.FocusedSurfaceId = surfaceId;
            Controller.FocusedStackId = stack?.Id;
        };
    }

    /// <summary>Walks up from a focused element to the surface it belongs to.</summary>
    internal static string? FindSurfaceId(DependencyObject element)
    {
        var current = element;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: AvalonDock.Layout.LayoutContent content }
                && !string.IsNullOrEmpty(content.ContentId))
            {
                return content.ContentId;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    /// <summary>The command palette's rows: every keyboard-reachable layout command.</summary>
    public static IReadOnlyList<WorkbenchCommand> PaletteCommands(string search) =>
        [.. WorkbenchCommandCatalog.Search(search)];
}
