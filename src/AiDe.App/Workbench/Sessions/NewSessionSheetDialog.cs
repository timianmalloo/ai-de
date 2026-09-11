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
/// Create button stays disabled, with its reason beside it, until the operator chooses one.</para>
///
/// <para><b>It is a picker now, not a text box (RQ1).</b> The requirement was never the failure; the
/// control was. A value whose only use is exact equality against a set is entered by choosing from
/// that set, because a free text box makes a typo indistinguishable from an answer. Nothing is
/// preselected, so choosing is still an act and the no-default contract is untouched.</para>
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
        // NOTHING IS PRESELECTED (SelectedIndex stays -1), so the type-level no-default contract and
        // the reflective test that pins it both still hold: choosing from a set is not the same as
        // being given one.
        var taskClass = new ListBox
        {
            SelectedIndex = -1,
            Padding = new Thickness(2),
            MaxHeight = 190,
        };
        AutomationProperties.SetName(taskClass, "Task class (required, no default)");

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
        }

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

            taskClassState.Text = sheet.TaskClassAnswered
                ? "\u2713 " + TaskClassVocabulary.AnsweredLabel
                : "\u26a0 " + TaskClassVocabulary.RequiredLabel;
            taskClassState.SetResourceReference(
                TextBlock.ForegroundProperty,
                sheet.TaskClassAnswered ? "VerifiedBrush" : "InferredBrush");

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
        taskClass.SelectionChanged += (_, _) => Reflect();
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
        // Ruling 42: a sentence, never a Lease. There is nothing at sheet time to derive one from,
        // and a derived "everything" would have travelled out of the sheet into a run.
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
