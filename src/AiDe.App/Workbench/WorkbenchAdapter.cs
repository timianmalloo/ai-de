using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AiDe.Core.Dispatch;
using AiDe.Core.Workbench;
using AvalonDock;
using AvalonDock.Layout;
using CoreOrientation = AiDe.Core.Workbench.Orientation;

namespace AiDe.App.Workbench;

/// <summary>
/// Renders the owned <see cref="Layout"/> model into AvalonDock, and supplies the accessibility the
/// library does not (ADR-0012).
/// </summary>
/// <remarks>
/// <para>Rendering is one-way: model → view. The view is a projection; it is never the source of
/// truth.</para>
/// <para><b>This used to claim that "pointer gestures enter as <see cref="LayoutOperation"/> requests
/// through <see cref="ILayoutService.Apply"/>, never as direct view mutations", and that is not true
/// of the running app</b> (INV-0006 §2). The workbench's own pointer pipeline —
/// <c>DropTargetResolver.Resolve</c> → <c>WorkbenchController.DragOver</c> → <c>Drop</c> →
/// <c>LayoutOperation.MoveSurface</c> — has <b>no production caller</b>: verified by exhaustive grep,
/// only <c>DragStateChanged</c> is subscribed. It is fully tested, and those tests prove nothing about
/// the app. A native tab drag is AvalonDock's own gesture and DOES mutate the view directly; it is
/// folded back into the model by <see cref="ViewArrangementChanged"/> and the shell's reconcile.</para>
/// <para>The unwired path is NOT dead code to sweep: it is the pointer half of the SC 2.5.7
/// keyboard-equivalence argument, and that claim needs re-examining by the UX &amp; Accessibility lens
/// rather than deleting. The comment was the defect; the code stays.</para>
/// </remarks>
public sealed class WorkbenchAdapter
{
    /// <summary>
    /// Automation names starting with this prefix are the library's type names leaking through as
    /// accessible names — the defect the UIA probe found (spikes/avalondock-a11y).
    /// </summary>
    public const string LeakedNamePrefix = "AvalonDock.";

    private readonly ILayoutService _service;
    private readonly Func<Surface, FrameworkElement>? _contentFactory;

    // Surface ids to REBUILD (not reuse) on the next Render - used when a workspace-dependent read pane
    // (the watcher surfaces) must be reconstructed against a factory that gained its queries after the
    // pane was first realized. Never contains a terminal id (rebuilding a terminal kills its process,
    // DC-029); the shell only ever marks the stateless watcher read surfaces.
    private readonly HashSet<string> _pendingRebuild = new(StringComparer.Ordinal);

    // Content the model still holds but the projection does not show — a collapsed zone's panes
    // (Ruling 88's Bottom; a Left rail holding the session) — kept alive, detached, until the zone
    // expands. Collapse is a HIDE, not a close: before this the projection alone decided what to keep,
    // so collapsing a zone disposed its terminal's process and, on expand, handed the factory a
    // retained session document still parented to the old island (WPF: "already the logical child of
    // another element") — the WPF lens's two Majors on SH-4.2.
    private readonly Dictionary<string, FrameworkElement> _parked = new(StringComparer.Ordinal);

    // The pane topology this adapter last SAW in the view - set by Render (what it just drew) and by
    // the drag watcher (what it just observed). Comparing against it is what turns WPF's very chatty
    // layout-pass event into "the arrangement actually changed", exactly once per change.
    private string _lastSeenArrangement = string.Empty;
    private bool _raisingArrangementChanged;
    private bool _rendering;
    private LayoutRoot? _watchedRoot;

    public WorkbenchAdapter(
        DockingManager manager, ILayoutService service,
        Func<Surface, FrameworkElement>? contentFactory = null)
    {
        Manager = manager;
        _service = service;
        _contentFactory = contentFactory;

        // The naming pass must re-run whenever the layout changes: tabs are realized and recycled as
        // panes are docked, floated and collapsed, so a one-off startup hook would name the first
        // arrangement and silently lose every one after it. Tab decoration (context menu + a working
        // close button) rides the same signal for the same reason.
        Manager.LayoutUpdated += (_, _) =>
        {
            ApplyAccessibleNames();
            DecorateTabs(Manager);
            RaiseWhenTheViewArrangementChanged();
        };

        // AvalonDock's OWN model-mutation signal, watched ALONGSIDE the WPF layout pass above.
        //
        // MEASURED, not assumed (WorkbenchDragCompletedHookTests): moving a tab into another pane
        // raises LayoutRoot.Updated once and raises Manager.LayoutUpdated ZERO times in a host that
        // is running no visual pass. Wiring the reconcile to Manager.LayoutUpdated alone — which is
        // what INV-0006 proposed — therefore produces a hook that cannot be tested at all, and rests
        // on a WPF layout pass that the docking model does not promise.
        //
        // KNOWN GAP, stated rather than implied: reordering a tab WITHIN one pane raises neither
        // signal here, so that drag still reaches the model only at the next command's reconcile. It
        // cannot produce INV-0006's defect (no surface changes column), and closing it needs a driven
        // docking host to establish what a real pointer reorder raises — the WorkbenchProbe node.
        //
        // Re-subscribed on LayoutChanged because Render REPLACES Manager.Layout, and a subscription
        // to the discarded root is a hook that silently stops firing after the first render.
        Manager.LayoutChanged += (_, _) => WatchTheDockingModel();
        WatchTheDockingModel();

        // Route AvalonDock's own close (whichever tab button or gesture triggers it) through the
        // model, so the layout stays the source of truth rather than AvalonDock silently removing a
        // document the model still has (which a later Render would then re-add).
        Manager.DocumentClosing += (_, e) =>
        {
            if (e.Document?.ContentId is { } id)
            {
                e.Cancel = true;
                CloseSurface(id);
            }
        };
    }

