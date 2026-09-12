using System.Windows;
using System.Windows.Automation;
using AiDe.Core.Workbench;
using AvalonDock;

namespace AiDe.App.Workbench;

/// <summary>
/// One docking host, composed as a unit (ADR-0031 rule 1): the manager, the adapter that projects
/// the model into it, the zone service that IS the model, the controller that dispatches layout
/// commands over that service, the collapse-to-rail strips, and — once a workspace attaches — the
/// persistence of its own slot. <see cref="WorkbenchShell"/> composes it twice: host A (Coding,
/// today's instances) and host B (Architecture).
/// </summary>
/// <remarks>
/// <para><b>A positional record plus <see cref="Create"/> and <see cref="Dispose"/>, no behaviour</b>
/// (the Tech Lead's bound). The record is what makes "a member host B cannot take" mechanical: a
/// shell member that is not one of these six is workspace-level by construction — the factory, the
/// session documents, the watcher, dispatch, the canvas binding.</para>
///
/// <para><b>One controller per host</b> (the Owner's residual, decided in ADR-0031): a controller
/// carries per-host focus and a keyboard-resize session over its service, so a second host is a
/// second controller. The shell-level router (<see cref="PerspectiveShell"/>) resolves which one a
/// command reaches.</para>
/// </remarks>
/// <param name="Row">The perspective row this host bodies (ADR-0030).</param>
/// <param name="Service">The host's zone model, guarded by the row's allow-list.</param>
/// <param name="Manager">The AvalonDock host.</param>
/// <param name="Adapter">Projects <paramref name="Service"/> into <paramref name="Manager"/>.</param>
/// <param name="Controller">Layout-command dispatch over <paramref name="Service"/>.</param>
/// <param name="Rails">The collapse-to-rail strips around <paramref name="Manager"/> (ADR-0021).</param>
public sealed record DockHost(
    Perspective Row,
    ZoneBackedLayoutService Service,
    DockingManager Manager,
    WorkbenchAdapter Adapter,
    WorkbenchController Controller,
    ZoneRails Rails) : IDisposable
{
    /// <summary>The element the presenter shows as this perspective's body: the rails wrapping the manager.</summary>
    public FrameworkElement Root => Rails.Root;

    /// <summary>Saves and restores this host's own slot (ADR-0032). Null until a workspace attaches.</summary>
    public LayoutPersistence? Persistence { get; set; }

    /// <summary>
    /// Composes one host for <paramref name="row"/> over the shared factory and announcer
    /// (ADR-0031's parameterised Factory Method).
    /// </summary>
    /// <param name="row">The perspective whose body this is; its allow-list becomes the service's admission.</param>
    /// <param name="content">The one shared <see cref="SurfaceContentFactory"/>, read through a delegate so the shell can replace the factory when a workspace attaches.</param>
    /// <param name="announcer">The one shared live region.</param>
    /// <param name="expandZone">What a collapsed zone's rail does when clicked — the shell's <c>ExpandZone</c>, bound to this host.</param>
    public static DockHost Create(
        Perspective row,
        Func<Surface, FrameworkElement> content,
        IWorkbenchAnnouncer announcer,
        Action<DockHost, ZoneId> expandZone)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(announcer);
        ArgumentNullException.ThrowIfNull(expandZone);

        if (row.Body != PerspectiveBody.DockHost)
        {
            throw new ArgumentException($"'{row.Id}' is a full-window perspective; it has no docking host", nameof(row));
        }

        // ADR-0021: the layout is zone-based; the Strangler service projects zones to the tree the
        // adapter renders. ADR-0031 rule 3: its admission is the row's allow-list.
        var service = new ZoneBackedLayoutService(AdmissionFor(row));

        var manager = new DockingManager();
        AutomationProperties.SetName(manager, $"{row.Title} perspective body");

        var adapter = new WorkbenchAdapter(manager, service, content);
        var controller = new WorkbenchController(service, announcer);

        DockHost? host = null;
        var rails = new ZoneRails(manager, () => service.Zones, zone => expandZone(host!, zone));
        manager.LayoutChanged += (_, _) => rails.Refresh();

        host = new DockHost(row, service, manager, adapter, controller, rails);
        return host;
    }

    /// <summary>
    /// The row's allow-list and one-instance rule as the service's admission — every kind row's
    /// <c>Perspectives</c> and <c>Instances</c> columns, handed down as data (ADR-0030 rule 2).
    /// </summary>
    public static SurfaceAdmission AdmissionFor(Perspective row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new SurfaceAdmission(row, SurfaceContentFactory.Kinds.ToDictionary(
            k => k.Kind,
            k => new KindRule(k.Perspectives, k.Instances == SurfaceContentFactory.Instances.One),
            StringComparer.Ordinal));
    }

    public void Dispose() => Persistence?.Dispose();
}
