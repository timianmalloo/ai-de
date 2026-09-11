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

        // The most common moment to lose an arrangement is rearranging and immediately closing, so
        // the pending debounced save is flushed on the way out rather than left to a timer.
        Closed += (_, _) => Shell.Dispose();

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
            Shell.AttachWorkspace(
                viewModel.Queries, viewModel.DataDirectory, viewModel.Commands,
                workspaceRoot: viewModel.WorkspaceRoot);
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
    /// <para><b>No provider registry is configured yet, and the sheet says so.</b> §14.2's
    /// <c>providers.yaml</c> has no reader in this repository — <c>ProviderRegistry</c>'s own remarks
    /// record that parsing it is a caller's job — so the sheet is handed an empty registry and
    /// renders "no agent backend is configured", which is true. A session still opens; a governed
    /// run is what needs a backend.</para>
    /// </remarks>
    private string NewSession()
    {
        var flow = new Workbench.Sessions.NewSessionFlow(
            activeWorkspaceRoot: () => (DataContext as MainWindowViewModel)?.WorkspaceRoot,
            chooseWorkspace: ChooseWorkspaceForSession,
            showSheet: sheet => Workbench.Sessions.NewSessionSheetDialog.Show(
                sheet, this, Shell.Announcer.Announce),
            registry: () => new AiDe.Core.AgentPlane.ProviderRegistry([]),
            workspaceId: root => root,
            opened: created =>
            {
                Workbench.Sessions.RecentSessions.Remember(
                    ShellStateDirectory,
                    new Workbench.Sessions.RecentSessionEntry(
                        created.Config.SessionId, created.Config.Name, created.Config.WorkspaceId));

                var opened = Shell.OpenSessionDocument(created.Config);
                Shell.Announcer.Announce(GiveTheNewSessionTheWholeTree(created.Config.SessionId, opened));
                RebuildMenu();
            });

        return flow.Start().Announcement;
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

        Shell.Announcer.Announce(Shell.OpenSessionDocument(config));
    }

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

        Shell.AttachWorkspace(
            viewModel.Queries, viewModel.DataDirectory, viewModel.Commands,
            workspaceRoot: viewModel.WorkspaceRoot);

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
        _mode.Toggle();

        return _mode.Mode == ShellViewMode.Explorer
            ? "Explorer: graph and reader. The workbench is retained, not closed."
            : "Workbench. The same panes, in the arrangement you left them.";
    }

    /// <summary>
    /// Maximizes the pane a newly created session document landed in.
    /// </summary>
    /// <remarks>
    /// <para><b>AWAITING RATIFICATION — this is a proposal in code, and it is one line to withdraw.</b>
    /// The operator asked for New Session to give <i>"a full window view like the explorer view icon
    /// does"</i>. That cannot be delivered by pointing a button at the Explorer path: the Explorer
    /// icon swaps <c>ContentControl.Content</c> over a <b>two-value</b> <see cref="ShellViewMode"/>
    /// and is full-<b>body</b>, not full-window — the menu bar, title strip, rail and status strip
    /// all remain — and a session is a <b>dock document</b>. A4.4 and ADR-0017 say so in as many
    /// words: <i>"Sessions are dock documents inside the Workbench, not a third shell mode."</i></para>
    ///
    /// <para><b>What this does instead.</b> DESIGN.md already defines a <c>maximized</c> dock state —
    /// <i>"fills the tree; siblings are temporarily minimized and remembered as such"</i> — reached
    /// today by <c>workbench.maximizePane</c>. Creating a session applies it to the document's own
    /// stack. The felt experience is the one that was asked for; no third shell mode is introduced
    /// and no ADR is reopened. <c>Restore</c> undoes what maximizing did and not what the user
    /// did, so the arrangement survives.</para>
    ///
    /// <para><b>If the Owner rules otherwise</b> — either that a session really should be a third
    /// shell mode, or that a new session should not disturb the arrangement at all — the change is
    /// to stop calling this from <see cref="NewSession"/>. Nothing else depends on it, and the
    /// <c>maximized</c> state remains exactly as reachable by command as it is today.</para>
    ///
    /// <para><b>Reopening a session deliberately does not do this.</b> Maximizing is a response to
    /// "I just made this and I want to work in it", not a property of session documents; doing it on
    /// every reopen would rearrange the workbench behind an operator who asked for a tab.</para>
    /// </remarks>
    /// <param name="sessionId">The session whose document was just opened.</param>
    /// <param name="announcement">What opening the document already had to say.</param>
    /// <returns>That sentence, plus what maximizing did — or unchanged, when it did nothing.</returns>
    private string GiveTheNewSessionTheWholeTree(string sessionId, string announcement)
    {
        var surfaceId = Workbench.Sessions.SessionDocumentSurface.SurfaceIdFor(sessionId);
        var stack = Shell.Service.Current.FindStackOf(surfaceId);

        if (stack is null)
        {
            // The document did not reach the layout. Saying nothing extra is the honest outcome:
            // the open announcement already carries whatever did happen.
            return announcement;
        }

        var result = Shell.Service.Apply(
            new AiDe.Core.Workbench.LayoutOperation.SetStackState(
                stack.Id, AiDe.Core.Workbench.StackState.Maximized));

        Shell.Adapter.Render();

        // A refusal is announced, never swallowed — a maximize that silently did nothing is
        // indistinguishable from a dead command (DC-011).
        return $"{announcement} {result.Announcement}";
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
