using System.Collections.Immutable;

namespace AiDe.Core.Workbench;

/// <summary>
/// The four named, absolute regions of the workbench frame. Unlike the proportional split tree
/// (<see cref="Layout"/>), these are <b>stable containers</b>: the frame never restructures, so an
/// operation on a pane can only change the zone(s) that pane belongs to — never relocate or reorient
/// an unrelated pane (defect class DC-063). See <c>adr-0021-named-dock-zones</c>.
/// </summary>
public enum ZoneId
{
    /// <summary>Vertical tool stack on the left (explorers, parked panels). Collapses to a rail.</summary>
    Left,

    /// <summary>Vertical tool stack on the right. Collapses to a rail.</summary>
    Right,

    /// <summary>Horizontal tool stack along the bottom (terminals, diagnostics, output). Collapses to a rail.</summary>
    Bottom,

    /// <summary>The document / editor anchor. Always present; never collapses; may split into editor groups.</summary>
    Center,
}

/// <summary>
/// What a zone holds: either a single tab stack or, within the Center, a split into editor groups.
/// </summary>
/// <remarks>
/// The load-bearing rule is that a <see cref="ZoneSplit"/>'s children never leave the zone — a split
/// is <i>scoped to its zone</i>. That is what keeps the top-level frame from being a tree: there is
/// no operation that restructures the relationship <i>between</i> zones. A zone with no content is
/// represented by a null <see cref="ZoneState.Content"/> (a rail for a tool zone; a placeholder for
/// the Center), not by an empty stack — an empty <see cref="ZoneStack"/> is not constructible.
/// </remarks>
public abstract record ZoneContent
{
    /// <summary>Every surface this content holds, in tab/traversal order.</summary>
    public abstract IEnumerable<Surface> Surfaces();
}

/// <summary>A tab stack of one or more surfaces. The unit v1 tool zones are built from.</summary>
public sealed record ZoneStack : ZoneContent
{
    public ZoneStack(ImmutableList<Surface> surfaces, int activeIndex = 0)
    {
        if (surfaces.Count == 0)
        {
            throw new ArgumentException("a zone stack holds at least one surface", nameof(surfaces));
        }

        Tabs = surfaces;
        ActiveIndex = Math.Clamp(activeIndex, 0, surfaces.Count - 1);
    }

    public ImmutableList<Surface> Tabs { get; init; }

    public int ActiveIndex { get; init; }

    public Surface Active => Tabs[ActiveIndex];

    public override IEnumerable<Surface> Surfaces() => Tabs;
}

/// <summary>
/// A split into editor groups, scoped to its zone (Center only in v1). Its children are themselves
/// zone content, so a group can be a stack or a nested split — but always inside this zone.
/// </summary>
public sealed record ZoneSplit : ZoneContent
{
    public ZoneSplit(Orientation orientation, ImmutableList<ZoneContent> children, ImmutableList<double> weights)
    {
        if (children.Count < 2)
        {
            throw new ArgumentException("a zone split holds at least two children", nameof(children));
        }

        if (children.Count != weights.Count)
        {
            throw new ArgumentException("one weight per child", nameof(weights));
        }

        Orientation = orientation;
        Children = children;
        Weights = SplitNode.Normalize(weights);
    }

    public Orientation Orientation { get; init; }

    public ImmutableList<ZoneContent> Children { get; init; }

    public ImmutableList<double> Weights { get; init; }

    public override IEnumerable<Surface> Surfaces() => Children.SelectMany(c => c.Surfaces());
}

/// <summary>
/// One zone's state: what it holds, its cross-axis size relative to the Center, and whether it is
/// collapsed to a rail. The Center is never collapsed and its <see cref="Extent"/> is ignored (it
/// takes the remaining space).
/// </summary>
public sealed record ZoneState(ZoneId Id, ZoneContent? Content, double Extent, bool Collapsed)
{
    /// <summary>Default cross-axis extent for a tool zone, as a proportion of the frame.</summary>
    public const double DefaultExtent = 0.22;

    public bool IsEmpty => Content is null;

    public IEnumerable<Surface> Surfaces() => Content?.Surfaces() ?? [];
}

/// <summary>The arrangement to restore a maximized zone or pane back to.</summary>
public sealed record MaximizeMemo(ZoneId? Zone, string? SurfaceId, WorkbenchLayout Snapshot);

