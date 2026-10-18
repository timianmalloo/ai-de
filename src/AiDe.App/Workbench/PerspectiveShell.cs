using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>One perspective switch, as the presenter reports it: from, to, and what asked for it.</summary>
public sealed record PerspectiveChange(Perspective From, Perspective To, string Trigger);

/// <summary>
/// The shell-level presenter and command router (ADR-0017 as amended; ADR-0031 rule 2): owns the
/// active <see cref="Perspective"/>, the three bodies — one docking host per row (ADR-0045 §3) —
/// and the body-content swap that realises a switch. Every catalog command enters through
/// <see cref="Execute"/>, which resolves
/// the host a body-conditional command reaches and delegates to that host's
/// <see cref="WorkbenchController"/>.
/// </summary>
/// <remarks>
/// <para><b>Retain, never rebuild (the load-bearing invariant).</b> Every body is held for the
/// window's life; a switch only <i>unparents</i> the one showing. WPF keeps an unparented
/// <c>HwndHost</c>/<c>WebView2</c> child alive rather than destroying it — measured for a second
/// docking host holding a live WebView2 and a raw <c>HwndHost</c> by
/// <c>spikes/second-dock-host-unparent</c> (same <c>CoreWebView2</c>, HWND identical, zero
/// <c>DestroyWindowCore</c> across A → B → A → B ×3) — so the swap is a view change, not a session
/// loss. Every host is built at construction, so a switch never builds a body.</para>
///
/// <para><b>The router is table-driven, bounded (the Tech Lead's ruling).</b> The active perspective,
/// the bodies, <see cref="Execute"/> = resolve the host then delegate, the perspective commands, the
/// entry-verb rule. The host a command reaches is read from the command's own
/// <see cref="CommandScope"/> and the kind rows' allow-lists — <b>a <c>switch</c> on any other
/// command id inside this class is the falsifier.</b> Entry verbs and workspace verbs
/// (<see cref="CommandScopeKind.Global"/>) reach the initial host, whose controller carries the
/// shell's workspace delegates; the switch that follows an entry verb happens through the shell's
/// <c>DocumentOpening</c> seam, document first (US-C5, US-C11).</para>
///
/// <para><b>The perspective is in the log, not inferred from it (INV-0009, IO1).</b> One
/// <c>shell.mode</c> line per change, naming the trigger and the outcome; a second line when the
/// new body has had its first layout pass carries the measured switch duration (US-C12). A call
/// that changes nothing writes nothing.</para>
/// </remarks>
public sealed class PerspectiveShell
{
    private readonly ContentControl _body;
    private readonly IReadOnlyDictionary<Perspective, DockHost> _hosts;
    private readonly IWorkbenchAnnouncer _announcer;

    // The stop edge still pending from the last switch: the body it was armed on and the handler.
    // A switch superseded before its body's first layout pass is NOT recorded (IO12: never a
    // plausible wrong number) — the next Activate detaches it before arming its own.
    private (FrameworkElement Body, RoutedEventHandler Handler)? _pendingShown;

    /// <param name="body">The region the docking host occupies; its content is the active perspective's projection.</param>
    /// <param name="hosts">One <see cref="DockHost"/> per row of <see cref="PerspectiveSet.All"/> (ADR-0045 §3).</param>
    /// <param name="announcer">The one live region every switch is announced through.</param>
    public PerspectiveShell(
        ContentControl body,
        IReadOnlyList<DockHost> hosts,
        IWorkbenchAnnouncer announcer)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
        ArgumentNullException.ThrowIfNull(hosts);
        _announcer = announcer ?? throw new ArgumentNullException(nameof(announcer));

        _hosts = hosts.ToDictionary(h => h.Row, h => h);
        foreach (var row in PerspectiveSet.All)
        {
            if (!_hosts.ContainsKey(row))
            {
                throw new ArgumentException($"no host was composed for the '{row.Id}' perspective", nameof(hosts));
            }
        }

