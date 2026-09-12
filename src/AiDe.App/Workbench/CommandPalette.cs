using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// The keyboard route to every command the active perspective offers.
/// </summary>
/// <remarks>
/// This is the mechanism US-9 names for SC 2.5.7: "an equivalent command exists and is reachable
/// from the command palette". Without it the catalog is a list nobody can invoke — the conformance
/// test would still pass while the product remained mouse-only, which is exactly the gap between
/// *tested* and *usable* that the criterion exists to close.
///
/// <b>Its rows are exactly the menu bar's commands (Addendum C US-C4 b3).</b> There is no
/// palette-only set: the rows come from the same <see cref="PerspectiveMenu"/> the bar renders,
/// set by the presenter on every switch through <see cref="Menu"/>, so a command the active
/// perspective cannot offer is absent here as it is there (PS-M3).
///
/// Focus handling is the load-bearing part. Opening moves focus into the search box deliberately
/// (the user asked for it); closing **restores focus to wherever it was**, so invoking a command
/// never strands a keyboard user somewhere they did not choose (SC 2.4.3).
/// </remarks>
public sealed class CommandPalette
{
    private readonly Func<string, bool> _execute;
    private readonly IWorkbenchAnnouncer _announcer;
    private IInputElement? _focusBeforeOpen;
    private PerspectiveMenu _menu = PerspectiveMenu.For(PerspectiveSet.Initial);

    /// <summary>A palette over one controller — a headless test's shape; the shell passes its router.</summary>
    /// <remarks>simplify: a test-only overload (eleven test sites, one of them in a file the census
    /// track owns this horizon). Ceiling: this constructor; trigger: when those sites can be edited,
    /// substitute <c>controller.Execute</c> at each and delete this.</remarks>
    public CommandPalette(WorkbenchController controller, IWorkbenchAnnouncer announcer)
        : this((controller ?? throw new ArgumentNullException(nameof(controller))).Execute, announcer)
    {
    }

    /// <param name="execute">Where a chosen row goes: the shell-level router (ADR-0031), which resolves the host a command reaches.</param>
    public CommandPalette(Func<string, bool> execute, IWorkbenchAnnouncer announcer)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _announcer = announcer;

        SearchBox = new TextBox();
        AutomationProperties.SetName(SearchBox, "Search commands");

        Results = new ListBox { DisplayMemberPath = nameof(WorkbenchCommand.Title) };
        AutomationProperties.SetName(Results, "Commands");

        Root = BuildRoot();
        SearchBox.TextChanged += (_, _) => Refresh();
        Refresh();
    }

    public Border Root { get; }

    public TextBox SearchBox { get; }

    public ListBox Results { get; }

    public bool IsOpen => Root.Visibility == Visibility.Visible;

    /// <summary>
    /// The active perspective's contribution — the rows this palette offers. Starts as the initial
    /// perspective's; the presenter sets it on every switch with the same model it hands the menu
    /// builder, so the two surfaces cannot disagree (E12).
    /// </summary>
    public PerspectiveMenu Menu
    {
        get => _menu;
        set
        {
            _menu = value ?? throw new ArgumentNullException(nameof(value));
            Refresh();
        }
    }

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
        _announcer.Announce($"Command palette. {Visible.Count} commands in the {_menu.Perspective.Title} perspective. Type to filter.");
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
        _execute(command.Id);
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
        //
        // A keystroke is spoken only when one is BOUND (PS-M4, US-C10 b3): the catalog's chord
        // strings are announced data nothing binds, and "New class diagram. Ctrl+K, M." promised a
        // key that did nothing. The palette's role for an unbound command is to run it.
        if (Results.SelectedItem is WorkbenchCommand command)
        {
            var bound = KeyGestures.For(command).FirstOrDefault();
            _announcer.Announce(bound is null
                ? $"{command.Title}. {command.Hint}"
                : $"{command.Title}. {bound.GetDisplayStringForCulture(System.Globalization.CultureInfo.CurrentCulture)}. {command.Hint}");
        }
    }

    private void Refresh()
    {
        var matches = _menu.Search(SearchBox.Text ?? string.Empty).ToList();
        Results.ItemsSource = matches;
        Results.SelectedIndex = matches.Count > 0 ? 0 : -1;

        var peer = UIElementAutomationPeer.FromElement(Results);
        peer?.RaiseAutomationEvent(AutomationEvents.StructureChanged);
    }

    private Border BuildRoot()
    {
        var heading = new TextBlock
        {
            Text = "Commands",
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
