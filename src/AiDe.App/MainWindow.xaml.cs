using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media;
using AiDe.App.ViewModels;
using AiDe.App.Workbench;

namespace AiDe.App;

public partial class MainWindow : Window
{
    private readonly ShellModeController _mode;

    /// <summary>
    /// The provider file, as the last <c>File → New Session</c> read it. <b>The one construction site
    /// of a <c>ProviderRegistry</c> in the product</b>, and the source of the sheet's account list and
    /// the composer's run binding alike.
    /// </summary>
    private AiDe.Core.AgentPlane.ProviderConfiguration? _providers;

    public MainWindow()
    {
        InitializeComponent();

        // Composition root. The window is built and shown immediately over the first-run state, and
        // the workspace attaches when it resolves — reaching a daemon can mean a cold process start,
        // and a window that appears only once another process has launched looks like a failure to
        // launch.
        DataContext = new MainWindowViewModel();

        Shell = new WorkbenchShell(null);
        WorkbenchHost.Content = Shell.WorkbenchRoot;

        // FACELIFT — the docking host ships AvalonDock's default LIGHT theme (white panes, light
        // square tabs), which clashed with the dark shell and was the "clunky, square" look. A dark
        // theme is applied HERE — in the Design-owned window, not the Core-owned WorkbenchShell — so
        // the panes and tabs read as part of the app instead of a white rectangle bolted on. Its
        // accents are then pulled from VS blue toward our palette by DockThemeAccents (a value-based
        // brush override, no template surgery — see the AvalonDock decision note).
        Shell.Manager.Theme = new AvalonDock.Themes.Vs2013DarkTheme();
        AiDe.App.Workbench.DockThemeAccents.Retokenise(Shell.Manager);
        AiDe.App.Workbench.DockRoundedTabs.Apply(Shell.Manager);

        LiveRegionHost.Content = Shell.LiveRegion;

        // Keyboard commands bind to the window so they work wherever focus is inside it —
        // a layout command that only fires when a pane happens to be focused is not keyboard
        // operable in any useful sense.
        // The palette overlays the whole window, above the docking host.
        RootLayer.Children.Add(Shell.Palette.Root);

        Shell.Bind(this);

        // Primary view mode (ADR-0017 primary-view-mode): the Explore rail item toggles the body between the workbench
        // docking host and the full-window Explorer surface. Shell is held for the window's life, so
        // the swap only unparents the docking host — the workbench (and a running terminal) survive.
        _mode = new ShellModeController(
            WorkbenchHost,
            Shell.WorkbenchRoot,
            () => new ExplorerSurface(Shell.CreateExplorerGraph(), new NodeReaderView()));
        _mode.ModeChanged += (_, mode) =>
        {
            ReflectMode(mode);

            // Reload the Explorer graph with the CURRENT workspace when entering. The surface is
            // retained (US-E6), so without this a surface first created before a workspace opened
            // would keep showing "No workspace is open" on every later entry. No-ops on the very
            // first entry (the canvas is not Ready yet); its own NavigationCompleted does that load.
            if (mode == ShellViewMode.Explorer && _mode.ExplorerSurface is ExplorerSurface explorer)
            {
                _ = explorer.Graph.RefreshAsync();
            }
        };
        ReflectMode(ShellViewMode.Workbench);

        // A dock document opens into a body that is on screen (INV-0009, DC-148). Every opening
        // command in the shell raises this just before it adds the surface; when the Explorer is the
        // body, the workbench returns — the Explorer surface is retained, exactly as the rail's
        // toggle leaves it — so the document the command announces is the document the operator
        // sees. The replay probe wires the same line, and a scan asserts both carry it.
        Shell.DocumentOpening += () => _mode.Set(ShellViewMode.Workbench, "document-opening");

        // The most common moment to lose an arrangement is rearranging and immediately closing, so
        // the pending debounced save is flushed on the way out rather than left to a timer.
        Closed += (_, _) => Shell.Dispose();

        // The shell names its binary the moment it has a size and a DPI to report (INV-0008, Fix D):
        // one app.start line on the normal path, so a UI report can be attributed to `1.0.0+<sha>`
        // before it is triaged as new or recurring. Written before the workspace opens, so a boot
        // that fails to attach still recorded which build failed.
        Loaded += (_, _) => WorkbenchDiagnostics.AppStart(
            Shell.Manager.Theme?.GetType().Name ?? "(none)",
            VisualTreeHelper.GetDpi(this),
            ActualWidth,
            ActualHeight,
            WindowState.ToString());

        Loaded += async (_, _) => await OpenWorkspaceAsync();

        // The folder picker lives here because only a Window can show one; the controller holds the
        // command and knows nothing about dialogs.
        Shell.Controller.WorkspaceOpen = ChooseAndOpenAsync;

        // File → New Session (R13 b1). Wired here for the same reason as the folder picker: the
        // sheet and the workspace chooser are both windows, and only a Window can show one.
        Shell.Controller.NewSessionRequested = NewSession;

        // AR5 — Explorer is a catalog command, so it reaches the menu and the palette and is no
        // longer reachable only by pressing one 44×44 icon.
        Shell.Controller.ExplorerToggleRequested = ToggleExplorerMode;

        // Built from the command catalog, so the menu cannot offer something the product no longer
        // does — and every item shows its chord, which is how the chord becomes discoverable.
        RebuildMenu();
    }

