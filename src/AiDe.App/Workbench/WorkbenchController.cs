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

    /// <summary>
    /// Asks the workspace to re-index itself. Set when a workspace attaches; null before that.
    /// </summary>
    /// <remarks>
    /// A delegate rather than a workspace handle: the controller's job is layout and command
    /// dispatch, and giving it something it could read evidence from would invite exactly that.
    /// </remarks>
    public Func<Task<string>>? WorkspaceRefresh { get; set; }

    /// <summary>
    /// Raised after a command that CHANGED what the store holds has finished.
    /// </summary>
    /// <remarks>
    /// <para><b>Indexing used to end at the announcement.</b> A re-index wrote 10,242 new assertions
    /// — the whole knowledge half of a real repository — and every open pane went on rendering the
    /// projection it had fetched when it loaded. The user re-indexed, watched the message say it had
    /// worked, and read a Knowledge count of 0 taken from a graph twenty-six seconds out of date.
    /// The store was right and the screen was wrong, which is the worst of the two.</para>
    ///
    /// <para>An event rather than a call into the panes: the controller dispatches commands and owns
    /// no surface. Who listens, and what re-reading costs them, belongs to whoever holds the pane.</para>
    /// </remarks>
    public event Action? WorkspaceDataChanged;

    /// <summary>
    /// Routes focus across the canvas boundary. Set when a graph canvas surface attaches.
    /// </summary>
    /// <remarks>
    /// <b>Null until the canvas exists, and that is a working state rather than a gap.</b> The
    /// command stays in the catalog and stays keyboard-reachable; with no canvas it refuses and says
    /// so, which is the same path a canvas that has not created its handle yet takes. Hiding the
    /// command instead would make "the graph cannot be focused" indistinguishable from "the graph
    /// does not exist", and a user who pressed the chord would get silence (<b>DC-011</b>).
    /// </remarks>
    public CanvasFocusRouter? CanvasFocus { get; set; }

    /// <summary>Runs a catalog command by id. Returns false when the id is unknown.</summary>
    public bool Execute(string commandId)
    {
        switch (commandId)
        {
            case "workbench.resetLayout":
                return ApplyAndAnnounce(new LayoutOperation.ResetToDefault());

            case "workspace.refresh":
                return RefreshWorkspace();

            case "workspace.reindexAll":
                return ReindexAll();

            case "workspace.indexSolution":
                return IndexSolution();

            case "workspace.diagnostics":
                return ShowDiagnostics();

            case "workspace.open":
                return OpenWorkspace();

            case "terminal.newAgent":
                return NewAgentTerminal();

            case "terminal.new":
                return NewTerminal();

            case "workbench.focusCanvas":
                return FocusCanvas();

            case "workbench.dispatchPrompt":
                return OpenPromptBar();

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

    // ── the pointer path (1b.10) ──────────────────────────────────────────────────────────

    private bool _lockRefusalAnnounced;

    /// <summary>The destination the in-flight drag currently points at, or null for none.</summary>
    public DropTarget? HoveredTarget { get; private set; }

    /// <summary>The rectangle the UI should highlight for <see cref="HoveredTarget"/>.</summary>
    public LayoutRect? HoveredPreview { get; private set; }

    /// <summary>
    /// Reports pointer movement during a drag. Resolves the destination and its preview from the
    /// SAME call, so the highlight the user sees and the drop that follows cannot disagree.
    /// </summary>
    /// <remarks>
    /// Announces only when the destination actually changes. Re-announcing on every mouse-move would
    /// flood a screen reader with the same sentence and drown everything else out.
    /// </remarks>
    /// <summary>
    /// Raised when a drag starts and when it ends, so an airspace-limited surface can stand aside.
    /// </summary>
    /// <remarks>
    /// ADR-0015: the windowed WebView2 is drawn by the OS above every WPF element in the same space,
    /// so a drop indicator over the canvas is simply not visible. For the duration of a drag the
    /// canvas is swapped for a still frame. The composition control fixes the airspace problem and
    /// then kills the process when a pane is floated, which is why this exists at all.
    /// </remarks>
    public event Action<bool>? DragStateChanged;

    private bool _dragging;

    private void SetDragging(bool dragging)
    {
        if (_dragging == dragging) return;
        _dragging = dragging;
        DragStateChanged?.Invoke(dragging);
    }

    public DropTarget? DragOver(IReadOnlyList<PaneHitBox> panes, LayoutPoint pointer)
    {
        // Raised on the FIRST DragOver rather than on a separate begin call: this is the earliest
        // moment the workbench knows a drag is happening, and a swap that starts later than the
        // drop indicator flickers the canvas back over it.
        SetDragging(true);

        var target = DropTargetResolver.Resolve(panes, pointer, service.IsLocked);

        // The locked case is handled BEFORE the change-detection below. Both a locked resolve and
        // the initial state are null, so comparing them first would short-circuit and leave the user
        // with silence — the same "unannounced no-op reads as a dead control" failure the keyboard
        // path already guards against.
        if (target is null)
        {
            var wasAnnounced = _lockRefusalAnnounced;
            HoveredTarget = null;
            HoveredPreview = null;
            _lockRefusalAnnounced = true;
            if (!wasAnnounced)
            {
                announcer.Announce("Layout is locked. Unlock to rearrange panes.");
            }

            return null;
        }

        _lockRefusalAnnounced = false;
        if (target == HoveredTarget)
        {
            return target;
        }

        HoveredTarget = target;

        var pane = panes.FirstOrDefault(p => p.StackId == target.TargetNodeId);
        HoveredPreview = target.Kind == DropKind.Float || pane.StackId is null
            ? null
            : DropTargetResolver.PreviewFor(pane, target);

        announcer.Announce(DropTargetResolver.Describe(target, TitleOf(target.TargetNodeId)));
        return target;
    }

    /// <summary>Commits the drag at the hovered destination. Returns false when there is none.</summary>
    public bool Drop(string surfaceId)
    {
        var target = HoveredTarget;
        CancelDrag();
        return target is not null && Move(surfaceId, target);
    }

    /// <summary>Abandons the drag with the layout untouched (Escape, or the pointer leaving).</summary>
    public void CancelDrag()
    {
        SetDragging(false);
        var had = HoveredTarget is not null;
        HoveredTarget = null;
        HoveredPreview = null;
        _lockRefusalAnnounced = false;
        if (had)
        {
            announcer.Announce("Move cancelled.");
        }
    }

    private string TitleOf(string stackId) =>
        service.Current.AllStacks().FirstOrDefault(s => s.Id == stackId)?.Active.Title ?? "the workbench";

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

    /// <summary>Starts a re-index and announces both the start and the outcome.</summary>
    /// <remarks>
    /// <para><b>Announced twice on purpose.</b> Re-indexing takes as long as it takes, and a command
    /// that acknowledged nothing until it finished would be indistinguishable from a key that did
    /// not register — so the operator is told it started, and told again what happened.</para>
    ///
    /// <para>With no workspace attached this still returns handled, and says so. A command in the
    /// palette that silently does nothing is the failure the catalog's conformance test exists to
    /// prevent (<b>DC-011</b>).</para>
    /// </remarks>
    /// <summary>
    /// <c>workbench.focusCanvas</c> — moves focus into the graph canvas, or says why it cannot.
    /// </summary>
    /// <remarks>
    /// Always returns true: the command IS handled, and its refusal is an outcome rather than a
    /// failure to dispatch. Returning false would make the palette treat a legitimate "the canvas is
    /// mid-drag" refusal as an unknown command.
    /// </remarks>
    /// <summary>
    /// Opens the prompt bar. Set when the shell builds one; null in a headless controller.
    /// </summary>
    public Action? PromptBarOpen { get; set; }

    /// <summary>
    /// Indexes the workspace's C# projects. Set when a workspace attaches; null before that.
    /// </summary>
    public Func<Task<string>>? WorkspaceIndex { get; set; }

    /// <summary>Re-reads every scope, ignoring the fingerprint cache.</summary>
    public Func<Task<string>>? WorkspaceReindexAll { get; set; }

    /// <summary>Reports daemon, health and MCP state. Set when a workspace attaches.</summary>
    public Func<string>? WorkspaceDiagnostics { get; set; }

    /// <summary>Chooses and opens a workspace. Set by the window that can show a folder picker.</summary>
    public Func<Task<string>>? WorkspaceOpen { get; set; }

    /// <summary>Opens a terminal running an agent CLI. Set by the shell that can create surfaces.</summary>
    public Func<string>? NewAgentTerminalRequested { get; set; }

    /// <summary>Opens a plain shell terminal (never an agent). Set by the shell that can create surfaces.</summary>
    public Func<string>? NewTerminalRequested { get; set; }

    private bool NewAgentTerminal()
    {
        announcer.Announce(NewAgentTerminalRequested is null
            ? "Agent terminals are not available in this build."
            : NewAgentTerminalRequested());

        return true;
    }

    private bool NewTerminal()
    {
        announcer.Announce(NewTerminalRequested is null
            ? "Terminals are not available in this build."
            : NewTerminalRequested());

        return true;
    }

    private bool OpenWorkspace()
    {
        if (WorkspaceOpen is null)
        {
            announcer.Announce("Opening a workspace is not available in this build.");
            return true;
        }

        _ = RunAndAnnounce(WorkspaceOpen);
        return true;
    }

    private bool ShowDiagnostics()
    {
        announcer.Announce(WorkspaceDiagnostics is null
            ? "No workspace is open, so there is nothing to report."
            : WorkspaceDiagnostics());

        return true;
    }

    private bool ReindexAll()
    {
        if (WorkspaceReindexAll is null)
        {
            announcer.Announce("No workspace is open to index.");
            return true;
        }

        // Says it is ignoring the cache, because the only reason to run this is that the user does
        // not trust the fast answer, and a command that looks identical to the fast one gives them
        // no way to tell which they got.
        announcer.Announce("Re-indexing every scope, ignoring the cache…");
        _ = RunAndAnnounce(WorkspaceReindexAll);
        return true;
    }

    private bool IndexSolution()
    {
        if (WorkspaceIndex is null)
        {
            announcer.Announce("No workspace is open to index.");
            return true;
        }

        announcer.Announce("Indexing C# projects…");
        _ = RunAndAnnounce(WorkspaceIndex);
        return true;
    }

    /// <summary>
    /// Runs a long command and announces whatever it reports, including a failure.
    /// </summary>
    /// <remarks>
    /// Fire-and-announce rather than awaited: the command returns to the palette immediately, and
    /// the outcome arrives on the announcement channel. A thrown exception is announced too — a
    /// command that started and then said nothing is indistinguishable from one that hung.
    /// </remarks>
    private async Task RunAndAnnounce(Func<Task<string>> work)
    {
        try
        {
            var outcome = await work();

            // Before the message, not after: the panes start re-reading while the user is still
            // reading what happened, so the number they look at next is the new one.
            WorkspaceDataChanged?.Invoke();
            announcer.Announce(outcome);
        }
        catch (Exception ex)
        {
            announcer.Announce($"Indexing failed: {ex.Message}");
        }
    }

    private bool OpenPromptBar()
    {
        if (PromptBarOpen is null)
        {
            announcer.Announce("Prompt dispatch is not available in this build.");
            return true;
        }

        PromptBarOpen();
        return true;
    }

    private bool FocusCanvas()
    {
        if (CanvasFocus is null)
        {
            announcer.Announce("The graph canvas is not ready.");
            return true;
        }

        announcer.Announce(CanvasFocus.Enter().Announcement);
        return true;
    }

    private bool RefreshWorkspace()
    {
        var refresh = WorkspaceRefresh;

        if (refresh is null)
        {
            announcer.Announce("No workspace is open, so there is nothing to re-index.");
            return true;
        }

        announcer.Announce("Re-indexing the workspace. The current evidence keeps rendering.");

        _ = Task.Run(async () =>
        {
            string outcome;

            try
            {
                outcome = await refresh();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                outcome = $"Re-indexing failed: {ex.Message}";
                announcer.Announce(outcome);
                return;
            }

            WorkspaceDataChanged?.Invoke();
            announcer.Announce(outcome);
        });

        return true;
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
