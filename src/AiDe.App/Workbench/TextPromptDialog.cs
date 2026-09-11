using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AiDe.App.Workbench;

/// <summary>
/// A minimal modal text prompt — used for renaming a terminal tab. Returns the entered text on OK
/// (Enter), or null on Cancel (Escape). Deliberately tiny and dependency-free; it reuses the app's
/// tokens so it reads as part of the shell rather than a bare Windows dialog.
/// </summary>
public static class TextPromptDialog
{
    /// <summary>Shows the prompt modally and returns the text, or null if cancelled.</summary>
    public static string? Show(string title, string initial, Window? owner)
    {
        string? result = null;

        // Every brush here used to be looked up by key with a HARD-CODED COLOUR AS THE FALLBACK -
        // a second copy of the palette that drifts on its own, and a failed token lookup rendering
        // as a plausible colour instead of as a failure (C3/TC5). The implicit defaults in App.xaml
        // now carry all of it, so the control that needs no styling has none.
        var box = new TextBox
        {
            Text = initial,
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 14,
            BorderThickness = new Thickness(1),
        };

        var window = DarkCaption.CreateDialog(title, owner, width: 360);

        var ok = new Button { Content = "Rename", IsDefault = true, MinWidth = 80, Margin = new Thickness(8, 0, 0, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80 };

        ok.Click += (_, _) =>
        {
            result = box.Text?.Trim();
            window.DialogResult = true;
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);

        var label = new TextBlock { Text = title, Margin = new Thickness(0, 0, 0, 8) };
        label.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(label);
        panel.Children.Add(box);
        panel.Children.Add(buttons);
        window.Content = panel;

        window.Loaded += (_, _) =>
        {
            box.SelectAll();
            box.Focus();
        };
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                result = box.Text?.Trim();
                window.DialogResult = true;
            }
        };

        return window.ShowDialog() == true ? result : null;
    }
}
