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
public sealed class SurfaceContentFactory(IWorkspaceQueries? queries)
{
    /// <summary>Surface kinds this factory can build. An unknown kind still gets an honest pane.</summary>
    public static IReadOnlyList<string> KnownKinds { get; } = ["view", "inspector", "terminal", "canvas", "contexts", "joins"];

    public FrameworkElement Create(Surface surface)
    {
        var content = surface.Kind switch
        {
            "view" or "inspector" when queries is not null => EvidenceContent(surface),
            "terminal" => Terminal(surface),
            "canvas" => new CanvasSurface(surface.SurfaceId, surface.Title),
            "contexts" => new ContextMapSurface(surface.Title),
            "joins" => new JoinSurface(surface.Title),
            _ => Unavailable(surface),
        };

        // Every surface carries its title into the accessibility tree in its own right, not only via
        // its tab — a screen-reader user who moves focus into the pane must still know where they are.
        AutomationProperties.SetName(content, surface.Title);
        return content;
    }

    private FrameworkElement EvidenceContent(Surface surface)
    {
        var pane = new EvidencePaneViewModel(queries!);

        var list = new ListBox
        {
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

        // Started, not awaited — a factory that blocked on a pipe round trip would freeze the window
        // while a pane is being built. The pane shows its Loading state until the answer arrives.
        //
        // The controls MUST be updated when it does. An earlier revision bound `pane.Rows` and
        // `pane.StatusMessage` at construction and left it there: the load replaces `Rows` with a
        // new list and `Rows` is not observable, so the pane sat on "Loading evidence…" forever.
        // Every test passed — the pane view model was correct, and nothing asserted on what the
        // control showed. Found by running the application.
        _ = LoadInto(pane, list, status);

        return stack;
    }

    /// <summary>Loads the pane and pushes the result into its controls, on the UI thread.</summary>
    /// <remarks>
    /// <para><b>Failure is shown because the pane reports it, and this shows what the pane says.</b>
    /// An unreachable workspace becomes the pane's error state, and pushing that text into the
    /// control is what stops it presenting as merely slow.</para>
    ///
    /// <para>Marshalled explicitly because the continuation runs wherever the IPC round trip
    /// completed, and touching a WPF control from that thread throws.</para>
    /// </remarks>
    private static async Task LoadInto(EvidencePaneViewModel pane, ListBox list, TextBlock status)
    {
        try
        {
            await pane.LoadAsync();
        }
        catch (OperationCanceledException)
        {
            // The ONLY thing that escapes LoadAsync. The pane catches everything else itself and
            // degrades to an explicit error state with its own message, which the update below then
            // shows.
            //
            // An earlier revision also caught the general case here and wrote its own message. That
            // branch was unreachable — mutation proved it, by deleting it and failing nothing — and
            // a control that cannot fire reads as protection while providing none (DC-016).
            return;
        }

        await list.Dispatcher.InvokeAsync(() =>
        {
            list.ItemsSource = pane.Rows;
            status.Text = pane.StatusMessage;
        });
    }

    /// <summary>A live terminal: a real ConPTY session, drawn by the real renderer.</summary>
    /// <remarks>
    /// Replaces the Phase-1b placeholder. The surface owns the session's lifetime, so the factory
    /// hands one back and does not keep it — a pane that is closed disposes what it built.
    /// </remarks>
    private static FrameworkElement Terminal(Surface surface) =>
        // An "agent:<exe>" surface id carries which executable this pane runs, so the layout — which
        // is persisted — remembers it. Storing it anywhere else would restore an agent pane as a
        // shell after a restart.
        new TerminalSurface(surface.SurfaceId, surface.Title)
        {
            Executable = surface.SurfaceId.StartsWith("agent:", StringComparison.Ordinal)
                ? surface.SurfaceId["agent:".Length..].Split('#')[0]
                : null,
        };

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
