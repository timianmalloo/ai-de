using System.Windows;
using System.Windows.Input;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Routes every keyboard layout command through the model and announces the outcome.
/// </summary>
/// <remarks>
/// This is where SC 2.5.7 and SC 4.1.3 stop being design intent and become behaviour: a command
/// produces a <see cref="LayoutOperation"/> — the same type a pointer drag produces — applies it via
/// the single mutation path, and announces whatever came back, including a refusal. A refused
/// operation is announced too, because "nothing happened" is information the user needs and silence
/// is indistinguishable from a broken key.
/// </remarks>
public sealed class WorkbenchController(ILayoutService service, IWorkbenchAnnouncer announcer)
{
    private readonly KeyboardResizeSession _resize = new(service);

    /// <summary>The stack the user is working in. Layout commands act on this.</summary>
    public string? FocusedStackId { get; set; }

    /// <summary>The surface within the focused stack, when one is selected.</summary>
    public string? FocusedSurfaceId { get; set; }

    public bool IsResizing => _resize.IsActive;

    /// <summary>Runs a catalog command by id. Returns false when the id is unknown.</summary>
    public bool Execute(string commandId)
    {
        switch (commandId)
        {
            case "workbench.resetLayout":
                return ApplyAndAnnounce(new LayoutOperation.ResetToDefault());

            case "workbench.toggleLock":
                service.IsLocked = !service.IsLocked;
                announcer.Announce(service.IsLocked
                    ? "Layout is locked. Unlock to rearrange panes."
                    : "Layout unlocked.");
                return true;

            case "workbench.closeSurface":
                return FocusedSurfaceId is not null
                    && ApplyAndAnnounce(new LayoutOperation.CloseSurface(FocusedSurfaceId));

            case "workbench.floatPane":
                return WithFocusedStack(id =>
                    new LayoutOperation.SetStackState(id, StackState.Floating));

            case "workbench.collapsePane":
                return WithFocusedStack(id =>
                    new LayoutOperation.SetStackState(id, StackState.Collapsed));

            case "workbench.maximizePane":
                return WithFocusedStack(id =>
                {
                    var stack = service.Current.AllStacks().FirstOrDefault(s => s.Id == id);
                    // One key toggles, because a user who pressed maximize expects the same key to
                    // undo it rather than having to find a different command.
                    var next = stack?.State == StackState.Maximized
                        ? StackState.Docked
                        : StackState.Maximized;
                    return new LayoutOperation.SetStackState(id, next);
                });

            case "workbench.nextSurface":
                return CycleSurface(+1);

            case "workbench.previousSurface":
                return CycleSurface(-1);

            case "workbench.reorderSurface":
                return ReorderFocusedSurface(+1);

            case "workbench.resizePane":
                return BeginResize();

            case "workbench.moveSurface":
                announcer.Announce(
                    "Move pane: choose a destination with the arrow keys. Enter places it, Escape cancels.");
                return true;

            default:
                return false;
        }
    }

    /// <summary>Applies a move produced either by a keyboard destination choice or by a drop.</summary>
    public bool Move(string surfaceId, DropTarget target) =>
        ApplyAndAnnounce(new LayoutOperation.MoveSurface(surfaceId, target));

    // ── keyboard resize (the Eclipse pattern) ─────────────────────────────────────────────

    private bool BeginResize()
    {
        var split = service.Current.Walk().OfType<SplitNode>().FirstOrDefault();
        if (split is null)
        {
            announcer.Announce("There is nothing to resize — the workbench has a single pane.");
            return true;
        }

        var label = split.Orientation == Orientation.Horizontal ? "vertical divider" : "horizontal divider";
        announcer.Announce(_resize.Begin(split.Id, 0, label));
        return true;
    }

