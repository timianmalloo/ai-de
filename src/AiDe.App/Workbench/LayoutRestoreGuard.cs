using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Guards a workspace-open layout restore against a degenerate saved arrangement. A per-workspace
/// layout persists (US-9), but a saved layout that has lost the primary graph pane restores to a
/// scattered, graph-less workbench — and the user experiences opening the workspace as "it reset my
/// panes and lost the graph." When that happens, keep the pre-restore layout (which has the graph)
/// rather than applying the degenerate one. Pure, so it is verifiable headlessly.
/// </summary>
public static class LayoutRestoreGuard
{
    public static bool HasCanvas(Layout layout) =>
        layout.AllStacks().SelectMany(s => s.Surfaces).Any(su => su.Kind == "canvas");

    /// <summary>
    /// True when the restore should be rejected in favour of the pre-restore layout: the previous
    /// layout had the graph and the restored one lost it. (If the previous had no graph either, there
    /// is nothing to protect and the restore stands.)
    /// </summary>
    public static bool ShouldKeepPrevious(Layout before, Layout restored) =>
        HasCanvas(before) && !HasCanvas(restored);
}
