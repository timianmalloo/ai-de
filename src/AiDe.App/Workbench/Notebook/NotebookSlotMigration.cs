using System.Collections.Immutable;
using AiDe.App.Workbench.Sessions;
using AiDe.Core.Workbench;

namespace AiDe.App.Workbench.Notebook;

/// <summary>
/// The slot migration by stable id for a session's pane (ADR-0045 §7; the WL-S design's §4 and §7). A saved
/// <c>session-document:&lt;session&gt;</c> placement becomes the <c>notebook</c> of the same session: the same zone, the
/// same tab position and the same saved caption. Only the kind and the id prefix change, so the session's identity is kept.
/// </summary>
/// <remarks>
/// A placement the migration cannot map stays a <c>session-document</c>, which admission then drops with the
/// <see cref="SurfaceContentFactory.RetiredKinds"/> sentence. That covers an id with no session, and a session whose
/// notebook the slot already holds (two tabs of one id would be one surface twice). A <c>prompt</c> placement is left as it
/// is (Ruling 173): the notebook does not yet offer the prompt draft's six capabilities, so the migration names it as kept and
/// the restore report says so. Floating windows are not walked here.
/// <c>LayoutPersistence</c>'s floating guard (Ruling 171) owns what a restored floating window becomes.
/// </remarks>
public static class NotebookSlotMigration
{
    /// <summary>The retired kind this migration maps (ADR-0045 §2).</summary>
    public const string SessionDocumentKind = "session-document";

    /// <summary>The kind this migration leaves as it is (Ruling 173): a prompt draft stays a prompt draft.</summary>
    public const string PromptKind = "prompt";

    /// <summary>
    /// <paramref name="saved"/> with every mappable <c>session-document</c> placement in its zones (and in its maximize
    /// snapshot) replaced by its notebook, the notebooks it made, in zone order, and the prompt drafts it kept as they were.
    /// </summary>
    public static (WorkbenchLayout Layout, IReadOnlyList<Surface> Migrated, IReadOnlyList<Surface> KeptPrompts) Migrate(WorkbenchLayout saved)
    {
        ArgumentNullException.ThrowIfNull(saved);

        var taken = saved.AllSurfaces().Select(s => s.SurfaceId).ToHashSet(StringComparer.Ordinal);
        var migrated = new List<Surface>();
        var zones = saved.Zones.ToImmutableDictionary(z => z.Key, z => z.Value with { Content = Map(z.Value.Content, taken, migrated) });

        var memo = saved.Maximized is { } maximized
            ? maximized with
            {
                SurfaceId = maximized.SurfaceId is { } id && NotebookIdFor(id) is { } notebook && migrated.Any(m => m.SurfaceId == notebook) ? notebook : maximized.SurfaceId,
                Snapshot = Migrate(maximized.Snapshot).Layout,
            }
            : null;

        var kept = saved.AllSurfaces().Where(s => string.Equals(s.Kind, PromptKind, StringComparison.Ordinal)).ToList();
        return (saved with { Zones = zones, Maximized = memo }, migrated, kept);
    }

    private static ZoneContent? Map(ZoneContent? content, HashSet<string> taken, List<Surface> migrated) => content switch
    {
        null => null,
        ZoneStack stack => stack with { Tabs = [.. stack.Tabs.Select(s => Map(s, taken, migrated))] },
        ZoneSplit split => split with { Children = [.. split.Children.Select(c => Map(c, taken, migrated)!)] },
        _ => throw new InvalidOperationException($"zone content '{content.GetType().Name}' has no migration"),
    };

    private static Surface Map(Surface surface, HashSet<string> taken, List<Surface> migrated)
    {
        if (!string.Equals(surface.Kind, SessionDocumentKind, StringComparison.Ordinal)
            || NotebookIdFor(surface.SurfaceId) is not { } id
            || !taken.Add(id))
        {
            return surface;
        }

        var notebook = new Surface(id, SessionDocumentSurface.Kind, surface.Title);
        migrated.Add(notebook);
        return notebook;
    }

    // `session-document:<session>` → `notebook:<session>`, or null when the id names no session.
    private static string? NotebookIdFor(string surfaceId) =>
        surfaceId.StartsWith(SessionDocumentKind + ":", StringComparison.Ordinal) && surfaceId.Length > SessionDocumentKind.Length + 1
            ? SessionDocumentSurface.SurfaceIdFor(surfaceId[(SessionDocumentKind.Length + 1)..])
            : null;
}
