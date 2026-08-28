using System.Globalization;

namespace AiDe.Core.Workbench;

/// <summary>
/// One keyboard-reachable layout command, as the command palette lists it.
/// </summary>
/// <remarks>
/// This catalog is the machine-checkable form of SC 2.5.7: every operation reachable by dragging
/// must have a keyboard equivalent. Because both the palette and the conformance test read the same
/// list, an operation added without a command fails the suite instead of shipping mouse-only.
/// </remarks>
public sealed record WorkbenchCommand(
    string Id,
    string Title,
    string Gesture,
    string OperationKind,
    string Hint);

public static class WorkbenchCommandCatalog
{
    /// <summary>
    /// The commands, in palette order. Gestures follow Windows/Fluent conventions and deliberately
    /// avoid the Alt+&lt;letter&gt; menu-mnemonic space.
    /// </summary>
    public static IReadOnlyList<WorkbenchCommand> All { get; } =
    [
        new("workbench.moveSurface", "Move pane…", "Ctrl+K, M",
            nameof(LayoutOperation.MoveSurface),
            "Choose a destination with the arrow keys. Enter places it, Escape cancels."),

        new("workbench.resizePane", "Resize pane…", "Ctrl+K, R",
            nameof(LayoutOperation.ResizeSplit),
            "Select an edge, then adjust it with the arrow keys. Enter commits, Escape cancels."),

        new("workbench.floatPane", "Float pane", "Ctrl+K, F",
            nameof(LayoutOperation.SetStackState),
            "Detaches the pane into its own window."),

        new("workbench.collapsePane", "Collapse pane", "Ctrl+K, C",
            nameof(LayoutOperation.SetStackState),
            "Hides the pane; its surfaces stay reachable by name."),

        new("workbench.maximizePane", "Maximize pane", "Ctrl+K, Z",
            nameof(LayoutOperation.SetStackState),
            "Restoring returns the previous arrangement."),

        new("workbench.nextSurface", "Next tab in pane", "Ctrl+PageDown",
            nameof(LayoutOperation.ActivateSurface),
            "Moves to the next surface in the focused pane."),

        new("workbench.previousSurface", "Previous tab in pane", "Ctrl+PageUp",
            nameof(LayoutOperation.ActivateSurface),
            "Moves to the previous surface in the focused pane."),

        new("workbench.reorderSurface", "Move tab left/right", "Ctrl+Shift+PageUp/PageDown",
            nameof(LayoutOperation.ReorderSurface),
            "Reorders the surface within its pane."),

        new("workbench.closeSurface", "Close surface", "Ctrl+W",
            nameof(LayoutOperation.CloseSurface),
            "Closes the focused surface."),

        new("workbench.resetLayout", "Reset workbench layout", "Ctrl+K, Ctrl+R",
            nameof(LayoutOperation.ResetToDefault),
            "Returns to the default arrangement."),

        // Not a layout operation, so it carries no OperationKind: SC 2.5.7's conformance test asks
        // that every DRAGGABLE operation has a keyboard path, and focusing the canvas is not one.
        // It is here because WPF traversal cannot reach the canvas at all (spike S4), so without an
        // explicit command the graph is unreachable from the keyboard entirely.
        // Also not a layout operation. The receipt, not the send, is what this command exists to
        // surface — a prompt reaching an agent session cannot be taken back.
        // The command that makes the extractor visible. Its announcement carries the DISCLOSURES,
        // because a graph that silently omits package types looks complete and the user has no way
        // to know it is not.
        // Read-only on purpose. Upgrade and rollback are choreographed against a store a running
        // binary may not be able to read halfway through, so the shell REPORTS the state and names
        // what a rollback would do; the act itself stays with the Bootstrap.
        // Without this the daemon path was reachable only by setting an environment variable before
        // launch, which made indexing untestable by anyone who did not already know that.
        new("workspace.open", "Open a repository as a workspace…", "Ctrl+K, O",
            string.Empty,
            "Choose a folder. Its daemon is started if it is not already running, and its evidence becomes queryable."),

        new("workspace.diagnostics", "Show daemon, health and MCP diagnostics", "Ctrl+K, D",
            string.Empty,
            "Reports the daemon version, whether a rollback is possible, open health incidents, and the registered MCP tools."),

        new("workspace.indexSolution", "Index C# projects in this workspace", "Ctrl+K, I",
            string.Empty,
            "Finds every C# project and indexes one scope per target framework. Reports what was not analysed."),

        // The command that makes agent dispatch reachable at all. An agent session gets a readiness
        // watcher instead of shell integration, so it can be dispatched to rather than only refused.
        new("terminal.newAgent", "New agent terminal…", "Ctrl+K, A",
            // AddSurface's keyboard equivalent (SC 2.5.7). Declared rather than left empty: the
            // conformance test reflects over the operation union, and it caught this immediately.
            nameof(LayoutOperation.AddSurface),
            "Opens a terminal running an installed agent CLI. Prompts can be dispatched to it once it reaches its prompt."),

        new("workbench.dispatchPrompt", "Dispatch prompt to terminal…", "Ctrl+K, P",
            string.Empty,
            "Type a prompt and press Enter. The recorded delivery receipt is announced, including when delivery is unknown."),

        new("workbench.focusCanvas", "Focus graph canvas", "Ctrl+K, G",
            string.Empty,
            "Moves focus into the graph. Tab off either end or press Escape to come back."),

        new("workbench.toggleLock", "Lock/unlock layout", "Ctrl+K, L",
            OperationKind: "",   // a mode, not a tree mutation
            "Freezes the arrangement so a stray drag cannot change it."),

        // Not a layout command, and the first entry here that is not. Re-indexing is a WRITE that
        // crosses the daemon boundary, and it needs a keyboard-reachable trigger for the same reason
        // every layout operation does: an action available only by some other route is an action a
        // keyboard-first operator does not have.
        new("workspace.refresh", "Re-index this workspace", "Ctrl+K, Ctrl+I",
            OperationKind: "",   // ingestion, not a tree mutation
            "Asks the daemon to re-read the repository. The current evidence keeps rendering until "
            + "a complete snapshot replaces it."),
    ];

