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
/// <para><b>Task class opens on <c>free-form</c></b> (Ruling 72 (b)): the dialog selects the row
/// the model already holds — it never decides the default itself — and the operator changes it by
/// choosing another row. Create is enabled from open: the sheet has zero required inputs.</para>
///
/// <para><b>It is a picker, not a text box (RQ1).</b> A value whose only use is exact equality
/// against a set is entered by choosing from that set, because a free text box makes a typo
/// indistinguishable from an answer.</para>
///
/// <para><b>The budget is a state with an optional cap</b> (Ruling 72 (a)): <i>bounded by your
/// subscription</i> until the operator ticks <i>Enforce a cap</i>, and only then do the two number
/// boxes exist. <b>The fan-out ceiling is prefilled</b> (Ruling 56). <b>There is no tier</b>
/// (Ruling 63).</para>
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

        // Through the factory, so the CAPTION is dark too. This sheet was the measured instance of
        // TC4: a #F9F1EF title bar with black text and a red close button, above a #12151A body,
        // because only MainWindow ever opted into DWM's dark mode.
        var window = DarkCaption.CreateDialog("New session", owner, width: 520);
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

        // RQ1 — A BOUNDED PICKER, NOT A TEXT BOX. The old control was
        // `new TextBox { Padding = ... }`: free text, no options, no placeholder, no autocomplete,
        // for a value whose only use is exact string equality against a cohort key. A typo there is
        // worse than a default — it forms a cohort of one, renders Not Comparable, and silently
        // removes the episode from the cohort it belonged to.
        //
        var taskClass = new ListBox
        {
            Padding = new Thickness(2),
            MaxHeight = 190,
        };
        AutomationProperties.SetName(taskClass, "Task class");

        foreach (var option in sheet.TaskClassOptions)
        {
            var classId = new TextBlock { Text = option.Id, FontWeight = FontWeights.SemiBold };
            classId.FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono, Consolas");

            // NO MUTED BRUSH ON THIS LINE, deliberately. A selected row paints the accent ground and
            // the template hands its content the sunken ink; a description pinned to the muted token
            // would stay #98A3B2 on #5B9DD9 and measure 1.13:1 — the same partial-pairing defect
            // (TC2) that this whole change exists to remove, manufactured at authoring time.
            // Hierarchy comes from size and weight, which survive a ground change.
            var says = new TextBlock
            {
                Text = option.WhatItIs,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 1, 0, 0),
            };

            var stack = new StackPanel();
            stack.Children.Add(classId);
            stack.Children.Add(says);

            var row = new ListBoxItem { Content = stack, Tag = option.Id };
            AutomationProperties.SetName(row, $"{option.Id}. {option.WhatItIs}");
            taskClass.Items.Add(row);

            // THE MODEL'S VALUE SELECTS THE ROW, never the other way round (Ruling 72): the dialog
            // reflects the declared default; it does not supply one.
            if (string.Equals(option.Id, sheet.TaskClass, StringComparison.Ordinal))
            {
                taskClass.SelectedItem = row;
            }
        }

        // Ruling 56 — the fan-out ceiling, prefilled from the ruled default; a number, typed.
        var ceiling = new TextBox
        {
            Text = sheet.FanOutCeiling?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Padding = new Thickness(8, 6, 8, 6),
            MinWidth = 80,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        AutomationProperties.SetName(ceiling, "Fan-out ceiling");

        // Ruling 72 (a) — THE BUDGET IS A STATE. No number is required; the cap is an affordance the
        // operator opts into, and its two boxes exist only once they have.
        var budgetState = new TextBlock { TextWrapping = TextWrapping.Wrap };
        budgetState.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var enforceCap = new CheckBox { Content = "Enforce a cap", Margin = new Thickness(0, 4, 0, 0) };
        AutomationProperties.SetName(enforceCap, "Enforce a cap");

        var capRow = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            Margin = new Thickness(0, 4, 0, 0),
            Visibility = Visibility.Collapsed,
        };
        var requests = new TextBox { Padding = new Thickness(8, 6, 8, 6), MinWidth = 90 };
        AutomationProperties.SetName(requests, "Cap: requests");
        var tokens = new TextBox { Padding = new Thickness(8, 6, 8, 6), MinWidth = 120, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetName(tokens, "Cap: tokens");
        capRow.Children.Add(Label("requests"));
        capRow.Children.Add(requests);
        capRow.Children.Add(Label("tokens"));
        capRow.Children.Add(tokens);

        // RQ4 — REQUIRED-AND-UNDEFAULTED IS A VISIBLE STATE, carried by a glyph and a word as well
        // as by colour, and flipping to Answered. Never a bare asterisk.
        var taskClassState = new TextBlock { FontSize = 12, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

        var backends = new StackPanel();
        AutomationProperties.SetName(backends, "Agent backends");

        // RQ5 — THE REASON TRAVELS WITH THE BUTTON. This used to render in a footnote UNDER the
        // buttons, in the vocabulary of the ranking subsystem, about 200px from the field it was
        // about. It now sits in the same row as the disabled Create, in the amber that means
        // "waiting on you".
        var blocked = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
        };
        blocked.SetResourceReference(TextBlock.ForegroundProperty, "InferredBrush");

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
            sheet.TaskClass = (taskClass.SelectedItem as ListBoxItem)?.Tag as string;
            sheet.FanOutCeiling = int.TryParse(
                ceiling.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;

            taskClassState.Text = sheet.TaskClassAnswered
                ? "\u2713 " + TaskClassVocabulary.AnsweredLabel
                : "\u26a0 " + TaskClassVocabulary.RequiredLabel;
            taskClassState.SetResourceReference(
                TextBlock.ForegroundProperty,
                sheet.TaskClassAnswered ? "VerifiedBrush" : "InferredBrush");

            budgetState.Text = sheet.BudgetDisplay;

            create.IsEnabled = sheet.CanCreate;
            blocked.Text = sheet.BlockedReason ?? string.Empty;
            blocked.Visibility = sheet.CanCreate ? Visibility.Collapsed : Visibility.Visible;
        }

        void ApplyCap()
        {
            // The cap exists only while the box is ticked AND both numbers parse positive; anything
            // else is the subscription-bounded state again, said so on the line, never a silent zero.
            capRow.Visibility = enforceCap.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            if (enforceCap.IsChecked == true
                && int.TryParse(requests.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var r)
                && long.TryParse(tokens.Text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var t)
                && r > 0
                && t > 0)
            {
                sheet.EnforceCap(new AiDe.Core.AgentPlane.RunBudget(r, t));
            }
            else
            {
                sheet.ClearCap();
            }

            Reflect();
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
        taskClass.SelectionChanged += (_, _) => Reflect();
        ceiling.TextChanged += (_, _) => Reflect();
        enforceCap.Checked += (_, _) => ApplyCap();
        enforceCap.Unchecked += (_, _) => ApplyCap();
        requests.TextChanged += (_, _) => ApplyCap();
        tokens.TextChanged += (_, _) => ApplyCap();
        create.Click += (_, _) => onCreate();

        var actions = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        actions.Children.Add(create);
        actions.Children.Add(cancel);

        var buttons = new DockPanel { Margin = new Thickness(0, 14, 0, 0), LastChildFill = false };
        DockPanel.SetDock(actions, Dock.Right);
        buttons.Children.Add(actions);
        DockPanel.SetDock(blocked, Dock.Right);
        buttons.Children.Add(blocked);

        // RQ3 — THE EXPLANATION SITS AT THE FIELD, above the control, before the answer is needed.
        var taskClassHeading = new DockPanel { Margin = new Thickness(0, 12, 0, 4) };
        var taskClassLabel = Label("Task class");
        taskClassLabel.Margin = new Thickness(0);
        taskClassHeading.Children.Add(taskClassLabel);
        taskClassHeading.Children.Add(taskClassState);

        var body = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(Label("Name"));
        body.Children.Add(name);
        body.Children.Add(taskClassHeading);
        body.Children.Add(Muted(TaskClassVocabulary.Explanation));
        body.Children.Add(taskClass);
        body.Children.Add(Label("Fan-out ceiling"));
        body.Children.Add(ceiling);
        body.Children.Add(Label("Budget"));
        body.Children.Add(budgetState);
        body.Children.Add(enforceCap);
        body.Children.Add(capRow);
        // Rulings 42 and 73: a sentence, never a Lease. A lease is derived per prompt from the
        // operator's mentions; a prompt that names none runs read-only.
        body.Children.Add(Label("Lease"));
        body.Children.Add(Muted(NewSessionSheetViewModel.LeaseDisplay));
        body.Children.Add(Label("Agent backends"));
        body.Children.Add(backends);
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
            VerticalAlignment = VerticalAlignment.Center,
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