    /// <summary>Reaches the workspace's daemon and points the window at it.</summary>
    /// <remarks>
    /// Failure is shown on the status strip by the view model itself and the window stays on its
    /// first-run surface. Nothing falls back to running the core in this process: that would work,
    /// and would abandon the trust boundary, the workspace lock and the epoch fence at the moment
    /// they were most obviously needed.
    /// </remarks>
    private async Task OpenWorkspaceAsync()
    {
        var viewModel = await MainWindowViewModel.OpenDefaultAsync();
        DataContext = viewModel;

        if (viewModel.Queries is not null)
        {
            AttachWorkspace(viewModel);
        }
    }

    /// <summary>
    /// Points the shell at the workspace the window just opened, then gives every session document
    /// the saved arrangement restored its live, bound document (INV-0009 Phase 2b).
    /// </summary>
    /// <remarks>
    /// <b>The one site.</b> Both open paths — the default workspace at launch and a chosen folder —
    /// come through here, so a restored session document cannot be live on one and an island on the
    /// other. Revived panes are rendered before they are bound, so the composer is in the tree when
    /// its context arrives, exactly as on the New Session path; the bind sentences go to the log,
    /// not the live region — the restore already announced, and three sentences on top of it would be
    /// three interruptions.
    /// </remarks>
    private void AttachWorkspace(MainWindowViewModel workspace)
    {
        Shell.AttachWorkspace(
            workspace.Queries!, workspace.DataDirectory, workspace.Commands,
            workspaceRoot: workspace.WorkspaceRoot);

        if (workspace.WorkspaceRoot is not { } root)
        {
            return;
        }

        var revived = Shell.ReviveRestoredSessionDocuments(root);
        if (revived.Count == 0)
        {
            return;
        }

        Shell.Adapter.Render();

        var malformed = ReadProviders();
        foreach (var config in revived)
        {
            _ = malformed is { } reason
                ? Workbench.Sessions.SessionComposerBinder.Refuse(Shell, config, "providers", reason)
                : BindComposer(config, RoutableBackendsOf(config), taskClass: null);
        }
    }

    /// <summary>Where this installation keeps state that is not any one workspace's.</summary>
    private static string ShellStateDirectory => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AiDe");

    /// <summary>
    /// Rebuilds the menu, including the recent list.
    /// </summary>
    /// <remarks>
    /// Rebuilt after every open rather than once at startup: the recent list changes when a
    /// workspace is opened, and a menu built once would show a list that is always one behind.
    /// </remarks>
    private void RebuildMenu() => MainMenuBuilder.Build(
        MainMenu,
        Shell.Controller,
        Close,
        MainMenuBuilder.RecentWorkspaces(ShellStateDirectory),
        path => _ = OpenAndAnnounceAsync(path),
        Workbench.Sessions.RecentSessions.All(ShellStateDirectory),
        ReopenSession);

