using AiDe.Core.Projections;

namespace AiDe.Core.Presentation;

/// <summary>One node as the canvas draws it.</summary>
public sealed record CanvasNode(string Id, string Label, string Kind, bool IsRoot, string? Context = null);

/// <summary>One edge as the canvas draws it.</summary>
public sealed record CanvasEdge(string From, string To, string Predicate, string Status)
{
    /// <summary>
    /// True for a join across artifact types — code to schema, schema to infrastructure.
    /// </summary>
    /// <remarks>
    /// Drawn differently because it is a different KIND of claim. An edge inside one artifact was
    /// resolved by a compiler; a join between two was resolved by a convention or a literal match,
    /// and it looks more authoritative than it is precisely because it spans more.
    /// </remarks>
    public bool IsJoin => Predicate is "maps_to" or "hosted_on" or "is_declared_secret";

    /// <summary>True when the claim is a convention rather than a declaration.</summary>
    public bool IsInferred => string.Equals(Status, "Inferred", StringComparison.Ordinal);
}

/// <summary>
/// What the canvas renders, including what it could not show.
/// </summary>
/// <param name="Omitted">
/// Edges the bounded projection left out. Carried into the page, because a graph that quietly drops
/// half its edges looks like a small graph rather than a truncated one.
/// </param>
/// <param name="Disclosures">
/// What the extractor could not analyse for the scopes behind this view. Distinct from
/// <paramref name="Omitted"/>: those edges exist and were not returned, these were never extracted.
/// </param>
public sealed record CanvasGraph(
    IReadOnlyList<CanvasNode> Nodes,
    IReadOnlyList<CanvasEdge> Edges,
    string? RootId,
    int Omitted,
    IReadOnlyList<string> Disclosures,
    string? Message);

/// <summary>
/// Builds the canvas's view from the same read surface every other pane uses.
/// </summary>
/// <remarks>
/// <para><b>In Core, with no WPF and no browser.</b> The canvas is a rendering of a projection, and
/// what it shows is decidable — and testable — without a window. Putting this in the WPF layer would
/// make "does the graph show the right nodes" a question only answerable by looking at one.</para>
///
/// <para><b>Every empty case gets its own message.</b> "No workspace", "nothing indexed yet" and "a
/// node with no neighbours" are three different situations with three different next actions, and a
/// blank canvas for all three tells the user nothing (<b>DC-011</b>).</para>
/// </remarks>
public sealed class CanvasGraphViewModel(IWorkspaceQueries? queries)
{
    /// <summary>The graph around <paramref name="rootId"/>, or around whatever Find offers first.</summary>
    public async Task<CanvasGraph> LoadAsync(
        string? rootId = null, int maxNeighbors = 40, CancellationToken cancellationToken = default)
    {
        if (queries is null)
        {
            return Empty("No workspace is open. Open one to see its graph.");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(rootId))
            {
                var candidates = await queries.FindAsync(string.Empty, 1, cancellationToken).ConfigureAwait(false);
                rootId = candidates.Matches.FirstOrDefault()?.NodeId;

                if (string.IsNullOrWhiteSpace(rootId))
                {
                    return Empty("Nothing indexed yet. Run \"Index C# projects in this workspace\".");
                }
            }

            var describe = await queries.DescribeAsync(rootId, maxNeighbors, cancellationToken).ConfigureAwait(false);

            var nodes = new List<CanvasNode>
            {
                new(describe.Node.NodeId, describe.Node.DisplayLabel, describe.Node.NodeKind,
                    IsRoot: true, Context: ContextOf(describe.Node.NodeId)),
            };

            var edges = new List<CanvasEdge>();
            var seen = new HashSet<string>(StringComparer.Ordinal) { describe.Node.NodeId };
            var disclosures = new List<string>();

            foreach (var edge in describe.Neighbors)
            {
                // Scope disclosures are facts like anything else, so they arrive as edges. They are
                // lifted OUT of the graph rather than drawn: a "discloses" arrow to a node called
                // "packages-not-restored" is noise on a canvas and a warning in a caption.
                if (edge.Predicate == "discloses")
                {
                    disclosures.Add(edge.Object);
                    continue;
                }

                var other = string.Equals(edge.Subject, describe.Node.NodeId, StringComparison.Ordinal)
                    ? edge.Object
                    : edge.Subject;

                if (seen.Add(other))
                {
                    nodes.Add(new CanvasNode(other, Shorten(other), "source", IsRoot: false, Context: ContextOf(other)));
                }

                edges.Add(new CanvasEdge(edge.Subject, edge.Object, edge.Predicate, edge.Status.ToString()));
            }

            var message = edges.Count == 0
                ? $"{describe.Node.DisplayLabel} has no recorded relationships."
                : null;

            return new CanvasGraph(
                nodes, edges, describe.Node.NodeId, describe.Bounds.OmittedEdges,
                disclosures.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList(), message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The canvas is one pane. A workspace that cannot answer must not take the shell down,
            // and the pane says what happened rather than rendering an empty graph that reads as
            // "there is nothing here".
            return Empty($"The graph could not be loaded: {ex.Message}");
        }
    }

    /// <summary>
    /// The declared context a node belongs to, or null.
    /// </summary>
    /// <remarks>
    /// Null is a real answer and is drawn as such: a node in no context is uncovered, not
    /// unimportant, and colouring it as though it belonged somewhere would be the inference
    /// ADR-0016 refuses.
    /// </remarks>
    public Func<string, string?> ContextLookup { get; set; } = _ => null;

    private string? ContextOf(string nodeId) => ContextLookup(nodeId);

    /// <summary>The last path segment, so a canvas of fully-qualified names stays readable.</summary>
    private static string Shorten(string nodeId)
    {
        var cut = nodeId.LastIndexOf('.');
        return cut > 0 && cut < nodeId.Length - 1 ? nodeId[(cut + 1)..] : nodeId;
    }

    private static CanvasGraph Empty(string message) => new([], [], null, 0, [], message);
}