    /// <summary>
    /// Raised when the view's pane topology changed <b>under the model</b> — which, for a workbench
    /// whose every model-driven change goes through <see cref="Render"/>, means a native AvalonDock
    /// gesture: a tab dragged between panes, or reordered within one.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this exists (INV-0006).</b> AvalonDock owns the drag; it mutates its own tree and
    /// tells nobody. Nothing in the workbench subscribed to a drag completing, so the zone model — the
    /// source of truth — learned about a drag only when one of four <i>unrelated</i> commands happened
    /// to call <c>ReconcileViewIntoModel</c>. In the reported session that was 3m49s and eight drags
    /// later, and a reconcile handed that much drift re-derives each zone's identity by majority
    /// content overlap, crosses a column, and redraws <b>both columns on the other side</b>: eleven
    /// bystander surfaces moved by one tab drag, measured. With this event every reconcile happens one
    /// drag after the last one — the regime the zone tests already prove correct — so no change to the
    /// mapping heuristic is needed.</para>
    /// <para><b>Why it is guarded rather than raised raw.</b> <c>Manager.LayoutUpdated</c> is WPF's
    /// <see cref="UIElement.LayoutUpdated"/> — a layout-pass event, not a docking event; AvalonDock
    /// declares no event of that name. It fires on resizes, selection changes, tab realization and
    /// animation frames, most of which change no arrangement at all, and <see cref="LayoutRoot.Updated"/>
    /// fires for selection and float-property changes too. The signature comparison is what turns two
    /// chatty signals into one event per actual rearrangement.</para>
    /// </remarks>
    public event EventHandler? ViewArrangementChanged;

    public DockingManager Manager { get; }