    /// <summary>Handles an arrow/Enter/Escape while a resize is in flight. Returns true if consumed.</summary>
    public bool HandleResizeKey(Key key)
    {
        if (!_resize.IsActive)
        {
            return false;
        }

        switch (key)
        {
            case Key.Left or Key.Up:
                announcer.Announce(_resize.Adjust(-1).Announcement);
                return true;
            case Key.Right or Key.Down:
                announcer.Announce(_resize.Adjust(+1).Announcement);
                return true;
            case Key.Enter:
                announcer.Announce(_resize.Commit());
                return true;
            case Key.Escape:
                announcer.Announce(_resize.Cancel());
                return true;
            default:
                return false;
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves the focused surface one position within its pane, wrapping at the ends.
    /// </summary>
    /// <remarks>
    /// Wrapping rather than stopping: a keyboard user repeating the key should be able to reach any
    /// position without having to know which end they are at.
    /// </remarks>
    public bool ReorderFocusedSurface(int direction)
    {
        var stack = FocusedStackId is null
            ? null
            : service.Current.AllStacks().FirstOrDefault(s => s.Id == FocusedStackId);
        if (stack is null || stack.Surfaces.Count < 2)
        {
            announcer.Announce("This pane has a single surface, so there is nothing to reorder.");
            return true;
        }

        var from = stack.ActiveIndex;
        var to = (from + direction + stack.Surfaces.Count) % stack.Surfaces.Count;
        return ApplyAndAnnounce(new LayoutOperation.ReorderSurface(stack.Id, from, to));
    }

    private bool CycleSurface(int direction)
    {
        var stack = FocusedStackId is null
            ? null
            : service.Current.AllStacks().FirstOrDefault(s => s.Id == FocusedStackId);
        if (stack is null || stack.Surfaces.Count < 2)
        {
            announcer.Announce("This pane has a single surface.");
            return true;
        }

        var next = (stack.ActiveIndex + direction + stack.Surfaces.Count) % stack.Surfaces.Count;
        return ApplyAndAnnounce(new LayoutOperation.ActivateSurface(stack.Surfaces[next].SurfaceId));
    }

    private bool WithFocusedStack(Func<string, LayoutOperation> build)
    {
        if (FocusedStackId is null)
        {
            announcer.Announce("No pane is focused.");
            return true;
        }

        return ApplyAndAnnounce(build(FocusedStackId));
    }

    private bool ApplyAndAnnounce(LayoutOperation operation)
    {
        var result = service.Apply(operation);
        // Refusals are announced too: an unannounced no-op is indistinguishable from a dead key.
        announcer.Announce(result.Announcement);
        return true;
    }

    /// <summary>Binds the catalog's gestures to this controller on a WPF element.</summary>
    public void Bind(UIElement host)
    {
        foreach (var command in WorkbenchCommandCatalog.All)
        {
            var routed = new RoutedUICommand(command.Title, command.Id, typeof(WorkbenchController));
            var id = command.Id;
            host.CommandBindings.Add(new CommandBinding(routed, (_, e) =>
            {
                Execute(id);
                e.Handled = true;
            }));

            foreach (var gesture in KeyGestures.For(command))
            {
                host.InputBindings.Add(new KeyBinding(routed, gesture));
            }
        }

        host.PreviewKeyDown += (_, e) =>
        {
            if (HandleResizeKey(e.Key))
            {
                e.Handled = true;
            }
        };
    }
}

/// <summary>
/// Maps a catalog gesture string to WPF key gestures.
/// </summary>
/// <remarks>
/// Chorded gestures (`Ctrl+K, R`) are not expressible as a single WPF <see cref="KeyGesture"/>. They
/// are surfaced in the palette — which is the accessible, discoverable path SC 2.5.7 actually
/// requires — and only the single-chord gestures get a direct binding. Pretending a chord was bound
/// when it was not would be a worse failure than binding fewer keys.
/// </remarks>
internal static class KeyGestures
{
    internal static IEnumerable<KeyGesture> For(WorkbenchCommand command)
    {
        switch (command.Id)
        {
            case "workbench.nextSurface":
                yield return new KeyGesture(Key.PageDown, ModifierKeys.Control);
                break;
            case "workbench.previousSurface":
                yield return new KeyGesture(Key.PageUp, ModifierKeys.Control);
                break;
            case "workbench.closeSurface":
                yield return new KeyGesture(Key.W, ModifierKeys.Control);
                break;
            default:
                yield break;   // chorded — reachable through the command palette
        }
    }
}