/// <summary>
/// The whole workbench arrangement as named zones: a fixed frame plus the floating stacks held
/// outside it. Replaces the proportional split tree (<see cref="Layout"/>).
/// </summary>
/// <remarks>
/// All four zones are <b>always present</b> in <see cref="Zones"/> — an empty zone has null content,
/// it is never removed. That is what makes "the Center is always there" and "moving a pane cannot
/// delete a zone" structural rather than rules to remember. Floating stacks live outside the frame,
/// unchanged from the tree model (only docked layout changes in ADR-0021).
/// </remarks>
public sealed record WorkbenchLayout(
    ImmutableDictionary<ZoneId, ZoneState> Zones,
    ImmutableList<StackNode> Floating,
    MaximizeMemo? Maximized)
{
    /// <summary>The default arrangement: graph document in the Center, a terminal in the Bottom.</summary>
    public static WorkbenchLayout Default()
    {
        var center = new ZoneStack(
        [
            new Surface("graph", "canvas", "Graph"),
            new Surface("domain", "view", "Domain"),
            new Surface("sessions", "sessions", "Sessions"),
            new Surface("board", "board", "Board"),
            new Surface("leaderboard", "leaderboard", "Leaderboard"),
            new Surface("ledger", "ledger", "Ledger"),
        ]);

        var left = new ZoneStack(
        [
            new Surface("explore", "view", "Explore"),
            new Surface("provenance", "inspector", "Provenance"),
            new Surface("contexts", "contexts", "Contexts"),
            new Surface("joins", "joins", "Joins"),
        ]);

        var bottom = new ZoneStack(
            [new Surface("terminal-1", "terminal", "Terminal — pwsh")]);

        var zones = ImmutableDictionary.CreateRange(new[]
        {
            KeyValuePair.Create(ZoneId.Left, new ZoneState(ZoneId.Left, left, ZoneState.DefaultExtent, Collapsed: false)),
            KeyValuePair.Create(ZoneId.Right, new ZoneState(ZoneId.Right, Content: null, ZoneState.DefaultExtent, Collapsed: false)),
            KeyValuePair.Create(ZoneId.Bottom, new ZoneState(ZoneId.Bottom, bottom, 0.30, Collapsed: false)),
            KeyValuePair.Create(ZoneId.Center, new ZoneState(ZoneId.Center, center, Extent: 1.0, Collapsed: false)),
        });

        return new WorkbenchLayout(zones, [], Maximized: null);
    }

    /// <summary>An empty frame — all four zones present, none with content. Used by the converter as a base.</summary>
    public static WorkbenchLayout Empty()
    {
        var zones = ImmutableDictionary.CreateRange(
            Enum.GetValues<ZoneId>().Select(id => KeyValuePair.Create(
                id, new ZoneState(id, Content: null,
                    id == ZoneId.Center ? 1.0 : ZoneState.DefaultExtent, Collapsed: false))));
        return new WorkbenchLayout(zones, [], Maximized: null);
    }

    public ZoneState Zone(ZoneId id) => Zones[id];

    public IEnumerable<Surface> AllSurfaces() =>
        Zones.Values.SelectMany(z => z.Surfaces()).Concat(Floating.SelectMany(s => s.Surfaces));

    /// <summary>The zone currently holding <paramref name="surfaceId"/>, or null if it is floating/absent.</summary>
    public ZoneId? FindZoneOf(string surfaceId) =>
        Zones.Values.FirstOrDefault(z => z.Surfaces().Any(s => s.SurfaceId == surfaceId))?.Id;

    /// <summary>Replaces one zone's state, leaving the other three byte-identical (the containment primitive).</summary>
    public WorkbenchLayout WithZone(ZoneState zone) =>
        this with { Zones = Zones.SetItem(zone.Id, zone) };

    /// <summary>
    /// The frame invariant, checked after every operation: the four zones exist, the Center is never
    /// collapsed, no surface appears twice, and no stack is empty.
    /// </summary>
    public void AssertInvariant()
    {
        foreach (var id in Enum.GetValues<ZoneId>())
        {
            if (!Zones.ContainsKey(id))
            {
                throw new InvalidOperationException($"zone '{id}' is missing — all four zones are always present");
            }
        }

        if (Zones[ZoneId.Center].Collapsed)
        {
            throw new InvalidOperationException("the Center zone is never collapsed");
        }

        var surfaceIds = AllSurfaces().Select(s => s.SurfaceId).ToList();
        if (surfaceIds.Count != surfaceIds.Distinct(StringComparer.Ordinal).Count())
        {
            throw new InvalidOperationException("a surface appears in more than one zone");
        }
    }

    /// <summary>
    /// Structural signature ignoring extents — the oracle for "which zone holds what, in what order".
    /// Two layouts with the same shape are the same arrangement of panes.
    /// </summary>
    public string Shape()
    {
        var zones = Enum.GetValues<ZoneId>()
            .Select(id => $"{id}:{ShapeOf(Zones[id].Content)}{(Zones[id].Collapsed ? "/collapsed" : "")}");
        return string.Join("|", zones) + "|float:" +
            string.Join(",", Floating.Select(f => "[" + string.Join("+", f.Surfaces.Select(s => s.SurfaceId)) + "]"));
    }

    private static string ShapeOf(ZoneContent? content) => content switch
    {
        null => "-",
        ZoneStack s => $"[{string.Join("+", s.Tabs.Select(t => t.SurfaceId))}@{s.ActiveIndex}]",
        ZoneSplit p => $"({p.Orientation}:{string.Join(",", p.Children.Select(ShapeOf))})",
        _ => "?",
    };
}
