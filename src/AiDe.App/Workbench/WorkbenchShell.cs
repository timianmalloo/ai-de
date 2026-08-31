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
using AiDe.Core.Watcher;
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
    // Which canvas instance already has its FocusLeaveRequested wired. Reconcile (DC-029) now keeps
    // the same CanvasSurface across re-renders, so BindCanvas can run repeatedly on one instance; the
    // FocusLeaveRequested handler is a lambda (cannot be -='d), so it is subscribed once per instance.
    private CanvasSurface? _focusBoundCanvas;
    // Cross-restart terminal customization, keyed by the stable SurfaceId (off the Core layout model).
    private TerminalCustomizationStore? _customizationStore;
    private readonly HashSet<string> _customizationInitialized = new(StringComparer.Ordinal);
    private CanvasGraphViewModel? _canvasGraph;

    // The per-workspace Loomkeeper host: it owns the observation store AND runs the ingest (the
    // coordination-contract log pump, in-process, so liveness is exact). Null until a workspace with a
    // data directory is attached; reset on each attach. The panes then show "not available".
    private WatcherHost? _watcherHost;
    private SessionCoordinationEmitter? _watcherEmitter;
    private CancellationTokenSource? _watcherPump;

    public WorkbenchShell(IWorkspaceQueries? queries, string? workspaceDataDirectory = null)
    {
        Service = new LayoutService();

        LiveRegion = new TextBlock { TextWrapping = TextWrapping.Wrap };
        LiveRegion.SetResourceReference(TextBlock.ForegroundProperty, "TextMutedBrush");
        Announcer = new WorkbenchAnnouncer(LiveRegion);

        _queries = queries;

        // Loomkeeper read surfaces (Sessions/Board/Leaderboard) read a per-workspace watcher store the
        // in-process host also WRITES (the coordination-contract log pump). Wired both here (for a data
        // directory supplied at construction) AND in AttachWorkspace (the real runtime path, where the
        // directory arrives after the daemon resolves) - the shell is built with a null workspace and
        // the workspace attaches later, so wiring only the constructor would leave the surfaces inert.
        var watcher = StartWatcher(workspaceDataDirectory);

        _factory = new SurfaceContentFactory(
            queries, watcher.Sessions, watcher.Board, watcher.Leaderboard, watcher.Disputes);
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
            BindTerminalAttention();
        AnnounceEnvironmentHealth();

            return result.Applied
                ? $"{agent} terminal opened. Dispatch is refused until it reaches its prompt."
                : result.Announcement;
        };
        Controller.NewTerminalRequested = () =>
        {
            var terminalStack = Service.Current.AllStacks()
                .FirstOrDefault(s => s.Surfaces.Any(su => su.Kind == "terminal"));

            if (terminalStack is null) return "There is no terminal pane to open it beside.";

            // A non-"agent:" id makes the content factory launch a plain shell (Executable = null),
            // and the title is "Terminal" — never an agent name. The guid keeps ids unique so a new
            // terminal is ADDED to the collection, never replacing an existing session.
            var id = $"terminal#{Guid.NewGuid().ToString("N")[..6]}";
            var result = Service.Apply(new LayoutOperation.AddSurface(
                terminalStack.Id, new Surface(id, "terminal", "Terminal")));

            Adapter.Render();
            BindCanvas();
            BindContexts();
            BindJoins();
            BindTerminalAttention();
            AnnounceEnvironmentHealth();

            return result.Applied ? "Terminal opened." : result.Announcement;
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

            _customizationStore = new TerminalCustomizationStore(
                Path.Combine(workspaceDataDirectory, "terminal-customization.json"));

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

        // Wire the Loomkeeper watcher for THIS workspace's data directory (the real runtime path - the
        // constructor was built with a null workspace). Without this the read surfaces would be inert:
        // AttachWorkspace rebuilding the factory without the watcher queries is what left them showing
        // "not available" even after a workspace opened.
        var watcher = StartWatcher(dataDirectory);
        _factory = new SurfaceContentFactory(
            queries, watcher.Sessions, watcher.Board, watcher.Leaderboard, watcher.Disputes);

        // The watcher read panes may already have been realized (at construction) against a factory with
        // no watcher queries - showing "not available". Mark them to rebuild on the next Render so they
        // pick up the now-wired factory. Only the stateless watcher kinds - never a terminal (DC-029).
        var watcherKinds = new HashSet<string>(StringComparer.Ordinal) { "sessions", "board", "leaderboard" };
        Adapter.Invalidate(Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => watcherKinds.Contains(s.Kind))
            .Select(s => s.SurfaceId));

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

            Controller.WorkspaceReindexAll = async () =>
                (await commands.IndexSolutionAsync(artifactRevision, CancellationToken.None, force: true))
                    .Describe();
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

            _customizationStore = new TerminalCustomizationStore(
                Path.Combine(dataDirectory, "terminal-customization.json"));

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
        BindTerminalAttention();
        AnnounceEnvironmentHealth();
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
            var found = _queries.FindAsync(string.Empty, AiDe.Core.Projections.ProjectionService.MaxSearchResultsCeiling, CancellationToken.None)
                .GetAwaiter().GetResult();

            var symbols = found.Matches.Select(m => m.NodeId).ToList();
            var map = BoundedContextReader.Load(path, symbols);

            var read = ReadAssertions();
            AnnounceShortfall("Contexts", read);

            return new ContextProjection(map, read.Assertions).Compute();
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
    /// <summary>How many assertions are asked for per page.</summary>
    /// <remarks>
    /// The panes want every current assertion. They used to rebuild that set node by node through
    /// Describe, bounded at 50 neighbours each, which lost two join edges of 124 on a real
    /// repository and asked the store for a graph walk when a table scan was wanted. Paged because
    /// the answer crosses a pipe.
    /// </remarks>
    private const int AssertionsPerPage = AiDe.Core.Projections.ProjectionService.MaxEvidencePageCeiling;

    /// <summary>
    /// A safety bound on paging, so a runaway cursor cannot spin for ever.
    /// </summary>
    /// <remarks>
    /// Two million assertions is far beyond anything measured — the largest real workspace read so
    /// far held twelve thousand. Hitting it is a defect signal, and it is reported as a shortfall
    /// rather than silently ending the read.
    /// </remarks>
    private const int MaxPages = 1000;

    /// <summary>Every current assertion, with what the read did NOT see.</summary>
    /// <remarks>
    /// Shared by the context and join panes rather than read twice. Two reads of the same store
    /// would be two chances to disagree, and a context map and a join view that disagree about the
    /// same edge is a defect the user has no way to diagnose.
    /// </remarks>
    private AiDe.Core.Projections.EvidenceRead ReadAssertions()
    {
        if (_queries is null) return AiDe.Core.Projections.EvidenceRead.Empty;

        var assertions = new List<AiDe.Core.Facts.EvidenceAssertion>();
        string? cursor = null;
        var pages = 0;

        do
        {
            var page = _queries.EvidenceAsync(cursor, AssertionsPerPage, CancellationToken.None)
                .GetAwaiter().GetResult();

            assertions.AddRange(page.Assertions);
            cursor = page.NextCursor;
            pages++;
        }
        while (cursor is not null && pages < MaxPages);

        // Complete unless the page cap stopped us, which is the only way this read can now be short.
        return new AiDe.Core.Projections.EvidenceRead(
            assertions,
            NodesMatched: assertions.Count,
            NodesRead: cursor is null ? assertions.Count : 0,
            NeighbourLimit: AssertionsPerPage,
            NodesAtNeighbourLimit: 0);
    }

    /// <summary>
    /// Tells the user when a pane's numbers are lower bounds.
    /// </summary>
    /// <remarks>
    /// Announced from the shell rather than drawn by the panes: the caps live here, the panes render
    /// what they are given, and a surface computing its own caveat would be a second definition of a
    /// fact this side already owns. The panes will show it too once the view models carry it —
    /// recorded as a request in docs/collaboration/session-contracts.md.
    /// </remarks>
    private void AnnounceShortfall(string pane, AiDe.Core.Projections.EvidenceRead read)
    {
        if (read.Shortfall is not { } shortfall) return;

        // Once per distinct sentence. A refresh that repeats the same caveat trains the user to
        // ignore it, and the caveat is the part that matters.
        var message = $"{pane}: {shortfall}";
        if (string.Equals(message, _lastShortfall, StringComparison.Ordinal)) return;

        _lastShortfall = message;
        Announcer.Announce(message);
    }

    private string? _lastShortfall;

    /// <summary>
    /// Reports any terminal pane that is waiting on a person.
    /// </summary>
    /// <remarks>
    /// Measured: an agent CLI opens on a trust gate even in a directory whose sessions run daily
    /// (<c>spikes/agent-readiness</c>). Before this the shell simply refused to dispatch and said
    /// nothing, which is indistinguishable from a broken pane (DC-011).
    /// </remarks>
    internal void BindTerminalAttention()
    {
        // Materialised before iterating: applying a saved rename triggers a re-render, and enumerating
        // lazily while the view rebuilds is asking for trouble.
        var terminals = Service.Current.AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Select(surface => Adapter.ContentFor(surface.SurfaceId))
            .OfType<TerminalSurface>()
            .ToList();

        foreach (var terminal in terminals)
        {
            terminal.AttentionRequired -= OnTerminalAttentionRequired;
            terminal.AttentionRequired += OnTerminalAttentionRequired;
            terminal.DisplayNameChanged -= OnTerminalRenamed;
            terminal.DisplayNameChanged += OnTerminalRenamed;
            terminal.CustomizationChanged -= OnTerminalCustomizationChanged;
            terminal.CustomizationChanged += OnTerminalCustomizationChanged;

            // Apply saved customization once per surface (reconcile reuses the instance, so this must
            // not re-apply every render). The SurfaceId round-trips through the layout store, so a
            // renamed/recoloured terminal comes back the same after a restart.
            if (_customizationStore is not null
                && _customizationInitialized.Add(terminal.SurfaceId)
                && _customizationStore.TryGet(terminal.SurfaceId, out var saved)
                && saved is not null)
            {
                if (!string.IsNullOrEmpty(saved.Name))
                {
                    terminal.Rename(saved.Name);
                }

                if (!string.IsNullOrEmpty(saved.Scheme))
                {
                    terminal.ApplyScheme(TerminalColorScheme.ByName(saved.Scheme));
                }

                if (!string.IsNullOrEmpty(saved.TabColour))
                {
                    terminal.TabColour = HexToBrush(saved.TabColour);
                }
            }
        }
    }

    // A rename lives on the surface (reconcile keeps it alive); re-render so the tab caption, which
    // BuildPane reads from the surface's DisplayName, reflects it. Reconcile makes this cheap and
    // leaves every live session untouched.
    private void OnTerminalRenamed(object? sender, EventArgs e) => Adapter.Render();

    // Persist any customization change (name, scheme, tab colour) keyed by the surface id, so it
    // survives a restart. Best-effort inside the store; a write failure never reaches here.
    private void OnTerminalCustomizationChanged(object? sender, EventArgs e)
    {
        if (sender is TerminalSurface terminal && _customizationStore is not null)
        {
            _customizationStore.Save(terminal.SurfaceId, new TerminalCustomization(
                terminal.DisplayName,
                terminal.Scheme.Name,
                BrushToHex(terminal.TabColour)));
        }
    }

    private static string? BrushToHex(System.Windows.Media.Brush? brush) =>
        brush is System.Windows.Media.SolidColorBrush solid ? solid.Color.ToString() : null;

    private static System.Windows.Media.Brush? HexToBrush(string hex)
    {
        try
        {
            return new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        }
        catch
        {
            return null;
        }
    }

    private void OnTerminalAttentionRequired(object? sender, string message) =>
        Announcer.Announce(message);

    /// <summary>
    /// Says so when this machine's environment cannot survive being handed to a child process.
    /// </summary>
    /// <remarks>
    /// Announced ONCE per shell, not per terminal: it is a property of the machine, and repeating it
    /// for every pane would turn the one message that explains a whole class of confusion into
    /// noise. Not a failure — the terminal works — so it is said and the shell carries on.
    /// </remarks>
    internal void AnnounceEnvironmentHealth()
    {
        if (_environmentAnnounced) return;
        _environmentAnnounced = true;

        foreach (var finding in AiDe.Core.Terminal.EnvironmentHealth.Inspect())
        {
            Announcer.Announce(finding);
        }
    }

    private bool _environmentAnnounced;

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
            var found = _queries.FindAsync(string.Empty, AiDe.Core.Projections.ProjectionService.MaxSearchResultsCeiling, CancellationToken.None)
                .GetAwaiter().GetResult();

            var read = ReadAssertions();
            AnnounceShortfall("Joins", read);

            return new JoinProjection(read.Assertions).Compute();
        };

        pane.NodeSelected -= OnJoinNodeSelected;
        pane.NodeSelected += OnJoinNodeSelected;

        pane.Refresh();
    }

    /// <summary>Centres the graph on a join's endpoint.</summary>
    /// <remarks>
    /// Any context filter in force is cleared first. A join whose endpoint sits outside the filtered
    /// context would otherwise centre the graph on a node the canvas has been told not to draw, and
    /// the user would click a row and watch nothing happen.
    /// </remarks>
    private void OnJoinNodeSelected(object? sender, string nodeId)
    {
        var canvas = Service.Current.AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Select(surface => Adapter.ContentFor(surface.SurfaceId))
            .OfType<CanvasSurface>()
            .FirstOrDefault();

        if (canvas is null) return;

        if (_canvasGraph?.ContextFilter is not null)
        {
            _canvasGraph.ContextFilter = null;
            Announcer.Announce("Graph filter cleared.");
        }

        Announcer.Announce($"Graph centred on {nodeId}.");
        _ = canvas.RefreshAsync(nodeId);
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

        // Subscribed once per canvas instance: reconcile reuses the surface across renders, and a
        // lambda handler cannot be removed, so an unguarded += would accumulate on every mutation.
        if (!ReferenceEquals(_focusBoundCanvas, canvas))
        {
            _focusBoundCanvas = canvas;
            canvas.FocusLeaveRequested += (_, direction) =>
                Announcer.Announce(router.Leave(direction).Announcement);
        }

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

    /// <summary>
    /// Opens (or re-opens) the per-workspace Loomkeeper host for a data directory and returns the read
    /// queries the factory wires into the watcher surfaces. Called from the constructor AND from
    /// <see cref="AttachWorkspace"/> - the latter is the real runtime path, where the data directory
    /// arrives after the shell is built with a null workspace.
    /// </summary>
    /// <remarks>
    /// The pump loop is started with <see cref="Task.Run(System.Action)"/> so that even its synchronous
    /// first pump (a directory read + a SQLite fold) never runs on the UI thread during attach. A host
    /// that cannot open degrades to null queries - the panes show "not available" and the workbench is
    /// never blocked. A re-attach disposes the previous host first.
    /// </remarks>
    private (IWatcherSessionsQuery? Sessions, IWatcherBoardQuery? Board,
             IWatcherLeaderboardQuery? Leaderboard, IWatcherDisputeQuery? Disputes) StartWatcher(string? dataDirectory)
    {
        // A re-attach (opening a different workspace) resets the host.
        _watcherPump?.Cancel();
        _watcherPump?.Dispose();
        _watcherHost?.Dispose();
        _watcherPump = null;
        _watcherHost = null;
        _watcherEmitter = null;

        if (string.IsNullOrEmpty(dataDirectory))
        {
            return (null, null, null, null);
        }

        try
        {
            var host = WatcherHost.Open(dataDirectory, Path.Combine(dataDirectory, "loomkeeper-coord"));
            _watcherHost = host;
            _watcherEmitter = host.CreateEmitter();
            _watcherPump = new CancellationTokenSource();
            var token = _watcherPump.Token;

            // Off the UI thread: the loop's synchronous work (a directory read + a SQLite fold, and a
            // snapshot of the layout's terminal surfaces) must not run on the caller's thread during
            // construction/attach. The loop reconciles the terminal panes into coordination sessions so a
            // terminal the user opens appears in the watcher, then pumps the coordination log into the store.
            var emitter = _watcherEmitter;
            _ = Task.Run(() => WatcherLoopAsync(host, emitter, token), token);

            return (
                new WatcherSessionsQuery(host.Store, host.Liveness),
                new WatcherBoardQuery(host.Store),
                new WatcherLeaderboardQuery(host.Store),
                new WatcherDisputeQuery(host.Store));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _watcherPump?.Cancel();
            _watcherHost?.Dispose();
            _watcherHost = null;
            _watcherEmitter = null;
            _watcherPump = null;
            return (null, null, null, null);
        }
    }

    /// <summary>
    /// The watcher's background loop: every tick it reconciles the terminal panes that currently exist into
    /// coordination sessions (a new pane registers, an existing one heartbeats, a closed one ends - conn-8),
    /// then pumps the coordination log into the store. A hiccup on any tick is swallowed so the workbench is
    /// never taken down by watcher work; cancellation ends the loop cleanly.
    /// </summary>
    private async Task WatcherLoopAsync(WatcherHost host, SessionCoordinationEmitter emitter, CancellationToken token)
    {
        var interval = TimeSpan.FromSeconds(2);
        while (!token.IsCancellationRequested)
        {
            try
            {
                var terminals = TerminalSnapshot();
                var ids = new HashSet<string>(terminals.Select(t => t.Id), StringComparer.Ordinal);
                emitter.Reconcile(ids, id => IdentityFor(id, terminals));
                host.PumpOnce();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A reconcile/pump hiccup must never take down the workbench; skip this tick.
            }

            try { await Task.Delay(interval, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>A snapshot of the terminal surfaces (id + agent title) in the current layout.</summary>
    private IReadOnlyList<(string Id, string Agent)> TerminalSnapshot()
    {
        var list = new List<(string, string)>();
        foreach (var stack in Service.Current.AllStacks())
        {
            foreach (var surface in stack.Surfaces)
            {
                if (surface.Kind == "terminal")
                {
                    list.Add((surface.SurfaceId, surface.Title));
                }
            }
        }

        return list;
    }

    /// <summary>Builds the coordination identity a terminal pane presents when it registers.</summary>
    private SessionCoordinationIdentity IdentityFor(string surfaceId, IReadOnlyList<(string Id, string Agent)> terminals)
    {
        var root = _workspaceRoot ?? string.Empty;
        var display = string.IsNullOrEmpty(root) ? "workspace" : Path.GetFileName(root.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(display))
        {
            display = "workspace";
        }

        var agent = terminals.FirstOrDefault(t => t.Id == surfaceId).Agent;
        if (string.IsNullOrEmpty(agent))
        {
            agent = "terminal";
        }

        var repoPath = string.IsNullOrEmpty(root) ? display : root;
        return new SessionCoordinationIdentity(
            RepoPath: repoPath,
            RepoDisplay: display,
            WorktreeBranch: "workspace",
            WorktreePath: repoPath,
            TerminalId: surfaceId,
            AgentName: agent);
    }

    public void Dispose()
    {
        Persistence?.Dispose();
        _watcherPump?.Cancel();
        _watcherPump?.Dispose();
        _watcherHost?.Dispose();
    }

    /// <summary>The command palette's rows: every keyboard-reachable layout command.</summary>
    public static IReadOnlyList<WorkbenchCommand> PaletteCommands(string search) =>
        [.. WorkbenchCommandCatalog.Search(search)];
}
