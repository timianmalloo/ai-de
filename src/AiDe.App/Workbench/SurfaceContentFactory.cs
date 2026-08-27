using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using AiDe.Core.Presentation;
using AiDe.Core.Projections;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Builds the content for one surface.
/// </summary>
/// <remarks>
/// The workbench does not know what a surface renders and must not: a surface's identity, state and
/// content are independent of where it is docked (US-9). This factory is the single place that
/// mapping lives, so adding a surface kind never means touching the layout model.
/// </remarks>
public sealed class SurfaceContentFactory(ProjectionService? projections)
{
    /// <summary>Surface kinds this factory can build. An unknown kind still gets an honest pane.</summary>
    public static IReadOnlyList<string> KnownKinds { get; } = ["view", "inspector", "terminal"];

    public FrameworkElement Create(Surface surface)
    {
        var content = surface.Kind switch
        {
            "view" or "inspector" when projections is not null => EvidenceContent(surface),
            "terminal" => Terminal(surface),
            _ => Unavailable(surface),
        };

        // Every surface carries its title into the accessibility tree in its own right, not only via
        // its tab — a screen-reader user who moves focus into the pane must still know where they are.
        AutomationProperties.SetName(content, surface.Title);
        return content;
    }

    private FrameworkElement EvidenceContent(Surface surface)
    {
        var pane = new EvidencePaneViewModel(projections!);
        pane.Load();

        var list = new ListBox
        {
            ItemsSource = pane.Rows,
            DisplayMemberPath = nameof(EvidenceRow.DisplayLabel),
            BorderThickness = new Thickness(0),
            Background = null,
        };
        AutomationProperties.SetName(list, $"{surface.Title} items");

        var status = new TextBlock
        {
            Text = pane.StatusMessage,
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        };
        status.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");

        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(list);
        stack.Children.Add(status);
        return stack;
    }

    /// <summary>A live terminal: a real ConPTY session, drawn by the real renderer.</summary>
    /// <remarks>
    /// Replaces the Phase-1b placeholder. The surface owns the session's lifetime, so the factory
    /// hands one back and does not keep it — a pane that is closed disposes what it built.
    /// </remarks>
    private static FrameworkElement Terminal(Surface surface) =>
        new TerminalSurface(surface.SurfaceId, surface.Title);

    private static FrameworkElement Unavailable(Surface surface)
    {
        var text = new TextBlock
        {
            Text = $"“{surface.Title}” is not available in this build.",
            Margin = new Thickness(12),
            TextWrapping = TextWrapping.Wrap,
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        return text;
    }
}