    /// <summary>
    /// Runs <c>File → New Session</c> (R13 b1) and returns what to announce.
    /// </summary>
    /// <remarks>
    /// <para><b>The chooser interposes here, not in the sheet.</b> With no workspace open the flow
    /// calls <see cref="ChooseWorkspaceForSession"/> first, and a cancelled chooser ends the flow
    /// having created nothing — the sheet is not even constructed, because it is not constructible
    /// without a workspace.</para>
    ///
    /// <para><b>The provider registry is read here, once, and handed to BOTH consumers.</b>
    /// <c>~/.aide/providers.json</c> — §14.2's <c>providers.yaml</c>, with the <c>.yaml</c> filed as
    /// an erratum, see <see cref="AiDe.Core.AgentPlane.ProviderConfiguration"/> — is read into
    /// <see cref="_providers"/> before the sheet opens, and the same instance then binds the
    /// composer's run context. Two reads would be two registries, and the sheet's account list would
    /// be able to disagree with the account a run bills.</para>
    ///
    /// <para><b>Missing, malformed and configured are three states, not two.</b> No file: the sheet
    /// shows "no agent backend is configured", which is true, and a session still opens — a governed
    /// run is what needs a backend. A malformed file: this refuses before the sheet opens, naming the
    /// file and the field, because a registry read empty out of a broken file renders as the first
    /// state and is a wrong claim about a file the operator wrote (Ruling 47 (b)).</para>
    /// </remarks>
    private string NewSession()
    {
        if (ReadProviders() is { } malformed)
        {
            // REFUSED, NOT DEFAULTED. The flow does not start: an empty registry here would open the
            // sheet reading "no agent backend is configured" over a file that configures several.
            return malformed;
        }

        var flow = new Workbench.Sessions.NewSessionFlow(
            activeWorkspaceRoot: () => (DataContext as MainWindowViewModel)?.WorkspaceRoot,
            chooseWorkspace: ChooseWorkspaceForSession,
            showSheet: sheet => Workbench.Sessions.NewSessionSheetDialog.Show(
                sheet, this, Shell.Announcer.Announce),
            registry: () => _providers?.Registry ?? new AiDe.Core.AgentPlane.ProviderRegistry([]),
            workspaceId: root => root,
            opened: created =>
            {
                Workbench.Sessions.RecentSessions.Remember(
                    ShellStateDirectory,
                    new Workbench.Sessions.RecentSessionEntry(
                        created.Config.SessionId, created.Config.Name, created.Config.WorkspaceId));

                var opened = Shell.OpenSessionDocument(created.Config);
                // Both halves, in the order the operator experiences them: the document opens and
                // its composer is bound, then the pane takes the tree (Ruling 47). One sentence,
                // because a live region read three times in a row is three interruptions.
                Shell.Announcer.Announce(GiveTheNewSessionTheWholeTree(
                    created.Config.SessionId,
                    opened + " " + BindComposer(created.Config, created.RoutableBackends, created.TaskClass)));
                RebuildMenu();
            });

        return flow.Start().Announcement;
    }

    /// <summary>
    /// Reads the provider file into <see cref="_providers"/>, or returns the refusal when the file
    /// is malformed.
    /// </summary>
    /// <remarks>
    /// <b>Missing, malformed and configured are three states, not two.</b> No file: null, and the
    /// binder refuses by name when a run needs one. A malformed file: the message names the file
    /// and the field, and the caller refuses rather than proceeding over an empty registry that
    /// would render as "no agent backend is configured" — a wrong claim about a file the operator
    /// wrote (Ruling 47 (b)). Read on every path that binds a composer, so a reopen sees the file
    /// as it is now rather than as the last New Session left it.
    /// </remarks>
    private string? ReadProviders()
    {
        try
        {
            _providers = AiDe.Core.AgentPlane.ProviderConfiguration.ReadIfPresent(
                AiDe.Core.AgentPlane.ProviderConfiguration.DefaultPath);
            return null;
        }
        catch (AiDe.Core.AgentPlane.AgentPlaneException error)
        {
            _providers = null;
            return error.Message;
        }
    }

    /// <summary>
    /// Binds a session document's composer to the run it will start — the window's half is the
    /// workspace it has open and the modal that asks about an outside-workspace file; the binding
    /// itself is <see cref="Workbench.Sessions.SessionComposerBinder"/>, shared by every path that
    /// opens a session document (INV-0009 Phase 2).
    /// </summary>
    private string BindComposer(
        AiDe.Core.Sessions.SessionConfig config, IReadOnlyList<string> routableBackends, string? taskClass)
    {
        var workspace = DataContext as MainWindowViewModel;

        return Workbench.Sessions.SessionComposerBinder.Bind(
            Shell,
            config,
            routableBackends,
            taskClass,
            repositoryRoot: workspace?.WorkspaceRoot,
            dataDirectory: workspace?.DataDirectory,
            _providers,
            new AffirmOutsideWorkspaceAttachment(this));
    }

