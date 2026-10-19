using System.Windows;
using System.Windows.Controls;

namespace AiDe.App.Workbench.Execution;

/// <summary>
/// One attempt's execution (the WL-S design's §4): identity <c>execution:&lt;DispatchKey&gt;</c>, caption
/// <i>Execution — &lt;attempt&gt;</i>, one per attempt, in the Bottom zone. The attempt is the host's record; this
/// surface only shows it.
/// </summary>
public sealed class ExecutionSurface : ContentControl
{
    /// <summary>The surface kind — <c>execution</c>; the row is <see cref="SurfaceContentFactory.Kinds"/>'s.</summary>
    public const string Kind = "execution";

    /// <summary>The execution surface id for an attempt: <c>execution:&lt;dispatchKey&gt;</c>.</summary>
    public static string SurfaceIdFor(string dispatchKey) => Kind + ":" + dispatchKey;

    /// <summary>The caption, which is also the UIA name (<see cref="SurfaceContentFactory.Create"/> names every surface by its title): <i>Execution — &lt;attempt&gt;</i>.</summary>
    public static string CaptionFor(string dispatchKey) => "Execution — " + dispatchKey;

    public ExecutionSurface(string caption)
    {
        var text = new TextBlock { Text = caption, Margin = new Thickness(12), TextWrapping = TextWrapping.Wrap };
        text.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        Content = text;
    }
}