        _body.Content = _hosts[PerspectiveSet.Initial].Root;
    }

    /// <summary>The active perspective; <see cref="PerspectiveSet.Initial"/> until a switch.</summary>
    public Perspective Active { get; private set; } = PerspectiveSet.Initial;

    /// <summary>The host of <paramref name="perspective"/> — every row has one.</summary>
    public DockHost HostFor(Perspective perspective) =>
        _hosts[perspective ?? throw new ArgumentNullException(nameof(perspective))];

    /// <summary>The active perspective's host.</summary>
    public DockHost ActiveHost => HostFor(Active);

    /// <summary>Raised after the active perspective changes. Never for a no-op.</summary>
    public event EventHandler<PerspectiveChange>? Changed;

    /// <summary>
    /// Where focus lands after a switch, once the new body has had its first layout pass (spec §C5:
    /// the row's <see cref="Perspective.Landing"/> zone's active tab — <see cref="LandingSurfaceFor"/>). Set by the window, which knows the bodies' insides; returns true
    /// when it placed focus. Unset, focus goes to the body's first focusable.
    /// </summary>
    public Func<Perspective, FrameworkElement, bool>? EntryFocus { get; set; }

    /// <summary>
    /// The surface a switch to <paramref name="host"/>'s perspective lands focus on (spec §C5): the
    /// active tab of the row's <see cref="Perspective.Landing"/> zone when the arrangement has that
    /// zone open with content; else the Center's active tab — the document region, and for an empty
    /// Center its empty copy (the placeholder the shell renders, Ruling 83 condition 2); else the
    /// body's active surface as the view reports it. The window's
    /// <see cref="EntryFocus"/> reads this, then moves focus into that surface's content.
    /// </summary>
    public static string? LandingSurfaceFor(DockHost host)
    {
        ArgumentNullException.ThrowIfNull(host);

        var zone = host.Service.Zones.Zone(host.Row.Landing);
        if (!zone.Collapsed && FirstActive(zone.Content) is { } landing)
        {
            return landing.SurfaceId;
        }

        // The landing zone is empty or collapsed: the Center's active tab is a chosen landing
        // where the view's "active" is whichever pane realized last (DC-194).
        var center = host.Service.Zones.Zone(ZoneId.Center);
        return center.IsEmpty ? ZonesToTree.WelcomePlaceholder.SurfaceId : FirstActive(center.Content)?.SurfaceId ?? host.Adapter.ActiveSurfaceId;

        static Surface? FirstActive(ZoneContent? content) => content switch
        {
            ZoneStack stack when stack.Tabs.Count > 0 => stack.Active,
            ZoneSplit split => split.Children.Select(FirstActive).FirstOrDefault(s => s is not null),
            _ => null,
        };
    }

    /// <summary>
    /// Moves focus into a landing surface's content (spec §C5; DESIGN.md's landing row): a session
    /// document lands in its <b>editor</b> — the first action, where the operator types — and any
    /// other surface on its first focusable. Returns whether focus was placed.
    /// </summary>
    /// <remarks>
    /// <c>MoveFocus(First)</c> on a session document lands on the header's first button (the
    /// session settings), not the editor — the UX lens's finding on SH-4.2; the document's own
    /// <see cref="Sessions.SessionDocumentSurface.FocusRegion"/> knows where the editor is, page
    /// or first line.
    /// </remarks>
    public static bool FocusLanding(FrameworkElement content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var document = content as Sessions.SessionDocumentSurface ?? (content as Border)?.Child as Sessions.SessionDocumentSurface;
        if (document is not null && document.FocusRegion(Sessions.SessionDocumentSurface.Region.Composer, "landing").Outcome == CanvasFocusOutcome.Entered)
        {
            return true;
        }

        return content.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }

    /// <summary>
    /// Makes <paramref name="perspective"/> the active one and returns what to announce. Activating
    /// the active perspective is a no-op that emits nothing (US-C1).
    /// </summary>
    /// <param name="perspective">The perspective to show.</param>
    /// <param name="trigger">What asked for it — a catalog command id, <c>rail</c>, <c>document-opening</c>, a replay step.</param>
    public string Activate(Perspective perspective, string trigger)
    {
        ArgumentNullException.ThrowIfNull(perspective);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

        if (perspective == Active)
        {
            // A no-op is still answered: a command that does its work without saying so is
            // indistinguishable from a dead key (DC-011). Nothing switches and no event fires.
            return $"{perspective.Title} perspective is already showing.";
        }

        var started = Stopwatch.GetTimestamp();
        var from = Active;
        var body = _hosts[perspective].Root;

        Active = perspective;
        WorkbenchDiagnostics.ShellMode(from, perspective, trigger);

        // The stop edge of the switch (US-C12): the new body's first Loaded after the presenter set
        // its content. Loaded fires on every re-attach of a retained body, so it is the edge for
        // every switch; a body with no presentation source (headless) never
        // fires it, and the duration is then simply not recorded — never modelled. A switch that
        // is superseded before its edge fires is detached here and never measured either.
        if (_pendingShown is { } stale)
        {
            stale.Body.Loaded -= stale.Handler;
            _pendingShown = null;
        }

        RoutedEventHandler? shown = null;
        shown = (_, _) =>
        {
            body.Loaded -= shown;
            _pendingShown = null;
            WorkbenchDiagnostics.ShellModeShown(perspective, trigger, Stopwatch.GetElapsedTime(started).TotalMilliseconds);

            // The announcement was queued before this; focus lands in the new body only now that it
            // has a tree to land in (spec §C5: never on the rail, never lost to the window) — and
            // one dispatcher turn later than this edge. Measured (SH-4.1, the census window): a
            // landing placed here directly was overridden by the docking pane controls' own
            // activation of their selected content as they realize, the last one winning (the
            // Right's Provenance for Architecture as then laid out, the Center's last tab for
            // Coordination); one placed at
            // Loaded priority from here runs after every pending realization and holds. A switch
            // superseded in between places nothing.
            body.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (!ReferenceEquals(_body.Content, body))
                {
                    return;
                }

                if (EntryFocus?.Invoke(perspective, body) != true)
                {
                    body.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                }
            });
        };
        body.Loaded += shown;
        _pendingShown = (body, shown);

        // The same instance returns — it was only unparented, never rebuilt.
        _body.Content = body;

        Changed?.Invoke(this, new PerspectiveChange(from, perspective, trigger));
        return Announcement(perspective);
    }

    /// <summary>
    /// The shell's <c>DocumentOpening</c> seam (ADR-0017 amendment clause 4, generalised): a dock
    /// document is about to open in <paramref name="host"/>'s body; if that body is not on screen,
    /// it becomes the body — document first, then the switch, as one transaction. Retained, never
    /// rebuilt: a host perspective's own openers never switch it away (the reviewers' finding).
    /// </summary>
    public void OnDocumentOpening(Perspective host)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (host != Active)
        {
            Activate(host, "document-opening");
        }
    }

    /// <summary>
    /// Runs a catalog command by id: a perspective command switches; a derived opener and a
    /// kind-scoped command reach the host that admits the kind (the active one when it does,
    /// else the first in routing order — ADR-0030 <c>Resolve</c>); a host-scoped command reaches
    /// the active host; everything else — entry verbs, workspace verbs — reaches the initial host's
    /// controller. Returns false only for
    /// an id no controller knows.
    /// </summary>
    public bool Execute(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        if (PerspectiveSet.ByCommandId(commandId) is { } perspective)
        {
            _announcer.Announce(Activate(perspective, commandId));
            return true;
        }

        var initial = _hosts[PerspectiveSet.Initial];

        if (PerspectiveMenu.TryParseOpener(commandId, out _, out var kind))
        {
            return RouteByKind(kind, commandId, initial);
        }

        var row = WorkbenchCommandCatalog.All.FirstOrDefault(c => string.Equals(c.Id, commandId, StringComparison.Ordinal));
        return row?.Scope.Kind switch
        {
            CommandScopeKind.DockHost => ActiveHost.Controller.Execute(commandId),
            CommandScopeKind.Admits => RouteByKind(row.Scope.SurfaceKind ?? string.Empty, commandId, initial),
            _ => initial.Controller.Execute(commandId),
        };
    }

    private bool RouteByKind(string kind, string commandId, DockHost fallback)
    {
        var target = PerspectiveMenu.Resolve(kind, Active) is { } resolved ? HostFor(resolved) : fallback;
        return target.Controller.Execute(commandId);
    }

    /// <summary>What a switch says (spec §C5's copy): the perspective, and how many panes its host shows.</summary>
    private string Announcement(Perspective perspective)
    {
        var panes = _hosts[perspective].Service.Zones.AllSurfaces().Count();
        return panes == 1
            ? $"{perspective.Title} perspective — 1 pane."
            : $"{perspective.Title} perspective — {panes} panes.";
    }
}