    /// <summary>
    /// Asks the operator about one outside-workspace file, in a modal owned by this window.
    /// </summary>
    /// <remarks>
    /// <b>It lives here for the same reason the folder picker does:</b> only a <c>Window</c> can show
    /// a modal, and the gate that decides <i>whether</i> to ask is in Core, where it is testable
    /// without one. The default is No — a dialog dismissed with Escape, closed with the title bar, or
    /// answered by a mis-click must not read as consent to send a file off the machine.
    /// </remarks>
    private sealed class AffirmOutsideWorkspaceAttachment(Window owner)
        : AiDe.Core.Presentation.Composer.IAttachmentAffirmation
    {
        public bool Confirm(AiDe.Core.Presentation.Composer.OutsideWorkspaceAffirmation affirmation)
        {
            ArgumentNullException.ThrowIfNull(affirmation);

            return MessageBox.Show(
                owner,
                affirmation.Prompt,
                "Attach a file from outside this workspace?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No) == MessageBoxResult.Yes;
        }
    }

    /// <summary>Reopens a session from the Recent sessions list, restoring its workspace first.</summary>
    /// <remarks>
    /// The workspace comes first because a session cannot exist unbound and its document reads its
    /// state from inside that workspace — reopening the document against a different workspace would
    /// show a session that is not the one the operator clicked.
    /// </remarks>
    private void ReopenSession(string sessionId)
    {
        var entry = Workbench.Sessions.RecentSessions.Find(ShellStateDirectory, sessionId);

        if (entry is null)
        {
            Shell.Announcer.Announce("That session is no longer available.");
            return;
        }

        _ = ReopenSessionAsync(entry);
    }

    private async Task ReopenSessionAsync(Workbench.Sessions.RecentSessionEntry entry)
    {
        var current = (DataContext as MainWindowViewModel)?.WorkspaceRoot;

        if (!string.Equals(current, entry.WorkspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            var opened = await OpenWorkspaceAtAsync(entry.WorkspaceRoot);

            if ((DataContext as MainWindowViewModel)?.Queries is null)
            {
                Shell.Announcer.Announce(opened);
                return;
            }
        }

        var store = new AiDe.Core.Sessions.SessionConfigStore(entry.WorkspaceRoot, entry.SessionId);
        AiDe.Core.Sessions.SessionConfig config;

        try
        {
            config = store.Load();
        }
        catch (Exception error) when (error is System.IO.IOException or System.Text.Json.JsonException)
        {
            Shell.Announcer.Announce($"“{entry.Name}” could not be read from its workspace.");
            return;
        }

        // Shown AND bound (INV-0009 §6). The reopen used to end at the open, so the document was
        // shown with a composer nothing had configured; the routable set is derived from the
        // config's enabled backends against the registry as it reads now, and the task class is
        // whatever the session has on record — none, until the operator chooses one for a prompt.
        // A malformed provider file is a refusal on the composer, by name: the session exists and
        // is shown; a bound composer over an empty registry would be a wrong claim about the file.
        var shown = Shell.OpenSessionDocument(config);
        var malformed = ReadProviders();
        Shell.Announcer.Announce(shown + " " + (malformed is { } reason
            ? Workbench.Sessions.SessionComposerBinder.Refuse(Shell, config, "providers", reason)
            : BindComposer(config, RoutableBackendsOf(config), taskClass: null)));
    }

    /// <summary>
    /// The backends a session on record may bind now: its enabled set, filtered by the registry's
    /// login state — the sheet's own derivation, so a reopen cannot bind an engine the sheet would
    /// have refused. Empty with no provider file, where the binder refuses by name anyway.
    /// </summary>
    private IReadOnlyList<string> RoutableBackendsOf(AiDe.Core.Sessions.SessionConfig config) =>
        _providers is { } providers
            ? AiDe.Core.Presentation.Sessions.NewSessionSheetViewModel.RoutableAmong(config.EnabledBackends, providers.Registry)
            : [];

    /// <summary>Shows the workspace chooser that interposes when no workspace is open (R13 b1).</summary>
    private string? ChooseWorkspaceForSession()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "A session belongs to a workspace — choose one",
            Multiselect = false,
        };

        return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
    }

    private async Task OpenAndAnnounceAsync(string path) =>
        Shell.Announcer.Announce(await OpenWorkspaceAtAsync(path));