    public static IEnumerable<WorkbenchCommand> Search(string term) =>
        string.IsNullOrWhiteSpace(term)
            ? All
            : All.Where(c =>
                c.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Hint.Contains(term, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// The keyboard resize interaction, modelled after Eclipse's `Alt+-` → Size → arrows — the only
/// keyboard resize proven in any of the four exemplars.
/// </summary>
/// <remarks>
/// It is a small explicit state machine rather than a stream of resize operations because the user
/// must be able to **see which edge is selected** before moving it, and to **cancel back to where
/// they started**. Adjustments are applied live so the effect is visible, and Cancel restores the
/// layout captured on entry.
/// </remarks>
public sealed class KeyboardResizeSession(ILayoutService service)
{
    private Layout? _entryLayout;

    public bool IsActive { get; private set; }

    public string? SplitId { get; private set; }

    public int EdgeIndex { get; private set; }

    /// <summary>The step per arrow press, as a share of the split. Matches the mockup's declared increments.</summary>
    public double Step { get; init; } = 0.02;

    /// <summary>Enters resize mode on an edge and announces which edge is selected.</summary>
    public string Begin(string splitId, int edgeIndex, string edgeLabel)
    {
        _entryLayout = service.Current;
        IsActive = true;
        SplitId = splitId;
        EdgeIndex = edgeIndex;
        return $"Resize: {edgeLabel}. Arrow keys adjust. Enter commits, Escape cancels.";
    }

    /// <summary>Applies one arrow press. A refusal (minimum size) keeps the session open.</summary>
    public LayoutResult Adjust(int direction)
    {
        if (!IsActive || SplitId is null)
        {
            return new LayoutResult(service.Current, false, LayoutErrorCodes.InvalidTarget,
                "Not resizing.");
        }

        return service.Apply(new LayoutOperation.ResizeSplit(SplitId, EdgeIndex, Step * direction));
    }

    public string Commit()
    {
        IsActive = false;
        _entryLayout = null;
        SplitId = null;
        return "Resize committed.";
    }

    /// <summary>Abandons the resize and puts the layout back exactly as it was on entry.</summary>
    public string Cancel()
    {
        if (_entryLayout is not null)
        {
            service.Restore(_entryLayout);
        }

        IsActive = false;
        _entryLayout = null;
        SplitId = null;
        return "Resize cancelled.";
    }

    public string Describe() => IsActive
        ? string.Create(CultureInfo.InvariantCulture, $"Resizing edge {EdgeIndex + 1} of {SplitId}")
        : "Not resizing.";
}
