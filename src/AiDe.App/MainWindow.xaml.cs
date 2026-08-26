using System.Windows;
using AiDe.App.ViewModels;

namespace AiDe.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Composition root: the window binds to the real in-process core when a workspace root is
        // configured, and to the first-run view model otherwise. Nothing is invented when absent.
        DataContext = MainWindowViewModel.OpenDefault();
    }
}
