using AiDe.Core.Workbench;

namespace AiDe.App.Workbench;

/// <summary>
/// Where a new reference-document surface (class diagram, code viewer) should open. A document must
/// never be tabbed on top of the graph/canvas — that hides the graph, which is the surface the user
/// is almost always working *from* (the "my graph pane disappeared" defect). So the policy is: tab
/// into the focused document stack, else any existing document stack, else split a new stack BESIDE
/// the graph so both stay visible.
/// </summary>
public sealed record DocumentPlacement(string? TabIntoStackId, string? SplitBesideStackId)
{
    public bool IsSplit => SplitBesideStackId is not null;
}

/// <summary>Pure placement policy for reference-document surfaces, so it is verifiable headlessly.</summary>
public static class DocumentPlacementPolicy
{
    // The reference-document kinds this policy governs. A prompt draft has its own placement (beside a
    // terminal, its transfer target); terminals dock to the terminal stack — neither is governed here.
    private static readonly HashSet<string> DocumentKinds =
        new(StringComparer.Ordinal) { "classdiagram", "codeviewer" };

    private static bool IsCanvas(StackNode s) => s.Surfaces.Any(su => su.Kind == "canvas");

    private static bool IsDocumentStack(StackNode s) =>
        !IsCanvas(s) && s.Surfaces.Any(su => DocumentKinds.Contains(su.Kind));

    public static DocumentPlacement? Decide(Layout layout, string? activeSurfaceId)
    {
        var stacks = layout.AllStacks().ToList();
        if (stacks.Count == 0) { return null; }

        // 1. The focused stack, when it is already a document stack — honour where the user is working.
        if (activeSurfaceId is not null)
        {
            var focused = stacks.FirstOrDefault(s => s.Surfaces.Any(su => su.SurfaceId == activeSurfaceId));
            if (focused is not null && IsDocumentStack(focused))
            {
                return new DocumentPlacement(focused.Id, null);
            }
        }

        // 2. Any existing document stack — keep documents together, away from the graph.
        var doc = stacks.FirstOrDefault(IsDocumentStack);
        if (doc is not null) { return new DocumentPlacement(doc.Id, null); }

        // 3. No document region yet — split one BESIDE the graph so the graph stays visible.
        var graph = stacks.FirstOrDefault(IsCanvas);
        if (graph is not null) { return new DocumentPlacement(null, graph.Id); }

        // 4. No graph at all — fall back to any stack (tab in).
        return new DocumentPlacement(stacks[0].Id, null);
    }
}
