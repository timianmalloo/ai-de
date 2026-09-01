using System.Collections.Immutable;
using System.Diagnostics;

namespace AiDe.Core.Workbench;

/// <summary>
/// An <see cref="ILayoutService"/> whose real state is a <see cref="WorkbenchLayout"/> of named zones,
/// projected to a fixed-shape <see cref="Layout"/> tree for the existing adapter/persistence to render
/// (ADR-0021). Every tree-shaped operation is translated to a zone-scoped one, so an operation on one
/// pane changes only the zone(s) it names — the frame cannot "flip" (defect class DC-063). This is the
/// Strangler that lets the layout logic become zone-based without touching the adapter, controller,
/// persistence or shell wiring, all of which speak <see cref="ILayoutService"/>.
/// </summary>
public sealed class ZoneBackedLayoutService : ILayoutService
{
    private static readonly ActivitySource Activity = new("aide.workbench.operation");
    private int _floatSeq;

    private WorkbenchLayout _zones;
    private Layout? _projection;

    public ZoneBackedLayoutService(WorkbenchLayout? initial = null)
    {
        _zones = initial ?? WorkbenchLayout.Default();
    }

    /// <summary>The zone model — the real source of truth behind the projected tree.</summary>
    public WorkbenchLayout Zones => _zones;

    public Layout Current => _projection ??= ZonesToTree.ToTree(_zones);

    public bool IsLocked { get; set; }

    public void Restore(Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        Set(TreeToZones.Convert(layout));
    }

    public LayoutResult Apply(LayoutOperation operation)
    {
        using var activity = Activity.StartActivity("aide.workbench.operation");
        activity?.SetTag("operation.kind", operation.GetType().Name);
        activity?.SetTag("layout.model", "zones");

        if (IsLocked && operation is not LayoutOperation.ActivateSurface)
        {
            return Refuse(LayoutErrorCodes.Locked, "Layout is locked. Unlock to rearrange panes.");
        }

        ZoneLayoutResult zoneResult;
        try
        {
            zoneResult = operation switch
            {
                LayoutOperation.MoveSurface op => Move(op),
                LayoutOperation.AddSurface op => ZoneLayoutService.OpenPane(_zones, op.Surface, ZoneFor(op.StackId)),
                LayoutOperation.CloseSurface op => ZoneLayoutService.ClosePane(_zones, op.SurfaceId),
                LayoutOperation.ActivateSurface op => ZoneLayoutService.Activate(_zones, op.SurfaceId),
                LayoutOperation.ReorderSurface op => Reorder(op),
                LayoutOperation.ResizeSplit op => Resize(op),
                LayoutOperation.SetStackState op => SetState(op),
                LayoutOperation.ResetToDefault => new ZoneLayoutResult(
                    WorkbenchLayout.Default(), true, null, "Workbench layout reset to the default."),
                _ => new ZoneLayoutResult(_zones, false, LayoutErrorCodes.InvalidTarget, "Unsupported layout operation."),
            };
        }
        catch (InvalidOperationException ex)
        {
            return Refuse(LayoutErrorCodes.InvalidTarget, ex.Message);
        }

        if (zoneResult.Applied)
        {
            Set(zoneResult.Layout);
        }

        activity?.SetTag("outcome", zoneResult.Applied ? "applied" : "refused");
        activity?.SetTag("error.code", zoneResult.RefusalCode);
        return new LayoutResult(Current, zoneResult.Applied, zoneResult.RefusalCode, zoneResult.Announcement);
    }

    // ── operation translation ──────────────────────────────────────────────────────────────

    private ZoneLayoutResult Move(LayoutOperation.MoveSurface op)
    {
        if (op.Target.Kind == DropKind.Float)
        {
            return Float(op.SurfaceId);
        }

        // A drop targets a zone: map the target node id back to its zone. Splits and unknown ids fall
        // back to the Center so a move is never silently lost.
        var zone = ZonesToTree.ZoneOfStackId(op.Target.TargetNodeId) ?? ZoneId.Center;
        return ZoneLayoutService.MovePane(_zones, op.SurfaceId, zone);
    }

