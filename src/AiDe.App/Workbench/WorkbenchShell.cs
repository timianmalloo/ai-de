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
    /// <summary>The UI thread, captured where the shell is wired — indexing completes on a worker.</summary>
    private System.Windows.Threading.Dispatcher _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

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
    // Prompt dispatch context, stored so both the focused-terminal path (Prompt.Dispatch) and the
    // named-session path (DispatchToAsync, for the prompt-draft surface) share one choreography.
    private IWorkspaceDispatch? _dispatch;
    private string? _dispatchScopeId;
    // The last re-index's summary + the daemon diagnostics VM, for the Diagnostics pane. The pane is
    // opened on demand and shows the most recent index's coverage rather than re-indexing itself.
    private AiDe.Core.Ipc.IndexSummary? _lastIndex;
    private AiDe.Core.Presentation.WorkspaceDiagnosticsViewModel? _diagnosticsVm;
    private readonly ZoneRails _rails;
    // Cross-restart prompt drafts, keyed by the stable SurfaceId (off the Core layout model).
    private PromptDraftStore? _promptDraftStore;

    // The per-workspace Loomkeeper host: it owns the observation store AND runs the ingest (the
    // coordination-contract log pump, in-process, so liveness is exact). Null until a workspace with a
    // data directory is attached; reset on each attach. The panes then show "not available".
    private WatcherHost? _watcherHost;
    private SessionCoordinationEmitter? _watcherEmitter;
    private CancellationTokenSource? _watcherPump;

    /// <summary>The stateless watcher read-pane kinds - rebuilt on refresh; never a terminal (DC-029).</summary>
    private static readonly HashSet<string> WatcherPaneKinds = new(StringComparer.Ordinal) { "sessions", "board", "leaderboard" };

    /// <summary>
    /// Every pane kind whose content depends on a workspace being open.
    /// </summary>
    /// <remarks>
    /// <para><b>Derived from the factory, not restated.</b> A pane realized before
    /// <see cref="AttachWorkspace"/> was built against a factory with no queries, so it rendered
    /// "not available" — and it stays that way until something marks it to rebuild. The watcher
    /// kinds were marked; <c>view</c> and <c>inspector</c> were not, so the default layout's
    /// "Domain" pane read <i>"Domain is not available in this build"</i> against a fully indexed
    /// workspace, permanently.</para>
    ///
    /// <para>That is the same defect as the watcher panes, fixed once and swept as far as the
    /// reported instance. The list is now the union rather than the three kinds somebody happened
    /// to be looking at — the fix is the sweep, not the patch.</para>
    ///
    /// <para><b>Never a terminal</b> (DC-029): rebuilding one kills a live process. Only kinds whose
    /// content is a pure function of the queries belong here.</para>
    /// </remarks>
    private static readonly HashSet<string> WorkspaceDependentPaneKinds =
        new(StringComparer.Ordinal) { "sessions", "board", "leaderboard", "view", "inspector" };

    /// <summary>The last observed watcher-store fingerprint; the loop only re-renders the panes when it changes (conn-9).</summary>
    private string? _watcherFingerprint;

    /// <summary>Guards the one-shot import of the workspace's declared-goal episodes from its audit log (ep-capture).</summary>
    private bool _episodesImported;

    public WorkbenchShell(IWorkspaceQueries? queries, string? workspaceDataDirectory = null)
    {
        // ADR-0021: the layout is zone-based. The Strangler service projects zones to the fixed-shape
        // tree the adapter/persistence render, so moving/closing a pane can no longer relocate others.
        Service = new ZoneBackedLayoutService();

        LiveRegion = new TextBlock
        {
            // A status strip is ONE line. Wrapping + an Auto-height row let a long announcement (a
            // re-index reports 200+ analysis-boundary disclosures) grow the strip until it ate ~70%
            // of the window. Single-line with an ellipsis caps it permanently; the full text stays on
            // hover (tooltip, set in the announcer) and is still read in full by assistive tech.
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
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
            queries, watcher.Sessions, watcher.Board, watcher.Leaderboard, watcher.Disputes,
            queries is not null ? SearchWorkspaceAsync : null);
        Manager = new DockingManager();
        AutomationProperties.SetName(Manager, "Workbench");

        // Indirected through a field so the workspace can arrive AFTER the window exists. The
        // shell is built synchronously and shown immediately; reaching a daemon may take a cold
        // start, and a window that appears only once a process has launched looks like a failure to
        // launch.
        Adapter = new WorkbenchAdapter(Manager, Service, surface => _factory.Create(surface));
        Controller = new WorkbenchController(Service, Announcer);

        // ADR-0021 collapse-to-rail: wrap the docking host in edge rails that appear when a tool zone
        // is collapsed. The rail's one-click expand returns the zone (its panes were retained in the
        // model). Rails refresh whenever the projection is re-rendered (Manager.LayoutChanged fires
        // when Render replaces the layout).
        _rails = new ZoneRails(
            Manager,
            () => (Service as ZoneBackedLayoutService)?.Zones,
            ExpandZone);
        Manager.LayoutChanged += (_, _) => _rails.Refresh();

        Palette = new CommandPalette(Controller, Announcer);
        Prompt = new PromptBar(Announcer);

        Controller.NewAgentTerminalRequested = agent =>
        {
            ReconcileViewIntoModel();

            // The harness is CHOSEN, not discovered. The previous command took whatever was first on
            // PATH, so it could not say which harness it had started — and a session's harness cannot
            // be added afterwards, because a second coordination register for a known session
            // discards its attributes rather than merging them.
            var profile = TerminalSurface.Profiles.All
                .FirstOrDefault(p => string.Equals(p.Agent, agent, StringComparison.OrdinalIgnoreCase));

            if (profile is null)
            {
                return $"No profile is configured for '{agent}'.";
            }

            if (!TerminalSurface.AvailableAgents.Contains(agent, StringComparer.OrdinalIgnoreCase))
            {
                // Named rather than a generic failure: the fix is to install it, and the user cannot
                // act on "not available".
                return $"{profile.DisplayName ?? agent} was not found on PATH.";
            }

            var terminalStack = Service.Current.AllStacks()
                .FirstOrDefault(s => s.Surfaces.Any(su => su.Kind == "terminal"));

            if (terminalStack is null) return "There is no terminal pane to open it beside.";

            var id = $"agent:{agent}#{Guid.NewGuid().ToString("N")[..6]}";

            // The tab now names the harness, because the user chose it. The previous title was
            // deliberately harness-neutral ("Agent terminal") for the honest reason that the CLI had
            // been picked off PATH and naming it would have presumed one — that reason is gone.
            var title = profile.DisplayName ?? "Agent terminal";
            _harnessBySurface[id] = profile;

            var result = Service.Apply(new LayoutOperation.AddSurface(
                terminalStack.Id, new Surface(id, "terminal", title)));

            Adapter.Render();
            BindCanvas();
            BindContexts();
            BindJoins();
            BindTerminalAttention();
        AnnounceEnvironmentHealth();

            return result.Applied
                ? $"{title} session opened. Dispatch is refused until it reaches its prompt."
                : result.Announcement;
        };
        Controller.NewTerminalRequested = () =>
        {
            ReconcileViewIntoModel();
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

        // The operator's recourse against a score they disagree with (US rule 12): an append-only dispute
        // against the latest scored episode. It records evidence for review; it never changes the score.
        Controller.RaiseDisputeRequested = () => RaiseDisputeOnLatestScore();

        Controller.NewPromptDraftRequested = () =>
        {
            ReconcileViewIntoModel();
            // Open the draft beside a terminal (its transfer target) when there is one, else in any
            // stack — a draft is useful even before a session exists (it just cannot transfer yet).
            var stack = Service.Current.AllStacks()
                .FirstOrDefault(s => s.Surfaces.Any(su => su.Kind == "terminal"))
                ?? Service.Current.AllStacks().FirstOrDefault();

            if (stack is null) return "There is no pane to open a prompt draft in.";

            var id = $"prompt#{Guid.NewGuid().ToString("N")[..6]}";
            var result = Service.Apply(new LayoutOperation.AddSurface(
                stack.Id, new Surface(id, "prompt", "Prompt draft")));

            Adapter.Render();
            BindCanvas();
            BindContexts();
            BindJoins();
            BindTerminalAttention();

            return result.Applied ? "Prompt draft opened." : result.Announcement;
        };

        Controller.NewClassDiagramRequested = () =>
            OpenReferenceDocument(
                new Surface($"classdiagram#{Guid.NewGuid().ToString("N")[..6]}", "classdiagram", "Class diagram"),
                "Class diagram opened.",
                "There is no pane to open a class diagram in.");

        Controller.NewSequenceDiagramRequested = () =>
            OpenReferenceDocument(
                new Surface($"sequence#{Guid.NewGuid().ToString("N")[..6]}", "sequence", "Sequence diagram"),
                "Sequence diagram opened.",
                "There is no pane to open a sequence diagram in.");

        Controller.NewSearchRequested = () =>
            OpenReferenceDocument(
                new Surface($"search#{Guid.NewGuid().ToString("N")[..6]}", "search", "Search"),
                "Search opened.",
                "There is no pane to open search in.");

        Controller.NewCodeViewerRequested = () =>
            OpenReferenceDocument(
                new Surface($"codeviewer#{Guid.NewGuid().ToString("N")[..6]}", "codeviewer", "Source"),
                "Code viewer opened.",
                "There is no pane to open a code viewer in.");

        Controller.NewDiagnosticsRequested = () =>
            OpenReferenceDocument(
                new Surface($"diagnostics#{Guid.NewGuid().ToString("N")[..6]}", "diagnostics", "Diagnostics"),
                "Diagnostics opened.",
                "There is no pane to open diagnostics in.");

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

            RestoreArrangementOnWorkspaceOpen();
        }

        Adapter.Render();
        TrackFocusedPane();
        PersistOnEveryChange();
    }

    public ILayoutService Service { get; }

    public DockingManager Manager { get; }

    /// <summary>
    /// The docking host wrapped in collapse-to-rail edge strips (ADR-0021). Host this instead of
    /// <see cref="Manager"/> so a collapsed tool zone shows a one-click rail back. Falls back to the
    /// bare manager when the layout is not zone-based.
    /// </summary>
    public FrameworkElement WorkbenchRoot => _rails.Root;

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
            queries, watcher.Sessions, watcher.Board, watcher.Leaderboard, watcher.Disputes,
            SearchWorkspaceAsync);

        // Panes realized at construction were built against a factory with no queries and render
        // "not available". Mark every workspace-dependent kind to rebuild on the next Render so they
        // pick up the now-wired factory. Never a terminal (DC-029) - rebuilding one kills a live
        // process.
        //
        // This was the three WATCHER kinds. `view` and `inspector` have the same dependency - the
        // factory builds them only `when queries is not null` - so the default layout's "Domain"
        // pane read "not available in this build" against a fully indexed workspace, forever. Same
        // defect, swept only as far as the pane somebody reported.
        var rebuildable = WorkspaceDependentPaneKinds;
        Adapter.Invalidate(Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => rebuildable.Contains(s.Kind))
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
        // Stored on fields so the focused-terminal path (here) and the named-session path
        // (DispatchToAsync, for the prompt-draft surface) share ONE choreography (DispatchToSurfaceAsync).
        if (commands is IWorkspaceDispatch dispatch)
        {
            _dispatch = dispatch;
            _dispatchScopeId = scopeId;

            Prompt.Dispatch = body =>
            {
                var surface = FocusedTerminal()
                    ?? throw new InvalidOperationException("focus a terminal pane before dispatching");
                return DispatchToSurfaceAsync(surface, body);
            };
        }

        // Diagnostics needs no daemon connection: the installation layout is on disk and the
        // incident sidecar is a local file, so the state is readable even when the daemon is not.
        _diagnosticsVm = new WorkspaceDiagnosticsViewModel(
            string.IsNullOrEmpty(dataDirectory) ? null : new DaemonInstallation(dataDirectory),
            string.IsNullOrEmpty(dataDirectory)
                ? null
                : new HealthIncidentSidecar(Path.Combine(dataDirectory, "health-incidents.jsonl")));

        Controller.WorkspaceDiagnostics = () => _diagnosticsVm.Read().Describe();

        // Indexing changes what every data-backed pane is showing. Subscribed once here rather than
        // per pane: panes come and go, and RereadDataSurfaces asks the layout what is open now.
        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        Controller.WorkspaceDataChanged -= OnWorkspaceDataChanged;
        Controller.WorkspaceDataChanged += OnWorkspaceDataChanged;

        if (commands is not null)
        {
            Controller.WorkspaceIndex = async () =>
                CaptureIndex(await commands.IndexSolutionAsync(artifactRevision, CancellationToken.None)).Describe();

            Controller.WorkspaceReindexAll = async () =>
                CaptureIndex(await commands.IndexSolutionAsync(artifactRevision, CancellationToken.None, force: true))
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

            RestoreArrangementOnWorkspaceOpen();
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
            .Select(surface => Adapter.SurfaceContent<ContextMapSurface>(surface.SurfaceId))
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

        // Prompt-draft targets are terminal-derived, so refresh them whenever the terminal set is
        // (re)bound — a session becoming ready or a pane closing changes what a draft can transfer to.
        BindPromptDrafts();

        // Class diagrams derive from the graph; (re)populate any that are open (early-returns when none).
        BindClassDiagrams();

        // Code viewers show node source (a labelled sample until Core's content query ships).
        BindCodeViewers();

        // Search surfaces navigate a hit into the graph; (re)bind the activation hand-off.
        BindSearchSurfaces();

        // Sequence diagrams follow the last "Sequence diagram" action (Phase E).
        BindSequenceDiagrams();
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
            .Select(surface => Adapter.SurfaceContent<JoinSurface>(surface.SurfaceId))
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

    /// <summary>Re-reads every pane whose content came from the store.</summary>
    /// <remarks>
    /// <para>Raised off the UI thread — indexing runs on a worker — so the work is marshalled before
    /// any of these touch a WPF element.</para>
    ///
    /// <para>The layout is asked what is open rather than a list being kept: a pane can be closed,
    /// reopened or moved between stacks while an index runs, and a remembered reference would either
    /// refresh a pane that is gone or miss one that arrived.</para>
    /// </remarks>
    private void OnWorkspaceDataChanged() => _ = _dispatcher.InvokeAsync(RereadDataSurfaces);

    internal void RereadDataSurfaces()
    {
        var contents = Service.Current.AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Select(surface => Adapter.ContentFor(surface.SurfaceId))
            .ToList();

        foreach (var content in contents)
        {
            switch (content)
            {
                // The canvas re-queries from its current root, so a user who has navigated into a
                // node stays where they are and sees that node's new neighbours.
                case CanvasSurface canvas:
                    _ = canvas.RefreshAsync();
                    break;

                // These two pull through a Source delegate that reads the store on every call, so
                // Refresh IS the re-read.
                case ContextMapSurface contexts:
                    contexts.Refresh();
                    break;

                case JoinSurface joins:
                    joins.Refresh();
                    break;
            }
        }
    }

    /// <summary>Centres the graph on a join's endpoint.</summary>
    /// <remarks>
    /// Any context filter in force is cleared first. A join whose endpoint sits outside the filtered
    /// context would otherwise centre the graph on a node the canvas has been told not to draw, and
    /// the user would click a row and watch nothing happen.
    /// </remarks>
    /// <summary>
    /// Centres the canvas on a node and announces WHAT HAPPENED, after it happened.
    /// </summary>
    /// <remarks>
    /// <para><b>The defect this replaces.</b> Three sites announced <i>"Graph centred on X"</i> and
    /// then started the refresh fire-and-forget. Measured on a real surface: the canvas was not
    /// ready, the refresh returned immediately, the graph source was asked <b>0</b> times, and the
    /// announcement had already gone out. A screen-reader user was told the graph centred on a node
    /// it never looked up.</para>
    ///
    /// <para><b>It is not the same defect as a value that fails to reach the surface.</b> The
    /// dropped-in-transit family concerns something that EXISTED and did not arrive. This is a
    /// statement about an ACTION, made before the action, about an action that may not occur — it
    /// was never true when it was said, so no amount of care in the wording repairs it. The only fix
    /// is to speak from the result.</para>
    ///
    /// <para><b>And the second reason it could be false.</b> The graph draws a bounded
    /// most-connected-first slice, so a node the user picked may not be in view at all — measured
    /// median relation degree for knowledge nodes is 0, which makes this the ordinary case rather
    /// than the edge case. <c>NotInView</c> says so. An honest negative is a better sentence than a
    /// centring that did not happen.</para>
    ///
    /// <para><b>The discarded task is safe here</b> — and only here — because nothing can escape it:
    /// every path inside announces, including the failure. A bare <c>_ =</c> over a call that can
    /// fault observes the fault with nobody, which was the other half of the finding.</para>
    /// </remarks>
    private async Task CentreOnAsync(CanvasSurface canvas, string nodeId, string fallbackLabel)
    {
        CanvasRefresh result;

        try
        {
            result = await canvas.RefreshAsync(nodeId);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // The fault the discarded task used to swallow. Saying the graph could not be centred is
            // the whole point of catching it; silence here would rebuild the defect one layer in.
            Announcer.Announce($"The graph could not be centred on {fallbackLabel}: {ex.Message}");
            return;
        }

        var label = result.Label ?? fallbackLabel;

        Announcer.Announce(result.Outcome switch
        {
            CanvasRefreshOutcome.Centred => $"Graph centred on {label}.",
            CanvasRefreshOutcome.NotInView =>
                $"{label} is not in the current view. The graph draws the most connected nodes, and "
                + "this one is outside that slice.",
            CanvasRefreshOutcome.Deferred => $"The graph is still loading. It will centre on {label} when ready.",
            CanvasRefreshOutcome.NoWorkspace => "No workspace is open, so there is no graph to centre.",
            _ => $"Graph refreshed for {label}.",
        });
    }

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

        _ = CentreOnAsync(canvas, nodeId, nodeId);
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
        graph.ContextLookup = BuildContextLookup();

        canvas.GraphSource = (rootId, ct) => LoadRouted(graph, rootId, ct);

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

        // Follow graph selection into any open Source pane (§4s — Design owns which node a viewer
        // shows; the answer is "the one you just selected"). Named handler with -=/+= so repeated
        // binds do not stack, the same idiom the join-endpoint subscription uses (smoke 9-1: "select
        // a node … nothing updates in any of the source tabs").
        canvas.NodeSelected -= OnCanvasNodeSelectedForViewers;
        canvas.NodeSelected += OnCanvasNodeSelectedForViewers;

        // Right-click a node for the contextual "Open as source / class diagram / …" menu (smoke 9-1
        // §3 — the IntelliJ model: a viewer is opened from an entry point in the model).
        canvas.NodeContextMenuRequested -= OnNodeContextMenuRequested;
        canvas.NodeContextMenuRequested += OnNodeContextMenuRequested;
    }

    // Builds and shows the contextual "Open as…" menu for a right-clicked node, filtered to the
    // viewers its type supports (NodeViewMenu). The menu shows at the cursor.
    private void OnNodeContextMenuRequested(object? sender, NodeContextMenuRequest req)
    {
        var options = NodeViewMenu.OptionsFor(req.NodeKind, req.IsKnowledge);
        if (options.Count == 0) { return; }

        var menu = new System.Windows.Controls.ContextMenu
        {
            Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
        };
        foreach (var option in options)
        {
            var item = new System.Windows.Controls.MenuItem { Header = option.Label };
            var kind = option.Kind;
            item.Click += (_, _) => OpenNodeView(req.NodeId, kind);
            menu.Items.Add(item);
        }

        menu.IsOpen = true;
    }

    // Performs a chosen "Open as…" action, reusing the existing open-surface commands and the
    // selection→viewer routing. New viewers fill from _lastSelectedNodeId on their next bind.
    private void OpenNodeView(string nodeId, NodeViewKind kind)
    {
        switch (kind)
        {
            case NodeViewKind.Source:
            case NodeViewKind.Read:
                _lastSelectedNodeId = nodeId;
                Controller.NewCodeViewerRequested?.Invoke();          // opens a Source pane
                _ = ShowNodeInCodeViewersAsync(nodeId, OpenCodeViewers());  // fill any already open
                break;

            case NodeViewKind.ClassDiagram:
                Controller.NewClassDiagramRequested?.Invoke();
                break;

            case NodeViewKind.Sequence:
                _lastSequenceNodeId = nodeId;
                Controller.NewSequenceDiagramRequested?.Invoke();
                _ = ShowNodeInSequenceDiagramsAsync(nodeId, OpenSequenceDiagrams());
                break;

            case NodeViewKind.GraphNeighbourhood:
            case NodeViewKind.Metadata:
                var canvas = OpenCanvas();
                if (canvas is not null)
                {
                    _ = CentreOnAsync(canvas, nodeId, nodeId);
                }
                break;
        }
    }

    private List<SequenceDiagramSurface> OpenSequenceDiagrams() =>
        Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => s.Kind == "sequence")
            .Select(s => Adapter.SurfaceContent<SequenceDiagramSurface>(s.SurfaceId))
            .OfType<SequenceDiagramSurface>()
            .ToList();

    // Feeds a node's ordered outgoing calls (Core's InteractionAsync — the real §4k feed) into open
    // sequence diagrams as a SequenceModel. Not the `calls` edges: those dedupe per pair and destroy a
    // repeated message, where an interaction preserves order and repetition (Phase E).
    internal async Task ShowNodeInSequenceDiagramsAsync(
        string nodeId, IReadOnlyList<SequenceDiagramSurface> surfaces)
    {
        if (surfaces.Count == 0 || _queries is null) { return; }
        try
        {
            var result = await _queries.InteractionAsync(nodeId, 200, CancellationToken.None);
            var calls = result.Messages
                .OrderBy(m => m.Ordinal)
                .Select(m => (m.From, m.To, m.Member))
                .ToList();
            var model = SequenceModel.Build(calls);
            foreach (var s in surfaces) { s.ShowFor(nodeId, model); }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Leave the diagrams in their current state rather than crash on a lookup failure.
        }
    }

    /// <summary>Fills any open sequence diagram not already showing the last-requested node (Phase E).</summary>
    internal void BindSequenceDiagrams()
    {
        if (_lastSequenceNodeId is not { } id) { return; }
        var surfaces = OpenSequenceDiagrams().Where(s => s.NodeId != id).ToList();
        if (surfaces.Count == 0) { return; }
        _ = ShowNodeInSequenceDiagramsAsync(id, surfaces);
    }

    private CanvasSurface? OpenCanvas() =>
        Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Select(s => Adapter.ContentFor(s.SurfaceId))
            .OfType<CanvasSurface>()
            .FirstOrDefault();

    /// <summary>
    /// Builds a graph canvas for the full-window Explorer surface (design D2), bound to the SAME
    /// workspace queries the workbench canvas reads — two graph-shaped APIs would be two answers that
    /// can disagree. A dedicated instance: the workbench's canvas is a pane in the docking tree and is
    /// never reparented across visual trees (the WebView2 airspace trap).
    /// </summary>
    public CanvasSurface CreateExplorerGraph()
    {
        var canvas = new CanvasSurface("explorer-graph", "Graph");

        // Read _queries AND the context map LIVE at load time, not captured once: the Explorer
        // surface is created lazily and then retained (US-E6), so a canvas built before the workspace
        // attached would otherwise stay bound to a null queries forever (DC-040) — and, symmetrically,
        // a VM built without the context lookup renders every node grey because colour comes from
        // context (the Explorer-graph-monochrome defect). Wiring ContextLookup here makes the Explorer
        // graph colour-consistent with the workbench graph.
        canvas.GraphSource = (rootId, ct) =>
            LoadRouted(
                new CanvasGraphViewModel(_queries) { ContextLookup = BuildContextLookup() }, rootId, ct);

        return canvas;
    }

    // The canvas asks for three kinds of view through one GraphSource seam: a described node (a real
    // id), the grouped semantic-zoom overview (GroupedOverviewRoot), or one group's contents
    // (GroupRootPrefix + id). Routed here so both canvas wirings agree on what a sentinel means, and so
    // the flat default (a null/real root) is unchanged.
    private static Task<CanvasGraph> LoadRouted(
        CanvasGraphViewModel vm, string? rootId, CancellationToken ct)
    {
        if (string.Equals(rootId, CanvasSurface.GroupedOverviewRoot, StringComparison.Ordinal))
        {
            return vm.OverviewAsync(cancellationToken: ct);
        }

        if (rootId is { } r && r.StartsWith(CanvasSurface.GroupRootPrefix, StringComparison.Ordinal))
        {
            return vm.GroupAsync(r[CanvasSurface.GroupRootPrefix.Length..], ct);
        }

        return vm.LoadAsync(rootId, cancellationToken: ct);
    }

    /// <summary>
    /// Builds the context lookup that colours graph nodes, from the current workspace's declared
    /// bounded-context map. Returns a lookup that yields null (no colour) when there is no workspace
    /// or no map. Shared by the workbench canvas and the Explorer canvas so the two colour identically.
    /// </summary>
    private Func<string, string?> BuildContextLookup()
    {
        if (string.IsNullOrEmpty(_workspaceRoot)) { return _ => null; }

        var contexts = BoundedContextReader.Load(
            Path.Combine(_workspaceRoot, BoundedContextReader.DefaultRelativePath), []);
        if (contexts.Contexts.Count == 0) { return _ => null; }

        return id => contexts.Contexts
            .FirstOrDefault(c => c.Includes.Any(p => BoundedContextReader.Matches(p, id)))?.Name;
    }

    /// <summary>
    /// The prompt-dispatch choreography for one terminal surface — shared by the focused-terminal path
    /// (Prompt.Dispatch) and the named-session path (DispatchToAsync). The shell owns the terminal, the
    /// daemon owns the receipt (D1); readiness decides whether the write is attempted at all so a
    /// prompt is never fed into a sign-in or confirmation the session is showing.
    /// </summary>
    private async Task<DispatchReceipt> DispatchToSurfaceAsync(TerminalSurface surface, string body)
    {
        if (_dispatch is null) { throw new InvalidOperationException("prompt dispatch is not available"); }

        var session = surface.Session
            ?? throw new InvalidOperationException("the terminal has not started yet");

        var command = new DispatchCommand(
            WorkspaceId: _dispatchScopeId ?? "workspace",
            WorkspaceEpoch: await _dispatch.EpochAsync(CancellationToken.None),
            Caller: new CallerPrincipal(Environment.UserName, CallerKind.Shell),
            CommandId: Guid.NewGuid().ToString("N"),
            DraftId: $"draft-{session.SessionId}",
            RevisionNo: 1,
            Body: body,
            SessionId: session.SessionId,
            SessionGeneration: session.Generation);

        var readiness = ReadinessOf(surface, session);

        return await BoundaryDispatcher.BeginAndWriteAsync(
            command, session, _dispatch.DispatchBeginAsync, _dispatch.DispatchFinalizeAsync,
            CancellationToken.None, readiness);
    }

    /// <summary>How a terminal reports readiness — OSC 133 for a shell, an observed marker for an agent,
    /// nothing otherwise (an agent not yet at its prompt is refused rather than written into).</summary>
    private static SessionReadiness ReadinessOf(TerminalSurface surface, ITerminalSession session)
    {
        var evidence = surface.ReadinessEvidence;
        var ready = evidence == ReadinessEvidence.ObservedPattern
            ? surface.AgentReadiness?.IsReady == true
            : session.Activity == SessionActivity.Ready;

        return evidence == ReadinessEvidence.None
            ? SessionReadiness.Unknown
            : ready ? SessionReadiness.Ready : SessionReadiness.NotReady;
    }

    /// <summary>
    /// Transfers a prompt-draft body to a NAMED ready session (spec-editor-surfaces US-ED6), by its
    /// session id, through the same choreography as the focused path. Returns whether the terminal
    /// accepted the write (PtyWriteAccepted); anything else leaves the draft retryable.
    /// </summary>
    public async Task<bool> DispatchToAsync(string sessionId, string body)
    {
        if (_dispatch is null) { return false; }

        var surface = TerminalSurfaces()
            .FirstOrDefault(s => string.Equals(s.Session?.SessionId, sessionId, StringComparison.Ordinal));
        if (surface is null) { return false; }

        try
        {
            var receipt = await DispatchToSurfaceAsync(surface, body);
            return receipt.State == DispatchState.PtyWriteAccepted;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return false;
        }
    }

    /// <summary>The ready terminal sessions a prompt draft may transfer to (US-ED6), live.</summary>
    public IReadOnlyList<PromptTarget> ReadyPromptTargets()
    {
        var targets = new List<PromptTarget>();
        foreach (var surface in TerminalSurfaces())
        {
            var session = surface.Session;
            if (session is null) { continue; }
            if (ReadinessOf(surface, session) != SessionReadiness.Ready) { continue; }
            targets.Add(new PromptTarget(session.SessionId, surface.DisplayName ?? surface.SurfaceId));
        }

        return targets;
    }

    /// <summary>Every live terminal surface in the current layout.</summary>
    private IEnumerable<TerminalSurface> TerminalSurfaces() =>
        Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => s.Kind == "terminal")
            .Select(s => Adapter.ContentFor(s.SurfaceId))
            .OfType<TerminalSurface>();

    /// <summary>
    /// Wires every prompt-draft surface to the shell after a render (US-ED5/ED6): the live ready
    /// targets, the named-session dispatch, the persisted body, and the save callback. Idempotent —
    /// reconcile reuses surfaces, and Configure re-reads targets each time.
    /// </summary>
    internal void BindPromptDrafts()
    {
        _promptDraftStore ??= new PromptDraftStore(
            string.IsNullOrEmpty(_workspaceRoot)
                ? Path.Combine(Path.GetTempPath(), "aide-prompt-drafts.json")
                : Path.Combine(_workspaceRoot, ".aide", "prompt-drafts.json"));

        foreach (var stack in Service.Current.AllStacks())
        {
            foreach (var surface in stack.Surfaces.Where(s => s.Kind == "prompt"))
            {
                if (Adapter.SurfaceContent<PromptDraftSurface>(surface.SurfaceId) is not { } draft) { continue; }

                var id = surface.SurfaceId;
                draft.Configure(
                    ReadyPromptTargets,
                    DispatchToAsync,
                    _promptDraftStore.TryGet(id, out var body) ? body : null,
                    text => _promptDraftStore.Save(id, text));
            }
        }
    }

    /// <summary>
    /// Feeds every class-diagram surface the current graph, from which it derives the type hierarchy
    /// (ADR-0020). Reads `_queries` live and wires the same context lookup the canvas uses, so a class
    /// diagram opened before or after the workspace attaches still populates.
    /// </summary>
    // The stack a new view should open in: the one the user is focused in, else the graph/canvas stack,
    // else any. Opening "where I am" is the least surprising placement; the canvas fallback preserves the
    // prior behaviour when nothing is focused. Read after ReconcileViewIntoModel, which only touches the
    // model (no render), so AvalonDock's active-content tracking is still valid.
    // Opens a reference-document surface (class diagram, code viewer) via DocumentPlacementPolicy so it
    // NEVER tabs on top of the graph (the "graph pane disappeared" defect): it tabs into a document
    // stack, or splits a fresh one beside the graph so both stay visible. Shared so every reference
    // document places — and is traced — identically.
    private string OpenReferenceDocument(Surface surface, string okMessage, string noPaneMessage)
    {
        ReconcileViewIntoModel();

        var placement = DocumentPlacementPolicy.Decide(Service.Current, Adapter.ActiveSurfaceId);
        if (placement is null) { return noPaneMessage; }

        LayoutResult result;
        string mode;
        if (placement.TabIntoStackId is { } tabStackId)
        {
            mode = "tab";
            result = Service.Apply(new LayoutOperation.AddSurface(tabStackId, surface));
        }
        else
        {
            mode = "split-beside-graph";

            // ADDED STRAIGHT INTO THE NEIGHBOURING ZONE, not added here and then moved.
            //
            // Add-then-move worked and had a side effect that defeated the whole point of this
            // branch: the surface landed in the graph's stack, became its active tab, and left again
            // — and removing an active tab applies CLOSE semantics, which activate the neighbour. So
            // opening a code viewer beside the graph left the graph fourth of five behind the
            // Leaderboard. Rule 3 exists so the graph stays visible, and the mechanism implementing
            // it was what hid the graph.
            //
            // Measured by the design session re-running its probe after the split fix landed: the
            // document went to zone-right correctly AND zone-center's active tab changed. Neither of
            // us would have seen it in the diff.
            var beside = ZonesToTree.ZoneOfStackId(placement.SplitBesideStackId!) == ZoneId.Center
                ? ZonesToTree.RightStackId
                : ZonesToTree.CenterStackId;

            result = Service.Apply(new LayoutOperation.AddSurface(beside, surface));
        }

        WorkbenchDiagnostics.LayoutMutation(
            $"open-{surface.Kind}", mode, surface.SurfaceId, Adapter.ActiveSurfaceId, Service.Current);

        Adapter.Render();
        BindCanvas();
        BindContexts();
        BindJoins();
        BindClassDiagrams();
        BindCodeViewers();
        BindDiagnostics();
        BindSearchSurfaces();
        BindSequenceDiagrams();
        BindTerminalAttention();

        return result.Applied ? okMessage : result.Announcement;
    }

    // Before a layout mutation that will trigger a full Render, fold any native pane drag or splitter
    // resize the user performed back into the model, so the rebuild preserves their arrangement instead
    // of reverting it. Fail-safe: ReadLayoutFromView returns null on any shape it cannot map losslessly,
    // and this is then a no-op — the pre-existing revert stands, never a corrupted layout.
    private void ReconcileViewIntoModel()
    {
        if (Adapter.ReadLayoutFromView() is { } reconciled)
        {
            Service.Restore(reconciled);
        }
    }

    // ADR-0021 dz-persist: opening a workspace RESTORES its saved zone arrangement. This is safe now
    // that the layout is zone-based: restore preserves exact placement (it cannot scatter panes the way
    // the old tree-restore did), the zone store drops surfaces the workspace can no longer provide, and
    // an absent/unreadable/corrupt save degrades to "keep the current arrangement" rather than resetting
    // to a degenerate layout. So the earlier keep-current guard is subsumed by faithful restore.
    private void RestoreArrangementOnWorkspaceOpen()
    {
        var restore = Persistence?.Restore();
        WorkbenchDiagnostics.LayoutMutation(
            "workspace-open", restore is null ? "keep-current" : "restore-zones", "layout", null, Service.Current);
    }

    // Expands a collapsed tool zone from its rail (ADR-0021 collapse-to-rail). SetStackState(Docked)
    // maps to ExpandZone on the zone service; the re-render then reincludes the zone's pane and the
    // rail hides itself.
    private void ExpandZone(ZoneId zone)
    {
        var stackId = zone switch
        {
            ZoneId.Left => ZonesToTree.LeftStackId,
            ZoneId.Right => ZonesToTree.RightStackId,
            ZoneId.Bottom => ZonesToTree.BottomStackId,
            _ => (string?)null,
        };

        if (stackId is null)
        {
            return;
        }

        Service.Apply(new LayoutOperation.SetStackState(stackId, StackState.Docked));
        Adapter.Render();
    }

    // The breadth-search provider (app-search-breadth), wired to the two Core queries that shipped for
    // it: FindAsync (cheap node/attribute lookup, safe on a keystroke) and SearchContentAsync (the file-
    // content half, behind the surface's debounce). The mapping is Core's (session-contracts §4r): a
    // member match is an attribute hit whose evidence starts "has_member"; a file hit carries the NodeId
    // of the node it belongs to, so activation navigates to a node, never a raw path (DC-022).
    internal async Task<IReadOnlyList<SearchResult>> SearchWorkspaceAsync(string term)
    {
        var q = _queries;
        if (q is null) { return new List<SearchResult>(); }

        var found = await q.FindAsync(term, 50, CancellationToken.None);
        var results = found.Matches.Select(m => new SearchResult(
            m.NodeId,
            m.MatchedOn == AiDe.Core.Store.NodeMatchKind.Attribute
                && m.Evidence?.StartsWith("has_member", StringComparison.Ordinal) == true
                    ? SearchResultKind.Member
                    : m.NodeKind.Contains("class", StringComparison.OrdinalIgnoreCase)
                      || m.NodeKind.Contains("interface", StringComparison.OrdinalIgnoreCase)
                        ? SearchResultKind.Type
                        : SearchResultKind.Node,
            m.DisplayLabel,
            m.Evidence ?? string.Empty)).ToList();

        var content = await q.SearchContentAsync(term, 50, CancellationToken.None);
        results.AddRange(content.Matches.Select(c => new SearchResult(
            c.NodeId,
            SearchResultKind.File,
            $"{c.RelativePath}:{c.Line}",
            c.Text)));

        // THE BOUND IS PART OF THE ANSWER, so it travels as a row.
        //
        // Core counts what it could not read — files too large to serve, files it could not open —
        // and reports whether the match cap fired. A list that shows twelve hits and says nothing
        // about forty unread files is a coverage claim nobody can check, which is DC-025 exactly,
        // re-entered one boundary further out than where it was fixed. Core can only make the
        // number available; only this can make it visible.
        //
        // It is a ROW rather than a field because the provider's contract is
        // `Task<IReadOnlyList<SearchResult>>` and has nowhere else to put it. A row is honest and
        // costs no change to a surface Design owns; if the shortfall deserves its own affordance
        // later, that is a shape change to agree, not a reason to drop the number now.
        var shortfall = new List<string>();

        if (content.Truncated) shortfall.Add($"first {content.Matches.Count} match(es) only");
        if (content.FilesSkipped > 0) shortfall.Add($"{content.FilesSkipped:N0} file(s) not read");

        if (shortfall.Count > 0)
        {
            results.Add(new SearchResult(
                Id: string.Empty,
                SearchResultKind.Other,
                $"Searched {content.FilesSearched:N0} file(s) — {string.Join(", ", shortfall)}",
                "This result is a lower bound. Narrow the term to see the rest."));
        }

        return results;
    }

    /// <summary>Wires each open search surface's activation to graph navigation (idempotent per surface).</summary>
    internal void BindSearchSurfaces()
    {
        var surfaces = Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => s.Kind == "search")
            .Select(s => Adapter.SurfaceContent<SearchSurface>(s.SurfaceId))
            .OfType<SearchSurface>()
            .ToList();

        foreach (var surface in surfaces)
        {
            surface.OnActivate ??= OnSearchResultActivated;   // set once — avoids rebinding on every render
        }
    }

    // A search hit is a place in the graph: centre the canvas on the hit's node, exactly as selecting a
    // join endpoint does. A file hit carries the NodeId of its file's node, so this works for every kind.
    private void OnSearchResultActivated(SearchResult hit)
    {
        var canvas = Service.Current.AllStacks()
            .SelectMany(stack => stack.Surfaces)
            .Select(surface => Adapter.ContentFor(surface.SurfaceId))
            .OfType<CanvasSurface>()
            .FirstOrDefault();

        if (canvas is null)
        {
            Announcer.Announce($"Selected {hit.Label}. Open the graph to navigate to it.");
            return;
        }

        if (_canvasGraph?.ContextFilter is not null)
        {
            _canvasGraph.ContextFilter = null;
            Announcer.Announce("Graph filter cleared.");
        }

        _ = CentreOnAsync(canvas, hit.Id, hit.Label);
    }

    internal void BindClassDiagrams()
    {
        var all = Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => s.Kind == "classdiagram")
            .Select(s => Adapter.SurfaceContent<ClassDiagramSurface>(s.SurfaceId))
            .OfType<ClassDiagramSurface>()
            .ToList();

        // Right-click "Open as…" on a type box, reusing the graph's dispatcher (Phase C/D). Idempotent
        // per surface, the same -=/+= idiom the canvas uses.
        foreach (var diagram in all)
        {
            diagram.NodeContextMenuRequested -= OnNodeContextMenuRequested;
            diagram.NodeContextMenuRequested += OnNodeContextMenuRequested;
        }

        var surfaces = all
            .Where(s => s.IsEmpty)   // first load only — avoids reloading (and flickering) on every render
            .ToList();
        if (surfaces.Count == 0) { return; }

        _ = PopulateClassDiagramsAsync(surfaces);
    }

    private async Task PopulateClassDiagramsAsync(IReadOnlyList<ClassDiagramSurface> surfaces)
    {
        foreach (var surface in surfaces) { surface.ShowLoading(); }

        try
        {
            var vm = new CanvasGraphViewModel(_queries) { ContextLookup = BuildContextLookup() };
            var graph = await vm.LoadAsync(null, cancellationToken: CancellationToken.None);
            foreach (var surface in surfaces)
            {
                surface.MembersSource = MembersForTypeAsync;   // fills each box's UML member compartment
                surface.ShowGraph(graph.Nodes, graph.Edges);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // An explicit error state, never a misleading empty "no classes" (U9).
            foreach (var surface in surfaces) { surface.ShowError(ex.Message); }
        }
    }

    // A type's declared members for the class-diagram compartment, read through the workspace's Describe
    // query (ADR-0020 Phase 2). maxNeighbors is 1 because members ride on the node itself, not its edges.
    private async Task<(IReadOnlyList<string> Members, int Declared)> MembersForTypeAsync(string typeId)
    {
        if (_queries is null) { return ([], 0); }

        var described = await _queries.DescribeAsync(typeId, 1, CancellationToken.None);
        return (described.Members ?? [], described.MembersDeclared);
    }

    // The code viewer's content source. DERIVED from `_queries` rather than assigned, because
    // `_queries` is set in two places (the constructor and AttachWorkspace) and a field set in one of
    // them is a field that is stale in the other — which is how the watcher panes came to read "not
    // available" after a workspace opened.
    //
    // It was `new MockNodeContentSource()`, written to stand in "until Core ships NodeContentAsync".
    // Core shipped it, nothing swapped the field, and the viewer went on showing a labelled SAMPLE
    // against a fully indexed workspace. A stand-in is honest only while the thing it stands in for
    // is missing; after that it is a defect wearing a feature's clothes.
    internal INodeContentSource NodeContentSource => _queries is null
        ? _mockNodeContent
        : _coreNodeContent ??= new CoreNodeContentSource(_queries);

    private readonly INodeContentSource _mockNodeContent = new MockNodeContentSource();
    private CoreNodeContentSource? _coreNodeContent;

    // The last node the user selected in the graph, so a Source pane opened AFTER a selection shows
    // that node rather than a blank — the "code viewer opened but no source" case (smoke 9-1).
    private string? _lastSelectedNodeId;

    // The last node a "Sequence diagram" action was invoked on (Phase E), so a sequence pane opened
    // for it populates from Core's InteractionAsync feed.
    private string? _lastSequenceNodeId;

    private void OnCanvasNodeSelectedForViewers(object? sender, CanvasNodeSelection selection)
    {
        _lastSelectedNodeId = selection.Node.Id;
        _ = ShowNodeInCodeViewersAsync(selection.Node.Id, OpenCodeViewers());
    }

    private List<CodeViewerView> OpenCodeViewers() =>
        Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => s.Kind == "codeviewer")
            .Select(s => Adapter.SurfaceContent<CodeViewerView>(s.SurfaceId))
            .OfType<CodeViewerView>()
            .ToList();

    // Loads one node's content into the given code viewers through the SAME source the whole app uses
    // (real when a workspace is open, mock never — see NodeContentSource). Internal so a shell test can
    // drive the routing without a WebView selection event.
    internal async Task ShowNodeInCodeViewersAsync(string nodeId, IReadOnlyList<CodeViewerView> viewers)
    {
        if (viewers.Count == 0 || _queries is null) { return; }
        try
        {
            var content = await NodeContentSource.GetAsync(nodeId, CancellationToken.None);
            foreach (var v in viewers) { v.Show(content); }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Leave the viewers in their current state rather than crash on a lookup failure.
        }
    }

    /// <summary>
    /// Feeds each open code-viewer surface content (ADR-0018/0019). Until Core's NodeContentAsync ships,
    /// this shows a labelled SAMPLE so the read-only highlighted viewer is visible and reachable; the
    /// real per-node source (following graph selection) drops in when the source is swapped.
    /// </summary>
    internal void BindCodeViewers()
    {
        var surfaces = Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => s.Kind == "codeviewer")
            .Select(s => Adapter.SurfaceContent<CodeViewerView>(s.SurfaceId))
            .OfType<CodeViewerView>()
            .Where(v => v.NodeId is null)   // first load only
            .ToList();
        if (surfaces.Count == 0) { return; }

        _ = PopulateCodeViewersAsync(surfaces);
    }

    private async Task PopulateCodeViewersAsync(IReadOnlyList<CodeViewerView> viewers)
    {
        try
        {
            // No fake sample any more (smoke 9-1: "source worked with no workspace open"). With no
            // workspace the viewer stays in its honest "Select a node to read its source." empty
            // state. With a workspace AND a prior selection, a freshly opened viewer shows that node
            // — so opening Source after clicking a node is not a blank pane (smoke 9-1: "code viewer
            // opened but no source tab").
            if (_queries is not null && _lastSelectedNodeId is { } id)
            {
                await ShowNodeInCodeViewersAsync(id, viewers);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Leaves the viewer in its fallback state rather than crashing.
        }
    }

    // Captures a completed re-index so the Diagnostics pane can show its coverage, and refreshes any
    // open pane. Called from the re-index handlers, which complete on a background thread — the pane
    // update touches WPF, so it is marshalled to the UI dispatcher.
    private AiDe.Core.Ipc.IndexSummary CaptureIndex(AiDe.Core.Ipc.IndexSummary result)
    {
        _lastIndex = result;
        _ = _dispatcher.InvokeAsync(BindDiagnostics);
        return result;
    }

    internal void BindDiagnostics()
    {
        var surfaces = Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => s.Kind == "diagnostics")
            .Select(s => Adapter.SurfaceContent<DiagnosticsSurface>(s.SurfaceId))
            .OfType<DiagnosticsSurface>()
            .ToList();
        if (surfaces.Count == 0) { return; }

        var report = BuildDiagnosticsReport();
        foreach (var surface in surfaces) { surface.Show(report); }
    }

    private DiagnosticsReport BuildDiagnosticsReport()
    {
        string? summary = null;
        IReadOnlyList<string> disclosures = [];
        var failed = 0;
        if (_lastIndex is { } idx)
        {
            summary = ConciseIndexSummary(idx);
            disclosures = AiDe.Core.Facts.DisclosureSummary.Fold(idx.Disclosures);
            failed = idx.Failed.Count;
        }

        var daemon = _diagnosticsVm?.Read().Describe();
        return new DiagnosticsReport(summary, disclosures, failed, daemon);
    }

    private static string ConciseIndexSummary(AiDe.Core.Ipc.IndexSummary idx)
    {
        var text = $"Indexed {idx.ScopesIndexed} of {idx.ScopesFound} scope(s) · {idx.Assertions:N0} assertion(s)";
        if (idx.ScopesReused > 0) { text += $" · {idx.ScopesReused} reused"; }
        if (!string.IsNullOrWhiteSpace(idx.Contexts)) { text += $" · {idx.Contexts}"; }
        return text;
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
        _episodesImported = false;
        _watcherFingerprint = null;

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
                // One-shot: import the workspace's declared-goal episodes from its audit log so real Work
                // Episodes exist to observe (ep-capture). Off the UI thread, guarded so it runs once per
                // attach; a missing log imports nothing.
                if (!_episodesImported && !string.IsNullOrEmpty(_workspaceRoot))
                {
                    _episodesImported = true;
                    var auditLog = Path.Combine(_workspaceRoot, "docs", "audit", "audit-log.jsonl");
                    host.ImportAndScoreEpisodesFromAuditLog(auditLog);
                }

                var terminals = TerminalSnapshot();
                var ids = new HashSet<string>(terminals.Select(t => t.Id), StringComparer.Ordinal);
                emitter.Reconcile(ids, id => IdentityFor(id, terminals));
                host.PumpOnce();

                // conn-9: re-render the open watcher panes only when the store actually changed, so a
                // session registering/ending, a board post, or a new score shows up live without a manual
                // reopen - and an idle watcher never gratuitously rebuilds a pane (no scroll reset/flicker).
                var fingerprint = WatcherFingerprint(host);
                if (!string.Equals(fingerprint, _watcherFingerprint, StringComparison.Ordinal))
                {
                    _watcherFingerprint = fingerprint;
                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher is not null && !token.IsCancellationRequested)
                    {
                        _ = dispatcher.BeginInvoke(RefreshWatcherPanesOnUi);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A reconcile/pump/refresh hiccup must never take down the workbench; skip this tick.
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
    /// <summary>
    /// The repository facts a session's identity carries, resolved once per workspace.
    /// </summary>
    /// <param name="Branch">
    /// The real branch, <c>HEAD</c> when detached, or <see cref="GitFacts.BranchUnknown"/> when git
    /// cannot answer. Never a value that could be mistaken for a branch name.
    /// </param>
    internal sealed record GitFacts(string RepoPath, string RepoDisplay, string WorktreePath, string Branch)
    {
        /// <summary>
        /// What the branch reads as when it could not be determined.
        /// </summary>
        /// <remarks>
        /// The parenthesised form is deliberate: <c>WorktreeBranch</c> is a required attribute the
        /// contract cannot omit (<c>SessionCoordinationIdentity.ToAttributes</c> always emits it, and
        /// a register with incomplete identity is QUARANTINED, so a missing value would delete the
        /// session from the watcher rather than annotate it). Since some string must be sent, it must
        /// be one no reader can mistake for a branch. The previous value was the literal
        /// <c>"workspace"</c> — a plausible branch name, sent for every session in every repository.
        /// </remarks>
        internal const string BranchUnknown = "(unknown)";
    }

    /// <summary>
    /// Cached per workspace: <see cref="IdentityFor"/> runs once PER TERMINAL on a 2-second loop, so
    /// resolving git on each call would spawn a process per terminal every two seconds forever.
    /// </summary>
    private GitFacts? _gitFacts;
    private string? _gitFactsFor;

    /// <summary>
    /// The harness a terminal surface was launched as, by surface id.
    /// </summary>
    /// <remarks>
    /// Recorded at launch and never inferred later, because there is no later: a second coordination
    /// register for a known session DISCARDS its attributes rather than merging them (observed —
    /// <c>CoordinationContractTests.Apply_DuplicateRegister_DiscardsTheSecondAttributes_ItDoesNotMerge</c>).
    /// A session registered without its harness stays without one for its whole life, so the value
    /// has to be in hand before the first register — which is why the harness is chosen from the
    /// menu rather than discovered from PATH.
    /// </remarks>
    private readonly Dictionary<string, AiDe.Core.Terminal.AgentReadinessProfile> _harnessBySurface = new(StringComparer.Ordinal);

    /// <summary>
    /// Resolves the repository, worktree and branch for <paramref name="root"/>, once.
    /// </summary>
    /// <remarks>
    /// <b>Worktree and repository are asked for separately and deliberately.</b> They were previously
    /// the same variable, so a linked worktree could not be distinguished from its repository by
    /// construction — the watcher could never show two worktrees of one repo as different sessions,
    /// which is precisely what the field exists to do. <c>--show-toplevel</c> gives the worktree;
    /// <c>--git-common-dir</c>'s parent gives the repository, and they differ exactly when the tree is
    /// linked.
    /// </remarks>
    internal static GitFacts ResolveGitFacts(string root)
    {
        var display = string.IsNullOrEmpty(root) ? "workspace" : Path.GetFileName(root.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(display)) { display = "workspace"; }

        var fallbackPath = string.IsNullOrEmpty(root) ? display : root;
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            return new GitFacts(fallbackPath, display, fallbackPath, GitFacts.BranchUnknown);
        }

        var worktree = Git(root, "rev-parse", "--show-toplevel") ?? fallbackPath;
        var branch = Git(root, "rev-parse", "--abbrev-ref", "HEAD") ?? GitFacts.BranchUnknown;

        // The repository is the common dir's PARENT: from a linked worktree --git-common-dir is the
        // primary .git (absolute); from the primary checkout it is a relative ".git". Its parent is
        // the repository in both cases — the same primitive coord-core resolves the record with.
        var repo = worktree;
        var common = Git(root, "rev-parse", "--git-common-dir");
        if (!string.IsNullOrEmpty(common))
        {
            try
            {
                var full = Path.IsPathRooted(common) ? common : Path.Combine(worktree, common);
                var parent = Directory.GetParent(Path.GetFullPath(full))?.FullName;
                if (!string.IsNullOrEmpty(parent)) { repo = parent; }
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
            {
                // Keep the worktree as the repository rather than inventing one.
            }
        }

        var repoDisplay = Path.GetFileName(repo.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(repoDisplay)) { repoDisplay = display; }

        return new GitFacts(Canonical(repo), repoDisplay, Canonical(worktree), branch);
    }

    /// <summary>One spelling per directory, so two sessions in one repository group together.</summary>
    /// <remarks>
    /// git answers with forward slashes and .NET's path APIs answer with backslashes, so the same
    /// directory arrives spelled two ways depending on which resolved it — and the fallback path
    /// (the workspace root) uses a third. <c>RepoPath</c> is what groups sessions in the watcher, so
    /// two spellings of one repository would present as two repositories. Normalised here rather
    /// than at each comparison, because a comparison somebody forgets is the defect.
    /// </remarks>
    private static string Canonical(string path)
    {
        if (string.IsNullOrEmpty(path)) { return path; }
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException)
        {
            return path;
        }
    }

    /// <summary>One git query. Any failure returns null — never a guessed value.</summary>
    private static string? Git(string workingDirectory, params string[] args)
    {
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo("git")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var a in args) { info.ArgumentList.Add(a); }

            using var process = System.Diagnostics.Process.Start(info);
            if (process is null) { return null; }

            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(3000)) { try { process.Kill(true); } catch { /* best effort */ } return null; }
            if (process.ExitCode != 0) { return null; }

            var trimmed = output.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // git missing, not a repository, or blocked: the caller substitutes an honest unknown.
            return null;
        }
    }

    private SessionCoordinationIdentity IdentityFor(string surfaceId, IReadOnlyList<(string Id, string Agent)> terminals)
    {
        var root = _workspaceRoot ?? string.Empty;
        if (_gitFacts is null || !string.Equals(_gitFactsFor, root, StringComparison.Ordinal))
        {
            _gitFacts = ResolveGitFacts(root);
            _gitFactsFor = root;
        }

        var facts = _gitFacts;

        var agent = terminals.FirstOrDefault(t => t.Id == surfaceId).Agent;
        if (string.IsNullOrEmpty(agent))
        {
            agent = "terminal";
        }

        // Harness only when this surface was launched AS one. Absent stays absent — never a guess
        // from the executable name, which is the same rule ToAttributes already applies (US-13).
        _harnessBySurface.TryGetValue(surfaceId, out var profile);

        return new SessionCoordinationIdentity(
            RepoPath: facts.RepoPath,
            RepoDisplay: facts.RepoDisplay,
            WorktreeBranch: facts.Branch,
            WorktreePath: facts.WorktreePath,
            TerminalId: surfaceId,
            AgentName: agent,
            Harness: profile?.HarnessId);
    }

    /// <summary>
    /// A cheap fingerprint over what the watcher panes show - session count and each session's liveness
    /// state (so a session going Stale/Ended is caught, not just a count change), plus episode, board and
    /// scorecard counts. The loop re-renders the panes only when this changes (conn-9). Read on the loop
    /// thread only; the panes are read on the UI thread when rendered.
    /// </summary>
    internal static string WatcherFingerprint(WatcherHost host)
    {
        var sessions = host.Store.AllSessions();
        var builder = new System.Text.StringBuilder();
        builder.Append(sessions.Count).Append('|');
        foreach (var session in sessions)
        {
            builder.Append(session.SessionId).Append('=')
                   .Append((int)host.Liveness.Evaluate(session.SessionId)).Append(';');
        }

        builder.Append('|').Append(host.Store.AllEpisodes().Count)
               .Append('|').Append(host.Store.AllBoardMessages().Count)
               .Append('|').Append(host.Store.AllScoredEpisodes().Count);
        return builder.ToString();
    }

    /// <summary>
    /// Marks the open watcher read panes to rebuild and renders. Runs on the UI thread (marshalled from
    /// the loop). Only the stateless watcher kinds are invalidated - a terminal is reconciled, never
    /// rebuilt (DC-029). A no-op if the host was reset since the tick was queued.
    /// </summary>
    private void RefreshWatcherPanesOnUi()
    {
        if (_watcherHost is null)
        {
            return;
        }

        Adapter.Invalidate(Service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .Where(s => WatcherPaneKinds.Contains(s.Kind))
            .Select(s => s.SurfaceId));
        Adapter.Render();
    }

    /// <summary>
    /// Raises an append-only dispute against the latest genuinely-scored episode (conn-11 / US rule 12).
    /// A no-op-with-message when the watcher is unavailable or nothing has been scored yet. The dispute
    /// records the operator's disagreement as evidence; it never changes the score.
    /// </summary>
    internal string RaiseDisputeOnLatestScore(string reason = "Operator disputes this score.")
        => _watcherHost is null
            ? "The watcher is not available."
            : RaiseDisputeOnLatest(_watcherHost.Store, TimeProvider.System, "loomkeeper-operator", reason);

    /// <summary>
    /// The pure dispute selection + append: dispute the most recently scored episode that carries a real
    /// verdict (Not-Scored has no number to dispute), via the append-only <see cref="DisputeService"/>.
    /// operatorId is a fixed local operator, never a human identity (privacy). Returns a status message.
    /// </summary>
    internal static string RaiseDisputeOnLatest(
        IWatcherObservationStore store, TimeProvider time, string operatorId, string reason)
    {
        var disputable = store.AllScoredEpisodes()
            .Where(s => s.Scorecard.Verdict != WeaveVerdict.NotScored)
            .OrderByDescending(s => s.Scorecard.EvaluatedAt)
            .FirstOrDefault();

        if (disputable is null)
        {
            return "There is no scored episode to dispute yet.";
        }

        new DisputeService(store, time).RaiseDispute(disputable.EpisodeId, operatorId, reason);
        return $"Dispute recorded against {disputable.EpisodeId} (append-only; the score is unchanged).";
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

