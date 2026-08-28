using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.Core;
using AiDe.Core.Ipc;
using AiDe.Core.Projections;
using AiDe.Core.Health;
using AiDe.Core.Upgrade;
using AiDe.Core.Extraction;
using AiDe.Core.Presentation;
using AiDe.Core.Terminal;
using AiDe.Core.Workbench;
using AvalonDock;
using AiDe.Core.Dispatch;
using AiDe.Core.Facts;

namespace AiDe.App.Workbench;

/// <summary>
/// The composition root for the workbench: model, adapter, controller, announcer and the docking
/// host, assembled and wired.
/// </summary>
/// <remarks>
/// This is the E10 reachability piece. Everything in Phase 1b was built and tested but unreachable —
/// the window still showed the superseded fixed grid, so a user could not touch any of it. A
/// capability nobody can open is not delivered.
///
/// Composition happens in one place on purpose: the live region, the controller and the adapter must
/// share the same <see cref="ILayoutService"/> instance, or the keyboard would mutate one layout
/// while the view rendered another.
/// </remarks>
public sealed class WorkbenchShell : IDisposable
{
    private SurfaceContentFactory _factory;
    private IWorkspaceQueries? _queries;
    private string? _workspaceRoot;
    private CanvasGraphViewModel? _canvasGraph;

    public WorkbenchShell(IWorkspaceQueries? queries, string? workspaceDataDirectory = null)
    {
        Service = new LayoutService();

        LiveRegion = new TextBlock { TextWrapping = TextWrapping.Wrap };
        LiveRegion.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        Announcer = new WorkbenchAnnouncer(LiveRegion);

        _queries = queries;
        _factory = new SurfaceContentFactory(queries);
        Manager = new DockingManager();
        AutomationProperties.SetName(Manager, "Workbench");

        // Indirected through a field so the workspace can arrive AFTER the window exists. The
        // shell is built synchronously and shown immediately; reaching a daemon may take a cold
        // start, and a window that appears only once a process has launched looks like a failure to
        // launch.
        Adapter = new WorkbenchAdapter(Manager, Service, surface => _factory.Create(surface));
        Controller = new WorkbenchController(Service, Announcer);

        Palette = new CommandPalette(Controller, Announcer);
        Prompt = new PromptBar(Announcer);

        Controller.NewAgentTerminalRequested = () =>
        {
            var agent = TerminalSurface.AvailableAgents.FirstOrDefault();
            if (agent is null)
            {
                // Named rather than a generic failure: the fix is to install one, and the user
                // cannot act on "not available".
                return "No agent CLI was found on PATH. Looked for: "
                    + string.Join(", ", TerminalSurface.Profiles.All.Select(p => p.Agent)) + ".";
            }

            var terminalStack = Service.Current.AllStacks()
                .FirstOrDefault(s => s.Surfaces.Any(su => su.Kind == "terminal"));

            if (terminalStack is null) return "There is no terminal pane to open it beside.";

            var id = $"agent:{agent}#{Guid.NewGuid().ToString("N")[..6]}";
            var result = Service.Apply(new LayoutOperation.AddSurface(
                terminalStack.Id, new Surface(id, "terminal", agent)));

            Adapter.Render();
            BindCanvas();
            BindContexts();
            BindJoins();

            return result.Applied
                ? $"{agent} terminal opened. Dispatch is refused until it reaches its prompt."
                : result.Announcement;
        };
        Controller.PromptBarOpen = Prompt.Open;

        // Persistence is per workspace and lives beside the fact store (ADR-0013). With no workspace
        // open there is nothing to persist against, so first-run simply starts from the default.
        //
        // The directory is passed in rather than read off a core: the shell no longer holds one.
        // Layout is the SHELL's state, not the workspace's — it stays local even when the evidence
        // it arranges is answered by another process.
        if (!string.IsNullOrEmpty(workspaceDataDirectory))
        {
            var surfaces = Service.Current.AllStacks()
                .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId)
                .ToHashSet(StringComparer.Ordinal);

            // Readiness markers are workspace state, like the layout: the agent a team runs and the
            // prompt it draws are properties of the repository being worked on, not of the machine.
            TerminalSurface.Profiles = AgentReadinessProfiles.Load(workspaceDataDirectory);
            foreach (var problem in TerminalSurface.Profiles.Problems)
            {
                // A marker that could not be compiled is announced, never absorbed. Absorbing it
                // would leave the user tuning a file that is not in force.
                Announcer.Announce(problem);
            }

            Persistence = new LayoutPersistence(
                Service, Path.Combine(workspaceDataDirectory, "layout.json"), surfaces,
                restorableKinds: SurfaceContentFactory.KnownKinds.ToHashSet(StringComparer.Ordinal));

            var restored = Persistence.Restore();
            if (restored.ErrorCode is not null || restored.WasDefaulted)
            {
                // A partial or failed restore must be told to the user, not silently absorbed: they
                // are about to look at an arrangement that is not the one they left.
                Announcer.Announce(restored.Announcement);
            }
        }

