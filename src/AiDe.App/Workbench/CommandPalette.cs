using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// The keyboard route to every layout command.
/// </summary>
/// <remarks>
/// This is the mechanism US-9 names for SC 2.5.7: "an equivalent command exists and is reachable
/// from the command palette". Without it the catalog is a list nobody can invoke — the conformance
/// test would still pass while the product remained mouse-only, which is exactly the gap between
/// *tested* and *usable* that the criterion exists to close.
///
/// Focus handling is the load-bearing part. Opening moves focus into the search box deliberately
/// (the user asked for it); closing **restores focus to wherever it was**, so invoking a command
/// never strands a keyboard user somewhere they did not choose (SC 2.4.3).
/// </remarks>
public sealed class CommandPalette
{
    private readonly WorkbenchController _controller;
    private readonly IWorkbenchAnnouncer _announcer;
    private IInputElement? _focusBeforeOpen;

    public CommandPalette(WorkbenchController controller, IWorkbenchAnnouncer announcer)
    {
        _controller = controller;
        _announcer = announcer;

        SearchBox = new TextBox();
        AutomationProperties.SetName(SearchBox, "Search layout commands");

        Results = new ListBox { DisplayMemberPath = nameof(WorkbenchCommand.Title) };
        AutomationProperties.SetName(Results, "Layout commands");

        Root = BuildRoot();
        SearchBox.TextChanged += (_, _) => Refresh();
        Refresh();
    }

    public Border Root { get; }

    public TextBox SearchBox { get; }

    public ListBox Results { get; }

    public bool IsOpen => Root.Visibility == Visibility.Visible;

    /// <summary>The commands currently listed — what a test and the UI both read.</summary>
    public IReadOnlyList<WorkbenchCommand> Visible =>
        [.. Results.Items.Cast<WorkbenchCommand>()];

    public void Open()
    {
        _focusBeforeOpen = Keyboard.FocusedElement;
        SearchBox.Text = string.Empty;
        Refresh();
        Root.Visibility = Visibility.Visible;
        SearchBox.Focus();
        Keyboard.Focus(SearchBox);

        // Announced because a palette that opens silently is invisible to a screen-reader user until
        // they happen to arrow into it.
        _announcer.Announce($"Command palette. {Visible.Count} layout commands. Type to filter.");
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        Root.Visibility = Visibility.Collapsed;

        // Focus goes back where the user left it. Dropping focus to the window root here is the
        // classic way a palette strands keyboard users after every single invocation.
        if (_focusBeforeOpen is not null)
        {
            Keyboard.Focus(_focusBeforeOpen);
        }

        _focusBeforeOpen = null;
    }

    /// <summary>Runs the selected command and closes. Returns false when nothing is selected.</summary>
    public bool InvokeSelected()
    {
        if (Results.SelectedItem is not WorkbenchCommand command)
        {
            return false;
        }

        Close();
        _controller.Execute(command.Id);
        return true;
    }

    /// <summary>Handles palette keys. Returns true when the key was consumed.</summary>
    public bool HandleKey(Key key)
    {
        if (!IsOpen)
        {
            return false;
        }

        switch (key)
        {
            case Key.Down:
                Move(+1);
                return true;
            case Key.Up:
                Move(-1);
                return true;
            case Key.Enter:
                InvokeSelected();
                return true;
            case Key.Escape:
                Close();
                _announcer.Announce("Command palette closed.");
                return true;
            default:
                return false;
        }
    }

    private void Move(int direction)
    {
        if (Results.Items.Count == 0)
        {
            return;
        }

        var next = (Results.SelectedIndex + direction + Results.Items.Count) % Results.Items.Count;
        Results.SelectedIndex = next;
        Results.ScrollIntoView(Results.SelectedItem);

        // Selection moves without focus leaving the search box, so the change has to be announced
        // explicitly — the listbox never gets focus and therefore never announces itself.
        if (Results.SelectedItem is WorkbenchCommand command)
        {
            _announcer.Announce($"{command.Title}. {command.Gesture}. {command.Hint}");
        }
    }

    private void Refresh()
    {
        var matches = WorkbenchCommandCatalog.Search(SearchBox.Text ?? string.Empty).ToList();
        Results.ItemsSource = matches;
        Results.SelectedIndex = matches.Count > 0 ? 0 : -1;

        var peer = UIElementAutomationPeer.FromElement(Results);
        peer?.RaiseAutomationEvent(AutomationEvents.StructureChanged);
    }

    private Border BuildRoot()
    {
        var heading = new TextBlock
        {
            Text = "Layout commands",
            Margin = new Thickness(0, 0, 0, 6),
            FontWeight = FontWeights.SemiBold,
        };
        heading.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var hint = new TextBlock
        {
            Text = "Up and Down to choose · Enter to run · Escape to close",
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(heading);
        stack.Children.Add(SearchBox);
        stack.Children.Add(Results);
        stack.Children.Add(hint);

        Results.MaxHeight = 260;
        Results.Margin = new Thickness(0, 6, 0, 0);

        var border = new Border
        {
            Child = stack,
            Width = 520,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 80, 0, 0),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Visibility = Visibility.Collapsed,
        };
        border.SetResourceReference(Border.BackgroundProperty, "SurfaceRaisedBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        // Elevation belongs on COMPOSITED chrome — this overlay is WPF over WPF, so a soft shadow is
        // correct here (unlike over an HwndHost/WebView2 pane, where it would not composite). Set by
        // resource reference so it tracks the token.
        border.SetResourceReference(UIElement.EffectProperty, "ElevationRaised");
        AutomationProperties.SetName(border, "Command palette");
        return border;
    }
}
