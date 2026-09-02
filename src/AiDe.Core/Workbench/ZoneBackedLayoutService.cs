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

    /// <summary>Replaces the whole zone arrangement (used by persistence restore).</summary>
    public void RestoreZones(WorkbenchLayout zones)
    {
        ArgumentNullException.ThrowIfNull(zones);
        Set(zones);
    }

    public Layout Current => _projection ??= ZonesToTree.ToTree(_zones);

    public bool IsLocked { get; set; }

    public void Restore(Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        // Two callers reach here through one ILayoutService.Restore:
        //  (1) the native-drag reconcile passes a FIXED-FRAME tree — map it by POSITION so a tab
        //      dragged into another zone's pane follows the drag (not reclassified by kind);
        //  (2) persistence/migration passes an arbitrary or legacy tree — position mapping returns
        //      null and we fall back to kind-based conversion (AC-F9). A reconcile shape we cannot
        //      map confidently also returns null, and the fallback keeps the surfaces without a flip.
        Set(TryMapByPosition(layout, _zones) ?? TreeToZones.Convert(layout));
    }

    /// <summary>
    /// Reconciles a native drag from the VIEW's fixed-frame tree by POSITION only. Returns true when it
    /// mapped confidently and applied; returns <b>false without touching the model</b> when it cannot —
    /// so an unmappable drag reverts (the dragged pane snaps back) rather than falling through to the
    /// kind-based conversion, which re-seats <i>every</i> stack and moved a bystander zone on a single
    /// drag ("I moved joins and contexts moved too", smoke 9-2 #3). Kind conversion belongs only to the
    /// persistence/migration path (<see cref="Restore"/>), never to a live drag.
    /// </summary>
    public bool ReconcileFromView(Layout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var mapped = TryMapByPosition(layout, _zones);
        if (mapped is null)
        {
            return false; // not confident — leave the model as it is; the next Render reverts the drag
        }

        Set(mapped);
        return true;
    }

    /// <summary>
    /// Maps a fixed-frame tree back to zones by POSITION, using the current occupancy to disambiguate
    /// which columns are present. Returns null for any shape that is not the expected frame — the caller
    /// then falls back to kind-based conversion. This is what makes a native tab drag between zones
    /// follow the drop instead of snapping back to the surface's kind-zone.
    /// </summary>
    internal static WorkbenchLayout? TryMapByPosition(Layout tree, WorkbenchLayout current)
    {
        bool Rendered(ZoneId z) => !current.Zone(z).Collapsed && !current.Zone(z).IsEmpty;

        // Split the root into the columns row and (optionally) the bottom zone.
        LayoutNode columns = tree.Root;
        LayoutNode? bottom = null;
        if (Rendered(ZoneId.Bottom)
            && tree.Root is SplitNode { Orientation: Orientation.Vertical, Children: { Count: 2 } rootKids })
        {
            columns = rootKids[0];
            bottom = rootKids[1];
        }

        // The columns row holds the side and center zones. A native drag can INSERT a column (a new
        // side pane) or REORDER them, so the column's zone is identified by its CONTENT — which zone's
        // surfaces it already holds — never by its raw index. Index-based mapping ("first is Left, last
        // is Right") scatters a real zone the moment a dragged pane reads first or last: the reported
        // bug where a pane dropped near the right landed in the Left zone and pushed the explorers into
        // the Center (smoke 9-1 #10).
        var colChildren = columns is SplitNode { Orientation: Orientation.Horizontal } split
            ? split.Children.ToList()
            : [columns];

        // Each side/center zone's ANCHOR is the column holding the most of that zone's prior surfaces —
        // the column that IS that zone. A column that is nobody's anchor is a dragged pane (it split off,
        // or arrived new), placed by its position relative to the Center anchor. Anchoring by majority —
        // not by "a surface once lived here" — is what lets a pane dragged OUT of the Center into its own
        // column stay where it was dropped instead of snapping back to the Center.
        int? AnchorFor(ZoneId z)
        {
            var owned = current.Zone(z).Surfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
            if (owned.Count == 0)
            {
                return null;
            }

            var best = -1;
            var bestCount = 0;
            for (var i = 0; i < colChildren.Count; i++)
            {
                var count = SurfacesUnder(colChildren[i]).Count(s => owned.Contains(s.SurfaceId));
                if (count > bestCount)
                {
                    bestCount = count;
                    best = i;
                }
            }

            return best >= 0 ? best : null;
        }

        var centerAnchor = AnchorFor(ZoneId.Center);
        if (centerAnchor is not { } centerIndex)
        {
            return null; // no column carries the Center's content — not our frame; let the caller revert
        }

        var anchorZone = new Dictionary<int, ZoneId>();
        if (AnchorFor(ZoneId.Left) is { } li)
        {
            anchorZone[li] = ZoneId.Left;
        }

        if (AnchorFor(ZoneId.Right) is { } ri)
        {
            anchorZone[ri] = ZoneId.Right;
        }

        anchorZone[centerIndex] = ZoneId.Center; // the Center anchor wins any tie with a side

        var assigned = new Dictionary<ZoneId, IReadOnlyList<Surface>>
        {
            [ZoneId.Left] = new List<Surface>(),
            [ZoneId.Center] = new List<Surface>(),
            [ZoneId.Right] = new List<Surface>(),
        };

        for (var i = 0; i < colChildren.Count; i++)
        {
            var target = anchorZone.TryGetValue(i, out var z)
                ? z
                // A dragged column with no anchor joins the side it now sits on relative to the Center:
                // a drop to the right lands in the Right zone even when it was empty, a drop to the left
                // lands in Left — never merged into the Center or swapped to the wrong side.
                : i < centerIndex ? ZoneId.Left : ZoneId.Right;
            ((List<Surface>)assigned[target]).AddRange(SurfacesUnder(colChildren[i]));
        }

        if (bottom is not null)
        {
            assigned[ZoneId.Bottom] = SurfacesUnder(bottom);
        }

        // Build the result from the current model (so extents / collapsed tool zones are preserved),
        // replacing the content of each rendered zone with its position-mapped surfaces.
        var result = current;
        foreach (var id in Enum.GetValues<ZoneId>())
        {
            if (!assigned.TryGetValue(id, out var surfaces))
            {
                continue; // a collapsed/empty zone that was not rendered keeps its current content
            }

            var welcomeOnly = surfaces.Count == 1 && surfaces[0].SurfaceId == ZonesToTree.WelcomePlaceholder.SurfaceId;
            ZoneContent? content = surfaces.Count == 0 || welcomeOnly
                ? null
                : new ZoneStack([.. surfaces]);
            result = result.WithZone(result.Zone(id) with { Content = content });
        }

        // The strong guard: a reconcile that lost, duplicated or invented a surface is corrupt — refuse
        // and let the caller fall back rather than render a dropped pane.
        var before = current.AllSurfaces().Select(s => s.SurfaceId).Where(id => id != ZonesToTree.WelcomePlaceholder.SurfaceId).ToHashSet(StringComparer.Ordinal);
        var after = result.AllSurfaces().Select(s => s.SurfaceId).Where(id => id != ZonesToTree.WelcomePlaceholder.SurfaceId).ToList();
        if (after.Count != before.Count || !after.ToHashSet(StringComparer.Ordinal).SetEquals(before))
        {
            return null;
        }

        result.AssertInvariant();
        return result;
    }

    private static IReadOnlyList<Surface> SurfacesUnder(LayoutNode node)
    {
        var surfaces = new List<Surface>();
        Walk(node);
        return surfaces;

        void Walk(LayoutNode n)
        {
            switch (n)
            {
                case StackNode s:
                    surfaces.AddRange(s.Surfaces);
                    break;
                case SplitNode p:
                    foreach (var child in p.Children)
                    {
                        Walk(child);
                    }

                    break;
            }
        }
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

        // A DROP LANDS WHERE IT WAS DROPPED, whatever gesture made it.
        //
        // A zone layout cannot split WITHIN a zone — that is what zones are — so every non-float
        // drop resolves to the zone it targeted, and the split kinds differ from JoinStack only in
        // the gesture, not the destination.
        //
        // This briefly remapped the split kinds to a neighbouring zone, to make a
        // "split beside the graph" placement land beside the graph rather than on it. That was a
        // PLACEMENT POLICY translation put in a USER GESTURE handler, and it did what such a thing
        // always does: a user dragging a pane onto the left zone with a split gesture had it sent
        // to the centre, announced as "moved within the center" — a drop that reports success and
        // names a destination nobody asked for. Measured across every kind and zone by the design
        // session; only JoinStack honoured its target.
        //
        // The "beside" rule belongs to the caller that wants it, and lives there:
        // `WorkbenchShell.OpenReferenceDocument` adds a reference document straight into the
        // neighbouring zone. A policy that needs a different destination should ask for that
        // destination, not rely on the mover to reinterpret the one it gave.
        //
        // Unknown ids still fall back to the Center so a move is never silently lost.
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
