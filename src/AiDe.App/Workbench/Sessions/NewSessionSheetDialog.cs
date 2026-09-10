using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.Presentation.Sessions;

namespace AiDe.App.Workbench.Sessions;

/// <summary>
/// The New Session sheet as a modal window (A4.3: <b>one screen, not a wizard</b>).
/// </summary>
/// <remarks>
/// <para><b>The rules are not here.</b> Every refusal, every derivation and the whole backend list
/// live on <see cref="NewSessionSheetViewModel"/>, which is testable without a window; this renders it
/// and reflects it. A dialog that decided anything would be a second place a session can be created
/// wrongly.</para>
///
/// <para><b>Task class carries no pre-filled value</b>, deliberately: pre-filling one is how a
/// default arrives by another route, and a defaulted class ranks in the wrong cohort (DC-110). The
/// Create button stays disabled, with its reason on screen, until the operator types one.</para>
///
/// <para><b>Sign in stays on the sheet</b> (R13 b2, Ruling 20): it launches the engine's own login,
/// re-probes, and re-renders the rows in place — the sheet is never left, and no credential is
/// handled here.</para>
/// </remarks>
public static class NewSessionSheetDialog
{
    /// <summary>Shows the sheet modally. Returns whether the operator pressed Create.</summary>
    public static bool Show(NewSessionSheetViewModel sheet, Window? owner, Action<string>? announce = null)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var window = new Window
        {
            Title = "New session",
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.ToolWindow,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Owner = owner,
        };
        window.SetResourceReference(Window.BackgroundProperty, "SurfaceRaisedBrush");
        window.Content = Build(sheet, announce, () => window.DialogResult = true);

        return window.ShowDialog() == true;
    }

    /// <summary>
    /// Builds the sheet's body, with no window around it.
    /// </summary>
    /// <remarks>
    /// Separated from <see cref="Show"/> so what the sheet <b>renders</b> can be asserted:
    /// <c>ShowDialog</c> blocks, so a test that had to open the window could only ever check the
    /// model. What the operator actually reads — the lease sentence in particular (Ruling 42) — is a
    /// property of this tree, and <c>TheSheetSaysTheLeaseIsNotDerivableUntilAGoalBlockExists</c>
    /// reads it here.
    /// </remarks>
    /// <param name="onCreate">What Create does; the window supplies its own dialog result.</param>
    internal static FrameworkElement Build(
        NewSessionSheetViewModel sheet, Action<string>? announce, Action onCreate)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(onCreate);

        var name = new TextBox { Text = sheet.Name, Padding = new Thickness(8, 6, 8, 6) };
        AutomationProperties.SetName(name, "Session name");

        var taskClass = new TextBox { Padding = new Thickness(8, 6, 8, 6) };
        AutomationProperties.SetName(taskClass, "Task class (required)");

        var backends = new StackPanel();
        AutomationProperties.SetName(backends, "Agent backends");

        var blocked = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
        blocked.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var create = new Button { Content = "Create session", IsDefault = true, MinWidth = 120 };
        var cancel = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 90,
            Margin = new Thickness(8, 0, 0, 0),
        };

        void Reflect()
        {
            sheet.Name = name.Text;
            sheet.TaskClass = string.IsNullOrWhiteSpace(taskClass.Text) ? null : taskClass.Text;

            create.IsEnabled = sheet.CanCreate;
            blocked.Text = sheet.BlockedReason ?? string.Empty;
            blocked.Visibility = sheet.CanCreate ? Visibility.Collapsed : Visibility.Visible;
        }

        void RenderBackends()
        {
            backends.Children.Clear();

            if (sheet.Backends.Count == 0)
            {
                // The honest empty state. No provider is configured, so there is nothing to offer —
                // saying so is different from a list that is empty because something failed.
                var none = new TextBlock
                {
                    Text = "No agent backend is configured. Sessions still open; a run needs one.",
                    TextWrapping = TextWrapping.Wrap,
                };
                none.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
                backends.Children.Add(none);
                return;
            }

            foreach (var backend in sheet.Backends)
            {
                var row = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };

                if (sheet.CanSignIn(backend.EngineId) && !backend.RoutableForThisSession)
                {
                    var signIn = new Button
                    {
                        Content = "Sign in",
                        Padding = new Thickness(10, 2, 10, 2),
                        MinWidth = 80,
                    };
                    AutomationProperties.SetName(signIn, $"Sign in to {backend.EngineId}");
                    signIn.Click += (_, _) =>
                    {
                        announce?.Invoke(sheet.SignIn(backend.EngineId));
                        RenderBackends();
                        Reflect();
                    };
                    DockPanel.SetDock(signIn, Dock.Right);
                    row.Children.Add(signIn);
                }

                var toggle = new CheckBox
                {
                    Content = backend.DisplayLabel,
                    IsChecked = sheet.IsBackendEnabled(backend.EngineId),
                    VerticalAlignment = VerticalAlignment.Center,
                };
                AutomationProperties.SetName(toggle, backend.DisplayLabel);
                toggle.Checked += (_, _) => sheet.SetBackendEnabled(backend.EngineId, true);
                toggle.Unchecked += (_, _) => sheet.SetBackendEnabled(backend.EngineId, false);
                row.Children.Add(toggle);

                backends.Children.Add(row);
            }
        }

        name.TextChanged += (_, _) => Reflect();
        taskClass.TextChanged += (_, _) => Reflect();
        create.Click += (_, _) => onCreate();

        var buttons = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0),
        };
        buttons.Children.Add(create);
        buttons.Children.Add(cancel);

        var body = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(Label("Name"));
        body.Children.Add(name);
        body.Children.Add(Label("Task class — required, and never defaulted"));
        body.Children.Add(taskClass);
        // Ruling 42: a sentence, never a Lease. There is nothing at sheet time to derive one from,
        // and a derived "everything" would have travelled out of the sheet into a run.
        body.Children.Add(Label("Lease"));
        body.Children.Add(Muted(NewSessionSheetViewModel.LeaseDisplay));
        body.Children.Add(Label("Agent backends"));
        body.Children.Add(backends);
        body.Children.Add(blocked);
        body.Children.Add(buttons);

        RenderBackends();
        Reflect();

        return body;
    }

    private static TextBlock Label(string text)
    {
        var block = new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 4),
        };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        return block;
    }

    private static TextBlock Muted(string text)
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        block.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return block;
    }
}
