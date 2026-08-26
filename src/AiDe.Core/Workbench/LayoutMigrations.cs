namespace AiDe.Core.Workbench;

/// <summary>
/// One step up the schema ladder: transforms a layout written at <see cref="FromVersion"/> into the
/// shape the next version expects.
/// </summary>
/// <remarks>
/// Migrations operate on the **DTO**, not the domain model, deliberately. A migration's whole job is
/// to read a shape the current domain model can no longer represent; running it through today's
/// types would defeat the purpose, because those types are exactly what changed.
/// </remarks>
public sealed record LayoutMigration(int FromVersion, Func<LayoutDto, LayoutDto> Apply);

public static class LayoutMigrations
{
    /// <summary>
    /// The shipped migration chain.
    /// </summary>
    /// <remarks>
    /// Empty until the schema first changes — but the *mechanism* ships from day one, because a
    /// migration hook added after the first breaking change is added too late for every layout
    /// already on disk. The v1→v2 entry below is the worked example the round-trip spike exercises.
    /// </remarks>
    public static IReadOnlyList<LayoutMigration> Default { get; } =
    [
        // v1 → v2: the terminal surface was renamed when sessions gained stable identities.
        // Renaming without a migration would silently drop the pane from every saved layout —
        // the user would open the app and find their terminal simply gone from the arrangement.
        new(1, dto => RenameSurface(dto, "terminal-1", "terminal.session.1")),
    ];

    /// <summary>Rewrites one surface id throughout a layout, preserving its position and tab order.</summary>
    public static LayoutDto RenameSurface(LayoutDto dto, string oldId, string newId) =>
        new(RenameIn(dto.Root, oldId, newId),
            [.. dto.Floating.Select(f => RenameIn(f, oldId, newId))]);

    private static NodeDto RenameIn(NodeDto node, string oldId, string newId) => node with
    {
        Children = node.Children is null ? null : [.. node.Children.Select(c => RenameIn(c, oldId, newId))],
        Surfaces = node.Surfaces is null
            ? null
            : [.. node.Surfaces.Select(s => s.SurfaceId == oldId ? s with { SurfaceId = newId } : s)],
    };

    /// <summary>Drops a surface the current release no longer ships, healing the tree around it.</summary>
    public static LayoutDto RemoveSurface(LayoutDto dto, string surfaceId) =>
        new(RemoveIn(dto.Root, surfaceId) ?? dto.Root,
            [.. dto.Floating.Select(f => RemoveIn(f, surfaceId)).OfType<NodeDto>()]);

    private static NodeDto? RemoveIn(NodeDto node, string surfaceId)
    {
        if (node.Kind == "stack")
        {
            var kept = (node.Surfaces ?? []).Where(s => s.SurfaceId != surfaceId).ToList();
            // A stack with nothing left ceases to exist rather than persisting empty — the same
            // invariant the domain model enforces, applied at the DTO layer where the model cannot.
            return kept.Count == 0 ? null : node with { Surfaces = kept };
        }

        var children = (node.Children ?? []).Select(c => RemoveIn(c, surfaceId)).OfType<NodeDto>().ToList();
        return children.Count switch
        {
            0 => null,
            1 => children[0],
            _ => node with
            {
                Children = children,
                Weights = [.. Enumerable.Repeat(1.0 / children.Count, children.Count)],
            },
        };
    }
}
