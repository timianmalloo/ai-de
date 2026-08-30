using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        var box = new TextBox
        {
            Text = initial,
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 14,
            Background = Brush("SurfaceSunkenBrush", Color.FromRgb(0x0D, 0x10, 0x14)),
            Foreground = Brush("TextBrush", Color.FromRgb(0xE4, 0xE9, 0xEF)),
            BorderBrush = Brush("BorderBrush", Color.FromRgb(0x2A, 0x31, 0x3B)),
            BorderThickness = new Thickness(1),
            CaretBrush = Brush("AccentBrush", Color.FromRgb(0x5B, 0x9D, 0xD9)),
        };

        var window = new Window
        {
            Title = title,
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.ToolWindow,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Owner = owner,
            Background = Brush("SurfaceRaisedBrush", Color.FromRgb(0x1A, 0x1F, 0x26)),
        };

        var ok = new Button { Content = "Rename", IsDefault = true, MinWidth = 80, Margin = new Thickness(8, 0, 0, 0) };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 80 };
        if (TryFindStyle("RoundedButton", out var buttonStyle))
        {
            ok.Style = buttonStyle;
            cancel.Style = buttonStyle;
        }

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

        var label = new TextBlock
        {
            Text = title,
            Foreground = Brush("TextMutedBrush", Color.FromRgb(0x98, 0xA3, 0xB2)),
            Margin = new Thickness(0, 0, 0, 8),
        };

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

    private static SolidColorBrush Brush(string key, Color fallback) =>
        Application.Current?.TryFindResource(key) as SolidColorBrush ?? new SolidColorBrush(fallback);

    private static bool TryFindStyle(string key, out Style style)
    {
        if (Application.Current?.TryFindResource(key) is Style found)
        {
            style = found;
            return true;
        }

        style = null!;
        return false;
    }
}
