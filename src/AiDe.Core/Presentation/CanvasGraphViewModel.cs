using AiDe.Core.Facts;
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
    /// <summary>
    /// How many nodes the canvas asks for when nothing is focused.
    /// </summary>
    /// <remarks>
    /// The projection's own ceiling, so the canvas asks for everything the read surface will give.
    /// A lower number here omitted 813 of TheTerrace's 2,813 nodes — a limit the SURFACE imposed on
    /// itself while the store and the projection were both willing. How much of it to draw at once
    /// is a rendering decision and belongs with the renderer; withholding it here would make that
    /// decision on the renderer's behalf and hide the rest.
    /// </remarks>
    /// <summary>
    /// Nodes in the default overview, before it says what it left out.
    /// </summary>
    /// <remarks>
    /// <para><b>Derived from a measurement, and from the spec.</b> MEASURED on a real repository: the
    /// whole graph serialises to <b>1,522,284 bytes</b> against a 1 MiB frame — so the previous
    /// default could not be delivered at all (INV-0003), and the user saw "the daemon closed the
    /// connection". Declared-only nodes are ~294 bytes each including their edges, so a frame holds
    /// roughly 3,500 of them; this cap keeps real headroom under that.</para>
    ///
    /// <para><b>But the size is the smaller reason.</b> <c>docs/specs/knowledge-exploration.md</c>
    /// US-K2 says the whole graph is never rendered at once, and a 2,815-node hairball is unreadable
    /// even when it fits. The fix for "one arbitrary alphabetical node" was a bounded overview of
    /// MEANINGFUL nodes; loading everything over-corrected past it. What is dropped is counted and
    /// reported, which is the part that makes a bounded view honest rather than a smaller lie.</para>
    /// </remarks>
    public const int OverviewNodeCap = 1_500;

    /// <summary>Retained name for callers that want the projection's own ceiling.</summary>
    public const int WholeGraphNodeCap = GraphProjection.DefaultMaxNodes;

    /// <summary>Every node and edge in the workspace, bounded and honest about the bound.</summary>
    /// <summary>
    /// The default overview: this workspace's own code, bounded and honest about the bound.
    /// </summary>
    /// <remarks>
    /// <para><b>Bounded, not whole — corrected by INV-0003.</b> This asked for the whole graph, which
    /// on a real repository is 1,522,284 bytes against a 1 MiB frame, so it could not be delivered:
    /// the user saw "the daemon closed the connection without responding". The transport failure
    /// exposed the deeper error, which is that the spec never asked for a whole graph
    /// (<c>knowledge-exploration.md</c> US-K2 — "the whole graph is never rendered at once").</para>
    ///
    /// <para><b>Both defaults were wrong in opposite directions.</b> Before, this drew one
    /// alphabetically-first node and its neighbours; the fix for that loaded everything. The answer
    /// to "one arbitrary node" was a bounded overview of MEANINGFUL nodes — declared here, ranked by
    /// degree, capped, with what it dropped counted and said out loud.</para>
    ///
    /// <para><b>External nodes are excluded from the DEFAULT, not from the product.</b> Measured: the
    /// six most-connected nodes of a real repository were <c>string</c>, <c>int</c>,
    /// <c>Task&lt;T&gt;</c>, <c>DateTimeOffset</c>, <c>IReadOnlyList&lt;T&gt;</c> and <c>Guid</c>.
    /// A first view centred on the BCL is not a picture of anybody's domain. Callers who want them
    /// pass their own <see cref="GraphQuery"/>.</para>
    /// </remarks>
    private async Task<CanvasGraph> WholeGraphAsync(CancellationToken cancellationToken)
    {
        var graph = await queries!
            .GraphAsync(new GraphQuery(OverviewNodeCap, IncludeExternal: false), cancellationToken)
            .ConfigureAwait(false);

        if (graph.Nodes.Count == 0)
        {
            return Empty("Nothing indexed yet. Run \"Index C# projects in this workspace\".");
        }

        var kept = graph.Nodes
            .Where(n => ContextFilter is null
                || string.Equals(ContextOf(n.Id), ContextFilter, StringComparison.Ordinal))
            .ToList();

        var visible = kept.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        var filtered = graph.Nodes.Count - kept.Count;

        // The caption states the bound rather than implying completeness. "1,500 node(s)" and
        // "1,500 of 2,118" are different claims, and only one of them is true here.
        var message = graph.Omitted > 0
            ? $"{kept.Count:N0} of {graph.Nodes.Count + graph.Omitted:N0} node(s) declared here, " +
              $"most connected first. {graph.Omitted:N0} not drawn — search or pick a node to go deeper."
            : $"{kept.Count:N0} node(s) declared here, {graph.Edges.Count:N0} edge(s).";

        if (ContextFilter is not null)
        {
            message += $" Showing only {ContextFilter}. {filtered:N0} node(s) in other contexts hidden.";
        }

        return new CanvasGraph(
            [.. kept.Select(n => new CanvasNode(n.Id, n.Label, n.Kind, IsRoot: false, ContextOf(n.Id)))],
            [.. graph.Edges
                .Where(e => visible.Contains(e.From) && visible.Contains(e.To))
                .Select(e => new CanvasEdge(e.From, e.To, e.Predicate, e.Status.ToString()))],
            RootId: null,
            graph.Omitted,
            graph.Disclosures,
            message);
    }

    public async Task<CanvasGraph> LoadAsync(
        string? rootId = null, int maxNeighbors = 40, CancellationToken cancellationToken = default)
    {
        if (queries is null)
        {
            return Empty("No workspace is open. Open one to see its graph.");
        }

        try
        {
            // NO ROOT means the WHOLE GRAPH, not one arbitrary node's neighbourhood. This used to
            // ask for a single node (FindAsync with a limit of 1) and then draw its neighbours, so a
            // workspace of 12,100 assertions across 2,164 nodes rendered as TWO — the alphabetically
            // first symbol and its one neighbour. A root is a drill-down, not the default.
            if (string.IsNullOrWhiteSpace(rootId))
            {
                return await WholeGraphAsync(cancellationToken).ConfigureAwait(false);
            }

            var describe = await queries.DescribeAsync(rootId, maxNeighbors, cancellationToken).ConfigureAwait(false);

            var nodes = new List<CanvasNode>
            {
                new(describe.Node.NodeId, describe.Node.DisplayLabel, describe.Node.NodeKind,
                    IsRoot: true, Context: ContextOf(describe.Node.NodeId)),
            };

            var edges = new List<CanvasEdge>();
            var filtered = 0;
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

                var otherContext = ContextOf(other);
                if (ContextFilter is not null &&
                    !string.Equals(otherContext, ContextFilter, StringComparison.Ordinal))
                {
                    filtered++;
                    continue;
                }

                if (seen.Add(other))
                {
                    nodes.Add(new CanvasNode(other, Shorten(other), "source", IsRoot: false, Context: otherContext));
                }

                edges.Add(new CanvasEdge(edge.Subject, edge.Object, edge.Predicate, edge.Status.ToString()));
            }

            // A filter that hid things silently would look like a small graph. The count is stated,
            // and so is the filter itself.
            var message = edges.Count == 0
                ? ContextFilter is null
                    ? $"{describe.Node.DisplayLabel} has no recorded relationships."
                    : $"{describe.Node.DisplayLabel} has no neighbours in {ContextFilter}."
                : ContextFilter is not null
                    ? $"Showing only {ContextFilter}. {filtered} neighbour(s) in other contexts hidden."
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
    /// <summary>
    /// How one node reaches another, rendered as the same graph the canvas already draws.
    /// </summary>
    /// <remarks>
    /// <para><b>Deliberately returns <see cref="CanvasGraph"/> rather than a route type.</b> A route
    /// IS a subgraph, and giving it its own shape would mean a second renderer, a second set of
    /// bindings and a second place for the two sessions to disagree about what a node looks like.
    /// The design session binds what it already binds; only the caption changes.</para>
    ///
    /// <para><b>The caption carries the weakest link.</b> A route drawn without it looks like a fact
    /// about the code, when one inferred edge anywhere along it makes the whole claim inferred.</para>
    ///
    /// <para><b>Every empty case says which one it is.</b> "No workspace", "that node is not in the
    /// graph" and "there is no route within eight edges" are three different situations with three
    /// different next actions (DC-011).</para>
    /// </remarks>
    public async Task<CanvasGraph> RouteAsync(
        string fromId, string toId, CancellationToken cancellationToken = default)
    {
        if (queries is null)
        {
            return new CanvasGraph([], [], null, 0, [], "No workspace is open.");
        }

        if (string.IsNullOrWhiteSpace(fromId) || string.IsNullOrWhiteSpace(toId))
        {
            return new CanvasGraph([], [], null, 0, [], "Pick a start and an end.");
        }

        var result = await queries
            .PathsAsync(new PathQuery(fromId, toId), cancellationToken)
            .ConfigureAwait(false);

        if (result.Paths.Count == 0)
        {
            // The projection's reason is the useful half — "not in this graph" and "no route within
            // 8 edge(s)" send a user to different places — so it is passed through rather than
            // replaced with a house style.
            return new CanvasGraph(
                [], [], fromId, 0, [],
                result.Reason is null
                    ? $"No route from {Shorten(fromId)} to {Shorten(toId)}."
                    : $"No route: {result.Reason}.");
        }

        var nodes = new List<CanvasNode>();
        var edges = new List<CanvasEdge>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(string id, bool isEndpoint)
        {
            if (seen.Add(id))
            {
                nodes.Add(new CanvasNode(
                    id, Shorten(id), "source", IsRoot: isEndpoint, Context: ContextOf(id)));
            }
        }

        Add(fromId, isEndpoint: true);
        Add(toId, isEndpoint: true);

        foreach (var path in result.Paths)
        {
            foreach (var edge in path.Edges)
            {
                Add(edge.From, isEndpoint: false);
                Add(edge.To, isEndpoint: false);

                edges.Add(new CanvasEdge(edge.From, edge.To, edge.Predicate, edge.Status.ToString()));
            }
        }

        // The same pair can appear on two routes of equal length; drawing it twice would render a
        // thicker line that means nothing.
        var distinct = edges
            .GroupBy(e => (e.From, e.To, e.Predicate))
            .Select(g => g.First())
            .ToList();

        var shortest = result.Paths.Min(p => p.Edges.Count);
        var weakest = result.Paths.Min(p => p.Status);

        var route = result.Paths.Count == 1 ? "1 route" : $"{result.Paths.Count} routes";
        var hops = shortest == 1 ? "1 edge" : $"{shortest} edges";

        var confidence = weakest == VerificationStatus.Verified
            ? "every edge is verified"
            : $"the weakest link is {weakest}";

        var truncation = result.Truncated ? " More routes of the same length exist." : string.Empty;

        return new CanvasGraph(
            nodes, distinct, fromId, 0, [],
            $"{route} from {Shorten(fromId)} to {Shorten(toId)}, shortest {hops}; {confidence}.{truncation}");
    }

    public Func<string, string?> ContextLookup { get; set; } = _ => null;

    /// <summary>
    /// When set, only nodes in this context are drawn.
    /// </summary>
    /// <remarks>
    /// The ROOT is kept even when it is outside the filter, and labelled as such. Dropping it would
    /// leave a graph with no anchor and no way back — a filter that can strip the thing you were
    /// looking at is one nobody trusts twice.
    /// </remarks>
    public string? ContextFilter { get; set; }

    private string? ContextOf(string nodeId) => ContextLookup(nodeId);

    /// <summary>The last path segment, so a canvas of fully-qualified names stays readable.</summary>
    private static string Shorten(string nodeId)
    {
        var cut = nodeId.LastIndexOf('.');
        return cut > 0 && cut < nodeId.Length - 1 ? nodeId[(cut + 1)..] : nodeId;
    }

    private static CanvasGraph Empty(string message) => new([], [], null, 0, [], message);
}