        Adapter.Render();
        TrackFocusedPane();
        PersistOnEveryChange();
    }

    public ILayoutService Service { get; }

    public DockingManager Manager { get; }

    public WorkbenchAdapter Adapter { get; }

    public WorkbenchController Controller { get; }

    public IWorkbenchAnnouncer Announcer { get; }

    /// <summary>The polite live region announcements are written to; also the visible status text.</summary>
    public TextBlock LiveRegion { get; }

    /// <summary>The keyboard route to every layout command (SC 2.5.7).</summary>
    public CommandPalette Palette { get; }

    /// <summary>Stages a prompt for the focused terminal and reports the delivery receipt.</summary>
    public PromptBar Prompt { get; }

    /// <summary>Saves and restores the arrangement across restarts. Null on first run.</summary>
    public LayoutPersistence? Persistence { get; private set; }

    /// <summary>Binds keyboard commands and the palette to a host element — normally the window.</summary>
    public void Bind(UIElement host)
    {
        Controller.Bind(host);

        // The palette must intercept BEFORE the workbench sees the key, or Up/Down would move the
        // pane selection underneath it while the user is choosing a command.
        host.PreviewKeyDown += (_, e) =>
        {
            // Before the palette: both are overlay surfaces, and a prompt containing the word the
            // palette filters on must reach the prompt box rather than the command list.
            if (Prompt.HandleKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (Palette.HandleKey(e.Key))
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.P && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                Palette.Open();
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// Keeps the controller's notion of "the focused pane" in step with real focus.
    /// </summary>
    /// <remarks>
    /// Layout commands act on the focused pane, so a controller whose idea of focus drifts from the
    /// user's would apply commands to the wrong pane — silently, and only sometimes, which is the
    /// worst shape of bug. Derived from the actual focus event rather than tracked by hand.
    /// </remarks>
    private void TrackFocusedPane()
    {
        Manager.GotKeyboardFocus += (_, e) =>
        {
            if (e.NewFocus is not DependencyObject element)
            {
                return;
            }

            var surfaceId = FindSurfaceId(element);
            if (surfaceId is null)
            {
                return;
            }

            var stack = Service.Current.FindStackOf(surfaceId);
            Controller.FocusedSurfaceId = surfaceId;
            Controller.FocusedStackId = stack?.Id;
        };
    }

    /// <summary>Walks up from a focused element to the surface it belongs to.</summary>
    internal static string? FindSurfaceId(DependencyObject element)
    {
        var current = element;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: AvalonDock.Layout.LayoutContent content }
                && !string.IsNullOrEmpty(content.ContentId))
            {
                return content.ContentId;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    /// <summary>
    /// Marks the layout dirty after any change, so nothing has to remember to save.
    /// </summary>
    /// <remarks>
    /// Hooked to the adapter's render rather than to individual commands: a save triggered per
    /// command would miss changes made by a drag, and every new operation would need to remember to
    /// opt in. One hook downstream of all of them cannot be forgotten.
    /// </remarks>
    private void PersistOnEveryChange()
    {
        if (Persistence is null)
        {
            return;
        }

        Manager.LayoutUpdated += (_, _) => Persistence.MarkDirty();
    }

    /// <summary>
    /// Points the shell at a workspace that became available after it was built.
    /// </summary>
    /// <remarks>
    /// Panes already on screen are re-rendered, because a pane showing "not available in this build"
    /// after the workspace opened is worse than one that never claimed anything.
    /// </remarks>
    public void AttachWorkspace(
        IWorkspaceQueries queries,
        string? dataDirectory,
        IWorkspaceCommands? commands = null,
        string scopeId = "fixture",
        string artifactRevision = "rev-1",
        string? workspaceRoot = null)
    {
        ArgumentNullException.ThrowIfNull(queries);

        _queries = queries;
        _factory = new SurfaceContentFactory(queries);

        // The palette's re-index command is inert until a workspace exists to re-index. Wiring it
        // here rather than at construction is what makes it act on THIS workspace.
        if (commands is not null)
        {
            Controller.WorkspaceRefresh = async () =>
            {
                var status = await commands.RefreshScopeAsync(
                    scopeId, artifactRevision, CancellationToken.None);

                return status.State == ScopeRefreshState.Completed
                    ? $"Re-indexed: {status.AssertionCount} assertion(s). Reopen a pane to see them."
                    : $"Re-indexing failed: {status.Failure}";
            };
        }

        // New terminals open in the workspace, not wherever the shell was launched from.
        _workspaceRoot = workspaceRoot;
        if (!string.IsNullOrEmpty(workspaceRoot)) TerminalSurface.WorkingDirectory = workspaceRoot;

        // Prompt dispatch: the shell owns the terminal, the daemon owns the receipt (D1), so the
        // choreography runs here with the two durable phases supplied by whoever holds the store.
        if (commands is IWorkspaceDispatch dispatch)
        {
            Prompt.Dispatch = async body =>
            {
                var surface = FocusedTerminal()
                    ?? throw new InvalidOperationException("focus a terminal pane before dispatching");

                var session = surface.Session
                    ?? throw new InvalidOperationException("the terminal has not started yet");

                var command = new DispatchCommand(
                    WorkspaceId: scopeId,
                    WorkspaceEpoch: await dispatch.EpochAsync(CancellationToken.None),
                    Caller: new CallerPrincipal(Environment.UserName, CallerKind.Shell),
                    CommandId: Guid.NewGuid().ToString("N"),
                    DraftId: $"draft-{session.SessionId}",
                    RevisionNo: 1,
                    Body: body,
                    SessionId: session.SessionId,
                    SessionGeneration: session.Generation);

                // Readiness decides whether this is attempted at all. A session that cannot report
                // when it is waiting for input may be showing a sign-in or a confirmation, and a
                // prompt sent into one of those is consumed by it.
                // The surface knows how ITS session reports readiness — OSC 133 for a shell, an
                // observed prompt marker for an agent, nothing otherwise — and an agent that has
                // not yet reached its prompt is refused rather than written into.
                var evidence = surface.ReadinessEvidence;
                var ready = evidence == ReadinessEvidence.ObservedPattern
                    ? surface.AgentReadiness?.IsReady == true
                    : session.Activity == SessionActivity.Ready;

                var readiness = evidence == ReadinessEvidence.None
                    ? SessionReadiness.Unknown
                    : ready ? SessionReadiness.Ready : SessionReadiness.NotReady;

                return await BoundaryDispatcher.BeginAndWriteAsync(
                    command, session, dispatch.DispatchBeginAsync, dispatch.DispatchFinalizeAsync,
                    CancellationToken.None, readiness);
            };
        }

        // Diagnostics needs no daemon connection: the installation layout is on disk and the
        // incident sidecar is a local file, so the state is readable even when the daemon is not.
        var diagnostics = new WorkspaceDiagnosticsViewModel(
            string.IsNullOrEmpty(dataDirectory) ? null : new DaemonInstallation(dataDirectory),
            string.IsNullOrEmpty(dataDirectory)
                ? null
                : new HealthIncidentSidecar(Path.Combine(dataDirectory, "health-incidents.jsonl")));

        Controller.WorkspaceDiagnostics = () => diagnostics.Read().Describe();

        if (commands is not null)
        {
            Controller.WorkspaceIndex = async () =>
                (await commands.IndexSolutionAsync(artifactRevision, CancellationToken.None)).Describe();
        }

        if (!string.IsNullOrEmpty(dataDirectory) && Persistence is null)
        {
            var surfaces = Service.Current.AllStacks()
                .SelectMany(stack => stack.Surfaces).Select(surface => surface.SurfaceId)
                .ToHashSet(StringComparer.Ordinal);

            // Readiness markers are workspace state, like the layout: the agent a team runs and the
            // prompt it draws are properties of the repository being worked on, not of the machine.
            TerminalSurface.Profiles = AgentReadinessProfiles.Load(dataDirectory);
            foreach (var problem in TerminalSurface.Profiles.Problems)
            {
                // A marker that could not be compiled is announced, never absorbed. Absorbing it
                // would leave the user tuning a file that is not in force.
                Announcer.Announce(problem);
            }

            Persistence = new LayoutPersistence(
                Service, Path.Combine(dataDirectory, "layout.json"), surfaces,
                restorableKinds: SurfaceContentFactory.KnownKinds.ToHashSet(StringComparer.Ordinal));

            var restored = Persistence.Restore();
            if (restored.ErrorCode is not null || restored.WasDefaulted)
            {
                Announcer.Announce(restored.Announcement);
            }
        }

        Adapter.Render();
        BindCanvas();
        BindContexts();
        BindJoins();
    }

    /// <summary>
    /// Connects the focus router to a canvas pane once one is on screen.
    /// </summary>
    /// <remarks>
    /// Called after every render rather than once: a canvas pane can be closed and reopened, and a
    /// router still holding the old surface would focus a control that is no longer in the tree.
    /// </remarks>
    /// <summary>Connects the context pane to the workspace's declared map, if there is one.</summary>
    internal void BindContexts()
    {
        var pane = Service.Current.AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Select(surface => Adapter.ContentFor(surface.SurfaceId))
            .OfType<ContextMapSurface>()
            .FirstOrDefault();

        if (pane is null || string.IsNullOrEmpty(_workspaceRoot) || _queries is null) return;

        pane.Source = () =>
        {
            // Validated against the symbols actually extracted, every refresh. A map validated once
            // at startup would keep drawing after the code it names had been renamed.
            var path = Path.Combine(_workspaceRoot, BoundedContextReader.DefaultRelativePath);
            var found = _queries.FindAsync(string.Empty, 20_000, CancellationToken.None)
                .GetAwaiter().GetResult();

            var symbols = found.Matches.Select(m => m.NodeId).ToList();
            var map = BoundedContextReader.Load(path, symbols);

            return new ContextProjection(map, ReadAssertions(found)).Compute();
        };

        // Choosing a context filters the graph to it. The two views already share colours; this is
        // what makes them one tool rather than two pictures of the same data.
        pane.ContextSelected -= OnContextSelected;
        pane.ContextSelected += OnContextSelected;

        pane.Refresh();
    }

    /// <summary>Every neighbour edge of every matched node, as assertions.</summary>
    /// <remarks>
    /// Shared by the context and join panes rather than read twice. Two reads of the same store
    /// would be two chances to disagree, and a context map and a join view that disagree about the
    /// same edge is a defect the user has no way to diagnose.
    /// </remarks>
    private List<AiDe.Core.Facts.EvidenceAssertion> ReadAssertions(AiDe.Core.Projections.FindResult found)
    {
        var assertions = new List<AiDe.Core.Facts.EvidenceAssertion>();
        if (_queries is null) return assertions;

        foreach (var match in found.Matches.Take(4000))
        {
            var describe = _queries.DescribeAsync(match.NodeId, 60, CancellationToken.None)
                .GetAwaiter().GetResult();

            assertions.AddRange(describe.Neighbors.Select(e => new AiDe.Core.Facts.EvidenceAssertion(
                "view", e.ArtifactRevision, e.Subject, e.Predicate, e.Object,
                e.Origin, e.Status, e.Provenance)));
        }

        return assertions;
    }

    /// <summary>Connects the joins pane to the workspace's own evidence.</summary>
    internal void BindJoins()
    {
        var pane = Service.Current.AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Select(surface => Adapter.ContentFor(surface.SurfaceId))
            .OfType<JoinSurface>()
            .FirstOrDefault();

        if (pane is null || _queries is null) return;

        pane.Source = () =>
        {
            var found = _queries.FindAsync(string.Empty, 20_000, CancellationToken.None)
                .GetAwaiter().GetResult();

            return new JoinProjection(ReadAssertions(found)).Compute();
        };

        pane.Refresh();
    }

    private void OnContextSelected(object? sender, string context)
    {
        var canvas = Service.Current.AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Select(surface => Adapter.ContentFor(surface.SurfaceId))
            .OfType<CanvasSurface>()
            .FirstOrDefault();

        if (canvas is null || _canvasGraph is null) return;

        // Toggling: choosing the context already shown clears the filter, so the way out is the same
        // gesture as the way in.
        _canvasGraph.ContextFilter =
            string.Equals(_canvasGraph.ContextFilter, context, StringComparison.Ordinal) ? null : context;

        Announcer.Announce(_canvasGraph.ContextFilter is null
            ? "Graph filter cleared."
            : $"Graph filtered to {context}.");

        _ = canvas.RefreshAsync();
    }

    internal void BindCanvas()
    {
        var canvas = Service.Current.AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Select(surface => Adapter.ContentFor(surface.SurfaceId))
            .OfType<CanvasSurface>()
            .FirstOrDefault();

        if (canvas is null)
        {
            Controller.CanvasFocus = null;
            return;
        }

        var router = new CanvasFocusRouter(canvas.FocusTarget, new WpfHostFocusScope(Manager));
        Controller.CanvasFocus = router;

        // The canvas reads the SAME projection every other pane does, rather than a graph-shaped
        // API of its own: two ways to ask what the graph contains is two answers that can disagree.
        var graph = new CanvasGraphViewModel(_queries);
        _canvasGraph = graph;

        // Nodes carry their declared context so the canvas can colour by it. Loaded once per bind
        // rather than per node: the map is a file, and re-reading it for every node in a graph would
        // make navigation cost scale with the map.
        if (!string.IsNullOrEmpty(_workspaceRoot))
        {
            var contexts = BoundedContextReader.Load(
                Path.Combine(_workspaceRoot, BoundedContextReader.DefaultRelativePath), []);

            if (contexts.Contexts.Count > 0)
            {
                graph.ContextLookup = id => contexts.Contexts
                    .FirstOrDefault(c => c.Includes.Any(p => BoundedContextReader.Matches(p, id)))?.Name;
            }
        }

        canvas.GraphSource = (rootId, ct) => graph.LoadAsync(rootId, cancellationToken: ct);

        canvas.FocusLeaveRequested += (_, direction) =>
            Announcer.Announce(router.Leave(direction).Announcement);

        // ADR-0015's snapshot swap, driven by the real drag rather than left as a method nothing
        // calls. While it is set the canvas also REFUSES focus and says why (P2-FOCUS-04), so the
        // two halves of "the canvas is standing aside" cannot drift apart.
        Controller.DragStateChanged -= canvas.SetObscured;
        Controller.DragStateChanged += canvas.SetObscured;
    }

    /// <summary>The terminal pane the user is working in, or null when none is focused.</summary>
    /// <remarks>
    /// Derived from the controller's focused surface rather than from a remembered handle: dispatch
    /// must go to the pane the user is looking at, and a cached reference goes stale the moment a
    /// pane is closed.
    /// </remarks>
    internal TerminalSurface? FocusedTerminal()
    {
        var focused = Controller.FocusedSurfaceId;
        if (focused is null) return null;

        return Manager.GetType() is not null
            ? FindTerminal(Adapter, focused)
            : null;

        static TerminalSurface? FindTerminal(WorkbenchAdapter adapter, string surfaceId) =>
            adapter.ContentFor(surfaceId) as TerminalSurface;
    }

    public void Dispose() => Persistence?.Dispose();

    /// <summary>The command palette's rows: every keyboard-reachable layout command.</summary>
    public static IReadOnlyList<WorkbenchCommand> PaletteCommands(string search) =>
        [.. WorkbenchCommandCatalog.Search(search)];
}
