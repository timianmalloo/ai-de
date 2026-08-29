using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using AiDe.App.ViewModels;
using AiDe.App.Workbench;

namespace AiDe.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Composition root. The window is built and shown immediately over the first-run state, and
        // the workspace attaches when it resolves — reaching a daemon can mean a cold process start,
        // and a window that appears only once another process has launched looks like a failure to
        // launch.
        DataContext = new MainWindowViewModel();

        Shell = new WorkbenchShell(null);
        WorkbenchHost.Content = Shell.Manager;

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

        // The most common moment to lose an arrangement is rearranging and immediately closing, so
        // the pending debounced save is flushed on the way out rather than left to a timer.
        Closed += (_, _) => Shell.Dispose();

        Loaded += async (_, _) => await OpenWorkspaceAsync();

        // The folder picker lives here because only a Window can show one; the controller holds the
        // command and knows nothing about dialogs.
        Shell.Controller.WorkspaceOpen = ChooseAndOpenAsync;

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
        path => _ = OpenAndAnnounceAsync(path));

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

    // FACELIFT — the window's own chrome, drawn by DWM, not by us. AllowsTransparency stays False
    // (the WPF default) so the system keeps the drop shadow and the Windows 11 rounded corners; the
    // AllowsTransparency=True path would disable both, which is the single most common modern-WPF
    // defect (WPF-TRANSPARENCY-TRAP). Here we only opt in explicitly and darken the caption so the
    // title bar matches the app instead of flashing a light system bar on a dark window.
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        // A hint, not a guarantee: pre-Windows-11 and unusual composition states ignore it, and that
        // is the correct outcome — the window is still a normal, usable window, just not rounded.
        int dark = 1;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

        int round = DWMWCP_ROUND;
        _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