    /// <summary>Shows a folder picker and opens the chosen repository as a workspace.</summary>
    /// <remarks>
    /// Returns the sentence to announce rather than announcing itself, so every outcome — chosen,
    /// cancelled, failed — comes back through one path and none of them can be silent.
    /// </remarks>
    private async Task<string> ChooseAndOpenAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose a repository to open as a workspace",
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) != true)
        {
            return "No workspace was opened.";
        }

        return await OpenWorkspaceAtAsync(dialog.FolderName);
    }

    /// <summary>Opens a specific folder as a workspace and reports the outcome.</summary>
    private async Task<string> OpenWorkspaceAtAsync(string folder)
    {
        var viewModel = await MainWindowViewModel.OpenAsync(folder);
        DataContext = viewModel;

        if (viewModel.Queries is null)
        {
            return viewModel.StatusMessage;
        }

        AttachWorkspace(viewModel);

        Shell.Adapter.Render();
        Shell.BindCanvas();
        Shell.BindContexts();

        // Remembered only on SUCCESS. A folder that could not be opened does not belong in a list
        // whose whole promise is that clicking an entry works.
        MainMenuBuilder.RememberWorkspace(ShellStateDirectory, folder);
        RebuildMenu();

        return $"Workspace open: {System.IO.Path.GetFileName(folder.TrimEnd((char)92))}. " +
               "Press Ctrl+K, I to index its C# projects.";
    }

    internal WorkbenchShell Shell { get; }

    private void OnResetLayout(object sender, RoutedEventArgs e)
    {
        Shell.Controller.Execute("workbench.resetLayout");
        Shell.Adapter.Render();
    }

    /// <summary>
    /// The rail's Explorer item, routed through the catalog command rather than calling the mode
    /// controller directly — one door, so the rail and the palette cannot drift apart (AR5).
    /// </summary>
    private void OnToggleExplorer(object sender, RoutedEventArgs e) =>
        Shell.Controller.Execute("shell.toggleExplorer");

    /// <summary>The rail's one primary action, on the same catalog command as File → New Session.</summary>
    private void OnNewSessionFromRail(object sender, RoutedEventArgs e) =>
        Shell.Controller.Execute("session.new");

    /// <summary>Swaps the body between the workbench and Explorer, and says which one is showing.</summary>
    private string ToggleExplorerMode()
    {
        _mode.Toggle("shell.toggleExplorer");

        return _mode.Mode == ShellViewMode.Explorer
            ? "Explorer: graph and reader. The workbench is retained, not closed."
            : "Workbench. The same panes, in the arrangement you left them.";
    }

    /// <summary>
    /// Hands a newly created session's document the whole tree (Ruling 47), and re-renders.
    /// </summary>
    /// <remarks>
    /// The decision itself lives on <see cref="Workbench.Sessions.NewSessionPlacement"/>, which a
    /// test can reach; this is the window's half — the projection has to be re-rendered after the
    /// layout changes, and only the window holds the adapter. Reopening a session does not call it.
    /// </remarks>
    private string GiveTheNewSessionTheWholeTree(string sessionId, string announcement)
    {
        var said = Workbench.Sessions.NewSessionPlacement.GiveItTheWholeTree(
            Shell.Service, sessionId, announcement);

        Shell.Adapter.Render();
        return said;
    }

    /// <summary>Reflects the active view mode on the Explore rail item — accent bar, pill, icon colour.</summary>
    private void ReflectMode(ShellViewMode mode)
    {
        var active = mode == ShellViewMode.Explorer;
        ExploreAccentBar.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        ExploreRailButton.Background = active ? (Brush)FindResource("SurfaceRaisedBrush") : Brushes.Transparent;
        ExploreRailButton.Foreground = active
            ? (Brush)FindResource("AccentBrush")
            : (Brush)FindResource("TextMutedBrush");
        AutomationProperties.SetName(ExploreRailButton, active ? "Explorer mode (active)" : "Explorer mode");
    }

    // FACELIFT — the window's own chrome, drawn by DWM, not by us. AllowsTransparency stays False
    // (the WPF default) so the system keeps the drop shadow and the Windows 11 rounded corners; the
    // AllowsTransparency=True path would disable both, which is the single most common modern-WPF
    // defect (WPF-TRANSPARENCY-TRAP).
    //
    // The interop moved to DarkCaption, which is now the only place a window's non-client area is
    // decided. It used to live here and ONLY here, which is exactly why both dialogs shipped a light
    // caption: the opt-in was a thing you had to know about rather than a thing you got (TC4).
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Workbench.DarkCaption.ApplyToHandle(new WindowInteropHelper(this).Handle, round: true);
    }
}
