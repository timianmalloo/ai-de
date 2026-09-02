using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
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
/// The adapter is deliberately **one-way**: model → view. Pointer gestures enter as
/// <see cref="LayoutOperation"/> requests through <see cref="ILayoutService.Apply"/>, never as direct
/// view mutations — that is what keeps the keyboard path and the drag path provably identical
/// (SC 2.5.7). The view is a projection; it is never the source of truth.
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
        };

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

    public void Render()
    {
        // Preserve which surface is active across the layout swap. Replacing Manager.Layout wholesale
        // otherwise drops AvalonDock's active-content tracking, so focus snaps to the first document
        // (the Explorer) — the "opening/closing a pane stole focus to explore" reports (#3-focus, #11).
        var preActive = ActiveSurfaceId;

        // Reconcile, do not rebuild (DC-029). Reuse the content element already realized for each
        // surface that still exists, so a mutation to ONE pane (opening a terminal, splitting,
        // restoring a layout) does not reconstruct — and thereby destroy the live state of — every
        // OTHER pane. A rebuilt terminal looks identical to the one it replaced but its process is
        // gone: each ConPTY child runs in a kill-on-close job, so orphaning its surface kills it.
        var keep = _service.Current.AllStacks()
            .SelectMany(s => s.Surfaces).Select(s => s.SurfaceId)
            .ToHashSet(StringComparer.Ordinal);

        var reuse = new Dictionary<string, FrameworkElement>(StringComparer.Ordinal);
        if (Manager.Layout is { } current)
        {
            foreach (var doc in current.Descendents().OfType<LayoutDocument>())
            {
                if (doc.ContentId is not { } id || doc.Content is not FrameworkElement fe)
                {
                    continue;
                }

                if (keep.Contains(id) && !_pendingRebuild.Contains(id))
                {
                    if (!reuse.ContainsKey(id))
                    {
                        // Free the element so it can re-parent into the new tree without a "already has a
                        // parent" fault when the layout is replaced below.
                        doc.Content = null;
                        reuse[id] = fe;
                    }
                }
                else if (fe is IDisposable disposable)
                {
                    // A surface that was closed - or one explicitly marked for rebuild (a workspace-
                    // dependent read pane after the workspace attached) - is ended NOW rather than at a
                    // finalizer, so a closed terminal's process stops deterministically. A rebuilt pane
                    // that owns no resource (the watcher read surfaces) simply drops here and BuildPane
                    // reconstructs it against the new content factory.
                    disposable.Dispose();
                }
            }
        }

        _pendingRebuild.Clear();

        var panel = BuildPanel(_service.Current.Root, reuse);
        Manager.Layout = new LayoutRoot { RootPanel = panel };
        RestoreSelection();
        RestoreActive(preActive);
        ApplyAccessibleNames();
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
    // (it was the one just closed) is ignored — RestoreSelection already surfaces the surviving tab.
    private void RestoreActive(string? surfaceId)
    {
        if (surfaceId is null || Manager.Layout is not { } root)
        {
            return;
        }

        // Only re-focus the pre-render surface when the MODEL still considers it the active tab of its
        // stack. When the model changed the active tab (the user activated another surface), that
        // change wins — RestoreSelection has already applied it — and re-activating the stale one here
        // would clobber it back (the "activate did nothing" desync).
        var stack = _service.Current.FindStackOf(surfaceId);
        if (stack is null || stack.Surfaces.Count == 0
            || !string.Equals(stack.Active.SurfaceId, surfaceId, StringComparison.Ordinal))
        {
            return;
        }

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
    /// </summary>
    internal void ActivateInView(string surfaceId)
    {
        if (Manager.Layout is not { } root)
        {
            return;
        }

        var doc = root.Descendents().OfType<LayoutDocument>()
            .FirstOrDefault(d => string.Equals(d.ContentId, surfaceId, StringComparison.Ordinal));
        if (doc is not null)
        {
            doc.IsActive = true;
        }
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
        Manager.Layout?.Descendents().OfType<LayoutDocument>()
            .FirstOrDefault(d => string.Equals(d.ContentId, surfaceId, StringComparison.Ordinal))
            ?.Content as FrameworkElement;

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

        var mapped = MapNode(root.RootPanel, known);
        if (mapped is null) { return null; }

        var reconciled = _service.Current with { Root = mapped, Floating = [] };

        // The strong guard: a reconcile that lost, duplicated or invented a surface is a corrupt
        // reconcile, and rendering it would drop a pane. Compare the surface SET, and refuse if it moved.
        var before = known.Keys.ToHashSet(StringComparer.Ordinal);
        var after = reconciled.AllStacks().SelectMany(s => s.Surfaces).Select(s => s.SurfaceId).ToList();
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
            return new StackNode(NewNodeId("stack"), [.. surfaces], active);
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
