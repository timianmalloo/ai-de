using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.Core;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;
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
public sealed class WorkbenchShell : IDisposable
{
    private SurfaceContentFactory _factory;

    public WorkbenchShell(IWorkspaceQueries? queries, string? workspaceDataDirectory = null)
    {
        Service = new LayoutService();

        LiveRegion = new TextBlock { TextWrapping = TextWrapping.Wrap };
        LiveRegion.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        Announcer = new WorkbenchAnnouncer(LiveRegion);

        _factory = new SurfaceContentFactory(queries);
        Manager = new DockingManager();
        AutomationProperties.SetName(Manager, "Workbench");

        // Indirected through a field so the workspace can arrive AFTER the window exists. The
        // shell is built synchronously and shown immediately; reaching a daemon may take a cold
        // start, and a window that appears only once a process has launched looks like a failure to
        // launch.
        Adapter = new WorkbenchAdapter(Manager, Service, surface => _factory.Create(surface));
        Controller = new WorkbenchController(Service, Announcer);

        Palette = new CommandPalette(Controller, Announcer);

        // Persistence is per workspace and lives beside the fact store (ADR-0013). With no workspace
        // open there is nothing to persist against, so first-run simply starts from the default.
        //
        // The directory is passed in rather than read off a core: the shell no longer holds one.
        // Layout is the SHELL's state, not the workspace's — it stays local even when the evidence
        // it arranges is answered by another process.
        if (!string.IsNullOrEmpty(workspaceDataDirectory))
        {
            var surfaces = Service.Current.AllStacks()
                .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId)
                .ToHashSet(StringComparer.Ordinal);

            Persistence = new LayoutPersistence(
                Service, Path.Combine(workspaceDataDirectory, "layout.json"), surfaces);

            var restored = Persistence.Restore();
            if (restored.ErrorCode is not null || restored.WasDefaulted)
            {
                // A partial or failed restore must be told to the user, not silently absorbed: they
                // are about to look at an arrangement that is not the one they left.
                Announcer.Announce(restored.Announcement);
            }
        }

        Adapter.Render();
        TrackFocusedPane();
        PersistOnEveryChange();
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

    /// <summary>Saves and restores the arrangement across restarts. Null on first run.</summary>
    public LayoutPersistence? Persistence { get; private set; }

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

    /// <summary>
    /// Marks the layout dirty after any change, so nothing has to remember to save.
    /// </summary>
    /// <remarks>
    /// Hooked to the adapter's render rather than to individual commands: a save triggered per
    /// command would miss changes made by a drag, and every new operation would need to remember to
    /// opt in. One hook downstream of all of them cannot be forgotten.
    /// </remarks>
    private void PersistOnEveryChange()
    {
        if (Persistence is null)
        {
            return;
        }

        Manager.LayoutUpdated += (_, _) => Persistence.MarkDirty();
    }

    /// <summary>
    /// Points the shell at a workspace that became available after it was built.
    /// </summary>
    /// <remarks>
    /// Panes already on screen are re-rendered, because a pane showing "not available in this build"
    /// after the workspace opened is worse than one that never claimed anything.
    /// </remarks>
    public void AttachWorkspace(
        IWorkspaceQueries queries,
        string? dataDirectory,
        IWorkspaceCommands? commands = null,
        string scopeId = "fixture",
        string artifactRevision = "rev-1")
    {
        ArgumentNullException.ThrowIfNull(queries);

        _factory = new SurfaceContentFactory(queries);

        // The palette's re-index command is inert until a workspace exists to re-index. Wiring it
        // here rather than at construction is what makes it act on THIS workspace.
        if (commands is not null)
        {
            Controller.WorkspaceRefresh = async () =>
            {
                var status = await commands.RefreshScopeAsync(
                    scopeId, artifactRevision, CancellationToken.None);

                return status.State == ScopeRefreshState.Completed
                    ? $"Re-indexed: {status.AssertionCount} assertion(s). Reopen a pane to see them."
                    : $"Re-indexing failed: {status.Failure}";
            };
        }

        if (!string.IsNullOrEmpty(dataDirectory) && Persistence is null)
        {
            var surfaces = Service.Current.AllStacks()
                .SelectMany(stack => stack.Surfaces).Select(surface => surface.SurfaceId)
                .ToHashSet(StringComparer.Ordinal);

            Persistence = new LayoutPersistence(
                Service, Path.Combine(dataDirectory, "layout.json"), surfaces);

            var restored = Persistence.Restore();
            if (restored.ErrorCode is not null || restored.WasDefaulted)
            {
                Announcer.Announce(restored.Announcement);
            }
        }

        Adapter.Render();
    }

    public void Dispose() => Persistence?.Dispose();

    /// <summary>The command palette's rows: every keyboard-reachable layout command.</summary>
    public static IReadOnlyList<WorkbenchCommand> PaletteCommands(string search) =>
        [.. WorkbenchCommandCatalog.Search(search)];
}
