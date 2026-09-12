using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>One perspective switch, as the presenter reports it: from, to, and what asked for it.</summary>
public sealed record PerspectiveChange(Perspective From, Perspective To, string Trigger);

/// <summary>A body that could not be built: which perspective, and why (Addendum C §A9 Reliability; the rail's error state).</summary>
public sealed record PerspectiveBodyFailure(Perspective Perspective, string Reason);

/// <summary>
/// The shell-level presenter and command router (ADR-0017 as amended; ADR-0031 rule 2): owns the
/// active <see cref="Perspective"/>, the three bodies — host A, host B and the full-window Explore
/// surface — the one <i>previous-perspective</i> slot (US-C1), and the body-content swap that
/// realises a switch. Every catalog command enters through <see cref="Execute"/>, which resolves
/// the host a body-conditional command reaches and delegates to that host's
/// <see cref="WorkbenchController"/>.
/// </summary>
/// <remarks>
/// <para><b>Retain, never rebuild (the load-bearing invariant).</b> Every body is held for the
/// window's life; a switch only <i>unparents</i> the one showing. WPF keeps an unparented
/// <c>HwndHost</c>/<c>WebView2</c> child alive rather than destroying it — measured for a second
/// docking host holding a live WebView2 and a raw <c>HwndHost</c> by
/// <c>spikes/second-dock-host-unparent</c> (same <c>CoreWebView2</c>, HWND identical, zero
/// <c>DestroyWindowCore</c> across A → B → A → B ×3 and B → Explore → B) — so the swap is a view
/// change, not a session loss. The Explore surface is created on first entry and then held.</para>
///
/// <para><b>The router is table-driven, bounded (the Tech Lead's ruling).</b> Active and previous,
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
    /// <summary>The stable error code of a switch whose body could not be built.</summary>
    public const string BodyFailedCode = "AIDE-PERSPECTIVE-BODY-FAILED";

    /// <summary>The stable error code of a body-conditional command run where no docking host is the body.</summary>
    public const string NoHostCode = "AIDE-PERSPECTIVE-NO-HOST";

    private readonly ContentControl _body;
    private readonly IReadOnlyDictionary<Perspective, DockHost> _hosts;
    private readonly Func<UIElement> _explorerFactory;
    private readonly IWorkbenchAnnouncer _announcer;
    private UIElement? _explorer;

    // The stop edge still pending from the last switch: the body it was armed on and the handler.
    // A switch superseded before its body's first layout pass is NOT recorded (IO12: never a
    // plausible wrong number) — the next Activate detaches it before arming its own.
    private (FrameworkElement Body, RoutedEventHandler Handler)? _pendingShown;

    /// <param name="body">The region the docking host occupies; its content is the active perspective's projection.</param>
    /// <param name="hosts">One <see cref="DockHost"/> per host-bodied perspective (ADR-0031: host A and host B).</param>
    /// <param name="explorerFactory">Builds the full-window Explore surface, once, on first entry.</param>
    /// <param name="announcer">The one live region every switch, refusal and failure is announced through.</param>
    public PerspectiveShell(
        ContentControl body,
        IReadOnlyList<DockHost> hosts,
        Func<UIElement> explorerFactory,
        IWorkbenchAnnouncer announcer)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
        ArgumentNullException.ThrowIfNull(hosts);
        _explorerFactory = explorerFactory ?? throw new ArgumentNullException(nameof(explorerFactory));
        _announcer = announcer ?? throw new ArgumentNullException(nameof(announcer));

        _hosts = hosts.ToDictionary(h => h.Row, h => h);
        foreach (var row in PerspectiveSet.All.Where(p => p.Body == PerspectiveBody.DockHost))
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

    /// <summary>
    /// The one previous-perspective slot (US-C1): what Escape-from-Explore and the drill-to-node
    /// return restore. Initialised to Coding; after A → B → A it holds B.
    /// </summary>
    public Perspective Previous { get; private set; } = PerspectiveSet.Initial;

    /// <summary>The host of <paramref name="perspective"/>, or null for a full-window perspective.</summary>
    public DockHost? HostFor(Perspective perspective) =>
        _hosts.TryGetValue(perspective ?? throw new ArgumentNullException(nameof(perspective)), out var host) ? host : null;

    /// <summary>The active perspective's host, or null while a full-window body is showing.</summary>
    public DockHost? ActiveHost => HostFor(Active);

    /// <summary>The Explore surface once it has been created; null until first entry.</summary>
    public UIElement? ExplorerSurface => _explorer;

    /// <summary>Raised after the active perspective changes. Never for a no-op, never for a failed switch.</summary>
    public event EventHandler<PerspectiveChange>? Changed;

    /// <summary>Raised when a switch could not build its body; the active perspective is unchanged.</summary>
    public event EventHandler<PerspectiveBodyFailure>? BodyFailed;

    /// <summary>
    /// Where focus lands after a switch, once the new body has had its first layout pass (spec §C5:
    /// Coding — the active document; Explore — the reader, never the canvas trap; Architecture —
    /// the Center's active tab). Set by the window, which knows the bodies' insides; returns true
    /// when it placed focus. Unset, focus goes to the body's first focusable.
    /// </summary>
    public Func<Perspective, FrameworkElement, bool>? EntryFocus { get; set; }

    /// <summary>
    /// Makes <paramref name="perspective"/> the active one and returns what to announce. Activating
    /// the active perspective is a no-op that emits nothing (US-C1); a body that fails to build
    /// leaves the active perspective, the focus and the other bodies as they were and reports why.
    /// </summary>
    /// <param name="perspective">The perspective to show.</param>
    /// <param name="trigger">What asked for it — a catalog command id, <c>rail</c>, <c>escape</c>, <c>document-opening</c>, a replay step.</param>
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
        FrameworkElement body;
        var firstEntry = false;

        if (perspective.Body == PerspectiveBody.FullWindow)
        {
            if (_explorer is null)
            {
                try
                {
                    _explorer = _explorerFactory();   // created once, then retained (US-E6)
                    firstEntry = true;
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    // C7: the other perspectives stay usable, the rail item shows the error, and the
                    // active perspective is unchanged (spec §C4's build-failure state).
                    WorkbenchDiagnostics.ShellMode(from, perspective, trigger, firstEntry: true, outcome: "failed", errorCode: BodyFailedCode);
                    BodyFailed?.Invoke(this, new PerspectiveBodyFailure(perspective, error.Message));
                    return $"Couldn't open {perspective.Title} — {error.Message}.";
                }
            }

            body = (FrameworkElement)_explorer;
        }
        else
        {
            body = _hosts[perspective].Root;
        }

        Previous = from;
        Active = perspective;
        WorkbenchDiagnostics.ShellMode(from, perspective, trigger, firstEntry, outcome: "switched", errorCode: null);

        // The stop edge of the switch (US-C12): the new body's first Loaded after the presenter set
        // its content. Loaded fires on every re-attach of a retained body, so it is the edge for
        // first entries and returns alike; a body with no presentation source (headless) never
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
            // has a tree to land in (spec §C5: never on the rail, never lost to the window).
            if (EntryFocus?.Invoke(perspective, body) != true)
            {
                body.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        };
        body.Loaded += shown;
        _pendingShown = (body, shown);

        // The same instance returns — it was only unparented, never rebuilt.
        _body.Content = body;

        Changed?.Invoke(this, new PerspectiveChange(from, perspective, trigger));
        return Announcement(perspective);
    }

    /// <summary>
    /// Escape from the Explore surface's root restores the previous perspective (US-C1). Returns
    /// false — and does nothing — from a host, where Escape keeps its other roles.
    /// </summary>
    public bool Escape()
    {
        if (Active.Body != PerspectiveBody.FullWindow)
        {
            return false;
        }

        _announcer.Announce(Activate(Previous, "escape"));
        return true;
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
    /// the active host, or is refused with a reason while no host is the body; everything else —
    /// entry verbs, workspace verbs — reaches the initial host's controller. Returns false only for
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
            CommandScopeKind.DockHost when ActiveHost is { } host => host.Controller.Execute(commandId),
            CommandScopeKind.DockHost => RefuseWithoutHost(row),
            CommandScopeKind.Admits => RouteByKind(row.Scope.SurfaceKind ?? string.Empty, commandId, initial),
            _ => initial.Controller.Execute(commandId),
        };
    }

    private bool RouteByKind(string kind, string commandId, DockHost fallback)
    {
        var target = PerspectiveMenu.Resolve(kind, Active) is { } resolved ? HostFor(resolved) : null;
        return (target ?? fallback).Controller.Execute(commandId);
    }

    private bool RefuseWithoutHost(WorkbenchCommand row)
    {
        WorkbenchDiagnostics.ShellMode(Active, Active, row.Id, firstEntry: false, outcome: "refused", errorCode: NoHostCode);
        _announcer.Announce($"{row.Title} needs a docking host; {Active.Title} has none. Switch to Coding or Architecture first.");
        return true;
    }

    /// <summary>What a switch says (spec §C5's copy): the perspective, and for a host how many panes it shows.</summary>
    private string Announcement(Perspective perspective)
    {
        if (perspective.Body == PerspectiveBody.FullWindow)
        {
            return $"{perspective.Title} perspective: graph and reader. The workbench is retained, not closed.";
        }

        var panes = _hosts[perspective].Service.Zones.AllSurfaces().Count();
        return panes == 1
            ? $"{perspective.Title} perspective — 1 pane."
            : $"{perspective.Title} perspective — {panes} panes.";
    }
}
