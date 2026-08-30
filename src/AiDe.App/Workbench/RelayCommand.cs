using System.Windows.Input;

namespace AiDe.App.Workbench;

/// <summary>A minimal always-executable <see cref="ICommand"/> for wiring a button to an action.</summary>
public sealed class RelayCommand(Action execute) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute();
}
