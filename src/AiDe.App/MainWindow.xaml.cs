using System.Windows;
using AiDe.App.ViewModels;
using AiDe.App.Workbench;

namespace AiDe.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Composition root. The view model opens the workspace (or the first-run state); the shell
        // assembles the workbench over the same core so panes render real evidence.
        var viewModel = MainWindowViewModel.OpenDefault();
        DataContext = viewModel;

        Shell = new WorkbenchShell(viewModel.Core);
        WorkbenchHost.Content = Shell.Manager;
        LiveRegionHost.Content = Shell.LiveRegion;

        // Keyboard commands bind to the window so they work wherever focus is inside it —
        // a layout command that only fires when a pane happens to be focused is not keyboard
        // operable in any useful sense.
        Shell.Bind(this);
    }

    internal WorkbenchShell Shell { get; }

    private void OnResetLayout(object sender, RoutedEventArgs e)
    {
        Shell.Controller.Execute("workbench.resetLayout");
        Shell.Adapter.Render();
    }
}
