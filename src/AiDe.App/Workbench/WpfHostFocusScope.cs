using System.Windows;
using System.Windows.Input;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// <see cref="IHostFocusScope"/> over WPF's own focus system — the half of the crossing that WPF
/// <i>can</i> do.
/// </summary>
/// <remarks>
/// Only the canvas is unreachable by traversal. Once focus is back on the WPF side, moving it on is
/// ordinary <see cref="FrameworkElement.MoveFocus"/>, so this deliberately does not reimplement
/// traversal — it hands the direction to WPF and lets the normal tab order apply.
/// </remarks>
public sealed class WpfHostFocusScope(UIElement root) : IHostFocusScope
{
    private readonly UIElement _root = root ?? throw new ArgumentNullException(nameof(root));

    public object? Current => Keyboard.FocusedElement;

    public bool Restore(object target) =>
        target is IInputElement element && Keyboard.Focus(element) == element;

    public bool MoveNext(CanvasFocusDirection direction)
    {
        var request = new TraversalRequest(
            direction == CanvasFocusDirection.Backward
                ? FocusNavigationDirection.Previous
                : FocusNavigationDirection.Next);

        // From the root rather than from the canvas: the canvas is an HwndHost whose child window
        // is not in WPF's logical tree, so asking IT to move focus has nothing to move from.
        return _root.MoveFocus(request);
    }
}