    /// <summary>Projects the current model into AvalonDock and names everything for assistive tech.</summary>
    /// <summary>
    /// Marks surfaces to be REBUILT (not reused) on the next <see cref="Render"/>. Used by the shell
    /// when a workspace attaches and the watcher read panes - realized earlier against a factory with no
    /// watcher queries - must be reconstructed against the now-wired factory. Only stateless read
    /// surfaces are ever passed; a terminal is never rebuilt (DC-029).
    /// </summary>
    public void Invalidate(IEnumerable<string> surfaceIds)
    {
        ArgumentNullException.ThrowIfNull(surfaceIds);
        foreach (var id in surfaceIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _pendingRebuild.Add(id);
            }
        }
    }

    /// <summary>
    /// Rebuilds the content of specific surfaces <b>in place</b> — replacing each
    /// <see cref="LayoutDocument"/>'s <c>Content</c> without swapping <see cref="DockingManager.Layout"/>
    /// — so refreshing one set of panes never disturbs the others.
    /// </summary>
    /// <remarks>
    /// <para>This exists because the watcher-pane refresh used to call the full <see cref="Render"/> on
    /// every ~2s tick (a session heartbeat, a board post, a new score). A full render swaps
    /// <c>Manager.Layout</c> wholesale, which <b>re-parents every pane</b> — re-firing the graph
    /// canvas's <c>ResizeObserver</c> so it re-fits, and re-seating every tab from the model so a user
    /// sitting on Sessions/Leaderboard is snapped back to the default tab. With live agents heartbeating
    /// that made the graph "keep refreshing" and the watcher tabs impossible to stay on (smoke video
    /// 2026-09-02). Only the named surfaces are rebuilt here; layout, selection, focus and every other
    /// pane are left exactly as the user left them.</para>
    /// <para>Safe for the watcher read surfaces because they own no live process (unlike a terminal,
    /// whose ConPTY a rebuild would kill — DC-029): the old content is dropped and the factory
    /// reconstructs it against the current store.</para>
    /// </remarks>
    public void RefreshInPlace(IEnumerable<string> surfaceIds)
    {
        ArgumentNullException.ThrowIfNull(surfaceIds);
        if (Manager.Layout is not { } root)
        {
            return;
        }

        var ids = surfaceIds.Where(id => !string.IsNullOrEmpty(id)).ToHashSet(StringComparer.Ordinal);
        if (ids.Count == 0)
        {
            return;
        }

        foreach (var doc in root.Descendents().OfType<LayoutDocument>().ToList())
        {
            if (doc.ContentId is not { } id || !ids.Contains(id))
            {
                continue;
            }

            var surface = _service.Current.AllStacks()
                .SelectMany(s => s.Surfaces)
                .FirstOrDefault(s => string.Equals(s.SurfaceId, id, StringComparison.Ordinal));
            if (surface is null || _contentFactory is null)
            {
                continue;
            }

            var rebuilt = _contentFactory.Invoke(surface);
            if (doc.Content is IDisposable old)
            {
                old.Dispose();
            }

            doc.Content = rebuilt;
            doc.Title = (rebuilt as IHasDisplayName)?.DisplayName is { Length: > 0 } displayName
                ? displayName
                : surface.Title;
        }
    }

    /// <summary>
    /// Raised after a render for each surface the MODEL no longer holds — a tab the operator closed,
    /// a document a command closed, every pre-render surface after a whole-arrangement replacement
    /// — with the content that rendered it (already disposed when it was disposable). Never for a
    /// surface a collapsed zone still holds: collapse hides, it does not close. The one funnel every
    /// close passes through: AvalonDock's own close, the keyboard's, the shell's all end in a render,
    /// which is why the shell listens here for a session document leaving (its console closes with
    /// it, Ruling 89) rather than at each caller.
    /// </summary>
    public event Action<string, FrameworkElement?>? SurfaceClosed;

    /// <summary>
    /// Every surface the model holds — the projection's, plus what collapsed zones still hold — or
    /// the projection's alone for a tree service with no zone model behind it.
    /// </summary>
    private HashSet<string> HeldByTheModel() =>
        (_service is ZoneBackedLayoutService zones
            ? zones.Zones.AllSurfaces().Select(s => s.SurfaceId)
            : _service.Current.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.SurfaceId))
        .ToHashSet(StringComparer.Ordinal);

    public void Render()
    {
        // Preserve which surface is active across the layout swap. Replacing Manager.Layout wholesale
        // otherwise drops AvalonDock's active-content tracking, so focus snaps to the first document
        // (the Explorer) — the "opening/closing a pane stole focus to explore" reports (#3-focus, #11).
        var preActive = ActiveSurfaceId;
        var closed = new List<(string Id, FrameworkElement? Content)>();

        // Reconcile, do not rebuild (DC-029). Reuse the content element already realized for each
        // surface that still exists, so a mutation to ONE pane (opening a terminal, splitting,
        // restoring a layout) does not reconstruct — and thereby destroy the live state of — every
        // OTHER pane. A rebuilt terminal looks identical to the one it replaced but its process is
        // gone: each ConPTY child runs in a kill-on-close job, so orphaning its surface kills it.
        var keep = _service.Current.AllStacks()
            .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId)
            .ToHashSet(StringComparer.Ordinal);
        var held = HeldByTheModel();

        // Parked content whose surface left the model while its zone was collapsed is closed now,
        // the same way a rendered surface's is; the rest stays parked until its zone renders again.
        foreach (var (id, parked) in _parked.ToList())
        {
            if (!held.Contains(id) || _pendingRebuild.Contains(id))
            {
                _parked.Remove(id);
                (parked as IDisposable)?.Dispose();
                if (!held.Contains(id))
                {
                    closed.Add((id, parked));
                }
            }
        }

        var reuse = new Dictionary<string, FrameworkElement>(StringComparer.Ordinal);
        if (Manager.Layout is { } current)
        {
            foreach (var doc in current.Descendents().OfType<LayoutDocument>())
            {
                if (doc.ContentId is not { } id || doc.Content is not FrameworkElement fe)
                {
                    continue;
                }

                // The Center's placeholder is REBUILT on every render: its content is the empty copy
                // the shell derives from the model — "No session open" / "The session is docked at
                // the left" — and a reused element would keep the sentence of the render before
                // (Ruling 83 condition 2). It holds no resource and no operator state.
                var placeholder = string.Equals(id, ZonesToTree.WelcomePlaceholder.SurfaceId, StringComparison.Ordinal);

                if (held.Contains(id) && !_pendingRebuild.Contains(id) && !placeholder)
                {
                    // Free the element so it can re-parent into the new tree without a "already has a
                    // parent" fault when the layout is replaced below — into the next render when its
                    // zone still shows, into the park when its zone collapsed (a hide, not a close).
                    doc.Content = null;
                    if (keep.Contains(id))
                    {
                        reuse.TryAdd(id, fe);
                    }
                    else
                    {
                        _parked.TryAdd(id, fe);
                    }
                }
                else
                {
                    if (fe is IDisposable disposable)
                    {
                        // A surface that was closed - or one explicitly marked for rebuild (a workspace-
                        // dependent read pane after the workspace attached) - is ended NOW rather than at a
                        // finalizer, so a closed terminal's process stops deterministically. A rebuilt pane
                        // that owns no resource (the watcher read surfaces) simply drops here and BuildPane
                        // reconstructs it against the new content factory.
                        disposable.Dispose();
                    }

                    if (!held.Contains(id) && !placeholder)
                    {
                        closed.Add((id, fe));
                    }
                }
            }
        }

        // A zone that expanded again: its parked content is the content the pane gets back —
        // the same terminal, the same session document — never a second build.
        foreach (var id in keep)
        {
            if (_parked.Remove(id, out var parked))
            {
                reuse.TryAdd(id, parked);
            }
        }

        _pendingRebuild.Clear();

        var panel = BuildPanel(_service.Current.Root, reuse);

        // A render IS the model speaking; every mutation it makes to the docking tree must not come
        // back as "the view changed under us". What we drew becomes the arrangement the watcher has
        // seen, recorded once the whole render (selection and activation included) has settled.
        _rendering = true;
        try
        {
            Manager.Layout = new LayoutRoot { RootPanel = panel };
            RestoreSelection();
            RestoreActive(preActive);
            ApplyAccessibleNames();
        }
        finally
        {
            _rendering = false;
            _lastSeenArrangement = ViewArrangementSignature() ?? string.Empty;
        }

        // After the render has settled, so a handler that closes a dependent surface and renders
        // again nests a whole render rather than re-entering this one.
        foreach (var (id, content) in closed)
        {
            SurfaceClosed?.Invoke(id, content);
        }
    }

    /// <summary>
    /// Whether the view currently holds any rendered document. Distinguishes "nothing is on screen
    /// yet" from "the user arranged something we could not read" — which are the same <c>null</c> out
    /// of <see cref="ReadLayoutFromView"/> and very different things to record.
    /// </summary>
    internal bool HoldsDocuments() =>
        Manager.Layout?.Descendents().OfType<LayoutDocument>().Any() == true;

    private void WatchTheDockingModel()
    {
        if (ReferenceEquals(_watchedRoot, Manager.Layout))
        {
            return;
        }

        if (_watchedRoot is not null)
        {
            _watchedRoot.Updated -= OnDockingModelUpdated;
        }

        _watchedRoot = Manager.Layout;
        if (_watchedRoot is not null)
        {
            _watchedRoot.Updated += OnDockingModelUpdated;
        }
    }

    private void OnDockingModelUpdated(object? sender, EventArgs e) => RaiseWhenTheViewArrangementChanged();

    /// <summary>
    /// The view's pane topology: each document pane's surfaces, in pane order and in tab order.
    /// Null when nothing is rendered. Deliberately ignores sizes, selection and node identity — a
    /// splitter drag or a tab click is not a rearrangement.
    /// </summary>
    private string? ViewArrangementSignature()
    {
        if (Manager.Layout is not { } root)
        {
            return null;
        }

        var panes = root.Descendents().OfType<LayoutDocumentPane>()
            .Select(pane => string.Join(
                ",", pane.Children.OfType<LayoutDocument>().Select(d => d.ContentId ?? "?")));
        return string.Join("|", panes);
    }

    private void RaiseWhenTheViewArrangementChanged()
    {
        // A handler that renders would come straight back through here; one pass per change.
        if (_rendering || _raisingArrangementChanged || ViewArrangementChanged is null)
        {
            return;
        }

        var signature = ViewArrangementSignature();
        if (signature is null || string.Equals(signature, _lastSeenArrangement, StringComparison.Ordinal))
        {
            return;
        }

        // Recorded BEFORE the raise: the handler reconciles the model to this arrangement, so this is
        // now what the adapter has seen, whether the reconcile accepted it or refused it. Recording it
        // afterwards would re-raise on every layout pass for a view we cannot map.
        _lastSeenArrangement = signature;

        _raisingArrangementChanged = true;
        try { ViewArrangementChanged.Invoke(this, EventArgs.Empty); }
        finally { _raisingArrangementChanged = false; }
    }

    // Selects each pane's active tab from the model AFTER the layout is attached — AvalonDock resets a
    // pane's SelectedContentIndex when it joins the LayoutRoot, so setting it during construction does
    // not survive. Without this the rebuilt pane shows its first document, hiding the tab the user was
    // on when a close changed the active index ("both source tabs gone", #11). The zone's ActiveIndex
    // is the source of truth (DM7); document order in a pane matches surface order in its stack.
    private void RestoreSelection()
    {
        if (Manager.Layout is not { } root)
        {
            return;
        }

        foreach (var stack in _service.Current.AllStacks())
        {
            if (stack.Surfaces.Count == 0)
            {
                continue;
            }

            var activeId = stack.Surfaces[Math.Clamp(stack.ActiveIndex, 0, stack.Surfaces.Count - 1)].SurfaceId;
            var doc = root.Descendents().OfType<LayoutDocument>()
                .FirstOrDefault(d => string.Equals(d.ContentId, activeId, StringComparison.Ordinal));
            if (doc?.Parent is LayoutDocumentPane pane)
            {
                pane.SelectedContentIndex = pane.Children.IndexOf(doc);
            }
        }
    }

    // Re-activates the surface that was active before the layout was replaced, so focus stays where
    // the user had it rather than snapping to the first document. A surface that no longer exists
    // — the one just closed, or every pre-render surface after a whole-arrangement restore or reset
    // — hands the activation to the Center's active tab, the document region the operator works
    // from; RestoreSelection has already surfaced every stack's surviving tab.
    //
    // Either way the activation is ASSERTED, not merely set (see AssertActive): left to AvalonDock,
    // which content ends up active after `Manager.Layout` is replaced depends on the ORDER its pane
    // controls realize — each LayoutDocumentPaneControl marks its selected content active from its
    // own SelectionChanged as its template applies, so the last pane to realize wins, and which
    // pane that is changes with the arrangement's zone occupancy. Measured (SH-4.1,
    // docs/proof/coordination-perspective.md): in a Coding host whose Left zone is empty the Bottom
    // terminal realizes last, and it beat both a restored session document (this method's gone
    // branch) and a Center document that was active before a plain re-render (the kept branch).
    private void RestoreActive(string? surfaceId)
    {
        if (surfaceId is null || Manager.Layout is not { } root)
        {
            return;
        }

        var stack = _service.Current.FindStackOf(surfaceId);
        if (stack is null)
        {
            // Gone from the model: the Center's active tab, read from the MODEL at each assertion so
            // a Center tab the operator activated in between is what gets activated, never a stale
            // one. (A surface in another zone activated within that one dispatcher turn is the
            // accepted window: the view cannot tell that activation from the realization steal this
            // exists to undo, and the model records active tabs per stack, not the focused stack.)
            AssertActive(root, () => CenterStack() is { Surfaces.Count: > 0 } center ? center.Active.SurfaceId : null);
            return;
        }

        // Only re-focus the pre-render surface when the MODEL still considers it the active tab of its
        // stack. When the model changed the active tab (the user activated another surface), that
        // change wins — RestoreSelection has already applied it — and re-activating the stale one here
        // would clobber it back (the "activate did nothing" desync). The same rule is re-read at the
        // deferred assertion.
        AssertActive(root, () =>
            _service.Current.FindStackOf(surfaceId) is { Surfaces.Count: > 0 } now
            && string.Equals(now.Active.SurfaceId, surfaceId, StringComparison.Ordinal)
                ? surfaceId
                : null);
    }

    private StackNode? CenterStack() =>
        _service.Current.AllStacks().FirstOrDefault(st => string.Equals(st.Id, ZonesToTree.CenterStackId, StringComparison.Ordinal));

    /// <summary>
    /// Activates the surface <paramref name="intended"/> names — now, and again one dispatcher turn
    /// later at <see cref="DispatcherPriority.Loaded"/>, which runs after the layout pass that
    /// realizes the pane controls (each of which activates its own selection as it realizes, the
    /// last one winning). <paramref name="intended"/> is re-evaluated at the deferred turn so the
    /// model's answer at that moment is what is asserted; null means nothing to assert. A tree
    /// replaced by a later render is left to that render's own assertion.
    /// </summary>
    private void AssertActive(LayoutRoot root, Func<string?> intended)
    {
        if (intended() is { } now)
        {
            Activate(root, now);
        }

        Manager.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (ReferenceEquals(Manager.Layout, root) && intended() is { } later)
            {
                Activate(root, later);
            }
        });
    }

    private static void Activate(LayoutRoot root, string surfaceId)
    {
        var doc = root.Descendents().OfType<LayoutDocument>()
            .FirstOrDefault(d => string.Equals(d.ContentId, surfaceId, StringComparison.Ordinal));
        if (doc is not null)
        {
            doc.IsActive = true;
        }
    }

    /// <summary>
    /// Focuses a specific surface in the view — used right after opening a surface the user expects to
    /// interact with immediately (a terminal/agent session: you open it to type in it). This overrides
    /// the focus-preservation in <see cref="Render"/> for the deliberate open case, so focus lands on
    /// the new session rather than staying on — or snapping to — some other pane (smoke 9-2 #2).
    /// Asserted (see <see cref="AssertActive"/>): the call usually follows a <see cref="Render"/> whose
    /// pane controls have not realized yet, and the last of them to realize would otherwise win.
    /// </summary>
    internal void ActivateInView(string surfaceId)
    {
        if (Manager.Layout is not { } root)
        {
            return;
        }

        AssertActive(root, () => _service.Current.FindStackOf(surfaceId) is not null ? surfaceId : null);
    }
    /// <remarks>
    /// Without this, AvalonDock reports each tab's **.NET type name** — `AvalonDock.Layout.LayoutDocument`
    /// — as its accessible name, so every surface sounds identical to a screen reader
    /// (verified, spikes/avalondock-a11y). A typed `TabItem` style setting the same property does
    /// **not** reach these items; that was tested and rejected. Walking the realized visual tree does.
    /// </remarks>
    public void ApplyAccessibleNames() => NameTabs(Manager);

    internal static int NameTabs(DependencyObject root)
    {
        var named = 0;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TabItem tab && tab.DataContext is LayoutContent content
                && !string.IsNullOrEmpty(content.Title))
            {
                AutomationProperties.SetName(tab, content.Title);
                named++;
            }

            named += NameTabs(child);
        }

        return named;
    }

    // ── Tab decoration: a working close button and a customization context menu ─────────────────

    /// <summary>
    /// Gives every realized tab a context menu (Rename / Colour scheme / Tab colour for terminals,
    /// and Close for all) and a working close button. The rounded-tab template wires the close
    /// button's command to <c>{x:Null}</c>, so without this the tab's ✕ does nothing; routing it
    /// through <see cref="ILayoutService"/> keeps the model the source of truth.
    /// </summary>
    private void DecorateTabs(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TabItem tab
                && tab.DataContext is LayoutContent { ContentId: { } id, Content: FrameworkElement content })
            {
                // Decorate once per (tab, content) binding. Tabs are recycled, so re-check the id.
                if (!string.Equals(tab.Tag as string, id, StringComparison.Ordinal))
                {
                    tab.Tag = id;
                    tab.ContextMenu = BuildTabMenu(content, id);
                }

                var closeButton = FindDescendant<Button>(tab, "DocumentCloseButton");
                if (closeButton is not null && closeButton.Command is null)
                {
                    closeButton.Command = new RelayCommand(() => CloseSurface(id));
                }
            }

            DecorateTabs(child);
        }
    }

    private ContextMenu BuildTabMenu(FrameworkElement content, string surfaceId)
    {
        var menu = content is TerminalSurface terminal ? terminal.CreateContextMenu() : new ContextMenu();

        // Move-to-zone: a deterministic, keyboard-reachable way to send a pane to another zone (the
        // reliable counterpart to native drag). Only when the layout is zone-based (ADR-0021).
        if (_service is ZoneBackedLayoutService)
        {
            if (menu.Items.Count > 0)
            {
                menu.Items.Add(new Separator());
            }

            var moveTo = new MenuItem { Header = "Move to" };
            foreach (var (label, stackId) in new[]
            {
                ("_Left", ZonesToTree.LeftStackId),
                ("_Center", ZonesToTree.CenterStackId),
                ("_Right", ZonesToTree.RightStackId),
                ("_Bottom", ZonesToTree.BottomStackId),
            })
            {
                var target = stackId;
                var item = new MenuItem { Header = label };
                item.Click += (_, _) => MoveToZone(surfaceId, target);
                moveTo.Items.Add(item);
            }

            menu.Items.Add(moveTo);
        }

        if (menu.Items.Count > 0)
        {
            menu.Items.Add(new Separator());
        }

        var close = new MenuItem { Header = "Close" };
        close.Click += (_, _) => CloseSurface(surfaceId);
        menu.Items.Add(close);
        return menu;
    }

    // Sends a surface to a named zone through the model (never a direct view mutation), then re-renders.
    private void MoveToZone(string surfaceId, string zoneStackId)
    {
        _service.Apply(new LayoutOperation.MoveSurface(surfaceId, new DropTarget(zoneStackId, DropKind.JoinStack)));
        Render();
    }

    private void CloseSurface(string surfaceId)
    {
        // A running terminal is a live session the user set up; confirm before ending it so a stray
        // click on the tab's ✕ cannot lose it. Idle/ready terminals close instantly.
        if (ContentFor(surfaceId) is TerminalSurface { Activity: SessionActivity.Busy })
        {
            var proceed = MessageBox.Show(
                Window.GetWindow(Manager),
                "This terminal is running something. Close it and end the session?",
                "Close terminal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (proceed != MessageBoxResult.Yes)
            {
                return;
            }
        }

        _service.Apply(new LayoutOperation.CloseSurface(surfaceId));
        Render();
    }

    private static T? FindDescendant<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed && typed.Name == name)
            {
                return typed;
            }

            var found = FindDescendant<T>(child, name);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Every automation name in the subtree — the regression control's input.
    /// </summary>
    /// <remarks>
    /// Reflection cannot catch the leaked-name defect because it is a data-binding fault rather than
    /// a missing type, so the control has to read the realized tree.
    /// </remarks>
    internal static IEnumerable<string> AutomationNames(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement fe)
            {
                var name = AutomationProperties.GetName(fe);
                if (!string.IsNullOrEmpty(name))
                {
                    yield return name;
                }

                if (fe is TabItem { DataContext: LayoutContent content } && string.IsNullOrEmpty(name))
                {
                    // An unnamed tab still reports SOMETHING to UIA — the bound object's ToString().
                    // Surfacing it here is what lets the control fail on the real defect.
                    yield return content.ToString() ?? string.Empty;
                }
            }

            foreach (var nested in AutomationNames(child))
            {
                yield return nested;
            }
        }
    }

    // ── model → AvalonDock projection ─────────────────────────────────────────────────────

    private LayoutPanel BuildPanel(LayoutNode node, IReadOnlyDictionary<string, FrameworkElement> reuse)
    {
        if (node is SplitNode split)
        {
            var panel = new LayoutPanel
            {
                Orientation = split.Orientation == CoreOrientation.Horizontal
                    ? System.Windows.Controls.Orientation.Horizontal
                    : System.Windows.Controls.Orientation.Vertical,
            };

            for (var i = 0; i < split.Children.Count; i++)
            {
                // Apply the model's proportional weight to AvalonDock's own sizing. Without this every
                // pane defaulted to an equal 1* share and the split ratios were lost — which is why the
                // terminal pane sat at a fixed size the user could not change, and why a resized (or
                // restored) layout did not keep its proportions.
                var weight = new GridLength(split.Weights[i], GridUnitType.Star);
                var horizontal = split.Orientation == CoreOrientation.Horizontal;

                switch (split.Children[i])
                {
                    case SplitNode nested:
                    {
                        var childPanel = BuildPanel(nested, reuse);
                        if (horizontal)
                        {
                            childPanel.DockWidth = weight;
                        }
                        else
                        {
                            childPanel.DockHeight = weight;
                        }

                        panel.Children.Add(childPanel);
                        break;
                    }

                    case StackNode stack:
                    {
                        var pane = BuildPane(stack, reuse);
                        if (horizontal)
                        {
                            pane.DockWidth = weight;
                        }
                        else
                        {
                            pane.DockHeight = weight;
                        }

                        panel.Children.Add(pane);
                        break;
                    }
                }
            }

            return panel;
        }

        var single = new LayoutPanel();
        single.Children.Add(BuildPane((StackNode)node, reuse));
        return single;
    }

    /// <summary>
    /// The content element currently hosting <paramref name="surfaceId"/>, or null.
    /// </summary>
    /// <remarks>
    /// Read from AvalonDock's own tree by <c>ContentId</c> rather than from a parallel dictionary:
    /// a second map of surface-to-content is a second thing to keep in step with a layout the user
    /// rearranges, and it would go stale exactly when a pane is moved or closed.
    /// </remarks>
    public FrameworkElement? ContentFor(string surfaceId) =>
        (Manager.Layout?.Descendents().OfType<LayoutDocument>()
            .FirstOrDefault(d => string.Equals(d.ContentId, surfaceId, StringComparison.Ordinal))
            ?.Content as FrameworkElement)
        ?? _parked.GetValueOrDefault(surfaceId);   // hidden by a collapsed zone, still this surface's live content

    /// <summary>
    /// The inner surface content of type <typeparamref name="T"/> for <paramref name="surfaceId"/>,
    /// looking THROUGH the island chrome (<see cref="SurfaceChrome.WrapAsIsland"/>) that non-windowed
    /// panes are wrapped in.
    /// </summary>
    /// <remarks>
    /// A wrapped pane's <see cref="ContentFor"/> returns the framing <see cref="Border"/>, not the
    /// surface, so <c>ContentFor(id).OfType&lt;ClassDiagramSurface&gt;()</c> silently finds nothing and
    /// the pane never populates — the exact defect that left the class diagram (and every other wrapped
    /// surface bound by type) empty over a fully indexed workspace. Canvas and terminal are returned
    /// UNWRAPPED (airspace), so the direct-cast branch finds them; everything else is a
    /// <see cref="Border"/> whose <see cref="Border.Child"/> is the real surface. Both are handled here
    /// so no caller has to know which, and so a future wrapped kind cannot reintroduce the same silence.
    /// </remarks>
    public T? SurfaceContent<T>(string surfaceId) where T : class =>
        ContentFor(surfaceId) switch
        {
            T direct => direct,
            Border { Child: T wrapped } => wrapped,
            _ => null,
        };

    /// <summary>
    /// The surface id of the document the user is currently focused in, or null. Read from AvalonDock's
    /// own active-content tracking so a "new pane" command can open where the user is looking rather than
    /// in a fixed corner of the layout.
    /// </summary>
    public string? ActiveSurfaceId =>
        Manager.Layout?.ActiveContent?.ContentId
            ?? Manager.Layout?.Descendents().OfType<LayoutDocument>()
                .FirstOrDefault(d => d.IsSelected)?.ContentId;

    /// <summary>
    /// Reads the CURRENT AvalonDock arrangement back into the owned model, so a native pane drag or a
    /// splitter resize the user performed is captured before the next <see cref="Render"/> would rebuild
    /// from a stale model and revert it. Returns null when the view cannot be mapped confidently.
    /// </summary>
    /// <remarks>
    /// <para><b>Fail-safe by construction.</b> The model is the source of truth and a wrong reconcile
    /// would be rendered AND persisted, so this returns null the moment it meets a shape it cannot map
    /// losslessly — a floating window, an anchorable pane, an empty pane, an unknown node, a document
    /// whose surface the model does not know, or a result that does not carry exactly the same set of
    /// surfaces it started with. The caller then leaves the model untouched, degrading to the
    /// pre-existing revert-on-rebuild, never to a lost or duplicated pane.</para>
    ///
    /// <para><b>Surface identity comes from the model, not the view.</b> A <see cref="LayoutDocument"/>
    /// carries only its <c>ContentId</c> (the surface id); the Kind and Title live on the model's
    /// <see cref="Surface"/> record, looked up here, so a reconciled surface keeps the identity the rest
    /// of the system routes on. Node ids are freshly minted — they are internal and need not be stable.</para>
    /// </remarks>
    public Layout? ReadLayoutFromView()
    {
        if (Manager.Layout is not { } root) { return null; }

        // Floating windows are not mapped yet — bail rather than silently drop a floated pane.
        if (root.FloatingWindows.Any()) { return null; }

        var known = _service.Current.AllStacks()
            .SelectMany(s => s.Surfaces)
            .ToDictionary(s => s.SurfaceId, StringComparer.Ordinal);

        // The Center's placeholder is a document in the VIEW for as long as the last render left it
        // there, whatever the model's Center holds now: a drag into the empty Center reconciles the
        // model (no re-render — the view already shows the drop), so the projection stops carrying
        // the placeholder while the view still does. Always readable, never counted (F-1; O-2's
        // second half was "view-unreadable" without this — a silent revert).
        var placeholder = ZonesToTree.WelcomePlaceholder.SurfaceId;
        known.TryAdd(placeholder, ZonesToTree.WelcomePlaceholder);

        var mapped = MapNode(root.RootPanel, known);
        if (mapped is null) { return null; }

        var reconciled = _service.Current with { Root = mapped, Floating = [] };

        // The strong guard: a reconcile that lost, duplicated or invented a surface is a corrupt
        // reconcile, and rendering it would drop a pane. Compare the surface SET, and refuse if it moved.
        var before = known.Keys.Where(id => id != placeholder).ToHashSet(StringComparer.Ordinal);
        var after = reconciled.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).Where(id => id != placeholder).ToList();
        if (after.Count != before.Count || !after.ToHashSet(StringComparer.Ordinal).SetEquals(before))
        {
            return null;
        }

        return reconciled;
    }

    private static LayoutNode? MapNode(ILayoutElement element, IReadOnlyDictionary<string, Surface> known)
    {
        // A group — a LayoutPanel or a document-pane group — is an oriented split over weighted children.
        System.Windows.Controls.Orientation? orientation = element switch
        {
            LayoutPanel p => p.Orientation,
            LayoutDocumentPaneGroup g => g.Orientation,
            _ => null,
        };

        if (orientation is { } o && element is ILayoutContainer container)
        {
            var children = new List<LayoutNode>();
            var weights = new List<double>();
            foreach (var child in container.Children.OfType<ILayoutElement>())
            {
                // AvalonDock can leave an emptied LayoutDocumentPane in the tree after its last
                // document is dragged elsewhere — the zone is correctly GONE, not a shape this
                // adapter cannot read. Without this, dragging the only surface out of a one-surface
                // zone (SH-3's per-perspective defaults: Architecture's Left/Right, Coding's Left)
                // made every reconcile "view-unreadable" and silently reverted the drag, whatever it
                // was — the fail-safe firing on a shape it should recognise, not one it cannot.
                if (child is LayoutDocumentPane { Children.Count: 0 })
                {
                    continue;
                }

                var node = MapNode(child, known);
                if (node is null) { return null; }
                children.Add(node);
                weights.Add(WeightOf(child, o));
            }

            if (children.Count == 0) { return null; }
            if (children.Count == 1) { return children[0]; }   // an unsplit group is just its child

            var core = o == System.Windows.Controls.Orientation.Horizontal
                ? CoreOrientation.Horizontal
                : CoreOrientation.Vertical;
            return new SplitNode(NewNodeId("split"), core, [.. children], [.. weights]);
        }

        if (element is LayoutDocumentPane docPane)
        {
            var surfaces = new List<Surface>();
            foreach (var doc in docPane.Children.OfType<LayoutDocument>())
            {
                if (doc.ContentId is not { } id || !known.TryGetValue(id, out var surface)) { return null; }
                surfaces.Add(surface);
            }

            if (surfaces.Count == 0) { return null; }
            var active = Math.Clamp(docPane.SelectedContentIndex, 0, surfaces.Count - 1);

            // The pane's IDENTITY, where it survived the gesture: BuildPane names every rendered pane
            // with its zone's stack id, and AvalonDock moves DOCUMENTS between panes — a pane that is
            // still here is the zone it was rendered as, whatever it now holds. A pane AvalonDock
            // created for a split-off column carries no name and is minted fresh, so the mapping falls
            // back to content and position for it (ZoneBackedLayoutService.TryMapByPosition; F-1).
            var paneId = ((ILayoutPaneSerializable)docPane).Id;
            var stackId = paneId is { Length: > 0 } && ZonesToTree.ZoneOfStackId(paneId) is not null ? paneId : NewNodeId("stack");
            return new StackNode(stackId, [.. surfaces], active);
        }

        // Anchorable panes and anything else this workbench does not produce — fail safe.
        return null;
    }

    private static double WeightOf(ILayoutElement element, System.Windows.Controls.Orientation orientation)
    {
        var horizontal = orientation == System.Windows.Controls.Orientation.Horizontal;
        GridLength length = element switch
        {
            LayoutPanel p => horizontal ? p.DockWidth : p.DockHeight,
            LayoutDocumentPaneGroup g => horizontal ? g.DockWidth : g.DockHeight,
            LayoutDocumentPane dp => horizontal ? dp.DockWidth : dp.DockHeight,
            _ => default,
        };

        if (length.IsStar && length.Value > 0) { return length.Value; }
        return 1.0;   // SplitNode normalizes, so an equal share is a safe default
    }

    private static int _nodeSeq;
    private static string NewNodeId(string prefix) =>
        $"{prefix}-view-{System.Threading.Interlocked.Increment(ref _nodeSeq)}";

    private LayoutDocumentPane BuildPane(StackNode stack, IReadOnlyDictionary<string, FrameworkElement> reuse)
    {
        var pane = new LayoutDocumentPane();

        // Named for the zone it renders (the projection's deterministic stack ids), so the reconcile
        // can read a surviving pane's zone back by identity rather than guess it from its contents
        // (MapNode above). AvalonDock's own serializer is the only other reader of this id.
        ((ILayoutPaneSerializable)pane).Id = stack.Id;

        foreach (var surface in stack.Surfaces)
        {
            var content = reuse.TryGetValue(surface.SurfaceId, out var kept)
                ? kept
                : _contentFactory?.Invoke(surface) ?? new ContentControl();

            pane.Children.Add(new LayoutDocument
            {
                // ContentId is the key AvalonDock uses to reunite a restored layout with its content,
                // so it must be the surface's stable identity, not its display title.
                ContentId = surface.SurfaceId,
                // A renamed surface carries its display name on the content itself (Design-owned
                // session state). Reconcile keeps that content instance alive across re-renders, so a
                // rename persists without a Core model change; the model title is the fallback.
                Title = (content as IHasDisplayName)?.DisplayName is { Length: > 0 } displayName
                    ? displayName
                    : surface.Title,
                Content = content,
            });
        }

        // The model's active tab is applied after the layout attaches (RestoreSelection) — AvalonDock
        // resets a detached pane's selection on attach, so setting it here would not survive.
        return pane;
    }
}