    private ZoneLayoutResult Float(string surfaceId)
    {
        var zoneId = _zones.FindZoneOf(surfaceId);
        if (zoneId is null)
        {
            return new ZoneLayoutResult(_zones, false, LayoutErrorCodes.SurfaceUnknown, $"No surface “{surfaceId}”.");
        }

        var surface = _zones.Zone(zoneId.Value).Surfaces().First(s => s.SurfaceId == surfaceId);
        var removed = ZoneLayoutService.ClosePane(_zones, surfaceId).Layout;
        var floated = removed with
        {
            Floating = removed.Floating.Add(
                new StackNode($"float-{++_floatSeq}", [surface], 0, StackState.Floating)),
        };
        return new ZoneLayoutResult(floated, true, null, $"{surface.Title} is now floating.");
    }

    private ZoneLayoutResult Reorder(LayoutOperation.ReorderSurface op)
    {
        var zoneId = ZonesToTree.ZoneOfStackId(op.StackId);
        if (zoneId is null || _zones.Zone(zoneId.Value).Content is not ZoneStack stack)
        {
            return new ZoneLayoutResult(_zones, false, LayoutErrorCodes.InvalidTarget, "Cannot reorder that pane.");
        }

        var from = Math.Clamp(op.From, 0, stack.Tabs.Count - 1);
        var to = Math.Clamp(op.To, 0, stack.Tabs.Count - 1);
        if (from == to)
        {
            return new ZoneLayoutResult(_zones, false, null, string.Empty);
        }

        var moved = stack.Tabs[from];
        var tabs = stack.Tabs.RemoveAt(from).Insert(to, moved);
        var next = _zones.WithZone(_zones.Zone(zoneId.Value) with
        {
            Content = new ZoneStack(tabs, tabs.IndexOf(moved)),
        });
        return new ZoneLayoutResult(next, true, null, $"Reordered {moved.Title}.");
    }

    private ZoneLayoutResult Resize(LayoutOperation.ResizeSplit op)
    {
        // Map a split-edge resize to a zone extent nudge. The bottom row and the side columns are the
        // only resizable boundaries; the Center absorbs the remainder (AC-F6).
        var zone = op.SplitId switch
        {
            ZonesToTree.RootSplitId => ZoneId.Bottom,
            ZonesToTree.ColumnsSplitId => op.EdgeIndex == 0 && ColumnsStartWithLeft() ? ZoneId.Left : ZoneId.Right,
            _ => (ZoneId?)null,
        };

        if (zone is null)
        {
            return new ZoneLayoutResult(_zones, false, LayoutErrorCodes.InvalidTarget, "That edge cannot be resized.");
        }

        // Bottom grows when the top-of-bottom edge moves up (negative delta); a side grows on a positive delta.
        var current = _zones.Zone(zone.Value).Extent;
        var next = zone == ZoneId.Bottom ? current - op.Delta : current + op.Delta;
        return ZoneLayoutService.ResizeZone(_zones, zone.Value, next);
    }

    private ZoneLayoutResult SetState(LayoutOperation.SetStackState op)
    {
        var zoneId = ZonesToTree.ZoneOfStackId(op.StackId);
        if (zoneId is null)
        {
            return new ZoneLayoutResult(_zones, false, LayoutErrorCodes.InvalidTarget, "Unknown pane.");
        }

        return op.State switch
        {
            StackState.Collapsed or StackState.Hidden => ZoneLayoutService.CollapseZone(_zones, zoneId.Value),
            StackState.Docked => ZoneLayoutService.ExpandZone(_zones, zoneId.Value),
            StackState.Maximized => ZoneLayoutService.Maximize(_zones, zoneId.Value),
            StackState.Floating => Float(_zones.Zone(zoneId.Value).Surfaces().FirstOrDefault()?.SurfaceId ?? string.Empty),
            _ => new ZoneLayoutResult(_zones, false, LayoutErrorCodes.InvalidTarget, "Unsupported state."),
        };
    }

    private bool ColumnsStartWithLeft() =>
        !_zones.Zone(ZoneId.Left).Collapsed && !_zones.Zone(ZoneId.Left).IsEmpty;

    private static ZoneId ZoneFor(string stackId) => ZonesToTree.ZoneOfStackId(stackId) ?? ZoneId.Center;

    private void Set(WorkbenchLayout zones)
    {
        zones.AssertInvariant();
        _zones = zones;
        _projection = null; // invalidate the cached tree projection
    }

    private LayoutResult Refuse(string code, string announcement) =>
        new(Current, false, code, announcement);
}
